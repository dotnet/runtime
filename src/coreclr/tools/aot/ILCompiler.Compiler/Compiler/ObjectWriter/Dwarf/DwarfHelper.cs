// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Numerics;

namespace ILCompiler.ObjectWriter
{
    internal static class DwarfHelper
    {
        public static uint SizeOfULEB128(ulong value)
        {
            // bits_to_encode = (data != 0) ? 64 - CLZ(x) : 1 = 64 - CLZ(data | 1)
            // bytes = ceil(bits_to_encode / 7.0);            = (6 + bits_to_encode) / 7
            uint x = 6 + 64 - (uint)BitOperations.LeadingZeroCount(value | 1UL);
            // Division by 7 is done by (x * 37) >> 8 where 37 = ceil(256 / 7).
            // This works for 0 <= x < 256 / (7 * 37 - 256), i.e. 0 <= x <= 85.
            return (x * 37) >> 8;
        }

        public static uint SizeOfSLEB128(long value)
        {
            // The same as SizeOfULEB128 calculation but we have to account for the sign bit.
            uint x = 1 + 6 + 64 - (uint)BitOperations.LeadingZeroCount((ulong)(value ^ (value >> 63)) | 1UL);
            return (x * 37) >> 8;
        }

        public static int WriteULEB128(Span<byte> buffer, ulong value)
        {
            if (value >= 0x80)
            {
                int pos = 0;
                do
                {
                    buffer[pos++] = (byte)((value & 0x7F) | ((value >= 0x80) ? 0x80u : 0));
                    value >>= 7;
                }
                while (value > 0);
                return pos;
            }
            else
            {
                buffer[0] = (byte)value;
                return 1;
            }
        }

        public static void WriteULEB128(IBufferWriter<byte> writer, ulong value)
        {
            Span<byte> buffer = writer.GetSpan((int)SizeOfULEB128(value));
            writer.Advance(WriteULEB128(buffer, value));
        }

        public static int WriteSLEB128(Span<byte> buffer, long value)
        {
            bool cont = true;
            int pos = 0;
            while (cont)
            {
                var b = (byte)((byte)value & 0x7F);
                value >>= 7;
                bool isSignBitSet = (b & 0x40) != 0;
                if ((value == 0 && !isSignBitSet) || (value == -1 && isSignBitSet))
                {
                    cont = false;
                }
                else
                {
                    b |= 0x80;
                }
                buffer[pos++] = b;
            }
            return pos;
        }

        public static void WriteSLEB128(IBufferWriter<byte> writer, long value)
        {
            Span<byte> buffer = writer.GetSpan((int)SizeOfSLEB128(value));
            writer.Advance(WriteSLEB128(buffer, value));
        }
    }
}
