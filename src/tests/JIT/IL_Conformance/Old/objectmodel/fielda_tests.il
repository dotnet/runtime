// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern legacy library mscorlib {}
.assembly fielda_tests{}

.class public field_tests {

.field public			int8 i1Field
.field public			int16 i2Field
.field public			int32 i4Field
.field public			int64 i8Field
.field public			float32 r4Field
.field public			float64 r8Field
.field public			class field_tests ptrField

.field public static	int8 i1SField
.field public static	int16 i2SField
.field public static	int32 i4SField
.field public static	int64 i8SField
.field public static	float32 r4SField
.field public static	float64 r8SField
.field public static	class field_tests ptrSField


.method public void .ctor() {

.maxstack	10
	ldarg.0
	call instance void [mscorlib]System.Object::.ctor() 
	ret
}

.method public	void initialize() {
.maxstack	10

	ldarg	0
	ldc.i4	0x1
	stfld	int8 field_tests::i1Field
	ldarg	0
	ldc.i4	0x2
	stfld	int16 field_tests::i2Field
	ldarg	0
	ldc.i4	0x4
	stfld	int32 field_tests::i4Field
	ldarg	0
	ldc.i8	0x8
	stfld	int64 field_tests::i8Field
	ldarg	0
	ldc.r4	4.4
	stfld	float32 field_tests::r4Field
	ldarg	0
	ldc.r8	8.8
	stfld	float64 field_tests::r8Field
	ldarg	0
	ldarg	0
	stfld	class field_tests field_tests::ptrField
	ldc.i4	0x1
	stsfld	int8 field_tests::i1SField
	ldc.i4	0x2
	stsfld	int16 field_tests::i2SField
	ldc.i4	0x4
	stsfld	int32 field_tests::i4SField
	ldc.i8	0x8
	stsfld	int64 field_tests::i8SField
	ldc.r4	4.4
	stsfld	float32 field_tests::r4SField
	ldc.r8	8.8
	stsfld	float64 field_tests::r8SField
	ldarg	0
	stsfld	class field_tests field_tests::ptrSField

	ret
}

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
.locals	(class field_tests)
	newobj	instance	void field_tests::.ctor() 
	dup
	stloc	0
	call		instance void field_tests::initialize()
	ldloc	0
	ldflda		int8 field_tests::i1Field
	ldind.i1
	ldc.i4		0x1
	ceq
	brfalse		FAIL
	ldloc	0
	ldflda		int16 field_tests::i2Field
	ldind.i2
	ldc.i4		0x2
	ceq
	brfalse		FAIL
	ldloc	0
	ldflda		int32 field_tests::i4Field
	ldind.i4
	ldc.i4		0x4
	ceq
	brfalse		FAIL
	ldloc	0
	ldflda		int64 field_tests::i8Field
	ldind.i8
	ldc.i8		0x8
	ceq
	brfalse		FAIL
	ldloc	0
	ldflda		float32 field_tests::r4Field
	ldind.r4
	ldc.r4		4.4
	ceq
	brfalse		FAIL
	ldloc	0
	ldflda		float64 field_tests::r8Field
	ldind.r8
	ldc.r8		8.8
	ceq
	brfalse		FAIL
	ldloc	0
	ldflda		class field_tests field_tests::ptrField
	ldind.ref
	isinst 		field_tests	
	brfalse		FAIL
	ldsflda	int8 field_tests::i1SField
	ldind.i1
	ldc.i4	0x1
	ceq
	brfalse	FAIL
	ldsflda	int16 field_tests::i2SField
	ldind.i2
	ldc.i4	0x2
	ceq
	brfalse	FAIL
	ldsflda	int32 field_tests::i4SField
	ldind.i4
	ldc.i4	0x4
	ceq
	brfalse	FAIL
	ldsflda	int64 field_tests::i8SField
	ldind.i8
	ldc.i8	0x8
	ceq
	brfalse	FAIL
	ldsflda	float32 field_tests::r4SField
	ldind.r4
	ldc.r4	4.4
	ceq
	brfalse	FAIL
	ldsflda	float64 field_tests::r8SField
	ldind.r8
	ldc.r8	8.8
	ceq
	brfalse	FAIL
	ldsflda	class field_tests field_tests::ptrSField
	ldind.ref
	isinst	field_tests
	brfalse	FAIL

PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
