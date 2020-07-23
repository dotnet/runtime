// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Immutable
{
    /// <summary>
    /// An immutable stack.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the stack.</typeparam>
    public interface IImmutableStack<T> : IEnumerable<T>
    {
        /// <summary>
        /// Gets a value indicating whether this is the empty stack.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this stack is empty; otherwise, <c>false</c>.
        /// </value>
        bool IsEmpty { get; }

        /// <summary>
        /// Gets an empty stack.
        /// </summary>
        IImmutableStack<T> Clear();

        /// <summary>
        /// Pushes an element onto a stack and returns the new stack.
        /// </summary>
        /// <param name="value">The element to push onto the stack.</param>
        /// <returns>The new stack.</returns>
        IImmutableStack<T> Push(T value);

        /// <summary>
        /// Pops the top element off the stack.
        /// </summary>
        /// <returns>The new stack; never <c>null</c></returns>
        /// <exception cref="InvalidOperationException">Thrown when the stack is empty.</exception>
        IImmutableStack<T> Pop();

        /// <summary>
        /// Gets the element on the top of the stack.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the stack is empty.</exception>
        T Peek();
    }
}
