// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib {}
.assembly extern System {}
.assembly 'b92289' {}
.assembly extern xunit.core {}
.class public EHTest
{
.method public static void f2() cil managed synchronized
{
	.maxstack 500
	.locals init (int32 V_0,
			int64 V_1)
	br start_try1
		start_fault1: 
		br fault1_br
			start_finally1: 
				TARGET_1_0:
				  sizeof native unsigned int
				  sizeof [mscorlib]System.UIntPtr
				  conv.i4
				  stloc.0
				  ldloc.0
				  sub
				  newarr     [mscorlib]System.Int32
				  ldc.i4 112
				  ldc.i4 41
				  sub
				  ldc.i4 3
				  shr
				  stloc.0
				  ldloc.0
				  ldc.i4 82
				  sub
				  ldelem.i4
				  ldc.i4 26
				  box int32
				  unbox int32
				  ldind.i4
				  bge	TARGET_1_1
				TARGET_1_1:
			finally1_out: endfinally
			end_finally1: 

			fault1_br: 
			start_try2: 
			call void EHTest::f3()
			try2_out:
			leave fault1_out
			end_try2: 
		fault1_out: endfault
		end_fault1: 

	start_try1: 
		call void EHTest::f3()
	try1_out:
	newobj instance void [mscorlib]System.Exception::.ctor()
	throw
	end_try1: 
	OUT: ret
	OUT2:
	leave OUT
	.try start_try2 to end_try2 finally handler start_finally1 to end_finally1
	.try start_try1 to end_try1 fault handler start_fault1 to end_fault1
}

.method public static void f3() cil managed synchronized
{
	call void [mscorlib]System.GC::Collect()
	ret
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
