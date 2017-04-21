// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Tests;
using System.Diagnostics;
using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class HybridDictionaryKeysTests : ICollection_NonGeneric_Tests
    {
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfEnumType_ThrowType => typeof(InvalidCastException);
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectReferenceType_ThrowType => typeof(InvalidCastException);
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectValueType_ThrowType => typeof(InvalidCastException);

        protected override bool Enumerator_Current_UndefinedOperation_Throws => true;

        protected override bool IsReadOnly => true;
        protected override bool SupportsSerialization => false;

        protected override ICollection NonGenericICollectionFactory() => new HybridDictionary().Keys;

        protected override ICollection NonGenericICollectionFactory(int count)
        {
            HybridDictionary list = new HybridDictionary();
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

        protected override void AddToCollection(ICollection collection, int numberOfItemsToAdd) => Debug.Assert(false);

        protected override IEnumerable<ModifyEnumerable> ModifyEnumerables => new List<ModifyEnumerable>();

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public override void ICollection_NonGeneric_CopyTo_NonZeroLowerBound(int count)
        {
            if (!PlatformDetection.IsNonZeroLowerBoundArraySupported)
                return;

            ICollection collection = NonGenericICollectionFactory(count);
            Array arr = Array.CreateInstance(typeof(object), new int[] { count }, new int[] { 2 });
            Assert.Equal(1, arr.Rank);
            Assert.Equal(2, arr.GetLowerBound(0));
            if (count == 0)
            {
                collection.CopyTo(arr, count);
                Assert.Equal(0, arr.Length);
                return;
            }

            Assert.Throws<IndexOutOfRangeException>(() => collection.CopyTo(arr, 0));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public override void ICollection_NonGeneric_CopyTo_IndexEqualToArrayCount_ThrowsArgumentException(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            object[] array = new object[count];
            if (count > 0)
                Assert.Throws(count < 10 ? typeof(IndexOutOfRangeException) : typeof(ArgumentException), () => collection.CopyTo(array, count));
            else
                collection.CopyTo(array, count); // does nothing since the array is empty
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public override void ICollection_NonGeneric_CopyTo_NotEnoughSpaceInOffsettedArray_ThrowsArgumentException(int count)
        {
            if (count > 0) // Want the T array to have at least 1 element
            {
                ICollection collection = NonGenericICollectionFactory(count);
                object[] array = new object[count];
                Assert.Throws(count < 10 ? typeof(IndexOutOfRangeException) : typeof(ArgumentException), () => collection.CopyTo(array, 1));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public override void ICollection_NonGeneric_CopyTo_IndexLargerThanArrayCount_ThrowsAnyArgumentException(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            object[] array = new object[count];
            if (count == 0)
            {
                collection.CopyTo(array, count + 1);
                Assert.Equal(count, array.Length);
                return;
            }

            Assert.Throws(count < 10 ? typeof(IndexOutOfRangeException) : typeof(ArgumentException), () => collection.CopyTo(array, count + 1));
        }
    }
}
