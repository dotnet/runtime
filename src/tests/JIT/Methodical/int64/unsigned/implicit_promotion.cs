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
        // These tests attempt to verify that the implicit upcasting from I4 to I which happens on 64 bit platforms
        // is done in a consistent manner across all implementations.
        // Notable details of interest:
        // add.ovf.un, sub.ovf.un, mul.ovf.un upcast without sign-extension.
        // div.un, and rem.un upcast with sign-extension.
        // clt.un, cgt.un upcast without sign-extension
        // bne.un, blt.un, ble.un, bgt.un, bge.un upcast without sign-extension
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/122398", TestRuntimes.Mono)]
        public static void TestUpcastBehavior()
        {
            unchecked
            {
                //////////////////////////////////////////////////////////////////
                /// Test scenarios where the first operand is I and the second is i32
                /////////////////////////////////////////////////////////////////

                // add: (int)0x1 + -2
                Assert.Equal(Environment.Is64BitProcess ? (nint)(-1) : (nint)(-1), Operator.add_I_i32((nint)0x1, -2));

                // add.ovf.un: (int)0x1 + -2
                Assert.Equal(Environment.Is64BitProcess ? (nint)(ulong)0x00000000FFFFFFFF : (nint)(-1), Operator.add_ovf_un_I_i32((nint)0x1, -2));

                // add.ovf: (int)0x1 + -2
                Assert.Equal(Environment.Is64BitProcess ? (nint)(-1) : (nint)(-1), Operator.add_ovf_I_i32((nint)0x1, -2));

                // sub: -1 - -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)0 : (nint)0, Operator.sub_I_i32((nint)(-1), -1));

                // sub.ovf: -1 - -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)0 : (nint)0, Operator.sub_ovf_I_i32((nint)(-1), -1));

                // sub.ovf.un: -1 - -1
                Assert.Equal(Environment.Is64BitProcess ? unchecked((nint)(ulong)0xFFFFFFFF00000000) : (nint)0, Operator.sub_ovf_un_I_i32((nint)(-1), -1));

                // mul: (int)0x2 * -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)(-2) : (nint)(-2), Operator.mul_I_i32((nint)0x2, -1));

                // mul.ovf: (int)0x2 * -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)(-2) : (nint)(-2), Operator.mul_ovf_I_i32((nint)0x2, -1));

                // mul.ovf.un: (int)0x2 * -1
                if (!Environment.Is64BitProcess)
                {
                    Assert.Throws<OverflowException>(() => Operator.mul_ovf_un_I_i32((nint)0x2, -1));
                }
                else
                {
                    Assert.Equal((nint)(ulong)0x1FFFFFFFE, Operator.mul_ovf_un_I_i32((nint)0x2, -1));
                }

                // div: -1 / -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)1, Operator.div_I_i32((nint)(-1), -1));

                // div.un: -1 / -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)1, Operator.div_un_I_i32((nint)(-1), -1));

                // rem: -1 % -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)0 : (nint)0, Operator.rem_I_i32((nint)(-1), -1));

                // rem.un: -1 % -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)0 : (nint)0, Operator.rem_un_I_i32((nint)(-1), -1));

                // and: -1 & -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)(-1) : (nint)(-1), Operator.and_I_i32((nint)(-1), -1));

                // or: 0 | -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)(-1) : (nint)(-1), Operator.or_I_i32((nint)0, -1));

                // xor: -1 ^ -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)0 : (nint)0, Operator.xor_I_i32((nint)(-1), -1));

                // ceq: -1 == -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)1, Operator.ceq_I_i32((nint)(-1), -1));

                // cgt: -2 > -3
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)1, Operator.cgt_I_i32((nint)(-2), -3));

                // cgt.un: -2 > -1 (unsigned)
                Assert.Equal(Environment.Is64BitProcess ? (nint)0 : (nint)0, Operator.cgt_un_I_i32((nint)(-2), -1));

                // clt: -1 < -2
                Assert.Equal(Environment.Is64BitProcess ? (nint)0 : (nint)0, Operator.clt_I_i32((nint)(-1), -2));

                // clt.un: -2 < -1 (unsigned)
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)1, Operator.clt_un_I_i32((nint)(-2), -1));

                // beq: -1 == -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)1, Operator.beq_I_i32((nint)(-1), -1));

                // bne.un: -1 != -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)0, Operator.bne_un_I_i32((nint)(-1), -1));

                // blt: -1 < -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)0 : (nint)0, Operator.blt_I_i32((nint)(-1), -1));

                // blt.un: -2 < -1 (unsigned)
                Assert.Equal(Environment.Is64BitProcess ? (nint)0 : (nint)1, Operator.blt_un_I_i32((nint)(-2), -1));

                // ble: -1 <= -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)1, Operator.ble_I_i32((nint)(-1), -1));

                // ble.un: -1 <= -1 (unsigned)
                Assert.Equal(Environment.Is64BitProcess ? (nint)0 : (nint)1, Operator.ble_un_I_i32((nint)(-1), -1));

                // bgt: -1 > -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)0 : (nint)0, Operator.bgt_I_i32((nint)(-1), -1));

                // bgt.un: -1 > -1 (unsigned)
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)0, Operator.bgt_un_I_i32((nint)(-1), -1));

                // bge: -1 >= -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)1, Operator.bge_I_i32((nint)(-1), -1));

                // bge.un: 0xFFFFFFFF >= -2 (unsigned, special case)
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)1, Operator.bge_un_I_i32(unchecked((nint)(nuint)0xFFFFFFFF), -2));

                //////////////////////////////////////////////////////////////////
                /// Test scenarios where the first operand is i32 and the second is I
                /////////////////////////////////////////////////////////////////

                // add: -2 + 1
                Assert.Equal(Environment.Is64BitProcess ? (nint)(-1) : (nint)(-1), Operator.add_i32_I(-2, (nint)1));

                // add.ovf.un: -2 + 1
                Assert.Equal(Environment.Is64BitProcess ? (nint)(ulong)0x00000000FFFFFFFF : (nint)(-1), Operator.add_ovf_un_i32_I(-2, (nint)1));

                // add.ovf: -2 + 1
                Assert.Equal(Environment.Is64BitProcess ? (nint)(-1) : (nint)(-1), Operator.add_ovf_i32_I(-2, (nint)1));

                // sub: -1 - -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)0 : (nint)0, Operator.sub_i32_I(-1, (nint)(-1)));

                // sub.ovf: -1 - -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)0 : (nint)0, Operator.sub_ovf_i32_I(-1, (nint)(-1)));

                // sub.ovf.un: -1 - 1
                Assert.Equal(Environment.Is64BitProcess ? (nint)(ulong)0xFFFFFFFE : (nint)(-2), Operator.sub_ovf_un_i32_I(-1, (nint)1));

                // mul: -1 * 2
                Assert.Equal(Environment.Is64BitProcess ? (nint)(-2) : (nint)(-2), Operator.mul_i32_I(-1, (nint)2));

                // mul.ovf: -1 * 2
                Assert.Equal(Environment.Is64BitProcess ? (nint)(-2) : (nint)(-2), Operator.mul_ovf_i32_I(-1, (nint)2));

                // mul.ovf.un: -1 * 2
                if (!Environment.Is64BitProcess)
                {
                    Assert.Throws<OverflowException>(() => Operator.mul_ovf_un_i32_I(-1, (nint)2));
                }
                else
                {
                    Assert.Equal((nint)(ulong)0x1FFFFFFFE, Operator.mul_ovf_un_i32_I(-1, (nint)2));
                }

                // div: -1 / -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)1, Operator.div_i32_I(-1, (nint)(-1)));

                // div.un: -1 / -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)1, Operator.div_un_i32_I(-1, (nint)(-1)));

                // rem: -1 % -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)0 : (nint)0, Operator.rem_i32_I(-1, (nint)(-1)));

                // rem.un: -1 % -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)0 : (nint)0, Operator.rem_un_i32_I(-1, (nint)(-1)));

                // and: -1 & -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)(-1) : (nint)(-1), Operator.and_i32_I(-1, (nint)(-1)));

                // or: -1 | 0
                Assert.Equal(Environment.Is64BitProcess ? (nint)(-1) : (nint)(-1), Operator.or_i32_I(-1, (nint)0));

                // xor: -1 ^ -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)0 : (nint)0, Operator.xor_i32_I(-1, (nint)(-1)));

                // ceq: -1 == -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)1, Operator.ceq_i32_I(-1, (nint)(-1)));

                // cgt: -2 > -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)0 : (nint)0, Operator.cgt_i32_I(-2, (nint)(-1)));

                // cgt.un: -1 > -2 (unsigned)
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)1, Operator.cgt_un_i32_I(-1, (nint)(-2)));

                // clt: -1 < -2
                Assert.Equal(Environment.Is64BitProcess ? (nint)0 : (nint)0, Operator.clt_i32_I(-1, (nint)(-2)));

                // clt.un: -2 < -1 (unsigned)
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)1, Operator.clt_un_i32_I(-2, (nint)(-1)));

                // beq: -1 == -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)1, Operator.beq_i32_I(-1, (nint)(-1)));

                // bne.un: -1 != -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)0, Operator.bne_un_i32_I(-1, (nint)(-1)));

                // blt: -2 < -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)1, Operator.blt_i32_I(-2, (nint)(-1)));

                // blt.un: -1 < -2 (unsigned)
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)0, Operator.blt_un_i32_I(-1, (nint)(-2)));

                // ble: -1 <= -1
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)1, Operator.ble_i32_I(-1, (nint)(-1)));

                // ble.un: -1 <= -2 (unsigned)
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)0, Operator.ble_un_i32_I(-1, (nint)(-2)));

                // bgt: -1 > -2
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)1, Operator.bgt_i32_I(-1, (nint)(-2)));

                // bgt.un: -1 > -2 (unsigned)
                Assert.Equal(Environment.Is64BitProcess ? (nint)0 : (nint)1, Operator.bgt_un_i32_I(-1, (nint)(-2)));

                // bge: -1 >= -2
                Assert.Equal(Environment.Is64BitProcess ? (nint)1 : (nint)1, Operator.bge_i32_I(-1, (nint)(-2)));

                // bge.un: -1 >= -2 (unsigned)
                Assert.Equal(Environment.Is64BitProcess ? (nint)0 : (nint)1, Operator.bge_un_i32_I(-1, (nint)(-2)));
            }
        }
    }
}
