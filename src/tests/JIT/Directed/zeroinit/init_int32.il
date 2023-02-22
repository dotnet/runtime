// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly init_int32 { }
.class public auto ansi beforefieldinit Test_init_int32
       extends [mscorlib]System.Object
{
  .field private int32 m_ret
  .field private static class Test_init_int32 global
  .method private hidebysig static int32 
          noinline1(int32 'ret') cil managed
  {
    .maxstack  1
    ldarga.s 0
    ldind.i4
    ret
  }

  .method private hidebysig static int32 
          noinline2(int32& 'ret') cil managed
  {
    .maxstack  1
    ldarga.s 0
    ldind.i
    ldind.i4
    ret
  } // end of method Test::noinline2

  .method private hidebysig static int32 
          test1() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  1
    .locals init (int32 V_0,
             int32 V_1)
    IL_0006:  ldloc.1
    IL_0007:  ret
  } // end of method Test::test1

  .method private hidebysig static int32 
          test2() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  3
    .locals init (int32 V_0,
             int32 V_1,
             int32 V_2)
    IL_0004:  ldloc.0
    IL_0005:  ldc.i4.1
    IL_0006:  shr
    IL_0007:  ldc.i4.1
    IL_0008:  and
    IL_0009:  ldloc.1
    IL_000a:  ldc.i4.2
    IL_000b:  shl
    IL_000c:  sub
    IL_000d:  ldc.i4.s   11
    IL_000f:  add
    IL_0010:  stloc.2
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.2
    IL_0014:  ret
  } // end of method Test::test2

  .method private hidebysig static int32 
          test3() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  1
    .locals init (int32 V_0,
             int32 V_1)
    IL_0002:  ldloc.0
    IL_0003:  call       int32 Test_init_int32::noinline1(int32)
    IL_0008:  stloc.1
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.1
    IL_000c:  ret
  } // end of method Test::test3

  .method private hidebysig static int32 
          test4() cil managed
  {
    // Code size       14 (0xe)
    .maxstack  1
    .locals init (int32 V_0,
             int32 V_1)
    IL_0002:  ldloca.s   V_0
    IL_0004:  call       int32 Test_init_int32::noinline2(int32&)
    IL_0009:  stloc.1
    IL_000a:  br.s       IL_000c

    IL_000c:  ldloc.1
    IL_000d:  ret
  } // end of method Test::test4

  .method private hidebysig static void  test5(int32& 'ret') cil managed
  {
    // Code size       11 (0xb)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0002:  ldarg.0
    IL_0003:  ldloc.0
    IL_0004:  call       int32 Test_init_int32::noinline1(int32)
    IL_0009:  stind.i4
    IL_000a:  ret
  } // end of method Test::test5

  .method private hidebysig static void  test6(int32& 'ret') cil managed
  {
    // Code size       12 (0xc)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0002:  ldarg.0
    IL_0003:  ldloca.s   V_0
    IL_0005:  call       int32 Test_init_int32::noinline2(int32&)
    IL_000a:  stind.i4
    IL_000b:  ret
  } // end of method Test::test6

  .method private hidebysig static void  test7() cil managed
  {
    // Code size       14 (0xe)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0002:  ldsfld     class Test_init_int32 Test_init_int32::global
    IL_0007:  ldloc.0
    IL_0008:  stfld      int32 Test_init_int32::m_ret
    IL_000d:  ret
  } // end of method Test::test7

  .method private hidebysig static void  test8() cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  4
    .locals init (int32 V_0,
             int32 V_1)
    IL_0004:  ldsfld     class Test_init_int32 Test_init_int32::global
    IL_0009:  ldloc.0
    IL_000a:  ldc.i4.1
    IL_000b:  shr
    IL_000c:  ldc.i4.1
    IL_000d:  and
    IL_000e:  ldloc.1
    IL_000f:  ldc.i4.2
    IL_0010:  shl
    IL_0011:  sub
    IL_0012:  ldc.i4.s   11
    IL_0014:  add
    IL_0015:  stfld      int32 Test_init_int32::m_ret
    IL_001a:  ret
  } // end of method Test::test8

  .method private hidebysig static void  test9() cil managed
  {
    // Code size       19 (0x13)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0002:  ldsfld     class Test_init_int32 Test_init_int32::global
    IL_0007:  ldloc.0
    IL_0008:  call       int32 Test_init_int32::noinline1(int32)
    IL_000d:  stfld      int32 Test_init_int32::m_ret
    IL_0012:  ret
  } // end of method Test::test9

  .method private hidebysig static void  test10() cil managed
  {
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0002:  ldsfld     class Test_init_int32 Test_init_int32::global
    IL_0007:  ldloca.s   V_0
    IL_0009:  call       int32 Test_init_int32::noinline2(int32&)
    IL_000e:  stfld      int32 Test_init_int32::m_ret
    IL_0013:  ret
  } // end of method Test::test10

  .method private hidebysig static void  test11(int32[] arr,
                                                int32 index) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  3
    .locals init (int32 V_0)
    IL_0002:  ldarg.0
    IL_0003:  ldarg.1
    IL_0004:  ldloc.0
    IL_0005:  stelem.i4
    IL_0006:  ret
  } // end of method Test::test11

  .method private hidebysig static void  test12(int32[] arr,
                                                int32 index) cil managed
  {
    // Code size       20 (0x14)
    .maxstack  5
    .locals init (int32 V_0,
             int32 V_1)
    IL_0004:  ldarg.0
    IL_0005:  ldarg.1
    IL_0006:  ldloc.0
    IL_0007:  ldc.i4.1
    IL_0008:  shr
    IL_0009:  ldc.i4.1
    IL_000a:  and
    IL_000b:  ldloc.1
    IL_000c:  ldc.i4.2
    IL_000d:  shl
    IL_000e:  sub
    IL_000f:  ldc.i4.s   11
    IL_0011:  add
    IL_0012:  stelem.i4
    IL_0013:  ret
  } // end of method Test::test12

  .method private hidebysig static void  test13(int32[] arr,
                                                int32 index) cil managed
  {
    // Code size       12 (0xc)
    .maxstack  3
    .locals init (int32 V_0)
    IL_0002:  ldarg.0
    IL_0003:  ldarg.1
    IL_0004:  ldloc.0
    IL_0005:  call       int32 Test_init_int32::noinline1(int32)
    IL_000a:  stelem.i4
    IL_000b:  ret
  } // end of method Test::test13

  .method private hidebysig static void  test14(int32[] arr,
                                                int32 index) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  3
    .locals init (int32 V_0)
    IL_0002:  ldarg.0
    IL_0003:  ldarg.1
    IL_0004:  ldloca.s   V_0
    IL_0006:  call       int32 Test_init_int32::noinline2(int32&)
    IL_000b:  stelem.i4
    IL_000c:  ret
  } // end of method Test::test14

  .method private hidebysig static void  test15(int32[0...,0...,0...] arr,
                                                int32 index1,
                                                int32 index3) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  5
    .locals init (int32 V_0)
    IL_0002:  ldarg.0
    IL_0003:  ldarg.1
    IL_0004:  ldc.i4.2
    IL_0005:  ldarg.2
    IL_0006:  ldloc.0
    IL_0007:  call       instance void int32[0...,0...,0...]::Set(int32,
                                                                  int32,
                                                                  int32,
                                                                  int32)
    IL_000c:  ret
  } // end of method Test::test15

  .method private hidebysig static void  test16(int32[0...,0...,0...] arr,
                                                int32 index1,
                                                int32 index3) cil managed
  {
    // Code size       26 (0x1a)
    .maxstack  7
    .locals init (int32 V_0,
             int32 V_1)
    IL_0004:  ldarg.0
    IL_0005:  ldarg.1
    IL_0006:  ldc.i4.2
    IL_0007:  ldarg.2
    IL_0008:  ldloc.0
    IL_0009:  ldc.i4.1
    IL_000a:  shr
    IL_000b:  ldc.i4.1
    IL_000c:  and
    IL_000d:  ldloc.1
    IL_000e:  ldc.i4.2
    IL_000f:  shl
    IL_0010:  sub
    IL_0011:  ldc.i4.s   11
    IL_0013:  add
    IL_0014:  call       instance void int32[0...,0...,0...]::Set(int32,
                                                                  int32,
                                                                  int32,
                                                                  int32)
    IL_0019:  ret
  } // end of method Test::test16

  .method private hidebysig static void  test17(int32[0...,0...,0...] arr,
                                                int32 index1,
                                                int32 index3) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  5
    .locals init (int32 V_0)
    IL_0002:  ldarg.0
    IL_0003:  ldarg.1
    IL_0004:  ldc.i4.2
    IL_0005:  ldarg.2
    IL_0006:  ldloc.0
    IL_0007:  call       int32 Test_init_int32::noinline1(int32)
    IL_000c:  call       instance void int32[0...,0...,0...]::Set(int32,
                                                                  int32,
                                                                  int32,
                                                                  int32)
    IL_0011:  ret
  } // end of method Test::test17

  .method private hidebysig static void  test18(int32[0...,0...,0...] arr,
                                                int32 index1,
                                                int32 index3) cil managed
  {
    // Code size       19 (0x13)
    .maxstack  5
    .locals init (int32 V_0)
    IL_0002:  ldarg.0
    IL_0003:  ldarg.1
    IL_0004:  ldc.i4.2
    IL_0005:  ldarg.2
    IL_0006:  ldloca.s   V_0
    IL_0008:  call       int32 Test_init_int32::noinline2(int32&)
    IL_000d:  call       instance void int32[0...,0...,0...]::Set(int32,
                                                                  int32,
                                                                  int32,
                                                                  int32)
    IL_0012:  ret
  } // end of method Test::test18

  .method private hidebysig static int64 
          test19() cil managed
  {
    // Code size       9 (0x9)
    .maxstack  1
    .locals init (int32 V_0,
             int64 V_1)
    IL_0002:  ldloc.0
    IL_0003:  conv.i8
    IL_0004:  stloc.1
    IL_0005:  br.s       IL_0007

    IL_0007:  ldloc.1
    IL_0008:  ret
  } // end of method Test::test19

  .method private hidebysig static unsigned int64 
          test20() cil managed
  {
    // Code size       23 (0x17)
    .maxstack  3
    .locals init (int32 V_0,
             int32 V_1,
             unsigned int64 V_2)
    IL_0004:  ldloc.0
    IL_0005:  ldc.i4.1
    IL_0006:  shr
    IL_0007:  ldc.i4.1
    IL_0008:  and
    IL_0009:  ldloc.1
    IL_000a:  ldc.i4.2
    IL_000b:  shl
    IL_000c:  sub
    IL_000d:  conv.i8
    IL_000e:  ldc.i4.s   11
    IL_0010:  conv.i8
    IL_0011:  add
    IL_0012:  stloc.2
    IL_0013:  br.s       IL_0015

    IL_0015:  ldloc.2
    IL_0016:  ret
  } // end of method Test::test20

  .method private hidebysig static unsigned int8 
          test21() cil managed
  {
    // Code size       14 (0xe)
    .maxstack  1
    .locals init (int32 V_0,
             unsigned int8 V_1)
    IL_0002:  ldloc.0
    IL_0003:  call       int32 Test_init_int32::noinline1(int32)
    IL_0008:  conv.u1
    IL_0009:  stloc.1
    IL_000a:  br.s       IL_000c

    IL_000c:  ldloc.1
    IL_000d:  ret
  } // end of method Test::test21

  .method private hidebysig static unsigned int16 
          test22() cil managed
  {
    // Code size       15 (0xf)
    .maxstack  1
    .locals init (int32 V_0,
             unsigned int16 V_1)
    IL_0002:  ldloca.s   V_0
    IL_0004:  call       int32 Test_init_int32::noinline2(int32&)
    IL_0009:  conv.ovf.u2
    IL_000a:  stloc.1
    IL_000b:  br.s       IL_000d

    IL_000d:  ldloc.1
    IL_000e:  ret
  } // end of method Test::test22

  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       709 (0x2c5)
    .maxstack  4
    .locals init (int32 V_0,
             int32[] V_1,
             int32[0...,0...,0...] V_2,
             int32 V_3)
    IL_0000:  call       int32 Test_init_int32::test1()
    IL_0005:  brfalse.s  IL_0019

    IL_0007:  ldstr      "Error 101"
    IL_000c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0011:  ldc.i4.s   101
    IL_0013:  stloc.3
    IL_0014:  br         IL_02c3

    IL_0019:  call       int32 Test_init_int32::test2()
    IL_001e:  ldc.i4.s   11
    IL_0020:  beq.s      IL_0034

    IL_0022:  ldstr      "Error 102"
    IL_0027:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002c:  ldc.i4.s   102
    IL_002e:  stloc.3
    IL_002f:  br         IL_02c3

    IL_0034:  call       int32 Test_init_int32::test3()
    IL_0039:  brfalse.s  IL_004d

    IL_003b:  ldstr      "Error 103"
    IL_0040:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0045:  ldc.i4.s   103
    IL_0047:  stloc.3
    IL_0048:  br         IL_02c3

    IL_004d:  call       int32 Test_init_int32::test4()
    IL_0052:  brfalse.s  IL_0066

    IL_0054:  ldstr      "Error 104"
    IL_0059:  call       void [System.Console]System.Console::WriteLine(string)
    IL_005e:  ldc.i4.s   104
    IL_0060:  stloc.3
    IL_0061:  br         IL_02c3

    IL_0066:  ldc.i4.0
    IL_0067:  stloc.0
    IL_0068:  ldloca.s   V_0
    IL_006a:  call       void Test_init_int32::test5(int32&)
    IL_006f:  ldloc.0
    IL_0070:  brfalse.s  IL_0084

    IL_0072:  ldstr      "Error 105"
    IL_0077:  call       void [System.Console]System.Console::WriteLine(string)
    IL_007c:  ldc.i4.s   105
    IL_007e:  stloc.3
    IL_007f:  br         IL_02c3

    IL_0084:  ldloca.s   V_0
    IL_0086:  call       void Test_init_int32::test6(int32&)
    IL_008b:  ldloc.0
    IL_008c:  brfalse.s  IL_00a0

    IL_008e:  ldstr      "Error 106"
    IL_0093:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0098:  ldc.i4.s   106
    IL_009a:  stloc.3
    IL_009b:  br         IL_02c3

    IL_00a0:  call       void Test_init_int32::test7()
    IL_00a5:  ldsfld     class Test_init_int32 Test_init_int32::global
    IL_00aa:  ldfld      int32 Test_init_int32::m_ret
    IL_00af:  brfalse.s  IL_00c3

    IL_00b1:  ldstr      "Error 107"
    IL_00b6:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00bb:  ldc.i4.s   107
    IL_00bd:  stloc.3
    IL_00be:  br         IL_02c3

    IL_00c3:  call       void Test_init_int32::test8()
    IL_00c8:  ldsfld     class Test_init_int32 Test_init_int32::global
    IL_00cd:  ldfld      int32 Test_init_int32::m_ret
    IL_00d2:  ldc.i4.s   11
    IL_00d4:  beq.s      IL_00e8

    IL_00d6:  ldstr      "Error 108"
    IL_00db:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00e0:  ldc.i4.s   108
    IL_00e2:  stloc.3
    IL_00e3:  br         IL_02c3

    IL_00e8:  call       void Test_init_int32::test9()
    IL_00ed:  ldsfld     class Test_init_int32 Test_init_int32::global
    IL_00f2:  ldfld      int32 Test_init_int32::m_ret
    IL_00f7:  brfalse.s  IL_010b

    IL_00f9:  ldstr      "Error 109"
    IL_00fe:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0103:  ldc.i4.s   109
    IL_0105:  stloc.3
    IL_0106:  br         IL_02c3

    IL_010b:  call       void Test_init_int32::test10()
    IL_0110:  ldsfld     class Test_init_int32 Test_init_int32::global
    IL_0115:  ldfld      int32 Test_init_int32::m_ret
    IL_011a:  brfalse.s  IL_012e

    IL_011c:  ldstr      "Error 110"
    IL_0121:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0126:  ldc.i4.s   110
    IL_0128:  stloc.3
    IL_0129:  br         IL_02c3

    IL_012e:  ldc.i4.5
    IL_012f:  newarr     [mscorlib]System.Int32
    IL_013f:  stloc.1
    IL_0140:  ldloc.1
    IL_0141:  ldc.i4.1
    IL_0142:  call       void Test_init_int32::test11(int32[],
                                           int32)
    IL_0147:  ldloc.1
    IL_0148:  ldc.i4.1
    IL_0149:  ldelem.i4
    IL_014a:  brfalse.s  IL_015e

    IL_014c:  ldstr      "Error 111"
    IL_0151:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0156:  ldc.i4.s   111
    IL_0158:  stloc.3
    IL_0159:  br         IL_02c3

    IL_015e:  ldloc.1
    IL_015f:  ldc.i4.2
    IL_0160:  call       void Test_init_int32::test12(int32[],
                                           int32)
    IL_0165:  ldloc.1
    IL_0166:  ldc.i4.2
    IL_0167:  ldelem.i4
    IL_0168:  ldc.i4.s   11
    IL_016a:  beq.s      IL_017e

    IL_016c:  ldstr      "Error 112"
    IL_0171:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0176:  ldc.i4.s   112
    IL_0178:  stloc.3
    IL_0179:  br         IL_02c3

    IL_017e:  ldloc.1
    IL_017f:  ldc.i4.3
    IL_0180:  call       void Test_init_int32::test13(int32[],
                                           int32)
    IL_0185:  ldloc.1
    IL_0186:  ldc.i4.3
    IL_0187:  ldelem.i4
    IL_0188:  brfalse.s  IL_019c

    IL_018a:  ldstr      "Error 113"
    IL_018f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0194:  ldc.i4.s   113
    IL_0196:  stloc.3
    IL_0197:  br         IL_02c3

    IL_019c:  ldloc.1
    IL_019d:  ldc.i4.4
    IL_019e:  call       void Test_init_int32::test14(int32[],
                                           int32)
    IL_01a3:  ldloc.1
    IL_01a4:  ldc.i4.4
    IL_01a5:  ldelem.i4
    IL_01a6:  brfalse.s  IL_01ba

    IL_01a8:  ldstr      "Error 114"
    IL_01ad:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01b2:  ldc.i4.s   114
    IL_01b4:  stloc.3
    IL_01b5:  br         IL_02c3

    IL_01ba:  ldc.i4.5
    IL_01bb:  ldc.i4.3
    IL_01bc:  ldc.i4.5
    IL_01bd:  newobj     instance void int32[0...,0...,0...]::.ctor(int32,
                                                                    int32,
                                                                    int32)
    IL_01c2:  stloc.2
    IL_01c3:  ldloc.2
    IL_01c4:  ldc.i4.1
    IL_01c5:  ldc.i4.1
    IL_01c6:  call       void Test_init_int32::test15(int32[0...,0...,0...],
                                           int32,
                                           int32)
    IL_01cb:  ldloc.2
    IL_01cc:  ldc.i4.1
    IL_01cd:  ldc.i4.2
    IL_01ce:  ldc.i4.1
    IL_01cf:  call       instance int32 int32[0...,0...,0...]::Get(int32,
                                                                   int32,
                                                                   int32)
    IL_01d4:  brfalse.s  IL_01e8

    IL_01d6:  ldstr      "Error 115"
    IL_01db:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01e0:  ldc.i4.s   115
    IL_01e2:  stloc.3
    IL_01e3:  br         IL_02c3

    IL_01e8:  ldloc.2
    IL_01e9:  ldc.i4.2
    IL_01ea:  ldc.i4.2
    IL_01eb:  call       void Test_init_int32::test16(int32[0...,0...,0...],
                                           int32,
                                           int32)
    IL_01f0:  ldloc.2
    IL_01f1:  ldc.i4.2
    IL_01f2:  ldc.i4.2
    IL_01f3:  ldc.i4.2
    IL_01f4:  call       instance int32 int32[0...,0...,0...]::Get(int32,
                                                                   int32,
                                                                   int32)
    IL_01f9:  ldc.i4.s   11
    IL_01fb:  beq.s      IL_020f

    IL_01fd:  ldstr      "Error 116"
    IL_0202:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0207:  ldc.i4.s   116
    IL_0209:  stloc.3
    IL_020a:  br         IL_02c3

    IL_020f:  ldloc.2
    IL_0210:  ldc.i4.3
    IL_0211:  ldc.i4.3
    IL_0212:  call       void Test_init_int32::test17(int32[0...,0...,0...],
                                           int32,
                                           int32)
    IL_0217:  ldloc.2
    IL_0218:  ldc.i4.3
    IL_0219:  ldc.i4.2
    IL_021a:  ldc.i4.3
    IL_021b:  call       instance int32 int32[0...,0...,0...]::Get(int32,
                                                                   int32,
                                                                   int32)
    IL_0220:  brfalse.s  IL_0234

    IL_0222:  ldstr      "Error 117"
    IL_0227:  call       void [System.Console]System.Console::WriteLine(string)
    IL_022c:  ldc.i4.s   117
    IL_022e:  stloc.3
    IL_022f:  br         IL_02c3

    IL_0234:  ldloc.2
    IL_0235:  ldc.i4.4
    IL_0236:  ldc.i4.4
    IL_0237:  call       void Test_init_int32::test18(int32[0...,0...,0...],
                                           int32,
                                           int32)
    IL_023c:  ldloc.2
    IL_023d:  ldc.i4.4
    IL_023e:  ldc.i4.2
    IL_023f:  ldc.i4.4
    IL_0240:  call       instance int32 int32[0...,0...,0...]::Get(int32,
                                                                   int32,
                                                                   int32)
    IL_0245:  brfalse.s  IL_0256

    IL_0247:  ldstr      "Error 118"
    IL_024c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0251:  ldc.i4.s   118
    IL_0253:  stloc.3
    IL_0254:  br.s       IL_02c3

    IL_0256:  call       int64 Test_init_int32::test19()
    IL_025b:  ldc.i4.0
    IL_025c:  conv.i8
    IL_025d:  beq.s      IL_026e

    IL_025f:  ldstr      "Error 119"
    IL_0264:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0269:  ldc.i4.s   119
    IL_026b:  stloc.3
    IL_026c:  br.s       IL_02c3

    IL_026e:  call       unsigned int64 Test_init_int32::test20()
    IL_0273:  ldc.i4.s   11
    IL_0275:  conv.i8
    IL_0276:  beq.s      IL_0287

    IL_0278:  ldstr      "Error 120"
    IL_027d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0282:  ldc.i4.s   120
    IL_0284:  stloc.3
    IL_0285:  br.s       IL_02c3

    IL_0287:  call       unsigned int8 Test_init_int32::test21()
    IL_028c:  brfalse.s  IL_029d

    IL_028e:  ldstr      "Error 121"
    IL_0293:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0298:  ldc.i4.s   121
    IL_029a:  stloc.3
    IL_029b:  br.s       IL_02c3

    IL_029d:  call       unsigned int16 Test_init_int32::test22()
    IL_02a2:  conv.i4
    IL_02a3:  brfalse.s  IL_02b4

    IL_02a5:  ldstr      "Error 122"
    IL_02aa:  call       void [System.Console]System.Console::WriteLine(string)
    IL_02af:  ldc.i4.s   122
    IL_02b1:  stloc.3
    IL_02b2:  br.s       IL_02c3

    IL_02b4:  ldstr      "Passed"
    IL_02b9:  call       void [System.Console]System.Console::WriteLine(string)
    IL_02be:  ldc.i4.s   100
    IL_02c0:  stloc.3
    IL_02c1:  br.s       IL_02c3

    IL_02c3:  ldloc.3
    IL_02c4:  ret
  } // end of method Test::Main

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    IL_0000:  newobj     instance void Test_init_int32::.ctor()
    IL_0005:  stsfld     class Test_init_int32 Test_init_int32::global
    IL_000a:  ret
  } // end of method Test::.cctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       14 (0xe)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.0
    IL_0002:  stfld      int32 Test_init_int32::m_ret
    IL_0007:  ldarg.0
    IL_0008:  call       instance void [mscorlib]System.Object::.ctor()
    IL_000d:  ret
  } // end of method Test::.ctor

} // end of class Test
