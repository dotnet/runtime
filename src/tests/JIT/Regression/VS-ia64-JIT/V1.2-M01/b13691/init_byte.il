// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}





// Metadata version: v1.2.2123
.assembly extern mscorlib
{
  .ver 0:0:0:0
}
.assembly init_byte
{

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                bool) = ( 01 00 01 01 00 00 ) 

  .ver 0:0:0:0
}
.assembly extern xunit.core {}
.module init_byte.exe
// MVID: {FDCD7B4C-5230-4304-82F7-A3D10057533E}
.imagebase 0x00400000
.file alignment 0x00001000
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x03EE0000

// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit Test_init_byte
       extends [mscorlib]System.Object
{
  .field private int8 m_ret
  .field private static class Test_init_byte global
  .method private hidebysig static int8  noinline1(int8 'ret') cil managed
  {
    // Code size       4 (0x4)
    .maxstack  1
    IL_0000:  ldarga.s   'ret'
    IL_0002:  ldind.i1
    IL_0003:  ret
  } // end of method Test::noinline1

  .method private hidebysig static int8  noinline2(int8& 'ret') cil managed
  {
    // Code size       5 (0x5)
    .maxstack  1
    IL_0000:  ldarga.s   'ret'
    IL_0002:  ldind.i
    IL_0003:  ldind.i1
    IL_0004:  ret
  } // end of method Test::noinline2

  .method private hidebysig static int8  test1() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  1
    .locals init (int8 V_0,
             int8 V_1)
    IL_0000:  ldloc.1
    IL_0001:  ret
  } // end of method Test::test1

  .method private hidebysig static int8  test2() cil managed
  {
    // Code size       24 (0x18)
    .maxstack  3
    .locals init (int8 V_0,
             int8 V_1,
             int8 V_2)
    IL_0000:  ldloc.0
    IL_0001:  ldc.i4.1
    IL_0002:  shr
    IL_0003:  ldc.i4     0x1
    IL_0008:  and
    IL_0009:  ldloc.1
    IL_000a:  ldc.i4.2
    IL_000b:  shl
    IL_000c:  sub
    IL_000d:  ldc.i4     0xb
    IL_0012:  add
    IL_0013:  stloc.2
    IL_0014:  br.s       IL_0016

    IL_0016:  ldloc.2
    IL_0017:  ret
  } // end of method Test::test2

  .method private hidebysig static int8  test3() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (int8 V_0,
             int8 V_1)
    IL_0000:  ldloc.0
    IL_0001:  call       int8 Test_init_byte::noinline1(int8)
    IL_0006:  stloc.1
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.1
    IL_000a:  ret
  } // end of method Test::test3

  .method private hidebysig static int8  test4() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (int8 V_0,
             int8 V_1)
    IL_0000:  ldloca.s   V_0
    IL_0002:  call       int8 Test_init_byte::noinline2(int8&)
    IL_0007:  stloc.1
    IL_0008:  br.s       IL_000a

    IL_000a:  ldloc.1
    IL_000b:  ret
  } // end of method Test::test4

  .method private hidebysig static void  test5(int8& 'ret') cil managed
  {
    // Code size       9 (0x9)
    .maxstack  2
    .locals init (int8 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldloc.0
    IL_0002:  call       int8 Test_init_byte::noinline1(int8)
    IL_0007:  stind.i1
    IL_0008:  ret
  } // end of method Test::test5

  .method private hidebysig static void  test6(int8& 'ret') cil managed
  {
    // Code size       10 (0xa)
    .maxstack  2
    .locals init (int8 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldloca.s   V_0
    IL_0003:  call       int8 Test_init_byte::noinline2(int8&)
    IL_0008:  stind.i1
    IL_0009:  ret
  } // end of method Test::test6

  .method private hidebysig static void  test7() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  2
    .locals init (int8 V_0)
    IL_0000:  ldsfld     class Test_init_byte Test_init_byte::global
    IL_0005:  ldloc.0
    IL_0006:  stfld      int8 Test_init_byte::m_ret
    IL_000b:  ret
  } // end of method Test::test7

  .method private hidebysig static void  test8() cil managed
  {
    // Code size       30 (0x1e)
    .maxstack  4
    .locals init (int8 V_0,
             int8 V_1)
    IL_0000:  ldsfld     class Test_init_byte Test_init_byte::global
    IL_0005:  ldloc.0
    IL_0006:  ldc.i4.1
    IL_0007:  shr
    IL_0008:  ldc.i4     0x1
    IL_000d:  and
    IL_000e:  ldloc.1
    IL_000f:  ldc.i4.2
    IL_0010:  shl
    IL_0011:  sub
    IL_0012:  ldc.i4     0xb
    IL_0017:  add
    IL_0018:  stfld      int8 Test_init_byte::m_ret
    IL_001d:  ret
  } // end of method Test::test8

  .method private hidebysig static void  test9() cil managed
  {
    // Code size       17 (0x11)
    .maxstack  2
    .locals init (int8 V_0)
    IL_0000:  ldsfld     class Test_init_byte Test_init_byte::global
    IL_0005:  ldloc.0
    IL_0006:  call       int8 Test_init_byte::noinline1(int8)
    IL_000b:  stfld      int8 Test_init_byte::m_ret
    IL_0010:  ret
  } // end of method Test::test9

  .method private hidebysig static void  test10() cil managed
  {
    // Code size       18 (0x12)
    .maxstack  2
    .locals init (int8 V_0)
    IL_0000:  ldsfld     class Test_init_byte Test_init_byte::global
    IL_0005:  ldloca.s   V_0
    IL_0007:  call       int8 Test_init_byte::noinline2(int8&)
    IL_000c:  stfld      int8 Test_init_byte::m_ret
    IL_0011:  ret
  } // end of method Test::test10

  .method private hidebysig static void  test11(int8[] arr,
                                                int8 index) cil managed
  {
    // Code size       6 (0x6)
    .maxstack  3
    .locals init (int8 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  conv.i4
    IL_0003:  ldloc.0
    IL_0004:  stelem.i1
    IL_0005:  ret
  } // end of method Test::test11

  .method private hidebysig static void  test12(int8[] arr,
                                                int8 index) cil managed
  {
    // Code size       24 (0x18)
    .maxstack  5
    .locals init (int8 V_0,
             int8 V_1)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  conv.i4
    IL_0003:  ldloc.0
    IL_0004:  ldc.i4.1
    IL_0005:  shr
    IL_0006:  ldc.i4     0x1
    IL_000b:  and
    IL_000c:  ldloc.1
    IL_000d:  ldc.i4.2
    IL_000e:  shl
    IL_000f:  sub
    IL_0010:  ldc.i4     0xb
    IL_0015:  add
    IL_0016:  stelem.i1
    IL_0017:  ret
  } // end of method Test::test12

  .method private hidebysig static void  test13(int8[] arr,
                                                int8 index) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  3
    .locals init (int8 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  conv.i4
    IL_0003:  ldloc.0
    IL_0004:  call       int8 Test_init_byte::noinline1(int8)
    IL_0009:  stelem.i1
    IL_000a:  ret
  } // end of method Test::test13

  .method private hidebysig static void  test14(int8[] arr,
                                                int8 index) cil managed
  {
    // Code size       12 (0xc)
    .maxstack  3
    .locals init (int8 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  conv.i4
    IL_0003:  ldloca.s   V_0
    IL_0005:  call       int8 Test_init_byte::noinline2(int8&)
    IL_000a:  stelem.i1
    IL_000b:  ret
  } // end of method Test::test14

  .method private hidebysig static void  test15(int8[0...,0...,0...] arr,
                                                int8 index1,
                                                int8 index3) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  5
    .locals init (int8 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  conv.i4
    IL_0003:  ldc.i4     0x2
    IL_0008:  conv.i4
    IL_0009:  ldarg.2
    IL_000a:  conv.i4
    IL_000b:  ldloc.0
    IL_000c:  call       instance void int8[0...,0...,0...]::Set(int32,
                                                                 int32,
                                                                 int32,
                                                                 int8)
    IL_0011:  ret
  } // end of method Test::test15

  .method private hidebysig static void  test16(int8[0...,0...,0...] arr,
                                                int8 index1,
                                                int8 index3) cil managed
  {
    // Code size       31 (0x1f)
    .maxstack  7
    .locals init (int8 V_0,
             int8 V_1)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  conv.i4
    IL_0003:  ldc.i4.2
    IL_0004:  ldarg.2
    IL_0005:  conv.i4
    IL_0006:  ldloc.0
    IL_0007:  ldc.i4.1
    IL_0008:  shr
    IL_0009:  ldc.i4     0x1
    IL_000e:  and
    IL_000f:  ldloc.1
    IL_0010:  ldc.i4.2
    IL_0011:  shl
    IL_0012:  sub
    IL_0013:  ldc.i4     0xb
    IL_0018:  add
    IL_0019:  call       instance void int8[0...,0...,0...]::Set(int32,
                                                                 int32,
                                                                 int32,
                                                                 int8)
    IL_001e:  ret
  } // end of method Test::test16

  .method private hidebysig static void  test17(int8[0...,0...,0...] arr,
                                                int8 index1,
                                                int8 index3) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  5
    .locals init (int8 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  conv.i4
    IL_0003:  ldc.i4.2
    IL_0004:  ldarg.2
    IL_0005:  conv.i4
    IL_0006:  ldloc.0
    IL_0007:  call       int8 Test_init_byte::noinline1(int8)
    IL_000c:  call       instance void int8[0...,0...,0...]::Set(int32,
                                                                 int32,
                                                                 int32,
                                                                 int8)
    IL_0011:  ret
  } // end of method Test::test17

  .method private hidebysig static void  test18(int8[0...,0...,0...] arr,
                                                int8 index1,
                                                int8 index3) cil managed
  {
    // Code size       24 (0x18)
    .maxstack  5
    .locals init (int8 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  conv.i4
    IL_0003:  ldc.i4     0x2
    IL_0008:  conv.i
    IL_0009:  ldarg.2
    IL_000a:  conv.u4
    IL_000b:  ldloca.s   V_0
    IL_000d:  call       int8 Test_init_byte::noinline2(int8&)
    IL_0012:  call       instance void int8[0...,0...,0...]::Set(int32,
                                                                 int32,
                                                                 int32,
                                                                 int8)
    IL_0017:  ret
  } // end of method Test::test18

  .method private hidebysig static int8  test19() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init (int8 V_0,
             int8 V_1)
    IL_0000:  ldloc.0
    IL_0001:  conv.i4
    IL_0002:  stloc.1
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.1
    IL_0006:  ret
  } // end of method Test::test19

  .method private hidebysig static uint8 
          test20() cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init (int8 V_0,
             int8 V_1,
             uint8 V_2)
    IL_0000:  ldloc.0
    IL_0001:  ldc.i4.1
    IL_0002:  shr
    IL_0003:  ldc.i4     0x1
    IL_0008:  and
    IL_0009:  ldloc.1
    IL_000a:  ldc.i4.s   2
    IL_000c:  shl
    IL_000d:  sub
    IL_000e:  conv.i4
    IL_000f:  ldc.i4     0xb
    IL_0014:  conv.i4
    IL_0015:  add
    IL_0016:  stloc.2
    IL_0017:  br.s       IL_0019

    IL_0019:  ldloc.2
    IL_001a:  ret
  } // end of method Test::test20

  .method private hidebysig static uint8 
          test21() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (int8 V_0,
             uint8 V_1)
    IL_0000:  ldloc.0
    IL_0001:  call       int8 Test_init_byte::noinline1(int8)
    IL_0006:  conv.u1
    IL_0007:  stloc.1
    IL_0008:  br.s       IL_000a

    IL_000a:  ldloc.1
    IL_000b:  ret
  } // end of method Test::test21

  .method private hidebysig static uint16 
          test22() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  1
    .locals init (int8 V_0,
             uint16 V_1)
    IL_0000:  ldloca.s   V_0
    IL_0002:  call       int8 Test_init_byte::noinline2(int8&)
    IL_0007:  conv.ovf.u2
    IL_0008:  stloc.1
    IL_0009:  br.s       IL_000b

    IL_000b:  ldloc.1
    IL_000c:  ret
  } // end of method Test::test22

  .method private hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       842 (0x34a)
    .maxstack  4
    .locals init (int8 V_0,
             int8[] V_1,
             int8[0...,0...,0...] V_2,
             int8 V_3)
    IL_0000:  call       int8 Test_init_byte::test1()
    IL_0005:  brfalse.s  IL_001c

    IL_0007:  ldstr      "Error 101"
    IL_000c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0011:  ldc.i4     0x65
    IL_0016:  stloc.3
    IL_0017:  br         IL_0347

    IL_001c:  call       int8 Test_init_byte::test2()
    IL_0021:  ldc.i4     0xb
    IL_0026:  beq.s      IL_003d

    IL_0028:  ldstr      "Error 102"
    IL_002d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0032:  ldc.i4     0x66
    IL_0037:  stloc.3
    IL_0038:  br         IL_0347

    IL_003d:  call       int8 Test_init_byte::test3()
    IL_0042:  brfalse.s  IL_0059

    IL_0044:  ldstr      "Error 103"
    IL_0049:  call       void [System.Console]System.Console::WriteLine(string)
    IL_004e:  ldc.i4     0x67
    IL_0053:  stloc.3
    IL_0054:  br         IL_0347

    IL_0059:  call       int8 Test_init_byte::test4()
    IL_005e:  brfalse.s  IL_0075

    IL_0060:  ldstr      "Error 104"
    IL_0065:  call       void [System.Console]System.Console::WriteLine(string)
    IL_006a:  ldc.i4     0x68
    IL_006f:  stloc.3
    IL_0070:  br         IL_0347

    IL_0075:  ldc.i4     0x0
    IL_007a:  stloc.0
    IL_007b:  ldloca.s   V_0
    IL_007d:  call       void Test_init_byte::test5(int8&)
    IL_0082:  ldloc.0
    IL_0083:  brfalse.s  IL_009a

    IL_0085:  ldstr      "Error 105"
    IL_008a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_008f:  ldc.i4     0x69
    IL_0094:  stloc.3
    IL_0095:  br         IL_0347

    IL_009a:  ldloca.s   V_0
    IL_009c:  call       void Test_init_byte::test6(int8&)
    IL_00a1:  ldloc.0
    IL_00a2:  brfalse.s  IL_00b9

    IL_00a4:  ldstr      "Error 106"
    IL_00a9:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00ae:  ldc.i4     0x6a
    IL_00b3:  stloc.3
    IL_00b4:  br         IL_0347

    IL_00b9:  call       void Test_init_byte::test7()
    IL_00be:  ldsfld     class Test_init_byte Test_init_byte::global
    IL_00c3:  ldfld      int8 Test_init_byte::m_ret
    IL_00c8:  brfalse.s  IL_00df

    IL_00ca:  ldstr      "Error 107"
    IL_00cf:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00d4:  ldc.i4     0x6b
    IL_00d9:  stloc.3
    IL_00da:  br         IL_0347

    IL_00df:  call       void Test_init_byte::test8()
    IL_00e4:  ldsfld     class Test_init_byte Test_init_byte::global
    IL_00e9:  ldfld      int8 Test_init_byte::m_ret
    IL_00ee:  ldc.i4     0xb
    IL_00f3:  beq.s      IL_010a

    IL_00f5:  ldstr      "Error 108"
    IL_00fa:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00ff:  ldc.i4     0x6c
    IL_0104:  stloc.3
    IL_0105:  br         IL_0347

    IL_010a:  call       void Test_init_byte::test9()
    IL_010f:  ldsfld     class Test_init_byte Test_init_byte::global
    IL_0114:  ldfld      int8 Test_init_byte::m_ret
    IL_0119:  brfalse.s  IL_0130

    IL_011b:  ldstr      "Error 109"
    IL_0120:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0125:  ldc.i4     0x6d
    IL_012a:  stloc.3
    IL_012b:  br         IL_0347

    IL_0130:  call       void Test_init_byte::test10()
    IL_0135:  ldsfld     class Test_init_byte Test_init_byte::global
    IL_013a:  ldfld      int8 Test_init_byte::m_ret
    IL_013f:  brfalse.s  IL_0156

    IL_0141:  ldstr      "Error 110"
    IL_0146:  call       void [System.Console]System.Console::WriteLine(string)
    IL_014b:  ldc.i4     0x6e
    IL_0150:  stloc.3
    IL_0151:  br         IL_0347

    IL_0156:  ldc.i4.5
    IL_0157:  newarr     [mscorlib]System.SByte
    IL_015c:  stloc.1
    IL_015d:  ldloc.1
    IL_015e:  ldc.i4     0x1
    IL_0163:  call       void Test_init_byte::test11(int8[],
                                           int8)
    IL_0168:  ldloc.1
    IL_0169:  ldc.i4.1
    IL_016a:  ldelem.i1
    IL_016b:  brfalse.s  IL_0182

    IL_016d:  ldstr      "Error 111"
    IL_0172:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0177:  ldc.i4     0x6f
    IL_017c:  stloc.3
    IL_017d:  br         IL_0347

    IL_0182:  ldloc.1
    IL_0183:  ldc.i4     0x2
    IL_0188:  call       void Test_init_byte::test12(int8[],
                                           int8)
    IL_018d:  ldloc.1
    IL_018e:  ldc.i4.2
    IL_018f:  ldelem.i1
    IL_0190:  ldc.i4     0xb
    IL_0195:  beq.s      IL_01ac

    IL_0197:  ldstr      "Error 112"
    IL_019c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01a1:  ldc.i4     0x70
    IL_01a6:  stloc.3
    IL_01a7:  br         IL_0347

    IL_01ac:  ldloc.1
    IL_01ad:  ldc.i4     0x3
    IL_01b2:  call       void Test_init_byte::test13(int8[],
                                           int8)
    IL_01b7:  ldloc.1
    IL_01b8:  ldc.i4.3
    IL_01b9:  ldelem.i1
    IL_01ba:  brfalse.s  IL_01d1

    IL_01bc:  ldstr      "Error 113"
    IL_01c1:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01c6:  ldc.i4     0x71
    IL_01cb:  stloc.3
    IL_01cc:  br         IL_0347

    IL_01d1:  ldloc.1
    IL_01d2:  ldc.i4     0x4
    IL_01d7:  call       void Test_init_byte::test14(int8[],
                                           int8)
    IL_01dc:  ldloc.1
    IL_01dd:  ldc.i4.4
    IL_01de:  ldelem.i1
    IL_01df:  brfalse.s  IL_01f6

    IL_01e1:  ldstr      "Error 114"
    IL_01e6:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01eb:  ldc.i4     0x72
    IL_01f0:  stloc.3
    IL_01f1:  br         IL_0347

    IL_01f6:  ldc.i4.5
    IL_01f7:  ldc.i4.3
    IL_01f8:  ldc.i4.5
    IL_01f9:  newobj     instance void int8[0...,0...,0...]::.ctor(int32,
                                                                   int32,
                                                                   int32)
    IL_01fe:  stloc.2
    IL_01ff:  ldloc.2
    IL_0200:  ldc.i4     0x1
    IL_0205:  ldc.i4     0x1
    IL_020a:  call       void Test_init_byte::test15(int8[0...,0...,0...],
                                           int8,
                                           int8)
    IL_020f:  ldloc.2
    IL_0210:  ldc.i4.1
    IL_0211:  ldc.i4.2
    IL_0212:  ldc.i4.1
    IL_0213:  call       instance int8 int8[0...,0...,0...]::Get(int32,
                                                                 int32,
                                                                 int32)
    IL_0218:  brfalse.s  IL_022f

    IL_021a:  ldstr      "Error 115"
    IL_021f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0224:  ldc.i4     0x73
    IL_0229:  stloc.3
    IL_022a:  br         IL_0347

    IL_022f:  ldloc.2
    IL_0230:  ldc.i4     0x2
    IL_0235:  ldc.i4     0x2
    IL_023a:  call       void Test_init_byte::test16(int8[0...,0...,0...],
                                           int8,
                                           int8)
    IL_023f:  ldloc.2
    IL_0240:  ldc.i4.2
    IL_0241:  dup
    IL_0242:  dup
    IL_0243:  call       instance int8 int8[0...,0...,0...]::Get(int32,
                                                                 int32,
                                                                 int32)
    IL_0248:  ldc.i4     0xb
    IL_024d:  beq.s      IL_0264

    IL_024f:  ldstr      "Error 116"
    IL_0254:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0259:  ldc.i4     0x74
    IL_025e:  stloc.3
    IL_025f:  br         IL_0347

    IL_0264:  ldloc.2
    IL_0265:  ldc.i4     0x3
    IL_026a:  ldc.i4     0x3
    IL_026f:  call       void Test_init_byte::test17(int8[0...,0...,0...],
                                           int8,
                                           int8)
    IL_0274:  ldloc.2
    IL_0275:  ldc.i4.3
    IL_0276:  ldc.i4.2
    IL_0277:  ldc.i4.3
    IL_0278:  call       instance int8 int8[0...,0...,0...]::Get(int32,
                                                                 int32,
                                                                 int32)
    IL_027d:  brfalse.s  IL_0294

    IL_027f:  ldstr      "Error 117"
    IL_0284:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0289:  ldc.i4     0x75
    IL_028e:  stloc.3
    IL_028f:  br         IL_0347

    IL_0294:  ldloc.2
    IL_0295:  ldc.i4     0x4
    IL_029a:  ldc.i4     0x4
    IL_029f:  call       void Test_init_byte::test18(int8[0...,0...,0...],
                                           int8,
                                           int8)
    IL_02a4:  ldloc.2
    IL_02a5:  ldc.i4.4
    IL_02a6:  ldc.i4.2
    IL_02a7:  ldc.i4.4
    IL_02a8:  call       instance int8 int8[0...,0...,0...]::Get(int32,
                                                                 int32,
                                                                 int32)
    IL_02ad:  brfalse.s  IL_02c4

    IL_02af:  ldstr      "Error 118"
    IL_02b4:  call       void [System.Console]System.Console::WriteLine(string)
    IL_02b9:  ldc.i4     0x76
    IL_02be:  stloc.3
    IL_02bf:  br         IL_0347

    IL_02c4:  call       int8 Test_init_byte::test19()
    IL_02c9:  ldc.i4     0x0
    IL_02ce:  conv.i4
    IL_02cf:  beq.s      IL_02e3

    IL_02d1:  ldstr      "Error 119"
    IL_02d6:  call       void [System.Console]System.Console::WriteLine(string)
    IL_02db:  ldc.i4     0x77
    IL_02e0:  stloc.3
    IL_02e1:  br.s       IL_0347

    IL_02e3:  call       uint8 Test_init_byte::test20()
    IL_02e8:  ldc.i4     0xb
    IL_02ed:  conv.i4
    IL_02ee:  beq.s      IL_0302

    IL_02f0:  ldstr      "Error 120"
    IL_02f5:  call       void [System.Console]System.Console::WriteLine(string)
    IL_02fa:  ldc.i4     0x78
    IL_02ff:  stloc.3
    IL_0300:  br.s       IL_0347

    IL_0302:  call       uint8 Test_init_byte::test21()
    IL_0307:  brfalse.s  IL_031b

    IL_0309:  ldstr      "Error 121"
    IL_030e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0313:  ldc.i4     0x79
    IL_0318:  stloc.3
    IL_0319:  br.s       IL_0347

    IL_031b:  call       uint16 Test_init_byte::test22()
    IL_0320:  conv.i4
    IL_0321:  brfalse.s  IL_0335

    IL_0323:  ldstr      "Error 122"
    IL_0328:  call       void [System.Console]System.Console::WriteLine(string)
    IL_032d:  ldc.i4     0x7a
    IL_0332:  stloc.3
    IL_0333:  br.s       IL_0347

    IL_0335:  ldstr      "Passed"
    IL_033a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_033f:  ldc.i4     0x64
    IL_0344:  stloc.3
    IL_0345:  br.s       IL_0347

    IL_0347:  ldloc.3
    IL_0348:  conv.ovf.i4
    IL_0349:  ret
  } // end of method Test::Main

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    IL_0000:  newobj     instance void Test_init_byte::.ctor()
    IL_0005:  stsfld     class Test_init_byte Test_init_byte::global
    IL_000a:  ret
  } // end of method Test::.cctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       18 (0x12)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4     0x0
    IL_0006:  stfld      int8 Test_init_byte::m_ret
    IL_000b:  ldarg.0
    IL_000c:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0011:  ret
  } // end of method Test::.ctor

} // end of class Test


// =============================================================

//*********** DISASSEMBLY COMPLETE ***********************
