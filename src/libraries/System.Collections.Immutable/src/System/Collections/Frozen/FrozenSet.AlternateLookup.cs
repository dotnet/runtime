// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    public abstract partial class FrozenSet<T>
    {
        /// <summary>
        /// Gets an instance of a type that may be used to perform operations on a <see cref="FrozenSet{T}"/>
        /// using a <typeparamref name="TAlternate"/> instead of a <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="TAlternate">The alternate type of a item for performing lookups.</typeparam>
        /// <returns>The created lookup instance.</returns>
        /// <exception cref="InvalidOperationException">This instance's comparer is not compatible with <typeparamref name="TAlternate"/>.</exception>
        /// <remarks>
        /// This instance must be using a comparer that implements <see cref="IAlternateEqualityComparer{TAlternate, T}"/> with
        /// <typeparamref name="TAlternate"/> and <typeparamref name="T"/>. If it doesn't, an exception will be thrown.
        /// </remarks>
        public AlternateLookup<TAlternate> GetAlternateLookup<TAlternate>() where TAlternate : allows ref struct
        {
            if (!TryGetAlternateLookup(out AlternateLookup<TAlternate> lookup))
            {
                ThrowHelper.ThrowIncompatibleComparer();
            }

            return lookup;
        }

        /// <summary>
        /// Gets an instance of a type that may be used to perform operations on a <see cref="FrozenSet{T}"/>
        /// using a <typeparamref name="TAlternate"/> instead of a <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="TAlternate">The alternate type of a key for performing lookups.</typeparam>
        /// <param name="lookup">The created lookup instance when the method returns true, or a default instance that should not be used if the method returns false.</param>
        /// <returns>true if a lookup could be created; otherwise, false.</returns>
        /// <remarks>
        /// This instance must be using a comparer that implements <see cref="IAlternateEqualityComparer{TAlternate, T}"/> with
        /// <typeparamref name="TAlternate"/> and <typeparamref name="T"/>. If it doesn't, the method will return false.
        /// </remarks>
        public bool TryGetAlternateLookup<TAlternate>(out AlternateLookup<TAlternate> lookup) where TAlternate : allows ref struct
        {
            // The comparer must support the specified TAlternate.
            // Some implementations where TKey is string rely on the length of the input and use it as part of the storage scheme.
            // That means we can only support TAlternates that have a length we can check, which means we have to special-case
            // it. Since which implementation we pick is based on a heuristic and can't be predicted by the consumer, we don't
            // just have this requirement in that one implementation but for all implementations that might be picked for string.
            // As such, if the key is a string, we only support ReadOnlySpan<char> as the alternate key.
            if (Comparer is IAlternateEqualityComparer<TAlternate, T> &&
                (typeof(T) != typeof(string) || typeof(TAlternate) == typeof(ReadOnlySpan<char>)))
            {
                lookup = new AlternateLookup<TAlternate>(this);
                return true;
            }

            lookup = default;
            return false;
        }

        /// <summary>Gets the <see cref="Comparer"/> as an <see cref="IAlternateEqualityComparer{TAlternate, T}"/>.</summary>
        /// <remarks>This must only be used when it's already been proven that the comparer implements the target interface.</remarks>
        private protected IAlternateEqualityComparer<TAlternateKey, T> GetAlternateEqualityComparer<TAlternateKey>() where TAlternateKey : allows ref struct
        {
            Debug.Assert(Comparer is IAlternateEqualityComparer<TAlternateKey, T>, "Must have already been verified");
            return Unsafe.As<IAlternateEqualityComparer<TAlternateKey, T>>(Comparer);
        }

        /// <summary>
        /// Provides a type that may be used to perform operations on a <see cref="FrozenSet{T}"/>
        /// using a <typeparamref name="TAlternate"/> as a key instead of a <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="TAlternate">The alternate type of a key for performing lookups.</typeparam>
        public readonly struct AlternateLookup<TAlternate> where TAlternate : allows ref struct
        {
            /// <summary>Initialize the instance. The set must have already been verified to have a compatible comparer.</summary>
            internal AlternateLookup(FrozenSet<T> set)
            {
                Debug.Assert(set is not null);
                Debug.Assert(set.Comparer is IAlternateEqualityComparer<TAlternate, T>);
                Set = set;
            }

            /// <summary>Gets the <see cref="FrozenSet{T}"/> against which this instance performs operations.</summary>
            public FrozenSet<T> Set { get; }

            /// <summary>Determines whether a set contains the specified element.</summary>
            /// <param name="item">The element to locate in the set.</param>
            /// <returns>true if the set contains the specified element; otherwise, false.</returns>
            public bool Contains(TAlternate item) => Set.FindItemIndex(item) >= 0;

            /// <summary>Searches the set for a given value and returns the equal value it finds, if any.</summary>
            /// <param name="equalValue">The value to search for.</param>
            /// <param name="actualValue">The value from the set that the search found, or the default value of <typeparamref name="T"/> when the search yielded no match.</param>
            /// <returns>A value indicating whether the search was successful.</returns>
            public bool TryGetValue(TAlternate equalValue, [MaybeNullWhen(false)] out T actualValue)
            {
                int index = Set.FindItemIndex(equalValue);
                if (index >= 0)
                {
                    actualValue = Set.Items[index];
                    return true;
                }

                actualValue = default;
                return false;
            }
        }
    }
}
