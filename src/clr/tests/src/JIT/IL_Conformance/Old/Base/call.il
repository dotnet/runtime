// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}



.assembly call.exe{}

.method public public static int32 test() {
	ldc.i4	0xFF
	ret
}

.class public _call {


.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
	call	int32 test()
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



