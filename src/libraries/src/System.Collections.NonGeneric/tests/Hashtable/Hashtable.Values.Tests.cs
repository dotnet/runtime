// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace System.Collections.Tests
{
    public class HashtableValuesTests : ICollection_NonGeneric_Tests
    {
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfEnumType_ThrowType => typeof(InvalidCastException);
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectReferenceType_ThrowType => typeof(InvalidCastException);
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectValueType_ThrowType => typeof(InvalidCastException);

        protected override bool IsReadOnly => true;
        protected override EnumerableOrder Order => EnumerableOrder.Unspecified;
        protected override bool SupportsSerialization => false;

        protected override bool Enumerator_Current_UndefinedOperation_Throws => true;
        protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations) => new List<ModifyEnumerable>();

        protected override ICollection NonGenericICollectionFactory() => new Hashtable().Values;

        protected override ICollection NonGenericICollectionFactory(int count)
        {
            Hashtable hashtable = new Hashtable();
            int seed = 13453;
            for (int i = 0; i < count; i++)
                hashtable.Add(CreateT(seed++), CreateT(seed++));
            return hashtable.Values;
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

            // A bug in Hashtable.Values.CopyTo means we don't check the lower bounds of the destination array
            if (count == 0)
            {
                collection.CopyTo(arr, 0);
            }
            else
            {
                Assert.Throws<IndexOutOfRangeException>(() => collection.CopyTo(arr, 0));
            }
        }
    }
}
