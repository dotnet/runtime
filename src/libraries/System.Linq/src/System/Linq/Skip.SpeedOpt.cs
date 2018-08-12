// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private static IEnumerable<TSource> SkipIterator<TSource>(IEnumerable<TSource> source, int count) =>
            source is IList<TSource> sourceList ?
                (IEnumerable<TSource>)new ListPartition<TSource>(sourceList, count, int.MaxValue) :
                new EnumerablePartition<TSource>(source, count, -1);
    }
}
