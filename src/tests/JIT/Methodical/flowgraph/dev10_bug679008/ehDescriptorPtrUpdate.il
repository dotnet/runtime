// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib { auto }
.assembly extern System { auto }
.assembly 'ehDescriptorPtrUpdate' {}
.assembly extern xunit.core {}
.namespace Test_ehDescriptorPtrUpdate {
.class public EHTest
{

  .method public static int32 GetInt32()
  {
	ldc.i4 135258854
	ret
  }
  .method public static int64 GetInt64()
  {
	ldc.i4 669963539
	conv.i8
	ret
  }
  .method public static int32 GetInt32_tailcall()
  {
  	tail. call   int32 Test_ehDescriptorPtrUpdate.EHTest::GetInt32()
  	ret
  }
  .method public static int64 GetInt64_tailcall()
  {
  	tail. call   int64 Test_ehDescriptorPtrUpdate.EHTest::GetInt64()
  	ret
  }

.method public static int32 f1() cil managed
{
.maxstack 500
.locals init (int32 V_0,
		int64 V_1,
		int32 V_2,
		int64 V_3,
		int64 V_4,
		int32 V_5,
		int32 V_6)
br start_try1
start_fault1: 
fault1_out: endfault
end_fault1: 


start_try1: 
nop
	TARGET_2_0:
	TARGET_2_1:
	TARGET_2_2:
	TARGET_2_3:
	TARGET_2_4:
	  ldc.i8 1
	  ldftn int32 Test_ehDescriptorPtrUpdate.EHTest::GetInt32_tailcall()
	  calli int32 ()
	  newarr     [mscorlib]System.Int64
	  ldc.i4 24
     stloc 6
     ldloc 6
	  conv.i4
	  ldc.i4 62
	  rem
	  ldelem.i8
	  rem
	  ldc.i8 23
	  bgt	TARGET_2_4
try1_out:
leave OUT0
end_try1: 

OUT0:
call void Test_ehDescriptorPtrUpdate.EHTest::f2()
br start_try2
start_fault2: 
br fault2_br
start_catch1: 
catch1_out:
call void Test_ehDescriptorPtrUpdate.EHTest::f2()
pop
newobj instance void [mscorlib]System.ArithmeticException::.ctor()
throw
end_catch1: 


start_catch2: 
pop
br catch2_br
catch2_br: 
start_try4: 
try4_out:
newobj instance void [mscorlib]System.ArgumentNullException::.ctor()
throw
end_try4: 

start_finally1: 
finally1_out: endfinally
end_finally1: 


catch2_out:
call void Test_ehDescriptorPtrUpdate.EHTest::f2()
newobj instance void [mscorlib]System.StackOverflowException::.ctor()
throw
end_catch2: 


exception_filter1: 
pop
ldc.i4.1
endfilter
start_filter1: 
pop
filter1_out: leave fault2_out
end_filter1: 

fault2_br: 
start_try3: 
call void Test_ehDescriptorPtrUpdate.EHTest::f2()
try3_out:
leave fault2_out
end_try3: 

exception_filter2: 
pop
ldc.i4.0
endfilter
start_filter2: 
pop
call void Test_ehDescriptorPtrUpdate.EHTest::f2()
filter2_out: leave fault2_out
end_filter2: 

exception_filter3: 
pop
ldc.i4.0
endfilter
start_filter3: 
pop
call void Test_ehDescriptorPtrUpdate.EHTest::f2()
filter3_out: leave fault2_out
end_filter3: 

call void Test_ehDescriptorPtrUpdate.EHTest::f2()
fault2_out: endfault
end_fault2: 


start_try2: 
call void Test_ehDescriptorPtrUpdate.EHTest::f2()
try2_out:
newobj instance void [mscorlib]System.ArgumentOutOfRangeException::.ctor()
throw
end_try2: 
OUT: 
	  ldc.i4.8
ret
OUT1:
leave OUT
.try start_try4 to end_try4 finally handler start_finally1 to end_finally1
.try start_try3 to end_try3 catch [mscorlib]System.MethodAccessException handler start_catch1 to end_catch1
.try start_try3 to end_try3 catch [mscorlib]System.ArithmeticException handler start_catch2 to end_catch2
.try start_try3 to end_try3 filter exception_filter1 handler start_filter1 to end_filter1
.try start_try3 to end_try3 filter exception_filter2 handler start_filter2 to end_filter2
.try start_try3 to end_try3 filter exception_filter3 handler start_filter3 to end_filter3
.try start_try2 to end_try2 fault handler start_fault2 to end_fault2
.try start_try1 to end_try1 fault handler start_fault1 to end_fault1
}

.method public static void f2() cil managed
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
.maxstack 500
.maxstack 500
.locals init (int32 V_0,
		int64 V_1,
		int64 V_2,
		int32 V_3,
		int32 V_4)
.try
{
call int32 Test_ehDescriptorPtrUpdate.EHTest::f1()
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
}
