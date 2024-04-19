// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Except.asm
// Revised 4.01.99 by t-joecl
// Revised 7.01.99 by manishg
// Basic exception handling test case.
//
.assembly extern legacy library mscorlib{}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly Except{}

.class public		Except {

// global for storing return value - defaults to 0 for success


//    .data iResult = int32(0x00000000)
.field static int32 iResult

// Execution starts here

.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.locals (string)
.entrypoint
.maxstack	2

	ldstr	"Done"
	stloc.0

try_start:
	ldc.i4		0xFFFFFF80
	ldc.i4		0x0
	div
	pop
	leave ENDOFTEST
try_end:
	// Error case 4 - exception _not thrown
	ldstr	"Error case 4 - exception _not thrown"


	call void [System.Console]System.Console::WriteLine(class System.String)

	ldc.i4		0x4
	stsfld int32 Except::iResult
	br ENDOFTEST
	//leave ENDOFTEST
handler1:
	isinst	[mscorlib]System.ArithmeticException
	brfalse	error_case1
	leave ENDOFTEST
error_case1:
	// Error case 1 - aHandler failure: trapped the wrong exception
	ldstr	"Error case 1 - aHandler failure: trapped the wrong exception"


    call void [System.Console]System.Console::WriteLine(class System.String)
	ldc.i4		0x1
	stsfld int32 Except::iResult
	leave ENDOFTEST
end_handler1:


.try try_start to try_end catch [mscorlib]System.ArithmeticException handler handler1 to end_handler1

ENDOFTEST:
	ldsfld int32 Except::iResult
	ldc.i4		0
	ceq
	brfalse		RETFAIL
	ldstr	"Test succeeded (returning 100)."


    call void [System.Console]System.Console::WriteLine(class System.String)
	ldc.i4	0x64
	br RETEND
RETFAIL:
	ldstr	"Test FAILED."


   	call void [System.Console]System.Console::WriteLine(class System.String)
	ldsfld int32 Except::iResult
RETEND:
	ldloc.0
	call void [System.Console]System.Console::WriteLine(string)
	ret

}
}
