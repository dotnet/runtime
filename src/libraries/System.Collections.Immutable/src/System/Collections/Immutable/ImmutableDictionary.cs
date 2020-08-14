// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A set of initialization methods for instances of <see cref="ImmutableDictionary{TKey, TValue}"/>.
    /// </summary>
    public static class ImmutableDictionary
    {
        /// <summary>
        /// Returns an empty collection.
        /// </summary>
        /// <typeparam name="TKey">The type of keys stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values stored by the dictionary.</typeparam>
        /// <returns>The immutable collection.</returns>
        public static ImmutableDictionary<TKey, TValue> Create<TKey, TValue>() where TKey : notnull
        {
            return ImmutableDictionary<TKey, TValue>.Empty;
        }

        /// <summary>
        /// Returns an empty collection with the specified key comparer.
        /// </summary>
        /// <typeparam name="TKey">The type of keys stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values stored by the dictionary.</typeparam>
        /// <param name="keyComparer">The key comparer.</param>
        /// <returns>
        /// The immutable collection.
        /// </returns>
        public static ImmutableDictionary<TKey, TValue> Create<TKey, TValue>(IEqualityComparer<TKey>? keyComparer) where TKey : notnull
        {
            return ImmutableDictionary<TKey, TValue>.Empty.WithComparers(keyComparer);
        }

        /// <summary>
        /// Returns an empty collection with the specified comparers.
        /// </summary>
        /// <typeparam name="TKey">The type of keys stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values stored by the dictionary.</typeparam>
        /// <param name="keyComparer">The key comparer.</param>
        /// <param name="valueComparer">The value comparer.</param>
        /// <returns>
        /// The immutable collection.
        /// </returns>
        public static ImmutableDictionary<TKey, TValue> Create<TKey, TValue>(IEqualityComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer) where TKey : notnull
        {
            return ImmutableDictionary<TKey, TValue>.Empty.WithComparers(keyComparer, valueComparer);
        }

        /// <summary>
        /// Creates a new immutable collection prefilled with the specified items.
        /// </summary>
        /// <typeparam name="TKey">The type of keys stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values stored by the dictionary.</typeparam>
        /// <param name="items">The items to prepopulate.</param>
        /// <returns>The new immutable collection.</returns>
        public static ImmutableDictionary<TKey, TValue> CreateRange<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> items) where TKey : notnull
        {
            return ImmutableDictionary<TKey, TValue>.Empty.AddRange(items);
        }

        /// <summary>
        /// Creates a new immutable collection prefilled with the specified items.
        /// </summary>
        /// <typeparam name="TKey">The type of keys stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values stored by the dictionary.</typeparam>
        /// <param name="keyComparer">The key comparer.</param>
        /// <param name="items">The items to prepopulate.</param>
        /// <returns>The new immutable collection.</returns>
        public static ImmutableDictionary<TKey, TValue> CreateRange<TKey, TValue>(IEqualityComparer<TKey>? keyComparer, IEnumerable<KeyValuePair<TKey, TValue>> items) where TKey : notnull
        {
            return ImmutableDictionary<TKey, TValue>.Empty.WithComparers(keyComparer).AddRange(items);
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
        public static ImmutableDictionary<TKey, TValue> CreateRange<TKey, TValue>(IEqualityComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer, IEnumerable<KeyValuePair<TKey, TValue>> items) where TKey : notnull
        {
            return ImmutableDictionary<TKey, TValue>.Empty.WithComparers(keyComparer, valueComparer).AddRange(items);
        }

        /// <summary>
        /// Creates a new immutable dictionary builder.
        /// </summary>
        /// <typeparam name="TKey">The type of keys stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values stored by the dictionary.</typeparam>
        /// <returns>The new builder.</returns>
        public static ImmutableDictionary<TKey, TValue>.Builder CreateBuilder<TKey, TValue>() where TKey : notnull
        {
            return Create<TKey, TValue>().ToBuilder();
        }

        /// <summary>
        /// Creates a new immutable dictionary builder.
        /// </summary>
        /// <typeparam name="TKey">The type of keys stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values stored by the dictionary.</typeparam>
        /// <param name="keyComparer">The key comparer.</param>
        /// <returns>The new builder.</returns>
        public static ImmutableDictionary<TKey, TValue>.Builder CreateBuilder<TKey, TValue>(IEqualityComparer<TKey>? keyComparer) where TKey : notnull
        {
            return Create<TKey, TValue>(keyComparer).ToBuilder();
        }

        /// <summary>
        /// Creates a new immutable dictionary builder.
        /// </summary>
        /// <typeparam name="TKey">The type of keys stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values stored by the dictionary.</typeparam>
        /// <param name="keyComparer">The key comparer.</param>
        /// <param name="valueComparer">The value comparer.</param>
        /// <returns>The new builder.</returns>
        public static ImmutableDictionary<TKey, TValue>.Builder CreateBuilder<TKey, TValue>(IEqualityComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer) where TKey : notnull
        {
            return Create<TKey, TValue>(keyComparer, valueComparer).ToBuilder();
        }

        /// <summary>
        /// Constructs an immutable dictionary based on some transformation of a sequence.
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
        public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> elementSelector, IEqualityComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer) where TKey : notnull
        {
            Requires.NotNull(source, nameof(source));
            Requires.NotNull(keySelector, nameof(keySelector));
            Requires.NotNull(elementSelector, nameof(elementSelector));

            return ImmutableDictionary<TKey, TValue>.Empty.WithComparers(keyComparer, valueComparer)
                .AddRange(source.Select(element => new KeyValuePair<TKey, TValue>(keySelector(element), elementSelector(element))));
        }

        /// <summary>
        /// Returns an immutable copy of the current contents of the builder's collection.
        /// </summary>
        /// <param name="builder">The builder to create the immutable dictionary from.</param>
        /// <returns>An immutable dictionary.</returns>
        public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue>(this ImmutableDictionary<TKey, TValue>.Builder builder) where TKey : notnull
        {
            Requires.NotNull(builder, nameof(builder));

            return builder.ToImmutable();
        }

        /// <summary>
        /// Constructs an immutable dictionary based on some transformation of a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of element in the sequence.</typeparam>
        /// <typeparam name="TKey">The type of key in the resulting map.</typeparam>
        /// <typeparam name="TValue">The type of value in the resulting map.</typeparam>
        /// <param name="source">The sequence to enumerate to generate the map.</param>
        /// <param name="keySelector">The function that will produce the key for the map from each sequence element.</param>
        /// <param name="elementSelector">The function that will produce the value for the map from each sequence element.</param>
        /// <param name="keyComparer">The key comparer to use for the map.</param>
        /// <returns>The immutable map.</returns>
        public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> elementSelector, IEqualityComparer<TKey>? keyComparer) where TKey : notnull
        {
            return ToImmutableDictionary(source, keySelector, elementSelector, keyComparer, null);
        }

        /// <summary>
        /// Constructs an immutable dictionary based on some transformation of a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of element in the sequence.</typeparam>
        /// <typeparam name="TKey">The type of key in the resulting map.</typeparam>
        /// <param name="source">The sequence to enumerate to generate the map.</param>
        /// <param name="keySelector">The function that will produce the key for the map from each sequence element.</param>
        /// <returns>The immutable map.</returns>
        public static ImmutableDictionary<TKey, TSource> ToImmutableDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) where TKey : notnull
        {
            return ToImmutableDictionary(source, keySelector, v => v, null, null);
        }

        /// <summary>
        /// Constructs an immutable dictionary based on some transformation of a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of element in the sequence.</typeparam>
        /// <typeparam name="TKey">The type of key in the resulting map.</typeparam>
        /// <param name="source">The sequence to enumerate to generate the map.</param>
        /// <param name="keySelector">The function that will produce the key for the map from each sequence element.</param>
        /// <param name="keyComparer">The key comparer to use for the map.</param>
        /// <returns>The immutable map.</returns>
        public static ImmutableDictionary<TKey, TSource> ToImmutableDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? keyComparer) where TKey : notnull
        {
            return ToImmutableDictionary(source, keySelector, v => v, keyComparer, null);
        }

        /// <summary>
        /// Constructs an immutable dictionary based on some transformation of a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of element in the sequence.</typeparam>
        /// <typeparam name="TKey">The type of key in the resulting map.</typeparam>
        /// <typeparam name="TValue">The type of value in the resulting map.</typeparam>
        /// <param name="source">The sequence to enumerate to generate the map.</param>
        /// <param name="keySelector">The function that will produce the key for the map from each sequence element.</param>
        /// <param name="elementSelector">The function that will produce the value for the map from each sequence element.</param>
        /// <returns>The immutable map.</returns>
        public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> elementSelector) where TKey : notnull
        {
            return ToImmutableDictionary(source, keySelector, elementSelector, null, null);
        }

        /// <summary>
        /// Creates an immutable dictionary given a sequence of key=value pairs.
        /// </summary>
        /// <typeparam name="TKey">The type of key in the map.</typeparam>
        /// <typeparam name="TValue">The type of value in the map.</typeparam>
        /// <param name="source">The sequence of key=value pairs.</param>
        /// <param name="keyComparer">The key comparer to use when building the immutable map.</param>
        /// <param name="valueComparer">The value comparer to use for the immutable map.</param>
        /// <returns>An immutable map.</returns>
        public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer) where TKey : notnull
        {
            Requires.NotNull(source, nameof(source));

            var existingDictionary = source as ImmutableDictionary<TKey, TValue>;
            if (existingDictionary != null)
            {
                return existingDictionary.WithComparers(keyComparer, valueComparer);
            }

            return ImmutableDictionary<TKey, TValue>.Empty.WithComparers(keyComparer, valueComparer).AddRange(source);
        }

        /// <summary>
        /// Creates an immutable dictionary given a sequence of key=value pairs.
        /// </summary>
        /// <typeparam name="TKey">The type of key in the map.</typeparam>
        /// <typeparam name="TValue">The type of value in the map.</typeparam>
        /// <param name="source">The sequence of key=value pairs.</param>
        /// <param name="keyComparer">The key comparer to use when building the immutable map.</param>
        /// <returns>An immutable map.</returns>
        public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey>? keyComparer) where TKey : notnull
        {
            return ToImmutableDictionary(source, keyComparer, null);
        }

        /// <summary>
        /// Creates an immutable dictionary given a sequence of key=value pairs.
        /// </summary>
        /// <typeparam name="TKey">The type of key in the map.</typeparam>
        /// <typeparam name="TValue">The type of value in the map.</typeparam>
        /// <param name="source">The sequence of key=value pairs.</param>
        /// <returns>An immutable map.</returns>
        public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source) where TKey : notnull
        {
            return ToImmutableDictionary(source, null, null);
        }

        /// <summary>
        /// Determines whether this map contains the specified key-value pair.
        /// </summary>
        /// <typeparam name="TKey">The type of key in the map.</typeparam>
        /// <typeparam name="TValue">The type of value in the map.</typeparam>
        /// <param name="map">The map to search.</param>
        /// <param name="key">The key to check for.</param>
        /// <param name="value">The value to check for on a matching key, if found.</param>
        /// <returns>
        ///   <c>true</c> if this map contains the key-value pair; otherwise, <c>false</c>.
        /// </returns>
        public static bool Contains<TKey, TValue>(this IImmutableDictionary<TKey, TValue> map, TKey key, TValue value) where TKey : notnull
        {
            Requires.NotNull(map, nameof(map));
            Requires.NotNullAllowStructs(key, nameof(key));
            return map.Contains(new KeyValuePair<TKey, TValue>(key, value));
        }

        /// <summary>
        /// Gets the value for a given key if a matching key exists in the dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary to retrieve the value from.</param>
        /// <param name="key">The key to search for.</param>
        /// <returns>The value for the key, or the default value of type <typeparamref name="TValue"/> if no matching key was found.</returns>
        public static TValue? GetValueOrDefault<TKey, TValue>(this IImmutableDictionary<TKey, TValue> dictionary, TKey key) where TKey : notnull
        {
            return GetValueOrDefault(dictionary, key, default(TValue)!);
        }

        /// <summary>
        /// Gets the value for a given key if a matching key exists in the dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="dictionary">The dictionary to retrieve the value from.</param>
        /// <param name="key">The key to search for.</param>
        /// <param name="defaultValue">The default value to return if no matching key is found in the dictionary.</param>
        /// <returns>
        /// The value for the key, or <paramref name="defaultValue"/> if no matching key was found.
        /// </returns>
        public static TValue GetValueOrDefault<TKey, TValue>(this IImmutableDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue) where TKey : notnull
        {
            Requires.NotNull(dictionary, nameof(dictionary));
            Requires.NotNullAllowStructs(key, nameof(key));

            TValue value;
            if (dictionary.TryGetValue(key, out value!))
            {
                return value;
            }

            return defaultValue;
        }
    }
}
