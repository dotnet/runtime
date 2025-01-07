// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public partial class Lookup<TKey, TElement>
    {
        internal TResult[] ToArray<TResult>(Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
        {
            TResult[] array = new TResult[_count];
            int index = 0;
            Grouping<TKey, TElement>? g = _lastGrouping;
            if (g is not null)
            {
                do
                {
                    g = g._next;
                    Debug.Assert(g is not null);

                    g.Trim();
                    array[index] = resultSelector(g._key, g._elements);
                    ++index;
                }
                while (g != _lastGrouping);
            }

            return array;
        }
    }
}
