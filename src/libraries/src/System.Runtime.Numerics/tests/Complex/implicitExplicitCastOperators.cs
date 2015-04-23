// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ComplexTestSupport;
using Xunit;

namespace System.Numerics.Tests
{
    public class implicitExplicitCastOperatorsTest
    {
        private static void VerifyInt16ImplicitCastToComplex(Int16 value)
        {
            Complex c_cast = value;

            Support.VerifyRealImaginaryProperties(c_cast, value, 0.0, 
                string.Format("Int16ImplicitCast ({0})", value));
            
            if (value != Int16.MaxValue)
            {
                Complex c_cast_plus = c_cast + 1;
                Support.VerifyRealImaginaryProperties(c_cast_plus, value + 1, 0.0, 
                    string.Format("PLuS + Int16ImplicitCast ({0})", value));
            }

            if (value != Int16.MinValue)
            {
                Complex c_cast_minus = c_cast - 1;
                Support.VerifyRealImaginaryProperties(c_cast_minus, value - 1, 0.0, 
                    string.Format("Minus - Int16ImplicitCast + 1 ({0})", value));
            }
        }

        [Fact]
        public static void RunTests_Int16ImplicitCastToComplex()
        {
            VerifyInt16ImplicitCastToComplex(Int16.MinValue);

            for (int i = 0; i < 3; ++i)
            {
                Int16 randomValue = Support.GetRandomInt16Value(true);
                VerifyInt16ImplicitCastToComplex(randomValue);
            }

            VerifyInt16ImplicitCastToComplex(-1);
            VerifyInt16ImplicitCastToComplex(0);
            VerifyInt16ImplicitCastToComplex(1);

            for (int i = 0; i < 3; ++i)
            {
                Int16 randomValue = Support.GetRandomInt16Value(false);
                VerifyInt16ImplicitCastToComplex(randomValue);
            }

            VerifyInt16ImplicitCastToComplex(Int16.MaxValue);
        }

        private static void VerifyInt32ImplicitCastToComplex(Int32 value)
        {
            Complex c_cast = value;

            Support.VerifyRealImaginaryProperties(c_cast, value, 0.0, 
                string.Format("Int32ImplicitCast ({0})", value));
            
            if (value != Int32.MaxValue)
            {
                Complex c_cast_plus = c_cast + 1;
                Support.VerifyRealImaginaryProperties(c_cast_plus, value + 1, 0.0,
                    string.Format("PLuS + Int32ImplicitCast ({0})", value));
            }

            if (value != Int32.MinValue)
            {
                Complex c_cast_minus = c_cast - 1;
                Support.VerifyRealImaginaryProperties(c_cast_minus, value - 1, 0.0,
                    string.Format("Minus - Int32ImplicitCast + 1 ({0})", value));
            }
        }

        [Fact]
        public static void RunTests_Int32ImplicitCastToComplex()
        {
            VerifyInt32ImplicitCastToComplex(Int32.MinValue);

            for (int i = 0; i < 3; ++i)
            {
                Int32 randomValue = Support.GetRandomInt32Value(true);
                VerifyInt32ImplicitCastToComplex(randomValue);
            }

            VerifyInt32ImplicitCastToComplex(-1);
            VerifyInt32ImplicitCastToComplex(0);
            VerifyInt32ImplicitCastToComplex(1);

            for (int i = 0; i < 3; ++i)
            {
                Int32 randomValue = Support.GetRandomInt32Value(false);
                VerifyInt32ImplicitCastToComplex(randomValue);
            }

            VerifyInt32ImplicitCastToComplex(Int32.MaxValue);
        }

        private static void VerifyInt64ImplicitCastToComplex(Int64 value)
        {
            Complex c_cast = value;

            Support.VerifyRealImaginaryProperties(c_cast, value, 0.0, 
                string.Format("Int64ImplicitCast ({0})", value));
           
            if (value != Int64.MaxValue)
            {
                Complex c_cast_plus = c_cast + 1;
                Support.VerifyRealImaginaryProperties(c_cast_plus, value + 1, 0.0, 
                    string.Format("PLuS + Int64ImplicitCast ({0})", value));
            }

            if (value != Int64.MinValue)
            {
                Complex c_cast_minus = c_cast - 1;
                Support.VerifyRealImaginaryProperties(c_cast_minus, value - 1, 0.0, 
                    string.Format("Minus - Int64ImplicitCast + 1 ({0})", value));
            }
        }

