// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    internal sealed class BitmapCharSearchValues : SearchValues<char>
    {
        private readonly uint[] _bitmap;

        public BitmapCharSearchValues(ReadOnlySpan<char> values, int maxInclusive)
        {
            Debug.Assert(maxInclusive <= char.MaxValue);

            _bitmap = new uint[maxInclusive / 32 + 1];

            foreach (char c in values)
            {
                _bitmap[c >> 5] |= 1u << c;
            }
        }

        internal override char[] GetValues()
        {
            var chars = new List<char>();
            uint[] bitmap = _bitmap;

            for (int i = 0; i < _bitmap.Length * 32; i++)
            {
                if (Contains(bitmap, i))
                {
                    chars.Add((char)i);
                }
            }

            return chars.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override bool ContainsCore(char value) =>
            Contains(_bitmap, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Contains(uint[] bitmap, int value)
        {
            uint offset = (uint)(value >> 5);
            return offset < (uint)bitmap.Length && (bitmap[offset] & (1u << value)) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAny(ReadOnlySpan<char> span) =>
            IndexOfAny<IndexOfAnyAsciiSearcher.DontNegate>(span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAnyExcept(ReadOnlySpan<char> span) =>
            IndexOfAny<IndexOfAnyAsciiSearcher.Negate>(span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAny(ReadOnlySpan<char> span) =>
            LastIndexOfAny<IndexOfAnyAsciiSearcher.DontNegate>(span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAnyExcept(ReadOnlySpan<char> span) =>
            LastIndexOfAny<IndexOfAnyAsciiSearcher.Negate>(span);

        private int IndexOfAny<TNegator>(ReadOnlySpan<char> span)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            uint[] bitmap = _bitmap;

            for (int i = 0; i < span.Length; i++)
            {
                char c = span[i];
                if (TNegator.NegateIfNeeded(Contains(bitmap, c)))
                {
                    return i;
                }
            }

            return -1;
        }

        private int LastIndexOfAny<TNegator>(ReadOnlySpan<char> span)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            uint[] bitmap = _bitmap;
            int searchSpaceLength = span.Length;

            while (--searchSpaceLength >= 0)
            {
                char c = span[searchSpaceLength];
                if (TNegator.NegateIfNeeded(Contains(bitmap, c)))
                {
                    break;
                }
            }

            return searchSpaceLength;
        }
    }
}
