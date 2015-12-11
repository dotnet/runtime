// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


.assembly extern legacy library mscorlib {}
.assembly callstatic.exe{}

.class public TestStatic {

.method public static	int32 StaticMethodTest(int32) {
.maxstack	10

	ldarg	0
	ldc.i4		0x2
	mul
	ret

}


}
.class public callstatic {

.method public static int32 main(class System.String[]) {
.entrypoint
.maxstack	10

	ldc.i4		0x6
	call		int32 TestStatic::StaticMethodTest(int32)
	ldc.i4		0xC
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
