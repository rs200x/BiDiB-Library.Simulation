using System;

namespace org.bidib.Net.Simulation.Services
{
    public interface IBiDiBInterfaceSimulator : IDisposable
    {
        string SimulationFilePath { get; }

        void ProcessMessage(byte[] messageBytes);

        void Load(string simulationFilePath);

        void Start();

        void Stop();

        Action<byte[]> DataReceived { get; set; }
    }
}