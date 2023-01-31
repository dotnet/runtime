// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.namespace starg_ref
{
.class public _starg {

.method public	void _starg() {
.maxstack		0
	ret
}

.method public static	int32 main() {
.entrypoint
.maxstack		10
.locals	(class starg_ref._starg)
	newobj	instance	void starg_ref._starg::_starg()
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
.assembly starg_ref{}
}
