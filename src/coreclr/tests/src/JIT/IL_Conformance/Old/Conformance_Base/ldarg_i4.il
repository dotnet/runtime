// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public _ldarg {
.method public static	int32 args(int32) {
.maxstack	5
	ldarg	0
	ldc.i4		0x2
	add
	ret
}


.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	ldc.i4		0x80000002
	ldc.i4		0x80000000
	call	int32 _ldarg::args(int32)
	ceq
	brfalse		FAIL

	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFF
	call	int32 _ldarg::args(int32)
	ceq
	brfalse		FAIL

	ldc.i4		0x00000002
	ldc.i4		0x00000000
	call	int32 _ldarg::args(int32)
	ceq
	brfalse		FAIL

	ldc.i4		0x00000003
	ldc.i4		0x00000001
	call	int32 _ldarg::args(int32)
	ceq
	brfalse		FAIL

	ldc.i4		0x80000001
	ldc.i4		0x7FFFFFFF
	call	int32 _ldarg::args(int32)
	ceq
	brfalse		FAIL



	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ldarg_i4.exe{}
