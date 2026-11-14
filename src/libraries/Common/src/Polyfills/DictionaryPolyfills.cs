// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic;

/// <summary>Provides downlevel polyfills for dictionary instance methods.</summary>
internal static class DictionaryPolyfills
{
    public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (dictionary is null)
        {
            throw new ArgumentNullException(nameof(dictionary));
        }

        if (!dictionary.ContainsKey(key))
        {
            dictionary.Add(key, value);
            return true;
        }

        return false;
    }

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
