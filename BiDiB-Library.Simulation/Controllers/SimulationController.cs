using System;
using System.Threading.Tasks;
using log4net;
using org.bidib.nbidibc.simulation.Services;
using org.bidib.netbidibc.core.Controllers;
using org.bidib.netbidibc.core.Controllers.Interfaces;
using org.bidib.netbidibc.core.Enumerations;
using org.bidib.netbidibc.core.Message;
using org.bidib.netbidibc.core.Models;
using org.bidib.netbidibc.core.Services.Interfaces;
using org.bidib.netbidibc.core.Utils;

namespace org.bidib.nbidibc.simulation.Controllers
{
    public sealed class SimulationController : ConnectionController<ISimulationConfig>, ISimulationController
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SimulationController));
        private static readonly ILog RawLogger = LogManager.GetLogger("RAW");

        private readonly IBiDiBInterfaceSimulator interfaceSimulator;
        private InterfaceConnectionState connectionState = InterfaceConnectionState.Disconnected;
        private string simulationFilePath;

        public SimulationController(IXmlService xmlService, IBiDiBMessageExtractor messageExtractor)
        {
            interfaceSimulator = new BiDiBInterfaceSimulator(xmlService, messageExtractor) { DataReceived = HandleDataReceived };
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
            byte[] realMessage = new byte[byteCount];
            Array.Copy(messageBytes, 0, realMessage, 0, byteCount);
            RawLogger.Info($">>> {realMessage.GetDataString()}");
            interfaceSimulator.ProcessMessage(realMessage);
            return true;
        }

        public override Task<ConnectionStateInfo> OpenConnectionAsync()
        {
            return Task.Factory.StartNew(() =>
            {
                Logger.Debug("Connection open requested, starting simulator");
                interfaceSimulator.Load(simulationFilePath);
                interfaceSimulator.Start();
                connectionState = InterfaceConnectionState.FullyConnected;
                return new ConnectionStateInfo(connectionState, InterfaceConnectionType.SerialSimulation);
            });
        }

        public override void Close()
        {
            Logger.Debug("Connection close requested, stopping simulator");
            interfaceSimulator.Stop();
            connectionState = InterfaceConnectionState.Disconnected;
        }

        private void HandleDataReceived(byte[] messageBytes)
        {
            RawLogger.Info($"<<< {messageBytes.GetDataString()}");
            ProcessReceivedData?.Invoke(messageBytes);
        }

        public void Dispose()
        {
            interfaceSimulator.Dispose();
        }
    }
}