// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>
        /// Sorts the elements of a sequence in ascending order.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <returns>An <see cref="IOrderedEnumerable{TElement}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method is implemented by using deferred execution. The immediate return value is an object
        /// that stores all the information that is required to perform the action.
        /// The query represented by this method is not executed until the object is enumerated by calling
        /// its <see cref="IEnumerable{T}.GetEnumerator"/> method.
        ///
        /// This method compares elements by using the default comparer <see cref="Comparer{T}.Default"/>.
        /// </remarks>
        public static IOrderedEnumerable<T> Order<T>(this IEnumerable<T> source) =>
            Order(source, comparer: null);

        /// <summary>
        /// Sorts the elements of a sequence in ascending order.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
        /// <returns>An <see cref="IOrderedEnumerable{TElement}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method is implemented by using deferred execution. The immediate return value is an object
        /// that stores all the information that is required to perform the action.
        /// The query represented by this method is not executed until the object is enumerated by calling
        /// its <see cref="IEnumerable{T}.GetEnumerator"/> method.
        ///
        /// If comparer is <see langword="null"/>, the default comparer <see cref="Comparer{T}.Default"/> is used to compare elements.
        /// </remarks>
        public static IOrderedEnumerable<T> Order<T>(this IEnumerable<T> source, IComparer<T>? comparer) =>
            TypeIsImplicitlyStable<T>() && (comparer is null || comparer == Comparer<T>.Default) ?
                new ImplicitlyStableOrderedIterator<T>(source, descending: false) :
                OrderBy(source, EnumerableSorter<T>.IdentityFunc, comparer);

        public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
            => new OrderedIterator<TSource, TKey>(source, keySelector, null, false, null);

        public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
            => new OrderedIterator<TSource, TKey>(source, keySelector, comparer, false, null);

        /// <summary>
        /// Sorts the elements of a sequence in descending order.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <returns>An <see cref="IOrderedEnumerable{TElement}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method is implemented by using deferred execution. The immediate return value is an object
        /// that stores all the information that is required to perform the action.
        /// The query represented by this method is not executed until the object is enumerated by calling
        /// its <see cref="IEnumerable{T}.GetEnumerator"/> method.
        ///
        /// This method compares elements by using the default comparer <see cref="Comparer{T}.Default"/>.
        /// </remarks>
        public static IOrderedEnumerable<T> OrderDescending<T>(this IEnumerable<T> source) =>
            OrderDescending(source, comparer: null);

        /// <summary>
        /// Sorts the elements of a sequence in descending order.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to order.</param>
        /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys.</param>
        /// <returns>An <see cref="IOrderedEnumerable{TElement}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method is implemented by using deferred execution. The immediate return value is an object
        /// that stores all the information that is required to perform the action.
        /// The query represented by this method is not executed until the object is enumerated by calling
        /// its <see cref="IEnumerable{T}.GetEnumerator"/> method.
        ///
        /// If comparer is <see langword="null"/>, the default comparer <see cref="Comparer{T}.Default"/> is used to compare elements.
        /// </remarks>
        public static IOrderedEnumerable<T> OrderDescending<T>(this IEnumerable<T> source, IComparer<T>? comparer) =>
            TypeIsImplicitlyStable<T>() && (comparer is null || comparer == Comparer<T>.Default) ?
                new ImplicitlyStableOrderedIterator<T>(source, descending: true) :
                OrderByDescending(source, EnumerableSorter<T>.IdentityFunc, comparer);

        public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) =>
            new OrderedIterator<TSource, TKey>(source, keySelector, null, true, null);

        public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer) =>
            new OrderedIterator<TSource, TKey>(source, keySelector, comparer, true, null);

        public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return source.CreateOrderedEnumerable(keySelector, null, false);
        }

        public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return source.CreateOrderedEnumerable(keySelector, comparer, false);
        }

        public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return source.CreateOrderedEnumerable(keySelector, null, true);
        }

        public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return source.CreateOrderedEnumerable(keySelector, comparer, true);
        }

        /// <summary>Gets whether the results of an unstable sort will be observably the same as a stable sort.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TypeIsImplicitlyStable<T>() =>
            typeof(T) == typeof(sbyte) || typeof(T) == typeof(byte) ||
            typeof(T) == typeof(int) || typeof(T) == typeof(uint) ||
            typeof(T) == typeof(short) || typeof(T) == typeof(ushort) ||
            typeof(T) == typeof(long) || typeof(T) == typeof(ulong) ||
            typeof(T) == typeof(Int128) || typeof(T) == typeof(UInt128) ||
            typeof(T) == typeof(nint) || typeof(T) == typeof(nuint) ||
            typeof(T) == typeof(bool) || typeof(T) == typeof(char);
    }

    public interface IOrderedEnumerable<out TElement> : IEnumerable<TElement>
    {
        IOrderedEnumerable<TElement> CreateOrderedEnumerable<TKey>(Func<TElement, TKey> keySelector, IComparer<TKey>? comparer, bool descending);
    }
}
