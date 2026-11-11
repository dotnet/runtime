// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ILLink.Shared.DataFlow;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    public class ValueSetTests
    {
        [Theory]
        [InlineData(2, false)]
        [InlineData(2, true)]
        [InlineData(16, false)]
        [InlineData(16, true)]
        [InlineData(128, false)]
        [InlineData(128, true)]
        public void MixedMutableSnapshotsAllocateOnlyStorageAndPayload(int count, bool mutableFirst)
        {
            int mutableId = mutableFirst ? 0 : count - 1;
            TestValue[] values = Enumerable.Range(0, count).Select(i => new TestValue(i, mutable: i == mutableId)).ToArray();
            ValueSet<TestValue> source = new(values);
            TestValue mutable = values[mutableId];
            source.DeepCopy();
            GC.KeepAlive(CreateStorage());
            mutable.DeepCopy();

            long before = GC.GetAllocatedBytesForCurrentThread();
            HashSet<TestValue> expectedStorage = CreateStorage();
            long storageBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            TestValue expectedPayload = mutable.DeepCopy();
            long payloadBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            ValueSet<TestValue> copy = source.DeepCopy();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            GC.KeepAlive(expectedStorage);
            GC.KeepAlive(expectedPayload);

            Assert.Equal(Enumerable.Range(0, count), copy.GetKnownValues().Select(value => value.Id));
            TestValue copiedMutable = Assert.Single(copy.GetKnownValues().Where(value => value.Id == mutableId));
            Assert.NotSame(mutable, copiedMutable);
            mutable.Id = -1;
            Assert.Equal(mutableId, copiedMutable.Id);
            Assert.Equal(storageBytes + payloadBytes, allocated);

            HashSet<TestValue> CreateStorage()
            {
                var storage = new HashSet<TestValue>();
                foreach (TestValue value in values)
                    storage.Add(value);
                return storage;
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(32)]
        public void ArraySnapshotsAllocateOnlyTheirFinalIndexStorage(int count)
        {
            SingleValue source = CreateArray(count);
            Dictionary<int, ValueSet<SingleValue>> indices = GetArrayIndices(source);
            for (int i = 0; i < count; i++)
                indices.Add(i, new TestSingleValue(i));
            SingleValue empty = CreateArray(0);
            source.DeepCopy();
            empty.DeepCopy();
            var capacityWarmup = new Dictionary<int, ValueSet<SingleValue>>(count);
            GC.KeepAlive(capacityWarmup);

            long before = GC.GetAllocatedBytesForCurrentThread();
            SingleValue emptyCopy = empty.DeepCopy();
            long emptyCopyBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            var emptyDictionary = new Dictionary<int, ValueSet<SingleValue>>();
            long emptyDictionaryBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            var finalDictionary = new Dictionary<int, ValueSet<SingleValue>>(count);
            long finalDictionaryBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            SingleValue copy = source.DeepCopy();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            GC.KeepAlive(emptyCopy);
            GC.KeepAlive(emptyDictionary);
            GC.KeepAlive(finalDictionary);

            Dictionary<int, ValueSet<SingleValue>> copiedIndices = GetArrayIndices(copy);
            Assert.Equal(count, copiedIndices.Count);
            for (int i = 0; i < count; i++)
                Assert.Equal(new TestSingleValue(i), Assert.Single(copiedIndices[i].GetKnownValues()));
            copiedIndices[count] = new TestSingleValue(-1);
            Assert.Equal(count, indices.Count);
            Assert.Equal(emptyCopyBytes - emptyDictionaryBytes + finalDictionaryBytes, allocated);
        }

        [Fact]
        public void ArraySnapshotsStillDetachMutableElementValues()
        {
            SingleValue source = CreateArray(1);
            var element = new MutableTestSingleValue(1);
            GetArrayIndices(source).Add(0, element);

            SingleValue copy = source.DeepCopy();
            element.Id = 2;

            var copiedElement = Assert.IsType<MutableTestSingleValue>(Assert.Single(GetArrayIndices(copy)[0].GetKnownValues()));
            Assert.Equal(1, copiedElement.Id);
            Assert.NotSame(element, copiedElement);
        }

        private static SingleValue CreateArray(int size)
        {
            Type type = typeof(SingleValue).Assembly.GetType("ILLink.Shared.TrimAnalysis.ArrayValue", throwOnError: true)!;
            var values = (ValueSet<SingleValue>)type.GetMethod("Create", new[] { typeof(int) })!.Invoke(null, new object[] { size })!;
            return Assert.Single(values.GetKnownValues());
        }

        private static Dictionary<int, ValueSet<SingleValue>> GetArrayIndices(SingleValue array) =>
            (Dictionary<int, ValueSet<SingleValue>>)array.GetType().GetField("IndexValues")!.GetValue(array)!;

        private sealed record MutableTestSingleValue : SingleValue, IValueWithMutableState
        {
            public int Id { get; set; }
            public bool HasMutableState => true;
            public MutableTestSingleValue(int id) => Id = id;
            public override SingleValue DeepCopy() => new MutableTestSingleValue(Id);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void DistinctUnchangedSetJoinsDoNotAllocate(bool intersect, bool subset)
        {
            ValueSet<int> left = new(Enumerable.Range(0, 16));
            int rightCount = subset ? (intersect ? 32 : 8) : 16;
            ValueSet<int> right = new(Enumerable.Range(0, rightCount));
            Func<ValueSet<int>, ValueSet<int>, ValueSet<int>> join = intersect
                ? (Func<ValueSet<int>, ValueSet<int>, ValueSet<int>>)typeof(ValueSet<int>)
                    .GetMethod("Intersection", BindingFlags.Static | BindingFlags.NonPublic)!
                    .CreateDelegate(typeof(Func<ValueSet<int>, ValueSet<int>, ValueSet<int>>))
                : default(ValueSetLattice<int>).Meet;
            join(left, right);

            long before = GC.GetAllocatedBytesForCurrentThread();
            ValueSet<int> result = join(left, right);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.Equal(Enumerable.Range(0, 16), result.GetKnownValues());
            Assert.Equal(0, allocated);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DistinctMutableSetJoinsStillSnapshotValues(bool intersect)
        {
            TestValue leftMutable = new(2, mutable: true);
            TestValue rightMutable = new(2, mutable: true);
            ValueSet<TestValue> left = new(new[] { new TestValue(1, mutable: false), leftMutable });
            ValueSet<TestValue> right = new(new[] { new TestValue(1, mutable: false), rightMutable });
            ValueSet<TestValue> result = intersect
                ? (ValueSet<TestValue>)typeof(ValueSet<TestValue>).GetMethod("Intersection", BindingFlags.Static | BindingFlags.NonPublic)!
                    .Invoke(null, new object[] { left, right })!
                : default(ValueSetLattice<TestValue>).Meet(left, right);
            TestValue copiedMutable = Assert.Single(result.GetKnownValues().Where(value => value.Id == 2));
            leftMutable.Id = 3;
            rightMutable.Id = 4;

            Assert.Equal(2, copiedMutable.Id);
            Assert.True(result.Contains(copiedMutable));
            Assert.NotSame(leftMutable, copiedMutable);
            Assert.NotSame(rightMutable, copiedMutable);
        }

        [Theory]
        [InlineData(256, false)]
        [InlineData(257, true)]
        public void DistinctEqualSetsPreserveTheWideningBoundary(int count, bool unknown)
        {
            ValueSet<int> left = new(Enumerable.Range(0, count));
            ValueSet<int> right = new(Enumerable.Range(0, count));
            ValueSet<int> result = default(ValueSetLattice<int>).Meet(left, right);

            Assert.Equal(unknown, result.IsUnknown());
            if (!unknown)
                Assert.Equal(count, result.GetKnownValues().Count());
        }

        private delegate SingleValue? ReadSingleValue(in ValueSet<SingleValue> value);

        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(1, true)]
        [InlineData(16, false)]
        public void SingleValueReadPreservesUnknownAndDoesNotAllocate(int count, bool direct)
        {
            Type extensions = typeof(ValueSet<SingleValue>).Assembly.GetType("ILLink.Shared.TrimAnalysis.ValueExtensions", throwOnError: true)!;
            var read = (ReadSingleValue)extensions.GetMethod("AsSingleValue", BindingFlags.Static | BindingFlags.NonPublic)!
                .CreateDelegate(typeof(ReadSingleValue));
            ValueSet<SingleValue> values = count < 0
                ? ValueSet<SingleValue>.Unknown
                : direct
                    ? new ValueSet<SingleValue>(new TestSingleValue(0))
                    : new ValueSet<SingleValue>(Enumerable.Range(0, count).Select(i => (SingleValue)new TestSingleValue(i)));
            SingleValue? result = read(in values);
            if (count < 0)
                Assert.Equal("UnknownValue", result!.GetType().Name);
            else if (count == 1)
                Assert.Equal(new TestSingleValue(0), result);
            else
                Assert.Null(result);

            long before = GC.GetAllocatedBytesForCurrentThread();
            read(in values);
            Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(16)]
        public void SingleValueLookupDoesNotExposeObjectSetStorage(int count)
        {
            ValueSet<object> values = count < 0 ? ValueSet<object>.Unknown
                : count == 1 ? new ValueSet<object>("single")
                : new ValueSet<object>(Enumerable.Range(0, count).Cast<object>());
            object?[] arguments = [null];
            bool success = (bool)typeof(ValueSet<object>).GetMethod("TryGetSingleValue", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(values, arguments)!;

            Assert.Equal(count == 1, success);
            Assert.Equal(count == 1 ? "single" : null, arguments[0]);
        }

        private sealed record TestSingleValue(int Id) : SingleValue
        {
            public override SingleValue DeepCopy() => this;
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(16)]
        public void EnumerationPreservesResetAndInterfaceBehavior(int count)
        {
            ValueSet<int> values = count == 1 ? new ValueSet<int>(0) : new ValueSet<int>(Enumerable.Range(0, count));
            var enumerator = values.GetKnownValues().GetEnumerator();
            for (int pass = 0; pass < 2; pass++)
            {
                List<int> actual = new();
                while (enumerator.MoveNext())
                    actual.Add(enumerator.Current);
                Assert.Equal(Enumerable.Range(0, count), actual);
                enumerator.Reset();
            }
            Assert.Equal(Enumerable.Range(0, count), (IEnumerable<int>)values.GetKnownValues());
            Assert.Equal(Enumerable.Range(0, count), ((IEnumerable)values.GetKnownValues()).Cast<int>());
        }

        [Fact]
        public void MultiValueEnumerationDoesNotAllocate()
        {
            ValueSet<int> values = new(Enumerable.Range(0, 16));
            int sum = 0;
            foreach (int value in values.GetKnownValues())
                sum += value;

            long before = GC.GetAllocatedBytesForCurrentThread();
            foreach (int value in values.GetKnownValues())
                sum += value;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.Equal(240, sum);
            Assert.Equal(0, allocated);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(16)]
        [InlineData(128)]
        public void DeepCopyDoesNotRehashImmutableValues(int count)
        {
            TestValue[] values = Enumerable.Range(0, count).Select(i => new TestValue(i, mutable: false)).ToArray();
            ValueSet<TestValue> source = new(values);
            foreach (TestValue value in values)
                value.HashCodeCalls = 0;

            ValueSet<TestValue> copy = source.DeepCopy();

            Assert.Equal(0, values.Sum(value => value.HashCodeCalls));
            Assert.Equal(values, copy.GetKnownValues());
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void MutableValuesRemainIsolated(bool mutableFirst, bool join)
        {
            TestValue first = new(1, mutable: false);
            TestValue second = new(2, mutable: false);
            TestValue mutable = new(3, mutable: true);
            TestValue[] values = mutableFirst ? [mutable, first, second] : [first, second, mutable];
            ValueSet<TestValue> source = new(values);
            mutable.Id = 4;

            ValueSet<TestValue> copy = join ? default(ValueSetLattice<TestValue>).Meet(source, source) : source.DeepCopy();
            TestValue copiedMutable = Assert.Single(copy.GetKnownValues().Where(value => value.Id == 4));

            Assert.NotSame(mutable, copiedMutable);
            Assert.True(copy.Contains(copiedMutable));
            Assert.Same(first, Assert.Single(copy.GetKnownValues().Where(value => value.Id == 1)));
            Assert.Same(second, Assert.Single(copy.GetKnownValues().Where(value => value.Id == 2)));

            mutable.Id = 5;

            Assert.Equal(4, copiedMutable.Id);
            Assert.DoesNotContain(copy.GetKnownValues(), value => value.Id == 5);
        }

        [Fact]
        public void JoiningSharedImmutableValuesDoesNotModifyInputs()
        {
            ValueSet<TestValue> source = new(new[] { new TestValue(1, mutable: false), new TestValue(2, mutable: false) });
            ValueSet<TestValue> copy = source.DeepCopy();
            ValueSet<TestValue> other = new(new TestValue(3, mutable: false));

            ValueSet<TestValue> merged = default(ValueSetLattice<TestValue>).Meet(copy, other);

            Assert.Equal(new[] { 1, 2 }, source.GetKnownValues().Select(value => value.Id).OrderBy(value => value));
            Assert.Equal(new[] { 1, 2 }, copy.GetKnownValues().Select(value => value.Id).OrderBy(value => value));
            Assert.Equal(new[] { 1, 2, 3 }, merged.GetKnownValues().Select(value => value.Id).OrderBy(value => value));
        }

        [Theory]
        [InlineData(256, false)]
        [InlineData(257, true)]
        public void JoiningSameSetPreservesTheTrackedValueLimit(int count, bool unknown)
        {
            ValueSet<int> source = new(Enumerable.Range(0, count));

            ValueSet<int> merged = default(ValueSetLattice<int>).Meet(source, source);

            Assert.Equal(unknown, merged.IsUnknown());
            if (!unknown)
                Assert.Equal(count, merged.GetKnownValues().Count());
        }

        [Fact]
        public void JoiningOversizedMutableSetNormalizesBeforeApplyingTheLimit()
        {
            TestValue[] values = Enumerable.Range(0, 257).Select(i => new TestValue(i, mutable: true)).ToArray();
            ValueSet<TestValue> source = new(values);
            foreach (TestValue value in values)
                value.Id = 0;

            ValueSet<TestValue> merged = default(ValueSetLattice<TestValue>).Meet(source, source);

            Assert.False(merged.IsUnknown());
            TestValue copiedValue = Assert.Single(merged.GetKnownValues());
            Assert.Equal(0, copiedValue.Id);
            Assert.True(merged.Contains(copiedValue));
            Assert.DoesNotContain(values, value => ReferenceEquals(value, copiedValue));
        }

        private sealed class TestValue(int id, bool mutable) : IEquatable<TestValue>, IDeepCopyValue<TestValue>
        {
            private readonly bool _mutable = mutable;

            public int Id { get; set; } = id;
            public int HashCodeCalls { get; set; }

            public TestValue DeepCopy() => _mutable ? new TestValue(Id, _mutable) : this;

            public bool Equals(TestValue? other) => other is not null && Id == other.Id && _mutable == other._mutable;

            public override bool Equals(object? obj) => obj is TestValue other && Equals(other);

            public override int GetHashCode()
            {
                HashCodeCalls++;
                return Id;
            }
        }
    }
}
