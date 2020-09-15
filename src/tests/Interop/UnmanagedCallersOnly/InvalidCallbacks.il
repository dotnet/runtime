// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Runtime.InteropServices { }

.assembly InvalidCSharp { }

.class public auto ansi beforefieldinit InvalidCSharp.GenericClass`1<T>
    extends System.Object
{
    .method public hidebysig static 
        void CallbackMethod (
            int32 n
        ) cil managed
    {
        .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        .maxstack 8
        IL_0000: ldstr "Functions with attribute UnmanagedCallersOnlyAttribute within a generic type are invalid"
        IL_0005: newobj instance void [System.Runtime]System.Exception::.ctor(string)
        IL_000a: throw
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: call instance void [System.Runtime]System.Object::.ctor()
        IL_0006: ret
    }
}

.class public auto ansi beforefieldinit InvalidCSharp.Callbacks
    extends [System.Runtime]System.Object
{
    .method public hidebysig static 
        int32 CallbackMethodGeneric<T> (
            !!T arg
        ) cil managed
    {
        .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        .maxstack 8
        IL_0000: ldstr "Functions with attribute UnmanagedCallersOnlyAttribute cannot have generic arguments"
        IL_0005: newobj instance void [System.Runtime]System.Exception::.ctor(string)
        IL_000a: throw
    }

    .method public hidebysig 
        instance int32 CallbackNonStatic (
            int32 val
        ) cil managed
    {
        .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        .maxstack 8
        IL_0000: ldstr "Instance functions with attribute UnmanagedCallersOnlyAttribute are invalid"
        IL_0005: newobj instance void [System.Runtime]System.Exception::.ctor(string)
        IL_000a: throw
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: call instance void [System.Runtime]System.Object::.ctor()
        IL_0006: ret
    }
}
