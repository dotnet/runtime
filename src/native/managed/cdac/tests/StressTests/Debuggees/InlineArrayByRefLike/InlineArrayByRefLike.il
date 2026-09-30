// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
    .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
}

.assembly InlineArrayByRefLike { }

.class private sequential ansi sealed beforefieldinit ByRef
    extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
        01 00 00 00
    )

    .field assembly int32& _value

    .method public hidebysig specialname rtspecialname instance void .ctor(
        int32&) cil managed
    {
        .maxstack 2

        ldarg.0
        ldarg.1
        stfld int32& ByRef::_value
        ret
    }

    .method public hidebysig instance int32 GetValue() cil managed
    {
        .maxstack 1

        ldarg.0
        ldfld int32& ByRef::_value
        ldind.i4
        ret
    }
}

.class private sequential ansi sealed beforefieldinit InlineArrayOfByRefs
    extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = {
        int32(2)
    }
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
        01 00 00 00
    )

    .field assembly valuetype ByRef _element0

    .method public hidebysig specialname rtspecialname instance void .ctor(
        valuetype ByRef first,
        valuetype ByRef second) cil managed
    {
        .maxstack 2

        ldarg.0
        ldarg.1
        stfld valuetype ByRef InlineArrayOfByRefs::_element0

        ldarg.0
        ldflda valuetype ByRef InlineArrayOfByRefs::_element0
        sizeof valuetype ByRef
        conv.i
        add
        ldarg.2
        stobj valuetype ByRef
        ret
    }
}

.class private auto ansi abstract sealed beforefieldinit Program
    extends [System.Runtime]System.Object
{
    .field private static object s_sink

    .method private hidebysig static int32 SumValues(
        valuetype InlineArrayOfByRefs values) cil managed noinlining
    {
        .maxstack 2
        .locals init (
            valuetype ByRef first,
            valuetype ByRef second
        )

        ldc.i4 65536
        newarr [System.Runtime]System.Byte
        stsfld object Program::s_sink

        ldarga.s values
        ldfld valuetype ByRef InlineArrayOfByRefs::_element0
        stloc.0

        ldarga.s values
        ldflda valuetype ByRef InlineArrayOfByRefs::_element0
        sizeof valuetype ByRef
        conv.i
        add
        ldobj valuetype ByRef
        stloc.1

        ldloca.s first
        call instance int32 ByRef::GetValue()
        ldloca.s second
        call instance int32 ByRef::GetValue()
        add
        ret
    }

    .method public hidebysig static int32 Main() cil managed
    {
        .entrypoint
        .maxstack 3
        .locals init (
            int32[] firstValues,
            int32[] secondValues,
            valuetype ByRef first,
            valuetype ByRef second,
            valuetype InlineArrayOfByRefs values
        )

        ldc.i4.1
        newarr [System.Runtime]System.Int32
        stloc.0
        ldloc.0
        ldc.i4.0
        ldc.i4.s 40
        stelem.i4

        ldc.i4.1
        newarr [System.Runtime]System.Int32
        stloc.1
        ldloc.1
        ldc.i4.0
        ldc.i4.s 60
        stelem.i4

        ldloc.0
        ldc.i4.0
        ldelema [System.Runtime]System.Int32
        newobj instance void ByRef::.ctor(int32&)
        stloc.2

        ldloc.1
        ldc.i4.0
        ldelema [System.Runtime]System.Int32
        newobj instance void ByRef::.ctor(int32&)
        stloc.3

        ldloc.2
        ldloc.3
        newobj instance void InlineArrayOfByRefs::.ctor(
            valuetype ByRef,
            valuetype ByRef)
        stloc.s 4

        ldloc.s 4
        call int32 Program::SumValues(valuetype InlineArrayOfByRefs)
        ret
    }
}
