// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Concurrent;

/// <summary>Provides downlevel polyfills for instance methods on <see cref="ConcurrentDictionary{TKey, TValue}"/>.</summary>
internal static class ConcurrentDictionaryPolyfills
{
    extension<TKey, TValue>(ConcurrentDictionary<TKey, TValue> dictionary) where TKey : notnull
    {
        public TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
        {
            ArgumentNullException.ThrowIfNull(valueFactory);
            return dictionary.GetOrAdd(key, key => valueFactory(key, factoryArgument));
        }
    }
}
