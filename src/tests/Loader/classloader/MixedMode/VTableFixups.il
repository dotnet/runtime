// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime {}
.assembly VTableFixups {}
.corflags 0

#ifdef ARM64
#define SLOT "int64"
#else
#ifdef X64
#define SLOT "int64"
#else
#define SLOT "int32"
#endif
#endif

.vtfixup [2] SLOT at Managed
.vtfixup [2] SLOT fromunmanaged at Unmanaged
.vtfixup [1] SLOT retainappdomain at Retained
.vtfixup [0] SLOT at Empty

.data Managed = SLOT(-1)[2]
.data Unmanaged = SLOT(-1)[2]
.data Retained = SLOT(-1)
.data Empty = SLOT(0)

.class public VTableFixups extends [System.Runtime]System.Object
{
    .field private static native int ManagedSlots at Managed
    .field private static native int UnmanagedSlots at Unmanaged
    .field private static native int RetainedSlot at Retained

    .method public static native int GetTable(int32 index) cil managed
    {
        ldarg.0
        switch (MANAGED, UNMANAGED, RETAINED)
        newobj instance void [System.Runtime]System.ArgumentOutOfRangeException::.ctor()
        throw
    MANAGED:
        ldsflda native int VTableFixups::ManagedSlots
        ret
    UNMANAGED:
        ldsflda native int VTableFixups::UnmanagedSlots
        ret
    RETAINED:
        ldsflda native int VTableFixups::RetainedSlot
        ret
    }

    // Method order deliberately differs from slot order.
    .method public static int32 Subtract(int32 input) cil managed
    {
        .vtentry 1 : 2
        ldarg.0
        ldc.i4.s 20
        sub
        ret
    }

    .method public static int32 Add(int32 input) cil managed
    {
        .vtentry 1 : 1
        ldarg.0
        ldc.i4.s 10
        add
        ret
    }

    .method public static int32 Double(int32 input) cil managed
    {
        .vtentry 2 : 1
        .export [7] as Twice
        ldarg.0
        ldc.i4.2
        mul
        ret
    }

    .method public static int32 Triple(int32 input) cil managed
    {
        .vtentry 2 : 2
        .export [8]
        ldarg.0
        ldc.i4.3
        mul
        ret
    }

    .method public static int32 Retain(int32 input) cil managed
    {
        .vtentry 3 : 1
        ldarg.0
        ldc.i4.s 100
        add
        ret
    }
}
