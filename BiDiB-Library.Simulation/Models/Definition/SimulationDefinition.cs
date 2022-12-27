using System;
using System.Xml.Serialization;
using org.bidib.netbidibc.core.Models;

namespace org.bidib.nbidibc.Simulation.Models.Definition
{
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.SimulationNamespaceUrl)]
    [XmlRoot("simulation", Namespace = Namespaces.SimulationNamespaceUrl, IsNullable = false)]
    public class SimulationDefinition
    {
        [XmlElement("master")]
        public Master Master { get; set; }
    }
}