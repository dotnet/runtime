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
.class public		ldc_mul_ovf_i4 {

//---- GLOBAL DATA ----------


//---- METHODS --------------


.method public static int32 i4_0(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x80000000
		ldc.i4		0x80000000
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_1(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x80000000
		ldc.i4		0xFFFFFFFF
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_2(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x80000000
		ldc.i4		0x00000000
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_3(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x80000000
		ldc.i4		0x00000001
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_4(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x80000000
		ldc.i4		0x7FFFFFFF
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_5(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x80000000
		ldc.i4		0x55555555
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_6(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x80000000
		ldc.i4		0xAAAAAAAA
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_7(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0x80000000
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_8(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0xFFFFFFFF
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_9(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0x00000000
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_10(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0x00000001
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_11(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0x7FFFFFFF
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_12(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0x55555555
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_13(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0xAAAAAAAA
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_14(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x80000000
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_15(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0xFFFFFFFF
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_16(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x00000000
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_17(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x00000001
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_18(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x7FFFFFFF
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_19(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x55555555
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_20(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0xAAAAAAAA
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_21(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x80000000
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_22(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0xFFFFFFFF
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_23(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x00000000
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_24(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x00000001
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_25(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x7FFFFFFF
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_26(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x55555555
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_27(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0xAAAAAAAA
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_28(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x7FFFFFFF
		ldc.i4		0x80000000
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_29(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x7FFFFFFF
		ldc.i4		0xFFFFFFFF
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_30(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x7FFFFFFF
		ldc.i4		0x00000000
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_31(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x7FFFFFFF
		ldc.i4		0x00000001
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_32(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x7FFFFFFF
		ldc.i4		0x7FFFFFFF
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_33(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x7FFFFFFF
		ldc.i4		0x55555555
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_34(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x7FFFFFFF
		ldc.i4		0xAAAAAAAA
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_35(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x55555555
		ldc.i4		0x80000000
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_36(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x55555555
		ldc.i4		0xFFFFFFFF
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_37(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x55555555
		ldc.i4		0x00000000
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_38(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x55555555
		ldc.i4		0x00000001
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_39(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x55555555
		ldc.i4		0x7FFFFFFF
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_40(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x55555555
		ldc.i4		0x55555555
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_41(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0x55555555
		ldc.i4		0xAAAAAAAA
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_42(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0xAAAAAAAA
		ldc.i4		0x80000000
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_43(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0xAAAAAAAA
		ldc.i4		0xFFFFFFFF
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_44(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0xAAAAAAAA
		ldc.i4		0x00000000
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_45(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0xAAAAAAAA
		ldc.i4		0x00000001
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_46(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0xAAAAAAAA
		ldc.i4		0x7FFFFFFF
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_47(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0xAAAAAAAA
		ldc.i4		0x55555555
	mul.ovf
//	conv.ovf.i4
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
.method public static int32 i4_48(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
		ldc.i4		0xAAAAAAAA
		ldc.i4		0xAAAAAAAA
	mul.ovf
//	conv.ovf.i4
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
.method public		void ldc_mul_ovf_i4() {
.maxstack	0
	ret
}

//---- MAIN -----------------
.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
//====== begin testing ======
// -- Min * Min should overflow
	ldc.i4		0x80000000
	ldc.i4		0x80000000
	call	int32 ldc_mul_ovf_i4::i4_0(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- Min * -1
	ldc.i4		0x80000000
	ldc.i4		0xFFFFFFFF
	call	int32 ldc_mul_ovf_i4::i4_1(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- Min * 0
	ldc.i4		0x80000000
	ldc.i4		0x00000000
	call	int32 ldc_mul_ovf_i4::i4_2(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- Min * 1
	ldc.i4		0x80000000
	ldc.i4		0x00000001
	call	int32 ldc_mul_ovf_i4::i4_3(int32,int32)
	ldc.i4		0x80000000
	ceq
	brfalse		FAIL
// -- Min * Max
	ldc.i4		0x80000000
	ldc.i4		0x7FFFFFFF
	call	int32 ldc_mul_ovf_i4::i4_4(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- Min * Odd
	ldc.i4		0x80000000
	ldc.i4		0x55555555
	call	int32 ldc_mul_ovf_i4::i4_5(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- Min * Even
	ldc.i4		0x80000000
	ldc.i4		0xAAAAAAAA
	call	int32 ldc_mul_ovf_i4::i4_6(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL

//----------------------------------------------------------
// -- -1 * Min
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x80000000
	call	int32 ldc_mul_ovf_i4::i4_7(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- -1 * -1
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFFF
	call	int32 ldc_mul_ovf_i4::i4_8(int32,int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL
// -- -1 * 0
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000000
	call	int32 ldc_mul_ovf_i4::i4_9(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- -1 * 1
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000001
	call	int32 ldc_mul_ovf_i4::i4_10(int32,int32)
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL
// -- -1 * Max
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x7FFFFFFF
	call	int32 ldc_mul_ovf_i4::i4_11(int32,int32)
	ldc.i4		0x80000001
	ceq
	brfalse		FAIL
// -- -1 * Odd
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x55555555
	call	int32 ldc_mul_ovf_i4::i4_12(int32,int32)
	ldc.i4		0xAAAAAAAB
	ceq
	brfalse		FAIL
// -- -1 * Even
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xAAAAAAAA
	call	int32 ldc_mul_ovf_i4::i4_13(int32,int32)
	ldc.i4		0x55555556
	ceq
	brfalse		FAIL
//----------------------------------------------------------
// -- 0 * Min
	ldc.i4		0x00000000
	ldc.i4		0x80000000
	call	int32 ldc_mul_ovf_i4::i4_14(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- 0 * -1
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFFF
	call	int32 ldc_mul_ovf_i4::i4_15(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- 0 * 0
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	call	int32 ldc_mul_ovf_i4::i4_16(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- 0 * 1
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	call	int32 ldc_mul_ovf_i4::i4_17(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- 0 * Max
	ldc.i4		0x00000000
	ldc.i4		0x7FFFFFFF
	call	int32 ldc_mul_ovf_i4::i4_18(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- 0 * Odd
	ldc.i4		0x00000000
	ldc.i4		0x55555555
	call	int32 ldc_mul_ovf_i4::i4_19(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- 0 * Even
	ldc.i4		0x00000000
	ldc.i4		0xAAAAAAAA
	call	int32 ldc_mul_ovf_i4::i4_20(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL

//----------------------------------------------------------
// -- 1 * Min
	ldc.i4		0x00000001
	ldc.i4		0x80000000
	call	int32 ldc_mul_ovf_i4::i4_21(int32,int32)
	ldc.i4		0x80000000
	ceq
	brfalse		FAIL
// -- 1 * -1
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFF
	call	int32 ldc_mul_ovf_i4::i4_22(int32,int32)
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL
// -- 1 * 0
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	call	int32 ldc_mul_ovf_i4::i4_23(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- 1 * 1
	ldc.i4		0x00000001
	ldc.i4		0x00000001
	call	int32 ldc_mul_ovf_i4::i4_24(int32,int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL
// -- 1 * Max
	ldc.i4		0x00000001
	ldc.i4		0x7FFFFFFF
	call	int32 ldc_mul_ovf_i4::i4_25(int32,int32)
	ldc.i4		0x7FFFFFFF
	ceq
	brfalse		FAIL
// -- 1 * Odd
	ldc.i4		0x00000001
	ldc.i4		0x55555555
	call	int32 ldc_mul_ovf_i4::i4_26(int32,int32)
	ldc.i4		0x55555555
	ceq
	brfalse		FAIL
// -- 1 * Even
	ldc.i4		0x00000001
	ldc.i4		0xAAAAAAAA
	call	int32 ldc_mul_ovf_i4::i4_27(int32,int32)
	ldc.i4		0xAAAAAAAA
	ceq
	brfalse		FAIL

//----------------------------------------------------------
// -- Max * Min
	ldc.i4		0x7FFFFFFF
	ldc.i4		0x80000000
	call	int32 ldc_mul_ovf_i4::i4_28(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- Max * -1
	ldc.i4		0x7FFFFFFF
	ldc.i4		0xFFFFFFFF
	call	int32 ldc_mul_ovf_i4::i4_29(int32,int32)
	ldc.i4		0x80000001
	ceq
	brfalse		FAIL
// -- Max * 0
	ldc.i4		0x7FFFFFFF
	ldc.i4		0x00000000
	call	int32 ldc_mul_ovf_i4::i4_30(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- Max * 1
	ldc.i4		0x7FFFFFFF
	ldc.i4		0x00000001
	call	int32 ldc_mul_ovf_i4::i4_31(int32,int32)
	ldc.i4		0x7FFFFFFF
	ceq
	brfalse		FAIL
// -- Max * Max
	ldc.i4		0x7FFFFFFF
	ldc.i4		0x7FFFFFFF
	call	int32 ldc_mul_ovf_i4::i4_32(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- Max * Odd
	ldc.i4		0x7FFFFFFF
	ldc.i4		0x55555555
	call	int32 ldc_mul_ovf_i4::i4_33(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- Max * Even
	ldc.i4		0x7FFFFFFF
	ldc.i4		0xAAAAAAAA
	call	int32 ldc_mul_ovf_i4::i4_34(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL

//----------------------------------------------------------
// -- Odd * Min
	ldc.i4		0x55555555
	ldc.i4		0x80000000
	call	int32 ldc_mul_ovf_i4::i4_35(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- Odd * -1
	ldc.i4		0x55555555
	ldc.i4		0xFFFFFFFF
	call	int32 ldc_mul_ovf_i4::i4_36(int32,int32)
	ldc.i4		0xAAAAAAAB
	ceq
	brfalse		FAIL
// -- Odd * 0
	ldc.i4		0x55555555
	ldc.i4		0x00000000
	call	int32 ldc_mul_ovf_i4::i4_37(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- Odd * 1
	ldc.i4		0x55555555
	ldc.i4		0x00000001
	call	int32 ldc_mul_ovf_i4::i4_38(int32,int32)
	ldc.i4		0x55555555
	ceq
	brfalse		FAIL
// -- Odd * Max
	ldc.i4		0x55555555
	ldc.i4		0x7FFFFFFF
	call	int32 ldc_mul_ovf_i4::i4_39(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- Odd * Odd
	ldc.i4		0x55555555
	ldc.i4		0x55555555
	call	int32 ldc_mul_ovf_i4::i4_40(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- Odd * Even
	ldc.i4		0x55555555
	ldc.i4		0xAAAAAAAA
	call	int32 ldc_mul_ovf_i4::i4_41(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL

//----------------------------------------------------------
// -- Even * Min
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x80000000
	call	int32 ldc_mul_ovf_i4::i4_42(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- Even * -1
	ldc.i4		0xAAAAAAAA
	ldc.i4		0xFFFFFFFF
	call	int32 ldc_mul_ovf_i4::i4_43(int32,int32)
	ldc.i4		0x55555556
	ceq
	brfalse		FAIL
// -- Even * 0
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x00000000
	call	int32 ldc_mul_ovf_i4::i4_44(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- Even * 1
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x00000001
	call	int32 ldc_mul_ovf_i4::i4_45(int32,int32)
	ldc.i4		0xAAAAAAAA
	ceq
	brfalse		FAIL
// -- Even * Max
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x7FFFFFFF
	call	int32 ldc_mul_ovf_i4::i4_46(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- Even * Odd
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x55555555
	call	int32 ldc_mul_ovf_i4::i4_47(int32,int32)
	ldc.i4		0xAEAEAEAE
	ceq
	brfalse		FAIL
// -- Even * Even
	ldc.i4		0xAAAAAAAA
	ldc.i4		0xAAAAAAAA
	call	int32 ldc_mul_ovf_i4::i4_48(int32,int32)
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

.assembly ldc_mul_ovf_i4.exe{}

