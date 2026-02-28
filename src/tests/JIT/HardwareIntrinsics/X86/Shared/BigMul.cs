// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JIT.HardwareIntrinsics.X86
{
    public class BigMulTestSuite
    {
        private class NumberContainer
        {
            public long LongVal;
            public ulong ULongVal;
        }

        private static class StaticNumberContainer
        {
            public static long LongVal = 0x7FFFFFFFFFFFFFFF;
            public static ulong ULongVal = 0xFFFFFFFFFFFFFFFF;
        }

        /// <summary>
        /// Main entry point for the XUnit test suite.
        /// This method orchestrates the execution of all sub-test modules.
        /// </summary>
        [Fact]
        public static void TestEntryPoint()
        {
            // Execution of specialized test modules
            TestZeroMultiplying();
            TestIdentityMultiplying();
            TestUnsignedMaxBoundaries();
            TestSignedMaxBoundaries();
            TestNegativeValueLogic();
            TestPowerOfTwoPatterns();
            TestBitPatternInterference();
            TestInstanceFieldAccess();
            TestStaticFieldAccess();
            TestNullReferenceScenarios();
            TestLargeRandomCalculations();
            TestSignedOverflowIntoHigh();
        }

        private static void TestZeroMultiplying()
        {
            // Testing zero cases for both signed and unsigned
            long lowL;
            long highL = Math.BigMul(0L, 12345L, out lowL);
            Assert.Equal(0L, highL);
            Assert.Equal(0L, lowL);

            highL = Math.BigMul(99L, 0L, out lowL);
            Assert.Equal(0L, highL);
            Assert.Equal(0L, lowL);

            ulong lowU;
            ulong highU = Math.BigMul(0UL, 0UL, out lowU);
            Assert.Equal(0UL, highU);
            Assert.Equal(0UL, lowU);
        }

        private static void TestIdentityMultiplying()
        {
            // Testing 1 and -1 (identity elements)
            long lowL;
            long highL = Math.BigMul(1L, 500L, out lowL);
            Assert.Equal(0L, highL);
            Assert.Equal(500L, lowL);

            highL = Math.BigMul(-1L, 10L, out lowL);
            // -1 * 10 = -10. In 128-bit, this is 0xFF...FFF6
            // High part should be -1 (all ones in two's complement)
            Assert.Equal(-1L, highL);
            Assert.Equal(-10L, lowL);
        }

        private static void TestUnsignedMaxBoundaries()
        {
            // Testing MaxValue * MaxValue
            // (2^64 - 1)^2 = 2^128 - 2^65 + 1
            ulong a = ulong.MaxValue;
            ulong b = ulong.MaxValue;
            ulong low;
            ulong high = Math.BigMul(a, b, out low);

            // Expected low: (ulong.MaxValue * ulong.MaxValue) truncate to 64 bits which is 1
            // Expected high: ulong.MaxValue - 1
            Assert.Equal(1UL, low);
            Assert.Equal(0xFFFFFFFFFFFFFFFEUL, high);
        }

        private static void TestSignedMaxBoundaries()
        {
            long low;
            long high;

            // Max * Max
            high = Math.BigMul(long.MaxValue, long.MaxValue, out low);
            Int128 expected = (Int128)long.MaxValue * long.MaxValue;

            Assert.Equal((long)(expected & 0xFFFFFFFFFFFFFFFF), low);
            Assert.Equal((long)(expected >> 64), high);

            // Min * Min
            high = Math.BigMul(long.MinValue, long.MinValue, out low);
            expected = (Int128)long.MinValue * long.MinValue;

            Assert.Equal((long)(expected & 0xFFFFFFFFFFFFFFFF), low);
            Assert.Equal((long)(expected >> 64), high);
        }

        private static void TestNegativeValueLogic()
        {
            long low;
            long high;

            // Small negative * Large positive
            high = Math.BigMul(-2L, 0x7FFFFFFFFFFFFFFF, out low);
            Int128 expected = (Int128)(-2L) * 0x7FFFFFFFFFFFFFFF;

            Assert.Equal((long)(expected & 0xFFFFFFFFFFFFFFFF), low);
            Assert.Equal((long)(expected >> 64), high);

            // MinValue * 1
            high = Math.BigMul(long.MinValue, 1L, out low);
            Assert.Equal(long.MinValue, low);
            Assert.Equal(-1L, high); // Sign extension for 128-bit
        }

        private static void TestPowerOfTwoPatterns()
        {
            // 2^32 * 2^32 = 2^64. 
            // Result should be High=1, Low=0
            ulong lowU;
            ulong highU = Math.BigMul(1UL << 32, 1UL << 32, out lowU);
            Assert.Equal(1UL, highU);
            Assert.Equal(0UL, lowU);

            // 2^63 * 2
            highU = Math.BigMul(1UL << 63, 2UL, out lowU);
            Assert.Equal(1UL, highU);
            Assert.Equal(0UL, lowU);
        }

        private static void TestBitPatternInterference()
        {
            // Alternating bit patterns
            ulong a = 0xAAAAAAAAAAAAAAAA;
            ulong b = 0x5555555555555555;
            ulong low;
            ulong high = Math.BigMul(a, b, out low);

            UInt128 expected = (UInt128)a * b;
            Assert.Equal((ulong)(expected & 0xFFFFFFFFFFFFFFFF), low);
            Assert.Equal((ulong)(expected >> 64), high);
        }

        private static void TestInstanceFieldAccess()
        {
            // Testing passing values from instance fields
            var container = new NumberContainer { LongVal = 4000000000L };
            long multiplier = 5000000000L;
            long low;
            long high = Math.BigMul(container.LongVal, multiplier, out low);

            Int128 expected = (Int128)container.LongVal * multiplier;
            Assert.Equal((long)(expected & 0xFFFFFFFFFFFFFFFF), low);
            Assert.Equal((long)(expected >> 64), high);
        }

        private static void TestStaticFieldAccess()
        {
            // Testing passing values from static fields
            ulong low;
            ulong high = Math.BigMul(StaticNumberContainer.ULongVal, 2UL, out low);

            UInt128 expected = (UInt128)StaticNumberContainer.ULongVal * 2UL;
            Assert.Equal((ulong)(expected & 0xFFFFFFFFFFFFFFFF), low);
            Assert.Equal((ulong)(expected >> 64), high);
        }

        private static void TestNullReferenceScenarios()
        {
            // Checking that accessing fields from a null object throws NRE 
            // before it ever reaches BigMul
            NumberContainer nullContainer = null;

            Assert.Throws<NullReferenceException>(() =>
            {
                long low;
                Math.BigMul(nullContainer.LongVal, 10L, out low);
            });

            Assert.Throws<NullReferenceException>(() =>
            {
                ulong low;
                Math.BigMul(10UL, nullContainer.ULongVal, out low);
            });
        }

        private static void TestLargeRandomCalculations()
        {
            // Series of semi-random large values
            ulong[] testValues = {
                0xDEADC0DEBEEFCAFE,
                0x1234567890ABCDEF,
                0xFFEEEECCCCAAAA88,
                0x7777777777777777
            };

            for (int i = 0; i < testValues.Length; i++)
            {
                for (int j = 0; j < testValues.Length; j++)
                {
                    ulong low;
                    ulong high = Math.BigMul(testValues[i], testValues[j], out low);
                    UInt128 expected = (UInt128)testValues[i] * testValues[j];

                    Assert.Equal((ulong)(expected & 0xFFFFFFFFFFFFFFFF), low);
                    Assert.Equal((ulong)(expected >> 64), high);
                }
            }
        }

        private static void TestSignedOverflowIntoHigh()
        {
            // Specifically check cases where the signed product overflows 64 bits
            // into the high part but remains positive.
            long a = 0x4000000000000000; // 2^62
            long b = 4;                  // 2^2
                                         // 2^64 -> High = 1, Low = 0 (as a 128-bit signed)

            long low;
            long high = Math.BigMul(a, b, out low);

            Assert.Equal(1L, high);
            Assert.Equal(0L, low);

            // Case where result is extremely negative
            // MinValue * 2
            high = Math.BigMul(long.MinValue, 2, out low);
            // long.MinValue is -2^63. * 2 = -2^64.
            // In 128-bit: High = -1 (all Fs), Low = 0
            Assert.Equal(-1L, high);
            Assert.Equal(0L, low);
        }

        /// <summary>
        /// Supplementary test to ensure that the 'out' parameter behavior
        /// handles stack-allocated variables correctly.
        /// </summary>
        private static void TestOutParameterConsistency()
        {
            long a = 123456789;
            long b = 987654321;
            long lowExpected = a * b;

            Math.BigMul(a, b, out long lowActual);

            Assert.Equal(lowExpected, lowActual);
        }

        /// <summary>
        /// Test using properties instead of fields to ensure getter logic 
        /// doesn't interfere with the intrinsic mapping.
        /// </summary>
        private class PropertyContainer
        {
            public long Val => 0x1122334455667788;
        }

        private static void TestPropertyInputs()
        {
            var p = new PropertyContainer();
            long low;
            long high = Math.BigMul(p.Val, 2, out low);

            Assert.Equal(0L, high);
            Assert.Equal(p.Val * 2, low);
        }
    }
}
