// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public bne_un {

.field public static	float32 _inf
.field public static	float32 _min
.field public static	float32 _one
.field public static	float32 _zero
.field public static	float32 zero
.field public static	float32 one
.field public static	float32 max
.field public static	float32 inf
.field public static	float32 NaN
.method public static void initialize() {
.maxstack		10
	ldc.r4		float32(0xFF800000)
	stsfld	float32 bne_un::_inf
	ldc.r4		float32(0xFF7FFFFF)
	stsfld	float32 bne_un::_min
	ldc.r4		float32(0xBF800000)
	stsfld	float32 bne_un::_one
	ldc.r4		float32(0x80000000)
	stsfld	float32 bne_un::_zero
	ldc.r4		float32(0x00000000)
	stsfld	float32 bne_un::zero
	ldc.r4		float32(0x3F800000)
	stsfld	float32 bne_un::one
	ldc.r4		float32(0x7F7FFFFF)
	stsfld	float32 bne_un::max
	ldc.r4		float32(0x7F800000)
	stsfld	float32 bne_un::inf
	ldc.r4		float32(0x7FC00000)
	stsfld	float32 bne_un::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void bne_un::initialize()
	ldsfld	float32 bne_un::_inf
	ldsfld	float32 bne_un::_inf
	bne.un FAIL

	ldsfld	float32 bne_un::_inf
	ldsfld	float32 bne_un::_min
	bne.un A
	br FAIL

A:
	ldsfld	float32 bne_un::_inf
	ldsfld	float32 bne_un::_one
	bne.un B
	br FAIL

B:
	ldsfld	float32 bne_un::_inf
	ldsfld	float32 bne_un::_zero
	bne.un C
	br FAIL

C:
	ldsfld	float32 bne_un::_inf
	ldsfld	float32 bne_un::zero
	bne.un D
	br FAIL

D:
	ldsfld	float32 bne_un::_inf
	ldsfld	float32 bne_un::one
	bne.un E
	br FAIL

E:
	ldsfld	float32 bne_un::_inf
	ldsfld	float32 bne_un::max
	bne.un F
	br FAIL

F:
	ldsfld	float32 bne_un::_inf
	ldsfld	float32 bne_un::inf
	bne.un G
	br FAIL

G:
	ldsfld	float32 bne_un::_inf
	ldsfld	float32 bne_un::NaN
	bne.un H
	br FAIL

H:
	ldsfld	float32 bne_un::_min
	ldsfld	float32 bne_un::_inf
	bne.un I
	br FAIL

I:
	ldsfld	float32 bne_un::_min
	ldsfld	float32 bne_un::_min
	bne.un FAIL

	ldsfld	float32 bne_un::_min
	ldsfld	float32 bne_un::_one
	bne.un J
	br FAIL

J:
	ldsfld	float32 bne_un::_min
	ldsfld	float32 bne_un::_zero
	bne.un K
	br FAIL

K:
	ldsfld	float32 bne_un::_min
	ldsfld	float32 bne_un::zero
	bne.un L
	br		FAIL

L:
	ldsfld	float32 bne_un::_min
	ldsfld	float32 bne_un::one
	bne.un M
	br		FAIL

M:
	ldsfld	float32 bne_un::_min
	ldsfld	float32 bne_un::max
	bne.un N
	br		FAIL

N:
	ldsfld	float32 bne_un::_min
	ldsfld	float32 bne_un::inf
	bne.un O
	br		FAIL

O:
	ldsfld	float32 bne_un::_min
	ldsfld	float32 bne_un::NaN
	bne.un P
	br		FAIL
P:
	ldsfld	float32 bne_un::_one
	ldsfld	float32 bne_un::_inf
	bne.un Q
	br		FAIL

Q:
	ldsfld	float32 bne_un::_one
	ldsfld	float32 bne_un::_min
	bne.un R
	br		FAIL

R:
	ldsfld	float32 bne_un::_one
	ldsfld	float32 bne_un::_one
	bne.un FAIL

S:
	ldsfld	float32 bne_un::_one
	ldsfld	float32 bne_un::_zero
	bne.un T
	br		FAIL

T:
	ldsfld	float32 bne_un::_one
	ldsfld	float32 bne_un::zero
	bne.un U
	br		FAIL

U:
	ldsfld	float32 bne_un::_one
	ldsfld	float32 bne_un::one
	bne.un V
	br		FAIL

V:
	ldsfld	float32 bne_un::_one
	ldsfld	float32 bne_un::max
	bne.un W
	br		FAIL

W:
	ldsfld	float32 bne_un::_one
	ldsfld	float32 bne_un::inf
	bne.un X
	br		FAIL

X:
	ldsfld	float32 bne_un::_one
	ldsfld	float32 bne_un::NaN
	bne.un Y
	br		FAIL
Y:
	ldsfld	float32 bne_un::_zero
	ldsfld	float32 bne_un::_inf
	bne.un Z
	br		FAIL

Z:
	ldsfld	float32 bne_un::_zero
	ldsfld	float32 bne_un::_min
	bne.un AA
	br		FAIL

AA:
	ldsfld	float32 bne_un::_zero
	ldsfld	float32 bne_un::_one
	bne.un BB
	br		FAIL

BB:
	ldsfld	float32 bne_un::_zero
	ldsfld	float32 bne_un::_zero
	bne.un FAIL

	ldsfld	float32 bne_un::_zero
	ldsfld	float32 bne_un::zero
	bne.un FAIL

	ldsfld	float32 bne_un::_zero
	ldsfld	float32 bne_un::one
	bne.un CC
	br		FAIL

CC:
	ldsfld	float32 bne_un::_zero
	ldsfld	float32 bne_un::max
	bne.un DD
	br		FAIL

DD:
	ldsfld	float32 bne_un::_zero
	ldsfld	float32 bne_un::inf
	bne.un EE
	br		FAIL

EE:
	ldsfld	float32 bne_un::_zero
	ldsfld	float32 bne_un::NaN
	bne.un FF
	br		FAIL
FF:
	ldsfld	float32 bne_un::zero
	ldsfld	float32 bne_un::_inf
	bne.un GG
	br		FAIL

GG:
	ldsfld	float32 bne_un::zero
	ldsfld	float32 bne_un::_min
	bne.un HH
	br		FAIL

HH:
	ldsfld	float32 bne_un::zero
	ldsfld	float32 bne_un::_one
	bne.un II
	br		FAIL

II:
	ldsfld	float32 bne_un::zero
	ldsfld	float32 bne_un::_zero
	bne.un FAIL

	ldsfld	float32 bne_un::zero
	ldsfld	float32 bne_un::zero
	bne.un FAIL

	ldsfld	float32 bne_un::zero
	ldsfld	float32 bne_un::one
	bne.un JJ
	br		FAIL

JJ:
	ldsfld	float32 bne_un::zero
	ldsfld	float32 bne_un::max
	bne.un KK
	br		FAIL

KK:
	ldsfld	float32 bne_un::zero
	ldsfld	float32 bne_un::inf
	bne.un LL
	br		FAIL

LL:
	ldsfld	float32 bne_un::zero
	ldsfld	float32 bne_un::NaN
	bne.un MM
	br		FAIL

MM:
	ldsfld	float32 bne_un::one
	ldsfld	float32 bne_un::_inf
	bne.un NN
	br		FAIL

NN:
	ldsfld	float32 bne_un::one
	ldsfld	float32 bne_un::_min
	bne.un OO
	br		FAIL

OO:
	ldsfld	float32 bne_un::one
	ldsfld	float32 bne_un::_one
	bne.un PP
	br		FAIL

PP:
	ldsfld	float32 bne_un::one
	ldsfld	float32 bne_un::_zero
	bne.un QQ
	br		FAIL

QQ:
	ldsfld	float32 bne_un::one
	ldsfld	float32 bne_un::zero
	bne.un RR
	br		FAIL

RR:
	ldsfld	float32 bne_un::one
	ldsfld	float32 bne_un::one
	bne.un FAIL

SS:
	ldsfld	float32 bne_un::one
	ldsfld	float32 bne_un::max
	bne.un TT
	br		FAIL

TT:
	ldsfld	float32 bne_un::one
	ldsfld	float32 bne_un::inf
	bne.un UU
	br		FAIL

UU:
	ldsfld	float32 bne_un::one
	ldsfld	float32 bne_un::NaN
	bne.un VV
	br		FAIL

VV:
	ldsfld	float32 bne_un::max
	ldsfld	float32 bne_un::_inf
	bne.un WW
	br		FAIL

WW:
	ldsfld	float32 bne_un::max
	ldsfld	float32 bne_un::_min
	bne.un XX
	br		FAIL

XX:
	ldsfld	float32 bne_un::max
	ldsfld	float32 bne_un::_one
	bne.un YY
	br		FAIL

YY:
	ldsfld	float32 bne_un::max
	ldsfld	float32 bne_un::_zero
	bne.un ZZ
	br		FAIL

ZZ:
	ldsfld	float32 bne_un::max
	ldsfld	float32 bne_un::zero
	bne.un AAA
	br		FAIL

AAA:
	ldsfld	float32 bne_un::max
	ldsfld	float32 bne_un::one
	bne.un BBB
	br		FAIL

BBB:
	ldsfld	float32 bne_un::max
	ldsfld	float32 bne_un::max
	bne.un FAIL

	ldsfld	float32 bne_un::max
	ldsfld	float32 bne_un::inf
	bne.un CCC
	br		FAIL

CCC:
	ldsfld	float32 bne_un::max
	ldsfld	float32 bne_un::NaN
	bne.un DDD
	br	FAIL

DDD:
	ldsfld	float32 bne_un::inf
	ldsfld	float32 bne_un::_inf
	bne.un EEE
	br		FAIL

EEE:
	ldsfld	float32 bne_un::inf
	ldsfld	float32 bne_un::_min
	bne.un FFF
	br		FAIL

FFF:
	ldsfld	float32 bne_un::inf
	ldsfld	float32 bne_un::_one
	bne.un GGG
	br		FAIL

GGG:
	ldsfld	float32 bne_un::inf
	ldsfld	float32 bne_un::_zero
	bne.un HHH
	br		FAIL

HHH:
	ldsfld	float32 bne_un::inf
	ldsfld	float32 bne_un::zero
	bne.un III
	br		FAIL

III:
	ldsfld	float32 bne_un::inf
	ldsfld	float32 bne_un::one
	bne.un JJJ
	br		FAIL

JJJ:
	ldsfld	float32 bne_un::inf
	ldsfld	float32 bne_un::max
	bne.un KK_
	br		FAIL

KK_:
	ldsfld	float32 bne_un::inf
	ldsfld	float32 bne_un::inf
	bne.un FAIL

	ldsfld	float32 bne_un::inf
	ldsfld	float32 bne_un::NaN
	bne.un LLL
	br		FAIL

LLL:
	ldsfld	float32 bne_un::NaN
	ldsfld	float32 bne_un::_inf
	bne.un MMM
	br		FAIL

MMM:
	ldsfld	float32 bne_un::NaN
	ldsfld	float32 bne_un::_min
	bne.un NNN
	br		FAIL

NNN:
	ldsfld	float32 bne_un::NaN
	ldsfld	float32 bne_un::_one
	bne.un OOO
	br		FAIL

OOO:
	ldsfld	float32 bne_un::NaN
	ldsfld	float32 bne_un::_zero
	bne.un PPP
	br		FAIL

PPP:
	ldsfld	float32 bne_un::NaN
	ldsfld	float32 bne_un::zero
	bne.un QQQ
	br		FAIL

QQQ:
	ldsfld	float32 bne_un::NaN
	ldsfld	float32 bne_un::one
	bne.un RRR
	br		FAIL

RRR:
	ldsfld	float32 bne_un::NaN
	ldsfld	float32 bne_un::max
	bne.un SSS
	br		FAIL

SSS:
	ldsfld	float32 bne_un::NaN
	ldsfld	float32 bne_un::inf
	bne.un TTT
	br		FAIL

TTT:
	ldsfld	float32 bne_un::NaN
	ldsfld	float32 bne_un::NaN
	bne.un BACKCHECK
	br FAIL

TOPASS:
	br PASS
BACKCHECK:
	ldc.r4 0x0
	ldc.r4 0x1
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
.assembly bne_un_r4.exe{}
