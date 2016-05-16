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
  .field private static int64 fld0
  .field private static int64 fld1
  .field private static int32 fld2
  .field private static int64 fld3
  .field private static int64 fld4
  .field private static int32 fld5

  .method public static int32 GetInt32()
  {
	ldc.i4 1136776502
	ret
  }
  .method public static int64 GetInt64()
  {
	ldc.i4 1108205630
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
.method public static int64 f0(int64 arg0,int32 arg1,int64 arg2) cil managed
{
.maxstack 500
.locals init (int32 V_0,
		int64 V_1,
		int64 V_2)
br start_try1
start_finally1: 
finally1_out: endfinally
end_finally1:


start_try1: 
br try1_br
try1_br: 
start_try2: 
call void EHTest::f2()
try2_out:
newobj instance void [mscorlib]System.CannotUnloadAppDomainException::.ctor()
throw
end_try2:

start_fault1: 
br fault1_br
start_finally2: 
finally2_out: endfinally
end_finally2:


fault1_br: 
start_try3: 
nop
	TARGET_0_0:
	  ldc.i4 94
	  starg.s arg1
	  ldc.i4 118
	  stsfld int32 EHTest::fld2
	  ldc.i4 33
	  ldc.i4 64
	  or
	  ldc.i4 95
	  starg.s arg1
	  ldc.i4 12
	  or
	  ldc.i4 12
	  ble	TARGET_0_2
	TARGET_0_1:
	  ldc.i4 63
	  ldc.i4 64
	  stloc.0
	  ldc.i4.5
	  stsfld int32 EHTest::fld5
	  ldc.i4 75
	  ldc.i4 52
	  starg.s arg1
	  ldc.i4 99
	  starg.s arg1
	  ldc.i4 16
	  ldc.i4 64
	  stsfld int32 EHTest::fld5
	  ldc.i4 67
	  ldc.i4 38
	  stsfld int32 EHTest::fld5
	  ldc.i4 100
	  ldc.i4 32
	  stsfld int32 EHTest::fld5
	  ldc.i4 63
	  ldc.i4 121
	  ldc.i4 80
	  ldc.i4 74
	  ldc.i4 51
	  ldc.i4 37
	  ldc.i4 43
	  stsfld int32 EHTest::fld2
	  ldc.i4 88
	  ldc.i4 67
	  ldc.i4 110
	  stloc.0
	  ldc.i4 93
	  stsfld int32 EHTest::fld5
	  ldc.i4 35
	  ldc.i4 69
	  starg.s arg1
	  ldc.i4 30
	  stsfld int32 EHTest::fld5
	  ldc.i4 117
	  ldc.i4 68
	  stsfld int32 EHTest::fld5
	  ldc.i4 88
	  ldc.i4 32
	  ldc.i4 55
	  starg.s arg1
	  ldc.i4 115
	  ldc.i4.5
	  ldc.i4 87
	  ldc.i4 12
	  ldc.i4 87
	  ldc.i4 100
	  stloc.0
	  ldc.i4 22
	  ldc.i4 68
	  stsfld int32 EHTest::fld5
	  ldc.i4 108
	  ldc.i4 81
	  ldc.i4 54
	  ldc.i4 32
	  ldc.i4 94
	  ldc.i4 91
	  starg.s arg1
	  ldc.i4 125
	  starg.s arg1
	  ldc.i4 19
	  ldc.i4 121
	  stloc.0
	  ldc.i4 56
	  newobj instance void int32[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]::.ctor(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4.8
	  ldc.i4.6
	  ldc.i4 38
	  ldc.i4 71
	  starg.s arg1
	  ldc.i4 63
	  stsfld int32 EHTest::fld5
	  ldc.i4 40
	  ldc.i4.5
	  starg.s arg1
	  ldc.i4 108
	  ldc.i4.7
	  ldc.i4 68
	  starg.s arg1
	  ldc.i4 56
	  ldc.i4 26
	  stloc.0
	  ldc.i4 43
	  stloc.0
	  ldc.i4 124
	  starg.s arg1
	  ldc.i4 98
	  ldc.i4.2
	  stsfld int32 EHTest::fld5
	  ldc.i4 94
	  stsfld int32 EHTest::fld5
	  ldc.i4 82
	  stsfld int32 EHTest::fld5
	  ldc.i4 38
	  ldc.i4 72
	  starg.s arg1
	  ldc.i4 9
	  stloc.0
	  ldc.i4 85
	  ldc.i4 113
	  ldc.i4 74
	  ldc.i4 75
	  ldc.i4.5
	  ldc.i4 110
	  stloc.0
	  ldc.i4 84
	  ldc.i4 36
	  ldc.i4 69
	  ldc.i4 98
	  ldc.i4.7
	  starg.s arg1
	  ldc.i4 18
	  stloc.0
	  ldc.i4 82
	  ldc.i4 114
	  stloc.0
	  ldc.i4 75
	  stsfld int32 EHTest::fld2
	  ldc.i4 108
	  ldc.i4 61
	  ldc.i4 94
	  starg.s arg1
	  ldc.i4 87
	  ldc.i4 12
	  stloc.0
	  ldc.i4 126
	  stloc.0
	  ldc.i4 72
	  ldc.i4 16
	  ldc.i4 39
	  ldc.i4 39
	  ldc.i4 53
	  starg.s arg1
	  ldc.i4 89
	  ldc.i4 90
	  stsfld int32 EHTest::fld2
	  ldc.i4 47
	  ldc.i4 120
	  ldc.i4 47
	  stloc.0
	  ldc.i4 95
	  call instance int32 int32[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]::Get(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  not
	  neg
	  neg
	  ldc.i4 81
	  bge	TARGET_0_3
	TARGET_0_2:
	  call int32 EHTest::GetInt32()
	  ldc.i4 85
	  cgt
	  stsfld int32 EHTest::fld5
	  ldc.i4 32
	  ldc.i4 59
	  stsfld int32 EHTest::fld5
	  ldc.i4 37
	  starg.s arg1
	  ldc.i4 71
	  bge.un	TARGET_0_3
	TARGET_0_3:
	  sizeof native int
	  stloc.0
	  ldc.i4 92
	  ldc.i4 98
	  clt
	  ldc.i4.0
	  stloc.0
	  ldc.i4 83
	  bge.un	TARGET_0_4
	TARGET_0_4:
          ldc.i8 0x123456
	  starg.s arg0
	  ldc.i8 109
	  ldc.i8 64
	  stsfld int64 EHTest::fld1
	  ldc.i8 62
	  mul
	  ldc.i8 52
	  blt	TARGET_0_4
try3_out:
leave fault1_out
end_try3:

fault1_out: endfault
end_fault1:


call void EHTest::f2()
try1_out:
leave OUT0
end_try1:

OUT0:
nop
	TARGET_1_0:
	  ldc.i4 106
	  ldc.i4 24
	  shl
	  starg.s arg1
	  ldc.i4 40
	  starg.s arg1
	  ldc.i4 88
	  neg
	  ldc.i4 71
	  ble.un	TARGET_1_4
	TARGET_1_1:
	  ldarg.1
	  stloc.0
	  ldc.i4 56
	  conv.i8
	  starg.s arg2
	  ldc.i8 56
	  ldc.i8 5
	  div
	  ldc.i8 4
	  rem
	  ldc.i8 105
	  ceq
	  stsfld int32 EHTest::fld2
	  ldc.i4.0
	  starg.s arg1
	  ldc.i4 57
	  conv.i4
	  ldc.i4 90
	  add
	  ldc.i4 79
	  bgt.un	TARGET_1_1
	TARGET_1_2:
	  ldc.i4 101
	  ldc.i4 9
	  stloc.0
	  ldc.i4 65
	  starg.s arg1
	  ldc.i4 122
	  ldc.i4 107
	  ldc.i4.6
	  ldc.i4 32
	  starg.s arg1
	  ldc.i4 50
	  stsfld int32 EHTest::fld5
	  ldc.i4 126
	  ldc.i4 113
	  newobj instance void int32[13...,68...,1...,3...,98...,23...]::.ctor(int32,int32,int32,int32,int32,int32)
	  ldc.i4 18
	  ldc.i4 71
	  ldc.i4 106
	  stsfld int32 EHTest::fld5
	  ldc.i4 92
	  ldc.i4 74
	  ldc.i4 92
	  ldc.i4 93
	  call instance int32 int32[13...,68...,1...,3...,98...,23...]::Get(int32,int32,int32,int32,int32,int32)
	  ldc.i4 66
	  blt.un	TARGET_1_1
	TARGET_1_3:
	  ldc.i8 114
     stsfld int64 EHTest::fld4
	  ldsfld int64 EHTest::fld4
	  starg.s arg0
	  sizeof [mscorlib]System.UIntPtr
	  starg.s arg1
	  ldc.i4 87
	  starg.s arg1
	  ldc.i4 62
	  ldc.i4 83
	  stloc.0
	  ldc.i4 96
	  stloc.0
	  ldc.i4 78
	  mul
	  newarr     [mscorlib]System.Int32
	  ldc.i4.2
	  ldelem.i4
	  ldc.i4 62
	  starg.s arg1
	  ldc.i4 105
	  starg.s arg1
	  ldc.i4 35
	  ldc.i4 77
	  ldc.i4 36
	  stloc.0
	  ldc.i4 30
	  ldc.i4 36
	  starg.s arg1
	  ldc.i4 38
	  starg.s arg1
	  ldc.i4 47
	  ldc.i4 50
	  ldc.i4 44
	  ldc.i4 72
	  stloc.0
	  ldc.i4 90
	  ldc.i4 89
	  ldc.i4 121
	  ldc.i4 10
	  ldc.i4 83
	  stloc.0
	  ldc.i4 34
	  ldc.i4 119
	  ldc.i4 94
	  ldc.i4 62
	  stloc.0
	  ldc.i4.4
	  ldc.i4 33
	  newobj instance void int64[89...,0...,2...,34...,75...,124...,10...,86...,126...,126...,78...,113...,42...,2...,105...,44...]::.ctor(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4 124
	  ldc.i4 34
	  ldc.i4.8
	  ldc.i4 54
	  ldc.i4 46
	  ldc.i4 11
	  starg.s arg1
	  ldc.i4 36
	  ldc.i4 124
	  ldc.i4 72
	  ldc.i4 94
	  ldc.i4 42
	  ldc.i4 99
	  starg.s arg1
	  ldc.i4 96
	  starg.s arg1
	  ldc.i4 42
	  starg.s arg1
	  ldc.i4 37
	  ldc.i4 70
	  stloc.0
	  ldc.i4 17
	  starg.s arg1
	  ldc.i4 65
	  stsfld int32 EHTest::fld2
	  ldc.i4 88
	  ldc.i4 35
	  ldc.i4 94
	  stloc.0
	  ldc.i4 20
	  ldc.i4 23
	  ldc.i4 112
	  call instance int64 int64[89...,0...,2...,34...,75...,124...,10...,86...,126...,126...,78...,113...,42...,2...,105...,44...]::Get(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4.8
	  stsfld int32 EHTest::fld2
	  ldc.i4 103
	  newarr     [mscorlib]System.Int64
	  ldc.i4 122
	  ldelem.i8
	  starg.s arg0
	  ldarg.0
	  blt	TARGET_1_2
	TARGET_1_4:
ldc.i4 0
	  ldc.i4.5
	  ldc.i4 55
	  stloc.0
	  ldc.i4 32
	  mul
	  bge	TARGET_1_0
br start_try4
start_try4: 
nop
	TARGET_2_0:
	  ldc.i8 15
	  ldc.i8 127
	  starg.s arg0
	  ldc.i8 117
	  or
	  stsfld int64 EHTest::fld3
	  ldc.i8 49
	  ldc.i8 17
	  ble.un	TARGET_2_4
	TARGET_2_1:
ldc.i8 0
	  ldc.i8 82
	  blt	TARGET_2_3
	TARGET_2_2:
	  ldarga arg1
	  mkrefany   [mscorlib]System.Int32
	  refanyval	[mscorlib]System.Int32
	  ldind.i4
	  starg.s arg1
	  ldc.i4 83
	  ldc.i4 112
	  blt.un	TARGET_2_3
	TARGET_2_3:
	  ldsfld int32 EHTest::fld2
	  newarr     [mscorlib]System.Int32
	  ldc.i4 91
	  ldelem.i4
	  ldc.i4 77
	  starg.s arg1
	  ldc.i4 34
	  stloc.0
	  ldc.i4 114
	  stsfld int32 EHTest::fld5
	  ldc.i4 109
	  stloc.0
	  ldc.i4 99
	  ldc.i4 106
	  mul
	  ble.un	TARGET_2_0
	TARGET_2_4:
	  ldsfld int64 EHTest::fld4
	  ldc.i8 41
     stloc 2
     ldloca 2
	  mkrefany   [mscorlib]System.Int64
	  refanyval	[mscorlib]System.Int64
	  ldind.i8
	  stloc.2
	  ldarg.0
	  stsfld int64 EHTest::fld3
	  ldc.i8 100
	  neg
	  ble.un	TARGET_2_3
try4_out:
newobj instance void [mscorlib]System.StackOverflowException::.ctor()
throw
end_try4:

start_catch1: 
catch1_out:
pop
newobj instance void [mscorlib]System.NullReferenceException::.ctor()
throw
end_catch1:


start_catch2: 
pop
catch2_out:
nop
	TARGET_3_0:
	  ldc.i4 31
	  ldc.i4 11
	  shl
	  ldc.i4 118
	  stloc.0
	  ldc.i4 95
	  beq	TARGET_3_1
	TARGET_3_1:
	  ldc.i8 61
	  ldc.i8 109
	  add
	  stloc.2
	  ldc.i8 14
	  stloc.1
	  ldc.i8 120
	  box int64
	  unbox int64
	  ldind.i8
	  neg
	  starg.s arg0
	  ldc.i8 82
	  ldc.i8 56
	  ble.un	TARGET_3_2
	TARGET_3_2:
	  call int32 EHTest::GetInt32()
	  ldc.i4 117
	  add
	  ldc.i4 102
	  starg.s arg1
	  ldc.i4 25
	  bge.un	TARGET_3_3
	TARGET_3_3:
	  ldarg.0
	  ldloca 1
	  mkrefany   [mscorlib]System.Int64
	  refanyval	[mscorlib]System.Int64
	  ldind.i8
	  bge.un	TARGET_3_3
	TARGET_3_4:
	  call int64 EHTest::GetInt64()
	  stloc.1
	  ldsfld int32 EHTest::fld5
	  newarr     [mscorlib]System.Int64
	  ldc.i4 59
	  ldc.i4 12
	  shl
	  ldelem.i8
	  starg.s arg2
	  ldc.i8 42
	  ldc.i8 37
	  stloc.1
	  ldc.i8 102
	  starg.s arg0
	  ldc.i8 99
	  stsfld int64 EHTest::fld1
	  ldc.i8 43
	  stloc.2
	  ldc.i8 83
	  starg.s arg0
	  ldc.i8 121
	  starg.s arg0
	  ldc.i8 72
	  rem
	  ldc.i8 124
	  clt
	  ldc.i4.3
	  or
	  ldc.i4 123
	  stloc.0
	  ldc.i4 78
	  beq	TARGET_3_2
newobj instance void [mscorlib]System.OverflowException::.ctor()
throw
end_catch2:


exception_filter1: 
pop
ldc.i4.1
endfilter
start_filter1: 
pop
call void EHTest::f2()
filter1_out: leave OUT1
end_filter1:

exception_filter2: 
pop
ldc.i4.0
endfilter
start_filter2: 
pop
filter2_out: leave OUT1
end_filter2:

OUT1:
leave OUT
OUT: 
	  ldc.i8 31
ret
.try start_try4 to end_try4 catch [mscorlib]System.StackOverflowException handler start_catch1 to end_catch1
.try start_try4 to end_try4 catch [mscorlib]System.NullReferenceException handler start_catch2 to end_catch2
.try start_try4 to end_try4 filter exception_filter1 handler start_filter1 to end_filter1
.try start_try4 to end_try4 filter exception_filter2 handler start_filter2 to end_filter2
.try start_try3 to end_try3 finally handler start_finally2 to end_finally2
.try start_try2 to end_try2 fault handler start_fault1 to end_fault1
.try start_try1 to end_try1 finally handler start_finally1 to end_finally1
}

.method public static int32 f1(int32 arg0,int64 arg1,int64 arg2,int32 arg3,int64 arg4) cil managed
{
.maxstack 500
.locals init (int32 V_0,
		int64 V_1)
br start_try1
start_try1: 
br try1_br
start_finally1: 
finally1_out: endfinally
end_finally1:


try1_br: 
start_try2: 
nop
	TARGET_4_0:
	  ldc.i8 118
	  ldc.i8 88
	  clt
	  ldc.i8 113
	  ldc.i8 124
	  cgt
	  starg.s arg0
	  ldc.i4 125
	  ldc.i4 119
	  rem
	  ldc.i4 107
	  or
	  stsfld int32 EHTest::fld2
	  ldc.i4 19
	  stsfld int32 EHTest::fld2
	  ldc.i4 65
	  ldc.i4 31
	  starg.s arg3
	  ldc.i4 25
	  ldc.i4 120
	  ldc.i4 42
	  ldc.i4 90
	  ldc.i4 62
	  ldc.i4 88
	  stsfld int32 EHTest::fld5
	  ldc.i4 10
	  ldc.i4 55
	  stloc.0
	  ldc.i4 99
	  stloc.0
	  ldc.i4 124
	  stsfld int32 EHTest::fld5
	  ldc.i4 100
	  ldc.i4 112
	  ldc.i4 39
	  ldc.i4.5
	  starg.s arg0
	  ldc.i4 121
	  ldc.i4 51
	  stsfld int32 EHTest::fld5
	  ldc.i4 53
	  ldc.i4 33
	  ldc.i4 47
	  ldc.i4 56
	  starg.s arg3
	  ldc.i4 54
	  starg.s arg3
	  ldc.i4 95
	  ldc.i4 53
	  ldc.i4 104
	  stloc.0
	  ldc.i4 32
	  ldc.i4 127
	  newobj instance void int64[...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...]::.ctor(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4 72
	  starg.s arg0
	  ldc.i4 100
	  ldc.i4 89
	  starg.s arg0
	  ldc.i4 115
	  ldc.i4 15
	  stloc.0
	  ldc.i4 41
	  ldc.i4 95
	  ldc.i4 94
	  ldc.i4 51
	  ldc.i4 81
	  ldc.i4 118
	  ldc.i4 22
	  ldc.i4 82
	  ldc.i4 73
	  stsfld int32 EHTest::fld5
	  ldc.i4 83
	  ldc.i4 114
	  stloc.0
	  ldc.i4 68
	  ldc.i4 31
	  stsfld int32 EHTest::fld2
	  ldc.i4 59
	  stsfld int32 EHTest::fld2
	  ldc.i4 118
	  ldc.i4 25
	  stsfld int32 EHTest::fld2
	  ldc.i4 70
	  ldc.i4 32
	  stloc.0
	  ldc.i4 17
	  ldc.i4 33
	  starg.s arg3
	  ldc.i4 47
	  stsfld int32 EHTest::fld5
	  ldc.i4 9
	  ldc.i4 89
	  ldc.i4 18
	  ldc.i4 37
	  stsfld int32 EHTest::fld2
	  ldc.i4 44
	  starg.s arg0
	  ldc.i4 43
	  starg.s arg0
	  ldc.i4.7
	  call instance int64 int64[...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...]::Get(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  stloc.1
	  ldc.i8 70
	  ldc.i8 14
	  xor
	  ldc.i8 92
	  starg.s arg1
	  ldc.i8 112
	  bge	TARGET_4_2
	TARGET_4_1:
	  ldloca 1
	  mkrefany   [mscorlib]System.Int64
	  refanyval	[mscorlib]System.Int64
	  ldind.i8
	  ldc.i8 80
	  stsfld int64 EHTest::fld3
	  ldc.i8 27
	  stsfld int64 EHTest::fld4
	  ldc.i8 102
	  not
	  bgt.un	TARGET_4_3
	TARGET_4_2:
	  ldloc.1
	  ldc.i8 50
	  ldc.i8 107
	  sub
	  starg.s arg2
	  ldc.i8 55
	  bge.un	TARGET_4_0
	TARGET_4_3:
	  ldc.i4 96
	  ldarg.3
	  starg.s arg0
	  ldarg.3
	  div
	  stloc.0
	  ldc.i4 68
	  ldc.i4.1
	  stloc.0
	  ldc.i4 107
	  beq	TARGET_4_0
	TARGET_4_4:
	  ldloc.1
	  stsfld int64 EHTest::fld0
	  ldc.i8 9
	  stsfld int64 EHTest::fld3
	  ldc.i8 98
	  ldc.i8 85
	  bge	TARGET_4_1
try2_out:
newobj instance void [mscorlib]System.ArgumentNullException::.ctor()
throw
end_try2:

call void EHTest::f2()
try1_out:
newobj instance void [mscorlib]System.OutOfMemoryException::.ctor()
throw
end_try1:

start_finally2: 
br finally2_br
start_catch1: 
catch1_out:
pop
newobj instance void [mscorlib]System.NotFiniteNumberException::.ctor()
throw
end_catch1:


exception_filter1: 
pop
ldc.i4.1
endfilter
start_filter1: 
pop
nop
	TARGET_5_0:
	  ldc.i4 27
	  starg.s arg0
	  ldc.i4 36
	  starg.s arg0
	  ldc.i4 92
	  starg.s arg3
	  ldc.i4 50
	  stloc.0
	  ldc.i4 23
	  ldc.i4 14
	  mul
	  ldc.i4 72
	  stsfld int32 EHTest::fld2
	  ldc.i4 27
	  cgt
	  ldc.i4.6
	  stloc.0
	  ldc.i4 19
	  bgt.un	TARGET_5_4
	TARGET_5_1:
	  ldsfld int64 EHTest::fld1
	  ldc.i8 103
	  bgt	TARGET_5_2
	TARGET_5_2:
	  ldc.i8 127
	  ldc.i8 113
	  xor
	  ldc.i8 106
	  blt	TARGET_5_4
	TARGET_5_3:
	  ldloca 1
	  mkrefany   [mscorlib]System.Int64
	  refanyval	[mscorlib]System.Int64
	  ldind.i8
	  starg.s arg1
	  ldc.i8 108
	  ldc.i8 69
	  sub
	  ldc.i8 21
	  starg.s arg4
	  ldc.i8 27
	  blt.un	TARGET_5_0
	TARGET_5_4:
	  ldc.i4 101
	  ldc.i4 103
	  stloc.0
	  ldc.i4 86
	  mul
	  ldc.i4 124
	  starg.s arg0
	  ldc.i4 95
	  add
	  newarr     [mscorlib]System.Int64
	  ldc.i4 70
	  stloc.0
	  ldc.i4 61
	  ldelem.i8
	  stsfld int64 EHTest::fld1
	  ldc.i8 45
	  ldc.i8 75
	  starg.s arg1
	  ldc.i8 49
	  bge.un	TARGET_5_2
filter1_out: leave finally2_out
end_filter1:

exception_filter2: 
pop
ldc.i4.1
endfilter
start_filter2: 
pop
filter2_out: leave finally2_out
end_filter2:

exception_filter3: 
pop
ldc.i4.0
endfilter
start_filter3: 
pop
br filter3_br
start_finally3: 
call void EHTest::f2()
finally3_out: endfinally
end_finally3:


filter3_br: 
start_try4: 
call void EHTest::f2()
try4_out:
newobj instance void [mscorlib]System.MissingFieldException::.ctor()
throw
end_try4:

call void EHTest::f2()
filter3_out: leave finally2_out
end_filter3:

finally2_br: 
start_try3: 
call void EHTest::f2()
try3_out:
leave finally2_out
end_try3:

call void EHTest::f2()
finally2_out: endfinally
end_finally2:


OUT: 
	  ldc.i4 87
ret
OUT0:
leave OUT
.try start_try4 to end_try4 finally handler start_finally3 to end_finally3
.try start_try3 to end_try3 catch [mscorlib]System.OutOfMemoryException handler start_catch1 to end_catch1
.try start_try3 to end_try3 filter exception_filter1 handler start_filter1 to end_filter1
.try start_try3 to end_try3 filter exception_filter2 handler start_filter2 to end_filter2
.try start_try3 to end_try3 filter exception_filter3 handler start_filter3 to end_filter3
.try start_try2 to end_try2 finally handler start_finally1 to end_finally1
.try start_try1 to end_try1 finally handler start_finally2 to end_finally2
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
		int64 V_2,
		int64 V_3,
		int32 V_4,
		int32 V_5)
.try
{
	  ldc.i4.6
	  ldc.i8 43
	  ldc.i8 90
	  ldc.i4 113
	  ldc.i8 22
call int32 EHTest::f1(int32 ,int64 ,int64 ,int32 ,int64 )
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
