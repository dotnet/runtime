// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace System.Collections.Immutable
{
    /// <summary>
    /// Extension methods for immutable types.
    /// </summary>
    internal static partial class ImmutableExtensions
    {
        internal static bool IsValueType<T>()
        {
#if NETCOREAPP
            return typeof(T).IsValueType;
#else
            if (default(T) != null)
            {
                return true;
            }

            Type t = typeof(T);
            if (t.IsConstructedGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return true;
            }

            return false;
#endif
        }

        /// <summary>
        /// Provides a known wrapper around a sequence of elements that provides the number of elements
        /// and an indexer into its contents.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="sequence">The collection.</param>
        /// <returns>An ordered collection.  May not be thread-safe.  Never null.</returns>
        internal static IOrderedCollection<T> AsOrderedCollection<T>(this IEnumerable<T> sequence)
        {
            Requires.NotNull(sequence, nameof(sequence));

            var orderedCollection = sequence as IOrderedCollection<T>;
            if (orderedCollection != null)
            {
                return orderedCollection;
            }

            var listOfT = sequence as IList<T>;
            if (listOfT != null)
            {
                return new ListOfTWrapper<T>(listOfT);
            }

            // It would be great if SortedSet<T> and SortedDictionary<T> provided indexers into their collections,
            // but since they don't we have to clone them to an array.
            return new FallbackWrapper<T>(sequence);
        }

        /// <summary>
        /// Clears the specified stack.  For empty stacks, it avoids the call to <see cref="Stack{T}.Clear"/>, which
        /// avoids a call into the runtime's implementation of <see cref="Array.Clear"/>, helping performance,
        /// in particular around inlining.  <see cref="Stack{T}.Count"/> typically gets inlined by today's JIT, while
        /// <see cref="Stack{T}.Clear"/> and <see cref="Array.Clear"/> typically don't.
        /// </summary>
        /// <typeparam name="T">Specifies the type of data in the stack to be cleared.</typeparam>
        /// <param name="stack">The stack to clear.</param>
        internal static void ClearFastWhenEmpty<T>(this Stack<T> stack)
        {
            if (stack.Count > 0)
            {
                stack.Clear();
            }
        }

        /// <summary>
        /// Gets a disposable enumerable that can be used as the source for a C# foreach loop
        /// that will not box the enumerator if it is of a particular type.
        /// </summary>
        /// <typeparam name="T">The type of value to be enumerated.</typeparam>
        /// <typeparam name="TEnumerator">The type of the Enumerator struct.</typeparam>
        /// <param name="enumerable">The collection to be enumerated.</param>
        /// <returns>A struct that enumerates the collection.</returns>
        internal static DisposableEnumeratorAdapter<T, TEnumerator> GetEnumerableDisposable<T, TEnumerator>(this IEnumerable<T> enumerable)
            where TEnumerator : struct, IStrongEnumerator<T>, IEnumerator<T>
        {
            Requires.NotNull(enumerable, nameof(enumerable));

            var strongEnumerable = enumerable as IStrongEnumerable<T, TEnumerator>;
            if (strongEnumerable != null)
            {
                return new DisposableEnumeratorAdapter<T, TEnumerator>(strongEnumerable.GetEnumerator());
            }
            else
            {
                // Consider for future: we could add more special cases for common
                // mutable collection types like List<T>+Enumerator and such.
                return new DisposableEnumeratorAdapter<T, TEnumerator>(enumerable.GetEnumerator());
            }
        }

        /// <summary>
        /// Wraps a <see cref="IList{T}"/> as an ordered collection.
        /// </summary>
        /// <typeparam name="T">The type of element in the collection.</typeparam>
        private sealed class ListOfTWrapper<T> : IOrderedCollection<T>
        {
            /// <summary>
            /// The list being exposed.
            /// </summary>
            private readonly IList<T> _collection;

            /// <summary>
            /// Initializes a new instance of the <see cref="ListOfTWrapper{T}"/> class.
            /// </summary>
            /// <param name="collection">The collection.</param>
            internal ListOfTWrapper(IList<T> collection)
            {
                Requires.NotNull(collection, nameof(collection));
                _collection = collection;
            }

            /// <summary>
            /// Gets the count.
            /// </summary>
            public int Count
            {
                get { return _collection.Count; }
            }

            /// <summary>
            /// Gets the <typeparamref name="T"/> at the specified index.
            /// </summary>
            public T this[int index]
            {
                get { return _collection[index]; }
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            public IEnumerator<T> GetEnumerator()
            {
                return _collection.GetEnumerator();
            }

            /// <summary>
            /// Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>
            /// An <see cref="IEnumerator"/> object that can be used to iterate through the collection.
            /// </returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        /// <summary>
        /// Wraps any <see cref="IEnumerable{T}"/> as an ordered, indexable list.
        /// </summary>
        /// <typeparam name="T">The type of element in the collection.</typeparam>
        private sealed class FallbackWrapper<T> : IOrderedCollection<T>
        {
            /// <summary>
            /// The original sequence.
            /// </summary>
            private readonly IEnumerable<T> _sequence;

            /// <summary>
            /// The list-ified sequence.
            /// </summary>
            private IList<T>? _collection;

            /// <summary>
            /// Initializes a new instance of the <see cref="FallbackWrapper{T}"/> class.
            /// </summary>
            /// <param name="sequence">The sequence.</param>
            internal FallbackWrapper(IEnumerable<T> sequence)
            {
                Requires.NotNull(sequence, nameof(sequence));
                _sequence = sequence;
            }

            /// <summary>
            /// Gets the count.
            /// </summary>
            public int Count
            {
                get
                {
                    if (_collection == null)
                    {
                        int count;
                        if (_sequence.TryGetCount(out count))
                        {
                            return count;
                        }

                        _collection = _sequence.ToArray();
                    }

                    return _collection.Count;
                }
            }

            /// <summary>
            /// Gets the <typeparamref name="T"/> at the specified index.
            /// </summary>
            public T this[int index]
            {
                get
                {
                    if (_collection == null)
                    {
                        _collection = _sequence.ToArray();
                    }

                    return _collection[index];
                }
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
            /// </returns>
            public IEnumerator<T> GetEnumerator()
            {
                return _sequence.GetEnumerator();
            }

            /// <summary>
            /// Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>
            /// An <see cref="IEnumerator"/> object that can be used to iterate through the collection.
            /// </returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }
    }
}
