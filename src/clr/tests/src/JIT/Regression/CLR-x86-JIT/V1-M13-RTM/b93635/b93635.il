// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
.method public static void f2() cil managed
{
	.maxstack 500
	.locals init (int32 V_0,
			int64 V_1)

	br start_try2

	start_catch2: 
		catch2_out:
		  nop
		TARGET_5_0:
		  ldc.i4 25
		  ldc.i4 66
		  bge	TARGET_5_0
		TARGET_5_1:
		  ldc.i8 45
		  ldc.i8 44
		  beq	TARGET_5_1
		TARGET_5_2:
		  ldc.i4 21
		  ldc.i4 53
		  ldc.i4 101
		  ldc.i4 13
		  ldc.i4 84
		  ldc.i4 119
		  newobj instance void int32[...,...,...,...,...,...]::.ctor(int32,int32,int32,int32,int32,int32)
		  ldc.i4 89
		  ldc.i4 55
		  ldc.i4 110
		  ldc.i4 98
		  ldc.i4 56
		  ldc.i4 38
		  call instance int32 int32[...,...,...,...,...,...]::Get(int32,int32,int32,int32,int32,int32)
		  ldc.i4 106
		  blt	TARGET_5_1
		TARGET_5_3:
		  ldc.i4 85
		  ldc.i4.2
		  ble	TARGET_5_3
		pop
		newobj instance void [mscorlib]System.SystemException::.ctor()
		throw
	end_catch2: 

	start_try2: 
		call void [mscorlib]System.GC::Collect()
		try2_out:
		leave OUT1
	end_try2: 
	OUT1: ret
	
	.try start_try2 to end_try2 catch [mscorlib]System.InvalidOperationException handler start_catch2 to end_catch2
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
