// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Tests;

namespace System.Collections.Immutable.Tests
{
    public class ImmutableArray_NonGeneric_Tests : IList_NonGeneric_Tests
    {
        protected override bool IsReadOnly => true;
        protected override bool ExpectedFixedSize => true;
        protected override bool ExpectedIsSynchronized => true;
        protected override bool ICollection_NonGeneric_SupportsSyncRoot => false;

        protected override Type IList_NonGeneric_Item_InvalidIndex_ThrowType => typeof(IndexOutOfRangeException);
        protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations) => new List<ModifyEnumerable>();
        protected override bool Enumerator_Current_UndefinedOperation_Throws => true;

        protected override Type ICollection_NonGeneric_CopyTo_TwoDimensionArray_ThrowType => typeof(RankException);
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectReferenceType_ThrowType => typeof(InvalidCastException);
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectValueType_ThrowType => typeof(InvalidCastException);
        protected override Type ICollection_NonGeneric_CopyTo_NonZeroLowerBound_ThrowType => typeof(ArgumentOutOfRangeException);

        protected override IList NonGenericIListFactory() => NonGenericIListFactory(0);
        protected override IList NonGenericIListFactory(int count)
        {
            object[] items = new object[count];
            for (int i = 0; i < count; i++)
            {
                items[i] = CreateT(i);
            }
            return ImmutableArray.Create(items);
        }
    }
}
