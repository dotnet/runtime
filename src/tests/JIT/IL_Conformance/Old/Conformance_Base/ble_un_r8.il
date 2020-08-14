// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public ble_un {

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
	stsfld	float64 ble_un::_inf
	ldc.r8		float64(0xFF7FFFFFFFFFFFFF)
	stsfld	float64 ble_un::_min
	ldc.r8		float64(0xBFF0000000000000)
	stsfld	float64 ble_un::_one
	ldc.r8		float64(0x8000000000000000)
	stsfld	float64 ble_un::_zero
	ldc.r8		float64(0x0000000000000000)
	stsfld	float64 ble_un::zero
	ldc.r8		float64(0x3FF0000000000000)
	stsfld	float64 ble_un::one
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	stsfld	float64 ble_un::max
	ldc.r8		float64(0x7FF0000000000000)
	stsfld	float64 ble_un::inf
	ldc.r8		float64(0x7FF8000000000000)
	stsfld	float64 ble_un::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void ble_un::initialize()

	ldsfld	float64 ble_un::_inf
	ldsfld	float64 ble_un::_inf
	ble.un A
	br FAIL

A:
	ldsfld	float64 ble_un::_inf
	ldsfld	float64 ble_un::_min
	ble.un B
	br FAIL

B:
	ldsfld	float64 ble_un::_inf
	ldsfld	float64 ble_un::_one
	ble.un C
	br FAIL

C:
	ldsfld	float64 ble_un::_inf
	ldsfld	float64 ble_un::_zero
	ble.un D
	br FAIL

D:
	ldsfld	float64 ble_un::_inf
	ldsfld	float64 ble_un::zero
	ble.un E
	br FAIL

E:
	ldsfld	float64 ble_un::_inf
	ldsfld	float64 ble_un::one
	ble.un F
	br FAIL

F:
	ldsfld	float64 ble_un::_inf
	ldsfld	float64 ble_un::max
	ble.un G
	br FAIL

G:
	ldsfld	float64 ble_un::_inf
	ldsfld	float64 ble_un::inf
	ble.un H
	br FAIL

H:
	ldsfld	float64 ble_un::_inf
	ldsfld	float64 ble_un::NaN
	ble.un K
	br FAIL
K:
	ldsfld	float64 ble_un::_min
	ldsfld	float64 ble_un::_inf
	ble.un FAIL

	ldsfld	float64 ble_un::_min
	ldsfld	float64 ble_un::_min
	ble.un L
	br FAIL

L:
	ldsfld	float64 ble_un::_min
	ldsfld	float64 ble_un::_one
	ble.un M
	br FAIL

M:
	ldsfld	float64 ble_un::_min
	ldsfld	float64 ble_un::_zero
	ble.un N
	br FAIL

N:
	ldsfld	float64 ble_un::_min
	ldsfld	float64 ble_un::zero
	ble.un O
	br FAIL

O:
	ldsfld	float64 ble_un::_min
	ldsfld	float64 ble_un::one
	ble.un P
	br FAIL

P:
	ldsfld	float64 ble_un::_min
	ldsfld	float64 ble_un::max
	ble.un Q
	br FAIL

Q:
	ldsfld	float64 ble_un::_min
	ldsfld	float64 ble_un::inf
	ble.un R
	br FAIL

R:
	ldsfld	float64 ble_un::_min
	ldsfld	float64 ble_un::NaN
	ble.un S
	br FAIL


S:
	ldsfld	float64 ble_un::_one
	ldsfld	float64 ble_un::_inf
	ble.un FAIL

	ldsfld	float64 ble_un::_one
	ldsfld	float64 ble_un::_min
	ble.un FAIL

	ldsfld	float64 ble_un::_one
	ldsfld	float64 ble_un::_one
	ble.un T
	br FAIL

T:
	ldsfld	float64 ble_un::_one
	ldsfld	float64 ble_un::_zero
	ble.un U
	br FAIL

