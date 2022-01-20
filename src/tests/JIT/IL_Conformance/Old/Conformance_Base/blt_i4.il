// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public _blt {

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
	stsfld	int32 _blt::min
	ldc.i4		0xFFFFFFFF
	stsfld	int32 _blt::_one
	ldc.i4		0x00000000
	stsfld	int32 _blt::zero
	ldc.i4		0x00000001
	stsfld	int32 _blt::one
	ldc.i4		0x7FFFFFFF
	stsfld	int32 _blt::max
	ldc.i4		0x55555555
	stsfld	int32 _blt::odd
	ldc.i4		0xAAAAAAAA
	stsfld	int32 _blt::even
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void _blt::initialize()

	ldsfld	int32 _blt::min
	ldsfld	int32 _blt::min
	blt FAIL

A:
	ldsfld	int32 _blt::min
	ldsfld	int32 _blt::_one
	blt B
	br FAIL

B:
	ldsfld	int32 _blt::min
	ldsfld	int32 _blt::zero
	blt C
	br FAIL

C:
	ldsfld	int32 _blt::min
	ldsfld	int32 _blt::one
	blt D
	br FAIL

D:
	ldsfld	int32 _blt::min
	ldsfld	int32 _blt::max
	blt E
	br FAIL

E:
	ldsfld	int32 _blt::min
	ldsfld	int32 _blt::odd
	blt F
	br FAIL

F:
	ldsfld	int32 _blt::min
	ldsfld	int32 _blt::even
	blt G
	br FAIL
G:
	ldsfld	int32 _blt::_one
	ldsfld	int32 _blt::min
	blt FAIL

	ldsfld	int32 _blt::_one
	ldsfld	int32 _blt::_one
	blt FAIL

H:
	ldsfld	int32 _blt::_one
	ldsfld	int32 _blt::zero
	blt I
	br FAIL

I:
	ldsfld	int32 _blt::_one
	ldsfld	int32 _blt::one
	blt J
	br FAIL

J:
	ldsfld	int32 _blt::_one
	ldsfld	int32 _blt::max
	blt K
	br FAIL

K:
	ldsfld	int32 _blt::_one
	ldsfld	int32 _blt::odd
	blt L
	br FAIL

L:
	ldsfld	int32 _blt::_one
	ldsfld	int32 _blt::even
	blt FAIL


	ldsfld	int32 _blt::zero
	ldsfld	int32 _blt::min
	blt FAIL

	ldsfld	int32 _blt::zero
	ldsfld	int32 _blt::_one
	blt FAIL

	ldsfld	int32 _blt::zero
	ldsfld	int32 _blt::zero
	blt FAIL

M:
	ldsfld	int32 _blt::zero
	ldsfld	int32 _blt::one
	blt N
	br FAIL

N:
	ldsfld	int32 _blt::zero
	ldsfld	int32 _blt::max
	blt O
	br FAIL

O:
	ldsfld	int32 _blt::zero
	ldsfld	int32 _blt::odd
	blt P
	br FAIL

P:
	ldsfld	int32 _blt::zero
	ldsfld	int32 _blt::even
	blt FAIL


	ldsfld	int32 _blt::one
	ldsfld	int32 _blt::min
	blt FAIL

	ldsfld	int32 _blt::one
	ldsfld	int32 _blt::_one
	blt FAIL

	ldsfld	int32 _blt::one
	ldsfld	int32 _blt::zero
	blt FAIL

	ldsfld	int32 _blt::one
	ldsfld	int32 _blt::one
	blt FAIL

Q:
	ldsfld	int32 _blt::one
	ldsfld	int32 _blt::max
	blt R
	br FAIL

R:
	ldsfld	int32 _blt::one
	ldsfld	int32 _blt::odd
	blt S
	br FAIL

S:
	ldsfld	int32 _blt::one
	ldsfld	int32 _blt::even
	blt FAIL


	ldsfld	int32 _blt::max
	ldsfld	int32 _blt::min
	blt FAIL

	ldsfld	int32 _blt::max
	ldsfld	int32 _blt::_one
	blt FAIL

	ldsfld	int32 _blt::max
	ldsfld	int32 _blt::zero
	blt FAIL

	ldsfld	int32 _blt::max
	ldsfld	int32 _blt::one
	blt FAIL

	ldsfld	int32 _blt::max
	ldsfld	int32 _blt::max
	blt FAIL

T:
	ldsfld	int32 _blt::max
	ldsfld	int32 _blt::odd
	blt FAIL

	ldsfld	int32 _blt::max
	ldsfld	int32 _blt::even
	blt FAIL

	ldsfld	int32 _blt::odd
	ldsfld	int32 _blt::min
	blt FAIL

	ldsfld	int32 _blt::odd
	ldsfld	int32 _blt::_one
	blt FAIL

	ldsfld	int32 _blt::odd
	ldsfld	int32 _blt::zero
	blt FAIL

	ldsfld	int32 _blt::odd
	ldsfld	int32 _blt::one
	blt FAIL

	ldsfld	int32 _blt::odd
	ldsfld	int32 _blt::max
	blt U
	br FAIL

U:
	ldsfld	int32 _blt::odd
	ldsfld	int32 _blt::odd
	blt FAIL

V:
	ldsfld	int32 _blt::odd
	ldsfld	int32 _blt::even
	blt FAIL


	ldsfld	int32 _blt::even
	ldsfld	int32 _blt::min
	blt FAIL

	ldsfld	int32 _blt::even
	ldsfld	int32 _blt::_one
	blt W
	br FAIL

W:
	ldsfld	int32 _blt::even
	ldsfld	int32 _blt::zero
	blt X
	br FAIL

X:
	ldsfld	int32 _blt::even
	ldsfld	int32 _blt::one
	blt Y
	br FAIL

Y:
	ldsfld	int32 _blt::even
	ldsfld	int32 _blt::max
	blt Z
	br FAIL

Z:
	ldsfld	int32 _blt::even
	ldsfld	int32 _blt::odd
	blt AA
	br FAIL

AA:
	ldsfld	int32 _blt::even
	ldsfld	int32 _blt::even
	blt FAIL
	br BACKCHECK

TOPASS:
	br PASS
BACKCHECK:
	ldc.i4 0x0
	ldc.i4 0x1
	blt TOPASS
	br FAIL




PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly blt_i4{}
