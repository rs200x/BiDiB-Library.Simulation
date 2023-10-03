using System;
using System.Xml.Serialization;
using org.bidib.Net.Core.Models;

namespace org.bidib.Net.Simulation.Models.Definition
{
    [Serializable]
    [XmlType(TypeName = "LightPortType", Namespace = Namespaces.SimulationNamespaceUrl)]
    public class LightPort : Port
    {
        [XmlElement("port")]
        public LightPortParams[] Port { get; set; }
    }
}