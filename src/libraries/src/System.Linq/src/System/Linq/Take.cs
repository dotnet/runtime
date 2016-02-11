// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TSource> Take<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (count <= 0) return new EmptyPartition<TSource>();
            IPartition<TSource> partition = source as IPartition<TSource>;
            if (partition != null) return partition.Take(count);
            return TakeIterator(source, count);
        }

        private static IEnumerable<TSource> TakeIterator<TSource>(IEnumerable<TSource> source, int count)
        {
            foreach (TSource element in source)
            {
                yield return element;
                if (--count == 0) break;
            }
        }

        public static IEnumerable<TSource> TakeWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (predicate == null) throw Error.ArgumentNull("predicate");
            return TakeWhileIterator(source, predicate);
        }

        private static IEnumerable<TSource> TakeWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            foreach (TSource element in source)
            {
                if (!predicate(element)) break;
                yield return element;
            }
        }

        public static IEnumerable<TSource> TakeWhile<TSource>(this IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (predicate == null) throw Error.ArgumentNull("predicate");
            return TakeWhileIterator(source, predicate);
        }

        private static IEnumerable<TSource> TakeWhileIterator<TSource>(IEnumerable<TSource> source, Func<TSource, int, bool> predicate)
        {
            int index = -1;
            foreach (TSource element in source)
            {
                checked { index++; }
                if (!predicate(element, index)) break;
                yield return element;
            }
        }
    }
}
