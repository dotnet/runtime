// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public cgt_un {

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
	stsfld	float64 cgt_un::_inf
	ldc.r8		float64(0xFF7FFFFFFFFFFFFF)
	stsfld	float64 cgt_un::_min
	ldc.r8		float64(0xBFF0000000000000)
	stsfld	float64 cgt_un::_one
	ldc.r8		float64(0x8000000000000000)
	stsfld	float64 cgt_un::_zero
	ldc.r8		float64(0x0000000000000000)
	stsfld	float64 cgt_un::zero
	ldc.r8		float64(0x3FF0000000000000)
	stsfld	float64 cgt_un::one
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	stsfld	float64 cgt_un::max
	ldc.r8		float64(0x7FF0000000000000)
	stsfld	float64 cgt_un::inf
	ldc.r8		float64(0x7FF8000000000000)
	stsfld	float64 cgt_un::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void cgt_un::initialize()

	ldsfld	float64 cgt_un::_inf
	ldsfld	float64 cgt_un::_inf
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_inf
	ldsfld	float64 cgt_un::_min
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_inf
	ldsfld	float64 cgt_un::_one
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_inf
	ldsfld	float64 cgt_un::_zero
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_inf
	ldsfld	float64 cgt_un::zero
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_inf
	ldsfld	float64 cgt_un::one
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_inf
	ldsfld	float64 cgt_un::max
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_inf
	ldsfld	float64 cgt_un::inf
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_inf
	ldsfld	float64 cgt_un::NaN
	cgt.un
	brfalse		FAIL
	ldsfld	float64 cgt_un::_min
	ldsfld	float64 cgt_un::_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::_min
	ldsfld	float64 cgt_un::_min
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_min
	ldsfld	float64 cgt_un::_one
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_min
	ldsfld	float64 cgt_un::_zero
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_min
	ldsfld	float64 cgt_un::zero
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_min
	ldsfld	float64 cgt_un::one
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_min
	ldsfld	float64 cgt_un::max
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_min
	ldsfld	float64 cgt_un::inf
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_min
	ldsfld	float64 cgt_un::NaN
	cgt.un
	brfalse		FAIL


	ldsfld	float64 cgt_un::_one
	ldsfld	float64 cgt_un::_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::_one
	ldsfld	float64 cgt_un::_min
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::_one
	ldsfld	float64 cgt_un::_one
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_one
	ldsfld	float64 cgt_un::_zero
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_one
	ldsfld	float64 cgt_un::zero
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_one
	ldsfld	float64 cgt_un::one
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_one
	ldsfld	float64 cgt_un::max
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_one
	ldsfld	float64 cgt_un::inf
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_one
	ldsfld	float64 cgt_un::NaN
	cgt.un
	brfalse		FAIL
	ldsfld	float64 cgt_un::_zero
	ldsfld	float64 cgt_un::_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::_zero
	ldsfld	float64 cgt_un::_min
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::_zero
	ldsfld	float64 cgt_un::_one
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::_zero
	ldsfld	float64 cgt_un::_zero
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_zero
	ldsfld	float64 cgt_un::zero
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_zero
	ldsfld	float64 cgt_un::one
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_zero
	ldsfld	float64 cgt_un::max
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_zero
	ldsfld	float64 cgt_un::inf
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::_zero
	ldsfld	float64 cgt_un::NaN
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::zero
	ldsfld	float64 cgt_un::_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::zero
	ldsfld	float64 cgt_un::_min
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::zero
	ldsfld	float64 cgt_un::_one
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::zero
	ldsfld	float64 cgt_un::_zero
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::zero
	ldsfld	float64 cgt_un::zero
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::zero
	ldsfld	float64 cgt_un::one
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::zero
	ldsfld	float64 cgt_un::max
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::zero
	ldsfld	float64 cgt_un::inf
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::zero
	ldsfld	float64 cgt_un::NaN
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::one
	ldsfld	float64 cgt_un::_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::one
	ldsfld	float64 cgt_un::_min
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::one
	ldsfld	float64 cgt_un::_one
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::one
	ldsfld	float64 cgt_un::_zero
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::one
	ldsfld	float64 cgt_un::zero
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::one
	ldsfld	float64 cgt_un::one
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::one
	ldsfld	float64 cgt_un::max
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::one
	ldsfld	float64 cgt_un::inf
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::one
	ldsfld	float64 cgt_un::NaN
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::max
	ldsfld	float64 cgt_un::_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::max
	ldsfld	float64 cgt_un::_min
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::max
	ldsfld	float64 cgt_un::_one
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::max
	ldsfld	float64 cgt_un::_zero
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::max
	ldsfld	float64 cgt_un::zero
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::max
	ldsfld	float64 cgt_un::one
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::max
	ldsfld	float64 cgt_un::max
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::max
	ldsfld	float64 cgt_un::inf
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::max
	ldsfld	float64 cgt_un::NaN
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::inf
	ldsfld	float64 cgt_un::_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::inf
	ldsfld	float64 cgt_un::_min
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::inf
	ldsfld	float64 cgt_un::_one
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::inf
	ldsfld	float64 cgt_un::_zero
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::inf
	ldsfld	float64 cgt_un::zero
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::inf
	ldsfld	float64 cgt_un::one
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::inf
	ldsfld	float64 cgt_un::max
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::inf
	ldsfld	float64 cgt_un::inf
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::inf
	ldsfld	float64 cgt_un::NaN
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::NaN
	ldsfld	float64 cgt_un::_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::NaN
	ldsfld	float64 cgt_un::_min
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::NaN
	ldsfld	float64 cgt_un::_one
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::NaN
	ldsfld	float64 cgt_un::_zero
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::NaN
	ldsfld	float64 cgt_un::zero
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::NaN
	ldsfld	float64 cgt_un::one
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::NaN
	ldsfld	float64 cgt_un::max
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::NaN
	ldsfld	float64 cgt_un::inf
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::NaN
	ldsfld	float64 cgt_un::NaN
	cgt.un
	brfalse		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly cgt_un_r8.exe{}
