// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// NestedExcept.asm
// Revised 4.01.99 by t-joecl
// 
// Nested exception test cases.
//
.assembly extern legacy library mscorlib{}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly NestedExcept{}

.class public		NestedExcept {

// global for storing return value - defaults to 0 for success


//    .data iResult = int32(0x00000000)
.field static int32 iResult

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
	newobj instance void [mscorlib]System.Security.SecurityException::.ctor()
	throw 
try_end:
	// Error case 4 - exception _not thrown
	ldstr	"Error case 4 - exception _not thrown"


	call void [System.Console]System.Console::WriteLine(class System.String)
	ldc.i4		0x4
	stsfld int32 NestedExcept::iResult
	br ENDOFTEST

handler1:
	isinst	[mscorlib]System.Security.SecurityException
	brfalse	error_case1
//	br end_handler1
	leave ENDOFhandler1
error_case1:
	// Error case 1 - aHandler failure: trapped the wrong exception
	ldstr	"Error case 1 - aHandler failure: trapped the wrong exception"


    call void [System.Console]System.Console::WriteLine(class System.String)
	ldc.i4		0x1
	stsfld int32 NestedExcept::iResult
end_handler1:
	leave ENDOFhandler1
ENDOFhandler1:
	br after_handler2

handler2:
	pop
	// Error case 5 - exception propagates beyond first aHandler
	ldstr	"Error case 5 - exception propagates beyond first aHandler"


    call void [System.Console]System.Console::WriteLine(class System.String)
	ldc.i4		0x5
	stsfld int32 NestedExcept::iResult
	leave after_handler2

after_handler2:

.try try_start to try_end catch [mscorlib]System.Security.SecurityException handler handler1 to ENDOFhandler1
.try try_start to try_end catch [mscorlib]System.Security.SecurityException handler handler2 to after_handler2

ENDOFTEST:
	ldsfld int32 NestedExcept::iResult
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
	ldsfld int32 NestedExcept::iResult
RETEND:
	ldloc.0
   	call void [System.Console]System.Console::WriteLine(class System.String)
	ret

}
}
