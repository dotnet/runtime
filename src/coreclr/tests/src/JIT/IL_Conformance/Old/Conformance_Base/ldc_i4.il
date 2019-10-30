// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}


.class public ldc_I4 {

.field public static int32 mix
.field public static int32 odd
.field public static int32 even
.field public static int32 all
.field public static int32 none

.method public static void initialize() {
.maxstack		10
	ldc.i4		0xFAFBFCFD
	stsfld	int32 ldc_I4::mix
	ldc.i4		0x55555555
	stsfld	int32 ldc_I4::odd
	ldc.i4		0xAAAAAAAA
	stsfld	int32 ldc_I4::even
	ldc.i4		0xFFFFFFFF
	stsfld	int32 ldc_I4::all
	ldc.i4		0x00000000
	stsfld	int32 ldc_I4::none
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		6
	call	void ldc_I4::initialize()

	ldc.i4		0xFAFBFCFD
	ldsfld	int32 ldc_I4::mix
	ceq
	brfalse	FAIL

	ldc.i4		0x55555555
	ldsfld	int32 ldc_I4::odd
	ceq
	brfalse	FAIL

	ldc.i4		0xAAAAAAAA
	ldsfld	int32 ldc_I4::even
	ceq
	brfalse	FAIL

	ldc.i4		0xFFFFFFFF
	ldsfld	int32 ldc_I4::all
	ceq
	brfalse	FAIL

	ldc.i4		0x00000000
	ldsfld	int32 ldc_I4::none
	ceq
	brfalse	FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ldc_i4.exe{}
