// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;

namespace System.Collections.Immutable
{
    /// <summary>
    /// An immutable stack.
    /// </summary>
    /// <typeparam name="T">The type of element stored by the stack.</typeparam>
    [DebuggerDisplay("IsEmpty = {IsEmpty}; Top = {_head}")]
    [DebuggerTypeProxy(typeof(ImmutableStackDebuggerProxy<>))]
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
        private static readonly ImmutableStack<T> s_EmptyField = new ImmutableStack<T>();

        /// <summary>
        /// The element on the top of the stack.
        /// </summary>
        private readonly T _head;

        /// <summary>
        /// A stack that contains the rest of the elements (under the top element).
        /// </summary>
        private readonly ImmutableStack<T> _tail;

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
            Requires.NotNull(tail, nameof(tail));
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
                Contract.Ensures(Contract.Result<ImmutableStack<T>>() != null);
                Contract.Ensures(Contract.Result<ImmutableStack<T>>().IsEmpty);
                Contract.Assume(s_EmptyField.IsEmpty);
                return s_EmptyField;
            }
        }

        /// <summary>
        /// Gets the empty stack, upon which all stacks are built.
        /// </summary>
        public ImmutableStack<T> Clear()
        {
            Contract.Ensures(Contract.Result<ImmutableStack<T>>() != null);
            Contract.Ensures(Contract.Result<ImmutableStack<T>>().IsEmpty);
            Contract.Assume(s_EmptyField.IsEmpty);
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
        [Pure]
        public T Peek()
        {
            if (this.IsEmpty)
            {
                throw new InvalidOperationException(SR.InvalidEmptyOperation);
            }

            return _head;
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
                throw new InvalidOperationException(SR.InvalidEmptyOperation);
            }

            return _tail;
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
        /// An <see cref="Enumerator"/> that can be used to iterate through the collection.
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
        /// A <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.
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
        /// An <see cref="IEnumerator"/> object that can be used to iterate through the collection.
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
            private readonly ImmutableStack<T> _originalStack;

            /// <summary>
            /// The remaining stack not yet enumerated.
            /// </summary>
            private ImmutableStack<T> _remainingStack;

            /// <summary>
            /// Initializes a new instance of the <see cref="Enumerator"/> struct.
            /// </summary>
            /// <param name="stack">The stack to enumerator.</param>
            internal Enumerator(ImmutableStack<T> stack)
            {
                Requires.NotNull(stack, nameof(stack));
                _originalStack = stack;
                _remainingStack = null;
            }

            /// <summary>
            /// Gets the current element.
            /// </summary>
            public T Current
            {
                get
                {
                    if (_remainingStack == null || _remainingStack.IsEmpty)
                    {
                        throw new InvalidOperationException();
                    }
                    else
                    {
                        return _remainingStack.Peek();
                    }
                }
            }

            /// <summary>
            /// Moves to the first or next element.
            /// </summary>
            /// <returns>A value indicating whether there are any more elements.</returns>
            public bool MoveNext()
            {
                if (_remainingStack == null)
                {
                    // initial move
                    _remainingStack = _originalStack;
                }
                else if (!_remainingStack.IsEmpty)
                {
                    _remainingStack = _remainingStack.Pop();
                }

                return !_remainingStack.IsEmpty;
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
            private readonly ImmutableStack<T> _originalStack;

            /// <summary>
            /// The remaining stack not yet enumerated.
            /// </summary>
            private ImmutableStack<T> _remainingStack;

            /// <summary>
            /// A flag indicating whether this enumerator has been disposed.
            /// </summary>
            private bool _disposed;

            /// <summary>
            /// Initializes a new instance of the <see cref="EnumeratorObject"/> class.
            /// </summary>
            /// <param name="stack">The stack to enumerator.</param>
            internal EnumeratorObject(ImmutableStack<T> stack)
            {
                Requires.NotNull(stack, nameof(stack));
                _originalStack = stack;
            }

            /// <summary>
            /// Gets the current element.
            /// </summary>
            public T Current
            {
                get
                {
                    this.ThrowIfDisposed();
                    if (_remainingStack == null || _remainingStack.IsEmpty)
                    {
                        throw new InvalidOperationException();
                    }
                    else
                    {
                        return _remainingStack.Peek();
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

                if (_remainingStack == null)
                {
                    // initial move
                    _remainingStack = _originalStack;
                }
                else if (!_remainingStack.IsEmpty)
                {
                    _remainingStack = _remainingStack.Pop();
                }

                return !_remainingStack.IsEmpty;
            }

            /// <summary>
            /// Resets the position to just before the first element in the list.
            /// </summary>
            public void Reset()
            {
                this.ThrowIfDisposed();
                _remainingStack = null;
            }

            /// <summary>
            /// Disposes this instance.
            /// </summary>
            public void Dispose()
            {
                _disposed = true;
            }

            /// <summary>
            /// Throws an <see cref="ObjectDisposedException"/> if this 
            /// enumerator has already been disposed.
            /// </summary>
            private void ThrowIfDisposed()
            {
                if (_disposed)
                {
                    Requires.FailObjectDisposed(this);
                }
            }
        }
    }

    /// <summary>
    /// A simple view of the immutable collection that the debugger can show to the developer.
    /// </summary>
    internal class ImmutableStackDebuggerProxy<T>
    {
        /// <summary>
        /// The collection to be enumerated.
        /// </summary>
        private readonly ImmutableStack<T> _stack;

        /// <summary>
        /// The simple view of the collection.
        /// </summary>
        private T[] _contents;

        /// <summary>   
        /// Initializes a new instance of the <see cref="ImmutableStackDebuggerProxy{T}"/> class.
        /// </summary>
        /// <param name="stack">The collection to display in the debugger</param>
        public ImmutableStackDebuggerProxy(ImmutableStack<T> stack)
        {
            Requires.NotNull(stack, nameof(stack));
            _stack = stack;
        }

        /// <summary>
        /// Gets a simple debugger-viewable collection.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Contents
        {
            get
            {
                if (_contents == null)
                {
                    _contents = _stack.ToArray();
                }

                return _contents;
            }
        }
    }
}
