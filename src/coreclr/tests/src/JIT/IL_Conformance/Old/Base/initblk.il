// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}

.assembly initblk.exe{
}

.class public _initblk {


	.field public static int32 DATA

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
	ldsflda		int32 _initblk::DATA
	ldc.i4		0xAA
	ldc.i4		4
	initblk
	ldsflda		int32 _initblk::DATA
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
