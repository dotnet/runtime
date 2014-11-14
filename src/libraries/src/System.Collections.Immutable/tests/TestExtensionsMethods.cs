// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Validation;

namespace System.Collections.Immutable.Test
{
    internal static class TestExtensionsMethods
    {
        internal static IDictionary<TKey, TValue> ToReadOnlyDictionary<TKey, TValue>(this IImmutableDictionary<TKey, TValue> dictionary)
        {
            Requires.NotNull(dictionary, "dictionary");

            return (IDictionary<TKey, TValue>)dictionary;
        }

        internal static IDictionary<TKey, TValue> ToBuilder<TKey, TValue>(this IImmutableDictionary<TKey, TValue> dictionary)
        {
            Requires.NotNull(dictionary, "dictionary");

            var hashDictionary = dictionary as ImmutableDictionary<TKey, TValue>;
            if (hashDictionary != null)
            {
                return hashDictionary.ToBuilder();
            }

            var sortedDictionary = dictionary as ImmutableSortedDictionary<TKey, TValue>;
            if (sortedDictionary != null)
            {
                return sortedDictionary.ToBuilder();
            }

            throw new NotSupportedException();
        }
    }
}
