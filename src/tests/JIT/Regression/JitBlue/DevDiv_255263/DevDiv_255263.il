// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly initblk.exe{}

.class public sealed Foo
    extends [mscorlib]System.ValueType
{
    .field public int64 i1
    .field public int64 i2
    .field public int64 i3
    .field public uint8 b
}

.class DevDiv_255263 {


//    .data d = int32(0x00000000)
	.field static valuetype Foo f
        .field static float64 d

//-------------------------
// Entry point - Main
//-------------------------
.method static int32 Main(class [mscorlib]System.String[])
{
.entrypoint
    .maxstack  10
    .locals init (class [mscorlib]System.Random V_0,
             float64 V_1,
             float64 V_2,
             int32 V_3)

        // Get some floating point values and keep them live to avoid
        // ymm0 being available.
        newobj     instance void [mscorlib]System.Random::.ctor()
        stloc.0
        ldloc.0
        callvirt   instance float64 [mscorlib]System.Random::NextDouble()
        stloc.1
        ldloc.0
        callvirt   instance float64 [mscorlib]System.Random::NextDouble()
        stloc.2

	// -- init 25 bytes of memory to 0xAA
	ldsflda		valuetype Foo DevDiv_255263::f
	ldc.i4		0xAA
	ldc.i4		25
	unaligned. 0x1
	initblk

        // Now use the floating point values.
        ldloc.1
        ldloc.2
        add
        stsfld          float64 DevDiv_255263::d
        ldc.i4.s        100
        stloc.3

	// -- load the 25 bytes of memory _and be sure they are all 0xAA
	ldsflda		valuetype Foo DevDiv_255263::f
        ldfld           int64 Foo::i1
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ceq
	brfalse		FAIL

	ldsflda		valuetype Foo DevDiv_255263::f
        ldfld           int64 Foo::i2
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ceq
	brfalse		FAIL
	ldsflda		valuetype Foo DevDiv_255263::f
        ldfld           int64 Foo::i3
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ceq
	brfalse		FAIL
	ldsflda		valuetype Foo DevDiv_255263::f
        ldfld           uint8 Foo::b
	ldc.i4		0x000000AA
	ceq
	brfalse		FAIL

//----------------
PASS:
	ldc.i4	0x64
	ret
//----------------
FAIL:
	ldc.i4	0xFFFFFFFF
	ret
}
}
