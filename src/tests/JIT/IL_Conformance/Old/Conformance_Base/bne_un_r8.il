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

.class public bne_un {

.field public static	float64 neg_inf
.field public static	float64 min
.field public static	float64 minus_one
.field public static	float64 minus_zero
.field public static	float64 zero
.field public static	float64 one
.field public static	float64 max
.field public static	float64 pos_inf
.field public static	float64 NaN
.method public static void initialize() {
.maxstack		10
	ldc.r8		NEG_INF
	stsfld	float64 bne_un::neg_inf
	ldc.r8		MIN
	stsfld	float64 bne_un::min
	ldc.r8		MINUS_ONE
	stsfld	float64 bne_un::minus_one
	ldc.r8		MINUS_ZERO
	stsfld	float64 bne_un::minus_zero
	ldc.r8		ZERO
	stsfld	float64 bne_un::zero
	ldc.r8		ONE
	stsfld	float64 bne_un::one
	ldc.r8		MAX
	stsfld	float64 bne_un::max
	ldc.r8		POS_INF
	stsfld	float64 bne_un::pos_inf
	ldc.r8		NAN
	stsfld	float64 bne_un::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void bne_un::initialize()
	ldsfld	float64 bne_un::neg_inf
	ldsfld	float64 bne_un::neg_inf
	bne.un FAIL

	ldsfld	float64 bne_un::neg_inf
	ldsfld	float64 bne_un::min
	bne.un A
	br FAIL

A:
	ldsfld	float64 bne_un::neg_inf
	ldsfld	float64 bne_un::minus_one
	bne.un B
	br FAIL

B:
	ldsfld	float64 bne_un::neg_inf
	ldsfld	float64 bne_un::minus_zero
	bne.un C
	br FAIL

C:
	ldsfld	float64 bne_un::neg_inf
	ldsfld	float64 bne_un::zero
	bne.un D
	br FAIL

D:
	ldsfld	float64 bne_un::neg_inf
	ldsfld	float64 bne_un::one
	bne.un E
	br FAIL

E:
	ldsfld	float64 bne_un::neg_inf
	ldsfld	float64 bne_un::max
	bne.un F
	br FAIL

F:
	ldsfld	float64 bne_un::neg_inf
	ldsfld	float64 bne_un::pos_inf
	bne.un G
	br FAIL

G:
	ldsfld	float64 bne_un::neg_inf
	ldsfld	float64 bne_un::NaN
	bne.un H
	br FAIL

H:
	ldsfld	float64 bne_un::min
	ldsfld	float64 bne_un::neg_inf
	bne.un I
	br FAIL

I:
	ldsfld	float64 bne_un::min
	ldsfld	float64 bne_un::min
	bne.un FAIL

	ldsfld	float64 bne_un::min
	ldsfld	float64 bne_un::minus_one
	bne.un J
	br FAIL

J:
	ldsfld	float64 bne_un::min
	ldsfld	float64 bne_un::minus_zero
	bne.un K
	br FAIL

K:
	ldsfld	float64 bne_un::min
	ldsfld	float64 bne_un::zero
	bne.un L
	br		FAIL

L:
	ldsfld	float64 bne_un::min
	ldsfld	float64 bne_un::one
	bne.un M
	br		FAIL

M:
	ldsfld	float64 bne_un::min
	ldsfld	float64 bne_un::max
	bne.un N
	br		FAIL

N:
	ldsfld	float64 bne_un::min
	ldsfld	float64 bne_un::pos_inf
	bne.un O
	br		FAIL

O:
	ldsfld	float64 bne_un::min
	ldsfld	float64 bne_un::NaN
	bne.un P
	br		FAIL
P:
	ldsfld	float64 bne_un::minus_one
	ldsfld	float64 bne_un::neg_inf
	bne.un Q
	br		FAIL

Q:
	ldsfld	float64 bne_un::minus_one
	ldsfld	float64 bne_un::min
	bne.un R
	br		FAIL

R:
	ldsfld	float64 bne_un::minus_one
	ldsfld	float64 bne_un::minus_one
	bne.un FAIL

S:
	ldsfld	float64 bne_un::minus_one
	ldsfld	float64 bne_un::minus_zero
	bne.un T
	br		FAIL

T:
	ldsfld	float64 bne_un::minus_one
	ldsfld	float64 bne_un::zero
	bne.un U
	br		FAIL

U:
	ldsfld	float64 bne_un::minus_one
	ldsfld	float64 bne_un::one
	bne.un V
	br		FAIL

V:
	ldsfld	float64 bne_un::minus_one
	ldsfld	float64 bne_un::max
	bne.un W
	br		FAIL

W:
	ldsfld	float64 bne_un::minus_one
	ldsfld	float64 bne_un::pos_inf
	bne.un X
	br		FAIL

X:
	ldsfld	float64 bne_un::minus_one
	ldsfld	float64 bne_un::NaN
	bne.un Y
	br		FAIL
Y:
	ldsfld	float64 bne_un::minus_zero
	ldsfld	float64 bne_un::neg_inf
	bne.un Z
	br		FAIL

Z:
	ldsfld	float64 bne_un::minus_zero
	ldsfld	float64 bne_un::min
	bne.un AA
	br		FAIL

AA:
	ldsfld	float64 bne_un::minus_zero
	ldsfld	float64 bne_un::minus_one
	bne.un BB
	br		FAIL

BB:
	ldsfld	float64 bne_un::minus_zero
	ldsfld	float64 bne_un::minus_zero
	bne.un FAIL

	ldsfld	float64 bne_un::minus_zero
	ldsfld	float64 bne_un::zero
	bne.un FAIL

	ldsfld	float64 bne_un::minus_zero
	ldsfld	float64 bne_un::one
	bne.un CC
	br		FAIL

CC:
	ldsfld	float64 bne_un::minus_zero
	ldsfld	float64 bne_un::max
	bne.un DD
	br		FAIL

DD:
	ldsfld	float64 bne_un::minus_zero
	ldsfld	float64 bne_un::pos_inf
	bne.un EE
	br		FAIL

EE:
	ldsfld	float64 bne_un::minus_zero
	ldsfld	float64 bne_un::NaN
	bne.un FF
	br		FAIL
FF:
	ldsfld	float64 bne_un::zero
	ldsfld	float64 bne_un::neg_inf
	bne.un GG
	br		FAIL

GG:
	ldsfld	float64 bne_un::zero
	ldsfld	float64 bne_un::min
	bne.un HH
	br		FAIL

HH:
	ldsfld	float64 bne_un::zero
	ldsfld	float64 bne_un::minus_one
	bne.un II
	br		FAIL

II:
	ldsfld	float64 bne_un::zero
	ldsfld	float64 bne_un::minus_zero
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
	ldsfld	float64 bne_un::pos_inf
	bne.un LL
	br		FAIL

LL:
	ldsfld	float64 bne_un::zero
	ldsfld	float64 bne_un::NaN
	bne.un MM
	br		FAIL

MM:
	ldsfld	float64 bne_un::one
	ldsfld	float64 bne_un::neg_inf
	bne.un NN
	br		FAIL

NN:
	ldsfld	float64 bne_un::one
	ldsfld	float64 bne_un::min
	bne.un OO
	br		FAIL

OO:
	ldsfld	float64 bne_un::one
	ldsfld	float64 bne_un::minus_one
	bne.un PP
	br		FAIL

PP:
	ldsfld	float64 bne_un::one
	ldsfld	float64 bne_un::minus_zero
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
	ldsfld	float64 bne_un::pos_inf
	bne.un UU
	br		FAIL

UU:
	ldsfld	float64 bne_un::one
	ldsfld	float64 bne_un::NaN
	bne.un VV
	br		FAIL

VV:
	ldsfld	float64 bne_un::max
	ldsfld	float64 bne_un::neg_inf
	bne.un WW
	br		FAIL

WW:
	ldsfld	float64 bne_un::max
	ldsfld	float64 bne_un::min
	bne.un XX
	br		FAIL

XX:
	ldsfld	float64 bne_un::max
	ldsfld	float64 bne_un::minus_one
	bne.un YY
	br		FAIL

YY:
	ldsfld	float64 bne_un::max
	ldsfld	float64 bne_un::minus_zero
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
	ldsfld	float64 bne_un::pos_inf
	bne.un CCC
	br		FAIL

CCC:
	ldsfld	float64 bne_un::max
	ldsfld	float64 bne_un::NaN
	bne.un DDD
	br	FAIL

DDD:
	ldsfld	float64 bne_un::pos_inf
	ldsfld	float64 bne_un::neg_inf
	bne.un EEE
	br		FAIL

EEE:
	ldsfld	float64 bne_un::pos_inf
	ldsfld	float64 bne_un::min
	bne.un FFF
	br		FAIL

FFF:
	ldsfld	float64 bne_un::pos_inf
	ldsfld	float64 bne_un::minus_one
	bne.un GGG
	br		FAIL

GGG:
	ldsfld	float64 bne_un::pos_inf
	ldsfld	float64 bne_un::minus_zero
	bne.un HHH
	br		FAIL

HHH:
	ldsfld	float64 bne_un::pos_inf
	ldsfld	float64 bne_un::zero
	bne.un III
	br		FAIL

III:
	ldsfld	float64 bne_un::pos_inf
	ldsfld	float64 bne_un::one
	bne.un JJJ
	br		FAIL

JJJ:
	ldsfld	float64 bne_un::pos_inf
	ldsfld	float64 bne_un::max
	bne.un KK_
	br		FAIL

KK_:
	ldsfld	float64 bne_un::pos_inf
	ldsfld	float64 bne_un::pos_inf
	bne.un FAIL

	ldsfld	float64 bne_un::pos_inf
	ldsfld	float64 bne_un::NaN
	bne.un LLL
	br		FAIL

LLL:
	ldsfld	float64 bne_un::NaN
	ldsfld	float64 bne_un::neg_inf
	bne.un MMM
	br		FAIL

MMM:
	ldsfld	float64 bne_un::NaN
	ldsfld	float64 bne_un::min
	bne.un NNN
	br		FAIL

NNN:
	ldsfld	float64 bne_un::NaN
	ldsfld	float64 bne_un::minus_one
	bne.un OOO
	br		FAIL

OOO:
	ldsfld	float64 bne_un::NaN
	ldsfld	float64 bne_un::minus_zero
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
	ldsfld	float64 bne_un::pos_inf
	bne.un TTT
	br		FAIL

TTT:
	ldsfld	float64 bne_un::NaN
	ldsfld	float64 bne_un::NaN
	bne.un L0
	br FAIL

	// Testing early folding logic

L0:
	ldc.r8		NEG_INF
	ldc.r8		NEG_INF
	bne.un FAIL

	ldc.r8		NEG_INF
	ldc.r8		MIN
	bne.un L1
	br FAIL

L1:
	ldc.r8		NEG_INF
	ldc.r8		MINUS_ONE
	bne.un L2
	br FAIL

L2:
	ldc.r8		NEG_INF
	ldc.r8		MINUS_ZERO
	bne.un L3
	br FAIL

L3:
	ldc.r8		NEG_INF
	ldc.r8		ZERO
	bne.un L4
	br FAIL

L4:
	ldc.r8		NEG_INF
	ldc.r8		ONE
	bne.un L5
	br FAIL

L5:
	ldc.r8		NEG_INF
	ldc.r8		MAX
	bne.un L6
	br FAIL

L6:
	ldc.r8		NEG_INF
	ldc.r8		POS_INF
	bne.un L7
	br FAIL

L7:
	ldc.r8		NEG_INF
	ldc.r8		NAN
	bne.un L8
	br FAIL

L8:
	ldc.r8		MIN
	ldc.r8		NEG_INF
	bne.un L9
	br FAIL

L9:
	ldc.r8		MIN
	ldc.r8		MIN
	bne.un FAIL

	ldc.r8		MIN
	ldc.r8		MINUS_ONE
	bne.un L10
	br FAIL

L10:
	ldc.r8		MIN
	ldc.r8		MINUS_ZERO
	bne.un L11
	br FAIL

L11:
	ldc.r8		MIN
	ldc.r8		ZERO
	bne.un L12
	br FAIL

L12:
	ldc.r8		MIN
	ldc.r8		ONE
	bne.un L13
	br FAIL

L13:
	ldc.r8		MIN
	ldc.r8		MAX
	bne.un L14
	br FAIL

L14:
	ldc.r8		MIN
	ldc.r8		POS_INF
	bne.un L15
	br FAIL

L15:
	ldc.r8		MIN
	ldc.r8		NAN
	bne.un L16
	br FAIL

L16:
	ldc.r8		MINUS_ONE
	ldc.r8		NEG_INF
	bne.un L17
	br FAIL

L17:
	ldc.r8		MINUS_ONE
	ldc.r8		MIN
	bne.un L18
	br FAIL

L18:
	ldc.r8		MINUS_ONE
	ldc.r8		MINUS_ONE
	bne.un FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		MINUS_ZERO
	bne.un L19
	br FAIL

L19:
	ldc.r8		MINUS_ONE
	ldc.r8		ZERO
	bne.un L20
	br FAIL

L20:
	ldc.r8		MINUS_ONE
	ldc.r8		ONE
	bne.un L21
	br FAIL

L21:
	ldc.r8		MINUS_ONE
	ldc.r8		MAX
	bne.un L22
	br FAIL

L22:
	ldc.r8		MINUS_ONE
	ldc.r8		POS_INF
	bne.un L23
	br FAIL

L23:
	ldc.r8		MINUS_ONE
	ldc.r8		NAN
	bne.un L24
	br FAIL

L24:
	ldc.r8		MINUS_ZERO
	ldc.r8		NEG_INF
	bne.un L25
	br FAIL

L25:
	ldc.r8		MINUS_ZERO
	ldc.r8		MIN
	bne.un L26
	br FAIL

L26:
	ldc.r8		MINUS_ZERO
	ldc.r8		MINUS_ONE
	bne.un L27
	br FAIL

L27:
	ldc.r8		MINUS_ZERO
	ldc.r8		MINUS_ZERO
	bne.un FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		ZERO
	bne.un FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		ONE
	bne.un L28
	br FAIL

L28:
	ldc.r8		MINUS_ZERO
	ldc.r8		MAX
	bne.un L29
	br FAIL

L29:
	ldc.r8		MINUS_ZERO
	ldc.r8		POS_INF
	bne.un L30
	br FAIL

L30:
	ldc.r8		MINUS_ZERO
	ldc.r8		NAN
	bne.un L31
	br FAIL

L31:
	ldc.r8		ZERO
	ldc.r8		NEG_INF
	bne.un L32
	br FAIL

L32:
	ldc.r8		ZERO
	ldc.r8		MIN
	bne.un L33
	br FAIL

L33:
	ldc.r8		ZERO
	ldc.r8		MINUS_ONE
	bne.un L34
	br FAIL

L34:
	ldc.r8		ZERO
	ldc.r8		MINUS_ZERO
	bne.un FAIL

	ldc.r8		ZERO
	ldc.r8		ZERO
	bne.un FAIL

	ldc.r8		ZERO
	ldc.r8		ONE
	bne.un L35
	br FAIL

L35:
	ldc.r8		ZERO
	ldc.r8		MAX
	bne.un L36
	br FAIL

L36:
	ldc.r8		ZERO
	ldc.r8		POS_INF
	bne.un L37
	br FAIL

L37:
	ldc.r8		ZERO
	ldc.r8		NAN
	bne.un L38
	br FAIL

L38:
	ldc.r8		ONE
	ldc.r8		NEG_INF
	bne.un L39
	br FAIL

L39:
	ldc.r8		ONE
	ldc.r8		MIN
	bne.un L40
	br FAIL

L40:
	ldc.r8		ONE
	ldc.r8		MINUS_ONE
	bne.un L41
	br FAIL

L41:
	ldc.r8		ONE
	ldc.r8		MINUS_ZERO
	bne.un L42
	br FAIL

L42:
	ldc.r8		ONE
	ldc.r8		ZERO
	bne.un L43
	br FAIL

L43:
	ldc.r8		ONE
	ldc.r8		ONE
	bne.un FAIL

	ldc.r8		ONE
	ldc.r8		MAX
	bne.un L44
	br FAIL

L44:
	ldc.r8		ONE
	ldc.r8		POS_INF
	bne.un L45
	br FAIL

L45:
	ldc.r8		ONE
	ldc.r8		NAN
	bne.un L46
	br FAIL

L46:
	ldc.r8		MAX
	ldc.r8		NEG_INF
	bne.un L47
	br FAIL

L47:
	ldc.r8		MAX
	ldc.r8		MIN
	bne.un L48
	br FAIL

L48:
	ldc.r8		MAX
	ldc.r8		MINUS_ONE
	bne.un L49
	br FAIL

L49:
	ldc.r8		MAX
	ldc.r8		MINUS_ZERO
	bne.un L50
	br FAIL

L50:
	ldc.r8		MAX
	ldc.r8		ZERO
	bne.un L51
	br FAIL

L51:
	ldc.r8		MAX
	ldc.r8		ONE
	bne.un L52
	br FAIL

L52:
	ldc.r8		MAX
	ldc.r8		MAX
	bne.un FAIL

	ldc.r8		MAX
	ldc.r8		POS_INF
	bne.un L53
	br FAIL

L53:
	ldc.r8		MAX
	ldc.r8		NAN
	bne.un L54
	br FAIL

L54:
	ldc.r8		POS_INF
	ldc.r8		NEG_INF
	bne.un L55
	br FAIL

L55:
	ldc.r8		POS_INF
	ldc.r8		MIN
	bne.un L56
	br FAIL

L56:
	ldc.r8		POS_INF
	ldc.r8		MINUS_ONE
	bne.un L57
	br FAIL

L57:
	ldc.r8		POS_INF
	ldc.r8		MINUS_ZERO
	bne.un L58
	br FAIL

L58:
	ldc.r8		POS_INF
	ldc.r8		ZERO
	bne.un L59
	br FAIL

L59:
	ldc.r8		POS_INF
	ldc.r8		ONE
	bne.un L60
	br FAIL

L60:
	ldc.r8		POS_INF
	ldc.r8		MAX
	bne.un L61
	br FAIL

L61:
	ldc.r8		POS_INF
	ldc.r8		POS_INF
	bne.un FAIL

	ldc.r8		POS_INF
	ldc.r8		NAN
	bne.un L62
	br FAIL

L62:
	ldc.r8		NAN
	ldc.r8		NEG_INF
	bne.un L63
	br FAIL

L63:
	ldc.r8		NAN
	ldc.r8		MIN
	bne.un L64
	br FAIL

L64:
	ldc.r8		NAN
	ldc.r8		MINUS_ONE
	bne.un L65
	br FAIL

L65:
	ldc.r8		NAN
	ldc.r8		MINUS_ZERO
	bne.un L66
	br FAIL

L66:
	ldc.r8		NAN
	ldc.r8		ZERO
	bne.un L67
	br FAIL

L67:
	ldc.r8		NAN
	ldc.r8		ONE
	bne.un L68
	br FAIL

L68:
	ldc.r8		NAN
	ldc.r8		MAX
	bne.un L69
	br FAIL

L69:
	ldc.r8		NAN
	ldc.r8		POS_INF
	bne.un L70
	br FAIL

L70:
	ldc.r8		NAN
	ldc.r8		NAN
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
