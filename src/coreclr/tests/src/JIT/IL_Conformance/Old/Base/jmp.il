// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}

.assembly jmp.exe{
}

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.locals	()
.maxstack	2
	jmp		int32 _jmp::jmpTest(class [mscorlib]System.String[])
	ldc.i4	0x0
	ret
}



.class public	_jmp	 {
.method public	void _jmp() {
.maxstack	4
	ret
}

.method public static	int32 jmpTest(class [mscorlib]System.String[]) {
.maxstack	4
	ldc.i4 100
	ret
}

}
