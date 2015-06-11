// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly initblk.exe{}

.class public _initblk {
	.field public static int32 DATA
.method public static int32 main() {
.entrypoint
.maxstack	10
	// -- init a 4 byte block of memory to 0xAAAAAAAA
	ldsflda		int32 _initblk::DATA
	ldc.i4		0xAA
	ldc.i4		4
	initblk
	// -- load the 4 bytes of memory _and be sure it is 0xAAAAAAAA
	ldsflda		int32 _initblk::DATA
	ldind.i4
	ldc.i4		0xAAAAAAAA
	ceq
	brfalse		FAIL
	ldc.i4	100
	ret
FAIL:
	ldc.i4	0
	ret
}
}
