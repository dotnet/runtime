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
.class public		ldc_sub_ovf_u4 {

//---- GLOBAL DATA ----------


//---- METHODS --------------


.method public static int32 u4_0(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x00000000
	sub.ovf.un
	//conv.ovf.u4
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
.method public static int32 u4_1(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x00000001
	sub.ovf.un
	//conv.ovf.u4
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
.method public static int32 u4_2(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0xFFFFFFFE
	sub.ovf.un
	//conv.ovf.u4
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
.method public static int32 u4_3(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0xFFFFFFFF
	sub.ovf.un
	//conv.ovf.u4
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
.method public static int32 u4_4(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0x55555555
	sub.ovf.un
	//conv.ovf.u4
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
.method public static int32 u4_5(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0x00000000
		ldc.i4		0xAAAAAAAA
	sub.ovf.un
	//conv.ovf.u4
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
.method public static int32 u4_6(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x00000000
	sub.ovf.un
	//conv.ovf.u4
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
.method public static int32 u4_7(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x00000001
	sub.ovf.un
	//conv.ovf.u4
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
.method public static int32 u4_8(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0xFFFFFFFE
	sub.ovf.un
	//conv.ovf.u4
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
.method public static int32 u4_9(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0xFFFFFFFF
	sub.ovf.un
	//conv.ovf.u4
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
.method public static int32 u4_10(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0x55555555
	sub.ovf.un
	//conv.ovf.u4
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
.method public static int32 u4_11(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0x00000001
		ldc.i4		0xAAAAAAAA
	sub.ovf.un
	//conv.ovf.u4
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
.method public static int32 u4_12(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFE
		ldc.i4		0x00000000
	sub.ovf.un
	//conv.ovf.u4
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
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u4_13(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFE
		ldc.i4		0x00000001
	sub.ovf.un
	//conv.ovf.u4
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xFFFFFFFD
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xFFFFFFFD
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u4_14(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFE
		ldc.i4		0xFFFFFFFE
	sub.ovf.un
	//conv.ovf.u4
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
.method public static int32 u4_15(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFE
		ldc.i4		0xFFFFFFFF
	sub.ovf.un
	//conv.ovf.u4
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
.method public static int32 u4_16(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFE
		ldc.i4		0x55555555
	sub.ovf.un
	//conv.ovf.u4
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xAAAAAAA9
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xAAAAAAA9
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u4_17(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFE
		ldc.i4		0xAAAAAAAA
	sub.ovf.un
	//conv.ovf.u4
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x55555554
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x55555554
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u4_18(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0x00000000
	sub.ovf.un
	//conv.ovf.u4
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
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u4_19(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0x00000001
	sub.ovf.un
	//conv.ovf.u4
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
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u4_20(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0xFFFFFFFE
	sub.ovf.un
	//conv.ovf.u4
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
.method public static int32 u4_21(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0xFFFFFFFF
	sub.ovf.un
	//conv.ovf.u4
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
.method public static int32 u4_22(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0x55555555
	sub.ovf.un
	//conv.ovf.u4
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xAAAAAAAA
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xAAAAAAAA
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u4_23(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0xFFFFFFFF
		ldc.i4		0xAAAAAAAA
	sub.ovf.un
	//conv.ovf.u4
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x55555555
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x55555555
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u4_24(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0x55555555
		ldc.i4		0x00000000
	sub.ovf.un
	//conv.ovf.u4
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x55555555
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x55555555
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u4_25(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0x55555555
		ldc.i4		0x00000001
	sub.ovf.un
	//conv.ovf.u4
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x55555554
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x55555554
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u4_26(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0x55555555
		ldc.i4		0xFFFFFFFE
	sub.ovf.un
	//conv.ovf.u4
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
.method public static int32 u4_27(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0x55555555
		ldc.i4		0xFFFFFFFF
	sub.ovf.un
	//conv.ovf.u4
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
.method public static int32 u4_28(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0x55555555
		ldc.i4		0x55555555
	sub.ovf.un
	//conv.ovf.u4
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
.method public static int32 u4_29(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0x55555555
		ldc.i4		0xAAAAAAAA
	sub.ovf.un
	//conv.ovf.u4
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
.method public static int32 u4_30(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0xAAAAAAAA
		ldc.i4		0x00000000
	sub.ovf.un
	//conv.ovf.u4
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xAAAAAAAA
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xAAAAAAAA
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u4_31(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0xAAAAAAAA
		ldc.i4		0x00000001
	sub.ovf.un
	//conv.ovf.u4
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0xAAAAAAA9
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0xAAAAAAA9
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u4_32(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0xAAAAAAAA
		ldc.i4		0xFFFFFFFE
	sub.ovf.un
	//conv.ovf.u4
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
.method public static int32 u4_33(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0xAAAAAAAA
		ldc.i4		0xFFFFFFFF
	sub.ovf.un
	//conv.ovf.u4
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
.method public static int32 u4_34(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0xAAAAAAAA
		ldc.i4		0x55555555
	sub.ovf.un
	//conv.ovf.u4
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i4		0x55555555
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i4		0x55555555
	ldc.i4		0xEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4	0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 u4_35(int32,int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int32)
.maxstack	2
try_start:
		ldc.i4		0xAAAAAAAA
		ldc.i4		0xAAAAAAAA
	sub.ovf.un
	//conv.ovf.u4
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
.method public		void ldc_sub_ovf_u4() {
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
	call	int32 ldc_sub_ovf_u4::u4_0(int32,int32,int32)
	brfalse		FAIL
// -- 0 - 1
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u4::u4_1(int32,int32,int32)
	brfalse		FAIL
// -- 0 - (Max - 1)
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFFE
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u4::u4_2(int32,int32,int32)
	brfalse		FAIL
// -- 0 - Max
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u4::u4_3(int32,int32,int32)
	brfalse		FAIL
// -- 0 - Odd
	ldc.i4		0x00000000
	ldc.i4		0x55555555
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u4::u4_4(int32,int32,int32)
	brfalse		FAIL
// -- 0 - Even
	ldc.i4		0x00000000
	ldc.i4		0xAAAAAAAA
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u4::u4_5(int32,int32,int32)
	brfalse		FAIL

//------------------------------------------------
// -- 1 - 0
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	call	int32 ldc_sub_ovf_u4::u4_6(int32,int32,int32)
	brfalse		FAIL
// -- 1 - 1
	ldc.i4		0x00000001
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_u4::u4_7(int32,int32,int32)
	brfalse		FAIL
// -- 1 - (Max - 1)
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFE
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u4::u4_8(int32,int32,int32)
	brfalse		FAIL
// -- 1 - Max
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u4::u4_9(int32,int32,int32)
	brfalse		FAIL
// -- 1 - Odd
	ldc.i4		0x00000001
	ldc.i4		0x55555555
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u4::u4_10(int32,int32,int32)
	brfalse		FAIL
// -- 1 - Even
	ldc.i4		0x00000001
	ldc.i4		0xAAAAAAAA
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u4::u4_11(int32,int32,int32)
	brfalse		FAIL

//------------------------------------------------
// -- (Max - 1) - 0
	ldc.i4		0xFFFFFFFE
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFFE
	call	int32 ldc_sub_ovf_u4::u4_12(int32,int32,int32)
	brfalse		FAIL
// -- (Max - 1) - 1
	ldc.i4		0xFFFFFFFE
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFD
	call	int32 ldc_sub_ovf_u4::u4_13(int32,int32,int32)
	brfalse		FAIL
// -- (Max - 1) - (Max - 1)
	ldc.i4		0xFFFFFFFE
	ldc.i4		0xFFFFFFFE
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_u4::u4_14(int32,int32,int32)
	brfalse		FAIL
// -- (Max - 1) - Max
	ldc.i4		0xFFFFFFFE
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u4::u4_15(int32,int32,int32)
	brfalse		FAIL
// -- (Max - 1) - Odd
	ldc.i4		0xFFFFFFFE
	ldc.i4		0x55555555
	ldc.i4		0xAAAAAAA9
	call	int32 ldc_sub_ovf_u4::u4_16(int32,int32,int32)
	brfalse		FAIL
// -- (Max - 1) - Even
	ldc.i4		0xFFFFFFFE
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x55555554
	call	int32 ldc_sub_ovf_u4::u4_17(int32,int32,int32)
	brfalse		FAIL
//------------------------------------------------

// -- Max - 0
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFFF
	call	int32 ldc_sub_ovf_u4::u4_18(int32,int32,int32)
	brfalse		FAIL
// -- Max - 1
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFE
	call	int32 ldc_sub_ovf_u4::u4_19(int32,int32,int32)
	brfalse		FAIL
// -- Max - (Max - 1)
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFFE
	ldc.i4		0x00000001
	call	int32 ldc_sub_ovf_u4::u4_20(int32,int32,int32)
	brfalse		FAIL
// -- Max - Max
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_u4::u4_21(int32,int32,int32)
	brfalse		FAIL
// -- Max - Odd
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x55555555
	ldc.i4		0xAAAAAAAA
	call	int32 ldc_sub_ovf_u4::u4_22(int32,int32,int32)
	brfalse		FAIL
// -- Max - Even
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x55555555
	call	int32 ldc_sub_ovf_u4::u4_23(int32,int32,int32)
	brfalse		FAIL
//------------------------------------------------
// -- Odd - 0
	ldc.i4		0x55555555
	ldc.i4		0x00000000
	ldc.i4		0x55555555
	call	int32 ldc_sub_ovf_u4::u4_24(int32,int32,int32)
	brfalse		FAIL
// -- Odd - 1
	ldc.i4		0x55555555
	ldc.i4		0x00000001
	ldc.i4		0x55555554
	call	int32 ldc_sub_ovf_u4::u4_25(int32,int32,int32)
	brfalse		FAIL
// -- Odd - (Max - 1)
	ldc.i4		0x55555555
	ldc.i4		0xFFFFFFFE
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u4::u4_26(int32,int32,int32)
	brfalse		FAIL
// -- Odd - Max
	ldc.i4		0x55555555
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u4::u4_27(int32,int32,int32)
	brfalse		FAIL
// -- Odd - Odd
	ldc.i4		0x55555555
	ldc.i4		0x55555555
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_u4::u4_28(int32,int32,int32)
	brfalse		FAIL
// -- Odd - Even
	ldc.i4		0x55555555
	ldc.i4		0xAAAAAAAA
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u4::u4_29(int32,int32,int32)
	brfalse		FAIL
//------------------------------------------------
// -- Even - 0
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x00000000
	ldc.i4		0xAAAAAAAA
	call	int32 ldc_sub_ovf_u4::u4_30(int32,int32,int32)
	brfalse		FAIL
// -- Even - 1
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x00000001
	ldc.i4		0xAAAAAAA9
	call	int32 ldc_sub_ovf_u4::u4_31(int32,int32,int32)
	brfalse		FAIL
// -- Even - (Max - 1)
	ldc.i4		0xAAAAAAAA
	ldc.i4		0xFFFFFFFE
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u4::u4_32(int32,int32,int32)
	brfalse		FAIL
// -- Even - Max
	ldc.i4		0xAAAAAAAA
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xEEEEEEEE
	call	int32 ldc_sub_ovf_u4::u4_33(int32,int32,int32)
	brfalse		FAIL
// -- Even - Odd
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x55555555
	ldc.i4		0x55555555
	call	int32 ldc_sub_ovf_u4::u4_34(int32,int32,int32)
	brfalse		FAIL
// -- Even - Even
	ldc.i4		0xAAAAAAAA
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x00000000
	call	int32 ldc_sub_ovf_u4::u4_35(int32,int32,int32)
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

.assembly ldc_sub_ovf_u4{}

