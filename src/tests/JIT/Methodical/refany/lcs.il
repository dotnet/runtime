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
.assembly lcs// as "lcs"
{
}
.assembly extern xunit.core {}
.namespace JitTest_lcs_refany_il
{
  .class public auto ansi LCS
         extends [mscorlib]System.Object
  {
    .field private static literal int32 RANK = int32(0x00000004)
    .method private hidebysig static string 
            buildLCS(typedref _b,
                     typedref _X,
                     typedref _ind) il managed
    {
      .maxstack  6
      .locals (int32 V_0,
               typedref V_1,
               int32 V_2,
               typedref V_3,
               int32 V_4,
               int32[] V_5,
               int32 V_6)
      IL_0000:  ldc.i4.0
      IL_0001:  stloc.0
      IL_0002:  ldloca.s   V_0
      IL_0004:  mkrefany   [mscorlib]System.Int32
      IL_0009:  stloc.1
      IL_000a:  br.s       IL_0027

      IL_000c:  ldarg.2
      IL_000d:  refanyval  int32[]
      IL_0012:  ldind.ref
      IL_0013:  ldloc.1
      IL_0014:  refanyval  [mscorlib]System.Int32
      IL_0019:  ldind.i4
      IL_001a:  ldelem.i4
      IL_001b:  brtrue.s   IL_0023

      IL_001d:  ldstr      ""
      IL_0022:  ret

      IL_0023:  ldloc.0
      IL_0024:  ldc.i4.1
      IL_0025:  add
      IL_0026:  stloc.0
      IL_0027:  ldloc.1
      IL_0028:  refanyval  [mscorlib]System.Int32
      IL_002d:  ldind.i4
      IL_002e:  ldc.i4.4
      IL_002f:  blt.s      IL_000c

      IL_0031:  ldarg.0
      IL_0032:  refanyval  int32[0...,0...,0...,0...]
      IL_0037:  ldind.ref
      IL_0038:  ldarg.2
      IL_0039:  refanyval  int32[]
      IL_003e:  ldind.ref
      IL_003f:  ldc.i4.0
      IL_0040:  ldelem.i4
      IL_0041:  ldarg.2
      IL_0042:  refanyval  int32[]
      IL_0047:  ldind.ref
      IL_0048:  ldc.i4.1
      IL_0049:  ldelem.i4
      IL_004a:  ldarg.2
      IL_004b:  refanyval  int32[]
      IL_0050:  ldind.ref
      IL_0051:  ldc.i4.2
      IL_0052:  ldelem.i4
      IL_0053:  ldarg.2
      IL_0054:  refanyval  int32[]
      IL_0059:  ldind.ref
      IL_005a:  ldc.i4.3
      IL_005b:  ldelem.i4
      IL_005c:  call       instance int32 int32[0...,0...,0...,0...]::Get(int32,
                                                                          int32,
                                                                          int32,
                                                                          int32)
      IL_0061:  stloc.2
      IL_0062:  ldloc.2
      IL_0063:  ldc.i4.4
      IL_0064:  bne.un.s   IL_00c8

      IL_0066:  ldc.i4.0
      IL_0067:  stloc.0
      IL_0068:  ldloca.s   V_0
      IL_006a:  mkrefany   [mscorlib]System.Int32
      IL_006f:  stloc.3
      IL_0070:  br.s       IL_0092

      IL_0072:  ldarg.2
      IL_0073:  refanyval  int32[]
      IL_0078:  ldind.ref
      IL_0079:  dup
      IL_007a:  stloc.s    V_5
      IL_007c:  ldloc.3
      IL_007d:  refanyval  [mscorlib]System.Int32
      IL_0082:  ldind.i4
      IL_0083:  dup
      IL_0084:  stloc.s    V_6
      IL_0086:  ldloc.s    V_5
      IL_0088:  ldloc.s    V_6
      IL_008a:  ldelem.i4
      IL_008b:  ldc.i4.1
      IL_008c:  sub
      IL_008d:  stelem.i4
      IL_008e:  ldloc.0
      IL_008f:  ldc.i4.1
      IL_0090:  add
      IL_0091:  stloc.0
      IL_0092:  ldloc.3
      IL_0093:  refanyval  [mscorlib]System.Int32
      IL_0098:  ldind.i4
      IL_0099:  ldc.i4.4
      IL_009a:  blt.s      IL_0072

      IL_009c:  ldarg.2
      IL_009d:  refanyval  int32[]
      IL_00a2:  ldind.ref
      IL_00a3:  ldc.i4.0
      IL_00a4:  ldelem.i4
      IL_00a5:  stloc.s    V_4
      IL_00a7:  ldarg.0
      IL_00a8:  ldarg.1
      IL_00a9:  ldarg.2
      IL_00aa:  call       string JitTest_lcs_refany_il.LCS::buildLCS(typedref,
                                                                     typedref,
                                                                     typedref)
      IL_00af:  ldarg.1
      IL_00b0:  refanyval  wchar[]
      IL_00b5:  ldind.ref
      IL_00b6:  ldloc.s    V_4
      IL_00b8:  ldelem.u2
      IL_00bd:  box        [mscorlib]System.Char
      IL_00c2:  call       string [mscorlib]System.String::Concat(class [mscorlib]System.Object,
                                                                               class [mscorlib]System.Object)
      IL_00c7:  ret

      IL_00c8:  ldloc.2
      IL_00c9:  ldc.i4.0
      IL_00ca:  blt.s      IL_00ef

      IL_00cc:  ldloc.2
      IL_00cd:  ldc.i4.4
      IL_00ce:  bge.s      IL_00ef

      IL_00d0:  ldarg.2
      IL_00d1:  refanyval  int32[]
      IL_00d6:  ldind.ref
      IL_00d7:  dup
      IL_00d8:  stloc.s    V_5
      IL_00da:  ldloc.2
      IL_00db:  dup
      IL_00dc:  stloc.s    V_6
      IL_00de:  ldloc.s    V_5
      IL_00e0:  ldloc.s    V_6
      IL_00e2:  ldelem.i4
      IL_00e3:  ldc.i4.1
      IL_00e4:  sub
      IL_00e5:  stelem.i4
      IL_00e6:  ldarg.0
      IL_00e7:  ldarg.1
      IL_00e8:  ldarg.2
      IL_00e9:  call       string JitTest_lcs_refany_il.LCS::buildLCS(typedref,
                                                                     typedref,
                                                                     typedref)
      IL_00ee:  ret

      IL_00ef:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_00f4:  throw
    } // end of method LCS::buildLCS

    .method private hidebysig static void 
            findLCS(typedref _c,
                    typedref _b,
                    typedref _seq,
                    typedref _len) il managed
    {
      .maxstack  11
      .locals (int32[] V_0,
               bool V_1,
               int32 V_2,
               int32 V_3,
               int32 V_4,
               int32 V_5,
               int32[] V_6,
               int32 V_7)

		ldarg.0 refanytype
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle )
		ldarg.1 refanytype
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle )
		ldarg.2 refanytype
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle )
		ldarg.3 refanytype
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle )
		ceq		
		ceq
		ceq
		pop

		
      IL_0000:  ldc.i4.4
      IL_0001:  newarr     [mscorlib]System.Int32
      IL_0006:  stloc.0
      IL_0007:  ldloc.0
      IL_0008:  ldc.i4.0
      IL_0009:  ldc.i4.1
      IL_000a:  stelem.i4
      IL_000b:  br         IL_01d1

      IL_0010:  ldloc.0
      IL_0011:  ldc.i4.1
      IL_0012:  ldc.i4.1
      IL_0013:  stelem.i4
      IL_0014:  br         IL_01b4

      IL_0019:  ldloc.0
      IL_001a:  ldc.i4.2
      IL_001b:  ldc.i4.1
      IL_001c:  stelem.i4
      IL_001d:  br         IL_0197

      IL_0022:  ldloc.0
      IL_0023:  ldc.i4.3
      IL_0024:  ldc.i4.1
      IL_0025:  stelem.i4
      IL_0026:  br         IL_017a

      IL_002b:  ldc.i4.1
      IL_002c:  stloc.1
      IL_002d:  ldc.i4.1
      IL_002e:  stloc.2
      IL_002f:  br.s       IL_005d

      IL_0031:  ldarg.2
      IL_0032:  refanyval  wchar[][]
      IL_0037:  ldind.ref
      IL_0038:  ldloc.2
      IL_0039:  ldelem.ref
      IL_003a:  ldloc.0
      IL_003b:  ldloc.2
      IL_003c:  ldelem.i4
      IL_003d:  ldc.i4.1
      IL_003e:  sub
      IL_003f:  ldelem.u2
      IL_0040:  ldarg.2
      IL_0041:  refanyval  wchar[][]
      IL_0046:  ldind.ref
      IL_0047:  ldloc.2
      IL_0048:  ldc.i4.1
      IL_0049:  sub
      IL_004a:  ldelem.ref
      IL_004b:  ldloc.0
      IL_004c:  ldloc.2
      IL_004d:  ldc.i4.1
      IL_004e:  sub
      IL_004f:  ldelem.i4
      IL_0050:  ldc.i4.1
      IL_0051:  sub
      IL_0052:  ldelem.u2
      IL_0053:  beq.s      IL_0059

      IL_0055:  ldc.i4.0
      IL_0056:  stloc.1
      IL_0057:  br.s       IL_0061

      IL_0059:  ldloc.2
      IL_005a:  ldc.i4.1
      IL_005b:  add
      IL_005c:  stloc.2
      IL_005d:  ldloc.2
      IL_005e:  ldc.i4.4
      IL_005f:  blt.s      IL_0031

      IL_0061:  ldloc.1
      IL_0062:  brfalse.s  IL_00bc

      IL_0064:  ldarg.0
      IL_0065:  refanyval  int32[0...,0...,0...,0...]
      IL_006a:  ldind.ref
      IL_006b:  ldloc.0
      IL_006c:  ldc.i4.0
      IL_006d:  ldelem.i4
      IL_006e:  ldloc.0
      IL_006f:  ldc.i4.1
      IL_0070:  ldelem.i4
      IL_0071:  ldloc.0
      IL_0072:  ldc.i4.2
      IL_0073:  ldelem.i4
      IL_0074:  ldloc.0
      IL_0075:  ldc.i4.3
      IL_0076:  ldelem.i4
      IL_0077:  ldarg.0
      IL_0078:  refanyval  int32[0...,0...,0...,0...]
      IL_007d:  ldind.ref
      IL_007e:  ldloc.0
      IL_007f:  ldc.i4.0
      IL_0080:  ldelem.i4
      IL_0081:  ldc.i4.1
      IL_0082:  sub
      IL_0083:  ldloc.0
      IL_0084:  ldc.i4.1
      IL_0085:  ldelem.i4
      IL_0086:  ldc.i4.1
      IL_0087:  sub
      IL_0088:  ldloc.0
      IL_0089:  ldc.i4.2
      IL_008a:  ldelem.i4
      IL_008b:  ldc.i4.1
      IL_008c:  sub
      IL_008d:  ldloc.0
      IL_008e:  ldc.i4.3
      IL_008f:  ldelem.i4
      IL_0090:  ldc.i4.1
      IL_0091:  sub
      IL_0092:  call       instance int32 int32[0...,0...,0...,0...]::Get(int32,
                                                                          int32,
                                                                          int32,
                                                                          int32)
      IL_0097:  ldc.i4.1
      IL_0098:  add
      IL_0099:  call       instance void int32[0...,0...,0...,0...]::Set(int32,
                                                                         int32,
                                                                         int32,
                                                                         int32,
                                                                         int32)
      IL_009e:  ldarg.1
      IL_009f:  refanyval  int32[0...,0...,0...,0...]
      IL_00a4:  ldind.ref
      IL_00a5:  ldloc.0
      IL_00a6:  ldc.i4.0
      IL_00a7:  ldelem.i4
      IL_00a8:  ldloc.0
      IL_00a9:  ldc.i4.1
      IL_00aa:  ldelem.i4
      IL_00ab:  ldloc.0
      IL_00ac:  ldc.i4.2
      IL_00ad:  ldelem.i4
      IL_00ae:  ldloc.0
      IL_00af:  ldc.i4.3
      IL_00b0:  ldelem.i4
      IL_00b1:  ldc.i4.4
      IL_00b2:  call       instance void int32[0...,0...,0...,0...]::Set(int32,
                                                                         int32,
                                                                         int32,
                                                                         int32,
                                                                         int32)
      IL_00b7:  br         IL_016e

      IL_00bc:  ldc.i4.m1
      IL_00bd:  stloc.3
      IL_00be:  ldc.i4.m1
      IL_00bf:  stloc.s    V_4
      IL_00c1:  ldc.i4.0
      IL_00c2:  stloc.s    V_5
      IL_00c4:  br.s       IL_0127

      IL_00c6:  ldloc.0
      IL_00c7:  dup
      IL_00c8:  stloc.s    V_6
      IL_00ca:  ldloc.s    V_5
      IL_00cc:  dup
      IL_00cd:  stloc.s    V_7
      IL_00cf:  ldloc.s    V_6
      IL_00d1:  ldloc.s    V_7
      IL_00d3:  ldelem.i4
      IL_00d4:  ldc.i4.1
      IL_00d5:  sub
      IL_00d6:  stelem.i4
      IL_00d7:  ldarg.0
      IL_00d8:  refanyval  int32[0...,0...,0...,0...]
      IL_00dd:  ldind.ref
      IL_00de:  ldloc.0
      IL_00df:  ldc.i4.0
      IL_00e0:  ldelem.i4
      IL_00e1:  ldloc.0
      IL_00e2:  ldc.i4.1
      IL_00e3:  ldelem.i4
      IL_00e4:  ldloc.0
      IL_00e5:  ldc.i4.2
      IL_00e6:  ldelem.i4
      IL_00e7:  ldloc.0
      IL_00e8:  ldc.i4.3
      IL_00e9:  ldelem.i4
      IL_00ea:  call       instance int32 int32[0...,0...,0...,0...]::Get(int32,
                                                                          int32,
                                                                          int32,
                                                                          int32)
      IL_00ef:  ldloc.s    V_4
      IL_00f1:  ble.s      IL_0110

      IL_00f3:  ldloc.s    V_5
      IL_00f5:  stloc.3
      IL_00f6:  ldarg.0
      IL_00f7:  refanyval  int32[0...,0...,0...,0...]
      IL_00fc:  ldind.ref
      IL_00fd:  ldloc.0
      IL_00fe:  ldc.i4.0
      IL_00ff:  ldelem.i4
      IL_0100:  ldloc.0
      IL_0101:  ldc.i4.1
      IL_0102:  ldelem.i4
      IL_0103:  ldloc.0
      IL_0104:  ldc.i4.2
      IL_0105:  ldelem.i4
      IL_0106:  ldloc.0
      IL_0107:  ldc.i4.3
      IL_0108:  ldelem.i4
      IL_0109:  call       instance int32 int32[0...,0...,0...,0...]::Get(int32,
                                                                          int32,
                                                                          int32,
                                                                          int32)
      IL_010e:  stloc.s    V_4
      IL_0110:  ldloc.0
      IL_0111:  dup
      IL_0112:  stloc.s    V_6
      IL_0114:  ldloc.s    V_5
      IL_0116:  dup
      IL_0117:  stloc.s    V_7
      IL_0119:  ldloc.s    V_6
      IL_011b:  ldloc.s    V_7
      IL_011d:  ldelem.i4
      IL_011e:  ldc.i4.1
      IL_011f:  add
      IL_0120:  stelem.i4
      IL_0121:  ldloc.s    V_5
      IL_0123:  ldc.i4.1
      IL_0124:  add
      IL_0125:  stloc.s    V_5
      IL_0127:  ldloc.s    V_5
      IL_0129:  ldc.i4.4
      IL_012a:  blt.s      IL_00c6

      IL_012c:  ldloc.3
      IL_012d:  ldc.i4.0
      IL_012e:  blt.s      IL_0135

      IL_0130:  ldloc.s    V_4
      IL_0132:  ldc.i4.0
      IL_0133:  bge.s      IL_013b

      IL_0135:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_013a:  throw

      IL_013b:  ldarg.0
      IL_013c:  refanyval  int32[0...,0...,0...,0...]
      IL_0141:  ldind.ref
      IL_0142:  ldloc.0
      IL_0143:  ldc.i4.0
      IL_0144:  ldelem.i4
      IL_0145:  ldloc.0
      IL_0146:  ldc.i4.1
      IL_0147:  ldelem.i4
      IL_0148:  ldloc.0
      IL_0149:  ldc.i4.2
      IL_014a:  ldelem.i4
      IL_014b:  ldloc.0
      IL_014c:  ldc.i4.3
      IL_014d:  ldelem.i4
      IL_014e:  ldloc.s    V_4
      IL_0150:  call       instance void int32[0...,0...,0...,0...]::Set(int32,
                                                                         int32,
                                                                         int32,
                                                                         int32,
                                                                         int32)
      IL_0155:  ldarg.1
      IL_0156:  refanyval  int32[0...,0...,0...,0...]
      IL_015b:  ldind.ref
      IL_015c:  ldloc.0
      IL_015d:  ldc.i4.0
      IL_015e:  ldelem.i4
      IL_015f:  ldloc.0
      IL_0160:  ldc.i4.1
      IL_0161:  ldelem.i4
      IL_0162:  ldloc.0
      IL_0163:  ldc.i4.2
      IL_0164:  ldelem.i4
      IL_0165:  ldloc.0
      IL_0166:  ldc.i4.3
      IL_0167:  ldelem.i4
      IL_0168:  ldloc.3
      IL_0169:  call       instance void int32[0...,0...,0...,0...]::Set(int32,
                                                                         int32,
                                                                         int32,
                                                                         int32,
                                                                         int32)
      IL_016e:  ldloc.0
      IL_016f:  dup
      IL_0170:  stloc.s    V_6
      IL_0172:  ldc.i4.3
      IL_0173:  ldloc.s    V_6
      IL_0175:  ldc.i4.3
      IL_0176:  ldelem.i4
      IL_0177:  ldc.i4.1
      IL_0178:  add
      IL_0179:  stelem.i4
      IL_017a:  ldloc.0
      IL_017b:  ldc.i4.3
      IL_017c:  ldelem.i4
      IL_017d:  ldarg.3
      IL_017e:  refanyval  int32[]
      IL_0183:  ldind.ref
      IL_0184:  ldc.i4.3
      IL_0185:  ldelem.i4
      IL_0186:  blt        IL_002b

      IL_018b:  ldloc.0
      IL_018c:  dup
      IL_018d:  stloc.s    V_6
      IL_018f:  ldc.i4.2
      IL_0190:  ldloc.s    V_6
      IL_0192:  ldc.i4.2
      IL_0193:  ldelem.i4
      IL_0194:  ldc.i4.1
      IL_0195:  add
      IL_0196:  stelem.i4
      IL_0197:  ldloc.0
      IL_0198:  ldc.i4.2
      IL_0199:  ldelem.i4
      IL_019a:  ldarg.3
      IL_019b:  refanyval  int32[]
      IL_01a0:  ldind.ref
      IL_01a1:  ldc.i4.2
      IL_01a2:  ldelem.i4
      IL_01a3:  blt        IL_0022

      IL_01a8:  ldloc.0
      IL_01a9:  dup
      IL_01aa:  stloc.s    V_6
      IL_01ac:  ldc.i4.1
      IL_01ad:  ldloc.s    V_6
      IL_01af:  ldc.i4.1
      IL_01b0:  ldelem.i4
      IL_01b1:  ldc.i4.1
      IL_01b2:  add
      IL_01b3:  stelem.i4
      IL_01b4:  ldloc.0
      IL_01b5:  ldc.i4.1
      IL_01b6:  ldelem.i4
      IL_01b7:  ldarg.3
      IL_01b8:  refanyval  int32[]
      IL_01bd:  ldind.ref
      IL_01be:  ldc.i4.1
      IL_01bf:  ldelem.i4
      IL_01c0:  blt        IL_0019

      IL_01c5:  ldloc.0
      IL_01c6:  dup
      IL_01c7:  stloc.s    V_6
      IL_01c9:  ldc.i4.0
      IL_01ca:  ldloc.s    V_6
      IL_01cc:  ldc.i4.0
      IL_01cd:  ldelem.i4
      IL_01ce:  ldc.i4.1
      IL_01cf:  add
      IL_01d0:  stelem.i4
      IL_01d1:  ldloc.0
      IL_01d2:  ldc.i4.0
      IL_01d3:  ldelem.i4
      IL_01d4:  ldarg.3
      IL_01d5:  refanyval  int32[]
      IL_01da:  ldind.ref
      IL_01db:  ldc.i4.0
      IL_01dc:  ldelem.i4
      IL_01dd:  blt        IL_0010

      IL_01e2:  ret
    } // end of method LCS::findLCS

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  5
      .locals (string[] V_0,
               int32[] V_1,
               wchar[][] V_2,
               int32 V_3,
               int32[0...,0...,0...,0...] V_4,
               int32[0...,0...,0...,0...] V_5,
               int32 V_6,
               string[] V_7,
               int32[] V_8,
               int32 V_9)
      IL_0000:  ldstr      "Test searches for longest common subsequence of 4 "
      + "strings\n\n"
      IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
      IL_000a:  ldc.i4.4
      IL_000b:  newarr     [mscorlib]System.String
      IL_0010:  stloc.s    V_7
      IL_0012:  ldloc.s    V_7
      IL_0014:  ldc.i4.0
      IL_0015:  ldstr      "The Sun has left"
      IL_001a:  stelem.ref
      IL_001b:  ldloc.s    V_7
      IL_001d:  ldc.i4.1
      IL_001e:  ldstr      "his blackness and"
      IL_0023:  stelem.ref
      IL_0024:  ldloc.s    V_7
      IL_0026:  ldc.i4.2
      IL_0027:  ldstr      "has found a fresher"
      IL_002c:  stelem.ref
      IL_002d:  ldloc.s    V_7
      IL_002f:  ldc.i4.3
      IL_0030:  ldstr      "morning and the fair Moon"
      IL_0035:  stelem.ref
      IL_0036:  ldloc.s    V_7
      IL_0038:  stloc.0
      IL_0039:  ldc.i4.4
      IL_003a:  newarr     [mscorlib]System.Int32
      IL_003f:  stloc.1
      IL_0040:  ldc.i4.4
      IL_0041:  newarr     wchar[]
      IL_0046:  stloc.2
      IL_0047:  ldc.i4.0
      IL_0048:  stloc.3
      IL_0049:  br.s       IL_0067

      IL_004b:  ldloc.1
      IL_004c:  ldloc.3
      IL_004d:  ldloc.0
      IL_004e:  ldloc.3
      IL_004f:  ldelem.ref
      IL_0050:  call       instance int32 [mscorlib]System.String::get_Length()
      IL_0055:  ldc.i4.1
      IL_0056:  add
      IL_0057:  stelem.i4
      IL_0058:  ldloc.2
      IL_0059:  ldloc.3
      IL_005a:  ldloc.0
      IL_005b:  ldloc.3
      IL_005c:  ldelem.ref
      IL_005d:  call       instance wchar[] [mscorlib]System.String::ToCharArray()
      IL_0062:  stelem.ref
      IL_0063:  ldloc.3
      IL_0064:  ldc.i4.1
      IL_0065:  add
      IL_0066:  stloc.3
      IL_0067:  ldloc.3
      IL_0068:  ldc.i4.4
      IL_0069:  blt.s      IL_004b

      IL_006b:  ldloc.1
      IL_006c:  ldc.i4.0
      IL_006d:  ldelem.i4
      IL_006e:  ldloc.1
      IL_006f:  ldc.i4.1
      IL_0070:  ldelem.i4
      IL_0071:  ldloc.1
      IL_0072:  ldc.i4.2
      IL_0073:  ldelem.i4
      IL_0074:  ldloc.1
      IL_0075:  ldc.i4.3
      IL_0076:  ldelem.i4
      IL_0077:  newobj     instance void int32[0...,0...,0...,0...]::.ctor(int32,
                                                                           int32,
                                                                           int32,
                                                                           int32)
      IL_007c:  stloc.s    V_4
      IL_007e:  ldloc.1
      IL_007f:  ldc.i4.0
      IL_0080:  ldelem.i4
      IL_0081:  ldloc.1
      IL_0082:  ldc.i4.1
      IL_0083:  ldelem.i4
      IL_0084:  ldloc.1
      IL_0085:  ldc.i4.2
      IL_0086:  ldelem.i4
      IL_0087:  ldloc.1
      IL_0088:  ldc.i4.3
      IL_0089:  ldelem.i4
      IL_008a:  newobj     instance void int32[0...,0...,0...,0...]::.ctor(int32,
                                                                           int32,
                                                                           int32,
                                                                           int32)
      IL_008f:  stloc.s    V_5
      IL_0091:  ldloca.s   V_4
      IL_0093:  mkrefany   int32[0...,0...,0...,0...]
      IL_0098:  ldloca.s   V_5
      IL_009a:  mkrefany   int32[0...,0...,0...,0...]
      IL_009f:  ldloca.s   V_2
      IL_00a1:  mkrefany   wchar[][]
      IL_00a6:  ldloca.s   V_1
      IL_00a8:  mkrefany   int32[]
      IL_00ad:  call       void JitTest_lcs_refany_il.LCS::findLCS(typedref,
                                                     typedref,
                                                     typedref,
                                                     typedref)
      IL_00b2:  ldc.i4.0
      IL_00b3:  stloc.s    V_6
      IL_00b5:  br.s       IL_00ce

      IL_00b7:  ldloc.1
      IL_00b8:  dup
      IL_00b9:  stloc.s    V_8
      IL_00bb:  ldloc.s    V_6
      IL_00bd:  dup
      IL_00be:  stloc.s    V_9
      IL_00c0:  ldloc.s    V_8
      IL_00c2:  ldloc.s    V_9
      IL_00c4:  ldelem.i4
      IL_00c5:  ldc.i4.1
      IL_00c6:  sub
      IL_00c7:  stelem.i4
      IL_00c8:  ldloc.s    V_6
      IL_00ca:  ldc.i4.1
      IL_00cb:  add
      IL_00cc:  stloc.s    V_6
      IL_00ce:  ldloc.s    V_6
      IL_00d0:  ldc.i4.4
      IL_00d1:  blt.s      IL_00b7

      IL_00d3:  ldstr      " n a"
      IL_00d8:  ldloca.s   V_5
      IL_00da:  mkrefany   int32[0...,0...,0...,0...]
      IL_00df:  ldloc.2
      IL_00e0:  ldc.i4.0
      IL_00e1:  ldelema    wchar[]
      IL_00e6:  mkrefany   wchar[]
      IL_00eb:  ldloca.s   V_1
      IL_00ed:  mkrefany   int32[]
      IL_00f2:  call       string JitTest_lcs_refany_il.LCS::buildLCS(typedref,
                                                                     typedref,
                                                                     typedref)
      IL_00f7:  call       bool [mscorlib]System.String::Equals(string,
                                                                string)
      IL_00fc:  brfalse.s  IL_010a

      IL_00fe:  ldstr      "Test passed"
      IL_0103:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0108:  ldc.i4 100
      IL_0109:  ret

      IL_010a:  ldstr      "Test failed."
      IL_010f:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0114:  ldc.i4.1
      IL_0115:  ret
    } // end of method LCS::Main

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method LCS::.ctor

  } // end of class LCS

} // end of namespace JitTest_lcs_refany_il

