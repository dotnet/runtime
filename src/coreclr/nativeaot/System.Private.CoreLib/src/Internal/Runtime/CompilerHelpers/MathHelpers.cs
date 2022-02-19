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
    [CLSCompliant(false)]
    public static class MathHelpers
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
                return ThrowLngArithExc();
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static long ThrowLngArithExc()
        {
            throw new ArithmeticException();
        }
#endif // TARGET_64BIT

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static sbyte PlatformDoubleToInt8(double val)
        {
            return (sbyte)val;
        }

        [RuntimeExport("DoubleToInt8Ovf")]
        public static sbyte DoubleToInt8Ovf(double val)
        {
            if (val > -129.0 && val < 128.0)
            {
                // -129.0 and +128.0 are exactly representable
                // Note that the above condition also works properly for val = NaN case
                return PlatformDoubleToInt8(val);
            }

            return ThrowInt8OverflowException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte PlatformDoubleToUInt8(double val)
        {
            return (byte)val;
        }

        [RuntimeExport("DoubleToUInt8Ovf")]
        public static byte DoubleToUInt8Ovf(double val)
        {
            if (val > -1.0 && val < +256.0)
            {
                // -1.0 and +256.0 are exactly representable
                // Note that the above condition also works properly for val = NaN case
                return PlatformDoubleToUInt8(val);
            }

            return ThrowUInt8OverflowException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static short PlatformDoubleToInt16(double val)
        {
            return (short)val;
        }

        [RuntimeExport("DoubleToInt16Ovf")]
        public static short DoubleToInt16Ovf(double val)
        {
            if (val > -32769.0 && val < +32768.0)
            {
                // -32769.0 and +32768.0 are exactly representable
                // Note that the above condition also works properly for val = NaN case
                return PlatformDoubleToInt16(val);
            }

            return ThrowInt16OverflowException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort PlatformDoubleToUInt16(double val)
        {
            return (ushort)val;
        }

        [RuntimeExport("DoubleToUInt16Ovf")]
        public static ushort DoubleToUInt16Ovf(double val)
        {
            if (val > -1.0 && val < +65536.0)
            {
                // -1.0 and +65536.0 are exactly representable
                // Note that the above condition also works properly for val = NaN case
                return PlatformDoubleToUInt16(val);
            }

            return ThrowUInt16OverflowException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PlatformDoubleToInt32(double val)
        {
            return (int)val;
        }

        [RuntimeExport("DoubleToInt32Ovf")]
        public static int DoubleToInt32Ovf(double val)
        {
            if (val > -2147483649.0 && val < +2147483648.0)
            {
                // -2147483649.0 and +2147483648.0 are exactly representable
                // Note that the above condition also works properly for val = NaN case
                return PlatformDoubleToInt32(val);
            }

            return ThrowInt32OverflowException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint PlatformDoubleToUInt32(double val)
        {
            return (uint)val;
        }

        [RuntimeExport("DoubleToUInt32Ovf")]
        public static uint DoubleToUInt32Ovf(double val)
        {
            if (val > -1.0 && val < +4294967296.0)
            {
                // -1.0 and +4294967296.0 are exactly representable
                // Note that the above condition also works properly for val = NaN case
                return PlatformDoubleToUInt32(val);
            }

            return ThrowUInt32OverflowException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long PlatformDoubleToInt64(double val)
        {
            return (long)val;
        }

        [RuntimeExport("DoubleToInt64Ovf")]
        public static long DoubleToInt64Ovf(double val)
        {
            if (val > -9223372036854777856.0 && val < +9223372036854775808.0)
            {
                // +9223372036854775808.0 is exactly representable
                //
                // -9223372036854777809.0 however, is not and rounds to -9223372036854777808.0
                // we use -9223372036854777856.0 instead which is the next representable value smaller
                // than -9223372036854777808.0
                //
                // Note that this expression also works properly for val = NaN case
                return PlatformDoubleToInt64(val);
            }

            return ThrowInt64OverflowException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong PlatformDoubleToUInt64(double val)
        {
            return (ulong)val;
        }

        [RuntimeExport("DoubleToUInt64Ovf")]
        public static ulong DoubleToUInt64Ovf(double val)
        {
            if (val > -1.0 && val < +18446744073709551616.0)
            {
                // -1.0 and +18446744073709551616.0 are exactly representable
                // Note that the above condition also works properly for val = NaN case
                return PlatformDoubleToUInt64(val);
            }

            return ThrowUInt64OverflowException();
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
                return ThrowIntArithExc();
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
        private static sbyte ThrowInt8OverflowException()
        {
            throw new OverflowException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static byte ThrowUInt8OverflowException()
        {
            throw new OverflowException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static short ThrowInt16OverflowException()
        {
            throw new OverflowException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ushort ThrowUInt16OverflowException()
        {
            throw new OverflowException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ThrowInt32OverflowException()
        {
            throw new OverflowException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint ThrowUInt32OverflowException()
        {
            throw new OverflowException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static long ThrowInt64OverflowException()
        {
            throw new OverflowException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ulong ThrowUInt64OverflowException()
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ThrowIntArithExc()
        {
            throw new ArithmeticException();
        }
#endif // TARGET_ARM
    }
}
