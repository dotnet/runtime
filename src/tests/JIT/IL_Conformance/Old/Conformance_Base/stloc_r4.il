// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}




.class public Stloc_r4 {

.field public static	float32 onlyhigh
.field public static	float32 none
.field public static	float32 onlylow

.method public static	void initialize() {
.maxstack	10
	ldc.r4		float32(0x80000000)
	stsfld		float32 Stloc_r4::onlyhigh
	ldc.r4		float32(0x00000000)
	stsfld		float32 Stloc_r4::none
	ldc.r4		float32(0x00000001)
	stsfld		float32 Stloc_r4::onlylow
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
.locals	(float32,float32,float32)
	call	void Stloc_r4::initialize()

	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x00000001)
	stloc	2
	stloc	1
	stloc	0

	ldsfld	float32 Stloc_r4::onlyhigh
	ldloc	0
	ceq
	brfalse		FAIL

	ldsfld	float32 Stloc_r4::none
	ldloc	1
	ceq
	brfalse		FAIL

	ldsfld	float32 Stloc_r4::onlylow
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
.assembly stloc_r4.exe{}
