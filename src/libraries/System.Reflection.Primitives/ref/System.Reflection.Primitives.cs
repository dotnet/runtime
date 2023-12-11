// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Reflection.Emit
{
    public enum FlowControl
    {
        Branch = 0,
        Break = 1,
        Call = 2,
        Cond_Branch = 3,
        Meta = 4,
        Next = 5,
        [System.ObsoleteAttribute("FlowControl.Phi has been deprecated and is not supported.")]
        Phi = 6,
        Return = 7,
        Throw = 8,
    }
    public readonly partial struct OpCode : System.IEquatable<System.Reflection.Emit.OpCode>
    {
        private readonly int _dummyPrimitive;
        public int EvaluationStackDelta { get { throw null; } }
        public System.Reflection.Emit.FlowControl FlowControl { get { throw null; } }
        public string? Name { get { throw null; } }
        public System.Reflection.Emit.OpCodeType OpCodeType { get { throw null; } }
        public System.Reflection.Emit.OperandType OperandType { get { throw null; } }
        public int Size { get { throw null; } }
        public System.Reflection.Emit.StackBehaviour StackBehaviourPop { get { throw null; } }
        public System.Reflection.Emit.StackBehaviour StackBehaviourPush { get { throw null; } }
        public short Value { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public bool Equals(System.Reflection.Emit.OpCode obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Reflection.Emit.OpCode a, System.Reflection.Emit.OpCode b) { throw null; }
        public static bool operator !=(System.Reflection.Emit.OpCode a, System.Reflection.Emit.OpCode b) { throw null; }
        public override string? ToString() { throw null; }
    }
    public partial class OpCodes
    {
        internal OpCodes() { }
        public static readonly System.Reflection.Emit.OpCode Add;
        public static readonly System.Reflection.Emit.OpCode Add_Ovf;
        public static readonly System.Reflection.Emit.OpCode Add_Ovf_Un;
        public static readonly System.Reflection.Emit.OpCode And;
        public static readonly System.Reflection.Emit.OpCode Arglist;
        public static readonly System.Reflection.Emit.OpCode Beq;
        public static readonly System.Reflection.Emit.OpCode Beq_S;
        public static readonly System.Reflection.Emit.OpCode Bge;
        public static readonly System.Reflection.Emit.OpCode Bge_S;
        public static readonly System.Reflection.Emit.OpCode Bge_Un;
        public static readonly System.Reflection.Emit.OpCode Bge_Un_S;
        public static readonly System.Reflection.Emit.OpCode Bgt;
        public static readonly System.Reflection.Emit.OpCode Bgt_S;
        public static readonly System.Reflection.Emit.OpCode Bgt_Un;
        public static readonly System.Reflection.Emit.OpCode Bgt_Un_S;
        public static readonly System.Reflection.Emit.OpCode Ble;
        public static readonly System.Reflection.Emit.OpCode Ble_S;
        public static readonly System.Reflection.Emit.OpCode Ble_Un;
        public static readonly System.Reflection.Emit.OpCode Ble_Un_S;
        public static readonly System.Reflection.Emit.OpCode Blt;
        public static readonly System.Reflection.Emit.OpCode Blt_S;
        public static readonly System.Reflection.Emit.OpCode Blt_Un;
        public static readonly System.Reflection.Emit.OpCode Blt_Un_S;
        public static readonly System.Reflection.Emit.OpCode Bne_Un;
        public static readonly System.Reflection.Emit.OpCode Bne_Un_S;
        public static readonly System.Reflection.Emit.OpCode Box;
        public static readonly System.Reflection.Emit.OpCode Br;
        public static readonly System.Reflection.Emit.OpCode Break;
        public static readonly System.Reflection.Emit.OpCode Brfalse;
        public static readonly System.Reflection.Emit.OpCode Brfalse_S;
        public static readonly System.Reflection.Emit.OpCode Brtrue;
        public static readonly System.Reflection.Emit.OpCode Brtrue_S;
        public static readonly System.Reflection.Emit.OpCode Br_S;
        public static readonly System.Reflection.Emit.OpCode Call;
        public static readonly System.Reflection.Emit.OpCode Calli;
        public static readonly System.Reflection.Emit.OpCode Callvirt;
        public static readonly System.Reflection.Emit.OpCode Castclass;
        public static readonly System.Reflection.Emit.OpCode Ceq;
        public static readonly System.Reflection.Emit.OpCode Cgt;
        public static readonly System.Reflection.Emit.OpCode Cgt_Un;
        public static readonly System.Reflection.Emit.OpCode Ckfinite;
        public static readonly System.Reflection.Emit.OpCode Clt;
        public static readonly System.Reflection.Emit.OpCode Clt_Un;
        public static readonly System.Reflection.Emit.OpCode Constrained;
        public static readonly System.Reflection.Emit.OpCode Conv_I;
        public static readonly System.Reflection.Emit.OpCode Conv_I1;
        public static readonly System.Reflection.Emit.OpCode Conv_I2;
        public static readonly System.Reflection.Emit.OpCode Conv_I4;
        public static readonly System.Reflection.Emit.OpCode Conv_I8;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I1;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I1_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I2;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I2_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I4;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I4_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I8;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I8_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U1;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U1_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U2;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U2_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U4;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U4_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U8;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U8_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_R4;
        public static readonly System.Reflection.Emit.OpCode Conv_R8;
        public static readonly System.Reflection.Emit.OpCode Conv_R_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_U;
        public static readonly System.Reflection.Emit.OpCode Conv_U1;
        public static readonly System.Reflection.Emit.OpCode Conv_U2;
        public static readonly System.Reflection.Emit.OpCode Conv_U4;
        public static readonly System.Reflection.Emit.OpCode Conv_U8;
        public static readonly System.Reflection.Emit.OpCode Cpblk;
        public static readonly System.Reflection.Emit.OpCode Cpobj;
        public static readonly System.Reflection.Emit.OpCode Div;
        public static readonly System.Reflection.Emit.OpCode Div_Un;
        public static readonly System.Reflection.Emit.OpCode Dup;
        public static readonly System.Reflection.Emit.OpCode Endfilter;
        public static readonly System.Reflection.Emit.OpCode Endfinally;
        public static readonly System.Reflection.Emit.OpCode Initblk;
        public static readonly System.Reflection.Emit.OpCode Initobj;
        public static readonly System.Reflection.Emit.OpCode Isinst;
        public static readonly System.Reflection.Emit.OpCode Jmp;
        public static readonly System.Reflection.Emit.OpCode Ldarg;
        public static readonly System.Reflection.Emit.OpCode Ldarga;
        public static readonly System.Reflection.Emit.OpCode Ldarga_S;
        public static readonly System.Reflection.Emit.OpCode Ldarg_0;
        public static readonly System.Reflection.Emit.OpCode Ldarg_1;
        public static readonly System.Reflection.Emit.OpCode Ldarg_2;
        public static readonly System.Reflection.Emit.OpCode Ldarg_3;
        public static readonly System.Reflection.Emit.OpCode Ldarg_S;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4_0;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4_1;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4_2;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4_3;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4_4;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4_5;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4_6;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4_7;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4_8;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4_M1;
        public static readonly System.Reflection.Emit.OpCode Ldc_I4_S;
        public static readonly System.Reflection.Emit.OpCode Ldc_I8;
        public static readonly System.Reflection.Emit.OpCode Ldc_R4;
        public static readonly System.Reflection.Emit.OpCode Ldc_R8;
        public static readonly System.Reflection.Emit.OpCode Ldelem;
        public static readonly System.Reflection.Emit.OpCode Ldelema;
        public static readonly System.Reflection.Emit.OpCode Ldelem_I;
        public static readonly System.Reflection.Emit.OpCode Ldelem_I1;
        public static readonly System.Reflection.Emit.OpCode Ldelem_I2;
        public static readonly System.Reflection.Emit.OpCode Ldelem_I4;
        public static readonly System.Reflection.Emit.OpCode Ldelem_I8;
        public static readonly System.Reflection.Emit.OpCode Ldelem_R4;
        public static readonly System.Reflection.Emit.OpCode Ldelem_R8;
        public static readonly System.Reflection.Emit.OpCode Ldelem_Ref;
        public static readonly System.Reflection.Emit.OpCode Ldelem_U1;
        public static readonly System.Reflection.Emit.OpCode Ldelem_U2;
        public static readonly System.Reflection.Emit.OpCode Ldelem_U4;
        public static readonly System.Reflection.Emit.OpCode Ldfld;
        public static readonly System.Reflection.Emit.OpCode Ldflda;
        public static readonly System.Reflection.Emit.OpCode Ldftn;
        public static readonly System.Reflection.Emit.OpCode Ldind_I;
        public static readonly System.Reflection.Emit.OpCode Ldind_I1;
        public static readonly System.Reflection.Emit.OpCode Ldind_I2;
        public static readonly System.Reflection.Emit.OpCode Ldind_I4;
        public static readonly System.Reflection.Emit.OpCode Ldind_I8;
        public static readonly System.Reflection.Emit.OpCode Ldind_R4;
        public static readonly System.Reflection.Emit.OpCode Ldind_R8;
        public static readonly System.Reflection.Emit.OpCode Ldind_Ref;
        public static readonly System.Reflection.Emit.OpCode Ldind_U1;
        public static readonly System.Reflection.Emit.OpCode Ldind_U2;
        public static readonly System.Reflection.Emit.OpCode Ldind_U4;
        public static readonly System.Reflection.Emit.OpCode Ldlen;
        public static readonly System.Reflection.Emit.OpCode Ldloc;
        public static readonly System.Reflection.Emit.OpCode Ldloca;
        public static readonly System.Reflection.Emit.OpCode Ldloca_S;
        public static readonly System.Reflection.Emit.OpCode Ldloc_0;
        public static readonly System.Reflection.Emit.OpCode Ldloc_1;
        public static readonly System.Reflection.Emit.OpCode Ldloc_2;
        public static readonly System.Reflection.Emit.OpCode Ldloc_3;
        public static readonly System.Reflection.Emit.OpCode Ldloc_S;
        public static readonly System.Reflection.Emit.OpCode Ldnull;
        public static readonly System.Reflection.Emit.OpCode Ldobj;
        public static readonly System.Reflection.Emit.OpCode Ldsfld;
        public static readonly System.Reflection.Emit.OpCode Ldsflda;
        public static readonly System.Reflection.Emit.OpCode Ldstr;
        public static readonly System.Reflection.Emit.OpCode Ldtoken;
        public static readonly System.Reflection.Emit.OpCode Ldvirtftn;
        public static readonly System.Reflection.Emit.OpCode Leave;
        public static readonly System.Reflection.Emit.OpCode Leave_S;
        public static readonly System.Reflection.Emit.OpCode Localloc;
        public static readonly System.Reflection.Emit.OpCode Mkrefany;
        public static readonly System.Reflection.Emit.OpCode Mul;
        public static readonly System.Reflection.Emit.OpCode Mul_Ovf;
        public static readonly System.Reflection.Emit.OpCode Mul_Ovf_Un;
        public static readonly System.Reflection.Emit.OpCode Neg;
        public static readonly System.Reflection.Emit.OpCode Newarr;
        public static readonly System.Reflection.Emit.OpCode Newobj;
        public static readonly System.Reflection.Emit.OpCode Nop;
        public static readonly System.Reflection.Emit.OpCode Not;
        public static readonly System.Reflection.Emit.OpCode Or;
        public static readonly System.Reflection.Emit.OpCode Pop;
        public static readonly System.Reflection.Emit.OpCode Prefix1;
        public static readonly System.Reflection.Emit.OpCode Prefix2;
        public static readonly System.Reflection.Emit.OpCode Prefix3;
        public static readonly System.Reflection.Emit.OpCode Prefix4;
        public static readonly System.Reflection.Emit.OpCode Prefix5;
        public static readonly System.Reflection.Emit.OpCode Prefix6;
        public static readonly System.Reflection.Emit.OpCode Prefix7;
        public static readonly System.Reflection.Emit.OpCode Prefixref;
        public static readonly System.Reflection.Emit.OpCode Readonly;
        public static readonly System.Reflection.Emit.OpCode Refanytype;
        public static readonly System.Reflection.Emit.OpCode Refanyval;
        public static readonly System.Reflection.Emit.OpCode Rem;
        public static readonly System.Reflection.Emit.OpCode Rem_Un;
        public static readonly System.Reflection.Emit.OpCode Ret;
        public static readonly System.Reflection.Emit.OpCode Rethrow;
        public static readonly System.Reflection.Emit.OpCode Shl;
        public static readonly System.Reflection.Emit.OpCode Shr;
        public static readonly System.Reflection.Emit.OpCode Shr_Un;
        public static readonly System.Reflection.Emit.OpCode Sizeof;
        public static readonly System.Reflection.Emit.OpCode Starg;
        public static readonly System.Reflection.Emit.OpCode Starg_S;
        public static readonly System.Reflection.Emit.OpCode Stelem;
        public static readonly System.Reflection.Emit.OpCode Stelem_I;
        public static readonly System.Reflection.Emit.OpCode Stelem_I1;
        public static readonly System.Reflection.Emit.OpCode Stelem_I2;
        public static readonly System.Reflection.Emit.OpCode Stelem_I4;
        public static readonly System.Reflection.Emit.OpCode Stelem_I8;
        public static readonly System.Reflection.Emit.OpCode Stelem_R4;
        public static readonly System.Reflection.Emit.OpCode Stelem_R8;
        public static readonly System.Reflection.Emit.OpCode Stelem_Ref;
        public static readonly System.Reflection.Emit.OpCode Stfld;
        public static readonly System.Reflection.Emit.OpCode Stind_I;
        public static readonly System.Reflection.Emit.OpCode Stind_I1;
        public static readonly System.Reflection.Emit.OpCode Stind_I2;
        public static readonly System.Reflection.Emit.OpCode Stind_I4;
        public static readonly System.Reflection.Emit.OpCode Stind_I8;
        public static readonly System.Reflection.Emit.OpCode Stind_R4;
        public static readonly System.Reflection.Emit.OpCode Stind_R8;
        public static readonly System.Reflection.Emit.OpCode Stind_Ref;
        public static readonly System.Reflection.Emit.OpCode Stloc;
        public static readonly System.Reflection.Emit.OpCode Stloc_0;
        public static readonly System.Reflection.Emit.OpCode Stloc_1;
        public static readonly System.Reflection.Emit.OpCode Stloc_2;
        public static readonly System.Reflection.Emit.OpCode Stloc_3;
        public static readonly System.Reflection.Emit.OpCode Stloc_S;
        public static readonly System.Reflection.Emit.OpCode Stobj;
        public static readonly System.Reflection.Emit.OpCode Stsfld;
        public static readonly System.Reflection.Emit.OpCode Sub;
        public static readonly System.Reflection.Emit.OpCode Sub_Ovf;
        public static readonly System.Reflection.Emit.OpCode Sub_Ovf_Un;
        public static readonly System.Reflection.Emit.OpCode Switch;
        public static readonly System.Reflection.Emit.OpCode Tailcall;
        public static readonly System.Reflection.Emit.OpCode Throw;
        public static readonly System.Reflection.Emit.OpCode Unaligned;
        public static readonly System.Reflection.Emit.OpCode Unbox;
        public static readonly System.Reflection.Emit.OpCode Unbox_Any;
        public static readonly System.Reflection.Emit.OpCode Volatile;
        public static readonly System.Reflection.Emit.OpCode Xor;
        public static bool TakesSingleByteArgument(System.Reflection.Emit.OpCode inst) { throw null; }
    }
    public enum OpCodeType
    {
        [System.ObsoleteAttribute("OpCodeType.Annotation has been deprecated and is not supported.")]
        Annotation = 0,
        Macro = 1,
        Nternal = 2,
        Objmodel = 3,
        Prefix = 4,
        Primitive = 5,
    }
    public enum OperandType
    {
        InlineBrTarget = 0,
        InlineField = 1,
        InlineI = 2,
        InlineI8 = 3,
        InlineMethod = 4,
        InlineNone = 5,
        [System.ObsoleteAttribute("OperandType.InlinePhi has been deprecated and is not supported.")]
        InlinePhi = 6,
        InlineR = 7,
        InlineSig = 9,
        InlineString = 10,
        InlineSwitch = 11,
        InlineTok = 12,
        InlineType = 13,
        InlineVar = 14,
        ShortInlineBrTarget = 15,
        ShortInlineI = 16,
        ShortInlineR = 17,
        ShortInlineVar = 18,
    }
    public enum PackingSize
    {
        Unspecified = 0,
        Size1 = 1,
        Size2 = 2,
        Size4 = 4,
        Size8 = 8,
        Size16 = 16,
        Size32 = 32,
        Size64 = 64,
        Size128 = 128,
    }
    public enum StackBehaviour
    {
        Pop0 = 0,
        Pop1 = 1,
        Pop1_pop1 = 2,
        Popi = 3,
        Popi_pop1 = 4,
        Popi_popi = 5,
        Popi_popi8 = 6,
        Popi_popi_popi = 7,
        Popi_popr4 = 8,
        Popi_popr8 = 9,
        Popref = 10,
        Popref_pop1 = 11,
        Popref_popi = 12,
        Popref_popi_popi = 13,
        Popref_popi_popi8 = 14,
        Popref_popi_popr4 = 15,
        Popref_popi_popr8 = 16,
        Popref_popi_popref = 17,
        Push0 = 18,
        Push1 = 19,
        Push1_push1 = 20,
        Pushi = 21,
        Pushi8 = 22,
        Pushr4 = 23,
        Pushr8 = 24,
        Pushref = 25,
        Varpop = 26,
        Varpush = 27,
        Popref_popi_pop1 = 28,
    }
}
