// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}


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

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.locals		(class _ret,int32)
.maxstack		5
	newobj		instance void _ret::_ret()
	stloc		0
	ldc.i4		0xFF
	stloc		1
	ldloc		0
	ldloca		1
	call		instance int32& _ret::ret_test(int32&)
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



.assembly ret_i.exe{}
