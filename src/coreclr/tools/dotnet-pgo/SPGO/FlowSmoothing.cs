// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/********
 * This class handles smoothing over a circulation graph to be consistent and cost-minimal.
 * 
 * A circulation graph consists of nodes v, directed edges e, and two functions on the edges:
 * 
 * cost(e) = the cost of each positive unit of flow on the edge
 * capacity(e) = the range of possible values of flow on the edge
 * 
 * where flow is a function on the edges such that, for every node, the flow on in-edges adds up
 * to the flow on out-edges.
 * 
 * The objective of this class's main function (SmoothFlowGraph) is to take an inconsistent count of
 * each node's net flow and map it onto a consistent circulation. This circulation is constructed to map
 * back onto a consistent flow, and when a minimum cost circulation is found (by using a call to
 * MinimumCostCirculation.FindMinCostCirculation), the flow it maps back onto will also minimize
 * a cost metric. In other words, the parameter 'Func<T, bool, long> costFunction' assigns to each
 * node T a cost to increasing its net flow (when the bool is true) and a cost to decreasing its
 * net flow (when the bool is false.) SmoothFlowGraph then constructs a consistent circulation whose
 * cost will be minimized exactly when the cost of changing the net flows of the blocks is minimized.
 * 
 * The translation is outlined in detail in Section 4 of "Complementing Incomplete Edge Profile by applying
 * Minimum Cost Circulation Algorithms" (Levin 2007)
 ********/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    public class FlowSmoothing<T>
    {
        public Dictionary<T, long> NodeResults = new Dictionary<T, long>();
        public Dictionary<(T, T), long> EdgeResults = new Dictionary<(T, T), long>();

        Dictionary<T, long> m_sampleData;
        T m_startBlock;
        Func<T, HashSet<T>> m_successorFunction;
        Func<T, bool, long> m_costFunction;

        public FlowSmoothing(Dictionary<T, long> sampleData, T startBlock, Func<T, HashSet<T>> successorFunction, Func<T, bool, long> costFunction)
        {
            m_sampleData = sampleData;
            m_startBlock = startBlock;
            m_successorFunction = successorFunction;
            m_costFunction = costFunction;
        }

        // Using sampleData, the graph given by successor function, and a cost assigned to each node,
        // find a minimum-cost circulation to get a consistent block count.

        public void Perform(int smoothingIterations = -1)
        {
            // Graph to run the circulation on.
            CirculationGraph graph = new CirculationGraph();

            // Map each concrete block T to a pair of Nodes and an Edge in the circulation graph: entrance, exit, and backedge.
            Dictionary<T, (Node entrance, Node exit, Edge back)> abstractNodeMap = new Dictionary<T, (Node entrance, Node exit, Edge back)>();

            // Create privileged nodes source and target that will be connected to induce flow.
            Node source = new Node();
            Node target = new Node();

            // Sum all the weights so we can properly induce flow from target to source
            long totalWeight = 0;

            // Now generate the nodes of the graph.
            foreach (T basicBlock in m_sampleData.Keys)
            {
                // ----------------------------------
                // Create a subgraph structure:
                //            ( ENTRY )
                //   cost = c- |    | c+
                //cap. = [w,w] ^    v cap. = [0, infty]
                //    flow = w |    | f=0
                //            ( EXIT )
                // ----------------------------------
                Node entryNode = new Node();
                Node exitNode = new Node();

                // Make the blockWeight correspond to the counts acquired from sampling.
                long blockWeight = m_sampleData[basicBlock];
                totalWeight += blockWeight;

                // Create edge from s to exit with capacity equal to the block's weight. Vice-versa for entry to t.
                new Edge(source, exitNode, blockWeight, blockWeight, 0);
                new Edge(entryNode, target, blockWeight, blockWeight, 0);

                // Add the edges for node-splitting with costs given by costFunction
                new Edge(entryNode, exitNode, 0, long.MaxValue, m_costFunction(basicBlock, true));
                Edge backEdge = new Edge(exitNode, entryNode, 0, blockWeight, m_costFunction(basicBlock, false));
                backEdge.AddFlow(blockWeight);

                // Create the entry for basicBlock in abstractNodeMap.
                abstractNodeMap.Add(basicBlock, (entryNode, exitNode, backEdge));
            }
            // Create edges from the exit node of each subgraph to the entry of subgraphs corresponding to the concrete block's successors.

            Dictionary<(T, T), Edge> abstractEdgeMap = new Dictionary<(T, T), Edge>();

            foreach (T predecessorBlock in abstractNodeMap.Keys)
            {
                foreach (T successorBlock in m_successorFunction(predecessorBlock))
                {
                    Node predecessor = abstractNodeMap[predecessorBlock].exit;
                    Node successor = abstractNodeMap[successorBlock].entrance;
                    Edge newEdge = new Edge(predecessor, successor, 0, long.MaxValue, 0);
                    abstractEdgeMap[(predecessorBlock, successorBlock)] = newEdge;
                }
                if (m_successorFunction(predecessorBlock).Count == 0)
                {
                    new Edge(abstractNodeMap[predecessorBlock].exit, abstractNodeMap[m_startBlock].entrance, 0, long.MaxValue, 0);
                }
            }
            // Add the entrance/exit nodes, as well as the source and target nodes, to the graph.

            foreach (T basicBlock in abstractNodeMap.Keys)
            {
                graph.AddNode(abstractNodeMap[basicBlock].entrance);
                graph.AddNode(abstractNodeMap[basicBlock].exit);
            }
            (new Edge(target, source, 0, long.MaxValue, 0)).AddFlow(totalWeight);
            graph.AddNode(source);
            graph.AddNode(target);

            MinimumCostCirculation.FindMinCostCirculation(graph);

            // Derive the new concrete block hit counts by subtracting the backflow from the inflow of the entry node.
            foreach (T concreteNode in abstractNodeMap.Keys)
            {
                long entryNodeFlow = abstractNodeMap[concreteNode].entrance.NetInFlow();
                long backEdgeFlow = abstractNodeMap[concreteNode].back.Flow;
                NodeResults[concreteNode] = entryNodeFlow - backEdgeFlow;
            }
            // Now log all the edge values back into the edgeResult dictionary.

            foreach (var concreteEdge in abstractEdgeMap.Keys)
            {
                EdgeResults[concreteEdge] = abstractEdgeMap[concreteEdge].Flow;
            }

            MakeGraphFeasible();
            CheckGraphConsistency();
        }

        // Helper function to perform parametric mapping on the NodeResults dictionary.
        public Dictionary<T, S> MapNodes<S>(Func<T, long, S> transformation)
        {
            Dictionary<T, S> results = new Dictionary<T, S>();

            foreach (T node in NodeResults.Keys)
            {
                results[node] = transformation(node, NodeResults[node]);
            }

            return results;
        }

        // Helper function to perform parametric mapping on the EdgeResults dictionary.
        public Dictionary<(T, T), S> MapEdges<S>(Func<(T, T), long, S> transformation)
        {
            Dictionary<(T, T), S> results = new Dictionary<(T, T), S>();

            foreach (var edge in EdgeResults.Keys)
            {
                results[edge] = transformation(edge, EdgeResults[edge]);
            }

            return results;
        }

        // Current "hacky" function to ensure that the profile counts are feasible in some execution.
        // Looks for blocks with non-zero counts that are not connected by positive counts to the start and end,
        // These are, by invariants of the smoothing algorithm, necessarily strongly-connected components before this function.
        // Then, perform DFS from the start block to any block of such a strongly-connected component; once found, light up
        // that path with incremented counts; repeat the same from that block to any exit block.
        // There are several invariants that must hold for this to work; as such, this is provisionary but seems to work quickly and adequately
        // without error.
        public void MakeGraphFeasible()
        {
            // Keep a HashSet of which blocks are accessible from m_startBlock, traveling only over positive edges.

            System.Collections.Generic.HashSet<T> reachableFromStart = new System.Collections.Generic.HashSet<T>();
            Queue<T> toExamine = new Queue<T>();
            reachableFromStart.Add(m_startBlock);
            toExamine.Enqueue(m_startBlock);

            // Perform a BFS to populate reachableFromStart; use toExamine as the auxiliary data structure.

            while (toExamine.Count > 0)
            {
                T predBlock = toExamine.Dequeue();

                foreach (T succBlock in m_successorFunction(predBlock))
                {
                    if (EdgeResults[(predBlock, succBlock)] > 0 && !reachableFromStart.Contains(succBlock))
                    {
                        reachableFromStart.Add(succBlock);
                        toExamine.Enqueue(succBlock);
                    }
                }
            }

            // Iterate over each block, checking for the conditions of needing a path "lighted up"

            foreach (T block in m_sampleData.Keys)
            {
                if (NodeResults[block] > 0 && !reachableFromStart.Contains(block))
                {
                    System.Collections.Generic.HashSet<T> stronglyConnectedComponent = new System.Collections.Generic.HashSet<T>();
                    stronglyConnectedComponent.Add(block);
                    toExamine.Enqueue(block);

                    // Build a set containing the strongly connected component. Is possible that it is now connected
                    // due to another "lighting up" iteration, but this case is ignored at present and does not affect feasibility.

                    while (toExamine.Count > 0)
                    {
                        T predBlock = toExamine.Dequeue();

                        foreach (T succBlock in m_successorFunction(predBlock))
                        {
                            if (EdgeResults[(predBlock, succBlock)] > 0 && !stronglyConnectedComponent.Contains(succBlock))
                            {
                                stronglyConnectedComponent.Add(succBlock);
                                toExamine.Enqueue(succBlock);
                            }
                        }
                    }

                    // Now perform a search from the start to the component and from the component to the end.
                    // Increment zero edges along the way along with their two ends.
                    // For now use DFS.

                    System.Collections.Generic.HashSet<T> visited = new System.Collections.Generic.HashSet<T>();
                    Stack<T> trace = new Stack<T>();

                    visited.Add(m_startBlock);
                    trace.Push(m_startBlock);

                    while (!stronglyConnectedComponent.Contains(trace.Peek()))
                    {
                        bool foundSuccessor = false;
                        foreach (T succBlock in m_successorFunction(trace.Peek()))
                        {
                            if (!visited.Contains(succBlock))
                            {
                                visited.Add(succBlock);
                                trace.Push(succBlock);
                                foundSuccessor = true;
                                break;
                            }
                        }

                        if (!foundSuccessor)
                        {
                            trace.Pop();
                        }
                    }

                    // Exhaust stack, "lighting up" path on the way through.

                    T destination = trace.Peek();
                    while (trace.Count > 1)
                    {
                        T succBlock = trace.Pop();
                        T predBlock = trace.Peek();
                        EdgeResults[(predBlock, succBlock)]++;
                        NodeResults[succBlock]++;
                        reachableFromStart.Add(predBlock);
                    }

                    NodeResults[m_startBlock]++;
                    reachableFromStart.UnionWith(stronglyConnectedComponent);

                    // Repeat similar for any node in the strongly connected component to an end block.
                    // Start from the very end of the last path.

                    visited = new System.Collections.Generic.HashSet<T>();
                    trace = new Stack<T>();

                    visited.Add(destination);
                    trace.Push(destination);

                    while (trace.Count > 0 && m_successorFunction(trace.Peek()).Count > 0)
                    {
                        bool foundSuccessor = false;
                        foreach (T succBlock in m_successorFunction(trace.Peek()))
                        {
                            if (!visited.Contains(succBlock))
                            {
                                visited.Add(succBlock);
                                trace.Push(succBlock);
                                foundSuccessor = true;
                                break;
                            }
                        }

                        if (!foundSuccessor)
                        {
                            trace.Pop();
                        }
                    }

                    if (trace.Count == 0)
                    {
                        Console.WriteLine("WARNING: No trace found to exit node. Light up all visited blocks");
                        foreach (T predBlock in visited)
                        {
                            foreach (T succBlock in m_successorFunction(predBlock))
                            {
                                if (visited.Contains(succBlock))
                                {
                                    EdgeResults[(predBlock, succBlock)]++;
                                    NodeResults[succBlock]++;
                                    reachableFromStart.Add(predBlock);
                                }
                            }
                        }
                    }
                    else
                    {
                        while (trace.Count > 1)
                        {
                            T succBlock = trace.Pop();
                            T predBlock = trace.Peek();
                            EdgeResults[(predBlock, succBlock)]++;
                            NodeResults[succBlock]++;
                            reachableFromStart.Add(predBlock);
                        }
                    }
                }
            }
        }

        // For now checks that the flow constraints hold and that the entry block has a count if any block has a count.
        // Throws a descriptive Exception if an inconsistency is found.
        public void CheckGraphConsistency()
        {
            // Logs the in-flow for each node from all of its in-edges.
            Dictionary<T, long> inFlow = new Dictionary<T, long>();
            long totalFlow = 0;

            // Initialize the Dictionary.

            foreach (T node in NodeResults.Keys)
            {
                inFlow[node] = 0;
            }

            foreach (T predNode in NodeResults.Keys)
            {
                long outFlow = 0;
                long flow;

                foreach (T succNode in m_successorFunction(predNode))
                {
                    flow = EdgeResults[(predNode, succNode)];
                    inFlow[succNode] += flow;
                    outFlow += flow;
                    totalFlow += flow;
                }
                // Directs all flow to the entry node if there are no successors.

                if (m_successorFunction(predNode).Count == 0)
                {
                    flow = NodeResults[predNode];
                    inFlow[m_startBlock] += flow;
                    outFlow += flow;
                    totalFlow += flow;
                }
                // Checks for the condition that the node emits as much flow as recorded by NodeResults

                if (NodeResults[predNode] != outFlow)
                {
                    Console.WriteLine(string.Format("WARNING: Node's count is {0}, but emits {1} flow to its successors", NodeResults[predNode], outFlow));
                }
            }
            // Now check that the inFlow of each node adds up correctly.

            foreach (T node in NodeResults.Keys)
            {
                if (NodeResults[node] != inFlow[node])
                {
                    Console.WriteLine(string.Format("WARNING: Node's count is {0}, but accepts {1} from its predecessors", NodeResults[node], inFlow[node]));
                }
            }
            // Preliminary check that the start node has positive count as long as any node in the graph has positive count.

            if (NodeResults[m_startBlock] == 0 && totalFlow > 0)
            {
                Console.WriteLine("WARNING: Graph has positive flow somewhere but zero flow at the entry");
            }
            // Check in more detail whether the graph is feasible. That is, if for every non-zero count there is a positive trace
            // from the start to that block to an exit node. First check accessibility from the start using BFS.

            System.Collections.Generic.HashSet<T> accessibleFromStart = new System.Collections.Generic.HashSet<T>();
            Stack<T> toSee = new Stack<T>();
            toSee.Push(m_startBlock);
            accessibleFromStart.Add(m_startBlock);

            while (toSee.Count > 0)
            {
                T predNode = toSee.Pop();
                foreach (T succNode in m_successorFunction(predNode))
                {
                    if (EdgeResults[(predNode, succNode)] > 0 && !accessibleFromStart.Contains(succNode))
                    {
                        accessibleFromStart.Add(succNode);
                        toSee.Push(succNode);
                    }
                }
            }

            foreach (T node in NodeResults.Keys)
            {
                if (NodeResults[node] > 0 && !accessibleFromStart.Contains(node))
                {
                    Console.WriteLine("WARNING: Node has positive count but not accessible from start");
                }
            }
            // Now, check for blocks that are hit but don't lead to exit nodes.
            // First, reverse the direction of the successor function and find the exit nodes.

            Dictionary<T, List<T>> predMap = new Dictionary<T, List<T>>();
            Stack<T> exitableNodes = new Stack<T>();
            System.Collections.Generic.HashSet<T> accessibleFromExit = new System.Collections.Generic.HashSet<T>();

            // Initialize the predecessor map.

            foreach (T node in NodeResults.Keys)
            {
                predMap[node] = new List<T>();
            }

            foreach (T predNode in NodeResults.Keys)
            {
                foreach (T succNode in m_successorFunction(predNode))
                {
                    predMap[succNode].Add(predNode);
                }
                if (m_successorFunction(predNode).Count == 0)
                {
                    exitableNodes.Push(predNode);
                    accessibleFromExit.Add(predNode);
                }
            }

            // Then, produce the exit-able blocks by BFS.

            while (exitableNodes.Count > 0)
            {
                T succNode = exitableNodes.Pop();
                foreach (T predNode in predMap[succNode])
                {
                    if (EdgeResults[(predNode, succNode)] > 0 && !accessibleFromExit.Contains(predNode))
                    {
                        exitableNodes.Push(predNode);
                        accessibleFromExit.Add(predNode);
                    }
                }
            }
            // Finally, iterate over all the nodes and check that if they have positive count, they are accessible from the exit.

            foreach (T node in NodeResults.Keys)
            {
                if (NodeResults[node] > 0 && !accessibleFromExit.Contains(node))
                {
                    Console.WriteLine("WARNING: Node has positive count does not reach an exit node");
                }
            }
        }
    }
}
