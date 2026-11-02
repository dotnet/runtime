// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
    .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
}

.assembly DelayedR2RActivationAliasHelper
{
}

.module DelayedR2RActivationAliasHelper.dll

.namespace System.Runtime
{
    // Crossgen2 skips methods marked with an attribute of this name.
    .class private auto ansi sealed beforefieldinit BypassReadyToRunAttribute extends [System.Runtime]System.Attribute
    {
        .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
        {
            ldarg.0
            call instance void [System.Runtime]System.Attribute::.ctor()
            ret
        }
    }
}

.namespace DelayedR2RActivationHelpers
{
    // Implementation takes over the vtable slot of Declared through a MethodImpl in the same type.
    // The interpreter resolves a call to Declared to Implementation and caches Implementation's
    // byte code on Declared, which makes Declared an alias of Implementation.
    .class public auto ansi beforefieldinit AliasTarget extends [System.Runtime]System.Object
    {
        .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
        {
            ldarg.0
            call instance void [System.Runtime]System.Object::.ctor()
            ret
        }

        // Same result as Implementation: a call that reaches this body instead of Implementation's is a
        // separate interpreter inconsistency for MethodImpls in the same type, not what this test checks.
        .method public hidebysig newslot virtual instance int32 Declared(int32 'value') cil managed noinlining
        {
            ldarg.1
            ldc.i4.s 53
            add
            ret
        }

        .method public hidebysig newslot virtual instance int32 Implementation(int32 'value') cil managed noinlining
        {
            .override DelayedR2RActivationHelpers.AliasTarget::Declared
            ldarg.1
            ldc.i4.s 53
            add
            ret
        }
    }

    .class public abstract auto ansi sealed beforefieldinit AliasCaller extends [System.Runtime]System.Object
    {
        // Stays interpreted so that its non-virtual call to Declared goes through the interpreter's alias.
        .method public hidebysig static int32 CallDeclared(class DelayedR2RActivationHelpers.AliasTarget target, int32 'value') cil managed noinlining
        {
            .custom instance void System.Runtime.BypassReadyToRunAttribute::.ctor() = (01 00 00 00)
            ldarg.0
            ldarg.1
            call instance int32 DelayedR2RActivationHelpers.AliasTarget::Declared(int32)
            ret
        }
    }
}
