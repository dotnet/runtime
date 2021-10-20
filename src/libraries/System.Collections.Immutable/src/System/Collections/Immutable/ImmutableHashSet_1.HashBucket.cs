// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Immutable
{
    /// <content>
    /// Contains the inner <see cref="ImmutableHashSet{T}.HashBucket"/> struct.
    /// </content>
    public partial class ImmutableHashSet<T>
    {
        /// <summary>
        /// The result of a mutation operation.
        /// </summary>
        internal enum OperationResult
        {
            /// <summary>
            /// The change required element(s) to be added or removed from the collection.
            /// </summary>
            SizeChanged,

            /// <summary>
            /// No change was required (the operation ended in a no-op).
            /// </summary>
            NoChangeRequired,
        }

        /// <summary>
        /// Contains all the keys in the collection that hash to the same value.
        /// </summary>
        internal readonly struct HashBucket
        {
            /// <summary>
            /// One of the values in this bucket.
            /// </summary>
            private readonly T _firstValue;

            /// <summary>
            /// Any other elements that hash to the same value.
            /// </summary>
            /// <value>
            /// This is null if and only if the entire bucket is empty (including <see cref="_firstValue"/>).
            /// It's empty if <see cref="_firstValue"/> has an element but no additional elements.
            /// </value>
            private readonly ImmutableList<T>.Node _additionalElements;

            /// <summary>
            /// Initializes a new instance of the <see cref="HashBucket"/> struct.
            /// </summary>
            /// <param name="firstElement">The first element.</param>
            /// <param name="additionalElements">The additional elements.</param>
            private HashBucket(T firstElement, ImmutableList<T>.Node? additionalElements = null)
            {
                _firstValue = firstElement;
                _additionalElements = additionalElements ?? ImmutableList<T>.Node.EmptyNode;
            }

            /// <summary>
            /// Gets a value indicating whether this instance is empty.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this instance is empty; otherwise, <c>false</c>.
            /// </value>
            internal bool IsEmpty
            {
                get { return _additionalElements == null; }
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            public Enumerator GetEnumerator()
            {
                return new Enumerator(this);
            }

            /// <summary>
            /// Throws an exception to catch any errors in comparing <see cref="HashBucket"/> instances.
            /// </summary>
            public override bool Equals(object? obj)
            {
                // This should never be called, as hash buckets don't know how to equate themselves.
                throw new NotSupportedException();
            }

            /// <summary>
            /// Throws an exception to catch any errors in comparing <see cref="HashBucket"/> instances.
            /// </summary>
            public override int GetHashCode()
            {
                // This should never be called, as hash buckets don't know how to hash themselves.
                throw new NotSupportedException();
            }

            /// <summary>
            /// Checks whether this <see cref="HashBucket"/> is exactly like another one,
            /// comparing by reference. For use when type parameter T is an object.
            /// </summary>
            /// <param name="other">The other bucket.</param>
            /// <returns><c>true</c> if the two <see cref="HashBucket"/> structs have precisely the same values.</returns>
            internal bool EqualsByRef(HashBucket other)
            {
                return object.ReferenceEquals(_firstValue, other._firstValue)
                    && object.ReferenceEquals(_additionalElements, other._additionalElements);
            }

            /// <summary>
            /// Checks whether this <see cref="HashBucket"/> is exactly like another one,
            /// comparing by value. For use when type parameter T is a struct.
            /// </summary>
            /// <param name="other">The other bucket.</param>
            /// <param name="valueComparer">The comparer to use for the first value in the bucket.</param>
            /// <returns><c>true</c> if the two <see cref="HashBucket"/> structs have precisely the same values.</returns>
            internal bool EqualsByValue(HashBucket other, IEqualityComparer<T> valueComparer)
            {
                return valueComparer.Equals(_firstValue, other._firstValue)
                    && object.ReferenceEquals(_additionalElements, other._additionalElements);
            }

            /// <summary>
            /// Adds the specified value.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <param name="valueComparer">The value comparer.</param>
            /// <param name="result">A description of the effect was on adding an element to this <see cref="HashBucket"/>.</param>
            /// <returns>A new <see cref="HashBucket"/> that contains the added value and any values already held by this <see cref="HashBucket"/>.</returns>
            internal HashBucket Add(T value, IEqualityComparer<T> valueComparer, out OperationResult result)
            {
                if (this.IsEmpty)
                {
                    result = OperationResult.SizeChanged;
                    return new HashBucket(value);
                }

                if (valueComparer.Equals(value, _firstValue) || _additionalElements.IndexOf(value, valueComparer) >= 0)
                {
                    result = OperationResult.NoChangeRequired;
                    return this;
                }

                result = OperationResult.SizeChanged;
                return new HashBucket(_firstValue, _additionalElements.Add(value));
            }

            /// <summary>
            /// Determines whether the <see cref="HashBucket"/> contains the specified value.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <param name="valueComparer">The value comparer.</param>
            internal bool Contains(T value, IEqualityComparer<T> valueComparer)
            {
                if (this.IsEmpty)
                {
                    return false;
                }

                return valueComparer.Equals(value, _firstValue) || _additionalElements.IndexOf(value, valueComparer) >= 0;
            }

            /// <summary>
            /// Searches the set for a given value and returns the equal value it finds, if any.
            /// </summary>
            /// <param name="value">The value to search for.</param>
            /// <param name="valueComparer">The value comparer.</param>
            /// <param name="existingValue">The value from the set that the search found, or the original value if the search yielded no match.</param>
            /// <returns>
            /// A value indicating whether the search was successful.
            /// </returns>
            internal bool TryExchange(T value, IEqualityComparer<T> valueComparer, out T existingValue)
            {
                if (!this.IsEmpty)
                {
                    if (valueComparer.Equals(value, _firstValue))
                    {
                        existingValue = _firstValue;
                        return true;
                    }

                    int index = _additionalElements.IndexOf(value, valueComparer);
                    if (index >= 0)
                    {
                        existingValue = _additionalElements.ItemRef(index);
                        return true;
                    }
                }

                existingValue = value;
                return false;
            }

            /// <summary>
            /// Removes the specified value if it exists in the collection.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <param name="equalityComparer">The equality comparer.</param>
            /// <param name="result">A description of the effect was on adding an element to this <see cref="HashBucket"/>.</param>
            /// <returns>A new <see cref="HashBucket"/> that does not contain the removed value and any values already held by this <see cref="HashBucket"/>.</returns>
            internal HashBucket Remove(T value, IEqualityComparer<T> equalityComparer, out OperationResult result)
            {
                if (this.IsEmpty)
                {
                    result = OperationResult.NoChangeRequired;
                    return this;
                }

                if (equalityComparer.Equals(_firstValue, value))
                {
                    if (_additionalElements.IsEmpty)
                    {
                        result = OperationResult.SizeChanged;
                        return default;
                    }
                    else
                    {
                        // We can promote any element from the list into the first position, but it's most efficient
                        // to remove the root node in the binary tree that implements the list.
                        int indexOfRootNode = _additionalElements.Left!.Count;
                        result = OperationResult.SizeChanged;
                        return new HashBucket(_additionalElements.Key, _additionalElements.RemoveAt(indexOfRootNode));
                    }
                }

                int index = _additionalElements.IndexOf(value, equalityComparer);
                if (index < 0)
                {
                    result = OperationResult.NoChangeRequired;
                    return this;
                }
                else
                {
                    result = OperationResult.SizeChanged;
                    return new HashBucket(_firstValue, _additionalElements.RemoveAt(index));
                }
            }

            /// <summary>
            /// Freezes this instance so that any further mutations require new memory allocations.
            /// </summary>
            internal void Freeze()
            {
                if (_additionalElements != null)
                {
                    _additionalElements.Freeze();
                }
            }

            /// <summary>
            /// Enumerates all the elements in this instance.
            /// </summary>
            internal struct Enumerator : IEnumerator<T>, IDisposable
            {
                /// <summary>
                /// The bucket being enumerated.
                /// </summary>
                private readonly HashBucket _bucket;

                /// <summary>
                /// A value indicating whether this enumerator has been disposed.
                /// </summary>
                private bool _disposed;

                /// <summary>
                /// The current position of this enumerator.
                /// </summary>
                private Position _currentPosition;

                /// <summary>
                /// The enumerator that represents the current position over the <see cref="_additionalElements"/> of the <see cref="HashBucket"/>.
                /// </summary>
                private ImmutableList<T>.Enumerator _additionalEnumerator;

                /// <summary>
                /// Initializes a new instance of the <see cref="ImmutableHashSet{T}.HashBucket.Enumerator"/> struct.
                /// </summary>
                /// <param name="bucket">The bucket.</param>
                internal Enumerator(HashBucket bucket)
                {
                    _disposed = false;
                    _bucket = bucket;
                    _currentPosition = Position.BeforeFirst;
                    _additionalEnumerator = default(ImmutableList<T>.Enumerator);
                }

                /// <summary>
                /// Describes the positions the enumerator state machine may be in.
                /// </summary>
                private enum Position
                {
                    /// <summary>
                    /// The first element has not yet been moved to.
                    /// </summary>
                    BeforeFirst,

                    /// <summary>
                    /// We're at the <see cref="_firstValue"/> of the containing bucket.
                    /// </summary>
                    First,

                    /// <summary>
                    /// We're enumerating the <see cref="_additionalElements"/> in the bucket.
                    /// </summary>
                    Additional,

                    /// <summary>
                    /// The end of enumeration has been reached.
                    /// </summary>
                    End,
                }

                /// <summary>
                /// Gets the current element.
                /// </summary>
                object? IEnumerator.Current
                {
                    get { return this.Current; }
                }

                /// <summary>
                /// Gets the current element.
                /// </summary>
                public T Current
                {
                    get
                    {
                        this.ThrowIfDisposed();
                        return _currentPosition switch
                        {
                            Position.First => _bucket._firstValue,
                            Position.Additional => _additionalEnumerator.Current,
                            _ => throw new InvalidOperationException(),
                        };
                    }
                }

                /// <summary>
                /// Advances the enumerator to the next element of the collection.
                /// </summary>
                /// <returns>
                /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
                /// </returns>
                /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created. </exception>
                public bool MoveNext()
                {
                    this.ThrowIfDisposed();
                    if (_bucket.IsEmpty)
                    {
                        _currentPosition = Position.End;
                        return false;
                    }

                    switch (_currentPosition)
                    {
                        case Position.BeforeFirst:
                            _currentPosition = Position.First;
                            return true;
                        case Position.First:
                            if (_bucket._additionalElements.IsEmpty)
                            {
                                _currentPosition = Position.End;
                                return false;
                            }

                            _currentPosition = Position.Additional;
                            _additionalEnumerator = new ImmutableList<T>.Enumerator(_bucket._additionalElements);
                            return _additionalEnumerator.MoveNext();
                        case Position.Additional:
                            return _additionalEnumerator.MoveNext();
                        case Position.End:
                            return false;
                        default:
                            throw new InvalidOperationException();
                    }
                }

                /// <summary>
                /// Sets the enumerator to its initial position, which is before the first element in the collection.
                /// </summary>
                /// <exception cref="InvalidOperationException">The collection was modified after the enumerator was created. </exception>
                public void Reset()
                {
                    this.ThrowIfDisposed();
                    _additionalEnumerator.Dispose();
                    _currentPosition = Position.BeforeFirst;
                }

                /// <summary>
                /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
                /// </summary>
                public void Dispose()
                {
                    _disposed = true;
                    _additionalEnumerator.Dispose();
                }

                /// <summary>
                /// Throws an <see cref="ObjectDisposedException"/> if this enumerator has been disposed.
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
}
