// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NETSTANDARD2_1

namespace System
{
    /// <summary>Provides downlevel polyfills for string extension methods.</summary>
    internal static class StringPolyfills
    {
        public static bool StartsWith(this string s, char value) =>
            s.Length > 0 && s[0] == value;

        public static bool EndsWith(this string s, char value) =>
            s.Length > 0 && s[s.Length - 1] == value;

        public static bool Contains(this string s, char value) =>
            s.IndexOf(value) >= 0;

        internal delegate void SpanAction<T, in TArg>(Span<T> span, TArg arg);

        extension(string)
        {
            public static string Create<TState>(int length, TState state, SpanAction<char, TState> action)
            {
                char[] arr = new char[length];
                action(arr, state);

                return new string(arr);
            }
        }
    }
}

#endif
