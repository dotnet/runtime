// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly ValidationOnlyTests
{
}

// IL that is valid but not verifiable, and the validity checks that still apply to it.
// Native int parameters stand for unmanaged pointers.
.class public auto ansi beforefieldinit ValidationOnlyTestsType
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

    // Instructions that are never verifiable (III.3.37, III.3.30, III.3.36, III.2.2)

    .method public hidebysig static void Jmp.ToTarget_Invalid_Unverifiable_Validation_Valid() cil managed
    {
        jmp void ValidationOnlyTestsType::Target()
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

    // Their validity rules are still reported

    .method public hidebysig static void Jmp.StackNotEmpty_Invalid_Unverifiable_Validation_StackUnexpected() cil managed
    {
        ldc.i4.0
        jmp void ValidationOnlyTestsType::Target()
    }

    .method public hidebysig static void Jmp.ArgumentCount_Invalid_Unverifiable_Validation_StackUnexpected() cil managed
    {
        jmp void ValidationOnlyTestsType::TakesInt32(int32)
    }

    .method public hidebysig static void Jmp.ArgumentType_Invalid_Unverifiable_Validation_StackUnexpected(string s) cil managed
    {
        jmp void ValidationOnlyTestsType::TakesInt32(int32)
    }

    .method public hidebysig static void Jmp.SameArgument_Invalid_Unverifiable_Validation_Valid(int32 i) cil managed
    {
        jmp void ValidationOnlyTestsType::TakesInt32(int32)
    }

    .method public hidebysig instance void Jmp.InstanceToStatic_Invalid_Unverifiable_Validation_StackUnexpected() cil managed
    {
        jmp void ValidationOnlyTestsType::Target()
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
        jmp instance void ValidationOnlyTestsType::InstanceTarget()
    }

    .method public hidebysig instance void Jmp.InstanceToOtherType_Invalid_Unverifiable_Validation_StackUnexpected() cil managed
    {
        jmp instance void ValidationOnlyTestsType/OtherType::InstanceTarget()
    }

    .class nested public auto ansi beforefieldinit DerivedType
           extends ValidationOnlyTestsType/OtherType
    {
        .method public hidebysig instance void Jmp.InstanceToBaseType_Invalid_Unverifiable_Validation_Valid() cil managed
        {
            jmp instance void ValidationOnlyTestsType/OtherType::InstanceTarget()
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
            jmp instance void ValidationOnlyTestsType/Struct::InstanceTarget()
        }

        .method public hidebysig instance void Jmp.ValueTypeToClass_Invalid_Unverifiable_Validation_StackUnexpected() cil managed
        {
            jmp instance void ValidationOnlyTestsType/OtherType::InstanceTarget()
        }
    }

    .method public hidebysig static void Jmp.InsideTry_Invalid_Unverifiable_Validation_JmpInsideER() cil managed
    {
        .try
        {
            jmp void ValidationOnlyTestsType::Target()
        }
        catch [System.Runtime]System.Object
        {
            pop
            leave.s END
        }
    END:
        ret
    }

    .method public hidebysig static void Jmp.InsideFilter_Invalid_Unverifiable_Validation_JmpInsideER() cil managed
    {
        .try
        {
            leave.s END
        }
        filter
        {
            pop
            jmp void ValidationOnlyTestsType::Target()
            endfilter
        }
        {
            pop
            leave.s END
        }
    END:
        ret
    }

    .method public hidebysig static void Jmp.InsideHandler_Invalid_Unverifiable_Validation_JmpInsideER() cil managed
    {
        .try
        {
            leave.s END
        }
        catch [System.Runtime]System.Object
        {
            pop
            jmp void ValidationOnlyTestsType::Target()
        }
    END:
        ret
    }

    .method public hidebysig static void Jmp.FromSynchronized_Invalid_Unverifiable_Validation_JmpInsideER() cil managed synchronized
    {
        jmp void ValidationOnlyTestsType::Target()
    }

    .method public hidebysig static void CopyBlock.FloatSource_Invalid_Unverifiable_Validation_StackByRef(native int d) cil managed
    {
        ldarg.0
        ldc.r8 1
        ldc.i4.4
        cpblk
        ret
    }

    .method public hidebysig static void CopyBlock.FloatDestination_Invalid_Unverifiable_Validation_StackByRef(native int s) cil managed
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

    .method public hidebysig static void InitBlock.FloatAddress_Invalid_Unverifiable_Validation_StackByRef() cil managed
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

    .method public hidebysig static void CopyBlock.Int64Size_Invalid_Unverifiable_Validation_ExpectedIntegerType(native int d, native int s) cil managed
    {
        ldarg.0
        ldarg.1
        ldc.i8 4
        cpblk
        ret
    }

    .method public hidebysig static void InitBlock.Int64Size_Invalid_Unverifiable_Validation_ExpectedIntegerType(native int d) cil managed
    {
        ldarg.0
        ldc.i4.0
        ldc.i8 4
        initblk
        ret
    }

    .method public hidebysig static void InitBlock.Int64Value_Invalid_Unverifiable_Validation_ExpectedIntegerType(native int d) cil managed
    {
        ldarg.0
        ldc.i8 0
        ldc.i4.4
        initblk
        ret
    }

    // localsinit is required only for verifiable methods (III.1.8.1.1)

    .method public hidebysig static void InitLocals.LoadLocal_Invalid_InitLocals_Validation_Valid() cil managed
    {
        .locals (int32 V_0)
        ldloc.0
        pop
        ret
    }

    .method public hidebysig static void InitLocals.LoadLocalAddress_Invalid_InitLocals_Validation_Valid() cil managed
    {
        .locals (int32 V_0)
        ldloca.s V_0
        pop
        ret
    }
}
