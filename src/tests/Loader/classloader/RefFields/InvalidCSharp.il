// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly InvalidCSharp { }

.class public auto ansi sealed beforefieldinit InvalidCSharp.InvalidStructWithRefField
    extends [System.Runtime]System.ValueType
{
    // Type requires IsByRefLikeAttribute to be valid.
    .field public string& invalid
}

// This is invalid metadata and is unable to be loaded.
// -  [field sig] (0x80131815 (VER_E_FIELD_SIG))
//
// .class public auto ansi sealed beforefieldinit InvalidCSharp.InvalidStructWithStaticRefField
//     extends [System.Runtime]System.ValueType
// {
//     .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
//         01 00 00 00
//     )
//     .field public static string& invalid
// }

.class public auto ansi sealed beforefieldinit InvalidCSharp.WithRefField
    extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
        01 00 00 00
    )
    .field public string& Str

    .method public hidebysig specialname rtspecialname
        instance void .ctor (
            string&
        ) cil managed
    {
        ldarg.0
        ldarg.1
        stfld string& InvalidCSharp.WithRefField::Str
        ret
    }

    .method public hidebysig
        instance bool ConfirmFieldInstance (
            string
        ) cil managed
    {
        ldarg.0
        ldfld string& InvalidCSharp.WithRefField::Str
        ldind.ref
        ldarg.1
        ceq
        ret
    }
}

.class public auto ansi sealed beforefieldinit InvalidCSharp.WithRefStructField
    extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
        01 00 00 00
    )
    .field public valuetype InvalidCSharp.WithRefField& Field

    .method public hidebysig specialname rtspecialname
        instance void .ctor (
            valuetype InvalidCSharp.WithRefField&
        ) cil managed
    {
        ldarg.0
        ldarg.1
        stfld valuetype InvalidCSharp.WithRefField& InvalidCSharp.WithRefStructField::Field
        ret
    }

    .method public hidebysig
        instance bool ConfirmFieldInstance (
            valuetype InvalidCSharp.WithRefField&
        ) cil managed
    {
        ldarg.0
        ldfld valuetype InvalidCSharp.WithRefField& InvalidCSharp.WithRefStructField::Field
        ldind.ref
        ldarg.1
        ldind.ref
        ceq
        ret
    }
}

.class public auto ansi sealed beforefieldinit InvalidCSharp.WithTypedReferenceField`1<T>
    extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
        01 00 00 00
    )
    .field public typedref Field

    .method public hidebysig specialname rtspecialname
        instance void .ctor (
            !T
        ) cil managed
    {
        ldarg.0
        ldarga.s 1
        mkrefany !T
        stfld typedref InvalidCSharp.WithTypedReferenceField`1::Field
        ret
    }

    .method public hidebysig
        instance class [System.Runtime]System.Type GetFieldType () cil managed
    {
        ldarg.0
        ldfld typedref InvalidCSharp.WithTypedReferenceField`1::Field
        refanytype
        call class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle )
        ret
    }
}