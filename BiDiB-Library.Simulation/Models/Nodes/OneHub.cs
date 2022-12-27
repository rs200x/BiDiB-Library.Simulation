using System;
using org.bidib.netbidibc.core;
using org.bidib.netbidibc.core.Models.Messages.Input;

namespace org.bidib.nbidibc.Simulation.Models.Nodes
{
    public class OneHub : SimulationNode
    {
        protected override void OnHandleMessage(BiDiBInputMessage message, Action<byte[]> addResponse)
        {
            base.OnHandleMessage(message, addResponse);

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