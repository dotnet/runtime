// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// A conditional branch to its own fall-through, where that instruction starts two nested try regions.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A) }
.assembly extern xunit.core { }
.assembly Runtime_136505 { }

.class public auto ansi sealed beforefieldinit Runtime_136505
       extends [System.Runtime]System.Object
{
    .method public hidebysig static int32 TestEntryPoint() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = ( 01 00 00 00 )
        .entrypoint
        .maxstack 2

        ldc.i4.0
        call int32 Runtime_136505::Test(int32)
        ldc.i4.5
        call int32 Runtime_136505::Test(int32)
        add
        ldc.i4.s 100
        add
        ret
    }

    .method private hidebysig static int32 Test(int32 x) cil managed noinlining
    {
        .maxstack 2

        ldarg.0
        ldc.i4.1
        blt.s TRY

        .try
        {
            .try
            {
            TRY:
                nop
                leave.s INNER_DONE
            }
            finally
            {
                endfinally
            }

        INNER_DONE:
            leave.s DONE
        }
        catch [System.Runtime]System.Exception
        {
            pop
            leave.s DONE
        }

    DONE:
        ldc.i4.0
        ret
    }
}
