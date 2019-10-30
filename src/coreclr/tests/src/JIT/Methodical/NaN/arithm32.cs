// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace JitTest
{
    using System;

    class Test
    {
        static void RunTests(float nan, float plusinf, float minusinf)
        {
            if (!Single.IsNaN(nan + nan))
                throw new Exception("! Single.IsNaN(nan + nan)");
            if (!Single.IsNaN(nan + plusinf))
                throw new Exception("! Single.IsNaN(nan + plusinf)");
            if (!Single.IsNaN(nan + minusinf))
                throw new Exception("! Single.IsNaN(nan + minusinf)");
            if (!Single.IsNaN(plusinf + nan))
                throw new Exception("! Single.IsNaN(plusinf + nan)");
            if (!Single.IsPositiveInfinity(plusinf + plusinf))
                throw new Exception("! Single.IsPositiveInfinity(plusinf + plusinf)");
            if (!Single.IsNaN(plusinf + minusinf))
                throw new Exception("! Single.IsNaN(plusinf + minusinf)");
            if (!Single.IsNaN(minusinf + nan))
                throw new Exception("! Single.IsNaN(minusinf + nan)");
            if (!Single.IsNaN(minusinf + plusinf))
                throw new Exception("! Single.IsNaN(minusinf + plusinf)");
            if (!Single.IsNegativeInfinity(minusinf + minusinf))
                throw new Exception("! Single.IsNegativeInfinity(minusinf + minusinf)");
            if (!Single.IsNaN(nan + nan))
                throw new Exception("! Single.IsNaN(nan + nan)");
            if (!Single.IsNaN(nan + plusinf))
                throw new Exception("! Single.IsNaN(nan + plusinf)");
            if (!Single.IsNaN(nan + minusinf))
                throw new Exception("! Single.IsNaN(nan + minusinf)");
            if (!Single.IsNaN(plusinf + nan))
                throw new Exception("! Single.IsNaN(plusinf + nan)");
            if (!Single.IsPositiveInfinity(plusinf + plusinf))
                throw new Exception("! Single.IsPositiveInfinity(plusinf + plusinf)");
            if (!Single.IsNaN(plusinf + minusinf))
                throw new Exception("! Single.IsNaN(plusinf + minusinf)");
            if (!Single.IsNaN(minusinf + nan))
                throw new Exception("! Single.IsNaN(minusinf + nan)");
            if (!Single.IsNaN(minusinf + plusinf))
                throw new Exception("! Single.IsNaN(minusinf + plusinf)");
            if (!Single.IsNegativeInfinity(minusinf + minusinf))
                throw new Exception("! Single.IsNegativeInfinity(minusinf + minusinf)");
            if (!Single.IsNaN(nan + nan))
                throw new Exception("! Single.IsNaN(nan + nan)");
            if (!Single.IsNaN(nan + plusinf))
                throw new Exception("! Single.IsNaN(nan + plusinf)");
            if (!Single.IsNaN(nan + minusinf))
                throw new Exception("! Single.IsNaN(nan + minusinf)");
            if (!Single.IsNaN(plusinf + nan))
                throw new Exception("! Single.IsNaN(plusinf + nan)");
            if (!Single.IsPositiveInfinity(plusinf + plusinf))
                throw new Exception("! Single.IsPositiveInfinity(plusinf + plusinf)");
            if (!Single.IsNaN(plusinf + minusinf))
                throw new Exception("! Single.IsNaN(plusinf + minusinf)");
            if (!Single.IsNaN(minusinf + nan))
                throw new Exception("! Single.IsNaN(minusinf + nan)");
            if (!Single.IsNaN(minusinf + plusinf))
                throw new Exception("! Single.IsNaN(minusinf + plusinf)");
            if (!Single.IsNegativeInfinity(minusinf + minusinf))
                throw new Exception("! Single.IsNegativeInfinity(minusinf + minusinf)");
            if (!Single.IsNaN(nan + nan))
                throw new Exception("! Single.IsNaN(nan + nan)");
            if (!Single.IsNaN(nan + plusinf))
                throw new Exception("! Single.IsNaN(nan + plusinf)");
            if (!Single.IsNaN(nan + minusinf))
                throw new Exception("! Single.IsNaN(nan + minusinf)");
            if (!Single.IsNaN(plusinf + nan))
                throw new Exception("! Single.IsNaN(plusinf + nan)");
            if (!Single.IsPositiveInfinity(plusinf + plusinf))
                throw new Exception("! Single.IsPositiveInfinity(plusinf + plusinf)");
            if (!Single.IsNaN(plusinf + minusinf))
                throw new Exception("! Single.IsNaN(plusinf + minusinf)");
            if (!Single.IsNaN(minusinf + nan))
                throw new Exception("! Single.IsNaN(minusinf + nan)");
            if (!Single.IsNaN(minusinf + plusinf))
                throw new Exception("! Single.IsNaN(minusinf + plusinf)");
            if (!Single.IsNegativeInfinity(minusinf + minusinf))
                throw new Exception("! Single.IsNegativeInfinity(minusinf + minusinf)");
        }

        static int Main()
        {
            RunTests(Single.NaN, Single.PositiveInfinity, Single.NegativeInfinity);
            Console.WriteLine("=== PASSED ===");
            return 100;
        }
    }
}
