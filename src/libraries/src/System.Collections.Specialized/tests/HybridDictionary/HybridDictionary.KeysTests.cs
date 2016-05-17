// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Tests;
using System.Diagnostics;

namespace System.Collections.Specialized.Tests
{
    public class HybridDictionaryKeysTests : ICollection_NonGeneric_Tests
    {
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfEnumType_ThrowType => typeof(InvalidCastException);
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectReferenceType_ThrowType => typeof(InvalidCastException);
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectValueType_ThrowType => typeof(InvalidCastException);

        protected override bool Enumerator_Current_UndefinedOperation_Throws => true;

        protected override bool IsReadOnly => true;

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
    }
}
