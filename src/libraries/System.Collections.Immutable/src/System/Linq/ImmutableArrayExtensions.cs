// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq
{
    /// <summary>
    /// LINQ extension method overrides that offer greater efficiency for <see cref="ImmutableArray{T}"/> than the standard LINQ methods
    /// </summary>
    public static class ImmutableArrayExtensions
    {
        #region ImmutableArray<T> extensions

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        /// <typeparam name="TResult">The type of the result element.</typeparam>
        /// <param name="immutableArray">The immutable array.</param>
        /// <param name="selector">The selector.</param>
        public static IEnumerable<TResult> Select<T, TResult>(this ImmutableArray<T> immutableArray, Func<T, TResult> selector)
        {
            immutableArray.ThrowNullRefIfNotInitialized();

            // LINQ Select/Where have optimized treatment for arrays.
            // They also do not modify the source arrays or expose them to modifications.
            // Therefore we will just apply Select/Where to the underlying this.array array.
            return immutableArray.array!.Select(selector);
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IEnumerable{T}"/>,
        /// flattens the resulting sequences into one sequence, and invokes a result
        /// selector function on each element therein.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="immutableArray"/>.</typeparam>
        /// <typeparam name="TCollection">The type of the intermediate elements collected by <paramref name="collectionSelector"/>.</typeparam>
        /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
        /// <param name="immutableArray">The immutable array.</param>
        /// <param name="collectionSelector">A transform function to apply to each element of the input sequence.</param>
        /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> whose elements are the result
        /// of invoking the one-to-many transform function <paramref name="collectionSelector"/> on each
        /// element of <paramref name="immutableArray"/> and then mapping each of those sequence elements and their
        /// corresponding source element to a result element.
        /// </returns>
        public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(
            this ImmutableArray<TSource> immutableArray,
            Func<TSource, IEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
        {
            immutableArray.ThrowNullRefIfNotInitialized();
            if (collectionSelector == null || resultSelector == null)
            {
                // throw the same exception as would LINQ
                return Enumerable.SelectMany(immutableArray, collectionSelector!, resultSelector!);
            }

            // This SelectMany overload is used by the C# compiler for a query of the form:
            //     from i in immutableArray
            //     from j in anotherCollection
            //     select Something(i, j);
            // SelectMany accepts an IEnumerable<TSource>, and ImmutableArray<TSource> is a struct.
            // By having a special implementation of SelectMany that operates on the ImmutableArray's
            // underlying array, we can avoid a few allocations, in particular for the boxed
            // immutable array object that would be allocated when it's passed as an IEnumerable<T>,
            // and for the EnumeratorObject that would be allocated when enumerating the boxed array.

            return immutableArray.Length == 0 ?
                Enumerable.Empty<TResult>() :
                SelectManyIterator(immutableArray, collectionSelector, resultSelector);
        }

        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        public static IEnumerable<T> Where<T>(this ImmutableArray<T> immutableArray, Func<T, bool> predicate)
        {
            immutableArray.ThrowNullRefIfNotInitialized();

            // LINQ Select/Where have optimized treatment for arrays.
            // They also do not modify the source arrays or expose them to modifications.
            // Therefore we will just apply Select/Where to the underlying this.array array.
            return immutableArray.array!.Where(predicate);
        }

        /// <summary>
        /// Gets a value indicating whether any elements are in this collection.
        /// </summary>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        /// <param name="immutableArray"></param>
        public static bool Any<T>(this ImmutableArray<T> immutableArray)
        {
            return immutableArray.Length > 0;
        }

        /// <summary>
        /// Gets a value indicating whether any elements are in this collection
        /// that match a given condition.
        /// </summary>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        /// <param name="immutableArray"></param>
        /// <param name="predicate">The predicate.</param>
        public static bool Any<T>(this ImmutableArray<T> immutableArray, Func<T, bool> predicate)
        {
            immutableArray.ThrowNullRefIfNotInitialized();
            Requires.NotNull(predicate, nameof(predicate));

            foreach (var v in immutableArray.array!)
            {
                if (predicate(v))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets a value indicating whether all elements in this collection
        /// match a given condition.
        /// </summary>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        /// <param name="immutableArray"></param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>
        /// <c>true</c> if every element of the source sequence passes the test in the specified predicate, or if the sequence is empty; otherwise, <c>false</c>.
        /// </returns>
        public static bool All<T>(this ImmutableArray<T> immutableArray, Func<T, bool> predicate)
        {
            immutableArray.ThrowNullRefIfNotInitialized();
            Requires.NotNull(predicate, nameof(predicate));

            foreach (var v in immutableArray.array!)
            {
                if (!predicate(v))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether two sequences are equal according to an equality comparer.
        /// </summary>
        /// <typeparam name="TDerived">The type of element in the compared array.</typeparam>
        /// <typeparam name="TBase">The type of element contained by the collection.</typeparam>
        public static bool SequenceEqual<TDerived, TBase>(this ImmutableArray<TBase> immutableArray, ImmutableArray<TDerived> items, IEqualityComparer<TBase>? comparer = null) where TDerived : TBase
        {
            immutableArray.ThrowNullRefIfNotInitialized();
            items.ThrowNullRefIfNotInitialized();
            if (object.ReferenceEquals(immutableArray.array, items.array))
            {
                return true;
            }

            if (immutableArray.Length != items.Length)
            {
                return false;
            }

            if (comparer == null)
            {
                comparer = EqualityComparer<TBase>.Default;
            }

            for (int i = 0; i < immutableArray.Length; i++)
            {
                if (!comparer.Equals(immutableArray.array![i], items.array![i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether two sequences are equal according to an equality comparer.
        /// </summary>
        /// <typeparam name="TDerived">The type of element in the compared array.</typeparam>
        /// <typeparam name="TBase">The type of element contained by the collection.</typeparam>
        public static bool SequenceEqual<TDerived, TBase>(this ImmutableArray<TBase> immutableArray, IEnumerable<TDerived> items, IEqualityComparer<TBase>? comparer = null) where TDerived : TBase
        {
            Requires.NotNull(items, nameof(items));

            if (comparer == null)
            {
                comparer = EqualityComparer<TBase>.Default;
            }

            int i = 0;
            int n = immutableArray.Length;
            foreach (var item in items)
            {
                if (i == n)
                {
                    return false;
                }

                if (!comparer.Equals(immutableArray[i], item))
                {
                    return false;
                }

                i++;
            }

            return i == n;
        }

        /// <summary>
        /// Determines whether two sequences are equal according to an equality comparer.
        /// </summary>
        /// <typeparam name="TDerived">The type of element in the compared array.</typeparam>
        /// <typeparam name="TBase">The type of element contained by the collection.</typeparam>
        public static bool SequenceEqual<TDerived, TBase>(this ImmutableArray<TBase> immutableArray, ImmutableArray<TDerived> items, Func<TBase, TBase, bool> predicate) where TDerived : TBase
        {
            Requires.NotNull(predicate, nameof(predicate));
            immutableArray.ThrowNullRefIfNotInitialized();
            items.ThrowNullRefIfNotInitialized();

            if (object.ReferenceEquals(immutableArray.array, items.array))
            {
                return true;
            }

            if (immutableArray.Length != items.Length)
            {
                return false;
            }

            for (int i = 0, n = immutableArray.Length; i < n; i++)
            {
                if (!predicate(immutableArray[i], items[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Applies an accumulator function over a sequence.
        /// </summary>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        public static T? Aggregate<T>(this ImmutableArray<T> immutableArray, Func<T, T, T> func)
        {
            Requires.NotNull(func, nameof(func));

            if (immutableArray.Length == 0)
            {
                return default;
            }

            var result = immutableArray[0];
            for (int i = 1, n = immutableArray.Length; i < n; i++)
            {
                result = func(result, immutableArray[i]);
            }

            return result;
        }

        /// <summary>
        /// Applies an accumulator function over a sequence.
        /// </summary>
        /// <typeparam name="TAccumulate">The type of the accumulated value.</typeparam>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        public static TAccumulate Aggregate<TAccumulate, T>(this ImmutableArray<T> immutableArray, TAccumulate seed, Func<TAccumulate, T, TAccumulate> func)
        {
            Requires.NotNull(func, nameof(func));

            var result = seed;
            foreach (var v in immutableArray.array!)
            {
                result = func(result, v);
            }

            return result;
        }

        /// <summary>
        /// Applies an accumulator function over a sequence.
        /// </summary>
        /// <typeparam name="TAccumulate">The type of the accumulated value.</typeparam>
        /// <typeparam name="TResult">The type of result returned by the result selector.</typeparam>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        public static TResult Aggregate<TAccumulate, TResult, T>(this ImmutableArray<T> immutableArray, TAccumulate seed, Func<TAccumulate, T, TAccumulate> func, Func<TAccumulate, TResult> resultSelector)
        {
            Requires.NotNull(resultSelector, nameof(resultSelector));

            return resultSelector(Aggregate(immutableArray, seed, func));
        }

        /// <summary>
        /// Returns the element at a specified index in a sequence.
        /// </summary>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        public static T ElementAt<T>(this ImmutableArray<T> immutableArray, int index)
        {
            return immutableArray[index];
        }

        /// <summary>
        /// Returns the element at a specified index in a sequence or a default value if the index is out of range.
        /// </summary>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        public static T? ElementAtOrDefault<T>(this ImmutableArray<T> immutableArray, int index)
        {
            if (index < 0 || index >= immutableArray.Length)
            {
                return default;
            }

            return immutableArray[index];
        }

        /// <summary>
        /// Returns the first element in a sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        public static T First<T>(this ImmutableArray<T> immutableArray, Func<T, bool> predicate)
        {
            Requires.NotNull(predicate, nameof(predicate));

            foreach (var v in immutableArray.array!)
            {
                if (predicate(v))
                {
                    return v;
                }
            }

            // Throw the same exception that LINQ would.
            return Enumerable.Empty<T>().First();
        }

        /// <summary>
        /// Returns the first element in a sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        /// <param name="immutableArray"></param>
        public static T First<T>(this ImmutableArray<T> immutableArray)
        {

            // In the event of an empty array, generate the same exception
            // that the linq extension method would.
            return immutableArray.Length > 0
                ? immutableArray[0]
                : Enumerable.First(immutableArray.array!);
        }

        /// <summary>
        /// Returns the first element of a sequence, or a default value if the sequence contains no elements.
        /// </summary>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        /// <param name="immutableArray"></param>
        public static T? FirstOrDefault<T>(this ImmutableArray<T> immutableArray)
        {
            return immutableArray.array!.Length > 0 ? immutableArray.array[0] : default;
        }

        /// <summary>
        /// Returns the first element of the sequence that satisfies a condition or a default value if no such element is found.
        /// </summary>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        public static T? FirstOrDefault<T>(this ImmutableArray<T> immutableArray, Func<T, bool> predicate)
        {
            Requires.NotNull(predicate, nameof(predicate));

            foreach (var v in immutableArray.array!)
            {
                if (predicate(v))
                {
                    return v;
                }
            }

            return default;
        }

        /// <summary>
        /// Returns the last element of a sequence.
        /// </summary>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        /// <param name="immutableArray"></param>
        public static T Last<T>(this ImmutableArray<T> immutableArray)
        {
            // In the event of an empty array, generate the same exception
            // that the linq extension method would.
            return immutableArray.Length > 0
                ? immutableArray[immutableArray.Length - 1]
                : Enumerable.Last(immutableArray.array!);
        }

        /// <summary>
        /// Returns the last element of a sequence that satisfies a specified condition.
        /// </summary>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        public static T Last<T>(this ImmutableArray<T> immutableArray, Func<T, bool> predicate)
        {
            Requires.NotNull(predicate, nameof(predicate));

            for (int i = immutableArray.Length - 1; i >= 0; i--)
            {
                if (predicate(immutableArray[i]))
                {
                    return immutableArray[i];
                }
            }

            // Throw the same exception that LINQ would.
            return Enumerable.Empty<T>().Last();
        }

        /// <summary>
        /// Returns the last element of a sequence, or a default value if the sequence contains no elements.
        /// </summary>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        /// <param name="immutableArray"></param>
        public static T? LastOrDefault<T>(this ImmutableArray<T> immutableArray)
        {
            immutableArray.ThrowNullRefIfNotInitialized();
            return immutableArray.array!.LastOrDefault()!;
        }

        /// <summary>
        /// Returns the last element of a sequence that satisfies a condition or a default value if no such element is found.
        /// </summary>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        public static T? LastOrDefault<T>(this ImmutableArray<T> immutableArray, Func<T, bool> predicate)
        {
            Requires.NotNull(predicate, nameof(predicate));

            for (int i = immutableArray.Length - 1; i >= 0; i--)
            {
                if (predicate(immutableArray[i]))
                {
                    return immutableArray[i];
                }
            }

            return default;
        }

        /// <summary>
        /// Returns the only element of a sequence, and throws an exception if there is not exactly one element in the sequence.
        /// </summary>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        /// <param name="immutableArray"></param>
        public static T Single<T>(this ImmutableArray<T> immutableArray)
        {
            immutableArray.ThrowNullRefIfNotInitialized();
            return immutableArray.array!.Single();
        }

        /// <summary>
        /// Returns the only element of a sequence that satisfies a specified condition, and throws an exception if more than one such element exists.
        /// </summary>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        public static T Single<T>(this ImmutableArray<T> immutableArray, Func<T, bool> predicate)
        {
            Requires.NotNull(predicate, nameof(predicate));

            bool first = true;
            T? result = default;
            foreach (var v in immutableArray.array!)
            {
                if (predicate(v))
                {
                    if (!first)
                    {
                        ImmutableArray.TwoElementArray.Single(); // throw the same exception as LINQ would
                    }

                    first = false;
                    result = v;
                }
            }

            if (first)
            {
                Enumerable.Empty<T>().Single(); // throw the same exception as LINQ would
            }

            return result!;
        }

        /// <summary>
        /// Returns the only element of a sequence, or a default value if the sequence is empty; this method throws an exception if there is more than one element in the sequence.
        /// </summary>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        /// <param name="immutableArray"></param>
        public static T? SingleOrDefault<T>(this ImmutableArray<T> immutableArray)
        {
            immutableArray.ThrowNullRefIfNotInitialized();
            return immutableArray.array!.SingleOrDefault()!;
        }

        /// <summary>
        /// Returns the only element of a sequence that satisfies a specified condition or a default value if no such element exists; this method throws an exception if more than one element satisfies the condition.
        /// </summary>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        public static T? SingleOrDefault<T>(this ImmutableArray<T> immutableArray, Func<T, bool> predicate)
        {
            Requires.NotNull(predicate, nameof(predicate));

            bool first = true;
            T? result = default;
            foreach (var v in immutableArray.array!)
            {
                if (predicate(v))
                {
                    if (!first)
                    {
                        ImmutableArray.TwoElementArray.Single(); // throw the same exception as LINQ would
                    }

                    first = false;
                    result = v;
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a dictionary based on the contents of this array.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        /// <param name="immutableArray"></param>
        /// <param name="keySelector">The key selector.</param>
        /// <returns>The newly initialized dictionary.</returns>
        public static Dictionary<TKey, T> ToDictionary<TKey, T>(this ImmutableArray<T> immutableArray, Func<T, TKey> keySelector) where TKey : notnull
        {
            return ToDictionary(immutableArray, keySelector, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Creates a dictionary based on the contents of this array.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TElement">The type of the element.</typeparam>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        /// <param name="immutableArray"></param>
        /// <param name="keySelector">The key selector.</param>
        /// <param name="elementSelector">The element selector.</param>
        /// <returns>The newly initialized dictionary.</returns>
        public static Dictionary<TKey, TElement> ToDictionary<TKey, TElement, T>(this ImmutableArray<T> immutableArray, Func<T, TKey> keySelector, Func<T, TElement> elementSelector) where TKey : notnull
        {
            return ToDictionary(immutableArray, keySelector, elementSelector, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Creates a dictionary based on the contents of this array.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        /// <param name="immutableArray"></param>
        /// <param name="keySelector">The key selector.</param>
        /// <param name="comparer">The comparer to initialize the dictionary with.</param>
        /// <returns>The newly initialized dictionary.</returns>
        public static Dictionary<TKey, T> ToDictionary<TKey, T>(this ImmutableArray<T> immutableArray, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
        {
            Requires.NotNull(keySelector, nameof(keySelector));

            var result = new Dictionary<TKey, T>(immutableArray.Length, comparer);
            foreach (var v in immutableArray)
            {
                result.Add(keySelector(v), v);
            }

            return result;
        }

        /// <summary>
        /// Creates a dictionary based on the contents of this array.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TElement">The type of the element.</typeparam>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        /// <param name="immutableArray"></param>
        /// <param name="keySelector">The key selector.</param>
        /// <param name="elementSelector">The element selector.</param>
        /// <param name="comparer">The comparer to initialize the dictionary with.</param>
        /// <returns>The newly initialized dictionary.</returns>
        public static Dictionary<TKey, TElement> ToDictionary<TKey, TElement, T>(this ImmutableArray<T> immutableArray, Func<T, TKey> keySelector, Func<T, TElement> elementSelector, IEqualityComparer<TKey>? comparer) where TKey : notnull
        {
            Requires.NotNull(keySelector, nameof(keySelector));
            Requires.NotNull(elementSelector, nameof(elementSelector));

            var result = new Dictionary<TKey, TElement>(immutableArray.Length, comparer);
            foreach (var v in immutableArray.array!)
            {
                result.Add(keySelector(v), elementSelector(v));
            }

            return result;
        }

        /// <summary>
        /// Copies the contents of this array to a mutable array.
        /// </summary>
        /// <typeparam name="T">The type of element contained by the collection.</typeparam>
        /// <param name="immutableArray"></param>
        /// <returns>The newly instantiated array.</returns>
        public static T[] ToArray<T>(this ImmutableArray<T> immutableArray)
        {
            immutableArray.ThrowNullRefIfNotInitialized();
            if (immutableArray.array!.Length == 0)
            {
                return ImmutableArray<T>.Empty.array!;
            }

            return (T[])immutableArray.array.Clone();
        }

        #endregion

        #region ImmutableArray<T>.Builder extensions

        /// <summary>
        /// Returns the first element in the collection.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the collection is empty.</exception>
        public static T First<T>(this ImmutableArray<T>.Builder builder)
        {
            Requires.NotNull(builder, nameof(builder));

            if (!builder.Any())
            {
                throw new InvalidOperationException();
            }

            return builder[0];
        }

        /// <summary>
        /// Returns the first element in the collection, or the default value if the collection is empty.
        /// </summary>
        public static T? FirstOrDefault<T>(this ImmutableArray<T>.Builder builder)
        {
            Requires.NotNull(builder, nameof(builder));

            return builder.Any() ? builder[0] : default;
        }

        /// <summary>
        /// Returns the last element in the collection.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the collection is empty.</exception>
        public static T Last<T>(this ImmutableArray<T>.Builder builder)
        {
            Requires.NotNull(builder, nameof(builder));

            if (!builder.Any())
            {
                throw new InvalidOperationException();
            }

            return builder[builder.Count - 1];
        }

        /// <summary>
        /// Returns the last element in the collection, or the default value if the collection is empty.
        /// </summary>
        public static T? LastOrDefault<T>(this ImmutableArray<T>.Builder builder)
        {
            Requires.NotNull(builder, nameof(builder));

            return builder.Any() ? builder[builder.Count - 1] : default;
        }

        /// <summary>
        /// Returns a value indicating whether this collection contains any elements.
        /// </summary>
        public static bool Any<T>(this ImmutableArray<T>.Builder builder)
        {
            Requires.NotNull(builder, nameof(builder));

            return builder.Count > 0;
        }
        #endregion

        #region Private Implementation Details
        /// <summary>Provides the core iterator implementation of <see cref="SelectMany"/>.</summary>
        private static IEnumerable<TResult> SelectManyIterator<TSource, TCollection, TResult>(
            this ImmutableArray<TSource> immutableArray,
            Func<TSource, IEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
        {
            foreach (TSource item in immutableArray.array!)
            {
                foreach (TCollection result in collectionSelector(item))
                {
                    yield return resultSelector(item, result);
                }
            }
        }
        #endregion
    }
}
