// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b84592'{}

.class public _initblk {
	.field public static int32 DATA
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	10
	// -- init a 4 byte block of memory to 0xAAAAAAAA
	ldsflda		int32 _initblk::DATA
	ldc.i4		0xAA
	ldc.i4		4
	initblk
	// -- load the 4 bytes of memory _and be sure it is 0xAAAAAAAA
	ldsflda		int32 _initblk::DATA
	ldind.i4
	ldc.i4		0xAAAAAAAA
	ceq
	brfalse		FAIL
	ldc.i4	100
	ret
FAIL:
	ldc.i4	0
	ret
}
}
