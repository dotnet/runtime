// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}


.class public ldc_r4 {

.field public static	float32 mix
.field public static	float32 none
.field public static	float32 odd
.field public static	float32 even

.method public static void initialize() {
.maxstack	10
	ldc.r4	float32(0xFAFBFCFD)
	stsfld	float32 ldc_r4::mix
	ldc.r4	float32(0x00000000)
	stsfld	float32 ldc_r4::none
	ldc.r4	float32(0x55555555)
	stsfld	float32 ldc_r4::odd
	ldc.r4	float32(0xAAAAAAAA)
	stsfld	float32 ldc_r4::even

	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		6
	call	void ldc_r4::initialize()


	ldc.r4	float32(0xFAFBFCFD)
	ldsfld	float32 ldc_r4::mix
	ceq
	brfalse	FAIL

	ldc.r4	float32(0x00000000)
	ldsfld	float32 ldc_r4::none
	ceq
	brfalse	FAIL

	ldc.r4	float32(0x55555555)
	ldsfld	float32 ldc_r4::odd
	ceq
	brfalse	FAIL

	ldc.r4	float32(0xAAAAAAAA)
	ldsfld	float32 ldc_r4::even
	ceq
	brfalse	FAIL



	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ldc_r4.exe{}
