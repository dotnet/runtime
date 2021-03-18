// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static (List<TSource> Matched, List<TSource> Unmatched) Match<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            throw new NotImplementedException();
        }
    }
}
