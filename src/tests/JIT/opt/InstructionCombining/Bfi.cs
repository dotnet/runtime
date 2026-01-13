// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using static TestLibrary.Expect;
using Xunit;

namespace TestBfi
{
    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Fact]
        public static int CheckBfi()
        {
            bool fail = false;

            ExpectEqual(() => ComposeBits_BaseAnd_Mask0(0xB, 0x1), 0xB, ref fail);
            ExpectEqual(() => ComposeBits_BaseAnd_Mask1(0xB, 0x1), 0x1B, ref fail);
            ExpectEqual(() => ComposeBits_BaseAnd_Mask2(0xB, 0x2), 0x2B, ref fail);
            ExpectEqual(() => ComposeBits_BaseAnd_Mask3(0xB, 0x2), 0x2B, ref fail);
            ExpectEqual(() => ComposeBits_BaseAnd_MaskFF(0xB, 0xFA), 0xFAB, ref fail);
            ExpectEqual<uint>(() => ComposeBits_BaseAnd_Shift0(0xE, 0x1), 0x7, ref fail);
            ExpectEqual<uint>(() => ComposeBits_BaseAnd_Shift31(0xE, 0x1), 0x8000000E, ref fail);
            ExpectEqual<uint>(() => ComposeBits_BaseAnd_Shift31_Mask3(0xA, 0x3), 0x8000000A, ref fail);
            ExpectEqual<uint>(() => ComposeBits_BaseAnd_Shift32(0xE, 0x1), 0xF, ref fail);
            ExpectEqual(() => ComposeBits_BaseAnd_Overlap(0x1, 0x3), 0xD, ref fail);
            ExpectEqual(() => ComposeBits_BaseAnd_Variable(0x1000, 0xC), 0xD000, ref fail);
            ExpectEqual(() => ComposeBits_BaseAnd_Variables(0xF, 0xA, 0x2), 0x2A, ref fail);
            ExpectEqual(() => ComposeBits_BaseConstant_Mask3(0x3), 0x3A, ref fail);
            ExpectEqual(() => ComposeBits_BaseConstant_Mask4(0x7), 0x4A, ref fail);
            ExpectEqual(() => ComposeBits_BaseConstant_Overlap(0x2), 0x3F, ref fail);
            ExpectEqual<uint>(() => ComposeBits_BaseBfi(0xB, 0x2, 0x4C), 0x132B, ref fail);
            ExpectEqual<uint>(() => ComposeBits_BaseBfi_SwapOrder(0xB, 0x2, 0x4C), 0x132B, ref fail);
            ExpectEqual<uint>(() => ComposeBits_BaseBfi_BfiOverlap(0xB, 0x2, 0x4C), 0x9AB, ref fail);
            ExpectEqual<uint>(() => ComposeBits_BaseBfi_OverlapBfi(0xB, 0x2, 0x4C), 0x99B, ref fail);
            ExpectEqual<uint>(() => ComposeBits_BaseBfi_BfiOverlapBfi(0xB, 0x2, 0x4C, 0x5), 0x59AB, ref fail);
            ExpectEqual<uint>(() => ComposeBits_BaseBfi_NoBfiAfterInvalidPattern(0xB, 0x2, 0xFC, 0xFF, 0x7), 0xFFAB, ref fail);
            ExpectEqual<uint>(() => ComposeBits_Pack32Values(1,0,0,0,0,0,0,0,1,1,1,1,0,1,1,1,
                                                             1,0,1,1,0,0,1,1,1,1,0,1,0,1,0,1), 0xABCDEF01, ref fail);

            if (fail)
            {
                return 101;
            }
            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseAnd_Mask0(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            return (a & 0xF) | ((b & 0x0) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseAnd_Mask1(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #1
            return (a & 0xf) | ((b & 0x1) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseAnd_Mask2(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #2
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #4
            return (a & 0xf) | ((b & 0x2) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseAnd_Mask3(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #2
            return (a & 0xf) | ((b & 0x3) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseAnd_MaskFF(int a, int b)
        {
            //AR M64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //AR M64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #2
            return (a & 0xf) | ((b & 0xFF) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseAnd_Shift0(uint a, uint b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #7
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #1
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            return (a & 0x7) | (b & 0x1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseAnd_Shift31(uint a, uint b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #31, #1
            return (a & 0xf) | ((b & 0x1) << 31);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseAnd_Shift31_Mask3(uint a, uint b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #3
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #31
            return (a & 0xf) | ((b & 0x3) << 31);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseAnd_Shift32(uint a, uint b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #1
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            return (a & 0xf) | ((b & 0x1) << 32);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseAnd_Overlap(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #7
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #3
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #2
            return (a & 0x7) | ((b & 0x3) << 2);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseAnd_Variable(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #12
            return a | ((b & 0xF) << 12);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseAnd_Variables(int a, int b, int c)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #3
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #4
            return (a & b) | ((c & 0x3) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseConstant_Mask3(int a)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, #10
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #2
            return 0xA | ((a & 0x3) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseConstant_Mask4(int a)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, #10
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #4
            return 0xA | ((a & 0x4) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseConstant_Overlap(int a)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #3
            //ARM64-FULL-LINE: lsl {{w[0-9]+}}, {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, #31
            return 0x1F | ((a & 0x3) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseBfi(uint a, uint b, uint c)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #2
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #6, #7
            return (a & 0xf) | ((b & 0x3) << 4) | ((c & 0x7F) << 6);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseBfi_SwapOrder(uint a, uint b, uint c)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #6, #7
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #2
            return (a & 0xf) | ((c & 0x7F) << 6) | ((b & 0x3) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseBfi_BfiOverlap(uint a, uint b, uint c)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #2
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #127
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #5
            return (a & 0xf) | ((b & 0x3) << 4) | ((c & 0x7F) << 5);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseBfi_OverlapBfi(uint a, uint b, uint c)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #3
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #3
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #5, #7
            return (a & 0xf) | ((b & 0x3) << 3) | ((c & 0x7F) << 5);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseBfi_BfiOverlapBfi(uint a, uint b, uint c, uint d)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #2
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #127
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #5
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #12, #3
            return (a & 0xf) | ((b & 0x3) << 4) | ((c & 0x7F) << 5) | ((d & 0x7) << 12);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseBfi_NoBfiAfterInvalidPattern(uint a, uint b, uint c, uint d, uint e)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #2
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #5
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #7
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #13
            return (a & 0xf) | ((b & 0x3) << 4) | ((c & d) << 5) | ((e & 0x7) << 13);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_Pack32Values(
            uint b0,  uint b1,  uint b2,  uint b3,
            uint b4,  uint b5,  uint b6,  uint b7,
            uint b8,  uint b9,  uint b10, uint b11,
            uint b12, uint b13, uint b14, uint b15,
            uint b16, uint b17, uint b18, uint b19,
            uint b20, uint b21, uint b22, uint b23,
            uint b24, uint b25, uint b26, uint b27,
            uint b28, uint b29, uint b30, uint b31)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #1, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #2, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #3, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #5, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #6, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #7, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #8, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #9, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #10, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #11, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #12, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #13, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #14, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #15, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #16, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #17, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #18, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #19, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #20, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #21, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #22, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #23, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #24, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #25, #1
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #26, #1
            return  ((b0  & 1u) << 0)  |
                    ((b1  & 1u) << 1)  |
                    ((b2  & 1u) << 2)  |
                    ((b3  & 1u) << 3)  |
                    ((b4  & 1u) << 4)  |
                    ((b5  & 1u) << 5)  |
                    ((b6  & 1u) << 6)  |
                    ((b7  & 1u) << 7)  |
                    ((b8  & 1u) << 8)  |
                    ((b9  & 1u) << 9)  |
                    ((b10 & 1u) << 10) |
                    ((b11 & 1u) << 11) |
                    ((b12 & 1u) << 12) |
                    ((b13 & 1u) << 13) |
                    ((b14 & 1u) << 14) |
                    ((b15 & 1u) << 15) |
                    ((b16 & 1u) << 16) |
                    ((b17 & 1u) << 17) |
                    ((b18 & 1u) << 18) |
                    ((b19 & 1u) << 19) |
                    ((b20 & 1u) << 20) |
                    ((b21 & 1u) << 21) |
                    ((b22 & 1u) << 22) |
                    ((b23 & 1u) << 23) |
                    ((b24 & 1u) << 24) |
                    ((b25 & 1u) << 25) |
                    ((b26 & 1u) << 26) |
                    ((b27 & 1u) << 27) |
                    ((b28 & 1u) << 28) |
                    ((b29 & 1u) << 29) |
                    ((b30 & 1u) << 30) |
                    ((b31 & 1u) << 31);
        }
    }
}
