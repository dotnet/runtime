// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
            IndexOfAny<IndexOfAnyAsciiSearcher.DontNegate>(ref MemoryMarshal.GetReference(span), span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int IndexOfAnyExcept(ReadOnlySpan<char> span) =>
            IndexOfAny<IndexOfAnyAsciiSearcher.Negate>(ref MemoryMarshal.GetReference(span), span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAny(ReadOnlySpan<char> span) =>
            LastIndexOfAny<IndexOfAnyAsciiSearcher.DontNegate>(ref MemoryMarshal.GetReference(span), span.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override int LastIndexOfAnyExcept(ReadOnlySpan<char> span) =>
            LastIndexOfAny<IndexOfAnyAsciiSearcher.Negate>(ref MemoryMarshal.GetReference(span), span.Length);

        private int IndexOfAny<TNegator>(ref char searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            ref char searchSpaceEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength);
            ref char cur = ref searchSpace;
            uint[] bitmap = _bitmap;

            while (!Unsafe.AreSame(ref cur, ref searchSpaceEnd))
            {
                char c = cur;
                if (TNegator.NegateIfNeeded(Contains(bitmap, c)))
                {
                    return (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref cur) / sizeof(char));
                }

                cur = ref Unsafe.Add(ref cur, 1);
            }

            return -1;
        }

        private int LastIndexOfAny<TNegator>(ref char searchSpace, int searchSpaceLength)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            uint[] bitmap = _bitmap;

            while (--searchSpaceLength >= 0)
            {
                char c = Unsafe.Add(ref searchSpace, searchSpaceLength);
                if (TNegator.NegateIfNeeded(Contains(bitmap, c)))
                {
                    break;
                }
            }

            return searchSpaceLength;
        }
    }
}
