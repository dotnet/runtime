// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class public		add_ovf_i1 {



.method public static int32 i1(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
	ldarg	0
	ldarg	1
	add
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	ldarg	2
	ceq
	brfalse	FAIL
	ldc.i4	0x11111111
	br		END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			aeEnd
aeEnd:

	ldloc	0
	brfalse	FAIL
	ldc.i4	0xEEEEEEEE
	br		END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to aeEnd
}


.method public		void add_ovf_i1() {
.maxstack	0
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
	ldc.i4		0xFFFFFF80
	ldc.i4		0xFFFFFF80
	ldc.i4		0x00000000
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFF80
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000000
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFF80
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFF80
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFF80
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFF81
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFF80
	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFFFF
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFF80
	ldc.i4		0x00000055
	ldc.i4		0xFFFFFFD5
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFF80
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000000
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL

	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFF80
	ldc.i4		0x00000000
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFFE
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFFF
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x0000007F
	ldc.i4		0x0000007E
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000055
	ldc.i4		0x00000054
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFAA
	ldc.i4		0xFFFFFFA9
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFF80
	ldc.i4		0xFFFFFF80
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFFF
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	ldc.i4		0x00000001
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x0000007F
	ldc.i4		0x0000007F
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x00000055
	ldc.i4		0x00000055
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFAA
	ldc.i4		0xFFFFFFAA
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL

	ldc.i4		0x00000001
	ldc.i4		0xFFFFFF80
	ldc.i4		0xFFFFFF81
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000000
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x00000001
	ldc.i4		0x00000002
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x0000007F
	ldc.i4		0x00000000
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x00000055
	ldc.i4		0x00000056
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFAA
	ldc.i4		0xFFFFFFAB
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL

	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFF80
	ldc.i4		0xFFFFFFFF
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x0000007E
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x0000007F
	ldc.i4		0x00000000
	ldc.i4		0x0000007F
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x0000007F
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.i4		0x0000007F
	ldc.i4		0x0000007F
	ldc.i4		0x00000000
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.i4		0x0000007F
	ldc.i4		0x00000055
	ldc.i4		0x00000000
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000029
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL

	ldc.i4		0x00000055
	ldc.i4		0xFFFFFF80
	ldc.i4		0xFFFFFFD5
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000055
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000054
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000055
	ldc.i4		0x00000000
	ldc.i4		0x00000055
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000055
	ldc.i4		0x00000001
	ldc.i4		0x00000056
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0x00000055
	ldc.i4		0x0000007F
	ldc.i4		0x00000000
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.i4		0x00000055
	ldc.i4		0x00000055
	ldc.i4		0x00000000
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.i4		0x00000055
	ldc.i4		0xFFFFFFAA
	ldc.i4		0xFFFFFFFF
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL

	ldc.i4		0xFFFFFFAA
	ldc.i4		0xFFFFFF80
	ldc.i4		0x00000000
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFAA
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFA9
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFAA
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFAB
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x0000007F
	ldc.i4		0x00000029
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000055
	ldc.i4		0xFFFFFFFF
	call	int32 add_ovf_i1::i1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFAA
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000000
	call	int32 add_ovf_i1::i1(int32,int32,int32)
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



.assembly add_ovf_i1{}
