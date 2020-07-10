// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.class public _add {


	.field public static int32 PTR

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
	ldc.i4	0x000000FF
	ldsflda	int32 _add::PTR
	add
	conv.i4
	ldc.i4	0x000000FF
	ldsflda	int32 _add::PTR
	add
	conv.i4
	ceq
	brfalse		FAIL

	ldsflda	int32 _add::PTR
	conv.i4
	ldsflda	int32 _add::PTR
	conv.i4
	add
	ldsflda	int32 _add::PTR
	conv.i4
	ldsflda	int32 _add::PTR
	conv.i4
	add
	ceq
	brfalse		FAIL
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly add_i.exe{}
