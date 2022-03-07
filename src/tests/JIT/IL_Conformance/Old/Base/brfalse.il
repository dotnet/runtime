// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.assembly 'brfalse'{}

.class public _brfalse {

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10

	ldc.i4	0x0
	brfalse PASS

FAIL:
	ldc.i4	0x0
	ret
PASS:
	ldc.i4 100
	ret
}
}
