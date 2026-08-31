// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Numerics.Tests
{
    public class op_leftshiftTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunLeftShiftTests()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // LeftShift Method - Large BigIntegers - large + Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomPosByteArray(s_random, 2);
                VerifyLeftShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b<<");
            }

            // LeftShift Method - Large BigIntegers - small + Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { (byte)s_random.Next(1, 32) };
                VerifyLeftShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b<<");
            }

            // LeftShift Method - Large BigIntegers - 32 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { (byte)32 };
                VerifyLeftShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b<<");
            }
            // LeftShift Method - Large BigIntegers - large - Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomNegByteArray(s_random, 2);
                VerifyLeftShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b<<");
            }

            // LeftShift Method - Large BigIntegers - small - Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { unchecked((byte)s_random.Next(-31, 0)) };
                VerifyLeftShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b<<");
            }

            // LeftShift Method - Large BigIntegers - -32 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { (byte)0xe0 };
                VerifyLeftShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b<<");
            }

            // LeftShift Method - Large BigIntegers - 0 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { (byte)0 };
                VerifyLeftShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b<<");
            }

            // LeftShift Method - Small BigIntegers - large + Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomPosByteArray(s_random, 2);
                VerifyLeftShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b<<");
            }

            // LeftShift Method - Small BigIntegers - small + Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { (byte)s_random.Next(1, 32) };
                VerifyLeftShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b<<");
            }

            // LeftShift Method - Small BigIntegers - 32 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { (byte)32 };
                VerifyLeftShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b<<");
            }
            // LeftShift Method - Small BigIntegers - large - Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomNegByteArray(s_random, 2);
                VerifyLeftShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b<<");
            }

            // LeftShift Method - Small BigIntegers - small - Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { unchecked((byte)s_random.Next(-31, 0)) };
                VerifyLeftShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b<<");
            }

            // LeftShift Method - Small BigIntegers - -32 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { (byte)0xe0 };
                VerifyLeftShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b<<");
            }

            // LeftShift Method - Small BigIntegers - 0 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { (byte)0 };
                VerifyLeftShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b<<");
            }

            // LeftShift Method - Positive BigIntegers - Shift to 0
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomPosByteArray(s_random, 100);
                tempByteArray2 = BitConverter.GetBytes(s_random.Next(-1000, -8 * tempByteArray1.Length));
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(tempByteArray2);
                }
                VerifyLeftShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b<<");
            }

            // LeftShift Method - Negative BigIntegers - Shift to -1
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomNegByteArray(s_random, 100);
                tempByteArray2 = BitConverter.GetBytes(s_random.Next(-1000, -8 * tempByteArray1.Length));
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(tempByteArray2);
                }
                VerifyLeftShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b<<");
            }
        }

        [Fact]
        public void RunSmallTests()
        {
            foreach (int i in new int[] {
                    0,
                    1,
                    16,
                    31,
                    32,
                    33,
                    63,
                    64,
                    65,
                    100,
                    127,
                    128,
            })
            {
                foreach (int shift in new int[] {
                    0,
                    -1, 1,
                    -16, 16,
                    -31, 31,
                    -32, 32,
                    -33, 33,
                    -63, 63,
                    -64, 64,
                    -65, 65,
                    -100, 100,
                    -127, 127,
                    -128, 128,
                })
                {
                    var num = Int128.One << i;
                    for (int k = -1; k <= 1; k++)
                    {
                        foreach (int sign in new int[] { -1, +1 })
                        {
                            Int128 value128 = sign * (num + k);

                            byte[] tempByteArray1 = GetRandomSmallByteArray(value128);
                            byte[] tempByteArray2 = GetRandomSmallByteArray(shift);

                            VerifyLeftShiftString(Print(tempByteArray2) + Print(tempByteArray1) + "b<<");
                        }
                    }
                }
            }
        }

        private static void VerifyLeftShiftString(string opstring)
        {
            StackCalc sc = new StackCalc(opstring);
            while (sc.DoNextOperation())
            {
                Assert.Equal(sc.snCalc.Peek().ToString(), sc.myCalc.Peek().ToString());
            }
        }

        private static byte[] GetRandomSmallByteArray(Int128 num)
        {
            byte[] value = new byte[16];

            for (int i = 0; i < value.Length; i++)
            {
                value[i] = (byte)num;
                num >>= 8;
            }

            return value;
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

        private static string Print(byte[] bytes)
        {
            return MyBigIntImp.Print(bytes);
        }
    }
}
