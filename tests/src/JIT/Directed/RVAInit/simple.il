// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib { }
.assembly simple
{
  .permissionset reqmin = (3C 00 50 00 65 00 72 00 6D 00 69 00 73 00 73 00   // <.P.e.r.m.i.s.s.
                           69 00 6F 00 6E 00 53 00 65 00 74 00 20 00 63 00   // i.o.n.S.e.t. .c.
                           6C 00 61 00 73 00 73 00 3D 00 22 00 53 00 79 00   // l.a.s.s.=.".S.y.
                           73 00 74 00 65 00 6D 00 2E 00 53 00 65 00 63 00   // s.t.e.m...S.e.c.
                           75 00 72 00 69 00 74 00 79 00 2E 00 50 00 65 00   // u.r.i.t.y...P.e.
                           72 00 6D 00 69 00 73 00 73 00 69 00 6F 00 6E 00   // r.m.i.s.s.i.o.n.
                           53 00 65 00 74 00 22 00 0D 00 0A 00 20 00 20 00   // S.e.t."..... . .
                           20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00   //  . . . . . . . .
                           20 00 20 00 20 00 20 00 20 00 76 00 65 00 72 00   //  . . . . .v.e.r.
                           73 00 69 00 6F 00 6E 00 3D 00 22 00 31 00 22 00   // s.i.o.n.=.".1.".
                           3E 00 0D 00 0A 00 20 00 20 00 20 00 3C 00 49 00   // >..... . . .<.I.
                           50 00 65 00 72 00 6D 00 69 00 73 00 73 00 69 00   // P.e.r.m.i.s.s.i.
                           6F 00 6E 00 20 00 63 00 6C 00 61 00 73 00 73 00   // o.n. .c.l.a.s.s.
                           3D 00 22 00 53 00 79 00 73 00 74 00 65 00 6D 00   // =.".S.y.s.t.e.m.
                           2E 00 53 00 65 00 63 00 75 00 72 00 69 00 74 00   // ..S.e.c.u.r.i.t.
                           79 00 2E 00 50 00 65 00 72 00 6D 00 69 00 73 00   // y...P.e.r.m.i.s.
                           73 00 69 00 6F 00 6E 00 73 00 2E 00 53 00 65 00   // s.i.o.n.s...S.e.
                           63 00 75 00 72 00 69 00 74 00 79 00 50 00 65 00   // c.u.r.i.t.y.P.e.
                           72 00 6D 00 69 00 73 00 73 00 69 00 6F 00 6E 00   // r.m.i.s.s.i.o.n.
                           2C 00 20 00 6D 00 73 00 63 00 6F 00 72 00 6C 00   // ,. .m.s.c.o.r.l.
                           69 00 62 00 2C 00 20 00 56 00 65 00 72 00 73 00   // i.b.,. .V.e.r.s.
                           69 00 6F 00 6E 00 3D 00 31 00 2E 00 30 00 2E 00   // i.o.n.=.1...0...
                           32 00 34 00 31 00 31 00 2E 00 30 00 2C 00 20 00   // 2.4.1.1...0.,. .
                           43 00 75 00 6C 00 74 00 75 00 72 00 65 00 3D 00   // C.u.l.t.u.r.e.=.
                           6E 00 65 00 75 00 74 00 72 00 61 00 6C 00 2C 00   // n.e.u.t.r.a.l.,.
                           20 00 50 00 75 00 62 00 6C 00 69 00 63 00 4B 00   //  .P.u.b.l.i.c.K.
                           65 00 79 00 54 00 6F 00 6B 00 65 00 6E 00 3D 00   // e.y.T.o.k.e.n.=.
                           62 00 37 00 37 00 61 00 35 00 63 00 35 00 36 00   // b.7.7.a.5.c.5.6.
                           31 00 39 00 33 00 34 00 65 00 30 00 38 00 39 00   // 1.9.3.4.e.0.8.9.
                           22 00 0D 00 0A 00 20 00 20 00 20 00 20 00 20 00   // "..... . . . . .
                           20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00   //  . . . . . . . .
                           20 00 20 00 20 00 76 00 65 00 72 00 73 00 69 00   //  . . .v.e.r.s.i.
                           6F 00 6E 00 3D 00 22 00 31 00 22 00 0D 00 0A 00   // o.n.=.".1.".....
                           20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00   //  . . . . . . . .
                           20 00 20 00 20 00 20 00 20 00 20 00 20 00 20 00   //  . . . . . . . .
                           46 00 6C 00 61 00 67 00 73 00 3D 00 22 00 53 00   // F.l.a.g.s.=.".S.
                           6B 00 69 00 70 00 56 00 65 00 72 00 69 00 66 00   // k.i.p.V.e.r.i.f.
                           69 00 63 00 61 00 74 00 69 00 6F 00 6E 00 22 00   // i.c.a.t.i.o.n.".
                           2F 00 3E 00 0D 00 0A 00 3C 00 2F 00 50 00 65 00   // /.>.....<./.P.e.
                           72 00 6D 00 69 00 73 00 73 00 69 00 6F 00 6E 00   // r.m.i.s.s.i.o.n.
                           53 00 65 00 74 00 3E 00 0D 00 0A 00 )             // S.e.t.>.....
}
.class private sequential ansi sealed beforefieldinit Test
       extends [mscorlib]System.ValueType
{
  .field public unsigned int8 _byte
  .field public int8 _sbyte
  .field public int16 _short
  .field public unsigned int16 _ushort
  .field public int32 _int
  .field public unsigned int32 _uint
  .field public int64 _long
  .field public unsigned int64 _ulong
  .field public float32 _float
  .field public float64 _double
  .field public static valuetype Test static_test at D_1
  .method private hidebysig static char  hex(unsigned int8 v) cil managed
  {
    .maxstack  3
    .locals (char V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.s   9
    IL_0003:  bgt.s      IL_000b

    IL_0005:  ldarg.0
    IL_0006:  ldc.i4.s   48
    IL_0008:  add
    IL_0009:  br.s       IL_0012

    IL_000b:  ldarg.0
    IL_000c:  ldc.i4.s   10
    IL_000e:  sub
    IL_000f:  ldc.i4.s   65
    IL_0011:  add
    IL_0012:  conv.u2
    IL_0013:  stloc.0
    IL_0014:  br.s       IL_0016

    IL_0016:  ldloc.0
    IL_0017:  ret
  } // end of method Test::hex

  .method private hidebysig static void  DumpStruct(valuetype Test& test) cil managed
  {
    .maxstack  2
    .locals (valuetype Test& pinned V_0,
             unsigned int8* V_1,
             int32 V_2)
    IL_0000:  ldarg.0
    IL_0001:  stloc.0
    IL_0002:  ldstr      ".data D_1 = bytearray( "
    IL_0007:  call       void [System.Console]System.Console::Write(string)
    IL_000c:  ldloc.0
    IL_000d:  conv.i
    IL_000e:  stloc.1
    IL_000f:  ldc.i4.0
    IL_0010:  stloc.2
    IL_0011:  br.s       IL_0051

    IL_0013:  ldloc.1
    IL_0014:  ldloc.2
    IL_0015:  add
    IL_0016:  ldind.u1
    IL_0017:  ldc.i4.4
    IL_0018:  shr
    IL_0019:  conv.u1
    IL_001a:  call       char Test::hex(unsigned int8)
    IL_001f:  call       void [System.Console]System.Console::Write(char)
    IL_0024:  ldloc.1
    IL_0025:  ldloc.2
    IL_0026:  add
    IL_0027:  ldind.u1
    IL_0028:  ldc.i4.s   15
    IL_002a:  and
    IL_002b:  conv.u1
    IL_002c:  call       char Test::hex(unsigned int8)
    IL_0031:  call       void [System.Console]System.Console::Write(char)
    IL_0036:  ldstr      " "
    IL_003b:  call       void [System.Console]System.Console::Write(string)
    IL_0040:  ldloc.2
    IL_0041:  ldc.i4.s   16
    IL_0043:  rem
    IL_0044:  ldc.i4.s   15
    IL_0046:  bne.un.s   IL_004d

    IL_0048:  call       void [System.Console]System.Console::WriteLine()
    IL_004d:  ldloc.2
    IL_004e:  ldc.i4.1
    IL_004f:  add
    IL_0050:  stloc.2
    IL_0051:  ldloc.2
    IL_0052:  sizeof     Test
    IL_0058:  blt.s      IL_0013

    IL_005a:  ldstr      " )"
    IL_005f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0064:  ldc.i4.0
    IL_0065:  conv.u
    IL_0066:  stloc.0
    IL_0067:  ret
  } // end of method Test::DumpStruct

  .method private hidebysig static void  InitStruct(valuetype Test& test) cil managed
  {
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4     0xde
    IL_0006:  stfld      unsigned int8 Test::_byte
    IL_000b:  ldarg.0
    IL_000c:  ldc.i4.s   111
    IL_000e:  stfld      int8 Test::_sbyte
    IL_0013:  ldarg.0
    IL_0014:  ldc.i4     0x3039
    IL_0019:  stfld      int16 Test::_short
    IL_001e:  ldarg.0
    IL_001f:  ldc.i4     0xc822
    IL_0024:  stfld      unsigned int16 Test::_ushort
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4     0x2134566
    IL_002f:  stfld      int32 Test::_int
    IL_0034:  ldarg.0
    IL_0035:  ldc.i4     0x2134566
    IL_003a:  stfld      unsigned int32 Test::_uint
    IL_003f:  ldarg.0
    IL_0040:  ldc.i4     0x2134566
    IL_0045:  conv.i8
    IL_0046:  stfld      int64 Test::_long
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4     0x2134566
    IL_0051:  conv.i8
    IL_0052:  stfld      unsigned int64 Test::_ulong
    IL_0057:  ldarg.0
    IL_0058:  ldc.r4     70.000999
    IL_005d:  stfld      float32 Test::_float
    IL_0062:  ldarg.0
    IL_0063:  ldc.r8     80.001999999999995
    IL_006c:  stfld      float64 Test::_double
    IL_0071:  ret
  } // end of method Test::InitStruct

  .method private hidebysig static void  VerifyStruct(valuetype Test& test) cil managed
  {
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldfld      unsigned int8 Test::_byte
    IL_0006:  ldc.i4     0xde
    IL_000b:  bne.un.s   IL_0087

    IL_000d:  ldarg.0
    IL_000e:  ldfld      int8 Test::_sbyte
    IL_0013:  conv.i4
    IL_0014:  ldc.i4.s   111
    IL_0016:  bne.un.s   IL_0087

    IL_0018:  ldarg.0
    IL_0019:  ldfld      int16 Test::_short
    IL_001e:  ldc.i4     0x3039
    IL_0023:  bne.un.s   IL_0087

    IL_0025:  ldarg.0
    IL_0026:  ldfld      unsigned int16 Test::_ushort
    IL_002b:  conv.i4
    IL_002c:  ldc.i4     0xc822
    IL_0031:  bne.un.s   IL_0087

    IL_0033:  ldarg.0
    IL_0034:  ldfld      int32 Test::_int
    IL_0039:  ldc.i4     0x2134566
    IL_003e:  bne.un.s   IL_0087

    IL_0040:  ldarg.0
    IL_0041:  ldfld      unsigned int32 Test::_uint
    IL_0046:  ldc.i4     0x2134566
    IL_004b:  bne.un.s   IL_0087

    IL_004d:  ldarg.0
    IL_004e:  ldfld      int64 Test::_long
    IL_0053:  ldc.i4     0x2134566
    IL_0058:  conv.i8
    IL_0059:  bne.un.s   IL_0087

    IL_005b:  ldarg.0
    IL_005c:  ldfld      unsigned int64 Test::_ulong
    IL_0061:  ldc.i4     0x2134566
    IL_0066:  conv.i8
    IL_0067:  bne.un.s   IL_0087

    IL_0069:  ldarg.0
    IL_006a:  ldfld      float32 Test::_float
    IL_006f:  ldc.r4     70.000999
    IL_0074:  bne.un.s   IL_0087

    IL_0076:  ldarg.0
    IL_0077:  ldfld      float64 Test::_double
    IL_007c:  ldc.r8     80.001999999999995
    IL_0085:  beq.s      IL_008d

    IL_0087:  newobj     instance void [mscorlib]System.ApplicationException::.ctor()
    IL_008c:  throw

    IL_008d:  ret
  } // end of method Test::VerifyStruct

  .method private hidebysig static int32 Main() cil managed
  {
    .entrypoint
    .maxstack  1
    .locals (int32 V_0)
    IL_000a:  ldsflda    valuetype Test Test::static_test
    IL_000f:  call       void Test::DumpStruct(valuetype Test&)
    IL_0014:  ldsflda    valuetype Test Test::static_test
    IL_0019:  call       void Test::VerifyStruct(valuetype Test&)
    IL_001e:  ldc.i4.s   100
    IL_0020:  stloc.0
    IL_0021:  br.s       IL_0023

    IL_0023:  ldloc.0
    IL_0024:  ret
  } // end of method Test::Main

} // end of class Test

.data D_1 = bytearray( DE 6F 39 30 22 C8 00 00 66 45 13 02 66 45 13 02
66 45 13 02 00 00 00 00 66 45 13 02 00 00 00 00
83 00 8C 42 00 00 00 00 E3 A5 9B C4 20 00 54 40
 )