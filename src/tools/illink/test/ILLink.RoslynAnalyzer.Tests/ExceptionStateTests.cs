// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using SourceGenerators.Tests;
using Xunit;
using State = ILLink.RoslynAnalyzer.DataFlow.LocalDataFlowState<
    ILLink.Shared.DataFlow.ValueSet<ILLink.RoslynAnalyzer.Tests.ExceptionStateTests.TestValue>,
    ILLink.Shared.DataFlow.ValueSet<string>,
    ILLink.Shared.DataFlow.ValueSetLattice<ILLink.RoslynAnalyzer.Tests.ExceptionStateTests.TestValue>,
    ILLink.Shared.DataFlow.ValueSetLattice<string>>;
using StateValue = ILLink.RoslynAnalyzer.DataFlow.LocalStateAndContext<
    ILLink.Shared.DataFlow.ValueSet<ILLink.RoslynAnalyzer.Tests.ExceptionStateTests.TestValue>,
    ILLink.Shared.DataFlow.ValueSet<string>>;

namespace ILLink.RoslynAnalyzer.Tests
{
    public class ExceptionStateTests
    {
        private static (LocalKey[] Keys, CapturedTargetKey CaptureKey) GetKeys(int count = 2)
        {
            string declarations = string.Join(" ", Enumerable.Range(0, count).Select(i => $"int local{i} = {i};"));
            SyntaxTree tree = CSharpSyntaxTree.ParseText($"class C {{ void M() {{ {declarations} }} }}");
            CSharpCompilation compilation = CSharpCompilation.Create("StateTests", new[] { tree }, LiveReferencePack.GetMetadataReferences());
            SemanticModel model = compilation.GetSemanticModel(tree);
            LocalKey[] keys = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>()
                .Select(node => new LocalKey((ILocalSymbol)model.GetDeclaredSymbol(node)!)).ToArray();
            IOperation operation = model.GetOperation(tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().First())!;
            return (keys, new CapturedTargetKey(operation));
        }

