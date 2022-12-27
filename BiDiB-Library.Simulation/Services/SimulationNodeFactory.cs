using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using org.bidib.netbidibc.core;
using org.bidib.netbidibc.core.Models.BiDiB;
using org.bidib.nbidibc.Simulation.Models.Nodes;
using Node = org.bidib.nbidibc.Simulation.Models.Definition.Node;

namespace org.bidib.nbidibc.simulation.Services
{
    public static class SimulationNodeFactory
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SimulationNodeFactory));

        public static SimulationNode Create(Node node, Action<byte[]> addResponse)
        {
            if (node == null)
            {
                return null;
            }

            SimulationNode simNode;
            
            switch (node.ProductId)
            {
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

        private static void SetFeatureValues(netbidibc.core.Models.BiDiB.Node simNode, Node node)
        {
            var features = simNode.Features?.ToList() ?? new List<Feature>();

            if (node.Features != null)
            {
                foreach (var featureDef in node.Features)
                {
                    if (!Enum.TryParse(featureDef.Type, out BiDiBFeature featureType))
                    {
                        Logger.Warn($"Feature {featureDef.Type} is not known!");
                        continue;
                    }

                    var feature = features.FirstOrDefault(x => x.FeatureType == featureType);
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