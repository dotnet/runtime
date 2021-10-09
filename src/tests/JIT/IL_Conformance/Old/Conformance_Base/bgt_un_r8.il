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

.class public bgt_un {

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
	stsfld	float64 bgt_un::neg_inf
	ldc.r8		MIN
	stsfld	float64 bgt_un::min
	ldc.r8		MINUS_ONE
	stsfld	float64 bgt_un::minus_one
	ldc.r8		MINUS_ZERO
	stsfld	float64 bgt_un::minus_zero
	ldc.r8		ZERO
	stsfld	float64 bgt_un::zero
	ldc.r8		ONE
	stsfld	float64 bgt_un::one
	ldc.r8		MAX
	stsfld	float64 bgt_un::max
	ldc.r8		POS_INF
	stsfld	float64 bgt_un::pos_inf
	ldc.r8		NAN
	stsfld	float64 bgt_un::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void bgt_un::initialize()

	ldsfld	float64 bgt_un::neg_inf
	ldsfld	float64 bgt_un::neg_inf
	bgt.un FAIL

	ldsfld	float64 bgt_un::neg_inf
	ldsfld	float64 bgt_un::min
	bgt.un FAIL

	ldsfld	float64 bgt_un::neg_inf
	ldsfld	float64 bgt_un::minus_one
	bgt.un FAIL

	ldsfld	float64 bgt_un::neg_inf
	ldsfld	float64 bgt_un::minus_zero
	bgt.un FAIL

	ldsfld	float64 bgt_un::neg_inf
	ldsfld	float64 bgt_un::zero
	bgt.un FAIL

	ldsfld	float64 bgt_un::neg_inf
	ldsfld	float64 bgt_un::one
	bgt.un FAIL

	ldsfld	float64 bgt_un::neg_inf
	ldsfld	float64 bgt_un::max
	bgt.un FAIL

	ldsfld	float64 bgt_un::neg_inf
	ldsfld	float64 bgt_un::pos_inf
	bgt.un FAIL

	ldsfld	float64 bgt_un::neg_inf
	ldsfld	float64 bgt_un::NaN
	bgt.un A
	br FAIL
A:
	ldsfld	float64 bgt_un::min
	ldsfld	float64 bgt_un::neg_inf
	bgt.un B
	br FAIL

B:
	ldsfld	float64 bgt_un::min
	ldsfld	float64 bgt_un::min
	bgt.un FAIL

	ldsfld	float64 bgt_un::min
	ldsfld	float64 bgt_un::minus_one
	bgt.un FAIL

	ldsfld	float64 bgt_un::min
	ldsfld	float64 bgt_un::minus_zero
	bgt.un FAIL

	ldsfld	float64 bgt_un::min
	ldsfld	float64 bgt_un::zero
	bgt.un FAIL

	ldsfld	float64 bgt_un::min
	ldsfld	float64 bgt_un::one
	bgt.un FAIL

	ldsfld	float64 bgt_un::min
	ldsfld	float64 bgt_un::max
	bgt.un FAIL

	ldsfld	float64 bgt_un::min
	ldsfld	float64 bgt_un::pos_inf
	bgt.un FAIL

	ldsfld	float64 bgt_un::min
	ldsfld	float64 bgt_un::NaN
	bgt.un C
	br FAIL


C:
	ldsfld	float64 bgt_un::minus_one
	ldsfld	float64 bgt_un::neg_inf
	bgt.un D
	br FAIL

D:
	ldsfld	float64 bgt_un::minus_one
	ldsfld	float64 bgt_un::min
	bgt.un E
	br FAIL

E:
	ldsfld	float64 bgt_un::minus_one
	ldsfld	float64 bgt_un::minus_one
	bgt.un FAIL

	ldsfld	float64 bgt_un::minus_one
	ldsfld	float64 bgt_un::minus_zero
	bgt.un FAIL

	ldsfld	float64 bgt_un::minus_one
	ldsfld	float64 bgt_un::zero
	bgt.un FAIL

	ldsfld	float64 bgt_un::minus_one
	ldsfld	float64 bgt_un::one
	bgt.un FAIL

	ldsfld	float64 bgt_un::minus_one
	ldsfld	float64 bgt_un::max
	bgt.un FAIL

	ldsfld	float64 bgt_un::minus_one
	ldsfld	float64 bgt_un::pos_inf
	bgt.un FAIL

	ldsfld	float64 bgt_un::minus_one
	ldsfld	float64 bgt_un::NaN
	bgt.un F
	br FAIL
F:
	ldsfld	float64 bgt_un::minus_zero
	ldsfld	float64 bgt_un::neg_inf
	bgt.un G
	br FAIL

G:
	ldsfld	float64 bgt_un::minus_zero
	ldsfld	float64 bgt_un::min
	bgt.un H
	br FAIL

H:
	ldsfld	float64 bgt_un::minus_zero
	ldsfld	float64 bgt_un::minus_one
	bgt.un I
	br FAIL

I:
	ldsfld	float64 bgt_un::minus_zero
	ldsfld	float64 bgt_un::minus_zero
	bgt.un FAIL

	ldsfld	float64 bgt_un::minus_zero
	ldsfld	float64 bgt_un::zero
	bgt.un FAIL

	ldsfld	float64 bgt_un::minus_zero
	ldsfld	float64 bgt_un::one
	bgt.un FAIL

	ldsfld	float64 bgt_un::minus_zero
	ldsfld	float64 bgt_un::max
	bgt.un FAIL

	ldsfld	float64 bgt_un::minus_zero
	ldsfld	float64 bgt_un::pos_inf
	bgt.un FAIL

	ldsfld	float64 bgt_un::minus_zero
	ldsfld	float64 bgt_un::NaN
	bgt.un J
	br FAIL

J:
	ldsfld	float64 bgt_un::zero
	ldsfld	float64 bgt_un::neg_inf
	bgt.un K
	br FAIL

K:
	ldsfld	float64 bgt_un::zero
	ldsfld	float64 bgt_un::min
	bgt.un L
	br FAIL

L:
	ldsfld	float64 bgt_un::zero
	ldsfld	float64 bgt_un::minus_one
	bgt.un M
	br FAIL

M:
	ldsfld	float64 bgt_un::zero
	ldsfld	float64 bgt_un::minus_zero
	bgt.un FAIL

	ldsfld	float64 bgt_un::zero
	ldsfld	float64 bgt_un::zero
	bgt.un FAIL

	ldsfld	float64 bgt_un::zero
	ldsfld	float64 bgt_un::one
	bgt.un FAIL

	ldsfld	float64 bgt_un::zero
	ldsfld	float64 bgt_un::max
	bgt.un FAIL

	ldsfld	float64 bgt_un::zero
	ldsfld	float64 bgt_un::pos_inf
	bgt.un FAIL

	ldsfld	float64 bgt_un::zero
	ldsfld	float64 bgt_un::NaN
	bgt.un N
	br FAIL

N:
	ldsfld	float64 bgt_un::one
	ldsfld	float64 bgt_un::neg_inf
	bgt.un O
	br FAIL

O:
	ldsfld	float64 bgt_un::one
	ldsfld	float64 bgt_un::min
	bgt.un P
	br FAIL

P:
	ldsfld	float64 bgt_un::one
	ldsfld	float64 bgt_un::minus_one
	bgt.un Q
	br FAIL

Q:
	ldsfld	float64 bgt_un::one
	ldsfld	float64 bgt_un::minus_zero
	bgt.un R
	br FAIL

R:
	ldsfld	float64 bgt_un::one
	ldsfld	float64 bgt_un::zero
	bgt.un S
	br FAIL

S:
	ldsfld	float64 bgt_un::one
	ldsfld	float64 bgt_un::one
	bgt.un FAIL

	ldsfld	float64 bgt_un::one
	ldsfld	float64 bgt_un::max
	bgt.un FAIL

	ldsfld	float64 bgt_un::one
	ldsfld	float64 bgt_un::pos_inf
	bgt.un FAIL

	ldsfld	float64 bgt_un::one
	ldsfld	float64 bgt_un::NaN
	bgt.un T
	br FAIL

T:
	ldsfld	float64 bgt_un::max
	ldsfld	float64 bgt_un::neg_inf
	bgt.un U
	br FAIL

U:
	ldsfld	float64 bgt_un::max
	ldsfld	float64 bgt_un::min
	bgt.un V
	br FAIL

V:
	ldsfld	float64 bgt_un::max
	ldsfld	float64 bgt_un::minus_one
	bgt.un W
	br FAIL

W:
	ldsfld	float64 bgt_un::max
	ldsfld	float64 bgt_un::minus_zero
	bgt.un X
	br FAIL

X:
	ldsfld	float64 bgt_un::max
	ldsfld	float64 bgt_un::zero
	bgt.un Y
	br FAIL

Y:
	ldsfld	float64 bgt_un::max
	ldsfld	float64 bgt_un::one
	bgt.un Z
	br FAIL

Z:
	ldsfld	float64 bgt_un::max
	ldsfld	float64 bgt_un::max
	bgt.un FAIL

	ldsfld	float64 bgt_un::max
	ldsfld	float64 bgt_un::pos_inf
	bgt.un FAIL

	ldsfld	float64 bgt_un::max
	ldsfld	float64 bgt_un::NaN
	bgt.un AA
	br FAIL

AA:
	ldsfld	float64 bgt_un::pos_inf
	ldsfld	float64 bgt_un::neg_inf
	bgt.un BB
	br FAIL

BB:
	ldsfld	float64 bgt_un::pos_inf
	ldsfld	float64 bgt_un::min
	bgt.un CC
	br FAIL

CC:
	ldsfld	float64 bgt_un::pos_inf
	ldsfld	float64 bgt_un::minus_one
	bgt.un DD
	br FAIL

DD:
	ldsfld	float64 bgt_un::pos_inf
	ldsfld	float64 bgt_un::minus_zero
	bgt.un EE
	br FAIL

EE:
	ldsfld	float64 bgt_un::pos_inf
	ldsfld	float64 bgt_un::zero
	bgt.un FF
	br FAIL

FF:
	ldsfld	float64 bgt_un::pos_inf
	ldsfld	float64 bgt_un::one
	bgt.un GG
	br FAIL

GG:
	ldsfld	float64 bgt_un::pos_inf
	ldsfld	float64 bgt_un::max
	bgt.un HH
	br FAIL

HH:
	ldsfld	float64 bgt_un::pos_inf
	ldsfld	float64 bgt_un::pos_inf
	bgt.un FAIL

	ldsfld	float64 bgt_un::pos_inf
	ldsfld	float64 bgt_un::NaN
	bgt.un II
	br FAIL

II:
	ldsfld	float64 bgt_un::NaN
	ldsfld	float64 bgt_un::neg_inf
	bgt.un JJ
	br FAIL

JJ:
	ldsfld	float64 bgt_un::NaN
	ldsfld	float64 bgt_un::min
	bgt.un KK
	br FAIL

KK:
	ldsfld	float64 bgt_un::NaN
	ldsfld	float64 bgt_un::minus_one
	bgt.un LL
	br FAIL

LL:
	ldsfld	float64 bgt_un::NaN
	ldsfld	float64 bgt_un::minus_zero
	bgt.un MM
	br FAIL

MM:
	ldsfld	float64 bgt_un::NaN
	ldsfld	float64 bgt_un::zero
	bgt.un NN
	br FAIL

NN:
	ldsfld	float64 bgt_un::NaN
	ldsfld	float64 bgt_un::one
	bgt.un OO
	br FAIL

OO:
	ldsfld	float64 bgt_un::NaN
	ldsfld	float64 bgt_un::max
	bgt.un PP
	br FAIL

PP:
	ldsfld	float64 bgt_un::NaN
	ldsfld	float64 bgt_un::pos_inf
	bgt.un QQ
	br FAIL

QQ:
	ldsfld	float64 bgt_un::NaN
	ldsfld	float64 bgt_un::NaN
	bgt.un  L0
	br FAIL

    // Testing early folding logic

L0:
	ldc.r8		NEG_INF
	ldc.r8		NEG_INF
	bgt.un FAIL

	ldc.r8		NEG_INF
	ldc.r8		MIN
	bgt.un FAIL

	ldc.r8		NEG_INF
	ldc.r8		MINUS_ONE
	bgt.un FAIL

	ldc.r8		NEG_INF
	ldc.r8		MINUS_ZERO
	bgt.un FAIL

	ldc.r8		NEG_INF
	ldc.r8		ZERO
	bgt.un FAIL

	ldc.r8		NEG_INF
	ldc.r8		ONE
	bgt.un FAIL

	ldc.r8		NEG_INF
	ldc.r8		MAX
	bgt.un FAIL

	ldc.r8		NEG_INF
	ldc.r8		POS_INF
	bgt.un FAIL

	ldc.r8		NEG_INF
	ldc.r8		NAN
	bgt.un L1
	br FAIL

L1:
	ldc.r8		MIN
	ldc.r8		NEG_INF
	bgt.un L2
	br FAIL

L2:
	ldc.r8		MIN
	ldc.r8		MIN
	bgt.un FAIL

	ldc.r8		MIN
	ldc.r8		MINUS_ONE
	bgt.un FAIL

	ldc.r8		MIN
	ldc.r8		MINUS_ZERO
	bgt.un FAIL

	ldc.r8		MIN
	ldc.r8		ZERO
	bgt.un FAIL

	ldc.r8		MIN
	ldc.r8		ONE
	bgt.un FAIL

	ldc.r8		MIN
	ldc.r8		MAX
	bgt.un FAIL

	ldc.r8		MIN
	ldc.r8		POS_INF
	bgt.un FAIL

	ldc.r8		MIN
	ldc.r8		NAN
	bgt.un L3
	br FAIL

L3:
	ldc.r8		MINUS_ONE
	ldc.r8		NEG_INF
	bgt.un L4
	br FAIL

L4:
	ldc.r8		MINUS_ONE
	ldc.r8		MIN
	bgt.un L5
	br FAIL

L5:
	ldc.r8		MINUS_ONE
	ldc.r8		MINUS_ONE
	bgt.un FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		MINUS_ZERO
	bgt.un FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		ZERO
	bgt.un FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		ONE
	bgt.un FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		MAX
	bgt.un FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		POS_INF
	bgt.un FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		NAN
	bgt.un L6
	br FAIL

L6:
	ldc.r8		MINUS_ZERO
	ldc.r8		NEG_INF
	bgt.un L7
	br FAIL

L7:
	ldc.r8		MINUS_ZERO
	ldc.r8		MIN
	bgt.un L8
	br FAIL

L8:
	ldc.r8		MINUS_ZERO
	ldc.r8		MINUS_ONE
	bgt.un L9
	br FAIL

L9:
	ldc.r8		MINUS_ZERO
	ldc.r8		MINUS_ZERO
	bgt.un FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		ZERO
	bgt.un FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		ONE
	bgt.un FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		MAX
	bgt.un FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		POS_INF
	bgt.un FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		NAN
	bgt.un L10
	br FAIL

L10:
	ldc.r8		ZERO
	ldc.r8		NEG_INF
	bgt.un L11
	br FAIL

L11:
	ldc.r8		ZERO
	ldc.r8		MIN
	bgt.un L12
	br FAIL

L12:
	ldc.r8		ZERO
	ldc.r8		MINUS_ONE
	bgt.un L13
	br FAIL

L13:
	ldc.r8		ZERO
	ldc.r8		MINUS_ZERO
	bgt.un FAIL

	ldc.r8		ZERO
	ldc.r8		ZERO
	bgt.un FAIL

	ldc.r8		ZERO
	ldc.r8		ONE
	bgt.un FAIL

	ldc.r8		ZERO
	ldc.r8		MAX
	bgt.un FAIL

	ldc.r8		ZERO
	ldc.r8		POS_INF
	bgt.un FAIL

	ldc.r8		ZERO
	ldc.r8		NAN
	bgt.un L14
	br FAIL

L14:
	ldc.r8		ONE
	ldc.r8		NEG_INF
	bgt.un L15
	br FAIL

L15:
	ldc.r8		ONE
	ldc.r8		MIN
	bgt.un L16
	br FAIL

L16:
	ldc.r8		ONE
	ldc.r8		MINUS_ONE
	bgt.un L17
	br FAIL

L17:
	ldc.r8		ONE
	ldc.r8		MINUS_ZERO
	bgt.un L18
	br FAIL

L18:
	ldc.r8		ONE
	ldc.r8		ZERO
	bgt.un L19
	br FAIL

L19:
	ldc.r8		ONE
	ldc.r8		ONE
	bgt.un FAIL

	ldc.r8		ONE
	ldc.r8		MAX
	bgt.un FAIL

	ldc.r8		ONE
	ldc.r8		POS_INF
	bgt.un FAIL

	ldc.r8		ONE
	ldc.r8		NAN
	bgt.un L20
	br FAIL

L20:
	ldc.r8		MAX
	ldc.r8		NEG_INF
	bgt.un L21
	br FAIL

L21:
	ldc.r8		MAX
	ldc.r8		MIN
	bgt.un L22
	br FAIL

L22:
	ldc.r8		MAX
	ldc.r8		MINUS_ONE
	bgt.un L23
	br FAIL

L23:
	ldc.r8		MAX
	ldc.r8		MINUS_ZERO
	bgt.un L24
	br FAIL

L24:
	ldc.r8		MAX
	ldc.r8		ZERO
	bgt.un L25
	br FAIL

L25:
	ldc.r8		MAX
	ldc.r8		ONE
	bgt.un L26
	br FAIL

L26:
	ldc.r8		MAX
	ldc.r8		MAX
	bgt.un FAIL

	ldc.r8		MAX
	ldc.r8		POS_INF
	bgt.un FAIL

	ldc.r8		MAX
	ldc.r8		NAN
	bgt.un L27
	br FAIL

L27:
	ldc.r8		POS_INF
	ldc.r8		NEG_INF
	bgt.un L28
	br FAIL

L28:
	ldc.r8		POS_INF
	ldc.r8		MIN
	bgt.un L29
	br FAIL

L29:
	ldc.r8		POS_INF
	ldc.r8		MINUS_ONE
	bgt.un L30
	br FAIL

L30:
	ldc.r8		POS_INF
	ldc.r8		MINUS_ZERO
	bgt.un L31
	br FAIL

L31:
	ldc.r8		POS_INF
	ldc.r8		ZERO
	bgt.un L32
	br FAIL

L32:
	ldc.r8		POS_INF
	ldc.r8		ONE
	bgt.un L33
	br FAIL

L33:
	ldc.r8		POS_INF
	ldc.r8		MAX
	bgt.un L34
	br FAIL

L34:
	ldc.r8		POS_INF
	ldc.r8		POS_INF
	bgt.un FAIL

	ldc.r8		POS_INF
	ldc.r8		NAN
	bgt.un L35
	br FAIL

L35:
	ldc.r8		NAN
	ldc.r8		NEG_INF
	bgt.un L36
	br FAIL

L36:
	ldc.r8		NAN
	ldc.r8		MIN
	bgt.un L37
	br FAIL

L37:
	ldc.r8		NAN
	ldc.r8		MINUS_ONE
	bgt.un L38
	br FAIL

L38:
	ldc.r8		NAN
	ldc.r8		MINUS_ZERO
	bgt.un L39
	br FAIL

L39:
	ldc.r8		NAN
	ldc.r8		ZERO
	bgt.un L40
	br FAIL

L40:
	ldc.r8		NAN
	ldc.r8		ONE
	bgt.un L41
	br FAIL

L41:
	ldc.r8		NAN
	ldc.r8		MAX
	bgt.un L42
	br FAIL

L42:
	ldc.r8		NAN
	ldc.r8		POS_INF
	bgt.un L43
	br FAIL

L43:
	ldc.r8		NAN
	ldc.r8		NAN
	bgt.un BACKCHECK
	br FAIL

TOPASS:
	br PASS
BACKCHECK:
	ldc.r8 0x1
	ldc.r8 0x0
	bgt.un TOPASS
	br FAIL


PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly bgt_un_r8.exe{}
