// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;
// [assembly: System.Reflection.Consumption.EnableDynamicProgramming(typeof(System.ArgumentException))]
// [assembly: System.Reflection.Consumption.EnableDynamicProgramming(typeof(System.FormatException))]

namespace Test
{
    public class Co6067ToUInt16_all
    {
        [Fact]
        public static void runTest()
        {
            ///////////////////////////////////////////// UInt16 Convert.ToUInt16( Boolean )

            //[] ToUInt16(Boolean) - Vanilla Cases (true,false)

            // Setup Boolean Test
            {
                Boolean[] testValues = { true, false, };
                UInt16[] expectedValues = { 1, 0, };
                // Vanila Test Casse
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt16 result = Convert.ToUInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt16 Convert.ToUInt16( Double )

            //[] ToUInt16(Double) - Vanilla Cases (1000.0,0.0)

            // Setup Double Test
            {
                Double[] testValues = { 1000.0, 0.0, };
                UInt16[] expectedValues = { 1000, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt16 result = Convert.ToUInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt16 Convert.ToUInt16( Single )

            //[] ToUInt16(Single) - Vanilla Cases (1000.0f,0.0f)

            // Setup Single Test

            {
                Single[] testValues = { 1000.0f, 0.0f, };
                UInt16[] expectedValues = { 1000, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt16 result = Convert.ToUInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt16 Convert.ToUInt16( Int32 )

            //[] ToUInt16(Int32) - Vanilla Cases (1000,0)

            // Setup Int32 Test

            {
                Int32[] testValues = { 1000, 0, };
                UInt16[] expectedValues = { 1000, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt16 result = Convert.ToUInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt16 Convert.ToUInt16( Int64 )

            //[] ToUInt16(Int64) - Vanilla Cases (1000,0)

            // Setup Int64 Test

            {
                Int64[] testValues = { 1000, 0, };
                UInt16[] expectedValues = { 1000, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt16 result = Convert.ToUInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt16 Convert.ToUInt16( Int16 )

            //[] ToUInt16(Int16) - Vanilla Cases (1000,0)

            // Setup Int16 Test

            {
                Int16[] testValues = { 1000, 0, };
                UInt16[] expectedValues = { 1000, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt16 result = Convert.ToUInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt16 Convert.ToUInt16( Decimal )

            //[] ToUInt16(Decimal) - Vanilla Cases (Decimal( 1000 ),Decimal( 0 ))

            // Setup Decimal Test

            {
                Decimal[] testValues = { new Decimal(1000), new Decimal(0), };
                UInt16[] expectedValues = { 1000, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt16 result = Convert.ToUInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt16 Convert.ToUInt16( String )

            //[] ToUInt16(String) - Vanilla Cases ("1000","0",UInt16.MaxValue.ToString())

            // Setup String Test

            {
                String[] testValues = { "1000", "0", UInt16.MaxValue.ToString(), null, };
                UInt16[] expectedValues = { 1000, 0, UInt16.MaxValue, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt16 result = Convert.ToUInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ///////////////////////////////////////////// []UInt16 Convert.ToUInt16( String, IFormatProvider )

            // Setup String Test
            {
                String[] testValues = { "1000", "0", UInt16.MaxValue.ToString(), null, };
                UInt16[] expectedValues = { 1000, 0, UInt16.MaxValue, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt16 result = Convert.ToUInt16(testValues[i], new TestFormatProvider());
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt16 Convert.ToUInt16( Byte )

            //[] ToUInt16(Byte) - Vanilla Cases (Byte.MaxValue,Byte.MinValue)

            // Setup Byte Test
            {
                Byte[] testValues = { Byte.MaxValue, Byte.MinValue, };
                UInt16[] expectedValues = { 255, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt16 result = Convert.ToUInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt16 Convert.ToUInt16( String, Int32 )

            //[] ToUInt16(String,Int32) - Vanilla Cases {("ffff",16),("65535",10),("177777",8),("1111111111111111",2),("0",16),("0",10),("0",8),("0"2)}

            // Setup String, Int32 Test

            {
                String[] dummy = { null, };
                Assert.Equal(0, Convert.ToUInt16(dummy[0], 10));
                Assert.Equal(0, Convert.ToUInt16(dummy[0], 2));
                Assert.Equal(0, Convert.ToUInt16(dummy[0], 8));
                Assert.Equal(0, Convert.ToUInt16(dummy[0], 16));
            }

            {
                String[] testValues = { "ffff", "65535", "177777", "1111111111111111", "0", "0", "0", "0", };
                Int32[] testBases = { 16, 10, 8, 2, 16, 10, 8, 2, };
                UInt16[] expectedValues = { UInt16.MaxValue, UInt16.MaxValue, UInt16.MaxValue, UInt16.MaxValue, UInt16.MinValue, UInt16.MinValue, UInt16.MinValue, UInt16.MinValue, };                // Vanila Test Cases

                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt16 result = Convert.ToUInt16(testValues[i], testBases[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt16 Convert.ToUInt16( SByte )

            //[] ToUInt16(SByte) - Vanilla Cases (100,0)

            // Setup SByte Test

            {
                SByte[] testValues = { 100, 0, };
                UInt16[] expectedValues = { 100, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt16 result = Convert.ToUInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt16 Convert.ToUInt16( UInt32 )

            //[] ToUInt16(UInt32) - Vanilla Cases (100,0)

            // Setup UInt32 Test
            {
                UInt32[] testValues = { 100, 0, };
                UInt16[] expectedValues = { 100, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt16 result = Convert.ToUInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt16 Convert.ToUInt16( UInt64 )

            //[] ToUInt16(UInt64) - Vanilla Cases (100,0)

            // Setup UInt64 Test
            {
                UInt64[] testValues = { 100, 0, };
                UInt16[] expectedValues = { 100, 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt16 result = Convert.ToUInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt16 Convert.ToUInt16( UInt16 )

            //[] ToUInt16(UInt16) - Vanilla cases (UInt16.MaxValue,UInt16.MinValue)

            // Setup UInt16 Test

            {
                UInt16[] testValues = { UInt16.MaxValue, UInt16.MinValue };
                UInt16[] expectedValues = { UInt16.MaxValue, UInt16.MinValue };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt16 result = Convert.ToUInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// UInt16 Convert.ToUInt16( Char )

            //[] ToUInt16(Char) - Vanilla Cases (Char.MaxValue,Char.MinValue, 'b')

            // Setup Char Test

            {
                Char[] testValues = { Char.MaxValue, Char.MinValue, 'b' };
                UInt16[] expectedValues = { (UInt16)Char.MaxValue, (UInt16)Char.MinValue, 98 };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    UInt16 result = Convert.ToUInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            //[] ToUInt16( Object ) - obj = null

            {
                UInt16 bTest = Convert.ToUInt16((Object)null);
                Assert.Equal(0, bTest);
            }
            //[] ToUInt16( Object, IFP ) - obj = null
            {
                UInt16 bTest = Convert.ToUInt16((Object)null, new TestFormatProvider());
                Assert.Equal(0, bTest);
            }
        }

        [Fact]
        public static void runTest_Negative()
        {
            {
                // Exception Test Cases
                //[] ToUInt16(Double) - Exception Cases (Double.MaxValue,-100.0)
                Double[] errorValues = { Double.MaxValue, -100.0, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { UInt16 result = Convert.ToUInt16(errorValues[i]); });
                }
            }
            {
                // Exception Test Cases
                //[] ToUInt16(Single) - Exception Cases (Single.MaxValue,-100.0f)
                Single[] errorValues = { Single.MaxValue, -100.0f, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { UInt16 result = Convert.ToUInt16(errorValues[i]); });
                }
            }
            {
                // Exception Test Cases
                //[] ToUInt16(Int32) - Exception Cases (Int32.MinValue,Int32.MaxValue)
                Int32[] errorValues = { Int32.MinValue, Int32.MaxValue, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { UInt16 result = Convert.ToUInt16(errorValues[i]); });
                }
            }
            {
                // Exception Test Cases
                //[] ToUInt16(Int64) - Exception Cases (Int64.MaxValue,Int64.MinValue)
                Int64[] errorValues = { Int64.MaxValue, Int64.MinValue, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { UInt16 result = Convert.ToUInt16(errorValues[i]); });
                }
            }
            // Exception Test Cases
            //[] ToUInt16(Int16) - Exception Cases (Int16.MinValue)

            Assert.Throws<OverflowException>(() => { UInt16 result = Convert.ToUInt16(Int16.MinValue); });
            {
                // Exception Test Cases
                //[] ToUInt16(Decimal) - Exception Cases (Decimal.MaxValue,Decimal.MinValue)
                Decimal[] errorValues = { Decimal.MaxValue, Decimal.MinValue, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { UInt16 result = Convert.ToUInt16(errorValues[i]); });
                }
            }
            // Exception Test Cases
            //[] ToUInt16(String) - Exception Cases ("-1",Decimal.MaxValue.ToString(),null,"abba")

            Assert.Throws<OverflowException>(() => { UInt16 result = Convert.ToUInt16(Decimal.MaxValue.ToString()); });
            Assert.Throws<OverflowException>(() => { UInt16 result = Convert.ToUInt16("-1"); });
            Assert.Throws<FormatException>(() => { UInt16 result = Convert.ToUInt16("abba"); });

            // Exception Test Cases
            //[] ToUInt16(String) - Exception Cases ("-1",Decimal.MaxValue.ToString(),null,"abba")
            Assert.Throws<OverflowException>(() => { UInt16 result = Convert.ToUInt16(Decimal.MaxValue.ToString(), new TestFormatProvider()); });
            Assert.Throws<OverflowException>(() => { UInt16 result = Convert.ToUInt16("-1", new TestFormatProvider()); });
            Assert.Throws<FormatException>(() => { UInt16 result = Convert.ToUInt16("abba", new TestFormatProvider()); });
            {
                // Exception Test Cases
                //[] ToUInt16(String,Int32) - Exception Cases {(null,2),("12",3),("11",5),("abba",10),("ffffffffffffffffffff",16)}
                String[] expectedExceptions = { "System.FormatException", "System.ArgumentException", "System.ArgumentException", "System.ArgumentException", "System.FormatException", "System.ArgumentException", "System.OverflowException", "System.OverflowException", "System.OverflowException", "System.OverflowException", "System.OverflowException", };
                String[] errorValues = { "12", "10", "11", "abba", "ffffffffffffffffffff", "-ab", "65536", "-1", "11111111111111111", "1FFFF", "777777", };
                Int32[] errorBases = { 2, -1, 3, 0, 8, 16, 10, 10, 2, 16, 8, };

                for (int i = 0; i < errorValues.Length; i++)
                {
                    try
                    {
                        UInt16 result = Convert.ToUInt16(errorValues[i], errorBases[i]);
                        Assert.True(false, "Exception expected: " + expectedExceptions[i]);
                    }
                    catch (Exception e)
                    {
                        Assert.True(e.GetType().FullName.Equals(expectedExceptions[i]), " Wrong Exception Thrown. Expected" + expectedExceptions[i] + ", Actual: " + e.GetType().FullName);
                    }
                }
            }
            // Exception Test Cases
            //[] ToUInt16(SByte) - Exception Cases (SByte.MinValue)
            Assert.Throws<OverflowException>(() => { UInt16 result = Convert.ToUInt16(SByte.MinValue); });

            // Exception Test Cases
            //[] ToUInt16(UInt32) - Exception Cases (UInt32.MaxValue)
            Assert.Throws<OverflowException>(() => { UInt16 result = Convert.ToUInt16(UInt32.MaxValue); });

            // Exception Test Cases
            //[] ToUInt16(UInt64) - Exception Cases (UInt64.MaxValue)
            Assert.Throws<OverflowException>(() => { UInt16 result = Convert.ToUInt16(UInt64.MaxValue); });

            ///////////////////////////////////////////// UInt16 Convert.ToUInt16( Object )
            //[] ToUInt16( Object ) - Exception Case (Object that does not implement IConvertible) 

            Assert.Throws<InvalidCastException>(() => { UInt16 bTest = Convert.ToUInt16(new Object()); });

            ///////////////////////////////////////////// []UInt16 Convert.ToUInt16( Object, IFormatPRovider )
            Assert.Throws<InvalidCastException>(() => { UInt16 bTest = Convert.ToUInt16(new Object(), new TestFormatProvider()); });

            ///////////////////////////////////////////// []UInt16 Convert.ToUInt16( DateTime )
            Assert.Throws<InvalidCastException>(() => { UInt16 bTest = Convert.ToUInt16(DateTime.Now); });
        }
    }
}
