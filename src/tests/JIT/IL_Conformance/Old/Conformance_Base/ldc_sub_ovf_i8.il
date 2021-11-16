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
.class public		ldc_sub_ovf_i8 {

//---- GLOBAL DATA ----------


//---- METHODS --------------


.method public static int32 i8_0(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x8000000000000000
		ldc.i8		0x8000000000000000
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x0000000000000000
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x0000000000000000
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_1(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x8000000000000000
		ldc.i8		0xFFFFFFFFFFFFFFFF
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x8000000000000001
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x8000000000000001
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_2(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x8000000000000000
		ldc.i8		0x0000000000000000
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x8000000000000000
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x8000000000000000
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_3(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x8000000000000000
		ldc.i8		0x0000000000000001
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_4(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x8000000000000000
		ldc.i8		0x7FFFFFFFFFFFFFFF
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_5(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x8000000000000000
		ldc.i8		0x5555555555555555
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_6(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x8000000000000000
		ldc.i8		0xAAAAAAAAAAAAAAAA
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0xD555555555555556
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0xD555555555555556
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_7(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0xFFFFFFFFFFFFFFFF
		ldc.i8		0x8000000000000000
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x7FFFFFFFFFFFFFFF
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x7FFFFFFFFFFFFFFF
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_8(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0xFFFFFFFFFFFFFFFF
		ldc.i8		0xFFFFFFFFFFFFFFFF
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x0000000000000000
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x0000000000000000
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_9(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0xFFFFFFFFFFFFFFFF
		ldc.i8		0x0000000000000000
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0xFFFFFFFFFFFFFFFF
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_10(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0xFFFFFFFFFFFFFFFF
		ldc.i8		0x0000000000000001
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0xFFFFFFFFFFFFFFFE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0xFFFFFFFFFFFFFFFE
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_11(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0xFFFFFFFFFFFFFFFF
		ldc.i8		0x7FFFFFFFFFFFFFFF
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x8000000000000000
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x8000000000000000
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_12(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0xFFFFFFFFFFFFFFFF
		ldc.i8		0x5555555555555555
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0xAAAAAAAAAAAAAAAA
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_13(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0xFFFFFFFFFFFFFFFF
		ldc.i8		0xAAAAAAAAAAAAAAAA
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x5555555555555555
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x5555555555555555
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_14(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x0000000000000000
		ldc.i8		0x8000000000000000
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_15(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x0000000000000000
		ldc.i8		0xFFFFFFFFFFFFFFFF
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x0000000000000001
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x0000000000000001
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_16(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x0000000000000000
		ldc.i8		0x0000000000000000
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x0000000000000000
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x0000000000000000
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_17(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x0000000000000000
		ldc.i8		0x0000000000000001
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0xFFFFFFFFFFFFFFFF
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_18(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x0000000000000000
		ldc.i8		0x7FFFFFFFFFFFFFFF
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x8000000000000001
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x8000000000000001
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_19(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x0000000000000000
		ldc.i8		0x5555555555555555
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0xAAAAAAAAAAAAAAAB
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0xAAAAAAAAAAAAAAAB
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_20(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x0000000000000000
		ldc.i8		0xAAAAAAAAAAAAAAAA
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x5555555555555556
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x5555555555555556
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_21(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x0000000000000001
		ldc.i8		0x8000000000000000
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_22(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x0000000000000001
		ldc.i8		0xFFFFFFFFFFFFFFFF
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x0000000000000002
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x0000000000000002
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_23(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x0000000000000001
		ldc.i8		0x0000000000000000
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x0000000000000001
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x0000000000000001
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_24(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x0000000000000001
		ldc.i8		0x0000000000000001
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x0000000000000000
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x0000000000000000
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_25(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x0000000000000001
		ldc.i8		0x7FFFFFFFFFFFFFFF
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x8000000000000002
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x8000000000000002
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_26(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x0000000000000001
		ldc.i8		0x5555555555555555
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0xAAAAAAAAAAAAAAAC
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0xAAAAAAAAAAAAAAAC
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_27(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x0000000000000001
		ldc.i8		0xAAAAAAAAAAAAAAAA
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x5555555555555557
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x5555555555555557
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_28(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x7FFFFFFFFFFFFFFF
		ldc.i8		0x8000000000000000
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_29(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x7FFFFFFFFFFFFFFF
		ldc.i8		0xFFFFFFFFFFFFFFFF
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_30(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x7FFFFFFFFFFFFFFF
		ldc.i8		0x0000000000000000
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x7FFFFFFFFFFFFFFF
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x7FFFFFFFFFFFFFFF
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_31(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x7FFFFFFFFFFFFFFF
		ldc.i8		0x0000000000000001
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x7FFFFFFFFFFFFFFE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x7FFFFFFFFFFFFFFE
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_32(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x7FFFFFFFFFFFFFFF
		ldc.i8		0x7FFFFFFFFFFFFFFF
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x0000000000000000
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x0000000000000000
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_33(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x7FFFFFFFFFFFFFFF
		ldc.i8		0x5555555555555555
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x2AAAAAAAAAAAAAAA
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x2AAAAAAAAAAAAAAA
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_34(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x7FFFFFFFFFFFFFFF
		ldc.i8		0xAAAAAAAAAAAAAAAA
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_35(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x5555555555555555
		ldc.i8		0x8000000000000000
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_36(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x5555555555555555
		ldc.i8		0xFFFFFFFFFFFFFFFF
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x5555555555555556
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x5555555555555556
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_37(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x5555555555555555
		ldc.i8		0x0000000000000000
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x5555555555555555
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x5555555555555555
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_38(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x5555555555555555
		ldc.i8		0x0000000000000001
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x5555555555555554
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x5555555555555554
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_39(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x5555555555555555
		ldc.i8		0x7FFFFFFFFFFFFFFF
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0xD555555555555556
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0xD555555555555556
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_40(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x5555555555555555
		ldc.i8		0x5555555555555555
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x0000000000000000
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x0000000000000000
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_41(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0x5555555555555555
		ldc.i8		0xAAAAAAAAAAAAAAAA
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_42(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0xAAAAAAAAAAAAAAAA
		ldc.i8		0x8000000000000000
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x2AAAAAAAAAAAAAAA
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x2AAAAAAAAAAAAAAA
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_43(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0xAAAAAAAAAAAAAAAA
		ldc.i8		0xFFFFFFFFFFFFFFFF
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0xAAAAAAAAAAAAAAAB
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0xAAAAAAAAAAAAAAAB
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_44(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0xAAAAAAAAAAAAAAAA
		ldc.i8		0x0000000000000000
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0xAAAAAAAAAAAAAAAA
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_45(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0xAAAAAAAAAAAAAAAA
		ldc.i8		0x0000000000000001
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0xAAAAAAAAAAAAAAA9
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0xAAAAAAAAAAAAAAA9
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_46(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0xAAAAAAAAAAAAAAAA
		ldc.i8		0x7FFFFFFFFFFFFFFF
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_47(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0xAAAAAAAAAAAAAAAA
		ldc.i8		0x5555555555555555
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0xEEEEEEEEEEEEEEEE
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
.method public static int32 i8_48(int64,int64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start:
		ldc.i8		0xAAAAAAAAAAAAAAAA
		ldc.i8		0xAAAAAAAAAAAAAAAA
	sub.ovf
	//conv.ovf.i8
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
		ldc.i8		0x0000000000000000
	ceq
	br			END
aHandler:
	isinst	[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
		ldc.i8		0x0000000000000000
	ldc.i8		0xEEEEEEEEEEEEEEEE
	ceq
	br			END
FAIL:
	ldc.i4		0x00000000
END:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler aHandler to HEnd

}
//---- CONSTRUCTOR ----------
.method public		void ldc_sub_ovf_i8() {
.maxstack	0
	ret
}

//---- MAIN -----------------
.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
//====== begin testing ======
// -- Min - Min should overflow
	ldc.i8		0x8000000000000000
	ldc.i8		0x8000000000000000
	ldc.i8		0x0000000000000000
	call	int32 ldc_sub_ovf_i8::i8_0(int64,int64,int64)
	brfalse		FAIL
// -- Min - -1
	ldc.i8		0x8000000000000000
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0x8000000000000001
	call	int32 ldc_sub_ovf_i8::i8_1(int64,int64,int64)
	brfalse		FAIL
// -- Min - 0
	ldc.i8		0x8000000000000000
	ldc.i8		0x0000000000000000
	ldc.i8		0x8000000000000000
	call	int32 ldc_sub_ovf_i8::i8_2(int64,int64,int64)
	brfalse		FAIL
// -- Min - 1
	ldc.i8		0x8000000000000000
	ldc.i8		0x0000000000000001
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 ldc_sub_ovf_i8::i8_3(int64,int64,int64)
	brfalse		FAIL
// -- Min - Max
	ldc.i8		0x8000000000000000
	ldc.i8		0x7FFFFFFFFFFFFFFF
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 ldc_sub_ovf_i8::i8_4(int64,int64,int64)
	brfalse		FAIL
// -- Min - Odd
	ldc.i8		0x8000000000000000
	ldc.i8		0x5555555555555555
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 ldc_sub_ovf_i8::i8_5(int64,int64,int64)
	brfalse		FAIL
// -- Min - Even
	ldc.i8		0x8000000000000000
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0xD555555555555556
	call	int32 ldc_sub_ovf_i8::i8_6(int64,int64,int64)
	brfalse		FAIL
//----------------------------------------------------------
// -- -1 - Min
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0x8000000000000000
	ldc.i8		0x7FFFFFFFFFFFFFFF
	call	int32 ldc_sub_ovf_i8::i8_7(int64,int64,int64)
	brfalse		FAIL
// -- -1 - -1
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0x0000000000000000
	call	int32 ldc_sub_ovf_i8::i8_8(int64,int64,int64)
	brfalse		FAIL
// -- -1 - 0
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0x0000000000000000
	ldc.i8		0xFFFFFFFFFFFFFFFF
	call	int32 ldc_sub_ovf_i8::i8_9(int64,int64,int64)
	brfalse		FAIL
// -- -1 - 1
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0x0000000000000001
	ldc.i8		0xFFFFFFFFFFFFFFFE
	call	int32 ldc_sub_ovf_i8::i8_10(int64,int64,int64)
	brfalse		FAIL
// -- -1 - Max
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0x7FFFFFFFFFFFFFFF
	ldc.i8		0x8000000000000000
	call	int32 ldc_sub_ovf_i8::i8_11(int64,int64,int64)
	brfalse		FAIL
// -- -1 - Odd
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0x5555555555555555
	ldc.i8		0xAAAAAAAAAAAAAAAA
	call	int32 ldc_sub_ovf_i8::i8_12(int64,int64,int64)
	brfalse		FAIL
// -- -1 - Even
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0x5555555555555555
	call	int32 ldc_sub_ovf_i8::i8_13(int64,int64,int64)
	brfalse		FAIL
//----------------------------------------------------------
// -- 0 - Min
	ldc.i8		0x0000000000000000
	ldc.i8		0x8000000000000000
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 ldc_sub_ovf_i8::i8_14(int64,int64,int64)
	brfalse		FAIL
// -- 0 - -1
	ldc.i8		0x0000000000000000
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0x0000000000000001
	call	int32 ldc_sub_ovf_i8::i8_15(int64,int64,int64)
	brfalse		FAIL
// -- 0 - 0
	ldc.i8		0x0000000000000000
	ldc.i8		0x0000000000000000
	ldc.i8		0x0000000000000000
	call	int32 ldc_sub_ovf_i8::i8_16(int64,int64,int64)
	brfalse		FAIL
// -- 0 - 1
	ldc.i8		0x0000000000000000
	ldc.i8		0x0000000000000001
	ldc.i8		0xFFFFFFFFFFFFFFFF
	call	int32 ldc_sub_ovf_i8::i8_17(int64,int64,int64)
	brfalse		FAIL
// -- 0 - Max
	ldc.i8		0x0000000000000000
	ldc.i8		0x7FFFFFFFFFFFFFFF
	ldc.i8		0x8000000000000001
	call	int32 ldc_sub_ovf_i8::i8_18(int64,int64,int64)
	brfalse		FAIL
// -- 0 - Odd
	ldc.i8		0x0000000000000000
	ldc.i8		0x5555555555555555
	ldc.i8		0xAAAAAAAAAAAAAAAB
	call	int32 ldc_sub_ovf_i8::i8_19(int64,int64,int64)
	brfalse		FAIL
// -- 0 - Even
	ldc.i8		0x0000000000000000
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0x5555555555555556
	call	int32 ldc_sub_ovf_i8::i8_20(int64,int64,int64)
	brfalse		FAIL

//----------------------------------------------------------
// -- 1 - Min
	ldc.i8		0x0000000000000001
	ldc.i8		0x8000000000000000
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 ldc_sub_ovf_i8::i8_21(int64,int64,int64)
	brfalse		FAIL
// -- 1 - -1
	ldc.i8		0x0000000000000001
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0x0000000000000002
	call	int32 ldc_sub_ovf_i8::i8_22(int64,int64,int64)
	brfalse		FAIL
// -- 1 - 0
	ldc.i8		0x0000000000000001
	ldc.i8		0x0000000000000000
	ldc.i8		0x0000000000000001
	call	int32 ldc_sub_ovf_i8::i8_23(int64,int64,int64)
	brfalse		FAIL
// -- 1 - 1
	ldc.i8		0x0000000000000001
	ldc.i8		0x0000000000000001
	ldc.i8		0x0000000000000000
	call	int32 ldc_sub_ovf_i8::i8_24(int64,int64,int64)
	brfalse		FAIL
// -- 1 - Max
	ldc.i8		0x0000000000000001
	ldc.i8		0x7FFFFFFFFFFFFFFF
	ldc.i8		0x8000000000000002
	call	int32 ldc_sub_ovf_i8::i8_25(int64,int64,int64)
	brfalse		FAIL
// -- 1 - Odd
	ldc.i8		0x0000000000000001
	ldc.i8		0x5555555555555555
	ldc.i8		0xAAAAAAAAAAAAAAAC
	call	int32 ldc_sub_ovf_i8::i8_26(int64,int64,int64)
	brfalse		FAIL
// -- 1 - Even
	ldc.i8		0x0000000000000001
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0x5555555555555557
	call	int32 ldc_sub_ovf_i8::i8_27(int64,int64,int64)
	brfalse		FAIL

//----------------------------------------------------------
// -- Max - Min
	ldc.i8		0x7FFFFFFFFFFFFFFF
	ldc.i8		0x8000000000000000
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 ldc_sub_ovf_i8::i8_28(int64,int64,int64)
	brfalse		FAIL
// -- Max - -1
	ldc.i8		0x7FFFFFFFFFFFFFFF
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 ldc_sub_ovf_i8::i8_29(int64,int64,int64)
	brfalse		FAIL
// -- Max - 0
	ldc.i8		0x7FFFFFFFFFFFFFFF
	ldc.i8		0x0000000000000000
	ldc.i8		0x7FFFFFFFFFFFFFFF
	call	int32 ldc_sub_ovf_i8::i8_30(int64,int64,int64)
	brfalse		FAIL
// -- Max - 1
	ldc.i8		0x7FFFFFFFFFFFFFFF
	ldc.i8		0x0000000000000001
	ldc.i8		0x7FFFFFFFFFFFFFFE
	call	int32 ldc_sub_ovf_i8::i8_31(int64,int64,int64)
	brfalse		FAIL
// -- Max - Max
	ldc.i8		0x7FFFFFFFFFFFFFFF
	ldc.i8		0x7FFFFFFFFFFFFFFF
	ldc.i8		0x0000000000000000
	call	int32 ldc_sub_ovf_i8::i8_32(int64,int64,int64)
	brfalse		FAIL
// -- Max - Odd
	ldc.i8		0x7FFFFFFFFFFFFFFF
	ldc.i8		0x5555555555555555
	ldc.i8		0x2AAAAAAAAAAAAAAA
	call	int32 ldc_sub_ovf_i8::i8_33(int64,int64,int64)
	brfalse		FAIL
// -- Max - Even
	ldc.i8		0x7FFFFFFFFFFFFFFF
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 ldc_sub_ovf_i8::i8_34(int64,int64,int64)
	brfalse		FAIL

//----------------------------------------------------------
// -- Odd - Min
	ldc.i8		0x5555555555555555
	ldc.i8		0x8000000000000000
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 ldc_sub_ovf_i8::i8_35(int64,int64,int64)
	brfalse		FAIL
// -- Odd - -1
	ldc.i8		0x5555555555555555
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0x5555555555555556
	call	int32 ldc_sub_ovf_i8::i8_36(int64,int64,int64)
	brfalse		FAIL
// -- Odd - 0
	ldc.i8		0x5555555555555555
	ldc.i8		0x0000000000000000
	ldc.i8		0x5555555555555555
	call	int32 ldc_sub_ovf_i8::i8_37(int64,int64,int64)
	brfalse		FAIL
// -- Odd - 1
	ldc.i8		0x5555555555555555
	ldc.i8		0x0000000000000001
	ldc.i8		0x5555555555555554
	call	int32 ldc_sub_ovf_i8::i8_38(int64,int64,int64)
	brfalse		FAIL
// -- Odd - Max
	ldc.i8		0x5555555555555555
	ldc.i8		0x7FFFFFFFFFFFFFFF
	ldc.i8		0xD555555555555556
	call	int32 ldc_sub_ovf_i8::i8_39(int64,int64,int64)
	brfalse		FAIL
// -- Odd - Odd
	ldc.i8		0x5555555555555555
	ldc.i8		0x5555555555555555
	ldc.i8		0x0000000000000000
	call	int32 ldc_sub_ovf_i8::i8_40(int64,int64,int64)
	brfalse		FAIL
// -- Odd - Even
	ldc.i8		0x5555555555555555
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 ldc_sub_ovf_i8::i8_41(int64,int64,int64)
	brfalse		FAIL

//----------------------------------------------------------
// -- Even - Min
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0x8000000000000000
	ldc.i8		0x2AAAAAAAAAAAAAAA
	call	int32 ldc_sub_ovf_i8::i8_42(int64,int64,int64)
	brfalse		FAIL
// -- Even - -1
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0xAAAAAAAAAAAAAAAB
	call	int32 ldc_sub_ovf_i8::i8_43(int64,int64,int64)
	brfalse		FAIL
// -- Even - 0
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0x0000000000000000
	ldc.i8		0xAAAAAAAAAAAAAAAA
	call	int32 ldc_sub_ovf_i8::i8_44(int64,int64,int64)
	brfalse		FAIL
// -- Even - 1
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0x0000000000000001
	ldc.i8		0xAAAAAAAAAAAAAAA9
	call	int32 ldc_sub_ovf_i8::i8_45(int64,int64,int64)
	brfalse		FAIL
// -- Even - Max
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0x7FFFFFFFFFFFFFFF
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 ldc_sub_ovf_i8::i8_46(int64,int64,int64)
	brfalse		FAIL
// -- Even - Odd
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0x5555555555555555
	ldc.i8		0xEEEEEEEEEEEEEEEE
	call	int32 ldc_sub_ovf_i8::i8_47(int64,int64,int64)
	brfalse		FAIL
// -- Even - Even
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0x0000000000000000
	call	int32 ldc_sub_ovf_i8::i8_48(int64,int64,int64)
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

.assembly ldc_sub_ovf_i8{}

