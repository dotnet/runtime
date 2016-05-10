// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


.assembly extern legacy library mscorlib {}
.assembly callintf.exe{}



.class public TestInterface {
.method public void .ctor(){
	.maxstack 10
	ldarg.0
	call	instance void [mscorlib]System.Object::.ctor()
	ret
}

.method public	virtual int32 TestMethod(int32) {
        ldc.i4 55
ret
}


}
.class public	TestImplementation extends TestInterface {

.method public void .ctor(){

.maxstack	10
	ldarg.0
	call instance void TestInterface::.ctor()
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
.class public callintf {

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10

	newobj	instance		void TestImplementation::.ctor()
	ldc.i4		0x6
	callvirt	instance	int32 TestInterface::TestMethod(int32)
	ldc.i4		0xC
	ceq
	brfalse		FAIL

PASS:
	ldc.i4 100
	ret

FAIL:
	ldc.i4	0x0
	ret

}

}
