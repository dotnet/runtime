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
.class public		conv_ovf_i4 {

//---- GLOBAL DATA ----------


//---- METHODS --------------


.method public static		int32 conv_0(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_0(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_1(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_1(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_2(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_2(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_3(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_3(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_4(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_4(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_5(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_5(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_6(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_6(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_7(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_7(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_8(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_8(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_9(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_9(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_10(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_10(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_11(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_11(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_12(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_12(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_13(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_13(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_14(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_14(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_15(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_15(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_16(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_16(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_17(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_17(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_18(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_18(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_19(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_19(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_20(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_20(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_21(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_21(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_22(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_22(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_23(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_23(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_24(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_24(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public static		int32 conv_25(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start:
	ldarg		0
	conv.ovf.i
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
.method public static		int32 conv_u_25(float64,int32) {
.locals		(class [mscorlib]System.OverflowException,native int)

.maxstack	2
try_start2:
	ldarg		0
	conv.ovf.i.un
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
.method public		void conv_ovf_i4() {
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
	ldc.i4			0x00000000
	call		int32 conv_ovf_i4::conv_0(float64,int32)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL
//-- -min r8 --
	ldc.r8			float64(0xFFEFFFFFFFFFFFFF)
	ldc.i4			0x00000000
	call		int32 conv_ovf_i4::conv_1(float64,int32)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL
//-- -1 r8 --
	ldc.r8			float64(0xBFF0000000000000)
	ldc.i4			0xFFFFFFFF
	call		int32 conv_ovf_i4::conv_2(float64,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- -0 r8 --
	ldc.r8			float64(0x8000000000000000)
	ldc.i4			0x00000000
	call		int32 conv_ovf_i4::conv_3(float64,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- +0 r8 --
	ldc.r8			float64(0x0000000000000000)
	ldc.i4			0x00000000
	call		int32 conv_ovf_i4::conv_4(float64,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- +1 r8 --
	ldc.r8			float64(0x3FF0000000000000)
	ldc.i4			0x00000001
	call		int32 conv_ovf_i4::conv_5(float64,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- +max r8 --
	ldc.r8			float64(0x7FEFFFFFFFFFFFFF)
	ldc.i4			0x00000000
	call		int32 conv_ovf_i4::conv_6(float64,int32)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL
//-- +inf r8 --
	ldc.r8			float64(0x7FF0000000000000)
	ldc.i4			0x00000000
	call		int32 conv_ovf_i4::conv_7(float64,int32)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL
//-- NaN r8 --
	ldc.r8			float64(0x7FF8000000000000)
	ldc.i4			0x00000000
	call		int32 conv_ovf_i4::conv_8(float64,int32)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL

//-- 1.5 r8 --
	ldc.r8			1.5
	ldc.i4			0x00000001
	call		int32 conv_ovf_i4::conv_9(float64,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- 999.99 r8 --
	ldc.r8			99.99
	ldc.i4			0x00000063
	call		int32 conv_ovf_i4::conv_10(float64,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL

//-- -min i4 --
	ldc.r8			float64(0xC1E0000000000000)
	ldc.i4			0x80000000
	call		int32 conv_ovf_i4::conv_11(float64,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- +max i4 --
	ldc.r8			float64(0x41DFFFFFFFC00000)
	ldc.i4			0x7FFFFFFF
	call		int32 conv_ovf_i4::conv_12(float64,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL

//====== conv_u testing ======

//-- -inf r8 --
/*	ldc.r8			float64(0xFFF0000000000000)
	ldc.i4			0x00000000
	call		int32 conv_ovf_i4::conv_u_13(float64,int32)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL
//-- -min r8 --
	ldc.r8			float64(0xFFEFFFFFFFFFFFFF)
	ldc.i4			0x00000000
	call		int32 conv_ovf_i4::conv_u_14(float64,int32)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL
//-- -1 r8 --
	ldc.r8			float64(0xBFF0000000000000)
	ldc.i4			0xFFFFFFFF
	call		int32 conv_ovf_i4::conv_u_15(float64,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- -0 r8 --
	ldc.r8			float64(0x8000000000000000)
	ldc.i4			0x00000000
	call		int32 conv_ovf_i4::conv_u_16(float64,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
*///-- +0 r8 --
	ldc.r8			float64(0x0000000000000000)
	ldc.i4			0x00000000
	call		int32 conv_ovf_i4::conv_u_17(float64,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- +1 r8 --
	ldc.r8			float64(0x3FF0000000000000)
	ldc.i4			0x00000001
	call		int32 conv_ovf_i4::conv_u_18(float64,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- +max r8 --
	ldc.r8			float64(0x7FEFFFFFFFFFFFFF)
	ldc.i4			0x00000000
	call		int32 conv_ovf_i4::conv_u_19(float64,int32)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL
//-- +inf r8 --
	ldc.r8			float64(0x7FF0000000000000)
	ldc.i4			0x00000000
	call		int32 conv_ovf_i4::conv_u_20(float64,int32)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL
//-- NaN r8 --
	ldc.r8			float64(0x7FF8000000000000)
	ldc.i4			0x00000000
	call		int32 conv_ovf_i4::conv_u_21(float64,int32)
	ldc.i4			0xEEEEEEEE
	ceq
	brfalse			FAIL

//-- 1.5 r8 --
	ldc.r8			1.5
	ldc.i4			0x00000001
	call		int32 conv_ovf_i4::conv_u_22(float64,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
//-- 999.99 r8 --
	ldc.r8			99.99
	ldc.i4			0x00000063
	call		int32 conv_ovf_i4::conv_u_23(float64,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL

//-- -min i4 --
/*	ldc.r8			float64(0xC1E0000000000000)
	ldc.i4			0x80000000
	call		int32 conv_ovf_i4::conv_u_24(float64,int32)
	ldc.i4			0x11111111
	ceq
	brfalse			FAIL
*///-- +max i4 --
	ldc.r8			float64(0x41DFFFFFFFC00000)
	ldc.i4			0x7FFFFFFF
	call		int32 conv_ovf_i4::conv_u_25(float64,int32)
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


.assembly ldc_conv_ovf_r8_i.exe{}

