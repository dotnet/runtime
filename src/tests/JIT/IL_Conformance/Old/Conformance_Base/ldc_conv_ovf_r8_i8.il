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
.class public		conv_ovf_i8 {

//---- GLOBAL DATA ----------


//---- METHODS --------------


.method public static		int32 conv_0(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_0(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_1(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_1(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_2(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_2(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_3(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_3(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_4(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_4(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_5(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_5(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_6(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_6(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_7(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_7(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_8(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_8(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_9(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_9(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_10(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_10(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_11(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_11(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_12(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_12(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_13(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_13(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_14(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_14(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_15(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_15(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_16(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_16(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_17(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_17(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_18(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_18(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_19(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_19(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_20(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_20(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_21(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_21(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_22(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_22(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_23(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_23(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_24(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_24(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
.method public static		int32 conv_25(float64,int64) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i8
	//- No exception
	ldarg		1
	ceq
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	brfalse			FAIL
	ldc.i4			0x11111111
	br				END
aHandler:
	//- Got an OverflowException
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
.method public static		int32 conv_un_25(float64,int64) {
.locals		(class [mscorlib]System.OverflowException,int64)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i8.un
	stloc.1
	leave.s	try_end2
try_end2:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
	brfalse			FAIL2
	ldc.i4			0x11111111
	br				END2
aHandler2:
	//- Got an OverflowException
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
//---- CONSTRUCTOR ----------
.method public		void conv_ovf_i8() {
.maxstack	1
	ret
}

//---- MAIN -----------------
.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
//====== begin testing ======

//-- -inf r8 --
	ldc.r8			float64(0xFFF0000000000000)
	ldc.i8			0x0000000000000000
	call		int32 conv_ovf_i8::conv_0(float64,int64)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL
//-- -min r8 --
	ldc.r8			float64(0xFFEFFFFFFFFFFFFF)
	ldc.i8			0x0000000000000000
	call		int32 conv_ovf_i8::conv_1(float64,int64)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL
//-- -1 r8 --
	ldc.r8			float64(0xBFF0000000000000)
	ldc.i8			0xFFFFFFFFFFFFFFFF
	call		int32 conv_ovf_i8::conv_2(float64,int64)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- -0 r8 --
	ldc.r8			float64(0x8000000000000000)
	ldc.i8			0x0000000000000000
	call		int32 conv_ovf_i8::conv_3(float64,int64)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- +0 r8 --
	ldc.r8			float64(0x0000000000000000)
	ldc.i8			0x0000000000000000
	call		int32 conv_ovf_i8::conv_4(float64,int64)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- +1 r8 --
	ldc.r8			float64(0x3FF0000000000000)
	ldc.i8			0x0000000000000001
	call		int32 conv_ovf_i8::conv_5(float64,int64)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- +max r8 --
	ldc.r8			float64(0x7FEFFFFFFFFFFFFF)
	ldc.i8			0x0000000000000000
	call		int32 conv_ovf_i8::conv_6(float64,int64)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL
//-- +inf r8 --
	ldc.r8			float64(0x7FF0000000000000)
	ldc.i8			0x0000000000000000
	call		int32 conv_ovf_i8::conv_7(float64,int64)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL
//-- NaN r8 --
	ldc.r8			float64(0x7FF8000000000000)
	ldc.i8			0x0000000000000000
	call		int32 conv_ovf_i8::conv_8(float64,int64)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL
//-- 1.5 r8 --
	ldc.r8			1.5
	ldc.i8			0x0000000000000001
	call		int32 conv_ovf_i8::conv_9(float64,int64)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- 999.99 r8 --
	ldc.r8			99.99
	ldc.i8			0x0000000000000063
	call		int32 conv_ovf_i8::conv_10(float64,int64)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL

//-- -min i8 --
	// the smallest i8 that can be represented as an r8 is 0x8000000000000400.
	// therefore the smallest r8 that can be converted to an i8 without
	// overflow is 0xC3DFFFFFFFFFFFFF.
	ldc.r8			float64(0xC3DFFFFFFFFFFFFF)
	ldc.i8			0x8000000000000400
	call		int32 conv_ovf_i8::conv_11(float64,int64)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- +max i8 --
	// the largest i8 that can be represented as an r8 is 0x2000000000000000.
	// therefore the largest r8 that can be converted to an i8 without
	// overflow is 0x43C0000000000000.
	ldc.r8			float64(0x43C0000000000000)
	ldc.i8			0x2000000000000000
	call		int32 conv_ovf_i8::conv_12(float64,int64)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL

//====== conv_un testing ======

//-- -inf r8 --
/*	ldc.r8			float64(0xFFF0000000000000)
	ldc.i8			0x0000000000000000
	call		int32 conv_ovf_i8::conv_un_13(float64,int64)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL
//-- -min r8 --
	ldc.r8			float64(0xFFEFFFFFFFFFFFFF)
	ldc.i8			0x0000000000000000
	call		int32 conv_ovf_i8::conv_un_14(float64,int64)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL
//-- -1 r8 --
	ldc.r8			float64(0xBFF0000000000000)
	ldc.i8			0xFFFFFFFFFFFFFFFF
	call		int32 conv_ovf_i8::conv_un_15(float64,int64)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- -0 r8 --
	ldc.r8			float64(0x8000000000000000)
	ldc.i8			0x0000000000000000
	call		int32 conv_ovf_i8::conv_un_16(float64,int64)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
*///-- +0 r8 --
	ldc.r8			float64(0x0000000000000000)
	ldc.i8			0x0000000000000000
	call		int32 conv_ovf_i8::conv_un_17(float64,int64)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- +1 r8 --
	ldc.r8			float64(0x3FF0000000000000)
	ldc.i8			0x0000000000000001
	call		int32 conv_ovf_i8::conv_un_18(float64,int64)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- +max r8 --
	ldc.r8			float64(0x7FEFFFFFFFFFFFFF)
	ldc.i8			0x0000000000000000
	call		int32 conv_ovf_i8::conv_un_19(float64,int64)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL
//-- +inf r8 --
	ldc.r8			float64(0x7FF0000000000000)
	ldc.i8			0x0000000000000000
	call		int32 conv_ovf_i8::conv_un_20(float64,int64)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL
//-- NaN r8 --
	ldc.r8			float64(0x7FF8000000000000)
	ldc.i8			0x0000000000000000
	call		int32 conv_ovf_i8::conv_un_21(float64,int64)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL
//-- 1.5 r8 --
	ldc.r8			1.5
	ldc.i8			0x0000000000000001
	call		int32 conv_ovf_i8::conv_un_22(float64,int64)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- 999.99 r8 --
	ldc.r8			99.99
	ldc.i8			0x0000000000000063
	call		int32 conv_ovf_i8::conv_un_23(float64,int64)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL

//-- -min i8 --
/*	// the smallest i8 that can be represented as an r8 is 0x8000000000000400.
	// therefore the smallest r8 that can be converted to an i8 without
	// overflow is 0xC3DFFFFFFFFFFFFF.
	ldc.r8			float64(0xC3DFFFFFFFFFFFFF)
	ldc.i8			0x8000000000000400
	call		int32 conv_ovf_i8::conv_un_24(float64,int64)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
*///-- +max i8 --
	// the largest i8 that can be represented as an r8 is 0x2000000000000000.
	// therefore the largest r8 that can be converted to an i8 without
	// overflow is 0x43C0000000000000.
	ldc.r8			float64(0x43C0000000000000)
	ldc.i8			0x2000000000000000
	call		int32 conv_ovf_i8::conv_un_25(float64,int64)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL


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

.assembly ldc_conv_ovf_r8_i8.exe{}

