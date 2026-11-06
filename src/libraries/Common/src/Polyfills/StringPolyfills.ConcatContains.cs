// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

internal static class StringCompatibilityPolyfills
{
    extension(string)
    {
        public static string Concat(ReadOnlySpan<char> str0, ReadOnlySpan<char> str1) =>
            string.Concat(str0.ToString(), str1.ToString());
    }

    extension(string text)
    {
        public bool Contains(string value, StringComparison comparisonType) =>
            text.IndexOf(value, comparisonType) >= 0;
    }
}
