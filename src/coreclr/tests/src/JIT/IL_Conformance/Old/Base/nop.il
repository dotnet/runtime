// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}

.assembly nop.exe{}

.class public _nop {

.method public static int32 return(int32) {
.maxstack	2
	ldarg	0
	ret
}

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
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
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
