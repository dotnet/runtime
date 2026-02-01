// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
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

            // Int

            if (ComposeBits_BaseAnd_Mask0_Int(0xB, 0x1) != 0xB)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Mask1_Int(0xB, 0x1) != 0x1B)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Mask2_Int(0xB, 0x2) != 0x2B)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Mask3_Int(0xB, 0x2) != 0x2B)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift0_Int(0xE, 0x1) != 0x7)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift31_Int(0xE, 0x1) != unchecked((int)0x8000000E))
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift31_Mask3_Int(0xA, 0x3) != unchecked((int)0x8000000A))
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift32_Int(0xE, 0x1) != 0xF)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift28_MaskF_MaxWidth_Int(0xE, 0xF) != unchecked((int)0xF000000E))
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift28_Mask1F_TooWide_Int(0xE, 0x1F) != unchecked((int)0xF000000E))
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Overlap_Int(0x1, 0x3) != 0xD)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Variable_Int(0x1000, 0xC) != 0xD000)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Variables_Int(0xF, 0xA, 0x2) != 0x2A)
            {
                fail = true;
            }

            if (ComposeBits_BaseConstant_Mask3_Int(0x3) != 0x3A)
            {
                fail = true;
            }

            if (ComposeBits_BaseConstant_Mask4_Int(0x7) != 0x4A)
            {
                fail = true;
            }

            if (ComposeBits_BaseConstant_Overlap_Int(0x2) != 0x3F)
            {
                fail = true;
            }

            if (ComposeBits_BaseBfi_Int(0xB, 0x2, 0x4C) != 0x132B)
            {
                fail = true;
            }

            if (ComposeBits_BaseBfi_SwapOrder_Int(0xB, 0x2, 0x4C) != 0x132B)
            {
                fail = true;
            }

            if (ComposeBits_BaseBfi_BfiOverlap_Int(0xB, 0x2, 0x4C) != 0x9AB)
            {
                fail = true;
            }

            if (ComposeBits_BaseBfi_OverlapBfi_Int(0xB, 0x2, 0x4C) != 0x99B)
            {
                fail = true;
            }

            if (ComposeBits_BaseBfi_BfiOverlapBfi_Int(0xB, 0x2, 0x4C, 0x5) != 0x59AB)
            {
                fail = true;
            }

            if (ComposeBits_Pack32Values_Int(1, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 0, 1, 1, 1,
                                         1, 0, 1, 1, 0, 0, 1, 1, 1, 1, 0, 1, 0, 1, 0, 1) != -0x543210ff)
            {
                fail = true;
            }


            // UInt

            if (ComposeBits_BaseAnd_Mask0_UInt(0xBu, 0x1u) != 0xBu)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Mask1_UInt(0xBu, 0x1u) != 0x1Bu)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Mask2_UInt(0xBu, 0x2u) != 0x2Bu)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Mask3_UInt(0xBu, 0x2u) != 0x2Bu)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift0_UInt(0xEu, 0x1u) != 0x7u)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift31_UInt(0xEu, 0x1u) != 0x8000000Eu)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift31_Mask3_UInt(0xAu, 0x3u) != 0x8000000Au)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift32_UInt(0xEu, 0x1u) != 0xFu)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift28_MaskF_MaxWidth_UInt(0xEu, 0xFu) != 0xF000000Eu)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift28_Mask1F_TooWide_UInt(0xEu, 0x1Fu) != 0xF000000Eu)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Overlap_UInt(0x1u, 0x3u) != 0xDu)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Variable_UInt(0x1000u, 0xCu) != 0xD000u)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Variables_UInt(0xFu, 0xAu, 0x2u) != 0x2Au)
            {
                fail = true;
            }

            if (ComposeBits_BaseConstant_Mask3_UInt(0x3u) != 0x3Au)
            {
                fail = true;
            }

            if (ComposeBits_BaseConstant_Mask4_UInt(0x7u) != 0x4Au)
            {
                fail = true;
            }

            if (ComposeBits_BaseConstant_Overlap_UInt(0x2u) != 0x3Fu)
            {
                fail = true;
            }

            if (ComposeBits_BaseBfi_UInt(0xBu, 0x2u, 0x4Cu) != 0x132Bu)
            {
                fail = true;
            }

            if (ComposeBits_BaseBfi_SwapOrder_UInt(0xBu, 0x2u, 0x4Cu) != 0x132Bu)
            {
                fail = true;
            }

            if (ComposeBits_BaseBfi_BfiOverlap_UInt(0xBu, 0x2u, 0x4Cu) != 0x9ABu)
            {
                fail = true;
            }

            if (ComposeBits_BaseBfi_OverlapBfi_UInt(0xBu, 0x2u, 0x4Cu) != 0x99Bu)
            {
                fail = true;
            }

            if (ComposeBits_BaseBfi_BfiOverlapBfi_UInt(0xBu, 0x2u, 0x4Cu, 0x5u) != 0x59ABu)
            {
                fail = true;
            }

            if (ComposeBits_Pack32Values_UInt(1, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 0, 1, 1, 1,
                                         1, 0, 1, 1, 0, 0, 1, 1, 1, 1, 0, 1, 0, 1, 0, 1) != 0xABCDEF01u)
            {
                fail = true;
            }

            // Long

            if (ComposeBits_BaseAnd_Mask0_Long(0xBL, 0x1L) != 0xBL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Mask1_Long(0xBL, 0x1L) != 0x1BL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Mask2_Long(0xBL, 0x2L) != 0x2BL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Mask3_Long(0xBL, 0x2L) != 0x2BL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift0_Long(0xEL, 0x1L) != 0x7L)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift31_Long(0xEL, 0x1L) != 0x8000000EL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift31_Mask3_Long(0xAL, 0x3L) != 0x18000000AL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift32_Long(0xEL, 0x1L) != 0x10000000EL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift28_MaskF_MaxWidth_Long(0xEL, 0xFL) != 0xF000000EL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift28_Mask1F_TooWide_Long(0xEL, 0x1FL) != 0x1F000000EL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift60_MaskF_MaxWidth_Long(0xEL, 0xFL) != unchecked((long)0xF00000000000000EUL))
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift60_Mask1F_TooWide_Long(0xEL, 0x1FL) != unchecked((long)0xF00000000000000EUL))
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Overlap_Long(0x1L, 0x3L) != 0xDL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Variable_Long(0x1000L, 0xCL) != 0xD000L)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Variables_Long(0xFL, 0xAL, 0x2L) != 0x2AL)
            {
                fail = true;
            }

            if (ComposeBits_BaseConstant_Mask3_Long(0x3L) != 0x3AL)
            {
                fail = true;
            }

            if (ComposeBits_BaseConstant_Mask4_Long(0x7L) != 0x4AL)
            {
                fail = true;
            }

            if (ComposeBits_BaseConstant_Overlap_Long(0x2L) != 0x3FL)
            {
                fail = true;
            }

            if (ComposeBits_BaseBfi_Long(0xBL, 0x2L, 0x4CL) != 0x132BL)
            {
                fail = true;
            }

            if (ComposeBits_BaseBfi_SwapOrder_Long(0xBL, 0x2L, 0x4CL) != 0x132BL)
            {
                fail = true;
            }

            if (ComposeBits_BaseBfi_BfiOverlap_Long(0xBL, 0x2L, 0x4CL) != 0x9ABL)
            {
                fail = true;
            }

            if (ComposeBits_BaseBfi_OverlapBfi_Long(0xBL, 0x2L, 0x4CL) != 0x99BL)
            {
                fail = true;
            }

            if (ComposeBits_BaseBfi_BfiOverlapBfi_Long(0xBL, 0x2L, 0x4CL, 0x5L) != 0x59ABL)
            {
                fail = true;
            }

            if (ComposeBits_Pack32Values_Long(1, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 0, 1, 1, 1,
                                         1, 0, 1, 1, 0, 0, 1, 1, 1, 1, 0, 1, 0, 1, 0, 1) != 0xABCDEF01L)
            {
                fail = true;
            }

            // ULong

            if (ComposeBits_BaseAnd_Mask0_ULong(0xBUL, 0x1UL) != 0xBUL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Mask1_ULong(0xBUL, 0x1UL) != 0x1BUL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Mask2_ULong(0xBUL, 0x2UL) != 0x2BUL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Mask3_ULong(0xBUL, 0x2UL) != 0x2BUL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift0_ULong(0xEUL, 0x1UL) != 0x7UL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift31_ULong(0xEUL, 0x1UL) != 0x8000000EUL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift31_Mask3_ULong(0xAUL, 0x3UL) != 0x18000000AUL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift32_ULong(0xEUL, 0x1UL) != 0x10000000EUL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift28_MaskF_MaxWidth_ULong(0xEUL, 0xFUL) != 0xF000000EUL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift28_Mask1F_TooWide_ULong(0xEUL, 0x1FUL) != 0x1F000000EUL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift60_MaskF_MaxWidth_ULong(0xEUL, 0xFUL) != 0xF00000000000000EUL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Shift60_Mask1F_TooWide_ULong(0xEUL, 0x1FUL) != 0xF00000000000000EUL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Overlap_ULong(0x1UL, 0x3UL) != 0xDUL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Variable_ULong(0x1000UL, 0xCUL) != 0xD000UL)
            {
                fail = true;
            }

            if (ComposeBits_BaseAnd_Variables_ULong(0xFUL, 0xAUL, 0x2UL) != 0x2AUL)
            {
                fail = true;
            }

            if (ComposeBits_BaseConstant_Mask3_ULong(0x3UL) != 0x3AUL)
            {
                fail = true;
            }

            if (ComposeBits_BaseConstant_Mask4_ULong(0x7UL) != 0x4AUL)
            {
                fail = true;
            }

            if (ComposeBits_BaseConstant_Overlap_ULong(0x2UL) != 0x3FUL)
            {
                fail = true;
            }

            if (ComposeBits_BaseBfi_ULong(0xBUL, 0x2UL, 0x4CUL) != 0x132BUL)
            {
                fail = true;
            }

            if (ComposeBits_BaseBfi_SwapOrder_ULong(0xBUL, 0x2UL, 0x4CUL) != 0x132BUL)
            {
                fail = true;
            }

            if (ComposeBits_BaseBfi_BfiOverlap_ULong(0xBUL, 0x2UL, 0x4CUL) != 0x9ABUL)
            {
                fail = true;
            }

            if (ComposeBits_BaseBfi_OverlapBfi_ULong(0xBUL, 0x2UL, 0x4CUL) != 0x99BUL)
            {
                fail = true;
            }

            if (ComposeBits_BaseBfi_BfiOverlapBfi_ULong(0xBUL, 0x2UL, 0x4CUL, 0x5UL) != 0x59ABUL)
            {
                fail = true;
            }

            if (ComposeBits_Pack32Values_ULong(1, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 0, 1, 1, 1,
                                          1, 0, 1, 1, 0, 0, 1, 1, 1, 1, 0, 1, 0, 1, 0, 1) != 0xABCDEF01UL)
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
        static int ComposeBits_BaseAnd_Mask0_Int(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            return (a & 0xF) | ((b & 0x0) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseAnd_Mask1_Int(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #1
            return (a & 0xf) | ((b & 0x1) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseAnd_Mask2_Int(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #2
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #4
            return (a & 0xf) | ((b & 0x2) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseAnd_Mask3_Int(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #2
            return (a & 0xf) | ((b & 0x3) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseAnd_Shift0_Int(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #7
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #1
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            return (a & 0x7) | (b & 0x1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseAnd_Shift31_Int(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #31, #1
            return (a & 0xf) | ((b & 0x1) << 31);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseAnd_Shift31_Mask3_Int(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #3
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #31
            return (a & 0xf) | ((b & 0x3) << 31);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseAnd_Shift32_Int(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #1
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            return (a & 0xf) | ((b & 0x1) << 32);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseAnd_Shift28_MaskF_MaxWidth_Int(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #28, #4
            return (a & 0xf) | ((b & 0xF) << 28);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseAnd_Shift28_Mask1F_TooWide_Int(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #31
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #28
            return (a & 0xf) | ((b & 0x1F) << 28);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseAnd_Overlap_Int(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #7
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #3
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #2
            return (a & 0x7) | ((b & 0x3) << 2);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseAnd_Variable_Int(int a, int b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #12
            return a | ((b & 0xF) << 12);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseAnd_Variables_Int(int a, int b, int c)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #3
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #4
            return (a & b) | ((c & 0x3) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseConstant_Mask3_Int(int a)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, #10
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #2
            return 0xA | ((a & 0x3) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseConstant_Mask4_Int(int a)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, #10
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #4
            return 0xA | ((a & 0x4) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseConstant_Overlap_Int(int a)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #3
            //ARM64-FULL-LINE: lsl {{w[0-9]+}}, {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, #31
            return 0x1F | ((a & 0x3) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseBfi_Int(int a, int b, int c)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #2
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #6, #7
            return (a & 0xf) | ((b & 0x3) << 4) | ((c & 0x7F) << 6);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseBfi_SwapOrder_Int(int a, int b, int c)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #6, #7
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #2
            return (a & 0xf) | ((c & 0x7F) << 6) | ((b & 0x3) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseBfi_BfiOverlap_Int(int a, int b, int c)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #2
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #127
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #5
            return (a & 0xf) | ((b & 0x3) << 4) | ((c & 0x7F) << 5);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseBfi_OverlapBfi_Int(int a, int b, int c)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #3
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #3
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #5, #7
            return (a & 0xf) | ((b & 0x3) << 3) | ((c & 0x7F) << 5);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_BaseBfi_BfiOverlapBfi_Int(int a, int b, int c, int d)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #2
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #127
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #5
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #12, #3
            return (a & 0xf) | ((b & 0x3) << 4) | ((c & 0x7F) << 5) | ((d & 0x7) << 12);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ComposeBits_Pack32Values_Int(
            int b0,  int b1,  int b2,  int b3,
            int b4,  int b5,  int b6,  int b7,
            int b8,  int b9,  int b10, int b11,
            int b12, int b13, int b14, int b15,
            int b16, int b17, int b18, int b19,
            int b20, int b21, int b22, int b23,
            int b24, int b25, int b26, int b27,
            int b28, int b29, int b30, int b31)
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
            return  ((b0  & 1) << 0)  |
                    ((b1  & 1) << 1)  |
                    ((b2  & 1) << 2)  |
                    ((b3  & 1) << 3)  |
                    ((b4  & 1) << 4)  |
                    ((b5  & 1) << 5)  |
                    ((b6  & 1) << 6)  |
                    ((b7  & 1) << 7)  |
                    ((b8  & 1) << 8)  |
                    ((b9  & 1) << 9)  |
                    ((b10 & 1) << 10) |
                    ((b11 & 1) << 11) |
                    ((b12 & 1) << 12) |
                    ((b13 & 1) << 13) |
                    ((b14 & 1) << 14) |
                    ((b15 & 1) << 15) |
                    ((b16 & 1) << 16) |
                    ((b17 & 1) << 17) |
                    ((b18 & 1) << 18) |
                    ((b19 & 1) << 19) |
                    ((b20 & 1) << 20) |
                    ((b21 & 1) << 21) |
                    ((b22 & 1) << 22) |
                    ((b23 & 1) << 23) |
                    ((b24 & 1) << 24) |
                    ((b25 & 1) << 25) |
                    ((b26 & 1) << 26) |
                    ((b27 & 1) << 27) |
                    ((b28 & 1) << 28) |
                    ((b29 & 1) << 29) |
                    ((b30 & 1) << 30) |
                    ((b31 & 1) << 31);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseAnd_Mask0_UInt(uint a, uint b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            return (a & 0xF) | ((b & 0x0) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseAnd_Mask1_UInt(uint a, uint b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #1
            return (a & 0xf) | ((b & 0x1) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseAnd_Mask2_UInt(uint a, uint b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #2
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #4
            return (a & 0xf) | ((b & 0x2) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseAnd_Mask3_UInt(uint a, uint b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #2
            return (a & 0xf) | ((b & 0x3) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseAnd_Shift0_UInt(uint a, uint b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #7
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #1
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            return (a & 0x7) | (b & 0x1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseAnd_Shift31_UInt(uint a, uint b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #31, #1
            return (a & 0xf) | ((b & 0x1) << 31);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseAnd_Shift31_Mask3_UInt(uint a, uint b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #3
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #31
            return (a & 0xf) | ((b & 0x3) << 31);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseAnd_Shift32_UInt(uint a, uint b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #1
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            return (a & 0xf) | ((b & 0x1) << 32);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseAnd_Shift28_MaskF_MaxWidth_UInt(uint a, uint b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #28, #4
            return (a & 0xf) | ((b & 0xF) << 28);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseAnd_Shift28_Mask1F_TooWide_UInt(uint a, uint b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #31
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #28
            return (a & 0xf) | ((b & 0x1F) << 28);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseAnd_Overlap_UInt(uint a, uint b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #7
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #3
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #2
            return (a & 0x7) | ((b & 0x3) << 2);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseAnd_Variable_UInt(uint a, uint b)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #12
            return a | ((b & 0xF) << 12);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseAnd_Variables_UInt(uint a, uint b, uint c)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #3
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #4
            return (a & b) | ((c & 0x3) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseConstant_Mask3_UInt(uint a)
        {
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, #10
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #2
            return 0xAu | ((a & 0x3) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseConstant_Mask4_UInt(uint a)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: mov {{w[0-9]+}}, #10
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #4
            return 0xAu | ((a & 0x4) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseConstant_Overlap_UInt(uint a)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #3
            //ARM64-FULL-LINE: lsl {{w[0-9]+}}, {{w[0-9]+}}, #4
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, #31
            return 0x1Fu | ((a & 0x3) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseBfi_UInt(uint a, uint b, uint c)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #2
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #6, #7
            return (a & 0xf) | ((b & 0x3) << 4) | ((c & 0x7F) << 6);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseBfi_SwapOrder_UInt(uint a, uint b, uint c)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #6, #7
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #2
            return (a & 0xf) | ((c & 0x7F) << 6) | ((b & 0x3) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseBfi_BfiOverlap_UInt(uint a, uint b, uint c)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #2
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #127
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #5
            return (a & 0xf) | ((b & 0x3) << 4) | ((c & 0x7F) << 5);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseBfi_OverlapBfi_UInt(uint a, uint b, uint c)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #3
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #3
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #5, #7
            return (a & 0xf) | ((b & 0x3) << 3) | ((c & 0x7F) << 5);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_BaseBfi_BfiOverlapBfi_UInt(uint a, uint b, uint c, uint d)
        {
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #4, #2
            //ARM64-FULL-LINE: and {{w[0-9]+}}, {{w[0-9]+}}, #127
            //ARM64-FULL-LINE: orr {{w[0-9]+}}, {{w[0-9]+}}, {{w[0-9]+}}, LSL #5
            //ARM64-FULL-LINE: bfi {{w[0-9]+}}, {{w[0-9]+}}, #12, #3
            return (a & 0xf) | ((b & 0x3) << 4) | ((c & 0x7F) << 5) | ((d & 0x7) << 12);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static uint ComposeBits_Pack32Values_UInt(
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



        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_BaseAnd_Mask0_Long(long a, long b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            return (a & 0xFL) | ((b & 0x0L) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_BaseAnd_Mask1_Long(long a, long b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #4, #1
            return (a & 0xfL) | ((b & 0x1L) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_BaseAnd_Mask2_Long(long a, long b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #2
            //ARM64-FULL-LINE: orr {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSL #4
            return (a & 0xfL) | ((b & 0x2L) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_BaseAnd_Mask3_Long(long a, long b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #4, #2
            return (a & 0xfL) | ((b & 0x3L) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_BaseAnd_Shift0_Long(long a, long b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #7
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #1
            //ARM64-FULL-LINE: orr {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}
            return (a & 0x7L) | (b & 0x1L);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_BaseAnd_Shift31_Long(long a, long b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #31, #1
            return (a & 0xfL) | ((b & 0x1L) << 31);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_BaseAnd_Shift31_Mask3_Long(long a, long b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #31, #2
            return (a & 0xfL) | ((b & 0x3L) << 31);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_BaseAnd_Shift32_Long(long a, long b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #32, #1
            return (a & 0xfL) | ((b & 0x1L) << 32);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_BaseAnd_Shift28_MaskF_MaxWidth_Long(long a, long b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #28, #4
            return (a & 0xfL) | ((b & 0xFL) << 28);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_BaseAnd_Shift28_Mask1F_TooWide_Long(long a, long b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #28, #5
            return (a & 0xfL) | ((b & 0x1FL) << 28);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_BaseAnd_Shift60_MaskF_MaxWidth_Long(long a, long b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #60, #4
            return (a & 0xfL) | ((b & 0xFL) << 60);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_BaseAnd_Shift60_Mask1F_TooWide_Long(long a, long b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #31
            //ARM64-FULL-LINE: orr {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSL #60
            return (a & 0xfL) | ((b & 0x1FL) << 60);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_BaseAnd_Overlap_Long(long a, long b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #7
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #3
            //ARM64-FULL-LINE: orr {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSL #2
            return (a & 0x7L) | ((b & 0x3L) << 2);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_BaseAnd_Variable_Long(long a, long b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: orr {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSL #12
            return a | ((b & 0xFL) << 12);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_BaseAnd_Variables_Long(long a, long b, long c)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #3
            //ARM64-FULL-LINE: orr {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSL #4
            return (a & b) | ((c & 0x3L) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_BaseConstant_Mask3_Long(long a)
        {
            //ARM64-FULL-LINE: mov {{x[0-9]+}}, #10
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #4, #2
            return 0xAL | ((a & 0x3L) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_BaseConstant_Mask4_Long(long a)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #4
            //ARM64-FULL-LINE: mov {{x[0-9]+}}, #10
            //ARM64-FULL-LINE: orr {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSL #4
            return 0xAL | ((a & 0x4L) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_BaseConstant_Overlap_Long(long a)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #3
            //ARM64-FULL-LINE: lsl {{x[0-9]+}}, {{x[0-9]+}}, #4
            //ARM64-FULL-LINE: orr {{x[0-9]+}}, {{x[0-9]+}}, #31
            return 0x1FL | ((a & 0x3L) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_BaseBfi_Long(long a, long b, long c)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #4, #2
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #6, #7
            return (a & 0xfL) | ((b & 0x3L) << 4) | ((c & 0x7FL) << 6);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_BaseBfi_SwapOrder_Long(long a, long b, long c)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #6, #7
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #4, #2
            return (a & 0xfL) | ((c & 0x7FL) << 6) | ((b & 0x3L) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_BaseBfi_BfiOverlap_Long(long a, long b, long c)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #4, #2
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #127
            //ARM64-FULL-LINE: orr {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSL #5
            return (a & 0xfL) | ((b & 0x3L) << 4) | ((c & 0x7FL) << 5);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_BaseBfi_OverlapBfi_Long(long a, long b, long c)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #3
            //ARM64-FULL-LINE: orr {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSL #3
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #5, #7
            return (a & 0xfL) | ((b & 0x3L) << 3) | ((c & 0x7FL) << 5);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_BaseBfi_BfiOverlapBfi_Long(long a, long b, long c, long d)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #4, #2
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #127
            //ARM64-FULL-LINE: orr {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSL #5
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #12, #3
            return (a & 0xfL) | ((b & 0x3L) << 4) | ((c & 0x7FL) << 5) | ((d & 0x7L) << 12);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long ComposeBits_Pack32Values_Long(
            long b0,  long b1,  long b2,  long b3,
            long b4,  long b5,  long b6,  long b7,
            long b8,  long b9,  long b10, long b11,
            long b12, long b13, long b14, long b15,
            long b16, long b17, long b18, long b19,
            long b20, long b21, long b22, long b23,
            long b24, long b25, long b26, long b27,
            long b28, long b29, long b30, long b31)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #1, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #2, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #3, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #4, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #5, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #6, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #7, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #8, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #9, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #10, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #11, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #12, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #13, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #14, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #15, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #16, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #17, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #18, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #19, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #20, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #21, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #22, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #23, #1
            return  ((b0  & 1L) << 0)  |
                    ((b1  & 1L) << 1)  |
                    ((b2  & 1L) << 2)  |
                    ((b3  & 1L) << 3)  |
                    ((b4  & 1L) << 4)  |
                    ((b5  & 1L) << 5)  |
                    ((b6  & 1L) << 6)  |
                    ((b7  & 1L) << 7)  |
                    ((b8  & 1L) << 8)  |
                    ((b9  & 1L) << 9)  |
                    ((b10 & 1L) << 10) |
                    ((b11 & 1L) << 11) |
                    ((b12 & 1L) << 12) |
                    ((b13 & 1L) << 13) |
                    ((b14 & 1L) << 14) |
                    ((b15 & 1L) << 15) |
                    ((b16 & 1L) << 16) |
                    ((b17 & 1L) << 17) |
                    ((b18 & 1L) << 18) |
                    ((b19 & 1L) << 19) |
                    ((b20 & 1L) << 20) |
                    ((b21 & 1L) << 21) |
                    ((b22 & 1L) << 22) |
                    ((b23 & 1L) << 23) |
                    ((b24 & 1L) << 24) |
                    ((b25 & 1L) << 25) |
                    ((b26 & 1L) << 26) |
                    ((b27 & 1L) << 27) |
                    ((b28 & 1L) << 28) |
                    ((b29 & 1L) << 29) |
                    ((b30 & 1L) << 30) |
                    ((b31 & 1L) << 31);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_BaseAnd_Mask0_ULong(ulong a, ulong b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            return (a & 0xFUL) | ((b & 0x0UL) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_BaseAnd_Mask1_ULong(ulong a, ulong b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #4, #1
            return (a & 0xfUL) | ((b & 0x1UL) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_BaseAnd_Mask2_ULong(ulong a, ulong b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #2
            //ARM64-FULL-LINE: orr {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSL #4
            return (a & 0xfUL) | ((b & 0x2UL) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_BaseAnd_Mask3_ULong(ulong a, ulong b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #4, #2
            return (a & 0xfUL) | ((b & 0x3UL) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_BaseAnd_Shift0_ULong(ulong a, ulong b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #7
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #1
            //ARM64-FULL-LINE: orr {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}
            return (a & 0x7UL) | (b & 0x1UL);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_BaseAnd_Shift31_ULong(ulong a, ulong b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #31, #1
            return (a & 0xfUL) | ((b & 0x1UL) << 31);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_BaseAnd_Shift31_Mask3_ULong(ulong a, ulong b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #31, #2
            return (a & 0xfUL) | ((b & 0x3UL) << 31);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_BaseAnd_Shift32_ULong(ulong a, ulong b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #32, #1
            return (a & 0xfUL) | ((b & 0x1UL) << 32);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_BaseAnd_Shift28_MaskF_MaxWidth_ULong(ulong a, ulong b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #28, #4
            return (a & 0xfUL) | ((b & 0xFUL) << 28);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_BaseAnd_Shift28_Mask1F_TooWide_ULong(ulong a, ulong b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #28, #5
            return (a & 0xfUL) | ((b & 0x1FUL) << 28);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_BaseAnd_Shift60_MaskF_MaxWidth_ULong(ulong a, ulong b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #60, #4
            return (a & 0xfUL) | ((b & 0xFUL) << 60);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_BaseAnd_Shift60_Mask1F_TooWide_ULong(ulong a, ulong b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #31
            //ARM64-FULL-LINE: orr {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSL #60
            return (a & 0xfUL) | ((b & 0x1FUL) << 60);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_BaseAnd_Overlap_ULong(ulong a, ulong b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #7
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #3
            //ARM64-FULL-LINE: orr {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSL #2
            return (a & 0x7UL) | ((b & 0x3UL) << 2);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_BaseAnd_Variable_ULong(ulong a, ulong b)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: orr {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSL #12
            return a | ((b & 0xFUL) << 12);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_BaseAnd_Variables_ULong(ulong a, ulong b, ulong c)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #3
            //ARM64-FULL-LINE: orr {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSL #4
            return (a & b) | ((c & 0x3UL) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_BaseConstant_Mask3_ULong(ulong a)
        {
            //ARM64-FULL-LINE: mov {{x[0-9]+}}, #10
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #4, #2
            return 0xAUL | ((a & 0x3UL) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_BaseConstant_Mask4_ULong(ulong a)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #4
            //ARM64-FULL-LINE: mov {{x[0-9]+}}, #10
            //ARM64-FULL-LINE: orr {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSL #4
            return 0xAUL | ((a & 0x4UL) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_BaseConstant_Overlap_ULong(ulong a)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #3
            //ARM64-FULL-LINE: lsl {{x[0-9]+}}, {{x[0-9]+}}, #4
            //ARM64-FULL-LINE: orr {{x[0-9]+}}, {{x[0-9]+}}, #31
            return 0x1FUL | ((a & 0x3UL) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_BaseBfi_ULong(ulong a, ulong b, ulong c)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #4, #2
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #6, #7
            return (a & 0xfUL) | ((b & 0x3UL) << 4) | ((c & 0x7FUL) << 6);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_BaseBfi_SwapOrder_ULong(ulong a, ulong b, ulong c)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #6, #7
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #4, #2
            return (a & 0xfUL) | ((c & 0x7FUL) << 6) | ((b & 0x3UL) << 4);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_BaseBfi_BfiOverlap_ULong(ulong a, ulong b, ulong c)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #4, #2
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #127
            //ARM64-FULL-LINE: orr {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSL #5
            return (a & 0xfUL) | ((b & 0x3UL) << 4) | ((c & 0x7FUL) << 5);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_BaseBfi_OverlapBfi_ULong(ulong a, ulong b, ulong c)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #3
            //ARM64-FULL-LINE: orr {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSL #3
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #5, #7
            return (a & 0xfUL) | ((b & 0x3UL) << 3) | ((c & 0x7FUL) << 5);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_BaseBfi_BfiOverlapBfi_ULong(ulong a, ulong b, ulong c, ulong d)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #15
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #4, #2
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #127
            //ARM64-FULL-LINE: orr {{x[0-9]+}}, {{x[0-9]+}}, {{x[0-9]+}}, LSL #5
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #12, #3
            return (a & 0xfUL) | ((b & 0x3UL) << 4) | ((c & 0x7FUL) << 5) | ((d & 0x7UL) << 12);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong ComposeBits_Pack32Values_ULong(
            ulong b0,  ulong b1,  ulong b2,  ulong b3,
            ulong b4,  ulong b5,  ulong b6,  ulong b7,
            ulong b8,  ulong b9,  ulong b10, ulong b11,
            ulong b12, ulong b13, ulong b14, ulong b15,
            ulong b16, ulong b17, ulong b18, ulong b19,
            ulong b20, ulong b21, ulong b22, ulong b23,
            ulong b24, ulong b25, ulong b26, ulong b27,
            ulong b28, ulong b29, ulong b30, ulong b31)
        {
            //ARM64-FULL-LINE: and {{x[0-9]+}}, {{x[0-9]+}}, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #1, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #2, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #3, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #4, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #5, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #6, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #7, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #8, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #9, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #10, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #11, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #12, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #13, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #14, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #15, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #16, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #17, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #18, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #19, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #20, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #21, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #22, #1
            //ARM64-FULL-LINE: bfi {{x[0-9]+}}, {{x[0-9]+}}, #23, #1
            return  ((b0  & 1UL) << 0)  |
                    ((b1  & 1UL) << 1)  |
                    ((b2  & 1UL) << 2)  |
                    ((b3  & 1UL) << 3)  |
                    ((b4  & 1UL) << 4)  |
                    ((b5  & 1UL) << 5)  |
                    ((b6  & 1UL) << 6)  |
                    ((b7  & 1UL) << 7)  |
                    ((b8  & 1UL) << 8)  |
                    ((b9  & 1UL) << 9)  |
                    ((b10 & 1UL) << 10) |
                    ((b11 & 1UL) << 11) |
                    ((b12 & 1UL) << 12) |
                    ((b13 & 1UL) << 13) |
                    ((b14 & 1UL) << 14) |
                    ((b15 & 1UL) << 15) |
                    ((b16 & 1UL) << 16) |
                    ((b17 & 1UL) << 17) |
                    ((b18 & 1UL) << 18) |
                    ((b19 & 1UL) << 19) |
                    ((b20 & 1UL) << 20) |
                    ((b21 & 1UL) << 21) |
                    ((b22 & 1UL) << 22) |
                    ((b23 & 1UL) << 23) |
                    ((b24 & 1UL) << 24) |
                    ((b25 & 1UL) << 25) |
                    ((b26 & 1UL) << 26) |
                    ((b27 & 1UL) << 27) |
                    ((b28 & 1UL) << 28) |
                    ((b29 & 1UL) << 29) |
                    ((b30 & 1UL) << 30) |
                    ((b31 & 1UL) << 31);
        }
    }
}
