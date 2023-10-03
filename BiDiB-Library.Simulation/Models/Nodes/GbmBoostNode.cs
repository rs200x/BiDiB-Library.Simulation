using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using org.bidib.Net.Core;
using org.bidib.Net.Core.Enumerations;
using org.bidib.Net.Core.Message;
using org.bidib.Net.Core.Models.Messages.Input;
using org.bidib.Net.Core.Utils;

namespace org.bidib.Net.Simulation.Models.Nodes
{
    public class GbmBoostNode : SimulationNode
    {
        private readonly Queue<BoosterState> boosterStates = new();

        public GbmBoostNode()
        {
            foreach (var state in Enum.GetValues(typeof(BoosterState)))
            {
                boosterStates.Enqueue((BoosterState) state);
            }
        }

        internal BoosterState BoosterState { get; private set; }

        internal bool IsBoosterOn => BoosterState >= BoosterState.BIDIB_BST_STATE_ON;

        protected override void PrepareFeatures()
        {
            base.PrepareFeatures();

            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_SIZE, 48);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_ON, 1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_SECACK_AVAILABLE, 1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_SECACK_ON, 20);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_ADDR_DETECT_AVAILABLE, 1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_ADDR_DETECT_ON, 1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_ADDR_AND_DIR, 1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_ISTSPEED_AVAILABLE, 1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_ISTSPEED_INTERVAL, 10);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_CV_AVAILABLE, 1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_CV_ON, 1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BST_CUTOUT_AVAIALABLE, 1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BST_CUTOUT_ON, 1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BST_TURNOFF_TIME, 8);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BST_INRUSH_TURNOFF_TIME, 30);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BST_AMPERE_ADJUSTABLE, 0);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BST_AMPERE, 155);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BST_CURMEAS_INTERVAL, 200);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BST_INHIBIT_AUTOSTART, 0);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BST_INHIBIT_LOCAL_ONOFF, 0);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_DYN_STATE_INTERVAL, 5);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_RCPLUS_AVAILABLE, 1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_TIMESTAMP_ON, 1);
        }

        protected override void OnHandleMessage(BiDiBInputMessage message, Action<byte[]> addResponse)
        {
            base.OnHandleMessage(message, addResponse);

            if (message == null || addResponse == null)
            {
                return;
            }
            
            switch (message.MessageType)
            {
                case BiDiBMessage.MSG_BOOST_ON:
                {
                    BoosterState = BoosterState.BIDIB_BST_STATE_ON;
                    addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(Address, BiDiBMessage.MSG_BOOST_STAT,
                        message.SequenceNumber, (byte) BoosterState.BIDIB_BST_STATE_ON));
                    if (message.MessageParameters[0] == 0)
                    {
                        ForwardToChildren(message, addResponse);
                    }

                    break;
                }

                case BiDiBMessage.MSG_BOOST_OFF:
                {
                    BoosterState = BoosterState.BIDIB_BST_STATE_OFF;
                    addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(Address, BiDiBMessage.MSG_BOOST_STAT,
                        message.SequenceNumber, (byte) BoosterState.BIDIB_BST_STATE_OFF));
                    if (message.MessageParameters[0] == 0)
                    {
                        ForwardToChildren(message, addResponse);
                    }

                    break;
                }

                case BiDiBMessage.MSG_BOOST_QUERY:
                {
                    addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(Address, BiDiBMessage.MSG_BOOST_STAT,
                        message.SequenceNumber, (byte) BoosterState.BIDIB_BST_STATE_OFF));
                    break;
                }
                case BiDiBMessage.MSG_CS_PROG:
                {
                    HandleCommandStationProgrammingMessageAsync(message).ConfigureAwait(false);
                    break;
                }
                //case BiDiBMessage.MSG_CS_POM:
                //    {
                //        HandleCommandStationProgrammingOnMainMessageAsync(message).ConfigureAwait(false);
                //        break;
                //    }
            }
        }

        private async Task HandleCommandStationProgrammingOnMainMessageAsync(BiDiBInputMessage message)
        {
            var parameters = new List<byte>(message.MessageParameters.Take(5)) { 1 };
            AddResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(Address, BiDiBMessage.MSG_CS_POM_ACK,
                message.SequenceNumber, parameters.ToArray()));

            await Task.Delay(500).ConfigureAwait(false);
            parameters = new List<byte>(message.MessageParameters.Take(2))
            {
                message.MessageParameters[6],
                message.MessageParameters[7],
                Convert.ToByte(StaticRandom.Instance.Next(0, 255))
            };
            AddResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(Address, BiDiBMessage.MSG_BM_CV,
                message.SequenceNumber, parameters.ToArray()));
        }

        private async Task HandleCommandStationProgrammingMessageAsync(BiDiBInputMessage message)
        {
            var parameters = new byte[]
            {
                (byte) CommandStationProgState.BIDIB_CS_PROG_OKAY,
                0,
                message.MessageParameters[1],
                message.MessageParameters[2],
                Convert.ToByte(StaticRandom.Instance.Next(0, 255))
            };
            await Task.Delay(500).ConfigureAwait(false);
            AddResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(Address, BiDiBMessage.MSG_CS_PROG_STATE,
                message.SequenceNumber, parameters));
        }

        protected override void OnTriggerValueChange()
        {
            base.OnTriggerValueChange();

            if (!SysEnabled)
            {
                return;
            }

            var current = Convert.ToByte(StaticRandom.Instance.Next(0,
                Features.FirstOrDefault(x => x.FeatureType == BiDiBFeature.FEATURE_BST_AMPERE)?.Value ?? 250));
            var voltage = Convert.ToByte(StaticRandom.Instance.Next(0, 253));
            var temp = Convert.ToByte(StaticRandom.Instance.Next(0, 127));

            AddResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(Address, BiDiBMessage.MSG_BOOST_DIAGNOSTIC,
                LastSequenceNumber++, 0x00, current, 0x01, voltage, 0x02, temp));
        }

        protected override void OnFifthCycleValueChangeTrigger()
        {
            base.OnFifthCycleValueChangeTrigger();

            if (!(FeedbackPorts?.Length > 0))
            {
                return;
            }

            var feedbackIndex = StaticRandom.Instance.Next(0, FeedbackPorts.Length - 1);
            if (FeedbackPorts[feedbackIndex].IsFree)
            {
                return;
            }

            SendTrackVoltage(feedbackIndex);
            SendSignalQuality(feedbackIndex);
        }

        private void SendTrackVoltage(int feedbackIndex)
        {
            var parameters = new List<byte>
            {
                (byte) FeedbackPorts[feedbackIndex].Number,
            };

            parameters.AddRange(BitConverter.GetBytes(Convert.ToInt16(StaticRandom.Instance.Next(0, 10))));
            parameters.Add((byte) DynState.TrackVoltage);
            parameters.Add((byte) StaticRandom.Instance.Next(100, 200));

            AddResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(Address, BiDiBMessage.MSG_BM_DYN_STATE,
                LastSequenceNumber++, parameters.ToArray()));
        }

        private void SendSignalQuality(int feedbackIndex)
        {
            var parameters = new List<byte>
            {
                (byte) FeedbackPorts[feedbackIndex].Number,
            };

            parameters.AddRange(BitConverter.GetBytes(Convert.ToInt16(StaticRandom.Instance.Next(0, 10))));
            parameters.Add((byte) DynState.SignalQuality);
            parameters.Add((byte) StaticRandom.Instance.Next(0, 100));

            AddResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(Address, BiDiBMessage.MSG_BM_DYN_STATE,
                LastSequenceNumber++, parameters.ToArray()));
        }
    }
}