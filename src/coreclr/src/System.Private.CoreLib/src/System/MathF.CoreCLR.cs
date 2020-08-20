// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Purpose: Some single-precision floating-point math operations
**
===========================================================*/

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public static partial class MathF
    {
#pragma warning disable CA1401 // P/Invokes should not be visible

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Acos(float x);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Acosh(float x);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Asin(float x);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Asinh(float x);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Atan(float x);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Atan2(float y, float x);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Atanh(float x);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Cbrt(float x);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Ceiling(float x);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Cos(float x);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Cosh(float x);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Exp(float x);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Floor(float x);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float FusedMultiplyAdd(float x, float y, float z);

        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ILogB(float x);

        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Log(float x);

        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Log2(float x);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Log10(float x);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Pow(float x, float y);

        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float ScaleB(float x, int n);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Sin(float x);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Sinh(float x);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Sqrt(float x);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Tan(float x);

        [Intrinsic]
        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        public static extern float Tanh(float x);

#pragma warning restore CA1401 // P/Invokes should not be visible

        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        private static extern float FMod(float x, float y);

        [SuppressGCTransition]
        [DllImport(RuntimeHelpers.QCall, CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe float ModF(float x, float* intptr);
    }
}
