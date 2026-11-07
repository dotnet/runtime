// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System;

/// <summary>Provides downlevel polyfills for string methods.</summary>
internal static partial class StringPolyfills
{
    extension(string)
    {
        public static string Concat(ReadOnlySpan<char> str0, ReadOnlySpan<char> str1) =>
            string.Concat(str0.ToString(), str1.ToString());

        public static string Create<TState>(int length, TState state, SpanAction<char, TState> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentOutOfRangeException.ThrowIfNegative(length);

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
        public bool Contains(string value, StringComparison comparisonType) =>
            text.IndexOf(value, comparisonType) >= 0;
    }
}
