// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime {}
.assembly ReferenceAssemblyLifetimes
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.ReferenceAssemblyAttribute::.ctor() = ( 01 00 00 00 )
}
.module ReferenceAssemblyLifetimes.dll
.custom instance void [System.Runtime]System.Runtime.CompilerServices.RefSafetyRulesAttribute::.ctor(int32) = ( 01 00 0B 00 00 00 00 00 )

.class public sequential ansi sealed Carrier extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 )
    .field public int32& Item
}

.class public auto ansi ReferenceAssemblyLifetimesType extends [System.Runtime]System.Object
{
    .method public hidebysig static void WholeOutput_Valid([out] valuetype Carrier& destination, valuetype Carrier input) cil managed
    {
        ldarg.0
        ldarg.1
        stobj Carrier
        ret
    }

    .method public hidebysig static void UnknownLayout_Invalid_Unverifiable([out] valuetype Carrier& destination, int32& input) cil managed
    {
        ldarg.0
        ldarg.1
        stfld int32& Carrier::Item
        ret
    }
}
