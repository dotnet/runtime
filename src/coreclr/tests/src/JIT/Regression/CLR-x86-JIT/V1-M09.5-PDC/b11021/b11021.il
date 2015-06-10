// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly b11021 { }
.method public static int32 foo() {
	.maxstack 20
	.locals(float32)
	.entrypoint
	ldc.r8 0.0
	stloc 0
ldc.i4 100
    ret
}
