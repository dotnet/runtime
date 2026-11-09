// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

/// <summary>Provides downlevel polyfills for string methods.</summary>
internal static partial class StringPolyfills
{
    internal delegate void StringCreateAction<TState>(Span<char> chars, TState state);

    extension(string)
    {
        public static string Concat(ReadOnlySpan<char> str0, ReadOnlySpan<char> str1) =>
            string.Concat(str0.ToString(), str1.ToString());

        public static string Create<TState>(int length, TState state, StringCreateAction<TState> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (length == 0)
            {
                return string.Empty;
            }

            char[] chars = new char[length];
            action(chars, state);
            return new string(chars);
        }
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
