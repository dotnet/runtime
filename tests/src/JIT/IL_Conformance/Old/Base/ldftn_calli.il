// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}

.assembly ldftn_calli.exe{}


.method public public static int32 test() {
	ldc.i4		0xFFAABBCC
	ret 
}

.class public ldftn_calli {

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
	ldftn		int32 test()
	calli		default int32 ()
	ldc.i4		0xFFAABBCC
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
