// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}


.class public		_ret {



.method public static		float32 ret_test(float32) {
.maxstack	1
	ldarg	0
	ret
}

.method public		void _ret() {
.maxstack	0
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		12
	ldc.r4	float32(0x7FC00000)
	call	float32 _ret::ret_test(float32)
	ldc.r4	float32(0x7F800000)
	cgt.un
	brfalse		FAIL

	ldc.r4	float32(0x7F800000)
	call	float32 _ret::ret_test(float32)
	ldc.r4	float32(0x7F800000)
	ceq
	brfalse		FAIL

	ldc.r4	float32(0x7F7FFFFF)
	call	float32 _ret::ret_test(float32)
	ldc.r4	float32(0x7F7FFFFF)
	ceq
	brfalse		FAIL

	ldc.r4	float32(0x3F800000)
	call	float32 _ret::ret_test(float32)
	ldc.r4	float32(0x3F800000)
	ceq
	brfalse		FAIL

	ldc.r4	float32(0x00000000)
	call	float32 _ret::ret_test(float32)
	ldc.r4	float32(0x00000000)
	ceq
	brfalse		FAIL

	ldc.r4	float32(0x80000000)
	call	float32 _ret::ret_test(float32)
	ldc.r4	float32(0x80000000)
	ceq
	brfalse		FAIL

	ldc.r4	float32(0xBF800000)
	call	float32 _ret::ret_test(float32)
	ldc.r4	float32(0xBF800000)
	ceq
	brfalse		FAIL

	ldc.r4	float32(0xFF7FFFFF)
	call	float32 _ret::ret_test(float32)
	ldc.r4	float32(0xFF7FFFFF)
	ceq
	brfalse		FAIL

	ldc.r4	float32(0xFF800000)
	call	float32 _ret::ret_test(float32)
	ldc.r4	float32(0xFF800000)
	ceq
	brfalse		FAIL


PASS:
	ldc.i4 100
	br		END

FAIL:
	ldc.i4	0x00000000

END:
	ret
}
}

.assembly ret_r4.exe{}
