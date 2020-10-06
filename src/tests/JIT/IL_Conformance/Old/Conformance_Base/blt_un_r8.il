// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public blt_un {

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
	stsfld	float64 blt_un::_inf
	ldc.r8		float64(0xFF7FFFFFFFFFFFFF)
	stsfld	float64 blt_un::_min
	ldc.r8		float64(0xBFF0000000000000)
	stsfld	float64 blt_un::_one
	ldc.r8		float64(0x8000000000000000)
	stsfld	float64 blt_un::_zero
	ldc.r8		float64(0x0000000000000000)
	stsfld	float64 blt_un::zero
	ldc.r8		float64(0x3FF0000000000000)
	stsfld	float64 blt_un::one
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	stsfld	float64 blt_un::max
	ldc.r8		float64(0x7FF0000000000000)
	stsfld	float64 blt_un::inf
	ldc.r8		float64(0x7FF8000000000000)
	stsfld	float64 blt_un::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void blt_un::initialize()

	ldsfld	float64 blt_un::_inf
	ldsfld	float64 blt_un::_inf
	blt.un FAIL

A:
	ldsfld	float64 blt_un::_inf
	ldsfld	float64 blt_un::_min
	blt.un B
	br FAIL

B:
	ldsfld	float64 blt_un::_inf
	ldsfld	float64 blt_un::_one
	blt.un C
	br FAIL

C:
	ldsfld	float64 blt_un::_inf
	ldsfld	float64 blt_un::_zero
	blt.un D
	br FAIL

D:
	ldsfld	float64 blt_un::_inf
	ldsfld	float64 blt_un::zero
	blt.un E
	br FAIL

E:
	ldsfld	float64 blt_un::_inf
	ldsfld	float64 blt_un::one
	blt.un F
	br FAIL

F:
	ldsfld	float64 blt_un::_inf
	ldsfld	float64 blt_un::max
	blt.un G
	br FAIL

G:
	ldsfld	float64 blt_un::_inf
	ldsfld	float64 blt_un::inf
	blt.un H
	br FAIL

H:
	ldsfld	float64 blt_un::_inf
	ldsfld	float64 blt_un::NaN
	blt.un K
	br FAIL
K:
	ldsfld	float64 blt_un::_min
	ldsfld	float64 blt_un::_inf
	blt.un FAIL

	ldsfld	float64 blt_un::_min
	ldsfld	float64 blt_un::_min
	blt.un FAIL

L:
	ldsfld	float64 blt_un::_min
	ldsfld	float64 blt_un::_one
	blt.un M
	br FAIL

M:
	ldsfld	float64 blt_un::_min
	ldsfld	float64 blt_un::_zero
	blt.un N
	br FAIL

N:
	ldsfld	float64 blt_un::_min
	ldsfld	float64 blt_un::zero
	blt.un O
	br FAIL

O:
	ldsfld	float64 blt_un::_min
	ldsfld	float64 blt_un::one
	blt.un P
	br FAIL

P:
	ldsfld	float64 blt_un::_min
	ldsfld	float64 blt_un::max
	blt.un Q
	br FAIL

Q:
	ldsfld	float64 blt_un::_min
	ldsfld	float64 blt_un::inf
	blt.un R
	br FAIL

R:
	ldsfld	float64 blt_un::_min
	ldsfld	float64 blt_un::NaN
	blt.un S
	br FAIL


S:
	ldsfld	float64 blt_un::_one
	ldsfld	float64 blt_un::_inf
	blt.un FAIL

	ldsfld	float64 blt_un::_one
	ldsfld	float64 blt_un::_min
	blt.un FAIL

	ldsfld	float64 blt_un::_one
	ldsfld	float64 blt_un::_one
	blt.un FAIL

T:
	ldsfld	float64 blt_un::_one
	ldsfld	float64 blt_un::_zero
	blt.un U
	br FAIL

