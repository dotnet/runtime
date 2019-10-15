// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}


.class public _ble {

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
	stsfld	int32 _ble::min
	ldc.i4		0xFFFFFFFF
	stsfld	int32 _ble::_one
	ldc.i4		0x00000000
	stsfld	int32 _ble::zero
	ldc.i4		0x00000001
	stsfld	int32 _ble::one
	ldc.i4		0x7FFFFFFF
	stsfld	int32 _ble::max
	ldc.i4		0x55555555
	stsfld	int32 _ble::odd
	ldc.i4		0xAAAAAAAA
	stsfld	int32 _ble::even
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void _ble::initialize()

	ldsfld	int32 _ble::min
	ldsfld	int32 _ble::min
	ble A
	br FAIL

A:
	ldsfld	int32 _ble::min
	ldsfld	int32 _ble::_one
	ble B
	br FAIL

B:
	ldsfld	int32 _ble::min
	ldsfld	int32 _ble::zero
	ble C
	br FAIL

C:
	ldsfld	int32 _ble::min
	ldsfld	int32 _ble::one
	ble D
	br FAIL

D:
	ldsfld	int32 _ble::min
	ldsfld	int32 _ble::max
	ble E
	br FAIL

E:
	ldsfld	int32 _ble::min
	ldsfld	int32 _ble::odd
	ble F
	br FAIL

F:
	ldsfld	int32 _ble::min
	ldsfld	int32 _ble::even
	ble G
	br FAIL
G:
	ldsfld	int32 _ble::_one
	ldsfld	int32 _ble::min
	ble FAIL

	ldsfld	int32 _ble::_one
	ldsfld	int32 _ble::_one
	ble H
	br FAIL

H:
	ldsfld	int32 _ble::_one
	ldsfld	int32 _ble::zero
	ble I
	br FAIL

I:
	ldsfld	int32 _ble::_one
	ldsfld	int32 _ble::one
	ble J
	br FAIL

J:
	ldsfld	int32 _ble::_one
	ldsfld	int32 _ble::max
	ble K
	br FAIL

K:
	ldsfld	int32 _ble::_one
	ldsfld	int32 _ble::odd
	ble L
	br FAIL

L:
	ldsfld	int32 _ble::_one
	ldsfld	int32 _ble::even
	ble FAIL


	ldsfld	int32 _ble::zero
	ldsfld	int32 _ble::min
	ble FAIL

	ldsfld	int32 _ble::zero
	ldsfld	int32 _ble::_one
	ble FAIL

	ldsfld	int32 _ble::zero
	ldsfld	int32 _ble::zero
	ble M
	br FAIL

M:
	ldsfld	int32 _ble::zero
	ldsfld	int32 _ble::one
	ble N
	br FAIL

N:
	ldsfld	int32 _ble::zero
	ldsfld	int32 _ble::max
	ble O
	br FAIL

O:
	ldsfld	int32 _ble::zero
	ldsfld	int32 _ble::odd
	ble P
	br FAIL

P:
	ldsfld	int32 _ble::zero
	ldsfld	int32 _ble::even
	ble FAIL


	ldsfld	int32 _ble::one
	ldsfld	int32 _ble::min
	ble FAIL

	ldsfld	int32 _ble::one
	ldsfld	int32 _ble::_one
	ble FAIL

	ldsfld	int32 _ble::one
	ldsfld	int32 _ble::zero
	ble FAIL

	ldsfld	int32 _ble::one
	ldsfld	int32 _ble::one
	ble Q
	br FAIL

Q:
	ldsfld	int32 _ble::one
	ldsfld	int32 _ble::max
	ble R
	br FAIL

R:
	ldsfld	int32 _ble::one
	ldsfld	int32 _ble::odd
	ble S
	br FAIL

S:
	ldsfld	int32 _ble::one
	ldsfld	int32 _ble::even
	ble FAIL


	ldsfld	int32 _ble::max
	ldsfld	int32 _ble::min
	ble FAIL

	ldsfld	int32 _ble::max
	ldsfld	int32 _ble::_one
	ble FAIL

	ldsfld	int32 _ble::max
	ldsfld	int32 _ble::zero
	ble FAIL

	ldsfld	int32 _ble::max
	ldsfld	int32 _ble::one
	ble FAIL

	ldsfld	int32 _ble::max
	ldsfld	int32 _ble::max
	ble T
	br FAIL

T:
	ldsfld	int32 _ble::max
	ldsfld	int32 _ble::odd
	ble FAIL

	ldsfld	int32 _ble::max
	ldsfld	int32 _ble::even
	ble FAIL

	ldsfld	int32 _ble::odd
	ldsfld	int32 _ble::min
	ble FAIL

	ldsfld	int32 _ble::odd
	ldsfld	int32 _ble::_one
	ble FAIL

	ldsfld	int32 _ble::odd
	ldsfld	int32 _ble::zero
	ble FAIL

	ldsfld	int32 _ble::odd
	ldsfld	int32 _ble::one
	ble FAIL

	ldsfld	int32 _ble::odd
	ldsfld	int32 _ble::max
	ble U
	br FAIL

U:
	ldsfld	int32 _ble::odd
	ldsfld	int32 _ble::odd
	ble V
	br FAIL

V:
	ldsfld	int32 _ble::odd
	ldsfld	int32 _ble::even
	ble FAIL


	ldsfld	int32 _ble::even
	ldsfld	int32 _ble::min
	ble FAIL

	ldsfld	int32 _ble::even
	ldsfld	int32 _ble::_one
	ble W
	br FAIL

W:
	ldsfld	int32 _ble::even
	ldsfld	int32 _ble::zero
	ble X
	br FAIL

X:
	ldsfld	int32 _ble::even
	ldsfld	int32 _ble::one
	ble Y
	br FAIL

Y:
	ldsfld	int32 _ble::even
	ldsfld	int32 _ble::max
	ble Z
	br FAIL

Z:
	ldsfld	int32 _ble::even
	ldsfld	int32 _ble::odd
	ble AA
	br FAIL

AA:
	ldsfld	int32 _ble::even
	ldsfld	int32 _ble::even
	ble BACKCHECK
	br FAIL

TOPASS:
	br PASS
BACKCHECK:
	ldc.i4 0x0
	ldc.i4 0x1
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
.assembly ble_i4.exe{}
