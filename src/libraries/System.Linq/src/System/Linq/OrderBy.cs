// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

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
        /// Two methods are defined to extend the type <see cref="IOrderedEnumerable{TElement}"/>, which is the return type of this method.
        /// These two methods, namely <see cref="ThenBy{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/>
        /// and <see cref="ThenByDescending{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/>, enable you to specify additional
        /// sort criteria to sort a sequence.
        /// <see cref="ThenBy{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/>
        /// and <see cref="ThenByDescending{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/> also return
        /// an <see cref="IOrderedEnumerable{TElement}"/>, which means any number of consecutive calls
        /// to <see cref="ThenBy{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/>
        /// or <see cref="ThenByDescending{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/> can be made.
        ///
        /// This method compares keys by using the default comparer <see cref="Comparer{T}.Default"/>.
        /// </remarks>
        public static IOrderedEnumerable<T> Order<T>(this IEnumerable<T> source) =>
            new OrderedKeylessEnumerable<T>(source, null, false, null);

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
        /// Two methods are defined to extend the type <see cref="IOrderedEnumerable{TElement}"/>, which is the return type of this method.
        /// These two methods, namely <see cref="ThenBy{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/>
        /// and <see cref="ThenByDescending{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/>, enable you to specify additional
        /// sort criteria to sort a sequence.
        /// <see cref="ThenBy{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/>
        /// and <see cref="ThenByDescending{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/> also return
        /// an <see cref="IOrderedEnumerable{TElement}"/>, which means any number of consecutive calls
        /// to <see cref="ThenBy{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/>
        /// or <see cref="ThenByDescending{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/> can be made.
        ///
        /// If comparer is <see langword="null"/>, the default comparer <see cref="Comparer{T}.Default"/> is used to compare keys.
        /// </remarks>
        public static IOrderedEnumerable<T> Order<T>(this IEnumerable<T> source, IComparer<T> comparer) =>
            new OrderedKeylessEnumerable<T>(source, comparer, false, null);

        public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) =>
            new OrderedEnumerable<TSource, TKey>(source, keySelector, null, false, null);

        public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer) =>
            new OrderedEnumerable<TSource, TKey>(source, keySelector, comparer, false, null);

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
        /// Two methods are defined to extend the type <see cref="IOrderedEnumerable{TElement}"/>, which is the return type of this method.
        /// These two methods, namely <see cref="ThenBy{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/>
        /// and <see cref="ThenByDescending{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/>, enable you to specify additional
        /// sort criteria to sort a sequence.
        /// <see cref="ThenBy{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/>
        /// and <see cref="ThenByDescending{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/> also return
        /// an <see cref="IOrderedEnumerable{TElement}"/>, which means any number of consecutive calls
        /// to <see cref="ThenBy{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/>
        /// or <see cref="ThenByDescending{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/> can be made.
        ///
        /// This method compares keys by using the default comparer <see cref="Comparer{T}.Default"/>.
        /// </remarks>
        public static IOrderedEnumerable<T> OrderDescending<T>(this IEnumerable<T> source) =>
            new OrderedKeylessEnumerable<T>(source, null, true, null);

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
        /// Two methods are defined to extend the type <see cref="IOrderedEnumerable{TElement}"/>, which is the return type of this method.
        /// These two methods, namely <see cref="ThenBy{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/>
        /// and <see cref="ThenByDescending{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/>, enable you to specify additional
        /// sort criteria to sort a sequence.
        /// <see cref="ThenBy{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/>
        /// and <see cref="ThenByDescending{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/> also return
        /// an <see cref="IOrderedEnumerable{TElement}"/>, which means any number of consecutive calls
        /// to <see cref="ThenBy{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/>
        /// or <see cref="ThenByDescending{TSource, TKey}(IOrderedEnumerable{TSource}, Func{TSource, TKey})"/> can be made.
        ///
        /// If comparer is <see langword="null"/>, the default comparer <see cref="Comparer{T}.Default"/> is used to compare keys.
        /// </remarks>
        public static IOrderedEnumerable<T> OrderDescending<T>(this IEnumerable<T> source, IComparer<T> comparer) =>
            new OrderedKeylessEnumerable<T>(source, comparer, true, null);

        public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) =>
            new OrderedEnumerable<TSource, TKey>(source, keySelector, null, true, null);

        public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer) =>
            new OrderedEnumerable<TSource, TKey>(source, keySelector, comparer, true, null);

        public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return source.CreateOrderedEnumerable(keySelector, null, false);
        }

        public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return source.CreateOrderedEnumerable(keySelector, comparer, false);
        }

        public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return source.CreateOrderedEnumerable(keySelector, null, true);
        }

        public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return source.CreateOrderedEnumerable(keySelector, comparer, true);
        }
    }

    public interface IOrderedEnumerable<out TElement> : IEnumerable<TElement>
    {
        IOrderedEnumerable<TElement> CreateOrderedEnumerable<TKey>(Func<TElement, TKey> keySelector, IComparer<TKey>? comparer, bool descending);
    }
}
