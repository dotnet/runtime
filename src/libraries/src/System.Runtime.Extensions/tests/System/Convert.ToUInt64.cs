// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;
// [assembly: System.Reflection.Consumption.EnableDynamicProgramming(typeof(System.ArgumentException))]
// [assembly: System.Reflection.Consumption.EnableDynamicProgramming(typeof(System.FormatException))]

namespace Test
{
    public class Co6069ToUInt64_all
    {
        [Fact]
        public static void runTest()
        {
            ///////////////////////////////////////////// UInt64 Convert.ToUInt64( Boolean )

            //[] ToUInt64(Boolean) - Vanilla Cases (true,false)

            // Setup Boolean Test
            {
                Boolean[] testValues = { true, false, };
                UInt64[] expectedValues = { ((UInt64)1), ((UInt64)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt64 result = Convert.ToUInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt64 Convert.ToUInt64( Double )

            //[] ToUInt64(Double) - Vanilla Cases (1000.0,0.0)

            // Setup Double Test
            {
                Double[] testValues = { 1000.0, 0.0, };
                UInt64[] expectedValues = { ((UInt64)1000), ((UInt64)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt64 result = Convert.ToUInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt64 Convert.ToUInt64( Single )

            //[] ToUInt64(Single) - Vanilla Cases (((Single) 1000.0f ),((Single) 0.0f ))

            // Setup Single Test

            {
                Single[] testValues = { ((Single)1000.0f), ((Single)0.0f), };
                UInt64[] expectedValues = { ((UInt64)1000), ((UInt64)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt64 result = Convert.ToUInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt64 Convert.ToUInt64( Int32 )

            //[] ToUInt64(Int32) - Vanilla Cases (((Int32) 1000 ),((Int32) 0 ),Int32.MaxValue)

            // Setup Int32 Test
            {
                Int32[] testValues = { ((Int32)1000), ((Int32)0), Int32.MaxValue, };
                UInt64[] expectedValues = { ((UInt64)1000), ((UInt64)0), ((UInt64)Int32.MaxValue), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt64 result = Convert.ToUInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt64 Convert.ToUInt64( Int64 )

            //[] ToUInt64(Int64) - Vanilla Cases (((Int64) 1000 ),((Int64) 0 ),Int64.MaxValue)

            // Setup Int64 Test

            {
                Int64[] testValues = { ((Int64)1000), ((Int64)0), Int64.MaxValue, };
                UInt64[] expectedValues = { ((UInt64)1000), ((UInt64)0), (UInt64)Int64.MaxValue };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt64 result = Convert.ToUInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt64 Convert.ToUInt64( Int16 )

            //[] ToUInt64(Int16) - Vanilla Cases (((Int16) 1000 ),((Int16) 0 ),Int16.MaxValue)

            // Setup Int16 Test
            {
                Int16[] testValues = { ((Int16)1000), ((Int16)0), Int16.MaxValue, };
                UInt64[] expectedValues = { ((UInt64)1000), ((UInt64)0), ((UInt64)(UInt64)Int16.MaxValue), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt64 result = Convert.ToUInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt64 Convert.ToUInt64( Decimal )

            //[] ToUInt64(Decimal) - Vanilla Cases (Decimal( 1000 ),Decimal( 0 ))

            // Setup Decimal Test
            {
                Decimal[] testValues = { new Decimal(1000), new Decimal(0), };
                UInt64[] expectedValues = { ((UInt64)1000), ((UInt64)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt64 result = Convert.ToUInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt64 Convert.ToUInt64( String )

            //[] ToUInt64(String) - Vanilla Cases ("1000","0",UInt16.MaxValue.ToString(),UInt32.MaxValue.ToString(),UInt64.MaxValue.ToString())

            // Setup String Test
            {
                String[] testValues = { "1000", "0", UInt16.MaxValue.ToString(), UInt32.MaxValue.ToString(), UInt64.MaxValue.ToString(), "9223372036854775807"  /*Int64.MaxValue*/, "9223372036854775808", "9223372036854775809", null, };
                UInt64[] expectedValues = { ((UInt64)1000), ((UInt64)0), Convert.ToUInt64(UInt16.MaxValue), Convert.ToUInt64(UInt32.MaxValue), UInt64.MaxValue, (UInt64)Int64.MaxValue, (UInt64)Int64.MaxValue + 1, (UInt64)Int64.MaxValue + 2, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt64 result = Convert.ToUInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ///////////////////////////////////////////// []UInt64 Convert.ToUInt64( String, IFormatProvider )

            // Setup String Test

            {
                String[] testValues = { "1000", "0", UInt16.MaxValue.ToString(), UInt32.MaxValue.ToString(), UInt64.MaxValue.ToString(), null, };
                UInt64[] expectedValues = { ((UInt64)1000), ((UInt64)0), Convert.ToUInt64(UInt16.MaxValue), Convert.ToUInt64(UInt32.MaxValue), UInt64.MaxValue, ((UInt64)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt64 result = Convert.ToUInt64(testValues[i], new TestFormatProvider());
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt64 Convert.ToUInt64( Byte )

            //[] ToUInt64(Byte) - Vanilla Cases (Byte.MaxValue,Byte.MinValue)

            // Setup Byte Test

            {
                Byte[] testValues = { Byte.MaxValue, Byte.MinValue, };
                UInt64[] expectedValues = { ((UInt32)255), ((UInt32)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt64 result = Convert.ToUInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ///////////////////////////////////////////// UInt64 Convert.ToUInt64( UInt32 )

            //[] ToUInt64(UInt32) - Vanilla Cases (UInt32.MinValue,UInt32.MaxValue)

            // Setup UInt32 Test
            {
                UInt32[] testValues = { UInt32.MinValue, UInt32.MaxValue, };
                UInt64[] expectedValues = { UInt32.MinValue, UInt32.MaxValue, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt64 result = Convert.ToUInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt64 Convert.ToUInt64( String, Int32 )

            //[] ToUInt64(String,Int32) - Vanilla Cases {("ffffffffffffffff",16),("18446744073709551615",10),("1777777777777777777777",8),("1111111111111111111111111111111111111111111111111111111111111111",2),("0",16),("0",10),("0",8),("0",2)}

            // Setup String, Int32 Test

            {
                String[] dummy = { null, };
                Assert.Equal((UInt64)0, Convert.ToUInt64(dummy[0], 10));
                Assert.Equal((UInt64)0, Convert.ToUInt64(dummy[0], 2));
                Assert.Equal((UInt64)0, Convert.ToUInt64(dummy[0], 8));
                Assert.Equal((UInt64)0, Convert.ToUInt64(dummy[0], 16));
            }
            {
                String[] testValues = { "ffffffffffffffff", "18446744073709551615", "1777777777777777777777", "1111111111111111111111111111111111111111111111111111111111111111", "0", "0", "0", "0", "9223372036854775807", "9223372036854775808" /*VSWhidbey #526568*/, "9223372036854775809", "9223372036854775810", "9223372036854775811", };
                Int32[] testBases = { 16, 10, 8, 2, 16, 10, 8, 2, 10, 10, 10, 10, 10, };
                UInt64[] expectedValues = { UInt64.MaxValue, UInt64.MaxValue, UInt64.MaxValue, UInt64.MaxValue, UInt64.MinValue, UInt64.MinValue, UInt64.MinValue, UInt64.MinValue, (UInt64)Int64.MaxValue, (UInt64)Int64.MaxValue + 1 /*VSWhidbey #526568*/, (UInt64)Int64.MaxValue + 2, (UInt64)Int64.MaxValue + 3, (UInt64)Int64.MaxValue + 4, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt64 result = Convert.ToUInt64(testValues[i], testBases[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt64 Convert.ToUInt64( SByte )

            //[] ToUInt64(SByte) - Vanilla Cases (100,0)

            // Setup SByte Test
            {
                SByte[] testValues = { 100, 0, };
                UInt64[] expectedValues = { ((UInt64)100), ((UInt64)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt64 result = Convert.ToUInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt64 Convert.ToUInt64( UInt16 )

            //[] ToUInt64(UInt16) - Vanilla Cases ((UInt16)100,(UInt16)0)

            // Setup UInt16 Test

            {
                UInt16[] testValues = { (UInt16)100, (UInt16)0, };
                UInt64[] expectedValues = { ((UInt64)100), ((UInt64)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt64 result = Convert.ToUInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt64 Convert.ToUInt64( UInt64 )

            //[] ToUInt64(UInt64) - Vanilla cases (UInt64.MaxValue,UInt64.MinValue)

            // Setup UInt64 Test

            {
                UInt64[] testValues = { UInt64.MaxValue, UInt64.MinValue };
                UInt64[] expectedValues = { UInt64.MaxValue, UInt64.MinValue };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt64 result = Convert.ToUInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt64 Convert.ToUInt64( Char )

            //[] ToUInt64(Char) - Vanilla Cases (Char.MaxValue,Char.MinValue, 'b')

            // Setup Char Test

            {
                Char[] testValues = { Char.MaxValue, Char.MinValue, 'b' };
                UInt64[] expectedValues = { (UInt64)Char.MaxValue, (UInt64)Char.MinValue, 98 };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt64 result = Convert.ToUInt64(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            //[] ToUInt64( Object ) - obj = null
            {
                UInt64 bTest = Convert.ToUInt64((Object)null);
                Assert.Equal((UInt64)0, bTest);
            }

            //[] ToUInt64( Object, IFP ) - obj = null

            {
                UInt64 bTest = Convert.ToUInt64((Object)null, new TestFormatProvider());
                Assert.Equal((UInt64)0, bTest);
            }
        }

        [Fact]
        public static void runTests_Negative()
        {
            {
                // Exception Test Cases
                //[] ToUInt64(Double) - Exception Cases (Double.MaxValue,-100.0)
                Double[] errorValues = { Double.MaxValue, -100.0, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { UInt64 result = Convert.ToUInt64(errorValues[i]); });
                }
            }
            {
                // Exception Test Cases
                //[] ToUInt64(Single) - Exception Cases (Single.MaxValue,-100.0f)
                Single[] errorValues = { Single.MaxValue, -100.0f, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { UInt64 result = Convert.ToUInt64(errorValues[i]); });
                }
            }
            {
                // Exception Test Cases
                //[] ToUInt64(Int32) - Exception Cases (((Int32) Int32.MinValue ))
                Int32[] errorValues = { ((Int32)Int32.MinValue), };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { UInt64 result = Convert.ToUInt64(errorValues[i]); });
                }
            }
            {
                // Exception Test Cases
                //[] ToUInt64(Int16) - Exception Cases (Int16.MinValue)
                Int16[] errorValues = { Int16.MinValue, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { UInt64 result = Convert.ToUInt64(errorValues[i]); });
                }
            }
            {
                // Exception Test Cases
                //[] ToUInt64(Decimal) - Exception Cases (Decimal.MaxValue,Decimal.MinValue)
                Decimal[] errorValues = { Decimal.MaxValue, Decimal.MinValue, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { UInt64 result = Convert.ToUInt64(errorValues[i]); });
                }
            }

            // Exception Test Cases
            //[] ToUInt64(String) - Exception Cases ("-1",Decimal.MaxValue.ToString(),null,"abba")
            Assert.Throws<OverflowException>(() => { UInt64 result = Convert.ToUInt64("-1"); });
            Assert.Throws<OverflowException>(() => { UInt64 result = Convert.ToUInt64(Decimal.MaxValue.ToString()); });
            Assert.Throws<FormatException>(() => { UInt64 result = Convert.ToUInt64("abba"); });

            // Exception Test Cases
            //[] ToUInt64(String) - Exception Cases ("-1",Decimal.MaxValue.ToString(),null,"abba")
            Assert.Throws<OverflowException>(() => { UInt64 result = Convert.ToUInt64("-1", new TestFormatProvider()); });
            Assert.Throws<OverflowException>(() => { UInt64 result = Convert.ToUInt64(Decimal.MaxValue.ToString(), new TestFormatProvider()); });
            Assert.Throws<FormatException>(() => { UInt64 result = Convert.ToUInt64("abba", new TestFormatProvider()); });

            {
                // Exception Test Cases
                //[] ToUInt64(String,Int32) - Exception Cases {(null,2),("12",3),("11",5),("abba",8),("ffffffffffffffffffff",16)}
                String[] expectedExceptions = { "System.FormatException", "System.ArgumentException", "System.ArgumentException", "System.ArgumentException", "System.FormatException", "System.ArgumentException", "System.OverflowException", "System.OverflowException", "System.OverflowException", "System.OverflowException", "System.OverflowException", "System.OverflowException", "System.OverflowException", "System.OverflowException", "System.OverflowException", };
                String[] errorValues = { "12", "10", "11", "abba", "ffffffffffffffffffff", "-ab", "18446744073709551616" /*VSWhidbey #143726 */, "18446744073709551617"/*VSWhidbey #143726 */, "18446744073709551618"/*VSWhidbey #143726 */, "18446744073709551619"/*VSWhidbey #143726 */, "18446744073709551620"/*VSWhidbey #143726 */, "-4294967297", "11111111111111111111111111111111111111111111111111111111111111111", "1FFFFffffFFFFffff", "7777777777777777777777777", };
                Int32[] errorBases = { 2, -1, 3, 0, 8, 16, 10, 10, 10, 10, 10, 10, 2, 16, 8, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    try
                    {
                        UInt64 result = Convert.ToUInt64(errorValues[i], errorBases[i]);
                        Assert.True(false, "Exception expected: " + expectedExceptions[i]);
                    }
                    catch (Exception e)
                    {
                        Assert.True(e.GetType().FullName.Equals(expectedExceptions[i]), " Wrong Exception Thrown. Expected" + expectedExceptions[i] + ", Actual: " + e.GetType().FullName);
                    }
                }
                // Exception Test Cases
                //[] ToUInt64(SByte) - Exception Cases (SByte.MinValue)
            }
            {
                SByte[] errorValues = { SByte.MinValue };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { UInt64 result = Convert.ToUInt64(errorValues[i]); });
                }
            }
            ///////////////////////////////////////////// UInt64 Convert.ToUInt64( Object )
            //[] ToUInt64( Object ) - Exception Case (Object that does not implement IConvertible) 
            Assert.Throws<InvalidCastException>(() => { UInt64 bTest = Convert.ToUInt64(new Object()); });

            ///////////////////////////////////////////// []UInt64 Convert.ToUInt64( Object, IFormatProvider )
            Assert.Throws<InvalidCastException>(() => { UInt64 bTest = Convert.ToUInt64(new Object(), new TestFormatProvider()); });

            ///////////////////////////////////////////// []UInt64 Convert.ToUInt64( DateTime )
            Assert.Throws<InvalidCastException>(() => { UInt64 bTest = Convert.ToUInt64(DateTime.Now); });
        }
    }
}
