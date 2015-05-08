// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace JitTest
{
    using System;

    class Test
    {
        static void RunTests(double nan, double plusinf, double minusinf)
        {
            if (!Double.IsNaN(nan + nan))
                throw new Exception("! Double.IsNaN(nan + nan)");
            if (!Double.IsNaN(nan + plusinf))
                throw new Exception("! Double.IsNaN(nan + plusinf)");
            if (!Double.IsNaN(nan + minusinf))
                throw new Exception("! Double.IsNaN(nan + minusinf)");
            if (!Double.IsNaN(plusinf + nan))
                throw new Exception("! Double.IsNaN(plusinf + nan)");
            if (!Double.IsPositiveInfinity(plusinf + plusinf))
                throw new Exception("! Double.IsPositiveInfinity(plusinf + plusinf)");
            if (!Double.IsNaN(plusinf + minusinf))
                throw new Exception("! Double.IsNaN(plusinf + minusinf)");
            if (!Double.IsNaN(minusinf + nan))
                throw new Exception("! Double.IsNaN(minusinf + nan)");
            if (!Double.IsNaN(minusinf + plusinf))
                throw new Exception("! Double.IsNaN(minusinf + plusinf)");
            if (!Double.IsNegativeInfinity(minusinf + minusinf))
                throw new Exception("! Double.IsNegativeInfinity(minusinf + minusinf)");
            if (!Double.IsNaN(nan + nan))
                throw new Exception("! Double.IsNaN(nan + nan)");
            if (!Double.IsNaN(nan + plusinf))
                throw new Exception("! Double.IsNaN(nan + plusinf)");
            if (!Double.IsNaN(nan + minusinf))
                throw new Exception("! Double.IsNaN(nan + minusinf)");
            if (!Double.IsNaN(plusinf + nan))
                throw new Exception("! Double.IsNaN(plusinf + nan)");
            if (!Double.IsPositiveInfinity(plusinf + plusinf))
                throw new Exception("! Double.IsPositiveInfinity(plusinf + plusinf)");
            if (!Double.IsNaN(plusinf + minusinf))
                throw new Exception("! Double.IsNaN(plusinf + minusinf)");
            if (!Double.IsNaN(minusinf + nan))
                throw new Exception("! Double.IsNaN(minusinf + nan)");
            if (!Double.IsNaN(minusinf + plusinf))
                throw new Exception("! Double.IsNaN(minusinf + plusinf)");
            if (!Double.IsNegativeInfinity(minusinf + minusinf))
                throw new Exception("! Double.IsNegativeInfinity(minusinf + minusinf)");
            if (!Double.IsNaN(nan + nan))
                throw new Exception("! Double.IsNaN(nan + nan)");
            if (!Double.IsNaN(nan + plusinf))
                throw new Exception("! Double.IsNaN(nan + plusinf)");
            if (!Double.IsNaN(nan + minusinf))
                throw new Exception("! Double.IsNaN(nan + minusinf)");
            if (!Double.IsNaN(plusinf + nan))
                throw new Exception("! Double.IsNaN(plusinf + nan)");
            if (!Double.IsPositiveInfinity(plusinf + plusinf))
                throw new Exception("! Double.IsPositiveInfinity(plusinf + plusinf)");
            if (!Double.IsNaN(plusinf + minusinf))
                throw new Exception("! Double.IsNaN(plusinf + minusinf)");
            if (!Double.IsNaN(minusinf + nan))
                throw new Exception("! Double.IsNaN(minusinf + nan)");
            if (!Double.IsNaN(minusinf + plusinf))
                throw new Exception("! Double.IsNaN(minusinf + plusinf)");
            if (!Double.IsNegativeInfinity(minusinf + minusinf))
                throw new Exception("! Double.IsNegativeInfinity(minusinf + minusinf)");
            if (!Double.IsNaN(nan + nan))
                throw new Exception("! Double.IsNaN(nan + nan)");
            if (!Double.IsNaN(nan + plusinf))
                throw new Exception("! Double.IsNaN(nan + plusinf)");
            if (!Double.IsNaN(nan + minusinf))
                throw new Exception("! Double.IsNaN(nan + minusinf)");
            if (!Double.IsNaN(plusinf + nan))
                throw new Exception("! Double.IsNaN(plusinf + nan)");
            if (!Double.IsPositiveInfinity(plusinf + plusinf))
                throw new Exception("! Double.IsPositiveInfinity(plusinf + plusinf)");
            if (!Double.IsNaN(plusinf + minusinf))
                throw new Exception("! Double.IsNaN(plusinf + minusinf)");
            if (!Double.IsNaN(minusinf + nan))
                throw new Exception("! Double.IsNaN(minusinf + nan)");
            if (!Double.IsNaN(minusinf + plusinf))
                throw new Exception("! Double.IsNaN(minusinf + plusinf)");
            if (!Double.IsNegativeInfinity(minusinf + minusinf))
                throw new Exception("! Double.IsNegativeInfinity(minusinf + minusinf)");
        }

        static int Main()
        {
            RunTests(Double.NaN, Double.PositiveInfinity, Double.NegativeInfinity);
            Console.WriteLine("=== PASSED ===");
            return 100;
        }
    }
}
