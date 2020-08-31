// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Collections
{
    internal static class HashtableExtensions
    {
        public static bool TryGetValue<T>(this Hashtable table, object key, out T? value)
        {
            if (table.ContainsKey(key))
            {
                value = (T)table[key]!;
                return true;
            }

            value = default;
            return false;
        }
    }
}
