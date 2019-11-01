// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}

.assembly ldloca.exe{
}

.class public _ldloca {
.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
.locals	(int32,int32&)
	ldloca		0
	stloc		1

	ldc.i4		0xFF
	stloc	0
	ldloc	1
	ldind.i4
	ldc.i4		0xFF
	ceq
	brfalse		FAIL
	
	ldc.i4		0xAA
	stloc	0
	ldloc	1
	ldind.i4
	ldc.i4		0xAA
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
