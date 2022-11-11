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

        protected override IDictionary<TKey, TValue> GenericIDictionaryFactory(int count)
        {
            var d = new Dictionary<TKey, TValue>();
            for (int i = 0; i < count; i++)
            {
                d.Add(CreateTKey(i), CreateTValue(i));
            }
            return d.ToFrozenDictionary(GetKeyIEqualityComparer());
        }

        protected override IDictionary<TKey, TValue> GenericIDictionaryFactory() => Enumerable.Empty<KeyValuePair<TKey, TValue>>().ToFrozenDictionary();

        protected override IDictionary<TKey, TValue> GenericIDictionaryFactory(IEqualityComparer<TKey> comparer) => Enumerable.Empty<KeyValuePair<TKey, TValue>>().ToFrozenDictionary(comparer);

        protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations) => new List<ModifyEnumerable>();

        protected override bool ResetImplemented => true;
        protected override bool IDictionary_Generic_Keys_Values_Enumeration_ResetImplemented => true;

        protected override EnumerableOrder Order => EnumerableOrder.Unspecified;

        [Theory]
        [InlineData(100_000)]
        public void CreateVeryLargeDictionary_Success(int largeCount)
        {
            GenericIDictionaryFactory(largeCount);
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
            Assert.Same(FrozenDictionary<TKey, TValue>.Empty, new Dictionary<TKey, TValue>().ToFrozenDictionary());
            Assert.Same(FrozenDictionary<TKey, TValue>.Empty, Enumerable.Empty<KeyValuePair<TKey, TValue>>().ToFrozenDictionary());
            Assert.Same(FrozenDictionary<TKey, TValue>.Empty, Array.Empty<KeyValuePair<TKey, TValue>>().ToFrozenDictionary());
            Assert.Same(FrozenDictionary<TKey, TValue>.Empty, new List<KeyValuePair<TKey, TValue>>().ToFrozenDictionary());

            foreach (IEqualityComparer<TKey> comparer in new IEqualityComparer<TKey>[] { null, EqualityComparer<TKey>.Default, NonDefaultEqualityComparer<TKey>.Instance })
            {
                Assert.Same(FrozenDictionary<TKey, TValue>.Empty, new Dictionary<TKey, TValue>().ToFrozenDictionary(comparer));
                Assert.Same(FrozenDictionary<TKey, TValue>.Empty, Enumerable.Empty<KeyValuePair<TKey, TValue>>().ToFrozenDictionary(comparer));
                Assert.Same(FrozenDictionary<TKey, TValue>.Empty, Array.Empty<KeyValuePair<TKey, TValue>>().ToFrozenDictionary(comparer));
                Assert.Same(FrozenDictionary<TKey, TValue>.Empty, new List<KeyValuePair<TKey, TValue>>().ToFrozenDictionary(comparer));
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
            foreach (IEqualityComparer<TKey> comparer in new IEqualityComparer<TKey>[] { null, EqualityComparer<TKey>.Default, NonDefaultEqualityComparer<TKey>.Instance })
            {
                Assert.Same(FrozenDictionary<TKey, TValue>.Empty, FrozenDictionary<TKey, TValue>.Empty.ToFrozenDictionary(comparer));
            }

            FrozenDictionary<TKey, TValue> frozen = new Dictionary<TKey, TValue>() { { CreateTKey(0), CreateTValue(0) } }.ToFrozenDictionary();
            Assert.Same(frozen, frozen.ToFrozenDictionary());
            Assert.NotSame(frozen, frozen.ToFrozenDictionary(NonDefaultEqualityComparer<TKey>.Instance));
        }

        public static IEnumerable<object[]> LookupItems_AllItemsFoundAsExpected_MemberData()
        {
            foreach (int size in new[] { 1, 2, 10, 999, 1024 })
            {
                foreach (IEqualityComparer<TKey> comparer in new IEqualityComparer<TKey>[] { null, EqualityComparer<TKey>.Default, NonDefaultEqualityComparer<TKey>.Instance })
                {
                    foreach (bool specifySameComparer in new[] { false, true })
                    {
                        yield return new object[] { size, comparer, specifySameComparer };
                    }
                }
            }
        }

        [Fact]
        public void ToFrozenDictionary_KeySelector_ResultsAreUsed()
        {
            TKey[] keys = Enumerable.Range(0, 10).Select(CreateTKey).ToArray();

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
            TKey[] keys = Enumerable.Range(0, 10).Select(CreateTKey).ToArray();
            TValue[] values = Enumerable.Range(0, 10).Select(CreateTValue).ToArray();

            FrozenDictionary<TKey, TValue> frozen = Enumerable.Range(0, 10).ToFrozenDictionary(i => keys[i], i => values[i], NonDefaultEqualityComparer<TKey>.Instance);
            Assert.Same(NonDefaultEqualityComparer<TKey>.Instance, frozen.Comparer);

            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(values[i], frozen[keys[i]]);
            }
        }

        [Theory]
        [MemberData(nameof(LookupItems_AllItemsFoundAsExpected_MemberData))]
        public void LookupItems_AllItemsFoundAsExpected(int size, IEqualityComparer<TKey> comparer, bool specifySameComparer)
        {
            Dictionary<TKey, TValue> original =
                Enumerable.Range(0, size)
                .Select(i => new KeyValuePair<TKey, TValue>(CreateTKey(i), CreateTValue(i)))
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

        [Fact]
        public void MultipleValuesSameKey_LastInSourceWins()
        {
            TKey[] keys = Enumerable.Range(0, 2).Select(CreateTKey).ToArray();
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
        [MemberData(nameof(ValidCollectionSizes))]
        public void IReadOnlyDictionary_Generic_Keys_ContainsAllCorrectKeys(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            IEnumerable<TKey> expected = dictionary.Select((pair) => pair.Key);

            IReadOnlyDictionary<TKey, TValue> rod = (IReadOnlyDictionary<TKey, TValue>)dictionary;
            Assert.True(expected.SequenceEqual(rod.Keys));
            Assert.All(expected, k => rod.ContainsKey(k));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
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
        protected override KeyValuePair<string, string> CreateT(int seed)
        {
            return new KeyValuePair<string, string>(CreateTKey(seed), CreateTKey(seed + 500));
        }

        protected override string CreateTKey(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes1 = new byte[stringLength];
            rand.NextBytes(bytes1);
            return Convert.ToBase64String(bytes1);
        }

        protected override string CreateTValue(int seed) => CreateTKey(seed);
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
            GenericIDictionaryFactory(largeCount);
        }
    }

    public class FrozenDictionary_Generic_Tests_int_int : FrozenDictionary_Generic_Tests<int, int>
    {
        protected override bool DefaultValueAllowed => true;

        protected override KeyValuePair<int, int> CreateT(int seed)
        {
            Random rand = new Random(seed);
            return new KeyValuePair<int, int>(rand.Next(), rand.Next());
        }

        protected override int CreateTKey(int seed) => new Random(seed).Next();

        protected override int CreateTValue(int seed) => CreateTKey(seed);
    }

    public class FrozenDictionary_Generic_Tests_SimpleClass_SimpleClass : FrozenDictionary_Generic_Tests<SimpleClass, SimpleClass>
    {
        protected override KeyValuePair<SimpleClass, SimpleClass> CreateT(int seed)
        {
            return new KeyValuePair<SimpleClass, SimpleClass>(CreateTKey(seed), CreateTKey(seed + 500));
        }

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
            Assert.Equal(new KeyValuePair<string, int>("hello", 123), kvpArray[1]);
            Assert.Equal(new KeyValuePair<string, int>("world", 456), kvpArray[2]);
            Assert.Equal(new KeyValuePair<string, int>(null, 0), kvpArray[3]);

            var deArray = new DictionaryEntry[4];
            ((ICollection)frozen).CopyTo(deArray, 2);
            Assert.Equal(new DictionaryEntry(null, null), deArray[0]);
            Assert.Equal(new DictionaryEntry(null, null), deArray[1]);
            Assert.Equal(new DictionaryEntry("hello", 123), deArray[2]);
            Assert.Equal(new DictionaryEntry("world", 456), deArray[3]);
        }
    }
}
