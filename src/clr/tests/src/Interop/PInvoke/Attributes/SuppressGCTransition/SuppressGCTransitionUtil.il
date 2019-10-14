// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { }
.assembly extern System.Private.CoreLib { auto }

.assembly SuppressGCTransitionUtil { }

.class public auto ansi beforefieldinit FunctionPointer
       extends [System.Private.CoreLib]System.Object
{
    .method hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
        ldarg.0
        call instance void [System.Private.CoreLib]System.Object::.ctor()
        ret
    }

    .method public hidebysig static int32  Call_NextUInt(native int fptr,
                                                 int32* n) cil managed
    {
        .custom instance void [System.Private.CoreLib]System.Runtime.InteropServices.SuppressGCTransitionAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack  8
        ldarg.1
        ldarg.0
        calli unmanaged stdcall int32 (int32*)
        ret
    }
}


