// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly DelegateTests
{
}

.class public auto ansi sealed ByteEnum
       extends [System.Runtime]System.Enum
{
    .field public specialname rtspecialname uint8 value__
    .field public static literal valuetype ByteEnum A = uint8(0)
    .field public static literal valuetype ByteEnum B = uint8(0)
}

.class private auto ansi beforefieldinit DelegateTestsType
       extends [System.Runtime]System.Object
{
    // assignment from Func<int, string> to Func<int, object> is valid
    .method private hidebysig instance class [System.Runtime]System.Func`2<int32,object>
          DelegateAssignmentReturn_Valid(class [System.Runtime]System.Func`2<int32,string> input) cil managed
    {
        ldarg.1
        ret
    }

    // assignment from Func<object, int> to Func<string, int> is valid
    .method private hidebysig instance class [System.Runtime]System.Func`2<string,int32>
          DelegateAssignmentParameter_Valid(class [System.Runtime]System.Func`2<object,int32> input) cil managed
    {
        ldarg.1
        ret
    }

    // assignment from Func<string, int> to Func<object, int> is invalid
    .method private hidebysig instance class [System.Runtime]System.Func`2<object,int32>
          DelegateAssignmentParameter_Invalid_StackUnexpected(class [System.Runtime]System.Func`2<string,int32> input) cil managed
    {
        ldarg.1
        ret
    }

    // assignment from Func<int, object> to Func<int, string> is invalid
    .method private hidebysig instance class [System.Runtime]System.Func`2<int32,string>
          DelegateAssignment_Invalid_StackUnexpected(class [System.Runtime]System.Func`2<int32,object> input) cil managed
    {
        ldarg.1
        ret
    }

    // assignment from Func<int> to Func<byte> is invalid
    .method private hidebysig instance class [System.Runtime]System.Func`1<uint8>
          AssignIntFuncToByteFunc_Invalid_StackUnexpected(class [System.Runtime]System.Func`2<int32> input) cil managed
    {
        ldarg.1
        ret
    }

    // assignment from Func<byte> to Func<int> is invalid
    .method private hidebysig instance class [System.Runtime]System.Func`1<int32>
          AssignByteFuncToIntFunc_Invalid_StackUnexpected(class [System.Runtime]System.Func`1<uint8> input) cil managed
    {
        ldarg.1
        ret
    }

    // assignment from Func<ByteEnum> to Func<byte> is invalid
    .method private hidebysig instance class [System.Runtime]System.Func`1<uint8>
          AssignByteActionToIntAction_Invalid_StackUnexpected(class [System.Runtime]System.Func`1<valuetype ByteEnum> input) cil managed
    {
        ldarg.1
        ret
    }

    // assignment from Action<byte> to Action<int> is invalid
    .method private hidebysig instance class [System.Runtime]System.Action`1<int32>
          AssignByteActionToIntAction_Invalid_StackUnexpected(class [System.Runtime]System.Action`1<uint8> input) cil managed
    {
        ldarg.1
        ret
    }

    // assignment from Action<ByteEnum> to Action<byte> is invalid
    .method private hidebysig instance class [System.Runtime]System.Action`1<uint8>
          AssignByteActionToByteEnumAction_Invalid_StackUnexpected(class [System.Runtime]System.Action`1<valuetype ByteEnum> input) cil managed
    {
        ldarg.1
        ret
    }

    .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
    {
        ldarg.0
        call       instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}
