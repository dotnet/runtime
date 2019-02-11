// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static TSource Last<TSource>(this IEnumerable<TSource> source)
        {
            TSource last = source.TryGetLast(out bool found);
            if (!found)
            {
                ThrowHelper.ThrowNoElementsException();
            }

            return last;
        }

        public static TSource Last<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            TSource last = source.TryGetLast(predicate, out bool found);
            if (!found)
            {
                ThrowHelper.ThrowNoMatchException();
            }

            return last;
        }

        public static TSource LastOrDefault<TSource>(this IEnumerable<TSource> source) =>
            source.TryGetLast(out bool _);

        public static TSource LastOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) =>
            source.TryGetLast(predicate, out bool _);

        private static TSource TryGetLast<TSource>(this IEnumerable<TSource> source, out bool found)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (source is IPartition<TSource> partition)
            {
                return partition.TryGetLast(out found);
            }

            if (source is IList<TSource> list)
            {
                int count = list.Count;
                if (count > 0)
                {
                    found = true;
                    return list[count - 1];
                }
            }
            else
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (e.MoveNext())
                    {
                        TSource result;
                        do
                        {
                            result = e.Current;
                        }
                        while (e.MoveNext());

                        found = true;
                        return result;
                    }
                }
            }

            found = false;
            return default(TSource);
        }

        private static TSource TryGetLast<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, out bool found)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            if (source is OrderedEnumerable<TSource> ordered)
            {
                return ordered.TryGetLast(predicate, out found);
            }

            if (source is IList<TSource> list)
            {
                for (int i = list.Count - 1; i >= 0; --i)
                {
                    TSource result = list[i];
                    if (predicate(result))
                    {
                        found = true;
                        return result;
                    }
                }
            }
            else
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    while (e.MoveNext())
                    {
                        TSource result = e.Current;
                        if (predicate(result))
                        {
                            while (e.MoveNext())
                            {
                                TSource element = e.Current;
                                if (predicate(element))
                                {
                                    result = element;
                                }
                            }

                            found = true;
                            return result;
                        }
                    }
                }
            }

            found = false;
            return default(TSource);
        }
    }
}
