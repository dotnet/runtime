.assembly extern mscorlib {}
.assembly extern xunit.core {}
.assembly ASSEMBLY_NAME { }
.method public static class MyTest f(class MyTest)
{
ldarg.0
ret
}

.class public auto ansi ldvirtftnsideeffect {
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.locals	(int32, class MyTest)
.entrypoint
.maxstack	4
	ldc.i4	0x77
	stloc		0x0
	newobj	instance void		MyTest::f1()
	stloc		0x1
	ldloc		0x1
	ldloc		0x0
	ldloc		0x1
	call class MyTest f(class MyTest)
	ldvirtftn	instance int32		MyTest::f2(int32)
	calli		default	int32 (class MyTest,int32)
	ldc.i4	0xEE
	bne.un	FAIL

PASS:
	ldc.i4	100
	br		END
FAIL:
	ldc.i4	0x0
END:
	ret
}
}

.class public	MyTest {
.method public void f1() {
.maxstack	4
	ret
}

.method public int32 f2(int32) {
.maxstack	2
	ldarg	1
	ldc.i4	0x2
	mul
	ret
}

}
.class public TestInterface {
.method public virtual int32 TestMethod(int32) {
ret
}

}
.class public TestImplementation extends TestInterface {

.method public void TestImplementation() {
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
