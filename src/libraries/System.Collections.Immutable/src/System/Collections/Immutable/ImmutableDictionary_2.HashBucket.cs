// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Collections.Immutable
{
    /// <content>
    /// Contains the inner <see cref="ImmutableDictionary{TKey, TValue}.HashBucket"/> struct.
    /// </content>
    public partial class ImmutableDictionary<TKey, TValue>
    {
        /// <summary>
        /// Contains all the key/values in the collection that hash to the same value.
        /// </summary>
        internal readonly struct HashBucket : IEnumerable<KeyValuePair<TKey, TValue>>
        {
            /// <summary>
            /// One of the values in this bucket.
            /// </summary>
            private readonly KeyValuePair<TKey, TValue> _firstValue;

            /// <summary>
            /// Any other elements that hash to the same value.
            /// </summary>
            /// <value>
            /// This is null if and only if the entire bucket is empty (including <see cref="_firstValue"/>).
            /// It's empty if <see cref="_firstValue"/> has an element but no additional elements.
            /// </value>
            private readonly ImmutableList<KeyValuePair<TKey, TValue>>.Node _additionalElements;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImmutableDictionary{TKey, TValue}.HashBucket"/> struct.
            /// </summary>
            /// <param name="firstElement">The first element.</param>
            /// <param name="additionalElements">The additional elements.</param>
            private HashBucket(KeyValuePair<TKey, TValue> firstElement, ImmutableList<KeyValuePair<TKey, TValue>>.Node? additionalElements = null)
            {
                _firstValue = firstElement;
                _additionalElements = additionalElements ?? ImmutableList<KeyValuePair<TKey, TValue>>.Node.EmptyNode;
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
            /// Gets the first value in this bucket.
            /// </summary>
            internal KeyValuePair<TKey, TValue> FirstValue
            {
                get
                {
                    if (this.IsEmpty)
                    {
                        throw new InvalidOperationException();
                    }

                    return _firstValue;
                }
            }

            /// <summary>
            /// Gets the list of additional (hash collision) elements.
            /// </summary>
            internal ImmutableList<KeyValuePair<TKey, TValue>>.Node AdditionalElements
            {
                get { return _additionalElements; }
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
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
            IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            /// <summary>
            /// Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>
            /// An <see cref="IEnumerator"/> object that can be used to iterate through the collection.
            /// </returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
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
            /// Adds the specified key.
            /// </summary>
            /// <param name="key">The key to add.</param>
            /// <param name="value">The value to add.</param>
            /// <param name="keyOnlyComparer">The key comparer.</param>
            /// <param name="valueComparer">The value comparer.</param>
            /// <param name="behavior">The intended behavior for certain cases that may come up during the operation.</param>
            /// <param name="result">A description of the effect was on adding an element to this <see cref="HashBucket"/>.</param>
            /// <returns>A new <see cref="HashBucket"/> that contains the added value and any values already held by this <see cref="HashBucket"/>.</returns>
            internal HashBucket Add(TKey key, TValue value, IEqualityComparer<KeyValuePair<TKey, TValue>> keyOnlyComparer, IEqualityComparer<TValue> valueComparer, KeyCollisionBehavior behavior, out OperationResult result)
            {
                var kv = new KeyValuePair<TKey, TValue>(key, value);
                if (this.IsEmpty)
                {
                    result = OperationResult.SizeChanged;
                    return new HashBucket(kv);
                }

                if (keyOnlyComparer.Equals(kv, _firstValue))
                {
                    switch (behavior)
                    {
                        case KeyCollisionBehavior.SetValue:
                            result = OperationResult.AppliedWithoutSizeChange;
                            return new HashBucket(kv, _additionalElements);
                        case KeyCollisionBehavior.Skip:
                            result = OperationResult.NoChangeRequired;
                            return this;
                        case KeyCollisionBehavior.ThrowIfValueDifferent:
                            if (!valueComparer.Equals(_firstValue.Value, value))
                            {
                                throw new ArgumentException(SR.Format(SR.DuplicateKey, key));
                            }

                            result = OperationResult.NoChangeRequired;
                            return this;
                        case KeyCollisionBehavior.ThrowAlways:
                            throw new ArgumentException(SR.Format(SR.DuplicateKey, key));
                        default:
                            throw new InvalidOperationException(); // unreachable
                    }
                }

                int keyCollisionIndex = _additionalElements.IndexOf(kv, keyOnlyComparer);
                if (keyCollisionIndex < 0)
                {
                    result = OperationResult.SizeChanged;
                    return new HashBucket(_firstValue, _additionalElements.Add(kv));
                }
                else
                {
                    switch (behavior)
                    {
                        case KeyCollisionBehavior.SetValue:
                            result = OperationResult.AppliedWithoutSizeChange;
                            return new HashBucket(_firstValue, _additionalElements.ReplaceAt(keyCollisionIndex, kv));
                        case KeyCollisionBehavior.Skip:
                            result = OperationResult.NoChangeRequired;
                            return this;
                        case KeyCollisionBehavior.ThrowIfValueDifferent:
                            ref readonly var existingEntry = ref _additionalElements.ItemRef(keyCollisionIndex);
                            if (!valueComparer.Equals(existingEntry.Value, value))
                            {
                                throw new ArgumentException(SR.Format(SR.DuplicateKey, key));
                            }

                            result = OperationResult.NoChangeRequired;
                            return this;
                        case KeyCollisionBehavior.ThrowAlways:
                            throw new ArgumentException(SR.Format(SR.DuplicateKey, key));
                        default:
                            throw new InvalidOperationException(); // unreachable
                    }
                }
            }

            /// <summary>
            /// Removes the specified value if it exists in the collection.
            /// </summary>
            /// <param name="key">The key to remove.</param>
            /// <param name="keyOnlyComparer">The equality comparer.</param>
            /// <param name="result">A description of the effect was on adding an element to this <see cref="HashBucket"/>.</param>
            /// <returns>A new <see cref="HashBucket"/> that does not contain the removed value and any values already held by this <see cref="HashBucket"/>.</returns>
            internal HashBucket Remove(TKey key, IEqualityComparer<KeyValuePair<TKey, TValue>> keyOnlyComparer, out OperationResult result)
            {
                if (this.IsEmpty)
                {
                    result = OperationResult.NoChangeRequired;
                    return this;
                }

                var kv = new KeyValuePair<TKey, TValue>(key, default(TValue)!);
                if (keyOnlyComparer.Equals(_firstValue, kv))
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

                int index = _additionalElements.IndexOf(kv, keyOnlyComparer);
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
            /// Gets the value for the given key in the collection if one exists..
            /// </summary>
            /// <param name="key">The key to search for.</param>
            /// <param name="comparers">The comparers.</param>
            /// <param name="value">The value for the given key.</param>
            /// <returns>A value indicating whether the key was found.</returns>
            internal bool TryGetValue(TKey key, Comparers comparers, [MaybeNullWhen(false)] out TValue value)
            {
                if (this.IsEmpty)
                {
                    value = default;
                    return false;
                }

                if (comparers.KeyComparer.Equals(_firstValue.Key, key))
                {
                    value = _firstValue.Value;
                    return true;
                }

                var kv = new KeyValuePair<TKey, TValue>(key, default(TValue)!);
                var index = _additionalElements.IndexOf(kv, comparers.KeyOnlyComparer);
                if (index < 0)
                {
                    value = default;
                    return false;
                }

                value = _additionalElements.ItemRef(index).Value;
                return true;
            }

            /// <summary>
            /// Searches the dictionary for a given key and returns the equal key it finds, if any.
            /// </summary>
            /// <param name="equalKey">The key to search for.</param>
            /// <param name="comparers">The comparers.</param>
            /// <param name="actualKey">The key from the dictionary that the search found, or <paramref name="equalKey"/> if the search yielded no match.</param>
            /// <returns>A value indicating whether the search was successful.</returns>
            /// <remarks>
            /// This can be useful when you want to reuse a previously stored reference instead of
            /// a newly constructed one (so that more sharing of references can occur) or to look up
            /// the canonical value, or a value that has more complete data than the value you currently have,
            /// although their comparer functions indicate they are equal.
            /// </remarks>
            internal bool TryGetKey(TKey equalKey, Comparers comparers, out TKey actualKey)
            {
                if (this.IsEmpty)
                {
                    actualKey = equalKey;
                    return false;
                }

                if (comparers.KeyComparer.Equals(_firstValue.Key, equalKey))
                {
                    actualKey = _firstValue.Key;
                    return true;
                }

                var kv = new KeyValuePair<TKey, TValue>(equalKey, default(TValue)!);
                var index = _additionalElements.IndexOf(kv, comparers.KeyOnlyComparer);
                if (index < 0)
                {
                    actualKey = equalKey;
                    return false;
                }

                actualKey = _additionalElements.ItemRef(index).Key;
                return true;
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
            internal struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDisposable
            {
                /// <summary>
                /// The bucket being enumerated.
                /// </summary>
                private readonly HashBucket _bucket;

                /// <summary>
                /// The current position of this enumerator.
                /// </summary>
                private Position _currentPosition;

                /// <summary>
                /// The enumerator that represents the current position over the <see cref="_additionalElements"/> of the <see cref="HashBucket"/>.
                /// </summary>
                private ImmutableList<KeyValuePair<TKey, TValue>>.Enumerator _additionalEnumerator;

                /// <summary>
                /// Initializes a new instance of the <see cref="ImmutableDictionary{TKey, TValue}.HashBucket.Enumerator"/> struct.
                /// </summary>
                /// <param name="bucket">The bucket.</param>
                internal Enumerator(HashBucket bucket)
                {
                    _bucket = bucket;
                    _currentPosition = Position.BeforeFirst;
                    _additionalEnumerator = default(ImmutableList<KeyValuePair<TKey, TValue>>.Enumerator);
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
                object IEnumerator.Current
                {
                    get { return this.Current; }
                }

                /// <summary>
                /// Gets the current element.
                /// </summary>
                public KeyValuePair<TKey, TValue> Current
                {
                    get
                    {
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
                            _additionalEnumerator = new ImmutableList<KeyValuePair<TKey, TValue>>.Enumerator(_bucket._additionalElements);
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
                    // We can safely dispose of the additional enumerator because if the client reuses this enumerator
                    // we'll acquire a new one anyway (and so for that matter we should be sure to dispose of this).
                    _additionalEnumerator.Dispose();
                    _currentPosition = Position.BeforeFirst;
                }

                /// <summary>
                /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
                /// </summary>
                public void Dispose()
                {
                    _additionalEnumerator.Dispose();
                }
            }
        }
    }
}
