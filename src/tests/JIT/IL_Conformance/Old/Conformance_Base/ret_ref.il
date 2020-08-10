// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}



.class public		_ret {


    .data DATA = int32(0x00000000)

.method public static		class _ret ret_test(class _ret) {
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
.maxstack		5

	newobj	instance 	void _ret::_ret()
	dup
	call		class _ret _ret::ret_test(class _ret)
	ceq
	brfalse		FAIL

	newobj	instance 	void _ret::_ret()
	dup
	call		class _ret _ret::ret_test(class _ret)
	ceq
	brfalse		FAIL

	newobj	instance 	void _ret::_ret()
	dup
	call		class _ret _ret::ret_test(class _ret)
	ceq
	brfalse		FAIL

	newobj	instance 	void _ret::_ret()
	dup
	call		class _ret _ret::ret_test(class _ret)
	ceq
	brfalse		FAIL

	newobj	instance 	void _ret::_ret()
	dup
	call		class _ret _ret::ret_test(class _ret)
	ceq
	brfalse		FAIL

	newobj	instance 	void _ret::_ret()
	dup
	call		class _ret _ret::ret_test(class _ret)
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



.assembly ret_ref.exe{}
