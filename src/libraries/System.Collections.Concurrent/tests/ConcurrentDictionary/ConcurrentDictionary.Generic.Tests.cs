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

            const int StartOfRange = 0xE020; // use the Unicode Private Use range to avoid accidentally creating strings that really do compare as equal OrdinalIgnoreCase
            const int Stride = 0x40; // to ensure we don't accidentally reset the 0x20 bit of the seed, which is used to negate OrdinalIgnoreCase effects
            int currentSeed = StartOfRange;

            List<string> collidingStrings = new List<string>(count);
            while (collidingStrings.Count < count)
            {
                Assert.True(currentSeed <= ushort.MaxValue,
                    $"Couldn't create enough colliding strings? Created {collidingStrings.Count}, needed {count}.");

                // Generates a possible string with a well-known non-randomized hash code:
                // - string.GetNonRandomizedHashCode returns 0.
                // - string.GetNonRandomizedHashCodeOrdinalIgnoreCase returns 0x24716ca0.
                // Provide a different seed to produce a different string.
                // Must check OrdinalIgnoreCase hash code to ensure correctness.
                string candidate = string.Create(8, currentSeed, static (span, seed) =>
                {
                    Span<byte> asBytes = MemoryMarshal.AsBytes(span);

                    uint hash1 = (5381 << 16) + 5381;
                    uint hash2 = BitOperations.RotateLeft(hash1, 5) + hash1;

                    MemoryMarshal.Write(asBytes, ref seed);
                    MemoryMarshal.Write(asBytes.Slice(4), ref hash2); // set hash2 := 0 (for Ordinal)

                    hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1) ^ (uint)seed;
                    hash1 = (BitOperations.RotateLeft(hash1, 5) + hash1);

                    MemoryMarshal.Write(asBytes.Slice(8), ref hash1); // set hash1 := 0 (for Ordinal)
                });

                int ordinalHashCode = nonRandomizedOrdinal(candidate);
                Assert.Equal(0, ordinalHashCode); // ensure has a zero hash code Ordinal

                int ordinalIgnoreCaseHashCode = nonRandomizedOrdinalIgnoreCase(candidate);
                if (ordinalIgnoreCaseHashCode == 0x24716ca0) // ensure has a zero hash code OrdinalIgnoreCase (might not have one)
                {
                    collidingStrings.Add(candidate); // success!
                }

                currentSeed += Stride;
            }

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
