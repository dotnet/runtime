// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static TSource Single<TSource>(this IEnumerable<TSource> source)
        {
            TSource? single = source.TryGetSingle(out bool found);
            if (!found)
            {
                ThrowHelper.ThrowNoElementsException();
            }

            return single!;
        }
        public static TSource Single<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            TSource? single = source.TryGetSingle(predicate, out bool found);
            if (!found)
            {
                ThrowHelper.ThrowNoMatchException();
            }

            return single!;
        }

        public static TSource? SingleOrDefault<TSource>(this IEnumerable<TSource> source)
            => source.TryGetSingle(out _);

        public static TSource SingleOrDefault<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
        {
            var single = source.TryGetSingle(out bool found);
            return found ? single! : defaultValue;
        }

        public static TSource? SingleOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
            => source.TryGetSingle(predicate, out _);

        public static TSource SingleOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, TSource defaultValue)
        {
            var single = source.TryGetSingle(predicate, out bool found);
            return found ? single! : defaultValue;
        }

        private static TSource? TryGetSingle<TSource>(this IEnumerable<TSource> source, out bool found)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (source is IList<TSource> list)
            {
                switch (list.Count)
                {
                    case 0:
                        found = false;
                        return default;
                    case 1:
                        found = true;
                        return list[0];
                }
            }
            else
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (!e.MoveNext())
                    {
                        found = false;
                        return default;
                    }

                    TSource result = e.Current;
                    if (!e.MoveNext())
                    {
                        found = true;
                        return result;
                    }
                }
            }

            found = false;
            ThrowHelper.ThrowMoreThanOneElementException();
            return default;
        }

        private static TSource? TryGetSingle<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, out bool found)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (predicate == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.predicate);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    TSource result = e.Current;
                    if (predicate(result))
                    {
                        while (e.MoveNext())
                        {
                            if (predicate(e.Current))
                            {
                                ThrowHelper.ThrowMoreThanOneMatchException();
                            }
                        }
                        found = true;
                        return result;
                    }
                }
            }

            found = false;
            return default;
        }
    }
}
