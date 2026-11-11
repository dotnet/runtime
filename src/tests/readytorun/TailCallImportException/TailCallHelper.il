// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly TailCallHelper {}
.module TailCallHelper.dll

.class public auto ansi TailCallHelper extends [mscorlib]System.Object
{
    .method public static int32 TailCall() cil managed noinlining
    {
        .maxstack 1
        tail.
        call int32 TailCallHelper::InvalidProgram()
        ret
    }

    // Crossgen cannot compile this body. Resolving the tailcall import must compile it
    // at runtime and throw from ExternalMethodFixupWorker, with no R2R frame remaining.
    .method private static int32 InvalidProgram() cil managed noinlining
    {
        .maxstack 1
        ret
    }
}
