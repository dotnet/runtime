// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Numerics;
using Xunit;

namespace VectorMathTests
{
    public class Program
    {
        public const int DefaultSeed = 20010415;
        public static int Seed = Environment.GetEnvironmentVariable("CORECLR_SEED") switch
        {
            string seedStr when seedStr.Equals("random", StringComparison.OrdinalIgnoreCase) => new Random().Next(),
            string seedStr when int.TryParse(seedStr, out int envSeed) => envSeed,
            _ => DefaultSeed
        };

        [Fact]
        public static int TestEntryPoint()
        {
            Random random = new Random(Seed);
            var a = new System.Numerics.Vector<short>(25);
            a = System.Numerics.Vector.SquareRoot(a);
            if (a[0] != 5)
            {
                return 0;
            }
            var b = System.Numerics.Vector<int>.One;
            b = System.Numerics.Vector.SquareRoot(b);
            if (b[3] != 1)
            {
                return 0;
            }
            var c = new System.Numerics.Vector<long>(1231111222 * (long)1231111222);
            c = System.Numerics.Vector.SquareRoot(c);
            if (c[1] != 1231111222)
            {
                return 0;
            }
            var d = new System.Numerics.Vector<double>(100.0);
            d = System.Numerics.Vector.SquareRoot(d);
            if (((int)d[0]) != 10)
            {
                return 0;
            }
            var e = new System.Numerics.Vector<float>(64);
            e = System.Numerics.Vector.SquareRoot(e);
            if (((int)e[3]) != 8)
            {
                return 0;
            }
            var f = new System.Numerics.Vector<ushort>(36);
            f = System.Numerics.Vector.SquareRoot(f);
            if (f[7] != 6)
            {
                return 0;
            }
            var g = new System.Numerics.Vector<ulong>(16);
            g = System.Numerics.Vector.SquareRoot(g);
            if (g[1] != 4)
            {
                return 0;
            }
            return 100;
        }
    }
}
