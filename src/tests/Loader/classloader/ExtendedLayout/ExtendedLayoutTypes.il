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

// CStruct types below (can be moved to C# once we have ExtendedLayout support in the C# compiler)

.class flags(0x18) public value beforefieldinit CStructBlittablePrimitiveFields
{
    .custom instance void [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutAttribute::.ctor(valuetype [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutKind) = {
        int32(0x00000000) // CStruct
    }

    .field public int32 a
    .field public float32 b
    .field public uint8 c
}

.class flags(0x18) public value beforefieldinit CStructNonBlittablePrimitiveFields
{
    .custom instance void [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutAttribute::.ctor(valuetype [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutKind) = {
        int32(0x00000000) // CStruct
    }

    .field public bool b
    .field public char c
}

.class flags(0x18) public value beforefieldinit CStructWithReferenceFields
{
    .custom instance void [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutAttribute::.ctor(valuetype [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutKind) = {
        int32(0x00000000) // CStruct
    }

    .field public class [System.Runtime]System.String a
}

.class flags(0x18) public value beforefieldinit CStructWithMixedFields
{
    .custom instance void [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutAttribute::.ctor(valuetype [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutKind) = {
        int32(0x00000000) // CStruct
    }

    .field public int32 a
    .field public class [System.Runtime]System.String b
}

.class flags(0x18) public value beforefieldinit NestedCStructType
{
    .custom instance void [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutAttribute::.ctor(valuetype [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutKind) = {
        int32(0x00000000) // CStruct
    }
    .field public int32 x
}

.class flags(0x18) public value beforefieldinit CStructCustomCStructField
{
    .custom instance void [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutAttribute::.ctor(valuetype [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutKind) = {
        int32(0x00000000) // CStruct
    }

    .field public valuetype NestedCStructType y
}

.class public sequential value beforefieldinit NestedSequentialType
{
    .field public int32 x
}

.class flags(0x18) public value beforefieldinit CStructCustomSeqStructField
{
    .custom instance void [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutAttribute::.ctor(valuetype [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutKind) = {
        int32(0x00000000) // CStruct
    }

    .field public valuetype NestedSequentialType y
}

.class public auto value beforefieldinit NestedAutoLayoutType
{
    .field public int32 x
}

.class flags(0x18) public value beforefieldinit CStructCustomAutoStructField
{
    .custom instance void [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutAttribute::.ctor(valuetype [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutKind) = {
        int32(0x00000000) // CStruct
    }

    .field public valuetype NestedAutoLayoutType y
}

.class flags(0x18) public value beforefieldinit EmptyCStruct
{
    .custom instance void [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutAttribute::.ctor(valuetype [System.Runtime]System.Runtime.InteropServices.ExtendedLayoutKind) = {
        int32(0x00000000) // CStruct
    }
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
