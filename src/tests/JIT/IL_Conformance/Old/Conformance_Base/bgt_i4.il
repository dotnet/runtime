// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}


.class public _bgt {

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
	stsfld	int32 _bgt::min
	ldc.i4		0xFFFFFFFF
	stsfld	int32 _bgt::_one
	ldc.i4		0x00000000
	stsfld	int32 _bgt::zero
	ldc.i4		0x00000001
	stsfld	int32 _bgt::one
	ldc.i4		0x7FFFFFFF
	stsfld	int32 _bgt::max
	ldc.i4		0x55555555
	stsfld	int32 _bgt::odd
	ldc.i4		0xAAAAAAAA
	stsfld	int32 _bgt::even
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void _bgt::initialize()

	ldsfld	int32 _bgt::min
	ldsfld	int32 _bgt::min
	bgt FAIL

	ldsfld	int32 _bgt::min
	ldsfld	int32 _bgt::_one
	bgt FAIL

	ldsfld	int32 _bgt::min
	ldsfld	int32 _bgt::zero
	bgt FAIL

	ldsfld	int32 _bgt::min
	ldsfld	int32 _bgt::one
	bgt FAIL

	ldsfld	int32 _bgt::min
	ldsfld	int32 _bgt::max
	bgt FAIL

	ldsfld	int32 _bgt::min
	ldsfld	int32 _bgt::odd
	bgt FAIL

	ldsfld	int32 _bgt::min
	ldsfld	int32 _bgt::even
	bgt FAIL
	ldsfld	int32 _bgt::_one
	ldsfld	int32 _bgt::min
	bgt A
	br FAIL

A:
	ldsfld	int32 _bgt::_one
	ldsfld	int32 _bgt::_one
	bgt FAIL

	ldsfld	int32 _bgt::_one
	ldsfld	int32 _bgt::zero
	bgt FAIL

	ldsfld	int32 _bgt::_one
	ldsfld	int32 _bgt::one
	bgt FAIL

	ldsfld	int32 _bgt::_one
	ldsfld	int32 _bgt::max
	bgt FAIL

	ldsfld	int32 _bgt::_one
	ldsfld	int32 _bgt::odd
	bgt FAIL

	ldsfld	int32 _bgt::_one
	ldsfld	int32 _bgt::even
	bgt _A
	br FAIL


_A:
	ldsfld	int32 _bgt::zero
	ldsfld	int32 _bgt::min
	bgt B
	br FAIL

B:
	ldsfld	int32 _bgt::zero
	ldsfld	int32 _bgt::_one
	bgt C
	br FAIL

C:
	ldsfld	int32 _bgt::zero
	ldsfld	int32 _bgt::zero
	bgt FAIL

	ldsfld	int32 _bgt::zero
	ldsfld	int32 _bgt::one
	bgt FAIL

	ldsfld	int32 _bgt::zero
	ldsfld	int32 _bgt::max
	bgt FAIL

	ldsfld	int32 _bgt::zero
	ldsfld	int32 _bgt::odd
	bgt FAIL

	ldsfld	int32 _bgt::zero
	ldsfld	int32 _bgt::even
	bgt _C
	br FAIL


_C:
	ldsfld	int32 _bgt::one
	ldsfld	int32 _bgt::min
	bgt D
	br FAIL

D:
	ldsfld	int32 _bgt::one
	ldsfld	int32 _bgt::_one
	bgt E
	br FAIL

E:
	ldsfld	int32 _bgt::one
	ldsfld	int32 _bgt::zero
	bgt F
	br FAIL

F:
	ldsfld	int32 _bgt::one
	ldsfld	int32 _bgt::one
	bgt FAIL

	ldsfld	int32 _bgt::one
	ldsfld	int32 _bgt::max
	bgt FAIL

	ldsfld	int32 _bgt::one
	ldsfld	int32 _bgt::odd
	bgt FAIL

	ldsfld	int32 _bgt::one
	ldsfld	int32 _bgt::even
	bgt _F
	br FAIL


_F:
	ldsfld	int32 _bgt::max
	ldsfld	int32 _bgt::min
	bgt G
	br FAIL

G:
	ldsfld	int32 _bgt::max
	ldsfld	int32 _bgt::_one
	bgt H
	br FAIL

H:
	ldsfld	int32 _bgt::max
	ldsfld	int32 _bgt::zero
	bgt I
	br FAIL

I:
	ldsfld	int32 _bgt::max
	ldsfld	int32 _bgt::one
	bgt J
	br FAIL

J:
	ldsfld	int32 _bgt::max
	ldsfld	int32 _bgt::max
	bgt FAIL

	ldsfld	int32 _bgt::max
	ldsfld	int32 _bgt::odd
	bgt K
	br FAIL

K:
	ldsfld	int32 _bgt::max
	ldsfld	int32 _bgt::even
	bgt L
	br FAIL

L:
	ldsfld	int32 _bgt::odd
	ldsfld	int32 _bgt::min
	bgt M
	br FAIL

M:
	ldsfld	int32 _bgt::odd
	ldsfld	int32 _bgt::_one
	bgt N
	br FAIL

N:
	ldsfld	int32 _bgt::odd
	ldsfld	int32 _bgt::zero
	bgt O
	br FAIL

O:
	ldsfld	int32 _bgt::odd
	ldsfld	int32 _bgt::one
	bgt P
	br FAIL

P:
	ldsfld	int32 _bgt::odd
	ldsfld	int32 _bgt::max
	bgt FAIL

	ldsfld	int32 _bgt::odd
	ldsfld	int32 _bgt::odd
	bgt FAIL

	ldsfld	int32 _bgt::odd
	ldsfld	int32 _bgt::even
	bgt Q
	br FAIL


Q:
	ldsfld	int32 _bgt::even
	ldsfld	int32 _bgt::min
	bgt R
	br FAIL

R:
	ldsfld	int32 _bgt::even
	ldsfld	int32 _bgt::_one
	bgt FAIL

	ldsfld	int32 _bgt::even
	ldsfld	int32 _bgt::zero
	bgt FAIL

	ldsfld	int32 _bgt::even
	ldsfld	int32 _bgt::one
	bgt FAIL

	ldsfld	int32 _bgt::even
	ldsfld	int32 _bgt::max
	bgt FAIL

	ldsfld	int32 _bgt::even
	ldsfld	int32 _bgt::odd
	bgt FAIL

	ldsfld	int32 _bgt::even
	ldsfld	int32 _bgt::even
	bgt FAIL

	br BACKCHECK
TOPASS:
	br PASS
BACKCHECK:
	ldc.i4 0x1
	ldc.i4 0x0
	bgt TOPASS
	br FAIL




PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly bgt_i4.exe{}
