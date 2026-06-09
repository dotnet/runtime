// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly BaseTypeTests
{
}

.class public auto ansi beforefieldinit ObjectTypeSpecBase_InvalidType_InvalidBaseType
    extends object
{
    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        .maxstack 8

        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}

.class public auto ansi beforefieldinit ArrayTypeSpecBase_InvalidType_InvalidBaseType
    extends int32[]
{
}

.class public auto ansi beforefieldinit PointerTypeSpecBase_InvalidType_InvalidBaseType
    extends int32*
{
}

.class interface public auto ansi abstract NilBaseInterface_ValidType_Valid
{
}

.class public auto ansi beforefieldinit GenericBase`1<T>
    extends [System.Runtime]System.Object
{
}

.class public auto ansi beforefieldinit GenericClassTypeSpecBase_ValidType_Valid
    extends class GenericBase`1<int32>
{
}

.class public auto ansi beforefieldinit GenericOpenClassTypeSpecBase_ValidType_Valid`1<T>
    extends class GenericBase`1<!0>
{
}

.class public sequential ansi sealed beforefieldinit ValueTypeBase
    extends [System.Runtime]System.ValueType
{
}

.class public auto ansi beforefieldinit ValueTypeBase_InvalidType_InvalidBaseType
    extends valuetype ValueTypeBase
{
}

.class public sequential ansi sealed beforefieldinit GenericValueTypeBase`1<T>
    extends [System.Runtime]System.ValueType
{
}

.class public auto ansi beforefieldinit GenericValueTypeSpecBase_InvalidType_InvalidBaseType
    extends valuetype GenericValueTypeBase`1<int32>
{
}
