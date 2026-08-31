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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System
{
    public static partial class Math
    {
        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe double Acos(double d);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe double Acosh(double d);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe double Asin(double d);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe double Asinh(double d);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe double Atan(double d);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe double Atanh(double d);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe double Atan2(double y, double x);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe double Cbrt(double d);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe double Ceiling(double a);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe double Cos(double d);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe double Cosh(double value);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe double Exp(double d);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe double Floor(double d);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe double FusedMultiplyAdd(double x, double y, double z);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe double Log(double d);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe double Log2(double x);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe double Log10(double d);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe double Pow(double x, double y);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe double Sin(double a);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe (double Sin, double Cos) SinCos(double x)
        {
            if (RuntimeHelpers.IsKnownConstant(x))
            {
                return (Sin(x), Cos(x));
            }

            double sin, cos;
            SinCos(x, &sin, &cos);
            return (sin, cos);
        }

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe double Sinh(double value);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe double Sqrt(double d);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe double Tan(double a);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe double Tanh(double value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe double ModF(double x, double* intptr);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe void SinCos(double x, double* sin, double* cos);
    }
}
