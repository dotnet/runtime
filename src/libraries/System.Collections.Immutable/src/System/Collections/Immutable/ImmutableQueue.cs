// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A set of initialization methods for instances of <see cref="ImmutableQueue{T}"/>.
    /// </summary>
    public static class ImmutableQueue
    {
        /// <summary>
        /// Returns an empty collection.
        /// </summary>
        /// <typeparam name="T">The type of items stored by the collection.</typeparam>
        /// <returns>The immutable collection.</returns>
        public static ImmutableQueue<T> Create<T>()
        {
            return ImmutableQueue<T>.Empty;
        }

        /// <summary>
        /// Creates a new immutable collection prefilled with the specified item.
        /// </summary>
        /// <typeparam name="T">The type of items stored by the collection.</typeparam>
        /// <param name="item">The item to prepopulate.</param>
        /// <returns>The new immutable collection.</returns>
        public static ImmutableQueue<T> Create<T>(T item) => ImmutableQueue<T>.Empty.Enqueue(item);

        /// <summary>
        /// Creates a new immutable queue from the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items to store in the queue.</typeparam>
        /// <param name="items">The enumerable to copy items from.</param>
        /// <returns>The new immutable queue.</returns>
        public static ImmutableQueue<T> CreateRange<T>(IEnumerable<T> items)
        {
            Requires.NotNull(items, nameof(items));

            if (items is T[] array)
            {
                return Create(items: array);
            }

            using (IEnumerator<T> e = items.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    return ImmutableQueue<T>.Empty;
                }

                ImmutableStack<T> forwards = ImmutableStack.Create(e.Current);
                ImmutableStack<T> backwards = ImmutableStack<T>.Empty;

                while (e.MoveNext())
                {
                    backwards = backwards.Push(e.Current);
                }

                return new ImmutableQueue<T>(forwards: forwards, backwards: backwards);
            }
        }

        /// <summary>
        /// Creates a new immutable queue from the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items to store in the queue.</typeparam>
        /// <param name="items">The array to copy items from.</param>
        /// <returns>The new immutable queue.</returns>
        public static ImmutableQueue<T> Create<T>(params T[] items)
        {
            Requires.NotNull(items, nameof(items));

            return Create((ReadOnlySpan<T>)items);
        }

        /// <summary>
        /// Creates a new immutable queue that contains the specified array of items.
        /// </summary>
        /// <typeparam name="T">The type of items in the immutable queue.</typeparam>
        /// <param name="items">A span that contains the items to prepopulate the queue with.</param>
        /// <returns>A new immutable queue that contains the specified items.</returns>
        public static ImmutableQueue<T> Create<T>(params ReadOnlySpan<T> items)
        {
            if (items.IsEmpty)
            {
                return ImmutableQueue<T>.Empty;
            }

            ImmutableStack<T> forwards = ImmutableStack<T>.Empty;

            for (int i = items.Length - 1; i >= 0; i--)
            {
                forwards = forwards.Push(items[i]);
            }

            return new ImmutableQueue<T>(forwards: forwards, backwards: ImmutableStack<T>.Empty);
        }

        /// <summary>
        /// Retrieves the item at the head of the queue, and returns a queue with the head element removed.
        /// </summary>
        /// <typeparam name="T">The type of elements stored in the queue.</typeparam>
        /// <param name="queue">The queue to dequeue from.</param>
        /// <param name="value">Receives the value from the head of the queue.</param>
        /// <returns>The new queue with the head element removed.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the stack is empty.</exception>
        public static IImmutableQueue<T> Dequeue<T>(this IImmutableQueue<T> queue, out T value)
        {
            Requires.NotNull(queue, nameof(queue));

            value = queue.Peek();
            return queue.Dequeue();
        }
    }
}
