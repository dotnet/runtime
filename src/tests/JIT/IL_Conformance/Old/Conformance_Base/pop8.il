// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public Pop8 {

.field public static int64 none
.field public static int64 all
.field public static int32 sentinel

.method public static	void initialize() {
.maxstack		20
	ldc.i8		0x0000000000000000
	stsfld	int64 Pop8::none
	ldc.i8		0xFFFFFFFFFFFFFFFF
	stsfld	int64 Pop8::all
	ldc.i4		0xFAFB0C0D
	stsfld	int32 Pop8::sentinel
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
	call	void Pop8::initialize()
	ldsfld	int32 Pop8::sentinel
	ldsfld	int64 Pop8::none
	pop
	ldsfld	int32 Pop8::sentinel
	ceq
	brfalse		FAIL

	ldsfld	int32 Pop8::sentinel
	ldsfld	int64 Pop8::all
	pop
	ldsfld	int32 Pop8::sentinel
	ceq
	brfalse		FAIL
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly pop8.exe{}
