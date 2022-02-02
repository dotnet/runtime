// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public _ceq {

.field public static	int32 min
.field public static	int32 _one
.field public static	int32 zero
.field public static	int32 one
.field public static	int32 max
.field public static	int32 odd
.field public static	int32 even

.method public static	void initialize() {
.maxstack	10
	ldc.i4		0x80000000
	stsfld	int32 _ceq::min
	ldc.i4		0xFFFFFFFF
	stsfld	int32 _ceq::_one
	ldc.i4		0x00000000
	stsfld	int32 _ceq::zero
	ldc.i4		0x00000001
	stsfld	int32 _ceq::one
	ldc.i4		0x7FFFFFFF
	stsfld	int32 _ceq::max
	ldc.i4		0x55555555
	stsfld	int32 _ceq::odd
	ldc.i4		0xAAAAAAAA
	stsfld	int32 _ceq::even
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void _ceq::initialize()
	ldsfld	int32 _ceq::min
	ldsfld	int32 _ceq::min
	ceq
	brfalse		FAIL

	ldsfld	int32 _ceq::min
	ldsfld	int32 _ceq::_one
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::min
	ldsfld	int32 _ceq::zero
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::min
	ldsfld	int32 _ceq::one
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::min
	ldsfld	int32 _ceq::max
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::min
	ldsfld	int32 _ceq::odd
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::min
	ldsfld	int32 _ceq::even
	ceq
	brtrue		FAIL
	ldsfld	int32 _ceq::_one
	ldsfld	int32 _ceq::min
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::_one
	ldsfld	int32 _ceq::_one
	ceq
	brfalse		FAIL

	ldsfld	int32 _ceq::_one
	ldsfld	int32 _ceq::zero
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::_one
	ldsfld	int32 _ceq::one
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::_one
	ldsfld	int32 _ceq::max
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::_one
	ldsfld	int32 _ceq::odd
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::_one
	ldsfld	int32 _ceq::even
	ceq
	brtrue		FAIL


	ldsfld	int32 _ceq::zero
	ldsfld	int32 _ceq::min
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::zero
	ldsfld	int32 _ceq::_one
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::zero
	ldsfld	int32 _ceq::zero
	ceq
	brfalse		FAIL

	ldsfld	int32 _ceq::zero
	ldsfld	int32 _ceq::one
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::zero
	ldsfld	int32 _ceq::max
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::zero
	ldsfld	int32 _ceq::odd
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::zero
	ldsfld	int32 _ceq::even
	ceq
	brtrue		FAIL


	ldsfld	int32 _ceq::one
	ldsfld	int32 _ceq::min
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::one
	ldsfld	int32 _ceq::_one
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::one
	ldsfld	int32 _ceq::zero
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::one
	ldsfld	int32 _ceq::one
	ceq
	brfalse		FAIL

	ldsfld	int32 _ceq::one
	ldsfld	int32 _ceq::max
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::one
	ldsfld	int32 _ceq::odd
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::one
	ldsfld	int32 _ceq::even
	ceq
	brtrue		FAIL


	ldsfld	int32 _ceq::max
	ldsfld	int32 _ceq::min
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::max
	ldsfld	int32 _ceq::_one
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::max
	ldsfld	int32 _ceq::zero
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::max
	ldsfld	int32 _ceq::one
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::max
	ldsfld	int32 _ceq::max
	ceq
	brfalse		FAIL

	ldsfld	int32 _ceq::max
	ldsfld	int32 _ceq::odd
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::max
	ldsfld	int32 _ceq::even
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::odd
	ldsfld	int32 _ceq::min
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::odd
	ldsfld	int32 _ceq::_one
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::odd
	ldsfld	int32 _ceq::zero
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::odd
	ldsfld	int32 _ceq::one
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::odd
	ldsfld	int32 _ceq::max
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::odd
	ldsfld	int32 _ceq::odd
	ceq
	brfalse		FAIL

	ldsfld	int32 _ceq::odd
	ldsfld	int32 _ceq::even
	ceq
	brtrue		FAIL


	ldsfld	int32 _ceq::even
	ldsfld	int32 _ceq::min
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::even
	ldsfld	int32 _ceq::_one
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::even
	ldsfld	int32 _ceq::zero
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::even
	ldsfld	int32 _ceq::one
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::even
	ldsfld	int32 _ceq::max
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::even
	ldsfld	int32 _ceq::odd
	ceq
	brtrue		FAIL

	ldsfld	int32 _ceq::even
	ldsfld	int32 _ceq::even
	ceq
	brfalse		FAIL




	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ceq_i4{}
