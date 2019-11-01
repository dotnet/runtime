// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}

.class public _clt {

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
	stsfld	float32 _clt::_inf
	ldc.r4		float32(0xFF7FFFFF)
	stsfld	float32 _clt::_min
	ldc.r4		float32(0xBF800000)
	stsfld	float32 _clt::_one
	ldc.r4		float32(0x80000000)
	stsfld	float32 _clt::_zero
	ldc.r4		float32(0x00000000)
	stsfld	float32 _clt::zero
	ldc.r4		float32(0x3F800000)
	stsfld	float32 _clt::one
	ldc.r4		float32(0x7F7FFFFF)
	stsfld	float32 _clt::max
	ldc.r4		float32(0x7F800000)
	stsfld	float32 _clt::inf
	ldc.r4		float32(0x7FC00000)
	stsfld	float32 _clt::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10

call	void _clt::initialize()

	ldsfld	float32 _clt::_inf
	ldsfld	float32 _clt::_inf
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::_inf
	ldsfld	float32 _clt::_min
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::_inf
	ldsfld	float32 _clt::_one
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::_inf
	ldsfld	float32 _clt::_zero
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::_inf
	ldsfld	float32 _clt::zero
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::_inf
	ldsfld	float32 _clt::one
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::_inf
	ldsfld	float32 _clt::max
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::_inf
	ldsfld	float32 _clt::inf
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::_inf
	ldsfld	float32 _clt::NaN
	clt
	brtrue		FAIL
	ldsfld	float32 _clt::_min
	ldsfld	float32 _clt::_inf
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::_min
	ldsfld	float32 _clt::_min
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::_min
	ldsfld	float32 _clt::_one
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::_min
	ldsfld	float32 _clt::_zero
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::_min
	ldsfld	float32 _clt::zero
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::_min
	ldsfld	float32 _clt::one
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::_min
	ldsfld	float32 _clt::max
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::_min
	ldsfld	float32 _clt::inf
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::_min
	ldsfld	float32 _clt::NaN
	clt
	brtrue		FAIL
	ldsfld	float32 _clt::_one
	ldsfld	float32 _clt::_inf
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::_one
	ldsfld	float32 _clt::_min
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::_one
	ldsfld	float32 _clt::_one
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::_one
	ldsfld	float32 _clt::_zero
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::_one
	ldsfld	float32 _clt::zero
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::_one
	ldsfld	float32 _clt::one
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::_one
	ldsfld	float32 _clt::max
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::_one
	ldsfld	float32 _clt::inf
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::_one
	ldsfld	float32 _clt::NaN
	clt
	brtrue		FAIL
	ldsfld	float32 _clt::_zero
	ldsfld	float32 _clt::_inf
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::_zero
	ldsfld	float32 _clt::_min
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::_zero
	ldsfld	float32 _clt::_one
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::_zero
	ldsfld	float32 _clt::_zero
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::_zero
	ldsfld	float32 _clt::zero
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::_zero
	ldsfld	float32 _clt::one
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::_zero
	ldsfld	float32 _clt::max
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::_zero
	ldsfld	float32 _clt::inf
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::_zero
	ldsfld	float32 _clt::NaN
	clt
	brtrue		FAIL
	ldsfld	float32 _clt::zero
	ldsfld	float32 _clt::_inf
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::zero
	ldsfld	float32 _clt::_min
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::zero
	ldsfld	float32 _clt::_one
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::zero
	ldsfld	float32 _clt::_zero
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::zero
	ldsfld	float32 _clt::zero
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::zero
	ldsfld	float32 _clt::one
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::zero
	ldsfld	float32 _clt::max
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::zero
	ldsfld	float32 _clt::inf
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::zero
	ldsfld	float32 _clt::NaN
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::one
	ldsfld	float32 _clt::_inf
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::one
	ldsfld	float32 _clt::_min
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::one
	ldsfld	float32 _clt::_one
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::one
	ldsfld	float32 _clt::_zero
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::one
	ldsfld	float32 _clt::zero
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::one
	ldsfld	float32 _clt::one
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::one
	ldsfld	float32 _clt::max
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::one
	ldsfld	float32 _clt::inf
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::one
	ldsfld	float32 _clt::NaN
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::max
	ldsfld	float32 _clt::_inf
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::max
	ldsfld	float32 _clt::_min
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::max
	ldsfld	float32 _clt::_one
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::max
	ldsfld	float32 _clt::_zero
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::max
	ldsfld	float32 _clt::zero
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::max
	ldsfld	float32 _clt::one
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::max
	ldsfld	float32 _clt::max
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::max
	ldsfld	float32 _clt::inf
	clt
	brfalse		FAIL

	ldsfld	float32 _clt::max
	ldsfld	float32 _clt::NaN
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::inf
	ldsfld	float32 _clt::_inf
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::inf
	ldsfld	float32 _clt::_min
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::inf
	ldsfld	float32 _clt::_one
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::inf
	ldsfld	float32 _clt::_zero
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::inf
	ldsfld	float32 _clt::zero
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::inf
	ldsfld	float32 _clt::one
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::inf
	ldsfld	float32 _clt::max
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::inf
	ldsfld	float32 _clt::inf
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::inf
	ldsfld	float32 _clt::NaN
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::NaN
	ldsfld	float32 _clt::_inf
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::NaN
	ldsfld	float32 _clt::_min
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::NaN
	ldsfld	float32 _clt::_one
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::NaN
	ldsfld	float32 _clt::_zero
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::NaN
	ldsfld	float32 _clt::zero
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::NaN
	ldsfld	float32 _clt::one
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::NaN
	ldsfld	float32 _clt::max
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::NaN
	ldsfld	float32 _clt::inf
	clt
	brtrue		FAIL

	ldsfld	float32 _clt::NaN
	ldsfld	float32 _clt::NaN
	clt
	brtrue		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly clt_r4.exe{}
