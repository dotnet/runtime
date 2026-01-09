// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections
{
    internal static class HashtableExtensions
    {
        public static bool TryGetValue(this Hashtable table, object key, out int value)
        {
            if (table[key] is { } obj)
            {
                value = (int)obj;
                return true;
            }

            value = 0;
            return false;
        }
    }
}
