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
        private const double Int32MaxValueOffset = (double)int.MaxValue + 1;
        private const double UInt32MaxValueOffset = (double)uint.MaxValue + 1;

        [RuntimeExport("Dbl2IntOvf")]
        public static int Dbl2IntOvf(double value)
        {
            // Note that this expression also works properly for val = NaN case
            if (value is > -Int32MaxValueOffset - 1 and < Int32MaxValueOffset)
            {
                return (int)value;
            }

            ThrowHelper.ThrowOverflowException();
            return 0;
        }

        [RuntimeExport("Dbl2UIntOvf")]
        public static uint Dbl2UIntOvf(double value)
        {
            // Note that this expression also works properly for val = NaN case
            if (value is > -1.0 and < UInt32MaxValueOffset)
            {
                return (uint)value;
            }

            ThrowHelper.ThrowOverflowException();
            return 0;
        }

        [RuntimeExport("Dbl2LngOvf")]
        public static long Dbl2LngOvf(double value)
        {
            const double two63 = Int32MaxValueOffset * UInt32MaxValueOffset;

            // Note that this expression also works properly for val = NaN case
            // We need to compare with the very next double to two63. 0x402 is epsilon to get us there.
            if (value is > -two63 - 0x402 and < two63)
            {
                return (long)value;
            }

            ThrowHelper.ThrowOverflowException();
            return 0;
        }

        [RuntimeExport("Dbl2ULngOvf")]
        public static ulong Dbl2ULngOvf(double value)
        {
            const double two64 = UInt32MaxValueOffset * UInt32MaxValueOffset;
            // Note that this expression also works properly for val = NaN case
            if (value is > -1.0 and < two64)
            {
                return (ulong)value;
            }

            ThrowHelper.ThrowOverflowException();
            return 0;
        }

#if !TARGET_64BIT
        //
        // 64-bit checked multiplication for 32-bit platforms
        //

        private const string RuntimeLibrary = "*";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint High32Bits(ulong a)
        {
            return (uint)(a >> 32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong BigMul(uint left, uint right)
        {
            return (ulong)left * right;
        }

        [RuntimeExport("LMulOvf")]
        public static long LMulOvf(long left, long right)
        {
#if DEBUG
            long result = left * right;
#endif

            // Remember the sign of the result
            int sign = (int)(High32Bits((ulong)left) ^ High32Bits((ulong)right));

            // Convert to unsigned multiplication
            if (left < 0)
                left = -left;
            if (right < 0)
                right = -right;

            // Get the upper 32 bits of the numbers
            uint val1High = High32Bits((ulong)left);
            uint val2High = High32Bits((ulong)right);

            ulong valMid;

            if (val1High == 0)
            {
                // Compute the 'middle' bits of the long multiplication
                valMid = BigMul(val2High, (uint)left);
            }
            else
            {
                if (val2High != 0)
                    goto Overflow;
                // Compute the 'middle' bits of the long multiplication
                valMid = BigMul(val1High, (uint)right);
            }

            // See if any bits after bit 32 are set
            if (High32Bits(valMid) != 0)
                goto Overflow;

            long ret = (long)(BigMul((uint)left, (uint)right) + (valMid << 32));

            // check for overflow
            if (High32Bits((ulong)ret) < (uint)valMid)
                goto Overflow;

            if (sign >= 0)
            {
                // have we spilled into the sign bit?
                if (ret < 0)
                    goto Overflow;
            }
            else
            {
                ret = -ret;
                // have we spilled into the sign bit?
                if (ret > 0)
                    goto Overflow;
            }

#if DEBUG
            Debug.Assert(ret == result, $"Multiply overflow got: {ret}, expected: {result}");
#endif
            return ret;

        Overflow:
            ThrowHelper.ThrowOverflowException();
            return 0;
        }

        [RuntimeExport("ULMulOvf")]
        public static ulong ULMulOvf(ulong left, ulong right)
        {
            // Get the upper 32 bits of the numbers
            uint val1High = High32Bits(left);
            uint val2High = High32Bits(right);

            ulong valMid;

            if (val1High == 0)
            {
                if (val2High == 0)
                    return (ulong)(uint)left * (uint)right;
                // Compute the 'middle' bits of the long multiplication
                valMid = BigMul(val2High, (uint)left);
            }
            else
            {
                if (val2High != 0)
                    goto Overflow;
                // Compute the 'middle' bits of the long multiplication
                valMid = BigMul(val1High, (uint)right);
            }

            // See if any bits after bit 32 are set
            if (High32Bits(valMid) != 0)
                goto Overflow;

            ulong ret = BigMul((uint)left, (uint)right) + (valMid << 32);

            // check for overflow
            if (High32Bits(ret) < (uint)valMid)
                goto Overflow;

            Debug.Assert(ret == left * right, $"Multiply overflow got: {ret}, expected: {left * right}");
            return ret;

        Overflow:
            ThrowHelper.ThrowOverflowException();
            return 0;
        }

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
