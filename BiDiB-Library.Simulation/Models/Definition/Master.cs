using System;
using System.Xml.Serialization;
using org.bidib.Net.Core.Models;

namespace org.bidib.Net.Simulation.Models.Definition
{
    [Serializable]
    [XmlType(TypeName = "MasterType", Namespace = Namespaces.SimulationNamespaceUrl)]
    public class Master : Node
    {
        [XmlArrayItem("node", IsNullable = false)]
        [XmlArray("subNodes")]
        public Node[] Nodes { get; set; }
    }
}