        [Fact]
        public static void RunTests_Int64ImplicitCastToComplex()
        {
            VerifyInt64ImplicitCastToComplex(Int64.MinValue);

            for (int i = 0; i < 3; ++i)
            {
                Int64 randomValue = Support.GetRandomInt64Value(true);
                VerifyInt64ImplicitCastToComplex(randomValue);
            }

            VerifyInt64ImplicitCastToComplex(-1);
            VerifyInt64ImplicitCastToComplex(0);
            VerifyInt64ImplicitCastToComplex(1);

            for (int i = 0; i < 3; ++i)
            {
                Int64 randomValue = Support.GetRandomInt64Value(false);
                VerifyInt64ImplicitCastToComplex(randomValue);
            }

            VerifyInt64ImplicitCastToComplex(Int64.MaxValue);
        }

        private static void VerifyUInt16ImplicitCastToComplex(UInt16 value)
        {
            Complex c_cast = value;

            Support.VerifyRealImaginaryProperties(c_cast, value, 0.0,
                string.Format("UInt16ImplicitCast ({0})", value));
            
            if (value != UInt16.MaxValue)
            {
                Complex c_cast_plus = c_cast + 1;
                Support.VerifyRealImaginaryProperties(c_cast_plus, value + 1, 0.0,
                    string.Format("PLuS + UInt16ImplicitCast ({0})", value));
            }

            if (value != UInt16.MinValue)
            {
                Complex c_cast_minus = c_cast - 1;
                Support.VerifyRealImaginaryProperties(c_cast_minus, value - 1, 0.0,
                    string.Format("Minus - UInt16ImplicitCast + 1 ({0})", value));
            }
        }

        [Fact]
        public static void RunTests_UInt16ImplicitCastToComplex()
        {
            VerifyUInt16ImplicitCastToComplex(UInt16.MinValue);
            VerifyUInt16ImplicitCastToComplex(0);
            VerifyUInt16ImplicitCastToComplex(1);

#if  CLS_Compliant
        for (int i = 0; i < 3; ++i)
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

            Support.VerifyRealImaginaryProperties(c_cast, value, 0.0, 
                string.Format("UInt32ImplicitCast ({0})", value));
            
            if (value != UInt32.MaxValue)
            {
                Complex c_cast_plus = c_cast + 1;
                Support.VerifyRealImaginaryProperties(c_cast_plus, value + 1, 0.0,
                    string.Format("PLuS + UInt32ImplicitCast ({0})", value));
            }

            if (value != UInt32.MinValue)
            {
                Complex c_cast_minus = c_cast - 1;
                Support.VerifyRealImaginaryProperties(c_cast_minus, value - 1, 0.0,
                    string.Format("Minus - UInt32ImplicitCast + 1 ({0})", value));
            }
        }

        [Fact]
        public static void RunTests_UInt32ImplicitCastToComplex()
        {
            VerifyUInt32ImplicitCastToComplex(UInt32.MinValue);
            VerifyUInt32ImplicitCastToComplex(0);
            VerifyUInt32ImplicitCastToComplex(1);

#if  CLS_Compliant
        for (int i = 0; i < 3; ++i)
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

            Support.VerifyRealImaginaryProperties(c_cast, value, 0.0,
                string.Format("UInt64ImplicitCast ({0})", value));
            
            if (value != UInt64.MaxValue)
            {
                Complex c_cast_plus = c_cast + 1;
                Support.VerifyRealImaginaryProperties(c_cast_plus, value + 1, 0.0,
                    string.Format("PLuS + UInt64ImplicitCast ({0})", value));
            }

            if (value != UInt64.MinValue)
            {
                Complex c_cast_minus = c_cast - 1;
                Support.VerifyRealImaginaryProperties(c_cast_minus, value - 1, 0.0,
                    string.Format("Minus - UInt64ImplicitCast + 1 ({0})", value));
            }
        }

