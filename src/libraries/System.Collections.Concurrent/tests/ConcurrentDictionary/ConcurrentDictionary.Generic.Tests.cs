// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Tests;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;
using System.Runtime.CompilerServices;

namespace System.Collections.Concurrent.Tests
{
    public class ConcurrentDictionary_Generic_Tests_enum_enum : ConcurrentDictionary_Generic_Tests<SimpleEnum, SimpleEnum>
    {
        protected override bool DefaultValueAllowed => true;

        protected override KeyValuePair<SimpleEnum, SimpleEnum> CreateT(int seed)
        {
            return new KeyValuePair<SimpleEnum, SimpleEnum>(CreateTKey(seed), CreateTValue(seed));
        }

        protected override SimpleEnum CreateTKey(int seed) => (SimpleEnum)new Random(seed).Next();

        protected override SimpleEnum CreateTValue(int seed) => CreateTKey(seed);
    }

    public class ConcurrentDictionary_Generic_Tests_string_string : ConcurrentDictionary_Generic_Tests<string, string>
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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NonRandomizedToRandomizedUpgrade_FunctionsCorrectly(bool ignoreCase)
        {
            List<string> strings = GenerateCollidingStrings(110); // higher than the collisions threshold

            var cd = new ConcurrentDictionary<string, string>(ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            for (int i = 0; i < strings.Count; i++)
            {
                string s = strings[i];

                Assert.True(cd.TryAdd(s, s));
                Assert.False(cd.TryAdd(s, s));

                for (int j = 0; j < strings.Count; j++)
                {
                    Assert.Equal(j <= i, cd.ContainsKey(strings[j]));
                }
            }
        }

        private static List<string> GenerateCollidingStrings(int count)
        {
            static Func<string, int> GetHashCodeFunc(ConcurrentDictionary<string, string> cd)
            {
                // If the layout of ConcurrentDictionary changes, this will need to change as well.

                FieldInfo tablesField = AssertNotNull(typeof(ConcurrentDictionary<string, string>).GetField("_tables", BindingFlags.Instance | BindingFlags.NonPublic));
                Type tablesType = Type.GetType("System.Collections.Concurrent.ConcurrentDictionary`2+Tables, System.Collections.Concurrent", throwOnError: true);
                object tables = AssertNotNull(tablesField.GetValue(cd));

                FieldInfo comparerField = AssertNotNull(tablesType.GetField("_comparer", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
                comparerField = AssertNotNull((FieldInfo)tables.GetType().GetMemberWithSameMetadataDefinitionAs(comparerField));
                IEqualityComparer<string> comparer = AssertNotNull((IEqualityComparer<string>)comparerField.GetValue(tables));

                return comparer.GetHashCode;

                static T AssertNotNull<T>(T value, [CallerArgumentExpression(nameof(value))] string valueArg = null)
                {
                    Assert.True(value is not null, valueArg);
                    return value;
                }
            }

            Func<string, int> nonRandomizedOrdinal = GetHashCodeFunc(new ConcurrentDictionary<string, string>(StringComparer.Ordinal));
            Func<string, int> nonRandomizedOrdinalIgnoreCase = GetHashCodeFunc(new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase));

            // Every code unit is taken from the Unicode Private Use Area with bit 0x20 set.
            // Those are uncased, so ToUpperOrdinal leaves them alone, and they already carry
            // the bit that ignore-case hashing ORs in, so a string built from them hashes the
            // same under Ordinal and OrdinalIgnoreCase and no two compare equal
            // OrdinalIgnoreCase.
            List<ushort> pool = new List<ushort>();
            for (int c = 0xE000; c <= 0xF8FF; c++)
            {
                if ((c & 0x20) != 0)
                {
                    pool.Add((ushort)c);
                }
            }

            // For an eight char string the hash is a function of two 64-bit halves, the first
            // offset by a length term. Pinning the value they are combined into pins the hash,
            // so the second half can be chosen freely and the first solved for; solutions that
            // fall outside the pool are discarded.
            const uint HashPrime = 0x9E3779B1u;
            ulong lengthTerm = unchecked(16u * HashPrime); // computed in 32-bit arithmetic

            static ulong Pack(ushort a, ushort b, ushort c, ushort d) =>
                a | ((ulong)b << 16) | ((ulong)c << 32) | ((ulong)d << 48);
            static bool InPool(ushort c) => c >= 0xE000 && c <= 0xF8FF && (c & 0x20) != 0;

            ulong seedSecond = Pack(pool[0], pool[1], pool[2], pool[3]);
            ulong seedFirst = lengthTerm + Pack(pool[4], pool[5], pool[6], pool[7]);
            ulong combined = (seedFirst ^ BitOperations.RotateLeft(seedSecond, 27)) + BitOperations.RotateLeft(seedSecond, 41);

            List<string> collidingStrings = new List<string>(count);
            for (int i0 = 0; i0 < pool.Count && collidingStrings.Count < count; i0++)
            for (int i1 = 0; i1 < pool.Count && collidingStrings.Count < count; i1++)
            for (int i2 = 0; i2 < pool.Count && collidingStrings.Count < count; i2++)
            for (int i3 = 0; i3 < pool.Count && collidingStrings.Count < count; i3++)
            {
                ulong second = Pack(pool[i0], pool[i1], pool[i2], pool[i3]);
                ulong first = ((combined - BitOperations.RotateLeft(second, 41)) ^ BitOperations.RotateLeft(second, 27)) - lengthTerm;

                ushort c0 = (ushort)first, c1 = (ushort)(first >> 16), c2 = (ushort)(first >> 32), c3 = (ushort)(first >> 48);
                if (!InPool(c0) || !InPool(c1) || !InPool(c2) || !InPool(c3))
                {
                    continue;
                }

                string candidate = new string(new char[]
                {
                    (char)c0, (char)c1, (char)c2, (char)c3,
                    (char)pool[i0], (char)pool[i1], (char)pool[i2], (char)pool[i3]
                });

                // The construction encodes the current implementation, so check it against
                // the comparers actually in use rather than trusting it.
                Assert.Equal(nonRandomizedOrdinal(collidingStrings.Count == 0 ? candidate : collidingStrings[0]), nonRandomizedOrdinal(candidate));
                Assert.Equal(nonRandomizedOrdinalIgnoreCase(collidingStrings.Count == 0 ? candidate : collidingStrings[0]), nonRandomizedOrdinalIgnoreCase(candidate));
                collidingStrings.Add(candidate);
            }

            Assert.True(collidingStrings.Count == count,
                $"Couldn't create enough colliding strings? Created {collidingStrings.Count}, needed {count}.");

            return collidingStrings;
        }
    }

    public class ConcurrentDictionary_Generic_Tests_ulong_ulong : ConcurrentDictionary_Generic_Tests<ulong, ulong>
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
    }

