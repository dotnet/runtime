// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
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
            char[] chars = new char[Count()];
            int count = 0;

            for (int i = 0; i < 256; i++)
            {
                if (ContainsUnchecked(i))
                {
                    chars[count++] = (char)i;
                }
            }

            Debug.Assert(count == chars.Length);
            return chars;
        }

        public readonly byte[] GetByteValues()
        {
            byte[] bytes = new byte[Count()];
            int count = 0;

            for (int i = 0; i < 256; i++)
            {
                if (ContainsUnchecked(i))
                {
                    bytes[count++] = (byte)i;
                }
            }

            Debug.Assert(count == bytes.Length);
            return bytes;
        }

        public readonly int Count()
        {
            int count = 0;

            for (int i = 0; i < 8; i++)
            {
                count += PopCount(_values[i]);
            }

            return count;
        }

        private static int PopCount(uint value)
        {
#if NET8_0_OR_GREATER
            return BitOperations.PopCount(value);
#else
            const uint c1 = 0x_55555555u;
            const uint c2 = 0x_33333333u;
            const uint c3 = 0x_0F0F0F0Fu;
            const uint c4 = 0x_01010101u;

            value -= (value >> 1) & c1;
            value = (value & c2) + ((value >> 2) & c2);
            value = (((value + (value >> 4)) & c3) * c4) >> 24;

            return (int)value;
#endif
        }

        public readonly int IndexOf(int value)
        {
            uint offset = (uint)(value >> 5);
            uint significantBit = 1u << value;

            if (offset >= 8 || (_values[offset] & significantBit) == 0)
            {
                return -1;
            }

            int index = PopCount(_values[offset] & (significantBit - 1));

            for (int i = 0; i < offset; i++)
            {
                index += PopCount(_values[i]);
            }

            return index;
        }
    }
}
