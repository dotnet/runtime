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
.class public		ldc_sub_ovf_i2 {

//---- GLOBAL DATA ----------


//---- METHODS --------------


.method public static int32 i2_0(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0xFFFF8000
		ldc.i4		0xFFFF8000
	sub
	conv.ovf.i2
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
.method public static int32 i2_1(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0xFFFF8000
		ldc.i4		0xFFFFFFFF
	sub
	conv.ovf.i2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFF8001
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFF8001
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_2(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0xFFFF8000
		ldc.i4		0x00000000
	sub
	conv.ovf.i2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFF8000
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFF8000
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_3(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0xFFFF8000
		ldc.i4		0x00000001
	sub
	conv.ovf.i2
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
.method public static int32 i2_4(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0xFFFF8000
		ldc.i4		0x00007FFF
	sub
	conv.ovf.i2
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
.method public static int32 i2_5(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0xFFFF8000
		ldc.i4		0x00005555
	sub
	conv.ovf.i2
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
.method public static int32 i2_6(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0xFFFF8000
		ldc.i4		0xFFFFAAAA
	sub
	conv.ovf.i2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFD556
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFD556
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_7(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0xFFFF8000
	sub
	conv.ovf.i2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00007FFF
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00007FFF
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_8(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0xFFFFFFFF
	sub
	conv.ovf.i2
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
.method public static int32 i2_9(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0x00000000
	sub
	conv.ovf.i2
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
.method public static int32 i2_10(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0x00000001
	sub
	conv.ovf.i2
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
.method public static int32 i2_11(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0x00007FFF
	sub
	conv.ovf.i2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFF8000
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFF8000
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_12(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0x00005555
	sub
	conv.ovf.i2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFAAAA
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFAAAA
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_13(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0xFFFFAAAA
	sub
	conv.ovf.i2
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_14(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0xFFFF8000
	sub
	conv.ovf.i2
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
.method public static int32 i2_15(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0xFFFFFFFF
	sub
	conv.ovf.i2
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
.method public static int32 i2_16(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x00000000
	sub
	conv.ovf.i2
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
.method public static int32 i2_17(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x00000001
	sub
	conv.ovf.i2
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
.method public static int32 i2_18(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x00007FFF
	sub
	conv.ovf.i2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFF8001
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFF8001
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_19(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x00005555
	sub
	conv.ovf.i2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFAAAB
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFAAAB
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_20(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0xFFFFAAAA
	sub
	conv.ovf.i2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00005556
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00005556
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_21(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0xFFFF8000
	sub
	conv.ovf.i2
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
.method public static int32 i2_22(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0xFFFFFFFF
	sub
	conv.ovf.i2
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
.method public static int32 i2_23(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x00000000
	sub
	conv.ovf.i2
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
.method public static int32 i2_24(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x00000001
	sub
	conv.ovf.i2
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
.method public static int32 i2_25(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x00007FFF
	sub
	conv.ovf.i2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFF8002
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFF8002
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_26(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x00005555
	sub
	conv.ovf.i2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFAAAC
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFAAAC
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_27(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0xFFFFAAAA
	sub
	conv.ovf.i2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00005557
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00005557
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_28(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00007FFF
		ldc.i4		0xFFFF8000
	sub
	conv.ovf.i2
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
.method public static int32 i2_29(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00007FFF
		ldc.i4		0xFFFFFFFF
	sub
	conv.ovf.i2
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
.method public static int32 i2_30(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00007FFF
		ldc.i4		0x00000000
	sub
	conv.ovf.i2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00007FFF
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00007FFF
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_31(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00007FFF
		ldc.i4		0x00000001
	sub
	conv.ovf.i2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00007FFE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00007FFE
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_32(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00007FFF
		ldc.i4		0x00007FFF
	sub
	conv.ovf.i2
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
.method public static int32 i2_33(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00007FFF
		ldc.i4		0x00005555
	sub
	conv.ovf.i2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00002AAA
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00002AAA
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_34(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00007FFF
		ldc.i4		0xFFFFAAAA
	sub
	conv.ovf.i2
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
.method public static int32 i2_35(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00005555
		ldc.i4		0xFFFF8000
	sub
	conv.ovf.i2
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
.method public static int32 i2_36(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00005555
		ldc.i4		0xFFFFFFFF
	sub
	conv.ovf.i2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00005556
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00005556
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_37(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00005555
		ldc.i4		0x00000000
	sub
	conv.ovf.i2
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_38(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00005555
		ldc.i4		0x00000001
	sub
	conv.ovf.i2
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
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_39(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00005555
		ldc.i4		0x00007FFF
	sub
	conv.ovf.i2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFD556
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFD556
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_40(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00005555
		ldc.i4		0x00005555
	sub
	conv.ovf.i2
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
.method public static int32 i2_41(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0x00005555
		ldc.i4		0xFFFFAAAA
	sub
	conv.ovf.i2
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
.method public static int32 i2_42(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0xFFFFAAAA
		ldc.i4		0xFFFF8000
	sub
	conv.ovf.i2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x00002AAA
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x00002AAA
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_43(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0xFFFFAAAA
		ldc.i4		0xFFFFFFFF
	sub
	conv.ovf.i2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFAAAB
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFAAAB
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_44(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0xFFFFAAAA
		ldc.i4		0x00000000
	sub
	conv.ovf.i2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFAAAA
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFAAAA
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_45(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0xFFFFAAAA
		ldc.i4		0x00000001
	sub
	conv.ovf.i2
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFAAA9
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFAAA9
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i2_46(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0xFFFFAAAA
		ldc.i4		0x00007FFF
	sub
	conv.ovf.i2
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
.method public static int32 i2_47(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0xFFFFAAAA
		ldc.i4		0x00005555
	sub
	conv.ovf.i2
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
.method public static int32 i2_48(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int16)
.maxstack	2
try_start:
		ldc.i4		0xFFFFAAAA
		ldc.i4		0xFFFFAAAA
	sub
	conv.ovf.i2
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
.method public		void ldc_sub_ovf_i2() {
.maxstack	0
	ret
}

//---- MAIN -----------------
.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
//====== begin testing ======
// -- Min - Min should overflow
	ldc.i4		0xFFFF8000
	ldc.i4		0xFFFF8000
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_i2::i2_0(int32,int32,int32)
	brfalse		FAIL
// -- Min - -1
	ldc.i4		0xFFFF8000
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFF8001
	call	int32 ldc_sub_ovf_i2::i2_1(int32,int32,int32)
	brfalse		FAIL
// -- Min - 0
	ldc.i4		0xFFFF8000
	ldc.i4		0x00000000
	ldc.i4		0xFFFF8000
	call	int32 ldc_sub_ovf_i2::i2_2(int32,int32,int32)
	brfalse		FAIL
// -- Min - 1
	ldc.i4		0xFFFF8000
	ldc.i4		0x00000001
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i2::i2_3(int32,int32,int32)
	brfalse		FAIL
// -- Min - Max
	ldc.i4		0xFFFF8000
	ldc.i4		0x00007FFF
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i2::i2_4(int32,int32,int32)
	brfalse		FAIL
// -- Min - Odd
	ldc.i4		0xFFFF8000
	ldc.i4		0x00005555
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i2::i2_5(int32,int32,int32)
	brfalse		FAIL
// -- Min - Even
	ldc.i4		0xFFFF8000
	ldc.i4		0xFFFFAAAA
	ldc.i4		0xFFFFD556
	call	int32 ldc_sub_ovf_i2::i2_6(int32,int32,int32)
	brfalse		FAIL

//----------------------------------------------------------
// -- -1 - Min
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFF8000
	ldc.i4		0x00007FFF
	call	int32 ldc_sub_ovf_i2::i2_7(int32,int32,int32)
	brfalse		FAIL
// -- -1 - -1
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_i2::i2_8(int32,int32,int32)
	brfalse		FAIL
// -- -1 - 0
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFFF
	call	int32 ldc_sub_ovf_i2::i2_9(int32,int32,int32)
	brfalse		FAIL
// -- -1 - 1
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFE
	call	int32 ldc_sub_ovf_i2::i2_10(int32,int32,int32)
	brfalse		FAIL
// -- -1 - Max
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00007FFF
	ldc.i4		0xFFFF8000
	call	int32 ldc_sub_ovf_i2::i2_11(int32,int32,int32)
	brfalse		FAIL
// -- -1 - Odd
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00005555
	ldc.i4		0xFFFFAAAA
	call	int32 ldc_sub_ovf_i2::i2_12(int32,int32,int32)
	brfalse		FAIL
// -- -1 - Even
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFAAAA
	ldc.i4		0x00005555
	call	int32 ldc_sub_ovf_i2::i2_13(int32,int32,int32)
	brfalse		FAIL

//----------------------------------------------------------
// -- 0 - Min
	ldc.i4		0x00000000
	ldc.i4		0xFFFF8000
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i2::i2_14(int32,int32,int32)
	brfalse		FAIL
// -- 0 - -1
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000001
	call	int32 ldc_sub_ovf_i2::i2_15(int32,int32,int32)
	brfalse		FAIL
// -- 0 - 0
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_i2::i2_16(int32,int32,int32)
	brfalse		FAIL
// -- 0 - 1
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFF
	call	int32 ldc_sub_ovf_i2::i2_17(int32,int32,int32)
	brfalse		FAIL
// -- 0 - Max
	ldc.i4		0x00000000
	ldc.i4		0x00007FFF
	ldc.i4		0xFFFF8001
	call	int32 ldc_sub_ovf_i2::i2_18(int32,int32,int32)
	brfalse		FAIL
// -- 0 - Odd
	ldc.i4		0x00000000
	ldc.i4		0x00005555
	ldc.i4		0xFFFFAAAB
	call	int32 ldc_sub_ovf_i2::i2_19(int32,int32,int32)
	brfalse		FAIL
// -- 0 - Even
	ldc.i4		0x00000000
	ldc.i4		0xFFFFAAAA
	ldc.i4		0x00005556
	call	int32 ldc_sub_ovf_i2::i2_20(int32,int32,int32)
	brfalse		FAIL

//----------------------------------------------------------
// -- 1 - Min
	ldc.i4		0x00000001
	ldc.i4		0xFFFF8000
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i2::i2_21(int32,int32,int32)
	brfalse		FAIL
// -- 1 - -1
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000002
	call	int32 ldc_sub_ovf_i2::i2_22(int32,int32,int32)
	brfalse		FAIL
// -- 1 - 0
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	call	int32 ldc_sub_ovf_i2::i2_23(int32,int32,int32)
	brfalse		FAIL
// -- 1 - 1
	ldc.i4		0x00000001
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_i2::i2_24(int32,int32,int32)
	brfalse		FAIL
// -- 1 - Max
	ldc.i4		0x00000001
	ldc.i4		0x00007FFF
	ldc.i4		0xFFFF8002
	call	int32 ldc_sub_ovf_i2::i2_25(int32,int32,int32)
	brfalse		FAIL
// -- 1 - Odd
	ldc.i4		0x00000001
	ldc.i4		0x00005555
	ldc.i4		0xFFFFAAAC
	call	int32 ldc_sub_ovf_i2::i2_26(int32,int32,int32)
	brfalse		FAIL
// -- 1 - Even
	ldc.i4		0x00000001
	ldc.i4		0xFFFFAAAA
	ldc.i4		0x00005557
	call	int32 ldc_sub_ovf_i2::i2_27(int32,int32,int32)
	brfalse		FAIL

//----------------------------------------------------------
// -- Max - Min
	ldc.i4		0x00007FFF
	ldc.i4		0xFFFF8000
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i2::i2_28(int32,int32,int32)
	brfalse		FAIL
// -- Max - -1
	ldc.i4		0x00007FFF
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i2::i2_29(int32,int32,int32)
	brfalse		FAIL
// -- Max - 0
	ldc.i4		0x00007FFF
	ldc.i4		0x00000000
	ldc.i4		0x00007FFF
	call	int32 ldc_sub_ovf_i2::i2_30(int32,int32,int32)
	brfalse		FAIL
// -- Max - 1
	ldc.i4		0x00007FFF
	ldc.i4		0x00000001
	ldc.i4		0x00007FFE
	call	int32 ldc_sub_ovf_i2::i2_31(int32,int32,int32)
	brfalse		FAIL
// -- Max - Max
	ldc.i4		0x00007FFF
	ldc.i4		0x00007FFF
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_i2::i2_32(int32,int32,int32)
	brfalse		FAIL
// -- Max - Odd
	ldc.i4		0x00007FFF
	ldc.i4		0x00005555
	ldc.i4		0x00002AAA
	call	int32 ldc_sub_ovf_i2::i2_33(int32,int32,int32)
	brfalse		FAIL
// -- Max - Even
	ldc.i4		0x00007FFF
	ldc.i4		0xFFFFAAAA
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i2::i2_34(int32,int32,int32)
	brfalse		FAIL
//----------------------------------------------------------
// -- Odd - Min
	ldc.i4		0x00005555
	ldc.i4		0xFFFF8000
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i2::i2_35(int32,int32,int32)
	brfalse		FAIL
// -- Odd - -1
	ldc.i4		0x00005555
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00005556
	call	int32 ldc_sub_ovf_i2::i2_36(int32,int32,int32)
	brfalse		FAIL
// -- Odd - 0
	ldc.i4		0x00005555
	ldc.i4		0x00000000
	ldc.i4		0x00005555
	call	int32 ldc_sub_ovf_i2::i2_37(int32,int32,int32)
	brfalse		FAIL
// -- Odd - 1
	ldc.i4		0x00005555
	ldc.i4		0x00000001
	ldc.i4		0x00005554
	call	int32 ldc_sub_ovf_i2::i2_38(int32,int32,int32)
	brfalse		FAIL
// -- Odd - Max
	ldc.i4		0x00005555
	ldc.i4		0x00007FFF
	ldc.i4		0xFFFFD556
	call	int32 ldc_sub_ovf_i2::i2_39(int32,int32,int32)
	brfalse		FAIL
// -- Odd - Odd
	ldc.i4		0x00005555
	ldc.i4		0x00005555
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_i2::i2_40(int32,int32,int32)
	brfalse		FAIL
// -- Odd - Even
	ldc.i4		0x00005555
	ldc.i4		0xFFFFAAAA
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i2::i2_41(int32,int32,int32)
	brfalse		FAIL

//----------------------------------------------------------
// -- Even - Min
	ldc.i4		0xFFFFAAAA
	ldc.i4		0xFFFF8000
	ldc.i4		0x00002AAA
	call	int32 ldc_sub_ovf_i2::i2_42(int32,int32,int32)
	brfalse		FAIL
// -- Even - -1
	ldc.i4		0xFFFFAAAA
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFAAAB
	call	int32 ldc_sub_ovf_i2::i2_43(int32,int32,int32)
	brfalse		FAIL
// -- Even - 0
	ldc.i4		0xFFFFAAAA
	ldc.i4		0x00000000
	ldc.i4		0xFFFFAAAA
	call	int32 ldc_sub_ovf_i2::i2_44(int32,int32,int32)
	brfalse		FAIL
// -- Even - 1
	ldc.i4		0xFFFFAAAA
	ldc.i4		0x00000001
	ldc.i4		0xFFFFAAA9
	call	int32 ldc_sub_ovf_i2::i2_45(int32,int32,int32)
	brfalse		FAIL
// -- Even - Max
	ldc.i4		0xFFFFAAAA
	ldc.i4		0x00007FFF
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i2::i2_46(int32,int32,int32)
	brfalse		FAIL
// -- Even - Odd
	ldc.i4		0xFFFFAAAA
	ldc.i4		0x00005555
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_i2::i2_47(int32,int32,int32)
	brfalse		FAIL
// -- Even - Even
	ldc.i4		0xFFFFAAAA
	ldc.i4		0xFFFFAAAA
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_i2::i2_48(int32,int32,int32)
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

.assembly ldc_sub_ovf_i2{}

