using System;
using System.Xml.Serialization;
using org.bidib.Net.Core.Models;

namespace org.bidib.Net.Simulation.Models.Definition
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