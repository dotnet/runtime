// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}





.assembly extern ConvDLL{}


.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack 2
.locals	(class [ConvDLL]ConvDLL)

newobj instance void [ConvDLL]ConvDLL::.ctor()
stloc 0



	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	bge A
	br FAIL

A:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	bge B
	br FAIL

B:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	bge C
	br FAIL

C:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	bge D
	br FAIL

D:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	ldc.r8		float64(0x8000000000000000)
	bge E
	br FAIL

E:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	bge F
	br FAIL

F:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	bge G
	br FAIL

G:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	bge H
	br FAIL

H:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	bge FAIL


	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	bge I
	br FAIL

I:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	bge J
	br FAIL

J:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	bge K
	br FAIL

K:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	ldc.r8		float64(0x8000000000000000)
	bge L
	br FAIL

L:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	bge M
	br FAIL

M:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	bge N
	br FAIL

N:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	bge O
	br FAIL

O:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	bge FAIL



	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	bge P
	br FAIL

P:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	bge Q
	br FAIL

Q:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	ldc.r8		float64(0x8000000000000000)
	bge R
	br FAIL

R:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	bge S
	br FAIL

S:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	bge T
	br FAIL

T:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	bge U
	br FAIL

U:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	bge FAIL



	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	bge V
	br FAIL

V:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	ldc.r8		float64(0x8000000000000000)
	bge W
	br FAIL

W:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	bge X
	br FAIL

X:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	bge Y
	br FAIL

Y:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	bge Z
	br FAIL

Z:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	bge FAIL



	ldc.r8		float64(0x8000000000000000)
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	bge FAIL

	ldc.r8		float64(0x8000000000000000)
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	bge FAIL

	ldc.r8		float64(0x8000000000000000)
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	bge FAIL

	ldc.r8		float64(0x8000000000000000)
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	bge AA
	br FAIL

AA:
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x8000000000000000)
	bge BB
	br FAIL

BB:
	ldc.r8		float64(0x8000000000000000)
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	bge CC
	br FAIL

CC:
	ldc.r8		float64(0x8000000000000000)
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	bge DD
	br FAIL

DD:
	ldc.r8		float64(0x8000000000000000)
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	bge EE
	br FAIL

EE:
	ldc.r8		float64(0x8000000000000000)
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	bge FAIL



	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	ldc.r8		float64(0x8000000000000000)
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	bge FF
	br FAIL

FF:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	bge GG
	br FAIL

GG:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	bge HH
	br FAIL

HH:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	bge FAIL



	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	ldc.r8		float64(0x8000000000000000)
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	bge II
	br FAIL

II:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	bge JJ
	br FAIL

JJ:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	bge FAIL




	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	ldc.r8		float64(0x8000000000000000)
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	bge KK
	br FAIL

KK:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	bge FAIL




	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	ldc.r8		float64(0x8000000000000000)
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	bge FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	bge FAIL
	br BACKCHECK


TOPASS:
	br PASS
BACKCHECK:
	ldc.r8 0x0
	dup
	bge TOPASS
	br FAIL

	
PASS:
	ldc.i4		100
	ret
FAIL:
	ldc.i4		0x0
	ret

}
.assembly bge_r8.exe{}
