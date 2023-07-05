// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}

.class public ldnull_ref {
.method public	void ldnull_ref()	{
.maxstack		0
	ret
}

.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
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
