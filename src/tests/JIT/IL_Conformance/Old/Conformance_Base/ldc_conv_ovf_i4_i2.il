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
.class public		conv_ovf_i2 {

//---- GLOBAL DATA ----------


//---- METHODS --------------





.method public static		int32 conv_0(int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i2
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler aHandler to HEnd

}
.method public static		int32 conv_un_0(unsigned int32,unsigned int32) {
.locals		(class [mscorlib]System.ArithmeticException,unsigned int16)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i2.un
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start2 to try_end2 catch [mscorlib]System.ArithmeticException handler aHandler2 to HEnd2

}
.method public static		int32 conv_1(int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i2
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler aHandler to HEnd

}
.method public static		int32 conv_un_1(unsigned int32,unsigned int32) {
.locals		(class [mscorlib]System.ArithmeticException,unsigned int16)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i2.un
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start2 to try_end2 catch [mscorlib]System.ArithmeticException handler aHandler2 to HEnd2

}
.method public static		int32 conv_2(int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i2
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler aHandler to HEnd

}
.method public static		int32 conv_un_2(unsigned int32,unsigned int32) {
.locals		(class [mscorlib]System.ArithmeticException,unsigned int16)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i2.un
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start2 to try_end2 catch [mscorlib]System.ArithmeticException handler aHandler2 to HEnd2

}
.method public static		int32 conv_3(int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i2
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler aHandler to HEnd

}
.method public static		int32 conv_un_3(unsigned int32,unsigned int32) {
.locals		(class [mscorlib]System.ArithmeticException,unsigned int16)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i2.un
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start2 to try_end2 catch [mscorlib]System.ArithmeticException handler aHandler2 to HEnd2

}
.method public static		int32 conv_4(int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i2
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler aHandler to HEnd

}
.method public static		int32 conv_un_4(unsigned int32,unsigned int32) {
.locals		(class [mscorlib]System.ArithmeticException,unsigned int16)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i2.un
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start2 to try_end2 catch [mscorlib]System.ArithmeticException handler aHandler2 to HEnd2

}
.method public static		int32 conv_5(int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i2
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler aHandler to HEnd

}
.method public static		int32 conv_un_5(unsigned int32,unsigned int32) {
.locals		(class [mscorlib]System.ArithmeticException,unsigned int16)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i2.un
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start2 to try_end2 catch [mscorlib]System.ArithmeticException handler aHandler2 to HEnd2

}
.method public static		int32 conv_6(int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i2
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler aHandler to HEnd

}
.method public static		int32 conv_un_6(unsigned int32,unsigned int32) {
.locals		(class [mscorlib]System.ArithmeticException,unsigned int16)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i2.un
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start2 to try_end2 catch [mscorlib]System.ArithmeticException handler aHandler2 to HEnd2

}
.method public static		int32 conv_7(int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i2
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler aHandler to HEnd

}
.method public static		int32 conv_un_7(unsigned int32,unsigned int32) {
.locals		(class [mscorlib]System.ArithmeticException,unsigned int16)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i2.un
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start2 to try_end2 catch [mscorlib]System.ArithmeticException handler aHandler2 to HEnd2

}
.method public static		int32 conv_8(int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i2
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler aHandler to HEnd

}
.method public static		int32 conv_un_8(unsigned int32,unsigned int32) {
.locals		(class [mscorlib]System.ArithmeticException,unsigned int16)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i2.un
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start2 to try_end2 catch [mscorlib]System.ArithmeticException handler aHandler2 to HEnd2

}
.method public static		int32 conv_9(int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i2
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler aHandler to HEnd

}
.method public static		int32 conv_un_9(unsigned int32,unsigned int32) {
.locals		(class [mscorlib]System.ArithmeticException,unsigned int16)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i2.un
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start2 to try_end2 catch [mscorlib]System.ArithmeticException handler aHandler2 to HEnd2

}
.method public static		int32 conv_10(int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i2
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler aHandler to HEnd

}
.method public static		int32 conv_un_10(unsigned int32,unsigned int32) {
.locals		(class [mscorlib]System.ArithmeticException,unsigned int16)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i2.un
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start2 to try_end2 catch [mscorlib]System.ArithmeticException handler aHandler2 to HEnd2

}
.method public static		int32 conv_11(int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i2
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler aHandler to HEnd

}
.method public static		int32 conv_un_11(unsigned int32,unsigned int32) {
.locals		(class [mscorlib]System.ArithmeticException,unsigned int16)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i2.un
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start2 to try_end2 catch [mscorlib]System.ArithmeticException handler aHandler2 to HEnd2

}
.method public static		int32 conv_12(int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i2
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler aHandler to HEnd

}
.method public static		int32 conv_un_12(unsigned int32,unsigned int32) {
.locals		(class [mscorlib]System.ArithmeticException,unsigned int16)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i2.un
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start2 to try_end2 catch [mscorlib]System.ArithmeticException handler aHandler2 to HEnd2

}
.method public static		int32 conv_13(int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i2
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler aHandler to HEnd

}
.method public static		int32 conv_un_13(unsigned int32,unsigned int32) {
.locals		(class [mscorlib]System.ArithmeticException,unsigned int16)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i2.un
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start2 to try_end2 catch [mscorlib]System.ArithmeticException handler aHandler2 to HEnd2

}
.method public static		int32 conv_14(int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i2
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler aHandler to HEnd

}
.method public static		int32 conv_un_14(unsigned int32,unsigned int32) {
.locals		(class [mscorlib]System.ArithmeticException,unsigned int16)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i2.un
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start2 to try_end2 catch [mscorlib]System.ArithmeticException handler aHandler2 to HEnd2

}
.method public static		int32 conv_15(int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i2
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler aHandler to HEnd

}
.method public static		int32 conv_un_15(unsigned int32,unsigned int32) {
.locals		(class [mscorlib]System.ArithmeticException,unsigned int16)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i2.un
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start2 to try_end2 catch [mscorlib]System.ArithmeticException handler aHandler2 to HEnd2

}
.method public static		int32 conv_16(int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i2
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler aHandler to HEnd

}
.method public static		int32 conv_un_16(unsigned int32,unsigned int32) {
.locals		(class [mscorlib]System.ArithmeticException,unsigned int16)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i2.un
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start2 to try_end2 catch [mscorlib]System.ArithmeticException handler aHandler2 to HEnd2

}
.method public static		int32 conv_17(int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i2
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler aHandler to HEnd

}
.method public static		int32 conv_un_17(unsigned int32,unsigned int32) {
.locals		(class [mscorlib]System.ArithmeticException,unsigned int16)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i2.un
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start2 to try_end2 catch [mscorlib]System.ArithmeticException handler aHandler2 to HEnd2

}
.method public static		int32 conv_18(int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i2
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler aHandler to HEnd

}
.method public static		int32 conv_un_18(unsigned int32,unsigned int32) {
.locals		(class [mscorlib]System.ArithmeticException,unsigned int16)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i2.un
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start2 to try_end2 catch [mscorlib]System.ArithmeticException handler aHandler2 to HEnd2

}
.method public static		int32 conv_19(int32,int32) {
.locals		(class [mscorlib]System.OverflowException, int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i2
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler aHandler to HEnd

}
.method public static		int32 conv_un_19(unsigned int32,unsigned int32) {
.locals		(class [mscorlib]System.ArithmeticException,unsigned int16)
.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i2.un
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
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
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
.try try_start2 to try_end2 catch [mscorlib]System.ArithmeticException handler aHandler2 to HEnd2

}
//---- CONSTRUCTOR ----------
.method public		void conv_ovf_i2() {
.maxstack	1
	ret
}

//---- MAIN -----------------
.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
//====== begin testing ======
//-- min i4 --
	ldc.i4			0x80000000
	ldc.i4			0x00000000
	call		int32 conv_ovf_i2::conv_0(int32,int32)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL
//-- -1 i4 --
	ldc.i4			0xFFFFFFFF
	ldc.i4			0xFFFFFFFF
	call		int32 conv_ovf_i2::conv_1(int32,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- 0 i4 --
	ldc.i4			0x00000000
	ldc.i4			0x00000000
	call		int32 conv_ovf_i2::conv_2(int32,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- +1 i4 --
	ldc.i4			0x00000001
	ldc.i4			0x00000001
	call		int32 conv_ovf_i2::conv_3(int32,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- max i4 --
	ldc.i4			0x7FFFFFFF
	ldc.i4			0x00000000
	call		int32 conv_ovf_i2::conv_4(int32,int32)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL

//-- min i2 --
	ldc.i4			0xFFFF8000
	ldc.i4			0xFFFF8000
	call		int32 conv_ovf_i2::conv_5(int32,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- -1 i2 --
	ldc.i4			0xFFFFFFFF
	ldc.i4			0xFFFFFFFF
	call		int32 conv_ovf_i2::conv_6(int32,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- 0 i2 --
	ldc.i4			0x00000000
	ldc.i4			0x00000000
	call		int32 conv_ovf_i2::conv_7(int32,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- +1 i2 --
	ldc.i4			0x00000001
	ldc.i4			0x00000001
	call		int32 conv_ovf_i2::conv_8(int32,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- max i2 --
	ldc.i4			0x00007FFF
	ldc.i4			0x00007FFF
	call		int32 conv_ovf_i2::conv_9(int32,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL

//====== conv_un testing ======
//-- min i4 --
/*	ldc.i4			0x80000000
	ldc.i4			0x00000000
	call		int32 conv_ovf_i2::conv_un_10(unsigned int32,unsigned int32)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL
//-- -1 i4 --
	ldc.i4			0xFFFFFFFF
	ldc.i4			0xFFFFFFFF
	call		int32 conv_ovf_i2::conv_un_11(unsigned int32,unsigned int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
*///-- 0 i4 --
	ldc.i4			0x00000000
	ldc.i4			0x00000000
	call		int32 conv_ovf_i2::conv_un_12(unsigned int32,unsigned int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- +1 i4 --
	ldc.i4			0x00000001
	ldc.i4			0x00000001
	call		int32 conv_ovf_i2::conv_un_13(unsigned int32,unsigned int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- max i4 --
	ldc.i4			0x7FFFFFFF
	ldc.i4			0x00000000
	call		int32 conv_ovf_i2::conv_un_14(unsigned int32,unsigned int32)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL

//-- min i2 --
/*	ldc.i4			0xFFFF8000
	ldc.i4			0xFFFF8000
	call		int32 conv_ovf_i2::conv_un_15(unsigned int32,unsigned int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- -1 i2 --
	ldc.i4			0xFFFFFFFF
	ldc.i4			0xFFFFFFFF
	call		int32 conv_ovf_i2::conv_un_16(unsigned int32,unsigned int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
*///-- 0 i2 --
	ldc.i4			0x00000000
	ldc.i4			0x00000000
	call		int32 conv_ovf_i2::conv_un_17(unsigned int32,unsigned int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- +1 i2 --
	ldc.i4			0x00000001
	ldc.i4			0x00000001
	call		int32 conv_ovf_i2::conv_un_18(unsigned int32,unsigned int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- max i2 --
	ldc.i4			0x00007FFF
	ldc.i4			0x00007FFF
	call		int32 conv_ovf_i2::conv_un_19(unsigned int32,unsigned int32)
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



.assembly ldc_conv_ovf_i4_i2.exe{}

