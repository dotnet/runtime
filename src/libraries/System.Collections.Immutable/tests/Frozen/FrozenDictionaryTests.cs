// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Tests;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Collections.Frozen.Tests
{
    public abstract class FrozenDictionary_Generic_Tests<TKey, TValue> : IDictionary_Generic_Tests<TKey, TValue>
    {
        protected override bool IsReadOnly => true;
        protected override bool AddRemoveClear_ThrowsNotSupported => true;
        protected override bool Enumerator_Current_UndefinedOperation_Throws => true;
        protected override Type ICollection_Generic_CopyTo_IndexLargerThanArrayCount_ThrowType => typeof(ArgumentOutOfRangeException);

        protected virtual bool AllowVeryLargeSizes => true;
        protected virtual int MaxUniqueValueCount => int.MaxValue;

        public virtual TKey GetEqualKey(TKey key) => key;

        protected override IDictionary<TKey, TValue> GenericIDictionaryFactory(int count)
            => GenerateUniqueKeyValuePairs(count).ToFrozenDictionary(GetKeyIEqualityComparer());

        protected KeyValuePair<TKey, TValue>[] GenerateUniqueKeyValuePairs(int count)
        {
            if (count > MaxUniqueValueCount)
            {
                throw new NotSupportedException($"It's impossible to create {count} unique values for {typeof(TKey)} keys.");
            }

            Dictionary<TKey, TValue> dictionary = new();
            int seed = 0;
            while (dictionary.Count != count)
            {
                TKey key = CreateTKey(seed);
                if (!dictionary.ContainsKey(key))
                {
                    dictionary.Add(key, CreateTValue(seed));
                }
                seed++;
            }

            return dictionary.ToArray();
        }

        protected override IDictionary<TKey, TValue> GenericIDictionaryFactory() => Enumerable.Empty<KeyValuePair<TKey, TValue>>().ToFrozenDictionary();

        protected override IDictionary<TKey, TValue> GenericIDictionaryFactory(IEqualityComparer<TKey> comparer) => Enumerable.Empty<KeyValuePair<TKey, TValue>>().ToFrozenDictionary(comparer);

        protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations) => new List<ModifyEnumerable>();

        protected override bool ResetImplemented => true;
        protected override bool IDictionary_Generic_Keys_Values_Enumeration_ResetImplemented => true;

        protected override EnumerableOrder Order => EnumerableOrder.Unspecified;

        [Theory]
        [InlineData(100_000)]
        public virtual void CreateVeryLargeDictionary_Success(int largeCount)
        {
            if (AllowVeryLargeSizes)
            {
                GenericIDictionaryFactory(largeCount);
            }
        }

        [Fact]
        public void NullSource_ThrowsException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((Dictionary<TKey, TValue>)null).ToFrozenDictionary());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((Dictionary<TKey, TValue>)null).ToFrozenDictionary(null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((Dictionary<TKey, TValue>)null).ToFrozenDictionary(EqualityComparer<TKey>.Default));

            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => Enumerable.Empty<int>().ToFrozenDictionary((Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => Enumerable.Empty<int>().ToFrozenDictionary((Func<int, int>)null, EqualityComparer<int>.Default));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => Enumerable.Empty<int>().ToFrozenDictionary((Func<int, int>)null, (Func<int, int>)null, EqualityComparer<int>.Default));

            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => Enumerable.Empty<int>().ToFrozenDictionary(i => i, (Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("elementSelector", () => Enumerable.Empty<int>().ToFrozenDictionary(i => i, (Func<int, int>)null, EqualityComparer<int>.Default));
        }

        [Fact]
        public void EmptySource_ProducedFrozenDictionaryEmpty()
        {
            IEnumerable<KeyValuePair<TKey, TValue>>[] sources = new[]
            {
                new Dictionary<TKey, TValue>(),
                Enumerable.Empty<KeyValuePair<TKey, TValue>>(),
                Array.Empty<KeyValuePair<TKey, TValue>>(),
                new List<KeyValuePair<TKey, TValue>>()
            };

            foreach (IEnumerable<KeyValuePair<TKey, TValue>> source in sources)
            {
                Assert.Same(FrozenDictionary<TKey, TValue>.Empty, source.ToFrozenDictionary());
                Assert.Same(FrozenDictionary<TKey, KeyValuePair<TKey, TValue>>.Empty, source.ToFrozenDictionary(s => s.Key));
                Assert.Same(FrozenDictionary<TKey, TValue>.Empty, source.ToFrozenDictionary(s => s.Key, s => s.Value));

                foreach (IEqualityComparer<TKey> comparer in new IEqualityComparer<TKey>[] { null, EqualityComparer<TKey>.Default })
                {
                    Assert.Same(FrozenDictionary<TKey, TValue>.Empty, source.ToFrozenDictionary(comparer));
                    Assert.Same(FrozenDictionary<TKey, KeyValuePair<TKey, TValue>>.Empty, source.ToFrozenDictionary(s => s.Key, comparer));
                    Assert.Same(FrozenDictionary<TKey, TValue>.Empty, source.ToFrozenDictionary(s => s.Key, s => s.Value, comparer));
                }

                Assert.NotSame(FrozenDictionary<TKey, TValue>.Empty, source.ToFrozenDictionary(NonDefaultEqualityComparer<TKey>.Instance));
                Assert.NotSame(FrozenDictionary<TKey, KeyValuePair<TKey, TValue>>.Empty, source.ToFrozenDictionary(s => s.Key, NonDefaultEqualityComparer<TKey>.Instance));
                Assert.NotSame(FrozenDictionary<TKey, TValue>.Empty, source.ToFrozenDictionary(s => s.Key, s => s.Value, NonDefaultEqualityComparer<TKey>.Instance));

                Assert.Equal(0, source.ToFrozenDictionary(NonDefaultEqualityComparer<TKey>.Instance).Count);
                Assert.Equal(0, source.ToFrozenDictionary(s => s.Key, NonDefaultEqualityComparer<TKey>.Instance).Count);
                Assert.Equal(0, source.ToFrozenDictionary(s => s.Key, s => s.Value, NonDefaultEqualityComparer<TKey>.Instance).Count);
            }
        }

        [Fact]
        public void EmptyFrozenDictionary_Idempotent()
        {
            FrozenDictionary<TKey, TValue> empty = FrozenDictionary<TKey, TValue>.Empty;

            Assert.NotNull(empty);
            Assert.Same(empty, FrozenDictionary<TKey, TValue>.Empty);
        }

        [Fact]
        public void EmptyFrozenDictionary_OperationsAreNops()
        {
            FrozenDictionary<TKey, TValue> empty = FrozenDictionary<TKey, TValue>.Empty;

            Assert.Same(EqualityComparer<TKey>.Default, empty.Comparer);
            Assert.Equal(0, empty.Count);
            Assert.Empty(empty.Keys);
            Assert.Empty(empty.Values);

            TKey key = CreateTKey(0);
            Assert.False(empty.ContainsKey(key));
            Assert.False(empty.TryGetValue(key, out TValue value));
            Assert.Equal(default, value);
            Assert.True(Unsafe.IsNullRef(ref Unsafe.AsRef(in empty.GetValueRefOrNullRef(key))));
            Assert.Throws<KeyNotFoundException>(() => empty[key]);

            empty.CopyTo(Span<KeyValuePair<TKey, TValue>>.Empty);
            KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[1];
            empty.CopyTo(array);
            Assert.Equal(default, array[0]);

            int count = 0;
            foreach (KeyValuePair<TKey, TValue> pair in empty)
            {
                count++;
            }
            Assert.Equal(0, count);
        }

        [Fact]
        public void FrozenDictionary_ToFrozenDictionary_Idempotent()
        {
            Assert.Same(FrozenDictionary<TKey, TValue>.Empty, FrozenDictionary<TKey, TValue>.Empty.ToFrozenDictionary());
            Assert.Same(FrozenDictionary<TKey, TValue>.Empty, FrozenDictionary<TKey, TValue>.Empty.ToFrozenDictionary(null));
            Assert.Same(FrozenDictionary<TKey, TValue>.Empty, FrozenDictionary<TKey, TValue>.Empty.ToFrozenDictionary(EqualityComparer<TKey>.Default));

            Assert.NotSame(FrozenDictionary<TKey, TValue>.Empty, FrozenDictionary<TKey, TValue>.Empty.ToFrozenDictionary(NonDefaultEqualityComparer<TKey>.Instance));

            FrozenDictionary<TKey, TValue> frozen = new Dictionary<TKey, TValue>() { { CreateTKey(0), CreateTValue(0) } }.ToFrozenDictionary();
            Assert.Same(frozen, frozen.ToFrozenDictionary());
            Assert.NotSame(frozen, frozen.ToFrozenDictionary(NonDefaultEqualityComparer<TKey>.Instance));
        }

        [Fact]
        public void ToFrozenDictionary_KeySelector_ResultsAreUsed()
        {
            TKey[] keys = GenerateUniqueKeyValuePairs(10).Select(pair => pair.Key).ToArray();

            FrozenDictionary<TKey, int> frozen = Enumerable.Range(0, 10).ToFrozenDictionary(i => keys[i], NonDefaultEqualityComparer<TKey>.Instance);
            Assert.Same(NonDefaultEqualityComparer<TKey>.Instance, frozen.Comparer);

            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(i, frozen[keys[i]]);
            }
        }

        [Fact]
        public void ToFrozenDictionary_KeySelectorAndValueSelector_ResultsAreUsed()
        {
            KeyValuePair<TKey, TValue>[] uniquePairs = GenerateUniqueKeyValuePairs(10);
            TKey[] keys = uniquePairs.Select(pair => pair.Key).ToArray();
            TValue[] values = uniquePairs.Select(pair => pair.Value).ToArray();

            FrozenDictionary<TKey, TValue> frozen = Enumerable.Range(0, 10).ToFrozenDictionary(i => keys[i], i => values[i], NonDefaultEqualityComparer<TKey>.Instance);
            Assert.Same(NonDefaultEqualityComparer<TKey>.Instance, frozen.Comparer);

            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(values[i], frozen[keys[i]]);
            }
        }

        public static IEnumerable<object[]> LookupItems_AllItemsFoundAsExpected_MemberData() =>
            from size in new[] { 0, 1, 2, 10, 99 }
            from comparer in new IEqualityComparer<TKey>[] { null, EqualityComparer<TKey>.Default, NonDefaultEqualityComparer<TKey>.Instance }
            from specifySameComparer in new[] { false, true }
            select new object[] { size, comparer, specifySameComparer };

        [Theory]
        [MemberData(nameof(LookupItems_AllItemsFoundAsExpected_MemberData))]
        public void LookupItems_AllItemsFoundAsExpected(int size, IEqualityComparer<TKey> comparer, bool specifySameComparer)
        {
            Dictionary<TKey, TValue> original =
                GenerateUniqueKeyValuePairs(size)
                .ToDictionary(p => p.Key, p => p.Value, comparer);
            KeyValuePair<TKey, TValue>[] originalPairs = original.ToArray();

            FrozenDictionary<TKey, TValue> frozen = specifySameComparer ?
                original.ToFrozenDictionary(comparer) :
                original.ToFrozenDictionary();

            // Make sure creating the frozen dictionary didn't alter the original
            Assert.Equal(originalPairs.Length, original.Count);
            Assert.All(originalPairs, p => Assert.Equal(p.Value, original[p.Key]));

            // Make sure the frozen dictionary matches the original
            Assert.Equal(original.Count, frozen.Count);
            Assert.Equal(new HashSet<KeyValuePair<TKey, TValue>>(original), new HashSet<KeyValuePair<TKey, TValue>>(frozen));
            Assert.All(originalPairs, p => Assert.True(frozen.ContainsKey(p.Key)));
            Assert.All(originalPairs, p => Assert.Equal(p.Value, frozen[p.Key]));
            Assert.All(originalPairs, p => Assert.Equal(p.Value, frozen.GetValueRefOrNullRef(p.Key)));
            Assert.Equal(originalPairs.Length, frozen.Keys.Length);
            Assert.Equal(originalPairs.Length, frozen.Values.Length);
            Assert.Equal(new HashSet<TKey>(originalPairs.Select(p => p.Key)), new HashSet<TKey>(frozen.Keys));
            Assert.Equal(new HashSet<TValue>(originalPairs.Select(p => p.Value)), new HashSet<TValue>(frozen.Values));
            if (specifySameComparer ||
                comparer is null ||
                comparer == EqualityComparer<TKey>.Default)
            {
                Assert.Equal(original.Comparer, frozen.Comparer);
            }

            // Generate additional items and ensure they match iff the original matches.
            for (int i = size; i < size + 100; i++)
            {
                TKey key = CreateTKey(i);
                if (original.ContainsKey(key))
                {
                    Assert.True(frozen.ContainsKey(key));
                }
                else
                {
                    Assert.Throws<KeyNotFoundException>(() => frozen[key]);
                    Assert.False(frozen.TryGetValue(key, out TValue value));
                    Assert.Equal(default, value);
                    Assert.True(Unsafe.IsNullRef(ref Unsafe.AsRef(in frozen.GetValueRefOrNullRef(key))));
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EqualButPossiblyDifferentKeys_Found(bool fromDictionary)
        {
            Dictionary<TKey, TValue> original =
                GenerateUniqueKeyValuePairs(50)
                .ToDictionary(p => p.Key, p => p.Value, GetKeyIEqualityComparer());

            FrozenDictionary<TKey, TValue> frozen = fromDictionary ?
                original.ToFrozenDictionary(GetKeyIEqualityComparer()) :
                original.Select(k => k).ToFrozenDictionary(GetKeyIEqualityComparer());

            foreach (TKey key in original.Keys)
            {
                Assert.True(original.ContainsKey(key));
                Assert.True(frozen.ContainsKey(key));

                TKey equalKey = GetEqualKey(key);
                Assert.True(original.ContainsKey(equalKey));
                Assert.True(frozen.ContainsKey(equalKey));
            }
        }

        [Fact]
        public void MultipleValuesSameKey_LastInSourceWins()
        {
            TKey[] keys = GenerateUniqueKeyValuePairs(2).Select(pair => pair.Key).ToArray();
            TValue[] values = Enumerable.Range(0, 10).Select(CreateTValue).ToArray();

            foreach (bool reverse in new[] { false, true })
            {
                IEnumerable<KeyValuePair<TKey, TValue>> source =
                    from key in keys
                    from value in values
                    select new KeyValuePair<TKey, TValue>(key, value);

                if (reverse)
                {
                    source = source.Reverse();
                }

                FrozenDictionary<TKey, TValue> frozen = source.ToFrozenDictionary(GetKeyIEqualityComparer());

                Assert.Equal(values[reverse ? 0 : values.Length - 1], frozen[keys[0]]);
                Assert.Equal(values[reverse ? 0 : values.Length - 1], frozen[keys[1]]);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(75)]
        public void IReadOnlyDictionary_Generic_Keys_ContainsAllCorrectKeys(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            IEnumerable<TKey> expected = dictionary.Select((pair) => pair.Key);

            IReadOnlyDictionary<TKey, TValue> rod = (IReadOnlyDictionary<TKey, TValue>)dictionary;
            Assert.True(expected.SequenceEqual(rod.Keys));
            Assert.All(expected, k => rod.ContainsKey(k));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(75)]
        public void IReadOnlyDictionary_Generic_Values_ContainsAllCorrectValues(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            IEnumerable<TValue> expected = dictionary.Select((pair) => pair.Value);

            IReadOnlyDictionary<TKey, TValue> rod = (IReadOnlyDictionary<TKey, TValue>)dictionary;
            Assert.True(expected.SequenceEqual(rod.Values));

            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            {
                Assert.Equal(dictionary[pair.Key], rod[pair.Key]);
            }

            Assert.All(dictionary, pair =>
            {
                Assert.True(rod.TryGetValue(pair.Key, out TValue value));
                Assert.Equal(pair.Value, value);
            });
        }
    }

    public abstract class FrozenDictionary_Generic_Tests_string_string : FrozenDictionary_Generic_Tests<string, string>
    {
        protected override KeyValuePair<string, string> CreateT(int seed) =>
            new KeyValuePair<string, string>(CreateTKey(seed), CreateTKey(seed + 500));

        protected override string CreateTKey(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes1 = new byte[stringLength];
            rand.NextBytes(bytes1);
            return Convert.ToBase64String(bytes1);
        }

        protected override string CreateTValue(int seed) => CreateTKey(seed);

        [Theory]
        [InlineData(0)]
        [InlineData(25)]
        [InlineData(50)]
        [InlineData(75)]
        [InlineData(100)]
        public void ContainsKey_WithNonAscii(int percentageWithNonAscii)
        {
            Random rand = new(42);

            Dictionary<string, string> expected = new(GetKeyIEqualityComparer());
            for (int i = 0; i < 100; i++)
            {
                int stringLength = rand.Next(4, 30);
                byte[] bytes = new byte[stringLength];
                rand.NextBytes(bytes);
                string value = Convert.ToBase64String(bytes);
                if (rand.Next(100) < percentageWithNonAscii)
                {
                    value = value.Replace(value[rand.Next(value.Length)], '\u05D0');
                }
                expected.Add(value, value);
            }

            FrozenDictionary<string, string> actual = expected.ToFrozenDictionary(GetKeyIEqualityComparer());

            Assert.All(expected, kvp => actual.ContainsKey(kvp.Key));
        }
    }

    public class FrozenDictionary_Generic_Tests_string_string_Default : FrozenDictionary_Generic_Tests_string_string
    {
        public override IEqualityComparer<string> GetKeyIEqualityComparer() => EqualityComparer<string>.Default;
    }

    public class FrozenDictionary_Generic_Tests_string_string_Ordinal : FrozenDictionary_Generic_Tests_string_string
    {
        public override IEqualityComparer<string> GetKeyIEqualityComparer() => StringComparer.Ordinal;
    }

    public class FrozenDictionary_Generic_Tests_string_string_OrdinalIgnoreCase : FrozenDictionary_Generic_Tests_string_string
    {
        public override IEqualityComparer<string> GetKeyIEqualityComparer() => StringComparer.OrdinalIgnoreCase;

        public override string GetEqualKey(string key) => key.ToLowerInvariant();
    }

    public class FrozenDictionary_Generic_Tests_string_string_NonDefault : FrozenDictionary_Generic_Tests_string_string
    {
        public override IEqualityComparer<string> GetKeyIEqualityComparer() => NonDefaultEqualityComparer<string>.Instance;
    }

    public class FrozenDictionary_Generic_Tests_ulong_ulong : FrozenDictionary_Generic_Tests<ulong, ulong>
    {
        protected override bool DefaultValueAllowed => true;

        protected override KeyValuePair<ulong, ulong> CreateT(int seed)
        {
            ulong key = CreateTKey(seed);
            ulong value = CreateTKey(~seed);
            return new KeyValuePair<ulong, ulong>(key, value);
        }

        protected override ulong CreateTKey(int seed)
        {
            Random rand = new Random(seed);
            ulong hi = unchecked((ulong)rand.Next());
            ulong lo = unchecked((ulong)rand.Next());
            return (hi << 32) | lo;
        }

        protected override ulong CreateTValue(int seed) => CreateTKey(seed);

        [OuterLoop("Takes several seconds")]
        [Theory]
        [InlineData(8_000_000)]
        public void CreateHugeDictionary_Success(int largeCount)
        {
            if (AllowVeryLargeSizes)
            {
                GenericIDictionaryFactory(largeCount);
            }
        }
    }

    public abstract class FrozenDictionary_Generic_Tests_base_for_numbers<T> : FrozenDictionary_Generic_Tests<T, T>
    {
        protected override bool DefaultValueAllowed => true;

        protected override KeyValuePair<T, T> CreateT(int seed)
        {
            Random rand = new Random(seed);
            return new KeyValuePair<T, T>(Next(rand), Next(rand));
        }

        protected override T CreateTKey(int seed) => Next(new Random(seed));

        protected override T CreateTValue(int seed) => CreateTKey(seed);

        protected abstract T Next(Random random);

        protected static long NextLong(Random random)
        {
            byte[] bytes = new byte[8];
            random.NextBytes(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }
    }


    public class FrozenDictionary_Generic_Tests_int_int : FrozenDictionary_Generic_Tests_base_for_numbers<int>
    {
        protected override int Next(Random random) => random.Next(); 
    }

    public class FrozenDictionary_Generic_Tests_uint_uint : FrozenDictionary_Generic_Tests_base_for_numbers<uint>
    {
        protected override uint Next(Random random) => (uint)random.Next(int.MinValue, int.MaxValue);
    }

    public class FrozenDictionary_Generic_Tests_nint_nint : FrozenDictionary_Generic_Tests_base_for_numbers<nint>
    {
        protected override nint Next(Random random) => IntPtr.Size == sizeof(int)
            ? random.Next(int.MinValue, int.MaxValue)
            : (nint)NextLong(random);
    }

    public class FrozenDictionary_Generic_Tests_nuint_nuint : FrozenDictionary_Generic_Tests_base_for_numbers<nuint>
    {
        protected override nuint Next(Random random) => IntPtr.Size == sizeof(int)
            ? (nuint)random.Next(int.MinValue, int.MaxValue)
            : (nuint)NextLong(random);
    }

    public class FrozenDictionary_Generic_Tests_short_short : FrozenDictionary_Generic_Tests_base_for_numbers<short>
    {
        protected override bool AllowVeryLargeSizes => false;

        protected override int MaxUniqueValueCount => short.MaxValue - short.MinValue;

        protected override short Next(Random random) => (short)random.Next(short.MinValue, short.MaxValue);
    }

    public class FrozenDictionary_Generic_Tests_ushort_ushort : FrozenDictionary_Generic_Tests_base_for_numbers<ushort>
    {
        protected override bool AllowVeryLargeSizes => false;

        protected override int MaxUniqueValueCount => ushort.MaxValue;

        protected override ushort Next(Random random) => (ushort)random.Next(ushort.MinValue, ushort.MaxValue);
    }

    public class FrozenDictionary_Generic_Tests_byte_byte : FrozenDictionary_Generic_Tests_base_for_numbers<byte>
    {
        protected override bool AllowVeryLargeSizes => false;

        protected override int MaxUniqueValueCount => byte.MaxValue;

        protected override byte Next(Random random) => (byte)random.Next(byte.MinValue, byte.MaxValue);
    }

    public class FrozenDictionary_Generic_Tests_sbyte_sbyte : FrozenDictionary_Generic_Tests_base_for_numbers<sbyte>
    {
        protected override bool AllowVeryLargeSizes => false;

        protected override int MaxUniqueValueCount => sbyte.MaxValue - sbyte.MinValue;

        protected override sbyte Next(Random random) => (sbyte)random.Next(sbyte.MinValue, sbyte.MaxValue);
    }

    public class FrozenDictionary_Generic_Tests_SimpleClass_SimpleClass : FrozenDictionary_Generic_Tests<SimpleClass, SimpleClass>
    {
        protected override KeyValuePair<SimpleClass, SimpleClass> CreateT(int seed) =>
            new KeyValuePair<SimpleClass, SimpleClass>(CreateTKey(seed), CreateTValue(seed + 500));

        protected override SimpleClass CreateTKey(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes1 = new byte[stringLength];
            rand.NextBytes(bytes1);
            return new SimpleClass { Value = Convert.ToBase64String(bytes1) };
        }

        protected override SimpleClass CreateTValue(int seed) => CreateTKey(seed);
    }

    public class SimpleClass : IComparable<SimpleClass>
    {
        public string Value { get; set; }

        public int CompareTo(SimpleClass? other) =>
            other is null ? -1 :
            Value.CompareTo(other.Value);
    }

    public class FrozenDictionary_Generic_Tests_SimpleStruct_int : FrozenDictionary_Generic_Tests<SimpleStruct, int>
    {
        protected override KeyValuePair<SimpleStruct, int> CreateT(int seed) =>
            new KeyValuePair<SimpleStruct, int>(CreateTKey(seed), CreateTValue(seed + 500));

        protected override SimpleStruct CreateTKey(int seed) => new SimpleStruct { Value = seed + 1 };

        protected override int CreateTValue(int seed) => seed;

        protected override bool DefaultValueAllowed => true;

        protected override bool AllowVeryLargeSizes => false; // hash code contention leads to longer running times
    }

    public class FrozenDictionary_Generic_Tests_SimpleNonComparableStruct_int : FrozenDictionary_Generic_Tests<SimpleNonComparableStruct, int>
    {
        protected override KeyValuePair<SimpleNonComparableStruct, int> CreateT(int seed) =>
            new KeyValuePair<SimpleNonComparableStruct, int>(CreateTKey(seed), CreateTValue(seed + 500));

        protected override SimpleNonComparableStruct CreateTKey(int seed) => new SimpleNonComparableStruct { Value = seed + 1 };

        protected override int CreateTValue(int seed) => seed;

        protected override bool DefaultValueAllowed => true;

        protected override bool AllowVeryLargeSizes => false; // hash code contention leads to longer running times
    }

    public class FrozenDictionary_Generic_Tests_ValueTupleSimpleNonComparableStruct_int : FrozenDictionary_Generic_Tests<ValueTuple<SimpleNonComparableStruct,SimpleNonComparableStruct>, int>
    {
        protected override KeyValuePair<ValueTuple<SimpleNonComparableStruct, SimpleNonComparableStruct>, int> CreateT(int seed) =>
            new KeyValuePair<ValueTuple<SimpleNonComparableStruct, SimpleNonComparableStruct>, int>(CreateTKey(seed), CreateTValue(seed + 500));

        protected override ValueTuple<SimpleNonComparableStruct, SimpleNonComparableStruct> CreateTKey(int seed) =>
            new ValueTuple<SimpleNonComparableStruct, SimpleNonComparableStruct>(
                new SimpleNonComparableStruct { Value = seed + 1 },
                new SimpleNonComparableStruct { Value = seed + 1 });

        protected override int CreateTValue(int seed) => seed;

        protected override bool DefaultValueAllowed => true;

        protected override bool AllowVeryLargeSizes => false; // hash code contention leads to longer running times
    }

    public struct SimpleStruct : IEquatable<SimpleStruct>, IComparable<SimpleStruct>
    {
        public int Value { get; set; }

        public int CompareTo(SimpleStruct other) => Value.CompareTo(other.Value);

        public bool Equals(SimpleStruct other) => Value == other.Value;

        public override int GetHashCode() => 0; // to force hashcode contention in implementation

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is SimpleStruct other && Equals(other);
    }

    public struct SimpleNonComparableStruct : IEquatable<SimpleNonComparableStruct>
    {
        public int Value { get; set; }

        public bool Equals(SimpleNonComparableStruct other) => Value == other.Value;

        public override int GetHashCode() => 0; // to force hashcode contention in implementation

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is SimpleNonComparableStruct other && Equals(other);
    }

    public sealed class NonDefaultEqualityComparer<TKey> : IEqualityComparer<TKey>
    {
        public static NonDefaultEqualityComparer<TKey> Instance { get; } = new();
        public bool Equals(TKey? x, TKey? y) => EqualityComparer<TKey>.Default.Equals(x, y);
        public int GetHashCode([DisallowNull] TKey obj) => EqualityComparer<TKey>.Default.GetHashCode(obj);
    }

    public class FrozenDictionary_NonGeneric_Tests : IDictionary_NonGeneric_Tests
    {
        protected override IDictionary NonGenericIDictionaryFactory() => FrozenDictionary<object, object>.Empty;

        protected override IDictionary NonGenericIDictionaryFactory(int count)
        {
            var d = new Dictionary<object, object>();
            for (int i = 0; i < count; i++)
            {
                d.Add(CreateTKey(i), CreateTValue(i));
            }
            return d.ToFrozenDictionary();
        }

        protected override ICollection NonGenericICollectionFactory(int count) => NonGenericIDictionaryFactory(count);

        /// <summary>
        /// Creates an object that is dependent on the seed given. The object may be either
        /// a value type or a reference type, chosen based on the value of the seed.
        /// </summary>
        protected override object CreateTKey(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes = new byte[stringLength];
            rand.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Creates an object that is dependent on the seed given. The object may be either
        /// a value type or a reference type, chosen based on the value of the seed.
        /// </summary>
        protected override object CreateTValue(int seed) => CreateTKey(seed);

        protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations) => new List<ModifyEnumerable>();

        protected override bool Enumerator_Current_UndefinedOperation_Throws => true;

        protected override bool IsReadOnly => true;

        protected override bool ResetImplemented => true;

        protected override bool IDictionary_NonGeneric_Keys_Values_Enumeration_ResetImplemented => true;

        protected override bool SupportsSerialization => false;

        protected override bool ExpectedIsFixedSize => true;

        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectReferenceType_ThrowType => typeof(ArgumentException);

        protected override Type ICollection_NonGeneric_CopyTo_IndexLargerThanArrayCount_ThrowType => typeof(ArgumentOutOfRangeException);

        [Fact]
        public void ICollection_CopyTo_MultipleArrayTypesSupported()
        {
            FrozenDictionary<string, int> frozen = new Dictionary<string, int>()
            {
                { "hello", 123 },
                { "world", 456 }
            }.ToFrozenDictionary();

            var kvpArray = new KeyValuePair<string, int>[4];
            ((ICollection)frozen).CopyTo(kvpArray, 1);
            Assert.Equal(new KeyValuePair<string, int>(null, 0), kvpArray[0]);
            Assert.True(
                (kvpArray[1].Equals(new KeyValuePair<string, int>("hello", 123)) && kvpArray[2].Equals(new KeyValuePair<string, int>("world", 456))) ||
                (kvpArray[2].Equals(new KeyValuePair<string, int>("hello", 123)) && kvpArray[1].Equals(new KeyValuePair<string, int>("world", 456))));
            Assert.Equal(new KeyValuePair<string, int>(null, 0), kvpArray[3]);

            var deArray = new DictionaryEntry[4];
            ((ICollection)frozen).CopyTo(deArray, 2);
            Assert.Equal(new DictionaryEntry(null, null), deArray[0]);
            Assert.Equal(new DictionaryEntry(null, null), deArray[1]);
            Assert.True(
                (deArray[2].Equals(new DictionaryEntry("hello", 123)) && deArray[3].Equals(new DictionaryEntry("world", 456))) ||
                (deArray[3].Equals(new DictionaryEntry("hello", 123)) && deArray[2].Equals(new DictionaryEntry("world", 456))));
        }

        [Fact]
        public void Sparse_LookupItems_AlltemsFoundAsExpected()
        {
            foreach (int size in new[] { 1, 2, 10, 63, 64, 65, 999, 1024 })
            {
                foreach (int skip in new[] { 2, 3, 5 })
                {
                    IEnumerable<KeyValuePair<int, string>> sequence = Enumerable
                        .Range(-3, size)
                        .Where(i => i % skip == 0)
                        .Select(i => new KeyValuePair<int, string>(i, i.ToString()));

                    var original = new Dictionary<int, string>();
                    foreach (KeyValuePair<int, string> kvp in sequence)
                    {
                        original[kvp.Key] = kvp.Value;
                    }

                    FrozenDictionary<int, string> frozen = original.ToFrozenDictionary();

                    for (int i = -10; i <= size + 66; i++)
                    {
                        Assert.Equal(original.ContainsKey(i), frozen.ContainsKey(i));

                        if (original.ContainsKey(i))
                        {
                            Assert.Equal(original[i], frozen[i]);
                        }
                    }
                }
            }
        }
    }
}
