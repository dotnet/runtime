// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern legacy library mscorlib { auto }
.assembly ASSEMBLY_NAME {}
.assembly extern xunit.core {}
.assembly extern sinlib_il
{
}
.namespace SinCalc_sin_il_xlang_il
{
  .class public auto ansi SinCalc_sin_il_xlang_il extends [mscorlib]System.Object
  {
    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  3
      .locals (class System.Object V_0,
               class System.Object V_1,
               class System.Object V_2,
               class System.Object V_3,
               class System.Object[] V_4,
               class System.Object V_5,
               float64 V_6,
               int32 V_7,
               value class [sinlib_il]SinCalcLib.PiVal V_8)
      IL_0000:  ldc.i4.s   10
      IL_0002:  newarr     [mscorlib]System.Object
      IL_0007:  stloc.s    V_4
      IL_0009:  ldloc.s    V_4
      IL_000b:  ldc.i4.0
      IL_000c:  ldc.r8     0.
      IL_0015:  stloc.s    V_6
      IL_0017:  ldloc.s   V_6
      IL_0019:  box  [mscorlib]System.Double
      IL_001e:  stelem.ref
      IL_001f:  ldloc.s    V_4
      IL_0021:  ldc.i4.1
      IL_0022:  ldc.r8     0.30901699399999999
      IL_002b:  stloc.s    V_6
      IL_002d:  ldloc.s   V_6
      IL_002f:  box  [mscorlib]System.Double
      IL_0034:  stelem.ref
      IL_0035:  ldloc.s    V_4
      IL_0037:  ldc.i4.2
      IL_0038:  ldc.r8     0.58778525199999998
      IL_0041:  stloc.s    V_6
      IL_0043:  ldloc.s   V_6
      IL_0045:  box  [mscorlib]System.Double
      IL_004a:  stelem.ref
      IL_004b:  ldloc.s    V_4
      IL_004d:  ldc.i4.3
      IL_004e:  ldc.r8     0.80901699400000004
      IL_0057:  stloc.s    V_6
      IL_0059:  ldloc.s   V_6
      IL_005b:  box  [mscorlib]System.Double
      IL_0060:  stelem.ref
      IL_0061:  ldloc.s    V_4
      IL_0063:  ldc.i4.4
      IL_0064:  ldc.r8     0.95105651599999996
      IL_006d:  stloc.s    V_6
      IL_006f:  ldloc.s   V_6
      IL_0071:  box  [mscorlib]System.Double
      IL_0076:  stelem.ref
      IL_0077:  ldloc.s    V_4
      IL_0079:  ldc.i4.5
      IL_007a:  ldc.r8     1.
      IL_0083:  stloc.s    V_6
      IL_0085:  ldloc.s   V_6
      IL_0087:  box  [mscorlib]System.Double
      IL_008c:  stelem.ref
      IL_008d:  ldloc.s    V_4
      IL_008f:  ldc.i4.6
      IL_0090:  ldc.r8     0.95105651599999996
      IL_0099:  stloc.s    V_6
      IL_009b:  ldloc.s   V_6
      IL_009d:  box  [mscorlib]System.Double
      IL_00a2:  stelem.ref
      IL_00a3:  ldloc.s    V_4
      IL_00a5:  ldc.i4.7
      IL_00a6:  ldc.r8     0.80901699400000004
      IL_00af:  stloc.s    V_6
      IL_00b1:  ldloc.s   V_6
      IL_00b3:  box  [mscorlib]System.Double
      IL_00b8:  stelem.ref
      IL_00b9:  ldloc.s    V_4
      IL_00bb:  ldc.i4.8
      IL_00bc:  ldc.r8     0.58778525199999998
      IL_00c5:  stloc.s    V_6
      IL_00c7:  ldloc.s   V_6
      IL_00c9:  box  [mscorlib]System.Double
      IL_00ce:  stelem.ref
      IL_00cf:  ldloc.s    V_4
      IL_00d1:  ldc.i4.s   9
      IL_00d3:  ldc.r8     0.30901699399999999
      IL_00dc:  stloc.s    V_6
      IL_00de:  ldloc.s   V_6
      IL_00e0:  box  [mscorlib]System.Double
      IL_00e5:  stelem.ref
      IL_00e6:  ldc.r8     1.0000000000000001e-009
      IL_00ef:  stloc.s    V_6
      IL_00f1:  ldloc.s   V_6
      IL_00f3:  box  [mscorlib]System.Double
      IL_00f8:  stloc.s    V_5
      IL_00fa:  ldc.i4.0
      IL_00fb:  stloc.s    V_7
      IL_00fd:  ldloc.s   V_7
      IL_00ff:  box  [mscorlib]System.Int32
      IL_0104:  stloc.0
      IL_0105:  br         IL_0271

      IL_010a:  ldsfld     class System.Object [sinlib_il]SinCalcLib.SinCalcLib::PI
      IL_010f:  unbox      [sinlib_il]SinCalcLib.PiVal
      IL_0114:  ldobj      [sinlib_il]SinCalcLib.PiVal
      IL_0119:  stloc.s    V_8
      IL_011b:  ldloc.s   V_8
      IL_011d:  ldfld      float64 [sinlib_il]SinCalcLib.PiVal::Value
      IL_0122:  ldloc.0
      IL_0123:  unbox      [mscorlib]System.Int32
      IL_0128:  ldind.i4
      IL_0129:  conv.r8
      IL_012a:  ldc.r8     10.
      IL_0133:  div
      IL_0134:  mul
      IL_0135:  stloc.s    V_6
      IL_0137:  ldloc.s   V_6
      IL_0139:  box  [mscorlib]System.Double
      IL_013e:  stloc.1
      IL_013f:  ldstr      "Classlib Sin("
      IL_0144:  call       void [System.Console]System.Console::Write(class System.String)
      IL_0149:  ldloc.1
      IL_014a:  call       void [System.Console]System.Console::Write(class System.Object)
      IL_014f:  ldstr      ")="
      IL_0154:  call       void [System.Console]System.Console::Write(class System.String)
      IL_0159:  ldloc.1
      IL_015a:  unbox      [mscorlib]System.Double
      IL_015f:  ldind.r8
      IL_0160:  call       float64 [mscorlib]System.Math::Sin(float64)
      IL_0165:  stloc.s    V_6
      IL_0167:  ldloc.s   V_6
      IL_0169:  box  [mscorlib]System.Double
      IL_016e:  dup
      IL_016f:  stloc.2
      IL_0170:  call       void [System.Console]System.Console::WriteLine(class System.Object)
      IL_0175:  ldstr      "This Version("
      IL_017a:  call       void [System.Console]System.Console::Write(class System.String)
      IL_017f:  ldloc.1
      IL_0180:  call       void [System.Console]System.Console::Write(class System.Object)
      IL_0185:  ldstr      ")="
      IL_018a:  call       void [System.Console]System.Console::Write(class System.String)
      IL_018f:  ldloc.1
      IL_0190:  call       class System.Object [sinlib_il]SinCalcLib.SinCalcLib::mySin(class System.Object)
      IL_0195:  unbox      [mscorlib]System.Double
      IL_019a:  ldind.r8
      IL_019b:  stloc.s    V_6
      IL_019d:  ldloc.s   V_6
      IL_019f:  box  [mscorlib]System.Double
      IL_01a4:  dup
      IL_01a5:  stloc.3
      IL_01a6:  call       void [System.Console]System.Console::WriteLine(class System.Object)
      IL_01ab:  ldstr      "Error is:"
      IL_01b0:  call       void [System.Console]System.Console::Write(class System.String)
      IL_01b5:  ldloc.2
      IL_01b6:  unbox      [mscorlib]System.Double
      IL_01bb:  ldind.r8
      IL_01bc:  ldloc.3
      IL_01bd:  unbox      [mscorlib]System.Double
      IL_01c2:  ldind.r8
      IL_01c3:  sub
      IL_01c4:  call       void [System.Console]System.Console::WriteLine(float64)
      IL_01c9:  call       void [System.Console]System.Console::WriteLine()
      IL_01ce:  ldloc.2
      IL_01cf:  unbox      [mscorlib]System.Double
      IL_01d4:  ldind.r8
      IL_01d5:  ldloc.3
      IL_01d6:  unbox      [mscorlib]System.Double
      IL_01db:  ldind.r8
      IL_01dc:  sub
      IL_01dd:  call       float64 [mscorlib]System.Math::Abs(float64)
      IL_01e2:  ldloc.s    V_5
      IL_01e4:  unbox      [mscorlib]System.Double
      IL_01e9:  ldind.r8
      IL_01ea:  ble.un.s   IL_01f8

      IL_01ec:  ldstr      "ERROR, Versions too far apart!"
      IL_01f1:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_01f6:  ldc.i4.1
      IL_01f7:  ret

      IL_01f8:  ldloc.s    V_4
      IL_01fa:  ldloc.0
      IL_01fb:  unbox      [mscorlib]System.Int32
      IL_0200:  ldind.i4
      IL_0201:  ldelem.ref
      IL_0202:  unbox      [mscorlib]System.Double
      IL_0207:  ldind.r8
      IL_0208:  ldloc.2
      IL_0209:  unbox      [mscorlib]System.Double
      IL_020e:  ldind.r8
      IL_020f:  sub
      IL_0210:  call       float64 [mscorlib]System.Math::Abs(float64)
      IL_0215:  ldloc.s    V_5
      IL_0217:  unbox      [mscorlib]System.Double
      IL_021c:  ldind.r8
      IL_021d:  ble.un.s   IL_022b

      IL_021f:  ldstr      "ERROR, Classlib version isnt right!"
      IL_0224:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0229:  ldc.i4.1
      IL_022a:  ret

      IL_022b:  ldloc.s    V_4
      IL_022d:  ldloc.0
      IL_022e:  unbox      [mscorlib]System.Int32
      IL_0233:  ldind.i4
      IL_0234:  ldelem.ref
      IL_0235:  unbox      [mscorlib]System.Double
      IL_023a:  ldind.r8
      IL_023b:  ldloc.3
      IL_023c:  unbox      [mscorlib]System.Double
      IL_0241:  ldind.r8
      IL_0242:  sub
      IL_0243:  call       float64 [mscorlib]System.Math::Abs(float64)
      IL_0248:  ldloc.s    V_5
      IL_024a:  unbox      [mscorlib]System.Double
      IL_024f:  ldind.r8
      IL_0250:  ble.un.s   IL_025e

      IL_0252:  ldstr      "ERROR, our version isnt right!"
      IL_0257:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_025c:  ldc.i4.1
      IL_025d:  ret

      IL_025e:  ldloc.0
      IL_025f:  unbox      [mscorlib]System.Int32
      IL_0264:  ldind.i4
      IL_0265:  ldc.i4.1
      IL_0266:  add
      IL_0267:  stloc.s    V_7
      IL_0269:  ldloc.s   V_7
      IL_026b:  box  [mscorlib]System.Int32
      IL_0270:  stloc.0
      IL_0271:  ldloc.0
      IL_0272:  unbox      [mscorlib]System.Int32
      IL_0277:  ldind.i4
      IL_0278:  ldc.i4.s   10
      IL_027a:  blt        IL_010a

      IL_027f:  ldstr      "Yippie, all correct"
      IL_0284:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0289:  ldc.i4    0x64
      IL_028a:  ret
    } 

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } 

  } 

} 
