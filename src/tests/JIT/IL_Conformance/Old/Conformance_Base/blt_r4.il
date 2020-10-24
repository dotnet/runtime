// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public _blt {

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
	stsfld	float32 _blt::_inf
	ldc.r4		float32(0xFF7FFFFF)
	stsfld	float32 _blt::_min
	ldc.r4		float32(0xBF800000)
	stsfld	float32 _blt::_one
	ldc.r4		float32(0x80000000)
	stsfld	float32 _blt::_zero
	ldc.r4		float32(0x00000000)
	stsfld	float32 _blt::zero
	ldc.r4		float32(0x3F800000)
	stsfld	float32 _blt::one
	ldc.r4		float32(0x7F7FFFFF)
	stsfld	float32 _blt::max
	ldc.r4		float32(0x7F800000)
	stsfld	float32 _blt::inf
	ldc.r4		float32(0x7FC00000)
	stsfld	float32 _blt::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void _blt::initialize()

	ldsfld	float32 _blt::_inf
	ldsfld	float32 _blt::_inf
	blt FAIL

A:
	ldsfld	float32 _blt::_inf
	ldsfld	float32 _blt::_min
	blt B
	br FAIL

B:
	ldsfld	float32 _blt::_inf
	ldsfld	float32 _blt::_one
	blt C
	br FAIL

C:
	ldsfld	float32 _blt::_inf
	ldsfld	float32 _blt::_zero
	blt D
	br FAIL

D:
	ldsfld	float32 _blt::_inf
	ldsfld	float32 _blt::zero
	blt E
	br FAIL

E:
	ldsfld	float32 _blt::_inf
	ldsfld	float32 _blt::one
	blt F
	br FAIL

F:
	ldsfld	float32 _blt::_inf
	ldsfld	float32 _blt::max
	blt G
	br FAIL

G:
	ldsfld	float32 _blt::_inf
	ldsfld	float32 _blt::inf
	blt H
	br FAIL

H:
	ldsfld	float32 _blt::_inf
	ldsfld	float32 _blt::NaN
	blt	FAIL
K:
	ldsfld	float32 _blt::_min
	ldsfld	float32 _blt::_inf
	blt FAIL

	ldsfld	float32 _blt::_min
	ldsfld	float32 _blt::_min
	blt FAIL

L:
	ldsfld	float32 _blt::_min
	ldsfld	float32 _blt::_one
	blt M
	br FAIL

M:
	ldsfld	float32 _blt::_min
	ldsfld	float32 _blt::_zero
	blt N
	br FAIL

N:
	ldsfld	float32 _blt::_min
	ldsfld	float32 _blt::zero
	blt O
	br FAIL

O:
	ldsfld	float32 _blt::_min
	ldsfld	float32 _blt::one
	blt P
	br FAIL

P:
	ldsfld	float32 _blt::_min
	ldsfld	float32 _blt::max
	blt Q
	br FAIL

Q:
	ldsfld	float32 _blt::_min
	ldsfld	float32 _blt::inf
	blt R
	br FAIL

R:
	ldsfld	float32 _blt::_min
	ldsfld	float32 _blt::NaN
	blt	FAIL


S:
	ldsfld	float32 _blt::_one
	ldsfld	float32 _blt::_inf
	blt FAIL

	ldsfld	float32 _blt::_one
	ldsfld	float32 _blt::_min
	blt FAIL

	ldsfld	float32 _blt::_one
	ldsfld	float32 _blt::_one
	blt FAIL

T:
	ldsfld	float32 _blt::_one
	ldsfld	float32 _blt::_zero
	blt U
	br FAIL

U:
	ldsfld	float32 _blt::_one
	ldsfld	float32 _blt::zero
	blt V
	br FAIL

V:
	ldsfld	float32 _blt::_one
	ldsfld	float32 _blt::one
	blt W
	br FAIL

W:
	ldsfld	float32 _blt::_one
	ldsfld	float32 _blt::max
	blt X
	br FAIL

X:
	ldsfld	float32 _blt::_one
	ldsfld	float32 _blt::inf
	blt Y
	br FAIL

Y:
	ldsfld	float32 _blt::_one
	ldsfld	float32 _blt::NaN
	blt	FAIL
Z:
	ldsfld	float32 _blt::_zero
	ldsfld	float32 _blt::_inf
	blt FAIL

	ldsfld	float32 _blt::_zero
	ldsfld	float32 _blt::_min
	blt FAIL

	ldsfld	float32 _blt::_zero
	ldsfld	float32 _blt::_one
	blt FAIL

	ldsfld	float32 _blt::_zero
	ldsfld	float32 _blt::_zero
	blt FAIL

AA:
	ldsfld	float32 _blt::_zero
	ldsfld	float32 _blt::zero
	blt FAIL

BB:
	ldsfld	float32 _blt::_zero
	ldsfld	float32 _blt::one
	blt CC
	br FAIL

CC:
	ldsfld	float32 _blt::_zero
	ldsfld	float32 _blt::max
	blt DD
	br FAIL

DD:
	ldsfld	float32 _blt::_zero
	ldsfld	float32 _blt::inf
	blt EE
	br FAIL

EE:
	ldsfld	float32 _blt::_zero
	ldsfld	float32 _blt::NaN
	blt	FAIL

