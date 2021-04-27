// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

#define NEG_INF    "float64(0xFFF0000000000000)"
#define MIN        "float64(0xFF7FFFFFFFFFFFFF)"
#define MINUS_ONE  "float64(0xBFF0000000000000)"
#define MINUS_ZERO "float64(0x8000000000000000)"
#define ZERO       "float64(0x0000000000000000)"
#define ONE        "float64(0x3FF0000000000000)"
#define MAX        "float64(0x7FEFFFFFFFFFFFFF)"
#define POS_INF    "float64(0x7FF0000000000000)"
#define NAN        "float64(0x7FF8000000000000)"

.class public blt_un {

.field public static	float64 neg_inf
.field public static	float64 min
.field public static	float64 minus_one
.field public static	float64 minus_zero
.field public static	float64 zero
.field public static	float64 one
.field public static	float64 max
.field public static	float64 pos_inf
.field public static	float64 NaN

.method public static	void initialize() {
.maxstack	10
	ldc.r8		NEG_INF
	stsfld	float64 blt_un::neg_inf
	ldc.r8		MIN
	stsfld	float64 blt_un::min
	ldc.r8		MINUS_ONE
	stsfld	float64 blt_un::minus_one
	ldc.r8		MINUS_ZERO
	stsfld	float64 blt_un::minus_zero
	ldc.r8		ZERO
	stsfld	float64 blt_un::zero
	ldc.r8		ONE
	stsfld	float64 blt_un::one
	ldc.r8		MAX
	stsfld	float64 blt_un::max
	ldc.r8		POS_INF
	stsfld	float64 blt_un::pos_inf
	ldc.r8		NAN
	stsfld	float64 blt_un::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void blt_un::initialize()

	ldsfld	float64 blt_un::neg_inf
	ldsfld	float64 blt_un::neg_inf
	blt.un FAIL

A:
	ldsfld	float64 blt_un::neg_inf
	ldsfld	float64 blt_un::min
	blt.un B
	br FAIL

B:
	ldsfld	float64 blt_un::neg_inf
	ldsfld	float64 blt_un::minus_one
	blt.un C
	br FAIL

C:
	ldsfld	float64 blt_un::neg_inf
	ldsfld	float64 blt_un::minus_zero
	blt.un D
	br FAIL

D:
	ldsfld	float64 blt_un::neg_inf
	ldsfld	float64 blt_un::zero
	blt.un E
	br FAIL

E:
	ldsfld	float64 blt_un::neg_inf
	ldsfld	float64 blt_un::one
	blt.un F
	br FAIL

F:
	ldsfld	float64 blt_un::neg_inf
	ldsfld	float64 blt_un::max
	blt.un G
	br FAIL

G:
	ldsfld	float64 blt_un::neg_inf
	ldsfld	float64 blt_un::pos_inf
	blt.un H
	br FAIL

H:
	ldsfld	float64 blt_un::neg_inf
	ldsfld	float64 blt_un::NaN
	blt.un K
	br FAIL
K:
	ldsfld	float64 blt_un::min
	ldsfld	float64 blt_un::neg_inf
	blt.un FAIL

	ldsfld	float64 blt_un::min
	ldsfld	float64 blt_un::min
	blt.un FAIL

L:
	ldsfld	float64 blt_un::min
	ldsfld	float64 blt_un::minus_one
	blt.un M
	br FAIL

M:
	ldsfld	float64 blt_un::min
	ldsfld	float64 blt_un::minus_zero
	blt.un N
	br FAIL

N:
	ldsfld	float64 blt_un::min
	ldsfld	float64 blt_un::zero
	blt.un O
	br FAIL

O:
	ldsfld	float64 blt_un::min
	ldsfld	float64 blt_un::one
	blt.un P
	br FAIL

P:
	ldsfld	float64 blt_un::min
	ldsfld	float64 blt_un::max
	blt.un Q
	br FAIL

Q:
	ldsfld	float64 blt_un::min
	ldsfld	float64 blt_un::pos_inf
	blt.un R
	br FAIL

R:
	ldsfld	float64 blt_un::min
	ldsfld	float64 blt_un::NaN
	blt.un S
	br FAIL


S:
	ldsfld	float64 blt_un::minus_one
	ldsfld	float64 blt_un::neg_inf
	blt.un FAIL

	ldsfld	float64 blt_un::minus_one
	ldsfld	float64 blt_un::min
	blt.un FAIL

	ldsfld	float64 blt_un::minus_one
	ldsfld	float64 blt_un::minus_one
	blt.un FAIL

T:
	ldsfld	float64 blt_un::minus_one
	ldsfld	float64 blt_un::minus_zero
	blt.un U
	br FAIL

U:
	ldsfld	float64 blt_un::minus_one
	ldsfld	float64 blt_un::zero
	blt.un V
	br FAIL

V:
	ldsfld	float64 blt_un::minus_one
	ldsfld	float64 blt_un::one
	blt.un W
	br FAIL

W:
	ldsfld	float64 blt_un::minus_one
	ldsfld	float64 blt_un::max
	blt.un X
	br FAIL

X:
	ldsfld	float64 blt_un::minus_one
	ldsfld	float64 blt_un::pos_inf
	blt.un Y
	br FAIL

Y:
	ldsfld	float64 blt_un::minus_one
	ldsfld	float64 blt_un::NaN
	blt.un Z
	br FAIL
Z:
	ldsfld	float64 blt_un::minus_zero
	ldsfld	float64 blt_un::neg_inf
	blt.un FAIL

	ldsfld	float64 blt_un::minus_zero
	ldsfld	float64 blt_un::min
	blt.un FAIL

	ldsfld	float64 blt_un::minus_zero
	ldsfld	float64 blt_un::minus_one
	blt.un FAIL

	ldsfld	float64 blt_un::minus_zero
	ldsfld	float64 blt_un::minus_zero
	blt.un FAIL

AA:
	ldsfld	float64 blt_un::minus_zero
	ldsfld	float64 blt_un::zero
	blt.un FAIL

BB:
	ldsfld	float64 blt_un::minus_zero
	ldsfld	float64 blt_un::one
	blt.un CC
	br FAIL

CC:
	ldsfld	float64 blt_un::minus_zero
	ldsfld	float64 blt_un::max
	blt.un DD
	br FAIL

DD:
	ldsfld	float64 blt_un::minus_zero
	ldsfld	float64 blt_un::pos_inf
	blt.un EE
	br FAIL

EE:
	ldsfld	float64 blt_un::minus_zero
	ldsfld	float64 blt_un::NaN
	blt.un FF
	br FAIL

FF:
	ldsfld	float64 blt_un::zero
	ldsfld	float64 blt_un::neg_inf
	blt.un FAIL

	ldsfld	float64 blt_un::zero
	ldsfld	float64 blt_un::min
	blt.un FAIL

	ldsfld	float64 blt_un::zero
	ldsfld	float64 blt_un::minus_one
	blt.un FAIL

	ldsfld	float64 blt_un::zero
	ldsfld	float64 blt_un::minus_zero
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
	ldsfld	float64 blt_un::pos_inf
	blt.un KK
	br FAIL

KK:
	ldsfld	float64 blt_un::zero
	ldsfld	float64 blt_un::NaN
	blt.un LL
	br FAIL

LL:
	ldsfld	float64 blt_un::one
	ldsfld	float64 blt_un::neg_inf
	blt.un FAIL

	ldsfld	float64 blt_un::one
	ldsfld	float64 blt_un::min
	blt.un FAIL

	ldsfld	float64 blt_un::one
	ldsfld	float64 blt_un::minus_one
	blt.un FAIL

	ldsfld	float64 blt_un::one
	ldsfld	float64 blt_un::minus_zero
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
	ldsfld	float64 blt_un::pos_inf
	blt.un OO
	br FAIL

OO:
	ldsfld	float64 blt_un::one
	ldsfld	float64 blt_un::NaN
	blt.un PP
	br FAIL

PP:
	ldsfld	float64 blt_un::max
	ldsfld	float64 blt_un::neg_inf
	blt.un FAIL

	ldsfld	float64 blt_un::max
	ldsfld	float64 blt_un::min
	blt.un FAIL

	ldsfld	float64 blt_un::max
	ldsfld	float64 blt_un::minus_one
	blt.un FAIL

	ldsfld	float64 blt_un::max
	ldsfld	float64 blt_un::minus_zero
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
	ldsfld	float64 blt_un::pos_inf
	blt.un RR
	br FAIL

RR:
	ldsfld	float64 blt_un::max
	ldsfld	float64 blt_un::NaN
	blt.un SS
	br FAIL

SS:
	ldsfld	float64 blt_un::pos_inf
	ldsfld	float64 blt_un::neg_inf
	blt.un FAIL

	ldsfld	float64 blt_un::pos_inf
	ldsfld	float64 blt_un::min
	blt.un FAIL

	ldsfld	float64 blt_un::pos_inf
	ldsfld	float64 blt_un::minus_one
	blt.un FAIL

	ldsfld	float64 blt_un::pos_inf
	ldsfld	float64 blt_un::minus_zero
	blt.un FAIL

	ldsfld	float64 blt_un::pos_inf
	ldsfld	float64 blt_un::zero
	blt.un FAIL

	ldsfld	float64 blt_un::pos_inf
	ldsfld	float64 blt_un::one
	blt.un FAIL

	ldsfld	float64 blt_un::pos_inf
	ldsfld	float64 blt_un::max
	blt.un FAIL

	ldsfld	float64 blt_un::pos_inf
	ldsfld	float64 blt_un::pos_inf
	blt.un FAIL

TT:
	ldsfld	float64 blt_un::pos_inf
	ldsfld	float64 blt_un::NaN
	blt.un UU
	br FAIL

UU:
	ldsfld	float64 blt_un::NaN
	ldsfld	float64 blt_un::neg_inf
	blt.un VV
	br FAIL

VV:
	ldsfld	float64 blt_un::NaN
	ldsfld	float64 blt_un::min
	blt.un WW
	br FAIL

WW:
	ldsfld	float64 blt_un::NaN
	ldsfld	float64 blt_un::minus_one
	blt.un XX
	br FAIL

XX:
	ldsfld	float64 blt_un::NaN
	ldsfld	float64 blt_un::minus_zero
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
	ldsfld	float64 blt_un::pos_inf
	blt.un CCC
	br FAIL

CCC:
	ldsfld	float64 blt_un::NaN
	ldsfld	float64 blt_un::NaN
	blt.un  L0
	br FAIL

	// Testing early folding logic

L0:
	ldc.r8		NEG_INF
	ldc.r8		NEG_INF
	blt.un FAIL

	ldc.r8		NEG_INF
	ldc.r8		MIN
	blt.un L1
	br FAIL

L1:
	ldc.r8		NEG_INF
	ldc.r8		MINUS_ONE
	blt.un L2
	br FAIL

L2:
	ldc.r8		NEG_INF
	ldc.r8		MINUS_ZERO
	blt.un L3
	br FAIL

L3:
	ldc.r8		NEG_INF
	ldc.r8		ZERO
	blt.un L4
	br FAIL

L4:
	ldc.r8		NEG_INF
	ldc.r8		ONE
	blt.un L5
	br FAIL

L5:
	ldc.r8		NEG_INF
	ldc.r8		MAX
	blt.un L6
	br FAIL

L6:
	ldc.r8		NEG_INF
	ldc.r8		POS_INF
	blt.un L7
	br FAIL

L7:
	ldc.r8		NEG_INF
	ldc.r8		NAN
	blt.un L8
	br FAIL

L8:
	ldc.r8		MIN
	ldc.r8		NEG_INF
	blt.un FAIL

	ldc.r8		MIN
	ldc.r8		MIN
	blt.un FAIL

	ldc.r8		MIN
	ldc.r8		MINUS_ONE
	blt.un L9
	br FAIL

L9:
	ldc.r8		MIN
	ldc.r8		MINUS_ZERO
	blt.un L10
	br FAIL

L10:
	ldc.r8		MIN
	ldc.r8		ZERO
	blt.un L11
	br FAIL

L11:
	ldc.r8		MIN
	ldc.r8		ONE
	blt.un L12
	br FAIL

L12:
	ldc.r8		MIN
	ldc.r8		MAX
	blt.un L13
	br FAIL

L13:
	ldc.r8		MIN
	ldc.r8		POS_INF
	blt.un L14
	br FAIL

L14:
	ldc.r8		MIN
	ldc.r8		NAN
	blt.un L15
	br FAIL

L15:
	ldc.r8		MINUS_ONE
	ldc.r8		NEG_INF
	blt.un FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		MIN
	blt.un FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		MINUS_ONE
	blt.un FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		MINUS_ZERO
	blt.un L16
	br FAIL

L16:
	ldc.r8		MINUS_ONE
	ldc.r8		ZERO
	blt.un L17
	br FAIL

L17:
	ldc.r8		MINUS_ONE
	ldc.r8		ONE
	blt.un L18
	br FAIL

L18:
	ldc.r8		MINUS_ONE
	ldc.r8		MAX
	blt.un L19
	br FAIL

L19:
	ldc.r8		MINUS_ONE
	ldc.r8		POS_INF
	blt.un L20
	br FAIL

L20:
	ldc.r8		MINUS_ONE
	ldc.r8		NAN
	blt.un L21
	br FAIL

L21:
	ldc.r8		MINUS_ZERO
	ldc.r8		NEG_INF
	blt.un FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		MIN
	blt.un FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		MINUS_ONE
	blt.un FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		MINUS_ZERO
	blt.un FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		ZERO
	blt.un FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		ONE
	blt.un L22
	br FAIL

L22:
	ldc.r8		MINUS_ZERO
	ldc.r8		MAX
	blt.un L23
	br FAIL

L23:
	ldc.r8		MINUS_ZERO
	ldc.r8		POS_INF
	blt.un L24
	br FAIL

L24:
	ldc.r8		MINUS_ZERO
	ldc.r8		NAN
	blt.un L25
	br FAIL

L25:
	ldc.r8		ZERO
	ldc.r8		NEG_INF
	blt.un FAIL

	ldc.r8		ZERO
	ldc.r8		MIN
	blt.un FAIL

	ldc.r8		ZERO
	ldc.r8		MINUS_ONE
	blt.un FAIL

	ldc.r8		ZERO
	ldc.r8		MINUS_ZERO
	blt.un FAIL

	ldc.r8		ZERO
	ldc.r8		ZERO
	blt.un FAIL

	ldc.r8		ZERO
	ldc.r8		ONE
	blt.un L26
	br FAIL

L26:
	ldc.r8		ZERO
	ldc.r8		MAX
	blt.un L27
	br FAIL

L27:
	ldc.r8		ZERO
	ldc.r8		POS_INF
	blt.un L28
	br FAIL

L28:
	ldc.r8		ZERO
	ldc.r8		NAN
	blt.un L29
	br FAIL

L29:
	ldc.r8		ONE
	ldc.r8		NEG_INF
	blt.un FAIL

	ldc.r8		ONE
	ldc.r8		MIN
	blt.un FAIL

	ldc.r8		ONE
	ldc.r8		MINUS_ONE
	blt.un FAIL

	ldc.r8		ONE
	ldc.r8		MINUS_ZERO
	blt.un FAIL

	ldc.r8		ONE
	ldc.r8		ZERO
	blt.un FAIL

	ldc.r8		ONE
	ldc.r8		ONE
	blt.un FAIL

	ldc.r8		ONE
	ldc.r8		MAX
	blt.un L30
	br FAIL

L30:
	ldc.r8		ONE
	ldc.r8		POS_INF
	blt.un L31
	br FAIL

L31:
	ldc.r8		ONE
	ldc.r8		NAN
	blt.un L32
	br FAIL

L32:
	ldc.r8		MAX
	ldc.r8		NEG_INF
	blt.un FAIL

	ldc.r8		MAX
	ldc.r8		MIN
	blt.un FAIL

	ldc.r8		MAX
	ldc.r8		MINUS_ONE
	blt.un FAIL

	ldc.r8		MAX
	ldc.r8		MINUS_ZERO
	blt.un FAIL

	ldc.r8		MAX
	ldc.r8		ZERO
	blt.un FAIL

	ldc.r8		MAX
	ldc.r8		ONE
	blt.un FAIL

	ldc.r8		MAX
	ldc.r8		MAX
	blt.un FAIL

	ldc.r8		MAX
	ldc.r8		POS_INF
	blt.un L33
	br FAIL

L33:
	ldc.r8		MAX
	ldc.r8		NAN
	blt.un L34
	br FAIL

L34:
	ldc.r8		POS_INF
	ldc.r8		NEG_INF
	blt.un FAIL

	ldc.r8		POS_INF
	ldc.r8		MIN
	blt.un FAIL

	ldc.r8		POS_INF
	ldc.r8		MINUS_ONE
	blt.un FAIL

	ldc.r8		POS_INF
	ldc.r8		MINUS_ZERO
	blt.un FAIL

	ldc.r8		POS_INF
	ldc.r8		ZERO
	blt.un FAIL

	ldc.r8		POS_INF
	ldc.r8		ONE
	blt.un FAIL

	ldc.r8		POS_INF
	ldc.r8		MAX
	blt.un FAIL

	ldc.r8		POS_INF
	ldc.r8		POS_INF
	blt.un FAIL

	ldc.r8		POS_INF
	ldc.r8		NAN
	blt.un L35
	br FAIL

L35:
	ldc.r8		NAN
	ldc.r8		NEG_INF
	blt.un L36
	br FAIL

L36:
	ldc.r8		NAN
	ldc.r8		MIN
	blt.un L37
	br FAIL

L37:
	ldc.r8		NAN
	ldc.r8		MINUS_ONE
	blt.un L38
	br FAIL

L38:
	ldc.r8		NAN
	ldc.r8		MINUS_ZERO
	blt.un L39
	br FAIL

L39:
	ldc.r8		NAN
	ldc.r8		ZERO
	blt.un L40
	br FAIL

L40:
	ldc.r8		NAN
	ldc.r8		ONE
	blt.un L41
	br FAIL

L41:
	ldc.r8		NAN
	ldc.r8		MAX
	blt.un L42
	br FAIL

L42:
	ldc.r8		NAN
	ldc.r8		POS_INF
	blt.un L43
	br FAIL

L43:
	ldc.r8		NAN
	ldc.r8		NAN
	blt.un BACKCHECK
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
