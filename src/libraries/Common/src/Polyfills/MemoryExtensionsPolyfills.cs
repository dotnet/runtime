// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

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

        public static int Count<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>
        {
            int count = 0;
            for (int i = 0; i < span.Length; i++)
            {
                if (EqualityComparer<T>.Default.Equals(span[i], value))
                {
                    count++;
                }
            }

            return count;
        }

        public static int CommonPrefixLength<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other) where T : IEquatable<T>
        {
            int length = Math.Min(span.Length, other.Length);
            int i = 0;
            while (i < length && EqualityComparer<T>.Default.Equals(span[i], other[i]))
            {
                i++;
            }

            return i;
        }
    }
}
