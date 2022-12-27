using System;
using System.Xml.Serialization;
using org.bidib.netbidibc.core.Models;

namespace org.bidib.nbidibc.Simulation.Models.Definition
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