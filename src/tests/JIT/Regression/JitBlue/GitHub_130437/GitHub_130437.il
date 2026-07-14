// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly extern System.Numerics.Vectors {}
.assembly extern System.Runtime.Intrinsics {}

.assembly GitHub_130437 {}
.module GitHub_130437.dll

.class private explicit ansi sealed beforefieldinit LargeFrame
       extends [mscorlib]System.ValueType
{
    .pack 1
    .size 8192
}

.class private auto ansi beforefieldinit GitHub_130437
       extends [mscorlib]System.Object
{
    .method public hidebysig static int32
        Main() cil managed
    {
        .entrypoint
        .maxstack 2

        call bool [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Arm.Sve2::get_IsSupported()
        brtrue.s SVE_SUPPORTED

        ldc.i4.s 100
        ret

    SVE_SUPPORTED:
        call int64 GitHub_130437::RunBasicScenario_UnsafeRead()
        ldc.i4.s 44
        conv.i8
        ceq
        brfalse.s FAIL

        ldc.i4.s 100
        ret

    FAIL:
        ldc.i4.m1
        ret
    }

    .method private hidebysig static int64
        RunBasicScenario_UnsafeRead() cil managed noinlining
    {
        .maxstack 4
        .locals init (
            // The mask local must be allocated above the large frame local so the
            // SP-based predicate spill/reload uses a large positive offset.
            [0] valuetype [System.Numerics.Vectors]System.Numerics.Vector`1<int64> mask,
            [1] valuetype LargeFrame largeFrame,
            [2] uint32[] baseArray,
            [3] int64[] maskArray,
            [4] int64[] offsetsArray,
            [5] uint32* pinned basePinned,
            [6] int64* pinned maskPinned,
            [7] int64* pinned offsetsPinned,
            [8] valuetype [System.Numerics.Vectors]System.Numerics.Vector`1<int64> result1,
            [9] valuetype [System.Numerics.Vectors]System.Numerics.Vector`1<int64> result2,
            [10] int32 index)

        ldc.i4 1024
        newarr [mscorlib]System.UInt32
        stloc.2

        ldloc.2
        ldc.i4.0
        ldc.i4.s 20
        stelem.i4

        ldc.i4.s 64
        newarr [mscorlib]System.Int64
        stloc.3

        ldc.i4.0
        stloc.s index

    FILL_MASK:
        ldloc.s index
        ldc.i4.s 64
        bge.s FILL_MASK_DONE

        ldloc.3
        ldloc.s index
        ldc.i4.1
        conv.i8
        stelem.i8

        ldloc.s index
        ldc.i4.1
        add
        stloc.s index
        br.s FILL_MASK

    FILL_MASK_DONE:
        ldc.i4.s 64
        newarr [mscorlib]System.Int64
        stloc.s offsetsArray

        ldloc.2
        ldc.i4.0
        ldelema [mscorlib]System.UInt32
        stloc.s basePinned

        ldloc.3
        ldc.i4.0
        ldelema [mscorlib]System.Int64
        stloc.s maskPinned

        ldloc.s offsetsArray
        ldc.i4.0
        ldelema [mscorlib]System.Int64
        stloc.s offsetsPinned

        ldloc.s maskPinned
        ldobj valuetype [System.Numerics.Vectors]System.Numerics.Vector`1<int64>
        stloc.0

        ldloca.s largeFrame
        call void GitHub_130437::TouchLargeFrame(valuetype LargeFrame&)

        ldloc.0
        ldloc.s basePinned
        ldloc.s offsetsPinned
        ldobj valuetype [System.Numerics.Vectors]System.Numerics.Vector`1<int64>
        call valuetype [System.Numerics.Vectors]System.Numerics.Vector`1<int64> [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Arm.Sve2::GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(valuetype [System.Numerics.Vectors]System.Numerics.Vector`1<int64>, uint32*, valuetype [System.Numerics.Vectors]System.Numerics.Vector`1<int64>)
        stloc.s result1

        ldloc.0
        ldloc.s basePinned
        ldloc.s offsetsPinned
        ldobj valuetype [System.Numerics.Vectors]System.Numerics.Vector`1<int64>
        call valuetype [System.Numerics.Vectors]System.Numerics.Vector`1<int64> [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Arm.Sve2::GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal(valuetype [System.Numerics.Vectors]System.Numerics.Vector`1<int64>, uint32*, valuetype [System.Numerics.Vectors]System.Numerics.Vector`1<int64>)
        stloc.s result2

        ldloca.s result1
        ldc.i4.0
        call instance !0 valuetype [System.Numerics.Vectors]System.Numerics.Vector`1<int64>::get_Item(int32)

        ldloca.s result2
        ldc.i4.0
        call instance !0 valuetype [System.Numerics.Vectors]System.Numerics.Vector`1<int64>::get_Item(int32)
        add

        ldloca.s largeFrame
        call int64 GitHub_130437::SumLargeFrame(valuetype LargeFrame&)
        add
        ret
    }

    .method private hidebysig static void
        TouchLargeFrame(valuetype LargeFrame& frame) cil managed noinlining
    {
        .maxstack 3

        ldarg.0
        conv.u
        ldc.i4.0
        add
        ldc.i4.1
        stind.i1

        ldarg.0
        conv.u
        ldc.i4 4096
        add
        ldc.i4.2
        stind.i1

        ldarg.0
        conv.u
        ldc.i4 8191
        add
        ldc.i4.3
        stind.i1

        ret
    }

    .method private hidebysig static int64
        SumLargeFrame(valuetype LargeFrame& frame) cil managed noinlining
    {
        .maxstack 3

        ldarg.0
        conv.u
        ldc.i4.0
        add
        ldind.u1
        conv.i8

        ldarg.0
        conv.u
        ldc.i4 8191
        add
        ldind.u1
        conv.i8
        add

        ret
    }
}
