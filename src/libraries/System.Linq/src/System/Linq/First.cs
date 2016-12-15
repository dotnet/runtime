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
            bool found;
            TSource first = source.TryGetFirst(out found);
            
            if (!found)
            {
                throw Error.NoElements();
            }

            return first;
        }

        public static TSource First<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            bool found;
            TSource first = source.TryGetFirst(predicate, out found);

            if (!found)
            {
                throw Error.NoMatch();
            }

            return first;
        }

        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source)
        {
            bool found;
            return source.TryGetFirst(out found);
        }

        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            bool found;
            return source.TryGetFirst(predicate, out found);
        }

        internal static TSource TryGetFirst<TSource>(this IEnumerable<TSource> source, out bool found)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            IPartition<TSource> partition = source as IPartition<TSource>;
            if (partition != null)
            {
                return partition.TryGetFirst(out found);
            }
            
            IList<TSource> list = source as IList<TSource>;
            if (list != null)
            {
                if (list.Count > 0)
                {
                    found = true;
                    return list[0];
                }
            }
            else
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (e.MoveNext())
                    {
                        found = true;
                        return e.Current;
                    }
                }
            }

            found = false;
            return default(TSource);
        }

        internal static TSource TryGetFirst<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, out bool found)
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
                return ordered.TryGetFirst(predicate, out found);
            }

            foreach (TSource element in source)
            {
                if (predicate(element))
                {
                    found = true;
                    return element;
                }
            }

            found = false;
            return default(TSource);
        }
    }
}
