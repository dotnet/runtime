// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Tests;
using System.Collections.Generic;
using System.Linq;
using Xunit;

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

        protected override IDictionary<TKey, TValue> GenericIDictionaryFactory()
        {
            return new ConcurrentDictionary<TKey, TValue>();
        }

        protected override IEnumerable<ModifyEnumerable> ModifyEnumerables => new List<ModifyEnumerable>();

        protected override bool IDictionary_Generic_Keys_Values_Enumeration_ThrowsInvalidOperation_WhenParentModified => false;

        protected override bool IDictionary_Generic_Keys_Values_ModifyingTheDictionaryUpdatesTheCollection => false;

        protected override bool ResetImplemented => false;
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
