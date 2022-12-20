// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
}
.assembly extern System.Runtime.InteropServices
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
}

.assembly InvalidCSharp { }

.class public auto ansi beforefieldinit InvalidCSharp.GenericClass`1<T>
    extends [System.Runtime]System.Object
{
    .method public hidebysig static
        int32 CallbackMethod (
            int32 n
        ) cil managed preservesig
    {
        .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        .maxstack 8
        ldstr "Functions with attribute UnmanagedCallersOnlyAttribute within a generic type are invalid"
        newobj instance void [System.Runtime]System.Exception::.ctor(string)
        throw
    }

    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        .maxstack 8
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}

.class public auto ansi beforefieldinit InvalidCSharp.Callbacks
    extends [System.Runtime]System.Object
{
    .method public hidebysig static
        int32 CallbackMethodGeneric<T> (
            !!T arg
        ) cil managed preservesig
    {
        .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        .maxstack 8
        ldstr "Functions with attribute UnmanagedCallersOnlyAttribute cannot have generic arguments"
        newobj instance void [System.Runtime]System.Exception::.ctor(string)
        throw
    }

    .method public hidebysig
        instance int32 CallbackNonStatic (
            int32 val
        ) cil managed preservesig
    {
        .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        .maxstack 8
        ldstr "Instance functions with attribute UnmanagedCallersOnlyAttribute are invalid"
        newobj instance void [System.Runtime]System.Exception::.ctor(string)
        throw
    }

    .method public hidebysig static
        native int GetNonStaticCallbackFunctionPointer (
        ) cil managed preservesig
    {
        .maxstack 8
        ldftn instance int32 InvalidCSharp.Callbacks::CallbackNonStatic(int32)
        ret
    }

    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        .maxstack 8
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}

.class public auto ansi beforefieldinit InvalidCSharp.CallingUnmanagedCallersOnlyDirectly
    extends [System.Runtime]System.Object
{
    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        .maxstack 8
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }

    .method public hidebysig static
    int32 ManagedDoubleCallback (
        int32 n
    ) cil managed
    {
        .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        .maxstack 1

        nop
        ldarg.0
        call int32 InvalidCSharp.CallingUnmanagedCallersOnlyDirectly::DoubleImpl(int32)
        ret
    }

    .method private hidebysig static
        int32 DoubleImpl (
            int32 n
        ) cil managed
    {
        .maxstack 2

        nop
        ldc.i4.2
        ldarg.0
        mul
        ret
    }

    .method public hidebysig static
    class [System.Runtime]System.Func`2<int32, int32> GetDoubleDelegate () cil managed
    {
        .maxstack 8

        ldnull
        ldftn int32 InvalidCSharp.CallingUnmanagedCallersOnlyDirectly::ManagedDoubleCallback(int32)
        newobj instance void class [System.Runtime]System.Func`2<int32, int32>::.ctor(object, native int)
        ret
    }

    .method public hidebysig static pinvokeimpl("UnmanagedCallersOnlyDll" as "DoesntExist" winapi)
    int32 PInvokeMarkedWithUnmanagedCallersOnly (
        int32 n
    ) cil managed preservesig
    {
        .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
    }

    .method public hidebysig static
    int32 CallPInvokeMarkedWithUnmanagedCallersOnly (
        int32 n
    ) cil managed
    {
        .maxstack 1

        ldarg.0
        call int32 InvalidCSharp.CallingUnmanagedCallersOnlyDirectly::PInvokeMarkedWithUnmanagedCallersOnly(int32)
        ret
    }
}

.class public auto ansi beforefieldinit InvalidCSharp.UnmanagedCallersOnlyWithByRefs
    extends [System.Runtime]System.Object
{
    .method private hidebysig static
        int32 WithByRef (
            int32& val
        ) cil managed
    {
        .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        .maxstack 8
        ldstr "Functions with by-ref and attribute UnmanagedCallersOnlyAttribute are invalid"
        newobj instance void [System.Runtime]System.Exception::.ctor(string)
        throw
    }
    .method public hidebysig static
        native int GetWithByRefFunctionPointer (
        ) cil managed preservesig
    {
        .maxstack 8
        ldftn int32 InvalidCSharp.UnmanagedCallersOnlyWithByRefs::WithByRef(int32&)
        ret
    }
    .method private hidebysig static
        int32 WithByRefIn (
            [in] int32& val
        ) cil managed
    {
        .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        .maxstack 8
        ldstr "Functions with 'in' by-ref and attribute UnmanagedCallersOnlyAttribute are invalid"
        newobj instance void [System.Runtime]System.Exception::.ctor(string)
        throw
    }
    .method public hidebysig static
        native int GetWithByRefInFunctionPointer (
        ) cil managed preservesig
    {
        .maxstack 8
        ldftn int32 InvalidCSharp.UnmanagedCallersOnlyWithByRefs::WithByRefIn([in] int32&)
        ret
    }
    .method private hidebysig static
        int32 WithByRefOut (
            [out] int32& val
        ) cil managed
    {
        .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        .maxstack 8
        ldstr "Functions with 'out' by-ref and attribute UnmanagedCallersOnlyAttribute are invalid"
        newobj instance void [System.Runtime]System.Exception::.ctor(string)
        throw
    }
    .method public hidebysig static
        native int GetWithByRefOutFunctionPointer (
        ) cil managed preservesig
    {
        .maxstack 8
        ldftn int32 InvalidCSharp.UnmanagedCallersOnlyWithByRefs::WithByRefOut([out] int32&)
        ret
    }

    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        .maxstack 8
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}