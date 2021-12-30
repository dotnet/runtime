// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test checks that value numbering does not incorrectly propagate the value number for an operand to its
// consuming operator unless the type of the operand and the operator agree.
//
// In particular, in the body of method N, the optimizer will fold the two `ceq` conditions together using a logical
// or of type int32. Value numbering will then determine that one operand to the logical or is a constant `0` (namely,
// the result of `ldloc.0` and attempt to propagate the value number of the logical or's other operand to the logical
// or itself. This should not succeed, as the type of the logical or is int32 and the type of its non-zero operand is
// `ref`.

.assembly extern mscorlib {}

.assembly 'DevDiv_278369' {}
.assembly extern xunit.core {}

.class private auto ansi beforefieldinit C extends [mscorlib]System.Object
{
    .method private hidebysig static int32 N(object o) cil managed noinlining
    {
        .locals (int32)

        ldc.i4.0
        stloc.0
        ldarg.0
        isinst C
        ldnull
        ceq
        brfalse.s label
        ldloc.0
        ldc.i4.0
        ceq
        brfalse.s label
        ldc.i4.s 0
        ret

label:
        ldc.i4 100
        ret
    }

    .method private hidebysig static int32 Main(string[] args) cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint

        newobj instance void C::.ctor()
        call int32 C::N(object)
        ret
    }

    .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }
}
