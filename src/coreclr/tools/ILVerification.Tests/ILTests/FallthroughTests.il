// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly FallthroughTests
{
}

// In the invalid tests the last instruction is a conditional branch or switch back to the ret at IL_0002,
// so control falls through the end of the method when the branch is not taken. The valid control adds
// a ret after the conditional branch. The branch operands are numeric offsets.

.class public auto ansi beforefieldinit FallthroughTestsType
       extends [System.Runtime]System.Object
{
    .method public hidebysig static void BrTrueS.LastInstruction_Invalid_MethodFallthrough() cil managed
    {
        br.s        1               // IL_0000: to IL_0003
        ret                         // IL_0002
        ldc.i4.0                    // IL_0003
        brtrue.s    -4              // IL_0004: to IL_0002; the method ends at IL_0006
    }

    .method public hidebysig static void BrFalseS.LastInstruction_Invalid_MethodFallthrough() cil managed
    {
        br.s        1               // IL_0000: to IL_0003
        ret                         // IL_0002
        ldc.i4.1                    // IL_0003
        brfalse.s   -4              // IL_0004: to IL_0002; the method ends at IL_0006
    }

    .method public hidebysig static void BrTrue.LastInstruction_Invalid_MethodFallthrough() cil managed
    {
        br.s        1               // IL_0000: to IL_0003
        ret                         // IL_0002
        ldc.i4.0                    // IL_0003
        brtrue      -7              // IL_0004: to IL_0002; the method ends at IL_0009
    }

    .method public hidebysig static void Blt.LastInstruction_Invalid_MethodFallthrough() cil managed
    {
        br.s        1               // IL_0000: to IL_0003
        ret                         // IL_0002
        ldc.i4.1                    // IL_0003
        ldc.i4.0                    // IL_0004
        blt         -8              // IL_0005: to IL_0002; the method ends at IL_000a
    }

    .method public hidebysig static void Switch.LastInstruction_Invalid_MethodFallthrough() cil managed
    {
        br.s        1               // IL_0000: to IL_0003
        ret                         // IL_0002
        ldc.i4.1                    // IL_0003
        switch      (-11)           // IL_0004: to IL_0002; the method ends at IL_000d
    }

    .method public hidebysig static void BrTrueS.BeforeReturn_Valid() cil managed
    {
        br.s        1               // IL_0000: to IL_0003
        ret                         // IL_0002
        ldc.i4.0                    // IL_0003
        brtrue.s    -4              // IL_0004: to IL_0002
        ret                         // IL_0006
    }
}
