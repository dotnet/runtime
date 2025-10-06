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

        public readonly BitVector256 CreateInverse()
        {
            BitVector256 inverse = default;

            for (int i = 0; i < 8; i++)
            {
                inverse._values[i] = ~_values[i];
            }

            return inverse;
        }

        public void Set(int c)
        {
            Debug.Assert(c < 256);
            uint offset = (uint)(c >> 5);
            uint significantBit = 1u << c;
            _values[offset] |= significantBit;
        }

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
            Span<char> chars = stackalloc char[256];
            int size = 0;
            for (int i = 0; i < 256; i++)
            {
                if (ContainsUnchecked(i))
                {
                    chars[size] = (char)i;
                    size++;
                }
            }
            return chars.Slice(0, size).ToArray();
        }

        public readonly byte[] GetByteValues()
        {
            Span<byte> bytes = stackalloc byte[256];
            int size = 0;
            for (int i = 0; i < 256; i++)
            {
                if (ContainsUnchecked(i))
                {
                    bytes[size] = (byte)i;
                    size++;
                }
            }
            return bytes.Slice(0, size).ToArray();
        }
    }
}
