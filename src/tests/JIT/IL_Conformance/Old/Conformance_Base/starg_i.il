// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}





.class public _starg {
.field public static int32 FOO
.field public static int32 BAR
.method public static	int32 args(int32,int32) {
.maxstack	10
.locals	(int32*)
	ldarg		0
	ldarg		1
	ldarg		0
	ldarg		1
	starg		0
	starg		1
	ldarg		0
	ceq
	stloc		0
	ldarg		1
	ceq
	ldloc		0
	add
	ldc.i4		0x2
	ceq
	ret
}


.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	ldsflda		int32 _starg::FOO
	ldsflda		int32 _starg::BAR
	call	int32 _starg::args(int32,int32)
	brfalse		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly starg_i.exe{}
