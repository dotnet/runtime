// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private static IEnumerable<TSource> TakeIterator<TSource>(IEnumerable<TSource> source, int count)
        {
            Debug.Assert(count > 0);

            foreach (TSource element in source)
            {
                yield return element;
                if (--count == 0) break;
            }
        }

        private static IEnumerable<TSource> TakeRangeIterator<TSource>(IEnumerable<TSource> source, int startIndex, int endIndex)
        {
            Debug.Assert(source != null);
            Debug.Assert(startIndex >= 0 && startIndex < endIndex);

            using IEnumerator<TSource> e = source.GetEnumerator();

            int index = 0;
            while (index < startIndex && e.MoveNext())
            {
                ++index;
            }

            if (index < startIndex)
            {
                yield break;
            }

            while (index < endIndex && e.MoveNext())
            {
                yield return e.Current;
                ++index;
            }
        }
    }
}
