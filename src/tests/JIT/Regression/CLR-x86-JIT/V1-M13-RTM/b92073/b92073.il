// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib {}
.assembly extern System {}
.assembly EHTest {}
.assembly extern xunit.core {}
.class public EHTest
{
	.method public static void f2() cil managed synchronized
	{
		.maxstack 500
		.locals init (int32 V_0)
		br start_try1
		
		start_finally1:			
			finally1_out: endfinally
		end_finally1: 

		start_try1: 
			try1_out:
			leave OUT0
		end_try1: 

		OUT: ret
		
		OUT0:
		br start_try2
		
		start_finally2: 
			br finally2_br

			exception_filter1: 
				pop
				ldc.i4.1
			endfilter
			start_filter1: 
				pop
				br filter1_br
				start_fault1: 
					fault1_out: endfault
				end_fault1: 

				filter1_br: 
				start_try4:						
					try4_out:
					leave filter1_out
				end_try4: 

				filter1_out: leave finally2_out
			end_filter1: 

			finally2_br: 
			start_try3: 
				try3_out:
				newobj instance void [mscorlib]System.ArrayTypeMismatchException::.ctor()
				throw
			end_try3: 

			finally2_out: endfinally
		end_finally2: 

		start_try2: 
			try2_out:
			newobj instance void [mscorlib]System.ArgumentNullException::.ctor()
			throw
		end_try2: 
		OUT1:
		leave OUT
		.try start_try4 to end_try4 fault handler start_fault1 to end_fault1
		.try start_try3 to end_try3 filter exception_filter1 handler start_filter1 to end_filter1
		.try start_try2 to end_try2 finally handler start_finally2 to end_finally2
		.try start_try1 to end_try1 finally handler start_finally1 to end_finally1
	}

	.method public static int32 Main() cil managed
	{
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		.try
		{
		call void EHTest::f2()
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
