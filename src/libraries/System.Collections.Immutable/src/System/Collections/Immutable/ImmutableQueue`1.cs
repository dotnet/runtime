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
    /// An immutable queue.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the queue.</typeparam>
    [DebuggerDisplay("IsEmpty = {IsEmpty}")]
    [DebuggerTypeProxy(typeof(ImmutableQueueDebuggerProxy<>))]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Justification = "Ignored")]
    public sealed class ImmutableQueue<T> : IImmutableQueue<T>
    {
        /// <summary>
        /// The singleton empty queue.
        /// </summary>
        /// <remarks>
        /// Additional instances representing the empty queue may exist on deserialized instances.
        /// Actually since this queue is a struct, instances don't even apply and there are no singletons.
        /// </remarks>
        private static readonly ImmutableQueue<T> s_EmptyField = new ImmutableQueue<T>(ImmutableStack<T>.Empty, ImmutableStack<T>.Empty);

        /// <summary>
        /// The end of the queue that enqueued elements are pushed onto.
        /// </summary>
        private readonly ImmutableStack<T> _backwards;

        /// <summary>
        /// The end of the queue from which elements are dequeued.
        /// </summary>
        private readonly ImmutableStack<T> _forwards;

        /// <summary>
        /// Backing field for the <see cref="BackwardsReversed"/> property.
        /// </summary>
        private ImmutableStack<T> _backwardsReversed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableQueue{T}"/> class.
        /// </summary>
        /// <param name="forward">The forward stack.</param>
        /// <param name="backward">The backward stack.</param>
        private ImmutableQueue(ImmutableStack<T> forward, ImmutableStack<T> backward)
        {
            Requires.NotNull(forward, nameof(forward));
            Requires.NotNull(backward, nameof(backward));

            _forwards = forward;
            _backwards = backward;
            _backwardsReversed = null;
        }

        /// <summary>
        /// Gets the empty queue.
        /// </summary>
        public ImmutableQueue<T> Clear()
        {
            Contract.Ensures(Contract.Result<ImmutableQueue<T>>().IsEmpty);
            Contract.Assume(s_EmptyField.IsEmpty);
            return Empty;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is empty.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is empty; otherwise, <c>false</c>.
        /// </value>
        public bool IsEmpty
        {
            get { return _forwards.IsEmpty && _backwards.IsEmpty; }
        }

        /// <summary>
        /// Gets the empty queue.
        /// </summary>
        public static ImmutableQueue<T> Empty
        {
            get
            {
                Contract.Ensures(Contract.Result<ImmutableQueue<T>>().IsEmpty);
                Contract.Assume(s_EmptyField.IsEmpty);
                return s_EmptyField;
            }
        }

        /// <summary>
        /// Gets an empty queue.
        /// </summary>
        IImmutableQueue<T> IImmutableQueue<T>.Clear()
        {
            Contract.Assume(s_EmptyField.IsEmpty);
            return this.Clear();
        }

        /// <summary>
        /// Gets the reversed <see cref="_backwards"/> stack.
        /// </summary>
        private ImmutableStack<T> BackwardsReversed
        {
            get
            {
                Contract.Ensures(Contract.Result<ImmutableStack<T>>() != null);

                // Although this is a lazy-init pattern, no lock is required because
                // this instance is immutable otherwise, and a double-assignment from multiple
                // threads is harmless.
                if (_backwardsReversed == null)
                {
                    _backwardsReversed = _backwards.Reverse();
                }

                return _backwardsReversed;
            }
        }

        /// <summary>
        /// Gets the element at the front of the queue.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the queue is empty.</exception>
        [Pure]
        public T Peek()
        {
            if (this.IsEmpty)
            {
                throw new InvalidOperationException(SR.InvalidEmptyOperation);
            }

            return _forwards.Peek();
        }

        /// <summary>
        /// Adds an element to the back of the queue.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The new queue.
        /// </returns>
        [Pure]
        public ImmutableQueue<T> Enqueue(T value)
        {
            Contract.Ensures(!Contract.Result<ImmutableQueue<T>>().IsEmpty);

            if (this.IsEmpty)
            {
                return new ImmutableQueue<T>(ImmutableStack<T>.Empty.Push(value), ImmutableStack<T>.Empty);
            }
            else
            {
                return new ImmutableQueue<T>(_forwards, _backwards.Push(value));
            }
        }

        /// <summary>
        /// Adds an element to the back of the queue.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The new queue.
        /// </returns>
        [Pure]
        IImmutableQueue<T> IImmutableQueue<T>.Enqueue(T value)
        {
            return this.Enqueue(value);
        }

        /// <summary>
        /// Returns a queue that is missing the front element.
        /// </summary>
        /// <returns>A queue; never <c>null</c>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the queue is empty.</exception>
        [Pure]
        public ImmutableQueue<T> Dequeue()
        {
            if (this.IsEmpty)
            {
                throw new InvalidOperationException(SR.InvalidEmptyOperation);
            }

            ImmutableStack<T> f = _forwards.Pop();
            if (!f.IsEmpty)
            {
                return new ImmutableQueue<T>(f, _backwards);
            }
            else if (_backwards.IsEmpty)
            {
                return ImmutableQueue<T>.Empty;
            }
            else
            {
                return new ImmutableQueue<T>(this.BackwardsReversed, ImmutableStack<T>.Empty);
            }
        }

        /// <summary>
        /// Retrieves the item at the head of the queue, and returns a queue with the head element removed.
        /// </summary>
        /// <param name="value">Receives the value from the head of the queue.</param>
        /// <returns>The new queue with the head element removed.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the queue is empty.</exception>
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "0#")]
        [Pure]
        public ImmutableQueue<T> Dequeue(out T value)
        {
            value = this.Peek();
            return this.Dequeue();
        }

        /// <summary>
        /// Returns a queue that is missing the front element.
        /// </summary>
        /// <returns>A queue; never <c>null</c>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the queue is empty.</exception>
        [Pure]
        IImmutableQueue<T> IImmutableQueue<T>.Dequeue()
        {
            return this.Dequeue();
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
        /// A memory allocation-free enumerator of <see cref="ImmutableQueue{T}"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public struct Enumerator
        {
            /// <summary>
            /// The original queue being enumerated.
            /// </summary>
            private readonly ImmutableQueue<T> _originalQueue;

            /// <summary>
            /// The remaining forwards stack of the queue being enumerated.
            /// </summary>
            private ImmutableStack<T> _remainingForwardsStack;

            /// <summary>
            /// The remaining backwards stack of the queue being enumerated.
            /// Its order is reversed when the field is first initialized.
            /// </summary>
            private ImmutableStack<T> _remainingBackwardsStack;

            /// <summary>
            /// Initializes a new instance of the <see cref="Enumerator"/> struct.
            /// </summary>
            /// <param name="queue">The queue to enumerate.</param>
            internal Enumerator(ImmutableQueue<T> queue)
            {
                _originalQueue = queue;

                // The first call to MoveNext will initialize these.
                _remainingForwardsStack = null;
                _remainingBackwardsStack = null;
            }

            /// <summary>
            /// The current element.
            /// </summary>
            public T Current
            {
                get
                {
                    if (_remainingForwardsStack == null)
                    {
                        // The initial call to MoveNext has not yet been made.
                        throw new InvalidOperationException();
                    }

                    if (!_remainingForwardsStack.IsEmpty)
                    {
                        return _remainingForwardsStack.Peek();
                    }
                    else if (!_remainingBackwardsStack.IsEmpty)
                    {
                        return _remainingBackwardsStack.Peek();
                    }
                    else
                    {
                        // We've advanced beyond the end of the queue.
                        throw new InvalidOperationException();
                    }
                }
            }

            /// <summary>
            /// Advances enumeration to the next element.
            /// </summary>
            /// <returns>A value indicating whether there is another element in the enumeration.</returns>
            public bool MoveNext()
            {
                if (_remainingForwardsStack == null)
                {
                    // This is the initial step.
                    // Empty queues have no forwards or backwards 
                    _remainingForwardsStack = _originalQueue._forwards;
                    _remainingBackwardsStack = _originalQueue.BackwardsReversed;
                }
                else if (!_remainingForwardsStack.IsEmpty)
                {
                    _remainingForwardsStack = _remainingForwardsStack.Pop();
                }
                else if (!_remainingBackwardsStack.IsEmpty)
                {
                    _remainingBackwardsStack = _remainingBackwardsStack.Pop();
                }

                return !_remainingForwardsStack.IsEmpty || !_remainingBackwardsStack.IsEmpty;
            }
        }

        /// <summary>
        /// A memory allocation-free enumerator of <see cref="ImmutableQueue{T}"/>.
        /// </summary>
        private class EnumeratorObject : IEnumerator<T>
        {
            /// <summary>
            /// The original queue being enumerated.
            /// </summary>
            private readonly ImmutableQueue<T> _originalQueue;

            /// <summary>
            /// The remaining forwards stack of the queue being enumerated.
            /// </summary>
            private ImmutableStack<T> _remainingForwardsStack;

            /// <summary>
            /// The remaining backwards stack of the queue being enumerated.
            /// Its order is reversed when the field is first initialized.
            /// </summary>
            private ImmutableStack<T> _remainingBackwardsStack;

            /// <summary>
            /// A value indicating whether this enumerator has been disposed.
            /// </summary>
            private bool _disposed;

            /// <summary>
            /// Initializes a new instance of the <see cref="Enumerator"/> struct.
            /// </summary>
            /// <param name="queue">The queue to enumerate.</param>
            internal EnumeratorObject(ImmutableQueue<T> queue)
            {
                _originalQueue = queue;
            }

            /// <summary>
            /// The current element.
            /// </summary>
            public T Current
            {
                get
                {
                    this.ThrowIfDisposed();
                    if (_remainingForwardsStack == null)
                    {
                        // The initial call to MoveNext has not yet been made.
                        throw new InvalidOperationException();
                    }

                    if (!_remainingForwardsStack.IsEmpty)
                    {
                        return _remainingForwardsStack.Peek();
                    }
                    else if (!_remainingBackwardsStack.IsEmpty)
                    {
                        return _remainingBackwardsStack.Peek();
                    }
                    else
                    {
                        // We've advanced beyond the end of the queue.
                        throw new InvalidOperationException();
                    }
                }
            }

            /// <summary>
            /// The current element.
            /// </summary>
            object IEnumerator.Current
            {
                get { return this.Current; }
            }

            /// <summary>
            /// Advances enumeration to the next element.
            /// </summary>
            /// <returns>A value indicating whether there is another element in the enumeration.</returns>
            public bool MoveNext()
            {
                this.ThrowIfDisposed();
                if (_remainingForwardsStack == null)
                {
                    // This is the initial step.
                    // Empty queues have no forwards or backwards 
                    _remainingForwardsStack = _originalQueue._forwards;
                    _remainingBackwardsStack = _originalQueue.BackwardsReversed;
                }
                else if (!_remainingForwardsStack.IsEmpty)
                {
                    _remainingForwardsStack = _remainingForwardsStack.Pop();
                }
                else if (!_remainingBackwardsStack.IsEmpty)
                {
                    _remainingBackwardsStack = _remainingBackwardsStack.Pop();
                }

                return !_remainingForwardsStack.IsEmpty || !_remainingBackwardsStack.IsEmpty;
            }

            /// <summary>
            /// Restarts enumeration.
            /// </summary>
            public void Reset()
            {
                this.ThrowIfDisposed();
                _remainingBackwardsStack = null;
                _remainingForwardsStack = null;
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
    internal class ImmutableQueueDebuggerProxy<T>
    {
        /// <summary>
        /// The collection to be enumerated.
        /// </summary>
        private readonly ImmutableQueue<T> _queue;

        /// <summary>
        /// The simple view of the collection.
        /// </summary>
        private T[] _contents;

        /// <summary>   
        /// Initializes a new instance of the <see cref="ImmutableQueueDebuggerProxy{T}"/> class.
        /// </summary>
        /// <param name="queue">The collection to display in the debugger</param>
        public ImmutableQueueDebuggerProxy(ImmutableQueue<T> queue)
        {
            _queue = queue;
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
                    _contents = _queue.ToArray();
                }

                return _contents;
            }
        }
    }
}
