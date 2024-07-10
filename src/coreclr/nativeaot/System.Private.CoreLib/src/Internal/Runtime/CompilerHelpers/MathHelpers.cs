// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// Math helpers for generated code. The helpers here are referenced by the runtime.
    /// </summary>
    [StackTraceHidden]
    internal static partial class MathHelpers
    {
#if !TARGET_64BIT
        private const string RuntimeLibrary = "*";

        [LibraryImport(RuntimeLibrary)]
        [SuppressGCTransition]
        private static partial ulong RhpULMod(ulong dividend, ulong divisor);

        public static ulong ULMod(ulong dividend, ulong divisor)
        {
            if (divisor == 0)
                ThrowHelper.ThrowDivideByZeroException();

            return RhpULMod(dividend, divisor);
        }

        [LibraryImport(RuntimeLibrary)]
        [SuppressGCTransition]
        private static partial long RhpLMod(long dividend, long divisor);

        public static long LMod(long dividend, long divisor)
        {
            if (divisor == 0)
                ThrowHelper.ThrowDivideByZeroException();
            if (divisor == -1 && dividend == long.MinValue)
                ThrowHelper.ThrowOverflowException();

            return RhpLMod(dividend, divisor);
        }

        [LibraryImport(RuntimeLibrary)]
        [SuppressGCTransition]
        private static partial ulong RhpULDiv(ulong dividend, ulong divisor);

        public static ulong ULDiv(ulong dividend, ulong divisor)
        {
            if (divisor == 0)
                ThrowHelper.ThrowDivideByZeroException();

            return RhpULDiv(dividend, divisor);
        }

        [LibraryImport(RuntimeLibrary)]
        [SuppressGCTransition]
        private static partial long RhpLDiv(long dividend, long divisor);

        public static long LDiv(long dividend, long divisor)
        {
            if (divisor == 0)
                ThrowHelper.ThrowDivideByZeroException();
            if (divisor == -1 && dividend == long.MinValue)
                ThrowHelper.ThrowOverflowException();

            return RhpLDiv(dividend, divisor);
        }

#if TARGET_ARM
        [RuntimeImport(RuntimeLibrary, "RhpIDiv")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int RhpIDiv(int dividend, int divisor);

        public static int IDiv(int dividend, int divisor)
        {
            if (divisor == 0)
                ThrowHelper.ThrowDivideByZeroException();
            if (divisor == -1 && dividend == int.MinValue)
                ThrowHelper.ThrowOverflowException();

            return RhpIDiv(dividend, divisor);
        }

        [RuntimeImport(RuntimeLibrary, "RhpUDiv")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern uint RhpUDiv(uint dividend, uint divisor);

        public static long UDiv(uint dividend, uint divisor)
        {
            if (divisor == 0)
                ThrowHelper.ThrowDivideByZeroException();

            return RhpUDiv(dividend, divisor);
        }

        [RuntimeImport(RuntimeLibrary, "RhpIMod")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int RhpIMod(int dividend, int divisor);

        public static int IMod(int dividend, int divisor)
        {
            if (divisor == 0)
                ThrowHelper.ThrowDivideByZeroException();
            if (divisor == -1 && dividend == int.MinValue)
                ThrowHelper.ThrowOverflowException();

            return RhpIMod(dividend, divisor);
        }

        [RuntimeImport(RuntimeLibrary, "RhpUMod")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern uint RhpUMod(uint dividend, uint divisor);

        public static long UMod(uint dividend, uint divisor)
        {
            if (divisor == 0)
                ThrowHelper.ThrowDivideByZeroException();

            return RhpUMod(dividend, divisor);
        }
#endif // TARGET_ARM
#endif // TARGET_64BIT
    }
}
