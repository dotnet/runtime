// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public		sub_ovf_i1 {



.method public static int32 i1(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
	ldarg	0
	ldarg	1
	sub
	conv.ovf.i1
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
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd
}


.method public		void sub_ovf_i1() {
.maxstack	0
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
	ldc.i4		0xFFFFFF80
	ldc.i4		0xFFFFFF80
	ldc.i4		0x00000000
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0xFFFFFF80
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFF81
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0xFFFFFF80
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFF80
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0xFFFFFF80
	ldc.i4		0x00000001
	ldc.i4		0xEEEEEEEE
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0xFFFFFF80
	ldc.i4		0x0000007F
	ldc.i4		0xEEEEEEEE
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0xFFFFFF80
	ldc.i4		0x00000055
	ldc.i4		0xEEEEEEEE
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0xFFFFFF80
	ldc.i4		0xFFFFFFAA
	ldc.i4		0xFFFFFFD6
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFF80
	ldc.i4		0x0000007F
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000000
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFFF
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFE
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFF80
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000055
	ldc.i4		0xFFFFFFAA
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000055
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFF80
	ldc.i4		0xEEEEEEEE
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000001
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFF
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFF81
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x00000055
	ldc.i4		0xFFFFFFAB
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000056
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x00000001
	ldc.i4		0xFFFFFF80
	ldc.i4		0xEEEEEEEE
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000002
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFF82
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x00000055
	ldc.i4		0xFFFFFFAC
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000057
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFF80
	ldc.i4		0xEEEEEEEE
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xEEEEEEEE
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x0000007F
	ldc.i4		0x00000000
	ldc.i4		0x0000007F
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x0000007F
	ldc.i4		0x00000001
	ldc.i4		0x0000007E
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x0000007F
	ldc.i4		0x0000007F
	ldc.i4		0x00000000
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x0000007F
	ldc.i4		0x00000055
	ldc.i4		0x0000002A
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFFAA
	ldc.i4		0xEEEEEEEE
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0x00000055
	ldc.i4		0xFFFFFF80
	ldc.i4		0xEEEEEEEE
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x00000055
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000056
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x00000055
	ldc.i4		0x00000000
	ldc.i4		0x00000055
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x00000055
	ldc.i4		0x00000001
	ldc.i4		0x00000054
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x00000055
	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFFD6
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x00000055
	ldc.i4		0x00000055
	ldc.i4		0x00000000
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0x00000055
	ldc.i4		0xFFFFFFAA
	ldc.i4		0xEEEEEEEE
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL

	ldc.i4		0xFFFFFFAA
	ldc.i4		0xFFFFFF80
	ldc.i4		0x0000002A
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0xFFFFFFAA
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFAB
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFAA
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFA9
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x0000007F
	ldc.i4		0xEEEEEEEE
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000055
	ldc.i4		0xEEEEEEEE
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
	brfalse		FAIL
	ldc.i4		0xFFFFFFAA
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000000
	call	int32 sub_ovf_i1::i1(int32,int32,int32)
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

.assembly sub_ovf_i1.exe{}
