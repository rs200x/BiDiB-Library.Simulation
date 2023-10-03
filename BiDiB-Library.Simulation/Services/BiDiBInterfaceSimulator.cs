using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using org.bidib.Net.Core.Message;
using org.bidib.Net.Core.Models;
using org.bidib.Net.Core.Models.BiDiB.Extensions;
using org.bidib.Net.Core.Models.Xml;
using org.bidib.Net.Core.Services.Interfaces;
using org.bidib.Net.Core.Utils;
using org.bidib.Net.Simulation.Models.Definition;
using org.bidib.Net.Simulation.Models.Nodes;
using Node = org.bidib.Net.Simulation.Models.Definition.Node;

namespace org.bidib.Net.Simulation.Services
{
    public sealed class BiDiBInterfaceSimulator : IBiDiBInterfaceSimulator
    {
        private readonly IXmlService xmlService;
        private readonly ILogger<BiDiBInterfaceSimulator> logger;
        private readonly BlockingCollection<byte[]> outputMessageQueue;
        private readonly BlockingCollection<byte[]> inputMessageQueue;
        private bool isActive;
        private readonly IBiDiBMessageExtractor messageExtractor;
        private readonly ISimulationNodeFactory simulationNodeFactory;
        private readonly bool useMessageSecurity;
        private readonly Dictionary<int, SimulationNode> nodes;
        private readonly string defaultSimulationFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".bidib", "data", "simulation", "simulation.xml");
        private readonly string simulationSchemaFilePath = AppDomain.CurrentDomain.BaseDirectory + "data\\Schema\\simulation.xsd";

        public BiDiBInterfaceSimulator(
            IXmlService xmlService, 
            IBiDiBMessageExtractor messageExtractor, 
            ISimulationNodeFactory simulationNodeFactory,
            ILogger<BiDiBInterfaceSimulator> logger)
            : this(xmlService, messageExtractor, simulationNodeFactory, true, logger)
        {
        }

        public BiDiBInterfaceSimulator(
            IXmlService xmlService, 
            IBiDiBMessageExtractor messageExtractor,
            ISimulationNodeFactory simulationNodeFactory,
            bool useMessageSecurity, 
            ILogger<BiDiBInterfaceSimulator> logger)
        {
            this.xmlService = xmlService;
            this.messageExtractor = messageExtractor;
            this.simulationNodeFactory = simulationNodeFactory;
            this.useMessageSecurity = useMessageSecurity;
            this.logger = logger;

            inputMessageQueue = new BlockingCollection<byte[]>();
            outputMessageQueue = new BlockingCollection<byte[]>();
            nodes = new Dictionary<int, SimulationNode>();
        }

        public string SimulationFilePath { get; private set; }

        public void Load(string simulationFilePath)
        {
            SimulationFilePath = !string.IsNullOrEmpty(simulationFilePath) ? simulationFilePath : defaultSimulationFilePath;

            var result = xmlService.ValidateFile(SimulationFilePath, Namespaces.SimulationNamespaceUrl, simulationSchemaFilePath);
            if (result.Result == XmlValidationResult.Valid)
            {
                LoadSimulationDefinition();
            }
            else
            {
                logger.LogWarning($"could not read simulation file '{SimulationFilePath}' -> {result.Result}! Using basics");
                SimulationFilePath = "Fallback simulation";
                CreateSampleSimulationNodes();
            }
        }

        private void LoadSimulationDefinition()
        {
            var simulation = xmlService.LoadFromFile<SimulationDefinition>(SimulationFilePath);
            var master = simulationNodeFactory.Create(simulation.Master, EnqueueResponse);
            nodes.Add(master.GetAddress(), master);
            AddSubNodes(master, simulation.Master.Nodes);
        }

        private void AddSubNodes(SimulationNode parent, IEnumerable<Node> subNodes)
        {
            foreach (var subNode in subNodes)
            {
                var node = simulationNodeFactory.Create(subNode, EnqueueResponse);
                if (parent.GetAddress() != 0)
                {
                    var extendedAddress = new byte[parent.Address.Length + 1];
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
            var master = new SimulationNode { Address = new byte[] { 0 }, UniqueId = 40_532_454_695_511_040, ProtocolVersion = "0.6", SoftwareVersion = "2.04.03" };
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

            foreach (var simNode in simNodes)
            {
                simNode.Initialize(EnqueueResponse);
                nodes.Add(simNode.GetAddress(), simNode);
                master.Children.Add(simNode);
                foreach (var child in simNode.Children)
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
                if (!inputMessageQueue.TryTake(out var message, 100))
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
                if (!outputMessageQueue.TryTake(out var message, 100))
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
                queue.TryTake(out var _, 10);
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
            var messages = messageExtractor.ExtractMessage(message, useMessageSecurity);

            foreach (var inputMessage in messages)
            {
                if (!nodes.ContainsKey(inputMessage.Address.GetArrayValue()))
                {
                    continue;
                }

                var node = nodes[inputMessage.Address.GetArrayValue()];
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