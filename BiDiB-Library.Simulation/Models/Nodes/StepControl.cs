using System;
using System.Collections.Generic;

namespace org.bidib.Net.Simulation.Models.Nodes
{
    public class StepControl : SimulationNode
    {
        private readonly Dictionary<byte, Tuple<bool, uint>> turnouts;
        private const uint TotalSteps = 12800;
        private uint currentPosition;
        private uint targetPosition;
        private byte currentAngle;
        private byte targetAngle;

        public StepControl()
        {
            turnouts = new Dictionary<byte, Tuple<bool, uint>>
            {
                {0, new Tuple<bool, uint>(false, 0)},
                {1, new Tuple<bool, uint>(false, 800)},
                {2, new Tuple<bool, uint>(false, 1600)},
                {3, new Tuple<bool, uint>(false, 2667)},
                {4, new Tuple<bool, uint>(false, 5600)},
                {5, new Tuple<bool, uint>(false, 6400)},
                {6, new Tuple<bool, uint>(false, 7200)},
                {7, new Tuple<bool, uint>(false, 8000)},
                {8, new Tuple<bool, uint>(false, 9067)},
                {9, new Tuple<bool, uint>(true, 12000)},
                {10, new Tuple<bool, uint>(false, 4294967295)}
            };

            currentAngle = 0;
            currentPosition = 0;

            AccessoryAspects.Add(0, 1);
            AccessoryAspects.Add(1, 1);
            AccessoryAspects.Add(2, 0);
            AccessoryAspects.Add(3, 255);
            AccessoryAspects.Add(4, 255);
            AccessoryAspects.Add(5, 255);

            SetSampleValues();
        }

        protected override void OnHandleVendorGet(string cvName)
        {
            base.OnHandleVendorGet(cvName);

            if (cvName != "STEPPOS")
            {
                return;
            }

            uint gap = currentPosition < targetPosition ? targetPosition - currentPosition : currentPosition - targetPosition;
            if (gap < 100)
            {
                currentPosition = targetPosition;
            }
            else
            {
                currentPosition = currentPosition < targetPosition ? currentPosition + 100 : currentPosition - 100;
            }

            if (currentPosition > TotalSteps)
            {
                currentPosition -= TotalSteps;
            }

            currentAngle = GetAngle(targetPosition);
            CvValues[cvName] = currentPosition.ToString();
        }

        protected override void OnHandleVendorSet(string cvName, string cvValue)
        {
            base.OnHandleVendorSet(cvName, cvValue);

            if (cvName == "STEPPOS")
            {
                targetPosition = uint.Parse(cvValue);
                targetAngle = GetAngle(targetPosition);
                cvValue = currentPosition.ToString();
            }

            CvValues[cvName] = cvValue;
        }

        protected override void OnAccessorySet(byte accessory, byte aspect, ICollection<byte> parameters)
        {
            base.OnAccessorySet(accessory, aspect, parameters);

            if (accessory == 0)
            {
                Tuple<bool, uint> turnout = turnouts[aspect];
                targetPosition = turnout.Item2;
                targetAngle = GetAngle(targetPosition);
            }
            parameters.Add(1);
            parameters.Add(currentAngle);
            parameters.Add(2);
            parameters.Add(targetAngle);
        }

        protected override void OnAccessoryGet(byte accessory, ICollection<byte> parameters)
        {
            base.OnAccessoryGet(accessory, parameters);

            if (accessory != 0 || currentAngle == targetAngle) { return; }

            int gap = Math.Abs(currentAngle - targetAngle);

            if (gap < 5)
            {
                currentAngle = targetAngle;
            }
            else
            {
                currentAngle = currentAngle < targetAngle ? Convert.ToByte(currentAngle + 5) : Convert.ToByte(currentAngle - 5);
            }

            if (currentAngle > 240)
            {
                currentAngle = Convert.ToByte(currentAngle - 240);
            }

            currentPosition = Convert.ToUInt32(((currentAngle * 1.5) * TotalSteps) / 360);

            parameters.Add(1);
            parameters.Add(currentAngle);
            parameters.Add(2);
            parameters.Add(targetAngle);
        }

        private void SetSampleValues()
        {
            CvValues.Add("116", "0");
            CvValues.Add("117", "50");
            CvValues.Add("118", "0");
            CvValues.Add("119", "0");

            CvValues.Add("POLA", "1");
            CvValues.Add("STEPPOS", "0");

            int index = 169;

            foreach (KeyValuePair<byte, Tuple<bool, uint>> keyValuePair in turnouts)
            {
                CvValues.Add(index.ToString(), keyValuePair.Value.Item1 ? "1" : "0");
                index++;
                byte[] valueBytes = BitConverter.GetBytes(keyValuePair.Value.Item2);
                foreach (byte valueByte in valueBytes)
                {
                    CvValues.Add(index.ToString(), valueByte.ToString());
                    index++;
                }
            }
        }

        private static byte GetAngle(uint pos)
        {
            return Convert.ToByte(pos * 360 / TotalSteps / 1.5);
        }
    }
}