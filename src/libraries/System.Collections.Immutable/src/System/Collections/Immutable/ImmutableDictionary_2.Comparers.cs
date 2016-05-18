// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Collections.Immutable
{
    /// <content>
    /// Contains the inner <see cref="ImmutableDictionary{TKey, TValue}.Comparers"/> class.
    /// </content>
    public sealed partial class ImmutableDictionary<TKey, TValue>
    {
        /// <summary>
        /// A shareable container for the comparers used by an immutable dictionary.
        /// </summary>
        /// <remarks>
        /// To reduce allocations, we directly implement the <see cref="HashBucket"/> and Key-Only comparers,
        /// but we try to keep this an implementation detail by exposing properties that return
        /// references for these particular facilities, that are implemented as returning "this".
        /// </remarks>
        internal class Comparers : IEqualityComparer<HashBucket>, IEqualityComparer<KeyValuePair<TKey, TValue>>
        {
            /// <summary>
            /// The default instance to use when all the comparers used are their default values.
            /// </summary>
            internal static readonly Comparers Default = new Comparers(EqualityComparer<TKey>.Default, EqualityComparer<TValue>.Default);

            /// <summary>
            /// The equality comparer to use for the key.
            /// </summary>
            private readonly IEqualityComparer<TKey> _keyComparer;

            /// <summary>
            /// The value comparer.
            /// </summary>
            private readonly IEqualityComparer<TValue> _valueComparer;

            /// <summary>
            /// Initializes a new instance of the <see cref="Comparers"/> class.
            /// </summary>
            /// <param name="keyComparer">The key only comparer.</param>
            /// <param name="valueComparer">The value comparer.</param>
            internal Comparers(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
            {
                Requires.NotNull(keyComparer, nameof(keyComparer));
                Requires.NotNull(valueComparer, nameof(valueComparer));

                _keyComparer = keyComparer;
                _valueComparer = valueComparer;
            }

            /// <summary>
            /// Gets the key comparer.
            /// </summary>
            /// <value>
            /// The key comparer.
            /// </value>
            internal IEqualityComparer<TKey> KeyComparer
            {
                get { return _keyComparer; }
            }

            /// <summary>
            /// Gets the key only comparer.
            /// </summary>
            /// <value>
            /// The key only comparer.
            /// </value>
            internal IEqualityComparer<KeyValuePair<TKey, TValue>> KeyOnlyComparer
            {
                get { return this; }
            }

            /// <summary>
            /// Gets the value comparer.
            /// </summary>
            /// <value>
            /// The value comparer.
            /// </value>
            internal IEqualityComparer<TValue> ValueComparer
            {
                get { return _valueComparer; }
            }

            /// <summary>
            /// Gets the equality comparer to use with hash buckets.
            /// </summary>
            internal IEqualityComparer<HashBucket> HashBucketEqualityComparer
            {
                get { return this; }
            }

            /// <summary>
            /// Determines whether the specified objects are equal.
            /// </summary>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            /// <returns>
            /// true if the specified objects are equal; otherwise, false.
            /// </returns>
            public bool Equals(HashBucket x, HashBucket y)
            {
                return object.ReferenceEquals(x.AdditionalElements, y.AdditionalElements)
                    && this.KeyComparer.Equals(x.FirstValue.Key, y.FirstValue.Key)
                    && this.ValueComparer.Equals(x.FirstValue.Value, y.FirstValue.Value);
            }

            /// <summary>
            /// Returns a hash code for this instance.
            /// </summary>
            /// <param name="obj">The obj.</param>
            /// <returns>
            /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
            /// </returns>
            public int GetHashCode(HashBucket obj)
            {
                return this.KeyComparer.GetHashCode(obj.FirstValue.Key);
            }

            /// <summary>
            /// Determines whether the specified objects are equal.
            /// </summary>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            /// <returns>
            /// true if the specified objects are equal; otherwise, false.
            /// </returns>
            bool IEqualityComparer<KeyValuePair<TKey, TValue>>.Equals(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
            {
                return _keyComparer.Equals(x.Key, y.Key);
            }

            /// <summary>
            /// Returns a hash code for this instance.
            /// </summary>
            /// <param name="obj">The obj.</param>
            /// <returns>
            /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
            /// </returns>
            int IEqualityComparer<KeyValuePair<TKey, TValue>>.GetHashCode(KeyValuePair<TKey, TValue> obj)
            {
                return _keyComparer.GetHashCode(obj.Key);
            }

            /// <summary>
            /// Gets an instance that refers to the specified combination of comparers.
            /// </summary>
            /// <param name="keyComparer">The key comparer.</param>
            /// <param name="valueComparer">The value comparer.</param>
            /// <returns>An instance of <see cref="Comparers"/></returns>
            internal static Comparers Get(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
            {
                Requires.NotNull(keyComparer, nameof(keyComparer));
                Requires.NotNull(valueComparer, nameof(valueComparer));

                return keyComparer == Default.KeyComparer && valueComparer == Default.ValueComparer
                    ? Default
                    : new Comparers(keyComparer, valueComparer);
            }

            /// <summary>
            /// Returns an instance of <see cref="Comparers"/> that shares the same key comparers
            /// with this instance, but uses the specified value comparer.
            /// </summary>
            /// <param name="valueComparer">The new value comparer to use.</param>
            /// <returns>A new instance of <see cref="Comparers"/></returns>
            internal Comparers WithValueComparer(IEqualityComparer<TValue> valueComparer)
            {
                Requires.NotNull(valueComparer, nameof(valueComparer));

                return _valueComparer == valueComparer
                    ? this
                    : Get(this.KeyComparer, valueComparer);
            }
        }
    }
}
