// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Runtime { }
.assembly extern System.Numerics.Vectors { }
.assembly GitHub_21761 { }

// Some basic tests to ensure that the JIT handles non-zero
// initialization correctly.

.class sequential sealed Point extends [System.Runtime]System.ValueType
{
    .field public int32 X
    .field public int32 Y
}

.method static bool ConstFixedSizeInitBlk() cil managed noinlining
{
    .locals init (valuetype Point a)

    ldloca a
    ldc.i4 42
    sizeof Point
    initblk

    ldloca a
    ldind.i4
    ldc.i4 0x2a2a2a2a
    ceq
    ret
}

.method static bool NonConstFixedSizeInitBlk() cil managed noinlining
{
    .locals init (valuetype Point a, int32 s)

    ldloca a
    ldloc s
    sizeof Point
    initblk

    ldloca a
    ldfld int32 Point::X
    ldc.i4 0
    ceq
    ret
}

.class sequential sealed Point64 extends [System.Runtime]System.ValueType
{
    .field public int64 X
    .field public int64 Y
}

.method static bool ConstFixedSizeInitBlk64() cil managed noinlining
{
    .locals init (valuetype Point64 a)

    ldloca a
    ldc.i4 42
    sizeof Point64
    initblk

    ldloca a
    ldind.i8
    ldc.i8 0x2a2a2a2a2a2a2a2a
    ceq
    ret
}

.method static bool NonConstFixedSizeInitBlk64() cil managed noinlining
{
    .locals init (valuetype Point64 a, int32 s)

    ldloca a
    ldloc s
    sizeof Point64
    initblk

    ldloca a
    ldfld int64 Point64::X
    ldc.i8 0
    ceq
    ret
}

// Small int promoted fields are supposed to be "normalize on load" so
// no special care is needed when initializing them. Still, make sure
// that field by field initialization handles small ints correctly.

.class sequential sealed SmallInts extends [System.Runtime]System.ValueType
{
    .field public int8 I8;
    .field public uint8 U8;
    .field public int16 I16;
}

.method static bool SmallIntsInitBlk() cil managed noinlining
{
    .locals init (valuetype SmallInts a)

    ldloca a
    ldc.i4 42
    sizeof SmallInts
    initblk

    ldloca a
    ldind.i1
    ldc.i4 0x2a
    bne.un FAIL

    ldloca a
    ldind.u1
    ldc.i4 0x2a
    bne.un FAIL

    ldloca a
    ldind.i2
    ldc.i4 0x2a2a
    bne.un FAIL

    ldc.i4 1
    ret

FAIL:
    ldc.i4 0
    ret
}

.method static bool SmallIntsSignedInitBlk() cil managed noinlining
{
    .locals init (valuetype SmallInts a)

    ldloca a
    ldc.i4 0x8a
    sizeof SmallInts
    initblk

    ldloca a
    ldind.i1
    ldc.i4 0xFFFFFF8a
    bne.un FAIL

    ldloca a
    ldind.u1
    ldc.i4 0x8a
    bne.un FAIL

    ldloca a
    ldind.i2
    ldc.i4 0xFFFF8a8a
    bne.un FAIL

    ldc.i4 1
    ret

FAIL:
    ldc.i4 0
    ret
}

// If floating point fields are involved, special care is needed
// since floating point constants having a required bit pattern
// have to be generated.

.class sequential sealed F32Vec3 extends [System.Runtime]System.ValueType
{
    .field public float32 X
    .field public float32 Y
    .field public float32 Z
}

// Make sure that the JIT produces an appropiate floating point constant.
// JIT's internal representation uses doubles to store constants, even
// when constants are float-typed. This means that the JIT should first
// create a float value having the required bit pattern and then convert
// it double. Directly creating a double having the required bit pattern
// is not valid.

.method static bool Float32InitBlk() cil managed noinlining
{
    .locals init (valuetype F32Vec3 a)

    ldloca a
    ldc.i4 42
    sizeof F32Vec3
    initblk

    ldloca a
    ldind.i4
    ldc.i4 0x2a2a2a2a
    ceq
    ret
}

// Initializing a float value with 255, 255, 255... is a special case
// because the result is a NaN. And since the JIT uses double to store
// float constants, this means that the JIT may end up producing a float
// NaN value, convert it to double and then convert it back to float
// during codegen. Will the NaN payload be preserved through conversions?
// This may depend on the host's floating point implementation.

.method static bool Float32NaNInitBlk() cil managed noinlining
{
    .locals init (valuetype F32Vec3 a)

    ldloca a
    ldc.i4 255
    sizeof F32Vec3
    initblk

    ldloca a
    ldind.i4
    ldc.i4 0xFFFFFFFF
    ceq
    ret
}

// Non-zero initialization of a GC reference is not exactly a valid scenario.
// Still, the JIT shouldn't end up generating invalid IR (non-zero GC typed
// constant nodes).

.class sequential sealed Pair extends [System.Runtime]System.ValueType
{
    .field public int64 Key
    .field public class [System.Runtime]System.Object Value
}

.method static bool ObjRefInitBlk() cil managed noinlining
{
    .locals init (valuetype Pair a)

    ldloca a
    ldc.i4 1
    sizeof Pair
    initblk

    ldloca a
    ldind.i8
    ldc.i8 0x0101010101010101
    ceq
    ret
}

// Non-zero SIMD constants are not supported so field by field initialization
// should not be attempted.

.class sequential sealed Wrapper extends [System.Runtime]System.ValueType
{
    .field public valuetype [System.Numerics.Vectors]System.Numerics.Vector4 Value
}

.method static bool SimdInitBlk() cil managed noinlining
{
    .locals init (valuetype Wrapper a, valuetype [System.Numerics.Vectors]System.Numerics.Vector4 v, float32 len)

    ldloca a
    ldc.i4 42
    ldc.i4 16
    initblk

    ldloca a
    ldflda valuetype [System.Numerics.Vectors]System.Numerics.Vector4 Wrapper::Value
    call instance float32 [System.Numerics.Vectors]System.Numerics.Vector4::Length()
    stloc len

    ldloca a
    ldfld valuetype [System.Numerics.Vectors]System.Numerics.Vector4 Wrapper::Value
    stloc v

    ldloca v
    ldind.i4
    ldc.i4 0x2a2a2a2a
    ceq
    ret
}

.method hidebysig static int32 Main() cil managed
{
    .entrypoint
    .locals init (valuetype Point a)

    call bool ConstFixedSizeInitBlk()
    brfalse FAIL

    call bool NonConstFixedSizeInitBlk()
    brfalse FAIL

    call bool ConstFixedSizeInitBlk64()
    brfalse FAIL

    call bool NonConstFixedSizeInitBlk64()
    brfalse FAIL

    call bool SmallIntsInitBlk()
    brfalse FAIL

    call bool SmallIntsSignedInitBlk()
    brfalse FAIL

    call bool Float32InitBlk()
    brfalse FAIL

    call bool Float32NaNInitBlk()
    brfalse FAIL

    call bool ObjRefInitBlk()
    brfalse FAIL
    
    call bool SimdInitBlk()
    brfalse FAIL

    ldc.i4 100
    ret
 FAIL:
    ldc.i4 1
    ret
}
