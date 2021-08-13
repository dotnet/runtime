// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern ConvDLL{}

#define NEG_INF    "float32(0xFF800000)"
#define MIN        "float32(0xFF7FFFFF)"
#define MINUS_ONE  "float32(0xBF800000)"
#define MINUS_ZERO "float32(0x80000000)"
#define ZERO       "float32(0x00000000)"
#define ONE        "float32(0x3F800000)"
#define MAX        "float32(0x7F7FFFFF)"
#define POS_INF    "float32(0x7F800000)"
#define NAN        "float32(0x7FC00000)"

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack 2
.locals	(class [ConvDLL]ConvDLL)

newobj instance void [ConvDLL]ConvDLL::.ctor()
stloc 0



	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	bge.un A
	br FAIL

A:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	bge.un B
	br FAIL

B:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	bge.un C
	br FAIL

C:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	bge.un D
	br FAIL

D:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldc.r4		float32(0x80000000)
	bge.un E
	br FAIL

E:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	bge.un F
	br FAIL

F:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	bge.un G
	br FAIL

G:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	bge.un H
	br FAIL

H:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	bge.un _H
	br FAIL


_H:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	bge.un I
	br FAIL

I:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	bge.un J
	br FAIL

J:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	bge.un K
	br FAIL

K:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldc.r4		float32(0x80000000)
	bge.un L
	br FAIL

L:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	bge.un M
	br FAIL

M:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	bge.un N
	br FAIL

N:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	bge.un O
	br FAIL

O:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	bge.un _O
	br FAIL



_O:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	bge.un P
	br FAIL

P:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	bge.un Q
	br FAIL

Q:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldc.r4		float32(0x80000000)
	bge.un R
	br FAIL

R:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	bge.un S
	br FAIL

S:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	bge.un T
	br FAIL

T:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	bge.un U
	br FAIL

U:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	bge.un _U
	br FAIL



_U:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	bge.un V
	br FAIL

V:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldc.r4		float32(0x80000000)
	bge.un W
	br FAIL

W:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	bge.un X
	br FAIL

X:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	bge.un Y
	br FAIL

Y:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	bge.un Z
	br FAIL

Z:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	bge.un _Z
	br FAIL



_Z:
	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	bge.un FAIL

	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	bge.un FAIL

	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	bge.un FAIL

	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	bge.un AA
	br FAIL

AA:
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x80000000)
	bge.un BB
	br FAIL

BB:
	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	bge.un CC
	br FAIL

CC:
	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	bge.un DD
	br FAIL

DD:
	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	bge.un EE
	br FAIL

EE:
	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	bge.un _EE
	br FAIL



_EE:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldc.r4		float32(0x80000000)
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	bge.un FF
	br FAIL

FF:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	bge.un GG
	br FAIL

GG:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	bge.un HH
	br FAIL

HH:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	bge.un _HH
	br FAIL



_HH:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldc.r4		float32(0x80000000)
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	bge.un II
	br FAIL

II:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	bge.un JJ
	br FAIL

JJ:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	bge.un _JJ
	br FAIL




_JJ:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldc.r4		float32(0x80000000)
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	bge.un FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	bge.un KK
	br FAIL

KK:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	bge.un _KK
	br FAIL




_KK:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	bge.un LL
	br FAIL

LL:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	bge.un MM
	br FAIL

MM:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	bge.un NN
	br FAIL

NN:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	bge.un OO
	br FAIL

OO:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldc.r4		float32(0x80000000)
	bge.un PP
	br FAIL

PP:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	bge.un QQ
	br FAIL

QQ:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	bge.un RR
	br FAIL

RR:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	bge.un SS
	br FAIL

SS:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	bge.un L0
	br FAIL

	// Testing early folding logic

L0:
	ldc.r4		POS_INF
	ldc.r4		POS_INF
	bge.un L1
	br FAIL

L1:
	ldc.r4		POS_INF
	ldc.r4		MAX
	bge.un L2
	br FAIL

L2:
	ldc.r4		POS_INF
	ldc.r4		ONE
	bge.un L3
	br FAIL

L3:
	ldc.r4		POS_INF
	ldc.r4		ZERO
	bge.un L4
	br FAIL

