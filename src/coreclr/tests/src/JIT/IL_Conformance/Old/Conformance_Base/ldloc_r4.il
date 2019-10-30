// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}

.class public _ldloc {
.field public static	float32 onlyhigh
.field public static	float32 none
.field public static	float32 onlylow

.method public static	void initialize() {
.maxstack	10
	ldc.r4		float32(0x80000000)
	stsfld	float32 _ldloc::onlyhigh
	ldc.r4		float32(0x00000000)
	stsfld	float32 _ldloc::none
	ldc.r4		float32(0x00000001)
	stsfld	float32 _ldloc::onlylow
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
.locals	(float32,float32,float32)
	call	void _ldloc::initialize()

	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x00000001)
	stloc	2
	stloc	1
	stloc	0

	ldsfld	float32 _ldloc::onlyhigh
	ldloc	0
	ceq
	brfalse		FAIL

	ldsfld	float32 _ldloc::none
	ldloc	1
	ceq
	brfalse		FAIL

	ldsfld	float32 _ldloc::onlylow
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
.assembly ldloc_r4.exe{}
