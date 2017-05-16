// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace System.Collections.Tests
{
    public class SortedDictionary_Generic_Tests_Keys : ICollection_Generic_Tests<string>
    {
        protected override bool DefaultValueAllowed { get { return false; } }
        protected override bool DuplicateValuesAllowed { get { return false; } }
        protected override bool IsReadOnly { get { return true; } }
        protected override IEnumerable<ModifyEnumerable> ModifyEnumerables { get { return new List<ModifyEnumerable>(); } }

        protected override ICollection<string> GenericICollectionFactory()
        {
            return new SortedDictionary<string, string>().Keys;
        }

        protected override ICollection<string> GenericICollectionFactory(int count)
        {
            SortedDictionary<string, string> list = new SortedDictionary<string, string>();
            int seed = 13453;
            for (int i = 0; i < count; i++)
                list.Add(CreateT(seed++), CreateT(seed++));
            return list.Keys;
        }

        protected override string CreateT(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes = new byte[stringLength];
            rand.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedDictionary_Generic_KeyCollection_Constructor_NullDictionary(int count)
        {
            Assert.Throws<ArgumentNullException>(() => new SortedDictionary<string, string>.KeyCollection(null));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedDictionary_Generic_KeyCollection_GetEnumerator(int count)
        {
            SortedDictionary<string, string> dictionary = new SortedDictionary<string, string>();
            int seed = 13453;
            while (dictionary.Count < count)
                dictionary.Add(CreateT(seed++), CreateT(seed++));
            dictionary.Keys.GetEnumerator();
        }
    }

    public class SortedDictionary_Generic_Tests_Keys_AsICollection : ICollection_NonGeneric_Tests
    {
        protected override bool NullAllowed { get { return false; } }
        protected override bool DuplicateValuesAllowed { get { return false; } }
        protected override bool IsReadOnly { get { return true; } }
        protected override bool Enumerator_Current_UndefinedOperation_Throws { get { return true; } }
        protected override IEnumerable<ModifyEnumerable> ModifyEnumerables { get { return new List<ModifyEnumerable>(); } }
        protected override ICollection NonGenericICollectionFactory()
        {
            return (ICollection)(new SortedDictionary<string, string>().Keys);
        }
        protected override bool SupportsSerialization { get { return false; } }

        protected override ICollection NonGenericICollectionFactory(int count)
        {
            SortedDictionary<string, string> list = new SortedDictionary<string, string>();
            int seed = 13453;
            for (int i = 0; i < count; i++)
                list.Add(CreateT(seed++), CreateT(seed++));
            return (ICollection)(list.Keys);
        }

        private string CreateT(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes = new byte[stringLength];
            rand.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        protected override void AddToCollection(ICollection collection, int numberOfItemsToAdd)
        {
            Debug.Assert(false);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public override void ICollection_NonGeneric_CopyTo_ArrayOfIncorrectValueType(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            float[] array = new float[count * 3 / 2];
            Assert.Throws<InvalidCastException>(() => collection.CopyTo(array, 0));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedDictionary_Generic_KeyCollection_CopyTo_ExactlyEnoughSpaceInTypeCorrectArray(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            string[] array = new string[count];
            collection.CopyTo(array, 0);
            int i = 0;
            foreach (object obj in collection)
                Assert.Equal(array[i++], obj);
        }
    }
}
