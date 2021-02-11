// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public _cgt {

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
	stsfld	int32 _cgt::min
	ldc.i4		0xFFFFFFFF
	stsfld	int32 _cgt::_one
	ldc.i4		0x00000000
	stsfld	int32 _cgt::zero
	ldc.i4		0x00000001
	stsfld	int32 _cgt::one
	ldc.i4		0x7FFFFFFF
	stsfld	int32 _cgt::max
	ldc.i4		0x55555555
	stsfld	int32 _cgt::odd
	ldc.i4		0xAAAAAAAA
	stsfld	int32 _cgt::even
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void _cgt::initialize()

	ldsfld	int32 _cgt::min
	ldsfld	int32 _cgt::min
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::min
	ldsfld	int32 _cgt::_one
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::min
	ldsfld	int32 _cgt::zero
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::min
	ldsfld	int32 _cgt::one
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::min
	ldsfld	int32 _cgt::max
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::min
	ldsfld	int32 _cgt::odd
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::min
	ldsfld	int32 _cgt::even
	cgt
	brtrue		FAIL
	ldsfld	int32 _cgt::_one
	ldsfld	int32 _cgt::min
	cgt
	brfalse		FAIL

	ldsfld	int32 _cgt::_one
	ldsfld	int32 _cgt::_one
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::_one
	ldsfld	int32 _cgt::zero
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::_one
	ldsfld	int32 _cgt::one
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::_one
	ldsfld	int32 _cgt::max
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::_one
	ldsfld	int32 _cgt::odd
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::_one
	ldsfld	int32 _cgt::even
	cgt
	brfalse		FAIL


	ldsfld	int32 _cgt::zero
	ldsfld	int32 _cgt::min
	cgt
	brfalse		FAIL

	ldsfld	int32 _cgt::zero
	ldsfld	int32 _cgt::_one
	cgt
	brfalse		FAIL

	ldsfld	int32 _cgt::zero
	ldsfld	int32 _cgt::zero
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::zero
	ldsfld	int32 _cgt::one
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::zero
	ldsfld	int32 _cgt::max
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::zero
	ldsfld	int32 _cgt::odd
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::zero
	ldsfld	int32 _cgt::even
	cgt
	brfalse		FAIL


	ldsfld	int32 _cgt::one
	ldsfld	int32 _cgt::min
	cgt
	brfalse		FAIL

	ldsfld	int32 _cgt::one
	ldsfld	int32 _cgt::_one
	cgt
	brfalse		FAIL

	ldsfld	int32 _cgt::one
	ldsfld	int32 _cgt::zero
	cgt
	brfalse		FAIL

	ldsfld	int32 _cgt::one
	ldsfld	int32 _cgt::one
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::one
	ldsfld	int32 _cgt::max
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::one
	ldsfld	int32 _cgt::odd
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::one
	ldsfld	int32 _cgt::even
	cgt
	brfalse		FAIL


	ldsfld	int32 _cgt::max
	ldsfld	int32 _cgt::min
	cgt
	brfalse		FAIL

	ldsfld	int32 _cgt::max
	ldsfld	int32 _cgt::_one
	cgt
	brfalse		FAIL

	ldsfld	int32 _cgt::max
	ldsfld	int32 _cgt::zero
	cgt
	brfalse		FAIL

	ldsfld	int32 _cgt::max
	ldsfld	int32 _cgt::one
	cgt
	brfalse		FAIL

	ldsfld	int32 _cgt::max
	ldsfld	int32 _cgt::max
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::max
	ldsfld	int32 _cgt::odd
	cgt
	brfalse		FAIL

	ldsfld	int32 _cgt::max
	ldsfld	int32 _cgt::even
	cgt
	brfalse		FAIL

	ldsfld	int32 _cgt::odd
	ldsfld	int32 _cgt::min
	cgt
	brfalse		FAIL

	ldsfld	int32 _cgt::odd
	ldsfld	int32 _cgt::_one
	cgt
	brfalse		FAIL

	ldsfld	int32 _cgt::odd
	ldsfld	int32 _cgt::zero
	cgt
	brfalse		FAIL

	ldsfld	int32 _cgt::odd
	ldsfld	int32 _cgt::one
	cgt
	brfalse		FAIL

	ldsfld	int32 _cgt::odd
	ldsfld	int32 _cgt::max
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::odd
	ldsfld	int32 _cgt::odd
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::odd
	ldsfld	int32 _cgt::even
	cgt
	brfalse		FAIL


	ldsfld	int32 _cgt::even
	ldsfld	int32 _cgt::min
	cgt
	brfalse		FAIL

	ldsfld	int32 _cgt::even
	ldsfld	int32 _cgt::_one
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::even
	ldsfld	int32 _cgt::zero
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::even
	ldsfld	int32 _cgt::one
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::even
	ldsfld	int32 _cgt::max
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::even
	ldsfld	int32 _cgt::odd
	cgt
	brtrue		FAIL

	ldsfld	int32 _cgt::even
	ldsfld	int32 _cgt::even
	cgt
	brtrue		FAIL




	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly cgt_i4.exe{}
