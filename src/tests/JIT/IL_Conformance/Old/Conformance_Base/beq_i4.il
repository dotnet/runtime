// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public _beq {

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
	stsfld	int32 _beq::min
	ldc.i4		0xFFFFFFFF
	stsfld	int32 _beq::_one
	ldc.i4		0x00000000
	stsfld	int32 _beq::zero
	ldc.i4		0x00000001
	stsfld	int32 _beq::one
	ldc.i4		0x7FFFFFFF
	stsfld	int32 _beq::max
	ldc.i4		0x55555555
	stsfld	int32 _beq::odd
	ldc.i4		0xAAAAAAAA
	stsfld	int32 _beq::even
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void _beq::initialize()
	ldsfld	int32 _beq::min
	ldsfld	int32 _beq::min
	beq A
	br FAIL

A:
	ldsfld	int32 _beq::min
	ldsfld	int32 _beq::_one
	beq FAIL

	ldsfld	int32 _beq::min
	ldsfld	int32 _beq::zero
	beq FAIL

	ldsfld	int32 _beq::min
	ldsfld	int32 _beq::one
	beq FAIL

	ldsfld	int32 _beq::min
	ldsfld	int32 _beq::max
	beq FAIL

	ldsfld	int32 _beq::min
	ldsfld	int32 _beq::odd
	beq FAIL

	ldsfld	int32 _beq::min
	ldsfld	int32 _beq::even
	beq FAIL
	ldsfld	int32 _beq::_one
	ldsfld	int32 _beq::min
	beq FAIL

	ldsfld	int32 _beq::_one
	ldsfld	int32 _beq::_one
	beq B
	br FAIL

B:
	ldsfld	int32 _beq::_one
	ldsfld	int32 _beq::zero
	beq FAIL

	ldsfld	int32 _beq::_one
	ldsfld	int32 _beq::one
	beq FAIL

	ldsfld	int32 _beq::_one
	ldsfld	int32 _beq::max
	beq FAIL

	ldsfld	int32 _beq::_one
	ldsfld	int32 _beq::odd
	beq FAIL

	ldsfld	int32 _beq::_one
	ldsfld	int32 _beq::even
	beq FAIL


	ldsfld	int32 _beq::zero
	ldsfld	int32 _beq::min
	beq FAIL

	ldsfld	int32 _beq::zero
	ldsfld	int32 _beq::_one
	beq FAIL

	ldsfld	int32 _beq::zero
	ldsfld	int32 _beq::zero
	beq C
	br FAIL

C:
	ldsfld	int32 _beq::zero
	ldsfld	int32 _beq::one
	beq FAIL

	ldsfld	int32 _beq::zero
	ldsfld	int32 _beq::max
	beq FAIL

	ldsfld	int32 _beq::zero
	ldsfld	int32 _beq::odd
	beq FAIL

	ldsfld	int32 _beq::zero
	ldsfld	int32 _beq::even
	beq FAIL


	ldsfld	int32 _beq::one
	ldsfld	int32 _beq::min
	beq FAIL

	ldsfld	int32 _beq::one
	ldsfld	int32 _beq::_one
	beq FAIL

	ldsfld	int32 _beq::one
	ldsfld	int32 _beq::zero
	beq FAIL

	ldsfld	int32 _beq::one
	ldsfld	int32 _beq::one
	beq D
	br FAIL

D:
	ldsfld	int32 _beq::one
	ldsfld	int32 _beq::max
	beq FAIL

	ldsfld	int32 _beq::one
	ldsfld	int32 _beq::odd
	beq FAIL

	ldsfld	int32 _beq::one
	ldsfld	int32 _beq::even
	beq FAIL


	ldsfld	int32 _beq::max
	ldsfld	int32 _beq::min
	beq FAIL

	ldsfld	int32 _beq::max
	ldsfld	int32 _beq::_one
	beq FAIL

	ldsfld	int32 _beq::max
	ldsfld	int32 _beq::zero
	beq FAIL

	ldsfld	int32 _beq::max
	ldsfld	int32 _beq::one
	beq FAIL

	ldsfld	int32 _beq::max
	ldsfld	int32 _beq::max
	beq E
	br FAIL

E:
	ldsfld	int32 _beq::max
	ldsfld	int32 _beq::odd
	beq FAIL

	ldsfld	int32 _beq::max
	ldsfld	int32 _beq::even
	beq FAIL

	ldsfld	int32 _beq::odd
	ldsfld	int32 _beq::min
	beq FAIL

	ldsfld	int32 _beq::odd
	ldsfld	int32 _beq::_one
	beq FAIL

	ldsfld	int32 _beq::odd
	ldsfld	int32 _beq::zero
	beq FAIL

	ldsfld	int32 _beq::odd
	ldsfld	int32 _beq::one
	beq FAIL

	ldsfld	int32 _beq::odd
	ldsfld	int32 _beq::max
	beq FAIL

	ldsfld	int32 _beq::odd
	ldsfld	int32 _beq::odd
	beq F
	br FAIL

F:
	ldsfld	int32 _beq::odd
	ldsfld	int32 _beq::even
	beq FAIL

	ldsfld	int32 _beq::even
	ldsfld	int32 _beq::min
	beq FAIL

	ldsfld	int32 _beq::even
	ldsfld	int32 _beq::_one
	beq FAIL

	ldsfld	int32 _beq::even
	ldsfld	int32 _beq::zero
	beq FAIL

	ldsfld	int32 _beq::even
	ldsfld	int32 _beq::one
	beq FAIL

	ldsfld	int32 _beq::even
	ldsfld	int32 _beq::max
	beq FAIL

	ldsfld	int32 _beq::even
	ldsfld	int32 _beq::odd
	beq FAIL

	ldsfld	int32 _beq::even
	ldsfld	int32 _beq::even
	beq BACKCHECK
	br FAIL

TOPASS:
	br PASS
BACKCHECK:
	ldc.i4 0x00000000
	ldc.i4 0x00000000
	beq TOPASS
	br FAIL


PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly beq_i4.exe{}
