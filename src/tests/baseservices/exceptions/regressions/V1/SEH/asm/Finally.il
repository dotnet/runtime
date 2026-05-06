// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Finally.asm
// Revised 4.01.99 by t-joecl
//
// Exercises finally exception syntax.
//
.assembly extern legacy library mscorlib{}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly Finally{}


.class public		Finally {
.field static int32 iResult
.field static int32 finally_flag

// global for storing return value - defaults to 0 for success
//iResult:
//	i4	0x00000000
//finally_flag:
//	i4	0x00000000

//clinit
.method static	void .cctor() {
.maxstack 1
		ldc.i4 		0
		stsfld		int32 Finally::iResult
		ldc.i4		0
		stsfld		int32 Finally::finally_flag
		ret
}

// Execution starts here

.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.locals(string)
.entrypoint
.maxstack	2

	ldstr "Done"
	stloc.0

try_start:
	newobj instance void [mscorlib]System.ArithmeticException::.ctor()
	throw
try_end:
	// Error case 4 - exception _not thrown
	ldstr	"Error case 4 - exception _not thrown"


	call void [System.Console]System.Console::WriteLine(class System.String)
	ldc.i4		0x4
	stsfld		int32 Finally::iResult
	leave ENDOFTEST

handler1:
	isinst	[mscorlib]System.ArithmeticException
	brfalse	handler_continue1
	leave end_handler1
handler_continue1:
	// Error case 1 - aHandler failure: trapped the wrong exception
	ldstr	"Error case 1 - aHandler failure: trapped the wrong exception"


	call void [System.Console]System.Console::WriteLine(class System.String)
	ldc.i4		0x1
	stsfld 		int32 Finally::iResult
end_handler1:
	leave ENDOFhandler1
ENDOFhandler1:
	leave ENDOFTEST

finally1:
	ldstr	"Successfully called the finally block."
	call void [System.Console]System.Console::WriteLine(class System.String)
	ldc.i4		0x1
	stsfld 		int32 Finally::finally_flag
	endfinally
ENDOFFinally:

.try try_start to try_end catch [mscorlib]System.ArithmeticException handler handler1 to ENDOFhandler1
.try try_start to finally1 finally handler finally1 to ENDOFFinally

ENDOFTEST:
	ldsfld 		int32 Finally::finally_flag
	ldc.i4		1
	ceq
	brfalse		FINALLYFAIL

	ldsfld 		int32 Finally::iResult
	ldc.i4		0
	ceq
	brfalse		RETFAIL
	ldstr	"Test succeeded (returning 100)."


    	call void [System.Console]System.Console::WriteLine(class System.String)
	ldc.i4	0x64
	br RETEND
FINALLYFAIL:
	ldstr	"Error case 2 - finally _not called"


    	call void [System.Console]System.Console::WriteLine(class System.String)
RETFAIL:
	ldstr	"Test FAILED."


    	call void [System.Console]System.Console::WriteLine(class System.String)
	ldsfld		int32 Finally::iResult
RETEND:
	ldloc.0
    	call void [System.Console]System.Console::WriteLine(class System.String)
	ret

}
}
