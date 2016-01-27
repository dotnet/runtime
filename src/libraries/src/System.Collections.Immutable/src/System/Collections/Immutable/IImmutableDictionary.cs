// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace System.Collections.Immutable
{
    /// <summary>
    /// An immutable key-value dictionary. 
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    public interface IImmutableDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        /// <summary>
        /// Gets an empty dictionary with equivalent ordering and key/value comparison rules.
        /// </summary>
        [Pure]
        IImmutableDictionary<TKey, TValue> Clear();

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key">The key of the entry to add.</param>
        /// <param name="value">The value of the entry to add.</param>
        /// <returns>The new dictionary containing the additional key-value pair.</returns>
        /// <exception cref="ArgumentException">Thrown when the given key already exists in the dictionary but has a different value.</exception>
        /// <remarks>
        /// If the given key-value pair are already in the dictionary, the existing instance is returned.
        /// </remarks>
        [Pure]
        IImmutableDictionary<TKey, TValue> Add(TKey key, TValue value);

        /// <summary>
        /// Adds the specified key-value pairs to the dictionary.
        /// </summary>
        /// <param name="pairs">The pairs.</param>
        /// <returns>The new dictionary containing the additional key-value pairs.</returns>
        /// <exception cref="ArgumentException">Thrown when one of the given keys already exists in the dictionary but has a different value.</exception>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        [Pure]
        IImmutableDictionary<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs);

        /// <summary>
        /// Sets the specified key and value to the dictionary, possibly overwriting an existing value for the given key.
        /// </summary>
        /// <param name="key">The key of the entry to add.</param>
        /// <param name="value">The value of the entry to add.</param>
        /// <returns>The new dictionary containing the additional key-value pair.</returns>
        /// <remarks>
        /// If the given key-value pair are already in the dictionary, the existing instance is returned.
        /// If the key already exists but with a different value, a new instance with the overwritten value will be returned.
        /// </remarks>
        [Pure]
        IImmutableDictionary<TKey, TValue> SetItem(TKey key, TValue value);

        /// <summary>
        /// Applies a given set of key=value pairs to an immutable dictionary, replacing any conflicting keys in the resulting dictionary.
        /// </summary>
        /// <param name="items">The key=value pairs to set on the dictionary.  Any keys that conflict with existing keys will overwrite the previous values.</param>
        /// <returns>An immutable dictionary.</returns>
        [Pure]
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        IImmutableDictionary<TKey, TValue> SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items);

        /// <summary>
        /// Removes the specified keys from the dictionary with their associated values.
        /// </summary>
        /// <param name="keys">The keys to remove.</param>
        /// <returns>A new dictionary with those keys removed; or this instance if those keys are not in the dictionary.</returns>
        [Pure]
        IImmutableDictionary<TKey, TValue> RemoveRange(IEnumerable<TKey> keys);

        /// <summary>
        /// Removes the specified key from the dictionary with its associated value.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>A new dictionary with the matching entry removed; or this instance if the key is not in the dictionary.</returns>
        [Pure]
        IImmutableDictionary<TKey, TValue> Remove(TKey key);

        /// <summary>
        /// Determines whether this dictionary contains the specified key-value pair.
        /// </summary>
        /// <param name="pair">The key value pair.</param>
        /// <returns>
        ///   <c>true</c> if this dictionary contains the key-value pair; otherwise, <c>false</c>.
        /// </returns>
        [Pure]
        bool Contains(KeyValuePair<TKey, TValue> pair);

        /// <summary>
        /// Searches the dictionary for a given key and returns the equal key it finds, if any.
        /// </summary>
        /// <param name="equalKey">The key to search for.</param>
        /// <param name="actualKey">The key from the dictionary that the search found, or <paramref name="equalKey"/> if the search yielded no match.</param>
        /// <returns>A value indicating whether the search was successful.</returns>
        /// <remarks>
        /// This can be useful when you want to reuse a previously stored reference instead of
        /// a newly constructed one (so that more sharing of references can occur) or to look up
        /// the canonical value, or a value that has more complete data than the value you currently have,
        /// although their comparer functions indicate they are equal.
        /// </remarks>
        [Pure]
        bool TryGetKey(TKey equalKey, out TKey actualKey);
    }
}
