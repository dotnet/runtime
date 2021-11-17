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
  .field private static int64 fld0

.method public static int32 f1(int64 arg0,int64 arg1,int64 arg2,int32 arg3,int64 arg4) cil managed
{
.maxstack 500
.locals init (int32 V_0,
		int64 V_1,
		int64 V_2,
		int32 V_3)
br start_try1
start_fault1: 
nop
fault1_out: endfault
end_fault1: 


start_try1: 
br try1_br

start_catch2: 
catch2_out:
nop
	TARGET_7_0:
	  ldc.i4 23
	  box int32
	  unbox int32
	  ldind.i4
	  starg.s arg3
	  ldc.i4 61
	  neg
	  stloc.0
	  ldc.i4 56
	  newarr     [mscorlib]System.Int64
	  ldc.i4 108
	  ldelem.i8
	  starg.s arg2
	TARGET_7_1:
	  ldc.i8 1
	  ldc.i8 51
	  mul
	  ldc.i8 97
	  bgt	TARGET_7_1
rethrow
end_catch2: 


exception_filter1: 
pop
ldc.i4.0
endfilter
start_filter1: 
pop
nop

filter1_out: leave try1_out
end_filter1: 

try1_br: 
start_try2: 
br try2_br
try2_br: 

nop

try2_out:
newobj instance void [mscorlib]System.InvalidCastException::.ctor()
throw
end_try2: 

exception_filter2: 
pop
ldc.i4.1
endfilter
start_filter2: 
pop
filter2_out: leave try1_out
end_filter2: 

exception_filter3: 
pop
ldc.i4.0
endfilter
start_filter3: 
pop
filter3_out: leave try1_out
end_filter3: 

call void EHTest::f2()
try1_out:
newobj instance void [mscorlib]System.AppDomainUnloadedException::.ctor()
throw
end_try1: 

OUT0:
nop

br start_try4

start_try4: 
end_try4: 
OUT1:
leave OUT
OUT: 
	  ldc.i4 77
	  ldc.i4 78
	  stloc.3
	  ldc.i4 85
	  sub
ret

.try start_try2 to end_try2 catch [mscorlib]System.Exception handler start_catch2 to end_catch2
.try start_try2 to end_try2 filter exception_filter1 handler start_filter1 to end_filter1
.try start_try2 to end_try2 filter exception_filter2 handler start_filter2 to end_filter2
.try start_try2 to end_try2 filter exception_filter3 handler start_filter3 to end_filter3
.try start_try1 to end_try1 fault handler start_fault1 to end_fault1
}

.method public static void f2()
{
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
		int32 V_2,
		int32 V_3,
		int64 V_4,
		int64 V_5,
		int32 V_6)
.try
{
	  ldc.i8 76
	  ldc.i8 31
	  ldc.i8 115
	  stsfld int64 EHTest::fld0
	  ldc.i8 41
	  ldc.i4 12
	  stloc.2
	  ldc.i4 96
	  ldc.i8 28
call int32 EHTest::f1(int64 ,int64 ,int64 ,int32 ,int64 )
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
