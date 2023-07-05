// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}

.assembly ldftn_calli{}


.method public public static int32 test() {
	ldc.i4		0xFFAABBCC
	ret 
}

.class public ldftn_calli {

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	10
	ldftn		int32 test()
	calli		default int32 ()
	ldc.i4		0xFFAABBCC
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
