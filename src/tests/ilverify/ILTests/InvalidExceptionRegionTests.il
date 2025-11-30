// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly InvalidExceptionRegionTests
{
}

.class public sequential ansi sealed beforefieldinit InvalidExceptionRegionTests
       extends [System.Runtime]System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed
    {
        .maxstack 1

        IL_0000: ldarg.0
        IL_0001: call instance void [System.Runtime]System.Object::.ctor()
        IL_0006: ret
    }

    // Test: Try region offset exceeds code size
    .method public hidebysig instance void TryOffset.ExceedsCodeSize_Invalid_EHClauseOutOfRange() cil managed
    {
        .maxstack 1

        IL_0000: ret
        // Try region starts at IL_1000 which is beyond code size (code size is 1)
        .try IL_1000 to IL_1001 catch [System.Runtime]System.Exception handler IL_0000 to IL_0001
    }

    // Test: Handler region offset exceeds code size
    .method public hidebysig instance void HandlerOffset.ExceedsCodeSize_Invalid_EHClauseOutOfRange() cil managed
    {
        .maxstack 1

        IL_0000: ret
        // Handler region starts at IL_1000 which is beyond code size (code size is 1)
        .try IL_0000 to IL_0001 catch [System.Runtime]System.Exception handler IL_1000 to IL_1001
    }

    // Test: Filter region offset exceeds code size
    .method public hidebysig instance void FilterOffset.ExceedsCodeSize_Invalid_EHClauseOutOfRange() cil managed
    {
        .maxstack 1

        IL_0000: ldc.i4.0
        IL_0001: endfilter
        IL_0002: leave.s IL_0004
        IL_0004: ret
        // Filter region starts at IL_1000 which is beyond code size (code size is 5)
        .try IL_0000 to IL_0002 filter IL_1000 handler IL_0002 to IL_0004
    }

    // Test: Try region end offset exceeds code size
    .method public hidebysig instance void TryEndOffset.ExceedsCodeSize_Invalid_EHClauseOutOfRange() cil managed
    {
        .maxstack 1

        IL_0000: nop
        IL_0001: ret
        // Try region ends at IL_1000 which is beyond code size (code size is 2)
        .try IL_0000 to IL_1000 catch [System.Runtime]System.Exception handler IL_0001 to IL_0002
    }

    // Test: Handler region end offset exceeds code size
    .method public hidebysig instance void HandlerEndOffset.ExceedsCodeSize_Invalid_EHClauseOutOfRange() cil managed
    {
        .maxstack 1

        IL_0000: nop
        IL_0001: ret
        // Handler region ends at IL_1000 which is beyond code size (code size is 2)
        .try IL_0000 to IL_0001 catch [System.Runtime]System.Exception handler IL_0001 to IL_1000
    }
}
