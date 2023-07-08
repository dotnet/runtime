// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.namespace add_i
{
.class public _add {


	.field public static int32 PTR

.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack		20
	ldc.i4	0x000000FF
	ldsflda	int32 add_i._add::PTR
	add
	conv.i4
	ldc.i4	0x000000FF
	ldsflda	int32 add_i._add::PTR
	add
	conv.i4
	ceq
	brfalse		FAIL

	ldsflda	int32 add_i._add::PTR
	conv.i4
	ldsflda	int32 add_i._add::PTR
	conv.i4
	add
	ldsflda	int32 add_i._add::PTR
	conv.i4
	ldsflda	int32 add_i._add::PTR
	conv.i4
	add
	ceq
	brfalse		FAIL
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly add_i{}
}
