// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Purpose: Some single-precision floating-point math operations
**
===========================================================*/

//This class contains only static members and doesn't require serialization.

using System.Runtime;
using System.Runtime.CompilerServices;

namespace System
{
    public static partial class MathF
    {
        [Intrinsic]
        public static float Acos(float x)
        {
            return RuntimeImports.acosf(x);
        }

        [Intrinsic]
        public static float Acosh(float x)
        {
            return RuntimeImports.acoshf(x);
        }

        [Intrinsic]
        public static float Asin(float x)
        {
            return RuntimeImports.asinf(x);
        }

        [Intrinsic]
        public static float Asinh(float x)
        {
            return RuntimeImports.asinhf(x);
        }

        [Intrinsic]
        public static float Atan(float x)
        {
            return RuntimeImports.atanf(x);
        }

        [Intrinsic]
        public static float Atan2(float y, float x)
        {
            return RuntimeImports.atan2f(y, x);
        }

        [Intrinsic]
        public static float Atanh(float x)
        {
            return RuntimeImports.atanhf(x);
        }

        [Intrinsic]
        public static float Cbrt(float x)
        {
            return RuntimeImports.cbrtf(x);
        }

        [Intrinsic]
        public static float Ceiling(float x)
        {
            return RuntimeImports.ceilf(x);
        }

        [Intrinsic]
        public static float Cos(float x)
        {
            return RuntimeImports.cosf(x);
        }

        [Intrinsic]
        public static float Cosh(float x)
        {
            return RuntimeImports.coshf(x);
        }

        [Intrinsic]
        public static float Exp(float x)
        {
            return RuntimeImports.expf(x);
        }

        [Intrinsic]
        public static float Floor(float x)
        {
            return RuntimeImports.floorf(x);
        }

        [Intrinsic]
        public static float FusedMultiplyAdd(float x, float y, float z)
        {
            return RuntimeImports.fmaf(x, y, z);
        }

        [Intrinsic]
        public static float Log(float x)
        {
            return RuntimeImports.logf(x);
        }

        [Intrinsic]
        public static float Log2(float x)
        {
            return RuntimeImports.log2f(x);
        }

        [Intrinsic]
        public static float Log10(float x)
        {
            return RuntimeImports.log10f(x);
        }

        [Intrinsic]
        public static float Pow(float x, float y)
        {
            return RuntimeImports.powf(x, y);
        }

        [Intrinsic]
        public static float Sin(float x)
        {
            return RuntimeImports.sinf(x);
        }

        [Intrinsic]
        public static float Sinh(float x)
        {
            return RuntimeImports.sinhf(x);
        }

        [Intrinsic]
        public static float Sqrt(float x)
        {
            return RuntimeImports.sqrtf(x);
        }

        [Intrinsic]
        public static float Tan(float x)
        {
            return RuntimeImports.tanf(x);
        }

        [Intrinsic]
        public static float Tanh(float x)
        {
            return RuntimeImports.tanhf(x);
        }

        [Intrinsic]
        private static float FMod(float x, float y)
        {
            return RuntimeImports.fmodf(x, y);
        }

        [Intrinsic]
        private static unsafe float ModF(float x, float* intptr)
        {
            return RuntimeImports.modff(x, intptr);
        }

        public static (float Sin, float Cos) SinCos(float x) => (Sin(x), Cos(x));
    }
}
