// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

//
//======================================

//---- CLASS ----------------
.class public		ldc_sub_ovf_u2 {

//---- GLOBAL DATA ----------


//---- METHODS --------------


.method public static int32 u2_0(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x00000000
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00000000
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00000000
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_1(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x00000001
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xEEEEEEEE
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_2(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x0000FFFE
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xEEEEEEEE
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_3(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x0000FFFF
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xEEEEEEEE
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_4(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x00005555
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xEEEEEEEE
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_5(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x0000AAAA
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xEEEEEEEE
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_6(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x00000000
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00000001
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00000001
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_7(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x00000001
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00000000
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00000000
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_8(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x0000FFFE
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xEEEEEEEE
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_9(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x0000FFFF
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xEEEEEEEE
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_10(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x00005555
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xEEEEEEEE
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_11(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x0000AAAA
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xEEEEEEEE
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_12(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFE
		ldc.i4		0x00000000
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x0000FFFE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x0000FFFE
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_13(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFE
		ldc.i4		0x00000001
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x0000FFFD
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x0000FFFD
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_14(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFE
		ldc.i4		0x0000FFFE
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00000000
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00000000
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_15(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFE
		ldc.i4		0x0000FFFF
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xEEEEEEEE
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_16(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFE
		ldc.i4		0x00005555
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x0000AAA9
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x0000AAA9
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_17(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFE
		ldc.i4		0x0000AAAA
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00005554
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00005554
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_18(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFF
		ldc.i4		0x00000000
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x0000FFFF
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x0000FFFF
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_19(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFF
		ldc.i4		0x00000001
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x0000FFFE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x0000FFFE
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_20(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFF
		ldc.i4		0x0000FFFE
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00000001
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00000001
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_21(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFF
		ldc.i4		0x0000FFFF
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00000000
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00000000
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_22(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFF
		ldc.i4		0x00005555
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x0000AAAA
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x0000AAAA
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_23(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFF
		ldc.i4		0x0000AAAA
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00005555
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00005555
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_24(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x00005555
		ldc.i4		0x00000000
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00005555
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00005555
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_25(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x00005555
		ldc.i4		0x00000001
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00005554
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00005554
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_26(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x00005555
		ldc.i4		0x0000FFFE
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xEEEEEEEE
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_27(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x00005555
		ldc.i4		0x0000FFFF
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xEEEEEEEE
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_28(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x00005555
		ldc.i4		0x00005555
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00000000
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00000000
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_29(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x00005555
		ldc.i4		0x0000AAAA
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xEEEEEEEE
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_30(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x0000AAAA
		ldc.i4		0x00000000
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x0000AAAA
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x0000AAAA
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_31(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x0000AAAA
		ldc.i4		0x00000001
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x0000AAA9
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x0000AAA9
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_32(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x0000AAAA
		ldc.i4		0x0000FFFE
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xEEEEEEEE
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_33(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x0000AAAA
		ldc.i4		0x0000FFFF
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xEEEEEEEE
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_34(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x0000AAAA
		ldc.i4		0x00005555
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00005555
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00005555
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u2_35(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
		ldc.i4		0x0000AAAA
		ldc.i4		0x0000AAAA
	sub
	conv.ovf.u2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00000000
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00000000
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
//---- CONSTRUCTOR ----------
.method public		void ldc_sub_ovf_u2() {
.maxstack	0
	ret
}

//---- MAIN -----------------
.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
//====== begin testing ======
// -- 0 - 0
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_u2::u2_0(int32,int32,int32)
	brfalse		FAIL
// -- 0 - 1
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u2::u2_1(int32,int32,int32)
	brfalse		FAIL
// -- 0 - (Max - 1)
	ldc.i4		0x00000000
	ldc.i4		0x0000FFFE
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u2::u2_2(int32,int32,int32)
	brfalse		FAIL
// -- 0 - Max
	ldc.i4		0x00000000
	ldc.i4		0x0000FFFF
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u2::u2_3(int32,int32,int32)
	brfalse		FAIL
// -- 0 - Odd
	ldc.i4		0x00000000
	ldc.i4		0x00005555
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u2::u2_4(int32,int32,int32)
	brfalse		FAIL
// -- 0 - Even
	ldc.i4		0x00000000
	ldc.i4		0x0000AAAA
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u2::u2_5(int32,int32,int32)
	brfalse		FAIL

//------------------------------------------------
// -- 1 - 0
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	call	int32 ldc_sub_ovf_u2::u2_6(int32,int32,int32)
	brfalse		FAIL
// -- 1 - 1
	ldc.i4		0x00000001
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_u2::u2_7(int32,int32,int32)
	brfalse		FAIL
// -- 1 - (Max - 1)
	ldc.i4		0x00000001
	ldc.i4		0x0000FFFE
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u2::u2_8(int32,int32,int32)
	brfalse		FAIL
// -- 1 - Max
	ldc.i4		0x00000001
	ldc.i4		0x0000FFFF
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u2::u2_9(int32,int32,int32)
	brfalse		FAIL
// -- 1 - Odd
	ldc.i4		0x00000001
	ldc.i4		0x00005555
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u2::u2_10(int32,int32,int32)
	brfalse		FAIL
// -- 1 - Even
	ldc.i4		0x00000001
	ldc.i4		0x0000AAAA
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u2::u2_11(int32,int32,int32)
	brfalse		FAIL

//------------------------------------------------
// -- (Max - 1) - 0
	ldc.i4		0x0000FFFE
	ldc.i4		0x00000000
	ldc.i4		0x0000FFFE
	call	int32 ldc_sub_ovf_u2::u2_12(int32,int32,int32)
	brfalse		FAIL
// -- (Max - 1) - 1
	ldc.i4		0x0000FFFE
	ldc.i4		0x00000001
	ldc.i4		0x0000FFFD
	call	int32 ldc_sub_ovf_u2::u2_13(int32,int32,int32)
	brfalse		FAIL
// -- (Max - 1) - (Max - 1)
	ldc.i4		0x0000FFFE
	ldc.i4		0x0000FFFE
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_u2::u2_14(int32,int32,int32)
	brfalse		FAIL
// -- (Max - 1) - Max
	ldc.i4		0x0000FFFE
	ldc.i4		0x0000FFFF
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u2::u2_15(int32,int32,int32)
	brfalse		FAIL
// -- (Max - 1) - Odd
	ldc.i4		0x0000FFFE
	ldc.i4		0x00005555
	ldc.i4		0x0000AAA9
	call	int32 ldc_sub_ovf_u2::u2_16(int32,int32,int32)
	brfalse		FAIL
// -- (Max - 1) - Even
	ldc.i4		0x0000FFFE
	ldc.i4		0x0000AAAA
	ldc.i4		0x00005554
	call	int32 ldc_sub_ovf_u2::u2_17(int32,int32,int32)
	brfalse		FAIL
//------------------------------------------------

// -- Max - 0
	ldc.i4		0x0000FFFF
	ldc.i4		0x00000000
	ldc.i4		0x0000FFFF
	call	int32 ldc_sub_ovf_u2::u2_18(int32,int32,int32)
	brfalse		FAIL
// -- Max - 1
	ldc.i4		0x0000FFFF
	ldc.i4		0x00000001
	ldc.i4		0x0000FFFE
	call	int32 ldc_sub_ovf_u2::u2_19(int32,int32,int32)
	brfalse		FAIL
// -- Max - (Max - 1)
	ldc.i4		0x0000FFFF
	ldc.i4		0x0000FFFE
	ldc.i4		0x00000001
	call	int32 ldc_sub_ovf_u2::u2_20(int32,int32,int32)
	brfalse		FAIL
// -- Max - Max
	ldc.i4		0x0000FFFF
	ldc.i4		0x0000FFFF
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_u2::u2_21(int32,int32,int32)
	brfalse		FAIL
// -- Max - Odd
	ldc.i4		0x0000FFFF
	ldc.i4		0x00005555
	ldc.i4		0x0000AAAA
	call	int32 ldc_sub_ovf_u2::u2_22(int32,int32,int32)
	brfalse		FAIL
// -- Max - Even
	ldc.i4		0x0000FFFF
	ldc.i4		0x0000AAAA
	ldc.i4		0x00005555
	call	int32 ldc_sub_ovf_u2::u2_23(int32,int32,int32)
	brfalse		FAIL
//------------------------------------------------
// -- Odd - 0
	ldc.i4		0x00005555
	ldc.i4		0x00000000
	ldc.i4		0x00005555
	call	int32 ldc_sub_ovf_u2::u2_24(int32,int32,int32)
	brfalse		FAIL
// -- Odd - 1
	ldc.i4		0x00005555
	ldc.i4		0x00000001
	ldc.i4		0x00005554
	call	int32 ldc_sub_ovf_u2::u2_25(int32,int32,int32)
	brfalse		FAIL
// -- Odd - (Max - 1)
	ldc.i4		0x00005555
	ldc.i4		0x0000FFFE
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u2::u2_26(int32,int32,int32)
	brfalse		FAIL
// -- Odd - Max
	ldc.i4		0x00005555
	ldc.i4		0x0000FFFF
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u2::u2_27(int32,int32,int32)
	brfalse		FAIL
// -- Odd - Odd
	ldc.i4		0x00005555
	ldc.i4		0x00005555
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_u2::u2_28(int32,int32,int32)
	brfalse		FAIL
// -- Odd - Even
	ldc.i4		0x00005555
	ldc.i4		0x0000AAAA
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u2::u2_29(int32,int32,int32)
	brfalse		FAIL
//------------------------------------------------
// -- Even - 0
	ldc.i4		0x0000AAAA
	ldc.i4		0x00000000
	ldc.i4		0x0000AAAA
	call	int32 ldc_sub_ovf_u2::u2_30(int32,int32,int32)
	brfalse		FAIL
// -- Even - 1
	ldc.i4		0x0000AAAA
	ldc.i4		0x00000001
	ldc.i4		0x0000AAA9
	call	int32 ldc_sub_ovf_u2::u2_31(int32,int32,int32)
	brfalse		FAIL
// -- Even - (Max - 1)
	ldc.i4		0x0000AAAA
	ldc.i4		0x0000FFFE
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u2::u2_32(int32,int32,int32)
	brfalse		FAIL
// -- Even - Max
	ldc.i4		0x0000AAAA
	ldc.i4		0x0000FFFF
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u2::u2_33(int32,int32,int32)
	brfalse		FAIL
// -- Even - Odd
	ldc.i4		0x0000AAAA
	ldc.i4		0x00005555
	ldc.i4		0x00005555
	call	int32 ldc_sub_ovf_u2::u2_34(int32,int32,int32)
	brfalse		FAIL
// -- Even - Even
	ldc.i4		0x0000AAAA
	ldc.i4		0x0000AAAA
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_u2::u2_35(int32,int32,int32)
	brfalse		FAIL


//====== end testing ========

//---- branch here on pass --
PASS:
	ldc.i4 100
	br		END

//---- branch here on fail --
FAIL:
	ldc.i4 101

//---- return the result ----
END:
	ret
//---- END OF METHOD --------
}
//---- EOF ------------------
}

.assembly ldc_sub_ovf_u2.exe{}

