using System;
using System.Xml.Serialization;
using org.bidib.netbidibc.core.Models;

namespace org.bidib.nbidibc.Simulation.Models.Definition
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