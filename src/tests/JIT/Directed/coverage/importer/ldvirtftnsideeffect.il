// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly ldvirtftnsideeffect {}
.assembly extern xunit.core {}
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.method public static class MyTest f(class MyTest)
{
ldarg.0
ret
}

//-------------------------
// Entry point - Main     -
//-------------------------
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
// ---------------------------------------------------------

// -- C L A S S --------------------------------------------
.class public	MyTest {
//# constructor ##
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
// ---------------------------------------------------------

// -- I N T E R F A C E ------------------------------------
}
.class public TestInterface {
.method public virtual int32 TestMethod(int32) {
ret
}
// ---------------------------------------------------------

// -- I M P L E M E N T A T I O N --------------------------
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
// ---------------------------------------------------------


}
