// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}

.class public ldind_ref {

.method public	void ldind_ref() {
.maxstack		0
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
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
.assembly ldind_ref.exe{}
