// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class OpCodesTests
    {
        public static IEnumerable<object[]> TestData()
        {
            yield return new object[] { OpCodes.Add, "add", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x58, StackBehaviour.Pop1_pop1, StackBehaviour.Push1,  -1};
            yield return new object[] { OpCodes.Add_Ovf, "add.ovf", OpCodeType.Primitive, OperandType.InlineNone, 1, 0xd6, StackBehaviour.Pop1_pop1, StackBehaviour.Push1, -1 };
            yield return new object[] { OpCodes.Add_Ovf_Un, "add.ovf.un", OpCodeType.Primitive, OperandType.InlineNone, 1, 0xd7, StackBehaviour.Pop1_pop1, StackBehaviour.Push1, -1 };
            yield return new object[] { OpCodes.And, "and", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x5f, StackBehaviour.Pop1_pop1, StackBehaviour.Push1 , -1 };
            yield return new object[] { OpCodes.Arglist, "arglist", OpCodeType.Primitive, OperandType.InlineNone, 2, 0xfe00, StackBehaviour.Pop0, StackBehaviour.Pushi, 1 };
            yield return new object[] { OpCodes.Beq, "beq", OpCodeType.Macro, OperandType.InlineBrTarget, 1, 0x3b, StackBehaviour.Pop1_pop1, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Beq_S, "beq.s", OpCodeType.Macro, OperandType.ShortInlineBrTarget, 1, 0x2e, StackBehaviour.Pop1_pop1, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Bge, "bge", OpCodeType.Macro, OperandType.InlineBrTarget, 1, 0x3c, StackBehaviour.Pop1_pop1, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Bge_S, "bge.s", OpCodeType.Macro, OperandType.ShortInlineBrTarget, 1, 0x2f, StackBehaviour.Pop1_pop1, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Bge_Un, "bge.un", OpCodeType.Macro, OperandType.InlineBrTarget, 1, 0x41, StackBehaviour.Pop1_pop1, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Bge_Un_S, "bge.un.s", OpCodeType.Macro, OperandType.ShortInlineBrTarget, 1, 0x34, StackBehaviour.Pop1_pop1, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Bgt, "bgt", OpCodeType.Macro, OperandType.InlineBrTarget, 1, 0x3d, StackBehaviour.Pop1_pop1, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Bgt_S, "bgt.s", OpCodeType.Macro, OperandType.ShortInlineBrTarget, 1, 0x30, StackBehaviour.Pop1_pop1, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Bgt_Un, "bgt.un", OpCodeType.Macro, OperandType.InlineBrTarget, 1, 0x42, StackBehaviour.Pop1_pop1, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Bgt_Un_S, "bgt.un.s", OpCodeType.Macro, OperandType.ShortInlineBrTarget, 1, 0x35, StackBehaviour.Pop1_pop1, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Ble, "ble", OpCodeType.Macro, OperandType.InlineBrTarget, 1, 0x3e, StackBehaviour.Pop1_pop1, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Ble_S, "ble.s", OpCodeType.Macro, OperandType.ShortInlineBrTarget, 1, 0x31, StackBehaviour.Pop1_pop1, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Ble_Un, "ble.un", OpCodeType.Macro, OperandType.InlineBrTarget, 1, 0x43, StackBehaviour.Pop1_pop1, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Ble_Un_S, "ble.un.s", OpCodeType.Macro, OperandType.ShortInlineBrTarget, 1, 0x36, StackBehaviour.Pop1_pop1, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Blt, "blt", OpCodeType.Macro, OperandType.InlineBrTarget, 1, 0x3f, StackBehaviour.Pop1_pop1, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Blt_S, "blt.s", OpCodeType.Macro, OperandType.ShortInlineBrTarget, 1, 0x32, StackBehaviour.Pop1_pop1, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Blt_Un, "blt.un", OpCodeType.Macro, OperandType.InlineBrTarget, 1, 0x44, StackBehaviour.Pop1_pop1, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Blt_Un_S, "blt.un.s", OpCodeType.Macro, OperandType.ShortInlineBrTarget, 1, 0x37, StackBehaviour.Pop1_pop1, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Bne_Un, "bne.un", OpCodeType.Macro, OperandType.InlineBrTarget, 1, 0x40, StackBehaviour.Pop1_pop1, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Bne_Un_S, "bne.un.s", OpCodeType.Macro, OperandType.ShortInlineBrTarget, 1, 0x33, StackBehaviour.Pop1_pop1, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Box, "box", OpCodeType.Primitive, OperandType.InlineType, 1, 0x8c, StackBehaviour.Pop1, StackBehaviour.Pushref, 0 };
            yield return new object[] { OpCodes.Br, "br", OpCodeType.Primitive, OperandType.InlineBrTarget, 1, 0x38, StackBehaviour.Pop0, StackBehaviour.Push0, 0 };
            yield return new object[] { OpCodes.Br_S, "br.s", OpCodeType.Macro, OperandType.ShortInlineBrTarget, 1, 0x2b, StackBehaviour.Pop0, StackBehaviour.Push0, 0 };
            yield return new object[] { OpCodes.Break, "break", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x1, StackBehaviour.Pop0, StackBehaviour.Push0, 0 };
            yield return new object[] { OpCodes.Brfalse, "brfalse", OpCodeType.Primitive, OperandType.InlineBrTarget, 1, 0x39, StackBehaviour.Popi, StackBehaviour.Push0, -1 };
            yield return new object[] { OpCodes.Brfalse_S, "brfalse.s", OpCodeType.Macro, OperandType.ShortInlineBrTarget, 1, 0x2c, StackBehaviour.Popi, StackBehaviour.Push0, -1 };
            yield return new object[] { OpCodes.Brtrue, "brtrue", OpCodeType.Primitive, OperandType.InlineBrTarget, 1, 0x3a, StackBehaviour.Popi, StackBehaviour.Push0, -1 };
            yield return new object[] { OpCodes.Brtrue_S, "brtrue.s", OpCodeType.Macro, OperandType.ShortInlineBrTarget, 1, 0x2d, StackBehaviour.Popi, StackBehaviour.Push0, -1 };
            yield return new object[] { OpCodes.Call, "call", OpCodeType.Primitive, OperandType.InlineMethod, 1, 0x28, StackBehaviour.Varpop, StackBehaviour.Varpush, 0 };
            yield return new object[] { OpCodes.Calli, "calli", OpCodeType.Primitive, OperandType.InlineSig, 1, 0x29, StackBehaviour.Varpop, StackBehaviour.Varpush, 0 };
            yield return new object[] { OpCodes.Callvirt, "callvirt", OpCodeType.Objmodel, OperandType.InlineMethod, 1, 0x6f, StackBehaviour.Varpop, StackBehaviour.Varpush, 0 };
            yield return new object[] { OpCodes.Castclass, "castclass", OpCodeType.Objmodel, OperandType.InlineType, 1, 0x74, StackBehaviour.Popref, StackBehaviour.Pushref, 0 };
            yield return new object[] { OpCodes.Ceq, "ceq", OpCodeType.Primitive, OperandType.InlineNone, 2, 0xfe01, StackBehaviour.Pop1_pop1, StackBehaviour.Pushi, -1 };
            yield return new object[] { OpCodes.Cgt, "cgt", OpCodeType.Primitive, OperandType.InlineNone, 2, 0xfe02, StackBehaviour.Pop1_pop1, StackBehaviour.Pushi, -1 };
            yield return new object[] { OpCodes.Cgt_Un, "cgt.un", OpCodeType.Primitive, OperandType.InlineNone, 2, 0xfe03, StackBehaviour.Pop1_pop1, StackBehaviour.Pushi, -1 };
            yield return new object[] { OpCodes.Ckfinite, "ckfinite", OpCodeType.Primitive, OperandType.InlineNone, 1, 0xc3, StackBehaviour.Pop1, StackBehaviour.Pushr8, 0 };
            yield return new object[] { OpCodes.Clt, "clt", OpCodeType.Primitive, OperandType.InlineNone, 2, 0xfe04, StackBehaviour.Pop1_pop1, StackBehaviour.Pushi, -1 };
            yield return new object[] { OpCodes.Clt_Un, "clt.un", OpCodeType.Primitive, OperandType.InlineNone, 2, 0xfe05, StackBehaviour.Pop1_pop1, StackBehaviour.Pushi, -1 };
            yield return new object[] { OpCodes.Constrained, "constrained.", OpCodeType.Prefix, OperandType.InlineType, 2, 0xfe16, StackBehaviour.Pop0, StackBehaviour.Push0, 0 };
            yield return new object[] { OpCodes.Conv_I, "conv.i", OpCodeType.Primitive, OperandType.InlineNone, 1, 0xd3, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_I1, "conv.i1", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x67, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_I2, "conv.i2", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x68, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_I4, "conv.i4", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x69, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_I8, "conv.i8", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x6a, StackBehaviour.Pop1, StackBehaviour.Pushi8, 0 };
            yield return new object[] { OpCodes.Conv_Ovf_I, "conv.ovf.i", OpCodeType.Primitive, OperandType.InlineNone, 1, 0xd4, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_Ovf_I_Un, "conv.ovf.i.un", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x8a, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_Ovf_I1, "conv.ovf.i1", OpCodeType.Primitive, OperandType.InlineNone, 1, 0xb3, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_Ovf_I1_Un, "conv.ovf.i1.un", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x82, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_Ovf_I2, "conv.ovf.i2", OpCodeType.Primitive, OperandType.InlineNone, 1, 0xb5, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_Ovf_I2_Un, "conv.ovf.i2.un", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x83, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_Ovf_I4, "conv.ovf.i4", OpCodeType.Primitive, OperandType.InlineNone, 1, 0xb7, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_Ovf_I4_Un, "conv.ovf.i4.un", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x84, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_Ovf_I8, "conv.ovf.i8", OpCodeType.Primitive, OperandType.InlineNone, 1, 0xb9, StackBehaviour.Pop1, StackBehaviour.Pushi8, 0 };
            yield return new object[] { OpCodes.Conv_Ovf_I8_Un, "conv.ovf.i8.un", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x85, StackBehaviour.Pop1, StackBehaviour.Pushi8, 0 };
            yield return new object[] { OpCodes.Conv_Ovf_U, "conv.ovf.u", OpCodeType.Primitive, OperandType.InlineNone, 1, 0xd5, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_Ovf_U_Un, "conv.ovf.u.un", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x8b, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_Ovf_U1, "conv.ovf.u1", OpCodeType.Primitive, OperandType.InlineNone, 1, 0xb4, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_Ovf_U1_Un, "conv.ovf.u1.un", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x86, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_Ovf_U2, "conv.ovf.u2", OpCodeType.Primitive, OperandType.InlineNone, 1, 0xb6, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_Ovf_U2_Un, "conv.ovf.u2.un", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x87, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_Ovf_U4, "conv.ovf.u4", OpCodeType.Primitive, OperandType.InlineNone, 1, 0xb8, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_Ovf_U4_Un, "conv.ovf.u4.un", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x88, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_Ovf_U8, "conv.ovf.u8", OpCodeType.Primitive, OperandType.InlineNone, 1, 0xba, StackBehaviour.Pop1, StackBehaviour.Pushi8, 0 };
            yield return new object[] { OpCodes.Conv_Ovf_U8_Un, "conv.ovf.u8.un", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x89, StackBehaviour.Pop1, StackBehaviour.Pushi8, 0 };
            yield return new object[] { OpCodes.Conv_R_Un, "conv.r.un", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x76, StackBehaviour.Pop1, StackBehaviour.Pushr8, 0 };
            yield return new object[] { OpCodes.Conv_R4, "conv.r4", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x6b, StackBehaviour.Pop1, StackBehaviour.Pushr4, 0 };
            yield return new object[] { OpCodes.Conv_R8, "conv.r8", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x6c, StackBehaviour.Pop1, StackBehaviour.Pushr8, 0 };
            yield return new object[] { OpCodes.Conv_U, "conv.u", OpCodeType.Primitive, OperandType.InlineNone, 1, 0xe0, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_U1, "conv.u1", OpCodeType.Primitive, OperandType.InlineNone, 1, 0xd2, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_U2, "conv.u2", OpCodeType.Primitive, OperandType.InlineNone, 1, 0xd1, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_U4, "conv.u4", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x6d, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Conv_U8, "conv.u8", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x6e, StackBehaviour.Pop1, StackBehaviour.Pushi8, 0 };
            yield return new object[] { OpCodes.Cpblk, "cpblk", OpCodeType.Primitive, OperandType.InlineNone, 2, 0xfe17, StackBehaviour.Popi_popi_popi, StackBehaviour.Push0, -3 };
            yield return new object[] { OpCodes.Cpobj, "cpobj", OpCodeType.Objmodel, OperandType.InlineType, 1, 0x70, StackBehaviour.Popi_popi, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Div, "div", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x5b, StackBehaviour.Pop1_pop1, StackBehaviour.Push1, -1 };
            yield return new object[] { OpCodes.Div_Un, "div.un", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x5c, StackBehaviour.Pop1_pop1, StackBehaviour.Push1, -1 };
            yield return new object[] { OpCodes.Dup, "dup", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x25, StackBehaviour.Pop1, StackBehaviour.Push1_push1, 1 };
            yield return new object[] { OpCodes.Endfilter, "endfilter", OpCodeType.Primitive, OperandType.InlineNone, 2, 0xfe11, StackBehaviour.Popi, StackBehaviour.Push0, -1 };
            yield return new object[] { OpCodes.Endfinally, "endfinally", OpCodeType.Primitive, OperandType.InlineNone, 1, 0xdc, StackBehaviour.Pop0, StackBehaviour.Push0, 0 };
            yield return new object[] { OpCodes.Initblk, "initblk", OpCodeType.Primitive, OperandType.InlineNone, 2, 0xfe18, StackBehaviour.Popi_popi_popi, StackBehaviour.Push0, -3 };
            yield return new object[] { OpCodes.Initobj, "initobj", OpCodeType.Objmodel, OperandType.InlineType, 2, 0xfe15, StackBehaviour.Popi, StackBehaviour.Push0, -1 };
            yield return new object[] { OpCodes.Isinst, "isinst", OpCodeType.Objmodel, OperandType.InlineType, 1, 0x75, StackBehaviour.Popref, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Jmp, "jmp", OpCodeType.Primitive, OperandType.InlineMethod, 1, 0x27, StackBehaviour.Pop0, StackBehaviour.Push0, 0 };
            yield return new object[] { OpCodes.Ldarg, "ldarg", OpCodeType.Primitive, OperandType.InlineVar, 2, 0xfe09, StackBehaviour.Pop0, StackBehaviour.Push1, 1 };
            yield return new object[] { OpCodes.Ldarg_0, "ldarg.0", OpCodeType.Macro, OperandType.InlineNone, 1, 0x2, StackBehaviour.Pop0, StackBehaviour.Push1, 1 };
            yield return new object[] { OpCodes.Ldarg_1, "ldarg.1", OpCodeType.Macro, OperandType.InlineNone, 1, 0x3, StackBehaviour.Pop0, StackBehaviour.Push1, 1 };
            yield return new object[] { OpCodes.Ldarg_2, "ldarg.2", OpCodeType.Macro, OperandType.InlineNone, 1, 0x4, StackBehaviour.Pop0, StackBehaviour.Push1, 1 };
            yield return new object[] { OpCodes.Ldarg_3, "ldarg.3", OpCodeType.Macro, OperandType.InlineNone, 1, 0x5, StackBehaviour.Pop0, StackBehaviour.Push1, 1 };
            yield return new object[] { OpCodes.Ldarg_S, "ldarg.s", OpCodeType.Macro, OperandType.ShortInlineVar, 1, 0xe, StackBehaviour.Pop0, StackBehaviour.Push1, 1 };
            yield return new object[] { OpCodes.Ldarga, "ldarga", OpCodeType.Primitive, OperandType.InlineVar, 2, 0xfe0a, StackBehaviour.Pop0, StackBehaviour.Pushi, 1 };
            yield return new object[] { OpCodes.Ldarga_S, "ldarga.s", OpCodeType.Macro, OperandType.ShortInlineVar, 1, 0xf, StackBehaviour.Pop0, StackBehaviour.Pushi, 1 };
            yield return new object[] { OpCodes.Ldc_I4, "ldc.i4", OpCodeType.Primitive, OperandType.InlineI, 1, 0x20, StackBehaviour.Pop0, StackBehaviour.Pushi, 1 };
            yield return new object[] { OpCodes.Ldc_I4_0, "ldc.i4.0", OpCodeType.Macro, OperandType.InlineNone, 1, 0x16, StackBehaviour.Pop0, StackBehaviour.Pushi, 1 };
            yield return new object[] { OpCodes.Ldc_I4_1, "ldc.i4.1", OpCodeType.Macro, OperandType.InlineNone, 1, 0x17, StackBehaviour.Pop0, StackBehaviour.Pushi, 1 };
            yield return new object[] { OpCodes.Ldc_I4_2, "ldc.i4.2", OpCodeType.Macro, OperandType.InlineNone, 1, 0x18, StackBehaviour.Pop0, StackBehaviour.Pushi, 1 };
            yield return new object[] { OpCodes.Ldc_I4_3, "ldc.i4.3", OpCodeType.Macro, OperandType.InlineNone, 1, 0x19, StackBehaviour.Pop0, StackBehaviour.Pushi, 1 };
            yield return new object[] { OpCodes.Ldc_I4_4, "ldc.i4.4", OpCodeType.Macro, OperandType.InlineNone, 1, 0x1a, StackBehaviour.Pop0, StackBehaviour.Pushi, 1 };
            yield return new object[] { OpCodes.Ldc_I4_5, "ldc.i4.5", OpCodeType.Macro, OperandType.InlineNone, 1, 0x1b, StackBehaviour.Pop0, StackBehaviour.Pushi, 1 };
            yield return new object[] { OpCodes.Ldc_I4_6, "ldc.i4.6", OpCodeType.Macro, OperandType.InlineNone, 1, 0x1c, StackBehaviour.Pop0, StackBehaviour.Pushi, 1 };
            yield return new object[] { OpCodes.Ldc_I4_7, "ldc.i4.7", OpCodeType.Macro, OperandType.InlineNone, 1, 0x1d, StackBehaviour.Pop0, StackBehaviour.Pushi, 1 };
            yield return new object[] { OpCodes.Ldc_I4_8, "ldc.i4.8", OpCodeType.Macro, OperandType.InlineNone, 1, 0x1e, StackBehaviour.Pop0, StackBehaviour.Pushi, 1 };
            yield return new object[] { OpCodes.Ldc_I4_M1, "ldc.i4.m1", OpCodeType.Macro, OperandType.InlineNone, 1, 0x15, StackBehaviour.Pop0, StackBehaviour.Pushi, 1 };
            yield return new object[] { OpCodes.Ldc_I4_S, "ldc.i4.s", OpCodeType.Macro, OperandType.ShortInlineI, 1, 0x1f, StackBehaviour.Pop0, StackBehaviour.Pushi, 1 };
            yield return new object[] { OpCodes.Ldc_I8, "ldc.i8", OpCodeType.Primitive, OperandType.InlineI8, 1, 0x21, StackBehaviour.Pop0, StackBehaviour.Pushi8, 1 };
            yield return new object[] { OpCodes.Ldc_R4, "ldc.r4", OpCodeType.Primitive, OperandType.ShortInlineR, 1, 0x22, StackBehaviour.Pop0, StackBehaviour.Pushr4, 1 };
            yield return new object[] { OpCodes.Ldc_R8, "ldc.r8", OpCodeType.Primitive, OperandType.InlineR, 1, 0x23, StackBehaviour.Pop0, StackBehaviour.Pushr8, 1 };
            yield return new object[] { OpCodes.Ldelem, "ldelem", OpCodeType.Objmodel, OperandType.InlineType, 1, 0xa3, StackBehaviour.Popref_popi, StackBehaviour.Push1, -1 };
            yield return new object[] { OpCodes.Ldelem_I, "ldelem.i", OpCodeType.Objmodel, OperandType.InlineNone, 1, 0x97, StackBehaviour.Popref_popi, StackBehaviour.Pushi, -1 };
            yield return new object[] { OpCodes.Ldelem_I1, "ldelem.i1", OpCodeType.Objmodel, OperandType.InlineNone, 1, 0x90, StackBehaviour.Popref_popi, StackBehaviour.Pushi, -1 };
            yield return new object[] { OpCodes.Ldelem_I2, "ldelem.i2", OpCodeType.Objmodel, OperandType.InlineNone, 1, 0x92, StackBehaviour.Popref_popi, StackBehaviour.Pushi, -1 };
            yield return new object[] { OpCodes.Ldelem_I4, "ldelem.i4", OpCodeType.Objmodel, OperandType.InlineNone, 1, 0x94, StackBehaviour.Popref_popi, StackBehaviour.Pushi, -1 };
            yield return new object[] { OpCodes.Ldelem_I8, "ldelem.i8", OpCodeType.Objmodel, OperandType.InlineNone, 1, 0x96, StackBehaviour.Popref_popi, StackBehaviour.Pushi8, -1 };
            yield return new object[] { OpCodes.Ldelem_R4, "ldelem.r4", OpCodeType.Objmodel, OperandType.InlineNone, 1, 0x98, StackBehaviour.Popref_popi, StackBehaviour.Pushr4, -1 };
            yield return new object[] { OpCodes.Ldelem_R8, "ldelem.r8", OpCodeType.Objmodel, OperandType.InlineNone, 1, 0x99, StackBehaviour.Popref_popi, StackBehaviour.Pushr8, -1 };
            yield return new object[] { OpCodes.Ldelem_Ref, "ldelem.ref", OpCodeType.Objmodel, OperandType.InlineNone, 1, 0x9a, StackBehaviour.Popref_popi, StackBehaviour.Pushref, -1 };
            yield return new object[] { OpCodes.Ldelem_U1, "ldelem.u1", OpCodeType.Objmodel, OperandType.InlineNone, 1, 0x91, StackBehaviour.Popref_popi, StackBehaviour.Pushi, -1 };
            yield return new object[] { OpCodes.Ldelem_U2, "ldelem.u2", OpCodeType.Objmodel, OperandType.InlineNone, 1, 0x93, StackBehaviour.Popref_popi, StackBehaviour.Pushi, -1 };
            yield return new object[] { OpCodes.Ldelem_U4, "ldelem.u4", OpCodeType.Objmodel, OperandType.InlineNone, 1, 0x95, StackBehaviour.Popref_popi, StackBehaviour.Pushi, -1 };
            yield return new object[] { OpCodes.Ldelema, "ldelema", OpCodeType.Objmodel, OperandType.InlineType, 1, 0x8f, StackBehaviour.Popref_popi, StackBehaviour.Pushi, -1 };
            yield return new object[] { OpCodes.Ldfld, "ldfld", OpCodeType.Objmodel, OperandType.InlineField, 1, 0x7b, StackBehaviour.Popref, StackBehaviour.Push1, 0 };
            yield return new object[] { OpCodes.Ldflda, "ldflda", OpCodeType.Objmodel, OperandType.InlineField, 1, 0x7c, StackBehaviour.Popref, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Ldftn, "ldftn", OpCodeType.Primitive, OperandType.InlineMethod, 2, 0xfe06, StackBehaviour.Pop0, StackBehaviour.Pushi, 1 };
            yield return new object[] { OpCodes.Ldind_I, "ldind.i", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x4d, StackBehaviour.Popi, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Ldind_I1, "ldind.i1", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x46, StackBehaviour.Popi, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Ldind_I2, "ldind.i2", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x48, StackBehaviour.Popi, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Ldind_I4, "ldind.i4", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x4a, StackBehaviour.Popi, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Ldind_I8, "ldind.i8", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x4c, StackBehaviour.Popi, StackBehaviour.Pushi8, 0 };
            yield return new object[] { OpCodes.Ldind_R4, "ldind.r4", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x4e, StackBehaviour.Popi, StackBehaviour.Pushr4, 0 };
            yield return new object[] { OpCodes.Ldind_R8, "ldind.r8", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x4f, StackBehaviour.Popi, StackBehaviour.Pushr8, 0 };
            yield return new object[] { OpCodes.Ldind_Ref, "ldind.ref", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x50, StackBehaviour.Popi, StackBehaviour.Pushref, 0 };
            yield return new object[] { OpCodes.Ldind_U1, "ldind.u1", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x47, StackBehaviour.Popi, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Ldind_U2, "ldind.u2", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x49, StackBehaviour.Popi, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Ldind_U4, "ldind.u4", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x4b, StackBehaviour.Popi, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Ldlen, "ldlen", OpCodeType.Objmodel, OperandType.InlineNone, 1, 0x8e, StackBehaviour.Popref, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Ldloc, "ldloc", OpCodeType.Primitive, OperandType.InlineVar, 2, 0xfe0c, StackBehaviour.Pop0, StackBehaviour.Push1, 1 };
            yield return new object[] { OpCodes.Ldloc_0, "ldloc.0", OpCodeType.Macro, OperandType.InlineNone, 1, 0x6, StackBehaviour.Pop0, StackBehaviour.Push1, 1 };
            yield return new object[] { OpCodes.Ldloc_1, "ldloc.1", OpCodeType.Macro, OperandType.InlineNone, 1, 0x7, StackBehaviour.Pop0, StackBehaviour.Push1, 1 };
            yield return new object[] { OpCodes.Ldloc_2, "ldloc.2", OpCodeType.Macro, OperandType.InlineNone, 1, 0x8, StackBehaviour.Pop0, StackBehaviour.Push1, 1 };
            yield return new object[] { OpCodes.Ldloc_3, "ldloc.3", OpCodeType.Macro, OperandType.InlineNone, 1, 0x9, StackBehaviour.Pop0, StackBehaviour.Push1, 1 };
            yield return new object[] { OpCodes.Ldloc_S, "ldloc.s", OpCodeType.Macro, OperandType.ShortInlineVar, 1, 0x11, StackBehaviour.Pop0, StackBehaviour.Push1, 1 };
            yield return new object[] { OpCodes.Ldloca, "ldloca", OpCodeType.Primitive, OperandType.InlineVar, 2, 0xfe0d, StackBehaviour.Pop0, StackBehaviour.Pushi, 1 };
            yield return new object[] { OpCodes.Ldloca_S, "ldloca.s", OpCodeType.Macro, OperandType.ShortInlineVar, 1, 0x12, StackBehaviour.Pop0, StackBehaviour.Pushi, 1 };
            yield return new object[] { OpCodes.Ldnull, "ldnull", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x14, StackBehaviour.Pop0, StackBehaviour.Pushref, 1 };
            yield return new object[] { OpCodes.Ldobj, "ldobj", OpCodeType.Objmodel, OperandType.InlineType, 1, 0x71, StackBehaviour.Popi, StackBehaviour.Push1, 0 };
            yield return new object[] { OpCodes.Ldsfld, "ldsfld", OpCodeType.Objmodel, OperandType.InlineField, 1, 0x7e, StackBehaviour.Pop0, StackBehaviour.Push1, 1 };
            yield return new object[] { OpCodes.Ldsflda, "ldsflda", OpCodeType.Objmodel, OperandType.InlineField, 1, 0x7f, StackBehaviour.Pop0, StackBehaviour.Pushi, 1 };
            yield return new object[] { OpCodes.Ldstr, "ldstr", OpCodeType.Objmodel, OperandType.InlineString, 1, 0x72, StackBehaviour.Pop0, StackBehaviour.Pushref, 1 };
            yield return new object[] { OpCodes.Ldtoken, "ldtoken", OpCodeType.Primitive, OperandType.InlineTok, 1, 0xd0, StackBehaviour.Pop0, StackBehaviour.Pushi, 1 };
            yield return new object[] { OpCodes.Ldvirtftn, "ldvirtftn", OpCodeType.Primitive, OperandType.InlineMethod, 2, 0xfe07, StackBehaviour.Popref, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Leave, "leave", OpCodeType.Primitive, OperandType.InlineBrTarget, 1, 0xdd, StackBehaviour.Pop0, StackBehaviour.Push0, 0 };
            yield return new object[] { OpCodes.Leave_S, "leave.s", OpCodeType.Primitive, OperandType.ShortInlineBrTarget, 1, 0xde, StackBehaviour.Pop0, StackBehaviour.Push0, 0 };
            yield return new object[] { OpCodes.Localloc, "localloc", OpCodeType.Primitive, OperandType.InlineNone, 2, 0xfe0f, StackBehaviour.Popi, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Mkrefany, "mkrefany", OpCodeType.Primitive, OperandType.InlineType, 1, 0xc6, StackBehaviour.Popi, StackBehaviour.Push1, 0 };
            yield return new object[] { OpCodes.Mul, "mul", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x5a, StackBehaviour.Pop1_pop1, StackBehaviour.Push1, -1 };
            yield return new object[] { OpCodes.Mul_Ovf, "mul.ovf", OpCodeType.Primitive, OperandType.InlineNone, 1, 0xd8, StackBehaviour.Pop1_pop1, StackBehaviour.Push1, -1 };
            yield return new object[] { OpCodes.Mul_Ovf_Un, "mul.ovf.un", OpCodeType.Primitive, OperandType.InlineNone, 1, 0xd9, StackBehaviour.Pop1_pop1, StackBehaviour.Push1, -1 };
            yield return new object[] { OpCodes.Neg, "neg", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x65, StackBehaviour.Pop1, StackBehaviour.Push1, 0 };
            yield return new object[] { OpCodes.Newarr, "newarr", OpCodeType.Objmodel, OperandType.InlineType, 1, 0x8d, StackBehaviour.Popi, StackBehaviour.Pushref, 0 };
            yield return new object[] { OpCodes.Newobj, "newobj", OpCodeType.Objmodel, OperandType.InlineMethod, 1, 0x73, StackBehaviour.Varpop, StackBehaviour.Pushref, 1 };
            yield return new object[] { OpCodes.Nop, "nop", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x0, StackBehaviour.Pop0, StackBehaviour.Push0, 0 };
            yield return new object[] { OpCodes.Not, "not", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x66, StackBehaviour.Pop1, StackBehaviour.Push1, 0 };
            yield return new object[] { OpCodes.Or, "or", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x60, StackBehaviour.Pop1_pop1, StackBehaviour.Push1, -1 };
            yield return new object[] { OpCodes.Pop, "pop", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x26, StackBehaviour.Pop1, StackBehaviour.Push0, -1 };
            yield return new object[] { OpCodes.Prefix1, "prefix1", OpCodeType.Nternal, OperandType.InlineNone, 1, 0xfe, StackBehaviour.Pop0, StackBehaviour.Push0, 0 };
            yield return new object[] { OpCodes.Prefix2, "prefix2", OpCodeType.Nternal, OperandType.InlineNone, 1, 0xfd, StackBehaviour.Pop0, StackBehaviour.Push0, 0 };
            yield return new object[] { OpCodes.Prefix3, "prefix3", OpCodeType.Nternal, OperandType.InlineNone, 1, 0xfc, StackBehaviour.Pop0, StackBehaviour.Push0, 0 };
            yield return new object[] { OpCodes.Prefix4, "prefix4", OpCodeType.Nternal, OperandType.InlineNone, 1, 0xfb, StackBehaviour.Pop0, StackBehaviour.Push0, 0 };
            yield return new object[] { OpCodes.Prefix5, "prefix5", OpCodeType.Nternal, OperandType.InlineNone, 1, 0xfa, StackBehaviour.Pop0, StackBehaviour.Push0, 0 };
            yield return new object[] { OpCodes.Prefix6, "prefix6", OpCodeType.Nternal, OperandType.InlineNone, 1, 0xf9, StackBehaviour.Pop0, StackBehaviour.Push0, 0 };
            yield return new object[] { OpCodes.Prefix7, "prefix7", OpCodeType.Nternal, OperandType.InlineNone, 1, 0xf8, StackBehaviour.Pop0, StackBehaviour.Push0, 0 };
            yield return new object[] { OpCodes.Prefixref, "prefixref", OpCodeType.Nternal, OperandType.InlineNone, 1, 0xff, StackBehaviour.Pop0, StackBehaviour.Push0, 0 };
            yield return new object[] { OpCodes.Readonly, "readonly.", OpCodeType.Prefix, OperandType.InlineNone, 2, 0xfe1e, StackBehaviour.Pop0, StackBehaviour.Push0, 0 };
            yield return new object[] { OpCodes.Refanytype, "refanytype", OpCodeType.Primitive, OperandType.InlineNone, 2, 0xfe1d, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Refanyval, "refanyval", OpCodeType.Primitive, OperandType.InlineType, 1, 0xc2, StackBehaviour.Pop1, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Rem, "rem", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x5d, StackBehaviour.Pop1_pop1, StackBehaviour.Push1, -1 };
            yield return new object[] { OpCodes.Rem_Un, "rem.un", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x5e, StackBehaviour.Pop1_pop1, StackBehaviour.Push1, -1 };
            yield return new object[] { OpCodes.Ret, "ret", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x2a, StackBehaviour.Varpop, StackBehaviour.Push0, 0 }; // it depends if the method returns a value
            yield return new object[] { OpCodes.Rethrow, "rethrow", OpCodeType.Objmodel, OperandType.InlineNone, 2, 0xfe1a, StackBehaviour.Pop0, StackBehaviour.Push0, 0 };
            yield return new object[] { OpCodes.Shl, "shl", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x62, StackBehaviour.Pop1_pop1, StackBehaviour.Push1, -1 };
            yield return new object[] { OpCodes.Shr, "shr", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x63, StackBehaviour.Pop1_pop1, StackBehaviour.Push1, -1 };
            yield return new object[] { OpCodes.Shr_Un, "shr.un", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x64, StackBehaviour.Pop1_pop1, StackBehaviour.Push1, -1 };
            yield return new object[] { OpCodes.Sizeof, "sizeof", OpCodeType.Primitive, OperandType.InlineType, 2, 0xfe1c, StackBehaviour.Pop0, StackBehaviour.Pushi, 1 };
            yield return new object[] { OpCodes.Starg, "starg", OpCodeType.Primitive, OperandType.InlineVar, 2, 0xfe0b, StackBehaviour.Pop1, StackBehaviour.Push0, -1 };
            yield return new object[] { OpCodes.Starg_S, "starg.s", OpCodeType.Macro, OperandType.ShortInlineVar, 1, 0x10, StackBehaviour.Pop1, StackBehaviour.Push0, -1 };
            yield return new object[] { OpCodes.Stelem, "stelem", OpCodeType.Objmodel, OperandType.InlineType, 1, 0xa4, StackBehaviour.Popref_popi_pop1, StackBehaviour.Push0, -3 };
            yield return new object[] { OpCodes.Stelem_I, "stelem.i", OpCodeType.Objmodel, OperandType.InlineNone, 1, 0x9b, StackBehaviour.Popref_popi_popi, StackBehaviour.Push0, -3 };
            yield return new object[] { OpCodes.Stelem_I1, "stelem.i1", OpCodeType.Objmodel, OperandType.InlineNone, 1, 0x9c, StackBehaviour.Popref_popi_popi, StackBehaviour.Push0, -3 };
            yield return new object[] { OpCodes.Stelem_I2, "stelem.i2", OpCodeType.Objmodel, OperandType.InlineNone, 1, 0x9d, StackBehaviour.Popref_popi_popi, StackBehaviour.Push0, -3 };
            yield return new object[] { OpCodes.Stelem_I4, "stelem.i4", OpCodeType.Objmodel, OperandType.InlineNone, 1, 0x9e, StackBehaviour.Popref_popi_popi, StackBehaviour.Push0, -3 };
            yield return new object[] { OpCodes.Stelem_I8, "stelem.i8", OpCodeType.Objmodel, OperandType.InlineNone, 1, 0x9f, StackBehaviour.Popref_popi_popi8, StackBehaviour.Push0, -3 };
            yield return new object[] { OpCodes.Stelem_R4, "stelem.r4", OpCodeType.Objmodel, OperandType.InlineNone, 1, 0xa0, StackBehaviour.Popref_popi_popr4, StackBehaviour.Push0, -3 };
            yield return new object[] { OpCodes.Stelem_R8, "stelem.r8", OpCodeType.Objmodel, OperandType.InlineNone, 1, 0xa1, StackBehaviour.Popref_popi_popr8, StackBehaviour.Push0, -3 };
            yield return new object[] { OpCodes.Stelem_Ref, "stelem.ref", OpCodeType.Objmodel, OperandType.InlineNone, 1, 0xa2, StackBehaviour.Popref_popi_popref, StackBehaviour.Push0, -3 };
            yield return new object[] { OpCodes.Stfld, "stfld", OpCodeType.Objmodel, OperandType.InlineField, 1, 0x7d, StackBehaviour.Popref_pop1, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Stind_I, "stind.i", OpCodeType.Primitive, OperandType.InlineNone, 1, 0xdf, StackBehaviour.Popi_popi, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Stind_I1, "stind.i1", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x52, StackBehaviour.Popi_popi, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Stind_I2, "stind.i2", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x53, StackBehaviour.Popi_popi, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Stind_I4, "stind.i4", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x54, StackBehaviour.Popi_popi, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Stind_I8, "stind.i8", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x55, StackBehaviour.Popi_popi8, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Stind_R4, "stind.r4", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x56, StackBehaviour.Popi_popr4, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Stind_R8, "stind.r8", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x57, StackBehaviour.Popi_popr8, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Stind_Ref, "stind.ref", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x51, StackBehaviour.Popi_popi, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Stloc, "stloc", OpCodeType.Primitive, OperandType.InlineVar, 2, 0xfe0e, StackBehaviour.Pop1, StackBehaviour.Push0, -1 };
            yield return new object[] { OpCodes.Stloc_0, "stloc.0", OpCodeType.Macro, OperandType.InlineNone, 1, 0xa, StackBehaviour.Pop1, StackBehaviour.Push0, -1 };
            yield return new object[] { OpCodes.Stloc_1, "stloc.1", OpCodeType.Macro, OperandType.InlineNone, 1, 0xb, StackBehaviour.Pop1, StackBehaviour.Push0, -1 };
            yield return new object[] { OpCodes.Stloc_2, "stloc.2", OpCodeType.Macro, OperandType.InlineNone, 1, 0xc, StackBehaviour.Pop1, StackBehaviour.Push0, -1 };
            yield return new object[] { OpCodes.Stloc_3, "stloc.3", OpCodeType.Macro, OperandType.InlineNone, 1, 0xd, StackBehaviour.Pop1, StackBehaviour.Push0, -1 };
            yield return new object[] { OpCodes.Stloc_S, "stloc.s", OpCodeType.Macro, OperandType.ShortInlineVar, 1, 0x13, StackBehaviour.Pop1, StackBehaviour.Push0, -1 };
            yield return new object[] { OpCodes.Stobj, "stobj", OpCodeType.Primitive, OperandType.InlineType, 1, 0x81, StackBehaviour.Popi_pop1, StackBehaviour.Push0, -2 };
            yield return new object[] { OpCodes.Stsfld, "stsfld", OpCodeType.Objmodel, OperandType.InlineField, 1, 0x80, StackBehaviour.Pop1, StackBehaviour.Push0, -1 };
            yield return new object[] { OpCodes.Sub, "sub", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x59, StackBehaviour.Pop1_pop1, StackBehaviour.Push1, -1 };
            yield return new object[] { OpCodes.Sub_Ovf, "sub.ovf", OpCodeType.Primitive, OperandType.InlineNone, 1, 0xda, StackBehaviour.Pop1_pop1, StackBehaviour.Push1, -1 };
            yield return new object[] { OpCodes.Sub_Ovf_Un, "sub.ovf.un", OpCodeType.Primitive, OperandType.InlineNone, 1, 0xdb, StackBehaviour.Pop1_pop1, StackBehaviour.Push1, -1 };
            yield return new object[] { OpCodes.Switch, "switch", OpCodeType.Primitive, OperandType.InlineSwitch, 1, 0x45, StackBehaviour.Popi, StackBehaviour.Push0, -1 };
            yield return new object[] { OpCodes.Tailcall, "tail.", OpCodeType.Prefix, OperandType.InlineNone, 2, 0xfe14, StackBehaviour.Pop0, StackBehaviour.Push0, 0 };
            yield return new object[] { OpCodes.Throw, "throw", OpCodeType.Objmodel, OperandType.InlineNone, 1, 0x7a, StackBehaviour.Popref, StackBehaviour.Push0, -1 };
            yield return new object[] { OpCodes.Unaligned, "unaligned.", OpCodeType.Prefix, OperandType.ShortInlineI, 2, 0xfe12, StackBehaviour.Pop0, StackBehaviour.Push0, 0 };
            yield return new object[] { OpCodes.Unbox, "unbox", OpCodeType.Primitive, OperandType.InlineType, 1, 0x79, StackBehaviour.Popref, StackBehaviour.Pushi, 0 };
            yield return new object[] { OpCodes.Unbox_Any, "unbox.any", OpCodeType.Objmodel, OperandType.InlineType, 1, 0xa5, StackBehaviour.Popref, StackBehaviour.Push1, 0 };
            yield return new object[] { OpCodes.Volatile, "volatile.", OpCodeType.Prefix, OperandType.InlineNone, 2, 0xfe13, StackBehaviour.Pop0, StackBehaviour.Push0, 0 };
            yield return new object[] { OpCodes.Xor, "xor", OpCodeType.Primitive, OperandType.InlineNone, 1, 0x61, StackBehaviour.Pop1_pop1, StackBehaviour.Push1, -1 };
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void VerifyOpCode(OpCode opCode, string name, OpCodeType opCodeType, OperandType operandType, int size, int value, StackBehaviour stackBehaviourPop, StackBehaviour stackBehaviourPush, int evaluationStackDelta)
        {
            Assert.Equal(name, opCode.Name);
            Assert.Equal(opCodeType, opCode.OpCodeType);
            Assert.Equal(operandType, opCode.OperandType);
            Assert.Equal(size, opCode.Size);
            Assert.Equal((short)value, opCode.Value);
            Assert.Equal(stackBehaviourPop, opCode.StackBehaviourPop);
            Assert.Equal(stackBehaviourPush, opCode.StackBehaviourPush);
            Assert.Equal(evaluationStackDelta, opCode.EvaluationStackDelta);
            Assert.Equal(name, opCode.ToString());

            Assert.Equal(opCode.GetHashCode(), opCode.GetHashCode());

            Assert.True(opCode.Equals(opCode));
            Assert.False(opCode.Equals("OpCode"));
            Assert.False(opCode.Equals(null));
        }

        [Fact]
        public void OpCode_Equals()
        {
            Assert.True(OpCodes.Unaligned.Equals(OpCodes.Unaligned));
            Assert.False(OpCodes.Shr.Equals(OpCodes.Shr_Un));
            Assert.True(OpCodes.Nop.Equals(default(OpCode)));

            Assert.False(OpCodes.Switch == default(OpCode));
            Assert.True(OpCodes.Nop == default(OpCode));

            Assert.True(OpCodes.Sub != OpCodes.Sub_Ovf);
            Assert.False(OpCodes.Nop != default(OpCode));

            Assert.True(OpCodes.Unbox.GetHashCode() != OpCodes.Unbox_Any.GetHashCode());
        }
    }
}