L4:
	ldc.r4		POS_INF
	ldc.r4		float32(0x80000000)
	bge.un L5
	br FAIL

L5:
	ldc.r4		POS_INF
	ldc.r4		MINUS_ONE
	bge.un L6
	br FAIL

L6:
	ldc.r4		POS_INF
	ldc.r4		MIN
	bge.un L7
	br FAIL

L7:
	ldc.r4		POS_INF
	ldc.r4		NEG_INF
	bge.un L8
	br FAIL

L8:
	ldc.r4		POS_INF
	ldc.r4		NAN
	bge.un L9
	br FAIL


L9:
	ldc.r4		MAX
	ldc.r4		POS_INF
	bge.un L10

	ldc.r4		MAX
	ldc.r4		MAX
	bge.un L10
	br FAIL

L10:
	ldc.r4		MAX
	ldc.r4		ONE
	bge.un L11
	br FAIL

L11:
	ldc.r4		MAX
	ldc.r4		ZERO
	bge.un L12
	br FAIL

L12:
	ldc.r4		MAX
	ldc.r4		float32(0x80000000)
	bge.un L13
	br FAIL

L13:
	ldc.r4		MAX
	ldc.r4		MINUS_ONE
	bge.un L14
	br FAIL

L14:
	ldc.r4		MAX
	ldc.r4		MIN
	bge.un L15
	br FAIL

L15:
	ldc.r4		MAX
	ldc.r4		NEG_INF
	bge.un L16
	br FAIL

L16:
	ldc.r4		MAX
	ldc.r4		NAN
	bge.un L17
	br FAIL



L17:
	ldc.r4		ONE
	ldc.r4		POS_INF
	bge.un L18

	ldc.r4		ONE
	ldc.r4		MAX
	bge.un L18

	ldc.r4		ONE
	ldc.r4		ONE
	bge.un L18
	br FAIL

L18:
	ldc.r4		ONE
	ldc.r4		ZERO
	bge.un L19
	br FAIL

L19:
	ldc.r4		ONE
	ldc.r4		float32(0x80000000)
	bge.un L20
	br FAIL

L20:
	ldc.r4		ONE
	ldc.r4		MINUS_ONE
	bge.un L21
	br FAIL

L21:
	ldc.r4		ONE
	ldc.r4		MIN
	bge.un L22
	br FAIL

L22:
	ldc.r4		ONE
	ldc.r4		NEG_INF
	bge.un L23
	br FAIL

L23:
	ldc.r4		ONE
	ldc.r4		NAN
	bge.un L24
	br FAIL



L24:
	ldc.r4		ZERO
	ldc.r4		POS_INF
	bge.un L25

	ldc.r4		ZERO
	ldc.r4		MAX
	bge.un L25

	ldc.r4		ZERO
	ldc.r4		ONE
	bge.un L25

	ldc.r4		ZERO
	ldc.r4		ZERO
	bge.un L25
	br FAIL

L25:
	ldc.r4		ZERO
	ldc.r4		float32(0x80000000)
	bge.un L26
	br FAIL

L26:
	ldc.r4		ZERO
	ldc.r4		MINUS_ONE
	bge.un L27
	br FAIL

L27:
	ldc.r4		ZERO
	ldc.r4		MIN
	bge.un L28
	br FAIL

L28:
	ldc.r4		ZERO
	ldc.r4		NEG_INF
	bge.un L29
	br FAIL

L29:
	ldc.r4		ZERO
	ldc.r4		NAN
	bge.un L30
	br FAIL



L30:
	ldc.r4		float32(0x80000000)
	ldc.r4		POS_INF
	bge.un L31

	ldc.r4		float32(0x80000000)
	ldc.r4		MAX
	bge.un L31

	ldc.r4		float32(0x80000000)
	ldc.r4		ONE
	bge.un L31

	ldc.r4		float32(0x80000000)
	ldc.r4		ZERO
	bge.un L31
	br FAIL

L31:
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x80000000)
	bge.un L32
	br FAIL

L32:
	ldc.r4		float32(0x80000000)
	ldc.r4		MINUS_ONE
	bge.un L33
	br FAIL

L33:
	ldc.r4		float32(0x80000000)
	ldc.r4		MIN
	bge.un L34
	br FAIL

