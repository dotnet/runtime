// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A) .ver 4:0:0:0 }
.assembly extern xunit.core {}

.assembly 'modified-task-covariant-return'
{
}

.class public auto ansi beforefieldinit ModifiedTask extends class [System.Runtime]System.Threading.Tasks.Task`1<int32>
{
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        ldarg.0
        ldnull
        ldftn int32 ModifiedTask::GetValue()
        newobj instance void class [System.Runtime]System.Func`1<int32>::.ctor(object, native int)
        call instance void class [System.Runtime]System.Threading.Tasks.Task`1<int32>::.ctor(class [System.Runtime]System.Func`1<!0>)
        ldarg.0
        call instance void [System.Runtime]System.Threading.Tasks.Task::RunSynchronously()
        ret
    }

    .method private hidebysig static int32 GetValue() cil managed
    {
        ldc.i4.s 42
        ret
    }
}

.class public auto ansi beforefieldinit ModifiedBase extends [System.Runtime]System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }

    .method public hidebysig newslot virtual instance class [System.Runtime]System.Threading.Tasks.Task`1<int32 modopt([System.Runtime]System.ObsoleteAttribute)> Get() cil managed async
    {
        ldc.i4.1
        ret
    }
}

.class public auto ansi beforefieldinit ModifiedDerived extends ModifiedBase
{
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        ldarg.0
        call instance void ModifiedBase::.ctor()
        ret
    }

    .method public hidebysig newslot virtual instance class ModifiedTask Get() cil managed
    {
        .override method instance class [System.Runtime]System.Threading.Tasks.Task`1<int32 modopt([System.Runtime]System.ObsoleteAttribute)> ModifiedBase::Get()
        newobj instance void ModifiedTask::.ctor()
        ret
    }
}

