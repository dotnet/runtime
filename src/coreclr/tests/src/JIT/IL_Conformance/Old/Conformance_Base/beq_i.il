// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}


.class public _beq {


.field public static int32 PTR

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
.locals	(int32*,int32*)
	ldsflda		int32 _beq::PTR
	stloc	0
	ldsflda		int32 _beq::PTR
	ldc.i4		0x2
	add
	stloc	1

	ldloc	0
	ldloc	1
	beq FAIL

	ldloc	1
	ldloc	0
	beq FAIL

	ldloc	0
	ldloc	0
	beq PASS
	br FAIL



PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly beq_i.exe{}
