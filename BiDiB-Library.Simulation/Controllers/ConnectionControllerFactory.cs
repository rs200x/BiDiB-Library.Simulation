using System.ComponentModel.Composition;
using Microsoft.Extensions.Logging;
using org.bidib.Net.Core.Controllers.Interfaces;
using org.bidib.Net.Core.Enumerations;
using org.bidib.Net.Core.Message;
using org.bidib.Net.Core.Services.Interfaces;
using org.bidib.Net.Simulation.Services;

namespace org.bidib.Net.Simulation.Controllers
{
    [Export(typeof(IConnectionControllerFactory))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class ConnectionControllerFactory : IConnectionControllerFactory
    {
        private readonly IXmlService xmlService;
        private readonly IBiDiBMessageExtractor messageExtractor;
        private readonly ISimulationNodeFactory simulationNodeFactory;
        private readonly ILoggerFactory loggerFactory;

        [ImportingConstructor]
        public ConnectionControllerFactory(IXmlService xmlService, IBiDiBMessageExtractor messageExtractor, ISimulationNodeFactory simulationNodeFactory, ILoggerFactory loggerFactory)
        {
            this.xmlService = xmlService;
            this.messageExtractor = messageExtractor;
            this.simulationNodeFactory = simulationNodeFactory;
            this.loggerFactory = loggerFactory;
        }

        /// <inheritdoc />
        public InterfaceConnectionType ConnectionType => InterfaceConnectionType.SerialSimulation;

        /// <inheritdoc />
        public IConnectionController GetController(IConnectionConfig connectionConfig)
        {
            var controller = new SimulationController(xmlService, messageExtractor, simulationNodeFactory, loggerFactory);
            controller.Initialize(connectionConfig as ISimulationConfig);

            return controller;
        }
    }
}