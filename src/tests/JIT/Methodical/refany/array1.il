// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME
{
}
.assembly extern xunit.core {}
// MVID: {F8BFB824-C7D0-485F-A06E-07B6D1F1151B}
.namespace JitTest_array1_refany_il
{
  .class public auto ansi Test
         extends [mscorlib]System.Object
  {
    .method private hidebysig static void 
            TestRef(typedref _ref) il managed
    {
      // Code size       285 (0x11d)
      .maxstack  6
      .locals (int32 V_0,
               int32 V_1,
               int32 V_2,
               int32 V_3,
               int32 V_4,
               int32 V_5,
               int64[] V_6,
               int32 V_7,
               unsigned int64[0...,0...] V_8,
               int32 V_9,
               unsigned int64[] V_10)
      IL_0000:  ldarg.0
      IL_0001:  refanytype
      IL_0003:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0008:  ldtoken    [mscorlib]System.Array
      IL_000d:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0012:  bne.un.s   IL_003d

      IL_0014:  ldc.i4.0
      IL_0015:  stloc.0
      IL_0016:  br.s       IL_002e

      IL_0018:  ldarg.0
      IL_0019:  refanyval  [mscorlib]System.Array
      IL_001e:  ldind.ref
      IL_001f:  newobj     instance void JitTest_array1_refany_il.Test::.ctor()
      IL_0024:  ldloc.0
      IL_0025:  call       instance void [mscorlib]System.Array::SetValue(class [mscorlib]System.Object,
                                                                          int32)
      IL_002a:  ldloc.0
      IL_002b:  ldc.i4.1
      IL_002c:  add
      IL_002d:  stloc.0
      IL_002e:  ldloc.0
      IL_002f:  ldarg.0
      IL_0030:  refanyval  [mscorlib]System.Array
      IL_0035:  ldind.ref
      IL_0036:  call       instance int32 [mscorlib]System.Array::get_Length()
      IL_003b:  blt.s      IL_0018

      IL_003d:  ldarg.0
      IL_003e:  refanytype
      IL_0040:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0045:  ldtoken    int64[]
      IL_004a:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_004f:  bne.un.s   IL_007c

      IL_0051:  ldc.i4.0
      IL_0052:  stloc.1
      IL_0053:  br.s       IL_0070

      IL_0055:  ldarg.0
      IL_0056:  refanyval  int64[]
      IL_005b:  ldind.ref
      IL_005c:  dup
      IL_005d:  stloc.s    V_6
      IL_005f:  ldloc.1
      IL_0060:  dup
      IL_0061:  stloc.s    V_7
      IL_0063:  ldloc.s    V_6
      IL_0065:  ldloc.s    V_7
      IL_0067:  ldelem.i8
      IL_0068:  ldc.i4.1
      IL_0069:  conv.i8
      IL_006a:  add
      IL_006b:  stelem.i8
      IL_006c:  ldloc.1
      IL_006d:  ldc.i4.1
      IL_006e:  add
      IL_006f:  stloc.1
      IL_0070:  ldloc.1
      IL_0071:  ldarg.0
      IL_0072:  refanyval  int64[]
      IL_0077:  ldind.ref
      IL_0078:  ldlen
      IL_0079:  conv.i4
      IL_007a:  blt.s      IL_0055

      IL_007c:  ldarg.0
      IL_007d:  refanytype
      IL_007f:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0084:  ldtoken    unsigned int64[0...,0...]
      IL_0089:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_008e:  bne.un.s   IL_00cd

      IL_0090:  ldc.i4.0
      IL_0091:  stloc.2
      IL_0092:  br.s       IL_00c9

      IL_0094:  ldc.i4.0
      IL_0095:  stloc.3
      IL_0096:  br.s       IL_00c1

      IL_0098:  ldarg.0
      IL_0099:  refanyval  unsigned int64[0...,0...]
      IL_009e:  ldind.ref
      IL_009f:  dup
      IL_00a0:  stloc.s    V_8
      IL_00a2:  ldloc.2
      IL_00a3:  dup
      IL_00a4:  stloc.s    V_7
      IL_00a6:  ldloc.3
      IL_00a7:  dup
      IL_00a8:  stloc.s    V_9
      IL_00aa:  ldloc.s    V_8
      IL_00ac:  ldloc.s    V_7
      IL_00ae:  ldloc.s    V_9
      IL_00b0:  call       instance unsigned int64 unsigned int64[0...,0...]::Get(int32,
                                                                                  int32)
      IL_00b5:  ldc.i4.1
      IL_00b6:  conv.i8
      IL_00b7:  sub
      IL_00b8:  call       instance void unsigned int64[0...,0...]::Set(int32,
                                                                        int32,
                                                                        unsigned int64)
      IL_00bd:  ldloc.3
      IL_00be:  ldc.i4.1
      IL_00bf:  add
      IL_00c0:  stloc.3
      IL_00c1:  ldloc.3
      IL_00c2:  ldc.i4.3
      IL_00c3:  blt.s      IL_0098

      IL_00c5:  ldloc.2
      IL_00c6:  ldc.i4.1
      IL_00c7:  add
      IL_00c8:  stloc.2
      IL_00c9:  ldloc.2
      IL_00ca:  ldc.i4.2
      IL_00cb:  blt.s      IL_0094

      IL_00cd:  ldarg.0
      IL_00ce:  refanytype
      IL_00d0:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_00d5:  ldtoken    unsigned int64[][]
      IL_00da:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_00df:  bne.un.s   IL_011c

      IL_00e1:  ldc.i4.0
      IL_00e2:  stloc.s    V_4
      IL_00e4:  br.s       IL_0117

      IL_00e6:  ldc.i4.0
      IL_00e7:  stloc.s    V_5
      IL_00e9:  br.s       IL_010c

      IL_00eb:  ldarg.0
      IL_00ec:  refanyval  unsigned int64[][]
      IL_00f1:  ldind.ref
      IL_00f2:  ldloc.s    V_4
      IL_00f4:  ldelem.ref
      IL_00f5:  dup
      IL_00f6:  stloc.s    V_10
      IL_00f8:  ldloc.s    V_5
      IL_00fa:  dup
      IL_00fb:  stloc.s    V_7
      IL_00fd:  ldloc.s    V_10
      IL_00ff:  ldloc.s    V_7
      IL_0101:  ldelem.i8
      IL_0102:  ldc.i4.1
      IL_0103:  conv.i8
      IL_0104:  sub
      IL_0105:  stelem.i8
      IL_0106:  ldloc.s    V_5
      IL_0108:  ldc.i4.1
      IL_0109:  add
      IL_010a:  stloc.s    V_5
      IL_010c:  ldloc.s    V_5
      IL_010e:  ldc.i4.3
      IL_010f:  blt.s      IL_00eb

      IL_0111:  ldloc.s    V_4
      IL_0113:  ldc.i4.1
      IL_0114:  add
      IL_0115:  stloc.s    V_4
      IL_0117:  ldloc.s    V_4
      IL_0119:  ldc.i4.2
      IL_011a:  blt.s      IL_00e6

      IL_011c:  ret
    } // end of method Test::TestRef

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       438 (0x1b6)
      .maxstack  5
      .locals (class [mscorlib]System.Array V_0,
               int32 V_1,
               int64[] V_2,
               unsigned int64[0...,0...] V_3,
               int32 V_4,
               int32 V_5,
               unsigned int64[][] V_6,
               int32 V_7,
               int32 V_8,
               int64[] V_9,
               unsigned int64[0...,0...] V_10,
               unsigned int64[][] V_11,
               unsigned int64[] V_12)
      IL_0000:  ldtoken    JitTest_array1_refany_il.Test
      IL_0005:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_000a:  ldc.i4.s   16
      IL_000c:  call       class [mscorlib]System.Array [mscorlib]System.Array::CreateInstance(class [mscorlib]System.Type,
                                                                                               int32)
      IL_0011:  stloc.0
      IL_0012:  ldloca.s   V_0
      IL_0014:  mkrefany   [mscorlib]System.Array
      IL_0019:  call       void JitTest_array1_refany_il.Test::TestRef(typedref)
      IL_001e:  ldc.i4.0
      IL_001f:  stloc.1
      IL_0020:  br.s       IL_0049

      IL_0022:  ldloc.0
      IL_0023:  ldloc.1
      IL_0024:  call       instance class [mscorlib]System.Object [mscorlib]System.Array::GetValue(int32)
      IL_0029:  brfalse.s  IL_0043

      IL_002b:  ldloc.0
      IL_002c:  ldloc.1
      IL_002d:  call       instance class [mscorlib]System.Object [mscorlib]System.Array::GetValue(int32)
      IL_0032:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0037:  ldtoken    JitTest_array1_refany_il.Test
      IL_003c:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(value class [mscorlib]System.RuntimeTypeHandle)
      IL_0041:  beq.s      IL_0045

      IL_0043:  ldc.i4.1
      IL_0044:  ret

      IL_0045:  ldloc.1
      IL_0046:  ldc.i4.1
      IL_0047:  add
      IL_0048:  stloc.1
      IL_0049:  ldloc.1
      IL_004a:  ldc.i4.s   16
      IL_004c:  blt.s      IL_0022

      IL_004e:  ldc.i4.3
      IL_004f:  newarr     [mscorlib]System.Int64
      IL_0054:  stloc.s    V_9
      IL_0056:  ldloc.s    V_9
      IL_0058:  ldc.i4.0
      IL_0059:  ldc.i4.1
      IL_005a:  conv.i8
      IL_005b:  stelem.i8
      IL_005c:  ldloc.s    V_9
      IL_005e:  ldc.i4.1
      IL_005f:  ldc.i4.2
      IL_0060:  conv.i8
      IL_0061:  stelem.i8
      IL_0062:  ldloc.s    V_9
      IL_0064:  ldc.i4.2
      IL_0065:  ldc.i4.3
      IL_0066:  conv.i8
      IL_0067:  stelem.i8
      IL_0068:  ldloc.s    V_9
      IL_006a:  stloc.2
      IL_006b:  ldloca.s   V_2
      IL_006d:  mkrefany   int64[]
      IL_0072:  call       void JitTest_array1_refany_il.Test::TestRef(typedref)
      IL_0077:  ldloc.2
      IL_0078:  ldc.i4.0
      IL_0079:  ldelem.i8
      IL_007a:  ldc.i4.2
      IL_007b:  conv.i8
      IL_007c:  bne.un.s   IL_008c

      IL_007e:  ldloc.2
      IL_007f:  ldc.i4.1
      IL_0080:  ldelem.i8
      IL_0081:  ldc.i4.3
      IL_0082:  conv.i8
      IL_0083:  bne.un.s   IL_008c

      IL_0085:  ldloc.2
      IL_0086:  ldc.i4.2
      IL_0087:  ldelem.i8
      IL_0088:  ldc.i4.4
      IL_0089:  conv.i8
      IL_008a:  beq.s      IL_008e

      IL_008c:  ldc.i4.2
      IL_008d:  ret

      IL_008e:  ldc.i4.2
      IL_008f:  ldc.i4.3
      IL_0090:  newobj     instance void unsigned int64[0...,0...]::.ctor(int32,
                                                                          int32)
      IL_0095:  stloc.s    V_10
      IL_0097:  ldloc.s    V_10
      IL_0099:  ldc.i4.0
      IL_009a:  ldc.i4.0
      IL_009b:  ldc.i4.1
      IL_009c:  conv.i8
      IL_009d:  call       instance void unsigned int64[0...,0...]::Set(int32,
                                                                        int32,
                                                                        unsigned int64)
      IL_00a2:  ldloc.s    V_10
      IL_00a4:  ldc.i4.0
      IL_00a5:  ldc.i4.1
      IL_00a6:  ldc.i4.2
      IL_00a7:  conv.i8
      IL_00a8:  call       instance void unsigned int64[0...,0...]::Set(int32,
                                                                        int32,
                                                                        unsigned int64)
      IL_00ad:  ldloc.s    V_10
      IL_00af:  ldc.i4.0
      IL_00b0:  ldc.i4.2
      IL_00b1:  ldc.i4.3
      IL_00b2:  conv.i8
      IL_00b3:  call       instance void unsigned int64[0...,0...]::Set(int32,
                                                                        int32,
                                                                        unsigned int64)
      IL_00b8:  ldloc.s    V_10
      IL_00ba:  ldc.i4.1
      IL_00bb:  ldc.i4.0
      IL_00bc:  ldc.i4.4
      IL_00bd:  conv.i8
      IL_00be:  call       instance void unsigned int64[0...,0...]::Set(int32,
                                                                        int32,
                                                                        unsigned int64)
      IL_00c3:  ldloc.s    V_10
      IL_00c5:  ldc.i4.1
      IL_00c6:  ldc.i4.1
      IL_00c7:  ldc.i4.5
      IL_00c8:  conv.i8
      IL_00c9:  call       instance void unsigned int64[0...,0...]::Set(int32,
                                                                        int32,
                                                                        unsigned int64)
      IL_00ce:  ldloc.s    V_10
      IL_00d0:  ldc.i4.1
      IL_00d1:  ldc.i4.2
      IL_00d2:  ldc.i4.6
      IL_00d3:  conv.i8
      IL_00d4:  call       instance void unsigned int64[0...,0...]::Set(int32,
                                                                        int32,
                                                                        unsigned int64)
      IL_00d9:  ldloc.s    V_10
      IL_00db:  stloc.3
      IL_00dc:  ldloca.s   V_3
      IL_00de:  mkrefany   unsigned int64[0...,0...]
      IL_00e3:  call       void JitTest_array1_refany_il.Test::TestRef(typedref)
      IL_00e8:  ldc.i4.0
      IL_00e9:  stloc.s    V_4
      IL_00eb:  br.s       IL_0119

      IL_00ed:  ldc.i4.0
      IL_00ee:  stloc.s    V_5
      IL_00f0:  br.s       IL_010e

      IL_00f2:  ldloc.3
      IL_00f3:  ldloc.s    V_4
      IL_00f5:  ldloc.s    V_5
      IL_00f7:  call       instance unsigned int64 unsigned int64[0...,0...]::Get(int32,
                                                                                  int32)
      IL_00fc:  ldloc.s    V_4
      IL_00fe:  ldc.i4.3
      IL_00ff:  mul
      IL_0100:  ldloc.s    V_5
      IL_0102:  add
      IL_0103:  conv.i8
      IL_0104:  beq.s      IL_0108

      IL_0106:  ldc.i4.3
      IL_0107:  ret

      IL_0108:  ldloc.s    V_5
      IL_010a:  ldc.i4.1
      IL_010b:  add
      IL_010c:  stloc.s    V_5
      IL_010e:  ldloc.s    V_5
      IL_0110:  ldc.i4.3
      IL_0111:  blt.s      IL_00f2

      IL_0113:  ldloc.s    V_4
      IL_0115:  ldc.i4.1
      IL_0116:  add
      IL_0117:  stloc.s    V_4
      IL_0119:  ldloc.s    V_4
      IL_011b:  ldc.i4.2
      IL_011c:  blt.s      IL_00ed

      IL_011e:  ldc.i4.2
      IL_011f:  newarr     unsigned int64[]
      IL_0124:  stloc.s    V_11
      IL_0126:  ldloc.s    V_11
      IL_0128:  ldc.i4.0
      IL_0129:  ldc.i4.3
      IL_012a:  newarr     [mscorlib]System.UInt64
      IL_012f:  stloc.s    V_12
      IL_0131:  ldloc.s    V_12
      IL_0133:  ldc.i4.0
      IL_0134:  ldc.i4.1
      IL_0135:  conv.i8
      IL_0136:  stelem.i8
      IL_0137:  ldloc.s    V_12
      IL_0139:  ldc.i4.1
      IL_013a:  ldc.i4.2
      IL_013b:  conv.i8
      IL_013c:  stelem.i8
      IL_013d:  ldloc.s    V_12
      IL_013f:  ldc.i4.2
      IL_0140:  ldc.i4.3
      IL_0141:  conv.i8
      IL_0142:  stelem.i8
      IL_0143:  ldloc.s    V_12
      IL_0145:  stelem.ref
      IL_0146:  ldloc.s    V_11
      IL_0148:  ldc.i4.1
      IL_0149:  ldc.i4.3
      IL_014a:  newarr     [mscorlib]System.UInt64
      IL_014f:  stloc.s    V_12
      IL_0151:  ldloc.s    V_12
      IL_0153:  ldc.i4.0
      IL_0154:  ldc.i4.4
      IL_0155:  conv.i8
      IL_0156:  stelem.i8
      IL_0157:  ldloc.s    V_12
      IL_0159:  ldc.i4.1
      IL_015a:  ldc.i4.5
      IL_015b:  conv.i8
      IL_015c:  stelem.i8
      IL_015d:  ldloc.s    V_12
      IL_015f:  ldc.i4.2
      IL_0160:  ldc.i4.6
      IL_0161:  conv.i8
      IL_0162:  stelem.i8
      IL_0163:  ldloc.s    V_12
      IL_0165:  stelem.ref
      IL_0166:  ldloc.s    V_11
      IL_0168:  stloc.s    V_6
      IL_016a:  ldloca.s   V_6
      IL_016c:  mkrefany   unsigned int64[][]
      IL_0171:  call       void JitTest_array1_refany_il.Test::TestRef(typedref)
      IL_0176:  ldc.i4.0
      IL_0177:  stloc.s    V_7
      IL_0179:  br.s       IL_01a5

      IL_017b:  ldc.i4.0
      IL_017c:  stloc.s    V_8
      IL_017e:  br.s       IL_019a

      IL_0180:  ldloc.s    V_6
      IL_0182:  ldloc.s    V_7
      IL_0184:  ldelem.ref
      IL_0185:  ldloc.s    V_8
      IL_0187:  ldelem.i8
      IL_0188:  ldloc.s    V_7
      IL_018a:  ldc.i4.3
      IL_018b:  mul
      IL_018c:  ldloc.s    V_8
      IL_018e:  add
      IL_018f:  conv.i8
      IL_0190:  beq.s      IL_0194

      IL_0192:  ldc.i4.3
      IL_0193:  ret

      IL_0194:  ldloc.s    V_8
      IL_0196:  ldc.i4.1
      IL_0197:  add
      IL_0198:  stloc.s    V_8
      IL_019a:  ldloc.s    V_8
      IL_019c:  ldc.i4.3
      IL_019d:  blt.s      IL_0180

      IL_019f:  ldloc.s    V_7
      IL_01a1:  ldc.i4.1
      IL_01a2:  add
      IL_01a3:  stloc.s    V_7
      IL_01a5:  ldloc.s    V_7
      IL_01a7:  ldc.i4.2
      IL_01a8:  blt.s      IL_017b

      IL_01aa:  ldstr      "Test passed."
      IL_01af:  call       void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
      IL_01b4:  ldc.i4 100
      IL_01b5:  ret
    } // end of method Test::Main

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test::.ctor

  } // end of class Test

} // end of namespace JitTest_array1_refany_il

//*********** DISASSEMBLY COMPLETE ***********************
