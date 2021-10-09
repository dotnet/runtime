// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;

namespace System.Collections.Immutable
{
    public sealed partial class ImmutableStack<T>
    {
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
            private ImmutableStack<T>? _remainingStack;

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
        private sealed class EnumeratorObject : IEnumerator<T>
        {
            /// <summary>
            /// The original stack being enumerated.
            /// </summary>
            private readonly ImmutableStack<T> _originalStack;

            /// <summary>
            /// The remaining stack not yet enumerated.
            /// </summary>
            private ImmutableStack<T>? _remainingStack;

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
            object? IEnumerator.Current
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
}
