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

    .method public hidebysig instance void LoadElement.RefFromNull_Valid() cil managed
    {
        .locals init (
            [0] string
        )

        // string s = ((string[])null)[0];

        ldnull
        ldc.i4.0
        ldelem.ref
        stloc.0
        ret
    }

    .method public hidebysig instance void LoadElement.Int32FromString_Invalid_ExpectedArray() cil managed
    {
        ldstr       "a"
        ldc.i4.0
        ldelem.i4
        pop
        ret
    }

    .method public hidebysig instance void LoadElement.RefFromString_Invalid_ExpectedArray() cil managed
    {
        ldstr       "a"
        ldc.i4.0
        ldelem.ref
        pop
        ret
    }

    .method public hidebysig instance void LoadElement.TokenFromString_Invalid_ExpectedArray() cil managed
    {
        ldstr       "a"
        ldc.i4.0
        ldelem      [System.Runtime]System.Int32
        pop
        ret
    }

    .method public hidebysig instance void LoadElement.RefFromInt32_Invalid_ExpectedArray() cil managed
    {
        ldc.i4.0
        ldc.i4.0
        ldelem.ref
        pop
        ret
    }

    .method public hidebysig instance void StoreElement.Int32IntoString_Invalid_ExpectedArray() cil managed
    {
        ldstr       "a"
        ldc.i4.0
        ldc.i4.0
        stelem.i4
        ret
    }

    .method public hidebysig instance void StoreElement.RefIntoString_Invalid_ExpectedArray() cil managed
    {
        ldstr       "a"
        ldc.i4.0
        ldnull
        stelem.ref
        ret
    }

    .method public hidebysig instance void StoreElement.TokenIntoString_Invalid_ExpectedArray() cil managed
    {
        ldstr       "a"
        ldc.i4.0
        ldc.i4.0
        stelem      [System.Runtime]System.Int32
        ret
    }

    .method public hidebysig instance void LoadElementAddress.FromString_Invalid_ExpectedArray() cil managed
    {
        ldstr       "a"
        ldc.i4.0
        ldelema     [System.Runtime]System.Int32
        pop
        ret
    }
}
