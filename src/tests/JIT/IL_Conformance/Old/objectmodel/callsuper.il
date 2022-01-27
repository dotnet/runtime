// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly callsuper{}


.class public Super {

.method public void .ctor(){

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
.class public callsuper extends Super {

.method public void .ctor(){

.maxstack	10
	ldarg.0
	call instance void Super::.ctor()
	ret
}
.method public	int32 SuperMethod(int32) {
.maxstack	10

	ldarg	1
	ldc.i4	0x3
	mul
	ret

}
.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10

	newobj		instance void callsuper::.ctor()
	ldc.i4		0x5
	call		instance int32 Super::SuperMethod(int32)

	ldc.i4		0xA
	ceq
	brfalse		FAIL

	newobj	instance		void callsuper::.ctor()
	ldc.i4		0x5
	callvirt	instance	int32 callsuper::SuperMethod(int32)

	ldc.i4	0xF
	ceq
	brfalse	FAIL

PASS:
	ldc.i4 100
	ret

FAIL:
	ldc.i4	0x0
	ret

}
}
