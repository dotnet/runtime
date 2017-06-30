// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Tests
{
    public class ArrayListBasicTests : ArrayListIListTestBase
    {
        protected override bool SupportsSerialization => true;
        protected override IList NonGenericIListFactory() => new ArrayList();
    }

    public class ArrayListSynchronizedTests : ArrayListIListTestBase
    {
        protected override bool ExpectedIsSynchronized => true;

        protected override IList NonGenericIListFactory() => NonGenericIListFactory(0);
        protected override IList NonGenericIListFactory(int count) => ArrayList.Synchronized(Helpers.CreateIntArrayList(count));
    }

    public class ArrayListSynchronizedILstTests : ArrayListIListTestBase
    {
        protected override bool ExpectedIsSynchronized => true;

        protected override IList NonGenericIListFactory() => NonGenericIListFactory(0);
        protected override IList NonGenericIListFactory(int count) => ArrayList.Synchronized((IList)Helpers.CreateIntArrayList(count));
    }

    public class ArrayListFixedSizeTests : ArrayListIListTestBase
    {
        protected override bool ExpectedFixedSize => true;

        protected override IList NonGenericIListFactory() => NonGenericIListFactory(0);
        protected override IList NonGenericIListFactory(int count) => ArrayList.FixedSize(Helpers.CreateIntArrayList(count));
    }

    public class ArrayListFixedIListSizeTests : ArrayListIListTestBase
    {
        protected override bool ExpectedFixedSize => true;

        protected override IList NonGenericIListFactory() => NonGenericIListFactory(0);
        protected override IList NonGenericIListFactory(int count) => ArrayList.FixedSize((IList)Helpers.CreateIntArrayList(count));
    }

    public class ArrayListReadOnlyTests : ArrayListIListTestBase
    {
        protected override bool ExpectedFixedSize => true;
        protected override bool IsReadOnly => true;

        protected override IList NonGenericIListFactory() => NonGenericIListFactory(0);
        protected override IList NonGenericIListFactory(int count) => ArrayList.ReadOnly(Helpers.CreateIntArrayList(count));
    }

    public class ArrayListReadOnlyIListTests : ArrayListIListTestBase
    {
        protected override bool ExpectedFixedSize => true;
        protected override bool IsReadOnly => true;

        protected override IList NonGenericIListFactory() => NonGenericIListFactory(0);
        protected override IList NonGenericIListFactory(int count) => ArrayList.ReadOnly((IList)Helpers.CreateIntArrayList(count));
    }

    public class ArrayListAdapterTests : ArrayListIListTestBase
    {
        protected override IList NonGenericIListFactory() => NonGenericIListFactory(0);
        protected override IList NonGenericIListFactory(int count) => ArrayList.Adapter(Helpers.CreateIntArrayList(count));
    }

    public class ArrayListRangeTests : ArrayListIListTestBase
    {
        protected override IList NonGenericIListFactory() => NonGenericIListFactory(0);
        protected override IList NonGenericIListFactory(int count) => Helpers.CreateIntArrayList(count).GetRange(0, count);
    }

    public abstract class ArrayListIListTestBase : IList_NonGeneric_Tests
    {
        protected override bool Enumerator_Current_UndefinedOperation_Throws => true;
        // ArrayList supports serialization, but its nested types don't
        protected override bool SupportsSerialization => false;

        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfEnumType_ThrowType => typeof(InvalidCastException);
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectReferenceType_ThrowType => typeof(InvalidCastException);
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectValueType_ThrowType => typeof(InvalidCastException);
        protected override Type ICollection_NonGeneric_CopyTo_NonZeroLowerBound_ThrowType => typeof(ArgumentOutOfRangeException);

        protected override object CreateT(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes = new byte[stringLength];
            rand.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
