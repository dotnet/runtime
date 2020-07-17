// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public _beq {

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
	stsfld	int64 _beq::min
	ldc.i8		0xFFFFFFFFFFFFFFFF
	stsfld	int64 _beq::_one
	ldc.i8		0x0000000000000000
	stsfld	int64 _beq::zero
	ldc.i8		0x0000000000000001
	stsfld	int64 _beq::one
	ldc.i8		0x7FFFFFFFFFFFFFFF
	stsfld	int64 _beq::max
	ldc.i8		0x5555555555555555
	stsfld	int64 _beq::odd
	ldc.i8		0xAAAAAAAAAAAAAAAA
	stsfld	int64 _beq::even
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void _beq::initialize()
	ldsfld	int64 _beq::min
	ldsfld	int64 _beq::min
	beq A
	br FAIL

A:
	ldsfld	int64 _beq::min
	ldsfld	int64 _beq::_one
	beq FAIL

	ldsfld	int64 _beq::min
	ldsfld	int64 _beq::zero
	beq FAIL

	ldsfld	int64 _beq::min
	ldsfld	int64 _beq::one
	beq FAIL

	ldsfld	int64 _beq::min
	ldsfld	int64 _beq::max
	beq FAIL

	ldsfld	int64 _beq::min
	ldsfld	int64 _beq::odd
	beq FAIL

	ldsfld	int64 _beq::min
	ldsfld	int64 _beq::even
	beq FAIL
	ldsfld	int64 _beq::_one
	ldsfld	int64 _beq::min
	beq FAIL

	ldsfld	int64 _beq::_one
	ldsfld	int64 _beq::_one
	beq B
	br FAIL

B:
	ldsfld	int64 _beq::_one
	ldsfld	int64 _beq::zero
	beq FAIL

	ldsfld	int64 _beq::_one
	ldsfld	int64 _beq::one
	beq FAIL

	ldsfld	int64 _beq::_one
	ldsfld	int64 _beq::max
	beq FAIL

	ldsfld	int64 _beq::_one
	ldsfld	int64 _beq::odd
	beq FAIL

	ldsfld	int64 _beq::_one
	ldsfld	int64 _beq::even
	beq FAIL


	ldsfld	int64 _beq::zero
	ldsfld	int64 _beq::min
	beq FAIL

	ldsfld	int64 _beq::zero
	ldsfld	int64 _beq::_one
	beq FAIL

	ldsfld	int64 _beq::zero
	ldsfld	int64 _beq::zero
	beq C
	br FAIL

C:
	ldsfld	int64 _beq::zero
	ldsfld	int64 _beq::one
	beq FAIL

	ldsfld	int64 _beq::zero
	ldsfld	int64 _beq::max
	beq FAIL

	ldsfld	int64 _beq::zero
	ldsfld	int64 _beq::odd
	beq FAIL

	ldsfld	int64 _beq::zero
	ldsfld	int64 _beq::even
	beq FAIL


	ldsfld	int64 _beq::one
	ldsfld	int64 _beq::min
	beq FAIL

	ldsfld	int64 _beq::one
	ldsfld	int64 _beq::_one
	beq FAIL

	ldsfld	int64 _beq::one
	ldsfld	int64 _beq::zero
	beq FAIL

	ldsfld	int64 _beq::one
	ldsfld	int64 _beq::one
	beq D
	br FAIL

D:
	ldsfld	int64 _beq::one
	ldsfld	int64 _beq::max
	beq FAIL

	ldsfld	int64 _beq::one
	ldsfld	int64 _beq::odd
	beq FAIL

	ldsfld	int64 _beq::one
	ldsfld	int64 _beq::even
	beq FAIL


	ldsfld	int64 _beq::max
	ldsfld	int64 _beq::min
	beq FAIL

	ldsfld	int64 _beq::max
	ldsfld	int64 _beq::_one
	beq FAIL

	ldsfld	int64 _beq::max
	ldsfld	int64 _beq::zero
	beq FAIL

	ldsfld	int64 _beq::max
	ldsfld	int64 _beq::one
	beq FAIL

	ldsfld	int64 _beq::max
	ldsfld	int64 _beq::max
	beq E
	br FAIL

E:
	ldsfld	int64 _beq::max
	ldsfld	int64 _beq::odd
	beq FAIL

	ldsfld	int64 _beq::max
	ldsfld	int64 _beq::even
	beq FAIL

	ldsfld	int64 _beq::odd
	ldsfld	int64 _beq::min
	beq FAIL

	ldsfld	int64 _beq::odd
	ldsfld	int64 _beq::_one
	beq FAIL

	ldsfld	int64 _beq::odd
	ldsfld	int64 _beq::zero
	beq FAIL

	ldsfld	int64 _beq::odd
	ldsfld	int64 _beq::one
	beq FAIL

	ldsfld	int64 _beq::odd
	ldsfld	int64 _beq::max
	beq FAIL

	ldsfld	int64 _beq::odd
	ldsfld	int64 _beq::odd
	beq F
	br FAIL

F:
	ldsfld	int64 _beq::odd
	ldsfld	int64 _beq::even
	beq FAIL

	ldsfld	int64 _beq::even
	ldsfld	int64 _beq::min
	beq FAIL

	ldsfld	int64 _beq::even
	ldsfld	int64 _beq::_one
	beq FAIL

	ldsfld	int64 _beq::even
	ldsfld	int64 _beq::zero
	beq FAIL

	ldsfld	int64 _beq::even
	ldsfld	int64 _beq::one
	beq FAIL

	ldsfld	int64 _beq::even
	ldsfld	int64 _beq::max
	beq FAIL

	ldsfld	int64 _beq::even
	ldsfld	int64 _beq::odd
	beq FAIL

	ldsfld	int64 _beq::even
	ldsfld	int64 _beq::even
	beq BACKCHECK
	br FAIL

TOPASS:
	br PASS
BACKCHECK:
	ldc.i8 0x0
	ldc.i8 0x0
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
.assembly beq_i8.exe{}
