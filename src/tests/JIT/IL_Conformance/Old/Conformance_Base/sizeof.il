// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public _sizeof {
.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
	sizeof int8
	ldc.i4 0x1
	bne.un FAIL

	sizeof int16
	ldc.i4 0x2
	bne.un FAIL

	sizeof int32
	ldc.i4 0x4
	bne.un FAIL

	sizeof int64
	ldc.i4 0x8
	bne.un FAIL

	sizeof unsigned int8
	ldc.i4 0x1
	bne.un FAIL

	sizeof unsigned int16
	ldc.i4 0x2
	bne.un FAIL

	sizeof unsigned int32
	ldc.i4 0x4
	bne.un FAIL

	sizeof unsigned int64
	ldc.i4 0x8
	bne.un FAIL

	sizeof float32
	ldc.i4 0x4
	bne.un FAIL

	sizeof float64
	ldc.i4 0x8
	bne.un FAIL
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly sizeof.exe{}
