// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Internal;

namespace BinaryPrimitivesReverseEndianness
{
    class Program
    {
        public const int Pass = 100;
        public const int Fail = 0;

        private const ushort ConstantUInt16Input = 0x9876;
        private const ushort ConstantUInt16Expected = 0x7698;

        private const uint ConstantUInt32Input = 0x98765432;
        private const uint ConstantUInt32Expected = 0x32547698;

        private const ulong ConstantUInt64Input = 0xfedcba9876543210;
        private const ulong ConstantUInt64Expected = 0x1032547698badcfe;

        private static readonly byte[] s_bufferLE = new byte[] { 0x32, 0x54, 0x76, 0x98 };
        private static readonly byte[] s_bufferBE = new byte[] { 0x98, 0x76, 0x54, 0x32 };

        static int Main(string[] args)
        {
            /*
             * CONST VALUE TESTS
             */

            ushort swappedUInt16 = BinaryPrimitives.ReverseEndianness(ConstantUInt16Input);
            if (swappedUInt16 != ConstantUInt16Expected)
            {
                ReportError("const UInt16", ConstantUInt16Input, swappedUInt16, ConstantUInt16Expected);
                return Fail;
            }

            uint swappedUInt32 = BinaryPrimitives.ReverseEndianness(ConstantUInt32Input);
            if (swappedUInt32 != ConstantUInt32Expected)
            {
                ReportError("const UInt32", ConstantUInt32Input, swappedUInt32, ConstantUInt32Expected);
                return Fail;
            }

            ulong swappedUInt64 = BinaryPrimitives.ReverseEndianness(ConstantUInt64Input);
            if (swappedUInt64 != ConstantUInt64Expected)
            {
                ReportError("const UInt64", ConstantUInt64Input, swappedUInt64, ConstantUInt64Expected);
                return Fail;
            }

            /*
             * SIGN-EXTENDED VALUE TESTS
             */

            Span<byte> spanInt16 = BitConverter.IsLittleEndian ? s_bufferLE.AsSpan().Slice(2) : s_bufferBE;
            short swappedInt16 = BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<short>(spanInt16));
            if (swappedInt16 != ConstantUInt16Expected)
            {
                ReportError("sign-extended Int16", ConstantUInt16Input, (int)swappedInt16, ConstantUInt16Expected);
                return Fail;
            }

            Span<byte> spanInt32 = BitConverter.IsLittleEndian ? s_bufferLE : s_bufferBE;
            int swappedInt32 = BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<int>(spanInt32));
            if (swappedInt32 != ConstantUInt32Expected)
            {
                ReportError("sign-extended Int32", ConstantUInt32Input, (long)swappedInt32, ConstantUInt32Expected);
                return Fail;
            }

            /*
             * NON-CONST VALUE TESTS
             */

            ushort nonConstUInt16Input = (ushort)DateTime.UtcNow.Ticks;
            ushort nonConstUInt16Output = BinaryPrimitives.ReverseEndianness(nonConstUInt16Input);
            ushort nonConstUInt16Expected = ByteSwapUInt16_Control(nonConstUInt16Input);
            if (nonConstUInt16Output != nonConstUInt16Expected)
            {
                ReportError("non-const UInt16", nonConstUInt16Input, nonConstUInt16Output, nonConstUInt16Expected);
                return Fail;
            }

            uint nonConstUInt32Input = (uint)DateTime.UtcNow.Ticks;
            uint nonConstUInt32Output = BinaryPrimitives.ReverseEndianness(nonConstUInt32Input);
            uint nonConstUInt32Expected = ByteSwapUInt32_Control(nonConstUInt32Input);
            if (nonConstUInt32Output != nonConstUInt32Expected)
            {
                ReportError("non-const UInt32", nonConstUInt32Input, nonConstUInt32Output, nonConstUInt32Expected);
                return Fail;
            }

            ulong nonConstUInt64Input = (ulong)DateTime.UtcNow.Ticks;
            ulong nonConstUInt64Output = BinaryPrimitives.ReverseEndianness(nonConstUInt64Input);
            ulong nonConstUInt64Expected = ByteSwapUInt64_Control(nonConstUInt64Input);
            if (nonConstUInt64Output != nonConstUInt64Expected)
            {
                ReportError("non-const UInt64", nonConstUInt64Input, nonConstUInt64Output, nonConstUInt64Expected);
                return Fail;
            }

            return Pass;
        }

        private static ushort ByteSwapUInt16_Control(ushort value)
        {
            return (ushort)ByteSwapUnsigned_General(value, sizeof(ushort));
        }

        private static uint ByteSwapUInt32_Control(uint value)
        {
            return (uint)ByteSwapUnsigned_General(value, sizeof(uint));
        }

        private static ulong ByteSwapUInt64_Control(ulong value)
        {
            return (ulong)ByteSwapUnsigned_General(value, sizeof(ulong));
        }

        private static ulong ByteSwapUnsigned_General(ulong value, int width)
        {
            // A naive byte swap routine that works on integers of any arbitrary width.
            // Width is specified in bytes.

            ulong retVal = 0;
            do
            {
                retVal = retVal << 8 | (byte)value;
                value >>= 8;
            } while (--width > 0);

            if (value != 0)
            {
                // All bits of value should've been shifted out at this point.
                throw new Exception("Unexpected data width specified - error in test program?");
            }

            return retVal;
        }

        private static string GetHexString<T>(T value)
        {
            if (typeof(T) == typeof(short))
                return ((short)(object)value).ToString("X4");
            if (typeof(T) == typeof(ushort))
                return ((ushort)(object)value).ToString("X4");
            if (typeof(T) == typeof(int))
                return ((int)(object)value).ToString("X8");
            if (typeof(T) == typeof(uint))
                return ((uint)(object)value).ToString("X8");
            if (typeof(T) == typeof(long))
                return ((long)(object)value).ToString("X16");
            if (typeof(T) == typeof(ulong))
                return ((ulong)(object)value).ToString("X16");

            throw new NotSupportedException();
        }

        private static void ReportError<T>(string testName, T input, T output, T expected)
        {
            Console.WriteLine($"BinaryPrimitives.ReverseEndianness({testName}) failed.");
            Console.WriteLine($"Input:    0x{GetHexString(input)}");
            Console.WriteLine($"Output:   0x{GetHexString(output)}");
            Console.WriteLine($"Expected: 0x{GetHexString(expected)}");
        }

    }
}
