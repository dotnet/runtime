// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public _blt {

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
	stsfld	float64 _blt::_inf
	ldc.r8		float64(0xFF7FFFFFFFFFFFFF)
	stsfld	float64 _blt::_min
	ldc.r8		float64(0xBFF0000000000000)
	stsfld	float64 _blt::_one
	ldc.r8		float64(0x8000000000000000)
	stsfld	float64 _blt::_zero
	ldc.r8		float64(0x0000000000000000)
	stsfld	float64 _blt::zero
	ldc.r8		float64(0x3FF0000000000000)
	stsfld	float64 _blt::one
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	stsfld	float64 _blt::max
	ldc.r8		float64(0x7FF0000000000000)
	stsfld	float64 _blt::inf
	ldc.r8		float64(0x7FF8000000000000)
	stsfld	float64 _blt::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void _blt::initialize()

	ldsfld	float64 _blt::_inf
	ldsfld	float64 _blt::_inf
	blt FAIL

A:
	ldsfld	float64 _blt::_inf
	ldsfld	float64 _blt::_min
	blt B
	br FAIL

B:
	ldsfld	float64 _blt::_inf
	ldsfld	float64 _blt::_one
	blt C
	br FAIL

C:
	ldsfld	float64 _blt::_inf
	ldsfld	float64 _blt::_zero
	blt D
	br FAIL

D:
	ldsfld	float64 _blt::_inf
	ldsfld	float64 _blt::zero
	blt E
	br FAIL

E:
	ldsfld	float64 _blt::_inf
	ldsfld	float64 _blt::one
	blt F
	br FAIL

F:
	ldsfld	float64 _blt::_inf
	ldsfld	float64 _blt::max
	blt G
	br FAIL

G:
	ldsfld	float64 _blt::_inf
	ldsfld	float64 _blt::inf
	blt H
	br FAIL

H:
	ldsfld	float64 _blt::_inf
	ldsfld	float64 _blt::NaN
	blt	FAIL
K:
	ldsfld	float64 _blt::_min
	ldsfld	float64 _blt::_inf
	blt FAIL

	ldsfld	float64 _blt::_min
	ldsfld	float64 _blt::_min
	blt FAIL

L:
	ldsfld	float64 _blt::_min
	ldsfld	float64 _blt::_one
	blt M
	br FAIL

M:
	ldsfld	float64 _blt::_min
	ldsfld	float64 _blt::_zero
	blt N
	br FAIL

N:
	ldsfld	float64 _blt::_min
	ldsfld	float64 _blt::zero
	blt O
	br FAIL

O:
	ldsfld	float64 _blt::_min
	ldsfld	float64 _blt::one
	blt P
	br FAIL

P:
	ldsfld	float64 _blt::_min
	ldsfld	float64 _blt::max
	blt Q
	br FAIL

Q:
	ldsfld	float64 _blt::_min
	ldsfld	float64 _blt::inf
	blt R
	br FAIL

R:
	ldsfld	float64 _blt::_min
	ldsfld	float64 _blt::NaN
	blt	FAIL

S:
	ldsfld	float64 _blt::_one
	ldsfld	float64 _blt::_inf
	blt FAIL

	ldsfld	float64 _blt::_one
	ldsfld	float64 _blt::_min
	blt FAIL

	ldsfld	float64 _blt::_one
	ldsfld	float64 _blt::_one
	blt FAIL

T:
	ldsfld	float64 _blt::_one
	ldsfld	float64 _blt::_zero
	blt U
	br FAIL

U:
	ldsfld	float64 _blt::_one
	ldsfld	float64 _blt::zero
	blt V
	br FAIL

V:
	ldsfld	float64 _blt::_one
	ldsfld	float64 _blt::one
	blt W
	br FAIL

W:
	ldsfld	float64 _blt::_one
	ldsfld	float64 _blt::max
	blt X
	br FAIL

X:
	ldsfld	float64 _blt::_one
	ldsfld	float64 _blt::inf
	blt Y
	br FAIL

Y:
	ldsfld	float64 _blt::_one
	ldsfld	float64 _blt::NaN
	blt	FAIL
Z:
	ldsfld	float64 _blt::_zero
	ldsfld	float64 _blt::_inf
	blt FAIL

	ldsfld	float64 _blt::_zero
	ldsfld	float64 _blt::_min
	blt FAIL

	ldsfld	float64 _blt::_zero
	ldsfld	float64 _blt::_one
	blt FAIL

	ldsfld	float64 _blt::_zero
	ldsfld	float64 _blt::_zero
	blt FAIL

AA:
	ldsfld	float64 _blt::_zero
	ldsfld	float64 _blt::zero
	blt FAIL

BB:
	ldsfld	float64 _blt::_zero
	ldsfld	float64 _blt::one
	blt CC
	br FAIL

CC:
	ldsfld	float64 _blt::_zero
	ldsfld	float64 _blt::max
	blt DD
	br FAIL

DD:
	ldsfld	float64 _blt::_zero
	ldsfld	float64 _blt::inf
	blt EE
	br FAIL

EE:
	ldsfld	float64 _blt::_zero
	ldsfld	float64 _blt::NaN
	blt	FAIL

