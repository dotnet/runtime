// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Tests
{
    public class HashtableBasicTests : HashtableIDictionaryTestBase
    {
        protected override IDictionary NonGenericIDictionaryFactory() => new Hashtable();
    }

    public class HashtableSynchronizedTests : HashtableIDictionaryTestBase
    {
        protected override bool ExpectedIsSynchronized => true;
        protected override bool SupportsSerialization => false;

        protected override IDictionary NonGenericIDictionaryFactory() => Hashtable.Synchronized(new Hashtable());
    }

    public abstract class HashtableIDictionaryTestBase : IDictionary_NonGeneric_Tests
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
        public override void ICollection_NonGeneric_CopyTo_NonZeroLowerBound(int count)
        {
            if (!PlatformDetection.IsNonZeroLowerBoundArraySupported)
                return;

            ICollection collection = NonGenericICollectionFactory(count);
            Array arr = Array.CreateInstance(typeof(object), new int[] { count }, new int[] { 2 });
            Assert.Equal(1, arr.Rank);
            Assert.Equal(2, arr.GetLowerBound(0));

            // A bug in Hashtable.CopyTo means we don't check the lower bounds of the destination array
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
