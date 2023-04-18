// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using System.Diagnostics;
using Xunit;

namespace Runtime_49489
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Callee(float fltRef0, float fltRef1, float fltReg2, float fltReg3, float fltReg4, float fltReg5, float fltReg6, float fltReg7, Vector2 simd8)
        {
            const double eps = 1E-10;
            Debug.Assert(Math.Abs(simd8.X) <= eps);
            Debug.Assert(Math.Abs(simd8.Y - 1) <= eps);
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Caller()
        {
            Vector2 simd8;
            simd8.X = 0;
            simd8.Y = 1;
            return Callee(0, 0, 0, 0, 0, 0, 0, 0, simd8);

        }

        [Fact]
        public static int TestEntryPoint()
        {
            return Caller();
        }
    }
}
