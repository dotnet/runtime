// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Buffers.Binary;
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


        static int Main(string[] args)
        {
            /*
             * CONST VALUE TESTS
             */

            ushort swappedUInt16 = BinaryPrimitives.ReverseEndianness(ConstantUInt16Input);
            if (swappedUInt16 != ConstantUInt16Expected)
            {
                Console.WriteLine($"BinaryPrimitives.ReverseEndianness(const UInt16) failed.");
                Console.WriteLine($"Input:    0x{ConstantUInt16Input:X4}");
                Console.WriteLine($"Output:   0x{swappedUInt16:X4}");
                Console.WriteLine($"Expected: 0x{ConstantUInt16Expected:X4}");
            }

            uint swappedUInt32 = BinaryPrimitives.ReverseEndianness(ConstantUInt32Input);
            if (swappedUInt32 != ConstantUInt32Expected)
            {
                Console.WriteLine($"BinaryPrimitives.ReverseEndianness(const UInt32) failed.");
                Console.WriteLine($"Input:    0x{ConstantUInt32Input:X8}");
                Console.WriteLine($"Output:   0x{swappedUInt32:X8}");
                Console.WriteLine($"Expected: 0x{ConstantUInt32Expected:X8}");
            }

            ulong swappedUInt64 = BinaryPrimitives.ReverseEndianness(ConstantUInt64Input);
            if (swappedUInt64 != ConstantUInt64Expected)
            {
                Console.WriteLine($"BinaryPrimitives.ReverseEndianness(const UInt32) failed.");
                Console.WriteLine($"Input:    0x{ConstantUInt64Input:X16}");
                Console.WriteLine($"Output:   0x{swappedUInt64:X16}");
                Console.WriteLine($"Expected: 0x{ConstantUInt64Expected:X16}");
            }

            /*
             * NON-CONST VALUE TESTS
             */

            ushort nonConstUInt16Input = (ushort)DateTime.UtcNow.Ticks;
            ushort nonConstUInt16Output = BinaryPrimitives.ReverseEndianness(nonConstUInt16Input);
            ushort nonConstUInt16Expected = ByteSwapUInt16_Control(nonConstUInt16Input);
            if (nonConstUInt16Output != nonConstUInt16Expected)
            {
                Console.WriteLine($"BinaryPrimitives.ReverseEndianness(non-const UInt16) failed.");
                Console.WriteLine($"Input:    0x{nonConstUInt16Input:X4}");
                Console.WriteLine($"Output:   0x{nonConstUInt16Output:X4}");
                Console.WriteLine($"Expected: 0x{nonConstUInt16Expected:X4}");
            }

            uint nonConstUInt32Input = (uint)DateTime.UtcNow.Ticks;
            uint nonConstUInt32Output = BinaryPrimitives.ReverseEndianness(nonConstUInt32Input);
            uint nonConstUInt32Expected = ByteSwapUInt32_Control(nonConstUInt32Input);
            if (nonConstUInt32Output != nonConstUInt32Expected)
            {
                Console.WriteLine($"BinaryPrimitives.ReverseEndianness(non-const UInt32) failed.");
                Console.WriteLine($"Input:    0x{nonConstUInt32Input:X8}");
                Console.WriteLine($"Output:   0x{nonConstUInt32Output:X8}");
                Console.WriteLine($"Expected: 0x{nonConstUInt32Expected:X8}");
            }

            ulong nonConstUInt64Input = (ulong)DateTime.UtcNow.Ticks;
            ulong nonConstUInt64Output = BinaryPrimitives.ReverseEndianness(nonConstUInt64Input);
            ulong nonConstUInt64Expected = ByteSwapUInt64_Control(nonConstUInt64Input);
            if (nonConstUInt64Output != nonConstUInt64Expected)
            {
                Console.WriteLine($"BinaryPrimitives.ReverseEndianness(non-const UInt64) failed.");
                Console.WriteLine($"Input:    0x{nonConstUInt64Input:X16}");
                Console.WriteLine($"Output:   0x{nonConstUInt64Output:X16}");
                Console.WriteLine($"Expected: 0x{nonConstUInt64Expected:X16}");
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
    }
}
