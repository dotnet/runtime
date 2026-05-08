// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    /// <summary>Provides downlevel polyfills for span extension methods.</summary>
    internal static class MemoryExtensionsPolyfills
    {
        public static bool Contains<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T> =>
            span.IndexOf(value) >= 0;

        public static bool ContainsAnyExcept(this ReadOnlySpan<char> span, char value)
        {
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] != value)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ContainsAny(this ReadOnlySpan<char> span, ReadOnlySpan<char> values) =>
            span.IndexOfAny(values) >= 0;
    }
}
