// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib {}

.assembly DevDiv_279829.exe {}

.class public Test
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
        jmp int32 Test::JmpTarget(int64)
        ldc.i4 0
        ret
    }

    .method private static int32 Main(class [mscorlib]System.String[])
    {
        .entrypoint
        .maxstack 1

        ldc.i8 100
        call int32 Test::Jmp(int64)
        ret
    }
}
