using System.ComponentModel.Composition;
using org.bidib.netbidibc.core.Controllers.Interfaces;
using org.bidib.netbidibc.core.Enumerations;
using org.bidib.netbidibc.core.Message;
using org.bidib.netbidibc.core.Services.Interfaces;

namespace org.bidib.nbidibc.simulation.Controllers
{
    [Export(typeof(IConnectionControllerFactory))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ConnectionControllerFactory : IConnectionControllerFactory
    {
        private readonly IXmlService xmlService;
        private readonly IBiDiBMessageExtractor messageExtractor;

        [ImportingConstructor]
        public ConnectionControllerFactory(IXmlService xmlService, IBiDiBMessageExtractor messageExtractor)
        {
            this.xmlService = xmlService;
            this.messageExtractor = messageExtractor;
        }

        /// <inheritdoc />
        public InterfaceConnectionType ConnectionType => InterfaceConnectionType.SerialSimulation;

        /// <inheritdoc />
        public IConnectionController GetController(IConnectionConfig connectionConfig)
        {
            var controller = new SimulationController(xmlService, messageExtractor);
            controller.Initialize(connectionConfig as ISimulationConfig);

            return controller;
        }
    }
}