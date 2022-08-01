// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern TypeImpl { auto }

.assembly PunningLib { }

.class public auto ansi abstract sealed beforefieldinit CreateFunctionPointer
    extends [System.Runtime]System.Object
{
    .method public hidebysig static
        int32 For_1( valuetype [TypeImpl]TypeEquivalenceTypes.OnlyLoadOnce_1 s ) cil managed
    {
        ldarg.0
        ldfld int32 [TypeImpl]TypeEquivalenceTypes.OnlyLoadOnce_1::Field
        ret
    }
    .method public hidebysig static
        native int For_2_Ldftn() cil managed
    {
        ldftn int32 CreateFunctionPointer::For_2( valuetype [TypeImpl]TypeEquivalenceTypes.OnlyLoadOnce_2 )
        call native int [System.Runtime]System.IntPtr::op_Explicit(void*)
        ret
    }
    .method private hidebysig static
        int32 For_2( valuetype [TypeImpl]TypeEquivalenceTypes.OnlyLoadOnce_2 s ) cil managed
    {
        ldarg.0
        ldfld int32 [TypeImpl]TypeEquivalenceTypes.OnlyLoadOnce_2::Field
        ret
    }
    .method public hidebysig static
        native int For_3_Ldvirtftn( [out] object& c ) cil managed
    {
        .locals ( [0] object )
        newobj instance void Derived::.ctor()
        stloc.0
        ldarg.0
        ldloc.0
        stind.ref
        ldloc.0
        ldvirtftn instance int32 Class::GetField (valuetype [TypeImpl]TypeEquivalenceTypes.OnlyLoadOnce_3 )
        call native int [System.Runtime]System.IntPtr::op_Explicit(void*)
        ret
    }
}

.class private auto ansi beforefieldinit Class
    extends [System.Runtime]System.Object
{
    .method public hidebysig newslot virtual
        instance int32 GetField (
            valuetype [TypeImpl]TypeEquivalenceTypes.OnlyLoadOnce_3 s
        ) cil managed
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

.class private auto ansi beforefieldinit Derived
    extends Class
{
    .method public hidebysig virtual
        instance int32 GetField (
            valuetype [TypeImpl]TypeEquivalenceTypes.OnlyLoadOnce_3 s
        ) cil managed
    {
        ldarg.1
        ldfld int32 [TypeImpl]TypeEquivalenceTypes.OnlyLoadOnce_3::Field
        ret
    }
    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        ldarg.0
        call instance void Class::.ctor()
        ret
    }
}