        [Fact]
        public static void RunTests_UInt64ImplicitCastToComplex()
        {
            VerifyUInt64ImplicitCastToComplex(UInt64.MinValue);
            VerifyUInt64ImplicitCastToComplex(0);
            VerifyUInt64ImplicitCastToComplex(1);

#if CLS_Compliant
        for (int i = 0; i < 3; ++i)
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

            Support.VerifyRealImaginaryProperties(c_cast, value, 0.0,
                string.Format("SByteImplicitCast ({0})", value));
            
            if (value != SByte.MaxValue)
            {
                Complex c_cast_plus = c_cast + 1;
                Support.VerifyRealImaginaryProperties(c_cast_plus, value + 1, 0.0,
                    string.Format("PLuS + SByteImplicitCast ({0})", value));
            }

            if (value != SByte.MinValue)
            {
                Complex c_cast_minus = c_cast - 1;
                Support.VerifyRealImaginaryProperties(c_cast_minus, value - 1, 0.0,
                    string.Format("Minus - SByteImplicitCast + 1 ({0})", value));
            }
        }

        [Fact]
        public static void RunTests_SByteImplicitCastToComplex()
        {
            VerifySByteImplicitCastToComplex(SByte.MinValue);
#if  CLS_Compliant
        for (int i = 0; i < 3; ++i)
        {
            SByte randomValue = Support.GetRandomSByteValue(false);
            VerifySByteImplicitCastToComplex(randomValue);
        }
#endif
            VerifySByteImplicitCastToComplex(0);
            VerifySByteImplicitCastToComplex(1);

#if  CLS_Compliant
        for (int i = 0; i < 3; ++i)
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

            Support.VerifyRealImaginaryProperties(c_cast, value, 0.0, 
                string.Format("ByteImplicitCast ({0})", value));
           
            if (value != Byte.MaxValue)
            {
                Complex c_cast_plus = c_cast + 1;
                Support.VerifyRealImaginaryProperties(c_cast_plus, value + 1, 0.0,
                    string.Format("PLuS + ByteImplicitCast ({0})", value));
            }

            if (value != Byte.MinValue)
            {
                Complex c_cast_minus = c_cast - 1;
                Support.VerifyRealImaginaryProperties(c_cast_minus, value - 1, 0.0,
                    string.Format("Minus - ByteImplicitCast + 1 ({0})", value));
            }
        }

        [Fact]
        public static void RunTests_ByteImplicitCastToComplex()
        {
            VerifyByteImplicitCastToComplex(Byte.MinValue);
            VerifyByteImplicitCastToComplex(0);
            VerifyByteImplicitCastToComplex(1);

            for (int i = 0; i < 3; ++i)
            {
                Byte randomValue = Support.GetRandomByteValue();
                VerifyByteImplicitCastToComplex(randomValue);
            }

            VerifyByteImplicitCastToComplex(Byte.MaxValue);
        }

        private static void VerifySingleImplicitCastToComplex(Single value)
        {
            Complex c_cast = value;

            Support.VerifyRealImaginaryProperties(c_cast, value, 0.0,
                string.Format("SingleImplicitCast ({0})",  value));
            if (value != Single.MaxValue)
            {
                Complex c_cast_plus = c_cast + 1;
                Support.VerifyRealImaginaryProperties(c_cast_plus, value + 1, 0.0, 
                    string.Format("PLuS + SingleImplicitCast ({0})", value));
            }

            if (value != Single.MinValue)
            {
                Complex c_cast_minus = c_cast - 1;
                Support.VerifyRealImaginaryProperties(c_cast_minus, value - 1, 0.0, 
                    string.Format("Minus - SingleImplicitCast + 1 ({0})", value));
            }
        }

        [Fact]
        public static void RunTests_SingleImplicitCastToComplex()
        {
            VerifySingleImplicitCastToComplex(Single.MinValue);

            for (int i = 0; i < 3; ++i)
            {
                Single randomValue = Support.GetRandomSingleValue(false);
                VerifySingleImplicitCastToComplex(randomValue);
            }

            VerifySingleImplicitCastToComplex(0);
            VerifySingleImplicitCastToComplex(1);

            for (int i = 0; i < 3; ++i)
            {
                Single randomValue = Support.GetRandomSingleValue(true);
                VerifySingleImplicitCastToComplex(randomValue);
            }

            VerifySingleImplicitCastToComplex(Single.MaxValue);
        }

        private static void VerifyDoubleImplicitCastToComplex(double value)
        {
            Complex c_cast = value;

            Support.VerifyRealImaginaryProperties(c_cast, value, 0.0, 
                string.Format("DoubleImplicitCast ({0})", value));
            if (value != double.MaxValue)
            {
                Complex c_cast_plus = c_cast + 1;
                Support.VerifyRealImaginaryProperties(c_cast_plus, value + 1, 0.0,
                    string.Format("PLuS + DoubleImplicitCast ({0})", value));
            }

            if (value != double.MinValue)
            {
                Complex c_cast_minus = c_cast - 1;
                Support.VerifyRealImaginaryProperties(c_cast_minus, value - 1, 0.0, 
                    string.Format("Minus - DoubleImplicitCast + 1 ({0})", value));
            }
        }

