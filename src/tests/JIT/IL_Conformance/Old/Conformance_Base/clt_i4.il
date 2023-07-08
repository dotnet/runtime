// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}

.namespace clt_i4
{
.class public _clt {

.field public static	int32 min
.field public static	int32 _one
.field public static	int32 zero
.field public static	int32 one
.field public static	int32 max
.field public static	int32 odd
.field public static	int32 even

.method public static void initialize() {
.maxstack		10
	ldc.i4		0x80000000
	stsfld	int32 clt_i4._clt::min
	ldc.i4		0xFFFFFFFF
	stsfld	int32 clt_i4._clt::_one
	ldc.i4		0x00000000
	stsfld	int32 clt_i4._clt::zero
	ldc.i4		0x00000001
	stsfld	int32 clt_i4._clt::one
	ldc.i4		0x7FFFFFFF
	stsfld	int32 clt_i4._clt::max
	ldc.i4		0x55555555
	stsfld	int32 clt_i4._clt::odd
	ldc.i4		0xAAAAAAAA
	stsfld	int32 clt_i4._clt::even
	ret
}
.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack		10

	call void clt_i4._clt::initialize()

	ldsfld	int32 clt_i4._clt::min
	ldsfld	int32 clt_i4._clt::min
	clt
	brtrue		FAIL

	ldsfld	int32 clt_i4._clt::min
	ldsfld	int32 clt_i4._clt::_one
	clt
	brfalse		FAIL

	ldsfld	int32 clt_i4._clt::min
	ldsfld	int32 clt_i4._clt::zero
	clt
	brfalse		FAIL

	ldsfld	int32 clt_i4._clt::min
	ldsfld	int32 clt_i4._clt::one
	clt
	brfalse		FAIL

	ldsfld	int32 clt_i4._clt::min
	ldsfld	int32 clt_i4._clt::max
	clt
	brfalse		FAIL

	ldsfld	int32 clt_i4._clt::min
	ldsfld	int32 clt_i4._clt::odd
	clt
	brfalse		FAIL

	ldsfld	int32 clt_i4._clt::min
	ldsfld	int32 clt_i4._clt::even
	clt
	brfalse		FAIL
	ldsfld	int32 clt_i4._clt::_one
	ldsfld	int32 clt_i4._clt::min
	clt
	brtrue		FAIL

	ldsfld	int32 clt_i4._clt::_one
	ldsfld	int32 clt_i4._clt::_one
	clt
	brtrue		FAIL

	ldsfld	int32 clt_i4._clt::_one
	ldsfld	int32 clt_i4._clt::zero
	clt
	brfalse		FAIL

	ldsfld	int32 clt_i4._clt::_one
	ldsfld	int32 clt_i4._clt::one
	clt
	brfalse		FAIL

	ldsfld	int32 clt_i4._clt::_one
	ldsfld	int32 clt_i4._clt::max
	clt
	brfalse		FAIL

	ldsfld	int32 clt_i4._clt::_one
	ldsfld	int32 clt_i4._clt::odd
	clt
	brfalse		FAIL

	ldsfld	int32 clt_i4._clt::_one
	ldsfld	int32 clt_i4._clt::even
	clt
	brtrue		FAIL


	ldsfld	int32 clt_i4._clt::zero
	ldsfld	int32 clt_i4._clt::min
	clt
	brtrue		FAIL

	ldsfld	int32 clt_i4._clt::zero
	ldsfld	int32 clt_i4._clt::_one
	clt
	brtrue		FAIL

	ldsfld	int32 clt_i4._clt::zero
	ldsfld	int32 clt_i4._clt::zero
	clt
	brtrue		FAIL

	ldsfld	int32 clt_i4._clt::zero
	ldsfld	int32 clt_i4._clt::one
	clt
	brfalse		FAIL

	ldsfld	int32 clt_i4._clt::zero
	ldsfld	int32 clt_i4._clt::max
	clt
	brfalse		FAIL

	ldsfld	int32 clt_i4._clt::zero
	ldsfld	int32 clt_i4._clt::odd
	clt
	brfalse		FAIL

	ldsfld	int32 clt_i4._clt::zero
	ldsfld	int32 clt_i4._clt::even
	clt
	brtrue		FAIL


	ldsfld	int32 clt_i4._clt::one
	ldsfld	int32 clt_i4._clt::min
	clt
	brtrue		FAIL

	ldsfld	int32 clt_i4._clt::one
	ldsfld	int32 clt_i4._clt::_one
	clt
	brtrue		FAIL

	ldsfld	int32 clt_i4._clt::one
	ldsfld	int32 clt_i4._clt::zero
	clt
	brtrue		FAIL

	ldsfld	int32 clt_i4._clt::one
	ldsfld	int32 clt_i4._clt::one
	clt
	brtrue		FAIL

	ldsfld	int32 clt_i4._clt::one
	ldsfld	int32 clt_i4._clt::max
	clt
	brfalse		FAIL

	ldsfld	int32 clt_i4._clt::one
	ldsfld	int32 clt_i4._clt::odd
	clt
	brfalse		FAIL

	ldsfld	int32 clt_i4._clt::one
	ldsfld	int32 clt_i4._clt::even
	clt
	brtrue		FAIL


	ldsfld	int32 clt_i4._clt::max
	ldsfld	int32 clt_i4._clt::min
	clt
	brtrue		FAIL

	ldsfld	int32 clt_i4._clt::max
	ldsfld	int32 clt_i4._clt::_one
	clt
	brtrue		FAIL

	ldsfld	int32 clt_i4._clt::max
	ldsfld	int32 clt_i4._clt::zero
	clt
	brtrue		FAIL

	ldsfld	int32 clt_i4._clt::max
	ldsfld	int32 clt_i4._clt::one
	clt
	brtrue		FAIL

	ldsfld	int32 clt_i4._clt::max
	ldsfld	int32 clt_i4._clt::max
	clt
	brtrue		FAIL

	ldsfld	int32 clt_i4._clt::max
	ldsfld	int32 clt_i4._clt::odd
	clt
	brtrue		FAIL

	ldsfld	int32 clt_i4._clt::max
	ldsfld	int32 clt_i4._clt::even
	clt
	brtrue		FAIL

	ldsfld	int32 clt_i4._clt::odd
	ldsfld	int32 clt_i4._clt::min
	clt
	brtrue		FAIL

	ldsfld	int32 clt_i4._clt::odd
	ldsfld	int32 clt_i4._clt::_one
	clt
	brtrue		FAIL

	ldsfld	int32 clt_i4._clt::odd
	ldsfld	int32 clt_i4._clt::zero
	clt
	brtrue		FAIL

	ldsfld	int32 clt_i4._clt::odd
	ldsfld	int32 clt_i4._clt::one
	clt
	brtrue		FAIL

	ldsfld	int32 clt_i4._clt::odd
	ldsfld	int32 clt_i4._clt::max
	clt
	brfalse		FAIL

	ldsfld	int32 clt_i4._clt::odd
	ldsfld	int32 clt_i4._clt::odd
	clt
	brtrue		FAIL

	ldsfld	int32 clt_i4._clt::odd
	ldsfld	int32 clt_i4._clt::even
	clt
	brtrue		FAIL


	ldsfld	int32 clt_i4._clt::even
	ldsfld	int32 clt_i4._clt::min
	clt
	brtrue		FAIL

	ldsfld	int32 clt_i4._clt::even
	ldsfld	int32 clt_i4._clt::_one
	clt
	brfalse		FAIL

	ldsfld	int32 clt_i4._clt::even
	ldsfld	int32 clt_i4._clt::zero
	clt
	brfalse		FAIL

	ldsfld	int32 clt_i4._clt::even
	ldsfld	int32 clt_i4._clt::one
	clt
	brfalse		FAIL

	ldsfld	int32 clt_i4._clt::even
	ldsfld	int32 clt_i4._clt::max
	clt
	brfalse		FAIL

	ldsfld	int32 clt_i4._clt::even
	ldsfld	int32 clt_i4._clt::odd
	clt
	brfalse		FAIL

	ldsfld	int32 clt_i4._clt::even
	ldsfld	int32 clt_i4._clt::even
	clt
	brtrue		FAIL




	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly clt_i4{}
}
