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
        public ReadyToRunMethodCompilationInfo(int codeSize, int level, bool isEager = false)
        {
            CodeSize = codeSize;
            Level = level;
            IsEager = isEager;
        }

        public int CodeSize { get; }
        public int Level { get; }
        public bool IsEager { get; }
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
        private readonly MethodDesc[] _methods;
        private readonly int[][] _adjacency;

        private ReadyToRunCompilationPlan(MethodDesc[] methods, int[][] adjacency, bool includeColdCalls)
        {
            _methods = methods;
            _adjacency = adjacency;
            _methodsByLevel = ComputeMethodsByLevel(methods, adjacency, includeColdCalls);
        }

        internal int LevelCount => _methodsByLevel.Length;

        internal IReadOnlyList<MethodDesc> GetMethodsAtLevel(int level)
        {
            return _methodsByLevel[level];
        }

        internal ReadyToRunCompilationPlan CreateEagerPlan(HashSet<MethodDesc> roots)
        {
            bool[] included = new bool[_methods.Length];
            var pending = new Stack<int>();
            for (int i = 0; i < _methods.Length; i++)
            {
                if (roots.Contains(_methods[i]))
                {
                    pending.Push(i);
                }
            }

            while (pending.TryPop(out int method))
            {
                if (included[method])
                {
                    continue;
                }

                included[method] = true;
                foreach (int callee in _adjacency[method])
                {
                    pending.Push(callee);
                }
            }

            return CreateSubset(included, includeColdCalls: true);
        }

        internal ReadyToRunCompilationPlan CreateNonEagerPlan(HashSet<MethodDesc> compiledEagerMethods)
        {
            bool[] included = new bool[_methods.Length];
            for (int i = 0; i < _methods.Length; i++)
            {
                included[i] = !compiledEagerMethods.Contains(_methods[i]);
            }

            return CreateSubset(included, includeColdCalls: false);
        }

        private ReadyToRunCompilationPlan CreateSubset(bool[] included, bool includeColdCalls)
        {
            var methods = new List<MethodDesc>();
            int[] indices = new int[_methods.Length];
            Array.Fill(indices, -1);
            for (int i = 0; i < _methods.Length; i++)
            {
                if (included[i])
                {
                    indices[i] = methods.Count;
                    methods.Add(_methods[i]);
                }
            }

            int[][] adjacency = new int[methods.Count][];
            for (int i = 0; i < _methods.Length; i++)
            {
                if (!included[i])
                {
                    continue;
                }

                var callees = new List<int>();
                foreach (int callee in _adjacency[i])
                {
                    if (included[callee])
                    {
                        callees.Add(indices[callee]);
                    }
                }

                adjacency[indices[i]] = callees.ToArray();
            }

            return new ReadyToRunCompilationPlan(methods.ToArray(), adjacency, includeColdCalls);
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

            return new ReadyToRunCompilationPlan(methods.ToArray(), adjacency, includeColdCalls: false);
        }

        private static MethodDesc[][] ComputeMethodsByLevel(MethodDesc[] methods, int[][] adjacency, bool includeColdCalls)
        {
            if (!includeColdCalls)
            {
                int[][] hotAdjacency = new int[adjacency.Length][];
                for (int i = 0; i < adjacency.Length; i++)
                {
                    var callees = new List<int>();
                    foreach (int callee in adjacency[i])
                    {
                        if (!methods[callee].IsNoInlining && !methods[callee].IsNoOptimization)
                        {
                            callees.Add(callee);
                        }
                    }

                    hotAdjacency[i] = callees.ToArray();
                }

                adjacency = hotAdjacency;
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

            for (int i = 0; i < methods.Length; i++)
            {
                int level = componentLevels[components[i]];
                methodsByLevel[level].Add(methods[i]);
            }

            var resultLevels = new MethodDesc[methodsByLevel.Length][];
            for (int i = 0; i < methodsByLevel.Length; i++)
            {
                resultLevels[i] = methodsByLevel[i].ToArray();
            }

            return resultLevels;
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
