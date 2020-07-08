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
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	bge A
	br FAIL

A:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	bge B
	br FAIL

B:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	bge C
	br FAIL

C:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	bge D
	br FAIL

D:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldc.r4		float32(0x80000000)
	bge E
	br FAIL

E:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	bge F
	br FAIL

F:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	bge G
	br FAIL

G:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	bge H
	br FAIL

H:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	bge FAIL


	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	bge I
	br FAIL

I:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	bge J
	br FAIL

J:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	bge K
	br FAIL

K:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldc.r4		float32(0x80000000)
	bge L
	br FAIL

L:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	bge M
	br FAIL

M:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	bge N
	br FAIL

N:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	bge O
	br FAIL

O:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	bge FAIL



	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	bge P
	br FAIL

P:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	bge Q
	br FAIL

Q:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldc.r4		float32(0x80000000)
	bge R
	br FAIL

R:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	bge S
	br FAIL

S:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	bge T
	br FAIL

T:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	bge U
	br FAIL

U:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	bge FAIL



	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	bge V
	br FAIL

V:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldc.r4		float32(0x80000000)
	bge W
	br FAIL

W:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	bge X
	br FAIL

X:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	bge Y
	br FAIL

Y:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	bge Z
	br FAIL

Z:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	bge FAIL



	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	bge FAIL

	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	bge FAIL

	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	bge FAIL

	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	bge AA
	br FAIL

AA:
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x80000000)
	bge BB
	br FAIL

BB:
	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	bge CC
	br FAIL

CC:
	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	bge DD
	br FAIL

DD:
	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	bge EE
	br FAIL

EE:
	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	bge FAIL



	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldc.r4		float32(0x80000000)
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	bge FF
	br FAIL

FF:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	bge GG
	br FAIL

GG:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	bge HH
	br FAIL

HH:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	bge FAIL



	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldc.r4		float32(0x80000000)
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	bge II
	br FAIL

II:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	bge JJ
	br FAIL

JJ:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	bge FAIL




	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldc.r4		float32(0x80000000)
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	bge KK
	br FAIL

KK:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	bge FAIL




	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldc.r4		float32(0x80000000)
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	bge FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	bge FAIL
	br BACKCHECK


TOPASS:
	br PASS
BACKCHECK:
	ldc.r4 0x0
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
.assembly bge_r4.exe{}
