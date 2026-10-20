// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// TypeSpecs that start with custom modifiers. C# encodes the 'unmanaged' constraint as
// modreq(System.Runtime.InteropServices.UnmanagedType) class System.ValueType

.assembly extern System.Runtime
{
}

.assembly TypeSpecModifierTests
{
}

.class public auto ansi beforefieldinit TypeSpecModifierTests
    extends [System.Runtime]System.Object
{
    // where T : unmanaged
    .method public hidebysig static !!T UnmanagedConstraintMethod<valuetype .ctor (class [System.Runtime]System.ValueType modreq([System.Runtime]System.Runtime.InteropServices.UnmanagedType)) T>(!!T 'value') cil managed
    {
        ldarg.0
        ret
    }

    // A custom modifier followed by a primitive type: still not a valid TypeSpec.
    .method public hidebysig static void ModifiedPrimitiveConstraintMethod<(int32 modopt([System.Runtime]System.Runtime.CompilerServices.IsConst)) T>() cil managed
    {
        ret
    }

    // A custom modifier followed by BYREF: still not a valid TypeSpec.
    .method public hidebysig static void ModifiedByRefConstraintMethod<(int32& modreq([System.Runtime]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
    {
        ret
    }

    // A custom modifier followed by PINNED: still not a valid TypeSpec.
    .method public hidebysig static void ModifiedPinnedConstraintMethod<(class [System.Runtime]System.ValueType pinned modreq([System.Runtime]System.Runtime.InteropServices.UnmanagedType)) T>() cil managed
    {
        ret
    }

    .method public hidebysig static int32 CallUnmanagedConstraintMethod_Valid() cil managed
    {
        ldc.i4.1
        call !!0 TypeSpecModifierTests::UnmanagedConstraintMethod<int32>(!!0)
        ret
    }

    .method public hidebysig static void CallModifiedPrimitiveConstraintMethod_Invalid_BadTypeSpec() cil managed
    {
        call void TypeSpecModifierTests::ModifiedPrimitiveConstraintMethod<int32>()
        ret
    }

    .method public hidebysig static void CallModifiedByRefConstraintMethod_Invalid_BadTypeSpec() cil managed
    {
        call void TypeSpecModifierTests::ModifiedByRefConstraintMethod<int32>()
        ret
    }

    .method public hidebysig static void CallModifiedPinnedConstraintMethod_Invalid_BadTypeSpec() cil managed
    {
        call void TypeSpecModifierTests::ModifiedPinnedConstraintMethod<int32>()
        ret
    }

    // Without custom modifiers, a class needs no TypeSpec: 'class' here makes ilasm emit the TypeSpec CLASS System.GC.
    .method public hidebysig static void CallThroughClassTypeSpec_Invalid_BadTypeSpec() cil managed
    {
        call void class [System.Runtime]System.GC::Collect()
        ret
    }
}
