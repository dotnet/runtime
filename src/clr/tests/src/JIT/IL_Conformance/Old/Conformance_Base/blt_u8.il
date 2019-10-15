// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}


.class public blt_un {

.field public static	int64 all
.field public static	int64 none
.field public static	int64 odd
.field public static	int64 even

.method public static	void initialize() {
.maxstack	10
	ldc.i8		0xFFFFFFFFFFFFFFFF
	stsfld	int64 blt_un::all
	ldc.i8		0x0000000000000000
	stsfld	int64 blt_un::none
	ldc.i8		0x5555555555555555
	stsfld	int64 blt_un::odd
	ldc.i8		0xAAAAAAAAAAAAAAAA
	stsfld	int64 blt_un::even
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void blt_un::initialize()


	ldsfld	int64 blt_un::all
	ldsfld	int64 blt_un::all
	blt.un FAIL

A:
	ldsfld	int64 blt_un::all
	ldsfld	int64 blt_un::none
	blt.un FAIL

	ldsfld	int64 blt_un::all
	ldsfld	int64 blt_un::odd
	blt.un FAIL

	ldsfld	int64 blt_un::all
	ldsfld	int64 blt_un::even
	blt.un FAIL
	ldsfld	int64 blt_un::none
	ldsfld	int64 blt_un::all
	blt.un B
	br FAIL

B:
	ldsfld	int64 blt_un::none
	ldsfld	int64 blt_un::none
	blt.un FAIL

C:
	ldsfld	int64 blt_un::none
	ldsfld	int64 blt_un::odd
	blt.un D
	br FAIL

D:
	ldsfld	int64 blt_un::none
	ldsfld	int64 blt_un::even
	blt.un E
	br FAIL
E:
	ldsfld	int64 blt_un::odd
	ldsfld	int64 blt_un::all
	blt.un F
	br FAIL

F:
	ldsfld	int64 blt_un::odd
	ldsfld	int64 blt_un::none
	blt.un FAIL

	ldsfld	int64 blt_un::odd
	ldsfld	int64 blt_un::odd
	blt.un FAIL

G:
	ldsfld	int64 blt_un::odd
	ldsfld	int64 blt_un::even
	blt.un H
	br FAIL
H:
	ldsfld	int64 blt_un::even
	ldsfld	int64 blt_un::all
	blt.un I
	br FAIL

I:
	ldsfld	int64 blt_un::even
	ldsfld	int64 blt_un::none
	blt.un FAIL

	ldsfld	int64 blt_un::even
	ldsfld	int64 blt_un::odd
	blt.un FAIL

	ldsfld	int64 blt_un::even
	ldsfld	int64 blt_un::even
	blt.un FAIL
	br BACKCHECK
TOPASS:
	br PASS
BACKCHECK:
	ldc.i8 0x0
	ldc.i8 0x1
	blt.un TOPASS
	br FAIL



PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly blt_u8.exe{}
