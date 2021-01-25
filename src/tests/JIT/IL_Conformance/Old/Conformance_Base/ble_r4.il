// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public _ble {

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
	stsfld	float32 _ble::_inf
	ldc.r4		float32(0xFF7FFFFF)
	stsfld	float32 _ble::_min
	ldc.r4		float32(0xBF800000)
	stsfld	float32 _ble::_one
	ldc.r4		float32(0x80000000)
	stsfld	float32 _ble::_zero
	ldc.r4		float32(0x00000000)
	stsfld	float32 _ble::zero
	ldc.r4		float32(0x3F800000)
	stsfld	float32 _ble::one
	ldc.r4		float32(0x7F7FFFFF)
	stsfld	float32 _ble::max
	ldc.r4		float32(0x7F800000)
	stsfld	float32 _ble::inf
	ldc.r4		float32(0x7FC00000)
	stsfld	float32 _ble::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void _ble::initialize()

	ldsfld	float32 _ble::_inf
	ldsfld	float32 _ble::_inf
	ble A
	br FAIL

A:
	ldsfld	float32 _ble::_inf
	ldsfld	float32 _ble::_min
	ble B
	br FAIL

B:
	ldsfld	float32 _ble::_inf
	ldsfld	float32 _ble::_one
	ble C
	br FAIL

C:
	ldsfld	float32 _ble::_inf
	ldsfld	float32 _ble::_zero
	ble D
	br FAIL

D:
	ldsfld	float32 _ble::_inf
	ldsfld	float32 _ble::zero
	ble E
	br FAIL

E:
	ldsfld	float32 _ble::_inf
	ldsfld	float32 _ble::one
	ble F
	br FAIL

F:
	ldsfld	float32 _ble::_inf
	ldsfld	float32 _ble::max
	ble G
	br FAIL

G:
	ldsfld	float32 _ble::_inf
	ldsfld	float32 _ble::inf
	ble H
	br FAIL

H:
	ldsfld	float32 _ble::_inf
	ldsfld	float32 _ble::NaN
	ble  FAIL
K:
	ldsfld	float32 _ble::_min
	ldsfld	float32 _ble::_inf
	ble FAIL

	ldsfld	float32 _ble::_min
	ldsfld	float32 _ble::_min
	ble L
	br FAIL

L:
	ldsfld	float32 _ble::_min
	ldsfld	float32 _ble::_one
	ble M
	br FAIL

M:
	ldsfld	float32 _ble::_min
	ldsfld	float32 _ble::_zero
	ble N
	br FAIL

N:
	ldsfld	float32 _ble::_min
	ldsfld	float32 _ble::zero
	ble O
	br FAIL

O:
	ldsfld	float32 _ble::_min
	ldsfld	float32 _ble::one
	ble P
	br FAIL

P:
	ldsfld	float32 _ble::_min
	ldsfld	float32 _ble::max
	ble Q
	br FAIL

Q:
	ldsfld	float32 _ble::_min
	ldsfld	float32 _ble::inf
	ble R
	br FAIL

R:
	ldsfld	float32 _ble::_min
	ldsfld	float32 _ble::NaN
	ble	FAIL


S:
	ldsfld	float32 _ble::_one
	ldsfld	float32 _ble::_inf
	ble FAIL

	ldsfld	float32 _ble::_one
	ldsfld	float32 _ble::_min
	ble FAIL

	ldsfld	float32 _ble::_one
	ldsfld	float32 _ble::_one
	ble T
	br FAIL

T:
	ldsfld	float32 _ble::_one
	ldsfld	float32 _ble::_zero
	ble U
	br FAIL

U:
	ldsfld	float32 _ble::_one
	ldsfld	float32 _ble::zero
	ble V
	br FAIL

V:
	ldsfld	float32 _ble::_one
	ldsfld	float32 _ble::one
	ble W
	br FAIL

W:
	ldsfld	float32 _ble::_one
	ldsfld	float32 _ble::max
	ble X
	br FAIL

X:
	ldsfld	float32 _ble::_one
	ldsfld	float32 _ble::inf
	ble Y
	br FAIL

Y:
	ldsfld	float32 _ble::_one
	ldsfld	float32 _ble::NaN
	ble	FAIL
Z:
	ldsfld	float32 _ble::_zero
	ldsfld	float32 _ble::_inf
	ble FAIL

	ldsfld	float32 _ble::_zero
	ldsfld	float32 _ble::_min
	ble FAIL

	ldsfld	float32 _ble::_zero
	ldsfld	float32 _ble::_one
	ble FAIL

	ldsfld	float32 _ble::_zero
	ldsfld	float32 _ble::_zero
	ble AA
	br FAIL

AA:
	ldsfld	float32 _ble::_zero
	ldsfld	float32 _ble::zero
	ble BB
	br FAIL

BB:
	ldsfld	float32 _ble::_zero
	ldsfld	float32 _ble::one
	ble CC
	br FAIL

CC:
	ldsfld	float32 _ble::_zero
	ldsfld	float32 _ble::max
	ble DD
	br FAIL

DD:
	ldsfld	float32 _ble::_zero
	ldsfld	float32 _ble::inf
	ble EE
	br FAIL

EE:
	ldsfld	float32 _ble::_zero
	ldsfld	float32 _ble::NaN
	ble	FAIL

