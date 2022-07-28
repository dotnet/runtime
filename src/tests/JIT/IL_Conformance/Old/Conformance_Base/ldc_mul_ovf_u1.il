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
.class public		ldc_mul_ovf_u1 {

//---- GLOBAL DATA ----------


//---- METHODS --------------


.method public static int32 u1_0(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x00000000
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
.method public static int32 u1_1(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x00000001
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
.method public static int32 u1_2(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x000000FE
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
.method public static int32 u1_3(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x000000FF
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
.method public static int32 u1_4(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x00000055
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
.method public static int32 u1_5(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x000000AA
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
.method public static int32 u1_6(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x00000000
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
.method public static int32 u1_7(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x00000001
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
.method public static int32 u1_8(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x000000FE
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
.method public static int32 u1_9(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x000000FF
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
.method public static int32 u1_10(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x00000055
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
.method public static int32 u1_11(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x000000AA
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
.method public static int32 u1_12(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x000000FE
		ldc.i4		0x00000000
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
.method public static int32 u1_13(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x000000FE
		ldc.i4		0x00000001
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
.method public static int32 u1_14(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x000000FE
		ldc.i4		0x000000FE
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
.method public static int32 u1_15(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x000000FE
		ldc.i4		0x000000FF
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
.method public static int32 u1_16(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x000000FE
		ldc.i4		0x00000055
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
.method public static int32 u1_17(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x000000FE
		ldc.i4		0x000000AA
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
.method public static int32 u1_18(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x000000FF
		ldc.i4		0x00000000
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
.method public static int32 u1_19(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x000000FF
		ldc.i4		0x00000001
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
.method public static int32 u1_20(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x000000FF
		ldc.i4		0x000000FE
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
.method public static int32 u1_21(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x000000FF
		ldc.i4		0x000000FF
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
.method public static int32 u1_22(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x000000FF
		ldc.i4		0x00000055
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
.method public static int32 u1_23(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x000000FF
		ldc.i4		0x000000AA
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
.method public static int32 u1_24(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x00000055
		ldc.i4		0x00000000
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
.method public static int32 u1_25(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x00000055
		ldc.i4		0x00000001
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
.method public static int32 u1_26(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x00000055
		ldc.i4		0x000000FE
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
.method public static int32 u1_27(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x00000055
		ldc.i4		0x000000FF
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
.method public static int32 u1_28(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x00000055
		ldc.i4		0x00000055
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
.method public static int32 u1_29(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x00000055
		ldc.i4		0x000000AA
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
.method public static int32 u1_30(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x000000AA
		ldc.i4		0x00000000
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
.method public static int32 u1_31(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x000000AA
		ldc.i4		0x00000001
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
.method public static int32 u1_32(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x000000AA
		ldc.i4		0x000000FE
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
.method public static int32 u1_33(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x000000AA
		ldc.i4		0x000000FF
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
.method public static int32 u1_34(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x000000AA
		ldc.i4		0x00000055
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
.method public static int32 u1_35(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int8)
.maxstack	2
try_start:
		ldc.i4		0x000000AA
		ldc.i4		0x000000AA
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
//---- CONSTRUCTOR ----------
.method public		void ldc_mul_ovf_u1() {
.maxstack	0
	ret
}

//---- MAIN -----------------
.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
//====== begin testing ======
// -- 0 * 0
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	call	int32 ldc_mul_ovf_u1::u1_0(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- 0 * 1
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	call	int32 ldc_mul_ovf_u1::u1_1(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- 0 * (Max - 1)
	ldc.i4		0x00000000
	ldc.i4		0x000000FE
	call	int32 ldc_mul_ovf_u1::u1_2(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- 0 * Max
	ldc.i4		0x00000000
	ldc.i4		0x000000FF
	call	int32 ldc_mul_ovf_u1::u1_3(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- 0 * Odd
	ldc.i4		0x00000000
	ldc.i4		0x00000055
	call	int32 ldc_mul_ovf_u1::u1_4(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- 0 * Even
	ldc.i4		0x00000000
	ldc.i4		0x000000AA
	call	int32 ldc_mul_ovf_u1::u1_5(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
//------------------------------------------------
// -- 1 * 0
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	call	int32 ldc_mul_ovf_u1::u1_6(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- 1 * 1
	ldc.i4		0x00000001
	ldc.i4		0x00000001
	call	int32 ldc_mul_ovf_u1::u1_7(int32,int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL
// -- 1 * (Max - 1)
	ldc.i4		0x00000001
	ldc.i4		0x000000FE
	call	int32 ldc_mul_ovf_u1::u1_8(int32,int32)
	ldc.i4		0x000000FE
	ceq
	brfalse		FAIL
// -- 1 * Max
	ldc.i4		0x00000001
	ldc.i4		0x000000FF
	call	int32 ldc_mul_ovf_u1::u1_9(int32,int32)
	ldc.i4		0x000000FF
	ceq
	brfalse		FAIL
// -- 1 * Odd
	ldc.i4		0x00000001
	ldc.i4		0x00000055
	call	int32 ldc_mul_ovf_u1::u1_10(int32,int32)
	ldc.i4		0x00000055
	ceq
	brfalse		FAIL
// -- 1 * Even
	ldc.i4		0x00000001
	ldc.i4		0x000000AA
	call	int32 ldc_mul_ovf_u1::u1_11(int32,int32)
	ldc.i4		0x000000AA
	ceq
	brfalse		FAIL
//------------------------------------------------
// -- (Max - 1) * 0
	ldc.i4		0x000000FE
	ldc.i4		0x00000000
	call	int32 ldc_mul_ovf_u1::u1_12(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- (Max - 1) * 1
	ldc.i4		0x000000FE
	ldc.i4		0x00000001
	call	int32 ldc_mul_ovf_u1::u1_13(int32,int32)
	ldc.i4		0x000000FE
	ceq
	brfalse		FAIL
// -- (Max - 1) * (Max - 1)
	ldc.i4		0x000000FE
	ldc.i4		0x000000FE
	call	int32 ldc_mul_ovf_u1::u1_14(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- (Max - 1) * Max
	ldc.i4		0x000000FE
	ldc.i4		0x000000FF
	call	int32 ldc_mul_ovf_u1::u1_15(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- (Max - 1) * Odd
	ldc.i4		0x000000FE
	ldc.i4		0x00000055
	call	int32 ldc_mul_ovf_u1::u1_16(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- (Max - 1) * Even
	ldc.i4		0x000000FE
	ldc.i4		0x000000AA
	call	int32 ldc_mul_ovf_u1::u1_17(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
//------------------------------------------------

// -- Max * 0
	ldc.i4		0x000000FF
	ldc.i4		0x00000000
	call	int32 ldc_mul_ovf_u1::u1_18(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- Max * 1
	ldc.i4		0x000000FF
	ldc.i4		0x00000001
	call	int32 ldc_mul_ovf_u1::u1_19(int32,int32)
	ldc.i4		0x000000FF
	ceq
	brfalse		FAIL
// -- Max * (Max - 1)
	ldc.i4		0x000000FF
	ldc.i4		0x000000FE
	call	int32 ldc_mul_ovf_u1::u1_20(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- Max * Max
	ldc.i4		0x000000FF
	ldc.i4		0x000000FF
	call	int32 ldc_mul_ovf_u1::u1_21(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- Max * Odd
	ldc.i4		0x000000FF
	ldc.i4		0x00000055
	call	int32 ldc_mul_ovf_u1::u1_22(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- Max * Even
	ldc.i4		0x000000FF
	ldc.i4		0x000000AA
  	call	int32 ldc_mul_ovf_u1::u1_23(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
//------------------------------------------------
// -- Odd * 0
	ldc.i4		0x00000055
	ldc.i4		0x00000000
	call	int32 ldc_mul_ovf_u1::u1_24(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- Odd * 1
	ldc.i4		0x00000055
	ldc.i4		0x00000001
	call	int32 ldc_mul_ovf_u1::u1_25(int32,int32)
	ldc.i4		0x00000055
	ceq
	brfalse		FAIL
// -- Odd * (Max - 1)
	ldc.i4		0x00000055
	ldc.i4		0x000000FE
	call	int32 ldc_mul_ovf_u1::u1_26(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- Odd * Max
	ldc.i4		0x00000055
	ldc.i4		0x000000FF
	call	int32 ldc_mul_ovf_u1::u1_27(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- Odd * Odd
	ldc.i4		0x00000055
	ldc.i4		0x00000055
	call	int32 ldc_mul_ovf_u1::u1_28(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- Odd * Even
	ldc.i4		0x00000055
	ldc.i4		0x000000AA
	call	int32 ldc_mul_ovf_u1::u1_29(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
//------------------------------------------------
// -- Even * 0
	ldc.i4		0x000000AA
	ldc.i4		0x00000000
	call	int32 ldc_mul_ovf_u1::u1_30(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- Even * 1
	ldc.i4		0x000000AA
	ldc.i4		0x00000001
	call	int32 ldc_mul_ovf_u1::u1_31(int32,int32)
	ldc.i4		0x000000AA
	ceq
	brfalse		FAIL
// -- Even * (Max - 1)
	ldc.i4		0x000000AA
	ldc.i4		0x000000FE
	call	int32 ldc_mul_ovf_u1::u1_32(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- Even * Max
	ldc.i4		0x000000AA
	ldc.i4		0x000000FF
	call	int32 ldc_mul_ovf_u1::u1_33(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- Even * Odd
	ldc.i4		0x000000AA
	ldc.i4		0x00000055
	call	int32 ldc_mul_ovf_u1::u1_34(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- Even * Even
	ldc.i4		0x000000AA
	ldc.i4		0x000000AA
	call	int32 ldc_mul_ovf_u1::u1_35(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
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


.assembly ldc_mul_ovf_u1{}

