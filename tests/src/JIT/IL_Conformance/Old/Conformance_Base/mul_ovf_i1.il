// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}


.class public		mul_ovf_i1 {



.method public static int32 i1(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
	ldarg	0
	ldarg	1
	mul
	conv.ovf.i1
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


.method public		void mul_ovf_i1() {
.maxstack	0
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
.locals	(int32)
	ldc.i4		0xFFFFFF80
	ldc.i4		0xFFFFFF80
	call	int32 mul_ovf_i1::i1(int32,int32)
	stloc	0

	ldloc	0
	ldc.i4		0xEEEEEEEE
	ceq
	brtrue		FAILEX	// invalid exception was generated

	ldloc	0
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL1	// OverflowException did _not occur

	ldc.i4		0xFFFFFF80
	ldc.i4		0xFFFFFFFF
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL2
	ldc.i4		0xFFFFFF80
	ldc.i4		0x00000000
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL3
	ldc.i4		0xFFFFFF80
	ldc.i4		0x00000001
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xFFFFFF80
	ceq
	brfalse		FAIL4
	ldc.i4		0xFFFFFF80
	ldc.i4		0x0000007F
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL5
	ldc.i4		0xFFFFFF80
	ldc.i4		0x00000055
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL6
	ldc.i4		0xFFFFFF80
	ldc.i4		0xFFFFFFAA
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL7

	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFF80
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL8
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFFF
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL9
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000000
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL10
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000001
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL11
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x0000007F
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xFFFFFF81
	ceq
	brfalse		FAIL12
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000055
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xFFFFFFAB
	ceq
	brfalse		FAIL13
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFAA
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0x00000056
	ceq
	brfalse		FAIL14
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFF80
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL15
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFFF
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL16
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL17
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL18
	ldc.i4		0x00000000
	ldc.i4		0x0000007F
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL19
	ldc.i4		0x00000000
	ldc.i4		0x00000055
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL20
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFAA
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL21

	ldc.i4		0x00000001
	ldc.i4		0xFFFFFF80
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xFFFFFF80
	ceq
	brfalse		FAIL22
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFF
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL23
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL24
	ldc.i4		0x00000001
	ldc.i4		0x00000001
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL25
	ldc.i4		0x00000001
	ldc.i4		0x0000007F
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0x0000007F
	ceq
	brfalse		FAIL26
	ldc.i4		0x00000001
	ldc.i4		0x00000055
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0x00000055
	ceq
	brfalse		FAIL27
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFAA
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xFFFFFFAA
	ceq
	brfalse		FAIL28

	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFF80
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL29
	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFFFF
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xFFFFFF81
	ceq
	brfalse		FAIL30
	ldc.i4		0x0000007F
	ldc.i4		0x00000000
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL31
	ldc.i4		0x0000007F
	ldc.i4		0x00000001
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0x0000007F
	ceq
	brfalse		FAIL32
	ldc.i4		0x0000007F
	ldc.i4		0x0000007F
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL33
	ldc.i4		0x0000007F
	ldc.i4		0x00000055
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL34
	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFFAA
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL35

	ldc.i4		0x00000055
	ldc.i4		0xFFFFFF80
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL36
	ldc.i4		0x00000055
	ldc.i4		0xFFFFFFFF
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xFFFFFFAB
	ceq
	brfalse		FAIL37
	ldc.i4		0x00000055
	ldc.i4		0x00000000
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL38
	ldc.i4		0x00000055
	ldc.i4		0x00000001
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0x00000055
	ceq
	brfalse		FAIL39
	ldc.i4		0x00000055
	ldc.i4		0x0000007F
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL40
	ldc.i4		0x00000055
	ldc.i4		0x00000055
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL41
	ldc.i4		0x00000055
	ldc.i4		0xFFFFFFAA
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL42

	ldc.i4		0xFFFFFFAA
	ldc.i4		0xFFFFFF80
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL43
	ldc.i4		0xFFFFFFAA
	ldc.i4		0xFFFFFFFF
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0x00000056
	ceq
	brfalse		FAIL44
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000000
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL45
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000001
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xFFFFFFAA
	ceq
	brfalse		FAIL46
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x0000007F
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL47
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000055
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL48
	ldc.i4		0xFFFFFFAA
	ldc.i4		0xFFFFFFAA
	call	int32 mul_ovf_i1::i1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL49


PASS:
	ldc.i4 100
	br		END

FAIL1:
	ldc.i4	0x00000001
	br		END
FAIL2:
	ldc.i4	0x00000002
	br		END
FAIL3:
	ldc.i4	0x00000003
	br		END
FAIL4:
	ldc.i4	0x00000004
	br		END
FAIL5:
	ldc.i4	0x00000005
	br		END
FAIL6:
	ldc.i4	0x00000006
	br		END
FAIL7:
	ldc.i4	0x00000007
	br		END
FAIL8:
	ldc.i4	0x00000008
	br		END
FAIL9:
	ldc.i4	0x00000009
	br		END
FAIL10:
	ldc.i4	0x000000010
	br		END
FAIL11:
	ldc.i4	0x000000011
	br		END
FAIL12:
	ldc.i4	0x000000012
	br		END
FAIL13:
	ldc.i4	0x000000013
	br		END
FAIL14:
	ldc.i4	0x000000014
	br		END
FAIL15:
	ldc.i4	0x000000015
	br		END
FAIL16:
	ldc.i4	0x000000016
	br		END
FAIL17:
	ldc.i4	0x000000017
	br		END
FAIL18:
	ldc.i4	0x000000018
	br		END
FAIL19:
	ldc.i4	0x000000019
	br		END

FAIL20:
	ldc.i4	0x000000020
	br		END
FAIL21:
	ldc.i4	0x000000021
	br		END
FAIL22:
	ldc.i4	0x000000022
	br		END
FAIL23:
	ldc.i4	0x000000023
	br		END
FAIL24:
	ldc.i4	0x000000024
	br		END
FAIL25:
	ldc.i4	0x000000025
	br		END
FAIL26:
	ldc.i4	0x000000026
	br		END
FAIL27:
	ldc.i4	0x000000027
	br		END
FAIL28:
	ldc.i4	0x000000028
	br		END
FAIL29:
	ldc.i4	0x000000029
	br		END

FAIL30:
	ldc.i4	0x000000030
	br		END
FAIL31:
	ldc.i4	0x000000031
	br		END
FAIL32:
	ldc.i4	0x000000032
	br		END
FAIL33:
	ldc.i4	0x000000033
	br		END
FAIL34:
	ldc.i4	0x000000034
	br		END
FAIL35:
	ldc.i4	0x000000035
	br		END
FAIL36:
	ldc.i4	0x000000036
	br		END
FAIL37:
	ldc.i4	0x000000037
	br		END
FAIL38:
	ldc.i4	0x000000038
	br		END
FAIL39:
	ldc.i4	0x000000039
	br		END

FAIL40:
	ldc.i4	0x000000040
	br		END
FAIL41:
	ldc.i4	0x000000041
	br		END
FAIL42:
	ldc.i4	0x000000042
	br		END
FAIL43:
	ldc.i4	0x000000043
	br		END
FAIL44:
	ldc.i4	0x000000044
	br		END
FAIL45:
	ldc.i4	0x000000045
	br		END
FAIL46:
	ldc.i4	0x000000046
	br		END
FAIL47:
	ldc.i4	0x000000047
	br		END
FAIL48:
	ldc.i4	0x000000048
	br		END
FAIL49:
	ldc.i4	0x000000049
	br		END

FAILEX:
	ldc.i4	0xFFFFFFFF
	br		END


END:
	ret
}
}



.assembly mul_ovf_i1.exe{}
