// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}


.class public _blt_un {

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
	stsfld	float32 _blt_un::_inf
	ldc.r4		float32(0xFF7FFFFF)
	stsfld	float32 _blt_un::_min
	ldc.r4		float32(0xBF800000)
	stsfld	float32 _blt_un::_one
	ldc.r4		float32(0x80000000)
	stsfld	float32 _blt_un::_zero
	ldc.r4		float32(0x00000000)
	stsfld	float32 _blt_un::zero
	ldc.r4		float32(0x3F800000)
	stsfld	float32 _blt_un::one
	ldc.r4		float32(0x7F7FFFFF)
	stsfld	float32 _blt_un::max
	ldc.r4		float32(0x7F800000)
	stsfld	float32 _blt_un::inf
	ldc.r4		float32(0x7FC00000)
	stsfld	float32 _blt_un::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void _blt_un::initialize()

	ldsfld	float32 _blt_un::_inf
	ldsfld	float32 _blt_un::_inf
	blt.un FAIL

A:
	ldsfld	float32 _blt_un::_inf
	ldsfld	float32 _blt_un::_min
	blt.un B
	br FAIL

B:
	ldsfld	float32 _blt_un::_inf
	ldsfld	float32 _blt_un::_one
	blt.un C
	br FAIL

C:
	ldsfld	float32 _blt_un::_inf
	ldsfld	float32 _blt_un::_zero
	blt.un D
	br FAIL

D:
	ldsfld	float32 _blt_un::_inf
	ldsfld	float32 _blt_un::zero
	blt.un E
	br FAIL

E:
	ldsfld	float32 _blt_un::_inf
	ldsfld	float32 _blt_un::one
	blt.un F
	br FAIL

F:
	ldsfld	float32 _blt_un::_inf
	ldsfld	float32 _blt_un::max
	blt.un G
	br FAIL

G:
	ldsfld	float32 _blt_un::_inf
	ldsfld	float32 _blt_un::inf
	blt.un H
	br FAIL

H:
	ldsfld	float32 _blt_un::_inf
	ldsfld	float32 _blt_un::NaN
	blt.un K
	br FAIL
K:
	ldsfld	float32 _blt_un::_min
	ldsfld	float32 _blt_un::_inf
	blt.un FAIL

	ldsfld	float32 _blt_un::_min
	ldsfld	float32 _blt_un::_min
	blt.un FAIL

L:
	ldsfld	float32 _blt_un::_min
	ldsfld	float32 _blt_un::_one
	blt.un M
	br FAIL

M:
	ldsfld	float32 _blt_un::_min
	ldsfld	float32 _blt_un::_zero
	blt.un N
	br FAIL

N:
	ldsfld	float32 _blt_un::_min
	ldsfld	float32 _blt_un::zero
	blt.un O
	br FAIL

O:
	ldsfld	float32 _blt_un::_min
	ldsfld	float32 _blt_un::one
	blt.un P
	br FAIL

P:
	ldsfld	float32 _blt_un::_min
	ldsfld	float32 _blt_un::max
	blt.un Q
	br FAIL

Q:
	ldsfld	float32 _blt_un::_min
	ldsfld	float32 _blt_un::inf
	blt.un R
	br FAIL

R:
	ldsfld	float32 _blt_un::_min
	ldsfld	float32 _blt_un::NaN
	blt.un S
	br FAIL


S:
	ldsfld	float32 _blt_un::_one
	ldsfld	float32 _blt_un::_inf
	blt.un FAIL

	ldsfld	float32 _blt_un::_one
	ldsfld	float32 _blt_un::_min
	blt.un FAIL

	ldsfld	float32 _blt_un::_one
	ldsfld	float32 _blt_un::_one
	blt.un FAIL

T:
	ldsfld	float32 _blt_un::_one
	ldsfld	float32 _blt_un::_zero
	blt.un U
	br FAIL

U:
	ldsfld	float32 _blt_un::_one
	ldsfld	float32 _blt_un::zero
	blt.un V
	br FAIL

V:
	ldsfld	float32 _blt_un::_one
	ldsfld	float32 _blt_un::one
	blt.un W
	br FAIL

W:
	ldsfld	float32 _blt_un::_one
	ldsfld	float32 _blt_un::max
	blt.un X
	br FAIL

X:
	ldsfld	float32 _blt_un::_one
	ldsfld	float32 _blt_un::inf
	blt.un Y
	br FAIL

Y:
	ldsfld	float32 _blt_un::_one
	ldsfld	float32 _blt_un::NaN
	blt.un Z
	br FAIL
Z:
	ldsfld	float32 _blt_un::_zero
	ldsfld	float32 _blt_un::_inf
	blt.un FAIL

	ldsfld	float32 _blt_un::_zero
	ldsfld	float32 _blt_un::_min
	blt.un FAIL

	ldsfld	float32 _blt_un::_zero
	ldsfld	float32 _blt_un::_one
	blt.un FAIL

	ldsfld	float32 _blt_un::_zero
	ldsfld	float32 _blt_un::_zero
	blt.un FAIL

AA:
	ldsfld	float32 _blt_un::_zero
	ldsfld	float32 _blt_un::zero
	blt.un FAIL

BB:
	ldsfld	float32 _blt_un::_zero
	ldsfld	float32 _blt_un::one
	blt.un CC
	br FAIL

CC:
	ldsfld	float32 _blt_un::_zero
	ldsfld	float32 _blt_un::max
	blt.un DD
	br FAIL

DD:
	ldsfld	float32 _blt_un::_zero
	ldsfld	float32 _blt_un::inf
	blt.un EE
	br FAIL

EE:
	ldsfld	float32 _blt_un::_zero
	ldsfld	float32 _blt_un::NaN
	blt.un FF
	br FAIL

FF:
	ldsfld	float32 _blt_un::zero
	ldsfld	float32 _blt_un::_inf
	blt.un FAIL

	ldsfld	float32 _blt_un::zero
	ldsfld	float32 _blt_un::_min
	blt.un FAIL

	ldsfld	float32 _blt_un::zero
	ldsfld	float32 _blt_un::_one
	blt.un FAIL

	ldsfld	float32 _blt_un::zero
	ldsfld	float32 _blt_un::_zero
	blt.un FAIL

GG:
	ldsfld	float32 _blt_un::zero
	ldsfld	float32 _blt_un::zero
	blt.un FAIL

HH:
	ldsfld	float32 _blt_un::zero
	ldsfld	float32 _blt_un::one
	blt.un II
	br FAIL

II:
	ldsfld	float32 _blt_un::zero
	ldsfld	float32 _blt_un::max
	blt.un JJ
	br FAIL

JJ:
	ldsfld	float32 _blt_un::zero
	ldsfld	float32 _blt_un::inf
	blt.un KK
	br FAIL

KK:
	ldsfld	float32 _blt_un::zero
	ldsfld	float32 _blt_un::NaN
	blt.un LL
	br FAIL

LL:
	ldsfld	float32 _blt_un::one
	ldsfld	float32 _blt_un::_inf
	blt.un FAIL

	ldsfld	float32 _blt_un::one
	ldsfld	float32 _blt_un::_min
	blt.un FAIL

	ldsfld	float32 _blt_un::one
	ldsfld	float32 _blt_un::_one
	blt.un FAIL

	ldsfld	float32 _blt_un::one
	ldsfld	float32 _blt_un::_zero
	blt.un FAIL

	ldsfld	float32 _blt_un::one
	ldsfld	float32 _blt_un::zero
	blt.un FAIL

	ldsfld	float32 _blt_un::one
	ldsfld	float32 _blt_un::one
	blt.un FAIL

MM:
	ldsfld	float32 _blt_un::one
	ldsfld	float32 _blt_un::max
	blt.un NN
	br FAIL

NN:
	ldsfld	float32 _blt_un::one
	ldsfld	float32 _blt_un::inf
	blt.un OO
	br FAIL

OO:
	ldsfld	float32 _blt_un::one
	ldsfld	float32 _blt_un::NaN
	blt.un PP
	br FAIL

PP:
	ldsfld	float32 _blt_un::max
	ldsfld	float32 _blt_un::_inf
	blt.un FAIL

	ldsfld	float32 _blt_un::max
	ldsfld	float32 _blt_un::_min
	blt.un FAIL

	ldsfld	float32 _blt_un::max
	ldsfld	float32 _blt_un::_one
	blt.un FAIL

	ldsfld	float32 _blt_un::max
	ldsfld	float32 _blt_un::_zero
	blt.un FAIL

	ldsfld	float32 _blt_un::max
	ldsfld	float32 _blt_un::zero
	blt.un FAIL

	ldsfld	float32 _blt_un::max
	ldsfld	float32 _blt_un::one
	blt.un FAIL

	ldsfld	float32 _blt_un::max
	ldsfld	float32 _blt_un::max
	blt.un FAIL

QQ:
	ldsfld	float32 _blt_un::max
	ldsfld	float32 _blt_un::inf
	blt.un RR
	br FAIL

RR:
	ldsfld	float32 _blt_un::max
	ldsfld	float32 _blt_un::NaN
	blt.un SS
	br FAIL

SS:
	ldsfld	float32 _blt_un::inf
	ldsfld	float32 _blt_un::_inf
	blt.un FAIL

	ldsfld	float32 _blt_un::inf
	ldsfld	float32 _blt_un::_min
	blt.un FAIL

	ldsfld	float32 _blt_un::inf
	ldsfld	float32 _blt_un::_one
	blt.un FAIL

	ldsfld	float32 _blt_un::inf
	ldsfld	float32 _blt_un::_zero
	blt.un FAIL

	ldsfld	float32 _blt_un::inf
	ldsfld	float32 _blt_un::zero
	blt.un FAIL

	ldsfld	float32 _blt_un::inf
	ldsfld	float32 _blt_un::one
	blt.un FAIL

	ldsfld	float32 _blt_un::inf
	ldsfld	float32 _blt_un::max
	blt.un FAIL

	ldsfld	float32 _blt_un::inf
	ldsfld	float32 _blt_un::inf
	blt.un FAIL

TT:
	ldsfld	float32 _blt_un::inf
	ldsfld	float32 _blt_un::NaN
	blt.un UU
	br FAIL

UU:
	ldsfld	float32 _blt_un::NaN
	ldsfld	float32 _blt_un::_inf
	blt.un VV
	br FAIL

VV:
	ldsfld	float32 _blt_un::NaN
	ldsfld	float32 _blt_un::_min
	blt.un WW
	br FAIL

WW:
	ldsfld	float32 _blt_un::NaN
	ldsfld	float32 _blt_un::_one
	blt.un XX
	br FAIL

XX:
	ldsfld	float32 _blt_un::NaN
	ldsfld	float32 _blt_un::_zero
	blt.un YY
	br FAIL

YY:
	ldsfld	float32 _blt_un::NaN
	ldsfld	float32 _blt_un::zero
	blt.un ZZ
	br FAIL

ZZ:
	ldsfld	float32 _blt_un::NaN
	ldsfld	float32 _blt_un::one
	blt.un AAA
	br FAIL

AAA:
	ldsfld	float32 _blt_un::NaN
	ldsfld	float32 _blt_un::max
	blt.un BBB
	br FAIL

BBB:
	ldsfld	float32 _blt_un::NaN
	ldsfld	float32 _blt_un::inf
	blt.un CCC
	br FAIL

CCC:
	ldsfld	float32 _blt_un::NaN
	ldsfld	float32 _blt_un::NaN
	blt.un  BACKCHECK
	br FAIL

TOPASS:
	br PASS
BACKCHECK:
	ldc.r4 0x0
	ldc.r4 0x1
	blt.un TOPASS
	br FAIL


PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly blt_un_r4.exe{}
