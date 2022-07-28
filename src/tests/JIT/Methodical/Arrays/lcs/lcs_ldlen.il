// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.







.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'lcs_ldlen'// as "lcs"
{

  //  .custom instance void ['mscorlib']System.Diagnostics.DebuggableAttribute::.ctor(bool,
  
  
  
}
.assembly extern xunit.core {}
// MVID: {52ED7FCA-78F1-42F6-8DE5-7388302AFAA6}
.namespace JitTest_lcs_ldlen_lcs_il
{
  .class public auto ansi LCS
         extends ['mscorlib']System.Object
  {
    .field private static literal int32 RANK = int32(0x00000004)
    .method private hidebysig static class System.String 
            buildLCS(int32[0...,0...,0...,0...] b,
                     wchar[] X,
                     int32[] ind) il managed
    {
      // Code size       203 (0xcb)
      .maxstack  7
      .locals (int32 V_0,
               int32 V_1,
               int32 V_2,
               class System.String V_3,
               int32[] V_4,
               int32 V_5)
      IL_0000:  ldc.i4.0
      IL_0001:  stloc.0
      IL_0002:  br.s       IL_0018

      IL_0004:  ldarg.2
      IL_0005:  ldloc.0
      IL_0006:  ldelem.i4
      IL_0007:  brtrue.s   IL_0014

      IL_0009:  ldnull
      IL_000e:  stloc.3
      IL_000f:  br         IL_00c9

      IL_0014:  ldloc.0
      IL_0015:  ldc.i4.1
      IL_0016:  add
      IL_0017:  stloc.0
      IL_0018:  ldloc.0
      IL_0019:  ldc.i4.4
      IL_001a:  blt.s      IL_0004

      IL_001c:  ldarg.0
      IL_001d:  ldarg.2
      IL_001e:  ldc.i4.0
      IL_001f:  ldelem.i4
      IL_0020:  ldarg.2
      IL_0021:  ldc.i4.1
      IL_0022:  ldelem.i4
      IL_0023:  ldarg.2
      IL_0024:  ldc.i4.2
      IL_0025:  ldelem.i4
      IL_0026:  ldarg.2
      IL_0027:  ldc.i4.3
      IL_0028:  ldelem.i4
      IL_0029:  call       instance int32 int32[0...,0...,0...,0...]::Get(int32,
                                                                          int32,
                                                                          int32,
                                                                          int32)
      IL_002e:  ldc.i4.4
      IL_002f:  bne.un.s   IL_006d

      IL_0031:  ldc.i4.0
      IL_0032:  stloc.1
      IL_0033:  br.s       IL_0049

      IL_0035:  ldarg.2
      IL_0036:  dup
      IL_0037:  stloc.s    V_4
      IL_0039:  ldloc.1
      IL_003a:  dup
      IL_003b:  stloc.s    V_5
      IL_003d:  ldloc.s    V_4
      IL_003f:  ldloc.s    V_5
      IL_0041:  ldelem.i4
      IL_0042:  ldc.i4.1
      IL_0043:  sub
      IL_0044:  stelem.i4
      IL_0045:  ldloc.1
      IL_0046:  ldc.i4.1
      IL_0047:  add
      IL_0048:  stloc.1
      IL_0049:  ldloc.1
      IL_004a:  ldc.i4.4
      IL_004b:  blt.s      IL_0035

      IL_004d:  ldarg.2
      IL_004e:  ldc.i4.0
      IL_004f:  ldelem.i4
      IL_0050:  stloc.2
      IL_0051:  ldarg.0
      IL_0052:  ldarg.1
      IL_0053:  ldarg.2
      IL_0054:  call       class System.String JitTest_lcs_ldlen_lcs_il.LCS::buildLCS(int32[0...,0...,0...,0...],
                                                                     wchar[],
                                                                     int32[])
      IL_0059:  ldarg.1
      IL_005a:  ldloc.2
      IL_005b:  ldelem.u2
      IL_0060:  box        ['mscorlib']System.Char
      IL_0065:  call       class System.String ['mscorlib']System.String::Concat(class System.Object,
                                                                                 class System.Object)
      IL_006a:  stloc.3
      IL_006b:  br.s       IL_00c9

      IL_006d:  ldarg.0
      IL_006e:  ldarg.2
      IL_006f:  ldc.i4.0
      IL_0070:  ldelem.i4
      IL_0071:  ldarg.2
      IL_0072:  ldc.i4.1
      IL_0073:  ldelem.i4
      IL_0074:  ldarg.2
      IL_0075:  ldc.i4.2
      IL_0076:  ldelem.i4
      IL_0077:  ldarg.2
      IL_0078:  ldc.i4.3
      IL_0079:  ldelem.i4
      IL_007a:  call       instance int32 int32[0...,0...,0...,0...]::Get(int32,
                                                                          int32,
                                                                          int32,
                                                                          int32)
      IL_007f:  ldc.i4.0
      IL_0080:  blt.s      IL_00c3

      IL_0082:  ldarg.0
      IL_0083:  ldarg.2
      IL_0084:  ldc.i4.0
      IL_0085:  ldelem.i4
      IL_0086:  ldarg.2
      IL_0087:  ldc.i4.1
      IL_0088:  ldelem.i4
      IL_0089:  ldarg.2
      IL_008a:  ldc.i4.2
      IL_008b:  ldelem.i4
      IL_008c:  ldarg.2
      IL_008d:  ldc.i4.3
      IL_008e:  ldelem.i4
      IL_008f:  call       instance int32 int32[0...,0...,0...,0...]::Get(int32,
                                                                          int32,
                                                                          int32,
                                                                          int32)
      IL_0094:  ldc.i4.4
      IL_0095:  bge.s      IL_00c3

      IL_0097:  ldarg.2
      IL_0098:  dup
      IL_0099:  stloc.s    V_4
      IL_009b:  ldarg.0
      IL_009c:  ldarg.2
      IL_009d:  ldc.i4.0
      IL_009e:  ldelem.i4
      IL_009f:  ldarg.2
      IL_00a0:  ldc.i4.1
      IL_00a1:  ldelem.i4
      IL_00a2:  ldarg.2
      IL_00a3:  ldc.i4.2
      IL_00a4:  ldelem.i4
      IL_00a5:  ldarg.2
      IL_00a6:  ldc.i4.3
      IL_00a7:  ldelem.i4
      IL_00a8:  call       instance int32 int32[0...,0...,0...,0...]::Get(int32,
                                                                          int32,
                                                                          int32,
                                                                          int32)
      IL_00ad:  dup
      IL_00ae:  stloc.s    V_5
      IL_00b0:  ldloc.s    V_4
      IL_00b2:  ldloc.s    V_5
      IL_00b4:  ldelem.i4
      IL_00b5:  ldc.i4.1
      IL_00b6:  sub
      IL_00b7:  stelem.i4
      IL_00b8:  ldarg.0
      IL_00b9:  ldarg.1
      IL_00ba:  ldarg.2
      IL_00bb:  call       class System.String JitTest_lcs_ldlen_lcs_il.LCS::buildLCS(int32[0...,0...,0...,0...],
                                                                     wchar[],
                                                                     int32[])
      IL_00c0:  stloc.3
      IL_00c1:  br.s       IL_00c9

      IL_00c3:  newobj     instance void ['mscorlib']System.Exception::.ctor()
      IL_00c8:  throw

      IL_00c9:  ldloc.3
      IL_00ca:  ret
    } // end of method 'LCS::buildLCS'

    .method private hidebysig static void 
            findLCS(int32[0...,0...,0...,0...] c,
                    int32[0...,0...,0...,0...] b,
                    wchar[][] seq) il managed
    {
      // Code size       405 (0x195)
      .maxstack  11
      .locals (int32[] V_0,
               bool V_1,
               int32 V_2,
               int32 V_3,
               int32 V_4,
               int32 V_5,
               int32[] V_6,
               int32 V_7)
      IL_0000:  ldc.i4.4
      IL_0001:  newarr     ['mscorlib']System.Int32
      IL_0006:  stloc.0
      IL_0007:  ldloc.0
      IL_0008:  ldc.i4.0
      IL_0009:  ldc.i4.1
      IL_000a:  stelem.i4
      IL_000b:  br         IL_0189

      IL_0010:  ldloc.0
      IL_0011:  ldc.i4.1
      IL_0012:  ldc.i4.1
      IL_0013:  stelem.i4
      IL_0014:  br         IL_0172

      IL_0019:  ldloc.0
      IL_001a:  ldc.i4.2
      IL_001b:  ldc.i4.1
      IL_001c:  stelem.i4
      IL_001d:  br         IL_015b

      IL_0022:  ldloc.0
      IL_0023:  ldc.i4.3
      IL_0024:  ldc.i4.1
      IL_0025:  stelem.i4
      IL_0026:  br         IL_0144

      IL_002b:  ldc.i4.1
      IL_002c:  stloc.1
      IL_002d:  ldc.i4.1
      IL_002e:  stloc.2
      IL_002f:  br.s       IL_0051

      IL_0031:  ldarg.2
      IL_0032:  ldloc.2
      IL_0033:  ldelem.ref
      IL_0034:  ldloc.0
      IL_0035:  ldloc.2
      IL_0036:  ldelem.i4
      IL_0037:  ldc.i4.1
      IL_0038:  sub
      IL_0039:  ldelem.u2
      IL_003a:  ldarg.2
      IL_003b:  ldloc.2
      IL_003c:  ldc.i4.1
      IL_003d:  sub
      IL_003e:  ldelem.ref
      IL_003f:  ldloc.0
      IL_0040:  ldloc.2
      IL_0041:  ldc.i4.1
      IL_0042:  sub
      IL_0043:  ldelem.i4
      IL_0044:  ldc.i4.1
      IL_0045:  sub
      IL_0046:  ldelem.u2
      IL_0047:  beq.s      IL_004d

      IL_0049:  ldc.i4.0
      IL_004a:  stloc.1
      IL_004b:  br.s       IL_0055

      IL_004d:  ldloc.2
      IL_004e:  ldc.i4.1
      IL_004f:  add
      IL_0050:  stloc.2
      IL_0051:  ldloc.2
      IL_0052:  ldc.i4.4
      IL_0053:  blt.s      IL_0031

      IL_0055:  ldloc.1
      IL_0056:  brfalse.s  IL_009e

      IL_0058:  ldarg.0
      IL_0059:  ldloc.0
      IL_005a:  ldc.i4.0
      IL_005b:  ldelem.i4
      IL_005c:  ldloc.0
      IL_005d:  ldc.i4.1
      IL_005e:  ldelem.i4
      IL_005f:  ldloc.0
      IL_0060:  ldc.i4.2
      IL_0061:  ldelem.i4
      IL_0062:  ldloc.0
      IL_0063:  ldc.i4.3
      IL_0064:  ldelem.i4
      IL_0065:  ldarg.0
      IL_0066:  ldloc.0
      IL_0067:  ldc.i4.0
      IL_0068:  ldelem.i4
      IL_0069:  ldc.i4.1
      IL_006a:  sub
      IL_006b:  ldloc.0
      IL_006c:  ldc.i4.1
      IL_006d:  ldelem.i4
      IL_006e:  ldc.i4.1
      IL_006f:  sub
      IL_0070:  ldloc.0
      IL_0071:  ldc.i4.2
      IL_0072:  ldelem.i4
      IL_0073:  ldc.i4.1
      IL_0074:  sub
      IL_0075:  ldloc.0
      IL_0076:  ldc.i4.3
      IL_0077:  ldelem.i4
      IL_0078:  ldc.i4.1
      IL_0079:  sub
      IL_007a:  call       instance int32 int32[0...,0...,0...,0...]::Get(int32,
                                                                          int32,
                                                                          int32,
                                                                          int32)
      IL_007f:  ldc.i4.1
      IL_0080:  add
      IL_0081:  call       instance void int32[0...,0...,0...,0...]::Set(int32,
                                                                         int32,
                                                                         int32,
                                                                         int32,
                                                                         int32)
      IL_0086:  ldarg.1
      IL_0087:  ldloc.0
      IL_0088:  ldc.i4.0
      IL_0089:  ldelem.i4
      IL_008a:  ldloc.0
      IL_008b:  ldc.i4.1
      IL_008c:  ldelem.i4
      IL_008d:  ldloc.0
      IL_008e:  ldc.i4.2
      IL_008f:  ldelem.i4
      IL_0090:  ldloc.0
      IL_0091:  ldc.i4.3
      IL_0092:  ldelem.i4
      IL_0093:  ldc.i4.4
      IL_0094:  call       instance void int32[0...,0...,0...,0...]::Set(int32,
                                                                         int32,
                                                                         int32,
                                                                         int32,
                                                                         int32)
      IL_0099:  br         IL_0138

      IL_009e:  ldc.i4.m1
      IL_009f:  stloc.3
      IL_00a0:  ldc.i4.m1
      IL_00a1:  stloc.s    V_4
      IL_00a3:  ldc.i4.0
      IL_00a4:  stloc.s    V_5
      IL_00a6:  br.s       IL_00fd

      IL_00a8:  ldloc.0
      IL_00a9:  dup
      IL_00aa:  stloc.s    V_6
      IL_00ac:  ldloc.s    V_5
      IL_00ae:  dup
      IL_00af:  stloc.s    V_7
      IL_00b1:  ldloc.s    V_6
      IL_00b3:  ldloc.s    V_7
      IL_00b5:  ldelem.i4
      IL_00b6:  ldc.i4.1
      IL_00b7:  sub
      IL_00b8:  stelem.i4
      IL_00b9:  ldarg.0
      IL_00ba:  ldloc.0
      IL_00bb:  ldc.i4.0
      IL_00bc:  ldelem.i4
      IL_00bd:  ldloc.0
      IL_00be:  ldc.i4.1
      IL_00bf:  ldelem.i4
      IL_00c0:  ldloc.0
      IL_00c1:  ldc.i4.2
      IL_00c2:  ldelem.i4
      IL_00c3:  ldloc.0
      IL_00c4:  ldc.i4.3
      IL_00c5:  ldelem.i4
      IL_00c6:  call       instance int32 int32[0...,0...,0...,0...]::Get(int32,
                                                                          int32,
                                                                          int32,
                                                                          int32)
      IL_00cb:  ldloc.s    V_4
      IL_00cd:  ble.s      IL_00e6

      IL_00cf:  ldloc.s    V_5
      IL_00d1:  stloc.3
      IL_00d2:  ldarg.0
      IL_00d3:  ldloc.0
      IL_00d4:  ldc.i4.0
      IL_00d5:  ldelem.i4
      IL_00d6:  ldloc.0
      IL_00d7:  ldc.i4.1
      IL_00d8:  ldelem.i4
      IL_00d9:  ldloc.0
      IL_00da:  ldc.i4.2
      IL_00db:  ldelem.i4
      IL_00dc:  ldloc.0
      IL_00dd:  ldc.i4.3
      IL_00de:  ldelem.i4
      IL_00df:  call       instance int32 int32[0...,0...,0...,0...]::Get(int32,
                                                                          int32,
                                                                          int32,
                                                                          int32)
      IL_00e4:  stloc.s    V_4
      IL_00e6:  ldloc.0
      IL_00e7:  dup
      IL_00e8:  stloc.s    V_6
      IL_00ea:  ldloc.s    V_5
      IL_00ec:  dup
      IL_00ed:  stloc.s    V_7
      IL_00ef:  ldloc.s    V_6
      IL_00f1:  ldloc.s    V_7
      IL_00f3:  ldelem.i4
      IL_00f4:  ldc.i4.1
      IL_00f5:  add
      IL_00f6:  stelem.i4
      IL_00f7:  ldloc.s    V_5
      IL_00f9:  ldc.i4.1
      IL_00fa:  add
      IL_00fb:  stloc.s    V_5
      IL_00fd:  ldloc.s    V_5
      IL_00ff:  ldc.i4.4
      IL_0100:  blt.s      IL_00a8

      IL_0102:  ldloc.3
      IL_0103:  ldc.i4.0
      IL_0104:  blt.s      IL_010b

      IL_0106:  ldloc.s    V_4
      IL_0108:  ldc.i4.0
      IL_0109:  bge.s      IL_0111

      IL_010b:  newobj     instance void ['mscorlib']System.Exception::.ctor()
      IL_0110:  throw

      IL_0111:  ldarg.0
      IL_0112:  ldloc.0
      IL_0113:  ldc.i4.0
      IL_0114:  ldelem.i4
      IL_0115:  ldloc.0
      IL_0116:  ldc.i4.1
      IL_0117:  ldelem.i4
      IL_0118:  ldloc.0
      IL_0119:  ldc.i4.2
      IL_011a:  ldelem.i4
      IL_011b:  ldloc.0
      IL_011c:  ldc.i4.3
      IL_011d:  ldelem.i4
      IL_011e:  ldloc.s    V_4
      IL_0120:  call       instance void int32[0...,0...,0...,0...]::Set(int32,
                                                                         int32,
                                                                         int32,
                                                                         int32,
                                                                         int32)
      IL_0125:  ldarg.1
      IL_0126:  ldloc.0
      IL_0127:  ldc.i4.0
      IL_0128:  ldelem.i4
      IL_0129:  ldloc.0
      IL_012a:  ldc.i4.1
      IL_012b:  ldelem.i4
      IL_012c:  ldloc.0
      IL_012d:  ldc.i4.2
      IL_012e:  ldelem.i4
      IL_012f:  ldloc.0
      IL_0130:  ldc.i4.3
      IL_0131:  ldelem.i4
      IL_0132:  ldloc.3
      IL_0133:  call       instance void int32[0...,0...,0...,0...]::Set(int32,
                                                                         int32,
                                                                         int32,
                                                                         int32,
                                                                         int32)
      IL_0138:  ldloc.0
      IL_0139:  dup
      IL_013a:  stloc.s    V_6
      IL_013c:  ldc.i4.3
      IL_013d:  ldloc.s    V_6
      IL_013f:  ldc.i4.3
      IL_0140:  ldelem.i4
      IL_0141:  ldc.i4.1
      IL_0142:  add
      IL_0143:  stelem.i4
      IL_0144:  ldloc.0
      IL_0145:  ldc.i4.3
      IL_0146:  ldelem.i4
      
//		ldarg.3
//		ldc.i4.3
//		ldelem.i4

		ldarg.2
		ldc.i4.3
		ldelem.ref
		ldlen
		ldc.i4.1
		add
      
      IL_014a:  blt        IL_002b

      IL_014f:  ldloc.0
      IL_0150:  dup
      IL_0151:  stloc.s    V_6
      IL_0153:  ldc.i4.2
      IL_0154:  ldloc.s    V_6
      IL_0156:  ldc.i4.2
      IL_0157:  ldelem.i4
      IL_0158:  ldc.i4.1
      IL_0159:  add
      IL_015a:  stelem.i4
      IL_015b:  ldloc.0
      IL_015c:  ldc.i4.2
      IL_015d:  ldelem.i4
      
//      IL_015e:  ldarg.3
//      IL_015f:  ldc.i4.2
//      IL_0160:  ldelem.i4
		ldarg.2
		ldc.i4.2
		ldelem.ref
		ldlen
		ldc.i4.1
		add
      
      IL_0161:  blt        IL_0022

      IL_0166:  ldloc.0
      IL_0167:  dup
      IL_0168:  stloc.s    V_6
      IL_016a:  ldc.i4.1
      IL_016b:  ldloc.s    V_6
      IL_016d:  ldc.i4.1
      IL_016e:  ldelem.i4
      IL_016f:  ldc.i4.1
      IL_0170:  add
      IL_0171:  stelem.i4
      IL_0172:  ldloc.0
      IL_0173:  ldc.i4.1
      IL_0174:  ldelem.i4
//      IL_0175:  ldarg.3
//      IL_0176:  ldc.i4.1
//      IL_0177:  ldelem.i4
		ldarg.2
		ldc.i4.1
		ldelem.ref
		ldlen
		ldc.i4.1
		add

      IL_0178:  blt        IL_0019

      IL_017d:  ldloc.0
      IL_017e:  dup
      IL_017f:  stloc.s    V_6
      IL_0181:  ldc.i4.0
      IL_0182:  ldloc.s    V_6
      IL_0184:  ldc.i4.0
      IL_0185:  ldelem.i4
      IL_0186:  ldc.i4.1
      IL_0187:  add
      IL_0188:  stelem.i4
      IL_0189:  ldloc.0
      IL_018a:  ldc.i4.0
      IL_018b:  ldelem.i4
      
//      IL_018c:  ldarg.3
//      IL_018d:  ldc.i4.0
//      IL_018e:  ldelem.i4

		ldarg.2
		ldc.i4.0
		ldelem.ref
		ldlen
		ldc.i4.1
		add
      
      IL_018f:  blt        IL_0010

      IL_0194:  ret
    } // end of method 'LCS::findLCS'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       227 (0xe3)
      .maxstack  5
      .locals (class System.String[] V_0,
               int32[] V_1,
               wchar[][] V_2,
               int32 V_3,
               int32[0...,0...,0...,0...] V_4,
               int32[0...,0...,0...,0...] V_5,
               int32 V_6,
               int32 V_7,
               int32[] V_8,
               int32 V_9)
      IL_0000:  ldc.i4.4
      IL_0001:  newarr     ['mscorlib']System.String
      IL_0006:  stloc.0
      IL_0007:  ldloc.0
      IL_0008:  ldc.i4.0
      IL_0009:  ldstr      "The Sun has left his blackness"
      IL_000e:  stelem.ref
      IL_000f:  ldloc.0
      IL_0010:  ldc.i4.1
      IL_0011:  ldstr      "and has found a fresher morning"
      IL_0016:  stelem.ref
      IL_0017:  ldloc.0
      IL_0018:  ldc.i4.2
      IL_0019:  ldstr      "and the fair Moon rejoices"
      IL_001e:  stelem.ref
      IL_001f:  ldloc.0
      IL_0020:  ldc.i4.3
      IL_0021:  ldstr      "in the clear and cloudless night"
      IL_0026:  stelem.ref
      IL_0027:  ldc.i4.4
      IL_0028:  newarr     ['mscorlib']System.Int32
      IL_002d:  stloc.1
      IL_002e:  ldc.i4.4
      IL_002f:  newarr     wchar[]
      IL_0034:  stloc.2
      IL_0035:  ldc.i4.0
      IL_0036:  stloc.3
      IL_0037:  br.s       IL_0055

      IL_0039:  ldloc.1
      IL_003a:  ldloc.3
      IL_003b:  ldloc.0
      IL_003c:  ldloc.3
      IL_003d:  ldelem.ref
      IL_003e:  call       instance int32 ['mscorlib']System.String::get_Length()
      IL_0043:  ldc.i4.1
      IL_0044:  add
      IL_0045:  stelem.i4
      IL_0046:  ldloc.2
      IL_0047:  ldloc.3
      IL_0048:  ldloc.0
      IL_0049:  ldloc.3
      IL_004a:  ldelem.ref
      IL_004b:  call       instance wchar[] ['mscorlib']System.String::ToCharArray()
      IL_0050:  stelem.ref
      IL_0051:  ldloc.3
      IL_0052:  ldc.i4.1
      IL_0053:  add
      IL_0054:  stloc.3
      IL_0055:  ldloc.3
      IL_0056:  ldc.i4.4
      IL_0057:  blt.s      IL_0039

      IL_0059:  ldloc.1
      IL_005a:  ldc.i4.0
      IL_005b:  ldelem.i4
      IL_005c:  ldloc.1
      IL_005d:  ldc.i4.1
      IL_005e:  ldelem.i4
      IL_005f:  ldloc.1
      IL_0060:  ldc.i4.2
      IL_0061:  ldelem.i4
      IL_0062:  ldloc.1
      IL_0063:  ldc.i4.3
      IL_0064:  ldelem.i4
      IL_0065:  newobj     instance void int32[0...,0...,0...,0...]::.ctor(int32,
                                                                           int32,
                                                                           int32,
                                                                           int32)
      IL_006a:  stloc.s    V_4
      IL_006c:  ldloc.1
      IL_006d:  ldc.i4.0
      IL_006e:  ldelem.i4
      IL_006f:  ldloc.1
      IL_0070:  ldc.i4.1
      IL_0071:  ldelem.i4
      IL_0072:  ldloc.1
      IL_0073:  ldc.i4.2
      IL_0074:  ldelem.i4
      IL_0075:  ldloc.1
      IL_0076:  ldc.i4.3
      IL_0077:  ldelem.i4
      IL_0078:  newobj     instance void int32[0...,0...,0...,0...]::.ctor(int32,
                                                                           int32,
                                                                           int32,
                                                                           int32)
      IL_007d:  stloc.s    V_5
      IL_007f:  ldloc.s    V_4
      IL_0081:  ldloc.s    V_5
      IL_0083:  ldloc.2
      //IL_0084:  ldloc.1
      IL_0085:  call       void JitTest_lcs_ldlen_lcs_il.LCS::findLCS(int32[0...,0...,0...,0...],
                                                     int32[0...,0...,0...,0...],
                                                     wchar[][])
      IL_008a:  ldc.i4.0
      IL_008b:  stloc.s    V_6
      IL_008d:  br.s       IL_00a6

      IL_008f:  ldloc.1
      IL_0090:  dup
      IL_0091:  stloc.s    V_8
      IL_0093:  ldloc.s    V_6
      IL_0095:  dup
      IL_0096:  stloc.s    V_9
      IL_0098:  ldloc.s    V_8
      IL_009a:  ldloc.s    V_9
      IL_009c:  ldelem.i4
      IL_009d:  ldc.i4.1
      IL_009e:  sub
      IL_009f:  stelem.i4
      IL_00a0:  ldloc.s    V_6
      IL_00a2:  ldc.i4.1
      IL_00a3:  add
      IL_00a4:  stloc.s    V_6
      IL_00a6:  ldloc.s    V_6
      IL_00a8:  ldc.i4.4
      IL_00a9:  blt.s      IL_008f

      IL_00ab:  ldstr      "n ha  es"
      IL_00b0:  ldloc.s    V_5
      IL_00b2:  ldloc.2
      IL_00b3:  ldc.i4.0
      IL_00b4:  ldelem.ref
      IL_00b5:  ldloc.1
      IL_00b6:  call       class System.String JitTest_lcs_ldlen_lcs_il.LCS::buildLCS(int32[0...,0...,0...,0...],
                                                                     wchar[],
                                                                     int32[])
      IL_00bb:  call       bool ['mscorlib']System.String::Equals(class System.String,
                                                                  class System.String)
      IL_00c0:  brfalse.s  IL_00d1

      IL_00c2:  ldstr      "Test passed"
      IL_00c7:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_00cc:  ldc.i4    0x64
      IL_00cd:  stloc.s    V_7
      IL_00cf:  br.s       IL_00e0

      IL_00d1:  ldstr      "Test failed."
      IL_00d6:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_00db:  ldc.i4.0
      IL_00dc:  stloc.s    V_7
      IL_00de:  br.s       IL_00e0

      IL_00e0:  ldloc.s    V_7
      IL_00e2:  ret
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

} // end of namespace 'JitTest_lcs_ldlen_lcs_il'

//*********** DISASSEMBLY COMPLETE ***********************
