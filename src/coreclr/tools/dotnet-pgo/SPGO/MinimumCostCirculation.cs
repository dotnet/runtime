// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Pgo
{

    /********
     * This class find the minimum-cost circulation on a circulation graph.
     * 
     * The CirculationGraph object that this acts on seeks to minimize the value of
     * TotalCirculationCost() while maintaining flow invariants (that the flow into a node
     * equals the flow out of a node.)
     * 
     * The standard way to solve this problem is to start with a consistent circulation
     * which probably has a non-minimum cost, then to find cycles where all the edges
     * 
     * (1) All have positive capacities.
     * (2) Have a negative sum of costs.
     * 
     * Then the algorithm will force as much flow around this cycle as possible, thus decreasing
     * the cost. It is possible to prove that iterating this algorithm until no negative cycles
     * exist will always find the optimal solution as long as the costs/capacities are integers.
     * 
     * In this implementation, Bellman-Ford's minimum cost path-finding algorithm is used to find
     * negative cycles (it is able to detect whether and where a negative cycle exists if it does not
     * halt in a consistent state, since graphs with negative cycle will not have "shortest path"
     * well-defined for all pairs of nodes.)
     * 
     * This algorithm is by far the worst in the literature in terms of asymptotic runtime complexity, 
     * but very simple to implement. If the process of finding min-cost circulations become a
     * bottleneck, much more efficient algorithms exist.
     ********/


    public class MinimumCostCirculation
    {

        // Changes graph state into a minimum-cost circulation, if it exists.
        public static void FindMinCostCirculation(CirculationGraph graph, int smoothingIterations = -1)
        {
            int numIterations = 0;

            // Represent a cycle as a tuple of the edges on the cycle and the minimum free capacity.
            Tuple<List<Edge>, long> cycle = FindNegativeCycle(graph);
            while (cycle.Item1 != null && numIterations != smoothingIterations)
            {

                // Force flow equal to the minimum free capacity through all the edges on the negative cycle.
                foreach (Edge e in cycle.Item1)
                {
                    e.AddFlow(cycle.Item2);
                }

                // Ensure that our new flow does not violate any flow conditions.
                graph.CheckConsistentCirculation();
                cycle = FindNegativeCycle(graph);
                numIterations++;
            }
        }

        // Returns a negative cycle on the graph, if it exists.
        // Judicious choice of this cycle is the main way to get asymptotic speed-up.
        // Current implementation: Application of Bellman-Ford shortest path algorithm.
        public static Tuple<List<Edge>, long> FindNegativeCycle(CirculationGraph graph)
        {
            // First reset the metadata associated with this algorithm.

            foreach (Node n in graph.Nodes)
            {
                n.MetaData = new NodeMetaData();
            }
            // Decide which edges should even be considered for increasing flow by those with positive Free space.

            List<Edge> viableEdges = new List<Edge>();
            foreach (Edge e in graph.Edges)
            {
                if (e.Free > 0)
                {
                    viableEdges.Add(e);
                }
            }

            // Iterate Bellman-Ford n-1 times.
            for (int i = 0; i < graph.Nodes.Count - 1; i++)
            {
                foreach (Edge e in viableEdges)
                {
                    if (e.Target.MetaData.Distance > e.Source.MetaData.Distance + e.Cost)
                    {
                        e.Target.MetaData.Distance = e.Source.MetaData.Distance + e.Cost;
                        e.Target.MetaData.PredEdge = e;
                    }
                }
            }
            // Iterate over all edges one last time to find negative cycles.

            foreach (Edge e in viableEdges)
            {
                if (e.Target.MetaData.Distance > e.Source.MetaData.Distance + e.Cost)
                {
                    return FindBellmanFordCycle(e.Source);
                }
            }
            // Return a null cycle if no negative cycles are found; signals that there are no more negative cycles.

            return Tuple.Create<List<Edge>, long>(null, 0);
        }

        // After Bellman-Ford is run and a negative cycle is signalled, find that negative cost cycle by traversing the
        // parent edges until a repeat node is reached.
        public static Tuple<List<Edge>, long> FindBellmanFordCycle(Node currentNode)
        {
            // Set of seen nodes; once there is a repeat we know we are lying on a cycle.

            HashSet<Node> seenNodes = new HashSet<Node>();
            while (!seenNodes.Contains(currentNode))
            {
                seenNodes.Add(currentNode);
                currentNode = currentNode.MetaData.PredEdge.Source;
            }
            // Now keep traversing up this cycle until a repeat node is found, deriving the edges and min capacity.

            List<Edge> cycleEdges = new List<Edge>();
            long minCapacity = long.MaxValue;
            seenNodes = new HashSet<Node>();

            while (!seenNodes.Contains(currentNode))
            {
                seenNodes.Add(currentNode);
                minCapacity = Math.Min(minCapacity, currentNode.MetaData.PredEdge.Free);
                cycleEdges.Add(currentNode.MetaData.PredEdge);
                currentNode = currentNode.MetaData.PredEdge.Source;
            }

            return Tuple.Create<List<Edge>, long>(cycleEdges, minCapacity);
        }
    }
}
