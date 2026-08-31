// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

#if NATIVEAOT
using System.Runtime;
#endif

namespace System
{
    /// <summary>
    /// Math helpers for generated code. The helpers here are referenced by the runtime.
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
                    return DivInt32Internal((int)dividend, (int)divisor);
                }
            }

            return DivInt64Internal(dividend, divisor);
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
                    return DivUInt32Internal((uint)dividend, (uint)divisor);
                }
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
                    return ModInt32Internal((int)dividend, (int)divisor);
                }
            }

            return ModInt64Internal(dividend, divisor);
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
                    return ModUInt32Internal((uint)dividend, (uint)divisor);
                }
            }

            return ModUInt64Internal(dividend, divisor);
        }

#if NATIVEAOT
        private const string RuntimeLibrary = "*";
#endif

        [MethodImpl(MethodImplOptions.InternalCall)]
#if NATIVEAOT
        [RuntimeImport(RuntimeLibrary, "DivInt32Internal")]
#endif
        private static extern int DivInt32Internal(int dividend, int divisor);

        [MethodImpl(MethodImplOptions.InternalCall)]
#if NATIVEAOT
        [RuntimeImport(RuntimeLibrary, "DivUInt32Internal")]
#endif
        private static extern uint DivUInt32Internal(uint dividend, uint divisor);

        [MethodImpl(MethodImplOptions.InternalCall)]
#if NATIVEAOT
        [RuntimeImport(RuntimeLibrary, "DivInt64Internal")]
#endif
        private static extern long DivInt64Internal(long dividend, long divisor);

        [MethodImpl(MethodImplOptions.InternalCall)]
#if NATIVEAOT
        [RuntimeImport(RuntimeLibrary, "DivUInt64Internal")]
#endif
        private static extern ulong DivUInt64Internal(ulong dividend, ulong divisor);

        [MethodImpl(MethodImplOptions.InternalCall)]
#if NATIVEAOT
        [RuntimeImport(RuntimeLibrary, "ModInt32Internal")]
#endif
        private static extern int ModInt32Internal(int dividend, int divisor);

        [MethodImpl(MethodImplOptions.InternalCall)]
#if NATIVEAOT
        [RuntimeImport(RuntimeLibrary, "ModUInt32Internal")]
#endif
        private static extern uint ModUInt32Internal(uint dividend, uint divisor);

        [MethodImpl(MethodImplOptions.InternalCall)]
#if NATIVEAOT
        [RuntimeImport(RuntimeLibrary, "ModInt64Internal")]
#endif
        private static extern long ModInt64Internal(long dividend, long divisor);

        [MethodImpl(MethodImplOptions.InternalCall)]
#if NATIVEAOT
        [RuntimeImport(RuntimeLibrary, "ModUInt64Internal")]
#endif
        private static extern ulong ModUInt64Internal(ulong dividend, ulong divisor);
    }
}
