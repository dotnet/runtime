// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public ldc_r8 {
.field public static	float64 mix
.field public static	float64 none
.field public static	float64 odd
.field public static	float64 even

.method public static void initialize() {
.maxstack	10
	ldc.r8	float64(0xFAFBFCFDFAFBFCFD)
	stsfld	float64 ldc_r8::mix
	ldc.r8	float64(0x0000000000000000)
	stsfld	float64 ldc_r8::none
	ldc.r8	float64(0x5555555555555555)
	stsfld	float64 ldc_r8::odd
	ldc.r8	float64(0xAAAAAAAAAAAAAAAA)
	stsfld	float64 ldc_r8::even

	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		6
	call	void ldc_r8::initialize()

	ldc.r8	float64(0x5555555555555555)
	ldsfld	float64 ldc_r8::odd
	ceq
	brfalse	FAIL

	ldc.r8	float64(0xAAAAAAAAAAAAAAAA)
	ldsfld	float64 ldc_r8::even
	ceq
	brfalse	FAIL

	ldc.r8	float64(0x0000000000000000)
	ldsfld	float64 ldc_r8::none
	ceq
	brfalse	FAIL

	ldc.r8	float64(0xFAFBFCFDFAFBFCFD)
	ldsfld	float64 ldc_r8::mix
	ceq
	brfalse	FAIL



	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ldc_r8{}
