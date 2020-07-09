// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public _dup {

.field public static	int32 sentinel
.field public static	int32 none
.field public static	int32 all

.method public static	void initialize() {
.maxstack		20
	ldc.i4		0x00000000
	stsfld	int32 _dup::none
	ldc.i4		0xFFFFFFFF
	stsfld	int32 _dup::all
	ldc.i4		0xFAFB0C0D
	stsfld	int32 _dup::sentinel
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
	call	void _dup::initialize()

	ldsfld	int32 _dup::sentinel

	ldsfld	int32 _dup::none
	dup
	ceq
	brfalse		FAIL

	ldsfld	int32 _dup::all
	dup
	ceq
	brfalse		FAIL

	dup
	ldsfld	int32 _dup::sentinel
	ceq
	brfalse		FAIL
	pop

	ldc.i4 100
	ret

FAIL:
	pop
	ldc.i4	0x0
	ret
}
}
.assembly dup4.exe{}
