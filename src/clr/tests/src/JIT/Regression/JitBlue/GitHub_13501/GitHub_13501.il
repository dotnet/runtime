// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


.assembly extern System.Console { auto }
.assembly extern mscorlib { auto }

.assembly GitHub_13501 { }

.class private Program extends [mscorlib]System.Object
{
    .method hidebysig static int32 Main() cil managed
    {
        .entrypoint
        .maxstack 8

    T1: // Test that add.ovf.un(native int(0), int32(-1)) produces native int(0xFFFFFFFF),
        // -1 is supposed to be treated as unsigned by add.ovf.un.
        // Test_Cast_gtFoldExprConst is supposed to result in the compile time evaluation
        // of the cast in gtFoldExprConst.

        ldc.i4 0
        conv.i
        ldc.i4 0
        call native int Program::Test_Cast_gtFoldExprConst(native int, bool)
        ldc.i4 -1
        conv.u
        bne.un FAIL

    T2: // Same test as above but using Test_Cast_EvalCastForConstantArgs. In this case
        // -1 is the result of a more complex expression that can be evaluated at compile
        // time by value numbering in EvalCastForConstantArgs.

        ldc.i4 0
        conv.i
        ldc.i4 0
        call native int Program::Test_Cast_EvalCastForConstantArgs(native int, bool)
        ldc.i4 -1
        conv.u
        bne.un FAIL

    T3: // Same test as above but using Test_Cast_Codegen. In this case -1 is passed
        // as a parameter so the cast cannot be evaluated at compile time and actual
        // cast code needs to be generated.

        ldc.i4 0
        conv.i
        ldc.i4 -1
        call native int Program::Test_Cast_Codegen(native int, int32)
        ldc.i4 -1
        conv.u
        bne.un FAIL

    PASS:
        ldstr "PASS"
        call void [System.Console]System.Console::WriteLine(string)
        ldc.i4 100
        ret
    FAIL:
        ldstr "FAIL"
        call void [System.Console]System.Console::WriteLine(string)
        ldc.i4 1
        ret
    }

    .method static native int Test_Cast_gtFoldExprConst(native int, bool) cil managed noinlining
    {
        .maxstack 4

        ldc.i4 -1
        ldarg.0
        add.ovf.un
        ret
    }

    .method static native int Test_Cast_EvalCastForConstantArgs(native int, bool) cil managed noinlining
    {
        .maxstack 4

        ldarg.1
        brtrue L1
        ldc.i4 -1
        br L2
    L1: ldc.i4 -1
    L2: ldarg.0
        add.ovf.un
        ret
    }

    .method static native int Test_Cast_Codegen(native int, int32) cil managed noinlining
    {
        .maxstack 4

        ldarg.1
        ldarg.0
        add.ovf.un
        ret
    }
}
