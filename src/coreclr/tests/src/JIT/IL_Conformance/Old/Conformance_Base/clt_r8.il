// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}

.class public _clt {

.field public static	float64 _inf
.field public static	float64 _min
.field public static	float64 _one
.field public static	float64 _zero
.field public static	float64 zero
.field public static	float64 one
.field public static	float64 max
.field public static	float64 inf
.field public static	float64 NaN
.method public static void initialize() {
.maxstack		10
	ldc.r8		float64(0xFFF0000000000000)
	stsfld	float64 _clt::_inf
	ldc.r8		float64(0xFF7FFFFFFFFFFFFF)
	stsfld	float64 _clt::_min
	ldc.r8		float64(0xBFF0000000000000)
	stsfld	float64 _clt::_one
	ldc.r8		float64(0x8000000000000000)
	stsfld	float64 _clt::_zero
	ldc.r8		float64(0x0000000000000000)
	stsfld	float64 _clt::zero
	ldc.r8		float64(0x3FF0000000000000)
	stsfld	float64 _clt::one
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	stsfld	float64 _clt::max
	ldc.r8		float64(0x7FF0000000000000)
	stsfld	float64 _clt::inf
	ldc.r8		float64(0x7FF8000000000000)
	stsfld	float64 _clt::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void _clt::initialize()
	ldsfld	float64 _clt::_inf
	ldsfld	float64 _clt::_inf
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::_inf
	ldsfld	float64 _clt::_min
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::_inf
	ldsfld	float64 _clt::_one
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::_inf
	ldsfld	float64 _clt::_zero
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::_inf
	ldsfld	float64 _clt::zero
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::_inf
	ldsfld	float64 _clt::one
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::_inf
	ldsfld	float64 _clt::max
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::_inf
	ldsfld	float64 _clt::inf
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::_inf
	ldsfld	float64 _clt::NaN
	clt
	brtrue		FAIL
	ldsfld	float64 _clt::_min
	ldsfld	float64 _clt::_inf
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::_min
	ldsfld	float64 _clt::_min
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::_min
	ldsfld	float64 _clt::_one
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::_min
	ldsfld	float64 _clt::_zero
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::_min
	ldsfld	float64 _clt::zero
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::_min
	ldsfld	float64 _clt::one
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::_min
	ldsfld	float64 _clt::max
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::_min
	ldsfld	float64 _clt::inf
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::_min
	ldsfld	float64 _clt::NaN
	clt
	brtrue		FAIL
	ldsfld	float64 _clt::_one
	ldsfld	float64 _clt::_inf
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::_one
	ldsfld	float64 _clt::_min
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::_one
	ldsfld	float64 _clt::_one
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::_one
	ldsfld	float64 _clt::_zero
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::_one
	ldsfld	float64 _clt::zero
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::_one
	ldsfld	float64 _clt::one
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::_one
	ldsfld	float64 _clt::max
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::_one
	ldsfld	float64 _clt::inf
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::_one
	ldsfld	float64 _clt::NaN
	clt
	brtrue		FAIL
	ldsfld	float64 _clt::_zero
	ldsfld	float64 _clt::_inf
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::_zero
	ldsfld	float64 _clt::_min
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::_zero
	ldsfld	float64 _clt::_one
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::_zero
	ldsfld	float64 _clt::_zero
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::_zero
	ldsfld	float64 _clt::zero
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::_zero
	ldsfld	float64 _clt::one
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::_zero
	ldsfld	float64 _clt::max
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::_zero
	ldsfld	float64 _clt::inf
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::_zero
	ldsfld	float64 _clt::NaN
	clt
	brtrue		FAIL
	ldsfld	float64 _clt::zero
	ldsfld	float64 _clt::_inf
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::zero
	ldsfld	float64 _clt::_min
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::zero
	ldsfld	float64 _clt::_one
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::zero
	ldsfld	float64 _clt::_zero
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::zero
	ldsfld	float64 _clt::zero
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::zero
	ldsfld	float64 _clt::one
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::zero
	ldsfld	float64 _clt::max
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::zero
	ldsfld	float64 _clt::inf
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::zero
	ldsfld	float64 _clt::NaN
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::one
	ldsfld	float64 _clt::_inf
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::one
	ldsfld	float64 _clt::_min
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::one
	ldsfld	float64 _clt::_one
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::one
	ldsfld	float64 _clt::_zero
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::one
	ldsfld	float64 _clt::zero
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::one
	ldsfld	float64 _clt::one
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::one
	ldsfld	float64 _clt::max
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::one
	ldsfld	float64 _clt::inf
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::one
	ldsfld	float64 _clt::NaN
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::max
	ldsfld	float64 _clt::_inf
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::max
	ldsfld	float64 _clt::_min
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::max
	ldsfld	float64 _clt::_one
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::max
	ldsfld	float64 _clt::_zero
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::max
	ldsfld	float64 _clt::zero
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::max
	ldsfld	float64 _clt::one
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::max
	ldsfld	float64 _clt::max
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::max
	ldsfld	float64 _clt::inf
	clt
	brfalse		FAIL

	ldsfld	float64 _clt::max
	ldsfld	float64 _clt::NaN
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::inf
	ldsfld	float64 _clt::_inf
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::inf
	ldsfld	float64 _clt::_min
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::inf
	ldsfld	float64 _clt::_one
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::inf
	ldsfld	float64 _clt::_zero
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::inf
	ldsfld	float64 _clt::zero
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::inf
	ldsfld	float64 _clt::one
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::inf
	ldsfld	float64 _clt::max
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::inf
	ldsfld	float64 _clt::inf
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::inf
	ldsfld	float64 _clt::NaN
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::NaN
	ldsfld	float64 _clt::_inf
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::NaN
	ldsfld	float64 _clt::_min
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::NaN
	ldsfld	float64 _clt::_one
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::NaN
	ldsfld	float64 _clt::_zero
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::NaN
	ldsfld	float64 _clt::zero
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::NaN
	ldsfld	float64 _clt::one
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::NaN
	ldsfld	float64 _clt::max
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::NaN
	ldsfld	float64 _clt::inf
	clt
	brtrue		FAIL

	ldsfld	float64 _clt::NaN
	ldsfld	float64 _clt::NaN
	clt
	brtrue		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly clt_r8.exe{}
