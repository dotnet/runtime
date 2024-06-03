// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A set of initialization methods for instances of <see cref="ImmutableStack{T}"/>.
    /// </summary>
    public static class ImmutableStack
    {
        /// <summary>
        /// Returns an empty collection.
        /// </summary>
        /// <typeparam name="T">The type of items stored by the collection.</typeparam>
        /// <returns>The immutable collection.</returns>
        public static ImmutableStack<T> Create<T>()
        {
            return ImmutableStack<T>.Empty;
        }

        /// <summary>
        /// Creates a new immutable collection prefilled with the specified item.
        /// </summary>
        /// <typeparam name="T">The type of items stored by the collection.</typeparam>
        /// <param name="item">The item to prepopulate.</param>
        /// <returns>The new immutable collection.</returns>
        public static ImmutableStack<T> Create<T>(T item)
        {
            return ImmutableStack<T>.Empty.Push(item);
        }

        /// <summary>
        /// Creates a new immutable collection prefilled with the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items stored by the collection.</typeparam>
        /// <param name="items">The items to prepopulate.</param>
        /// <returns>The new immutable collection.</returns>
        public static ImmutableStack<T> CreateRange<T>(IEnumerable<T> items)
        {
            Requires.NotNull(items, nameof(items));

            ImmutableStack<T> stack = ImmutableStack<T>.Empty;
            foreach (T item in items)
            {
                stack = stack.Push(item);
            }

            return stack;
        }

        /// <summary>
        /// Creates a new immutable collection prefilled with the specified items.
        /// </summary>
        /// <typeparam name="T">The type of items stored by the collection.</typeparam>
        /// <param name="items">The items to prepopulate.</param>
        /// <returns>The new immutable collection.</returns>
        public static ImmutableStack<T> Create<T>(params T[] items)
        {
            Requires.NotNull(items, nameof(items));

            return Create((ReadOnlySpan<T>)items);
        }

        /// <summary>
        /// Creates a new immutable stack that contains the specified array of items.
        /// </summary>
        /// <typeparam name="T">The type of items in the immutable stack.</typeparam>
        /// <param name="items">A span that contains the items to prepopulate the stack with.</param>
        /// <returns>A new immutable stack that contains the specified items.</returns>
        public static ImmutableStack<T> Create<T>(params ReadOnlySpan<T> items)
        {
            ImmutableStack<T> stack = ImmutableStack<T>.Empty;
            foreach (T item in items)
            {
                stack = stack.Push(item);
            }

            return stack;
        }

        /// <summary>
        /// Pops the top element off the stack.
        /// </summary>
        /// <typeparam name="T">The type of values contained in the stack.</typeparam>
        /// <param name="stack">The stack to modify.</param>
        /// <param name="value">The value that was removed from the stack.</param>
        /// <returns>
        /// A stack; never <c>null</c>
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown when the stack is empty.</exception>
        public static IImmutableStack<T> Pop<T>(this IImmutableStack<T> stack, out T value)
        {
            Requires.NotNull(stack, nameof(stack));

            value = stack.Peek();
            return stack.Pop();
        }
    }
}
