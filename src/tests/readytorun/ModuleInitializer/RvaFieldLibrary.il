// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly RvaFieldLibrary {}
.module RvaFieldLibrary.dll

.method private specialname rtspecialname static void .cctor() cil managed
{
    .maxstack 2
    ldstr "RvaFieldLibrary.Initialized"
    ldc.i4.1
    call void [mscorlib]System.AppContext::SetSwitch(string, bool)
    ret
}

.class public auto ansi beforefieldinit RvaFieldHolder extends [mscorlib]System.Object
{
    .field public static int32 Value at ValueData
}

.data ValueData = int32(42)
