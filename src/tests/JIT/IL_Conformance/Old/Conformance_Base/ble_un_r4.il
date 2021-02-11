// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public _ble_un {

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
	stsfld	float32 _ble_un::_inf
	ldc.r4		float32(0xFF7FFFFF)
	stsfld	float32 _ble_un::_min
	ldc.r4		float32(0xBF800000)
	stsfld	float32 _ble_un::_one
	ldc.r4		float32(0x80000000)
	stsfld	float32 _ble_un::_zero
	ldc.r4		float32(0x00000000)
	stsfld	float32 _ble_un::zero
	ldc.r4		float32(0x3F800000)
	stsfld	float32 _ble_un::one
	ldc.r4		float32(0x7F7FFFFF)
	stsfld	float32 _ble_un::max
	ldc.r4		float32(0x7F800000)
	stsfld	float32 _ble_un::inf
	ldc.r4		float32(0x7FC00000)
	stsfld	float32 _ble_un::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void _ble_un::initialize()

	ldsfld	float32 _ble_un::_inf
	ldsfld	float32 _ble_un::_inf
	ble.un A
	br FAIL

A:
	ldsfld	float32 _ble_un::_inf
	ldsfld	float32 _ble_un::_min
	ble.un B
	br FAIL

B:
	ldsfld	float32 _ble_un::_inf
	ldsfld	float32 _ble_un::_one
	ble.un C
	br FAIL

C:
	ldsfld	float32 _ble_un::_inf
	ldsfld	float32 _ble_un::_zero
	ble.un D
	br FAIL

D:
	ldsfld	float32 _ble_un::_inf
	ldsfld	float32 _ble_un::zero
	ble.un E
	br FAIL

E:
	ldsfld	float32 _ble_un::_inf
	ldsfld	float32 _ble_un::one
	ble.un F
	br FAIL

F:
	ldsfld	float32 _ble_un::_inf
	ldsfld	float32 _ble_un::max
	ble.un G
	br FAIL

G:
	ldsfld	float32 _ble_un::_inf
	ldsfld	float32 _ble_un::inf
	ble.un H
	br FAIL

H:
	ldsfld	float32 _ble_un::_inf
	ldsfld	float32 _ble_un::NaN
	ble.un K
	br FAIL
K:
	ldsfld	float32 _ble_un::_min
	ldsfld	float32 _ble_un::_inf
	ble.un FAIL

	ldsfld	float32 _ble_un::_min
	ldsfld	float32 _ble_un::_min
	ble.un L
	br FAIL

L:
	ldsfld	float32 _ble_un::_min
	ldsfld	float32 _ble_un::_one
	ble.un M
	br FAIL

M:
	ldsfld	float32 _ble_un::_min
	ldsfld	float32 _ble_un::_zero
	ble.un N
	br FAIL

N:
	ldsfld	float32 _ble_un::_min
	ldsfld	float32 _ble_un::zero
	ble.un O
	br FAIL

O:
	ldsfld	float32 _ble_un::_min
	ldsfld	float32 _ble_un::one
	ble.un P
	br FAIL

P:
	ldsfld	float32 _ble_un::_min
	ldsfld	float32 _ble_un::max
	ble.un Q
	br FAIL

Q:
	ldsfld	float32 _ble_un::_min
	ldsfld	float32 _ble_un::inf
	ble.un R
	br FAIL

R:
	ldsfld	float32 _ble_un::_min
	ldsfld	float32 _ble_un::NaN
	ble.un S
	br FAIL


S:
	ldsfld	float32 _ble_un::_one
	ldsfld	float32 _ble_un::_inf
	ble.un FAIL

	ldsfld	float32 _ble_un::_one
	ldsfld	float32 _ble_un::_min
	ble.un FAIL

	ldsfld	float32 _ble_un::_one
	ldsfld	float32 _ble_un::_one
	ble.un T
	br FAIL

T:
	ldsfld	float32 _ble_un::_one
	ldsfld	float32 _ble_un::_zero
	ble.un U
	br FAIL

