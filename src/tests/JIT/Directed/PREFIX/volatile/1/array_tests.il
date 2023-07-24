// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'array_tests_volatile'{}

.class value sealed valClass {
	.field		int32 Field
}

.class public	array_tests {

.field static	int8[] i1
.field static	unsigned int8[] u1
.field static	int16[] i2
.field static	unsigned int16[] u2
.field static	int32[] i4
.field static	int64[] i8
.field static	float32[] r4
.field static	float64[] r8
.field static	class array_tests[] obj
.field static	value class valClass[] val

.field static	native int[] i

//-------------------------
// Constructor -
//-------------------------
.method void .ctor() {

.maxstack	10
	ldarg.0
	call instance void [mscorlib]System.Object::.ctor()
	ret
}


//-------------------------
// Initialize -
//-------------------------
.method static void initialize() {
.maxstack	10
.locals	(int32, value class valClass)

	ldc.i4	0x00000004
	newarr	[mscorlib]System.SByte
	volatile.
	stsfld	int8[] array_tests::i1

	ldc.i4	0x00000004
	newarr	[mscorlib]System.Byte
	volatile.
	stsfld	unsigned int8[] array_tests::u1

	ldc.i4	0x00000004
	newarr	[mscorlib]System.Int16
	volatile.
	stsfld	int16[] array_tests::i2

	ldc.i4	0x00000004
	newarr	[mscorlib]System.UInt16
	volatile.
	stsfld	unsigned int16[] array_tests::u2

	ldc.i4	0x00000004
	newarr	[mscorlib]System.Int32
	volatile.
	stsfld	int32[] array_tests::i4

	ldc.i4	0x00000004
	newarr	[mscorlib]System.Int64
	volatile.
	stsfld	int64[] array_tests::i8

	ldc.i4	0x00000004
	newarr	[mscorlib]System.Single
	volatile.
	stsfld	float32[] array_tests::r4

	ldc.i4	0x00000004
	newarr	[mscorlib]System.Double
	volatile.
	stsfld	float64[] array_tests::r8

	ldc.i4	0x00000004
	newarr	array_tests
	volatile.
	stsfld	class array_tests[] array_tests::obj

	ldc.i4	0x00000004
	newarr	valClass
	volatile.
	stsfld	value class valClass[] array_tests::val

	ldc.i4	0x00000004
	newarr	[mscorlib]System.IntPtr
	volatile.
	stsfld	native int[] array_tests::i

//---------------------------------

BEGIN:
	ldc.i4		0x00000000
	stloc	0

LOOP:
	// -- initialize 4 element array of i1 --
	volatile.
	ldsfld		int8[] array_tests::i1
	ldloc		0
	dup
	ldc.i4		0x10
	add
	stelem.i1
	// -- initialize 4 element array of u1 --
	volatile.
	ldsfld		unsigned int8[] array_tests::u1
	ldloc	0
	dup
	ldc.i4		0x10
	add
	stelem.i1
	// -- initialize 4 element array of i2 --
	volatile.
	ldsfld		int16[] array_tests::i2
	ldloc	0
	dup
	ldc.i4		0x1000
	add
	stelem.i2
	// -- initialize 4 element array of u2 --
	volatile.
	ldsfld		unsigned int16[] array_tests::u2
	ldloc	0
	dup
	ldc.i4		0x1000
	add
	stelem.i2
	// -- initialize 4 element array of i4 --
	volatile.
	ldsfld		int32[] array_tests::i4
	ldloc	0
	dup
	ldc.i4		0x10000000
	add
	stelem.i4
	// -- initialize 4 element array of i8 --
	volatile.
	ldsfld		int64[] array_tests::i8
	ldloc	0
	dup
	conv.i8
	ldc.i8		0x1000000000000000
	add
	stelem.i8
	// -- initialize 4 element array of r4 --
	volatile.
	ldsfld		float32[] array_tests::r4
	ldloc	0
	dup
	conv.r4
	ldc.r4		float32(0x00000000)
	add
	stelem.r4
	// -- initialize 4 element array of r8 --
	volatile.
	ldsfld		float64[] array_tests::r8
	ldloc	0
	dup
	conv.r8
	ldc.r8		float64(0x0000000000000000)
	add
	stelem.r8
	// -- initialize class array_tests array --
	volatile.
	ldsfld		class array_tests[] array_tests::obj
	ldloc			0
	newobj		instance void array_tests::.ctor()
	stelem.ref
	// -- initialize value class valClass array --
	volatile.
	ldsfld		value class valClass[] array_tests::val
	ldloc		0x0
        ldelema		valClass
	ldloc		0x1
	stobj		valClass

	// -- initialize 4 element array of i --
	volatile.
	ldsfld		native int[] array_tests::i
	ldloc	0
	dup
	ldc.i4		0x10000000
	add
	stelem.i
//-------------

	ldloc	0
	ldc.i4		0x00000003
	ceq
	brtrue		END
	ldloc	0
	ldc.i4		0x00000001
	add
	stloc	0
	br			LOOP
END:
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
.locals	(int32)
//----------------
// begin testing -
//----------------
	call	void array_tests::initialize()
//----------------	
BEGIN:
	ldc.i4		0x00
	stloc	0
//---------------------------------
LOOP:
	volatile.
	ldsfld		int8[] array_tests::i1
	ldloc	0
	ldelem.i1
	ldloc	0
	ldc.i4		0x00000010
	add
	ceq
	brfalse		FAIL
	//----------------	
	volatile.
	ldsfld		unsigned int8[] array_tests::u1
	ldloc	0
	ldelem.u1
	ldloc	0
	ldc.i4		0x00000010
	add
	ceq
	brfalse		FAIL
	//----------------	
	volatile.
	ldsfld		int16[] array_tests::i2
	ldloc	0
	ldelem.i2
	ldloc	0
	ldc.i4		0x00001000
	add
	ceq
	brfalse		FAIL
	//----------------	
	volatile.
	ldsfld		unsigned int16[] array_tests::u2
	ldloc	0
	ldelem.u2
	ldloc	0
	ldc.i4		0x00001000
	add
	ceq
	brfalse		FAIL
	//----------------	
	volatile.
	ldsfld		int32[] array_tests::i4
	ldloc	0
	ldelem.i4
	ldloc	0
	ldc.i4		0x10000000
	add
	ceq
	brfalse		FAIL
	//----------------	
	volatile.
	ldsfld		int64[] array_tests::i8
	ldloc	0
	ldelem.i8
	ldloc	0
	conv.i8
	ldc.i8		0x1000000000000000
	add
	ceq
	brfalse		FAIL
	//----------------	
	volatile.
	ldsfld		float32[] array_tests::r4
	ldloc	0
	ldelem.r4
	ldloc	0
	conv.r4
	ldc.r4		float32(0x00000000)
	add
	ceq
	brfalse		FAIL
	//----------------	
	volatile.
	ldsfld		float64[] array_tests::r8
	ldloc	0
	ldelem.r8
	ldloc	0
	conv.r8
	ldc.r8		float64(0x0000000000000000)
	add
	ceq
	brfalse		FAIL
	//----------------	
	volatile.
	ldsfld		class array_tests[] array_tests::obj
	ldloc	0
	ldelem.ref
	isinst		array_tests
	brfalse		FAIL
	//----------------	
	volatile.
	ldsfld		value class valClass[] array_tests::val
	ldloc			0
	ldelema		valClass
	volatile.
	ldobj		valClass
	box		valClass
	isinst		valClass
	brfalse		FAIL

	volatile.
	ldsfld		native int[] array_tests::i
	ldloc	0
	ldelem.i
	ldloc	0
	ldc.i4		0x10000000
	add
	ceq
	brfalse		FAIL
//----------------------------------
	ldloc	0
	ldc.i4		0x00000003
	ceq
	brtrue		PASS
	ldloc	0
	ldc.i4		0x00000001
	add
	stloc	0
	br			LOOP


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