U:
	ldsfld	float64 ble_un::_one
	ldsfld	float64 ble_un::zero
	ble.un V
	br FAIL

V:
	ldsfld	float64 ble_un::_one
	ldsfld	float64 ble_un::one
	ble.un W
	br FAIL

W:
	ldsfld	float64 ble_un::_one
	ldsfld	float64 ble_un::max
	ble.un X
	br FAIL

X:
	ldsfld	float64 ble_un::_one
	ldsfld	float64 ble_un::inf
	ble.un Y
	br FAIL

Y:
	ldsfld	float64 ble_un::_one
	ldsfld	float64 ble_un::NaN
	ble.un Z
	br FAIL
Z:
	ldsfld	float64 ble_un::_zero
	ldsfld	float64 ble_un::_inf
	ble.un FAIL

	ldsfld	float64 ble_un::_zero
	ldsfld	float64 ble_un::_min
	ble.un FAIL

	ldsfld	float64 ble_un::_zero
	ldsfld	float64 ble_un::_one
	ble.un FAIL

	ldsfld	float64 ble_un::_zero
	ldsfld	float64 ble_un::_zero
	ble.un AA
	br FAIL

AA:
	ldsfld	float64 ble_un::_zero
	ldsfld	float64 ble_un::zero
	ble.un BB
	br FAIL

BB:
	ldsfld	float64 ble_un::_zero
	ldsfld	float64 ble_un::one
	ble.un CC
	br FAIL

CC:
	ldsfld	float64 ble_un::_zero
	ldsfld	float64 ble_un::max
	ble.un DD
	br FAIL

DD:
	ldsfld	float64 ble_un::_zero
	ldsfld	float64 ble_un::inf
	ble.un EE
	br FAIL

EE:
	ldsfld	float64 ble_un::_zero
	ldsfld	float64 ble_un::NaN
	ble.un FF
	br FAIL

FF:
	ldsfld	float64 ble_un::zero
	ldsfld	float64 ble_un::_inf
	ble.un FAIL

	ldsfld	float64 ble_un::zero
	ldsfld	float64 ble_un::_min
	ble.un FAIL

	ldsfld	float64 ble_un::zero
	ldsfld	float64 ble_un::_one
	ble.un FAIL

	ldsfld	float64 ble_un::zero
	ldsfld	float64 ble_un::_zero
	ble.un GG
	br FAIL

GG:
	ldsfld	float64 ble_un::zero
	ldsfld	float64 ble_un::zero
	ble.un HH
	br FAIL

HH:
	ldsfld	float64 ble_un::zero
	ldsfld	float64 ble_un::one
	ble.un II
	br FAIL

II:
	ldsfld	float64 ble_un::zero
	ldsfld	float64 ble_un::max
	ble.un JJ
	br FAIL

JJ:
	ldsfld	float64 ble_un::zero
	ldsfld	float64 ble_un::inf
	ble.un KK
	br FAIL

KK:
	ldsfld	float64 ble_un::zero
	ldsfld	float64 ble_un::NaN
	ble.un LL
	br FAIL

LL:
	ldsfld	float64 ble_un::one
	ldsfld	float64 ble_un::_inf
	ble.un FAIL

	ldsfld	float64 ble_un::one
	ldsfld	float64 ble_un::_min
	ble.un FAIL

	ldsfld	float64 ble_un::one
	ldsfld	float64 ble_un::_one
	ble.un FAIL

	ldsfld	float64 ble_un::one
	ldsfld	float64 ble_un::_zero
	ble.un FAIL

	ldsfld	float64 ble_un::one
	ldsfld	float64 ble_un::zero
	ble.un FAIL

	ldsfld	float64 ble_un::one
	ldsfld	float64 ble_un::one
	ble.un MM
	br FAIL

MM:
	ldsfld	float64 ble_un::one
	ldsfld	float64 ble_un::max
	ble.un NN
	br FAIL

