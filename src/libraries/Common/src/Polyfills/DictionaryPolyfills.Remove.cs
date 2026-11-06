// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic;

internal static class DictionaryRemovalPolyfills
{
    extension<TKey, TValue>(Dictionary<TKey, TValue> dictionary) where TKey : notnull
    {
        public bool Remove(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (!dictionary.TryGetValue(key, out value))
            {
                return false;
            }

            return dictionary.Remove(key);
        }
    }
}
