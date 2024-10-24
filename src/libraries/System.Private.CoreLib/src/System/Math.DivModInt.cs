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

            return InternalDivInt32(dividend, divisor);
        }

        [StackTraceHidden]
        internal static uint DivUInt32(uint dividend, uint divisor)
        {
            if (divisor == 0)
            {
                ThrowHelper.ThrowDivideByZeroException();
            }

            return InternalDivUInt32(dividend, divisor);
        }

        [StackTraceHidden]
        internal static long DivInt64(long dividend, long divisor)
        {
            if (Is32BitSigned(divisor))
            {
                if ((int)divisor == 0)
                {
                    ThrowHelper.ThrowDivideByZeroException();
                }

                if ((int)divisor == -1)
                {
                    if ((ulong)dividend == 0x8000000000000000ul)
                    {
                        ThrowHelper.ThrowOverflowException();
                    }
                    return -dividend;
                }

                // Check for -ive or +ive numbers in the range -2**31 to 2**31
                if (Is32BitSigned(dividend))
                {
                    return (int)dividend / (int)divisor;
                }
            }

            return InternalDivInt64(dividend, divisor);
        }

        [StackTraceHidden]
        internal static ulong DivUInt64(ulong dividend, ulong divisor)
        {
            if (Hi32Bits(divisor) == 0)
            {
                if ((uint)divisor == 0)
                {
                    ThrowHelper.ThrowDivideByZeroException();
                }

                if (Hi32Bits(dividend) == 0)
                    return (uint)dividend / (uint)divisor;
            }

            return InternalDivUInt64(dividend, divisor);
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

            return InternalModInt32(dividend, divisor);
        }

        [StackTraceHidden]
        internal static uint ModUInt32(uint dividend, uint divisor)
        {
            if (divisor == 0)
            {
                ThrowHelper.ThrowDivideByZeroException();
            }

            return InternalModUInt32(dividend, divisor);
        }

        [StackTraceHidden]
        internal static long ModInt64(long dividend, long divisor)
        {
            if (Is32BitSigned(divisor))
            {
                if ((int)divisor == 0)
                {
                    ThrowHelper.ThrowDivideByZeroException();
                }

                if ((int)divisor == -1)
                {
                    if ((ulong)dividend == 0x8000000000000000ul)
                    {
                        ThrowHelper.ThrowOverflowException();
                    }
                    return 0;
                }

                if (Is32BitSigned(dividend))
                {
                    return (int)dividend % (int)divisor;
                }
            }

            return InternalModInt64(dividend, divisor);
        }

        [StackTraceHidden]
        internal static ulong ModUInt64(ulong dividend, ulong divisor)
        {
            if (Hi32Bits(divisor) == 0)
            {
                if ((uint)divisor == 0)
                {
                    ThrowHelper.ThrowDivideByZeroException();
                }

                if (Hi32Bits(dividend) == 0)
                    return (uint)dividend % (uint)divisor;
            }

            return InternalModUInt64(dividend, divisor);
        }

        // helper method to get high 32-bit of 64-bit int
        private static uint Hi32Bits<T>(T a) where T : INumber<T>
        {
            return (uint)(ulong.CreateTruncating(a) >> 32);
        }

        // helper method to check whether 64-bit signed int fits into 32-bit signed (compiles into one 32-bit compare)
        private static bool Is32BitSigned<T>(T a) where T : INumber<T>
        {
            return Hi32Bits(a) == Hi32Bits(long.CreateTruncating(int.CreateTruncating(a)));
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int InternalDivInt32(int dividend, int divisor);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern uint InternalDivUInt32(uint dividend, uint divisor);

#if NATIVEAOT
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "InternalDivInt64"), SuppressGCTransition]
        private static partial long InternalDivInt64(long dividend, long divisor);
#else
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern long InternalDivInt64(long dividend, long divisor);
#endif

#if NATIVEAOT
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "InternalDivUInt64"), SuppressGCTransition]
        private static partial ulong InternalDivUInt64(ulong dividend, ulong divisor);
#else
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern ulong InternalDivUInt64(ulong dividend, ulong divisor);
#endif

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int InternalModInt32(int dividend, int divisor);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern uint InternalModUInt32(uint dividend, uint divisor);

#if NATIVEAOT
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "InternalModInt64"), SuppressGCTransition]
        private static partial long InternalModInt64(long dividend, long divisor);
#else
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern long InternalModInt64(long dividend, long divisor);
#endif

#if NATIVEAOT
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "InternalModUInt64"), SuppressGCTransition]
        private static partial ulong InternalModUInt64(ulong dividend, ulong divisor);
#else
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern ulong InternalModUInt64(ulong dividend, ulong divisor);
#endif
    }
}
