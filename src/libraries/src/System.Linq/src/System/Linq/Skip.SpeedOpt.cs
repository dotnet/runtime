// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private static IEnumerable<TSource> SkipIterator<TSource>(IEnumerable<TSource> source, int count) =>
            source is IList<TSource> sourceList ?
                (IEnumerable<TSource>)new ListPartition<TSource>(sourceList, count, int.MaxValue) :
                new EnumerablePartition<TSource>(source, count, -1);

        private static IEnumerable<TSource> SkipLastEnumerableFactory<TSource>(IEnumerable<TSource> source, int count)
        {
            Debug.Assert(count > 0);

            if (source is IPartition<TSource> partition)
            {
                int length = partition.GetCount(onlyIfCheap: true);

                if (length >= 0)
                {
                    return length - count > 0 ?
                        partition.Take(length - count) :
                        EmptyPartition<TSource>.Instance;
                }
            }
            else if (source is IList<TSource> sourceList)
            {
                int sourceCount = sourceList.Count;

                return sourceCount > count ?
                    new ListPartition<TSource>(sourceList, 0, sourceCount - count - 1) :
                    EmptyPartition<TSource>.Instance;
            }

            return SkipLastIterator<TSource>(source, count);
        }
    }
}
