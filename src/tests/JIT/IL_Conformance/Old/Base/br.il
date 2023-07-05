// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}

.assembly 'br'{}

.namespace br_
{
.class public _br {



.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	10
	

	br PASS


FAIL:
	ldc.i4	0x0
	ret

PASS:
	ldc.i4 100
	ret
}
}
}
