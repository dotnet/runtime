// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}




.namespace stloc_i
{
.class public _stloc {


.field public static int32 LOADME

.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack		10
.locals	(int32*)
	ldsflda		int32 stloc_i._stloc::LOADME
	stloc	0

	ldsflda		int32 stloc_i._stloc::LOADME
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
.assembly stloc_i{}
}
