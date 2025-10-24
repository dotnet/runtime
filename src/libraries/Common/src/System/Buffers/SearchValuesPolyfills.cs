// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Buffers
{
    /// <summary>Provides downlevel polyfills for SearchValues APIs.</summary>
    internal abstract class SearchValues<T>
    {
        public abstract bool Contains(T value);

        public abstract int IndexOfAny(ReadOnlySpan<char> span);

        public abstract int IndexOfAnyExcept(ReadOnlySpan<char> span);
    }

    internal static class SearchValues
    {
        public static SearchValues<char> Create(string values) =>
            Create(values.AsSpan());

        public static SearchValues<char> Create(ReadOnlySpan<char> values) =>
            new CharSearchValuesPolyfill(values);

        public static int IndexOfAny(this ReadOnlySpan<char> span, SearchValues<char> values) =>
            values.IndexOfAny(span);

        public static int IndexOfAnyExcept(this ReadOnlySpan<char> span, SearchValues<char> values) =>
            values.IndexOfAnyExcept(span);

        public static bool ContainsAny(this ReadOnlySpan<char> span, SearchValues<char> values) =>
            values.IndexOfAny(span) >= 0;

        public static bool ContainsAnyExcept(this ReadOnlySpan<char> span, SearchValues<char> values) =>
            values.IndexOfAnyExcept(span) >= 0;

        internal sealed class CharSearchValuesPolyfill : SearchValues<char>
        {
            private readonly uint[] _ascii = new uint[8];
            private readonly string _nonAscii;

            public CharSearchValuesPolyfill(ReadOnlySpan<char> values)
            {
                StringBuilder? nonAscii = null;

                foreach (char c in values)
                {
                    if (c < 128)
                    {
                        uint offset = (uint)(c >> 5);
                        uint significantBit = 1u << c;
                        _ascii[offset] |= significantBit;
                    }
                    else
                    {
                        nonAscii ??= new();
                        nonAscii.Append(c);
                    }
                }

                _nonAscii = nonAscii?.ToString() ?? string.Empty;
                Debug.Assert(_nonAscii.Length < 10, "Expected few non-ASCII characters at most.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override bool Contains(char value)
            {
                uint offset = (uint)(value >> 5);
                uint significantBit = 1u << value;
                uint[] lookup = _ascii;

                if (offset < (uint)lookup.Length)
                {
                    return (lookup[offset] & significantBit) != 0;
                }
                else
                {
                    return NonAsciiContains(value);
                }
            }

            private bool NonAsciiContains(char value)
            {
                foreach (char c in _nonAscii)
                {
                    if (c == value)
                    {
                        return true;
                    }
                }

                return false;
            }

            public override int IndexOfAny(ReadOnlySpan<char> span)
            {
                for (int i = 0; i < span.Length; i++)
                {
                    if (Contains(span[i]))
                    {
                        return i;
                    }
                }

                return -1;
            }

            public override int IndexOfAnyExcept(ReadOnlySpan<char> span)
            {
                for (int i = 0; i < span.Length; i++)
                {
                    if (!Contains(span[i]))
                    {
                        return i;
                    }
                }

                return -1;
            }
        }
    }
}
