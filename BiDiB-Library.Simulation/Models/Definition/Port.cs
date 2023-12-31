﻿using System;
using System.Xml.Serialization;
using org.bidib.Net.Core.Models;

namespace org.bidib.Net.Simulation.Models.Definition
{
    [XmlInclude(typeof(InputPort))]
    [XmlInclude(typeof(MotorPort))]
    [XmlInclude(typeof(SoundPort))]
    [XmlInclude(typeof(ServoPort))]
    [XmlInclude(typeof(SwitchPort))]
    [XmlInclude(typeof(LightPort))]
    [XmlInclude(typeof(BacklightPort))]
    [Serializable]
    [XmlType(TypeName = "PortType", Namespace = Namespaces.SimulationNamespaceUrl)]
    public abstract class Port
    {
        [XmlAttribute("count")]
        public int Count { get; set; }

        [XmlAttribute("offset")]
        public int Offset { get; set; }
    }
}