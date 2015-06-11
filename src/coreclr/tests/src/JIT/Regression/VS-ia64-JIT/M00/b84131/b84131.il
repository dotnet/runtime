// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly bge.exe{}
.method public	static int32 main() {
.entrypoint
	ldc.r4		1.0
	ldc.r4		-1.0
	bge			next3
	ldc.i4		0x1
	ret
next3:
	ldc.r4		1.0
	ldc.r4		1.0
	bge			next4
	ldc.i4		0x2
	ret
next4:
	ldc.r4		1.0
	ldc.r4		-1.0
	bge.un		pass
	ldc.i4		0x3
	ret

pass:
	ldc.i4		100
end:
	ret
}

