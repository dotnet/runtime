// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using SourceGenerators.Tests;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    public class ForwardDataFlowTests
    {
        private static ControlFlowGraphProxy CreateGraph(string body)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText($$"""
                using System;
                class C
                {
                    static void Marker(string name) { }
                    static bool Check(string name) => false;
                    static void M(bool condition) { {{body}} }
                }
                """);
            CSharpCompilation compilation = CSharpCompilation.Create("FlowTests", new[] { tree }, LiveReferencePack.GetMetadataReferences(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
            SemanticModel model = compilation.GetSemanticModel(tree);
            MethodDeclarationSyntax method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Single(node => node.Identifier.ValueText == "M");
            var operation = (IMethodBodyOperation)model.GetOperation(method)!;
            var graph = ControlFlowGraph.Create(operation);
            Assert.InRange(graph.Blocks.Length, 1, 64);
            return new ControlFlowGraphProxy(graph);
        }

        private static BlockProxy FindMarker(ControlFlowGraphProxy graph, string marker)
        {
            string text = $"\"{marker}\"";
            return Assert.Single(graph.Blocks.Where(block =>
                block.Block.Operations.Any(operation => operation.Syntax.ToString().Contains(text)) ||
                (block.Block.BranchValue?.Syntax.ToString().Contains(text) ?? false)));
        }

        private static ulong Bit(BlockProxy block) => 1UL << block.Block.Ordinal;

        internal static object CreateCompilationState() => Activator.CreateInstance(
            typeof(ControlFlowGraphProxy).Assembly.GetType("ILLink.RoslynAnalyzer.AnalyzerCompilationState", throwOnError: true)!, nonPublic: true)!;

        internal static Func<ControlFlowGraph, ControlFlowGraphProxy> CreateGraphCache(object? state = null)
        {
            state ??= CreateCompilationState();
            return (Func<ControlFlowGraph, ControlFlowGraphProxy>)state.GetType().GetMethod("GetControlFlowGraph")!
                .CreateDelegate(typeof(Func<ControlFlowGraph, ControlFlowGraphProxy>), state);
        }

        [Fact]
        public void CompilerMetadataCachesAreNotStatic()
        {
            Type hierarchy = typeof(ControlFlowGraphProxy).Assembly.GetType("ILLink.RoslynAnalyzer.ITypeSymbolExtensions", throwOnError: true)!;
            Assert.Empty(hierarchy.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
            Assert.Empty(typeof(ControlFlowGraphProxy).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
            Type owner = typeof(ControlFlowGraphProxy).Assembly.GetType("ILLink.RoslynAnalyzer.AnalyzerCompilationState", throwOnError: true)!;
            Type annotations = typeof(ControlFlowGraphProxy).Assembly.GetType("ILLink.Shared.TrimAnalysis.FlowAnnotations", throwOnError: true)!;
            Assert.Empty(owner.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
            Assert.Empty(annotations.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
        }

        [Fact]
        public void DisabledAnalysisDoesNotCreateCompilationCaches()
        {
            Type contextType = typeof(ControlFlowGraphProxy).Assembly.GetType("ILLink.RoslynAnalyzer.DataFlowAnalyzerContext", throwOnError: true)!;
            MethodInfo create = contextType.GetMethod("Create")!;
            object analyzers = create.GetParameters()[2].ParameterType.GetField("Empty")!.GetValue(null)!;
            var compilation = CSharpCompilation.Create("DisabledCacheTests", references: LiveReferencePack.GetMetadataReferences());
            object context = create.Invoke(null, new object[] { new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty), compilation, analyzers })!;

            Assert.False((bool)contextType.GetProperty("AnyAnalyzersEnabled")!.GetValue(context)!);
            Assert.Null(contextType.GetField("_compilationState", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(context));
        }

        [Fact]
        public void CompilationCachesPublishIndependentGraphsUnderConcurrency()
        {
            ControlFlowGraph cfg = CreateGraph("Marker(condition ? \"left\" : \"right\");").ControlFlowGraph;
            var getGraph = CreateGraphCache();
            var graphs = new ControlFlowGraphProxy[64];
            Parallel.For(0, graphs.Length, i => graphs[i] = getGraph(cfg));
            var expected = graphs[0].GetSuccessors(graphs[0].Entry);
            foreach (var graph in graphs)
                Assert.Same(expected, graph.GetSuccessors(graph.Entry));
            GC.Collect();
            Assert.Same(expected, getGraph(cfg).GetSuccessors(graphs[0].Entry));

            ControlFlowGraphProxy separate = CreateGraphCache()(cfg);
            Assert.NotSame(expected, separate.GetSuccessors(separate.Entry));
        }

        [Fact]
        public void KeepingCompilationStateAliveDoesNotKeepUnusedGraphs()
        {
            object owner = CreateCompilationState();
            WeakReference weak = CreateWeakGraph(owner);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Assert.False(weak.IsAlive);
            GC.KeepAlive(owner);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static WeakReference CreateWeakGraph(object owner)
            {
                ControlFlowGraph graph = CreateGraph("Marker(\"body\");").ControlFlowGraph;
                CreateGraphCache(owner)(graph);
                return new WeakReference(graph);
            }
        }

        [Fact]
        public void DroppingCompilationStateReleasesAllCompilerData()
        {
            (WeakReference owner, WeakReference graph, WeakReference compilation) = CreateWeakState();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Assert.False(owner.IsAlive);
            Assert.False(graph.IsAlive);
            Assert.False(compilation.IsAlive);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static (WeakReference Owner, WeakReference Graph, WeakReference Compilation) CreateWeakState()
            {
                object owner = CreateCompilationState();
                ControlFlowGraphProxy graph = CreateGraphCache(owner)(CreateGraph("Marker(condition ? \"left\" : \"right\");").ControlFlowGraph);
                Compilation compilation = graph.ControlFlowGraph.OriginalOperation.SemanticModel!.Compilation;
                var classify = (Func<ITypeSymbol, bool, bool>)owner.GetType().GetMethod("IsTypeInterestingForDataflow")!
                    .CreateDelegate(typeof(Func<ITypeSymbol, bool, bool>), owner);
                classify(compilation.GetTypeByMetadataName("C")!, false);
                return (new WeakReference(owner), new WeakReference(graph.ControlFlowGraph), new WeakReference(compilation));
            }
        }

        [Theory]
        [InlineData("string[] values = new string[1]; values[0] = condition ? \"left\" : \"right\"; Marker(values[0]);", FlowCaptureKind.LValueCapture)]
        [InlineData("string[] values = new string[1]; values[0] += condition ? \"left\" : \"right\"; Marker(values[0]);", FlowCaptureKind.LValueAndRValueCapture)]
        public void CaptureClassificationReusesImmutablePerGraphResults(string body, FlowCaptureKind kind)
        {
            ControlFlowGraph cfg = CreateGraph(body).ControlFlowGraph;
            var getGraph = CreateGraphCache();
            var property = typeof(ControlFlowGraphProxy).GetProperty("LValueFlowCaptures", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var first = (ImmutableDictionary<CaptureId, FlowCaptureKind>)property.GetValue(getGraph(cfg))!;
            var second = (ImmutableDictionary<CaptureId, FlowCaptureKind>)property.GetValue(getGraph(cfg))!;
            Assert.Contains(kind, first.Values);

            long before = GC.GetAllocatedBytesForCurrentThread();
            ControlFlowGraphProxy repeated = getGraph(cfg);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.Same(first, second);
            Assert.Same(first, property.GetValue(repeated));
            Assert.Equal(0, allocated);
        }

        private delegate bool IsInDeconstruction(IOperation operation, out IDeconstructionAssignmentOperation? assignment);

        [Theory]
        [InlineData("string[] values = new string[2]; (values[0], values[1]) = (condition ? \"a\" : \"b\", \"c\"); Marker(values[0]);", true)]
        [InlineData("string[] values = new string[1]; values[0] = condition ? \"a\" : \"b\"; Marker(values[0]);", false)]
        public void CachedDeconstructionClassificationMatchesReferenceWalk(string body, bool hasDeconstruction)
        {
            var getGraph = CreateGraphCache();
            ControlFlowGraphProxy graph = getGraph(CreateGraph(body).ControlFlowGraph);
            Type graphExtensions = typeof(ControlFlowGraphProxy).Assembly.GetType("ILLink.RoslynAnalyzer.DataFlow.ControlFlowGraphExtensions", throwOnError: true)!;
            MethodInfo descendantsMethod = graphExtensions.GetMethods().Single(method => method.Name == "DescendantOperations" && method.IsGenericMethod)
                .MakeGenericMethod(typeof(IFlowCaptureReferenceOperation));
            var descendants = (Func<ControlFlowGraph, OperationKind, IEnumerable<IFlowCaptureReferenceOperation>>)descendantsMethod
                .CreateDelegate(typeof(Func<ControlFlowGraph, OperationKind, IEnumerable<IFlowCaptureReferenceOperation>>));
            Type operationExtensions = typeof(ControlFlowGraphProxy).Assembly.GetType("ILLink.RoslynAnalyzer.IOperationExtensions", throwOnError: true)!;
            var isInDeconstruction = (IsInDeconstruction)operationExtensions.GetMethod("IsInLeftOfDeconstructionAssignment")!
                .CreateDelegate(typeof(IsInDeconstruction));
            ImmutableHashSet<CaptureId> expected = descendants(graph.ControlFlowGraph, OperationKind.FlowCaptureReference)
                .Where(reference => isInDeconstruction(reference, out _)).Select(reference => reference.Id).ToImmutableHashSet();
            var property = typeof(ControlFlowGraphProxy).GetProperty("DeconstructionLValueFlowCaptures", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var actual = (ImmutableHashSet<CaptureId>)property.GetValue(graph)!;

            Assert.Equal(hasDeconstruction, !expected.IsEmpty);
            Assert.True(expected.SetEquals(actual));
            Assert.Same(actual, property.GetValue(getGraph(graph.ControlFlowGraph)));
        }

        [Theory]
        [InlineData("{ string scoped = \"value\"; Marker(scoped); } Marker(\"end\");")]
        [InlineData("{ string scoped = \"value\"; try { Marker(\"try\"); } finally { Marker(scoped); } } Marker(\"end\");")]
        [InlineData("for (int scoped = 0; scoped < 2; scoped++) { Marker(\"loop\"); } Marker(\"end\");")]
        public void ScopeExitIndexIncludesOnlyRegionsOutsideTheDestination(string body)
        {
            var getGraph = CreateGraphCache();
            ControlFlowGraphProxy graph = getGraph(CreateGraph(body).ControlFlowGraph);
            var exits = (IReadOnlyDictionary<int, ImmutableArray<ControlFlowRegion>>?)typeof(ControlFlowGraphProxy)
                .GetProperty("ScopeExits", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(graph);
            Assert.NotNull(exits);
            ImmutableArray<ControlFlowRegion> regions = exits[FindMarker(graph, "end").Block.Ordinal];
            Assert.Contains(regions.SelectMany(region => region.Locals), local => local.Name == "scoped");
            foreach (var (ordinal, leavingRegions) in exits)
            {
                foreach (ControlFlowRegion region in leavingRegions)
                    Assert.False(ordinal >= region.FirstBlockOrdinal && ordinal <= region.LastBlockOrdinal);
            }
            Assert.Same(exits, typeof(ControlFlowGraphProxy).GetProperty("ScopeExits", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(getGraph(graph.ControlFlowGraph)));
        }

        [Fact]
        public void EmptyFinallyRegionConversionDoesNotAllocate()
        {
            ControlFlowGraphProxy graph = CreateGraph("Marker(\"body\");");
            Microsoft.CodeAnalysis.FlowAnalysis.ControlFlowBranch branch = graph.Entry.Block.FallThroughSuccessor!;
            ControlFlowGraphProxy.CreateProxyBranch(branch);
            long before = GC.GetAllocatedBytesForCurrentThread();
            var proxy = ControlFlowGraphProxy.CreateProxyBranch(branch);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.Empty(proxy.FinallyRegions);
            Assert.Equal(0, allocated);
        }

        [Theory]
        [InlineData("if (condition) Marker(\"left\"); else Marker(\"right\");")]
        [InlineData("try { try { Marker(\"try\"); } finally { Marker(\"inner\"); } } finally { Marker(\"outer\"); }")]
        public void CachedEdgesPreserveNativeOrderAndFinallyMetadata(string body)
        {
            var getGraph = CreateGraphCache();
            ControlFlowGraphProxy graph = getGraph(CreateGraph(body).ControlFlowGraph);
            foreach (BlockProxy block in graph.Blocks)
            {
                var successors = graph.GetSuccessors(block);
                var predecessors = graph.GetPredecessors(block);
                Assert.Same(successors, getGraph(graph.ControlFlowGraph).GetSuccessors(block));
                Assert.Same(predecessors, graph.GetPredecessors(block));
                var nativeSuccessors = new[] { block.Block.ConditionalSuccessor, block.Block.FallThroughSuccessor }
                    .Where(branch => branch is not null).ToArray();
                Assert.Equal(nativeSuccessors.Length, successors.Count());
                foreach (var (native, proxy) in nativeSuccessors.Zip(successors))
                {
                    Assert.Same(native!.Source, proxy.Source.Block);
                    Assert.Same(native.Destination, proxy.Destination?.Block);
                    Assert.Equal(native.FinallyRegions, proxy.FinallyRegions.Select(region => region.Region));
                    Assert.Equal(ControlFlowGraphProxy.CreateProxyBranch(native).ConditionKind, proxy.ConditionKind);
                }
                Assert.Equal(block.Block.Predecessors.Select(branch => branch.Source.Ordinal), predecessors.Select(branch => branch.Source.Block.Ordinal));
            }
        }

        [Theory]
        [InlineData("Marker(\"body\");")]
        [InlineData("{ string scoped = \"value\"; Marker(scoped); } Marker(\"end\");")]
        [InlineData("Marker(condition ? \"left\" : \"right\");")]
        public void CachedEdgesDoNotKeepGraphsAlive(string body)
        {
            WeakReference weak = CreateWeakGraph(body);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Assert.False(weak.IsAlive);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static WeakReference CreateWeakGraph(string body)
            {
                ControlFlowGraphProxy graph = CreateGraph(body);
                graph.GetSuccessors(graph.Entry).ToArray();
                return new WeakReference(graph.ControlFlowGraph);
            }
        }

        [Fact]
        public void AcyclicBlocksAreTransferredOnce()
        {
            ControlFlowGraphProxy graph = CreateGraph("if (condition) Marker(\"left\"); else Marker(\"right\"); Marker(\"end\");");
            var transfer = new TransferFunctions();

            Assert.True(new Analysis().Fixpoint(graph, transfer));

            foreach (BlockProxy block in graph.Blocks.Where(block => !block.Equals(graph.Entry)))
                Assert.Equal(1, transfer.Visits[block.Block.Ordinal]);
            BlockProxy end = FindMarker(graph, "end");
            ulong expected = Bit(FindMarker(graph, "left")) | Bit(FindMarker(graph, "right"));
            Assert.Equal(expected, transfer.Inputs[end.Block.Ordinal].Bits & expected);
        }

        [Theory]
        [InlineData("try { Marker(\"try\"); } finally { Marker(\"finally\"); } Marker(\"end\");")]
        [InlineData("try { try { Marker(\"try\"); } finally { Marker(\"nested\"); } } finally { Marker(\"finally\"); } Marker(\"end\");")]
        public void FinallyInputsAndExitsRescheduleDependentBlocks(string body)
        {
            ControlFlowGraphProxy graph = CreateGraph(body);
            var transfer = new TransferFunctions();

            Assert.True(new Analysis().Fixpoint(graph, transfer));

            BlockProxy end = FindMarker(graph, "end");
            ulong expected = Bit(FindMarker(graph, "try")) | Bit(FindMarker(graph, "finally"));
            if (body.Contains("\"nested\""))
                expected |= Bit(FindMarker(graph, "nested"));
            Assert.Equal(expected, transfer.Inputs[end.Block.Ordinal].Bits & expected);
        }

        [Theory]
        [InlineData("do { try { Marker(\"try\"); } catch { Marker(\"handler\"); } Marker(\"late\"); } while (condition);", "handler")]
        [InlineData("do { try { Marker(\"try\"); } catch (Exception) when (Check(\"filter\")) { Marker(\"handler\"); } catch { Marker(\"other\"); } Marker(\"late\"); } while (condition);", "other")]
        [InlineData("do { try { Marker(\"try\"); } catch { Marker(\"handler\"); } finally { Marker(\"finally\"); } Marker(\"late\"); } while (condition);", "handler")]
        [InlineData("do { try { try { Marker(\"try\"); } finally { Marker(\"finally\"); } } catch { Marker(\"handler\"); } Marker(\"late\"); } while (condition);", "handler")]
        public void ExceptionDependenciesPropagateBackEdgeChanges(string body, string handler)
        {
            ControlFlowGraphProxy graph = CreateGraph(body);
            var transfer = new TransferFunctions();

            Assert.True(new Analysis().Fixpoint(graph, transfer));

            ulong expected = Bit(FindMarker(graph, "late"));
            Assert.Equal(expected, transfer.Inputs[FindMarker(graph, handler).Block.Ordinal].Bits & expected);
        }

        [Fact]
        public void ExceptionalFinallyInputsRemainIndependent()
        {
            ControlFlowGraphProxy graph = CreateGraph("try { Marker(\"try\"); throw new Exception(); } finally { Marker(\"finally\"); }");
            var transfer = new TransferFunctions();

            Assert.True(new Analysis().Fixpoint(graph, transfer));

            ulong expected = Bit(FindMarker(graph, "try"));
            Assert.Equal(expected, transfer.Inputs[FindMarker(graph, "finally").Block.Ordinal].Bits & expected);
        }

        [Fact]
        public void ExhaustingTheVisitBudgetDoesNotReportConvergence()
        {
            ControlFlowGraphProxy graph = CreateGraph("while (condition) { Marker(\"body\"); }");
            var transfer = new TransferFunctions { KeepChanging = true };

            Assert.False(new Analysis().Fixpoint(graph, transfer));
            Assert.InRange(transfer.Visits.Values.Sum(), 9_000, 10_000);
        }

        private readonly record struct Facts(ulong Bits, int Generation = 0);

        private readonly struct FactsLattice : ILattice<Facts>
        {
            public Facts Top => default;
            public Facts Meet(Facts left, Facts right) => new(left.Bits | right.Bits, Math.Max(left.Generation, right.Generation));
        }

        private readonly struct NoCondition : INegate<NoCondition>
        {
            public NoCondition Negate() => this;
        }

        private sealed class State : IDataFlowState<Facts, FactsLattice>
        {
            public State() { }
            public Facts Current { get; set; }
            public Box<Facts>? Exception { get; set; }
            public FactsLattice Lattice { get; init; }
            public Facts CreateBranchOutput(bool canTransferOwnership) => Lattice.Meet(Lattice.Top, Current);
        }

        private sealed class TransferFunctions : ITransfer<BlockProxy, Facts, State, FactsLattice, NoCondition>
        {
            public readonly Dictionary<int, int> Visits = new();
            public readonly Dictionary<int, Facts> Inputs = new();
            public bool KeepChanging { get; init; }

            public NoCondition? Transfer(BlockProxy block, State state)
            {
                int ordinal = block.Block.Ordinal;
                Visits.TryGetValue(ordinal, out int visits);
                Visits[ordinal] = visits + 1;
                Inputs.TryGetValue(ordinal, out Facts input);
                Inputs[ordinal] = state.Lattice.Meet(input, state.Current);
                state.Current = new Facts(state.Current.Bits | Bit(block), state.Current.Generation + (KeepChanging ? 1 : 0));
                if (state.Exception is not null)
                    state.Exception.Value = state.Lattice.Meet(state.Exception.Value, state.Current);
                return null;
            }

            public void ApplyCondition(NoCondition condition, ref Facts value) { }
        }

        private sealed class Analysis : ForwardDataFlowAnalysis<
            Facts, State, FactsLattice, BlockProxy, RegionProxy, ControlFlowGraphProxy, TransferFunctions, NoCondition>
        {
            public Analysis() : base(default, new Facts(1)) { }
        }
    }
}