L34:
	ldc.r4		float32(0x80000000)
	ldc.r4		NEG_INF
	bge.un L35
	br FAIL

L35:
	ldc.r4		float32(0x80000000)
	ldc.r4		NAN
	bge.un L36
	br FAIL



L36:
	ldc.r4		MINUS_ONE
	ldc.r4		POS_INF
	bge.un L37

	ldc.r4		MINUS_ONE
	ldc.r4		MAX
	bge.un L37

	ldc.r4		MINUS_ONE
	ldc.r4		ONE
	bge.un L37

	ldc.r4		MINUS_ONE
	ldc.r4		ZERO
	bge.un L37

	ldc.r4		MINUS_ONE
	ldc.r4		float32(0x80000000)
	bge.un L37

	ldc.r4		MINUS_ONE
	ldc.r4		MINUS_ONE
	bge.un L37
	br FAIL

L37:
	ldc.r4		MINUS_ONE
	ldc.r4		MIN
	bge.un L38
	br FAIL

L38:
	ldc.r4		MINUS_ONE
	ldc.r4		NEG_INF
	bge.un L39
	br FAIL

L39:
	ldc.r4		MINUS_ONE
	ldc.r4		NAN
	bge.un L40
	br FAIL



L40:
	ldc.r4		MIN
	ldc.r4		POS_INF
	bge.un L41

	ldc.r4		MIN
	ldc.r4		MAX
	bge.un L41

	ldc.r4		MIN
	ldc.r4		ONE
	bge.un L41

	ldc.r4		MIN
	ldc.r4		ZERO
	bge.un L41

	ldc.r4		MIN
	ldc.r4		float32(0x80000000)
	bge.un L41

	ldc.r4		MIN
	ldc.r4		MINUS_ONE
	bge.un L41

	ldc.r4		MIN
	ldc.r4		MIN
	bge.un L41
	br FAIL

L41:
	ldc.r4		MIN
	ldc.r4		NEG_INF
	bge.un L42
	br FAIL

L42:
	ldc.r4		MIN
	ldc.r4		NAN
	bge.un L43
	br FAIL




L43:
	ldc.r4		NEG_INF
	ldc.r4		POS_INF
	bge.un L44

	ldc.r4		NEG_INF
	ldc.r4		MAX
	bge.un L44

	ldc.r4		NEG_INF
	ldc.r4		ONE
	bge.un L44

	ldc.r4		NEG_INF
	ldc.r4		ZERO
	bge.un L44

	ldc.r4		NEG_INF
	ldc.r4		float32(0x80000000)
	bge.un L44

	ldc.r4		NEG_INF
	ldc.r4		MINUS_ONE
	bge.un L44

	ldc.r4		NEG_INF
	ldc.r4		MIN
	bge.un L44

	ldc.r4		NEG_INF
	ldc.r4		NEG_INF
	bge.un L44
	br FAIL

L44:
	ldc.r4		NEG_INF
	ldc.r4		NAN
	bge.un L45
	br FAIL




L45:
	ldc.r4		NAN
	ldc.r4		POS_INF
	bge.un L46
	br FAIL

L46:
	ldc.r4		NAN
	ldc.r4		MAX
	bge.un L47
	br FAIL

L47:
	ldc.r4		NAN
	ldc.r4		ONE
	bge.un L48
	br FAIL

L48:
	ldc.r4		NAN
	ldc.r4		ZERO
	bge.un L49
	br FAIL

L49:
	ldc.r4		NAN
	ldc.r4		float32(0x80000000)
	bge.un L50
	br FAIL

L50:
	ldc.r4		NAN
	ldc.r4		MINUS_ONE
	bge.un L51
	br FAIL

L51:
	ldc.r4		NAN
	ldc.r4		MIN
	bge.un L52
	br FAIL

L52:
	ldc.r4		NAN
	ldc.r4		NEG_INF
	bge.un L53
	br FAIL

L53:
	ldc.r4		NAN
	ldc.r4		NAN
	bge.un BACKCHECK
	br FAIL


TOPASS:
	br PASS
BACKCHECK:
	ldc.r4 0x0
	dup
	bge.un TOPASS
	br FAIL

	
PASS:
	ldc.i4		100
	ret
FAIL:
	ldc.i4		0x0
	ret

}
.assembly bge_un_r4.exe{}
