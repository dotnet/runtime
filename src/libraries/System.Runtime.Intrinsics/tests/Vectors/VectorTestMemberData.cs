// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Runtime.Intrinsics.Tests.Vectors
{
    internal static class VectorTestMemberData
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
    }
}
