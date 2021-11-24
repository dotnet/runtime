// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public _ldloc {
.method public	void _ldloc() {
.maxstack		0
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		2
.locals	(class _ldloc)
	newobj	instance 	void _ldloc::_ldloc()
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
