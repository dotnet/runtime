// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly 'ldvirtftn_objectmodel'{
}

.namespace ldvirtftn_objectmodel
{
.class public auto ansi ldvirtftn_objectmodel {
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.locals	(int32, class ldvirtftn_objectmodel._ldvirtftn)
.entrypoint
.maxstack	4
	ldc.i4	0x77
	stloc		0x0
	newobj	instance	void		ldvirtftn_objectmodel._ldvirtftn::_ldvirtftn()
	stloc		0x1
	ldloc		0x1
	ldloc		0x0
	ldloc		0x1
	ldvirtftn	instance int32		ldvirtftn_objectmodel._ldvirtftn::test(int32)
	calli		default	int32 (class ldvirtftn_objectmodel._ldvirtftn,int32)
	ldc.i4	0xEE
	bne.un	FAIL

	newobj	instance	void 		ldvirtftn_objectmodel.TestImplementation::TestImplementation()
	stloc		0x1
	ldloc		0x1
	ldloc		0x0
	ldloc		0x1
	ldvirtftn	instance int32		ldvirtftn_objectmodel.TestImplementation::TestMethod(int32)
	calli		default	int32 (class ldvirtftn_objectmodel.TestInterface,int32)
	ldc.i4	0xEE
	bne.un	FAIL

	newobj	instance	void 		ldvirtftn_objectmodel.TestImplementation::TestImplementation()
	stloc		0x1
	ldloc		0x1
	ldloc		0x0
	ldloc		0x1
	ldvirtftn	instance int32		ldvirtftn_objectmodel.TestInterface::TestMethod(int32)
	calli		default	int32 (class ldvirtftn_objectmodel.TestInterface,int32)
	ldc.i4	0xEE
	bne.un	FAIL

	newobj	instance	void 		ldvirtftn_objectmodel.TestImplementation::TestImplementation()
	stloc		0x1
	ldloc		0x1
	ldloc		0x0
	ldloc		0x1
	ldvirtftn	instance int32		ldvirtftn_objectmodel.TestInterface::TestMethod(int32)
	calli		default	int32 (class ldvirtftn_objectmodel.TestImplementation,int32)
	ldc.i4	0xEE
	bne.un	FAIL

PASS:
	ldc.i4 100
	br		END
FAIL:
	ldc.i4	0x0
END:
	ret
}
}

.class public	_ldvirtftn {
.method public	void _ldvirtftn() {
.maxstack	4
	ret
}

.method public	int32 test(int32) {
.maxstack	2
	ldarg	1
	ldc.i4	0x2
	mul
	ret
}

}
.class public TestInterface {
.method public virtual int32 TestMethod(int32) {
ldc.i4 55
ret
}

}
.class public TestImplementation extends ldvirtftn_objectmodel.TestInterface {

.method public	void TestImplementation() {
.maxstack	10
	ret
}

.method public	virtual int32 TestMethod(int32) {
.maxstack	10

	ldarg	1
	ldc.i4		0x2
	mul
	ret

}

}
}
