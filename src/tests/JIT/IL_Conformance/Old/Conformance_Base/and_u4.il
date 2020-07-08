// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public _and {

.field public static	int32 ALL
.field public static	int32 NONE
.field public static	int32 ODD
.field public static	int32 EVEN
.field public static	int32 HIGH
.field public static	int32 LOW
.field public static	int32 RW


.method public static void initialize() {
.maxstack		20
	ldc.i4	0xFFFFFFFF
	stsfld	int32 _and::ALL
	ldc.i4	0x00000000
	stsfld	int32 _and::NONE
	ldc.i4	0x55555555
	stsfld	int32 _and::ODD
	ldc.i4	0xAAAAAAAA
	stsfld	int32 _and::EVEN
	ldc.i4	0x80000001
	stsfld	int32 _and::HIGH
	ldc.i4	0x00000001
	stsfld	int32 _and::LOW
	ldc.i4	0xF155E54A
	stsfld	int32 _and::RW
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
	call	void _and::initialize()

	ldsfld	int32 _and::ALL
	ldsfld	int32 _and::ALL
	and
	ldc.i4	0xFFFFFFFF
	ceq
	brfalse	FAIL

	ldsfld	int32 _and::ALL
	ldsfld	int32 _and::NONE
	and
	ldc.i4	0x00000000
	ceq
	brfalse	FAIL

	ldsfld	int32 _and::NONE
	ldsfld	int32 _and::ALL
	and
	ldc.i4	0x00000000
	ceq
	brfalse	FAIL

	ldsfld	int32 _and::NONE
	ldsfld	int32 _and::NONE
	and
	ldc.i4	0x00000000
	ceq
	brfalse	FAIL

	ldsfld	int32 _and::EVEN
	ldsfld	int32 _and::EVEN
	and
	ldc.i4	0xAAAAAAAA
	ceq
	brfalse	FAIL

	ldsfld	int32 _and::EVEN
	ldsfld	int32 _and::ODD
	and
	ldc.i4	0x00000000
	ceq
	brfalse	FAIL

	ldsfld	int32 _and::HIGH
	ldsfld	int32 _and::LOW
	and
	ldc.i4	0x00000001
	ceq
	brfalse	FAIL

	ldsfld	int32 _and::LOW
	ldsfld	int32 _and::HIGH
	and
	ldc.i4	0x00000001
	ceq
	brfalse	FAIL

	ldsfld	int32 _and::RW
	ldc.i4	0xA4710EDC
	and
	ldc.i4	0xA0510448
	ceq
	brfalse	FAIL
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly and_u4.exe{}
