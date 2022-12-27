using System;
using System.ComponentModel;
using System.Xml.Serialization;
using org.bidib.netbidibc.core.Models;

namespace org.bidib.nbidibc.Simulation.Models.Definition
{
    [XmlInclude(typeof(LightPortParams))]
    [XmlInclude(typeof(BacklightPortParams))]
    [Serializable]
    [XmlType(TypeName= "PortParamsType", Namespace = Namespaces.SimulationNamespaceUrl)]
    public abstract class PortParams
    {
        protected PortParams()
        {
            DmxMapping = 0;
            DimSlopeUp = 1;
            DimSlopeDown = 1;
            Value = 0;
        }

        [XmlAttribute("portId")]
        public int PortId { get; set; }

        [XmlAttribute("dmxMapping")]
        [DefaultValue(0)]
        public int DmxMapping { get; set; }

        [XmlAttribute("dimSlopeUp")]
        [DefaultValue(1)]
        public int DimSlopeUp { get; set; }

        [XmlAttribute("dimSlopeDown")]
        [DefaultValue(1)]
        public int DimSlopeDown { get; set; }

        [XmlAttribute("value")]
        [DefaultValue(0)]
        public int Value { get; set; }
    }
}