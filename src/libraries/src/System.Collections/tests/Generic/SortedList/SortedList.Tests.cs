// Copyright (c) Justin Van Patten. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Tests
{
    public class SortedList_IDictionary_NonGeneric_Tests : IDictionary_NonGeneric_Tests
    {
        #region IDictionary Helper Methods

        protected override IDictionary NonGenericIDictionaryFactory()
        {
            return new SortedList<string, string>();
        }

        protected override object CreateTKey(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes = new byte[stringLength];
            rand.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        protected override object CreateTValue(int seed)
        {
            return CreateTKey(seed);
        }

        #endregion

        #region IDictionary tests

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_ItemSet_NullValueWhenDefaultValueIsNonNull(int count)
        {
            IDictionary dictionary = new SortedList<string, int>();
            Assert.Throws<ArgumentNullException>(() => dictionary[GetNewKey(dictionary)] = null);
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_ItemSet_KeyOfWrongType(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary dictionary = new SortedList<string, string>();
                Assert.Throws<ArgumentException>(() => dictionary[23] = CreateTValue(12345));
                Assert.Empty(dictionary);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_ItemSet_ValueOfWrongType(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary dictionary = new SortedList<string, string>();
                object missingKey = GetNewKey(dictionary);
                Assert.Throws<ArgumentException>(() => dictionary[missingKey] = 324);
                Assert.Empty(dictionary);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Add_KeyOfWrongType(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary dictionary = new SortedList<string, string>();
                object missingKey = 23;
                Assert.Throws<ArgumentException>(() => dictionary.Add(missingKey, CreateTValue(12345)));
                Assert.Empty(dictionary);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Add_ValueOfWrongType(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary dictionary = new SortedList<string, string>();
                object missingKey = GetNewKey(dictionary);
                Assert.Throws<ArgumentException>(() => dictionary.Add(missingKey, 324));
                Assert.Empty(dictionary);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Add_NullValueWhenDefaultTValueIsNonNull(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary dictionary = new SortedList<string, int>();
                object missingKey = GetNewKey(dictionary);
                Assert.Throws<ArgumentNullException>(() => dictionary.Add(missingKey, null));
                Assert.Empty(dictionary);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IDictionary_NonGeneric_Contains_KeyOfWrongType(int count)
        {
            if (!IsReadOnly)
            {
                IDictionary dictionary = new SortedList<string, int>();
                Assert.False(dictionary.Contains(1));
            }
        }

        #endregion

        #region ICollection tests

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_CopyTo_ArrayOfIncorrectKeyValuePairType(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            KeyValuePair<string, int>[] array = new KeyValuePair<string, int>[count * 3 / 2];
            Assert.Throws<ArgumentException>(() => collection.CopyTo(array, 0));
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_CopyTo_ArrayOfCorrectKeyValuePairType(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            KeyValuePair<string, string>[] array = new KeyValuePair<string, string>[count];
            collection.CopyTo(array, 0);
            int i = 0;
            foreach (object obj in collection)
                Assert.Equal(array[i++], obj);
        }

        #endregion
    }
}
