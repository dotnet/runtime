// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq
{
    internal struct Buffer<TElement>
    {
        internal readonly TElement[] _items;
        internal readonly int _count;

        internal Buffer(IEnumerable<TElement> source)
        {
            IIListProvider<TElement> iterator = source as IIListProvider<TElement>;
            if (iterator != null)
            {
                TElement[] array = iterator.ToArray();
                _items = array;
                _count = array.Length;
            }
            else
            {
                _items = EnumerableHelpers.ToArray(source, out _count);
            }
        }
    }
}
