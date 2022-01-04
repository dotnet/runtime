// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib {}
.assembly extern System {}
.assembly 'b487372' {}
.assembly extern xunit.core {}
.class public EHTest
{
  .field private static int64 fld0
  .field private static int32 fld1
  .field private static int32 fld2
  .field private static int32 fld3
  .field private static int32 fld4

.method public static int64 f1(int32 arg0,int64 arg1,int64 arg2,int32 arg3,int32 arg4) cil managed
{
.maxstack 500
.locals init (int32 V_0,
		int64 V_1,
		int32 V_2,
		int32 V_3)
br start_try1
start_try1: 
br try1_br
try1_br: 
start_try2: 
try2_out:
leave try1_out
end_try2: 

start_fault1: 
call void EHTest::f2()
fault1_out: endfault
end_fault1: 


nop
	TARGET_2_0:
	  ldc.i8 114
	  starg.s arg2
	  ldsfld int64 EHTest::fld0
	  stsfld int64 EHTest::fld0
	  ldsflda int64 EHTest::fld0
	  mkrefany   [mscorlib]System.Int64
	  refanyval	[mscorlib]System.Int64
	  ldind.i8
	  stloc.1
	  ldc.i8 17
	  stloc.1
	  ldc.i8 74
	  ldc.i8 58
	  ceq
	  newarr     [mscorlib]System.Int64
	  sizeof native int
	  ldelem.i8
	  ldc.i8 33
	  blt.un	TARGET_2_1
	TARGET_2_1:
	  ldc.i4 74
	  ldc.i4 29
	  cgt
	  starg.s arg3
	  ldc.i4 57
	  stsfld int32 EHTest::fld4
	  ldc.i4 107
	  ldc.i4 97
	  cgt
	  starg.s arg0
	  ldc.i4 60
	  ldc.i4 42
	  starg.s arg0
	  ldc.i4 70
	  beq	TARGET_2_3
	TARGET_2_2:
	  ldc.i4 76
	  starg.s arg3
	  ldc.i4 122
	  ldc.i4.1
	  stsfld int32 EHTest::fld3
	  ldc.i4 20
	  stsfld int32 EHTest::fld4
	  ldc.i4 67
	  ldc.i4 117
	  starg.s arg3
	  ldc.i4 99
	  ldc.i4 47
	  ldc.i4 121
	  ldc.i4 16
	  ldc.i4 101
	  ldc.i4 85
	  ldc.i4 77
	  stloc.3
	  ldc.i4 78
	  ldc.i4 121
	  ldc.i4 24
	  ldc.i4 31
	  stsfld int32 EHTest::fld4
	  ldc.i4 19
	  stsfld int32 EHTest::fld2
	  ldc.i4 57
	  stsfld int32 EHTest::fld4
	  ldc.i4 112
	  stsfld int32 EHTest::fld2
	  ldc.i4 55
	  ldc.i4.8
	  ldc.i4 15
	  ldc.i4 39
	  ldc.i4 58
	  ldc.i4 113
	  stloc.2
	  ldc.i4 102
	  stsfld int32 EHTest::fld1
	  ldc.i4 48
	  ldc.i4 78
	  ldc.i4 106
	  ldc.i4.2
	  ldc.i4 124
	  starg.s arg4
	  ldc.i4 73
	  ldc.i4 83
	  ldc.i4 59
	  ldc.i4 63
	  stloc.2
	  ldc.i4 63
	  ldc.i4 115
	  ldc.i4 105
	  ldc.i4 64
	  newobj instance void int32[86...,75...,82...,49...,7...,69...,6...,82...,17...,32...,100...,52...,21...,117...,41...,4...,32...,124...,7...,17...,48...,46...,20...,108...,116...,53...,125...]::.ctor(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4 17
	  ldc.i4 18
	  ldc.i4 109
	  ldc.i4 116
	  ldc.i4 89
	  ldc.i4.5
	  ldc.i4.2
	  starg.s arg3
	  ldc.i4.6
	  starg.s arg0
	  ldc.i4 33
	  ldc.i4 55
	  stloc.3
	  ldc.i4 98
	  ldc.i4 101
	  stloc.3
	  ldc.i4 51
	  ldc.i4 27
	  ldc.i4 79
	  starg.s arg3
	  ldc.i4 40
	  ldc.i4 53
	  ldc.i4 32
	  stsfld int32 EHTest::fld4
	  ldc.i4 22
	  ldc.i4 127
	  ldc.i4 109
	  starg.s arg4
	  ldc.i4.4
	  stsfld int32 EHTest::fld3
	  ldc.i4 122
	  stloc.2
	  ldc.i4 41
	  ldc.i4 23
	  ldc.i4 53
	  ldc.i4 21
	  ldc.i4 112
	  ldc.i4 98
	  stsfld int32 EHTest::fld2
	  ldc.i4 21
	  stsfld int32 EHTest::fld2
	  ldc.i4 104
	  ldc.i4 29
	  ldc.i4 28
	  stsfld int32 EHTest::fld4
	  ldc.i4.7
	  stloc.0
	  ldc.i4 123
	  stsfld int32 EHTest::fld3
	  ldc.i4 89
	  ldc.i4 114
	  starg.s arg0
	  ldc.i4 19
	  stsfld int32 EHTest::fld4
	  ldc.i4 83
	  ldc.i4 125
	  ldc.i4 46
	  stloc.2
	  ldc.i4 118
	  ldc.i4 38
	  ldc.i4 106
	  call instance int32 int32[86...,75...,82...,49...,7...,69...,6...,82...,17...,32...,100...,52...,21...,117...,41...,4...,32...,124...,7...,17...,48...,46...,20...,108...,116...,53...,125...]::Get(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  stloc.2
	  ldc.i4 33
	  ldc.i4 41
	  or
	  starg.s arg4
	  ldc.i4 30
	  ldc.i4 41
	  stloc.2
	  ldc.i4 94
	  or
	  ldc.i4 95
	  bge	TARGET_2_4
	TARGET_2_3:
	  ldloc.0
	  ldc.i4 23
	  stloc.0
	  ldc.i4 98
	  bgt.un	TARGET_2_2
	TARGET_2_4:
	  ldftn int32 EHTest::GetInt32_jmp()
	  calli int32 ()
	  neg
	  ldc.i4 70
	  starg.s arg4
	  ldc.i4 35
	  ble.un	TARGET_2_4
try1_out:
newobj instance void [mscorlib]System.OverflowException::.ctor()
throw
end_try1: 

start_fault2: 
nop
	TARGET_3_0:
	  ldloc.0
	  newarr     [mscorlib]System.Int32
	  ldc.i4.1
	  ldelem.i4
	  stloc.0
	  ldc.i4 9
	  ldc.i4 55
	  clt
	  stsfld int32 EHTest::fld1
	  ldc.i4.0
	  starg.s arg4
	  ldc.i4 67
	  ldc.i4 38
	  beq	TARGET_3_4
	TARGET_3_1:
	  ldarg.1
	  ldc.i8 37
	  ceq
	  stloc.0
	  ldc.i4 16
	  stsfld int32 EHTest::fld2
	  ldc.i4 54
	  ldc.i4 89
	  starg.s arg3
	  ldc.i4 115
	  stloc.0
	  ldc.i4.3
	  mul
	  ldc.i4 60
	  bge.un	TARGET_3_2
	TARGET_3_2:
	  ldc.i4 96
	  ldc.i4 117
	  div
	  starg.s arg3
	  ldc.i4 21
	  stsfld int32 EHTest::fld3
	  ldc.i4 108
	  stsfld int32 EHTest::fld4
	  ldc.i4 86
	  neg
	  ldc.i4 10
	  or
	  ldc.i4 92
	  stsfld int32 EHTest::fld1
	  ldc.i4 122
	  beq	TARGET_3_3
	TARGET_3_3:
	  ldarg.3
	  stsfld int32 EHTest::fld3
	  ldc.i4 56
	  neg
	  stloc.0
	  ldc.i4 13
	  stsfld int32 EHTest::fld1
	  ldc.i4 94
	  ldc.i4.1
	  add
	  ldc.i4 124
	  stloc.0
	  ldc.i4 27
	  rem
	  ldc.i4 76
	  ble	TARGET_3_4
	TARGET_3_4:
	  ldsflda int64 EHTest::fld0
	  mkrefany   [mscorlib]System.Int64
	  refanyval	[mscorlib]System.Int64
	  ldind.i8
	  stsfld int64 EHTest::fld0
	  ldc.i8 127
	  ldc.i8 24
	  or
	  starg.s arg2
	  ldc.i8 31
	  stsfld int64 EHTest::fld0
	  ldc.i8 58
	  ldc.i8 34
	  sub
	  stloc.1
	  ldc.i8 122
	  starg.s arg2
	  ldc.i8 98
	  ldc.i8 67
	  starg.s arg1
	  ldc.i8 102
	  rem
	  ldc.i8 78
	  ble	TARGET_3_1
fault2_out: endfault
end_fault2: 

OUT: 
	  ldc.i8 70
ret
OUT0:
leave OUT
.try start_try2 to end_try2 fault handler start_fault1 to end_fault1
.try start_try1 to end_try1 fault handler start_fault2 to end_fault2
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
		int64 V_2)
.try
{
	  ldc.i4 109
	  box int32
	  unbox int32
	  ldind.i4
	  stloc.0
	  ldc.i4 57
	  ldc.i4.8
	  add
	  ldc.i8 69
	  ldc.i8 4
	  ldc.i4 67
	  stsfld int32 EHTest::fld3
	  ldc.i4 120
	  ldc.i4 112
	  stloc.0
	  ldc.i4 78
call int64 EHTest::f1(int32 ,int64 ,int64 ,int32 ,int32 )
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
