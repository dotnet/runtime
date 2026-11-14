// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime {}
.assembly RefSafetyRulesUnknown {}
.module RefSafetyRulesUnknown.dll
.custom instance void [System.Runtime]System.Runtime.CompilerServices.RefSafetyRulesAttribute::.ctor(int32) = ( 01 00 0C 00 00 00 00 00 )

.class public auto ansi RefSafetyRulesUnknownType extends [System.Runtime]System.Object
{
    .method public hidebysig static int32& UnknownVersion_Invalid_Unverifiable(int32& input) cil managed
    {
        ldarg.0
        ret
    }
}
