// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern xunit.core { }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Console { }

.assembly ExceptionSetsPropagation_LclHeap { }

.class public ExceptionSetsPropagation_LclHeap extends [System.Runtime]System.Object
{
    .method public static int32 Main()
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (01 00 00 00)
        .entrypoint

        .try
        {
            ldc.i4 300
            conv.i
            call bool ExceptionSetsPropagation_LclHeap::Problem(native int)
            pop
            leave FAIL
        }
        catch [System.Runtime]System.OverflowException
        {
            pop
            leave SUCCESS
        }

    SUCCESS:
        ldc.i4 100
        ret

    FAIL:
        ldc.i4 101
        ret
    }

    .method private static bool Problem(native int a)
    {
        .locals (bool c1, bool c2)

        ldarg a
        localloc
        ldc.i4 0
        conv.i
        and
        ldarg a
        ceq
        stloc c1

        ldloc c1
        stloc c2

        ldarg a
        conv.ovf.i1
        conv.i
        localloc
        ldc.i4 0
        conv.i
        and
        ldarg a
        ceq
        brtrue RET

        ldstr "The expected OverflowException was not thrown!"
        call void [System.Console]System.Console::WriteLine(string)

    RET:
        ldloc c2
        ret
    }
}

