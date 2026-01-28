// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly InvalidCSharp { }

.class public sequential ansi sealed beforefieldinit InvalidCSharp.InvalidStructWithRefField
    extends [System.Runtime]System.ValueType
{
    // Type requires IsByRefLikeAttribute to be valid.
    .field public string& Invalid
}

.class public explicit ansi sealed beforefieldinit InvalidCSharp.InvalidRefFieldAlignment
    extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
        01 00 00 00
    )
    .field [0] public int16 Field
    .field [2] public int32& Invalid
}

.class public explicit ansi sealed beforefieldinit InvalidCSharp.InvalidObjectRefRefFieldOverlap
    extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
        01 00 00 00
    )
    .field [0] public object Field
    .field [0] public int32& Invalid
}

// This is invalid metadata and is unable to be loaded.
// -  [field sig] (0x80131815 (VER_E_FIELD_SIG))
//
// .class public sequential ansi sealed beforefieldinit InvalidCSharp.InvalidStructWithStaticRefField
//     extends [System.Runtime]System.ValueType
// {
//     .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
//         01 00 00 00
//     )
//     .field public static string& Invalid
// }

.class public explicit ansi sealed beforefieldinit InvalidCSharp.IntPtrRefFieldOverlap
    extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
        01 00 00 00
    )
    .field [0] public native int Field
    .field [0] public int32& Invalid
}

.class public sequential ansi sealed beforefieldinit InvalidCSharp.InnerValidByRef
    extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
        01 00 00 00
    )
    .field public int32& Field
    .field public int32 Size
}

.class public explicit ansi sealed beforefieldinit InvalidCSharp.IntPtrOverlapWithInnerFieldType
    extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
        01 00 00 00
    )
    .field [0] public native int Field
    // This field's valid Type would illegally overlap this type's first field using a precise GC.
    .field [0] public valuetype InvalidCSharp.InnerValidByRef Invalid
}
