// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}


.class public bgt_un {

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
	stsfld	float32 bgt_un::_inf
	ldc.r4		float32(0xFF7FFFFF)
	stsfld	float32 bgt_un::_min
	ldc.r4		float32(0xBF800000)
	stsfld	float32 bgt_un::_one
	ldc.r4		float32(0x80000000)
	stsfld	float32 bgt_un::_zero
	ldc.r4		float32(0x00000000)
	stsfld	float32 bgt_un::zero
	ldc.r4		float32(0x3F800000)
	stsfld	float32 bgt_un::one
	ldc.r4		float32(0x7F7FFFFF)
	stsfld	float32 bgt_un::max
	ldc.r4		float32(0x7F800000)
	stsfld	float32 bgt_un::inf
	ldc.r4		float32(0x7FC00000)
	stsfld	float32 bgt_un::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void bgt_un::initialize()

	ldsfld	float32 bgt_un::_inf
	ldsfld	float32 bgt_un::_inf
	bgt.un FAIL

	ldsfld	float32 bgt_un::_inf
	ldsfld	float32 bgt_un::_min
	bgt.un FAIL

	ldsfld	float32 bgt_un::_inf
	ldsfld	float32 bgt_un::_one
	bgt.un FAIL

	ldsfld	float32 bgt_un::_inf
	ldsfld	float32 bgt_un::_zero
	bgt.un FAIL

	ldsfld	float32 bgt_un::_inf
	ldsfld	float32 bgt_un::zero
	bgt.un FAIL

	ldsfld	float32 bgt_un::_inf
	ldsfld	float32 bgt_un::one
	bgt.un FAIL

	ldsfld	float32 bgt_un::_inf
	ldsfld	float32 bgt_un::max
	bgt.un FAIL

	ldsfld	float32 bgt_un::_inf
	ldsfld	float32 bgt_un::inf
	bgt.un FAIL

	ldsfld	float32 bgt_un::_inf
	ldsfld	float32 bgt_un::NaN
	bgt.un A
	br FAIL
A:
	ldsfld	float32 bgt_un::_min
	ldsfld	float32 bgt_un::_inf
	bgt.un B
	br FAIL

B:
	ldsfld	float32 bgt_un::_min
	ldsfld	float32 bgt_un::_min
	bgt.un FAIL

	ldsfld	float32 bgt_un::_min
	ldsfld	float32 bgt_un::_one
	bgt.un FAIL

	ldsfld	float32 bgt_un::_min
	ldsfld	float32 bgt_un::_zero
	bgt.un FAIL

	ldsfld	float32 bgt_un::_min
	ldsfld	float32 bgt_un::zero
	bgt.un FAIL

	ldsfld	float32 bgt_un::_min
	ldsfld	float32 bgt_un::one
	bgt.un FAIL

	ldsfld	float32 bgt_un::_min
	ldsfld	float32 bgt_un::max
	bgt.un FAIL

	ldsfld	float32 bgt_un::_min
	ldsfld	float32 bgt_un::inf
	bgt.un FAIL

	ldsfld	float32 bgt_un::_min
	ldsfld	float32 bgt_un::NaN
	bgt.un C
	br FAIL


C:
	ldsfld	float32 bgt_un::_one
	ldsfld	float32 bgt_un::_inf
	bgt.un D
	br FAIL

D:
	ldsfld	float32 bgt_un::_one
	ldsfld	float32 bgt_un::_min
	bgt.un E
	br FAIL

E:
	ldsfld	float32 bgt_un::_one
	ldsfld	float32 bgt_un::_one
	bgt.un FAIL

	ldsfld	float32 bgt_un::_one
	ldsfld	float32 bgt_un::_zero
	bgt.un FAIL

	ldsfld	float32 bgt_un::_one
	ldsfld	float32 bgt_un::zero
	bgt.un FAIL

	ldsfld	float32 bgt_un::_one
	ldsfld	float32 bgt_un::one
	bgt.un FAIL

	ldsfld	float32 bgt_un::_one
	ldsfld	float32 bgt_un::max
	bgt.un FAIL

	ldsfld	float32 bgt_un::_one
	ldsfld	float32 bgt_un::inf
	bgt.un FAIL

	ldsfld	float32 bgt_un::_one
	ldsfld	float32 bgt_un::NaN
	bgt.un F
	br FAIL
F:
	ldsfld	float32 bgt_un::_zero
	ldsfld	float32 bgt_un::_inf
	bgt.un G
	br FAIL

G:
	ldsfld	float32 bgt_un::_zero
	ldsfld	float32 bgt_un::_min
	bgt.un H
	br FAIL

H:
	ldsfld	float32 bgt_un::_zero
	ldsfld	float32 bgt_un::_one
	bgt.un I
	br FAIL

I:
	ldsfld	float32 bgt_un::_zero
	ldsfld	float32 bgt_un::_zero
	bgt.un FAIL

	ldsfld	float32 bgt_un::_zero
	ldsfld	float32 bgt_un::zero
	bgt.un FAIL

	ldsfld	float32 bgt_un::_zero
	ldsfld	float32 bgt_un::one
	bgt.un FAIL

	ldsfld	float32 bgt_un::_zero
	ldsfld	float32 bgt_un::max
	bgt.un FAIL

	ldsfld	float32 bgt_un::_zero
	ldsfld	float32 bgt_un::inf
	bgt.un FAIL

	ldsfld	float32 bgt_un::_zero
	ldsfld	float32 bgt_un::NaN
	bgt.un J
	br FAIL

J:
	ldsfld	float32 bgt_un::zero
	ldsfld	float32 bgt_un::_inf
	bgt.un K
	br FAIL

K:
	ldsfld	float32 bgt_un::zero
	ldsfld	float32 bgt_un::_min
	bgt.un L
	br FAIL

L:
	ldsfld	float32 bgt_un::zero
	ldsfld	float32 bgt_un::_one
	bgt.un M
	br FAIL

M:
	ldsfld	float32 bgt_un::zero
	ldsfld	float32 bgt_un::_zero
	bgt.un FAIL

	ldsfld	float32 bgt_un::zero
	ldsfld	float32 bgt_un::zero
	bgt.un FAIL

	ldsfld	float32 bgt_un::zero
	ldsfld	float32 bgt_un::one
	bgt.un FAIL

	ldsfld	float32 bgt_un::zero
	ldsfld	float32 bgt_un::max
	bgt.un FAIL

	ldsfld	float32 bgt_un::zero
	ldsfld	float32 bgt_un::inf
	bgt.un FAIL

	ldsfld	float32 bgt_un::zero
	ldsfld	float32 bgt_un::NaN
	bgt.un N
	br FAIL

N:
	ldsfld	float32 bgt_un::one
	ldsfld	float32 bgt_un::_inf
	bgt.un O
	br FAIL

O:
	ldsfld	float32 bgt_un::one
	ldsfld	float32 bgt_un::_min
	bgt.un P
	br FAIL

P:
	ldsfld	float32 bgt_un::one
	ldsfld	float32 bgt_un::_one
	bgt.un Q
	br FAIL

Q:
	ldsfld	float32 bgt_un::one
	ldsfld	float32 bgt_un::_zero
	bgt.un R
	br FAIL

R:
	ldsfld	float32 bgt_un::one
	ldsfld	float32 bgt_un::zero
	bgt.un S
	br FAIL

S:
	ldsfld	float32 bgt_un::one
	ldsfld	float32 bgt_un::one
	bgt.un FAIL

	ldsfld	float32 bgt_un::one
	ldsfld	float32 bgt_un::max
	bgt.un FAIL

	ldsfld	float32 bgt_un::one
	ldsfld	float32 bgt_un::inf
	bgt.un FAIL

	ldsfld	float32 bgt_un::one
	ldsfld	float32 bgt_un::NaN
	bgt.un T
	br FAIL

T:
	ldsfld	float32 bgt_un::max
	ldsfld	float32 bgt_un::_inf
	bgt.un U
	br FAIL

U:
	ldsfld	float32 bgt_un::max
	ldsfld	float32 bgt_un::_min
	bgt.un V
	br FAIL

V:
	ldsfld	float32 bgt_un::max
	ldsfld	float32 bgt_un::_one
	bgt.un W
	br FAIL

W:
	ldsfld	float32 bgt_un::max
	ldsfld	float32 bgt_un::_zero
	bgt.un X
	br FAIL

X:
	ldsfld	float32 bgt_un::max
	ldsfld	float32 bgt_un::zero
	bgt.un Y
	br FAIL

Y:
	ldsfld	float32 bgt_un::max
	ldsfld	float32 bgt_un::one
	bgt.un Z
	br FAIL

Z:
	ldsfld	float32 bgt_un::max
	ldsfld	float32 bgt_un::max
	bgt.un FAIL

	ldsfld	float32 bgt_un::max
	ldsfld	float32 bgt_un::inf
	bgt.un FAIL

	ldsfld	float32 bgt_un::max
	ldsfld	float32 bgt_un::NaN
	bgt.un AA
	br FAIL

AA:
	ldsfld	float32 bgt_un::inf
	ldsfld	float32 bgt_un::_inf
	bgt.un BB
	br FAIL

BB:
	ldsfld	float32 bgt_un::inf
	ldsfld	float32 bgt_un::_min
	bgt.un CC
	br FAIL

CC:
	ldsfld	float32 bgt_un::inf
	ldsfld	float32 bgt_un::_one
	bgt.un DD
	br FAIL

DD:
	ldsfld	float32 bgt_un::inf
	ldsfld	float32 bgt_un::_zero
	bgt.un EE
	br FAIL

EE:
	ldsfld	float32 bgt_un::inf
	ldsfld	float32 bgt_un::zero
	bgt.un FF
	br FAIL

FF:
	ldsfld	float32 bgt_un::inf
	ldsfld	float32 bgt_un::one
	bgt.un GG
	br FAIL

GG:
	ldsfld	float32 bgt_un::inf
	ldsfld	float32 bgt_un::max
	bgt.un HH
	br FAIL

HH:
	ldsfld	float32 bgt_un::inf
	ldsfld	float32 bgt_un::inf
	bgt.un FAIL

	ldsfld	float32 bgt_un::inf
	ldsfld	float32 bgt_un::NaN
	bgt.un II
	br FAIL

II:
	ldsfld	float32 bgt_un::NaN
	ldsfld	float32 bgt_un::_inf
	bgt.un JJ
	br FAIL

JJ:
	ldsfld	float32 bgt_un::NaN
	ldsfld	float32 bgt_un::_min
	bgt.un KK
	br FAIL

KK:
	ldsfld	float32 bgt_un::NaN
	ldsfld	float32 bgt_un::_one
	bgt.un LL
	br FAIL

LL:
	ldsfld	float32 bgt_un::NaN
	ldsfld	float32 bgt_un::_zero
	bgt.un MM
	br FAIL

MM:
	ldsfld	float32 bgt_un::NaN
	ldsfld	float32 bgt_un::zero
	bgt.un NN
	br FAIL

NN:
	ldsfld	float32 bgt_un::NaN
	ldsfld	float32 bgt_un::one
	bgt.un OO
	br FAIL

OO:
	ldsfld	float32 bgt_un::NaN
	ldsfld	float32 bgt_un::max
	bgt.un PP
	br FAIL

PP:
	ldsfld	float32 bgt_un::NaN
	ldsfld	float32 bgt_un::inf
	bgt.un QQ
	br FAIL

QQ:
	ldsfld	float32 bgt_un::NaN
	ldsfld	float32 bgt_un::NaN
	bgt.un  BACKCHECK
	br FAIL

TOPASS:
	br PASS
BACKCHECK:
	ldc.r4 0x1
	ldc.r4 0x0
	bgt.un TOPASS
	br FAIL


PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly bgt_un_r4.exe{}
