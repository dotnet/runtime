// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public _clt {

.field public static	int64 min
.field public static	int64 _one
.field public static	int64 zero
.field public static	int64 one
.field public static	int64 max
.field public static	int64 odd
.field public static	int64 even

.method public static void initialize() {
.maxstack		10
	ldc.i8		0x8000000000000000
	stsfld	int64 _clt::min
	ldc.i8		0xFFFFFFFFFFFFFFFF
	stsfld	int64 _clt::_one
	ldc.i8		0x0000000000000000
	stsfld	int64 _clt::zero
	ldc.i8		0x0000000000000001
	stsfld	int64 _clt::one
	ldc.i8		0x7FFFFFFFFFFFFFFF
	stsfld	int64 _clt::max
	ldc.i8		0x5555555555555555
	stsfld	int64 _clt::odd
	ldc.i8		0xAAAAAAAAAAAAAAAA
	stsfld	int64 _clt::even
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10

	call	void _clt::initialize()

	ldsfld	int64 _clt::min
	ldsfld	int64 _clt::min
	clt
	brtrue		FAIL

	ldsfld	int64 _clt::min
	ldsfld	int64 _clt::_one
	clt
	brfalse		FAIL

	ldsfld	int64 _clt::min
	ldsfld	int64 _clt::zero
	clt
	brfalse		FAIL

	ldsfld	int64 _clt::min
	ldsfld	int64 _clt::one
	clt
	brfalse		FAIL

	ldsfld	int64 _clt::min
	ldsfld	int64 _clt::max
	clt
	brfalse		FAIL

	ldsfld	int64 _clt::min
	ldsfld	int64 _clt::odd
	clt
	brfalse		FAIL

	ldsfld	int64 _clt::min
	ldsfld	int64 _clt::even
	clt
	brfalse		FAIL
	ldsfld	int64 _clt::_one
	ldsfld	int64 _clt::min
	clt
	brtrue		FAIL

	ldsfld	int64 _clt::_one
	ldsfld	int64 _clt::_one
	clt
	brtrue		FAIL

	ldsfld	int64 _clt::_one
	ldsfld	int64 _clt::zero
	clt
	brfalse		FAIL

	ldsfld	int64 _clt::_one
	ldsfld	int64 _clt::one
	clt
	brfalse		FAIL

	ldsfld	int64 _clt::_one
	ldsfld	int64 _clt::max
	clt
	brfalse		FAIL

	ldsfld	int64 _clt::_one
	ldsfld	int64 _clt::odd
	clt
	brfalse		FAIL

	ldsfld	int64 _clt::_one
	ldsfld	int64 _clt::even
	clt
	brtrue		FAIL


	ldsfld	int64 _clt::zero
	ldsfld	int64 _clt::min
	clt
	brtrue		FAIL

	ldsfld	int64 _clt::zero
	ldsfld	int64 _clt::_one
	clt
	brtrue		FAIL

	ldsfld	int64 _clt::zero
	ldsfld	int64 _clt::zero
	clt
	brtrue		FAIL

	ldsfld	int64 _clt::zero
	ldsfld	int64 _clt::one
	clt
	brfalse		FAIL

	ldsfld	int64 _clt::zero
	ldsfld	int64 _clt::max
	clt
	brfalse		FAIL

	ldsfld	int64 _clt::zero
	ldsfld	int64 _clt::odd
	clt
	brfalse		FAIL

	ldsfld	int64 _clt::zero
	ldsfld	int64 _clt::even
	clt
	brtrue		FAIL


	ldsfld	int64 _clt::one
	ldsfld	int64 _clt::min
	clt
	brtrue		FAIL

	ldsfld	int64 _clt::one
	ldsfld	int64 _clt::_one
	clt
	brtrue		FAIL

	ldsfld	int64 _clt::one
	ldsfld	int64 _clt::zero
	clt
	brtrue		FAIL

	ldsfld	int64 _clt::one
	ldsfld	int64 _clt::one
	clt
	brtrue		FAIL

	ldsfld	int64 _clt::one
	ldsfld	int64 _clt::max
	clt
	brfalse		FAIL

	ldsfld	int64 _clt::one
	ldsfld	int64 _clt::odd
	clt
	brfalse		FAIL

	ldsfld	int64 _clt::one
	ldsfld	int64 _clt::even
	clt
	brtrue		FAIL


	ldsfld	int64 _clt::max
	ldsfld	int64 _clt::min
	clt
	brtrue		FAIL

	ldsfld	int64 _clt::max
	ldsfld	int64 _clt::_one
	clt
	brtrue		FAIL

	ldsfld	int64 _clt::max
	ldsfld	int64 _clt::zero
	clt
	brtrue		FAIL

	ldsfld	int64 _clt::max
	ldsfld	int64 _clt::one
	clt
	brtrue		FAIL

	ldsfld	int64 _clt::max
	ldsfld	int64 _clt::max
	clt
	brtrue		FAIL

	ldsfld	int64 _clt::max
	ldsfld	int64 _clt::odd
	clt
	brtrue		FAIL

	ldsfld	int64 _clt::max
	ldsfld	int64 _clt::even
	clt
	brtrue		FAIL

	ldsfld	int64 _clt::odd
	ldsfld	int64 _clt::min
	clt
	brtrue		FAIL

	ldsfld	int64 _clt::odd
	ldsfld	int64 _clt::_one
	clt
	brtrue		FAIL

	ldsfld	int64 _clt::odd
	ldsfld	int64 _clt::zero
	clt
	brtrue		FAIL

	ldsfld	int64 _clt::odd
	ldsfld	int64 _clt::one
	clt
	brtrue		FAIL

	ldsfld	int64 _clt::odd
	ldsfld	int64 _clt::max
	clt
	brfalse		FAIL

	ldsfld	int64 _clt::odd
	ldsfld	int64 _clt::odd
	clt
	brtrue		FAIL

	ldsfld	int64 _clt::odd
	ldsfld	int64 _clt::even
	clt
	brtrue		FAIL


	ldsfld	int64 _clt::even
	ldsfld	int64 _clt::min
	clt
	brtrue		FAIL

	ldsfld	int64 _clt::even
	ldsfld	int64 _clt::_one
	clt
	brfalse		FAIL

	ldsfld	int64 _clt::even
	ldsfld	int64 _clt::zero
	clt
	brfalse		FAIL

	ldsfld	int64 _clt::even
	ldsfld	int64 _clt::one
	clt
	brfalse		FAIL

	ldsfld	int64 _clt::even
	ldsfld	int64 _clt::max
	clt
	brfalse		FAIL

	ldsfld	int64 _clt::even
	ldsfld	int64 _clt::odd
	clt
	brfalse		FAIL

	ldsfld	int64 _clt::even
	ldsfld	int64 _clt::even
	clt
	brtrue		FAIL


	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly clt_i8{}