U:
	ldsfld	float32 _ble_un::_one
	ldsfld	float32 _ble_un::zero
	ble.un V
	br FAIL

V:
	ldsfld	float32 _ble_un::_one
	ldsfld	float32 _ble_un::one
	ble.un W
	br FAIL

W:
	ldsfld	float32 _ble_un::_one
	ldsfld	float32 _ble_un::max
	ble.un X
	br FAIL

X:
	ldsfld	float32 _ble_un::_one
	ldsfld	float32 _ble_un::inf
	ble.un Y
	br FAIL

Y:
	ldsfld	float32 _ble_un::_one
	ldsfld	float32 _ble_un::NaN
	ble.un Z
	br FAIL
Z:
	ldsfld	float32 _ble_un::_zero
	ldsfld	float32 _ble_un::_inf
	ble.un FAIL

	ldsfld	float32 _ble_un::_zero
	ldsfld	float32 _ble_un::_min
	ble.un FAIL

	ldsfld	float32 _ble_un::_zero
	ldsfld	float32 _ble_un::_one
	ble.un FAIL

	ldsfld	float32 _ble_un::_zero
	ldsfld	float32 _ble_un::_zero
	ble.un AA
	br FAIL

AA:
	ldsfld	float32 _ble_un::_zero
	ldsfld	float32 _ble_un::zero
	ble.un BB
	br FAIL

BB:
	ldsfld	float32 _ble_un::_zero
	ldsfld	float32 _ble_un::one
	ble.un CC
	br FAIL

CC:
	ldsfld	float32 _ble_un::_zero
	ldsfld	float32 _ble_un::max
	ble.un DD
	br FAIL

DD:
	ldsfld	float32 _ble_un::_zero
	ldsfld	float32 _ble_un::inf
	ble.un EE
	br FAIL

EE:
	ldsfld	float32 _ble_un::_zero
	ldsfld	float32 _ble_un::NaN
	ble.un FF
	br FAIL

FF:
	ldsfld	float32 _ble_un::zero
	ldsfld	float32 _ble_un::_inf
	ble.un FAIL

	ldsfld	float32 _ble_un::zero
	ldsfld	float32 _ble_un::_min
	ble.un FAIL

	ldsfld	float32 _ble_un::zero
	ldsfld	float32 _ble_un::_one
	ble.un FAIL

	ldsfld	float32 _ble_un::zero
	ldsfld	float32 _ble_un::_zero
	ble.un GG
	br FAIL

GG:
	ldsfld	float32 _ble_un::zero
	ldsfld	float32 _ble_un::zero
	ble.un HH
	br FAIL

HH:
	ldsfld	float32 _ble_un::zero
	ldsfld	float32 _ble_un::one
	ble.un II
	br FAIL

II:
	ldsfld	float32 _ble_un::zero
	ldsfld	float32 _ble_un::max
	ble.un JJ
	br FAIL

JJ:
	ldsfld	float32 _ble_un::zero
	ldsfld	float32 _ble_un::inf
	ble.un KK
	br FAIL

KK:
	ldsfld	float32 _ble_un::zero
	ldsfld	float32 _ble_un::NaN
	ble.un LL
	br FAIL

LL:
	ldsfld	float32 _ble_un::one
	ldsfld	float32 _ble_un::_inf
	ble.un FAIL

	ldsfld	float32 _ble_un::one
	ldsfld	float32 _ble_un::_min
	ble.un FAIL

	ldsfld	float32 _ble_un::one
	ldsfld	float32 _ble_un::_one
	ble.un FAIL

	ldsfld	float32 _ble_un::one
	ldsfld	float32 _ble_un::_zero
	ble.un FAIL

	ldsfld	float32 _ble_un::one
	ldsfld	float32 _ble_un::zero
	ble.un FAIL

	ldsfld	float32 _ble_un::one
	ldsfld	float32 _ble_un::one
	ble.un MM
	br FAIL

MM:
	ldsfld	float32 _ble_un::one
	ldsfld	float32 _ble_un::max
	ble.un NN
	br FAIL

