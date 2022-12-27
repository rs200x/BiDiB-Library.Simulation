
using System;
using System.Collections.Generic;
using System.Linq;
using org.bidib.netbidibc.core;
using org.bidib.netbidibc.core.Message;
using org.bidib.netbidibc.core.Models.BiDiB;
using org.bidib.netbidibc.core.Utils;

namespace org.bidib.nbidibc.Simulation.Models.Nodes
{
    public class ReadyBoostNode : GbmBoostNode
    {
        private readonly List<OccupancyInfo> occupancies = new List<OccupancyInfo>();

        protected override void PrepareFeatures()
        {
            base.PrepareFeatures();

            AddOrUpdateFeature(BiDiBFeature.FEATURE_ACCESSORY_COUNT, 1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_ACCESSORY_SURVEILLED, 0);

            AddOrUpdateFeature(BiDiBFeature.FEATURE_BST_TURNOFF_TIME, 8);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BST_CURMEAS_INTERVAL, 200);

            AddOrUpdateFeature(BiDiBFeature.FEATURE_BST_AMPERE, 186);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BST_AMPERE_ADJUSTABLE, 1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BST_INHIBIT_AUTOSTART, 0);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BST_CUTOUT_ON, 1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BST_CUTOUT_AVAIALABLE, 1);

            AddOrUpdateFeature(BiDiBFeature.FEATURE_BST_INHIBIT_LOCAL_ONOFF, 0);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BST_TURNOFF_TIME, 30);

            AddOrUpdateFeature(BiDiBFeature.FEATURE_GEN_WATCHDOG, 20);

            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_SIZE, 0);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_CV_AVAILABLE, 2);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_CV_ON, 2);

            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_ADDR_DETECT_AVAILABLE, 2);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_ADDR_DETECT_ON, 2);
        }

        private static ushort GetNextDecoderAddress()
        {
            return (ushort)StaticRandom.Instance.Next(1, 16);
        }

        protected override void OnTenthCycleValueChangeTrigger()
        {
            base.OnTenthCycleValueChangeTrigger();
            if (!IsBoosterOn) { return; }

            var decoderAddress = GetNextDecoderAddress();
            var occupancy = occupancies.FirstOrDefault(x => x.Address == decoderAddress);
            if (occupancy == null)
            {
                OccupancyInfo occInfo = new OccupancyInfo { Address = decoderAddress };
                occupancies.Add(occInfo);
            }
            else
            {
                occupancies.Remove(occupancy);
            }

            SendAddresses(255, occupancies.Select(x => x.Address));
        }

        protected override void OnFifthCycleValueChangeTrigger()
        {
            base.OnFifthCycleValueChangeTrigger();
            if (!IsBoosterOn) { return; }

            var decoderAddress = GetNextDecoderAddress();
            List<byte> parameters = new List<byte> { 255 };
            parameters.AddRange(BitConverter.GetBytes(decoderAddress));
            parameters.Add((byte)StaticRandom.Instance.Next(1, 5));
            parameters.Add((byte)StaticRandom.Instance.Next(byte.MinValue, byte.MaxValue));

            AddResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(Address, BiDiBMessage.MSG_BM_DYN_STATE, LastSequenceNumber++, parameters.ToArray()));
        }

        protected override void OnSecondCycleValueChangeTrigger()
        {
            base.OnSecondCycleValueChangeTrigger();
            if (!IsBoosterOn) { return; }

            var decoderAddress = GetNextDecoderAddress();
            List<byte> parameters = new List<byte>();
            parameters.AddRange(BitConverter.GetBytes(decoderAddress));
            parameters.AddRange(BitConverter.GetBytes(StaticRandom.Instance.Next(0, 300)));
            AddResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(Address, BiDiBMessage.MSG_BM_SPEED, LastSequenceNumber++, parameters.ToArray()));
        }
    }
}