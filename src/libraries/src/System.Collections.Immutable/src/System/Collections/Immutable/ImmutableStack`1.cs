// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using Validation;

namespace System.Collections.Immutable
{
    /// <summary>
    /// An immutable stack.
    /// </summary>
    /// <typeparam name="T">The type of element stored by the stack.</typeparam>
    [DebuggerDisplay("IsEmpty = {IsEmpty}; Top = {head}")]
    [DebuggerTypeProxy(typeof(ImmutableStack<>.DebuggerProxy))]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Justification = "Ignored")]
    public sealed class ImmutableStack<T> : IImmutableStack<T>
    {
        /// <summary>
        /// The singleton empty stack.
        /// </summary>
        /// <remarks>
        /// Additional instances representing the empty stack may exist on deserialized stacks.
        /// </remarks>
        private static readonly ImmutableStack<T> EmptyField = new ImmutableStack<T>();

        /// <summary>
        /// The element on the top of the stack.
        /// </summary>
        private readonly T head;

        /// <summary>
        /// A stack that contains the rest of the elements (under the top element).
        /// </summary>
        private readonly ImmutableStack<T> tail;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableStack&lt;T&gt;"/> class
        /// that acts as the empty stack.
        /// </summary>
        private ImmutableStack()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableStack&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="head">The head element on the stack.</param>
        /// <param name="tail">The rest of the elements on the stack.</param>
        private ImmutableStack(T head, ImmutableStack<T> tail)
        {
            Requires.NotNull(tail, "tail");
            this.head = head;
            this.tail = tail;
        }

        /// <summary>
        /// Gets the empty stack, upon which all stacks are built.
        /// </summary>
        public static ImmutableStack<T> Empty
        {
            get
            {
                Contract.Ensures(Contract.Result<ImmutableStack<T>>() != null);
                Contract.Ensures(Contract.Result<ImmutableStack<T>>().IsEmpty);
                Contract.Assume(EmptyField.IsEmpty);
                return EmptyField;
            }
        }

        /// <summary>
        /// Gets the empty stack, upon which all stacks are built.
        /// </summary>
        public ImmutableStack<T> Clear()
        {
            Contract.Ensures(Contract.Result<ImmutableStack<T>>() != null);
            Contract.Ensures(Contract.Result<ImmutableStack<T>>().IsEmpty);
            Contract.Assume(EmptyField.IsEmpty);
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
            get { return this.tail == null; }
        }

        /// <summary>
        /// Gets the element on the top of the stack.
        /// </summary>
        /// <returns>
        /// The element on the top of the stack. 
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown when the stack is empty.</exception>
        [Pure]
        public T Peek()
        {
            if (this.IsEmpty)
            {
                throw new InvalidOperationException(Strings.InvalidEmptyOperation);
            }

            return this.head;
        }

        /// <summary>
        /// Pushes an element onto a stack and returns the new stack.
        /// </summary>
        /// <param name="value">The element to push onto the stack.</param>
        /// <returns>The new stack.</returns>
        [Pure]
        public ImmutableStack<T> Push(T value)
        {
            Contract.Ensures(Contract.Result<ImmutableStack<T>>() != null);
            Contract.Ensures(!Contract.Result<ImmutableStack<T>>().IsEmpty);
            return new ImmutableStack<T>(value, this);
        }

        /// <summary>
        /// Pushes an element onto a stack and returns the new stack.
        /// </summary>
        /// <param name="value">The element to push onto the stack.</param>
        /// <returns>The new stack.</returns>
        [Pure]
        IImmutableStack<T> IImmutableStack<T>.Push(T value)
        {
            return this.Push(value);
        }

        /// <summary>
        /// Returns a stack that lacks the top element on this stack.
        /// </summary>
        /// <returns>A stack; never <c>null</c></returns>
        /// <exception cref="InvalidOperationException">Thrown when the stack is empty.</exception>
        [Pure]
        public ImmutableStack<T> Pop()
        {
            Contract.Ensures(Contract.Result<ImmutableStack<T>>() != null);
            if (this.IsEmpty)
            {
                throw new InvalidOperationException(Strings.InvalidEmptyOperation);
            }

            return this.tail;
        }

        /// <summary>
        /// Pops the top element off the stack.
        /// </summary>
        /// <param name="value">The value that was removed from the stack.</param>
        /// <returns>
        /// A stack; never <c>null</c>
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "0#")]
        [Pure]
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
        [Pure]
        IImmutableStack<T> IImmutableStack<T>.Pop()
        {
            return this.Pop();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:Enumerator"/> that can be used to iterate through the collection.
        /// </returns>
        [Pure]
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        [Pure]
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new EnumeratorObject(this);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        [Pure]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new EnumeratorObject(this);
        }

        /// <summary>
        /// Reverses the order of a stack.
        /// </summary>
        /// <returns>The reversed stack.</returns>
        [Pure]
        internal ImmutableStack<T> Reverse()
        {
            Contract.Ensures(Contract.Result<ImmutableStack<T>>() != null);
            Contract.Ensures(Contract.Result<ImmutableStack<T>>().IsEmpty == this.IsEmpty);

            var r = this.Clear();
            for (ImmutableStack<T> f = this; !f.IsEmpty; f = f.Pop())
            {
                r = r.Push(f.Peek());
            }

            return r;
        }

        /// <summary>
        /// Enumerates a stack with no memory allocations.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public struct Enumerator
        {
            /// <summary>
            /// The original stack being enumerated.
            /// </summary>
            private readonly ImmutableStack<T> originalStack;

            /// <summary>
            /// The remaining stack not yet enumerated.
            /// </summary>
            private ImmutableStack<T> remainingStack;

            /// <summary>
            /// Initializes a new instance of the <see cref="Enumerator"/> struct.
            /// </summary>
            /// <param name="stack">The stack to enumerator.</param>
            internal Enumerator(ImmutableStack<T> stack)
            {
                Requires.NotNull(stack, "stack");
                this.originalStack = stack;
                this.remainingStack = null;
            }

            /// <summary>
            /// Gets the current element.
            /// </summary>
            public T Current
            {
                get
                {
                    if (this.remainingStack == null || this.remainingStack.IsEmpty)
                    {
                        throw new InvalidOperationException();
                    }
                    else
                    {
                        return this.remainingStack.Peek();
                    }
                }
            }

            /// <summary>
            /// Moves to the first or next element.
            /// </summary>
            /// <returns>A value indicating whether there are any more elements.</returns>
            public bool MoveNext()
            {
                if (this.remainingStack == null)
                {
                    // initial move
                    this.remainingStack = this.originalStack;
                }
                else if (!this.remainingStack.IsEmpty)
                {
                    this.remainingStack = this.remainingStack.Pop();
                }

                return !this.remainingStack.IsEmpty;
            }
        }

        /// <summary>
        /// Enumerates a stack with no memory allocations.
        /// </summary>
        private class EnumeratorObject : IEnumerator<T>
        {
            /// <summary>
            /// The original stack being enumerated.
            /// </summary>
            private readonly ImmutableStack<T> originalStack;

            /// <summary>
            /// The remaining stack not yet enumerated.
            /// </summary>
            private ImmutableStack<T> remainingStack;

            /// <summary>
            /// A flag indicating whether this enumerator has been disposed.
            /// </summary>
            private bool disposed;

            /// <summary>
            /// Initializes a new instance of the <see cref="EnumeratorObject"/> class.
            /// </summary>
            /// <param name="stack">The stack to enumerator.</param>
            internal EnumeratorObject(ImmutableStack<T> stack)
            {
                Requires.NotNull(stack, "stack");
                this.originalStack = stack;
            }

            /// <summary>
            /// Gets the current element.
            /// </summary>
            public T Current
            {
                get
                {
                    this.ThrowIfDisposed();
                    if (this.remainingStack == null || this.remainingStack.IsEmpty)
                    {
                        throw new InvalidOperationException();
                    }
                    else
                    {
                        return this.remainingStack.Peek();
                    }
                }
            }

            /// <summary>
            /// Gets the current element.
            /// </summary>
            object IEnumerator.Current
            {
                get { return this.Current; }
            }

            /// <summary>
            /// Moves to the first or next element.
            /// </summary>
            /// <returns>A value indicating whether there are any more elements.</returns>
            public bool MoveNext()
            {
                this.ThrowIfDisposed();

                if (this.remainingStack == null)
                {
                    // initial move
                    this.remainingStack = this.originalStack;
                }
                else if (!this.remainingStack.IsEmpty)
                {
                    this.remainingStack = this.remainingStack.Pop();
                }

                return !this.remainingStack.IsEmpty;
            }

            /// <summary>
            /// Resets the position to just before the first element in the list.
            /// </summary>
            public void Reset()
            {
                this.ThrowIfDisposed();
                this.remainingStack = null;
            }

            /// <summary>
            /// Disposes this instance.
            /// </summary>
            public void Dispose()
            {
                this.disposed = true;
            }

            /// <summary>
            /// Throws an <see cref="ObjectDisposedException"/> if this 
            /// enumerator has already been disposed.
            /// </summary>
            private void ThrowIfDisposed()
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }
            }
        }

        /// <summary>
        /// A simple view of the immutable collection that the debugger can show to the developer.
        /// </summary>
        [ExcludeFromCodeCoverage]
        private class DebuggerProxy
        {
            /// <summary>
            /// The collection to be enumerated.
            /// </summary>
            private readonly ImmutableStack<T> stack;

            /// <summary>
            /// The simple view of the collection.
            /// </summary>
            private T[] contents;

            /// <summary>   
            /// Initializes a new instance of the <see cref="DebuggerProxy"/> class.
            /// </summary>
            /// <param name="stack">The collection to display in the debugger</param>
            public DebuggerProxy(ImmutableStack<T> stack)
            {
                Requires.NotNull(stack, "stack");
                this.stack = stack;
            }

            /// <summary>
            /// Gets a simple debugger-viewable collection.
            /// </summary>
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public T[] Contents
            {
                get
                {
                    if (this.contents == null)
                    {
                        this.contents = this.stack.ToArray();
                    }

                    return this.contents;
                }
            }
        }
    }
}
