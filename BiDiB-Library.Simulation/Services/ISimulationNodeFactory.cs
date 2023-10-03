using System;
using org.bidib.Net.Simulation.Models.Definition;
using org.bidib.Net.Simulation.Models.Nodes;

namespace org.bidib.Net.Simulation.Services;

public interface ISimulationNodeFactory
{
    SimulationNode Create(Node node, Action<byte[]> addResponse);
}