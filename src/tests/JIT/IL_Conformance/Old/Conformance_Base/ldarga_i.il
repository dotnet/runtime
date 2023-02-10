// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}





.namespace ldarga_i
{
.class public _ldarga {

.field public static int32 FOO
.method public static	native int args(native int) {
.maxstack	5
	ldarga 0
	ldind.i4
	ldc.i4 0x2
	add
	ret
}


.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack		10
	ldsflda		int32 ldarga_i._ldarga::FOO
	ldc.i4		0x2
	add
	conv.i4
	ldsflda		int32 ldarga_i._ldarga::FOO
	call	native int ldarga_i._ldarga::args(native int)
	ceq
	brfalse		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ldarga_i{}
}
