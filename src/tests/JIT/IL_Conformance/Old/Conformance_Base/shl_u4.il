// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.class public Shl_U4 {
.method public static	int32 _shl(int32,int32,int32) {
.maxstack		20
	ldarg	0
	ldarg	1
	shl
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
	call	int32 Shl_U4::_shl(int32,int32,int32)
	brfalse		FAIL
	
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	call	int32 Shl_U4::_shl(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x00000000
	ldc.i4		0x0000001F
	ldc.i4		0x00000000
	call	int32 Shl_U4::_shl(int32,int32,int32)
	brfalse		FAIL


	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFFF
	call	int32 Shl_U4::_shl(int32,int32,int32)
	brfalse		FAIL
	
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFE
	call	int32 Shl_U4::_shl(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0xFFFFFFFF
	ldc.i4		0x0000001F
	ldc.i4		0x80000000
	call	int32 Shl_U4::_shl(int32,int32,int32)
	brfalse		FAIL



	ldc.i4		0x6789ABCD
	ldc.i4		0x00000000
	ldc.i4		0x6789ABCD
	call	int32 Shl_U4::_shl(int32,int32,int32)
	brfalse		FAIL
	
	ldc.i4		0x6789ABCD
	ldc.i4		0x00000001
	ldc.i4		0xCF13579A
	call	int32 Shl_U4::_shl(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x6789ABCD
	ldc.i4		0x0000001F
	ldc.i4		0x80000000
	call	int32 Shl_U4::_shl(int32,int32,int32)
	brfalse		FAIL


TRY_START:
	ldc.i4		0x00000000
	ldc.i4		0x00000020
	ldc.i4		0x00000000
	call	int32 Shl_U4::_shl(int32,int32,int32)
	pop

	ldc.i4		0x00000000
	ldc.i4		0x0000003F
	ldc.i4		0x00000000
	call	int32 Shl_U4::_shl(int32,int32,int32)
	pop

	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000020
	ldc.i4		0xFFFFFFFF
	call		int32 Shl_U4::_shl(int32,int32,int32)
	pop

	ldc.i4		0xFFFFFFFF
	ldc.i4		0x0000003F
	ldc.i4		0x80000000
	call		int32 Shl_U4::_shl(int32,int32,int32)
	pop

	ldc.i4		0x6789ABCD
	ldc.i4		0x00000020
	ldc.i4		0x6789ABCD
	call		int32 Shl_U4::_shl(int32,int32,int32)
	pop

	ldc.i4		0x6789ABCD
	ldc.i4		0x0000003F
	ldc.i4		0x80000000
	call		int32 Shl_U4::_shl(int32,int32,int32)
	pop

	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000001
	ldc.i4		0x80000000
	call		int32 Shl_U4::_shl(int32,int32,int32)
	pop

	leave.s	TRY_END
TRY_END:
	br	PASS
HANDLER:
	pop		//ref
	leave		END_HANDLER
END_HANDLER:
	br	PASS
PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
.try TRY_START to TRY_END catch [mscorlib]System.Exception handler HANDLER to END_HANDLER
}
}
.assembly shl_u4{}
