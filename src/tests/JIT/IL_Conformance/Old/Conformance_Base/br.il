// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.class public _br {
.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
.locals	(int32,int32)
	ldc.i4		0x0
	dup
	stloc	0
	stloc	1

BEGIN:
	ldloc	0
	dup
	ldc.i4		0x0
	ceq
	ldloc	1
	add
	stloc	1
	ldc.i4		0x1
	add
	stloc	0
	br			TAG4
	
TAG1:
	ldloc	0
	dup
	ldc.i4		0x9
	ceq
	ldloc	1
	add
	stloc	1
	ldc.i4		0x2
	add
	stloc	0
	br			TAG3

TAG2:
	ldloc	0
	dup
	ldc.i4		0x11
	ceq
	ldloc	1
	add
	stloc	1
	ldc.i4		0x4
	add
	stloc	0
	br			END

TAG3:
	ldloc	0
	dup
	ldc.i4		0xB
	ceq
	ldloc	1
	add
	stloc	1
	ldc.i4		0x6
	add
	stloc	0
	br			TAG2

TAG4:
	ldloc	0
	dup
 	ldc.i4		0x1
	ceq
	ldloc	1
	add
	stloc	1
	ldc.i4		0x8
	add
	stloc	0
	br			TAG1

END:
	br			0
	ldloc	1
	ldc.i4		0x00000005
	ceq
	brfalse		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly br.exe{}
