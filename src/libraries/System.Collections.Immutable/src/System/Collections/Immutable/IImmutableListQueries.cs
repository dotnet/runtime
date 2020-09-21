// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Immutable
{
    /// <summary>
    /// An interface that describes the methods that the <see cref="ImmutableList{T}"/> and <see cref="ImmutableList{T}.Builder"/> types have in common.
    /// </summary>
    /// <typeparam name="T">The type of element in the collection.</typeparam>
    internal interface IImmutableListQueries<T> : IReadOnlyList<T>
    {
        /// <summary>
        /// Converts the elements in the current <see cref="ImmutableList{T}"/> to
        /// another type, and returns a list containing the converted elements.
        /// </summary>
        /// <param name="converter">
        /// A <see cref="Func{T, TResult}"/> delegate that converts each element from
        /// one type to another type.
        /// </param>
        /// <typeparam name="TOutput">
        /// The type of the elements of the target array.
        /// </typeparam>
        /// <returns>
        /// A <see cref="ImmutableList{T}"/> of the target type containing the converted
        /// elements from the current <see cref="ImmutableList{T}"/>.
        /// </returns>
        ImmutableList<TOutput> ConvertAll<TOutput>(Func<T, TOutput> converter);

        /// <summary>
        /// Performs the specified action on each element of the list.
        /// </summary>
        /// <param name="action">The <see cref="Action{T}"/> delegate to perform on each element of the list.</param>
        void ForEach(Action<T> action);

        /// <summary>
        /// Creates a shallow copy of a range of elements in the source <see cref="ImmutableList{T}"/>.
        /// </summary>
        /// <param name="index">
        /// The zero-based <see cref="ImmutableList{T}"/> index at which the range
        /// starts.
        /// </param>
        /// <param name="count">
        /// The number of elements in the range.
        /// </param>
        /// <returns>
        /// A shallow copy of a range of elements in the source <see cref="ImmutableList{T}"/>.
        /// </returns>
        ImmutableList<T> GetRange(int index, int count);

        /// <summary>
        /// Copies the entire <see cref="ImmutableList{T}"/> to a compatible one-dimensional
        /// array, starting at the beginning of the target array.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional <see cref="Array"/> that is the destination of the elements
        /// copied from <see cref="ImmutableList{T}"/>. The <see cref="Array"/> must have
        /// zero-based indexing.
        /// </param>
        void CopyTo(T[] array);

        /// <summary>
        /// Copies the entire <see cref="ImmutableList{T}"/> to a compatible one-dimensional
        /// array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional <see cref="Array"/> that is the destination of the elements
        /// copied from <see cref="ImmutableList{T}"/>. The <see cref="Array"/> must have
        /// zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">
        /// The zero-based index in <paramref name="array"/> at which copying begins.
        /// </param>
        void CopyTo(T[] array, int arrayIndex);

        /// <summary>
        /// Copies a range of elements from the <see cref="ImmutableList{T}"/> to
        /// a compatible one-dimensional array, starting at the specified index of the
        /// target array.
        /// </summary>
        /// <param name="index">
        /// The zero-based index in the source <see cref="ImmutableList{T}"/> at
        /// which copying begins.
        /// </param>
        /// <param name="array">
        /// The one-dimensional <see cref="Array"/> that is the destination of the elements
        /// copied from <see cref="ImmutableList{T}"/>. The <see cref="Array"/> must have
        /// zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <param name="count">The number of elements to copy.</param>
        void CopyTo(int index, T[] array, int arrayIndex, int count);

        /// <summary>
        /// Determines whether the <see cref="ImmutableList{T}"/> contains elements
        /// that match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">
        /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the elements
        /// to search for.
        /// </param>
        /// <returns>
        /// true if the <see cref="ImmutableList{T}"/> contains one or more elements
        /// that match the conditions defined by the specified predicate; otherwise,
        /// false.
        /// </returns>
        bool Exists(Predicate<T> match);

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the first occurrence within the entire <see cref="ImmutableList{T}"/>.
        /// </summary>
        /// <param name="match">
        /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
        /// to search for.
        /// </param>
        /// <returns>
        /// The first element that matches the conditions defined by the specified predicate,
        /// if found; otherwise, the default value for type <typeparamref name="T"/>.
        /// </returns>
        T? Find(Predicate<T> match);

        /// <summary>
        /// Retrieves all the elements that match the conditions defined by the specified
        /// predicate.
        /// </summary>
        /// <param name="match">
        /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the elements
        /// to search for.
        /// </param>
        /// <returns>
        /// A <see cref="ImmutableList{T}"/> containing all the elements that match
        /// the conditions defined by the specified predicate, if found; otherwise, an
        /// empty <see cref="ImmutableList{T}"/>.
        /// </returns>
        ImmutableList<T> FindAll(Predicate<T> match);

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the first occurrence within
        /// the entire <see cref="ImmutableList{T}"/>.
        /// </summary>
        /// <param name="match">
        /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
        /// to search for.
        /// </param>
        /// <returns>
        /// The zero-based index of the first occurrence of an element that matches the
        /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
        /// </returns>
        int FindIndex(Predicate<T> match);

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the first occurrence within
        /// the range of elements in the <see cref="ImmutableList{T}"/> that extends
        /// from the specified index to the last element.
        /// </summary>
        /// <param name="startIndex">The zero-based starting index of the search.</param>
        /// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the element to search for.</param>
        /// <returns>
        /// The zero-based index of the first occurrence of an element that matches the
        /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
        /// </returns>
        int FindIndex(int startIndex, Predicate<T> match);

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the first occurrence within
        /// the range of elements in the <see cref="ImmutableList{T}"/> that starts
        /// at the specified index and contains the specified number of elements.
        /// </summary>
        /// <param name="startIndex">The zero-based starting index of the search.</param>
        /// <param name="count">The number of elements in the section to search.</param>
        /// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the element to search for.</param>
        /// <returns>
        /// The zero-based index of the first occurrence of an element that matches the
        /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
        /// </returns>
        int FindIndex(int startIndex, int count, Predicate<T> match);

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the last occurrence within the entire <see cref="ImmutableList{T}"/>.
        /// </summary>
        /// <param name="match">
        /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
        /// to search for.
        /// </param>
        /// <returns>
        /// The last element that matches the conditions defined by the specified predicate,
        /// if found; otherwise, the default value for type <typeparamref name="T"/>.
        /// </returns>
        T? FindLast(Predicate<T> match);

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the last occurrence within
        /// the entire <see cref="ImmutableList{T}"/>.
        /// </summary>
        /// <param name="match">
        /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
        /// to search for.
        /// </param>
        /// <returns>
        /// The zero-based index of the last occurrence of an element that matches the
        /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
        /// </returns>
        int FindLastIndex(Predicate<T> match);

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the last occurrence within
        /// the range of elements in the <see cref="ImmutableList{T}"/> that extends
        /// from the first element to the specified index.
        /// </summary>
        /// <param name="startIndex">The zero-based starting index of the backward search.</param>
        /// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
        /// to search for.</param>
        /// <returns>
        /// The zero-based index of the last occurrence of an element that matches the
        /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
        /// </returns>
        int FindLastIndex(int startIndex, Predicate<T> match);

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the last occurrence within
        /// the range of elements in the <see cref="ImmutableList{T}"/> that contains
        /// the specified number of elements and ends at the specified index.
        /// </summary>
        /// <param name="startIndex">The zero-based starting index of the backward search.</param>
        /// <param name="count">The number of elements in the section to search.</param>
        /// <param name="match">
        /// The <see cref="Predicate{T}"/> delegate that defines the conditions of the element
        /// to search for.
        /// </param>
        /// <returns>
        /// The zero-based index of the last occurrence of an element that matches the
        /// conditions defined by <paramref name="match"/>, if found; otherwise, -1.
        /// </returns>
        int FindLastIndex(int startIndex, int count, Predicate<T> match);

        /// <summary>
        /// Determines whether every element in the <see cref="ImmutableList{T}"/>
        /// matches the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match">
        /// The <see cref="Predicate{T}"/> delegate that defines the conditions to check against
        /// the elements.
        /// </param>
        /// <returns>
        /// true if every element in the <see cref="ImmutableList{T}"/> matches the
        /// conditions defined by the specified predicate; otherwise, false. If the list
        /// has no elements, the return value is true.
        /// </returns>
        bool TrueForAll(Predicate<T> match);

        /// <summary>
        /// Searches the entire sorted <see cref="IReadOnlyList{T}"/> for an element
        /// using the default comparer and returns the zero-based index of the element.
        /// </summary>
        /// <param name="item">The object to locate. The value can be null for reference types.</param>
        /// <returns>
        /// The zero-based index of <paramref name="item"/> in the sorted <see cref="IReadOnlyList{T}"/>,
        /// if <paramref name="item"/> is found; otherwise, a negative number that is the bitwise complement
        /// of the index of the next element that is larger than <paramref name="item"/> or, if there is
        /// no larger element, the bitwise complement of <see cref="IReadOnlyCollection{T}.Count"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The default comparer <see cref="Comparer{T}.Default"/> cannot
        /// find an implementation of the <see cref="IComparable{T}"/> generic interface or
        /// the <see cref="IComparable"/> interface for type <typeparamref name="T"/>.
        /// </exception>
        int BinarySearch(T item);

        /// <summary>
        ///  Searches the entire sorted <see cref="IReadOnlyList{T}"/> for an element
        ///  using the specified comparer and returns the zero-based index of the element.
        /// </summary>
        /// <param name="item">The object to locate. The value can be null for reference types.</param>
        /// <param name="comparer">
        /// The <see cref="IComparer{T}"/> implementation to use when comparing
        /// elements.-or-null to use the default comparer <see cref="Comparer{T}.Default"/>.
        /// </param>
        /// <returns>
        /// The zero-based index of <paramref name="item"/> in the sorted <see cref="IReadOnlyList{T}"/>,
        /// if <paramref name="item"/> is found; otherwise, a negative number that is the bitwise complement
        /// of the index of the next element that is larger than <paramref name="item"/> or, if there is
        /// no larger element, the bitwise complement of <see cref="IReadOnlyCollection{T}.Count"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="comparer"/> is null, and the default comparer <see cref="Comparer{T}.Default"/>
        /// cannot find an implementation of the <see cref="IComparable{T}"/> generic interface
        /// or the <see cref="IComparable"/> interface for type <typeparamref name="T"/>.
        /// </exception>
        int BinarySearch(T item, IComparer<T>? comparer);

        /// <summary>
        /// Searches a range of elements in the sorted <see cref="IReadOnlyList{T}"/>
        /// for an element using the specified comparer and returns the zero-based index
        /// of the element.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range to search.</param>
        /// <param name="count"> The length of the range to search.</param>
        /// <param name="item">The object to locate. The value can be null for reference types.</param>
        /// <param name="comparer">
        /// The <see cref="IComparer{T}"/> implementation to use when comparing
        /// elements, or null to use the default comparer <see cref="Comparer{T}.Default"/>.
        /// </param>
        /// <returns>
        /// The zero-based index of <paramref name="item"/> in the sorted <see cref="IReadOnlyList{T}"/>,
        /// if <paramref name="item"/> is found; otherwise, a negative number that is the bitwise complement
        /// of the index of the next element that is larger than <paramref name="item"/> or, if there is
        /// no larger element, the bitwise complement of <see cref="IReadOnlyCollection{T}.Count"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> is less than 0.-or-<paramref name="count"/> is less than 0.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="index"/> and <paramref name="count"/> do not denote a valid range in the <see cref="IReadOnlyList{T}"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="comparer"/> is null, and the default comparer <see cref="Comparer{T}.Default"/>
        /// cannot find an implementation of the <see cref="IComparable{T}"/> generic interface
        /// or the <see cref="IComparable"/> interface for type <typeparamref name="T"/>.
        /// </exception>
        int BinarySearch(int index, int count, T item, IComparer<T>? comparer);
    }
}
