




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
.assembly 'rva_rvastatic3'
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
    IL_009c:  ldsflda    int64 IntrinsicTest::counter64
    IL_00a1:  call       int64 [mscorlib]System.Threading.Interlocked::Increment(int64&)
    IL_00a6:  stsfld     int64 IntrinsicTest::counter64
    IL_00ab:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_00b0:  ldc.i4.1
    IL_00b1:  conv.i8
    IL_00b2:  add
    IL_00b3:  stsfld     int64 IntrinsicTest::id_counter64
    IL_00b8:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_00bd:  stsfld     int64 IntrinsicTest::id_counter64
    IL_00c2:  ldstr      "------------------------------------INC 64 1 1 0 1"
    IL_00c7:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00cc:  ldloc.0
    IL_00cd:  ldloc.s    V_10
    IL_00cf:  beq.s      IL_00eb

    IL_00d1:  ldc.i4.1
    IL_00d2:  stloc.s    V_20
    IL_00d4:  ldstr      "te0 check failed {0} {1}"
    IL_00d9:  ldloc.0
    IL_00da:  box        [mscorlib]System.Int32
    IL_00df:  ldloc.s    V_10
    IL_00e1:  box        [mscorlib]System.Int32
    IL_00e6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_00eb:  ldloc.1
    IL_00ec:  ldloc.s    V_11
    IL_00ee:  beq.s      IL_010a

    IL_00f0:  ldc.i4.1
    IL_00f1:  stloc.s    V_20
    IL_00f3:  ldstr      "te1 check failed {0} {1}"
    IL_00f8:  ldloc.1
    IL_00f9:  box        [mscorlib]System.Int32
    IL_00fe:  ldloc.s    V_11
    IL_0100:  box        [mscorlib]System.Int32
    IL_0105:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_010a:  ldloc.2
    IL_010b:  ldloc.s    V_12
    IL_010d:  beq.s      IL_0129

    IL_010f:  ldc.i4.1
    IL_0110:  stloc.s    V_20
    IL_0112:  ldstr      "te2 check failed {0} {1}"
    IL_0117:  ldloc.2
    IL_0118:  box        [mscorlib]System.Int32
    IL_011d:  ldloc.s    V_12
    IL_011f:  box        [mscorlib]System.Int32
    IL_0124:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0129:  ldloc.3
    IL_012a:  ldloc.s    V_13
    IL_012c:  beq.s      IL_0148

    IL_012e:  ldc.i4.1
    IL_012f:  stloc.s    V_20
    IL_0131:  ldstr      "te3 check failed {0} {1}"
    IL_0136:  ldloc.3
    IL_0137:  box        [mscorlib]System.Int32
    IL_013c:  ldloc.s    V_13
    IL_013e:  box        [mscorlib]System.Int32
    IL_0143:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0148:  ldloc.s    V_4
    IL_014a:  ldloc.s    V_14
    IL_014c:  beq.s      IL_0169

    IL_014e:  ldc.i4.1
    IL_014f:  stloc.s    V_20
    IL_0151:  ldstr      "te064 check failed {0} {1}"
    IL_0156:  ldloc.s    V_4
    IL_0158:  box        [mscorlib]System.Int64
    IL_015d:  ldloc.s    V_14
    IL_015f:  box        [mscorlib]System.Int64
    IL_0164:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0169:  ldloc.s    V_5
    IL_016b:  ldloc.s    V_15
    IL_016d:  beq.s      IL_018a

    IL_016f:  ldc.i4.1
    IL_0170:  stloc.s    V_20
    IL_0172:  ldstr      "te164 check failed {0} {1}"
    IL_0177:  ldloc.s    V_5
    IL_0179:  box        [mscorlib]System.Int64
    IL_017e:  ldloc.s    V_15
    IL_0180:  box        [mscorlib]System.Int64
    IL_0185:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_018a:  ldloc.s    V_6
    IL_018c:  ldloc.s    V_16
    IL_018e:  beq.s      IL_01ab

    IL_0190:  ldc.i4.1
    IL_0191:  stloc.s    V_20
    IL_0193:  ldstr      "te264 check failed {0} {1}"
    IL_0198:  ldloc.s    V_6
    IL_019a:  box        [mscorlib]System.Int64
    IL_019f:  ldloc.s    V_16
    IL_01a1:  box        [mscorlib]System.Int64
    IL_01a6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_01ab:  ldloc.s    V_7
    IL_01ad:  ldloc.s    V_17
    IL_01af:  beq.s      IL_01cc

    IL_01b1:  ldc.i4.1
    IL_01b2:  stloc.s    V_20
    IL_01b4:  ldstr      "te364 check failed {0} {1}"
    IL_01b9:  ldloc.s    V_7
    IL_01bb:  box        [mscorlib]System.Int64
    IL_01c0:  ldloc.s    V_17
    IL_01c2:  box        [mscorlib]System.Int64
    IL_01c7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_01cc:  ldloc.s    V_8
    IL_01ce:  ldloc.s    V_18
    IL_01d0:  beq.s      IL_01ed

    IL_01d2:  ldc.i4.1
    IL_01d3:  stloc.s    V_20
    IL_01d5:  ldstr      "dummy check failed {0} {1}"
    IL_01da:  ldloc.s    V_8
    IL_01dc:  box        [mscorlib]System.Int32
    IL_01e1:  ldloc.s    V_18
    IL_01e3:  box        [mscorlib]System.Int32
    IL_01e8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_01ed:  ldloc.s    V_9
    IL_01ef:  ldloc.s    V_19
    IL_01f1:  beq.s      IL_020e

    IL_01f3:  ldc.i4.1
    IL_01f4:  stloc.s    V_20
    IL_01f6:  ldstr      "dummy64 check failed {0} {1}"
    IL_01fb:  ldloc.s    V_9
    IL_01fd:  box        [mscorlib]System.Int64
    IL_0202:  ldloc.s    V_19
    IL_0204:  box        [mscorlib]System.Int64
    IL_0209:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_020e:  ldsfld     int32 IntrinsicTest::counter
    IL_0213:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0218:  beq.s      IL_023b

    IL_021a:  ldstr      "counter mismatch {0} {1}"
    IL_021f:  ldsfld     int32 IntrinsicTest::counter
    IL_0224:  box        [mscorlib]System.Int32
    IL_0229:  ldsfld     int32 IntrinsicTest::id_counter
    IL_022e:  box        [mscorlib]System.Int32
    IL_0233:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0238:  ldc.i4.1
    IL_0239:  stloc.s    V_20
    IL_023b:  ldsfld     int64 IntrinsicTest::counter64
    IL_0240:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0245:  beq.s      IL_0268

    IL_0247:  ldstr      "counter64 mismatch {0} {1}"
    IL_024c:  ldsfld     int64 IntrinsicTest::counter64
    IL_0251:  box        [mscorlib]System.Int64
    IL_0256:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_025b:  box        [mscorlib]System.Int64
    IL_0260:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0265:  ldc.i4.1
    IL_0266:  stloc.s    V_20
    IL_0268:  ldloc.s    V_21
    IL_026a:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_026f:  ldloc.s    V_21
    IL_0271:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_0276:  beq.s      IL_029d

    IL_0278:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_027d:  ldloc.s    V_21
    IL_027f:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_0284:  box        [mscorlib]System.Int32
    IL_0289:  ldloc.s    V_21
    IL_028b:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_0290:  box        [mscorlib]System.Int32
    IL_0295:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_029a:  ldc.i4.1
    IL_029b:  stloc.s    V_20
    IL_029d:  ldloc.s    V_21
    IL_029f:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_02a4:  ldloc.s    V_21
    IL_02a6:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_02ab:  beq.s      IL_02d2

    IL_02ad:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_02b2:  ldloc.s    V_21
    IL_02b4:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_02b9:  box        [mscorlib]System.Int64
    IL_02be:  ldloc.s    V_21
    IL_02c0:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_02c5:  box        [mscorlib]System.Int64
    IL_02ca:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_02cf:  ldc.i4.1
    IL_02d0:  stloc.s    V_20
    IL_02d2:  ldsfld     int64 IntrinsicTest::temp
    IL_02d7:  ldsfld     int64 IntrinsicTest::idmp
    IL_02dc:  beq.s      IL_02ff

    IL_02de:  ldstr      "temp mismatch {0} {1}"
    IL_02e3:  ldsfld     int64 IntrinsicTest::temp
    IL_02e8:  box        [mscorlib]System.Int64
    IL_02ed:  ldsfld     int64 IntrinsicTest::idmp
    IL_02f2:  box        [mscorlib]System.Int64
    IL_02f7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_02fc:  ldc.i4.1
    IL_02fd:  stloc.s    V_20
    IL_02ff:  ldsflda    int64 IntrinsicTest::counter64
    IL_0304:  call       int64 [mscorlib]System.Threading.Interlocked::Decrement(int64&)
    IL_0309:  stsfld     int64 IntrinsicTest::counter64
    IL_030e:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0313:  ldc.i4.1
    IL_0314:  conv.i8
    IL_0315:  sub
    IL_0316:  stsfld     int64 IntrinsicTest::id_counter64
    IL_031b:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0320:  stsfld     int64 IntrinsicTest::id_counter64
    IL_0325:  ldstr      "------------------------------------DEC 64 1 1 0 1"
    IL_032a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_032f:  ldloc.0
    IL_0330:  ldloc.s    V_10
    IL_0332:  beq.s      IL_034e

    IL_0334:  ldc.i4.1
    IL_0335:  stloc.s    V_20
    IL_0337:  ldstr      "te0 check failed {0} {1}"
    IL_033c:  ldloc.0
    IL_033d:  box        [mscorlib]System.Int32
    IL_0342:  ldloc.s    V_10
    IL_0344:  box        [mscorlib]System.Int32
    IL_0349:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_034e:  ldloc.1
    IL_034f:  ldloc.s    V_11
    IL_0351:  beq.s      IL_036d

    IL_0353:  ldc.i4.1
    IL_0354:  stloc.s    V_20
    IL_0356:  ldstr      "te1 check failed {0} {1}"
    IL_035b:  ldloc.1
    IL_035c:  box        [mscorlib]System.Int32
    IL_0361:  ldloc.s    V_11
    IL_0363:  box        [mscorlib]System.Int32
    IL_0368:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_036d:  ldloc.2
    IL_036e:  ldloc.s    V_12
    IL_0370:  beq.s      IL_038c

    IL_0372:  ldc.i4.1
    IL_0373:  stloc.s    V_20
    IL_0375:  ldstr      "te2 check failed {0} {1}"
    IL_037a:  ldloc.2
    IL_037b:  box        [mscorlib]System.Int32
    IL_0380:  ldloc.s    V_12
    IL_0382:  box        [mscorlib]System.Int32
    IL_0387:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_038c:  ldloc.3
    IL_038d:  ldloc.s    V_13
    IL_038f:  beq.s      IL_03ab

    IL_0391:  ldc.i4.1
    IL_0392:  stloc.s    V_20
    IL_0394:  ldstr      "te3 check failed {0} {1}"
    IL_0399:  ldloc.3
    IL_039a:  box        [mscorlib]System.Int32
    IL_039f:  ldloc.s    V_13
    IL_03a1:  box        [mscorlib]System.Int32
    IL_03a6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_03ab:  ldloc.s    V_4
    IL_03ad:  ldloc.s    V_14
    IL_03af:  beq.s      IL_03cc

    IL_03b1:  ldc.i4.1
    IL_03b2:  stloc.s    V_20
    IL_03b4:  ldstr      "te064 check failed {0} {1}"
    IL_03b9:  ldloc.s    V_4
    IL_03bb:  box        [mscorlib]System.Int64
    IL_03c0:  ldloc.s    V_14
    IL_03c2:  box        [mscorlib]System.Int64
    IL_03c7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_03cc:  ldloc.s    V_5
    IL_03ce:  ldloc.s    V_15
    IL_03d0:  beq.s      IL_03ed

    IL_03d2:  ldc.i4.1
    IL_03d3:  stloc.s    V_20
    IL_03d5:  ldstr      "te164 check failed {0} {1}"
    IL_03da:  ldloc.s    V_5
    IL_03dc:  box        [mscorlib]System.Int64
    IL_03e1:  ldloc.s    V_15
    IL_03e3:  box        [mscorlib]System.Int64
    IL_03e8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_03ed:  ldloc.s    V_6
    IL_03ef:  ldloc.s    V_16
    IL_03f1:  beq.s      IL_040e

    IL_03f3:  ldc.i4.1
    IL_03f4:  stloc.s    V_20
    IL_03f6:  ldstr      "te264 check failed {0} {1}"
    IL_03fb:  ldloc.s    V_6
    IL_03fd:  box        [mscorlib]System.Int64
    IL_0402:  ldloc.s    V_16
    IL_0404:  box        [mscorlib]System.Int64
    IL_0409:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_040e:  ldloc.s    V_7
    IL_0410:  ldloc.s    V_17
    IL_0412:  beq.s      IL_042f

    IL_0414:  ldc.i4.1
    IL_0415:  stloc.s    V_20
    IL_0417:  ldstr      "te364 check failed {0} {1}"
    IL_041c:  ldloc.s    V_7
    IL_041e:  box        [mscorlib]System.Int64
    IL_0423:  ldloc.s    V_17
    IL_0425:  box        [mscorlib]System.Int64
    IL_042a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_042f:  ldloc.s    V_8
    IL_0431:  ldloc.s    V_18
    IL_0433:  beq.s      IL_0450

    IL_0435:  ldc.i4.1
    IL_0436:  stloc.s    V_20
    IL_0438:  ldstr      "dummy check failed {0} {1}"
    IL_043d:  ldloc.s    V_8
    IL_043f:  box        [mscorlib]System.Int32
    IL_0444:  ldloc.s    V_18
    IL_0446:  box        [mscorlib]System.Int32
    IL_044b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0450:  ldloc.s    V_9
    IL_0452:  ldloc.s    V_19
    IL_0454:  beq.s      IL_0471

    IL_0456:  ldc.i4.1
    IL_0457:  stloc.s    V_20
    IL_0459:  ldstr      "dummy64 check failed {0} {1}"
    IL_045e:  ldloc.s    V_9
    IL_0460:  box        [mscorlib]System.Int64
    IL_0465:  ldloc.s    V_19
    IL_0467:  box        [mscorlib]System.Int64
    IL_046c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0471:  ldsfld     int32 IntrinsicTest::counter
    IL_0476:  ldsfld     int32 IntrinsicTest::id_counter
    IL_047b:  beq.s      IL_049e

    IL_047d:  ldstr      "counter mismatch {0} {1}"
    IL_0482:  ldsfld     int32 IntrinsicTest::counter
    IL_0487:  box        [mscorlib]System.Int32
    IL_048c:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0491:  box        [mscorlib]System.Int32
    IL_0496:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_049b:  ldc.i4.1
    IL_049c:  stloc.s    V_20
    IL_049e:  ldsfld     int64 IntrinsicTest::counter64
    IL_04a3:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_04a8:  beq.s      IL_04cb

    IL_04aa:  ldstr      "counter64 mismatch {0} {1}"
    IL_04af:  ldsfld     int64 IntrinsicTest::counter64
    IL_04b4:  box        [mscorlib]System.Int64
    IL_04b9:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_04be:  box        [mscorlib]System.Int64
    IL_04c3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_04c8:  ldc.i4.1
    IL_04c9:  stloc.s    V_20
    IL_04cb:  ldloc.s    V_21
    IL_04cd:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_04d2:  ldloc.s    V_21
    IL_04d4:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_04d9:  beq.s      IL_0500

    IL_04db:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_04e0:  ldloc.s    V_21
    IL_04e2:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_04e7:  box        [mscorlib]System.Int32
    IL_04ec:  ldloc.s    V_21
    IL_04ee:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_04f3:  box        [mscorlib]System.Int32
    IL_04f8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_04fd:  ldc.i4.1
    IL_04fe:  stloc.s    V_20
    IL_0500:  ldloc.s    V_21
    IL_0502:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_0507:  ldloc.s    V_21
    IL_0509:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_050e:  beq.s      IL_0535

    IL_0510:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_0515:  ldloc.s    V_21
    IL_0517:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_051c:  box        [mscorlib]System.Int64
    IL_0521:  ldloc.s    V_21
    IL_0523:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_0528:  box        [mscorlib]System.Int64
    IL_052d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0532:  ldc.i4.1
    IL_0533:  stloc.s    V_20
    IL_0535:  ldsfld     int64 IntrinsicTest::temp
    IL_053a:  ldsfld     int64 IntrinsicTest::idmp
    IL_053f:  beq.s      IL_0562

    IL_0541:  ldstr      "temp mismatch {0} {1}"
    IL_0546:  ldsfld     int64 IntrinsicTest::temp
    IL_054b:  box        [mscorlib]System.Int64
    IL_0550:  ldsfld     int64 IntrinsicTest::idmp
    IL_0555:  box        [mscorlib]System.Int64
    IL_055a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_055f:  ldc.i4.1
    IL_0560:  stloc.s    V_20
    IL_0562:  ldsflda    int64 IntrinsicTest::counter64
    IL_0567:  ldsfld     int64 IntrinsicTest::counter64
    IL_056c:  call       int64 [mscorlib]System.Threading.Interlocked::Exchange(int64&,
                                                                                int64)
    IL_0571:  stsfld     int64 IntrinsicTest::counter64
    IL_0576:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_057b:  stsfld     int64 IntrinsicTest::id_counter64
    IL_0580:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0585:  stsfld     int64 IntrinsicTest::idjunk
    IL_058a:  ldstr      "------------------------------------XCHG 64 1 1 0 1"
    IL_058f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0594:  ldloc.0
    IL_0595:  ldloc.s    V_10
    IL_0597:  beq.s      IL_05b3

    IL_0599:  ldc.i4.1
    IL_059a:  stloc.s    V_20
    IL_059c:  ldstr      "te0 check failed {0} {1}"
    IL_05a1:  ldloc.0
    IL_05a2:  box        [mscorlib]System.Int32
    IL_05a7:  ldloc.s    V_10
    IL_05a9:  box        [mscorlib]System.Int32
    IL_05ae:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_05b3:  ldloc.1
    IL_05b4:  ldloc.s    V_11
    IL_05b6:  beq.s      IL_05d2

    IL_05b8:  ldc.i4.1
    IL_05b9:  stloc.s    V_20
    IL_05bb:  ldstr      "te1 check failed {0} {1}"
    IL_05c0:  ldloc.1
    IL_05c1:  box        [mscorlib]System.Int32
    IL_05c6:  ldloc.s    V_11
    IL_05c8:  box        [mscorlib]System.Int32
    IL_05cd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_05d2:  ldloc.2
    IL_05d3:  ldloc.s    V_12
    IL_05d5:  beq.s      IL_05f1

    IL_05d7:  ldc.i4.1
    IL_05d8:  stloc.s    V_20
    IL_05da:  ldstr      "te2 check failed {0} {1}"
    IL_05df:  ldloc.2
    IL_05e0:  box        [mscorlib]System.Int32
    IL_05e5:  ldloc.s    V_12
    IL_05e7:  box        [mscorlib]System.Int32
    IL_05ec:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_05f1:  ldloc.3
    IL_05f2:  ldloc.s    V_13
    IL_05f4:  beq.s      IL_0610

    IL_05f6:  ldc.i4.1
    IL_05f7:  stloc.s    V_20
    IL_05f9:  ldstr      "te3 check failed {0} {1}"
    IL_05fe:  ldloc.3
    IL_05ff:  box        [mscorlib]System.Int32
    IL_0604:  ldloc.s    V_13
    IL_0606:  box        [mscorlib]System.Int32
    IL_060b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0610:  ldloc.s    V_4
    IL_0612:  ldloc.s    V_14
    IL_0614:  beq.s      IL_0631

    IL_0616:  ldc.i4.1
    IL_0617:  stloc.s    V_20
    IL_0619:  ldstr      "te064 check failed {0} {1}"
    IL_061e:  ldloc.s    V_4
    IL_0620:  box        [mscorlib]System.Int64
    IL_0625:  ldloc.s    V_14
    IL_0627:  box        [mscorlib]System.Int64
    IL_062c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0631:  ldloc.s    V_5
    IL_0633:  ldloc.s    V_15
    IL_0635:  beq.s      IL_0652

    IL_0637:  ldc.i4.1
    IL_0638:  stloc.s    V_20
    IL_063a:  ldstr      "te164 check failed {0} {1}"
    IL_063f:  ldloc.s    V_5
    IL_0641:  box        [mscorlib]System.Int64
    IL_0646:  ldloc.s    V_15
    IL_0648:  box        [mscorlib]System.Int64
    IL_064d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0652:  ldloc.s    V_6
    IL_0654:  ldloc.s    V_16
    IL_0656:  beq.s      IL_0673

    IL_0658:  ldc.i4.1
    IL_0659:  stloc.s    V_20
    IL_065b:  ldstr      "te264 check failed {0} {1}"
    IL_0660:  ldloc.s    V_6
    IL_0662:  box        [mscorlib]System.Int64
    IL_0667:  ldloc.s    V_16
    IL_0669:  box        [mscorlib]System.Int64
    IL_066e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0673:  ldloc.s    V_7
    IL_0675:  ldloc.s    V_17
    IL_0677:  beq.s      IL_0694

    IL_0679:  ldc.i4.1
    IL_067a:  stloc.s    V_20
    IL_067c:  ldstr      "te364 check failed {0} {1}"
    IL_0681:  ldloc.s    V_7
    IL_0683:  box        [mscorlib]System.Int64
    IL_0688:  ldloc.s    V_17
    IL_068a:  box        [mscorlib]System.Int64
    IL_068f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0694:  ldloc.s    V_8
    IL_0696:  ldloc.s    V_18
    IL_0698:  beq.s      IL_06b5

    IL_069a:  ldc.i4.1
    IL_069b:  stloc.s    V_20
    IL_069d:  ldstr      "dummy check failed {0} {1}"
    IL_06a2:  ldloc.s    V_8
    IL_06a4:  box        [mscorlib]System.Int32
    IL_06a9:  ldloc.s    V_18
    IL_06ab:  box        [mscorlib]System.Int32
    IL_06b0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_06b5:  ldloc.s    V_9
    IL_06b7:  ldloc.s    V_19
    IL_06b9:  beq.s      IL_06d6

    IL_06bb:  ldc.i4.1
    IL_06bc:  stloc.s    V_20
    IL_06be:  ldstr      "dummy64 check failed {0} {1}"
    IL_06c3:  ldloc.s    V_9
    IL_06c5:  box        [mscorlib]System.Int64
    IL_06ca:  ldloc.s    V_19
    IL_06cc:  box        [mscorlib]System.Int64
    IL_06d1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_06d6:  ldsfld     int32 IntrinsicTest::counter
    IL_06db:  ldsfld     int32 IntrinsicTest::id_counter
    IL_06e0:  beq.s      IL_0703

    IL_06e2:  ldstr      "counter mismatch {0} {1}"
    IL_06e7:  ldsfld     int32 IntrinsicTest::counter
    IL_06ec:  box        [mscorlib]System.Int32
    IL_06f1:  ldsfld     int32 IntrinsicTest::id_counter
    IL_06f6:  box        [mscorlib]System.Int32
    IL_06fb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0700:  ldc.i4.1
    IL_0701:  stloc.s    V_20
    IL_0703:  ldsfld     int64 IntrinsicTest::counter64
    IL_0708:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_070d:  beq.s      IL_0730

    IL_070f:  ldstr      "counter64 mismatch {0} {1}"
    IL_0714:  ldsfld     int64 IntrinsicTest::counter64
    IL_0719:  box        [mscorlib]System.Int64
    IL_071e:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0723:  box        [mscorlib]System.Int64
    IL_0728:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_072d:  ldc.i4.1
    IL_072e:  stloc.s    V_20
    IL_0730:  ldloc.s    V_21
    IL_0732:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_0737:  ldloc.s    V_21
    IL_0739:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_073e:  beq.s      IL_0765

    IL_0740:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_0745:  ldloc.s    V_21
    IL_0747:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_074c:  box        [mscorlib]System.Int32
    IL_0751:  ldloc.s    V_21
    IL_0753:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_0758:  box        [mscorlib]System.Int32
    IL_075d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0762:  ldc.i4.1
    IL_0763:  stloc.s    V_20
    IL_0765:  ldloc.s    V_21
    IL_0767:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_076c:  ldloc.s    V_21
    IL_076e:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_0773:  beq.s      IL_079a

    IL_0775:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_077a:  ldloc.s    V_21
    IL_077c:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_0781:  box        [mscorlib]System.Int64
    IL_0786:  ldloc.s    V_21
    IL_0788:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_078d:  box        [mscorlib]System.Int64
    IL_0792:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0797:  ldc.i4.1
    IL_0798:  stloc.s    V_20
    IL_079a:  ldsfld     int64 IntrinsicTest::temp
    IL_079f:  ldsfld     int64 IntrinsicTest::idmp
    IL_07a4:  beq.s      IL_07c7

    IL_07a6:  ldstr      "temp mismatch {0} {1}"
    IL_07ab:  ldsfld     int64 IntrinsicTest::temp
    IL_07b0:  box        [mscorlib]System.Int64
    IL_07b5:  ldsfld     int64 IntrinsicTest::idmp
    IL_07ba:  box        [mscorlib]System.Int64
    IL_07bf:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_07c4:  ldc.i4.1
    IL_07c5:  stloc.s    V_20
    IL_07c7:  ldsflda    int64 IntrinsicTest::counter64
    IL_07cc:  ldsfld     int64 IntrinsicTest::counter64
    IL_07d1:  call       int64 [mscorlib]System.Threading.Interlocked::Add(int64&,
                                                                           int64)
    IL_07d6:  stsfld     int64 IntrinsicTest::counter64
    IL_07db:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_07e0:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_07e5:  add
    IL_07e6:  stsfld     int64 IntrinsicTest::id_counter64
    IL_07eb:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_07f0:  stsfld     int64 IntrinsicTest::id_counter64
    IL_07f5:  ldstr      "------------------------------------XADD 64 1 1 0 1"
    IL_07fa:  call       void [System.Console]System.Console::WriteLine(string)
    IL_07ff:  ldloc.0
    IL_0800:  ldloc.s    V_10
    IL_0802:  beq.s      IL_081e

    IL_0804:  ldc.i4.1
    IL_0805:  stloc.s    V_20
    IL_0807:  ldstr      "te0 check failed {0} {1}"
    IL_080c:  ldloc.0
    IL_080d:  box        [mscorlib]System.Int32
    IL_0812:  ldloc.s    V_10
    IL_0814:  box        [mscorlib]System.Int32
    IL_0819:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_081e:  ldloc.1
    IL_081f:  ldloc.s    V_11
    IL_0821:  beq.s      IL_083d

    IL_0823:  ldc.i4.1
    IL_0824:  stloc.s    V_20
    IL_0826:  ldstr      "te1 check failed {0} {1}"
    IL_082b:  ldloc.1
    IL_082c:  box        [mscorlib]System.Int32
    IL_0831:  ldloc.s    V_11
    IL_0833:  box        [mscorlib]System.Int32
    IL_0838:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_083d:  ldloc.2
    IL_083e:  ldloc.s    V_12
    IL_0840:  beq.s      IL_085c

    IL_0842:  ldc.i4.1
    IL_0843:  stloc.s    V_20
    IL_0845:  ldstr      "te2 check failed {0} {1}"
    IL_084a:  ldloc.2
    IL_084b:  box        [mscorlib]System.Int32
    IL_0850:  ldloc.s    V_12
    IL_0852:  box        [mscorlib]System.Int32
    IL_0857:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_085c:  ldloc.3
    IL_085d:  ldloc.s    V_13
    IL_085f:  beq.s      IL_087b

    IL_0861:  ldc.i4.1
    IL_0862:  stloc.s    V_20
    IL_0864:  ldstr      "te3 check failed {0} {1}"
    IL_0869:  ldloc.3
    IL_086a:  box        [mscorlib]System.Int32
    IL_086f:  ldloc.s    V_13
    IL_0871:  box        [mscorlib]System.Int32
    IL_0876:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_087b:  ldloc.s    V_4
    IL_087d:  ldloc.s    V_14
    IL_087f:  beq.s      IL_089c

    IL_0881:  ldc.i4.1
    IL_0882:  stloc.s    V_20
    IL_0884:  ldstr      "te064 check failed {0} {1}"
    IL_0889:  ldloc.s    V_4
    IL_088b:  box        [mscorlib]System.Int64
    IL_0890:  ldloc.s    V_14
    IL_0892:  box        [mscorlib]System.Int64
    IL_0897:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_089c:  ldloc.s    V_5
    IL_089e:  ldloc.s    V_15
    IL_08a0:  beq.s      IL_08bd

    IL_08a2:  ldc.i4.1
    IL_08a3:  stloc.s    V_20
    IL_08a5:  ldstr      "te164 check failed {0} {1}"
    IL_08aa:  ldloc.s    V_5
    IL_08ac:  box        [mscorlib]System.Int64
    IL_08b1:  ldloc.s    V_15
    IL_08b3:  box        [mscorlib]System.Int64
    IL_08b8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_08bd:  ldloc.s    V_6
    IL_08bf:  ldloc.s    V_16
    IL_08c1:  beq.s      IL_08de

    IL_08c3:  ldc.i4.1
    IL_08c4:  stloc.s    V_20
    IL_08c6:  ldstr      "te264 check failed {0} {1}"
    IL_08cb:  ldloc.s    V_6
    IL_08cd:  box        [mscorlib]System.Int64
    IL_08d2:  ldloc.s    V_16
    IL_08d4:  box        [mscorlib]System.Int64
    IL_08d9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_08de:  ldloc.s    V_7
    IL_08e0:  ldloc.s    V_17
    IL_08e2:  beq.s      IL_08ff

    IL_08e4:  ldc.i4.1
    IL_08e5:  stloc.s    V_20
    IL_08e7:  ldstr      "te364 check failed {0} {1}"
    IL_08ec:  ldloc.s    V_7
    IL_08ee:  box        [mscorlib]System.Int64
    IL_08f3:  ldloc.s    V_17
    IL_08f5:  box        [mscorlib]System.Int64
    IL_08fa:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_08ff:  ldloc.s    V_8
    IL_0901:  ldloc.s    V_18
    IL_0903:  beq.s      IL_0920

    IL_0905:  ldc.i4.1
    IL_0906:  stloc.s    V_20
    IL_0908:  ldstr      "dummy check failed {0} {1}"
    IL_090d:  ldloc.s    V_8
    IL_090f:  box        [mscorlib]System.Int32
    IL_0914:  ldloc.s    V_18
    IL_0916:  box        [mscorlib]System.Int32
    IL_091b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0920:  ldloc.s    V_9
    IL_0922:  ldloc.s    V_19
    IL_0924:  beq.s      IL_0941

    IL_0926:  ldc.i4.1
    IL_0927:  stloc.s    V_20
    IL_0929:  ldstr      "dummy64 check failed {0} {1}"
    IL_092e:  ldloc.s    V_9
    IL_0930:  box        [mscorlib]System.Int64
    IL_0935:  ldloc.s    V_19
    IL_0937:  box        [mscorlib]System.Int64
    IL_093c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0941:  ldsfld     int32 IntrinsicTest::counter
    IL_0946:  ldsfld     int32 IntrinsicTest::id_counter
    IL_094b:  beq.s      IL_096e

    IL_094d:  ldstr      "counter mismatch {0} {1}"
    IL_0952:  ldsfld     int32 IntrinsicTest::counter
    IL_0957:  box        [mscorlib]System.Int32
    IL_095c:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0961:  box        [mscorlib]System.Int32
    IL_0966:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_096b:  ldc.i4.1
    IL_096c:  stloc.s    V_20
    IL_096e:  ldsfld     int64 IntrinsicTest::counter64
    IL_0973:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0978:  beq.s      IL_099b

    IL_097a:  ldstr      "counter64 mismatch {0} {1}"
    IL_097f:  ldsfld     int64 IntrinsicTest::counter64
    IL_0984:  box        [mscorlib]System.Int64
    IL_0989:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_098e:  box        [mscorlib]System.Int64
    IL_0993:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0998:  ldc.i4.1
    IL_0999:  stloc.s    V_20
    IL_099b:  ldloc.s    V_21
    IL_099d:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_09a2:  ldloc.s    V_21
    IL_09a4:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_09a9:  beq.s      IL_09d0

    IL_09ab:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_09b0:  ldloc.s    V_21
    IL_09b2:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_09b7:  box        [mscorlib]System.Int32
    IL_09bc:  ldloc.s    V_21
    IL_09be:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_09c3:  box        [mscorlib]System.Int32
    IL_09c8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_09cd:  ldc.i4.1
    IL_09ce:  stloc.s    V_20
    IL_09d0:  ldloc.s    V_21
    IL_09d2:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_09d7:  ldloc.s    V_21
    IL_09d9:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_09de:  beq.s      IL_0a05

    IL_09e0:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_09e5:  ldloc.s    V_21
    IL_09e7:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_09ec:  box        [mscorlib]System.Int64
    IL_09f1:  ldloc.s    V_21
    IL_09f3:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_09f8:  box        [mscorlib]System.Int64
    IL_09fd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0a02:  ldc.i4.1
    IL_0a03:  stloc.s    V_20
    IL_0a05:  ldsfld     int64 IntrinsicTest::temp
    IL_0a0a:  ldsfld     int64 IntrinsicTest::idmp
    IL_0a0f:  beq.s      IL_0a32

    IL_0a11:  ldstr      "temp mismatch {0} {1}"
    IL_0a16:  ldsfld     int64 IntrinsicTest::temp
    IL_0a1b:  box        [mscorlib]System.Int64
    IL_0a20:  ldsfld     int64 IntrinsicTest::idmp
    IL_0a25:  box        [mscorlib]System.Int64
    IL_0a2a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0a2f:  ldc.i4.1
    IL_0a30:  stloc.s    V_20
    IL_0a32:  ldloc.s    V_20
    IL_0a34:  ret
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



