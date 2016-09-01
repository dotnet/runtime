// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Reflection
{
    [System.FlagsAttribute]
    public enum CallingConventions
    {
        Any = 3,
        ExplicitThis = 64,
        HasThis = 32,
        Standard = 1,
        VarArgs = 2,
    }
    [System.FlagsAttribute]
    public enum EventAttributes
    {
        None = 0,
        RTSpecialName = 1024,
        SpecialName = 512,
    }
    [System.FlagsAttribute]
    public enum FieldAttributes
    {
        Assembly = 3,
        FamANDAssem = 2,
        Family = 4,
        FamORAssem = 5,
        FieldAccessMask = 7,
        HasDefault = 32768,
        HasFieldMarshal = 4096,
        HasFieldRVA = 256,
        InitOnly = 32,
        Literal = 64,
        NotSerialized = 128,
        PinvokeImpl = 8192,
        Private = 1,
        PrivateScope = 0,
        Public = 6,
        RTSpecialName = 1024,
        SpecialName = 512,
        Static = 16,
    }
    [System.FlagsAttribute]
    public enum GenericParameterAttributes
    {
        Contravariant = 2,
        Covariant = 1,
        DefaultConstructorConstraint = 16,
        None = 0,
        NotNullableValueTypeConstraint = 8,
        ReferenceTypeConstraint = 4,
        SpecialConstraintMask = 28,
        VarianceMask = 3,
    }
    [System.FlagsAttribute]
    public enum MethodAttributes
    {
        Abstract = 1024,
        Assembly = 3,
        CheckAccessOnOverride = 512,
        FamANDAssem = 2,
        Family = 4,
        FamORAssem = 5,
        Final = 32,
        HasSecurity = 16384,
        HideBySig = 128,
        MemberAccessMask = 7,
        NewSlot = 256,
        PinvokeImpl = 8192,
        Private = 1,
        PrivateScope = 0,
        Public = 6,
        RequireSecObject = 32768,
        ReuseSlot = 0,
        RTSpecialName = 4096,
        SpecialName = 2048,
        Static = 16,
        UnmanagedExport = 8,
        Virtual = 64,
        VtableLayoutMask = 256,
    }
    public enum MethodImplAttributes
    {
        AggressiveInlining = 256,
        CodeTypeMask = 3,
        ForwardRef = 16,
        IL = 0,
        InternalCall = 4096,
        Managed = 0,
        ManagedMask = 4,
        Native = 1,
        NoInlining = 8,
        NoOptimization = 64,
        OPTIL = 2,
        PreserveSig = 128,
        Runtime = 3,
        Synchronized = 32,
        Unmanaged = 4,
    }
    [System.FlagsAttribute]
    public enum ParameterAttributes
    {
        HasDefault = 4096,
        HasFieldMarshal = 8192,
        In = 1,
        Lcid = 4,
        None = 0,
        Optional = 16,
        Out = 2,
        Retval = 8,
    }
    [System.FlagsAttribute]
    public enum PropertyAttributes
    {
        HasDefault = 4096,
        None = 0,
        RTSpecialName = 1024,
        SpecialName = 512,
    }
    [System.FlagsAttribute]
    public enum TypeAttributes
    {
        Abstract = 128,
        AnsiClass = 0,
        AutoClass = 131072,
        AutoLayout = 0,
        BeforeFieldInit = 1048576,
        Class = 0,
        ClassSemanticsMask = 32,
        CustomFormatClass = 196608,
        CustomFormatMask = 12582912,
        ExplicitLayout = 16,
        HasSecurity = 262144,
        Import = 4096,
        Interface = 32,
        LayoutMask = 24,
        NestedAssembly = 5,
        NestedFamANDAssem = 6,
        NestedFamily = 4,
        NestedFamORAssem = 7,
        NestedPrivate = 3,
        NestedPublic = 2,
        NotPublic = 0,
        Public = 1,
        RTSpecialName = 2048,
        Sealed = 256,
        SequentialLayout = 8,
        Serializable = 8192,
        SpecialName = 1024,
        StringFormatMask = 196608,
        UnicodeClass = 65536,
        VisibilityMask = 7,
        WindowsRuntime = 16384,
    }
}
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
        Return = 7,
        Throw = 8,
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct OpCode
    {
        public System.Reflection.Emit.FlowControl FlowControl { get { return default(System.Reflection.Emit.FlowControl); } }
        public string Name { get { return default(string); } }
        public System.Reflection.Emit.OpCodeType OpCodeType { get { return default(System.Reflection.Emit.OpCodeType); } }
        public System.Reflection.Emit.OperandType OperandType { get { return default(System.Reflection.Emit.OperandType); } }
        public int Size { get { return default(int); } }
        public System.Reflection.Emit.StackBehaviour StackBehaviourPop { get { return default(System.Reflection.Emit.StackBehaviour); } }
        public System.Reflection.Emit.StackBehaviour StackBehaviourPush { get { return default(System.Reflection.Emit.StackBehaviour); } }
        public short Value { get { return default(short); } }
        public override bool Equals(object obj) { return default(bool); }
        public bool Equals(System.Reflection.Emit.OpCode obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Reflection.Emit.OpCode a, System.Reflection.Emit.OpCode b) { return default(bool); }
        public static bool operator !=(System.Reflection.Emit.OpCode a, System.Reflection.Emit.OpCode b) { return default(bool); }
        public override string ToString() { return default(string); }
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
        public static readonly System.Reflection.Emit.OpCode Br_S;
        public static readonly System.Reflection.Emit.OpCode Break;
        public static readonly System.Reflection.Emit.OpCode Brfalse;
        public static readonly System.Reflection.Emit.OpCode Brfalse_S;
        public static readonly System.Reflection.Emit.OpCode Brtrue;
        public static readonly System.Reflection.Emit.OpCode Brtrue_S;
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
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I1;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I1_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I2;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I2_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I4;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I4_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I8;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_I8_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U1;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U1_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U2;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U2_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U4;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U4_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U8;
        public static readonly System.Reflection.Emit.OpCode Conv_Ovf_U8_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_R_Un;
        public static readonly System.Reflection.Emit.OpCode Conv_R4;
        public static readonly System.Reflection.Emit.OpCode Conv_R8;
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
        public static readonly System.Reflection.Emit.OpCode Ldarg_0;
        public static readonly System.Reflection.Emit.OpCode Ldarg_1;
        public static readonly System.Reflection.Emit.OpCode Ldarg_2;
        public static readonly System.Reflection.Emit.OpCode Ldarg_3;
        public static readonly System.Reflection.Emit.OpCode Ldarg_S;
        public static readonly System.Reflection.Emit.OpCode Ldarga;
        public static readonly System.Reflection.Emit.OpCode Ldarga_S;
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
        public static readonly System.Reflection.Emit.OpCode Ldelema;
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
        public static readonly System.Reflection.Emit.OpCode Ldloc_0;
        public static readonly System.Reflection.Emit.OpCode Ldloc_1;
        public static readonly System.Reflection.Emit.OpCode Ldloc_2;
        public static readonly System.Reflection.Emit.OpCode Ldloc_3;
        public static readonly System.Reflection.Emit.OpCode Ldloc_S;
        public static readonly System.Reflection.Emit.OpCode Ldloca;
        public static readonly System.Reflection.Emit.OpCode Ldloca_S;
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
        public static bool TakesSingleByteArgument(System.Reflection.Emit.OpCode inst) { return default(bool); }
    }
    public enum OpCodeType
    {
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
        Size1 = 1,
        Size128 = 128,
        Size16 = 16,
        Size2 = 2,
        Size32 = 32,
        Size4 = 4,
        Size64 = 64,
        Size8 = 8,
        Unspecified = 0,
    }
    public enum StackBehaviour
    {
        Pop0 = 0,
        Pop1 = 1,
        Pop1_pop1 = 2,
        Popi = 3,
        Popi_pop1 = 4,
        Popi_popi = 5,
        Popi_popi_popi = 7,
        Popi_popi8 = 6,
        Popi_popr4 = 8,
        Popi_popr8 = 9,
        Popref = 10,
        Popref_pop1 = 11,
        Popref_popi = 12,
        Popref_popi_pop1 = 28,
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
    }
}
