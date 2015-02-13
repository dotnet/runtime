// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;
// [assembly: System.Reflection.Consumption.EnableDynamicProgramming(typeof(System.FormatException))]

namespace Test
{
    public class Co6068ToUInt32_all
    {
        [Fact]
        public static void runTest()
        {
            ///////////////////////////////////////////// UInt16 Convert.ToUInt32( Boolean )

            //[] ToUInt32(Boolean) - Vanilla Cases (true,false)

            // Setup Boolean Test
            {
                Boolean[] testValues = { true, false, };
                UInt32[] expectedValues = { ((UInt32)1), ((UInt32)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt32 result = Convert.ToUInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt32 Convert.ToUInt32( Char )

            //[] ToUInt32(Char) - Vanilla Cases (Char.MinValue,Char.MaxValue)

            // Setup Char Test

            {
                Char[] testValues = { Char.MinValue, Char.MaxValue, };
                UInt32[] expectedValues = { ((UInt32)Char.MinValue), ((UInt32)Char.MaxValue), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt32 result = Convert.ToUInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt16 Convert.ToUInt32( Double )

            //[] ToUInt32(Double) - Vanilla Cases (1000.0,0.0)

            // Setup Double Test

            {
                Double[] testValues = { 1000.0, 0.0, -0.5, 4294967295.49999, 472.2, 472.6, 472.5, 471.5, };
                UInt32[] expectedValues = { ((UInt32)1000), ((UInt32)0), ((UInt32)0), ((UInt32)4294967295), ((UInt32)472), ((UInt32)473), ((UInt32)472), ((UInt32)472), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt32 result = Convert.ToUInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt16 Convert.ToUInt32( Single )

            //[] ToUInt32(Single) - Vanilla Cases (((Single) 1000.0f ),((Single) 0.0f ))

            // Setup Single Test
            {
                Single[] testValues = { ((Single)1000.0f), ((Single)0.0f), };
                UInt32[] expectedValues = { ((UInt32)1000), ((UInt32)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt32 result = Convert.ToUInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt16 Convert.ToUInt32( Int32 )

            //[] ToUInt32(Int32) - Vanilla Cases (((Int32) 1000 ),((Int32) 0 ),Int32.MaxValue)

            // Setup Int32 Test

            {
                Int32[] testValues = { ((Int32)1000), ((Int32)0), Int32.MaxValue, };
                UInt32[] expectedValues = { ((UInt32)1000), ((UInt32)0), ((UInt32)Int32.MaxValue), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt32 result = Convert.ToUInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt16 Convert.ToUInt32( Int64 )

            //[] ToUInt32(Int64) - Vanilla Cases (((Int64) 1000 ),((Int64) 0 ))

            // Setup Int64 Test

            {
                Int64[] testValues = { ((Int64)1000), ((Int64)0), };
                UInt32[] expectedValues = { ((UInt32)1000), ((UInt32)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt32 result = Convert.ToUInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt16 Convert.ToUInt32( Int16 )

            //[] ToUInt32(Int16) - Vanilla Cases (((Int16) 1000 ),((Int16) 0 ),Int16.MaxValue)

            // Setup Int16 Test

            {
                Int16[] testValues = { ((Int16)1000), ((Int16)0), Int16.MaxValue, };
                UInt32[] expectedValues = { ((UInt32)1000), ((UInt32)0), ((UInt32)(UInt32)Int16.MaxValue), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt32 result = Convert.ToUInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt16 Convert.ToUInt32( Decimal )

            //[] ToUInt32(Decimal) - Vanilla Cases (Decimal( 1000 ),Decimal( 0 ))

            // Setup Decimal Test

            {
                Decimal[] testValues = { new Decimal(1000), new Decimal(0), };
                UInt32[] expectedValues = { ((UInt32)1000), ((UInt32)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt32 result = Convert.ToUInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt16 Convert.ToUInt32( String )

            //[] ToUInt32(String) - Vanilla Cases ("1000","0",UInt16.MaxValue.ToString(),UInt32.MaxValue.ToString())

            // Setup String Test

            {
                String[] testValues = { "1000", "0", UInt16.MaxValue.ToString(), UInt32.MaxValue.ToString(), "2147483647" /*int.MaxValue*/, "2147483648", "2147483649", null, };
                UInt32[] expectedValues = { ((UInt32)1000), ((UInt32)0), UInt16.MaxValue, UInt32.MaxValue, Int32.MaxValue, (UInt32)Int32.MaxValue + 1, (UInt32)Int32.MaxValue + 2, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt32 result = Convert.ToUInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ///////////////////////////////////////////// []UInt16 Convert.ToUInt32( String, IFormatProvider )

            // Setup String Test
            {
                String[] testValues = { "1000", "0", UInt16.MaxValue.ToString(), UInt32.MaxValue.ToString(), null, };
                UInt32[] expectedValues = { ((UInt32)1000), ((UInt32)0), UInt16.MaxValue, UInt32.MaxValue, ((UInt32)0), };

                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt32 result = Convert.ToUInt32(testValues[i], new TestFormatProvider());
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt16 Convert.ToUInt32( Byte )

            //[] ToUInt32(Byte) - Vanilla Cases (Byte.MaxValue,Byte.MinValue)

            // Setup Byte Test
            {
                Byte[] testValues = { Byte.MaxValue, Byte.MinValue, };
                UInt32[] expectedValues = { ((UInt32)255), ((UInt32)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt32 result = Convert.ToUInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt32 Convert.ToUInt32( String, Int32 )

            //[] ToUInt32(String,Int32) - Vanilla Cases {("ffffffff",16),("4294967295",10),("37777777777",8),("11111111111111111111111111111111",2),("0",16),("0",10),("0",8),("0",2)}

            // Setup String, Int32 Test

            {
                String[] dummy = { null, };
                Assert.Equal((UInt32)0, Convert.ToUInt32(dummy[0], 10));
                Assert.Equal((UInt32)0, Convert.ToUInt32(dummy[0], 2));
                Assert.Equal((UInt32)0, Convert.ToUInt32(dummy[0], 8));
                Assert.Equal((UInt32)0, Convert.ToUInt32(dummy[0], 16));
            }

            {
                String[] testValues = { "ffffffff", "4294967295", "37777777777", "11111111111111111111111111111111", "0", "0", "0", "0", "2147483647", "2147483648" /*VSWhidbey #526568*/, "2147483649", };
                Int32[] testBases = { 16, 10, 8, 2, 16, 10, 8, 2, 10, 10, 10, };
                UInt32[] expectedValues = { UInt32.MaxValue, UInt32.MaxValue, UInt32.MaxValue, UInt32.MaxValue, UInt32.MinValue, UInt32.MinValue, UInt32.MinValue, UInt32.MinValue, (UInt32)Int32.MaxValue, (UInt32)Int32.MaxValue + 1 /*VSWhidbey #526568 */, (UInt32)Int32.MaxValue + 2, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt32 result = Convert.ToUInt32(testValues[i], testBases[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt32 Convert.ToUInt32( SByte )

            //[] ToUInt32(SByte) - Vanilla Cases (100,0)

            // Setup SByte Test

            {
                SByte[] testValues = { 100, 0, };
                UInt32[] expectedValues = { ((UInt32)100), ((UInt32)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt32 result = Convert.ToUInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt32 Convert.ToUInt32( UInt16 )

            //[] ToUInt32(UInt16) - Vanilla Cases ((UInt16)100,(UInt16)0)

            // Setup UInt16 Test

            {
                UInt16[] testValues = { (UInt16)100, (UInt16)0, };
                UInt32[] expectedValues = { ((UInt32)100), ((UInt32)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt32 result = Convert.ToUInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt32 Convert.ToUInt32( UInt64 )

            //[] ToUInt32(UInt64) - Vanilla Cases (100,0)

            // Setup UInt64 Test

            {
                UInt64[] testValues = { 100, 0, };
                UInt32[] expectedValues = { ((UInt32)100), ((UInt32)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt32 result = Convert.ToUInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt32 Convert.ToUInt32( UInt32 )

            //[] ToUInt32(UInt32) - Vanilla cases (UInt32.MaxValue,UInt32.MinValue)

            // Setup UInt32 Test

            {
                UInt32[] testValues = { UInt32.MaxValue, UInt32.MinValue };
                UInt32[] expectedValues = { UInt32.MaxValue, UInt32.MinValue };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt32 result = Convert.ToUInt32(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            //[] ToUInt32( Object ) - obj = null
            {
                UInt32 bTest = Convert.ToUInt32((Object)null);
                Assert.Equal((UInt32)0, bTest);
            }

            //[] ToUInt32( Object, IFP ) - obj = null
            {
                UInt32 bTest = Convert.ToUInt32((Object)null, new TestFormatProvider());
                Assert.Equal((UInt32)0, bTest);
            }
        }

        [Fact]
        public static void runTest_negative()
        {
            {
                // Exception Test Cases
                //[] ToUInt32(Double) - Exception Cases (Double.MaxValue,-100.0)
                Double[] errorValues = { Double.MaxValue, -0.500000000001, -100.0, 4294967296, 4294967295.5, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { UInt32 result = Convert.ToUInt32(errorValues[i]); });
                }
            }
            {
                // Exception Test Cases
                //[] ToUInt32(Single) - Exception Cases (Single.MaxValue,-100.0f)
                Single[] errorValues = { Single.MaxValue, -100.0f, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { UInt32 result = Convert.ToUInt32(errorValues[i]); });
                }
            }
            {
                // Exception Test Cases
                //[] ToUInt32(Int32) - Exception Cases (((Int32) Int32.MinValue ))
                Int32[] errorValues = { ((Int32)Int32.MinValue), };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { UInt32 result = Convert.ToUInt32(errorValues[i]); });
                }
            }
            {
                // Exception Test Cases
                //[] ToUInt32(Int64) - Exception Cases (((Int64) Int64.MaxValue ),((Int64) Int64.MinValue ))
                Int64[] errorValues = { ((Int64)Int64.MaxValue), ((Int64)Int64.MinValue), };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { UInt32 result = Convert.ToUInt32(errorValues[i]); });
                }
            }
            {
                // Exception Test Cases
                //[] ToUInt32(Int16) - Exception Cases (((Int16) Int16.MinValue ))

                Int16[] errorValues = { ((Int16)Int16.MinValue), };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { UInt32 result = Convert.ToUInt32(errorValues[i]); });
                }
            }
            {
                // Exception Test Cases
                //[] ToUInt32(Decimal) - Exception Cases (Decimal.MaxValue,Decimal.MinValue)
                Decimal[] errorValues = { Decimal.MaxValue, Decimal.MinValue, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { UInt32 result = Convert.ToUInt32(errorValues[i]); });
                }
            }

            // Exception Test Cases
            //[] ToUInt32(String) - Exception Cases ("-1",Decimal.MaxValue.ToString(),null,"abba")

            Assert.Throws<OverflowException>(() => { UInt32 result = Convert.ToUInt32("-1"); });
            Assert.Throws<OverflowException>(() => { UInt32 result = Convert.ToUInt32(Decimal.MaxValue.ToString()); });
            Assert.Throws<FormatException>(() => { UInt32 result = Convert.ToUInt32("abba"); });

            // Exception Test Cases
            //[] ToUInt32(String) - Exception Cases ("-1",Decimal.MaxValue.ToString(),null,"abba")
            Assert.Throws<OverflowException>(() => { UInt32 result = Convert.ToUInt32("-1", new TestFormatProvider()); });
            Assert.Throws<OverflowException>(() => { UInt32 result = Convert.ToUInt32(Decimal.MaxValue.ToString(), new TestFormatProvider()); });
            Assert.Throws<FormatException>(() => { UInt32 result = Convert.ToUInt32("abba", new TestFormatProvider()); });

            {
                // Exception Test Cases
                //[] ToUInt32(String,Int32) - Exception Cases {(null,2),("12",3),("11",5),("abba",8),("ffffffffffffffffffff",16)}
                String[] expectedExceptions = { "System.FormatException", "System.ArgumentException", "System.ArgumentException", "System.ArgumentException", "System.FormatException", "System.ArgumentException", "System.OverflowException", "System.OverflowException", "System.OverflowException", "System.OverflowException", "System.OverflowException", "System.OverflowException", "System.OverflowException", "System.OverflowException", "System.OverflowException", };
                String[] errorValues = { "12", "10", "11", "abba", "ffffffffffffffffffff", "-ab", "4294967296" /*VSWhidbey #143726*/, "4294967297"/*VSWhidbey #143726*/, "4294967298"/*VSWhidbey #143726*/, "4294967299"/*VSWhidbey #143726*/, "4294967300"/*VSWhidbey #143726*/, "-4294967297"/*VSWhidbey #143726*/, "111111111111111111111111111111111", "1FFFFffff", "777777777777", };
                Int32[] errorBases = { 2, -1, 3, 0, 8, 16, 10, 10, 10, 10, 10, 10, 2, 16, 8, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    try
                    {
                        UInt32 result = Convert.ToUInt32(errorValues[i], errorBases[i]);
                        Assert.True(false, "Exception expected: " + expectedExceptions[i]);
                    }
                    catch (Exception e)
                    {
                        Assert.True(e.GetType().FullName.Equals(expectedExceptions[i]), " Wrong Exception Thrown. Expected" + expectedExceptions[i] + ", Actual: " + e.GetType().FullName);
                    }
                }
            }
            {
                // Exception Test Cases
                //[] ToUInt32(SByte) - Exception Cases (SByte.MinValue)

                SByte[] errorValues = { SByte.MinValue };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { UInt32 result = Convert.ToUInt32(errorValues[i]); });
                }
            }
            {
                // Exception Test Cases
                //[] ToUInt32(UInt64) - Exception Cases (UInt64.MaxValue)
                UInt64[] errorValues = { UInt64.MaxValue, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { UInt32 result = Convert.ToUInt32(errorValues[i]); });
                }
            }
            ///////////////////////////////////////////// UInt32 Convert.ToUInt32( Object )
            //[] ToUInt32( Object ) - Exception Case (Object that does not implement IConvertible) 
            Assert.Throws<InvalidCastException>(() => { UInt32 bTest = Convert.ToUInt32(new Object()); });

            ///////////////////////////////////////////// []UInt32 Convert.ToUInt32( Object, IFormatProvider )
            Assert.Throws<InvalidCastException>(() => { UInt32 bTest = Convert.ToUInt32(new Object(), new TestFormatProvider()); });
            ///////////////////////////////////////////// []UInt32 Convert.ToUInt32( DateTime )
            //[] ToUInt32( Object ) - Exception Case (Object that does not implement IConvertible) 

            Assert.Throws<InvalidCastException>(() => { UInt32 bTest = Convert.ToUInt32(DateTime.Now); });
        }
    }
}
