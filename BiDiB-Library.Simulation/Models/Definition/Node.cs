using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using org.bidib.Net.Core.Models;

namespace org.bidib.Net.Simulation.Models.Definition
{
    [XmlInclude(typeof(Hub))]
    [XmlInclude(typeof(Master))]
    [Serializable]
    [XmlType(TypeName = "NodeType", Namespace = Namespaces.SimulationNamespaceUrl)]
    public class Node
    {
        private byte[] uniqueId;

        public Node()
        {
            ProtocolVersion= "0.5";
            SoftwareVersion = "0.0.1";
        }

        [XmlElement("BACKLIGHT")]
        public BacklightPort Backlight { get; set; }

        [XmlElement("LPORT")]
        public LightPort Lport { get; set; }

        [XmlElement("SPORT")]
        public SwitchPort Sport { get; set; }

        [XmlElement("INPUT")]
        public InputPort Input { get; set; }

        [XmlElement("SERVO")]
        public ServoPort Servo { get; set; }

        [XmlElement("SOUND")]
        public SoundPort Sound { get; set; }

        [XmlElement("MOTOR")]
        public MotorPort Motor { get; set; }

        [XmlElement("DmxChannels")]
        public DmxChannels DmxChannels { get; set; }

        [XmlArrayItem("feature", IsNullable = false)]
        [XmlArray("Features")]
        public FeatureType[] Features { get; set; }

        [XmlArrayItem("cv", IsNullable = false)]
        [XmlArray("CVs")]
        public Cv[] CVs { get; set; }

        [XmlAttribute("uniqueId", DataType = "hexBinary")]
        public byte[] UniqueId
        {
            get { return uniqueId; }
            set
            {
                uniqueId = value; 
                UpdateNodeInfo();
            }
        }

        [XmlIgnore]
        public long UniqueIdLong { get; private set; }

        [XmlAttribute("protocolVersion")]
        [DefaultValue("0.5")]
        public string ProtocolVersion { get; set; }

        [XmlAttribute("className")]
        public string ClassName { get; set; }

        [XmlAttribute("address")]
        public string Address { get; set; }

        [XmlAttribute("autoAddFeature")]
        public bool AutoAddFeature { get; set; }

        [XmlAttribute("productName")]
        public string ProductName { get; set; }

        [XmlAttribute("userName")]
        public string UserName { get; set; }

        [XmlAttribute("softwareVersion")]
        [DefaultValue("0.0.1")]
        public string SoftwareVersion { get; set; }

        [XmlIgnore]
        public byte ClassId { get; private set; }

        [XmlIgnore]
        public byte ClassIdExtended { get; private set; }

        [XmlIgnore]
        public int ManufacturerId { get; private set; }

        [XmlIgnore]
        public int ProductId { get; private set; }

        private void UpdateNodeInfo()
        {
            //Array.Reverse(UniqueId);

            ClassId = UniqueId[0];
            ClassIdExtended = UniqueId[1];
            ManufacturerId = UniqueId[2];

            BitArray pidBits = new BitArray(new[] { UniqueId[3], UniqueId[4], UniqueId[5], UniqueId[6]});

            int newPid = 0;

            for (int i = 0; i < 16; i++)
            {
                if (pidBits[i])
                {
                    newPid += Convert.ToInt32(Math.Pow(2, i));
                }
            }

            ProductId = newPid;

            byte[] uniqueIdBytes = new byte[8];
            Array.Reverse(UniqueId);
            Array.Copy(UniqueId, 0, uniqueIdBytes, 0, 7);
            UniqueIdLong = BitConverter.ToInt64(uniqueIdBytes, 0);
        }

        public byte[] GetAddress()
        {
            return Address.Split('.').Select(byte.Parse).ToArray();
        }
    }
}