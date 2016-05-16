// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}

.class public Not_U4 {
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
	stsfld	int32 Not_U4::ALL
	ldc.i4	0x00000000
	stsfld	int32 Not_U4::NONE
	ldc.i4	0x80000000
	stsfld	int32 Not_U4::HIGH
	ldc.i4	0xF155E54A
	stsfld	int32 Not_U4::RW
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
	call	void Not_U4::initialize()
	ldsfld	int32 Not_U4::ALL
	not
	ldc.i4	0x00000000
	ceq
	brfalse	FAIL

	ldsfld	int32 Not_U4::NONE
	not
	ldc.i4	0xFFFFFFFF
	ceq
	brfalse	FAIL

	ldsfld	int32 Not_U4::HIGH
	not
	ldc.i4	0x7FFFFFFF
	ceq
	brfalse	FAIL

	ldsfld	int32 Not_U4::RW
	not
	ldc.i4	0xEAA1AB5
	ceq
	brfalse	FAIL
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly not_u4.exe{}
