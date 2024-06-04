// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal;
using Xunit;

namespace BinaryPrimitivesReverseEndianness
{
    public class Program
    {
        public const int Pass = 100;
        public const int Fail = 0;

        private const ushort ConstantUInt16Input = 0x9876;
        private const ushort ConstantUInt16Expected = 0x7698;

        private const uint ConstantUInt32Input = 0x98765432;
        private const uint ConstantUInt32Expected = 0x32547698;

        private const long ConstantInt64Input = 0x1edcba9876543210;
        private const long ConstantInt64Expected = 0x1032547698badc1e;

        private const ulong ConstantUInt64Input = 0xfedcba9876543210;
        private const ulong ConstantUInt64Expected = 0x1032547698badcfe;

        private static readonly byte[] s_bufferLE = new byte[] { 0x32, 0x54, 0x76, 0x98 };
        private static readonly byte[] s_bufferBE = new byte[] { 0x98, 0x76, 0x54, 0x32 };

        private static readonly byte[] s_bufferLESigned64 = new byte[] { 0x10, 0x32, 0x54, 0x76, 0x98, 0xba, 0xdc, 0x1e };
        private static readonly byte[] s_bufferBESigned64 = new byte[] { 0x1e, 0xdc, 0xba, 0x98, 0x76, 0x54, 0x32, 0x10 };

        [Fact]
        public static int TestEntryPoint()
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

            Span<byte> spanInt16 = BitConverter.IsLittleEndian ? s_bufferLE.AsSpan(2) : s_bufferBE;
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

