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
  .field private static int64 fld2
  .field private static int64 fld3
  .field private static int32 fld4
  .field private static int64 fld5
  .field private static int64 fld6

  .method public static int32 GetInt32()
  {
	ldc.i4 1229647333
	ret
  }
  .method public static int64 GetInt64()
  {
	ldc.i4 198558402
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
.method public static int32 f0() cil managed
{
.maxstack 500
.locals init (int32 V_0,
		int64 V_1,
		int32 V_2,
		int32 V_3,
		int32 V_4)
br start_try1
start_fault1: 
br fault1_br
start_finally1: 
nop
	TARGET_0_0:
	  ldc.i8 62
	  ldc.i8 122
	  add
	  ldc.i8 82
	  bgt	TARGET_0_4
	TARGET_0_1:
	  ldc.i8 111
	  stsfld int64 EHTest::fld5
	  ldc.i8 63
	  ldc.i8 118
	  add
	  stsfld int64 EHTest::fld3
	  ldc.i8 121
	  ldc.i8 109
	  blt	TARGET_0_0
	TARGET_0_2:
	  ldc.i4 57
	  not
	  ldc.i4 5
	  shr
	  ldc.i4 75
	  stloc 4
	  ldc.i4 83
	  stsfld int32 EHTest::fld4
	  ldc.i4 18
	  ble	TARGET_0_3
	TARGET_0_3:
	  ldc.i4 11
	  ldc.i4 36
	  or
	  newarr     [mscorlib]System.Int32
	  ldc.i4 32
	  stloc.0
	  ldc.i4 68
	  ldelem.i4
	  newarr     [mscorlib]System.Int64
	  ldc.i4 95
	  ldelem.i8
	  ldc.i8 62
	  div
	  ldc.i8 0
	  bge	TARGET_0_2
	TARGET_0_4:
	  ldc.i8 73
     stloc 1
     ldloca 1
	  mkrefany   [mscorlib]System.Int64
	  refanyval	[mscorlib]System.Int64
	  ldind.i8
	  ldsfld int64 EHTest::fld3
	  stloc.1
	  ldc.i8 56
	  neg
	  ble	TARGET_0_0
finally1_out: endfinally
end_finally1: 


fault1_br: 
start_try2: 
try2_out:
newobj instance void [mscorlib]System.DllNotFoundException::.ctor()
throw
end_try2: 

fault1_out: endfault
end_fault1: 


start_try1: 
try1_out:
newobj instance void [mscorlib]System.EntryPointNotFoundException::.ctor()
throw
end_try1: 

OUT: 
	  ldc.i4 74
	  stloc.0
	  ldc.i4 56
	  stloc.0
	  ldc.i4 102
	  stloc.2
	  ldc.i4 60
	  stsfld int32 EHTest::fld4
	  ldc.i4 96
ret
OUT0:
leave OUT
.try start_try2 to end_try2 finally handler start_finally1 to end_finally1
.try start_try1 to end_try1 fault handler start_fault1 to end_fault1
}

.method public static int32 f1(int64 arg0,int64 arg1) cil managed
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
start_finally2: 
br finally2_br
finally2_br: 
start_try3: 
try3_out:
newobj instance void [mscorlib]System.PlatformNotSupportedException::.ctor()
throw
end_try3: 

start_fault1: 
fault1_out: endfault
end_fault1: 


finally2_out: endfinally
end_finally2: 


try1_br: 
start_try2: 
br try2_br
start_fault2: 
fault2_out: endfault
end_fault2: 


try2_br: 
start_try4: 
nop
	TARGET_1_0:
	  ldc.i4 35
	  ldc.i4 28
	  shr
	  ldc.i4 83
	  ldc.i4 113
	  ldc.i4 99
	  stloc.0
	  ldc.i4 43
	  stsfld int32 EHTest::fld4
	  ldc.i4 22
	  ldc.i4 125
	  stloc.0
	  ldc.i4 92
	  ldc.i4.5
	  stloc.0
	  ldc.i4 83
	  ldc.i4 54
	  ldc.i4 23
	  ldc.i4 20
	  stsfld int32 EHTest::fld4
	  ldc.i4 73
	  ldc.i4 106
	  ldc.i4 111
	  stsfld int32 EHTest::fld4
	  ldc.i4 58
	  stsfld int32 EHTest::fld4
	  ldc.i4 113
	  stloc.0
	  ldc.i4 72
	  ldc.i4 19
	  ldc.i4 87
	  newobj instance void int32[,,,,,,,,,,,,]::.ctor(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4 46
	  ldc.i4 51
	  ldc.i4 92
	  ldc.i4 32
	  ldc.i4 124
	  stsfld int32 EHTest::fld4
	  ldc.i4 47
	  ldc.i4.6
	  ldc.i4 64
	  ldc.i4 18
	  ldc.i4 16
	  stsfld int32 EHTest::fld4
	  ldc.i4.8
	  ldc.i4 21
	  stsfld int32 EHTest::fld4
	  ldc.i4 47
	  ldc.i4 13
	  ldc.i4.3
	  ldc.i4 27
	  call instance int32 int32[,,,,,,,,,,,,]::Get(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4 98
	  stsfld int32 EHTest::fld4
	  ldc.i4 21
	  bge	TARGET_1_4
	TARGET_1_1:
	  ldarg.1
	  starg.s arg0
	  ldc.i8 87
	  ldc.i8 66
	  bgt	TARGET_1_1
	TARGET_1_2:
	  ldarg.1
	  ldftn int64 EHTest::GetInt64_tailcall()
	  calli int64 ()
	  conv.i4
	  ldc.i4 21
	  ldc.i4 47
	  ldc.i4 82
	  ldc.i4 80
	  ldc.i4 94
	  stsfld int32 EHTest::fld4
	  ldc.i4 29
	  stsfld int32 EHTest::fld4
	  ldc.i4 98
	  stsfld int32 EHTest::fld4
	  ldc.i4 90
	  ldc.i4 61
	  ldc.i4.3
	  stloc.0
	  ldc.i4 61
	  ldc.i4 107
	  ldc.i4 115
	  ldc.i4 41
	  ldc.i4 103
	  ldc.i4 64
	  ldc.i4 18
	  stloc.0
	  ldc.i4.8
	  ldc.i4 96
	  ldc.i4 74
	  ldc.i4 122
	  ldc.i4 79
	  stloc.0
	  ldc.i4 124
	  ldc.i4 30
	  stloc.0
	  ldc.i4 32
	  ldc.i4 62
	  stsfld int32 EHTest::fld4
	  ldc.i4 45
	  ldc.i4.0
	  ldc.i4 112
	  ldc.i4.5
	  stsfld int32 EHTest::fld4
	  ldc.i4 114
	  stloc.0
	  ldc.i4 28
	  newobj instance void int32[55...85,102...113,84...104,55...104,43...83,64...93,118...122,67...71,76...105,83...123,115...124,16...56,117...127,26...104,51...97,60...104,88...110,18...80,27...47,119...123,103...122,111...112]::.ctor(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4 99
	  stsfld int32 EHTest::fld4
	  ldc.i4.2
	  stsfld int32 EHTest::fld4
	  ldc.i4 32
	  ldc.i4 51
	  ldc.i4 122
	  ldc.i4 11
	  ldc.i4 37
	  ldc.i4 43
	  ldc.i4 105
	  ldc.i4 120
	  ldc.i4 101
	  stsfld int32 EHTest::fld4
	  ldc.i4 101
	  ldc.i4 73
	  ldc.i4 91
	  stsfld int32 EHTest::fld4
	  ldc.i4 121
	  ldc.i4 71
	  stsfld int32 EHTest::fld4
	  ldc.i4 37
	  stloc.0
	  ldc.i4 34
	  ldc.i4.3
	  stsfld int32 EHTest::fld4
	  ldc.i4.3
	  stsfld int32 EHTest::fld4
	  ldc.i4.3
	  ldc.i4 85
	  ldc.i4 28
	  ldc.i4 102
	  stsfld int32 EHTest::fld4
	  ldc.i4 12
	  stloc.0
	  ldc.i4.7
	  ldc.i4 126
	  stloc.0
	  ldc.i4 25
	  stloc.0
	  ldc.i4 100
	  ldc.i4 81
	  stloc.0
	  ldc.i4 82
	  ldc.i4 51
	  ldc.i4 87
	  ldc.i4 126
	  stloc.0
	  ldc.i4 102
	  stsfld int32 EHTest::fld4
	  ldc.i4 119
	  stloc.0
	  ldc.i4 67
	  stsfld int32 EHTest::fld4
	  ldc.i4 58
	  ldc.i4 126
	  stsfld int32 EHTest::fld4
	  ldc.i4 61
	  stloc.0
	  ldc.i4.5
	  call instance int32 int32[55...85,102...113,84...104,55...104,43...83,64...93,118...122,67...71,76...105,83...123,115...124,16...56,117...127,26...104,51...97,60...104,88...110,18...80,27...47,119...123,103...122,111...112]::Get(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  newarr     [mscorlib]System.Int32
	  ldc.i4.6
	  stsfld int32 EHTest::fld4
	  ldc.i4 96
	  stsfld int32 EHTest::fld4
	  ldc.i4 14
	  ldelem.i4
	  ldc.i4 44
	  ldc.i4 64
	  ldc.i4 107
	  stsfld int32 EHTest::fld4
	  ldc.i4 48
	  stsfld int32 EHTest::fld4
	  ldc.i4 109
	  ldc.i4 53
	  ldc.i4 74
	  ldc.i4 61
	  stsfld int32 EHTest::fld4
	  ldc.i4 56
	  stloc.0
	  ldc.i4 111
	  ldc.i4 45
	  stsfld int32 EHTest::fld4
	  ldc.i4 60
	  stsfld int32 EHTest::fld4
	  ldc.i4 85
	  ldc.i4 45
	  ldc.i4 58
	  ldc.i4 69
	  ldc.i4 55
	  ldc.i4 121
	  ldc.i4 41
	  ldc.i4 80
	  ldc.i4 107
	  ldc.i4 125
	  stsfld int32 EHTest::fld4
	  ldc.i4 112
	  stsfld int32 EHTest::fld4
	  ldc.i4 115
	  stloc.0
	  ldc.i4 74
	  stloc.0
	  ldc.i4 107
	  ldc.i4 112
	  ldc.i4 95
	  stsfld int32 EHTest::fld4
	  ldc.i4.0
	  stloc.0
	  ldc.i4 100
	  ldc.i4 46
	  ldc.i4.5
	  ldc.i4 80
	  ldc.i4 56
	  ldc.i4 20
	  ldc.i4 36
	  ldc.i4 56
	  ldc.i4 97
	  stsfld int32 EHTest::fld4
	  ldc.i4 78
	  stsfld int32 EHTest::fld4
	  ldc.i4 122
	  ldc.i4 121
	  newobj instance void int32[...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...]::.ctor(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4 94
	  ldc.i4.6
	  ldc.i4 86
	  ldc.i4 76
	  ldc.i4.1
	  stsfld int32 EHTest::fld4
	  ldc.i4 118
	  ldc.i4 74
	  ldc.i4 38
	  ldc.i4 113
	  stsfld int32 EHTest::fld4
	  ldc.i4 80
	  ldc.i4 69
	  stsfld int32 EHTest::fld4
	  ldc.i4 13
	  ldc.i4 67
	  stsfld int32 EHTest::fld4
	  ldc.i4 123
	  ldc.i4 105
	  ldc.i4 55
	  stsfld int32 EHTest::fld4
	  ldc.i4 78
	  stsfld int32 EHTest::fld4
	  ldc.i4 19
	  ldc.i4.4
	  ldc.i4 87
	  ldc.i4 99
	  ldc.i4 76
	  ldc.i4 42
	  stsfld int32 EHTest::fld4
	  ldc.i4 60
	  ldc.i4 102
	  ldc.i4 43
	  stloc.0
	  ldc.i4 117
	  ldc.i4 30
	  ldc.i4.1
	  ldc.i4 9
	  stsfld int32 EHTest::fld4
	  ldc.i4 111
	  ldc.i4 113
	  stsfld int32 EHTest::fld4
	  ldc.i4 62
	  ldc.i4 104
	  ldc.i4 115
	  ldc.i4 51
	  ldc.i4 76
	  ldc.i4 110
	  ldc.i4 99
	  call instance int32 int32[...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...,...]::Get(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4 63
	  ldc.i4 106
	  stsfld int32 EHTest::fld4
	  ldc.i4 122
	  ldc.i4 14
	  stsfld int32 EHTest::fld4
	  ldc.i4 125
	  ldc.i4 100
	  ldc.i4 77
	  ldc.i4 127
	  ldc.i4 22
	  ldc.i4 22
	  stloc.0
	  ldc.i4 123
	  stsfld int32 EHTest::fld4
	  ldc.i4 33
	  ldc.i4 112
	  stsfld int32 EHTest::fld4
	  ldc.i4 30
	  stsfld int32 EHTest::fld4
	  ldc.i4 70
	  ldc.i4 59
	  ldc.i4 114
	  ldc.i4 11
	  ldc.i4 91
	  stloc.0
	  ldc.i4 99
	  ldc.i4 65
	  stsfld int32 EHTest::fld4
	  ldc.i4 11
	  ldc.i4 16
	  ldc.i4 10
	  newobj instance void int64[,,,,,,,,,,,,,,,,]::.ctor(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4 75
	  ldc.i4 75
	  ldc.i4 123
	  stloc.0
	  ldc.i4 108
	  ldc.i4 110
	  stloc.0
	  ldc.i4 64
	  stsfld int32 EHTest::fld4
	  ldc.i4 80
	  ldc.i4 88
	  ldc.i4.5
	  ldc.i4 97
	  ldc.i4 18
	  stloc.0
	  ldc.i4 93
	  ldc.i4 9
	  ldc.i4 43
	  stsfld int32 EHTest::fld4
	  ldc.i4 79
	  stsfld int32 EHTest::fld4
	  ldc.i4 124
	  stloc.0
	  ldc.i4 107
	  ldc.i4 43
	  ldc.i4 91
	  ldc.i4 25
	  ldc.i4 33
	  stloc.0
	  ldc.i4 70
	  ldc.i4 111
	  ldc.i4 119
	  ldc.i4 78
	  stsfld int32 EHTest::fld4
	  ldc.i4 10
	  call instance int64 int64[,,,,,,,,,,,,,,,,]::Get(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  bgt.un	TARGET_1_2
	TARGET_1_3:
	  call int64 EHTest::GetInt64_jmp()
	  ldarg.0
	  mul
	  ldc.i8 119
	  xor
	  starg.s arg0
	  ldc.i8 61
	  ldc.i8 11
	  beq	TARGET_1_2
	TARGET_1_4:
	  ldc.i4 43
	  ldc.i4 89
	  ldc.i4 29
	  rem
	  ldc.i4 8
	  shr
	  ldc.i4 42
	  ceq
	  blt.un	TARGET_1_4
try4_out:
newobj instance void [mscorlib]System.AppDomainUnloadedException::.ctor()
throw
end_try4: 

try2_out:
newobj instance void [mscorlib]System.StackOverflowException::.ctor()
throw
end_try2: 

nop
	TARGET_2_0:
	  call int64 EHTest::GetInt64()
	  stloc.1
	  ldc.i8 25
	  ldc.i8 16
	  stsfld int64 EHTest::fld2
	  ldc.i8 15
	  beq	TARGET_2_3
	TARGET_2_1:
	  call int64 EHTest::GetInt64()
	  conv.i8
	  ldsfld int64 EHTest::fld2
	  ble.un	TARGET_2_1
	TARGET_2_2:
	  ldc.i4 47
	  ldc.i4 86
	  ldc.i4 61
	  rem
	  stloc.0
	  ldc.i4 66
	  box int32
	  unbox int32
	  ldind.i4
	  blt.un	TARGET_2_2
	TARGET_2_3:
	  ldc.i4 33
	  stloc.0
	  ldc.i4 55
	  ldc.i4.5
	  or
	  stsfld int32 EHTest::fld4
	  ldc.i4 58
	  ldc.i4 52
	  or
	  stsfld int32 EHTest::fld4
	  ldc.i4 44
	  newarr     [mscorlib]System.Int64
	  ldc.i4 36
	  ldelem.i8
	  ldc.i8 15
	  ldc.i8 26
	  sub
	  stsfld int64 EHTest::fld2
	  ldc.i8 81
	  stsfld int64 EHTest::fld6
	  ldc.i8 80
	  ldc.i8 88
	  sub
	  bge.un	TARGET_2_2
	TARGET_2_4:
	  ldc.i8 8
	  ldc.i8 68
	  div
	  ldc.i8 8
	  sub
	  ldc.i8 117
	  mul
	  box int64
	  unbox int64
	  ldind.i8
	  starg.s arg1
	  ldc.i8 56
	  ldc.i8 20
	  starg.s arg0
	  ldc.i8 45
	  beq	TARGET_2_0
try1_out:
leave OUT0
end_try1: 
OUT: 
	  ldc.i4 29
	  newarr     [mscorlib]System.Int32
	  ldc.i4 103
	  stsfld int32 EHTest::fld4
	  ldc.i4 15
	  ldelem.i4
	  not
	  stsfld int32 EHTest::fld4
	  ldc.i4 48
	  ldc.i4 74
	  stloc.0
	  ldc.i4 16
	  ceq
	  stloc.0
	  ldc.i4 87
	  stloc.0
	  ldc.i4 42
	  stsfld int32 EHTest::fld4
	  ldc.i4 30
	  stsfld int32 EHTest::fld4
	  ldc.i4 28
	  stloc.0
	  ldc.i4.7
ret
OUT0:
nop
	TARGET_3_0:
	  ldc.i8 78
	  ldc.i8 19
	  div
	  stloc.1
	  ldc.i8 21
	  box int64
	  unbox int64
	  ldind.i8
	  ldc.i8 85
	  blt	TARGET_3_0
	TARGET_3_1:
	  ldsflda int64 EHTest::fld3
	  mkrefany   [mscorlib]System.Int64
	  refanyval	[mscorlib]System.Int64
	  ldind.i8
	  ldc.i8 85
	  stloc.2
	  ldc.i8 25
	  stsfld int64 EHTest::fld1
	  ldc.i8 103
	  clt
	  stsfld int32 EHTest::fld4
	  ldc.i4 77
	  conv.i4
	  ldc.i4 88
	  stloc.0
	  ldc.i4 69
	  ldc.i4 84
	  ldc.i4.5
	  ldc.i4 105
	  ldc.i4 32
	  ldc.i4 81
	  stloc.0
	  ldc.i4 103
	  ldc.i4 12
	  ldc.i4 40
	  stsfld int32 EHTest::fld4
	  ldc.i4 91
	  ldc.i4 123
	  ldc.i4 100
	  stsfld int32 EHTest::fld4
	  ldc.i4 91
	  stloc.0
	  ldc.i4 47
	  ldc.i4 67
	  ldc.i4 107
	  ldc.i4 77
	  ldc.i4 16
	  ldc.i4 89
	  ldc.i4 13
	  stsfld int32 EHTest::fld4
	  ldc.i4 52
	  ldc.i4 121
	  ldc.i4 19
	  ldc.i4 47
	  ldc.i4.8
	  stsfld int32 EHTest::fld4
	  ldc.i4.7
	  ldc.i4 36
	  stsfld int32 EHTest::fld4
	  ldc.i4 61
	  stsfld int32 EHTest::fld4
	  ldc.i4 27
	  stsfld int32 EHTest::fld4
	  ldc.i4 125
	  ldc.i4 36
	  stsfld int32 EHTest::fld4
	  ldc.i4 96
	  ldc.i4 39
	  ldc.i4 77
	  stsfld int32 EHTest::fld4
	  ldc.i4 29
	  ldc.i4 20
	  ldc.i4 15
	  stsfld int32 EHTest::fld4
	  ldc.i4 88
	  ldc.i4 126
	  ldc.i4.0
	  newobj instance void int32[67...72,77...122,8...55,10...14,49...124,5...38,81...103,40...104,68...93,75...101,8...91,70...78,75...85,106...109,1...71,71...126,105...109,88...123,122...123,5...127,89...103,18...73,90...113,116...119,15...78,100...100,46...75,76...120,76...87]::.ctor(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4 120
	  ldc.i4 12
	  stsfld int32 EHTest::fld4
	  ldc.i4 109
	  ldc.i4.1
	  stsfld int32 EHTest::fld4
	  ldc.i4 29
	  ldc.i4 121
	  ldc.i4 19
	  ldc.i4 76
	  ldc.i4 122
	  ldc.i4 101
	  ldc.i4 9
	  ldc.i4 66
	  ldc.i4 115
	  ldc.i4 79
	  stsfld int32 EHTest::fld4
	  ldc.i4 31
	  ldc.i4 102
	  stsfld int32 EHTest::fld4
	  ldc.i4 55
	  ldc.i4 40
	  ldc.i4 25
	  stloc.0
	  ldc.i4 108
	  ldc.i4 89
	  ldc.i4 86
	  ldc.i4 123
	  ldc.i4.4
	  ldc.i4 124
	  ldc.i4 18
	  stloc.0
	  ldc.i4 63
	  ldc.i4 48
	  ldc.i4.7
	  ldc.i4 22
	  stsfld int32 EHTest::fld4
	  ldc.i4 98
	  ldc.i4 122
	  ldc.i4 100
	  ldc.i4 12
	  ldc.i4 78
	  ldc.i4 58
	  call instance int32 int32[67...72,77...122,8...55,10...14,49...124,5...38,81...103,40...104,68...93,75...101,8...91,70...78,75...85,106...109,1...71,71...126,105...109,88...123,122...123,5...127,89...103,18...73,90...113,116...119,15...78,100...100,46...75,76...120,76...87]::Get(int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32,int32)
	  ldc.i4 15
	  stsfld int32 EHTest::fld4
	  ldc.i4 39
	  bge.un	TARGET_3_4
	TARGET_3_2:
	  ldftn int64 EHTest::GetInt64_tailcall()
	  calli int64 ()
	  ldc.i4 47
	  ldc.i4 7
	  shl
	  newarr     [mscorlib]System.Int64
	  ldc.i4 57
	  stloc.0
	  ldc.i4 127
	  stsfld int32 EHTest::fld4
	  ldc.i4 108
	  stsfld int32 EHTest::fld4
	  ldc.i4 123
	  ldelem.i8
	  starg.s arg1
	  ldc.i8 14
	  ldc.i8 55
	  starg.s arg1
	  ldc.i8 23
	  starg.s arg1
	  ldc.i8 25
	  starg.s arg1
	  ldc.i8 100
	  add
	  ldc.i8 84
	  sub
	  bgt	TARGET_3_4
	TARGET_3_3:
	  ldloc.2
	  stloc.2
	  ldc.i8 26
	  box int64
	  unbox int64
	  ldind.i8
	  starg.s arg0
	  ldc.i8 89
	  starg.s arg0
	  ldc.i8 1
	  starg.s arg1
	  ldc.i8 117
	  stloc.1
	  ldc.i8 34
	  starg.s arg0
	  ldc.i8 102
	  ldc.i8 105
	  stloc.1
	  ldc.i8 40
	  bge	TARGET_3_2
	TARGET_3_4:
	  ldarg.0
	  ldc.i8 15
	  ldc.i8 66
	  sub
	  beq	TARGET_3_3
leave OUT
.try start_try4 to end_try4 fault handler start_fault2 to end_fault2
.try start_try3 to end_try3 fault handler start_fault1 to end_fault1
.try start_try2 to end_try2 finally handler start_finally2 to end_finally2
.try start_try1 to end_try1 finally handler start_finally1 to end_finally1
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
		int32 V_4)
.try
{
	  ldc.i8 1
	  ldc.i8 24
call int32 EHTest::f1(int64 ,int64 )
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
