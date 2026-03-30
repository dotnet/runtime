// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic
{
    /// <summary>Provides downlevel polyfills for instance methods on dictionary types.</summary>
    internal static class DictionaryPolyfills
    {
        extension(Dictionary<TKey, TValue> dictionary) where TKey : notnull
        {
            public bool TryAdd(TKey key, TValue value)
            {
                if (!dictionary.ContainsKey(key))
                {
                    dictionary[key] = value;
                    return true;
                }

                return false;
            }
        }

        extension(IDictionary<TKey, TValue> dictionary) where TKey : notnull
        {
            public bool TryAdd(TKey key, TValue value)
            {
                if (!dictionary.ContainsKey(key))
                {
                    dictionary[key] = value;
                    return true;
                }

                return false;
            }
        }

        extension(Queue<T> queue)
        {
            public bool TryDequeue([NotNullWhen(true)] out T? result)
            {
                if (queue.Count > 0)
                {
                    result = queue.Dequeue();
                    return true;
                }

                result = default;
                return false;
            }
        }
    }
}
