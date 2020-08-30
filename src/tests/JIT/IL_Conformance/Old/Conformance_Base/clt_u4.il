// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.class public clt_un {

.field public static	int32 all
.field public static	int32 none
.field public static	int32 odd
.field public static	int32 even
.method public static void initialize() {
.maxstack		10
	ldc.i4		0xFFFFFFFF
	stsfld	int32 clt_un::all
	ldc.i4		0x00000000
	stsfld	int32 clt_un::none
	ldc.i4		0x55555555
	stsfld	int32 clt_un::odd
	ldc.i4		0xAAAAAAAA
	stsfld	int32 clt_un::even
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void clt_un::initialize()

	ldsfld	int32 clt_un::all
	ldsfld	int32 clt_un::all
	clt.un
	brtrue		FAIL

	ldsfld	int32 clt_un::all
	ldsfld	int32 clt_un::none
	clt.un
	brtrue		FAIL

	ldsfld	int32 clt_un::all
	ldsfld	int32 clt_un::odd
	clt.un
	brtrue		FAIL

	ldsfld	int32 clt_un::all
	ldsfld	int32 clt_un::even
	clt.un
	brtrue		FAIL
	ldsfld	int32 clt_un::none
	ldsfld	int32 clt_un::all
	clt.un
	brfalse		FAIL

	ldsfld	int32 clt_un::none
	ldsfld	int32 clt_un::none
	clt.un
	brtrue		FAIL

	ldsfld	int32 clt_un::none
	ldsfld	int32 clt_un::odd
	clt.un
	brfalse		FAIL

	ldsfld	int32 clt_un::none
	ldsfld	int32 clt_un::even
	clt.un
	brfalse		FAIL
	ldsfld	int32 clt_un::odd
	ldsfld	int32 clt_un::all
	clt.un
	brfalse		FAIL

	ldsfld	int32 clt_un::odd
	ldsfld	int32 clt_un::none
	clt.un
	brtrue		FAIL

	ldsfld	int32 clt_un::odd
	ldsfld	int32 clt_un::odd
	clt.un
	brtrue		FAIL

	ldsfld	int32 clt_un::odd
	ldsfld	int32 clt_un::even
	clt.un
	brfalse		FAIL
	ldsfld	int32 clt_un::even
	ldsfld	int32 clt_un::all
	clt.un
	brfalse		FAIL

	ldsfld	int32 clt_un::even
	ldsfld	int32 clt_un::none
	clt.un
	brtrue		FAIL

	ldsfld	int32 clt_un::even
	ldsfld	int32 clt_un::odd
	clt.un
	brtrue		FAIL

	ldsfld	int32 clt_un::even
	ldsfld	int32 clt_un::even
	clt.un
	brtrue		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly clt_u4.exe{}
