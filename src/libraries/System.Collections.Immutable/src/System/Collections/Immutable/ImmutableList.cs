// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A set of initialization methods for instances of <see cref="ImmutableList{T}"/>.
    /// </summary>
    public static class ImmutableList
    {
        /// <summary>
        /// Returns an empty collection.
        /// </summary>
        /// <typeparam name="T">The type of items stored by the collection.</typeparam>
        /// <returns>The immutable collection.</returns>
        [Pure]
        public static ImmutableList<T> Create<T>() => ImmutableList<T>.Empty;

        /// <summary>
        /// Creates a new immutable collection prefilled with the specified item.
        /// </summary>
        /// <typeparam name="T">The type of items stored by the collection.</typeparam>
        /// <param name="item">The item to prepopulate.</param>
        /// <returns>The new immutable collection.</returns>
        [Pure]
        public static ImmutableList<T> Create<T>(T item) => ImmutableList<T>.Empty.Add(item);

        /// <summary>
        /// Creates a new immutable collection prefilled with the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items stored by the collection.</typeparam>
        /// <param name="items">The items to prepopulate.</param>
        /// <returns>The new immutable collection.</returns>
        [Pure]
        public static ImmutableList<T> CreateRange<T>(IEnumerable<T> items) => ImmutableList<T>.Empty.AddRange(items);

        /// <summary>
        /// Creates a new immutable collection prefilled with the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items stored by the collection.</typeparam>
        /// <param name="items">The items to prepopulate.</param>
        /// <returns>The new immutable collection.</returns>
        [Pure]
        public static ImmutableList<T> Create<T>(params T[] items) => ImmutableList<T>.Empty.AddRange(items);

        /// <summary>
        /// Creates a new immutable list builder.
        /// </summary>
        /// <typeparam name="T">The type of items stored by the collection.</typeparam>
        /// <returns>The immutable collection builder.</returns>
        [Pure]
        public static ImmutableList<T>.Builder CreateBuilder<T>() => Create<T>().ToBuilder();

        /// <summary>
        /// Enumerates a sequence exactly once and produces an immutable list of its contents.
        /// </summary>
        /// <typeparam name="TSource">The type of element in the sequence.</typeparam>
        /// <param name="source">The sequence to enumerate.</param>
        /// <returns>An immutable list.</returns>
        [Pure]
        public static ImmutableList<TSource> ToImmutableList<TSource>(this IEnumerable<TSource> source)
        {
            var existingList = source as ImmutableList<TSource>;
            if (existingList != null)
            {
                return existingList;
            }

            return ImmutableList<TSource>.Empty.AddRange(source);
        }

        /// <summary>
        /// Returns an immutable copy of the current contents of the builder's collection.
        /// </summary>
        /// <param name="builder">The builder to create the immutable list from.</param>
        /// <returns>An immutable list.</returns>
        [Pure]
        public static ImmutableList<TSource> ToImmutableList<TSource>(this ImmutableList<TSource>.Builder builder)
        {
            Requires.NotNull(builder, nameof(builder));

            return builder.ToImmutable();
        }

        /// <summary>
        /// Replaces the first equal element in the list with the specified element.
        /// </summary>
        /// <param name="list">The list to search.</param>
        /// <param name="oldValue">The element to replace.</param>
        /// <param name="newValue">The element to replace the old element with.</param>
        /// <returns>The new list -- even if the value being replaced is equal to the new value for that position.</returns>
        /// <exception cref="ArgumentException">Thrown when the old value does not exist in the list.</exception>
        [Pure]
        public static IImmutableList<T> Replace<T>(this IImmutableList<T> list, T oldValue, T newValue)
        {
            Requires.NotNull(list, nameof(list));
            return list.Replace(oldValue, newValue, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Removes the specified value from this list.
        /// </summary>
        /// <param name="list">The list to search.</param>
        /// <param name="value">The value to remove.</param>
        /// <returns>A new list with the element removed, or this list if the element is not in this list.</returns>
        [Pure]
        public static IImmutableList<T> Remove<T>(this IImmutableList<T> list, T value)
        {
            Requires.NotNull(list, nameof(list));
            return list.Remove(value, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Removes the specified values from this list.
        /// </summary>
        /// <param name="list">The list to search.</param>
        /// <param name="items">The items to remove if matches are found in this list.</param>
        /// <returns>
        /// A new list with the elements removed.
        /// </returns>
        [Pure]
        public static IImmutableList<T> RemoveRange<T>(this IImmutableList<T> list, IEnumerable<T> items)
        {
            Requires.NotNull(list, nameof(list));
            return list.RemoveRange(items, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the
        /// first occurrence within the <see cref="IImmutableList{T}"/>.
        /// </summary>
        /// <param name="list">The list to search.</param>
        /// <param name="item">
        /// The object to locate in the <see cref="IImmutableList{T}"/>. The value
        /// can be null for reference types.
        /// </param>
        /// <returns>
        /// The zero-based index of the first occurrence of item within the range of
        /// elements in the <see cref="IImmutableList{T}"/> that extends from index
        /// to the last element, if found; otherwise, -1.
        /// </returns>
        [Pure]
        public static int IndexOf<T>(this IImmutableList<T> list, T item)
        {
            Requires.NotNull(list, nameof(list));
            return list.IndexOf(item, 0, list.Count, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the
        /// first occurrence within the <see cref="IImmutableList{T}"/>.
        /// </summary>
        /// <param name="list">The list to search.</param>
        /// <param name="item">
        /// The object to locate in the <see cref="IImmutableList{T}"/>. The value
        /// can be null for reference types.
        /// </param>
        /// <param name="equalityComparer">The equality comparer to use in the search.</param>
        /// <returns>
        /// The zero-based index of the first occurrence of item within the range of
        /// elements in the <see cref="IImmutableList{T}"/> that extends from index
        /// to the last element, if found; otherwise, -1.
        /// </returns>
        [Pure]
        public static int IndexOf<T>(this IImmutableList<T> list, T item, IEqualityComparer<T> equalityComparer)
        {
            Requires.NotNull(list, nameof(list));
            return list.IndexOf(item, 0, list.Count, equalityComparer);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the
        /// first occurrence within the range of elements in the <see cref="IImmutableList{T}"/>
        /// that extends from the specified index to the last element.
        /// </summary>
        /// <param name="list">The list to search.</param>
        /// <param name="item">
        /// The object to locate in the <see cref="IImmutableList{T}"/>. The value
        /// can be null for reference types.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based starting index of the search. 0 (zero) is valid in an empty
        /// list.
        /// </param>
        /// <returns>
        /// The zero-based index of the first occurrence of item within the range of
        /// elements in the <see cref="IImmutableList{T}"/> that extends from index
        /// to the last element, if found; otherwise, -1.
        /// </returns>
        [Pure]
        public static int IndexOf<T>(this IImmutableList<T> list, T item, int startIndex)
        {
            Requires.NotNull(list, nameof(list));
            return list.IndexOf(item, startIndex, list.Count - startIndex, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the
        /// first occurrence within the range of elements in the <see cref="IImmutableList{T}"/>
        /// that extends from the specified index to the last element.
        /// </summary>
        /// <param name="list">The list to search.</param>
        /// <param name="item">
        /// The object to locate in the <see cref="IImmutableList{T}"/>. The value
        /// can be null for reference types.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based starting index of the search. 0 (zero) is valid in an empty
        /// list.
        /// </param>
        /// <param name="count">
        /// The number of elements in the section to search.
        /// </param>
        /// <returns>
        /// The zero-based index of the first occurrence of item within the range of
        /// elements in the <see cref="IImmutableList{T}"/> that extends from index
        /// to the last element, if found; otherwise, -1.
        /// </returns>
        [Pure]
        public static int IndexOf<T>(this IImmutableList<T> list, T item, int startIndex, int count)
        {
            Requires.NotNull(list, nameof(list));
            return list.IndexOf(item, startIndex, count, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the
        /// last occurrence within the entire <see cref="IImmutableList{T}"/>.
        /// </summary>
        /// <param name="list">The list to search.</param>
        /// <param name="item">
        /// The object to locate in the <see cref="IImmutableList{T}"/>. The value
        /// can be null for reference types.
        /// </param>
        /// <returns>
        /// The zero-based index of the last occurrence of item within the entire the
        /// <see cref="IImmutableList{T}"/>, if found; otherwise, -1.
        /// </returns>
        [Pure]
        public static int LastIndexOf<T>(this IImmutableList<T> list, T item)
        {
            Requires.NotNull(list, nameof(list));

            if (list.Count == 0)
            {
                // Avoid argument out of range exceptions.
                return -1;
            }

            return list.LastIndexOf(item, list.Count - 1, list.Count, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the
        /// last occurrence within the entire <see cref="IImmutableList{T}"/>.
        /// </summary>
        /// <param name="list">The list to search.</param>
        /// <param name="item">
        /// The object to locate in the <see cref="IImmutableList{T}"/>. The value
        /// can be null for reference types.
        /// </param>
        /// <param name="equalityComparer">The equality comparer to use in the search.</param>
        /// <returns>
        /// The zero-based index of the last occurrence of item within the entire the
        /// <see cref="IImmutableList{T}"/>, if found; otherwise, -1.
        /// </returns>
        [Pure]
        public static int LastIndexOf<T>(this IImmutableList<T> list, T item, IEqualityComparer<T> equalityComparer)
        {
            Requires.NotNull(list, nameof(list));

            if (list.Count == 0)
            {
                // Avoid argument out of range exceptions.
                return -1;
            }

            return list.LastIndexOf(item, list.Count - 1, list.Count, equalityComparer);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the
        /// last occurrence within the range of elements in the <see cref="IImmutableList{T}"/>
        /// that extends from the first element to the specified index.
        /// </summary>
        /// <param name="list">The list to search.</param>
        /// <param name="item">
        /// The object to locate in the <see cref="IImmutableList{T}"/>. The value
        /// can be null for reference types.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based starting index of the backward search.
        /// </param>
        /// <returns>
        /// The zero-based index of the last occurrence of item within the range of elements
        /// in the <see cref="IImmutableList{T}"/> that extends from the first element
        /// to index, if found; otherwise, -1.
        /// </returns>
        [Pure]
        public static int LastIndexOf<T>(this IImmutableList<T> list, T item, int startIndex)
        {
            Requires.NotNull(list, nameof(list));

            if (list.Count == 0 && startIndex == 0)
            {
                return -1;
            }

            return list.LastIndexOf(item, startIndex, startIndex + 1, EqualityComparer<T>.Default);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the
        /// last occurrence within the range of elements in the <see cref="IImmutableList{T}"/>
        /// that extends from the first element to the specified index.
        /// </summary>
        /// <param name="list">The list to search.</param>
        /// <param name="item">
        /// The object to locate in the <see cref="IImmutableList{T}"/>. The value
        /// can be null for reference types.
        /// </param>
        /// <param name="startIndex">
        /// The zero-based starting index of the backward search.
        /// </param>
        /// <param name="count">
        /// The number of elements in the section to search.
        /// </param>
        /// <returns>
        /// The zero-based index of the last occurrence of item within the range of elements
        /// in the <see cref="IImmutableList{T}"/> that extends from the first element
        /// to index, if found; otherwise, -1.
        /// </returns>
        [Pure]
        public static int LastIndexOf<T>(this IImmutableList<T> list, T item, int startIndex, int count)
        {
            Requires.NotNull(list, nameof(list));
            return list.LastIndexOf(item, startIndex, count, EqualityComparer<T>.Default);
        }
    }
}
