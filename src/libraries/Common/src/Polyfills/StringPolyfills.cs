// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

/// <summary>Provides downlevel polyfills for string methods.</summary>
internal static partial class StringPolyfills
{
    extension(string)
    {
        public static string Concat(ReadOnlySpan<char> str0, ReadOnlySpan<char> str1) =>
            string.Concat(str0.ToString(), str1.ToString());
    }

    extension(string text)
    {
        public bool StartsWith(char value) =>
            text.Length > 0 && text[0] == value;

        public bool EndsWith(char value) =>
            text.Length > 0 && text[text.Length - 1] == value;

#pragma warning disable CA2249 // These Contains polyfills must use IndexOf.
        public bool Contains(char value) =>
            text.IndexOf(value) >= 0;

        public bool Contains(string value, StringComparison comparisonType) =>
            text.IndexOf(value, comparisonType) >= 0;
#pragma warning restore CA2249
    }
}
