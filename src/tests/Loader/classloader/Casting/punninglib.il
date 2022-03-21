// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly punninglib { }

.class public sequential ansi sealed beforefieldinit Caller.Struct extends [System.Runtime]System.ValueType
{
    .field public int32 Field
}
.class public sequential ansi sealed beforefieldinit Caller.Struct`1<T> extends [System.Runtime]System.ValueType
{
    .field public int32 Field
}
.class public auto ansi abstract sealed beforefieldinit Caller.Class
    extends [System.Runtime]System.Object
{
    .method public hidebysig static
        int32 CallGetField (
            valuetype Caller.Struct s,
            native int callbackRaw,
            object inst
        ) cil managed
    {
        ldarg.2
        ldnull
        ceq
        brfalse.s FALSE

        TRUE: nop
        ldarg.0
        ldarg.1
        calli int32(valuetype Caller.Struct)
        br.s DONE

        FALSE: nop
        ldarg.2
        ldarg.0
        ldarg.1
        calli instance int32(valuetype Caller.Struct)
        br.s DONE

        DONE: ret
    }
    .method public hidebysig static
        int32 CallGetField<T> (
            valuetype Caller.Struct`1<!!T> s,
            native int callbackRaw,
            object inst
        ) cil managed
    {
        ldarg.2
        ldnull
        ceq
        brfalse.s FALSE

        TRUE: nop
        ldarg.0
        ldarg.1
        calli int32(valuetype Caller.Struct`1<!!T>)
        br.s DONE

        FALSE: nop
        ldarg.2
        ldarg.0
        ldarg.1
        calli instance int32(valuetype Caller.Struct`1<!!T>)
        br.s DONE

        DONE: ret
    }
}

