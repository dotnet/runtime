// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using System.Diagnostics;
using Xunit;

namespace System.Numerics.Tests
{
    public class implicitExplicitCastOperatorsTest
    {
        private static void VerifyInt16ImplicitCastToComplex(Int16 value)
        {
            Complex c_cast = value;

            if (false == Support.VerifyRealImaginaryProperties(c_cast, value, 0.0))
            {
                Console.WriteLine("Int16ImplicitCast ({0})" + value);
                Assert.True(false, "Verification Failed");
            }
            else
            {
                if (value != Int16.MaxValue)
                {
                    Complex c_cast_plus = c_cast + 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_plus, value + 1, 0.0))
                    {
                        Console.WriteLine("PLuS + Int16ImplicitCast ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }

                if (value != Int16.MinValue)
                {
                    Complex c_cast_minus = c_cast - 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_minus, value - 1, 0.0))
                    {
                        Console.WriteLine("Minus - Int16ImplicitCast + 1 ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }
            }
        }

        [Fact]
        public static void RunTests_Int16ImplicitCastToComplex()
        {
            VerifyInt16ImplicitCastToComplex(Int16.MinValue);

            for (int i = 0; i < Support.RandomSampleCount; ++i)
            {
                Int16 randomValue = Support.GetRandomInt16Value(true);
                VerifyInt16ImplicitCastToComplex(randomValue);
            }

            VerifyInt16ImplicitCastToComplex(-1);
            VerifyInt16ImplicitCastToComplex(0);
            VerifyInt16ImplicitCastToComplex(1);

            for (int i = 0; i < Support.RandomSampleCount; ++i)
            {
                Int16 randomValue = Support.GetRandomInt16Value(false);
                VerifyInt16ImplicitCastToComplex(randomValue);
            }

            VerifyInt16ImplicitCastToComplex(Int16.MaxValue);
        }

        private static void VerifyInt32ImplicitCastToComplex(Int32 value)
        {
            Complex c_cast = value;

            if (false == Support.VerifyRealImaginaryProperties(c_cast, value, 0.0))
            {
                Console.WriteLine("Int32ImplicitCast ({0})" + value);
                Assert.True(false, "Verification Failed");
            }
            else
            {
                if (value != Int32.MaxValue)
                {
                    Complex c_cast_plus = c_cast + 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_plus, value + 1, 0.0))
                    {
                        Console.WriteLine("PLuS + Int32ImplicitCast ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }

                if (value != Int32.MinValue)
                {
                    Complex c_cast_minus = c_cast - 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_minus, value - 1, 0.0))
                    {
                        Console.WriteLine("Minus - Int32ImplicitCast + 1 ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }
            }
        }

        [Fact]
        public static void RunTests_Int32ImplicitCastToComplex()
        {
            VerifyInt32ImplicitCastToComplex(Int32.MinValue);

            for (int i = 0; i < Support.RandomSampleCount; ++i)
            {
                Int32 randomValue = Support.GetRandomInt32Value(true);
                VerifyInt32ImplicitCastToComplex(randomValue);
            }

            VerifyInt32ImplicitCastToComplex(-1);
            VerifyInt32ImplicitCastToComplex(0);
            VerifyInt32ImplicitCastToComplex(1);

            for (int i = 0; i < Support.RandomSampleCount; ++i)
            {
                Int32 randomValue = Support.GetRandomInt32Value(false);
                VerifyInt32ImplicitCastToComplex(randomValue);
            }

            VerifyInt32ImplicitCastToComplex(Int32.MaxValue);
        }

        private static void VerifyInt64ImplicitCastToComplex(Int64 value)
        {
            Complex c_cast = value;

            if (false == Support.VerifyRealImaginaryProperties(c_cast, value, 0.0))
            {
                Console.WriteLine("Int64ImplicitCast ({0})" + value);
                Assert.True(false, "Verification Failed");
            }
            else
            {
                if (value != Int64.MaxValue)
                {
                    Complex c_cast_plus = c_cast + 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_plus, value + 1, 0.0))
                    {
                        Console.WriteLine("PLuS + Int64ImplicitCast ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }

                if (value != Int64.MinValue)
                {
                    Complex c_cast_minus = c_cast - 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_minus, value - 1, 0.0))
                    {
                        Console.WriteLine("Minus - Int64ImplicitCast + 1 ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }
            }
        }

        [Fact]
        public static void RunTests_Int64ImplicitCastToComplex()
        {
            VerifyInt64ImplicitCastToComplex(Int64.MinValue);

            for (int i = 0; i < Support.RandomSampleCount; ++i)
            {
                Int64 randomValue = Support.GetRandomInt64Value(true);
                VerifyInt64ImplicitCastToComplex(randomValue);
            }

            VerifyInt64ImplicitCastToComplex(-1);
            VerifyInt64ImplicitCastToComplex(0);
            VerifyInt64ImplicitCastToComplex(1);

            for (int i = 0; i < Support.RandomSampleCount; ++i)
            {
                Int64 randomValue = Support.GetRandomInt64Value(false);
                VerifyInt64ImplicitCastToComplex(randomValue);
            }

            VerifyInt64ImplicitCastToComplex(Int64.MaxValue);
        }

        private static void VerifyUInt16ImplicitCastToComplex(UInt16 value)
        {
            Complex c_cast = value;

            if (false == Support.VerifyRealImaginaryProperties(c_cast, value, 0.0))
            {
                Console.WriteLine("UInt16ImplicitCast ({0})" + value);
                Assert.True(false, "Verification Failed");
            }
            else
            {
                if (value != UInt16.MaxValue)
                {
                    Complex c_cast_plus = c_cast + 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_plus, value + 1, 0.0))
                    {
                        Console.WriteLine("PLuS + UInt16ImplicitCast ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }

                if (value != UInt16.MinValue)
                {
                    Complex c_cast_minus = c_cast - 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_minus, value - 1, 0.0))
                    {
                        Console.WriteLine("Minus - UInt16ImplicitCast + 1 ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }
            }
        }

        [Fact]
        public static void RunTests_UInt16ImplicitCastToComplex()
        {
            VerifyUInt16ImplicitCastToComplex(UInt16.MinValue);
            VerifyUInt16ImplicitCastToComplex(0);
            VerifyUInt16ImplicitCastToComplex(1);

#if  CLS_Compliant
        for (int i = 0; i < Support.RandomSampleCount; ++i)
        {
            UInt16 randomValue = Support.GetRandomUInt16Value();
            VerifyUInt16ImplicitCastToComplex(randomValue);
        }
#endif
            VerifyUInt16ImplicitCastToComplex(UInt16.MaxValue);
        }

        private static void VerifyUInt32ImplicitCastToComplex(UInt32 value)
        {
            Complex c_cast = value;

            if (false == Support.VerifyRealImaginaryProperties(c_cast, value, 0.0))
            {
                Console.WriteLine("UInt32ImplicitCast ({0})" + value);
                Assert.True(false, "Verification Failed");
            }
            else
            {
                if (value != UInt32.MaxValue)
                {
                    Complex c_cast_plus = c_cast + 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_plus, value + 1, 0.0))
                    {
                        Console.WriteLine("PLuS + UInt32ImplicitCast ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }

                if (value != UInt32.MinValue)
                {
                    Complex c_cast_minus = c_cast - 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_minus, value - 1, 0.0))
                    {
                        Console.WriteLine("Minus - UInt32ImplicitCast + 1 ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }
            }
        }

        [Fact]
        public static void RunTests_UInt32ImplicitCastToComplex()
        {
            VerifyUInt32ImplicitCastToComplex(UInt32.MinValue);
            VerifyUInt32ImplicitCastToComplex(0);
            VerifyUInt32ImplicitCastToComplex(1);

#if  CLS_Compliant
        for (int i = 0; i < Support.RandomSampleCount; ++i)
        {
            UInt32 randomValue = Support.GetRandomUInt32Value();
            VerifyUInt32ImplicitCastToComplex(randomValue);
        }
#endif

            VerifyUInt32ImplicitCastToComplex(UInt32.MaxValue);
        }

        private static void VerifyUInt64ImplicitCastToComplex(UInt64 value)
        {
            Complex c_cast = value;

            if (false == Support.VerifyRealImaginaryProperties(c_cast, value, 0.0))
            {
                Console.WriteLine("UInt64ImplicitCast ({0})" + value);
                Assert.True(false, "Verification Failed");
            }
            else
            {
                if (value != UInt64.MaxValue)
                {
                    Complex c_cast_plus = c_cast + 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_plus, value + 1, 0.0))
                    {
                        Console.WriteLine("PLuS + UInt64ImplicitCast ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }

                if (value != UInt64.MinValue)
                {
                    Complex c_cast_minus = c_cast - 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_minus, value - 1, 0.0))
                    {
                        Console.WriteLine("Minus - UInt64ImplicitCast + 1 ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }
            }
        }

        [Fact]
        public static void RunTests_UInt64ImplicitCastToComplex()
        {
            VerifyUInt64ImplicitCastToComplex(UInt64.MinValue);
            VerifyUInt64ImplicitCastToComplex(0);
            VerifyUInt64ImplicitCastToComplex(1);

#if CLS_Compliant
        for (int i = 0; i < Support.RandomSampleCount; ++i)
        {
            UInt64 randomValue = Support.GetRandomUInt64Value();
            VerifyUInt64ImplicitCastToComplex(randomValue);
        }
#endif
            VerifyUInt64ImplicitCastToComplex(UInt64.MaxValue);
        }

        private static void VerifySByteImplicitCastToComplex(SByte value)
        {
            Complex c_cast = value;

            if (false == Support.VerifyRealImaginaryProperties(c_cast, value, 0.0))
            {
                Console.WriteLine("SByteImplicitCast ({0})" + value);
                Assert.True(false, "Verification Failed");
            }
            else
            {
                if (value != SByte.MaxValue)
                {
                    Complex c_cast_plus = c_cast + 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_plus, value + 1, 0.0))
                    {
                        Console.WriteLine("PLuS + SByteImplicitCast ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }

                if (value != SByte.MinValue)
                {
                    Complex c_cast_minus = c_cast - 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_minus, value - 1, 0.0))
                    {
                        Console.WriteLine("Minus - SByteImplicitCast + 1 ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }
            }
        }

        [Fact]
        public static void RunTests_SByteImplicitCastToComplex()
        {
            VerifySByteImplicitCastToComplex(SByte.MinValue);
#if  CLS_Compliant
        for (int i = 0; i < Support.RandomSampleCount; ++i)
        {
            SByte randomValue = Support.GetRandomSByteValue(false);
            VerifySByteImplicitCastToComplex(randomValue);
        }
#endif
            VerifySByteImplicitCastToComplex(0);
            VerifySByteImplicitCastToComplex(1);

#if  CLS_Compliant
        for (int i = 0; i < Support.RandomSampleCount; ++i)
        {
            SByte randomValue = Support.GetRandomSByteValue(true);
            VerifySByteImplicitCastToComplex(randomValue);
        }
#endif

            VerifySByteImplicitCastToComplex(SByte.MaxValue);
        }

        private static void VerifyByteImplicitCastToComplex(Byte value)
        {
            Complex c_cast = value;

            if (false == Support.VerifyRealImaginaryProperties(c_cast, value, 0.0))
            {
                Console.WriteLine("ByteImplicitCast ({0})" + value);
                Assert.True(false, "Verification Failed");
            }
            else
            {
                if (value != Byte.MaxValue)
                {
                    Complex c_cast_plus = c_cast + 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_plus, value + 1, 0.0))
                    {
                        Console.WriteLine("PLuS + ByteImplicitCast ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }

                if (value != Byte.MinValue)
                {
                    Complex c_cast_minus = c_cast - 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_minus, value - 1, 0.0))
                    {
                        Console.WriteLine("Minus - ByteImplicitCast + 1 ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }
            }
        }

        [Fact]
        public static void RunTests_ByteImplicitCastToComplex()
        {
            VerifyByteImplicitCastToComplex(Byte.MinValue);
            VerifyByteImplicitCastToComplex(0);
            VerifyByteImplicitCastToComplex(1);

            for (int i = 0; i < Support.RandomSampleCount; ++i)
            {
                Byte randomValue = Support.GetRandomByteValue();
                VerifyByteImplicitCastToComplex(randomValue);
            }

            VerifyByteImplicitCastToComplex(Byte.MaxValue);
        }

        private static void VerifySingleImplicitCastToComplex(Single value)
        {
            Complex c_cast = value;

            if (false == Support.VerifyRealImaginaryProperties(c_cast, value, 0.0))
            {
                Console.WriteLine("SingleImplicitCast ({0})" + value);
                Assert.True(false, "Verification Failed");
            }
            else
            {
                if (value != Single.MaxValue)
                {
                    Complex c_cast_plus = c_cast + 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_plus, value + 1, 0.0))
                    {
                        Console.WriteLine("PLuS + SingleImplicitCast ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }

                if (value != Single.MinValue)
                {
                    Complex c_cast_minus = c_cast - 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_minus, value - 1, 0.0))
                    {
                        Console.WriteLine("Minus - SingleImplicitCast + 1 ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }
            }
        }

        [Fact]
        public static void RunTests_SingleImplicitCastToComplex()
        {
            VerifySingleImplicitCastToComplex(Single.MinValue);

            for (int i = 0; i < Support.RandomSampleCount; ++i)
            {
                Single randomValue = Support.GetRandomSingleValue(false);
                VerifySingleImplicitCastToComplex(randomValue);
            }

            VerifySingleImplicitCastToComplex(0);
            VerifySingleImplicitCastToComplex(1);

            for (int i = 0; i < Support.RandomSampleCount; ++i)
            {
                Single randomValue = Support.GetRandomSingleValue(true);
                VerifySingleImplicitCastToComplex(randomValue);
            }

            VerifySingleImplicitCastToComplex(Single.MaxValue);
        }

        private static void VerifyDoubleImplicitCastToComplex(Double value)
        {
            Complex c_cast = value;

            if (false == Support.VerifyRealImaginaryProperties(c_cast, value, 0.0))
            {
                Console.WriteLine("DoubleImplicitCast ({0})" + value);
                Assert.True(false, "Verification Failed");
            }
            else
            {
                if (value != Double.MaxValue)
                {
                    Complex c_cast_plus = c_cast + 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_plus, value + 1, 0.0))
                    {
                        Console.WriteLine("PLuS + DoubleImplicitCast ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }

                if (value != Double.MinValue)
                {
                    Complex c_cast_minus = c_cast - 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_minus, value - 1, 0.0))
                    {
                        Console.WriteLine("Minus - DoubleImplicitCast + 1 ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }
            }
        }

        [Fact]
        public static void RunTests_DoubleImplicitCastToComplex()
        {
            VerifyDoubleImplicitCastToComplex(Double.MinValue);

            for (int i = 0; i < Support.RandomSampleCount; ++i)
            {
                Double randomValue = Support.GetRandomDoubleValue(false);
                VerifyDoubleImplicitCastToComplex(randomValue);
            }

            VerifyDoubleImplicitCastToComplex(0);
            VerifyDoubleImplicitCastToComplex(1);

            for (int i = 0; i < Support.RandomSampleCount; ++i)
            {
                Double randomValue = Support.GetRandomDoubleValue(true);
                VerifyDoubleImplicitCastToComplex(randomValue);
            }

            VerifyDoubleImplicitCastToComplex(Double.MaxValue);
        }

        private static void VerifyBigIntegerExplicitCastToComplex(BigInteger value)
        {
            Complex c_cast = (Complex)value;

            if (false == Support.VerifyRealImaginaryProperties(c_cast, (Double)value, 0.0))
            {
                Console.WriteLine("BigIntegerExplicitCast ({0})" + value);
                Assert.True(false, "Verification Failed");
            }
            else
            {
                if (value != (BigInteger)Double.MaxValue)
                {
                    Complex c_cast_plus = c_cast + 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_plus, (Double)(value + 1), 0.0))
                    {
                        Console.WriteLine("PLuS + BigIntegerExplicitCast ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }

                if (value != (BigInteger)Double.MinValue)
                {
                    Complex c_cast_minus = c_cast - 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_minus, (Double)(value - 1), 0.0))
                    {
                        Console.WriteLine("Minus - BigIntegerExplicitCast + 1 ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }
            }
        }

        [Fact]
        public static void RunTests_BigIntegerExplicitCastToComplex()
        {
            VerifyBigIntegerExplicitCastToComplex((BigInteger)Double.MinValue);

            for (int i = 0; i < Support.RandomSampleCount; ++i)
            {
                BigInteger randomValue = Support.GetRandomBigIntegerValue(false);
                VerifyBigIntegerExplicitCastToComplex(randomValue);
            }

            VerifyBigIntegerExplicitCastToComplex(0);
            VerifyBigIntegerExplicitCastToComplex(1);

            for (int i = 0; i < Support.RandomSampleCount; ++i)
            {
                BigInteger randomValue = Support.GetRandomBigIntegerValue(true);
                VerifyBigIntegerExplicitCastToComplex(randomValue);
            }

            VerifyBigIntegerExplicitCastToComplex((BigInteger)Double.MaxValue);
        }

        private static void VerifyDecimalExplicitCastToComplex(Decimal value)
        {
            Complex c_cast = (Complex)value;

            if (false == Support.VerifyRealImaginaryProperties(c_cast, (Double)value, 0.0))
            {
                Console.WriteLine("DecimalExplicitCast ({0})" + value);
                Assert.True(false, "Verification Failed");
            }
            else
            {
                if (value != Decimal.MaxValue)
                {
                    Complex c_cast_plus = c_cast + 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_plus, (Double)(value + 1), 0.0))
                    {
                        Console.WriteLine("PLuS + DecimalExplicitCast ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }

                if (value != Decimal.MinValue)
                {
                    Complex c_cast_minus = c_cast - 1;
                    if (false == Support.VerifyRealImaginaryProperties(c_cast_minus, (Double)(value - 1), 0.0))
                    {
                        Console.WriteLine("Minus - DecimalExplicitCast + 1 ({0})" + value);
                        Assert.True(false, "Verification Failed");
                    }
                }
            }
        }

        [Fact]
        public static void RunTests_DecimalExplicitCastToComplex()
        {
            VerifyDecimalExplicitCastToComplex(Decimal.MinValue);

            for (int i = 0; i < Support.RandomSampleCount; ++i)
            {
                Decimal randomValue = Support.GetRandomDecimalValue(false);
                VerifyDecimalExplicitCastToComplex(randomValue);
            }

            VerifyDecimalExplicitCastToComplex(0);
            VerifyDecimalExplicitCastToComplex(1);

            for (int i = 0; i < Support.RandomSampleCount; ++i)
            {
                Decimal randomValue = Support.GetRandomDecimalValue(true);
                VerifyDecimalExplicitCastToComplex(randomValue);
            }

            VerifyDecimalExplicitCastToComplex(Decimal.MaxValue);
        }
    }
}
