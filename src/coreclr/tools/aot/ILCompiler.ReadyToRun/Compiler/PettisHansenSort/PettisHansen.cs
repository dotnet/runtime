// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILCompiler.PettisHansenSort
{
    public static class PettisHansen
    {
        public static List<List<int>> Sort(List<CallGraphNode> graph)
        {
            // Create initial graph with a node for each method.
            DisjointSetForest unionFind = new DisjointSetForest(graph.Count);
            var phNodes = new List<int>[graph.Count];
            // Undirected edges, stored in both directions.
            var phEdges = new Dictionary<int, long>[graph.Count];
            // Construct initial graph with nodes for each method.
            for (int i = 0; i < phNodes.Length; i++)
            {
                CallGraphNode node = graph[i];
                phNodes[i] = new List<int>(1) { i };
                var dict = new Dictionary<int, long>(node.OutgoingEdges.Count);
                phEdges[i] = dict;
            }

            void AddEdge(int a, int b, long weight)
            {
                if (a == b)
                    return;

                if (phEdges[a].TryGetValue(b, out long curWeight))
                    phEdges[a][b] = curWeight + weight;
                else
                    phEdges[a].Add(b, weight);
            }
            // Now add edges.
            for (int i = 0; i < phNodes.Length; i++)
            {
                foreach (var kvp in graph[i].OutgoingEdges)
                {
                    AddEdge(i, kvp.Key.Index, kvp.Value);
                    AddEdge(kvp.Key.Index, i, kvp.Value);
                }
            }

#if DEBUG
            for (int i = 0; i < phNodes.Length; i++)
            {
                foreach (var kvp in phEdges[i])
                    Debug.Assert(phEdges[kvp.Key][i] == phEdges[i][kvp.Key]);
            }
#endif

            var queue = new PriorityQueue<(int from, int to), long>();
            for (int i = 0; i < phEdges.Length; i++)
            {
                foreach (var kvp in phEdges[i])
                {
                    if (kvp.Key > i)
                    {
                        queue.Enqueue((i, kvp.Key), -kvp.Value); // PriorityQueue gives lowest prio first
                    }
                }
            }

            while (queue.Count > 0)
            {
                (int from, int to) = queue.Dequeue();
                from = unionFind.FindSet(from);
                to = unionFind.FindSet(to);

                if (from == to)
                    continue; // Already unioned through a different path

                Debug.Assert(phEdges[from][to] == phEdges[to][from]);

                bool unioned = unionFind.Union(from, to);
                Trace.Assert(unioned);

                int winner = unionFind.FindSet(from);
                int loser = winner == from ? to : from;

                long OrigWeight(int a, int b)
                {
                    graph[a].OutgoingEdges.TryGetValue(graph[b], out long ab);
                    graph[b].OutgoingEdges.TryGetValue(graph[a], out long ba);
                    return ab + ba;
                }

                // Transfer all method names from loser to winner, preferring highest weight between endpoints
                long wff = OrigWeight(phNodes[winner].First(), phNodes[loser].First());
                long wfl = OrigWeight(phNodes[winner].First(), phNodes[loser].Last());
                long wlf = OrigWeight(phNodes[winner].Last(), phNodes[loser].First());
                long wll = OrigWeight(phNodes[winner].Last(), phNodes[loser].Last());
                if (wlf >= wff && wlf >= wfl && wlf >= wll)
                {
                    // Already in right order
                }
                else if (wll >= wff && wll >= wfl && wll >= wlf)
                {
                    phNodes[loser].Reverse();
                }
                else if (wff >= wfl && wff >= wlf && wff >= wll)
                {
                    phNodes[winner].Reverse();
                }
                else
                {
                    Debug.Assert(wfl >= wff && wfl >= wlf && wfl >= wll);
                    phNodes[winner].Reverse();
                    phNodes[loser].Reverse();
                }

                phNodes[winner].AddRange(phNodes[loser]);
                phNodes[loser].Clear();

                // Verify that there is exactly one edge between winner's set and loser's set
                Debug.Assert(phEdges[loser].Count(e => unionFind.FindSet(e.Key) == winner) == 1);

                // Get rid of unifying edge
                phEdges[winner].Remove(loser);
                phEdges[loser].Remove(winner);

                // Transfer all edges from loser to winner, coalescing when there are multiple.
                foreach (var edge in phEdges[loser])
                {
                    // Remove counter edge.
                    bool removed = phEdges[edge.Key].Remove(loser);
                    Debug.Assert(removed);

                    // Add edge and counter edge, coalescing when necessary.
                    AddEdge(winner, edge.Key, edge.Value);
                    AddEdge(edge.Key, winner, edge.Value);
                    // Add a new entry in the queue as the edge could have changed weight from coalescing.
                    long weight = phEdges[winner][edge.Key];
                    queue.Enqueue((winner, edge.Key), -weight); // Priority queue gives lowest priority first
                }

                phEdges[loser].Clear();

#if DEBUG
                // Assert that there are only edges between representatives.
                for (int i = 0; i < phEdges.Length; i++)
                {
                    foreach (var edge in phEdges[i])
                    {
                        Debug.Assert(unionFind.FindSet(i) == i && unionFind.FindSet(edge.Key) == edge.Key);
                        Debug.Assert(phEdges[edge.Key][i] == phEdges[i][edge.Key]);
                    }
                }
#endif
            }

            // Order by component size as we return. Note that we rely on the
            // stability of the sort here to keep trivial components of only a
            // single method (meaning that we did not see any call edges) in
            // the same order as it was in the input (i.e. increasing indices,
            // asserted below).
            List<List<int>> components =
                phNodes
                .Where(n => n.Count != 0)
                .OrderByDescending(n => n.Count)
                .ToList();

            // We also expect to see a permutation.
            Debug.Assert(components.SelectMany(l => l).OrderBy(i => i).SequenceEqual(Enumerable.Range(0, graph.Count)));

#if DEBUG
            int prev = -1;
            foreach (List<int> component in components.SkipWhile(l => l.Count != 1))
            {
                Debug.Assert(component[0] > prev);
                prev = component[0];
            }
#endif

            return components;
        }
    }
}
