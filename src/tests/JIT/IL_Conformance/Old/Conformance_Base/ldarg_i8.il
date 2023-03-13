// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}


.namespace ldarg_i8
{
.class public _ldarg {

.method public static	int64 args(int64) {
.maxstack	5
	ldarg	0
	ldc.i8		0x2
	add
	ret
}

.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack		10


	ldc.i8		0x8000000000000002
	ldc.i8		0x8000000000000000
	call	int64 ldarg_i8._ldarg::args(int64)
	ceq
	brfalse		FAIL

	ldc.i8		0x0000000000000001
	ldc.i8		0xFFFFFFFFFFFFFFFF
	call	int64 ldarg_i8._ldarg::args(int64)
	ceq
	brfalse		FAIL

	ldc.i8		0x0000000000000002
	ldc.i8		0x0000000000000000
	call	int64 ldarg_i8._ldarg::args(int64)
	ceq
	brfalse		FAIL

	ldc.i8		0x0000000000000003
	ldc.i8		0x0000000000000001
	call	int64 ldarg_i8._ldarg::args(int64)
	ceq
	brfalse		FAIL

	ldc.i8		0x8000000000000001
	ldc.i8		0x7FFFFFFFFFFFFFFF
	call	int64 ldarg_i8._ldarg::args(int64)
	ceq
	brfalse		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ldarg_i8{}
}
