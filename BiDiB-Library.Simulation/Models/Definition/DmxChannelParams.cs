using System;
using System.Xml.Serialization;
using org.bidib.Net.Core.Models;

namespace org.bidib.Net.Simulation.Models.Definition
{
    [Serializable]
    [XmlType(TypeName= "DmxChannelParamsType",Namespace = Namespaces.SimulationNamespaceUrl)]
    public class DmxChannelParams
    {
        [XmlAttribute("channelId")]
        public int ChannelId { get; set; }

        [XmlAttribute("initialState")]
        public int InitialState { get; set; }
    }
}