NN:
	ldsfld	float32 _ble_un::one
	ldsfld	float32 _ble_un::inf
	ble.un OO
	br FAIL

OO:
	ldsfld	float32 _ble_un::one
	ldsfld	float32 _ble_un::NaN
	ble.un PP
	br FAIL

PP:
	ldsfld	float32 _ble_un::max
	ldsfld	float32 _ble_un::_inf
	ble.un FAIL

	ldsfld	float32 _ble_un::max
	ldsfld	float32 _ble_un::_min
	ble.un FAIL

	ldsfld	float32 _ble_un::max
	ldsfld	float32 _ble_un::_one
	ble.un FAIL

	ldsfld	float32 _ble_un::max
	ldsfld	float32 _ble_un::_zero
	ble.un FAIL

	ldsfld	float32 _ble_un::max
	ldsfld	float32 _ble_un::zero
	ble.un FAIL

	ldsfld	float32 _ble_un::max
	ldsfld	float32 _ble_un::one
	ble.un FAIL

	ldsfld	float32 _ble_un::max
	ldsfld	float32 _ble_un::max
	ble.un QQ
	br FAIL

QQ:
	ldsfld	float32 _ble_un::max
	ldsfld	float32 _ble_un::inf
	ble.un RR
	br FAIL

RR:
	ldsfld	float32 _ble_un::max
	ldsfld	float32 _ble_un::NaN
	ble.un SS
	br FAIL

SS:
	ldsfld	float32 _ble_un::inf
	ldsfld	float32 _ble_un::_inf
	ble.un FAIL

	ldsfld	float32 _ble_un::inf
	ldsfld	float32 _ble_un::_min
	ble.un FAIL

	ldsfld	float32 _ble_un::inf
	ldsfld	float32 _ble_un::_one
	ble.un FAIL

	ldsfld	float32 _ble_un::inf
	ldsfld	float32 _ble_un::_zero
	ble.un FAIL

	ldsfld	float32 _ble_un::inf
	ldsfld	float32 _ble_un::zero
	ble.un FAIL

	ldsfld	float32 _ble_un::inf
	ldsfld	float32 _ble_un::one
	ble.un FAIL

	ldsfld	float32 _ble_un::inf
	ldsfld	float32 _ble_un::max
	ble.un FAIL

	ldsfld	float32 _ble_un::inf
	ldsfld	float32 _ble_un::inf
	ble.un TT
	br FAIL

TT:
	ldsfld	float32 _ble_un::inf
	ldsfld	float32 _ble_un::NaN
	ble.un UU
	br FAIL

UU:
	ldsfld	float32 _ble_un::NaN
	ldsfld	float32 _ble_un::_inf
	ble.un VV
	br FAIL

VV:
	ldsfld	float32 _ble_un::NaN
	ldsfld	float32 _ble_un::_min
	ble.un WW
	br FAIL

WW:
	ldsfld	float32 _ble_un::NaN
	ldsfld	float32 _ble_un::_one
	ble.un XX
	br FAIL

XX:
	ldsfld	float32 _ble_un::NaN
	ldsfld	float32 _ble_un::_zero
	ble.un YY
	br FAIL

YY:
	ldsfld	float32 _ble_un::NaN
	ldsfld	float32 _ble_un::zero
	ble.un ZZ
	br FAIL

ZZ:
	ldsfld	float32 _ble_un::NaN
	ldsfld	float32 _ble_un::one
	ble.un AAA
	br FAIL

AAA:
	ldsfld	float32 _ble_un::NaN
	ldsfld	float32 _ble_un::max
	ble.un BBB
	br FAIL

BBB:
	ldsfld	float32 _ble_un::NaN
	ldsfld	float32 _ble_un::inf
	ble.un CCC
	br FAIL

CCC:
	ldsfld	float32 _ble_un::NaN
	ldsfld	float32 _ble_un::NaN
	ble.un  BACKCHECK
	br FAIL

TOPASS:
	br PASS
BACKCHECK:
	ldc.r4 0x0
	ldc.r4 0x1
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
.assembly ble_un_r4.exe{}
