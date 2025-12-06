// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly ArrayTests
{
}

.class public auto ansi beforefieldinit ArrayTestsType
       extends [System.Runtime]System.Object
{

    .method public hidebysig instance void StoreElement.Int8IntoBoolArray_Valid() cil managed
    {
        // bool[] a = new bool[];
        // a[0] = true;

        ldc.i4.1
        newarr      [System.Runtime]System.Boolean
        ldc.i4.0
        ldc.i4.1
        stelem.i1
        ret
    }

    .method public hidebysig instance void StoreElement.Int16IntoCharArray_Valid() cil managed
    {
        // char[] a = new char[];
        // a[0] = 'a';

        ldc.i4.1
        newarr      [System.Runtime]System.Char
        ldc.i4.0
        ldc.i4.s    97
        stelem.i2
        ret
    }

    .method public hidebysig instance void LoadElement.UInt16FromCharArray_Valid() cil managed
    {
        .locals init (
            [0] char[],
            [1] char
        )

        // char[] a = new char[];
        // a[0] = 'a';
        // char b = a[0];

        ldc.i4.1
        newarr      [System.Runtime]System.Char
        stloc.0
        ldloc.0
        ldc.i4.0
        ldc.i4.s    97
        stelem.i2
        ldloc.0
        ldc.i4.0
        ldelem.u2
        stloc.1
        ret
    }

    .method public hidebysig instance void LoadElement.UInt8FromBoolArray_Valid() cil managed
    {
        .locals init (
            [0] bool[],
            [1] bool
        )

        // bool[] a = new bool[];
        // a[0] = true;
        // bool b = a[0];

        ldc.i4.1
        newarr      [System.Runtime]System.Boolean
        stloc.0
        ldloc.0
        ldc.i4.0
        ldc.i4.1
        stelem.i1
        ldloc.0
        ldc.i4.0
        ldelem.u1
        stloc.1
        ret
    }
}
