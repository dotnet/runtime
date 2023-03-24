// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly extern xunit.core {}
.assembly 'ret_Conformance'{}

.namespace ret_Conformance
{
.class public		_ret {



.method public static		void ret_test() {
.maxstack	0
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
	ldc.i4	0xFEFE
	call	void ret_Conformance._ret::ret_test()
	call	void ret_Conformance._ret::ret_test()
	call	void ret_Conformance._ret::ret_test()
	call	void ret_Conformance._ret::ret_test()
	call	void ret_Conformance._ret::ret_test()
	call	void ret_Conformance._ret::ret_test()
	call	void ret_Conformance._ret::ret_test()
	ldc.i4	0xFEFE
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

}
