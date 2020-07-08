// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


.assembly extern legacy library mscorlib {}
.assembly ldvirtftn.exe{
}

.method public static int32 main(class System.String[]) {
.locals	(int32, class _ldvirtftn)
.entrypoint
.maxstack	4
	ldc.i4	0x77
	stloc		0x0
	newobj	instance	void		_ldvirtftn::_ldvirtftn()
	stloc		0x1
	ldloc		0x1
	ldloc		0x0
	ldloc		0x1
	ldvirtftn	instance int32		_ldvirtftn::test(int32)
	calli		default	int32 (class _ldvirtftn,int32)
	ldc.i4	0xEE
	bne.un	FAIL

	newobj	instance	void 		TestImplementation::TestImplementation()
	stloc		0x1
	ldloc		0x1
	ldloc		0x0
	ldloc		0x1
	ldvirtftn	instance int32		TestImplementation::TestMethod(int32)
	calli		default	int32 (class TestInterface,int32)
	ldc.i4	0xEE
	bne.un	FAIL

	newobj	instance	void 		TestImplementation::TestImplementation()
	stloc		0x1
	ldloc		0x1
	ldloc		0x0
	ldloc		0x1
	ldvirtftn	instance int32		TestInterface::TestMethod(int32)
	calli		default	int32 (class TestInterface,int32)
	ldc.i4	0xEE
	bne.un	FAIL

	newobj	instance	void 		TestImplementation::TestImplementation()
	stloc		0x1
	ldloc		0x1
	ldloc		0x0
	ldloc		0x1
	ldvirtftn	instance int32		TestInterface::TestMethod(int32)
	calli		default	int32 (class TestImplementation,int32)
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
.class public TestImplementation extends TestInterface {

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
