// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ILCompiler
{
    internal static partial class LazyGenericsSupport
    {
        private const long s_previousAlgorithmTimeout = 10000;

        private sealed partial class Graph<P>
        {
            private sealed class TarjanWorkerClass
            {
                private Stack<Vertex> _inProgress = new Stack<Vertex>();
                private int _currentComponentIndex;
                private Vertex[] _vertices;
                private List<List<Vertex>> _result = new List<List<Vertex>>();

                public TarjanWorkerClass(Vertex[] vertices, bool iterative)
                {
                    this._vertices = vertices;

                    foreach (Vertex vertex in vertices)
                    {
                        vertex.Index = -1;
                        vertex.LowLink = -1;
                        vertex.OnStack = false;
                    }

                    foreach (Vertex vertex in vertices)
                    {
                        if (vertex.Index == -1)
                        {
                            if (iterative)
                            {
                                StrongConnectIterative(vertex);
                            }
                            else
                            {
                                StrongConnectRecursive(vertex);
                            }
                        }
                    }
                }

                private void StrongConnectRecursive(Vertex vertex)
                {
                    vertex.Index = _currentComponentIndex;
                    vertex.LowLink = _currentComponentIndex;

                    _currentComponentIndex++;

                    _inProgress.Push(vertex);
                    vertex.OnStack = true;

                    foreach (Edge edge in vertex.Edges)
                    {
                        if (edge.Destination.Index == -1)
                        {
                            // Recurse if the destination has not begun processing yet
                            StrongConnectRecursive(edge.Destination);
                            vertex.LowLink = Math.Min(vertex.LowLink, edge.Destination.LowLink);
                        }
                        else if (edge.Destination.OnStack)
                        {
                            vertex.LowLink = Math.Min(vertex.LowLink, edge.Destination.Index);
                        }
                    }

                    // If v is a root node, pop the stack and generate an SCC
                    if (vertex.LowLink == vertex.Index)
                    {
                        List<Vertex> newStronglyConnectedComponent = new List<Vertex>();
                        Vertex poppedVertex = null;
                        while (poppedVertex != vertex)
                        {
                            poppedVertex = _inProgress.Pop();
                            poppedVertex.OnStack = false;

                            newStronglyConnectedComponent.Add(poppedVertex);
                        }

                        _result.Add(newStronglyConnectedComponent);
                    }
                }

                private struct StrongConnectStackElement
                {
                    public Vertex Vertex;
                    public IEnumerator<Edge> EdgeEnumeratorPosition;
                }

                private Stack<StrongConnectStackElement> IterativeStrongConnectStack = new Stack<StrongConnectStackElement>();

                private void StrongConnectIterative(Vertex vertex)
                {
#pragma warning disable IDE0059 // Unnecessary assignment of a value
                    IEnumerator<Edge> currentEdgeEnumerator = null;
#pragma warning restore IDE0059 // Unnecessary assignment of a value

                StartOfFunctionWithLogicalRecursion:
                    vertex.Index = _currentComponentIndex;
                    vertex.LowLink = _currentComponentIndex;

                    _currentComponentIndex++;

                    _inProgress.Push(vertex);
                    vertex.OnStack = true;

                    currentEdgeEnumerator = vertex.Edges.GetEnumerator();

ReturnFromEndOfRecursiveFunction:
                    if (currentEdgeEnumerator == null)
                    {
                        // Return from logically recursive call
                        StrongConnectStackElement iterativeStackElementOnReturn = IterativeStrongConnectStack.Pop();
                        vertex = iterativeStackElementOnReturn.Vertex;
                        currentEdgeEnumerator = iterativeStackElementOnReturn.EdgeEnumeratorPosition;

                        vertex.LowLink = Math.Min(vertex.LowLink, currentEdgeEnumerator.Current.Destination.LowLink);
                    }

                    while (currentEdgeEnumerator.MoveNext())
                    {
                        Edge edge = currentEdgeEnumerator.Current;

                        if (edge.Destination.Index == -1)
                        {
                            // Recurse if the destination has not begun processing yet
                            StrongConnectStackElement iterativeStackElement = default(StrongConnectStackElement);
                            iterativeStackElement.Vertex = vertex;
                            iterativeStackElement.EdgeEnumeratorPosition = currentEdgeEnumerator;

                            IterativeStrongConnectStack.Push(iterativeStackElement);

#pragma warning disable IDE0059 // Unnecessary assignment of a value
                            currentEdgeEnumerator = null;
#pragma warning restore IDE0059 // Unnecessary assignment of a value
                            vertex = edge.Destination;
                            goto StartOfFunctionWithLogicalRecursion;
                        }
                        else if (edge.Destination.OnStack)
                        {
                            vertex.LowLink = Math.Min(vertex.LowLink, edge.Destination.Index);
                        }
                    }

                    // If v is a root node, pop the stack and generate an SCC
                    if (vertex.LowLink == vertex.Index)
                    {
                        List<Vertex> newStronglyConnectedComponent = new List<Vertex>();
                        Vertex poppedVertex = null;
                        while (poppedVertex != vertex)
                        {
                            poppedVertex = _inProgress.Pop();
                            poppedVertex.OnStack = false;

                            newStronglyConnectedComponent.Add(poppedVertex);
                        }

                        _result.Add(newStronglyConnectedComponent);
                    }

                    if (IterativeStrongConnectStack.Count > 0)
                    {
                        currentEdgeEnumerator = null;
                        vertex = null;
                        goto ReturnFromEndOfRecursiveFunction;
                    }
                }

                public IEnumerable<List<Vertex>> Result
                {
                    get
                    {
                        return _result;
                    }
                }
            }

            private IEnumerable<List<Vertex>> TarjansAlgorithm()
            {
                Vertex[] vertices = _vertexMap.Values.ToArray();

                TarjanWorkerClass tarjansResultsIterative = new TarjanWorkerClass(vertices, true);

#if DEBUG
                TarjanWorkerClass tarjansResultsRecursive = new TarjanWorkerClass(vertices, false);

                // assert the result of the iterative and recursive versions of the algorithm are EXACTLY the same
                Debug.Assert(tarjansResultsIterative.Result.SelectMany(x => x).SequenceEqual(tarjansResultsRecursive.Result.SelectMany(x => x)));
#endif
                return tarjansResultsIterative.Result;
            }

            /// <summary>
            /// Returns the set of vertices that are part of at least one cycle where one of the edges are flagged.
            /// </summary>
            public IEnumerable<P> ComputeVerticesInvolvedInAFlaggedCycle()
            {
                // New Algorithm.
                IEnumerable<List<Vertex>> stronglyConnectedComponents = this.TarjansAlgorithm();
                foreach (List<Vertex> stronglyConnectedComponent in stronglyConnectedComponents)
                {
                    HashSet<Vertex> strongConnectedComponentVertexContainsChecker = new HashSet<Vertex>(stronglyConnectedComponent);
                    // Detect flags between elements of cycle.
                    // Walk all edges of the strongly connected component.
                    //  - If an edge is not flagged, it can't affect behavior
                    //  - If an edge is flagged, if it refers to another Vertex in the strongly connected component, then the cycle should be flagged.
                    bool flagDetected = false;
                    foreach (Vertex vertex in stronglyConnectedComponent)
                    {
                        foreach (Edge edge in vertex.Edges)
                        {
                            if (edge.Flagged)
                            {
                                if (strongConnectedComponentVertexContainsChecker.Contains(edge.Destination))
                                {
                                    flagDetected = true;
                                    break;
                                }
                            }
                        }

                        if (flagDetected)
                        {
                            break;
                        }
                    }

                    // Flag was detected, therefore each vertex in the strongly connected component is part of a flagged cycle
                    if (flagDetected)
                    {
                        foreach (Vertex vertex in stronglyConnectedComponent)
                        {
                            vertex.ProvedToBeInvolvedInAFlaggedCycle = true;
                        }
                    }
                }

                IEnumerable<Vertex> verticesInAFlaggedCycleTarjanStyle = _vertexMap.Values.Where(v => v.ProvedToBeInvolvedInAFlaggedCycle);

#if DEBUG
                Vertex[] vertices = _vertexMap.Values.ToArray();
                foreach (Vertex vertex in vertices)
                {
                    vertex.ProvedToBeNotPartOfAnyCycle = false;
                    vertex.ProvedToBeInvolvedInAFlaggedCycle = false;
                }

                Stopwatch previousAlgorithmTimeoutWatch = new Stopwatch();
                previousAlgorithmTimeoutWatch.Start();
                bool abortedDueToTimeout = false;
                int operationCount = 0;

                for (; ; )
                {
                    //
                    // Each pass visits every vertex and attempts to detect whether it is part of a flagged cycle. The first pass only finds simple
                    // cycles (i.e. it finds T==>U==>T but not T-->U==>V==>U-->T where "-->" denotes a non-flagged edge and "==>" denotes a flagged edge.). The second
                    // pass detects the "T-->U==>V==>U-->T" cycle (as it now benefits from the knowledge that any cycle including "U" is itself a flagged cycle.)
                    // More complex graphs require more than two passes, with the worst case being the number of vertices.
                    //
                    // Rather than count the passes, however, we will iterate the passes until we find no new cycles.
                    //

                    int count = vertices.Count(v => v.ProvedToBeInvolvedInAFlaggedCycle);
                    foreach (Vertex vertex in vertices)
                    {
                        if (!vertex.ProvedToBeNotPartOfAnyCycle)
                        {
                            if (!vertex.ProvedToBeInvolvedInAFlaggedCycle)
                            {
                                // FindCyclesWorker recurses on edges rather than vertices, so we have to invent a fictitious edge leading to the root vertex
                                // to kick it off.
                                Edge startingEdge = new Edge(vertex, false);
                                FindCyclesWorker(startingEdge, new List<Edge>(), ref operationCount, previousAlgorithmTimeoutWatch);
                            }
                        }
                    }

                    if (previousAlgorithmTimeoutWatch.ElapsedMilliseconds > s_previousAlgorithmTimeout)
                    {
                        abortedDueToTimeout = true;
                        break;
                    }

                    int newCount = vertices.Count(v => v.ProvedToBeInvolvedInAFlaggedCycle);
                    if (count == newCount)
                        break;

                }
                previousAlgorithmTimeoutWatch.Stop();

                if (!abortedDueToTimeout)
                {
                    Vertex[] verticesInAFlaggedCyclePreviousAlgorithmStyle = vertices.Where(v => v.ProvedToBeInvolvedInAFlaggedCycle).ToArray();

                    // Generate hashset of preview algorithm style result
                    Debug.Assert(verticesInAFlaggedCyclePreviousAlgorithmStyle.Length == verticesInAFlaggedCycleTarjanStyle.Count());
                    HashSet<Vertex> verticesInFlaggedCyclePreviousAlgorithmStyleHashset = new HashSet<Vertex>(verticesInAFlaggedCyclePreviousAlgorithmStyle);
                    foreach (Vertex v in verticesInAFlaggedCycleTarjanStyle)
                    {
                        Debug.Assert(verticesInFlaggedCyclePreviousAlgorithmStyleHashset.Contains(v));
                    }
                }
#endif

                return verticesInAFlaggedCycleTarjanStyle.Select(v => v.Payload).ToArray();
            }

            /// <summary>
            /// Depth-first walk every path from edge.Destination to every other reachable vertex. If one of those vertices is edge.Destination itself
            /// and the cycle includes a flagged edge or a vertex that itself is involved in a flagged cycle, then mark edge.Destination as belonging to a flagged cycle.
            /// </summary>
            /// <remarks>
            /// "alreadySeen" is actually a stack but we use a List&lt;&gt; because Stack&lt;&gt; doesn't support indexing.
            /// </remarks>
            private void FindCyclesWorker(Edge edge, List<Edge> alreadySeen, ref int operationCount, Stopwatch previousAlgorithmTimeoutWatch)
            {
                Vertex vertex = edge.Destination;

                if (vertex.ProvedToBeNotPartOfAnyCycle)
                    return;

                if ((operationCount % 10000) == 0)
                {
                    if (previousAlgorithmTimeoutWatch.ElapsedMilliseconds > s_previousAlgorithmTimeout)
                    {
                        return;
                    }
                }
                operationCount++;

                // If this a vertex we've visited already on this path?
                bool flagged = edge.Flagged || vertex.ProvedToBeInvolvedInAFlaggedCycle;

                int idx = alreadySeen.Count - 1;
                while (idx != -1 && !(alreadySeen[idx].Destination == vertex))
                {
                    if (alreadySeen[idx].Flagged || alreadySeen[idx].Destination.ProvedToBeInvolvedInAFlaggedCycle)
                        flagged = true;
                    idx--;
                }

                if (idx != -1)
                {
                    Debug.Assert(alreadySeen[idx].Destination == vertex);

                    // We've seen this vertex already in our path. We now know that this vertex is involved in a simple cycle.
                    //
                    // At minimum, we need to mark the root vertex (alreadySeen[0].Destination) if the cycle includes the root and
                    // includes a flagged edge or a vertex that is itself known to be part of a flagged cycle.
                    // That's the primary answer our caller seeks.
                    //
                    // At minimum, we also need to stop recursing so that our caller gets an answer at all.
                    //
                    // Having said that, we are in a position to do more than the minimum since we may have
                    // discovered a flagged cycle involving vertices other than the root and are in a position to mark them.
                    // So we'll be nice and do that too as this will save work overall.
                    if (flagged)
                    {
                        while (idx < alreadySeen.Count)
                        {
                            alreadySeen[idx++].Destination.ProvedToBeInvolvedInAFlaggedCycle = true;
                        }
                    }

                    return;
                }

                bool allChildrenProvenNotPartOfCycle = true;
                alreadySeen.Add(edge);  // Push
                foreach (Edge newEdge in vertex.Edges)
                {
                    FindCyclesWorker(newEdge, alreadySeen, ref operationCount, previousAlgorithmTimeoutWatch);
                    allChildrenProvenNotPartOfCycle = allChildrenProvenNotPartOfCycle && newEdge.Destination.ProvedToBeNotPartOfAnyCycle;
                }
                alreadySeen.RemoveAt(alreadySeen.Count - 1); // Pop

                if (allChildrenProvenNotPartOfCycle)
                    vertex.ProvedToBeNotPartOfAnyCycle = true;

                return;
            }
        }
    }
}
