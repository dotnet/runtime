// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly ExtendedLayoutTypes { }

// Invalid type examples

.class flags(0x18) public value beforefieldinit NoExtendedAttributeOnType
{
    .field public int32 a
}

.class flags(0x18) public value beforefieldinit InlineArrayOnExtendedLayout
{
    .custom instance void [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutAttribute::.ctor(valuetype [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutKind) = {
        int32(0x00000000) // CStruct
    }

    .custom instance void [System.Runtime]System.Runtime.CompilerServices.InlineArrayAttribute::.ctor(int32) = {
        int32(0x00000004)
    }

    .field public int32 a
}

.class flags(0x18) public value beforefieldinit ExtendedLayoutInvalidKind
{
    .custom instance void [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutAttribute::.ctor(valuetype [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutKind) = {
        int32(-2) // Unknown kind
    }

    .field public int32 a
}


.class flags(0x18) public value beforefieldinit CStructWithOffsets
{
    .custom instance void [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutAttribute::.ctor(valuetype [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutKind) = {
        int32(0x00000000) // CStruct
    }

    .field [4] public int32 a
}

.class flags(0x18) public value beforefieldinit CStructWithSize
{
    .custom instance void [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutAttribute::.ctor(valuetype [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutKind) = {
        int32(0x00000000) // CStruct
    }

    .field public int32 a

    .size 12
}

.class flags(0x18) public value beforefieldinit CStructWithPack
{
    .custom instance void [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutAttribute::.ctor(valuetype [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutKind) = {
        int32(0x00000000) // CStruct
    }

    .field public int8 a
    .field public int32 b

    .pack 1
}

.class flags(0x18) public value beforefieldinit CUnionWithOffsets
{
    .custom instance void [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutAttribute::.ctor(valuetype [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutKind) = {
        int32(0x00000001) // CUnion
    }

    .field [4] public int32 a
}

.class flags(0x18) public value beforefieldinit CUnionWithSize
{
    .custom instance void [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutAttribute::.ctor(valuetype [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutKind) = {
        int32(0x00000001) // CUnion
    }

    .field public int32 a

    .size 12
}

.class flags(0x18) public value beforefieldinit CUnionWithPack
{
    .custom instance void [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutAttribute::.ctor(valuetype [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutKind) = {
        int32(0x00000001) // CUnion
    }

    .field public uint8 a
    .field public int32 b

    .pack 1
}

