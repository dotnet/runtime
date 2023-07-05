// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b05737
{
}
.class public _simple {

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	100

	.try
	{
		ldc.r8 1
		conv.ovf.u
		conv.ovf.u8
		conv.i4
		ldc.i4 1
		sub
		leave Passed
	}
	catch [mscorlib]System.Exception
	{
		pop
		leave Failed
	}
	
Failed:
	ldstr	"Test failed."
    call	void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
	ldc.i4 1
	ret

Passed:
	ldstr	"Test passed."
    call	void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
	ldc.i4 100
	ret
}
}
