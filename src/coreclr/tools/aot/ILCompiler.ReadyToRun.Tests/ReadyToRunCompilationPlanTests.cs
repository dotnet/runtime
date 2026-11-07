// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias crossgen2;

using System;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using ReadyToRunCompilationPlan = crossgen2::ILCompiler.ReadyToRunCompilationPlan;

using Xunit;

namespace ILCompiler.ReadyToRun.Tests;

public class ReadyToRunCompilationPlanTests
{
    public static IEnumerable<object[]> CallGraphs()
    {
        yield return [Array.Empty<int[]>(), Array.Empty<int>()];
        yield return [new int[][] { [0, 0] }, new[] { 0 }];
        yield return [new int[][] { [1, 1, 2], [0, 2, 2], [] }, new[] { 1, 1, 0 }];
        yield return [new int[][] { [], [0], [1, 0], [4], [3, 2] }, new[] { 0, 1, 2, 3, 3 }];
        yield return [new int[][] { [1, 2], [0, 3], [4, 4], [3], [2, 3] }, new[] { 2, 2, 1, 0, 1 }];
        yield return
        [
            new int[][]
            {
                [1],
                [2],
                [],
            },
            new[] { 2, 1, 0 },
        ];

        yield return
        [
            new int[][]
            {
                [1],
                [0, 2],
                [],
            },
            new[] { 1, 1, 0 },
        ];

        yield return
        [
            new int[][]
            {
                [1, 2],
                [3],
                [3],
                [],
                [],
            },
            new[] { 2, 1, 1, 0, 0 },
        ];
    }

    [Theory]
    [MemberData(nameof(CallGraphs))]
    public void ComputesCalleeFirstLevels(int[][] adjacency, int[] expectedLevels)
    {
        Assert.Equal(expectedLevels, ReadyToRunCompilationPlan.ComputeLevelsForTest(adjacency));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(16)]
    public void LevelsMatchIndependentReachabilityReference(int methodCount)
    {
        Random random = new(42 + methodCount);
        for (int sample = 0; sample < 256; sample++)
        {
            int[][] adjacency = new int[methodCount][];
            for (int caller = 0; caller < methodCount; caller++)
            {
                List<int> callees = new();
                for (int callee = 0; callee < methodCount; callee++)
                {
                    if (random.Next(100) < 5 + sample % 5 * 20)
                    {
                        callees.Add(callee);
                        if (random.Next(4) == 0)
                        {
                            callees.Add(callee);
                        }
                    }
                }
                adjacency[caller] = callees.ToArray();
            }

            Assert.Equal(ComputeReferenceLevels(adjacency), ReadyToRunCompilationPlan.ComputeLevelsForTest(adjacency));
        }
    }

    [Fact]
    public void LongCallChainDoesNotRequireRecursiveTraversal()
    {
        const int MethodCount = 20_000;
        int[][] adjacency = new int[MethodCount][];
        for (int i = 0; i < MethodCount; i++)
        {
            adjacency[i] = i + 1 == MethodCount ? [] : [i + 1];
        }
        int[] levels = ReadyToRunCompilationPlan.ComputeLevelsForTest(adjacency);

        for (int i = 0; i < MethodCount; i++)
        {
            Assert.Equal(MethodCount - i - 1, levels[i]);
        }
    }

    private static int[] ComputeReferenceLevels(int[][] adjacency)
    {
        int count = adjacency.Length;
        bool[,] reachable = new bool[count, count];
        for (int caller = 0; caller < count; caller++)
        {
            reachable[caller, caller] = true;
            foreach (int callee in adjacency[caller])
            {
                reachable[caller, callee] = true;
            }
        }
        for (int intermediate = 0; intermediate < count; intermediate++)
        {
            for (int caller = 0; caller < count; caller++)
            {
                for (int callee = 0; callee < count; callee++)
                {
                    reachable[caller, callee] |= reachable[caller, intermediate] && reachable[intermediate, callee];
                }
            }
        }

        int[] components = new int[count];
        for (int method = 0; method < count; method++)
        {
            for (int representative = 0; representative <= method; representative++)
            {
                if (reachable[method, representative] && reachable[representative, method])
                {
                    components[method] = representative;
                    break;
                }
            }
        }
        int[] componentLevels = new int[count];
        for (int iteration = 0; iteration < count; iteration++)
        {
            for (int caller = 0; caller < count; caller++)
            {
                foreach (int callee in adjacency[caller])
                {
                    if (components[caller] != components[callee])
                    {
                        componentLevels[components[caller]] = Math.Max(
                            componentLevels[components[caller]], componentLevels[components[callee]] + 1);
                    }
                }
            }
        }
        int[] levels = new int[count];
        for (int method = 0; method < count; method++)
        {
            levels[method] = componentLevels[components[method]];
        }

        return levels;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void QuiescenceResumesDependencyAnalysisWithoutAdvancingPhase(int phase)
    {
        var context = new TrackingContext();
        var graph = new DependencyAnalyzer<NoLogStrategy<TrackingContext>, TrackingContext>(context, resultSorter: null);
        var root = new TestDependencyNode(phase);
        var late = new TestDependencyNode(0);
        graph.ComputingDependencyPhaseChange += nextPhase => context.Phase = nextPhase;
        graph.ComputeDependencyRoutine += nodes =>
        {
            foreach (TestDependencyNode node in nodes)
            {
                node.Computed = true;
            }
        };
        int checkpoints = 0;
        graph.DependencyAnalysisQuiesced += () =>
        {
            checkpoints++;
            if (checkpoints == 1)
            {
                Assert.Equal(phase, context.Phase);
                root.Dependencies.Add(late);
                graph.RevisitStaticDependencies(root);
            }
        };
        graph.AddRoot(root, "Test root");
        graph.ComputeMarkedNodes();

        Assert.Equal(2, checkpoints);
        Assert.Equal(phase, late.MarkedPhase);
        Assert.Equal(2, graph.MarkedNodeList.Length);
        Assert.Contains(late, graph.MarkedNodeList);
        graph.ComputeMarkedNodes();
        Assert.Equal(2, checkpoints);
    }

    private sealed class TrackingContext
    {
        internal int Phase;
    }

    private sealed class TestDependencyNode(int phase) : DependencyNodeCore<TrackingContext>
    {
        internal readonly List<TestDependencyNode> Dependencies = new();
        internal bool Computed;
        internal int MarkedPhase = -1;
        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => Computed;
        public override int DependencyPhaseForDeferredStaticComputation => phase;
        protected override string GetName(TrackingContext context) => "Deferred dependency test node";
        protected override void OnMarked(TrackingContext context) => MarkedPhase = context.Phase;
        public override void AddStaticDependencies(DependencySink sink, TrackingContext context)
        {
            foreach (TestDependencyNode dependency in Dependencies)
            {
                sink.Add(dependency, "Published dependency");
            }
        }
        public override void SearchDynamicDependencies(List<DependencyNodeCore<TrackingContext>> nodes, int firstNode, DependencySink sink, TrackingContext context) { }
    }
}
