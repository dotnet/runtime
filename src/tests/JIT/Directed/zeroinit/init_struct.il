// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly init_struct { }

.class private sequential ansi sealed beforefieldinit Val
       extends [mscorlib]System.ValueType
{
  .field public int64 val
} // end of class Val

.class public auto ansi beforefieldinit Test_init_struct
       extends [mscorlib]System.Object
{
  .field private int64 m_ret
  .field private static class Test_init_struct global
  .method private hidebysig static int64 
          noinline1(int64 'ret') cil managed
  {
    // Code size       6 (0x6)
    .maxstack  1
    .locals init (int64 V_0)
    ldarga.s 0
    pop
    IL_0000:  ldarg.0
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
  } // end of method Test::noinline1

  .method private hidebysig static int64 
          noinline2(int64& 'ret') cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int64 V_0)
    ldarga.s 0
    pop
    IL_0000:  ldarg.0
    IL_0001:  ldind.i8
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method Test::noinline2

  .method private hidebysig static int64 
          test1() cil managed
  {
    // Code size       20 (0x14)
    .maxstack  1
    .locals init (valuetype Val V_0,
             int64 V_1)
    IL_0008:  ldloca.s   V_0
    IL_000a:  ldfld      int64 Val::val
    IL_000f:  stloc.1
    IL_0010:  br.s       IL_0012

    IL_0012:  ldloc.1
    IL_0013:  ret
  } // end of method Test::test1

  .method private hidebysig static int64 
          test2() cil managed
  {
    // Code size       47 (0x2f)
    .maxstack  3
    .locals init (valuetype Val V_0,
             valuetype Val V_1,
             int64 V_2)
    IL_0010:  ldloca.s   V_0
    IL_0012:  ldfld      int64 Val::val
    IL_0017:  ldc.i4.1
    IL_0018:  shr
    IL_0019:  ldc.i4.1
    IL_001a:  conv.i8
    IL_001b:  and
    IL_001c:  ldloca.s   V_1
    IL_001e:  ldfld      int64 Val::val
    IL_0023:  ldc.i4.2
    IL_0024:  shl
    IL_0025:  sub
    IL_0026:  ldc.i4.s   11
    IL_0028:  conv.i8
    IL_0029:  add
    IL_002a:  stloc.2
    IL_002b:  br.s       IL_002d

    IL_002d:  ldloc.2
    IL_002e:  ret
  } // end of method Test::test2

  .method private hidebysig static int64 
          test3() cil managed
  {
    // Code size       25 (0x19)
    .maxstack  1
    .locals init (valuetype Val V_0,
             int64 V_1)
    IL_0008:  ldloca.s   V_0
    IL_000a:  ldfld      int64 Val::val
    IL_000f:  call       int64 Test_init_struct::noinline1(int64)
    IL_0014:  stloc.1
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.1
    IL_0018:  ret
  } // end of method Test::test3

  .method private hidebysig static int64 
          test4() cil managed
  {
    // Code size       25 (0x19)
    .maxstack  1
    .locals init (valuetype Val V_0,
             int64 V_1)
    IL_0008:  ldloca.s   V_0
    IL_000a:  ldflda     int64 Val::val
    IL_000f:  call       int64 Test_init_struct::noinline2(int64&)
    IL_0014:  stloc.1
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.1
    IL_0018:  ret
  } // end of method Test::test4

  .method private hidebysig static void  test5(int64& 'ret') cil managed
  {
    // Code size       23 (0x17)
    .maxstack  2
    .locals init (valuetype Val V_0)
    IL_0008:  ldarg.0
    IL_0009:  ldloca.s   V_0
    IL_000b:  ldfld      int64 Val::val
    IL_0010:  call       int64 Test_init_struct::noinline1(int64)
    IL_0015:  stind.i8
    IL_0016:  ret
  } // end of method Test::test5

  .method private hidebysig static void  test6(int64& 'ret') cil managed
  {
    // Code size       23 (0x17)
    .maxstack  2
    .locals init (valuetype Val V_0)
    IL_0008:  ldarg.0
    IL_0009:  ldloca.s   V_0
    IL_000b:  ldflda     int64 Val::val
    IL_0010:  call       int64 Test_init_struct::noinline2(int64&)
    IL_0015:  stind.i8
    IL_0016:  ret
  } // end of method Test::test6

  .method private hidebysig static void  test7() cil managed
  {
    // Code size       26 (0x1a)
    .maxstack  2
    .locals init (valuetype Val V_0)
    IL_0008:  ldsfld     class Test_init_struct Test_init_struct::global
    IL_000d:  ldloca.s   V_0
    IL_000f:  ldfld      int64 Val::val
    IL_0014:  stfld      int64 Test_init_struct::m_ret
    IL_0019:  ret
  } // end of method Test::test7

  .method private hidebysig static void  test8() cil managed
  {
    // Code size       53 (0x35)
    .maxstack  4
    .locals init (valuetype Val V_0,
             valuetype Val V_1)
    IL_0010:  ldsfld     class Test_init_struct Test_init_struct::global
    IL_0015:  ldloca.s   V_0
    IL_0017:  ldfld      int64 Val::val
    IL_001c:  ldc.i4.1
    IL_001d:  shr
    IL_001e:  ldc.i4.1
    IL_001f:  conv.i8
    IL_0020:  and
    IL_0021:  ldloca.s   V_1
    IL_0023:  ldfld      int64 Val::val
    IL_0028:  ldc.i4.2
    IL_0029:  shl
    IL_002a:  sub
    IL_002b:  ldc.i4.s   11
    IL_002d:  conv.i8
    IL_002e:  add
    IL_002f:  stfld      int64 Test_init_struct::m_ret
    IL_0034:  ret
  } // end of method Test::test8

  .method private hidebysig static void  test9() cil managed
  {
    // Code size       31 (0x1f)
    .maxstack  2
    .locals init (valuetype Val V_0)
    IL_0008:  ldsfld     class Test_init_struct Test_init_struct::global
    IL_000d:  ldloca.s   V_0
    IL_000f:  ldfld      int64 Val::val
    IL_0014:  call       int64 Test_init_struct::noinline1(int64)
    IL_0019:  stfld      int64 Test_init_struct::m_ret
    IL_001e:  ret
  } // end of method Test::test9

  .method private hidebysig static void  test10() cil managed
  {
    // Code size       31 (0x1f)
    .maxstack  2
    .locals init (valuetype Val V_0)
    IL_0008:  ldsfld     class Test_init_struct Test_init_struct::global
    IL_000d:  ldloca.s   V_0
    IL_000f:  ldflda     int64 Val::val
    IL_0014:  call       int64 Test_init_struct::noinline2(int64&)
    IL_0019:  stfld      int64 Test_init_struct::m_ret
    IL_001e:  ret
  } // end of method Test::test10

  .method private hidebysig static void  test11(int64[] arr,
                                                int32 index) cil managed
  {
    // Code size       19 (0x13)
    .maxstack  3
    .locals init (valuetype Val V_0)
    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  ldloca.s   V_0
    IL_000c:  ldfld      int64 Val::val
    IL_0011:  stelem.i8
    IL_0012:  ret
  } // end of method Test::test11

  .method private hidebysig static void  test12(int64[] arr,
                                                int32 index) cil managed
  {
    // Code size       46 (0x2e)
    .maxstack  5
    .locals init (valuetype Val V_0,
             valuetype Val V_1)
    IL_0010:  ldarg.0
    IL_0011:  ldarg.1
    IL_0012:  ldloca.s   V_0
    IL_0014:  ldfld      int64 Val::val
    IL_0019:  ldc.i4.1
    IL_001a:  shr
    IL_001b:  ldc.i4.1
    IL_001c:  conv.i8
    IL_001d:  and
    IL_001e:  ldloca.s   V_1
    IL_0020:  ldfld      int64 Val::val
    IL_0025:  ldc.i4.2
    IL_0026:  shl
    IL_0027:  sub
    IL_0028:  ldc.i4.s   11
    IL_002a:  conv.i8
    IL_002b:  add
    IL_002c:  stelem.i8
    IL_002d:  ret
  } // end of method Test::test12

  .method private hidebysig static void  test13(int64[] arr,
                                                int32 index) cil managed
  {
    // Code size       24 (0x18)
    .maxstack  3
    .locals init (valuetype Val V_0)
    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  ldloca.s   V_0
    IL_000c:  ldfld      int64 Val::val
    IL_0011:  call       int64 Test_init_struct::noinline1(int64)
    IL_0016:  stelem.i8
    IL_0017:  ret
  } // end of method Test::test13

  .method private hidebysig static void  test14(int64[] arr,
                                                int32 index) cil managed
  {
    // Code size       24 (0x18)
    .maxstack  3
    .locals init (valuetype Val V_0)
    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  ldloca.s   V_0
    IL_000c:  ldflda     int64 Val::val
    IL_0011:  call       int64 Test_init_struct::noinline2(int64&)
    IL_0016:  stelem.i8
    IL_0017:  ret
  } // end of method Test::test14

  .method private hidebysig static void  test15(int64[0...,0...,0...] arr,
                                                int32 index1,
                                                int32 index3) cil managed
  {
    // Code size       25 (0x19)
    .maxstack  5
    .locals init (valuetype Val V_0)
    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  ldc.i4.2
    IL_000b:  ldarg.2
    IL_000c:  ldloca.s   V_0
    IL_000e:  ldfld      int64 Val::val
    IL_0013:  call       instance void int64[0...,0...,0...]::Set(int32,
                                                                  int32,
                                                                  int32,
                                                                  int64)
    IL_0018:  ret
  } // end of method Test::test15

  .method private hidebysig static void  test16(int64[0...,0...,0...] arr,
                                                int32 index1,
                                                int32 index3) cil managed
  {
    // Code size       52 (0x34)
    .maxstack  7
    .locals init (valuetype Val V_0,
             valuetype Val V_1)
    IL_0010:  ldarg.0
    IL_0011:  ldarg.1
    IL_0012:  ldc.i4.2
    IL_0013:  ldarg.2
    IL_0014:  ldloca.s   V_0
    IL_0016:  ldfld      int64 Val::val
    IL_001b:  ldc.i4.1
    IL_001c:  shr
    IL_001d:  ldc.i4.1
    IL_001e:  conv.i8
    IL_001f:  and
    IL_0020:  ldloca.s   V_1
    IL_0022:  ldfld      int64 Val::val
    IL_0027:  ldc.i4.2
    IL_0028:  shl
    IL_0029:  sub
    IL_002a:  ldc.i4.s   11
    IL_002c:  conv.i8
    IL_002d:  add
    IL_002e:  call       instance void int64[0...,0...,0...]::Set(int32,
                                                                  int32,
                                                                  int32,
                                                                  int64)
    IL_0033:  ret
  } // end of method Test::test16

  .method private hidebysig static void  test17(int64[0...,0...,0...] arr,
                                                int32 index1,
                                                int32 index3) cil managed
  {
    // Code size       30 (0x1e)
    .maxstack  5
    .locals init (valuetype Val V_0)
    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  ldc.i4.2
    IL_000b:  ldarg.2
    IL_000c:  ldloca.s   V_0
    IL_000e:  ldfld      int64 Val::val
    IL_0013:  call       int64 Test_init_struct::noinline1(int64)
    IL_0018:  call       instance void int64[0...,0...,0...]::Set(int32,
                                                                  int32,
                                                                  int32,
                                                                  int64)
    IL_001d:  ret
  } // end of method Test::test17

  .method private hidebysig static void  test18(int64[0...,0...,0...] arr,
                                                int32 index1,
                                                int32 index3) cil managed
  {
    // Code size       30 (0x1e)
    .maxstack  5
    .locals init (valuetype Val V_0)
    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  ldc.i4.2
    IL_000b:  ldarg.2
    IL_000c:  ldloca.s   V_0
    IL_000e:  ldflda     int64 Val::val
    IL_0013:  call       int64 Test_init_struct::noinline2(int64&)
    IL_0018:  call       instance void int64[0...,0...,0...]::Set(int32,
                                                                  int32,
                                                                  int32,
                                                                  int64)
    IL_001d:  ret
  } // end of method Test::test18

  .method private hidebysig static int32 
          test19() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  1
    .locals init (valuetype Val V_0,
             int32 V_1)
    IL_0008:  ldloca.s   V_0
    IL_000a:  ldfld      int64 Val::val
    IL_000f:  conv.i4
    IL_0010:  stloc.1
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.1
    IL_0014:  ret
  } // end of method Test::test19

  .method private hidebysig static unsigned int64 
          test20() cil managed
  {
    // Code size       47 (0x2f)
    .maxstack  3
    .locals init (valuetype Val V_0,
             valuetype Val V_1,
             unsigned int64 V_2)
    IL_0010:  ldloca.s   V_0
    IL_0012:  ldfld      int64 Val::val
    IL_0017:  ldc.i4.1
    IL_0018:  shr
    IL_0019:  ldc.i4.1
    IL_001a:  conv.i8
    IL_001b:  and
    IL_001c:  ldloca.s   V_1
    IL_001e:  ldfld      int64 Val::val
    IL_0023:  ldc.i4.2
    IL_0024:  shl
    IL_0025:  sub
    IL_0026:  ldc.i4.s   11
    IL_0028:  conv.i8
    IL_0029:  add
    IL_002a:  stloc.2
    IL_002b:  br.s       IL_002d

    IL_002d:  ldloc.2
    IL_002e:  ret
  } // end of method Test::test20

  .method private hidebysig static unsigned int8 
          test21() cil managed
  {
    // Code size       26 (0x1a)
    .maxstack  1
    .locals init (valuetype Val V_0,
             unsigned int8 V_1)
    IL_0008:  ldloca.s   V_0
    IL_000a:  ldfld      int64 Val::val
    IL_000f:  call       int64 Test_init_struct::noinline1(int64)
    IL_0014:  conv.u1
    IL_0015:  stloc.1
    IL_0016:  br.s       IL_0018

    IL_0018:  ldloc.1
    IL_0019:  ret
  } // end of method Test::test21

  .method private hidebysig static unsigned int16 
          test22() cil managed
  {
    // Code size       26 (0x1a)
    .maxstack  1
    .locals init (valuetype Val V_0,
             unsigned int16 V_1)
    IL_0008:  ldloca.s   V_0
    IL_000a:  ldflda     int64 Val::val
    IL_000f:  call       int64 Test_init_struct::noinline2(int64&)
    IL_0014:  conv.ovf.u2
    IL_0015:  stloc.1
    IL_0016:  br.s       IL_0018

    IL_0018:  ldloc.1
    IL_0019:  ret
  } // end of method Test::test22

  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       740 (0x2e4)
    .maxstack  4
    .locals init (int64 V_0,
             int64[] V_1,
             int64[0...,0...,0...] V_2,
             int32 V_3)
    IL_0000:  call       int64 Test_init_struct::test1()
    IL_0005:  ldc.i4.0
    IL_0006:  conv.i8
    IL_0007:  beq.s      IL_001b

    IL_0009:  ldstr      "Error 101"
    IL_000e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0013:  ldc.i4.s   101
    IL_0015:  stloc.3
    IL_0016:  br         IL_02e2

    IL_001b:  call       int64 Test_init_struct::test2()
    IL_0020:  ldc.i4.s   11
    IL_0022:  conv.i8
    IL_0023:  beq.s      IL_0037

    IL_0025:  ldstr      "Error 102"
    IL_002a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002f:  ldc.i4.s   102
    IL_0031:  stloc.3
    IL_0032:  br         IL_02e2

    IL_0037:  call       int64 Test_init_struct::test3()
    IL_003c:  ldc.i4.0
    IL_003d:  conv.i8
    IL_003e:  beq.s      IL_0052

    IL_0040:  ldstr      "Error 103"
    IL_0045:  call       void [System.Console]System.Console::WriteLine(string)
    IL_004a:  ldc.i4.s   103
    IL_004c:  stloc.3
    IL_004d:  br         IL_02e2

    IL_0052:  call       int64 Test_init_struct::test4()
    IL_0057:  ldc.i4.0
    IL_0058:  conv.i8
    IL_0059:  beq.s      IL_006d

    IL_005b:  ldstr      "Error 104"
    IL_0060:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0065:  ldc.i4.s   104
    IL_0067:  stloc.3
    IL_0068:  br         IL_02e2

    IL_006d:  ldc.i4.0
    IL_006e:  conv.i8
    IL_006f:  stloc.0
    IL_0070:  ldloca.s   V_0
    IL_0072:  call       void Test_init_struct::test5(int64&)
    IL_0077:  ldloc.0
    IL_0078:  ldc.i4.0
    IL_0079:  conv.i8
    IL_007a:  beq.s      IL_008e

    IL_007c:  ldstr      "Error 105"
    IL_0081:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0086:  ldc.i4.s   105
    IL_0088:  stloc.3
    IL_0089:  br         IL_02e2

    IL_008e:  ldloca.s   V_0
    IL_0090:  call       void Test_init_struct::test6(int64&)
    IL_0095:  ldloc.0
    IL_0096:  ldc.i4.0
    IL_0097:  conv.i8
    IL_0098:  beq.s      IL_00ac

    IL_009a:  ldstr      "Error 106"
    IL_009f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00a4:  ldc.i4.s   106
    IL_00a6:  stloc.3
    IL_00a7:  br         IL_02e2

    IL_00ac:  call       void Test_init_struct::test7()
    IL_00b1:  ldsfld     class Test_init_struct Test_init_struct::global
    IL_00b6:  ldfld      int64 Test_init_struct::m_ret
    IL_00bb:  ldc.i4.0
    IL_00bc:  conv.i8
    IL_00bd:  beq.s      IL_00d1

    IL_00bf:  ldstr      "Error 107"
    IL_00c4:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00c9:  ldc.i4.s   107
    IL_00cb:  stloc.3
    IL_00cc:  br         IL_02e2

    IL_00d1:  call       void Test_init_struct::test8()
    IL_00d6:  ldsfld     class Test_init_struct Test_init_struct::global
    IL_00db:  ldfld      int64 Test_init_struct::m_ret
    IL_00e0:  ldc.i4.s   11
    IL_00e2:  conv.i8
    IL_00e3:  beq.s      IL_00f7

    IL_00e5:  ldstr      "Error 108"
    IL_00ea:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00ef:  ldc.i4.s   108
    IL_00f1:  stloc.3
    IL_00f2:  br         IL_02e2

    IL_00f7:  call       void Test_init_struct::test9()
    IL_00fc:  ldsfld     class Test_init_struct Test_init_struct::global
    IL_0101:  ldfld      int64 Test_init_struct::m_ret
    IL_0106:  ldc.i4.0
    IL_0107:  conv.i8
    IL_0108:  beq.s      IL_011c

    IL_010a:  ldstr      "Error 109"
    IL_010f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0114:  ldc.i4.s   109
    IL_0116:  stloc.3
    IL_0117:  br         IL_02e2

    IL_011c:  call       void Test_init_struct::test10()
    IL_0121:  ldsfld     class Test_init_struct Test_init_struct::global
    IL_0126:  ldfld      int64 Test_init_struct::m_ret
    IL_012b:  ldc.i4.0
    IL_012c:  conv.i8
    IL_012d:  beq.s      IL_0141

    IL_012f:  ldstr      "Error 110"
    IL_0134:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0139:  ldc.i4.s   110
    IL_013b:  stloc.3
    IL_013c:  br         IL_02e2

    IL_0141:  ldc.i4.5
    IL_0142:  newarr     [mscorlib]System.Int64
    IL_0147:  dup
    IL_0148:  ldtoken    field valuetype '<PrivateImplementationDetails>'/'$$struct0x6000019-1' '<PrivateImplementationDetails>'::'$$method0x6000019-1'
    IL_014d:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                        valuetype [mscorlib]System.RuntimeFieldHandle)
    IL_0152:  stloc.1
    IL_0153:  ldloc.1
    IL_0154:  ldc.i4.1
    IL_0155:  call       void Test_init_struct::test11(int64[],
                                           int32)
    IL_015a:  ldloc.1
    IL_015b:  ldc.i4.1
    IL_015c:  ldelem.i8
    IL_015d:  ldc.i4.0
    IL_015e:  conv.i8
    IL_015f:  beq.s      IL_0173

    IL_0161:  ldstr      "Error 111"
    IL_0166:  call       void [System.Console]System.Console::WriteLine(string)
    IL_016b:  ldc.i4.s   111
    IL_016d:  stloc.3
    IL_016e:  br         IL_02e2

    IL_0173:  ldloc.1
    IL_0174:  ldc.i4.2
    IL_0175:  call       void Test_init_struct::test12(int64[],
                                           int32)
    IL_017a:  ldloc.1
    IL_017b:  ldc.i4.2
    IL_017c:  ldelem.i8
    IL_017d:  ldc.i4.s   11
    IL_017f:  conv.i8
    IL_0180:  beq.s      IL_0194

    IL_0182:  ldstr      "Error 112"
    IL_0187:  call       void [System.Console]System.Console::WriteLine(string)
    IL_018c:  ldc.i4.s   112
    IL_018e:  stloc.3
    IL_018f:  br         IL_02e2

    IL_0194:  ldloc.1
    IL_0195:  ldc.i4.3
    IL_0196:  call       void Test_init_struct::test13(int64[],
                                           int32)
    IL_019b:  ldloc.1
    IL_019c:  ldc.i4.3
    IL_019d:  ldelem.i8
    IL_019e:  ldc.i4.0
    IL_019f:  conv.i8
    IL_01a0:  beq.s      IL_01b4

    IL_01a2:  ldstr      "Error 113"
    IL_01a7:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01ac:  ldc.i4.s   113
    IL_01ae:  stloc.3
    IL_01af:  br         IL_02e2

    IL_01b4:  ldloc.1
    IL_01b5:  ldc.i4.4
    IL_01b6:  call       void Test_init_struct::test14(int64[],
                                           int32)
    IL_01bb:  ldloc.1
    IL_01bc:  ldc.i4.4
    IL_01bd:  ldelem.i8
    IL_01be:  ldc.i4.0
    IL_01bf:  conv.i8
    IL_01c0:  beq.s      IL_01d4

    IL_01c2:  ldstr      "Error 114"
    IL_01c7:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01cc:  ldc.i4.s   114
    IL_01ce:  stloc.3
    IL_01cf:  br         IL_02e2

    IL_01d4:  ldc.i4.5
    IL_01d5:  ldc.i4.3
    IL_01d6:  ldc.i4.5
    IL_01d7:  newobj     instance void int64[0...,0...,0...]::.ctor(int32,
                                                                    int32,
                                                                    int32)
    IL_01dc:  stloc.2
    IL_01dd:  ldloc.2
    IL_01de:  ldc.i4.1
    IL_01df:  ldc.i4.1
    IL_01e0:  call       void Test_init_struct::test15(int64[0...,0...,0...],
                                           int32,
                                           int32)
    IL_01e5:  ldloc.2
    IL_01e6:  ldc.i4.1
    IL_01e7:  ldc.i4.2
    IL_01e8:  ldc.i4.1
    IL_01e9:  call       instance int64 int64[0...,0...,0...]::Get(int32,
                                                                   int32,
                                                                   int32)
    IL_01ee:  ldc.i4.0
    IL_01ef:  conv.i8
    IL_01f0:  beq.s      IL_0204

    IL_01f2:  ldstr      "Error 115"
    IL_01f7:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01fc:  ldc.i4.s   115
    IL_01fe:  stloc.3
    IL_01ff:  br         IL_02e2

    IL_0204:  ldloc.2
    IL_0205:  ldc.i4.2
    IL_0206:  ldc.i4.2
    IL_0207:  call       void Test_init_struct::test16(int64[0...,0...,0...],
                                           int32,
                                           int32)
    IL_020c:  ldloc.2
    IL_020d:  ldc.i4.2
    IL_020e:  ldc.i4.2
    IL_020f:  ldc.i4.2
    IL_0210:  call       instance int64 int64[0...,0...,0...]::Get(int32,
                                                                   int32,
                                                                   int32)
    IL_0215:  ldc.i4.s   11
    IL_0217:  conv.i8
    IL_0218:  beq.s      IL_022c

    IL_021a:  ldstr      "Error 116"
    IL_021f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0224:  ldc.i4.s   116
    IL_0226:  stloc.3
    IL_0227:  br         IL_02e2

    IL_022c:  ldloc.2
    IL_022d:  ldc.i4.3
    IL_022e:  ldc.i4.3
    IL_022f:  call       void Test_init_struct::test17(int64[0...,0...,0...],
                                           int32,
                                           int32)
    IL_0234:  ldloc.2
    IL_0235:  ldc.i4.3
    IL_0236:  ldc.i4.2
    IL_0237:  ldc.i4.3
    IL_0238:  call       instance int64 int64[0...,0...,0...]::Get(int32,
                                                                   int32,
                                                                   int32)
    IL_023d:  ldc.i4.0
    IL_023e:  conv.i8
    IL_023f:  beq.s      IL_0253

    IL_0241:  ldstr      "Error 117"
    IL_0246:  call       void [System.Console]System.Console::WriteLine(string)
    IL_024b:  ldc.i4.s   117
    IL_024d:  stloc.3
    IL_024e:  br         IL_02e2

    IL_0253:  ldloc.2
    IL_0254:  ldc.i4.4
    IL_0255:  ldc.i4.4
    IL_0256:  call       void Test_init_struct::test18(int64[0...,0...,0...],
                                           int32,
                                           int32)
    IL_025b:  ldloc.2
    IL_025c:  ldc.i4.4
    IL_025d:  ldc.i4.2
    IL_025e:  ldc.i4.4
    IL_025f:  call       instance int64 int64[0...,0...,0...]::Get(int32,
                                                                   int32,
                                                                   int32)
    IL_0264:  ldc.i4.0
    IL_0265:  conv.i8
    IL_0266:  beq.s      IL_0277

    IL_0268:  ldstr      "Error 118"
    IL_026d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0272:  ldc.i4.s   118
    IL_0274:  stloc.3
    IL_0275:  br.s       IL_02e2

    IL_0277:  call       int32 Test_init_struct::test19()
    IL_027c:  brfalse.s  IL_028d

    IL_027e:  ldstr      "Error 119"
    IL_0283:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0288:  ldc.i4.s   119
    IL_028a:  stloc.3
    IL_028b:  br.s       IL_02e2

    IL_028d:  call       unsigned int64 Test_init_struct::test20()
    IL_0292:  ldc.i4.s   11
    IL_0294:  conv.i8
    IL_0295:  beq.s      IL_02a6

    IL_0297:  ldstr      "Error 120"
    IL_029c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_02a1:  ldc.i4.s   120
    IL_02a3:  stloc.3
    IL_02a4:  br.s       IL_02e2

    IL_02a6:  call       unsigned int8 Test_init_struct::test21()
    IL_02ab:  brfalse.s  IL_02bc

    IL_02ad:  ldstr      "Error 121"
    IL_02b2:  call       void [System.Console]System.Console::WriteLine(string)
    IL_02b7:  ldc.i4.s   121
    IL_02b9:  stloc.3
    IL_02ba:  br.s       IL_02e2

    IL_02bc:  call       unsigned int16 Test_init_struct::test22()
    IL_02c1:  conv.i4
    IL_02c2:  brfalse.s  IL_02d3

    IL_02c4:  ldstr      "Error 122"
    IL_02c9:  call       void [System.Console]System.Console::WriteLine(string)
    IL_02ce:  ldc.i4.s   122
    IL_02d0:  stloc.3
    IL_02d1:  br.s       IL_02e2

    IL_02d3:  ldstr      "Passed"
    IL_02d8:  call       void [System.Console]System.Console::WriteLine(string)
    IL_02dd:  ldc.i4.s   100
    IL_02df:  stloc.3
    IL_02e0:  br.s       IL_02e2

    IL_02e2:  ldloc.3
    IL_02e3:  ret
  } // end of method Test::Main

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    IL_0000:  newobj     instance void Test_init_struct::.ctor()
    IL_0005:  stsfld     class Test_init_struct Test_init_struct::global
    IL_000a:  ret
  } // end of method Test::.cctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       15 (0xf)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.0
    IL_0002:  conv.i8
    IL_0003:  stfld      int64 Test_init_struct::m_ret
    IL_0008:  ldarg.0
    IL_0009:  call       instance void [mscorlib]System.Object::.ctor()
    IL_000e:  ret
  } // end of method Test::.ctor

} // end of class Test

.class private auto ansi '<PrivateImplementationDetails>'
       extends [mscorlib]System.Object
{
  .class explicit ansi sealed nested private '$$struct0x6000019-1'
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 40
  } // end of class '$$struct0x6000019-1'

  .field static assembly valuetype '<PrivateImplementationDetails>'/'$$struct0x6000019-1' '$$method0x6000019-1' at D_00002448
} // end of class '<PrivateImplementationDetails>'


// =============================================================

.data D_00002448 = bytearray (
                 01 00 00 00 00 00 00 00 02 00 00 00 00 00 00 00
                 03 00 00 00 00 00 00 00 04 00 00 00 00 00 00 00
                 05 00 00 00 00 00 00 00) 
//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file init_struct.res
