// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public Pop4 {

.field public static int32 none
.field public static int32 all
.field public static int32 sentinel

.method public static	void initialize() {
.maxstack		20
	ldc.i4		0x00000000
	stsfld	int32 Pop4::none
	ldc.i4		0xFFFFFFFF
	stsfld	int32 Pop4::all
	ldc.i4		0xFAFB0C0D
	stsfld	int32 Pop4::sentinel
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
	call	void Pop4::initialize()

	ldsfld	int32 Pop4::sentinel
	ldsfld	int32 Pop4::none
	pop
	ldsfld	int32 Pop4::sentinel
	ceq
	brfalse		FAIL

	ldsfld	int32 Pop4::sentinel
	ldsfld	int32 Pop4::all
	pop
	ldsfld	int32 Pop4::sentinel
	ceq
	brfalse		FAIL
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly pop4.exe{}
