// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System
{
    class MathTests
    {
        public struct ScaleBTest
        {

            public ScaleBTest(double f, int n, double e)
            {
                this.f = f;
                this.n = n;
                this.e = e;
            }

            public double f { get; set; }
            public int n { get; set; }
            public double e { get; set; }
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
                // Sanity
                new ScaleBTest(-8.06684839057968, -2, -2.01671209764492),
                new ScaleBTest(4.345239849338305, -1, 2.1726199246691524),
                new ScaleBTest(-8.38143342755525, 0, -8.38143342755525),
                new ScaleBTest(-6.531673581913484, 1, -13.063347163826968),
                new ScaleBTest(9.267056966972586, 2, 37.06822786789034),
                new ScaleBTest(0.6619858980995045, 3, 5.295887184796036),
                new ScaleBTest(-0.4066039223853553, 4, -6.505662758165685),
                new ScaleBTest(0.5617597462207241, 5, 17.97631187906317),
                new ScaleBTest(0.7741522965913037, 6, 49.545746981843436),
                new ScaleBTest(-0.6787637026394024, 7, -86.88175393784351),
                // Special
                new ScaleBTest(0, 2147483647, 0),
                new ScaleBTest(0, -2147483648, 0),
                new ScaleBTest(-0, 2147483647, -0),
                new ScaleBTest(double.NaN, 0, double.NaN),
                new ScaleBTest(double.PositiveInfinity, 0, double.PositiveInfinity),
                new ScaleBTest(double.NegativeInfinity, 0, double.NegativeInfinity),
                new ScaleBTest(1, 0, 1),
                new ScaleBTest(1, 1, 2),
                new ScaleBTest(1, -1, 0.5),
                new ScaleBTest(1, 2147483647, double.PositiveInfinity),
                new ScaleBTest(double.NaN, 1, double.NaN),
                new ScaleBTest(double.PositiveInfinity, 2147483647, double.PositiveInfinity),
                new ScaleBTest(double.PositiveInfinity, -1, double.PositiveInfinity),
                new ScaleBTest(double.NegativeInfinity, 2147483647, double.NegativeInfinity),
                new ScaleBTest(8.98846567431158E+307, -2097, 5E-324),
                new ScaleBTest(5E-324, 2097, 8.98846567431158E+307),
                new ScaleBTest(1.000244140625, -1074, 5E-324),
                new ScaleBTest(0.7499999999999999, -1073, 5E-324),
                new ScaleBTest(0.5000000000000012, -1024, 2.781342323134007E-309),
            };

            foreach (ScaleBTest t in tests)
            {
                if (Math.ScaleB(t.f, t.n) != t.e
                    && (!double.IsNaN(t.e) && !double.IsNaN(Math.ScaleB(t.f, t.n))))
                {
                    Console.WriteLine("Math.ScaleB({0}, {1}) != {2}, got {3} instead.", t.f, t.n, t.e, Math.ScaleB(t.f, t.n));
                    success = false;
                }
            }

            return success;
        }
    }
}
