// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A set of initialization methods for instances of <see cref="ImmutableSortedDictionary{TKey, TValue}"/>.
    /// </summary>
    public static class ImmutableSortedDictionary
    {
        /// <summary>
        /// Returns an empty collection.
        /// </summary>
        /// <typeparam name="TKey">The type of keys stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values stored by the dictionary.</typeparam>
        /// <returns>The immutable collection.</returns>
        public static ImmutableSortedDictionary<TKey, TValue> Create<TKey, TValue>() where TKey : notnull
        {
            return ImmutableSortedDictionary<TKey, TValue>.Empty;
        }

        /// <summary>
        /// Returns an empty collection.
        /// </summary>
        /// <typeparam name="TKey">The type of keys stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values stored by the dictionary.</typeparam>
        /// <param name="keyComparer">The key comparer.</param>
        /// <returns>The immutable collection.</returns>
        public static ImmutableSortedDictionary<TKey, TValue> Create<TKey, TValue>(IComparer<TKey>? keyComparer) where TKey : notnull
        {
            return ImmutableSortedDictionary<TKey, TValue>.Empty.WithComparers(keyComparer);
        }

        /// <summary>
        /// Returns an empty collection.
        /// </summary>
        /// <typeparam name="TKey">The type of keys stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values stored by the dictionary.</typeparam>
        /// <param name="keyComparer">The key comparer.</param>
        /// <param name="valueComparer">The value comparer.</param>
        /// <returns>The immutable collection.</returns>
        public static ImmutableSortedDictionary<TKey, TValue> Create<TKey, TValue>(IComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer) where TKey : notnull
        {
            return ImmutableSortedDictionary<TKey, TValue>.Empty.WithComparers(keyComparer, valueComparer);
        }

        /// <summary>
        /// Creates a new immutable collection prefilled with the specified items.
        /// </summary>
        /// <typeparam name="TKey">The type of keys stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values stored by the dictionary.</typeparam>
        /// <param name="items">The items to prepopulate.</param>
        /// <returns>The new immutable collection.</returns>
        public static ImmutableSortedDictionary<TKey, TValue> CreateRange<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> items) where TKey : notnull
        {
            return ImmutableSortedDictionary<TKey, TValue>.Empty.AddRange(items);
        }

        /// <summary>
        /// Creates a new immutable collection prefilled with the specified items.
        /// </summary>
        /// <typeparam name="TKey">The type of keys stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values stored by the dictionary.</typeparam>
        /// <param name="keyComparer">The key comparer.</param>
        /// <param name="items">The items to prepopulate.</param>
        /// <returns>The new immutable collection.</returns>
        public static ImmutableSortedDictionary<TKey, TValue> CreateRange<TKey, TValue>(IComparer<TKey>? keyComparer, IEnumerable<KeyValuePair<TKey, TValue>> items) where TKey : notnull
        {
            return ImmutableSortedDictionary<TKey, TValue>.Empty.WithComparers(keyComparer).AddRange(items);
        }

        /// <summary>
        /// Creates a new immutable collection prefilled with the specified items.
        /// </summary>
        /// <typeparam name="TKey">The type of keys stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values stored by the dictionary.</typeparam>
        /// <param name="keyComparer">The key comparer.</param>
        /// <param name="valueComparer">The value comparer.</param>
        /// <param name="items">The items to prepopulate.</param>
        /// <returns>The new immutable collection.</returns>
        public static ImmutableSortedDictionary<TKey, TValue> CreateRange<TKey, TValue>(IComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer, IEnumerable<KeyValuePair<TKey, TValue>> items) where TKey : notnull
        {
            return ImmutableSortedDictionary<TKey, TValue>.Empty.WithComparers(keyComparer, valueComparer).AddRange(items);
        }

        /// <summary>
        /// Creates a new immutable sorted dictionary builder.
        /// </summary>
        /// <typeparam name="TKey">The type of keys stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values stored by the dictionary.</typeparam>
        /// <returns>The immutable collection builder.</returns>
        public static ImmutableSortedDictionary<TKey, TValue>.Builder CreateBuilder<TKey, TValue>() where TKey : notnull
        {
            return Create<TKey, TValue>().ToBuilder();
        }

        /// <summary>
        /// Creates a new immutable sorted dictionary builder.
        /// </summary>
        /// <typeparam name="TKey">The type of keys stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values stored by the dictionary.</typeparam>
        /// <param name="keyComparer">The key comparer.</param>
        /// <returns>The immutable collection builder.</returns>
        public static ImmutableSortedDictionary<TKey, TValue>.Builder CreateBuilder<TKey, TValue>(IComparer<TKey>? keyComparer) where TKey : notnull
        {
            return Create<TKey, TValue>(keyComparer).ToBuilder();
        }

        /// <summary>
        /// Creates a new immutable sorted dictionary builder.
        /// </summary>
        /// <typeparam name="TKey">The type of keys stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values stored by the dictionary.</typeparam>
        /// <param name="keyComparer">The key comparer.</param>
        /// <param name="valueComparer">The value comparer.</param>
        /// <returns>The immutable collection builder.</returns>
        public static ImmutableSortedDictionary<TKey, TValue>.Builder CreateBuilder<TKey, TValue>(IComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer) where TKey : notnull
        {
            return Create<TKey, TValue>(keyComparer, valueComparer).ToBuilder();
        }

        /// <summary>
        /// Constructs an immutable sorted dictionary based on some transformation of a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of element in the sequence.</typeparam>
        /// <typeparam name="TKey">The type of key in the resulting map.</typeparam>
        /// <typeparam name="TValue">The type of value in the resulting map.</typeparam>
        /// <param name="source">The sequence to enumerate to generate the map.</param>
        /// <param name="keySelector">The function that will produce the key for the map from each sequence element.</param>
        /// <param name="elementSelector">The function that will produce the value for the map from each sequence element.</param>
        /// <param name="keyComparer">The key comparer to use for the map.</param>
        /// <param name="valueComparer">The value comparer to use for the map.</param>
        /// <returns>The immutable map.</returns>
        public static ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> elementSelector, IComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer) where TKey : notnull
        {
            Requires.NotNull(source, nameof(source));
            Requires.NotNull(keySelector, nameof(keySelector));
            Requires.NotNull(elementSelector, nameof(elementSelector));

            return ImmutableSortedDictionary<TKey, TValue>.Empty.WithComparers(keyComparer, valueComparer)
                .AddRange(source.Select(element => new KeyValuePair<TKey, TValue>(keySelector(element), elementSelector(element))));
        }

        /// <summary>
        /// Returns an immutable copy of the current contents of the builder's collection.
        /// </summary>
        /// <param name="builder">The builder to create the immutable map from.</param>
        /// <returns>An immutable map.</returns>
        public static ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TKey, TValue>(this ImmutableSortedDictionary<TKey, TValue>.Builder builder) where TKey : notnull
        {
            Requires.NotNull(builder, nameof(builder));

            return builder.ToImmutable();
        }

        /// <summary>
        /// Constructs an immutable sorted dictionary based on some transformation of a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of element in the sequence.</typeparam>
        /// <typeparam name="TKey">The type of key in the resulting map.</typeparam>
        /// <typeparam name="TValue">The type of value in the resulting map.</typeparam>
        /// <param name="source">The sequence to enumerate to generate the map.</param>
        /// <param name="keySelector">The function that will produce the key for the map from each sequence element.</param>
        /// <param name="elementSelector">The function that will produce the value for the map from each sequence element.</param>
        /// <param name="keyComparer">The key comparer to use for the map.</param>
        /// <returns>The immutable map.</returns>
        public static ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> elementSelector, IComparer<TKey>? keyComparer) where TKey : notnull
        {
            return ToImmutableSortedDictionary(source, keySelector, elementSelector, keyComparer, null);
        }

        /// <summary>
        /// Constructs an immutable sorted dictionary based on some transformation of a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of element in the sequence.</typeparam>
        /// <typeparam name="TKey">The type of key in the resulting map.</typeparam>
        /// <typeparam name="TValue">The type of value in the resulting map.</typeparam>
        /// <param name="source">The sequence to enumerate to generate the map.</param>
        /// <param name="keySelector">The function that will produce the key for the map from each sequence element.</param>
        /// <param name="elementSelector">The function that will produce the value for the map from each sequence element.</param>
        /// <returns>The immutable map.</returns>
        public static ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> elementSelector) where TKey : notnull
        {
            return ToImmutableSortedDictionary(source, keySelector, elementSelector, null, null);
        }

        /// <summary>
        /// Creates an immutable sorted dictionary given a sequence of key=value pairs.
        /// </summary>
        /// <typeparam name="TKey">The type of key in the map.</typeparam>
        /// <typeparam name="TValue">The type of value in the map.</typeparam>
        /// <param name="source">The sequence of key=value pairs.</param>
        /// <param name="keyComparer">The key comparer to use when building the immutable map.</param>
        /// <param name="valueComparer">The value comparer to use for the immutable map.</param>
        /// <returns>An immutable map.</returns>
        public static ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer) where TKey : notnull
        {
            Requires.NotNull(source, nameof(source));

            var existingDictionary = source as ImmutableSortedDictionary<TKey, TValue>;
            if (existingDictionary != null)
            {
                return existingDictionary.WithComparers(keyComparer, valueComparer);
            }

            return ImmutableSortedDictionary<TKey, TValue>.Empty.WithComparers(keyComparer, valueComparer).AddRange(source);
        }

        /// <summary>
        /// Creates an immutable sorted dictionary given a sequence of key=value pairs.
        /// </summary>
        /// <typeparam name="TKey">The type of key in the map.</typeparam>
        /// <typeparam name="TValue">The type of value in the map.</typeparam>
        /// <param name="source">The sequence of key=value pairs.</param>
        /// <param name="keyComparer">The key comparer to use when building the immutable map.</param>
        /// <returns>An immutable map.</returns>
        public static ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IComparer<TKey>? keyComparer) where TKey : notnull
        {
            return ToImmutableSortedDictionary(source, keyComparer, null);
        }

        /// <summary>
        /// Creates an immutable sorted dictionary given a sequence of key=value pairs.
        /// </summary>
        /// <typeparam name="TKey">The type of key in the map.</typeparam>
        /// <typeparam name="TValue">The type of value in the map.</typeparam>
        /// <param name="source">The sequence of key=value pairs.</param>
        /// <returns>An immutable map.</returns>
        public static ImmutableSortedDictionary<TKey, TValue> ToImmutableSortedDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source) where TKey : notnull
        {
            return ToImmutableSortedDictionary(source, null, null);
        }
    }
}
