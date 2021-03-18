// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static (List<TSource> Matched, List<TSource> Unmatched) Match<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            List<TSource> matched = new List<TSource>();
            List<TSource> unmatched = new List<TSource>();

            foreach (var element in source)
            {
                if (predicate(element))
                {
                    matched.Add(element);
                }
                else
                {
                    unmatched.Add(element);
                }
            }

            return (matched, unmatched);
        }
    }
}
