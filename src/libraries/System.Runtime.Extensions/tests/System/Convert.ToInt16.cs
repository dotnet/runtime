// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;
// [assembly: System.Reflection.Consumption.EnableDynamicProgramming(typeof(System.ArgumentException))]
// [assembly: System.Reflection.Consumption.EnableDynamicProgramming(typeof(System.FormatException))]

namespace Test
{
    public class Co6060ToInt16_all
    {
        [Fact]
        public static void runTest()
        {
            ///////////////////////////////////////////// Int16 Convert.ToInt16( Boolean )

            //[] ToInt16(Boolean) - Vanilla cases (true,false)

            // Setup Boolean Test
            {
                Boolean[] testValues = { true, false, };
                Int16[] expectedValues = { (Int16)1, (Int16)0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int16 result = Convert.ToInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int16 Convert.ToInt16( Double )

            //[] ToInt16(Double) - Vanilla cases (100.0,-100.0,0)

            // Setup Double Test
            {
                Double[] testValues = { 100.0, -100.0, 0, };
                Int16[] expectedValues = { (short)100, (short)-100, (short)0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int16 result = Convert.ToInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int16 Convert.ToInt16( Single )

            //[] ToInt16(Single) - Vanilla cases (100.0f,-100.0f,0.0f)

            // Setup Single Test
            {
                Single[] testValues = { 100.0f, -100.0f, 0.0f, };
                Int16[] expectedValues = { (short)100, (short)-100, (short)0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int16 result = Convert.ToInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int16 Convert.ToInt16( Int32 )

            //[] ToInt16(Int32) - Vanilla cases (100,-100,0)

            // Setup Int32 Test
            {
                Int32[] testValues = { 100, -100, 0, };
                Int16[] expectedValues = { ((Int16)100), -100, ((Int16)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int16 result = Convert.ToInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int16 Convert.ToInt16( Int64 )

            //[] ToInt16(Int64) - Vanilla cases (100,-100,0)

            // Setup Int64 Test
            {
                Int64[] testValues = { 100, -100, 0, };
                Int16[] expectedValues = { ((Int16)100), -100, ((Int16)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int16 result = Convert.ToInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int16 Convert.ToInt16( Decimal )

            //[] ToInt16(Decimal) - Vanilla cases (100,-100,0.0)

            // Setup Decimal Test
            {
                Decimal[] testValues = { new Decimal(100), new Decimal(-100), new Decimal(0.0), };
                Int16[] expectedValues = { ((Int16)100), -100, ((Int16)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int16 result = Convert.ToInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int16 Convert.ToInt16( String )

            //[] ToInt16(String) - Vanilla cases ("100","-100","0")

            // Setup String Test
            {
                String[] testValues = { "100", "-100", "0", null, };
                Int16[] expectedValues = { ((Int16)100), -100, ((Int16)0),
                                             ((Int16)0), // This was a bug, but we're not going to fix it.  See VSWhidbey #103151
                                         };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int16 result = Convert.ToInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ///////////////////////////////////////////// []Int16 Convert.ToInt16( String, IFormatPRovider )

            // Setup String Test
            {
                String[] testValues = { "100", "-100", "0", null, };
                Int16[] expectedValues = { ((Int16)100), -100, ((Int16)0), 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int16 result = Convert.ToInt16(testValues[i], new TestFormatProvider());
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int16 Convert.ToInt16( Byte )

            //[] ToInt16(Byte) - Vanilla cases (MaxValue,MinValue)

            // Setup Byte Test
            {
                Byte[] testValues = { Byte.MaxValue, Byte.MinValue, };
                Int16[] expectedValues = { ((Int16)255), ((Int16)0), };

                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int16 result = Convert.ToInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int16 Convert.ToInt16( String, Int32 )

            //[] ToInt16(String,Int32) - Vanilla cases {("7fff",16),("32767",10),("77777",8),("111111111111111",2),("8000",16),("-32768",10),("100000",8),("1000000000000000",2)}

            // Setup String, Int32 Test
            {
                String[] testValues = { "7fff", "32767", "77777", "111111111111111", "8000", "-32768", "100000", "1000000000000000", };
                Int32[] testBases = { 16, 10, 8, 2, 16, 10, 8, 2, };
                Int16[] expectedValues = { Int16.MaxValue, Int16.MaxValue, Int16.MaxValue, Int16.MaxValue, Int16.MinValue, Int16.MinValue, Int16.MinValue, Int16.MinValue, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int16 result = Convert.ToInt16(testValues[i], testBases[i]);
                    Assert.Equal(expectedValues[i], result);
                }

                String[] dummy = { null, };
                Assert.Equal(0, Convert.ToInt16(dummy[0], 10));
                Assert.Equal(0, Convert.ToInt16(dummy[0], 2));
                Assert.Equal(0, Convert.ToInt16(dummy[0], 8));
                Assert.Equal(0, Convert.ToInt16(dummy[0], 16));
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int16 Convert.ToInt16( SByte )

            //[] ToInt16(SByte) - Vanilla cases (100,-100,0)

            // Setup SByte Test
            {
                SByte[] testValues = { 100, -100, 0, };
                Int16[] expectedValues = { ((Int16)100), -100, ((Int16)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int16 result = Convert.ToInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int16 Convert.ToInt16( UInt16 )

            //[] ToInt16(UInt16) - Vanilla cases (100,0)

            // Setup UInt16 Test
            {
                UInt16[] testValues = { 100, 0, };
                Int16[] expectedValues = { ((Int16)100), ((Int16)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int16 result = Convert.ToInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int16 Convert.ToInt16( UInt32 )

            //[] ToInt16(UInt32) - Vanilla cases (100,0)

            // Setup UInt32 Test
            {
                UInt32[] testValues = { 100, 0, };
                Int16[] expectedValues = { ((Int16)100), ((Int16)0), };

                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int16 result = Convert.ToInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int16 Convert.ToInt16( UInt64 )

            //[] ToInt16(UInt64) - Vanilla cases (100,0)

            // Setup UInt64 Test
            {
                UInt64[] testValues = { 100, 0, };
                Int16[] expectedValues = { ((Int16)100), ((Int16)0), };

                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int16 result = Convert.ToInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int16 Convert.ToInt16( Char )

            //[] ToInt16(Char) - Vanilla cases ('A',Char.MinValue)

            // Setup Char Test
            {
                Char[] testValues = { 'A', Char.MinValue, };
                Int16[] expectedValues = { (Int16)'A', (Int16)Char.MinValue };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int16 result = Convert.ToInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Int16 Convert.ToInt16( Int16 )

            //[] ToInt16(Int16) - Vanilla cases (Int16.MaxValue,Int16.MinValue, 0)

            // Setup Int16 Test
            {
                Int16[] testValues = { Int16.MaxValue, Int16.MinValue, 0 };
                Int16[] expectedValues = { Int16.MaxValue, Int16.MinValue, 0 };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Int16 result = Convert.ToInt16(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            //[] ToInt16( Object ) - obj = null
            {
                Int16 bTest = Convert.ToInt16((Object)null);
                Assert.Equal(0, bTest);
            }

            //[] ToInt16( Object, IFP ) - obj = null
            {
                Int16 bTest = Convert.ToInt16((Object)null, new TestFormatProvider());
                Assert.Equal(0, bTest);
            }
        }

        [Fact]
        public static void runTests_negative()
        {
            // Exception Test Cases
            //[] ToInt16(Single) - Exception cases (Single.MaxValue, Single.MinValue)
            {
                Single[] errorValues = { Single.MaxValue, Single.MinValue, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { Int16 result = Convert.ToInt16(errorValues[i]); });
                }
            }

            // Exception Test Cases
            {
                //[] ToInt16(Double) - Exception cases ((Double) Int32.MaxValue, (Double)Int32.MinValue)
                Double[] errorValues = { ((Double)Int32.MaxValue), ((Double)Int32.MinValue), };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { Int16 result = Convert.ToInt16(errorValues[i]); });
                }
            }

            // Exception Test Cases
            {
                //[] ToInt16(Int32) - Exception cases (Int32.MaxValue, Int32.MinValue)
                Int32[] errorValues = { Int32.MaxValue, Int32.MinValue, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { Int16 result = Convert.ToInt16(errorValues[i]); });
                }
            }
            // Exception Test Cases
            {
                //[] ToInt16(Int64) - Exception cases (Int64.MaxValue, Int64.MinValue)
                Int64[] errorValues = { Int64.MaxValue, Int64.MinValue, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { Int16 result = Convert.ToInt16(errorValues[i]); });
                }
            }
            // Exception Test Cases
            //[] ToInt16(Decimal) - Exception cases (Decimal.MaxValue, Decimal.MinValue)
            {
                Decimal[] errorValues = { Decimal.MaxValue, Decimal.MinValue, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { Int16 result = Convert.ToInt16(errorValues[i]); });
                }
            }
            // Exception Test Cases

            //[] ToInt16(String) - Exception cases (Int32.MaxValue.ToString(), Int64.MaxValue.ToString(), null, "abba")
            Assert.Throws<OverflowException>(() => { Int16 result = Convert.ToInt16(Int32.MaxValue.ToString()); });
            Assert.Throws<OverflowException>(() => { Int16 result = Convert.ToInt16(Int32.MaxValue.ToString()); });

            Assert.Throws<FormatException>(() => { Int16 result = Convert.ToInt16("abba"); });

            // Exception Test Cases

            //[] ToInt16(String) - Exception cases (Int32.MaxValue.ToString(), Int64.MaxValue.ToString(), null, "abba")
            Assert.Throws<OverflowException>(() => { Int16 result = Convert.ToInt16(Int32.MaxValue.ToString(), new TestFormatProvider()); });
            Assert.Throws<OverflowException>(() => { Int16 result = Convert.ToInt16(Int32.MaxValue.ToString(), new TestFormatProvider()); });

            Assert.Throws<FormatException>(() => { Int16 result = Convert.ToInt16("abba", new TestFormatProvider()); });

            // Exception Test Cases
            {
                //[] ToInt16(String) - Exception cases {(null,2),("12",3),("11",5),("abba",8),("ffffffffffffffffffff",16)}

                String[] expectedExceptions = {
                                                  "System.FormatException", "System.ArgumentException", "System.ArgumentException",
                                                  "System.ArgumentException", "System.FormatException", "System.ArgumentException",
                                                  "System.OverflowException", "System.OverflowException", "System.OverflowException",
                                                  "System.OverflowException", "System.OverflowException",
                                              };
                String[] errorValues = {
                                           "12", "10", "11", "abba", "ffffffffffffffffffff", "-ab", "32768", "-32769",
                                           "11111111111111111", "1FFFF", "777777",
                                       };

                Int32[] errorBases = { 2, -1, 3, 0, 8, 16, 10, 10, 2, 16, 8, };

                for (int i = 0; i < errorValues.Length; i++)
                {
                    try
                    {
                        Int16 result = Convert.ToInt16(errorValues[i], errorBases[i]);
                        Assert.True(false, "Exception expected: " + expectedExceptions[i]);
                    }
                    catch (Exception e)
                    {
                        var exceptionName = e.GetType().FullName;
                        var expectedName = expectedExceptions[i];
                        Assert.Equal(exceptionName, expectedName);
                    }
                }
            }
            // Exception Test Cases

            //[] ToInt16(UInt16) - Exception cases (UInt16.MaxValue)
            Assert.Throws<OverflowException>(() => { Int16 result = Convert.ToInt16(UInt16.MaxValue); });// Exception Test Cases

            //[] ToInt16(UInt32) - Exception cases (UInt32.MaxValue)
            Assert.Throws<OverflowException>(() => { Int16 result = Convert.ToInt16(UInt32.MaxValue); });
            // Exception Test Cases
            //[] ToInt16(UInt64) - Exception cases (UInt64.MaxValue)
            Assert.Throws<OverflowException>(() => { Int16 result = Convert.ToInt16(UInt64.MaxValue); });

            ///////////////////////////////////////////// Int16 Convert.ToInt16( Object )
            //[] ToInt16( Object ) - Exception Case (Object that does not implement IConvertible) 
            Assert.Throws<InvalidCastException>(() => { Int16 bTest = Convert.ToInt16(new Object()); });

            ///////////////////////////////////////////// []Int16 Convert.ToInt16( Object, IformatProvider )
            Assert.Throws<InvalidCastException>(() => { Int16 bTest = Convert.ToInt16(new Object(), new TestFormatProvider()); });

            ///////////////////////////////////////////// []Int16 Convert.ToInt16( DateTime )
            Assert.Throws<InvalidCastException>(() => { Int16 bTest = Convert.ToInt16(DateTime.Now); });
        }
    }
}