NN:
	ldsfld	float64 ble_un::one
	ldsfld	float64 ble_un::inf
	ble.un OO
	br FAIL

OO:
	ldsfld	float64 ble_un::one
	ldsfld	float64 ble_un::NaN
	ble.un PP
	br FAIL

PP:
	ldsfld	float64 ble_un::max
	ldsfld	float64 ble_un::_inf
	ble.un FAIL

	ldsfld	float64 ble_un::max
	ldsfld	float64 ble_un::_min
	ble.un FAIL

	ldsfld	float64 ble_un::max
	ldsfld	float64 ble_un::_one
	ble.un FAIL

	ldsfld	float64 ble_un::max
	ldsfld	float64 ble_un::_zero
	ble.un FAIL

	ldsfld	float64 ble_un::max
	ldsfld	float64 ble_un::zero
	ble.un FAIL

	ldsfld	float64 ble_un::max
	ldsfld	float64 ble_un::one
	ble.un FAIL

	ldsfld	float64 ble_un::max
	ldsfld	float64 ble_un::max
	ble.un QQ
	br FAIL

QQ:
	ldsfld	float64 ble_un::max
	ldsfld	float64 ble_un::inf
	ble.un RR
	br FAIL

RR:
	ldsfld	float64 ble_un::max
	ldsfld	float64 ble_un::NaN
	ble.un SS
	br FAIL

SS:
	ldsfld	float64 ble_un::inf
	ldsfld	float64 ble_un::_inf
	ble.un FAIL

	ldsfld	float64 ble_un::inf
	ldsfld	float64 ble_un::_min
	ble.un FAIL

	ldsfld	float64 ble_un::inf
	ldsfld	float64 ble_un::_one
	ble.un FAIL

	ldsfld	float64 ble_un::inf
	ldsfld	float64 ble_un::_zero
	ble.un FAIL

	ldsfld	float64 ble_un::inf
	ldsfld	float64 ble_un::zero
	ble.un FAIL

	ldsfld	float64 ble_un::inf
	ldsfld	float64 ble_un::one
	ble.un FAIL

	ldsfld	float64 ble_un::inf
	ldsfld	float64 ble_un::max
	ble.un FAIL

	ldsfld	float64 ble_un::inf
	ldsfld	float64 ble_un::inf
	ble.un TT
	br FAIL

TT:
	ldsfld	float64 ble_un::inf
	ldsfld	float64 ble_un::NaN
	ble.un UU
	br FAIL

UU:
	ldsfld	float64 ble_un::NaN
	ldsfld	float64 ble_un::_inf
	ble.un VV
	br FAIL

VV:
	ldsfld	float64 ble_un::NaN
	ldsfld	float64 ble_un::_min
	ble.un WW
	br FAIL

WW:
	ldsfld	float64 ble_un::NaN
	ldsfld	float64 ble_un::_one
	ble.un XX
	br FAIL

XX:
	ldsfld	float64 ble_un::NaN
	ldsfld	float64 ble_un::_zero
	ble.un YY
	br FAIL

YY:
	ldsfld	float64 ble_un::NaN
	ldsfld	float64 ble_un::zero
	ble.un ZZ
	br FAIL

ZZ:
	ldsfld	float64 ble_un::NaN
	ldsfld	float64 ble_un::one
	ble.un AAA
	br FAIL

AAA:
	ldsfld	float64 ble_un::NaN
	ldsfld	float64 ble_un::max
	ble.un BBB
	br FAIL

BBB:
	ldsfld	float64 ble_un::NaN
	ldsfld	float64 ble_un::inf
	ble.un CCC
	br FAIL

CCC:
	ldsfld	float64 ble_un::NaN
	ldsfld	float64 ble_un::NaN
	ble.un  BACKCHECK
	br FAIL

TOPASS:
	br PASS
BACKCHECK:
	ldc.r8 0x0
	ldc.r8 0x1
	ble.un TOPASS
	br FAIL


PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ble_un_r8.exe{}
