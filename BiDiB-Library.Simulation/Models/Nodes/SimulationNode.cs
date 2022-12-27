using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using org.bidib.netbidibc.core;
using org.bidib.netbidibc.core.Enumerations;
using org.bidib.netbidibc.core.Message;
using org.bidib.netbidibc.core.Models.BiDiB;
using org.bidib.netbidibc.core.Models.BiDiB.Extensions;
using org.bidib.netbidibc.core.Models.Messages.Input;
using org.bidib.netbidibc.core.Utils;
using Node = org.bidib.netbidibc.core.Models.BiDiB.Node;
using Timer = System.Timers.Timer;

namespace org.bidib.nbidibc.Simulation.Models.Nodes
{
    public class SimulationNode : Node, IDisposable
    {
        private Queue<SimulationNode> nodeTabNodes;
        private int nodeTabVersion;
        private int featureIndex;
        private readonly Timer valueChangeTimer;

        public SimulationNode()
        {
            Children = new List<SimulationNode>();
            CvValues = new Dictionary<string, string>();
            AccessoryAspects = new Dictionary<byte, byte>();
            Features = Array.Empty<Feature>();

            nodeTabVersion = 0;

            valueChangeTimer = new Timer(1000) { AutoReset = true };
            valueChangeTimer.Elapsed += HandleValueChangeTimerOnElapsed;
        }

        public ICollection<SimulationNode> Children { get; }
        protected Dictionary<string, string> CvValues { get; }
        protected Dictionary<byte, byte> AccessoryAspects { get; }

        public string SoftwareVersion { get; set; }
        public string ProtocolVersion { get; set; }
        public string ProductName { get; set; }

        protected Action<byte[]> AddResponse { get; private set; }

        protected byte LastSequenceNumber { get; set; }
        protected byte LastValueChangedCycle { get; set; }
        protected bool SysEnabled { get; private set; }

        public void Initialize(Action<byte[]> addResponse)
        {
            AddResponse = addResponse;
            PrepareFeatures();
        }

        public virtual void Start()
        {
            Feature feature = this.GetFeature(BiDiBFeature.FEATURE_BM_SIZE);
            if (feature?.Value > 0)
            {
                List<FeedbackPort> feedbackPorts = new List<FeedbackPort>();
                for (int i = 0; i < feature.Value; i++)
                {
                    feedbackPorts.Add(new FeedbackPort { Number = i, IsFree = true });
                }
                FeedbackPorts = feedbackPorts.ToArray();
            }

            feature = this.GetFeature(BiDiBFeature.FEATURE_ACCESSORY_COUNT);
            if (feature?.Value > 0 && !AccessoryAspects.Any())
            {
                AccessoryAspects.Add(0, 1);
                AccessoryAspects.Add(1, 1);
                AccessoryAspects.Add(2, 0);
                AccessoryAspects.Add(3, 0);
            }

            valueChangeTimer.Start();
        }

        public void Stop()
        {
            SysEnabled = false;
            valueChangeTimer.Stop();
        }

