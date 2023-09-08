// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TResult> Empty<TResult>() => Array.Empty<TResult>();

        private static TResult[]? GetEmptyIfEmpty<TSource, TResult>(IEnumerable<TSource> source) =>
            source is TSource[] { Length: 0 } ?
                Array.Empty<TResult>() :
                null;
    }
}
