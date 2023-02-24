// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}

.class public ldind_ref {

.method public	void ldind_ref() {
.maxstack		0
	ret
}

.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack		2
.locals	(class ldind_ref)
	newobj	instance 	void ldind_ref::ldind_ref()
	stloc	0
	ldloca		0
	ldind.ref
	ldloc	0
	ceq
	brfalse		FAIL
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ldind_ref{}
