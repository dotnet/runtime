// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public _bgt {

.field public static	float32 _inf
.field public static	float32 _min
.field public static	float32 _one
.field public static	float32 _zero
.field public static	float32 zero
.field public static	float32 one
.field public static	float32 max
.field public static	float32 inf
.field public static	float32 NaN

.method public static	void initialize() {
.maxstack	10
	ldc.r4		float32(0xFF800000)
	stsfld	float32 _bgt::_inf
	ldc.r4		float32(0xFF7FFFFF)
	stsfld	float32 _bgt::_min
	ldc.r4		float32(0xBF800000)
	stsfld	float32 _bgt::_one
	ldc.r4		float32(0x80000000)
	stsfld	float32 _bgt::_zero
	ldc.r4		float32(0x00000000)
	stsfld	float32 _bgt::zero
	ldc.r4		float32(0x3F800000)
	stsfld	float32 _bgt::one
	ldc.r4		float32(0x7F7FFFFF)
	stsfld	float32 _bgt::max
	ldc.r4		float32(0x7F800000)
	stsfld	float32 _bgt::inf
	ldc.r4		float32(0x7FC00000)
	stsfld	float32 _bgt::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void _bgt::initialize()

	ldsfld	float32 _bgt::_inf
	ldsfld	float32 _bgt::_inf
	bgt FAIL

	ldsfld	float32 _bgt::_inf
	ldsfld	float32 _bgt::_min
	bgt FAIL

	ldsfld	float32 _bgt::_inf
	ldsfld	float32 _bgt::_one
	bgt FAIL

	ldsfld	float32 _bgt::_inf
	ldsfld	float32 _bgt::_zero
	bgt FAIL

	ldsfld	float32 _bgt::_inf
	ldsfld	float32 _bgt::zero
	bgt FAIL

	ldsfld	float32 _bgt::_inf
	ldsfld	float32 _bgt::one
	bgt FAIL

	ldsfld	float32 _bgt::_inf
	ldsfld	float32 _bgt::max
	bgt FAIL

	ldsfld	float32 _bgt::_inf
	ldsfld	float32 _bgt::inf
	bgt FAIL

	ldsfld	float32 _bgt::_inf
	ldsfld	float32 _bgt::NaN
	bgt FAIL
	ldsfld	float32 _bgt::_min
	ldsfld	float32 _bgt::_inf
	bgt B
	br FAIL

B:
	ldsfld	float32 _bgt::_min
	ldsfld	float32 _bgt::_min
	bgt FAIL

	ldsfld	float32 _bgt::_min
	ldsfld	float32 _bgt::_one
	bgt FAIL

	ldsfld	float32 _bgt::_min
	ldsfld	float32 _bgt::_zero
	bgt FAIL

	ldsfld	float32 _bgt::_min
	ldsfld	float32 _bgt::zero
	bgt FAIL

	ldsfld	float32 _bgt::_min
	ldsfld	float32 _bgt::one
	bgt FAIL

	ldsfld	float32 _bgt::_min
	ldsfld	float32 _bgt::max
	bgt FAIL

	ldsfld	float32 _bgt::_min
	ldsfld	float32 _bgt::inf
	bgt FAIL

	ldsfld	float32 _bgt::_min
	ldsfld	float32 _bgt::NaN
	bgt FAIL


	ldsfld	float32 _bgt::_one
	ldsfld	float32 _bgt::_inf
	bgt D
	br FAIL

D:
	ldsfld	float32 _bgt::_one
	ldsfld	float32 _bgt::_min
	bgt E
	br FAIL

E:
	ldsfld	float32 _bgt::_one
	ldsfld	float32 _bgt::_one
	bgt FAIL

	ldsfld	float32 _bgt::_one
	ldsfld	float32 _bgt::_zero
	bgt FAIL

	ldsfld	float32 _bgt::_one
	ldsfld	float32 _bgt::zero
	bgt FAIL

	ldsfld	float32 _bgt::_one
	ldsfld	float32 _bgt::one
	bgt FAIL

	ldsfld	float32 _bgt::_one
	ldsfld	float32 _bgt::max
	bgt FAIL

	ldsfld	float32 _bgt::_one
	ldsfld	float32 _bgt::inf
	bgt FAIL

	ldsfld	float32 _bgt::_one
	ldsfld	float32 _bgt::NaN
	bgt FAIL
	ldsfld	float32 _bgt::_zero
	ldsfld	float32 _bgt::_inf
	bgt G
	br FAIL

G:
	ldsfld	float32 _bgt::_zero
	ldsfld	float32 _bgt::_min
	bgt H
	br FAIL

H:
	ldsfld	float32 _bgt::_zero
	ldsfld	float32 _bgt::_one
	bgt I
	br FAIL

I:
	ldsfld	float32 _bgt::_zero
	ldsfld	float32 _bgt::_zero
	bgt FAIL

	ldsfld	float32 _bgt::_zero
	ldsfld	float32 _bgt::zero
	bgt FAIL

	ldsfld	float32 _bgt::_zero
	ldsfld	float32 _bgt::one
	bgt FAIL

	ldsfld	float32 _bgt::_zero
	ldsfld	float32 _bgt::max
	bgt FAIL

	ldsfld	float32 _bgt::_zero
	ldsfld	float32 _bgt::inf
	bgt FAIL

	ldsfld	float32 _bgt::_zero
	ldsfld	float32 _bgt::NaN
	bgt FAIL

	ldsfld	float32 _bgt::zero
	ldsfld	float32 _bgt::_inf
	bgt K
	br FAIL

K:
	ldsfld	float32 _bgt::zero
	ldsfld	float32 _bgt::_min
	bgt L
	br FAIL

L:
	ldsfld	float32 _bgt::zero
	ldsfld	float32 _bgt::_one
	bgt M
	br FAIL

M:
	ldsfld	float32 _bgt::zero
	ldsfld	float32 _bgt::_zero
	bgt FAIL

	ldsfld	float32 _bgt::zero
	ldsfld	float32 _bgt::zero
	bgt FAIL

	ldsfld	float32 _bgt::zero
	ldsfld	float32 _bgt::one
	bgt FAIL

	ldsfld	float32 _bgt::zero
	ldsfld	float32 _bgt::max
	bgt FAIL

	ldsfld	float32 _bgt::zero
	ldsfld	float32 _bgt::inf
	bgt FAIL

	ldsfld	float32 _bgt::zero
	ldsfld	float32 _bgt::NaN
	bgt FAIL

	ldsfld	float32 _bgt::one
	ldsfld	float32 _bgt::_inf
	bgt O
	br FAIL

O:
	ldsfld	float32 _bgt::one
	ldsfld	float32 _bgt::_min
	bgt P
	br FAIL

P:
	ldsfld	float32 _bgt::one
	ldsfld	float32 _bgt::_one
	bgt Q
	br FAIL

Q:
	ldsfld	float32 _bgt::one
	ldsfld	float32 _bgt::_zero
	bgt R
	br FAIL

R:
	ldsfld	float32 _bgt::one
	ldsfld	float32 _bgt::zero
	bgt S
	br FAIL

S:
	ldsfld	float32 _bgt::one
	ldsfld	float32 _bgt::one
	bgt FAIL

	ldsfld	float32 _bgt::one
	ldsfld	float32 _bgt::max
	bgt FAIL

	ldsfld	float32 _bgt::one
	ldsfld	float32 _bgt::inf
	bgt FAIL

	ldsfld	float32 _bgt::one
	ldsfld	float32 _bgt::NaN
	bgt FAIL

	ldsfld	float32 _bgt::max
	ldsfld	float32 _bgt::_inf
	bgt U
	br FAIL

U:
	ldsfld	float32 _bgt::max
	ldsfld	float32 _bgt::_min
	bgt V
	br FAIL

V:
	ldsfld	float32 _bgt::max
	ldsfld	float32 _bgt::_one
	bgt W
	br FAIL

W:
	ldsfld	float32 _bgt::max
	ldsfld	float32 _bgt::_zero
	bgt X
	br FAIL

X:
	ldsfld	float32 _bgt::max
	ldsfld	float32 _bgt::zero
	bgt Y
	br FAIL

Y:
	ldsfld	float32 _bgt::max
	ldsfld	float32 _bgt::one
	bgt Z
	br FAIL

Z:
	ldsfld	float32 _bgt::max
	ldsfld	float32 _bgt::max
	bgt FAIL

	ldsfld	float32 _bgt::max
	ldsfld	float32 _bgt::inf
	bgt FAIL

	ldsfld	float32 _bgt::max
	ldsfld	float32 _bgt::NaN
	bgt FAIL

	ldsfld	float32 _bgt::inf
	ldsfld	float32 _bgt::_inf
	bgt BB
	br FAIL

BB:
	ldsfld	float32 _bgt::inf
	ldsfld	float32 _bgt::_min
	bgt CC
	br FAIL

CC:
	ldsfld	float32 _bgt::inf
	ldsfld	float32 _bgt::_one
	bgt DD
	br FAIL

DD:
	ldsfld	float32 _bgt::inf
	ldsfld	float32 _bgt::_zero
	bgt EE
	br FAIL

EE:
	ldsfld	float32 _bgt::inf
	ldsfld	float32 _bgt::zero
	bgt FF
	br FAIL

FF:
	ldsfld	float32 _bgt::inf
	ldsfld	float32 _bgt::one
	bgt GG
	br FAIL

GG:
	ldsfld	float32 _bgt::inf
	ldsfld	float32 _bgt::max
	bgt HH
	br FAIL

HH:
	ldsfld	float32 _bgt::inf
	ldsfld	float32 _bgt::inf
	bgt FAIL

	ldsfld	float32 _bgt::inf
	ldsfld	float32 _bgt::NaN
	bgt FAIL

	ldsfld	float32 _bgt::NaN
	ldsfld	float32 _bgt::_inf
	bgt FAIL

	ldsfld	float32 _bgt::NaN
	ldsfld	float32 _bgt::_min
	bgt FAIL

	ldsfld	float32 _bgt::NaN
	ldsfld	float32 _bgt::_one
	bgt FAIL

	ldsfld	float32 _bgt::NaN
	ldsfld	float32 _bgt::_zero
	bgt FAIL

	ldsfld	float32 _bgt::NaN
	ldsfld	float32 _bgt::zero
	bgt FAIL

	ldsfld	float32 _bgt::NaN
	ldsfld	float32 _bgt::one
	bgt FAIL

	ldsfld	float32 _bgt::NaN
	ldsfld	float32 _bgt::max
	bgt FAIL

	ldsfld	float32 _bgt::NaN
	ldsfld	float32 _bgt::inf
	bgt FAIL

	ldsfld	float32 _bgt::NaN
	ldsfld	float32 _bgt::NaN
	bgt  FAIL
	br BACKCHECK

TOPASS:
	br PASS
BACKCHECK:
	ldc.r4 0x1
	ldc.r4 0x0
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
.assembly bgt_r4{}
