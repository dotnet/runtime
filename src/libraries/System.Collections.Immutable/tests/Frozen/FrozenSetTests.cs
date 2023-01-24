// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Tests;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;
using System.Numerics;

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

        protected override ISet<T> GenericISetFactory(int count)
        {
            var s = new HashSet<T>();
            for (int i = 0; i < count; i++)
            {
                s.Add(CreateT(i));
            }
            return s.ToFrozenSet(GetIEqualityComparer());
        }

        [Theory]
        [InlineData(100_000)]
        public void CreateVeryLargeSet_Success(int largeCount)
        {
            GenericISetFactory(largeCount);
        }

        [Fact]
        public void NullSource_ThrowsException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((HashSet<T>)null).ToFrozenSet());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((HashSet<T>)null).ToFrozenSet(null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((HashSet<T>)null).ToFrozenSet(EqualityComparer<T>.Default));
        }

        [Fact]
        public void EmptySource_ProducedFrozenSetEmpty()
        {
            Assert.Same(FrozenSet<T>.Empty, new List<T>().ToFrozenSet());
            Assert.Same(FrozenSet<T>.Empty, Enumerable.Empty<T>().ToFrozenSet());
            Assert.Same(FrozenSet<T>.Empty, Array.Empty<T>().ToFrozenSet());
            Assert.Same(FrozenSet<T>.Empty, new List<T>().ToFrozenSet());

            foreach (IEqualityComparer<T> comparer in new IEqualityComparer<T>[] { null, EqualityComparer<T>.Default, NonDefaultEqualityComparer<T>.Instance })
            {
                Assert.Same(FrozenSet<T>.Empty, new List<T>().ToFrozenSet(comparer));
                Assert.Same(FrozenSet<T>.Empty, Enumerable.Empty<T>().ToFrozenSet(comparer));
                Assert.Same(FrozenSet<T>.Empty, Array.Empty<T>().ToFrozenSet(comparer));
                Assert.Same(FrozenSet<T>.Empty, new List<T>().ToFrozenSet(comparer));
            }
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
            foreach (IEqualityComparer<T> comparer in new IEqualityComparer<T>[] { null, EqualityComparer<T>.Default, NonDefaultEqualityComparer<T>.Instance })
            {
                Assert.Same(FrozenSet<T>.Empty, FrozenSet<T>.Empty.ToFrozenSet(comparer));
            }

            FrozenSet<T> frozen = new HashSet<T>() { { CreateT(0) } }.ToFrozenSet();
            Assert.Same(frozen, frozen.ToFrozenSet());
            Assert.NotSame(frozen, frozen.ToFrozenSet(NonDefaultEqualityComparer<T>.Instance));
        }

        public static IEnumerable<object[]> LookupItems_AllItemsFoundAsExpected_MemberData()
        {
            foreach (int size in new[] { 1, 2, 10, 999, 1024 })
            {
                foreach (IEqualityComparer<T> comparer in new IEqualityComparer<T>[] { null, EqualityComparer<T>.Default, NonDefaultEqualityComparer<T>.Instance })
                {
                    foreach (bool specifySameComparer in new[] { false, true })
                    {
                        yield return new object[] { size, comparer, specifySameComparer };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(LookupItems_AllItemsFoundAsExpected_MemberData))]
        public void LookupItems_AllItemsFoundAsExpected(int size, IEqualityComparer<T> comparer, bool specifySameComparer)
        {
            HashSet<T> original = new HashSet<T>(Enumerable.Range(0, size).Select(CreateT), comparer);
            T[] originalItems = original.ToArray();

            FrozenSet<T> frozen = specifySameComparer ?
                original.ToFrozenSet(comparer) :
                original.ToFrozenSet();

            // Make sure creating the frozen set didn't alter the original
            Assert.Equal(originalItems.Length, original.Count);
            Assert.All(originalItems, p => Assert.True(frozen.Contains(p)));

            // Make sure the frozen set matches the original
            Assert.Equal(original.Count, frozen.Count);
            Assert.Equal(original, new HashSet<T>(frozen));
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

    public class FrozenSet_Generic_Tests_string_OrdinalIgnoreCase : FrozenSet_Generic_Tests_string
    {
        protected override IEqualityComparer<string> GetIEqualityComparer() => StringComparer.OrdinalIgnoreCase;

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

#if NET7_0_OR_GREATER
        [Theory]
        [InlineData(new int[] { 0 })]
        [InlineData(new int[] { 0, 1 })]
        [InlineData(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })]
        [InlineData(new int[] { 0, 2, 4, 6, 8, 10 })]
        [InlineData(new int[] { 1, 2, 4, 6, 8, 10 })]
        public void FrozenIntegerSet_SanityCheck(int[] values)
        {
            RunIntegerSetTests<int>(values);
            RunIntegerSetTests<uint>(values.Select(x => (uint)x).ToArray());
            RunIntegerSetTests<long>(values.Select(x => (long)x).ToArray());
            RunIntegerSetTests<ulong>(values.Select(x => (ulong)x).ToArray());
            RunIntegerSetTests<short>(values.Select(x => (short)x).ToArray());
            RunIntegerSetTests<ushort>(values.Select(x => (ushort)x).ToArray());
            RunIntegerSetTests<byte>(values.Select(x => (byte)x).ToArray());
            RunIntegerSetTests<sbyte>(values.Select(x => (sbyte)x).ToArray());
        }

        private void RunIntegerSetTests<T>(T[] values)
            where T : struct, IBinaryInteger<T>
        {
            for (int i = 0; i < 2; i++)
            {
                FrozenSet<T> s = i switch
                {
                    0 => s = new SmallIntegerFrozenSet<T>(values),
                    _ => s = new SparseRangeIntegerFrozenSet<T>(values),
                };

                HashSet<T> hs = new();
                foreach (T value in values)
                {
                    hs.Add(value);
                }

                Assert.Equal(values.Length, hs.Count);
                Assert.Equal(values.Length, s.Count);

                Assert.True(s.SetEquals(hs));
                Assert.True(hs.SetEquals(s));

                Assert.True(s.Overlaps(hs));
                Assert.True(hs.Overlaps(s));

                foreach (T v in hs)
                {
                    s.Contains(v);
                }
            }
        }
#endif

#if !NET7_0_OR_GREATER
        [Theory]
        [InlineData(new int[] { 0 })]
        [InlineData(new int[] { 0, 1 })]
        [InlineData(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })]
        [InlineData(new int[] { 0, 2, 4, 6, 8, 10 })]
        [InlineData(new int[] { -1, 0, 2, 4, 6, 8, 10 })]
        public void FrozenInt32Set_SanityCheck(int[] values)
        {
            for (int i = 0; i < 3; i++)
            {
                FrozenSet<int> s = i switch
                {
                    0 => s = new SmallInt32FrozenSet(values),
                    1 => s = new SparseRangeInt32FrozenSet(values),
                    _ => s = new Int32FrozenSet(values),
                };

                HashSet<int> hs = new();
                foreach (int value in values)
                {
                    hs.Add(value);
                }

                Assert.Equal(values.Length, hs.Count);
                Assert.Equal(values.Length, s.Count);

                Assert.True(s.SetEquals(hs));
                Assert.True(hs.SetEquals(s));

                Assert.True(s.Overlaps(hs));
                Assert.True(hs.Overlaps(s));

                for (int v = -1; v < 12; v++)
                {
                    Assert.Equal(hs.Contains(v), s.Contains(v));
                }
            }
        }
#endif
    }
}
