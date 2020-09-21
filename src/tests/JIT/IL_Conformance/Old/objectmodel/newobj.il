// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly newobj.exe{}


.class public	Test {

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
.class public _newobj {

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
	newobj	instance	void Test::.ctor() 
	isinst		Test
	ldnull
	ceq
	brtrue		FAIL

PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
