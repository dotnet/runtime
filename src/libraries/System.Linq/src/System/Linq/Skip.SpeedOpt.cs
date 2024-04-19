// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private static IEnumerable<TSource> SkipIterator<TSource>(IEnumerable<TSource> source, int count) =>
            source is IList<TSource> sourceList ?
                (IEnumerable<TSource>)new IListSkipTakeIterator<TSource>(sourceList, count, int.MaxValue) :
                new IEnumerableSkipTakeIterator<TSource>(source, count, -1);
    }
}
