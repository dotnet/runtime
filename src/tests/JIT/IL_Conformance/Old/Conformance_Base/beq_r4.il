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
	beq R4INFMAX
	br FAIL

R4INFMAX:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldc.r4		float32(0x80000000)
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	beq FAIL


	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	beq R4MAXONE
	br FAIL

R4MAXONE:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldc.r4		float32(0x80000000)
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	beq FAIL



	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	beq R4ONEZERO
	br FAIL

R4ONEZERO:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldc.r4		float32(0x80000000)
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	beq FAIL



	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	beq R4ZERONEGZERO
	br FAIL

R4ZERONEGZERO:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldc.r4		float32(0x80000000)
	beq R4ZERONEGONE
	br FAIL

R4ZERONEGONE:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	beq FAIL



	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	beq FAIL

	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	beq FAIL

	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	beq FAIL

	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	beq R4NEGZERONEGZERO
	br FAIL

R4NEGZERONEGZERO:
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x80000000)
	beq R4NEGZEROONE
	br FAIL

R4NEGZEROONE:
	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	beq FAIL

	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	beq FAIL

	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	beq FAIL

	ldc.r4		float32(0x80000000)
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	beq FAIL



	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldc.r4		float32(0x80000000)
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	beq R4NEGONEMIN
	br FAIL

R4NEGONEMIN:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	beq FAIL



	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldc.r4		float32(0x80000000)
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	beq R4MINNEGINF
	br FAIL

R4MINNEGINF:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	beq FAIL




	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldc.r4		float32(0x80000000)
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	beq R4NEGINFNAN
	br FAIL

R4NEGINFNAN:
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	beq FAIL




	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::inf_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::max_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::one_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::zero_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldc.r4		float32(0x80000000)
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_one_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::min_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::_inf_r4
	beq FAIL

	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	ldloc 0
	ldfld float32 [ConvDLL]ConvDLL::NaN_r4
	beq FAIL
	br BACKCHECK


TOPASS:
	br PASS
BACKCHECK:
	ldc.r4 0x0
	dup
	beq TOPASS
	br FAIL
	
PASS:
	ldc.i4		100
	ret
FAIL:
	ldc.i4		0x0
	ret

}
.assembly beq_r4.exe{}
