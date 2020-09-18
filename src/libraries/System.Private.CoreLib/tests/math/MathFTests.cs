// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System
{
    class MathTests
    {
        public struct ScaleBTest
        {

            public ScaleBTest(float f, int n, float e)
            {
                this.f = f;
                this.n = n;
                this.e = e;
            }

            public float f { get; set; }
            public int n { get; set; }
            public float e { get; set; }
        }

        static int Main()
        {
            bool success = true;

            success = TestScaleB();

            return success ? 100 : 101;
        }

        static bool TestScaleB()
        {
            bool success = true;
            ScaleBTest[] tests =
            {
                // Special
                new ScaleBTest(0, 2147483647, 0),
                new ScaleBTest(0, -2147483647, 0),
                new ScaleBTest(-0, 2147483647, -0),
                new ScaleBTest(float.NaN, 0, float.NaN),
                new ScaleBTest(float.PositiveInfinity, 0, float.PositiveInfinity),
                new ScaleBTest(float.NaN, 0, float.NaN),
                new ScaleBTest(1f, 0, 1f),
                new ScaleBTest(1f, 1, 2f),
                new ScaleBTest(1f, -1, 0.5f),
                new ScaleBTest(1f, 2147483647, float.NaN),
                new ScaleBTest(float.NaN, 1, float.NaN),
                new ScaleBTest(float.PositiveInfinity, 2147483647, float.PositiveInfinity),
                new ScaleBTest(float.PositiveInfinity, -2147483647, float.PositiveInfinity),
                new ScaleBTest(float.NaN, 2147483647, float.NaN),
                new ScaleBTest(1.7014118E+38f, -276, 1E-45f),
                new ScaleBTest(1E-45f, 276, 1.7014118E+38f),
                new ScaleBTest(1.0002441f, -149, 1E-45f),
                new ScaleBTest(0.74999994f, -148, 1E-45f),
                new ScaleBTest(0.50000066f, -128, 1.46937E-39f),

                // Sanity
                new ScaleBTest(-8.066849f, -2, -2.0167122f),
                new ScaleBTest(4.3452396f, -1, 2.1726198f),
                new ScaleBTest(-8.3814335f, 0, -8.3814335f),
                new ScaleBTest(-6.5316734f, 1, -13.063347f),
                new ScaleBTest(9.267057f, 2, 37.06823f),
                new ScaleBTest(0.6619859f, 3, 5.295887f),
                new ScaleBTest(-0.40660393f, 4, -6.505663f),
                new ScaleBTest(0.56175977f, 5, 17.976313f),
                new ScaleBTest(0.7741523f, 6, 49.545746f),
                new ScaleBTest(-0.6787637f, 7, -86.88175f),
            };

            foreach (ScaleBTest t in tests)
            {
                if (MathF.ScaleB(t.f, t.n) != t.e
                    && (!float.IsNaN(t.e) && !float.IsNaN(MathF.ScaleB(t.f, t.n))))
                {
                    Console.WriteLine("MathF.ScaleB({0}, {1}) != {2}, got {3} instead.", t.f, t.n, t.e, MathF.ScaleB(t.f, t.n));
                    success = false;
                }
            }

            return success;
        }
    }
}
