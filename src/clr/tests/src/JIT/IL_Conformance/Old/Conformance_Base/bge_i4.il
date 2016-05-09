// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}


.class public _bge {

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
	stsfld	int32 _bge::min
	ldc.i4		0xFFFFFFFF
	stsfld	int32 _bge::_one
	ldc.i4		0x00000000
	stsfld	int32 _bge::zero
	ldc.i4		0x00000001
	stsfld	int32 _bge::one
	ldc.i4		0x7FFFFFFF
	stsfld	int32 _bge::max
	ldc.i4		0x55555555
	stsfld	int32 _bge::odd
	ldc.i4		0xAAAAAAAA
	stsfld	int32 _bge::even
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void _bge::initialize()
	ldsfld	int32 _bge::min
	ldsfld	int32 _bge::min
	bge A
	br FAIL

A:
	ldsfld	int32 _bge::min
	ldsfld	int32 _bge::_one
	bge FAIL

	ldsfld	int32 _bge::min
	ldsfld	int32 _bge::zero
	bge FAIL

	ldsfld	int32 _bge::min
	ldsfld	int32 _bge::one
	bge FAIL

	ldsfld	int32 _bge::min
	ldsfld	int32 _bge::max
	bge FAIL

	ldsfld	int32 _bge::min
	ldsfld	int32 _bge::odd
	bge FAIL

	ldsfld	int32 _bge::min
	ldsfld	int32 _bge::even
	bge FAIL
	ldsfld	int32 _bge::_one
	ldsfld	int32 _bge::min
	bge B
	br FAIL

B:
	ldsfld	int32 _bge::_one
	ldsfld	int32 _bge::_one
	bge C
	br FAIL

C:
	ldsfld	int32 _bge::_one
	ldsfld	int32 _bge::zero
	bge FAIL

	ldsfld	int32 _bge::_one
	ldsfld	int32 _bge::one
	bge FAIL

	ldsfld	int32 _bge::_one
	ldsfld	int32 _bge::max
	bge FAIL

	ldsfld	int32 _bge::_one
	ldsfld	int32 _bge::odd
	bge FAIL

	ldsfld	int32 _bge::_one
	ldsfld	int32 _bge::even
	bge _C
	br FAIL


_C:
	ldsfld	int32 _bge::zero
	ldsfld	int32 _bge::min
	bge D
	br FAIL

D:
	ldsfld	int32 _bge::zero
	ldsfld	int32 _bge::_one
	bge E
	br FAIL

E:
	ldsfld	int32 _bge::zero
	ldsfld	int32 _bge::zero
	bge F
	br FAIL

F:
	ldsfld	int32 _bge::zero
	ldsfld	int32 _bge::one
	bge FAIL

	ldsfld	int32 _bge::zero
	ldsfld	int32 _bge::max
	bge FAIL

	ldsfld	int32 _bge::zero
	ldsfld	int32 _bge::odd
	bge FAIL

	ldsfld	int32 _bge::zero
	ldsfld	int32 _bge::even
	bge _F
	br FAIL


_F:
	ldsfld	int32 _bge::one
	ldsfld	int32 _bge::min
	bge G
	br FAIL

G:
	ldsfld	int32 _bge::one
	ldsfld	int32 _bge::_one
	bge H
	br FAIL

H:
	ldsfld	int32 _bge::one
	ldsfld	int32 _bge::zero
	bge I
	br FAIL

I:
	ldsfld	int32 _bge::one
	ldsfld	int32 _bge::one
	bge J
	br FAIL

J:
	ldsfld	int32 _bge::one
	ldsfld	int32 _bge::max
	bge FAIL

	ldsfld	int32 _bge::one
	ldsfld	int32 _bge::odd
	bge FAIL

	ldsfld	int32 _bge::one
	ldsfld	int32 _bge::even
	bge _J
	br FAIL


_J:
	ldsfld	int32 _bge::max
	ldsfld	int32 _bge::min
	bge K
	br FAIL

K:
	ldsfld	int32 _bge::max
	ldsfld	int32 _bge::_one
	bge L
	br FAIL

L:
	ldsfld	int32 _bge::max
	ldsfld	int32 _bge::zero
	bge M
	br FAIL

M:
	ldsfld	int32 _bge::max
	ldsfld	int32 _bge::one
	bge N
	br FAIL

N:
	ldsfld	int32 _bge::max
	ldsfld	int32 _bge::max
	bge O
	br FAIL

O:
	ldsfld	int32 _bge::max
	ldsfld	int32 _bge::odd
	bge P
	br FAIL

P:
	ldsfld	int32 _bge::max
	ldsfld	int32 _bge::even
	bge Q
	br FAIL

Q:
	ldsfld	int32 _bge::odd
	ldsfld	int32 _bge::min
	bge R
	br FAIL

R:
	ldsfld	int32 _bge::odd
	ldsfld	int32 _bge::_one
	bge S
	br FAIL

S:
	ldsfld	int32 _bge::odd
	ldsfld	int32 _bge::zero
	bge T
	br FAIL

T:
	ldsfld	int32 _bge::odd
	ldsfld	int32 _bge::one
	bge U
	br FAIL

U:
	ldsfld	int32 _bge::odd
	ldsfld	int32 _bge::max
	bge FAIL

	ldsfld	int32 _bge::odd
	ldsfld	int32 _bge::odd
	bge V
	br FAIL

V:
	ldsfld	int32 _bge::odd
	ldsfld	int32 _bge::even
	bge _V
	br FAIL

_V:
	ldsfld	int32 _bge::even
	ldsfld	int32 _bge::min
	bge W
	br FAIL

W:
	ldsfld	int32 _bge::even
	ldsfld	int32 _bge::_one
	bge FAIL

	ldsfld	int32 _bge::even
	ldsfld	int32 _bge::zero
	bge FAIL

	ldsfld	int32 _bge::even
	ldsfld	int32 _bge::one
	bge FAIL

	ldsfld	int32 _bge::even
	ldsfld	int32 _bge::max
	bge FAIL

	ldsfld	int32 _bge::even
	ldsfld	int32 _bge::odd
	bge FAIL

	ldsfld	int32 _bge::even
	ldsfld	int32 _bge::even
	bge BACKCHECK
	br FAIL

TOPASS:
	br PASS
BACKCHECK:
	ldc.i4 0x1
	ldc.i4 0x0
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
.assembly bge_i4.exe{}
