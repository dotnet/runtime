// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib {}
.assembly extern System {}
.assembly extern eh_common{}
.assembly ASSEMBLY_NAME {}
.assembly extern xunit.core {}
.namespace Test_nestedfinallycall {
.class public EHTest
{
	.field private static class [eh_common]TestUtil.TestLog testLog
  	.method private hidebysig specialname rtspecialname static void  .cctor() cil managed
    	{
      		.maxstack  2
      		.locals init (class [mscorlib]System.IO.StringWriter expectedOut)
      		newobj     instance void [mscorlib]System.IO.StringWriter::.ctor()
      		stloc.s    expectedOut
      		ldloc.s    expectedOut
      		ldstr      "in try1"
      		callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
      		ldloc.s    expectedOut
      		ldstr      "in finally1"
      		callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
      		ldloc.s    expectedOut
      		ldstr      "in try2"
      		callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
      		ldloc.s    expectedOut
      		ldstr      "in try3"
      		callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
      		ldloc.s    expectedOut
      		ldstr      "in try3 catch handler"
      		callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
      		ldloc.s    expectedOut
      		ldstr      "PASSED"
      		callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
      		ldloc.s    expectedOut
      		newobj     instance void [eh_common]TestUtil.TestLog::.ctor(object)
      		stsfld     class [eh_common]TestUtil.TestLog Test_nestedfinallycall.EHTest::testLog
      		ret
    } 
    
	.method public static void f2() cil managed synchronized
	{
		.maxstack 500 
		.locals init (int32 V_0)
		.try {
			ldstr "in try1"
			call void [System.Console]System.Console::WriteLine(string)
			leave OUT0

		} finally {
			ldstr "in finally1"
			call void [System.Console]System.Console::WriteLine(string)

			.try {
  			   	ldstr "in try2"
				call void [System.Console]System.Console::WriteLine(string)

				leave finally1_out
			
			} finally {

				.try { 
					ldstr "in try3"
					call void [System.Console]System.Console::WriteLine(string)

					newobj instance void [mscorlib]System.InvalidProgramException::.ctor()
					throw
				} catch [mscorlib]System.InvalidProgramException {

				      	nop
				     	ldstr "in try3 catch handler"
					call void [System.Console]System.Console::WriteLine(string)
                       			leave.s catch1_out

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
						 leave catch1_out
				catch1_out:

					leave finally2_out
				}
			finally2_out:
			        endfinally
			}
			end_finally2: 


		finally1_out: 
		        endfinally
		}

		OUT: ret
		OUT0:
		leave OUT
	}

	.method public static int32 Main() cil managed
	{
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		
		ldsfld     class [eh_common]TestUtil.TestLog Test_nestedfinallycall.EHTest::testLog
		callvirt   instance void [eh_common]TestUtil.TestLog::StartRecording()

		.try
		{
			call void Test_nestedfinallycall.EHTest::f2()
			leave RET
		}
		catch [mscorlib]System.Exception
		{
			leave RET
		}
		RET:
			ldstr "PASSED"
			call void [System.Console]System.Console::WriteLine(string)
			ldsfld     class [eh_common]TestUtil.TestLog Test_nestedfinallycall.EHTest::testLog
      		callvirt   instance void [eh_common]TestUtil.TestLog::StopRecording()
      
      		ldsfld     class [eh_common]TestUtil.TestLog Test_nestedfinallycall.EHTest::testLog
      		callvirt   instance int32 [eh_common]TestUtil.TestLog::VerifyOutput()
       		ret
		}
}
}
