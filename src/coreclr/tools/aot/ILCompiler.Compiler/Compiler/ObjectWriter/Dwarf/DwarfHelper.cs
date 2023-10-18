// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        public static void WriteULEB128(IBufferWriter<byte> writer, ulong value)
        {
            if (value >= 0x80)
            {
                int size = (int)SizeOfULEB128(value);
                var buffer = writer.GetSpan(size);
                do
                {
                    buffer[0] = (byte)((value & 0x7f) | ((value >= 0x80) ? 0x80u : 0));
                    buffer = buffer.Slice(1);
                    value >>= 7;
                }
                while (value > 0);
                writer.Advance(size);
            }
            else
            {
                var buffer = writer.GetSpan(1);
                buffer[0] = (byte)value;
                writer.Advance(1);
            }
        }

        public static void WriteSLEB128(IBufferWriter<byte> writer, long value)
        {
            int size = (int)SizeOfSLEB128(value);
            var buffer = writer.GetSpan(size);
            bool cont = true;
            while (cont)
            {
                var b = (byte)((byte)value & 0x7f);
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
                buffer[0] = b;
                buffer = buffer.Slice(1);
            }
            writer.Advance(size);
        }
    }
}
