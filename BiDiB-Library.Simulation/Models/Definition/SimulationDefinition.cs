using System;
using System.Xml.Serialization;
using org.bidib.Net.Core.Models;

namespace org.bidib.Net.Simulation.Models.Definition
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