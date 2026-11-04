// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime {}
.assembly ExecutableData {}
// This is obviously not IL-only
.corflags 0

#ifdef ARM64
// mov w0, 100; ret
.data cil Code = bytearray (80 0C 80 52 C0 03 5F D6)
#else
// mov eax, 100; ret, padded to eight bytes.
.data cil Code = bytearray (B8 64 00 00 00 C3 90 90)
#endif

.class public ExecutableData extends [System.Runtime]System.Object
{
    .field private static int64 Code at Code

    .method public static int32 Run() cil managed
    {
        .maxstack 1

        ldsflda int64 ExecutableData::Code
        conv.u
        calli unmanaged cdecl int32()
        ret
    }
}
