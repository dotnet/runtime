// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly EHTest {}
.class public EHTest
{
	.method public static void f2() cil managed
	{
		.maxstack 500
		.locals init (int32 V_0,
				int64 V_1)
		br start_try1

		start_catch1: 
			nop	
			pop
			leave OUT
		end_catch1: //

		start_try1: 
			br try1_br

			start_catch2: 
				pop
				nop
				TARGET_7_0:
				  ldc.i8 81
				  neg
				  ldc.i8 34
				  or
				  ldc.i8 94
				  ble	TARGET_7_1
				TARGET_7_1:
				  ldc.i8 81
				  ldc.i4 46
				  newarr     [mscorlib]System.Int64
				  ldc.i4 45
				  ldc.i4.1
				  ldc.i4 84
				  ldc.i4 9
				  ldc.i4 114
				  ldc.i4 81
				  ldc.i4 120
				  newobj instance void int32[...,...,...,...,...,...,...]::.ctor(int32,int32,int32,int32,int32,int32,int32)
				  ldc.i4 75
				  ldc.i4 30
				  ldc.i4 68
				  ldc.i4 63
				  ldc.i4 97
				  ldc.i4 79
				  ldc.i4 90
				  call instance int32 int32[...,...,...,...,...,...,...]::Get(int32,int32,int32,int32,int32,int32,int32)
				  ldelem.i8
				  clt
				  ldc.i4 57
				  ble.un	TARGET_7_4
				TARGET_7_2:
				  ldc.i4 21
				  ldc.i4 51
				  add
				  ldc.i4 72
				  blt.un	TARGET_7_2
				TARGET_7_3:
				  ldc.i8 81
				  ldc.i8 115
				  ldc.i8 113
				  add
				  rem
				  ldc.i8 72
				  bge.un	TARGET_7_4
				TARGET_7_4:
				  ldc.i4 93
				  ldc.i4 107
				  sub
				  ldc.i4 76
				  ble	TARGET_7_3
				newobj instance void [mscorlib]System.MissingFieldException::.ctor()
				throw
			end_catch2: //

		try1_br: 
			start_try2: 
				newobj instance void [mscorlib]System.NotSupportedException::.ctor()
				throw
			end_try2: //
			
			leave OUT
		end_try1: //

		OUT: ret

		.try start_try2 to end_try2 catch [mscorlib]System.Exception handler start_catch2 to end_catch2
		.try start_try1 to end_try1 catch [mscorlib]System.FieldAccessException handler start_catch1 to end_catch1
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
