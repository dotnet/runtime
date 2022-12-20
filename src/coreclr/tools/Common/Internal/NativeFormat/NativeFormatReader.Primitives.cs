// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ---------------------------------------------------------------------------
// Native Format Reader
//
// Utilities to read native data from images, that are written by the NativeFormatWriter engine
// ---------------------------------------------------------------------------

using System.Diagnostics;

namespace Internal.NativeFormat
{
    // Minimal functionality that is low level enough for use in the managed runtime.
    internal unsafe partial struct NativePrimitiveDecoder
    {
        public static byte ReadUInt8(ref byte* stream)
        {
            byte result = *(stream); // Assumes little endian and unaligned access
            stream++;
            return result;
        }

        public static ushort ReadUInt16(ref byte* stream)
        {
            ushort result = *(ushort*)(stream); // Assumes little endian and unaligned access
            stream += 2;
            return result;
        }

        public static uint ReadUInt32(ref byte* stream)
        {
            uint result = *(uint*)(stream); // Assumes little endian and unaligned access
            stream += 4;
            return result;
        }

        public static ulong ReadUInt64(ref byte* stream)
        {
            ulong result = *(ulong*)(stream); // Assumes little endian and unaligned access
            stream += 8;
            return result;
        }

        public static unsafe float ReadFloat(ref byte* stream)
        {
            uint value = ReadUInt32(ref stream);
            return *(float*)(&value);
        }

        public static double ReadDouble(ref byte* stream)
        {
            ulong value = ReadUInt64(ref stream);
            return *(double*)(&value);
        }

        public static uint GetUnsignedEncodingSize(uint value)
        {
            if (value < 128) return 1;
            if (value < 128 * 128) return 2;
            if (value < 128 * 128 * 128) return 3;
            if (value < 128 * 128 * 128 * 128) return 4;
            return 5;
        }

        public static uint DecodeUnsigned(ref byte* stream)
        {
            uint value = 0;

            uint val = *stream;
            if ((val & 1) == 0)
            {
                value = (val >> 1);
                stream += 1;
            }
            else if ((val & 2) == 0)
            {
                value = (val >> 2) |
                      (((uint)*(stream + 1)) << 6);
                stream += 2;
            }
            else if ((val & 4) == 0)
            {
                value = (val >> 3) |
                      (((uint)*(stream + 1)) << 5) |
                      (((uint)*(stream + 2)) << 13);
                stream += 3;
            }
            else if ((val & 8) == 0)
            {
                value = (val >> 4) |
                      (((uint)*(stream + 1)) << 4) |
                      (((uint)*(stream + 2)) << 12) |
                      (((uint)*(stream + 3)) << 20);
                stream += 4;
            }
            else if ((val & 16) == 0)
            {
                stream += 1;
                value = ReadUInt32(ref stream);
            }
            else
            {
                Debug.Assert(false);
                return 0;
            }

            return value;
        }

        public static int DecodeSigned(ref byte* stream)
        {
            int value = 0;

            int val = *(stream);
            if ((val & 1) == 0)
            {
                value = ((sbyte)val) >> 1;
                stream += 1;
            }
            else if ((val & 2) == 0)
            {
                value = (val >> 2) |
                      (((int)*(sbyte*)(stream + 1)) << 6);
                stream += 2;
            }
            else if ((val & 4) == 0)
            {
                value = (val >> 3) |
                      (((int)*(stream + 1)) << 5) |
                      (((int)*(sbyte*)(stream + 2)) << 13);
                stream += 3;
            }
            else if ((val & 8) == 0)
            {
                value = (val >> 4) |
                      (((int)*(stream + 1)) << 4) |
                      (((int)*(stream + 2)) << 12) |
                      (((int)*(sbyte*)(stream + 3)) << 20);
                stream += 4;
            }
            else if ((val & 16) == 0)
            {
                stream += 1;
                value = (int)ReadUInt32(ref stream);
            }
            else
            {
                Debug.Assert(false);
                return 0;
            }

            return value;
        }

        public static ulong DecodeUnsignedLong(ref byte* stream)
        {
            ulong value = 0;

            byte val = *stream;
            if ((val & 31) != 31)
            {
                value = DecodeUnsigned(ref stream);
            }
            else if ((val & 32) == 0)
            {
                stream += 1;
                value = ReadUInt64(ref stream);
            }
            else
            {
                Debug.Assert(false);
                return 0;
            }

            return value;
        }

        public static long DecodeSignedLong(ref byte* stream)
        {
            long value = 0;

            byte val = *stream;
            if ((val & 31) != 31)
            {
                value = DecodeSigned(ref stream);
            }
            else if ((val & 32) == 0)
            {
                stream += 1;
                value = (long)ReadUInt64(ref stream);
            }
            else
            {
                Debug.Assert(false);
                return 0;
            }

            return value;
        }
    }
}
