// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public _blt {

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
	stsfld	int64 _blt::min
	ldc.i8		0xFFFFFFFFFFFFFFFF
	stsfld	int64 _blt::_one
	ldc.i8		0x0000000000000000
	stsfld	int64 _blt::zero
	ldc.i8		0x0000000000000001
	stsfld	int64 _blt::one
	ldc.i8		0x7FFFFFFFFFFFFFFF
	stsfld	int64 _blt::max
	ldc.i8		0x5555555555555555
	stsfld	int64 _blt::odd
	ldc.i8		0xAAAAAAAAAAAAAAAA
	stsfld	int64 _blt::even
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void _blt::initialize()

	ldsfld	int64 _blt::min
	ldsfld	int64 _blt::min
	blt FAIL

A:
	ldsfld	int64 _blt::min
	ldsfld	int64 _blt::_one
	blt B
	br FAIL

B:
	ldsfld	int64 _blt::min
	ldsfld	int64 _blt::zero
	blt C
	br FAIL

C:
	ldsfld	int64 _blt::min
	ldsfld	int64 _blt::one
	blt D
	br FAIL

D:
	ldsfld	int64 _blt::min
	ldsfld	int64 _blt::max
	blt E
	br FAIL

E:
	ldsfld	int64 _blt::min
	ldsfld	int64 _blt::odd
	blt F
	br FAIL

F:
	ldsfld	int64 _blt::min
	ldsfld	int64 _blt::even
	blt G
	br FAIL
G:
	ldsfld	int64 _blt::_one
	ldsfld	int64 _blt::min
	blt FAIL

	ldsfld	int64 _blt::_one
	ldsfld	int64 _blt::_one
	blt FAIL

H:
	ldsfld	int64 _blt::_one
	ldsfld	int64 _blt::zero
	blt I
	br FAIL

I:
	ldsfld	int64 _blt::_one
	ldsfld	int64 _blt::one
	blt J
	br FAIL

J:
	ldsfld	int64 _blt::_one
	ldsfld	int64 _blt::max
	blt K
	br FAIL

K:
	ldsfld	int64 _blt::_one
	ldsfld	int64 _blt::odd
	blt L
	br FAIL

L:
	ldsfld	int64 _blt::_one
	ldsfld	int64 _blt::even
	blt FAIL


	ldsfld	int64 _blt::zero
	ldsfld	int64 _blt::min
	blt FAIL

	ldsfld	int64 _blt::zero
	ldsfld	int64 _blt::_one
	blt FAIL

	ldsfld	int64 _blt::zero
	ldsfld	int64 _blt::zero
	blt FAIL

M:
	ldsfld	int64 _blt::zero
	ldsfld	int64 _blt::one
	blt N
	br FAIL

N:
	ldsfld	int64 _blt::zero
	ldsfld	int64 _blt::max
	blt O
	br FAIL

O:
	ldsfld	int64 _blt::zero
	ldsfld	int64 _blt::odd
	blt P
	br FAIL

P:
	ldsfld	int64 _blt::zero
	ldsfld	int64 _blt::even
	blt FAIL


	ldsfld	int64 _blt::one
	ldsfld	int64 _blt::min
	blt FAIL

	ldsfld	int64 _blt::one
	ldsfld	int64 _blt::_one
	blt FAIL

	ldsfld	int64 _blt::one
	ldsfld	int64 _blt::zero
	blt FAIL

	ldsfld	int64 _blt::one
	ldsfld	int64 _blt::one
	blt FAIL

Q:
	ldsfld	int64 _blt::one
	ldsfld	int64 _blt::max
	blt R
	br FAIL

R:
	ldsfld	int64 _blt::one
	ldsfld	int64 _blt::odd
	blt S
	br FAIL

S:
	ldsfld	int64 _blt::one
	ldsfld	int64 _blt::even
	blt FAIL


	ldsfld	int64 _blt::max
	ldsfld	int64 _blt::min
	blt FAIL

	ldsfld	int64 _blt::max
	ldsfld	int64 _blt::_one
	blt FAIL

	ldsfld	int64 _blt::max
	ldsfld	int64 _blt::zero
	blt FAIL

	ldsfld	int64 _blt::max
	ldsfld	int64 _blt::one
	blt FAIL

	ldsfld	int64 _blt::max
	ldsfld	int64 _blt::max
	blt FAIL

T:
	ldsfld	int64 _blt::max
	ldsfld	int64 _blt::odd
	blt FAIL

	ldsfld	int64 _blt::max
	ldsfld	int64 _blt::even
	blt FAIL

	ldsfld	int64 _blt::odd
	ldsfld	int64 _blt::min
	blt FAIL

	ldsfld	int64 _blt::odd
	ldsfld	int64 _blt::_one
	blt FAIL

	ldsfld	int64 _blt::odd
	ldsfld	int64 _blt::zero
	blt FAIL

	ldsfld	int64 _blt::odd
	ldsfld	int64 _blt::one
	blt FAIL

	ldsfld	int64 _blt::odd
	ldsfld	int64 _blt::max
	blt U
	br FAIL

U:
	ldsfld	int64 _blt::odd
	ldsfld	int64 _blt::odd
	blt FAIL

V:
	ldsfld	int64 _blt::odd
	ldsfld	int64 _blt::even
	blt FAIL


	ldsfld	int64 _blt::even
	ldsfld	int64 _blt::min
	blt FAIL

	ldsfld	int64 _blt::even
	ldsfld	int64 _blt::_one
	blt W
	br FAIL

W:
	ldsfld	int64 _blt::even
	ldsfld	int64 _blt::zero
	blt X
	br FAIL

X:
	ldsfld	int64 _blt::even
	ldsfld	int64 _blt::one
	blt Y
	br FAIL

Y:
	ldsfld	int64 _blt::even
	ldsfld	int64 _blt::max
	blt Z
	br FAIL

Z:
	ldsfld	int64 _blt::even
	ldsfld	int64 _blt::odd
	blt AA
	br FAIL

AA:
	ldsfld	int64 _blt::even
	ldsfld	int64 _blt::even
	blt FAIL
	br BACKCHECK

TOPASS:
	br PASS
BACKCHECK:
	ldc.i8 0x0
	ldc.i8 0x1
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
.assembly blt_i8.exe{}
