using System;
using System.Xml.Serialization;
using org.bidib.Net.Core.Models;

namespace org.bidib.Net.Simulation.Models.Definition
{
    [Serializable]
    [XmlType(TypeName = "HubType", Namespace = Namespaces.SimulationNamespaceUrl)]
    public class Hub : Node
    {
        [XmlArrayItem("node", IsNullable = false)]
        [XmlArray("subNodes")]
        public Node[] Nodes { get; set; }
    }
}