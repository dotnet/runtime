// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}



.class sealed public FAKENATIVE extends [mscorlib]System.ValueType
{.field native int foobar}

.class public ldind_i {



.field public static native int LOADME
.field public static native int  LOADME2
.field public static native int LOADME3
.field public static native int LOADME4



.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
	ldsflda		native int ldind_i::LOADME
	ldc.i4		0x01234567
	stind.i4
	ldsflda		native int ldind_i::LOADME
 	sizeof FAKENATIVE
	add
	ldsflda		native int ldind_i::LOADME
 	sizeof FAKENATIVE
	ldc.i4		0x2
	mul
	add
	stind.i
	ldsflda		native int ldind_i::LOADME
 	sizeof FAKENATIVE
	ldc.i4		0x2
	mul
	add
	ldc.i4		0x89ABCDEF
	conv.i
	stind.i
	ldsflda		native int ldind_i::LOADME
 	sizeof FAKENATIVE
	ldc.i4		0x3
	mul
	add
	ldsflda		native int ldind_i::LOADME
	stind.i



	ldc.i4		0x89ABCDEF
	ldsflda		native int ldind_i::LOADME
 	sizeof FAKENATIVE
	add
	ldind.i
	ldind.i4
	ceq
	brfalse		FAIL

	ldc.i4		0x01234567
	ldsflda		native int ldind_i::LOADME
 	sizeof FAKENATIVE
	ldc.i4		0x3
	mul
	add
	ldind.i
	ldind.i4
	ceq
	brfalse		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ldind_i.exe{}
