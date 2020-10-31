// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}




.class public Stloc_r8 {
.field public static	float64 onlyhigh
.field public static	float64 none
.field public static	float64 onlylow

.method public static	void initialize() {
.maxstack	10
	ldc.r8		float64(0x8000000000000000)
	stsfld		float64 Stloc_r8::onlyhigh
	ldc.r8		float64(0x0000000000000000)
	stsfld		float64 Stloc_r8::none
	ldc.r8		float64(0x0000000000000001)
	stsfld		float64 Stloc_r8::onlylow
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
.locals	(float64,float64,float64)
	call	void Stloc_r8::initialize()

	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x0000000000000001)
	stloc	2
	stloc	1
	stloc	0

	ldsfld	float64 Stloc_r8::onlyhigh
	ldloc	0
	ceq
	brfalse		FAIL


	ldsfld	float64 Stloc_r8::none
	ldloc	1
	ceq
	brfalse		FAIL


	ldsfld	float64 Stloc_r8::onlylow
	ldloc	2
	ceq
	brfalse		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly stloc_r8.exe{}
