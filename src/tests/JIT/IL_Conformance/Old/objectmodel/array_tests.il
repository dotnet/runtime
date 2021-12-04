// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly array_tests{}

.class public value sealed valClass {
	.field public		int32 Field
}

.class public	array_tests {

.field public static	int8[] i1
.field public static	unsigned int8[] u1
.field public static	int16[] i2
.field public static	unsigned int16[] u2
.field public static	int32[] i4
.field public static	int64[] i8
.field public static	float32[] r4
.field public static	float64[] r8
.field public static	class array_tests[] obj
.field public static	value class valClass[] val

.field public static	native int[] i

.method public void .ctor() {

.maxstack	10
	ldarg.0
	call instance void [mscorlib]System.Object::.ctor()
	ret
}


.method public static void initialize() {
.maxstack	10
.locals	(int32, value class valClass)

	ldc.i4	0x00000004
	newarr	[mscorlib]System.SByte
	stsfld	int8[] array_tests::i1

	ldc.i4	0x00000004
	newarr	[mscorlib]System.Byte
	stsfld	unsigned int8[] array_tests::u1

	ldc.i4	0x00000004
	newarr	[mscorlib]System.Int16
	stsfld	int16[] array_tests::i2

	ldc.i4	0x00000004
	newarr	[mscorlib]System.UInt16
	stsfld	unsigned int16[] array_tests::u2

	ldc.i4	0x00000004
	newarr	[mscorlib]System.Int32
	stsfld	int32[] array_tests::i4

	ldc.i4	0x00000004
	newarr	[mscorlib]System.Int64
	stsfld	int64[] array_tests::i8

	ldc.i4	0x00000004
	newarr	[mscorlib]System.Single
	stsfld	float32[] array_tests::r4

	ldc.i4	0x00000004
	newarr	[mscorlib]System.Double
	stsfld	float64[] array_tests::r8

	ldc.i4	0x00000004
	newarr	array_tests
	stsfld	class array_tests[] array_tests::obj

	ldc.i4	0x00000004
	newarr	valClass
	stsfld	value class valClass[] array_tests::val

	ldc.i4	0x00000004
	newarr	[mscorlib]System.IntPtr
	stsfld	native int[] array_tests::i


BEGIN:
	ldc.i4		0x00000000
	stloc	0

LOOP:
	ldsfld		int8[] array_tests::i1
	ldloc		0
	dup
	ldc.i4		0x10
	add
	stelem.i1
	ldsfld		unsigned int8[] array_tests::u1
	ldloc	0
	dup
	ldc.i4		0x10
	add
	stelem.i1
	ldsfld		int16[] array_tests::i2
	ldloc	0
	dup
	ldc.i4		0x1000
	add
	stelem.i2
	ldsfld		unsigned int16[] array_tests::u2
	ldloc	0
	dup
	ldc.i4		0x1000
	add
	stelem.i2
	ldsfld		int32[] array_tests::i4
	ldloc	0
	dup
	ldc.i4		0x10000000
	add
	stelem.i4
	ldsfld		int64[] array_tests::i8
	ldloc	0
	dup
	conv.i8
	ldc.i8		0x1000000000000000
	add
	stelem.i8
	ldsfld		float32[] array_tests::r4
	ldloc	0
	dup
	conv.r4
	ldc.r4		float32(0x00000000)
	add
	stelem.r4
	ldsfld		float64[] array_tests::r8
	ldloc	0
	dup
	conv.r8
	ldc.r8		float64(0x0000000000000000)
	add
	stelem.r8
	ldsfld		class array_tests[] array_tests::obj
	ldloc			0
	newobj	instance		void array_tests::.ctor()
	stelem.ref
	ldsfld		value class valClass[] array_tests::val
	ldloc		0x0
    ldelema		valClass
	ldloca		0x1
	initobj		valClass
	ldloc		1
	stobj		valClass

	ldsfld		native int[] array_tests::i
	ldloc	0
	dup
	ldc.i4		0x10000000
	add
	stelem.i

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

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
.locals	(int32)
	call	void array_tests::initialize()
BEGIN:
	ldc.i4		0x00
	stloc	0
LOOP:
	ldsfld		int8[] array_tests::i1
	ldloc	0
	ldelem.i1
	ldloc	0
	ldc.i4		0x00000010
	add
	ceq
	brfalse		FAIL
	ldsfld		unsigned int8[] array_tests::u1
	ldloc	0
	ldelem.u1
	ldloc	0
	ldc.i4		0x00000010
	add
	ceq
	brfalse		FAIL
	ldsfld		int16[] array_tests::i2
	ldloc	0
	ldelem.i2
	ldloc	0
	ldc.i4		0x00001000
	add
	ceq
	brfalse		FAIL
	ldsfld		unsigned int16[] array_tests::u2
	ldloc	0
	ldelem.u2
	ldloc	0
	ldc.i4		0x00001000
	add
	ceq
	brfalse		FAIL
	ldsfld		int32[] array_tests::i4
	ldloc	0
	ldelem.i4
	ldloc	0
	ldc.i4		0x10000000
	add
	ceq
	brfalse		FAIL
	ldsfld		int64[] array_tests::i8
	ldloc	0
	ldelem.i8
	ldloc	0
	conv.i8
	ldc.i8		0x1000000000000000
	add
	ceq
	brfalse		FAIL
	ldsfld		float32[] array_tests::r4
	ldloc	0
	ldelem.r4
	ldloc	0
	conv.r4
	ldc.r4		float32(0x00000000)
	add
	ceq
	brfalse		FAIL
	ldsfld		float64[] array_tests::r8
	ldloc	0
	ldelem.r8
	ldloc	0
	conv.r8
	ldc.r8		float64(0x0000000000000000)
	add
	ceq
	brfalse		FAIL
	ldsfld		class array_tests[] array_tests::obj
	ldloc	0
	ldelem.ref
	isinst		array_tests
	brfalse		FAIL
	ldsflda		value class valClass[] array_tests::val
	ldobj		valClass
	box			valClass
	isinst		valClass
	brfalse		FAIL

	ldsfld		native int[] array_tests::i
	ldloc	0
	ldelem.i
	ldloc	0
	ldc.i4		0x10000000
	add
	ceq
	brfalse		FAIL
	ldloc	0
	ldc.i4		0x00000003
	ceq
	brtrue		PASS
	ldloc	0
	ldc.i4		0x00000001
	add
	stloc	0
	br			LOOP


PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
