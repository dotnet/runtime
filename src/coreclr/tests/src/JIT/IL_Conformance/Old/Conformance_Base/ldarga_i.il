// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}





.class public _ldarga {

.field public static int32 FOO
.method public static	int32 args(int32) {
.maxstack	5
	ldarga 0
	ldind.i4
	ldc.i4 0x2
	add
	ret
}


.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	ldsflda		int32 _ldarga::FOO
	ldc.i4		0x2
	add
	conv.i4
	ldsflda		int32 _ldarga::FOO
	call	int32 _ldarga::args(int32)
	ceq
	brfalse		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ldarga_i.exe{}
