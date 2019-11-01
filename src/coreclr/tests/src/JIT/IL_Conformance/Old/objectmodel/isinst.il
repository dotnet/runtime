// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


.assembly extern legacy library mscorlib {}
.assembly isinst.exe{}

.class public	Super {

.method public void .ctor() {

.maxstack	10
	ldarg.0
	call instance void [mscorlib]System.Object::.ctor() 
	ret
}
.method public	int32 SuperMethod(int32) {
.maxstack	10

	ldarg	1
	ldc.i4		0x2
	mul
	ret

}


}
.class public Sub extends Super {

.method public void .ctor() {

.maxstack	10
	ldarg.0
	call instance void Super::.ctor() 
	ret
}
.method public	int32 SubMethod(int32) {
.maxstack	10

	ldarg	1
	ldc.i4		0x2
	mul
	ret

}


}
.class public _isinst {

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
	newobj	instance	void Sub::.ctor() 
	isinst		Sub
	brfalse		FAIL

	newobj	instance	void Sub::.ctor() 
	isinst		Super	
	brfalse		FAIL

	newobj	instance	void Super::.ctor() 
	isinst		Sub
	brtrue		FAIL

PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
