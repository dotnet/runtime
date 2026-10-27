// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib
{
    .publickeytoken = (B7 7A 5C 56 19 34 E0 89)
    .ver 4:0:0:0
}
.assembly TlsFieldLibrary {}
.module TlsFieldLibrary.dll

.data tls TlsData = int32(42)

.class public auto ansi beforefieldinit TlsField extends [mscorlib]System.Object
{
    .field public static int32 Value at TlsData

    .method public hidebysig static int32 Read() cil managed
    {
        .maxstack 1
        ldsfld int32 TlsField::Value
        ret
    }

    .method public hidebysig static void Write(int32 newValue) cil managed
    {
        .maxstack 1
        ldarg.0
        stsfld int32 TlsField::Value
        ret
    }

    .method public hidebysig static int32* GetAddress() cil managed
    {
        .maxstack 1
        ldsflda int32 TlsField::Value
        conv.u
        ret
    }
}
