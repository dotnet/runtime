// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public bge_un {

.field public static	int32 all
.field public static	int32 none
.field public static	int32 odd
.field public static	int32 even

.method public static	void initialize() {
.maxstack	10
	ldc.i4		0xFFFFFFFF
	stsfld	int32 bge_un::all
	ldc.i4		0x00000000
	stsfld	int32 bge_un::none
	ldc.i4		0x55555555
	stsfld	int32 bge_un::odd
	ldc.i4		0xAAAAAAAA
	stsfld	int32 bge_un::even
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void bge_un::initialize()


	ldsfld	int32 bge_un::all
	ldsfld	int32 bge_un::all
	bge.un A
	br FAIL

A:
	ldsfld	int32 bge_un::all
	ldsfld	int32 bge_un::none
	bge.un B
	br FAIL

B:
	ldsfld	int32 bge_un::all
	ldsfld	int32 bge_un::odd
	bge.un C
	br FAIL

C:
	ldsfld	int32 bge_un::all
	ldsfld	int32 bge_un::even
	bge.un _C
	br FAIL
_C:
	ldsfld	int32 bge_un::none
	ldsfld	int32 bge_un::all
	bge.un FAIL

	ldsfld	int32 bge_un::none
	ldsfld	int32 bge_un::none
	bge.un D
	br FAIL

D:
	ldsfld	int32 bge_un::none
	ldsfld	int32 bge_un::odd
	bge.un FAIL

	ldsfld	int32 bge_un::none
	ldsfld	int32 bge_un::even
	bge.un FAIL
	ldsfld	int32 bge_un::odd
	ldsfld	int32 bge_un::all
	bge.un FAIL

	ldsfld	int32 bge_un::odd
	ldsfld	int32 bge_un::none
	bge.un E
	br FAIL

E:
	ldsfld	int32 bge_un::odd
	ldsfld	int32 bge_un::odd
	bge.un F
	br FAIL

F:
	ldsfld	int32 bge_un::odd
	ldsfld	int32 bge_un::even
	bge.un FAIL
	ldsfld	int32 bge_un::even
	ldsfld	int32 bge_un::all
	bge.un FAIL

	ldsfld	int32 bge_un::even
	ldsfld	int32 bge_un::none
	bge.un H
	br FAIL

H:
	ldsfld	int32 bge_un::even
	ldsfld	int32 bge_un::odd
	bge.un I
	br FAIL

I:
	ldsfld	int32 bge_un::even
	ldsfld	int32 bge_un::even
	bge.un BACKCHECK

	br FAIL
TOPASS:
	br PASS
BACKCHECK:
	ldc.i4 0x1
	ldc.i4 0x0
	bge.un TOPASS
	br FAIL


PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly bge_un_i4{}
