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

.class public sequential ansi sealed beforefieldinit SpanArr
    extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = (
        01 00 64 00 00 00 00 00
    )

    .field private static literal int32 LengthConst = int32(100)
    .field public valuetype [System.Runtime]System.Span`1<object> element

    .method public hidebysig
        instance valuetype [System.Runtime]System.Span`1<object>* At (
            int32
        ) cil managed
    {
        .locals init (
            [0] valuetype [System.Runtime]System.Span`1<object>& pinned
        )
        ldarg.0
        ldflda valuetype [System.Runtime]System.Span`1<object> SpanArr::element
        stloc.0
        ldloc.0
        conv.u
        ldarg.1
        conv.i
        sizeof valuetype [System.Runtime]System.Span`1<object>
        mul
        add
        ret
    }

    .method public hidebysig specialname
        instance int32 get_Length () cil managed
    {
        ldc.i4.s 100
        ret
    }

    .property instance int32 Length()
    {
        .get instance int32 SpanArr::get_Length()
    }
}
