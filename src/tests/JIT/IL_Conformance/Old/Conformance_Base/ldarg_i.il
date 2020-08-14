// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}




.class public _ldarg {

.field public static int32 FOO
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
	ldsflda		int32 _ldarg::FOO
	ldc.i4		0x2
	add
	conv.i4
	ldsflda		int32 _ldarg::FOO		
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
.assembly ldarg_i.exe{}
