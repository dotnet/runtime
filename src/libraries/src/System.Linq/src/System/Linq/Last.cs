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
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            IPartition<TSource> partition = source as IPartition<TSource>;
            if (partition != null)
            {
                bool found;
                TSource last = partition.TryGetLast(out found);
                if (found)
                {
                    return last;
                }
            }
            else
            {
                IList<TSource> list = source as IList<TSource>;
                if (list != null)
                {
                    int count = list.Count;
                    if (count > 0)
                    {
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

                            return result;
                        }
                    }
                }
            }

            throw Error.NoElements();
        }

        public static TSource Last<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
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
                return ordered.Last(predicate);
            }

            IList<TSource> list = source as IList<TSource>;
            if (list != null)
            {
                for (int i = list.Count - 1; i >= 0; --i)
                {
                    TSource result = list[i];
                    if (predicate(result))
                    {
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

                            return result;
                        }
                    }
                }
            }

            throw Error.NoMatch();
        }

        public static TSource LastOrDefault<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            IPartition<TSource> partition = source as IPartition<TSource>;
            if (partition != null)
            {
                bool found;
                return partition.TryGetLast(out found);
            }

            IList<TSource> list = source as IList<TSource>;
            if (list != null)
            {
                int count = list.Count;
                if (count > 0)
                {
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

                        return result;
                    }
                }
            }

            return default(TSource);
        }

        public static TSource LastOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
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
                return ordered.LastOrDefault(predicate);
            }

            IList<TSource> list = source as IList<TSource>;
            if (list != null)
            {
                for (int i = list.Count - 1; i >= 0; --i)
                {
                    TSource element = list[i];
                    if (predicate(element))
                    {
                        return element;
                    }
                }

                return default(TSource);
            }
            else
            {
                TSource result = default(TSource);
                foreach (TSource element in source)
                {
                    if (predicate(element))
                    {
                        result = element;
                    }
                }

                return result;
            }
        }
    }
}
