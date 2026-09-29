// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly InvalidCSharpInlineArray { }

.class public explicit ansi sealed beforefieldinit Explicit
    extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 01 00 00 00 00 00
    )
    .field [0] public valuetype [System.Runtime]System.Guid Guid
}

.class public sequential ansi sealed beforefieldinit ExplicitSize
    extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 01 00 00 00 00 00
    )

    .size 256
    .field [0] public valuetype [System.Runtime]System.Guid Guid
}

.class public auto ansi sealed beforefieldinit ExplicitSizeAuto
    extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 01 00 00 00 00 00
    )

    .size 256
    .field [0] public valuetype [System.Runtime]System.Guid Guid
}

.class public auto ansi sealed beforefieldinit ExplicitSizeGeneric`1<T>
    extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 01 00 00 00 00 00
    )

    .size 256
    .field [0] public valuetype [System.Runtime]System.Guid Guid
}

.class public sequential ansi sealed beforefieldinit ZeroLength
    extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 00 00 00 00 00 00
    )
    .field public int32 'field'
}

.class public sequential ansi sealed beforefieldinit NegativeLength
    extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 ff ff ff ff 00 00
    )
    .field public int64 'field'
}

.class public sequential ansi sealed beforefieldinit NoFields
    extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 7b 00 00 00 00 00
    )
    .pack 0
    .size 1
    .field public static int32 x
}

.class public sequential ansi sealed beforefieldinit TwoFields
    extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 01 00 00 00 00 00
    )
    .field private int32 a
    .field private int32 b
}
