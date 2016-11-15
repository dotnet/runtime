// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A set of initialization methods for instances of <see cref="ImmutableArray{T}"/>.
    /// </summary>
    public static class ImmutableArray
    {
        /// <summary>
        /// A two element array useful for throwing exceptions the way LINQ does.
        /// </summary>
        internal static readonly byte[] TwoElementArray = new byte[2];

        /// <summary>
        /// Creates an empty <see cref="ImmutableArray{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of element stored in the array.</typeparam>
        /// <returns>An empty array.</returns>
        [Pure]
        public static ImmutableArray<T> Create<T>()
        {
            return ImmutableArray<T>.Empty;
        }

        /// <summary>
        /// Creates an <see cref="ImmutableArray{T}"/> with the specified element as its only member.
        /// </summary>
        /// <typeparam name="T">The type of element stored in the array.</typeparam>
        /// <param name="item">The element to store in the array.</param>
        /// <returns>A 1-element array.</returns>
        [Pure]
        public static ImmutableArray<T> Create<T>(T item)
        {
            T[] array = new[] { item };
            return new ImmutableArray<T>(array);
        }

        /// <summary>
        /// Creates an <see cref="ImmutableArray{T}"/> with the specified elements.
        /// </summary>
        /// <typeparam name="T">The type of element stored in the array.</typeparam>
        /// <param name="item1">The first element to store in the array.</param>
        /// <param name="item2">The second element to store in the array.</param>
        /// <returns>A 2-element array.</returns>
        [Pure]
        public static ImmutableArray<T> Create<T>(T item1, T item2)
        {
            T[] array = new[] { item1, item2 };
            return new ImmutableArray<T>(array);
        }

        /// <summary>
        /// Creates an <see cref="ImmutableArray{T}"/> with the specified elements.
        /// </summary>
        /// <typeparam name="T">The type of element stored in the array.</typeparam>
        /// <param name="item1">The first element to store in the array.</param>
        /// <param name="item2">The second element to store in the array.</param>
        /// <param name="item3">The third element to store in the array.</param>
        /// <returns>A 3-element array.</returns>
        [Pure]
        public static ImmutableArray<T> Create<T>(T item1, T item2, T item3)
        {
            T[] array = new[] { item1, item2, item3 };
            return new ImmutableArray<T>(array);
        }

        /// <summary>
        /// Creates an <see cref="ImmutableArray{T}"/> with the specified elements.
        /// </summary>
        /// <typeparam name="T">The type of element stored in the array.</typeparam>
        /// <param name="item1">The first element to store in the array.</param>
        /// <param name="item2">The second element to store in the array.</param>
        /// <param name="item3">The third element to store in the array.</param>
        /// <param name="item4">The fourth element to store in the array.</param>
        /// <returns>A 4-element array.</returns>
        [Pure]
        public static ImmutableArray<T> Create<T>(T item1, T item2, T item3, T item4)
        {
            T[] array = new[] { item1, item2, item3, item4 };
            return new ImmutableArray<T>(array);
        }

        /// <summary>
        /// Creates an <see cref="ImmutableArray{T}"/> populated with the contents of the specified sequence.
        /// </summary>
        /// <typeparam name="T">The type of element stored in the array.</typeparam>
        /// <param name="items">The elements to store in the array.</param>
        /// <returns>An immutable array.</returns>
        [Pure]
        public static ImmutableArray<T> CreateRange<T>(IEnumerable<T> items)
        {
            Requires.NotNull(items, nameof(items));

            // As an optimization, if the provided enumerable is actually a
            // boxed ImmutableArray<T> instance, reuse the underlying array if possible.
            // Note that this allows for automatic upcasting and downcasting of arrays
            // where the CLR allows it.
            var immutableArray = items as IImmutableArray;
            if (immutableArray != null)
            {
                immutableArray.ThrowInvalidOperationIfNotInitialized();

                // immutableArray.Array must not be null at this point, and we know it's an
                // ImmutableArray<T> or ImmutableArray<SomethingDerivedFromT> as they are
                // the only types that could be both IEnumerable<T> and IImmutableArray.
                // As such, we know that items is either an ImmutableArray<T> or
                // ImmutableArray<TypeDerivedFromT>, and we can cast the array to T[].
                return new ImmutableArray<T>((T[])immutableArray.Array);
            }

            // We don't recognize the source as an array that is safe to use.
            // So clone the sequence into an array and return an immutable wrapper.
            int count;
            if (items.TryGetCount(out count))
            {
                // We know how long the sequence is. Linq's built-in ToArray extension method
                // isn't as comprehensive in finding the length as we are, so call our own method
                // to avoid reallocating arrays as the sequence is enumerated.
                return new ImmutableArray<T>(items.ToArray(count));
            }
            else
            {
                return new ImmutableArray<T>(items.ToArray());
            }
        }

        /// <summary>
        /// Creates an empty <see cref="ImmutableArray{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of element stored in the array.</typeparam>
        /// <param name="items">The elements to store in the array.</param>
        /// <returns>An immutable array.</returns>
        [Pure]
        public static ImmutableArray<T> Create<T>(params T[] items)
        {
            if (items == null)
            {
                return Create<T>();
            }

            // We can't trust that the array passed in will never be mutated by the caller.
            // The caller may have passed in an array explicitly (not relying on compiler params keyword)
            // and could then change the array after the call, thereby violating the immutable
            // guarantee provided by this struct. So we always copy the array to ensure it won't ever change.
            return CreateDefensiveCopy(items);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableArray{T}"/> struct.
        /// </summary>
        /// <param name="items">The array to initialize the array with. A defensive copy is made.</param>
        /// <param name="start">The index of the first element in the source array to include in the resulting array.</param>
        /// <param name="length">The number of elements from the source array to include in the resulting array.</param>
        /// <remarks>
        /// This overload allows helper methods or custom builder classes to efficiently avoid paying a redundant
        /// tax for copying an array when the new array is a segment of an existing array.
        /// </remarks>
        [Pure]
        public static ImmutableArray<T> Create<T>(T[] items, int start, int length)
        {
            Requires.NotNull(items, nameof(items));
            Requires.Range(start >= 0 && start <= items.Length, nameof(start));
            Requires.Range(length >= 0 && start + length <= items.Length, nameof(length));

            if (length == 0)
            {
                // Avoid allocating an array.
                return Create<T>();
            }

            var array = new T[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = items[start + i];
            }

            return new ImmutableArray<T>(array);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableArray{T}"/> struct.
        /// </summary>
        /// <param name="items">The array to initialize the array with.
        /// The selected array segment may be copied into a new array.</param>
        /// <param name="start">The index of the first element in the source array to include in the resulting array.</param>
        /// <param name="length">The number of elements from the source array to include in the resulting array.</param>
        /// <remarks>
        /// This overload allows helper methods or custom builder classes to efficiently avoid paying a redundant
        /// tax for copying an array when the new array is a segment of an existing array.
        /// </remarks>
        [Pure]
        public static ImmutableArray<T> Create<T>(ImmutableArray<T> items, int start, int length)
        {
            Requires.Range(start >= 0 && start <= items.Length, nameof(start));
            Requires.Range(length >= 0 && start + length <= items.Length, nameof(length));

            if (length == 0)
            {
                return Create<T>();
            }

            if (start == 0 && length == items.Length)
            {
                return items;
            }

            var array = new T[length];
            Array.Copy(items.array, start, array, 0, length);
            return new ImmutableArray<T>(array);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableArray{T}"/> struct.
        /// </summary>
        /// <param name="items">The source array to initialize the resulting array with.</param>
        /// <param name="selector">The function to apply to each element from the source array.</param>
        /// <remarks>
        /// This overload allows efficient creation of an <see cref="ImmutableArray{T}"/> based on an existing
        /// <see cref="ImmutableArray{T}"/>, where a mapping function needs to be applied to each element from
        /// the source array.
        /// </remarks>
        [Pure]
        public static ImmutableArray<TResult> CreateRange<TSource, TResult>(ImmutableArray<TSource> items, Func<TSource, TResult> selector)
        {
            Requires.NotNull(selector, nameof(selector));

            int length = items.Length;

            if (length == 0)
            {
                return Create<TResult>();
            }

            var array = new TResult[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = selector(items[i]);
            }

            return new ImmutableArray<TResult>(array);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableArray{T}"/> struct.
        /// </summary>
        /// <param name="items">The source array to initialize the resulting array with.</param>
        /// <param name="start">The index of the first element in the source array to include in the resulting array.</param>
        /// <param name="length">The number of elements from the source array to include in the resulting array.</param>
        /// <param name="selector">The function to apply to each element from the source array included in the resulting array.</param>
        /// <remarks>
        /// This overload allows efficient creation of an <see cref="ImmutableArray{T}"/> based on a slice of an existing
        /// <see cref="ImmutableArray{T}"/>, where a mapping function needs to be applied to each element from the source array
        /// included in the resulting array.
        /// </remarks>
        [Pure]
        public static ImmutableArray<TResult> CreateRange<TSource, TResult>(ImmutableArray<TSource> items, int start, int length, Func<TSource, TResult> selector)
        {
            int itemsLength = items.Length;

            Requires.Range(start >= 0 && start <= itemsLength, nameof(start));
            Requires.Range(length >= 0 && start + length <= itemsLength, nameof(length));
            Requires.NotNull(selector, nameof(selector));

            if (length == 0)
            {
                return Create<TResult>();
            }

            var array = new TResult[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = selector(items[i + start]);
            }

            return new ImmutableArray<TResult>(array);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableArray{T}"/> struct.
        /// </summary>
        /// <param name="items">The source array to initialize the resulting array with.</param>
        /// <param name="selector">The function to apply to each element from the source array.</param>
        /// <param name="arg">An argument to be passed to the selector mapping function.</param>
        /// <remarks>
        /// This overload allows efficient creation of an <see cref="ImmutableArray{T}"/> based on an existing
        /// <see cref="ImmutableArray{T}"/>, where a mapping function needs to be applied to each element from
        /// the source array.
        /// </remarks>
        [Pure]
        public static ImmutableArray<TResult> CreateRange<TSource, TArg, TResult>(ImmutableArray<TSource> items, Func<TSource, TArg, TResult> selector, TArg arg)
        {
            Requires.NotNull(selector, nameof(selector));

            int length = items.Length;

            if (length == 0)
            {
                return Create<TResult>();
            }

            var array = new TResult[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = selector(items[i], arg);
            }

            return new ImmutableArray<TResult>(array);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableArray{T}"/> struct.
        /// </summary>
        /// <param name="items">The source array to initialize the resulting array with.</param>
        /// <param name="start">The index of the first element in the source array to include in the resulting array.</param>
        /// <param name="length">The number of elements from the source array to include in the resulting array.</param>
        /// <param name="selector">The function to apply to each element from the source array included in the resulting array.</param>
        /// <param name="arg">An argument to be passed to the selector mapping function.</param>
        /// <remarks>
        /// This overload allows efficient creation of an <see cref="ImmutableArray{T}"/> based on a slice of an existing
        /// <see cref="ImmutableArray{T}"/>, where a mapping function needs to be applied to each element from the source array
        /// included in the resulting array.
        /// </remarks>
        [Pure]
        public static ImmutableArray<TResult> CreateRange<TSource, TArg, TResult>(ImmutableArray<TSource> items, int start, int length, Func<TSource, TArg, TResult> selector, TArg arg)
        {
            int itemsLength = items.Length;

            Requires.Range(start >= 0 && start <= itemsLength, nameof(start));
            Requires.Range(length >= 0 && start + length <= itemsLength, nameof(length));
            Requires.NotNull(selector, nameof(selector));

            if (length == 0)
            {
                return Create<TResult>();
            }

            var array = new TResult[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = selector(items[i + start], arg);
            }

            return new ImmutableArray<TResult>(array);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableArray{T}.Builder"/> class.
        /// </summary>
        /// <typeparam name="T">The type of elements stored in the array.</typeparam>
        /// <returns>A new builder.</returns>
        [Pure]
        public static ImmutableArray<T>.Builder CreateBuilder<T>()
        {
            return Create<T>().ToBuilder();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableArray{T}.Builder"/> class.
        /// </summary>
        /// <typeparam name="T">The type of elements stored in the array.</typeparam>
        /// <param name="initialCapacity">The size of the initial array backing the builder.</param>
        /// <returns>A new builder.</returns>
        [Pure]
        public static ImmutableArray<T>.Builder CreateBuilder<T>(int initialCapacity)
        {
            return new ImmutableArray<T>.Builder(initialCapacity);
        }

        /// <summary>
        /// Enumerates a sequence exactly once and produces an immutable array of its contents.
        /// </summary>
        /// <typeparam name="TSource">The type of element in the sequence.</typeparam>
        /// <param name="items">The sequence to enumerate.</param>
        /// <returns>An immutable array.</returns>
        [Pure]
        public static ImmutableArray<TSource> ToImmutableArray<TSource>(this IEnumerable<TSource> items)
        {
            if (items is ImmutableArray<TSource>)
            {
                return (ImmutableArray<TSource>)items;
            }

            return CreateRange(items);
        }

        /// <summary>
        /// Searches an entire one-dimensional sorted <see cref="ImmutableArray{T}"/> for a specific element,
        /// using the <see cref="IComparable{T}"/> generic interface implemented by each element
        /// of the <see cref="ImmutableArray{T}"/> and by the specified object.
        /// </summary>
        /// <typeparam name="T">The type of element stored in the array.</typeparam>
        /// <param name="array">The sorted, one-dimensional array to search.</param>
        /// <param name="value">The object to search for.</param>
        /// <returns>
        /// The index of the specified <paramref name="value"/> in the specified array, if <paramref name="value"/> is found.
        /// If <paramref name="value"/> is not found and <paramref name="value"/> is less than one or more elements in array,
        /// a negative number which is the bitwise complement of the index of the first
        /// element that is larger than <paramref name="value"/>. If <paramref name="value"/> is not found and <paramref name="value"/> is greater
        /// than any of the elements in array, a negative number which is the bitwise
        /// complement of (the index of the last element plus 1).
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="value"/> does not implement the <see cref="IComparable{T}"/> generic interface, and
        /// the search encounters an element that does not implement the <see cref="IComparable{T}"/>
        /// generic interface.
        /// </exception>
        [Pure]
        public static int BinarySearch<T>(this ImmutableArray<T> array, T value)
        {
            return Array.BinarySearch<T>(array.array, value);
        }

        /// <summary>
        /// Searches an entire one-dimensional sorted <see cref="ImmutableArray{T}"/> for a value using
        /// the specified <see cref="IComparer{T}"/> generic interface.
        /// </summary>
        /// <typeparam name="T">The type of element stored in the array.</typeparam>
        /// <param name="array">The sorted, one-dimensional array to search.</param>
        /// <param name="value">The object to search for.</param>
        /// <param name="comparer">
        /// The <see cref="IComparer{T}"/> implementation to use when comparing
        /// elements; or null to use the <see cref="IComparable{T}"/> implementation of each
        /// element.
        /// </param>
        /// <returns>
        /// The index of the specified <paramref name="value"/> in the specified array, if <paramref name="value"/> is found.
        /// If <paramref name="value"/> is not found and <paramref name="value"/> is less than one or more elements in array,
        /// a negative number which is the bitwise complement of the index of the first
        /// element that is larger than <paramref name="value"/>. If <paramref name="value"/> is not found and <paramref name="value"/> is greater
        /// than any of the elements in array, a negative number which is the bitwise
        /// complement of (the index of the last element plus 1).
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="comparer"/> is null, <paramref name="value"/> does not implement the <see cref="IComparable{T}"/> generic interface, and
        /// the search encounters an element that does not implement the <see cref="IComparable{T}"/>
        /// generic interface.
        /// </exception>
        [Pure]
        public static int BinarySearch<T>(this ImmutableArray<T> array, T value, IComparer<T> comparer)
        {
            return Array.BinarySearch<T>(array.array, value, comparer);
        }

        /// <summary>
        /// Searches a range of elements in a one-dimensional sorted <see cref="ImmutableArray{T}"/> for
        /// a value, using the <see cref="IComparable{T}"/> generic interface implemented by
        /// each element of the <see cref="ImmutableArray{T}"/> and by the specified value.
        /// </summary>
        /// <typeparam name="T">The type of element stored in the array.</typeparam>
        /// <param name="array">The sorted, one-dimensional array to search.</param>
        /// <param name="index">The starting index of the range to search.</param>
        /// <param name="length">The length of the range to search.</param>
        /// <param name="value">The object to search for.</param>
        /// <returns>
        /// The index of the specified <paramref name="value"/> in the specified <paramref name="array"/>, if <paramref name="value"/> is found.
        /// If <paramref name="value"/> is not found and <paramref name="value"/> is less than one or more elements in <paramref name="array"/>,
        /// a negative number which is the bitwise complement of the index of the first
        /// element that is larger than <paramref name="value"/>. If <paramref name="value"/> is not found and <paramref name="value"/> is greater
        /// than any of the elements in <paramref name="array"/>, a negative number which is the bitwise
        /// complement of (the index of the last element plus 1).
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="value"/> does not implement the <see cref="IComparable{T}"/> generic interface, and
        /// the search encounters an element that does not implement the <see cref="IComparable{T}"/>
        /// generic interface.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="index"/> and <paramref name="length"/> do not specify a valid range in <paramref name="array"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> is less than the lower bound of <paramref name="array"/>. -or- <paramref name="length"/> is less than zero.
        /// </exception>
        [Pure]
        public static int BinarySearch<T>(this ImmutableArray<T> array, int index, int length, T value)
        {
            return Array.BinarySearch<T>(array.array, index, length, value);
        }

        /// <summary>
        /// Searches a range of elements in a one-dimensional sorted <see cref="ImmutableArray{T}"/> for
        /// a value, using the specified <see cref="IComparer{T}"/> generic
        /// interface.
        /// </summary>
        /// <typeparam name="T">The type of element stored in the array.</typeparam>
        /// <param name="array">The sorted, one-dimensional array to search.</param>
        /// <param name="index">The starting index of the range to search.</param>
        /// <param name="length">The length of the range to search.</param>
        /// <param name="value">The object to search for.</param>
        /// <param name="comparer">
        /// The <see cref="IComparer{T}"/> implementation to use when comparing
        /// elements; or null to use the <see cref="IComparable{T}"/> implementation of each
        /// element.
        /// </param>
        /// <returns>
        /// The index of the specified <paramref name="value"/> in the specified <paramref name="array"/>, if <paramref name="value"/> is found.
        /// If <paramref name="value"/> is not found and <paramref name="value"/> is less than one or more elements in <paramref name="array"/>,
        /// a negative number which is the bitwise complement of the index of the first
        /// element that is larger than <paramref name="value"/>. If <paramref name="value"/> is not found and <paramref name="value"/> is greater
        /// than any of the elements in <paramref name="array"/>, a negative number which is the bitwise
        /// complement of (the index of the last element plus 1).
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="comparer"/> is null, <paramref name="value"/> does not implement the <see cref="IComparable{T}"/> generic
        /// interface, and the search encounters an element that does not implement the
        /// <see cref="IComparable{T}"/> generic interface.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="index"/> and <paramref name="length"/> do not specify a valid range in <paramref name="array"/>.-or-<paramref name="comparer"/> is null,
        /// and <paramref name="value"/> is of a type that is not compatible with the elements of <paramref name="array"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> is less than the lower bound of <paramref name="array"/>. -or- <paramref name="length"/> is less than zero.
        /// </exception>
        [Pure]
        public static int BinarySearch<T>(this ImmutableArray<T> array, int index, int length, T value, IComparer<T> comparer)
        {
            return Array.BinarySearch<T>(array.array, index, length, value, comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableArray{T}"/> struct.
        /// </summary>
        /// <param name="items">The array from which to copy.</param>
        internal static ImmutableArray<T> CreateDefensiveCopy<T>(T[] items)
        {
            Debug.Assert(items != null);

            if (items.Length == 0)
            {
                return ImmutableArray<T>.Empty; // use just a shared empty array, allowing the input array to be potentially GC'd
            }

            // defensive copy
            var tmp = new T[items.Length];
            Array.Copy(items, 0, tmp, 0, items.Length);
            return new ImmutableArray<T>(tmp);
        }
    }
}
