// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public ldc_I8 {

.field public static	int64 mix
.field public static	int64 odd
.field public static	int64 even
.field public static	int64 all
.field public static	int64 none

.method public static void initialize() {
.maxstack	10
	ldc.i8		0xFAFBFCFDFAFBFCFD
	stsfld	int64 ldc_I8::mix
	ldc.i8		0x5555555555555555
	stsfld	int64 ldc_I8::odd
	ldc.i8		0xAAAAAAAAAAAAAAAA
	stsfld	int64 ldc_I8::even
	ldc.i8		0xFFFFFFFFFFFFFFFF
	stsfld	int64 ldc_I8::all
	ldc.i8		0x0000000000000000
	stsfld	int64 ldc_I8::none
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		6

	call	void ldc_I8::initialize()


	ldc.i8		0xFAFBFCFDFAFBFCFD
	ldsfld	int64 ldc_I8::mix
	ceq
	brfalse	FAIL

	ldc.i8		0x5555555555555555
	ldsfld	int64 ldc_I8::odd
	ceq
	brfalse	FAIL

	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldsfld	int64 ldc_I8::even
	ceq
	brfalse	FAIL

	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldsfld	int64 ldc_I8::all
	ceq
	brfalse	FAIL

	ldc.i8		0x0000000000000000
	ldsfld	int64 ldc_I8::none
	ceq
	brfalse	FAIL


	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ldc_i8.exe{}
