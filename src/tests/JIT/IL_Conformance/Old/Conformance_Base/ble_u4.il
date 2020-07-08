// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}


.class public ble_un {

.field public static	int32 all
.field public static	int32 none
.field public static	int32 odd
.field public static	int32 even

.method public static	void initialize() {
.maxstack	10
	ldc.i4		0xFFFFFFFF
	stsfld	int32 ble_un::all
	ldc.i4		0x00000000
	stsfld	int32 ble_un::none
	ldc.i4		0x55555555
	stsfld	int32 ble_un::odd
	ldc.i4		0xAAAAAAAA
	stsfld	int32 ble_un::even
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void ble_un::initialize()


	ldsfld	int32 ble_un::all
	ldsfld	int32 ble_un::all
	ble.un A
	br FAIL

A:
	ldsfld	int32 ble_un::all
	ldsfld	int32 ble_un::none
	ble.un FAIL

	ldsfld	int32 ble_un::all
	ldsfld	int32 ble_un::odd
	ble.un FAIL

	ldsfld	int32 ble_un::all
	ldsfld	int32 ble_un::even
	ble.un FAIL
	ldsfld	int32 ble_un::none
	ldsfld	int32 ble_un::all
	ble.un B
	br FAIL

B:
	ldsfld	int32 ble_un::none
	ldsfld	int32 ble_un::none
	ble.un C
	br FAIL

C:
	ldsfld	int32 ble_un::none
	ldsfld	int32 ble_un::odd
	ble.un D
	br FAIL

D:
	ldsfld	int32 ble_un::none
	ldsfld	int32 ble_un::even
	ble.un E
	br FAIL
E:
	ldsfld	int32 ble_un::odd
	ldsfld	int32 ble_un::all
	ble.un F
	br FAIL

F:
	ldsfld	int32 ble_un::odd
	ldsfld	int32 ble_un::none
	ble.un FAIL

	ldsfld	int32 ble_un::odd
	ldsfld	int32 ble_un::odd
	ble.un G
	br FAIL

G:
	ldsfld	int32 ble_un::odd
	ldsfld	int32 ble_un::even
	ble.un H
	br FAIL
H:
	ldsfld	int32 ble_un::even
	ldsfld	int32 ble_un::all
	ble.un I
	br FAIL

I:
	ldsfld	int32 ble_un::even
	ldsfld	int32 ble_un::none
	ble.un FAIL

	ldsfld	int32 ble_un::even
	ldsfld	int32 ble_un::odd
	ble.un FAIL

	ldsfld	int32 ble_un::even
	ldsfld	int32 ble_un::even
	ble.un BACKCHECK
	br FAIL
TOPASS:
	br PASS
BACKCHECK:
	ldc.i4 0x0
	ldc.i4 0x1
	ble.un TOPASS
	br FAIL



PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly ble_u4.exe{}
