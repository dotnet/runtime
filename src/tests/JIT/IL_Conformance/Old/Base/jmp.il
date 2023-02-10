// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}

.assembly 'jmp'{
}

.class public auto ansi _jmp {
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.locals	()
.maxstack	2
	ldc.i4	0x0
	newarr		[mscorlib]System.String
	call		int32 _jmp::jmpTest1(class [mscorlib]System.String[])
	ret
}
}



.class public	_jmp	 {
.method public	void _jmp() {
.maxstack	4
	ret
}

.method public static	int32 jmpTest1(class [mscorlib]System.String[]) {
.maxstack	4
	jmp		int32 _jmp::jmpTest2(class [mscorlib]System.String[])
	ldc.i4	0x0
	ret
}

.method public static	int32 jmpTest2(class [mscorlib]System.String[]) {
.maxstack	4
	ldc.i4 100
	ret
}

}
