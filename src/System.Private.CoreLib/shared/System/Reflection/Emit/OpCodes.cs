// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Emit
{
    //
    // Internal enums for opcode values. Note that the value names are used to construct
    // publicly visible ilasm-compatible opcode names, so their exact form is important!
    //
    internal enum OpCodeValues
    {
        Nop = 0x00,
        Break = 0x01,
        Ldarg_0 = 0x02,
        Ldarg_1 = 0x03,
        Ldarg_2 = 0x04,
        Ldarg_3 = 0x05,
        Ldloc_0 = 0x06,
        Ldloc_1 = 0x07,
        Ldloc_2 = 0x08,
        Ldloc_3 = 0x09,
        Stloc_0 = 0x0a,
        Stloc_1 = 0x0b,
        Stloc_2 = 0x0c,
        Stloc_3 = 0x0d,
        Ldarg_S = 0x0e,
        Ldarga_S = 0x0f,
        Starg_S = 0x10,
        Ldloc_S = 0x11,
        Ldloca_S = 0x12,
        Stloc_S = 0x13,
        Ldnull = 0x14,
        Ldc_I4_M1 = 0x15,
        Ldc_I4_0 = 0x16,
        Ldc_I4_1 = 0x17,
        Ldc_I4_2 = 0x18,
        Ldc_I4_3 = 0x19,
        Ldc_I4_4 = 0x1a,
        Ldc_I4_5 = 0x1b,
        Ldc_I4_6 = 0x1c,
        Ldc_I4_7 = 0x1d,
        Ldc_I4_8 = 0x1e,
        Ldc_I4_S = 0x1f,
        Ldc_I4 = 0x20,
        Ldc_I8 = 0x21,
        Ldc_R4 = 0x22,
        Ldc_R8 = 0x23,
        Dup = 0x25,
        Pop = 0x26,
        Jmp = 0x27,
        Call = 0x28,
        Calli = 0x29,
        Ret = 0x2a,
        Br_S = 0x2b,
        Brfalse_S = 0x2c,
        Brtrue_S = 0x2d,
        Beq_S = 0x2e,
        Bge_S = 0x2f,
        Bgt_S = 0x30,
        Ble_S = 0x31,
        Blt_S = 0x32,
        Bne_Un_S = 0x33,
        Bge_Un_S = 0x34,
        Bgt_Un_S = 0x35,
        Ble_Un_S = 0x36,
        Blt_Un_S = 0x37,
        Br = 0x38,
        Brfalse = 0x39,
        Brtrue = 0x3a,
        Beq = 0x3b,
        Bge = 0x3c,
        Bgt = 0x3d,
        Ble = 0x3e,
        Blt = 0x3f,
        Bne_Un = 0x40,
        Bge_Un = 0x41,
        Bgt_Un = 0x42,
        Ble_Un = 0x43,
        Blt_Un = 0x44,
        Switch = 0x45,
        Ldind_I1 = 0x46,
        Ldind_U1 = 0x47,
        Ldind_I2 = 0x48,
        Ldind_U2 = 0x49,
        Ldind_I4 = 0x4a,
        Ldind_U4 = 0x4b,
        Ldind_I8 = 0x4c,
        Ldind_I = 0x4d,
        Ldind_R4 = 0x4e,
        Ldind_R8 = 0x4f,
        Ldind_Ref = 0x50,
        Stind_Ref = 0x51,
        Stind_I1 = 0x52,
        Stind_I2 = 0x53,
        Stind_I4 = 0x54,
        Stind_I8 = 0x55,
        Stind_R4 = 0x56,
        Stind_R8 = 0x57,
        Add = 0x58,
        Sub = 0x59,
        Mul = 0x5a,
        Div = 0x5b,
        Div_Un = 0x5c,
        Rem = 0x5d,
        Rem_Un = 0x5e,
        And = 0x5f,
        Or = 0x60,
        Xor = 0x61,
        Shl = 0x62,
        Shr = 0x63,
        Shr_Un = 0x64,
        Neg = 0x65,
        Not = 0x66,
        Conv_I1 = 0x67,
        Conv_I2 = 0x68,
        Conv_I4 = 0x69,
        Conv_I8 = 0x6a,
        Conv_R4 = 0x6b,
        Conv_R8 = 0x6c,
        Conv_U4 = 0x6d,
        Conv_U8 = 0x6e,
        Callvirt = 0x6f,
        Cpobj = 0x70,
        Ldobj = 0x71,
        Ldstr = 0x72,
        Newobj = 0x73,
        Castclass = 0x74,
        Isinst = 0x75,
        Conv_R_Un = 0x76,
        Unbox = 0x79,
        Throw = 0x7a,
        Ldfld = 0x7b,
        Ldflda = 0x7c,
        Stfld = 0x7d,
        Ldsfld = 0x7e,
        Ldsflda = 0x7f,
        Stsfld = 0x80,
        Stobj = 0x81,
        Conv_Ovf_I1_Un = 0x82,
        Conv_Ovf_I2_Un = 0x83,
        Conv_Ovf_I4_Un = 0x84,
        Conv_Ovf_I8_Un = 0x85,
        Conv_Ovf_U1_Un = 0x86,
        Conv_Ovf_U2_Un = 0x87,
        Conv_Ovf_U4_Un = 0x88,
        Conv_Ovf_U8_Un = 0x89,
        Conv_Ovf_I_Un = 0x8a,
        Conv_Ovf_U_Un = 0x8b,
        Box = 0x8c,
        Newarr = 0x8d,
        Ldlen = 0x8e,
        Ldelema = 0x8f,
        Ldelem_I1 = 0x90,
        Ldelem_U1 = 0x91,
        Ldelem_I2 = 0x92,
        Ldelem_U2 = 0x93,
        Ldelem_I4 = 0x94,
        Ldelem_U4 = 0x95,
        Ldelem_I8 = 0x96,
        Ldelem_I = 0x97,
        Ldelem_R4 = 0x98,
        Ldelem_R8 = 0x99,
        Ldelem_Ref = 0x9a,
        Stelem_I = 0x9b,
        Stelem_I1 = 0x9c,
        Stelem_I2 = 0x9d,
        Stelem_I4 = 0x9e,
        Stelem_I8 = 0x9f,
        Stelem_R4 = 0xa0,
        Stelem_R8 = 0xa1,
        Stelem_Ref = 0xa2,
        Ldelem = 0xa3,
        Stelem = 0xa4,
        Unbox_Any = 0xa5,
        Conv_Ovf_I1 = 0xb3,
        Conv_Ovf_U1 = 0xb4,
        Conv_Ovf_I2 = 0xb5,
        Conv_Ovf_U2 = 0xb6,
        Conv_Ovf_I4 = 0xb7,
        Conv_Ovf_U4 = 0xb8,
        Conv_Ovf_I8 = 0xb9,
        Conv_Ovf_U8 = 0xba,
        Refanyval = 0xc2,
        Ckfinite = 0xc3,
        Mkrefany = 0xc6,
        Ldtoken = 0xd0,
        Conv_U2 = 0xd1,
        Conv_U1 = 0xd2,
        Conv_I = 0xd3,
        Conv_Ovf_I = 0xd4,
        Conv_Ovf_U = 0xd5,
        Add_Ovf = 0xd6,
        Add_Ovf_Un = 0xd7,
        Mul_Ovf = 0xd8,
        Mul_Ovf_Un = 0xd9,
        Sub_Ovf = 0xda,
        Sub_Ovf_Un = 0xdb,
        Endfinally = 0xdc,
        Leave = 0xdd,
        Leave_S = 0xde,
        Stind_I = 0xdf,
        Conv_U = 0xe0,
        Prefix7 = 0xf8,
        Prefix6 = 0xf9,
        Prefix5 = 0xfa,
        Prefix4 = 0xfb,
        Prefix3 = 0xfc,
        Prefix2 = 0xfd,
        Prefix1 = 0xfe,
        Prefixref = 0xff,
        Arglist = 0xfe00,
        Ceq = 0xfe01,
        Cgt = 0xfe02,
        Cgt_Un = 0xfe03,
        Clt = 0xfe04,
        Clt_Un = 0xfe05,
        Ldftn = 0xfe06,
        Ldvirtftn = 0xfe07,
        Ldarg = 0xfe09,
        Ldarga = 0xfe0a,
        Starg = 0xfe0b,
        Ldloc = 0xfe0c,
        Ldloca = 0xfe0d,
        Stloc = 0xfe0e,
        Localloc = 0xfe0f,
        Endfilter = 0xfe11,
        Unaligned_ = 0xfe12,
        Volatile_ = 0xfe13,
        Tail_ = 0xfe14,
        Initobj = 0xfe15,
        Constrained_ = 0xfe16,
        Cpblk = 0xfe17,
        Initblk = 0xfe18,
        Rethrow = 0xfe1a,
        Sizeof = 0xfe1c,
        Refanytype = 0xfe1d,
        Readonly_ = 0xfe1e,
        // If you add more opcodes here, modify OpCode.Name to handle them correctly
    };

    public class OpCodes
    {
        /// <summary>
        ///    <para>
        ///       The IL instruction opcodes supported by the
        ///       runtime. The IL Instruction Specification describes each
        ///       Opcode.
        ///    </para>
        /// </summary>
        /// <seealso topic='IL Instruction Set       Specification'/>

        private OpCodes()
        {
        }

        public static readonly OpCode Nop = new OpCode(OpCodeValues.Nop,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Break = new OpCode(OpCodeValues.Break,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Break << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldarg_0 = new OpCode(OpCodeValues.Ldarg_0,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldarg_1 = new OpCode(OpCodeValues.Ldarg_1,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldarg_2 = new OpCode(OpCodeValues.Ldarg_2,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldarg_3 = new OpCode(OpCodeValues.Ldarg_3,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldloc_0 = new OpCode(OpCodeValues.Ldloc_0,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldloc_1 = new OpCode(OpCodeValues.Ldloc_1,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldloc_2 = new OpCode(OpCodeValues.Ldloc_2,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldloc_3 = new OpCode(OpCodeValues.Ldloc_3,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stloc_0 = new OpCode(OpCodeValues.Stloc_0,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stloc_1 = new OpCode(OpCodeValues.Stloc_1,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stloc_2 = new OpCode(OpCodeValues.Stloc_2,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stloc_3 = new OpCode(OpCodeValues.Stloc_3,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldarg_S = new OpCode(OpCodeValues.Ldarg_S,
            ((int)OperandType.ShortInlineVar) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldarga_S = new OpCode(OpCodeValues.Ldarga_S,
            ((int)OperandType.ShortInlineVar) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Starg_S = new OpCode(OpCodeValues.Starg_S,
            ((int)OperandType.ShortInlineVar) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldloc_S = new OpCode(OpCodeValues.Ldloc_S,
            ((int)OperandType.ShortInlineVar) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldloca_S = new OpCode(OpCodeValues.Ldloca_S,
            ((int)OperandType.ShortInlineVar) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stloc_S = new OpCode(OpCodeValues.Stloc_S,
            ((int)OperandType.ShortInlineVar) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldnull = new OpCode(OpCodeValues.Ldnull,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushref << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldc_I4_M1 = new OpCode(OpCodeValues.Ldc_I4_M1,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldc_I4_0 = new OpCode(OpCodeValues.Ldc_I4_0,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldc_I4_1 = new OpCode(OpCodeValues.Ldc_I4_1,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldc_I4_2 = new OpCode(OpCodeValues.Ldc_I4_2,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldc_I4_3 = new OpCode(OpCodeValues.Ldc_I4_3,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldc_I4_4 = new OpCode(OpCodeValues.Ldc_I4_4,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldc_I4_5 = new OpCode(OpCodeValues.Ldc_I4_5,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldc_I4_6 = new OpCode(OpCodeValues.Ldc_I4_6,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldc_I4_7 = new OpCode(OpCodeValues.Ldc_I4_7,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldc_I4_8 = new OpCode(OpCodeValues.Ldc_I4_8,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldc_I4_S = new OpCode(OpCodeValues.Ldc_I4_S,
            ((int)OperandType.ShortInlineI) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldc_I4 = new OpCode(OpCodeValues.Ldc_I4,
            ((int)OperandType.InlineI) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldc_I8 = new OpCode(OpCodeValues.Ldc_I8,
            ((int)OperandType.InlineI8) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi8 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldc_R4 = new OpCode(OpCodeValues.Ldc_R4,
            ((int)OperandType.ShortInlineR) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushr4 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldc_R8 = new OpCode(OpCodeValues.Ldc_R8,
            ((int)OperandType.InlineR) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushr8 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Dup = new OpCode(OpCodeValues.Dup,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1_push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Pop = new OpCode(OpCodeValues.Pop,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Jmp = new OpCode(OpCodeValues.Jmp,
            ((int)OperandType.InlineMethod) |
            ((int)FlowControl.Call << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            OpCode.EndsUncondJmpBlkFlag |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Call = new OpCode(OpCodeValues.Call,
            ((int)OperandType.InlineMethod) |
            ((int)FlowControl.Call << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Varpop << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Varpush << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Calli = new OpCode(OpCodeValues.Calli,
            ((int)OperandType.InlineSig) |
            ((int)FlowControl.Call << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Varpop << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Varpush << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ret = new OpCode(OpCodeValues.Ret,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Return << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Varpop << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            OpCode.EndsUncondJmpBlkFlag |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Br_S = new OpCode(OpCodeValues.Br_S,
            ((int)OperandType.ShortInlineBrTarget) |
            ((int)FlowControl.Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            OpCode.EndsUncondJmpBlkFlag |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Brfalse_S = new OpCode(OpCodeValues.Brfalse_S,
            ((int)OperandType.ShortInlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Brtrue_S = new OpCode(OpCodeValues.Brtrue_S,
            ((int)OperandType.ShortInlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Beq_S = new OpCode(OpCodeValues.Beq_S,
            ((int)OperandType.ShortInlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Bge_S = new OpCode(OpCodeValues.Bge_S,
            ((int)OperandType.ShortInlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Bgt_S = new OpCode(OpCodeValues.Bgt_S,
            ((int)OperandType.ShortInlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ble_S = new OpCode(OpCodeValues.Ble_S,
            ((int)OperandType.ShortInlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Blt_S = new OpCode(OpCodeValues.Blt_S,
            ((int)OperandType.ShortInlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Bne_Un_S = new OpCode(OpCodeValues.Bne_Un_S,
            ((int)OperandType.ShortInlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Bge_Un_S = new OpCode(OpCodeValues.Bge_Un_S,
            ((int)OperandType.ShortInlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Bgt_Un_S = new OpCode(OpCodeValues.Bgt_Un_S,
            ((int)OperandType.ShortInlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ble_Un_S = new OpCode(OpCodeValues.Ble_Un_S,
            ((int)OperandType.ShortInlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Blt_Un_S = new OpCode(OpCodeValues.Blt_Un_S,
            ((int)OperandType.ShortInlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Br = new OpCode(OpCodeValues.Br,
            ((int)OperandType.InlineBrTarget) |
            ((int)FlowControl.Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            OpCode.EndsUncondJmpBlkFlag |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Brfalse = new OpCode(OpCodeValues.Brfalse,
            ((int)OperandType.InlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Brtrue = new OpCode(OpCodeValues.Brtrue,
            ((int)OperandType.InlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Beq = new OpCode(OpCodeValues.Beq,
            ((int)OperandType.InlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Bge = new OpCode(OpCodeValues.Bge,
            ((int)OperandType.InlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Bgt = new OpCode(OpCodeValues.Bgt,
            ((int)OperandType.InlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ble = new OpCode(OpCodeValues.Ble,
            ((int)OperandType.InlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Blt = new OpCode(OpCodeValues.Blt,
            ((int)OperandType.InlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Bne_Un = new OpCode(OpCodeValues.Bne_Un,
            ((int)OperandType.InlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Bge_Un = new OpCode(OpCodeValues.Bge_Un,
            ((int)OperandType.InlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Bgt_Un = new OpCode(OpCodeValues.Bgt_Un,
            ((int)OperandType.InlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ble_Un = new OpCode(OpCodeValues.Ble_Un,
            ((int)OperandType.InlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Blt_Un = new OpCode(OpCodeValues.Blt_Un,
            ((int)OperandType.InlineBrTarget) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Macro << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Switch = new OpCode(OpCodeValues.Switch,
            ((int)OperandType.InlineSwitch) |
            ((int)FlowControl.Cond_Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldind_I1 = new OpCode(OpCodeValues.Ldind_I1,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldind_U1 = new OpCode(OpCodeValues.Ldind_U1,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldind_I2 = new OpCode(OpCodeValues.Ldind_I2,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldind_U2 = new OpCode(OpCodeValues.Ldind_U2,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldind_I4 = new OpCode(OpCodeValues.Ldind_I4,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldind_U4 = new OpCode(OpCodeValues.Ldind_U4,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldind_I8 = new OpCode(OpCodeValues.Ldind_I8,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi8 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldind_I = new OpCode(OpCodeValues.Ldind_I,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldind_R4 = new OpCode(OpCodeValues.Ldind_R4,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushr4 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldind_R8 = new OpCode(OpCodeValues.Ldind_R8,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushr8 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldind_Ref = new OpCode(OpCodeValues.Ldind_Ref,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushref << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stind_Ref = new OpCode(OpCodeValues.Stind_Ref,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stind_I1 = new OpCode(OpCodeValues.Stind_I1,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stind_I2 = new OpCode(OpCodeValues.Stind_I2,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stind_I4 = new OpCode(OpCodeValues.Stind_I4,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stind_I8 = new OpCode(OpCodeValues.Stind_I8,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi_popi8 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stind_R4 = new OpCode(OpCodeValues.Stind_R4,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi_popr4 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stind_R8 = new OpCode(OpCodeValues.Stind_R8,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi_popr8 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Add = new OpCode(OpCodeValues.Add,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Sub = new OpCode(OpCodeValues.Sub,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Mul = new OpCode(OpCodeValues.Mul,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Div = new OpCode(OpCodeValues.Div,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Div_Un = new OpCode(OpCodeValues.Div_Un,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Rem = new OpCode(OpCodeValues.Rem,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Rem_Un = new OpCode(OpCodeValues.Rem_Un,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode And = new OpCode(OpCodeValues.And,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Or = new OpCode(OpCodeValues.Or,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Xor = new OpCode(OpCodeValues.Xor,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Shl = new OpCode(OpCodeValues.Shl,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Shr = new OpCode(OpCodeValues.Shr,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Shr_Un = new OpCode(OpCodeValues.Shr_Un,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Neg = new OpCode(OpCodeValues.Neg,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Not = new OpCode(OpCodeValues.Not,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_I1 = new OpCode(OpCodeValues.Conv_I1,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_I2 = new OpCode(OpCodeValues.Conv_I2,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_I4 = new OpCode(OpCodeValues.Conv_I4,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_I8 = new OpCode(OpCodeValues.Conv_I8,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi8 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_R4 = new OpCode(OpCodeValues.Conv_R4,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushr4 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_R8 = new OpCode(OpCodeValues.Conv_R8,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushr8 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_U4 = new OpCode(OpCodeValues.Conv_U4,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_U8 = new OpCode(OpCodeValues.Conv_U8,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi8 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Callvirt = new OpCode(OpCodeValues.Callvirt,
            ((int)OperandType.InlineMethod) |
            ((int)FlowControl.Call << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Varpop << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Varpush << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Cpobj = new OpCode(OpCodeValues.Cpobj,
            ((int)OperandType.InlineType) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldobj = new OpCode(OpCodeValues.Ldobj,
            ((int)OperandType.InlineType) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldstr = new OpCode(OpCodeValues.Ldstr,
            ((int)OperandType.InlineString) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushref << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Newobj = new OpCode(OpCodeValues.Newobj,
            ((int)OperandType.InlineMethod) |
            ((int)FlowControl.Call << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Varpop << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushref << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Castclass = new OpCode(OpCodeValues.Castclass,
            ((int)OperandType.InlineType) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushref << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Isinst = new OpCode(OpCodeValues.Isinst,
            ((int)OperandType.InlineType) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_R_Un = new OpCode(OpCodeValues.Conv_R_Un,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushr8 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Unbox = new OpCode(OpCodeValues.Unbox,
            ((int)OperandType.InlineType) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Throw = new OpCode(OpCodeValues.Throw,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Throw << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            OpCode.EndsUncondJmpBlkFlag |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldfld = new OpCode(OpCodeValues.Ldfld,
            ((int)OperandType.InlineField) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldflda = new OpCode(OpCodeValues.Ldflda,
            ((int)OperandType.InlineField) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stfld = new OpCode(OpCodeValues.Stfld,
            ((int)OperandType.InlineField) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldsfld = new OpCode(OpCodeValues.Ldsfld,
            ((int)OperandType.InlineField) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldsflda = new OpCode(OpCodeValues.Ldsflda,
            ((int)OperandType.InlineField) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stsfld = new OpCode(OpCodeValues.Stsfld,
            ((int)OperandType.InlineField) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stobj = new OpCode(OpCodeValues.Stobj,
            ((int)OperandType.InlineType) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_Ovf_I1_Un = new OpCode(OpCodeValues.Conv_Ovf_I1_Un,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_Ovf_I2_Un = new OpCode(OpCodeValues.Conv_Ovf_I2_Un,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_Ovf_I4_Un = new OpCode(OpCodeValues.Conv_Ovf_I4_Un,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_Ovf_I8_Un = new OpCode(OpCodeValues.Conv_Ovf_I8_Un,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi8 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_Ovf_U1_Un = new OpCode(OpCodeValues.Conv_Ovf_U1_Un,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_Ovf_U2_Un = new OpCode(OpCodeValues.Conv_Ovf_U2_Un,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_Ovf_U4_Un = new OpCode(OpCodeValues.Conv_Ovf_U4_Un,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_Ovf_U8_Un = new OpCode(OpCodeValues.Conv_Ovf_U8_Un,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi8 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_Ovf_I_Un = new OpCode(OpCodeValues.Conv_Ovf_I_Un,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_Ovf_U_Un = new OpCode(OpCodeValues.Conv_Ovf_U_Un,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Box = new OpCode(OpCodeValues.Box,
            ((int)OperandType.InlineType) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushref << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Newarr = new OpCode(OpCodeValues.Newarr,
            ((int)OperandType.InlineType) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushref << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldlen = new OpCode(OpCodeValues.Ldlen,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldelema = new OpCode(OpCodeValues.Ldelema,
            ((int)OperandType.InlineType) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldelem_I1 = new OpCode(OpCodeValues.Ldelem_I1,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldelem_U1 = new OpCode(OpCodeValues.Ldelem_U1,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldelem_I2 = new OpCode(OpCodeValues.Ldelem_I2,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldelem_U2 = new OpCode(OpCodeValues.Ldelem_U2,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldelem_I4 = new OpCode(OpCodeValues.Ldelem_I4,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldelem_U4 = new OpCode(OpCodeValues.Ldelem_U4,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldelem_I8 = new OpCode(OpCodeValues.Ldelem_I8,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi8 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldelem_I = new OpCode(OpCodeValues.Ldelem_I,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldelem_R4 = new OpCode(OpCodeValues.Ldelem_R4,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushr4 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldelem_R8 = new OpCode(OpCodeValues.Ldelem_R8,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushr8 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldelem_Ref = new OpCode(OpCodeValues.Ldelem_Ref,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushref << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stelem_I = new OpCode(OpCodeValues.Stelem_I,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref_popi_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-3 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stelem_I1 = new OpCode(OpCodeValues.Stelem_I1,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref_popi_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-3 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stelem_I2 = new OpCode(OpCodeValues.Stelem_I2,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref_popi_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-3 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stelem_I4 = new OpCode(OpCodeValues.Stelem_I4,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref_popi_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-3 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stelem_I8 = new OpCode(OpCodeValues.Stelem_I8,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref_popi_popi8 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-3 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stelem_R4 = new OpCode(OpCodeValues.Stelem_R4,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref_popi_popr4 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-3 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stelem_R8 = new OpCode(OpCodeValues.Stelem_R8,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref_popi_popr8 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-3 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stelem_Ref = new OpCode(OpCodeValues.Stelem_Ref,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref_popi_popref << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-3 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldelem = new OpCode(OpCodeValues.Ldelem,
            ((int)OperandType.InlineType) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stelem = new OpCode(OpCodeValues.Stelem,
            ((int)OperandType.InlineType) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref_popi_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Unbox_Any = new OpCode(OpCodeValues.Unbox_Any,
            ((int)OperandType.InlineType) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_Ovf_I1 = new OpCode(OpCodeValues.Conv_Ovf_I1,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_Ovf_U1 = new OpCode(OpCodeValues.Conv_Ovf_U1,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_Ovf_I2 = new OpCode(OpCodeValues.Conv_Ovf_I2,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_Ovf_U2 = new OpCode(OpCodeValues.Conv_Ovf_U2,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_Ovf_I4 = new OpCode(OpCodeValues.Conv_Ovf_I4,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_Ovf_U4 = new OpCode(OpCodeValues.Conv_Ovf_U4,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_Ovf_I8 = new OpCode(OpCodeValues.Conv_Ovf_I8,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi8 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_Ovf_U8 = new OpCode(OpCodeValues.Conv_Ovf_U8,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi8 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Refanyval = new OpCode(OpCodeValues.Refanyval,
            ((int)OperandType.InlineType) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ckfinite = new OpCode(OpCodeValues.Ckfinite,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushr8 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Mkrefany = new OpCode(OpCodeValues.Mkrefany,
            ((int)OperandType.InlineType) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldtoken = new OpCode(OpCodeValues.Ldtoken,
            ((int)OperandType.InlineTok) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_U2 = new OpCode(OpCodeValues.Conv_U2,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_U1 = new OpCode(OpCodeValues.Conv_U1,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_I = new OpCode(OpCodeValues.Conv_I,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_Ovf_I = new OpCode(OpCodeValues.Conv_Ovf_I,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_Ovf_U = new OpCode(OpCodeValues.Conv_Ovf_U,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Add_Ovf = new OpCode(OpCodeValues.Add_Ovf,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Add_Ovf_Un = new OpCode(OpCodeValues.Add_Ovf_Un,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Mul_Ovf = new OpCode(OpCodeValues.Mul_Ovf,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Mul_Ovf_Un = new OpCode(OpCodeValues.Mul_Ovf_Un,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Sub_Ovf = new OpCode(OpCodeValues.Sub_Ovf,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Sub_Ovf_Un = new OpCode(OpCodeValues.Sub_Ovf_Un,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Endfinally = new OpCode(OpCodeValues.Endfinally,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Return << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            OpCode.EndsUncondJmpBlkFlag |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Leave = new OpCode(OpCodeValues.Leave,
            ((int)OperandType.InlineBrTarget) |
            ((int)FlowControl.Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            OpCode.EndsUncondJmpBlkFlag |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Leave_S = new OpCode(OpCodeValues.Leave_S,
            ((int)OperandType.ShortInlineBrTarget) |
            ((int)FlowControl.Branch << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            OpCode.EndsUncondJmpBlkFlag |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stind_I = new OpCode(OpCodeValues.Stind_I,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (-2 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Conv_U = new OpCode(OpCodeValues.Conv_U,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Prefix7 = new OpCode(OpCodeValues.Prefix7,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Meta << OpCode.FlowControlShift) |
            ((int)OpCodeType.Nternal << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Prefix6 = new OpCode(OpCodeValues.Prefix6,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Meta << OpCode.FlowControlShift) |
            ((int)OpCodeType.Nternal << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Prefix5 = new OpCode(OpCodeValues.Prefix5,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Meta << OpCode.FlowControlShift) |
            ((int)OpCodeType.Nternal << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Prefix4 = new OpCode(OpCodeValues.Prefix4,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Meta << OpCode.FlowControlShift) |
            ((int)OpCodeType.Nternal << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Prefix3 = new OpCode(OpCodeValues.Prefix3,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Meta << OpCode.FlowControlShift) |
            ((int)OpCodeType.Nternal << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Prefix2 = new OpCode(OpCodeValues.Prefix2,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Meta << OpCode.FlowControlShift) |
            ((int)OpCodeType.Nternal << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Prefix1 = new OpCode(OpCodeValues.Prefix1,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Meta << OpCode.FlowControlShift) |
            ((int)OpCodeType.Nternal << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Prefixref = new OpCode(OpCodeValues.Prefixref,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Meta << OpCode.FlowControlShift) |
            ((int)OpCodeType.Nternal << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (1 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Arglist = new OpCode(OpCodeValues.Arglist,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ceq = new OpCode(OpCodeValues.Ceq,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Cgt = new OpCode(OpCodeValues.Cgt,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Cgt_Un = new OpCode(OpCodeValues.Cgt_Un,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Clt = new OpCode(OpCodeValues.Clt,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Clt_Un = new OpCode(OpCodeValues.Clt_Un,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1_pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldftn = new OpCode(OpCodeValues.Ldftn,
            ((int)OperandType.InlineMethod) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldvirtftn = new OpCode(OpCodeValues.Ldvirtftn,
            ((int)OperandType.InlineMethod) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popref << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldarg = new OpCode(OpCodeValues.Ldarg,
            ((int)OperandType.InlineVar) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldarga = new OpCode(OpCodeValues.Ldarga,
            ((int)OperandType.InlineVar) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Starg = new OpCode(OpCodeValues.Starg,
            ((int)OperandType.InlineVar) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldloc = new OpCode(OpCodeValues.Ldloc,
            ((int)OperandType.InlineVar) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push1 << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Ldloca = new OpCode(OpCodeValues.Ldloca,
            ((int)OperandType.InlineVar) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Stloc = new OpCode(OpCodeValues.Stloc,
            ((int)OperandType.InlineVar) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Localloc = new OpCode(OpCodeValues.Localloc,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Endfilter = new OpCode(OpCodeValues.Endfilter,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Return << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            OpCode.EndsUncondJmpBlkFlag |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Unaligned = new OpCode(OpCodeValues.Unaligned_,
            ((int)OperandType.ShortInlineI) |
            ((int)FlowControl.Meta << OpCode.FlowControlShift) |
            ((int)OpCodeType.Prefix << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Volatile = new OpCode(OpCodeValues.Volatile_,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Meta << OpCode.FlowControlShift) |
            ((int)OpCodeType.Prefix << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Tailcall = new OpCode(OpCodeValues.Tail_,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Meta << OpCode.FlowControlShift) |
            ((int)OpCodeType.Prefix << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Initobj = new OpCode(OpCodeValues.Initobj,
            ((int)OperandType.InlineType) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (-1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Constrained = new OpCode(OpCodeValues.Constrained_,
            ((int)OperandType.InlineType) |
            ((int)FlowControl.Meta << OpCode.FlowControlShift) |
            ((int)OpCodeType.Prefix << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Cpblk = new OpCode(OpCodeValues.Cpblk,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi_popi_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (-3 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Initblk = new OpCode(OpCodeValues.Initblk,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Popi_popi_popi << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (-3 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Rethrow = new OpCode(OpCodeValues.Rethrow,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Throw << OpCode.FlowControlShift) |
            ((int)OpCodeType.Objmodel << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            OpCode.EndsUncondJmpBlkFlag |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Sizeof = new OpCode(OpCodeValues.Sizeof,
            ((int)OperandType.InlineType) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (1 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Refanytype = new OpCode(OpCodeValues.Refanytype,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Next << OpCode.FlowControlShift) |
            ((int)OpCodeType.Primitive << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop1 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Pushi << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );

        public static readonly OpCode Readonly = new OpCode(OpCodeValues.Readonly_,
            ((int)OperandType.InlineNone) |
            ((int)FlowControl.Meta << OpCode.FlowControlShift) |
            ((int)OpCodeType.Prefix << OpCode.OpCodeTypeShift) |
            ((int)StackBehaviour.Pop0 << OpCode.StackBehaviourPopShift) |
            ((int)StackBehaviour.Push0 << OpCode.StackBehaviourPushShift) |
            (2 << OpCode.SizeShift) |
            (0 << OpCode.StackChangeShift)
        );


        public static bool TakesSingleByteArgument(OpCode inst)
        {
            switch (inst.OperandType)
            {
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    return true;
            };
            return false;
        }
    }
}