.class public auto ansi beforefieldinit FunctionPointerTask`1<T> extends class [System.Runtime]System.Threading.Tasks.Task`1<!0>
{
    .field private !0 _value

    .method public hidebysig specialname rtspecialname instance void .ctor(!0 'value') cil managed
    {
        ldarg.0
        ldarg.1
        stfld !0 class FunctionPointerTask`1<!0>::_value
        ldarg.0
        ldarg.0
        ldftn instance !0 class FunctionPointerTask`1<!0>::GetValue()
        newobj instance void class [System.Runtime]System.Func`1<!0>::.ctor(object, native int)
        call instance void class [System.Runtime]System.Threading.Tasks.Task`1<!0>::.ctor(class [System.Runtime]System.Func`1<!0>)
        ldarg.0
        call instance void [System.Runtime]System.Threading.Tasks.Task::RunSynchronously()
        ret
    }

    .method private hidebysig instance !0 GetValue() cil managed
    {
        ldarg.0
        ldfld !0 class FunctionPointerTask`1<!0>::_value
        ret
    }
}

.class public auto ansi beforefieldinit FunctionPointerBase`1<T> extends [System.Runtime]System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }

    .method public hidebysig newslot virtual instance class [System.Runtime]System.Threading.Tasks.Task`1<method void *()[]> GetSimple() cil managed async
    {
        ldc.i4.0
        newarr method void *()
        ret
    }

    .method public hidebysig newslot virtual instance class [System.Runtime]System.Threading.Tasks.Task`1<method !0& *(method unmanaged cdecl !!0 *(!0&))[]> Get<U>() cil managed async
    {
        ldc.i4.0
        newarr method !0& *(method unmanaged cdecl !!0 *(!0&))
        ret
    }
}

.class public auto ansi beforefieldinit FunctionPointerDerived extends class FunctionPointerBase`1<int64>
{
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        ldarg.0
        call instance void class FunctionPointerBase`1<int64>::.ctor()
        ret
    }

    .method public hidebysig newslot virtual instance class FunctionPointerTask`1<method void *()[]> GetSimple() cil managed
    {
        .override method instance class [System.Runtime]System.Threading.Tasks.Task`1<method void *()[]> class FunctionPointerBase`1<int64>::GetSimple()
        ldc.i4.1
        newarr method void *()
        newobj instance void class FunctionPointerTask`1<method void *()[]>::.ctor(!0)
        ret
    }

    .method public hidebysig newslot virtual instance class FunctionPointerTask`1<method int64& *(method unmanaged cdecl !!0 *(int64&))[]> Get<U>() cil managed
    {
        .override method instance class [System.Runtime]System.Threading.Tasks.Task`1<method !0& *(method unmanaged cdecl !!0 *(!0&))[]> class FunctionPointerBase`1<int64>::Get<[1]>()
        ldc.i4.1
        newarr method int64& *(method unmanaged cdecl !!0 *(int64&))
        newobj instance void class FunctionPointerTask`1<method int64& *(method unmanaged cdecl !!0 *(int64&))[]>::.ctor(!0)
        ret
    }
}

.class public auto ansi abstract sealed beforefieldinit ModifiedTaskCovariantReturn
       extends [System.Runtime]System.Object
{
    .method private hidebysig static class [System.Runtime]System.Threading.Tasks.Task`1<int32>
            AwaitModified() cil managed async
    {
        .maxstack 1
        newobj instance void ModifiedDerived::.ctor()
        callvirt instance class [System.Runtime]System.Threading.Tasks.Task`1<int32 modopt([System.Runtime]System.ObsoleteAttribute)> ModifiedBase::Get()
        call !!0 [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::Await<int32>(class [System.Runtime]System.Threading.Tasks.Task`1<!!0>)
        ret
    }

    .method public hidebysig static void ModifiedGenericArgumentTest() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 2

        call class [System.Runtime]System.Threading.Tasks.Task`1<int32> ModifiedTaskCovariantReturn::AwaitModified()
        callvirt instance !0 class [System.Runtime]System.Threading.Tasks.Task`1<int32>::get_Result()
        ldc.i4.s 42
        beq.s Ok

        ldstr "Modified generic argument dispatched to the wrong method"
        newobj instance void [System.Runtime]System.Exception::.ctor(string)
        throw

    Ok:
        ret
    }

    .method private hidebysig static class [System.Runtime]System.Threading.Tasks.Task`1<int32>
            AwaitSimpleFunctionPointers() cil managed async
    {
        .maxstack 1
        newobj instance void FunctionPointerDerived::.ctor()
        callvirt instance class [System.Runtime]System.Threading.Tasks.Task`1<method void *()[]> class FunctionPointerBase`1<int64>::GetSimple()
        call !!0 [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::Await<method void *()[]>(class [System.Runtime]System.Threading.Tasks.Task`1<!!0>)
        ldlen
        conv.i4
        ret
    }

    .method private hidebysig static class [System.Runtime]System.Threading.Tasks.Task`1<int32>
            AwaitFunctionPointers<U>() cil managed async
    {
        .maxstack 1
        newobj instance void FunctionPointerDerived::.ctor()
        callvirt instance class [System.Runtime]System.Threading.Tasks.Task`1<method !0& *(method unmanaged cdecl !!0 *(!0&))[]> class FunctionPointerBase`1<int64>::Get<!!0>()
        call !!0 [System.Runtime]System.Runtime.CompilerServices.AsyncHelpers::Await<method int64& *(method unmanaged cdecl !!0 *(int64&))[]>(class [System.Runtime]System.Threading.Tasks.Task`1<!!0>)
        ldlen
        conv.i4
        ret
    }

    .method public hidebysig static void FunctionPointerGenericArgumentTest() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack 2

        call class [System.Runtime]System.Threading.Tasks.Task`1<int32> ModifiedTaskCovariantReturn::AwaitSimpleFunctionPointers()
        callvirt instance !0 class [System.Runtime]System.Threading.Tasks.Task`1<int32>::get_Result()
        ldc.i4.1
        bne.un.s Fail

        call class [System.Runtime]System.Threading.Tasks.Task`1<int32> ModifiedTaskCovariantReturn::AwaitFunctionPointers<int32>()
        callvirt instance !0 class [System.Runtime]System.Threading.Tasks.Task`1<int32>::get_Result()
        ldc.i4.1
        bne.un.s Fail

        call class [System.Runtime]System.Threading.Tasks.Task`1<int32> ModifiedTaskCovariantReturn::AwaitFunctionPointers<int64>()
        callvirt instance !0 class [System.Runtime]System.Threading.Tasks.Task`1<int32>::get_Result()
        ldc.i4.1
        beq.s Ok

    Fail:
        ldstr "Function-pointer generic argument dispatched to the wrong method"
        newobj instance void [System.Runtime]System.Exception::.ctor(string)
        throw

    Ok:
        ret
    }

    .method public hidebysig static int32 Main() cil managed
    {
        .entrypoint
        .maxstack 1
        .locals init (int32 exitCode)

        .try
        {
            call void ModifiedTaskCovariantReturn::ModifiedGenericArgumentTest()
            call void ModifiedTaskCovariantReturn::FunctionPointerGenericArgumentTest()
            ldc.i4.s 100
            stloc.0
            leave.s Done
        }
        catch [System.Runtime]System.Exception
        {
            pop
            ldc.i4.s 101
            stloc.0
            leave.s Done
        }

    Done:
        ldloc.0
        ret
    }
}
