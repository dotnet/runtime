// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public _ble {

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
	stsfld	float64 _ble::_inf
	ldc.r8		float64(0xFF7FFFFFFFFFFFFF)
	stsfld	float64 _ble::_min
	ldc.r8		float64(0xBFF0000000000000)
	stsfld	float64 _ble::_one
	ldc.r8		float64(0x8000000000000000)
	stsfld	float64 _ble::_zero
	ldc.r8		float64(0x0000000000000000)
	stsfld	float64 _ble::zero
	ldc.r8		float64(0x3FF0000000000000)
	stsfld	float64 _ble::one
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	stsfld	float64 _ble::max
	ldc.r8		float64(0x7FF0000000000000)
	stsfld	float64 _ble::inf
	ldc.r8		float64(0x7FF8000000000000)
	stsfld	float64 _ble::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void _ble::initialize()

	ldsfld	float64 _ble::_inf
	ldsfld	float64 _ble::_inf
	ble A
	br FAIL

A:
	ldsfld	float64 _ble::_inf
	ldsfld	float64 _ble::_min
	ble B
	br FAIL

B:
	ldsfld	float64 _ble::_inf
	ldsfld	float64 _ble::_one
	ble C
	br FAIL

C:
	ldsfld	float64 _ble::_inf
	ldsfld	float64 _ble::_zero
	ble D
	br FAIL

D:
	ldsfld	float64 _ble::_inf
	ldsfld	float64 _ble::zero
	ble E
	br FAIL

E:
	ldsfld	float64 _ble::_inf
	ldsfld	float64 _ble::one
	ble F
	br FAIL

F:
	ldsfld	float64 _ble::_inf
	ldsfld	float64 _ble::max
	ble G
	br FAIL

G:
	ldsfld	float64 _ble::_inf
	ldsfld	float64 _ble::inf
	ble H
	br FAIL

H:
	ldsfld	float64 _ble::_inf
	ldsfld	float64 _ble::NaN
	ble 	FAIL

K:
	ldsfld	float64 _ble::_min
	ldsfld	float64 _ble::_inf
	ble FAIL

	ldsfld	float64 _ble::_min
	ldsfld	float64 _ble::_min
	ble L
	br FAIL

L:
	ldsfld	float64 _ble::_min
	ldsfld	float64 _ble::_one
	ble M
	br FAIL

M:
	ldsfld	float64 _ble::_min
	ldsfld	float64 _ble::_zero
	ble N
	br FAIL

N:
	ldsfld	float64 _ble::_min
	ldsfld	float64 _ble::zero
	ble O
	br FAIL

O:
	ldsfld	float64 _ble::_min
	ldsfld	float64 _ble::one
	ble P
	br FAIL

P:
	ldsfld	float64 _ble::_min
	ldsfld	float64 _ble::max
	ble Q
	br FAIL

Q:
	ldsfld	float64 _ble::_min
	ldsfld	float64 _ble::inf
	ble R
	br FAIL

R:
	ldsfld	float64 _ble::_min
	ldsfld	float64 _ble::NaN
	ble FAIL

S:
	ldsfld	float64 _ble::_one
	ldsfld	float64 _ble::_inf
	ble FAIL

	ldsfld	float64 _ble::_one
	ldsfld	float64 _ble::_min
	ble FAIL

	ldsfld	float64 _ble::_one
	ldsfld	float64 _ble::_one
	ble T
	br FAIL

T:
	ldsfld	float64 _ble::_one
	ldsfld	float64 _ble::_zero
	ble U
	br FAIL

U:
	ldsfld	float64 _ble::_one
	ldsfld	float64 _ble::zero
	ble V
	br FAIL

V:
	ldsfld	float64 _ble::_one
	ldsfld	float64 _ble::one
	ble W
	br FAIL

W:
	ldsfld	float64 _ble::_one
	ldsfld	float64 _ble::max
	ble X
	br FAIL

X:
	ldsfld	float64 _ble::_one
	ldsfld	float64 _ble::inf
	ble Y
	br FAIL

Y:
	ldsfld	float64 _ble::_one
	ldsfld	float64 _ble::NaN
	ble FAIL
Z:
	ldsfld	float64 _ble::_zero
	ldsfld	float64 _ble::_inf
	ble FAIL

	ldsfld	float64 _ble::_zero
	ldsfld	float64 _ble::_min
	ble FAIL

	ldsfld	float64 _ble::_zero
	ldsfld	float64 _ble::_one
	ble FAIL

	ldsfld	float64 _ble::_zero
	ldsfld	float64 _ble::_zero
	ble AA
	br FAIL

AA:
	ldsfld	float64 _ble::_zero
	ldsfld	float64 _ble::zero
	ble BB
	br FAIL

BB:
	ldsfld	float64 _ble::_zero
	ldsfld	float64 _ble::one
	ble CC
	br FAIL

CC:
	ldsfld	float64 _ble::_zero
	ldsfld	float64 _ble::max
	ble DD
	br FAIL

DD:
	ldsfld	float64 _ble::_zero
	ldsfld	float64 _ble::inf
	ble EE
	br FAIL

EE:
	ldsfld	float64 _ble::_zero
	ldsfld	float64 _ble::NaN
	ble FAIL

