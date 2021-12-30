// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}

.assembly DevDiv_279829 {}
.assembly extern xunit.core {}

.class public Test_DevDiv_279829
{
    .method private static int32 JmpTarget(int64)
    {
        .maxstack 1
        ldarg.0
        conv.i4
        ret
    }

    .method private static int32 Jmp(int64)
    {
        .maxstack 1
        jmp int32 Test_DevDiv_279829::JmpTarget(int64)
        ldc.i4 0
        ret
    }

    .method private static int32 Main(class [mscorlib]System.String[])
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint
        .maxstack 1

        ldc.i8 100
        call int32 Test_DevDiv_279829::Jmp(int64)
        ret
    }
}
