// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b85565'{}
.class public s {
.method public static int32 return(int32) {
.maxstack	2
	ldarg	0
	ret
}
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	10
	ldc.i4	0x7FFFFFFF
	ldc.i4	0x1
	call	int32 s::return(int32)
	shl
	ldc.i4	0xFFFFFFFE
	ceq
	brfalse	FAIL
	ldc.i4	100
	ret
FAIL:
	ldc.i4	5
	ret
}
}
