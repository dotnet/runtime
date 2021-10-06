// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// That was a repro for a wrong IL stack optimization that happen when all conditions were met:
//   - debug code, so we spill entries often,
//   - we reimport blocks, `reimportSpillClique = true`,
//   - there is a value that is popped and we were bashing it to nop(wrong optimization);
//   - during reimportation we were asked to spill 'nop' value to the stack.

.assembly Test1 {}
.assembly extern mscorlib{auto}
.class FullProof {
    .method public static int32  Main()
    {
        .maxstack 8
        .entrypoint

        IL_0000: ldc.r8 1.0
        IL_0003: brtrue  IL_0007

        IL_0005: ldc.i4.2
        IL_0006: ret

        IL_0007: ldc.i4.s 100
        IL_0009: ret
    }
}
