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
.class public		ldc_add_ovf_u2 {

//---- GLOBAL DATA ----------


//---- METHODS --------------


.method public static int32 u2_0(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x00000000
	add
	conv.ovf.u2
		ldc.i4		0x00000000
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
.method public static int32 u2_1(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x00000001
	add
	conv.ovf.u2
		ldc.i4		0x00000001
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
.method public static int32 u2_2(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x0000FFFE
	add
	conv.ovf.u2
		ldc.i4		0x0000FFFE
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
.method public static int32 u2_3(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x0000FFFF
	add
	conv.ovf.u2
		ldc.i4		0x0000FFFF
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
.method public static int32 u2_4(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x00005555
	add
	conv.ovf.u2
		ldc.i4		0x00005555
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
.method public static int32 u2_5(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x0000AAAA
	add
	conv.ovf.u2
		ldc.i4		0x0000AAAA
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
.method public static int32 u2_6(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x00000000
	add
	conv.ovf.u2
		ldc.i4		0x00000001
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
.method public static int32 u2_7(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x00000001
	add
	conv.ovf.u2
		ldc.i4		0x00000002
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
.method public static int32 u2_8(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x0000FFFE
	add
	conv.ovf.u2
		ldc.i4		0x0000FFFF
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
.method public static int32 u2_9(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x0000FFFF
	add
	conv.ovf.u2
		ldc.i4		0x00000000
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
.method public static int32 u2_10(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x00005555
	add
	conv.ovf.u2
		ldc.i4		0x00005556
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
.method public static int32 u2_11(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x0000AAAA
	add
	conv.ovf.u2
		ldc.i4		0x0000AAAB
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
.method public static int32 u2_12(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFE
		ldc.i4		0x00000000
	add
	conv.ovf.u2
		ldc.i4		0x0000FFFE
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
.method public static int32 u2_13(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFE
		ldc.i4		0x00000001
	add
	conv.ovf.u2
		ldc.i4		0x0000FFFF
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
.method public static int32 u2_14(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFE
		ldc.i4		0x0000FFFE
	add
	conv.ovf.u2
		ldc.i4		0x00000000
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
.method public static int32 u2_15(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFE
		ldc.i4		0x0000FFFF
	add
	conv.ovf.u2
		ldc.i4		0x00000000
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
.method public static int32 u2_16(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFE
		ldc.i4		0x00005555
	add
	conv.ovf.u2
		ldc.i4		0x00000000
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
.method public static int32 u2_17(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFE
		ldc.i4		0x0000AAAA
	add
	conv.ovf.u2
		ldc.i4		0x00000000
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
.method public static int32 u2_18(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFF
		ldc.i4		0x00000000
	add
	conv.ovf.u2
		ldc.i4		0x0000FFFF
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
.method public static int32 u2_19(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFF
		ldc.i4		0x00000001
	add
	conv.ovf.u2
		ldc.i4		0x00000000
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
.method public static int32 u2_20(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFF
		ldc.i4		0x0000FFFE
	add
	conv.ovf.u2
		ldc.i4		0x00000000
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
.method public static int32 u2_21(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFF
		ldc.i4		0x0000FFFF
	add
	conv.ovf.u2
		ldc.i4		0x00000000
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
.method public static int32 u2_22(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFF
		ldc.i4		0x00005555
	add
	conv.ovf.u2
		ldc.i4		0x00000000
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
.method public static int32 u2_23(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x0000FFFF
		ldc.i4		0x0000AAAA
	add
	conv.ovf.u2
		ldc.i4		0x00000000
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
.method public static int32 u2_24(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x00005555
		ldc.i4		0x00000000
	add
	conv.ovf.u2
		ldc.i4		0x00005555
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
.method public static int32 u2_25(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x00005555
		ldc.i4		0x00000001
	add
	conv.ovf.u2
		ldc.i4		0x00005556
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
.method public static int32 u2_26(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x00005555
		ldc.i4		0x0000FFFE
	add
	conv.ovf.u2
		ldc.i4		0x00000000
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
.method public static int32 u2_27(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x00005555
		ldc.i4		0x0000FFFF
	add
	conv.ovf.u2
		ldc.i4		0x00000000
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
.method public static int32 u2_28(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x00005555
		ldc.i4		0x00005555
	add
	conv.ovf.u2
		ldc.i4		0x0000AAAA
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
.method public static int32 u2_29(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x00005555
		ldc.i4		0x0000AAAA
	add
	conv.ovf.u2
		ldc.i4		0x0000FFFF
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
.method public static int32 u2_30(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x0000AAAA
		ldc.i4		0x00000000
	add
	conv.ovf.u2
		ldc.i4		0x0000AAAA
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
.method public static int32 u2_31(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x0000AAAA
		ldc.i4		0x00000001
	add
	conv.ovf.u2
		ldc.i4		0x0000AAAB
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
.method public static int32 u2_32(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x0000AAAA
		ldc.i4		0x0000FFFE
	add
	conv.ovf.u2
		ldc.i4		0x00000000
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
.method public static int32 u2_33(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x0000AAAA
		ldc.i4		0x0000FFFF
	add
	conv.ovf.u2
		ldc.i4		0x00000000
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
.method public static int32 u2_34(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x0000AAAA
		ldc.i4		0x00005555
	add
	conv.ovf.u2
		ldc.i4		0x0000FFFF
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
.method public static int32 u2_35(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
		ldc.i4		0x0000AAAA
		ldc.i4		0x0000AAAA
	add
	conv.ovf.u2
		ldc.i4		0x00000000
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
//---- CONSTRUCTOR ----------
.method public		void ldc_add_ovf_u2() {
.maxstack	0
	ret
}

//---- MAIN -----------------
.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
//====== begin testing ======
// -- 0 + 0
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	call	int32 ldc_add_ovf_u2::u2_0(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
// -- 0 + 1
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	ldc.i4		0x00000001
	call	int32 ldc_add_ovf_u2::u2_1(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
// -- 0 + (Max - 1)
	ldc.i4		0x00000000
	ldc.i4		0x0000FFFE
	ldc.i4		0x0000FFFE
	call	int32 ldc_add_ovf_u2::u2_2(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
// -- 0 + Max
	ldc.i4		0x00000000
	ldc.i4		0x0000FFFF
	ldc.i4		0x0000FFFF
	call	int32 ldc_add_ovf_u2::u2_3(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
// -- 0 + Odd
	ldc.i4		0x00000000
	ldc.i4		0x00005555
	ldc.i4		0x00005555
	call	int32 ldc_add_ovf_u2::u2_4(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
// -- 0 + Even
	ldc.i4		0x00000000
	ldc.i4		0x0000AAAA
	ldc.i4		0x0000AAAA
	call	int32 ldc_add_ovf_u2::u2_5(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
//------------------------------------------------
// -- 1 + 0
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	call	int32 ldc_add_ovf_u2::u2_6(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
// -- 1 + 1
	ldc.i4		0x00000001
	ldc.i4		0x00000001
	ldc.i4		0x00000002
	call	int32 ldc_add_ovf_u2::u2_7(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
// -- 1 + (Max - 1)
	ldc.i4		0x00000001
	ldc.i4		0x0000FFFE
	ldc.i4		0x0000FFFF
	call	int32 ldc_add_ovf_u2::u2_8(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
// -- 1 + Max
	ldc.i4		0x00000001
	ldc.i4		0x0000FFFF
	ldc.i4		0x00000000
	call	int32 ldc_add_ovf_u2::u2_9(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
// -- 1 + Odd
	ldc.i4		0x00000001
	ldc.i4		0x00005555
	ldc.i4		0x00005556
	call	int32 ldc_add_ovf_u2::u2_10(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
// -- 1 + Even
	ldc.i4		0x00000001
	ldc.i4		0x0000AAAA
	ldc.i4		0x0000AAAB
	call	int32 ldc_add_ovf_u2::u2_11(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
//------------------------------------------------
// -- (Max - 1) + 0
	ldc.i4		0x0000FFFE
	ldc.i4		0x00000000
	ldc.i4		0x0000FFFE
	call	int32 ldc_add_ovf_u2::u2_12(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
// -- (Max - 1) + 1
	ldc.i4		0x0000FFFE
	ldc.i4		0x00000001
	ldc.i4		0x0000FFFF
	call	int32 ldc_add_ovf_u2::u2_13(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
// -- (Max - 1) + (Max - 1)
	ldc.i4		0x0000FFFE
	ldc.i4		0x0000FFFE
	ldc.i4		0x00000000
	call	int32 ldc_add_ovf_u2::u2_14(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
// -- (Max - 1) + Max
	ldc.i4		0x0000FFFE
	ldc.i4		0x0000FFFF
	ldc.i4		0x00000000
	call	int32 ldc_add_ovf_u2::u2_15(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
// -- (Max - 1) + Odd
	ldc.i4		0x0000FFFE
	ldc.i4		0x00005555
	ldc.i4		0x00000000
	call	int32 ldc_add_ovf_u2::u2_16(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
// -- (Max - 1) + Even
	ldc.i4		0x0000FFFE
	ldc.i4		0x0000AAAA
	ldc.i4		0x00000000
	call	int32 ldc_add_ovf_u2::u2_17(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
//------------------------------------------------

// -- Max + 0
	ldc.i4		0x0000FFFF
	ldc.i4		0x00000000
	ldc.i4		0x0000FFFF
	call	int32 ldc_add_ovf_u2::u2_18(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
// -- Max + 1
	ldc.i4		0x0000FFFF
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	call	int32 ldc_add_ovf_u2::u2_19(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
// -- Max + (Max - 1)
	ldc.i4		0x0000FFFF
	ldc.i4		0x0000FFFE
	ldc.i4		0x00000000
	call	int32 ldc_add_ovf_u2::u2_20(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
// -- Max + Max
	ldc.i4		0x0000FFFF
	ldc.i4		0x0000FFFF
	ldc.i4		0x00000000
	call	int32 ldc_add_ovf_u2::u2_21(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
// -- Max + Odd
	ldc.i4		0x0000FFFF
	ldc.i4		0x00005555
	ldc.i4		0x00000000
	call	int32 ldc_add_ovf_u2::u2_22(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
// -- Max + Even
	ldc.i4		0x0000FFFF
	ldc.i4		0x0000AAAA
	ldc.i4		0x00000000
	call	int32 ldc_add_ovf_u2::u2_23(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
//------------------------------------------------
// -- Odd + 0
	ldc.i4		0x00005555
	ldc.i4		0x00000000
	ldc.i4		0x00005555
	call	int32 ldc_add_ovf_u2::u2_24(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
// -- Odd + 1
	ldc.i4		0x00005555
	ldc.i4		0x00000001
	ldc.i4		0x00005556
	call	int32 ldc_add_ovf_u2::u2_25(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
// -- Odd + (Max - 1)
	ldc.i4		0x00005555
	ldc.i4		0x0000FFFE
	ldc.i4		0x00000000
	call	int32 ldc_add_ovf_u2::u2_26(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
// -- Odd + Max
	ldc.i4		0x00005555
	ldc.i4		0x0000FFFF
	ldc.i4		0x00000000
	call	int32 ldc_add_ovf_u2::u2_27(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
// -- Odd + Odd
	ldc.i4		0x00005555
	ldc.i4		0x00005555
	ldc.i4		0x0000AAAA
	call	int32 ldc_add_ovf_u2::u2_28(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
// -- Odd + Even
	ldc.i4		0x00005555
	ldc.i4		0x0000AAAA
	ldc.i4		0x0000FFFF
	call	int32 ldc_add_ovf_u2::u2_29(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
//------------------------------------------------
// -- Even + 0
	ldc.i4		0x0000AAAA
	ldc.i4		0x00000000
	ldc.i4		0x0000AAAA
	call	int32 ldc_add_ovf_u2::u2_30(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
// -- Even + 1
	ldc.i4		0x0000AAAA
	ldc.i4		0x00000001
	ldc.i4		0x0000AAAB
	call	int32 ldc_add_ovf_u2::u2_31(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
// -- Even + (Max - 1)
	ldc.i4		0x0000AAAA
	ldc.i4		0x0000FFFE
	ldc.i4		0x00000000
	call	int32 ldc_add_ovf_u2::u2_32(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
// -- Even + Max
	ldc.i4		0x0000AAAA
	ldc.i4		0x0000FFFF
	ldc.i4		0x00000000
	call	int32 ldc_add_ovf_u2::u2_33(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
	ceq
	brfalse		FAIL
// -- Even + Odd
	ldc.i4		0x0000AAAA
	ldc.i4		0x00005555
	ldc.i4		0x0000FFFF
	call	int32 ldc_add_ovf_u2::u2_34(int32,int32,int32)
	ldc.i4		0x11111111
	ceq
	brfalse		FAIL
// -- Even + Even
	ldc.i4		0x0000AAAA
	ldc.i4		0x0000AAAA
	ldc.i4		0x00000000
	call	int32 ldc_add_ovf_u2::u2_35(int32,int32,int32)
	ldc.i4		0xEEEEEEEE
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

.assembly ldc_add_ovf_u2.exe{}

