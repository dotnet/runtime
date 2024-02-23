// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// Math helpers for generated code. The helpers marked with [RuntimeExport] and the type
    /// itself need to be public because they constitute a public contract with the .NET Native toolchain.
    /// </summary>
    internal static class MathHelpers
    {
#if !TARGET_64BIT
        private const string RuntimeLibrary = "*";

        [RuntimeImport(RuntimeLibrary, "RhpULMod")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern ulong RhpULMod(ulong i, ulong j);

        public static ulong ULMod(ulong i, ulong j)
        {
            if (j == 0)
                return ThrowULngDivByZero();
            else
                return RhpULMod(i, j);
        }

        [RuntimeImport(RuntimeLibrary, "RhpLMod")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern long RhpLMod(long i, long j);

        public static long LMod(long i, long j)
        {
            if (j == 0)
                return ThrowLngDivByZero();
            else if (j == -1 && i == long.MinValue)
                return ThrowLngOvf();
            else
                return RhpLMod(i, j);
        }

        [RuntimeImport(RuntimeLibrary, "RhpULDiv")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern ulong RhpULDiv(ulong i, ulong j);

        public static ulong ULDiv(ulong i, ulong j)
        {
            if (j == 0)
                return ThrowULngDivByZero();
            else
                return RhpULDiv(i, j);
        }

        [RuntimeImport(RuntimeLibrary, "RhpLDiv")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern long RhpLDiv(long i, long j);

        public static long LDiv(long i, long j)
        {
            if (j == 0)
                return ThrowLngDivByZero();
            else if (j == -1 && i == long.MinValue)
                return ThrowLngOvf();
            else
                return RhpLDiv(i, j);
        }

#if TARGET_ARM
        [RuntimeImport(RuntimeLibrary, "RhpIDiv")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int RhpIDiv(int i, int j);

        public static int IDiv(int i, int j)
        {
            if (j == 0)
                return ThrowIntDivByZero();
            else if (j == -1 && i == int.MinValue)
                return ThrowIntOvf();
            else
                return RhpIDiv(i, j);
        }

        [RuntimeImport(RuntimeLibrary, "RhpUDiv")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern uint RhpUDiv(uint i, uint j);

        public static long UDiv(uint i, uint j)
        {
            if (j == 0)
                return ThrowUIntDivByZero();
            else
                return RhpUDiv(i, j);
        }

        [RuntimeImport(RuntimeLibrary, "RhpIMod")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int RhpIMod(int i, int j);

        public static int IMod(int i, int j)
        {
            if (j == 0)
                return ThrowIntDivByZero();
            else if (j == -1 && i == int.MinValue)
                return ThrowIntOvf();
            else
                return RhpIMod(i, j);
        }

        [RuntimeImport(RuntimeLibrary, "RhpUMod")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern uint RhpUMod(uint i, uint j);

        public static long UMod(uint i, uint j)
        {
            if (j == 0)
                return ThrowUIntDivByZero();
            else
                return RhpUMod(i, j);
        }
#endif // TARGET_ARM

        //
        // Matching return types of throw helpers enables tailcalling them. It improves performance
        // of the hot path because of it does not need to raise full stackframe.
        //
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static long ThrowLngOvf()
        {
            throw new OverflowException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static long ThrowLngDivByZero()
        {
            throw new DivideByZeroException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ulong ThrowULngDivByZero()
        {
            throw new DivideByZeroException();
        }

#if TARGET_ARM
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ThrowIntOvf()
        {
            throw new OverflowException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ThrowIntDivByZero()
        {
            throw new DivideByZeroException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint ThrowUIntDivByZero()
        {
            throw new DivideByZeroException();
        }
#endif // TARGET_ARM
#endif // TARGET_64BIT
    }
}
