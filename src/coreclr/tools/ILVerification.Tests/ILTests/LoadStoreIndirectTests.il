// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly LoadStoreIndirectTests
{
}

.class public auto ansi beforefieldinit LoadStoreIndirectTestsType
       extends [System.Runtime]System.Object
{
    .method static public hidebysig void LoadIndirectRef.FromStringRef_Valid(string&) cil managed
    {
        ldarg.0
        ldind.ref
        pop
        ret
    }

    .method static public hidebysig void LoadIndirect.FromInt32Ref_Valid(int32&) cil managed
    {
        ldarg.0
        ldind.i4
        pop
        ret
    }

    .method static public hidebysig void LoadIndirect.FromInt64Ref_Invalid_StackUnexpected(int64&) cil managed
    {
        ldarg.0
        ldind.i4
        pop
        ret
    }

    .method static public hidebysig void LoadIndirectRef.FromInt64Ref_Invalid_StackUnexpected(int64&) cil managed
    {
        ldarg.0
        ldind.ref
        pop
        ret
    }

    .method static public hidebysig void LoadIndirectRef.AssignRefStringToString_Valid(string&) cil managed
    {
        .locals init (
            string V_0)
        ldarg.0
        ldind.ref
        stloc.0
        ret
    }

    .method static public hidebysig void StoreIndirect.AssignToInt_Valid(int32&) cil managed
    {
        // arg = 0;
        ldarg.0
        ldc.i4.0
        stind.i4
        ret
    }

    .method static public hidebysig void StoreIndirect.AssignToRefBase_Valid(object&) cil managed
    {
        .locals init (string V_0)

        // ref object a;
        // string b;
        // a = b;

        ldarg.0
        ldloc.0
        stind.ref
        ret
    }

    .method static public hidebysig void StoreIndirect.AssignToRefString_Invalid_StackUnexpected(string&) cil managed
    {
        .locals init (object V_0)

        // ref string x;
        // object y;
        // x = y;

        ldarg.0
        ldloc.0
        stind.ref
        ret
    }

    .method static public hidebysig void StoreIndirect.AssignNullToRefString_Valid(string&) cil managed
    {
        // ref string x;
        // x = null;

        ldarg.0
        ldnull
        stind.ref
        ret
    }

    .method static public hidebysig void StoreIndirect.AssignStringToRefString_Valid(string&) cil managed
    {
        // ref string x;
        // string y = "a";
        // x = y;

        ldarg.0
        ldstr   "a"
        stind.ref
        ret
    }

    .method static public hidebysig void StoreObject.AssignStringToRefStringAsObject_Invalid_StackUnexpected(string&) cil managed
    {
        // ref string x;
        // string y = "a";
        // x = y;

        ldarg.0
        ldstr   "a"
        stobj   [System.Runtime]System.Object
        ret
    }

    .method static public hidebysig void StoreIndirect.AssignByteToBoolRef_Valid(bool&) cil managed
    {
        ldarg.0
        ldc.i4.0
        stind.i1
        ret
    }

    .method static public hidebysig void StoreObject.ValidTypeToken_Valid() cil managed
    {
        .locals init (object V_0)

        ldloca.s    V_0
        ldstr       "Hello"
        stobj       string
        ret
    }

    .method static public hidebysig void StoreObject.InvalidTypeToken_Invalid_StackUnexpected() cil managed
    {
        .locals init (object V_0)

        ldloca.s    V_0
        ldstr       "Hello"
        stobj       [System.Runtime]System.IO.Stream
        ret
    }

    .method static public hidebysig void LoadIndirect.AssignInt8ToBool_Valid(bool&) cil managed
    {
        .locals init (bool)

        // ref bool a;
        // bool b;
        // b = a;

        ldarg.0
        ldind.i1
        stloc.0
        ret
    }

    .method static public hidebysig void LoadIndirect.AssignUInt8ToBool_Valid(bool&) cil managed
    {
        .locals init (bool)

        // ref bool a;
        // bool b;
        // b = a;

        ldarg.0
        ldind.u1
        stloc.0
        ret
    }

    .method static public hidebysig void LoadIndirect.AssignInt16ToChar_Valid(char&) cil managed
    {
        .locals init (char)

        // ref char a;
        // char b;
        // b = a;

        ldarg.0
        ldind.i2
        stloc.0
        ret
    }

    .method static public hidebysig void LoadIndirect.AssignUInt16ToChar_Valid(char&) cil managed
    {
        .locals init (char)

        // ref char a;
        // char b;
        // b = a;

        ldarg.0
        ldind.u2
        stloc.0
        ret
    }

    .method static public hidebysig void StoreIndirect.AssignInt32ToCharRef_Valid(char&) cil managed
    {
        .locals init (char)

        // ref char a;
        // a = 'a';

        ldarg.0
        ldc.i4.s 97
        stind.i2
        ret
    }

    .method static public hidebysig void StoreIndirect.AssignInt32ToBoolRef_Valid(bool&) cil managed
    {
        .locals init (bool)

        // ref bool a;
        // a = true;

        ldarg.0
        ldc.i4.1
        stind.i1
        ret
    }
}
