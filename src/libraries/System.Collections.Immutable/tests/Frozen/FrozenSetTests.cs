// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Tests;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;
using System.Numerics;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Frozen.Tests
{
    public abstract class FrozenSet_Generic_Tests<T> : ISet_Generic_Tests<T>
    {
        protected override bool ResetImplemented => true;

        protected override ISet<T> GenericISetFactory() => Array.Empty<T>().ToFrozenSet();

        protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations) => Array.Empty<ModifyEnumerable>();

        protected override bool IsReadOnly => true;

        protected override EnumerableOrder Order => EnumerableOrder.Unspecified;

        protected override Type ICollection_Generic_CopyTo_IndexLargerThanArrayCount_ThrowType => typeof(ArgumentOutOfRangeException);

        protected override bool Enumerator_Current_UndefinedOperation_Throws => true;

        protected virtual bool TestLargeSizes => true;

        protected virtual bool OptimizeForReading => true;

        public virtual T GetEqualValue(T value) => value;

        protected override ISet<T> GenericISetFactory(int count)
        {
            var s = new HashSet<T>();
            for (int i = 0; i < count; i++)
            {
                s.Add(CreateT(i));
            }
            return OptimizeForReading ?
                s.ToFrozenSet(GetIEqualityComparer(), true) :
                s.ToFrozenSet(GetIEqualityComparer());
        }

        [Theory]
        [InlineData(100_000)]
        public void CreateVeryLargeSet_Success(int largeCount)
        {
            if (TestLargeSizes)
            {
                GenericISetFactory(largeCount);
            }
        }

        [Fact]
        public void NullSource_ThrowsException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((HashSet<T>)null).ToFrozenSet());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((HashSet<T>)null).ToFrozenSet(false));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((HashSet<T>)null).ToFrozenSet(true));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((HashSet<T>)null).ToFrozenSet(null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((HashSet<T>)null).ToFrozenSet(null, false));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((HashSet<T>)null).ToFrozenSet(null, true));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((HashSet<T>)null).ToFrozenSet(EqualityComparer<T>.Default));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((HashSet<T>)null).ToFrozenSet(EqualityComparer<T>.Default, false));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((HashSet<T>)null).ToFrozenSet(EqualityComparer<T>.Default, true));
        }

        [Fact]
        public void EmptySource_ProducedFrozenSetEmpty()
        {
            Assert.Same(FrozenSet<T>.Empty, new List<T>().ToFrozenSet());
            Assert.Same(FrozenSet<T>.Empty, Enumerable.Empty<T>().ToFrozenSet());
            Assert.Same(FrozenSet<T>.Empty, Array.Empty<T>().ToFrozenSet());
            Assert.Same(FrozenSet<T>.Empty, new List<T>().ToFrozenSet());

            foreach (IEqualityComparer<T> comparer in new IEqualityComparer<T>[] { null, EqualityComparer<T>.Default })
            {
                Assert.Same(FrozenSet<T>.Empty, new List<T>().ToFrozenSet(comparer));
                Assert.Same(FrozenSet<T>.Empty, Enumerable.Empty<T>().ToFrozenSet(comparer));
                Assert.Same(FrozenSet<T>.Empty, Array.Empty<T>().ToFrozenSet(comparer));
                Assert.Same(FrozenSet<T>.Empty, new List<T>().ToFrozenSet(comparer));

                Assert.Same(FrozenSet<T>.Empty, new List<T>().ToFrozenSet(comparer, OptimizeForReading));
                Assert.Same(FrozenSet<T>.Empty, Enumerable.Empty<T>().ToFrozenSet(comparer, OptimizeForReading));
                Assert.Same(FrozenSet<T>.Empty, Array.Empty<T>().ToFrozenSet(comparer, OptimizeForReading));
                Assert.Same(FrozenSet<T>.Empty, new List<T>().ToFrozenSet(comparer, OptimizeForReading));
            }

            Assert.NotSame(FrozenSet<T>.Empty, new List<T>().ToFrozenSet(NonDefaultEqualityComparer<T>.Instance));
            Assert.NotSame(FrozenSet<T>.Empty, Enumerable.Empty<T>().ToFrozenSet(NonDefaultEqualityComparer<T>.Instance));
            Assert.NotSame(FrozenSet<T>.Empty, Array.Empty<T>().ToFrozenSet(NonDefaultEqualityComparer<T>.Instance));
            Assert.NotSame(FrozenSet<T>.Empty, new List<T>().ToFrozenSet(NonDefaultEqualityComparer<T>.Instance));

            Assert.NotSame(FrozenSet<T>.Empty, new List<T>().ToFrozenSet(NonDefaultEqualityComparer<T>.Instance, OptimizeForReading));
            Assert.NotSame(FrozenSet<T>.Empty, Enumerable.Empty<T>().ToFrozenSet(NonDefaultEqualityComparer<T>.Instance, OptimizeForReading));
            Assert.NotSame(FrozenSet<T>.Empty, Array.Empty<T>().ToFrozenSet(NonDefaultEqualityComparer<T>.Instance, OptimizeForReading));
            Assert.NotSame(FrozenSet<T>.Empty, new List<T>().ToFrozenSet(NonDefaultEqualityComparer<T>.Instance, OptimizeForReading));
        }

        [Fact]
        public void EmptyFrozenSet_Idempotent()
        {
            FrozenSet<T> empty = FrozenSet<T>.Empty;

            Assert.NotNull(empty);
            Assert.Same(empty, FrozenSet<T>.Empty);
        }

        [Fact]
        public void EmptyFrozenSet_OperationsAreNops()
        {
            FrozenSet<T> empty = FrozenSet<T>.Empty;

            Assert.Same(EqualityComparer<T>.Default, empty.Comparer);
            Assert.Equal(0, empty.Count);
            Assert.Empty(empty.Items);

            T item = CreateT(0);
            Assert.False(empty.Contains(item));

            empty.CopyTo(Span<T>.Empty);
            T[] array = new T[1];
            empty.CopyTo(array);
            Assert.Equal(default, array[0]);

            int count = 0;
            foreach (T value in empty)
            {
                count++;
            }
            Assert.Equal(0, count);
        }

        [Fact]
        public void FrozenSet_ToFrozenSet_Idempotent()
        {
            Assert.Same(FrozenSet<T>.Empty, FrozenSet<T>.Empty.ToFrozenSet());
            Assert.Same(FrozenSet<T>.Empty, FrozenSet<T>.Empty.ToFrozenSet(null));
            Assert.Same(FrozenSet<T>.Empty, FrozenSet<T>.Empty.ToFrozenSet(null, false));
            Assert.Same(FrozenSet<T>.Empty, FrozenSet<T>.Empty.ToFrozenSet(null, true));
            Assert.Same(FrozenSet<T>.Empty, FrozenSet<T>.Empty.ToFrozenSet(false));
            Assert.Same(FrozenSet<T>.Empty, FrozenSet<T>.Empty.ToFrozenSet(true));
            Assert.Same(FrozenSet<T>.Empty, FrozenSet<T>.Empty.ToFrozenSet(EqualityComparer<T>.Default));
            Assert.Same(FrozenSet<T>.Empty, FrozenSet<T>.Empty.ToFrozenSet(EqualityComparer<T>.Default, false));
            Assert.Same(FrozenSet<T>.Empty, FrozenSet<T>.Empty.ToFrozenSet(EqualityComparer<T>.Default, true));

            Assert.NotSame(FrozenSet<T>.Empty, FrozenSet<T>.Empty.ToFrozenSet(NonDefaultEqualityComparer<T>.Instance));
            Assert.NotSame(FrozenSet<T>.Empty, FrozenSet<T>.Empty.ToFrozenSet(NonDefaultEqualityComparer<T>.Instance, false));
            Assert.NotSame(FrozenSet<T>.Empty, FrozenSet<T>.Empty.ToFrozenSet(NonDefaultEqualityComparer<T>.Instance, true));

            FrozenSet<T> frozen = new HashSet<T>() { { CreateT(0) } }.ToFrozenSet();
            Assert.Same(frozen, frozen.ToFrozenSet());
            Assert.NotSame(frozen, frozen.ToFrozenSet(NonDefaultEqualityComparer<T>.Instance));
        }

        [Fact]
        public void ToFrozenSet_BoolArg_UsesDefaultComparer()
        {
            HashSet<T> source = new HashSet<T>(Enumerable.Range(0, 4).Select(CreateT));

            FrozenSet<T> frozen = source.ToFrozenSet(OptimizeForReading);

            Assert.Same(EqualityComparer<T>.Default, frozen.Comparer);
        }

        public static IEnumerable<object[]> LookupItems_AllItemsFoundAsExpected_MemberData() =>
            from size in new[] { 0, 1, 2, 10, 99 }
            from comparer in new IEqualityComparer<T>[] { null, EqualityComparer<T>.Default, NonDefaultEqualityComparer<T>.Instance }
            from specifySameComparer in new[] { false, true }
            select new object[] { size, comparer, specifySameComparer };

        [Theory]
        [MemberData(nameof(LookupItems_AllItemsFoundAsExpected_MemberData))]
        public void LookupItems_AllItemsFoundAsExpected(int size, IEqualityComparer<T> comparer, bool specifySameComparer)
        {
            HashSet<T> original = new HashSet<T>(Enumerable.Range(0, size).Select(CreateT), comparer);
            T[] originalItems = original.ToArray();

            FrozenSet<T> frozen = (specifySameComparer, OptimizeForReading) switch
            {
                (false, false) => original.ToFrozenSet(),
                (false, true) => original.ToFrozenSet(null, true),
                (true, false) => original.ToFrozenSet(comparer),
                (true, true) => original.ToFrozenSet(comparer, true),
            };

            // Make sure creating the frozen set didn't alter the original
            Assert.Equal(originalItems.Length, original.Count);
            Assert.All(originalItems, p => Assert.True(frozen.Contains(p)));

            // Make sure the frozen set matches the original
            Assert.Equal(original.Count, frozen.Count);
            Assert.Equal(original, new HashSet<T>(frozen));
            Assert.Equal(original, new HashSet<T>(frozen.Items));
            Assert.All(originalItems, p => Assert.True(frozen.Contains(p)));
            if (specifySameComparer ||
                comparer is null ||
                comparer == EqualityComparer<T>.Default)
            {
                Assert.Equal(original.Comparer, frozen.Comparer);
            }

            // Generate additional items and ensure they match iff the original matches.
            for (int i = size; i < size + 100; i++)
            {
                T item = CreateT(i);
                Assert.Equal(original.Contains(item), frozen.Contains(item));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EqualButPossiblyDifferentValuesFound(bool fromSet)
        {
            HashSet<T> original = new HashSet<T>(Enumerable.Range(0, 50).Select(CreateT), GetIEqualityComparer());

            FrozenSet<T> frozen = fromSet ?
                original.ToFrozenSet(GetIEqualityComparer()) :
                original.Select(v => v).ToFrozenSet(GetIEqualityComparer());

            foreach (T key in original)
            {
                Assert.True(original.Contains(key));
                Assert.True(frozen.Contains(key));

                T equalKey = GetEqualValue(key);
                Assert.True(original.Contains(equalKey));
                Assert.True(frozen.Contains(equalKey));
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(5000)]
        public void ComparingWithOtherSets(int size)
        {
            if (size > 10 && !TestLargeSizes)
            {
                return;
            }

            foreach (IEqualityComparer<T> comparer in new IEqualityComparer<T>[] { EqualityComparer<T>.Default })//, NonDefaultEqualityComparer<T>.Instance })
            {
                IEqualityComparer<T> otherComparer = ReferenceEquals(comparer, EqualityComparer<T>.Default) ? NonDefaultEqualityComparer<T>.Instance : EqualityComparer<T>.Default;

                HashSet<T> source = new HashSet<T>(comparer);
                for (int i = 0; source.Count < size ; i++)
                {
                    source.Add(CreateT(i));
                }

                FrozenSet<T> frozen = OptimizeForReading ?
                    source.ToFrozenSet(source.Comparer, true) :
                    source.ToFrozenSet(source.Comparer);

                Assert.True(frozen.SetEquals(source));
                Assert.True(frozen.SetEquals(FrozenSet.ToFrozenSet(source, comparer)));
                Assert.True(frozen.SetEquals(source.ToImmutableHashSet(comparer)));
                Assert.True(frozen.SetEquals(source.Select(i => i)));
                Assert.True(frozen.SetEquals(new HashSet<T>(source, otherComparer)));
                Assert.True(frozen.SetEquals(FrozenSet.ToFrozenSet(source, otherComparer)));
                Assert.True(frozen.SetEquals(source.ToImmutableHashSet(otherComparer)));
                Assert.False(frozen.SetEquals(new HashSet<T>(InvertingComparer.Instance)));
                Assert.False(frozen.SetEquals(FrozenSet.ToFrozenSet(source, InvertingComparer.Instance)));
                Assert.False(frozen.SetEquals(source.ToImmutableHashSet(InvertingComparer.Instance)));
                Assert.False(frozen.SetEquals(new EmptySet()));

                Assert.True(frozen.IsSubsetOf(source));
                Assert.True(frozen.IsSubsetOf(FrozenSet.ToFrozenSet(source, comparer)));
                Assert.True(frozen.IsSubsetOf(source.ToImmutableHashSet(comparer)));
                Assert.True(frozen.IsSubsetOf(source.Select(i => i)));
                Assert.True(frozen.IsSubsetOf(new HashSet<T>(source, otherComparer)));
                Assert.True(frozen.IsSubsetOf(FrozenSet.ToFrozenSet(source, otherComparer)));
                Assert.True(frozen.IsSubsetOf(source.ToImmutableHashSet(otherComparer)));
                Assert.False(frozen.IsSubsetOf(new HashSet<T>(InvertingComparer.Instance)));
                Assert.False(frozen.IsSubsetOf(FrozenSet.ToFrozenSet(source, InvertingComparer.Instance)));
                Assert.False(frozen.IsSubsetOf(source.ToImmutableHashSet(InvertingComparer.Instance)));
                Assert.False(frozen.IsSubsetOf(new EmptySet()));

                Assert.True(frozen.IsSupersetOf(source));
                Assert.True(frozen.IsSupersetOf(FrozenSet.ToFrozenSet(source, comparer)));
                Assert.True(frozen.IsSupersetOf(source.ToImmutableHashSet(comparer)));
                Assert.True(frozen.IsSupersetOf(source.Select(i => i)));
                Assert.True(frozen.IsSupersetOf(new HashSet<T>(source, otherComparer)));
                Assert.True(frozen.IsSupersetOf(FrozenSet.ToFrozenSet(source, otherComparer)));
                Assert.True(frozen.IsSupersetOf(source.ToImmutableHashSet(otherComparer)));
                Assert.True(frozen.IsSupersetOf(new HashSet<T>(InvertingComparer.Instance)));
                Assert.True(frozen.IsSupersetOf(FrozenSet.ToFrozenSet(source, InvertingComparer.Instance)));
                Assert.True(frozen.IsSupersetOf(source.ToImmutableHashSet(InvertingComparer.Instance)));
                Assert.True(frozen.IsSupersetOf(new EmptySet()));

                Assert.False(frozen.IsProperSubsetOf(source));
                Assert.False(frozen.IsProperSubsetOf(FrozenSet.ToFrozenSet(source, comparer)));
                Assert.False(frozen.IsProperSubsetOf(source.ToImmutableHashSet(comparer)));
                Assert.False(frozen.IsProperSubsetOf(source.Select(i => i)));
                Assert.False(frozen.IsProperSubsetOf(new HashSet<T>(source, otherComparer)));
                Assert.False(frozen.IsProperSubsetOf(FrozenSet.ToFrozenSet(source, otherComparer)));
                Assert.False(frozen.IsProperSubsetOf(source.ToImmutableHashSet(otherComparer)));
                Assert.False(frozen.IsProperSubsetOf(new HashSet<T>(InvertingComparer.Instance)));
                Assert.False(frozen.IsProperSubsetOf(FrozenSet.ToFrozenSet(source, InvertingComparer.Instance)));
                Assert.False(frozen.IsProperSubsetOf(source.ToImmutableHashSet(InvertingComparer.Instance)));
                Assert.False(frozen.IsProperSubsetOf(new EmptySet()));

                Assert.False(frozen.IsProperSupersetOf(source));
                Assert.False(frozen.IsProperSupersetOf(FrozenSet.ToFrozenSet(source, comparer)));
                Assert.False(frozen.IsProperSupersetOf(source.ToImmutableHashSet(comparer)));
                Assert.False(frozen.IsProperSupersetOf(source.Select(i => i)));
                Assert.True(frozen.IsProperSupersetOf(source.Select(i => i).Skip(1)));
                Assert.False(frozen.IsProperSupersetOf(new HashSet<T>(source, otherComparer)));
                Assert.False(frozen.IsProperSupersetOf(FrozenSet.ToFrozenSet(source, otherComparer)));
                Assert.False(frozen.IsProperSupersetOf(source.ToImmutableHashSet(otherComparer)));
                Assert.True(frozen.IsProperSupersetOf(new HashSet<T>(InvertingComparer.Instance)));
                Assert.True(frozen.IsProperSupersetOf(FrozenSet.ToFrozenSet(source, InvertingComparer.Instance)));
                Assert.True(frozen.IsProperSupersetOf(source.ToImmutableHashSet(InvertingComparer.Instance)));
                Assert.True(frozen.IsProperSupersetOf(new EmptySet()));
            }
        }

        private sealed class InvertingComparer : IEqualityComparer<T>
        {
            public static InvertingComparer Instance { get; } = new InvertingComparer();
            public bool Equals(T? x, T? y) => !EqualityComparer<T>.Default.Equals(x, y);
            public int GetHashCode([DisallowNull] T obj) => 0;
        }

        private sealed class EmptySet :
            ISet<T>
#if NET5_0_OR_GREATER
            , IReadOnlySet<T>
#endif
        {
            public int Count => 0;
            public bool IsReadOnly => true;
            public bool Contains(T item) => false;
            public void CopyTo(T[] array, int arrayIndex) { }
            public bool Overlaps(IEnumerable<T> other) => false;

            public bool IsProperSubsetOf(IEnumerable<T> other) => false;
            public bool IsProperSupersetOf(IEnumerable<T> other) => false;
            public bool IsSubsetOf(IEnumerable<T> other) => true;
            public bool IsSupersetOf(IEnumerable<T> other) => !other.Any();
            public bool SetEquals(IEnumerable<T> other) => !other.Any();

            public IEnumerator<T> GetEnumerator() { yield break; }
            IEnumerator IEnumerable.GetEnumerator() { yield break; }

            public bool Add(T item) => throw new NotImplementedException();
            void ICollection<T>.Add(T item) => throw new NotImplementedException();
            public bool Remove(T item) => throw new NotImplementedException();
            public void Clear() => throw new NotImplementedException();
            public void UnionWith(IEnumerable<T> other) => throw new NotImplementedException();
            public void ExceptWith(IEnumerable<T> other) => throw new NotImplementedException();
            public void IntersectWith(IEnumerable<T> other) => throw new NotImplementedException();
            public void SymmetricExceptWith(IEnumerable<T> other) => throw new NotImplementedException();
        }
    }

    public abstract class FrozenSet_Generic_Tests_string : FrozenSet_Generic_Tests<string>
    {
        protected override string CreateT(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes1 = new byte[stringLength];
            rand.NextBytes(bytes1);
            return Convert.ToBase64String(bytes1);
        }
    }

    public class FrozenSet_Generic_Tests_string_Default : FrozenSet_Generic_Tests_string
    {
        protected override IEqualityComparer<string> GetIEqualityComparer() => EqualityComparer<string>.Default;
    }

    public class FrozenSet_Generic_Tests_string_Ordinal : FrozenSet_Generic_Tests_string
    {
        protected override IEqualityComparer<string> GetIEqualityComparer() => StringComparer.Ordinal;
    }

    public class FrozenSet_Generic_Tests_string_OrdinalIgnoreCase_ReadingUnoptimized : FrozenSet_Generic_Tests_string_OrdinalIgnoreCase
    {
        protected override bool OptimizeForReading => false;
    }

    public class FrozenSet_Generic_Tests_string_OrdinalIgnoreCase : FrozenSet_Generic_Tests_string
    {
        protected override IEqualityComparer<string> GetIEqualityComparer() => StringComparer.OrdinalIgnoreCase;

        public override string GetEqualValue(string value) => value.ToLowerInvariant();

        [Fact]
        public void TryGetValue_FindsExpectedResult()
        {
            FrozenSet<string> frozen = new[] { "abc" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

            Assert.False(frozen.TryGetValue("ab", out string actualValue));
            Assert.Null(actualValue);

            Assert.True(frozen.TryGetValue("ABC", out actualValue));
            Assert.Equal("abc", actualValue);
        }
    }

    public class FrozenSet_Generic_Tests_string_NonDefault : FrozenSet_Generic_Tests_string
    {
        protected override IEqualityComparer<string> GetIEqualityComparer() => NonDefaultEqualityComparer<string>.Instance;
    }

    public class FrozenSet_Generic_Tests_ulong : FrozenSet_Generic_Tests<ulong>
    {
        protected override bool DefaultValueAllowed => true;

        protected override ulong CreateT(int seed)
        {
            Random rand = new Random(seed);
            ulong hi = unchecked((ulong)rand.Next());
            ulong lo = unchecked((ulong)rand.Next());
            return (hi << 32) | lo;
        }
    }

    public class FrozenSet_Generic_Tests_int_ReadingUnoptimized : FrozenSet_Generic_Tests_int
    {
        protected override bool OptimizeForReading => false;
    }

    public class FrozenSet_Generic_Tests_int : FrozenSet_Generic_Tests<int>
    {
        protected override bool DefaultValueAllowed => true;

        protected override int CreateT(int seed) => new Random(seed).Next();
    }

    public class FrozenSet_Generic_Tests_SimpleClass : FrozenSet_Generic_Tests<SimpleClass>
    {
        protected override SimpleClass CreateT(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes1 = new byte[stringLength];
            rand.NextBytes(bytes1);
            return new SimpleClass { Value = Convert.ToBase64String(bytes1) };
        }
    }

    public class FrozenSet_Generic_Tests_SimpleStruct : FrozenSet_Generic_Tests<SimpleStruct>
    {
        protected override SimpleStruct CreateT(int seed) => new SimpleStruct { Value = seed + 1 };

        protected override bool TestLargeSizes => false; // hash code contention leads to longer running times
    }

    public class FrozenSet_Generic_Tests_SimpleNonComparableStruct : FrozenSet_Generic_Tests<SimpleNonComparableStruct>
    {
        protected override SimpleNonComparableStruct CreateT(int seed) => new SimpleNonComparableStruct { Value = seed + 1 };

        protected override bool TestLargeSizes => false; // hash code contention leads to longer running times
    }

    public class FrozenSet_Generic_Tests_ValueTupleSimpleNonComparableStruct : FrozenSet_Generic_Tests<ValueTuple<SimpleNonComparableStruct,SimpleNonComparableStruct>>
    {
        protected override ValueTuple<SimpleNonComparableStruct, SimpleNonComparableStruct> CreateT(int seed) =>
            new ValueTuple<SimpleNonComparableStruct, SimpleNonComparableStruct>(
                new SimpleNonComparableStruct { Value = seed + 1 },
                new SimpleNonComparableStruct { Value = seed + 1 });

        protected override bool TestLargeSizes => false; // hash code contention leads to longer running times
    }

    public class FrozenSet_NonGeneric_Tests : ICollection_NonGeneric_Tests
    {
        protected override ICollection NonGenericICollectionFactory() =>
            Array.Empty<object>().ToFrozenSet();

        protected override ICollection NonGenericICollectionFactory(int count)
        {
            var set = new HashSet<object>();
            var rand = new Random(42);
            while (set.Count < count)
            {
                set.Add(rand.Next().ToString(CultureInfo.InvariantCulture));
            }
            return set.ToFrozenSet();
        }

        protected override bool IsReadOnly => true;

        protected override bool Enumerator_Current_UndefinedOperation_Throws => true;

        protected override bool ResetImplemented => true;

        protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations) => Array.Empty<ModifyEnumerable>();

        protected override void AddToCollection(ICollection collection, int numberOfItemsToAdd) => throw new NotImplementedException();

        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectReferenceType_ThrowType => typeof(InvalidCastException);

        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectValueType_ThrowType => typeof(InvalidCastException);

        protected override Type ICollection_NonGeneric_CopyTo_NonZeroLowerBound_ThrowType => typeof(ArgumentOutOfRangeException);

        [Fact]
        public void Sparse_LookupItems_AlltemsFoundAsExpected()
        {
            foreach (int size in new[] { 1, 2, 10, 63, 64, 65, 999, 1024 })
            {
                foreach (int skip in new[] { 2, 3, 5 })
                {
                    var original = new HashSet<int>(Enumerable.Range(-3, size).Where(i => i % skip == 0));
                    FrozenSet<int> frozen = original.ToFrozenSet();

                    for (int i = -10; i <= size + 66; i++)
                    {
                        Assert.Equal(original.Contains(i), frozen.Contains(i));
                    }
                }
            }
        }

        [Fact]
        public void ClosedRange_Lookup_AllItemsFoundAsExpected()
        {
            foreach (long start in new long[]
                {
                    long.MinValue,
                    (long)int.MinValue - 1,
                    int.MinValue,
                    -1,
                    0,
                    1,
                    int.MaxValue - 1,
                    int.MaxValue,
                    (long)int.MaxValue + 1,
                    uint.MaxValue - 1,
                    uint.MaxValue,
                    (long)uint.MaxValue + 1,
                    long.MaxValue - 5
                })
            {
                foreach (long size in new[] { 1, 2, 5 })
                {
                    var original = new HashSet<long>();

                    long min = start;
                    long max = start + size - 1;
                    for (long i = min; i != max; i++)
                    {
                        original.Add(i);
                    }

                    FrozenSet<long> frozen = original.ToFrozenSet();

                    min = start > long.MinValue ? start - 10 : start;
                    max = start + size - 1 < long.MaxValue ? start + size + 9 : start + size - 1;
                    for (long i = min; i != max; i++)
                    {
                        Assert.Equal(original.Contains(i), frozen.Contains(i));
                    }
                }
            }
        }
    }
}