FF:
	ldsfld	float64 _blt::zero
	ldsfld	float64 _blt::_inf
	blt FAIL

	ldsfld	float64 _blt::zero
	ldsfld	float64 _blt::_min
	blt FAIL

	ldsfld	float64 _blt::zero
	ldsfld	float64 _blt::_one
	blt FAIL

	ldsfld	float64 _blt::zero
	ldsfld	float64 _blt::_zero
	blt FAIL

GG:
	ldsfld	float64 _blt::zero
	ldsfld	float64 _blt::zero
	blt FAIL

HH:
	ldsfld	float64 _blt::zero
	ldsfld	float64 _blt::one
	blt II
	br FAIL

II:
	ldsfld	float64 _blt::zero
	ldsfld	float64 _blt::max
	blt JJ
	br FAIL

JJ:
	ldsfld	float64 _blt::zero
	ldsfld	float64 _blt::inf
	blt KK
	br FAIL

KK:
	ldsfld	float64 _blt::zero
	ldsfld	float64 _blt::NaN
	blt	FAIL

LL:
	ldsfld	float64 _blt::one
	ldsfld	float64 _blt::_inf
	blt FAIL

	ldsfld	float64 _blt::one
	ldsfld	float64 _blt::_min
	blt FAIL

	ldsfld	float64 _blt::one
	ldsfld	float64 _blt::_one
	blt FAIL

	ldsfld	float64 _blt::one
	ldsfld	float64 _blt::_zero
	blt FAIL

	ldsfld	float64 _blt::one
	ldsfld	float64 _blt::zero
	blt FAIL

	ldsfld	float64 _blt::one
	ldsfld	float64 _blt::one
	blt FAIL

MM:
	ldsfld	float64 _blt::one
	ldsfld	float64 _blt::max
	blt NN
	br FAIL

NN:
	ldsfld	float64 _blt::one
	ldsfld	float64 _blt::inf
	blt OO
	br FAIL

OO:
	ldsfld	float64 _blt::one
	ldsfld	float64 _blt::NaN
	blt	FAIL

PP:
	ldsfld	float64 _blt::max
	ldsfld	float64 _blt::_inf
	blt FAIL

	ldsfld	float64 _blt::max
	ldsfld	float64 _blt::_min
	blt FAIL

	ldsfld	float64 _blt::max
	ldsfld	float64 _blt::_one
	blt FAIL

	ldsfld	float64 _blt::max
	ldsfld	float64 _blt::_zero
	blt FAIL

	ldsfld	float64 _blt::max
	ldsfld	float64 _blt::zero
	blt FAIL

	ldsfld	float64 _blt::max
	ldsfld	float64 _blt::one
	blt FAIL

	ldsfld	float64 _blt::max
	ldsfld	float64 _blt::max
	blt FAIL

QQ:
	ldsfld	float64 _blt::max
	ldsfld	float64 _blt::inf
	blt RR
	br FAIL

RR:
	ldsfld	float64 _blt::max
	ldsfld	float64 _blt::NaN
	blt	FAIL

SS:
	ldsfld	float64 _blt::inf
	ldsfld	float64 _blt::_inf
	blt FAIL

	ldsfld	float64 _blt::inf
	ldsfld	float64 _blt::_min
	blt FAIL

	ldsfld	float64 _blt::inf
	ldsfld	float64 _blt::_one
	blt FAIL

	ldsfld	float64 _blt::inf
	ldsfld	float64 _blt::_zero
	blt FAIL

	ldsfld	float64 _blt::inf
	ldsfld	float64 _blt::zero
	blt FAIL

	ldsfld	float64 _blt::inf
	ldsfld	float64 _blt::one
	blt FAIL

	ldsfld	float64 _blt::inf
	ldsfld	float64 _blt::max
	blt FAIL

	ldsfld	float64 _blt::inf
	ldsfld	float64 _blt::inf
	blt FAIL

TT:
	ldsfld	float64 _blt::inf
	ldsfld	float64 _blt::NaN
	blt	FAIL

UU:
	ldsfld	float64 _blt::NaN
	ldsfld	float64 _blt::_inf
	blt	FAIL

VV:
	ldsfld	float64 _blt::NaN
	ldsfld	float64 _blt::_min
	blt	FAIL

WW:
	ldsfld	float64 _blt::NaN
	ldsfld	float64 _blt::_one
	blt	FAIL

XX:
	ldsfld	float64 _blt::NaN
	ldsfld	float64 _blt::_zero
	blt	FAIL

YY:
	ldsfld	float64 _blt::NaN
	ldsfld	float64 _blt::zero
	blt	FAIL

ZZ:
	ldsfld	float64 _blt::NaN
	ldsfld	float64 _blt::one
	blt	FAIL

AAA:
	ldsfld	float64 _blt::NaN
	ldsfld	float64 _blt::max
	blt	FAIL

BBB:
	ldsfld	float64 _blt::NaN
	ldsfld	float64 _blt::inf
	blt	FAIL

CCC:
	ldsfld	float64 _blt::NaN
	ldsfld	float64 _blt::NaN
	blt	FAIL
	br	BACKCHECK

TOPASS:
	br PASS
BACKCHECK:
	ldc.r8 0x0
	ldc.r8 0x1
	blt TOPASS
	br FAIL


PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly blt_r8.exe{}
