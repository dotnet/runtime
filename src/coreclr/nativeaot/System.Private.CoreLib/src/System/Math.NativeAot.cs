// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Purpose: Some floating-point math operations
**
===========================================================*/

//This class contains only static members and doesn't require serialization.

using System.Runtime;
using System.Runtime.CompilerServices;

namespace System
{
    public static partial class Math
    {
        [Intrinsic]
        public static double Acos(double d)
        {
            return RuntimeImports.acos(d);
        }

        [Intrinsic]
        public static double Acosh(double d)
        {
            return RuntimeImports.acosh(d);
        }

        [Intrinsic]
        public static double Asin(double d)
        {
            return RuntimeImports.asin(d);
        }

        [Intrinsic]
        public static double Asinh(double d)
        {
            return RuntimeImports.asinh(d);
        }

        [Intrinsic]
        public static double Atan(double d)
        {
            return RuntimeImports.atan(d);
        }

        [Intrinsic]
        public static double Atan2(double y, double x)
        {
            return RuntimeImports.atan2(y, x);
        }

        [Intrinsic]
        public static double Atanh(double d)
        {
            return RuntimeImports.atanh(d);
        }

        [Intrinsic]
        public static double Cbrt(double d)
        {
            return RuntimeImports.cbrt(d);
        }

        [Intrinsic]
        public static double Ceiling(double a)
        {
            return RuntimeImports.ceil(a);
        }

        [Intrinsic]
        public static double Cos(double d)
        {
            return RuntimeImports.cos(d);
        }

        [Intrinsic]
        public static double Cosh(double value)
        {
            return RuntimeImports.cosh(value);
        }

        [Intrinsic]
        public static double Exp(double d)
        {
            return RuntimeImports.exp(d);
        }

        [Intrinsic]
        public static double Floor(double d)
        {
            return RuntimeImports.floor(d);
        }

        [Intrinsic]
        public static double FusedMultiplyAdd(double x, double y, double z)
        {
            return RuntimeImports.fma(x, y, z);
        }

        [Intrinsic]
        public static double Log(double d)
        {
            return RuntimeImports.log(d);
        }

        [Intrinsic]
        public static double Log2(double x)
        {
            return RuntimeImports.log2(x);
        }

        [Intrinsic]
        public static double Log10(double d)
        {
            return RuntimeImports.log10(d);
        }

        [Intrinsic]
        public static double Pow(double x, double y)
        {
            return RuntimeImports.pow(x, y);
        }

        [Intrinsic]
        public static double Sin(double a)
        {
            return RuntimeImports.sin(a);
        }

        [Intrinsic]
        public static double Sinh(double value)
        {
            return RuntimeImports.sinh(value);
        }

        [Intrinsic]
        public static double Sqrt(double d)
        {
            return RuntimeImports.sqrt(d);
        }

        [Intrinsic]
        public static double Tan(double a)
        {
            return RuntimeImports.tan(a);
        }

        [Intrinsic]
        public static double Tanh(double value)
        {
            return RuntimeImports.tanh(value);
        }

        [Intrinsic]
        private static double FMod(double x, double y)
        {
            return RuntimeImports.fmod(x, y);
        }

        [Intrinsic]
        private static unsafe double ModF(double x, double* intptr)
        {
            return RuntimeImports.modf(x, intptr);
        }

        public static (double Sin, double Cos) SinCos(double x) => (Sin(x), Cos(x));
    }
}
