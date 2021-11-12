// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly 'nop'{}

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

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
	
	ldc.i4	0xFF
	call	int32 _nop::return(int32)
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
	call	int32 _nop::return(int32)
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
