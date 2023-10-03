using System;
using System.Xml.Serialization;
using org.bidib.Net.Core.Models;

namespace org.bidib.Net.Simulation.Models.Definition
{
    [Serializable]
    [XmlType(TypeName= "FeatureType",Namespace = Namespaces.SimulationNamespaceUrl)]
    public class FeatureType
    {
        [XmlAttribute("type")]
        public string Type { get; set; }

        [XmlAttribute("value")]
        public int Value { get; set; }
    }
}