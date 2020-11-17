// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public _bgt {

.field public static	float64 _inf
.field public static	float64 _min
.field public static	float64 _one
.field public static	float64 _zero
.field public static	float64 zero
.field public static	float64 one
.field public static	float64 max
.field public static	float64 inf
.field public static	float64 NaN

.method public static	void initialize() {
.maxstack	10
	ldc.r8		float64(0xFFF0000000000000)
	stsfld	float64 _bgt::_inf
	ldc.r8		float64(0xFF7FFFFFFFFFFFFF)
	stsfld	float64 _bgt::_min
	ldc.r8		float64(0xBFF0000000000000)
	stsfld	float64 _bgt::_one
	ldc.r8		float64(0x8000000000000000)
	stsfld	float64 _bgt::_zero
	ldc.r8		float64(0x0000000000000000)
	stsfld	float64 _bgt::zero
	ldc.r8		float64(0x3FF0000000000000)
	stsfld	float64 _bgt::one
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	stsfld	float64 _bgt::max
	ldc.r8		float64(0x7FF0000000000000)
	stsfld	float64 _bgt::inf
	ldc.r8		float64(0x7FF8000000000000)
	stsfld	float64 _bgt::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void _bgt::initialize()

	ldsfld	float64 _bgt::_inf
	ldsfld	float64 _bgt::_inf
	bgt FAIL

	ldsfld	float64 _bgt::_inf
	ldsfld	float64 _bgt::_min
	bgt FAIL

	ldsfld	float64 _bgt::_inf
	ldsfld	float64 _bgt::_one
	bgt FAIL

	ldsfld	float64 _bgt::_inf
	ldsfld	float64 _bgt::_zero
	bgt FAIL

	ldsfld	float64 _bgt::_inf
	ldsfld	float64 _bgt::zero
	bgt FAIL

	ldsfld	float64 _bgt::_inf
	ldsfld	float64 _bgt::one
	bgt FAIL

	ldsfld	float64 _bgt::_inf
	ldsfld	float64 _bgt::max
	bgt FAIL

	ldsfld	float64 _bgt::_inf
	ldsfld	float64 _bgt::inf
	bgt FAIL

	ldsfld	float64 _bgt::_inf
	ldsfld	float64 _bgt::NaN
	bgt FAIL
	ldsfld	float64 _bgt::_min
	ldsfld	float64 _bgt::_inf
	bgt B
	br FAIL

B:
	ldsfld	float64 _bgt::_min
	ldsfld	float64 _bgt::_min
	bgt FAIL

	ldsfld	float64 _bgt::_min
	ldsfld	float64 _bgt::_one
	bgt FAIL

	ldsfld	float64 _bgt::_min
	ldsfld	float64 _bgt::_zero
	bgt FAIL

	ldsfld	float64 _bgt::_min
	ldsfld	float64 _bgt::zero
	bgt FAIL

	ldsfld	float64 _bgt::_min
	ldsfld	float64 _bgt::one
	bgt FAIL

	ldsfld	float64 _bgt::_min
	ldsfld	float64 _bgt::max
	bgt FAIL

	ldsfld	float64 _bgt::_min
	ldsfld	float64 _bgt::inf
	bgt FAIL

	ldsfld	float64 _bgt::_min
	ldsfld	float64 _bgt::NaN
	bgt FAIL


	ldsfld	float64 _bgt::_one
	ldsfld	float64 _bgt::_inf
	bgt D
	br FAIL

D:
	ldsfld	float64 _bgt::_one
	ldsfld	float64 _bgt::_min
	bgt E
	br FAIL

E:
	ldsfld	float64 _bgt::_one
	ldsfld	float64 _bgt::_one
	bgt FAIL

	ldsfld	float64 _bgt::_one
	ldsfld	float64 _bgt::_zero
	bgt FAIL

	ldsfld	float64 _bgt::_one
	ldsfld	float64 _bgt::zero
	bgt FAIL

	ldsfld	float64 _bgt::_one
	ldsfld	float64 _bgt::one
	bgt FAIL

	ldsfld	float64 _bgt::_one
	ldsfld	float64 _bgt::max
	bgt FAIL

	ldsfld	float64 _bgt::_one
	ldsfld	float64 _bgt::inf
	bgt FAIL

	ldsfld	float64 _bgt::_one
	ldsfld	float64 _bgt::NaN
	bgt FAIL
	ldsfld	float64 _bgt::_zero
	ldsfld	float64 _bgt::_inf
	bgt G
	br FAIL

G:
	ldsfld	float64 _bgt::_zero
	ldsfld	float64 _bgt::_min
	bgt H
	br FAIL

H:
	ldsfld	float64 _bgt::_zero
	ldsfld	float64 _bgt::_one
	bgt I
	br FAIL

I:
	ldsfld	float64 _bgt::_zero
	ldsfld	float64 _bgt::_zero
	bgt FAIL

	ldsfld	float64 _bgt::_zero
	ldsfld	float64 _bgt::zero
	bgt FAIL

	ldsfld	float64 _bgt::_zero
	ldsfld	float64 _bgt::one
	bgt FAIL

	ldsfld	float64 _bgt::_zero
	ldsfld	float64 _bgt::max
	bgt FAIL

	ldsfld	float64 _bgt::_zero
	ldsfld	float64 _bgt::inf
	bgt FAIL

	ldsfld	float64 _bgt::_zero
	ldsfld	float64 _bgt::NaN
	bgt FAIL

	ldsfld	float64 _bgt::zero
	ldsfld	float64 _bgt::_inf
	bgt K
	br FAIL

K:
	ldsfld	float64 _bgt::zero
	ldsfld	float64 _bgt::_min
	bgt L
	br FAIL

L:
	ldsfld	float64 _bgt::zero
	ldsfld	float64 _bgt::_one
	bgt M
	br FAIL

M:
	ldsfld	float64 _bgt::zero
	ldsfld	float64 _bgt::_zero
	bgt FAIL

	ldsfld	float64 _bgt::zero
	ldsfld	float64 _bgt::zero
	bgt FAIL

	ldsfld	float64 _bgt::zero
	ldsfld	float64 _bgt::one
	bgt FAIL

	ldsfld	float64 _bgt::zero
	ldsfld	float64 _bgt::max
	bgt FAIL

	ldsfld	float64 _bgt::zero
	ldsfld	float64 _bgt::inf
	bgt FAIL

	ldsfld	float64 _bgt::zero
	ldsfld	float64 _bgt::NaN
	bgt FAIL

	ldsfld	float64 _bgt::one
	ldsfld	float64 _bgt::_inf
	bgt O
	br FAIL

O:
	ldsfld	float64 _bgt::one
	ldsfld	float64 _bgt::_min
	bgt P
	br FAIL

P:
	ldsfld	float64 _bgt::one
	ldsfld	float64 _bgt::_one
	bgt Q
	br FAIL

Q:
	ldsfld	float64 _bgt::one
	ldsfld	float64 _bgt::_zero
	bgt R
	br FAIL

R:
	ldsfld	float64 _bgt::one
	ldsfld	float64 _bgt::zero
	bgt S
	br FAIL

S:
	ldsfld	float64 _bgt::one
	ldsfld	float64 _bgt::one
	bgt FAIL

	ldsfld	float64 _bgt::one
	ldsfld	float64 _bgt::max
	bgt FAIL

	ldsfld	float64 _bgt::one
	ldsfld	float64 _bgt::inf
	bgt FAIL

	ldsfld	float64 _bgt::one
	ldsfld	float64 _bgt::NaN
	bgt FAIL

	ldsfld	float64 _bgt::max
	ldsfld	float64 _bgt::_inf
	bgt U
	br FAIL

U:
	ldsfld	float64 _bgt::max
	ldsfld	float64 _bgt::_min
	bgt V
	br FAIL

V:
	ldsfld	float64 _bgt::max
	ldsfld	float64 _bgt::_one
	bgt W
	br FAIL

W:
	ldsfld	float64 _bgt::max
	ldsfld	float64 _bgt::_zero
	bgt X
	br FAIL

X:
	ldsfld	float64 _bgt::max
	ldsfld	float64 _bgt::zero
	bgt Y
	br FAIL

Y:
	ldsfld	float64 _bgt::max
	ldsfld	float64 _bgt::one
	bgt Z
	br FAIL

Z:
	ldsfld	float64 _bgt::max
	ldsfld	float64 _bgt::max
	bgt FAIL

	ldsfld	float64 _bgt::max
	ldsfld	float64 _bgt::inf
	bgt FAIL

	ldsfld	float64 _bgt::max
	ldsfld	float64 _bgt::NaN
	bgt FAIL

	ldsfld	float64 _bgt::inf
	ldsfld	float64 _bgt::_inf
	bgt BB
	br FAIL

BB:
	ldsfld	float64 _bgt::inf
	ldsfld	float64 _bgt::_min
	bgt CC
	br FAIL

CC:
	ldsfld	float64 _bgt::inf
	ldsfld	float64 _bgt::_one
	bgt DD
	br FAIL

DD:
	ldsfld	float64 _bgt::inf
	ldsfld	float64 _bgt::_zero
	bgt EE
	br FAIL

EE:
	ldsfld	float64 _bgt::inf
	ldsfld	float64 _bgt::zero
	bgt FF
	br FAIL

FF:
	ldsfld	float64 _bgt::inf
	ldsfld	float64 _bgt::one
	bgt GG
	br FAIL

GG:
	ldsfld	float64 _bgt::inf
	ldsfld	float64 _bgt::max
	bgt HH
	br FAIL

HH:
	ldsfld	float64 _bgt::inf
	ldsfld	float64 _bgt::inf
	bgt FAIL

	ldsfld	float64 _bgt::inf
	ldsfld	float64 _bgt::NaN
	bgt FAIL

	ldsfld	float64 _bgt::NaN
	ldsfld	float64 _bgt::_inf
	bgt FAIL

	ldsfld	float64 _bgt::NaN
	ldsfld	float64 _bgt::_min
	bgt FAIL

	ldsfld	float64 _bgt::NaN
	ldsfld	float64 _bgt::_one
	bgt FAIL

	ldsfld	float64 _bgt::NaN
	ldsfld	float64 _bgt::_zero
	bgt FAIL

	ldsfld	float64 _bgt::NaN
	ldsfld	float64 _bgt::zero
	bgt FAIL

	ldsfld	float64 _bgt::NaN
	ldsfld	float64 _bgt::one
	bgt FAIL

	ldsfld	float64 _bgt::NaN
	ldsfld	float64 _bgt::max
	bgt FAIL

	ldsfld	float64 _bgt::NaN
	ldsfld	float64 _bgt::inf
	bgt FAIL

	ldsfld	float64 _bgt::NaN
	ldsfld	float64 _bgt::NaN
	bgt  FAIL
	br BACKCHECK

TOPASS:
	br PASS
BACKCHECK:
	ldc.r8 0x1
	ldc.r8 0x0
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
.assembly bgt_r8.exe{}
