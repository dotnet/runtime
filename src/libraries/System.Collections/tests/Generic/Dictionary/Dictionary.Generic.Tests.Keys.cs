// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace System.Collections.Tests
{
    public class Dictionary_Generic_Tests_Keys : ICollection_Generic_Tests<string>
    {
        protected override bool DefaultValueAllowed => false;
        protected override bool DuplicateValuesAllowed => false;
        protected override bool IsReadOnly => true;
        protected override IEnumerable<ModifyEnumerable> ModifyEnumerables => new List<ModifyEnumerable>();
        protected override ICollection<string> GenericICollectionFactory()
        {
            return new Dictionary<string, string>().Keys;
        }

        protected override ICollection<string> GenericICollectionFactory(int count)
        {
            Dictionary<string, string> list = new Dictionary<string, string>();
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

        protected override Type ICollection_Generic_CopyTo_IndexLargerThanArrayCount_ThrowType => typeof(ArgumentOutOfRangeException);

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Dictionary_Generic_KeyCollection_Constructor_NullDictionary(int count)
        {
            Assert.Throws<ArgumentNullException>(() => new Dictionary<string, string>.KeyCollection(null));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Dictionary_Generic_KeyCollection_GetEnumerator(int count)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            int seed = 13453;
            while (dictionary.Count < count)
                dictionary.Add(CreateT(seed++), CreateT(seed++));
            dictionary.Keys.GetEnumerator();
        }
    }

    public class Dictionary_Generic_Tests_Keys_AsICollection : ICollection_NonGeneric_Tests
    {
        protected override bool NullAllowed => false;
        protected override bool DuplicateValuesAllowed => false;
        protected override bool IsReadOnly => true;
        protected override bool Enumerator_Current_UndefinedOperation_Throws => true;
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfEnumType_ThrowType => typeof(ArgumentException);
        protected override bool SupportsSerialization => false;

        protected override Type ICollection_NonGeneric_CopyTo_IndexLargerThanArrayCount_ThrowType => typeof(ArgumentOutOfRangeException);

        protected override ICollection NonGenericICollectionFactory()
        {
            return new Dictionary<string, string>().Keys;
        }

        protected override ICollection NonGenericICollectionFactory(int count)
        {
            Dictionary<string, string> list = new Dictionary<string, string>();
            int seed = 13453;
            for (int i = 0; i < count; i++)
                list.Add(CreateT(seed++), CreateT(seed++));
            return list.Keys;
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

        protected override IEnumerable<ModifyEnumerable> ModifyEnumerables => new List<ModifyEnumerable>();

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void Dictionary_Generic_KeyCollection_CopyTo_ExactlyEnoughSpaceInTypeCorrectArray(int count)
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
