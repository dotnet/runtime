// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}


.namespace ldarga_ref
{
.class public _ldarg {

.method public		void _ldarg() {
.maxstack	0
	ret
}

.method public	class ldarga_ref._ldarg args(class ldarga_ref._ldarg) {
.maxstack	5
	ldarga 0
	ldind.ref
	ret
}


.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.locals(class ldarga_ref._ldarg)
.maxstack		2
	newobj		instance void ldarga_ref._ldarg::_ldarg()
	stloc		0
	ldloc		0
	ldloc		0
	call		instance class ldarga_ref._ldarg ldarga_ref._ldarg::args(class ldarga_ref._ldarg)
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

.assembly ldarga_ref{}
}
