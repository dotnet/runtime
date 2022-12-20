// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib
{
}

.assembly extern System.Console as console
{
}

.assembly sin3double
{
}
.assembly extern xunit.core {}

.namespace Test_sin3double {
.class private sequential ansi sealed beforefieldinit VV extends [mscorlib]System.ValueType
{
  .pack 0
  .size 40
}

.class public auto ansi beforefieldinit Z extends [mscorlib] System.Object
{

.method static float64 mySin(float64 Angle) cil managed
{
  .locals (int32 V_0,
           float64 V_1,
           float64 V_2,
           float64 V_3,
           float64 V_4,
           object V_5,
           object V_6,
           object V_7,
           object V_8,
           object V_9,
           object V_10,
           object V_11,
           object V_12,
           object V_13,
           object V_14,
           float64 V_15,
           float64 V_16)
  IL_0000:  ldnull
  IL_0001:  stloc.s    V_14
  IL_0003:  ldnull
  IL_0004:  stloc.s    V_13
  IL_0006:  ldnull
  IL_0007:  stloc.s    V_12
  IL_0009:  ldnull
  IL_000a:  stloc.s    V_11
  IL_000c:  ldnull
  IL_000d:  stloc.s    V_10
  IL_000f:  ldc.r8     0.0
  IL_0018:  box        [mscorlib]System.Double
  IL_001d:  stloc.s    V_14
  IL_001f:  ldc.r8     0.0
  IL_0028:  box        [mscorlib]System.Double
  IL_002d:  stloc.s    V_13
  IL_002f:  ldc.r8     0.0
  IL_0038:  box        [mscorlib]System.Double
  IL_003d:  stloc.s    V_12
  IL_003f:  ldc.r8     0.0
  IL_0048:  box        [mscorlib]System.Double
  IL_004d:  stloc.s    V_11
  IL_004f:  ldc.r8     1.
  IL_0058:  box        [mscorlib]System.Double
  IL_005d:  stloc.s    V_10
  IL_005f:  ldloc.s    V_14
  IL_0061:  stloc.s    V_9
  IL_0063:  ldloc.s    V_9
  IL_0065:  isinst     [mscorlib]System.Double
  IL_006a:  brfalse.s  IL_0075

  IL_006c:  ldloc.s    V_9
  IL_006e:  unbox      [mscorlib]System.Double
  IL_0073:  br.s       IL_0077

  IL_0075:  ldc.i4.0
  IL_0076:  conv.i
  IL_0077:  ldind.r8
  IL_0078:  stloc.s    V_4
  IL_007a:  ldloc.s    V_13
  IL_007c:  stloc.s    V_8
  IL_007e:  ldloc.s    V_8
  IL_0080:  isinst     [mscorlib]System.Double
  IL_0085:  brfalse.s  IL_0090

  IL_0087:  ldloc.s    V_8
  IL_0089:  unbox      [mscorlib]System.Double
  IL_008e:  br.s       IL_0092

  IL_0090:  ldc.i4.0
  IL_0091:  conv.i
  IL_0092:  ldind.r8
  IL_0093:  stloc.3
  IL_0094:  ldloc.s    V_12
  IL_0096:  stloc.s    V_7
  IL_0098:  ldloc.s    V_7
  IL_009a:  isinst     [mscorlib]System.Double
  IL_009f:  brfalse.s  IL_00aa

  IL_00a1:  ldloc.s    V_7
  IL_00a3:  unbox      [mscorlib]System.Double
  IL_00a8:  br.s       IL_00ac

  IL_00aa:  ldc.i4.0
  IL_00ab:  conv.i
  IL_00ac:  ldind.r8
  IL_00ad:  stloc.2
  IL_00ae:  ldloc.s    V_11
  IL_00b0:  stloc.s    V_6
  IL_00b2:  ldloc.s    V_6
  IL_00b4:  isinst     [mscorlib]System.Double
  IL_00b9:  brfalse.s  IL_00c4

  IL_00bb:  ldloc.s    V_6
  IL_00bd:  unbox      [mscorlib]System.Double
  IL_00c2:  br.s       IL_00c6

  IL_00c4:  ldc.i4.0
  IL_00c5:  conv.i
  IL_00c6:  ldind.r8
  IL_00c7:  stloc.s    V_16
  IL_00c9:  ldloc.s    V_10
  IL_00cb:  stloc.s    V_5
  IL_00cd:  ldloc.s    V_5
  IL_00cf:  isinst     [mscorlib]System.Double
  IL_00d4:  brfalse.s  IL_00df

  IL_00d6:  ldloc.s    V_5
  IL_00d8:  unbox      [mscorlib]System.Double
  IL_00dd:  br.s       IL_00e1

  IL_00df:  ldc.i4.0
  IL_00e0:  conv.i
  IL_00e1:  ldind.r8
  IL_00e2:  stloc.1
  IL_00e3:  ldarg.0
  IL_00e4:  stloc.2
  IL_00e5:  ldloc.2
  IL_00e6:  stloc.s    V_4
  IL_00e8:  ldc.r8     0.0
  IL_00f1:  stloc.3
  IL_00f2:  ldc.i4.1
  IL_00f3:  stloc.0
  IL_00f4:  br.s       IL_00fa

  IL_00f6:  ldloc.0
  IL_00f7:  ldc.i4.2
  IL_00f8:  add
  IL_00f9:  stloc.0
  IL_00fa:  ldloc.0
  IL_00fb:  ldc.i4     0xc8
  IL_0100:  bgt.s      IL_012b

  IL_0102:  ldloc.3
  IL_0103:  ldloc.2
  IL_0104:  add
  IL_0105:  stloc.3
  IL_0106:  ldloc.s    V_4
  IL_0108:  ldarg.0
  IL_0109:  neg
  IL_010a:  ldarg.0
  IL_010b:  mul
  IL_010c:  mul
  IL_010d:  stloc.s    V_4
  IL_010f:  ldloc.1
  IL_0110:  ldloc.0
  IL_0111:  ldc.i4.1
  IL_0112:  add
  IL_0113:  ldloc.0
  IL_0114:  ldc.i4.2
  IL_0115:  add
  IL_0116:  mul
  IL_0117:  conv.r8
  IL_0118:  mul
  IL_0119:  stloc.1
  IL_011a:  ldloc.1
  IL_011b:  call       bool [mscorlib]System.Double::IsInfinity(float64)
  IL_0120:  brfalse.s  IL_0124

  IL_0122:  br.s       IL_012b

  IL_0124:  ldloc.s    V_4
  IL_0126:  ldloc.1
  IL_0127:  div
  IL_0128:  stloc.2
  IL_0129:  br.s       IL_00f6

  IL_012b:  ldloc.3
  IL_012c:  stloc.s    V_15
  IL_012e:  ldloc.s    V_15
  IL_0130:  ret
}

.method public static int32 Main() cil managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  .locals (int32 V_0,
           float64 V_1,
           float64 V_2,
           float64 V_3,
           int32 V_4,
           object V_5,
           object V_6,
           object V_7,
           object V_8,
           object V_9,
           object V_10,
           valuetype Test_sin3double.VV V_11)
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.s    V_4
  IL_0003:  ldnull
  IL_0004:  stloc.s    V_10
  IL_0006:  ldnull
  IL_0007:  stloc.s    V_9
  IL_0009:  ldnull
  IL_000a:  stloc.s    V_8
  IL_000c:  ldc.r8     0.0
  IL_0015:  box        [mscorlib]System.Double
  IL_001a:  stloc.s    V_10
  IL_001c:  ldc.r8     0.0
  IL_0025:  box        [mscorlib]System.Double
  IL_002a:  stloc.s    V_9
  IL_002c:  ldc.r8     0.0
  IL_0035:  box        [mscorlib]System.Double
  IL_003a:  stloc.s    V_8
  IL_003c:  ldloc.s    V_10
  IL_003e:  stloc.s    V_7
  IL_0040:  ldloc.s    V_7
  IL_0042:  isinst     [mscorlib]System.Double
  IL_0047:  brfalse.s  IL_0052

  IL_0049:  ldloc.s    V_7
  IL_004b:  unbox      [mscorlib]System.Double
  IL_0050:  br.s       IL_0054

  IL_0052:  ldc.i4.0
  IL_0053:  conv.i
  IL_0054:  ldind.r8
  IL_0055:  stloc.3
  IL_0056:  ldloc.s    V_9
  IL_0058:  stloc.s    V_6
  IL_005a:  ldloc.s    V_6
  IL_005c:  isinst     [mscorlib]System.Double
  IL_0061:  brfalse.s  IL_006c

  IL_0063:  ldloc.s    V_6
  IL_0065:  unbox      [mscorlib]System.Double
  IL_006a:  br.s       IL_006e

  IL_006c:  ldc.i4.0
  IL_006d:  conv.i
  IL_006e:  ldind.r8
  IL_006f:  stloc.2
  IL_0070:  ldloc.s    V_8
  IL_0072:  stloc.s    V_5
  IL_0074:  ldloc.s    V_5
  IL_0076:  isinst     [mscorlib]System.Double
  IL_007b:  brfalse.s  IL_0086

  IL_007d:  ldloc.s    V_5
  IL_007f:  unbox      [mscorlib]System.Double
  IL_0084:  br.s       IL_0088

  IL_0086:  ldc.i4.0
  IL_0087:  conv.i
  IL_0088:  ldind.r8
  IL_0089:  stloc.1
  IL_008a:  ldloca.s   V_11
  IL_008c:  ldc.r4     0.0
  IL_0091:  stind.r4
  IL_0092:  ldloca.s   V_11
  IL_0094:  ldc.i4.4
  IL_0095:  add
  IL_0096:  ldc.r4     0.309017
  IL_009b:  stind.r4
  IL_009c:  ldloca.s   V_11
  IL_009e:  ldc.i4.8
  IL_009f:  add
  IL_00a0:  ldc.r4     0.58778524
  IL_00a5:  stind.r4
  IL_00a6:  ldloca.s   V_11
  IL_00a8:  ldc.i4.s   12
  IL_00aa:  add
  IL_00ab:  ldc.r4     0.809017
  IL_00b0:  stind.r4
  IL_00b1:  ldloca.s   V_11
  IL_00b3:  ldc.i4.s   16
  IL_00b5:  add
  IL_00b6:  ldc.r4     0.95105654
  IL_00bb:  stind.r4
  IL_00bc:  ldloca.s   V_11
  IL_00be:  ldc.i4.s   20
  IL_00c0:  add
  IL_00c1:  ldc.r4     1.
  IL_00c6:  stind.r4
  IL_00c7:  ldloca.s   V_11
  IL_00c9:  ldc.i4.s   24
  IL_00cb:  add
  IL_00cc:  ldc.r4     0.95105654
  IL_00d1:  stind.r4
  IL_00d2:  ldloca.s   V_11
  IL_00d4:  ldc.i4.s   28
  IL_00d6:  add
  IL_00d7:  ldc.r4     0.809017
  IL_00dc:  stind.r4
  IL_00dd:  ldloca.s   V_11
  IL_00df:  ldc.i4.s   32
  IL_00e1:  add
  IL_00e2:  ldc.r4     0.58778524
  IL_00e7:  stind.r4
  IL_00e8:  ldloca.s   V_11
  IL_00ea:  ldc.i4.s   36
  IL_00ec:  add
  IL_00ed:  ldc.r4     0.309017
  IL_00f2:  stind.r4
  IL_00f3:  ldc.i4.0
  IL_00f4:  stloc.0
  IL_00f5:  br.s       IL_00fb

  IL_00f7:  ldloc.0
  IL_00f8:  ldc.i4.1
  IL_00f9:  add
  IL_00fa:  stloc.0
  IL_00fb:  ldloc.0
  IL_00fc:  ldc.i4.s   10
  IL_00fe:  bge        IL_0227

  IL_0103:  ldc.r8     3.1415926535897931
  IL_010c:  ldloc.0
  IL_010d:  conv.r8
  IL_010e:  ldc.r8     10.
  IL_0117:  div
  IL_0118:  mul
  IL_0119:  stloc.3
  IL_011a:  ldstr      "Classlib Sin("
  IL_0124:  call       void [console]System.Console::Write(string)
  IL_0129:  ldloc.3
  IL_012a:  call       void [console]System.Console::Write(float64)
  IL_012f:  ldstr      ")="
  IL_0139:  call       void [console]System.Console::Write(string)
  IL_013e:  ldloc.3
  IL_013f:  call       float64 [mscorlib]System.Math::Sin(float64)
  IL_0144:  stloc.2
  IL_0145:  ldloc.2
  IL_0146:  call       void [console]System.Console::WriteLine(float64)
  IL_014b:  ldstr      "This Version("
  IL_0155:  call       void [console]System.Console::Write(string)
  IL_015a:  ldloc.3
  IL_015b:  call       void [console]System.Console::Write(float64)
  IL_0160:  ldstr      ")="
  IL_016a:  call       void [console]System.Console::Write(string)
  IL_016f:  ldloc.3
  IL_0170:  call       float64 Test_sin3double.Z::mySin(float64)
  IL_0175:  stloc.1
  IL_0176:  ldloc.1
  IL_0177:  call       void [console]System.Console::WriteLine(float64)
  IL_017c:  ldstr      "Error is:"
  IL_0186:  call       void [console]System.Console::Write(string)
  IL_018b:  ldloc.2
  IL_018c:  ldloc.1
  IL_018d:  sub
  IL_018e:  call       void [console]System.Console::WriteLine(float64)
  IL_0193:  call       void [console]System.Console::WriteLine()
  IL_0198:  ldloc.2
  IL_0199:  ldloc.1
  IL_019a:  sub
  IL_019b:  call       float64 [mscorlib]System.Math::Abs(float64)
  IL_01a0:  ldc.r8     1.0000000000000001e-005
  IL_01a9:  ble.un.s   IL_01c2

  IL_01ab:  ldstr      "ERROR, Versions too far apart!"
  IL_01b5:  call       void [console]System.Console::WriteLine(string)
  IL_01ba:  ldc.i4.1
  IL_01bb:  stloc.s    V_4
  IL_01bd:  br         IL_023a

  IL_01c2:  ldloca.s   V_11
  IL_01c4:  ldloc.0
  IL_01c5:  conv.i8
  IL_01c6:  ldc.i4.4
  IL_01c7:  conv.i8
  IL_01c8:  mul
  IL_01c9:  add
            conv.i
  IL_01ca:  ldind.r4
  IL_01cb:  conv.r8
  IL_01cc:  ldloc.2
  IL_01cd:  sub
  IL_01ce:  call       float64 [mscorlib]System.Math::Abs(float64)
  IL_01d3:  ldc.r8     1.0000000000000001e-005
  IL_01dc:  ble.un.s   IL_01f2

  IL_01de:  ldstr      "ERROR, ClassLib version isn't right!"
  IL_01e8:  call       void [console]System.Console::WriteLine(string)
  IL_01ed:  ldc.i4.1
  IL_01ee:  stloc.s    V_4
  IL_01f0:  br.s       IL_023a

  IL_01f2:  ldloca.s   V_11
  IL_01f4:  ldloc.0
  IL_01f5:  conv.i8
  IL_01f6:  ldc.i4.4
  IL_01f7:  conv.i8
  IL_01f8:  mul
  IL_01f9:  add
            conv.i
  IL_01fa:  ldind.r4
  IL_01fb:  conv.r8
  IL_01fc:  ldloc.1
  IL_01fd:  sub
  IL_01fe:  call       float64 [mscorlib]System.Math::Abs(float64)
  IL_0203:  ldc.r8     1.0000000000000001e-005
  IL_020c:  ble.un.s   IL_0222

  IL_020e:  ldstr      "ERROR, our version isn't right!"
  IL_0218:  call       void [console]System.Console::WriteLine(string)
  IL_021d:  ldc.i4.1
  IL_021e:  stloc.s    V_4
  IL_0220:  br.s       IL_023a

  IL_0222:  br         IL_00f7

  IL_0227:  ldstr      "Yippee, all correct"
  IL_0231:  call       void [console]System.Console::WriteLine(string)
  IL_0236:  ldc.i4.s   100
  IL_0238:  stloc.s    V_4
  IL_023a:  ldloc.s    V_4
  IL_023c:  ret
}

}
}
