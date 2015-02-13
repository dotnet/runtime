// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace Test
{
    public class Co6065ToSingle_all
    {
        [Fact]
        public static void runTest()
        {
            ///////////////////////////////////////////// Single Convert.ToSingle( Double )

            //[] ToSingle(Double) - Vanilla Cases (1000.0,100.0,0.0,-100.0,-1000.0,double.MaxValue,double.MinValue)

            // Setup Double Test
            {
                Double[] testValues = { 1000.0, 100.0, 0.0, -100.0, -1000.0, Double.MaxValue, Double.MinValue };
                Single[] expectedValues = { ((Single)1000.0f), ((Single)100.0f), ((Single)0.0f), -100.0f, -1000.0f, Single.PositiveInfinity, Single.NegativeInfinity };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Single result = Convert.ToSingle(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Single Convert.ToSingle( Int32 )

            //[] ToSingle(Int32) - Vanilla Cases (Int32.MaxValue,Int32.MinValue,((Int32)0))

            // Setup Int32 Test
            {
                Int32[] testValues = { Int32.MaxValue, Int32.MinValue, ((Int32)0), };
                Single[] expectedValues = { (Single)Int32.MaxValue, (Single)Int32.MinValue, (Single)0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Single result = Convert.ToSingle(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Single Convert.ToSingle( Int64 )

            //[] ToSingle(Int64) - Vanilla Cases (Int64.MaxValue,Int64.MinValue,((Int64)0))

            // Setup Int64 Test
            {
                Int64[] testValues = { Int64.MaxValue, Int64.MinValue, ((Int64)0), };
                Single[] expectedValues = { (Single)Int64.MaxValue, (Single)Int64.MinValue, (Single)0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Single result = Convert.ToSingle(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Single Convert.ToSingle( Int16 )

            //[] ToSingle(Int16) - Vanilla Cases (Int16.MaxValue,Int16.MinValue,((Int16)0))

            // Setup Int16 Test
            {
                Int16[] testValues = { Int16.MaxValue, Int16.MinValue, ((Int16)0), };
                Single[] expectedValues = { (Single)Int16.MaxValue, (Single)Int16.MinValue, (Single)0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Single result = Convert.ToSingle(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ///////////////////////////////////////////// []Single Convert.ToSingle( Boolean )

            // Setup Int16 Test
            {
                Boolean[] testValues = { false, true, };
                Single[] expectedValues = { 0.0f, 1.0f, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Single result = Convert.ToSingle(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Single Convert.ToSingle( Decimal )

            //[] ToSingle(Decimal) - Vanilla Cases (new Decimal( 1000.0 ),new Decimal( 0.0 ),new Decimal( -1000.0 ),Decimal.MaxValue,Decimal.MinValue)

            // Setup Decimal Test
            {
                Decimal[] testValues = { new Decimal(1000.0), new Decimal(0.0), new Decimal(-1000.0), Decimal.MaxValue, Decimal.MinValue, };
                Single[] expectedValues = { ((Single)1000.0f), ((Single)0.0f), -1000.0f, (Single)Decimal.MaxValue, (Single)Decimal.MinValue, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Single result = Convert.ToSingle(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Single Convert.ToSingle( String )

            //[] ToSingle(String) - Vanilla Cases (Single.MaxValue.ToString(),"0.0",Single.MinValue.ToString(),double.MaxValue.ToString(),double.MinValue.ToString())

            // Setup String Test
            {
                String[] testValues = { Single.MaxValue.ToString(), (0.0f).ToString(), Single.MinValue.ToString(), null, };
                Single[] expectedValues = { Single.MaxValue, ((Single)0.0f), Single.MinValue, ((Single)0.0f), };                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Single result = Convert.ToSingle(testValues[i]);
                    // We are converting them to their string representation because if we compare Single.MaxValue 
                    // to one parsed from its string representation, it is not equal.
                    Assert.Equal(expectedValues[i].ToString(), result.ToString());
                }
            }
            ///////////////////////////////////////////// []Single Convert.ToSingle( String, IFormatPRovider )

            // Setup String Test
            {
                String[] testValues = { Single.MaxValue.ToString(), (0.0f).ToString(), Single.MinValue.ToString(), null, };
                Single[] expectedValues = { Single.MaxValue, ((Single)0.0f), Single.MinValue, ((Single)0.0f), };
                // Vanila Test Cases

                for (int i = 0; i < testValues.Length; i++)
                {
                    Single result = Convert.ToSingle(testValues[i], new TestFormatProvider());
                    Assert.Equal(expectedValues[i].ToString(), result.ToString());
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Single Convert.ToSingle( Byte )

            //[] ToSingle(Byte) - Vanilla Cases (Byte.MaxValue,Byte.MinValue)

            // Setup Byte Test
            {
                Byte[] testValues = { Byte.MaxValue, Byte.MinValue, };
                Single[] expectedValues = { (Single)Byte.MaxValue, (Single)Byte.MinValue, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Single result = Convert.ToSingle(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Single Convert.ToSingle( UInt64 )

            //[] ToSingle(UInt64) - Vanilla Cases (UInt64.MaxValue,UInt64.MinValue)

            // Setup UInt64 Test
            {
                UInt64[] testValues = { UInt64.MaxValue, UInt64.MinValue, };
                Single[] expectedValues = { (Single)UInt64.MaxValue, (Single)UInt64.MinValue, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Single result = Convert.ToSingle(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Single Convert.ToSingle( UInt32 )

            //[] ToSingle(UInt32) - Vanilla Cases (UInt32.MaxValue,UInt32.MinValue)

            // Setup UInt32 Test
            {
                UInt32[] testValues = { UInt32.MaxValue, UInt32.MinValue, };
                Single[] expectedValues = { (Single)UInt32.MaxValue, (Single)UInt32.MinValue, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Single result = Convert.ToSingle(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Single Convert.ToSingle( UInt16 )

            //[] ToSingle(UInt16) - Vanilla Cases (UInt16.MaxValue,UInt16.MinValue)

            // Setup UInt16 Test
            {
                UInt16[] testValues = { UInt16.MaxValue, UInt16.MinValue, };
                Single[] expectedValues = { (Single)UInt16.MaxValue, (Single)UInt16.MinValue, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Single result = Convert.ToSingle(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Single Convert.ToSingle( SByte )

            //[] ToSingle(SByte) - Vanilla Cases (100,-100,0)

            // Setup SByte Test
            {
                SByte[] testValues = { 100, -100, 0, };
                Single[] expectedValues = { ((Single)100), -100, ((Single)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Single result = Convert.ToSingle(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Single Convert.ToSingle( Single )

            //[] ToSingle(Single) - Vanilla Cases (Single.MaxValue,Single.MinValue, new Single(), Single.NegativeInfinity, Single.PositiveInfinity, Single.Epsilon)

            // Setup SByte Test
            {
                Single[] testValues = { Single.MaxValue, Single.MinValue, new Single(), Single.NegativeInfinity, Single.PositiveInfinity, Single.Epsilon };
                Single[] expectedValues = { Single.MaxValue, Single.MinValue, new Single(), Single.NegativeInfinity, Single.PositiveInfinity, Single.Epsilon };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Single result = Convert.ToSingle(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            //[] ToSingle( Object ) - obj = null
            {
                Single bTest = Convert.ToSingle((Object)null);
                Assert.Equal(0, bTest);
            }
            //[] ToSingle( Object, IFP ) - obj = null
            {
                Single bTest = Convert.ToSingle((Object)null, new TestFormatProvider());
                Assert.Equal(0, bTest);
            }
        }

        [Fact]
        public static void runTests_Negative()
        {
            // Exception Test Cases
            //[] ToSingle(String) - Exception Cases (null,"1f2d")
            Assert.Throws<OverflowException>(() => { Single result = Convert.ToSingle(Double.MaxValue.ToString()); });
            Assert.Throws<OverflowException>(() => { Single result = Convert.ToSingle(Double.MinValue.ToString()); });
            Assert.Throws<FormatException>(() => { Single result = Convert.ToSingle("1f2d"); });

            // Exception Test Cases
            //[] ToSingle(String) - Exception Cases (null,"1f2d")
            Assert.Throws<FormatException>(() => { Single result = Convert.ToSingle("1f2d", new TestFormatProvider()); });

            ///////////////////////////////////////////// Single Convert.ToSingle( Object )
            //[] ToSingle( Object ) - Exception Case (Object that does not implement IConvertible) 
            Assert.Throws<InvalidCastException>(() => { Single bTest = Convert.ToSingle(new Object()); });

            ///////////////////////////////////////////// []Single Convert.ToSingle( Object, IFormatPRovider )
            Assert.Throws<InvalidCastException>(() => { Single bTest = Convert.ToSingle(new Object(), new TestFormatProvider()); });

            ///////////////////////////////////////////// []Single Convert.ToSingle( DateTime )
            Assert.Throws<InvalidCastException>(() => { Single bTest = Convert.ToSingle(DateTime.Now); });
        }
    }
}
