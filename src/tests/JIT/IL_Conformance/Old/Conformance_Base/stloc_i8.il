// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}




.class public Stloc_I8 {
.field public static	int64 onlyhigh
.field public static	int64 all
.field public static	int64 none
.field public static	int64 onlylow
.field public static	int64 nothigh


.method public static	void initialize() {
.maxstack	10
	ldc.i8		0x8000000000000000
	stsfld	int64 Stloc_I8::onlyhigh
	ldc.i8		0xFFFFFFFFFFFFFFFF
	stsfld	int64 Stloc_I8::all
	ldc.i8		0x0000000000000000
	stsfld	int64 Stloc_I8::none
	ldc.i8		0x0000000000000001
	stsfld	int64 Stloc_I8::onlylow
	ldc.i8		0x7FFFFFFFFFFFFFFF
	stsfld	int64 Stloc_I8::nothigh
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
.locals	(int64,int64,int64,int64,int64)
	call	void Stloc_I8::initialize()

	ldc.i8		0x8000000000000000
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0x0000000000000000
	ldc.i8		0x0000000000000001
	ldc.i8		0x7FFFFFFFFFFFFFFF
	stloc	4
	stloc	3
	stloc	2
	stloc	1
	stloc	0

	ldsfld	int64 Stloc_I8::onlyhigh
	ldloc	0
	ceq
	brfalse		FAIL

	ldsfld	int64 Stloc_I8::all
	ldloc	1
	ceq
	brfalse		FAIL

	ldsfld	int64 Stloc_I8::none
	ldloc	2
	ceq
	brfalse		FAIL

	ldsfld	int64 Stloc_I8::onlylow
	ldloc	3
	ceq
	brfalse		FAIL

	ldsfld	int64 Stloc_I8::nothigh
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
.assembly stloc_i8.exe{}
