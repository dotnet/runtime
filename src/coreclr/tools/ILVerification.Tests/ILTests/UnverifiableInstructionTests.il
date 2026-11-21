// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly UnverifiableInstructionTests
{
}

// Instructions that are never verifiable (III.3.37, III.3.30, III.3.36, III.2.2), and the validity checks that
// still apply to them in both modes. Native int parameters stand for unmanaged pointers.
.class public auto ansi beforefieldinit UnverifiableInstructionTestsType
       extends [System.Runtime]System.Object
{
    .method public hidebysig static void Target() cil managed
    {
        ret
    }

    .method public hidebysig static void TakesInt32(int32 i) cil managed
    {
        ret
    }

    .method public hidebysig static void Jmp.ToTarget_Invalid_Unverifiable_Validation_Valid() cil managed
    {
        jmp void UnverifiableInstructionTestsType::Target()
    }

    .method public hidebysig static void CopyBlock.UnmanagedPointers_Invalid_Unverifiable_Validation_Valid(native int d, native int s) cil managed
    {
        ldarg.0
        ldarg.1
        ldc.i4.4
        cpblk
        ret
    }

    .method public hidebysig static void InitBlock.UnmanagedPointer_Invalid_Unverifiable_Validation_Valid(native int d) cil managed
    {
        ldarg.0
        ldc.i4.0
        ldc.i4.4
        initblk
        ret
    }

    .method public hidebysig static void NoPrefix.Castclass_Invalid_Unverifiable_Validation_Valid(object o) cil managed
    {
        ldarg.0
        // no. typecheck
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x01
        castclass [System.Runtime]System.Object
        pop
        ret
    }

    .method public hidebysig static void Jmp.StackNotEmpty_Invalid_Unverifiable.StackUnexpected_Validation_StackUnexpected() cil managed
    {
        ldc.i4.0
        jmp void UnverifiableInstructionTestsType::Target()
    }

    .method public hidebysig static void Jmp.ArgumentCount_Invalid_Unverifiable.StackUnexpected_Validation_StackUnexpected() cil managed
    {
        jmp void UnverifiableInstructionTestsType::TakesInt32(int32)
    }

    .method public hidebysig static void Jmp.ArgumentType_Invalid_Unverifiable.StackUnexpected_Validation_StackUnexpected(string s) cil managed
    {
        jmp void UnverifiableInstructionTestsType::TakesInt32(int32)
    }

    .method public hidebysig static void Jmp.SameArgument_Invalid_Unverifiable_Validation_Valid(int32 i) cil managed
    {
        jmp void UnverifiableInstructionTestsType::TakesInt32(int32)
    }

    .method public hidebysig static int32 Jmp.ReturnType_Invalid_Unverifiable.StackUnexpected_Validation_StackUnexpected() cil managed
    {
        jmp void UnverifiableInstructionTestsType::Target()
    }

    .method public hidebysig instance void Jmp.InstanceToStatic_Invalid_Unverifiable.StackUnexpected_Validation_StackUnexpected() cil managed
    {
        jmp void UnverifiableInstructionTestsType::Target()
    }

    .method public hidebysig instance void InstanceTarget() cil managed
    {
        ret
    }

    .class nested public auto ansi beforefieldinit OtherType
           extends [System.Runtime]System.Object
    {
        .method public hidebysig instance void InstanceTarget() cil managed
        {
            ret
        }
    }

    .method public hidebysig instance void Jmp.InstanceToSameType_Invalid_Unverifiable_Validation_Valid() cil managed
    {
        jmp instance void UnverifiableInstructionTestsType::InstanceTarget()
    }

    .method public hidebysig instance void Jmp.InstanceToOtherType_Invalid_Unverifiable.StackUnexpected_Validation_StackUnexpected() cil managed
    {
        jmp instance void UnverifiableInstructionTestsType/OtherType::InstanceTarget()
    }

    .class nested public auto ansi beforefieldinit DerivedType
           extends UnverifiableInstructionTestsType/OtherType
    {
        .method public hidebysig instance void Jmp.InstanceToBaseType_Invalid_Unverifiable_Validation_Valid() cil managed
        {
            jmp instance void UnverifiableInstructionTestsType/OtherType::InstanceTarget()
        }
    }

    .class nested public sequential ansi sealed beforefieldinit Struct
           extends [System.Runtime]System.ValueType
    {
        .method public hidebysig instance void InstanceTarget() cil managed
        {
            ret
        }

        .method public hidebysig instance void Jmp.ValueTypeToSameType_Invalid_Unverifiable_Validation_Valid() cil managed
        {
            jmp instance void UnverifiableInstructionTestsType/Struct::InstanceTarget()
        }

        .method public hidebysig instance void Jmp.ValueTypeToClass_Invalid_Unverifiable.StackUnexpected_Validation_StackUnexpected() cil managed
        {
            jmp instance void UnverifiableInstructionTestsType/OtherType::InstanceTarget()
        }
    }

    .method public hidebysig static void Jmp.InsideTry_Invalid_Unverifiable.JmpInsideER_Validation_JmpInsideER() cil managed
    {
        .try
        {
            jmp void UnverifiableInstructionTestsType::Target()
        }
        catch [System.Runtime]System.Object
        {
            pop
            leave.s END
        }
    END:
        ret
    }

    .method public hidebysig static void Jmp.InsideFilter_Invalid_Unverifiable.JmpInsideER_Validation_JmpInsideER() cil managed
    {
        .try
        {
            leave.s END
        }
        filter
        {
            pop
            jmp void UnverifiableInstructionTestsType::Target()
            endfilter
        }
        {
            pop
            leave.s END
        }
    END:
        ret
    }

    .method public hidebysig static void Jmp.InsideHandler_Invalid_Unverifiable.JmpInsideER_Validation_JmpInsideER() cil managed
    {
        .try
        {
            leave.s END
        }
        catch [System.Runtime]System.Object
        {
            pop
            jmp void UnverifiableInstructionTestsType::Target()
        }
    END:
        ret
    }

    .method public hidebysig static void Jmp.FromSynchronized_Invalid_Unverifiable.JmpInsideER_Validation_JmpInsideER() cil managed synchronized
    {
        jmp void UnverifiableInstructionTestsType::Target()
    }

    .method public hidebysig static void CopyBlock.FloatSource_Invalid_Unverifiable.StackByRef_Validation_StackByRef(native int d) cil managed
    {
        ldarg.0
        ldc.r8 1
        ldc.i4.4
        cpblk
        ret
    }

    .method public hidebysig static void CopyBlock.FloatDestination_Invalid_Unverifiable.StackByRef_Validation_StackByRef(native int s) cil managed
    {
        ldc.r8 1
        ldarg.0
        ldc.i4.4
        cpblk
        ret
    }

    .method public hidebysig static void CopyBlock.FloatSize_Invalid_Unverifiable.ExpectedIntegerType_Validation_ExpectedIntegerType(native int d, native int s) cil managed
    {
        ldarg.0
        ldarg.1
        ldc.r8 1
        cpblk
        ret
    }

    .method public hidebysig static void CopyBlock.Int64Size_Invalid_Unverifiable.ExpectedIntegerType_Validation_ExpectedIntegerType(native int d, native int s) cil managed
    {
        ldarg.0
        ldarg.1
        ldc.i8 4
        cpblk
        ret
    }

    .method public hidebysig static void InitBlock.FloatAddress_Invalid_Unverifiable.StackByRef_Validation_StackByRef() cil managed
    {
        ldc.r8 1
        ldc.i4.0
        ldc.i4.4
        initblk
        ret
    }

    .method public hidebysig static void InitBlock.ManagedPointer_Invalid_Unverifiable_Validation_Valid(int32& d) cil managed
    {
        ldarg.0
        ldc.i4.0
        ldc.i4.4
        initblk
        ret
    }

    .method public hidebysig static void InitBlock.FloatValue_Invalid_Unverifiable.ExpectedIntegerType_Validation_ExpectedIntegerType(native int d) cil managed
    {
        ldarg.0
        ldc.r8 1
        ldc.i4.4
        initblk
        ret
    }

    .method public hidebysig static void InitBlock.FloatSize_Invalid_Unverifiable.ExpectedIntegerType_Validation_ExpectedIntegerType(native int d) cil managed
    {
        ldarg.0
        ldc.i4.0
        ldc.r8 1
        initblk
        ret
    }

    .method public hidebysig static void InitBlock.Int64Size_Invalid_Unverifiable.ExpectedIntegerType_Validation_ExpectedIntegerType(native int d) cil managed
    {
        ldarg.0
        ldc.i4.0
        ldc.i8 4
        initblk
        ret
    }

    .method public hidebysig static void InitBlock.Int64Value_Invalid_Unverifiable.ExpectedIntegerType_Validation_ExpectedIntegerType(native int d) cil managed
    {
        ldarg.0
        ldc.i8 0
        ldc.i4.4
        initblk
        ret
    }

    // no. may only skip fault checks the next instruction performs (III.2.2). The emitted bytes are no. and its mask.

    .method public hidebysig static void NoPrefix.UnboxTypecheck_Invalid_Unverifiable_Validation_Valid(object o) cil managed
    {
        ldarg.0
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x01
        unbox [System.Runtime]System.Int32
        pop
        ret
    }

    .method public hidebysig static void NoPrefix.LdelemaAllChecks_Invalid_Unverifiable_Validation_Valid(int32[] a) cil managed
    {
        ldarg.0
        ldc.i4.0
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x07
        ldelema [System.Runtime]System.Int32
        pop
        ret
    }

    .method public hidebysig static void NoPrefix.LdelemRangeAndNullcheck_Invalid_Unverifiable_Validation_Valid(int32[] a) cil managed
    {
        ldarg.0
        ldc.i4.0
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x06
        ldelem.i4
        pop
        ret
    }

    .method public hidebysig static void NoPrefix.StelemRefAllChecks_Invalid_Unverifiable_Validation_Valid(object[] a) cil managed
    {
        ldarg.0
        ldc.i4.0
        ldnull
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x07
        stelem.ref
        ret
    }

    .method public hidebysig static void NoPrefix.StelemTokenAllChecks_Invalid_Unverifiable_Validation_Valid(object[] a) cil managed
    {
        ldarg.0
        ldc.i4.0
        ldnull
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x07
        stelem [System.Runtime]System.Object
        ret
    }

    .method public hidebysig static void NoPrefix.StelemI4RangeAndNullcheck_Invalid_Unverifiable_Validation_Valid(int32[] a) cil managed
    {
        ldarg.0
        ldc.i4.0
        ldc.i4.0
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x06
        stelem.i4
        ret
    }

    .method public hidebysig static void NoPrefix.LdfldNullcheck_Invalid_Unverifiable_Validation_Valid(class NoPrefixHolder h) cil managed
    {
        ldarg.0
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x04
        ldfld int32 NoPrefixHolder::Instance
        pop
        ret
    }

    .method public hidebysig static void NoPrefix.StfldNullcheck_Invalid_Unverifiable_Validation_Valid(class NoPrefixHolder h) cil managed
    {
        ldarg.0
        ldc.i4.0
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x04
        stfld int32 NoPrefixHolder::Instance
        ret
    }

    .method public hidebysig static void NoPrefix.CallvirtNullcheck_Invalid_Unverifiable_Validation_Valid(object o) cil managed
    {
        ldarg.0
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x04
        callvirt instance string [System.Runtime]System.Object::ToString()
        pop
        ret
    }

    .method public hidebysig static void NoPrefix.LdvirtftnNullcheck_Invalid_Unverifiable_Validation_Valid(object o) cil managed
    {
        ldarg.0
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x04
        ldvirtftn instance string [System.Runtime]System.Object::ToString()
        pop
        ret
    }

    .method public hidebysig static void NoPrefix.BeforeNop_Invalid_Unverifiable.NoPrefix_Validation_NoPrefix() cil managed
    {
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x01
        nop
        ret
    }

    .method public hidebysig static void NoPrefix.BeforeIsinst_Invalid_Unverifiable.NoPrefix_Validation_NoPrefix(object o) cil managed
    {
        ldarg.0
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x01
        isinst [System.Runtime]System.Object
        pop
        ret
    }

    .method public hidebysig static void NoPrefix.UnboxAnyTypecheck_Invalid_Unverifiable.NoPrefix_Validation_NoPrefix(object o) cil managed
    {
        ldarg.0
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x01
        unbox.any [System.Runtime]System.Int32
        pop
        ret
    }

    .method public hidebysig static void NoPrefix.CastclassRangecheck_Invalid_Unverifiable.NoPrefix_Validation_NoPrefix(object o) cil managed
    {
        ldarg.0
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x02
        castclass [System.Runtime]System.Object
        pop
        ret
    }

    .method public hidebysig static void NoPrefix.UnknownCheck_Invalid_Unverifiable.NoPrefix_Validation_NoPrefix(object o) cil managed
    {
        ldarg.0
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x08
        castclass [System.Runtime]System.Object
        pop
        ret
    }

    .method public hidebysig static void NoPrefix.LdelemTypecheck_Invalid_Unverifiable.NoPrefix_Validation_NoPrefix(int32[] a) cil managed
    {
        ldarg.0
        ldc.i4.0
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x01
        ldelem.i4
        pop
        ret
    }

    .method public hidebysig static void NoPrefix.StelemI4Typecheck_Invalid_Unverifiable.NoPrefix_Validation_NoPrefix(int32[] a) cil managed
    {
        ldarg.0
        ldc.i4.0
        ldc.i4.0
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x01
        stelem.i4
        ret
    }

    .method public hidebysig static void NoPrefix.LdsfldNullcheck_Invalid_Unverifiable.NoPrefix_Validation_NoPrefix() cil managed
    {
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x04
        ldsfld int32 NoPrefixHolder::Static
        pop
        ret
    }

    .method public hidebysig static void NoPrefix.StsfldNullcheck_Invalid_Unverifiable.NoPrefix_Validation_NoPrefix() cil managed
    {
        ldc.i4.0
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x04
        stsfld int32 NoPrefixHolder::Static
        ret
    }

    .method public hidebysig static void NoPrefix.CallNullcheck_Invalid_Unverifiable.NoPrefix_Validation_NoPrefix() cil managed
    {
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x04
        call void UnverifiableInstructionTestsType::Target()
        ret
    }

    .method public hidebysig static void NoPrefix.LdftnNullcheck_Invalid_Unverifiable.NoPrefix_Validation_NoPrefix() cil managed
    {
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x04
        ldftn void UnverifiableInstructionTestsType::Target()
        pop
        ret
    }

    .method public hidebysig static void NoPrefix.BeforeVolatile_Invalid_Unverifiable.NoPrefix_Validation_NoPrefix(class NoPrefixHolder h) cil managed
    {
        ldarg.0
        .emitbyte 0xFE
        .emitbyte 0x19
        .emitbyte 0x04
        volatile.
        ldfld int32 NoPrefixHolder::Instance
        pop
        ret
    }
}

.class public auto ansi beforefieldinit NoPrefixHolder
       extends [System.Runtime]System.Object
{
    .field public int32 Instance
    .field public static int32 Static
}