U:
	ldsfld	float64 blt_un::_one
	ldsfld	float64 blt_un::zero
	blt.un V
	br FAIL

V:
	ldsfld	float64 blt_un::_one
	ldsfld	float64 blt_un::one
	blt.un W
	br FAIL

W:
	ldsfld	float64 blt_un::_one
	ldsfld	float64 blt_un::max
	blt.un X
	br FAIL

X:
	ldsfld	float64 blt_un::_one
	ldsfld	float64 blt_un::inf
	blt.un Y
	br FAIL

Y:
	ldsfld	float64 blt_un::_one
	ldsfld	float64 blt_un::NaN
	blt.un Z
	br FAIL
Z:
	ldsfld	float64 blt_un::_zero
	ldsfld	float64 blt_un::_inf
	blt.un FAIL

	ldsfld	float64 blt_un::_zero
	ldsfld	float64 blt_un::_min
	blt.un FAIL

	ldsfld	float64 blt_un::_zero
	ldsfld	float64 blt_un::_one
	blt.un FAIL

	ldsfld	float64 blt_un::_zero
	ldsfld	float64 blt_un::_zero
	blt.un FAIL

AA:
	ldsfld	float64 blt_un::_zero
	ldsfld	float64 blt_un::zero
	blt.un FAIL

BB:
	ldsfld	float64 blt_un::_zero
	ldsfld	float64 blt_un::one
	blt.un CC
	br FAIL

CC:
	ldsfld	float64 blt_un::_zero
	ldsfld	float64 blt_un::max
	blt.un DD
	br FAIL

DD:
	ldsfld	float64 blt_un::_zero
	ldsfld	float64 blt_un::inf
	blt.un EE
	br FAIL

EE:
	ldsfld	float64 blt_un::_zero
	ldsfld	float64 blt_un::NaN
	blt.un FF
	br FAIL

FF:
	ldsfld	float64 blt_un::zero
	ldsfld	float64 blt_un::_inf
	blt.un FAIL

	ldsfld	float64 blt_un::zero
	ldsfld	float64 blt_un::_min
	blt.un FAIL

	ldsfld	float64 blt_un::zero
	ldsfld	float64 blt_un::_one
	blt.un FAIL

	ldsfld	float64 blt_un::zero
	ldsfld	float64 blt_un::_zero
	blt.un FAIL

GG:
	ldsfld	float64 blt_un::zero
	ldsfld	float64 blt_un::zero
	blt.un FAIL

HH:
	ldsfld	float64 blt_un::zero
	ldsfld	float64 blt_un::one
	blt.un II
	br FAIL

II:
	ldsfld	float64 blt_un::zero
	ldsfld	float64 blt_un::max
	blt.un JJ
	br FAIL

JJ:
	ldsfld	float64 blt_un::zero
	ldsfld	float64 blt_un::inf
	blt.un KK
	br FAIL

KK:
	ldsfld	float64 blt_un::zero
	ldsfld	float64 blt_un::NaN
	blt.un LL
	br FAIL

LL:
	ldsfld	float64 blt_un::one
	ldsfld	float64 blt_un::_inf
	blt.un FAIL

	ldsfld	float64 blt_un::one
	ldsfld	float64 blt_un::_min
	blt.un FAIL

	ldsfld	float64 blt_un::one
	ldsfld	float64 blt_un::_one
	blt.un FAIL

	ldsfld	float64 blt_un::one
	ldsfld	float64 blt_un::_zero
	blt.un FAIL

	ldsfld	float64 blt_un::one
	ldsfld	float64 blt_un::zero
	blt.un FAIL

	ldsfld	float64 blt_un::one
	ldsfld	float64 blt_un::one
	blt.un FAIL

MM:
	ldsfld	float64 blt_un::one
	ldsfld	float64 blt_un::max
	blt.un NN
	br FAIL

