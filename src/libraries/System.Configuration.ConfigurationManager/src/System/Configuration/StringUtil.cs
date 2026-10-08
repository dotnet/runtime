// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration
{
    internal static class StringUtil
    {
        internal static bool EqualsOrBothNullOrEmpty(string s1, string s2)
        {
            return string.Equals(s1 ?? string.Empty, s2 ?? string.Empty, StringComparison.Ordinal);
        }

        internal static bool EqualsIgnoreCase(string s1, string s2)
        {
            return string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool StartsWithOrdinal(string s1, string s2)
        {
            if (s1 is null || s2 is null) return false;
            return s1.StartsWith(s2, StringComparison.Ordinal);
        }

        internal static bool StartsWithOrdinalIgnoreCase(string s1, string s2)
        {
            if (s1 is null || s2 is null) return false;
            return s1.StartsWith(s2, StringComparison.OrdinalIgnoreCase);
        }

        internal static string[] ObjectArrayToStringArray(object[] objectArray)
        {
            string[] stringKeys = new string[objectArray.Length];
            objectArray.CopyTo(stringKeys, 0);
            return stringKeys;
        }
    }
}
