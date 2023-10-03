using System;
using System.ComponentModel;
using System.Xml.Serialization;
using org.bidib.Net.Core.Models;

namespace org.bidib.Net.Simulation.Models.Definition
{
    [Serializable]
    [XmlType(TypeName = "LightPortParamsType", Namespace = Namespaces.SimulationNamespaceUrl)]
    public class LightPortParams : PortParams
    {
        public LightPortParams()
        {
            IntensityOff = 0;
            IntensityOn = 0;
        }

        [XmlAttribute("intensityOff")]
        [DefaultValue(0)]
        public int IntensityOff { get; set; }

        [XmlAttribute("intensityOn")]
        [DefaultValue(0)]
        public int IntensityOn { get; set; }

        [XmlAttribute("rgbValue")]
        public string RgbValue { get; set; }

        [XmlAttribute("transitionTime")]
        public int TransitionTime { get; set; }
    }
}