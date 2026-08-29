// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using ILCompiler.DependencyAnalysisFramework;

using Xunit;

using CombinedDependencyListEntry = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<object>.CombinedDependencyListEntry;
using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<object>.DependencyList;
using DependencyListEntry = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<object>.DependencyListEntry;

namespace ILCompiler.Compiler.Tests
{
    public class DependencyAnalyzerTests
    {
        public enum DependencyCollectionKind
        {
            Array,
            DependencyList,
            Enumerable,
            List,
            ReimplementedList,
        }

        public static IEnumerable<object[]> StaticDependencyData()
        {
            DependencyCollectionKind[] collectionKinds =
            [
                DependencyCollectionKind.Array,
                DependencyCollectionKind.DependencyList,
                DependencyCollectionKind.Enumerable,
                DependencyCollectionKind.List,
                DependencyCollectionKind.ReimplementedList,
            ];

            return CreateDependencyData(collectionKinds);
        }

        public static IEnumerable<object[]> ConditionalDependencyData()
        {
            DependencyCollectionKind[] collectionKinds =
            [
                DependencyCollectionKind.Array,
                DependencyCollectionKind.Enumerable,
                DependencyCollectionKind.List,
                DependencyCollectionKind.ReimplementedList,
            ];

            return CreateDependencyData(collectionKinds);
        }

        public static IEnumerable<object[]> ConditionalDependencyCollectionKinds()
        {
            yield return new object[] { DependencyCollectionKind.Array };
            yield return new object[] { DependencyCollectionKind.Enumerable };
            yield return new object[] { DependencyCollectionKind.List };
            yield return new object[] { DependencyCollectionKind.ReimplementedList };
        }

        public static IEnumerable<object[]> MutableStaticDependencyLists()
        {
            yield return new object[] { DependencyCollectionKind.DependencyList };
            yield return new object[] { DependencyCollectionKind.List };
        }

        [Theory]
        [MemberData(nameof(StaticDependencyData))]
        public void StaticDependenciesPreserveOrder(DependencyCollectionKind collectionKind, int count)
        {
            TestNode[] dependencyNodes = CreateNodes("dependency", count);
            DependencyListEntry[] entries = CreateStaticEntries(dependencyNodes);
            var root = new TestNode("root")
            {
                StaticDependencies = CreateStaticDependencies(collectionKind, entries),
            };
            DependencyAnalyzer<NoLogStrategy<object>, object> analyzer = CreateAnalyzer();

            analyzer.AddRoot(root, "root");
            analyzer.ComputeMarkedNodes();

            DependencyNodeCore<object>[] expected = new DependencyNodeCore<object>[count + 1];
            expected[0] = root;
            CopyExpectedNodes(expected, 1, dependencyNodes, collectionKind);
            Assert.Equal(expected, analyzer.MarkedNodeList);
        }

        [Theory]
        [MemberData(nameof(ConditionalDependencyData))]
        public void ConditionalDependenciesPreserveOrder(DependencyCollectionKind collectionKind, int count)
        {
            var condition = new TestNode("condition");
            TestNode[] dependencyNodes = CreateNodes("dependency", count);
            CombinedDependencyListEntry[] entries = CreateConditionalEntries(dependencyNodes, condition);
            var root = new TestNode("root")
            {
                ConditionalDependencies = CreateConditionalDependencies(collectionKind, entries),
            };
            DependencyAnalyzer<NoLogStrategy<object>, object> analyzer = CreateAnalyzer();

            analyzer.AddRoot(condition, "condition");
            analyzer.AddRoot(root, "root");
            analyzer.ComputeMarkedNodes();

            DependencyNodeCore<object>[] expected = new DependencyNodeCore<object>[count + 2];
            expected[0] = condition;
            expected[1] = root;
            CopyExpectedNodes(expected, 2, dependencyNodes, collectionKind);
            Assert.Equal(expected, analyzer.MarkedNodeList);
        }

