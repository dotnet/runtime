// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


.assembly extern legacy library mscorlib {}
.assembly ldlen.exe{}

.class public _ldlen {

.field public static	int32[] arr

.method public static void initialize() {
.maxstack	2
	ldc.i4		0xFF
	newarr		[mscorlib]System.Int32
	stsfld		int32[] _ldlen::arr
	ret
}

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
	call	void _ldlen::initialize()

	ldsfld	int32[] _ldlen::arr
	ldlen
	ldc.i4	0xFF
	ceq
	brfalse	FAIL

PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
