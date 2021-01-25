// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly callvirt.exe{}


.class public TestClass {

.method public void .ctor() {

.maxstack	10
	ldarg.0
	call instance void [mscorlib]System.Object::.ctor() 
	ret
}
.method public	int32 TestMethod(int32) {
.maxstack	10

	ldarg	1
	ldc.i4		0x2
	mul
	ret

}

}
.class public _callvirt {

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10

	newobj	instance		void TestClass::.ctor() 
	ldc.i4		0x2
	callvirt	instance	int32 TestClass::TestMethod(int32)
	ldc.i4		0x4
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
