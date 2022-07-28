// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern legacy library mscorlib {}
.assembly 'b59546' {}
.assembly extern xunit.core {}
.class public bug
{
	.method public static void f1() cil managed
	{
		.maxstack 20
		br start_try1

		start_catch1: 
		catch1_out:
		pop
		newobj instance void [mscorlib]System.StackOverflowException::.ctor()
		throw
		end_catch1: 

		exception_filter1: 
		pop
		ldc.i4.1
		endfilter

		start_filter1: 
		pop
		br filter1_br
		filter1_br: 
		start_try2: 
		try2_out:
		leave filter1_out
		end_try2: 

		start_catch2: 
		catch2_out:
		pop
		leave filter1_out
		end_catch2: 

		filter1_out: leave OUT0
		end_filter1: 

		start_try1: 
		try1_out:
		leave OUT0
		end_try1: 
		OUT0:	ret
		.try start_try2 to end_try2 catch [mscorlib]System.StackOverflowException handler start_catch2 to end_catch2
		.try start_try1 to end_try1 catch [mscorlib]System.Exception handler start_catch1 to end_catch1
		.try start_try1 to end_try1 filter exception_filter1 handler start_filter1 to end_filter1

	}

	.method public static int32 Main() cil managed
	{
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		.try
		{
		call void bug::f1()
		leave RET
		}
		catch [mscorlib]System.Exception
		{
		leave RET
		}
		RET: ldc.i4 100
		ldstr "PASSED"
		call void [System.Console]System.Console::WriteLine(string)
		ret
	}

}
