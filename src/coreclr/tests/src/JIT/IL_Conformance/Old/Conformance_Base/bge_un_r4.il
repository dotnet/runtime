// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}



.assembly extern ConvDLL{}

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
