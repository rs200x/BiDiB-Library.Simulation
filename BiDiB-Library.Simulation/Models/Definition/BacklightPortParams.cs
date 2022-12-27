using System;
using System.Xml.Serialization;
using org.bidib.netbidibc.core.Models;

namespace org.bidib.nbidibc.Simulation.Models.Definition
{
    [Serializable]
    [XmlType(TypeName = "BacklightPortParamsType", Namespace = Namespaces.SimulationNamespaceUrl)]
    public class BacklightPortParams : PortParams { }
}