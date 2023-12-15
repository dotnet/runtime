// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}

.assembly 'ldloca'{
}

.class public _ldloca {
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	10
.locals	(int32,int32&)
	ldloca		0
	stloc		1

	ldc.i4		0xFF
	stloc	0
	ldloc	1
	ldind.i4
	ldc.i4		0xFF
	ceq
	brfalse		FAIL
	
	ldc.i4		0xAA
	stloc	0
	ldloc	1
	ldind.i4
	ldc.i4		0xAA
	ceq
	brfalse		FAIL

PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
