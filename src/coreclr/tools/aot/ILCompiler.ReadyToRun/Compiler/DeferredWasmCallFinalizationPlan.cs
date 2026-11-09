// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.ReadyToRun;

using Internal.TypeSystem;

namespace ILCompiler
{
    // Order deferred-call finalization using the emitted call graph. Recursive
    // components share a level; their callees occupy lower levels. The eager
    // subset includes cold callees, while the non-eager remainder is releveled
    // after eager methods become available. See #DeferredWasmCallFinalization.
    internal sealed class DeferredWasmCallFinalizationPlan
    {
        private readonly MethodDesc[][] _methodsByLevel;
        private readonly MethodDesc[] _methods;
        private readonly int[][] _adjacency;

        private DeferredWasmCallFinalizationPlan(MethodDesc[] methods, int[][] adjacency, bool includeColdCalls)
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

        internal DeferredWasmCallFinalizationPlan CreateEagerPlan(HashSet<MethodDesc> roots)
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

        internal DeferredWasmCallFinalizationPlan CreateNonEagerPlan(HashSet<MethodDesc> finalizedEagerMethods)
        {
            bool[] included = new bool[_methods.Length];
            for (int i = 0; i < _methods.Length; i++)
            {
                included[i] = !finalizedEagerMethods.Contains(_methods[i]);
            }

            return CreateSubset(included, includeColdCalls: false);
        }

        private DeferredWasmCallFinalizationPlan CreateSubset(bool[] included, bool includeColdCalls)
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

            return new DeferredWasmCallFinalizationPlan(methods.ToArray(), adjacency, includeColdCalls);
        }

        internal static int[] ComputeLevelsForTest(int[][] adjacency)
        {
            int[] components = ComputeStronglyConnectedComponents(adjacency, out int[] componentLevels);
            int[] levels = new int[adjacency.Length];
            for (int i = 0; i < levels.Length; i++)
            {
                levels[i] = componentLevels[components[i]];
            }

            return levels;
        }

        internal static DeferredWasmCallFinalizationPlan Create(List<MethodWithGCInfo> methodNodes, NodeFactory factory)
        {
            methodNodes.Sort(static (left, right) => TypeSystemComparer.Instance.Compare(left.Method, right.Method));

            MethodDesc[] methods = new MethodDesc[methodNodes.Count];
            var methodIndices = new Dictionary<MethodDesc, int>(methods.Length);
            for (int i = 0; i < methods.Length; i++)
            {
                methods[i] = methodNodes[i].Method;
                methodIndices.Add(methods[i], i);
            }

            int[][] adjacency = new int[methods.Length][];
            var calleeIndices = new HashSet<int>();
            for (int i = 0; i < methods.Length; i++)
            {
                calleeIndices.Clear();
                foreach (Relocation relocation in methodNodes[i].GetData(factory, relocsOnly: false).Relocs.AsSpan())
                {
                    if (relocation.RelocType != RelocType.WASM_FUNCTION_INDEX_LEB)
                    {
                        continue;
                    }

                    MethodWithGCInfo target = relocation.Target switch
                    {
                        DeferredWasmCallNode call => call.Target,
                        MethodWithGCInfo method => method,
                        _ => null,
                    };
                    if (target is not null && methodIndices.TryGetValue(target.Method, out int calleeIndex))
                    {
                        calleeIndices.Add(calleeIndex);
                    }
                }

                int[] callees = calleeIndices.Count == 0 ? Array.Empty<int>() : new int[calleeIndices.Count];
                calleeIndices.CopyTo(callees);
                Array.Sort(callees);
                adjacency[i] = callees;
            }

            return new DeferredWasmCallFinalizationPlan(methods, adjacency, includeColdCalls: false);
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

            int[] components = ComputeStronglyConnectedComponents(adjacency, out int[] componentLevels);

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

        private static int[] ComputeStronglyConnectedComponents(int[][] adjacency, out int[] componentLevels)
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
            var levels = new List<int>();
            var members = new List<int>();

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
                        int component = levels.Count;
                        members.Clear();
                        int componentVertex;
                        do
                        {
                            componentVertex = inProgress.Pop();
                            onStack[componentVertex] = false;
                            components[componentVertex] = component;
                            members.Add(componentVertex);
                        }
                        while (componentVertex != vertex);

                        // Tarjan completes external callees before callers. Label every member
                        // before scanning edges so internal calls do not contribute to the level.
                        int level = 0;
                        foreach (int member in members)
                        {
                            foreach (int callee in adjacency[member])
                            {
                                int calleeComponent = components[callee];
                                Debug.Assert(calleeComponent >= 0 && calleeComponent <= component);
                                if (calleeComponent != component)
                                {
                                    level = Math.Max(level, levels[calleeComponent] + 1);
                                }
                            }
                        }
                        levels.Add(level);
                    }
                }
            }

            componentLevels = levels.ToArray();
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
