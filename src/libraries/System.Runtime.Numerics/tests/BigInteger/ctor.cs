// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Numerics.Tests
{
    public class BigIntegerConstructorTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunCtorInt32Tests()
        {
            // ctor(Int32): Int32.MinValue
            VerifyCtorInt32(int.MinValue);

            // ctor(Int32): -1
            VerifyCtorInt32((int)(-1));

            // ctor(Int32): 0
            VerifyCtorInt32((int)0);

            // ctor(Int32): 1
            VerifyCtorInt32((int)1);

            // ctor(Int32): Int32.MaxValue
            VerifyCtorInt32(int.MaxValue);

            // ctor(Int32): Random Positive
            for (int i = 0; i < s_samples; ++i)
            {
                VerifyCtorInt32((int)s_random.Next(1, int.MaxValue));
            }

            // ctor(Int32): Random Negative
            for (int i = 0; i < s_samples; ++i)
            {
                VerifyCtorInt32((int)s_random.Next(int.MinValue, 0));
            }
        }

        private static void VerifyCtorInt32(int value)
        {
            BigInteger bigInteger = new BigInteger(value);

            Assert.Equal(value, bigInteger);
            Assert.Equal(0, string.CompareOrdinal(value.ToString(), bigInteger.ToString()));
            Assert.Equal(value, (int)bigInteger);

            if (value != int.MaxValue)
            {
                Assert.Equal((int)(value + 1), (int)(bigInteger + 1));
            }

            if (value != int.MinValue)
            {
                Assert.Equal((int)(value - 1), (int)(bigInteger - 1));
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == value);
        }

        [Fact]
        public static void RunCtorInt64Tests()
        {
            // ctor(Int64): Int64.MinValue
            VerifyCtorInt64(long.MinValue);

            // ctor(Int64): Int32.MinValue-1
            VerifyCtorInt64(((long)int.MinValue) - 1);

            // ctor(Int64): Int32.MinValue
            VerifyCtorInt64((long)int.MinValue);

            // ctor(Int64): -1
            VerifyCtorInt64((long)(-1));

            // ctor(Int64): 0
            VerifyCtorInt64((long)0);

            // ctor(Int64): 1
            VerifyCtorInt64((long)1);

            // ctor(Int64): Int32.MaxValue
            VerifyCtorInt64((long)int.MaxValue);

            // ctor(Int64): Int32.MaxValue+1
            VerifyCtorInt64(((long)int.MaxValue) + 1);

            // ctor(Int64): Int64.MaxValue
            VerifyCtorInt64(long.MaxValue);

            // ctor(Int64): Random Positive
            for (int i = 0; i < s_samples; ++i)
            {
                VerifyCtorInt64((long)(long.MaxValue * s_random.NextDouble()));
            }

            // ctor(Int64): Random Negative
            for (int i = 0; i < s_samples; ++i)
            {
                VerifyCtorInt64((long)(long.MaxValue * s_random.NextDouble()) - long.MaxValue);
            }
        }

        private static void VerifyCtorInt64(long value)
        {
            BigInteger bigInteger = new BigInteger(value);

            Assert.Equal(value, bigInteger);
            Assert.Equal(0, string.CompareOrdinal(value.ToString(), bigInteger.ToString()));
            Assert.Equal(value, (long)bigInteger);

            if (value != long.MaxValue)
            {
                Assert.Equal((long)(value + 1), (long)(bigInteger + 1));
            }

            if (value != long.MinValue)
            {
                Assert.Equal((long)(value - 1), (long)(bigInteger - 1));
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == value);
        }

        [Fact]
        public static void RunCtorUInt32Tests()
        {
            // ctor(UInt32): UInt32.MinValue
            VerifyCtorUInt32(uint.MinValue);

            // ctor(UInt32): 0
            VerifyCtorUInt32((uint)0);

            // ctor(UInt32): 1
            VerifyCtorUInt32((uint)1);

            // ctor(UInt32): Int32.MaxValue
            VerifyCtorUInt32((uint)int.MaxValue);

            // ctor(UInt32): Int32.MaxValue+1
            VerifyCtorUInt32(((uint)int.MaxValue) + 1);

            // ctor(UInt32): UInt32.MaxValue
            VerifyCtorUInt32(uint.MaxValue);

            // ctor(UInt32): Random Positive
            for (int i = 0; i < s_samples; ++i)
            {
                VerifyCtorUInt32((uint)(uint.MaxValue * s_random.NextDouble()));
            }
        }

        private static void VerifyCtorUInt32(uint value)
        {
            BigInteger bigInteger = new BigInteger(value);

            Assert.Equal(value, bigInteger);
            Assert.Equal(0, string.CompareOrdinal(value.ToString(), bigInteger.ToString()));
            Assert.Equal(value, (uint)bigInteger);

            if (value != uint.MaxValue)
            {
                Assert.Equal((uint)(value + 1), (uint)(bigInteger + 1));
            }

            if (value != uint.MinValue)
            {
                Assert.Equal((uint)(value - 1), (uint)(bigInteger - 1));
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == value);
        }

        [Fact]
        public static void RunCtorUInt64Tests()
        {
            // ctor(UInt64): UInt64.MinValue
            VerifyCtorUInt64(ulong.MinValue);

            // ctor(UInt64): 0
            VerifyCtorUInt64((ulong)0);

            // ctor(UInt64): 1
            VerifyCtorUInt64((ulong)1);

            // ctor(UInt64): Int32.MaxValue
            VerifyCtorUInt64((ulong)int.MaxValue);

            // ctor(UInt64): Int32.MaxValue+1
            VerifyCtorUInt64(((ulong)int.MaxValue) + 1);

            // ctor(UInt64): UInt64.MaxValue
            VerifyCtorUInt64(ulong.MaxValue);

            // ctor(UInt64): Random Positive
            for (int i = 0; i < s_samples; ++i)
            {
                VerifyCtorUInt64((ulong)(ulong.MaxValue * s_random.NextDouble()));
            }
        }

        private static void VerifyCtorUInt64(ulong value)
        {
            BigInteger bigInteger = new BigInteger(value);

            Assert.Equal(value, bigInteger);
            Assert.Equal(0, string.CompareOrdinal(value.ToString(), bigInteger.ToString()));
            Assert.Equal(value, (ulong)bigInteger);

            if (value != ulong.MaxValue)
            {
                Assert.Equal((ulong)(value + 1), (ulong)(bigInteger + 1));
            }

            if (value != ulong.MinValue)
            {
                Assert.Equal((ulong)(value - 1), (ulong)(bigInteger - 1));
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == value);
        }

        [Fact]
        public static void RunCtorSingleTests()
        {
            // ctor(Single): Single.Minvalue
            VerifyCtorSingle(float.MinValue);

            // ctor(Single): Int32.MinValue-1
            VerifyCtorSingle(((float)int.MinValue) - 1);

            // ctor(Single): Int32.MinValue
            VerifyCtorSingle(((float)int.MinValue));

            // ctor(Single): Int32.MinValue+1
            VerifyCtorSingle(((float)int.MinValue) + 1);

            // ctor(Single): -1
            VerifyCtorSingle((float)(-1));

            // ctor(Single): 0
            VerifyCtorSingle((float)0);

            // ctor(Single): 1
            VerifyCtorSingle((float)1);

            // ctor(Single): Int32.MaxValue-1
            VerifyCtorSingle(((float)int.MaxValue) - 1);

            // ctor(Single): Int32.MaxValue
            VerifyCtorSingle(((float)int.MaxValue));

            // ctor(Single): Int32.MaxValue+1
            VerifyCtorSingle(((float)int.MaxValue) + 1);

            // ctor(Single): Single.MaxValue
            VerifyCtorSingle(float.MaxValue);

            // ctor(Single): Random Positive
            for (int i = 0; i < s_samples; i++)
            {
                VerifyCtorSingle((float)(float.MaxValue * s_random.NextDouble()));
            }

            // ctor(Single): Random Negative
            for (int i = 0; i < s_samples; i++)
            {
                VerifyCtorSingle(((float)(float.MaxValue * s_random.NextDouble())) - float.MaxValue);
            }

            // ctor(Single): Small Random Positive with fractional part
            for (int i = 0; i < s_samples; i++)
            {
                VerifyCtorSingle((float)(s_random.Next(0, 100) + s_random.NextDouble()));
            }

            // ctor(Single): Small Random Negative with fractional part
            for (int i = 0; i < s_samples; i++)
            {
                VerifyCtorSingle(((float)(s_random.Next(-100, 0) - s_random.NextDouble())));
            }

            // ctor(Single): Large Random Positive with fractional part
            for (int i = 0; i < s_samples; i++)
            {
                VerifyCtorSingle((float)((float.MaxValue * s_random.NextDouble()) + s_random.NextDouble()));
            }

            // ctor(Single): Large Random Negative with fractional part
            for (int i = 0; i < s_samples; i++)
            {
                VerifyCtorSingle(((float)((-(float.MaxValue - 1) * s_random.NextDouble()) - s_random.NextDouble())));
            }

            // ctor(Single): Single.Epsilon
            VerifyCtorSingle(float.Epsilon);

            // ctor(Single): Single.NegativeInfinity
            Assert.Throws<OverflowException>(() => new BigInteger(float.NegativeInfinity));

            // ctor(Single): Single.PositiveInfinity
            Assert.Throws<OverflowException>(() =>
            {
                BigInteger temp = new BigInteger(float.PositiveInfinity);
            });

            // ctor(Single): Single.NaN
            Assert.Throws<OverflowException>(() =>
            {
                BigInteger temp = new BigInteger(float.NaN);
            });

            // ctor(Single): Single.NaN 2
            Assert.Throws<OverflowException>(() =>
            {
                BigInteger temp = new BigInteger(ConvertInt32ToSingle(0x7FC00000));
            });

            // ctor(Single): Smallest Exponent
            VerifyCtorSingle((float)Math.Pow(2, -126));

            // ctor(Single): Largest Exponent
            VerifyCtorSingle((float)Math.Pow(2, 127));

            // ctor(Single): Largest number less than 1
            float value = 0;
            for (int i = 1; i <= 24; ++i)
            {
                value += (float)(Math.Pow(2, -i));
            }
            VerifyCtorSingle(value);

            // ctor(Single): Smallest number greater than 1
            value = (float)(1 + Math.Pow(2, -23));
            VerifyCtorSingle(value);

            // ctor(Single): Largest number less than 2
            value = 0;
            for (int i = 1; i <= 23; ++i)
            {
                value += (float)(Math.Pow(2, -i));
            }
            value += 1;
            VerifyCtorSingle(value);
        }

        private static void VerifyCtorSingle(float value)
        {
            BigInteger bigInteger = new BigInteger(value);

            float expectedValue;
            if (value < 0)
            {
                expectedValue = (float)Math.Ceiling(value);
            }
            else
            {
                expectedValue = (float)Math.Floor(value);
            }

            Assert.Equal(expectedValue, (float)bigInteger);

            // Single can only accurately represent integers between -16777216 and 16777216 exclusive.
            // ToString starts to become inaccurate at this point.
            if (expectedValue < 16777216 && -16777216 < expectedValue)
            {
                Assert.True(expectedValue.ToString("G9").Equals(bigInteger.ToString(), StringComparison.OrdinalIgnoreCase), "Single.ToString() and BigInteger.ToString() not equal");
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == expectedValue);
        }

        [Fact]
        public static void RunCtorDoubleTests()
        {
            // ctor(Double): Double.Minvalue
            VerifyCtorDouble(double.MinValue);

            // ctor(Double): Single.Minvalue
            VerifyCtorDouble((double)float.MinValue);

            // ctor(Double): Int64.MinValue-1
            VerifyCtorDouble(((double)long.MinValue) - 1);

            // ctor(Double): Int64.MinValue
            VerifyCtorDouble(((double)long.MinValue));

            // ctor(Double): Int64.MinValue+1
            VerifyCtorDouble(((double)long.MinValue) + 1);

            // ctor(Double): Int32.MinValue-1
            VerifyCtorDouble(((double)int.MinValue) - 1);

            // ctor(Double): Int32.MinValue
            VerifyCtorDouble(((double)int.MinValue));

            // ctor(Double): Int32.MinValue+1
            VerifyCtorDouble(((double)int.MinValue) + 1);

            // ctor(Double): -1
            VerifyCtorDouble((double)(-1));

            // ctor(Double): 0
            VerifyCtorDouble((double)0);

            // ctor(Double): 1
            VerifyCtorDouble((double)1);

            // ctor(Double): Int32.MaxValue-1
            VerifyCtorDouble(((double)int.MaxValue) - 1);

            // ctor(Double): Int32.MaxValue
            VerifyCtorDouble(((double)int.MaxValue));

            // ctor(Double): Int32.MaxValue+1
            VerifyCtorDouble(((double)int.MaxValue) + 1);

            // ctor(Double): Int64.MaxValue-1
            VerifyCtorDouble(((double)long.MaxValue) - 1);

            // ctor(Double): Int64.MaxValue
            VerifyCtorDouble(((double)long.MaxValue));

            // ctor(Double): Int64.MaxValue+1
            VerifyCtorDouble(((double)long.MaxValue) + 1);

            // ctor(Double): Single.MaxValue
            VerifyCtorDouble((double)float.MaxValue);

            // ctor(Double): Double.MaxValue
            VerifyCtorDouble(double.MaxValue);

            // ctor(Double): Random Positive
            for (int i = 0; i < s_samples; i++)
            {
                VerifyCtorDouble((double)(double.MaxValue * s_random.NextDouble()));
            }

            // ctor(Double): Random Negative
            for (int i = 0; i < s_samples; i++)
            {
                VerifyCtorDouble((double.MaxValue * s_random.NextDouble()) - double.MaxValue);
            }

            // ctor(Double): Small Random Positive with fractional part
            for (int i = 0; i < s_samples; i++)
            {
                VerifyCtorDouble((double)(s_random.Next(0, 100) + s_random.NextDouble()));
            }

            // ctor(Double): Small Random Negative with fractional part
            for (int i = 0; i < s_samples; i++)
            {
                VerifyCtorDouble(((double)(s_random.Next(-100, 0) - s_random.NextDouble())));
            }

            // ctor(Double): Large Random Positive with fractional part
            for (int i = 0; i < s_samples; i++)
            {
                VerifyCtorDouble((double)((long.MaxValue / 100 * s_random.NextDouble()) + s_random.NextDouble()));
            }

            // ctor(Double): Large Random Negative with fractional part
            for (int i = 0; i < s_samples; i++)
            {
                VerifyCtorDouble(((double)((-(long.MaxValue / 100) * s_random.NextDouble()) - s_random.NextDouble())));
            }

            // ctor(Double): Double.Epsilon
            VerifyCtorDouble(double.Epsilon);

            // ctor(Double): Double.NegativeInfinity
            Assert.Throws<OverflowException>(() =>
            {
                BigInteger temp = new BigInteger(double.NegativeInfinity);
            });

            // ctor(Double): Double.PositiveInfinity
            Assert.Throws<OverflowException>(() =>
            {
                BigInteger temp = new BigInteger(double.PositiveInfinity);
            });

            // ctor(Double): Double.NaN
            Assert.Throws<OverflowException>(() =>
            {
                BigInteger temp = new BigInteger(double.NaN);
            });

            // ctor(Double): Double.NaN 2
            Assert.Throws<OverflowException>(() =>
            {
                BigInteger temp = new BigInteger(ConvertInt64ToDouble(0x7FF8000000000000));
            });

            // ctor(Double): Smallest Exponent
            VerifyCtorDouble((double)Math.Pow(2, -1022));

            // ctor(Double): Largest Exponent
            VerifyCtorDouble((double)Math.Pow(2, 1023));

            // ctor(Double): Largest number less than 1
            double value = 0;
            for (int i = 1; i <= 53; ++i)
            {
                value += (double)(Math.Pow(2, -i));
            }
            VerifyCtorDouble(value);

            // ctor(Double): Smallest number greater than 1
            value = (double)(1 + Math.Pow(2, -52));
            VerifyCtorDouble(value);

            // ctor(Double): Largest number less than 2
            value = 0;
            for (int i = 1; i <= 52; ++i)
            {
                value += (double)(Math.Pow(2, -i));
            }
            value += 2;
            VerifyCtorDouble(value);
        }

        private static void VerifyCtorDouble(double value)
        {
            BigInteger bigInteger = new BigInteger(value);

            double expectedValue;
            if (value < 0)
            {
                expectedValue = (double)Math.Ceiling(value);
            }
            else
            {
                expectedValue = (double)Math.Floor(value);
            }

            Assert.Equal(expectedValue, (double)bigInteger);

            // Single can only accurately represent integers between -16777216 and 16777216 exclusive.
            // ToString starts to become inaccurate at this point.
            if (expectedValue < 9007199254740992 && -9007199254740992 < expectedValue)
            {
                Assert.True(expectedValue.ToString("G17").Equals(bigInteger.ToString(), StringComparison.OrdinalIgnoreCase), "Double.ToString() and BigInteger.ToString() not equal");
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == expectedValue);
        }

        [Fact]
        public static void RunCtorDecimalTests()
        {
            decimal value;

            // ctor(Decimal): Decimal.MinValue
            VerifyCtorDecimal(decimal.MinValue);

            // ctor(Decimal): -1
            VerifyCtorDecimal(-1);

            // ctor(Decimal): 0
            VerifyCtorDecimal(0);

            // ctor(Decimal): 1
            VerifyCtorDecimal(1);

            // ctor(Decimal): Decimal.MaxValue
            VerifyCtorDecimal(decimal.MaxValue);

            // ctor(Decimal): Random Positive
            for (int i = 0; i < s_samples; i++)
            {
                value = new decimal(
                    s_random.Next(int.MinValue, int.MaxValue),
                    s_random.Next(int.MinValue, int.MaxValue),
                    s_random.Next(int.MinValue, int.MaxValue),
                    false,
                    (byte)s_random.Next(0, 29));
                VerifyCtorDecimal(value);
            }

            // ctor(Decimal): Random Negative
            for (int i = 0; i < s_samples; i++)
            {
                value = new decimal(
                    s_random.Next(int.MinValue, int.MaxValue),
                    s_random.Next(int.MinValue, int.MaxValue),
                    s_random.Next(int.MinValue, int.MaxValue),
                    true,
                    (byte)s_random.Next(0, 29));
                VerifyCtorDecimal(value);
            }

            // ctor(Decimal): Smallest Exponent
            unchecked
            {
                value = new decimal(1, 0, 0, false, 0);
            }
            VerifyCtorDecimal(value);

            // ctor(Decimal): Largest Exponent and zero integer
            unchecked
            {
                value = new decimal(0, 0, 0, false, 28);
            }
            VerifyCtorDecimal(value);

            // ctor(Decimal): Largest Exponent and non zero integer
            unchecked
            {
                value = new decimal(1, 0, 0, false, 28);
            }
            VerifyCtorDecimal(value);

            // ctor(Decimal): Largest number less than 1
            value = 1 - new decimal(1, 0, 0, false, 28);
            VerifyCtorDecimal(value);

            // ctor(Decimal): Smallest number greater than 1
            value = 1 + new decimal(1, 0, 0, false, 28);
            VerifyCtorDecimal(value);

            // ctor(Decimal): Largest number less than 2
            value = 2 - new decimal(1, 0, 0, false, 28);
            VerifyCtorDecimal(value);
        }

        private static void VerifyCtorDecimal(decimal value)
        {
            BigInteger bigInteger = new BigInteger(value);

            decimal expectedValue;
            if (value < 0)
            {
                expectedValue = Math.Ceiling(value);
            }
            else
            {
                expectedValue = Math.Floor(value);
            }

            Assert.True(expectedValue.ToString().Equals(bigInteger.ToString(), StringComparison.OrdinalIgnoreCase), "Decimal.ToString() and BigInteger.ToString()");
            Assert.Equal(expectedValue, (decimal)bigInteger);

            if (expectedValue != Math.Floor(decimal.MaxValue))
            {
                Assert.Equal((decimal)(expectedValue + 1), (decimal)(bigInteger + 1));
            }

            if (expectedValue != Math.Ceiling(decimal.MinValue))
            {
                Assert.Equal((decimal)(expectedValue - 1), (decimal)(bigInteger - 1));
            }

            VerifyBigIntegerUsingIdentities(bigInteger, 0 == expectedValue);
        }

        [Fact]
        public static void RunCtorByteArrayTests()
        {
            ulong tempUInt64;
            byte[] tempByteArray;

            // ctor(byte[]): array is null
            Assert.Throws<ArgumentNullException>(() =>
            {
                BigInteger bigInteger = new BigInteger((byte[])null);
            });

            // ctor(byte[]): array is empty
            VerifyCtorByteArray(new byte[0], 0);

            // ctor(byte[]): array is 1 byte
            tempUInt64 = (uint)s_random.Next(0, 256);
            tempByteArray = BitConverter.GetBytes(tempUInt64);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(tempByteArray);
            }
            if (tempByteArray[0] > 127)
            {
                VerifyCtorByteArray(new byte[] { tempByteArray[0] });
                VerifyCtorByteArray(new byte[] { tempByteArray[0], 0 }, tempUInt64);
            }
            else
            {
                VerifyCtorByteArray(new byte[] { tempByteArray[0] }, tempUInt64);
            }

            // ctor(byte[]): Small array with all zeros
            VerifyCtorByteArray(new byte[] { 0, 0, 0, 0 });

            // ctor(byte[]): Large array with all zeros
            VerifyCtorByteArray(
               new byte[] {
                    0, 0, 0, 0,
                    0, 0, 0, 0,
                    0, 0, 0, 0,
                    0, 0, 0, 0,
                    0, 0, 0, 0,
                    0, 0, 0, 0
                });

            // ctor(byte[]): Small array with all ones
            VerifyCtorByteArray(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });

            // ctor(byte[]): Large array with all ones
            VerifyCtorByteArray(
                new byte[] {
                    0xFF, 0xFF, 0xFF, 0xFF,
                    0xFF, 0xFF, 0xFF, 0xFF,
                    0xFF, 0xFF, 0xFF, 0xFF,
                    0xFF, 0xFF, 0xFF, 0xFF,
                    0xFF, 0xFF, 0xFF, 0xFF
                });

            // ctor(byte[]): array with a lot of leading zeros
            for (int i = 0; i < s_samples; i++)
            {
                tempUInt64 = unchecked((uint)s_random.Next(int.MinValue, int.MaxValue));
                tempByteArray = BitConverter.GetBytes(tempUInt64);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(tempByteArray);
                }

                VerifyCtorByteArray(
                    new byte[] {
                        tempByteArray[0],
                        tempByteArray[1],
                        tempByteArray[2],
                        tempByteArray[3],
                        0, 0, 0, 0,
                        0, 0, 0, 0,
                        0, 0, 0, 0,
                        0, 0, 0, 0,
                        0, 0, 0, 0,
                        0, 0, 0, 0
                    },
                    tempUInt64);
            }

            // ctor(byte[]): array 4 bytes
            for (int i = 0; i < s_samples; i++)
            {
                tempUInt64 = unchecked((uint)s_random.Next(int.MinValue, int.MaxValue));
                tempByteArray = BitConverter.GetBytes(tempUInt64);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(tempByteArray);
                }

                if (tempUInt64 > int.MaxValue)
                {
                    VerifyCtorByteArray(
                        new byte[] {
                            tempByteArray[0],
                            tempByteArray[1],
                            tempByteArray[2],
                            tempByteArray[3]
                        });
                    VerifyCtorByteArray(
                       new byte[] {
                            tempByteArray[0],
                            tempByteArray[1],
                            tempByteArray[2],
                            tempByteArray[3],
                            0
                       },
                       tempUInt64);
                }
                else
                {
                    VerifyCtorByteArray(
                        new byte[] {
                            tempByteArray[0],
                            tempByteArray[1],
                            tempByteArray[2],
                            tempByteArray[3]
                        },
                        tempUInt64);
                }
            }

            // ctor(byte[]): array 5 bytes
            for (int i = 0; i < s_samples; i++)
            {
                tempUInt64 = unchecked((uint)s_random.Next(int.MinValue, int.MaxValue));
                tempUInt64 <<= 8;
                tempUInt64 += (ulong)s_random.Next(0, 256);
                tempByteArray = BitConverter.GetBytes(tempUInt64);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(tempByteArray);
                }

                if (tempUInt64 >= (ulong)0x00080000)
                {
                    VerifyCtorByteArray(
                        new byte[] {
                            tempByteArray[0],
                            tempByteArray[1],
                            tempByteArray[2],
                            tempByteArray[3],
                            tempByteArray[4]
                        });

                    VerifyCtorByteArray(
                        new byte[] {
                            tempByteArray[0],
                            tempByteArray[1],
                            tempByteArray[2],
                            tempByteArray[3],
                            tempByteArray[4],
                            0
                        }, tempUInt64);
                }
                else
                {
                    VerifyCtorByteArray(
                        new byte[] {
                            tempByteArray[0],
                            tempByteArray[1],
                            tempByteArray[2],
                            tempByteArray[3],
                            tempByteArray[4]
                        }, tempUInt64);
                }
            }

            // ctor(byte[]): array 8 bytes
            for (int i = 0; i < s_samples; i++)
            {
                tempUInt64 = unchecked((uint)s_random.Next(int.MinValue, int.MaxValue));
                tempUInt64 <<= 32;
                tempUInt64 += unchecked((uint)s_random.Next(int.MinValue, int.MaxValue));
                tempByteArray = BitConverter.GetBytes(tempUInt64);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(tempByteArray);
                }

                if (tempUInt64 > long.MaxValue)
                {
                    VerifyCtorByteArray(
                        new byte[] {
                            tempByteArray[0],
                            tempByteArray[1],
                            tempByteArray[2],
                            tempByteArray[3],
                            tempByteArray[4],
                            tempByteArray[5],
                            tempByteArray[6],
                            tempByteArray[7]
                        });
                    VerifyCtorByteArray(
                        new byte[] {
                            tempByteArray[0],
                            tempByteArray[1],
                            tempByteArray[2],
                            tempByteArray[3],
                            tempByteArray[4],
                            tempByteArray[5],
                            tempByteArray[6],
                            tempByteArray[7],
                            0
                        }, tempUInt64);
                }
                else
                {
                    VerifyCtorByteArray(
                        new byte[] {
                            tempByteArray[0],
                            tempByteArray[1],
                            tempByteArray[2],
                            tempByteArray[3],
                            tempByteArray[4],
                            tempByteArray[5],
                            tempByteArray[6],
                            tempByteArray[7]
                        },
                        tempUInt64);
                }
            }

            // ctor(byte[]): array 9 bytes
            for (int i = 0; i < s_samples; i++)
            {
                VerifyCtorByteArray(
                    new byte[] {
                        (byte)s_random.Next(0, 256),
                        (byte)s_random.Next(0, 256),
                        (byte)s_random.Next(0, 256),
                        (byte)s_random.Next(0, 256),
                        (byte)s_random.Next(0, 256),
                        (byte)s_random.Next(0, 256),
                        (byte)s_random.Next(0, 256),
                        (byte)s_random.Next(0, 256),
                        (byte)s_random.Next(0, 256)
                    });
            }

            // ctor(byte[]): array is UInt32.MaxValue
            VerifyCtorByteArray(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0 }, uint.MaxValue);

            // ctor(byte[]): array is UInt32.MaxValue + 1
            VerifyCtorByteArray(new byte[] { 0, 0, 0, 0, 1 }, (ulong)uint.MaxValue + 1);

            // ctor(byte[]): array is Int32.MinValue with overlong representation.
            VerifyCtorByteArray(new byte[] {0, 0, 0, 0x80, 0xFF});
            Assert.Equal(new BigInteger(new byte[] { 0, 0, 0, 0x80, 0xFF, 0xFF, 0xFF }), int.MinValue);

            // ctor(byte[]): array is UInt64.MaxValue
            VerifyCtorByteArray(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0 }, ulong.MaxValue);

            // ctor(byte[]): UInt64.MaxValue + 1
            VerifyCtorByteArray(
                new byte[] {
                    0, 0, 0, 0,
                    0, 0, 0, 0,
                    1
                });

            // ctor(byte[]): UInt64.MaxValue + 2^64
            VerifyCtorByteArray(
                new byte[] {
                    0xFF, 0xFF, 0xFF, 0xFF,
                    0xFF, 0xFF, 0xFF, 0xFF,
                    1
                });

            // ctor(byte[]): array is random > UInt64
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray = new byte[s_random.Next(0, 1024)];
                for (int arrayIndex = 0; arrayIndex < tempByteArray.Length; ++arrayIndex)
                {
                    tempByteArray[arrayIndex] = (byte)s_random.Next(0, 256);
                }
                VerifyCtorByteArray(tempByteArray);
            }

            // ctor(byte[]): array is large
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray = new byte[s_random.Next(16384, 2097152)];
                for (int arrayIndex = 0; arrayIndex < tempByteArray.Length; ++arrayIndex)
                {
                    tempByteArray[arrayIndex] = (byte)s_random.Next(0, 256);
                }
                VerifyCtorByteArray(tempByteArray);
            }
        }

        private static void VerifyCtorByteArray(byte[] value, ulong expectedValue)
        {
            BigInteger bigInteger = new BigInteger(value);

            Assert.Equal(expectedValue, bigInteger);
            Assert.True(expectedValue.ToString().Equals(bigInteger.ToString(), StringComparison.OrdinalIgnoreCase), "UInt64.ToString() and BigInteger.ToString()");
            Assert.Equal(expectedValue, (ulong)bigInteger);

            if (expectedValue != ulong.MaxValue)
            {
                Assert.Equal((ulong)(expectedValue + 1), (ulong)(bigInteger + 1));
            }

            if (expectedValue != ulong.MinValue)
            {
                Assert.Equal((ulong)(expectedValue - 1), (ulong)(bigInteger - 1));
            }

            VerifyCtorByteArray(value);
        }

        static void VerifyCtorByteSpan(byte[] value) =>
            Assert.Equal(new BigInteger(value), new BigInteger(new ReadOnlySpan<byte>(value)));

        private static void VerifyCtorByteArray(byte[] value)
        {
            BigInteger bigInteger;
            byte[] roundTrippedByteArray;
            bool isZero = MyBigIntImp.IsZero(value);

            bigInteger = new BigInteger(value);
            VerifyCtorByteSpan(value);

            roundTrippedByteArray = bigInteger.ToByteArray();

            for (int i = Math.Min(value.Length, roundTrippedByteArray.Length) - 1; 0 <= i; --i)
            {
                Assert.True(value[i] == roundTrippedByteArray[i], string.Format("Round Tripped ByteArray at {0}", i));
            }
            if (value.Length < roundTrippedByteArray.Length)
            {
                for (int i = value.Length; i < roundTrippedByteArray.Length; ++i)
                {
                    Assert.True(0 == roundTrippedByteArray[i],
                        string.Format("Round Tripped ByteArray is larger than the original array and byte is non zero at {0}", i));
                }
            }
            else if (value.Length > roundTrippedByteArray.Length)
            {
                for (int i = roundTrippedByteArray.Length; i < value.Length; ++i)
                {
                    Assert.False((((0 != value[i]) && ((roundTrippedByteArray[roundTrippedByteArray.Length - 1] & 0x80) == 0)) ||
                        ((0xFF != value[i]) && ((roundTrippedByteArray[roundTrippedByteArray.Length - 1] & 0x80) != 0))),
                        string.Format("Round Tripped ByteArray is smaller than the original array and byte is non zero at {0}", i));
                }
            }

            if (value.Length < 8)
            {
                byte[] newvalue = new byte[8];

                for (int i = 0; i < 8; i++)
                {
                    if (bigInteger < 0)
                    {
                        newvalue[i] = 0xFF;
                    }
                    else
                    {
                        newvalue[i] = 0;
                    }
                }

                for (int i = 0; i < value.Length; i++)
                {
                    newvalue[i] = value[i];
                }

                value = newvalue;
            }
            else if (value.Length > 8)
            {
                int newlength = value.Length;

                for (; newlength > 8; newlength--)
                {
                    if (bigInteger < 0)
                    {
                        if ((value[newlength - 1] != 0xFF) | ((value[newlength - 2] & 0x80) == 0))
                        {
                            break;
                        }
                    }
                    else
                    {
                        if ((value[newlength - 1] != 0) | ((value[newlength - 2] & 0x80) != 0))
                        {
                            break;
                        }
                    }
                }

                byte[] newvalue = new byte[newlength];

                for (int i = 0; i < newlength; i++)
                {
                    newvalue[i] = value[i];
                }

                value = newvalue;
            }

            if (IsOutOfRangeInt64(value))
            {
                // Try subtracting a value from the BigInteger that will allow it to be represented as an Int64
                byte[] tempByteArray;
                BigInteger tempBigInteger;
                bool isNeg = ((value[value.Length - 1] & 0x80) != 0);

                tempByteArray = new byte[value.Length];
                Array.Copy(value, 8, tempByteArray, 8, value.Length - 8);

                tempBigInteger = bigInteger - (new BigInteger(tempByteArray));

                tempByteArray = new byte[8];
                Array.Copy(value, tempByteArray, 8);

                if (!(((tempByteArray[7] & 0x80) == 0) ^ isNeg))
                {
                    tempByteArray[7] ^= 0x80;
                    tempBigInteger = tempBigInteger + (new BigInteger(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0x80 }));
                }
                if (isNeg & (tempBigInteger > 0))
                {
                    tempBigInteger = tempBigInteger + (new BigInteger(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0xFF }));
                }

                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(tempByteArray);
                }
                Assert.Equal(BitConverter.ToInt64(tempByteArray, 0), (long)tempBigInteger);
            }
            else
            {
                byte[] tempByteArray = new byte[8];
                Array.Copy(value, tempByteArray, 8);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(tempByteArray);
                }
                Assert.Equal(BitConverter.ToInt64(tempByteArray, 0), (long)bigInteger);
            }

            if (IsOutOfRangeUInt64(value))
            {
                // Try subtracting a value from the BigInteger that will allow it to be represented as an UInt64
                byte[] tempByteArray;
                BigInteger tempBigInteger;
                bool isNeg = ((value[value.Length - 1] & 0x80) != 0);

                tempByteArray = new byte[value.Length];
                Array.Copy(value, 8, tempByteArray, 8, value.Length - 8);

                tempBigInteger = bigInteger - (new BigInteger(tempByteArray));

                tempByteArray = new byte[8];
                Array.Copy(value, tempByteArray, 8);

                if ((tempByteArray[7] & 0x80) != 0)
                {
                    tempByteArray[7] &= 0x7f;
                    if (tempBigInteger < 0)
                    {
                        tempBigInteger = tempBigInteger - (new BigInteger(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0x80 }));
                    }
                    else
                    {
                        tempBigInteger = tempBigInteger + (new BigInteger(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0x80 }));
                    }
                }

                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(tempByteArray);
                }
                Assert.Equal(BitConverter.ToUInt64(tempByteArray, 0), (ulong)tempBigInteger);
            }
            else
            {
                byte[] tempByteArray = new byte[8];
                Array.Copy(value, tempByteArray, 8);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(tempByteArray);
                }
                Assert.Equal(BitConverter.ToUInt64(tempByteArray, 0), (ulong)bigInteger);
            }

            VerifyBigIntegerUsingIdentities(bigInteger, isZero);
        }

        private static float ConvertInt32ToSingle(int value)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
        }

        private static double ConvertInt64ToDouble(long value)
        {
            return BitConverter.ToDouble(BitConverter.GetBytes(value), 0);
        }

        private static void VerifyBigIntegerUsingIdentities(BigInteger bigInteger, bool isZero)
        {
            BigInteger tempBigInteger = new BigInteger(bigInteger.ToByteArray());

            Assert.Equal(bigInteger, tempBigInteger);

            if (isZero)
            {
                Assert.Equal(BigInteger.Zero, bigInteger);
            }
            else
            {
                Assert.NotEqual(BigInteger.Zero, bigInteger);
                Assert.Equal(BigInteger.One, bigInteger / bigInteger);
            }

            // (x + 1) - 1 = x
            Assert.Equal(bigInteger, ((bigInteger + BigInteger.One) - BigInteger.One));

            // (x + 1) - x = 1
            Assert.Equal(BigInteger.One, ((bigInteger + BigInteger.One) - bigInteger));

            // x - x = 0
            Assert.Equal(BigInteger.Zero, (bigInteger - bigInteger));

            // x + x = 2x
            Assert.Equal((2 * bigInteger), (bigInteger + bigInteger));

            // x/1 = x
            Assert.Equal(bigInteger, (bigInteger / BigInteger.One));

            // 1 * x = x
            Assert.Equal(bigInteger, (BigInteger.One * bigInteger));
        }

        private static bool IsOutOfRangeUInt64(byte[] value)
        {
            if (value.Length == 0)
            {
                return false;
            }
            if ((0x80 & value[value.Length - 1]) != 0)
            {
                return true;
            }

            byte zeroValue = 0;

            if (value.Length <= 8)
            {
                return false;
            }

            for (int i = 8; i < value.Length; ++i)
            {
                if (zeroValue != value[i])
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsOutOfRangeInt64(byte[] value)
        {
            if (value.Length == 0)
            {
                return false;
            }

            bool isNeg = ((0x80 & value[value.Length - 1]) != 0);
            byte zeroValue = 0;

            if (isNeg)
            {
                zeroValue = 0xFF;
            }

            if (value.Length < 8)
            {
                return false;
            }

            for (int i = 8; i < value.Length; i++)
            {
                if (zeroValue != value[i])
                {
                    return true;
                }
            }

            return (!((0 == (0x80 & value[7])) ^ isNeg));
        }
    }
}
