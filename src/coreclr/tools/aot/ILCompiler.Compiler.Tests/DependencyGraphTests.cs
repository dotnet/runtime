// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.Dataflow;
using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Xunit;

using CustomAttributeValue = System.Reflection.Metadata.CustomAttributeValue<Internal.TypeSystem.TypeDesc>;

namespace ILCompiler.Compiler.Tests
{
    //
    // This test uses IL scanner to scan a dependency graph, starting with a
    // single method from the test assembly.
    // It then checks various invariants about the resulting dependency graph.
    // The test method declares these invariants using custom attributes.
    //
    // The invariants to check for are:
    // * Whether an EEType was/was not generated
    // * Whether a method body was/was not generated
    // * Etc.
    //
    // The most valuable tests are the ones that check that something was not
    // generated. These let us create unit tests for size on disk regressions.
    //

    public class DependencyGraphTests
    {
        public static IEnumerable<object[]> GetTestMethods()
        {
            var target = new TargetDetails(TargetArchitecture.X64, TargetOS.Windows, TargetAbi.NativeAot);
            var context = new CompilerTypeSystemContext(target, SharedGenericsMode.CanonicalReferenceTypes, DelegateFeature.All);

            context.InputFilePaths = new Dictionary<string, string> {
                { "Test.CoreLib", @"Test.CoreLib.dll" },
                { "ILCompiler.Compiler.Tests.Assets", @"ILCompiler.Compiler.Tests.Assets.dll" },
                };
            context.ReferenceFilePaths = new Dictionary<string, string>();

            context.SetSystemModule(context.GetModuleForSimpleName("Test.CoreLib"));
            var testModule = context.GetModuleForSimpleName("ILCompiler.Compiler.Tests.Assets");

            bool foundSomethingToCheck = false;
            foreach (var type in testModule.GetType("ILCompiler.Compiler.Tests.Assets"u8, "DependencyGraph"u8).GetNestedTypes())
            {
                foundSomethingToCheck = true;
                yield return new object[] { type.GetMethod("Entrypoint"u8, null) };
            }

            Assert.True(foundSomethingToCheck, "No methods to check?");
        }

        [Theory]
        [MemberData(nameof(GetTestMethods))]
        public void TestDependencyGraphInvariants(EcmaMethod method)
        {
            //
            // Scan the input method
            //

            var context = (CompilerTypeSystemContext)method.Context;
            CompilationModuleGroup compilationGroup = new SingleFileCompilationModuleGroup();

            NativeAotILProvider ilProvider = new NativeAotILProvider();
            CompilerGeneratedState compilerGeneratedState = new CompilerGeneratedState(ilProvider, Logger.Null, disableGeneratedCodeHeuristics: true);

            UsageBasedMetadataManager metadataManager = new UsageBasedMetadataManager(compilationGroup, context,
                new FullyBlockedMetadataBlockingPolicy(), new FullyBlockedManifestResourceBlockingPolicy(),
                null, new NoStackTraceEmissionPolicy(), new NoDynamicInvokeThunkGenerationPolicy(),
                new ILLink.Shared.TrimAnalysis.FlowAnnotations(Logger.Null, ilProvider, compilerGeneratedState), UsageBasedMetadataGenerationOptions.None,
                default, Logger.Null, new Dictionary<string, bool>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());

            CompilationBuilder builder = new RyuJitCompilationBuilder(context, compilationGroup)
                .UseILProvider(ilProvider);

            IILScanner scanner = builder.GetILScannerBuilder()
                .UseCompilationRoots(new ICompilationRootProvider[] { new SingleMethodRootProvider(method) })
                .UseMetadataManager(metadataManager)
                .ToILScanner();

            ILScanResults results = scanner.Scan();

            //
            // Check invariants
            //

            const string assetsNamespace = "ILCompiler.Compiler.Tests.Assets";
            bool foundSomethingToCheck = false;

            foreach (var attr in method.GetDecodedCustomAttributes(assetsNamespace, "GeneratesConstructedEETypeAttribute"))
            {
                foundSomethingToCheck = true;
                Assert.Contains((TypeDesc)attr.FixedArguments[0].Value, results.ConstructedEETypes);
            }

            foreach (var attr in method.GetDecodedCustomAttributes(assetsNamespace, "NoConstructedEETypeAttribute"))
            {
                foundSomethingToCheck = true;
                Assert.DoesNotContain((TypeDesc)attr.FixedArguments[0].Value, results.ConstructedEETypes);
            }

            foreach (var attr in method.GetDecodedCustomAttributes(assetsNamespace, "GeneratesMethodBodyAttribute"))
            {
                foundSomethingToCheck = true;
                MethodDesc methodToCheck = GetMethodFromAttribute(attr);
                Assert.Contains(methodToCheck.GetCanonMethodTarget(CanonicalFormKind.Specific), results.CompiledMethodBodies);
            }

            foreach (var attr in method.GetDecodedCustomAttributes(assetsNamespace, "NoMethodBodyAttribute"))
            {
                foundSomethingToCheck = true;
                MethodDesc methodToCheck = GetMethodFromAttribute(attr);
                Assert.DoesNotContain(methodToCheck.GetCanonMethodTarget(CanonicalFormKind.Specific), results.CompiledMethodBodies);
            }

            //
            // Make sure we checked something
            //

            Assert.True(foundSomethingToCheck, "No invariants to check?");
        }

