// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public _cgt {

.field public static	float32 _inf
.field public static	float32 _min
.field public static	float32 _one
.field public static	float32 _zero
.field public static	float32 zero
.field public static	float32 one
.field public static	float32 max
.field public static	float32 inf
.field public static	float32 NaN

.method public static	void initialize() {
.maxstack	10
	ldc.r4		float32(0xFF800000)
	stsfld	float32 _cgt::_inf
	ldc.r4		float32(0xFF7FFFFF)
	stsfld	float32 _cgt::_min
	ldc.r4		float32(0xBF800000)
	stsfld	float32 _cgt::_one
	ldc.r4		float32(0x80000000)
	stsfld	float32 _cgt::_zero
	ldc.r4		float32(0x00000000)
	stsfld	float32 _cgt::zero
	ldc.r4		float32(0x3F800000)
	stsfld	float32 _cgt::one
	ldc.r4		float32(0x7F7FFFFF)
	stsfld	float32 _cgt::max
	ldc.r4		float32(0x7F800000)
	stsfld	float32 _cgt::inf
	ldc.r4		float32(0x7FC00000)
	stsfld	float32 _cgt::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void _cgt::initialize()

	ldsfld	float32 _cgt::_inf
	ldsfld	float32 _cgt::_inf
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_inf
	ldsfld	float32 _cgt::_min
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_inf
	ldsfld	float32 _cgt::_one
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_inf
	ldsfld	float32 _cgt::_zero
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_inf
	ldsfld	float32 _cgt::zero
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_inf
	ldsfld	float32 _cgt::one
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_inf
	ldsfld	float32 _cgt::max
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_inf
	ldsfld	float32 _cgt::inf
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_inf
	ldsfld	float32 _cgt::NaN
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_min
	ldsfld	float32 _cgt::_inf
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::_min
	ldsfld	float32 _cgt::_min
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_min
	ldsfld	float32 _cgt::_one
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_min
	ldsfld	float32 _cgt::_zero
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_min
	ldsfld	float32 _cgt::zero
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_min
	ldsfld	float32 _cgt::one
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_min
	ldsfld	float32 _cgt::max
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_min
	ldsfld	float32 _cgt::inf
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_min
	ldsfld	float32 _cgt::NaN
	cgt
	brtrue		FAIL
	ldsfld	float32 _cgt::_one
	ldsfld	float32 _cgt::_inf
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::_one
	ldsfld	float32 _cgt::_min
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::_one
	ldsfld	float32 _cgt::_one
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_one
	ldsfld	float32 _cgt::_zero
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_one
	ldsfld	float32 _cgt::zero
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_one
	ldsfld	float32 _cgt::one
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_one
	ldsfld	float32 _cgt::max
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_one
	ldsfld	float32 _cgt::inf
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_one
	ldsfld	float32 _cgt::NaN
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_zero
	ldsfld	float32 _cgt::_inf
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::_zero
	ldsfld	float32 _cgt::_min
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::_zero
	ldsfld	float32 _cgt::_one
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::_zero
	ldsfld	float32 _cgt::_zero
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_zero
	ldsfld	float32 _cgt::zero
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_zero
	ldsfld	float32 _cgt::one
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_zero
	ldsfld	float32 _cgt::max
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_zero
	ldsfld	float32 _cgt::inf
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::_zero
	ldsfld	float32 _cgt::NaN
	cgt
	brtrue		FAIL
	ldsfld	float32 _cgt::zero
	ldsfld	float32 _cgt::_inf
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::zero
	ldsfld	float32 _cgt::_min
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::zero
	ldsfld	float32 _cgt::_one
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::zero
	ldsfld	float32 _cgt::_zero
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::zero
	ldsfld	float32 _cgt::zero
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::zero
	ldsfld	float32 _cgt::one
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::zero
	ldsfld	float32 _cgt::max
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::zero
	ldsfld	float32 _cgt::inf
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::zero
	ldsfld	float32 _cgt::NaN
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::one
	ldsfld	float32 _cgt::_inf
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::one
	ldsfld	float32 _cgt::_min
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::one
	ldsfld	float32 _cgt::_one
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::one
	ldsfld	float32 _cgt::_zero
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::one
	ldsfld	float32 _cgt::zero
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::one
	ldsfld	float32 _cgt::one
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::one
	ldsfld	float32 _cgt::max
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::one
	ldsfld	float32 _cgt::inf
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::one
	ldsfld	float32 _cgt::NaN
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::max
	ldsfld	float32 _cgt::_inf
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::max
	ldsfld	float32 _cgt::_min
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::max
	ldsfld	float32 _cgt::_one
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::max
	ldsfld	float32 _cgt::_zero
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::max
	ldsfld	float32 _cgt::zero
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::max
	ldsfld	float32 _cgt::one
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::max
	ldsfld	float32 _cgt::max
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::max
	ldsfld	float32 _cgt::inf
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::max
	ldsfld	float32 _cgt::NaN
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::inf
	ldsfld	float32 _cgt::_inf
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::inf
	ldsfld	float32 _cgt::_min
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::inf
	ldsfld	float32 _cgt::_one
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::inf
	ldsfld	float32 _cgt::_zero
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::inf
	ldsfld	float32 _cgt::zero
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::inf
	ldsfld	float32 _cgt::one
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::inf
	ldsfld	float32 _cgt::max
	cgt
	brfalse		FAIL

	ldsfld	float32 _cgt::inf
	ldsfld	float32 _cgt::inf
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::inf
	ldsfld	float32 _cgt::NaN
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::NaN
	ldsfld	float32 _cgt::_inf
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::NaN
	ldsfld	float32 _cgt::_min
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::NaN
	ldsfld	float32 _cgt::_one
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::NaN
	ldsfld	float32 _cgt::_zero
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::NaN
	ldsfld	float32 _cgt::zero
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::NaN
	ldsfld	float32 _cgt::one
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::NaN
	ldsfld	float32 _cgt::max
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::NaN
	ldsfld	float32 _cgt::inf
	cgt
	brtrue		FAIL

	ldsfld	float32 _cgt::NaN
	ldsfld	float32 _cgt::NaN
	cgt
	brtrue		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly cgt_r4.exe{}
