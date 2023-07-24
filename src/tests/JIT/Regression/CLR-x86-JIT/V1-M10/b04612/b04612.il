// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b04612
{
}

.class public _simple
{
	.method public static int32 main() {
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
	.maxstack	100

    ldstr       "Stage 1"
    call        void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)

	.try
	{
		ldc.i4 3
		ldc.i4 2
		sub.ovf.un
		pop
		leave	Pass1
	}
	catch [mscorlib]System.Exception
	{
		pop						//remove exception from stack
		leave	Failed
	}

Pass1:
    ldstr       "Stage 2"
    call        void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
    
	.try
	{
		ldc.i4 2
		ldc.i4 3
		sub.ovf.un
		pop
		leave	Failed
	}
	catch [mscorlib]System.Exception
	{
		pop						//remove exception from stack
		leave	Pass2
	}

Pass2:
	ldstr       "Test passed."
	call        void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
	ldc.i4 100
	ret

Failed:
	ldstr       "Test failed."
	call        void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
	ldc.i4 1
	ret
}
}
