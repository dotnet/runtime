// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// FiltFallThru.asm
// Revised 4.01.99 by t-joecl
//
// Apply filters that don't trap an exception, then catch it in an
// outer .exception aHandler.
//
.assembly extern legacy library mscorlib{}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly FiltFallThru{}

.class public		FiltFallThru {

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
    .try{
	try_start:
		newobj instance void [mscorlib]System.Security.SecurityException::.ctor()
		throw	
		try_end:
		// Error case 4 - exception _not thrown
		ldstr	"Error case 4 - exception _not thrown"


		call void [System.Console]System.Console::WriteLine(class System.String)
		ldc.i4		0x4
		stsfld int32 FiltFallThru::iResult
		leave ENDOFTEST
	}

	// aFilter two exceptions that won't be encountered;
	// let them fall through. 
	filter{
	OOMfilter:
		isinst	[mscorlib]System.OutOfMemoryException
		leave	OOMcontinue

		// Error case 2 - aFilter failure: filtered the wrong exception
		ldstr	"Error case 2 - aFilter failure: filtered the wrong exception"


		call void [System.Console]System.Console::WriteLine(class System.String)
		ldc.i4		0x2
		stsfld int32 FiltFallThru::iResult
		ldnull
		leave	OOMendfilter
		OOMcontinue:
		ldnull
		OOMendfilter:

		AEfilter:
		isinst	[mscorlib]System.ArithmeticException
		leave	AEcontinue

		// Error case 2 - aFilter failure: filtered the wrong exception
		ldstr	"Error case 2 - aFilter failure: filtered the wrong exception"


		call void [System.Console]System.Console::WriteLine(class System.String)
		ldc.i4		0x2
		stsfld int32 FiltFallThru::iResult
		ldc.i4  0x00000001
		leave	AEendfilter
		AEcontinue:
		ldc.i4  0x00000000
		AEendfilter:
		endfilter
	}
	{
	FILThandler:
		pop
		//	isinst	System.ArithmeticException
		//	brfalse	FILTcontinue
		//	br FILTcontinue
	FILTcontinue:
		// Error case 3 - running aHandler for the wrong aFilter
		ldstr	"Error case 3 - running aHandler for the wrong aFilter"


		call void [System.Console]System.Console::WriteLine(class System.String)
		ldc.i4		0x3
		stsfld int32 FiltFallThru::iResult
		leave ENDFILThandler
	ENDFILThandler:
		leave ENDOFTEST

	FILThandler1:
		pop
	//	isinst	System.ArithmeticException
	//	brfalse	FILTcontinue1
	//	br FILTcontinue1
	FILTcontinue1:
	// Error case 3 - running aHandler for the wrong aFilter
		ldstr	"Error case 3 - running aHandler for the wrong aFilter"
		call void [System.Console]System.Console::WriteLine(class System.String)
		ldc.i4		0x3
		stsfld int32 FiltFallThru::iResult
		leave ENDFILThandler1
	ENDFILThandler1:
	leave ENDOFTEST
      }
}

catch [mscorlib]System.Security.SecurityException{
SEChandler:
	isinst	[mscorlib]System.Security.SecurityException
	brfalse	handler_fail
	br end_handler

handler_fail:
	// Error case 1 - aHandler failure: trapped the wrong exception
	ldc.i4		0x1
	stsfld int32 FiltFallThru::iResult
	ldstr	"Error case 1 - aHandler failure: trapped the wrong exception"


    	call void [System.Console]System.Console::WriteLine(class System.String)
end_handler:
	leave ENDOFTEST
}
ENDOFTEST:
	ldsfld int32 FiltFallThru::iResult
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
	ldsfld int32 FiltFallThru::iResult
RETEND:
	ldloc.0
    	call void [System.Console]System.Console::WriteLine(class System.String)
	ret

}
}
