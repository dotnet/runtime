// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}



.class public _and {
.field public static	int64 ALL
.field public static	int64 NONE
.field public static	int64 ODD
.field public static	int64 EVEN
.field public static	int64 HIGH
.field public static	int64 LOW
.field public static	int64 RW


.method public static void initialize() {
.maxstack		20
	ldc.i8	0xFFFFFFFFFFFFFFFF
	stsfld	int64 _and::ALL
	ldc.i8	0x0000000000000000
	stsfld	int64 _and::NONE
	ldc.i8	0x5555555555555555
	stsfld	int64 _and::ODD
	ldc.i8	0xAAAAAAAAAAAAAAAA
	stsfld	int64 _and::EVEN
	ldc.i8	0x8000000000000001
	stsfld	int64 _and::HIGH
	ldc.i8	0x0000000000000001
	stsfld	int64 _and::LOW
	ldc.i8	0xF155E54AF155E54A
	stsfld	int64 _and::RW
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
	call	void _and::initialize()

	ldsfld	int64 _and::ALL
	ldsfld	int64 _and::ALL
	and
	ldc.i8	0xFFFFFFFFFFFFFFFF
	ceq
	brfalse	FAIL

	ldsfld	int64 _and::ALL
	ldsfld	int64 _and::NONE
	and
	ldc.i8	0x0000000000000000
	ceq
	brfalse	FAIL

	ldsfld	int64 _and::NONE
	ldsfld	int64 _and::ALL
	and
	ldc.i8	0x0000000000000000
	ceq
	brfalse	FAIL

	ldsfld	int64 _and::NONE
	ldsfld	int64 _and::NONE
	and
	ldc.i8	0x0000000000000000
	ceq
	brfalse	FAIL

	ldsfld	int64 _and::EVEN
	ldsfld	int64 _and::EVEN
	and
	ldc.i8	0xAAAAAAAAAAAAAAAA
	ceq
	brfalse	FAIL

	ldsfld	int64 _and::EVEN
	ldsfld	int64 _and::ODD
	and
	ldc.i8	0x0000000000000000
	ceq
	brfalse	FAIL

	ldsfld	int64 _and::HIGH
	ldsfld	int64 _and::LOW
	and
	ldc.i8	0x0000000000000001
	ceq
	brfalse	FAIL

	ldsfld	int64 _and::LOW
	ldsfld	int64 _and::HIGH
	and
	ldc.i8	0x0000000000000001
	ceq
	brfalse	FAIL

	ldsfld	int64 _and::RW
	ldc.i8	0xA4710EDCA4710EDC
	and
	ldc.i8	0xA0510448A0510448
	ceq
	brfalse	FAIL
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly and_u8.exe{}
