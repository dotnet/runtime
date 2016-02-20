// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static TSource ElementAt<TSource>(this IEnumerable<TSource> source, int index)
        {
            if (source == null) throw Error.ArgumentNull("source");
            IPartition<TSource> partition = source as IPartition<TSource>;
            if (partition != null)
            {
                bool found;
                TSource element = partition.TryGetElementAt(index, out found);
                if (found) return element;
            }
            else
            {
                IList<TSource> list = source as IList<TSource>;
                if (list != null) return list[index];
                if (index >= 0)
                {
                    using (IEnumerator<TSource> e = source.GetEnumerator())
                    {
                        while (e.MoveNext())
                        {
                            if (index == 0) return e.Current;
                            index--;
                        }
                    }
                }
            }
            throw Error.ArgumentOutOfRange("index");
        }

        public static TSource ElementAtOrDefault<TSource>(this IEnumerable<TSource> source, int index)
        {
            if (source == null) throw Error.ArgumentNull("source");
            IPartition<TSource> partition = source as IPartition<TSource>;
            if (partition != null)
            {
                bool found;
                return partition.TryGetElementAt(index, out found);
            }
            if (index >= 0)
            {
                IList<TSource> list = source as IList<TSource>;
                if (list != null)
                {
                    if (index < list.Count) return list[index];
                }
                else
                {
                    using (IEnumerator<TSource> e = source.GetEnumerator())
                    {
                        while (e.MoveNext())
                        {
                            if (index == 0) return e.Current;
                            index--;
                        }
                    }
                }
            }
            return default(TSource);
        }
    }
}
