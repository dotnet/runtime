// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;
// [assembly: System.Reflection.Consumption.EnableDynamicProgramming(typeof(System.FormatException))]

namespace Test
{
    public class Co6064ToSByte_all
    {
        [Fact]
        public static void runTest()
        {
            ///////////////////////////////////////////// SByte Convert.ToSByte( Boolean )

            //[] ToSByte(Boolean) - Vanilla Cases ()

            // Setup Boolean Test
            {
                Boolean[] testValues = { true, false, };
                SByte[] expectedValues = { (SByte)1, (SByte)0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    SByte result = Convert.ToSByte(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// SByte Convert.ToSByte( Double )

            //[] ToSByte(Double) - Vanilla Cases (100.0,-100.0,0)

            // Setup Double Test
            {
                Double[] testValues = { 100.0, -100.0, 0, };
                SByte[] expectedValues = { (SByte)100, (SByte)(-100), (SByte)0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    SByte result = Convert.ToSByte(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// SByte Convert.ToSByte( Single )

            //[] ToSByte(Single) - Vanilla Cases (100.0f,-100.0f,0.0f)

            // Setup Single Test

            {
                Single[] testValues = { 100.0f, -100.0f, 0.0f, };
                SByte[] expectedValues = { ((SByte)100), -100, ((SByte)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    SByte result = Convert.ToSByte(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// SByte Convert.ToSByte( Int32 )

            //[] ToSByte(Int32) - Vanilla Cases (100,-100,0)

            // Setup Int32 Test
            {
                Int32[] testValues = { 100, -100, 0, };
                SByte[] expectedValues = { ((SByte)100), -100, ((SByte)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    SByte result = Convert.ToSByte(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// SByte Convert.ToSByte( Int64 )

            //[] ToSByte(Int64) - Vanilla Cases (100,-100,0)

            // Setup Int64 Test
            {
                Int64[] testValues = { 100, -100, 0, };
                SByte[] expectedValues = { ((SByte)100), -100, ((SByte)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    SByte result = Convert.ToSByte(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// SByte Convert.ToSByte( Int16 )

            //[] ToSByte(Int16) - Vanilla Cases (((Int16) 100 ),-100,((Int16) 0 ))

            // Setup Int64 Test
            {
                Int16[] testValues = { ((Int16)100), -100, ((Int16)0), };
                SByte[] expectedValues = { ((SByte)100), -100, ((SByte)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    SByte result = Convert.ToSByte(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// SByte Convert.ToSByte( Decimal )

            //[] ToSByte(Decimal) - Vanilla Cases (new Decimal( 100 ),new Decimal( -100 ),new Decimal( 0.0 ))

            // Setup Decimal Test
            {
                Decimal[] testValues = { new Decimal(100), new Decimal(-100), new Decimal(0.0), };
                SByte[] expectedValues = { ((SByte)100), -100, ((SByte)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    SByte result = Convert.ToSByte(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// SByte Convert.ToSByte( String )

            //[] ToSByte(String) - Vanilla Cases ("100","-100","0")

            // Setup String Test
            {
                String[] testValues = { "100", "-100", "0", null, };
                SByte[] expectedValues = { ((SByte)100), -100, ((SByte)0), 0, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    SByte result = Convert.ToSByte(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ///////////////////////////////////////////// []SByte Convert.ToSByte( String, IFormatPRovider )

            // Setup String Test
            {
                String[] testValues = { "100", "-100", "0", };
                SByte[] expectedValues = { ((SByte)100), -100, ((SByte)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    SByte result = Convert.ToSByte(testValues[i], new TestFormatProvider());
                    Assert.Equal(expectedValues[i], result);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// SByte Convert.ToSByte( String, Int32 )

            //[] ToSByte(String,Int32) - Vanilla Cases {("7f",16),("127",10),("177",8),("1111111",2),("80",16),("-128",10),("200",8),("10000000"2)}

            // Setup String, Int32 Test

            {
                String[] dummy = { null, };
                Assert.Equal(0, Convert.ToSByte(dummy[0], 10));
                Assert.Equal(0, Convert.ToSByte(dummy[0], 2));
                Assert.Equal(0, Convert.ToSByte(dummy[0], 8));
                Assert.Equal(0, Convert.ToSByte(dummy[0], 16));
            }
            {
                String[] testValues = { "7f", "127", "177", "1111111", "80", "-128", "200", "10000000", };
                Int32[] testBases = { 16, 10, 8, 2, 16, 10, 8, 2, };
                SByte[] expectedValues = { SByte.MaxValue, SByte.MaxValue, SByte.MaxValue, SByte.MaxValue, SByte.MinValue, SByte.MinValue, SByte.MinValue, SByte.MinValue, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    SByte result = Convert.ToSByte(testValues[i], testBases[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// SByte Convert.ToSByte( UInt16 )

            //[] ToSByte(UInt16) - Vanilla Cases (100, 0)

            // Setup UInt16 Test

            {
                UInt16[] testValues = { 100, 0, };
                SByte[] expectedValues = { ((SByte)100), ((SByte)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    SByte result = Convert.ToSByte(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// SByte Convert.ToSByte( UInt32 )

            //[] ToSByte(UInt32) - Vanilla Cases (100,0)

            // Setup UInt32 Test

            {
                UInt32[] testValues = { 100, 0, };
                SByte[] expectedValues = { ((SByte)100), ((SByte)0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    SByte result = Convert.ToSByte(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// SByte Convert.ToSByte( UInt64 )

            //[] ToSByte(UInt64) - Vanilla Cases (100,0)

            // Setup UInt64 Test

            {
                UInt64[] testValues = { 100, 0, };
                SByte[] expectedValues = { ((SByte)100), ((SByte)0), };

                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    SByte result = Convert.ToSByte(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// SByte Convert.ToSByte( Char )

            //[] ToSByte(Char) - Vanilla Cases ('A',Char.MinValue)

            // Setup Char Test

            {
                Char[] testValues = { 'A', Char.MinValue, };
                SByte[] expectedValues = { (SByte)'A', (SByte)Char.MinValue, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    SByte result = Convert.ToSByte(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// SByte Convert.ToSByte( SByte )

            //[] ToSByte(SByte) - Vanilla Cases (SByte.MaxValue,SByte.MinValue, SByte.Empty)

            // Setup SByte Test
            {
                SByte[] testValues = { SByte.MaxValue, SByte.MinValue };
                SByte[] expectedValues = { SByte.MaxValue, SByte.MinValue };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    SByte result = Convert.ToSByte(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            //[] ToSByte( Object ) - obj = null
            {
                SByte bTest = Convert.ToSByte((Object)null);
                Assert.Equal(0, bTest);
            }

            //[] ToSByte( Object, IFP ) - obj = null
            {
                SByte bTest = Convert.ToSByte((Object)null, new TestFormatProvider());
                Assert.Equal(0, bTest);
            }
        }

        [Fact]
        public static void runTests_Negative()
        {
            {
                // Exception Test Cases
                //[] ToSByte(Single) - Exception Cases (Single.MaxValue,Single.MinValue)

                Single[] errorValues = { Single.MaxValue, Single.MinValue, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { SByte result = Convert.ToSByte(errorValues[i]); });
                }
            }
            {
                // Exception Test Cases
                //[] ToSByte(Int32) - Exception Cases (Int32.MaxValue,Int32.MinValue)
                Int32[] errorValues = { Int32.MaxValue, Int32.MinValue, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { SByte result = Convert.ToSByte(errorValues[i]); });
                }
            }
            {
                // Exception Test Cases
                //[] ToSByte(Int64) - Exception Cases (Int64.MaxValue,Int64.MinValue)
                Int64[] errorValues = { Int64.MaxValue, Int64.MinValue, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { SByte result = Convert.ToSByte(errorValues[i]); });
                }
            }
            {
                // Exception Test Cases
                //[] ToSByte(Int16) - Exception Cases (Int16.MaxValue,Int16.MinValue)
                Int16[] errorValues = { Int16.MaxValue, Int16.MinValue, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { SByte result = Convert.ToSByte(errorValues[i]); });
                }
            }
            // Exception Test
            //[] ToSByte(Byte) - Exception Cases (Byte.MaxValue)

            Assert.Throws<OverflowException>(() => Convert.ToSByte(Byte.MaxValue));

            // Exception Test Cases
            //[] ToSByte(Double) - Exception Cases (((Double) Int32.MaxValue ),((Double) Int32.MinValue ))
            {
                Double[] errorValues = { ((Double)Int32.MaxValue), ((Double)Int32.MinValue), };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { SByte result = Convert.ToSByte(errorValues[i]); });
                }
            }
            {
                // Exception Test Cases
                //[] ToSByte(Decimal) - Exception Cases (Decimal.MaxValue,Decimal.MinValue)
                Decimal[] errorValues = { Decimal.MaxValue, Decimal.MinValue, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { SByte result = Convert.ToSByte(errorValues[i]); });
                }
            }
            // Exception Test Cases
            //[] ToSByte(String) - Exception Cases (Int32.MaxValue.ToString(),Int64.MaxValue.ToString(),null,"abba")
            Assert.Throws<OverflowException>(() => { SByte result = Convert.ToSByte(Int32.MaxValue.ToString()); });
            Assert.Throws<OverflowException>(() => { SByte result = Convert.ToSByte(Int64.MaxValue.ToString()); });
            Assert.Throws<FormatException>(() => { SByte result = Convert.ToSByte("abba"); });

            // Exception Test Cases
            //[] ToSByte(String) - Exception Cases (Int32.MaxValue.ToString(),Int64.MaxValue.ToString(),null,"abba")
            Assert.Throws<OverflowException>(() => { SByte result = Convert.ToSByte(Int32.MaxValue.ToString(), new TestFormatProvider()); });
            Assert.Throws<OverflowException>(() => { SByte result = Convert.ToSByte(Int64.MaxValue.ToString(), new TestFormatProvider()); });
            Assert.Throws<ArgumentNullException>(() => { SByte result = Convert.ToSByte(null, new TestFormatProvider()); });
            Assert.Throws<FormatException>(() => { SByte result = Convert.ToSByte("abba", new TestFormatProvider()); });
            {
                // Exception Test Cases
                //[] ToSByte(String,Int32) - Exception Cases {(null,2),("12",3),("11",5),("abba",8),("ffffffffffffffffffff",16)}
                String[] expectedExceptions = { "System.FormatException", "System.ArgumentException", "System.ArgumentException", "System.ArgumentException", "System.FormatException", "System.ArgumentException", "System.OverflowException", "System.OverflowException", "System.OverflowException", "System.OverflowException", "System.OverflowException", };
                String[] errorValues = { "12", "10", "11", "abba", "ffffffffffffffffffff", "-ab", "128", "-129", "111111111", "1FF", "777", };
                Int32[] errorBases = { 2, -1, 3, 0, 8, 16, 10, 10, 2, 16, 8, };

                for (int i = 0; i < errorValues.Length; i++)
                {
                    try
                    {
                        SByte result = Convert.ToSByte(errorValues[i], errorBases[i]);
                        Assert.True(false, "Exception expected: " + expectedExceptions[i]);
                    }
                    catch (Exception e)
                    {
                        Assert.True(e.GetType().FullName.Equals(expectedExceptions[i]), " Wrong Exception Thrown. Expected" + expectedExceptions[i] + ", Actual: " + e.GetType().FullName);
                    }
                }
            }

            // Exception Test Cases
            //[] ToSByte(UInt16) - Exception Cases (UInt16.MaxValue)
            Assert.Throws<OverflowException>(() => { SByte result = Convert.ToSByte(UInt16.MaxValue); });// Exception Test Cases

            // Exception Test Cases
            //[] ToSByte(UInt32) - Exception Cases (UInt32.MaxValue)
            Assert.Throws<OverflowException>(() => { SByte result = Convert.ToSByte(UInt32.MaxValue); });

            // Exception Test Cases
            //[] ToSByte(UInt64) - Exception Cases (UInt64.MaxValue)
            Assert.Throws<OverflowException>(() => { SByte result = Convert.ToSByte(UInt64.MaxValue); });

            // Exception test
            //[] ToSByte(Char) - Exception Cases (Char.MaxValue)
            Assert.Throws<OverflowException>(() => { SByte result = Convert.ToSByte(Char.MaxValue); });
            ///////////////////////////////////////////// SByte Convert.ToSByte( Object )
            //[] ToSByte( Object ) - Exception Case (Object that does not implement IConvertible) 
            Assert.Throws<InvalidCastException>(() => { SByte bTest = Convert.ToSByte(new Object()); });

            ///////////////////////////////////////////// []SByte Convert.ToSByte( Object, IFormatPRovider )
            Assert.Throws<InvalidCastException>(() => { SByte bTest = Convert.ToSByte(new Object(), new TestFormatProvider()); });

            ///////////////////////////////////////////// []SByte Convert.ToSByte( DateTime )
            Assert.Throws<InvalidCastException>(() => { SByte bTest = Convert.ToSByte(DateTime.Now); });
        }
    }
}
