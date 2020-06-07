// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}

.class public cgt_un {

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
	stsfld	float32 cgt_un::_inf
	ldc.r4		float32(0xFF7FFFFF)
	stsfld	float32 cgt_un::_min
	ldc.r4		float32(0xBF800000)
	stsfld	float32 cgt_un::_one
	ldc.r4		float32(0x80000000)
	stsfld	float32 cgt_un::_zero
	ldc.r4		float32(0x00000000)
	stsfld	float32 cgt_un::zero
	ldc.r4		float32(0x3F800000)
	stsfld	float32 cgt_un::one
	ldc.r4		float32(0x7F7FFFFF)
	stsfld	float32 cgt_un::max
	ldc.r4		float32(0x7F800000)
	stsfld	float32 cgt_un::inf
	ldc.r4		float32(0x7FC00000)
	stsfld	float32 cgt_un::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void cgt_un::initialize()
	ldsfld	float32 cgt_un::_inf
	ldsfld	float32 cgt_un::_inf
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_inf
	ldsfld	float32 cgt_un::_min
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_inf
	ldsfld	float32 cgt_un::_one
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_inf
	ldsfld	float32 cgt_un::_zero
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_inf
	ldsfld	float32 cgt_un::zero
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_inf
	ldsfld	float32 cgt_un::one
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_inf
	ldsfld	float32 cgt_un::max
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_inf
	ldsfld	float32 cgt_un::inf
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_inf
	ldsfld	float32 cgt_un::NaN
	cgt.un
	brfalse		FAIL
	ldsfld	float32 cgt_un::_min
	ldsfld	float32 cgt_un::_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::_min
	ldsfld	float32 cgt_un::_min
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_min
	ldsfld	float32 cgt_un::_one
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_min
	ldsfld	float32 cgt_un::_zero
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_min
	ldsfld	float32 cgt_un::zero
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_min
	ldsfld	float32 cgt_un::one
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_min
	ldsfld	float32 cgt_un::max
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_min
	ldsfld	float32 cgt_un::inf
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_min
	ldsfld	float32 cgt_un::NaN
	cgt.un
	brfalse		FAIL


	ldsfld	float32 cgt_un::_one
	ldsfld	float32 cgt_un::_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::_one
	ldsfld	float32 cgt_un::_min
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::_one
	ldsfld	float32 cgt_un::_one
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_one
	ldsfld	float32 cgt_un::_zero
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_one
	ldsfld	float32 cgt_un::zero
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_one
	ldsfld	float32 cgt_un::one
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_one
	ldsfld	float32 cgt_un::max
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_one
	ldsfld	float32 cgt_un::inf
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_one
	ldsfld	float32 cgt_un::NaN
	cgt.un
	brfalse		FAIL
	ldsfld	float32 cgt_un::_zero
	ldsfld	float32 cgt_un::_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::_zero
	ldsfld	float32 cgt_un::_min
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::_zero
	ldsfld	float32 cgt_un::_one
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::_zero
	ldsfld	float32 cgt_un::_zero
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_zero
	ldsfld	float32 cgt_un::zero
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_zero
	ldsfld	float32 cgt_un::one
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_zero
	ldsfld	float32 cgt_un::max
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_zero
	ldsfld	float32 cgt_un::inf
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::_zero
	ldsfld	float32 cgt_un::NaN
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::zero
	ldsfld	float32 cgt_un::_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::zero
	ldsfld	float32 cgt_un::_min
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::zero
	ldsfld	float32 cgt_un::_one
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::zero
	ldsfld	float32 cgt_un::_zero
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::zero
	ldsfld	float32 cgt_un::zero
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::zero
	ldsfld	float32 cgt_un::one
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::zero
	ldsfld	float32 cgt_un::max
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::zero
	ldsfld	float32 cgt_un::inf
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::zero
	ldsfld	float32 cgt_un::NaN
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::one
	ldsfld	float32 cgt_un::_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::one
	ldsfld	float32 cgt_un::_min
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::one
	ldsfld	float32 cgt_un::_one
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::one
	ldsfld	float32 cgt_un::_zero
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::one
	ldsfld	float32 cgt_un::zero
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::one
	ldsfld	float32 cgt_un::one
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::one
	ldsfld	float32 cgt_un::max
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::one
	ldsfld	float32 cgt_un::inf
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::one
	ldsfld	float32 cgt_un::NaN
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::max
	ldsfld	float32 cgt_un::_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::max
	ldsfld	float32 cgt_un::_min
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::max
	ldsfld	float32 cgt_un::_one
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::max
	ldsfld	float32 cgt_un::_zero
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::max
	ldsfld	float32 cgt_un::zero
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::max
	ldsfld	float32 cgt_un::one
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::max
	ldsfld	float32 cgt_un::max
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::max
	ldsfld	float32 cgt_un::inf
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::max
	ldsfld	float32 cgt_un::NaN
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::inf
	ldsfld	float32 cgt_un::_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::inf
	ldsfld	float32 cgt_un::_min
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::inf
	ldsfld	float32 cgt_un::_one
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::inf
	ldsfld	float32 cgt_un::_zero
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::inf
	ldsfld	float32 cgt_un::zero
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::inf
	ldsfld	float32 cgt_un::one
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::inf
	ldsfld	float32 cgt_un::max
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::inf
	ldsfld	float32 cgt_un::inf
	cgt.un
	brtrue		FAIL

	ldsfld	float32 cgt_un::inf
	ldsfld	float32 cgt_un::NaN
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::NaN
	ldsfld	float32 cgt_un::_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::NaN
	ldsfld	float32 cgt_un::_min
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::NaN
	ldsfld	float32 cgt_un::_one
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::NaN
	ldsfld	float32 cgt_un::_zero
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::NaN
	ldsfld	float32 cgt_un::zero
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::NaN
	ldsfld	float32 cgt_un::one
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::NaN
	ldsfld	float32 cgt_un::max
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::NaN
	ldsfld	float32 cgt_un::inf
	cgt.un
	brfalse		FAIL

	ldsfld	float32 cgt_un::NaN
	ldsfld	float32 cgt_un::NaN
	cgt.un
	brfalse		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly cgt_un_r4.exe{}
