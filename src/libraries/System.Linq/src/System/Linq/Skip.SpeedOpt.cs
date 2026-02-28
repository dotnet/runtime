// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private static IEnumerable<TSource> SpeedOptimizedSkipIterator<TSource>(IEnumerable<TSource> source, int count) =>
#if NET11_0_OR_GREATER // IList<T> : IReadOnlyList<T> on .NET 11+
            source is IReadOnlyList<TSource> sourceList ?
#else
            source is IList<TSource> sourceList ?
#endif
                (IEnumerable<TSource>)new IListSkipTakeIterator<TSource>(sourceList, count, int.MaxValue) :
                new IEnumerableSkipTakeIterator<TSource>(source, count, -1);
    }
}
