// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}



.namespace ret_ref
{
.class public		_ret {


    .data DATA = int32(0x00000000)

.method public static		class ret_ref._ret ret_test(class ret_ref._ret) {
.maxstack	1
	ldarg	0
	ret
}

.method public		void _ret() {
.maxstack	0
	ret
}

.method public static	int32 main() {
.entrypoint
.maxstack		5

	newobj	instance 	void ret_ref._ret::_ret()
	dup
	call		class ret_ref._ret ret_ref._ret::ret_test(class ret_ref._ret)
	ceq
	brfalse		FAIL

	newobj	instance 	void ret_ref._ret::_ret()
	dup
	call		class ret_ref._ret ret_ref._ret::ret_test(class ret_ref._ret)
	ceq
	brfalse		FAIL

	newobj	instance 	void ret_ref._ret::_ret()
	dup
	call		class ret_ref._ret ret_ref._ret::ret_test(class ret_ref._ret)
	ceq
	brfalse		FAIL

	newobj	instance 	void ret_ref._ret::_ret()
	dup
	call		class ret_ref._ret ret_ref._ret::ret_test(class ret_ref._ret)
	ceq
	brfalse		FAIL

	newobj	instance 	void ret_ref._ret::_ret()
	dup
	call		class ret_ref._ret ret_ref._ret::ret_test(class ret_ref._ret)
	ceq
	brfalse		FAIL

	newobj	instance 	void ret_ref._ret::_ret()
	dup
	call		class ret_ref._ret ret_ref._ret::ret_test(class ret_ref._ret)
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



.assembly ret_ref{}
}
