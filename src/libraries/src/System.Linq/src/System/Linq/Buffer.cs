// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace System.Linq
{
    internal struct Buffer<TElement>
    {
        internal TElement[] items;
        internal int count;

        internal Buffer(IEnumerable<TElement> source)
        {
            IArrayProvider<TElement> iterator = source as IArrayProvider<TElement>;
            if (iterator != null)
            {
                TElement[] array = iterator.ToArray();
                items = array;
                count = array.Length;
            }
            else
            {
                items = EnumerableHelpers.ToArray(source, out count);
            }
        }
    }
}
