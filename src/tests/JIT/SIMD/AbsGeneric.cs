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
        const float EPS = Single.Epsilon * 5;

        static short[] GenerateArray(int size, short value)
        {
            short[] arr = new short[size];
            for (int i = 0; i < size; ++i)
            {
                arr[i] = value;
            }
            return arr;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            short[] arr = GenerateArray(60, 5);
            var a = new System.Numerics.Vector<short>(arr);
            a = System.Numerics.Vector.Abs(a);
            if (a[0] != 5)
            {
                return 0;
            }
            var b = System.Numerics.Vector<int>.One;
            b = System.Numerics.Vector.Abs(b);
            if (b[3] != 1)
            {
                return 0;
            }
            var c = new System.Numerics.Vector<long>(-11);
            c = System.Numerics.Vector.Abs(c);
            if (c[1] != 11)
            {
                return 0;
            }

            var d = new System.Numerics.Vector<double>(-100.0);
            d = System.Numerics.Vector.Abs(d);
            if (d[0] != 100)
            {
                return 0;
            }
            var e = new System.Numerics.Vector<float>(-22);
            e = System.Numerics.Vector.Abs(e);
            if (e[3] != 22)
            {
                return 0;
            }
            var f = new System.Numerics.Vector<ushort>(21);
            f = System.Numerics.Vector.Abs(f);
            if (f[7] != 21)
            {
                return 0;
            }
            var g = new System.Numerics.Vector<ulong>(21);
            g = System.Numerics.Vector.Abs(g);
            if (g[1] != 21)
            {
                return 0;
            }
            return 100;
        }
    }
}
