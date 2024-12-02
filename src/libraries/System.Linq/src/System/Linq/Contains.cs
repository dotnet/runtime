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

            if (comparer is null)
            {
                // While it's tempting, this must not delegate to ICollection<TSource>.Contains, as the historical semantics
                // of a null comparer with this method are to use EqualityComparer<TSource>.Default, and that might differ
                // from the semantics encoded in ICollection<TSource>.Contains.

                // We don't bother special-casing spans here as explicitly providing a null comparer with a known collection type
                // is relatively rare. If you don't care about the comparer, you use the other overload, and while it will delegate
                // to this overload with a null comparer, it'll only do so for collections from which we can't extract a span.
                // And if you do care about the comparer, you're generally passing in a non-null one.

                foreach (TSource element in source)
                {
                    if (EqualityComparer<TSource>.Default.Equals(element, value))
                    {
                        return true;
                    }
                }
            }
            else if (source.TryGetSpan(out ReadOnlySpan<TSource> span))
            {
                foreach (TSource element in span)
                {
                    if (comparer.Equals(element, value))
                    {
                        return true;
                    }
                }
            }
            else
            {
                foreach (TSource element in source)
                {
                    if (comparer.Equals(element, value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
