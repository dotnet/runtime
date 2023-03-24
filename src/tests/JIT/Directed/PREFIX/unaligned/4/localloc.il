// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}

.assembly extern legacy library mscorlib {}
.assembly 'localloc_unaligned_4' {
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
.locals		(class [mscorlib]System.IO.TextWriter,int32*,int32,class [mscorlib]System.Text.StringBuilder,int32,string)
.entrypoint
	call        class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
	stloc		0
	ldc.i4		0x0
	stloc		2
	newobj	instance 	void [mscorlib]System.Text.StringBuilder::.ctor() 
	stloc		3

TryStart:
	ldc.i4		0xFF
	localloc
	stloc		1

LOOP0:
	ldloc		2
	ldc.i4		0xFF
	bgt			END0

	ldloc		1
	ldloc		2
	add

	ldloc		2
	conv.i1
	unaligned. 0x4
	stind.i1

	ldc.i4		0x1
	ldloc		2
	add
	stloc		2

	br			LOOP0

END0:

	ldc.i4		0x0
	stloc		2

LOOP1:
	ldloc		2
	ldc.i4		0xFF
	bgt			EXITWITHPASS
	
	ldloc		1
	ldloc		2
	add

	unaligned. 0x4
	ldind.i1

	ldloc		2
	conv.i1

	bne.un		EXITWITHFAIL

	ldc.i4		0x1
	ldloc		2
	add
	stloc		2

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
	callvirt	instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
	ldc.i4		0x64
	br			End

Fail:
	ldloc		0
	ldstr		"!!!! FAILURE !!!! -- LOCALLOC Tests FAIL -- !!!! FAILURE !!!!"
	callvirt	instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
	ldc.i4		0x0
	br			End

End:
	ret
.try TryStart to TryEnd filter Filter handler Handler to EndHandler
}
}
