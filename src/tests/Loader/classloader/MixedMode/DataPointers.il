// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime {}
.assembly DataPointers {}

// Keep target offsets nonzero so omitted label fixups cannot pass.
.data Padding = int32(-1)
.data Before = int32(123)
.data Backward = &(Before)
.data Forward = &(After)
.data Self = &(Self)
.data cil CodePointers = { &(Before), &(After), &(Self) }
.data After = int32(456)

.class public DataPointers extends [System.Runtime]System.Object
{
    .field private static int32 Before at Before
    .field private static int32 After at After
    .field private static native int Backward at Backward
    .field private static native int Self at Self
    .field private static native int CodePointers at CodePointers

    .method private static void CheckTable(native int table) cil managed
    {
        .maxstack 3
        ldarg.0
        ldind.i
        ldsflda int32 DataPointers::Before
        conv.u
        bne.un.s BACKWARD_FAILED

        ldarg.0
        sizeof native int
        add
        ldind.i
        ldsflda int32 DataPointers::After
        conv.u
        bne.un.s FORWARD_FAILED

        ldarg.0
        sizeof native int
        ldc.i4.2
        mul
        add
        ldind.i
        ldsflda native int DataPointers::Self
        conv.u
        bne.un.s SELF_FAILED
        ret

    BACKWARD_FAILED:
        ldstr "Incorrect backward data-label pointer."
        br.s FAILED
    FORWARD_FAILED:
        ldstr "Incorrect forward data-label pointer."
        br.s FAILED
    SELF_FAILED:
        ldstr "Incorrect self data-label pointer."
    FAILED:
        newobj instance void [System.Runtime]System.Exception::.ctor(string)
        throw
    }

    .method public static int32 Run() cil managed
    {
        ldsflda native int DataPointers::Backward
        conv.u
        call void DataPointers::CheckTable(native int)
        ldsflda native int DataPointers::CodePointers
        conv.u
        call void DataPointers::CheckTable(native int)
        ldc.i4.s 100
        ret
    }
}
