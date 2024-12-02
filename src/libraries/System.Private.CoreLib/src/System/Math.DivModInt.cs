// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

#if NATIVEAOT
using System.Runtime.InteropServices;
#endif

namespace System
{
    /// <summary>
    /// Provides constants and static methods for trigonometric, logarithmic, and other common mathematical functions.
    /// </summary>
    public static partial class Math
    {
        [StackTraceHidden]
        internal static int DivInt32(int dividend, int divisor)
        {
            if ((uint)(divisor + 1) <= 1) // Unsigned test for divisor in [-1 .. 0]
            {
                if (divisor == 0)
                {
                    ThrowHelper.ThrowDivideByZeroException();
                }
                else if (divisor == -1)
                {
                    if (dividend == int.MinValue)
                    {
                        ThrowHelper.ThrowOverflowException();
                    }
                    return -dividend;
                }
            }

            return DivModSigned<int, uint>(dividend, divisor).quotient;
        }

        [StackTraceHidden]
        internal static uint DivUInt32(uint dividend, uint divisor)
        {
            if (divisor == 0)
            {
                ThrowHelper.ThrowDivideByZeroException();
            }

            return DivModUnsigned(dividend, divisor).quotient;
        }

        [StackTraceHidden]
        internal static long DivInt64(long dividend, long divisor)
        {
            if ((int)((ulong)divisor >> 32) == (int)(((ulong)(int)divisor) >> 32))
            {
                if ((int)divisor == 0)
                {
                    ThrowHelper.ThrowDivideByZeroException();
                }

                if ((int)divisor == -1)
                {
                    if (dividend == long.MinValue)
                    {
                        ThrowHelper.ThrowOverflowException();
                    }
                    return -dividend;
                }

                // Check for -ive or +ive numbers in the range -2**31 to 2**31
                if ((int)((ulong)dividend >> 32) == (int)(((ulong)(int)dividend) >> 32))
                {
                    return DivModSigned<int, uint>((int)dividend, (int)divisor).quotient;
                }
            }

            return DivModSigned<long, ulong>(dividend, divisor).quotient;
        }

        [StackTraceHidden]
        internal static ulong DivUInt64(ulong dividend, ulong divisor)
        {
            if ((int)(divisor >> 32) == 0)
            {
                if ((uint)divisor == 0)
                {
                    ThrowHelper.ThrowDivideByZeroException();
                }

                if ((int)(dividend >> 32) == 0)
                {
                    return DivModUnsigned((uint)dividend, (uint)divisor).quotient;
                }
            }

            return DivModUnsigned(dividend, divisor).quotient;
        }

        [StackTraceHidden]
        internal static int ModInt32(int dividend, int divisor)
        {
            if ((uint)(divisor + 1) <= 1) // Unsigned test for divisor in [-1 .. 0]
            {
                if (divisor == 0)
                {
                    ThrowHelper.ThrowDivideByZeroException();
                }
                else if (divisor == -1)
                {
                    if (dividend == int.MinValue)
                    {
                        ThrowHelper.ThrowOverflowException();
                    }
                    return 0;
                }
            }

            return DivModSigned<int, uint>(dividend, divisor).remainder;
        }

        [StackTraceHidden]
        internal static uint ModUInt32(uint dividend, uint divisor)
        {
            if (divisor == 0)
            {
                ThrowHelper.ThrowDivideByZeroException();
            }

            return DivModUnsigned(dividend, divisor).remainder;
        }

        [StackTraceHidden]
        internal static long ModInt64(long dividend, long divisor)
        {
            if ((int)((ulong)divisor >> 32) == (int)(((ulong)(int)divisor) >> 32))
            {
                if ((int)divisor == 0)
                {
                    ThrowHelper.ThrowDivideByZeroException();
                }

                if ((int)divisor == -1)
                {
                    if (dividend == long.MinValue)
                    {
                        ThrowHelper.ThrowOverflowException();
                    }
                    return 0;
                }

                if ((int)((ulong)dividend >> 32) == (int)(((ulong)(int)dividend) >> 32))
                {
                    return DivModSigned<long, ulong>((int)dividend, (int)divisor).remainder;
                }
            }

            return DivModSigned<long, ulong>(dividend, divisor).remainder;
        }

