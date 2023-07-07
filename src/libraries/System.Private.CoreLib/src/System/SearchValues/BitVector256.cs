// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    internal unsafe struct BitVector256
    {
        private fixed uint _values[8];

        public void Set(int c)
        {
            Debug.Assert(c < 256);
            uint offset = (uint)(c >> 5);
            uint significantBit = 1u << c;
            _values[offset] |= significantBit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains128(char c) =>
            c < 128 && ContainsUnchecked(c);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains256(char c) =>
            c < 256 && ContainsUnchecked(c);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(byte b) =>
            ContainsUnchecked(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly bool ContainsUnchecked(int b)
        {
            Debug.Assert(b < 256);
            uint offset = (uint)(b >> 5);
            uint significantBit = 1u << b;
            return (_values[offset] & significantBit) != 0;
        }

        public readonly char[] GetCharValues()
        {
            var chars = new List<char>();
            for (int i = 0; i < 256; i++)
            {
                if (ContainsUnchecked(i))
                {
                    chars.Add((char)i);
                }
            }
            return chars.ToArray();
        }

        public readonly byte[] GetByteValues()
        {
            var bytes = new List<byte>();
            for (int i = 0; i < 256; i++)
            {
                if (ContainsUnchecked(i))
                {
                    bytes.Add((byte)i);
                }
            }
            return bytes.ToArray();
        }
    }
}
