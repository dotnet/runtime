// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static TSource[] ToArray<TSource>(this IEnumerable<TSource> source)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (source is IIListProvider<TSource> arrayProvider)
            {
                return arrayProvider.ToArray();
            }

            if (source is ICollection<TSource> collection)
            {
                int count = collection.Count;
                if (count != 0)
                {
                    var result = new TSource[count];
                    collection.CopyTo(result, 0);
                    return result;
                }

                return [];
            }

            return EnumerableToArray(source);

            [MethodImpl(MethodImplOptions.NoInlining)] // avoid large stack allocation impacting other paths
            static TSource[] EnumerableToArray(IEnumerable<TSource> source)
            {
                SegmentedArrayBuilder<TSource>.ScratchBuffer scratch = default;
                SegmentedArrayBuilder<TSource> builder = new(scratch);

                builder.AddNonICollectionRangeInlined(source);
                TSource[] result = builder.ToArray();

                builder.Dispose();
                return result;
            }
        }

        public static List<TSource> ToList<TSource>(this IEnumerable<TSource> source)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (source is IIListProvider<TSource> listProvider)
            {
                return listProvider.ToList();
            }

            return new List<TSource>(source);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey,TValue}"/> from an <see cref="IEnumerable{T}"/> according to the default comparer for the key type.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys from elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values from elements of <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to create a <see cref="Dictionary{TKey,TValue}"/> from.</param>
        /// <returns>A <see cref="Dictionary{TKey,TValue}"/> that contains keys and values from <paramref name="source"/> and uses default comparer for the key type.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is a null reference.</exception>
        /// <exception cref="ArgumentException"><paramref name="source"/> contains one or more duplicate keys.</exception>
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source) where TKey : notnull =>
            source.ToDictionary(null);

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey,TValue}"/> from an <see cref="IEnumerable{T}"/> according to specified key comparer.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys from elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values from elements of <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to create a <see cref="Dictionary{TKey,TValue}"/> from.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{TKey}"/> to compare keys.</param>
        /// <returns>A <see cref="Dictionary{TKey,TValue}"/> that contains keys and values from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is a null reference.</exception>
        /// <exception cref="ArgumentException"><paramref name="source"/> contains one or more duplicate keys.</exception>
        /// <remarks>
        /// If <paramref name="comparer"/> is null, the default equality comparer <see cref="EqualityComparer{TKey}.Default"/> is used to compare keys.
        /// </remarks>
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey>? comparer) where TKey : notnull
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return new(source, comparer);
        }

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey,TValue}"/> from an <see cref="IEnumerable{T}"/> according to the default comparer for the key type.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys from elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values from elements of <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to create a <see cref="Dictionary{TKey,TValue}"/> from.</param>
        /// <returns>A <see cref="Dictionary{TKey,TValue}"/> that contains keys and values from <paramref name="source"/> and uses default comparer for the key type.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is a null reference.</exception>
        /// <exception cref="ArgumentException"><paramref name="source"/> contains one or more duplicate keys.</exception>
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<(TKey Key, TValue Value)> source) where TKey : notnull =>
            source.ToDictionary(null);

        /// <summary>
        /// Creates a <see cref="Dictionary{TKey,TValue}"/> from an <see cref="IEnumerable{T}"/> according to specified key comparer.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys from elements of <paramref name="source"/></typeparam>
        /// <typeparam name="TValue">The type of the values from elements of <paramref name="source"/></typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to create a <see cref="Dictionary{TKey,TValue}"/> from.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{TKey}"/> to compare keys.</param>
        /// <returns>A <see cref="Dictionary{TKey,TValue}"/> that contains keys and values from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is a null reference.</exception>
        /// <exception cref="ArgumentException"><paramref name="source"/> contains one or more duplicate keys.</exception>
        /// <remarks>
        /// If <paramref name="comparer"/> is null, the default equality comparer <see cref="EqualityComparer{TKey}.Default"/> is used to compare keys.
        /// </remarks>
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<(TKey Key, TValue Value)> source, IEqualityComparer<TKey>? comparer) where TKey : notnull =>
            source.ToDictionary(vt => vt.Key, vt => vt.Value, comparer);

        public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) where TKey : notnull =>
            ToDictionary(source, keySelector, null);

        public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (keySelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }

            if (source.TryGetNonEnumeratedCount(out int capacity))
            {
                if (capacity == 0)
                {
                    return new Dictionary<TKey, TSource>(comparer);
                }

                if (source is TSource[] array)
                {
                    return SpanToDictionary(array, keySelector, comparer);
                }

                if (source is List<TSource> list)
                {
                    ReadOnlySpan<TSource> span = CollectionsMarshal.AsSpan(list);
                    return SpanToDictionary(span, keySelector, comparer);
                }
            }

            Dictionary<TKey, TSource> d = new Dictionary<TKey, TSource>(capacity, comparer);
            foreach (TSource element in source)
            {
                d.Add(keySelector(element), element);
            }

            return d;
        }

        private static Dictionary<TKey, TSource> SpanToDictionary<TSource, TKey>(ReadOnlySpan<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
        {
            Dictionary<TKey, TSource> d = new Dictionary<TKey, TSource>(source.Length, comparer);
            foreach (TSource element in source)
            {
                d.Add(keySelector(element), element);
            }
            return d;
        }

        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector) where TKey : notnull =>
            ToDictionary(source, keySelector, elementSelector, null);

        public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (keySelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }

            if (elementSelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementSelector);
            }

            if (source.TryGetNonEnumeratedCount(out int capacity))
            {
                if (capacity == 0)
                {
                    return new Dictionary<TKey, TElement>(comparer);
                }

                if (source is TSource[] array)
                {
                    return SpanToDictionary(array, keySelector, elementSelector, comparer);
                }

                if (source is List<TSource> list)
                {
                    ReadOnlySpan<TSource> span = CollectionsMarshal.AsSpan(list);
                    return SpanToDictionary(span, keySelector, elementSelector, comparer);
                }
            }

            Dictionary<TKey, TElement> d = new Dictionary<TKey, TElement>(capacity, comparer);
            foreach (TSource element in source)
            {
                d.Add(keySelector(element), elementSelector(element));
            }

            return d;
        }

        private static Dictionary<TKey, TElement> SpanToDictionary<TSource, TKey, TElement>(ReadOnlySpan<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
        {
            Dictionary<TKey, TElement> d = new Dictionary<TKey, TElement>(source.Length, comparer);
            foreach (TSource element in source)
            {
                d.Add(keySelector(element), elementSelector(element));
            }
            return d;
        }

        public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source) => source.ToHashSet(comparer: null);

        public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource>? comparer)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            // Don't pre-allocate based on knowledge of size, as potentially many elements will be dropped.
            return new HashSet<TSource>(source, comparer);
        }

        /// <summary>Default initial capacity to use when creating sets for internal temporary storage.</summary>
        /// <remarks>This is based on the implicit size used in previous implementations, which used a custom Set type.</remarks>
        private const int DefaultInternalSetCapacity = 7;

        private static TSource[] HashSetToArray<TSource>(HashSet<TSource> set)
        {
            var result = new TSource[set.Count];
            set.CopyTo(result);
            return result;
        }
    }
}
