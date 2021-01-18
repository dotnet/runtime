// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Runtime { auto }

.assembly SuppressGCTransitionUtil { }

.class public auto ansi beforefieldinit FunctionPointer
       extends [System.Runtime]System.Object
{
    .method hidebysig specialname rtspecialname
            instance void .ctor() cil managed
    {
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }

    .method public hidebysig static int32  Call_NextUInt(native int fptr,
                                                 int32* n) cil managed
    {
        .custom instance void [System.Runtime]System.Runtime.InteropServices.SuppressGCTransitionAttribute::.ctor() = ( 01 00 00 00 )
        .maxstack  8
        ldarg.1
        ldarg.0
        calli unmanaged cdecl int32 (int32*)
        ret
    }
}


