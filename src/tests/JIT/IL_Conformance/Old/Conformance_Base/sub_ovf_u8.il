// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public		sub_ovf_u8 {



.method public static int32 u8(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,unsigned int64)
.maxstack	2
try_start:
	ldarg	0
	ldarg	1
	sub.ovf.un
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	ldarg	2
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
	ldarg	2
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x0000000000000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd
}


.method public		void sub_ovf_u8() {
.maxstack	0
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
	ldc.i8		0x0000000000000000
	ldc.i8		0x0000000000000000
	ldc.i8		0x0000000000000000
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0x0000000000000000
	ldc.i8		0x0000000000000001
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0x0000000000000000
	ldc.i8		0xFFFFFFFFFFFFFFFE
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0x0000000000000000
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0x0000000000000000
	ldc.i8		0x5555555555555555
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0x0000000000000000
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL

	ldc.i8		0x0000000000000001
	ldc.i8		0x0000000000000000
	ldc.i8		0x0000000000000001
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0x0000000000000001
	ldc.i8		0x0000000000000001
	ldc.i8		0x0000000000000000
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0x0000000000000001
	ldc.i8		0xFFFFFFFFFFFFFFFE
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0x0000000000000001
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0x0000000000000001
	ldc.i8		0x5555555555555555
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0x0000000000000001
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL

	ldc.i8		0xFFFFFFFFFFFFFFFE
	ldc.i8		0x0000000000000000
	ldc.i8		0xFFFFFFFFFFFFFFFE
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0xFFFFFFFFFFFFFFFE
	ldc.i8		0x0000000000000001
	ldc.i8		0xFFFFFFFFFFFFFFFD
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0xFFFFFFFFFFFFFFFE
	ldc.i8		0xFFFFFFFFFFFFFFFE
	ldc.i8		0x0000000000000000
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0xFFFFFFFFFFFFFFFE
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0xFFFFFFFFFFFFFFFE
	ldc.i8		0x5555555555555555
	ldc.i8		0xAAAAAAAAAAAAAAA9
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0xFFFFFFFFFFFFFFFE
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0x5555555555555554
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL

	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0x0000000000000000
	ldc.i8		0xFFFFFFFFFFFFFFFF
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0x0000000000000001
	ldc.i8		0xFFFFFFFFFFFFFFFE
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0xFFFFFFFFFFFFFFFE
	ldc.i8		0x0000000000000001
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0x0000000000000000
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0x5555555555555555
	ldc.i8		0xAAAAAAAAAAAAAAAA
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0x5555555555555555
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0x5555555555555555
	ldc.i8		0x0000000000000000
	ldc.i8		0x5555555555555555
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0x5555555555555555
	ldc.i8		0x0000000000000001
	ldc.i8		0x5555555555555554
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0x5555555555555555
	ldc.i8		0xFFFFFFFFFFFFFFFE
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0x5555555555555555
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0x5555555555555555
	ldc.i8		0x5555555555555555
	ldc.i8		0x0000000000000000
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0x5555555555555555
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0x0000000000000000
	ldc.i8		0xAAAAAAAAAAAAAAAA
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0x0000000000000001
	ldc.i8		0xAAAAAAAAAAAAAAA9
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0xFFFFFFFFFFFFFFFE
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0x5555555555555555
	ldc.i8		0x5555555555555555
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0x0000000000000000
	call	int32 sub_ovf_u8::u8(int64,int64,int64)
	brfalse		FAIL



PASS:
	ldc.i4 100
	br		END

FAIL:
	ldc.i4	0x0000000000000000

END:
	ret
}
}

.assembly sub_ovf_u8{}
