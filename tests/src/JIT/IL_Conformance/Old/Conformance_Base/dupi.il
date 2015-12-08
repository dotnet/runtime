// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}


.class public _dup {


.field public static int32 FOO

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
	ldsflda		int32 _dup::FOO
	dup
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
.assembly dupi.exe{}
