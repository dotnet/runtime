// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static bool SequenceEqual<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second) =>
            SequenceEqual(first, second, null);

        public static bool SequenceEqual<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource>? comparer)
        {
            if (first is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.first);
            }

            if (second is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.second);
            }

            if (first is IReadOnlyCollection<TSource> firstCol && second is IReadOnlyCollection<TSource> secondCol)
            {
                if (first.TryGetSpan(out ReadOnlySpan<TSource> firstSpan) && second.TryGetSpan(out ReadOnlySpan<TSource> secondSpan))
                {
                    return firstSpan.SequenceEqual(secondSpan, comparer);
                }

                if (firstCol.Count != secondCol.Count)
                {
                    return false;
                }

                if (firstCol is IReadOnlyList<TSource> firstList && secondCol is IReadOnlyList<TSource> secondList)
                {
                    comparer ??= EqualityComparer<TSource>.Default;

                    int count = firstCol.Count;
                    for (int i = 0; i < count; i++)
                    {
                        if (!comparer.Equals(firstList[i], secondList[i]))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            using (IEnumerator<TSource> e1 = first.GetEnumerator())
            using (IEnumerator<TSource> e2 = second.GetEnumerator())
            {
                comparer ??= EqualityComparer<TSource>.Default;

                while (e1.MoveNext())
                {
                    if (!(e2.MoveNext() && comparer.Equals(e1.Current, e2.Current)))
                    {
                        return false;
                    }
                }

                return !e2.MoveNext();
            }
        }
    }
}
