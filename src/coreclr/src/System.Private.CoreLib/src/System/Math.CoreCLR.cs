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
using System.Runtime.InteropServices;

namespace System
{
    public static partial class Math
    {
#pragma warning disable CA1401 // P/Invokes should not be visible

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Abs(double value);

        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        private static extern float AbsF(float value);
        [Intrinsic]
        public static float Abs(float value) => AbsF(value);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Acos(double d);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Acosh(double d);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Asin(double d);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Asinh(double d);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Atan(double d);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Atan2(double y, double x);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Atanh(double d);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Cbrt(double d);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Ceiling(double a);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Cos(double d);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Cosh(double value);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Exp(double d);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Floor(double d);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double FusedMultiplyAdd(double x, double y, double z);

        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ILogB(double x);

        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Log(double d);

        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Log2(double x);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Log10(double d);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Pow(double x, double y);

        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double ScaleB(double x, int n);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Sin(double a);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Sinh(double value);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Sqrt(double d);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Tan(double a);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Tanh(double value);

#pragma warning restore CA1401 // P/Invokes should not be visible

        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        private static extern double FMod(double x, double y);

        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe double ModF(double x, double* intptr);
    }
}
