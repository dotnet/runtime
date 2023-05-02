// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b109721'{}

.class public _div {
//
.method public static	int32 _div(int32,int32) {
.locals		(class [mscorlib]System.Exception,int32)
.maxstack		3
try_start:
	ldarg	0
	ldarg	1
	div
	stloc.1
	leave.s	try_end
try_end:
	//- No exception
	ldloc.1
	br				END

arithmetic:
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
        ldstr      "Got arithmetic exception"
        call       void [System.Console]System.Console::WriteLine(string)
	stloc	0
	leave			AEEnd
AEEnd:

	ldloc	0
	brfalse			FAIL
        ldstr      "returning 0xAE"
        call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4			0xAE
	br				END

divbyzero:
	//- Got a DivideByZeroException
	isinst			[mscorlib]System.DivideByZeroException
        ldstr      "Got divide by zero"
        call       void [System.Console]System.Console::WriteLine(string)
	stloc	0
	leave			DBZEnd
DBZEnd:

	ldloc	0
	brfalse			FAIL
        ldstr      "returning 0xDB0E"
        call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4			0xDB0E
	br				END

FAIL:
        ldstr      "Fail"
        call       void [System.Console]System.Console::WriteLine(string)
	ldc.i4			0x00000000
	br				END
END:
	ret
.try try_start to try_end catch [mscorlib]System.DivideByZeroException handler divbyzero to DBZEnd
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler arithmetic to AEEnd
}
//
// -- Begin Main --------------------
.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack		20
//

// Div has two handlers, but both ArithmeticExceptions and 
// DivideByZeroExceptions appear to be caught by the first
// handler. However, a handler for ArithmeticException shouldn't
// catch DivideByZero and vice-versa.

br START_DB0

START_AE:
// -- Min / -1
	ldc.i4		0x80000000
	ldc.i4		0xFFFFFFFF
        ldstr      "80000000/-1"
        call       void [System.Console]System.Console::WriteLine(string)
	call	int32 _div::_div(int32,int32)
	ldc.i4		0xAE
	ceq
	brfalse		FAIL
br ENDTESTS
END_AE:

START_DB0:
// -- Min / 0
	ldc.i4		0x80000000
	ldc.i4		0x00000000
        ldstr      "80000000/0"
        call       void [System.Console]System.Console::WriteLine(string)
	call	int32 _div::_div(int32,int32)
	ldc.i4		0xDB0E
	ceq
	brfalse		FAIL
br ENDTESTS
END_DB0:

ENDTESTS:
// -------------------------
//
// -- PASS --
	ldc.i4	100
	ret
// -- FAIL --
FAIL:
	ldc.i4	0x0
	ret
// -- End Main ----------------------
}
//
// -- EOF ---------------------------
}
// ----------------------------------
