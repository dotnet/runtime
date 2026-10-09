// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

using Internal.TypeSystem;

namespace ILCompiler
{
    internal readonly struct ReadyToRunMethodCompilationInfo
    {
        public ReadyToRunMethodCompilationInfo(int codeSize)
        {
            CodeSize = codeSize;
        }

        public int CodeSize { get; }
    }

    internal sealed class ReadyToRunCallGraphBuilder
    {
        private readonly ConcurrentDictionary<MethodDesc, ConcurrentDictionary<MethodDesc, byte>> _edges = new();

        public void AddCall(MethodDesc caller, MethodDesc callee)
        {
            _edges.GetOrAdd(caller, static _ => new ConcurrentDictionary<MethodDesc, byte>()).TryAdd(callee, 0);
        }

        public ReadyToRunCompilationPlan CreatePlan(List<MethodDesc> methods)
        {
            return ReadyToRunCompilationPlan.Create(methods, _edges);
        }
    }

    public sealed class ReadyToRunCompilationPlan
    {
        private readonly MethodDesc[][] _methodsByLevel;

        private ReadyToRunCompilationPlan(MethodDesc[][] methodsByLevel)
        {
            _methodsByLevel = methodsByLevel;
        }

        internal int LevelCount => _methodsByLevel.Length;

        internal IReadOnlyList<MethodDesc> GetMethodsAtLevel(int level)
        {
            return _methodsByLevel[level];
        }

        internal static int[] ComputeLevelsForTest(int[][] adjacency)
        {
            int[] components = ComputeStronglyConnectedComponents(adjacency, out int componentCount);
            int[] componentLevels = ComputeComponentLevels(adjacency, components, componentCount);
            int[] levels = new int[adjacency.Length];
            for (int i = 0; i < levels.Length; i++)
            {
                levels[i] = componentLevels[components[i]];
            }

            return levels;
        }

        internal static ReadyToRunCompilationPlan Create(
            List<MethodDesc> methods,
            ConcurrentDictionary<MethodDesc, ConcurrentDictionary<MethodDesc, byte>> edges)
        {
            methods.Sort(TypeSystemComparer.Instance.Compare);

            var methodIndices = new Dictionary<MethodDesc, int>(methods.Count);
            for (int i = 0; i < methods.Count; i++)
            {
                methodIndices.Add(methods[i], i);
            }

            int[][] adjacency = new int[methods.Count][];
            for (int i = 0; i < methods.Count; i++)
            {
                MethodDesc method = methods[i];
                if (!edges.TryGetValue(method, out ConcurrentDictionary<MethodDesc, byte> callees))
                {
                    adjacency[i] = Array.Empty<int>();
                    continue;
                }

                var calleeIndices = new List<int>(callees.Count);
                foreach (MethodDesc callee in callees.Keys)
                {
                    if (methodIndices.TryGetValue(callee, out int calleeIndex))
                    {
                        calleeIndices.Add(calleeIndex);
                    }
                }

                calleeIndices.Sort();
                adjacency[i] = calleeIndices.ToArray();
            }

            int[] components = ComputeStronglyConnectedComponents(adjacency, out int componentCount);
            int[] componentLevels = ComputeComponentLevels(adjacency, components, componentCount);

            int maxLevel = 0;
            for (int i = 0; i < componentLevels.Length; i++)
            {
                maxLevel = Math.Max(maxLevel, componentLevels[i]);
            }

            var methodsByLevel = new List<MethodDesc>[maxLevel + 1];
            for (int i = 0; i < methodsByLevel.Length; i++)
            {
                methodsByLevel[i] = new List<MethodDesc>();
            }

            for (int i = 0; i < methods.Count; i++)
            {
                int level = componentLevels[components[i]];
                methodsByLevel[level].Add(methods[i]);
            }

            var resultLevels = new MethodDesc[methodsByLevel.Length][];
            for (int i = 0; i < methodsByLevel.Length; i++)
            {
                resultLevels[i] = methodsByLevel[i].ToArray();
            }

            return new ReadyToRunCompilationPlan(resultLevels);
        }

