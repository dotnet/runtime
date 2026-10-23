// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime {}
.assembly extern xunit.core {}
.assembly ExecutableData {}
// This is obviously not IL-only
.corflags 0

// mov eax, 100; ret, padded to keep each code blob eight bytes long.
.data cil X86Code = bytearray (B8 64 00 00 00 C3 90 90)
// mov w0, 100; ret
.data cil Arm64Code = bytearray (80 0C 80 52 C0 03 5F D6)

.class public ExecutableData extends [System.Runtime]System.Object
{
    .field private static int64 X86 at X86Code
    .field private static int64 Arm64 at Arm64Code

    .method public static int32 Main() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (01 00 00 00)
        .entrypoint
        .maxstack 1

        call valuetype [System.Runtime]System.Runtime.InteropServices.Architecture [System.Runtime]System.Runtime.InteropServices.RuntimeInformation::get_ProcessArchitecture()
        switch (X86, X64, UNSUPPORTED, ARM64)
    UNSUPPORTED:
        ldstr "Unsupported process architecture."
        newobj instance void [System.Runtime]System.PlatformNotSupportedException::.ctor(string)
        throw

    X86:
    X64:
        ldsflda int64 ExecutableData::X86
        br.s CALL
    ARM64:
        ldsflda int64 ExecutableData::Arm64
    CALL:
        conv.u
        calli unmanaged cdecl int32()
        ret
    }
}
