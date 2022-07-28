// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public _bge {

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
	stsfld	int64 _bge::min
	ldc.i8		0xFFFFFFFFFFFFFFFF
	stsfld	int64 _bge::_one
	ldc.i8		0x0000000000000000
	stsfld	int64 _bge::zero
	ldc.i8		0x0000000000000001
	stsfld	int64 _bge::one
	ldc.i8		0x7FFFFFFFFFFFFFFF
	stsfld	int64 _bge::max
	ldc.i8		0x5555555555555555
	stsfld	int64 _bge::odd
	ldc.i8		0xAAAAAAAAAAAAAAAA
	stsfld	int64 _bge::even
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void _bge::initialize()
	ldsfld	int64 _bge::min
	ldsfld	int64 _bge::min
	bge A
	br FAIL

A:
	ldsfld	int64 _bge::min
	ldsfld	int64 _bge::_one
	bge FAIL

	ldsfld	int64 _bge::min
	ldsfld	int64 _bge::zero
	bge FAIL

	ldsfld	int64 _bge::min
	ldsfld	int64 _bge::one
	bge FAIL

	ldsfld	int64 _bge::min
	ldsfld	int64 _bge::max
	bge FAIL

	ldsfld	int64 _bge::min
	ldsfld	int64 _bge::odd
	bge FAIL

	ldsfld	int64 _bge::min
	ldsfld	int64 _bge::even
	bge FAIL
	ldsfld	int64 _bge::_one
	ldsfld	int64 _bge::min
	bge B
	br FAIL

B:
	ldsfld	int64 _bge::_one
	ldsfld	int64 _bge::_one
	bge C
	br FAIL

C:
	ldsfld	int64 _bge::_one
	ldsfld	int64 _bge::zero
	bge FAIL

	ldsfld	int64 _bge::_one
	ldsfld	int64 _bge::one
	bge FAIL

	ldsfld	int64 _bge::_one
	ldsfld	int64 _bge::max
	bge FAIL

	ldsfld	int64 _bge::_one
	ldsfld	int64 _bge::odd
	bge FAIL

	ldsfld	int64 _bge::_one
	ldsfld	int64 _bge::even
	bge _C
	br FAIL


_C:
	ldsfld	int64 _bge::zero
	ldsfld	int64 _bge::min
	bge D
	br FAIL

D:
	ldsfld	int64 _bge::zero
	ldsfld	int64 _bge::_one
	bge E
	br FAIL

E:
	ldsfld	int64 _bge::zero
	ldsfld	int64 _bge::zero
	bge F
	br FAIL

F:
	ldsfld	int64 _bge::zero
	ldsfld	int64 _bge::one
	bge FAIL

	ldsfld	int64 _bge::zero
	ldsfld	int64 _bge::max
	bge FAIL

	ldsfld	int64 _bge::zero
	ldsfld	int64 _bge::odd
	bge FAIL

	ldsfld	int64 _bge::zero
	ldsfld	int64 _bge::even
	bge _F
	br FAIL


_F:
	ldsfld	int64 _bge::one
	ldsfld	int64 _bge::min
	bge G
	br FAIL

G:
	ldsfld	int64 _bge::one
	ldsfld	int64 _bge::_one
	bge H
	br FAIL

H:
	ldsfld	int64 _bge::one
	ldsfld	int64 _bge::zero
	bge I
	br FAIL

I:
	ldsfld	int64 _bge::one
	ldsfld	int64 _bge::one
	bge J
	br FAIL

J:
	ldsfld	int64 _bge::one
	ldsfld	int64 _bge::max
	bge FAIL

	ldsfld	int64 _bge::one
	ldsfld	int64 _bge::odd
	bge FAIL

	ldsfld	int64 _bge::one
	ldsfld	int64 _bge::even
	bge _J
	br FAIL


_J:
	ldsfld	int64 _bge::max
	ldsfld	int64 _bge::min
	bge K
	br FAIL

K:
	ldsfld	int64 _bge::max
	ldsfld	int64 _bge::_one
	bge L
	br FAIL

L:
	ldsfld	int64 _bge::max
	ldsfld	int64 _bge::zero
	bge M
	br FAIL

M:
	ldsfld	int64 _bge::max
	ldsfld	int64 _bge::one
	bge N
	br FAIL

N:
	ldsfld	int64 _bge::max
	ldsfld	int64 _bge::max
	bge O
	br FAIL

O:
	ldsfld	int64 _bge::max
	ldsfld	int64 _bge::odd
	bge P
	br FAIL

P:
	ldsfld	int64 _bge::max
	ldsfld	int64 _bge::even
	bge Q
	br FAIL

Q:
	ldsfld	int64 _bge::odd
	ldsfld	int64 _bge::min
	bge R
	br FAIL

R:
	ldsfld	int64 _bge::odd
	ldsfld	int64 _bge::_one
	bge S
	br FAIL

S:
	ldsfld	int64 _bge::odd
	ldsfld	int64 _bge::zero
	bge T
	br FAIL

T:
	ldsfld	int64 _bge::odd
	ldsfld	int64 _bge::one
	bge U
	br FAIL

U:
	ldsfld	int64 _bge::odd
	ldsfld	int64 _bge::max
	bge FAIL

	ldsfld	int64 _bge::odd
	ldsfld	int64 _bge::odd
	bge V
	br FAIL

V:
	ldsfld	int64 _bge::odd
	ldsfld	int64 _bge::even
	bge _V
	br FAIL

_V:
	ldsfld	int64 _bge::even
	ldsfld	int64 _bge::min
	bge W
	br FAIL

W:
	ldsfld	int64 _bge::even
	ldsfld	int64 _bge::_one
	bge FAIL

	ldsfld	int64 _bge::even
	ldsfld	int64 _bge::zero
	bge FAIL

	ldsfld	int64 _bge::even
	ldsfld	int64 _bge::one
	bge FAIL

	ldsfld	int64 _bge::even
	ldsfld	int64 _bge::max
	bge FAIL

	ldsfld	int64 _bge::even
	ldsfld	int64 _bge::odd
	bge FAIL

	ldsfld	int64 _bge::even
	ldsfld	int64 _bge::even
	bge BACKCHECK
	br FAIL

TOPASS:
	br PASS
BACKCHECK:
	ldc.i8 0x1
	ldc.i8 0x0
	bge TOPASS
	br FAIL

PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly bge_i8{}
