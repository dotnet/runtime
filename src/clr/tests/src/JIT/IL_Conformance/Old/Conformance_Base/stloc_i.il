// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}




.class public _stloc {


.field public static int32 LOADME

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
.locals	(int32*)
	ldsflda		int32 _stloc::LOADME
	stloc	0

	ldsflda		int32 _stloc::LOADME
	ldloc	0
	ceq
	brfalse		FAIL
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly stloc_i.exe{}
