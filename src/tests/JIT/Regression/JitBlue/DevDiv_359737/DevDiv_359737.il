// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly DevDiv_359737 {}
.assembly extern xunit.core {}

// This test originally repro'd a bug in morph that transformed modulus by a constant into division and subtraction
// while re-morphing as part of CSE.

.class C extends [mscorlib]System.Object
{
    .method static int16 M() cil managed noinlining
    {
        ldc.i4 0x78804C04
        conv.ovf.u2
        dup
        rem
        ret
    }

    .method static int32 Main() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint

        .try
        {
            call int16 C::M()
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
