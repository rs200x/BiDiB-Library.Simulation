using System;
using System.Xml.Serialization;
using org.bidib.netbidibc.core.Models;

namespace org.bidib.nbidibc.Simulation.Models.Definition
{
    [Serializable]
    [XmlType(TypeName = "LightPortType", Namespace = Namespaces.SimulationNamespaceUrl)]
    public class LightPort : Port
    {
        [XmlElement("port")]
        public LightPortParams[] Port { get; set; }
    }
}