        [Fact]
        public static void RunTests_DoubleImplicitCastToComplex()
        {
            VerifyDoubleImplicitCastToComplex(double.MinValue);

            for (int i = 0; i < 3; ++i)
            {
                double randomValue = Support.GetRandomDoubleValue(false);
                VerifyDoubleImplicitCastToComplex(randomValue);
            }

            VerifyDoubleImplicitCastToComplex(0);
            VerifyDoubleImplicitCastToComplex(1);

            for (int i = 0; i < 3; ++i)
            {
                double randomValue = Support.GetRandomDoubleValue(true);
                VerifyDoubleImplicitCastToComplex(randomValue);
            }

            VerifyDoubleImplicitCastToComplex(double.MaxValue);
        }

        private static void VerifyBigIntegerExplicitCastToComplex(BigInteger value)
        {
            Complex c_cast = (Complex)value;

            Support.VerifyRealImaginaryProperties(c_cast, (Double)value, 0.0,
                string.Format("BigIntegerExplicitCast ({0})", value));
            if (value != (BigInteger)double.MaxValue)
            {
                Complex c_cast_plus = c_cast + 1;
                Support.VerifyRealImaginaryProperties(c_cast_plus, (Double)(value + 1), 0.0,
                    string.Format("PLuS + BigIntegerExplicitCast ({0})", value));
            }

            if (value != (BigInteger)double.MinValue)
            {
                Complex c_cast_minus = c_cast - 1;
                Support.VerifyRealImaginaryProperties(c_cast_minus, (Double)(value - 1), 0.0,
                    string.Format("Minus - BigIntegerExplicitCast + 1 ({0})", value));
            }
        }

        [Fact]
        public static void RunTests_BigIntegerExplicitCastToComplex()
        {
            VerifyBigIntegerExplicitCastToComplex((BigInteger)double.MinValue);

            for (int i = 0; i < 3; ++i)
            {
                BigInteger randomValue = Support.GetRandomBigIntegerValue(false);
                VerifyBigIntegerExplicitCastToComplex(randomValue);
            }

            VerifyBigIntegerExplicitCastToComplex(0);
            VerifyBigIntegerExplicitCastToComplex(1);

            for (int i = 0; i < 3; ++i)
            {
                BigInteger randomValue = Support.GetRandomBigIntegerValue(true);
                VerifyBigIntegerExplicitCastToComplex(randomValue);
            }

            VerifyBigIntegerExplicitCastToComplex((BigInteger)double.MaxValue);
        }

        private static void VerifyDecimalExplicitCastToComplex(Decimal value)
        {
            Complex c_cast = (Complex)value;

            Support.VerifyRealImaginaryProperties(c_cast, (Double)value, 0.0, 
                string.Format("DecimalExplicitCast ({0})", value));
            if (value != Decimal.MaxValue)
            {
                Complex c_cast_plus = c_cast + 1;
                Support.VerifyRealImaginaryProperties(c_cast_plus, (Double)(value + 1), 0.0, 
                    string.Format("PLuS + DecimalExplicitCast ({0})", value));
            }

            if (value != Decimal.MinValue)
            {
                Complex c_cast_minus = c_cast - 1;
                Support.VerifyRealImaginaryProperties(c_cast_minus, (Double)(value - 1), 0.0,
                    string.Format("Minus - DecimalExplicitCast + 1 ({0})", value));
            }
        }

        [Fact]
        public static void RunTests_DecimalExplicitCastToComplex()
        {
            VerifyDecimalExplicitCastToComplex(Decimal.MinValue);

            for (int i = 0; i < 3; ++i)
            {
                Decimal randomValue = Support.GetRandomDecimalValue(false);
                VerifyDecimalExplicitCastToComplex(randomValue);
            }

            VerifyDecimalExplicitCastToComplex(0);
            VerifyDecimalExplicitCastToComplex(1);

            for (int i = 0; i < 3; ++i)
            {
                Decimal randomValue = Support.GetRandomDecimalValue(true);
                VerifyDecimalExplicitCastToComplex(randomValue);
            }

            VerifyDecimalExplicitCastToComplex(Decimal.MaxValue);
        }
    }
}
