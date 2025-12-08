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
        [InlineData("add", (int)0x1, -2, -1, -1)]
        [InlineData("add.ovf.un", (int)0x1, -2, -1, (long)0x00000000FFFFFFFF)]
        [InlineData("add.ovf", (int)0x1, -2, -1, -1)]
        [InlineData("sub", -1, -1, 0, 0)]
        [InlineData("sub.ovf", -1, -1, 0, 0)]
        [InlineData("sub.ovf.un", -1, -1, 0, unchecked((long)0xFFFFFFFF00000000))]
        [InlineData("mul", (int)0x2, -1, -2, -2)]
        [InlineData("mul.ovf", (int)0x2, -1, -2, -2)]
        [InlineData("mul.ovf.un", (int)0x2, -1, -2, 0x1FFFFFFFE)]
        [InlineData("div", -1, -1, 1, 1)]
        [InlineData("div.un", -1, -1, 1, 1)]
        [InlineData("rem", -1, -1, 0, 0)]
        [InlineData("rem.un", -1, -1, 0, 0)]
        [InlineData("and", -1, -1, -1, -1)]
        [InlineData("or", 0, -1, -1, -1)]
        [InlineData("xor", -1, -1, 0, 0)]
        [InlineData("ceq", -1, -1, 1, 1)]
        [InlineData("cgt", -2, -3, 1, 1)]
        [InlineData("cgt.un", -2, -1, 0, 0)]
        [InlineData("clt", -1, -2, 0, 0)]
        [InlineData("clt.un", -2, -1, 1, 1)]
        public static void testI_i32_opcode(string opcode, int a, int b, int resultI_i32_32Bit_expected, long resultI_i32_64Bit_expected)
        {
            nint a_nint = (nint)a;
            nint result = 0;

            switch(opcode)
            {
                case "add":
                    result = Operator.add_I_i32(a_nint, b);
                    break;
                case "add.ovf.un":
                    result = Operator.add_ovf_un_I_i32(a_nint, b);
                    break;
                case "add.ovf":
                    result = Operator.add_ovf_I_i32(a_nint, b);
                    break;
                case "sub":
                    result = Operator.sub_I_i32(a_nint, b);
                    break;
                case "sub.ovf":
                    result = Operator.sub_ovf_I_i32(a_nint, b);
                    break;
                case "sub.ovf.un":
                    result = Operator.sub_ovf_un_I_i32(a_nint, b);
                    break;
                case "mul":
                    result = Operator.mul_I_i32(a_nint, b);
                    break;
                case "mul.ovf":
                    result = Operator.mul_ovf_I_i32(a_nint, b);
                    break;
                case "mul.ovf.un":
                    result = Operator.mul_ovf_un_I_i32(a_nint, b);
                    break;
                case "div":
                    result = Operator.div_I_i32(a_nint, b);
                    break;
                case "div.un":
                    result = Operator.div_un_I_i32(a_nint, b);
                    break;
                case "rem":
                    result = Operator.rem_I_i32(a_nint, b);
                    break;
                case "rem.un":
                    result = Operator.rem_un_I_i32(a_nint, b);
                    break;
                case "and":
                    result = Operator.and_I_i32(a_nint, b);
                    break;
                case "or":
                    result = Operator.or_I_i32(a_nint, b);
                    break;
                case "xor":
                    result = Operator.xor_I_i32(a_nint, b);
                    break;
                case "ceq":
                    result = Operator.ceq_I_i32(a_nint, b);
                    break;
                case "cgt":
                    result = Operator.cgt_I_i32(a_nint, b);
                    break;
                case "cgt.un":
                    result = Operator.cgt_un_I_i32(a_nint, b);
                    break;
                case "clt":
                    result = Operator.clt_I_i32(a_nint, b);
                    break;
                case "clt.un":
                    result = Operator.clt_un_I_i32(a_nint, b);
                    break;
                default:
                    throw new ArgumentException("Invalid opcode");
            }

            Assert.Equal(Environment.Is64BitProcess ? (nint)resultI_i32_64Bit_expected : (nint)resultI_i32_32Bit_expected, result);
        }

        [Theory]
        [InlineData("add", -2, 1, -1, -1)]
        [InlineData("add.ovf.un", -2, 1, -1, (long)0x00000000FFFFFFFF)]
        [InlineData("add.ovf", -2, 1, -1, -1)]
        [InlineData("sub", -1, -1, 0, 0)]
        [InlineData("sub.ovf", -1, -1, 0, 0)]
        [InlineData("sub.ovf.un", -1, 1, -2, 0xFFFFFFFE)]
        [InlineData("mul",  -1, 2,-2, -2)]
        [InlineData("mul.ovf", -1, 2, -2, -2)]
        [InlineData("mul.ovf.un", -1, 2, -2, 0x1FFFFFFFE)]
        [InlineData("div", -1, -1, 1, 1)]
        [InlineData("div.un", -1, -1, 1, 1)]
        [InlineData("rem", -1, -1, 0, 0)]
        [InlineData("rem.un", -1, -1, 0, 0)]
        [InlineData("and", -1, -1, -1, -1)]
        [InlineData("or", -1, 0, -1, -1)]
        [InlineData("xor", -1, -1, 0, 0)]
        [InlineData("ceq", -1, -1, 1, 1)]
        [InlineData("cgt", -2, -1, 0, 0)]
        [InlineData("cgt.un", -1, -2, 1, 1)]
        [InlineData("clt", -1, -2, 0, 0)]
        [InlineData("clt.un", -2, -1, 1, 1)]
        public static void testi32_I_opcode(string opcode, int a, int b, int resultI_i32_32Bit_expected, long resultI_i32_64Bit_expected)
        {
            nint b_nint = (nint)b;
            nint result = 0;

            switch(opcode)
            {
                case "add":
                    result = Operator.add_i32_I(a, b_nint);
                    break;
                case "add.ovf.un":
                    result = Operator.add_ovf_un_i32_I(a, b_nint);
                    break;
                case "add.ovf":
                    result = Operator.add_ovf_i32_I(a, b_nint);
                    break;
                case "sub":
                    result = Operator.sub_i32_I(a, b_nint);
                    break;
                case "sub.ovf":
                    result = Operator.sub_ovf_i32_I(a, b_nint);
                    break;
                case "sub.ovf.un":
                    result = Operator.sub_ovf_un_i32_I(a, b_nint);
                    break;
                case "mul":
                    result = Operator.mul_i32_I(a, b_nint);
                    break;
                case "mul.ovf":
                    result = Operator.mul_ovf_i32_I(a, b_nint);
                    break;
                case "mul.ovf.un":
                    result = Operator.mul_ovf_un_i32_I(a, b_nint);
                    break;
                case "div":
                    result = Operator.div_i32_I(a, b_nint);
                    break;
                case "div.un":
                    result = Operator.div_un_i32_I(a, b_nint);
                    break;
                case "rem":
                    result = Operator.rem_i32_I(a, b_nint);
                    break;
                case "rem.un":
                    result = Operator.rem_un_i32_I(a, b_nint);
                    break;
                case "and":
                    result = Operator.and_i32_I(a, b_nint);
                    break;
                case "or":
                    result = Operator.or_i32_I(a, b_nint);
                    break;
                case "xor":
                    result = Operator.xor_i32_I(a, b_nint);
                    break;
                case "ceq":
                    result = Operator.ceq_i32_I(a, b_nint);
                    break;
                case "cgt":
                    result = Operator.cgt_i32_I(a, b_nint);
                    break;
                case "cgt.un":
                    result = Operator.cgt_un_i32_I(a, b_nint);
                    break;
                case "clt":
                    result = Operator.clt_i32_I(a, b_nint);
                    break;
                case "clt.un":
                    result = Operator.clt_un_i32_I(a, b_nint);
                    break;
                default:
                    throw new ArgumentException("Invalid opcode");
            }

            Assert.Equal(Environment.Is64BitProcess ? (nint)resultI_i32_64Bit_expected : (nint)resultI_i32_32Bit_expected, result);
        }
    }
}
