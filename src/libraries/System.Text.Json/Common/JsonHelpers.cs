// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    }
}
