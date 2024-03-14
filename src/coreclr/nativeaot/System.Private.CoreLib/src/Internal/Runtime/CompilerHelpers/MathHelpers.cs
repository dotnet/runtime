// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// Math helpers for generated code. The helpers here are referenced by the runtime.
    /// </summary>
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
                ThrowULngDivByZero();

            return RhpULMod(dividend, divisor);
        }

        [LibraryImport(RuntimeLibrary)]
        [SuppressGCTransition]
        private static partial long RhpLMod(long dividend, long divisor);

        public static long LMod(long dividend, long divisor)
        {
            if (divisor == 0)
                ThrowLngDivByZero();
            if (divisor == -1 && dividend == long.MinValue)
                ThrowLngOvf();

            return RhpLMod(dividend, divisor);
        }

        [LibraryImport(RuntimeLibrary)]
        [SuppressGCTransition]
        private static partial ulong RhpULDiv(ulong dividend, ulong divisor);

        public static ulong ULDiv(ulong dividend, ulong divisor)
        {
            if (divisor == 0)
                ThrowULngDivByZero();

            return RhpULDiv(dividend, divisor);
        }

        [LibraryImport(RuntimeLibrary)]
        [SuppressGCTransition]
        private static partial long RhpLDiv(long dividend, long divisor);

        public static long LDiv(long dividend, long divisor)
        {
            if (divisor == 0)
                ThrowLngDivByZero();
            if (divisor == -1 && dividend == long.MinValue)
                ThrowLngOvf();

            return RhpLDiv(dividend, divisor);
        }

#if TARGET_ARM
        [RuntimeImport(RuntimeLibrary, "RhpIDiv")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int RhpIDiv(int dividend, int j);

        public static int IDiv(int dividend, int divisor)
        {
            if (divisor == 0)
                ThrowIntDivByZero();
            if (divisor == -1 && dividend == int.MinValue)
                ThrowIntOvf();

            return RhpIDiv(dividend, divisor);
        }

        [RuntimeImport(RuntimeLibrary, "RhpUDiv")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern uint RhpUDiv(uint dividend, uint divisor);

        public static long UDiv(uint dividend, uint divisor)
        {
            if (divisor == 0)
                ThrowUIntDivByZero();

            return RhpUDiv(dividend, divisor);
        }

        [RuntimeImport(RuntimeLibrary, "RhpIMod")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int RhpIMod(int dividend, int divisor);

        public static int IMod(int dividend, int divisor)
        {
            if (divisor == 0)
                ThrowIntDivByZero();
            if (divisor == -1 && dividend == int.MinValue)
                ThrowIntOvf();

            return RhpIMod(dividend, divisor);
        }

        [RuntimeImport(RuntimeLibrary, "RhpUMod")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern uint RhpUMod(uint dividend, uint divisor);

        public static long UMod(uint dividend, uint divisor)
        {
            if (divisor == 0)
                ThrowUIntDivByZero();

            return RhpUMod(dividend, divisor);
        }
#endif // TARGET_ARM

        //
        // Matching return types of throw helpers enables tailcalling them. It improves performance
        // of the hot path because of it does not need to raise full stackframe.
        //
        private static void ThrowLngOvf()
        {
            throw new OverflowException();
        }

        private static void ThrowLngDivByZero()
        {
            throw new DivideByZeroException();
        }

        private static void ThrowULngDivByZero()
        {
            throw new DivideByZeroException();
        }

#if TARGET_ARM
        private static void ThrowIntOvf()
        {
            throw new OverflowException();
        }

        private static void ThrowIntDivByZero()
        {
            throw new DivideByZeroException();
        }

        private static void ThrowUIntDivByZero()
        {
            throw new DivideByZeroException();
        }
#endif // TARGET_ARM
#endif // TARGET_64BIT
    }
}
