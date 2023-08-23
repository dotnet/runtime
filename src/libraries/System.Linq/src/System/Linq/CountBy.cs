// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer = null) where TKey : notnull
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }
            if (keySelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }

            Dictionary<TKey, int> countsBy = new(comparer);

            using IEnumerator<TSource> e = source.GetEnumerator();

            while (e.MoveNext())
            {
                TKey currentKey = keySelector(e.Current);

                ref int currentCount = ref CollectionsMarshal.GetValueRefOrAddDefault(countsBy, currentKey, out _);
                checked
                {
                    currentCount++;
                }
            }

            return countsBy;
        }
    }
}
