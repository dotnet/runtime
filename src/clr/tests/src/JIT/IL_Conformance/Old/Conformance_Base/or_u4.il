// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}

.class public OR_U4 {
.method public static	int32 _or(int32,int32,int32) {
.maxstack		20
	ldarg	0
	ldarg	1
	or
	ldarg	2
	ceq
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x00000000
	ldc.i4		0x00000001
	ldc.i4		0x00000001
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x00000000
	ldc.i4		0x80000000
	ldc.i4		0x80000000
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x00000000
	ldc.i4		0x55555555
	ldc.i4		0x55555555
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x00000000
	ldc.i4		0xAAAAAAAA
	ldc.i4		0xAAAAAAAA
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFFF
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x00000001
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x00000001
	ldc.i4		0x00000001
	ldc.i4		0x00000001
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x00000001
	ldc.i4		0x80000000
	ldc.i4		0x80000001
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x00000001
	ldc.i4		0x55555555
	ldc.i4		0x55555555
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x00000001
	ldc.i4		0xAAAAAAAA
	ldc.i4		0xAAAAAAAB
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFFF
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x80000000
	ldc.i4		0x00000000
	ldc.i4		0x80000000
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x80000000
	ldc.i4		0x00000001
	ldc.i4		0x80000001
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x80000000
	ldc.i4		0x80000000
	ldc.i4		0x80000000
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x80000000
	ldc.i4		0x55555555
	ldc.i4		0xD5555555
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x80000000
	ldc.i4		0xAAAAAAAA
	ldc.i4		0xAAAAAAAA
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x80000000
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFFF
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x55555555
	ldc.i4		0x00000000
	ldc.i4		0x55555555
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x55555555
	ldc.i4		0x00000001
	ldc.i4		0x55555555
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x55555555
	ldc.i4		0x80000000
	ldc.i4		0xD5555555
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x55555555
	ldc.i4		0x55555555
	ldc.i4		0x55555555
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x55555555
	ldc.i4		0xAAAAAAAA
	ldc.i4		0xFFFFFFFF
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x55555555
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFFF
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0xAAAAAAAA
	ldc.i4		0x00000000
	ldc.i4		0xAAAAAAAA
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0xAAAAAAAA
	ldc.i4		0x00000001
	ldc.i4		0xAAAAAAAB
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0xAAAAAAAA
	ldc.i4		0x80000000
	ldc.i4		0xAAAAAAAA
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0xAAAAAAAA
	ldc.i4		0x55555555
	ldc.i4		0xFFFFFFFF
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0xAAAAAAAA
	ldc.i4		0xAAAAAAAA
	ldc.i4		0xAAAAAAAA
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0xAAAAAAAA
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFFF
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFFF
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFF
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0xFFFFFFFF
	ldc.i4		0x80000000
	ldc.i4		0xFFFFFFFF
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0xFFFFFFFF
	ldc.i4		0x55555555
	ldc.i4		0xFFFFFFFF
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0xFFFFFFFF
	ldc.i4		0xAAAAAAAA
	ldc.i4		0xFFFFFFFF
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFFF
	call	int32 OR_U4::_or(int32,int32,int32)
	brfalse		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly or_u4.exe{}
