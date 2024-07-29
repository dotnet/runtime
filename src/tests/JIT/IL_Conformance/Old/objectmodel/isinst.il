// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly 'isinst'{}

.namespace isinst_
{
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
.class public Sub extends isinst_.Super {

.method public void .ctor() {

.maxstack	10
	ldarg.0
	call instance void isinst_.Super::.ctor() 
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

.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	10
	newobj	instance	void isinst_.Sub::.ctor() 
	isinst		isinst_.Sub
	brfalse		FAIL

	newobj	instance	void isinst_.Sub::.ctor() 
	isinst		isinst_.Super	
	brfalse		FAIL

	newobj	instance	void isinst_.Super::.ctor() 
	isinst		isinst_.Sub
	brtrue		FAIL

PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
}
