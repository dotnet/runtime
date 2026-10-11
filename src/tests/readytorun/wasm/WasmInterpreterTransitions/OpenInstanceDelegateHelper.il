// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
    .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
}

.assembly OpenInstanceDelegateHelper
{
}

.module OpenInstanceDelegateHelper.dll

.namespace WasmDelegateHelpers
{
    .class public auto ansi sealed Target extends [System.Runtime]System.Object
    {
        .field private int32 _state

        .method public hidebysig specialname rtspecialname instance void .ctor(int32) cil managed
        {
            .maxstack 2
            ldarg.0
            call instance void [System.Runtime]System.Object::.ctor()
            ldarg.0
            ldarg.1
            stfld int32 WasmDelegateHelpers.Target::_state
            ret
        }

        .method public hidebysig instance int32 Transform(int32) cil managed noinlining
        {
            .maxstack 2
            ldarg.1
            ldarg.0
            ldfld int32 WasmDelegateHelpers.Target::_state
            add
            ret
        }
    }

    .class public auto ansi sealed OpenTransform extends [System.Runtime]System.MulticastDelegate
    {
        .method public hidebysig specialname rtspecialname instance void .ctor(object, native int) runtime managed
        {
        }

        .method public hidebysig newslot virtual instance int32 Invoke(
            class WasmDelegateHelpers.Target,
            int32) runtime managed
        {
        }
    }

    .class public abstract auto ansi sealed Factory extends [System.Runtime]System.Object
    {
        .method public hidebysig static class WasmDelegateHelpers.OpenTransform Create() cil managed noinlining
        {
            .maxstack 2
            ldnull
            ldftn instance int32 WasmDelegateHelpers.Target::Transform(int32)
            newobj instance void WasmDelegateHelpers.OpenTransform::.ctor(object, native int)
            ret
        }
    }
}