        private static int[] ComputeStronglyConnectedComponents(int[][] adjacency, out int componentCount)
        {
            int currentIndex = 0;
            int[] indices = new int[adjacency.Length];
            int[] lowLinks = new int[adjacency.Length];
            int[] components = new int[adjacency.Length];
            bool[] onStack = new bool[adjacency.Length];
            Array.Fill(indices, -1);
            Array.Fill(components, -1);

            var inProgress = new Stack<int>();
            var traversal = new List<TraversalFrame>();
            componentCount = 0;

            for (int root = 0; root < adjacency.Length; root++)
            {
                if (indices[root] != -1)
                {
                    continue;
                }

                InitializeVertex(root);
                traversal.Add(new TraversalFrame(root));

                while (traversal.Count > 0)
                {
                    int frameIndex = traversal.Count - 1;
                    TraversalFrame frame = traversal[frameIndex];
                    int vertex = frame.Vertex;

                    if (frame.NextEdge < adjacency[vertex].Length)
                    {
                        int destination = adjacency[vertex][frame.NextEdge];
                        frame.NextEdge++;
                        traversal[frameIndex] = frame;

                        if (indices[destination] == -1)
                        {
                            InitializeVertex(destination);
                            traversal.Add(new TraversalFrame(destination));
                        }
                        else if (onStack[destination])
                        {
                            lowLinks[vertex] = Math.Min(lowLinks[vertex], indices[destination]);
                        }

                        continue;
                    }

                    traversal.RemoveAt(frameIndex);
                    if (traversal.Count > 0)
                    {
                        int parent = traversal[traversal.Count - 1].Vertex;
                        lowLinks[parent] = Math.Min(lowLinks[parent], lowLinks[vertex]);
                    }

                    if (lowLinks[vertex] == indices[vertex])
                    {
                        int componentVertex;
                        do
                        {
                            componentVertex = inProgress.Pop();
                            onStack[componentVertex] = false;
                            components[componentVertex] = componentCount;
                        }
                        while (componentVertex != vertex);

                        componentCount++;
                    }
                }
            }

            return components;

            void InitializeVertex(int vertex)
            {
                indices[vertex] = currentIndex;
                lowLinks[vertex] = currentIndex;
                currentIndex++;
                inProgress.Push(vertex);
                onStack[vertex] = true;
            }
        }

        private static int[] ComputeComponentLevels(int[][] adjacency, int[] components, int componentCount)
        {
            var outgoing = new HashSet<int>[componentCount];
            var incoming = new List<int>[componentCount];
            for (int i = 0; i < componentCount; i++)
            {
                outgoing[i] = new HashSet<int>();
                incoming[i] = new List<int>();
            }

            for (int caller = 0; caller < adjacency.Length; caller++)
            {
                int callerComponent = components[caller];
                foreach (int callee in adjacency[caller])
                {
                    int calleeComponent = components[callee];
                    if (callerComponent != calleeComponent && outgoing[callerComponent].Add(calleeComponent))
                    {
                        incoming[calleeComponent].Add(callerComponent);
                    }
                }
            }

            int[] remainingCallees = new int[componentCount];
            int[] levels = new int[componentCount];
            var ready = new Queue<int>();
            for (int i = 0; i < componentCount; i++)
            {
                remainingCallees[i] = outgoing[i].Count;
                if (remainingCallees[i] == 0)
                {
                    ready.Enqueue(i);
                }
            }

            int processed = 0;
            while (ready.Count > 0)
            {
                int callee = ready.Dequeue();
                processed++;

                foreach (int caller in incoming[callee])
                {
                    levels[caller] = Math.Max(levels[caller], levels[callee] + 1);
                    remainingCallees[caller]--;
                    if (remainingCallees[caller] == 0)
                    {
                        ready.Enqueue(caller);
                    }
                }
            }

            Debug.Assert(processed == componentCount);
            return levels;
        }

        private struct TraversalFrame
        {
            public TraversalFrame(int vertex)
            {
                Vertex = vertex;
                NextEdge = 0;
            }

            public int Vertex;
            public int NextEdge;
        }
    }
}
