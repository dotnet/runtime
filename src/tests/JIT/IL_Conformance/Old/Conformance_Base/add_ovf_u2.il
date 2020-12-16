// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public		add_ovf_u2 {



.method public static int32 u2(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg	0
	ldarg	1
	add
	conv.ovf.u2
	ldarg	2
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse	FAIL
	ldc.i4	0x11111111
	br		END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse	FAIL
	ldc.i4	0xEEEEEEEE
	br		END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd
}


.method public		void add_ovf_u2() {
.maxstack	0
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	ldc.i4		0x00000001
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x0000FFFE
	ldc.i4		0x0000FFFE
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x0000FFFF
	ldc.i4		0x0000FFFF
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x00005555
	ldc.i4		0x00005555
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x0000AAAA
	ldc.i4		0x0000AAAA
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x00000001
	ldc.i4		0x00000002
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x0000FFFE
	ldc.i4		0x0000FFFF
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x0000FFFF
	ldc.i4		0x00000000
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x00005555
	ldc.i4		0x00005556
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x0000AAAA
	ldc.i4		0x0000AAAB
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x0000FFFE
	ldc.i4		0x00000000
	ldc.i4		0x0000FFFE
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x0000FFFE
	ldc.i4		0x00000001
	ldc.i4		0x0000FFFF
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x0000FFFE
	ldc.i4		0x0000FFFE
	ldc.i4		0x00000000
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.i4		0x0000FFFE
	ldc.i4		0x0000FFFF
	ldc.i4		0x00000000
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.i4		0x0000FFFE
	ldc.i4		0x00005555
	ldc.i4		0x00000000
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.i4		0x0000FFFE
	ldc.i4		0x0000AAAA
	ldc.i4		0x00000000
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL

	ldc.i4		0x0000FFFF
	ldc.i4		0x00000000
	ldc.i4		0x0000FFFF
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x0000FFFF
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.i4		0x0000FFFF
	ldc.i4		0x0000FFFE
	ldc.i4		0x00000000
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.i4		0x0000FFFF
	ldc.i4		0x0000FFFF
	ldc.i4		0x00000000
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.i4		0x0000FFFF
	ldc.i4		0x00005555
	ldc.i4		0x00000000
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.i4		0x0000FFFF
	ldc.i4		0x0000AAAA
	ldc.i4		0x00000000
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.i4		0x00005555
	ldc.i4		0x00000000
	ldc.i4		0x00005555
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00005555
	ldc.i4		0x00000001
	ldc.i4		0x00005556
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00005555
	ldc.i4		0x0000FFFE
	ldc.i4		0x00000000
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.i4		0x00005555
	ldc.i4		0x0000FFFF
	ldc.i4		0x00000000
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.i4		0x00005555
	ldc.i4		0x00005555
	ldc.i4		0x0000AAAA
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00005555
	ldc.i4		0x0000AAAA
	ldc.i4		0x0000FFFF
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x0000AAAA
	ldc.i4		0x00000000
	ldc.i4		0x0000AAAA
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x0000AAAA
	ldc.i4		0x00000001
	ldc.i4		0x0000AAAB
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x0000AAAA
	ldc.i4		0x0000FFFE
	ldc.i4		0x00000000
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.i4		0x0000AAAA
	ldc.i4		0x0000FFFF
	ldc.i4		0x00000000
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.i4		0x0000AAAA
	ldc.i4		0x00005555
	ldc.i4		0x0000FFFF
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x0000AAAA
	ldc.i4		0x0000AAAA
	ldc.i4		0x00000000
	call	int32 add_ovf_u2::u2(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
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

.assembly add_ovf_u2.exe{}
