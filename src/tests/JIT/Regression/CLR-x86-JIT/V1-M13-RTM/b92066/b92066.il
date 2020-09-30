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
.class public EHTest
{
	.method public static void f2() cil managed synchronized
	{
		.maxstack 500
		.locals init (int32 V_0)

		br start_try1

		start_finally1: 
			br try2_br
			
				start_finally2: 

					start_try3: 
					try3_out:
					newobj instance void [mscorlib]System.InvalidProgramException::.ctor()
					throw
					end_try3: 

					start_catch1: 
					      nop
						TARGET_5_0:
						  ldc.i4.8
						  stloc.0
						  ldc.i4.1
						  ldloca V_0
						  mkrefany   [mscorlib]System.Int32
						  refanyval	[mscorlib]System.Int32
						  ldind.i4
						  blt.un	TARGET_5_2
						TARGET_5_1:
						  ldc.i4.8
						  ldc.i4.8
						  ldc.i4 17
						  shl
						  bgt	TARGET_5_0
						TARGET_5_2:
						  ldc.i4.8
						  box int32
						  unbox int32
						  ldind.i4
						  ldc.i4.8
						  box int32
						  unbox int32
						  ldind.i4
						  bne.un	TARGET_5_0
					catch1_out:
					pop
					leave finally2_out
					end_catch1: 

				finally2_out: endfinally
				end_finally2: 

			try2_br:
				start_try2: 
				try2_out:
				leave finally1_out
				end_try2: 

		finally1_out: endfinally
		end_finally1: 

		start_try1: 
		try1_out:
		leave OUT0
		end_try1: 
		OUT: ret
		OUT0:
		leave OUT

		.try start_try3 to end_try3 catch [mscorlib]System.InvalidProgramException handler start_catch1 to end_catch1
		.try start_try2 to end_try2 finally handler start_finally2 to end_finally2
		.try start_try1 to end_try1 finally handler start_finally1 to end_finally1
	}

	.method public static int32 Main() cil managed
	{
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
		RET:
			ldc.i4 100
			ldstr "PASSED"
			call void [System.Console]System.Console::WriteLine(string)
			ret
		}

}
