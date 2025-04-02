// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
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


        public static TheoryData<BigInteger, int, BigInteger> NegativeNumber_TestData = new TheoryData<BigInteger, int, BigInteger>
        {

            {
                new BigInteger(unchecked((long)0xFFFF_FFFF_0000_0000)),
                1,
                new BigInteger(unchecked((long)0xFFFF_FFFE_0000_0001))
            },
            {
                new BigInteger(unchecked((long)0xFFFF_FFFF_0000_0000)),
                2,
                new BigInteger(unchecked((long)0xFFFF_FFFC_0000_0003))
            },
            {
                new BigInteger(unchecked((long)0xFFFF_FFFF_0000_0001)),
                1,
                new BigInteger(unchecked((long)0xFFFF_FFFE_0000_0003))
            },
            {
                new BigInteger(unchecked((long)0xFFFF_FFFF_0000_0001)),
                2,
                new BigInteger(unchecked((long)0xFFFF_FFFC_0000_0007))
            },
            {
                new BigInteger(unchecked((long)0xFFFF_FFFF_0000_0002)),
                1,
                new BigInteger(unchecked((long)0xFFFF_FFFE_0000_0005))
            },
            {
                new BigInteger(unchecked((long)0xFFFF_FFFF_0000_0002)),
                2,
                new BigInteger(unchecked((long)0xFFFF_FFFC_0000_000B))
            },

            {
                new BigInteger(unchecked((long)0x8000_0000_0000_0000)),
                1,
                new BigInteger(0x1)
            },
            {
                new BigInteger(unchecked((long)0x8000_0000_0000_0000)),
                2,
                new BigInteger(0x2)
            },
            {
                new BigInteger(unchecked((long)0x8000_0000_0000_0001)),
                1,
                new BigInteger(0x3)
            },
            {
                new BigInteger(unchecked((long)0x8000_0000_0000_0001)),
                2,
                new BigInteger(0x6)
            },
            {
                new BigInteger(unchecked((long)0x8000_0000_0000_0002)),
                1,
                new BigInteger(0x5)
            },
            {
                new BigInteger(unchecked((long)0x8000_0000_0000_0002)),
                2,
                new BigInteger(0xA)
            },

            {
                BigInteger.Parse("8000_0000_0000_0000_0000_0000".Replace("_", ""), NumberStyles.HexNumber),
                1,
                new BigInteger(0x1)
            },
            {
                BigInteger.Parse("8000_0000_0000_0000_0000_0000".Replace("_", ""), NumberStyles.HexNumber),
                2,
                new BigInteger(0x2)
            },
            {
                BigInteger.Parse("8000_0000_0000_0000_0000_0001".Replace("_", ""), NumberStyles.HexNumber),
                1,
                new BigInteger(0x3)
            },
            {
                BigInteger.Parse("8000_0000_0000_0000_0000_0001".Replace("_", ""), NumberStyles.HexNumber),
                2,
                new BigInteger(0x6)
            },
            {
                BigInteger.Parse("8000_0000_0000_0000_0000_0002".Replace("_", ""), NumberStyles.HexNumber),
                1,
                new BigInteger(0x5)
            },
            {
                BigInteger.Parse("8000_0000_0000_0000_0000_0002".Replace("_", ""), NumberStyles.HexNumber),
                2,
                new BigInteger(0xA)
            },

            {
                BigInteger.Parse("________F_0000_0000_0000_0000_0000_0000".Replace("_", ""), NumberStyles.HexNumber),
                1,
                BigInteger.Parse("________E_0000_0000_0000_0000_0000_0001".Replace("_", ""), NumberStyles.HexNumber)
            },
            {
                BigInteger.Parse("________F_0000_0000_0000_0000_0000_0000".Replace("_", ""), NumberStyles.HexNumber),
                2,
                BigInteger.Parse("________C_0000_0000_0000_0000_0000_0003".Replace("_", ""), NumberStyles.HexNumber)
            },
            {
                BigInteger.Parse("________F_0000_0000_0000_0000_0000_0001".Replace("_", ""), NumberStyles.HexNumber),
                1,
                BigInteger.Parse("________E_0000_0000_0000_0000_0000_0003".Replace("_", ""), NumberStyles.HexNumber)
            },
            {
                BigInteger.Parse("________F_0000_0000_0000_0000_0000_0001".Replace("_", ""), NumberStyles.HexNumber),
                2,
                BigInteger.Parse("________C_0000_0000_0000_0000_0000_0007".Replace("_", ""), NumberStyles.HexNumber)
            },
            {
                BigInteger.Parse("________F_0000_0000_0000_0000_0000_0002".Replace("_", ""), NumberStyles.HexNumber),
                1,
                BigInteger.Parse("________E_0000_0000_0000_0000_0000_0005".Replace("_", ""), NumberStyles.HexNumber)
            },
            {
                BigInteger.Parse("________F_0000_0000_0000_0000_0000_0002".Replace("_", ""), NumberStyles.HexNumber),
                2,
                BigInteger.Parse("________C_0000_0000_0000_0000_0000_000B".Replace("_", ""), NumberStyles.HexNumber)
            },
        };

        [Theory]
        [MemberData(nameof(NegativeNumber_TestData))]
        public void NegativeNumber(BigInteger input, int rotateAmount, BigInteger expected)
        {
            Assert.Equal(expected, BigInteger.RotateLeft(input, rotateAmount));
        }

        [Fact]
        public void PowerOfTwo()
        {
            for (int i = 0; i < 32; i++)
            {
                foreach (int k in new int[] { 1, 2, 3, 10 })
                {
                    BigInteger plus = BigInteger.One << (32 * k + i);
                    BigInteger minus = BigInteger.MinusOne << (32 * k + i);

                    Assert.Equal(BigInteger.One << (i == 31 ? 0 : (32 * k + i + 1)), BigInteger.RotateLeft(plus, 1));
                    Assert.Equal(BigInteger.One << i, BigInteger.RotateLeft(plus, 32));
                    Assert.Equal(BigInteger.One << (32 * (k - 1) + i), BigInteger.RotateLeft(plus, 32 * k));

                    Assert.Equal(i == 31 ? BigInteger.One : (new BigInteger(-1 << (i + 1)) << 32 * k) + 1,
                        BigInteger.RotateLeft(minus, 1));
                    Assert.Equal(new BigInteger(uint.MaxValue << i), BigInteger.RotateLeft(minus, 32));
                    Assert.Equal(new BigInteger(uint.MaxValue << i) << (32 * (k - 1)), BigInteger.RotateLeft(minus, 32 * k));
                }
            }
        }
    }

    public class RotateRightTest : RotateTestBase
    {
        public override string opstring => "bRotateRight";

        public static TheoryData<BigInteger, int, BigInteger> NegativeNumber_TestData = new TheoryData<BigInteger, int, BigInteger>
        {

            {
                new BigInteger(unchecked((long)0xFFFF_FFFF_0000_0000)),
                1,
                new BigInteger(unchecked((long)0x7FFF_FFFF_8000_0000))
            },
            {
                new BigInteger(unchecked((long)0xFFFF_FFFF_0000_0000)),
                2,
                new BigInteger(unchecked((long)0x3FFF_FFFF_C000_0000))
            },
            {
                new BigInteger(unchecked((long)0xFFFF_FFFF_0000_0001)),
                1,
                new BigInteger(unchecked((int)0x8000_0000))
            },
            {
                new BigInteger(unchecked((long)0xFFFF_FFFF_0000_0001)),
                2,
                new BigInteger(unchecked((long)0x7FFF_FFFF_C000_0000))
            },
            {
                new BigInteger(unchecked((long)0xFFFF_FFFF_0000_0002)),
                1,
                new BigInteger(unchecked((long)0x7FFF_FFFF_8000_0001))
            },
            {
                new BigInteger(unchecked((long)0xFFFF_FFFF_0000_0002)),
                2,
                new BigInteger(unchecked((long)0xBFFF_FFFF_C000_0000))
            },

            {
                new BigInteger(unchecked((long)0x8000_0000_0000_0000)),
                1,
                new BigInteger(unchecked((long)0x4000_0000_0000_0000))
            },
            {
                new BigInteger(unchecked((long)0x8000_0000_0000_0000)),
                2,
                new BigInteger(unchecked((long)0x2000_0000_0000_0000))
            },
            {
                new BigInteger(unchecked((long)0x8000_0000_0000_0001)),
                1,
                new BigInteger(unchecked((long)0xC000_0000_0000_0000))
            },
            {
                new BigInteger(unchecked((long)0x8000_0000_0000_0001)),
                2,
                new BigInteger(unchecked((long)0x6000_0000_0000_0000))
            },
            {
                new BigInteger(unchecked((long)0x8000_0000_0000_0002)),
                1,
                new BigInteger(unchecked((long)0x4000_0000_0000_0001))
            },
            {
                new BigInteger(unchecked((long)0x8000_0000_0000_0002)),
                2,
                new BigInteger(unchecked((long)0xA000_0000_0000_0000))
            },

            {
                BigInteger.Parse("8000_0000_0000_0000_0000_0000".Replace("_", ""), NumberStyles.HexNumber),
                1,
                BigInteger.Parse("4000_0000_0000_0000_0000_0000".Replace("_", ""), NumberStyles.HexNumber)
            },
            {
                BigInteger.Parse("8000_0000_0000_0000_0000_0000".Replace("_", ""), NumberStyles.HexNumber),
                2,
                BigInteger.Parse("2000_0000_0000_0000_0000_0000".Replace("_", ""), NumberStyles.HexNumber)
            },
            {
                BigInteger.Parse("8000_0000_0000_0000_0000_0001".Replace("_", ""), NumberStyles.HexNumber),
                1,
                BigInteger.Parse("C000_0000_0000_0000_0000_0000".Replace("_", ""), NumberStyles.HexNumber)
            },
            {
                BigInteger.Parse("8000_0000_0000_0000_0000_0001".Replace("_", ""), NumberStyles.HexNumber),
                2,
                BigInteger.Parse("6000_0000_0000_0000_0000_0000".Replace("_", ""), NumberStyles.HexNumber)
            },
            {
                BigInteger.Parse("8000_0000_0000_0000_0000_0002".Replace("_", ""), NumberStyles.HexNumber),
                1,
                BigInteger.Parse("4000_0000_0000_0000_0000_0001".Replace("_", ""), NumberStyles.HexNumber)
            },
            {
                BigInteger.Parse("8000_0000_0000_0000_0000_0002".Replace("_", ""), NumberStyles.HexNumber),
                2,
                BigInteger.Parse("A000_0000_0000_0000_0000_0000".Replace("_", ""), NumberStyles.HexNumber)
            },

            {
                BigInteger.Parse("________F_0000_0000_0000_0000_0000_0000".Replace("_", ""), NumberStyles.HexNumber),
                1,
                BigInteger.Parse("7FFF_FFFF_8000_0000_0000_0000_0000_0000".Replace("_", ""), NumberStyles.HexNumber)
            },
            {
                BigInteger.Parse("________F_0000_0000_0000_0000_0000_0000".Replace("_", ""), NumberStyles.HexNumber),
                2,
                BigInteger.Parse("3FFF_FFFF_C000_0000_0000_0000_0000_0000".Replace("_", ""), NumberStyles.HexNumber)
            },
            {
                BigInteger.Parse("________F_0000_0000_0000_0000_0000_0001".Replace("_", ""), NumberStyles.HexNumber),
                1,
                BigInteger.Parse("__________8000_0000_0000_0000_0000_0000".Replace("_", ""), NumberStyles.HexNumber)
            },
            {
                BigInteger.Parse("________F_0000_0000_0000_0000_0000_0001".Replace("_", ""), NumberStyles.HexNumber),
                2,
                BigInteger.Parse("7FFF_FFFF_C000_0000_0000_0000_0000_0000".Replace("_", ""), NumberStyles.HexNumber)
            },
            {
                BigInteger.Parse("________F_0000_0000_0000_0000_0000_0002".Replace("_", ""), NumberStyles.HexNumber),
                1,
                BigInteger.Parse("7FFF_FFFF_8000_0000_0000_0000_0000_0001".Replace("_", ""), NumberStyles.HexNumber)
            },
            {
                BigInteger.Parse("________F_0000_0000_0000_0000_0000_0002".Replace("_", ""), NumberStyles.HexNumber),
                2,
                BigInteger.Parse("BFFF_FFFF_C000_0000_0000_0000_0000_0000".Replace("_", ""), NumberStyles.HexNumber)
            },
        };

        [Theory]
        [MemberData(nameof(NegativeNumber_TestData))]
        public void NegativeNumber(BigInteger input, int rotateAmount, BigInteger expected)
        {
            Assert.Equal(expected, BigInteger.RotateRight(input, rotateAmount));
        }

        [Fact]
        public void PowerOfTwo()
        {
            for (int i = 0; i < 32; i++)
            {
                foreach (int k in new int[] { 1, 2, 3, 10 })
                {
                    BigInteger plus = BigInteger.One << (32 * k + i);
                    BigInteger minus = BigInteger.MinusOne << (32 * k + i);

                    Assert.Equal(BigInteger.One << (32 * k + i - 1), BigInteger.RotateRight(plus, 1));
                    Assert.Equal(BigInteger.One << (32 * (k - 1) + i), BigInteger.RotateRight(plus, 32));
                    Assert.Equal(BigInteger.One << i, BigInteger.RotateRight(plus, 32 * k));

                    Assert.Equal(new BigInteger(uint.MaxValue << i) << (32 * k - 1), BigInteger.RotateRight(minus, 1));
                    Assert.Equal(new BigInteger(uint.MaxValue << i) << (32 * (k - 1)), BigInteger.RotateRight(minus, 32));
                    Assert.Equal(new BigInteger(uint.MaxValue << i), BigInteger.RotateRight(minus, 32 * k));
                }
            }
        }
    }
}
