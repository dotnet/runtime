// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// FiltCatch.asm
// Revised 4.01.99 by t-joecl
//
// Apply filters that do trap an exception; make sure it doesn't get out
// to an outer .exception aHandler.
//
.assembly extern legacy library mscorlib{}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly FiltCatch{}

.class public		FiltCatch {

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

.try{
	newobj instance void [mscorlib]System.ArithmeticException::.ctor()
	throw
try_end:
	// Error case 4 - exception _not thrown
	ldstr	"Error case 4 - exception _not thrown"
	call void [System.Console]System.Console::WriteLine(class System.String)
	ldc.i4		0x4
	stsfld  int32 FiltCatch::iResult
	//br ENDOFTEST
	leave ENDOFTEST
}

// aFilter one exception that won't be encountered;
// let it fall through.

filter{
	isinst	[mscorlib]System.OutOfMemoryException
	brfalse	OOMcontinue

	// Error case 2 - aFilter failure: filtered the wrong exception
	ldstr	"Error case 2 - aFilter failure: filtered the wrong exception"
	call void [System.Console]System.Console::WriteLine(class System.String)
	ldc.i4		0x2
	stsfld int32 FiltCatch::iResult
	ldnull
	br	OOMendfilter
OOMcontinue:
	ldnull
OOMendfilter:
AEfilter:
	isinst	[mscorlib]System.ArithmeticException
	brfalse	AEcontinue
	ldc.i4  0x00000001
	br	AEendfilter
AEcontinue:
	ldc.i4  0x00000001
	br	AEendfilter
AEendfilter:
	endfilter
}
{
	pop
	leave ENDOFTEST
ENDOOMFILThandler:
}

ENDOFTEST:
	ldsfld int32 FiltCatch::iResult
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
	ldsfld int32 FiltCatch::iResult
RETEND:
	ldloc.0
   	call void [System.Console]System.Console::WriteLine(class System.String)
	ret

}
}
