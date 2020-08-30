// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
** Purpose: Some floating-point math operations
**
**
===========================================================*/

using System.Runtime.CompilerServices;

namespace System
{
    public static partial class Math
    {
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Abs(double value);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern float Abs(float value);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Acos(double d);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Acosh(double d);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Asin(double d);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Asinh(double d);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Atan(double d);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Atan2(double y, double x);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Atanh(double d);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Cbrt(double d);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Ceiling(double a);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Cos(double d);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Cosh(double value);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Exp(double d);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Floor(double d);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double FusedMultiplyAdd(double x, double y, double z);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern int ILogB(double x);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Log(double d);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Log2(double x);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Log10(double d);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Pow(double x, double y);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double ScaleB(double x, int n);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Sin(double a);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Sinh(double value);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Sqrt(double d);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Tan(double a);

        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Tanh(double value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern double FMod(double x, double y);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe double ModF(double x, double* intptr);
    }
}
