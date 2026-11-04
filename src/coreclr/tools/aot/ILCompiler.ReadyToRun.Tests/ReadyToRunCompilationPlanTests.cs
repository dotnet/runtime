// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias crossgen2;

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using ReadyToRunCompilationPlan = crossgen2::ILCompiler.ReadyToRunCompilationPlan;

using Xunit;

namespace ILCompiler.ReadyToRun.Tests;

public class ReadyToRunCompilationPlanTests
{
    public static IEnumerable<object[]> CallGraphs()
    {
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
