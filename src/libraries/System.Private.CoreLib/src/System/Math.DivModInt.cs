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

            return DivMod<int, uint>(dividend, divisor).quotient;
        }

        [StackTraceHidden]
        internal static uint DivUInt32(uint dividend, uint divisor)
        {
            if (divisor == 0)
            {
                ThrowHelper.ThrowDivideByZeroException();
            }

            return DivMod<uint, uint>(dividend, divisor).quotient;
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
                    return DivMod<int, uint>((int)dividend, (int)divisor).quotient;
                }
            }

            return DivMod<long, ulong>(dividend, divisor).quotient;
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
                    return DivMod<ulong, ulong>((uint)dividend, (uint)divisor).quotient;
                }
            }

            return DivMod<ulong, ulong>(dividend, divisor).quotient;
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

            return DivMod<int, uint>(dividend, divisor).remainder;
        }

        [StackTraceHidden]
        internal static uint ModUInt32(uint dividend, uint divisor)
        {
            if (divisor == 0)
            {
                ThrowHelper.ThrowDivideByZeroException();
            }

            return DivMod<uint, uint>(dividend, divisor).remainder;
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
                    return DivMod<long, ulong>((int)dividend, (int)divisor).remainder;
                }
            }

            return DivMod<long, ulong>(dividend, divisor).remainder;
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
                    return DivMod<uint, uint>((uint)dividend, (uint)divisor).remainder;
                }
            }

            return DivMod<ulong, ulong>(dividend, divisor).remainder;
        }

        private static (T quotient, T remainder) DivMod<T, U>(T dividend, T divisor)
            where T : INumber<T>, IMinMaxValue<T>
            where U : INumber<U>, IMinMaxValue<U>, IShiftOperators<U, int, U>, IBitwiseOperators<U, U, U>
        {
            bool dividendIsNegative = false;
            bool divisorIsNegative = false;

            // Handle signs if T is signed
            if (typeof(T) != typeof(U))
            {
                if (dividend < T.Zero)
                {
                    dividend = -dividend;
                    dividendIsNegative = true;
                }
                if (divisor < T.Zero)
                {
                    divisor = -divisor;
                    divisorIsNegative = true;
                }
            }

            // Perform unsigned division using type U
            U uDividend = U.CreateTruncating(dividend);
            U uDivisor = U.CreateTruncating(divisor);

            U bit = U.One;
            U uQuotient = U.Zero;
            int mask = typeof(U) == typeof(uint) ? 31 : 63;

            // Align divisor with dividend
            while (uDivisor < uDividend && bit != U.Zero && U.IsZero(uDivisor & (U.One << mask)))
            {
                uDivisor <<= 1;
                bit <<= 1;
            }

            // Perform the division
            while (bit > U.Zero)
            {
                if (uDividend >= uDivisor)
                {
                    uDividend -= uDivisor;
                    uQuotient |= bit;
                }
                bit >>= 1;
                uDivisor >>= 1;
            }

            // Convert results back to type T
            T tQuotient = T.CreateTruncating(uQuotient);
            T tRemainder = T.CreateTruncating(uDividend);

            // Adjust signs if T is signed
            if (typeof(T) != typeof(U))
            {
                if (dividendIsNegative)
                    tRemainder = -tRemainder;
                if (dividendIsNegative ^ divisorIsNegative)
                    tQuotient = -tQuotient;
            }

            return (tQuotient, tRemainder);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int DivInt32Internal(int dividend, int divisor);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern uint DivUInt32Internal(uint dividend, uint divisor);

/*
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
*/
    }
}
