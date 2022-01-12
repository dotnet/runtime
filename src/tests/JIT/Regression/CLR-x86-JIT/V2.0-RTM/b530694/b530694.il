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
  .field private static int32 fld0
  .field private static int32 fld1
  .field private static int64 fld2

  .method public static int32 GetInt32()
  {
	ldc.i4 2131605038
	ret
  }
  .method public static int64 GetInt64()
  {
	ldc.i4 665271698
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
.method public static int32 f0(int32 arg0,int64 arg1) cil managed
{
.maxstack 500
.locals init (int32 V_0,
		int64 V_1)
br start_try1
start_catch1: 
catch1_out:
call void EHTest::f2()
rethrow
end_catch1: 


start_try1: 
try1_out:
leave OUT0
end_try1: 

start_catch2: 
pop
br catch2_br
start_fault1: 
br fault1_br
start_finally1: 
br finally1_br
start_catch3: 
catch3_out:
pop
leave finally1_out
end_catch3: 


exception_filter1: 
pop
ldc.i4.0
endfilter
start_filter1: 
pop
nop
	TARGET_0_0:
	  ldc.i8 100
	  ldc.i8 54
     stsfld int64 EHTest::fld2
	  ldsfld int64 EHTest::fld2
	  stsfld int64 EHTest::fld2
	  ldc.i8 120
	  ble.un	TARGET_0_2
	TARGET_0_1:
	  ldc.i8 11
     stloc.1
     ldloc.1
	  ldc.i8 127
	  stloc.1
	  ldc.i8 102
	  beq	TARGET_0_1
	TARGET_0_2:
	  call int64 EHTest::GetInt64_tailcall()
	  ldsfld int64 EHTest::fld2
	  box int64
	  unbox int64
	  ldind.i8
	  blt.un	TARGET_0_1
	TARGET_0_3:
	  ldftn int64 EHTest::GetInt64_jmp()
	  calli int64 ()
	  ldc.i4 31
	  stloc.0
	  ldc.i4 69
	  stloc.0
	  ldc.i4 23
	  conv.i4
	  ldc.i4 63
	  mul
	  newarr     [mscorlib]System.Int64
	  ldc.i4 96
	  ldelem.i8
	  stsfld int64 EHTest::fld2
	  ldc.i8 83
	  or
	  ldc.i8 50
	  bgt.un	TARGET_0_4
	TARGET_0_4:
	  ldc.i4 51
     stsfld int32 EHTest::fld1
	  ldsfld int32 EHTest::fld1
	  ldsflda int32 EHTest::fld1
	  mkrefany   [mscorlib]System.Int32
	  refanyval	[mscorlib]System.Int32
	  ldind.i4
	  blt.un	TARGET_0_4
filter1_out: leave finally1_out
end_filter1: 

finally1_br: 
start_try4: 
call void EHTest::f2()
try4_out:
newobj instance void [mscorlib]System.EntryPointNotFoundException::.ctor()
throw
end_try4: 

exception_filter2: 
pop
ldc.i4.1
endfilter
start_filter2: 
pop
filter2_out: leave finally1_out
end_filter2: 

exception_filter3: 
pop
ldc.i4.0
endfilter
start_filter3: 
pop
filter3_out: leave finally1_out
end_filter3: 

finally1_out: endfinally
end_finally1: 


fault1_br: 
start_try3: 
call void EHTest::f2()
try3_out:
newobj instance void [mscorlib]System.OutOfMemoryException::.ctor()
throw
end_try3: 

nop
	TARGET_1_0:
	  ldc.i4 80
	  stsfld int32 EHTest::fld1
	  ldc.i4 59
	  starg.s arg0
	  ldc.i4 43
	  ldc.i4 5
	  shr
	  ldc.i4 121
	  ldc.i4 57
	  ldc.i4.7
	  ldc.i4 49
	  ldc.i4 126
	  ldc.i4 102
	  ldc.i4 57
	  ldc.i4.4
	  ldc.i4.5
	  ldc.i4 92
	  starg.s arg0
	  ldc.i4 70
	  ldc.i4 30
	  newobj instance void int64[,,,,,,,,,,,]::.ctor(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4.6
	  stsfld int32 EHTest::fld0
	  ldc.i4.8
	  stloc.0
	  ldc.i4.3
	  ldc.i4 92
	  ldc.i4 21
	  starg.s arg0
	  ldc.i4 96
	  starg.s arg0
	  ldc.i4 45
	  stsfld int32 EHTest::fld1
	  ldc.i4 92
	  ldc.i4 120
	  ldc.i4 35
	  ldc.i4 43
	  ldc.i4.4
	  ldc.i4 33
	  starg.s arg0
	  ldc.i4 36
	  stloc.0
	  ldc.i4 88
	  ldc.i4 17
	  ldc.i4 33
	  ldc.i4.5
	  ldc.i4 126
	  call instance int64 int64[,,,,,,,,,,,]::Get(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i8 126
	  bge.un	TARGET_1_2
	TARGET_1_1:
	  ldc.i4.7
	  stsfld int32 EHTest::fld1
	  ldc.i4 43
	  not
	  stsfld int32 EHTest::fld1
	  ldc.i4 31
	  ldc.i4 53
	  mul
	  stloc.0
	  ldc.i4 19
	  ldc.i4 125
	  starg.s arg0
	  ldc.i4 50
	  add
	  newarr     [mscorlib]System.Int64
	  ldc.i4 65
	  ldelem.i8
	  call int64 EHTest::GetInt64_jmp()
	  blt.un	TARGET_1_1
	TARGET_1_2:
	  ldloca 0
	  mkrefany   [mscorlib]System.Int32
	  refanyval	[mscorlib]System.Int32
	  ldind.i4
	  stsfld int32 EHTest::fld0
	  ldc.i4 125
	  ldc.i4 19
	  beq	TARGET_1_0
	TARGET_1_3:
	  ldc.i4 63
	  starg.s arg0
	  ldc.i4 38
	  ldc.i4 87
	  add
	  ldc.i4.6
	  bgt.un	TARGET_1_1
	TARGET_1_4:
	  ldc.i8 24
	  starg.s arg1
	  ldc.i8 1
	  ldc.i8 56
	  mul
	  box int64
	  unbox int64
	  ldind.i8
	  not
	  stloc.1
	  ldc.i8 106
	  ldc.i8 89
	  rem
	  ldc.i8 50
	  ble.un	TARGET_1_0
fault1_out: endfault
end_fault1: 


catch2_br: 
start_try2: 
try2_out:
newobj instance void [mscorlib]System.ArgumentException::.ctor()
throw
end_try2: 

catch2_out:
leave OUT0
end_catch2: 


start_catch4: 
pop
catch4_out:
nop
	TARGET_2_0:
	  ldc.i4 26
	  box int32
	  unbox int32
	  ldind.i4
	  starg.s arg0
	  ldc.i4 66
	  ldc.i4 15
	  shl
	  ldc.i4 52
	  stloc.0
	  ldc.i4 123
	  starg.s arg0
	  ldc.i4 14
	  starg.s arg0
	  ldc.i4 125
	  mul
	  ldc.i4 108
	  stloc.0
	  ldc.i4 38
	  ble	TARGET_2_4
	TARGET_2_1:
	  ldarga arg1
	  mkrefany   [mscorlib]System.Int64
	  refanyval	[mscorlib]System.Int64
	  ldind.i8
	  ldloc.0
	  newarr     [mscorlib]System.Int64
	  ldc.i4 85
	  ldelem.i8
	  stsfld int64 EHTest::fld2
	  ldc.i4 48
	  starg.s arg0
	  ldc.i4 25
	  newarr     [mscorlib]System.Int64
	  ldc.i4 65
	  ldelem.i8
	  bgt.un	TARGET_2_0
	TARGET_2_2:
	  ldftn int32 EHTest::GetInt32_tailcall()
	  calli int32 ()
	  ldc.i4 41
	  blt	TARGET_2_3
	TARGET_2_3:
	  ldsfld int64 EHTest::fld2
	  ldc.i8 43
	  add
	  ldc.i8 47
	  starg.s arg1
	  ldc.i8 123
	  ble.un	TARGET_2_2
	TARGET_2_4:
	  ldc.i4 32
	  ldc.i4 48
	  bgt.un	TARGET_2_3
newobj instance void [mscorlib]System.EntryPointNotFoundException::.ctor()
throw
end_catch4: 


exception_filter4: 
pop
ldc.i4.0
endfilter
start_filter4: 
pop
nop
	TARGET_3_0:
	  ldloc.1
	  stloc.1
	  ldarg.1
	  ldc.i8 120
	  sub
	  stloc.1
	  ldc.i8 46
	  stsfld int64 EHTest::fld2
	  ldc.i8 13
	  stsfld int64 EHTest::fld2
	  ldc.i8 27
	  stloc.1
	  ldc.i8 106
	  ldc.i8 86
	  rem
	  starg.s arg1
	  ldc.i8 2
	  ldc.i8 38
	  blt.un	TARGET_3_1
	TARGET_3_1:
	  ldarg.1
	  ldc.i8 102
	  ble.un	TARGET_3_3
	TARGET_3_2:
	  ldloca 0
	  mkrefany   [mscorlib]System.Int32
	  refanyval	[mscorlib]System.Int32
	  ldind.i4
	  newarr     [mscorlib]System.Int32
	  ldc.i4 119
	  ldelem.i4
	  stloc.0
	  ldc.i4 18
	  stsfld int32 EHTest::fld1
	  ldc.i4 99
	  ldc.i4 76
	  starg.s arg0
	  ldc.i4 89
	  bgt.un	TARGET_3_2
	TARGET_3_3:
	  ldloc.1
	  ldc.i8 55
	  or
	  ldc.i8 61
	  mul
	  stsfld int64 EHTest::fld2
	  ldc.i8 91
	  stloc.1
	  ldc.i8 43
	  ldc.i8 26
	  bge.un	TARGET_3_0
	TARGET_3_4:
	  call int32 EHTest::GetInt32_jmp()
	  ldc.i4 59
	  box int32
	  unbox int32
	  ldind.i4
	  blt	TARGET_3_3
filter4_out: leave OUT0
end_filter4: 

exception_filter5: 
pop
ldc.i4.0
endfilter
start_filter5: 
pop
call void EHTest::f2()
filter5_out: leave OUT0
end_filter5: 

OUT: 
	  ldc.i4 119
	  stloc.0
	  ldc.i4 14
	  stloc.0
	  ldc.i4 83
	  stloc.0
	  ldc.i4 74
ret
OUT0:
nop
	TARGET_4_0:
	  ldarga arg1
	  mkrefany   [mscorlib]System.Int64
	  refanyval	[mscorlib]System.Int64
	  ldind.i8
	  ldc.i8 37
	  ceq
	  ldloca 0
	  mkrefany   [mscorlib]System.Int32
	  refanyval	[mscorlib]System.Int32
	  ldind.i4
	  ldc.i4 119
	  add
	  bge	TARGET_4_4
	TARGET_4_1:
	  ldarg.0
	  neg
	  ldsfld int32 EHTest::fld1
	  neg
	  bgt	TARGET_4_3
	TARGET_4_2:
	  sizeof native int
	  stloc.0
	  ldc.i4 64
	  ldc.i4 100
	  cgt
	  ldc.i4 22
	  bgt.un	TARGET_4_3
	TARGET_4_3:
	  ldc.i4 73
	  ldc.i4 122
	  stsfld int32 EHTest::fld1
	  ldc.i4 22
	  ceq
	  stloc.0
	  ldc.i4 51
	  ldc.i4 59
	  bge.un	TARGET_4_4
	TARGET_4_4:
	  ldftn int32 EHTest::GetInt32_tailcall()
	  calli int32 ()
	  starg.s arg0
	  ldsflda int32 EHTest::fld1
	  mkrefany   [mscorlib]System.Int32
	  refanyval	[mscorlib]System.Int32
	  ldind.i4
	  stsfld int32 EHTest::fld0
	  ldc.i4 25
	  ldc.i4 126
	  starg.s arg0
	  ldc.i4 40
	  sub
	  ldc.i4 18
	  bgt.un	TARGET_4_0
leave OUT
.try start_try4 to end_try4 catch [mscorlib]System.Exception handler start_catch3 to end_catch3
.try start_try4 to end_try4 filter exception_filter1 handler start_filter1 to end_filter1
.try start_try4 to end_try4 filter exception_filter2 handler start_filter2 to end_filter2
.try start_try4 to end_try4 filter exception_filter3 handler start_filter3 to end_filter3
.try start_try3 to end_try3 finally handler start_finally1 to end_finally1
.try start_try2 to end_try2 fault handler start_fault1 to end_fault1
.try start_try1 to end_try1 catch [mscorlib]System.Exception handler start_catch1 to end_catch1
.try start_try1 to end_try1 catch [mscorlib]System.Exception handler start_catch2 to end_catch2
.try start_try1 to end_try1 catch [mscorlib]System.ArgumentException handler start_catch4 to end_catch4
.try start_try1 to end_try1 filter exception_filter4 handler start_filter4 to end_filter4
.try start_try1 to end_try1 filter exception_filter5 handler start_filter5 to end_filter5
}

.method public static int64 f1(int64 arg0) cil managed
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
nop
	TARGET_5_0:
	  ldsfld int64 EHTest::fld2
	  call int32 EHTest::GetInt32()
	  neg
	  sizeof native unsigned int
	  stsfld int32 EHTest::fld1
	  ldc.i4 71
	  stsfld int32 EHTest::fld0
	  ldc.i4 93
	  stloc.0
	  ldc.i4 42
	  ldc.i4 17
	  shl
	  ldc.i4 16
	  stsfld int32 EHTest::fld1
	  ldc.i4 102
	  stsfld int32 EHTest::fld1
	  ldc.i4 26
	  stsfld int32 EHTest::fld0
	  ldc.i4 47
	  ldc.i4 58
	  ldc.i4 24
	  ldc.i4 26
	  stsfld int32 EHTest::fld1
	  ldc.i4 89
	  ldc.i4 55
	  ldc.i4 39
	  ldc.i4 118
	  stsfld int32 EHTest::fld1
	  ldc.i4 12
	  stsfld int32 EHTest::fld1
	  ldc.i4 64
	  ldc.i4 101
	  stloc.0
	  ldc.i4 97
	  stsfld int32 EHTest::fld0
	  ldc.i4 27
	  ldc.i4 28
	  stsfld int32 EHTest::fld1
	  ldc.i4.7
	  stsfld int32 EHTest::fld0
	  ldc.i4.0
	  ldc.i4 103
	  stsfld int32 EHTest::fld0
	  ldc.i4 25
	  ldc.i4 102
	  ldc.i4 44
	  ldc.i4.2
	  ldc.i4 103
	  stloc.0
	  ldc.i4 24
	  ldc.i4 83
	  ldc.i4 84
	  ldc.i4 39
	  ldc.i4 13
	  ldc.i4 70
	  ldc.i4 103
	  stloc.2
	  ldc.i4 104
	  ldc.i4 101
	  ldc.i4 57
	  stsfld int32 EHTest::fld1
	  ldc.i4 113
	  newobj instance void int64[...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...]::.ctor(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4 105
	  stloc.0
	  ldc.i4 25
	  ldc.i4 66
	  ldc.i4 40
	  ldc.i4.6
	  stloc.2
	  ldc.i4 18
	  ldc.i4 49
	  ldc.i4 110
	  ldc.i4.5
	  ldc.i4 73
	  ldc.i4.8
	  stloc.0
	  ldc.i4 113
	  ldc.i4 31
	  ldc.i4 123
	  ldc.i4.1
	  ldc.i4 13
	  ldc.i4.2
	  ldc.i4 62
	  stloc.0
	  ldc.i4 69
	  ldc.i4 72
	  ldc.i4.6
	  stsfld int32 EHTest::fld0
	  ldc.i4 71
	  ldc.i4 87
	  stsfld int32 EHTest::fld0
	  ldc.i4 69
	  ldc.i4 70
	  stsfld int32 EHTest::fld0
	  ldc.i4 27
	  stsfld int32 EHTest::fld1
	  ldc.i4 73
	  ldc.i4 66
	  ldc.i4 79
	  stsfld int32 EHTest::fld0
	  ldc.i4.2
	  ldc.i4 73
	  ldc.i4 108
	  stsfld int32 EHTest::fld1
	  ldc.i4 78
	  ldc.i4 57
	  call instance int64 int64[...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...]::Get(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  stloc 4
	  ldc.i8 19
	  ldc.i8 36
	  xor
	  ble.un	TARGET_5_3
	TARGET_5_1:
	  ldarg.0
	  stsfld int64 EHTest::fld2
	  ldc.i8 65
	  stsfld int64 EHTest::fld2
	  ldc.i8 23
	  ldc.i8 45
	  add
	  conv.i8
	  ldc.i8 97
	  ble.un	TARGET_5_4
	TARGET_5_2:
	  ldftn int32 EHTest::GetInt32_jmp()
	  calli int32 ()
	  ldc.i4 50
	  stsfld int32 EHTest::fld0
	  ldc.i4 110
	  bgt.un	TARGET_5_3
	TARGET_5_3:
	  call int64 EHTest::GetInt64()
	  ldloc 4
	  stloc 4
	  ldc.i8 60
	  bgt	TARGET_5_2
	TARGET_5_4:
	  ldc.i4 65
	  stloc.0
	  ldloc.2
	  stsfld int32 EHTest::fld0
	  ldc.i4 120
	  ldc.i4 77
	  or
	  not
	  ldc.i4 43
	  stsfld int32 EHTest::fld1
	  ldc.i4 21
	  bgt	TARGET_5_2
try1_out:
newobj instance void [mscorlib]System.ArgumentException::.ctor()
throw
end_try1: 

OUT: 
	  ldsfld int64 EHTest::fld2
ret
OUT0:
nop
	TARGET_6_0:
	  ldsflda int32 EHTest::fld0
	  mkrefany   [mscorlib]System.Int32
	  refanyval	[mscorlib]System.Int32
	  ldind.i4
	  stsfld int32 EHTest::fld0
	  ldc.i4 32
	  ldc.i4 15
	  clt
	  ldc.i4 99
	  ble.un	TARGET_6_2
	TARGET_6_1:
	  ldc.i8 73
	  ldc.i8 55
	  xor
	  stsfld int64 EHTest::fld2
	  ldc.i8 96
	  ldc.i8 110
	  stsfld int64 EHTest::fld2
	  ldc.i8 97
	  mul
	  neg
	  ldc.i8 83
	  blt.un	TARGET_6_1
	TARGET_6_2:
	  call int32 EHTest::GetInt32_jmp()
	  stsfld int32 EHTest::fld1
	  ldc.i4 45
	  stsfld int32 EHTest::fld1
	  ldc.i4 70
	  ldc.i4 45
	  clt
	  ldc.i4.2
	  blt	TARGET_6_2
	TARGET_6_3:
	  ldc.i8 52
	  neg
	  not
	  ldc.i8 76
	  sub
	  starg.s arg0
	  ldc.i8 39
	  ldc.i8 55
	  add
	  ldc.i8 32
	  starg.s arg0
	  ldc.i8 105
	  bgt	TARGET_6_0
	TARGET_6_4:
	  ldc.i8 88
	  ldc.i8 12
	  ble	TARGET_6_0
br start_try2
start_catch1: 
pop
br catch1_br
start_finally1: 
nop
	TARGET_7_0:
	  sizeof native unsigned int
	  ldc.i4 103
	  ldc.i4 110
	  rem
	  mul
	  stsfld int32 EHTest::fld0
	  ldc.i4 54
	  newarr     [mscorlib]System.Int32
	  ldc.i4.7
	  ldelem.i4
	  ldc.i4 63
	  ble.un	TARGET_7_4
	TARGET_7_1:
	  ldloca 4
	  mkrefany   [mscorlib]System.Int64
	  refanyval	[mscorlib]System.Int64
	  ldind.i8
	  stsfld int64 EHTest::fld2
	  ldc.i8 7
	  ldc.i8 13
	  starg.s arg0
	  ldc.i8 98
	  starg.s arg0
	  ldc.i8 114
	  or
	  stsfld int64 EHTest::fld2
	  ldc.i8 101
	  ldc.i8 1
	  stsfld int64 EHTest::fld2
	  ldc.i8 121
	  bge.un	TARGET_7_0
	TARGET_7_2:
	  ldc.i4 69
	  ldc.i4 60
	  rem
	  ldc.i4 44
	  clt
	  stsfld int32 EHTest::fld0
	  ldc.i4 118
	  stsfld int32 EHTest::fld0
	  ldc.i4 81
	  ldc.i4 29
	  beq	TARGET_7_2
	TARGET_7_3:
	  ldloc.2
	  ldc.i4 109
	  bge.un	TARGET_7_2
	TARGET_7_4:
	  ldc.i4 62
	  ldc.i4 95
	  bgt	TARGET_7_0
finally1_out: endfinally
end_finally1: 


catch1_br: 
start_try3: 
try3_out:
leave catch1_out
end_try3: 

catch1_out:
leave OUT1
end_catch1: 


exception_filter1: 
pop
ldc.i4.1
endfilter
start_filter1: 
pop
br filter1_br
start_finally2: 
nop
	TARGET_8_0:
	  ldftn int64 EHTest::GetInt64_tailcall()
	  calli int64 ()
	  conv.i4
	  stloc.0
	  ldc.i4 23
	  stloc.2
	  ldc.i4 63
	  ldc.i4 116
	  stloc.0
	  ldc.i4.6
	  mul
	  ldc.i4 80
	  sub
	  stsfld int32 EHTest::fld1
	  ldc.i4 117
	  ldc.i4 11
	  ldc.i4 76
	  stsfld int32 EHTest::fld0
	  ldc.i4 20
	  stsfld int32 EHTest::fld1
	  ldc.i4 84
	  stloc.0
	  ldc.i4 108
	  ldc.i4 97
	  ldc.i4.2
	  ldc.i4 79
	  stsfld int32 EHTest::fld0
	  ldc.i4 115
	  stloc.2
	  ldc.i4 50
	  stsfld int32 EHTest::fld0
	  ldc.i4 126
	  ldc.i4 125
	  ldc.i4 79
	  ldc.i4 22
	  ldc.i4 51
	  ldc.i4 109
	  ldc.i4 90
	  stloc.2
	  ldc.i4 72
	  ldc.i4.6
	  ldc.i4 112
	  ldc.i4 65
	  ldc.i4 120
	  stloc.2
	  ldc.i4 21
	  ldc.i4 14
	  newobj instance void int64[,,,,,,,,,,,,,,,,]::.ctor(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4 36
	  ldc.i4 72
	  ldc.i4 117
	  stloc.2
	  ldc.i4 91
	  stsfld int32 EHTest::fld0
	  ldc.i4 72
	  ldc.i4 10
	  ldc.i4.6
	  ldc.i4 89
	  ldc.i4 63
	  ldc.i4 86
	  ldc.i4 15
	  ldc.i4 66
	  ldc.i4 120
	  ldc.i4 108
	  ldc.i4 111
	  ldc.i4 82
	  ldc.i4 85
	  ldc.i4 122
	  ldc.i4 15
	  call instance int64 int64[,,,,,,,,,,,,,,,,]::Get(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i8 47
	  stsfld int64 EHTest::fld2
	  ldc.i8 1
	  div
	  ldc.i8 19
	  stsfld int64 EHTest::fld2
	  ldc.i8 2
	  ble	TARGET_8_1
	TARGET_8_1:
	  ldc.i4 80
	  stsfld int32 EHTest::fld1
	  ldc.i4 20
	  ldc.i4 23
	  stloc.2
	  ldc.i4 66
	  sub
	  ldc.i4 117
	  rem
	  ldc.i4.4
	  bge.un	TARGET_8_2
	TARGET_8_2:
	  ldftn int64 EHTest::GetInt64()
	  calli int64 ()
	  stsfld int64 EHTest::fld2
	  ldc.i8 75
	  stsfld int64 EHTest::fld2
	  ldc.i8 122
	  ldc.i8 77
	  bge	TARGET_8_0
	TARGET_8_3:
	  ldloc.0
	  stsfld int32 EHTest::fld1
	  ldc.i4 42
	  ldc.i4 58
	  mul
	  stsfld int32 EHTest::fld1
	  ldc.i4 120
	  stsfld int32 EHTest::fld0
	  ldc.i4 16
	  ldc.i4 88
	  bgt.un	TARGET_8_2
	TARGET_8_4:
	  ldsflda int64 EHTest::fld2
	  mkrefany   [mscorlib]System.Int64
	  refanyval	[mscorlib]System.Int64
	  ldind.i8
	  ldftn int32 EHTest::GetInt32_jmp()
	  calli int32 ()
	  newarr     [mscorlib]System.Int64
	  ldsfld int32 EHTest::fld0
	  ldc.i4 25
	  cgt
	  ldelem.i8
	  bgt.un	TARGET_8_1
finally2_out: endfinally
end_finally2: 


filter1_br: 
start_try4: 
nop
	TARGET_9_0:
	  call int64 EHTest::GetInt64_jmp()
	  stloc 4
	  ldc.i8 55
	  ldc.i8 99
	  starg.s arg0
	  ldc.i8 99
	  starg.s arg0
	  ldc.i8 65
	  stsfld int64 EHTest::fld2
	  ldc.i8 80
	  stloc.3
	  ldc.i8 17
	  blt.un	TARGET_9_3
	TARGET_9_1:
	  ldc.i8 74
	  ldc.i8 33
	  or
	  conv.i8
	  ldc.i8 36
	  div
	  stloc 4
	  ldc.i8 97
	  stsfld int64 EHTest::fld2
	  ldc.i8 125
	  ldc.i8 109
	  sub
	  ldc.i8 25
	  beq	TARGET_9_2
	TARGET_9_2:
	  ldc.i8 115
	  ldc.i8 126
	  stsfld int64 EHTest::fld2
	  ldc.i8 49
	  div
	  stloc.3
	  ldc.i8 29
	  box int64
	  unbox int64
	  ldind.i8
	  ldc.i8 44
	  starg.s arg0
	  ldc.i8 1
	  bgt	TARGET_9_4
	TARGET_9_3:
	  ldarg.0
	  box int64
	  unbox int64
	  ldind.i8
	  ldc.i8 19
	  bge.un	TARGET_9_3
	TARGET_9_4:
	  ldc.i4 106
	  stloc.0
	  ldc.i4 87
	  stsfld int32 EHTest::fld0
	  ldc.i4 29
	  stsfld int32 EHTest::fld0
	  ldc.i4 63
	  stloc.2
	  ldc.i4 39
	  ldc.i4 84
	  or
	  ldc.i4 13
	  shl
	  ldc.i4.1
	  beq	TARGET_9_4
try4_out:
leave filter1_out
end_try4: 

filter1_out: leave OUT1
end_filter1: 

start_try2: 
call void EHTest::f2()
try2_out:
newobj instance void [mscorlib]System.ContextMarshalException::.ctor()
throw
end_try2: 

exception_filter2: 
pop
ldc.i4.1
endfilter
start_filter2: 
pop
call void EHTest::f2()
filter2_out: leave OUT1
end_filter2: 

exception_filter3: 
pop
ldc.i4.1
endfilter
start_filter3: 
pop
filter3_out: leave OUT1
end_filter3: 
OUT1:
leave OUT
.try start_try4 to end_try4 finally handler start_finally2 to end_finally2
.try start_try3 to end_try3 finally handler start_finally1 to end_finally1
.try start_try2 to end_try2 catch [mscorlib]System.ArgumentException handler start_catch1 to end_catch1
.try start_try2 to end_try2 filter exception_filter1 handler start_filter1 to end_filter1
.try start_try2 to end_try2 filter exception_filter2 handler start_filter2 to end_filter2
.try start_try2 to end_try2 filter exception_filter3 handler start_filter3 to end_filter3
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
	  ldc.i8 75
	  stsfld int64 EHTest::fld2
	  ldsfld int64 EHTest::fld2
	  ldc.i8 68
	  mul
	  box int64
	  unbox int64
	  ldind.i8
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