        private static State CreateState()
        {
            var lattice = new LocalStateAndContextLattice<ValueSet<TestValue>, ValueSet<string>, ValueSetLattice<TestValue>, ValueSetLattice<string>>(
                new LocalStateLattice<ValueSet<TestValue>, ValueSetLattice<TestValue>>(default), default);
            return new State { Lattice = lattice, Current = lattice.Top, Exception = new Box<StateValue>(lattice.Top) };
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ScopeCleanupRemovesOnlyDeadCurrentLocalsAndCaptures(bool captured)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText("""
                class C
                {
                    static void M(bool condition)
                    {
                        { int scoped = condition ? 1 : 2; System.GC.KeepAlive(scoped); }
                        System.GC.KeepAlive(condition);
                    }
                }
                """);
            CSharpCompilation compilation = CSharpCompilation.Create("ScopeTests", new[] { tree }, LiveReferencePack.GetMetadataReferences());
            SemanticModel model = compilation.GetSemanticModel(tree);
            var body = (IMethodBodyOperation)model.GetOperation(tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single())!;
            ControlFlowGraph graph = ControlFlowGraph.Create(body);
            ControlFlowRegion region = GetRegions(graph.Root).Single(candidate => captured
                ? !candidate.CaptureIds.IsEmpty
                : candidate.Locals.Any(local => local.Name == "scoped"));
            LocalKey dead = captured ? new(region.CaptureIds[0]) : new(region.Locals.Single(local => local.Name == "scoped"));
            var (liveKeys, _) = GetKeys();
            LocalKey live = liveKeys[0];
            IOperation reference = model.GetOperation(tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First())!;
            var targetKey = new CapturedTargetKey(reference);
            State state = CreateState();
            StateValue current = state.Current;
            current.Context = new ValueSet<string>("feature");
            current.LocalState.CapturedTargetValues.Set(targetKey, new CapturedTargetValue<ValueSet<TestValue>>(new TestValue(3)));
            if (captured)
                current.LocalState.CapturedReferences.Set(region.CaptureIds[0], new ValueSet<CapturedReferenceValue>(new CapturedReferenceValue(reference)));
            state.Current = current;
            state.Set(dead, new TestValue(1));
            state.Set(live, new TestValue(2));
            StateValue predecessor = state.CreateBranchOutput(canTransferOwnership: false);
            StateValue exception = state.Exception!.Value;
            state.Current = state.Lattice.Meet(state.Lattice.Top, predecessor);

            typeof(State).GetMethod("RemoveLocals", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(state, new object[] { region });

            Assert.Empty(state.Get(dead).GetKnownValues());
            Assert.Equal(2, Assert.Single(state.Get(live).GetKnownValues()).Id);
            Assert.Equal(3, Assert.Single(state.Current.LocalState.CapturedTargetValues.Get(targetKey).Value.GetKnownValues()).Id);
            Assert.True(state.Current.Context.Contains("feature"));
            Assert.Equal(1, Assert.Single(predecessor.LocalState.Get(dead).GetKnownValues()).Id);
            Assert.Equal(1, Assert.Single(exception.LocalState.Get(dead).GetKnownValues()).Id);
            if (captured)
            {
                Assert.Empty(state.Current.LocalState.CapturedReferences.Get(region.CaptureIds[0]).GetKnownValues());
                Assert.Single(predecessor.LocalState.CapturedReferences.Get(region.CaptureIds[0]).GetKnownValues());
                Assert.Single(exception.LocalState.CapturedReferences.Get(region.CaptureIds[0]).GetKnownValues());
            }
        }

        private static IEnumerable<ControlFlowRegion> GetRegions(ControlFlowRegion region)
        {
            yield return region;
            foreach (ControlFlowRegion nested in region.NestedRegions)
            {
                foreach (ControlFlowRegion descendant in GetRegions(nested))
                    yield return descendant;
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CompletedImmutableOutputCanTransferItsOwnedMaps(bool canTransferOwnership)
        {
            LocalKey[] keys = GetKeys().Keys;
            State state = CreateState();
            state.Set(keys[0], new TestValue(1));
            state.Set(keys[1], new TestValue(2));
            var output = GetOutputFactory(state, canTransferOwnership);
            output();

            long before = GC.GetAllocatedBytesForCurrentThread();
            StateValue result = output();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.Equal(2, result.LocalState.Dictionary.Count);
            if (canTransferOwnership)
                Assert.Equal(0, allocated);
            else
                Assert.True(allocated > 0);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CompletedOutputStillSnapshotsMutableAliases(bool capturedTarget)
        {
            var (keys, captureKey) = GetKeys();
            State state = CreateState();
            TestValue mutable = new(1, mutable: true);
            if (capturedTarget)
            {
                StateValue current = state.Current;
                current.LocalState.CapturedTargetValues.Set(captureKey, new CapturedTargetValue<ValueSet<TestValue>>(mutable));
                state.Current = current;
            }
            else
            {
                state.Set(keys[0], mutable);
            }

            StateValue result = GetOutputFactory(state, canTransferOwnership: true)();
            mutable.Id = 2;
            ValueSet<TestValue> values = capturedTarget
                ? result.LocalState.CapturedTargetValues.Get(captureKey).Value
                : result.LocalState.Get(keys[0]);
            Assert.Equal(1, Assert.Single(values.GetKnownValues()).Id);
        }

        [Fact]
        public void CompletedMutableOutputCopiesOnlyTheChangedPayload()
        {
            LocalKey key = GetKeys().Keys[0];
            State state = CreateState();
            TestValue mutable = new(1, mutable: true);
            state.Set(key, mutable);
            var output = GetOutputFactory(state, canTransferOwnership: true);
            output();
            mutable.DeepCopy();

            long before = GC.GetAllocatedBytesForCurrentThread();
            TestValue expectedCopy = mutable.DeepCopy();
            long expectedAllocation = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            StateValue result = output();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.Equal(expectedAllocation, allocated);
            Assert.Equal(expectedCopy, Assert.Single(result.LocalState.Get(key).GetKnownValues()));
            Assert.NotSame(mutable, Assert.Single(result.LocalState.Get(key).GetKnownValues()));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public void CompletedOutputDetachesAllMutableLocalPayloads(int count)
        {
            LocalKey[] keys = GetKeys(count).Keys;
            TestValue[] mutable = Enumerable.Range(0, count).Select(i => new TestValue(i, mutable: true)).ToArray();
            State state = CreateState();
            for (int i = 0; i < count; i++)
                state.Set(keys[i], mutable[i]);

            StateValue result = GetOutputFactory(state, canTransferOwnership: true)();
            for (int i = 0; i < count; i++)
            {
                mutable[i].Id = -1;
                Assert.Equal(i, Assert.Single(result.LocalState.Get(keys[i]).GetKnownValues()).Id);
            }
        }

        [Fact]
        public void FinalOwnedOutputRemainsIndependentOfEarlierBranches()
        {
            LocalKey key = GetKeys().Keys[0];
            State state = CreateState();
            state.Set(key, new TestValue(1, mutable: true));
            StateValue earlier = GetOutputFactory(state, canTransferOwnership: false)();
            StateValue final = GetOutputFactory(state, canTransferOwnership: true)();
            earlier.LocalState.Set(key, new TestValue(2));
            final.Context = new ValueSet<string>("last-branch-condition");

            Assert.Equal(1, Assert.Single(final.LocalState.Get(key).GetKnownValues()).Id);
            Assert.True(earlier.Context.IsEmpty());
            Assert.True(state.Current.Context.IsEmpty());
        }

        private static Func<StateValue> GetOutputFactory(State state, bool canTransferOwnership)
        {
            MethodInfo? method = typeof(State).GetMethod(nameof(State.CreateBranchOutput));
            if (method is null)
                return () => state.Lattice.Meet(state.Lattice.Top, state.Current);
            var create = (Func<bool, StateValue>)method.CreateDelegate(typeof(Func<bool, StateValue>), state);
            return () => create(canTransferOwnership);
        }

        [Fact]
        public void LocalKeysPreserveLocalCaptureAndDefaultIdentity()
        {
            LocalKey[] locals = GetKeys().Keys;
            CaptureId otherCapture = (CaptureId)Activator.CreateInstance(typeof(CaptureId),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, binder: null, args: new object[] { 1 }, culture: null)!;
            LocalKey[] keys = [default, new(default(CaptureId)), new(otherCapture), locals[0], locals[1]];
            for (int i = 0; i < keys.Length; i++)
            {
                for (int j = 0; j < keys.Length; j++)
                {
                    Assert.Equal(i == j, keys[i].Equals(keys[j]));
                    Assert.Equal(i == j, keys[i].Equals((object)keys[j]));
                    if (i == j)
                        Assert.Equal(keys[i].GetHashCode(), keys[j].GetHashCode());
                }
            }
        }

        [Fact]
        public void CaptureKeyDictionaryLookupDoesNotAllocate()
        {
            LocalKey key = new(default(CaptureId));
            Dictionary<LocalKey, ValueSet<TestValue>> dictionary = new() { [key] = new TestValue(1) };
            for (int i = 0; i < 10_000; i++)
                Lookup(dictionary, key);

            long before = GC.GetAllocatedBytesForCurrentThread();
            int count = 0;
            for (int i = 0; i < 1_000; i++)
                count += Lookup(dictionary, key);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.Equal(1_000, count);
            Assert.Equal(0, allocated);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static int Lookup(Dictionary<LocalKey, ValueSet<TestValue>> values, LocalKey lookupKey) =>
                values[lookupKey].HasMultipleValues ? 0 : 1;
        }

        [Fact]
        public void RepeatedUpdatesPreserveIntermediateValuesAndPreTransferSnapshot()
        {
            LocalKey key = GetKeys().Keys[0];
            State state = CreateState();
            StateValue before = state.Exception!.Value;

            state.Set(key, new TestValue(1));
            state.Set(key, new TestValue(2));
            state.Set(key, new TestValue(3));

            Assert.Empty(before.LocalState.Get(key).GetKnownValues());
            Assert.Equal(new[] { 1, 2, 3 }, state.Exception.Value.LocalState.Get(key).GetKnownValues().Select(value => value.Id).OrderBy(value => value));

            StateValue previousTransfer = state.Exception.Value;
            state.Current = state.Current;
            state.Set(key, new TestValue(4));

            Assert.Equal(new[] { 1, 2, 3 }, previousTransfer.LocalState.Get(key).GetKnownValues().Select(value => value.Id).OrderBy(value => value));
            Assert.Equal(new[] { 1, 2, 3, 4 }, state.Exception.Value.LocalState.Get(key).GetKnownValues().Select(value => value.Id).OrderBy(value => value));
        }

        [Fact]
        public void UpdatingAnotherLocalSnapshotsMutableValues()
        {
            LocalKey[] keys = GetKeys().Keys;
            State state = CreateState();
            TestValue mutable = new(1, mutable: true);
            state.Set(keys[0], mutable);

            mutable.Id = 2;
            state.Set(keys[1], new TestValue(10));
            mutable.Id = 3;
            state.Set(keys[1], new TestValue(11));

            Assert.Equal(new[] { 1, 2, 3 }, state.Exception!.Value.LocalState.Get(keys[0]).GetKnownValues().Select(value => value.Id).OrderBy(value => value));
            Assert.Equal(new[] { 10, 11 }, state.Exception.Value.LocalState.Get(keys[1]).GetKnownValues().Select(value => value.Id).OrderBy(value => value));
            mutable.Id = 4;
            Assert.DoesNotContain(state.Exception.Value.LocalState.Get(keys[0]).GetKnownValues(), value => value.Id == 4);
        }

        [Fact]
        public void ReplacingCurrentContextForcesAFullSnapshot()
        {
            LocalKey[] keys = GetKeys().Keys;
            State state = CreateState();
            state.Set(keys[0], new TestValue(1));
            StateValue current = state.Current;
            current.Context = new ValueSet<string>("feature");
            current.LocalState.Set(keys[1], new TestValue(2));
            state.Current = current;

            state.Set(keys[0], new TestValue(3));

            Assert.True(state.Exception!.Value.Context.Contains("feature"));
            Assert.Equal(2, Assert.Single(state.Exception.Value.LocalState.Get(keys[1]).GetKnownValues()).Id);
        }

        [Fact]
        public void UpdatingAnotherLocalSnapshotsMutableCapturedTargets()
        {
            var (keys, captureKey) = GetKeys();
            State state = CreateState();
            TestValue mutable = new(1, mutable: true);
            StateValue current = state.Current;
            current.LocalState.CapturedTargetValues.Set(captureKey, new CapturedTargetValue<ValueSet<TestValue>>(mutable));
            state.Current = current;
            state.Set(keys[0], new TestValue(10));

            mutable.Id = 2;
            state.Set(keys[1], new TestValue(11));
            mutable.Id = 3;
            state.Set(keys[1], new TestValue(12));

            Assert.Equal(new[] { 1, 2, 3 }, state.Exception!.Value.LocalState.CapturedTargetValues.Get(captureKey).Value
                .GetKnownValues().Select(value => value.Id).OrderBy(value => value));
        }

        [Fact]
        public void LocalsCanBecomeMutableAfterIncrementalUpdatesStart()
        {
            LocalKey[] keys = GetKeys().Keys;
            State state = CreateState();
            state.Set(keys[0], new TestValue(1));
            state.Set(keys[1], new TestValue(2));
            TestValue mutable = new(3, mutable: true);
            state.Set(keys[0], mutable);

            mutable.Id = 4;
            state.Set(keys[1], new TestValue(5));

            Assert.Equal(new[] { 1, 3, 4 }, state.Exception!.Value.LocalState.Get(keys[0]).GetKnownValues().Select(value => value.Id).OrderBy(value => value));
        }

        [Fact]
        public void ImmutableLocalsAreNotRecopiedForEveryAssignment()
        {
            const int Count = 64;
            LocalKey[] keys = GetKeys(Count).Keys;
            TestValue[] values = Enumerable.Range(0, Count).Select(i => new TestValue(i)).ToArray();
            State state = CreateState();

            for (int i = 0; i < Count; i++)
                state.Set(keys[i], values[i]);

            Assert.Equal(Count, state.Exception!.Value.LocalState.Dictionary.Count);
            Assert.InRange(values.Sum(value => value.CopyCount), Count, 4 * Count);
        }

        public sealed class TestValue(int id, bool mutable = false) : IEquatable<TestValue>, IDeepCopyValue<TestValue>, IValueWithMutableState
        {
            private readonly bool _mutable = mutable;
            public int Id { get; set; } = id;
            public int CopyCount { get; private set; }
            public bool HasMutableState => _mutable;
            public TestValue DeepCopy()
            {
                CopyCount++;
                return _mutable ? new TestValue(Id, _mutable) : this;
            }
            public bool Equals(TestValue? other) => other is not null && Id == other.Id && _mutable == other._mutable;
            public override bool Equals(object? obj) => obj is TestValue other && Equals(other);
            public override int GetHashCode() => Id;
        }
    }
}
