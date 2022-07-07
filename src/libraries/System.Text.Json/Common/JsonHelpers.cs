// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace System.Text.Json
{
    internal static partial class JsonHelpers
    {
        /// <summary>
        /// Emulates Dictionary.TryAdd on netstandard.
        /// </summary>
        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, in TKey key, in TValue value) where TKey : notnull
        {
#if NETSTANDARD2_0 || NETFRAMEWORK
            if (!dictionary.ContainsKey(key))
            {
                dictionary[key] = value;
                return true;
            }

            return false;
#else
            return dictionary.TryAdd(key, value);
#endif
        }

        /// <summary>
        /// Provides an in-place, stable sorting implementation for List.
        /// </summary>
        internal static void StableSortByKey<T, TKey>(this List<T> items, Func<T, TKey> keySelector)
            where TKey : unmanaged, IComparable<TKey>
        {
#if NET6_0_OR_GREATER
            Span<T> span = CollectionsMarshal.AsSpan(items);

            // Tuples implement lexical ordering OOTB which can be used to encode stable sorting
            // using the actual key as the first element and index as the second element.
            const int StackallocThreshold = 32;
            Span<(TKey, int)> keys = span.Length <= StackallocThreshold
                ? (stackalloc (TKey, int)[StackallocThreshold]).Slice(0, span.Length)
                : new (TKey, int)[span.Length];

            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = (keySelector(span[i]), i);
            }

            MemoryExtensions.Sort(keys, span);
#else
            T[] arrayCopy = items.ToArray();
            (TKey, int)[] keys = new (TKey, int)[arrayCopy.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = (keySelector(arrayCopy[i]), i);
            }

            Array.Sort(keys, arrayCopy);
            items.Clear();
            items.AddRange(arrayCopy);
#endif
        }
    }
}
