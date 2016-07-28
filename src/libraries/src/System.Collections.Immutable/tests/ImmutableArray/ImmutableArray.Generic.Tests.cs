// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Tests;

namespace System.Collections.Immutable.Tests
{
    public class ImmutableArray_Generic_Int_Tests : ImmutableArray_Generic_Tests<int>
    {
        protected override int CreateT(int seed)
        {
            Random rand = new Random(seed);
            return rand.Next();
        }
    }

    public abstract class ImmutableArray_Generic_Tests<T> : IList_Generic_Tests<T>
    {
        protected override bool IsReadOnly => true;
        protected override IEnumerable<ModifyEnumerable> ModifyEnumerables => new List<ModifyEnumerable>();
        protected override Type IList_Generic_Item_InvalidIndex_ThrowType => typeof(IndexOutOfRangeException);
        protected override bool Enumerator_Current_UndefinedOperation_Throws => true;

        protected override IList<T> GenericIListFactory() => GenericIListFactory(0);
        protected override IList<T> GenericIListFactory(int count)
        {
            T[] items = new T[count];
            for (int i = 0; i < count; i++)
            {
                items[i] = CreateT(i);
            }
            return ImmutableArray.Create(items);
        }
    }
}