FF:
	ldsfld	float32 _ble::zero
	ldsfld	float32 _ble::_inf
	ble FAIL

	ldsfld	float32 _ble::zero
	ldsfld	float32 _ble::_min
	ble FAIL

	ldsfld	float32 _ble::zero
	ldsfld	float32 _ble::_one
	ble FAIL

	ldsfld	float32 _ble::zero
	ldsfld	float32 _ble::_zero
	ble GG
	br FAIL

GG:
	ldsfld	float32 _ble::zero
	ldsfld	float32 _ble::zero
	ble HH
	br FAIL

HH:
	ldsfld	float32 _ble::zero
	ldsfld	float32 _ble::one
	ble II
	br FAIL

II:
	ldsfld	float32 _ble::zero
	ldsfld	float32 _ble::max
	ble JJ
	br FAIL

JJ:
	ldsfld	float32 _ble::zero
	ldsfld	float32 _ble::inf
	ble KK
	br FAIL

KK:
	ldsfld	float32 _ble::zero
	ldsfld	float32 _ble::NaN
	ble	FAIL

LL:
	ldsfld	float32 _ble::one
	ldsfld	float32 _ble::_inf
	ble FAIL

	ldsfld	float32 _ble::one
	ldsfld	float32 _ble::_min
	ble FAIL

	ldsfld	float32 _ble::one
	ldsfld	float32 _ble::_one
	ble FAIL

	ldsfld	float32 _ble::one
	ldsfld	float32 _ble::_zero
	ble FAIL

	ldsfld	float32 _ble::one
	ldsfld	float32 _ble::zero
	ble FAIL

	ldsfld	float32 _ble::one
	ldsfld	float32 _ble::one
	ble MM
	br FAIL

MM:
	ldsfld	float32 _ble::one
	ldsfld	float32 _ble::max
	ble NN
	br FAIL

NN:
	ldsfld	float32 _ble::one
	ldsfld	float32 _ble::inf
	ble OO
	br FAIL

OO:
	ldsfld	float32 _ble::one
	ldsfld	float32 _ble::NaN
	ble	FAIL

PP:
	ldsfld	float32 _ble::max
	ldsfld	float32 _ble::_inf
	ble FAIL

	ldsfld	float32 _ble::max
	ldsfld	float32 _ble::_min
	ble FAIL

	ldsfld	float32 _ble::max
	ldsfld	float32 _ble::_one
	ble FAIL

	ldsfld	float32 _ble::max
	ldsfld	float32 _ble::_zero
	ble FAIL

	ldsfld	float32 _ble::max
	ldsfld	float32 _ble::zero
	ble FAIL

	ldsfld	float32 _ble::max
	ldsfld	float32 _ble::one
	ble FAIL

	ldsfld	float32 _ble::max
	ldsfld	float32 _ble::max
	ble QQ
	br FAIL

QQ:
	ldsfld	float32 _ble::max
	ldsfld	float32 _ble::inf
	ble RR
	br FAIL

RR:
	ldsfld	float32 _ble::max
	ldsfld	float32 _ble::NaN
	ble	FAIL

SS:
	ldsfld	float32 _ble::inf
	ldsfld	float32 _ble::_inf
	ble FAIL

	ldsfld	float32 _ble::inf
	ldsfld	float32 _ble::_min
	ble FAIL

	ldsfld	float32 _ble::inf
	ldsfld	float32 _ble::_one
	ble FAIL

	ldsfld	float32 _ble::inf
	ldsfld	float32 _ble::_zero
	ble FAIL

	ldsfld	float32 _ble::inf
	ldsfld	float32 _ble::zero
	ble FAIL

	ldsfld	float32 _ble::inf
	ldsfld	float32 _ble::one
	ble FAIL

	ldsfld	float32 _ble::inf
	ldsfld	float32 _ble::max
	ble FAIL

	ldsfld	float32 _ble::inf
	ldsfld	float32 _ble::inf
	ble TT
	br FAIL

TT:
	ldsfld	float32 _ble::inf
	ldsfld	float32 _ble::NaN
	ble	FAIL

UU:
	ldsfld	float32 _ble::NaN
	ldsfld	float32 _ble::_inf
	ble	FAIL

VV:
	ldsfld	float32 _ble::NaN
	ldsfld	float32 _ble::_min
	ble	FAIL

WW:
	ldsfld	float32 _ble::NaN
	ldsfld	float32 _ble::_one
	ble	FAIL

XX:
	ldsfld	float32 _ble::NaN
	ldsfld	float32 _ble::_zero
	ble	FAIL

YY:
	ldsfld	float32 _ble::NaN
	ldsfld	float32 _ble::zero
	ble	FAIL

ZZ:
	ldsfld	float32 _ble::NaN
	ldsfld	float32 _ble::one
	ble	FAIL

AAA:
	ldsfld	float32 _ble::NaN
	ldsfld	float32 _ble::max
	ble	FAIL

BBB:
	ldsfld	float32 _ble::NaN
	ldsfld	float32 _ble::inf
	ble	FAIL

CCC:
	ldsfld	float32 _ble::NaN
	ldsfld	float32 _ble::NaN
	ble	FAIL
	br	BACKCHECK

TOPASS:
	br PASS
BACKCHECK:
	ldc.r4 0x0
	ldc.r4 0x1
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
.assembly ble_r4.exe{}
