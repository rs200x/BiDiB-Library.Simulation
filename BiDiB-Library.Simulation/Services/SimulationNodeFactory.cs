using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.Extensions.Logging;
using org.bidib.Net.Core;
using org.bidib.Net.Core.Models.BiDiB;
using org.bidib.Net.Simulation.Models.Nodes;
using Node = org.bidib.Net.Simulation.Models.Definition.Node;

namespace org.bidib.Net.Simulation.Services
{
    [Export(typeof(ISimulationNodeFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class SimulationNodeFactory : ISimulationNodeFactory
    {
        private readonly ILogger<SimulationNodeFactory> logger;

        [ImportingConstructor]
        public SimulationNodeFactory(ILogger<SimulationNodeFactory> logger)
        {
            this.logger = logger;
        }

        public SimulationNode Create(Node node, Action<byte[]> addResponse)
        {
            if (node == null)
            {
                return null;
            }

            SimulationNode simNode;
            
            switch (node.ProductId)
            {
                case 110:
                case 111:
                case 112: 
                case 119: 
                case 125: 
                case 131: 
                case 147: { simNode = new BootLoaderNode(); break; }
                case 103: { simNode = new GbmBoostNode(); break; }
                case 104:
                case 132: { simNode = new GbmBoostMaster(); break; }
                case 114: { simNode = new OneHub(); break; }
                case 117 when node.ManufacturerId == 251: { simNode = new ReadyBoostNode(); break; }
                case 120: { simNode = new StepControl(); break; }
                case 124: { simNode = new S88TleNode(); break; }
                case 32770:
                case 302 when node.ManufacturerId == 251: { simNode = new RfBasisNode(); break; }
                default: { simNode = new SimulationNode(); break; }
            }

            simNode.Address = node.GetAddress();
            simNode.UniqueId = node.UniqueIdLong;
            simNode.ProtocolVersion = node.ProtocolVersion;
            simNode.SoftwareVersion = node.SoftwareVersion;
            simNode.ProductName = node.ProductName;
            simNode.UserName = node.UserName;

            simNode.Initialize(addResponse);

            SetFeatureValues(simNode, node);

            simNode.Start();
            return simNode;
        }

        private void SetFeatureValues(Net.Core.Models.BiDiB.Node simNode, Node node)
        {
            var features = simNode.Features?.ToList() ?? new List<Feature>();

            if (node.Features != null)
            {
                foreach (var featureDef in node.Features)
                {
                    if (!Enum.TryParse(featureDef.Type, out BiDiBFeature featureType))
                    {
                        logger.LogWarning("Feature {Type} is not known!", featureDef.Type);
                        continue;
                    }

                    var feature = features.Find(x => x.FeatureType == featureType);
                    if (feature == null)
                    {
                        feature = new Feature { FeatureId = (byte)featureType };
                        features.Add(feature);
                    }
                    feature.Value = Convert.ToByte(featureDef.Value);
                }
            }

            simNode.Features = features.ToArray();
        }
    }
}