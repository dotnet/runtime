// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public ldnull_ref {
.method public	void ldnull_ref()	{
.maxstack		0
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20

	ldnull
	ldnull
	ceq
	brfalse	FAIL

	ldc.i4 100
	ret

FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ldnull_ref{}
