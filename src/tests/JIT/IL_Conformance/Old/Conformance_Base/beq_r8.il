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
	beq R8INFMAX
	br FAIL

R8INFMAX:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	ldc.r8		float64(0x8000000000000000)
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	beq FAIL


	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	beq R8MAXONE
	br FAIL

R8MAXONE:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	ldc.r8		float64(0x8000000000000000)
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	beq FAIL



	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	beq R8ONEZERO
	br FAIL

R8ONEZERO:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	ldc.r8		float64(0x8000000000000000)
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	beq FAIL



	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	beq R8ZERONEGZERO
	br FAIL

R8ZERONEGZERO:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	ldc.r8		float64(0x8000000000000000)
	beq R8ZERONEGONE
	br FAIL

R8ZERONEGONE:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	beq FAIL



	ldc.r8		float64(0x8000000000000000)
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	beq FAIL

	ldc.r8		float64(0x8000000000000000)
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	beq FAIL

	ldc.r8		float64(0x8000000000000000)
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	beq FAIL

	ldc.r8		float64(0x8000000000000000)
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	beq R8NEGZERONEGZERO
	br FAIL

R8NEGZERONEGZERO:
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x8000000000000000)
	beq R8NEGZEROONE
	br FAIL

R8NEGZEROONE:
	ldc.r8		float64(0x8000000000000000)
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	beq FAIL

	ldc.r8		float64(0x8000000000000000)
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	beq FAIL

	ldc.r8		float64(0x8000000000000000)
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	beq FAIL

	ldc.r8		float64(0x8000000000000000)
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	beq FAIL



	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	ldc.r8		float64(0x8000000000000000)
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	beq R8NEGONEMIN
	br FAIL

R8NEGONEMIN:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	beq FAIL



	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	ldc.r8		float64(0x8000000000000000)
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	beq R8MINNEGINF
	br FAIL

R8MINNEGINF:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	beq FAIL




	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	ldc.r8		float64(0x8000000000000000)
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	beq R8NEGINFNAN
	br FAIL

R8NEGINFNAN:
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	beq FAIL




	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::inf_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::max_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::one_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::zero_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	ldc.r8		float64(0x8000000000000000)
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_one_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::min_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::_inf_r8
	beq FAIL

	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	ldloc 0
	ldfld float64 [ConvDLL]ConvDLL::NaN_r8
	beq FAIL
	br BACKCHECK


TOPASS:
	br PASS
BACKCHECK:
	ldc.r8 0x0
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
.assembly beq_r8.exe{}
