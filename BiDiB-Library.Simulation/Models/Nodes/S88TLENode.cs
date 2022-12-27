using System;
using org.bidib.netbidibc.core;
using org.bidib.netbidibc.core.Message;
using org.bidib.netbidibc.core.Models.Messages.Input;

namespace org.bidib.nbidibc.Simulation.Models.Nodes
{
    public class S88TleNode : SimulationNode
    {
        protected override void PrepareFeatures()
        {
            base.PrepareFeatures();

            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_SIZE, 128);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_ON, 1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_SECACK_AVAILABLE, 1);
            AddOrUpdateFeature(BiDiBFeature.FEATURE_BM_SECACK_ON, 0);
        }

        protected override void OnHandleMessage(BiDiBInputMessage message, Action<byte[]> addResponse)
        {
            base.OnHandleMessage(message, addResponse);
            if (message == null) { return; }

            if (message.MessageType == BiDiBMessage.MSG_BM_GET_RANGE)
            {
                AddResponse(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_BM_MULTIPLE,
                    LastSequenceNumber++, 0, 128, 0, 0, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255));
            }
        }
    }
}