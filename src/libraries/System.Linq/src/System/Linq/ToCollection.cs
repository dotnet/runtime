// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static TSource[] ToArray<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            IIListProvider<TSource> arrayProvider = source as IIListProvider<TSource>;
            return arrayProvider != null ? arrayProvider.ToArray() : EnumerableHelpers.ToArray(source);
        }

        public static List<TSource> ToList<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            IIListProvider<TSource> listProvider = source as IIListProvider<TSource>;
            return listProvider != null ? listProvider.ToList() : new List<TSource>(source);
        }

        public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return ToDictionary(source, keySelector, null);
        }

        public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (keySelector == null) throw Error.ArgumentNull("keySelector");

            int capacity = 0;
            ICollection<TSource> collection = source as ICollection<TSource>;
            if (collection != null)
            {
                capacity = collection.Count;
                if (capacity == 0)
                    return new Dictionary<TKey, TSource>(comparer);

                TSource[] array = collection as TSource[];
                if (array != null)
                    return ToDictionary(array, keySelector, comparer);
                List<TSource> list = collection as List<TSource>;
                if (list != null)
                    return ToDictionary(list, keySelector, comparer);
            }

            Dictionary<TKey, TSource> d = new Dictionary<TKey, TSource>(capacity, comparer);
            foreach (TSource element in source) d.Add(keySelector(element), element);
            return d;
        }

        private static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(TSource[] source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            Dictionary<TKey, TSource> d = new Dictionary<TKey, TSource>(source.Length, comparer);
            for (int i = 0; i < source.Length; i++) d.Add(keySelector(source[i]), source[i]);
            return d;
        }
        private static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(List<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            Dictionary<TKey, TSource> d = new Dictionary<TKey, TSource>(source.Count, comparer);
            foreach (TSource element in source) d.Add(keySelector(element), element);
            return d;
        }


        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            return ToDictionary(source, keySelector, elementSelector, null);
        }

        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (keySelector == null) throw Error.ArgumentNull("keySelector");
            if (elementSelector == null) throw Error.ArgumentNull("elementSelector");

            int capacity = 0;
            ICollection<TSource> collection = source as ICollection<TSource>;
            if (collection != null)
            {
                capacity = collection.Count;
                if (capacity == 0)
                    return new Dictionary<TKey, TElement>(comparer);

                TSource[] array = collection as TSource[];
                if (array != null)
                    return ToDictionary(array, keySelector, elementSelector, comparer);
                List<TSource> list = collection as List<TSource>;
                if (list != null)
                    return ToDictionary(list, keySelector, elementSelector, comparer);
            }

            Dictionary<TKey, TElement> d = new Dictionary<TKey, TElement>(capacity, comparer);
            foreach (TSource element in source) d.Add(keySelector(element), elementSelector(element));
            return d;
        }

        private static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(TSource[] source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            Dictionary<TKey, TElement> d = new Dictionary<TKey, TElement>(source.Length, comparer);
            for (int i = 0; i < source.Length; i++) d.Add(keySelector(source[i]), elementSelector(source[i]));
            return d;
        }

        private static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(List<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            Dictionary<TKey, TElement> d = new Dictionary<TKey, TElement>(source.Count, comparer);
            foreach (TSource element in source) d.Add(keySelector(element), elementSelector(element));
            return d;
        }
    }
}
