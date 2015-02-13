// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;
// [assembly: System.Reflection.Consumption.EnableDynamicProgramming(typeof(System.ArgumentException))]
// [assembly: System.Reflection.Consumption.EnableDynamicProgramming(typeof(System.FormatException))]

namespace Test
{
    public class Co6062ToInt64_all
    {
        [Fact]
        public static void runTest()
        {
            ///////////////////////////////////////////// Int64 Convert.ToInt64( Boolean )

            //[] ToInt64(Boolean) - Vanilla Cases (true,false)

            // Setup Boolean Test
            {
                Boolean[] testValues = { true, false, };
                Int64[] expectedValues = { (Int64)1, (Int64)0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int64 result = Convert.ToInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int64 Convert.ToInt64( Double )

            //[] ToInt64(Double) - Vanilla Cases (100.0,-100.0,0)

            // Setup Double Test
            {
                Double[] testValues = { 100.0, -100.0, 0, };
                Int64[] expectedValues = { 100, -100, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int64 result = Convert.ToInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int64 Convert.ToInt64( Single )

            //[] ToInt64(Single) - Vanilla Cases (100.0f,-100.0f,0.0f)

            // Setup Single Test
            {
                Single[] testValues = { 100.0f, -100.0f, 0.0f, };
                Int64[] expectedValues = { 100, -100, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int64 result = Convert.ToInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int64 Convert.ToInt64( Int16 )

            //[] ToInt64(Int16) - Vanilla Cases (100 ,-100 ,0)

            // Setup Int16 Test
            {
                Int16[] testValues = { 100, -100, 0, };
                Int64[] expectedValues = { 100, -100, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int64 result = Convert.ToInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int64 Convert.ToInt64( Int32 )

            //[] ToInt64(Int32) - Vanilla Cases (100,-100,0)

            // Setup Int64 Test
            {
                Int32[] testValues = { 100, -100, 0, };
                Int64[] expectedValues = { 100, -100, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int64 result = Convert.ToInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int64 Convert.ToInt64( Decimal )

            //[] ToInt64(Decimal) - Vanilla Cases (Decimal( 100 ),Decimal( -100 ),Decimal( 0.0 ))

            // Setup Decimal Test
            {
                Decimal[] testValues = { new Decimal(100), new Decimal(-100), new Decimal(0.0), };
                Int64[] expectedValues = { 100, -100, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int64 result = Convert.ToInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int64 Convert.ToInt64( String )

            //[] ToInt64(String) - Vanilla Cases ("100","-100","0")

            // Setup String Test
            {
                String[] testValues = { "100", "-100", "0", null, };
                Int64[] expectedValues = { 100, -100, 0, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int64 result = Convert.ToInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ///////////////////////////////////////////// []Int64 Convert.ToInt64( String, IFormatProvider )

            // Setup String Test
            {
                String[] testValues = { "100", "-100", "0", null, };
                Int64[] expectedValues = { 100, -100, 0, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int64 result = Convert.ToInt64(testValues[i], new TestFormatProvider());
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int64 Convert.ToInt64( Byte )

            //[] ToInt64(Byte) - Vanilla Cases (Byte.MaxValue,Byte.MinValue)

            // Setup Byte Test
            {
                Byte[] testValues = { Byte.MaxValue, Byte.MinValue, };
                Int64[] expectedValues = { 255, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int64 result = Convert.ToInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int64 Convert.ToInt64( String, Int32 )

            //[] ToInt64(String,Int32) - Vanilla Cases {("7FFFFFFFFFFFFFFF",16),("9223372036854775807",10),("777777777777777777777",8),("111111111111111111111111111111111111111111111111111111111111111",2),("8000000000000000",16),("-9223372036854775808",10),("1000000000000000000000",8),("1000000000000000000000000000000000000000000000000000000000000000"2)}

            // Setup String, Int32 Test
            {
                String[] dummy = { null, };
                Assert.Equal(0, Convert.ToInt64(dummy[0], 10));
                Assert.Equal(0, Convert.ToInt64(dummy[0], 2));
                Assert.Equal(0, Convert.ToInt64(dummy[0], 8));
                Assert.Equal(0, Convert.ToInt64(dummy[0], 16));
            }
            {
                String[] testValues = { "7FFFFFFFFFFFFFFF", "9223372036854775807", "777777777777777777777", "111111111111111111111111111111111111111111111111111111111111111", "8000000000000000", "-9223372036854775808", "1000000000000000000000", "1000000000000000000000000000000000000000000000000000000000000000", };
                Int32[] testBases = { 16, 10, 8, 2, 16, 10, 8, 2, };
                Int64[] expectedValues = { Int64.MaxValue, Int64.MaxValue, Int64.MaxValue, Int64.MaxValue, Int64.MinValue, Int64.MinValue, Int64.MinValue, Int64.MinValue, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int64 result = Convert.ToInt64(testValues[i], testBases[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int64 Convert.ToInt64( SByte )

            //[] ToInt64(SByte) - Vanilla Cases (100,-100,0)

            // Setup SByte Test
            {
                SByte[] testValues = { 100, -100, 0, };
                Int64[] expectedValues = { 100, -100, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int64 result = Convert.ToInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int64 Convert.ToInt64( UInt16 )

            //[] ToInt64(UInt16) - Vanilla Cases (100,0)

            // Setup UInt16 Test
            {
                UInt16[] testValues = { 100, 0, };
                Int64[] expectedValues = { 100, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int64 result = Convert.ToInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int64 Convert.ToInt64( UInt32 )

            //[] ToInt64(UInt32) - Vanilla Cases (100,0)

            // Setup UInt32 Test
            {
                UInt32[] testValues = { 100, 0, };
                Int64[] expectedValues = { 100, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int64 result = Convert.ToInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int64 Convert.ToInt64( UInt64 )

            //[] ToInt64(UInt64) - Vanilla Cases (100,0)

            // Setup UInt64 Test
            {
                UInt64[] testValues = { 100, 0, };
                Int64[] expectedValues = { 100, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int64 result = Convert.ToInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int64 Convert.ToInt64( Int64 )

            //[] ToInt64(Int64) - Vanilla cases (Int64.MaxValue,Int64.MinValue, 0)

            // Setup Int64 Test
            {
                Int64[] testValues = { Int64.MaxValue, Int64.MinValue, 0 };
                Int64[] expectedValues = { Int64.MaxValue, Int64.MinValue, 0 };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int64 result = Convert.ToInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int64 Convert.ToInt64( Char )

            //[] ToInt64(Char) - Vanilla Cases (Char.MaxValue,Char.MinValue, 'b')

            // Setup Char Test
            {
                Char[] testValues = { Char.MaxValue, Char.MinValue, 'b' };
                Int64[] expectedValues = { (Int64)Char.MaxValue, (Int64)Char.MinValue, 98 };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int64 result = Convert.ToInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            //[] ToInt64( Object ) - obj = null
            {
                Int64 bTest = Convert.ToInt64((Object)null);
                Assert.Equal(0, bTest);
            }

            {
                Int64 bTest = Convert.ToInt64((Object)null, new TestFormatProvider());
                Assert.Equal(0, bTest);
            }
        }

        [Fact]
        public static void runTests_Negative()
        {
            {
                // Exception Test Cases
                //[] ToInt64(Double) - Exception Cases (Double.MaxValue,Double.MinValue)

                Double[] errorValues = { Double.MaxValue, Double.MinValue, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { Int64 result = Convert.ToInt64(errorValues[i]); });
                }
            }
            {
                // Exception Test Cases
                //[] ToInt64(Single) - Exception Cases (Single.MaxValue,Single.MinValue)
                Single[] errorValues = { Single.MaxValue, Single.MinValue, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { Int64 result = Convert.ToInt64(errorValues[i]); });
                }
            }
            {
                // Exception Test Cases
                //[] ToInt64(Decimal) - Exception Cases (Decimal.MaxValue,Decimal.MinValue)

                Decimal[] errorValues = { Decimal.MaxValue, Decimal.MinValue, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { Int64 result = Convert.ToInt64(errorValues[i]); });
                }
            }
            // Exception Test Cases
            //[] ToInt64(String) - Exception Cases ("1" + Int64.MaxValue.ToString(),Int64.MinValue.ToString()+ "1",null,"abba")

            Assert.Throws<OverflowException>(() => { Int64 result = Convert.ToInt64("1" + Int64.MaxValue.ToString()); });
            Assert.Throws<OverflowException>(() => { Int64 result = Convert.ToInt64(Int64.MinValue.ToString() + "1"); });
            Assert.Throws<FormatException>(() => { Int64 result = Convert.ToInt64("abba"); });

            // Exception Test Cases
            //[] ToInt64(String) - Exception Cases ("1" + Int64.MaxValue.ToString(),Int64.MinValue.ToString()+ "1",null,"abba")
            Assert.Throws<OverflowException>(() => { Int64 result = Convert.ToInt64("1" + Int64.MaxValue.ToString(), new TestFormatProvider()); });
            Assert.Throws<OverflowException>(() => { Int64 result = Convert.ToInt64(Int64.MinValue.ToString() + "1", new TestFormatProvider()); });
            Assert.Throws<FormatException>(() => { Int64 result = Convert.ToInt64("abba", new TestFormatProvider()); });

            {
                // Exception Test Cases
                //[] ToInt64(String,Int32) - Exception Cases {(null,2),("12",3),("11",5),("abba",8),("ffffffffffffffffffff",16)}
                String[] expectedExceptions = { "System.FormatException", "System.ArgumentException", "System.ArgumentException", "System.ArgumentException", "System.FormatException", "System.ArgumentException", "System.OverflowException", "System.OverflowException", "System.OverflowException", "System.OverflowException", "System.OverflowException", };
                String[] errorValues = { "12", "10", "11", "abba", "ffffffffffffffffffff", "-ab", "9223372036854775808", "-9223372036854775809", "11111111111111111111111111111111111111111111111111111111111111111", "1FFFFffffFFFFffff", "7777777777777777777777777", };
                Int32[] errorBases = { 2, -1, 3, 0, 8, 16, 10, 10, 2, 16, 8, };

                for (int i = 0; i < errorValues.Length; i++)
                {
                    try
                    {
                        Int64 result = Convert.ToInt64(errorValues[i], errorBases[i]);
                        Assert.True(false, "Exception expected: " + expectedExceptions[i]);
                    }
                    catch (Exception e)
                    {
                        Assert.True(e.GetType().FullName.Equals(expectedExceptions[i]), " Wrong Exception Thrown. Expected" + expectedExceptions[i] + ", Actual: " + e.GetType().FullName);
                    }
                }
            }

            // Exception Test Cases
            //[] ToInt64(UInt64) - Exception Cases (UInt64.MaxValue)
            Assert.Throws<OverflowException>(() => { Int64 result = Convert.ToInt64(UInt64.MaxValue); });

            ///////////////////////////////////////////// Int64 Convert.ToInt64( Object )
            //[] ToInt64( Object ) - Exception Case (Object that does not implement IConvertible) 
            Assert.Throws<InvalidCastException>(() => { Int64 bTest = Convert.ToInt64(new Object()); });

            ///////////////////////////////////////////// []Int64 Convert.ToInt64( Object, IFormatProvider )
            Assert.Throws<InvalidCastException>(() => { Int64 bTest = Convert.ToInt64(new Object(), new TestFormatProvider()); });

            ///////////////////////////////////////////// []Int16 Convert.ToInt16( DateTime )
            Assert.Throws<InvalidCastException>(() => { Int64 bTest = Convert.ToInt64(DateTime.Now); });
        }
    }

    /// <summary>
    /// Helper class to test that the IFormatProvider is being called.
    /// </summary>
    internal class TestFormatProvider : IFormatProvider
    {
        public object GetFormat(Type formatType)
        {
            return this;
        }
    }
}
