// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public bne_un {

.field public static	float64 _inf
.field public static	float64 _min
.field public static	float64 _one
.field public static	float64 _zero
.field public static	float64 zero
.field public static	float64 one
.field public static	float64 max
.field public static	float64 inf
.field public static	float64 NaN
.method public static void initialize() {
.maxstack		10
	ldc.r8		float64(0xFFF0000000000000)
	stsfld	float64 bne_un::_inf
	ldc.r8		float64(0xFF7FFFFFFFFFFFFF)
	stsfld	float64 bne_un::_min
	ldc.r8		float64(0xBFF0000000000000)
	stsfld	float64 bne_un::_one
	ldc.r8		float64(0x8000000000000000)
	stsfld	float64 bne_un::_zero
	ldc.r8		float64(0x0000000000000000)
	stsfld	float64 bne_un::zero
	ldc.r8		float64(0x3FF0000000000000)
	stsfld	float64 bne_un::one
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	stsfld	float64 bne_un::max
	ldc.r8		float64(0x7FF0000000000000)
	stsfld	float64 bne_un::inf
	ldc.r8		float64(0x7FF8000000000000)
	stsfld	float64 bne_un::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void bne_un::initialize()
	ldsfld	float64 bne_un::_inf
	ldsfld	float64 bne_un::_inf
	bne.un FAIL

	ldsfld	float64 bne_un::_inf
	ldsfld	float64 bne_un::_min
	bne.un A
	br FAIL

A:
	ldsfld	float64 bne_un::_inf
	ldsfld	float64 bne_un::_one
	bne.un B
	br FAIL

B:
	ldsfld	float64 bne_un::_inf
	ldsfld	float64 bne_un::_zero
	bne.un C
	br FAIL

C:
	ldsfld	float64 bne_un::_inf
	ldsfld	float64 bne_un::zero
	bne.un D
	br FAIL

D:
	ldsfld	float64 bne_un::_inf
	ldsfld	float64 bne_un::one
	bne.un E
	br FAIL

E:
	ldsfld	float64 bne_un::_inf
	ldsfld	float64 bne_un::max
	bne.un F
	br FAIL

F:
	ldsfld	float64 bne_un::_inf
	ldsfld	float64 bne_un::inf
	bne.un G
	br FAIL

G:
	ldsfld	float64 bne_un::_inf
	ldsfld	float64 bne_un::NaN
	bne.un H
	br FAIL

H:
	ldsfld	float64 bne_un::_min
	ldsfld	float64 bne_un::_inf
	bne.un I
	br FAIL

I:
	ldsfld	float64 bne_un::_min
	ldsfld	float64 bne_un::_min
	bne.un FAIL

	ldsfld	float64 bne_un::_min
	ldsfld	float64 bne_un::_one
	bne.un J
	br FAIL

J:
	ldsfld	float64 bne_un::_min
	ldsfld	float64 bne_un::_zero
	bne.un K
	br FAIL

K:
	ldsfld	float64 bne_un::_min
	ldsfld	float64 bne_un::zero
	bne.un L
	br		FAIL

L:
	ldsfld	float64 bne_un::_min
	ldsfld	float64 bne_un::one
	bne.un M
	br		FAIL

M:
	ldsfld	float64 bne_un::_min
	ldsfld	float64 bne_un::max
	bne.un N
	br		FAIL

N:
	ldsfld	float64 bne_un::_min
	ldsfld	float64 bne_un::inf
	bne.un O
	br		FAIL

O:
	ldsfld	float64 bne_un::_min
	ldsfld	float64 bne_un::NaN
	bne.un P
	br		FAIL
P:
	ldsfld	float64 bne_un::_one
	ldsfld	float64 bne_un::_inf
	bne.un Q
	br		FAIL

Q:
	ldsfld	float64 bne_un::_one
	ldsfld	float64 bne_un::_min
	bne.un R
	br		FAIL

R:
	ldsfld	float64 bne_un::_one
	ldsfld	float64 bne_un::_one
	bne.un FAIL

S:
	ldsfld	float64 bne_un::_one
	ldsfld	float64 bne_un::_zero
	bne.un T
	br		FAIL

T:
	ldsfld	float64 bne_un::_one
	ldsfld	float64 bne_un::zero
	bne.un U
	br		FAIL

U:
	ldsfld	float64 bne_un::_one
	ldsfld	float64 bne_un::one
	bne.un V
	br		FAIL

V:
	ldsfld	float64 bne_un::_one
	ldsfld	float64 bne_un::max
	bne.un W
	br		FAIL

W:
	ldsfld	float64 bne_un::_one
	ldsfld	float64 bne_un::inf
	bne.un X
	br		FAIL

X:
	ldsfld	float64 bne_un::_one
	ldsfld	float64 bne_un::NaN
	bne.un Y
	br		FAIL
Y:
	ldsfld	float64 bne_un::_zero
	ldsfld	float64 bne_un::_inf
	bne.un Z
	br		FAIL

Z:
	ldsfld	float64 bne_un::_zero
	ldsfld	float64 bne_un::_min
	bne.un AA
	br		FAIL

AA:
	ldsfld	float64 bne_un::_zero
	ldsfld	float64 bne_un::_one
	bne.un BB
	br		FAIL

BB:
	ldsfld	float64 bne_un::_zero
	ldsfld	float64 bne_un::_zero
	bne.un FAIL

	ldsfld	float64 bne_un::_zero
	ldsfld	float64 bne_un::zero
	bne.un FAIL

	ldsfld	float64 bne_un::_zero
	ldsfld	float64 bne_un::one
	bne.un CC
	br		FAIL

CC:
	ldsfld	float64 bne_un::_zero
	ldsfld	float64 bne_un::max
	bne.un DD
	br		FAIL

DD:
	ldsfld	float64 bne_un::_zero
	ldsfld	float64 bne_un::inf
	bne.un EE
	br		FAIL

EE:
	ldsfld	float64 bne_un::_zero
	ldsfld	float64 bne_un::NaN
	bne.un FF
	br		FAIL
FF:
	ldsfld	float64 bne_un::zero
	ldsfld	float64 bne_un::_inf
	bne.un GG
	br		FAIL

GG:
	ldsfld	float64 bne_un::zero
	ldsfld	float64 bne_un::_min
	bne.un HH
	br		FAIL

HH:
	ldsfld	float64 bne_un::zero
	ldsfld	float64 bne_un::_one
	bne.un II
	br		FAIL

II:
	ldsfld	float64 bne_un::zero
	ldsfld	float64 bne_un::_zero
	bne.un FAIL

	ldsfld	float64 bne_un::zero
	ldsfld	float64 bne_un::zero
	bne.un FAIL

	ldsfld	float64 bne_un::zero
	ldsfld	float64 bne_un::one
	bne.un JJ
	br		FAIL

JJ:
	ldsfld	float64 bne_un::zero
	ldsfld	float64 bne_un::max
	bne.un KK
	br		FAIL

KK:
	ldsfld	float64 bne_un::zero
	ldsfld	float64 bne_un::inf
	bne.un LL
	br		FAIL

LL:
	ldsfld	float64 bne_un::zero
	ldsfld	float64 bne_un::NaN
	bne.un MM
	br		FAIL

MM:
	ldsfld	float64 bne_un::one
	ldsfld	float64 bne_un::_inf
	bne.un NN
	br		FAIL

NN:
	ldsfld	float64 bne_un::one
	ldsfld	float64 bne_un::_min
	bne.un OO
	br		FAIL

OO:
	ldsfld	float64 bne_un::one
	ldsfld	float64 bne_un::_one
	bne.un PP
	br		FAIL

PP:
	ldsfld	float64 bne_un::one
	ldsfld	float64 bne_un::_zero
	bne.un QQ
	br		FAIL

QQ:
	ldsfld	float64 bne_un::one
	ldsfld	float64 bne_un::zero
	bne.un RR
	br		FAIL

RR:
	ldsfld	float64 bne_un::one
	ldsfld	float64 bne_un::one
	bne.un FAIL

SS:
	ldsfld	float64 bne_un::one
	ldsfld	float64 bne_un::max
	bne.un TT
	br		FAIL

TT:
	ldsfld	float64 bne_un::one
	ldsfld	float64 bne_un::inf
	bne.un UU
	br		FAIL

UU:
	ldsfld	float64 bne_un::one
	ldsfld	float64 bne_un::NaN
	bne.un VV
	br		FAIL

VV:
	ldsfld	float64 bne_un::max
	ldsfld	float64 bne_un::_inf
	bne.un WW
	br		FAIL

WW:
	ldsfld	float64 bne_un::max
	ldsfld	float64 bne_un::_min
	bne.un XX
	br		FAIL

XX:
	ldsfld	float64 bne_un::max
	ldsfld	float64 bne_un::_one
	bne.un YY
	br		FAIL

YY:
	ldsfld	float64 bne_un::max
	ldsfld	float64 bne_un::_zero
	bne.un ZZ
	br		FAIL

ZZ:
	ldsfld	float64 bne_un::max
	ldsfld	float64 bne_un::zero
	bne.un AAA
	br		FAIL

AAA:
	ldsfld	float64 bne_un::max
	ldsfld	float64 bne_un::one
	bne.un BBB
	br		FAIL

BBB:
	ldsfld	float64 bne_un::max
	ldsfld	float64 bne_un::max
	bne.un FAIL

	ldsfld	float64 bne_un::max
	ldsfld	float64 bne_un::inf
	bne.un CCC
	br		FAIL

CCC:
	ldsfld	float64 bne_un::max
	ldsfld	float64 bne_un::NaN
	bne.un DDD
	br	FAIL

DDD:
	ldsfld	float64 bne_un::inf
	ldsfld	float64 bne_un::_inf
	bne.un EEE
	br		FAIL

EEE:
	ldsfld	float64 bne_un::inf
	ldsfld	float64 bne_un::_min
	bne.un FFF
	br		FAIL

FFF:
	ldsfld	float64 bne_un::inf
	ldsfld	float64 bne_un::_one
	bne.un GGG
	br		FAIL

GGG:
	ldsfld	float64 bne_un::inf
	ldsfld	float64 bne_un::_zero
	bne.un HHH
	br		FAIL

HHH:
	ldsfld	float64 bne_un::inf
	ldsfld	float64 bne_un::zero
	bne.un III
	br		FAIL

III:
	ldsfld	float64 bne_un::inf
	ldsfld	float64 bne_un::one
	bne.un JJJ
	br		FAIL

JJJ:
	ldsfld	float64 bne_un::inf
	ldsfld	float64 bne_un::max
	bne.un KK_
	br		FAIL

KK_:
	ldsfld	float64 bne_un::inf
	ldsfld	float64 bne_un::inf
	bne.un FAIL

	ldsfld	float64 bne_un::inf
	ldsfld	float64 bne_un::NaN
	bne.un LLL
	br		FAIL

LLL:
	ldsfld	float64 bne_un::NaN
	ldsfld	float64 bne_un::_inf
	bne.un MMM
	br		FAIL

MMM:
	ldsfld	float64 bne_un::NaN
	ldsfld	float64 bne_un::_min
	bne.un NNN
	br		FAIL

NNN:
	ldsfld	float64 bne_un::NaN
	ldsfld	float64 bne_un::_one
	bne.un OOO
	br		FAIL

OOO:
	ldsfld	float64 bne_un::NaN
	ldsfld	float64 bne_un::_zero
	bne.un PPP
	br		FAIL

PPP:
	ldsfld	float64 bne_un::NaN
	ldsfld	float64 bne_un::zero
	bne.un QQQ
	br		FAIL

QQQ:
	ldsfld	float64 bne_un::NaN
	ldsfld	float64 bne_un::one
	bne.un RRR
	br		FAIL

RRR:
	ldsfld	float64 bne_un::NaN
	ldsfld	float64 bne_un::max
	bne.un SSS
	br		FAIL

SSS:
	ldsfld	float64 bne_un::NaN
	ldsfld	float64 bne_un::inf
	bne.un TTT
	br		FAIL

TTT:
	ldsfld	float64 bne_un::NaN
	ldsfld	float64 bne_un::NaN
	bne.un BACKCHECK
	br FAIL

TOPASS:
	br PASS
BACKCHECK:
	ldc.r8 0x0
	ldc.r8 0x1
	bne.un TOPASS
	br FAIL


PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly bne_un_r8.exe{}
