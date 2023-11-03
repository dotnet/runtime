// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TResult> Empty<TResult>() => EmptyPartition<TResult>.Instance;

        private static IEnumerable<TResult>? GetEmptyIfEmpty<TSource, TResult>(IEnumerable<TSource> source) =>
            source is EmptyPartition<TSource> ?
                EmptyPartition<TResult>.Instance :
                null;
    }
}
