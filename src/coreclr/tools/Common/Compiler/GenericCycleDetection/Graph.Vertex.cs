// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace ILCompiler
{
    internal static partial class LazyGenericsSupport
    {
        private sealed partial class Graph<P>
        {
            /// <summary>
            /// Gets or creates the canonical Vertex object for this payload value. Payload equality is defined by its Object.Equals() override. Vertex equality
            /// is determined by reference equality.
            /// </summary>
            private Vertex GetVertex(P payload)
            {
                Vertex vertex;
                if (_vertexMap.TryGetValue(payload, out vertex))
                    return vertex;
                vertex = new Vertex(payload);
                _vertexMap.Add(payload, vertex);
                return vertex;
            }

            private sealed class Vertex
            {
                public Vertex(P payload)
                {
                    this.Payload = payload;
                    this._edges = new List<Edge>();
                    return;
                }

                public P Payload { get; private set; }

                public IEnumerable<Edge> Edges { get { return _edges; } }

                public void AddEdge(Vertex toVertex, bool flagged)
                {
                    for (int i = 0; i < _edges.Count; i++)
                    {
                        if (_edges[i].Destination.Equals(toVertex))
                        {
                            if (flagged)
                            {
                                // Don't shorten to "_edge[i].Flagged = true" - that falls into the "update a compiler temp" gotcha.
                                Edge e = _edges[i];
                                e.Flagged = true;
                                _edges[i] = e;
                            }
                            return;
                        }
                    }
                    Edge newEdge = new Edge(toVertex, flagged);
                    _edges.Add(newEdge);
                    return;
                }


                /// <summary>
                /// If true, we have established that this vertex is part of a cycle in which at least one edge is flagged (abbreviated as "flagged cycle"
                ///   in the interests of brevity.)
                /// If false, we have not yet established (but may yet establish) this fact.
                /// </summary>
                public bool ProvedToBeInvolvedInAFlaggedCycle;

                /// <summary>
                /// If true, we have established that this vertex is not part of any cycle.
                /// If false, we have not yet established (but may yet establish) this fact.
                /// </summary>
                public bool ProvedToBeNotPartOfAnyCycle;

                /// <summary>
                /// Flag used during Tarjan's algorithm
                /// </summary>
                public bool OnStack;

                /// <summary>
                /// Index used in Tarjan's algorithm
                /// </summary>
                public int Index = -1;

                /// <summary>
                /// LowLink field for Tarjan's algorithm
                /// </summary>
                public int LowLink = -1;

                public sealed override string ToString()
                {
                    return this.Payload.ToString();
                }

                private List<Edge> _edges = new List<Edge>();
            }

            private struct Edge
            {
                public Edge(Vertex destination, bool flagged)
                {
                    this.Destination = destination;
                    this.Flagged = flagged;
                    return;
                }

                public readonly Vertex Destination;
                public bool Flagged;

                public override string ToString()
                {
                    return "[" + (Flagged ? "==>" : "-->") + Destination + "]";
                }
            }

            private Dictionary<P, Vertex> _vertexMap = new Dictionary<P, Vertex>();
        }
    }
}
