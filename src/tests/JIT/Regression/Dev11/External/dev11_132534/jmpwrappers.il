// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib
{
    .publickeytoken = (B7 7A 5C 56 19 34 E0 89)
    .ver 4:0:0:0
}

.assembly extern CSharpPart {}

.assembly jmpwrappers {}

.module jmpwrappers.dll
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       
.corflags 0x00000001    



.class
    public abstract auto ansi sealed beforefieldinit
    Test.ILJmpWrappers
        extends [mscorlib]System.Object
{
    .method public static void Pass6Args_Maxstack_2
        (
            int32 A_0,
            int64 A_1,
            float32 A_2,
            float64 A_3,
            valuetype [CSharpPart]Test.BasicStruct A_4,
            string A_5
        )
        cil managed noinlining
    {

        .maxstack 2

        ldarg       A_0
        starg       A_0

        jmp
            void
            [CSharpPart]Test.CalleeSide::Pass6Args_Maxstack_2(
                int32,
                int64,
                float32,
                float64,
                valuetype [CSharpPart]Test.BasicStruct,
                string
            )

        ret
    }
    .method public static void Pass6Args_Maxstack_3
        (
            int32 A_0,
            int64 A_1,
            float32 A_2,
            float64 A_3,
            valuetype [CSharpPart]Test.BasicStruct A_4,
            string A_5
        )
        cil managed noinlining
    {

        .maxstack 3

        ldarg       A_0
        starg       A_0

        jmp
            void
            [CSharpPart]Test.CalleeSide::Pass6Args_Maxstack_3(
                int32,
                int64,
                float32,
                float64,
                valuetype [CSharpPart]Test.BasicStruct,
                string
            )

        ret
    }
    .method public static void Pass6Args_Maxstack_4
        (
            int32 A_0,
            int64 A_1,
            float32 A_2,
            float64 A_3,
            valuetype [CSharpPart]Test.BasicStruct A_4,
            string A_5
        )
        cil managed noinlining
    {

        .maxstack 4

        ldarg       A_0
        starg       A_0

        jmp
            void
            [CSharpPart]Test.CalleeSide::Pass6Args_Maxstack_4(
                int32,
                int64,
                float32,
                float64,
                valuetype [CSharpPart]Test.BasicStruct,
                string
            )

        ret
    }
    .method public static void Pass6Args_Maxstack_5
        (
            int32 A_0,
            int64 A_1,
            float32 A_2,
            float64 A_3,
            valuetype [CSharpPart]Test.BasicStruct A_4,
            string A_5
        )
        cil managed noinlining
    {

        .maxstack 5

        ldarg       A_0
        starg       A_0

        jmp
            void
            [CSharpPart]Test.CalleeSide::Pass6Args_Maxstack_5(
                int32,
                int64,
                float32,
                float64,
                valuetype [CSharpPart]Test.BasicStruct,
                string
            )

        ret
    }
    .method public static void Pass6Args_Maxstack_6
        (
            int32 A_0,
            int64 A_1,
            float32 A_2,
            float64 A_3,
            valuetype [CSharpPart]Test.BasicStruct A_4,
            string A_5
        )
        cil managed noinlining
    {

        .maxstack 6

        ldarg       A_0
        starg       A_0

        jmp
            void
            [CSharpPart]Test.CalleeSide::Pass6Args_Maxstack_6(
                int32,
                int64,
                float32,
                float64,
                valuetype [CSharpPart]Test.BasicStruct,
                string
            )

        ret
    }
    .method public static void Pass6Args_Maxstack_7
        (
            int32 A_0,
            int64 A_1,
            float32 A_2,
            float64 A_3,
            valuetype [CSharpPart]Test.BasicStruct A_4,
            string A_5
        )
        cil managed noinlining
    {

        .maxstack 7

        ldarg       A_0
        starg       A_0

        jmp
            void
            [CSharpPart]Test.CalleeSide::Pass6Args_Maxstack_7(
                int32,
                int64,
                float32,
                float64,
                valuetype [CSharpPart]Test.BasicStruct,
                string
            )

        ret
    }
    .method public static void Pass6Args_Maxstack_8
        (
            int32 A_0,
            int64 A_1,
            float32 A_2,
            float64 A_3,
            valuetype [CSharpPart]Test.BasicStruct A_4,
            string A_5
        )
        cil managed noinlining
    {

        .maxstack 8

        ldarg       A_0
        starg       A_0

        jmp
            void
            [CSharpPart]Test.CalleeSide::Pass6Args_Maxstack_8(
                int32,
                int64,
                float32,
                float64,
                valuetype [CSharpPart]Test.BasicStruct,
                string
            )

        ret
    }
    .method public static void Pass6Args_Maxstack_9
        (
            int32 A_0,
            int64 A_1,
            float32 A_2,
            float64 A_3,
            valuetype [CSharpPart]Test.BasicStruct A_4,
            string A_5
        )
        cil managed noinlining
    {

        .maxstack 9

        ldarg       A_0
        starg       A_0

        jmp
            void
            [CSharpPart]Test.CalleeSide::Pass6Args_Maxstack_9(
                int32,
                int64,
                float32,
                float64,
                valuetype [CSharpPart]Test.BasicStruct,
                string
            )

        ret
    }
    .method public static void Pass6Args_Maxstack_10
        (
            int32 A_0,
            int64 A_1,
            float32 A_2,
            float64 A_3,
            valuetype [CSharpPart]Test.BasicStruct A_4,
            string A_5
        )
        cil managed noinlining
    {

        .maxstack 10

        ldarg       A_0
        starg       A_0

        jmp
            void
            [CSharpPart]Test.CalleeSide::Pass6Args_Maxstack_10(
                int32,
                int64,
                float32,
                float64,
                valuetype [CSharpPart]Test.BasicStruct,
                string
            )

        ret
    }
}
