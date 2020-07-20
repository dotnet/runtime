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
  .field private static int32 fld0

  .method public static int32 GetInt32()
  {
	ldc.i4 684278239
	ret
  }
  .method public static int64 GetInt64()
  {
	ldc.i4 882062663
	conv.i8
	ret
  }
  .method public static int32 GetInt32_jmp()
  {
  	jmp   int32 EHTest::GetInt32()
  }
  .method public static int32 GetInt32_tailcall()
  {
  	tail. call   int32 EHTest::GetInt32()
  	ret
  }
  .method public static int64 GetInt64_jmp()
  {
  	jmp   int64 EHTest::GetInt64()
  }
  .method public static int64 GetInt64_tailcall()
  {
  	tail. call   int64 EHTest::GetInt64()
  	ret
  }
.method public static int64 f0(int64 arg0,int64 arg1) cil managed
{
.maxstack 500
.locals init (int32 V_0,
		int64 V_1,
		int32 V_2,
		int32 V_3,
		int32 V_4,
		int32 V_5)
br start_try1
start_fault1: 
fault1_out: endfault
end_fault1: 


start_try1: 
br try1_br
start_catch1: 
catch1_out:
call void EHTest::f2()
pop
newobj instance void [mscorlib]System.MissingFieldException::.ctor()
throw
end_catch1: 


start_catch2: 
pop
br catch2_br
start_catch3: 
catch3_out:
nop
	TARGET_0_0:
	  sizeof native unsigned int
	  ldc.i4 109
     stsfld int32 EHTest::fld0
	  ldsfld int32 EHTest::fld0
	  ldc.i4 52
	  ldc.i4.4
	  ldc.i4 73
	  ldc.i4 121
	  ldc.i4 122
	  stsfld int32 EHTest::fld0
	  ldc.i4 60
	  stloc 4
	  ldc.i4 70
	  ldc.i4 42
	  stsfld int32 EHTest::fld0
	  ldc.i4 9
	  ldc.i4 120
	  ldc.i4 86
	  ldc.i4.4
	  ldc.i4 98
	  ldc.i4 28
	  ldc.i4 125
	  stsfld int32 EHTest::fld0
	  ldc.i4 51
	  stsfld int32 EHTest::fld0
	  ldc.i4 22
	  stsfld int32 EHTest::fld0
	  ldc.i4 13
	  ldc.i4 65
	  stsfld int32 EHTest::fld0
	  ldc.i4 46
	  ldc.i4 26
	  stsfld int32 EHTest::fld0
	  ldc.i4 81
	  ldc.i4 72
	  ldc.i4 110
	  stsfld int32 EHTest::fld0
	  ldc.i4 104
	  ldc.i4 52
	  ldc.i4 98
	  ldc.i4.3
	  ldc.i4 46
	  stsfld int32 EHTest::fld0
	  ldc.i4 46
	  stloc.0
	  ldc.i4 93
	  ldc.i4 87
	  ldc.i4 100
	  stsfld int32 EHTest::fld0
	  ldc.i4 19
	  stsfld int32 EHTest::fld0
	  ldc.i4 103
	  stloc.0
	  ldc.i4 10
	  ldc.i4 72
	  ldc.i4 9
	  ldc.i4 45
	  ldc.i4 120
	  stsfld int32 EHTest::fld0
	  ldc.i4 57
	  ldc.i4 58
	  stsfld int32 EHTest::fld0
	  ldc.i4.0
	  newobj instance void int32[109,57,69,118,54,39,66,54,97,75,62,101,112,64,62,75,52,90,65,22,101,39,60,87,76,46,97,96]::.ctor(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4 22
	  ldc.i4 94
	  ldc.i4 82
	  ldc.i4 15
	  ldc.i4 91
	  ldc.i4 77
	  ldc.i4 17
	  ldc.i4 70
	  stsfld int32 EHTest::fld0
	  ldc.i4 32
	  ldc.i4 86
	  ldc.i4 27
	  ldc.i4 28
	  ldc.i4 75
	  ldc.i4 30
	  stsfld int32 EHTest::fld0
	  ldc.i4 21
	  ldc.i4 18
	  ldc.i4 111
	  ldc.i4 79
	  ldc.i4 102
	  ldc.i4 112
	  ldc.i4 98
	  ldc.i4 114
	  ldc.i4 17
	  ldc.i4 44
	  ldc.i4 45
	  ldc.i4.7
	  stsfld int32 EHTest::fld0
	  ldc.i4 103
	  ldc.i4 45
	  ldc.i4 94
	  ldc.i4 110
	  ldc.i4 104
	  stsfld int32 EHTest::fld0
	  ldc.i4 34
	  call instance int32 int32[109,57,69,118,54,39,66,54,97,75,62,101,112,64,62,75,52,90,65,22,101,39,60,87,76,46,97,96]::Get(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4 39
	  or
	  bgt.un	TARGET_0_1
	TARGET_0_1:
	  ldc.i8 58
	  not
	  ldc.i8 3
	  blt.un	TARGET_0_0
	TARGET_0_2:
	  ldc.i8 19
	  ldc.i8 50
	  box int64
	  unbox int64
	  ldind.i8
	  box int64
	  unbox int64
	  ldind.i8
	  starg.s arg0
	  ldc.i8 20
	  starg.s arg1
	  ldc.i8 91
	  not
	  bge	TARGET_0_2
	TARGET_0_3:
	  ldc.i4 83
	  stsfld int32 EHTest::fld0
	  ldc.i4 118
	  ldc.i4 3
	  shl
	  ldc.i4 24
	  bge.un	TARGET_0_1
	TARGET_0_4:
	  ldc.i8 75
	  ldc.i8 71
     stloc 1
     ldloca 1
	  mkrefany   [mscorlib]System.Int64
	  refanyval	[mscorlib]System.Int64
	  ldind.i8
	  starg.s arg1
	  ldc.i8 3
	  ble	TARGET_0_4
rethrow
end_catch3: 


start_catch4: 
catch4_out:
nop
	TARGET_1_0:
	  ldc.i8 15
	  stloc.1
	  ldc.i8 6
	  box int64
	  unbox int64
	  ldind.i8
	  stloc.1
	  ldc.i8 69
	  ldc.i8 53
	  starg.s arg0
	  ldc.i8 16
	  add
	  stloc.1
	  ldc.i8 19
	  not
	  ldc.i8 1
	  starg.s arg0
	  ldc.i8 56
	  bgt.un	TARGET_1_3
	TARGET_1_1:
	  ldarg.1
	  ldloc.1
	  starg.s arg1
	  ldc.i8 103
	  div
	  box int64
	  unbox int64
	  ldind.i8
	  conv.i4
	  stloc 4
	  ldloc 4
	  neg
	  ldc.i4 13
	  bgt.un	TARGET_1_3
	TARGET_1_2:
	  ldc.i4.5
	  ldc.i4 29
	  shl
	  stsfld int32 EHTest::fld0
	  ldc.i4 17
	  ldc.i4 109
	  stsfld int32 EHTest::fld0
	  ldc.i4.4
	  stsfld int32 EHTest::fld0
	  ldc.i4 14
	  beq	TARGET_1_3
	TARGET_1_3:
	  ldc.i4 114
	  stloc 4
	  ldc.i4 45
	  stsfld int32 EHTest::fld0
	  ldc.i4 117
	  neg
	  ldc.i4 24
	  shr
	  newarr     [mscorlib]System.Int64
	  ldc.i4 93
	  stsfld int32 EHTest::fld0
	  ldc.i4 95
	  ldelem.i8
	  ldc.i8 50
	  cgt
	  ldc.i4 57
	  stsfld int32 EHTest::fld0
	  ldc.i4 32
	  bgt	TARGET_1_2
	TARGET_1_4:
	  ldc.i8 64
	  stloc.1
	  ldc.i8 40
	  starg.s arg0
	  ldc.i8 70
	  box int64
	  unbox int64
	  ldind.i8
	  ldc.i8 14
	  stloc.1
	  ldc.i8 90
	  bge	TARGET_1_1
pop
leave catch2_out
end_catch4: 


catch2_br: 
start_try3: 
nop
	TARGET_2_0:
	  ldc.i4 109
	  box int32
	  unbox int32
	  ldind.i4
	  stloc.0
	  ldc.i4 105
	  ldc.i4 84
	  or
	  stsfld int32 EHTest::fld0
	  ldc.i4 23
	  ldc.i4 62
	  blt	TARGET_2_1
	TARGET_2_1:
	  ldarg.0
	  ldc.i8 58
	  sub
	  ldc.i8 71
	  blt.un	TARGET_2_0
	TARGET_2_2:
	  ldc.i4 66
	  ldc.i4 38
	  stloc 4
	  ldc.i4 67
	  cgt
	  ldc.i4 31
	  stsfld int32 EHTest::fld0
	  ldc.i4 25
	  or
	  stsfld int32 EHTest::fld0
	  ldc.i4 25
	  stsfld int32 EHTest::fld0
	  ldc.i4 59
	  newarr     [mscorlib]System.Int32
	  ldc.i4 19
	  ldelem.i4
	  stsfld int32 EHTest::fld0
	  ldc.i4 41
	  newarr     [mscorlib]System.Int64
	  ldc.i4 24
	  stloc.3
	  ldc.i4 19
	  stloc.2
	  ldc.i4 39
	  ldelem.i8
	  ldftn int64 EHTest::GetInt64_jmp()
	  calli int64 ()
	  ble	TARGET_2_4
	TARGET_2_3:
	  ldc.i8 45
	  ldc.i8 81
	  starg.s arg1
	  ldc.i8 123
	  starg.s arg0
	  ldc.i8 4
	  sub
	  neg
	  starg.s arg1
	  ldc.i8 64
	  ldc.i8 24
	  starg.s arg0
	  ldc.i8 51
	  cgt
	  ldc.i4 69
	  stsfld int32 EHTest::fld0
	  ldc.i4 94
	  bgt.un	TARGET_2_3
	TARGET_2_4:
	  ldc.i8 96
	  starg.s arg0
	  ldc.i8 101
	  stloc.1
	  ldc.i8 85
	  stloc.1
	  ldc.i8 95
	  not
	  ldc.i8 59
	  blt	TARGET_2_1
try3_out:
newobj instance void [mscorlib]System.DivideByZeroException::.ctor()
throw
end_try3: 

start_catch5: 
catch5_out:
pop
newobj instance void [mscorlib]System.OutOfMemoryException::.ctor()
throw
end_catch5: 


exception_filter1: 
pop
ldc.i4.0
endfilter
start_filter1: 
pop
filter1_out: leave catch2_out
end_filter1: 

exception_filter2: 
pop
ldc.i4.1
endfilter
start_filter2: 
pop
filter2_out: leave catch2_out
end_filter2: 

exception_filter3: 
pop
ldc.i4.0
endfilter
start_filter3: 
pop
br filter3_br
filter3_br: 
start_try4: 
try4_out:
newobj instance void [mscorlib]System.StackOverflowException::.ctor()
throw
end_try4: 

start_finally1: 
finally1_out: endfinally
end_finally1: 


nop
	TARGET_3_0:
	  ldc.i4 107
	  conv.i4
	  stsfld int32 EHTest::fld0
	  ldc.i4 84
	  ldc.i4 34
	  bge	TARGET_3_2
	TARGET_3_1:
	  call int32 EHTest::GetInt32()
	  stsfld int32 EHTest::fld0
	  ldc.i4 56
	  ldc.i4 57
	  stsfld int32 EHTest::fld0
	  ldc.i4 21
	  stsfld int32 EHTest::fld0
	  ldc.i4 87
	  stloc.3
	  ldc.i4 53
	  bge	TARGET_3_2
	TARGET_3_2:
	  ldsfld int32 EHTest::fld0
	  stsfld int32 EHTest::fld0
	  ldc.i4 27
	  newarr     [mscorlib]System.Int32
	  ldc.i4 14
	  ldelem.i4
	  ldc.i4 103
	  ble.un	TARGET_3_0
	TARGET_3_3:
	  ldloc.1
	  stloc.1
	  call int64 EHTest::GetInt64_jmp()
	  stloc.1
	  ldc.i8 94
	  ldc.i8 106
	  cgt
	  ldc.i8 63
	  ldc.i8 69
	  cgt
	  blt	TARGET_3_2
	TARGET_3_4:
	  ldftn int64 EHTest::GetInt64_jmp()
	  calli int64 ()
	  stloc.1
	  ldc.i8 44
	  stloc.1
	  ldc.i8 4
	  conv.i8
	  ldc.i8 53
	  xor
	  starg.s arg0
	  ldc.i8 75
	  ldc.i8 0
	  bge.un	TARGET_3_3
filter3_out: leave catch2_out
end_filter3: 

catch2_out:
nop
	TARGET_4_0:
	  ldloc.1
	  neg
	  ldc.i8 40
	  stloc.1
	  ldc.i8 18
	  stloc.1
	  ldc.i8 95
	  starg.s arg1
	  ldc.i8 87
	  rem
	  ldc.i8 108
	  bgt.un	TARGET_4_3
	TARGET_4_1:
	  ldarg.0
	  conv.i8
	  ldc.i8 36
	  stloc.1
	  ldc.i8 16
	  rem
	  ldc.i8 7
	  rem
	  ldc.i8 37
	  beq	TARGET_4_4
	TARGET_4_2:
	  ldloca 1
	  mkrefany   [mscorlib]System.Int64
	  refanyval	[mscorlib]System.Int64
	  ldind.i8
	  ldc.i4 126
	  conv.i8
	  box int64
	  unbox int64
	  ldind.i8
	  starg.s arg1
	  ldc.i8 42
	  starg.s arg1
	  ldc.i8 94
	  ldc.i8 18
	  div
	  ble	TARGET_4_1
	TARGET_4_3:
	  call int64 EHTest::GetInt64_jmp()
	  starg.s arg0
	  ldc.i4 60
	  ldc.i4 61
	  stsfld int32 EHTest::fld0
	  ldc.i4 70
	  stloc.0
	  ldc.i4 112
	  rem
	  stsfld int32 EHTest::fld0
	  ldc.i4 9
	  stsfld int32 EHTest::fld0
	  ldc.i4 59
	  neg
	  ldc.i4 29
	  shr
	  conv.i4
	  newarr     [mscorlib]System.Int64
	  ldc.i4 13
	  ldelem.i8
	  ldc.i8 86
	  bgt	TARGET_4_2
	TARGET_4_4:
	  call int64 EHTest::GetInt64()
	  stloc.1
	  ldc.i8 107
	  starg.s arg0
	  ldc.i8 89
	  ldc.i8 39
	  stloc.1
	  ldc.i8 98
	  blt.un	TARGET_4_1
leave try1_out
end_catch2: 


exception_filter4: 
pop
ldc.i4.0
endfilter
start_filter4: 
pop
filter4_out: leave try1_out
end_filter4: 

try1_br: 
start_try2: 
try2_out:
newobj instance void [mscorlib]System.RankException::.ctor()
throw
end_try2: 

call void EHTest::f2()
try1_out:
newobj instance void [mscorlib]System.IndexOutOfRangeException::.ctor()
throw
end_try1: 

OUT0:
leave OUT
OUT: 
	  ldc.i8 10
ret
.try start_try4 to end_try4 finally handler start_finally1 to end_finally1
.try start_try3 to end_try3 catch [mscorlib]System.Exception handler start_catch3 to end_catch3
.try start_try3 to end_try3 catch [mscorlib]System.Exception handler start_catch4 to end_catch4
.try start_try3 to end_try3 catch [mscorlib]System.DivideByZeroException handler start_catch5 to end_catch5
.try start_try3 to end_try3 filter exception_filter1 handler start_filter1 to end_filter1
.try start_try3 to end_try3 filter exception_filter2 handler start_filter2 to end_filter2
.try start_try3 to end_try3 filter exception_filter3 handler start_filter3 to end_filter3
.try start_try2 to end_try2 catch [mscorlib]System.Exception handler start_catch1 to end_catch1
.try start_try2 to end_try2 catch [mscorlib]System.MissingFieldException handler start_catch2 to end_catch2
.try start_try2 to end_try2 filter exception_filter4 handler start_filter4 to end_filter4
.try start_try1 to end_try1 fault handler start_fault1 to end_fault1
}

.method public static int64 f1(int64 arg0) cil managed
{
.maxstack 500
.locals init (int32 V_0,
		int64 V_1,
		int64 V_2,
		int64 V_3,
		int32 V_4)
br start_try1
start_fault1: 
br fault1_br
start_fault2: 
nop
	TARGET_5_0:
	  sizeof native int
	  stsfld int32 EHTest::fld0
	  ldc.i4 13
	  ldc.i4 45
	  bge	TARGET_5_0
	TARGET_5_1:
	  ldsfld int32 EHTest::fld0
	  ldc.i4 52
	  ldc.i4 60
	  or
	  ldc.i4 13
	  stloc 4
	  ldc.i4 28
	  ldc.i4 42
	  stsfld int32 EHTest::fld0
	  ldc.i4 125
	  ldc.i4 108
	  stloc.0
	  ldc.i4.7
	  newobj instance void int64[86,43,110,24,0]::.ctor(int32,int32,int32,int32,int32)
	  ldc.i4 19
	  stsfld int32 EHTest::fld0
	  ldc.i4 28
	  ldc.i4.4
	  ldc.i4 71
	  stsfld int32 EHTest::fld0
	  ldc.i4 56
	  ldc.i4 39
	  stsfld int32 EHTest::fld0
	  ldc.i4 84
	  ldc.i4 59
	  call instance int64 int64[86,43,110,24,0]::Get(int32,int32,int32,int32,int32)
	  ldc.i8 6
	  beq	TARGET_5_3
	TARGET_5_2:
	  call int32 EHTest::GetInt32_tailcall()
	  ldc.i4 33
	  stloc.0
	  ldc.i4 109
	  sub
	  ldc.i4 49
	  stsfld int32 EHTest::fld0
	  ldc.i4 78
	  beq	TARGET_5_2
	TARGET_5_3:
	  ldc.i8 108
	  starg.s arg0
	  ldc.i8 55
	  ldc.i8 97
	  mul
	  starg.s arg0
	  ldc.i8 113
	  ldc.i8 9
	  beq	TARGET_5_3
	TARGET_5_4:
	  ldc.i8 43
	  conv.i8
	  starg.s arg0
	  ldc.i8 59
	  ldc.i8 1
	  stloc.3
	  ldc.i8 53
	  cgt
	  ldloc.0
	  stloc 4
	  ldc.i4 68
	  stloc 4
	  ldc.i4 66
	  stloc 4
	  ldc.i4 53
	  stloc 4
	  ldc.i4 14
	  ldc.i4 64
	  stsfld int32 EHTest::fld0
	  ldc.i4 29
	  add
	  ble.un	TARGET_5_0
fault2_out: endfault
end_fault2: 


fault1_br: 
start_try2: 
br try2_br
start_finally1: 
br finally1_br
start_finally2: 
call void EHTest::f2()
finally2_out: endfinally
end_finally2: 


finally1_br: 
start_try4: 
try4_out:
leave finally1_out
end_try4: 

nop
	TARGET_6_0:
	  sizeof [mscorlib]System.UIntPtr
	  sizeof [mscorlib]System.IntPtr
	  bge	TARGET_6_3
	TARGET_6_1:
	  ldc.i8 26
	  stloc.3
	  ldc.i8 11
	  stloc.2
	  ldc.i8 67
	  ldc.i8 18
	  starg.s arg0
	  ldc.i8 38
	  rem
	  starg.s arg0
	  ldc.i8 79
	  stloc.3
	  ldc.i8 74
	  ldc.i8 88
	  stloc.1
	  ldc.i8 124
	  ble.un	TARGET_6_3
	TARGET_6_2:
	  ldc.i4 91
	  ldc.i4 6
	  shr
	  ldc.i4 23
	  beq	TARGET_6_2
	TARGET_6_3:
	  ldarg.0
	  ldc.i8 47
	  beq	TARGET_6_4
	TARGET_6_4:
	  ldloc.1
	  stloc.1
	  ldc.i4 58
	  ldc.i4 101
	  ldc.i4 68
	  ldc.i4 116
	  stsfld int32 EHTest::fld0
	  ldc.i4 112
	  ldc.i4 103
	  stsfld int32 EHTest::fld0
	  ldc.i4.2
	  stloc 4
	  ldc.i4 53
	  stloc.0
	  ldc.i4 46
	  stsfld int32 EHTest::fld0
	  ldc.i4 92
	  ldc.i4 118
	  ldc.i4 91
	  ldc.i4 74
	  ldc.i4 79
	  stsfld int32 EHTest::fld0
	  ldc.i4 116
	  ldc.i4 75
	  ldc.i4 80
	  stsfld int32 EHTest::fld0
	  ldc.i4 28
	  ldc.i4 84
	  ldc.i4 62
	  ldc.i4 68
	  stsfld int32 EHTest::fld0
	  ldc.i4 92
	  stsfld int32 EHTest::fld0
	  ldc.i4 125
	  stsfld int32 EHTest::fld0
	  ldc.i4 18
	  stsfld int32 EHTest::fld0
	  ldc.i4.0
	  ldc.i4 20
	  ldc.i4 26
	  ldc.i4 100
	  stsfld int32 EHTest::fld0
	  ldc.i4.2
	  ldc.i4 72
	  ldc.i4 102
	  newobj instance void int32[,,,,,,,,,,,,,,,,,,]::.ctor(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4.1
	  ldc.i4 83
	  ldc.i4 66
	  ldc.i4 36
	  stsfld int32 EHTest::fld0
	  ldc.i4 105
	  ldc.i4 66
	  ldc.i4.8
	  stloc.0
	  ldc.i4 101
	  ldc.i4 85
	  stsfld int32 EHTest::fld0
	  ldc.i4 69
	  ldc.i4 54
	  stsfld int32 EHTest::fld0
	  ldc.i4 30
	  ldc.i4 94
	  stloc.0
	  ldc.i4 23
	  stloc.0
	  ldc.i4 25
	  ldc.i4 13
	  ldc.i4.6
	  ldc.i4 85
	  ldc.i4 36
	  stloc 4
	  ldc.i4 44
	  ldc.i4 18
	  ldc.i4 96
	  ldc.i4 62
	  stloc.0
	  ldc.i4 14
	  ldc.i4 44
	  stsfld int32 EHTest::fld0
	  ldc.i4 121
	  stsfld int32 EHTest::fld0
	  ldc.i4 73
	  ldc.i4 15
	  stloc.0
	  ldc.i4 57
	  stsfld int32 EHTest::fld0
	  ldc.i4 119
	  ldc.i4 31
	  call instance int32 int32[,,,,,,,,,,,,,,,,,,]::Get(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4 85
	  cgt
	  newarr     [mscorlib]System.Int32
	  ldc.i4 106
	  stsfld int32 EHTest::fld0
	  ldc.i4 112
	  ldelem.i4
	  ldc.i4.8
	  stloc 4
	  ldc.i4 18
	  stloc.0
	  ldc.i4 59
	  add
	  conv.i8
	  stloc.3
	  ldc.i8 90
	  ldc.i8 120
	  starg.s arg0
	  ldc.i8 37
	  xor
	  stloc.2
	  ldc.i8 25
	  ldc.i8 107
	  bgt	TARGET_6_4
finally1_out: endfinally
end_finally1: 


try2_br: 
start_try3: 
call void EHTest::f2()
try3_out:
newobj instance void [mscorlib]System.ArgumentOutOfRangeException::.ctor()
throw
end_try3: 

try2_out:
newobj instance void [mscorlib]System.RankException::.ctor()
throw
end_try2: 

nop
	TARGET_7_0:
	  ldc.i8 123
	  ldc.i8 69
	  stloc.3
	  ldc.i8 85
	  stloc.1
	  ldc.i8 61
	  div
	  ldc.i8 106
	  bgt	TARGET_7_3
	TARGET_7_1:
	  ldftn int32 EHTest::GetInt32()
	  calli int32 ()
	  newarr     [mscorlib]System.Int64
	  ldc.i4 45
	  neg
	  ldc.i4 34
	  ceq
	  ldelem.i8
	  ldloc.3
	  starg.s arg0
	  ldc.i8 37
	  mul
	  stloc.2
	  ldc.i8 115
	  ldc.i8 87
	  starg.s arg0
	  ldc.i8 30
	  bgt.un	TARGET_7_3
	TARGET_7_2:
	  ldc.i8 40
	  neg
	  stloc.2
	  ldc.i8 5
	  starg.s arg0
	  ldc.i8 74
	  ldc.i8 73
	  stloc.2
	  ldc.i8 120
	  stloc.2
	  ldc.i8 86
	  stloc.1
	  ldc.i8 5
	  sub
	  ldc.i8 93
	  starg.s arg0
	  ldc.i8 48
	  stloc.3
	  ldc.i8 27
	  div
	  starg.s arg0
	  ldc.i8 71
	  stloc.3
	  ldc.i8 51
	  ldc.i8 11
	  ble	TARGET_7_1
	TARGET_7_3:
	  ldftn int64 EHTest::GetInt64_jmp()
	  calli int64 ()
	  ldc.i8 127
	  stloc.3
	  ldc.i8 65
	  stloc.2
	  ldc.i8 66
	  not
	  starg.s arg0
	  ldc.i8 15
	  ldc.i8 60
	  stloc.3
	  ldc.i8 83
	  add
	  stloc.1
	  ldc.i8 76
	  blt.un	TARGET_7_2
	TARGET_7_4:
	  call int64 EHTest::GetInt64_tailcall()
	  ldc.i8 70
	  stloc.2
	  ldarga arg0
	  mkrefany   [mscorlib]System.Int64
	  refanyval	[mscorlib]System.Int64
	  ldind.i8
	  neg
	  stloc.3
	  ldc.i8 46
	  bge.un	TARGET_7_2
fault1_out: endfault
end_fault1: 


start_try1: 
try1_out:
newobj instance void [mscorlib]System.StackOverflowException::.ctor()
throw
end_try1: 
OUT: 
	  ldc.i8 112
ret
OUT0:
nop
	TARGET_8_0:
	  sizeof [mscorlib]System.IntPtr
	  ldc.i4 19
	  shl
	  newarr     [mscorlib]System.Int64
	  ldc.i4 58
	  ldelem.i8
	  ldloc.2
	  stloc.1
	  ldc.i8 79
	  sub
	  stloc.2
	  ldc.i8 24
	  ldc.i8 42
	  xor
	  stloc.3
	  ldc.i8 26
	  stloc.2
	  ldc.i8 15
	  ldc.i8 5
	  blt.un	TARGET_8_1
	TARGET_8_1:
	  ldloca 4
	  mkrefany   [mscorlib]System.Int32
	  refanyval	[mscorlib]System.Int32
	  ldind.i4
	  ldc.i4 53
	  ble	TARGET_8_4
	TARGET_8_2:
	  call int64 EHTest::GetInt64_jmp()
	  starg.s arg0
	  ldc.i8 40
	  ldc.i8 79
	  div
	  ldc.i8 8
	  div
	  ldc.i8 62
	  stloc.2
	  ldc.i8 86
	  bgt	TARGET_8_1
	TARGET_8_3:
	  ldloca 0
	  mkrefany   [mscorlib]System.Int32
	  refanyval	[mscorlib]System.Int32
	  ldind.i4
	  ldc.i4 71
	  div
	  ldc.i4 19
	  stsfld int32 EHTest::fld0
	  ldc.i4 126
	  blt	TARGET_8_4
	TARGET_8_4:
	  ldc.i8 81
	  conv.i8
	  conv.i8
	  ldc.i8 99
	  blt	TARGET_8_4
leave OUT
.try start_try4 to end_try4 finally handler start_finally2 to end_finally2
.try start_try3 to end_try3 finally handler start_finally1 to end_finally1
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
		int32 V_2,
		int64 V_3,
		int64 V_4,
		int32 V_5,
		int64 V_6)
.try
{
	  ldc.i8 4
call int64 EHTest::f1(int64 )
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
