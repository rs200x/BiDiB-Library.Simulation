using System;
using System.Xml.Serialization;
using org.bidib.Net.Core.Models;

namespace org.bidib.Net.Simulation.Models.Definition
{
    [Serializable]
    [XmlType(TypeName = "CvType", Namespace = Namespaces.SimulationNamespaceUrl)]
    public class Cv
    {
        [XmlAttribute("number")]
        public string Number { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }
    }
}