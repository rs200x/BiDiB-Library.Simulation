using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using log4net;
using org.bidib.netbidibc.core.Message;
using org.bidib.netbidibc.core.Models;
using org.bidib.netbidibc.core.Models.BiDiB.Extensions;
using org.bidib.netbidibc.core.Models.Messages.Input;
using org.bidib.nbidibc.Simulation.Models.Definition;
using org.bidib.nbidibc.Simulation.Models.Nodes;
using org.bidib.netbidibc.core.Models.Xml;
using org.bidib.netbidibc.core.Services.Interfaces;
using org.bidib.netbidibc.core.Utils;
using Node = org.bidib.nbidibc.Simulation.Models.Definition.Node;

namespace org.bidib.nbidibc.simulation.Services
{
    public sealed class BiDiBInterfaceSimulator : IBiDiBInterfaceSimulator
    {
        private readonly IXmlService xmlService;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BiDiBInterfaceSimulator));
        private readonly BlockingCollection<byte[]> outputMessageQueue;
        private readonly BlockingCollection<byte[]> inputMessageQueue;
        private bool isActive;
        private readonly IBiDiBMessageExtractor messageExtractor;
        private readonly bool useMessageSecurity;
        private readonly Dictionary<int, SimulationNode> nodes;
        private readonly string defaultSimulationFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".bidib", "data", "simulation", "simulation.xml");
        private readonly string simulationSchemaFilePath = AppDomain.CurrentDomain.BaseDirectory + "data\\Schema\\simulation.xsd";

        public BiDiBInterfaceSimulator(IXmlService xmlService, IBiDiBMessageExtractor messageExtractor)
            : this(xmlService, messageExtractor, true)
        {
        }

        public BiDiBInterfaceSimulator(IXmlService xmlService, IBiDiBMessageExtractor messageExtractor, bool useMessageSecurity)
        {
            this.xmlService = xmlService;
            this.messageExtractor = messageExtractor;
            this.useMessageSecurity = useMessageSecurity;

            inputMessageQueue = new BlockingCollection<byte[]>();
            outputMessageQueue = new BlockingCollection<byte[]>();
            nodes = new Dictionary<int, SimulationNode>();
        }

        public string SimulationFilePath { get; private set; }

        public void Load(string simulationFilePath)
        {
            SimulationFilePath = !string.IsNullOrEmpty(simulationFilePath) ? simulationFilePath : defaultSimulationFilePath;

            IXmlValidationInfo result = xmlService.ValidateFile(SimulationFilePath, Namespaces.SimulationNamespaceUrl, simulationSchemaFilePath);
            if (result.Result == XmlValidationResult.Valid)
            {
                LoadSimulationDefinition();
            }
            else
            {
                Logger.Warn($"could not read simulation file '{SimulationFilePath}' -> {result.Result}! Using basics");
                SimulationFilePath = "Fallback simulation";
                CreateSampleSimulationNodes();
            }
        }

        private void LoadSimulationDefinition()
        {
            SimulationDefinition simulation = xmlService.LoadFromFile<SimulationDefinition>(SimulationFilePath);
            SimulationNode master = SimulationNodeFactory.Create(simulation.Master, EnqueueResponse);
            nodes.Add(master.GetAddress(), master);
            AddSubNodes(master, simulation.Master.Nodes);
        }

        private void AddSubNodes(SimulationNode parent, IEnumerable<Node> subNodes)
        {
            foreach (Node subNode in subNodes)
            {
                SimulationNode node = SimulationNodeFactory.Create(subNode, EnqueueResponse);
                if (parent.GetAddress() != 0)
                {
                    byte[] extendedAddress = new byte[parent.Address.Length + 1];
                    Array.Copy(parent.Address, 0, extendedAddress, 0, parent.Address.Length);
                    extendedAddress[parent.Address.Length] = node.Address[0];
                    node.Address = extendedAddress;
                }
                nodes.Add(node.GetAddress(), node);
                parent.Children.Add(node);

                if (subNode is Hub hub)
                {
                    AddSubNodes(node, hub.Nodes);
                }
            }
        }

        private void CreateSampleSimulationNodes()
        {
            SimulationNode master = new SimulationNode { Address = new byte[] { 0 }, UniqueId = 40_532_454_695_511_040, ProtocolVersion = "0.6", SoftwareVersion = "2.04.03" };
            master.Initialize(EnqueueResponse);
            nodes.Add(master.GetAddress(), master);
            SimulationNode[] simNodes =
            {
                new StepControl {Address = new byte[] {1}, UniqueId = 19_421_831_240_889_362, ProtocolVersion = "0.7", SoftwareVersion = "0.00.04"},
                new()
                {
                    Address = new byte[] {2}, UniqueId = 36_310_329_742_879_466, ProtocolVersion = "0.6", SoftwareVersion = "1.00.00", Children =
                    {
                        new SimulationNode {Address = new byte[] {2, 1}, UniqueId = 1_407_431_181_033_870, ProtocolVersion = "0.6", SoftwareVersion = "1.01.00"},
                    }
                },
                new() {Address = new byte[] {3}, UniqueId = 1_407_422_681_033_871, ProtocolVersion = "0.6", SoftwareVersion = "1.00.00"}
            };

            foreach (SimulationNode simNode in simNodes)
            {
                simNode.Initialize(EnqueueResponse);
                nodes.Add(simNode.GetAddress(), simNode);
                master.Children.Add(simNode);
                foreach (SimulationNode child in simNode.Children)
                {
                    nodes.Add(child.GetAddress(), child);
                }
            }
        }

        public void Start()
        {
            isActive = true;
            Task.Factory.StartNew(ProcessInputQueue, TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(ProcessOutputQueue, TaskCreationOptions.LongRunning);
        }

        private void ProcessInputQueue()
        {
            while (isActive)
            {
                if (!inputMessageQueue.TryTake(out byte[] message, 100))
                {
                    continue;
                }

                DataReceived?.Invoke(message);
            }
        }

        private void ProcessOutputQueue()
        {
            while (isActive)
            {
                if (!outputMessageQueue.TryTake(out byte[] message, 100))
                {
                    continue;
                }

                ProcessOutputMessage(message);
            }
        }

        public void Stop()
        {
            isActive = false;
            foreach (var simulationNode in nodes.Values)
            {
                simulationNode.Stop();
            }
            ClearQueue(inputMessageQueue);
            ClearQueue(outputMessageQueue);
        }

        private static void ClearQueue(BlockingCollection<byte[]> queue)
        {
            while (queue.Count > 0)
            {
                queue.TryTake(out byte[] _, 10);
            }
        }

        public void ProcessMessage(byte[] messageBytes)
        {
            if(messageBytes == null)
            {
                throw new ArgumentNullException(nameof(messageBytes));
            }

            outputMessageQueue.Add(messageBytes);
        }

        private void ProcessOutputMessage(byte[] message)
        {
            IEnumerable<BiDiBInputMessage> messages = messageExtractor.ExtractMessage(message, useMessageSecurity);

            foreach (BiDiBInputMessage inputMessage in messages)
            {
                if (!nodes.ContainsKey(inputMessage.Address.GetArrayValue()))
                {
                    continue;
                }

                SimulationNode node = nodes[inputMessage.Address.GetArrayValue()];
                node.HandleMessage(inputMessage, EnqueueResponse);
            }
        }

        private void EnqueueResponse(byte[] responseBytes)
        {
            inputMessageQueue.Add(responseBytes);
        }

        public Action<byte[]> DataReceived { get; set; }

        public void Dispose()
        {
            outputMessageQueue?.Dispose();
            inputMessageQueue?.Dispose();
        }
    }
}