        [StackTraceHidden]
        internal static ulong ModUInt64(ulong dividend, ulong divisor)
        {
            if ((int)(divisor >> 32) == 0)
            {
                if ((uint)divisor == 0)
                {
                    ThrowHelper.ThrowDivideByZeroException();
                }

                if ((int)(dividend >> 32) == 0)
                {
                    return DivModUnsigned((uint)dividend, (uint)divisor).remainder;
                }
            }

            return DivModUnsigned(dividend, divisor).remainder;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static (T quotient, T remainder) DivModUnsigned<T>(T dividend, T divisor)
            where T : INumber<T>, IMinMaxValue<T>, IShiftOperators<T, int, T>, IBitwiseOperators<T, T, T>
        {
            T bit = T.One;
            T quotient = T.Zero;
            int mask = typeof(T) == typeof(int) || typeof(T) == typeof(uint) ? 31 : 63;

            // Align divisor with dividend: align the divisor with the most significant bit of the dividend
            while (divisor < dividend && bit != T.Zero && T.IsZero(divisor & (T.One << mask)))
            {
                divisor <<= 1;
                bit <<= 1;
            }

            // Perform the division
            while (bit > T.Zero)
            {
                if (dividend >= divisor)
                {
                    dividend -= divisor;
                    quotient |= bit;
                }
                bit >>= 1;
                divisor >>= 1;
            }

            // Return the result as a tuple (quotient, remainder)
            return (quotient, dividend);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static (T quotient, T remainder) DivModSigned<T, U>(T dividend, T divisor)
            where T : INumber<T>, IMinMaxValue<T>, IShiftOperators<T, int, T>, IBitwiseOperators<T, T, T>
            where U : INumber<U>, IMinMaxValue<U>, IShiftOperators<U, int, U>, IBitwiseOperators<U, U, U>
        {
            bool dividendIsNegative = dividend < T.Zero;
            bool divisorIsNegative = divisor < T.Zero;

            dividend = dividendIsNegative ? -dividend : dividend;
            divisor = divisorIsNegative ? -divisor : divisor;

            // Use unsigned DivMod method for absolute values
            (U quotient, U remainder) = DivModUnsigned<U>(U.CreateTruncating(dividend), U.CreateTruncating(divisor));

            // Convert the quotient and remainder back to T
            T tQuotient = T.CreateTruncating(quotient);
            T tRemainder = T.CreateTruncating(remainder);

            // Adjust the signs if necessary
            if (dividendIsNegative)
                tRemainder = -tRemainder;
            if (dividendIsNegative ^ divisorIsNegative)
                tQuotient = -tQuotient;

            return (tQuotient, tRemainder);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int DivInt32Internal(int dividend, int divisor);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern uint DivUInt32Internal(uint dividend, uint divisor);

#if NATIVEAOT
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DivInt64Internal"), SuppressGCTransition]
        private static partial long DivInt64Internal(long dividend, long divisor);
#else
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern long DivInt64Internal(long dividend, long divisor);
#endif

#if NATIVEAOT
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "DivUInt64Internal"), SuppressGCTransition]
        private static partial ulong DivUInt64Internal(ulong dividend, ulong divisor);
#else
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern ulong DivUInt64Internal(ulong dividend, ulong divisor);
#endif

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int ModInt32Internal(int dividend, int divisor);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern uint ModUInt32Internal(uint dividend, uint divisor);

#if NATIVEAOT
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ModInt64Internal"), SuppressGCTransition]
        private static partial long ModInt64Internal(long dividend, long divisor);
#else
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern long ModInt64Internal(long dividend, long divisor);
#endif

#if NATIVEAOT
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ModUInt64Internal"), SuppressGCTransition]
        private static partial ulong ModUInt64Internal(ulong dividend, ulong divisor);
#else
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern ulong ModUInt64Internal(ulong dividend, ulong divisor);
#endif
    }
}
