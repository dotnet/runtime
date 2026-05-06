// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}


.namespace ret_i
{
.class public		_ret {


.method public		int32& ret_test(int32&) {
.maxstack	1
	ldarg	1
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
.locals		(class ret_i._ret,int32)
.maxstack		5
	newobj		instance void ret_i._ret::_ret()
	stloc		0
	ldc.i4		0xFF
	stloc		1
	ldloc		0
	ldloca		1
	call		instance int32& ret_i._ret::ret_test(int32&)
	ldloca		1
	ceq
	brfalse	FAIL

PASS:
	ldc.i4 100
	br		END

FAIL:
	ldc.i4	0x00000000

END:
	ret
}
}



.assembly ret_i{}
}
