// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace System.Collections.Tests
{
    public class SortedList_Generic_Tests_Keys : IList_Generic_Tests<string>
    {
        protected override bool DefaultValueAllowed => false;
        protected override bool DuplicateValuesAllowed => false;
        protected override bool IsReadOnly => true;
        protected override bool DefaultValueWhenNotAllowed_Throws => true;
        protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations) => new List<ModifyEnumerable>();

        protected override IList<string> GenericIListFactory()
        {
            return new SortedList<string, string>().Keys;
        }

        protected override IList<string> GenericIListFactory(int count)
        {
            SortedList<string, string> list = new SortedList<string, string>();
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
        public override void Enumerator_MoveNext_AfterDisposal(int count)
        {
            // Disposal of the enumerator is treated the same as a Reset call
            IEnumerator<string> enumerator = GenericIEnumerableFactory(count).GetEnumerator();
            for (int i = 0; i < count; i++)
                enumerator.MoveNext();
            enumerator.Dispose();
            if (count > 0)
                Assert.True(enumerator.MoveNext());
        }
    }

    public class SortedList_Generic_Tests_Keys_AsICollection : ICollection_NonGeneric_Tests
    {
        protected override bool NullAllowed => false;
        protected override bool DuplicateValuesAllowed => false;
        protected override bool IsReadOnly => true;
        protected override bool Enumerator_Current_UndefinedOperation_Throws => true;
        protected override bool SupportsSerialization => false;

        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfEnumType_ThrowType => typeof(ArgumentException);
        protected override Type ICollection_NonGeneric_CopyTo_NonZeroLowerBound_ThrowType => typeof(ArgumentOutOfRangeException);

        protected override ICollection NonGenericICollectionFactory()
        {
            return (ICollection)(new SortedList<string, string>().Keys);
        }

        protected override ICollection NonGenericICollectionFactory(int count)
        {
            SortedList<string, string> list = new SortedList<string, string>();
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

        protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations) => new List<ModifyEnumerable>();
    }
}
