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
                source is IPartition<TSource> partition ? (partition.Take(count) ?? Empty<TSource>()) :
                source is IList<TSource> sourceList ? new ListPartition<TSource>(sourceList, 0, count - 1) :
                new EnumerablePartition<TSource>(source, 0, count - 1);
        }

        private static IEnumerable<TSource> TakeRangeIterator<TSource>(IEnumerable<TSource> source, int startIndex, int endIndex)
        {
            Debug.Assert(source != null && !IsEmptyArray(source));
            Debug.Assert(startIndex >= 0 && startIndex < endIndex);

            return
                source is IPartition<TSource> partition ? TakePartitionRange(partition, startIndex, endIndex) :
                source is IList<TSource> sourceList ? new ListPartition<TSource>(sourceList, startIndex, endIndex - 1) :
                new EnumerablePartition<TSource>(source, startIndex, endIndex - 1);

            static IEnumerable<TSource> TakePartitionRange(IPartition<TSource> partition, int startIndex, int endIndex)
            {
                IPartition<TSource>? source;
                if (endIndex != 0 &&
                    (source = partition.Take(endIndex)) is not null &&
                    (startIndex == 0 || (source = source!.Skip(startIndex)) is not null))
                {
                    return source;
                }

                return [];
            }
        }
    }
}
