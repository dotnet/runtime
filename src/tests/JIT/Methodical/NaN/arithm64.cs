// Licensed to the .NET Foundation under one or more agreements.
using Xunit;
namespace Test_arithm64
{
// The .NET Foundation licenses this file to you under the MIT license.
//

namespace JitTest
{
    using System;

    public class Test
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

        [Fact]
        public static int TestEntryPoint()
        {
            RunTests(Double.NaN, Double.PositiveInfinity, Double.NegativeInfinity);
            Console.WriteLine("=== PASSED ===");
            return 100;
        }
    }
}
}
