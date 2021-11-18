// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public Dupptr {


.field public static int32 FOO

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
	ldc.i4		0xFAFB0C0D
	ldsflda		int32 Dupptr::FOO
	pop
	ldc.i4		0xFAFB0C0D
	ceq
	brfalse		FAIL
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly popi{}
