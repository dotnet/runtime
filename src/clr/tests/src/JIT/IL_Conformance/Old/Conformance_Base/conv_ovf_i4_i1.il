// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}


.class public		conv_ovf_i1 {



.method public static		int32 conv(int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	isinst			[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:


	ldloc	0

	brfalse			FAIL
	ldc.i4			0xEEEEEEEE
	br				END
FAIL:
	ldc.i4			0x00000000
	br				END
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd
}

.method public static		int32 conv_un(unsigned int32, unsigned int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i1.un
	stloc.1
	leave.s	try_end2
try_end2:
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	isinst			[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd2
HEnd2:


	ldloc	0

	brfalse			FAIL2
	ldc.i4			0xEEEEEEEE
	br				END2
FAIL2:
	ldc.i4			0x00000000
	br				END2
END2:
	ret
.try try_start2 to try_end2 catch [mscorlib]System.OverflowException handler aHandler2 to HEnd2
}

.method public		void conv_ovf_i1() {
.maxstack	1
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
	ldc.i4			0x80000000
	ldc.i4			0x00000000
	call		int32 conv_ovf_i1::conv(int32,int32)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL
	ldc.i4			0xFFFFFFFF
	ldc.i4			0xFFFFFFFF
	call		int32 conv_ovf_i1::conv(int32,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
	ldc.i4			0x00000000
	ldc.i4			0x00000000
	call		int32 conv_ovf_i1::conv(int32,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
	ldc.i4			0x00000001
	ldc.i4			0x00000001
	call		int32 conv_ovf_i1::conv(int32,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
	ldc.i4			0x7FFFFFFF
	ldc.i4			0x00000000
	call		int32 conv_ovf_i1::conv(int32,int32)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL

	ldc.i4			0xFFFFFF80
	ldc.i4			0xFFFFFF80
	call		int32 conv_ovf_i1::conv(int32,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
	ldc.i4			0xFFFFFFFF
	ldc.i4			0xFFFFFFFF
	call		int32 conv_ovf_i1::conv(int32,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
	ldc.i4			0x00000000
	ldc.i4			0x00000000
	call		int32 conv_ovf_i1::conv(int32,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
	ldc.i4			0x00000001
	ldc.i4			0x00000001
	call		int32 conv_ovf_i1::conv(int32,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
	ldc.i4			0x0000007F
	ldc.i4			0x0000007F
	call		int32 conv_ovf_i1::conv(int32,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL

	ldc.i4			0x80000000
	ldc.i4			0x00000000
	call		int32 conv_ovf_i1::conv_un(unsigned int32,unsigned int32)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL
	ldc.i4			0x00000000
	ldc.i4			0x00000000
	call		int32 conv_ovf_i1::conv_un(unsigned int32,unsigned int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
	ldc.i4			0x00000001
	ldc.i4			0x00000001
	call		int32 conv_ovf_i1::conv_un(unsigned int32,unsigned int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
	ldc.i4			0x7FFFFFFF
	ldc.i4			0x00000000
	call		int32 conv_ovf_i1::conv_un(unsigned int32,unsigned int32)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL

	ldc.i4			0x00000000
	ldc.i4			0x00000000
	call		int32 conv_ovf_i1::conv_un(unsigned int32,unsigned int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
	ldc.i4			0x00000001
	ldc.i4			0x00000001
	call		int32 conv_ovf_i1::conv_un(unsigned int32,unsigned int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
	ldc.i4			0x0000007F
	ldc.i4			0x0000007F
	call		int32 conv_ovf_i1::conv_un(unsigned int32,unsigned int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL



PASS:
	ldc.i4 100
	br		END

FAIL:
	ldc.i4	0x00000000

END:
	ret
}
}



.assembly conv_ovf_i4_i1.exe{}
