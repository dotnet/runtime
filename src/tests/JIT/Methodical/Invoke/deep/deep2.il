// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'deep2'// as "deep"
{
}
.assembly extern xunit.core {}
// MVID: {AA806490-3696-4693-BEEE-68E449C58639}
.namespace JitTest_deep2_deep_il
{
  .class public auto ansi Test
         extends ['mscorlib']System.Object
  {
    .method private hidebysig static float64[] 
            Method(float64 arg1,
                   float32 arg2,
                   float64& refarg) il managed
    {
      // Code size       173 (0xad)
      .maxstack  7
      .locals (float64[] V_0,
               float64[] V_1)
      IL_0000:  ldc.i4.4
      IL_0001:  newarr     ['mscorlib']System.Double
      IL_0006:  stloc.0
      IL_0007:  ldarg.0
      IL_0008:  ldc.r8     11.
      IL_0011:  bge.un     IL_0099

      IL_0016:  ldloc.0
      IL_0017:  ldc.i4.0
      IL_0018:  ldarg.0
      IL_0019:  ldarg.0
      IL_001a:  ldc.r8     1.
      IL_0023:  add
      IL_0024:  ldarg.1
      IL_0025:  ldarg.2
      IL_0026:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                          float32,
                                                          float64&)
      IL_002b:  ldc.i4.0
      IL_002c:  ldelem.r8
      IL_002d:  add
      IL_002e:  stelem.r8
      IL_002f:  ldloc.0
      IL_0030:  ldc.i4.1
      IL_0031:  ldarg.1
      IL_0032:  conv.r8
      IL_0033:  ldarg.0
      IL_0034:  ldc.r8     2.
      IL_003d:  add
      IL_003e:  ldarg.1
      IL_003f:  ldloc.0
      IL_0040:  ldc.i4.0
      IL_0041:  ldelema    ['mscorlib']System.Double
      IL_0046:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                          float32,
                                                          float64&)
      IL_004b:  ldc.i4.1
      IL_004c:  ldelem.r8
      IL_004d:  add
      IL_004e:  stelem.r8
      IL_004f:  ldloc.0
      IL_0050:  ldc.i4.2
      IL_0051:  ldarg.0
      IL_0052:  ldarg.1
      IL_0053:  conv.r8
      IL_0054:  add
      IL_0055:  ldarg.0
      IL_0056:  ldc.r8     3.
      IL_005f:  add
      IL_0060:  ldarg.1
      IL_0061:  ldloc.0
      IL_0062:  ldc.i4.1
      IL_0063:  ldelema    ['mscorlib']System.Double
      IL_0068:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                          float32,
                                                          float64&)
      IL_006d:  ldc.i4.2
      IL_006e:  ldelem.r8
      IL_006f:  add
      IL_0070:  stelem.r8
      IL_0071:  ldloc.0
      IL_0072:  ldc.i4.3
      IL_0073:  ldarg.0
      IL_0074:  ldarg.1
      IL_0075:  ldc.r4     2.
      IL_007a:  mul
      IL_007b:  conv.r8
      IL_007c:  add
      IL_007d:  ldarg.0
      IL_007e:  ldc.r8     4.
      IL_0087:  add
      IL_0088:  ldarg.1
      IL_0089:  ldloc.0
      IL_008a:  ldc.i4.2
      IL_008b:  ldelema    ['mscorlib']System.Double
      IL_0090:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                          float32,
                                                          float64&)
      IL_0095:  ldc.i4.3
      IL_0096:  ldelem.r8
      IL_0097:  add
      IL_0098:  stelem.r8
      IL_0099:  ldarg.2
      IL_009a:  dup
      IL_009b:  ldind.r8
      IL_009c:  ldc.r8     1.
      IL_00a5:  add
      IL_00a6:  stind.r8
      IL_00a7:  ldloc.0
      IL_00a8:  stloc.1
      IL_00a9:  br.s       IL_00ab

      IL_00ab:  ldloc.1
      IL_00ac:  ret
    } // end of method 'Test::Method'

    .method private hidebysig static float64 
            Method2(float64 arg1,
                    int32 arg2,
                    float64 refarg) il managed
    {
      // Code size       146 (0x92)
      .maxstack  7
      .locals (float64[] V_0,
               float64 V_1)
      IL_0000:  ldc.i4.4
      IL_0001:  newarr     ['mscorlib']System.Double
      IL_0006:  stloc.0
      IL_0007:  ldarg.0
      IL_0008:  ldc.r8     11.
      IL_0011:  bge.un.s   IL_007e

      IL_0013:  ldloc.0
      IL_0014:  ldc.i4.0
      IL_0015:  ldarg.0
      IL_0016:  ldarg.0
      IL_0017:  ldc.r8     1.
      IL_0020:  add
      IL_0021:  ldarg.1
      IL_0022:  ldarg.2
      IL_0023:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                         int32,
                                                         float64)
      IL_0028:  add
      IL_0029:  stelem.r8
      IL_002a:  ldloc.0
      IL_002b:  ldc.i4.1
      IL_002c:  ldarg.1
      IL_002d:  conv.r8
      IL_002e:  ldarg.0
      IL_002f:  ldc.r8     2.
      IL_0038:  add
      IL_0039:  ldarg.1
      IL_003a:  ldloc.0
      IL_003b:  ldc.i4.0
      IL_003c:  ldelem.r8
      IL_003d:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                         int32,
                                                         float64)
      IL_0042:  add
      IL_0043:  stelem.r8
      IL_0044:  ldloc.0
      IL_0045:  ldc.i4.2
      IL_0046:  ldarg.0
      IL_0047:  ldarg.1
      IL_0048:  conv.r8
      IL_0049:  add
      IL_004a:  ldarg.0
      IL_004b:  ldc.r8     3.
      IL_0054:  add
      IL_0055:  ldarg.1
      IL_0056:  ldloc.0
      IL_0057:  ldc.i4.1
      IL_0058:  ldelem.r8
      IL_0059:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                         int32,
                                                         float64)
      IL_005e:  add
      IL_005f:  stelem.r8
      IL_0060:  ldloc.0
      IL_0061:  ldc.i4.3
      IL_0062:  ldarg.0
      IL_0063:  ldarg.1
      IL_0064:  ldc.i4.2
      IL_0065:  mul
      IL_0066:  conv.r8
      IL_0067:  add
      IL_0068:  ldarg.0
      IL_0069:  ldc.r8     4.
      IL_0072:  add
      IL_0073:  ldarg.1
      IL_0074:  ldloc.0
      IL_0075:  ldc.i4.2
      IL_0076:  ldelem.r8
      IL_0077:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                         int32,
                                                         float64)
      IL_007c:  add
      IL_007d:  stelem.r8
      IL_007e:  ldloc.0
      IL_007f:  ldc.i4.0
      IL_0080:  ldelem.r8
      IL_0081:  ldloc.0
      IL_0082:  ldc.i4.1
      IL_0083:  ldelem.r8
      IL_0084:  add
      IL_0085:  ldloc.0
      IL_0086:  ldc.i4.2
      IL_0087:  ldelem.r8
      IL_0088:  add
      IL_0089:  ldloc.0
      IL_008a:  ldc.i4.3
      IL_008b:  ldelem.r8
      IL_008c:  add
      IL_008d:  stloc.1
      IL_008e:  br.s       IL_0090

      IL_0090:  ldloc.1
      IL_0091:  ret
    } // end of method 'Test::Method2'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       958 (0x3be)
      .maxstack  45
      .locals (float64 V_0,
               int32 V_1)
      .try
      {
        IL_0000:  ldc.r8     -1.
        IL_0009:  stloc.0
        IL_000a:  ldc.r8     0.
        IL_0013:  ldc.r4     1.
        IL_0018:  ldloca.s   V_0
        IL_001a:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                            float32,
                                                            float64&)
        IL_001f:  ldc.i4.3
        IL_0020:  ldelem.r8
        IL_0021:  ldc.r8     18.
        IL_002a:  beq.s      IL_0032

        IL_002c:  newobj     instance void ['mscorlib']System.Exception::.ctor()
        IL_0031:  throw

        IL_0032:  ldc.r8     0.
        IL_003b:  ldc.r4     0.
        IL_0040:  ldc.r8     1.
        IL_0049:  ldc.r4     0.
        IL_004e:  ldc.r8     2.
        IL_0057:  ldc.r4     0.
        IL_005c:  ldc.r8     3.
        IL_0065:  ldc.r4     0.
        IL_006a:  ldc.r8     4.
        IL_0073:  ldc.r4     0.
        IL_0078:  ldc.r8     5.
        IL_0081:  ldc.r4     0.
        IL_0086:  ldc.r8     6.
        IL_008f:  ldc.r4     0.
        IL_0094:  ldc.r8     7.
        IL_009d:  ldc.r4     0.
        IL_00a2:  ldc.r8     8.
        IL_00ab:  ldc.r4     0.
        IL_00b0:  ldc.r8     9.
        IL_00b9:  ldc.r4     0.
        IL_00be:  ldc.r8     0.
        IL_00c7:  ldc.r4     1.
        IL_00cc:  ldc.r8     0.
        IL_00d5:  ldc.r4     2.
        IL_00da:  ldc.r8     0.
        IL_00e3:  ldc.r4     3.
        IL_00e8:  ldc.r8     0.
        IL_00f1:  ldc.r4     4.
        IL_00f6:  ldc.r8     0.
        IL_00ff:  ldc.r4     5.
        IL_0104:  ldc.r8     0.
        IL_010d:  ldc.r4     6.
        IL_0112:  ldc.r8     0.
        IL_011b:  ldc.r4     7.
        IL_0120:  ldc.r8     0.
        IL_0129:  ldc.r4     8.
        IL_012e:  ldc.r8     0.
        IL_0137:  ldc.r4     9.
        IL_013c:  ldc.r8     0.
        IL_0145:  ldc.r4     10.
        IL_014a:  ldc.r8     0.
        IL_0153:  ldc.r4     11.
        IL_0158:  ldc.r8     0.
        IL_0161:  ldc.r4     12.
        IL_0166:  ldloca.s   V_0
        IL_0168:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                            float32,
                                                            float64&)
        IL_016d:  ldc.i4.1
        IL_016e:  ldelema    ['mscorlib']System.Double
        IL_0173:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                            float32,
                                                            float64&)
        IL_0178:  ldc.i4.2
        IL_0179:  ldelema    ['mscorlib']System.Double
        IL_017e:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                            float32,
                                                            float64&)
        IL_0183:  ldc.i4.3
        IL_0184:  ldelema    ['mscorlib']System.Double
        IL_0189:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                            float32,
                                                            float64&)
        IL_018e:  ldc.i4.0
        IL_018f:  ldelema    ['mscorlib']System.Double
        IL_0194:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                            float32,
                                                            float64&)
        IL_0199:  ldc.i4.1
        IL_019a:  ldelema    ['mscorlib']System.Double
        IL_019f:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                            float32,
                                                            float64&)
        IL_01a4:  ldc.i4.2
        IL_01a5:  ldelema    ['mscorlib']System.Double
        IL_01aa:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                            float32,
                                                            float64&)
        IL_01af:  ldc.i4.3
        IL_01b0:  ldelema    ['mscorlib']System.Double
        IL_01b5:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                            float32,
                                                            float64&)
        IL_01ba:  ldc.i4.2
        IL_01bb:  ldelema    ['mscorlib']System.Double
        IL_01c0:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                            float32,
                                                            float64&)
        IL_01c5:  ldc.i4.1
        IL_01c6:  ldelema    ['mscorlib']System.Double
        IL_01cb:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                            float32,
                                                            float64&)
        IL_01d0:  ldc.i4.0
        IL_01d1:  ldelema    ['mscorlib']System.Double
        IL_01d6:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                            float32,
                                                            float64&)
        IL_01db:  ldc.i4.0
        IL_01dc:  ldelema    ['mscorlib']System.Double
        IL_01e1:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                            float32,
                                                            float64&)
        IL_01e6:  ldc.i4.1
        IL_01e7:  ldelema    ['mscorlib']System.Double
        IL_01ec:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                            float32,
                                                            float64&)
        IL_01f1:  ldc.i4.2
        IL_01f2:  ldelema    ['mscorlib']System.Double
        IL_01f7:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                            float32,
                                                            float64&)
        IL_01fc:  ldc.i4.2
        IL_01fd:  ldelema    ['mscorlib']System.Double
        IL_0202:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                            float32,
                                                            float64&)
        IL_0207:  ldc.i4.3
        IL_0208:  ldelema    ['mscorlib']System.Double
        IL_020d:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                            float32,
                                                            float64&)
        IL_0212:  ldc.i4.2
        IL_0213:  ldelema    ['mscorlib']System.Double
        IL_0218:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                            float32,
                                                            float64&)
        IL_021d:  ldc.i4.1
        IL_021e:  ldelema    ['mscorlib']System.Double
        IL_0223:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                            float32,
                                                            float64&)
        IL_0228:  ldc.i4.0
        IL_0229:  ldelema    ['mscorlib']System.Double
        IL_022e:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                            float32,
                                                            float64&)
        IL_0233:  ldc.i4.1
        IL_0234:  ldelema    ['mscorlib']System.Double
        IL_0239:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                            float32,
                                                            float64&)
        IL_023e:  ldc.i4.1
        IL_023f:  ldelema    ['mscorlib']System.Double
        IL_0244:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                            float32,
                                                            float64&)
        IL_0249:  ldc.i4.1
        IL_024a:  ldelema    ['mscorlib']System.Double
        IL_024f:  call       float64[] JitTest_deep2_deep_il.Test::Method(float64,
                                                            float32,
                                                            float64&)
        IL_0254:  ldc.i4.1
        IL_0255:  ldelem.r8
        IL_0256:  ldc.i4.0
        IL_0257:  ldc.r8     1.
        IL_0260:  ldc.i4.0
        IL_0261:  ldc.r8     2.
        IL_026a:  ldc.i4.0
        IL_026b:  ldc.r8     3.
        IL_0274:  ldc.i4.0
        IL_0275:  ldc.r8     4.
        IL_027e:  ldc.i4.0
        IL_027f:  ldc.r8     5.
        IL_0288:  ldc.i4.0
        IL_0289:  ldc.r8     6.
        IL_0292:  ldc.i4.0
        IL_0293:  ldc.r8     7.
        IL_029c:  ldc.i4.0
        IL_029d:  ldc.r8     8.
        IL_02a6:  ldc.i4.0
        IL_02a7:  ldc.r8     9.
        IL_02b0:  ldc.i4.0
        IL_02b1:  ldc.r8     0.
        IL_02ba:  ldc.i4.1
        IL_02bb:  ldc.r8     0.
        IL_02c4:  ldc.i4.2
        IL_02c5:  ldc.r8     0.
        IL_02ce:  ldc.i4.3
        IL_02cf:  ldc.r8     0.
        IL_02d8:  ldc.i4.4
        IL_02d9:  ldc.r8     0.
        IL_02e2:  ldc.i4.5
        IL_02e3:  ldc.r8     0.
        IL_02ec:  ldc.i4.6
        IL_02ed:  ldc.r8     0.
        IL_02f6:  ldc.i4.7
        IL_02f7:  ldc.r8     0.
        IL_0300:  ldc.i4.8
        IL_0301:  ldc.r8     0.
        IL_030a:  ldc.i4.s   9
        IL_030c:  ldc.r8     0.
        IL_0315:  ldc.i4.s   10
        IL_0317:  ldc.r8     0.
        IL_0320:  ldc.i4.s   11
        IL_0322:  ldc.r8     0.
        IL_032b:  ldc.i4.s   12
        IL_032d:  ldloc.0
        IL_032e:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                           int32,
                                                           float64)
        IL_0333:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                           int32,
                                                           float64)
        IL_0338:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                           int32,
                                                           float64)
        IL_033d:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                           int32,
                                                           float64)
        IL_0342:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                           int32,
                                                           float64)
        IL_0347:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                           int32,
                                                           float64)
        IL_034c:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                           int32,
                                                           float64)
        IL_0351:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                           int32,
                                                           float64)
        IL_0356:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                           int32,
                                                           float64)
        IL_035b:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                           int32,
                                                           float64)
        IL_0360:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                           int32,
                                                           float64)
        IL_0365:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                           int32,
                                                           float64)
        IL_036a:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                           int32,
                                                           float64)
        IL_036f:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                           int32,
                                                           float64)
        IL_0374:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                           int32,
                                                           float64)
        IL_0379:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                           int32,
                                                           float64)
        IL_037e:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                           int32,
                                                           float64)
        IL_0383:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                           int32,
                                                           float64)
        IL_0388:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                           int32,
                                                           float64)
        IL_038d:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                           int32,
                                                           float64)
        IL_0392:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                           int32,
                                                           float64)
        IL_0397:  call       float64 JitTest_deep2_deep_il.Test::Method2(float64,
                                                           int32,
                                                           float64)
        IL_039c:  stloc.0
        IL_039d:  leave.s    IL_03ae

      }  // end .try
      catch ['mscorlib']System.Object 
      {
        IL_039f:  pop
        IL_03a0:  ldstr      "Failed w/ exception"
        IL_03a5:  call       void [System.Console]System.Console::WriteLine(class System.String)
        IL_03aa:  ldc.i4.1
        IL_03ab:  stloc.1
        IL_03ac:  leave.s    IL_03bc

      }  // end handler
      IL_03ae:  ldstr      "Passed"
      IL_03b3:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_03b8:  ldc.i4    0x64
      IL_03b9:  stloc.1
      IL_03ba:  br.s       IL_03bc

      IL_03bc:  ldloc.1
      IL_03bd:  ret
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Test::.ctor'

  } // end of class 'Test'

} // end of namespace 'JitTest_deep2_deep_il'

//*********** DISASSEMBLY COMPLETE ***********************
