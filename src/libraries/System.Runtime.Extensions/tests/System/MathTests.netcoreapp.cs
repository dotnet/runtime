// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Tests
{
    public static partial class MathTests
    {
        public static IEnumerable<object[]> Clamp_UnsignedInt_TestData()
        {
            yield return new object[] { 1, 1, 3, 1 };
            yield return new object[] { 2, 1, 3, 2 };
            yield return new object[] { 3, 1, 3, 3 };
            yield return new object[] { 1, 1, 1, 1 };

            yield return new object[] { 0, 1, 3, 1 };
            yield return new object[] { 4, 1, 3, 3 };
        }

        public static IEnumerable<object[]> Clamp_SignedInt_TestData()
        {
            yield return new object[] { -1, -1, 1, -1 };
            yield return new object[] { 0, -1, 1, 0 };
            yield return new object[] { 1, -1, 1, 1 };
            yield return new object[] { 1, -1, 1, 1 };

            yield return new object[] { -2, -1, 1, -1 };
            yield return new object[] { 2, -1, 1, 1 };
        }

        [Theory]
        [InlineData( double.NegativeInfinity, double.NaN,              0.0)]
        [InlineData(-3.1415926535897932,      double.NaN,              0.0)]                               //                               value: -(pi)
        [InlineData(-2.7182818284590452,      double.NaN,              0.0)]                               //                               value: -(e)
        [InlineData(-1.4142135623730950,      double.NaN,              0.0)]                               //                               value: -(sqrt(2))
        [InlineData(-1.0,                     double.NaN,              0.0)]
        [InlineData(-0.69314718055994531,     double.NaN,              0.0)]                               //                               value: -(ln(2))
        [InlineData(-0.43429448190325183,     double.NaN,              0.0)]                               //                               value: -(log10(e))
        [InlineData(-0.0,                     double.NaN,              0.0)]
        [InlineData( double.NaN,              double.NaN,              0.0)]
        [InlineData( 0.0,                     double.NaN,              0.0)]
        [InlineData( 1.0,                     0.0,                     CrossPlatformMachineEpsilon)]
        [InlineData( 1.0510897883672876,      0.31830988618379067,     CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData( 1.0957974645564909,      0.43429448190325183,     CrossPlatformMachineEpsilon)]       // expected:  (log10(e))
        [InlineData( 1.2095794864199787,      0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData( 1.25,                    0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData( 1.2605918365213561,      0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData( 1.3246090892520058,      0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected:  (pi / 4)
        [InlineData( 1.5430806348152438,      1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.7071001431069344,      1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 2.1781835566085709,      1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData( 2.2341880974508023,      1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData( 2.5091784786580568,      1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        [InlineData( 5.05,                    2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected:  (ln(10))
        [InlineData( 7.6101251386622884,      2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected:  (e)
        [InlineData( 11.591953275521521,      3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi)
        [InlineData( double.PositiveInfinity, double.PositiveInfinity, 0.0)]
        public static void Acosh(double value, double expectedResult, double allowedVariance)
        {
            AssertEqual(expectedResult, Math.Acosh(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NegativeInfinity, 0.0)]
        [InlineData(-11.548739357257748,      -3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]  // expected: -(pi)
        [InlineData(-7.5441371028169758,      -2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected: -(e)
        [InlineData(-4.95,                    -2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected: -(ln(10))
        [InlineData(-2.3012989023072949,      -1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected: -(pi / 2)
        [InlineData(-1.9978980091062796,      -1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected: -(log2(e))
        [InlineData(-1.9350668221743567,      -1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected: -(sqrt(2))
        [InlineData(-1.3835428792038633,      -1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-1.1752011936438015,      -1,                       CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.86867096148600961,     -0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected: -(pi / 4)
        [InlineData(-0.76752314512611633,     -0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.75,                    -0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected: -(ln(2))
        [InlineData(-0.68050167815224332,     -0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected: -(2 / pi)
        [InlineData(-0.44807597941469025,     -0.43429448190325183,     CrossPlatformMachineEpsilon)]       // expected: -(log10(e))
        [InlineData(-0.32371243907207108,     -0.31830988618379067,     CrossPlatformMachineEpsilon)]       // expected: -(1 / pi)
        [InlineData(-0.0,                     -0.0,                     0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData( 0.0,                      0.0,                     0.0)]
        [InlineData( 0.32371243907207108,      0.31830988618379067,     CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData( 0.44807597941469025,      0.43429448190325183,     CrossPlatformMachineEpsilon)]       // expected:  (log10(e))
        [InlineData( 0.68050167815224332,      0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData( 0.75,                     0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData( 0.76752314512611633,      0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData( 0.86867096148600961,      0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected:  (pi / 4)
        [InlineData( 1.1752011936438015,       1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.3835428792038633,       1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 1.9350668221743567,       1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData( 1.9978980091062796,       1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData( 2.3012989023072949,       1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        [InlineData( 4.95,                     2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected:  (ln(10))
        [InlineData( 7.5441371028169758,       2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected:  (e)
        [InlineData( 11.548739357257748,       3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void Asinh(double value, double expectedResult, double allowedVariance)
        {
            AssertEqual(expectedResult, Math.Asinh(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NaN,              0.0)]
        [InlineData(-3.1415926535897932,       double.NaN,              0.0)]                               //                              value: -(pi)
        [InlineData(-2.7182818284590452,       double.NaN,              0.0)]                               //                              value: -(e)
        [InlineData(-1.4142135623730950,       double.NaN,              0.0)]                               //                              value: -(sqrt(2))
        [InlineData(-1.0,                      double.NegativeInfinity, CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.99627207622074994,     -3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]  // expected: -(pi)
        [InlineData(-0.99132891580059984,     -2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected: -(e)
        [InlineData(-0.98019801980198020,     -2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected: -(ln(10))
        [InlineData(-0.91715233566727435,     -1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected: -(pi / 2)
        [InlineData(-0.89423894585503855,     -1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected: -(log2(e))
        [InlineData(-0.88838556158566054,     -1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected: -(sqrt(2))
        [InlineData(-0.81046380599898809,     -1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData(-0.76159415595576489,     -1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.65579420263267244,     -0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected: -(pi / 4)
        [InlineData(-0.60885936501391381,     -0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected: -(1 / sqrt(2))
        [InlineData(-0.6,                     -0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected: -(ln(2))
        [InlineData(-0.56259360033158334,     -0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected: -(2 / pi)
        [InlineData(-0.40890401183401433,     -0.43429448190325183,     CrossPlatformMachineEpsilon)]       // expected: -(log10(e))
        [InlineData(-0.30797791269089433,     -0.31830988618379067,     CrossPlatformMachineEpsilon)]       // expected: -(1 / pi)
        [InlineData(-0.0,                     -0.0,                     0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData( 0.0,                      0.0,                     0.0)]
        [InlineData( 0.30797791269089433,      0.31830988618379067,     CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData( 0.40890401183401433,      0.43429448190325183,     CrossPlatformMachineEpsilon)]       // expected:  (log10(e))
        [InlineData( 0.56259360033158334,      0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData( 0.6,                      0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData( 0.60885936501391381,      0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData( 0.65579420263267244,      0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected:  (pi / 4)
        [InlineData( 0.76159415595576489,      1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.81046380599898809,      1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 0.88838556158566054,      1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData( 0.89423894585503855,      1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData( 0.91715233566727435,      1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        [InlineData( 0.98019801980198020,      2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected:  (ln(10))
        [InlineData( 0.99132891580059984,      2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected:  (e)
        [InlineData( 0.99627207622074994,      3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi)
        [InlineData( 1.0,                      double.PositiveInfinity, 0.0)]
        [InlineData( 1.4142135623730950,       double.NaN,              0.0)]                               //                              value:  (sqrt(2))
        [InlineData( 2.7182818284590452,       double.NaN,              0.0)]                               //                              value:  (e)
        [InlineData( 3.1415926535897932,       double.NaN,              0.0)]                               //                              value:  (pi)
        [InlineData( double.PositiveInfinity,  double.NaN,              0.0)]
        public static void Atanh(double value, double expectedResult, double allowedVariance)
        {
            AssertEqual(expectedResult, Math.Atanh(value), allowedVariance);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NegativeInfinity)]
        [InlineData(-3.1415926535897932,      -3.1415926535897936)]     // value: -(pi)
        [InlineData(-2.7182818284590452,      -2.7182818284590455)]     // value: -(e)
        [InlineData(-2.3025850929940457,      -2.3025850929940463)]     // value: -(ln(10))
        [InlineData(-1.5707963267948966,      -1.5707963267948968)]     // value: -(pi / 2)
        [InlineData(-1.4426950408889634,      -1.4426950408889636)]     // value: -(log2(e))
        [InlineData(-1.4142135623730950,      -1.4142135623730951)]     // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,      -1.1283791670955128)]     // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                     -1.0000000000000002)]
        [InlineData(-0.78539816339744831,     -0.78539816339744839)]    // value: -(pi / 4)
        [InlineData(-0.70710678118654752,     -0.70710678118654768)]    // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,     -0.69314718055994540)]    // value: -(ln(2))
        [InlineData(-0.63661977236758134,     -0.63661977236758149)]    // value: -(2 / pi)
        [InlineData(-0.43429448190325183,     -0.43429448190325187)]    // value: -(log10(e))
        [InlineData(-0.31830988618379067,     -0.31830988618379075)]    // value: -(1 / pi)
        [InlineData(-0.0,                     -double.Epsilon)]
        [InlineData( double.NaN,               double.NaN)]
        [InlineData( 0.0,                     -double.Epsilon)]
        [InlineData( 0.31830988618379067,      0.31830988618379064)]    // value:  (1 / pi)
        [InlineData( 0.43429448190325183,      0.43429448190325176)]    // value:  (log10(e))
        [InlineData( 0.63661977236758134,      0.63661977236758127)]    // value:  (2 / pi)
        [InlineData( 0.69314718055994531,      0.69314718055994518)]    // value:  (ln(2))
        [InlineData( 0.70710678118654752,      0.70710678118654746)]    // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,      0.78539816339744817)]    // value:  (pi / 4)
        [InlineData( 1.0,                      0.99999999999999989)]
        [InlineData( 1.1283791670955126,       1.1283791670955123)]     // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,       1.4142135623730947)]     // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,       1.4426950408889632)]     // value:  (log2(e))
        [InlineData( 1.5707963267948966,       1.5707963267948963)]     // value:  (pi / 2)
        [InlineData( 2.3025850929940457,       2.3025850929940455)]     // value:  (ln(10))
        [InlineData( 2.7182818284590452,       2.7182818284590446)]     // value:  (e)
        [InlineData( 3.1415926535897932,       3.1415926535897927)]     // value:  (pi)
        [InlineData( double.PositiveInfinity,  double.MaxValue)]
        public static void BitDecrement(double value, double expectedResult)
        {
            AssertEqual(expectedResult, Math.BitDecrement(value), 0.0);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.MinValue)]
        [InlineData(-3.1415926535897932,      -3.1415926535897927)]     // value: -(pi)
        [InlineData(-2.7182818284590452,      -2.7182818284590446)]     // value: -(e)
        [InlineData(-2.3025850929940457,      -2.3025850929940455)]     // value: -(ln(10))
        [InlineData(-1.5707963267948966,      -1.5707963267948963)]     // value: -(pi / 2)
        [InlineData(-1.4426950408889634,      -1.4426950408889632)]     // value: -(log2(e))
        [InlineData(-1.4142135623730950,      -1.4142135623730947)]     // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,      -1.1283791670955123)]     // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                     -0.99999999999999989)]
        [InlineData(-0.78539816339744831,     -0.78539816339744817)]    // value: -(pi / 4)
        [InlineData(-0.70710678118654752,     -0.70710678118654746)]    // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,     -0.69314718055994518)]    // value: -(ln(2))
        [InlineData(-0.63661977236758134,     -0.63661977236758127)]    // value: -(2 / pi)
        [InlineData(-0.43429448190325183,     -0.43429448190325176)]    // value: -(log10(e))
        [InlineData(-0.31830988618379067,     -0.31830988618379064)]    // value: -(1 / pi)
        [InlineData(-0.0,                      double.Epsilon)]
        [InlineData( double.NaN,               double.NaN)]
        [InlineData( 0.0,                      double.Epsilon)]
        [InlineData( 0.31830988618379067,      0.31830988618379075)]    // value:  (1 / pi)
        [InlineData( 0.43429448190325183,      0.43429448190325187)]    // value:  (log10(e))
        [InlineData( 0.63661977236758134,      0.63661977236758149)]    // value:  (2 / pi)
        [InlineData( 0.69314718055994531,      0.69314718055994540)]    // value:  (ln(2))
        [InlineData( 0.70710678118654752,      0.70710678118654768)]    // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,      0.78539816339744839)]    // value:  (pi / 4)
        [InlineData( 1.0,                      1.0000000000000002 )]
        [InlineData( 1.1283791670955126,       1.1283791670955128 )]     // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,       1.4142135623730951 )]     // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,       1.4426950408889636 )]     // value:  (log2(e))
        [InlineData( 1.5707963267948966,       1.5707963267948968 )]     // value:  (pi / 2)
        [InlineData( 2.3025850929940457,       2.3025850929940463 )]     // value:  (ln(10))
        [InlineData( 2.7182818284590452,       2.7182818284590455 )]     // value:  (e)
        [InlineData( 3.1415926535897932,       3.1415926535897936 )]     // value:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity)]
        public static void BitIncrement(double value, double expectedResult)
        {
            AssertEqual(expectedResult, Math.BitIncrement(value), 0.0);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NegativeInfinity, 0.0)]
        [InlineData(-3.1415926535897932,      -1.4645918875615233,      CrossPlatformMachineEpsilon * 10)]   // value: -(pi)
        [InlineData(-2.7182818284590452,      -1.3956124250860895,      CrossPlatformMachineEpsilon * 10)]   // value: -(e)
        [InlineData(-2.3025850929940457,      -1.3205004784536852,      CrossPlatformMachineEpsilon * 10)]   // value: -(ln(10))
        [InlineData(-1.5707963267948966,      -1.1624473515096265,      CrossPlatformMachineEpsilon * 10)]   // value: -(pi / 2)
        [InlineData(-1.4426950408889634,      -1.1299472763373901,      CrossPlatformMachineEpsilon * 10)]   // value: -(log2(e))
        [InlineData(-1.4142135623730950,      -1.1224620483093730,      CrossPlatformMachineEpsilon * 10)]   // value: -(sqrt(2))
        [InlineData(-1.1283791670955126,      -1.0410821966965807,      CrossPlatformMachineEpsilon * 10)]   // value: -(2 / sqrt(pi))
        [InlineData(-1.0,                     -1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData(-0.78539816339744831,     -0.92263507432201421,     CrossPlatformMachineEpsilon)]        // value: -(pi / 4)
        [InlineData(-0.70710678118654752,     -0.89089871814033930,     CrossPlatformMachineEpsilon)]        // value: -(1 / sqrt(2))
        [InlineData(-0.69314718055994531,     -0.88499704450051772,     CrossPlatformMachineEpsilon)]        // value: -(ln(2))
        [InlineData(-0.63661977236758134,     -0.86025401382809963,     CrossPlatformMachineEpsilon)]        // value: -(2 / pi)
        [InlineData(-0.43429448190325183,     -0.75728863133090766,     CrossPlatformMachineEpsilon)]        // value: -(log10(e))
        [InlineData(-0.31830988618379067,     -0.68278406325529568,     CrossPlatformMachineEpsilon)]        // value: -(1 / pi)
        [InlineData(-0.0,                     -0.0,                     0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData( 0.0,                      0.0,                     0.0)]
        [InlineData( 0.31830988618379067,      0.68278406325529568,     CrossPlatformMachineEpsilon)]        // value:  (1 / pi)
        [InlineData( 0.43429448190325183,      0.75728863133090766,     CrossPlatformMachineEpsilon)]        // value:  (log10(e))
        [InlineData( 0.63661977236758134,      0.86025401382809963,     CrossPlatformMachineEpsilon)]        // value:  (2 / pi)
        [InlineData( 0.69314718055994531,      0.88499704450051772,     CrossPlatformMachineEpsilon)]        // value:  (ln(2))
        [InlineData( 0.70710678118654752,      0.89089871814033930,     CrossPlatformMachineEpsilon)]        // value:  (1 / sqrt(2))
        [InlineData( 0.78539816339744831,      0.92263507432201421,     CrossPlatformMachineEpsilon)]        // value:  (pi / 4)
        [InlineData( 1.0,                      1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.1283791670955126,       1.0410821966965807,      CrossPlatformMachineEpsilon * 10)]   // value:  (2 / sqrt(pi))
        [InlineData( 1.4142135623730950,       1.1224620483093730,      CrossPlatformMachineEpsilon * 10)]   // value:  (sqrt(2))
        [InlineData( 1.4426950408889634,       1.1299472763373901,      CrossPlatformMachineEpsilon * 10)]   // value:  (log2(e))
        [InlineData( 1.5707963267948966,       1.1624473515096265,      CrossPlatformMachineEpsilon * 10)]   // value:  (pi / 2)
        [InlineData( 2.3025850929940457,       1.3205004784536852,      CrossPlatformMachineEpsilon * 10)]   // value:  (ln(10))
        [InlineData( 2.7182818284590452,       1.3956124250860895,      CrossPlatformMachineEpsilon * 10)]   // value:  (e)
        [InlineData( 3.1415926535897932,       1.4645918875615233,      CrossPlatformMachineEpsilon * 10)]   // value:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void Cbrt(double value, double expectedResult, double allowedVariance)
        {
            AssertEqual(expectedResult, Math.Cbrt(value), allowedVariance);
        }

        [Theory]
        [MemberData(nameof(Clamp_SignedInt_TestData))]
        public static void Clamp_SByte(sbyte value, sbyte min, sbyte max, sbyte expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_UnsignedInt_TestData))]
        public static void Clamp_Byte(byte value, byte min, byte max, byte expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_SignedInt_TestData))]
        public static void Clamp_Short(short value, short min, short max, short expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_UnsignedInt_TestData))]
        public static void Clamp_UShort(ushort value, ushort min, ushort max, ushort expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_SignedInt_TestData))]
        public static void Clamp_Int(int value, int min, int max, int expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_UnsignedInt_TestData))]
        public static void Clamp_UInt(uint value, uint min, uint max, uint expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_SignedInt_TestData))]
        public static void Clamp_Long(long value, long min, long max, long expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_UnsignedInt_TestData))]
        public static void Clamp_ULong(ulong value, ulong min, ulong max, ulong expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_SignedInt_TestData))]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity, double.NegativeInfinity)]
        [InlineData(1, double.NegativeInfinity, double.PositiveInfinity, 1)]
        [InlineData(double.PositiveInfinity, double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(1, double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(1, double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN, double.NaN, double.NaN)]
        [InlineData(double.NaN, double.NaN, 1, double.NaN)]
        [InlineData(double.NaN, 1, double.NaN, double.NaN)]
        [InlineData(double.NaN, 1, 1, double.NaN)]
        [InlineData(1, double.NaN, double.NaN, 1)]
        [InlineData(1, double.NaN, 1, 1)]
        [InlineData(1, 1, double.NaN, 1)]
        public static void Clamp_Double(double value, double min, double max, double expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_SignedInt_TestData))]
        [InlineData(float.NegativeInfinity, float.NegativeInfinity, float.PositiveInfinity, float.NegativeInfinity)]
        [InlineData(1, float.NegativeInfinity, float.PositiveInfinity, 1)]
        [InlineData(float.PositiveInfinity, float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(1, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(1, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity)]
        [InlineData(float.NaN, float.NaN, float.NaN, float.NaN)]
        [InlineData(float.NaN, float.NaN, 1, float.NaN)]
        [InlineData(float.NaN, 1, float.NaN, float.NaN)]
        [InlineData(float.NaN, 1, 1, float.NaN)]
        [InlineData(1, float.NaN, float.NaN, 1)]
        [InlineData(1, float.NaN, 1, 1)]
        [InlineData(1, 1, float.NaN, 1)]
        public static void Clamp_Float(float value, float min, float max, float expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Theory]
        [MemberData(nameof(Clamp_SignedInt_TestData))]
        public static void Clamp_Decimal(decimal value, decimal min, decimal max, decimal expected)
        {
            Assert.Equal(expected, Math.Clamp(value, min, max));
        }

        [Fact]
        public static void Clamp_MinGreaterThanMax_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((sbyte)1, (sbyte)2, (sbyte)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((byte)1, (byte)2, (byte)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((short)1, (short)2, (short)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((ushort)1, (ushort)2, (ushort)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((int)1, (int)2, (int)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((uint)1, (uint)2, (uint)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((long)1, (long)2, (long)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((ulong)1, (ulong)2, (ulong)1));

            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((float)1, (float)2, (float)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((double)1, (double)2, (double)1));
            AssertExtensions.Throws<ArgumentException>(null, () => Math.Clamp((decimal)1, (decimal)2, (decimal)1));
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NegativeInfinity,  double.NegativeInfinity)]
        [InlineData( double.NegativeInfinity, -3.1415926535897932,       double.NegativeInfinity)]
        [InlineData( double.NegativeInfinity, -0.0,                      double.NegativeInfinity)]
        [InlineData( double.NegativeInfinity,  double.NaN,               double.NegativeInfinity)]
        [InlineData( double.NegativeInfinity,  0.0,                      double.PositiveInfinity)]
        [InlineData( double.NegativeInfinity,  3.1415926535897932,       double.PositiveInfinity)]
        [InlineData( double.NegativeInfinity,  double.PositiveInfinity,  double.PositiveInfinity)]
        [InlineData(-3.1415926535897932,       double.NegativeInfinity, -3.1415926535897932)]
        [InlineData(-3.1415926535897932,      -3.1415926535897932,      -3.1415926535897932)]
        [InlineData(-3.1415926535897932,      -0.0,                     -3.1415926535897932)]
        [InlineData(-3.1415926535897932,       double.NaN,              -3.1415926535897932)]
        [InlineData(-3.1415926535897932,       0.0,                      3.1415926535897932)]
        [InlineData(-3.1415926535897932,       3.1415926535897932,       3.1415926535897932)]
        [InlineData(-3.1415926535897932,       double.PositiveInfinity,  3.1415926535897932)]
        [InlineData(-0.0,                      double.NegativeInfinity, -0.0)]
        [InlineData(-0.0,                     -3.1415926535897932,      -0.0)]
        [InlineData(-0.0,                     -0.0,                     -0.0)]
        [InlineData(-0.0,                      double.NaN,              -0.0)]
        [InlineData(-0.0,                      0.0,                      0.0)]
        [InlineData(-0.0,                      3.1415926535897932,       0.0)]
        [InlineData(-0.0,                      double.PositiveInfinity,  0.0)]
        [InlineData( double.NaN,               double.NegativeInfinity,  double.NaN)]
        [InlineData( double.NaN,              -3.1415926535897932,       double.NaN)]
        [InlineData( double.NaN,              -0.0,                      double.NaN)]
        [InlineData( double.NaN,               double.NaN,               double.NaN)]
        [InlineData( double.NaN,               0.0,                      double.NaN)]
        [InlineData( double.NaN,               3.1415926535897932,       double.NaN)]
        [InlineData( double.NaN,               double.PositiveInfinity,  double.NaN)]
        [InlineData( 0.0,                      double.NegativeInfinity, -0.0)]
        [InlineData( 0.0,                     -3.1415926535897932,      -0.0)]
        [InlineData( 0.0,                     -0.0,                     -0.0)]
        [InlineData( 0.0,                      double.NaN,              -0.0)]
        [InlineData( 0.0,                      0.0,                      0.0)]
        [InlineData( 0.0,                      3.1415926535897932,       0.0)]
        [InlineData( 0.0,                      double.PositiveInfinity,  0.0)]
        [InlineData( 3.1415926535897932,       double.NegativeInfinity, -3.1415926535897932)]
        [InlineData( 3.1415926535897932,      -3.1415926535897932,      -3.1415926535897932)]
        [InlineData( 3.1415926535897932,      -0.0,                     -3.1415926535897932)]
        [InlineData( 3.1415926535897932,       double.NaN,              -3.1415926535897932)]
        [InlineData( 3.1415926535897932,       0.0,                      3.1415926535897932)]
        [InlineData( 3.1415926535897932,       3.1415926535897932,       3.1415926535897932)]
        [InlineData( 3.1415926535897932,       double.PositiveInfinity,  3.1415926535897932)]
        [InlineData( double.PositiveInfinity,  double.NegativeInfinity,  double.NegativeInfinity)]
        [InlineData( double.PositiveInfinity, -3.1415926535897932,       double.NegativeInfinity)]
        [InlineData( double.PositiveInfinity, -0.0,                      double.NegativeInfinity)]
        [InlineData( double.PositiveInfinity,  double.NaN,               double.NegativeInfinity)]
        [InlineData( double.PositiveInfinity,  0.0,                      double.PositiveInfinity)]
        [InlineData( double.PositiveInfinity,  3.1415926535897932,       double.PositiveInfinity)]
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity,  double.PositiveInfinity)]
        public static void CopySign(double x, double y, double expectedResult)
        {
            AssertEqual(expectedResult, Math.CopySign(x, y), 0.0);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NegativeInfinity,  double.NegativeInfinity,  double.NaN)]
        [InlineData( double.NegativeInfinity, -0.0,                      double.NegativeInfinity,  double.NaN)]
        [InlineData( double.NegativeInfinity, -0.0,                     -3.1415926535897932,       double.NaN)]
        [InlineData( double.NegativeInfinity, -0.0,                     -0.0,                      double.NaN)]
        [InlineData( double.NegativeInfinity, -0.0,                      double.NaN,               double.NaN)]
        [InlineData( double.NegativeInfinity, -0.0,                      0.0,                      double.NaN)]
        [InlineData( double.NegativeInfinity, -0.0,                      3.1415926535897932,       double.NaN)]
        [InlineData( double.NegativeInfinity, -0.0,                      double.PositiveInfinity,  double.NaN)]
        [InlineData( double.NegativeInfinity,  0.0,                      double.NegativeInfinity,  double.NaN)]
        [InlineData( double.NegativeInfinity,  0.0,                     -3.1415926535897932,       double.NaN)]
        [InlineData( double.NegativeInfinity,  0.0,                     -0.0,                      double.NaN)]
        [InlineData( double.NegativeInfinity,  0.0,                      double.NaN,               double.NaN)]
        [InlineData( double.NegativeInfinity,  0.0,                      0.0,                      double.NaN)]
        [InlineData( double.NegativeInfinity,  0.0,                      3.1415926535897932,       double.NaN)]
        [InlineData( double.NegativeInfinity,  0.0,                      double.PositiveInfinity,  double.NaN)]
        [InlineData( double.NegativeInfinity,  double.PositiveInfinity,  double.PositiveInfinity,  double.NaN)]
        [InlineData(-1e308,                    2.0,                      1e308,                   -1e308)]
        [InlineData(-1e308,                    2.0,                      double.PositiveInfinity,  double.PositiveInfinity)]
        [InlineData(-5,                        4,                       -3,                       -23)]
        [InlineData(-0.0,                      double.NegativeInfinity,  double.NegativeInfinity,  double.NaN)]
        [InlineData(-0.0,                      double.NegativeInfinity, -3.1415926535897932,       double.NaN)]
        [InlineData(-0.0,                      double.NegativeInfinity, -0.0,                      double.NaN)]
        [InlineData(-0.0,                      double.NegativeInfinity,  double.NaN,               double.NaN)]
        [InlineData(-0.0,                      double.NegativeInfinity,  0.0,                      double.NaN)]
        [InlineData(-0.0,                      double.NegativeInfinity,  3.1415926535897932,       double.NaN)]
        [InlineData(-0.0,                      double.NegativeInfinity,  double.PositiveInfinity,  double.NaN)]
        [InlineData(-0.0,                      double.PositiveInfinity,  double.NegativeInfinity,  double.NaN)]
        [InlineData(-0.0,                      double.PositiveInfinity, -3.1415926535897932,       double.NaN)]
        [InlineData(-0.0,                      double.PositiveInfinity, -0.0,                      double.NaN)]
        [InlineData(-0.0,                      double.PositiveInfinity,  double.NaN,               double.NaN)]
        [InlineData(-0.0,                      double.PositiveInfinity,  0.0,                      double.NaN)]
        [InlineData(-0.0,                      double.PositiveInfinity,  3.1415926535897932,       double.NaN)]
        [InlineData(-0.0,                      double.PositiveInfinity,  double.PositiveInfinity,  double.NaN)]
        [InlineData( 0.0,                      double.NegativeInfinity,  double.NegativeInfinity,  double.NaN)]
        [InlineData( 0.0,                      double.NegativeInfinity, -3.1415926535897932,       double.NaN)]
        [InlineData( 0.0,                      double.NegativeInfinity, -0.0,                      double.NaN)]
        [InlineData( 0.0,                      double.NegativeInfinity,  double.NaN,               double.NaN)]
        [InlineData( 0.0,                      double.NegativeInfinity,  0.0,                      double.NaN)]
        [InlineData( 0.0,                      double.NegativeInfinity,  3.1415926535897932,       double.NaN)]
        [InlineData( 0.0,                      double.NegativeInfinity,  double.PositiveInfinity,  double.NaN)]
        [InlineData( 0.0,                      double.PositiveInfinity,  double.NegativeInfinity,  double.NaN)]
        [InlineData( 0.0,                      double.PositiveInfinity, -3.1415926535897932,       double.NaN)]
        [InlineData( 0.0,                      double.PositiveInfinity, -0.0,                      double.NaN)]
        [InlineData( 0.0,                      double.PositiveInfinity,  double.NaN,               double.NaN)]
        [InlineData( 0.0,                      double.PositiveInfinity,  0.0,                      double.NaN)]
        [InlineData( 0.0,                      double.PositiveInfinity,  3.1415926535897932,       double.NaN)]
        [InlineData( 0.0,                      double.PositiveInfinity,  double.PositiveInfinity,  double.NaN)]
        [InlineData( 5,                        4,                        3,                        23)]
        [InlineData( 1e308,                    2.0,                     -1e308,                    1e308)]
        [InlineData( 1e308,                    2.0,                      double.NegativeInfinity,  double.NegativeInfinity)]
        [InlineData( double.PositiveInfinity,  double.NegativeInfinity,  double.PositiveInfinity,  double.NaN)]
        [InlineData( double.PositiveInfinity, -0.0,                      double.NegativeInfinity,  double.NaN)]
        [InlineData( double.PositiveInfinity, -0.0,                     -3.1415926535897932,       double.NaN)]
        [InlineData( double.PositiveInfinity, -0.0,                     -0.0,                      double.NaN)]
        [InlineData( double.PositiveInfinity, -0.0,                      double.NaN,               double.NaN)]
        [InlineData( double.PositiveInfinity, -0.0,                      0.0,                      double.NaN)]
        [InlineData( double.PositiveInfinity, -0.0,                      3.1415926535897932,       double.NaN)]
        [InlineData( double.PositiveInfinity, -0.0,                      double.PositiveInfinity,  double.NaN)]
        [InlineData( double.PositiveInfinity,  0.0,                      double.NegativeInfinity,  double.NaN)]
        [InlineData( double.PositiveInfinity,  0.0,                     -3.1415926535897932,       double.NaN)]
        [InlineData( double.PositiveInfinity,  0.0,                     -0.0,                      double.NaN)]
        [InlineData( double.PositiveInfinity,  0.0,                      double.NaN,               double.NaN)]
        [InlineData( double.PositiveInfinity,  0.0,                      0.0,                      double.NaN)]
        [InlineData( double.PositiveInfinity,  0.0,                      3.1415926535897932,       double.NaN)]
        [InlineData( double.PositiveInfinity,  0.0,                      double.PositiveInfinity,  double.NaN)]
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity,  double.NegativeInfinity,  double.NaN)]
        public static void FusedMultiplyAdd(double x, double y, double z, double expectedResult)
        {
            AssertEqual(expectedResult, Math.FusedMultiplyAdd(x, y, z), 0.0);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  unchecked((int)(0x7FFFFFFF)))]
        [InlineData(-0.0,                      unchecked((int)(0x80000000)))]
        [InlineData( double.NaN,               unchecked((int)(0x7FFFFFFF)))]
        [InlineData( 0.0,                      unchecked((int)(0x80000000)))]
        [InlineData( 0.11331473229676087,     -4)]
        [InlineData( 0.15195522325791297,     -3)]
        [InlineData( 0.20269956628651730,     -3)]
        [InlineData( 0.33662253682241906,     -2)]
        [InlineData( 0.36787944117144232,     -2)]
        [InlineData( 0.37521422724648177,     -2)]
        [InlineData( 0.45742934732229695,     -2)]
        [InlineData( 0.5,                     -1)]
        [InlineData( 0.58019181037172444,     -1)]
        [InlineData( 0.61254732653606592,     -1)]
        [InlineData( 0.61850313780157598,     -1)]
        [InlineData( 0.64321824193300488,     -1)]
        [InlineData( 0.74005557395545179,     -1)]
        [InlineData( 0.80200887896145195,     -1)]
        [InlineData( 1,                        0)]
        [InlineData( 1.2468689889006383,       0)]
        [InlineData( 1.3512498725672678,       0)]
        [InlineData( 1.5546822754821001,       0)]
        [InlineData( 1.6168066722416747,       0)]
        [InlineData( 1.6325269194381528,       0)]
        [InlineData( 1.7235679341273495,       0)]
        [InlineData( 2,                        1)]
        [InlineData( 2.1861299583286618,       1)]
        [InlineData( 2.6651441426902252,       1)]
        [InlineData( 2.7182818284590452,       1)]
        [InlineData( 2.9706864235520193,       1)]
        [InlineData( 4.9334096679145963,       2)]
        [InlineData( 6.5808859910179210,       2)]
        [InlineData( 8.8249778270762876,       3)]
        [InlineData( double.PositiveInfinity,  unchecked((int)(0x7FFFFFFF)))]
        public static void ILogB(double value, int expectedResult)
        {
            Assert.Equal(expectedResult, Math.ILogB(value));
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  double.NaN,              0.0)]
        [InlineData(-0.11331473229676087,      double.NaN,              0.0)]
        [InlineData(-0.0,                      double.NegativeInfinity, 0.0)]
        [InlineData( double.NaN,               double.NaN,              0.0)]
        [InlineData( 0.0,                      double.NegativeInfinity, 0.0)]
        [InlineData( 0.11331473229676087,     -3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]  // expected: -(pi)
        [InlineData( 0.15195522325791297,     -2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected: -(e)
        [InlineData( 0.20269956628651730,     -2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected: -(ln(10))
        [InlineData( 0.33662253682241906,     -1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected: -(pi / 2)
        [InlineData( 0.36787944117144232,     -1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected: -(log2(e))
        [InlineData( 0.37521422724648177,     -1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected: -(sqrt(2))
        [InlineData( 0.45742934732229695,     -1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected: -(2 / sqrt(pi))
        [InlineData( 0.5,                     -1.0,                     CrossPlatformMachineEpsilon * 10)]
        [InlineData( 0.58019181037172444,     -0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected: -(pi / 4)
        [InlineData( 0.61254732653606592,     -0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected: -(1 / sqrt(2))
        [InlineData( 0.61850313780157598,     -0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected: -(ln(2))
        [InlineData( 0.64321824193300488,     -0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected: -(2 / pi)
        [InlineData( 0.74005557395545179,     -0.43429448190325183,     CrossPlatformMachineEpsilon)]       // expected: -(log10(e))
        [InlineData( 0.80200887896145195,     -0.31830988618379067,     CrossPlatformMachineEpsilon)]       // expected: -(1 / pi)
        [InlineData( 1,                        0.0,                     0.0)]
        [InlineData( 1.2468689889006383,       0.31830988618379067,     CrossPlatformMachineEpsilon)]       // expected:  (1 / pi)
        [InlineData( 1.3512498725672678,       0.43429448190325183,     CrossPlatformMachineEpsilon)]       // expected:  (log10(e))
        [InlineData( 1.5546822754821001,       0.63661977236758134,     CrossPlatformMachineEpsilon)]       // expected:  (2 / pi)
        [InlineData( 1.6168066722416747,       0.69314718055994531,     CrossPlatformMachineEpsilon)]       // expected:  (ln(2))
        [InlineData( 1.6325269194381528,       0.70710678118654752,     CrossPlatformMachineEpsilon)]       // expected:  (1 / sqrt(2))
        [InlineData( 1.7235679341273495,       0.78539816339744831,     CrossPlatformMachineEpsilon)]       // expected:  (pi / 4)
        [InlineData( 2,                        1.0,                     CrossPlatformMachineEpsilon * 10)]  //                              value: (e)
        [InlineData( 2.1861299583286618,       1.1283791670955126,      CrossPlatformMachineEpsilon * 10)]  // expected:  (2 / sqrt(pi))
        [InlineData( 2.6651441426902252,       1.4142135623730950,      CrossPlatformMachineEpsilon * 10)]  // expected:  (sqrt(2))
        [InlineData( 2.7182818284590452,       1.4426950408889634,      CrossPlatformMachineEpsilon * 10)]  // expected:  (log2(e))
        [InlineData( 2.9706864235520193,       1.5707963267948966,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi / 2)
        [InlineData( 4.9334096679145963,       2.3025850929940457,      CrossPlatformMachineEpsilon * 10)]  // expected:  (ln(10))
        [InlineData( 6.5808859910179210,       2.7182818284590452,      CrossPlatformMachineEpsilon * 10)]  // expected:  (e)
        [InlineData( 8.8249778270762876,       3.1415926535897932,      CrossPlatformMachineEpsilon * 10)]  // expected:  (pi)
        [InlineData( double.PositiveInfinity,  double.PositiveInfinity, 0.0)]
        public static void Log2(double value, double expectedResult, double allowedVariance)
        {
            AssertEqual(expectedResult, Math.Log2(value), allowedVariance);
        }

        [Theory]
        [InlineData(double.NegativeInfinity, double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.MinValue, double.MaxValue, double.MaxValue)]
        [InlineData(double.NaN, double.NaN, double.NaN)]
        [InlineData(-0.0, 0.0, 0.0)]
        [InlineData(2.0, -3.0, -3.0)]
        [InlineData(3.0, -2.0, 3.0)]
        [InlineData(double.PositiveInfinity, double.NaN, double.NaN)]
        public static void MaxMagnitude(double x, double y, double expectedResult)
        {
            AssertEqual(expectedResult, Math.MaxMagnitude(x, y), 0.0);
        }
        
        [Theory]
        [InlineData(double.NegativeInfinity, double.PositiveInfinity, double.NegativeInfinity)]
        [InlineData(double.MinValue, double.MaxValue, double.MinValue)]
        [InlineData(double.NaN, double.NaN, double.NaN)]
        [InlineData(-0.0, 0.0, -0.0)]
        [InlineData(2.0, -3.0, 2.0)]
        [InlineData(3.0, -2.0, -2.0)]
        [InlineData(double.PositiveInfinity, double.NaN, double.NaN)]
        public static void MinMagnitude(double x, double y, double expectedResult)
        {
            AssertEqual(expectedResult, Math.MinMagnitude(x, y), 0.0);
        }

        [Theory]
        [InlineData( double.NegativeInfinity,  unchecked((int)(0x7FFFFFFF)),  double.NegativeInfinity,  0)]
        [InlineData(-0.11331473229676087,     -3,                            -0.014164341537095108,     CrossPlatformMachineEpsilon / 10)]
        [InlineData(-0.0,                      unchecked((int)(0x80000000)), -0.0,                      0)]
        [InlineData( double.NaN,               unchecked((int)(0x7FFFFFFF)),  double.NaN,               0)]
        [InlineData( 0.0,                      unchecked((int)(0x80000000)),  0,                        0)]
        [InlineData( 0.11331473229676087,     -4,                             0.0070821707685475542,    CrossPlatformMachineEpsilon / 100)]
        [InlineData( 0.15195522325791297,     -3,                             0.018994402907239121,     CrossPlatformMachineEpsilon / 10)]
        [InlineData( 0.20269956628651730,     -3,                             0.025337445785814663,     CrossPlatformMachineEpsilon / 10)]
        [InlineData( 0.33662253682241906,     -2,                             0.084155634205604762,     CrossPlatformMachineEpsilon / 10)]
        [InlineData( 0.36787944117144232,     -2,                             0.091969860292860584,     CrossPlatformMachineEpsilon / 10)]
        [InlineData( 0.37521422724648177,     -2,                             0.093803556811620448,     CrossPlatformMachineEpsilon / 10)]
        [InlineData( 0.45742934732229695,     -2,                             0.11435733683057424,      CrossPlatformMachineEpsilon)]
        [InlineData( 0.5,                     -1,                             0.25,                     CrossPlatformMachineEpsilon)]
        [InlineData( 0.58019181037172444,     -1,                             0.2900959051858622,       CrossPlatformMachineEpsilon)]
        [InlineData( 0.61254732653606592,     -1,                             0.30627366326803296,      CrossPlatformMachineEpsilon)]
        [InlineData( 0.61850313780157598,     -1,                             0.30925156890078798,      CrossPlatformMachineEpsilon)]
        [InlineData( 0.64321824193300488,     -1,                             0.32160912096650246,      CrossPlatformMachineEpsilon)]
        [InlineData( 0.74005557395545179,     -1,                             0.37002778697772587,      CrossPlatformMachineEpsilon)]
        [InlineData( 0.80200887896145195,     -1,                             0.40100443948072595,      CrossPlatformMachineEpsilon)]
        [InlineData( 1,                        0,                             1,                        CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.2468689889006383,       0,                             1.2468689889006384,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.3512498725672678,       0,                             1.3512498725672677,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.5546822754821001,       0,                             1.5546822754821001,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.6168066722416747,       0,                             1.6168066722416747,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.6325269194381528,       0,                             1.6325269194381529,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( 1.7235679341273495,       0,                             1.7235679341273495,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2,                        1,                             4,                        CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.1861299583286618,       1,                             4.3722599166573239,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.6651441426902252,       1,                             5.3302882853804503,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.7182818284590452,       1,                             5.4365636569180902,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( 2.9706864235520193,       1,                             5.9413728471040388,       CrossPlatformMachineEpsilon * 10)]
        [InlineData( 4.9334096679145963,       2,                             19.733638671658387,       CrossPlatformMachineEpsilon * 100)]
        [InlineData( 6.5808859910179210,       2,                             26.323543964071686,       CrossPlatformMachineEpsilon * 100)]
        [InlineData( 8.8249778270762876,       3,                             70.599822616610297,       CrossPlatformMachineEpsilon * 100)]
        [InlineData( double.PositiveInfinity,  unchecked((int)(0x7FFFFFFF)),  double.PositiveInfinity,  0)]
        public static void ScaleB(double x, int n, double expectedResult, double allowedVariance)
        {
            AssertEqual(expectedResult, Math.ScaleB(x, n), allowedVariance);
        }


        public static IEnumerable<object[]> Round_Digits_TestData
        {
            get 
            {
                yield return new object[] {0, 0, 3, MidpointRounding.ToEven};
                yield return new object[] {3.42156, 3.422, 3, MidpointRounding.ToEven};
                yield return new object[] {-3.42156, -3.422, 3, MidpointRounding.ToEven};

                yield return new object[] {0, 0, 3, MidpointRounding.AwayFromZero};
                yield return new object[] {3.42156, 3.422, 3, MidpointRounding.AwayFromZero};
                yield return new object[] {-3.42156, -3.422, 3, MidpointRounding.AwayFromZero};

                yield return new object[] {0, 0, 3, MidpointRounding.ToZero};
                yield return new object[] {3.42156, 3.421, 3, MidpointRounding.ToZero};
                yield return new object[] {-3.42156, -3.421, 3, MidpointRounding.ToZero};

                yield return new object[] {0, 0, 3, MidpointRounding.ToNegativeInfinity};
                yield return new object[] {3.42156, 3.421, 3, MidpointRounding.ToNegativeInfinity};
                yield return new object[] {-3.42156, -3.422, 3, MidpointRounding.ToNegativeInfinity};

                yield return new object[] {0, 0, 3, MidpointRounding.ToPositiveInfinity};
                yield return new object[] {3.42156, 3.422, 3, MidpointRounding.ToPositiveInfinity};
                yield return new object[] {-3.42156, -3.421, 3, MidpointRounding.ToPositiveInfinity};
              }
        }

        [Theory]
        [InlineData(MidpointRounding.ToEven)]
        [InlineData(MidpointRounding.AwayFromZero)]
        [InlineData(MidpointRounding.ToZero)]
        [InlineData(MidpointRounding.ToNegativeInfinity)]
        [InlineData(MidpointRounding.ToPositiveInfinity)]
        public static void Round_Double_Digits(MidpointRounding mode)
        {
            Assert.Equal(double.NaN, Math.Round(double.NaN, 3, mode));
            Assert.Equal(double.PositiveInfinity, Math.Round(double.PositiveInfinity, 3, mode));
            Assert.Equal(double.NegativeInfinity, Math.Round(double.NegativeInfinity, 3, mode));
        }

        [Theory]
        [MemberData(nameof(Round_Digits_TestData))]
        public static void Round_Double_Digits(double x, double expected, int digits, MidpointRounding mode)
        {
            Assert.Equal(expected, Math.Round(x, digits, mode));
        }

        [Theory]
        [MemberData(nameof(Round_Digits_TestData))]
        public static void Round_Decimal_Digits(decimal x, decimal expected, int digits, MidpointRounding mode)
        {
            Assert.Equal(expected, Math.Round(x, digits, mode));
        }

        [Theory]
        [InlineData(MidpointRounding.ToEven)]
        [InlineData(MidpointRounding.AwayFromZero)]
        [InlineData(MidpointRounding.ToZero)]
        [InlineData(MidpointRounding.ToNegativeInfinity)]
        [InlineData(MidpointRounding.ToPositiveInfinity)]
        public static void Round_Decimal_Digits(MidpointRounding mode)
        {
            Assert.Equal(decimal.Zero, Math.Round(decimal.Zero, 3, mode));
        }

        public static IEnumerable<object[]> Round_Modes_TestData
        {
            get 
            {
                yield return new object[] {11, 11, MidpointRounding.ToEven};
                yield return new object[] {11.4, 11, MidpointRounding.ToEven};
                yield return new object[] {11.5, 12, MidpointRounding.ToEven};
                yield return new object[] {11.6, 12, MidpointRounding.ToEven};
                yield return new object[] {-11, -11, MidpointRounding.ToEven};
                yield return new object[] {-11.4, -11, MidpointRounding.ToEven};
                yield return new object[] {-11.5, -12, MidpointRounding.ToEven};
                yield return new object[] {-11.6, -12, MidpointRounding.ToEven};
                yield return new object[] {11, 11, MidpointRounding.AwayFromZero};
                yield return new object[] {11.4, 11, MidpointRounding.AwayFromZero};
                yield return new object[] {11.5, 12, MidpointRounding.AwayFromZero};
                yield return new object[] {11.6, 12, MidpointRounding.AwayFromZero};
                yield return new object[] {-11, -11, MidpointRounding.AwayFromZero};
                yield return new object[] {-11.4, -11, MidpointRounding.AwayFromZero};
                yield return new object[] {-11.5, -12, MidpointRounding.AwayFromZero};
                yield return new object[] {-11.6, -12, MidpointRounding.AwayFromZero};
                yield return new object[] {11, 11, MidpointRounding.ToPositiveInfinity};
                yield return new object[] {11.4, 12, MidpointRounding.ToPositiveInfinity};
                yield return new object[] {11.5, 12, MidpointRounding.ToPositiveInfinity};
                yield return new object[] {11.6, 12, MidpointRounding.ToPositiveInfinity};
                yield return new object[] {-11, -11, MidpointRounding.ToPositiveInfinity};
                yield return new object[] {-11.4, -11, MidpointRounding.ToPositiveInfinity};
                yield return new object[] {-11.5, -11, MidpointRounding.ToPositiveInfinity};
                yield return new object[] {-11.6, -11, MidpointRounding.ToPositiveInfinity};
                yield return new object[] {11.0, 11, MidpointRounding.ToNegativeInfinity};
                yield return new object[] {11.4, 11, MidpointRounding.ToNegativeInfinity};
                yield return new object[] {11.5, 11, MidpointRounding.ToNegativeInfinity};
                yield return new object[] {11.6, 11, MidpointRounding.ToNegativeInfinity};
                yield return new object[] {-11.0, -11, MidpointRounding.ToNegativeInfinity};
                yield return new object[] {-11.4, -12, MidpointRounding.ToNegativeInfinity};
                yield return new object[] {-11.5, -12, MidpointRounding.ToNegativeInfinity};
                yield return new object[] {-11.6, -12, MidpointRounding.ToNegativeInfinity};
                yield return new object[] {11.0, 11, MidpointRounding.ToZero};
                yield return new object[] {11.4, 11, MidpointRounding.ToZero};
                yield return new object[] {11.5, 11, MidpointRounding.ToZero};
                yield return new object[] {11.6, 11, MidpointRounding.ToZero};
                yield return new object[] {-11.0, -11, MidpointRounding.ToZero};
                yield return new object[] {-11.4, -11, MidpointRounding.ToZero};
                yield return new object[] {-11.5, -11, MidpointRounding.ToZero};
                yield return new object[] {-11.6, -11, MidpointRounding.ToZero};
            }
        }

        [MemberData(nameof(Round_Modes_TestData))]
        public static void Round_Double_Modes(double x, double expected, MidpointRounding mode)
        {
            Assert.Equal(expected, Math.Round(x, 0, mode));
        }

        [MemberData(nameof(Round_Modes_TestData))]
        public static void Round_Float_Modes(float x, float expected, MidpointRounding mode)
        {
            Assert.Equal(expected, MathF.Round(x, 0, mode));
        }

        [MemberData(nameof(Round_Modes_TestData))]
        public static void Round_Decimal_Modes(decimal x, decimal expected, MidpointRounding mode)
        {
            Assert.Equal(expected, Math.Round(x, 0, mode));
            Assert.Equal(expected, decimal.Round(x, 0, mode));       
        }
    }
}
