using System;
using System.Xml.Serialization;
using org.bidib.netbidibc.core.Models;

namespace org.bidib.nbidibc.Simulation.Models.Definition
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