    public class ConcurrentDictionary_Generic_Tests_int_int : ConcurrentDictionary_Generic_Tests<int, int>
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

    /// <summary>
    /// Contains tests that ensure the correctness of the ConcurrentDictionary class.
    /// </summary>
    public abstract class ConcurrentDictionary_Generic_Tests<TKey, TValue> : IDictionary_Generic_Tests<TKey, TValue>
    {
        #region IDictionary<TKey, TValue Helper Methods

        protected override IDictionary<TKey, TValue> GenericIDictionaryFactory() => new ConcurrentDictionary<TKey, TValue>();

        protected override IDictionary<TKey, TValue> GenericIDictionaryFactory(IEqualityComparer<TKey> comparer) => new ConcurrentDictionary<TKey, TValue>(comparer);

        protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations) => new List<ModifyEnumerable>();

        protected override bool Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException => false;
        protected override bool IDictionary_Generic_Keys_Values_Enumeration_ThrowsInvalidOperation_WhenParentModified => false;

        protected override bool IDictionary_Generic_Keys_Values_ModifyingTheDictionaryUpdatesTheCollection => false;

        protected override bool ResetImplemented => true;
        protected override bool IDictionary_Generic_Keys_Values_Enumeration_ResetImplemented => true;

        protected override EnumerableOrder Order => EnumerableOrder.Unspecified;

        #endregion

        #region Constructors

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Ctor_IDictionary(int count)
        {
            IDictionary<TKey, TValue> source = GenericIDictionaryFactory(count);
            IDictionary<TKey, TValue> copied = new ConcurrentDictionary<TKey, TValue>(source);
            Assert.Equal(source, copied);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Ctor_IDictionary_IEqualityComparer(int count)
        {
            IEqualityComparer<TKey> comparer = GetKeyIEqualityComparer();
            IDictionary<TKey, TValue> source = GenericIDictionaryFactory(count);
            ConcurrentDictionary<TKey, TValue> copied = new ConcurrentDictionary<TKey, TValue>(source, comparer);
            Assert.Equal(source, copied);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Ctor_IEqualityComparer(int count)
        {
            IEqualityComparer<TKey> comparer = GetKeyIEqualityComparer();
            IDictionary<TKey, TValue> source = GenericIDictionaryFactory(count);
            ConcurrentDictionary<TKey, TValue> copied = new ConcurrentDictionary<TKey, TValue>(source, comparer);
            Assert.Equal(source, copied);
        }

        #endregion

        #region IReadOnlyDictionary<TKey, TValue>.Keys

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IReadOnlyDictionary_Generic_Keys_ContainsAllCorrectKeys(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            IEnumerable<TKey> expected = dictionary.Select((pair) => pair.Key);
            IEnumerable<TKey> keys = ((IReadOnlyDictionary<TKey, TValue>)dictionary).Keys;
            Assert.True(expected.SequenceEqual(keys));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IReadOnlyDictionary_Generic_Values_ContainsAllCorrectValues(int count)
        {
            IDictionary<TKey, TValue> dictionary = GenericIDictionaryFactory(count);
            IEnumerable<TValue> expected = dictionary.Select((pair) => pair.Value);
            IEnumerable<TValue> values = ((IReadOnlyDictionary<TKey, TValue>)dictionary).Values;
            Assert.True(expected.SequenceEqual(values));
        }

        #endregion
    }
}