        [Fact]
        public void ConditionalDependencyRequiresCondition()
        {
            TestContext context = new TestContext();
            TestNode condition = new TestNode("condition");
            TestNode dependency = new TestNode("dependency");
            TestNode emptySource = new TestNode("empty source");
            TestNode source = new TestNode(
                "source",
                conditionalDependencies: new[] { ConditionalDependency(dependency, condition, "condition") });

            DependencyAnalyzer<TrackingMarkStrategy, TestContext> analyzer = Analyze(context, emptySource, source);

            Assert.True(emptySource.Marked);
            Assert.False(condition.Marked);
            Assert.False(dependency.Marked);
            Assert.DoesNotContain(dependency, analyzer.MarkedNodeList);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ConditionalDependencyMarksForConditionOrder(bool conditionAlreadyMarked)
        {
            TestContext context = new TestContext();
            TestNode condition = new TestNode("condition");
            TestNode dependency = new TestNode("dependency");
            TestNode trigger = new TestNode(
                "trigger",
                dependencies: new[] { StaticDependency(condition, "condition") });
            TestNode source = new TestNode(
                "source",
                dependencies: conditionAlreadyMarked ? null : new[] { StaticDependency(trigger, "trigger") },
                conditionalDependencies: new[] { ConditionalDependency(dependency, condition, "condition") });
            DependencyAnalyzer<TrackingMarkStrategy, TestContext> analyzer =
                new DependencyAnalyzer<TrackingMarkStrategy, TestContext>(context, null);

            if (conditionAlreadyMarked)
            {
                analyzer.AddRoot(condition, "condition root");
            }
            analyzer.AddRoot(source, "source root");
            analyzer.ComputeMarkedNodes();

            Assert.True(condition.Marked);
            Assert.True(dependency.Marked);
            Assert.Equal(1, context.CountMarkAttempts(dependency));
        }

        [Fact]
        public void ConditionalDependenciesDeduplicateAcrossPromotion()
        {
            TestContext context = new TestContext();
            TestNode condition = new TestNode("condition");
            TestNode firstDependency = new TestNode("first dependency");
            TestNode secondDependency = new TestNode("second dependency");
            TestNode thirdDependency = new TestNode("third dependency");
            TestNode trigger = new TestNode(
                "trigger",
                dependencies: new[] { StaticDependency(condition, "condition") });
            string duplicateFirstReason = new string("first".ToCharArray());
            string duplicateSecondReason = new string("second".ToCharArray());
            TestNode source = new TestNode(
                "source",
                dependencies: new[] { StaticDependency(trigger, "trigger") },
                conditionalDependencies: new[]
                {
                    ConditionalDependency(firstDependency, condition, "first"),
                    ConditionalDependency(firstDependency, condition, duplicateFirstReason),
                    ConditionalDependency(secondDependency, condition, "second"),
                    ConditionalDependency(secondDependency, condition, duplicateSecondReason),
                    ConditionalDependency(thirdDependency, condition, "third"),
                });

            Analyze(context, source);

            Assert.Equal(1, context.CountMarkAttempts(firstDependency));
            Assert.Equal(1, context.CountMarkAttempts(secondDependency));
            Assert.Equal(1, context.CountMarkAttempts(thirdDependency));
        }

        [Fact]
        public void ConditionalDependenciesPreserveDistinctOwners()
        {
            TestContext context = new TestContext();
            TestNode condition = new TestNode("condition");
            TestNode dependency = new TestNode("dependency");
            TestNode trigger = new TestNode(
                "trigger",
                dependencies: new[] { StaticDependency(condition, "condition") });
            TestNode firstSource = new TestNode(
                "first source",
                conditionalDependencies: new[] { ConditionalDependency(dependency, condition, "condition") });
            TestNode secondSource = new TestNode(
                "second source",
                conditionalDependencies: new[] { ConditionalDependency(dependency, condition, "condition") });
            TestNode root = new TestNode(
                "root",
                dependencies: new[]
                {
                    StaticDependency(trigger, "trigger"),
                    StaticDependency(firstSource, "first source"),
                    StaticDependency(secondSource, "second source"),
                });

            Analyze(context, root);

            Assert.True(dependency.Marked);
            Assert.Equal(2, context.CountMarkAttempts(dependency));
        }

        [Fact]
        public void ConditionalDependenciesUseConditionIdentity()
        {
            TestContext context = new TestContext();
            TestNode firstCondition = new TestNode("condition");
            TestNode secondCondition = new TestNode("condition");
            TestNode firstDependency = new TestNode("first dependency");
            TestNode secondDependency = new TestNode("second dependency");
            TestNode trigger = new TestNode(
                "trigger",
                dependencies: new[] { StaticDependency(firstCondition, "first condition") });
            TestNode firstSource = new TestNode(
                "first source",
                conditionalDependencies: new[] { ConditionalDependency(firstDependency, firstCondition, "condition") });
            TestNode secondSource = new TestNode(
                "second source",
                conditionalDependencies: new[] { ConditionalDependency(secondDependency, secondCondition, "condition") });
            TestNode root = new TestNode(
                "root",
                dependencies: new[]
                {
                    StaticDependency(trigger, "trigger"),
                    StaticDependency(firstSource, "first source"),
                    StaticDependency(secondSource, "second source"),
                });

            Analyze(context, root);

            Assert.True(firstCondition.Marked);
            Assert.False(secondCondition.Marked);
            Assert.True(firstDependency.Marked);
            Assert.False(secondDependency.Marked);
        }

        [Fact]
        public void ConditionalDependencyDoesNotRemarkDependency()
        {
            TestContext context = new TestContext();
            TestNode condition = new TestNode("condition");
            TestNode dependency = new TestNode("dependency");
            TestNode trigger = new TestNode(
                "trigger",
                dependencies: new[] { StaticDependency(condition, "condition") });
            TestNode source = new TestNode(
                "source",
                dependencies: new[] { StaticDependency(trigger, "trigger") },
                conditionalDependencies: new[] { ConditionalDependency(dependency, condition, "condition") });
            DependencyAnalyzer<TrackingMarkStrategy, TestContext> analyzer =
                new DependencyAnalyzer<TrackingMarkStrategy, TestContext>(context, null);
            int dependencyMarkedCount = 0;
            analyzer.NewMarkedNode += node =>
            {
                if (ReferenceEquals(node, dependency))
                {
                    dependencyMarkedCount++;
                }
            };

            analyzer.AddRoot(dependency, "dependency root");
            analyzer.AddRoot(source, "source root");
            analyzer.ComputeMarkedNodes();

            Assert.Equal(2, context.CountMarkAttempts(dependency));
            Assert.Equal(1, dependencyMarkedCount);
        }

        [Fact]
        public void ConditionalDependencyAllowsNullCondition()
        {
            TestContext context = new TestContext();
            TestNode dependency = new TestNode("dependency");
            TestNode source = new TestNode(
                "source",
                conditionalDependencies: new[] { ConditionalDependency(dependency, null, "unconditional") });

            Analyze(context, source);

            Assert.True(dependency.Marked);
            Assert.Equal(1, context.CountMarkAttempts(dependency));
        }

        [Fact]
        public void DeferredConditionalDependencyReplays()
        {
            TestContext context = new TestContext();
            TestNode condition = new TestNode("condition");
            TestNode dependency = new TestNode("dependency");
            TestNode trigger = new TestNode(
                "trigger",
                dependencies: new[] { StaticDependency(condition, "condition") });
            TestNode source = new TestNode("source", dependenciesComputed: false, dependencyPhase: 2);
            DependencyAnalyzer<TrackingMarkStrategy, TestContext> analyzer =
                new DependencyAnalyzer<TrackingMarkStrategy, TestContext>(context, null);
            int computationCount = 0;
            analyzer.ComputeDependencyRoutine += nodes =>
            {
                if (nodes.Count == 0)
                {
                    return;
                }

                computationCount++;
                Assert.Single(nodes);
                Assert.Same(source, nodes[0]);
                source.SetDependencies(
                    new[] { StaticDependency(trigger, "trigger") },
                    new[] { ConditionalDependency(dependency, condition, "condition") });
            };

            analyzer.AddRoot(source, "source root");
            analyzer.ComputeMarkedNodes();

            Assert.True(condition.Marked);
            Assert.True(dependency.Marked);
            Assert.Equal(1, computationCount);
        }

        [Fact]
        public void SatisfiedConditionalDependencyDoesNotBlockLaterDependency()
        {
            TestContext context = new TestContext();
            TestNode condition = new TestNode("condition");
            TestNode laterDependency = new TestNode("later dependency");
            TestNode laterSource = new TestNode(
                "later source",
                conditionalDependencies: new[] { ConditionalDependency(laterDependency, condition, "later condition") });
            TestNode firstDependency = new TestNode(
                "first dependency",
                dependencies: new[] { StaticDependency(laterSource, "later source") });
            TestNode trigger = new TestNode(
                "trigger",
                dependencies: new[] { StaticDependency(condition, "condition") });
            TestNode source = new TestNode(
                "source",
                dependencies: new[] { StaticDependency(trigger, "trigger") },
                conditionalDependencies: new[] { ConditionalDependency(firstDependency, condition, "first condition") });

            Analyze(context, source);

            Assert.True(firstDependency.Marked);
            Assert.True(laterDependency.Marked);
            Assert.Equal(1, context.CountMarkAttempts(laterDependency));
        }

        [Fact]
        public void ConditionalDependenciesPreserveDistinctReasons()
        {
            TestContext context = new TestContext();
            TestNode condition = new TestNode("condition");
            TestNode dependency = new TestNode("dependency");
            TestNode trigger = new TestNode(
                "trigger",
                dependencies: new[] { StaticDependency(condition, "condition") });
            TestNode source = new TestNode(
                "source",
                dependencies: new[] { StaticDependency(trigger, "trigger") },
                conditionalDependencies: new[]
                {
                    ConditionalDependency(dependency, condition, "first"),
                    ConditionalDependency(dependency, condition, "second"),
                });

            Analyze(context, source);

            string[] reasons = context.GetMarkReasons(dependency);
            Array.Sort(reasons, StringComparer.Ordinal);
            Assert.Equal(new[] { "first", "second" }, reasons);
        }

        private static DependencyAnalyzer<TrackingMarkStrategy, TestContext> Analyze(
            TestContext context,
            params TestNode[] roots)
        {
            DependencyAnalyzer<TrackingMarkStrategy, TestContext> analyzer =
                new DependencyAnalyzer<TrackingMarkStrategy, TestContext>(context, null);
            foreach (TestNode root in roots)
            {
                analyzer.AddRoot(root, "root");
            }
            analyzer.ComputeMarkedNodes();
            return analyzer;
        }

        private static DependencyNodeCore<TestContext>.DependencyListEntry StaticDependency(TestNode node, string reason)
        {
            return new DependencyNodeCore<TestContext>.DependencyListEntry(node, reason);
        }

        private static DependencyNodeCore<TestContext>.CombinedDependencyListEntry ConditionalDependency(
            TestNode node,
            TestNode condition,
            string reason)
        {
            return new DependencyNodeCore<TestContext>.CombinedDependencyListEntry(node, condition, reason);
        }

        private sealed class TestContext
        {
            private readonly List<(DependencyNodeCore<TestContext> Node, string Reason)> _markAttempts =
                new List<(DependencyNodeCore<TestContext>, string)>();

            public void RecordMarkAttempt(DependencyNodeCore<TestContext> node, string reason)
            {
                _markAttempts.Add((node, reason));
            }

            public int CountMarkAttempts(DependencyNodeCore<TestContext> node)
            {
                int count = 0;
                foreach ((DependencyNodeCore<TestContext> attemptedNode, _) in _markAttempts)
                {
                    if (ReferenceEquals(attemptedNode, node))
                    {
                        count++;
                    }
                }
                return count;
            }

            public string[] GetMarkReasons(DependencyNodeCore<TestContext> node)
            {
                List<string> reasons = new List<string>();
                foreach ((DependencyNodeCore<TestContext> attemptedNode, string reason) in _markAttempts)
                {
                    if (ReferenceEquals(attemptedNode, node))
                    {
                        reasons.Add(reason);
                    }
                }
                return reasons.ToArray();
            }
        }

        private struct TrackingMarkStrategy : IDependencyAnalysisMarkStrategy<TestContext>
        {
            private TestContext _context;
            private IDependencyAnalysisMarkStrategy<TestContext> _innerStrategy;

            public void AttachContext(TestContext context)
            {
                _context = context;
                _innerStrategy = new NoLogStrategy<TestContext>();
                _innerStrategy.AttachContext(context);
            }

            public bool MarkNode(
                DependencyNodeCore<TestContext> node,
                DependencyNodeCore<TestContext> reasonNode,
                DependencyNodeCore<TestContext> reasonNode2,
                string reason)
            {
                _context.RecordMarkAttempt(node, reason);
                return _innerStrategy.MarkNode(node, reasonNode, reasonNode2, reason);
            }

            public void VisitLogEdges(
                IEnumerable<DependencyNodeCore<TestContext>> nodeList,
                IDependencyAnalyzerLogEdgeVisitor<TestContext> logEdgeVisitor)
            {
                _innerStrategy.VisitLogEdges(nodeList, logEdgeVisitor);
            }

            public void VisitLogNodes(
                IEnumerable<DependencyNodeCore<TestContext>> nodeList,
                IDependencyAnalyzerLogNodeVisitor<TestContext> logNodeVisitor)
            {
                _innerStrategy.VisitLogNodes(nodeList, logNodeVisitor);
            }
        }

        private sealed class TestNode : DependencyNodeCore<TestContext>
        {
            private readonly string _name;
            private readonly int _dependencyPhase;
            private IEnumerable<DependencyListEntry> _dependencies;
            private IEnumerable<CombinedDependencyListEntry> _conditionalDependencies;

            public TestNode(
                string name,
                IEnumerable<DependencyListEntry> dependencies = null,
                IEnumerable<CombinedDependencyListEntry> conditionalDependencies = null,
                bool dependenciesComputed = true,
                int dependencyPhase = 0)
            {
                _name = name;
                _dependencyPhase = dependencyPhase;
                if (dependenciesComputed)
                {
                    _dependencies = dependencies ?? Array.Empty<DependencyListEntry>();
                    _conditionalDependencies = conditionalDependencies;
                }
            }

            public string Name => _name;

            public override bool InterestingForDynamicDependencyAnalysis => false;

            public override bool HasDynamicDependencies => false;

            public override bool HasConditionalStaticDependencies => _conditionalDependencies is not null;

            public override bool StaticDependenciesAreComputed => _dependencies is not null;

            public override int DependencyPhaseForDeferredStaticComputation => _dependencyPhase;

            public void SetDependencies(
                IEnumerable<DependencyListEntry> dependencies,
                IEnumerable<CombinedDependencyListEntry> conditionalDependencies)
            {
                Assert.False(StaticDependenciesAreComputed);
                _dependencies = dependencies;
                _conditionalDependencies = conditionalDependencies;
            }

            public override IEnumerable<DependencyListEntry> GetStaticDependencies(TestContext context)
            {
                return _dependencies;
            }

            public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(TestContext context)
            {
                return _conditionalDependencies;
            }

            public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(
                List<DependencyNodeCore<TestContext>> markedNodes,
                int firstNode,
                TestContext context)
            {
                return Array.Empty<CombinedDependencyListEntry>();
            }

            protected override string GetName(TestContext context)
            {
                return _name;
            }
        }

        private static MethodDesc GetMethodFromAttribute(CustomAttributeValue attr)
        {
            if (attr.NamedArguments.Length > 0)
                throw new NotImplementedException(); // TODO: parse sig and instantiation

            return ((TypeDesc)attr.FixedArguments[0].Value).GetMethod(Encoding.UTF8.GetBytes((string)attr.FixedArguments[1].Value), null);
        }
    }
}
