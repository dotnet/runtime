// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly extern xunit.core {}
.assembly 'nop_Conformance'{}

.namespace nop_Conformance
{
.class public		_nop {



.method public static int32 return(int32) {
.maxstack	2
	ldarg	0
	ret
}

.method public		void _nop() {
.maxstack	0
	ret
}

.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack		5
	
	ldc.i4	0xFF
	call	int32 nop_Conformance._nop::return(int32)
	nop
	nop
	nop
	nop
	nop
	nop
	nop
	nop
	nop
	nop
	nop
	nop
	nop
	nop
	nop
	nop
	nop
	nop
	nop
	nop
	nop
	nop
	nop
	nop
	nop
	nop
	nop
	ldc.i4	0xFF
	call	int32 nop_Conformance._nop::return(int32)
	ceq
	brfalse	FAIL

PASS:
	ldc.i4 100
	br		END

FAIL:
	ldc.i4	0x00000000

END:
	ret
}
}
}
