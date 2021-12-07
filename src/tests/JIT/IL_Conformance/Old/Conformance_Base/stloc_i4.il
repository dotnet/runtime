// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}



.class public Stloc_I4 {

.field public static	int32 onlyhigh
.field public static	int32 all
.field public static	int32 none
.field public static	int32 onlylow
.field public static	int32 nothigh


.method public static	void initialize() {
.maxstack	10
	ldc.i4		0x80000000
	stsfld	int32 Stloc_I4::onlyhigh
	ldc.i4		0xFFFFFFFF
	stsfld	int32 Stloc_I4::all
	ldc.i4		0x00000000
	stsfld	int32 Stloc_I4::none
	ldc.i4		0x00000001
	stsfld	int32 Stloc_I4::onlylow
	ldc.i4		0x7FFFFFFF
	stsfld	int32 Stloc_I4::nothigh
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
.locals	(int32,int32,int32,int32,int32)
	call	void Stloc_I4::initialize()


	ldc.i4		0x80000000
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	ldc.i4		0x7FFFFFFF
	stloc	4
	stloc	3
	stloc	2
	stloc	1
	stloc	0

	ldsfld	int32 Stloc_I4::onlyhigh
	ldloc	0
	ceq
	brfalse		FAIL

	ldsfld	int32 Stloc_I4::all
	ldloc	1
	ceq
	brfalse		FAIL

	ldsfld	int32 Stloc_I4::none
	ldloc	2
	ceq
	brfalse		FAIL

	ldsfld	int32 Stloc_I4::onlylow
	ldloc	3
	ceq
	brfalse		FAIL

	ldsfld	int32 Stloc_I4::nothigh
	ldloc	4
	ceq
	brfalse		FAIL
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly stloc_i4{}
