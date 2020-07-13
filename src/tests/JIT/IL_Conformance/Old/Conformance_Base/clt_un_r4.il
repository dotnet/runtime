// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public clt_un {

.field public static	float32 _inf
.field public static	float32 _min
.field public static	float32 _one
.field public static	float32 _zero
.field public static	float32 zero
.field public static	float32 one
.field public static	float32 max
.field public static	float32 inf
.field public static	float32 NaN
.method public static void initialize() {
.maxstack		10
	ldc.r4		float32(0xFF800000)
	stsfld	float32 clt_un::_inf
	ldc.r4		float32(0xFF7FFFFF)
	stsfld	float32 clt_un::_min
	ldc.r4		float32(0xBF800000)
	stsfld	float32 clt_un::_one
	ldc.r4		float32(0x80000000)
	stsfld	float32 clt_un::_zero
	ldc.r4		float32(0x00000000)
	stsfld	float32 clt_un::zero
	ldc.r4		float32(0x3F800000)
	stsfld	float32 clt_un::one
	ldc.r4		float32(0x7F7FFFFF)
	stsfld	float32 clt_un::max
	ldc.r4		float32(0x7F800000)
	stsfld	float32 clt_un::inf
	ldc.r4		float32(0x7FC00000)
	stsfld	float32 clt_un::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void clt_un::initialize()
	ldsfld	float32 clt_un::_inf
	ldsfld	float32 clt_un::_inf
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::_inf
	ldsfld	float32 clt_un::_min
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::_inf
	ldsfld	float32 clt_un::_one
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::_inf
	ldsfld	float32 clt_un::_zero
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::_inf
	ldsfld	float32 clt_un::zero
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::_inf
	ldsfld	float32 clt_un::one
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::_inf
	ldsfld	float32 clt_un::max
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::_inf
	ldsfld	float32 clt_un::inf
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::_inf
	ldsfld	float32 clt_un::NaN
	clt.un
	brfalse		FAIL
	ldsfld	float32 clt_un::_min
	ldsfld	float32 clt_un::_inf
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::_min
	ldsfld	float32 clt_un::_min
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::_min
	ldsfld	float32 clt_un::_one
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::_min
	ldsfld	float32 clt_un::_zero
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::_min
	ldsfld	float32 clt_un::zero
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::_min
	ldsfld	float32 clt_un::one
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::_min
	ldsfld	float32 clt_un::max
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::_min
	ldsfld	float32 clt_un::inf
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::_min
	ldsfld	float32 clt_un::NaN
	clt.un
	brfalse		FAIL


	ldsfld	float32 clt_un::_one
	ldsfld	float32 clt_un::_inf
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::_one
	ldsfld	float32 clt_un::_min
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::_one
	ldsfld	float32 clt_un::_one
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::_one
	ldsfld	float32 clt_un::_zero
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::_one
	ldsfld	float32 clt_un::zero
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::_one
	ldsfld	float32 clt_un::one
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::_one
	ldsfld	float32 clt_un::max
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::_one
	ldsfld	float32 clt_un::inf
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::_one
	ldsfld	float32 clt_un::NaN
	clt.un
	brfalse		FAIL
	ldsfld	float32 clt_un::_zero
	ldsfld	float32 clt_un::_inf
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::_zero
	ldsfld	float32 clt_un::_min
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::_zero
	ldsfld	float32 clt_un::_one
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::_zero
	ldsfld	float32 clt_un::_zero
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::_zero
	ldsfld	float32 clt_un::zero
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::_zero
	ldsfld	float32 clt_un::one
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::_zero
	ldsfld	float32 clt_un::max
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::_zero
	ldsfld	float32 clt_un::inf
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::_zero
	ldsfld	float32 clt_un::NaN
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::zero
	ldsfld	float32 clt_un::_inf
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::zero
	ldsfld	float32 clt_un::_min
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::zero
	ldsfld	float32 clt_un::_one
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::zero
	ldsfld	float32 clt_un::_zero
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::zero
	ldsfld	float32 clt_un::zero
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::zero
	ldsfld	float32 clt_un::one
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::zero
	ldsfld	float32 clt_un::max
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::zero
	ldsfld	float32 clt_un::inf
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::zero
	ldsfld	float32 clt_un::NaN
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::one
	ldsfld	float32 clt_un::_inf
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::one
	ldsfld	float32 clt_un::_min
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::one
	ldsfld	float32 clt_un::_one
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::one
	ldsfld	float32 clt_un::_zero
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::one
	ldsfld	float32 clt_un::zero
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::one
	ldsfld	float32 clt_un::one
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::one
	ldsfld	float32 clt_un::max
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::one
	ldsfld	float32 clt_un::inf
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::one
	ldsfld	float32 clt_un::NaN
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::max
	ldsfld	float32 clt_un::_inf
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::max
	ldsfld	float32 clt_un::_min
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::max
	ldsfld	float32 clt_un::_one
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::max
	ldsfld	float32 clt_un::_zero
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::max
	ldsfld	float32 clt_un::zero
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::max
	ldsfld	float32 clt_un::one
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::max
	ldsfld	float32 clt_un::max
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::max
	ldsfld	float32 clt_un::inf
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::max
	ldsfld	float32 clt_un::NaN
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::inf
	ldsfld	float32 clt_un::_inf
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::inf
	ldsfld	float32 clt_un::_min
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::inf
	ldsfld	float32 clt_un::_one
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::inf
	ldsfld	float32 clt_un::_zero
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::inf
	ldsfld	float32 clt_un::zero
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::inf
	ldsfld	float32 clt_un::one
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::inf
	ldsfld	float32 clt_un::max
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::inf
	ldsfld	float32 clt_un::inf
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::inf
	ldsfld	float32 clt_un::NaN
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::NaN
	ldsfld	float32 clt_un::_inf
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::NaN
	ldsfld	float32 clt_un::_min
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::NaN
	ldsfld	float32 clt_un::_one
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::NaN
	ldsfld	float32 clt_un::_zero
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::NaN
	ldsfld	float32 clt_un::zero
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::NaN
	ldsfld	float32 clt_un::one
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::NaN
	ldsfld	float32 clt_un::max
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::NaN
	ldsfld	float32 clt_un::inf
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::NaN
	ldsfld	float32 clt_un::NaN
	clt.un
	brfalse		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly clt_un_r4.exe{}
