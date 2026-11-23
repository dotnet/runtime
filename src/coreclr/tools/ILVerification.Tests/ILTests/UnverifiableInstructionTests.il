// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly UnverifiableInstructionTests
{
}

// Instructions that are never verifiable (III.3.37, III.3.30, III.3.36, III.2.2): reported by default, not in
// validation-only mode. Native int parameters stand for unmanaged pointers.
.class public auto ansi beforefieldinit UnverifiableInstructionTestsType
       extends [System.Runtime]System.Object
{
    .method public hidebysig static void Target() cil managed
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
}
