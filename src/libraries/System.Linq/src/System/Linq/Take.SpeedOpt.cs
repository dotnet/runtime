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
            Debug.Assert(source != null && !IsEmptyArray(source));
            Debug.Assert(count > 0);

            return
                source is Iterator<TSource> iterator ? (iterator.Take(count) ?? Empty<TSource>()) :
                source is IList<TSource> sourceList ? new IListSkipTakeIterator<TSource>(sourceList, 0, count - 1) :
                new IEnumerableSkipTakeIterator<TSource>(source, 0, count - 1);
        }

        private static IEnumerable<TSource> TakeRangeIterator<TSource>(IEnumerable<TSource> source, int startIndex, int endIndex)
        {
            Debug.Assert(source != null && !IsEmptyArray(source));
            Debug.Assert(startIndex >= 0 && startIndex < endIndex);

            return
                source is Iterator<TSource> iterator ? TakeIteratorRange(iterator, startIndex, endIndex) :
                source is IList<TSource> sourceList ? new IListSkipTakeIterator<TSource>(sourceList, startIndex, endIndex - 1) :
                new IEnumerableSkipTakeIterator<TSource>(source, startIndex, endIndex - 1);

            static IEnumerable<TSource> TakeIteratorRange(Iterator<TSource> iterator, int startIndex, int endIndex)
            {
                Iterator<TSource>? source;
                if (endIndex != 0 &&
                    (source = iterator.Take(endIndex)) is not null &&
                    (startIndex == 0 || (source = source!.Skip(startIndex)) is not null))
                {
                    return source;
                }

                return [];
            }
        }
    }
}