        [Theory]
        [MemberData(nameof(ConditionalDependencyCollectionKinds))]
        public void NullConditionalDependencyIsUnconditional(DependencyCollectionKind collectionKind)
        {
            var dependency = new TestNode("dependency");
            CombinedDependencyListEntry[] entries =
            [
                new CombinedDependencyListEntry(dependency, null, "unconditional"),
            ];
            var root = new TestNode("root")
            {
                ConditionalDependencies = CreateConditionalDependencies(collectionKind, entries),
            };
            DependencyAnalyzer<NoLogStrategy<object>, object> analyzer = CreateAnalyzer();

            analyzer.AddRoot(root, "root");
            analyzer.ComputeMarkedNodes();

            Assert.Equal(new DependencyNodeCore<object>[] { root, dependency }, analyzer.MarkedNodeList);
        }

        [Theory]
        [MemberData(nameof(ConditionalDependencyData))]
        public void ConditionalDependenciesAreMarkedWhenConditionAppears(DependencyCollectionKind collectionKind, int count)
        {
            var condition = new TestNode("condition");
            TestNode[] dependencyNodes = CreateNodes("dependency", count);
            var conditionProvider = new TestNode("condition provider")
            {
                StaticDependencies =
                [
                    new DependencyListEntry(condition, "condition"),
                ],
            };
            CombinedDependencyListEntry[] entries = CreateConditionalEntries(dependencyNodes, condition);
            var root = new TestNode("root")
            {
                StaticDependencies =
                [
                    new DependencyListEntry(conditionProvider, "condition provider"),
                ],
                ConditionalDependencies = CreateConditionalDependencies(collectionKind, entries),
            };
            DependencyAnalyzer<NoLogStrategy<object>, object> analyzer = CreateAnalyzer();

            analyzer.AddRoot(root, "root");
            analyzer.ComputeMarkedNodes();

            DependencyNodeCore<object>[] expected = new DependencyNodeCore<object>[count + 3];
            expected[0] = root;
            expected[1] = conditionProvider;
            expected[2] = condition;
            CopyExpectedNodes(expected, 3, dependencyNodes, collectionKind);
            Assert.Equal(expected, analyzer.MarkedNodeList);
        }

        [Theory]
        [MemberData(nameof(MutableStaticDependencyLists))]
        public void StaticDependencyListMutationIsDetected(DependencyCollectionKind collectionKind)
        {
            List<DependencyListEntry> dependencies = collectionKind switch
            {
                DependencyCollectionKind.DependencyList => new DependencyList(),
                DependencyCollectionKind.List => new List<DependencyListEntry>(),
                _ => throw new UnreachableException(),
            };
            var addedDependency = new TestNode("added dependency");
            var dependency = new TestNode(
                "dependency",
                () => dependencies.Add(new DependencyListEntry(addedDependency, "added dependency")));
            dependencies.Add(new DependencyListEntry(dependency, "dependency"));
            var root = new TestNode("root")
            {
                StaticDependencies = dependencies,
            };
            DependencyAnalyzer<NoLogStrategy<object>, object> analyzer = CreateAnalyzer();
            analyzer.AddRoot(root, "root");

            Assert.Throws<InvalidOperationException>(analyzer.ComputeMarkedNodes);
        }

        [Fact]
        public void ConditionalDependencyListMutationIsDetected()
        {
            var condition = new TestNode("condition");
            var addedDependency = new TestNode("added dependency");
            var dependencies = new List<CombinedDependencyListEntry>();
            var dependency = new TestNode(
                "dependency",
                () => dependencies.Add(new CombinedDependencyListEntry(addedDependency, condition, "added dependency")));
            dependencies.Add(new CombinedDependencyListEntry(dependency, condition, "dependency"));
            var root = new TestNode("root")
            {
                ConditionalDependencies = dependencies,
            };
            DependencyAnalyzer<NoLogStrategy<object>, object> analyzer = CreateAnalyzer();
            analyzer.AddRoot(condition, "condition");
            analyzer.AddRoot(root, "root");

            Assert.Throws<InvalidOperationException>(analyzer.ComputeMarkedNodes);
        }

        private static DependencyAnalyzer<NoLogStrategy<object>, object> CreateAnalyzer()
        {
            return new DependencyAnalyzer<NoLogStrategy<object>, object>(new object(), resultSorter: null);
        }

        private static IEnumerable<object[]> CreateDependencyData(DependencyCollectionKind[] collectionKinds)
        {
            int[] counts = [0, 1, 3];
            foreach (DependencyCollectionKind collectionKind in collectionKinds)
            {
                foreach (int count in counts)
                {
                    yield return new object[] { collectionKind, count };
                }
            }
        }

