// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Numerics.Tests
{
    public abstract class RotateTestBase
    {
        public abstract string opstring { get; }
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public void RunRotateTests()
        {
            byte[] tempByteArray1;
            byte[] tempByteArray2;

            // Rotate Method - Large BigIntegers - large + Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomPosByteArray(s_random, 2);
                VerifyRotateString(Print(tempByteArray2) + Print(tempByteArray1) + opstring);
            }

            // Rotate Method - Large BigIntegers - small + Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { (byte)s_random.Next(1, 32) };
                VerifyRotateString(Print(tempByteArray2) + Print(tempByteArray1) + opstring);
            }

            // Rotate Method - Large BigIntegers - 32 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { (byte)32 };
                VerifyRotateString(Print(tempByteArray2) + Print(tempByteArray1) + opstring);
            }

            // Rotate Method - All One Uint Large BigIntegers - 32 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomLengthAllOnesUIntByteArray(s_random);
                tempByteArray2 = new byte[] { (byte)32 };
                VerifyRotateString(Print(tempByteArray2) + Print(tempByteArray1) + opstring);
            }

            // Rotate Method - Uint 0xffffffff 0x8000000 ... Large BigIntegers - 32 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomLengthFirstUIntMaxSecondUIntMSBMaxArray(s_random);
                tempByteArray2 = new byte[] { (byte)32 };
                VerifyRotateString(Print(tempByteArray2) + Print(tempByteArray1) + opstring);
            }

            // Rotate Method - Large BigIntegers - large - Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomNegByteArray(s_random, 2);
                VerifyRotateString(Print(tempByteArray2) + Print(tempByteArray1) + opstring);
            }

            // Rotate Method - Large BigIntegers - small - Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { unchecked((byte)s_random.Next(-31, 0)) };
                VerifyRotateString(Print(tempByteArray2) + Print(tempByteArray1) + opstring);
            }

            // Rotate Method - Large BigIntegers - -32 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { (byte)0xe0 };
                VerifyRotateString(Print(tempByteArray2) + Print(tempByteArray1) + opstring);
            }

            // Rotate Method - Large BigIntegers - 0 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { (byte)0 };
                VerifyRotateString(Print(tempByteArray2) + Print(tempByteArray1) + opstring);
            }

            // Rotate Method - Small BigIntegers - large + Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomPosByteArray(s_random, 2);
                VerifyRotateString(Print(tempByteArray2) + Print(tempByteArray1) + opstring);
            }

            // Rotate Method - Small BigIntegers - small + Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { (byte)s_random.Next(1, 32) };
                VerifyRotateString(Print(tempByteArray2) + Print(tempByteArray1) + opstring);
            }

            // Rotate Method - Small BigIntegers - 32 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { (byte)32 };
                VerifyRotateString(Print(tempByteArray2) + Print(tempByteArray1) + opstring);
            }
            // Rotate Method - Small BigIntegers - large - Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomNegByteArray(s_random, 2);
                VerifyRotateString(Print(tempByteArray2) + Print(tempByteArray1) + opstring);
            }

            // Rotate Method - Small BigIntegers - small - Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { unchecked((byte)s_random.Next(-31, 0)) };
                VerifyRotateString(Print(tempByteArray2) + Print(tempByteArray1) + opstring);
            }

            // Rotate Method - Small BigIntegers - -32 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { (byte)0xe0 };
                VerifyRotateString(Print(tempByteArray2) + Print(tempByteArray1) + opstring);
            }

            // Rotate Method - Small BigIntegers - 0 bit Shift
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { (byte)0 };
                VerifyRotateString(Print(tempByteArray2) + Print(tempByteArray1) + opstring);
            }

            // Rotate Method - Positive BigIntegers - Shift to 0
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomPosByteArray(s_random, 100);
                tempByteArray2 = BitConverter.GetBytes(s_random.Next(8 * tempByteArray1.Length, 1000));
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(tempByteArray2);
                }
                VerifyRotateString(Print(tempByteArray2) + Print(tempByteArray1) + opstring);
            }

            // Rotate Method - Negative BigIntegers - Shift to -1
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomNegByteArray(s_random, 100);
                tempByteArray2 = BitConverter.GetBytes(s_random.Next(8 * tempByteArray1.Length, 1000));
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(tempByteArray2);
                }
                VerifyRotateString(Print(tempByteArray2) + Print(tempByteArray1) + opstring);
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

                            VerifyRotateString(Print(tempByteArray2) + Print(tempByteArray1) + opstring);
                        }
                    }
                }
            }
        }

        private static void VerifyRotateString(string opstring)
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
            array[^5] = 0x80;
            array[^1] = 0xFF;
            return array;
        }

        private static string Print(byte[] bytes)
        {
            return MyBigIntImp.Print(bytes);
        }
    }

    public class RotateLeftTest : RotateTestBase
    {
        public override string opstring => "bRotateLeft";

        [Fact]
        public void PowerOfTwo()
        {
            for (int i = 0; i < 32; i++)
            {
                foreach (int k in new int[] { 1, 2, 10 })
                {
                    Assert.Equal(BigInteger.One << (32 * (k - 1) + i), BigInteger.RotateLeft(BigInteger.One << (32 * k + i), 32 * k));
                    Assert.Equal((new BigInteger(uint.MaxValue << i)) << (32 * (k - 1)), BigInteger.RotateLeft(BigInteger.MinusOne << (32 * k + i), 32 * k));
                }
            }
        }
    }

    public class RotateRightTest : RotateTestBase
    {
        public override string opstring => "bRotateRight";

        [Fact]
        public void PowerOfTwo()
        {
            for (int i = 0; i < 32; i++)
            {
                foreach (int k in new int[] { 1, 2, 10 })
                {
                    Assert.Equal(BigInteger.One << i, BigInteger.RotateRight(BigInteger.One << (32 * k + i), 32 * k));
                    Assert.Equal(new BigInteger(uint.MaxValue << i), BigInteger.RotateRight(BigInteger.MinusOne << (32 * k + i), 32 * k));
                }
            }
        }
    }
}
