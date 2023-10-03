using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using org.bidib.Net.Core;
using org.bidib.Net.Core.Controllers;
using org.bidib.Net.Core.Controllers.Interfaces;
using org.bidib.Net.Core.Enumerations;
using org.bidib.Net.Core.Message;
using org.bidib.Net.Core.Models;
using org.bidib.Net.Core.Services.Interfaces;
using org.bidib.Net.Core.Utils;
using org.bidib.Net.Simulation.Services;

namespace org.bidib.Net.Simulation.Controllers
{
    public sealed class SimulationController : ConnectionController<ISimulationConfig>, ISimulationController
    {
        private  readonly ILogger<SimulationController> logger;
        private  readonly ILogger rawLogger;

        private readonly IBiDiBInterfaceSimulator interfaceSimulator;
        private InterfaceConnectionState connectionState = InterfaceConnectionState.Disconnected;
        private string simulationFilePath;

        public SimulationController(
            IXmlService xmlService, 
            IBiDiBMessageExtractor messageExtractor, 
            ISimulationNodeFactory simulationNodeFactory, 
            ILoggerFactory loggerFactory)
        {
            interfaceSimulator = new BiDiBInterfaceSimulator(
                xmlService, 
                messageExtractor, 
                simulationNodeFactory, 
                loggerFactory.CreateLogger<BiDiBInterfaceSimulator>())
            {
                DataReceived = HandleDataReceived
            };

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }
            
            logger = loggerFactory.CreateLogger<SimulationController>();
            rawLogger = loggerFactory.CreateLogger(BiDiBConstants.LoggerContextRaw);
        }

        public override ConnectionStateInfo ConnectionState => new(connectionState, InterfaceConnectionType.SerialSimulation);
    
        public override string ConnectionName => interfaceSimulator.SimulationFilePath;

        public override void Initialize(ISimulationConfig config)
        {
            if (config == null)
            {
                return;
            }

            simulationFilePath = config.SimulationFilePath;
        }

        public override bool SendMessage(byte[] messageBytes, int byteCount)
        {
            var realMessage = new byte[byteCount];
            Array.Copy(messageBytes, 0, realMessage, 0, byteCount);
            rawLogger.LogInformation(">>> {Data}",realMessage.GetDataString());
            interfaceSimulator.ProcessMessage(realMessage);
            return true;
        }

        public override Task<ConnectionStateInfo> OpenConnectionAsync()
        {
            return Task.Factory.StartNew(() =>
            {
                logger.LogDebug("Connection open requested, starting simulator");
                interfaceSimulator.Load(simulationFilePath);
                interfaceSimulator.Start();
                connectionState = InterfaceConnectionState.FullyConnected;
                return new ConnectionStateInfo(connectionState, InterfaceConnectionType.SerialSimulation);
            });
        }

        public override void Close()
        {
            logger.LogDebug("Connection close requested, stopping simulator");
            interfaceSimulator.Stop();
            connectionState = InterfaceConnectionState.Disconnected;
        }

        private void HandleDataReceived(byte[] messageBytes)
        {
            rawLogger.LogInformation("<<< {Data}",messageBytes.GetDataString());
            ProcessReceivedData?.Invoke(messageBytes);
        }

        public void Dispose()
        {
            interfaceSimulator.Dispose();
        }
    }
}