        protected virtual void PrepareFeatures()
        {
            AddOrUpdateFeature(BiDiBFeature.FEATURE_STRING_SIZE, 24);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_RELEVANT_PID_BITS, 16);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_FW_UPDATE_MODE, 1);
        }

        protected void AddOrUpdateFeature(BiDiBFeature featureType, byte value)
        {
            var feature = Features.FirstOrDefault(x => x.FeatureType == featureType);
            if (feature == null)
            {
                feature = new Feature { FeatureId = (byte)featureType };
                var featuresList = Features.ToList();
                featuresList.Add(feature);
                Features = featuresList.ToArray();
            }

            feature.Value = value;
        }

        public void HandleMessage(BiDiBInputMessage message, Action<byte[]> addResponse)
        {
            OnHandleMessage(message, addResponse);
        }

        protected virtual void OnHandleMessage(BiDiBInputMessage message, Action<byte[]> addResponse)
        {
            switch (message.MessageType)
            {
                case BiDiBMessage.MSG_SYS_ENABLE:
                    {
                        SysEnabled = true;
                        ForwardToChildren(message, addResponse);
                        break;
                    }

                case BiDiBMessage.MSG_SYS_DISABLE:
                    {
                        SysEnabled = false;
                        ForwardToChildren(message, addResponse);
                        break;
                    }
                case BiDiBMessage.MSG_SYS_GET_MAGIC:
                    {
                        addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_SYS_MAGIC, message.SequenceNumber, 0xFE, 0xAF));
                        break;
                    }
                case BiDiBMessage.MSG_SYS_PING:
                    {
                        addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_SYS_PONG, message.SequenceNumber));
                        break;
                    }
                case BiDiBMessage.MSG_SYS_GET_P_VERSION:
                    {
                        byte[] protocol = GetBytesFromDotted(ProtocolVersion);
                        addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_SYS_P_VERSION, message.SequenceNumber, protocol));
                        break;
                    }
                case BiDiBMessage.MSG_SYS_GET_SW_VERSION:
                    {
                        byte[] software = GetBytesFromDotted(SoftwareVersion);
                        addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_SYS_SW_VERSION, message.SequenceNumber, software));
                        break;
                    }
                case BiDiBMessage.MSG_SYS_GET_UNIQUE_ID:
                    {
                        addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_SYS_UNIQUE_ID, message.SequenceNumber, UniqueIdBytes));
                        break;
                    }
                case BiDiBMessage.MSG_NODETAB_GETALL:
                    {
                        if (HasSubNodesFunctions)
                        {
                            nodeTabNodes = new Queue<SimulationNode>();
                            nodeTabNodes.Enqueue(this);
                            foreach (SimulationNode child in Children)
                            {
                                nodeTabNodes.Enqueue(child);
                            }
                            nodeTabVersion++;
                            addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_NODETAB_COUNT, message.SequenceNumber, Convert.ToByte(Children.Count + 1)));
                        }
                        break;
                    }
                case BiDiBMessage.MSG_NODETAB_GETNEXT:
                    {
                        if (nodeTabNodes?.Count > 0)
                        {
                            SimulationNode node = nodeTabNodes.Dequeue();
                            int nodeIndex = node == this ? 0 : node.Address.Last();
                            List<byte> parameters = new List<byte> { Convert.ToByte(nodeTabVersion), Convert.ToByte(nodeIndex) };
                            parameters.AddRange(node.UniqueIdBytes);
                            addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_NODETAB, message.SequenceNumber, parameters.ToArray()));
                        }

                        break;
                    }
                case BiDiBMessage.MSG_VENDOR_ENABLE:
                    {
                        addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_VENDOR_ACK, message.SequenceNumber, 1));
                        break;
                    }
                case BiDiBMessage.MSG_VENDOR_DISABLE:
                    {
                        addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_VENDOR_ACK, message.SequenceNumber, 0));
                        break;
                    }
                case BiDiBMessage.MSG_VENDOR_GET:
                    {
                        HandleVendorGet(message).ConfigureAwait(false);
                        break;
                    }
                case BiDiBMessage.MSG_VENDOR_SET:
                    {
                        HandleVendorSet(message).ConfigureAwait(false);
                        break;
                    }
                case BiDiBMessage.MSG_ACCESSORY_SET:
                    {
                        HandleAccessorySet(message);
                        break;
                    }
                case BiDiBMessage.MSG_ACCESSORY_GET:
                    {
                        HandleAccessoryGet(message);
                        break;
                    }
                case BiDiBMessage.MSG_ACCESSORY_PARA_GET:
                    {
                        HandleAccessoryPara(message);
                        break;
                    }
                case BiDiBMessage.MSG_CS_SET_STATE:
                    {
                        CommandStationState state = (CommandStationState)message.MessageParameters[0];
                        CommandStationState newState = state == CommandStationState.BIDIB_CS_STATE_QUERY ? CommandStationState.BIDIB_CS_STATE_OFF : state;
                        addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_CS_STATE, message.SequenceNumber, (byte)newState));
                        break;
                    }
                case BiDiBMessage.MSG_FEATURE_GETALL:
                    {
                        featureIndex = 0;
                        addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_FEATURE_COUNT, message.SequenceNumber, Convert.ToByte(Features?.Length, CultureInfo.CurrentCulture)));
                        break;
                    }
                case BiDiBMessage.MSG_FEATURE_GETNEXT:
                    {
                        if (featureIndex < Features.Length)
                        {
                            addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_FEATURE, message.SequenceNumber, Features[featureIndex].FeatureId, Features[featureIndex].Value));
                            featureIndex++;
                        }

                        break;
                    }
                case BiDiBMessage.MSG_FEATURE_GET:
                    {
                        Feature feature = Features.FirstOrDefault(x => x.FeatureId == message.MessageParameters[0]);
                        addResponse.Invoke(feature != null
                            ? BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_FEATURE, message.SequenceNumber, feature.FeatureId, feature.Value)
                            : BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_FEATURE_NA, message.SequenceNumber, message.MessageParameters[0]));
                        break;
                    }
                case BiDiBMessage.MSG_FEATURE_SET:
                    {
                        Feature feature = Features.FirstOrDefault(x => x.FeatureId == message.MessageParameters[0]);
                        if (feature != null)
                        {
                            feature.Value = message.MessageParameters[1];
                            addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_FEATURE, message.SequenceNumber, feature.FeatureId, feature.Value));
                        }
                        else
                        {
                            addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_FEATURE_NA, message.SequenceNumber, message.MessageParameters[0]));
                        }
                        break;
                    }
                case BiDiBMessage.MSG_STRING_GET:
                    {
                        List<byte> parameters = new List<byte> { 0 };
                        if (message.MessageParameters[1] == 0)
                        {
                            parameters.Add(0);
                            parameters.AddRange(GetNameBytes(ProductName));
                        }
                        if (message.MessageParameters[1] == 1)
                        {
                            parameters.Add(1);
                            parameters.AddRange(GetNameBytes(UserName));
                        }

                        if (parameters.Count > 1)
                        {
                            addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_STRING, message.SequenceNumber, parameters.ToArray()));
                        }
                        break;
                    }
                case BiDiBMessage.MSG_BM_GET_RANGE:
                    {
                        Feature feature = Features.FirstOrDefault(x => x.FeatureType == BiDiBFeature.FEATURE_BM_SIZE);
                        if (feature?.Value > 0)
                        {
                            List<bool> feedbackStates = FeedbackPorts.Select(port => !port.IsFree).ToList();
                            BitArray array = new BitArray(feedbackStates.ToArray());
                            byte[] parameters = new byte[2 + feature.Value / 8];
                            parameters[0] = message.MessageParameters[0];
                            parameters[1] = message.MessageParameters[1];
                            array.CopyTo(parameters, 2);
                            addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_BM_MULTIPLE, message.SequenceNumber, parameters));
                        }

                        break;
                    }
                case BiDiBMessage.MSG_SYS_IDENTIFY:
                    {
                        addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_SYS_IDENTIFY_STATE, message.SequenceNumber, message.MessageParameters));
                        break;
                    }
                case BiDiBMessage.MSG_FW_UPDATE_OP:
                    {
                        HandleFirmwareUpdateOperation(message);
                        break;
                    }
            }
        }

        private void HandleFirmwareUpdateOperation(BiDiBInputMessage message)
        {
            FirmwareUpdateOperation operation = (FirmwareUpdateOperation)message.MessageParameters[0];
            switch (operation)
            {
                case FirmwareUpdateOperation.BIDIB_MSG_FW_UPDATE_OP_ENTER:
                    {
                        AddResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_FW_UPDATE_STAT, message.SequenceNumber, (byte)FirmwareUpdateStatus.BIDIB_MSG_FW_UPDATE_STAT_READY, 0));
                        break;
                    }
                case FirmwareUpdateOperation.BIDIB_MSG_FW_UPDATE_OP_SETDEST:
                    {
                        AddResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_FW_UPDATE_STAT, message.SequenceNumber, (byte)FirmwareUpdateStatus.BIDIB_MSG_FW_UPDATE_STAT_DATA, message.MessageParameters[1]));
                        break;
                    }
                case FirmwareUpdateOperation.BIDIB_MSG_FW_UPDATE_OP_DATA:
                    {
                        AddResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_FW_UPDATE_STAT, message.SequenceNumber, (byte)FirmwareUpdateStatus.BIDIB_MSG_FW_UPDATE_STAT_DATA, 1));
                        break;
                    }
                case FirmwareUpdateOperation.BIDIB_MSG_FW_UPDATE_OP_DONE:
                    {
                        AddResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_FW_UPDATE_STAT, message.SequenceNumber, (byte)FirmwareUpdateStatus.BIDIB_MSG_FW_UPDATE_STAT_READY, 0));
                        break;
                    }
                case FirmwareUpdateOperation.BIDIB_MSG_FW_UPDATE_OP_EXIT:
                    {
                        AddResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_FW_UPDATE_STAT, message.SequenceNumber, (byte)FirmwareUpdateStatus.BIDIB_MSG_FW_UPDATE_STAT_EXIT, 0));
                        break;
                    }
            }
        }

        private void HandleAccessoryGet(BiDiBInputMessage message)
        {
            byte accessory = message.MessageParameters[0];
            byte aspect = AccessoryAspects.ContainsKey(accessory) ? AccessoryAspects[accessory] : (byte)255;
            List<byte> parameters = new List<byte> { accessory, aspect, (byte)StaticRandom.Instance.Next(1, 20), 0, 0 }; // max limit is 127

            OnAccessoryGet(accessory, parameters);

            AddResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_ACCESSORY_STATE, message.SequenceNumber, parameters.ToArray()));
        }

        protected virtual void OnAccessoryGet(byte accessory, ICollection<byte> parameters)
        { }

        private void HandleAccessoryPara(BiDiBInputMessage message)
        {
            byte accessory = message.MessageParameters[0];
            byte paraNum = message.MessageParameters[1];
            AddResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_ACCESSORY_PARA, message.SequenceNumber, accessory, paraNum, 0));
        }

        private void HandleAccessorySet(BiDiBInputMessage message)
        {
            byte accessory = message.MessageParameters[0];
            byte aspect = message.MessageParameters[1];

            List<byte> parameters = new List<byte> { accessory, aspect, Convert.ToByte(AccessoryAspects.Count), 0, 0 };

            OnAccessorySet(accessory, aspect, parameters);

            if(aspect == 3) {return;}

            AddResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_ACCESSORY_STATE, message.SequenceNumber, parameters.ToArray()));
        }

        protected virtual void OnAccessorySet(byte accessory, byte aspect, ICollection<byte> parameters)
        {
        }

        private async Task HandleVendorGet(BiDiBInputMessage message)
        {
            byte nameLength = message.MessageParameters[0];
            byte[] cvNameBytes = new byte[nameLength];
            Array.Copy(message.MessageParameters, 1, cvNameBytes, 0, nameLength);
            string cvName = cvNameBytes.GetStringValue();

            var parameters = new List<byte> { nameLength };
            parameters.AddRange(cvNameBytes);

            OnHandleVendorGet(cvName);

            parameters.AddRange(GetBytes(CvValues[cvName]));
            await Task.Delay(200);
            AddResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_VENDOR, message.SequenceNumber, parameters.ToArray()));
        }

        protected virtual void OnHandleVendorGet(string cvName)
        {
            var cvValue = CvValues.ContainsKey(cvName)
                ? CvValues[cvName]
                : StaticRandom.Instance.Next(1, 255).ToString(CultureInfo.CurrentCulture);

            CvValues[cvName] = cvValue;
        }

        private async Task HandleVendorSet(BiDiBInputMessage message)
        {
            byte nameLength = message.MessageParameters[0];
            byte[] cvNameBytes = new byte[nameLength];
            Array.Copy(message.MessageParameters, 1, cvNameBytes, 0, nameLength);
            string cvName = cvNameBytes.GetStringValue();

            byte valueLength = message.MessageParameters[nameLength + 1];
            byte[] cvValueBytes = new byte[valueLength];
            Array.Copy(message.MessageParameters, nameLength + 2, cvValueBytes, 0, valueLength);
            string cvValue = cvValueBytes.GetStringValue();

            OnHandleVendorSet(cvName, cvValue);

            if (cvName == "45")
            {
                // force timeout issue
                return;
            }

            await Task.Delay(200).ConfigureAwait(false);
            AddResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_VENDOR, message.SequenceNumber, message.MessageParameters));
        }

        protected virtual void OnHandleVendorSet(string cvName, string cvValue)
        {
            CvValues[cvName] = cvValue;
        }


        protected void ForwardToChildren(BiDiBInputMessage message, Action<byte[]> addResponse)
        {
            foreach (SimulationNode node in Children)
            {
                node.HandleMessage(message, addResponse);
            }
        }

        private static IEnumerable<byte> GetBytes(string value)
        {
            List<byte> parameters = new List<byte> { Convert.ToByte(value.Length) };
            parameters.AddRange(value.Select(Convert.ToByte));
            return parameters.ToArray();
        }

        private static byte[] GetBytesFromDotted(string value)
        {
            string[] parts = value.Split('.');
            byte[] bytes = parts.Select(x => Convert.ToByte(x, CultureInfo.CurrentCulture)).ToArray();
            Array.Reverse(bytes);
            return bytes;
        }

        private static byte[] GetNameBytes(string name)
        {
            List<byte> bytes = new List<byte>();
            if (!string.IsNullOrEmpty(name))
            {
                bytes.Add((byte)name.Length);
                bytes.AddRange(name.Select(Convert.ToByte));
            }
            else
            {
                bytes.Add(0);

            }
            return bytes.ToArray();
        }

        private void HandleValueChangeTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            LastValueChangedCycle++;
            if (!SysEnabled) { return; }
            OnTriggerValueChange();

            if (LastValueChangedCycle % 10 == 0)
            {
                OnTenthCycleValueChangeTrigger();
            }

            if (LastValueChangedCycle % 5 == 0)
            {
                OnFifthCycleValueChangeTrigger();
            }

            if (LastValueChangedCycle % 4 == 0)
            {
                OnFourthCycleValueChangeTrigger();
            }

            if (LastValueChangedCycle % 3 == 0)
            {
                OnThirdCycleValueChangeTrigger();
            }

            if (LastValueChangedCycle % 2 == 0)
            {
                OnSecondCycleValueChangeTrigger();
            }
        }

        protected virtual void OnTenthCycleValueChangeTrigger()
        {
            if (!(FeedbackPorts?.Length > 0)) { return; }

            int feedbackIndex = StaticRandom.Instance.Next(0, FeedbackPorts.Length - 1);
            FeedbackPort port = FeedbackPorts[feedbackIndex];
            port.IsFree = !port.IsFree;

            BiDiBMessage message = port.IsFree
                ? BiDiBMessage.MSG_BM_FREE
                : BiDiBMessage.MSG_BM_OCC;

            AddResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(Address, message, LastSequenceNumber++, (byte)port.Number));

            if (port.IsFree)
            {
                port.ClearOccupancies();
            }
            else
            {
                OccupancyInfo occInfo = new OccupancyInfo { Address = (ushort)StaticRandom.Instance.Next(1, 10) };
                port.AddOccupancy(occInfo);
                SendAddresses(port.Number, port.Occupancies.Select(x => x.Address));
            }
        }

        protected virtual void OnFifthCycleValueChangeTrigger()
        {
            if (!(FeedbackPorts?.Length > 0)) { return; }
            int feedbackIndex = StaticRandom.Instance.Next(0, FeedbackPorts.Length - 1);
            if (FeedbackPorts[feedbackIndex].IsFree) { return; }

            List<byte> parameters = new List<byte>
            {
                (byte) FeedbackPorts[feedbackIndex].Number,
                0x03,
                0x80,
                (byte) DynState.Distance
            };


            parameters.AddRange(BitConverter.GetBytes(Convert.ToInt16(StaticRandom.Instance.Next(0, short.MaxValue))));
            parameters.AddRange(BitConverter.GetBytes(Convert.ToInt16(StaticRandom.Instance.Next(0, short.MaxValue))));

            AddResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(Address, BiDiBMessage.MSG_BM_DYN_STATE, LastSequenceNumber++, parameters.ToArray()));
        }

        protected virtual void OnFourthCycleValueChangeTrigger()
        {
            if (!(FeedbackPorts?.Length > 0)) { return; }
            int feedbackIndex = StaticRandom.Instance.Next(0, FeedbackPorts.Length - 1);
            FeedbackPort port = FeedbackPorts[feedbackIndex];

            if (port.IsFree) { return; }

            var address = (ushort)StaticRandom.Instance.Next(1, 10);

            if (port.Occupancies.All(x => x.Address != address))
            {
                OccupancyInfo occInfo = new OccupancyInfo { Address = address };
                port.AddOccupancy(occInfo);

                SendAddresses(port.Number, port.Occupancies.Select(x => x.Address));
            }
            else if (port.Occupancies.Length > 2)
            {
                List<OccupancyInfo> occs = port.Occupancies.ToList();
                occs.RemoveAt(1);

                SendAddresses(port.Number, occs.Select(x => x.Address));
            }
        }

        protected virtual void OnThirdCycleValueChangeTrigger()
        {
        }

        protected virtual void OnSecondCycleValueChangeTrigger()
        {
        }

        protected virtual void OnTriggerValueChange()
        {
        }

        protected void SendAddresses(int portNumber, IEnumerable<ushort> addresses)
        {
            if (addresses == null) { return; }
            List<byte> parameters = new List<byte> { (byte)portNumber };
            foreach (var address in addresses)
            {
                byte[] adr = BitConverter.GetBytes(address);
                parameters.AddRange(adr);
            }

            AddResponse(BiDiBMessageGenerator.GenerateMessage(Address, BiDiBMessage.MSG_BM_ADDRESS, LastSequenceNumber++, parameters.ToArray()));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                valueChangeTimer?.Dispose();
            }
        }
    }
}