FF:
	ldsfld	float64 _ble::zero
	ldsfld	float64 _ble::_inf
	ble FAIL

	ldsfld	float64 _ble::zero
	ldsfld	float64 _ble::_min
	ble FAIL

	ldsfld	float64 _ble::zero
	ldsfld	float64 _ble::_one
	ble FAIL

	ldsfld	float64 _ble::zero
	ldsfld	float64 _ble::_zero
	ble GG
	br FAIL

GG:
	ldsfld	float64 _ble::zero
	ldsfld	float64 _ble::zero
	ble HH
	br FAIL

HH:
	ldsfld	float64 _ble::zero
	ldsfld	float64 _ble::one
	ble II
	br FAIL

II:
	ldsfld	float64 _ble::zero
	ldsfld	float64 _ble::max
	ble JJ
	br FAIL

JJ:
	ldsfld	float64 _ble::zero
	ldsfld	float64 _ble::inf
	ble KK
	br FAIL

KK:
	ldsfld	float64 _ble::zero
	ldsfld	float64 _ble::NaN
	ble FAIL

LL:
	ldsfld	float64 _ble::one
	ldsfld	float64 _ble::_inf
	ble FAIL

	ldsfld	float64 _ble::one
	ldsfld	float64 _ble::_min
	ble FAIL

	ldsfld	float64 _ble::one
	ldsfld	float64 _ble::_one
	ble FAIL

	ldsfld	float64 _ble::one
	ldsfld	float64 _ble::_zero
	ble FAIL

	ldsfld	float64 _ble::one
	ldsfld	float64 _ble::zero
	ble FAIL

	ldsfld	float64 _ble::one
	ldsfld	float64 _ble::one
	ble MM
	br FAIL

MM:
	ldsfld	float64 _ble::one
	ldsfld	float64 _ble::max
	ble NN
	br FAIL

NN:
	ldsfld	float64 _ble::one
	ldsfld	float64 _ble::inf
	ble OO
	br FAIL

OO:
	ldsfld	float64 _ble::one
	ldsfld	float64 _ble::NaN
	ble FAIL

PP:
	ldsfld	float64 _ble::max
	ldsfld	float64 _ble::_inf
	ble FAIL

	ldsfld	float64 _ble::max
	ldsfld	float64 _ble::_min
	ble FAIL

	ldsfld	float64 _ble::max
	ldsfld	float64 _ble::_one
	ble FAIL

	ldsfld	float64 _ble::max
	ldsfld	float64 _ble::_zero
	ble FAIL

	ldsfld	float64 _ble::max
	ldsfld	float64 _ble::zero
	ble FAIL

	ldsfld	float64 _ble::max
	ldsfld	float64 _ble::one
	ble FAIL

	ldsfld	float64 _ble::max
	ldsfld	float64 _ble::max
	ble QQ
	br FAIL

QQ:
	ldsfld	float64 _ble::max
	ldsfld	float64 _ble::inf
	ble RR
	br FAIL

RR:
	ldsfld	float64 _ble::max
	ldsfld	float64 _ble::NaN
	ble FAIL

SS:
	ldsfld	float64 _ble::inf
	ldsfld	float64 _ble::_inf
	ble FAIL

	ldsfld	float64 _ble::inf
	ldsfld	float64 _ble::_min
	ble FAIL

	ldsfld	float64 _ble::inf
	ldsfld	float64 _ble::_one
	ble FAIL

	ldsfld	float64 _ble::inf
	ldsfld	float64 _ble::_zero
	ble FAIL

	ldsfld	float64 _ble::inf
	ldsfld	float64 _ble::zero
	ble FAIL

	ldsfld	float64 _ble::inf
	ldsfld	float64 _ble::one
	ble FAIL

	ldsfld	float64 _ble::inf
	ldsfld	float64 _ble::max
	ble FAIL

	ldsfld	float64 _ble::inf
	ldsfld	float64 _ble::inf
	ble TT
	br FAIL

TT:
	ldsfld	float64 _ble::inf
	ldsfld	float64 _ble::NaN
	ble FAIL

UU:
	ldsfld	float64 _ble::NaN
	ldsfld	float64 _ble::_inf
	ble FAIL

VV:
	ldsfld	float64 _ble::NaN
	ldsfld	float64 _ble::_min
	ble FAIL

WW:
	ldsfld	float64 _ble::NaN
	ldsfld	float64 _ble::_one
	ble FAIL

XX:
	ldsfld	float64 _ble::NaN
	ldsfld	float64 _ble::_zero
	ble FAIL

YY:
	ldsfld	float64 _ble::NaN
	ldsfld	float64 _ble::zero
	ble FAIL

ZZ:
	ldsfld	float64 _ble::NaN
	ldsfld	float64 _ble::one
	ble FAIL

AAA:
	ldsfld	float64 _ble::NaN
	ldsfld	float64 _ble::max
	ble FAIL

BBB:
	ldsfld	float64 _ble::NaN
	ldsfld	float64 _ble::inf
	ble FAIL

CCC:
	ldsfld	float64 _ble::NaN
	ldsfld	float64 _ble::NaN
	ble  FAIL
	br BACKCHECK

TOPASS:
	br PASS
BACKCHECK:
	ldc.r8 0x0
	ldc.r8 0x1
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
.assembly ble_r8.exe{}
