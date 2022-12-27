using System;
using System.Xml.Serialization;
using org.bidib.netbidibc.core.Models;

namespace org.bidib.nbidibc.Simulation.Models.Definition
{
    [Serializable]
    [XmlType(TypeName = "DmxChannelsType", Namespace = Namespaces.SimulationNamespaceUrl)]
    public class DmxChannels
    {
        [XmlElement("channel")]
        public DmxChannelParams[] Channel { get; set; }

        [XmlAttribute("count")]
        public int Count { get; set; }
    }
}