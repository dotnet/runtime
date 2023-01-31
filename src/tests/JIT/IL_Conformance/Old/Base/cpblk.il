// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}

.assembly 'cpblk'{
}

.namespace cpblk_
{
.class public _cpblk {



	.field public static int32 DATA
	.field public static int32 DATA2

.method public static int32 initialize() {
	ldsflda		int32 cpblk_._cpblk::DATA
	ldc.i4		0xAA
	ldc.i4		4
	initblk
	ldsflda		int32 cpblk_._cpblk::DATA
	ldc.i4		4
	add
	ldc.i4		0xFF
	ldc.i4		4
	initblk
	ldsflda		int32 cpblk_._cpblk::DATA
	ldind.i4
	ldc.i4		0xAAAAAAAA
	ceq
	brfalse		FAIL	
	ldsflda		int32 cpblk_._cpblk::DATA
	ldc.i4		4
	add
	ldind.i4
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL	
	ldc.i4	0x00000001
	ret
FAIL:
	ldc.i4	0x00000000
	ret
}

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	10
	call	int32 cpblk_._cpblk::initialize()
	brfalse		FAIL

	ldsflda		int32 cpblk_._cpblk::DATA
	ldc.i4		4
	add
	ldsflda		int32 cpblk_._cpblk::DATA
	ldc.i4		4
	cpblk
	ldsflda		int32 cpblk_._cpblk::DATA
	ldc.i4		4
	add
	ldind.i4
	ldc.i4		0xAAAAAAAA
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
}
