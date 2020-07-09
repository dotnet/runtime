// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public		mul_ovf_u1 {



.method public static int32 u1(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
	ldarg	0
	ldarg	1
	mul
	conv.ovf.u1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	br		END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse	FAIL
	ldc.i4	0xAEAEAEAE
	br		END
FAIL:
	ldc.i4	0xEEEEEEEE
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd
}


.method public		void mul_ovf_u1() {
.maxstack	0
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x000000FE
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x000000FF
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x00000055
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x000000AA
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x00000001
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x000000FE
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0x000000FE
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x000000FF
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0x000000FF
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x00000055
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0x00000055
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x000000AA
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0x000000AA
	ceq
	brfalse		FAIL
	ldc.i4		0x000000FE
	ldc.i4		0x00000000
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x000000FE
	ldc.i4		0x00000001
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0x000000FE
	ceq
	brfalse		FAIL
	ldc.i4		0x000000FE
	ldc.i4		0x000000FE
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
	ldc.i4		0x000000FE
	ldc.i4		0x000000FF
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
	ldc.i4		0x000000FE
	ldc.i4		0x00000055
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
	ldc.i4		0x000000FE
	ldc.i4		0x000000AA
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL

	ldc.i4		0x000000FF
	ldc.i4		0x00000000
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x000000FF
	ldc.i4		0x00000001
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0x000000FF
	ceq
	brfalse		FAIL
	ldc.i4		0x000000FF
	ldc.i4		0x000000FE
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
	ldc.i4		0x000000FF
	ldc.i4		0x000000FF
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
	ldc.i4		0x000000FF
	ldc.i4		0x00000055
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
	ldc.i4		0x000000FF
	ldc.i4		0x000000AA
  	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
	ldc.i4		0x00000055
	ldc.i4		0x00000000
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x00000055
	ldc.i4		0x00000001
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0x00000055
	ceq
	brfalse		FAIL
	ldc.i4		0x00000055
	ldc.i4		0x000000FE
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
	ldc.i4		0x00000055
	ldc.i4		0x000000FF
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
	ldc.i4		0x00000055
	ldc.i4		0x00000055
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
	ldc.i4		0x00000055
	ldc.i4		0x000000AA
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
	ldc.i4		0x000000AA
	ldc.i4		0x00000000
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x000000AA
	ldc.i4		0x00000001
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0x000000AA
	ceq
	brfalse		FAIL
	ldc.i4		0x000000AA
	ldc.i4		0x000000FE
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
	ldc.i4		0x000000AA
	ldc.i4		0x000000FF
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
	ldc.i4		0x000000AA
	ldc.i4		0x00000055
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
	ldc.i4		0x000000AA
	ldc.i4		0x000000AA
	call	int32 mul_ovf_u1::u1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL



PASS:
	ldc.i4 100
	br		END

FAIL:
	ldc.i4	0x00000000

END:
	ret
}
}


.assembly mul_ovf_u1.exe{}
