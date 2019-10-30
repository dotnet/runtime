// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Collections.Tests
{
    public class SortedSet_ICollection_NonGeneric_Tests : ICollection_NonGeneric_Tests
    {
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfEnumType_ThrowType => typeof(ArgumentException);
        protected override bool Enumerator_Current_UndefinedOperation_Throws { get { return true; } }

        protected override void AddToCollection(ICollection collection, int numberOfItemsToAdd)
        {
            int seed = numberOfItemsToAdd * 34;
            for (int i = 0; i < numberOfItemsToAdd; i++)
                ((SortedSet<string>)collection).Add(CreateT(seed++));
        }

        protected override ICollection NonGenericICollectionFactory()
        {
            return new SortedSet<string>();
        }

        protected string CreateT(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes = new byte[stringLength];
            rand.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
