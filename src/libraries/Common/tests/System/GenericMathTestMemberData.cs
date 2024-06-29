// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Tests
{
    internal static class GenericMathTestMemberData
    {
        // binary64 (double) has a machine epsilon of 2^-52 (approx. 2.22e-16). However, this
        // is slightly too accurate when writing tests meant to run against libm implementations
        // for various platforms. 2^-50 (approx. 8.88e-16) seems to be as accurate as we can get.
        //
        // The tests themselves will take CrossPlatformMachineEpsilon and adjust it according to the expected result
        // so that the delta used for comparison will compare the most significant digits and ignore
        // any digits that are outside the double precision range (15-17 digits).
        //
        // For example, a test with an expect result in the format of 0.xxxxxxxxxxxxxxxxx will use
        // CrossPlatformMachineEpsilon for the variance, while an expected result in the format of 0.0xxxxxxxxxxxxxxxxx
        // will use CrossPlatformMachineEpsilon / 10 and expected result in the format of x.xxxxxxxxxxxxxxxx will
        // use CrossPlatformMachineEpsilon * 10.
        internal const double DoubleCrossPlatformMachineEpsilon = 8.8817841970012523e-16;

        // binary32 (float) has a machine epsilon of 2^-23 (approx. 1.19e-07). However, this
        // is slightly too accurate when writing tests meant to run against libm implementations
        // for various platforms. 2^-21 (approx. 4.76e-07) seems to be as accurate as we can get.
        //
        // The tests themselves will take CrossPlatformMachineEpsilon and adjust it according to the expected result
        // so that the delta used for comparison will compare the most significant digits and ignore
        // any digits that are outside the single precision range (6-9 digits).
        //
        // For example, a test with an expect result in the format of 0.xxxxxxxxx will use
        // CrossPlatformMachineEpsilon for the variance, while an expected result in the format of 0.0xxxxxxxxx
        // will use CrossPlatformMachineEpsilon / 10 and expected result in the format of x.xxxxxx will
        // use CrossPlatformMachineEpsilon * 10.
        private const float SingleCrossPlatformMachineEpsilon = 4.76837158e-07f;

        internal const double MinNormalDouble = 2.2250738585072014E-308;
        internal const float MinNormalSingle = 1.17549435E-38f;

        internal const double MaxSubnormalDouble = 2.2250738585072009E-308;
        internal const float MaxSubnormalSingle = 1.17549421E-38f;

        public static IEnumerable<object[]> ClampDouble
        {
            get
            {
                yield return new object[] {  double.NegativeInfinity,   1.0f, 63.0f, 1.0f };
                yield return new object[] {  double.MinValue,           1.0f, 63.0f, 1.0f };
                yield return new object[] { -1.0f,                      1.0f, 63.0f, 1.0f };
                yield return new object[] { -MinNormalDouble,           1.0f, 63.0f, 1.0f };
                yield return new object[] { -MaxSubnormalDouble,        1.0f, 63.0f, 1.0f };
                yield return new object[] { -double.Epsilon,            1.0f, 63.0f, 1.0f };
                yield return new object[] { -0.0f,                      1.0f, 63.0f, 1.0f };
                yield return new object[] {  double.NaN,                1.0f, 63.0f, double.NaN };
                yield return new object[] {  0.0f,                      1.0f, 63.0f, 1.0f };
                yield return new object[] {  double.Epsilon,            1.0f, 63.0f, 1.0f };
                yield return new object[] {  MaxSubnormalDouble,        1.0f, 63.0f, 1.0f };
                yield return new object[] {  MinNormalDouble,           1.0f, 63.0f, 1.0f };
                yield return new object[] {  1.0f,                      1.0f, 63.0f, 1.0f };
                yield return new object[] {  double.MaxValue,           1.0f, 63.0f, 63.0f };
                yield return new object[] {  double.PositiveInfinity,   1.0f, 63.0f, 63.0f };
            }
        }

        public static IEnumerable<object[]> ClampSingle
        {
            get
            {
                yield return new object[] {  float.NegativeInfinity,    1.0f, 63.0f, 1.0f };
                yield return new object[] {  float.MinValue,            1.0f, 63.0f, 1.0f };
                yield return new object[] { -1.0f,                      1.0f, 63.0f, 1.0f };
                yield return new object[] { -MinNormalSingle,           1.0f, 63.0f, 1.0f };
                yield return new object[] { -MaxSubnormalSingle,        1.0f, 63.0f, 1.0f };
                yield return new object[] { -float.Epsilon,             1.0f, 63.0f, 1.0f };
                yield return new object[] { -0.0f,                      1.0f, 63.0f, 1.0f };
                yield return new object[] {  float.NaN,                 1.0f, 63.0f, float.NaN };
                yield return new object[] {  0.0f,                      1.0f, 63.0f, 1.0f };
                yield return new object[] {  float.Epsilon,             1.0f, 63.0f, 1.0f };
                yield return new object[] {  MaxSubnormalSingle,        1.0f, 63.0f, 1.0f };
                yield return new object[] {  MinNormalSingle,           1.0f, 63.0f, 1.0f };
                yield return new object[] {  1.0f,                      1.0f, 63.0f, 1.0f };
                yield return new object[] {  float.MaxValue,            1.0f, 63.0f, 63.0f };
                yield return new object[] {  float.PositiveInfinity,    1.0f, 63.0f, 63.0f };
            }
        }

        public static IEnumerable<object[]> CopySignDouble
        {
            get
            {
                yield return new object[] {  double.NegativeInfinity,  double.NegativeInfinity,  double.NegativeInfinity };
                yield return new object[] {  double.NegativeInfinity, -3.1415926535897932,       double.NegativeInfinity };
                yield return new object[] {  double.NegativeInfinity, -0.0,                      double.NegativeInfinity };
                yield return new object[] {  double.NegativeInfinity,  double.NaN,               double.NegativeInfinity };
                yield return new object[] {  double.NegativeInfinity,  0.0,                      double.PositiveInfinity };
                yield return new object[] {  double.NegativeInfinity,  3.1415926535897932,       double.PositiveInfinity };
                yield return new object[] {  double.NegativeInfinity,  double.PositiveInfinity,  double.PositiveInfinity };
                yield return new object[] { -3.1415926535897932,       double.NegativeInfinity, -3.1415926535897932 };
                yield return new object[] { -3.1415926535897932,      -3.1415926535897932,      -3.1415926535897932 };
                yield return new object[] { -3.1415926535897932,      -0.0,                     -3.1415926535897932 };
                yield return new object[] { -3.1415926535897932,       double.NaN,              -3.1415926535897932 };
                yield return new object[] { -3.1415926535897932,       0.0,                      3.1415926535897932 };
                yield return new object[] { -3.1415926535897932,       3.1415926535897932,       3.1415926535897932 };
                yield return new object[] { -3.1415926535897932,       double.PositiveInfinity,  3.1415926535897932 };
                yield return new object[] { -0.0,                      double.NegativeInfinity, -0.0 };
                yield return new object[] { -0.0,                     -3.1415926535897932,      -0.0 };
                yield return new object[] { -0.0,                     -0.0,                     -0.0 };
                yield return new object[] { -0.0,                      double.NaN,              -0.0 };
                yield return new object[] { -0.0,                      0.0,                      0.0 };
                yield return new object[] { -0.0,                      3.1415926535897932,       0.0 };
                yield return new object[] { -0.0,                      double.PositiveInfinity,  0.0 };
                yield return new object[] {  double.NaN,               double.NegativeInfinity,  double.NaN };
                yield return new object[] {  double.NaN,              -3.1415926535897932,       double.NaN };
                yield return new object[] {  double.NaN,              -0.0,                      double.NaN };
                yield return new object[] {  double.NaN,               double.NaN,               double.NaN };
                yield return new object[] {  double.NaN,               0.0,                      double.NaN };
                yield return new object[] {  double.NaN,               3.1415926535897932,       double.NaN };
                yield return new object[] {  double.NaN,               double.PositiveInfinity,  double.NaN };
                yield return new object[] {  0.0,                      double.NegativeInfinity, -0.0 };
                yield return new object[] {  0.0,                     -3.1415926535897932,      -0.0 };
                yield return new object[] {  0.0,                     -0.0,                     -0.0 };
                yield return new object[] {  0.0,                      double.NaN,              -0.0 };
                yield return new object[] {  0.0,                      0.0,                      0.0 };
                yield return new object[] {  0.0,                      3.1415926535897932,       0.0 };
                yield return new object[] {  0.0,                      double.PositiveInfinity,  0.0 };
                yield return new object[] {  3.1415926535897932,       double.NegativeInfinity, -3.1415926535897932 };
                yield return new object[] {  3.1415926535897932,      -3.1415926535897932,      -3.1415926535897932 };
                yield return new object[] {  3.1415926535897932,      -0.0,                     -3.1415926535897932 };
                yield return new object[] {  3.1415926535897932,       double.NaN,              -3.1415926535897932 };
                yield return new object[] {  3.1415926535897932,       0.0,                      3.1415926535897932 };
                yield return new object[] {  3.1415926535897932,       3.1415926535897932,       3.1415926535897932 };
                yield return new object[] {  3.1415926535897932,       double.PositiveInfinity,  3.1415926535897932 };
                yield return new object[] {  double.PositiveInfinity,  double.NegativeInfinity,  double.NegativeInfinity };
                yield return new object[] {  double.PositiveInfinity, -3.1415926535897932,       double.NegativeInfinity };
                yield return new object[] {  double.PositiveInfinity, -0.0,                      double.NegativeInfinity };
                yield return new object[] {  double.PositiveInfinity,  double.NaN,               double.NegativeInfinity };
                yield return new object[] {  double.PositiveInfinity,  0.0,                      double.PositiveInfinity };
                yield return new object[] {  double.PositiveInfinity,  3.1415926535897932,       double.PositiveInfinity };
                yield return new object[] {  double.PositiveInfinity,  double.PositiveInfinity,  double.PositiveInfinity };
            }
        }

        public static IEnumerable<object[]> CopySignSingle
        {
            get
            {
                yield return new object[] {  float.NegativeInfinity,     float.NegativeInfinity,     float.NegativeInfinity };
                yield return new object[] {  float.NegativeInfinity,    -3.14159265f,                float.NegativeInfinity };
                yield return new object[] {  float.NegativeInfinity,    -0.0f,                       float.NegativeInfinity };
                yield return new object[] {  float.NegativeInfinity,     float.NaN,                  float.NegativeInfinity };
                yield return new object[] {  float.NegativeInfinity,     0.0f,                       float.PositiveInfinity };
                yield return new object[] {  float.NegativeInfinity,     3.14159265f,                float.PositiveInfinity };
                yield return new object[] {  float.NegativeInfinity,     float.PositiveInfinity,     float.PositiveInfinity };
                yield return new object[] { -3.14159265f,                float.NegativeInfinity,    -3.14159265f };
                yield return new object[] { -3.14159265f,               -3.14159265f,               -3.14159265f };
                yield return new object[] { -3.14159265f,               -0.0f,                      -3.14159265f };
                yield return new object[] { -3.14159265f,                float.NaN,                 -3.14159265f };
                yield return new object[] { -3.14159265f,                0.0f,                       3.14159265f };
                yield return new object[] { -3.14159265f,                3.14159265f,                3.14159265f };
                yield return new object[] { -3.14159265f,                float.PositiveInfinity,     3.14159265f };
                yield return new object[] { -0.0f,                       float.NegativeInfinity,    -0.0f };
                yield return new object[] { -0.0f,                      -3.14159265f,               -0.0f };
                yield return new object[] { -0.0f,                      -0.0f,                      -0.0f };
                yield return new object[] { -0.0f,                       float.NaN,                 -0.0f };
                yield return new object[] { -0.0f,                       0.0f,                       0.0f };
                yield return new object[] { -0.0f,                       3.14159265f,                0.0f };
                yield return new object[] { -0.0f,                       float.PositiveInfinity,     0.0f };
                yield return new object[] {  float.NaN,                  float.NegativeInfinity,     float.NaN };
                yield return new object[] {  float.NaN,                 -3.14159265f,                float.NaN };
                yield return new object[] {  float.NaN,                 -0.0f,                       float.NaN };
                yield return new object[] {  float.NaN,                  float.NaN,                  float.NaN };
                yield return new object[] {  float.NaN,                  0.0f,                       float.NaN };
                yield return new object[] {  float.NaN,                  3.14159265f,                float.NaN };
                yield return new object[] {  float.NaN,                  float.PositiveInfinity,     float.NaN };
                yield return new object[] {  0.0f,                       float.NegativeInfinity,    -0.0f };
                yield return new object[] {  0.0f,                      -3.14159265f,               -0.0f };
                yield return new object[] {  0.0f,                      -0.0f,                      -0.0f };
                yield return new object[] {  0.0f,                       float.NaN,                 -0.0f };
                yield return new object[] {  0.0f,                       0.0f,                       0.0f };
                yield return new object[] {  0.0f,                       3.14159265f,                0.0f };
                yield return new object[] {  0.0f,                       float.PositiveInfinity,     0.0f };
                yield return new object[] {  3.14159265f,                float.NegativeInfinity,    -3.14159265f };
                yield return new object[] {  3.14159265f,               -3.14159265f,               -3.14159265f };
                yield return new object[] {  3.14159265f,               -0.0f,                      -3.14159265f };
                yield return new object[] {  3.14159265f,                float.NaN,                 -3.14159265f };
                yield return new object[] {  3.14159265f,                0.0f,                       3.14159265f };
                yield return new object[] {  3.14159265f,                3.14159265f,                3.14159265f };
                yield return new object[] {  3.14159265f,                float.PositiveInfinity,     3.14159265f };
                yield return new object[] {  float.PositiveInfinity,     float.NegativeInfinity,     float.NegativeInfinity };
                yield return new object[] {  float.PositiveInfinity,    -3.14159265f,                float.NegativeInfinity };
                yield return new object[] {  float.PositiveInfinity,    -0.0f,                       float.NegativeInfinity };
                yield return new object[] {  float.PositiveInfinity,     float.NaN,                  float.NegativeInfinity };
                yield return new object[] {  float.PositiveInfinity,     0.0f,                       float.PositiveInfinity };
                yield return new object[] {  float.PositiveInfinity,     3.14159265f,                float.PositiveInfinity };
                yield return new object[] {  float.PositiveInfinity,     float.PositiveInfinity,     float.PositiveInfinity };
            }
        }

        public static IEnumerable<object[]> DegreesToRadiansDouble
        {
            get
            {
                yield return new object[] { double.NaN,               double.NaN,               0.0 };
                yield return new object[] { 0.0,                      0.0,                      0.0 };
                yield return new object[] { 0.31830988618379067,      0.005555555555555556,     DoubleCrossPlatformMachineEpsilon };       // value:  (1 / pi)
                yield return new object[] { 0.43429448190325183,      0.007579868632454674,     DoubleCrossPlatformMachineEpsilon };       // value:  (log10(e))
                yield return new object[] { 0.5,                      0.008726646259971648,     DoubleCrossPlatformMachineEpsilon };
                yield return new object[] { 0.63661977236758134,      0.011111111111111112,     DoubleCrossPlatformMachineEpsilon };       // value:  (2 / pi)
                yield return new object[] { 0.69314718055994531,      0.01209770050168668,      DoubleCrossPlatformMachineEpsilon };       // value:  (ln(2))
                yield return new object[] { 0.70710678118654752,      0.012341341494884351,     DoubleCrossPlatformMachineEpsilon };       // value:  (1 / sqrt(2))
                yield return new object[] { 0.78539816339744831,      0.013707783890401885,     DoubleCrossPlatformMachineEpsilon };       // value:  (pi / 4)
                yield return new object[] { 1.0,                      0.017453292519943295,     DoubleCrossPlatformMachineEpsilon };
                yield return new object[] { 1.1283791670955126,       0.019693931676727953,     DoubleCrossPlatformMachineEpsilon };       // value:  (2 / sqrt(pi))
                yield return new object[] { 1.4142135623730950,       0.024682682989768702,     DoubleCrossPlatformMachineEpsilon };       // value:  (sqrt(2))
                yield return new object[] { 1.4426950408889634,       0.02517977856570663,      DoubleCrossPlatformMachineEpsilon };       // value:  (log2(e))
                yield return new object[] { 1.5,                      0.02617993877991494,      DoubleCrossPlatformMachineEpsilon };
                yield return new object[] { 1.5707963267948966,       0.02741556778080377,      DoubleCrossPlatformMachineEpsilon };       // value:  (pi / 2)
                yield return new object[] { 2.0,                      0.03490658503988659,      DoubleCrossPlatformMachineEpsilon };
                yield return new object[] { 2.3025850929940457,       0.040187691180085916,     DoubleCrossPlatformMachineEpsilon };       // value:  (ln(10))
                yield return new object[] { 2.5,                      0.04363323129985824,      DoubleCrossPlatformMachineEpsilon };
                yield return new object[] { 2.7182818284590452,       0.047442967903742035,     DoubleCrossPlatformMachineEpsilon };       // value:  (e)
                yield return new object[] { 3.0,                      0.05235987755982988,      DoubleCrossPlatformMachineEpsilon };
                yield return new object[] { 3.1415926535897932,       0.05483113556160754,      DoubleCrossPlatformMachineEpsilon };       // value:  (pi)
                yield return new object[] { 3.5,                      0.061086523819801536,     DoubleCrossPlatformMachineEpsilon };
                yield return new object[] { double.PositiveInfinity,  double.PositiveInfinity,  0.0 };
            }
        }

        public static IEnumerable<object[]> DegreesToRadiansSingle
        {
            get
            {
                yield return new object[] { float.NaN,                 float.NaN,                 0.0f };
                yield return new object[] { 0.0f,                      0.0f,                      0.0f };
                yield return new object[] { 0.318309886f,              0.0055555557f,             SingleCrossPlatformMachineEpsilon };       // value:  (1 / pi)
                yield return new object[] { 0.434294482f,              0.007579869f,              SingleCrossPlatformMachineEpsilon };       // value:  (log10(e))
                yield return new object[] { 0.5f,                      0.008726646f,              SingleCrossPlatformMachineEpsilon };
                yield return new object[] { 0.636619772f,              0.011111111f,              SingleCrossPlatformMachineEpsilon };       // value:  (2 / pi)
                yield return new object[] { 0.693147181f,              0.0120977005f,             SingleCrossPlatformMachineEpsilon };       // value:  (ln(2))
                yield return new object[] { 0.707106781f,              0.012341342f,              SingleCrossPlatformMachineEpsilon };       // value:  (1 / sqrt(2))
                yield return new object[] { 0.785398163f,              0.013707785f,              SingleCrossPlatformMachineEpsilon };       // value:  (pi / 4)
                yield return new object[] { 1.0f,                      0.017453292f,              SingleCrossPlatformMachineEpsilon };
                yield return new object[] { 1.12837917f,               0.019693933f,              SingleCrossPlatformMachineEpsilon };       // value:  (2 / sqrt(pi))
                yield return new object[] { 1.41421356f,               0.024682684f,              SingleCrossPlatformMachineEpsilon };       // value:  (sqrt(2))
                yield return new object[] { 1.44269504f,               0.025179777f,              SingleCrossPlatformMachineEpsilon };       // value:  (log2(e))
                yield return new object[] { 1.5f,                      0.02617994f,               SingleCrossPlatformMachineEpsilon };
                yield return new object[] { 1.57079633f,               0.02741557f,               SingleCrossPlatformMachineEpsilon };       // value:  (pi / 2)
                yield return new object[] { 2.0f,                      0.034906585f,              SingleCrossPlatformMachineEpsilon };
                yield return new object[] { 2.30258509f,               0.040187694f,              SingleCrossPlatformMachineEpsilon };       // value:  (ln(10))
                yield return new object[] { 2.5f,                      0.043633234f,              SingleCrossPlatformMachineEpsilon };
                yield return new object[] { 2.71828183f,               0.047442965f,              SingleCrossPlatformMachineEpsilon };       // value:  (e)
                yield return new object[] { 3.0f,                      0.05235988f,               SingleCrossPlatformMachineEpsilon };
                yield return new object[] { 3.14159265f,               0.05483114f,               SingleCrossPlatformMachineEpsilon };       // value:  (pi)
                yield return new object[] { 3.5f,                      0.061086528f,              SingleCrossPlatformMachineEpsilon };
                yield return new object[] { float.PositiveInfinity,    float.PositiveInfinity,    0.0f };
            }
        }

        public static IEnumerable<object[]> ExpDouble
        {
            get
            {
                yield return new object[] {  double.NegativeInfinity, 0.0,                     0.0 };
                yield return new object[] { -3.1415926535897932,      0.043213918263772250,    DoubleCrossPlatformMachineEpsilon / 10 };   // value: -(pi)
                yield return new object[] { -2.7182818284590452,      0.065988035845312537,    DoubleCrossPlatformMachineEpsilon / 10 };   // value: -(e)
                yield return new object[] { -2.3025850929940457,      0.1,                     DoubleCrossPlatformMachineEpsilon };        // value: -(ln(10))
                yield return new object[] { -1.5707963267948966,      0.20787957635076191,     DoubleCrossPlatformMachineEpsilon };        // value: -(pi / 2)
                yield return new object[] { -1.4426950408889634,      0.23629008834452270,     DoubleCrossPlatformMachineEpsilon };        // value: -(log2(e))
                yield return new object[] { -1.4142135623730950,      0.24311673443421421,     DoubleCrossPlatformMachineEpsilon };        // value: -(sqrt(2))
                yield return new object[] { -1.1283791670955126,      0.32355726390307110,     DoubleCrossPlatformMachineEpsilon };        // value: -(2 / sqrt(pi))
                yield return new object[] { -1.0,                     0.36787944117144232,     DoubleCrossPlatformMachineEpsilon };
                yield return new object[] { -0.78539816339744831,     0.45593812776599624,     DoubleCrossPlatformMachineEpsilon };        // value: -(pi / 4)
                yield return new object[] { -0.70710678118654752,     0.49306869139523979,     DoubleCrossPlatformMachineEpsilon };        // value: -(1 / sqrt(2))
                yield return new object[] { -0.69314718055994531,     0.5,                     0.0 };                                     // value: -(ln(2))
                yield return new object[] { -0.63661977236758134,     0.52907780826773535,     DoubleCrossPlatformMachineEpsilon };        // value: -(2 / pi)
                yield return new object[] { -0.43429448190325183,     0.64772148514180065,     DoubleCrossPlatformMachineEpsilon };        // value: -(log10(e))
                yield return new object[] { -0.31830988618379067,     0.72737734929521647,     DoubleCrossPlatformMachineEpsilon };        // value: -(1 / pi)
                yield return new object[] { -0.0,                     1.0,                     0.0 };
                yield return new object[] {  double.NaN,              double.NaN,              0.0 };
                yield return new object[] {  0.0,                     1.0,                     0.0 };
                yield return new object[] {  0.31830988618379067,     1.3748022274393586,      DoubleCrossPlatformMachineEpsilon * 10 };   // value:  (1 / pi)
                yield return new object[] {  0.43429448190325183,     1.5438734439711811,      DoubleCrossPlatformMachineEpsilon * 10 };   // value:  (log10(e))
                yield return new object[] {  0.63661977236758134,     1.8900811645722220,      DoubleCrossPlatformMachineEpsilon * 10 };   // value:  (2 / pi)
                yield return new object[] {  0.69314718055994531,     2.0,                     0.0 };                                      // value:  (ln(2))
                yield return new object[] {  0.70710678118654752,     2.0281149816474725,      DoubleCrossPlatformMachineEpsilon * 10 };   // value:  (1 / sqrt(2))
                yield return new object[] {  0.78539816339744831,     2.1932800507380155,      DoubleCrossPlatformMachineEpsilon * 10 };   // value:  (pi / 4)
                yield return new object[] {  1.0,                     2.7182818284590452,      DoubleCrossPlatformMachineEpsilon * 10 };   //                          expected: (e)
                yield return new object[] {  1.1283791670955126,      3.0906430223107976,      DoubleCrossPlatformMachineEpsilon * 10 };   // value:  (2 / sqrt(pi))
                yield return new object[] {  1.4142135623730950,      4.1132503787829275,      DoubleCrossPlatformMachineEpsilon * 10 };   // value:  (sqrt(2))
                yield return new object[] {  1.4426950408889634,      4.2320861065570819,      DoubleCrossPlatformMachineEpsilon * 10 };   // value:  (log2(e))
                yield return new object[] {  1.5707963267948966,      4.8104773809653517,      DoubleCrossPlatformMachineEpsilon * 10 };   // value:  (pi / 2)
                yield return new object[] {  2.3025850929940457,      10.0,                    DoubleCrossPlatformMachineEpsilon * 10 };                                      // value:  (ln(10))
                yield return new object[] {  2.7182818284590452,      15.154262241479264,      DoubleCrossPlatformMachineEpsilon * 100 };  // value:  (e)
                yield return new object[] {  3.1415926535897932,      23.140692632779269,      DoubleCrossPlatformMachineEpsilon * 100 };  // value:  (pi)
                yield return new object[] {  double.PositiveInfinity, double.PositiveInfinity, 0.0 };
            }
        }

        public static IEnumerable<object[]> ExpSingle
        {
            get
            {
                yield return new object[] {  float.NegativeInfinity, 0.0f,                   0.0f };
                yield return new object[] { -3.14159265f,            0.0432139183f,          SingleCrossPlatformMachineEpsilon / 10 };     // value: -(pi)
                yield return new object[] { -2.71828183f,            0.0659880358f,          SingleCrossPlatformMachineEpsilon / 10 };     // value: -(e)
                yield return new object[] { -2.30258509f,            0.1f,                   SingleCrossPlatformMachineEpsilon };          // value: -(ln(10))
                yield return new object[] { -1.57079633f,            0.207879576f,           SingleCrossPlatformMachineEpsilon };          // value: -(pi / 2)
                yield return new object[] { -1.44269504f,            0.236290088f,           SingleCrossPlatformMachineEpsilon };          // value: -(log2(e))
                yield return new object[] { -1.41421356f,            0.243116734f,           SingleCrossPlatformMachineEpsilon };          // value: -(sqrt(2))
                yield return new object[] { -1.12837917f,            0.323557264f,           SingleCrossPlatformMachineEpsilon };          // value: -(2 / sqrt(pi))
                yield return new object[] { -1.0f,                   0.367879441f,           SingleCrossPlatformMachineEpsilon };
                yield return new object[] { -0.785398163f,           0.455938128f,           SingleCrossPlatformMachineEpsilon };          // value: -(pi / 4)
                yield return new object[] { -0.707106781f,           0.493068691f,           SingleCrossPlatformMachineEpsilon };          // value: -(1 / sqrt(2))
                yield return new object[] { -0.693147181f,           0.5f,                   SingleCrossPlatformMachineEpsilon };          // value: -(ln(2))
                yield return new object[] { -0.636619772f,           0.529077808f,           SingleCrossPlatformMachineEpsilon };          // value: -(2 / pi)
                yield return new object[] { -0.434294482f,           0.647721485f,           SingleCrossPlatformMachineEpsilon };          // value: -(log10(e))
                yield return new object[] { -0.318309886f,           0.727377349f,           SingleCrossPlatformMachineEpsilon };          // value: -(1 / pi)
                yield return new object[] { -0.0f,                   1.0f,                   SingleCrossPlatformMachineEpsilon * 10 };
                yield return new object[] {  float.NaN,              float.NaN,              0.0f };
                yield return new object[] {  0.0f,                   1.0f,                   SingleCrossPlatformMachineEpsilon * 10 };
                yield return new object[] {  0.318309886f,           1.37480223f,            SingleCrossPlatformMachineEpsilon * 10 };     // value:  (1 / pi)
                yield return new object[] {  0.434294482f,           1.54387344f,            SingleCrossPlatformMachineEpsilon * 10 };     // value:  (log10(e))
                yield return new object[] {  0.636619772f,           1.89008116f,            SingleCrossPlatformMachineEpsilon * 10 };     // value:  (2 / pi)
                yield return new object[] {  0.693147181f,           2.0f,                   SingleCrossPlatformMachineEpsilon * 10 };                                       // value:  (ln(2))
                yield return new object[] {  0.707106781f,           2.02811498f,            SingleCrossPlatformMachineEpsilon * 10 };     // value:  (1 / sqrt(2))
                yield return new object[] {  0.785398163f,           2.19328005f,            SingleCrossPlatformMachineEpsilon * 10 };     // value:  (pi / 4)
                yield return new object[] {  1.0f,                   2.71828183f,            SingleCrossPlatformMachineEpsilon * 10 };     //                          expected: (e)
                yield return new object[] {  1.12837917f,            3.09064302f,            SingleCrossPlatformMachineEpsilon * 10 };     // value:  (2 / sqrt(pi))
                yield return new object[] {  1.41421356f,            4.11325038f,            SingleCrossPlatformMachineEpsilon * 10 };     // value:  (sqrt(2))
                yield return new object[] {  1.44269504f,            4.23208611f,            SingleCrossPlatformMachineEpsilon * 10 };     // value:  (log2(e))
                yield return new object[] {  1.57079633f,            4.81047738f,            SingleCrossPlatformMachineEpsilon * 10 };     // value:  (pi / 2)
                yield return new object[] {  2.30258509f,            10.0f,                  SingleCrossPlatformMachineEpsilon * 10 };                                       // value:  (ln(10))
                yield return new object[] {  2.71828183f,            15.1542622f,            SingleCrossPlatformMachineEpsilon * 100 };    // value:  (e)
                yield return new object[] {  3.14159265f,            23.1406926f,            SingleCrossPlatformMachineEpsilon * 100 };    // value:  (pi)
                yield return new object[] {  float.PositiveInfinity, float.PositiveInfinity, 0.0f };
            }
        }

        public static IEnumerable<object[]> FusedMultiplyAddDouble
        {
            get
            {
                yield return new object[] { double.NegativeInfinity,  double.NegativeInfinity,  double.NegativeInfinity }; 
                yield return new object[] { double.NegativeInfinity, -0.0,                      double.NegativeInfinity }; 
                yield return new object[] { double.NegativeInfinity, -0.0,                     -3.1415926535897932 };      
                yield return new object[] { double.NegativeInfinity, -0.0,                     -0.0 };                     
                yield return new object[] { double.NegativeInfinity, -0.0,                      double.NaN };              
                yield return new object[] { double.NegativeInfinity, -0.0,                      0.0 };                     
                yield return new object[] { double.NegativeInfinity, -0.0,                      3.1415926535897932 };      
                yield return new object[] { double.NegativeInfinity, -0.0,                      double.PositiveInfinity }; 
                yield return new object[] { double.NegativeInfinity,  0.0,                      double.NegativeInfinity }; 
                yield return new object[] { double.NegativeInfinity,  0.0,                     -3.1415926535897932 };      
                yield return new object[] { double.NegativeInfinity,  0.0,                     -0.0 };                     
                yield return new object[] { double.NegativeInfinity,  0.0,                      double.NaN };              
                yield return new object[] { double.NegativeInfinity,  0.0,                      0.0 };                     
                yield return new object[] { double.NegativeInfinity,  0.0,                      3.1415926535897932 };      
                yield return new object[] { double.NegativeInfinity,  0.0,                      double.PositiveInfinity }; 
                yield return new object[] { double.NegativeInfinity,  double.PositiveInfinity,  double.PositiveInfinity }; 
                yield return new object[] {-1e308,                    2.0,                      1e308 };                   
                yield return new object[] {-1e308,                    2.0,                      double.PositiveInfinity }; 
                yield return new object[] {-5,                        4,                       -3 };                       
                yield return new object[] {-0.0,                      double.NegativeInfinity,  double.NegativeInfinity }; 
                yield return new object[] {-0.0,                      double.NegativeInfinity, -3.1415926535897932 };      
                yield return new object[] {-0.0,                      double.NegativeInfinity, -0.0 };                     
                yield return new object[] {-0.0,                      double.NegativeInfinity,  double.NaN };              
                yield return new object[] {-0.0,                      double.NegativeInfinity,  0.0 };                     
                yield return new object[] {-0.0,                      double.NegativeInfinity,  3.1415926535897932 };      
                yield return new object[] {-0.0,                      double.NegativeInfinity,  double.PositiveInfinity }; 
                yield return new object[] {-0.0,                      double.PositiveInfinity,  double.NegativeInfinity }; 
                yield return new object[] {-0.0,                      double.PositiveInfinity, -3.1415926535897932 };      
                yield return new object[] {-0.0,                      double.PositiveInfinity, -0.0 };                     
                yield return new object[] {-0.0,                      double.PositiveInfinity,  double.NaN };              
                yield return new object[] {-0.0,                      double.PositiveInfinity,  0.0 };                     
                yield return new object[] {-0.0,                      double.PositiveInfinity,  3.1415926535897932 };      
                yield return new object[] {-0.0,                      double.PositiveInfinity,  double.PositiveInfinity }; 
                yield return new object[] { 0.0,                      double.NegativeInfinity,  double.NegativeInfinity }; 
                yield return new object[] { 0.0,                      double.NegativeInfinity, -3.1415926535897932 };      
                yield return new object[] { 0.0,                      double.NegativeInfinity, -0.0 };                     
                yield return new object[] { 0.0,                      double.NegativeInfinity,  double.NaN };              
                yield return new object[] { 0.0,                      double.NegativeInfinity,  0.0 };                     
                yield return new object[] { 0.0,                      double.NegativeInfinity,  3.1415926535897932 };      
                yield return new object[] { 0.0,                      double.NegativeInfinity,  double.PositiveInfinity }; 
                yield return new object[] { 0.0,                      double.PositiveInfinity,  double.NegativeInfinity }; 
                yield return new object[] { 0.0,                      double.PositiveInfinity, -3.1415926535897932 };      
                yield return new object[] { 0.0,                      double.PositiveInfinity, -0.0 };                     
                yield return new object[] { 0.0,                      double.PositiveInfinity,  double.NaN };              
                yield return new object[] { 0.0,                      double.PositiveInfinity,  0.0 };                     
                yield return new object[] { 0.0,                      double.PositiveInfinity,  3.1415926535897932 };      
                yield return new object[] { 0.0,                      double.PositiveInfinity,  double.PositiveInfinity }; 
                yield return new object[] { 5,                        4,                        3 };                       
                yield return new object[] { 1e308,                    2.0,                     -1e308 };                   
                yield return new object[] { 1e308,                    2.0,                      double.NegativeInfinity }; 
                yield return new object[] { double.PositiveInfinity,  double.NegativeInfinity,  double.PositiveInfinity }; 
                yield return new object[] { double.PositiveInfinity, -0.0,                      double.NegativeInfinity }; 
                yield return new object[] { double.PositiveInfinity, -0.0,                     -3.1415926535897932 };      
                yield return new object[] { double.PositiveInfinity, -0.0,                     -0.0 };                     
                yield return new object[] { double.PositiveInfinity, -0.0,                      double.NaN };              
                yield return new object[] { double.PositiveInfinity, -0.0,                      0.0 };                     
                yield return new object[] { double.PositiveInfinity, -0.0,                      3.1415926535897932 };      
                yield return new object[] { double.PositiveInfinity, -0.0,                      double.PositiveInfinity }; 
                yield return new object[] { double.PositiveInfinity,  0.0,                      double.NegativeInfinity }; 
                yield return new object[] { double.PositiveInfinity,  0.0,                     -3.1415926535897932 };      
                yield return new object[] { double.PositiveInfinity,  0.0,                     -0.0 };                     
                yield return new object[] { double.PositiveInfinity,  0.0,                      double.NaN };              
                yield return new object[] { double.PositiveInfinity,  0.0,                      0.0 };                     
                yield return new object[] { double.PositiveInfinity,  0.0,                      3.1415926535897932 };      
                yield return new object[] { double.PositiveInfinity,  0.0,                      double.PositiveInfinity };
                yield return new object[] { double.PositiveInfinity,  double.PositiveInfinity,  double.NegativeInfinity }; 
            }
        }

        public static IEnumerable<object[]> FusedMultiplyAddSingle
        {
            get
            {
                yield return new object[] {  float.NegativeInfinity,  float.NegativeInfinity,  float.NegativeInfinity };
                yield return new object[] {  float.NegativeInfinity, -0.0f,                    float.NegativeInfinity };
                yield return new object[] {  float.NegativeInfinity, -0.0f,                   -3.14159265f };
                yield return new object[] {  float.NegativeInfinity, -0.0f,                   -0.0f };
                yield return new object[] {  float.NegativeInfinity, -0.0f,                    float.NaN };
                yield return new object[] {  float.NegativeInfinity, -0.0f,                    0.0f };
                yield return new object[] {  float.NegativeInfinity, -0.0f,                    3.14159265f };
                yield return new object[] {  float.NegativeInfinity, -0.0f,                    float.PositiveInfinity };
                yield return new object[] {  float.NegativeInfinity,  0.0f,                    float.NegativeInfinity };
                yield return new object[] {  float.NegativeInfinity,  0.0f,                   -3.14159265f };
                yield return new object[] {  float.NegativeInfinity,  0.0f,                   -0.0f };
                yield return new object[] {  float.NegativeInfinity,  0.0f,                    float.NaN };
                yield return new object[] {  float.NegativeInfinity,  0.0f,                    0.0f };
                yield return new object[] {  float.NegativeInfinity,  0.0f,                    3.14159265f };
                yield return new object[] {  float.NegativeInfinity,  0.0f,                    float.PositiveInfinity };
                yield return new object[] {  float.NegativeInfinity,  float.PositiveInfinity,  float.PositiveInfinity };
                yield return new object[] { -1e38f,                   2.0f,                    1e38f };
                yield return new object[] { -1e38f,                   2.0f,                    float.PositiveInfinity };
                yield return new object[] { -5,                       4,                      -3 };
                yield return new object[] { -0.0f,                    float.NegativeInfinity,  float.NegativeInfinity };
                yield return new object[] { -0.0f,                    float.NegativeInfinity, -3.14159265f };
                yield return new object[] { -0.0f,                    float.NegativeInfinity, -0.0f };
                yield return new object[] { -0.0f,                    float.NegativeInfinity,  float.NaN };
                yield return new object[] { -0.0f,                    float.NegativeInfinity,  0.0f };
                yield return new object[] { -0.0f,                    float.NegativeInfinity,  3.14159265f };
                yield return new object[] { -0.0f,                    float.NegativeInfinity,  float.PositiveInfinity };
                yield return new object[] { -0.0f,                    float.PositiveInfinity,  float.NegativeInfinity };
                yield return new object[] { -0.0f,                    float.PositiveInfinity, -3.14159265f };
                yield return new object[] { -0.0f,                    float.PositiveInfinity, -0.0f };
                yield return new object[] { -0.0f,                    float.PositiveInfinity,  float.NaN };
                yield return new object[] { -0.0f,                    float.PositiveInfinity,  0.0f };
                yield return new object[] { -0.0f,                    float.PositiveInfinity,  3.14159265f };
                yield return new object[] { -0.0f,                    float.PositiveInfinity,  float.PositiveInfinity };
                yield return new object[] {  0.0f,                    float.NegativeInfinity,  float.NegativeInfinity };
                yield return new object[] {  0.0f,                    float.NegativeInfinity, -3.14159265f };
                yield return new object[] {  0.0f,                    float.NegativeInfinity, -0.0f };
                yield return new object[] {  0.0f,                    float.NegativeInfinity,  float.NaN };
                yield return new object[] {  0.0f,                    float.NegativeInfinity,  0.0f };
                yield return new object[] {  0.0f,                    float.NegativeInfinity,  3.14159265f };
                yield return new object[] {  0.0f,                    float.NegativeInfinity,  float.PositiveInfinity };
                yield return new object[] {  0.0f,                    float.PositiveInfinity,  float.NegativeInfinity };
                yield return new object[] {  0.0f,                    float.PositiveInfinity, -3.14159265f };
                yield return new object[] {  0.0f,                    float.PositiveInfinity, -0.0f };
                yield return new object[] {  0.0f,                    float.PositiveInfinity,  float.NaN };
                yield return new object[] {  0.0f,                    float.PositiveInfinity,  0.0f };
                yield return new object[] {  0.0f,                    float.PositiveInfinity,  3.14159265f };
                yield return new object[] {  0.0f,                    float.PositiveInfinity,  float.PositiveInfinity };
                yield return new object[] {  5,                       4,                       3 };
                yield return new object[] {  1e38f,                   2.0f,                   -1e38f };
                yield return new object[] {  1e38f,                   2.0f,                    float.NegativeInfinity };
                yield return new object[] {  float.PositiveInfinity,  float.NegativeInfinity,  float.PositiveInfinity };
                yield return new object[] {  float.PositiveInfinity, -0.0f,                    float.NegativeInfinity };
                yield return new object[] {  float.PositiveInfinity, -0.0f,                   -3.14159265f };
                yield return new object[] {  float.PositiveInfinity, -0.0f,                   -0.0f };
                yield return new object[] {  float.PositiveInfinity, -0.0f,                    float.NaN };
                yield return new object[] {  float.PositiveInfinity, -0.0f,                    0.0f };
                yield return new object[] {  float.PositiveInfinity, -0.0f,                    3.14159265f };
                yield return new object[] {  float.PositiveInfinity, -0.0f,                    float.PositiveInfinity };
                yield return new object[] {  float.PositiveInfinity,  0.0f,                    float.NegativeInfinity };
                yield return new object[] {  float.PositiveInfinity,  0.0f,                   -3.14159265f };
                yield return new object[] {  float.PositiveInfinity,  0.0f,                   -0.0f };
                yield return new object[] {  float.PositiveInfinity,  0.0f,                    float.NaN };
                yield return new object[] {  float.PositiveInfinity,  0.0f,                    0.0f };
                yield return new object[] {  float.PositiveInfinity,  0.0f,                    3.14159265f };
                yield return new object[] {  float.PositiveInfinity,  0.0f,                    float.PositiveInfinity };
                yield return new object[] {  float.PositiveInfinity,  float.PositiveInfinity,  float.NegativeInfinity };
            }
        }

        public static IEnumerable<object[]> HypotDouble
        {
            get
            {
                yield return new object[] { double.NaN,              double.NaN,              double.NaN,              0.0 };
                yield return new object[] { double.NaN,              0.0f,                    double.NaN,              0.0 };
                yield return new object[] { double.NaN,              1.0f,                    double.NaN,              0.0 };
                yield return new object[] { double.NaN,              2.7182818284590452,      double.NaN,              0.0 };
                yield return new object[] { double.NaN,              10.0,                    double.NaN,              0.0 };
                yield return new object[] { 0.0,                     0.0,                     0.0,                     0.0 };
                yield return new object[] { 0.0,                     1.0,                     1.0,                     0.0 };
                yield return new object[] { 0.0,                     1.5707963267948966,      1.5707963267948966,      0.0 };
                yield return new object[] { 0.0,                     2.0,                     2.0,                     0.0 };
                yield return new object[] { 0.0,                     2.7182818284590452,      2.7182818284590452,      0.0 };
                yield return new object[] { 0.0,                     3.0,                     3.0,                     0.0 };
                yield return new object[] { 0.0,                     10.0,                    10.0,                    0.0 };
                yield return new object[] { 1.0,                     1.0,                     1.4142135623730950,      DoubleCrossPlatformMachineEpsilon * 10 };
                yield return new object[] { 1.0,                     1e+10,                   1e+10,                   0.0 }; // dotnet/runtime#75651
                yield return new object[] { 1.0,                     1e+20,                   1e+20,                   0.0 }; // dotnet/runtime#75651
                yield return new object[] { 2.7182818284590452,      0.31830988618379067,     2.7368553638387594,      DoubleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (1 / pi)
                yield return new object[] { 2.7182818284590452,      0.43429448190325183,     2.7527563996732919,      DoubleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (log10(e))
                yield return new object[] { 2.7182818284590452,      0.63661977236758134,     2.7918346715914253,      DoubleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (2 / pi)
                yield return new object[] { 2.7182818284590452,      0.69314718055994531,     2.8052645352709344,      DoubleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (ln(2))
                yield return new object[] { 2.7182818284590452,      0.70710678118654752,     2.8087463571726533,      DoubleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (1 / sqrt(2))
                yield return new object[] { 2.7182818284590452,      0.78539816339744831,     2.8294710413783590,      DoubleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (pi / 4)
                yield return new object[] { 2.7182818284590452,      1.0,                     2.8963867315900082,      DoubleCrossPlatformMachineEpsilon * 10 };   // x: (e)
                yield return new object[] { 2.7182818284590452,      1.1283791670955126,      2.9431778138036127,      DoubleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (2 / sqrt(pi))
                yield return new object[] { 2.7182818284590452,      1.4142135623730950,      3.0641566701020120,      DoubleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (sqrt(2))
                yield return new object[] { 2.7182818284590452,      1.4426950408889634,      3.0774055761202907,      DoubleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (log2(e))
                yield return new object[] { 2.7182818284590452,      1.5707963267948966,      3.1394995141268918,      DoubleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (pi / 2)
                yield return new object[] { 2.7182818284590452,      2.3025850929940457,      3.5624365551415857,      DoubleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (ln(10))
                yield return new object[] { 2.7182818284590452,      2.7182818284590452,      3.8442310281591168,      DoubleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (e)
                yield return new object[] { 2.7182818284590452,      3.1415926535897932,      4.1543544023133136,      DoubleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (pi)
                yield return new object[] { 10.0,                    0.31830988618379067,     10.005064776584025,      DoubleCrossPlatformMachineEpsilon * 100 };  //          y: (1 / pi)
                yield return new object[] { 10.0,                    0.43429448190325183,     10.009426142242702,      DoubleCrossPlatformMachineEpsilon * 100 };  //          y: (log10(e))
                yield return new object[] { 10.0,                    0.63661977236758134,     10.020243746265325,      DoubleCrossPlatformMachineEpsilon * 100 };  //          y: (2 / pi)
                yield return new object[] { 10.0,                    0.69314718055994531,     10.023993865417028,      DoubleCrossPlatformMachineEpsilon * 100 };  //          y: (ln(2))
                yield return new object[] { 10.0,                    0.70710678118654752,     10.024968827881711,      DoubleCrossPlatformMachineEpsilon * 100 };  //          y: (1 / sqrt(2))
                yield return new object[] { 10.0,                    0.78539816339744831,     10.030795096853892,      DoubleCrossPlatformMachineEpsilon * 100 };  //          y: (pi / 4)
                yield return new object[] { 10.0,                    1.0,                     10.049875621120890,      DoubleCrossPlatformMachineEpsilon * 100 };  //
                yield return new object[] { 10.0,                    1.1283791670955126,      10.063460614755501,      DoubleCrossPlatformMachineEpsilon * 100 };  //          y: (2 / sqrt(pi))
                yield return new object[] { 10.0,                    1.4142135623730950,      10.099504938362078,      DoubleCrossPlatformMachineEpsilon * 100 };  //          y: (sqrt(2))
                yield return new object[] { 10.0,                    1.4426950408889634,      10.103532500121213,      DoubleCrossPlatformMachineEpsilon * 100 };  //          y: (log2(e))
                yield return new object[] { 10.0,                    1.5707963267948966,      10.122618292728040,      DoubleCrossPlatformMachineEpsilon * 100 };  //          y: (pi / 2)
                yield return new object[] { 10.0,                    2.3025850929940457,      10.261671311754163,      DoubleCrossPlatformMachineEpsilon * 100 };  //          y: (ln(10))
                yield return new object[] { 10.0,                    2.7182818284590452,      10.362869105558106,      DoubleCrossPlatformMachineEpsilon * 100 };  //          y: (e)
                yield return new object[] { 10.0,                    3.1415926535897932,      10.481870272097884,      DoubleCrossPlatformMachineEpsilon * 100 };  //          y: (pi)
                yield return new object[] { double.PositiveInfinity, double.NaN,              double.PositiveInfinity, 0.0 };
                yield return new object[] { double.PositiveInfinity, 0.0,                     double.PositiveInfinity, 0.0 };
                yield return new object[] { double.PositiveInfinity, 1.0,                     double.PositiveInfinity, 0.0 };
                yield return new object[] { double.PositiveInfinity, 2.7182818284590452,      double.PositiveInfinity, 0.0 };
                yield return new object[] { double.PositiveInfinity, 10.0,                    double.PositiveInfinity, 0.0 };
                yield return new object[] { double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity, 0.0 };
            }
        }

        public static IEnumerable<object[]> HypotSingle
        {
            get
            {
                yield return new object[] { float.NaN,              float.NaN,              float.NaN,              0.0f };
                yield return new object[] { float.NaN,              0.0f,                   float.NaN,              0.0f };
                yield return new object[] { float.NaN,              1.0f,                   float.NaN,              0.0f };
                yield return new object[] { float.NaN,              2.71828183f,            float.NaN,              0.0f };
                yield return new object[] { float.NaN,              10.0f,                  float.NaN,              0.0f };
                yield return new object[] { 0.0f,                   0.0f,                   0.0f,                   0.0f };
                yield return new object[] { 0.0f,                   1.0f,                   1.0f,                   0.0f };
                yield return new object[] { 0.0f,                   1.57079633f,            1.57079633f,            0.0f };
                yield return new object[] { 0.0f,                   2.0f,                   2.0f,                   0.0f };
                yield return new object[] { 0.0f,                   2.71828183f,            2.71828183f,            0.0f };
                yield return new object[] { 0.0f,                   3.0f,                   3.0f,                   0.0f };
                yield return new object[] { 0.0f,                   10.0f,                  10.0f,                  0.0f };
                yield return new object[] { 1.0f,                   1.0f,                   1.41421356f,            SingleCrossPlatformMachineEpsilon * 10 };
                yield return new object[] { 1.0f,                   1e+10f,                 1e+10f,                 0.0 }; // dotnet/runtime#75651
                yield return new object[] { 1.0f,                   1e+20f,                 1e+20f,                 0.0 }; // dotnet/runtime#75651
                yield return new object[] { 2.71828183f,            0.318309886f,           2.73685536f,            SingleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (1 / pi)
                yield return new object[] { 2.71828183f,            0.434294482f,           2.75275640f,            SingleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (log10(e))
                yield return new object[] { 2.71828183f,            0.636619772f,           2.79183467f,            SingleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (2 / pi)
                yield return new object[] { 2.71828183f,            0.693147181f,           2.80526454f,            SingleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (ln(2))
                yield return new object[] { 2.71828183f,            0.707106781f,           2.80874636f,            SingleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (1 / sqrt(2))
                yield return new object[] { 2.71828183f,            0.785398163f,           2.82947104f,            SingleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (pi / 4)
                yield return new object[] { 2.71828183f,            1.0f,                   2.89638673f,            SingleCrossPlatformMachineEpsilon * 10 };   // x: (e)
                yield return new object[] { 2.71828183f,            1.12837917f,            2.94317781f,            SingleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (2 / sqrt(pi))
                yield return new object[] { 2.71828183f,            1.41421356f,            3.06415667f,            SingleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (sqrt(2))
                yield return new object[] { 2.71828183f,            1.44269504f,            3.07740558f,            SingleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (log2(e))
                yield return new object[] { 2.71828183f,            1.57079633f,            3.13949951f,            SingleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (pi / 2)
                yield return new object[] { 2.71828183f,            2.30258509f,            3.56243656f,            SingleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (ln(10))
                yield return new object[] { 2.71828183f,            2.71828183f,            3.84423103f,            SingleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (e)
                yield return new object[] { 2.71828183f,            3.14159265f,            4.15435440f,            SingleCrossPlatformMachineEpsilon * 10 };   // x: (e)   y: (pi)
                yield return new object[] { 10.0f,                  0.318309886f,           10.0050648f,            SingleCrossPlatformMachineEpsilon * 100 };  //          y: (1 / pi)
                yield return new object[] { 10.0f,                  0.434294482f,           10.0094261f,            SingleCrossPlatformMachineEpsilon * 100 };  //          y: (log10(e))
                yield return new object[] { 10.0f,                  0.636619772f,           10.0202437f,            SingleCrossPlatformMachineEpsilon * 100 };  //          y: (2 / pi)
                yield return new object[] { 10.0f,                  0.693147181f,           10.0239939f,            SingleCrossPlatformMachineEpsilon * 100 };  //          y: (ln(2))
                yield return new object[] { 10.0f,                  0.707106781f,           10.0249688f,            SingleCrossPlatformMachineEpsilon * 100 };  //          y: (1 / sqrt(2))
                yield return new object[] { 10.0f,                  0.785398163f,           10.0307951f,            SingleCrossPlatformMachineEpsilon * 100 };  //          y: (pi / 4)
                yield return new object[] { 10.0f,                  1.0f,                   10.0498756f,            SingleCrossPlatformMachineEpsilon * 100 };  //
                yield return new object[] { 10.0f,                  1.12837917f,            10.0634606f,            SingleCrossPlatformMachineEpsilon * 100 };  //          y: (2 / sqrt(pi))
                yield return new object[] { 10.0f,                  1.41421356f,            10.0995049f,            SingleCrossPlatformMachineEpsilon * 100 };  //          y: (sqrt(2))
                yield return new object[] { 10.0f,                  1.44269504f,            10.1035325f,            SingleCrossPlatformMachineEpsilon * 100 };  //          y: (log2(e))
                yield return new object[] { 10.0f,                  1.57079633f,            10.1226183f,            SingleCrossPlatformMachineEpsilon * 100 };  //          y: (pi / 2)
                yield return new object[] { 10.0f,                  2.30258509f,            10.2616713f,            SingleCrossPlatformMachineEpsilon * 100 };  //          y: (ln(10))
                yield return new object[] { 10.0f,                  2.71828183f,            10.3628691f,            SingleCrossPlatformMachineEpsilon * 100 };  //          y: (e)
                yield return new object[] { 10.0f,                  3.14159265f,            10.4818703f,            SingleCrossPlatformMachineEpsilon * 100 };  //          y: (pi)
                yield return new object[] { float.PositiveInfinity, float.NaN,              float.PositiveInfinity, 0.0f };
                yield return new object[] { float.PositiveInfinity, 0.0f,                   float.PositiveInfinity, 0.0f };
                yield return new object[] { float.PositiveInfinity, 1.0f,                   float.PositiveInfinity, 0.0f };
                yield return new object[] { float.PositiveInfinity, 2.71828183f,            float.PositiveInfinity, 0.0f };
                yield return new object[] { float.PositiveInfinity, 10.0f,                  float.PositiveInfinity, 0.0f };
                yield return new object[] { float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, 0.0f };
            }
        }

        public static IEnumerable<object[]> IsNaNDouble
        {
            get
            {
                yield return new object[] {  double.NegativeInfinity,   false };
                yield return new object[] {  double.MinValue,           false };
                yield return new object[] { -MinNormalDouble,           false };
                yield return new object[] { -MaxSubnormalDouble,        false };
                yield return new object[] { -double.Epsilon,            false };
                yield return new object[] { -0.0,                       false };
                yield return new object[] {  double.NaN,                true };
                yield return new object[] {  0.0,                       false };
                yield return new object[] {  double.Epsilon,            false };
                yield return new object[] {  MaxSubnormalDouble,        false };
                yield return new object[] {  MinNormalDouble,           false };
                yield return new object[] {  double.MaxValue,           false };
                yield return new object[] {  double.PositiveInfinity,   false };
            }
        }

        public static IEnumerable<object[]> IsNaNSingle
        {
            get
            {
                yield return new object[] {  float.NegativeInfinity,    false };
                yield return new object[] {  float.MinValue,            false };
                yield return new object[] { -MinNormalSingle,           false };
                yield return new object[] { -MaxSubnormalSingle,        false };
                yield return new object[] { -float.Epsilon,             false };
                yield return new object[] { -0.0f,                      false };
                yield return new object[] {  float.NaN,                 true };
                yield return new object[] {  0.0f,                      false };
                yield return new object[] {  float.Epsilon,             false };
                yield return new object[] {  MaxSubnormalSingle,        false };
                yield return new object[] {  MinNormalSingle,           false };
                yield return new object[] {  float.MaxValue,            false };
                yield return new object[] {  float.PositiveInfinity,    false };
            }
        }

        public static IEnumerable<object[]> IsNegativeDouble
        {
            get
            {
                yield return new object[] {  double.NegativeInfinity,   true };
                yield return new object[] {  double.MinValue,           true };
                yield return new object[] { -MinNormalDouble,           true };
                yield return new object[] { -MaxSubnormalDouble,        true };
                yield return new object[] { -0.0,                       true };
                yield return new object[] {  double.NaN,                true };
                yield return new object[] {  0.0,                       false };
                yield return new object[] {  MaxSubnormalDouble,        false };
                yield return new object[] {  MinNormalDouble,           false };
                yield return new object[] {  double.MaxValue,           false };
                yield return new object[] {  double.PositiveInfinity,   false };
            }
        }

        public static IEnumerable<object[]> IsNegativeSingle
        {
            get
            {
                yield return new object[] {  float.NegativeInfinity,    true };
                yield return new object[] {  float.MinValue,            true };
                yield return new object[] { -MinNormalSingle,           true };
                yield return new object[] { -MaxSubnormalSingle,        true };
                yield return new object[] { -0.0f,                      true };
                yield return new object[] {  float.NaN,                 true };
                yield return new object[] {  0.0f,                      false };
                yield return new object[] {  MaxSubnormalSingle,        false };
                yield return new object[] {  MinNormalSingle,           false };
                yield return new object[] {  float.MaxValue,            false };
                yield return new object[] {  float.PositiveInfinity,    false };
            }
        }

        public static IEnumerable<object[]> IsPositiveDouble
        {
            get
            {
                yield return new object[] {  double.NegativeInfinity,   false };
                yield return new object[] {  double.MinValue,           false };
                yield return new object[] { -MinNormalDouble,           false };
                yield return new object[] { -MaxSubnormalDouble,        false };
                yield return new object[] { -0.0,                       false };
                yield return new object[] {  double.NaN,                false };
                yield return new object[] {  0.0,                       true };
                yield return new object[] {  MaxSubnormalDouble,        true };
                yield return new object[] {  MinNormalDouble,           true };
                yield return new object[] {  double.MaxValue,           true };
                yield return new object[] {  double.PositiveInfinity,   true };
            }
        }

        public static IEnumerable<object[]> IsPositiveSingle
        {
            get
            {
                yield return new object[] {  float.NegativeInfinity,    false };
                yield return new object[] {  float.MinValue,            false };
                yield return new object[] { -MinNormalSingle,           false };
                yield return new object[] { -MaxSubnormalSingle,        false };
                yield return new object[] { -0.0f,                      false };
                yield return new object[] {  float.NaN,                 false };
                yield return new object[] {  0.0f,                      true };
                yield return new object[] {  MaxSubnormalSingle,        true };
                yield return new object[] {  MinNormalSingle,           true };
                yield return new object[] {  float.MaxValue,            true };
                yield return new object[] {  float.PositiveInfinity,    true };
            }
        }

        public static IEnumerable<object[]> IsPositiveInfinityDouble
        {
            get
            {
                yield return new object[] {  double.NegativeInfinity,   false };
                yield return new object[] {  double.MinValue,           false };
                yield return new object[] { -MinNormalDouble,           false };
                yield return new object[] { -MaxSubnormalDouble,        false };
                yield return new object[] { -double.Epsilon,            false };
                yield return new object[] { -0.0,                       false };
                yield return new object[] {  double.NaN,                false };
                yield return new object[] {  0.0,                       false };
                yield return new object[] {  double.Epsilon,            false };
                yield return new object[] {  MaxSubnormalDouble,        false };
                yield return new object[] {  MinNormalDouble,           false };
                yield return new object[] {  double.MaxValue,           false };
                yield return new object[] {  double.PositiveInfinity,   true };
            }
        }

        public static IEnumerable<object[]> IsPositiveInfinitySingle
        {
            get
            {
                yield return new object[] {  float.NegativeInfinity,    false };
                yield return new object[] {  float.MinValue,            false };
                yield return new object[] { -MinNormalSingle,           false };
                yield return new object[] { -MaxSubnormalSingle,        false };
                yield return new object[] { -float.Epsilon,             false };
                yield return new object[] { -0.0f,                      false };
                yield return new object[] {  float.NaN,                 false };
                yield return new object[] {  0.0f,                      false };
                yield return new object[] {  float.Epsilon,             false };
                yield return new object[] {  MaxSubnormalSingle,        false };
                yield return new object[] {  MinNormalSingle,           false };
                yield return new object[] {  float.MaxValue,            false };
                yield return new object[] {  float.PositiveInfinity,    true };
            }
        }

        public static IEnumerable<object[]> IsZeroDouble
        {
            get
            {
                yield return new object[] {  double.NegativeInfinity,   false };
                yield return new object[] {  double.MinValue,           false };
                yield return new object[] { -MinNormalDouble,           false };
                yield return new object[] { -MaxSubnormalDouble,        false };
                yield return new object[] { -double.Epsilon,            false };
                yield return new object[] { -0.0,                       true };
                yield return new object[] {  double.NaN,                false };
                yield return new object[] {  0.0,                       true };
                yield return new object[] {  double.Epsilon,            false };
                yield return new object[] {  MaxSubnormalDouble,        false };
                yield return new object[] {  MinNormalDouble,           false };
                yield return new object[] {  double.MaxValue,           false };
                yield return new object[] {  double.PositiveInfinity,   false };
            }
        }

        public static IEnumerable<object[]> IsZeroSingle
        {
            get
            {
                yield return new object[] {  float.NegativeInfinity,    false };
                yield return new object[] {  float.MinValue,            false };
                yield return new object[] { -MinNormalSingle,           false };
                yield return new object[] { -MaxSubnormalSingle,        false };
                yield return new object[] { -float.Epsilon,             false };
                yield return new object[] { -0.0f,                      true };
                yield return new object[] {  float.NaN,                 false };
                yield return new object[] {  0.0f,                      true };
                yield return new object[] {  float.Epsilon,             false };
                yield return new object[] {  MaxSubnormalSingle,        false };
                yield return new object[] {  MinNormalSingle,           false };
                yield return new object[] {  float.MaxValue,            false };
                yield return new object[] {  float.PositiveInfinity,    false };
            }
        }

        public static IEnumerable<object[]> LerpDouble
        {
            get
            {
                yield return new object[] { double.NegativeInfinity,    double.NegativeInfinity,    0.5,    double.NegativeInfinity };
                yield return new object[] { double.NegativeInfinity,    double.NaN,                 0.5,    double.NaN };
                yield return new object[] { double.NegativeInfinity,    double.PositiveInfinity,    0.5,    double.NaN };
                yield return new object[] { double.NegativeInfinity,    0.0,                        0.5,    double.NegativeInfinity };
                yield return new object[] { double.NegativeInfinity,    1.0,                        0.5,    double.NegativeInfinity };
                yield return new object[] { double.NaN,                 double.NegativeInfinity,    0.5,    double.NaN };
                yield return new object[] { double.NaN,                 double.NaN,                 0.5,    double.NaN };
                yield return new object[] { double.NaN,                 double.PositiveInfinity,    0.5,    double.NaN };
                yield return new object[] { double.NaN,                 0.0,                        0.5,    double.NaN };
                yield return new object[] { double.NaN,                 1.0,                        0.5,    double.NaN };
                yield return new object[] { double.PositiveInfinity,    double.NegativeInfinity,    0.5,    double.NaN };
                yield return new object[] { double.PositiveInfinity,    double.NaN,                 0.5,    double.NaN };
                yield return new object[] { double.PositiveInfinity,    double.PositiveInfinity,    0.5,    double.PositiveInfinity };
                yield return new object[] { double.PositiveInfinity,    0.0,                        0.5,    double.PositiveInfinity };
                yield return new object[] { double.PositiveInfinity,    1.0,                        0.5,    double.PositiveInfinity };
                yield return new object[] { 1.0,                        3.0,                        0.0,    1.0 };
                yield return new object[] { 1.0,                        3.0,                        0.5,    2.0 };
                yield return new object[] { 1.0,                        3.0,                        1.0,    3.0 };
                yield return new object[] { 1.0,                        3.0,                        2.0,    5.0 };
                yield return new object[] { 2.0,                        4.0,                        0.0,    2.0 };
                yield return new object[] { 2.0,                        4.0,                        0.5,    3.0 };
                yield return new object[] { 2.0,                        4.0,                        1.0,    4.0 };
                yield return new object[] { 2.0,                        4.0,                        2.0,    6.0 };
                yield return new object[] { 3.0,                        1.0,                        0.0,    3.0 };
                yield return new object[] { 3.0,                        1.0,                        0.5,    2.0 };
                yield return new object[] { 3.0,                        1.0,                        1.0,    1.0 };
                yield return new object[] { 3.0,                        1.0,                        2.0,   -1.0 };
                yield return new object[] { 4.0,                        2.0,                        0.0,    4.0 };
                yield return new object[] { 4.0,                        2.0,                        0.5,    3.0 };
                yield return new object[] { 4.0,                        2.0,                        1.0,    2.0 };
                yield return new object[] { 4.0,                        2.0,                        2.0,    0.0 };
            }
        }

        public static IEnumerable<object[]> LerpSingle
        {
            get
            {
                yield return new object[] { float.NegativeInfinity,    float.NegativeInfinity,    0.5f,    float.NegativeInfinity };
                yield return new object[] { float.NegativeInfinity,    float.NaN,                 0.5f,    float.NaN };
                yield return new object[] { float.NegativeInfinity,    float.PositiveInfinity,    0.5f,    float.NaN };
                yield return new object[] { float.NegativeInfinity,    0.0f,                      0.5f,    float.NegativeInfinity };
                yield return new object[] { float.NegativeInfinity,    1.0f,                      0.5f,    float.NegativeInfinity };
                yield return new object[] { float.NaN,                 float.NegativeInfinity,    0.5f,    float.NaN };
                yield return new object[] { float.NaN,                 float.NaN,                 0.5f,    float.NaN };
                yield return new object[] { float.NaN,                 float.PositiveInfinity,    0.5f,    float.NaN };
                yield return new object[] { float.NaN,                 0.0f,                      0.5f,    float.NaN };
                yield return new object[] { float.NaN,                 1.0f,                      0.5f,    float.NaN };
                yield return new object[] { float.PositiveInfinity,    float.NegativeInfinity,    0.5f,    float.NaN };
                yield return new object[] { float.PositiveInfinity,    float.NaN,                 0.5f,    float.NaN };
                yield return new object[] { float.PositiveInfinity,    float.PositiveInfinity,    0.5f,    float.PositiveInfinity };
                yield return new object[] { float.PositiveInfinity,    0.0f,                      0.5f,    float.PositiveInfinity };
                yield return new object[] { float.PositiveInfinity,    1.0f,                      0.5f,    float.PositiveInfinity };
                yield return new object[] { 1.0f,                      3.0f,                      0.0f,    1.0f };
                yield return new object[] { 1.0f,                      3.0f,                      0.5f,    2.0f };
                yield return new object[] { 1.0f,                      3.0f,                      1.0f,    3.0f };
                yield return new object[] { 1.0f,                      3.0f,                      2.0f,    5.0f };
                yield return new object[] { 2.0f,                      4.0f,                      0.0f,    2.0f };
                yield return new object[] { 2.0f,                      4.0f,                      0.5f,    3.0f };
                yield return new object[] { 2.0f,                      4.0f,                      1.0f,    4.0f };
                yield return new object[] { 2.0f,                      4.0f,                      2.0f,    6.0f };
                yield return new object[] { 3.0f,                      1.0f,                      0.0f,    3.0f };
                yield return new object[] { 3.0f,                      1.0f,                      0.5f,    2.0f };
                yield return new object[] { 3.0f,                      1.0f,                      1.0f,    1.0f };
                yield return new object[] { 3.0f,                      1.0f,                      2.0f,   -1.0f };
                yield return new object[] { 4.0f,                      2.0f,                      0.0f,    4.0f };
                yield return new object[] { 4.0f,                      2.0f,                      0.5f,    3.0f };
                yield return new object[] { 4.0f,                      2.0f,                      1.0f,    2.0f };
                yield return new object[] { 4.0f,                      2.0f,                      2.0f,    0.0f };
            }
        }

        public static IEnumerable<object[]> LogDouble
        {
            get
            {
                yield return new object[] { double.NegativeInfinity,  double.NaN,              0.0 };
                yield return new object[] { -3.1415926535897932,      double.NaN,              0.0 };                                     //                              value: -(pi)
                yield return new object[] { -2.7182818284590452,      double.NaN,              0.0 };                                     //                              value: -(e)
                yield return new object[] { -1.4142135623730950,      double.NaN,              0.0 };                                     //                              value: -(sqrt(2))
                yield return new object[] { -1.0,                     double.NaN,              0.0 };
                yield return new object[] { -0.69314718055994531,     double.NaN,              0.0 };                                     //                              value: -(ln(2))
                yield return new object[] { -0.43429448190325183,     double.NaN,              0.0 };                                     //                              value: -(log10(e))
                yield return new object[] { -0.0,                     double.NegativeInfinity, 0.0 };
                yield return new object[] { double.NaN,               double.NaN,              0.0 };
                yield return new object[] { 0.0,                      double.NegativeInfinity, 0.0 };
                yield return new object[] { 0.043213918263772250,    -3.1415926535897932,      DoubleCrossPlatformMachineEpsilon * 10 };  // expected: -(pi)
                yield return new object[] { 0.065988035845312537,    -2.7182818284590452,      DoubleCrossPlatformMachineEpsilon * 10 };  // expected: -(e)
                yield return new object[] { 0.1,                     -2.3025850929940457,      DoubleCrossPlatformMachineEpsilon * 10 };  // expected: -(ln(10))
                yield return new object[] { 0.20787957635076191,     -1.5707963267948966,      DoubleCrossPlatformMachineEpsilon * 10 };  // expected: -(pi / 2)
                yield return new object[] { 0.23629008834452270,     -1.4426950408889634,      DoubleCrossPlatformMachineEpsilon * 10 };  // expected: -(log2(e))
                yield return new object[] { 0.24311673443421421,     -1.4142135623730950,      DoubleCrossPlatformMachineEpsilon * 10 };  // expected: -(sqrt(2))
                yield return new object[] { 0.32355726390307110,     -1.1283791670955126,      DoubleCrossPlatformMachineEpsilon * 10 };  // expected: -(2 / sqrt(pi))
                yield return new object[] { 0.36787944117144232,     -1.0,                     0.0f };
                yield return new object[] { 0.45593812776599624,     -0.78539816339744831,     DoubleCrossPlatformMachineEpsilon };       // expected: -(pi / 4)
                yield return new object[] { 0.49306869139523979,     -0.70710678118654752,     DoubleCrossPlatformMachineEpsilon };       // expected: -(1 / sqrt(2))
                yield return new object[] { 0.5,                     -0.69314718055994531,     DoubleCrossPlatformMachineEpsilon };       // expected: -(ln(2))
                yield return new object[] { 0.52907780826773535,     -0.63661977236758134,     DoubleCrossPlatformMachineEpsilon };       // expected: -(2 / pi)
                yield return new object[] { 0.64772148514180065,     -0.43429448190325183,     DoubleCrossPlatformMachineEpsilon };       // expected: -(log10(e))
                yield return new object[] { 0.72737734929521647,     -0.31830988618379067,     DoubleCrossPlatformMachineEpsilon };       // expected: -(1 / pi)
                yield return new object[] { 1.0,                      0.0,                     0.0 };
                yield return new object[] { 1.3748022274393586,       0.31830988618379067,     DoubleCrossPlatformMachineEpsilon };       // expected:  (1 / pi)
                yield return new object[] { 1.5438734439711811,       0.43429448190325183,     DoubleCrossPlatformMachineEpsilon };       // expected:  (log10(e))
                yield return new object[] { 1.8900811645722220,       0.63661977236758134,     DoubleCrossPlatformMachineEpsilon };       // expected:  (2 / pi)
                yield return new object[] { 2.0,                      0.69314718055994531,     DoubleCrossPlatformMachineEpsilon };       // expected:  (ln(2))
                yield return new object[] { 2.0281149816474725,       0.70710678118654752,     DoubleCrossPlatformMachineEpsilon };       // expected:  (1 / sqrt(2))
                yield return new object[] { 2.1932800507380155,       0.78539816339744831,     DoubleCrossPlatformMachineEpsilon };       // expected:  (pi / 4)
                yield return new object[] { 2.7182818284590452,       1.0,                     DoubleCrossPlatformMachineEpsilon * 10 };  //                              value: (e)
                yield return new object[] { 3.0906430223107976,       1.1283791670955126,      DoubleCrossPlatformMachineEpsilon * 10 };  // expected:  (2 / sqrt(pi))
                yield return new object[] { 4.1132503787829275,       1.4142135623730950,      DoubleCrossPlatformMachineEpsilon * 10 };  // expected:  (sqrt(2))
                yield return new object[] { 4.2320861065570819,       1.4426950408889634,      DoubleCrossPlatformMachineEpsilon * 10 };  // expected:  (log2(e))
                yield return new object[] { 4.8104773809653517,       1.5707963267948966,      DoubleCrossPlatformMachineEpsilon * 10 };  // expected:  (pi / 2)
                yield return new object[] { 10.0,                     2.3025850929940457,      DoubleCrossPlatformMachineEpsilon * 10 };  // expected:  (ln(10))
                yield return new object[] { 15.154262241479264,       2.7182818284590452,      DoubleCrossPlatformMachineEpsilon * 10 };  // expected:  (e)
                yield return new object[] { 23.140692632779269,       3.1415926535897932,      DoubleCrossPlatformMachineEpsilon * 10 };  // expected:  (pi)
                yield return new object[] { double.PositiveInfinity,  double.PositiveInfinity, 0.0 };
            }
        }

        public static IEnumerable<object[]> LogSingle
        {
            get
            {
                yield return new object[] { float.NegativeInfinity,  float.NaN,              0.0f };
                yield return new object[] { -3.14159265f,            float.NaN,              0.0f };                                      //                               value: -(pi)
                yield return new object[] { -2.71828183f,            float.NaN,              0.0f };                                      //                               value: -(e)
                yield return new object[] { -1.41421356f,            float.NaN,              0.0f };                                      //                               value: -(sqrt(2))
                yield return new object[] { -1.0f,                   float.NaN,              0.0f };
                yield return new object[] { -0.693147181f,           float.NaN,              0.0f };                                      //                               value: -(ln(2))
                yield return new object[] { -0.434294482f,           float.NaN,              0.0f };                                      //                               value: -(log10(e))
                yield return new object[] { -0.0f,                   float.NegativeInfinity, 0.0f };
                yield return new object[] { float.NaN,               float.NaN,              0.0f };
                yield return new object[] { 0.0f,                    float.NegativeInfinity, 0.0f };
                yield return new object[] { 0.0432139183f,          -3.14159265f,            SingleCrossPlatformMachineEpsilon * 10 };    // expected: -(pi)
                yield return new object[] { 0.0659880358f,          -2.71828183f,            SingleCrossPlatformMachineEpsilon * 10 };    // expected: -(e)
                yield return new object[] { 0.1f,                   -2.30258509f,            SingleCrossPlatformMachineEpsilon * 10 };    // expected: -(ln(10))
                yield return new object[] { 0.207879576f,           -1.57079633f,            SingleCrossPlatformMachineEpsilon * 10 };    // expected: -(pi / 2)
                yield return new object[] { 0.236290088f,           -1.44269504f,            SingleCrossPlatformMachineEpsilon * 10 };    // expected: -(log2(e))
                yield return new object[] { 0.243116734f,           -1.41421356f,            SingleCrossPlatformMachineEpsilon * 10 };    // expected: -(sqrt(2))
                yield return new object[] { 0.323557264f,           -1.12837917f,            SingleCrossPlatformMachineEpsilon * 10 };    // expected: -(2 / sqrt(pi))
                yield return new object[] { 0.367879441f,           -1.0f,                   0.0f };
                yield return new object[] { 0.455938128f,           -0.785398163f,           SingleCrossPlatformMachineEpsilon };         // expected: -(pi / 4)
                yield return new object[] { 0.493068691f,           -0.707106781f,           SingleCrossPlatformMachineEpsilon };         // expected: -(1 / sqrt(2))
                yield return new object[] { 0.5f,                   -0.693147181f,           SingleCrossPlatformMachineEpsilon };         // expected: -(ln(2))
                yield return new object[] { 0.529077808f,           -0.636619772f,           SingleCrossPlatformMachineEpsilon };         // expected: -(2 / pi)
                yield return new object[] { 0.647721485f,           -0.434294482f,           SingleCrossPlatformMachineEpsilon };         // expected: -(log10(e))
                yield return new object[] { 0.727377349f,           -0.318309886f,           SingleCrossPlatformMachineEpsilon };         // expected: -(1 / pi)
                yield return new object[] { 1.0f,                    0.0f,                   0.0f };
                yield return new object[] { 1.37480223f,             0.318309886f,           SingleCrossPlatformMachineEpsilon };         // expected:  (1 / pi)
                yield return new object[] { 1.54387344f,             0.434294482f,           SingleCrossPlatformMachineEpsilon };         // expected:  (log10(e))
                yield return new object[] { 1.89008116f,             0.636619772f,           SingleCrossPlatformMachineEpsilon };         // expected:  (2 / pi)
                yield return new object[] { 2.0f,                    0.693147181f,           SingleCrossPlatformMachineEpsilon };         // expected:  (ln(2))
                yield return new object[] { 2.02811498f,             0.707106781f,           SingleCrossPlatformMachineEpsilon };         // expected:  (1 / sqrt(2))
                yield return new object[] { 2.19328005f,             0.785398163f,           SingleCrossPlatformMachineEpsilon };         // expected:  (pi / 4)
                yield return new object[] { 2.71828183f,             1.0f,                   SingleCrossPlatformMachineEpsilon * 10 };    //                              value: (e)
                yield return new object[] { 3.09064302f,             1.12837917f,            SingleCrossPlatformMachineEpsilon * 10 };    // expected:  (2 / sqrt(pi))
                yield return new object[] { 4.11325038f,             1.41421356f,            SingleCrossPlatformMachineEpsilon * 10 };    // expected:  (sqrt(2))
                yield return new object[] { 4.23208611f,             1.44269504f,            SingleCrossPlatformMachineEpsilon * 10 };    // expected:  (log2(e))
                yield return new object[] { 4.81047738f,             1.57079633f,            SingleCrossPlatformMachineEpsilon * 10 };    // expected:  (pi / 2)
                yield return new object[] { 10.0f,                   2.30258509f,            SingleCrossPlatformMachineEpsilon * 10 };    // expected:  (ln(10))
                yield return new object[] { 15.1542622f,             2.71828183f,            SingleCrossPlatformMachineEpsilon * 10 };    // expected:  (e)
                yield return new object[] { 23.1406926f,             3.14159265f,            SingleCrossPlatformMachineEpsilon * 10 };    // expected:  (pi)
                yield return new object[] { float.PositiveInfinity,  float.PositiveInfinity, 0.0f };
            }
        }

        public static IEnumerable<object[]> Log2Double
        {
            get
            {
                yield return new object[] { double.NegativeInfinity,  double.NaN,              0.0 };
                yield return new object[] {-0.11331473229676087,      double.NaN,              0.0 };
                yield return new object[] {-0.0,                      double.NegativeInfinity, 0.0 };
                yield return new object[] { double.NaN,               double.NaN,              0.0 };
                yield return new object[] { 0.0,                      double.NegativeInfinity, 0.0 };
                yield return new object[] { 0.11331473229676087,     -3.1415926535897932,      DoubleCrossPlatformMachineEpsilon * 10 };    // expected: -(pi)
                yield return new object[] { 0.15195522325791297,     -2.7182818284590453,      DoubleCrossPlatformMachineEpsilon * 10 };    // expected: -(e)
                yield return new object[] { 0.20269956628651730,     -2.3025850929940460,      DoubleCrossPlatformMachineEpsilon * 10 };    // expected: -(ln(10))
                yield return new object[] { 0.33662253682241906,     -1.5707963267948966,      DoubleCrossPlatformMachineEpsilon * 10 };    // expected: -(pi / 2)
                yield return new object[] { 0.36787944117144232,     -1.4426950408889634,      DoubleCrossPlatformMachineEpsilon * 10 };    // expected: -(log2(e))
                yield return new object[] { 0.37521422724648177,     -1.4142135623730950,      DoubleCrossPlatformMachineEpsilon * 10 };    // expected: -(sqrt(2))
                yield return new object[] { 0.45742934732229695,     -1.1283791670955126,      DoubleCrossPlatformMachineEpsilon * 10 };    // expected: -(2 / sqrt(pi))
                yield return new object[] { 0.5,                     -1.0,                     0.0f };
                yield return new object[] { 0.58019181037172444,     -0.78539816339744840,     DoubleCrossPlatformMachineEpsilon };         // expected: -(pi / 4)
                yield return new object[] { 0.61254732653606592,     -0.70710678118654750,     DoubleCrossPlatformMachineEpsilon };         // expected: -(1 / sqrt(2))
                yield return new object[] { 0.61850313780157598,     -0.69314718055994537,     DoubleCrossPlatformMachineEpsilon };         // expected: -(ln(2))
                yield return new object[] { 0.64321824193300488,     -0.63661977236758126,     DoubleCrossPlatformMachineEpsilon };         // expected: -(2 / pi)
                yield return new object[] { 0.74005557395545179,     -0.43429448190325190,     DoubleCrossPlatformMachineEpsilon };         // expected: -(log10(e))
                yield return new object[] { 0.80200887896145195,     -0.31830988618379073,     DoubleCrossPlatformMachineEpsilon };         // expected: -(1 / pi)
                yield return new object[] { 1,                        0.0,                     0.0 };                        
                yield return new object[] { 1.2468689889006383,       0.31830988618379073,     DoubleCrossPlatformMachineEpsilon };         // expected:  (1 / pi)
                yield return new object[] { 1.3512498725672678,       0.43429448190325226,     DoubleCrossPlatformMachineEpsilon };         // expected:  (log10(e))
                yield return new object[] { 1.5546822754821001,       0.63661977236758126,     DoubleCrossPlatformMachineEpsilon };         // expected:  (2 / pi)
                yield return new object[] { 1.6168066722416747,       0.69314718055994537,     DoubleCrossPlatformMachineEpsilon };         // expected:  (ln(2))
                yield return new object[] { 1.6325269194381528,       0.70710678118654750,     DoubleCrossPlatformMachineEpsilon };         // expected:  (1 / sqrt(2))
                yield return new object[] { 1.7235679341273495,       0.78539816339744830,     DoubleCrossPlatformMachineEpsilon };         // expected:  (pi / 4)
                yield return new object[] { 2,                        1.0,                     0.0 };                                       //                              value: (e)
                yield return new object[] { 2.1861299583286618,       1.1283791670955128,      DoubleCrossPlatformMachineEpsilon * 10 };    // expected:  (2 / sqrt(pi))
                yield return new object[] { 2.6651441426902252,       1.4142135623730950,      DoubleCrossPlatformMachineEpsilon * 10 };    // expected:  (sqrt(2))
                yield return new object[] { 2.7182818284590452,       1.4426950408889632,      DoubleCrossPlatformMachineEpsilon * 10 };    // expected:  (log2(e))
                yield return new object[] { 2.9706864235520193,       1.5707963267948966,      DoubleCrossPlatformMachineEpsilon * 10 };    // expected:  (pi / 2)
                yield return new object[] { 4.9334096679145963,       2.3025850929940460,      DoubleCrossPlatformMachineEpsilon * 10 };    // expected:  (ln(10))
                yield return new object[] { 6.5808859910179210,       2.7182818284590455,      DoubleCrossPlatformMachineEpsilon * 10 };    // expected:  (e)
                yield return new object[] { 8.8249778270762876,       3.1415926535897932,      DoubleCrossPlatformMachineEpsilon * 10 };    // expected:  (pi)
                yield return new object[] { double.PositiveInfinity,  double.PositiveInfinity, 0.0 };
            }
        }

        public static IEnumerable<object[]> Log2Single
        {
            get
            {
                yield return new object[] { float.NegativeInfinity,  float.NaN,              0.0f };
                yield return new object[] { -0.113314732f,           float.NaN,              0.0f };
                yield return new object[] { -0.0f,                   float.NegativeInfinity, 0.0f };
                yield return new object[] { float.NaN,               float.NaN,              0.0f };
                yield return new object[] { 0.0f,                    float.NegativeInfinity, 0.0f };
                yield return new object[] { 0.113314732f,           -3.14159265f,            SingleCrossPlatformMachineEpsilon * 10 };  // expected: -(pi)
                yield return new object[] { 0.151955223f,           -2.71828200f,            SingleCrossPlatformMachineEpsilon * 10 };  // expected: -(e)
                yield return new object[] { 0.202699566f,           -2.30258509f,            SingleCrossPlatformMachineEpsilon * 10 };  // expected: -(ln(10))
                yield return new object[] { 0.336622537f,           -1.57079630f,            SingleCrossPlatformMachineEpsilon * 10 };  // expected: -(pi / 2)
                yield return new object[] { 0.367879441f,           -1.44269500f,            SingleCrossPlatformMachineEpsilon * 10 };  // expected: -(log2(e))
                yield return new object[] { 0.375214227f,           -1.41421360f,            SingleCrossPlatformMachineEpsilon * 10 };  // expected: -(sqrt(2))
                yield return new object[] { 0.457429347f,           -1.12837910f,            SingleCrossPlatformMachineEpsilon * 10 };  // expected: -(2 / sqrt(pi))
                yield return new object[] { 0.5f,                   -1.0f,                   0.0f };
                yield return new object[] { 0.580191810f,           -0.785398211f,           SingleCrossPlatformMachineEpsilon };       // expected: -(pi / 4)
                yield return new object[] { 0.612547327f,           -0.707106700f,           SingleCrossPlatformMachineEpsilon };       // expected: -(1 / sqrt(2))
                yield return new object[] { 0.618503138f,           -0.693147144f,           SingleCrossPlatformMachineEpsilon };       // expected: -(ln(2))
                yield return new object[] { 0.643218242f,           -0.636619823f,           SingleCrossPlatformMachineEpsilon };       // expected: -(2 / pi)
                yield return new object[] { 0.740055574f,           -0.434294550f,           SingleCrossPlatformMachineEpsilon };       // expected: -(log10(e))
                yield return new object[] { 0.802008879f,           -0.318309900f,           SingleCrossPlatformMachineEpsilon };       // expected: -(1 / pi)
                yield return new object[] { 1,                       0.0f,                   0.0f };
                yield return new object[] { 1.24686899f,             0.318309870f,           SingleCrossPlatformMachineEpsilon };       // expected:  (1 / pi)
                yield return new object[] { 1.35124987f,             0.434294340f,           SingleCrossPlatformMachineEpsilon };       // expected:  (log10(e))
                yield return new object[] { 1.55468228f,             0.636619823f,           SingleCrossPlatformMachineEpsilon };       // expected:  (2 / pi)
                yield return new object[] { 1.61680667f,             0.693147144f,           SingleCrossPlatformMachineEpsilon };       // expected:  (ln(2))
                yield return new object[] { 1.63252692f,             0.707106700f,           SingleCrossPlatformMachineEpsilon };       // expected:  (1 / sqrt(2))
                yield return new object[] { 1.72356793f,             0.785398211f,           SingleCrossPlatformMachineEpsilon };       // expected:  (pi / 4)
                yield return new object[] { 2,                       1.0f,                   0.0f };                                    //                              value: (e)
                yield return new object[] { 2.18612996f,             1.12837920f,            SingleCrossPlatformMachineEpsilon * 10 };  // expected:  (2 / sqrt(pi))
                yield return new object[] { 2.66514414f,             1.41421360f,            SingleCrossPlatformMachineEpsilon * 10 };  // expected:  (sqrt(2))
                yield return new object[] { 2.71828183f,             1.44269490f,            SingleCrossPlatformMachineEpsilon * 10 };  // expected:  (log2(e))
                yield return new object[] { 2.97068642f,             1.57079630f,            SingleCrossPlatformMachineEpsilon * 10 };  // expected:  (pi / 2)
                yield return new object[] { 4.93340967f,             2.30258509f,            SingleCrossPlatformMachineEpsilon * 10 };  // expected:  (ln(10))
                yield return new object[] { 6.58088599f,             2.71828170f,            SingleCrossPlatformMachineEpsilon * 10 };  // expected:  (e)
                yield return new object[] { 8.82497783f,             3.14159265f,            SingleCrossPlatformMachineEpsilon * 10 };  // expected:  (pi)
                yield return new object[] { float.PositiveInfinity,  float.PositiveInfinity, 0.0f };
            }
        }

        public static IEnumerable<object[]> MaxDouble
        {
            get
            {
                yield return new object[] {  double.NegativeInfinity,    double.PositiveInfinity,   double.PositiveInfinity };
                yield return new object[] {  double.PositiveInfinity,    double.NegativeInfinity,   double.PositiveInfinity };
                yield return new object[] {  double.MinValue,            double.MaxValue,           double.MaxValue };
                yield return new object[] {  double.MaxValue,            double.MinValue,           double.MaxValue };
                yield return new object[] {  double.NaN,                 double.NaN,                double.NaN };
                yield return new object[] {  double.NaN,                 1.0f,                      double.NaN };
                yield return new object[] {  1.0f,                       double.NaN,                double.NaN };
                yield return new object[] {  double.PositiveInfinity,    double.NaN,                double.NaN };
                yield return new object[] {  double.NegativeInfinity,    double.NaN,                double.NaN };
                yield return new object[] {  double.NaN,                 double.PositiveInfinity,   double.NaN };
                yield return new object[] {  double.NaN,                 double.NegativeInfinity,   double.NaN };
                yield return new object[] { -0.0f,                       0.0f,                      0.0f };
                yield return new object[] {  0.0f,                      -0.0f,                      0.0f };
                yield return new object[] {  2.0f,                      -3.0f,                      2.0f };
                yield return new object[] { -3.0f,                       2.0f,                      2.0f };
                yield return new object[] {  3.0f,                      -2.0f,                      3.0f };
                yield return new object[] { -2.0f,                       3.0f,                      3.0f };
            }
        }

        public static IEnumerable<object[]> MaxSingle
        {
            get
            {
                yield return new object[] {  float.NegativeInfinity,     float.PositiveInfinity,    float.PositiveInfinity };
                yield return new object[] {  float.PositiveInfinity,     float.NegativeInfinity,    float.PositiveInfinity };
                yield return new object[] {  float.MinValue,             float.MaxValue,            float.MaxValue };
                yield return new object[] {  float.MaxValue,             float.MinValue,            float.MaxValue };
                yield return new object[] {  float.NaN,                  float.NaN,                 float.NaN };
                yield return new object[] {  float.NaN,                  1.0f,                      float.NaN };
                yield return new object[] {  1.0f,                       float.NaN,                 float.NaN };
                yield return new object[] {  float.PositiveInfinity,     float.NaN,                 float.NaN };
                yield return new object[] {  float.NegativeInfinity,     float.NaN,                 float.NaN };
                yield return new object[] {  float.NaN,                  float.PositiveInfinity,    float.NaN };
                yield return new object[] {  float.NaN,                  float.NegativeInfinity,    float.NaN };
                yield return new object[] { -0.0f,                       0.0f,                      0.0f };
                yield return new object[] {  0.0f,                      -0.0f,                      0.0f };
                yield return new object[] {  2.0f,                      -3.0f,                      2.0f };
                yield return new object[] { -3.0f,                       2.0f,                      2.0f };
                yield return new object[] {  3.0f,                      -2.0f,                      3.0f };
                yield return new object[] { -2.0f,                       3.0f,                      3.0f };
            }
        }

        public static IEnumerable<object[]> MaxMagnitudeDouble
        {
            get
            {
                yield return new object[] {  double.NegativeInfinity,    double.PositiveInfinity,    double.PositiveInfinity };
                yield return new object[] {  double.PositiveInfinity,    double.NegativeInfinity,    double.PositiveInfinity };
                yield return new object[] {  double.MinValue,            double.MaxValue,            double.MaxValue };
                yield return new object[] {  double.MaxValue,            double.MinValue,            double.MaxValue };
                yield return new object[] {  double.NaN,                 double.NaN,                 double.NaN };
                yield return new object[] {  double.NaN,                 1.0f,                       double.NaN };
                yield return new object[] {  1.0f,                       double.NaN,                 double.NaN };
                yield return new object[] {  double.PositiveInfinity,    double.NaN,                 double.NaN };
                yield return new object[] {  double.NegativeInfinity,    double.NaN,                 double.NaN };
                yield return new object[] {  double.NaN,                 double.PositiveInfinity,    double.NaN };
                yield return new object[] {  double.NaN,                 double.NegativeInfinity,    double.NaN };
                yield return new object[] { -0.0f,                       0.0f,                       0.0f };
                yield return new object[] {  0.0f,                      -0.0f,                       0.0f };
                yield return new object[] {  2.0f,                      -3.0f,                      -3.0f };
                yield return new object[] { -3.0f,                       2.0f,                      -3.0f };
                yield return new object[] {  3.0f,                      -2.0f,                       3.0f };
                yield return new object[] { -2.0f,                       3.0f,                       3.0f };
            }
        }

        public static IEnumerable<object[]> MaxMagnitudeSingle
        {
            get
            {
                yield return new object[] {  float.NegativeInfinity,     float.PositiveInfinity,     float.PositiveInfinity };
                yield return new object[] {  float.PositiveInfinity,     float.NegativeInfinity,     float.PositiveInfinity };
                yield return new object[] {  float.MinValue,             float.MaxValue,             float.MaxValue };
                yield return new object[] {  float.MaxValue,             float.MinValue,             float.MaxValue };
                yield return new object[] {  float.NaN,                  float.NaN,                  float.NaN };
                yield return new object[] {  float.NaN,                  1.0f,                       float.NaN };
                yield return new object[] {  1.0f,                       float.NaN,                  float.NaN };
                yield return new object[] {  float.PositiveInfinity,     float.NaN,                  float.NaN };
                yield return new object[] {  float.NegativeInfinity,     float.NaN,                  float.NaN };
                yield return new object[] {  float.NaN,                  float.PositiveInfinity,     float.NaN };
                yield return new object[] {  float.NaN,                  float.NegativeInfinity,     float.NaN };
                yield return new object[] { -0.0f,                       0.0f,                       0.0f };
                yield return new object[] {  0.0f,                      -0.0f,                       0.0f };
                yield return new object[] {  2.0f,                      -3.0f,                      -3.0f };
                yield return new object[] { -3.0f,                       2.0f,                      -3.0f };
                yield return new object[] {  3.0f,                      -2.0f,                       3.0f };
                yield return new object[] { -2.0f,                       3.0f,                       3.0f };
            }
        }

        public static IEnumerable<object[]> MaxMagnitudeNumberDouble
        {
            get
            {
                yield return new object[] {  double.NegativeInfinity,    double.PositiveInfinity,    double.PositiveInfinity };
                yield return new object[] {  double.PositiveInfinity,    double.NegativeInfinity,    double.PositiveInfinity };
                yield return new object[] {  double.MinValue,            double.MaxValue,            double.MaxValue };
                yield return new object[] {  double.MaxValue,            double.MinValue,            double.MaxValue };
                yield return new object[] {  double.NaN,                 double.NaN,                 double.NaN };
                yield return new object[] {  double.NaN,                 1.0f,                       1.0f };
                yield return new object[] {  1.0f,                       double.NaN,                 1.0f };
                yield return new object[] {  double.PositiveInfinity,    double.NaN,                 double.PositiveInfinity };
                yield return new object[] {  double.NegativeInfinity,    double.NaN,                 double.NegativeInfinity };
                yield return new object[] {  double.NaN,                 double.PositiveInfinity,    double.PositiveInfinity };
                yield return new object[] {  double.NaN,                 double.NegativeInfinity,    double.NegativeInfinity };
                yield return new object[] { -0.0f,                       0.0f,                       0.0f };
                yield return new object[] {  0.0f,                      -0.0f,                       0.0f };
                yield return new object[] {  2.0f,                      -3.0f,                      -3.0f };
                yield return new object[] { -3.0f,                       2.0f,                      -3.0f };
                yield return new object[] {  3.0f,                      -2.0f,                       3.0f };
                yield return new object[] { -2.0f,                       3.0f,                       3.0f };
            }
        }

        public static IEnumerable<object[]> MaxMagnitudeNumberSingle
        {
            get
            {
                yield return new object[] {  float.NegativeInfinity,     float.PositiveInfinity,     float.PositiveInfinity };
                yield return new object[] {  float.PositiveInfinity,     float.NegativeInfinity,     float.PositiveInfinity };
                yield return new object[] {  float.MinValue,             float.MaxValue,             float.MaxValue };
                yield return new object[] {  float.MaxValue,             float.MinValue,             float.MaxValue };
                yield return new object[] {  float.NaN,                  float.NaN,                  float.NaN };
                yield return new object[] {  float.NaN,                  1.0f,                       1.0f };
                yield return new object[] {  1.0f,                       float.NaN,                  1.0f };
                yield return new object[] {  float.PositiveInfinity,     float.NaN,                  float.PositiveInfinity };
                yield return new object[] {  float.NegativeInfinity,     float.NaN,                  float.NegativeInfinity };
                yield return new object[] {  float.NaN,                  float.PositiveInfinity,     float.PositiveInfinity };
                yield return new object[] {  float.NaN,                  float.NegativeInfinity,     float.NegativeInfinity };
                yield return new object[] { -0.0f,                       0.0f,                       0.0f };
                yield return new object[] {  0.0f,                      -0.0f,                       0.0f };
                yield return new object[] {  2.0f,                      -3.0f,                      -3.0f };
                yield return new object[] { -3.0f,                       2.0f,                      -3.0f };
                yield return new object[] {  3.0f,                      -2.0f,                       3.0f };
                yield return new object[] { -2.0f,                       3.0f,                       3.0f };
            }
        }

        public static IEnumerable<object[]> MaxNumberDouble
        {
            get
            {
                yield return new object[] {  double.NegativeInfinity,    double.PositiveInfinity,   double.PositiveInfinity };
                yield return new object[] {  double.PositiveInfinity,    double.NegativeInfinity,   double.PositiveInfinity };
                yield return new object[] {  double.MinValue,            double.MaxValue,           double.MaxValue };
                yield return new object[] {  double.MaxValue,            double.MinValue,           double.MaxValue };
                yield return new object[] {  double.NaN,                 double.NaN,                double.NaN };
                yield return new object[] {  double.NaN,                 1.0f,                      1.0f };
                yield return new object[] {  1.0f,                       double.NaN,                1.0f };
                yield return new object[] {  double.PositiveInfinity,    double.NaN,                double.PositiveInfinity };
                yield return new object[] {  double.NegativeInfinity,    double.NaN,                double.NegativeInfinity };
                yield return new object[] {  double.NaN,                 double.PositiveInfinity,   double.PositiveInfinity };
                yield return new object[] {  double.NaN,                 double.NegativeInfinity,   double.NegativeInfinity };
                yield return new object[] { -0.0f,                       0.0f,                      0.0f };
                yield return new object[] {  0.0f,                      -0.0f,                      0.0f };
                yield return new object[] {  2.0f,                      -3.0f,                      2.0f };
                yield return new object[] { -3.0f,                       2.0f,                      2.0f };
                yield return new object[] {  3.0f,                      -2.0f,                      3.0f };
                yield return new object[] { -2.0f,                       3.0f,                      3.0f };
            }
        }

        public static IEnumerable<object[]> MaxNumberSingle
        {
            get
            {
                yield return new object[] {  float.NegativeInfinity,     float.PositiveInfinity,    float.PositiveInfinity };
                yield return new object[] {  float.PositiveInfinity,     float.NegativeInfinity,    float.PositiveInfinity };
                yield return new object[] {  float.MinValue,             float.MaxValue,            float.MaxValue };
                yield return new object[] {  float.MaxValue,             float.MinValue,            float.MaxValue };
                yield return new object[] {  float.NaN,                  float.NaN,                 float.NaN };
                yield return new object[] {  float.NaN,                  1.0f,                      1.0f };
                yield return new object[] {  1.0f,                       float.NaN,                 1.0f };
                yield return new object[] {  float.PositiveInfinity,     float.NaN,                 float.PositiveInfinity };
                yield return new object[] {  float.NegativeInfinity,     float.NaN,                 float.NegativeInfinity };
                yield return new object[] {  float.NaN,                  float.PositiveInfinity,    float.PositiveInfinity };
                yield return new object[] {  float.NaN,                  float.NegativeInfinity,    float.NegativeInfinity };
                yield return new object[] { -0.0f,                       0.0f,                      0.0f };
                yield return new object[] {  0.0f,                      -0.0f,                      0.0f };
                yield return new object[] {  2.0f,                      -3.0f,                      2.0f };
                yield return new object[] { -3.0f,                       2.0f,                      2.0f };
                yield return new object[] {  3.0f,                      -2.0f,                      3.0f };
                yield return new object[] { -2.0f,                       3.0f,                      3.0f };
            }
        }

        public static IEnumerable<object[]> MinDouble
        {
            get
            {
                yield return new object[] {  double.NegativeInfinity,    double.PositiveInfinity,    double.NegativeInfinity };
                yield return new object[] {  double.PositiveInfinity,    double.NegativeInfinity,    double.NegativeInfinity };
                yield return new object[] {  double.MinValue,            double.MaxValue,            double.MinValue };
                yield return new object[] {  double.MaxValue,            double.MinValue,            double.MinValue };
                yield return new object[] {  double.NaN,                 double.NaN,                 double.NaN };
                yield return new object[] {  double.NaN,                 1.0f,                       double.NaN };
                yield return new object[] {  1.0f,                       double.NaN,                 double.NaN };
                yield return new object[] {  double.PositiveInfinity,    double.NaN,                 double.NaN };
                yield return new object[] {  double.NegativeInfinity,    double.NaN,                 double.NaN };
                yield return new object[] {  double.NaN,                 double.PositiveInfinity,    double.NaN };
                yield return new object[] {  double.NaN,                 double.NegativeInfinity,    double.NaN };
                yield return new object[] { -0.0f,                       0.0f,                      -0.0f };
                yield return new object[] {  0.0f,                      -0.0f,                      -0.0f };
                yield return new object[] {  2.0f,                      -3.0f,                      -3.0f };
                yield return new object[] { -3.0f,                       2.0f,                      -3.0f };
                yield return new object[] {  3.0f,                      -2.0f,                      -2.0f };
                yield return new object[] { -2.0f,                       3.0f,                      -2.0f };
            }
        }

        public static IEnumerable<object[]> MinSingle
        {
            get
            {
                yield return new object[] {  float.NegativeInfinity,     float.PositiveInfinity,     float.NegativeInfinity };
                yield return new object[] {  float.PositiveInfinity,     float.NegativeInfinity,     float.NegativeInfinity };
                yield return new object[] {  float.MinValue,             float.MaxValue,             float.MinValue };
                yield return new object[] {  float.MaxValue,             float.MinValue,             float.MinValue };
                yield return new object[] {  float.NaN,                  float.NaN,                  float.NaN };
                yield return new object[] {  float.NaN,                  1.0f,                       float.NaN };
                yield return new object[] {  1.0f,                       float.NaN,                  float.NaN };
                yield return new object[] {  float.PositiveInfinity,     float.NaN,                  float.NaN };
                yield return new object[] {  float.NegativeInfinity,     float.NaN,                  float.NaN };
                yield return new object[] {  float.NaN,                  float.PositiveInfinity,     float.NaN };
                yield return new object[] {  float.NaN,                  float.NegativeInfinity,     float.NaN };
                yield return new object[] { -0.0f,                       0.0f,                      -0.0f };
                yield return new object[] {  0.0f,                      -0.0f,                      -0.0f };
                yield return new object[] {  2.0f,                      -3.0f,                      -3.0f };
                yield return new object[] { -3.0f,                       2.0f,                      -3.0f };
                yield return new object[] {  3.0f,                      -2.0f,                      -2.0f };
                yield return new object[] { -2.0f,                       3.0f,                      -2.0f };
            }
        }

        public static IEnumerable<object[]> MinMagnitudeDouble
        {
            get
            {
                yield return new object[] {  double.NegativeInfinity,    double.PositiveInfinity,    double.NegativeInfinity };
                yield return new object[] {  double.PositiveInfinity,    double.NegativeInfinity,    double.NegativeInfinity };
                yield return new object[] {  double.MinValue,            double.MaxValue,            double.MinValue };
                yield return new object[] {  double.MaxValue,            double.MinValue,            double.MinValue };
                yield return new object[] {  double.NaN,                 double.NaN,                 double.NaN };
                yield return new object[] {  double.NaN,                 1.0f,                       double.NaN };
                yield return new object[] {  1.0f,                       double.NaN,                 double.NaN };
                yield return new object[] {  double.PositiveInfinity,    double.NaN,                 double.NaN };
                yield return new object[] {  double.NegativeInfinity,    double.NaN,                 double.NaN };
                yield return new object[] {  double.NaN,                 double.PositiveInfinity,    double.NaN };
                yield return new object[] {  double.NaN,                 double.NegativeInfinity,    double.NaN };
                yield return new object[] { -0.0f,                       0.0f,                      -0.0f };
                yield return new object[] {  0.0f,                      -0.0f,                      -0.0f };
                yield return new object[] {  2.0f,                      -3.0f,                       2.0f };
                yield return new object[] { -3.0f,                       2.0f,                       2.0f };
                yield return new object[] {  3.0f,                      -2.0f,                      -2.0f };
                yield return new object[] { -2.0f,                       3.0f,                      -2.0f };
            }
        }

        public static IEnumerable<object[]> MinMagnitudeSingle
        {
            get
            {
                yield return new object[] {  float.NegativeInfinity,     float.PositiveInfinity,     float.NegativeInfinity };
                yield return new object[] {  float.PositiveInfinity,     float.NegativeInfinity,     float.NegativeInfinity };
                yield return new object[] {  float.MinValue,             float.MaxValue,             float.MinValue };
                yield return new object[] {  float.MaxValue,             float.MinValue,             float.MinValue };
                yield return new object[] {  float.NaN,                  float.NaN,                  float.NaN };
                yield return new object[] {  float.NaN,                  1.0f,                       float.NaN };
                yield return new object[] {  1.0f,                       float.NaN,                  float.NaN };
                yield return new object[] {  float.PositiveInfinity,     float.NaN,                  float.NaN };
                yield return new object[] {  float.NegativeInfinity,     float.NaN,                  float.NaN };
                yield return new object[] {  float.NaN,                  float.PositiveInfinity,     float.NaN };
                yield return new object[] {  float.NaN,                  float.NegativeInfinity,     float.NaN };
                yield return new object[] { -0.0f,                       0.0f,                      -0.0f };
                yield return new object[] {  0.0f,                      -0.0f,                      -0.0f };
                yield return new object[] {  2.0f,                      -3.0f,                       2.0f };
                yield return new object[] { -3.0f,                       2.0f,                       2.0f };
                yield return new object[] {  3.0f,                      -2.0f,                      -2.0f };
                yield return new object[] { -2.0f,                       3.0f,                      -2.0f };
            }
        }

        public static IEnumerable<object[]> MinMagnitudeNumberDouble
        {
            get
            {
                yield return new object[] {  double.NegativeInfinity,    double.PositiveInfinity,    double.NegativeInfinity };
                yield return new object[] {  double.PositiveInfinity,    double.NegativeInfinity,    double.NegativeInfinity };
                yield return new object[] {  double.MinValue,            double.MaxValue,            double.MinValue };
                yield return new object[] {  double.MaxValue,            double.MinValue,            double.MinValue };
                yield return new object[] {  double.NaN,                 double.NaN,                 double.NaN };
                yield return new object[] {  double.NaN,                 1.0f,                       1.0f };
                yield return new object[] {  1.0f,                       double.NaN,                 1.0f };
                yield return new object[] {  double.PositiveInfinity,    double.NaN,                 double.PositiveInfinity };
                yield return new object[] {  double.NegativeInfinity,    double.NaN,                 double.NegativeInfinity };
                yield return new object[] {  double.NaN,                 double.PositiveInfinity,    double.PositiveInfinity };
                yield return new object[] {  double.NaN,                 double.NegativeInfinity,    double.NegativeInfinity };
                yield return new object[] { -0.0f,                       0.0f,                      -0.0f };
                yield return new object[] {  0.0f,                      -0.0f,                      -0.0f };
                yield return new object[] {  2.0f,                      -3.0f,                       2.0f };
                yield return new object[] { -3.0f,                       2.0f,                       2.0f };
                yield return new object[] {  3.0f,                      -2.0f,                      -2.0f };
                yield return new object[] { -2.0f,                       3.0f,                      -2.0f };
            }
        }

        public static IEnumerable<object[]> MinMagnitudeNumberSingle
        {
            get
            {
                yield return new object[] {  float.NegativeInfinity,     float.PositiveInfinity,     float.NegativeInfinity };
                yield return new object[] {  float.PositiveInfinity,     float.NegativeInfinity,     float.NegativeInfinity };
                yield return new object[] {  float.MinValue,             float.MaxValue,             float.MinValue };
                yield return new object[] {  float.MaxValue,             float.MinValue,             float.MinValue };
                yield return new object[] {  float.NaN,                  float.NaN,                  float.NaN };
                yield return new object[] {  float.NaN,                  1.0f,                       1.0f };
                yield return new object[] {  1.0f,                       float.NaN,                  1.0f };
                yield return new object[] {  float.PositiveInfinity,     float.NaN,                  float.PositiveInfinity };
                yield return new object[] {  float.NegativeInfinity,     float.NaN,                  float.NegativeInfinity };
                yield return new object[] {  float.NaN,                  float.PositiveInfinity,     float.PositiveInfinity };
                yield return new object[] {  float.NaN,                  float.NegativeInfinity,     float.NegativeInfinity };
                yield return new object[] { -0.0f,                       0.0f,                      -0.0f };
                yield return new object[] {  0.0f,                      -0.0f,                      -0.0f };
                yield return new object[] {  2.0f,                      -3.0f,                       2.0f };
                yield return new object[] { -3.0f,                       2.0f,                       2.0f };
                yield return new object[] {  3.0f,                      -2.0f,                      -2.0f };
                yield return new object[] { -2.0f,                       3.0f,                      -2.0f };
            }
        }

        public static IEnumerable<object[]> MinNumberDouble
        {
            get
            {
                yield return new object[] {  double.NegativeInfinity,    double.PositiveInfinity,    double.NegativeInfinity };
                yield return new object[] {  double.PositiveInfinity,    double.NegativeInfinity,    double.NegativeInfinity };
                yield return new object[] {  double.MinValue,            double.MaxValue,            double.MinValue };
                yield return new object[] {  double.MaxValue,            double.MinValue,            double.MinValue };
                yield return new object[] {  double.NaN,                 double.NaN,                 double.NaN };
                yield return new object[] {  double.NaN,                 1.0f,                       1.0f };
                yield return new object[] {  1.0f,                       double.NaN,                 1.0f };
                yield return new object[] {  double.PositiveInfinity,    double.NaN,                 double.PositiveInfinity };
                yield return new object[] {  double.NegativeInfinity,    double.NaN,                 double.NegativeInfinity };
                yield return new object[] {  double.NaN,                 double.PositiveInfinity,    double.PositiveInfinity };
                yield return new object[] {  double.NaN,                 double.NegativeInfinity,    double.NegativeInfinity };
                yield return new object[] { -0.0f,                       0.0f,                      -0.0f };
                yield return new object[] {  0.0f,                      -0.0f,                      -0.0f };
                yield return new object[] {  2.0f,                      -3.0f,                      -3.0f };
                yield return new object[] { -3.0f,                       2.0f,                      -3.0f };
                yield return new object[] {  3.0f,                      -2.0f,                      -2.0f };
                yield return new object[] { -2.0f,                       3.0f,                      -2.0f };
            }
        }

        public static IEnumerable<object[]> MinNumberSingle
        {
            get
            {
                yield return new object[] {  float.NegativeInfinity,     float.PositiveInfinity,     float.NegativeInfinity };
                yield return new object[] {  float.PositiveInfinity,     float.NegativeInfinity,     float.NegativeInfinity };
                yield return new object[] {  float.MinValue,             float.MaxValue,             float.MinValue };
                yield return new object[] {  float.MaxValue,             float.MinValue,             float.MinValue };
                yield return new object[] {  float.NaN,                  float.NaN,                  float.NaN };
                yield return new object[] {  float.NaN,                  1.0f,                       1.0f };
                yield return new object[] {  1.0f,                       float.NaN,                  1.0f };
                yield return new object[] {  float.PositiveInfinity,     float.NaN,                  float.PositiveInfinity };
                yield return new object[] {  float.NegativeInfinity,     float.NaN,                  float.NegativeInfinity };
                yield return new object[] {  float.NaN,                  float.PositiveInfinity,     float.PositiveInfinity };
                yield return new object[] {  float.NaN,                  float.NegativeInfinity,     float.NegativeInfinity };
                yield return new object[] { -0.0f,                       0.0f,                      -0.0f };
                yield return new object[] {  0.0f,                      -0.0f,                      -0.0f };
                yield return new object[] {  2.0f,                      -3.0f,                      -3.0f };
                yield return new object[] { -3.0f,                       2.0f,                      -3.0f };
                yield return new object[] {  3.0f,                      -2.0f,                      -2.0f };
                yield return new object[] { -2.0f,                       3.0f,                      -2.0f };
            }
        }

        public static IEnumerable<object[]> RadiansToDegreesDouble
        {
            get
            {
                yield return new object[] { double.NaN,               double.NaN,               0.0 };
                yield return new object[] { 0.0,                      0.0,                      0.0 };
                yield return new object[] { 0.0055555555555555567,    0.3183098861837906,       DoubleCrossPlatformMachineEpsilon };       // expected:  (1 / pi)
                yield return new object[] { 0.0075798686324546743,    0.4342944819032518,       DoubleCrossPlatformMachineEpsilon };       // expected:  (log10(e))
                yield return new object[] { 0.008726646259971648,     0.5,                      DoubleCrossPlatformMachineEpsilon };
                yield return new object[] { 0.0111111111111111124,    0.6366197723675813,       DoubleCrossPlatformMachineEpsilon };       // expected:  (2 / pi)
                yield return new object[] { 0.0120977005016866801,    0.6931471805599453,       DoubleCrossPlatformMachineEpsilon };       // expected:  (ln(2))
                yield return new object[] { 0.0123413414948843512,    0.7071067811865475,       DoubleCrossPlatformMachineEpsilon };       // expected:  (1 / sqrt(2))
                yield return new object[] { 0.0137077838904018851,    0.7853981633974483,       DoubleCrossPlatformMachineEpsilon };       // expected:  (pi / 4)
                yield return new object[] { 0.017453292519943295,     1.0,                      DoubleCrossPlatformMachineEpsilon };
                yield return new object[] { 0.019693931676727953,     1.1283791670955126,       DoubleCrossPlatformMachineEpsilon };       // expected:  (2 / sqrt(pi))
                yield return new object[] { 0.024682682989768702,     1.4142135623730950,       DoubleCrossPlatformMachineEpsilon };       // expected:  (sqrt(2))
                yield return new object[] { 0.025179778565706630,     1.4426950408889634,       DoubleCrossPlatformMachineEpsilon };       // expected:  (log2(e))
                yield return new object[] { 0.026179938779914940,     1.5,                      DoubleCrossPlatformMachineEpsilon };
                yield return new object[] { 0.027415567780803770,     1.5707963267948966,       DoubleCrossPlatformMachineEpsilon };       // expected:  (pi / 2)
                yield return new object[] { 0.034906585039886590,     2.0,                      DoubleCrossPlatformMachineEpsilon };
                yield return new object[] { 0.040187691180085916,     2.3025850929940457,       DoubleCrossPlatformMachineEpsilon };       // expected:  (ln(10))
                yield return new object[] { 0.043633231299858240,     2.5,                      DoubleCrossPlatformMachineEpsilon };
                yield return new object[] { 0.047442967903742035,     2.7182818284590452,       DoubleCrossPlatformMachineEpsilon };       // expected:  (e)
                yield return new object[] { 0.052359877559829880,     3.0,                      DoubleCrossPlatformMachineEpsilon };
                yield return new object[] { 0.054831135561607540,     3.1415926535897932,       DoubleCrossPlatformMachineEpsilon };       // expected:  (pi)
                yield return new object[] { 0.061086523819801536,     3.5,                      DoubleCrossPlatformMachineEpsilon };
                yield return new object[] { double.PositiveInfinity,  double.PositiveInfinity,  0.0 };
            }
        }

        public static IEnumerable<object[]> RadiansToDegreesSingle
        {
            get
            {
                yield return new object[] { float.NaN,                float.NaN,                  0.0 };
                yield return new object[] { 0.0f,                     0.0f,                       0.0 };
                yield return new object[] { 0.0055555557f,            0.318309886f,               SingleCrossPlatformMachineEpsilon };       // expected:  (1 / pi)
                yield return new object[] { 0.007579869f,             0.434294482f,               SingleCrossPlatformMachineEpsilon };       // expected:  (log10(e))
                yield return new object[] { 0.008726646f,             0.5f,                       SingleCrossPlatformMachineEpsilon };
                yield return new object[] { 0.011111111f,             0.636619772f,               SingleCrossPlatformMachineEpsilon };       // expected:  (2 / pi)
                yield return new object[] { 0.0120977005f,            0.693147181f,               SingleCrossPlatformMachineEpsilon };       // expected:  (ln(2))
                yield return new object[] { 0.012341342f,             0.707106781f,               SingleCrossPlatformMachineEpsilon };       // expected:  (1 / sqrt(2))
                yield return new object[] { 0.013707785f,             0.785398163f,               SingleCrossPlatformMachineEpsilon };       // expected:  (pi / 4)
                yield return new object[] { 0.017453292f,             1.0f,                       SingleCrossPlatformMachineEpsilon };
                yield return new object[] { 0.019693933f,             1.12837917f,                SingleCrossPlatformMachineEpsilon };       // expected:  (2 / sqrt(pi))
                yield return new object[] { 0.024682684f,             1.41421356f,                SingleCrossPlatformMachineEpsilon };       // expected:  (sqrt(2))
                yield return new object[] { 0.025179777f,             1.44269504f,                SingleCrossPlatformMachineEpsilon };       // expected:  (log2(e))
                yield return new object[] { 0.02617994f,              1.5f,                       SingleCrossPlatformMachineEpsilon };
                yield return new object[] { 0.02741557f,              1.57079633f,                SingleCrossPlatformMachineEpsilon };       // expected:  (pi / 2)
                yield return new object[] { 0.034906585f,             2.0f,                       SingleCrossPlatformMachineEpsilon };
                yield return new object[] { 0.040187694f,             2.30258509f,                SingleCrossPlatformMachineEpsilon };       // expected:  (ln(10))
                yield return new object[] { 0.043633234f,             2.5f,                       SingleCrossPlatformMachineEpsilon };
                yield return new object[] { 0.047442965f,             2.71828183f,                SingleCrossPlatformMachineEpsilon };       // expected:  (e)
                yield return new object[] { 0.05235988f,              3.0f,                       SingleCrossPlatformMachineEpsilon };
                yield return new object[] { 0.05483114f,              3.14159265f,                SingleCrossPlatformMachineEpsilon };       // expected:  (pi)
                yield return new object[] { 0.061086528f,             3.5f,                       SingleCrossPlatformMachineEpsilon };
                yield return new object[] { float.PositiveInfinity,   float.PositiveInfinity,     0.0 };
            }
        }

        public static IEnumerable<object[]> RoundDouble
        {
            get
            {
                yield return new object[] {  0.0,   0.0 };
                yield return new object[] {  1.4,   1.0 };
                yield return new object[] {  1.5,   2.0 };
                yield return new object[] {  2e7,   2e7 };
                yield return new object[] { -0.0,   0.0 };
                yield return new object[] { -1.4,  -1.0 };
                yield return new object[] { -1.5,  -2.0 };
                yield return new object[] { -2e7,  -2e7 };
            }
        }

        public static IEnumerable<object[]> RoundSingle
        {
            get
            {
                yield return new object[] {  0.0f,   0.0f };
                yield return new object[] {  1.4f,   1.0f };
                yield return new object[] {  1.5f,   2.0f };
                yield return new object[] {  2e7f,   2e7f };
                yield return new object[] { -0.0f,   0.0f };
                yield return new object[] { -1.4f,  -1.0f };
                yield return new object[] { -1.5f,  -2.0f };
                yield return new object[] { -2e7f,  -2e7f };
            }
        }

        public static IEnumerable<object[]> RoundAwayFromZeroDouble
        {
            get
            {
                yield return new object[] { 1,                           1 };
                yield return new object[] { 0.5,                         1 };
                yield return new object[] { 1.5,                         2 };
                yield return new object[] { 2.5,                         3 };
                yield return new object[] { 3.5,                         4 };
                yield return new object[] { 0.49999999999999994,         0 };
                yield return new object[] { 1.5,                         2 };
                yield return new object[] { 2.5,                         3 };
                yield return new object[] { 3.5,                         4 };
                yield return new object[] { 4.5,                         5 };
                yield return new object[] { 3.141592653589793,           3 };
                yield return new object[] { 2.718281828459045,           3 };
                yield return new object[] { 1385.4557313670111,          1385 };
                yield return new object[] { 3423423.43432,               3423423 };
                yield return new object[] { 535345.5,                    535346 };
                yield return new object[] { 535345.50001,                535346 };
                yield return new object[] { 535345.5,                    535346 };
                yield return new object[] { 535345.4,                    535345 };
                yield return new object[] { 535345.6,                    535346 };
                yield return new object[] { -2.718281828459045,         -3 };
                yield return new object[] { 10,                          10 };
                yield return new object[] { -10,                        -10 };
                yield return new object[] { -0,                         -0 };
                yield return new object[] { 0,                           0 };
                yield return new object[] { double.NaN,                  double.NaN };
                yield return new object[] { double.PositiveInfinity,     double.PositiveInfinity };
                yield return new object[] { double.NegativeInfinity,     double.NegativeInfinity };
                yield return new object[] { 1.7976931348623157E+308,     1.7976931348623157E+308 };
                yield return new object[] { -1.7976931348623157E+308,   -1.7976931348623157E+308 };
            }
        }

        public static IEnumerable<object[]> RoundAwayFromZeroSingle
        {
            get
            {
                yield return new object[] { 1f, 1f };
                yield return new object[] { 0.5f, 1f };
                yield return new object[] { 1.5f, 2f };
                yield return new object[] { 2.5f, 3f };
                yield return new object[] { 3.5f, 4f };
                yield return new object[] { 0.49999997f, 0f };
                yield return new object[] { 1.5f, 2f };
                yield return new object[] { 2.5f, 3f };
                yield return new object[] { 3.5f, 4f };
                yield return new object[] { 4.5f, 5f };
                yield return new object[] { 3.1415927f, 3f };
                yield return new object[] { 2.7182817f, 3f };
                yield return new object[] { 1385.4557f, 1385f };
                yield return new object[] { 3423.4343f, 3423f };
                yield return new object[] { 535345.5f, 535346f };
                yield return new object[] { 535345.5f, 535346f };
                yield return new object[] { 535345.5f, 535346f };
                yield return new object[] { 535345.4f, 535345f };
                yield return new object[] { 535345.6f, 535346f };
                yield return new object[] { -2.7182817f, -3f };
                yield return new object[] { 10f, 10f };
                yield return new object[] { -10f, -10f };
                yield return new object[] { -0f, -0f };
                yield return new object[] { 0f, 0f };
                yield return new object[] { float.NaN, float.NaN };
                yield return new object[] { float.PositiveInfinity, float.PositiveInfinity };
                yield return new object[] { float.NegativeInfinity, float.NegativeInfinity };
                yield return new object[] { 3.4028235E+38f, 3.4028235E+38f };
                yield return new object[] { -3.4028235E+38f, -3.4028235E+38f };
            }
        }

        public static IEnumerable<object[]> RoundToEvenDouble
        {
            get
            {
                yield return new object[] {  1,                         1 };
                yield return new object[] {  0.5,                       0 };
                yield return new object[] {  1.5,                       2 };
                yield return new object[] {  2.5,                       2 };
                yield return new object[] {  3.5,                       4 };

                // Math.Round(var = 0.49999999999999994) returns 1 on ARM32
                if (!PlatformDetection.IsArmProcess)
                    yield return new object[] {  0.49999999999999994,        0 };

                yield return new object[] {  1.5,                        2 };
                yield return new object[] {  2.5,                        2 };
                yield return new object[] {  3.5,                        4 };
                yield return new object[] {  4.5,                        4 };
                yield return new object[] {  3.141592653589793,          3 };
                yield return new object[] {  2.718281828459045,          3 };
                yield return new object[] {  1385.4557313670111,         1385 };
                yield return new object[] {  3423423.43432,              3423423 };
                yield return new object[] {  535345.5,                   535346 };
                yield return new object[] {  535345.50001,               535346 };
                yield return new object[] {  535345.5,                   535346 };
                yield return new object[] {  535345.4,                   535345 };
                yield return new object[] {  535345.6,                   535346 };
                yield return new object[] { -2.718281828459045,         -3 };
                yield return new object[] {  10,                         10 };
                yield return new object[] { -10,                        -10 };
                yield return new object[] { -0,                         -0 };
                yield return new object[] {  0,                          0 };
                yield return new object[] {  double.NaN,                 double.NaN };
                yield return new object[] {  double.PositiveInfinity,    double.PositiveInfinity };
                yield return new object[] {  double.NegativeInfinity,    double.NegativeInfinity };
                yield return new object[] {  1.7976931348623157E+308,    1.7976931348623157E+308 };
                yield return new object[] { -1.7976931348623157E+308,   -1.7976931348623157E+308 };
            }
        }

        public static IEnumerable<object[]> RoundToEvenSingle
        {
            get
            {
                yield return new object[] {  1f,                         1f };
                yield return new object[] {  0.5f,                       0f };
                yield return new object[] {  1.5f,                       2f };
                yield return new object[] {  2.5f,                       2f };
                yield return new object[] {  3.5f,                       4f };
                yield return new object[] {  0.49999997f,                0f };
                yield return new object[] {  1.5f,                       2f };
                yield return new object[] {  2.5f,                       2f };
                yield return new object[] {  3.5f,                       4f };
                yield return new object[] {  4.5f,                       4f };
                yield return new object[] {  3.1415927f,                 3f };
                yield return new object[] {  2.7182817f,                 3f };
                yield return new object[] {  1385.4557f,                 1385f };
                yield return new object[] {  3423.4343f,                 3423f };
                yield return new object[] {  535345.5f,                  535346f };
                yield return new object[] {  535345.5f,                  535346f };
                yield return new object[] {  535345.5f,                  535346f };
                yield return new object[] {  535345.4f,                  535345f };
                yield return new object[] {  535345.6f,                  535346f };
                yield return new object[] { -2.7182817f,                -3f };
                yield return new object[] {  10f,                        10f };
                yield return new object[] { -10f,                       -10f };
                yield return new object[] { -0f,                        -0f };
                yield return new object[] {  0f,                         0f };
                yield return new object[] {  float.NaN,                  float.NaN };
                yield return new object[] {  float.PositiveInfinity,     float.PositiveInfinity };
                yield return new object[] {  float.NegativeInfinity,     float.NegativeInfinity };
                yield return new object[] {  3.4028235E+38f,             3.4028235E+38f };
                yield return new object[] { -3.4028235E+38f,            -3.4028235E+38f };
            }
        }

        public static IEnumerable<object[]> TruncateDouble
        {
            get
            {
                yield return new object[] {  0.12345,  0.0f };
                yield return new object[] {  3.14159,  3.0f };
                yield return new object[] { -3.14159, -3.0f };
            }
        }

        public static IEnumerable<object[]> TruncateSingle
        {
            get
            {
                yield return new object[] {  0.12345f,   0.0f };
                yield return new object[] {  3.14159f,   3.0f };
                yield return new object[] { -3.14159f,  -3.0f };
            }
        }
    }
}
