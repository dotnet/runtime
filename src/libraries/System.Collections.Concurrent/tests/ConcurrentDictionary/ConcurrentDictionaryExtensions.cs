// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Collections.Concurrent.Tests
{
    // Allows the ConcurrentDictionary tests to run on targets that do not have the new GetOrAdd/AddOrUpdate overloads.
    internal static class ConcurrentDictionaryExtensions
    {
        public static TValue GetOrAdd<TKey, TValue, TArg>(
            this ConcurrentDictionary<TKey, TValue> dictionary,
            TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (valueFactory == null) throw new ArgumentNullException(nameof(valueFactory));

            while (true)
            {
                TValue value;
                if (dictionary.TryGetValue(key, out value))
                    return value;

                value = valueFactory(key, factoryArgument);
                if (dictionary.TryAdd(key, value))
                    return value;
            }
        }

        public static TValue AddOrUpdate<TKey, TValue, TArg>(
            this ConcurrentDictionary<TKey, TValue> dictionary,
            TKey key, Func<TKey, TArg, TValue> addValueFactory, Func<TKey, TValue, TArg, TValue> updateValueFactory, TArg factoryArgument)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (addValueFactory == null) throw new ArgumentNullException(nameof(addValueFactory));
            if (updateValueFactory == null) throw new ArgumentNullException(nameof(updateValueFactory));

            while (true)
            {
                TValue value;
                if (dictionary.TryGetValue(key, out value))
                {
                    TValue updatedValue = updateValueFactory(key, value, factoryArgument);
                    if (dictionary.TryUpdate(key, updatedValue, value))
                    {
                        return updatedValue;
                    }
                }
                else
                {
                    value = addValueFactory(key, factoryArgument);
                    if (dictionary.TryAdd(key, value))
                    {
                        return value;
                    }
                }
            }
        }
    }
}
