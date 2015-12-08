// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
