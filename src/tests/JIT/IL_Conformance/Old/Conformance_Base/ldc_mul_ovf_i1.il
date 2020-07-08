// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

//
//======================================

//---- CLASS ----------------
.class public		ldc_mul_ovf_i1 {

//---- GLOBAL DATA ----------


//---- METHODS --------------


.method public static int32 i1_0(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFF80
		ldc.i4		0xFFFFFF80
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
.method public static int32 i1_1(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFF80
		ldc.i4		0xFFFFFFFF
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
.method public static int32 i1_2(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFF80
		ldc.i4		0x00000000
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
.method public static int32 i1_3(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFF80
		ldc.i4		0x00000001
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
.method public static int32 i1_4(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFF80
		ldc.i4		0x0000007F
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
.method public static int32 i1_5(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFF80
		ldc.i4		0x00000055
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
.method public static int32 i1_6(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFF80
		ldc.i4		0xFFFFFFAA
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
.method public static int32 i1_7(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0xFFFFFF80
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
.method public static int32 i1_8(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0xFFFFFFFF
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
.method public static int32 i1_9(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0x00000000
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
.method public static int32 i1_10(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0x00000001
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
.method public static int32 i1_11(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0x0000007F
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
.method public static int32 i1_12(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0x00000055
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
.method public static int32 i1_13(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0xFFFFFFAA
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
.method public static int32 i1_14(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0xFFFFFF80
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
.method public static int32 i1_15(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0xFFFFFFFF
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
.method public static int32 i1_16(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x00000000
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
.method public static int32 i1_17(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x00000001
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
.method public static int32 i1_18(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x0000007F
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
.method public static int32 i1_19(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x00000055
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
.method public static int32 i1_20(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0xFFFFFFAA
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
.method public static int32 i1_21(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0xFFFFFF80
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
.method public static int32 i1_22(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0xFFFFFFFF
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
.method public static int32 i1_23(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x00000000
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
.method public static int32 i1_24(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x00000001
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
.method public static int32 i1_25(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x0000007F
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
.method public static int32 i1_26(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x00000055
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
.method public static int32 i1_27(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0xFFFFFFAA
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
.method public static int32 i1_28(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x0000007F
		ldc.i4		0xFFFFFF80
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
.method public static int32 i1_29(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x0000007F
		ldc.i4		0xFFFFFFFF
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
.method public static int32 i1_30(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x0000007F
		ldc.i4		0x00000000
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
.method public static int32 i1_31(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x0000007F
		ldc.i4		0x00000001
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
.method public static int32 i1_32(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x0000007F
		ldc.i4		0x0000007F
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
.method public static int32 i1_33(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x0000007F
		ldc.i4		0x00000055
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
.method public static int32 i1_34(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x0000007F
		ldc.i4		0xFFFFFFAA
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
.method public static int32 i1_35(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x00000055
		ldc.i4		0xFFFFFF80
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
.method public static int32 i1_36(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x00000055
		ldc.i4		0xFFFFFFFF
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
.method public static int32 i1_37(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x00000055
		ldc.i4		0x00000000
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
.method public static int32 i1_38(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x00000055
		ldc.i4		0x00000001
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
.method public static int32 i1_39(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x00000055
		ldc.i4		0x0000007F
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
.method public static int32 i1_40(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x00000055
		ldc.i4		0x00000055
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
.method public static int32 i1_41(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0x00000055
		ldc.i4		0xFFFFFFAA
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
.method public static int32 i1_42(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFAA
		ldc.i4		0xFFFFFF80
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
.method public static int32 i1_43(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFAA
		ldc.i4		0xFFFFFFFF
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
.method public static int32 i1_44(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFAA
		ldc.i4		0x00000000
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
.method public static int32 i1_45(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFAA
		ldc.i4		0x00000001
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
.method public static int32 i1_46(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFAA
		ldc.i4		0x0000007F
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
.method public static int32 i1_47(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFAA
		ldc.i4		0x00000055
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
.method public static int32 i1_48(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFAA
		ldc.i4		0xFFFFFFAA
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
//---- CONSTRUCTOR ----------
.method public		void ldc_mul_ovf_i1() {
.maxstack	0
	ret
}

//---- MAIN -----------------
.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
.locals	(int32)
//====== begin testing ======
// -- Min * Min should overflow
	ldc.i4		0xFFFFFF80
	ldc.i4		0xFFFFFF80
	call	int32 ldc_mul_ovf_i1::i1_0(int32,int32)
	stloc	0

	// check if exception generated was bogus
	ldloc	0
	ldc.i4		0xEEEEEEEE
	ceq
	brtrue		FAILEX	// invalid exception was generated

	// check if a valid exception actually happened
	ldloc	0
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL1	// OverflowException did _not occur

// -- Min * -1
	ldc.i4		0xFFFFFF80
	ldc.i4		0xFFFFFFFF
	call	int32 ldc_mul_ovf_i1::i1_1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL2
// -- Min * 0
	ldc.i4		0xFFFFFF80
	ldc.i4		0x00000000
	call	int32 ldc_mul_ovf_i1::i1_2(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL3
// -- Min * 1
	ldc.i4		0xFFFFFF80
	ldc.i4		0x00000001
	call	int32 ldc_mul_ovf_i1::i1_3(int32,int32)
	ldc.i4		0xFFFFFF80
	ceq
	brfalse		FAIL4
// -- Min * Max
	ldc.i4		0xFFFFFF80
	ldc.i4		0x0000007F
	call	int32 ldc_mul_ovf_i1::i1_4(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL5
// -- Min * Odd
	ldc.i4		0xFFFFFF80
	ldc.i4		0x00000055
	call	int32 ldc_mul_ovf_i1::i1_5(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL6
// -- Min * Even
	ldc.i4		0xFFFFFF80
	ldc.i4		0xFFFFFFAA
	call	int32 ldc_mul_ovf_i1::i1_6(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL7

//----------------------------------------------------------
// -- -1 * Min
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFF80
	call	int32 ldc_mul_ovf_i1::i1_7(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL8
// -- -1 * -1
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFFF
	call	int32 ldc_mul_ovf_i1::i1_8(int32,int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL9
// -- -1 * 0
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000000
	call	int32 ldc_mul_ovf_i1::i1_9(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL10
// -- -1 * 1
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000001
	call	int32 ldc_mul_ovf_i1::i1_10(int32,int32)
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL11
// -- -1 * Max
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x0000007F
	call	int32 ldc_mul_ovf_i1::i1_11(int32,int32)
	ldc.i4		0xFFFFFF81
	ceq
	brfalse		FAIL12
// -- -1 * Odd
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000055
	call	int32 ldc_mul_ovf_i1::i1_12(int32,int32)
	ldc.i4		0xFFFFFFAB
	ceq
	brfalse		FAIL13
// -- -1 * Even
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFAA
	call	int32 ldc_mul_ovf_i1::i1_13(int32,int32)
	ldc.i4		0x00000056
	ceq
	brfalse		FAIL14
//----------------------------------------------------------
// -- 0 * Min
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFF80
	call	int32 ldc_mul_ovf_i1::i1_14(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL15
// -- 0 * -1
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFFF
	call	int32 ldc_mul_ovf_i1::i1_15(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL16
// -- 0 * 0
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	call	int32 ldc_mul_ovf_i1::i1_16(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL17
// -- 0 * 1
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	call	int32 ldc_mul_ovf_i1::i1_17(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL18
// -- 0 * Max
	ldc.i4		0x00000000
	ldc.i4		0x0000007F
	call	int32 ldc_mul_ovf_i1::i1_18(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL19
// -- 0 * Odd
	ldc.i4		0x00000000
	ldc.i4		0x00000055
	call	int32 ldc_mul_ovf_i1::i1_19(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL20
// -- 0 * Even
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFAA
	call	int32 ldc_mul_ovf_i1::i1_20(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL21

//----------------------------------------------------------
// -- 1 * Min
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFF80
	call	int32 ldc_mul_ovf_i1::i1_21(int32,int32)
	ldc.i4		0xFFFFFF80
	ceq
	brfalse		FAIL22
// -- 1 * -1
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFF
	call	int32 ldc_mul_ovf_i1::i1_22(int32,int32)
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL23
// -- 1 * 0
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	call	int32 ldc_mul_ovf_i1::i1_23(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL24
// -- 1 * 1
	ldc.i4		0x00000001
	ldc.i4		0x00000001
	call	int32 ldc_mul_ovf_i1::i1_24(int32,int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL25
// -- 1 * Max
	ldc.i4		0x00000001
	ldc.i4		0x0000007F
	call	int32 ldc_mul_ovf_i1::i1_25(int32,int32)
	ldc.i4		0x0000007F
	ceq
	brfalse		FAIL26
// -- 1 * Odd
	ldc.i4		0x00000001
	ldc.i4		0x00000055
	call	int32 ldc_mul_ovf_i1::i1_26(int32,int32)
	ldc.i4		0x00000055
	ceq
	brfalse		FAIL27
// -- 1 * Even
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFAA
	call	int32 ldc_mul_ovf_i1::i1_27(int32,int32)
	ldc.i4		0xFFFFFFAA
	ceq
	brfalse		FAIL28

//----------------------------------------------------------
// -- Max * Min
	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFF80
	call	int32 ldc_mul_ovf_i1::i1_28(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL29
// -- Max * -1
	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFFFF
	call	int32 ldc_mul_ovf_i1::i1_29(int32,int32)
	ldc.i4		0xFFFFFF81
	ceq
	brfalse		FAIL30
// -- Max * 0
	ldc.i4		0x0000007F
	ldc.i4		0x00000000
	call	int32 ldc_mul_ovf_i1::i1_30(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL31
// -- Max * 1
	ldc.i4		0x0000007F
	ldc.i4		0x00000001
	call	int32 ldc_mul_ovf_i1::i1_31(int32,int32)
	ldc.i4		0x0000007F
	ceq
	brfalse		FAIL32
// -- Max * Max
	ldc.i4		0x0000007F
	ldc.i4		0x0000007F
	call	int32 ldc_mul_ovf_i1::i1_32(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL33
// -- Max * Odd
	ldc.i4		0x0000007F
	ldc.i4		0x00000055
	call	int32 ldc_mul_ovf_i1::i1_33(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL34
// -- Max * Even
	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFFAA
	call	int32 ldc_mul_ovf_i1::i1_34(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL35

//----------------------------------------------------------
// -- Odd * Min
	ldc.i4		0x00000055
	ldc.i4		0xFFFFFF80
	call	int32 ldc_mul_ovf_i1::i1_35(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL36
// -- Odd * -1
	ldc.i4		0x00000055
	ldc.i4		0xFFFFFFFF
	call	int32 ldc_mul_ovf_i1::i1_36(int32,int32)
	ldc.i4		0xFFFFFFAB
	ceq
	brfalse		FAIL37
// -- Odd * 0
	ldc.i4		0x00000055
	ldc.i4		0x00000000
	call	int32 ldc_mul_ovf_i1::i1_37(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL38
// -- Odd * 1
	ldc.i4		0x00000055
	ldc.i4		0x00000001
	call	int32 ldc_mul_ovf_i1::i1_38(int32,int32)
	ldc.i4		0x00000055
	ceq
	brfalse		FAIL39
// -- Odd * Max
	ldc.i4		0x00000055
	ldc.i4		0x0000007F
	call	int32 ldc_mul_ovf_i1::i1_39(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL40
// -- Odd * Odd
	ldc.i4		0x00000055
	ldc.i4		0x00000055
	call	int32 ldc_mul_ovf_i1::i1_40(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL41
// -- Odd * Even
	ldc.i4		0x00000055
	ldc.i4		0xFFFFFFAA
	call	int32 ldc_mul_ovf_i1::i1_41(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL42

//----------------------------------------------------------
// -- Even * Min
	ldc.i4		0xFFFFFFAA
	ldc.i4		0xFFFFFF80
	call	int32 ldc_mul_ovf_i1::i1_42(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL43
// -- Even * -1
	ldc.i4		0xFFFFFFAA
	ldc.i4		0xFFFFFFFF
	call	int32 ldc_mul_ovf_i1::i1_43(int32,int32)
	ldc.i4		0x00000056
	ceq
	brfalse		FAIL44
// -- Even * 0
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000000
	call	int32 ldc_mul_ovf_i1::i1_44(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL45
// -- Even * 1
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000001
	call	int32 ldc_mul_ovf_i1::i1_45(int32,int32)
	ldc.i4		0xFFFFFFAA
	ceq
	brfalse		FAIL46
// -- Even * Max
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x0000007F
	call	int32 ldc_mul_ovf_i1::i1_46(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL47
// -- Even * Odd
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000055
	call	int32 ldc_mul_ovf_i1::i1_47(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL48
// -- Even * Even
	ldc.i4		0xFFFFFFAA
	ldc.i4		0xFFFFFFAA
	call	int32 ldc_mul_ovf_i1::i1_48(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL49

//====== end testing ========

//---- branch here on pass --
PASS:
	ldc.i4 100
	br		END

//---- branch here on individual fails --
FAIL1:
	ldc.i4 100
	br		END
FAIL2:
	ldc.i4 100
	br		END
FAIL3:
	ldc.i4 100
	br		END
FAIL4:
	ldc.i4 100
	br		END
FAIL5:
	ldc.i4 100
	br		END
FAIL6:
	ldc.i4 100
	br		END
FAIL7:
	ldc.i4 100
	br		END
FAIL8:
	ldc.i4 100
	br		END
FAIL9:
	ldc.i4 100
	br		END
FAIL10:
	ldc.i4 100
	br		END
FAIL11:
	ldc.i4 100
	br		END
FAIL12:
	ldc.i4 100
	br		END
FAIL13:
	ldc.i4 100
	br		END
FAIL14:
	ldc.i4 100
	br		END
FAIL15:
	ldc.i4 100
	br		END
FAIL16:
	ldc.i4 100
	br		END
FAIL17:
	ldc.i4 100
	br		END
FAIL18:
	ldc.i4 100
	br		END
FAIL19:
	ldc.i4 100
	br		END

FAIL20:
	ldc.i4 100
	br		END
FAIL21:
	ldc.i4 100
	br		END
FAIL22:
	ldc.i4 100
	br		END
FAIL23:
	ldc.i4 100
	br		END
FAIL24:
	ldc.i4 100
	br		END
FAIL25:
	ldc.i4 100
	br		END
FAIL26:
	ldc.i4 100
	br		END
FAIL27:
	ldc.i4 100
	br		END
FAIL28:
	ldc.i4 100
	br		END
FAIL29:
	ldc.i4 100
	br		END

FAIL30:
	ldc.i4 100
	br		END
FAIL31:
	ldc.i4 100
	br		END
FAIL32:
	ldc.i4 100
	br		END
FAIL33:
	ldc.i4 100
	br		END
FAIL34:
	ldc.i4 100
	br		END
FAIL35:
	ldc.i4 100
	br		END
FAIL36:
	ldc.i4 100
	br		END
FAIL37:
	ldc.i4 100
	br		END
FAIL38:
	ldc.i4 100
	br		END
FAIL39:
	ldc.i4 100
	br		END

FAIL40:
	ldc.i4 100
	br		END
FAIL41:
	ldc.i4 100
	br		END
FAIL42:
	ldc.i4 100
	br		END
FAIL43:
	ldc.i4 100
	br		END
FAIL44:
	ldc.i4 100
	br		END
FAIL45:
	ldc.i4 100
	br		END
FAIL46:
	ldc.i4 100
	br		END
FAIL47:
	ldc.i4 100
	br		END
FAIL48:
	ldc.i4 100
	br		END
FAIL49:
	ldc.i4 100
	br		END

//---- branch here on invalid exception --
FAILEX:
	ldc.i4 100
	br		END

//---- branch here on fail --
//FAIL:
	ldc.i4 101

//---- return the result ----
END:
	ret
//---- END OF METHOD --------
}
//---- EOF ------------------
}



.assembly ldc_mul_ovf_i1.exe{}

