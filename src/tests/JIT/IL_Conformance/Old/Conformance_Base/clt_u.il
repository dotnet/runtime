// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public clt_un {


	.field public static int32 PTR

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
.locals	(int32*,int32*)
	ldsflda		int32 clt_un::PTR
	stloc	0
	ldsflda		int32 clt_un::PTR
	ldc.i4		0x2
	add
	stloc	1

	ldloc	0
	ldloc	1
	clt.un
	brfalse		FAIL

	ldloc	1
	ldloc	0
	clt.un
	brtrue		FAIL

	ldloc	0
	ldloc	0
	clt.un
	brtrue		FAIL



	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly clt_u{}
