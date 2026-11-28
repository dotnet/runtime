// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A) }
.assembly extern xunit.core {}
.assembly BoxedGenericVirtualTailCall {}

.class interface private abstract auto ansi ITarget
{
    .method public hidebysig newslot abstract virtual instance class [System.Runtime]System.Type GetTypeArg<T>() cil managed {}
}

.class private sequential ansi sealed Target extends [System.Runtime]System.ValueType implements ITarget
{
    .method public hidebysig newslot final virtual instance class [System.Runtime]System.Type GetTypeArg<T>() cil managed noinlining
    {
        .maxstack 1
        ldtoken !!T
        call class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
        ret
    }
}

.class public abstract sealed auto ansi BoxedGenericVirtualTailCall extends [System.Runtime]System.Object
{
    .method private static class [System.Runtime]System.Type Call<T>() cil managed noinlining
    {
        .maxstack 1
        .locals init (valuetype Target target)
        ldloc.0
        box Target
        tail.
        callvirt instance class [System.Runtime]System.Type ITarget::GetTypeArg<!!T>()
        ret
    }

    .method public static int32 Main() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (01 00 00 00)
        .entrypoint
        .maxstack 2
        call class [System.Runtime]System.Type BoxedGenericVirtualTailCall::Call<string>()
        ldtoken [System.Runtime]System.String
        call class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
        beq.s Passed
        ldc.i4.1
        ret
    Passed:
        ldc.i4.s 100
        ret
    }
}
