using System;
using org.bidib.Net.Core;
using org.bidib.Net.Core.Message;
using org.bidib.Net.Core.Models.Messages.Input;

namespace org.bidib.Net.Simulation.Models.Nodes;

public class BootLoaderNode : SimulationNode
{
    protected override void OnHandleMessage(BiDiBInputMessage message, Action<byte[]> addResponse)
    {
        if (message == null || addResponse == null)
        {
            return;
        }
        
        switch (message.MessageType)
        {
            case BiDiBMessage.MSG_SYS_GET_MAGIC:
            {
                addResponse.Invoke(BiDiBMessageGenerator.GenerateMessage(message.Address, BiDiBMessage.MSG_SYS_MAGIC, message.SequenceNumber, 0x0D, 0xB0));
                return;
            }
        }
        
        base.OnHandleMessage(message, addResponse);
    }
}