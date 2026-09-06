// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Purpose: Some single-precision floating-point math operations
**
===========================================================*/

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System
{
    public static partial class MathF
    {
        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe float Acos(float x);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe float Acosh(float x);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe float Asin(float x);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe float Asinh(float x);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe float Atan(float x);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe float Atanh(float x);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe float Atan2(float y, float x);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe float Cbrt(float x);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe float Ceiling(float x);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe float Cos(float x);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe float Cosh(float x);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe float Exp(float x);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe float Floor(float x);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe float FusedMultiplyAdd(float x, float y, float z);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe float Log(float x);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe float Log2(float x);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe float Log10(float x);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe float Pow(float x, float y);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe float Sin(float x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe (float Sin, float Cos) SinCos(float x)
        {
            if (RuntimeHelpers.IsKnownConstant(x))
            {
                return (Sin(x), Cos(x));
            }

            float sin, cos;
            SinCos(x, &sin, &cos);
            return (sin, cos);
        }

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe float Sinh(float x);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe float Sqrt(float x);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe float Tan(float x);

        /// <safety>Implemented by the runtime as an FCall that computes a result from the argument values alone; it dereferences no caller-supplied memory.</safety>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern safe float Tanh(float x);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe float ModF(float x, float* intptr);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe void SinCos(float x, float* sin, float* cos);
    }
}
