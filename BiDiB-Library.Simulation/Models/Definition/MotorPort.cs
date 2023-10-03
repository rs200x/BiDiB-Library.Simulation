using System;
using System.Xml.Serialization;
using org.bidib.Net.Core.Models;

namespace org.bidib.Net.Simulation.Models.Definition
{
    [Serializable]
    [XmlType(TypeName = "MotorPortType", Namespace = Namespaces.SimulationNamespaceUrl)]
    public class MotorPort : Port { }
}