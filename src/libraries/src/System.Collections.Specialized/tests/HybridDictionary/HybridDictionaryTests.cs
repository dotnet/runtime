// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Tests;

namespace System.Collections.Specialized.Tests
{
    public class HybridDictionary_CaseSensitive_Tests : HybridDictionaryTestBase
    {
        protected override IDictionary NonGenericIDictionaryFactory() => new HybridDictionary(false);
    }

    public class HybridDictionary_CaseInsensitive_Tests : HybridDictionaryTestBase
    {
        protected override IDictionary NonGenericIDictionaryFactory() => new HybridDictionary(true);
    }

    public abstract class HybridDictionaryTestBase : IDictionary_NonGeneric_Tests
    {
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfEnumType_ThrowType => typeof(InvalidCastException);
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectReferenceType_ThrowType => typeof(InvalidCastException);
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectValueType_ThrowType => typeof(InvalidCastException);

        protected override Type ICollection_NonGeneric_SyncRootType => typeof(HybridDictionary);

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
