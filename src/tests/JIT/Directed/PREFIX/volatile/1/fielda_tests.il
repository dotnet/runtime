// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'fielda_tests_volatile'{}

.class public field_tests {

.field			int8 i1Field
.field			int16 i2Field
.field			int32 i4Field
.field			int64 i8Field
.field			float32 r4Field
.field			float64 r8Field
.field			class field_tests ptrField

.field static	int8 i1SField
.field static	int16 i2SField
.field static	int32 i4SField
.field static	int64 i8SField
.field static	float32 r4SField
.field static	float64 r8SField
.field static	class field_tests ptrSField


//-------------------------
// Constructor     -
//-------------------------
.method void .ctor() {

.maxstack	10
	ldarg.0
	call instance void [mscorlib]System.Object::.ctor() 
	ret
}

//-------------------------
// Initialize     -
//-------------------------
.method	void initialize() {
.maxstack	10

	ldarg	0
	ldc.i4	0x1
	volatile.
	stfld	int8 field_tests::i1Field
	//------
	ldarg	0
	ldc.i4	0x2
	volatile.
	stfld	int16 field_tests::i2Field
	//------
	ldarg	0
	ldc.i4	0x4
	volatile.
	stfld	int32 field_tests::i4Field
	//------
	ldarg	0
	ldc.i8	0x8
	volatile.
	stfld	int64 field_tests::i8Field
	//------
	ldarg	0
	ldc.r4	4.4
	volatile.
	stfld	float32 field_tests::r4Field
	//------
	ldarg	0
	ldc.r8	8.8
	volatile.
	stfld	float64 field_tests::r8Field
	//------
	ldarg	0
	ldarg	0
	volatile.
	stfld	class field_tests field_tests::ptrField
//-----------------
	ldc.i4	0x1
	volatile.
	stsfld	int8 field_tests::i1SField
	//------
	ldc.i4	0x2
	volatile.
	stsfld	int16 field_tests::i2SField
	//------
	ldc.i4	0x4
	volatile.
	stsfld	int32 field_tests::i4SField
	//------
	ldc.i8	0x8
	volatile.
	stsfld	int64 field_tests::i8SField
	//------
	ldc.r4	4.4
	volatile.
	stsfld	float32 field_tests::r4SField
	//------
	ldc.r8	8.8
	volatile.
	stsfld	float64 field_tests::r8SField
	//------
	ldarg	0
	volatile.
	stsfld	class field_tests field_tests::ptrSField

	ret
}

//-------------------------
// Entry point - Main     -
//-------------------------
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	10
.locals	(class field_tests)
//----------------
// begin field_testsing -
//----------------
	newobj	instance void field_tests::.ctor() 
	dup
	stloc	0
	call		instance void field_tests::initialize()
	//------------
	ldloc	0
	ldflda		int8 field_tests::i1Field
	volatile.
	ldind.i1
	ldc.i4		0x1
	ceq
	brfalse		FAIL
	//-----
	ldloc	0
	ldflda		int16 field_tests::i2Field
	volatile.
	ldind.i2
	ldc.i4		0x2
	ceq
	brfalse		FAIL
	//-----
	ldloc	0
	ldflda		int32 field_tests::i4Field
	volatile.
	ldind.i4
	ldc.i4		0x4
	ceq
	brfalse		FAIL
	//-----
	ldloc	0
	ldflda		int64 field_tests::i8Field
	volatile.
	ldind.i8
	ldc.i8		0x8
	ceq
	brfalse		FAIL
	//-----
	ldloc	0
	ldflda		float32 field_tests::r4Field
	volatile.
	ldind.r4
	ldc.r4		4.4
	ceq
	brfalse		FAIL
	//-----
	ldloc	0
	ldflda		float64 field_tests::r8Field
	volatile.
	ldind.r8
	ldc.r8		8.8
	ceq
	brfalse		FAIL
	//-----
	ldloc	0
	ldflda		class field_tests field_tests::ptrField
	ldind.ref
	isinst 		field_tests	
	brfalse		FAIL
//--------------------------------
	ldsflda	int8 field_tests::i1SField
	volatile.
	ldind.i1
	ldc.i4	0x1
	ceq
	brfalse	FAIL
	//-----
	ldsflda	int16 field_tests::i2SField
	volatile.
	ldind.i2
	ldc.i4	0x2
	ceq
	brfalse	FAIL
	//-----
	ldsflda	int32 field_tests::i4SField
	volatile.
	ldind.i4
	ldc.i4	0x4
	ceq
	brfalse	FAIL
	//-----
	ldsflda	int64 field_tests::i8SField
	volatile.
	ldind.i8
	ldc.i8	0x8
	ceq
	brfalse	FAIL
	//-----
	ldsflda	float32 field_tests::r4SField
	volatile.
	ldind.r4
	ldc.r4	4.4
	ceq
	brfalse	FAIL
	//-----
	ldsflda	float64 field_tests::r8SField
	volatile.
	ldind.r8
	ldc.r8	8.8
	ceq
	brfalse	FAIL
	//-----
	ldsflda	class field_tests field_tests::ptrSField
	volatile.
	ldind.ref
	isinst	field_tests
	brfalse	FAIL

//----------------
PASS:
	ldc.i4	0x64
	ret
//----------------
FAIL:
	ldc.i4	0x0
	ret
}
}
