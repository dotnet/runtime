// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib { auto }
.assembly extern System { auto }
.assembly EHTest {}
.class public EHTest
{
  .field private static int32 fld0
  .field private static int32 fld1

  .method public static int32 GetInt32()
  {
	ldc.i4 180861962
	ret
  }
  .method public static int64 GetInt64()
  {
	ldc.i4 1679228860
	conv.i8
	ret
  }
   .method public static int32 GetInt32_tailcall()
  {
  	tail. call   int32 EHTest::GetInt32()
  	ret
  }
   .method public static int64 GetInt64_tailcall()
  {
  	tail. call   int64 EHTest::GetInt64()
  	ret
  }
.method public static int32 f0(int64 arg0,int32 arg1) cil managed
{
.maxstack 500
.locals init (int32 V_0,
		int64 V_1,
		int64 V_2,
		int64 V_3,
		int32 V_4,
		int32 V_5)
br start_try1
start_finally1: 
finally1_out: endfinally
end_finally1:


start_try1: 
try1_out:
leave OUT0
end_try1:

OUT: 
	  ldc.i4 56
ret
OUT0:
leave OUT
.try start_try1 to end_try1 finally handler start_finally1 to end_finally1
}

.method public static int64 f1(int32 arg0,int32 arg1,int32 arg2) cil managed
{
.maxstack 500
.locals init (int32 V_0,
		int64 V_1,
		int32 V_2,
		int64 V_3,
		int64 V_4)
br start_try1
start_fault1: 
fault1_out: endfault
end_fault1:


start_try1: 
try1_out:
leave OUT0
end_try1:

OUT: 
	  ldc.i8 77
	  stloc.1
	  ldc.i8 110
ret
OUT0:
nop
	TARGET_0_0:
	  ldc.i4 112
	  ldc.i4 74
	  rem
	  stsfld int32 EHTest::fld1
	  ldc.i4 98
	  ldc.i4 88
	  stloc.2
	  ldc.i4 106
	  bgt.un	TARGET_0_4
	TARGET_0_1:
	  call int64 EHTest::GetInt64_tailcall()
	  ldc.i8 67
	  sub
	  ldc.i8 74
	  div
	  ldc.i8 114
	  stloc 4
	  ldc.i8 94
	  add
	  ldc.i8 103
	  ble.un	TARGET_0_1
	TARGET_0_2:
	  ldsfld int32 EHTest::fld1
	  ldc.i4 28
	  ldc.i4 92
	  stsfld int32 EHTest::fld1
	  ldc.i4.1
	  ldc.i4 65
	  ldc.i4 67
	  ldc.i4 68
	  stloc.0
	  ldc.i4 90
	  stsfld int32 EHTest::fld0
	  ldc.i4 42
	  ldc.i4 30
	  stloc.2
	  ldc.i4 23
	  ldc.i4 96
	  ldc.i4.6
	  ldc.i4 67
	  stsfld int32 EHTest::fld1
	  ldc.i4 72
	  ldc.i4 43
	  starg.s arg2
	  ldc.i4 60
	  ldc.i4 60
	  stsfld int32 EHTest::fld1
	  ldc.i4 50
	  ldc.i4 66
	  ldc.i4 23
	  starg.s arg1
	  ldc.i4 10
	  stloc.0
	  ldc.i4.6
	  ldc.i4 43
	  ldc.i4 100
	  starg.s arg1
	  ldc.i4 53
	  starg.s arg2
	  ldc.i4 31
	  newobj instance void int64[5...,95...,105...,103...,45...,12...,50...,110...,17...,101...,80...,45...,11...,74...,68...,20...]::.ctor(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4 96
	  stsfld int32 EHTest::fld0
	  ldc.i4 19
	  ldc.i4 59
	  ldc.i4 44
	  stloc.0
	  ldc.i4 111
	  ldc.i4 109
	  ldc.i4 77
	  stloc.0
	  ldc.i4 53
	  ldc.i4 122
	  stsfld int32 EHTest::fld0
	  ldc.i4 82
	  ldc.i4 58
	  ldc.i4 80
	  stsfld int32 EHTest::fld0
	  ldc.i4 59
	  stloc.2
	  ldc.i4 13
	  ldc.i4 119
	  ldc.i4 38
	  ldc.i4 59
	  ldc.i4 83
	  ldc.i4 123
	  stloc.2
	  ldc.i4 79
	  ldc.i4 103
	  ldc.i4 32
	  ldc.i4 100
	  starg.s arg1
	  ldc.i4 125
	  call instance int64 int64[5...,95...,105...,103...,45...,12...,50...,110...,17...,101...,80...,45...,11...,74...,68...,20...]::Get(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i8 121
	  ldc.i8 43
	  add
	  ldc.i8 37
	  stloc 4
	  ldc.i8 83
	  add
	  neg
	  bgt.un	TARGET_0_1
	TARGET_0_3:
	  sizeof [mscorlib]System.UIntPtr
	  stloc.2
	  ldsfld int32 EHTest::fld1
	  ldc.i4 67
	  newarr     [mscorlib]System.Int32
	  ldc.i4 120
	  stloc.0
	  ldc.i4 126
	  ldelem.i4
	  stsfld int32 EHTest::fld0
	  ldc.i4 115
	  ldc.i4 116
	  starg.s arg2
	  ldc.i4 78
	  or
	  stloc.0
	  ldc.i4 50
	  ldc.i4 41
	  ldc.i4 116
	  stsfld int32 EHTest::fld0
	  ldc.i4 103
	  ldc.i4 26
	  stloc.0
	  ldc.i4 98
	  stsfld int32 EHTest::fld0
	  ldc.i4 78
	  ldc.i4 71
	  ldc.i4 86
	  stsfld int32 EHTest::fld1
	  ldc.i4 104
	  starg.s arg2
	  ldc.i4.1
	  starg.s arg2
	  ldc.i4 116
	  stloc.0
	  ldc.i4 81
	  ldc.i4 40
	  stsfld int32 EHTest::fld1
	  ldc.i4 100
	  stsfld int32 EHTest::fld1
	  ldc.i4 25
	  ldc.i4 53
	  ldc.i4 26
	  ldc.i4.7
	  stloc.0
	  ldc.i4 27
	  ldc.i4 67
	  stsfld int32 EHTest::fld0
	  ldc.i4 39
	  ldc.i4 111
	  starg.s arg1
	  ldc.i4 84
	  starg.s arg0
	  ldc.i4 108
	  stloc.0
	  ldc.i4 121
	  stloc.2
	  ldc.i4 75
	  starg.s arg1
	  ldc.i4 96
	  ldc.i4 89
	  ldc.i4 119
	  ldc.i4 32
	  ldc.i4 20
	  stloc.2
	  ldc.i4 55
	  ldc.i4 97
	  newobj instance void int64[74...,50...,108...,89...,75...,25...,31...,78...,106...,110...,37...,54...,45...,53...,0...,48...,42...,22...]::.ctor(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4 74
	  ldc.i4 116
	  stsfld int32 EHTest::fld0
	  ldc.i4 110
	  ldc.i4 123
	  stsfld int32 EHTest::fld1
	  ldc.i4 118
	  ldc.i4 117
	  ldc.i4 97
	  stloc.2
	  ldc.i4 103
	  stsfld int32 EHTest::fld0
	  ldc.i4 13
	  ldc.i4.6
	  ldc.i4 103
	  stsfld int32 EHTest::fld0
	  ldc.i4 104
	  ldc.i4 58
	  ldc.i4 16
	  stsfld int32 EHTest::fld0
	  ldc.i4.6
	  ldc.i4 44
	  ldc.i4 126
	  ldc.i4 14
	  stsfld int32 EHTest::fld0
	  ldc.i4 24
	  stsfld int32 EHTest::fld1
	  ldc.i4 40
	  ldc.i4 55
	  stsfld int32 EHTest::fld1
	  ldc.i4 31
	  ldc.i4 78
	  stloc.2
	  ldc.i4 62
	  starg.s arg2
	  ldc.i4 102
	  ldc.i4 116
	  ldc.i4 62
	  stloc.0
	  ldc.i4 48
	  ldc.i4 36
	  ldc.i4 66
	  call instance int64 int64[74...,50...,108...,89...,75...,25...,31...,78...,106...,110...,37...,54...,45...,53...,0...,48...,42...,22...]::Get(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  stloc 4
	  ldc.i8 111
	  stloc 4
	  ldc.i8 59
	  not
	  ldc.i8 47
	  stloc.1
	  ldc.i8 48
	  bge.un	TARGET_0_1
	TARGET_0_4:
	  ldc.i8 26
	  stloc 4
	  ldc.i8 50
	  ldc.i8 101
	  ldc.i8 56
	  xor
	  rem
	  ldc.i8 48
	  stloc.1
	  ldc.i8 118
	  blt	TARGET_0_0
br start_try2
start_fault2: 
br fault2_br
fault2_br: 
start_try3: 
br try3_br
start_finally1: 
nop
	TARGET_1_0:
	  ldc.i8 108
	  ldc.i8 4
	  or
	  ldc.i8 36
	  add
	  stloc.3
	  ldc.i8 26
	  ldc.i8 47
	  stloc 4
	  ldc.i8 84
	  stloc 4
	  ldc.i8 91
	  stloc.3
	  ldc.i8 25
	  stloc.3
	  ldc.i8 114
	  xor
	  ldc.i8 91
	  bgt	TARGET_1_3
	TARGET_1_1:
	  ldc.i8 72
	  stloc.3
	  ldc.i8 94
	  ldc.i8 1
	  or
	  stloc.3
	  ldc.i8 42
	  ldc.i8 25
	  bge.un	TARGET_1_1
	TARGET_1_2:
	  call int32 EHTest::GetInt32_tailcall()
	  ldc.i4 54
	  neg
	  bgt.un	TARGET_1_3
	TARGET_1_3:
ldc.i4 0
	  ldc.i4 15
	  stsfld int32 EHTest::fld1
	  ldc.i4 11
	  starg.s arg1
	  ldc.i4 85
	  stsfld int32 EHTest::fld1
	  ldc.i4.1
	  bgt.un	TARGET_1_2
	TARGET_1_4:
	  ldc.i4 68
	  not
	  newarr     [mscorlib]System.Int32
	  ldc.i4 122
	  stloc.2
	  ldc.i4 53
	  ldelem.i4
	  stloc.0
	  ldc.i4 126
	  ldc.i4 112
	  stsfld int32 EHTest::fld1
	  ldc.i4 67
	  stloc.0
	  ldc.i4.8
	  cgt
	  ldc.i4.5
	  ceq
	  ldc.i4 23
	  starg.s arg0
	  ldc.i4 91
	  blt.un	TARGET_1_1
finally1_out: endfinally
end_finally1:


try3_br: 
start_try4: 
try4_out:
leave try3_out
end_try4:

call void EHTest::f2()
try3_out:
leave fault2_out
end_try3:

start_finally2: 
nop
	TARGET_2_0:
	  call int64 EHTest::GetInt64()
	  stloc 4
	  ldc.i8 96
	  ldc.i8 46
	  ldc.i8 100
	  div
	  blt	TARGET_2_4
	TARGET_2_1:
	  ldc.i4 11
	  ldc.i4 112
	  mul
	  ldc.i4 38
	  blt.un	TARGET_2_3
	TARGET_2_2:
ldc.i4 0
	  ldc.i4 26
	  shl
	  ldc.i4 15
	  stloc.0
	  ldc.i4 87
	  ldc.i4 50
	  or
	  bgt	TARGET_2_3
	TARGET_2_3:
	  ldc.i8 19
	  stloc.3
	  ldc.i8 2
	  ldc.i8 93
	  or
	  stloc 4
	  ldc.i8 60
	  ldc.i8 75
	  bgt	TARGET_2_4
	TARGET_2_4:
	  sizeof native unsigned int
	  ldc.i4 120
	  starg.s arg1
	  ldc.i4 13
	  neg
	  stloc.0
	  ldc.i4 61
	  beq	TARGET_2_4
finally2_out: endfinally
end_finally2:


fault2_out: endfault
end_fault2:


start_try2: 
nop
	TARGET_3_0:
	  ldloc.3
	  ldloca 4
	  mkrefany   [mscorlib]System.Int64
	  refanyval	[mscorlib]System.Int64
	  ldind.i8
	  ldc.i8 39
	  rem
	  bgt.un	TARGET_3_2
	TARGET_3_1:
	  ldsflda int32 EHTest::fld0
	  mkrefany   [mscorlib]System.Int32
	  refanyval	[mscorlib]System.Int32
	  ldind.i4
	  newobj instance void int32[21...]::.ctor(int32)
	  ldc.i4 80
	  call instance int32 int32[21...]::Get(int32)
	  ldc.i4.8
	  stsfld int32 EHTest::fld1
	  ldc.i4 104
	  starg.s arg2
	  ldc.i4 10
	  sub
	  ldc.i4 37
	  bgt.un	TARGET_3_2
	TARGET_3_2:
ldc.i4 0
	  ldc.i4 36
	  blt	TARGET_3_0
	TARGET_3_3:
	  ldc.i8 98
	  stloc.1
	  ldloca 1
	  mkrefany   [mscorlib]System.Int64
	  refanyval	[mscorlib]System.Int64
	  ldind.i8
	  ldc.i8 30
	  conv.i8
	  ldc.i8 17
	  or
	  bgt.un	TARGET_3_0
	TARGET_3_4:
	  ldc.i4 123
	  stloc.2
	  ldc.i4 43
	  ldc.i4 82
	  bge.un	TARGET_3_4
try2_out:
newobj instance void [mscorlib]System.SystemException::.ctor()
throw
end_try2:

OUT1:
nop
	TARGET_4_0:
	  ldc.i8 70
	  stloc 4
	  ldc.i8 11
	  not
	  ldc.i8 26
	  blt.un	TARGET_4_3
	TARGET_4_1:
	  ldc.i8 97
	  stloc.3
	  ldc.i8 17
	  not
	  ldc.i8 115
	  blt	TARGET_4_3
	TARGET_4_2:
	  ldc.i4 89
	  ldc.i4 25
	  stloc.2
	  ldc.i4 35
	  not
	  stsfld int32 EHTest::fld0
	  ldc.i4 99
	  beq	TARGET_4_2
	TARGET_4_3:
	  ldc.i4 17
	  stsfld int32 EHTest::fld0
	  ldc.i4 125
	  ldc.i4 13
	  shl
	  starg.s arg2
	  ldc.i4 39
	  starg.s arg2
	  ldc.i4 102
	  ldc.i4 72
	  stloc.2
	  ldc.i4 34
	  bge.un	TARGET_4_4
	TARGET_4_4:
	  ldc.i4 101
	  not
	  ldc.i4 75
	  mul
	  ldc.i4 118
	  bge.un	TARGET_4_0
leave OUT
.try start_try4 to end_try4 finally handler start_finally1 to end_finally1
.try start_try3 to end_try3 finally handler start_finally2 to end_finally2
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
.entrypoint
.maxstack 500
.maxstack 500
.locals init (int32 V_0,
		int64 V_1,
		int32 V_2)
.try
{
	  ldc.i4 124
	  stloc.0
	  ldc.i4 124
	  ldc.i4 57
	  stloc.2
	  ldc.i4 116
	  ldc.i4 19
	  stloc.2
	  ldc.i4 110
call int64 EHTest::f1(int32 ,int32 ,int32 )
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
