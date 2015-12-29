// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}

.assembly unaligned.exe{
}

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.locals	(int32)
.maxstack	2
	ldc.i4		0x89ABCDEF
	stloc		0
	ldloca		0x0
	ldc.i4		0x1
	add
	ldc.i4		0x11
	unaligned.	0x1 
	stind.i1
	ldloca		0x0
	ldind.i4
	ldc.i4		0x89AB11EF
	bne.un		FAIL
PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