FF:
	ldsfld	float32 _blt::zero
	ldsfld	float32 _blt::_inf
	blt FAIL

	ldsfld	float32 _blt::zero
	ldsfld	float32 _blt::_min
	blt FAIL

	ldsfld	float32 _blt::zero
	ldsfld	float32 _blt::_one
	blt FAIL

	ldsfld	float32 _blt::zero
	ldsfld	float32 _blt::_zero
	blt FAIL

GG:
	ldsfld	float32 _blt::zero
	ldsfld	float32 _blt::zero
	blt FAIL

HH:
	ldsfld	float32 _blt::zero
	ldsfld	float32 _blt::one
	blt II
	br FAIL

II:
	ldsfld	float32 _blt::zero
	ldsfld	float32 _blt::max
	blt JJ
	br FAIL

JJ:
	ldsfld	float32 _blt::zero
	ldsfld	float32 _blt::inf
	blt KK
	br FAIL

KK:
	ldsfld	float32 _blt::zero
	ldsfld	float32 _blt::NaN
	blt	FAIL

LL:
	ldsfld	float32 _blt::one
	ldsfld	float32 _blt::_inf
	blt FAIL

	ldsfld	float32 _blt::one
	ldsfld	float32 _blt::_min
	blt FAIL

	ldsfld	float32 _blt::one
	ldsfld	float32 _blt::_one
	blt FAIL

	ldsfld	float32 _blt::one
	ldsfld	float32 _blt::_zero
	blt FAIL

	ldsfld	float32 _blt::one
	ldsfld	float32 _blt::zero
	blt FAIL

	ldsfld	float32 _blt::one
	ldsfld	float32 _blt::one
	blt FAIL

MM:
	ldsfld	float32 _blt::one
	ldsfld	float32 _blt::max
	blt NN
	br FAIL

NN:
	ldsfld	float32 _blt::one
	ldsfld	float32 _blt::inf
	blt OO
	br FAIL

OO:
	ldsfld	float32 _blt::one
	ldsfld	float32 _blt::NaN
	blt	FAIL

PP:
	ldsfld	float32 _blt::max
	ldsfld	float32 _blt::_inf
	blt FAIL

	ldsfld	float32 _blt::max
	ldsfld	float32 _blt::_min
	blt FAIL

	ldsfld	float32 _blt::max
	ldsfld	float32 _blt::_one
	blt FAIL

	ldsfld	float32 _blt::max
	ldsfld	float32 _blt::_zero
	blt FAIL

	ldsfld	float32 _blt::max
	ldsfld	float32 _blt::zero
	blt FAIL

	ldsfld	float32 _blt::max
	ldsfld	float32 _blt::one
	blt FAIL

	ldsfld	float32 _blt::max
	ldsfld	float32 _blt::max
	blt FAIL

QQ:
	ldsfld	float32 _blt::max
	ldsfld	float32 _blt::inf
	blt RR
	br FAIL

RR:
	ldsfld	float32 _blt::max
	ldsfld	float32 _blt::NaN
	blt	FAIL
SS:
	ldsfld	float32 _blt::inf
	ldsfld	float32 _blt::_inf
	blt FAIL

	ldsfld	float32 _blt::inf
	ldsfld	float32 _blt::_min
	blt FAIL

	ldsfld	float32 _blt::inf
	ldsfld	float32 _blt::_one
	blt FAIL

	ldsfld	float32 _blt::inf
	ldsfld	float32 _blt::_zero
	blt FAIL

	ldsfld	float32 _blt::inf
	ldsfld	float32 _blt::zero
	blt FAIL

	ldsfld	float32 _blt::inf
	ldsfld	float32 _blt::one
	blt FAIL

	ldsfld	float32 _blt::inf
	ldsfld	float32 _blt::max
	blt FAIL

	ldsfld	float32 _blt::inf
	ldsfld	float32 _blt::inf
	blt FAIL

TT:
	ldsfld	float32 _blt::inf
	ldsfld	float32 _blt::NaN
	blt	FAIL

UU:
	ldsfld	float32 _blt::NaN
	ldsfld	float32 _blt::_inf
	blt	FAIL

VV:
	ldsfld	float32 _blt::NaN
	ldsfld	float32 _blt::_min
	blt	FAIL

WW:
	ldsfld	float32 _blt::NaN
	ldsfld	float32 _blt::_one
	blt	FAIL

XX:
	ldsfld	float32 _blt::NaN
	ldsfld	float32 _blt::_zero
	blt	FAIL

YY:
	ldsfld	float32 _blt::NaN
	ldsfld	float32 _blt::zero
	blt	FAIL

ZZ:
	ldsfld	float32 _blt::NaN
	ldsfld	float32 _blt::one
	blt	FAIL

AAA:
	ldsfld	float32 _blt::NaN
	ldsfld	float32 _blt::max
	blt	FAIL

BBB:
	ldsfld	float32 _blt::NaN
	ldsfld	float32 _blt::inf
	blt	FAIL

CCC:
	ldsfld	float32 _blt::NaN
	ldsfld	float32 _blt::NaN
	blt	FAIL
	br	BACKCHECK

TOPASS:
	br PASS
BACKCHECK:
	ldc.r4 0x0
	ldc.r4 0x1
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
.assembly blt_r4.exe{}
