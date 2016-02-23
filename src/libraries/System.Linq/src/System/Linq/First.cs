// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static TSource First<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            IPartition<TSource> partition = source as IPartition<TSource>;
            if (partition != null)
            {
                bool found;
                TSource first = partition.TryGetFirst(out found);
                if (found)
                {
                    return first;
                }
            }
            else
            {
                IList<TSource> list = source as IList<TSource>;
                if (list != null)
                {
                    if (list.Count > 0)
                    {
                        return list[0];
                    }
                }
                else
                {
                    using (IEnumerator<TSource> e = source.GetEnumerator())
                    {
                        if (e.MoveNext())
                        {
                            return e.Current;
                        }
                    }
                }
            }

            throw Error.NoElements();
        }

        public static TSource First<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            if (predicate == null)
            {
                throw Error.ArgumentNull(nameof(predicate));
            }

            OrderedEnumerable<TSource> ordered = source as OrderedEnumerable<TSource>;
            if (ordered != null)
            {
                return ordered.First(predicate);
            }

            foreach (TSource element in source)
            {
                if (predicate(element))
                {
                    return element;
                }
            }

            throw Error.NoMatch();
        }

        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            IPartition<TSource> partition = source as IPartition<TSource>;
            if (partition != null)
            {
                bool found;
                return partition.TryGetFirst(out found);
            }

            IList<TSource> list = source as IList<TSource>;
            if (list != null)
            {
                if (list.Count > 0)
                {
                    return list[0];
                }
            }
            else
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (e.MoveNext())
                    {
                        return e.Current;
                    }
                }
            }

            return default(TSource);
        }

        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            if (predicate == null)
            {
                throw Error.ArgumentNull(nameof(predicate));
            }

            OrderedEnumerable<TSource> ordered = source as OrderedEnumerable<TSource>;
            if (ordered != null)
            {
                return ordered.FirstOrDefault(predicate);
            }

            foreach (TSource element in source)
            {
                if (predicate(element))
                {
                    return element;
                }
            }

            return default(TSource);
        }
    }
}
