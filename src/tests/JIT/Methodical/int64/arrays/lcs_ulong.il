// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.







.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'lcs_ulong'// as "lcs_ulong"
{
}
.assembly extern xunit.core {}
// MVID: {4FECC02C-D871-4196-A531-897C5458F477}
.namespace JitTest_lcs_ulong_arrays_il
{
  .class public auto ansi LCS
         extends ['mscorlib']System.Object
  {
    .field private static literal int32 RANK = int32(0x00000004)
    .method private hidebysig static class System.String 
            buildLCS(unsigned int64[0...,0...,0...,0...] b,
                     wchar[] X,
                     unsigned int64[] ind) il managed
    {
      // Code size       179 (0xb3)
      .maxstack  6
      .locals (unsigned int64 V_0,
               unsigned int64 V_1,
               unsigned int64 V_2,
               unsigned int64 V_3,
               class System.String V_4,
               unsigned int64[] V_5,
               int32 V_6)
      IL_0000:  ldc.i4.0
      IL_0001:  conv.i8
      IL_0002:  stloc.0
      IL_0003:  br.s       IL_001e

      IL_0005:  ldarg.2
      IL_0006:  ldloc.0
      IL_0007:  conv.i4
      IL_0008:  ldelem.i8
      IL_0009:  ldc.i4.0
      IL_000a:  conv.i8
      IL_000b:  bne.un.s   IL_0019

      IL_000d:  ldstr      ""
      IL_0012:  stloc.s    V_4
      IL_0014:  br         IL_00b0

      IL_0019:  ldloc.0
      IL_001a:  ldc.i4.1
      IL_001b:  conv.i8
      IL_001c:  add
      IL_001d:  stloc.0
      IL_001e:  ldloc.0
      IL_001f:  ldc.i4.4
      IL_0020:  conv.i8
      IL_0021:  blt.un.s   IL_0005

      IL_0023:  ldarg.0
      IL_0024:  ldarg.2
      IL_0025:  ldc.i4.0
      IL_0026:  ldelem.i8
      IL_0027:  conv.i4
      IL_0028:  ldarg.2
      IL_0029:  ldc.i4.1
      IL_002a:  ldelem.i8
      IL_002b:  conv.i4
      IL_002c:  ldarg.2
      IL_002d:  ldc.i4.2
      IL_002e:  ldelem.i8
      IL_002f:  conv.i4
      IL_0030:  ldarg.2
      IL_0031:  ldc.i4.3
      IL_0032:  ldelem.i8
      IL_0033:  conv.i4
      IL_0034:  call       instance unsigned int64 unsigned int64[0...,0...,0...,0...]::Get(int32,
                                                                                            int32,
                                                                                            int32,
                                                                                            int32)
      IL_0039:  stloc.1
      IL_003a:  ldloc.1
      IL_003b:  ldc.i4.4
      IL_003c:  conv.i8
      IL_003d:  bne.un.s   IL_0082

      IL_003f:  ldc.i4.0
      IL_0040:  conv.i8
      IL_0041:  stloc.2
      IL_0042:  br.s       IL_005b

      IL_0044:  ldarg.2
      IL_0045:  dup
      IL_0046:  stloc.s    V_5
      IL_0048:  ldloc.2
      IL_0049:  conv.i4
      IL_004a:  dup
      IL_004b:  stloc.s    V_6
      IL_004d:  ldloc.s    V_5
      IL_004f:  ldloc.s    V_6
      IL_0051:  ldelem.i8
      IL_0052:  ldc.i4.1
      IL_0053:  conv.i8
      IL_0054:  sub
      IL_0055:  stelem.i8
      IL_0056:  ldloc.2
      IL_0057:  ldc.i4.1
      IL_0058:  conv.i8
      IL_0059:  add
      IL_005a:  stloc.2
      IL_005b:  ldloc.2
      IL_005c:  ldc.i4.4
      IL_005d:  conv.i8
      IL_005e:  blt.un.s   IL_0044

      IL_0060:  ldarg.2
      IL_0061:  ldc.i4.0
      IL_0062:  ldelem.i8
      IL_0063:  stloc.3
      IL_0064:  ldarg.0
      IL_0065:  ldarg.1
      IL_0066:  ldarg.2
      IL_0067:  call       class System.String JitTest_lcs_ulong_arrays_il.LCS::buildLCS(unsigned int64[0...,0...,0...,0...],
                                                                     wchar[],
                                                                     unsigned int64[])
      IL_006c:  ldarg.1
      IL_006d:  ldloc.3
      IL_006e:  conv.i4
      IL_006f:  ldelem.u2
      IL_0074:  box     ['mscorlib']System.Char
      IL_0079:  call       class System.String ['mscorlib']System.String::Concat(class System.Object,
                                                                                 class System.Object)
      IL_007e:  stloc.s    V_4
      IL_0080:  br.s       IL_00b0

      IL_0082:  ldloc.1
      IL_0083:  ldc.i4.0
      IL_0084:  conv.i8
      IL_0085:  blt.un.s   IL_00aa

      IL_0087:  ldloc.1
      IL_0088:  ldc.i4.4
      IL_0089:  conv.i8
      IL_008a:  bge.un.s   IL_00aa

      IL_008c:  ldarg.2
      IL_008d:  dup
      IL_008e:  stloc.s    V_5
      IL_0090:  ldloc.1
      IL_0091:  conv.i4
      IL_0092:  dup
      IL_0093:  stloc.s    V_6
      IL_0095:  ldloc.s    V_5
      IL_0097:  ldloc.s    V_6
      IL_0099:  ldelem.i8
      IL_009a:  ldc.i4.1
      IL_009b:  conv.i8
      IL_009c:  sub
      IL_009d:  stelem.i8
      IL_009e:  ldarg.0
      IL_009f:  ldarg.1
      IL_00a0:  ldarg.2
      IL_00a1:  call       class System.String JitTest_lcs_ulong_arrays_il.LCS::buildLCS(unsigned int64[0...,0...,0...,0...],
                                                                     wchar[],
                                                                     unsigned int64[])
      IL_00a6:  stloc.s    V_4
      IL_00a8:  br.s       IL_00b0

      IL_00aa:  newobj     instance void ['mscorlib']System.Exception::.ctor()
      IL_00af:  throw

      IL_00b0:  ldloc.s    V_4
      IL_00b2:  ret
    } // end of method 'LCS::buildLCS'

    .method private hidebysig static void 
            findLCS(unsigned int64[0...,0...,0...,0...] c,
                    unsigned int64[0...,0...,0...,0...] b,
                    wchar[][] seq,
                    unsigned int64[] len) il managed
    {
      // Code size       486 (0x1e6)
      .maxstack  11
      .locals (unsigned int64[] V_0,
               bool V_1,
               unsigned int64 V_2,
               unsigned int64 V_3,
               unsigned int64 V_4,
               unsigned int64 V_5,
               unsigned int64[] V_6,
               int32 V_7)
      IL_0000:  ldc.i4.4
      IL_0001:  newarr     ['mscorlib']System.UInt64
      IL_0006:  stloc.0
      IL_0007:  ldloc.0
      IL_0008:  ldc.i4.0
      IL_0009:  ldc.i4.1
      IL_000a:  conv.i8
      IL_000b:  stelem.i8
      IL_000c:  br         IL_01da

      IL_0011:  ldloc.0
      IL_0012:  ldc.i4.1
      IL_0013:  ldc.i4.1
      IL_0014:  conv.i8
      IL_0015:  stelem.i8
      IL_0016:  br         IL_01c2

      IL_001b:  ldloc.0
      IL_001c:  ldc.i4.2
      IL_001d:  ldc.i4.1
      IL_001e:  conv.i8
      IL_001f:  stelem.i8
      IL_0020:  br         IL_01aa

      IL_0025:  ldloc.0
      IL_0026:  ldc.i4.3
      IL_0027:  ldc.i4.1
      IL_0028:  conv.i8
      IL_0029:  stelem.i8
      IL_002a:  br         IL_0192

      IL_002f:  ldc.i4.1
      IL_0030:  stloc.1
      IL_0031:  ldc.i4.1
      IL_0032:  conv.i8
      IL_0033:  stloc.2
      IL_0034:  br.s       IL_0061

      IL_0036:  ldarg.2
      IL_0037:  ldloc.2
      IL_0038:  conv.i4
      IL_0039:  ldelem.ref
      IL_003a:  ldloc.0
      IL_003b:  ldloc.2
      IL_003c:  conv.i4
      IL_003d:  ldelem.i8
      IL_003e:  ldc.i4.1
      IL_003f:  conv.i8
      IL_0040:  sub
      IL_0041:  conv.i4
      IL_0042:  ldelem.u2
      IL_0043:  ldarg.2
      IL_0044:  ldloc.2
      IL_0045:  ldc.i4.1
      IL_0046:  conv.i8
      IL_0047:  sub
      IL_0048:  conv.i4
      IL_0049:  ldelem.ref
      IL_004a:  ldloc.0
      IL_004b:  ldloc.2
      IL_004c:  ldc.i4.1
      IL_004d:  conv.i8
      IL_004e:  sub
      IL_004f:  conv.i4
      IL_0050:  ldelem.i8
      IL_0051:  ldc.i4.1
      IL_0052:  conv.i8
      IL_0053:  sub
      IL_0054:  conv.i4
      IL_0055:  ldelem.u2
      IL_0056:  beq.s      IL_005c

      IL_0058:  ldc.i4.0
      IL_0059:  stloc.1
      IL_005a:  br.s       IL_0066

      IL_005c:  ldloc.2
      IL_005d:  ldc.i4.1
      IL_005e:  conv.i8
      IL_005f:  add
      IL_0060:  stloc.2
      IL_0061:  ldloc.2
      IL_0062:  ldc.i4.4
      IL_0063:  conv.i8
      IL_0064:  blt.un.s   IL_0036

      IL_0066:  ldloc.1
      IL_0067:  brfalse.s  IL_00c1

      IL_0069:  ldarg.0
      IL_006a:  ldloc.0
      IL_006b:  ldc.i4.0
      IL_006c:  ldelem.i8
      IL_006d:  conv.i4
      IL_006e:  ldloc.0
      IL_006f:  ldc.i4.1
      IL_0070:  ldelem.i8
      IL_0071:  conv.i4
      IL_0072:  ldloc.0
      IL_0073:  ldc.i4.2
      IL_0074:  ldelem.i8
      IL_0075:  conv.i4
      IL_0076:  ldloc.0
      IL_0077:  ldc.i4.3
      IL_0078:  ldelem.i8
      IL_0079:  conv.i4
      IL_007a:  ldarg.0
      IL_007b:  ldloc.0
      IL_007c:  ldc.i4.0
      IL_007d:  ldelem.i8
      IL_007e:  ldc.i4.1
      IL_007f:  conv.i8
      IL_0080:  sub
      IL_0081:  conv.i4
      IL_0082:  ldloc.0
      IL_0083:  ldc.i4.1
      IL_0084:  ldelem.i8
      IL_0085:  ldc.i4.1
      IL_0086:  conv.i8
      IL_0087:  sub
      IL_0088:  conv.i4
      IL_0089:  ldloc.0
      IL_008a:  ldc.i4.2
      IL_008b:  ldelem.i8
      IL_008c:  ldc.i4.1
      IL_008d:  conv.i8
      IL_008e:  sub
      IL_008f:  conv.i4
      IL_0090:  ldloc.0
      IL_0091:  ldc.i4.3
      IL_0092:  ldelem.i8
      IL_0093:  ldc.i4.1
      IL_0094:  conv.i8
      IL_0095:  sub
      IL_0096:  conv.i4
      IL_0097:  call       instance unsigned int64 unsigned int64[0...,0...,0...,0...]::Get(int32,
                                                                                            int32,
                                                                                            int32,
                                                                                            int32)
      IL_009c:  ldc.i4.1
      IL_009d:  conv.i8
      IL_009e:  add
      IL_009f:  call       instance void unsigned int64[0...,0...,0...,0...]::Set(int32,
                                                                                  int32,
                                                                                  int32,
                                                                                  int32,
                                                                                  unsigned int64)
      IL_00a4:  ldarg.1
      IL_00a5:  ldloc.0
      IL_00a6:  ldc.i4.0
      IL_00a7:  ldelem.i8
      IL_00a8:  conv.i4
      IL_00a9:  ldloc.0
      IL_00aa:  ldc.i4.1
      IL_00ab:  ldelem.i8
      IL_00ac:  conv.i4
      IL_00ad:  ldloc.0
      IL_00ae:  ldc.i4.2
      IL_00af:  ldelem.i8
      IL_00b0:  conv.i4
      IL_00b1:  ldloc.0
      IL_00b2:  ldc.i4.3
      IL_00b3:  ldelem.i8
      IL_00b4:  conv.i4
      IL_00b5:  ldc.i4.4
      IL_00b6:  conv.i8
      IL_00b7:  call       instance void unsigned int64[0...,0...,0...,0...]::Set(int32,
                                                                                  int32,
                                                                                  int32,
                                                                                  int32,
                                                                                  unsigned int64)
      IL_00bc:  br         IL_0185

      IL_00c1:  ldc.i4.0
      IL_00c2:  conv.i8
      IL_00c3:  stloc.3
      IL_00c4:  ldc.i4.0
      IL_00c5:  conv.i8
      IL_00c6:  stloc.s    V_4
      IL_00c8:  ldc.i4.0
      IL_00c9:  conv.i8
      IL_00ca:  stloc.s    V_5
      IL_00cc:  br.s       IL_0139

      IL_00ce:  ldloc.0
      IL_00cf:  dup
      IL_00d0:  stloc.s    V_6
      IL_00d2:  ldloc.s    V_5
      IL_00d4:  conv.i4
      IL_00d5:  dup
      IL_00d6:  stloc.s    V_7
      IL_00d8:  ldloc.s    V_6
      IL_00da:  ldloc.s    V_7
      IL_00dc:  ldelem.i8
      IL_00dd:  ldc.i4.1
      IL_00de:  conv.i8
      IL_00df:  sub
      IL_00e0:  stelem.i8
      IL_00e1:  ldarg.0
      IL_00e2:  ldloc.0
      IL_00e3:  ldc.i4.0
      IL_00e4:  ldelem.i8
      IL_00e5:  conv.i4
      IL_00e6:  ldloc.0
      IL_00e7:  ldc.i4.1
      IL_00e8:  ldelem.i8
      IL_00e9:  conv.i4
      IL_00ea:  ldloc.0
      IL_00eb:  ldc.i4.2
      IL_00ec:  ldelem.i8
      IL_00ed:  conv.i4
      IL_00ee:  ldloc.0
      IL_00ef:  ldc.i4.3
      IL_00f0:  ldelem.i8
      IL_00f1:  conv.i4
      IL_00f2:  call       instance unsigned int64 unsigned int64[0...,0...,0...,0...]::Get(int32,
                                                                                            int32,
                                                                                            int32,
                                                                                            int32)
      IL_00f7:  ldc.i4.1
      IL_00f8:  conv.i8
      IL_00f9:  add
      IL_00fa:  ldloc.s    V_4
      IL_00fc:  ble.un.s   IL_011f

      IL_00fe:  ldloc.s    V_5
      IL_0100:  ldc.i4.1
      IL_0101:  conv.i8
      IL_0102:  add
      IL_0103:  stloc.3
      IL_0104:  ldarg.0
      IL_0105:  ldloc.0
      IL_0106:  ldc.i4.0
      IL_0107:  ldelem.i8
      IL_0108:  conv.i4
      IL_0109:  ldloc.0
      IL_010a:  ldc.i4.1
      IL_010b:  ldelem.i8
      IL_010c:  conv.i4
      IL_010d:  ldloc.0
      IL_010e:  ldc.i4.2
      IL_010f:  ldelem.i8
      IL_0110:  conv.i4
      IL_0111:  ldloc.0
      IL_0112:  ldc.i4.3
      IL_0113:  ldelem.i8
      IL_0114:  conv.i4
      IL_0115:  call       instance unsigned int64 unsigned int64[0...,0...,0...,0...]::Get(int32,
                                                                                            int32,
                                                                                            int32,
                                                                                            int32)
      IL_011a:  ldc.i4.1
      IL_011b:  conv.i8
      IL_011c:  add
      IL_011d:  stloc.s    V_4
      IL_011f:  ldloc.0
      IL_0120:  dup
      IL_0121:  stloc.s    V_6
      IL_0123:  ldloc.s    V_5
      IL_0125:  conv.i4
      IL_0126:  dup
      IL_0127:  stloc.s    V_7
      IL_0129:  ldloc.s    V_6
      IL_012b:  ldloc.s    V_7
      IL_012d:  ldelem.i8
      IL_012e:  ldc.i4.1
      IL_012f:  conv.i8
      IL_0130:  add
      IL_0131:  stelem.i8
      IL_0132:  ldloc.s    V_5
      IL_0134:  ldc.i4.1
      IL_0135:  conv.i8
      IL_0136:  add
      IL_0137:  stloc.s    V_5
      IL_0139:  ldloc.s    V_5
      IL_013b:  ldc.i4.4
      IL_013c:  conv.i8
      IL_013d:  blt.un.s   IL_00ce

      IL_013f:  ldloc.3
      IL_0140:  ldc.i4.0
      IL_0141:  conv.i8
      IL_0142:  beq.s      IL_014a

      IL_0144:  ldloc.s    V_4
      IL_0146:  ldc.i4.0
      IL_0147:  conv.i8
      IL_0148:  bne.un.s   IL_0150

      IL_014a:  newobj     instance void ['mscorlib']System.Exception::.ctor()
      IL_014f:  throw

      IL_0150:  ldarg.0
      IL_0151:  ldloc.0
      IL_0152:  ldc.i4.0
      IL_0153:  ldelem.i8
      IL_0154:  conv.i4
      IL_0155:  ldloc.0
      IL_0156:  ldc.i4.1
      IL_0157:  ldelem.i8
      IL_0158:  conv.i4
      IL_0159:  ldloc.0
      IL_015a:  ldc.i4.2
      IL_015b:  ldelem.i8
      IL_015c:  conv.i4
      IL_015d:  ldloc.0
      IL_015e:  ldc.i4.3
      IL_015f:  ldelem.i8
      IL_0160:  conv.i4
      IL_0161:  ldloc.s    V_4
      IL_0163:  ldc.i4.1
      IL_0164:  conv.i8
      IL_0165:  sub.ovf.un
      IL_0166:  call       instance void unsigned int64[0...,0...,0...,0...]::Set(int32,
                                                                                  int32,
                                                                                  int32,
                                                                                  int32,
                                                                                  unsigned int64)
      IL_016b:  ldarg.1
      IL_016c:  ldloc.0
      IL_016d:  ldc.i4.0
      IL_016e:  ldelem.i8
      IL_016f:  conv.i4
      IL_0170:  ldloc.0
      IL_0171:  ldc.i4.1
      IL_0172:  ldelem.i8
      IL_0173:  conv.i4
      IL_0174:  ldloc.0
      IL_0175:  ldc.i4.2
      IL_0176:  ldelem.i8
      IL_0177:  conv.i4
      IL_0178:  ldloc.0
      IL_0179:  ldc.i4.3
      IL_017a:  ldelem.i8
      IL_017b:  conv.i4
      IL_017c:  ldloc.3
      IL_017d:  ldc.i4.1
      IL_017e:  conv.i8
      IL_017f:  sub.ovf.un
      IL_0180:  call       instance void unsigned int64[0...,0...,0...,0...]::Set(int32,
                                                                                  int32,
                                                                                  int32,
                                                                                  int32,
                                                                                  unsigned int64)
      IL_0185:  ldloc.0
      IL_0186:  dup
      IL_0187:  stloc.s    V_6
      IL_0189:  ldc.i4.3
      IL_018a:  ldloc.s    V_6
      IL_018c:  ldc.i4.3
      IL_018d:  ldelem.i8
      IL_018e:  ldc.i4.1
      IL_018f:  conv.i8
      IL_0190:  add
      IL_0191:  stelem.i8
      IL_0192:  ldloc.0
      IL_0193:  ldc.i4.3
      IL_0194:  ldelem.i8
      IL_0195:  ldarg.3
      IL_0196:  ldc.i4.3
      IL_0197:  ldelem.i8
      IL_0198:  blt.un     IL_002f

      IL_019d:  ldloc.0
      IL_019e:  dup
      IL_019f:  stloc.s    V_6
      IL_01a1:  ldc.i4.2
      IL_01a2:  ldloc.s    V_6
      IL_01a4:  ldc.i4.2
      IL_01a5:  ldelem.i8
      IL_01a6:  ldc.i4.1
      IL_01a7:  conv.i8
      IL_01a8:  add
      IL_01a9:  stelem.i8
      IL_01aa:  ldloc.0
      IL_01ab:  ldc.i4.2
      IL_01ac:  ldelem.i8
      IL_01ad:  ldarg.3
      IL_01ae:  ldc.i4.2
      IL_01af:  ldelem.i8
      IL_01b0:  blt.un     IL_0025

      IL_01b5:  ldloc.0
      IL_01b6:  dup
      IL_01b7:  stloc.s    V_6
      IL_01b9:  ldc.i4.1
      IL_01ba:  ldloc.s    V_6
      IL_01bc:  ldc.i4.1
      IL_01bd:  ldelem.i8
      IL_01be:  ldc.i4.1
      IL_01bf:  conv.i8
      IL_01c0:  add
      IL_01c1:  stelem.i8
      IL_01c2:  ldloc.0
      IL_01c3:  ldc.i4.1
      IL_01c4:  ldelem.i8
      IL_01c5:  ldarg.3
      IL_01c6:  ldc.i4.1
      IL_01c7:  ldelem.i8
      IL_01c8:  blt.un     IL_001b

      IL_01cd:  ldloc.0
      IL_01ce:  dup
      IL_01cf:  stloc.s    V_6
      IL_01d1:  ldc.i4.0
      IL_01d2:  ldloc.s    V_6
      IL_01d4:  ldc.i4.0
      IL_01d5:  ldelem.i8
      IL_01d6:  ldc.i4.1
      IL_01d7:  conv.i8
      IL_01d8:  add
      IL_01d9:  stelem.i8
      IL_01da:  ldloc.0
      IL_01db:  ldc.i4.0
      IL_01dc:  ldelem.i8
      IL_01dd:  ldarg.3
      IL_01de:  ldc.i4.0
      IL_01df:  ldelem.i8
      IL_01e0:  blt.un     IL_0011

      IL_01e5:  ret
    } // end of method 'LCS::findLCS'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       266 (0x10a)
      .maxstack  5
      .locals (class System.String[] V_0,
               unsigned int64[] V_1,
               wchar[][] V_2,
               unsigned int64 V_3,
               unsigned int64[0...,0...,0...,0...] V_4,
               unsigned int64[0...,0...,0...,0...] V_5,
               unsigned int64 V_6,
               int32 V_7,
               class System.String[] V_8,
               unsigned int64[] V_9,
               int32 V_10)
      IL_0000:  ldstr      "Test searches for ulongest common subsequence of 4"
      + " strings\n\n"
      IL_0005:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_000a:  ldc.i4.4
      IL_000b:  newarr     ['mscorlib']System.String
      IL_0010:  stloc.s    V_8
      IL_0012:  ldloc.s    V_8
      IL_0014:  ldc.i4.0
      IL_0015:  ldstr      "The Sun has left his blackness"
      IL_001a:  stelem.ref
      IL_001b:  ldloc.s    V_8
      IL_001d:  ldc.i4.1
      IL_001e:  ldstr      "and has found a fresher morning"
      IL_0023:  stelem.ref
      IL_0024:  ldloc.s    V_8
      IL_0026:  ldc.i4.2
      IL_0027:  ldstr      "and the fair Moon rejoices"
      IL_002c:  stelem.ref
      IL_002d:  ldloc.s    V_8
      IL_002f:  ldc.i4.3
      IL_0030:  ldstr      "in the clear and cloudless night"
      IL_0035:  stelem.ref
      IL_0036:  ldloc.s    V_8
      IL_0038:  stloc.0
      IL_0039:  ldc.i4.4
      IL_003a:  newarr     ['mscorlib']System.UInt64
      IL_003f:  stloc.1
      IL_0040:  ldc.i4.4
      IL_0041:  newarr     wchar[]
      IL_0046:  stloc.2
      IL_0047:  ldc.i4.0
      IL_0048:  conv.i8
      IL_0049:  stloc.3
      IL_004a:  br.s       IL_006e

      IL_004c:  ldloc.1
      IL_004d:  ldloc.3
      IL_004e:  conv.i4
      IL_004f:  ldloc.0
      IL_0050:  ldloc.3
      IL_0051:  conv.ovf.i4
      IL_0052:  ldelem.ref
      IL_0053:  call       instance int32 ['mscorlib']System.String::get_Length()
      IL_0058:  ldc.i4.1
      IL_0059:  add.ovf
      IL_005a:  conv.ovf.i8
      IL_005b:  stelem.i8
      IL_005c:  ldloc.2
      IL_005d:  ldloc.3
      IL_005e:  conv.i4
      IL_005f:  ldloc.0
      IL_0060:  ldloc.3
      IL_0061:  conv.i4
      IL_0062:  ldelem.ref
      IL_0063:  call       instance wchar[] ['mscorlib']System.String::ToCharArray()
      IL_0068:  stelem.ref
      IL_0069:  ldloc.3
      IL_006a:  ldc.i4.1
      IL_006b:  conv.i8
      IL_006c:  add
      IL_006d:  stloc.3
      IL_006e:  ldloc.3
      IL_006f:  ldc.i4.4
      IL_0070:  conv.i8
      IL_0071:  blt.un.s   IL_004c

      IL_0073:  ldloc.1
      IL_0074:  ldc.i4.0
      IL_0075:  ldelem.i8
      IL_0076:  conv.i4
      IL_0077:  ldloc.1
      IL_0078:  ldc.i4.1
      IL_0079:  ldelem.i8
      IL_007a:  conv.i4
      IL_007b:  ldloc.1
      IL_007c:  ldc.i4.2
      IL_007d:  ldelem.i8
      IL_007e:  conv.i4
      IL_007f:  ldloc.1
      IL_0080:  ldc.i4.3
      IL_0081:  ldelem.i8
      IL_0082:  conv.i4
      IL_0083:  newobj     instance void unsigned int64[0...,0...,0...,0...]::.ctor(int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32)
      IL_0088:  stloc.s    V_4
      IL_008a:  ldloc.1
      IL_008b:  ldc.i4.0
      IL_008c:  ldelem.i8
      IL_008d:  conv.i4
      IL_008e:  ldloc.1
      IL_008f:  ldc.i4.1
      IL_0090:  ldelem.i8
      IL_0091:  conv.i4
      IL_0092:  ldloc.1
      IL_0093:  ldc.i4.2
      IL_0094:  ldelem.i8
      IL_0095:  conv.i4
      IL_0096:  ldloc.1
      IL_0097:  ldc.i4.3
      IL_0098:  ldelem.i8
      IL_0099:  conv.i4
      IL_009a:  newobj     instance void unsigned int64[0...,0...,0...,0...]::.ctor(int32,
                                                                                    int32,
                                                                                    int32,
                                                                                    int32)
      IL_009f:  stloc.s    V_5
      IL_00a1:  ldloc.s    V_4
      IL_00a3:  ldloc.s    V_5
      IL_00a5:  ldloc.2
      IL_00a6:  ldloc.1
      IL_00a7:  call       void JitTest_lcs_ulong_arrays_il.LCS::findLCS(unsigned int64[0...,0...,0...,0...],
                                                     unsigned int64[0...,0...,0...,0...],
                                                     wchar[][],
                                                     unsigned int64[])
      IL_00ac:  ldc.i4.0
      IL_00ad:  conv.i8
      IL_00ae:  stloc.s    V_6
      IL_00b0:  br.s       IL_00cc

      IL_00b2:  ldloc.1
      IL_00b3:  dup
      IL_00b4:  stloc.s    V_9
      IL_00b6:  ldloc.s    V_6
      IL_00b8:  conv.i4
      IL_00b9:  dup
      IL_00ba:  stloc.s    V_10
      IL_00bc:  ldloc.s    V_9
      IL_00be:  ldloc.s    V_10
      IL_00c0:  ldelem.i8
      IL_00c1:  ldc.i4.1
      IL_00c2:  conv.i8
      IL_00c3:  sub
      IL_00c4:  stelem.i8
      IL_00c5:  ldloc.s    V_6
      IL_00c7:  ldc.i4.1
      IL_00c8:  conv.i8
      IL_00c9:  add
      IL_00ca:  stloc.s    V_6
      IL_00cc:  ldloc.s    V_6
      IL_00ce:  ldc.i4.4
      IL_00cf:  conv.i8
      IL_00d0:  blt.un.s   IL_00b2

      IL_00d2:  ldstr      "n ha  es"
      IL_00d7:  ldloc.s    V_5
      IL_00d9:  ldloc.2
      IL_00da:  ldc.i4.0
      IL_00db:  ldelem.ref
      IL_00dc:  ldloc.1
      IL_00dd:  call       class System.String JitTest_lcs_ulong_arrays_il.LCS::buildLCS(unsigned int64[0...,0...,0...,0...],
                                                                     wchar[],
                                                                     unsigned int64[])
      IL_00e2:  call       bool ['mscorlib']System.String::Equals(class System.String,
                                                                  class System.String)
      IL_00e7:  brfalse.s  IL_00f8

      IL_00e9:  ldstr      "Test passed"
      IL_00ee:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_00f3:  ldc.i4     0x64
      IL_00f4:  stloc.s    V_7
      IL_00f6:  br.s       IL_0107

      IL_00f8:  ldstr      "Test failed."
      IL_00fd:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0102:  ldc.i4.0
      IL_0103:  stloc.s    V_7
      IL_0105:  br.s       IL_0107

      IL_0107:  ldloc.s    V_7
      IL_0109:  ret
    } // end of method 'LCS::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'LCS::.ctor'

  } // end of class 'LCS'

} // end of namespace 'JitTest_lcs_ulong_arrays_il'

//*********** DISASSEMBLY COMPLETE ***********************
