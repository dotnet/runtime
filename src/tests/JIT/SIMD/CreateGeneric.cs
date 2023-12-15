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
		
        [Fact]
        public static int TestEntryPoint()
        {
            var a = new System.Numerics.Vector<short>(51);
            for (int i = 0; i < System.Numerics.Vector<short>.Count; ++i)
            {
                if (a[i] != 51)
                {
                    return 0;
                }
            }
            var b = System.Numerics.Vector<int>.One;
            for (int i = 0; i < System.Numerics.Vector<int>.Count; ++i)
            {
                if (b[i] != 1)
                {
                    return 0;
                }
            }
            var c = System.Numerics.Vector<short>.One;
            for (int i = 0; i < System.Numerics.Vector<short>.Count; ++i)
            {
                if (c[i] != 1)
                {
                    return 0;
                }
            }
            var d = new System.Numerics.Vector<double>(100.0);
            for (int i = 0; i < System.Numerics.Vector<double>.Count; ++i)
            {
                if (Math.Abs(d[i] - 100.0) > EPS)
                {
                    return 0;
                }
            }

            var e = new System.Numerics.Vector<float>(100);
            for (int i = 0; i < System.Numerics.Vector<float>.Count; ++i)
            {
                if (Math.Abs(e[i] - 100.0) > EPS)
                {
                    return 0;
                }
            }
            var f = c * 49;
            var g = f + a;

            if (g[0] == 100)
            {
                return 100;
            }
            return 0;
        }
    }
}
