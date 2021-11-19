// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public _ble {

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
	stsfld	int64 _ble::min
	ldc.i8		0xFFFFFFFFFFFFFFFF
	stsfld	int64 _ble::_one
	ldc.i8		0x0000000000000000
	stsfld	int64 _ble::zero
	ldc.i8		0x0000000000000001
	stsfld	int64 _ble::one
	ldc.i8		0x7FFFFFFFFFFFFFFF
	stsfld	int64 _ble::max
	ldc.i8		0x5555555555555555
	stsfld	int64 _ble::odd
	ldc.i8		0xAAAAAAAAAAAAAAAA
	stsfld	int64 _ble::even
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void _ble::initialize()

	ldsfld	int64 _ble::min
	ldsfld	int64 _ble::min
	ble A
	br FAIL

A:
	ldsfld	int64 _ble::min
	ldsfld	int64 _ble::_one
	ble B
	br FAIL

B:
	ldsfld	int64 _ble::min
	ldsfld	int64 _ble::zero
	ble C
	br FAIL

C:
	ldsfld	int64 _ble::min
	ldsfld	int64 _ble::one
	ble D
	br FAIL

D:
	ldsfld	int64 _ble::min
	ldsfld	int64 _ble::max
	ble E
	br FAIL

E:
	ldsfld	int64 _ble::min
	ldsfld	int64 _ble::odd
	ble F
	br FAIL

F:
	ldsfld	int64 _ble::min
	ldsfld	int64 _ble::even
	ble G
	br FAIL
G:
	ldsfld	int64 _ble::_one
	ldsfld	int64 _ble::min
	ble FAIL

	ldsfld	int64 _ble::_one
	ldsfld	int64 _ble::_one
	ble H
	br FAIL

H:
	ldsfld	int64 _ble::_one
	ldsfld	int64 _ble::zero
	ble I
	br FAIL

I:
	ldsfld	int64 _ble::_one
	ldsfld	int64 _ble::one
	ble J
	br FAIL

J:
	ldsfld	int64 _ble::_one
	ldsfld	int64 _ble::max
	ble K
	br FAIL

K:
	ldsfld	int64 _ble::_one
	ldsfld	int64 _ble::odd
	ble L
	br FAIL

L:
	ldsfld	int64 _ble::_one
	ldsfld	int64 _ble::even
	ble FAIL


	ldsfld	int64 _ble::zero
	ldsfld	int64 _ble::min
	ble FAIL

	ldsfld	int64 _ble::zero
	ldsfld	int64 _ble::_one
	ble FAIL

	ldsfld	int64 _ble::zero
	ldsfld	int64 _ble::zero
	ble M
	br FAIL

M:
	ldsfld	int64 _ble::zero
	ldsfld	int64 _ble::one
	ble N
	br FAIL

N:
	ldsfld	int64 _ble::zero
	ldsfld	int64 _ble::max
	ble O
	br FAIL

O:
	ldsfld	int64 _ble::zero
	ldsfld	int64 _ble::odd
	ble P
	br FAIL

P:
	ldsfld	int64 _ble::zero
	ldsfld	int64 _ble::even
	ble FAIL


	ldsfld	int64 _ble::one
	ldsfld	int64 _ble::min
	ble FAIL

	ldsfld	int64 _ble::one
	ldsfld	int64 _ble::_one
	ble FAIL

	ldsfld	int64 _ble::one
	ldsfld	int64 _ble::zero
	ble FAIL

	ldsfld	int64 _ble::one
	ldsfld	int64 _ble::one
	ble Q
	br FAIL

Q:
	ldsfld	int64 _ble::one
	ldsfld	int64 _ble::max
	ble R
	br FAIL

R:
	ldsfld	int64 _ble::one
	ldsfld	int64 _ble::odd
	ble S
	br FAIL

S:
	ldsfld	int64 _ble::one
	ldsfld	int64 _ble::even
	ble FAIL


	ldsfld	int64 _ble::max
	ldsfld	int64 _ble::min
	ble FAIL

	ldsfld	int64 _ble::max
	ldsfld	int64 _ble::_one
	ble FAIL

	ldsfld	int64 _ble::max
	ldsfld	int64 _ble::zero
	ble FAIL

	ldsfld	int64 _ble::max
	ldsfld	int64 _ble::one
	ble FAIL

	ldsfld	int64 _ble::max
	ldsfld	int64 _ble::max
	ble T
	br FAIL

T:
	ldsfld	int64 _ble::max
	ldsfld	int64 _ble::odd
	ble FAIL

	ldsfld	int64 _ble::max
	ldsfld	int64 _ble::even
	ble FAIL

	ldsfld	int64 _ble::odd
	ldsfld	int64 _ble::min
	ble FAIL

	ldsfld	int64 _ble::odd
	ldsfld	int64 _ble::_one
	ble FAIL

	ldsfld	int64 _ble::odd
	ldsfld	int64 _ble::zero
	ble FAIL

	ldsfld	int64 _ble::odd
	ldsfld	int64 _ble::one
	ble FAIL

	ldsfld	int64 _ble::odd
	ldsfld	int64 _ble::max
	ble U
	br FAIL

U:
	ldsfld	int64 _ble::odd
	ldsfld	int64 _ble::odd
	ble V
	br FAIL

V:
	ldsfld	int64 _ble::odd
	ldsfld	int64 _ble::even
	ble FAIL


	ldsfld	int64 _ble::even
	ldsfld	int64 _ble::min
	ble FAIL

	ldsfld	int64 _ble::even
	ldsfld	int64 _ble::_one
	ble W
	br FAIL

W:
	ldsfld	int64 _ble::even
	ldsfld	int64 _ble::zero
	ble X
	br FAIL

X:
	ldsfld	int64 _ble::even
	ldsfld	int64 _ble::one
	ble Y
	br FAIL

Y:
	ldsfld	int64 _ble::even
	ldsfld	int64 _ble::max
	ble Z
	br FAIL

Z:
	ldsfld	int64 _ble::even
	ldsfld	int64 _ble::odd
	ble AA
	br FAIL

AA:
	ldsfld	int64 _ble::even
	ldsfld	int64 _ble::even
	ble BACKCHECK
	br FAIL

TOPASS:
	br PASS
BACKCHECK:
	ldc.i8 0x0
	ldc.i8 0x1
	ble TOPASS
	br FAIL




PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ble_i8{}
