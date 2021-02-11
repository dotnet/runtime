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

.method public static void f2() cil managed
{
.maxstack 500
.locals init (int32 V_0,
		int64 V_1)
br start_try1
start_fault1: 
	fault1_out: endfault
end_fault1: 

start_try1: 
	nop
	TARGET_2_0:
	TARGET_2_1:
	  ldc.i4.1
	  newarr     [mscorlib]System.Int64
	  ldloc 0
	  newarr     [mscorlib]System.Int32
	  ldloc.0
	  ldelem.i4
	  ldelem.i8
	  ldloc.1
	  bge.un	TARGET_2_0
	try1_out:
	  leave OUT0
end_try1: 

OUT: ret

OUT0:
br start_try2
start_fault2: 
	br fault2_br

	exception_filter3: 
		pop
		ldc.i4.1
		endfilter
	start_filter3: 
		pop
		br filter3_br

		start_catch4: 
			pop
			nop
			TARGET_3_1:
			  ldloc.1
			  ldloc.1
			  bgt	TARGET_3_2
			TARGET_3_2:
			TARGET_3_3:
			  ldloc 0
			  newarr     [mscorlib]System.Int64
			  ldloc 1
			  ldc.i8 27
			  ceq
			  ldelem.i8
			  stloc 1
			  ldloc 0
			  ldloc 0
			  bge	TARGET_3_1
			leave filter3_out
		end_catch4: 

	filter3_br: 
		start_try4: 
			try4_out:
			newobj instance void [mscorlib]System.ArgumentNullException::.ctor()
			throw
		end_try4: 

	filter3_out: leave fault2_out
	end_filter3: 

	fault2_br: 
	start_try3: 
		try3_out:
		newobj instance void [mscorlib]System.InvalidOperationException::.ctor()
		throw
	end_try3: 
	fault2_out: endfault
end_fault2: 

start_try2: 
	try2_out:
	newobj instance void [mscorlib]System.DllNotFoundException::.ctor()
	throw
end_try2: 
OUT1:
leave OUT
.try start_try4 to end_try4 catch [mscorlib]System.Exception handler start_catch4 to end_catch4
.try start_try3 to end_try3 filter exception_filter3 handler start_filter3 to end_filter3
.try start_try2 to end_try2 fault handler start_fault2 to end_fault2
.try start_try1 to end_try1 fault handler start_fault1 to end_fault1
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
