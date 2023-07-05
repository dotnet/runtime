// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}

.assembly extern mscorlib { }
.assembly simple
{
}
.class public sequential ansi sealed beforefieldinit Test_simple
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
  .field public unsigned int32 _pad
  .field public float64 _double
  .field public static valuetype Test_simple static_test at D_1
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

  .method private hidebysig static void  DumpStruct(valuetype Test_simple& test) cil managed
  {
    .maxstack  2
    .locals (valuetype Test_simple& pinned V_0,
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
    IL_001a:  call       char Test_simple::hex(unsigned int8)
    IL_001f:  call       void [System.Console]System.Console::Write(char)
    IL_0024:  ldloc.1
    IL_0025:  ldloc.2
    IL_0026:  add
    IL_0027:  ldind.u1
    IL_0028:  ldc.i4.s   15
    IL_002a:  and
    IL_002b:  conv.u1
    IL_002c:  call       char Test_simple::hex(unsigned int8)
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
    IL_0052:  sizeof     Test_simple
    IL_0058:  blt.s      IL_0013

    IL_005a:  ldstr      " )"
    IL_005f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0064:  ldc.i4.0
    IL_0065:  conv.u
    IL_0066:  stloc.0
    IL_0067:  ret
  } // end of method Test::DumpStruct

  .method private hidebysig static void  InitStruct(valuetype Test_simple& test) cil managed
  {
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4     0xde
    IL_0006:  stfld      unsigned int8 Test_simple::_byte
    IL_000b:  ldarg.0
    IL_000c:  ldc.i4.s   111
    IL_000e:  stfld      int8 Test_simple::_sbyte
    IL_0013:  ldarg.0
    IL_0014:  ldc.i4     0x3039
    IL_0019:  stfld      int16 Test_simple::_short
    IL_001e:  ldarg.0
    IL_001f:  ldc.i4     0xc822
    IL_0024:  stfld      unsigned int16 Test_simple::_ushort
    IL_0029:  ldarg.0
    IL_002a:  ldc.i4     0x2134566
    IL_002f:  stfld      int32 Test_simple::_int
    IL_0034:  ldarg.0
    IL_0035:  ldc.i4     0x2134566
    IL_003a:  stfld      unsigned int32 Test_simple::_uint
    IL_003f:  ldarg.0
    IL_0040:  ldc.i4     0x2134566
    IL_0045:  conv.i8
    IL_0046:  stfld      int64 Test_simple::_long
    IL_004b:  ldarg.0
    IL_004c:  ldc.i4     0x2134566
    IL_0051:  conv.i8
    IL_0052:  stfld      unsigned int64 Test_simple::_ulong
    IL_0057:  ldarg.0
    IL_0058:  ldc.r4     70.000999
    IL_005d:  stfld      float32 Test_simple::_float
    IL_0062:  ldarg.0
    IL_0063:  ldc.r8     80.001999999999995
    IL_006c:  stfld      float64 Test_simple::_double
    IL_0071:  ret
  } // end of method Test::InitStruct

  .method private hidebysig static void  VerifyStruct(valuetype Test_simple& test) cil managed
  {
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldfld      unsigned int8 Test_simple::_byte
    IL_0006:  ldc.i4     0xde
    IL_000b:  bne.un.s   IL_0087

    IL_000d:  ldarg.0
    IL_000e:  ldfld      int8 Test_simple::_sbyte
    IL_0013:  conv.i4
    IL_0014:  ldc.i4.s   111
    IL_0016:  bne.un.s   IL_0087

    IL_0018:  ldarg.0
    IL_0019:  ldfld      int16 Test_simple::_short
    IL_001e:  ldc.i4     0x3039
    IL_0023:  bne.un.s   IL_0087

    IL_0025:  ldarg.0
    IL_0026:  ldfld      unsigned int16 Test_simple::_ushort
    IL_002b:  conv.i4
    IL_002c:  ldc.i4     0xc822
    IL_0031:  bne.un.s   IL_0087

    IL_0033:  ldarg.0
    IL_0034:  ldfld      int32 Test_simple::_int
    IL_0039:  ldc.i4     0x2134566
    IL_003e:  bne.un.s   IL_0087

    IL_0040:  ldarg.0
    IL_0041:  ldfld      unsigned int32 Test_simple::_uint
    IL_0046:  ldc.i4     0x2134566
    IL_004b:  bne.un.s   IL_0087

    IL_004d:  ldarg.0
    IL_004e:  ldfld      int64 Test_simple::_long
    IL_0053:  ldc.i4     0x2134566
    IL_0058:  conv.i8
    IL_0059:  bne.un.s   IL_0087

    IL_005b:  ldarg.0
    IL_005c:  ldfld      unsigned int64 Test_simple::_ulong
    IL_0061:  ldc.i4     0x2134566
    IL_0066:  conv.i8
    IL_0067:  bne.un.s   IL_0087

    IL_0069:  ldarg.0
    IL_006a:  ldfld      float32 Test_simple::_float
    IL_006f:  ldc.r4     70.000999
    IL_0074:  bne.un.s   IL_0087

    IL_0076:  ldarg.0
    IL_0077:  ldfld      float64 Test_simple::_double
    IL_007c:  ldc.r8     80.001999999999995
    IL_0085:  beq.s      IL_008d

    IL_0087:  newobj     instance void [mscorlib]System.Exception::.ctor()
    IL_008c:  throw

    IL_008d:  ret
  } // end of method Test::VerifyStruct

  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  1
    .locals (int32 V_0)
    IL_000a:  ldsflda    valuetype Test_simple Test_simple::static_test
    IL_000f:  call       void Test_simple::DumpStruct(valuetype Test_simple&)
    IL_0014:  ldsflda    valuetype Test_simple Test_simple::static_test
    IL_0019:  call       void Test_simple::VerifyStruct(valuetype Test_simple&)
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
