// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;

using Internal.Runtime;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// Math helpers for generated code. The helpers marked with [RuntimeExport] and the type
    /// itself need to be public because they constitute a public contract with the .NET Native toolchain.
    /// </summary>
    internal static class MathHelpers
    {
#if !TARGET_64BIT
        //
        // 64-bit checked multiplication for 32-bit platforms
        //

        private const string RuntimeLibrary = "*";

        // Helper to multiply two 32-bit uints
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Mul32x32To64(uint a, uint b)
        {
            return a * (ulong)b;
        }

        // Helper to get high 32-bit of 64-bit int
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Hi32Bits(long a)
        {
            return (uint)(a >> 32);
        }

        // Helper to get high 32-bit of 64-bit int
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Hi32Bits(ulong a)
        {
            return (uint)(a >> 32);
        }

        [RuntimeExport("LMulOvf")]
        public static long LMulOvf(long i, long j)
        {
            long ret;

            // Remember the sign of the result
            int sign = (int)(Hi32Bits(i) ^ Hi32Bits(j));

            // Convert to unsigned multiplication
            if (i < 0) i = -i;
            if (j < 0) j = -j;

            // Get the upper 32 bits of the numbers
            uint val1High = Hi32Bits(i);
            uint val2High = Hi32Bits(j);

            ulong valMid;

            if (val1High == 0)
            {
                // Compute the 'middle' bits of the long multiplication
                valMid = Mul32x32To64(val2High, (uint)i);
            }
            else
            {
                if (val2High != 0)
                    goto ThrowExcep;
                // Compute the 'middle' bits of the long multiplication
                valMid = Mul32x32To64(val1High, (uint)j);
            }

            // See if any bits after bit 32 are set
            if (Hi32Bits(valMid) != 0)
                goto ThrowExcep;

            ret = (long)(Mul32x32To64((uint)i, (uint)j) + (valMid << 32));

            // check for overflow
            if (Hi32Bits(ret) < (uint)valMid)
                goto ThrowExcep;

            if (sign >= 0)
            {
                // have we spilled into the sign bit?
                if (ret < 0)
                    goto ThrowExcep;
            }
            else
            {
                ret = -ret;
                // have we spilled into the sign bit?
                if (ret > 0)
                    goto ThrowExcep;
            }
            return ret;

        ThrowExcep:
            return ThrowLngOvf();
        }

        [RuntimeExport("ULMulOvf")]
        public static ulong ULMulOvf(ulong i, ulong j)
        {
            ulong ret;

            // Get the upper 32 bits of the numbers
            uint val1High = Hi32Bits(i);
            uint val2High = Hi32Bits(j);

            ulong valMid;

            if (val1High == 0)
            {
                if (val2High == 0)
                    return Mul32x32To64((uint)i, (uint)j);
                // Compute the 'middle' bits of the long multiplication
                valMid = Mul32x32To64(val2High, (uint)i);
            }
            else
            {
                if (val2High != 0)
                    goto ThrowExcep;
                // Compute the 'middle' bits of the long multiplication
                valMid = Mul32x32To64(val1High, (uint)j);
            }

            // See if any bits after bit 32 are set
            if (Hi32Bits(valMid) != 0)
                goto ThrowExcep;

            ret = Mul32x32To64((uint)i, (uint)j) + (valMid << 32);

            // check for overflow
            if (Hi32Bits(ret) < (uint)valMid)
                goto ThrowExcep;
            return ret;

        ThrowExcep:
            return ThrowULngOvf();
        }

        [RuntimeImport(RuntimeLibrary, "RhpULMod")]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern ulong RhpULMod(ulong i, ulong j);

        public static ulong ULMod(ulong i, ulong j)
        {
            if (j == 0)
                return ThrowULngDivByZero();
            else
                return RhpULMod(i, j);
        }

        [RuntimeImport(RuntimeLibrary, "RhpLMod")]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
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
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern ulong RhpULDiv(ulong i, ulong j);

        public static ulong ULDiv(ulong i, ulong j)
        {
            if (j == 0)
                return ThrowULngDivByZero();
            else
                return RhpULDiv(i, j);
        }

        [RuntimeImport(RuntimeLibrary, "RhpLDiv")]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
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
#endif // TARGET_64BIT

        [RuntimeExport("Dbl2IntOvf")]
        public static int Dbl2IntOvf(double val)
        {
            const double two31 = 2147483648.0;

            // Note that this expression also works properly for val = NaN case
            if (val > -two31 - 1 && val < two31)
                return unchecked((int)val);

            return ThrowIntOvf();
        }

        [RuntimeExport("Dbl2UIntOvf")]
        public static uint Dbl2UIntOvf(double val)
        {
            // Note that this expression also works properly for val = NaN case
            if (val > -1.0 && val < 4294967296.0)
                return unchecked((uint)val);

            return ThrowUIntOvf();
        }

        [RuntimeExport("Dbl2LngOvf")]
        public static long Dbl2LngOvf(double val)
        {
            const double two63 = 2147483648.0 * 4294967296.0;

            // Note that this expression also works properly for val = NaN case
            // We need to compare with the very next double to two63. 0x402 is epsilon to get us there.
            if (val > -two63 - 0x402 && val < two63)
                return unchecked((long)val);

            return ThrowLngOvf();
        }

        [RuntimeExport("Dbl2ULngOvf")]
        public static ulong Dbl2ULngOvf(double val)
        {
            const double two64 = 2.0 * 2147483648.0 * 4294967296.0;

            // Note that this expression also works properly for val = NaN case
            if (val > -1.0 && val < two64)
                return unchecked((ulong)val);

            return ThrowULngOvf();
        }

        [RuntimeExport("Flt2IntOvf")]
        public static int Flt2IntOvf(float val)
        {
            const double two31 = 2147483648.0;

            // Note that this expression also works properly for val = NaN case
            if (val > -two31 - 1 && val < two31)
                return ((int)val);

            return ThrowIntOvf();
        }

        [RuntimeExport("Flt2LngOvf")]
        public static long Flt2LngOvf(float val)
        {
            const double two63 = 2147483648.0 * 4294967296.0;

            // Note that this expression also works properly for val = NaN case
            // We need to compare with the very next double to two63. 0x402 is epsilon to get us there.
            if (val > -two63 - 0x402 && val < two63)
                return ((long)val);

            return ThrowIntOvf();
        }

#if TARGET_ARM
        [RuntimeImport(RuntimeLibrary, "RhpIDiv")]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
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
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern uint RhpUDiv(uint i, uint j);

        public static long UDiv(uint i, uint j)
        {
            if (j == 0)
                return ThrowUIntDivByZero();
            else
                return RhpUDiv(i, j);
        }

        [RuntimeImport(RuntimeLibrary, "RhpIMod")]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
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
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
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
        private static int ThrowIntOvf()
        {
            throw new OverflowException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint ThrowUIntOvf()
        {
            throw new OverflowException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static long ThrowLngOvf()
        {
            throw new OverflowException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ulong ThrowULngOvf()
        {
            throw new OverflowException();
        }

#if TARGET_ARM
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
    }
}
