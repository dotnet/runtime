// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Tests;
using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class ListDictionary_NoComparer_Tests : ListDictionaryTestBase
    {
        protected override IDictionary NonGenericIDictionaryFactory() => new ListDictionary();
    }

    public class ListDictionary_CustomComparer_Tests : ListDictionaryTestBase
    {
        protected override IDictionary NonGenericIDictionaryFactory() => new ListDictionary(StringComparer.Ordinal);
    }

    public abstract class ListDictionaryTestBase : IDictionary_NonGeneric_Tests
    {
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfEnumType_ThrowType => typeof(InvalidCastException);
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectReferenceType_ThrowType => typeof(InvalidCastException);
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectValueType_ThrowType => typeof(InvalidCastException);
        protected override Type ICollection_NonGeneric_CopyTo_NonZeroLowerBound_ThrowType => typeof(IndexOutOfRangeException);

        protected override object CreateTKey(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes = new byte[stringLength];
            rand.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        protected override object CreateTValue(int seed) => CreateTKey(seed);

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public override void ICollection_NonGeneric_CopyTo_IndexLargerThanArrayCount_ThrowsAnyArgumentException(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            object[] array = new object[count];
            
            Assert.Throws(ICollection_NonGeneric_CopyTo_IndexLargerThanArrayCount_ThrowType, () => collection.CopyTo(array, count + 1));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public override void ICollection_NonGeneric_CopyTo_NonZeroLowerBound(int count)
        {
            if (!PlatformDetection.IsNonZeroLowerBoundArraySupported)
                return;

            ICollection collection = NonGenericICollectionFactory(count);

            Array arr = Array.CreateInstance(typeof(object), new int[1] { count }, new int[1] { 2 });
            Assert.Equal(1, arr.Rank);
            Assert.Equal(2, arr.GetLowerBound(0));
            if (count == 0)
            {
                collection.CopyTo(arr, count);
                Assert.Equal(0, arr.Length);
                return;
            }

            Assert.Throws(ICollection_NonGeneric_CopyTo_NonZeroLowerBound_ThrowType, () => collection.CopyTo(arr, 0));
        }
    }
}
