// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly extern xunit.core {}
.namespace initblk_Conformance
{
.class public		_initblk {


.field public static int32 DATA



.method public		void _initblk() {
.maxstack	0
	ret
}

.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack		5
	
	ldsflda		int32 initblk_Conformance._initblk::DATA
	ldc.i4		0xAA
	ldc.i4		4
	initblk
	ldsflda		int32 initblk_Conformance._initblk::DATA
	ldind.i4
	ldc.i4		0xAAAAAAAA
	ceq
	brfalse		FAIL


PASS:
	ldc.i4 100
	br		END

FAIL:
	ldc.i4	0x00000000

END:
	ret
}
}



.assembly 'initblk_Conformance'{}
}
