// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections.Immutable
{
    /// <summary>
    /// Factory methods for frozen collections.
    /// </summary>
    /// <remarks>
    /// Frozen collections are immutable and are optimized for situations where a collection
    /// is created infrequently, but used repeatedly at runtime. They have a relatively high
    /// cost to create, but provide excellent lookup performance. These are thus ideal for cases
    /// where a collection is created at startup of an application and used throughout the life
    /// of the application.
    /// </remarks>
    public static class Freezer
    {
        /// <summary>
        /// Initializes a new dictionary with the given set of key/value pairs.
        /// </summary>
        /// <param name="pairs">The pairs to initialize the dictionary with.</param>
        /// <param name="comparer">The comparer used to compare and hash keys. If this is null, then <see cref="EqualityComparer{T}.Default"/> is used.</param>
        /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <exception cref="ArgumentException">If more than 64K pairs are added.</exception>
        /// <remarks>
        /// Tf the same key appears multiple times in the input, the latter one in the sequence takes precedence.
        /// </remarks>
        /// <returns>A frozen dictionary.</returns>
        public static FrozenDictionary<TKey, TValue> ToFrozenDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>>? pairs, IEqualityComparer<TKey> comparer)
            where TKey : notnull
        {
            return new FrozenDictionary<TKey, TValue>(pairs.EmptyIfNull(), comparer);
        }

        /// <summary>
        /// Initializes a new dictionary with the given set of key/value pairs.
        /// </summary>
        /// <param name="pairs">The pairs to initialize the dictionary with.</param>
        /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <exception cref="ArgumentException">If more than 64K pairs are added.</exception>
        /// <remarks>
        /// Tf the same key appears multiple times in the input, the latter one in the sequence takes precedence.
        ///
        /// If your dictionary's keys are strings compared using ordinal string comparison, you should be using the
        /// <see cref="ToFrozenDictionary{TValue}(IEnumerable{KeyValuePair{string, TValue}}?, bool)"/> overload instead
        /// as it will give you a specialized dictionary which delivers higher performance for ordinal string keys.
        ///
        /// If your dictionary's keys are integers, you should be using the <see cref="ToFrozenDictionary{TValue}(IEnumerable{KeyValuePair{int, TValue}}?)"/>
        /// overload instead, also for higher performance.
        /// </remarks>
        /// <returns>A frozen dictionary.</returns>
        public static FrozenDictionary<TKey, TValue> ToFrozenDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>>? pairs)
            where TKey : notnull
        {
            return new FrozenDictionary<TKey, TValue>(pairs.EmptyIfNull(), EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Initializes a new dictionary with the given set of key/value pairs.
        /// </summary>
        /// <param name="pairs">The pairs to initialize the dictionary with.</param>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <exception cref="ArgumentException">If more than 64K pairs are added.</exception>
        /// <remarks>
        /// Tf the same key appears multiple times in the input, the latter one in the sequence takes precedence.
        /// </remarks>
        /// <returns>A frozen dictionary.</returns>
        public static FrozenIntDictionary<TValue> ToFrozenDictionary<TValue>(this IEnumerable<KeyValuePair<int, TValue>>? pairs)
        {
            return new FrozenIntDictionary<TValue>(pairs.EmptyIfNull());
        }

        /// <summary>
        /// Initializes a new dictionary with the given set of key/value pairs.
        /// </summary>
        /// <param name="pairs">The pairs to initialize the dictionary with.</param>
        /// <param name="ignoreCase">Whether to use case-insensitive semantics.</param>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <exception cref="ArgumentException">If more than 64K pairs are added.</exception>
        /// <remarks>
        /// Tf the same key appears multiple times in the input, the latter one in the sequence takes precedence.
        /// </remarks>
        /// <returns>A frozen dictionary.</returns>
        public static FrozenOrdinalStringDictionary<TValue> ToFrozenDictionary<TValue>(this IEnumerable<KeyValuePair<string, TValue>>? pairs, bool ignoreCase)
        {
            return new FrozenOrdinalStringDictionary<TValue>(pairs.EmptyIfNull(), ignoreCase);
        }

        /// <summary>
        /// Initializes a new dictionary with the given set of key/value pairs using case-sensitive ordinal string comparison.
        /// </summary>
        /// <param name="pairs">The pairs to initialize the dictionary with.</param>
        /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
        /// <exception cref="ArgumentException">If more than 64K pairs are added.</exception>
        /// <remarks>
        /// Tf the same key appears multiple times in the input, the latter one in the sequence takes precedence.
        /// </remarks>
        /// <returns>A frozen dictionary.</returns>
        public static FrozenOrdinalStringDictionary<TValue> ToFrozenDictionary<TValue>(this IEnumerable<KeyValuePair<string, TValue>>? pairs)
        {
            return new FrozenOrdinalStringDictionary<TValue>(pairs.EmptyIfNull(), false);
        }

        /// <summary>
        /// Initializes a new set with the given items.
        /// </summary>
        /// <param name="items">The items to initialize the set with.</param>
        /// <param name="comparer">The comparer used to compare and hash items.</param>
        /// <typeparam name="T">The type of the items in the set.</typeparam>
        /// <exception cref="ArgumentException">If more than 64K items are added.</exception>
        /// <returns>A frozen set.</returns>
        public static FrozenSet<T> ToFrozenSet<T>(this IEnumerable<T>? items, IEqualityComparer<T> comparer)
            where T : notnull
        {
            return new FrozenSet<T>(items.EmptyIfNull(), comparer);
        }

        /// <summary>
        /// Initializes a new set with the given items.
        /// </summary>
        /// <param name="items">The items to initialize the set with.</param>
        /// <typeparam name="T">The type of the items in the set.</typeparam>
        /// <exception cref="ArgumentException">If more than 64K items are added.</exception>
        /// <remarks>
        /// If your set's items are strings compared using ordinal string comparison, you should be using the
        /// <see cref="ToFrozenSet(IEnumerable{string}?, bool)"/> overload instead
        /// as it will give you a specialized set which delivers higher performance for ordinal string items.
        ///
        /// If your set's items are integers, you should be using the <see cref="ToFrozenSet(IEnumerable{int}?)" />
        /// overload instead, also for higher performance.
        /// </remarks>
        /// <returns>A frozen set.</returns>
        public static FrozenSet<T> ToFrozenSet<T>(this IEnumerable<T>? items)
            where T : notnull
        {
            return new FrozenSet<T>(items.EmptyIfNull(), EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Initializes a new set with the given items.
        /// </summary>
        /// <param name="items">The items to initialize the set with.</param>
        /// <exception cref="ArgumentException">If more than 64K items are added.</exception>
        /// <returns>A frozen set.</returns>
        public static FrozenIntSet ToFrozenSet(this IEnumerable<int>? items)
        {
            return new FrozenIntSet(items.EmptyIfNull());
        }

        /// <summary>
        /// Initializes a new set with the given items.
        /// </summary>
        /// <param name="items">The items to initialize the set with.</param>
        /// <param name="ignoreCase">Whether to use case-insensitive semantics.</param>
        /// <exception cref="ArgumentException">If more than 64K items are added.</exception>
        /// <returns>A frozen set.</returns>
        public static FrozenOrdinalStringSet ToFrozenSet(this IEnumerable<string>? items, bool ignoreCase)
        {
            return new FrozenOrdinalStringSet(items.EmptyIfNull(), ignoreCase);
        }

        /// <summary>
        /// Initializes a new set with the given items.
        /// </summary>
        /// <param name="items">The items to initialize the set with.</param>
        /// <exception cref="ArgumentException">If more than 64K items are added.</exception>
        /// <returns>A frozen set.</returns>
        public static FrozenOrdinalStringSet ToFrozenSet(this IEnumerable<string>? items)
        {
            return new FrozenOrdinalStringSet(items.EmptyIfNull());
        }

        /// <summary>
        /// Initializes a new list with the given items.
        /// </summary>
        /// <param name="items">The items to initialize the list with.</param>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <returns>A frozen list.</returns>
        public static FrozenList<T> ToFrozenList<T>(this IEnumerable<T>? items)
        {
            return new FrozenList<T>(items.EmptyIfNull());
        }

        private static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T>? items) => items ?? Array.Empty<T>();
    }
}
