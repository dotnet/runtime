// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly 'ldtoken'{}



.class public auto ansi _ldtoken {
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.locals	()
.maxstack	2
	ldtoken	method instance int32 _ldtoken::test(int32)
	pop


PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}


.class public	_ldtoken	 {

.field public static int32 PTR

.method public	void _ldtoken() {
.maxstack	4
	ret
}

.method public	int32 test(int32) {
.maxstack	2
	ldarg	1
	ldc.i4	0x2
	mul
	ret
}
}
