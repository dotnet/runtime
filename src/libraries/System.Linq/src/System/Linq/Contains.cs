// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static bool Contains<TSource>(this IEnumerable<TSource> source, TSource value) =>
            source is ICollection<TSource> collection ? collection.Contains(value) :
            Contains(source, value, null);

        public static bool Contains<TSource>(this IEnumerable<TSource> source, TSource value, IEqualityComparer<TSource>? comparer)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (source.TryGetSpan(out ReadOnlySpan<TSource> span))
            {
                return span.Contains(value, comparer);
            }

            if (comparer is null)
            {
                // While it's tempting, this must not delegate to ICollection<TSource>.Contains, as the historical semantics
                // of a null comparer with this method are to use EqualityComparer<TSource>.Default, and that might differ
                // from the semantics encoded in ICollection<TSource>.Contains.

                if (typeof(TSource).IsValueType)
                {
                    foreach (TSource element in source)
                    {
                        if (EqualityComparer<TSource>.Default.Equals(element, value))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                comparer = EqualityComparer<TSource>.Default;
            }
            foreach (TSource element in source)
            {
                if (comparer.Equals(element, value))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
