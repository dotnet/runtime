// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly InvalidCSharpNegative { }

.class public sequential ansi sealed beforefieldinit ByRefLikeType
    extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
        01 00 00 00
    )
}

//
// Begin invalid
//

.class public sequential ansi sealed beforefieldinit InvalidCSharpNegative.GenericClass_Invalid`1<T>
    extends [System.Runtime]System.Object
{
    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}

.class interface public auto ansi abstract InvalidCSharpNegative.GenericInterface_Invalid`1<T>
{
}

.class public sequential ansi sealed beforefieldinit InvalidCSharpNegative.GenericValueType_Invalid`1<T>
    extends [System.Runtime]System.ValueType
{
}

// Invalid generic substitution of non-allow-byreflike with allow-byreflike
.class interface public auto ansi abstract InvalidCSharpNegative.GenericDerivedInterface_Invalid`1<byreflike T>
    implements class InvalidCSharpNegative.GenericInterface_Invalid`1<!T>
{
}

.class public sequential ansi sealed beforefieldinit InvalidCSharpNegative.GenericDerivedClass_Invalid`1<byreflike T>
    extends class InvalidCSharpNegative.GenericClass_Invalid`1<!T>
{
}

.class public sequential ansi sealed beforefieldinit InvalidCSharpNegative.GenericValueTypeWrapper_Invalid`1<byreflike T>
    extends [System.Runtime]System.ValueType
{
    .field public valuetype InvalidCSharpNegative.GenericValueType_Invalid`1<!T> fld;
}

.class public auto ansi beforefieldinit InvalidCSharpNegative.BaseClassWithGenericMethod
    extends [System.Runtime]System.Object
{
    .method public hidebysig newslot virtual
        instance void AcceptsByRefLike<byreflike T> () cil managed
    {
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

.class public auto ansi beforefieldinit InvalidCSharpNegative.DerivedClassWithGenericMethod_Invalid
    extends InvalidCSharpNegative.BaseClassWithGenericMethod
{
    .method public hidebysig static
        class InvalidCSharpNegative.BaseClassWithGenericMethod Create () cil managed noinlining
    {
        newobj instance void InvalidCSharpNegative.DerivedClassWithGenericMethod_Invalid::.ctor()
        ret
    }
    .method public hidebysig virtual
        instance void AcceptsByRefLike<T> () cil managed // Missing constraint
    {
        ret
    }
    .method private hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        ldarg.0
        call instance void InvalidCSharpNegative.BaseClassWithGenericMethod::.ctor()
        ret
    }
}

// Entry points

.class public auto ansi abstract sealed beforefieldinit Exec
    extends [System.Runtime]System.Object
{
    .method public hidebysig static
        string TypeSubstitutionInterfaceImplementationAllowByRefLikeIntoNonByRefLike() cil managed
    {
        ldtoken class InvalidCSharpNegative.GenericDerivedInterface_Invalid`1<valuetype ByRefLikeType>
        call class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
        callvirt instance string [System.Runtime]System.Object::ToString()
        ret
    }

    .method public hidebysig static
        string TypeSubstitutionInheritanceAllowByRefLikeIntoNonByRefLike() cil managed
    {
        ldtoken class InvalidCSharpNegative.GenericDerivedClass_Invalid`1<valuetype ByRefLikeType>
        call class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
        callvirt instance string [System.Runtime]System.Object::ToString()
        ret
    }

    .method public hidebysig static
        string TypeSubstitutionFieldAllowByRefLikeIntoNonByRefLike() cil managed
    {
        ldtoken valuetype InvalidCSharpNegative.GenericValueTypeWrapper_Invalid`1<valuetype ByRefLikeType>
        call class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
        callvirt instance string [System.Runtime]System.Object::ToString()
        ret
    }

    .method public hidebysig static
        void OverrideMethodNotByRefLike() cil managed
    {
        .locals init (
            [0] class InvalidCSharpNegative.BaseClassWithGenericMethod
        )
        call class InvalidCSharpNegative.BaseClassWithGenericMethod InvalidCSharpNegative.DerivedClassWithGenericMethod_Invalid::Create()
        stloc.0
        ldloc.0
        callvirt instance void InvalidCSharpNegative.BaseClassWithGenericMethod::AcceptsByRefLike<valuetype ByRefLikeType>()
        ret
    }
}