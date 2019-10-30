// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A set of initialization methods for instances of <see cref="ImmutableHashSet{T}"/>.
    /// </summary>
    public static class ImmutableHashSet
    {
        /// <summary>
        /// Returns an empty collection.
        /// </summary>
        /// <typeparam name="T">The type of items stored by the collection.</typeparam>
        /// <returns>The immutable collection.</returns>
        [Pure]
        public static ImmutableHashSet<T> Create<T>()
        {
            return ImmutableHashSet<T>.Empty;
        }

        /// <summary>
        /// Returns an empty collection.
        /// </summary>
        /// <typeparam name="T">The type of items stored by the collection.</typeparam>
        /// <param name="equalityComparer">The equality comparer.</param>
        /// <returns>
        /// The immutable collection.
        /// </returns>
        [Pure]
        public static ImmutableHashSet<T> Create<T>(IEqualityComparer<T> equalityComparer)
        {
            return ImmutableHashSet<T>.Empty.WithComparer(equalityComparer);
        }

        /// <summary>
        /// Creates a new immutable collection prefilled with the specified item.
        /// </summary>
        /// <typeparam name="T">The type of items stored by the collection.</typeparam>
        /// <param name="item">The item to prepopulate.</param>
        /// <returns>The new immutable collection.</returns>
        [Pure]
        public static ImmutableHashSet<T> Create<T>(T item)
        {
            return ImmutableHashSet<T>.Empty.Add(item);
        }

        /// <summary>
        /// Creates a new immutable collection prefilled with the specified item.
        /// </summary>
        /// <typeparam name="T">The type of items stored by the collection.</typeparam>
        /// <param name="equalityComparer">The equality comparer.</param>
        /// <param name="item">The item to prepopulate.</param>
        /// <returns>The new immutable collection.</returns>
        [Pure]
        public static ImmutableHashSet<T> Create<T>(IEqualityComparer<T> equalityComparer, T item)
        {
            return ImmutableHashSet<T>.Empty.WithComparer(equalityComparer).Add(item);
        }

        /// <summary>
        /// Creates a new immutable collection prefilled with the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items stored by the collection.</typeparam>
        /// <param name="items">The items to prepopulate.</param>
        /// <returns>The new immutable collection.</returns>
        [Pure]
        public static ImmutableHashSet<T> CreateRange<T>(IEnumerable<T> items)
        {
            return ImmutableHashSet<T>.Empty.Union(items);
        }

        /// <summary>
        /// Creates a new immutable collection prefilled with the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items stored by the collection.</typeparam>
        /// <param name="equalityComparer">The equality comparer.</param>
        /// <param name="items">The items to prepopulate.</param>
        /// <returns>The new immutable collection.</returns>
        [Pure]
        public static ImmutableHashSet<T> CreateRange<T>(IEqualityComparer<T> equalityComparer, IEnumerable<T> items)
        {
            return ImmutableHashSet<T>.Empty.WithComparer(equalityComparer).Union(items);
        }

        /// <summary>
        /// Creates a new immutable collection prefilled with the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items stored by the collection.</typeparam>
        /// <param name="items">The items to prepopulate.</param>
        /// <returns>The new immutable collection.</returns>
        [Pure]
        public static ImmutableHashSet<T> Create<T>(params T[] items)
        {
            return ImmutableHashSet<T>.Empty.Union(items);
        }

        /// <summary>
        /// Creates a new immutable collection prefilled with the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items stored by the collection.</typeparam>
        /// <param name="equalityComparer">The equality comparer.</param>
        /// <param name="items">The items to prepopulate.</param>
        /// <returns>The new immutable collection.</returns>
        [Pure]
        public static ImmutableHashSet<T> Create<T>(IEqualityComparer<T> equalityComparer, params T[] items)
        {
            return ImmutableHashSet<T>.Empty.WithComparer(equalityComparer).Union(items);
        }

        /// <summary>
        /// Creates a new immutable hash set builder.
        /// </summary>
        /// <typeparam name="T">The type of items stored by the collection.</typeparam>
        /// <returns>The immutable collection.</returns>
        [Pure]
        public static ImmutableHashSet<T>.Builder CreateBuilder<T>()
        {
            return Create<T>().ToBuilder();
        }

        /// <summary>
        /// Creates a new immutable hash set builder.
        /// </summary>
        /// <typeparam name="T">The type of items stored by the collection.</typeparam>
        /// <param name="equalityComparer">The equality comparer.</param>
        /// <returns>
        /// The immutable collection.
        /// </returns>
        [Pure]
        public static ImmutableHashSet<T>.Builder CreateBuilder<T>(IEqualityComparer<T> equalityComparer)
        {
            return Create<T>(equalityComparer).ToBuilder();
        }

        /// <summary>
        /// Enumerates a sequence exactly once and produces an immutable set of its contents.
        /// </summary>
        /// <typeparam name="TSource">The type of element in the sequence.</typeparam>
        /// <param name="source">The sequence to enumerate.</param>
        /// <param name="equalityComparer">The equality comparer to use for initializing and adding members to the hash set.</param>
        /// <returns>An immutable set.</returns>
        [Pure]
        public static ImmutableHashSet<TSource> ToImmutableHashSet<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource> equalityComparer)
        {
            var existingSet = source as ImmutableHashSet<TSource>;
            if (existingSet != null)
            {
                return existingSet.WithComparer(equalityComparer);
            }

            return ImmutableHashSet<TSource>.Empty.WithComparer(equalityComparer).Union(source);
        }

        /// <summary>
        /// Returns an immutable copy of the current contents of the builder's collection.
        /// </summary>
        /// <param name="builder">The builder to create the immutable set from.</param>
        /// <returns>An immutable set.</returns>
        [Pure]
        public static ImmutableHashSet<TSource> ToImmutableHashSet<TSource>(this ImmutableHashSet<TSource>.Builder builder)
        {
            Requires.NotNull(builder, nameof(builder));

            return builder.ToImmutable();
        }


        /// <summary>
        /// Enumerates a sequence exactly once and produces an immutable set of its contents.
        /// </summary>
        /// <typeparam name="TSource">The type of element in the sequence.</typeparam>
        /// <param name="source">The sequence to enumerate.</param>
        /// <returns>An immutable set.</returns>
        [Pure]
        public static ImmutableHashSet<TSource> ToImmutableHashSet<TSource>(this IEnumerable<TSource> source)
        {
            return ToImmutableHashSet(source, null);
        }
    }
}