//
// Used for GetFunctionPointer()
//
.class public sequential ansi sealed beforefieldinit A.Struct extends [System.Runtime]System.ValueType
{
    .field public int32 Field
}
.class public sequential ansi sealed beforefieldinit A.Struct`1<T> extends [System.Runtime]System.ValueType
{
    .field public int32 Field
}
.class public auto ansi beforefieldinit A.Class
    extends [System.Runtime]System.Object
{
    .method public hidebysig static
        int32 GetField (valuetype A.Struct s) cil managed
    {
        ldarg.0
        ldfld int32 A.Struct::Field
        ret
    }
    .method public hidebysig static
        int32 GetFieldGeneric<T> (valuetype A.Struct`1<!!T> s) cil managed
    {
        ldarg.0
        ldfld int32 valuetype A.Struct`1<!!T>::Field
        ret
    }
    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}

//
// Used for ldftn
//
.class public sequential ansi sealed beforefieldinit B.Struct extends [System.Runtime]System.ValueType
{
    .field public int32 Field
}
.class public sequential ansi sealed beforefieldinit B.Struct`1<T> extends [System.Runtime]System.ValueType
{
    .field public int32 Field
}
.class public sequential ansi sealed beforefieldinit B.Struct`2<T, U> extends [System.Runtime]System.ValueType
{
    .field public int32 Field
}
.class public sequential ansi sealed beforefieldinit B.Struct`3<T, U, V> extends [System.Runtime]System.ValueType
{
    .field public int32 Field
}
.class public sequential ansi sealed beforefieldinit B.Struct`4<T, U, V, W> extends [System.Runtime]System.ValueType
{
    .field public int32 Field
}
.class public auto ansi beforefieldinit B.Class
    extends [System.Runtime]System.Object
{
    .method public hidebysig static
        native int GetFunctionPointer () cil managed
    {
        ldftn int32 B.Class::GetField(valuetype B.Struct)
        call native int [System.Runtime]System.IntPtr::op_Explicit(void*)
        ret
    }
    .method public hidebysig static
        int32 GetField (valuetype B.Struct s) cil managed
    {
        ldarg.0
        ldfld int32 B.Struct::Field
        ret
    }
    .method public hidebysig static
        native int GetFunctionPointerGeneric () cil managed
    {
        ldftn int32 B.Class::GetFieldGeneric<object>(valuetype B.Struct`1<!!0>)
        call native int [System.Runtime]System.IntPtr::op_Explicit(void*)
        ret
    }
    .method public hidebysig static
        int32 GetFieldGeneric<T> (valuetype B.Struct`1<!!T> s) cil managed
    {
        ldarg.0
        ldfld int32 valuetype B.Struct`1<!!T>::Field
        ret
    }
    .method public hidebysig static
        native int GetFunctionPointer<T> () cil managed
    {
        ldftn int32 B.Class::GetFieldGeneric<!!T>(valuetype B.Struct`2<!!T, !!T>)
        call native int [System.Runtime]System.IntPtr::op_Explicit(void*)
        ret
    }
    .method public hidebysig static
        int32 GetFieldGeneric<T> (valuetype B.Struct`2<!!T, !!T> s) cil managed
    {
        ldarg.0
        ldfld int32 valuetype B.Struct`2<!!T, !!T>::Field
        ret
    }
    .method public hidebysig static
        native int GetFunctionPointerGeneric ( object inst ) cil managed
    {
        ldftn instance int32 B.Class::GetFieldGeneric<object>(valuetype B.Struct`3<!!0, !!0, !!0>)
        call native int [System.Runtime]System.IntPtr::op_Explicit(void*)
        ret
    }
    .method public hidebysig newslot virtual
        instance int32 GetFieldGeneric<T> (valuetype B.Struct`3<!!T, !!T, !!T> s) cil managed
    {
        ldarg.1
        ldfld int32 valuetype B.Struct`3<!!T, !!T, !!T>::Field
        ret
    }
    .method public hidebysig static
        native int GetFunctionPointer<T> ( object inst ) cil managed
    {
        ldftn instance int32 B.Class::GetFieldGeneric<!!T>(valuetype B.Struct`4<!!T, !!T, !!T, !!T>)
        call native int [System.Runtime]System.IntPtr::op_Explicit(void*)
        ret
    }
    .method public hidebysig newslot virtual
        instance int32 GetFieldGeneric<T> (valuetype B.Struct`4<!!T, !!T, !!T, !!T> s) cil managed
    {
        ldarg.1
        ldfld int32 valuetype B.Struct`4<!!T, !!T, !!T, !!T>::Field
        ret
    }
    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}
.class public auto ansi beforefieldinit B.Derived
    extends B.Class
{
    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        ldarg.0
        call instance void B.Class::.ctor()
        ret
    }
}

//
// Used for ldvirtftn
//
.class public sequential ansi sealed beforefieldinit C.Struct extends [System.Runtime]System.ValueType
{
    .field public int32 Field
}
.class public sequential ansi sealed beforefieldinit C.Struct`1<T> extends [System.Runtime]System.ValueType
{
    .field public int32 Field
}
.class public sequential ansi sealed beforefieldinit C.Struct`2<T, U> extends [System.Runtime]System.ValueType
{
    .field public int32 Field
}
.class public auto ansi beforefieldinit C.Class
    extends [System.Runtime]System.Object
{
    .method public hidebysig static
        native int GetFunctionPointer ( object inst ) cil managed
    {
        ldarg.0
        ldvirtftn instance int32 C.Class::GetField(valuetype C.Struct)
        call native int [System.Runtime]System.IntPtr::op_Explicit(void*)
        ret
    }
    .method public hidebysig newslot virtual
        instance int32 GetField (valuetype C.Struct s) cil managed
    {
        newobj instance void [System.Runtime]System.NotImplementedException::.ctor()
        throw
    }
    .method public hidebysig static
        native int GetFunctionPointerGeneric ( object inst ) cil managed
    {
        ldarg.0
        ldvirtftn instance int32 C.Class::GetFieldGeneric<object>(valuetype C.Struct`1<!!0>)
        call native int [System.Runtime]System.IntPtr::op_Explicit(void*)
        ret
    }
    .method public hidebysig newslot virtual
        instance int32 GetFieldGeneric<T> (valuetype C.Struct`1<!!T> s) cil managed
    {
        newobj instance void [System.Runtime]System.NotImplementedException::.ctor()
        throw
    }
    .method public hidebysig static
        native int GetFunctionPointer<T> ( object inst ) cil managed
    {
        ldarg.0
        ldvirtftn instance int32 C.Class::GetFieldGeneric<!!T>(valuetype C.Struct`2<!!T, !!T>)
        call native int [System.Runtime]System.IntPtr::op_Explicit(void*)
        ret
    }
    .method public hidebysig newslot virtual
        instance int32 GetFieldGeneric<T> (valuetype C.Struct`2<!!T, !!T> s) cil managed
    {
        newobj instance void [System.Runtime]System.NotImplementedException::.ctor()
        throw
    }
    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}
.class public auto ansi beforefieldinit C.Derived
    extends C.Class
{
    .method public hidebysig virtual
        instance int32 GetField (valuetype C.Struct s) cil managed
    {
        ldarg.1
        ldfld int32 C.Struct::Field
        ret
    }
    .method public hidebysig virtual
        instance int32 GetFieldGeneric<T> (valuetype C.Struct`1<!!T> s) cil managed
    {
        ldarg.1
        ldfld int32 valuetype C.Struct`1<!!T>::Field
        ret
    }
    .method public hidebysig virtual
        instance int32 GetFieldGeneric<T> (valuetype C.Struct`2<!!T, !!T> s) cil managed
    {
        ldarg.1
        ldfld int32 valuetype C.Struct`2<!!T, !!T>::Field
        ret
    }
    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        ldarg.0
        call instance void C.Class::.ctor()
        ret
    }
}
