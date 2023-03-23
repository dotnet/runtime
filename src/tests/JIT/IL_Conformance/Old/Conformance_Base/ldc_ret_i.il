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
.namespace ldc_ret_i
{
.class public		_ret {

//---- GLOBAL DATA ----------

//---- METHODS --------------

.method public		int32& ret_test_0(int32&) {
.maxstack	1
	ldarg	1
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
.locals		(class ldc_ret_i._ret,int32)
.maxstack		5
//====== begin testing ======
	newobj		instance void ldc_ret_i._ret::_ret()
	stloc		0
	ldc.i4		0xFF
	stloc		1
	ldloc		0
	ldloca		1
	call		instance int32& ldc_ret_i._ret::ret_test_0(int32&)
	ldloca		1
	ceq
	brfalse	FAIL
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



.assembly ldc_ret_i{}

}
