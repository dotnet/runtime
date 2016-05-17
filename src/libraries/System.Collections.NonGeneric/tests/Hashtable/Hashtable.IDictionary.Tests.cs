// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Collections.Tests
{
    public class HashtableBasicTestBase : HashtableIDictionaryTestBase
    {
        protected override IDictionary NonGenericIDictionaryFactory() => new Hashtable();
    }

    public class HashtableSynchronizedTestBase : HashtableIDictionaryTestBase
    {
        protected override bool ExpectedIsSynchronized => true;

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
    }
}
