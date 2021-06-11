// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace System.Collections.Immutable
{
    public sealed partial class ImmutableQueue<T>
    {
        /// <summary>
        /// An immutable stack with lazy initialization.
        /// </summary>
        [DebuggerTypeProxy(typeof(ImmutableEnumerableDebuggerProxy<>))]
        [DebuggerDisplay("IsEmpty = {IsEmpty}; Top = {_head}")]
        internal class LazyStack : IImmutableStack<T>
        {
            /// <summary>
            /// The singleton empty stack.
            /// </summary>
            /// <remarks>
            /// Additional instances representing the empty stack may exist on deserialized stacks.
            /// </remarks>
            private static readonly LazyStack s_EmptyField = new LazyStack();

            /// <summary>
            /// The element on the top of the stack.
            /// </summary>
            private T? _head;

            /// <summary>
            /// A stack that contains the rest of the elements (under the top element).
            /// </summary>
            private LazyStack? _tail;


            /// <summary>
            /// A stack that contains the heads of the elements.
            /// </summary>
            private readonly LazyStack? _lazyHeads;

            /// <summary>
            /// A stack that contains the ends of the elements.
            /// </summary>
            private readonly ImmutableStack<T>? _lazyTails;

            /// <summary>
            /// A stack that contains the middle of the elements.
            /// </summary>
            private readonly LazyStack? _lazySchedule;

            /// <summary>
            /// Initializes a new instance of the <see cref="LazyStack"/> class
            /// that acts as the empty stack.
            /// </summary>
            private LazyStack()
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="LazyStack"/> class.
            /// </summary>
            /// <param name="head">The head element on the stack.</param>
            /// <param name="tail">The rest of the elements on the stack.</param>
            internal LazyStack(T head, LazyStack tail)
            {
                Debug.Assert(tail is not null);

                _head = head;
                _tail = tail;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="LazyStack"/> class with lazy initialization.
            /// </summary>
            internal LazyStack(LazyStack heads, ImmutableStack<T> tails, LazyStack schedule)
            {
                _lazyHeads = heads;
                _lazyTails = tails;
                _lazySchedule = schedule;
            }

            /// <summary>
            /// Gets the empty stack, upon which all stacks are built.
            /// </summary>
            public static LazyStack Empty
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
            public LazyStack Clear()
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
                get
                {
                    return _tail is null && _lazySchedule is null;
                }
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

                Rotate();
                return _head!;
            }

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

                Rotate();
                return ref _head!;
            }


            /// <summary>
            /// Pushes an element onto a stack and returns the new stack.
            /// </summary>
            /// <param name="value">The element to push onto the stack.</param>
            /// <returns>The new stack.</returns>
            public LazyStack Push(T value)
            {
                return new LazyStack(value, this);
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
            public LazyStack Pop()
            {
                if (this.IsEmpty)
                {
                    throw new InvalidOperationException(SR.InvalidEmptyOperation);
                }

                Rotate();
                Debug.Assert(_tail is not null);
                return _tail;
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
            /// Pops the top element off the stack.
            /// </summary>
            /// <param name="value">The value that was removed from the stack.</param>
            /// <returns>
            /// A stack; never <c>null</c>
            /// </returns>
            public LazyStack Pop(out T value)
            {
                Rotate();
                value = this.Peek();
                return this.Pop();
            }

            internal void Rotate()
            {
                if (_tail is not null || _lazySchedule is null)
                {
                    return;
                }
                Debug.Assert(_lazyHeads is not null);
                Debug.Assert(_lazyTails is not null);
                Debug.Assert(_lazySchedule is not null);

                if (_lazyHeads.IsEmpty)
                {
                    _head = _lazyTails.Peek();
                    _tail = _lazySchedule;
                }
                else
                {
                    var heads = _lazyHeads.Pop(out _head);
                    var tails = _lazyTails.Pop(out T tailValue);
                    var schedule = _lazySchedule.Push(tailValue);
                    _tail = new LazyStack(heads, tails, schedule);
                }
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// An <see cref="LazyStackEnumerator"/> that can be used to iterate through the collection.
            /// </returns>
            public LazyStackEnumerator GetEnumerator()
            {
                return new LazyStackEnumerator(this);
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
                    new LazyStackEnumeratorObject(this);
            }

            /// <summary>
            /// Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>
            /// An <see cref="IEnumerator"/> object that can be used to iterate through the collection.
            /// </returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return new LazyStackEnumeratorObject(this);
            }
        }
    }
}
