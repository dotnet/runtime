// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern legacy library mscorlib {}
.assembly 'localloc_unaligned_2' {
                        }



.class public			_localloc {

.method void .ctor() {

.maxstack	10
	ldarg.0
	call instance void [mscorlib]System.Object::.ctor() 
	ret
}

.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.locals		(class [mscorlib]System.IO.TextWriter,int32*,int32,class [mscorlib]System.Text.StringBuilder,int32,class [mscorlib]System.String)
.entrypoint
//INITIALIZATION
	call        class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
	stloc		0
	ldc.i4		0x0
	stloc		2
	newobj	instance	void [mscorlib]System.Text.StringBuilder::.ctor() 
	stloc		3

TryStart:
//Initialize 256 bytes of memory _and store ptr as loc1
	ldc.i4		0xFF
	localloc
	stloc		1

//Loop through _and fill up all 256 bytes. 
//stick a zero in byte zero, a 1 in byte 1, etc.
LOOP0:
	//exit loop if memory is full.
	ldloc		2
	ldc.i4		0xFF
	bgt			END0

	//advance to next available mem location
	ldloc		1
	ldloc		2
	add

	//store value of loop counter (loc 2) at the calculated location.	
	ldloc		2
	conv.i1
	unaligned. 0x2
	stind.i1

	//increment out loop counter
	ldc.i4		0x1
	ldloc		2
	add
	stloc		2

	//loop
	br			LOOP0

END0:

//Loop through _and test all bytes for correctness.
	//move loop counter back to zero
	ldc.i4		0x0
	stloc		2

LOOP1:
	//If we have moved through all of our memory, PASS
	ldloc		2
	ldc.i4		0xFF
	bgt			EXITWITHPASS
	
	//advance to next mem location
	ldloc		1
	ldloc		2
	add

	//load value at the calculated location.
	unaligned. 0x2
	ldind.i1

	//load loop counter _and compare value retrieved from mem.
	ldloc		2
	conv.i1

	//if the values are _not equal, imediatly fail.
	bne.un		EXITWITHFAIL

	//increment our loop counter
	ldc.i4		0x1
	ldloc		2
	add
	stloc		2

	//loop
	br			LOOP1

END1:
	EXITWITHPASS:
	leave Pass
	EXITWITHFAIL:
	leave Fail

TryEnd:
	br			Pass

Filter:
	pop
	ldc.i4		0x1
	endfilter

Handler:
	pop
	leave			EndHandler
EndHandler:
	br			Fail

Pass:
	ldloc			0
	ldstr			"LOCALLOC Tests PASS"
	callvirt	instance void [mscorlib]System.IO.TextWriter::WriteLine(class [mscorlib]System.String)
	ldc.i4		0x64
	br			End

Fail:
	ldloc		0
	ldstr		"!!!! FAILURE !!!! -- LOCALLOC Tests FAIL -- !!!! FAILURE !!!!"
	callvirt	instance void [mscorlib]System.IO.TextWriter::WriteLine(class [mscorlib]System.String)
	ldc.i4		0x0
	br			End

End:
	ret
.try TryStart to TryEnd filter Filter handler Handler to EndHandler
}
}
