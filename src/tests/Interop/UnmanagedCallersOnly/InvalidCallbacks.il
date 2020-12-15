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
        ) cil managed preservesig 
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
        ) cil managed preservesig 
    {
        .custom instance void [System.Runtime.InteropServices]System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute::.ctor() = (
            01 00 00 00
        )
        .maxstack 8
        IL_0000: ldstr "Instance functions with attribute UnmanagedCallersOnlyAttribute are invalid"
        IL_0005: newobj instance void [System.Runtime]System.Exception::.ctor(string)
        IL_000a: throw
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
        IL_0000: ldarg.0
        IL_0001: call instance void [System.Runtime]System.Object::.ctor()
        IL_0006: ret
    }
}

.class public auto ansi beforefieldinit InvalidCSharp.CallingUnmanagedCallersOnlyDirectly
    extends [System.Runtime]System.Object
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8
        IL_0000: ldarg.0
        IL_0001: call instance void [System.Runtime]System.Object::.ctor()
        IL_0006: ret
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
        .locals init (
            [0] int32
        )

        IL_0000: nop
        IL_0001: ldarg.0
        IL_0002: call int32 InvalidCSharp.CallingUnmanagedCallersOnlyDirectly::DoubleImpl(int32)
        IL_0007: stloc.0
        IL_0008: br.s IL_000a

        IL_000a: ldloc.0
        IL_000b: ret
    }
    
    .method private hidebysig static 
        int32 DoubleImpl (
            int32 n
        ) cil managed 
    {
        .maxstack 2
        .locals init (
            [0] int32
        )

        IL_0000: nop
        IL_0001: ldc.i4.2
        IL_0002: ldarg.0
        IL_0003: mul
        IL_0004: stloc.0
        IL_0005: br.s IL_0007

        IL_0007: ldloc.0
        IL_0008: ret
    }

    .method public hidebysig static 
    class [System.Runtime]System.Func`2<int32, int32> GetDoubleDelegate () cil managed 
    {
        .maxstack 8

        IL_0000: ldnull
        IL_0001: ldftn int32 InvalidCSharp.CallingUnmanagedCallersOnlyDirectly::ManagedDoubleCallback(int32)
        IL_0007: newobj instance void class [System.Runtime]System.Func`2<int32, int32>::.ctor(object, native int)
        IL_000c: ret
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