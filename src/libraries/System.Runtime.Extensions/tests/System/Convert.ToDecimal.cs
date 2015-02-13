// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace Test
{
    public class Co6058ToDecimal_all
    {
        [Fact]
        public static void runTestPositive1()
        {
            ///////////////////////////////////////////// Decimal Convert.ToDecimal( SByte )

            //[] ToDecimal(SByte) - Vanilla Cases (SByte.MinValue,SByte.MaxValue,((SByte) 0 ))

            // Setup SByte Test
            {
                //  String expected = null;
                SByte[] testValues = { SByte.MinValue, SByte.MaxValue, ((SByte)0), };
                Decimal[] expectedValues = { new Decimal(-128), new Decimal(127), new Decimal(0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Decimal result = Convert.ToDecimal(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
        }

        [Fact]
        public static void runTestPositive2()
        {
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Decimal Convert.ToDecimal( Double )

            //[] ToDecimal(Double) - Vanilla Cases (((Double) 1000.0 ),((Double) 100.0 ),((Double) 0.0 ),((Double) 0.001 ),-1000.0,-100.0,)

            // Setup Double Test
            {
                Double[] testValues = { ((Double)1000.0), ((Double)100.0), ((Double)0.0), ((Double)0.001), -1000.0, -100.0, };
                Decimal[] expectedValues = { new Decimal(1000.0), new Decimal(100.0), new Decimal(0.0), new Decimal(0.001), new Decimal(-1000.0), new Decimal(-100.0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Decimal result = Convert.ToDecimal(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
        }

        [Fact]
        public static void runTestPositive3()
        {
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Decimal Convert.ToDecimal( Single )

            //[] ToDecimal(Single) - Vanilla Cases (       1000.0f,
            //    100.0f,
            //    0.0f,
            //    -1.0f,
            //    -100.0f,)

            // Setup Single Test
            {
                Single[] testValues = { 1000.0f, 100.0f, 0.0f, -1.0f, -100.0f, };
                Decimal[] expectedValues = { new Decimal(1000.0), new Decimal(100.0), new Decimal(0.0), new Decimal(-1.0), new Decimal(-100.0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Decimal result = Convert.ToDecimal(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
        }
        [Fact]
        public static void runTestPositive4()
        {
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Decimal Convert.ToDecimal( Int32 )

            //[] ToDecimal(Int32) - Vanilla Cases (
            //      Int32.MaxValue,
            //      Int32.MinValue,
            //      0
            //      )

            // Setup Int32 Test
            {
                Int32[] testValues = { Int32.MaxValue, Int32.MinValue, 0, };
                Decimal[] expectedValues = { (Decimal)Int32.MaxValue, (Decimal)Int32.MinValue, (Decimal)((Int32)0), };

                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Decimal result = Convert.ToDecimal(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
        }
        [Fact]
        public static void runTestPositive5()
        {
            ///////////////////////////////////////////// []Decimal Convert.ToDecimal( Boolean )

            {
                Boolean[] testValues = { true, false, };
                Decimal[] expectedValues = { 1.0m, Decimal.Zero, };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Decimal result = Convert.ToDecimal(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
        }
        [Fact]
        public static void runTestPositive6()
        {
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Decimal Convert.ToDecimal( Int64 )

            //[] ToDecimal(Int64) - Vanilla Cases (Int64.MaxValue,Int64.MinValue,(long) 0,)

            // Setup Int64 Test
            {
                Int64[] testValues = { Int64.MaxValue, Int64.MinValue, (long)0, };
                Decimal[] expectedValues = { (Decimal)Int64.MaxValue, (Decimal)Int64.MinValue, (Decimal)(((Int64)(long)0)), };
                // Vanila Test Cases

                for (int i = 0; i < testValues.Length; i++)
                {
                    Decimal result = Convert.ToDecimal(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
        }
        [Fact]
        public static void runTestPositive7()
        {
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Decimal Convert.ToDecimal( Int16 )

            //[] ToDecimal(Int16) - Vanilla Cases (Int16.MaxValue,Int16.MinValue,(short) 0,)

            // Setup Int16 Test
            {
                Int16[] testValues = { Int16.MaxValue, Int16.MinValue, (short)0, };
                Decimal[] expectedValues = { (Decimal)Int16.MaxValue, (Decimal)Int16.MinValue, (Decimal)(((Int16)(short)0)), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Decimal result = Convert.ToDecimal(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
        }
        [Fact]
        public static void runTestPositive8()
        {
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Decimal Convert.ToDecimal( String )

            //[] ToDecimal(String) - Vanilla Cases ("2147483647","9223372036854775807",Decimal.MaxValue.ToString(),Decimal.MinValue.ToString(),"0",)

            // Setup String Test
            {
                String[] testValues = { "2147483647", "9223372036854775807", Decimal.MaxValue.ToString(), Decimal.MinValue.ToString(), "0", null, };
                Decimal[] expectedValues = { new Decimal(Int32.MaxValue), new Decimal(Int64.MaxValue), Decimal.MaxValue, Decimal.MinValue, new Decimal(), new Decimal(), };

                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Decimal result = Convert.ToDecimal(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
        }
        [Fact]
        public static void runTestPositive9()
        {
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// [] Decimal Convert.ToDecimal( String, IFormatProvider )

            // Setup String Test
            {
                String[] testValues = { "2147483647", "9223372036854775807", Decimal.MaxValue.ToString(), Decimal.MinValue.ToString(), "0", null, };
                Decimal[] expectedValues = { new Decimal(Int32.MaxValue), new Decimal(Int64.MaxValue), Decimal.MaxValue, Decimal.MinValue, new Decimal(), new Decimal(), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Decimal result = Convert.ToDecimal(testValues[i], new TestFormatProvider());
                    Assert.Equal(expectedValues[i], result);
                }
            }
        }
        [Fact]
        public static void runTestPositive10()
        {
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Decimal Convert.ToDecimal( Byte )

            //[] ToDecimal(Byte) - Vanilla Cases (Byte.MaxValue,Byte.MinValue,)

            // Setup Byte Test
            {
                Byte[] testValues = { Byte.MaxValue, Byte.MinValue, };
                Decimal[] expectedValues = { new Decimal(255), new Decimal(0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Decimal result = Convert.ToDecimal(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
        }
        [Fact]
        public static void runTestPositive11()
        {
            ////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Decimal Convert.ToDecimal( UInt64 )

            //[] ToDecimal(UInt64) - Vanilla Cases (UInt64.MaxValue,UInt64.MinValue,)

            // Setup UInt64 Test
            {
                UInt64[] testValues = { UInt64.MaxValue, UInt64.MinValue, };
                Decimal[] expectedValues = { new Decimal(UInt64.MaxValue), new Decimal(0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Decimal result = Convert.ToDecimal(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
        }
        [Fact]
        public static void runTestPositive12()
        {
            ////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Decimal Convert.ToDecimal( UInt32 )

            //[] ToDecimal(UInt32) - Vanilla Cases (UInt32.MaxValue,UInt32.MinValue,)

            // Setup UInt32 Test

            {
                UInt32[] testValues = { UInt32.MaxValue, UInt32.MinValue, };
                Decimal[] expectedValues = { new Decimal(UInt32.MaxValue), new Decimal(0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Decimal result = Convert.ToDecimal(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
        }
        [Fact]
        public static void runTestPositive13()
        {
            ////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Decimal Convert.ToDecimal( UInt16 )

            //[] ToDecimal(UInt16) - Vanilla Cases (UInt16.MaxValue,UInt16.MinValue,)

            // Setup UInt16 Test
            {
                UInt16[] testValues = { UInt16.MaxValue, UInt16.MinValue, };
                Decimal[] expectedValues = { new Decimal(UInt16.MaxValue), new Decimal(0), };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Decimal result = Convert.ToDecimal(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }
        }
        [Fact]
        public static void runTestPositive14()
        {
            ////////////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////// Decimal Convert.ToDecimal( Decimal )

            //[] ToDecimal(Decimal) - Vanilla Cases (Decimal.MaxValue,Decimal.MinValue, 0)
            // Setup Decimal Test
            {
                Decimal[] testValues = { Decimal.MaxValue, Decimal.MinValue, 0 };
                Decimal[] expectedValues = { Decimal.MaxValue, Decimal.MinValue, new Decimal(0) };
                // Vanila Test Cases
                for (int i = 0; i < testValues.Length; i++)
                {
                    Decimal result = Convert.ToDecimal(testValues[i]);
                    Assert.Equal(expectedValues[i], result);
                }
            }

            ///////////////////////////////////////////// Decimal Convert.ToDecimal( Object )
            {
                //[] ToDecimal( Object ) - obj = null
                Decimal bTest = Convert.ToDecimal((Object)null);
                Assert.Equal((Decimal)0, bTest);
            }

            //[] ToDecimal( Object, IFP ) - obj = null
            {
                Decimal bTest = Convert.ToDecimal((Object)null, new TestFormatProvider());
                Assert.Equal((Decimal)0, bTest);
            }
        }

        [Fact]
        public static void runTests_Negative1()
        {
            // Exception Test Cases
            //[] ToDecimal(Double) - Exception Cases (Double.MaxValue,Double.MinValue)
            {
                Double[] errorValues = { Double.MaxValue, Double.MinValue, };
                for (int i = 0; i < errorValues.Length; i++)
                {
                    Assert.Throws<OverflowException>(() => { Decimal result = Convert.ToDecimal(errorValues[i]); });
                }
            }
        }

        [Fact]
        public static void runTests_Negative2()
        {
            // Exception Test Cases
            //[] ToDecimal(Single) - Exception Cases (Single.MaxValue,Single.MinValue)
            Single[] errorValues = { Single.MaxValue, Single.MinValue, };
            for (int i = 0; i < errorValues.Length; i++)
            {
                Assert.Throws<OverflowException>(() => { Decimal result = Convert.ToDecimal(errorValues[i]); });
            }
        }

        [Fact]
        public static void runTests_Negative3()
        {
            // Exception Test Cases
            //[] ToDecimal(String) - Exception Cases (null,"100E12","1" + Decimal.MaxValue.ToString())
            Assert.Throws<FormatException>(() => { Decimal result = Convert.ToDecimal("100E12"); });
            Assert.Throws<OverflowException>(() => { Decimal result = Convert.ToDecimal("1" + Decimal.MaxValue.ToString()); });
        }

        [Fact]
        public static void runTests_Negative4()
        {
            // Exception Test Cases
            //[] ToDecimal(String) - Exception Cases (null,"100E12","1" + Decimal.MaxValue.ToString())
            Assert.Throws<FormatException>(() => { Decimal result = Convert.ToDecimal("100E12", new TestFormatProvider()); });
            Assert.Throws<OverflowException>(() => { Decimal result = Convert.ToDecimal("1" + Decimal.MaxValue.ToString(), new TestFormatProvider()); });
        }

        [Fact]
        public static void runTests_Negative5()
        {
            ///////////////////////////////////////////// Decimal Convert.ToDecimal( Char )
            // Setup Char Test
            // Exception Test Cases
            //[] ToDecimal(Char) - Exception Cases (Char.MinValue, Char.MaxValue, 'b')
            Char[] errorValues = { 'b', Char.MinValue, Char.MaxValue };
            for (int i = 0; i < errorValues.Length; i++)
            {
                try
                {
                    Decimal result = Convert.ToDecimal(errorValues[i]);
                    Assert.True(false, "FAIL. InvalidCastException expected for index: " + i + " result: " + result + ":" + (int)errorValues[i]);
                }
                catch (InvalidCastException)
                {
                    //Logger.LogInformation("exception caught for index: " + i);
                }
                //Assert.Throws<InvalidCastException>(() => {  }, "Exception expected.");
            }
        }

        [Fact]
        public static void runTests_Negative6()
        {
            //[] ToDecimal( Object ) - Exception Case (Object that does not implement IConvertible) 
            Assert.Throws<InvalidCastException>(() => { Decimal bTest = Convert.ToDecimal(new Object()); });
        }

        [Fact]
        public static void runTests_Negative7()
        {
            ///////////////////////////////////////////// [] Decimal Convert.ToDecimal( Object, IFormatProvider )
            Assert.Throws<InvalidCastException>(() => { Decimal bTest = Convert.ToDecimal(new Object(), new TestFormatProvider()); });
        }

        [Fact]
        public static void runTests_Negative8()
        {
            ///////////////////////////////////////////// [] Decimal Convert.ToDecimal( DateTime )
            Assert.Throws<InvalidCastException>(() => { Decimal bTest = Convert.ToDecimal(DateTime.Now); });
            ////////////////////////////////////////////////////////////////////////////////////////
        }
    }
}
