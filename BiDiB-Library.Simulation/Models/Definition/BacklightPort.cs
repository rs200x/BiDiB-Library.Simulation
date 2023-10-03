using System;
using System.Xml.Serialization;
using org.bidib.Net.Core.Models;

namespace org.bidib.Net.Simulation.Models.Definition
{
    [Serializable]
    [XmlType(TypeName = "BacklightPortType", Namespace = Namespaces.SimulationNamespaceUrl)]
    public class BacklightPort : Port
    {
        [XmlElement("port")]
        public BacklightPortParams[] Port { get; set; }
    }
}