// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Collections.Immutable
{
    internal interface IImmutableDictionaryInternal<TKey, TValue>
    {
        /// <summary>
        /// Determines whether the ImmutableDictionary&lt;TKey,TValue&gt;
        /// contains an element with the specified value.
        /// </summary>
        /// <param name="value">
        /// The value to locate in the ImmutableDictionary&lt;TKey,TValue&gt;.
        /// The value can be null for reference types.
        /// </param>
        /// <returns>
        /// true if the ImmutableDictionary&lt;TKey,TValue&gt; contains
        /// an element with the specified value; otherwise, false.
        /// </returns>
        bool ContainsValue(TValue value);
    }
}
