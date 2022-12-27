using System;
using System.Xml.Serialization;
using org.bidib.netbidibc.core.Models;

namespace org.bidib.nbidibc.Simulation.Models.Definition
{
    [Serializable]
    [XmlType(TypeName = "BacklightPortType", Namespace = Namespaces.SimulationNamespaceUrl)]
    public class BacklightPort : Port
    {
        [XmlElement("port")]
        public BacklightPortParams[] Port { get; set; }
    }
}