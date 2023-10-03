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
    public class GbmBoostMaster : GbmBoostNode
    {
        private readonly Queue<CommandStationState> dccStates = new();
        private readonly Dictionary<int, List<CvValue>> locoCvData = new();

        public GbmBoostMaster()
        {
            foreach (var state in Enum.GetValues(typeof(CommandStationState)))
            {
                dccStates.Enqueue((CommandStationState)state);
            }

            locoCvData.Add(3, new List<CvValue>());
            locoCvData[3].Add(new CvValue(1) { Value = 3 });
            locoCvData[3].Add(new CvValue(8) { Value = 145 });
            locoCvData[3].Add(new CvValue(17) { Value = 0 });
            locoCvData[3].Add(new CvValue(18) { Value = 0 });
            locoCvData[3].Add(new CvValue(29) { Value = 1 });
            locoCvData[3].Add(new CvValue(250) { Value = 166 });
            locoCvData.Add(4, new List<CvValue>
            {
                new(8) { Value = 97 },
                new(7) { Value = 115 },
                new(261) { Value = 218 }
            });
            locoCvData.Add(5, new List<CvValue> { new(8) { Value = 151 } });
            locoCvData.Add(6, new List<CvValue> {
                new(8) { Value = 151 },
                new(7) { Value = 255 },
                new(261) { Value = 71 },
                new(262) { Value = 0 },
                new(263) { Value = 0 },
                new(264) {Value = 2} });
            locoCvData.Add(128, new List<CvValue> {
                new(8) { Value = 151 },
                new(7) { Value = 255 },
                new(261) { Value = 179 },
                new(262) { Value = 0 },
                new(263) { Value = 0 },
                new(264) {Value = 2} });
            locoCvData.Add(75, new List<CvValue> {
                new(8) { Value = 151 },
                new(7) { Value = 255 },
                new(261) { Value = 104 },
                new(262) { Value = 0 },
                new(263) { Value = 0 },
                new(264) {Value = 2} });
        }

        protected override void PrepareFeatures()
        {
            base.PrepareFeatures();

            AddOrUpdateFeature(BiDiBFeature.FEATURE_GEN_WATCHDOG, 20);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_GEN_DRIVE_ACK, 1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_GEN_SWITCH_ACK, 1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_GEN_POM_REPEAT, 3);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_GEN_DRIVE_BUS, 1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_GEN_LOK_LOST_DETECT, 1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_GEN_NOTIFY_DRIVE_MANUAL, 3);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_GEN_START_STATE, 1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_GEN_EXT_AVAILABLE, 1);
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
                case BiDiBMessage.MSG_CS_POM:
                    {
                        byte[] ackParameters = new byte[6];
                        Array.Copy(message.MessageParameters, 0, ackParameters, 0, 5);
                        ackParameters[5] = 1;
                        addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_CS_POM_ACK, message.SequenceNumber, ackParameters));

                        byte[] bmCvParameters = new byte[5];
                        ushort address = ByteUtils.GetDecoderAddress(message.MessageParameters[0], message.MessageParameters[1]);

                        if (address == 20)
                        {
                            return; // simulate no response error
                        }

                        if (!locoCvData.ContainsKey(address))
                        {
                            locoCvData.Add(address, new List<CvValue>());
                        }

                        int cvNumber = BitConverter.ToInt16(new[] { message.MessageParameters[6], message.MessageParameters[7] }, 0) + 1;
                        if (locoCvData[address].All(x => x.Number != cvNumber))
                        {
                            locoCvData[address].Add(new CvValue(cvNumber) { Value = Convert.ToByte(StaticRandom.Instance.Next(255)) });
                        }

                        var optCode = (CommandStationProgPoMOpCode)message.MessageParameters[5];
                        if (optCode == CommandStationProgPoMOpCode.BIDIB_CS_POM_WR_BYTE)
                        {
                            byte value = message.MessageParameters[9];
                            locoCvData[address].First(x => x.Number == cvNumber).Value = value;
                        }

                        Array.Copy(message.MessageParameters, 0, bmCvParameters, 0, 2);
                        Array.Copy(message.MessageParameters, 6, bmCvParameters, 2, 2);
                        bmCvParameters[4] = locoCvData[address].First(x => x.Number == cvNumber).Value;

                        Task.Delay(50).Wait();
                        addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_BM_CV, message.SequenceNumber, bmCvParameters));
                        //Task.Delay(50).Wait();
                        //bmCvParameters[4] += 1;
                        //addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_BM_CV, message.SequenceNumber, bmCvParameters));

                        break;
                    }
            }
        }

        private sealed class CvValue
        {
            public CvValue(int number)
            {
                Number = number;
                Value = 0;
            }

            public int Number { get; }

            public byte Value { get; set; }
        }
    }


}