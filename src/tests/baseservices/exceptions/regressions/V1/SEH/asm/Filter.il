// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Filter.asm
// Revised 4.01.99 by t-joecl
// 
// Basic aFilter test case.
//
.assembly extern legacy library mscorlib{}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly Filter{}


.class public		Filter {

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
try_start:
	newobj instance void [mscorlib]System.Security.SecurityException::.ctor()
	throw 
try_end:
	// Error case 4 - exception _not thrown
	ldstr	"Error case 4 - exception _not thrown"


	call void [System.Console]System.Console::WriteLine(class System.String)
	ldc.i4		0x4
	stsfld int32 Filter::iResult
	leave ENDOFTEST
}
filter{
filter1:
	isinst	[mscorlib]System.Security.SecurityException
	brfalse	continue1
	ldc.i4  0x00000001
	br	endfilter1
continue1:
	ldc.i4  0x00000000
endfilter1:
	endfilter
}
{
handler1:
	isinst	[mscorlib]System.Security.SecurityException
	brfalse	handler_fail1
	br end_handler1
handler_fail1:
	// Error case 3 - running aHandler for the wrong aFilter
	ldstr	"Error case 3 - running aHandler for the wrong aFilter"


    call void [System.Console]System.Console::WriteLine(class System.String)
	ldc.i4		0x1
	stsfld int32 Filter::iResult
end_handler1:
	leave ENDOFTEST
}
ENDOFTEST:
	ldsfld int32 Filter::iResult
	ldc.i4		0
	ceq
	brfalse		RETFAIL
	ldstr	"Test succeeded (returning 100)."


    call void [System.Console]System.Console::WriteLine(class System.String)
	ldc.i4		0x64
	br RETEND
RETFAIL:
	ldstr	"Test FAILED."


   	call void [System.Console]System.Console::WriteLine(class System.String)
	ldsfld int32 Filter::iResult
RETEND:
	ldloc.0
   	call void [System.Console]System.Console::WriteLine(class System.String)
	ret

}
}
