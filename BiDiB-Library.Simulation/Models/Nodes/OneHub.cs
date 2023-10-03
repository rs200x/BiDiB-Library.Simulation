using System;
using org.bidib.Net.Core;
using org.bidib.Net.Core.Models.Messages.Input;

namespace org.bidib.Net.Simulation.Models.Nodes
{
    public class OneHub : SimulationNode
    {
        protected override void OnHandleMessage(BiDiBInputMessage message, Action<byte[]> addResponse)
        {
            base.OnHandleMessage(message, addResponse);

            if (message == null || addResponse == null)
            {
                return;
            }
            
            switch (message.MessageType)
            {
                case BiDiBMessage.MSG_SYS_ENABLE:
                case BiDiBMessage.MSG_SYS_DISABLE:
                {
                    ForwardToChildren(message, addResponse);
                    break;
                }

                case BiDiBMessage.MSG_BOOST_ON:
                case BiDiBMessage.MSG_BOOST_OFF:
                {
                    if (message.MessageParameters[0] == 0)
                    {
                        ForwardToChildren(message, addResponse);
                    }
                    break;
                }
            }
        }
    }
}