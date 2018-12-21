// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Some floating-point math operations
**
** 
===========================================================*/

//This class contains only static members and doesn't require serialization.

using System.Runtime.CompilerServices;

namespace System
{
    public static partial class Math
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Abs(double value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern float Abs(float value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Acos(double d);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Acosh(double d);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Asin(double d);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Asinh(double d);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Atan(double d);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Atan2(double y, double x);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Atanh(double d);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Cbrt(double d);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Ceiling(double a);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Cos(double d);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Cosh(double value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Exp(double d);

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

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Log10(double d);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Pow(double x, double y);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double ScaleB(double x, int n);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Sin(double a);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Sinh(double value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Sqrt(double d);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Tan(double a);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern double Tanh(double value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern double FMod(double x, double y);
        
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe double ModF(double x, double* intptr);
    }
}
