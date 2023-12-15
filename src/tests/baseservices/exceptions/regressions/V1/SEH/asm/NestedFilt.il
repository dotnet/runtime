// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// NestedFilt.asm
// Revised 4.01.99 by t-joecl
// 
// Nests filters that catch the same exception.
//
.assembly extern legacy library mscorlib{}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly NestedFilt{}

.class public		NestedFilt {

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
.maxstack	3

	ldstr	"Done"
	stloc.0

try_start:
	newobj instance void [mscorlib]System.ArithmeticException::.ctor()
	throw 
try_end:
	// Error case 4 - exception _not thrown
	ldstr	"Error case 4 - exception _not thrown"


	call void [System.Console]System.Console::WriteLine(class System.String)
	ldc.i4		0x4
	stsfld int32 NestedFilt::iResult
	br ENDOFTEST


filter1:
	isinst	[mscorlib]System.ArithmeticException
	brfalse	continue1
	ldc.i4  0x00000001
	br	endfilter1
continue1:
	ldc.i4  0x00000000
	// Error case 2 - aFilter failure: filtered the wrong exception
	ldstr	"Error case 2 - aFilter failure: filtered the wrong exception"


	call void [System.Console]System.Console::WriteLine(class System.String)
	ldc.i4		0x2
	stsfld int32 NestedFilt::iResult
endfilter1:
	endfilter


handler1:
	isinst	[mscorlib]System.ArithmeticException
	brfalse	handler_continue1
	br	end_handler1
handler_continue1:
	// Error case 1 - aHandler failure: trapped the wrong exception
	ldstr	"Error case 1 - aHandler failure: trapped the wrong exception"


	call void [System.Console]System.Console::WriteLine(class System.String)
	ldc.i4		0x1
	stsfld int32 NestedFilt::iResult
end_handler1:
	leave ENDOFhandler1
ENDOFhandler1:
	br after_handler2

filter2:
	isinst	[mscorlib]System.ArithmeticException
	brfalse	continue2
	ldc.i4  0x00000001
	br	endfilter2
continue2:
	ldc.i4  0x00000000
	// Error case 2 - aFilter failure: filtered the wrong exception
	ldstr	"Error case 2 - aFilter failure: filtered the wrong exception"


	call void [System.Console]System.Console::WriteLine(class System.String)
	ldc.i4		0x2
	stsfld int32 NestedFilt::iResult
endfilter2:
	// Error case 5 - exception propagates beyond first aFilter
	ldstr	"Error case 5 - exception propagates beyond first aFilter"


	call void [System.Console]System.Console::WriteLine(class System.String)
	ldc.i4		0x5
	stsfld int32 NestedFilt::iResult
	endfilter

handler2:
	pop
	// Error case 5 - exception propagates beyond first aFilter
	ldstr	"Error case 5 - exception propagates beyond first aFilter"


	call void [System.Console]System.Console::WriteLine(class System.String)
	ldc.i4		0x5
	stsfld int32 NestedFilt::iResult
	leave ENDOFhandler2

ENDOFhandler2:

.try try_start to try_end filter filter1 handler handler1 to ENDOFhandler1
.try try_start to try_end filter filter2 handler handler2 to ENDOFhandler2

after_handler2:

ENDOFTEST:
	ldsfld int32 NestedFilt::iResult
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
	ldsfld int32 NestedFilt::iResult
RETEND:
	ldloc.0
   	call void [System.Console]System.Console::WriteLine(class System.String)
	ret

}
}
