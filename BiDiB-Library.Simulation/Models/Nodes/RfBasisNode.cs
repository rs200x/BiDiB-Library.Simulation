using System;
using System.Collections.Generic;
using org.bidib.Net.Core;
using org.bidib.Net.Core.Message;
using org.bidib.Net.Core.Utils;

namespace org.bidib.Net.Simulation.Models.Nodes
{
    public class RfBasisNode : SimulationNode
    {
        protected override void PrepareFeatures()
        {
            base.PrepareFeatures();

            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_SIZE,48);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_ON,1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_SECACK_AVAILABLE,1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_SECACK_ON,20);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_ADDR_DETECT_AVAILABLE,1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_ADDR_DETECT_ON,1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_ADDR_AND_DIR,1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_ISTSPEED_AVAILABLE,1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_ISTSPEED_INTERVAL,10);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_CV_AVAILABLE,1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_CV_ON,1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_POSITION_ON,1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_POSITION_SECACK,20);
        }

        protected override void OnTriggerValueChange()
        {
            base.OnTriggerValueChange();

            if (!SysEnabled) { return; }

            // every fourth round
            if (LastValueChangedCycle % 4 == 0)
            {
                var decoderId = (ushort)StaticRandom.Instance.Next(1, 30);
                var position = (ushort) StaticRandom.Instance.Next(1, 512) * 125;
                var parameters = new List<byte>();
                parameters.AddRange(BitConverter.GetBytes(decoderId));
                parameters.Add(1);
                parameters.AddRange(BitConverter.GetBytes(position));
                AddResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(Address, BiDiBMessage.MSG_BM_POSITION, LastSequenceNumber++, parameters.ToArray()));
            }
        }
    }
}