NN:
	ldsfld	float64 blt_un::one
	ldsfld	float64 blt_un::inf
	blt.un OO
	br FAIL

OO:
	ldsfld	float64 blt_un::one
	ldsfld	float64 blt_un::NaN
	blt.un PP
	br FAIL

PP:
	ldsfld	float64 blt_un::max
	ldsfld	float64 blt_un::_inf
	blt.un FAIL

	ldsfld	float64 blt_un::max
	ldsfld	float64 blt_un::_min
	blt.un FAIL

	ldsfld	float64 blt_un::max
	ldsfld	float64 blt_un::_one
	blt.un FAIL

	ldsfld	float64 blt_un::max
	ldsfld	float64 blt_un::_zero
	blt.un FAIL

	ldsfld	float64 blt_un::max
	ldsfld	float64 blt_un::zero
	blt.un FAIL

	ldsfld	float64 blt_un::max
	ldsfld	float64 blt_un::one
	blt.un FAIL

	ldsfld	float64 blt_un::max
	ldsfld	float64 blt_un::max
	blt.un FAIL

QQ:
	ldsfld	float64 blt_un::max
	ldsfld	float64 blt_un::inf
	blt.un RR
	br FAIL

RR:
	ldsfld	float64 blt_un::max
	ldsfld	float64 blt_un::NaN
	blt.un SS
	br FAIL

SS:
	ldsfld	float64 blt_un::inf
	ldsfld	float64 blt_un::_inf
	blt.un FAIL

	ldsfld	float64 blt_un::inf
	ldsfld	float64 blt_un::_min
	blt.un FAIL

	ldsfld	float64 blt_un::inf
	ldsfld	float64 blt_un::_one
	blt.un FAIL

	ldsfld	float64 blt_un::inf
	ldsfld	float64 blt_un::_zero
	blt.un FAIL

	ldsfld	float64 blt_un::inf
	ldsfld	float64 blt_un::zero
	blt.un FAIL

	ldsfld	float64 blt_un::inf
	ldsfld	float64 blt_un::one
	blt.un FAIL

	ldsfld	float64 blt_un::inf
	ldsfld	float64 blt_un::max
	blt.un FAIL

	ldsfld	float64 blt_un::inf
	ldsfld	float64 blt_un::inf
	blt.un FAIL

TT:
	ldsfld	float64 blt_un::inf
	ldsfld	float64 blt_un::NaN
	blt.un UU
	br FAIL

UU:
	ldsfld	float64 blt_un::NaN
	ldsfld	float64 blt_un::_inf
	blt.un VV
	br FAIL

VV:
	ldsfld	float64 blt_un::NaN
	ldsfld	float64 blt_un::_min
	blt.un WW
	br FAIL

WW:
	ldsfld	float64 blt_un::NaN
	ldsfld	float64 blt_un::_one
	blt.un XX
	br FAIL

XX:
	ldsfld	float64 blt_un::NaN
	ldsfld	float64 blt_un::_zero
	blt.un YY
	br FAIL

YY:
	ldsfld	float64 blt_un::NaN
	ldsfld	float64 blt_un::zero
	blt.un ZZ
	br FAIL

ZZ:
	ldsfld	float64 blt_un::NaN
	ldsfld	float64 blt_un::one
	blt.un AAA
	br FAIL

AAA:
	ldsfld	float64 blt_un::NaN
	ldsfld	float64 blt_un::max
	blt.un BBB
	br FAIL

BBB:
	ldsfld	float64 blt_un::NaN
	ldsfld	float64 blt_un::inf
	blt.un CCC
	br FAIL

CCC:
	ldsfld	float64 blt_un::NaN
	ldsfld	float64 blt_un::NaN
	blt.un  BACKCHECK
	br FAIL

TOPASS:
	br PASS
BACKCHECK:
	ldc.r8 0x0
	ldc.r8 0x1
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
.assembly blt_un_r8.exe{}
