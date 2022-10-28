// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A fast read-only dictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in this dictionary.</typeparam>
    /// <remarks>
    /// Frozen dictionaries are optimized for highly efficient read access and dense
    /// memory representation. For this, they trade off creation time. Creating a frozen
    /// dictionary can take a relatively long time, making frozen dictionaries best suited
    /// for long-lived dictionaries that will experience many lookups in their lifetime
    /// in order to compensate for the cost of creation.
    /// </remarks>
    public interface IFrozenDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
    {
        /// <summary>
        /// Gets a list containing the keys in the dictionary.
        /// </summary>
        /// <remarks>
        /// The order of the keys in the dictionary is unspecified, but it is the same order as the associated values returned by the <see cref="Values"/> property.
        /// </remarks>
        new FrozenList<TKey> Keys { get; }

        /// <summary>
        /// Gets a list containing the values in the dictionary.
        /// </summary>
        /// <remarks>
        /// The order of the values in the dictionary is unspecified, but it is the same order as the associated keys returned by the <see cref="Keys"/> property.
        /// </remarks>
        new FrozenList<TValue> Values { get; }

        /// <summary>
        /// Returns an enumerator that iterates through the dictionary.
        /// </summary>
        /// <returns>An enumerator that makes it possible to iterate through the dictionary's entries.</returns>
        new FrozenPairEnumerator<TKey, TValue> GetEnumerator();

        /// <summary>
        /// Gets a reference to a value in the dictionary.
        /// </summary>
        /// <param name="key">The key to lookup.</param>
        /// <returns>A reference to the value associated with the key.</returns>
        /// <exception cref="KeyNotFoundException">If the specifed key doesn't exist in the dictionary.</exception>.
        ref readonly TValue GetByRef(TKey key);

        /// <summary>
        /// Gets a reference to a value in the dictionary.
        /// </summary>
        /// <param name="key">The key to lookup.</param>
        /// <returns>A reference to the value associated with the key, or a null reference if the specified key doesn't exist in the dictionary.</returns>
        /// <remarks>Use <see cref="Unsafe.IsNullRef"/> to test for the null reference return.</remarks>
        ref readonly TValue TryGetByRef(TKey key);
    }
}
