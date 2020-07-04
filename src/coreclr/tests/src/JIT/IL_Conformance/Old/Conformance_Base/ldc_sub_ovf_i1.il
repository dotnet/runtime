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
.class public		ldc_sub_ovf_i1 {

//---- GLOBAL DATA ----------


//---- METHODS --------------


.method public static int32 i1_0(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFF80
		ldc.i4		0xFFFFFF80
	sub
	conv.ovf.i1
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_1(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFF80
		ldc.i4		0xFFFFFFFF
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFFF81
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFFF81
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_2(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFF80
		ldc.i4		0x00000000
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFFF80
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFFF80
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_3(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFF80
		ldc.i4		0x00000001
	sub
	conv.ovf.i1
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_4(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFF80
		ldc.i4		0x0000007F
	sub
	conv.ovf.i1
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_5(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFF80
		ldc.i4		0x00000055
	sub
	conv.ovf.i1
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_6(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFF80
		ldc.i4		0xFFFFFFAA
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFFFD6
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFFFD6
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_7(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0xFFFFFF80
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x0000007F
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x0000007F
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_8(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0xFFFFFFFF
	sub
	conv.ovf.i1
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_9(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0x00000000
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFFFFF
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFFFFF
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_10(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0x00000001
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFFFFE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFFFFE
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_11(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0x0000007F
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFFF80
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFFF80
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_12(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0x00000055
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFFFAA
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFFFAA
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_13(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0xFFFFFFAA
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00000055
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00000055
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_14(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0xFFFFFF80
	sub
	conv.ovf.i1
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_15(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0xFFFFFFFF
	sub
	conv.ovf.i1
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_16(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x00000000
	sub
	conv.ovf.i1
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_17(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x00000001
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFFFFF
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFFFFF
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_18(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x0000007F
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFFF81
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFFF81
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_19(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x00000055
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFFFAB
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFFFAB
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_20(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0xFFFFFFAA
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00000056
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00000056
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_21(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0xFFFFFF80
	sub
	conv.ovf.i1
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_22(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0xFFFFFFFF
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00000002
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00000002
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_23(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x00000000
	sub
	conv.ovf.i1
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_24(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x00000001
	sub
	conv.ovf.i1
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_25(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x0000007F
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFFF82
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFFF82
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_26(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x00000055
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFFFAC
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFFFAC
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_27(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0xFFFFFFAA
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00000057
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00000057
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_28(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x0000007F
		ldc.i4		0xFFFFFF80
	sub
	conv.ovf.i1
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_29(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x0000007F
		ldc.i4		0xFFFFFFFF
	sub
	conv.ovf.i1
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_30(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x0000007F
		ldc.i4		0x00000000
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x0000007F
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x0000007F
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_31(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x0000007F
		ldc.i4		0x00000001
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x0000007E
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x0000007E
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_32(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x0000007F
		ldc.i4		0x0000007F
	sub
	conv.ovf.i1
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_33(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x0000007F
		ldc.i4		0x00000055
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x0000002A
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x0000002A
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_34(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x0000007F
		ldc.i4		0xFFFFFFAA
	sub
	conv.ovf.i1
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_35(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x00000055
		ldc.i4		0xFFFFFF80
	sub
	conv.ovf.i1
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_36(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x00000055
		ldc.i4		0xFFFFFFFF
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00000056
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00000056
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_37(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x00000055
		ldc.i4		0x00000000
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00000055
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00000055
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_38(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x00000055
		ldc.i4		0x00000001
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00000054
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00000054
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_39(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x00000055
		ldc.i4		0x0000007F
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFFFD6
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFFFD6
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_40(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x00000055
		ldc.i4		0x00000055
	sub
	conv.ovf.i1
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_41(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0x00000055
		ldc.i4		0xFFFFFFAA
	sub
	conv.ovf.i1
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_42(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFAA
		ldc.i4		0xFFFFFF80
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x0000002A
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x0000002A
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_43(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFAA
		ldc.i4		0xFFFFFFFF
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFFFAB
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFFFAB
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_44(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFAA
		ldc.i4		0x00000000
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFFFAA
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFFFAA
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_45(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFAA
		ldc.i4		0x00000001
	sub
	conv.ovf.i1
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFFFA9
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFFFA9
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_46(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFAA
		ldc.i4		0x0000007F
	sub
	conv.ovf.i1
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_47(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFAA
		ldc.i4		0x00000055
	sub
	conv.ovf.i1
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i1_48(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int8)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFAA
		ldc.i4		0xFFFFFFAA
	sub
	conv.ovf.i1
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
//---- CONSTRUCTOR ----------
.method public		void ldc_sub_ovf_i1() {
.maxstack	0
	ret
}

//---- MAIN -----------------
.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
//====== begin testing ======
// -- Min - Min should overflow
	ldc.i4		0xFFFFFF80
	ldc.i4		0xFFFFFF80
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_i1::i1_0(int32,int32,int32)
	brfalse		FAIL
// -- Min - -1
	ldc.i4		0xFFFFFF80
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFF81
	call	int32 ldc_sub_ovf_i1::i1_1(int32,int32,int32)
	brfalse		FAIL
// -- Min - 0
	ldc.i4		0xFFFFFF80
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFF80
	call	int32 ldc_sub_ovf_i1::i1_2(int32,int32,int32)
	brfalse		FAIL
// -- Min - 1
	ldc.i4		0xFFFFFF80
	ldc.i4		0x00000001
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i1::i1_3(int32,int32,int32)
	brfalse		FAIL
// -- Min - Max
	ldc.i4		0xFFFFFF80
	ldc.i4		0x0000007F
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i1::i1_4(int32,int32,int32)
	brfalse		FAIL
// -- Min - Odd
	ldc.i4		0xFFFFFF80
	ldc.i4		0x00000055
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i1::i1_5(int32,int32,int32)
	brfalse		FAIL
// -- Min - Even
	ldc.i4		0xFFFFFF80
	ldc.i4		0xFFFFFFAA
	ldc.i4		0xFFFFFFD6
	call	int32 ldc_sub_ovf_i1::i1_6(int32,int32,int32)
	brfalse		FAIL
//----------------------------------------------------------
// -- -1 - Min
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFF80
	ldc.i4		0x0000007F
	call	int32 ldc_sub_ovf_i1::i1_7(int32,int32,int32)
	brfalse		FAIL
// -- -1 - -1
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_i1::i1_8(int32,int32,int32)
	brfalse		FAIL
// -- -1 - 0
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFFF
	call	int32 ldc_sub_ovf_i1::i1_9(int32,int32,int32)
	brfalse		FAIL
// -- -1 - 1
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFE
	call	int32 ldc_sub_ovf_i1::i1_10(int32,int32,int32)
	brfalse		FAIL
// -- -1 - Max
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFF80
	call	int32 ldc_sub_ovf_i1::i1_11(int32,int32,int32)
	brfalse		FAIL
// -- -1 - Odd
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000055
	ldc.i4		0xFFFFFFAA
	call	int32 ldc_sub_ovf_i1::i1_12(int32,int32,int32)
	brfalse		FAIL
// -- -1 - Even
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000055
	call	int32 ldc_sub_ovf_i1::i1_13(int32,int32,int32)
	brfalse		FAIL
//----------------------------------------------------------
// -- 0 - Min
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFF80
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i1::i1_14(int32,int32,int32)
	brfalse		FAIL
// -- 0 - -1
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000001
	call	int32 ldc_sub_ovf_i1::i1_15(int32,int32,int32)
	brfalse		FAIL
// -- 0 - 0
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_i1::i1_16(int32,int32,int32)
	brfalse		FAIL
// -- 0 - 1
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFF
	call	int32 ldc_sub_ovf_i1::i1_17(int32,int32,int32)
	brfalse		FAIL
// -- 0 - Max
	ldc.i4		0x00000000
	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFF81
	call	int32 ldc_sub_ovf_i1::i1_18(int32,int32,int32)
	brfalse		FAIL
// -- 0 - Odd
	ldc.i4		0x00000000
	ldc.i4		0x00000055
	ldc.i4		0xFFFFFFAB
	call	int32 ldc_sub_ovf_i1::i1_19(int32,int32,int32)
	brfalse		FAIL
// -- 0 - Even
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000056
	call	int32 ldc_sub_ovf_i1::i1_20(int32,int32,int32)
	brfalse		FAIL

//----------------------------------------------------------
// -- 1 - Min
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFF80
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i1::i1_21(int32,int32,int32)
	brfalse		FAIL
// -- 1 - -1
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000002
	call	int32 ldc_sub_ovf_i1::i1_22(int32,int32,int32)
	brfalse		FAIL
// -- 1 - 0
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	call	int32 ldc_sub_ovf_i1::i1_23(int32,int32,int32)
	brfalse		FAIL
// -- 1 - 1
	ldc.i4		0x00000001
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_i1::i1_24(int32,int32,int32)
	brfalse		FAIL
// -- 1 - Max
	ldc.i4		0x00000001
	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFF82
	call	int32 ldc_sub_ovf_i1::i1_25(int32,int32,int32)
	brfalse		FAIL
// -- 1 - Odd
	ldc.i4		0x00000001
	ldc.i4		0x00000055
	ldc.i4		0xFFFFFFAC
	call	int32 ldc_sub_ovf_i1::i1_26(int32,int32,int32)
	brfalse		FAIL
// -- 1 - Even
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000057
	call	int32 ldc_sub_ovf_i1::i1_27(int32,int32,int32)
	brfalse		FAIL

//----------------------------------------------------------
// -- Max - Min
	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFF80
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i1::i1_28(int32,int32,int32)
	brfalse		FAIL
// -- Max - -1
	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i1::i1_29(int32,int32,int32)
	brfalse		FAIL
// -- Max - 0
	ldc.i4		0x0000007F
	ldc.i4		0x00000000
	ldc.i4		0x0000007F
	call	int32 ldc_sub_ovf_i1::i1_30(int32,int32,int32)
	brfalse		FAIL
// -- Max - 1
	ldc.i4		0x0000007F
	ldc.i4		0x00000001
	ldc.i4		0x0000007E
	call	int32 ldc_sub_ovf_i1::i1_31(int32,int32,int32)
	brfalse		FAIL
// -- Max - Max
	ldc.i4		0x0000007F
	ldc.i4		0x0000007F
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_i1::i1_32(int32,int32,int32)
	brfalse		FAIL
// -- Max - Odd
	ldc.i4		0x0000007F
	ldc.i4		0x00000055
	ldc.i4		0x0000002A
	call	int32 ldc_sub_ovf_i1::i1_33(int32,int32,int32)
	brfalse		FAIL
// -- Max - Even
	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFFAA
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i1::i1_34(int32,int32,int32)
	brfalse		FAIL

//----------------------------------------------------------
// -- Odd - Min
	ldc.i4		0x00000055
	ldc.i4		0xFFFFFF80
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i1::i1_35(int32,int32,int32)
	brfalse		FAIL
// -- Odd - -1
	ldc.i4		0x00000055
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000056
	call	int32 ldc_sub_ovf_i1::i1_36(int32,int32,int32)
	brfalse		FAIL
// -- Odd - 0
	ldc.i4		0x00000055
	ldc.i4		0x00000000
	ldc.i4		0x00000055
	call	int32 ldc_sub_ovf_i1::i1_37(int32,int32,int32)
	brfalse		FAIL
// -- Odd - 1
	ldc.i4		0x00000055
	ldc.i4		0x00000001
	ldc.i4		0x00000054
	call	int32 ldc_sub_ovf_i1::i1_38(int32,int32,int32)
	brfalse		FAIL
// -- Odd - Max
	ldc.i4		0x00000055
	ldc.i4		0x0000007F
	ldc.i4		0xFFFFFFD6
	call	int32 ldc_sub_ovf_i1::i1_39(int32,int32,int32)
	brfalse		FAIL
// -- Odd - Odd
	ldc.i4		0x00000055
	ldc.i4		0x00000055
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_i1::i1_40(int32,int32,int32)
	brfalse		FAIL
// -- Odd - Even
	ldc.i4		0x00000055
	ldc.i4		0xFFFFFFAA
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i1::i1_41(int32,int32,int32)
	brfalse		FAIL

//----------------------------------------------------------
// -- Even - Min
	ldc.i4		0xFFFFFFAA
	ldc.i4		0xFFFFFF80
	ldc.i4		0x0000002A
	call	int32 ldc_sub_ovf_i1::i1_42(int32,int32,int32)
	brfalse		FAIL
// -- Even - -1
	ldc.i4		0xFFFFFFAA
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFAB
	call	int32 ldc_sub_ovf_i1::i1_43(int32,int32,int32)
	brfalse		FAIL
// -- Even - 0
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFAA
	call	int32 ldc_sub_ovf_i1::i1_44(int32,int32,int32)
	brfalse		FAIL
// -- Even - 1
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFA9
	call	int32 ldc_sub_ovf_i1::i1_45(int32,int32,int32)
	brfalse		FAIL
// -- Even - Max
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x0000007F
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i1::i1_46(int32,int32,int32)
	brfalse		FAIL
// -- Even - Odd
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000055
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i1::i1_47(int32,int32,int32)
	brfalse		FAIL
// -- Even - Even
	ldc.i4		0xFFFFFFAA
	ldc.i4		0xFFFFFFAA
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_i1::i1_48(int32,int32,int32)
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

.assembly ldc_sub_ovf_i1.exe{}

