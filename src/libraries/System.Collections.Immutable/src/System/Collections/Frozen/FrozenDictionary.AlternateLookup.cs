// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    public abstract partial class FrozenDictionary<TKey, TValue>
    {
        /// <summary>
        /// Gets an instance of a type that may be used to perform operations on a <see cref="FrozenDictionary{TKey, TValue}"/>
        /// using a <typeparamref name="TAlternateKey"/> as a key instead of a <typeparamref name="TKey"/>.
        /// </summary>
        /// <typeparam name="TAlternateKey">The alternate type of a key for performing lookups.</typeparam>
        /// <returns>The created lookup instance.</returns>
        /// <exception cref="InvalidOperationException">This instance's comparer is not compatible with <typeparamref name="TAlternateKey"/>.</exception>
        /// <remarks>
        /// This instance must be using a comparer that implements <see cref="IAlternateEqualityComparer{TAlternateKey, TKey}"/> with
        /// <typeparamref name="TAlternateKey"/> and <typeparamref name="TKey"/>. If it doesn't, an exception will be thrown.
        /// </remarks>
        public AlternateLookup<TAlternateKey> GetAlternateLookup<TAlternateKey>() where TAlternateKey : notnull, allows ref struct
        {
            if (!TryGetAlternateLookup(out AlternateLookup<TAlternateKey> lookup))
            {
                ThrowHelper.ThrowIncompatibleComparer();
            }

            return lookup;
        }

        /// <summary>
        /// Gets an instance of a type that may be used to perform operations on a <see cref="FrozenDictionary{TKey, TValue}"/>
        /// using a <typeparamref name="TAlternateKey"/> as a key instead of a <typeparamref name="TKey"/>.
        /// </summary>
        /// <typeparam name="TAlternateKey">The alternate type of a key for performing lookups.</typeparam>
        /// <param name="lookup">The created lookup instance when the method returns true, or a default instance that should not be used if the method returns false.</param>
        /// <returns>true if a lookup could be created; otherwise, false.</returns>
        /// <remarks>
        /// This instance must be using a comparer that implements <see cref="IAlternateEqualityComparer{TAlternateKey, TKey}"/> with
        /// <typeparamref name="TAlternateKey"/> and <typeparamref name="TKey"/>. If it doesn't, the method will return false.
        /// </remarks>
        public bool TryGetAlternateLookup<TAlternateKey>(out AlternateLookup<TAlternateKey> lookup) where TAlternateKey : notnull, allows ref struct
        {
            // The comparer must support the specified TAlternateKey. If it doesn't we can't create a lookup.
            // Some implementations where TKey is string rely on the length of the input and use it as part of the storage scheme.
            // That means we can only support TAlternateKeys that have a length we can check, which means we have to special-case
            // it. Since which implementation we pick is based on a heuristic and can't be predicted by the consumer, we don't
            // just have this requirement in that one implementation but for all implementations that might be picked for string.
            // As such, if the key is a string, we only support ReadOnlySpan<char> as the alternate key.
            if (Comparer is IAlternateEqualityComparer<TAlternateKey, TKey> &&
                (typeof(TKey) != typeof(string) || typeof(TAlternateKey) == typeof(ReadOnlySpan<char>)))
            {
                lookup = new AlternateLookup<TAlternateKey>(this);
                return true;
            }

            lookup = default;
            return false;
        }

        /// <summary>Gets the <see cref="Comparer"/> as an <see cref="IAlternateEqualityComparer{TAlternate, T}"/>.</summary>
        /// <remarks>This must only be used when it's already been proven that the comparer implements the target interface.</remarks>
        private protected IAlternateEqualityComparer<TAlternateKey, TKey> GetAlternateEqualityComparer<TAlternateKey>() where TAlternateKey : notnull, allows ref struct
        {
            Debug.Assert(Comparer is IAlternateEqualityComparer<TAlternateKey, TKey>, "Must have already been verified");
            return Unsafe.As<IAlternateEqualityComparer<TAlternateKey, TKey>>(Comparer);
        }

        /// <summary>
        /// Provides a type that may be used to perform operations on a <see cref="FrozenDictionary{TKey, TValue}"/>
        /// using a <typeparamref name="TAlternateKey"/> as a key instead of a <typeparamref name="TKey"/>.
        /// </summary>
        /// <typeparam name="TAlternateKey">The alternate type of a key for performing lookups.</typeparam>
        public readonly struct AlternateLookup<TAlternateKey> where TAlternateKey : notnull, allows ref struct
        {
            /// <summary>Initialize the instance. The dictionary must have already been verified to have a compatible comparer.</summary>
            internal AlternateLookup(FrozenDictionary<TKey, TValue> dictionary)
            {
                Debug.Assert(dictionary is not null);
                Debug.Assert(dictionary.Comparer is IAlternateEqualityComparer<TAlternateKey, TKey>);
                Dictionary = dictionary;
            }

            /// <summary>Gets the <see cref="FrozenDictionary{TKey, TValue}"/> against which this instance performs operations.</summary>
            public FrozenDictionary<TKey, TValue> Dictionary { get; }

            /// <summary>Gets or sets the value associated with the specified alternate key.</summary>
            /// <param name="key">The alternate key of the value to get or set.</param>
            /// <value>
            /// The value associated with the specified alternate key. If the specified alternate key is not found, a get operation throws
            /// a <see cref="KeyNotFoundException"/>, and a set operation creates a new element with the specified key.
            /// </value>
            /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
            /// <exception cref="KeyNotFoundException">The alternate key does not exist in the collection.</exception>
            public TValue this[TAlternateKey key]
            {
                get
                {
                    ref readonly TValue valueRef = ref Dictionary.GetValueRefOrNullRefCore(key);
                    if (Unsafe.IsNullRef(in valueRef))
                    {
                        ThrowHelper.ThrowKeyNotFoundException();
                    }

                    return valueRef;
                }
            }

            /// <summary>Determines whether the <see cref="FrozenDictionary{TKey, TValue}"/> contains the specified alternate key.</summary>
            /// <param name="key">The alternate key to check.</param>
            /// <returns><see langword="true"/> if the key is in the dictionary; otherwise, <see langword="false"/>.</returns>
            /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
            public bool ContainsKey(TAlternateKey key) =>
                !Unsafe.IsNullRef(in Dictionary.GetValueRefOrNullRefCore(key));

            /// <summary>Gets the value associated with the specified alternate key.</summary>
            /// <param name="key">The alternate key of the value to get.</param>
            /// <param name="value">
            /// When this method returns, contains the value associated with the specified key, if the key is found;
            /// otherwise, the default value for the type of the value parameter.
            /// </param>
            /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
            public bool TryGetValue(TAlternateKey key, [MaybeNullWhen(false)] out TValue value)
            {
                ref readonly TValue valueRef = ref Dictionary.GetValueRefOrNullRefCore(key);

                if (!Unsafe.IsNullRef(in valueRef))
                {
                    value = valueRef;
                    return true;
                }

                value = default;
                return false;
            }
        }
    }
}
