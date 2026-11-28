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

    // Resolving the interface dispatch throws AmbiguousImplementationException from the
    // virtual stub dispatch resolver, with no frame of this method remaining. The receiver
    // is a parameter so that the call cannot be devirtualized. The method is kept out of the
    // R2R image because R2R code resolves the first interface call through a delay load
    // helper instead of the virtual stub dispatch resolver.
    .method public static int32 InterfaceTailCall(class IAmbiguous obj) cil managed noinlining
    {
        .custom instance void System.Runtime.BypassReadyToRunAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 1
        ldarg.0
        tail.
        callvirt instance int32 IAmbiguous::M()
        ret
    }

    .method public static class IAmbiguous CreateAmbiguous() cil managed noinlining
    {
        .maxstack 1
        newobj instance void AmbiguousImplementation::.ctor()
        ret
    }
}

.class private auto ansi sealed beforefieldinit System.Runtime.BypassReadyToRunAttribute extends [mscorlib]System.Attribute
{
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 1
        ldarg.0
        call instance void [mscorlib]System.Attribute::.ctor()
        ret
    }
}

.class interface public abstract auto ansi IAmbiguous
{
    .method public hidebysig newslot abstract virtual instance int32 M() cil managed
    {
    }
}

.class interface public abstract auto ansi IAmbiguousA implements IAmbiguous
{
    .method private hidebysig final virtual instance int32 IAmbiguous.M() cil managed
    {
        .override IAmbiguous::M
        .maxstack 1
        ldc.i4.1
        ret
    }
}

.class interface public abstract auto ansi IAmbiguousB implements IAmbiguous
{
    .method private hidebysig final virtual instance int32 IAmbiguous.M() cil managed
    {
        .override IAmbiguous::M
        .maxstack 1
        ldc.i4.2
        ret
    }
}

.class public auto ansi beforefieldinit AmbiguousImplementation extends [mscorlib]System.Object
    implements IAmbiguousA, IAmbiguousB, IAmbiguous
{
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 1
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        ret
    }
}
