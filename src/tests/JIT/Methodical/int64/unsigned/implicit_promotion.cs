// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
using JitTest_implicit_promotion;
using implicit_promotion;

namespace JitTest_implicit_promotion
{
    public class Test
    {
        [Theory]
        [InlineData("add.ovf.un", (int)0x1, -2, -1, (long)0x00000000FFFFFFFF)]
        [InlineData("add.ovf", (int)0x1, -2, -1, -1)]
        public static void testI_i32_opcode(string opcode, int a, int b, int resultI_i32_32Bit_expected, long resultI_i32_64Bit_expected)
        {
            nint a_nint = (nint)a;
            nint result = 0;
            switch(opcode)
            {
                case "add.ovf.un":
                    result = Operator.add_ovf_un_I_i32(a_nint, b);
                    break;
                case "add.ovf":
                    result = Operator.add_ovf_I_i32(a_nint, b);
                    break;
                default:
                    throw new ArgumentException("Invalid opcode");
            }

            Assert.Equal(Environment.Is64BitProcess ? (nint)resultI_i32_64Bit_expected : (nint)resultI_i32_32Bit_expected, result);
        }

        [Theory]
        [InlineData("add.ovf.un", -2, 1, -1, (long)0x00000000FFFFFFFF)]
        [InlineData("add.ovf", -2, 1, -1, -1)]
        public static void testi32_I_opcode(string opcode, int a, int b, int resultI_i32_32Bit_expected, long resultI_i32_64Bit_expected)
        {
            nint b_nint = (nint)b;
            nint result = 0;
            switch(opcode)
            {
                case "add.ovf.un":
                    result = Operator.add_ovf_un_i32_I(a, b_nint);
                    break;
                case "add.ovf":
                    result = Operator.add_ovf_i32_I(a, b_nint);
                    break;
                default:
                    throw new ArgumentException("Invalid opcode");
            }

            Assert.Equal(Environment.Is64BitProcess ? (nint)resultI_i32_64Bit_expected : (nint)resultI_i32_32Bit_expected, result);
        }
    }
}
