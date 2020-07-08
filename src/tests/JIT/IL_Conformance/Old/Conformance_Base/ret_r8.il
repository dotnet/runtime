// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}


.class public		_ret {



.method public static		float64 ret_test(float64) {
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

	ldc.r8	float64(0x7FF8000000000000)
	call	float64 _ret::ret_test(float64)
	ldc.r8	float64(0x7FF8000000000000)
	cgt.un
	brfalse		FAIL

	ldc.r8	float64(0x7FF0000000000000)
	call	float64 _ret::ret_test(float64)
	ldc.r8	float64(0x7FF0000000000000)
	ceq
	brfalse		FAIL

	ldc.r8	float64(0x7FEFFFFFFFFFFFFF)
	call	float64 _ret::ret_test(float64)
	ldc.r8	float64(0x7FEFFFFFFFFFFFFF)
	ceq
	brfalse		FAIL

	ldc.r8	float64(0x3FF0000000000000)
	call	float64 _ret::ret_test(float64)
	ldc.r8	float64(0x3FF0000000000000)
	ceq
	brfalse		FAIL

	ldc.r8	float64(0x0000000000000000)
	call	float64 _ret::ret_test(float64)
	ldc.r8	float64(0x0000000000000000)
	ceq
	brfalse		FAIL

	ldc.r8	float64(0x8000000000000000)
	call	float64 _ret::ret_test(float64)
	ldc.r8	float64(0x8000000000000000)
	ceq
	brfalse		FAIL

	ldc.r8	float64(0xBFF0000000000000)
	call	float64 _ret::ret_test(float64)
	ldc.r8	float64(0xBFF0000000000000)
	ceq
	brfalse		FAIL

	ldc.r8	float64(0xFFEFFFFFFFFFFFFF)
	call	float64 _ret::ret_test(float64)
	ldc.r8	float64(0xFFEFFFFFFFFFFFFF)
	ceq
	brfalse		FAIL

	ldc.r8	float64(0xFFF0000000000000)
	call	float64 _ret::ret_test(float64)
	ldc.r8	float64(0xFFF0000000000000)
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

.assembly ret_r8.exe{}