            Span<byte> spanInt64 = BitConverter.IsLittleEndian ? s_bufferLESigned64 : s_bufferBESigned64;
            long swappedSpanInt64 = BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<long>(spanInt64));
            if (swappedSpanInt64 != ConstantInt64Expected)
            {
                ReportError("sign-extended Int64", ConstantInt64Input, swappedSpanInt64, ConstantInt64Expected);
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

            /*
             * READ TESTS CAST
             */

            byte readCastUInt16Input = (byte)DateTime.UtcNow.Ticks;
            ushort readCastUInt16Output = ByteSwapUInt16_ReadCast(ref readCastUInt16Input);
            ushort readCastUInt16Expected = ByteSwapUInt16_Control(readCastUInt16Input);
            if (readCastUInt16Output != readCastUInt16Expected)
            {
                ReportError("read cast UInt16", readCastUInt16Input, readCastUInt16Output, readCastUInt16Expected);
                return Fail;
            }

            ushort readCastUInt32Input = (ushort)DateTime.UtcNow.Ticks;
            uint readCastUInt32Output = ByteSwapUInt32_ReadCast(ref readCastUInt32Input);
            uint readCastUInt32Expected = ByteSwapUInt32_Control(readCastUInt32Input);
            if (readCastUInt32Output != readCastUInt32Expected)
            {
                ReportError("read cast UInt32", readCastUInt32Input, readCastUInt32Output, readCastUInt32Expected);
                return Fail;
            }

            uint readCastUInt64Input = (uint)DateTime.UtcNow.Ticks;
            ulong readCastUInt64Output = ByteSwapUInt64_ReadCast(ref readCastUInt64Input);
            ulong readCastUInt64Expected = ByteSwapUInt64_Control(readCastUInt64Input);
            if (readCastUInt64Output != readCastUInt64Expected)
            {
                ReportError("read cast UInt64", readCastUInt64Input, readCastUInt64Output, readCastUInt64Expected);
                return Fail;
            }

            /*
             * WRITE TESTS
             */

            ushort writeUInt16Output = default;
            ushort writeUInt16Input = ByteSwapUInt16_Write(ref writeUInt16Output);
            ushort writeUInt16Expected = ByteSwapUInt16_Control(writeUInt16Input);
            if (writeUInt16Output != writeUInt16Expected)
            {
                ReportError("write UInt16", writeUInt16Input, writeUInt16Output, writeUInt16Expected);
                return Fail;
            }

            uint writeUInt32Output = default;
            uint writeUInt32Input = ByteSwapUInt32_Write(ref writeUInt32Output);
            uint writeUInt32Expected = ByteSwapUInt32_Control(writeUInt32Input);
            if (writeUInt32Output != writeUInt32Expected)
            {
                ReportError("write UInt32", writeUInt32Input, writeUInt32Output, writeUInt32Expected);
                return Fail;
            }

            ulong writeUInt64Output = default;
            ulong writeUInt64Input = ByteSwapUInt64_Write(ref writeUInt64Output);
            ulong writeUInt64Expected = ByteSwapUInt64_Control(writeUInt64Input);
            if (writeUInt64Output != writeUInt64Expected)
            {
                ReportError("write UInt64", writeUInt64Input, writeUInt64Output, writeUInt64Expected);
                return Fail;
            }

            /*
             * WRITE TESTS LEA
             */

            ushort writeLeaUInt16Output = default;
            ushort writeLeaUInt16Input = ByteSwapUInt16_WriteLea(ref writeLeaUInt16Output, 0);
            ushort writeLeaUInt16Expected = ByteSwapUInt16_Control(writeLeaUInt16Input);
            if (writeLeaUInt16Output != writeLeaUInt16Expected)
            {
                ReportError("write lea UInt16", writeLeaUInt16Input, writeLeaUInt16Output, writeLeaUInt16Expected);
                return Fail;
            }

            uint writeLeaUInt32Output = default;
            uint writeLeaUInt32Input = ByteSwapUInt32_WriteLea(ref writeLeaUInt32Output, 0);
            uint writeLeaUInt32Expected = ByteSwapUInt32_Control(writeLeaUInt32Input);
            if (writeLeaUInt32Output != writeLeaUInt32Expected)
            {
                ReportError("write lea UInt32", writeLeaUInt32Input, writeLeaUInt32Output, writeLeaUInt32Expected);
                return Fail;
            }

            ulong writeLeaUInt64Output = default;
            ulong writeLeaUInt64Input = ByteSwapUInt64_WriteLea(ref writeLeaUInt64Output, 0);
            ulong writeLeaUInt64Expected = ByteSwapUInt64_Control(writeLeaUInt64Input);
            if (writeLeaUInt64Output != writeLeaUInt64Expected)
            {
                ReportError("write lea UInt64", writeLeaUInt64Input, writeLeaUInt64Output, writeLeaUInt64Expected);
                return Fail;
            }

            /*
             * WRITE TESTS CAST
             */

            ulong writeCastUInt8Input = (ulong)DateTime.UtcNow.Ticks;
            byte writeCastUInt8Output = default;
            ByteSwapUInt8_WriteCast(ref writeCastUInt8Output, writeCastUInt8Input);
            ulong writeCastUInt8Expected = (byte)ByteSwapUInt64_Control(writeCastUInt8Input);
            if (writeCastUInt8Output != writeCastUInt8Expected)
            {
                ReportError("write cast UInt8", writeCastUInt8Input, writeCastUInt8Output, writeCastUInt8Expected);
                return Fail;
            }

            ulong writeCastUInt16Input = (ulong)DateTime.UtcNow.Ticks;
            ushort writeCastUInt16Output = default;
            ByteSwapUInt16_WriteCast(ref writeCastUInt16Output, writeCastUInt16Input);
            ulong writeCastUInt16Expected = (ushort)ByteSwapUInt64_Control(writeCastUInt16Input);
            if (writeCastUInt16Output != writeCastUInt16Expected)
            {
                ReportError("write cast UInt16", writeCastUInt16Input, writeCastUInt16Output, writeCastUInt16Expected);
                return Fail;
            }

            ulong writeCastUInt32Input = (ulong)DateTime.UtcNow.Ticks;
            uint writeCastUInt32Output = default;
            ByteSwapUInt32_WriteCast(ref writeCastUInt32Output, writeCastUInt32Input);
            ulong writeCastUInt32Expected = (uint)ByteSwapUInt64_Control(writeCastUInt32Input);
            if (writeCastUInt32Output != writeCastUInt32Expected)
            {
                ReportError("write cast UInt32", writeCastUInt32Input, writeCastUInt32Output, writeCastUInt32Expected);
                return Fail;
            }

            /*
             * READ & WRITE TESTS
             */

            ushort writeBackUInt16Input = (ushort)DateTime.UtcNow.Ticks;
            ushort writeBackUInt16Output = writeBackUInt16Input;
            ByteSwapUInt16_WriteBack(ref writeBackUInt16Output);
            ushort writeBackUInt16Expected = ByteSwapUInt16_Control(writeBackUInt16Input);
            if (writeBackUInt16Output != writeBackUInt16Expected)
            {
                ReportError("write back UInt16", writeBackUInt16Input, writeBackUInt16Output, writeBackUInt16Expected);
                return Fail;
            }

            uint writeBackUInt32Input = (uint)DateTime.UtcNow.Ticks;
            uint writeBackUInt32Output = writeBackUInt32Input;
            ByteSwapUInt32_WriteBack(ref writeBackUInt32Output);
            uint writeBackUInt32Expected = ByteSwapUInt32_Control(writeBackUInt32Input);
            if (writeBackUInt32Output != writeBackUInt32Expected)
            {
                ReportError("write back UInt32", writeBackUInt32Input, writeBackUInt32Output, writeBackUInt32Expected);
                return Fail;
            }

            ulong writeBackUInt64Input = (ulong)DateTime.UtcNow.Ticks;
            ulong writeBackUInt64Output = writeBackUInt64Input;
            ByteSwapUInt64_WriteBack(ref writeBackUInt64Output);
            ulong writeBackUInt64Expected = ByteSwapUInt64_Control(writeBackUInt64Input);
            if (writeBackUInt64Output != writeBackUInt64Expected)
            {
                ReportError("write back UInt64", writeBackUInt64Input, writeBackUInt64Output, writeBackUInt64Expected);
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ushort ByteSwapUInt16_ReadCast(ref byte input)
        {
            return BinaryPrimitives.ReverseEndianness((ushort)input);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint ByteSwapUInt32_ReadCast(ref ushort input)
        {
            return BinaryPrimitives.ReverseEndianness((uint)input);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ulong ByteSwapUInt64_ReadCast(ref uint input)
        {
            return BinaryPrimitives.ReverseEndianness((ulong)input);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ushort ByteSwapUInt16_Write(ref ushort output)
        {
            ushort input = (ushort)DateTime.UtcNow.Ticks;

            output = BinaryPrimitives.ReverseEndianness(input);

            return input;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint ByteSwapUInt32_Write(ref uint output)
        {
            uint input = (uint)DateTime.UtcNow.Ticks;

            output = BinaryPrimitives.ReverseEndianness(input);

            return input;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ulong ByteSwapUInt64_Write(ref ulong output)
        {
            ulong input = (ulong)DateTime.UtcNow.Ticks;

            output = BinaryPrimitives.ReverseEndianness(input);

            return input;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ushort ByteSwapUInt16_WriteLea(ref ushort output, int offset)
        {
            ushort input = (ushort)DateTime.UtcNow.Ticks;

            Unsafe.Add(ref output, offset) = BinaryPrimitives.ReverseEndianness(input);

            return input;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint ByteSwapUInt32_WriteLea(ref uint output, int offset)
        {
            uint input = (uint)DateTime.UtcNow.Ticks;

            Unsafe.Add(ref output, offset) = BinaryPrimitives.ReverseEndianness(input);

            return input;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ulong ByteSwapUInt64_WriteLea(ref ulong output, int offset)
        {
            ulong input = (ulong)DateTime.UtcNow.Ticks;

            Unsafe.Add(ref output, offset) = BinaryPrimitives.ReverseEndianness(input);

            return input;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ByteSwapUInt16_WriteBack(ref ushort value)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ByteSwapUInt32_WriteBack(ref uint value)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ByteSwapUInt64_WriteBack(ref ulong value)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ByteSwapUInt8_WriteCast(ref byte output, ulong input)
        {
            output = (byte)BinaryPrimitives.ReverseEndianness(input);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ByteSwapUInt16_WriteCast(ref ushort output, ulong input)
        {
            output = (ushort)BinaryPrimitives.ReverseEndianness(input);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ByteSwapUInt32_WriteCast(ref uint output, ulong input)
        {
            output = (uint)BinaryPrimitives.ReverseEndianness(input);
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
