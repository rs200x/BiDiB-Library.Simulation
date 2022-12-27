using System;
using System.Xml.Serialization;
using org.bidib.netbidibc.core.Models;

namespace org.bidib.nbidibc.Simulation.Models.Definition
{
    [Serializable]
    [XmlType(TypeName = "SwitchPortType", Namespace = Namespaces.SimulationNamespaceUrl)]
    public class SwitchPort : Port { }
}