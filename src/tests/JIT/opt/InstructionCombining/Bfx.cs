// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace TestBfx
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact]
        public static int CheckBfx()
        {
            bool fail = false;

            // Int
            if (ExtractBits_Int_NoShift(0x7F654321) != 0x1)
            {
                fail = true;
            }

            if (ExtractBits_Int_Shift(0x7F654321) != 0xC)
            {
                fail = true;
            }

            if (ExtractBits_Int_Shift_Multiple(0x7F654321) != 0x15C)
            {
                fail = true;
            }

            if (ExtractBits_Int_Shift_Non_Continous_Mask(0x7F654321) != 0x43)
            {
                fail = true;
            }

            if (ExtractBits_Int_Shift_Mask0xFF(0x7F654321) != 0xC)
            {
                fail = true;
            }

            if (ExtractBits_Int_Shift_Mask0xFFFF(0x7F654321) != 0x950C)
            {
                fail = true;
            }

            if (ExtractBits_Int_Shift_Mask0xF_MaxWidth(0x7F654321) != 0x7)
            {
                fail = true;
            }

            if (ExtractBits_Int_Shift_Mask0x1F_TooWide(0x7F654321) != 0x7)
            {
                fail = true;
            }

            // UInt
            if (ExtractBits_UInt_NoShift(0xFEDCBA98u) != 0x98u)
            {
                fail = true;
            }

            if (ExtractBits_UInt_Shift(0xFEDCBA98u) != 0x3FBu)
            {
                fail = true;
            }

            if (ExtractBits_UInt_Shift_Multiple(0xFEDCBA98u) != 0x1A18u)
            {
                fail = true;
            }

            if (ExtractBits_UInt_Shift_Non_Continous_Mask(0xFEDCBA98u) != 0x4u)
            {
                fail = true;
            }

            if (ExtractBits_UInt_Shift_Mask0xFF(0xFEDCBA98u) != 0xEAu)
            {
                fail = true;
            }

            if (ExtractBits_UInt_Shift_Mask0xFFFF(0xFEDCBA98u) != 0x72EAu)
            {
                fail = true;
            }

            if (ExtractBits_UInt_Shift_Mask0xF_MaxWidth(0xFEDCBA98u) != 0xFu)
            {
                fail = true;
            }

            if (ExtractBits_UInt_Shift_Mask0x1F_TooWide(0xFEDCBA98u) != 0xFu)
            {
                fail = true;
            }

            // Long
            if (ExtractBits_Long_NoShift(0x7FFFEDCBA9876543L) != 0x6543L)
            {
                fail = true;
            }

            if (ExtractBits_Long_Shift(0x7FFFEDCBA9876543L) != 0x1D95L)
            {
                fail = true;
            }

            if (ExtractBits_Long_Shift_Multiple(0x7FFFEDCBA9876543L) != 0x47F6EL)
            {
                fail = true;
            }

            if (ExtractBits_Long_Shift_Non_Continous_Mask(0x7FFFEDCBA9876543L) != 0x14L)
            {
                fail = true;
            }

            if (ExtractBits_Long_Shift_Mask0xFF(0x7FFFEDCBA9876543L) != 0x95L)
            {
                fail = true;
            }

            if (ExtractBits_Long_Shift_Mask0xFFFF(0x7FFFEDCBA9876543L) != 0x1D95L)
            {
                fail = true;
            }

            if (ExtractBits_Long_Shift_Mask0xFFFFFFFF(0x7FFFEDCBA9876543L) != 0x2EA61D95L)
            {
                fail = true;
            }

            if (ExtractBits_Long_Shift_Mask0xFFFFFFFFFFFFFFFF(0x7FFFEDCBA9876543L) != 0x1FFFFB72EA61D95L)
            {
                fail = true;
            }

            if (ExtractBits_Long_Shift_Mask0xF_MaxWidth(0x7FFFEDCBA9876543L) != 0x7L)
            {
                fail = true;
            }

            if (ExtractBits_Long_Shift_Mask0x1F_TooWide(0x7FFFEDCBA9876543L) != 0x7L)
            {
                fail = true;
            }

            // ULong
            if (ExtractBits_ULong_NoShift(0xFFFEEDCBA9876543UL) != 0x3UL)
            {
                fail = true;
            }

            if (ExtractBits_ULong_Shift(0xFFFEEDCBA9876543UL) != 0x261D95UL)
            {
                fail = true;
            }

            if (ExtractBits_ULong_Shift_Multiple(0xFFFEEDCBA9876543UL) != 0x1107F6EUL)
            {
                fail = true;
            }

            if (ExtractBits_ULong_Shift_Non_Continous_Mask(0xFFFEEDCBA9876543UL) != 0x8UL)
            {
                fail = true;
            }

            if (ExtractBits_ULong_Shift_Mask0xFF(0xFFFEEDCBA9876543UL) != 0x95UL)
            {
                fail = true;
            }

            if (ExtractBits_ULong_Shift_Mask0xFFFF(0xFFFEEDCBA9876543UL) != 0x1D95UL)
            {
                fail = true;
            }

            if (ExtractBits_ULong_Shift_Mask0xFFFFFFFF(0xFFFEEDCBA9876543UL) != 0xCBA98765UL)
            {
                fail = true;
            }

            if (ExtractBits_ULong_Shift_Mask0xF_MaxWidth(0xFFFEEDCBA9876543UL) != 0xFUL)
            {
                fail = true;
            }

            if (ExtractBits_ULong_Shift_Mask0x1F_TooWide(0xFFFEEDCBA9876543UL) != 0xFUL)
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
        static int ExtractBits_Int_NoShift(int x)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #31
            return x & 0x1F;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ExtractBits_Int_Shift(int x)
        {
            //ARM64-FULL-LINE: ubfx {{w[0-9]+}}, {{w[0-9]+}}, #6, #6
            return (x >> 6) & 0x3F;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ExtractBits_Int_Shift_Multiple(int x)
        {
            //ARM64-FULL-LINE: ubfx {{w[0-9]+}}, {{w[0-9]+}}, #6, #7
            //ARM64-FULL-LINE: ubfx {{w[0-9]+}}, {{w[0-9]+}}, #10, #9
            int a = (x >> 6) & 0x7F;
            int b = (x >> 10) & 0x1FF;
            return a + b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ExtractBits_Int_Shift_Non_Continous_Mask(int x)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, #243
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, ASR #8
            return (x >> 8) & 0xF3;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ExtractBits_Int_Shift_Mask0xFF(int x)
        {
            //ARM64-FULL-LINE: asr {{w[0-9]+}}, {{w[0-9]+}}, #6
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            return (x >> 6) & 0xFF;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ExtractBits_Int_Shift_Mask0xFFFF(int x)
        {
            //ARM64-FULL-LINE: lsr {{w[0-9]+}}, {{w[0-9]+}}, #6
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            return (int)(((uint)x >> 6) & 0xFFFF);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ExtractBits_Int_Shift_Mask0xF_MaxWidth(int x)
        {
            //ARM64-FULL-LINE: ubfx {{w[0-9]+}}, {{w[0-9]+}}, #28, #4
            return (x >> 28) & 0xF;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ExtractBits_Int_Shift_Mask0x1F_TooWide(int x)
        {
            //ARM64-FULL-LINE: asr {{w[0-9]+}}, {{w[0-9]+}}, #28
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #31
            return (x >> 28) & 0x1F;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ExtractBits_UInt_NoShift(uint x)
        {
            //ARM64-FULL-LINE and {{w[0-9]+}}, {{w[0-9]+}}, #511
            return x & 0x1FF;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ExtractBits_UInt_Shift(uint x)
        {
            //ARM64-FULL-LINE: ubfx {{w[0-9]+}}, {{w[0-9]+}}, #22, #10
            return (x >> 22) & 0x3FF;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ExtractBits_UInt_Shift_Multiple(uint x)
        {
            //ARM64-FULL-LINE: ubfx {{w[0-9]+}}, {{w[0-9]+}}, #6, #12
            //ARM64-FULL-LINE: ubfx {{w[0-9]+}}, {{w[0-9]+}}, #10, #13
            uint a = (x >> 6) & 0xFFF;
            uint b = (x >> 10) & 0x1FFF;
            return a + b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ExtractBits_UInt_Shift_Non_Continous_Mask(uint x)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, #5
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSR #24
            return (x >> 24) & 0x5;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ExtractBits_UInt_Shift_Mask0xFF(uint x)
        {
            //ARM64-FULL-LINE: lsr {{w[0-9]+}}, {{w[0-9]+}}, #6
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            return (x >> 6) & 0xFF;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ExtractBits_UInt_Shift_Mask0xFFFF(uint x)
        {
            //ARM64-FULL-LINE: lsr {{w[0-9]+}}, {{w[0-9]+}}, #6
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            return (x >> 6) & 0xFFFF;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ExtractBits_UInt_Shift_Mask0xF_MaxWidth(uint x)
        {
            //ARM64-FULL-LINE: ubfx {{w[0-9]+}}, {{w[0-9]+}}, #28, #4
            return (x >> 28) & 0xFu;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ExtractBits_UInt_Shift_Mask0x1F_TooWide(uint x)
        {
            //ARM64-FULL-LINE: lsr {{w[0-9]+}}, {{w[0-9]+}}, #28
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #31
            return (x >> 28) & 0x1Fu;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ExtractBits_Long_NoShift(long x)
        {
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            return x & 0xFFFF;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ExtractBits_Long_Shift(long x)
        {
            //ARM64-FULL-LINE: ubfx {{x[0-9]+}}, {{x[0-9]+}}, #6, #17
            return (x >> 6) & 0x1FFFF;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ExtractBits_Long_Shift_Multiple(long x)
        {
            //ARM64-FULL-LINE: ubfx {{x[0-9]+}}, {{x[0-9]+}}, #6, #18
            //ARM64-FULL-LINE: ubfx {{x[0-9]+}}, {{x[0-9]+}}, #10, #19
            long a = (x >> 6) & 0x3FFFF;
            long b = (x >> 10) & 0x7FFFF;
            return a + b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ExtractBits_Long_Shift_Non_Continous_Mask(long x)
        {
            //ARM64-FULL-LINE: mov {{x[0-9]+}}, #20
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, ASR #12
            return (x >> 12) & 0x14;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ExtractBits_Long_Shift_Mask0xFF(long x)
        {
            //ARM64-FULL-LINE: asr {{x[0-9]+}}, {{x[0-9]+}}, #6
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            return (x >> 6) & 0xFF;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ExtractBits_Long_Shift_Mask0xFFFF(long x)
        {
            //ARM64-FULL-LINE: asr {{x[0-9]+}}, {{x[0-9]+}}, #6
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            return (x >> 6) & 0xFFFF;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ExtractBits_Long_Shift_Mask0xFFFFFFFF(long x)
        {
            //ARM64-FULL-LINE: asr {{x[0-9]+}}, {{x[0-9]+}}, #6
            return (x >> 6) & 0xFFFFFFFFL;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ExtractBits_Long_Shift_Mask0xFFFFFFFFFFFFFFFF(long x)
        {
            //ARM64-FULL-LINE: lsr {{x[0-9]+}}, {{x[0-9]+}}, #6
            return (long)(((ulong)x >> 6) & 0xFFFFFFFFFFFFFFFFUL);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ExtractBits_Long_Shift_Mask0xF_MaxWidth(long x)
        {
            //ARM64-FULL-LINE: ubfx {{x[0-9]+}}, {{x[0-9]+}}, #60, #4
            return (x >> 60) & 0xF;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ExtractBits_Long_Shift_Mask0x1F_TooWide(long x)
        {
            //ARM64-FULL-LINE: asr {{x[0-9]+}}, {{x[0-9]+}}, #60
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #31
            return (x >> 60) & 0x1F;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ExtractBits_ULong_NoShift(ulong x)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            return x & 0xF;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ExtractBits_ULong_Shift(ulong x)
        {
            //ARM64-FULL-LINE: ubfx {{x[0-9]+}}, {{x[0-9]+}}, #6, #22
            return (x >> 6) & 0x3FFFFF;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ExtractBits_ULong_Shift_Multiple(ulong x)
        {
            //ARM64-FULL-LINE: ubfx {{x[0-9]+}}, {{x[0-9]+}}, #6, #23
            //ARM64-FULL-LINE: ubfx {{x[0-9]+}}, {{x[0-9]+}}, #10, #24
            ulong a = (x >> 6) & 0x7FFFFF;
            ulong b = (x >> 10) & 0xFFFFFF;
            return a + b;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ExtractBits_ULong_Shift_Non_Continous_Mask(ulong x)
        {
            //ARM64-FULL-LINE: mov {{x[0-9]+}}, #204
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSR #5
            return (x >> 5) & 0xCC;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ExtractBits_ULong_Shift_Mask0xFF(ulong x)
        {
            //ARM64-FULL-LINE: lsr {{x[0-9]+}}, {{x[0-9]+}}, #6
            //ARM64-FULL-LINE: uxtb {{w[0-9]+}}, {{w[0-9]+}}
            return (x >> 6) & 0xFF;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ExtractBits_ULong_Shift_Mask0xFFFF(ulong x)
        {
            //ARM64-FULL-LINE: lsr {{x[0-9]+}}, {{x[0-9]+}}, #6
            //ARM64-FULL-LINE: uxth {{w[0-9]+}}, {{w[0-9]+}}
            return (x >> 6) & 0xFFFF;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ExtractBits_ULong_Shift_Mask0xFFFFFFFF(ulong x)
        {
            //ARM64-FULL-LINE: lsr {{x[0-9]+}}, {{x[0-9]+}}, #8
            return (x >> 8) & 0xFFFFFFFFUL;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ExtractBits_ULong_Shift_Mask0xF_MaxWidth(ulong x)
        {
            //ARM64-FULL-LINE: ubfx {{x[0-9]+}}, {{x[0-9]+}}, #60, #4
            return (x >> 60) & 0xFu;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ExtractBits_ULong_Shift_Mask0x1F_TooWide(ulong x)
        {
            //ARM64-FULL-LINE: lsr {{x[0-9]+}}, {{x[0-9]+}}, #60
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #31
            return (x >> 60) & 0x1Fu;
        }
    }
}
