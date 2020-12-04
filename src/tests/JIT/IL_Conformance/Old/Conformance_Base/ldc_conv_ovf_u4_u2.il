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
.class public		conv_ovf_u2 {

//---- GLOBAL DATA ----------


//---- METHODS --------------




.method public static		int32 conv_0(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.u2
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
.method public static		int32 conv_un_0(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.u2.un
	stloc.1
	leave.s	try_end
try_end:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
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
.method public static		int32 conv_1(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.u2
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
.method public static		int32 conv_un_1(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.u2.un
	stloc.1
	leave.s	try_end
try_end:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
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
.method public static		int32 conv_2(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.u2
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
.method public static		int32 conv_un_2(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.u2.un
	stloc.1
	leave.s	try_end
try_end:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
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
.method public static		int32 conv_3(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.u2
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
.method public static		int32 conv_un_3(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.u2.un
	stloc.1
	leave.s	try_end
try_end:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
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
.method public static		int32 conv_4(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.u2
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
.method public static		int32 conv_un_4(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.u2.un
	stloc.1
	leave.s	try_end
try_end:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
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
.method public static		int32 conv_5(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.u2
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
.method public static		int32 conv_un_5(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.u2.un
	stloc.1
	leave.s	try_end
try_end:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
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
.method public static		int32 conv_6(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.u2
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
.method public static		int32 conv_un_6(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.u2.un
	stloc.1
	leave.s	try_end
try_end:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
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
.method public static		int32 conv_7(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,int32)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.u2
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
.method public static		int32 conv_un_7(int32,int32) {
.locals		(class [mscorlib]System.OverflowException,unsigned int16)
.maxstack	2
try_start:
	ldarg		0
	conv.ovf.u2.un
	stloc.1
	leave.s	try_end
try_end:
	//- No exception
	ldloc.1
	ldarg		1
	ceq
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
//---- CONSTRUCTOR ----------
.method public		void conv_ovf_u2() {
.maxstack	1
	ret
}

//---- MAIN -----------------
.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		5
//====== begin testing ======
//-- 0 u4 --
	ldc.i4			0x00000000
	ldc.i4			0x00000000
	call		int32 conv_ovf_u2::conv_0(int32,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- +1 u4 --
	ldc.i4			0x00000001
	ldc.i4			0x00000001
	call		int32 conv_ovf_u2::conv_1(int32,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- max u4 --
	ldc.i4			0xFFFFFFFF
	ldc.i4			0x00000000
	call		int32 conv_ovf_u2::conv_2(int32,int32)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL

//-- max u1 --
	ldc.i4			0x0000FFFF
	ldc.i4			0x0000FFFF
	call		int32 conv_ovf_u2::conv_3(int32,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL

//====== conv_un testing ======
//-- 0 u4 --
	ldc.i4			0x00000000
	ldc.i4			0x00000000
	call		int32 conv_ovf_u2::conv_un_4(int32,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- +1 u4 --
	ldc.i4			0x00000001
	ldc.i4			0x00000001
	call		int32 conv_ovf_u2::conv_un_5(int32,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- max u4 --
	ldc.i4			0xFFFFFFFF
	ldc.i4			0x00000000
	call		int32 conv_ovf_u2::conv_un_6(int32,int32)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL

//-- max u1 --
	ldc.i4			0x0000FFFF
	ldc.i4			0x0000FFFF
	call		int32 conv_ovf_u2::conv_un_7(int32,int32)
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

.assembly ldc_conv_ovf_u4_u2.exe{}

