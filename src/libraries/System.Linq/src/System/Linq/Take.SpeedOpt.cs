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
            Debug.Assert(source != null);
            Debug.Assert(count > 0);

            return
                source is IPartition<TSource> partition ? partition.Take(count) :
                source is IList<TSource> sourceList ? new ListPartition<TSource>(sourceList, 0, count - 1) :
                new EnumerablePartition<TSource>(source, 0, count - 1);
        }

        private static IEnumerable<TSource> TakeRangeIterator<TSource>(IEnumerable<TSource> source, int startIndex, int endIndex)
        {
            Debug.Assert(source != null);
            Debug.Assert(startIndex >= 0 && startIndex < endIndex);

            return
                source is IPartition<TSource> partition ? TakePartitionRange(partition, startIndex, endIndex) :
                source is IList<TSource> sourceList ? new ListPartition<TSource>(sourceList, startIndex, endIndex - 1) :
                new EnumerablePartition<TSource>(source, startIndex, endIndex - 1);

            static IPartition<TSource> TakePartitionRange(IPartition<TSource> partition, int startIndex, int endIndex)
            {
                partition = endIndex == 0 ? EmptyPartition<TSource>.Instance : partition.Take(endIndex);
                return startIndex == 0 ? partition : partition.Skip(startIndex);
            }
        }
    }
}
