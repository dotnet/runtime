// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}


.namespace dup8
{
.class public _dup {

.field public static	int32 sentinel
.field public static	int64 none
.field public static	int64 all

.method public static	void initialize() {
.maxstack		20
	ldc.i8		0x0000000000000000
	stsfld	int64 dup8._dup::none
	ldc.i8		0xFFFFFFFFFFFFFFFF
	stsfld	int64 dup8._dup::all
	ldc.i4		0xFAFB0C0D
	stsfld	int32 dup8._dup::sentinel
	ret
}

.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack		20
	call	void dup8._dup::initialize()

	ldsfld	int32 dup8._dup::sentinel

	ldsfld	int64 dup8._dup::none
	dup
	ceq
	brfalse		FAIL

	ldsfld	int64 dup8._dup::all
	dup
	ceq
	brfalse		FAIL

	dup
	ldsfld	int32 dup8._dup::sentinel
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
.assembly dup8{}
}
