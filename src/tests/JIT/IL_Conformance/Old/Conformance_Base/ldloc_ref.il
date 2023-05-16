// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}

.namespace ldloc_ref
{
.class public _ldloc {
.method public	void _ldloc() {
.maxstack		0
	ret
}

.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack		2
.locals	(class ldloc_ref._ldloc)
	newobj	instance 	void ldloc_ref._ldloc::_ldloc()
	dup
	stloc		0

	ldloc		0
	ceq
	brfalse		FAIL
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ldloc_ref{}
}
