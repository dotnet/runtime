// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public _ceq {

.field public static	int64 min
.field public static	int64 _one
.field public static	int64 zero
.field public static	int64 one
.field public static	int64 max
.field public static	int64 odd
.field public static	int64 even

.method public static	void initialize() {
.maxstack	10
	ldc.i8		0x8000000000000000
	stsfld	int64 _ceq::min
	ldc.i8		0xFFFFFFFFFFFFFFFF
	stsfld	int64 _ceq::_one
	ldc.i8		0x0000000000000000
	stsfld	int64 _ceq::zero
	ldc.i8		0x0000000000000001
	stsfld	int64 _ceq::one
	ldc.i8		0x7FFFFFFFFFFFFFFF
	stsfld	int64 _ceq::max
	ldc.i8		0x5555555555555555
	stsfld	int64 _ceq::odd
	ldc.i8		0xAAAAAAAAAAAAAAAA
	stsfld	int64 _ceq::even
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void _ceq::initialize()

	ldsfld	int64 _ceq::min
	ldsfld	int64 _ceq::min
	ceq
	brfalse		FAIL

	ldsfld	int64 _ceq::min
	ldsfld	int64 _ceq::_one
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::min
	ldsfld	int64 _ceq::zero
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::min
	ldsfld	int64 _ceq::one
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::min
	ldsfld	int64 _ceq::max
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::min
	ldsfld	int64 _ceq::odd
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::min
	ldsfld	int64 _ceq::even
	ceq
	brtrue		FAIL
	ldsfld	int64 _ceq::_one
	ldsfld	int64 _ceq::min
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::_one
	ldsfld	int64 _ceq::_one
	ceq
	brfalse		FAIL

	ldsfld	int64 _ceq::_one
	ldsfld	int64 _ceq::zero
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::_one
	ldsfld	int64 _ceq::one
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::_one
	ldsfld	int64 _ceq::max
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::_one
	ldsfld	int64 _ceq::odd
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::_one
	ldsfld	int64 _ceq::even
	ceq
	brtrue		FAIL


	ldsfld	int64 _ceq::zero
	ldsfld	int64 _ceq::min
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::zero
	ldsfld	int64 _ceq::_one
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::zero
	ldsfld	int64 _ceq::zero
	ceq
	brfalse		FAIL

	ldsfld	int64 _ceq::zero
	ldsfld	int64 _ceq::one
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::zero
	ldsfld	int64 _ceq::max
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::zero
	ldsfld	int64 _ceq::odd
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::zero
	ldsfld	int64 _ceq::even
	ceq
	brtrue		FAIL


	ldsfld	int64 _ceq::one
	ldsfld	int64 _ceq::min
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::one
	ldsfld	int64 _ceq::_one
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::one
	ldsfld	int64 _ceq::zero
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::one
	ldsfld	int64 _ceq::one
	ceq
	brfalse		FAIL

	ldsfld	int64 _ceq::one
	ldsfld	int64 _ceq::max
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::one
	ldsfld	int64 _ceq::odd
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::one
	ldsfld	int64 _ceq::even
	ceq
	brtrue		FAIL


	ldsfld	int64 _ceq::max
	ldsfld	int64 _ceq::min
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::max
	ldsfld	int64 _ceq::_one
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::max
	ldsfld	int64 _ceq::zero
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::max
	ldsfld	int64 _ceq::one
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::max
	ldsfld	int64 _ceq::max
	ceq
	brfalse		FAIL

	ldsfld	int64 _ceq::max
	ldsfld	int64 _ceq::odd
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::max
	ldsfld	int64 _ceq::even
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::odd
	ldsfld	int64 _ceq::min
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::odd
	ldsfld	int64 _ceq::_one
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::odd
	ldsfld	int64 _ceq::zero
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::odd
	ldsfld	int64 _ceq::one
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::odd
	ldsfld	int64 _ceq::max
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::odd
	ldsfld	int64 _ceq::odd
	ceq
	brfalse		FAIL

	ldsfld	int64 _ceq::odd
	ldsfld	int64 _ceq::even
	ceq
	brtrue		FAIL


	ldsfld	int64 _ceq::even
	ldsfld	int64 _ceq::min
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::even
	ldsfld	int64 _ceq::_one
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::even
	ldsfld	int64 _ceq::zero
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::even
	ldsfld	int64 _ceq::one
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::even
	ldsfld	int64 _ceq::max
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::even
	ldsfld	int64 _ceq::odd
	ceq
	brtrue		FAIL

	ldsfld	int64 _ceq::even
	ldsfld	int64 _ceq::even
	ceq
	brfalse		FAIL




	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ceq_i8.exe{}
