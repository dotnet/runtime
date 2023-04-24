// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib
{
}
.assembly knight
{
}
.assembly extern xunit.core {}

.file alignment 512

.namespace KnightMove_knight_il
{
  .class private sequential ansi sealed beforefieldinit MV
         extends [mscorlib]System.ValueType
  {
    .field public int32 x
    .field public int32 y
    .field public int32 v
  } // end of class MV

  .class public sequential ansi sealed beforefieldinit SQ
         extends [mscorlib]System.ValueType
  {
    .field public int32 visited
    .method public hidebysig static int32
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  4
      .locals (valuetype KnightMove_knight_il.SQ[0...,0...] V_0,
               int32[] V_1,
               int32[] V_2,
               valuetype KnightMove_knight_il.MV[] V_3,
               int32 V_4,
               int32 V_5,
               int32 V_6,
               int32 V_7,
               int32 V_8,
               string V_9,
               int32 V_10)
      IL_0000:  ldc.i4.5
      IL_0001:  ldc.i4.5
      IL_0002:  newobj     instance void valuetype KnightMove_knight_il.SQ[0...,0...]::.ctor(int32,
                                                                                   int32)
      IL_0007:  stloc.0
      IL_0008:  ldc.i4.8
      IL_0009:  newarr     [mscorlib]System.Int32
      IL_000e:  dup
      IL_000f:  ldtoken    field valuetype '<PrivateImplementationDetails>'/'$$struct0x6000001-1' '<PrivateImplementationDetails>'::'$$method0x6000001-1'
      IL_0014:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                          valuetype [mscorlib]System.RuntimeFieldHandle)
      IL_0019:  stloc.1
      IL_001a:  ldc.i4.8
      IL_001b:  newarr     [mscorlib]System.Int32
      IL_0020:  dup
      IL_0021:  ldtoken    field valuetype '<PrivateImplementationDetails>'/'$$struct0x6000001-2' '<PrivateImplementationDetails>'::'$$method0x6000001-2'
      IL_0026:  call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array,
                                                                                                          valuetype [mscorlib]System.RuntimeFieldHandle)
      IL_002b:  stloc.2
      IL_002c:  ldc.i4.s   25
      IL_002e:  newarr     KnightMove_knight_il.MV
      IL_0033:  stloc.3
      IL_0034:  ldloc.0
      IL_0035:  ldc.i4.0
      IL_0036:  ldc.i4.0
      IL_0037:  call       instance valuetype KnightMove_knight_il.SQ& valuetype KnightMove_knight_il.SQ[0...,0...]::Address(int32,
                                                                                                         int32)
      IL_003c:  ldc.i4.1
      IL_003d:  stfld      int32 KnightMove_knight_il.SQ::visited
      IL_0042:  ldloc.3
      IL_0043:  ldc.i4.0
      IL_0044:  ldelema    KnightMove_knight_il.MV
      IL_0049:  ldc.i4.0
      IL_004a:  stfld      int32 KnightMove_knight_il.MV::x
      IL_004f:  ldloc.3
      IL_0050:  ldc.i4.0
      IL_0051:  ldelema    KnightMove_knight_il.MV
      IL_0056:  ldc.i4.0
      IL_0057:  stfld      int32 KnightMove_knight_il.MV::y
      IL_005c:  ldc.i4.1
      IL_005d:  stloc.s    V_4
      IL_005f:  br         IL_023e

      IL_0064:  ldloc.3
      IL_0065:  ldloc.s    V_4
      IL_0067:  ldelema    KnightMove_knight_il.MV
      IL_006c:  ldfld      int32 KnightMove_knight_il.MV::v
      IL_0071:  ldc.i4.8
      IL_0072:  blt        IL_0104

      IL_0077:  ldloc.s    V_4
      IL_0079:  ldc.i4.1
      IL_007a:  sub
      IL_007b:  stloc.s    V_4
      IL_007d:  ldloc.s    V_4
      IL_007f:  brtrue.s   IL_0093

      IL_0081:  ldstr      "There is no solution."
      IL_0086:  call       void [System.Console]System.Console::WriteLine(string)
      IL_008b:  ldc.i4.1
      IL_008c:  stloc.s    V_10
      IL_008e:  br         IL_0243

      IL_0093:  ldloc.0
      IL_0094:  ldloc.3
      IL_0095:  ldloc.s    V_4
      IL_0097:  ldelema    KnightMove_knight_il.MV
      IL_009c:  ldfld      int32 KnightMove_knight_il.MV::x
      IL_00a1:  ldloc.3
      IL_00a2:  ldloc.s    V_4
      IL_00a4:  ldelema    KnightMove_knight_il.MV
      IL_00a9:  ldfld      int32 KnightMove_knight_il.MV::y
      IL_00ae:  call       instance valuetype KnightMove_knight_il.SQ& valuetype KnightMove_knight_il.SQ[0...,0...]::Address(int32,
                                                                                                         int32)
      IL_00b3:  ldfld      int32 KnightMove_knight_il.SQ::visited
      IL_00b8:  ldloc.s    V_4
      IL_00ba:  ldc.i4.1
      IL_00bb:  add
      IL_00bc:  beq.s      IL_00c4

      IL_00be:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_00c3:  throw

      IL_00c4:  ldloc.0
      IL_00c5:  ldloc.3
      IL_00c6:  ldloc.s    V_4
      IL_00c8:  ldelema    KnightMove_knight_il.MV
      IL_00cd:  ldfld      int32 KnightMove_knight_il.MV::x
      IL_00d2:  ldloc.3
      IL_00d3:  ldloc.s    V_4
      IL_00d5:  ldelema    KnightMove_knight_il.MV
      IL_00da:  ldfld      int32 KnightMove_knight_il.MV::y
      IL_00df:  call       instance valuetype KnightMove_knight_il.SQ& valuetype KnightMove_knight_il.SQ[0...,0...]::Address(int32,
                                                                                                         int32)
      IL_00e4:  ldc.i4.0
      IL_00e5:  stfld      int32 KnightMove_knight_il.SQ::visited
      IL_00ea:  ldloc.3
      IL_00eb:  ldloc.s    V_4
      IL_00ed:  ldelema    KnightMove_knight_il.MV
      IL_00f2:  dup
      IL_00f3:  ldfld      int32 KnightMove_knight_il.MV::v
      IL_00f8:  ldc.i4.1
      IL_00f9:  add
      IL_00fa:  stfld      int32 KnightMove_knight_il.MV::v
      IL_00ff:  br         IL_023e

      IL_0104:  ldloc.3
      IL_0105:  ldloc.s    V_4
      IL_0107:  ldc.i4.1
      IL_0108:  sub
      IL_0109:  ldelema    KnightMove_knight_il.MV
      IL_010e:  ldfld      int32 KnightMove_knight_il.MV::x
      IL_0113:  ldloc.1
      IL_0114:  ldloc.3
      IL_0115:  ldloc.s    V_4
      IL_0117:  ldelema    KnightMove_knight_il.MV
      IL_011c:  ldfld      int32 KnightMove_knight_il.MV::v
      IL_0121:  ldelem.i4
      IL_0122:  add
      IL_0123:  stloc.s    V_5
      IL_0125:  ldloc.3
      IL_0126:  ldloc.s    V_4
      IL_0128:  ldc.i4.1
      IL_0129:  sub
      IL_012a:  ldelema    KnightMove_knight_il.MV
      IL_012f:  ldfld      int32 KnightMove_knight_il.MV::y
      IL_0134:  ldloc.2
      IL_0135:  ldloc.3
      IL_0136:  ldloc.s    V_4
      IL_0138:  ldelema    KnightMove_knight_il.MV
      IL_013d:  ldfld      int32 KnightMove_knight_il.MV::v
      IL_0142:  ldelem.i4
      IL_0143:  add
      IL_0144:  stloc.s    V_6
      IL_0146:  ldloc.s    V_5
      IL_0148:  ldc.i4.0
      IL_0149:  blt.s      IL_016b

      IL_014b:  ldloc.s    V_5
      IL_014d:  ldc.i4.5
      IL_014e:  bge.s      IL_016b

      IL_0150:  ldloc.s    V_6
      IL_0152:  ldc.i4.0
      IL_0153:  blt.s      IL_016b

      IL_0155:  ldloc.s    V_6
      IL_0157:  ldc.i4.5
      IL_0158:  bge.s      IL_016b

      IL_015a:  ldloc.0
      IL_015b:  ldloc.s    V_5
      IL_015d:  ldloc.s    V_6
      IL_015f:  call       instance valuetype KnightMove_knight_il.SQ& valuetype KnightMove_knight_il.SQ[0...,0...]::Address(int32,
                                                                                                         int32)
      IL_0164:  ldfld      int32 KnightMove_knight_il.SQ::visited
      IL_0169:  brfalse.s  IL_0185

      IL_016b:  ldloc.3
      IL_016c:  ldloc.s    V_4
      IL_016e:  ldelema    KnightMove_knight_il.MV
      IL_0173:  dup
      IL_0174:  ldfld      int32 KnightMove_knight_il.MV::v
      IL_0179:  ldc.i4.1
      IL_017a:  add
      IL_017b:  stfld      int32 KnightMove_knight_il.MV::v
      IL_0180:  br         IL_023e

      IL_0185:  ldloc.3
      IL_0186:  ldloc.s    V_4
      IL_0188:  ldelema    KnightMove_knight_il.MV
      IL_018d:  ldloc.s    V_5
      IL_018f:  stfld      int32 KnightMove_knight_il.MV::x
      IL_0194:  ldloc.3
      IL_0195:  ldloc.s    V_4
      IL_0197:  dup
      IL_0198:  ldc.i4.1
      IL_0199:  add
      IL_019a:  stloc.s    V_4
      IL_019c:  ldelema    KnightMove_knight_il.MV
      IL_01a1:  ldloc.s    V_6
      IL_01a3:  stfld      int32 KnightMove_knight_il.MV::y
      IL_01a8:  ldloc.0
      IL_01a9:  ldloc.s    V_5
      IL_01ab:  ldloc.s    V_6
      IL_01ad:  call       instance valuetype KnightMove_knight_il.SQ& valuetype KnightMove_knight_il.SQ[0...,0...]::Address(int32,
                                                                                                         int32)
      IL_01b2:  ldloc.s    V_4
      IL_01b4:  stfld      int32 KnightMove_knight_il.SQ::visited
      IL_01b9:  ldloc.s    V_4
      IL_01bb:  ldc.i4.s   25
      IL_01bd:  bne.un.s   IL_0230

      IL_01bf:  ldstr      "Solution found."
      IL_01c4:  call       void [System.Console]System.Console::WriteLine(string)
      IL_01c9:  ldc.i4.0
      IL_01ca:  stloc.s    V_7
      IL_01cc:  br.s       IL_0225

      IL_01ce:  ldc.i4.0
      IL_01cf:  stloc.s    V_8
      IL_01d1:  br.s       IL_0215

      IL_01d3:  ldloc.0
      IL_01d4:  ldloc.s    V_7
      IL_01d6:  ldloc.s    V_8
      IL_01d8:  call       instance valuetype KnightMove_knight_il.SQ& valuetype KnightMove_knight_il.SQ[0...,0...]::Address(int32,
                                                                                                         int32)
      IL_01dd:  ldflda     int32 KnightMove_knight_il.SQ::visited
      IL_01e2:  call       instance string [mscorlib]System.Int32::ToString()
      IL_01e7:  stloc.s    V_9
      IL_01e9:  ldloc.s    V_9
      IL_01eb:  ldloc.s    V_9
      IL_01ed:  callvirt   instance int32 [mscorlib]System.String::get_Length()
      IL_01f2:  ldc.i4.1
      IL_01f3:  beq.s      IL_01fc

      IL_01f5:  ldstr      " "
      IL_01fa:  br.s       IL_0201

      IL_01fc:  ldstr      "  "
      IL_0201:  call       string [mscorlib]System.String::Concat(string,
                                                                  string)
      IL_0206:  stloc.s    V_9
      IL_0208:  ldloc.s    V_9
      IL_020a:  call       void [System.Console]System.Console::Write(string)
      IL_020f:  ldloc.s    V_8
      IL_0211:  ldc.i4.1
      IL_0212:  add
      IL_0213:  stloc.s    V_8
      IL_0215:  ldloc.s    V_8
      IL_0217:  ldc.i4.5
      IL_0218:  blt.s      IL_01d3

      IL_021a:  call       void [System.Console]System.Console::WriteLine()
      IL_021f:  ldloc.s    V_7
      IL_0221:  ldc.i4.1
      IL_0222:  add
      IL_0223:  stloc.s    V_7
      IL_0225:  ldloc.s    V_7
      IL_0227:  ldc.i4.5
      IL_0228:  blt.s      IL_01ce

      IL_022a:  ldc.i4.s   100
      IL_022c:  stloc.s    V_10
      IL_022e:  br.s       IL_0243

      IL_0230:  ldloc.3
      IL_0231:  ldloc.s    V_4
      IL_0233:  ldelema    KnightMove_knight_il.MV
      IL_0238:  ldc.i4.0
      IL_0239:  stfld      int32 KnightMove_knight_il.MV::v
      IL_023e:  br         IL_0064

      IL_0243:  ldloc.s    V_10
      IL_0245:  ret
    } // end of method SQ::Main

  } // end of class SQ

} // end of namespace KnightMove_knight_il

.class private auto ansi '<PrivateImplementationDetails>'
       extends [mscorlib]System.Object
{
  .class explicit ansi sealed nested private '$$struct0x6000001-1'
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 32
  } // end of class '$$struct0x6000001-1'

  .class explicit ansi sealed nested private '$$struct0x6000001-2'
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 32
  } // end of class '$$struct0x6000001-2'

  .field static assembly valuetype '<PrivateImplementationDetails>'/'$$struct0x6000001-1' '$$method0x6000001-1' at D_00002050
  .field static assembly valuetype '<PrivateImplementationDetails>'/'$$struct0x6000001-2' '$$method0x6000001-2' at D_00002070
} // end of class '<PrivateImplementationDetails>'

.data D_00002050 = bytearray (
                 01 00 00 00 01 00 00 00 FF FF FF FF FF FF FF FF
                 02 00 00 00 02 00 00 00 FE FF FF FF FE FF FF FF) 
.data D_00002070 = bytearray (
                 02 00 00 00 FE FF FF FF 02 00 00 00 FE FF FF FF
                 01 00 00 00 FF FF FF FF 01 00 00 00 FF FF FF FF) 
