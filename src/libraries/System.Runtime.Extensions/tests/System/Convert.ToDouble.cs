// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace Test
{
    public class Co6059ToDouble_all
    {
        [Fact]
        public static void runTest()
        {
            ///////////////////////////////////////////// Double Convert.ToDouble( Single )

            //[] ToDouble(Single) - Vanilla Cases (Single.MaxValue, Single.MinValue,((Single) 0.0f ))

            // Setup Single Test

            {
                Single[] testValues = { Single.MaxValue, Single.MinValue, ((Single)0.0f), };
                Double[] expectedValues = { ((Double)Single.MaxValue), ((Double)Single.MinValue), ((Double)0.0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Double result = Convert.ToDouble(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Double Convert.ToDouble( Int32 )

            //[] ToDouble(Int32) - Vanilla Cases (Int32.MaxValue,Int32.MinValue,0)

            // Setup Int32 Test
            {
                Int32[] testValues = { Int32.MaxValue, Int32.MinValue, 0, };
                Double[] expectedValues = { ((Double)Int32.MaxValue), ((Double)Int32.MinValue), ((Double)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Double result = Convert.ToDouble(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Double Convert.ToDouble( Int64 )

            //[] ToDouble(Int64) - Vanilla Cases (Int64.MaxValue,Int64.MinValue,(long) 0)

            // Setup Int64 Test
            {
                Int64[] testValues = { Int64.MaxValue, Int64.MinValue, (long)0, };
                Double[] expectedValues = { (Double)Int64.MaxValue, (Double)Int64.MinValue, ((Double)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Double result = Convert.ToDouble(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Double Convert.ToDouble( Int16 )

            //[] ToDouble(Int16) - Vanilla Cases (Int16.MaxValue,Int16.MinValue,(short) 0)

            // Setup Int16 Test
            {
                Int16[] testValues = { Int16.MaxValue, Int16.MinValue, (short)0, };
                Double[] expectedValues = { (Double)Int16.MaxValue, (Double)Int16.MinValue, ((Double)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Double result = Convert.ToDouble(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ///////////////////////////////////////////// [] Double Convert.ToDouble( Boolean )

            // Setup Int16 Test
            {
                Boolean[] testValues = { true, false, };
                Double[] expectedValues = { 1.0, 0.0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Double result = Convert.ToDouble(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Double Convert.ToDouble( Decimal )

            //[] ToDouble(Decimal) - Vanilla Cases (Decimal.MaxValue,Decimal.MinValue,new Decimal( 0 ))

            // Setup Decimal Test
            {
                Decimal[] testValues = { Decimal.MaxValue, Decimal.MinValue, new Decimal(0), };
                Double[] expectedValues = { (Double)Decimal.MaxValue, (Double)Decimal.MinValue, (Double)(new Decimal(0)), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Double result = Convert.ToDouble(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Double Convert.ToDouble( String )

            //[] ToDouble(String) - Vanilla Cases ("0.0","10.0","-10.0",double.MaxValue.ToString(),double.MinValue.ToString(),"1.7976931348623155E309","-1.7976931348623155E309")

            // Setup String Test
            {
                String[] testValues = { 0.0.ToString(), 10.0.ToString(), (-10.0).ToString(), null, };
                Double[] expectedValues = { 0.0, 10.0, -10.0, 0.0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Double result = Convert.ToDouble(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ///////////////////////////////////////////// [] Double Convert.ToDouble( String, IFormatProvider )

            // Setup String Test
            {
                String[] testValues = { "0.0", "10.0", "-10.0", null, };
                Double[] expectedValues = { 0.0, 10.0, -10.0, 0.0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Double result = Convert.ToDouble(testValues[i], new TestFormatProvider());
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Double Convert.ToDouble( Byte )

            //[] ToDouble(Byte) - Vanilla Cases (Byte.MaxValue,Byte.MinValue)

            // Setup Byte Test
            {
                Byte[] testValues = { Byte.MaxValue, Byte.MinValue, };
                Double[] expectedValues = { (Double)Byte.MaxValue, (Double)Byte.MinValue, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Double result = Convert.ToDouble(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Double Convert.ToDouble( UInt64 )

            //[] ToDouble(UInt64) - Vanilla Cases (UInt64.MaxValue,UInt64.MinValue)

            // Setup UInt64 Test
            {
                UInt64[] testValues = { UInt64.MaxValue, UInt64.MinValue, };
                Double[] expectedValues = { (Double)UInt64.MaxValue, (Double)UInt64.MinValue, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Double result = Convert.ToDouble(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Double Convert.ToDouble( UInt32 )

            //[] ToDouble(UInt32) - Vanilla Cases (UInt32.MaxValue,UInt32.MinValue)

            // Setup UInt32 Test
            {
                UInt32[] testValues = { UInt32.MaxValue, UInt32.MinValue, };
                Double[] expectedValues = { (Double)UInt32.MaxValue, (Double)UInt32.MinValue, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Double result = Convert.ToDouble(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Double Convert.ToDouble( UInt16 )

            //[] ToDouble(UInt16) - Vanilla Cases (UInt16.MaxValue,UInt16.MinValue)

            // Setup UInt16 Test
            {
                UInt16[] testValues = { UInt16.MaxValue, UInt16.MinValue, };
                Double[] expectedValues = { (Double)UInt16.MaxValue, (Double)UInt16.MinValue, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Double result = Convert.ToDouble(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Double Convert.ToDouble( SByte )

            //[] ToDouble(SByte) - Vanilla Cases (SByte.MaxValue,SByte.MinValue)

            // Setup SByte Test
            {
                SByte[] testValues = { SByte.MaxValue, SByte.MinValue, };
                Double[] expectedValues = { (Double)SByte.MaxValue, (Double)SByte.MinValue, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Double result = Convert.ToDouble(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Double Convert.ToDouble( Double )

            //[] ToDouble(Double) - Vanilla Cases (Double.MaxValue,Double.MinValue, Double.Empty, Double.NegativeInfinity, Double.PositiveInfinity, Double.Epsilon)

            // Setup SByte Test
            {
                Double[] testValues = { Double.MaxValue, Double.MinValue, Double.NegativeInfinity, Double.PositiveInfinity, Double.Epsilon };
                Double[] expectedValues = { Double.MaxValue, Double.MinValue, Double.NegativeInfinity, Double.PositiveInfinity, Double.Epsilon };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Double result = Convert.ToDouble(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            //[] ToDouble( Object ) - obj = null
            {
                Double bTest = Convert.ToDouble((Object)null);
                Assert.Equal((Double)0, bTest);
            }

            //[] ToDouble( Object, IFP ) - obj = null
            {
                Double bTest = Convert.ToDouble((Object)null, new TestFormatProvider());
                Assert.Equal((Double)0, bTest);
            }
        }
        [Fact]
        public static void runTests_Negative()
        {
            // Exception Test Cases
            //[] ToDouble(String) - Exception Cases (null,"123xyz")
            Assert.Throws<FormatException>(() => { Double result = Convert.ToDouble("123xyz"); });
            Assert.Throws<OverflowException>(() => { Double result = Convert.ToDouble(Double.MaxValue.ToString()); });
            Assert.Throws<OverflowException>(() => { Double result = Convert.ToDouble(Double.MinValue.ToString()); });

            // Exception Test Cases
            //[] ToDouble(String) - Exception Cases (null,"123xyz")
            Assert.Throws<FormatException>(() => { Double result = Convert.ToDouble("123xyz", new TestFormatProvider()); });
            Assert.Throws<OverflowException>(() => { Double result = Convert.ToDouble(Double.MaxValue.ToString(), new TestFormatProvider()); });
            Assert.Throws<OverflowException>(() => { Double result = Convert.ToDouble(Double.MinValue.ToString(), new TestFormatProvider()); });
            Assert.Throws<OverflowException>(() => { Double result = Convert.ToDouble("1.7976931348623155E309", new TestFormatProvider()); });
            Assert.Throws<OverflowException>(() => { Double result = Convert.ToDouble("-1.7976931348623155E309", new TestFormatProvider()); });

            ///////////////////////////////////////////// Double Convert.ToDouble( Object )
            //[] ToDouble( Object ) - Exception Case (Object that does not implement IConvertible) 
            Assert.Throws<InvalidCastException>(() => { Double bTest = Convert.ToDouble(new Object()); });

            ///////////////////////////////////////////// []Double Convert.ToDouble( Object, IFormatProvider )
            Assert.Throws<InvalidCastException>(() => { Double bTest = Convert.ToDouble(new Object(), new TestFormatProvider()); });

            ///////////////////////////////////////////// []Double Convert.ToDouble( DateTime )
            Assert.Throws<InvalidCastException>(() => { Double bTest = Convert.ToDouble(DateTime.Now); });
            ////////////////////////////////////////////////////////////////////////////////////////
        }
    }
}
