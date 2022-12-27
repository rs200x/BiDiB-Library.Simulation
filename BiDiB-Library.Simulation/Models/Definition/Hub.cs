using System;
using System.Xml.Serialization;
using org.bidib.netbidibc.core.Models;

namespace org.bidib.nbidibc.Simulation.Models.Definition
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