        private static TestNode[] CreateNodes(string namePrefix, int count)
        {
            var nodes = new TestNode[count];
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i] = new TestNode($"{namePrefix} {i}");
            }

            return nodes;
        }

        private static DependencyListEntry[] CreateStaticEntries(TestNode[] dependencyNodes)
        {
            var entries = new DependencyListEntry[dependencyNodes.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                entries[i] = new DependencyListEntry(dependencyNodes[i], $"dependency {i}");
            }

            return entries;
        }

        private static CombinedDependencyListEntry[] CreateConditionalEntries(
            TestNode[] dependencyNodes,
            TestNode condition)
        {
            var entries = new CombinedDependencyListEntry[dependencyNodes.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                entries[i] = new CombinedDependencyListEntry(dependencyNodes[i], condition, $"dependency {i}");
            }

            return entries;
        }

        private static IEnumerable<DependencyListEntry> CreateStaticDependencies(
            DependencyCollectionKind collectionKind,
            DependencyListEntry[] entries)
        {
            return collectionKind switch
            {
                DependencyCollectionKind.Array => entries,
                DependencyCollectionKind.DependencyList => new DependencyList(entries),
                DependencyCollectionKind.Enumerable => Enumerate(entries),
                DependencyCollectionKind.List => new List<DependencyListEntry>(entries),
                DependencyCollectionKind.ReimplementedList => new ReimplementedEnumerableList<DependencyListEntry>(entries),
                _ => throw new UnreachableException(),
            };
        }

        private static IEnumerable<CombinedDependencyListEntry> CreateConditionalDependencies(
            DependencyCollectionKind collectionKind,
            CombinedDependencyListEntry[] entries)
        {
            return collectionKind switch
            {
                DependencyCollectionKind.Array => entries,
                DependencyCollectionKind.Enumerable => Enumerate(entries),
                DependencyCollectionKind.List => new List<CombinedDependencyListEntry>(entries),
                DependencyCollectionKind.ReimplementedList => new ReimplementedEnumerableList<CombinedDependencyListEntry>(entries),
                _ => throw new UnreachableException(),
            };
        }

        private static IEnumerable<T> Enumerate<T>(T[] items)
        {
            foreach (T item in items)
            {
                yield return item;
            }
        }

        private static void CopyExpectedNodes(
            DependencyNodeCore<object>[] destination,
            int destinationIndex,
            TestNode[] nodes,
            DependencyCollectionKind collectionKind)
        {
            if (collectionKind == DependencyCollectionKind.ReimplementedList)
            {
                for (int i = nodes.Length - 1; i >= 0; i--)
                {
                    destination[destinationIndex++] = nodes[i];
                }
            }
            else
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    destination[destinationIndex++] = nodes[i];
                }
            }
        }

        private sealed class ReimplementedEnumerableList<T> : List<T>, IEnumerable<T>, IEnumerable
        {
            public ReimplementedEnumerableList(IEnumerable<T> items)
                : base(items)
            {
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                for (int i = Count - 1; i >= 0; i--)
                {
                    yield return this[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<T>)this).GetEnumerator();
            }
        }

        private sealed class TestNode : DependencyNodeCore<object>
        {
            private readonly string _name;
            private readonly Action _onMarked;

            public TestNode(string name, Action onMarked = null)
            {
                _name = name;
                _onMarked = onMarked;
            }

            public IEnumerable<DependencyListEntry> StaticDependencies { get; set; } = Array.Empty<DependencyListEntry>();

            public IEnumerable<CombinedDependencyListEntry> ConditionalDependencies { get; set; }

            public override bool InterestingForDynamicDependencyAnalysis => false;

            public override bool HasDynamicDependencies => false;

            public override bool HasConditionalStaticDependencies => ConditionalDependencies is not null;

            public override bool StaticDependenciesAreComputed => true;

            public override IEnumerable<DependencyListEntry> GetStaticDependencies(object context)
            {
                return StaticDependencies;
            }

            public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(object context)
            {
                return ConditionalDependencies;
            }

            public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(
                List<DependencyNodeCore<object>> markedNodes,
                int firstNode,
                object context)
            {
                return Array.Empty<CombinedDependencyListEntry>();
            }

            protected override void OnMarked(object context)
            {
                _onMarked?.Invoke();
            }

            protected override string GetName(object context)
            {
                return _name;
            }
        }
    }
}
