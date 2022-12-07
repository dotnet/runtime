// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test originally triggered an assert during remorphing while performing constant propagation when
// extracting the side effects of a discarded tree (specifically, the tree that corresponds to the xor in
// method C::M, which transitively takes exception-producing covnersions as arguments). The assert was
// attempting to ensure that if value numbers were present on the comma node used to hold the side effects,
// then value numbers were also present on the expression being added to the list. This condition may be violated
// when remorphing, however, and the assertion was appropriately weakened.

.assembly extern mscorlib {}
.assembly 'DevDiv_359733' {}
.assembly extern xunit.core {}

.class private C extends [mscorlib]System.Object
{
    .method private static int16 M(bool a0, int16 a1, int16 a2, int32 a3) cil managed noinlining
    {
        .locals init (int32 l0, int16 l1, int64 l2, int16 l3, int8 l4, float32 l5, int8 l6)

        ldloc l3
        ldloc l5
        conv.i8
        conv.r8
        neg
        conv.ovf.u1.un
        ldloc.s l5
        conv.ovf.i8
        ldc.i8 0x4007ACD1
        ldloc l6
        shr
        rem
        nop
        ldc.i8 0x21C591BD
        neg
        cgt.un
        xor
        pop
        ret
    }

    .method private static int32 Main() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint

        .try
        {
            ldc.i4 0
            ldc.i4 0
            ldc.i4 0
            ldc.i4 0
            call int16 C::M(bool, int16, int16, int32)
            pop
            leave.s done
        }
        catch [mscorlib]System.Exception
        {
            pop
            leave.s done
        }

    done:
        ldc.i4 100
        ret
    }
}
