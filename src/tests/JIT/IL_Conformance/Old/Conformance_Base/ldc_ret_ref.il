// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}


//
//======================================

//---- CLASS ----------------
.namespace ldc_ret_ref
{
.class public		_ret {

//---- GLOBAL DATA ----------

    .data DATA = int32(0x00000000)

//---- METHODS --------------

.method public static		class ldc_ret_ref._ret ret_test_0(class ldc_ret_ref._ret) {
.maxstack	1
	ldarg	0
	ret

}
.method public static		class ldc_ret_ref._ret ret_test_1(class ldc_ret_ref._ret) {
.maxstack	1
	ldarg	0
	ret

}
.method public static		class ldc_ret_ref._ret ret_test_2(class ldc_ret_ref._ret) {
.maxstack	1
	ldarg	0
	ret

}
.method public static		class ldc_ret_ref._ret ret_test_3(class ldc_ret_ref._ret) {
.maxstack	1
	ldarg	0
	ret

}
.method public static		class ldc_ret_ref._ret ret_test_4(class ldc_ret_ref._ret) {
.maxstack	1
	ldarg	0
	ret

}
.method public static		class ldc_ret_ref._ret ret_test_5(class ldc_ret_ref._ret) {
.maxstack	1
	ldarg	0
	ret

}
//---- CONSTRUCTOR ----------
.method public		void _ret() {
.maxstack	0
	ret
}

//---- MAIN -----------------
.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack		5
//====== begin testing ======

	newobj	instance 	void ldc_ret_ref._ret::_ret()
	dup
	call		class ldc_ret_ref._ret ldc_ret_ref._ret::ret_test_0(class ldc_ret_ref._ret)
	ceq
	brfalse		FAIL

	newobj	instance 	void ldc_ret_ref._ret::_ret()
	dup
	call		class ldc_ret_ref._ret ldc_ret_ref._ret::ret_test_1(class ldc_ret_ref._ret)
	ceq
	brfalse		FAIL

	newobj	instance 	void ldc_ret_ref._ret::_ret()
	dup
	call		class ldc_ret_ref._ret ldc_ret_ref._ret::ret_test_2(class ldc_ret_ref._ret)
	ceq
	brfalse		FAIL

	newobj	instance 	void ldc_ret_ref._ret::_ret()
	dup
	call		class ldc_ret_ref._ret ldc_ret_ref._ret::ret_test_3(class ldc_ret_ref._ret)
	ceq
	brfalse		FAIL

	newobj	instance 	void ldc_ret_ref._ret::_ret()
	dup
	call		class ldc_ret_ref._ret ldc_ret_ref._ret::ret_test_4(class ldc_ret_ref._ret)
	ceq
	brfalse		FAIL

	newobj	instance 	void ldc_ret_ref._ret::_ret()
	dup
	call		class ldc_ret_ref._ret ldc_ret_ref._ret::ret_test_5(class ldc_ret_ref._ret)
	ceq
	brfalse		FAIL


//====== end testing ========

//---- branch here on pass --
PASS:
	ldc.i4 100
	br		END

//---- branch here on fail --
FAIL:
	ldc.i4 101

//---- return the result ----
END:
	ret
//---- END OF METHOD --------
}
//---- EOF ------------------
}



.assembly ldc_ret_ref{}

}
