// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
