// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
    .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
}

.assembly InlineArrayByRefLike { }

.class private sequential ansi sealed beforefieldinit InlineArrayOfSpans
    extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = {
        int32(2)
    }
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
        01 00 00 00
    )

    .field assembly valuetype [System.Runtime]System.Span`1<uint8> _element0

    .method public hidebysig specialname rtspecialname instance void .ctor(
        valuetype [System.Runtime]System.Span`1<uint8> first,
        valuetype [System.Runtime]System.Span`1<uint8> second) cil managed
    {
        .maxstack 2

        ldarg.0
        ldarg.1
        stfld valuetype [System.Runtime]System.Span`1<uint8> InlineArrayOfSpans::_element0

        ldarg.0
        ldflda valuetype [System.Runtime]System.Span`1<uint8> InlineArrayOfSpans::_element0
        sizeof valuetype [System.Runtime]System.Span`1<uint8>
        conv.i
        add
        ldarg.2
        stobj valuetype [System.Runtime]System.Span`1<uint8>
        ret
    }
}

.class private auto ansi abstract sealed beforefieldinit Program
    extends [System.Runtime]System.Object
{
    .field private static object s_sink

    .method private hidebysig static valuetype [System.Runtime]System.Span`1<uint8> CreateSpan(
        int32 x,
        int32 length) cil managed noinlining
    {
        .maxstack 3
        .locals init (
            uint8[] values
        )

        ldarg.1
        newarr [System.Runtime]System.Byte
        stloc.0

        ldloc.0
        ldarg.0
        conv.u1
        call void [System.Runtime]System.Array::Fill<uint8>(!!0[], !!0)

        ldloc.0
        call valuetype [System.Runtime]System.Span`1<!0> valuetype [System.Runtime]System.Span`1<uint8>::op_Implicit(!0[])
        ret
    }

    .method private hidebysig static int32 SumValues(
        valuetype InlineArrayOfSpans spans) cil managed noinlining
    {
        .maxstack 3
        .locals init (
            valuetype [System.Runtime]System.Span`1<uint8> first,
            valuetype [System.Runtime]System.Span`1<uint8> second,
            int32 sum,
            int32 index
        )

        newobj instance void [System.Runtime]System.Object::.ctor()
        stsfld object Program::s_sink

        ldarga.s spans
        ldfld valuetype [System.Runtime]System.Span`1<uint8> InlineArrayOfSpans::_element0
        stloc.0

        ldarga.s spans
        ldflda valuetype [System.Runtime]System.Span`1<uint8> InlineArrayOfSpans::_element0
        sizeof valuetype [System.Runtime]System.Span`1<uint8>
        conv.i
        add
        ldobj valuetype [System.Runtime]System.Span`1<uint8>
        stloc.1

        ldc.i4.0
        stloc.2
        ldc.i4.0
        stloc.3
        br.s FIRST_CHECK

    FIRST_ADD:
        ldloc.2
        ldloca.s first
        ldloc.3
        call instance !0& valuetype [System.Runtime]System.Span`1<uint8>::get_Item(int32)
        ldind.u1
        add
        stloc.2

        ldloc.3
        ldc.i4.1
        add
        stloc.3

    FIRST_CHECK:
        ldloc.3
        ldloca.s first
        call instance int32 valuetype [System.Runtime]System.Span`1<uint8>::get_Length()
        blt.s FIRST_ADD

        ldc.i4.0
        stloc.3
        br.s SECOND_CHECK

    SECOND_ADD:
        ldloc.2
        ldloca.s second
        ldloc.3
        call instance !0& valuetype [System.Runtime]System.Span`1<uint8>::get_Item(int32)
        ldind.u1
        add
        stloc.2

        ldloc.3
        ldc.i4.1
        add
        stloc.3

    SECOND_CHECK:
        ldloc.3
        ldloca.s second
        call instance int32 valuetype [System.Runtime]System.Span`1<uint8>::get_Length()
        blt.s SECOND_ADD

        ldloc.2
        ret
    }

    .method public hidebysig static int32 Main() cil managed
    {
        .entrypoint
        .maxstack 2
        .locals init (
            valuetype [System.Runtime]System.Span`1<uint8> first,
            valuetype [System.Runtime]System.Span`1<uint8> second,
            valuetype InlineArrayOfSpans spans
        )

        ldc.i4.s 10
        ldc.i4.4
        call valuetype [System.Runtime]System.Span`1<uint8> Program::CreateSpan(int32, int32)
        stloc.0

        ldc.i4.s 20
        ldc.i4.3
        call valuetype [System.Runtime]System.Span`1<uint8> Program::CreateSpan(int32, int32)
        stloc.1

        ldloc.0
        ldloc.1
        newobj instance void InlineArrayOfSpans::.ctor(
            valuetype [System.Runtime]System.Span`1<uint8>,
            valuetype [System.Runtime]System.Span`1<uint8>)
        stloc.2

        ldloc.2
        call int32 Program::SumValues(valuetype InlineArrayOfSpans)
        ret
    }
}
