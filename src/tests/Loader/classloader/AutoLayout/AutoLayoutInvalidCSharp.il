// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly AutoLayoutInvalidCSharp { }

.class public sealed auto AutoLayoutTypeWithFieldsWithOffsets extends [System.Runtime]System.ValueType
{
    .field [4] public int32 i
    .field [0] public int32 j
    .method public hidebysig specialname rtspecialname
            instance void  .ctor() cil managed
    {
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance void [System.Runtime]System.ValueType::.ctor()
        IL_0006:  ret
    }
}