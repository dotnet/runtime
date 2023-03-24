




.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         
  .ver 4:0:0:0
}
.assembly 'rva_rvastatic4'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       
.corflags 0x00000001    



.class public auto ansi beforefieldinit IntrinsicTest
       extends [mscorlib]System.Object
{
  .field private static int32 counter at RVAInt
  .field private int32 instanceCounter
  .field private static int64 counter64 at RVALong
  .field private int64 instanceCounter64
  .field private static int32 id_counter
  .field private int32 id_instanceCounter
  .field private static int64 id_counter64
  .field private int64 id_instanceCounter64
  .field private static int64 temp
  .field private static int64 idmp
  .field private static int64 idjunk
  .method private hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldc.i4     0xcad
    IL_000c:  stfld      int32 IntrinsicTest::instanceCounter
    IL_0011:  ldarg.0
    IL_0012:  ldc.i4     0xcad
    IL_0017:  conv.i8
    IL_0018:  stfld      int64 IntrinsicTest::instanceCounter64
    IL_001d:  ldarg.0
    IL_001e:  ldc.i4     0xcad
    IL_0023:  stfld      int32 IntrinsicTest::id_instanceCounter
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4     0xcad
    IL_002e:  conv.i8
    IL_002f:  stfld      int64 IntrinsicTest::id_instanceCounter64
    IL_0034:  ret
  } 

  .method public hidebysig instance int32 
          GetValue() cil managed
  {
    .maxstack  8
    IL_0000:  ldsfld     int64 IntrinsicTest::temp
    IL_0005:  ldc.i4.1
    IL_0006:  conv.i8
    IL_0007:  add
    IL_0008:  stsfld     int64 IntrinsicTest::temp
    IL_000d:  ldc.i4     0x1ceddeed
    IL_0012:  ret
  } 

  .method public hidebysig instance int32 
          id_GetValue() cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ldsfld     int64 IntrinsicTest::idmp
    IL_0005:  ldc.i4.1
    IL_0006:  conv.i8
    IL_0007:  add
    IL_0008:  stsfld     int64 IntrinsicTest::idmp
    IL_000d:  ldc.i4     0x1ceddeed
    IL_0012:  ret
  } 

  .method public hidebysig static bool  MainTest() cil managed
  {
    .maxstack  3
    .locals init (int32 V_0,
             int32 V_1,
             int32 V_2,
             int32 V_3,
             int64 V_4,
             int64 V_5,
             int64 V_6,
             int64 V_7,
             int32 V_8,
             int64 V_9,
             int32 V_10,
             int32 V_11,
             int32 V_12,
             int32 V_13,
             int64 V_14,
             int64 V_15,
             int64 V_16,
             int64 V_17,
             int32 V_18,
             int64 V_19,
             bool V_20,
             class IntrinsicTest V_21)
    IL_0000:  ldc.i4     0xcad
    IL_0005:  stloc.0
    IL_0006:  ldc.i4     0x10e5
    IL_000b:  stloc.1
    IL_000c:  ldc.i4     0x10e5
    IL_0011:  stloc.2
    IL_0012:  ldc.i4     0x856
    IL_0017:  stloc.3
    IL_0018:  ldc.i4     0x6efa2
    IL_001d:  conv.i8
    IL_001e:  stloc.s    V_4
    IL_0020:  ldc.i4     0x54635
    IL_0025:  conv.i8
    IL_0026:  stloc.s    V_5
    IL_0028:  ldc.i4     0x54635
    IL_002d:  conv.i8
    IL_002e:  stloc.s    V_6
    IL_0030:  ldc.i4     0x545dc
    IL_0035:  conv.i8
    IL_0036:  stloc.s    V_7
    IL_0038:  ldc.i4     0x1103
    IL_003d:  stloc.s    V_8
    IL_003f:  ldc.i4     0xa03d6
    IL_0044:  conv.i8
    IL_0045:  stloc.s    V_9
    IL_0047:  ldc.i4     0xcad
    IL_004c:  stloc.s    V_10
    IL_004e:  ldc.i4     0x10e5
    IL_0053:  stloc.s    V_11
    IL_0055:  ldc.i4     0x10e5
    IL_005a:  stloc.s    V_12
    IL_005c:  ldc.i4     0x856
    IL_0061:  stloc.s    V_13
    IL_0063:  ldc.i4     0x6efa2
    IL_0068:  conv.i8
    IL_0069:  stloc.s    V_14
    IL_006b:  ldc.i4     0x54635
    IL_0070:  conv.i8
    IL_0071:  stloc.s    V_15
    IL_0073:  ldc.i4     0x54635
    IL_0078:  conv.i8
    IL_0079:  stloc.s    V_16
    IL_007b:  ldc.i4     0x545dc
    IL_0080:  conv.i8
    IL_0081:  stloc.s    V_17
    IL_0083:  ldc.i4     0x1103
    IL_0088:  stloc.s    V_18
    IL_008a:  ldc.i4     0xa03d6
    IL_008f:  conv.i8
    IL_0090:  stloc.s    V_19
    IL_0092:  ldc.i4.0
    IL_0093:  stloc.s    V_20
    IL_0095:  newobj     instance void IntrinsicTest::.ctor()
    IL_009a:  stloc.s    V_21
    IL_009c:  ldsflda    int32 IntrinsicTest::counter
    IL_00a1:  call       int32 [mscorlib]System.Threading.Interlocked::Increment(int32&)
    IL_00a6:  stsfld     int32 IntrinsicTest::counter
    IL_00ab:  ldsfld     int32 IntrinsicTest::id_counter
    IL_00b0:  ldc.i4.1
    IL_00b1:  add
    IL_00b2:  stsfld     int32 IntrinsicTest::id_counter
    IL_00b7:  ldsfld     int32 IntrinsicTest::id_counter
    IL_00bc:  stsfld     int32 IntrinsicTest::id_counter
    IL_00c1:  ldstr      "------------------------------------INC  1 1 0 1"
    IL_00c6:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00cb:  ldloc.0
    IL_00cc:  ldloc.s    V_10
    IL_00ce:  beq.s      IL_00ea

    IL_00d0:  ldc.i4.1
    IL_00d1:  stloc.s    V_20
    IL_00d3:  ldstr      "te0 check failed {0} {1}"
    IL_00d8:  ldloc.0
    IL_00d9:  box        [mscorlib]System.Int32
    IL_00de:  ldloc.s    V_10
    IL_00e0:  box        [mscorlib]System.Int32
    IL_00e5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_00ea:  ldloc.1
    IL_00eb:  ldloc.s    V_11
    IL_00ed:  beq.s      IL_0109

    IL_00ef:  ldc.i4.1
    IL_00f0:  stloc.s    V_20
    IL_00f2:  ldstr      "te1 check failed {0} {1}"
    IL_00f7:  ldloc.1
    IL_00f8:  box        [mscorlib]System.Int32
    IL_00fd:  ldloc.s    V_11
    IL_00ff:  box        [mscorlib]System.Int32
    IL_0104:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0109:  ldloc.2
    IL_010a:  ldloc.s    V_12
    IL_010c:  beq.s      IL_0128

    IL_010e:  ldc.i4.1
    IL_010f:  stloc.s    V_20
    IL_0111:  ldstr      "te2 check failed {0} {1}"
    IL_0116:  ldloc.2
    IL_0117:  box        [mscorlib]System.Int32
    IL_011c:  ldloc.s    V_12
    IL_011e:  box        [mscorlib]System.Int32
    IL_0123:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0128:  ldloc.3
    IL_0129:  ldloc.s    V_13
    IL_012b:  beq.s      IL_0147

    IL_012d:  ldc.i4.1
    IL_012e:  stloc.s    V_20
    IL_0130:  ldstr      "te3 check failed {0} {1}"
    IL_0135:  ldloc.3
    IL_0136:  box        [mscorlib]System.Int32
    IL_013b:  ldloc.s    V_13
    IL_013d:  box        [mscorlib]System.Int32
    IL_0142:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0147:  ldloc.s    V_4
    IL_0149:  ldloc.s    V_14
    IL_014b:  beq.s      IL_0168

    IL_014d:  ldc.i4.1
    IL_014e:  stloc.s    V_20
    IL_0150:  ldstr      "te064 check failed {0} {1}"
    IL_0155:  ldloc.s    V_4
    IL_0157:  box        [mscorlib]System.Int64
    IL_015c:  ldloc.s    V_14
    IL_015e:  box        [mscorlib]System.Int64
    IL_0163:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0168:  ldloc.s    V_5
    IL_016a:  ldloc.s    V_15
    IL_016c:  beq.s      IL_0189

    IL_016e:  ldc.i4.1
    IL_016f:  stloc.s    V_20
    IL_0171:  ldstr      "te164 check failed {0} {1}"
    IL_0176:  ldloc.s    V_5
    IL_0178:  box        [mscorlib]System.Int64
    IL_017d:  ldloc.s    V_15
    IL_017f:  box        [mscorlib]System.Int64
    IL_0184:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0189:  ldloc.s    V_6
    IL_018b:  ldloc.s    V_16
    IL_018d:  beq.s      IL_01aa

    IL_018f:  ldc.i4.1
    IL_0190:  stloc.s    V_20
    IL_0192:  ldstr      "te264 check failed {0} {1}"
    IL_0197:  ldloc.s    V_6
    IL_0199:  box        [mscorlib]System.Int64
    IL_019e:  ldloc.s    V_16
    IL_01a0:  box        [mscorlib]System.Int64
    IL_01a5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_01aa:  ldloc.s    V_7
    IL_01ac:  ldloc.s    V_17
    IL_01ae:  beq.s      IL_01cb

    IL_01b0:  ldc.i4.1
    IL_01b1:  stloc.s    V_20
    IL_01b3:  ldstr      "te364 check failed {0} {1}"
    IL_01b8:  ldloc.s    V_7
    IL_01ba:  box        [mscorlib]System.Int64
    IL_01bf:  ldloc.s    V_17
    IL_01c1:  box        [mscorlib]System.Int64
    IL_01c6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_01cb:  ldloc.s    V_8
    IL_01cd:  ldloc.s    V_18
    IL_01cf:  beq.s      IL_01ec

    IL_01d1:  ldc.i4.1
    IL_01d2:  stloc.s    V_20
    IL_01d4:  ldstr      "dummy check failed {0} {1}"
    IL_01d9:  ldloc.s    V_8
    IL_01db:  box        [mscorlib]System.Int32
    IL_01e0:  ldloc.s    V_18
    IL_01e2:  box        [mscorlib]System.Int32
    IL_01e7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_01ec:  ldloc.s    V_9
    IL_01ee:  ldloc.s    V_19
    IL_01f0:  beq.s      IL_020d

    IL_01f2:  ldc.i4.1
    IL_01f3:  stloc.s    V_20
    IL_01f5:  ldstr      "dummy64 check failed {0} {1}"
    IL_01fa:  ldloc.s    V_9
    IL_01fc:  box        [mscorlib]System.Int64
    IL_0201:  ldloc.s    V_19
    IL_0203:  box        [mscorlib]System.Int64
    IL_0208:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_020d:  ldsfld     int32 IntrinsicTest::counter
    IL_0212:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0217:  beq.s      IL_023a

    IL_0219:  ldstr      "counter mismatch {0} {1}"
    IL_021e:  ldsfld     int32 IntrinsicTest::counter
    IL_0223:  box        [mscorlib]System.Int32
    IL_0228:  ldsfld     int32 IntrinsicTest::id_counter
    IL_022d:  box        [mscorlib]System.Int32
    IL_0232:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0237:  ldc.i4.1
    IL_0238:  stloc.s    V_20
    IL_023a:  ldsfld     int64 IntrinsicTest::counter64
    IL_023f:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0244:  beq.s      IL_0267

    IL_0246:  ldstr      "counter64 mismatch {0} {1}"
    IL_024b:  ldsfld     int64 IntrinsicTest::counter64
    IL_0250:  box        [mscorlib]System.Int64
    IL_0255:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_025a:  box        [mscorlib]System.Int64
    IL_025f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0264:  ldc.i4.1
    IL_0265:  stloc.s    V_20
    IL_0267:  ldloc.s    V_21
    IL_0269:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_026e:  ldloc.s    V_21
    IL_0270:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_0275:  beq.s      IL_029c

    IL_0277:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_027c:  ldloc.s    V_21
    IL_027e:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_0283:  box        [mscorlib]System.Int32
    IL_0288:  ldloc.s    V_21
    IL_028a:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_028f:  box        [mscorlib]System.Int32
    IL_0294:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0299:  ldc.i4.1
    IL_029a:  stloc.s    V_20
    IL_029c:  ldloc.s    V_21
    IL_029e:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_02a3:  ldloc.s    V_21
    IL_02a5:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_02aa:  beq.s      IL_02d1

    IL_02ac:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_02b1:  ldloc.s    V_21
    IL_02b3:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_02b8:  box        [mscorlib]System.Int64
    IL_02bd:  ldloc.s    V_21
    IL_02bf:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_02c4:  box        [mscorlib]System.Int64
    IL_02c9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_02ce:  ldc.i4.1
    IL_02cf:  stloc.s    V_20
    IL_02d1:  ldsfld     int64 IntrinsicTest::temp
    IL_02d6:  ldsfld     int64 IntrinsicTest::idmp
    IL_02db:  beq.s      IL_02fe

    IL_02dd:  ldstr      "temp mismatch {0} {1}"
    IL_02e2:  ldsfld     int64 IntrinsicTest::temp
    IL_02e7:  box        [mscorlib]System.Int64
    IL_02ec:  ldsfld     int64 IntrinsicTest::idmp
    IL_02f1:  box        [mscorlib]System.Int64
    IL_02f6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_02fb:  ldc.i4.1
    IL_02fc:  stloc.s    V_20
    IL_02fe:  ldsflda    int32 IntrinsicTest::counter
    IL_0303:  call       int32 [mscorlib]System.Threading.Interlocked::Decrement(int32&)
    IL_0308:  stsfld     int32 IntrinsicTest::counter
    IL_030d:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0312:  ldc.i4.1
    IL_0313:  sub
    IL_0314:  stsfld     int32 IntrinsicTest::id_counter
    IL_0319:  ldsfld     int32 IntrinsicTest::id_counter
    IL_031e:  stsfld     int32 IntrinsicTest::id_counter
    IL_0323:  ldstr      "------------------------------------DEC  1 1 0 1"
    IL_0328:  call       void [System.Console]System.Console::WriteLine(string)
    IL_032d:  ldloc.0
    IL_032e:  ldloc.s    V_10
    IL_0330:  beq.s      IL_034c

    IL_0332:  ldc.i4.1
    IL_0333:  stloc.s    V_20
    IL_0335:  ldstr      "te0 check failed {0} {1}"
    IL_033a:  ldloc.0
    IL_033b:  box        [mscorlib]System.Int32
    IL_0340:  ldloc.s    V_10
    IL_0342:  box        [mscorlib]System.Int32
    IL_0347:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_034c:  ldloc.1
    IL_034d:  ldloc.s    V_11
    IL_034f:  beq.s      IL_036b

    IL_0351:  ldc.i4.1
    IL_0352:  stloc.s    V_20
    IL_0354:  ldstr      "te1 check failed {0} {1}"
    IL_0359:  ldloc.1
    IL_035a:  box        [mscorlib]System.Int32
    IL_035f:  ldloc.s    V_11
    IL_0361:  box        [mscorlib]System.Int32
    IL_0366:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_036b:  ldloc.2
    IL_036c:  ldloc.s    V_12
    IL_036e:  beq.s      IL_038a

    IL_0370:  ldc.i4.1
    IL_0371:  stloc.s    V_20
    IL_0373:  ldstr      "te2 check failed {0} {1}"
    IL_0378:  ldloc.2
    IL_0379:  box        [mscorlib]System.Int32
    IL_037e:  ldloc.s    V_12
    IL_0380:  box        [mscorlib]System.Int32
    IL_0385:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_038a:  ldloc.3
    IL_038b:  ldloc.s    V_13
    IL_038d:  beq.s      IL_03a9

    IL_038f:  ldc.i4.1
    IL_0390:  stloc.s    V_20
    IL_0392:  ldstr      "te3 check failed {0} {1}"
    IL_0397:  ldloc.3
    IL_0398:  box        [mscorlib]System.Int32
    IL_039d:  ldloc.s    V_13
    IL_039f:  box        [mscorlib]System.Int32
    IL_03a4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_03a9:  ldloc.s    V_4
    IL_03ab:  ldloc.s    V_14
    IL_03ad:  beq.s      IL_03ca

    IL_03af:  ldc.i4.1
    IL_03b0:  stloc.s    V_20
    IL_03b2:  ldstr      "te064 check failed {0} {1}"
    IL_03b7:  ldloc.s    V_4
    IL_03b9:  box        [mscorlib]System.Int64
    IL_03be:  ldloc.s    V_14
    IL_03c0:  box        [mscorlib]System.Int64
    IL_03c5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_03ca:  ldloc.s    V_5
    IL_03cc:  ldloc.s    V_15
    IL_03ce:  beq.s      IL_03eb

    IL_03d0:  ldc.i4.1
    IL_03d1:  stloc.s    V_20
    IL_03d3:  ldstr      "te164 check failed {0} {1}"
    IL_03d8:  ldloc.s    V_5
    IL_03da:  box        [mscorlib]System.Int64
    IL_03df:  ldloc.s    V_15
    IL_03e1:  box        [mscorlib]System.Int64
    IL_03e6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_03eb:  ldloc.s    V_6
    IL_03ed:  ldloc.s    V_16
    IL_03ef:  beq.s      IL_040c

    IL_03f1:  ldc.i4.1
    IL_03f2:  stloc.s    V_20
    IL_03f4:  ldstr      "te264 check failed {0} {1}"
    IL_03f9:  ldloc.s    V_6
    IL_03fb:  box        [mscorlib]System.Int64
    IL_0400:  ldloc.s    V_16
    IL_0402:  box        [mscorlib]System.Int64
    IL_0407:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_040c:  ldloc.s    V_7
    IL_040e:  ldloc.s    V_17
    IL_0410:  beq.s      IL_042d

    IL_0412:  ldc.i4.1
    IL_0413:  stloc.s    V_20
    IL_0415:  ldstr      "te364 check failed {0} {1}"
    IL_041a:  ldloc.s    V_7
    IL_041c:  box        [mscorlib]System.Int64
    IL_0421:  ldloc.s    V_17
    IL_0423:  box        [mscorlib]System.Int64
    IL_0428:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_042d:  ldloc.s    V_8
    IL_042f:  ldloc.s    V_18
    IL_0431:  beq.s      IL_044e

    IL_0433:  ldc.i4.1
    IL_0434:  stloc.s    V_20
    IL_0436:  ldstr      "dummy check failed {0} {1}"
    IL_043b:  ldloc.s    V_8
    IL_043d:  box        [mscorlib]System.Int32
    IL_0442:  ldloc.s    V_18
    IL_0444:  box        [mscorlib]System.Int32
    IL_0449:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_044e:  ldloc.s    V_9
    IL_0450:  ldloc.s    V_19
    IL_0452:  beq.s      IL_046f

    IL_0454:  ldc.i4.1
    IL_0455:  stloc.s    V_20
    IL_0457:  ldstr      "dummy64 check failed {0} {1}"
    IL_045c:  ldloc.s    V_9
    IL_045e:  box        [mscorlib]System.Int64
    IL_0463:  ldloc.s    V_19
    IL_0465:  box        [mscorlib]System.Int64
    IL_046a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_046f:  ldsfld     int32 IntrinsicTest::counter
    IL_0474:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0479:  beq.s      IL_049c

    IL_047b:  ldstr      "counter mismatch {0} {1}"
    IL_0480:  ldsfld     int32 IntrinsicTest::counter
    IL_0485:  box        [mscorlib]System.Int32
    IL_048a:  ldsfld     int32 IntrinsicTest::id_counter
    IL_048f:  box        [mscorlib]System.Int32
    IL_0494:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0499:  ldc.i4.1
    IL_049a:  stloc.s    V_20
    IL_049c:  ldsfld     int64 IntrinsicTest::counter64
    IL_04a1:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_04a6:  beq.s      IL_04c9

    IL_04a8:  ldstr      "counter64 mismatch {0} {1}"
    IL_04ad:  ldsfld     int64 IntrinsicTest::counter64
    IL_04b2:  box        [mscorlib]System.Int64
    IL_04b7:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_04bc:  box        [mscorlib]System.Int64
    IL_04c1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_04c6:  ldc.i4.1
    IL_04c7:  stloc.s    V_20
    IL_04c9:  ldloc.s    V_21
    IL_04cb:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_04d0:  ldloc.s    V_21
    IL_04d2:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_04d7:  beq.s      IL_04fe

    IL_04d9:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_04de:  ldloc.s    V_21
    IL_04e0:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_04e5:  box        [mscorlib]System.Int32
    IL_04ea:  ldloc.s    V_21
    IL_04ec:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_04f1:  box        [mscorlib]System.Int32
    IL_04f6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_04fb:  ldc.i4.1
    IL_04fc:  stloc.s    V_20
    IL_04fe:  ldloc.s    V_21
    IL_0500:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_0505:  ldloc.s    V_21
    IL_0507:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_050c:  beq.s      IL_0533

    IL_050e:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_0513:  ldloc.s    V_21
    IL_0515:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_051a:  box        [mscorlib]System.Int64
    IL_051f:  ldloc.s    V_21
    IL_0521:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_0526:  box        [mscorlib]System.Int64
    IL_052b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0530:  ldc.i4.1
    IL_0531:  stloc.s    V_20
    IL_0533:  ldsfld     int64 IntrinsicTest::temp
    IL_0538:  ldsfld     int64 IntrinsicTest::idmp
    IL_053d:  beq.s      IL_0560

    IL_053f:  ldstr      "temp mismatch {0} {1}"
    IL_0544:  ldsfld     int64 IntrinsicTest::temp
    IL_0549:  box        [mscorlib]System.Int64
    IL_054e:  ldsfld     int64 IntrinsicTest::idmp
    IL_0553:  box        [mscorlib]System.Int64
    IL_0558:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_055d:  ldc.i4.1
    IL_055e:  stloc.s    V_20
    IL_0560:  ldsflda    int32 IntrinsicTest::counter
    IL_0565:  ldsfld     int32 IntrinsicTest::counter
    IL_056a:  call       int32 [mscorlib]System.Threading.Interlocked::Exchange(int32&,
                                                                                int32)
    IL_056f:  stsfld     int32 IntrinsicTest::counter
    IL_0574:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0579:  stsfld     int32 IntrinsicTest::id_counter
    IL_057e:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0583:  conv.i8
    IL_0584:  stsfld     int64 IntrinsicTest::idjunk
    IL_0589:  ldstr      "------------------------------------XCHG  1 1 0 1"
    IL_058e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0593:  ldloc.0
    IL_0594:  ldloc.s    V_10
    IL_0596:  beq.s      IL_05b2

    IL_0598:  ldc.i4.1
    IL_0599:  stloc.s    V_20
    IL_059b:  ldstr      "te0 check failed {0} {1}"
    IL_05a0:  ldloc.0
    IL_05a1:  box        [mscorlib]System.Int32
    IL_05a6:  ldloc.s    V_10
    IL_05a8:  box        [mscorlib]System.Int32
    IL_05ad:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_05b2:  ldloc.1
    IL_05b3:  ldloc.s    V_11
    IL_05b5:  beq.s      IL_05d1

    IL_05b7:  ldc.i4.1
    IL_05b8:  stloc.s    V_20
    IL_05ba:  ldstr      "te1 check failed {0} {1}"
    IL_05bf:  ldloc.1
    IL_05c0:  box        [mscorlib]System.Int32
    IL_05c5:  ldloc.s    V_11
    IL_05c7:  box        [mscorlib]System.Int32
    IL_05cc:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_05d1:  ldloc.2
    IL_05d2:  ldloc.s    V_12
    IL_05d4:  beq.s      IL_05f0

    IL_05d6:  ldc.i4.1
    IL_05d7:  stloc.s    V_20
    IL_05d9:  ldstr      "te2 check failed {0} {1}"
    IL_05de:  ldloc.2
    IL_05df:  box        [mscorlib]System.Int32
    IL_05e4:  ldloc.s    V_12
    IL_05e6:  box        [mscorlib]System.Int32
    IL_05eb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_05f0:  ldloc.3
    IL_05f1:  ldloc.s    V_13
    IL_05f3:  beq.s      IL_060f

    IL_05f5:  ldc.i4.1
    IL_05f6:  stloc.s    V_20
    IL_05f8:  ldstr      "te3 check failed {0} {1}"
    IL_05fd:  ldloc.3
    IL_05fe:  box        [mscorlib]System.Int32
    IL_0603:  ldloc.s    V_13
    IL_0605:  box        [mscorlib]System.Int32
    IL_060a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_060f:  ldloc.s    V_4
    IL_0611:  ldloc.s    V_14
    IL_0613:  beq.s      IL_0630

    IL_0615:  ldc.i4.1
    IL_0616:  stloc.s    V_20
    IL_0618:  ldstr      "te064 check failed {0} {1}"
    IL_061d:  ldloc.s    V_4
    IL_061f:  box        [mscorlib]System.Int64
    IL_0624:  ldloc.s    V_14
    IL_0626:  box        [mscorlib]System.Int64
    IL_062b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0630:  ldloc.s    V_5
    IL_0632:  ldloc.s    V_15
    IL_0634:  beq.s      IL_0651

    IL_0636:  ldc.i4.1
    IL_0637:  stloc.s    V_20
    IL_0639:  ldstr      "te164 check failed {0} {1}"
    IL_063e:  ldloc.s    V_5
    IL_0640:  box        [mscorlib]System.Int64
    IL_0645:  ldloc.s    V_15
    IL_0647:  box        [mscorlib]System.Int64
    IL_064c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0651:  ldloc.s    V_6
    IL_0653:  ldloc.s    V_16
    IL_0655:  beq.s      IL_0672

    IL_0657:  ldc.i4.1
    IL_0658:  stloc.s    V_20
    IL_065a:  ldstr      "te264 check failed {0} {1}"
    IL_065f:  ldloc.s    V_6
    IL_0661:  box        [mscorlib]System.Int64
    IL_0666:  ldloc.s    V_16
    IL_0668:  box        [mscorlib]System.Int64
    IL_066d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0672:  ldloc.s    V_7
    IL_0674:  ldloc.s    V_17
    IL_0676:  beq.s      IL_0693

    IL_0678:  ldc.i4.1
    IL_0679:  stloc.s    V_20
    IL_067b:  ldstr      "te364 check failed {0} {1}"
    IL_0680:  ldloc.s    V_7
    IL_0682:  box        [mscorlib]System.Int64
    IL_0687:  ldloc.s    V_17
    IL_0689:  box        [mscorlib]System.Int64
    IL_068e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0693:  ldloc.s    V_8
    IL_0695:  ldloc.s    V_18
    IL_0697:  beq.s      IL_06b4

    IL_0699:  ldc.i4.1
    IL_069a:  stloc.s    V_20
    IL_069c:  ldstr      "dummy check failed {0} {1}"
    IL_06a1:  ldloc.s    V_8
    IL_06a3:  box        [mscorlib]System.Int32
    IL_06a8:  ldloc.s    V_18
    IL_06aa:  box        [mscorlib]System.Int32
    IL_06af:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_06b4:  ldloc.s    V_9
    IL_06b6:  ldloc.s    V_19
    IL_06b8:  beq.s      IL_06d5

    IL_06ba:  ldc.i4.1
    IL_06bb:  stloc.s    V_20
    IL_06bd:  ldstr      "dummy64 check failed {0} {1}"
    IL_06c2:  ldloc.s    V_9
    IL_06c4:  box        [mscorlib]System.Int64
    IL_06c9:  ldloc.s    V_19
    IL_06cb:  box        [mscorlib]System.Int64
    IL_06d0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_06d5:  ldsfld     int32 IntrinsicTest::counter
    IL_06da:  ldsfld     int32 IntrinsicTest::id_counter
    IL_06df:  beq.s      IL_0702

    IL_06e1:  ldstr      "counter mismatch {0} {1}"
    IL_06e6:  ldsfld     int32 IntrinsicTest::counter
    IL_06eb:  box        [mscorlib]System.Int32
    IL_06f0:  ldsfld     int32 IntrinsicTest::id_counter
    IL_06f5:  box        [mscorlib]System.Int32
    IL_06fa:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_06ff:  ldc.i4.1
    IL_0700:  stloc.s    V_20
    IL_0702:  ldsfld     int64 IntrinsicTest::counter64
    IL_0707:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_070c:  beq.s      IL_072f

    IL_070e:  ldstr      "counter64 mismatch {0} {1}"
    IL_0713:  ldsfld     int64 IntrinsicTest::counter64
    IL_0718:  box        [mscorlib]System.Int64
    IL_071d:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0722:  box        [mscorlib]System.Int64
    IL_0727:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_072c:  ldc.i4.1
    IL_072d:  stloc.s    V_20
    IL_072f:  ldloc.s    V_21
    IL_0731:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_0736:  ldloc.s    V_21
    IL_0738:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_073d:  beq.s      IL_0764

    IL_073f:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_0744:  ldloc.s    V_21
    IL_0746:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_074b:  box        [mscorlib]System.Int32
    IL_0750:  ldloc.s    V_21
    IL_0752:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_0757:  box        [mscorlib]System.Int32
    IL_075c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0761:  ldc.i4.1
    IL_0762:  stloc.s    V_20
    IL_0764:  ldloc.s    V_21
    IL_0766:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_076b:  ldloc.s    V_21
    IL_076d:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_0772:  beq.s      IL_0799

    IL_0774:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_0779:  ldloc.s    V_21
    IL_077b:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_0780:  box        [mscorlib]System.Int64
    IL_0785:  ldloc.s    V_21
    IL_0787:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_078c:  box        [mscorlib]System.Int64
    IL_0791:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0796:  ldc.i4.1
    IL_0797:  stloc.s    V_20
    IL_0799:  ldsfld     int64 IntrinsicTest::temp
    IL_079e:  ldsfld     int64 IntrinsicTest::idmp
    IL_07a3:  beq.s      IL_07c6

    IL_07a5:  ldstr      "temp mismatch {0} {1}"
    IL_07aa:  ldsfld     int64 IntrinsicTest::temp
    IL_07af:  box        [mscorlib]System.Int64
    IL_07b4:  ldsfld     int64 IntrinsicTest::idmp
    IL_07b9:  box        [mscorlib]System.Int64
    IL_07be:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_07c3:  ldc.i4.1
    IL_07c4:  stloc.s    V_20
    IL_07c6:  ldsflda    int32 IntrinsicTest::counter
    IL_07cb:  ldsfld     int32 IntrinsicTest::counter
    IL_07d0:  call       int32 [mscorlib]System.Threading.Interlocked::Add(int32&,
                                                                           int32)
    IL_07d5:  stsfld     int32 IntrinsicTest::counter
    IL_07da:  ldsfld     int32 IntrinsicTest::id_counter
    IL_07df:  ldsfld     int32 IntrinsicTest::id_counter
    IL_07e4:  add
    IL_07e5:  stsfld     int32 IntrinsicTest::id_counter
    IL_07ea:  ldsfld     int32 IntrinsicTest::id_counter
    IL_07ef:  stsfld     int32 IntrinsicTest::id_counter
    IL_07f4:  ldstr      "------------------------------------XADD  1 1 0 1"
    IL_07f9:  call       void [System.Console]System.Console::WriteLine(string)
    IL_07fe:  ldloc.0
    IL_07ff:  ldloc.s    V_10
    IL_0801:  beq.s      IL_081d

    IL_0803:  ldc.i4.1
    IL_0804:  stloc.s    V_20
    IL_0806:  ldstr      "te0 check failed {0} {1}"
    IL_080b:  ldloc.0
    IL_080c:  box        [mscorlib]System.Int32
    IL_0811:  ldloc.s    V_10
    IL_0813:  box        [mscorlib]System.Int32
    IL_0818:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_081d:  ldloc.1
    IL_081e:  ldloc.s    V_11
    IL_0820:  beq.s      IL_083c

    IL_0822:  ldc.i4.1
    IL_0823:  stloc.s    V_20
    IL_0825:  ldstr      "te1 check failed {0} {1}"
    IL_082a:  ldloc.1
    IL_082b:  box        [mscorlib]System.Int32
    IL_0830:  ldloc.s    V_11
    IL_0832:  box        [mscorlib]System.Int32
    IL_0837:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_083c:  ldloc.2
    IL_083d:  ldloc.s    V_12
    IL_083f:  beq.s      IL_085b

    IL_0841:  ldc.i4.1
    IL_0842:  stloc.s    V_20
    IL_0844:  ldstr      "te2 check failed {0} {1}"
    IL_0849:  ldloc.2
    IL_084a:  box        [mscorlib]System.Int32
    IL_084f:  ldloc.s    V_12
    IL_0851:  box        [mscorlib]System.Int32
    IL_0856:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_085b:  ldloc.3
    IL_085c:  ldloc.s    V_13
    IL_085e:  beq.s      IL_087a

    IL_0860:  ldc.i4.1
    IL_0861:  stloc.s    V_20
    IL_0863:  ldstr      "te3 check failed {0} {1}"
    IL_0868:  ldloc.3
    IL_0869:  box        [mscorlib]System.Int32
    IL_086e:  ldloc.s    V_13
    IL_0870:  box        [mscorlib]System.Int32
    IL_0875:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_087a:  ldloc.s    V_4
    IL_087c:  ldloc.s    V_14
    IL_087e:  beq.s      IL_089b

    IL_0880:  ldc.i4.1
    IL_0881:  stloc.s    V_20
    IL_0883:  ldstr      "te064 check failed {0} {1}"
    IL_0888:  ldloc.s    V_4
    IL_088a:  box        [mscorlib]System.Int64
    IL_088f:  ldloc.s    V_14
    IL_0891:  box        [mscorlib]System.Int64
    IL_0896:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_089b:  ldloc.s    V_5
    IL_089d:  ldloc.s    V_15
    IL_089f:  beq.s      IL_08bc

    IL_08a1:  ldc.i4.1
    IL_08a2:  stloc.s    V_20
    IL_08a4:  ldstr      "te164 check failed {0} {1}"
    IL_08a9:  ldloc.s    V_5
    IL_08ab:  box        [mscorlib]System.Int64
    IL_08b0:  ldloc.s    V_15
    IL_08b2:  box        [mscorlib]System.Int64
    IL_08b7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_08bc:  ldloc.s    V_6
    IL_08be:  ldloc.s    V_16
    IL_08c0:  beq.s      IL_08dd

    IL_08c2:  ldc.i4.1
    IL_08c3:  stloc.s    V_20
    IL_08c5:  ldstr      "te264 check failed {0} {1}"
    IL_08ca:  ldloc.s    V_6
    IL_08cc:  box        [mscorlib]System.Int64
    IL_08d1:  ldloc.s    V_16
    IL_08d3:  box        [mscorlib]System.Int64
    IL_08d8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_08dd:  ldloc.s    V_7
    IL_08df:  ldloc.s    V_17
    IL_08e1:  beq.s      IL_08fe

    IL_08e3:  ldc.i4.1
    IL_08e4:  stloc.s    V_20
    IL_08e6:  ldstr      "te364 check failed {0} {1}"
    IL_08eb:  ldloc.s    V_7
    IL_08ed:  box        [mscorlib]System.Int64
    IL_08f2:  ldloc.s    V_17
    IL_08f4:  box        [mscorlib]System.Int64
    IL_08f9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_08fe:  ldloc.s    V_8
    IL_0900:  ldloc.s    V_18
    IL_0902:  beq.s      IL_091f

    IL_0904:  ldc.i4.1
    IL_0905:  stloc.s    V_20
    IL_0907:  ldstr      "dummy check failed {0} {1}"
    IL_090c:  ldloc.s    V_8
    IL_090e:  box        [mscorlib]System.Int32
    IL_0913:  ldloc.s    V_18
    IL_0915:  box        [mscorlib]System.Int32
    IL_091a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_091f:  ldloc.s    V_9
    IL_0921:  ldloc.s    V_19
    IL_0923:  beq.s      IL_0940

    IL_0925:  ldc.i4.1
    IL_0926:  stloc.s    V_20
    IL_0928:  ldstr      "dummy64 check failed {0} {1}"
    IL_092d:  ldloc.s    V_9
    IL_092f:  box        [mscorlib]System.Int64
    IL_0934:  ldloc.s    V_19
    IL_0936:  box        [mscorlib]System.Int64
    IL_093b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0940:  ldsfld     int32 IntrinsicTest::counter
    IL_0945:  ldsfld     int32 IntrinsicTest::id_counter
    IL_094a:  beq.s      IL_096d

    IL_094c:  ldstr      "counter mismatch {0} {1}"
    IL_0951:  ldsfld     int32 IntrinsicTest::counter
    IL_0956:  box        [mscorlib]System.Int32
    IL_095b:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0960:  box        [mscorlib]System.Int32
    IL_0965:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_096a:  ldc.i4.1
    IL_096b:  stloc.s    V_20
    IL_096d:  ldsfld     int64 IntrinsicTest::counter64
    IL_0972:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0977:  beq.s      IL_099a

    IL_0979:  ldstr      "counter64 mismatch {0} {1}"
    IL_097e:  ldsfld     int64 IntrinsicTest::counter64
    IL_0983:  box        [mscorlib]System.Int64
    IL_0988:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_098d:  box        [mscorlib]System.Int64
    IL_0992:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0997:  ldc.i4.1
    IL_0998:  stloc.s    V_20
    IL_099a:  ldloc.s    V_21
    IL_099c:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_09a1:  ldloc.s    V_21
    IL_09a3:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_09a8:  beq.s      IL_09cf

    IL_09aa:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_09af:  ldloc.s    V_21
    IL_09b1:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_09b6:  box        [mscorlib]System.Int32
    IL_09bb:  ldloc.s    V_21
    IL_09bd:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_09c2:  box        [mscorlib]System.Int32
    IL_09c7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_09cc:  ldc.i4.1
    IL_09cd:  stloc.s    V_20
    IL_09cf:  ldloc.s    V_21
    IL_09d1:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_09d6:  ldloc.s    V_21
    IL_09d8:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_09dd:  beq.s      IL_0a04

    IL_09df:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_09e4:  ldloc.s    V_21
    IL_09e6:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_09eb:  box        [mscorlib]System.Int64
    IL_09f0:  ldloc.s    V_21
    IL_09f2:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_09f7:  box        [mscorlib]System.Int64
    IL_09fc:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0a01:  ldc.i4.1
    IL_0a02:  stloc.s    V_20
    IL_0a04:  ldsfld     int64 IntrinsicTest::temp
    IL_0a09:  ldsfld     int64 IntrinsicTest::idmp
    IL_0a0e:  beq.s      IL_0a31

    IL_0a10:  ldstr      "temp mismatch {0} {1}"
    IL_0a15:  ldsfld     int64 IntrinsicTest::temp
    IL_0a1a:  box        [mscorlib]System.Int64
    IL_0a1f:  ldsfld     int64 IntrinsicTest::idmp
    IL_0a24:  box        [mscorlib]System.Int64
    IL_0a29:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0a2e:  ldc.i4.1
    IL_0a2f:  stloc.s    V_20
    IL_0a31:  ldloc.s    V_20
    IL_0a33:  ret
  } 

  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  8
    IL_0000:  call       bool IntrinsicTest::MainTest()
    IL_0005:  brfalse.s  IL_0014

    IL_0007:  ldstr      "Test Failed"
    IL_000c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0011:  ldc.i4.s   101
    IL_0013:  ret

    IL_0014:  ldstr      "Test Passed"
    IL_0019:  call       void [System.Console]System.Console::WriteLine(string)
    IL_001e:  ldc.i4.s   100
    IL_0020:  ret
  } 

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    .maxstack  1
    IL_0000:  ldc.i4     0xcad
    IL_0005:  stsfld     int32 IntrinsicTest::counter
    IL_000a:  ldc.i4     0xcad
    IL_000f:  conv.i8
    IL_0010:  stsfld     int64 IntrinsicTest::counter64
    IL_0015:  ldc.i4     0xcad
    IL_001a:  stsfld     int32 IntrinsicTest::id_counter
    IL_001f:  ldc.i4     0xcad
    IL_0024:  conv.i8
    IL_0025:  stsfld     int64 IntrinsicTest::id_counter64
    IL_002a:  ldc.i4     0x457
    IL_002f:  conv.i8
    IL_0030:  stsfld     int64 IntrinsicTest::temp
    IL_0035:  ldc.i4     0x457
    IL_003a:  conv.i8
    IL_003b:  stsfld     int64 IntrinsicTest::idmp
    IL_0040:  ldc.i4.0
    IL_0041:  conv.i8
    IL_0042:  stsfld     int64 IntrinsicTest::idjunk
    IL_0047:  ret
  } 

} 

 .data bytearray1 = bytearray ( 0 0 0 0 ) 

 .data RVAInt = int32 (3245) 

 .data RVALong = int64 (3245)



