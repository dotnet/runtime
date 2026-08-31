// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}


.namespace ceq_i
{
.class public _ceq {


.field public static int32 PTR

.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack		10
.locals	(int32*,int32*)
	ldsflda		int32 ceq_i._ceq::PTR
	stloc	0
	ldsflda		int32 ceq_i._ceq::PTR
	ldc.i4		0x2
	add
	stloc	1

	ldloc	0
	ldloc	1
	ceq
	brtrue		FAIL

	ldloc	1
	ldloc	0
	ceq
	brtrue		FAIL

	ldloc	0
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
.assembly ceq_i{}
}
