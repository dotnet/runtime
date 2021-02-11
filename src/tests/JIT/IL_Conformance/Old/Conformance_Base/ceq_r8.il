// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}



.class public _ceq {

.field public static	float64 _inf
.field public static	float64 _min
.field public static	float64 _one
.field public static	float64 _zero
.field public static	float64 zero
.field public static	float64 one
.field public static	float64 max
.field public static	float64 inf
.field public static	float64 NaN

.method public static	void initialize() {
.maxstack	10
	ldc.r8		float64(0xFFF0000000000000)
	stsfld	float64 _ceq::_inf
	ldc.r8		float64(0xFF7FFFFFFFFFFFFF)
	stsfld	float64 _ceq::_min
	ldc.r8		float64(0xBFF0000000000000)
	stsfld	float64 _ceq::_one
	ldc.r8		float64(0x8000000000000000)
	stsfld	float64 _ceq::_zero
	ldc.r8		float64(0x0000000000000000)
	stsfld	float64 _ceq::zero
	ldc.r8		float64(0x3FF0000000000000)
	stsfld	float64 _ceq::one
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	stsfld	float64 _ceq::max
	ldc.r8		float64(0x7FF0000000000000)
	stsfld	float64 _ceq::inf
	ldc.r8		float64(0x7FF8000000000000)
	stsfld	float64 _ceq::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void _ceq::initialize()
	ldsfld	float64 _ceq::_inf
	ldsfld	float64 _ceq::_inf
	ceq
	brfalse		FAIL

	ldsfld	float64 _ceq::_inf
	ldsfld	float64 _ceq::_min
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_inf
	ldsfld	float64 _ceq::_one
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_inf
	ldsfld	float64 _ceq::_zero
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_inf
	ldsfld	float64 _ceq::zero
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_inf
	ldsfld	float64 _ceq::one
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_inf
	ldsfld	float64 _ceq::max
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_inf
	ldsfld	float64 _ceq::inf
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_inf
	ldsfld	float64 _ceq::NaN
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_min
	ldsfld	float64 _ceq::_inf
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_min
	ldsfld	float64 _ceq::_min
	ceq
	brfalse		FAIL

	ldsfld	float64 _ceq::_min
	ldsfld	float64 _ceq::_one
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_min
	ldsfld	float64 _ceq::_zero
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_min
	ldsfld	float64 _ceq::zero
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_min
	ldsfld	float64 _ceq::one
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_min
	ldsfld	float64 _ceq::max
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_min
	ldsfld	float64 _ceq::inf
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_min
	ldsfld	float64 _ceq::NaN
	ceq
	brtrue		FAIL
	ldsfld	float64 _ceq::_one
	ldsfld	float64 _ceq::_inf
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_one
	ldsfld	float64 _ceq::_min
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_one
	ldsfld	float64 _ceq::_one
	ceq
	brfalse		FAIL

	ldsfld	float64 _ceq::_one
	ldsfld	float64 _ceq::_zero
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_one
	ldsfld	float64 _ceq::zero
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_one
	ldsfld	float64 _ceq::one
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_one
	ldsfld	float64 _ceq::max
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_one
	ldsfld	float64 _ceq::inf
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_one
	ldsfld	float64 _ceq::NaN
	ceq
	brtrue		FAIL
	ldsfld	float64 _ceq::_zero
	ldsfld	float64 _ceq::_inf
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_zero
	ldsfld	float64 _ceq::_min
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_zero
	ldsfld	float64 _ceq::_one
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_zero
	ldsfld	float64 _ceq::_zero
	ceq
	brfalse		FAIL

	ldsfld	float64 _ceq::_zero
	ldsfld	float64 _ceq::zero
	ceq
	brfalse		FAIL

	ldsfld	float64 _ceq::_zero
	ldsfld	float64 _ceq::one
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_zero
	ldsfld	float64 _ceq::max
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_zero
	ldsfld	float64 _ceq::inf
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::_zero
	ldsfld	float64 _ceq::NaN
	ceq
	brtrue		FAIL
	ldsfld	float64 _ceq::zero
	ldsfld	float64 _ceq::_inf
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::zero
	ldsfld	float64 _ceq::_min
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::zero
	ldsfld	float64 _ceq::_one
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::zero
	ldsfld	float64 _ceq::_zero
	ceq
	brfalse		FAIL

	ldsfld	float64 _ceq::zero
	ldsfld	float64 _ceq::zero
	ceq
	brfalse		FAIL

	ldsfld	float64 _ceq::zero
	ldsfld	float64 _ceq::one
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::zero
	ldsfld	float64 _ceq::max
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::zero
	ldsfld	float64 _ceq::inf
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::zero
	ldsfld	float64 _ceq::NaN
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::one
	ldsfld	float64 _ceq::_inf
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::one
	ldsfld	float64 _ceq::_min
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::one
	ldsfld	float64 _ceq::_one
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::one
	ldsfld	float64 _ceq::_zero
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::one
	ldsfld	float64 _ceq::zero
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::one
	ldsfld	float64 _ceq::one
	ceq
	brfalse		FAIL

	ldsfld	float64 _ceq::one
	ldsfld	float64 _ceq::max
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::one
	ldsfld	float64 _ceq::inf
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::one
	ldsfld	float64 _ceq::NaN
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::max
	ldsfld	float64 _ceq::_inf
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::max
	ldsfld	float64 _ceq::_min
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::max
	ldsfld	float64 _ceq::_one
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::max
	ldsfld	float64 _ceq::_zero
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::max
	ldsfld	float64 _ceq::zero
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::max
	ldsfld	float64 _ceq::one
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::max
	ldsfld	float64 _ceq::max
	ceq
	brfalse		FAIL

	ldsfld	float64 _ceq::max
	ldsfld	float64 _ceq::inf
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::max
	ldsfld	float64 _ceq::NaN
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::inf
	ldsfld	float64 _ceq::_inf
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::inf
	ldsfld	float64 _ceq::_min
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::inf
	ldsfld	float64 _ceq::_one
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::inf
	ldsfld	float64 _ceq::_zero
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::inf
	ldsfld	float64 _ceq::zero
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::inf
	ldsfld	float64 _ceq::one
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::inf
	ldsfld	float64 _ceq::max
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::inf
	ldsfld	float64 _ceq::inf
	ceq
	brfalse		FAIL

	ldsfld	float64 _ceq::inf
	ldsfld	float64 _ceq::NaN
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::NaN
	ldsfld	float64 _ceq::_inf
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::NaN
	ldsfld	float64 _ceq::_min
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::NaN
	ldsfld	float64 _ceq::_one
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::NaN
	ldsfld	float64 _ceq::_zero
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::NaN
	ldsfld	float64 _ceq::zero
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::NaN
	ldsfld	float64 _ceq::one
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::NaN
	ldsfld	float64 _ceq::max
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::NaN
	ldsfld	float64 _ceq::inf
	ceq
	brtrue		FAIL

	ldsfld	float64 _ceq::NaN
	ldsfld	float64 _ceq::NaN
	ceq
	brtrue		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ceq_r8.exe{}
