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

            return DivInt32Internal(dividend, divisor);
        }

        [StackTraceHidden]
        internal static uint DivUInt32(uint dividend, uint divisor)
        {
            if (divisor == 0)
            {
                ThrowHelper.ThrowDivideByZeroException();
            }

            return DivUInt32Internal(dividend, divisor);
        }

        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static long DivInt64(long dividend, long divisor)
        {
            if ((uint)(int)((ulong)divisor >> 32) == (uint)(int)(((ulong)(int)divisor) >> 32))
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
                if ((uint)(int)((ulong)dividend >> 32) == (uint)(int)(((ulong)(int)dividend) >> 32))
                {
                    return (int)dividend / (int)divisor;
                }
            }

            return DivInt64Internal(dividend, divisor);
        }

        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static ulong DivUInt64(ulong dividend, ulong divisor)
        {
            if ((uint)(int)(divisor >> 32) == 0)
            {
                if ((uint)divisor == 0)
                {
                    ThrowHelper.ThrowDivideByZeroException();
                }

                if ((uint)(int)(dividend >> 32) == 0)
                    return (uint)dividend / (uint)divisor;
            }

            return DivUInt64Internal(dividend, divisor);
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

            return ModInt32Internal(dividend, divisor);
        }

        [StackTraceHidden]
        internal static uint ModUInt32(uint dividend, uint divisor)
        {
            if (divisor == 0)
            {
                ThrowHelper.ThrowDivideByZeroException();
            }

            return ModUInt32Internal(dividend, divisor);
        }

        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static long ModInt64(long dividend, long divisor)
        {
            if ((uint)(int)((ulong)divisor >> 32) == (uint)(int)(((ulong)(int)divisor) >> 32))
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

                if ((uint)(int)((ulong)dividend >> 32) == (uint)(int)(((ulong)(int)dividend) >> 32))
                {
                    return (int)dividend % (int)divisor;
                }
            }

            return ModInt64Internal(dividend, divisor);
        }

        [StackTraceHidden]
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static ulong ModUInt64(ulong dividend, ulong divisor)
        {
            if ((uint)(int)(divisor >> 32) == 0)
            {
                if ((uint)divisor == 0)
                {
                    ThrowHelper.ThrowDivideByZeroException();
                }

                if ((uint)(int)(dividend >> 32) == 0)
                    return (uint)dividend % (uint)divisor;
            }

            return ModUInt64Internal(dividend, divisor);
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
