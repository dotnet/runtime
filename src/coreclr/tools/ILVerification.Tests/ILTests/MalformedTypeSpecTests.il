// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly MalformedTypeSpecTests
{
}

.class public auto ansi beforefieldinit GenericBase`1<T>
    extends [System.Runtime]System.Object
{
}

.class public auto ansi beforefieldinit CustomModifierTypeSpecBase_InvalidType_None
    extends class GenericBase`1<int32 modopt(int32)>
{
}

.class public auto ansi beforefieldinit MethodSignatureTests
    extends [System.Runtime]System.Object
{
    .method public static hidebysig int32 modopt(int32) CustomModifierTypeSpecReturn_Invalid_None() cil managed
    {
        ldc.i4.0
        ret
    }
}
