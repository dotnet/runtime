// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace System.Collections.Immutable
{
    /// <summary>
    /// An immutable stack.
    /// </summary>
    /// <typeparam name="T">The type of element stored by the stack.</typeparam>
    [DebuggerDisplay("IsEmpty = {IsEmpty}; Top = {_head}")]
    [DebuggerTypeProxy(typeof(ImmutableEnumerableDebuggerProxy<>))]
    public sealed partial class ImmutableStack<T> : IImmutableStack<T>
    {
        /// <summary>
        /// The singleton empty stack.
        /// </summary>
        /// <remarks>
        /// Additional instances representing the empty stack may exist on deserialized stacks.
        /// </remarks>
        private static readonly ImmutableStack<T> s_EmptyField = new ImmutableStack<T>();

        /// <summary>
        /// The element on the top of the stack.
        /// </summary>
        private readonly T? _head;

        /// <summary>
        /// A stack that contains the rest of the elements (under the top element).
        /// </summary>
        private readonly ImmutableStack<T>? _tail;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableStack{T}"/> class
        /// that acts as the empty stack.
        /// </summary>
        private ImmutableStack()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableStack{T}"/> class.
        /// </summary>
        /// <param name="head">The head element on the stack.</param>
        /// <param name="tail">The rest of the elements on the stack.</param>
        private ImmutableStack(T head, ImmutableStack<T> tail)
        {
            Debug.Assert(tail != null);

            _head = head;
            _tail = tail;
        }

        /// <summary>
        /// Gets the empty stack, upon which all stacks are built.
        /// </summary>
        public static ImmutableStack<T> Empty
        {
            get
            {
                Debug.Assert(s_EmptyField.IsEmpty);
                return s_EmptyField;
            }
        }

        /// <summary>
        /// Gets the empty stack, upon which all stacks are built.
        /// </summary>
        public ImmutableStack<T> Clear()
        {
            Debug.Assert(s_EmptyField.IsEmpty);
            return Empty;
        }

        /// <summary>
        /// Gets an empty stack.
        /// </summary>
        IImmutableStack<T> IImmutableStack<T>.Clear()
        {
            return this.Clear();
        }

        /// <summary>
        /// Gets a value indicating whether this instance is empty.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is empty; otherwise, <c>false</c>.
        /// </value>
        public bool IsEmpty
        {
            get { return _tail == null; }
        }

        /// <summary>
        /// Gets the element on the top of the stack.
        /// </summary>
        /// <returns>
        /// The element on the top of the stack.
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown when the stack is empty.</exception>
        public T Peek()
        {
            if (this.IsEmpty)
            {
                throw new InvalidOperationException(SR.InvalidEmptyOperation);
            }

            return _head!;
        }

#if !NETSTANDARD1_0
        /// <summary>
        /// Gets a read-only reference to the element on the top of the stack.
        /// </summary>
        /// <returns>
        /// A read-only reference to the element on the top of the stack.
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown when the stack is empty.</exception>
        public ref readonly T PeekRef()
        {
            if (this.IsEmpty)
            {
                throw new InvalidOperationException(SR.InvalidEmptyOperation);
            }

            return ref _head!;
        }
#endif

        /// <summary>
        /// Pushes an element onto a stack and returns the new stack.
        /// </summary>
        /// <param name="value">The element to push onto the stack.</param>
        /// <returns>The new stack.</returns>
        public ImmutableStack<T> Push(T value)
        {
            return new ImmutableStack<T>(value, this);
        }

        /// <summary>
        /// Pushes an element onto a stack and returns the new stack.
        /// </summary>
        /// <param name="value">The element to push onto the stack.</param>
        /// <returns>The new stack.</returns>
        IImmutableStack<T> IImmutableStack<T>.Push(T value)
        {
            return this.Push(value);
        }

        /// <summary>
        /// Returns a stack that lacks the top element on this stack.
        /// </summary>
        /// <returns>A stack; never <c>null</c></returns>
        /// <exception cref="InvalidOperationException">Thrown when the stack is empty.</exception>
        public ImmutableStack<T> Pop()
        {
            if (this.IsEmpty)
            {
                throw new InvalidOperationException(SR.InvalidEmptyOperation);
            }

            Debug.Assert(_tail != null);
            return _tail;
        }

        /// <summary>
        /// Pops the top element off the stack.
        /// </summary>
        /// <param name="value">The value that was removed from the stack.</param>
        /// <returns>
        /// A stack; never <c>null</c>
        /// </returns>
        public ImmutableStack<T> Pop(out T value)
        {
            value = this.Peek();
            return this.Pop();
        }

        /// <summary>
        /// Returns a stack that lacks the top element on this stack.
        /// </summary>
        /// <returns>A stack; never <c>null</c></returns>
        /// <exception cref="InvalidOperationException">Thrown when the stack is empty.</exception>
        IImmutableStack<T> IImmutableStack<T>.Pop()
        {
            return this.Pop();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An <see cref="Enumerator"/> that can be used to iterate through the collection.
        /// </returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
        /// </returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this.IsEmpty ?
                Enumerable.Empty<T>().GetEnumerator() :
                new EnumeratorObject(this);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new EnumeratorObject(this);
        }

        /// <summary>
        /// Reverses the order of a stack.
        /// </summary>
        /// <returns>The reversed stack.</returns>
        internal ImmutableStack<T> Reverse()
        {
            var r = this.Clear();
            for (ImmutableStack<T> f = this; !f.IsEmpty; f = f.Pop())
            {
                r = r.Push(f.Peek());
            }

            Debug.Assert(r != null);
            Debug.Assert(r.IsEmpty == IsEmpty);
            return r;
        }
    }
}
