// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Validation;

namespace System.Collections.Immutable
{
    /// <content>
    /// Contains the inner HashBucket struct.
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
        internal struct HashBucket
        {
            /// <summary>
            /// One of the values in this bucket.
            /// </summary>
            private readonly T firstValue;

            /// <summary>
            /// Any other elements that hash to the same value.
            /// </summary>
            /// <value>
            /// This is null if and only if the entire bucket is empty (including <see cref="firstValue"/>).  
            /// It's empty if <see cref="firstValue"/> has an element but no additional elements.
            /// </value>
            private readonly ImmutableList<T>.Node additionalElements;

            /// <summary>
            /// Initializes a new instance of the <see cref="HashBucket"/> struct.
            /// </summary>
            /// <param name="firstElement">The first element.</param>
            /// <param name="additionalElements">The additional elements.</param>
            private HashBucket(T firstElement, ImmutableList<T>.Node additionalElements = null)
            {
                this.firstValue = firstElement;
                this.additionalElements = additionalElements ?? ImmutableList<T>.Node.EmptyNode;
            }

            /// <summary>
            /// Gets a value indicating whether this instance is empty.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this instance is empty; otherwise, <c>false</c>.
            /// </value>
            internal bool IsEmpty
            {
                get { return this.additionalElements == null; }
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            public Enumerator GetEnumerator()
            {
                return new Enumerator(this);
            }

            /// <summary>
            /// Adds the specified value.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <param name="valueComparer">The value comparer.</param>
            /// <param name="result">A description of the effect was on adding an element to this HashBucket.</param>
            /// <returns>A new HashBucket that contains the added value and any values already held by this hashbucket.</returns>
            internal HashBucket Add(T value, IEqualityComparer<T> valueComparer, out OperationResult result)
            {
                if (this.IsEmpty)
                {
                    result = OperationResult.SizeChanged;
                    return new HashBucket(value);
                }

                if (valueComparer.Equals(value, this.firstValue) || this.additionalElements.IndexOf(value, valueComparer) >= 0)
                {
                    result = OperationResult.NoChangeRequired;
                    return this;
                }

                result = OperationResult.SizeChanged;
                return new HashBucket(this.firstValue, this.additionalElements.Add(value));
            }

            /// <summary>
            /// Determines whether the HashBucket contains the specified value.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <param name="valueComparer">The value comparer.</param>
            internal bool Contains(T value, IEqualityComparer<T> valueComparer)
            {
                if (this.IsEmpty)
                {
                    return false;
                }

                return valueComparer.Equals(value, this.firstValue) || this.additionalElements.IndexOf(value, valueComparer) >= 0;
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
                    if (valueComparer.Equals(value, this.firstValue))
                    {
                        existingValue = this.firstValue;
                        return true;
                    }

                    int index = this.additionalElements.IndexOf(value, valueComparer);
                    if (index >= 0)
                    {
                        existingValue = this.additionalElements[index];
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
            /// <param name="result">A description of the effect was on adding an element to this HashBucket.</param>
            /// <returns>A new HashBucket that does not contain the removed value and any values already held by this hashbucket.</returns>
            internal HashBucket Remove(T value, IEqualityComparer<T> equalityComparer, out OperationResult result)
            {
                if (this.IsEmpty)
                {
                    result = OperationResult.NoChangeRequired;
                    return this;
                }

                if (equalityComparer.Equals(this.firstValue, value))
                {
                    if (this.additionalElements.IsEmpty)
                    {
                        result = OperationResult.SizeChanged;
                        return new HashBucket();
                    }
                    else
                    {
                        // We can promote any element from the list into the first position, but it's most efficient
                        // to remove the root node in the binary tree that implements the list.
                        int indexOfRootNode = ((IBinaryTree<T>)this.additionalElements).Left.Count;
                        result = OperationResult.SizeChanged;
                        return new HashBucket(this.additionalElements.Key, this.additionalElements.RemoveAt(indexOfRootNode));
                    }
                }

                int index = this.additionalElements.IndexOf(value, equalityComparer);
                if (index < 0)
                {
                    result = OperationResult.NoChangeRequired;
                    return this;
                }
                else
                {
                    result = OperationResult.SizeChanged;
                    return new HashBucket(this.firstValue, this.additionalElements.RemoveAt(index));
                }
            }

            /// <summary>
            /// Freezes this instance so that any further mutations require new memory allocations.
            /// </summary>
            internal void Freeze()
            {
                if (this.additionalElements != null)
                {
                    this.additionalElements.Freeze();
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
                private readonly HashBucket bucket;

                /// <summary>
                /// A value indicating whether this enumerator has been disposed.
                /// </summary>
                private bool disposed;

                /// <summary>
                /// The current position of this enumerator.
                /// </summary>
                private Position currentPosition;

                /// <summary>
                /// The enumerator that represents the current position over the additionalValues of the HashBucket.
                /// </summary>
                private ImmutableList<T>.Enumerator additionalEnumerator;

                /// <summary>
                /// Initializes a new instance of the <see cref="ImmutableHashSet&lt;T&gt;.HashBucket.Enumerator"/> struct.
                /// </summary>
                /// <param name="bucket">The bucket.</param>
                internal Enumerator(HashBucket bucket)
                {
                    this.disposed = false;
                    this.bucket = bucket;
                    this.currentPosition = Position.BeforeFirst;
                    this.additionalEnumerator = default(ImmutableList<T>.Enumerator);
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
                    /// We're at the firstValue of the containing bucket.
                    /// </summary>
                    First,

                    /// <summary>
                    /// We're enumerating the additionalValues in the bucket.
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
                object IEnumerator.Current
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
                        switch (this.currentPosition)
                        {
                            case Position.First:
                                return this.bucket.firstValue;
                            case Position.Additional:
                                return this.additionalEnumerator.Current;
                            default:
                                throw new InvalidOperationException();
                        }
                    }
                }

                /// <summary>
                /// Advances the enumerator to the next element of the collection.
                /// </summary>
                /// <returns>
                /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
                /// </returns>
                /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
                public bool MoveNext()
                {
                    this.ThrowIfDisposed();
                    if (this.bucket.IsEmpty)
                    {
                        this.currentPosition = Position.End;
                        return false;
                    }

                    switch (this.currentPosition)
                    {
                        case Position.BeforeFirst:
                            this.currentPosition = Position.First;
                            return true;
                        case Position.First:
                            if (this.bucket.additionalElements.IsEmpty)
                            {
                                this.currentPosition = Position.End;
                                return false;
                            }

                            this.currentPosition = Position.Additional;
                            this.additionalEnumerator = new ImmutableList<T>.Enumerator(this.bucket.additionalElements);
                            return this.additionalEnumerator.MoveNext();
                        case Position.Additional:
                            return this.additionalEnumerator.MoveNext();
                        case Position.End:
                            return false;
                        default:
                            throw new InvalidOperationException();
                    }
                }

                /// <summary>
                /// Sets the enumerator to its initial position, which is before the first element in the collection.
                /// </summary>
                /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
                public void Reset()
                {
                    this.ThrowIfDisposed();
                    this.additionalEnumerator.Dispose();
                    this.currentPosition = Position.BeforeFirst;
                }

                /// <summary>
                /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
                /// </summary>
                public void Dispose()
                {
                    this.disposed = true;
                    this.additionalEnumerator.Dispose();
                }

                /// <summary>
                /// Throws an ObjectDisposedException if this enumerator has been disposed.
                /// </summary>
                private void ThrowIfDisposed()
                {
                    if (this.disposed)
                    {
                        throw new ObjectDisposedException(this.GetType().FullName);
                    }
                }
            }
        }
    }
}
