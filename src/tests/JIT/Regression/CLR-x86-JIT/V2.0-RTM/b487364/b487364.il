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
  .field private static int32 fld0
  .field private static int64 fld1
  .field private static int64 fld2
  .field private static int64 fld3
  .field private static int32 fld4
  .field private static int64 fld5

  .method public static int32 GetInt32()
  {
	ldc.i4 195527541
	ret
  }
  .method public static int64 GetInt64()
  {
	ldc.i4 282825993
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

.method public static int32 f1(int32 arg0,int64 arg1,int64 arg2,int32 arg3) cil managed
{
.maxstack 500
.locals init (int32 V_0,
		int64 V_1,
		int32 V_2,
		int32 V_3,
		int64 V_4)
br start_try1
start_try1: 
call void EHTest::f2()
try1_out:
newobj instance void [mscorlib]System.MulticastNotSupportedException::.ctor()
throw
end_try1: 

start_catch1: 
catch1_out:
rethrow
end_catch1: 


start_catch2: 
pop
br catch2_br
start_catch3: 
pop
br catch3_br
start_finally1: 
call void EHTest::f2()
finally1_out: endfinally
end_finally1: 


catch3_br: 
start_try3: 
try3_out:
leave catch3_out
end_try3: 

catch3_out:
nop
	TARGET_4_0:
	  ldarg.0
	  ldc.i4 98
	  beq	TARGET_4_0
	TARGET_4_1:
	  ldsfld int32 EHTest::fld4
	  ldsfld int32 EHTest::fld0
	  starg.s arg3
	  ldc.i4 62
	  bge.un	TARGET_4_4
	TARGET_4_2:
	  ldc.i4 86
	  stloc.2
	  ldsfld int32 EHTest::fld0
	  ldc.i4 65
	  stsfld int32 EHTest::fld0
	  ldc.i4 43
	  stsfld int32 EHTest::fld0
	  ldc.i4 10
	  starg.s arg0
	  ldc.i4 33
	  blt.un	TARGET_4_4
	TARGET_4_3:
	  ldc.i4 59
	  ldc.i4 5
	  shr
	  starg.s arg0
	  ldc.i4 81
	  stsfld int32 EHTest::fld4
	  ldc.i4 34
	  ldc.i4 119
	  stsfld int32 EHTest::fld4
	  ldc.i4 93
	  ceq
	  starg.s arg0
	  ldc.i4 36
	  ldc.i4 93
	  starg.s arg0
	  ldc.i4 23
	  ceq
	  conv.i4
	  ldc.i4 93
	  stsfld int32 EHTest::fld4
	  ldc.i4 24
	  bgt.un	TARGET_4_0
	TARGET_4_4:
	  ldarg.1
	  starg.s arg2
	  ldc.i8 23
	  ldc.i8 8
	  not
	  blt.un	TARGET_4_3
newobj instance void [mscorlib]System.TypeLoadException::.ctor()
throw
end_catch3: 


exception_filter1: 
pop
ldc.i4.1
endfilter
start_filter1: 
pop
br filter1_br
start_catch4: 
catch4_out:
nop
	TARGET_5_0:
	  call int32 EHTest::GetInt32_jmp()
	  ldc.i4 117
	  ldc.i4 13
	  shr
	  box int32
	  unbox int32
	  ldind.i4
	  box int32
	  unbox int32
	  ldind.i4
	  ldc.i4 32
	  stsfld int32 EHTest::fld0
	  ldc.i4 44
	  ldc.i4 57
	  ldc.i4 21
	  ldc.i4 78
	  ldc.i4.1
	  ldc.i4 80
	  starg.s arg3
	  ldc.i4 126
	  ldc.i4 10
	  ldc.i4 105
	  stloc.3
	  ldc.i4 21
	  stsfld int32 EHTest::fld4
	  ldc.i4 91
	  ldc.i4 90
	  ldc.i4 25
	  ldc.i4 26
	  ldc.i4 87
	  ldc.i4 33
	  starg.s arg3
	  ldc.i4 77
	  ldc.i4 75
	  ldc.i4 39
	  newobj instance void int64[41...42,11...16,96...113,103...105,108...118,39...53,37...38,115...127,53...90,103...126,13...14,41...86,70...126,115...120,29...113,83...125,79...91]::.ctor(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4 110
	  ldc.i4 78
	  stsfld int32 EHTest::fld4
	  ldc.i4 72
	  ldc.i4 103
	  ldc.i4.8
	  stloc.2
	  ldc.i4.1
	  ldc.i4 32
	  stloc.0
	  ldc.i4 46
	  ldc.i4.5
	  ldc.i4 122
	  ldc.i4 62
	  stsfld int32 EHTest::fld4
	  ldc.i4 21
	  ldc.i4 92
	  ldc.i4 15
	  starg.s arg0
	  ldc.i4 75
	  ldc.i4 86
	  ldc.i4 14
	  ldc.i4 57
	  ldc.i4 27
	  ldc.i4 41
	  ldc.i4 35
	  ldc.i4 41
	  stsfld int32 EHTest::fld4
	  ldc.i4 61
	  call instance int64 int64[41...42,11...16,96...113,103...105,108...118,39...53,37...38,115...127,53...90,103...126,13...14,41...86,70...126,115...120,29...113,83...125,79...91]::Get(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i8 2
	  stloc.1
	  ldc.i8 46
	  starg.s arg1
	  ldc.i8 83
	  starg.s arg1
	  ldc.i8 8
	  starg.s arg1
	  ldc.i8 39
	  starg.s arg1
	  ldc.i8 69
	  ldc.i8 102
	  starg.s arg1
	  ldc.i8 127
	  rem
	  ldc.i8 34
	  rem
	  neg
	  beq	TARGET_5_2
	TARGET_5_1:
	  ldc.i8 36
	  ldarg.1
	  ldc.i8 55
	  div
	  ldc.i8 59
	  add
	  stloc.1
	  ldc.i8 123
	  blt.un	TARGET_5_0
	TARGET_5_2:
	  sizeof [mscorlib]System.UIntPtr
	  ldc.i4 120
	  ldc.i4 126
	  sub
	  add
	  ldc.i4 106
	  clt
	  ldc.i4 92
	  bge	TARGET_5_2
	TARGET_5_3:
	  ldarga arg1
	  mkrefany   [mscorlib]System.Int64
	  refanyval	[mscorlib]System.Int64
	  ldind.i8
	  ldloca 3
	  mkrefany   [mscorlib]System.Int32
	  refanyval	[mscorlib]System.Int32
	  ldind.i4
	  ldc.i4 127
	  starg.s arg3
	  ldc.i4 118
	  clt
	  ldc.i4 99
	  div
	  neg
	  starg.s arg0
	  ldc.i4 96
	  starg.s arg3
	  ldc.i4 119
	  ldc.i4 110
	  stloc.3
	  ldc.i4 99
	  stloc.3
	  ldc.i4 95
	  ldc.i4 58
	  ldc.i4 34
	  ldc.i4 62
	  ldc.i4.6
	  ldc.i4 45
	  ldc.i4 125
	  ldc.i4 32
	  ldc.i4 48
	  ldc.i4 116
	  stsfld int32 EHTest::fld0
	  ldc.i4 34
	  ldc.i4 107
	  ldc.i4 95
	  ldc.i4 121
	  ldc.i4 11
	  starg.s arg3
	  ldc.i4 44
	  ldc.i4 119
	  ldc.i4 76
	  ldc.i4 106
	  ldc.i4 13
	  ldc.i4 89
	  newobj instance void int64[38...95,116...118,88...116,59...65,5...120,16...67,10...85,41...95,112...115,14...18,18...19,4...62,107...118,43...121,123...125,32...112,2...46,94...101,51...127,89...91]::.ctor(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4 38
	  ldc.i4 33
	  ldc.i4 39
	  stloc.2
	  ldc.i4 126
	  ldc.i4.8
	  ldc.i4 90
	  ldc.i4 36
	  stloc.0
	  ldc.i4.4
	  stloc.2
	  ldc.i4 64
	  stloc.2
	  ldc.i4 37
	  stsfld int32 EHTest::fld0
	  ldc.i4 64
	  stsfld int32 EHTest::fld0
	  ldc.i4 13
	  ldc.i4 107
	  starg.s arg0
	  ldc.i4 88
	  ldc.i4 43
	  ldc.i4 98
	  ldc.i4 18
	  stloc.0
	  ldc.i4 68
	  ldc.i4 119
	  starg.s arg0
	  ldc.i4 123
	  starg.s arg0
	  ldc.i4 106
	  ldc.i4 126
	  ldc.i4 127
	  stloc.0
	  ldc.i4 106
	  ldc.i4 88
	  stsfld int32 EHTest::fld0
	  ldc.i4.3
	  ldc.i4 93
	  ldc.i4.0
	  ldc.i4 21
	  ldc.i4 24
	  ldc.i4 56
	  ldc.i4 58
	  call instance int64 int64[38...95,116...118,88...116,59...65,5...120,16...67,10...85,41...95,112...115,14...18,18...19,4...62,107...118,43...121,123...125,32...112,2...46,94...101,51...127,89...91]::Get(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i8 127
	  mul
	  starg.s arg2
	  ldc.i8 71
	  starg.s arg2
	  ldc.i8 123
	  starg.s arg1
	  ldc.i8 30
	  stsfld int64 EHTest::fld3
	  ldc.i8 38
	  bgt.un	TARGET_5_4
	TARGET_5_4:
	  ldc.i4 115
	  ldftn int32 EHTest::GetInt32()
	  calli int32 ()
	  starg.s arg3
	  ldc.i4 54
	  stloc.3
	  ldc.i4 113
	  bgt	TARGET_5_0
rethrow
end_catch4: 


start_catch5: 
pop
catch5_out:
call void EHTest::f2()
newobj instance void [mscorlib]System.InvalidCastException::.ctor()
throw
end_catch5: 


start_catch6: 
pop
catch6_out:
call void EHTest::f2()
leave filter1_out
end_catch6: 


exception_filter2: 
pop
ldc.i4.1
endfilter
start_filter2: 
pop
filter2_out: leave filter1_out
end_filter2: 

exception_filter3: 
pop
ldc.i4.1
endfilter
start_filter3: 
pop
nop
	TARGET_6_0:
	  ldloc.0
	  ldc.i4 115
	  sub
	  newarr     [mscorlib]System.Int32
	  ldc.i4.5
	  stsfld int32 EHTest::fld4
	  ldc.i4 59
	  ldelem.i4
	  ldc.i4 120
	  blt	TARGET_6_0
	TARGET_6_1:
	  ldarga arg3
	  mkrefany   [mscorlib]System.Int32
	  refanyval	[mscorlib]System.Int32
	  ldind.i4
	  ldc.i4 56
	  sub
	  ldc.i4 41
	  blt.un	TARGET_6_1
	TARGET_6_2:
	  ldc.i4 19
	  ldc.i4 97
	  ceq
	  ldc.i4 12
	  or
	  starg.s arg3
	  ldc.i4 79
	  neg
	  ldc.i4.2
	  blt	TARGET_6_0
	TARGET_6_3:
	  call int32 EHTest::GetInt32()
	  ldloc.0
	  ldc.i4.5
	  neg
	  ldc.i4 49
	  starg.s arg3
	  ldc.i4 63
	  ldc.i4 28
	  stsfld int32 EHTest::fld0
	  ldc.i4 20
	  starg.s arg3
	  ldc.i4 47
	  starg.s arg0
	  ldc.i4 44
	  stsfld int32 EHTest::fld0
	  ldc.i4 116
	  stsfld int32 EHTest::fld0
	  ldc.i4 62
	  stsfld int32 EHTest::fld0
	  ldc.i4 93
	  stloc.0
	  ldc.i4 27
	  ldc.i4 78
	  starg.s arg0
	  ldc.i4 67
	  ldc.i4 58
	  ldc.i4 79
	  stloc.3
	  ldc.i4 28
	  ldc.i4 24
	  stloc.0
	  ldc.i4 55
	  ldc.i4 84
	  ldc.i4 79
	  ldc.i4 9
	  ldc.i4 49
	  starg.s arg3
	  ldc.i4 96
	  ldc.i4 96
	  ldc.i4.5
	  ldc.i4.5
	  stsfld int32 EHTest::fld0
	  ldc.i4 31
	  ldc.i4 61
	  ldc.i4 31
	  ldc.i4 81
	  ldc.i4 74
	  ldc.i4 122
	  stsfld int32 EHTest::fld0
	  ldc.i4 100
	  ldc.i4 18
	  ldc.i4 50
	  newobj instance void int64[117,77,75,30,9,40,113,19,116,109,101,59,102,91,49,62,22,104,47,127,90,11,11]::.ctor(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4 127
	  starg.s arg3
	  ldc.i4 113
	  stloc.2
	  ldc.i4 95
	  starg.s arg0
	  ldc.i4 126
	  ldc.i4 37
	  ldc.i4 53
	  ldc.i4 12
	  stloc.0
	  ldc.i4 95
	  ldc.i4 84
	  ldc.i4 68
	  ldc.i4 29
	  ldc.i4 51
	  stsfld int32 EHTest::fld0
	  ldc.i4 126
	  starg.s arg0
	  ldc.i4 49
	  starg.s arg3
	  ldc.i4 73
	  ldc.i4 39
	  starg.s arg0
	  ldc.i4 112
	  starg.s arg0
	  ldc.i4 116
	  ldc.i4 97
	  ldc.i4.8
	  ldc.i4 91
	  stloc.0
	  ldc.i4 84
	  ldc.i4 42
	  ldc.i4 37
	  ldc.i4 94
	  stsfld int32 EHTest::fld4
	  ldc.i4 109
	  stloc.3
	  ldc.i4 121
	  ldc.i4 50
	  ldc.i4 74
	  ldc.i4 85
	  ldc.i4.3
	  ldc.i4 45
	  ldc.i4 53
	  starg.s arg0
	  ldc.i4 122
	  ldc.i4 9
	  ldc.i4 93
	  stloc.0
	  ldc.i4 33
	  stsfld int32 EHTest::fld4
	  ldc.i4 106
	  stloc.0
	  ldc.i4 28
	  call instance int64 int64[117,77,75,30,9,40,113,19,116,109,101,59,102,91,49,62,22,104,47,127,90,11,11]::Get(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i8 74
	  starg.s arg1
	  ldc.i8 69
	  xor
	  ldc.i8 18
	  rem
	  ldc.i8 12
	  bgt.un	TARGET_6_2
	TARGET_6_4:
	  ldsflda int32 EHTest::fld4
	  mkrefany   [mscorlib]System.Int32
	  refanyval	[mscorlib]System.Int32
	  ldind.i4
	  stloc.3
	  ldc.i4 120
	  stsfld int32 EHTest::fld4
	  ldc.i4 76
	  ldc.i4 57
	  add
	  stloc.3
	  ldc.i4 76
	  ldc.i4 43
	  beq	TARGET_6_2
filter3_out: leave filter1_out
end_filter3: 

filter1_br: 
start_try4: 
try4_out:
leave filter1_out
end_try4: 

exception_filter4: 
pop
ldc.i4.0
endfilter
start_filter4: 
pop
call void EHTest::f2()
filter4_out: leave filter1_out
end_filter4: 

filter1_out: leave catch2_out
end_filter1: 

exception_filter5: 
pop
ldc.i4.1
endfilter
start_filter5: 
pop
filter5_out: leave catch2_out
end_filter5: 

exception_filter6: 
pop
ldc.i4.1
endfilter
start_filter6: 
pop
call void EHTest::f2()
filter6_out: leave catch2_out
end_filter6: 

catch2_br: 
start_try2: 
try2_out:
leave catch2_out
end_try2: 

catch2_out:
leave OUT0
end_catch2: 


start_catch7: 
pop
catch7_out:
newobj instance void [mscorlib]System.FieldAccessException::.ctor()
throw
end_catch7: 

OUT: 
	  ldc.i4 18
	  stsfld int32 EHTest::fld0
	  ldc.i4 33
	  starg.s arg3
	  ldc.i4 105
ret
OUT0:
leave OUT
.try start_try4 to end_try4 catch [mscorlib]System.TypeLoadException handler start_catch4 to end_catch4
.try start_try4 to end_try4 catch [mscorlib]System.TypeUnloadedException handler start_catch5 to end_catch5
.try start_try4 to end_try4 catch [mscorlib]System.InvalidCastException handler start_catch6 to end_catch6
.try start_try4 to end_try4 filter exception_filter2 handler start_filter2 to end_filter2
.try start_try4 to end_try4 filter exception_filter3 handler start_filter3 to end_filter3
.try start_try4 to end_try4 filter exception_filter4 handler start_filter4 to end_filter4
.try start_try3 to end_try3 finally handler start_finally1 to end_finally1
.try start_try2 to end_try2 catch [mscorlib]System.NotFiniteNumberException handler start_catch3 to end_catch3
.try start_try2 to end_try2 filter exception_filter1 handler start_filter1 to end_filter1
.try start_try2 to end_try2 filter exception_filter5 handler start_filter5 to end_filter5
.try start_try2 to end_try2 filter exception_filter6 handler start_filter6 to end_filter6
.try start_try1 to end_try1 catch [mscorlib]System.MulticastNotSupportedException handler start_catch1 to end_catch1
.try start_try1 to end_try1 catch [mscorlib]System.OutOfMemoryException handler start_catch2 to end_catch2
.try start_try1 to end_try1 catch [mscorlib]System.Exception handler start_catch7 to end_catch7
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
		int64 V_3)
.try
{
	  ldc.i4 73
	  stloc.0
	  ldc.i4 94
	  ldc.i4 111
	  conv.i8
	  ldc.i8 46
	  stloc.1
	  ldc.i8 20
	  stsfld int64 EHTest::fld3
	  ldc.i8 92
	  stsfld int64 EHTest::fld2
	  ldc.i8 84
	  ldc.i4 122
call int32 EHTest::f1(int32 ,int64 ,int64 ,int32 )
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
