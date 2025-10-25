// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace TestCastRightShift
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact]
        public static int CheckCastRightShift()
        {
            bool fail = false;

            if (CastASR7_byte_int(255) == 0)
            {
                fail = true;
            }

            if (CastASR8_byte_int(255) != 0)
            {
                fail = true;
            }

            if (CastLSR7_byte_uint(255) != 1)
            {
                fail = true;
            }

            if (CastLSR8_byte_uint(255) != 0)
            {
                fail = true;
            }

            if (CastASR7_byte_long(255) == 0)
            {
                fail = true;
            }

            if (CastASR8_byte_long(255) != 0)
            {
                fail = true;
            }

            if (CastLSR7_byte_ulong(255) == 0)
            {
                fail = true;
            }

            if (CastLSR8_byte_ulong(255) != 0)
            {
                fail = true;
            }

            if (CastASR7_sbyte_int(-127) == 0)
            {
                fail = true;
            }

            if (CastASR8_sbyte_int(-127) != 0)
            {
                fail = true;
            }

            if (CastASR7_sbyte_long(-127) == 0)
            {
                fail = true;
            }

            if (CastASR8_sbyte_long(-127) != 0)
            {
                fail = true;
            }

            if (CastASR15_ushort_int(65535) == 0)
            {
                fail = true;
            }

            if (CastASR16_ushort_int(65535) != 0)
            {
                fail = true;
            }

            if (CastLSR15_ushort_uint(65535) == 0)
            {
                fail = true;
            }

            if (CastLSR16_ushort_uint(65535) != 0)
            {
                fail = true;
            }

            if (CastASR15_ushort_long(65535) == 0)
            {
                fail = true;
            }

            if (CastASR16_ushort_long(65535) != 0)
            {
                fail = true;
            }

            if (CastLSR15_ushort_ulong(65535) == 0)
            {
                fail = true;
            }

            if (CastLSR16_ushort_ulong(65535) != 0)
            {
                fail = true;
            }

            if (CastASR15_short_int(-1) == 0)
            {
                fail = true;
            }

            if (CastASR16_short_int(-1) != 0)
            {
                fail = true;
            }

            if (CastASR15_short_long(-1) == 0)
            {
                fail = true;
            }

            if (CastASR16_short_long(-1) != 0)
            {
                fail = true;
            }

            if (fail)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CastASR7_byte_int(byte x)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: asr {{w[0-9]+}}, {{w[0-9]+}}, #7
            return x >> 7;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CastASR8_byte_int(byte x)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, wzr
            return x >> 8;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint CastLSR7_byte_uint(byte x)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: lsr {{w[0-9]+}}, {{w[0-9]+}}, #7
            return (uint)x >> 7;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint CastLSR8_byte_uint(byte x)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, wzr
            return (uint)x >> 8;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long CastASR7_byte_long(byte x)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: asr {{w[0-9]+}}, {{w[0-9]+}}, #7
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            return x >> 7;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long CastASR8_byte_long(byte x)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, wzr
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            return x >> 8;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong CastLSR7_byte_ulong(byte x)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: lsr {{x[0-9]+}}, {{x[0-9]+}}, #7
            return (ulong)x >> 7;
        }

        // This produces the following tree section with 2 CASTs
        // We should check all the CASTs and then check that the lsr value
        // is greater than or equal to smallest src bits. In this case
        // we could change to mov w0, wzr.
        // *  RSZ       long
        // +--*  CAST      long <- ulong
        // |  \--*  CAST      int <- ubyte <- int
        // |     \--*  LCL_VAR   int
        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong CastLSR8_byte_ulong(byte x)
        {
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: lsr {{x[0-9]+}}, {{x[0-9]+}}, #8
            return (ulong)x >> 8;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CastASR7_sbyte_int(sbyte x)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: asr {{w[0-9]+}}, {{w[0-9]+}}, #7
            return x >> 7;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CastASR8_sbyte_int(sbyte x)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, wzr
            return x >> 8;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long CastASR7_sbyte_long(sbyte x)
        {
            //ARM64-FULL-LINE: sxtb {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: asr {{w[0-9]+}}, {{w[0-9]+}}, #7
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            return x >> 7;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long CastASR8_sbyte_long(sbyte x)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, wzr
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            return x >> 8;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CastASR15_ushort_int(ushort x)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: asr {{w[0-9]+}}, {{w[0-9]+}}, #15
            return x >> 15;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CastASR16_ushort_int(ushort x)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, wzr
            return x >> 16;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint CastLSR15_ushort_uint(ushort x)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: lsr {{w[0-9]+}}, {{w[0-9]+}}, #15
            return (uint)x >> 15;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint CastLSR16_ushort_uint(ushort x)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, wzr
            return (uint)x >> 16;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long CastASR15_ushort_long(ushort x)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: asr {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            return x >> 15;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long CastASR16_ushort_long(ushort x)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, wzr
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            return x >> 16;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong CastLSR15_ushort_ulong(ushort x)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: lsr {{x[0-9]+}}, {{x[0-9]+}}, #15
            return (ulong)x >> 15;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong CastLSR16_ushort_ulong(ushort x)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: lsr {{x[0-9]+}}, {{x[0-9]+}}, #16
            return (ulong)x >> 16;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CastASR15_short_int(short x)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: asr {{w[0-9]+}}, {{w[0-9]+}}, #15
            return x >> 15;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CastASR16_short_int(short x)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, wzr
            return x >> 16;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long CastASR15_short_long(short x)
        {
            //ARM64-FULL-LINE: sxth {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: asr {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            return x >> 15;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long CastASR16_short_long(short x)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, wzr
            //ARM64-FULL-LINE: sxtw {{x[0-9]+}}, {{w[0-9]+}}
            return x >> 16;
        }
    }
}
