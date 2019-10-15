// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private static IEnumerable<TSource> TakeIterator<TSource>(IEnumerable<TSource> source, int count) =>
            source is IPartition<TSource> partition ? partition.Take(count) :
            source is IList<TSource> sourceList ? (IEnumerable<TSource>)new ListPartition<TSource>(sourceList, 0, count - 1) :
            new EnumerablePartition<TSource>(source, 0, count - 1);

        private static IEnumerable<TSource> TakeLastEnumerableFactory<TSource>(IEnumerable<TSource> source, int count)
        {
            Debug.Assert(count > 0);

            if (source is IPartition<TSource> partition)
            {
                int length = partition.GetCount(true);

                if (length >= 0)
                {
                    return length - count > 0 ? partition.Skip(length - count) : partition;
                }
            }
            else if (source is IList<TSource> sourceList)
            {
                int sourceCount = sourceList.Count;

                if (sourceCount > count)
                {
                    return new ListPartition<TSource>(sourceList, sourceCount - count, sourceCount);
                }
                else
                {
                    return new ListPartition<TSource>(sourceList, 0, sourceCount);
                }
            }

            return TakeLastIterator<TSource>(source, count);
        }
    }
}
