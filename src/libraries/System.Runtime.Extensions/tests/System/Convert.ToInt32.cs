// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;
// [assembly: System.Reflection.Consumption.EnableDynamicProgramming(typeof(System.ArgumentException))]
// [assembly: System.Reflection.Consumption.EnableDynamicProgramming(typeof(System.FormatException))]

namespace Test
{
    public class Co6061ToInt32_all
    {
        [Fact]
        public static void runTest()
        {
            ///////////////////////////////////////////// Int32 Convert.ToInt32( Boolean )

            //[] ToInt32(Boolean) - Vanilla Cases (true,false)

            // Setup Boolean Test
            {
                Boolean[] testValues = { true, false, };
                Int32[] expectedValues = { 1, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int32 result = Convert.ToInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int32 Convert.ToInt32( Char )

            //[] ToInt32(Char) - Vanilla Cases (Char.MaxValue,Char.MinValue)

            // Setup Char Test

            {
                Char[] testValues = { Char.MaxValue, Char.MinValue, };
                Int32[] expectedValues = { (Int32)Char.MaxValue, (Int32)Char.MinValue, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int32 result = Convert.ToInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int32 Convert.ToInt32( Double )

            //[] ToInt32(Double) - Vanilla Cases (100.0,-100.0,0)

            // Setup Double Test
            {
                Double[] testValues = { 100.0, -100.0, 0, };
                Int32[] expectedValues = { 100, -100, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int32 result = Convert.ToInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int32 Convert.ToInt32( Single )

            //[] ToInt32(Single) - Vanilla Cases (100.0f,-100.0f,0.0f)

            // Setup Single Test
            {
                Single[] testValues = { 100.0f, -100.0f, 0.0f, };
                Int32[] expectedValues = { 100, -100, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int32 result = Convert.ToInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int32 Convert.ToInt32( Int16 )

            //[] ToInt32(Int16) - Vanilla Cases (100,(Int16) (-100 ),0)

            // Setup Int16 Test
            {
                Int16[] testValues = { 100, (Int16)(-100), 0, };
                Int32[] expectedValues = { 100, -100, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int32 result = Convert.ToInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int32 Convert.ToInt32( Int64 )

            //[] ToInt32(Int64) - Vanilla Cases (100,-100,0)

            // Setup Int64 Test
            {
                Int64[] testValues = { 100, -100, 0, };
                Int32[] expectedValues = { 100, -100, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int32 result = Convert.ToInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int32 Convert.ToInt32( Decimal )

            //[] ToInt32(Decimal) - Vanilla Cases (100,-100,0.0)

            // Setup Decimal Test
            {
                Decimal[] testValues = { new Decimal(100), new Decimal(-100), new Decimal(0.0), };
                Int32[] expectedValues = { 100, -100, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int32 result = Convert.ToInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int32 Convert.ToInt32( String )

            //[] ToInt32(String) - Vanilla Cases ("100","-100","0")

            // Setup String Test
            {
                String[] testValues = { "100", "-100", "0", null, };
                Int32[] expectedValues = { 100, -100, 0, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int32 result = Convert.ToInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ///////////////////////////////////////////// [] Int32 Convert.ToInt32( String, IFormatProvider )

            // Setup String Test

            {
                String[] testValues = { "100", "-100", "0", null, };
                Int32[] expectedValues = { 100, -100, 0, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int32 result = Convert.ToInt32(testValues[i], new TestFormatProvider());
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int32 Convert.ToInt32( Byte )

            //[] ToInt32(Byte) - Vanilla Cases (Byte.MaxValue,Byte.MinValue)

            // Setup Byte Test
            {
                Byte[] testValues = { Byte.MaxValue, Byte.MinValue, };
                Int32[] expectedValues = { 255, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int32 result = Convert.ToInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int32 Convert.ToInt32( String, Int32 )

            //[] ToInt32(String,Int32) - Vanilla Cases {("7FFFFFFF",16),("2147483647",10),("17777777777",8),("1111111111111111111111111111111",2),("80000000",16),("-2147483648",10),("20000000000",8),("10000000000000000000000000000000",2)}

            // Setup String, Int32 Test
            {
                String[] dummy = { null, };
                Assert.Equal(0, Convert.ToInt32(dummy[0], 10));
                Assert.Equal(0, Convert.ToInt32(dummy[0], 2));
                Assert.Equal(0, Convert.ToInt32(dummy[0], 8));
                Assert.Equal(0, Convert.ToInt32(dummy[0], 16));
            }

            {
                String[] testValues = { "7FFFFFFF", "2147483647", "17777777777", "1111111111111111111111111111111", "80000000", "-2147483648", "20000000000", "10000000000000000000000000000000", };
                Int32[] testBases = { 16, 10, 8, 2, 16, 10, 8, 2, };
                Int32[] expectedValues = { Int32.MaxValue, Int32.MaxValue, Int32.MaxValue, Int32.MaxValue, Int32.MinValue, Int32.MinValue, Int32.MinValue, Int32.MinValue, };
                // Vanila Test Cases

                for (int i = 0; i < testValues.Length; i++)
                {
                    Int32 result = Convert.ToInt32(testValues[i], testBases[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int32 Convert.ToInt32( SByte )

            //[] ToInt32(SByte) - Vanilla Cases (100,-100,0)

            // Setup SByte Test
            {
                SByte[] testValues = { 100, -100, 0, };
                Int32[] expectedValues = { 100, -100, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int32 result = Convert.ToInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int32 Convert.ToInt32( UInt16 )

            //[] ToInt32(UInt16) - Vanilla Cases (100,0)

            // Setup UInt16 Test
            {
                UInt16[] testValues = { 100, 0, };
                Int32[] expectedValues = { 100, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int32 result = Convert.ToInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int32 Convert.ToInt32( UInt32 )

            //[] ToInt32(UInt32) - Vanilla Cases (100,0)

            // Setup UInt32 Test
            {
                UInt32[] testValues = { 100, 0, };
                Int32[] expectedValues = { 100, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int32 result = Convert.ToInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int32 Convert.ToInt32( UInt64 )

            //[] ToInt32(UInt64) - Vanilla Cases (100,0)

            // Setup UInt64 Test
            {
                UInt64[] testValues = {
      100,
      0,
  };
                Int32[] expectedValues = {
       100 ,
       0 ,
  };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int32 result = Convert.ToInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int32 Convert.ToInt32( Int32 )

            //[] ToInt32(Int32) - Vanilla cases (Int32.MaxValue,Int32.MinValue, 0)

            // Setup Int32 Test
            {
                Int32[] testValues = { Int32.MaxValue, Int32.MinValue, 0 };
                Int32[] expectedValues = { Int32.MaxValue, Int32.MinValue, 0 };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int32 result = Convert.ToInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            //[] ToInt32( Object ) - obj = null
            {
                Int32 bTest = Convert.ToInt32((Object)null);
                Assert.Equal(0, bTest);
            }

            //[] ToInt32( Object, IFP ) - obj = null
            {
                Int32 bTest = Convert.ToInt32((Object)null, new TestFormatProvider());
                Assert.Equal(0, bTest);
            }
        }

        [Fact]
        public static void runTests_Negative()
        {
            {
                // Exception Test Cases
                //[] ToInt32(Double) - Exception Cases (Double.MaxValue,Double.MinValue)
                Double[] errorValues = { Double.MaxValue, Double.MinValue, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { Int32 result = Convert.ToInt32(errorValues[i]); });
                }
            }
            {
                // Exception Test Cases
                //[] ToInt32(Single) - Exception Cases (Single.MaxValue,Single.MinValue)
                Single[] errorValues = { Single.MaxValue, Single.MinValue, };

                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { Int32 result = Convert.ToInt32(errorValues[i]); });
                }
            }
            {
                // Exception Test Cases
                //[] ToInt32(Int64) - Exception Cases (Int64.MaxValue,Int64.MinValue)

                Int64[] errorValues = { Int64.MaxValue, Int64.MinValue, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { Int32 result = Convert.ToInt32(errorValues[i]); });
                }
            }
            {
                // Exception Test Cases
                //[] ToInt32(Decimal) - Exception Cases (Decimal.MaxValue,Decimal.MinValue)
                Decimal[] errorValues = { Decimal.MaxValue, Decimal.MinValue, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { Int32 result = Convert.ToInt32(errorValues[i]); });
                }
            }

            // Exception Test Cases
            //[] ToInt32(String) - Exception Cases (Int64.MaxValue.ToString(),Int64.MinValue.ToString(),null,"abba")
            Assert.Throws<OverflowException>(() => { Int32 result = Convert.ToInt32(Int64.MaxValue.ToString()); });
            Assert.Throws<OverflowException>(() => { Int32 result = Convert.ToInt32(Int64.MaxValue.ToString()); });
            Assert.Throws<FormatException>(() => { Int32 result = Convert.ToInt32("abba"); });

            // Exception Test Cases
            //[] ToInt32(String) - Exception Cases (Int64.MaxValue.ToString(),Int64.MinValue.ToString(),null,"abba")

            // Exception Test Cases
            //[] ToInt32(String) - Exception Cases (Int64.MaxValue.ToString(),Int64.MinValue.ToString(),null,"abba")
            Assert.Throws<OverflowException>(() => { Int32 result = Convert.ToInt32(Int64.MaxValue.ToString(), new TestFormatProvider()); });
            Assert.Throws<OverflowException>(() => { Int32 result = Convert.ToInt32(Int64.MaxValue.ToString(), new TestFormatProvider()); });
            Assert.Throws<FormatException>(() => { Int32 result = Convert.ToInt32("abba", new TestFormatProvider()); });

            // Exception Test Cases
            //[] ToInt32(String,Int32) - Exception Cases {(null,2),("12",3),("11",5),("abba",8),("ffffffffffffffffffff",16)}
            {
                String[] expectedExceptions = { "System.FormatException", "System.ArgumentException", "System.ArgumentException", "System.ArgumentException", "System.FormatException", "System.ArgumentException", "System.OverflowException", "System.OverflowException", "System.OverflowException", "System.OverflowException", "System.OverflowException", };
                String[] errorValues = { "12", "10", "11", "abba", "ffffffffffffffffffff", "-ab", "2147483648", "-2147483649", "111111111111111111111111111111111", "1FFFFffff", "777777777777", };
                Int32[] errorBases = { 2, -1, 3, 0, 8, 16, 10, 10, 2, 16, 8, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    try
                    {
                        Int32 result = Convert.ToInt32(errorValues[i], errorBases[i]);
                        Assert.True(false, "Exception expected: " + expectedExceptions[i]);
                    }
                    catch (Exception e)
                    {
                        Assert.True(e.GetType().FullName.Equals(expectedExceptions[i]), " Wrong Exception Thrown. Expected" + expectedExceptions[i] + ", Actual: " + e.GetType().FullName);
                    }
                }
            }

            // Exception Test Cases
            //[] ToInt32(UInt32) - Exception Cases (UInt32.MaxValue)
            Assert.Throws<OverflowException>(() => { Int32 result = Convert.ToInt32(UInt32.MaxValue); });

            // Exception Test Cases
            //[] ToInt32(UInt64) - Exception Cases (UInt64.MaxValue)
            Assert.Throws<OverflowException>(() => { Int32 result = Convert.ToInt32(UInt64.MaxValue); });

            ///////////////////////////////////////////// Int32 Convert.ToInt32( Object )
            //[] ToInt32( Object ) - Exception Case (Object that does not implement IConvertible) 
            Assert.Throws<InvalidCastException>(() => { Int32 bTest = Convert.ToInt32(new Object()); });

            ///////////////////////////////////////////// []Int32 Convert.ToInt32( Object, IFOrmatProvider )
            Assert.Throws<InvalidCastException>(() => { Int32 bTest = Convert.ToInt32(new Object(), new TestFormatProvider()); });

            ///////////////////////////////////////////// []Int32 Convert.ToInt32( DateTime )
            Assert.Throws<InvalidCastException>(() => { Int32 bTest = Convert.ToInt32(DateTime.Now); });
        }
    }
}
