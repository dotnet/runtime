// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public bge_un {

.field public static	int64 all
.field public static	int64 none
.field public static	int64 odd
.field public static	int64 even

.method public static	void initialize() {
.maxstack	10
	ldc.i8		0xFFFFFFFFFFFFFFFF
	stsfld	int64 bge_un::all
	ldc.i8		0x0000000000000000
	stsfld	int64 bge_un::none
	ldc.i8		0x5555555555555555
	stsfld	int64 bge_un::odd
	ldc.i8		0xAAAAAAAAAAAAAAAA
	stsfld	int64 bge_un::even
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void bge_un::initialize()


	ldsfld	int64 bge_un::all
	ldsfld	int64 bge_un::all
	bge.un A
	br FAIL

A:
	ldsfld	int64 bge_un::all
	ldsfld	int64 bge_un::none
	bge.un B
	br FAIL

B:
	ldsfld	int64 bge_un::all
	ldsfld	int64 bge_un::odd
	bge.un C
	br FAIL

C:
	ldsfld	int64 bge_un::all
	ldsfld	int64 bge_un::even
	bge.un D
	br FAIL
D:
	ldsfld	int64 bge_un::none
	ldsfld	int64 bge_un::all
	bge.un FAIL

	ldsfld	int64 bge_un::none
	ldsfld	int64 bge_un::none
	bge.un E
	br FAIL

E:
	ldsfld	int64 bge_un::none
	ldsfld	int64 bge_un::odd
	bge.un FAIL

	ldsfld	int64 bge_un::none
	ldsfld	int64 bge_un::even
	bge.un FAIL
	ldsfld	int64 bge_un::odd
	ldsfld	int64 bge_un::all
	bge.un FAIL

	ldsfld	int64 bge_un::odd
	ldsfld	int64 bge_un::none
	bge.un F
	br FAIL

F:
	ldsfld	int64 bge_un::odd
	ldsfld	int64 bge_un::odd
	bge.un G
	br FAIL

G:
	ldsfld	int64 bge_un::odd
	ldsfld	int64 bge_un::even
	bge.un FAIL
	ldsfld	int64 bge_un::even
	ldsfld	int64 bge_un::all
	bge.un	FAIL

	ldsfld	int64 bge_un::even
	ldsfld	int64 bge_un::none
	bge.un H
	br FAIL

H:
	ldsfld	int64 bge_un::even
	ldsfld	int64 bge_un::odd
	bge.un I
	br FAIL

I:
	ldsfld	int64 bge_un::even
	ldsfld	int64 bge_un::even
	bge.un BACKCHECK
	br FAIL

TOPASS:
	br PASS
BACKCHECK:
	ldc.i8 0x1
	ldc.i8 0x0
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
.assembly bge_un_i8{}
