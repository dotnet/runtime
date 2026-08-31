// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}


.namespace bgt_u
{
.class public _bgt {


.field public static int32 PTR

.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack		10
.locals	(int32*,int32*)
	ldsflda		int32 bgt_u._bgt::PTR
	stloc	0
	ldsflda		int32 bgt_u._bgt::PTR
	ldc.i4		0x2
	add
	stloc	1

	ldloc	0
	ldloc	1
	bgt FAIL

	ldloc	1
	ldloc	0
	bgt A
	br FAIL

A:
	ldloc	0
	ldloc	0
	bgt FAIL


TOPASS:
	br PASS
BACKCHECK:
	ldloc	1
	ldloc	0
	bgt PASS
	br FAIL

PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly bgt_u{}
}
