// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System
{
    internal static partial class Number
    {
        public static unsafe void DoubleToNumber(double value, int precision, ref NumberBuffer number)
        {
            fixed (NumberBuffer* numberPtr = &number)
            {
                DoubleToNumber(value, precision, (byte*)numberPtr);
            }
        }
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe void DoubleToNumber(double value, int precision, byte* number);


        public static unsafe double NumberToDouble(ref NumberBuffer number)
        {
            fixed (NumberBuffer* numberPtr = &number)
            {
                double d = 0;
                NumberToDouble((byte*)numberPtr, &d);
                return d;
            }
        }
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe void NumberToDouble(byte* number, double* result);


        public static unsafe bool NumberBufferToDecimal(ref Number.NumberBuffer number, ref decimal value)
        {
            fixed (NumberBuffer* numberPtr = &number)
            {
                return NumberBufferToDecimal((byte*)numberPtr, ref value);
            }
        }
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe bool NumberBufferToDecimal(byte* number, ref decimal value);
    }
}
