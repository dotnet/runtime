// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Numerics.Tests
{
    public class op_rightshiftTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void BigShiftsTest()
        {
            BigInteger a = new BigInteger(1);
            BigInteger b = new BigInteger(Math.Pow(2, 31));

            for (int i = 0; i < 100; i++)
            {
                BigInteger a1 = (a << (i + 31));
                BigInteger a2 = a1 >> i;

                Assert.Equal(b, a2);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))] // May fail on 32-bit due to a large memory requirement
        public static void LargeNegativeBigIntegerShiftTest()
        {
            // Create a very large negative BigInteger
            BigInteger bigInt = new BigInteger(-1) << int.MaxValue;
            Assert.Equal(2147483647, bigInt.GetBitLength());
            Assert.Equal(-1, bigInt.Sign);

            // Validate internal representation.
            // At this point, bigInt should be a 1 followed by int.MaxValue zeros.
            // Given this, bigInt._bits is expected to be structured as follows:
            // - _bits.Length == (int.MaxValue + 1) / (8 * sizeof(uint))
            // - First (_bits.Length - 1) elements: 0x00000000
            // - Last element: 0x80000000
            //                   ^------ (There's the leading '1')

            Assert.Equal(((uint)int.MaxValue + 1) / (8 * sizeof(uint)), (uint)bigInt._bits.Length);

            uint i = 0;
            for (; i < (bigInt._bits.Length - 1); i++) {
                Assert.Equal(0x00000000u, bigInt._bits[i]);
            }

            Assert.Equal(0x80000000u, bigInt._bits[i]);

            // Right shift the BigInteger
            BigInteger shiftedBigInt = bigInt >> 1;
            Assert.Equal(2147483646, shiftedBigInt.GetBitLength());
            Assert.Equal(-1, shiftedBigInt.Sign);

            // Validate internal representation.
            // At this point, shiftedBigInt should be a 1 followed by int.MaxValue - 1 zeros.
            // Given this, shiftedBigInt._bits is expected to be structured as follows:
            // - _bits.Length == (int.MaxValue + 1) / (8 * sizeof(uint))
            // - First (_bits.Length - 1) elements: 0x00000000
            // - Last element: 0x40000000
            //                   ^------ (the '1' is now one position to the right)

            Assert.Equal(((uint)int.MaxValue + 1) / (8 * sizeof(uint)), (uint)shiftedBigInt._bits.Length);

            i = 0;
            for (; i < (shiftedBigInt._bits.Length - 1); i++) {
                Assert.Equal(0x00000000u, shiftedBigInt._bits[i]);
            }

            Assert.Equal(0x40000000u, shiftedBigInt._bits[i]);
        }

        [Fact]
        public static void RunRightShiftTests()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // RightShift Method - Large BigIntegers - large + Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomPosByteArray(s_random, 2);
                VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>");
            }

            // RightShift Method - Large BigIntegers - small + Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { (byte)s_random.Next(1, 32) };
                VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>");
            }

            // RightShift Method - Large BigIntegers - 32 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { (byte)32 };
                VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>");
            }

            // RightShift Method - All One Uint Large BigIntegers - 32 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomLengthAllOnesUIntByteArray(s_random);
                tempByteArray2 = new byte[] { (byte)32 };
                VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>");
            }

            // RightShift Method - Uint 0xffffffff 0x8000000 ... Large BigIntegers - 32 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomLengthFirstUIntMaxSecondUIntMSBMaxArray(s_random);
                tempByteArray2 = new byte[] { (byte)32 };
                VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>");
            }

            // RightShift Method - Large BigIntegers - large - Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomNegByteArray(s_random, 2);
                VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>");
            }

            // RightShift Method - Large BigIntegers - small - Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { unchecked((byte)s_random.Next(-31, 0)) };
                VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>");
            }

            // RightShift Method - Large BigIntegers - -32 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { (byte)0xe0 };
                VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>");
            }

            // RightShift Method - Large BigIntegers - 0 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { (byte)0 };
                VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>");
            }

            // RightShift Method - Small BigIntegers - large + Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomPosByteArray(s_random, 2);
                VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>");
            }

            // RightShift Method - Small BigIntegers - small + Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { (byte)s_random.Next(1, 32) };
                VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>");
            }

            // RightShift Method - Small BigIntegers - 32 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { (byte)32 };
                VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>");
            }
            // RightShift Method - Small BigIntegers - large - Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomNegByteArray(s_random, 2);
                VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>");
            }

            // RightShift Method - Small BigIntegers - small - Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { unchecked((byte)s_random.Next(-31, 0)) };
                VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>");
            }

            // RightShift Method - Small BigIntegers - -32 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { (byte)0xe0 };
                VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>");
            }

            // RightShift Method - Small BigIntegers - 0 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { (byte)0 };
                VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>");
            }

            // RightShift Method - Positive BigIntegers - Shift to 0
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomPosByteArray(s_random, 100);
                tempByteArray2 = BitConverter.GetBytes(s_random.Next(8 * tempByteArray1.Length, 1000));
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(tempByteArray2);
                }
                VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>");
            }

            // RightShift Method - Negative BigIntegers - Shift to -1
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomNegByteArray(s_random, 100);
                tempByteArray2 = BitConverter.GetBytes(s_random.Next(8 * tempByteArray1.Length, 1000));
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(tempByteArray2);
                }
                VerifyRightShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b>>");
            }
        }

        private static void VerifyRightShiftString(string opstring)
        {
            StackCalc sc = new StackCalc(opstring);
            while (sc.DoNextOperation())
            {
                Assert.Equal(sc.snCalc.Peek().ToString(), sc.myCalc.Peek().ToString());
            }
        }

        private static byte[] GetRandomByteArray(Random random)
        {
            return GetRandomByteArray(random, random.Next(0, 1024));
        }

        private static byte[] GetRandomByteArray(Random random, int size)
        {
            return MyBigIntImp.GetRandomByteArray(random, size);
        }

        private static byte[] GetRandomPosByteArray(Random random, int size)
        {
            byte[] value = new byte[size];

            for (int i = 0; i < value.Length; ++i)
            {
                value[i] = (byte)random.Next(0, 256);
            }
            value[value.Length - 1] &= 0x7F;

            return value;
        }

        private static byte[] GetRandomNegByteArray(Random random, int size)
        {
            byte[] value = new byte[size];

            for (int i = 0; i < value.Length; ++i)
            {
                value[i] = (byte)random.Next(0, 256);
            }
            value[value.Length - 1] |= 0x80;

            return value;
        }

        private static byte[] GetRandomLengthAllOnesUIntByteArray(Random random)
        {
            int gap = random.Next(0, 128);
            int byteLength = 4 + gap * 4 + 1;
            byte[] array = new byte[byteLength];
            array[0] = 1;
            array[^1] = 0xFF;
            return array;
        }
        private static byte[] GetRandomLengthFirstUIntMaxSecondUIntMSBMaxArray(Random random)
        {
            int gap = random.Next(0, 128);
            int byteLength = 4 + gap * 4 + 1;
            byte[] array = new byte[byteLength];
            array[^6] = 0x80;
            array[^1] = 0xFF;
            return array;
        }

        private static string Print(byte[] bytes)
        {
            return MyBigIntImp.Print(bytes);
        }
    }
}
