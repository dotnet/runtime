// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}


.namespace ret_i4
{
.class public		_ret {



.method public static		int32 ret_test(int32) {
.maxstack	1
	ldarg	0
	ret
}

.method public		void _ret() {
.maxstack	0
	ret
}

.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack		5
	ldc.i4		0xAAAAAAAA
	call	int32 ret_i4._ret::ret_test(int32)
	ldc.i4		0xAAAAAAAA
	ceq
	brfalse		FAIL

	ldc.i4		0x55555555
	call	int32 ret_i4._ret::ret_test(int32)
	ldc.i4		0x55555555
	ceq
	brfalse		FAIL

	ldc.i4		0x7FFFFFFF
	call	int32 ret_i4._ret::ret_test(int32)
	ldc.i4		0x7FFFFFFF
	ceq
	brfalse		FAIL

	ldc.i4		0x00000001
	call	int32 ret_i4._ret::ret_test(int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL

	ldc.i4		0x00000000
	call	int32 ret_i4._ret::ret_test(int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL

	ldc.i4		0xFFFFFFFF
	call	int32 ret_i4._ret::ret_test(int32)
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL

	ldc.i4		0x80000000
	call	int32 ret_i4._ret::ret_test(int32)
	ldc.i4		0x80000000
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



.assembly ret_i4{}
}
