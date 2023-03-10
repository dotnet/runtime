




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
.assembly 'rva_rvastatic1'
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
    .maxstack  4
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
    IL_00a1:  ldsfld     int64 IntrinsicTest::counter64
    IL_00a6:  ldloc.s    V_4
    IL_00a8:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_00ad:  stloc.s    V_4
    IL_00af:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_00b4:  stloc.s    V_14
    IL_00b6:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_00bb:  ldloc.s    V_14
    IL_00bd:  bne.un.s   IL_00cb

    IL_00bf:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_00c4:  stsfld     int64 IntrinsicTest::id_counter64
    IL_00c9:  br.s       IL_00d5

    IL_00cb:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_00d0:  stsfld     int64 IntrinsicTest::idjunk
    IL_00d5:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 0 0"
    IL_00da:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00df:  ldloc.0
    IL_00e0:  ldloc.s    V_10
    IL_00e2:  beq.s      IL_00fe

    IL_00e4:  ldc.i4.1
    IL_00e5:  stloc.s    V_20
    IL_00e7:  ldstr      "te0 check failed {0} {1}"
    IL_00ec:  ldloc.0
    IL_00ed:  box        [mscorlib]System.Int32
    IL_00f2:  ldloc.s    V_10
    IL_00f4:  box        [mscorlib]System.Int32
    IL_00f9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_00fe:  ldloc.1
    IL_00ff:  ldloc.s    V_11
    IL_0101:  beq.s      IL_011d

    IL_0103:  ldc.i4.1
    IL_0104:  stloc.s    V_20
    IL_0106:  ldstr      "te1 check failed {0} {1}"
    IL_010b:  ldloc.1
    IL_010c:  box        [mscorlib]System.Int32
    IL_0111:  ldloc.s    V_11
    IL_0113:  box        [mscorlib]System.Int32
    IL_0118:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_011d:  ldloc.2
    IL_011e:  ldloc.s    V_12
    IL_0120:  beq.s      IL_013c

    IL_0122:  ldc.i4.1
    IL_0123:  stloc.s    V_20
    IL_0125:  ldstr      "te2 check failed {0} {1}"
    IL_012a:  ldloc.2
    IL_012b:  box        [mscorlib]System.Int32
    IL_0130:  ldloc.s    V_12
    IL_0132:  box        [mscorlib]System.Int32
    IL_0137:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_013c:  ldloc.3
    IL_013d:  ldloc.s    V_13
    IL_013f:  beq.s      IL_015b

    IL_0141:  ldc.i4.1
    IL_0142:  stloc.s    V_20
    IL_0144:  ldstr      "te3 check failed {0} {1}"
    IL_0149:  ldloc.3
    IL_014a:  box        [mscorlib]System.Int32
    IL_014f:  ldloc.s    V_13
    IL_0151:  box        [mscorlib]System.Int32
    IL_0156:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_015b:  ldloc.s    V_4
    IL_015d:  ldloc.s    V_14
    IL_015f:  beq.s      IL_017c

    IL_0161:  ldc.i4.1
    IL_0162:  stloc.s    V_20
    IL_0164:  ldstr      "te064 check failed {0} {1}"
    IL_0169:  ldloc.s    V_4
    IL_016b:  box        [mscorlib]System.Int64
    IL_0170:  ldloc.s    V_14
    IL_0172:  box        [mscorlib]System.Int64
    IL_0177:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_017c:  ldloc.s    V_5
    IL_017e:  ldloc.s    V_15
    IL_0180:  beq.s      IL_019d

    IL_0182:  ldc.i4.1
    IL_0183:  stloc.s    V_20
    IL_0185:  ldstr      "te164 check failed {0} {1}"
    IL_018a:  ldloc.s    V_5
    IL_018c:  box        [mscorlib]System.Int64
    IL_0191:  ldloc.s    V_15
    IL_0193:  box        [mscorlib]System.Int64
    IL_0198:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_019d:  ldloc.s    V_6
    IL_019f:  ldloc.s    V_16
    IL_01a1:  beq.s      IL_01be

    IL_01a3:  ldc.i4.1
    IL_01a4:  stloc.s    V_20
    IL_01a6:  ldstr      "te264 check failed {0} {1}"
    IL_01ab:  ldloc.s    V_6
    IL_01ad:  box        [mscorlib]System.Int64
    IL_01b2:  ldloc.s    V_16
    IL_01b4:  box        [mscorlib]System.Int64
    IL_01b9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_01be:  ldloc.s    V_7
    IL_01c0:  ldloc.s    V_17
    IL_01c2:  beq.s      IL_01df

    IL_01c4:  ldc.i4.1
    IL_01c5:  stloc.s    V_20
    IL_01c7:  ldstr      "te364 check failed {0} {1}"
    IL_01cc:  ldloc.s    V_7
    IL_01ce:  box        [mscorlib]System.Int64
    IL_01d3:  ldloc.s    V_17
    IL_01d5:  box        [mscorlib]System.Int64
    IL_01da:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_01df:  ldloc.s    V_8
    IL_01e1:  ldloc.s    V_18
    IL_01e3:  beq.s      IL_0200

    IL_01e5:  ldc.i4.1
    IL_01e6:  stloc.s    V_20
    IL_01e8:  ldstr      "dummy check failed {0} {1}"
    IL_01ed:  ldloc.s    V_8
    IL_01ef:  box        [mscorlib]System.Int32
    IL_01f4:  ldloc.s    V_18
    IL_01f6:  box        [mscorlib]System.Int32
    IL_01fb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0200:  ldloc.s    V_9
    IL_0202:  ldloc.s    V_19
    IL_0204:  beq.s      IL_0221

    IL_0206:  ldc.i4.1
    IL_0207:  stloc.s    V_20
    IL_0209:  ldstr      "dummy64 check failed {0} {1}"
    IL_020e:  ldloc.s    V_9
    IL_0210:  box        [mscorlib]System.Int64
    IL_0215:  ldloc.s    V_19
    IL_0217:  box        [mscorlib]System.Int64
    IL_021c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0221:  ldsfld     int32 IntrinsicTest::counter
    IL_0226:  ldsfld     int32 IntrinsicTest::id_counter
    IL_022b:  beq.s      IL_024e

    IL_022d:  ldstr      "counter mismatch {0} {1}"
    IL_0232:  ldsfld     int32 IntrinsicTest::counter
    IL_0237:  box        [mscorlib]System.Int32
    IL_023c:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0241:  box        [mscorlib]System.Int32
    IL_0246:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_024b:  ldc.i4.1
    IL_024c:  stloc.s    V_20
    IL_024e:  ldsfld     int64 IntrinsicTest::counter64
    IL_0253:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0258:  beq.s      IL_027b

    IL_025a:  ldstr      "counter64 mismatch {0} {1}"
    IL_025f:  ldsfld     int64 IntrinsicTest::counter64
    IL_0264:  box        [mscorlib]System.Int64
    IL_0269:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_026e:  box        [mscorlib]System.Int64
    IL_0273:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0278:  ldc.i4.1
    IL_0279:  stloc.s    V_20
    IL_027b:  ldloc.s    V_21
    IL_027d:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_0282:  ldloc.s    V_21
    IL_0284:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_0289:  beq.s      IL_02b0

    IL_028b:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_0290:  ldloc.s    V_21
    IL_0292:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_0297:  box        [mscorlib]System.Int32
    IL_029c:  ldloc.s    V_21
    IL_029e:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_02a3:  box        [mscorlib]System.Int32
    IL_02a8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_02ad:  ldc.i4.1
    IL_02ae:  stloc.s    V_20
    IL_02b0:  ldloc.s    V_21
    IL_02b2:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_02b7:  ldloc.s    V_21
    IL_02b9:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_02be:  beq.s      IL_02e5

    IL_02c0:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_02c5:  ldloc.s    V_21
    IL_02c7:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_02cc:  box        [mscorlib]System.Int64
    IL_02d1:  ldloc.s    V_21
    IL_02d3:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_02d8:  box        [mscorlib]System.Int64
    IL_02dd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_02e2:  ldc.i4.1
    IL_02e3:  stloc.s    V_20
    IL_02e5:  ldsfld     int64 IntrinsicTest::temp
    IL_02ea:  ldsfld     int64 IntrinsicTest::idmp
    IL_02ef:  beq.s      IL_0312

    IL_02f1:  ldstr      "temp mismatch {0} {1}"
    IL_02f6:  ldsfld     int64 IntrinsicTest::temp
    IL_02fb:  box        [mscorlib]System.Int64
    IL_0300:  ldsfld     int64 IntrinsicTest::idmp
    IL_0305:  box        [mscorlib]System.Int64
    IL_030a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_030f:  ldc.i4.1
    IL_0310:  stloc.s    V_20
    IL_0312:  ldsflda    int64 IntrinsicTest::counter64
    IL_0317:  ldsfld     int64 IntrinsicTest::counter64
    IL_031c:  ldloc.s    V_4
    IL_031e:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_0323:  stsfld     int64 IntrinsicTest::counter64
    IL_0328:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_032d:  stsfld     int64 IntrinsicTest::id_counter64
    IL_0332:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0337:  ldloc.s    V_14
    IL_0339:  bne.un.s   IL_0347

    IL_033b:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0340:  stsfld     int64 IntrinsicTest::idjunk
    IL_0345:  br.s       IL_0351

    IL_0347:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_034c:  stsfld     int64 IntrinsicTest::idjunk
    IL_0351:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 0 1"
    IL_0356:  call       void [System.Console]System.Console::WriteLine(string)
    IL_035b:  ldloc.0
    IL_035c:  ldloc.s    V_10
    IL_035e:  beq.s      IL_037a

    IL_0360:  ldc.i4.1
    IL_0361:  stloc.s    V_20
    IL_0363:  ldstr      "te0 check failed {0} {1}"
    IL_0368:  ldloc.0
    IL_0369:  box        [mscorlib]System.Int32
    IL_036e:  ldloc.s    V_10
    IL_0370:  box        [mscorlib]System.Int32
    IL_0375:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_037a:  ldloc.1
    IL_037b:  ldloc.s    V_11
    IL_037d:  beq.s      IL_0399

    IL_037f:  ldc.i4.1
    IL_0380:  stloc.s    V_20
    IL_0382:  ldstr      "te1 check failed {0} {1}"
    IL_0387:  ldloc.1
    IL_0388:  box        [mscorlib]System.Int32
    IL_038d:  ldloc.s    V_11
    IL_038f:  box        [mscorlib]System.Int32
    IL_0394:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0399:  ldloc.2
    IL_039a:  ldloc.s    V_12
    IL_039c:  beq.s      IL_03b8

    IL_039e:  ldc.i4.1
    IL_039f:  stloc.s    V_20
    IL_03a1:  ldstr      "te2 check failed {0} {1}"
    IL_03a6:  ldloc.2
    IL_03a7:  box        [mscorlib]System.Int32
    IL_03ac:  ldloc.s    V_12
    IL_03ae:  box        [mscorlib]System.Int32
    IL_03b3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_03b8:  ldloc.3
    IL_03b9:  ldloc.s    V_13
    IL_03bb:  beq.s      IL_03d7

    IL_03bd:  ldc.i4.1
    IL_03be:  stloc.s    V_20
    IL_03c0:  ldstr      "te3 check failed {0} {1}"
    IL_03c5:  ldloc.3
    IL_03c6:  box        [mscorlib]System.Int32
    IL_03cb:  ldloc.s    V_13
    IL_03cd:  box        [mscorlib]System.Int32
    IL_03d2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_03d7:  ldloc.s    V_4
    IL_03d9:  ldloc.s    V_14
    IL_03db:  beq.s      IL_03f8

    IL_03dd:  ldc.i4.1
    IL_03de:  stloc.s    V_20
    IL_03e0:  ldstr      "te064 check failed {0} {1}"
    IL_03e5:  ldloc.s    V_4
    IL_03e7:  box        [mscorlib]System.Int64
    IL_03ec:  ldloc.s    V_14
    IL_03ee:  box        [mscorlib]System.Int64
    IL_03f3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_03f8:  ldloc.s    V_5
    IL_03fa:  ldloc.s    V_15
    IL_03fc:  beq.s      IL_0419

    IL_03fe:  ldc.i4.1
    IL_03ff:  stloc.s    V_20
    IL_0401:  ldstr      "te164 check failed {0} {1}"
    IL_0406:  ldloc.s    V_5
    IL_0408:  box        [mscorlib]System.Int64
    IL_040d:  ldloc.s    V_15
    IL_040f:  box        [mscorlib]System.Int64
    IL_0414:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0419:  ldloc.s    V_6
    IL_041b:  ldloc.s    V_16
    IL_041d:  beq.s      IL_043a

    IL_041f:  ldc.i4.1
    IL_0420:  stloc.s    V_20
    IL_0422:  ldstr      "te264 check failed {0} {1}"
    IL_0427:  ldloc.s    V_6
    IL_0429:  box        [mscorlib]System.Int64
    IL_042e:  ldloc.s    V_16
    IL_0430:  box        [mscorlib]System.Int64
    IL_0435:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_043a:  ldloc.s    V_7
    IL_043c:  ldloc.s    V_17
    IL_043e:  beq.s      IL_045b

    IL_0440:  ldc.i4.1
    IL_0441:  stloc.s    V_20
    IL_0443:  ldstr      "te364 check failed {0} {1}"
    IL_0448:  ldloc.s    V_7
    IL_044a:  box        [mscorlib]System.Int64
    IL_044f:  ldloc.s    V_17
    IL_0451:  box        [mscorlib]System.Int64
    IL_0456:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_045b:  ldloc.s    V_8
    IL_045d:  ldloc.s    V_18
    IL_045f:  beq.s      IL_047c

    IL_0461:  ldc.i4.1
    IL_0462:  stloc.s    V_20
    IL_0464:  ldstr      "dummy check failed {0} {1}"
    IL_0469:  ldloc.s    V_8
    IL_046b:  box        [mscorlib]System.Int32
    IL_0470:  ldloc.s    V_18
    IL_0472:  box        [mscorlib]System.Int32
    IL_0477:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_047c:  ldloc.s    V_9
    IL_047e:  ldloc.s    V_19
    IL_0480:  beq.s      IL_049d

    IL_0482:  ldc.i4.1
    IL_0483:  stloc.s    V_20
    IL_0485:  ldstr      "dummy64 check failed {0} {1}"
    IL_048a:  ldloc.s    V_9
    IL_048c:  box        [mscorlib]System.Int64
    IL_0491:  ldloc.s    V_19
    IL_0493:  box        [mscorlib]System.Int64
    IL_0498:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_049d:  ldsfld     int32 IntrinsicTest::counter
    IL_04a2:  ldsfld     int32 IntrinsicTest::id_counter
    IL_04a7:  beq.s      IL_04ca

    IL_04a9:  ldstr      "counter mismatch {0} {1}"
    IL_04ae:  ldsfld     int32 IntrinsicTest::counter
    IL_04b3:  box        [mscorlib]System.Int32
    IL_04b8:  ldsfld     int32 IntrinsicTest::id_counter
    IL_04bd:  box        [mscorlib]System.Int32
    IL_04c2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_04c7:  ldc.i4.1
    IL_04c8:  stloc.s    V_20
    IL_04ca:  ldsfld     int64 IntrinsicTest::counter64
    IL_04cf:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_04d4:  beq.s      IL_04f7

    IL_04d6:  ldstr      "counter64 mismatch {0} {1}"
    IL_04db:  ldsfld     int64 IntrinsicTest::counter64
    IL_04e0:  box        [mscorlib]System.Int64
    IL_04e5:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_04ea:  box        [mscorlib]System.Int64
    IL_04ef:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_04f4:  ldc.i4.1
    IL_04f5:  stloc.s    V_20
    IL_04f7:  ldloc.s    V_21
    IL_04f9:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_04fe:  ldloc.s    V_21
    IL_0500:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_0505:  beq.s      IL_052c

    IL_0507:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_050c:  ldloc.s    V_21
    IL_050e:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_0513:  box        [mscorlib]System.Int32
    IL_0518:  ldloc.s    V_21
    IL_051a:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_051f:  box        [mscorlib]System.Int32
    IL_0524:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0529:  ldc.i4.1
    IL_052a:  stloc.s    V_20
    IL_052c:  ldloc.s    V_21
    IL_052e:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_0533:  ldloc.s    V_21
    IL_0535:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_053a:  beq.s      IL_0561

    IL_053c:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_0541:  ldloc.s    V_21
    IL_0543:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_0548:  box        [mscorlib]System.Int64
    IL_054d:  ldloc.s    V_21
    IL_054f:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_0554:  box        [mscorlib]System.Int64
    IL_0559:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_055e:  ldc.i4.1
    IL_055f:  stloc.s    V_20
    IL_0561:  ldsfld     int64 IntrinsicTest::temp
    IL_0566:  ldsfld     int64 IntrinsicTest::idmp
    IL_056b:  beq.s      IL_058e

    IL_056d:  ldstr      "temp mismatch {0} {1}"
    IL_0572:  ldsfld     int64 IntrinsicTest::temp
    IL_0577:  box        [mscorlib]System.Int64
    IL_057c:  ldsfld     int64 IntrinsicTest::idmp
    IL_0581:  box        [mscorlib]System.Int64
    IL_0586:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_058b:  ldc.i4.1
    IL_058c:  stloc.s    V_20
    IL_058e:  ldloc.s    V_21
    IL_0590:  ldsflda    int64 IntrinsicTest::counter64
    IL_0595:  ldsfld     int64 IntrinsicTest::counter64
    IL_059a:  ldloc.s    V_4
    IL_059c:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_05a1:  stfld      int64 IntrinsicTest::instanceCounter64
    IL_05a6:  ldloc.s    V_21
    IL_05a8:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_05ad:  stfld      int64 IntrinsicTest::id_instanceCounter64
    IL_05b2:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_05b7:  ldloc.s    V_14
    IL_05b9:  bne.un.s   IL_05c7

    IL_05bb:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_05c0:  stsfld     int64 IntrinsicTest::id_counter64
    IL_05c5:  br.s       IL_05d1

    IL_05c7:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_05cc:  stsfld     int64 IntrinsicTest::idjunk
    IL_05d1:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 0 2"
    IL_05d6:  call       void [System.Console]System.Console::WriteLine(string)
    IL_05db:  ldloc.0
    IL_05dc:  ldloc.s    V_10
    IL_05de:  beq.s      IL_05fa

    IL_05e0:  ldc.i4.1
    IL_05e1:  stloc.s    V_20
    IL_05e3:  ldstr      "te0 check failed {0} {1}"
    IL_05e8:  ldloc.0
    IL_05e9:  box        [mscorlib]System.Int32
    IL_05ee:  ldloc.s    V_10
    IL_05f0:  box        [mscorlib]System.Int32
    IL_05f5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_05fa:  ldloc.1
    IL_05fb:  ldloc.s    V_11
    IL_05fd:  beq.s      IL_0619

    IL_05ff:  ldc.i4.1
    IL_0600:  stloc.s    V_20
    IL_0602:  ldstr      "te1 check failed {0} {1}"
    IL_0607:  ldloc.1
    IL_0608:  box        [mscorlib]System.Int32
    IL_060d:  ldloc.s    V_11
    IL_060f:  box        [mscorlib]System.Int32
    IL_0614:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0619:  ldloc.2
    IL_061a:  ldloc.s    V_12
    IL_061c:  beq.s      IL_0638

    IL_061e:  ldc.i4.1
    IL_061f:  stloc.s    V_20
    IL_0621:  ldstr      "te2 check failed {0} {1}"
    IL_0626:  ldloc.2
    IL_0627:  box        [mscorlib]System.Int32
    IL_062c:  ldloc.s    V_12
    IL_062e:  box        [mscorlib]System.Int32
    IL_0633:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0638:  ldloc.3
    IL_0639:  ldloc.s    V_13
    IL_063b:  beq.s      IL_0657

    IL_063d:  ldc.i4.1
    IL_063e:  stloc.s    V_20
    IL_0640:  ldstr      "te3 check failed {0} {1}"
    IL_0645:  ldloc.3
    IL_0646:  box        [mscorlib]System.Int32
    IL_064b:  ldloc.s    V_13
    IL_064d:  box        [mscorlib]System.Int32
    IL_0652:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0657:  ldloc.s    V_4
    IL_0659:  ldloc.s    V_14
    IL_065b:  beq.s      IL_0678

    IL_065d:  ldc.i4.1
    IL_065e:  stloc.s    V_20
    IL_0660:  ldstr      "te064 check failed {0} {1}"
    IL_0665:  ldloc.s    V_4
    IL_0667:  box        [mscorlib]System.Int64
    IL_066c:  ldloc.s    V_14
    IL_066e:  box        [mscorlib]System.Int64
    IL_0673:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0678:  ldloc.s    V_5
    IL_067a:  ldloc.s    V_15
    IL_067c:  beq.s      IL_0699

    IL_067e:  ldc.i4.1
    IL_067f:  stloc.s    V_20
    IL_0681:  ldstr      "te164 check failed {0} {1}"
    IL_0686:  ldloc.s    V_5
    IL_0688:  box        [mscorlib]System.Int64
    IL_068d:  ldloc.s    V_15
    IL_068f:  box        [mscorlib]System.Int64
    IL_0694:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0699:  ldloc.s    V_6
    IL_069b:  ldloc.s    V_16
    IL_069d:  beq.s      IL_06ba

    IL_069f:  ldc.i4.1
    IL_06a0:  stloc.s    V_20
    IL_06a2:  ldstr      "te264 check failed {0} {1}"
    IL_06a7:  ldloc.s    V_6
    IL_06a9:  box        [mscorlib]System.Int64
    IL_06ae:  ldloc.s    V_16
    IL_06b0:  box        [mscorlib]System.Int64
    IL_06b5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_06ba:  ldloc.s    V_7
    IL_06bc:  ldloc.s    V_17
    IL_06be:  beq.s      IL_06db

    IL_06c0:  ldc.i4.1
    IL_06c1:  stloc.s    V_20
    IL_06c3:  ldstr      "te364 check failed {0} {1}"
    IL_06c8:  ldloc.s    V_7
    IL_06ca:  box        [mscorlib]System.Int64
    IL_06cf:  ldloc.s    V_17
    IL_06d1:  box        [mscorlib]System.Int64
    IL_06d6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_06db:  ldloc.s    V_8
    IL_06dd:  ldloc.s    V_18
    IL_06df:  beq.s      IL_06fc

    IL_06e1:  ldc.i4.1
    IL_06e2:  stloc.s    V_20
    IL_06e4:  ldstr      "dummy check failed {0} {1}"
    IL_06e9:  ldloc.s    V_8
    IL_06eb:  box        [mscorlib]System.Int32
    IL_06f0:  ldloc.s    V_18
    IL_06f2:  box        [mscorlib]System.Int32
    IL_06f7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_06fc:  ldloc.s    V_9
    IL_06fe:  ldloc.s    V_19
    IL_0700:  beq.s      IL_071d

    IL_0702:  ldc.i4.1
    IL_0703:  stloc.s    V_20
    IL_0705:  ldstr      "dummy64 check failed {0} {1}"
    IL_070a:  ldloc.s    V_9
    IL_070c:  box        [mscorlib]System.Int64
    IL_0711:  ldloc.s    V_19
    IL_0713:  box        [mscorlib]System.Int64
    IL_0718:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_071d:  ldsfld     int32 IntrinsicTest::counter
    IL_0722:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0727:  beq.s      IL_074a

    IL_0729:  ldstr      "counter mismatch {0} {1}"
    IL_072e:  ldsfld     int32 IntrinsicTest::counter
    IL_0733:  box        [mscorlib]System.Int32
    IL_0738:  ldsfld     int32 IntrinsicTest::id_counter
    IL_073d:  box        [mscorlib]System.Int32
    IL_0742:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0747:  ldc.i4.1
    IL_0748:  stloc.s    V_20
    IL_074a:  ldsfld     int64 IntrinsicTest::counter64
    IL_074f:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0754:  beq.s      IL_0777

    IL_0756:  ldstr      "counter64 mismatch {0} {1}"
    IL_075b:  ldsfld     int64 IntrinsicTest::counter64
    IL_0760:  box        [mscorlib]System.Int64
    IL_0765:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_076a:  box        [mscorlib]System.Int64
    IL_076f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0774:  ldc.i4.1
    IL_0775:  stloc.s    V_20
    IL_0777:  ldloc.s    V_21
    IL_0779:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_077e:  ldloc.s    V_21
    IL_0780:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_0785:  beq.s      IL_07ac

    IL_0787:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_078c:  ldloc.s    V_21
    IL_078e:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_0793:  box        [mscorlib]System.Int32
    IL_0798:  ldloc.s    V_21
    IL_079a:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_079f:  box        [mscorlib]System.Int32
    IL_07a4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_07a9:  ldc.i4.1
    IL_07aa:  stloc.s    V_20
    IL_07ac:  ldloc.s    V_21
    IL_07ae:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_07b3:  ldloc.s    V_21
    IL_07b5:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_07ba:  beq.s      IL_07e1

    IL_07bc:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_07c1:  ldloc.s    V_21
    IL_07c3:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_07c8:  box        [mscorlib]System.Int64
    IL_07cd:  ldloc.s    V_21
    IL_07cf:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_07d4:  box        [mscorlib]System.Int64
    IL_07d9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_07de:  ldc.i4.1
    IL_07df:  stloc.s    V_20
    IL_07e1:  ldsfld     int64 IntrinsicTest::temp
    IL_07e6:  ldsfld     int64 IntrinsicTest::idmp
    IL_07eb:  beq.s      IL_080e

    IL_07ed:  ldstr      "temp mismatch {0} {1}"
    IL_07f2:  ldsfld     int64 IntrinsicTest::temp
    IL_07f7:  box        [mscorlib]System.Int64
    IL_07fc:  ldsfld     int64 IntrinsicTest::idmp
    IL_0801:  box        [mscorlib]System.Int64
    IL_0806:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_080b:  ldc.i4.1
    IL_080c:  stloc.s    V_20
    IL_080e:  ldsflda    int64 IntrinsicTest::counter64
    IL_0813:  ldsfld     int64 IntrinsicTest::counter64
    IL_0818:  ldloc.s    V_4
    IL_081a:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_081f:  stloc.s    V_4
    IL_0821:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0826:  stloc.s    V_14
    IL_0828:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_082d:  ldloc.s    V_14
    IL_082f:  bne.un.s   IL_083d

    IL_0831:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0836:  stsfld     int64 IntrinsicTest::id_counter64
    IL_083b:  br.s       IL_0847

    IL_083d:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0842:  stsfld     int64 IntrinsicTest::idjunk
    IL_0847:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 0 3"
    IL_084c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0851:  ldloc.0
    IL_0852:  ldloc.s    V_10
    IL_0854:  beq.s      IL_0870

    IL_0856:  ldc.i4.1
    IL_0857:  stloc.s    V_20
    IL_0859:  ldstr      "te0 check failed {0} {1}"
    IL_085e:  ldloc.0
    IL_085f:  box        [mscorlib]System.Int32
    IL_0864:  ldloc.s    V_10
    IL_0866:  box        [mscorlib]System.Int32
    IL_086b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0870:  ldloc.1
    IL_0871:  ldloc.s    V_11
    IL_0873:  beq.s      IL_088f

    IL_0875:  ldc.i4.1
    IL_0876:  stloc.s    V_20
    IL_0878:  ldstr      "te1 check failed {0} {1}"
    IL_087d:  ldloc.1
    IL_087e:  box        [mscorlib]System.Int32
    IL_0883:  ldloc.s    V_11
    IL_0885:  box        [mscorlib]System.Int32
    IL_088a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_088f:  ldloc.2
    IL_0890:  ldloc.s    V_12
    IL_0892:  beq.s      IL_08ae

    IL_0894:  ldc.i4.1
    IL_0895:  stloc.s    V_20
    IL_0897:  ldstr      "te2 check failed {0} {1}"
    IL_089c:  ldloc.2
    IL_089d:  box        [mscorlib]System.Int32
    IL_08a2:  ldloc.s    V_12
    IL_08a4:  box        [mscorlib]System.Int32
    IL_08a9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_08ae:  ldloc.3
    IL_08af:  ldloc.s    V_13
    IL_08b1:  beq.s      IL_08cd

    IL_08b3:  ldc.i4.1
    IL_08b4:  stloc.s    V_20
    IL_08b6:  ldstr      "te3 check failed {0} {1}"
    IL_08bb:  ldloc.3
    IL_08bc:  box        [mscorlib]System.Int32
    IL_08c1:  ldloc.s    V_13
    IL_08c3:  box        [mscorlib]System.Int32
    IL_08c8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_08cd:  ldloc.s    V_4
    IL_08cf:  ldloc.s    V_14
    IL_08d1:  beq.s      IL_08ee

    IL_08d3:  ldc.i4.1
    IL_08d4:  stloc.s    V_20
    IL_08d6:  ldstr      "te064 check failed {0} {1}"
    IL_08db:  ldloc.s    V_4
    IL_08dd:  box        [mscorlib]System.Int64
    IL_08e2:  ldloc.s    V_14
    IL_08e4:  box        [mscorlib]System.Int64
    IL_08e9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_08ee:  ldloc.s    V_5
    IL_08f0:  ldloc.s    V_15
    IL_08f2:  beq.s      IL_090f

    IL_08f4:  ldc.i4.1
    IL_08f5:  stloc.s    V_20
    IL_08f7:  ldstr      "te164 check failed {0} {1}"
    IL_08fc:  ldloc.s    V_5
    IL_08fe:  box        [mscorlib]System.Int64
    IL_0903:  ldloc.s    V_15
    IL_0905:  box        [mscorlib]System.Int64
    IL_090a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_090f:  ldloc.s    V_6
    IL_0911:  ldloc.s    V_16
    IL_0913:  beq.s      IL_0930

    IL_0915:  ldc.i4.1
    IL_0916:  stloc.s    V_20
    IL_0918:  ldstr      "te264 check failed {0} {1}"
    IL_091d:  ldloc.s    V_6
    IL_091f:  box        [mscorlib]System.Int64
    IL_0924:  ldloc.s    V_16
    IL_0926:  box        [mscorlib]System.Int64
    IL_092b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0930:  ldloc.s    V_7
    IL_0932:  ldloc.s    V_17
    IL_0934:  beq.s      IL_0951

    IL_0936:  ldc.i4.1
    IL_0937:  stloc.s    V_20
    IL_0939:  ldstr      "te364 check failed {0} {1}"
    IL_093e:  ldloc.s    V_7
    IL_0940:  box        [mscorlib]System.Int64
    IL_0945:  ldloc.s    V_17
    IL_0947:  box        [mscorlib]System.Int64
    IL_094c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0951:  ldloc.s    V_8
    IL_0953:  ldloc.s    V_18
    IL_0955:  beq.s      IL_0972

    IL_0957:  ldc.i4.1
    IL_0958:  stloc.s    V_20
    IL_095a:  ldstr      "dummy check failed {0} {1}"
    IL_095f:  ldloc.s    V_8
    IL_0961:  box        [mscorlib]System.Int32
    IL_0966:  ldloc.s    V_18
    IL_0968:  box        [mscorlib]System.Int32
    IL_096d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0972:  ldloc.s    V_9
    IL_0974:  ldloc.s    V_19
    IL_0976:  beq.s      IL_0993

    IL_0978:  ldc.i4.1
    IL_0979:  stloc.s    V_20
    IL_097b:  ldstr      "dummy64 check failed {0} {1}"
    IL_0980:  ldloc.s    V_9
    IL_0982:  box        [mscorlib]System.Int64
    IL_0987:  ldloc.s    V_19
    IL_0989:  box        [mscorlib]System.Int64
    IL_098e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0993:  ldsfld     int32 IntrinsicTest::counter
    IL_0998:  ldsfld     int32 IntrinsicTest::id_counter
    IL_099d:  beq.s      IL_09c0

    IL_099f:  ldstr      "counter mismatch {0} {1}"
    IL_09a4:  ldsfld     int32 IntrinsicTest::counter
    IL_09a9:  box        [mscorlib]System.Int32
    IL_09ae:  ldsfld     int32 IntrinsicTest::id_counter
    IL_09b3:  box        [mscorlib]System.Int32
    IL_09b8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_09bd:  ldc.i4.1
    IL_09be:  stloc.s    V_20
    IL_09c0:  ldsfld     int64 IntrinsicTest::counter64
    IL_09c5:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_09ca:  beq.s      IL_09ed

    IL_09cc:  ldstr      "counter64 mismatch {0} {1}"
    IL_09d1:  ldsfld     int64 IntrinsicTest::counter64
    IL_09d6:  box        [mscorlib]System.Int64
    IL_09db:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_09e0:  box        [mscorlib]System.Int64
    IL_09e5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_09ea:  ldc.i4.1
    IL_09eb:  stloc.s    V_20
    IL_09ed:  ldloc.s    V_21
    IL_09ef:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_09f4:  ldloc.s    V_21
    IL_09f6:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_09fb:  beq.s      IL_0a22

    IL_09fd:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_0a02:  ldloc.s    V_21
    IL_0a04:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_0a09:  box        [mscorlib]System.Int32
    IL_0a0e:  ldloc.s    V_21
    IL_0a10:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_0a15:  box        [mscorlib]System.Int32
    IL_0a1a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0a1f:  ldc.i4.1
    IL_0a20:  stloc.s    V_20
    IL_0a22:  ldloc.s    V_21
    IL_0a24:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_0a29:  ldloc.s    V_21
    IL_0a2b:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_0a30:  beq.s      IL_0a57

    IL_0a32:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_0a37:  ldloc.s    V_21
    IL_0a39:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_0a3e:  box        [mscorlib]System.Int64
    IL_0a43:  ldloc.s    V_21
    IL_0a45:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_0a4a:  box        [mscorlib]System.Int64
    IL_0a4f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0a54:  ldc.i4.1
    IL_0a55:  stloc.s    V_20
    IL_0a57:  ldsfld     int64 IntrinsicTest::temp
    IL_0a5c:  ldsfld     int64 IntrinsicTest::idmp
    IL_0a61:  beq.s      IL_0a84

    IL_0a63:  ldstr      "temp mismatch {0} {1}"
    IL_0a68:  ldsfld     int64 IntrinsicTest::temp
    IL_0a6d:  box        [mscorlib]System.Int64
    IL_0a72:  ldsfld     int64 IntrinsicTest::idmp
    IL_0a77:  box        [mscorlib]System.Int64
    IL_0a7c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0a81:  ldc.i4.1
    IL_0a82:  stloc.s    V_20
    IL_0a84:  ldsflda    int64 IntrinsicTest::counter64
    IL_0a89:  ldsfld     int64 IntrinsicTest::counter64
    IL_0a8e:  ldloc.s    V_4
    IL_0a90:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_0a95:  stsfld     int64 IntrinsicTest::counter64
    IL_0a9a:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0a9f:  stsfld     int64 IntrinsicTest::id_counter64
    IL_0aa4:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0aa9:  ldloc.s    V_14
    IL_0aab:  bne.un.s   IL_0ab9

    IL_0aad:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0ab2:  stsfld     int64 IntrinsicTest::idjunk
    IL_0ab7:  br.s       IL_0ac3

    IL_0ab9:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0abe:  stsfld     int64 IntrinsicTest::idjunk
    IL_0ac3:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 0 4"
    IL_0ac8:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0acd:  ldloc.0
    IL_0ace:  ldloc.s    V_10
    IL_0ad0:  beq.s      IL_0aec

    IL_0ad2:  ldc.i4.1
    IL_0ad3:  stloc.s    V_20
    IL_0ad5:  ldstr      "te0 check failed {0} {1}"
    IL_0ada:  ldloc.0
    IL_0adb:  box        [mscorlib]System.Int32
    IL_0ae0:  ldloc.s    V_10
    IL_0ae2:  box        [mscorlib]System.Int32
    IL_0ae7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0aec:  ldloc.1
    IL_0aed:  ldloc.s    V_11
    IL_0aef:  beq.s      IL_0b0b

    IL_0af1:  ldc.i4.1
    IL_0af2:  stloc.s    V_20
    IL_0af4:  ldstr      "te1 check failed {0} {1}"
    IL_0af9:  ldloc.1
    IL_0afa:  box        [mscorlib]System.Int32
    IL_0aff:  ldloc.s    V_11
    IL_0b01:  box        [mscorlib]System.Int32
    IL_0b06:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0b0b:  ldloc.2
    IL_0b0c:  ldloc.s    V_12
    IL_0b0e:  beq.s      IL_0b2a

    IL_0b10:  ldc.i4.1
    IL_0b11:  stloc.s    V_20
    IL_0b13:  ldstr      "te2 check failed {0} {1}"
    IL_0b18:  ldloc.2
    IL_0b19:  box        [mscorlib]System.Int32
    IL_0b1e:  ldloc.s    V_12
    IL_0b20:  box        [mscorlib]System.Int32
    IL_0b25:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0b2a:  ldloc.3
    IL_0b2b:  ldloc.s    V_13
    IL_0b2d:  beq.s      IL_0b49

    IL_0b2f:  ldc.i4.1
    IL_0b30:  stloc.s    V_20
    IL_0b32:  ldstr      "te3 check failed {0} {1}"
    IL_0b37:  ldloc.3
    IL_0b38:  box        [mscorlib]System.Int32
    IL_0b3d:  ldloc.s    V_13
    IL_0b3f:  box        [mscorlib]System.Int32
    IL_0b44:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0b49:  ldloc.s    V_4
    IL_0b4b:  ldloc.s    V_14
    IL_0b4d:  beq.s      IL_0b6a

    IL_0b4f:  ldc.i4.1
    IL_0b50:  stloc.s    V_20
    IL_0b52:  ldstr      "te064 check failed {0} {1}"
    IL_0b57:  ldloc.s    V_4
    IL_0b59:  box        [mscorlib]System.Int64
    IL_0b5e:  ldloc.s    V_14
    IL_0b60:  box        [mscorlib]System.Int64
    IL_0b65:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0b6a:  ldloc.s    V_5
    IL_0b6c:  ldloc.s    V_15
    IL_0b6e:  beq.s      IL_0b8b

    IL_0b70:  ldc.i4.1
    IL_0b71:  stloc.s    V_20
    IL_0b73:  ldstr      "te164 check failed {0} {1}"
    IL_0b78:  ldloc.s    V_5
    IL_0b7a:  box        [mscorlib]System.Int64
    IL_0b7f:  ldloc.s    V_15
    IL_0b81:  box        [mscorlib]System.Int64
    IL_0b86:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0b8b:  ldloc.s    V_6
    IL_0b8d:  ldloc.s    V_16
    IL_0b8f:  beq.s      IL_0bac

    IL_0b91:  ldc.i4.1
    IL_0b92:  stloc.s    V_20
    IL_0b94:  ldstr      "te264 check failed {0} {1}"
    IL_0b99:  ldloc.s    V_6
    IL_0b9b:  box        [mscorlib]System.Int64
    IL_0ba0:  ldloc.s    V_16
    IL_0ba2:  box        [mscorlib]System.Int64
    IL_0ba7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0bac:  ldloc.s    V_7
    IL_0bae:  ldloc.s    V_17
    IL_0bb0:  beq.s      IL_0bcd

    IL_0bb2:  ldc.i4.1
    IL_0bb3:  stloc.s    V_20
    IL_0bb5:  ldstr      "te364 check failed {0} {1}"
    IL_0bba:  ldloc.s    V_7
    IL_0bbc:  box        [mscorlib]System.Int64
    IL_0bc1:  ldloc.s    V_17
    IL_0bc3:  box        [mscorlib]System.Int64
    IL_0bc8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0bcd:  ldloc.s    V_8
    IL_0bcf:  ldloc.s    V_18
    IL_0bd1:  beq.s      IL_0bee

    IL_0bd3:  ldc.i4.1
    IL_0bd4:  stloc.s    V_20
    IL_0bd6:  ldstr      "dummy check failed {0} {1}"
    IL_0bdb:  ldloc.s    V_8
    IL_0bdd:  box        [mscorlib]System.Int32
    IL_0be2:  ldloc.s    V_18
    IL_0be4:  box        [mscorlib]System.Int32
    IL_0be9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0bee:  ldloc.s    V_9
    IL_0bf0:  ldloc.s    V_19
    IL_0bf2:  beq.s      IL_0c0f

    IL_0bf4:  ldc.i4.1
    IL_0bf5:  stloc.s    V_20
    IL_0bf7:  ldstr      "dummy64 check failed {0} {1}"
    IL_0bfc:  ldloc.s    V_9
    IL_0bfe:  box        [mscorlib]System.Int64
    IL_0c03:  ldloc.s    V_19
    IL_0c05:  box        [mscorlib]System.Int64
    IL_0c0a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0c0f:  ldsfld     int32 IntrinsicTest::counter
    IL_0c14:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0c19:  beq.s      IL_0c3c

    IL_0c1b:  ldstr      "counter mismatch {0} {1}"
    IL_0c20:  ldsfld     int32 IntrinsicTest::counter
    IL_0c25:  box        [mscorlib]System.Int32
    IL_0c2a:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0c2f:  box        [mscorlib]System.Int32
    IL_0c34:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0c39:  ldc.i4.1
    IL_0c3a:  stloc.s    V_20
    IL_0c3c:  ldsfld     int64 IntrinsicTest::counter64
    IL_0c41:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0c46:  beq.s      IL_0c69

    IL_0c48:  ldstr      "counter64 mismatch {0} {1}"
    IL_0c4d:  ldsfld     int64 IntrinsicTest::counter64
    IL_0c52:  box        [mscorlib]System.Int64
    IL_0c57:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0c5c:  box        [mscorlib]System.Int64
    IL_0c61:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0c66:  ldc.i4.1
    IL_0c67:  stloc.s    V_20
    IL_0c69:  ldloc.s    V_21
    IL_0c6b:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_0c70:  ldloc.s    V_21
    IL_0c72:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_0c77:  beq.s      IL_0c9e

    IL_0c79:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_0c7e:  ldloc.s    V_21
    IL_0c80:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_0c85:  box        [mscorlib]System.Int32
    IL_0c8a:  ldloc.s    V_21
    IL_0c8c:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_0c91:  box        [mscorlib]System.Int32
    IL_0c96:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0c9b:  ldc.i4.1
    IL_0c9c:  stloc.s    V_20
    IL_0c9e:  ldloc.s    V_21
    IL_0ca0:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_0ca5:  ldloc.s    V_21
    IL_0ca7:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_0cac:  beq.s      IL_0cd3

    IL_0cae:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_0cb3:  ldloc.s    V_21
    IL_0cb5:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_0cba:  box        [mscorlib]System.Int64
    IL_0cbf:  ldloc.s    V_21
    IL_0cc1:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_0cc6:  box        [mscorlib]System.Int64
    IL_0ccb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0cd0:  ldc.i4.1
    IL_0cd1:  stloc.s    V_20
    IL_0cd3:  ldsfld     int64 IntrinsicTest::temp
    IL_0cd8:  ldsfld     int64 IntrinsicTest::idmp
    IL_0cdd:  beq.s      IL_0d00

    IL_0cdf:  ldstr      "temp mismatch {0} {1}"
    IL_0ce4:  ldsfld     int64 IntrinsicTest::temp
    IL_0ce9:  box        [mscorlib]System.Int64
    IL_0cee:  ldsfld     int64 IntrinsicTest::idmp
    IL_0cf3:  box        [mscorlib]System.Int64
    IL_0cf8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0cfd:  ldc.i4.1
    IL_0cfe:  stloc.s    V_20
    IL_0d00:  ldloc.s    V_21
    IL_0d02:  ldsflda    int64 IntrinsicTest::counter64
    IL_0d07:  ldsfld     int64 IntrinsicTest::counter64
    IL_0d0c:  ldloc.s    V_4
    IL_0d0e:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_0d13:  stfld      int64 IntrinsicTest::instanceCounter64
    IL_0d18:  ldloc.s    V_21
    IL_0d1a:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0d1f:  stfld      int64 IntrinsicTest::id_instanceCounter64
    IL_0d24:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0d29:  ldloc.s    V_14
    IL_0d2b:  bne.un.s   IL_0d39

    IL_0d2d:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0d32:  stsfld     int64 IntrinsicTest::id_counter64
    IL_0d37:  br.s       IL_0d43

    IL_0d39:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0d3e:  stsfld     int64 IntrinsicTest::idjunk
    IL_0d43:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 0 5"
    IL_0d48:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0d4d:  ldloc.0
    IL_0d4e:  ldloc.s    V_10
    IL_0d50:  beq.s      IL_0d6c

    IL_0d52:  ldc.i4.1
    IL_0d53:  stloc.s    V_20
    IL_0d55:  ldstr      "te0 check failed {0} {1}"
    IL_0d5a:  ldloc.0
    IL_0d5b:  box        [mscorlib]System.Int32
    IL_0d60:  ldloc.s    V_10
    IL_0d62:  box        [mscorlib]System.Int32
    IL_0d67:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0d6c:  ldloc.1
    IL_0d6d:  ldloc.s    V_11
    IL_0d6f:  beq.s      IL_0d8b

    IL_0d71:  ldc.i4.1
    IL_0d72:  stloc.s    V_20
    IL_0d74:  ldstr      "te1 check failed {0} {1}"
    IL_0d79:  ldloc.1
    IL_0d7a:  box        [mscorlib]System.Int32
    IL_0d7f:  ldloc.s    V_11
    IL_0d81:  box        [mscorlib]System.Int32
    IL_0d86:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0d8b:  ldloc.2
    IL_0d8c:  ldloc.s    V_12
    IL_0d8e:  beq.s      IL_0daa

    IL_0d90:  ldc.i4.1
    IL_0d91:  stloc.s    V_20
    IL_0d93:  ldstr      "te2 check failed {0} {1}"
    IL_0d98:  ldloc.2
    IL_0d99:  box        [mscorlib]System.Int32
    IL_0d9e:  ldloc.s    V_12
    IL_0da0:  box        [mscorlib]System.Int32
    IL_0da5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0daa:  ldloc.3
    IL_0dab:  ldloc.s    V_13
    IL_0dad:  beq.s      IL_0dc9

    IL_0daf:  ldc.i4.1
    IL_0db0:  stloc.s    V_20
    IL_0db2:  ldstr      "te3 check failed {0} {1}"
    IL_0db7:  ldloc.3
    IL_0db8:  box        [mscorlib]System.Int32
    IL_0dbd:  ldloc.s    V_13
    IL_0dbf:  box        [mscorlib]System.Int32
    IL_0dc4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0dc9:  ldloc.s    V_4
    IL_0dcb:  ldloc.s    V_14
    IL_0dcd:  beq.s      IL_0dea

    IL_0dcf:  ldc.i4.1
    IL_0dd0:  stloc.s    V_20
    IL_0dd2:  ldstr      "te064 check failed {0} {1}"
    IL_0dd7:  ldloc.s    V_4
    IL_0dd9:  box        [mscorlib]System.Int64
    IL_0dde:  ldloc.s    V_14
    IL_0de0:  box        [mscorlib]System.Int64
    IL_0de5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0dea:  ldloc.s    V_5
    IL_0dec:  ldloc.s    V_15
    IL_0dee:  beq.s      IL_0e0b

    IL_0df0:  ldc.i4.1
    IL_0df1:  stloc.s    V_20
    IL_0df3:  ldstr      "te164 check failed {0} {1}"
    IL_0df8:  ldloc.s    V_5
    IL_0dfa:  box        [mscorlib]System.Int64
    IL_0dff:  ldloc.s    V_15
    IL_0e01:  box        [mscorlib]System.Int64
    IL_0e06:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0e0b:  ldloc.s    V_6
    IL_0e0d:  ldloc.s    V_16
    IL_0e0f:  beq.s      IL_0e2c

    IL_0e11:  ldc.i4.1
    IL_0e12:  stloc.s    V_20
    IL_0e14:  ldstr      "te264 check failed {0} {1}"
    IL_0e19:  ldloc.s    V_6
    IL_0e1b:  box        [mscorlib]System.Int64
    IL_0e20:  ldloc.s    V_16
    IL_0e22:  box        [mscorlib]System.Int64
    IL_0e27:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0e2c:  ldloc.s    V_7
    IL_0e2e:  ldloc.s    V_17
    IL_0e30:  beq.s      IL_0e4d

    IL_0e32:  ldc.i4.1
    IL_0e33:  stloc.s    V_20
    IL_0e35:  ldstr      "te364 check failed {0} {1}"
    IL_0e3a:  ldloc.s    V_7
    IL_0e3c:  box        [mscorlib]System.Int64
    IL_0e41:  ldloc.s    V_17
    IL_0e43:  box        [mscorlib]System.Int64
    IL_0e48:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0e4d:  ldloc.s    V_8
    IL_0e4f:  ldloc.s    V_18
    IL_0e51:  beq.s      IL_0e6e

    IL_0e53:  ldc.i4.1
    IL_0e54:  stloc.s    V_20
    IL_0e56:  ldstr      "dummy check failed {0} {1}"
    IL_0e5b:  ldloc.s    V_8
    IL_0e5d:  box        [mscorlib]System.Int32
    IL_0e62:  ldloc.s    V_18
    IL_0e64:  box        [mscorlib]System.Int32
    IL_0e69:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0e6e:  ldloc.s    V_9
    IL_0e70:  ldloc.s    V_19
    IL_0e72:  beq.s      IL_0e8f

    IL_0e74:  ldc.i4.1
    IL_0e75:  stloc.s    V_20
    IL_0e77:  ldstr      "dummy64 check failed {0} {1}"
    IL_0e7c:  ldloc.s    V_9
    IL_0e7e:  box        [mscorlib]System.Int64
    IL_0e83:  ldloc.s    V_19
    IL_0e85:  box        [mscorlib]System.Int64
    IL_0e8a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0e8f:  ldsfld     int32 IntrinsicTest::counter
    IL_0e94:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0e99:  beq.s      IL_0ebc

    IL_0e9b:  ldstr      "counter mismatch {0} {1}"
    IL_0ea0:  ldsfld     int32 IntrinsicTest::counter
    IL_0ea5:  box        [mscorlib]System.Int32
    IL_0eaa:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0eaf:  box        [mscorlib]System.Int32
    IL_0eb4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0eb9:  ldc.i4.1
    IL_0eba:  stloc.s    V_20
    IL_0ebc:  ldsfld     int64 IntrinsicTest::counter64
    IL_0ec1:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0ec6:  beq.s      IL_0ee9

    IL_0ec8:  ldstr      "counter64 mismatch {0} {1}"
    IL_0ecd:  ldsfld     int64 IntrinsicTest::counter64
    IL_0ed2:  box        [mscorlib]System.Int64
    IL_0ed7:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0edc:  box        [mscorlib]System.Int64
    IL_0ee1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0ee6:  ldc.i4.1
    IL_0ee7:  stloc.s    V_20
    IL_0ee9:  ldloc.s    V_21
    IL_0eeb:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_0ef0:  ldloc.s    V_21
    IL_0ef2:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_0ef7:  beq.s      IL_0f1e

    IL_0ef9:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_0efe:  ldloc.s    V_21
    IL_0f00:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_0f05:  box        [mscorlib]System.Int32
    IL_0f0a:  ldloc.s    V_21
    IL_0f0c:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_0f11:  box        [mscorlib]System.Int32
    IL_0f16:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0f1b:  ldc.i4.1
    IL_0f1c:  stloc.s    V_20
    IL_0f1e:  ldloc.s    V_21
    IL_0f20:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_0f25:  ldloc.s    V_21
    IL_0f27:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_0f2c:  beq.s      IL_0f53

    IL_0f2e:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_0f33:  ldloc.s    V_21
    IL_0f35:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_0f3a:  box        [mscorlib]System.Int64
    IL_0f3f:  ldloc.s    V_21
    IL_0f41:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_0f46:  box        [mscorlib]System.Int64
    IL_0f4b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0f50:  ldc.i4.1
    IL_0f51:  stloc.s    V_20
    IL_0f53:  ldsfld     int64 IntrinsicTest::temp
    IL_0f58:  ldsfld     int64 IntrinsicTest::idmp
    IL_0f5d:  beq.s      IL_0f80

    IL_0f5f:  ldstr      "temp mismatch {0} {1}"
    IL_0f64:  ldsfld     int64 IntrinsicTest::temp
    IL_0f69:  box        [mscorlib]System.Int64
    IL_0f6e:  ldsfld     int64 IntrinsicTest::idmp
    IL_0f73:  box        [mscorlib]System.Int64
    IL_0f78:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0f7d:  ldc.i4.1
    IL_0f7e:  stloc.s    V_20
    IL_0f80:  ldsflda    int64 IntrinsicTest::counter64
    IL_0f85:  ldsfld     int64 IntrinsicTest::counter64
    IL_0f8a:  ldloc.s    V_4
    IL_0f8c:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_0f91:  stloc.s    V_7
    IL_0f93:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0f98:  stloc.s    V_17
    IL_0f9a:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0f9f:  ldloc.s    V_14
    IL_0fa1:  bne.un.s   IL_0faf

    IL_0fa3:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0fa8:  stsfld     int64 IntrinsicTest::id_counter64
    IL_0fad:  br.s       IL_0fb9

    IL_0faf:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0fb4:  stsfld     int64 IntrinsicTest::idjunk
    IL_0fb9:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 0 6"
    IL_0fbe:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0fc3:  ldloc.0
    IL_0fc4:  ldloc.s    V_10
    IL_0fc6:  beq.s      IL_0fe2

    IL_0fc8:  ldc.i4.1
    IL_0fc9:  stloc.s    V_20
    IL_0fcb:  ldstr      "te0 check failed {0} {1}"
    IL_0fd0:  ldloc.0
    IL_0fd1:  box        [mscorlib]System.Int32
    IL_0fd6:  ldloc.s    V_10
    IL_0fd8:  box        [mscorlib]System.Int32
    IL_0fdd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0fe2:  ldloc.1
    IL_0fe3:  ldloc.s    V_11
    IL_0fe5:  beq.s      IL_1001

    IL_0fe7:  ldc.i4.1
    IL_0fe8:  stloc.s    V_20
    IL_0fea:  ldstr      "te1 check failed {0} {1}"
    IL_0fef:  ldloc.1
    IL_0ff0:  box        [mscorlib]System.Int32
    IL_0ff5:  ldloc.s    V_11
    IL_0ff7:  box        [mscorlib]System.Int32
    IL_0ffc:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1001:  ldloc.2
    IL_1002:  ldloc.s    V_12
    IL_1004:  beq.s      IL_1020

    IL_1006:  ldc.i4.1
    IL_1007:  stloc.s    V_20
    IL_1009:  ldstr      "te2 check failed {0} {1}"
    IL_100e:  ldloc.2
    IL_100f:  box        [mscorlib]System.Int32
    IL_1014:  ldloc.s    V_12
    IL_1016:  box        [mscorlib]System.Int32
    IL_101b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1020:  ldloc.3
    IL_1021:  ldloc.s    V_13
    IL_1023:  beq.s      IL_103f

    IL_1025:  ldc.i4.1
    IL_1026:  stloc.s    V_20
    IL_1028:  ldstr      "te3 check failed {0} {1}"
    IL_102d:  ldloc.3
    IL_102e:  box        [mscorlib]System.Int32
    IL_1033:  ldloc.s    V_13
    IL_1035:  box        [mscorlib]System.Int32
    IL_103a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_103f:  ldloc.s    V_4
    IL_1041:  ldloc.s    V_14
    IL_1043:  beq.s      IL_1060

    IL_1045:  ldc.i4.1
    IL_1046:  stloc.s    V_20
    IL_1048:  ldstr      "te064 check failed {0} {1}"
    IL_104d:  ldloc.s    V_4
    IL_104f:  box        [mscorlib]System.Int64
    IL_1054:  ldloc.s    V_14
    IL_1056:  box        [mscorlib]System.Int64
    IL_105b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1060:  ldloc.s    V_5
    IL_1062:  ldloc.s    V_15
    IL_1064:  beq.s      IL_1081

    IL_1066:  ldc.i4.1
    IL_1067:  stloc.s    V_20
    IL_1069:  ldstr      "te164 check failed {0} {1}"
    IL_106e:  ldloc.s    V_5
    IL_1070:  box        [mscorlib]System.Int64
    IL_1075:  ldloc.s    V_15
    IL_1077:  box        [mscorlib]System.Int64
    IL_107c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1081:  ldloc.s    V_6
    IL_1083:  ldloc.s    V_16
    IL_1085:  beq.s      IL_10a2

    IL_1087:  ldc.i4.1
    IL_1088:  stloc.s    V_20
    IL_108a:  ldstr      "te264 check failed {0} {1}"
    IL_108f:  ldloc.s    V_6
    IL_1091:  box        [mscorlib]System.Int64
    IL_1096:  ldloc.s    V_16
    IL_1098:  box        [mscorlib]System.Int64
    IL_109d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_10a2:  ldloc.s    V_7
    IL_10a4:  ldloc.s    V_17
    IL_10a6:  beq.s      IL_10c3

    IL_10a8:  ldc.i4.1
    IL_10a9:  stloc.s    V_20
    IL_10ab:  ldstr      "te364 check failed {0} {1}"
    IL_10b0:  ldloc.s    V_7
    IL_10b2:  box        [mscorlib]System.Int64
    IL_10b7:  ldloc.s    V_17
    IL_10b9:  box        [mscorlib]System.Int64
    IL_10be:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_10c3:  ldloc.s    V_8
    IL_10c5:  ldloc.s    V_18
    IL_10c7:  beq.s      IL_10e4

    IL_10c9:  ldc.i4.1
    IL_10ca:  stloc.s    V_20
    IL_10cc:  ldstr      "dummy check failed {0} {1}"
    IL_10d1:  ldloc.s    V_8
    IL_10d3:  box        [mscorlib]System.Int32
    IL_10d8:  ldloc.s    V_18
    IL_10da:  box        [mscorlib]System.Int32
    IL_10df:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_10e4:  ldloc.s    V_9
    IL_10e6:  ldloc.s    V_19
    IL_10e8:  beq.s      IL_1105

    IL_10ea:  ldc.i4.1
    IL_10eb:  stloc.s    V_20
    IL_10ed:  ldstr      "dummy64 check failed {0} {1}"
    IL_10f2:  ldloc.s    V_9
    IL_10f4:  box        [mscorlib]System.Int64
    IL_10f9:  ldloc.s    V_19
    IL_10fb:  box        [mscorlib]System.Int64
    IL_1100:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1105:  ldsfld     int32 IntrinsicTest::counter
    IL_110a:  ldsfld     int32 IntrinsicTest::id_counter
    IL_110f:  beq.s      IL_1132

    IL_1111:  ldstr      "counter mismatch {0} {1}"
    IL_1116:  ldsfld     int32 IntrinsicTest::counter
    IL_111b:  box        [mscorlib]System.Int32
    IL_1120:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1125:  box        [mscorlib]System.Int32
    IL_112a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_112f:  ldc.i4.1
    IL_1130:  stloc.s    V_20
    IL_1132:  ldsfld     int64 IntrinsicTest::counter64
    IL_1137:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_113c:  beq.s      IL_115f

    IL_113e:  ldstr      "counter64 mismatch {0} {1}"
    IL_1143:  ldsfld     int64 IntrinsicTest::counter64
    IL_1148:  box        [mscorlib]System.Int64
    IL_114d:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1152:  box        [mscorlib]System.Int64
    IL_1157:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_115c:  ldc.i4.1
    IL_115d:  stloc.s    V_20
    IL_115f:  ldloc.s    V_21
    IL_1161:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_1166:  ldloc.s    V_21
    IL_1168:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_116d:  beq.s      IL_1194

    IL_116f:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_1174:  ldloc.s    V_21
    IL_1176:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_117b:  box        [mscorlib]System.Int32
    IL_1180:  ldloc.s    V_21
    IL_1182:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_1187:  box        [mscorlib]System.Int32
    IL_118c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1191:  ldc.i4.1
    IL_1192:  stloc.s    V_20
    IL_1194:  ldloc.s    V_21
    IL_1196:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_119b:  ldloc.s    V_21
    IL_119d:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_11a2:  beq.s      IL_11c9

    IL_11a4:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_11a9:  ldloc.s    V_21
    IL_11ab:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_11b0:  box        [mscorlib]System.Int64
    IL_11b5:  ldloc.s    V_21
    IL_11b7:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_11bc:  box        [mscorlib]System.Int64
    IL_11c1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_11c6:  ldc.i4.1
    IL_11c7:  stloc.s    V_20
    IL_11c9:  ldsfld     int64 IntrinsicTest::temp
    IL_11ce:  ldsfld     int64 IntrinsicTest::idmp
    IL_11d3:  beq.s      IL_11f6

    IL_11d5:  ldstr      "temp mismatch {0} {1}"
    IL_11da:  ldsfld     int64 IntrinsicTest::temp
    IL_11df:  box        [mscorlib]System.Int64
    IL_11e4:  ldsfld     int64 IntrinsicTest::idmp
    IL_11e9:  box        [mscorlib]System.Int64
    IL_11ee:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_11f3:  ldc.i4.1
    IL_11f4:  stloc.s    V_20
    IL_11f6:  ldsflda    int64 IntrinsicTest::counter64
    IL_11fb:  ldsfld     int64 IntrinsicTest::counter64
    IL_1200:  ldsfld     int64 IntrinsicTest::counter64
    IL_1205:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_120a:  stloc.s    V_4
    IL_120c:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1211:  stloc.s    V_14
    IL_1213:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1218:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_121d:  bne.un.s   IL_122b

    IL_121f:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1224:  stsfld     int64 IntrinsicTest::id_counter64
    IL_1229:  br.s       IL_1235

    IL_122b:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1230:  stsfld     int64 IntrinsicTest::idjunk
    IL_1235:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 1 0"
    IL_123a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_123f:  ldloc.0
    IL_1240:  ldloc.s    V_10
    IL_1242:  beq.s      IL_125e

    IL_1244:  ldc.i4.1
    IL_1245:  stloc.s    V_20
    IL_1247:  ldstr      "te0 check failed {0} {1}"
    IL_124c:  ldloc.0
    IL_124d:  box        [mscorlib]System.Int32
    IL_1252:  ldloc.s    V_10
    IL_1254:  box        [mscorlib]System.Int32
    IL_1259:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_125e:  ldloc.1
    IL_125f:  ldloc.s    V_11
    IL_1261:  beq.s      IL_127d

    IL_1263:  ldc.i4.1
    IL_1264:  stloc.s    V_20
    IL_1266:  ldstr      "te1 check failed {0} {1}"
    IL_126b:  ldloc.1
    IL_126c:  box        [mscorlib]System.Int32
    IL_1271:  ldloc.s    V_11
    IL_1273:  box        [mscorlib]System.Int32
    IL_1278:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_127d:  ldloc.2
    IL_127e:  ldloc.s    V_12
    IL_1280:  beq.s      IL_129c

    IL_1282:  ldc.i4.1
    IL_1283:  stloc.s    V_20
    IL_1285:  ldstr      "te2 check failed {0} {1}"
    IL_128a:  ldloc.2
    IL_128b:  box        [mscorlib]System.Int32
    IL_1290:  ldloc.s    V_12
    IL_1292:  box        [mscorlib]System.Int32
    IL_1297:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_129c:  ldloc.3
    IL_129d:  ldloc.s    V_13
    IL_129f:  beq.s      IL_12bb

    IL_12a1:  ldc.i4.1
    IL_12a2:  stloc.s    V_20
    IL_12a4:  ldstr      "te3 check failed {0} {1}"
    IL_12a9:  ldloc.3
    IL_12aa:  box        [mscorlib]System.Int32
    IL_12af:  ldloc.s    V_13
    IL_12b1:  box        [mscorlib]System.Int32
    IL_12b6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_12bb:  ldloc.s    V_4
    IL_12bd:  ldloc.s    V_14
    IL_12bf:  beq.s      IL_12dc

    IL_12c1:  ldc.i4.1
    IL_12c2:  stloc.s    V_20
    IL_12c4:  ldstr      "te064 check failed {0} {1}"
    IL_12c9:  ldloc.s    V_4
    IL_12cb:  box        [mscorlib]System.Int64
    IL_12d0:  ldloc.s    V_14
    IL_12d2:  box        [mscorlib]System.Int64
    IL_12d7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_12dc:  ldloc.s    V_5
    IL_12de:  ldloc.s    V_15
    IL_12e0:  beq.s      IL_12fd

    IL_12e2:  ldc.i4.1
    IL_12e3:  stloc.s    V_20
    IL_12e5:  ldstr      "te164 check failed {0} {1}"
    IL_12ea:  ldloc.s    V_5
    IL_12ec:  box        [mscorlib]System.Int64
    IL_12f1:  ldloc.s    V_15
    IL_12f3:  box        [mscorlib]System.Int64
    IL_12f8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_12fd:  ldloc.s    V_6
    IL_12ff:  ldloc.s    V_16
    IL_1301:  beq.s      IL_131e

    IL_1303:  ldc.i4.1
    IL_1304:  stloc.s    V_20
    IL_1306:  ldstr      "te264 check failed {0} {1}"
    IL_130b:  ldloc.s    V_6
    IL_130d:  box        [mscorlib]System.Int64
    IL_1312:  ldloc.s    V_16
    IL_1314:  box        [mscorlib]System.Int64
    IL_1319:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_131e:  ldloc.s    V_7
    IL_1320:  ldloc.s    V_17
    IL_1322:  beq.s      IL_133f

    IL_1324:  ldc.i4.1
    IL_1325:  stloc.s    V_20
    IL_1327:  ldstr      "te364 check failed {0} {1}"
    IL_132c:  ldloc.s    V_7
    IL_132e:  box        [mscorlib]System.Int64
    IL_1333:  ldloc.s    V_17
    IL_1335:  box        [mscorlib]System.Int64
    IL_133a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_133f:  ldloc.s    V_8
    IL_1341:  ldloc.s    V_18
    IL_1343:  beq.s      IL_1360

    IL_1345:  ldc.i4.1
    IL_1346:  stloc.s    V_20
    IL_1348:  ldstr      "dummy check failed {0} {1}"
    IL_134d:  ldloc.s    V_8
    IL_134f:  box        [mscorlib]System.Int32
    IL_1354:  ldloc.s    V_18
    IL_1356:  box        [mscorlib]System.Int32
    IL_135b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1360:  ldloc.s    V_9
    IL_1362:  ldloc.s    V_19
    IL_1364:  beq.s      IL_1381

    IL_1366:  ldc.i4.1
    IL_1367:  stloc.s    V_20
    IL_1369:  ldstr      "dummy64 check failed {0} {1}"
    IL_136e:  ldloc.s    V_9
    IL_1370:  box        [mscorlib]System.Int64
    IL_1375:  ldloc.s    V_19
    IL_1377:  box        [mscorlib]System.Int64
    IL_137c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1381:  ldsfld     int32 IntrinsicTest::counter
    IL_1386:  ldsfld     int32 IntrinsicTest::id_counter
    IL_138b:  beq.s      IL_13ae

    IL_138d:  ldstr      "counter mismatch {0} {1}"
    IL_1392:  ldsfld     int32 IntrinsicTest::counter
    IL_1397:  box        [mscorlib]System.Int32
    IL_139c:  ldsfld     int32 IntrinsicTest::id_counter
    IL_13a1:  box        [mscorlib]System.Int32
    IL_13a6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_13ab:  ldc.i4.1
    IL_13ac:  stloc.s    V_20
    IL_13ae:  ldsfld     int64 IntrinsicTest::counter64
    IL_13b3:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_13b8:  beq.s      IL_13db

    IL_13ba:  ldstr      "counter64 mismatch {0} {1}"
    IL_13bf:  ldsfld     int64 IntrinsicTest::counter64
    IL_13c4:  box        [mscorlib]System.Int64
    IL_13c9:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_13ce:  box        [mscorlib]System.Int64
    IL_13d3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_13d8:  ldc.i4.1
    IL_13d9:  stloc.s    V_20
    IL_13db:  ldloc.s    V_21
    IL_13dd:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_13e2:  ldloc.s    V_21
    IL_13e4:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_13e9:  beq.s      IL_1410

    IL_13eb:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_13f0:  ldloc.s    V_21
    IL_13f2:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_13f7:  box        [mscorlib]System.Int32
    IL_13fc:  ldloc.s    V_21
    IL_13fe:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_1403:  box        [mscorlib]System.Int32
    IL_1408:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_140d:  ldc.i4.1
    IL_140e:  stloc.s    V_20
    IL_1410:  ldloc.s    V_21
    IL_1412:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_1417:  ldloc.s    V_21
    IL_1419:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_141e:  beq.s      IL_1445

    IL_1420:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_1425:  ldloc.s    V_21
    IL_1427:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_142c:  box        [mscorlib]System.Int64
    IL_1431:  ldloc.s    V_21
    IL_1433:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_1438:  box        [mscorlib]System.Int64
    IL_143d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1442:  ldc.i4.1
    IL_1443:  stloc.s    V_20
    IL_1445:  ldsfld     int64 IntrinsicTest::temp
    IL_144a:  ldsfld     int64 IntrinsicTest::idmp
    IL_144f:  beq.s      IL_1472

    IL_1451:  ldstr      "temp mismatch {0} {1}"
    IL_1456:  ldsfld     int64 IntrinsicTest::temp
    IL_145b:  box        [mscorlib]System.Int64
    IL_1460:  ldsfld     int64 IntrinsicTest::idmp
    IL_1465:  box        [mscorlib]System.Int64
    IL_146a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_146f:  ldc.i4.1
    IL_1470:  stloc.s    V_20
    IL_1472:  ldsflda    int64 IntrinsicTest::counter64
    IL_1477:  ldsfld     int64 IntrinsicTest::counter64
    IL_147c:  ldsfld     int64 IntrinsicTest::counter64
    IL_1481:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_1486:  stsfld     int64 IntrinsicTest::counter64
    IL_148b:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1490:  stsfld     int64 IntrinsicTest::id_counter64
    IL_1495:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_149a:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_149f:  bne.un.s   IL_14ad

    IL_14a1:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_14a6:  stsfld     int64 IntrinsicTest::idjunk
    IL_14ab:  br.s       IL_14b7

    IL_14ad:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_14b2:  stsfld     int64 IntrinsicTest::idjunk
    IL_14b7:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 1 1"
    IL_14bc:  call       void [System.Console]System.Console::WriteLine(string)
    IL_14c1:  ldloc.0
    IL_14c2:  ldloc.s    V_10
    IL_14c4:  beq.s      IL_14e0

    IL_14c6:  ldc.i4.1
    IL_14c7:  stloc.s    V_20
    IL_14c9:  ldstr      "te0 check failed {0} {1}"
    IL_14ce:  ldloc.0
    IL_14cf:  box        [mscorlib]System.Int32
    IL_14d4:  ldloc.s    V_10
    IL_14d6:  box        [mscorlib]System.Int32
    IL_14db:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_14e0:  ldloc.1
    IL_14e1:  ldloc.s    V_11
    IL_14e3:  beq.s      IL_14ff

    IL_14e5:  ldc.i4.1
    IL_14e6:  stloc.s    V_20
    IL_14e8:  ldstr      "te1 check failed {0} {1}"
    IL_14ed:  ldloc.1
    IL_14ee:  box        [mscorlib]System.Int32
    IL_14f3:  ldloc.s    V_11
    IL_14f5:  box        [mscorlib]System.Int32
    IL_14fa:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_14ff:  ldloc.2
    IL_1500:  ldloc.s    V_12
    IL_1502:  beq.s      IL_151e

    IL_1504:  ldc.i4.1
    IL_1505:  stloc.s    V_20
    IL_1507:  ldstr      "te2 check failed {0} {1}"
    IL_150c:  ldloc.2
    IL_150d:  box        [mscorlib]System.Int32
    IL_1512:  ldloc.s    V_12
    IL_1514:  box        [mscorlib]System.Int32
    IL_1519:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_151e:  ldloc.3
    IL_151f:  ldloc.s    V_13
    IL_1521:  beq.s      IL_153d

    IL_1523:  ldc.i4.1
    IL_1524:  stloc.s    V_20
    IL_1526:  ldstr      "te3 check failed {0} {1}"
    IL_152b:  ldloc.3
    IL_152c:  box        [mscorlib]System.Int32
    IL_1531:  ldloc.s    V_13
    IL_1533:  box        [mscorlib]System.Int32
    IL_1538:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_153d:  ldloc.s    V_4
    IL_153f:  ldloc.s    V_14
    IL_1541:  beq.s      IL_155e

    IL_1543:  ldc.i4.1
    IL_1544:  stloc.s    V_20
    IL_1546:  ldstr      "te064 check failed {0} {1}"
    IL_154b:  ldloc.s    V_4
    IL_154d:  box        [mscorlib]System.Int64
    IL_1552:  ldloc.s    V_14
    IL_1554:  box        [mscorlib]System.Int64
    IL_1559:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_155e:  ldloc.s    V_5
    IL_1560:  ldloc.s    V_15
    IL_1562:  beq.s      IL_157f

    IL_1564:  ldc.i4.1
    IL_1565:  stloc.s    V_20
    IL_1567:  ldstr      "te164 check failed {0} {1}"
    IL_156c:  ldloc.s    V_5
    IL_156e:  box        [mscorlib]System.Int64
    IL_1573:  ldloc.s    V_15
    IL_1575:  box        [mscorlib]System.Int64
    IL_157a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_157f:  ldloc.s    V_6
    IL_1581:  ldloc.s    V_16
    IL_1583:  beq.s      IL_15a0

    IL_1585:  ldc.i4.1
    IL_1586:  stloc.s    V_20
    IL_1588:  ldstr      "te264 check failed {0} {1}"
    IL_158d:  ldloc.s    V_6
    IL_158f:  box        [mscorlib]System.Int64
    IL_1594:  ldloc.s    V_16
    IL_1596:  box        [mscorlib]System.Int64
    IL_159b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_15a0:  ldloc.s    V_7
    IL_15a2:  ldloc.s    V_17
    IL_15a4:  beq.s      IL_15c1

    IL_15a6:  ldc.i4.1
    IL_15a7:  stloc.s    V_20
    IL_15a9:  ldstr      "te364 check failed {0} {1}"
    IL_15ae:  ldloc.s    V_7
    IL_15b0:  box        [mscorlib]System.Int64
    IL_15b5:  ldloc.s    V_17
    IL_15b7:  box        [mscorlib]System.Int64
    IL_15bc:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_15c1:  ldloc.s    V_8
    IL_15c3:  ldloc.s    V_18
    IL_15c5:  beq.s      IL_15e2

    IL_15c7:  ldc.i4.1
    IL_15c8:  stloc.s    V_20
    IL_15ca:  ldstr      "dummy check failed {0} {1}"
    IL_15cf:  ldloc.s    V_8
    IL_15d1:  box        [mscorlib]System.Int32
    IL_15d6:  ldloc.s    V_18
    IL_15d8:  box        [mscorlib]System.Int32
    IL_15dd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_15e2:  ldloc.s    V_9
    IL_15e4:  ldloc.s    V_19
    IL_15e6:  beq.s      IL_1603

    IL_15e8:  ldc.i4.1
    IL_15e9:  stloc.s    V_20
    IL_15eb:  ldstr      "dummy64 check failed {0} {1}"
    IL_15f0:  ldloc.s    V_9
    IL_15f2:  box        [mscorlib]System.Int64
    IL_15f7:  ldloc.s    V_19
    IL_15f9:  box        [mscorlib]System.Int64
    IL_15fe:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1603:  ldsfld     int32 IntrinsicTest::counter
    IL_1608:  ldsfld     int32 IntrinsicTest::id_counter
    IL_160d:  beq.s      IL_1630

    IL_160f:  ldstr      "counter mismatch {0} {1}"
    IL_1614:  ldsfld     int32 IntrinsicTest::counter
    IL_1619:  box        [mscorlib]System.Int32
    IL_161e:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1623:  box        [mscorlib]System.Int32
    IL_1628:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_162d:  ldc.i4.1
    IL_162e:  stloc.s    V_20
    IL_1630:  ldsfld     int64 IntrinsicTest::counter64
    IL_1635:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_163a:  beq.s      IL_165d

    IL_163c:  ldstr      "counter64 mismatch {0} {1}"
    IL_1641:  ldsfld     int64 IntrinsicTest::counter64
    IL_1646:  box        [mscorlib]System.Int64
    IL_164b:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1650:  box        [mscorlib]System.Int64
    IL_1655:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_165a:  ldc.i4.1
    IL_165b:  stloc.s    V_20
    IL_165d:  ldloc.s    V_21
    IL_165f:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_1664:  ldloc.s    V_21
    IL_1666:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_166b:  beq.s      IL_1692

    IL_166d:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_1672:  ldloc.s    V_21
    IL_1674:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_1679:  box        [mscorlib]System.Int32
    IL_167e:  ldloc.s    V_21
    IL_1680:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_1685:  box        [mscorlib]System.Int32
    IL_168a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_168f:  ldc.i4.1
    IL_1690:  stloc.s    V_20
    IL_1692:  ldloc.s    V_21
    IL_1694:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_1699:  ldloc.s    V_21
    IL_169b:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_16a0:  beq.s      IL_16c7

    IL_16a2:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_16a7:  ldloc.s    V_21
    IL_16a9:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_16ae:  box        [mscorlib]System.Int64
    IL_16b3:  ldloc.s    V_21
    IL_16b5:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_16ba:  box        [mscorlib]System.Int64
    IL_16bf:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_16c4:  ldc.i4.1
    IL_16c5:  stloc.s    V_20
    IL_16c7:  ldsfld     int64 IntrinsicTest::temp
    IL_16cc:  ldsfld     int64 IntrinsicTest::idmp
    IL_16d1:  beq.s      IL_16f4

    IL_16d3:  ldstr      "temp mismatch {0} {1}"
    IL_16d8:  ldsfld     int64 IntrinsicTest::temp
    IL_16dd:  box        [mscorlib]System.Int64
    IL_16e2:  ldsfld     int64 IntrinsicTest::idmp
    IL_16e7:  box        [mscorlib]System.Int64
    IL_16ec:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_16f1:  ldc.i4.1
    IL_16f2:  stloc.s    V_20
    IL_16f4:  ldloc.s    V_21
    IL_16f6:  ldsflda    int64 IntrinsicTest::counter64
    IL_16fb:  ldsfld     int64 IntrinsicTest::counter64
    IL_1700:  ldsfld     int64 IntrinsicTest::counter64
    IL_1705:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_170a:  stfld      int64 IntrinsicTest::instanceCounter64
    IL_170f:  ldloc.s    V_21
    IL_1711:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1716:  stfld      int64 IntrinsicTest::id_instanceCounter64
    IL_171b:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1720:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1725:  bne.un.s   IL_1733

    IL_1727:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_172c:  stsfld     int64 IntrinsicTest::id_counter64
    IL_1731:  br.s       IL_173d

    IL_1733:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1738:  stsfld     int64 IntrinsicTest::idjunk
    IL_173d:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 1 2"
    IL_1742:  call       void [System.Console]System.Console::WriteLine(string)
    IL_1747:  ldloc.0
    IL_1748:  ldloc.s    V_10
    IL_174a:  beq.s      IL_1766

    IL_174c:  ldc.i4.1
    IL_174d:  stloc.s    V_20
    IL_174f:  ldstr      "te0 check failed {0} {1}"
    IL_1754:  ldloc.0
    IL_1755:  box        [mscorlib]System.Int32
    IL_175a:  ldloc.s    V_10
    IL_175c:  box        [mscorlib]System.Int32
    IL_1761:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1766:  ldloc.1
    IL_1767:  ldloc.s    V_11
    IL_1769:  beq.s      IL_1785

    IL_176b:  ldc.i4.1
    IL_176c:  stloc.s    V_20
    IL_176e:  ldstr      "te1 check failed {0} {1}"
    IL_1773:  ldloc.1
    IL_1774:  box        [mscorlib]System.Int32
    IL_1779:  ldloc.s    V_11
    IL_177b:  box        [mscorlib]System.Int32
    IL_1780:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1785:  ldloc.2
    IL_1786:  ldloc.s    V_12
    IL_1788:  beq.s      IL_17a4

    IL_178a:  ldc.i4.1
    IL_178b:  stloc.s    V_20
    IL_178d:  ldstr      "te2 check failed {0} {1}"
    IL_1792:  ldloc.2
    IL_1793:  box        [mscorlib]System.Int32
    IL_1798:  ldloc.s    V_12
    IL_179a:  box        [mscorlib]System.Int32
    IL_179f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_17a4:  ldloc.3
    IL_17a5:  ldloc.s    V_13
    IL_17a7:  beq.s      IL_17c3

    IL_17a9:  ldc.i4.1
    IL_17aa:  stloc.s    V_20
    IL_17ac:  ldstr      "te3 check failed {0} {1}"
    IL_17b1:  ldloc.3
    IL_17b2:  box        [mscorlib]System.Int32
    IL_17b7:  ldloc.s    V_13
    IL_17b9:  box        [mscorlib]System.Int32
    IL_17be:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_17c3:  ldloc.s    V_4
    IL_17c5:  ldloc.s    V_14
    IL_17c7:  beq.s      IL_17e4

    IL_17c9:  ldc.i4.1
    IL_17ca:  stloc.s    V_20
    IL_17cc:  ldstr      "te064 check failed {0} {1}"
    IL_17d1:  ldloc.s    V_4
    IL_17d3:  box        [mscorlib]System.Int64
    IL_17d8:  ldloc.s    V_14
    IL_17da:  box        [mscorlib]System.Int64
    IL_17df:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_17e4:  ldloc.s    V_5
    IL_17e6:  ldloc.s    V_15
    IL_17e8:  beq.s      IL_1805

    IL_17ea:  ldc.i4.1
    IL_17eb:  stloc.s    V_20
    IL_17ed:  ldstr      "te164 check failed {0} {1}"
    IL_17f2:  ldloc.s    V_5
    IL_17f4:  box        [mscorlib]System.Int64
    IL_17f9:  ldloc.s    V_15
    IL_17fb:  box        [mscorlib]System.Int64
    IL_1800:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1805:  ldloc.s    V_6
    IL_1807:  ldloc.s    V_16
    IL_1809:  beq.s      IL_1826

    IL_180b:  ldc.i4.1
    IL_180c:  stloc.s    V_20
    IL_180e:  ldstr      "te264 check failed {0} {1}"
    IL_1813:  ldloc.s    V_6
    IL_1815:  box        [mscorlib]System.Int64
    IL_181a:  ldloc.s    V_16
    IL_181c:  box        [mscorlib]System.Int64
    IL_1821:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1826:  ldloc.s    V_7
    IL_1828:  ldloc.s    V_17
    IL_182a:  beq.s      IL_1847

    IL_182c:  ldc.i4.1
    IL_182d:  stloc.s    V_20
    IL_182f:  ldstr      "te364 check failed {0} {1}"
    IL_1834:  ldloc.s    V_7
    IL_1836:  box        [mscorlib]System.Int64
    IL_183b:  ldloc.s    V_17
    IL_183d:  box        [mscorlib]System.Int64
    IL_1842:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1847:  ldloc.s    V_8
    IL_1849:  ldloc.s    V_18
    IL_184b:  beq.s      IL_1868

    IL_184d:  ldc.i4.1
    IL_184e:  stloc.s    V_20
    IL_1850:  ldstr      "dummy check failed {0} {1}"
    IL_1855:  ldloc.s    V_8
    IL_1857:  box        [mscorlib]System.Int32
    IL_185c:  ldloc.s    V_18
    IL_185e:  box        [mscorlib]System.Int32
    IL_1863:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1868:  ldloc.s    V_9
    IL_186a:  ldloc.s    V_19
    IL_186c:  beq.s      IL_1889

    IL_186e:  ldc.i4.1
    IL_186f:  stloc.s    V_20
    IL_1871:  ldstr      "dummy64 check failed {0} {1}"
    IL_1876:  ldloc.s    V_9
    IL_1878:  box        [mscorlib]System.Int64
    IL_187d:  ldloc.s    V_19
    IL_187f:  box        [mscorlib]System.Int64
    IL_1884:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1889:  ldsfld     int32 IntrinsicTest::counter
    IL_188e:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1893:  beq.s      IL_18b6

    IL_1895:  ldstr      "counter mismatch {0} {1}"
    IL_189a:  ldsfld     int32 IntrinsicTest::counter
    IL_189f:  box        [mscorlib]System.Int32
    IL_18a4:  ldsfld     int32 IntrinsicTest::id_counter
    IL_18a9:  box        [mscorlib]System.Int32
    IL_18ae:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_18b3:  ldc.i4.1
    IL_18b4:  stloc.s    V_20
    IL_18b6:  ldsfld     int64 IntrinsicTest::counter64
    IL_18bb:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_18c0:  beq.s      IL_18e3

    IL_18c2:  ldstr      "counter64 mismatch {0} {1}"
    IL_18c7:  ldsfld     int64 IntrinsicTest::counter64
    IL_18cc:  box        [mscorlib]System.Int64
    IL_18d1:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_18d6:  box        [mscorlib]System.Int64
    IL_18db:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_18e0:  ldc.i4.1
    IL_18e1:  stloc.s    V_20
    IL_18e3:  ldloc.s    V_21
    IL_18e5:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_18ea:  ldloc.s    V_21
    IL_18ec:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_18f1:  beq.s      IL_1918

    IL_18f3:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_18f8:  ldloc.s    V_21
    IL_18fa:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_18ff:  box        [mscorlib]System.Int32
    IL_1904:  ldloc.s    V_21
    IL_1906:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_190b:  box        [mscorlib]System.Int32
    IL_1910:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1915:  ldc.i4.1
    IL_1916:  stloc.s    V_20
    IL_1918:  ldloc.s    V_21
    IL_191a:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_191f:  ldloc.s    V_21
    IL_1921:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_1926:  beq.s      IL_194d

    IL_1928:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_192d:  ldloc.s    V_21
    IL_192f:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_1934:  box        [mscorlib]System.Int64
    IL_1939:  ldloc.s    V_21
    IL_193b:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_1940:  box        [mscorlib]System.Int64
    IL_1945:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_194a:  ldc.i4.1
    IL_194b:  stloc.s    V_20
    IL_194d:  ldsfld     int64 IntrinsicTest::temp
    IL_1952:  ldsfld     int64 IntrinsicTest::idmp
    IL_1957:  beq.s      IL_197a

    IL_1959:  ldstr      "temp mismatch {0} {1}"
    IL_195e:  ldsfld     int64 IntrinsicTest::temp
    IL_1963:  box        [mscorlib]System.Int64
    IL_1968:  ldsfld     int64 IntrinsicTest::idmp
    IL_196d:  box        [mscorlib]System.Int64
    IL_1972:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1977:  ldc.i4.1
    IL_1978:  stloc.s    V_20
    IL_197a:  ldsflda    int64 IntrinsicTest::counter64
    IL_197f:  ldsfld     int64 IntrinsicTest::counter64
    IL_1984:  ldsfld     int64 IntrinsicTest::counter64
    IL_1989:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_198e:  stloc.s    V_4
    IL_1990:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1995:  stloc.s    V_14
    IL_1997:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_199c:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_19a1:  bne.un.s   IL_19af

    IL_19a3:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_19a8:  stsfld     int64 IntrinsicTest::id_counter64
    IL_19ad:  br.s       IL_19b9

    IL_19af:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_19b4:  stsfld     int64 IntrinsicTest::idjunk
    IL_19b9:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 1 3"
    IL_19be:  call       void [System.Console]System.Console::WriteLine(string)
    IL_19c3:  ldloc.0
    IL_19c4:  ldloc.s    V_10
    IL_19c6:  beq.s      IL_19e2

    IL_19c8:  ldc.i4.1
    IL_19c9:  stloc.s    V_20
    IL_19cb:  ldstr      "te0 check failed {0} {1}"
    IL_19d0:  ldloc.0
    IL_19d1:  box        [mscorlib]System.Int32
    IL_19d6:  ldloc.s    V_10
    IL_19d8:  box        [mscorlib]System.Int32
    IL_19dd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_19e2:  ldloc.1
    IL_19e3:  ldloc.s    V_11
    IL_19e5:  beq.s      IL_1a01

    IL_19e7:  ldc.i4.1
    IL_19e8:  stloc.s    V_20
    IL_19ea:  ldstr      "te1 check failed {0} {1}"
    IL_19ef:  ldloc.1
    IL_19f0:  box        [mscorlib]System.Int32
    IL_19f5:  ldloc.s    V_11
    IL_19f7:  box        [mscorlib]System.Int32
    IL_19fc:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1a01:  ldloc.2
    IL_1a02:  ldloc.s    V_12
    IL_1a04:  beq.s      IL_1a20

    IL_1a06:  ldc.i4.1
    IL_1a07:  stloc.s    V_20
    IL_1a09:  ldstr      "te2 check failed {0} {1}"
    IL_1a0e:  ldloc.2
    IL_1a0f:  box        [mscorlib]System.Int32
    IL_1a14:  ldloc.s    V_12
    IL_1a16:  box        [mscorlib]System.Int32
    IL_1a1b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1a20:  ldloc.3
    IL_1a21:  ldloc.s    V_13
    IL_1a23:  beq.s      IL_1a3f

    IL_1a25:  ldc.i4.1
    IL_1a26:  stloc.s    V_20
    IL_1a28:  ldstr      "te3 check failed {0} {1}"
    IL_1a2d:  ldloc.3
    IL_1a2e:  box        [mscorlib]System.Int32
    IL_1a33:  ldloc.s    V_13
    IL_1a35:  box        [mscorlib]System.Int32
    IL_1a3a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1a3f:  ldloc.s    V_4
    IL_1a41:  ldloc.s    V_14
    IL_1a43:  beq.s      IL_1a60

    IL_1a45:  ldc.i4.1
    IL_1a46:  stloc.s    V_20
    IL_1a48:  ldstr      "te064 check failed {0} {1}"
    IL_1a4d:  ldloc.s    V_4
    IL_1a4f:  box        [mscorlib]System.Int64
    IL_1a54:  ldloc.s    V_14
    IL_1a56:  box        [mscorlib]System.Int64
    IL_1a5b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1a60:  ldloc.s    V_5
    IL_1a62:  ldloc.s    V_15
    IL_1a64:  beq.s      IL_1a81

    IL_1a66:  ldc.i4.1
    IL_1a67:  stloc.s    V_20
    IL_1a69:  ldstr      "te164 check failed {0} {1}"
    IL_1a6e:  ldloc.s    V_5
    IL_1a70:  box        [mscorlib]System.Int64
    IL_1a75:  ldloc.s    V_15
    IL_1a77:  box        [mscorlib]System.Int64
    IL_1a7c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1a81:  ldloc.s    V_6
    IL_1a83:  ldloc.s    V_16
    IL_1a85:  beq.s      IL_1aa2

    IL_1a87:  ldc.i4.1
    IL_1a88:  stloc.s    V_20
    IL_1a8a:  ldstr      "te264 check failed {0} {1}"
    IL_1a8f:  ldloc.s    V_6
    IL_1a91:  box        [mscorlib]System.Int64
    IL_1a96:  ldloc.s    V_16
    IL_1a98:  box        [mscorlib]System.Int64
    IL_1a9d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1aa2:  ldloc.s    V_7
    IL_1aa4:  ldloc.s    V_17
    IL_1aa6:  beq.s      IL_1ac3

    IL_1aa8:  ldc.i4.1
    IL_1aa9:  stloc.s    V_20
    IL_1aab:  ldstr      "te364 check failed {0} {1}"
    IL_1ab0:  ldloc.s    V_7
    IL_1ab2:  box        [mscorlib]System.Int64
    IL_1ab7:  ldloc.s    V_17
    IL_1ab9:  box        [mscorlib]System.Int64
    IL_1abe:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1ac3:  ldloc.s    V_8
    IL_1ac5:  ldloc.s    V_18
    IL_1ac7:  beq.s      IL_1ae4

    IL_1ac9:  ldc.i4.1
    IL_1aca:  stloc.s    V_20
    IL_1acc:  ldstr      "dummy check failed {0} {1}"
    IL_1ad1:  ldloc.s    V_8
    IL_1ad3:  box        [mscorlib]System.Int32
    IL_1ad8:  ldloc.s    V_18
    IL_1ada:  box        [mscorlib]System.Int32
    IL_1adf:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1ae4:  ldloc.s    V_9
    IL_1ae6:  ldloc.s    V_19
    IL_1ae8:  beq.s      IL_1b05

    IL_1aea:  ldc.i4.1
    IL_1aeb:  stloc.s    V_20
    IL_1aed:  ldstr      "dummy64 check failed {0} {1}"
    IL_1af2:  ldloc.s    V_9
    IL_1af4:  box        [mscorlib]System.Int64
    IL_1af9:  ldloc.s    V_19
    IL_1afb:  box        [mscorlib]System.Int64
    IL_1b00:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1b05:  ldsfld     int32 IntrinsicTest::counter
    IL_1b0a:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1b0f:  beq.s      IL_1b32

    IL_1b11:  ldstr      "counter mismatch {0} {1}"
    IL_1b16:  ldsfld     int32 IntrinsicTest::counter
    IL_1b1b:  box        [mscorlib]System.Int32
    IL_1b20:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1b25:  box        [mscorlib]System.Int32
    IL_1b2a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1b2f:  ldc.i4.1
    IL_1b30:  stloc.s    V_20
    IL_1b32:  ldsfld     int64 IntrinsicTest::counter64
    IL_1b37:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1b3c:  beq.s      IL_1b5f

    IL_1b3e:  ldstr      "counter64 mismatch {0} {1}"
    IL_1b43:  ldsfld     int64 IntrinsicTest::counter64
    IL_1b48:  box        [mscorlib]System.Int64
    IL_1b4d:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1b52:  box        [mscorlib]System.Int64
    IL_1b57:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1b5c:  ldc.i4.1
    IL_1b5d:  stloc.s    V_20
    IL_1b5f:  ldloc.s    V_21
    IL_1b61:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_1b66:  ldloc.s    V_21
    IL_1b68:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_1b6d:  beq.s      IL_1b94

    IL_1b6f:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_1b74:  ldloc.s    V_21
    IL_1b76:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_1b7b:  box        [mscorlib]System.Int32
    IL_1b80:  ldloc.s    V_21
    IL_1b82:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_1b87:  box        [mscorlib]System.Int32
    IL_1b8c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1b91:  ldc.i4.1
    IL_1b92:  stloc.s    V_20
    IL_1b94:  ldloc.s    V_21
    IL_1b96:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_1b9b:  ldloc.s    V_21
    IL_1b9d:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_1ba2:  beq.s      IL_1bc9

    IL_1ba4:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_1ba9:  ldloc.s    V_21
    IL_1bab:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_1bb0:  box        [mscorlib]System.Int64
    IL_1bb5:  ldloc.s    V_21
    IL_1bb7:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_1bbc:  box        [mscorlib]System.Int64
    IL_1bc1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1bc6:  ldc.i4.1
    IL_1bc7:  stloc.s    V_20
    IL_1bc9:  ldsfld     int64 IntrinsicTest::temp
    IL_1bce:  ldsfld     int64 IntrinsicTest::idmp
    IL_1bd3:  beq.s      IL_1bf6

    IL_1bd5:  ldstr      "temp mismatch {0} {1}"
    IL_1bda:  ldsfld     int64 IntrinsicTest::temp
    IL_1bdf:  box        [mscorlib]System.Int64
    IL_1be4:  ldsfld     int64 IntrinsicTest::idmp
    IL_1be9:  box        [mscorlib]System.Int64
    IL_1bee:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1bf3:  ldc.i4.1
    IL_1bf4:  stloc.s    V_20
    IL_1bf6:  ldsflda    int64 IntrinsicTest::counter64
    IL_1bfb:  ldsfld     int64 IntrinsicTest::counter64
    IL_1c00:  ldsfld     int64 IntrinsicTest::counter64
    IL_1c05:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_1c0a:  stsfld     int64 IntrinsicTest::counter64
    IL_1c0f:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1c14:  stsfld     int64 IntrinsicTest::id_counter64
    IL_1c19:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1c1e:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1c23:  bne.un.s   IL_1c31

    IL_1c25:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1c2a:  stsfld     int64 IntrinsicTest::idjunk
    IL_1c2f:  br.s       IL_1c3b

    IL_1c31:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1c36:  stsfld     int64 IntrinsicTest::idjunk
    IL_1c3b:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 1 4"
    IL_1c40:  call       void [System.Console]System.Console::WriteLine(string)
    IL_1c45:  ldloc.0
    IL_1c46:  ldloc.s    V_10
    IL_1c48:  beq.s      IL_1c64

    IL_1c4a:  ldc.i4.1
    IL_1c4b:  stloc.s    V_20
    IL_1c4d:  ldstr      "te0 check failed {0} {1}"
    IL_1c52:  ldloc.0
    IL_1c53:  box        [mscorlib]System.Int32
    IL_1c58:  ldloc.s    V_10
    IL_1c5a:  box        [mscorlib]System.Int32
    IL_1c5f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1c64:  ldloc.1
    IL_1c65:  ldloc.s    V_11
    IL_1c67:  beq.s      IL_1c83

    IL_1c69:  ldc.i4.1
    IL_1c6a:  stloc.s    V_20
    IL_1c6c:  ldstr      "te1 check failed {0} {1}"
    IL_1c71:  ldloc.1
    IL_1c72:  box        [mscorlib]System.Int32
    IL_1c77:  ldloc.s    V_11
    IL_1c79:  box        [mscorlib]System.Int32
    IL_1c7e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1c83:  ldloc.2
    IL_1c84:  ldloc.s    V_12
    IL_1c86:  beq.s      IL_1ca2

    IL_1c88:  ldc.i4.1
    IL_1c89:  stloc.s    V_20
    IL_1c8b:  ldstr      "te2 check failed {0} {1}"
    IL_1c90:  ldloc.2
    IL_1c91:  box        [mscorlib]System.Int32
    IL_1c96:  ldloc.s    V_12
    IL_1c98:  box        [mscorlib]System.Int32
    IL_1c9d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1ca2:  ldloc.3
    IL_1ca3:  ldloc.s    V_13
    IL_1ca5:  beq.s      IL_1cc1

    IL_1ca7:  ldc.i4.1
    IL_1ca8:  stloc.s    V_20
    IL_1caa:  ldstr      "te3 check failed {0} {1}"
    IL_1caf:  ldloc.3
    IL_1cb0:  box        [mscorlib]System.Int32
    IL_1cb5:  ldloc.s    V_13
    IL_1cb7:  box        [mscorlib]System.Int32
    IL_1cbc:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1cc1:  ldloc.s    V_4
    IL_1cc3:  ldloc.s    V_14
    IL_1cc5:  beq.s      IL_1ce2

    IL_1cc7:  ldc.i4.1
    IL_1cc8:  stloc.s    V_20
    IL_1cca:  ldstr      "te064 check failed {0} {1}"
    IL_1ccf:  ldloc.s    V_4
    IL_1cd1:  box        [mscorlib]System.Int64
    IL_1cd6:  ldloc.s    V_14
    IL_1cd8:  box        [mscorlib]System.Int64
    IL_1cdd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1ce2:  ldloc.s    V_5
    IL_1ce4:  ldloc.s    V_15
    IL_1ce6:  beq.s      IL_1d03

    IL_1ce8:  ldc.i4.1
    IL_1ce9:  stloc.s    V_20
    IL_1ceb:  ldstr      "te164 check failed {0} {1}"
    IL_1cf0:  ldloc.s    V_5
    IL_1cf2:  box        [mscorlib]System.Int64
    IL_1cf7:  ldloc.s    V_15
    IL_1cf9:  box        [mscorlib]System.Int64
    IL_1cfe:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1d03:  ldloc.s    V_6
    IL_1d05:  ldloc.s    V_16
    IL_1d07:  beq.s      IL_1d24

    IL_1d09:  ldc.i4.1
    IL_1d0a:  stloc.s    V_20
    IL_1d0c:  ldstr      "te264 check failed {0} {1}"
    IL_1d11:  ldloc.s    V_6
    IL_1d13:  box        [mscorlib]System.Int64
    IL_1d18:  ldloc.s    V_16
    IL_1d1a:  box        [mscorlib]System.Int64
    IL_1d1f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1d24:  ldloc.s    V_7
    IL_1d26:  ldloc.s    V_17
    IL_1d28:  beq.s      IL_1d45

    IL_1d2a:  ldc.i4.1
    IL_1d2b:  stloc.s    V_20
    IL_1d2d:  ldstr      "te364 check failed {0} {1}"
    IL_1d32:  ldloc.s    V_7
    IL_1d34:  box        [mscorlib]System.Int64
    IL_1d39:  ldloc.s    V_17
    IL_1d3b:  box        [mscorlib]System.Int64
    IL_1d40:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1d45:  ldloc.s    V_8
    IL_1d47:  ldloc.s    V_18
    IL_1d49:  beq.s      IL_1d66

    IL_1d4b:  ldc.i4.1
    IL_1d4c:  stloc.s    V_20
    IL_1d4e:  ldstr      "dummy check failed {0} {1}"
    IL_1d53:  ldloc.s    V_8
    IL_1d55:  box        [mscorlib]System.Int32
    IL_1d5a:  ldloc.s    V_18
    IL_1d5c:  box        [mscorlib]System.Int32
    IL_1d61:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1d66:  ldloc.s    V_9
    IL_1d68:  ldloc.s    V_19
    IL_1d6a:  beq.s      IL_1d87

    IL_1d6c:  ldc.i4.1
    IL_1d6d:  stloc.s    V_20
    IL_1d6f:  ldstr      "dummy64 check failed {0} {1}"
    IL_1d74:  ldloc.s    V_9
    IL_1d76:  box        [mscorlib]System.Int64
    IL_1d7b:  ldloc.s    V_19
    IL_1d7d:  box        [mscorlib]System.Int64
    IL_1d82:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1d87:  ldsfld     int32 IntrinsicTest::counter
    IL_1d8c:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1d91:  beq.s      IL_1db4

    IL_1d93:  ldstr      "counter mismatch {0} {1}"
    IL_1d98:  ldsfld     int32 IntrinsicTest::counter
    IL_1d9d:  box        [mscorlib]System.Int32
    IL_1da2:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1da7:  box        [mscorlib]System.Int32
    IL_1dac:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1db1:  ldc.i4.1
    IL_1db2:  stloc.s    V_20
    IL_1db4:  ldsfld     int64 IntrinsicTest::counter64
    IL_1db9:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1dbe:  beq.s      IL_1de1

    IL_1dc0:  ldstr      "counter64 mismatch {0} {1}"
    IL_1dc5:  ldsfld     int64 IntrinsicTest::counter64
    IL_1dca:  box        [mscorlib]System.Int64
    IL_1dcf:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1dd4:  box        [mscorlib]System.Int64
    IL_1dd9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1dde:  ldc.i4.1
    IL_1ddf:  stloc.s    V_20
    IL_1de1:  ldloc.s    V_21
    IL_1de3:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_1de8:  ldloc.s    V_21
    IL_1dea:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_1def:  beq.s      IL_1e16

    IL_1df1:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_1df6:  ldloc.s    V_21
    IL_1df8:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_1dfd:  box        [mscorlib]System.Int32
    IL_1e02:  ldloc.s    V_21
    IL_1e04:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_1e09:  box        [mscorlib]System.Int32
    IL_1e0e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1e13:  ldc.i4.1
    IL_1e14:  stloc.s    V_20
    IL_1e16:  ldloc.s    V_21
    IL_1e18:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_1e1d:  ldloc.s    V_21
    IL_1e1f:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_1e24:  beq.s      IL_1e4b

    IL_1e26:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_1e2b:  ldloc.s    V_21
    IL_1e2d:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_1e32:  box        [mscorlib]System.Int64
    IL_1e37:  ldloc.s    V_21
    IL_1e39:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_1e3e:  box        [mscorlib]System.Int64
    IL_1e43:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1e48:  ldc.i4.1
    IL_1e49:  stloc.s    V_20
    IL_1e4b:  ldsfld     int64 IntrinsicTest::temp
    IL_1e50:  ldsfld     int64 IntrinsicTest::idmp
    IL_1e55:  beq.s      IL_1e78

    IL_1e57:  ldstr      "temp mismatch {0} {1}"
    IL_1e5c:  ldsfld     int64 IntrinsicTest::temp
    IL_1e61:  box        [mscorlib]System.Int64
    IL_1e66:  ldsfld     int64 IntrinsicTest::idmp
    IL_1e6b:  box        [mscorlib]System.Int64
    IL_1e70:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1e75:  ldc.i4.1
    IL_1e76:  stloc.s    V_20
    IL_1e78:  ldloc.s    V_21
    IL_1e7a:  ldsflda    int64 IntrinsicTest::counter64
    IL_1e7f:  ldsfld     int64 IntrinsicTest::counter64
    IL_1e84:  ldsfld     int64 IntrinsicTest::counter64
    IL_1e89:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_1e8e:  stfld      int64 IntrinsicTest::instanceCounter64
    IL_1e93:  ldloc.s    V_21
    IL_1e95:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1e9a:  stfld      int64 IntrinsicTest::id_instanceCounter64
    IL_1e9f:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1ea4:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1ea9:  bne.un.s   IL_1eb7

    IL_1eab:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1eb0:  stsfld     int64 IntrinsicTest::id_counter64
    IL_1eb5:  br.s       IL_1ec1

    IL_1eb7:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1ebc:  stsfld     int64 IntrinsicTest::idjunk
    IL_1ec1:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 1 5"
    IL_1ec6:  call       void [System.Console]System.Console::WriteLine(string)
    IL_1ecb:  ldloc.0
    IL_1ecc:  ldloc.s    V_10
    IL_1ece:  beq.s      IL_1eea

    IL_1ed0:  ldc.i4.1
    IL_1ed1:  stloc.s    V_20
    IL_1ed3:  ldstr      "te0 check failed {0} {1}"
    IL_1ed8:  ldloc.0
    IL_1ed9:  box        [mscorlib]System.Int32
    IL_1ede:  ldloc.s    V_10
    IL_1ee0:  box        [mscorlib]System.Int32
    IL_1ee5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1eea:  ldloc.1
    IL_1eeb:  ldloc.s    V_11
    IL_1eed:  beq.s      IL_1f09

    IL_1eef:  ldc.i4.1
    IL_1ef0:  stloc.s    V_20
    IL_1ef2:  ldstr      "te1 check failed {0} {1}"
    IL_1ef7:  ldloc.1
    IL_1ef8:  box        [mscorlib]System.Int32
    IL_1efd:  ldloc.s    V_11
    IL_1eff:  box        [mscorlib]System.Int32
    IL_1f04:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1f09:  ldloc.2
    IL_1f0a:  ldloc.s    V_12
    IL_1f0c:  beq.s      IL_1f28

    IL_1f0e:  ldc.i4.1
    IL_1f0f:  stloc.s    V_20
    IL_1f11:  ldstr      "te2 check failed {0} {1}"
    IL_1f16:  ldloc.2
    IL_1f17:  box        [mscorlib]System.Int32
    IL_1f1c:  ldloc.s    V_12
    IL_1f1e:  box        [mscorlib]System.Int32
    IL_1f23:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1f28:  ldloc.3
    IL_1f29:  ldloc.s    V_13
    IL_1f2b:  beq.s      IL_1f47

    IL_1f2d:  ldc.i4.1
    IL_1f2e:  stloc.s    V_20
    IL_1f30:  ldstr      "te3 check failed {0} {1}"
    IL_1f35:  ldloc.3
    IL_1f36:  box        [mscorlib]System.Int32
    IL_1f3b:  ldloc.s    V_13
    IL_1f3d:  box        [mscorlib]System.Int32
    IL_1f42:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1f47:  ldloc.s    V_4
    IL_1f49:  ldloc.s    V_14
    IL_1f4b:  beq.s      IL_1f68

    IL_1f4d:  ldc.i4.1
    IL_1f4e:  stloc.s    V_20
    IL_1f50:  ldstr      "te064 check failed {0} {1}"
    IL_1f55:  ldloc.s    V_4
    IL_1f57:  box        [mscorlib]System.Int64
    IL_1f5c:  ldloc.s    V_14
    IL_1f5e:  box        [mscorlib]System.Int64
    IL_1f63:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1f68:  ldloc.s    V_5
    IL_1f6a:  ldloc.s    V_15
    IL_1f6c:  beq.s      IL_1f89

    IL_1f6e:  ldc.i4.1
    IL_1f6f:  stloc.s    V_20
    IL_1f71:  ldstr      "te164 check failed {0} {1}"
    IL_1f76:  ldloc.s    V_5
    IL_1f78:  box        [mscorlib]System.Int64
    IL_1f7d:  ldloc.s    V_15
    IL_1f7f:  box        [mscorlib]System.Int64
    IL_1f84:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1f89:  ldloc.s    V_6
    IL_1f8b:  ldloc.s    V_16
    IL_1f8d:  beq.s      IL_1faa

    IL_1f8f:  ldc.i4.1
    IL_1f90:  stloc.s    V_20
    IL_1f92:  ldstr      "te264 check failed {0} {1}"
    IL_1f97:  ldloc.s    V_6
    IL_1f99:  box        [mscorlib]System.Int64
    IL_1f9e:  ldloc.s    V_16
    IL_1fa0:  box        [mscorlib]System.Int64
    IL_1fa5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1faa:  ldloc.s    V_7
    IL_1fac:  ldloc.s    V_17
    IL_1fae:  beq.s      IL_1fcb

    IL_1fb0:  ldc.i4.1
    IL_1fb1:  stloc.s    V_20
    IL_1fb3:  ldstr      "te364 check failed {0} {1}"
    IL_1fb8:  ldloc.s    V_7
    IL_1fba:  box        [mscorlib]System.Int64
    IL_1fbf:  ldloc.s    V_17
    IL_1fc1:  box        [mscorlib]System.Int64
    IL_1fc6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1fcb:  ldloc.s    V_8
    IL_1fcd:  ldloc.s    V_18
    IL_1fcf:  beq.s      IL_1fec

    IL_1fd1:  ldc.i4.1
    IL_1fd2:  stloc.s    V_20
    IL_1fd4:  ldstr      "dummy check failed {0} {1}"
    IL_1fd9:  ldloc.s    V_8
    IL_1fdb:  box        [mscorlib]System.Int32
    IL_1fe0:  ldloc.s    V_18
    IL_1fe2:  box        [mscorlib]System.Int32
    IL_1fe7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1fec:  ldloc.s    V_9
    IL_1fee:  ldloc.s    V_19
    IL_1ff0:  beq.s      IL_200d

    IL_1ff2:  ldc.i4.1
    IL_1ff3:  stloc.s    V_20
    IL_1ff5:  ldstr      "dummy64 check failed {0} {1}"
    IL_1ffa:  ldloc.s    V_9
    IL_1ffc:  box        [mscorlib]System.Int64
    IL_2001:  ldloc.s    V_19
    IL_2003:  box        [mscorlib]System.Int64
    IL_2008:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_200d:  ldsfld     int32 IntrinsicTest::counter
    IL_2012:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2017:  beq.s      IL_203a

    IL_2019:  ldstr      "counter mismatch {0} {1}"
    IL_201e:  ldsfld     int32 IntrinsicTest::counter
    IL_2023:  box        [mscorlib]System.Int32
    IL_2028:  ldsfld     int32 IntrinsicTest::id_counter
    IL_202d:  box        [mscorlib]System.Int32
    IL_2032:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2037:  ldc.i4.1
    IL_2038:  stloc.s    V_20
    IL_203a:  ldsfld     int64 IntrinsicTest::counter64
    IL_203f:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2044:  beq.s      IL_2067

    IL_2046:  ldstr      "counter64 mismatch {0} {1}"
    IL_204b:  ldsfld     int64 IntrinsicTest::counter64
    IL_2050:  box        [mscorlib]System.Int64
    IL_2055:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_205a:  box        [mscorlib]System.Int64
    IL_205f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2064:  ldc.i4.1
    IL_2065:  stloc.s    V_20
    IL_2067:  ldloc.s    V_21
    IL_2069:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_206e:  ldloc.s    V_21
    IL_2070:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_2075:  beq.s      IL_209c

    IL_2077:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_207c:  ldloc.s    V_21
    IL_207e:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_2083:  box        [mscorlib]System.Int32
    IL_2088:  ldloc.s    V_21
    IL_208a:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_208f:  box        [mscorlib]System.Int32
    IL_2094:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2099:  ldc.i4.1
    IL_209a:  stloc.s    V_20
    IL_209c:  ldloc.s    V_21
    IL_209e:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_20a3:  ldloc.s    V_21
    IL_20a5:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_20aa:  beq.s      IL_20d1

    IL_20ac:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_20b1:  ldloc.s    V_21
    IL_20b3:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_20b8:  box        [mscorlib]System.Int64
    IL_20bd:  ldloc.s    V_21
    IL_20bf:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_20c4:  box        [mscorlib]System.Int64
    IL_20c9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_20ce:  ldc.i4.1
    IL_20cf:  stloc.s    V_20
    IL_20d1:  ldsfld     int64 IntrinsicTest::temp
    IL_20d6:  ldsfld     int64 IntrinsicTest::idmp
    IL_20db:  beq.s      IL_20fe

    IL_20dd:  ldstr      "temp mismatch {0} {1}"
    IL_20e2:  ldsfld     int64 IntrinsicTest::temp
    IL_20e7:  box        [mscorlib]System.Int64
    IL_20ec:  ldsfld     int64 IntrinsicTest::idmp
    IL_20f1:  box        [mscorlib]System.Int64
    IL_20f6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_20fb:  ldc.i4.1
    IL_20fc:  stloc.s    V_20
    IL_20fe:  ldsflda    int64 IntrinsicTest::counter64
    IL_2103:  ldsfld     int64 IntrinsicTest::counter64
    IL_2108:  ldsfld     int64 IntrinsicTest::counter64
    IL_210d:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_2112:  stloc.s    V_7
    IL_2114:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2119:  stloc.s    V_17
    IL_211b:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2120:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2125:  bne.un.s   IL_2133

    IL_2127:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_212c:  stsfld     int64 IntrinsicTest::id_counter64
    IL_2131:  br.s       IL_213d

    IL_2133:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2138:  stsfld     int64 IntrinsicTest::idjunk
    IL_213d:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 1 6"
    IL_2142:  call       void [System.Console]System.Console::WriteLine(string)
    IL_2147:  ldloc.0
    IL_2148:  ldloc.s    V_10
    IL_214a:  beq.s      IL_2166

    IL_214c:  ldc.i4.1
    IL_214d:  stloc.s    V_20
    IL_214f:  ldstr      "te0 check failed {0} {1}"
    IL_2154:  ldloc.0
    IL_2155:  box        [mscorlib]System.Int32
    IL_215a:  ldloc.s    V_10
    IL_215c:  box        [mscorlib]System.Int32
    IL_2161:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2166:  ldloc.1
    IL_2167:  ldloc.s    V_11
    IL_2169:  beq.s      IL_2185

    IL_216b:  ldc.i4.1
    IL_216c:  stloc.s    V_20
    IL_216e:  ldstr      "te1 check failed {0} {1}"
    IL_2173:  ldloc.1
    IL_2174:  box        [mscorlib]System.Int32
    IL_2179:  ldloc.s    V_11
    IL_217b:  box        [mscorlib]System.Int32
    IL_2180:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2185:  ldloc.2
    IL_2186:  ldloc.s    V_12
    IL_2188:  beq.s      IL_21a4

    IL_218a:  ldc.i4.1
    IL_218b:  stloc.s    V_20
    IL_218d:  ldstr      "te2 check failed {0} {1}"
    IL_2192:  ldloc.2
    IL_2193:  box        [mscorlib]System.Int32
    IL_2198:  ldloc.s    V_12
    IL_219a:  box        [mscorlib]System.Int32
    IL_219f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_21a4:  ldloc.3
    IL_21a5:  ldloc.s    V_13
    IL_21a7:  beq.s      IL_21c3

    IL_21a9:  ldc.i4.1
    IL_21aa:  stloc.s    V_20
    IL_21ac:  ldstr      "te3 check failed {0} {1}"
    IL_21b1:  ldloc.3
    IL_21b2:  box        [mscorlib]System.Int32
    IL_21b7:  ldloc.s    V_13
    IL_21b9:  box        [mscorlib]System.Int32
    IL_21be:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_21c3:  ldloc.s    V_4
    IL_21c5:  ldloc.s    V_14
    IL_21c7:  beq.s      IL_21e4

    IL_21c9:  ldc.i4.1
    IL_21ca:  stloc.s    V_20
    IL_21cc:  ldstr      "te064 check failed {0} {1}"
    IL_21d1:  ldloc.s    V_4
    IL_21d3:  box        [mscorlib]System.Int64
    IL_21d8:  ldloc.s    V_14
    IL_21da:  box        [mscorlib]System.Int64
    IL_21df:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_21e4:  ldloc.s    V_5
    IL_21e6:  ldloc.s    V_15
    IL_21e8:  beq.s      IL_2205

    IL_21ea:  ldc.i4.1
    IL_21eb:  stloc.s    V_20
    IL_21ed:  ldstr      "te164 check failed {0} {1}"
    IL_21f2:  ldloc.s    V_5
    IL_21f4:  box        [mscorlib]System.Int64
    IL_21f9:  ldloc.s    V_15
    IL_21fb:  box        [mscorlib]System.Int64
    IL_2200:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2205:  ldloc.s    V_6
    IL_2207:  ldloc.s    V_16
    IL_2209:  beq.s      IL_2226

    IL_220b:  ldc.i4.1
    IL_220c:  stloc.s    V_20
    IL_220e:  ldstr      "te264 check failed {0} {1}"
    IL_2213:  ldloc.s    V_6
    IL_2215:  box        [mscorlib]System.Int64
    IL_221a:  ldloc.s    V_16
    IL_221c:  box        [mscorlib]System.Int64
    IL_2221:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2226:  ldloc.s    V_7
    IL_2228:  ldloc.s    V_17
    IL_222a:  beq.s      IL_2247

    IL_222c:  ldc.i4.1
    IL_222d:  stloc.s    V_20
    IL_222f:  ldstr      "te364 check failed {0} {1}"
    IL_2234:  ldloc.s    V_7
    IL_2236:  box        [mscorlib]System.Int64
    IL_223b:  ldloc.s    V_17
    IL_223d:  box        [mscorlib]System.Int64
    IL_2242:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2247:  ldloc.s    V_8
    IL_2249:  ldloc.s    V_18
    IL_224b:  beq.s      IL_2268

    IL_224d:  ldc.i4.1
    IL_224e:  stloc.s    V_20
    IL_2250:  ldstr      "dummy check failed {0} {1}"
    IL_2255:  ldloc.s    V_8
    IL_2257:  box        [mscorlib]System.Int32
    IL_225c:  ldloc.s    V_18
    IL_225e:  box        [mscorlib]System.Int32
    IL_2263:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2268:  ldloc.s    V_9
    IL_226a:  ldloc.s    V_19
    IL_226c:  beq.s      IL_2289

    IL_226e:  ldc.i4.1
    IL_226f:  stloc.s    V_20
    IL_2271:  ldstr      "dummy64 check failed {0} {1}"
    IL_2276:  ldloc.s    V_9
    IL_2278:  box        [mscorlib]System.Int64
    IL_227d:  ldloc.s    V_19
    IL_227f:  box        [mscorlib]System.Int64
    IL_2284:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2289:  ldsfld     int32 IntrinsicTest::counter
    IL_228e:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2293:  beq.s      IL_22b6

    IL_2295:  ldstr      "counter mismatch {0} {1}"
    IL_229a:  ldsfld     int32 IntrinsicTest::counter
    IL_229f:  box        [mscorlib]System.Int32
    IL_22a4:  ldsfld     int32 IntrinsicTest::id_counter
    IL_22a9:  box        [mscorlib]System.Int32
    IL_22ae:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_22b3:  ldc.i4.1
    IL_22b4:  stloc.s    V_20
    IL_22b6:  ldsfld     int64 IntrinsicTest::counter64
    IL_22bb:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_22c0:  beq.s      IL_22e3

    IL_22c2:  ldstr      "counter64 mismatch {0} {1}"
    IL_22c7:  ldsfld     int64 IntrinsicTest::counter64
    IL_22cc:  box        [mscorlib]System.Int64
    IL_22d1:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_22d6:  box        [mscorlib]System.Int64
    IL_22db:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_22e0:  ldc.i4.1
    IL_22e1:  stloc.s    V_20
    IL_22e3:  ldloc.s    V_21
    IL_22e5:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_22ea:  ldloc.s    V_21
    IL_22ec:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_22f1:  beq.s      IL_2318

    IL_22f3:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_22f8:  ldloc.s    V_21
    IL_22fa:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_22ff:  box        [mscorlib]System.Int32
    IL_2304:  ldloc.s    V_21
    IL_2306:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_230b:  box        [mscorlib]System.Int32
    IL_2310:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2315:  ldc.i4.1
    IL_2316:  stloc.s    V_20
    IL_2318:  ldloc.s    V_21
    IL_231a:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_231f:  ldloc.s    V_21
    IL_2321:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_2326:  beq.s      IL_234d

    IL_2328:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_232d:  ldloc.s    V_21
    IL_232f:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_2334:  box        [mscorlib]System.Int64
    IL_2339:  ldloc.s    V_21
    IL_233b:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_2340:  box        [mscorlib]System.Int64
    IL_2345:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_234a:  ldc.i4.1
    IL_234b:  stloc.s    V_20
    IL_234d:  ldsfld     int64 IntrinsicTest::temp
    IL_2352:  ldsfld     int64 IntrinsicTest::idmp
    IL_2357:  beq.s      IL_237a

    IL_2359:  ldstr      "temp mismatch {0} {1}"
    IL_235e:  ldsfld     int64 IntrinsicTest::temp
    IL_2363:  box        [mscorlib]System.Int64
    IL_2368:  ldsfld     int64 IntrinsicTest::idmp
    IL_236d:  box        [mscorlib]System.Int64
    IL_2372:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2377:  ldc.i4.1
    IL_2378:  stloc.s    V_20
    IL_237a:  ldsflda    int64 IntrinsicTest::counter64
    IL_237f:  ldsfld     int64 IntrinsicTest::counter64
    IL_2384:  ldloc.s    V_21
    IL_2386:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_238b:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_2390:  stloc.s    V_4
    IL_2392:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2397:  stloc.s    V_14
    IL_2399:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_239e:  ldloc.s    V_21
    IL_23a0:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_23a5:  bne.un.s   IL_23b3

    IL_23a7:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_23ac:  stsfld     int64 IntrinsicTest::id_counter64
    IL_23b1:  br.s       IL_23bd

    IL_23b3:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_23b8:  stsfld     int64 IntrinsicTest::idjunk
    IL_23bd:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 2 0"
    IL_23c2:  call       void [System.Console]System.Console::WriteLine(string)
    IL_23c7:  ldloc.0
    IL_23c8:  ldloc.s    V_10
    IL_23ca:  beq.s      IL_23e6

    IL_23cc:  ldc.i4.1
    IL_23cd:  stloc.s    V_20
    IL_23cf:  ldstr      "te0 check failed {0} {1}"
    IL_23d4:  ldloc.0
    IL_23d5:  box        [mscorlib]System.Int32
    IL_23da:  ldloc.s    V_10
    IL_23dc:  box        [mscorlib]System.Int32
    IL_23e1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_23e6:  ldloc.1
    IL_23e7:  ldloc.s    V_11
    IL_23e9:  beq.s      IL_2405

    IL_23eb:  ldc.i4.1
    IL_23ec:  stloc.s    V_20
    IL_23ee:  ldstr      "te1 check failed {0} {1}"
    IL_23f3:  ldloc.1
    IL_23f4:  box        [mscorlib]System.Int32
    IL_23f9:  ldloc.s    V_11
    IL_23fb:  box        [mscorlib]System.Int32
    IL_2400:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2405:  ldloc.2
    IL_2406:  ldloc.s    V_12
    IL_2408:  beq.s      IL_2424

    IL_240a:  ldc.i4.1
    IL_240b:  stloc.s    V_20
    IL_240d:  ldstr      "te2 check failed {0} {1}"
    IL_2412:  ldloc.2
    IL_2413:  box        [mscorlib]System.Int32
    IL_2418:  ldloc.s    V_12
    IL_241a:  box        [mscorlib]System.Int32
    IL_241f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2424:  ldloc.3
    IL_2425:  ldloc.s    V_13
    IL_2427:  beq.s      IL_2443

    IL_2429:  ldc.i4.1
    IL_242a:  stloc.s    V_20
    IL_242c:  ldstr      "te3 check failed {0} {1}"
    IL_2431:  ldloc.3
    IL_2432:  box        [mscorlib]System.Int32
    IL_2437:  ldloc.s    V_13
    IL_2439:  box        [mscorlib]System.Int32
    IL_243e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2443:  ldloc.s    V_4
    IL_2445:  ldloc.s    V_14
    IL_2447:  beq.s      IL_2464

    IL_2449:  ldc.i4.1
    IL_244a:  stloc.s    V_20
    IL_244c:  ldstr      "te064 check failed {0} {1}"
    IL_2451:  ldloc.s    V_4
    IL_2453:  box        [mscorlib]System.Int64
    IL_2458:  ldloc.s    V_14
    IL_245a:  box        [mscorlib]System.Int64
    IL_245f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2464:  ldloc.s    V_5
    IL_2466:  ldloc.s    V_15
    IL_2468:  beq.s      IL_2485

    IL_246a:  ldc.i4.1
    IL_246b:  stloc.s    V_20
    IL_246d:  ldstr      "te164 check failed {0} {1}"
    IL_2472:  ldloc.s    V_5
    IL_2474:  box        [mscorlib]System.Int64
    IL_2479:  ldloc.s    V_15
    IL_247b:  box        [mscorlib]System.Int64
    IL_2480:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2485:  ldloc.s    V_6
    IL_2487:  ldloc.s    V_16
    IL_2489:  beq.s      IL_24a6

    IL_248b:  ldc.i4.1
    IL_248c:  stloc.s    V_20
    IL_248e:  ldstr      "te264 check failed {0} {1}"
    IL_2493:  ldloc.s    V_6
    IL_2495:  box        [mscorlib]System.Int64
    IL_249a:  ldloc.s    V_16
    IL_249c:  box        [mscorlib]System.Int64
    IL_24a1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_24a6:  ldloc.s    V_7
    IL_24a8:  ldloc.s    V_17
    IL_24aa:  beq.s      IL_24c7

    IL_24ac:  ldc.i4.1
    IL_24ad:  stloc.s    V_20
    IL_24af:  ldstr      "te364 check failed {0} {1}"
    IL_24b4:  ldloc.s    V_7
    IL_24b6:  box        [mscorlib]System.Int64
    IL_24bb:  ldloc.s    V_17
    IL_24bd:  box        [mscorlib]System.Int64
    IL_24c2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_24c7:  ldloc.s    V_8
    IL_24c9:  ldloc.s    V_18
    IL_24cb:  beq.s      IL_24e8

    IL_24cd:  ldc.i4.1
    IL_24ce:  stloc.s    V_20
    IL_24d0:  ldstr      "dummy check failed {0} {1}"
    IL_24d5:  ldloc.s    V_8
    IL_24d7:  box        [mscorlib]System.Int32
    IL_24dc:  ldloc.s    V_18
    IL_24de:  box        [mscorlib]System.Int32
    IL_24e3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_24e8:  ldloc.s    V_9
    IL_24ea:  ldloc.s    V_19
    IL_24ec:  beq.s      IL_2509

    IL_24ee:  ldc.i4.1
    IL_24ef:  stloc.s    V_20
    IL_24f1:  ldstr      "dummy64 check failed {0} {1}"
    IL_24f6:  ldloc.s    V_9
    IL_24f8:  box        [mscorlib]System.Int64
    IL_24fd:  ldloc.s    V_19
    IL_24ff:  box        [mscorlib]System.Int64
    IL_2504:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2509:  ldsfld     int32 IntrinsicTest::counter
    IL_250e:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2513:  beq.s      IL_2536

    IL_2515:  ldstr      "counter mismatch {0} {1}"
    IL_251a:  ldsfld     int32 IntrinsicTest::counter
    IL_251f:  box        [mscorlib]System.Int32
    IL_2524:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2529:  box        [mscorlib]System.Int32
    IL_252e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2533:  ldc.i4.1
    IL_2534:  stloc.s    V_20
    IL_2536:  ldsfld     int64 IntrinsicTest::counter64
    IL_253b:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2540:  beq.s      IL_2563

    IL_2542:  ldstr      "counter64 mismatch {0} {1}"
    IL_2547:  ldsfld     int64 IntrinsicTest::counter64
    IL_254c:  box        [mscorlib]System.Int64
    IL_2551:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2556:  box        [mscorlib]System.Int64
    IL_255b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2560:  ldc.i4.1
    IL_2561:  stloc.s    V_20
    IL_2563:  ldloc.s    V_21
    IL_2565:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_256a:  ldloc.s    V_21
    IL_256c:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_2571:  beq.s      IL_2598

    IL_2573:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_2578:  ldloc.s    V_21
    IL_257a:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_257f:  box        [mscorlib]System.Int32
    IL_2584:  ldloc.s    V_21
    IL_2586:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_258b:  box        [mscorlib]System.Int32
    IL_2590:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2595:  ldc.i4.1
    IL_2596:  stloc.s    V_20
    IL_2598:  ldloc.s    V_21
    IL_259a:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_259f:  ldloc.s    V_21
    IL_25a1:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_25a6:  beq.s      IL_25cd

    IL_25a8:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_25ad:  ldloc.s    V_21
    IL_25af:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_25b4:  box        [mscorlib]System.Int64
    IL_25b9:  ldloc.s    V_21
    IL_25bb:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_25c0:  box        [mscorlib]System.Int64
    IL_25c5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_25ca:  ldc.i4.1
    IL_25cb:  stloc.s    V_20
    IL_25cd:  ldsfld     int64 IntrinsicTest::temp
    IL_25d2:  ldsfld     int64 IntrinsicTest::idmp
    IL_25d7:  beq.s      IL_25fa

    IL_25d9:  ldstr      "temp mismatch {0} {1}"
    IL_25de:  ldsfld     int64 IntrinsicTest::temp
    IL_25e3:  box        [mscorlib]System.Int64
    IL_25e8:  ldsfld     int64 IntrinsicTest::idmp
    IL_25ed:  box        [mscorlib]System.Int64
    IL_25f2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_25f7:  ldc.i4.1
    IL_25f8:  stloc.s    V_20
    IL_25fa:  ldsflda    int64 IntrinsicTest::counter64
    IL_25ff:  ldsfld     int64 IntrinsicTest::counter64
    IL_2604:  ldloc.s    V_21
    IL_2606:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_260b:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_2610:  stsfld     int64 IntrinsicTest::counter64
    IL_2615:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_261a:  stsfld     int64 IntrinsicTest::id_counter64
    IL_261f:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2624:  ldloc.s    V_21
    IL_2626:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_262b:  bne.un.s   IL_2639

    IL_262d:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2632:  stsfld     int64 IntrinsicTest::idjunk
    IL_2637:  br.s       IL_2643

    IL_2639:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_263e:  stsfld     int64 IntrinsicTest::idjunk
    IL_2643:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 2 1"
    IL_2648:  call       void [System.Console]System.Console::WriteLine(string)
    IL_264d:  ldloc.0
    IL_264e:  ldloc.s    V_10
    IL_2650:  beq.s      IL_266c

    IL_2652:  ldc.i4.1
    IL_2653:  stloc.s    V_20
    IL_2655:  ldstr      "te0 check failed {0} {1}"
    IL_265a:  ldloc.0
    IL_265b:  box        [mscorlib]System.Int32
    IL_2660:  ldloc.s    V_10
    IL_2662:  box        [mscorlib]System.Int32
    IL_2667:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_266c:  ldloc.1
    IL_266d:  ldloc.s    V_11
    IL_266f:  beq.s      IL_268b

    IL_2671:  ldc.i4.1
    IL_2672:  stloc.s    V_20
    IL_2674:  ldstr      "te1 check failed {0} {1}"
    IL_2679:  ldloc.1
    IL_267a:  box        [mscorlib]System.Int32
    IL_267f:  ldloc.s    V_11
    IL_2681:  box        [mscorlib]System.Int32
    IL_2686:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_268b:  ldloc.2
    IL_268c:  ldloc.s    V_12
    IL_268e:  beq.s      IL_26aa

    IL_2690:  ldc.i4.1
    IL_2691:  stloc.s    V_20
    IL_2693:  ldstr      "te2 check failed {0} {1}"
    IL_2698:  ldloc.2
    IL_2699:  box        [mscorlib]System.Int32
    IL_269e:  ldloc.s    V_12
    IL_26a0:  box        [mscorlib]System.Int32
    IL_26a5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_26aa:  ldloc.3
    IL_26ab:  ldloc.s    V_13
    IL_26ad:  beq.s      IL_26c9

    IL_26af:  ldc.i4.1
    IL_26b0:  stloc.s    V_20
    IL_26b2:  ldstr      "te3 check failed {0} {1}"
    IL_26b7:  ldloc.3
    IL_26b8:  box        [mscorlib]System.Int32
    IL_26bd:  ldloc.s    V_13
    IL_26bf:  box        [mscorlib]System.Int32
    IL_26c4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_26c9:  ldloc.s    V_4
    IL_26cb:  ldloc.s    V_14
    IL_26cd:  beq.s      IL_26ea

    IL_26cf:  ldc.i4.1
    IL_26d0:  stloc.s    V_20
    IL_26d2:  ldstr      "te064 check failed {0} {1}"
    IL_26d7:  ldloc.s    V_4
    IL_26d9:  box        [mscorlib]System.Int64
    IL_26de:  ldloc.s    V_14
    IL_26e0:  box        [mscorlib]System.Int64
    IL_26e5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_26ea:  ldloc.s    V_5
    IL_26ec:  ldloc.s    V_15
    IL_26ee:  beq.s      IL_270b

    IL_26f0:  ldc.i4.1
    IL_26f1:  stloc.s    V_20
    IL_26f3:  ldstr      "te164 check failed {0} {1}"
    IL_26f8:  ldloc.s    V_5
    IL_26fa:  box        [mscorlib]System.Int64
    IL_26ff:  ldloc.s    V_15
    IL_2701:  box        [mscorlib]System.Int64
    IL_2706:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_270b:  ldloc.s    V_6
    IL_270d:  ldloc.s    V_16
    IL_270f:  beq.s      IL_272c

    IL_2711:  ldc.i4.1
    IL_2712:  stloc.s    V_20
    IL_2714:  ldstr      "te264 check failed {0} {1}"
    IL_2719:  ldloc.s    V_6
    IL_271b:  box        [mscorlib]System.Int64
    IL_2720:  ldloc.s    V_16
    IL_2722:  box        [mscorlib]System.Int64
    IL_2727:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_272c:  ldloc.s    V_7
    IL_272e:  ldloc.s    V_17
    IL_2730:  beq.s      IL_274d

    IL_2732:  ldc.i4.1
    IL_2733:  stloc.s    V_20
    IL_2735:  ldstr      "te364 check failed {0} {1}"
    IL_273a:  ldloc.s    V_7
    IL_273c:  box        [mscorlib]System.Int64
    IL_2741:  ldloc.s    V_17
    IL_2743:  box        [mscorlib]System.Int64
    IL_2748:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_274d:  ldloc.s    V_8
    IL_274f:  ldloc.s    V_18
    IL_2751:  beq.s      IL_276e

    IL_2753:  ldc.i4.1
    IL_2754:  stloc.s    V_20
    IL_2756:  ldstr      "dummy check failed {0} {1}"
    IL_275b:  ldloc.s    V_8
    IL_275d:  box        [mscorlib]System.Int32
    IL_2762:  ldloc.s    V_18
    IL_2764:  box        [mscorlib]System.Int32
    IL_2769:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_276e:  ldloc.s    V_9
    IL_2770:  ldloc.s    V_19
    IL_2772:  beq.s      IL_278f

    IL_2774:  ldc.i4.1
    IL_2775:  stloc.s    V_20
    IL_2777:  ldstr      "dummy64 check failed {0} {1}"
    IL_277c:  ldloc.s    V_9
    IL_277e:  box        [mscorlib]System.Int64
    IL_2783:  ldloc.s    V_19
    IL_2785:  box        [mscorlib]System.Int64
    IL_278a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_278f:  ldsfld     int32 IntrinsicTest::counter
    IL_2794:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2799:  beq.s      IL_27bc

    IL_279b:  ldstr      "counter mismatch {0} {1}"
    IL_27a0:  ldsfld     int32 IntrinsicTest::counter
    IL_27a5:  box        [mscorlib]System.Int32
    IL_27aa:  ldsfld     int32 IntrinsicTest::id_counter
    IL_27af:  box        [mscorlib]System.Int32
    IL_27b4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_27b9:  ldc.i4.1
    IL_27ba:  stloc.s    V_20
    IL_27bc:  ldsfld     int64 IntrinsicTest::counter64
    IL_27c1:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_27c6:  beq.s      IL_27e9

    IL_27c8:  ldstr      "counter64 mismatch {0} {1}"
    IL_27cd:  ldsfld     int64 IntrinsicTest::counter64
    IL_27d2:  box        [mscorlib]System.Int64
    IL_27d7:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_27dc:  box        [mscorlib]System.Int64
    IL_27e1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_27e6:  ldc.i4.1
    IL_27e7:  stloc.s    V_20
    IL_27e9:  ldloc.s    V_21
    IL_27eb:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_27f0:  ldloc.s    V_21
    IL_27f2:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_27f7:  beq.s      IL_281e

    IL_27f9:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_27fe:  ldloc.s    V_21
    IL_2800:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_2805:  box        [mscorlib]System.Int32
    IL_280a:  ldloc.s    V_21
    IL_280c:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_2811:  box        [mscorlib]System.Int32
    IL_2816:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_281b:  ldc.i4.1
    IL_281c:  stloc.s    V_20
    IL_281e:  ldloc.s    V_21
    IL_2820:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_2825:  ldloc.s    V_21
    IL_2827:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_282c:  beq.s      IL_2853

    IL_282e:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_2833:  ldloc.s    V_21
    IL_2835:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_283a:  box        [mscorlib]System.Int64
    IL_283f:  ldloc.s    V_21
    IL_2841:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_2846:  box        [mscorlib]System.Int64
    IL_284b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2850:  ldc.i4.1
    IL_2851:  stloc.s    V_20
    IL_2853:  ldsfld     int64 IntrinsicTest::temp
    IL_2858:  ldsfld     int64 IntrinsicTest::idmp
    IL_285d:  beq.s      IL_2880

    IL_285f:  ldstr      "temp mismatch {0} {1}"
    IL_2864:  ldsfld     int64 IntrinsicTest::temp
    IL_2869:  box        [mscorlib]System.Int64
    IL_286e:  ldsfld     int64 IntrinsicTest::idmp
    IL_2873:  box        [mscorlib]System.Int64
    IL_2878:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_287d:  ldc.i4.1
    IL_287e:  stloc.s    V_20
    IL_2880:  ldloc.s    V_21
    IL_2882:  ldsflda    int64 IntrinsicTest::counter64
    IL_2887:  ldsfld     int64 IntrinsicTest::counter64
    IL_288c:  ldloc.s    V_21
    IL_288e:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_2893:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_2898:  stfld      int64 IntrinsicTest::instanceCounter64
    IL_289d:  ldloc.s    V_21
    IL_289f:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_28a4:  stfld      int64 IntrinsicTest::id_instanceCounter64
    IL_28a9:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_28ae:  ldloc.s    V_21
    IL_28b0:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_28b5:  bne.un.s   IL_28c3

    IL_28b7:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_28bc:  stsfld     int64 IntrinsicTest::id_counter64
    IL_28c1:  br.s       IL_28cd

    IL_28c3:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_28c8:  stsfld     int64 IntrinsicTest::idjunk
    IL_28cd:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 2 2"
    IL_28d2:  call       void [System.Console]System.Console::WriteLine(string)
    IL_28d7:  ldloc.0
    IL_28d8:  ldloc.s    V_10
    IL_28da:  beq.s      IL_28f6

    IL_28dc:  ldc.i4.1
    IL_28dd:  stloc.s    V_20
    IL_28df:  ldstr      "te0 check failed {0} {1}"
    IL_28e4:  ldloc.0
    IL_28e5:  box        [mscorlib]System.Int32
    IL_28ea:  ldloc.s    V_10
    IL_28ec:  box        [mscorlib]System.Int32
    IL_28f1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_28f6:  ldloc.1
    IL_28f7:  ldloc.s    V_11
    IL_28f9:  beq.s      IL_2915

    IL_28fb:  ldc.i4.1
    IL_28fc:  stloc.s    V_20
    IL_28fe:  ldstr      "te1 check failed {0} {1}"
    IL_2903:  ldloc.1
    IL_2904:  box        [mscorlib]System.Int32
    IL_2909:  ldloc.s    V_11
    IL_290b:  box        [mscorlib]System.Int32
    IL_2910:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2915:  ldloc.2
    IL_2916:  ldloc.s    V_12
    IL_2918:  beq.s      IL_2934

    IL_291a:  ldc.i4.1
    IL_291b:  stloc.s    V_20
    IL_291d:  ldstr      "te2 check failed {0} {1}"
    IL_2922:  ldloc.2
    IL_2923:  box        [mscorlib]System.Int32
    IL_2928:  ldloc.s    V_12
    IL_292a:  box        [mscorlib]System.Int32
    IL_292f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2934:  ldloc.3
    IL_2935:  ldloc.s    V_13
    IL_2937:  beq.s      IL_2953

    IL_2939:  ldc.i4.1
    IL_293a:  stloc.s    V_20
    IL_293c:  ldstr      "te3 check failed {0} {1}"
    IL_2941:  ldloc.3
    IL_2942:  box        [mscorlib]System.Int32
    IL_2947:  ldloc.s    V_13
    IL_2949:  box        [mscorlib]System.Int32
    IL_294e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2953:  ldloc.s    V_4
    IL_2955:  ldloc.s    V_14
    IL_2957:  beq.s      IL_2974

    IL_2959:  ldc.i4.1
    IL_295a:  stloc.s    V_20
    IL_295c:  ldstr      "te064 check failed {0} {1}"
    IL_2961:  ldloc.s    V_4
    IL_2963:  box        [mscorlib]System.Int64
    IL_2968:  ldloc.s    V_14
    IL_296a:  box        [mscorlib]System.Int64
    IL_296f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2974:  ldloc.s    V_5
    IL_2976:  ldloc.s    V_15
    IL_2978:  beq.s      IL_2995

    IL_297a:  ldc.i4.1
    IL_297b:  stloc.s    V_20
    IL_297d:  ldstr      "te164 check failed {0} {1}"
    IL_2982:  ldloc.s    V_5
    IL_2984:  box        [mscorlib]System.Int64
    IL_2989:  ldloc.s    V_15
    IL_298b:  box        [mscorlib]System.Int64
    IL_2990:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2995:  ldloc.s    V_6
    IL_2997:  ldloc.s    V_16
    IL_2999:  beq.s      IL_29b6

    IL_299b:  ldc.i4.1
    IL_299c:  stloc.s    V_20
    IL_299e:  ldstr      "te264 check failed {0} {1}"
    IL_29a3:  ldloc.s    V_6
    IL_29a5:  box        [mscorlib]System.Int64
    IL_29aa:  ldloc.s    V_16
    IL_29ac:  box        [mscorlib]System.Int64
    IL_29b1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_29b6:  ldloc.s    V_7
    IL_29b8:  ldloc.s    V_17
    IL_29ba:  beq.s      IL_29d7

    IL_29bc:  ldc.i4.1
    IL_29bd:  stloc.s    V_20
    IL_29bf:  ldstr      "te364 check failed {0} {1}"
    IL_29c4:  ldloc.s    V_7
    IL_29c6:  box        [mscorlib]System.Int64
    IL_29cb:  ldloc.s    V_17
    IL_29cd:  box        [mscorlib]System.Int64
    IL_29d2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_29d7:  ldloc.s    V_8
    IL_29d9:  ldloc.s    V_18
    IL_29db:  beq.s      IL_29f8

    IL_29dd:  ldc.i4.1
    IL_29de:  stloc.s    V_20
    IL_29e0:  ldstr      "dummy check failed {0} {1}"
    IL_29e5:  ldloc.s    V_8
    IL_29e7:  box        [mscorlib]System.Int32
    IL_29ec:  ldloc.s    V_18
    IL_29ee:  box        [mscorlib]System.Int32
    IL_29f3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_29f8:  ldloc.s    V_9
    IL_29fa:  ldloc.s    V_19
    IL_29fc:  beq.s      IL_2a19

    IL_29fe:  ldc.i4.1
    IL_29ff:  stloc.s    V_20
    IL_2a01:  ldstr      "dummy64 check failed {0} {1}"
    IL_2a06:  ldloc.s    V_9
    IL_2a08:  box        [mscorlib]System.Int64
    IL_2a0d:  ldloc.s    V_19
    IL_2a0f:  box        [mscorlib]System.Int64
    IL_2a14:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2a19:  ldsfld     int32 IntrinsicTest::counter
    IL_2a1e:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2a23:  beq.s      IL_2a46

    IL_2a25:  ldstr      "counter mismatch {0} {1}"
    IL_2a2a:  ldsfld     int32 IntrinsicTest::counter
    IL_2a2f:  box        [mscorlib]System.Int32
    IL_2a34:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2a39:  box        [mscorlib]System.Int32
    IL_2a3e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2a43:  ldc.i4.1
    IL_2a44:  stloc.s    V_20
    IL_2a46:  ldsfld     int64 IntrinsicTest::counter64
    IL_2a4b:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2a50:  beq.s      IL_2a73

    IL_2a52:  ldstr      "counter64 mismatch {0} {1}"
    IL_2a57:  ldsfld     int64 IntrinsicTest::counter64
    IL_2a5c:  box        [mscorlib]System.Int64
    IL_2a61:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2a66:  box        [mscorlib]System.Int64
    IL_2a6b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2a70:  ldc.i4.1
    IL_2a71:  stloc.s    V_20
    IL_2a73:  ldloc.s    V_21
    IL_2a75:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_2a7a:  ldloc.s    V_21
    IL_2a7c:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_2a81:  beq.s      IL_2aa8

    IL_2a83:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_2a88:  ldloc.s    V_21
    IL_2a8a:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_2a8f:  box        [mscorlib]System.Int32
    IL_2a94:  ldloc.s    V_21
    IL_2a96:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_2a9b:  box        [mscorlib]System.Int32
    IL_2aa0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2aa5:  ldc.i4.1
    IL_2aa6:  stloc.s    V_20
    IL_2aa8:  ldloc.s    V_21
    IL_2aaa:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_2aaf:  ldloc.s    V_21
    IL_2ab1:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_2ab6:  beq.s      IL_2add

    IL_2ab8:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_2abd:  ldloc.s    V_21
    IL_2abf:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_2ac4:  box        [mscorlib]System.Int64
    IL_2ac9:  ldloc.s    V_21
    IL_2acb:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_2ad0:  box        [mscorlib]System.Int64
    IL_2ad5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2ada:  ldc.i4.1
    IL_2adb:  stloc.s    V_20
    IL_2add:  ldsfld     int64 IntrinsicTest::temp
    IL_2ae2:  ldsfld     int64 IntrinsicTest::idmp
    IL_2ae7:  beq.s      IL_2b0a

    IL_2ae9:  ldstr      "temp mismatch {0} {1}"
    IL_2aee:  ldsfld     int64 IntrinsicTest::temp
    IL_2af3:  box        [mscorlib]System.Int64
    IL_2af8:  ldsfld     int64 IntrinsicTest::idmp
    IL_2afd:  box        [mscorlib]System.Int64
    IL_2b02:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2b07:  ldc.i4.1
    IL_2b08:  stloc.s    V_20
    IL_2b0a:  ldsflda    int64 IntrinsicTest::counter64
    IL_2b0f:  ldsfld     int64 IntrinsicTest::counter64
    IL_2b14:  ldloc.s    V_21
    IL_2b16:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_2b1b:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_2b20:  stloc.s    V_4
    IL_2b22:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2b27:  stloc.s    V_14
    IL_2b29:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2b2e:  ldloc.s    V_21
    IL_2b30:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_2b35:  bne.un.s   IL_2b43

    IL_2b37:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2b3c:  stsfld     int64 IntrinsicTest::id_counter64
    IL_2b41:  br.s       IL_2b4d

    IL_2b43:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2b48:  stsfld     int64 IntrinsicTest::idjunk
    IL_2b4d:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 2 3"
    IL_2b52:  call       void [System.Console]System.Console::WriteLine(string)
    IL_2b57:  ldloc.0
    IL_2b58:  ldloc.s    V_10
    IL_2b5a:  beq.s      IL_2b76

    IL_2b5c:  ldc.i4.1
    IL_2b5d:  stloc.s    V_20
    IL_2b5f:  ldstr      "te0 check failed {0} {1}"
    IL_2b64:  ldloc.0
    IL_2b65:  box        [mscorlib]System.Int32
    IL_2b6a:  ldloc.s    V_10
    IL_2b6c:  box        [mscorlib]System.Int32
    IL_2b71:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2b76:  ldloc.1
    IL_2b77:  ldloc.s    V_11
    IL_2b79:  beq.s      IL_2b95

    IL_2b7b:  ldc.i4.1
    IL_2b7c:  stloc.s    V_20
    IL_2b7e:  ldstr      "te1 check failed {0} {1}"
    IL_2b83:  ldloc.1
    IL_2b84:  box        [mscorlib]System.Int32
    IL_2b89:  ldloc.s    V_11
    IL_2b8b:  box        [mscorlib]System.Int32
    IL_2b90:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2b95:  ldloc.2
    IL_2b96:  ldloc.s    V_12
    IL_2b98:  beq.s      IL_2bb4

    IL_2b9a:  ldc.i4.1
    IL_2b9b:  stloc.s    V_20
    IL_2b9d:  ldstr      "te2 check failed {0} {1}"
    IL_2ba2:  ldloc.2
    IL_2ba3:  box        [mscorlib]System.Int32
    IL_2ba8:  ldloc.s    V_12
    IL_2baa:  box        [mscorlib]System.Int32
    IL_2baf:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2bb4:  ldloc.3
    IL_2bb5:  ldloc.s    V_13
    IL_2bb7:  beq.s      IL_2bd3

    IL_2bb9:  ldc.i4.1
    IL_2bba:  stloc.s    V_20
    IL_2bbc:  ldstr      "te3 check failed {0} {1}"
    IL_2bc1:  ldloc.3
    IL_2bc2:  box        [mscorlib]System.Int32
    IL_2bc7:  ldloc.s    V_13
    IL_2bc9:  box        [mscorlib]System.Int32
    IL_2bce:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2bd3:  ldloc.s    V_4
    IL_2bd5:  ldloc.s    V_14
    IL_2bd7:  beq.s      IL_2bf4

    IL_2bd9:  ldc.i4.1
    IL_2bda:  stloc.s    V_20
    IL_2bdc:  ldstr      "te064 check failed {0} {1}"
    IL_2be1:  ldloc.s    V_4
    IL_2be3:  box        [mscorlib]System.Int64
    IL_2be8:  ldloc.s    V_14
    IL_2bea:  box        [mscorlib]System.Int64
    IL_2bef:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2bf4:  ldloc.s    V_5
    IL_2bf6:  ldloc.s    V_15
    IL_2bf8:  beq.s      IL_2c15

    IL_2bfa:  ldc.i4.1
    IL_2bfb:  stloc.s    V_20
    IL_2bfd:  ldstr      "te164 check failed {0} {1}"
    IL_2c02:  ldloc.s    V_5
    IL_2c04:  box        [mscorlib]System.Int64
    IL_2c09:  ldloc.s    V_15
    IL_2c0b:  box        [mscorlib]System.Int64
    IL_2c10:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2c15:  ldloc.s    V_6
    IL_2c17:  ldloc.s    V_16
    IL_2c19:  beq.s      IL_2c36

    IL_2c1b:  ldc.i4.1
    IL_2c1c:  stloc.s    V_20
    IL_2c1e:  ldstr      "te264 check failed {0} {1}"
    IL_2c23:  ldloc.s    V_6
    IL_2c25:  box        [mscorlib]System.Int64
    IL_2c2a:  ldloc.s    V_16
    IL_2c2c:  box        [mscorlib]System.Int64
    IL_2c31:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2c36:  ldloc.s    V_7
    IL_2c38:  ldloc.s    V_17
    IL_2c3a:  beq.s      IL_2c57

    IL_2c3c:  ldc.i4.1
    IL_2c3d:  stloc.s    V_20
    IL_2c3f:  ldstr      "te364 check failed {0} {1}"
    IL_2c44:  ldloc.s    V_7
    IL_2c46:  box        [mscorlib]System.Int64
    IL_2c4b:  ldloc.s    V_17
    IL_2c4d:  box        [mscorlib]System.Int64
    IL_2c52:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2c57:  ldloc.s    V_8
    IL_2c59:  ldloc.s    V_18
    IL_2c5b:  beq.s      IL_2c78

    IL_2c5d:  ldc.i4.1
    IL_2c5e:  stloc.s    V_20
    IL_2c60:  ldstr      "dummy check failed {0} {1}"
    IL_2c65:  ldloc.s    V_8
    IL_2c67:  box        [mscorlib]System.Int32
    IL_2c6c:  ldloc.s    V_18
    IL_2c6e:  box        [mscorlib]System.Int32
    IL_2c73:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2c78:  ldloc.s    V_9
    IL_2c7a:  ldloc.s    V_19
    IL_2c7c:  beq.s      IL_2c99

    IL_2c7e:  ldc.i4.1
    IL_2c7f:  stloc.s    V_20
    IL_2c81:  ldstr      "dummy64 check failed {0} {1}"
    IL_2c86:  ldloc.s    V_9
    IL_2c88:  box        [mscorlib]System.Int64
    IL_2c8d:  ldloc.s    V_19
    IL_2c8f:  box        [mscorlib]System.Int64
    IL_2c94:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2c99:  ldsfld     int32 IntrinsicTest::counter
    IL_2c9e:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2ca3:  beq.s      IL_2cc6

    IL_2ca5:  ldstr      "counter mismatch {0} {1}"
    IL_2caa:  ldsfld     int32 IntrinsicTest::counter
    IL_2caf:  box        [mscorlib]System.Int32
    IL_2cb4:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2cb9:  box        [mscorlib]System.Int32
    IL_2cbe:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2cc3:  ldc.i4.1
    IL_2cc4:  stloc.s    V_20
    IL_2cc6:  ldsfld     int64 IntrinsicTest::counter64
    IL_2ccb:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2cd0:  beq.s      IL_2cf3

    IL_2cd2:  ldstr      "counter64 mismatch {0} {1}"
    IL_2cd7:  ldsfld     int64 IntrinsicTest::counter64
    IL_2cdc:  box        [mscorlib]System.Int64
    IL_2ce1:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2ce6:  box        [mscorlib]System.Int64
    IL_2ceb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2cf0:  ldc.i4.1
    IL_2cf1:  stloc.s    V_20
    IL_2cf3:  ldloc.s    V_21
    IL_2cf5:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_2cfa:  ldloc.s    V_21
    IL_2cfc:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_2d01:  beq.s      IL_2d28

    IL_2d03:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_2d08:  ldloc.s    V_21
    IL_2d0a:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_2d0f:  box        [mscorlib]System.Int32
    IL_2d14:  ldloc.s    V_21
    IL_2d16:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_2d1b:  box        [mscorlib]System.Int32
    IL_2d20:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2d25:  ldc.i4.1
    IL_2d26:  stloc.s    V_20
    IL_2d28:  ldloc.s    V_21
    IL_2d2a:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_2d2f:  ldloc.s    V_21
    IL_2d31:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_2d36:  beq.s      IL_2d5d

    IL_2d38:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_2d3d:  ldloc.s    V_21
    IL_2d3f:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_2d44:  box        [mscorlib]System.Int64
    IL_2d49:  ldloc.s    V_21
    IL_2d4b:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_2d50:  box        [mscorlib]System.Int64
    IL_2d55:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2d5a:  ldc.i4.1
    IL_2d5b:  stloc.s    V_20
    IL_2d5d:  ldsfld     int64 IntrinsicTest::temp
    IL_2d62:  ldsfld     int64 IntrinsicTest::idmp
    IL_2d67:  beq.s      IL_2d8a

    IL_2d69:  ldstr      "temp mismatch {0} {1}"
    IL_2d6e:  ldsfld     int64 IntrinsicTest::temp
    IL_2d73:  box        [mscorlib]System.Int64
    IL_2d78:  ldsfld     int64 IntrinsicTest::idmp
    IL_2d7d:  box        [mscorlib]System.Int64
    IL_2d82:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2d87:  ldc.i4.1
    IL_2d88:  stloc.s    V_20
    IL_2d8a:  ldsflda    int64 IntrinsicTest::counter64
    IL_2d8f:  ldsfld     int64 IntrinsicTest::counter64
    IL_2d94:  ldloc.s    V_21
    IL_2d96:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_2d9b:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_2da0:  stsfld     int64 IntrinsicTest::counter64
    IL_2da5:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2daa:  stsfld     int64 IntrinsicTest::id_counter64
    IL_2daf:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2db4:  ldloc.s    V_21
    IL_2db6:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_2dbb:  bne.un.s   IL_2dc9

    IL_2dbd:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2dc2:  stsfld     int64 IntrinsicTest::idjunk
    IL_2dc7:  br.s       IL_2dd3

    IL_2dc9:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2dce:  stsfld     int64 IntrinsicTest::idjunk
    IL_2dd3:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 2 4"
    IL_2dd8:  call       void [System.Console]System.Console::WriteLine(string)
    IL_2ddd:  ldloc.0
    IL_2dde:  ldloc.s    V_10
    IL_2de0:  beq.s      IL_2dfc

    IL_2de2:  ldc.i4.1
    IL_2de3:  stloc.s    V_20
    IL_2de5:  ldstr      "te0 check failed {0} {1}"
    IL_2dea:  ldloc.0
    IL_2deb:  box        [mscorlib]System.Int32
    IL_2df0:  ldloc.s    V_10
    IL_2df2:  box        [mscorlib]System.Int32
    IL_2df7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2dfc:  ldloc.1
    IL_2dfd:  ldloc.s    V_11
    IL_2dff:  beq.s      IL_2e1b

    IL_2e01:  ldc.i4.1
    IL_2e02:  stloc.s    V_20
    IL_2e04:  ldstr      "te1 check failed {0} {1}"
    IL_2e09:  ldloc.1
    IL_2e0a:  box        [mscorlib]System.Int32
    IL_2e0f:  ldloc.s    V_11
    IL_2e11:  box        [mscorlib]System.Int32
    IL_2e16:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2e1b:  ldloc.2
    IL_2e1c:  ldloc.s    V_12
    IL_2e1e:  beq.s      IL_2e3a

    IL_2e20:  ldc.i4.1
    IL_2e21:  stloc.s    V_20
    IL_2e23:  ldstr      "te2 check failed {0} {1}"
    IL_2e28:  ldloc.2
    IL_2e29:  box        [mscorlib]System.Int32
    IL_2e2e:  ldloc.s    V_12
    IL_2e30:  box        [mscorlib]System.Int32
    IL_2e35:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2e3a:  ldloc.3
    IL_2e3b:  ldloc.s    V_13
    IL_2e3d:  beq.s      IL_2e59

    IL_2e3f:  ldc.i4.1
    IL_2e40:  stloc.s    V_20
    IL_2e42:  ldstr      "te3 check failed {0} {1}"
    IL_2e47:  ldloc.3
    IL_2e48:  box        [mscorlib]System.Int32
    IL_2e4d:  ldloc.s    V_13
    IL_2e4f:  box        [mscorlib]System.Int32
    IL_2e54:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2e59:  ldloc.s    V_4
    IL_2e5b:  ldloc.s    V_14
    IL_2e5d:  beq.s      IL_2e7a

    IL_2e5f:  ldc.i4.1
    IL_2e60:  stloc.s    V_20
    IL_2e62:  ldstr      "te064 check failed {0} {1}"
    IL_2e67:  ldloc.s    V_4
    IL_2e69:  box        [mscorlib]System.Int64
    IL_2e6e:  ldloc.s    V_14
    IL_2e70:  box        [mscorlib]System.Int64
    IL_2e75:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2e7a:  ldloc.s    V_5
    IL_2e7c:  ldloc.s    V_15
    IL_2e7e:  beq.s      IL_2e9b

    IL_2e80:  ldc.i4.1
    IL_2e81:  stloc.s    V_20
    IL_2e83:  ldstr      "te164 check failed {0} {1}"
    IL_2e88:  ldloc.s    V_5
    IL_2e8a:  box        [mscorlib]System.Int64
    IL_2e8f:  ldloc.s    V_15
    IL_2e91:  box        [mscorlib]System.Int64
    IL_2e96:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2e9b:  ldloc.s    V_6
    IL_2e9d:  ldloc.s    V_16
    IL_2e9f:  beq.s      IL_2ebc

    IL_2ea1:  ldc.i4.1
    IL_2ea2:  stloc.s    V_20
    IL_2ea4:  ldstr      "te264 check failed {0} {1}"
    IL_2ea9:  ldloc.s    V_6
    IL_2eab:  box        [mscorlib]System.Int64
    IL_2eb0:  ldloc.s    V_16
    IL_2eb2:  box        [mscorlib]System.Int64
    IL_2eb7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2ebc:  ldloc.s    V_7
    IL_2ebe:  ldloc.s    V_17
    IL_2ec0:  beq.s      IL_2edd

    IL_2ec2:  ldc.i4.1
    IL_2ec3:  stloc.s    V_20
    IL_2ec5:  ldstr      "te364 check failed {0} {1}"
    IL_2eca:  ldloc.s    V_7
    IL_2ecc:  box        [mscorlib]System.Int64
    IL_2ed1:  ldloc.s    V_17
    IL_2ed3:  box        [mscorlib]System.Int64
    IL_2ed8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2edd:  ldloc.s    V_8
    IL_2edf:  ldloc.s    V_18
    IL_2ee1:  beq.s      IL_2efe

    IL_2ee3:  ldc.i4.1
    IL_2ee4:  stloc.s    V_20
    IL_2ee6:  ldstr      "dummy check failed {0} {1}"
    IL_2eeb:  ldloc.s    V_8
    IL_2eed:  box        [mscorlib]System.Int32
    IL_2ef2:  ldloc.s    V_18
    IL_2ef4:  box        [mscorlib]System.Int32
    IL_2ef9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2efe:  ldloc.s    V_9
    IL_2f00:  ldloc.s    V_19
    IL_2f02:  beq.s      IL_2f1f

    IL_2f04:  ldc.i4.1
    IL_2f05:  stloc.s    V_20
    IL_2f07:  ldstr      "dummy64 check failed {0} {1}"
    IL_2f0c:  ldloc.s    V_9
    IL_2f0e:  box        [mscorlib]System.Int64
    IL_2f13:  ldloc.s    V_19
    IL_2f15:  box        [mscorlib]System.Int64
    IL_2f1a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2f1f:  ldsfld     int32 IntrinsicTest::counter
    IL_2f24:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2f29:  beq.s      IL_2f4c

    IL_2f2b:  ldstr      "counter mismatch {0} {1}"
    IL_2f30:  ldsfld     int32 IntrinsicTest::counter
    IL_2f35:  box        [mscorlib]System.Int32
    IL_2f3a:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2f3f:  box        [mscorlib]System.Int32
    IL_2f44:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2f49:  ldc.i4.1
    IL_2f4a:  stloc.s    V_20
    IL_2f4c:  ldsfld     int64 IntrinsicTest::counter64
    IL_2f51:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2f56:  beq.s      IL_2f79

    IL_2f58:  ldstr      "counter64 mismatch {0} {1}"
    IL_2f5d:  ldsfld     int64 IntrinsicTest::counter64
    IL_2f62:  box        [mscorlib]System.Int64
    IL_2f67:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2f6c:  box        [mscorlib]System.Int64
    IL_2f71:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2f76:  ldc.i4.1
    IL_2f77:  stloc.s    V_20
    IL_2f79:  ldloc.s    V_21
    IL_2f7b:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_2f80:  ldloc.s    V_21
    IL_2f82:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_2f87:  beq.s      IL_2fae

    IL_2f89:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_2f8e:  ldloc.s    V_21
    IL_2f90:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_2f95:  box        [mscorlib]System.Int32
    IL_2f9a:  ldloc.s    V_21
    IL_2f9c:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_2fa1:  box        [mscorlib]System.Int32
    IL_2fa6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2fab:  ldc.i4.1
    IL_2fac:  stloc.s    V_20
    IL_2fae:  ldloc.s    V_21
    IL_2fb0:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_2fb5:  ldloc.s    V_21
    IL_2fb7:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_2fbc:  beq.s      IL_2fe3

    IL_2fbe:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_2fc3:  ldloc.s    V_21
    IL_2fc5:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_2fca:  box        [mscorlib]System.Int64
    IL_2fcf:  ldloc.s    V_21
    IL_2fd1:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_2fd6:  box        [mscorlib]System.Int64
    IL_2fdb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2fe0:  ldc.i4.1
    IL_2fe1:  stloc.s    V_20
    IL_2fe3:  ldsfld     int64 IntrinsicTest::temp
    IL_2fe8:  ldsfld     int64 IntrinsicTest::idmp
    IL_2fed:  beq.s      IL_3010

    IL_2fef:  ldstr      "temp mismatch {0} {1}"
    IL_2ff4:  ldsfld     int64 IntrinsicTest::temp
    IL_2ff9:  box        [mscorlib]System.Int64
    IL_2ffe:  ldsfld     int64 IntrinsicTest::idmp
    IL_3003:  box        [mscorlib]System.Int64
    IL_3008:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_300d:  ldc.i4.1
    IL_300e:  stloc.s    V_20
    IL_3010:  ldloc.s    V_21
    IL_3012:  ldsflda    int64 IntrinsicTest::counter64
    IL_3017:  ldsfld     int64 IntrinsicTest::counter64
    IL_301c:  ldloc.s    V_21
    IL_301e:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_3023:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_3028:  stfld      int64 IntrinsicTest::instanceCounter64
    IL_302d:  ldloc.s    V_21
    IL_302f:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3034:  stfld      int64 IntrinsicTest::id_instanceCounter64
    IL_3039:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_303e:  ldloc.s    V_21
    IL_3040:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_3045:  bne.un.s   IL_3053

    IL_3047:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_304c:  stsfld     int64 IntrinsicTest::id_counter64
    IL_3051:  br.s       IL_305d

    IL_3053:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3058:  stsfld     int64 IntrinsicTest::idjunk
    IL_305d:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 2 5"
    IL_3062:  call       void [System.Console]System.Console::WriteLine(string)
    IL_3067:  ldloc.0
    IL_3068:  ldloc.s    V_10
    IL_306a:  beq.s      IL_3086

    IL_306c:  ldc.i4.1
    IL_306d:  stloc.s    V_20
    IL_306f:  ldstr      "te0 check failed {0} {1}"
    IL_3074:  ldloc.0
    IL_3075:  box        [mscorlib]System.Int32
    IL_307a:  ldloc.s    V_10
    IL_307c:  box        [mscorlib]System.Int32
    IL_3081:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3086:  ldloc.1
    IL_3087:  ldloc.s    V_11
    IL_3089:  beq.s      IL_30a5

    IL_308b:  ldc.i4.1
    IL_308c:  stloc.s    V_20
    IL_308e:  ldstr      "te1 check failed {0} {1}"
    IL_3093:  ldloc.1
    IL_3094:  box        [mscorlib]System.Int32
    IL_3099:  ldloc.s    V_11
    IL_309b:  box        [mscorlib]System.Int32
    IL_30a0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_30a5:  ldloc.2
    IL_30a6:  ldloc.s    V_12
    IL_30a8:  beq.s      IL_30c4

    IL_30aa:  ldc.i4.1
    IL_30ab:  stloc.s    V_20
    IL_30ad:  ldstr      "te2 check failed {0} {1}"
    IL_30b2:  ldloc.2
    IL_30b3:  box        [mscorlib]System.Int32
    IL_30b8:  ldloc.s    V_12
    IL_30ba:  box        [mscorlib]System.Int32
    IL_30bf:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_30c4:  ldloc.3
    IL_30c5:  ldloc.s    V_13
    IL_30c7:  beq.s      IL_30e3

    IL_30c9:  ldc.i4.1
    IL_30ca:  stloc.s    V_20
    IL_30cc:  ldstr      "te3 check failed {0} {1}"
    IL_30d1:  ldloc.3
    IL_30d2:  box        [mscorlib]System.Int32
    IL_30d7:  ldloc.s    V_13
    IL_30d9:  box        [mscorlib]System.Int32
    IL_30de:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_30e3:  ldloc.s    V_4
    IL_30e5:  ldloc.s    V_14
    IL_30e7:  beq.s      IL_3104

    IL_30e9:  ldc.i4.1
    IL_30ea:  stloc.s    V_20
    IL_30ec:  ldstr      "te064 check failed {0} {1}"
    IL_30f1:  ldloc.s    V_4
    IL_30f3:  box        [mscorlib]System.Int64
    IL_30f8:  ldloc.s    V_14
    IL_30fa:  box        [mscorlib]System.Int64
    IL_30ff:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3104:  ldloc.s    V_5
    IL_3106:  ldloc.s    V_15
    IL_3108:  beq.s      IL_3125

    IL_310a:  ldc.i4.1
    IL_310b:  stloc.s    V_20
    IL_310d:  ldstr      "te164 check failed {0} {1}"
    IL_3112:  ldloc.s    V_5
    IL_3114:  box        [mscorlib]System.Int64
    IL_3119:  ldloc.s    V_15
    IL_311b:  box        [mscorlib]System.Int64
    IL_3120:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3125:  ldloc.s    V_6
    IL_3127:  ldloc.s    V_16
    IL_3129:  beq.s      IL_3146

    IL_312b:  ldc.i4.1
    IL_312c:  stloc.s    V_20
    IL_312e:  ldstr      "te264 check failed {0} {1}"
    IL_3133:  ldloc.s    V_6
    IL_3135:  box        [mscorlib]System.Int64
    IL_313a:  ldloc.s    V_16
    IL_313c:  box        [mscorlib]System.Int64
    IL_3141:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3146:  ldloc.s    V_7
    IL_3148:  ldloc.s    V_17
    IL_314a:  beq.s      IL_3167

    IL_314c:  ldc.i4.1
    IL_314d:  stloc.s    V_20
    IL_314f:  ldstr      "te364 check failed {0} {1}"
    IL_3154:  ldloc.s    V_7
    IL_3156:  box        [mscorlib]System.Int64
    IL_315b:  ldloc.s    V_17
    IL_315d:  box        [mscorlib]System.Int64
    IL_3162:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3167:  ldloc.s    V_8
    IL_3169:  ldloc.s    V_18
    IL_316b:  beq.s      IL_3188

    IL_316d:  ldc.i4.1
    IL_316e:  stloc.s    V_20
    IL_3170:  ldstr      "dummy check failed {0} {1}"
    IL_3175:  ldloc.s    V_8
    IL_3177:  box        [mscorlib]System.Int32
    IL_317c:  ldloc.s    V_18
    IL_317e:  box        [mscorlib]System.Int32
    IL_3183:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3188:  ldloc.s    V_9
    IL_318a:  ldloc.s    V_19
    IL_318c:  beq.s      IL_31a9

    IL_318e:  ldc.i4.1
    IL_318f:  stloc.s    V_20
    IL_3191:  ldstr      "dummy64 check failed {0} {1}"
    IL_3196:  ldloc.s    V_9
    IL_3198:  box        [mscorlib]System.Int64
    IL_319d:  ldloc.s    V_19
    IL_319f:  box        [mscorlib]System.Int64
    IL_31a4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_31a9:  ldsfld     int32 IntrinsicTest::counter
    IL_31ae:  ldsfld     int32 IntrinsicTest::id_counter
    IL_31b3:  beq.s      IL_31d6

    IL_31b5:  ldstr      "counter mismatch {0} {1}"
    IL_31ba:  ldsfld     int32 IntrinsicTest::counter
    IL_31bf:  box        [mscorlib]System.Int32
    IL_31c4:  ldsfld     int32 IntrinsicTest::id_counter
    IL_31c9:  box        [mscorlib]System.Int32
    IL_31ce:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_31d3:  ldc.i4.1
    IL_31d4:  stloc.s    V_20
    IL_31d6:  ldsfld     int64 IntrinsicTest::counter64
    IL_31db:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_31e0:  beq.s      IL_3203

    IL_31e2:  ldstr      "counter64 mismatch {0} {1}"
    IL_31e7:  ldsfld     int64 IntrinsicTest::counter64
    IL_31ec:  box        [mscorlib]System.Int64
    IL_31f1:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_31f6:  box        [mscorlib]System.Int64
    IL_31fb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3200:  ldc.i4.1
    IL_3201:  stloc.s    V_20
    IL_3203:  ldloc.s    V_21
    IL_3205:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_320a:  ldloc.s    V_21
    IL_320c:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_3211:  beq.s      IL_3238

    IL_3213:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_3218:  ldloc.s    V_21
    IL_321a:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_321f:  box        [mscorlib]System.Int32
    IL_3224:  ldloc.s    V_21
    IL_3226:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_322b:  box        [mscorlib]System.Int32
    IL_3230:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3235:  ldc.i4.1
    IL_3236:  stloc.s    V_20
    IL_3238:  ldloc.s    V_21
    IL_323a:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_323f:  ldloc.s    V_21
    IL_3241:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_3246:  beq.s      IL_326d

    IL_3248:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_324d:  ldloc.s    V_21
    IL_324f:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_3254:  box        [mscorlib]System.Int64
    IL_3259:  ldloc.s    V_21
    IL_325b:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_3260:  box        [mscorlib]System.Int64
    IL_3265:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_326a:  ldc.i4.1
    IL_326b:  stloc.s    V_20
    IL_326d:  ldsfld     int64 IntrinsicTest::temp
    IL_3272:  ldsfld     int64 IntrinsicTest::idmp
    IL_3277:  beq.s      IL_329a

    IL_3279:  ldstr      "temp mismatch {0} {1}"
    IL_327e:  ldsfld     int64 IntrinsicTest::temp
    IL_3283:  box        [mscorlib]System.Int64
    IL_3288:  ldsfld     int64 IntrinsicTest::idmp
    IL_328d:  box        [mscorlib]System.Int64
    IL_3292:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3297:  ldc.i4.1
    IL_3298:  stloc.s    V_20
    IL_329a:  ldsflda    int64 IntrinsicTest::counter64
    IL_329f:  ldsfld     int64 IntrinsicTest::counter64
    IL_32a4:  ldloc.s    V_21
    IL_32a6:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_32ab:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_32b0:  stloc.s    V_7
    IL_32b2:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_32b7:  stloc.s    V_17
    IL_32b9:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_32be:  ldloc.s    V_21
    IL_32c0:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_32c5:  bne.un.s   IL_32d3

    IL_32c7:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_32cc:  stsfld     int64 IntrinsicTest::id_counter64
    IL_32d1:  br.s       IL_32dd

    IL_32d3:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_32d8:  stsfld     int64 IntrinsicTest::idjunk
    IL_32dd:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 2 6"
    IL_32e2:  call       void [System.Console]System.Console::WriteLine(string)
    IL_32e7:  ldloc.0
    IL_32e8:  ldloc.s    V_10
    IL_32ea:  beq.s      IL_3306

    IL_32ec:  ldc.i4.1
    IL_32ed:  stloc.s    V_20
    IL_32ef:  ldstr      "te0 check failed {0} {1}"
    IL_32f4:  ldloc.0
    IL_32f5:  box        [mscorlib]System.Int32
    IL_32fa:  ldloc.s    V_10
    IL_32fc:  box        [mscorlib]System.Int32
    IL_3301:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3306:  ldloc.1
    IL_3307:  ldloc.s    V_11
    IL_3309:  beq.s      IL_3325

    IL_330b:  ldc.i4.1
    IL_330c:  stloc.s    V_20
    IL_330e:  ldstr      "te1 check failed {0} {1}"
    IL_3313:  ldloc.1
    IL_3314:  box        [mscorlib]System.Int32
    IL_3319:  ldloc.s    V_11
    IL_331b:  box        [mscorlib]System.Int32
    IL_3320:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3325:  ldloc.2
    IL_3326:  ldloc.s    V_12
    IL_3328:  beq.s      IL_3344

    IL_332a:  ldc.i4.1
    IL_332b:  stloc.s    V_20
    IL_332d:  ldstr      "te2 check failed {0} {1}"
    IL_3332:  ldloc.2
    IL_3333:  box        [mscorlib]System.Int32
    IL_3338:  ldloc.s    V_12
    IL_333a:  box        [mscorlib]System.Int32
    IL_333f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3344:  ldloc.3
    IL_3345:  ldloc.s    V_13
    IL_3347:  beq.s      IL_3363

    IL_3349:  ldc.i4.1
    IL_334a:  stloc.s    V_20
    IL_334c:  ldstr      "te3 check failed {0} {1}"
    IL_3351:  ldloc.3
    IL_3352:  box        [mscorlib]System.Int32
    IL_3357:  ldloc.s    V_13
    IL_3359:  box        [mscorlib]System.Int32
    IL_335e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3363:  ldloc.s    V_4
    IL_3365:  ldloc.s    V_14
    IL_3367:  beq.s      IL_3384

    IL_3369:  ldc.i4.1
    IL_336a:  stloc.s    V_20
    IL_336c:  ldstr      "te064 check failed {0} {1}"
    IL_3371:  ldloc.s    V_4
    IL_3373:  box        [mscorlib]System.Int64
    IL_3378:  ldloc.s    V_14
    IL_337a:  box        [mscorlib]System.Int64
    IL_337f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3384:  ldloc.s    V_5
    IL_3386:  ldloc.s    V_15
    IL_3388:  beq.s      IL_33a5

    IL_338a:  ldc.i4.1
    IL_338b:  stloc.s    V_20
    IL_338d:  ldstr      "te164 check failed {0} {1}"
    IL_3392:  ldloc.s    V_5
    IL_3394:  box        [mscorlib]System.Int64
    IL_3399:  ldloc.s    V_15
    IL_339b:  box        [mscorlib]System.Int64
    IL_33a0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_33a5:  ldloc.s    V_6
    IL_33a7:  ldloc.s    V_16
    IL_33a9:  beq.s      IL_33c6

    IL_33ab:  ldc.i4.1
    IL_33ac:  stloc.s    V_20
    IL_33ae:  ldstr      "te264 check failed {0} {1}"
    IL_33b3:  ldloc.s    V_6
    IL_33b5:  box        [mscorlib]System.Int64
    IL_33ba:  ldloc.s    V_16
    IL_33bc:  box        [mscorlib]System.Int64
    IL_33c1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_33c6:  ldloc.s    V_7
    IL_33c8:  ldloc.s    V_17
    IL_33ca:  beq.s      IL_33e7

    IL_33cc:  ldc.i4.1
    IL_33cd:  stloc.s    V_20
    IL_33cf:  ldstr      "te364 check failed {0} {1}"
    IL_33d4:  ldloc.s    V_7
    IL_33d6:  box        [mscorlib]System.Int64
    IL_33db:  ldloc.s    V_17
    IL_33dd:  box        [mscorlib]System.Int64
    IL_33e2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_33e7:  ldloc.s    V_8
    IL_33e9:  ldloc.s    V_18
    IL_33eb:  beq.s      IL_3408

    IL_33ed:  ldc.i4.1
    IL_33ee:  stloc.s    V_20
    IL_33f0:  ldstr      "dummy check failed {0} {1}"
    IL_33f5:  ldloc.s    V_8
    IL_33f7:  box        [mscorlib]System.Int32
    IL_33fc:  ldloc.s    V_18
    IL_33fe:  box        [mscorlib]System.Int32
    IL_3403:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3408:  ldloc.s    V_9
    IL_340a:  ldloc.s    V_19
    IL_340c:  beq.s      IL_3429

    IL_340e:  ldc.i4.1
    IL_340f:  stloc.s    V_20
    IL_3411:  ldstr      "dummy64 check failed {0} {1}"
    IL_3416:  ldloc.s    V_9
    IL_3418:  box        [mscorlib]System.Int64
    IL_341d:  ldloc.s    V_19
    IL_341f:  box        [mscorlib]System.Int64
    IL_3424:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3429:  ldsfld     int32 IntrinsicTest::counter
    IL_342e:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3433:  beq.s      IL_3456

    IL_3435:  ldstr      "counter mismatch {0} {1}"
    IL_343a:  ldsfld     int32 IntrinsicTest::counter
    IL_343f:  box        [mscorlib]System.Int32
    IL_3444:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3449:  box        [mscorlib]System.Int32
    IL_344e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3453:  ldc.i4.1
    IL_3454:  stloc.s    V_20
    IL_3456:  ldsfld     int64 IntrinsicTest::counter64
    IL_345b:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3460:  beq.s      IL_3483

    IL_3462:  ldstr      "counter64 mismatch {0} {1}"
    IL_3467:  ldsfld     int64 IntrinsicTest::counter64
    IL_346c:  box        [mscorlib]System.Int64
    IL_3471:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3476:  box        [mscorlib]System.Int64
    IL_347b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3480:  ldc.i4.1
    IL_3481:  stloc.s    V_20
    IL_3483:  ldloc.s    V_21
    IL_3485:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_348a:  ldloc.s    V_21
    IL_348c:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_3491:  beq.s      IL_34b8

    IL_3493:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_3498:  ldloc.s    V_21
    IL_349a:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_349f:  box        [mscorlib]System.Int32
    IL_34a4:  ldloc.s    V_21
    IL_34a6:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_34ab:  box        [mscorlib]System.Int32
    IL_34b0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_34b5:  ldc.i4.1
    IL_34b6:  stloc.s    V_20
    IL_34b8:  ldloc.s    V_21
    IL_34ba:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_34bf:  ldloc.s    V_21
    IL_34c1:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_34c6:  beq.s      IL_34ed

    IL_34c8:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_34cd:  ldloc.s    V_21
    IL_34cf:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_34d4:  box        [mscorlib]System.Int64
    IL_34d9:  ldloc.s    V_21
    IL_34db:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_34e0:  box        [mscorlib]System.Int64
    IL_34e5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_34ea:  ldc.i4.1
    IL_34eb:  stloc.s    V_20
    IL_34ed:  ldsfld     int64 IntrinsicTest::temp
    IL_34f2:  ldsfld     int64 IntrinsicTest::idmp
    IL_34f7:  beq.s      IL_351a

    IL_34f9:  ldstr      "temp mismatch {0} {1}"
    IL_34fe:  ldsfld     int64 IntrinsicTest::temp
    IL_3503:  box        [mscorlib]System.Int64
    IL_3508:  ldsfld     int64 IntrinsicTest::idmp
    IL_350d:  box        [mscorlib]System.Int64
    IL_3512:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3517:  ldc.i4.1
    IL_3518:  stloc.s    V_20
    IL_351a:  ldsflda    int64 IntrinsicTest::counter64
    IL_351f:  ldsfld     int64 IntrinsicTest::counter64
    IL_3524:  ldloc.s    V_21
    IL_3526:  callvirt   instance int32 IntrinsicTest::GetValue()
    IL_352b:  conv.i8
    IL_352c:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_3531:  stloc.s    V_4
    IL_3533:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3538:  stloc.s    V_14
    IL_353a:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_353f:  ldloc.s    V_21
    IL_3541:  callvirt   instance int32 IntrinsicTest::id_GetValue()
    IL_3546:  conv.i8
    IL_3547:  bne.un.s   IL_3555

    IL_3549:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_354e:  stsfld     int64 IntrinsicTest::id_counter64
    IL_3553:  br.s       IL_355f

    IL_3555:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_355a:  stsfld     int64 IntrinsicTest::idjunk
    IL_355f:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 3 0"
    IL_3564:  call       void [System.Console]System.Console::WriteLine(string)
    IL_3569:  ldloc.0
    IL_356a:  ldloc.s    V_10
    IL_356c:  beq.s      IL_3588

    IL_356e:  ldc.i4.1
    IL_356f:  stloc.s    V_20
    IL_3571:  ldstr      "te0 check failed {0} {1}"
    IL_3576:  ldloc.0
    IL_3577:  box        [mscorlib]System.Int32
    IL_357c:  ldloc.s    V_10
    IL_357e:  box        [mscorlib]System.Int32
    IL_3583:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3588:  ldloc.1
    IL_3589:  ldloc.s    V_11
    IL_358b:  beq.s      IL_35a7

    IL_358d:  ldc.i4.1
    IL_358e:  stloc.s    V_20
    IL_3590:  ldstr      "te1 check failed {0} {1}"
    IL_3595:  ldloc.1
    IL_3596:  box        [mscorlib]System.Int32
    IL_359b:  ldloc.s    V_11
    IL_359d:  box        [mscorlib]System.Int32
    IL_35a2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_35a7:  ldloc.2
    IL_35a8:  ldloc.s    V_12
    IL_35aa:  beq.s      IL_35c6

    IL_35ac:  ldc.i4.1
    IL_35ad:  stloc.s    V_20
    IL_35af:  ldstr      "te2 check failed {0} {1}"
    IL_35b4:  ldloc.2
    IL_35b5:  box        [mscorlib]System.Int32
    IL_35ba:  ldloc.s    V_12
    IL_35bc:  box        [mscorlib]System.Int32
    IL_35c1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_35c6:  ldloc.3
    IL_35c7:  ldloc.s    V_13
    IL_35c9:  beq.s      IL_35e5

    IL_35cb:  ldc.i4.1
    IL_35cc:  stloc.s    V_20
    IL_35ce:  ldstr      "te3 check failed {0} {1}"
    IL_35d3:  ldloc.3
    IL_35d4:  box        [mscorlib]System.Int32
    IL_35d9:  ldloc.s    V_13
    IL_35db:  box        [mscorlib]System.Int32
    IL_35e0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_35e5:  ldloc.s    V_4
    IL_35e7:  ldloc.s    V_14
    IL_35e9:  beq.s      IL_3606

    IL_35eb:  ldc.i4.1
    IL_35ec:  stloc.s    V_20
    IL_35ee:  ldstr      "te064 check failed {0} {1}"
    IL_35f3:  ldloc.s    V_4
    IL_35f5:  box        [mscorlib]System.Int64
    IL_35fa:  ldloc.s    V_14
    IL_35fc:  box        [mscorlib]System.Int64
    IL_3601:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3606:  ldloc.s    V_5
    IL_3608:  ldloc.s    V_15
    IL_360a:  beq.s      IL_3627

    IL_360c:  ldc.i4.1
    IL_360d:  stloc.s    V_20
    IL_360f:  ldstr      "te164 check failed {0} {1}"
    IL_3614:  ldloc.s    V_5
    IL_3616:  box        [mscorlib]System.Int64
    IL_361b:  ldloc.s    V_15
    IL_361d:  box        [mscorlib]System.Int64
    IL_3622:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3627:  ldloc.s    V_6
    IL_3629:  ldloc.s    V_16
    IL_362b:  beq.s      IL_3648

    IL_362d:  ldc.i4.1
    IL_362e:  stloc.s    V_20
    IL_3630:  ldstr      "te264 check failed {0} {1}"
    IL_3635:  ldloc.s    V_6
    IL_3637:  box        [mscorlib]System.Int64
    IL_363c:  ldloc.s    V_16
    IL_363e:  box        [mscorlib]System.Int64
    IL_3643:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3648:  ldloc.s    V_7
    IL_364a:  ldloc.s    V_17
    IL_364c:  beq.s      IL_3669

    IL_364e:  ldc.i4.1
    IL_364f:  stloc.s    V_20
    IL_3651:  ldstr      "te364 check failed {0} {1}"
    IL_3656:  ldloc.s    V_7
    IL_3658:  box        [mscorlib]System.Int64
    IL_365d:  ldloc.s    V_17
    IL_365f:  box        [mscorlib]System.Int64
    IL_3664:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3669:  ldloc.s    V_8
    IL_366b:  ldloc.s    V_18
    IL_366d:  beq.s      IL_368a

    IL_366f:  ldc.i4.1
    IL_3670:  stloc.s    V_20
    IL_3672:  ldstr      "dummy check failed {0} {1}"
    IL_3677:  ldloc.s    V_8
    IL_3679:  box        [mscorlib]System.Int32
    IL_367e:  ldloc.s    V_18
    IL_3680:  box        [mscorlib]System.Int32
    IL_3685:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_368a:  ldloc.s    V_9
    IL_368c:  ldloc.s    V_19
    IL_368e:  beq.s      IL_36ab

    IL_3690:  ldc.i4.1
    IL_3691:  stloc.s    V_20
    IL_3693:  ldstr      "dummy64 check failed {0} {1}"
    IL_3698:  ldloc.s    V_9
    IL_369a:  box        [mscorlib]System.Int64
    IL_369f:  ldloc.s    V_19
    IL_36a1:  box        [mscorlib]System.Int64
    IL_36a6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_36ab:  ldsfld     int32 IntrinsicTest::counter
    IL_36b0:  ldsfld     int32 IntrinsicTest::id_counter
    IL_36b5:  beq.s      IL_36d8

    IL_36b7:  ldstr      "counter mismatch {0} {1}"
    IL_36bc:  ldsfld     int32 IntrinsicTest::counter
    IL_36c1:  box        [mscorlib]System.Int32
    IL_36c6:  ldsfld     int32 IntrinsicTest::id_counter
    IL_36cb:  box        [mscorlib]System.Int32
    IL_36d0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_36d5:  ldc.i4.1
    IL_36d6:  stloc.s    V_20
    IL_36d8:  ldsfld     int64 IntrinsicTest::counter64
    IL_36dd:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_36e2:  beq.s      IL_3705

    IL_36e4:  ldstr      "counter64 mismatch {0} {1}"
    IL_36e9:  ldsfld     int64 IntrinsicTest::counter64
    IL_36ee:  box        [mscorlib]System.Int64
    IL_36f3:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_36f8:  box        [mscorlib]System.Int64
    IL_36fd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3702:  ldc.i4.1
    IL_3703:  stloc.s    V_20
    IL_3705:  ldloc.s    V_21
    IL_3707:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_370c:  ldloc.s    V_21
    IL_370e:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_3713:  beq.s      IL_373a

    IL_3715:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_371a:  ldloc.s    V_21
    IL_371c:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_3721:  box        [mscorlib]System.Int32
    IL_3726:  ldloc.s    V_21
    IL_3728:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_372d:  box        [mscorlib]System.Int32
    IL_3732:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3737:  ldc.i4.1
    IL_3738:  stloc.s    V_20
    IL_373a:  ldloc.s    V_21
    IL_373c:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_3741:  ldloc.s    V_21
    IL_3743:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_3748:  beq.s      IL_376f

    IL_374a:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_374f:  ldloc.s    V_21
    IL_3751:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_3756:  box        [mscorlib]System.Int64
    IL_375b:  ldloc.s    V_21
    IL_375d:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_3762:  box        [mscorlib]System.Int64
    IL_3767:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_376c:  ldc.i4.1
    IL_376d:  stloc.s    V_20
    IL_376f:  ldsfld     int64 IntrinsicTest::temp
    IL_3774:  ldsfld     int64 IntrinsicTest::idmp
    IL_3779:  beq.s      IL_379c

    IL_377b:  ldstr      "temp mismatch {0} {1}"
    IL_3780:  ldsfld     int64 IntrinsicTest::temp
    IL_3785:  box        [mscorlib]System.Int64
    IL_378a:  ldsfld     int64 IntrinsicTest::idmp
    IL_378f:  box        [mscorlib]System.Int64
    IL_3794:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3799:  ldc.i4.1
    IL_379a:  stloc.s    V_20
    IL_379c:  ldsflda    int64 IntrinsicTest::counter64
    IL_37a1:  ldsfld     int64 IntrinsicTest::counter64
    IL_37a6:  ldloc.s    V_21
    IL_37a8:  callvirt   instance int32 IntrinsicTest::GetValue()
    IL_37ad:  conv.i8
    IL_37ae:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_37b3:  stsfld     int64 IntrinsicTest::counter64
    IL_37b8:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_37bd:  stsfld     int64 IntrinsicTest::id_counter64
    IL_37c2:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_37c7:  ldloc.s    V_21
    IL_37c9:  callvirt   instance int32 IntrinsicTest::id_GetValue()
    IL_37ce:  conv.i8
    IL_37cf:  bne.un.s   IL_37dd

    IL_37d1:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_37d6:  stsfld     int64 IntrinsicTest::idjunk
    IL_37db:  br.s       IL_37e7

    IL_37dd:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_37e2:  stsfld     int64 IntrinsicTest::idjunk
    IL_37e7:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 3 1"
    IL_37ec:  call       void [System.Console]System.Console::WriteLine(string)
    IL_37f1:  ldloc.0
    IL_37f2:  ldloc.s    V_10
    IL_37f4:  beq.s      IL_3810

    IL_37f6:  ldc.i4.1
    IL_37f7:  stloc.s    V_20
    IL_37f9:  ldstr      "te0 check failed {0} {1}"
    IL_37fe:  ldloc.0
    IL_37ff:  box        [mscorlib]System.Int32
    IL_3804:  ldloc.s    V_10
    IL_3806:  box        [mscorlib]System.Int32
    IL_380b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3810:  ldloc.1
    IL_3811:  ldloc.s    V_11
    IL_3813:  beq.s      IL_382f

    IL_3815:  ldc.i4.1
    IL_3816:  stloc.s    V_20
    IL_3818:  ldstr      "te1 check failed {0} {1}"
    IL_381d:  ldloc.1
    IL_381e:  box        [mscorlib]System.Int32
    IL_3823:  ldloc.s    V_11
    IL_3825:  box        [mscorlib]System.Int32
    IL_382a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_382f:  ldloc.2
    IL_3830:  ldloc.s    V_12
    IL_3832:  beq.s      IL_384e

    IL_3834:  ldc.i4.1
    IL_3835:  stloc.s    V_20
    IL_3837:  ldstr      "te2 check failed {0} {1}"
    IL_383c:  ldloc.2
    IL_383d:  box        [mscorlib]System.Int32
    IL_3842:  ldloc.s    V_12
    IL_3844:  box        [mscorlib]System.Int32
    IL_3849:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_384e:  ldloc.3
    IL_384f:  ldloc.s    V_13
    IL_3851:  beq.s      IL_386d

    IL_3853:  ldc.i4.1
    IL_3854:  stloc.s    V_20
    IL_3856:  ldstr      "te3 check failed {0} {1}"
    IL_385b:  ldloc.3
    IL_385c:  box        [mscorlib]System.Int32
    IL_3861:  ldloc.s    V_13
    IL_3863:  box        [mscorlib]System.Int32
    IL_3868:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_386d:  ldloc.s    V_4
    IL_386f:  ldloc.s    V_14
    IL_3871:  beq.s      IL_388e

    IL_3873:  ldc.i4.1
    IL_3874:  stloc.s    V_20
    IL_3876:  ldstr      "te064 check failed {0} {1}"
    IL_387b:  ldloc.s    V_4
    IL_387d:  box        [mscorlib]System.Int64
    IL_3882:  ldloc.s    V_14
    IL_3884:  box        [mscorlib]System.Int64
    IL_3889:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_388e:  ldloc.s    V_5
    IL_3890:  ldloc.s    V_15
    IL_3892:  beq.s      IL_38af

    IL_3894:  ldc.i4.1
    IL_3895:  stloc.s    V_20
    IL_3897:  ldstr      "te164 check failed {0} {1}"
    IL_389c:  ldloc.s    V_5
    IL_389e:  box        [mscorlib]System.Int64
    IL_38a3:  ldloc.s    V_15
    IL_38a5:  box        [mscorlib]System.Int64
    IL_38aa:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_38af:  ldloc.s    V_6
    IL_38b1:  ldloc.s    V_16
    IL_38b3:  beq.s      IL_38d0

    IL_38b5:  ldc.i4.1
    IL_38b6:  stloc.s    V_20
    IL_38b8:  ldstr      "te264 check failed {0} {1}"
    IL_38bd:  ldloc.s    V_6
    IL_38bf:  box        [mscorlib]System.Int64
    IL_38c4:  ldloc.s    V_16
    IL_38c6:  box        [mscorlib]System.Int64
    IL_38cb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_38d0:  ldloc.s    V_7
    IL_38d2:  ldloc.s    V_17
    IL_38d4:  beq.s      IL_38f1

    IL_38d6:  ldc.i4.1
    IL_38d7:  stloc.s    V_20
    IL_38d9:  ldstr      "te364 check failed {0} {1}"
    IL_38de:  ldloc.s    V_7
    IL_38e0:  box        [mscorlib]System.Int64
    IL_38e5:  ldloc.s    V_17
    IL_38e7:  box        [mscorlib]System.Int64
    IL_38ec:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_38f1:  ldloc.s    V_8
    IL_38f3:  ldloc.s    V_18
    IL_38f5:  beq.s      IL_3912

    IL_38f7:  ldc.i4.1
    IL_38f8:  stloc.s    V_20
    IL_38fa:  ldstr      "dummy check failed {0} {1}"
    IL_38ff:  ldloc.s    V_8
    IL_3901:  box        [mscorlib]System.Int32
    IL_3906:  ldloc.s    V_18
    IL_3908:  box        [mscorlib]System.Int32
    IL_390d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3912:  ldloc.s    V_9
    IL_3914:  ldloc.s    V_19
    IL_3916:  beq.s      IL_3933

    IL_3918:  ldc.i4.1
    IL_3919:  stloc.s    V_20
    IL_391b:  ldstr      "dummy64 check failed {0} {1}"
    IL_3920:  ldloc.s    V_9
    IL_3922:  box        [mscorlib]System.Int64
    IL_3927:  ldloc.s    V_19
    IL_3929:  box        [mscorlib]System.Int64
    IL_392e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3933:  ldsfld     int32 IntrinsicTest::counter
    IL_3938:  ldsfld     int32 IntrinsicTest::id_counter
    IL_393d:  beq.s      IL_3960

    IL_393f:  ldstr      "counter mismatch {0} {1}"
    IL_3944:  ldsfld     int32 IntrinsicTest::counter
    IL_3949:  box        [mscorlib]System.Int32
    IL_394e:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3953:  box        [mscorlib]System.Int32
    IL_3958:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_395d:  ldc.i4.1
    IL_395e:  stloc.s    V_20
    IL_3960:  ldsfld     int64 IntrinsicTest::counter64
    IL_3965:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_396a:  beq.s      IL_398d

    IL_396c:  ldstr      "counter64 mismatch {0} {1}"
    IL_3971:  ldsfld     int64 IntrinsicTest::counter64
    IL_3976:  box        [mscorlib]System.Int64
    IL_397b:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3980:  box        [mscorlib]System.Int64
    IL_3985:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_398a:  ldc.i4.1
    IL_398b:  stloc.s    V_20
    IL_398d:  ldloc.s    V_21
    IL_398f:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_3994:  ldloc.s    V_21
    IL_3996:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_399b:  beq.s      IL_39c2

    IL_399d:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_39a2:  ldloc.s    V_21
    IL_39a4:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_39a9:  box        [mscorlib]System.Int32
    IL_39ae:  ldloc.s    V_21
    IL_39b0:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_39b5:  box        [mscorlib]System.Int32
    IL_39ba:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_39bf:  ldc.i4.1
    IL_39c0:  stloc.s    V_20
    IL_39c2:  ldloc.s    V_21
    IL_39c4:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_39c9:  ldloc.s    V_21
    IL_39cb:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_39d0:  beq.s      IL_39f7

    IL_39d2:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_39d7:  ldloc.s    V_21
    IL_39d9:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_39de:  box        [mscorlib]System.Int64
    IL_39e3:  ldloc.s    V_21
    IL_39e5:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_39ea:  box        [mscorlib]System.Int64
    IL_39ef:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_39f4:  ldc.i4.1
    IL_39f5:  stloc.s    V_20
    IL_39f7:  ldsfld     int64 IntrinsicTest::temp
    IL_39fc:  ldsfld     int64 IntrinsicTest::idmp
    IL_3a01:  beq.s      IL_3a24

    IL_3a03:  ldstr      "temp mismatch {0} {1}"
    IL_3a08:  ldsfld     int64 IntrinsicTest::temp
    IL_3a0d:  box        [mscorlib]System.Int64
    IL_3a12:  ldsfld     int64 IntrinsicTest::idmp
    IL_3a17:  box        [mscorlib]System.Int64
    IL_3a1c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3a21:  ldc.i4.1
    IL_3a22:  stloc.s    V_20
    IL_3a24:  ldloc.s    V_21
    IL_3a26:  ldsflda    int64 IntrinsicTest::counter64
    IL_3a2b:  ldsfld     int64 IntrinsicTest::counter64
    IL_3a30:  ldloc.s    V_21
    IL_3a32:  callvirt   instance int32 IntrinsicTest::GetValue()
    IL_3a37:  conv.i8
    IL_3a38:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_3a3d:  stfld      int64 IntrinsicTest::instanceCounter64
    IL_3a42:  ldloc.s    V_21
    IL_3a44:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3a49:  stfld      int64 IntrinsicTest::id_instanceCounter64
    IL_3a4e:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3a53:  ldloc.s    V_21
    IL_3a55:  callvirt   instance int32 IntrinsicTest::id_GetValue()
    IL_3a5a:  conv.i8
    IL_3a5b:  bne.un.s   IL_3a69

    IL_3a5d:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3a62:  stsfld     int64 IntrinsicTest::id_counter64
    IL_3a67:  br.s       IL_3a73

    IL_3a69:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3a6e:  stsfld     int64 IntrinsicTest::idjunk
    IL_3a73:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 3 2"
    IL_3a78:  call       void [System.Console]System.Console::WriteLine(string)
    IL_3a7d:  ldloc.0
    IL_3a7e:  ldloc.s    V_10
    IL_3a80:  beq.s      IL_3a9c

    IL_3a82:  ldc.i4.1
    IL_3a83:  stloc.s    V_20
    IL_3a85:  ldstr      "te0 check failed {0} {1}"
    IL_3a8a:  ldloc.0
    IL_3a8b:  box        [mscorlib]System.Int32
    IL_3a90:  ldloc.s    V_10
    IL_3a92:  box        [mscorlib]System.Int32
    IL_3a97:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3a9c:  ldloc.1
    IL_3a9d:  ldloc.s    V_11
    IL_3a9f:  beq.s      IL_3abb

    IL_3aa1:  ldc.i4.1
    IL_3aa2:  stloc.s    V_20
    IL_3aa4:  ldstr      "te1 check failed {0} {1}"
    IL_3aa9:  ldloc.1
    IL_3aaa:  box        [mscorlib]System.Int32
    IL_3aaf:  ldloc.s    V_11
    IL_3ab1:  box        [mscorlib]System.Int32
    IL_3ab6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3abb:  ldloc.2
    IL_3abc:  ldloc.s    V_12
    IL_3abe:  beq.s      IL_3ada

    IL_3ac0:  ldc.i4.1
    IL_3ac1:  stloc.s    V_20
    IL_3ac3:  ldstr      "te2 check failed {0} {1}"
    IL_3ac8:  ldloc.2
    IL_3ac9:  box        [mscorlib]System.Int32
    IL_3ace:  ldloc.s    V_12
    IL_3ad0:  box        [mscorlib]System.Int32
    IL_3ad5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3ada:  ldloc.3
    IL_3adb:  ldloc.s    V_13
    IL_3add:  beq.s      IL_3af9

    IL_3adf:  ldc.i4.1
    IL_3ae0:  stloc.s    V_20
    IL_3ae2:  ldstr      "te3 check failed {0} {1}"
    IL_3ae7:  ldloc.3
    IL_3ae8:  box        [mscorlib]System.Int32
    IL_3aed:  ldloc.s    V_13
    IL_3aef:  box        [mscorlib]System.Int32
    IL_3af4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3af9:  ldloc.s    V_4
    IL_3afb:  ldloc.s    V_14
    IL_3afd:  beq.s      IL_3b1a

    IL_3aff:  ldc.i4.1
    IL_3b00:  stloc.s    V_20
    IL_3b02:  ldstr      "te064 check failed {0} {1}"
    IL_3b07:  ldloc.s    V_4
    IL_3b09:  box        [mscorlib]System.Int64
    IL_3b0e:  ldloc.s    V_14
    IL_3b10:  box        [mscorlib]System.Int64
    IL_3b15:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3b1a:  ldloc.s    V_5
    IL_3b1c:  ldloc.s    V_15
    IL_3b1e:  beq.s      IL_3b3b

    IL_3b20:  ldc.i4.1
    IL_3b21:  stloc.s    V_20
    IL_3b23:  ldstr      "te164 check failed {0} {1}"
    IL_3b28:  ldloc.s    V_5
    IL_3b2a:  box        [mscorlib]System.Int64
    IL_3b2f:  ldloc.s    V_15
    IL_3b31:  box        [mscorlib]System.Int64
    IL_3b36:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3b3b:  ldloc.s    V_6
    IL_3b3d:  ldloc.s    V_16
    IL_3b3f:  beq.s      IL_3b5c

    IL_3b41:  ldc.i4.1
    IL_3b42:  stloc.s    V_20
    IL_3b44:  ldstr      "te264 check failed {0} {1}"
    IL_3b49:  ldloc.s    V_6
    IL_3b4b:  box        [mscorlib]System.Int64
    IL_3b50:  ldloc.s    V_16
    IL_3b52:  box        [mscorlib]System.Int64
    IL_3b57:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3b5c:  ldloc.s    V_7
    IL_3b5e:  ldloc.s    V_17
    IL_3b60:  beq.s      IL_3b7d

    IL_3b62:  ldc.i4.1
    IL_3b63:  stloc.s    V_20
    IL_3b65:  ldstr      "te364 check failed {0} {1}"
    IL_3b6a:  ldloc.s    V_7
    IL_3b6c:  box        [mscorlib]System.Int64
    IL_3b71:  ldloc.s    V_17
    IL_3b73:  box        [mscorlib]System.Int64
    IL_3b78:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3b7d:  ldloc.s    V_8
    IL_3b7f:  ldloc.s    V_18
    IL_3b81:  beq.s      IL_3b9e

    IL_3b83:  ldc.i4.1
    IL_3b84:  stloc.s    V_20
    IL_3b86:  ldstr      "dummy check failed {0} {1}"
    IL_3b8b:  ldloc.s    V_8
    IL_3b8d:  box        [mscorlib]System.Int32
    IL_3b92:  ldloc.s    V_18
    IL_3b94:  box        [mscorlib]System.Int32
    IL_3b99:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3b9e:  ldloc.s    V_9
    IL_3ba0:  ldloc.s    V_19
    IL_3ba2:  beq.s      IL_3bbf

    IL_3ba4:  ldc.i4.1
    IL_3ba5:  stloc.s    V_20
    IL_3ba7:  ldstr      "dummy64 check failed {0} {1}"
    IL_3bac:  ldloc.s    V_9
    IL_3bae:  box        [mscorlib]System.Int64
    IL_3bb3:  ldloc.s    V_19
    IL_3bb5:  box        [mscorlib]System.Int64
    IL_3bba:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3bbf:  ldsfld     int32 IntrinsicTest::counter
    IL_3bc4:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3bc9:  beq.s      IL_3bec

    IL_3bcb:  ldstr      "counter mismatch {0} {1}"
    IL_3bd0:  ldsfld     int32 IntrinsicTest::counter
    IL_3bd5:  box        [mscorlib]System.Int32
    IL_3bda:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3bdf:  box        [mscorlib]System.Int32
    IL_3be4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3be9:  ldc.i4.1
    IL_3bea:  stloc.s    V_20
    IL_3bec:  ldsfld     int64 IntrinsicTest::counter64
    IL_3bf1:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3bf6:  beq.s      IL_3c19

    IL_3bf8:  ldstr      "counter64 mismatch {0} {1}"
    IL_3bfd:  ldsfld     int64 IntrinsicTest::counter64
    IL_3c02:  box        [mscorlib]System.Int64
    IL_3c07:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3c0c:  box        [mscorlib]System.Int64
    IL_3c11:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3c16:  ldc.i4.1
    IL_3c17:  stloc.s    V_20
    IL_3c19:  ldloc.s    V_21
    IL_3c1b:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_3c20:  ldloc.s    V_21
    IL_3c22:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_3c27:  beq.s      IL_3c4e

    IL_3c29:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_3c2e:  ldloc.s    V_21
    IL_3c30:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_3c35:  box        [mscorlib]System.Int32
    IL_3c3a:  ldloc.s    V_21
    IL_3c3c:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_3c41:  box        [mscorlib]System.Int32
    IL_3c46:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3c4b:  ldc.i4.1
    IL_3c4c:  stloc.s    V_20
    IL_3c4e:  ldloc.s    V_21
    IL_3c50:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_3c55:  ldloc.s    V_21
    IL_3c57:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_3c5c:  beq.s      IL_3c83

    IL_3c5e:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_3c63:  ldloc.s    V_21
    IL_3c65:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_3c6a:  box        [mscorlib]System.Int64
    IL_3c6f:  ldloc.s    V_21
    IL_3c71:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_3c76:  box        [mscorlib]System.Int64
    IL_3c7b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3c80:  ldc.i4.1
    IL_3c81:  stloc.s    V_20
    IL_3c83:  ldsfld     int64 IntrinsicTest::temp
    IL_3c88:  ldsfld     int64 IntrinsicTest::idmp
    IL_3c8d:  beq.s      IL_3cb0

    IL_3c8f:  ldstr      "temp mismatch {0} {1}"
    IL_3c94:  ldsfld     int64 IntrinsicTest::temp
    IL_3c99:  box        [mscorlib]System.Int64
    IL_3c9e:  ldsfld     int64 IntrinsicTest::idmp
    IL_3ca3:  box        [mscorlib]System.Int64
    IL_3ca8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3cad:  ldc.i4.1
    IL_3cae:  stloc.s    V_20
    IL_3cb0:  ldsflda    int64 IntrinsicTest::counter64
    IL_3cb5:  ldsfld     int64 IntrinsicTest::counter64
    IL_3cba:  ldloc.s    V_21
    IL_3cbc:  callvirt   instance int32 IntrinsicTest::GetValue()
    IL_3cc1:  conv.i8
    IL_3cc2:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_3cc7:  stloc.s    V_4
    IL_3cc9:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3cce:  stloc.s    V_14
    IL_3cd0:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3cd5:  ldloc.s    V_21
    IL_3cd7:  callvirt   instance int32 IntrinsicTest::id_GetValue()
    IL_3cdc:  conv.i8
    IL_3cdd:  bne.un.s   IL_3ceb

    IL_3cdf:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3ce4:  stsfld     int64 IntrinsicTest::id_counter64
    IL_3ce9:  br.s       IL_3cf5

    IL_3ceb:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3cf0:  stsfld     int64 IntrinsicTest::idjunk
    IL_3cf5:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 3 3"
    IL_3cfa:  call       void [System.Console]System.Console::WriteLine(string)
    IL_3cff:  ldloc.0
    IL_3d00:  ldloc.s    V_10
    IL_3d02:  beq.s      IL_3d1e

    IL_3d04:  ldc.i4.1
    IL_3d05:  stloc.s    V_20
    IL_3d07:  ldstr      "te0 check failed {0} {1}"
    IL_3d0c:  ldloc.0
    IL_3d0d:  box        [mscorlib]System.Int32
    IL_3d12:  ldloc.s    V_10
    IL_3d14:  box        [mscorlib]System.Int32
    IL_3d19:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3d1e:  ldloc.1
    IL_3d1f:  ldloc.s    V_11
    IL_3d21:  beq.s      IL_3d3d

    IL_3d23:  ldc.i4.1
    IL_3d24:  stloc.s    V_20
    IL_3d26:  ldstr      "te1 check failed {0} {1}"
    IL_3d2b:  ldloc.1
    IL_3d2c:  box        [mscorlib]System.Int32
    IL_3d31:  ldloc.s    V_11
    IL_3d33:  box        [mscorlib]System.Int32
    IL_3d38:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3d3d:  ldloc.2
    IL_3d3e:  ldloc.s    V_12
    IL_3d40:  beq.s      IL_3d5c

    IL_3d42:  ldc.i4.1
    IL_3d43:  stloc.s    V_20
    IL_3d45:  ldstr      "te2 check failed {0} {1}"
    IL_3d4a:  ldloc.2
    IL_3d4b:  box        [mscorlib]System.Int32
    IL_3d50:  ldloc.s    V_12
    IL_3d52:  box        [mscorlib]System.Int32
    IL_3d57:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3d5c:  ldloc.3
    IL_3d5d:  ldloc.s    V_13
    IL_3d5f:  beq.s      IL_3d7b

    IL_3d61:  ldc.i4.1
    IL_3d62:  stloc.s    V_20
    IL_3d64:  ldstr      "te3 check failed {0} {1}"
    IL_3d69:  ldloc.3
    IL_3d6a:  box        [mscorlib]System.Int32
    IL_3d6f:  ldloc.s    V_13
    IL_3d71:  box        [mscorlib]System.Int32
    IL_3d76:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3d7b:  ldloc.s    V_4
    IL_3d7d:  ldloc.s    V_14
    IL_3d7f:  beq.s      IL_3d9c

    IL_3d81:  ldc.i4.1
    IL_3d82:  stloc.s    V_20
    IL_3d84:  ldstr      "te064 check failed {0} {1}"
    IL_3d89:  ldloc.s    V_4
    IL_3d8b:  box        [mscorlib]System.Int64
    IL_3d90:  ldloc.s    V_14
    IL_3d92:  box        [mscorlib]System.Int64
    IL_3d97:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3d9c:  ldloc.s    V_5
    IL_3d9e:  ldloc.s    V_15
    IL_3da0:  beq.s      IL_3dbd

    IL_3da2:  ldc.i4.1
    IL_3da3:  stloc.s    V_20
    IL_3da5:  ldstr      "te164 check failed {0} {1}"
    IL_3daa:  ldloc.s    V_5
    IL_3dac:  box        [mscorlib]System.Int64
    IL_3db1:  ldloc.s    V_15
    IL_3db3:  box        [mscorlib]System.Int64
    IL_3db8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3dbd:  ldloc.s    V_6
    IL_3dbf:  ldloc.s    V_16
    IL_3dc1:  beq.s      IL_3dde

    IL_3dc3:  ldc.i4.1
    IL_3dc4:  stloc.s    V_20
    IL_3dc6:  ldstr      "te264 check failed {0} {1}"
    IL_3dcb:  ldloc.s    V_6
    IL_3dcd:  box        [mscorlib]System.Int64
    IL_3dd2:  ldloc.s    V_16
    IL_3dd4:  box        [mscorlib]System.Int64
    IL_3dd9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3dde:  ldloc.s    V_7
    IL_3de0:  ldloc.s    V_17
    IL_3de2:  beq.s      IL_3dff

    IL_3de4:  ldc.i4.1
    IL_3de5:  stloc.s    V_20
    IL_3de7:  ldstr      "te364 check failed {0} {1}"
    IL_3dec:  ldloc.s    V_7
    IL_3dee:  box        [mscorlib]System.Int64
    IL_3df3:  ldloc.s    V_17
    IL_3df5:  box        [mscorlib]System.Int64
    IL_3dfa:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3dff:  ldloc.s    V_8
    IL_3e01:  ldloc.s    V_18
    IL_3e03:  beq.s      IL_3e20

    IL_3e05:  ldc.i4.1
    IL_3e06:  stloc.s    V_20
    IL_3e08:  ldstr      "dummy check failed {0} {1}"
    IL_3e0d:  ldloc.s    V_8
    IL_3e0f:  box        [mscorlib]System.Int32
    IL_3e14:  ldloc.s    V_18
    IL_3e16:  box        [mscorlib]System.Int32
    IL_3e1b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3e20:  ldloc.s    V_9
    IL_3e22:  ldloc.s    V_19
    IL_3e24:  beq.s      IL_3e41

    IL_3e26:  ldc.i4.1
    IL_3e27:  stloc.s    V_20
    IL_3e29:  ldstr      "dummy64 check failed {0} {1}"
    IL_3e2e:  ldloc.s    V_9
    IL_3e30:  box        [mscorlib]System.Int64
    IL_3e35:  ldloc.s    V_19
    IL_3e37:  box        [mscorlib]System.Int64
    IL_3e3c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3e41:  ldsfld     int32 IntrinsicTest::counter
    IL_3e46:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3e4b:  beq.s      IL_3e6e

    IL_3e4d:  ldstr      "counter mismatch {0} {1}"
    IL_3e52:  ldsfld     int32 IntrinsicTest::counter
    IL_3e57:  box        [mscorlib]System.Int32
    IL_3e5c:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3e61:  box        [mscorlib]System.Int32
    IL_3e66:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3e6b:  ldc.i4.1
    IL_3e6c:  stloc.s    V_20
    IL_3e6e:  ldsfld     int64 IntrinsicTest::counter64
    IL_3e73:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3e78:  beq.s      IL_3e9b

    IL_3e7a:  ldstr      "counter64 mismatch {0} {1}"
    IL_3e7f:  ldsfld     int64 IntrinsicTest::counter64
    IL_3e84:  box        [mscorlib]System.Int64
    IL_3e89:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3e8e:  box        [mscorlib]System.Int64
    IL_3e93:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3e98:  ldc.i4.1
    IL_3e99:  stloc.s    V_20
    IL_3e9b:  ldloc.s    V_21
    IL_3e9d:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_3ea2:  ldloc.s    V_21
    IL_3ea4:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_3ea9:  beq.s      IL_3ed0

    IL_3eab:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_3eb0:  ldloc.s    V_21
    IL_3eb2:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_3eb7:  box        [mscorlib]System.Int32
    IL_3ebc:  ldloc.s    V_21
    IL_3ebe:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_3ec3:  box        [mscorlib]System.Int32
    IL_3ec8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3ecd:  ldc.i4.1
    IL_3ece:  stloc.s    V_20
    IL_3ed0:  ldloc.s    V_21
    IL_3ed2:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_3ed7:  ldloc.s    V_21
    IL_3ed9:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_3ede:  beq.s      IL_3f05

    IL_3ee0:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_3ee5:  ldloc.s    V_21
    IL_3ee7:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_3eec:  box        [mscorlib]System.Int64
    IL_3ef1:  ldloc.s    V_21
    IL_3ef3:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_3ef8:  box        [mscorlib]System.Int64
    IL_3efd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3f02:  ldc.i4.1
    IL_3f03:  stloc.s    V_20
    IL_3f05:  ldsfld     int64 IntrinsicTest::temp
    IL_3f0a:  ldsfld     int64 IntrinsicTest::idmp
    IL_3f0f:  beq.s      IL_3f32

    IL_3f11:  ldstr      "temp mismatch {0} {1}"
    IL_3f16:  ldsfld     int64 IntrinsicTest::temp
    IL_3f1b:  box        [mscorlib]System.Int64
    IL_3f20:  ldsfld     int64 IntrinsicTest::idmp
    IL_3f25:  box        [mscorlib]System.Int64
    IL_3f2a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3f2f:  ldc.i4.1
    IL_3f30:  stloc.s    V_20
    IL_3f32:  ldsflda    int64 IntrinsicTest::counter64
    IL_3f37:  ldsfld     int64 IntrinsicTest::counter64
    IL_3f3c:  ldloc.s    V_21
    IL_3f3e:  callvirt   instance int32 IntrinsicTest::GetValue()
    IL_3f43:  conv.i8
    IL_3f44:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_3f49:  stsfld     int64 IntrinsicTest::counter64
    IL_3f4e:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3f53:  stsfld     int64 IntrinsicTest::id_counter64
    IL_3f58:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3f5d:  ldloc.s    V_21
    IL_3f5f:  callvirt   instance int32 IntrinsicTest::id_GetValue()
    IL_3f64:  conv.i8
    IL_3f65:  bne.un.s   IL_3f73

    IL_3f67:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3f6c:  stsfld     int64 IntrinsicTest::idjunk
    IL_3f71:  br.s       IL_3f7d

    IL_3f73:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3f78:  stsfld     int64 IntrinsicTest::idjunk
    IL_3f7d:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 3 4"
    IL_3f82:  call       void [System.Console]System.Console::WriteLine(string)
    IL_3f87:  ldloc.0
    IL_3f88:  ldloc.s    V_10
    IL_3f8a:  beq.s      IL_3fa6

    IL_3f8c:  ldc.i4.1
    IL_3f8d:  stloc.s    V_20
    IL_3f8f:  ldstr      "te0 check failed {0} {1}"
    IL_3f94:  ldloc.0
    IL_3f95:  box        [mscorlib]System.Int32
    IL_3f9a:  ldloc.s    V_10
    IL_3f9c:  box        [mscorlib]System.Int32
    IL_3fa1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3fa6:  ldloc.1
    IL_3fa7:  ldloc.s    V_11
    IL_3fa9:  beq.s      IL_3fc5

    IL_3fab:  ldc.i4.1
    IL_3fac:  stloc.s    V_20
    IL_3fae:  ldstr      "te1 check failed {0} {1}"
    IL_3fb3:  ldloc.1
    IL_3fb4:  box        [mscorlib]System.Int32
    IL_3fb9:  ldloc.s    V_11
    IL_3fbb:  box        [mscorlib]System.Int32
    IL_3fc0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3fc5:  ldloc.2
    IL_3fc6:  ldloc.s    V_12
    IL_3fc8:  beq.s      IL_3fe4

    IL_3fca:  ldc.i4.1
    IL_3fcb:  stloc.s    V_20
    IL_3fcd:  ldstr      "te2 check failed {0} {1}"
    IL_3fd2:  ldloc.2
    IL_3fd3:  box        [mscorlib]System.Int32
    IL_3fd8:  ldloc.s    V_12
    IL_3fda:  box        [mscorlib]System.Int32
    IL_3fdf:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3fe4:  ldloc.3
    IL_3fe5:  ldloc.s    V_13
    IL_3fe7:  beq.s      IL_4003

    IL_3fe9:  ldc.i4.1
    IL_3fea:  stloc.s    V_20
    IL_3fec:  ldstr      "te3 check failed {0} {1}"
    IL_3ff1:  ldloc.3
    IL_3ff2:  box        [mscorlib]System.Int32
    IL_3ff7:  ldloc.s    V_13
    IL_3ff9:  box        [mscorlib]System.Int32
    IL_3ffe:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4003:  ldloc.s    V_4
    IL_4005:  ldloc.s    V_14
    IL_4007:  beq.s      IL_4024

    IL_4009:  ldc.i4.1
    IL_400a:  stloc.s    V_20
    IL_400c:  ldstr      "te064 check failed {0} {1}"
    IL_4011:  ldloc.s    V_4
    IL_4013:  box        [mscorlib]System.Int64
    IL_4018:  ldloc.s    V_14
    IL_401a:  box        [mscorlib]System.Int64
    IL_401f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4024:  ldloc.s    V_5
    IL_4026:  ldloc.s    V_15
    IL_4028:  beq.s      IL_4045

    IL_402a:  ldc.i4.1
    IL_402b:  stloc.s    V_20
    IL_402d:  ldstr      "te164 check failed {0} {1}"
    IL_4032:  ldloc.s    V_5
    IL_4034:  box        [mscorlib]System.Int64
    IL_4039:  ldloc.s    V_15
    IL_403b:  box        [mscorlib]System.Int64
    IL_4040:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4045:  ldloc.s    V_6
    IL_4047:  ldloc.s    V_16
    IL_4049:  beq.s      IL_4066

    IL_404b:  ldc.i4.1
    IL_404c:  stloc.s    V_20
    IL_404e:  ldstr      "te264 check failed {0} {1}"
    IL_4053:  ldloc.s    V_6
    IL_4055:  box        [mscorlib]System.Int64
    IL_405a:  ldloc.s    V_16
    IL_405c:  box        [mscorlib]System.Int64
    IL_4061:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4066:  ldloc.s    V_7
    IL_4068:  ldloc.s    V_17
    IL_406a:  beq.s      IL_4087

    IL_406c:  ldc.i4.1
    IL_406d:  stloc.s    V_20
    IL_406f:  ldstr      "te364 check failed {0} {1}"
    IL_4074:  ldloc.s    V_7
    IL_4076:  box        [mscorlib]System.Int64
    IL_407b:  ldloc.s    V_17
    IL_407d:  box        [mscorlib]System.Int64
    IL_4082:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4087:  ldloc.s    V_8
    IL_4089:  ldloc.s    V_18
    IL_408b:  beq.s      IL_40a8

    IL_408d:  ldc.i4.1
    IL_408e:  stloc.s    V_20
    IL_4090:  ldstr      "dummy check failed {0} {1}"
    IL_4095:  ldloc.s    V_8
    IL_4097:  box        [mscorlib]System.Int32
    IL_409c:  ldloc.s    V_18
    IL_409e:  box        [mscorlib]System.Int32
    IL_40a3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_40a8:  ldloc.s    V_9
    IL_40aa:  ldloc.s    V_19
    IL_40ac:  beq.s      IL_40c9

    IL_40ae:  ldc.i4.1
    IL_40af:  stloc.s    V_20
    IL_40b1:  ldstr      "dummy64 check failed {0} {1}"
    IL_40b6:  ldloc.s    V_9
    IL_40b8:  box        [mscorlib]System.Int64
    IL_40bd:  ldloc.s    V_19
    IL_40bf:  box        [mscorlib]System.Int64
    IL_40c4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_40c9:  ldsfld     int32 IntrinsicTest::counter
    IL_40ce:  ldsfld     int32 IntrinsicTest::id_counter
    IL_40d3:  beq.s      IL_40f6

    IL_40d5:  ldstr      "counter mismatch {0} {1}"
    IL_40da:  ldsfld     int32 IntrinsicTest::counter
    IL_40df:  box        [mscorlib]System.Int32
    IL_40e4:  ldsfld     int32 IntrinsicTest::id_counter
    IL_40e9:  box        [mscorlib]System.Int32
    IL_40ee:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_40f3:  ldc.i4.1
    IL_40f4:  stloc.s    V_20
    IL_40f6:  ldsfld     int64 IntrinsicTest::counter64
    IL_40fb:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4100:  beq.s      IL_4123

    IL_4102:  ldstr      "counter64 mismatch {0} {1}"
    IL_4107:  ldsfld     int64 IntrinsicTest::counter64
    IL_410c:  box        [mscorlib]System.Int64
    IL_4111:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4116:  box        [mscorlib]System.Int64
    IL_411b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4120:  ldc.i4.1
    IL_4121:  stloc.s    V_20
    IL_4123:  ldloc.s    V_21
    IL_4125:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_412a:  ldloc.s    V_21
    IL_412c:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_4131:  beq.s      IL_4158

    IL_4133:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_4138:  ldloc.s    V_21
    IL_413a:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_413f:  box        [mscorlib]System.Int32
    IL_4144:  ldloc.s    V_21
    IL_4146:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_414b:  box        [mscorlib]System.Int32
    IL_4150:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4155:  ldc.i4.1
    IL_4156:  stloc.s    V_20
    IL_4158:  ldloc.s    V_21
    IL_415a:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_415f:  ldloc.s    V_21
    IL_4161:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_4166:  beq.s      IL_418d

    IL_4168:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_416d:  ldloc.s    V_21
    IL_416f:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_4174:  box        [mscorlib]System.Int64
    IL_4179:  ldloc.s    V_21
    IL_417b:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_4180:  box        [mscorlib]System.Int64
    IL_4185:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_418a:  ldc.i4.1
    IL_418b:  stloc.s    V_20
    IL_418d:  ldsfld     int64 IntrinsicTest::temp
    IL_4192:  ldsfld     int64 IntrinsicTest::idmp
    IL_4197:  beq.s      IL_41ba

    IL_4199:  ldstr      "temp mismatch {0} {1}"
    IL_419e:  ldsfld     int64 IntrinsicTest::temp
    IL_41a3:  box        [mscorlib]System.Int64
    IL_41a8:  ldsfld     int64 IntrinsicTest::idmp
    IL_41ad:  box        [mscorlib]System.Int64
    IL_41b2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_41b7:  ldc.i4.1
    IL_41b8:  stloc.s    V_20
    IL_41ba:  ldloc.s    V_21
    IL_41bc:  ldsflda    int64 IntrinsicTest::counter64
    IL_41c1:  ldsfld     int64 IntrinsicTest::counter64
    IL_41c6:  ldloc.s    V_21
    IL_41c8:  callvirt   instance int32 IntrinsicTest::GetValue()
    IL_41cd:  conv.i8
    IL_41ce:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_41d3:  stfld      int64 IntrinsicTest::instanceCounter64
    IL_41d8:  ldloc.s    V_21
    IL_41da:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_41df:  stfld      int64 IntrinsicTest::id_instanceCounter64
    IL_41e4:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_41e9:  ldloc.s    V_21
    IL_41eb:  callvirt   instance int32 IntrinsicTest::id_GetValue()
    IL_41f0:  conv.i8
    IL_41f1:  bne.un.s   IL_41ff

    IL_41f3:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_41f8:  stsfld     int64 IntrinsicTest::id_counter64
    IL_41fd:  br.s       IL_4209

    IL_41ff:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4204:  stsfld     int64 IntrinsicTest::idjunk
    IL_4209:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 3 5"
    IL_420e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_4213:  ldloc.0
    IL_4214:  ldloc.s    V_10
    IL_4216:  beq.s      IL_4232

    IL_4218:  ldc.i4.1
    IL_4219:  stloc.s    V_20
    IL_421b:  ldstr      "te0 check failed {0} {1}"
    IL_4220:  ldloc.0
    IL_4221:  box        [mscorlib]System.Int32
    IL_4226:  ldloc.s    V_10
    IL_4228:  box        [mscorlib]System.Int32
    IL_422d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4232:  ldloc.1
    IL_4233:  ldloc.s    V_11
    IL_4235:  beq.s      IL_4251

    IL_4237:  ldc.i4.1
    IL_4238:  stloc.s    V_20
    IL_423a:  ldstr      "te1 check failed {0} {1}"
    IL_423f:  ldloc.1
    IL_4240:  box        [mscorlib]System.Int32
    IL_4245:  ldloc.s    V_11
    IL_4247:  box        [mscorlib]System.Int32
    IL_424c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4251:  ldloc.2
    IL_4252:  ldloc.s    V_12
    IL_4254:  beq.s      IL_4270

    IL_4256:  ldc.i4.1
    IL_4257:  stloc.s    V_20
    IL_4259:  ldstr      "te2 check failed {0} {1}"
    IL_425e:  ldloc.2
    IL_425f:  box        [mscorlib]System.Int32
    IL_4264:  ldloc.s    V_12
    IL_4266:  box        [mscorlib]System.Int32
    IL_426b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4270:  ldloc.3
    IL_4271:  ldloc.s    V_13
    IL_4273:  beq.s      IL_428f

    IL_4275:  ldc.i4.1
    IL_4276:  stloc.s    V_20
    IL_4278:  ldstr      "te3 check failed {0} {1}"
    IL_427d:  ldloc.3
    IL_427e:  box        [mscorlib]System.Int32
    IL_4283:  ldloc.s    V_13
    IL_4285:  box        [mscorlib]System.Int32
    IL_428a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_428f:  ldloc.s    V_4
    IL_4291:  ldloc.s    V_14
    IL_4293:  beq.s      IL_42b0

    IL_4295:  ldc.i4.1
    IL_4296:  stloc.s    V_20
    IL_4298:  ldstr      "te064 check failed {0} {1}"
    IL_429d:  ldloc.s    V_4
    IL_429f:  box        [mscorlib]System.Int64
    IL_42a4:  ldloc.s    V_14
    IL_42a6:  box        [mscorlib]System.Int64
    IL_42ab:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_42b0:  ldloc.s    V_5
    IL_42b2:  ldloc.s    V_15
    IL_42b4:  beq.s      IL_42d1

    IL_42b6:  ldc.i4.1
    IL_42b7:  stloc.s    V_20
    IL_42b9:  ldstr      "te164 check failed {0} {1}"
    IL_42be:  ldloc.s    V_5
    IL_42c0:  box        [mscorlib]System.Int64
    IL_42c5:  ldloc.s    V_15
    IL_42c7:  box        [mscorlib]System.Int64
    IL_42cc:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_42d1:  ldloc.s    V_6
    IL_42d3:  ldloc.s    V_16
    IL_42d5:  beq.s      IL_42f2

    IL_42d7:  ldc.i4.1
    IL_42d8:  stloc.s    V_20
    IL_42da:  ldstr      "te264 check failed {0} {1}"
    IL_42df:  ldloc.s    V_6
    IL_42e1:  box        [mscorlib]System.Int64
    IL_42e6:  ldloc.s    V_16
    IL_42e8:  box        [mscorlib]System.Int64
    IL_42ed:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_42f2:  ldloc.s    V_7
    IL_42f4:  ldloc.s    V_17
    IL_42f6:  beq.s      IL_4313

    IL_42f8:  ldc.i4.1
    IL_42f9:  stloc.s    V_20
    IL_42fb:  ldstr      "te364 check failed {0} {1}"
    IL_4300:  ldloc.s    V_7
    IL_4302:  box        [mscorlib]System.Int64
    IL_4307:  ldloc.s    V_17
    IL_4309:  box        [mscorlib]System.Int64
    IL_430e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4313:  ldloc.s    V_8
    IL_4315:  ldloc.s    V_18
    IL_4317:  beq.s      IL_4334

    IL_4319:  ldc.i4.1
    IL_431a:  stloc.s    V_20
    IL_431c:  ldstr      "dummy check failed {0} {1}"
    IL_4321:  ldloc.s    V_8
    IL_4323:  box        [mscorlib]System.Int32
    IL_4328:  ldloc.s    V_18
    IL_432a:  box        [mscorlib]System.Int32
    IL_432f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4334:  ldloc.s    V_9
    IL_4336:  ldloc.s    V_19
    IL_4338:  beq.s      IL_4355

    IL_433a:  ldc.i4.1
    IL_433b:  stloc.s    V_20
    IL_433d:  ldstr      "dummy64 check failed {0} {1}"
    IL_4342:  ldloc.s    V_9
    IL_4344:  box        [mscorlib]System.Int64
    IL_4349:  ldloc.s    V_19
    IL_434b:  box        [mscorlib]System.Int64
    IL_4350:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4355:  ldsfld     int32 IntrinsicTest::counter
    IL_435a:  ldsfld     int32 IntrinsicTest::id_counter
    IL_435f:  beq.s      IL_4382

    IL_4361:  ldstr      "counter mismatch {0} {1}"
    IL_4366:  ldsfld     int32 IntrinsicTest::counter
    IL_436b:  box        [mscorlib]System.Int32
    IL_4370:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4375:  box        [mscorlib]System.Int32
    IL_437a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_437f:  ldc.i4.1
    IL_4380:  stloc.s    V_20
    IL_4382:  ldsfld     int64 IntrinsicTest::counter64
    IL_4387:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_438c:  beq.s      IL_43af

    IL_438e:  ldstr      "counter64 mismatch {0} {1}"
    IL_4393:  ldsfld     int64 IntrinsicTest::counter64
    IL_4398:  box        [mscorlib]System.Int64
    IL_439d:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_43a2:  box        [mscorlib]System.Int64
    IL_43a7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_43ac:  ldc.i4.1
    IL_43ad:  stloc.s    V_20
    IL_43af:  ldloc.s    V_21
    IL_43b1:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_43b6:  ldloc.s    V_21
    IL_43b8:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_43bd:  beq.s      IL_43e4

    IL_43bf:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_43c4:  ldloc.s    V_21
    IL_43c6:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_43cb:  box        [mscorlib]System.Int32
    IL_43d0:  ldloc.s    V_21
    IL_43d2:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_43d7:  box        [mscorlib]System.Int32
    IL_43dc:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_43e1:  ldc.i4.1
    IL_43e2:  stloc.s    V_20
    IL_43e4:  ldloc.s    V_21
    IL_43e6:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_43eb:  ldloc.s    V_21
    IL_43ed:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_43f2:  beq.s      IL_4419

    IL_43f4:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_43f9:  ldloc.s    V_21
    IL_43fb:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_4400:  box        [mscorlib]System.Int64
    IL_4405:  ldloc.s    V_21
    IL_4407:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_440c:  box        [mscorlib]System.Int64
    IL_4411:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4416:  ldc.i4.1
    IL_4417:  stloc.s    V_20
    IL_4419:  ldsfld     int64 IntrinsicTest::temp
    IL_441e:  ldsfld     int64 IntrinsicTest::idmp
    IL_4423:  beq.s      IL_4446

    IL_4425:  ldstr      "temp mismatch {0} {1}"
    IL_442a:  ldsfld     int64 IntrinsicTest::temp
    IL_442f:  box        [mscorlib]System.Int64
    IL_4434:  ldsfld     int64 IntrinsicTest::idmp
    IL_4439:  box        [mscorlib]System.Int64
    IL_443e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4443:  ldc.i4.1
    IL_4444:  stloc.s    V_20
    IL_4446:  ldsflda    int64 IntrinsicTest::counter64
    IL_444b:  ldsfld     int64 IntrinsicTest::counter64
    IL_4450:  ldloc.s    V_21
    IL_4452:  callvirt   instance int32 IntrinsicTest::GetValue()
    IL_4457:  conv.i8
    IL_4458:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_445d:  stloc.s    V_7
    IL_445f:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4464:  stloc.s    V_17
    IL_4466:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_446b:  ldloc.s    V_21
    IL_446d:  callvirt   instance int32 IntrinsicTest::id_GetValue()
    IL_4472:  conv.i8
    IL_4473:  bne.un.s   IL_4481

    IL_4475:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_447a:  stsfld     int64 IntrinsicTest::id_counter64
    IL_447f:  br.s       IL_448b

    IL_4481:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4486:  stsfld     int64 IntrinsicTest::idjunk
    IL_448b:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 3 6"
    IL_4490:  call       void [System.Console]System.Console::WriteLine(string)
    IL_4495:  ldloc.0
    IL_4496:  ldloc.s    V_10
    IL_4498:  beq.s      IL_44b4

    IL_449a:  ldc.i4.1
    IL_449b:  stloc.s    V_20
    IL_449d:  ldstr      "te0 check failed {0} {1}"
    IL_44a2:  ldloc.0
    IL_44a3:  box        [mscorlib]System.Int32
    IL_44a8:  ldloc.s    V_10
    IL_44aa:  box        [mscorlib]System.Int32
    IL_44af:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_44b4:  ldloc.1
    IL_44b5:  ldloc.s    V_11
    IL_44b7:  beq.s      IL_44d3

    IL_44b9:  ldc.i4.1
    IL_44ba:  stloc.s    V_20
    IL_44bc:  ldstr      "te1 check failed {0} {1}"
    IL_44c1:  ldloc.1
    IL_44c2:  box        [mscorlib]System.Int32
    IL_44c7:  ldloc.s    V_11
    IL_44c9:  box        [mscorlib]System.Int32
    IL_44ce:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_44d3:  ldloc.2
    IL_44d4:  ldloc.s    V_12
    IL_44d6:  beq.s      IL_44f2

    IL_44d8:  ldc.i4.1
    IL_44d9:  stloc.s    V_20
    IL_44db:  ldstr      "te2 check failed {0} {1}"
    IL_44e0:  ldloc.2
    IL_44e1:  box        [mscorlib]System.Int32
    IL_44e6:  ldloc.s    V_12
    IL_44e8:  box        [mscorlib]System.Int32
    IL_44ed:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_44f2:  ldloc.3
    IL_44f3:  ldloc.s    V_13
    IL_44f5:  beq.s      IL_4511

    IL_44f7:  ldc.i4.1
    IL_44f8:  stloc.s    V_20
    IL_44fa:  ldstr      "te3 check failed {0} {1}"
    IL_44ff:  ldloc.3
    IL_4500:  box        [mscorlib]System.Int32
    IL_4505:  ldloc.s    V_13
    IL_4507:  box        [mscorlib]System.Int32
    IL_450c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4511:  ldloc.s    V_4
    IL_4513:  ldloc.s    V_14
    IL_4515:  beq.s      IL_4532

    IL_4517:  ldc.i4.1
    IL_4518:  stloc.s    V_20
    IL_451a:  ldstr      "te064 check failed {0} {1}"
    IL_451f:  ldloc.s    V_4
    IL_4521:  box        [mscorlib]System.Int64
    IL_4526:  ldloc.s    V_14
    IL_4528:  box        [mscorlib]System.Int64
    IL_452d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4532:  ldloc.s    V_5
    IL_4534:  ldloc.s    V_15
    IL_4536:  beq.s      IL_4553

    IL_4538:  ldc.i4.1
    IL_4539:  stloc.s    V_20
    IL_453b:  ldstr      "te164 check failed {0} {1}"
    IL_4540:  ldloc.s    V_5
    IL_4542:  box        [mscorlib]System.Int64
    IL_4547:  ldloc.s    V_15
    IL_4549:  box        [mscorlib]System.Int64
    IL_454e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4553:  ldloc.s    V_6
    IL_4555:  ldloc.s    V_16
    IL_4557:  beq.s      IL_4574

    IL_4559:  ldc.i4.1
    IL_455a:  stloc.s    V_20
    IL_455c:  ldstr      "te264 check failed {0} {1}"
    IL_4561:  ldloc.s    V_6
    IL_4563:  box        [mscorlib]System.Int64
    IL_4568:  ldloc.s    V_16
    IL_456a:  box        [mscorlib]System.Int64
    IL_456f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4574:  ldloc.s    V_7
    IL_4576:  ldloc.s    V_17
    IL_4578:  beq.s      IL_4595

    IL_457a:  ldc.i4.1
    IL_457b:  stloc.s    V_20
    IL_457d:  ldstr      "te364 check failed {0} {1}"
    IL_4582:  ldloc.s    V_7
    IL_4584:  box        [mscorlib]System.Int64
    IL_4589:  ldloc.s    V_17
    IL_458b:  box        [mscorlib]System.Int64
    IL_4590:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4595:  ldloc.s    V_8
    IL_4597:  ldloc.s    V_18
    IL_4599:  beq.s      IL_45b6

    IL_459b:  ldc.i4.1
    IL_459c:  stloc.s    V_20
    IL_459e:  ldstr      "dummy check failed {0} {1}"
    IL_45a3:  ldloc.s    V_8
    IL_45a5:  box        [mscorlib]System.Int32
    IL_45aa:  ldloc.s    V_18
    IL_45ac:  box        [mscorlib]System.Int32
    IL_45b1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_45b6:  ldloc.s    V_9
    IL_45b8:  ldloc.s    V_19
    IL_45ba:  beq.s      IL_45d7

    IL_45bc:  ldc.i4.1
    IL_45bd:  stloc.s    V_20
    IL_45bf:  ldstr      "dummy64 check failed {0} {1}"
    IL_45c4:  ldloc.s    V_9
    IL_45c6:  box        [mscorlib]System.Int64
    IL_45cb:  ldloc.s    V_19
    IL_45cd:  box        [mscorlib]System.Int64
    IL_45d2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_45d7:  ldsfld     int32 IntrinsicTest::counter
    IL_45dc:  ldsfld     int32 IntrinsicTest::id_counter
    IL_45e1:  beq.s      IL_4604

    IL_45e3:  ldstr      "counter mismatch {0} {1}"
    IL_45e8:  ldsfld     int32 IntrinsicTest::counter
    IL_45ed:  box        [mscorlib]System.Int32
    IL_45f2:  ldsfld     int32 IntrinsicTest::id_counter
    IL_45f7:  box        [mscorlib]System.Int32
    IL_45fc:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4601:  ldc.i4.1
    IL_4602:  stloc.s    V_20
    IL_4604:  ldsfld     int64 IntrinsicTest::counter64
    IL_4609:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_460e:  beq.s      IL_4631

    IL_4610:  ldstr      "counter64 mismatch {0} {1}"
    IL_4615:  ldsfld     int64 IntrinsicTest::counter64
    IL_461a:  box        [mscorlib]System.Int64
    IL_461f:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4624:  box        [mscorlib]System.Int64
    IL_4629:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_462e:  ldc.i4.1
    IL_462f:  stloc.s    V_20
    IL_4631:  ldloc.s    V_21
    IL_4633:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_4638:  ldloc.s    V_21
    IL_463a:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_463f:  beq.s      IL_4666

    IL_4641:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_4646:  ldloc.s    V_21
    IL_4648:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_464d:  box        [mscorlib]System.Int32
    IL_4652:  ldloc.s    V_21
    IL_4654:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_4659:  box        [mscorlib]System.Int32
    IL_465e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4663:  ldc.i4.1
    IL_4664:  stloc.s    V_20
    IL_4666:  ldloc.s    V_21
    IL_4668:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_466d:  ldloc.s    V_21
    IL_466f:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_4674:  beq.s      IL_469b

    IL_4676:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_467b:  ldloc.s    V_21
    IL_467d:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_4682:  box        [mscorlib]System.Int64
    IL_4687:  ldloc.s    V_21
    IL_4689:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_468e:  box        [mscorlib]System.Int64
    IL_4693:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4698:  ldc.i4.1
    IL_4699:  stloc.s    V_20
    IL_469b:  ldsfld     int64 IntrinsicTest::temp
    IL_46a0:  ldsfld     int64 IntrinsicTest::idmp
    IL_46a5:  beq.s      IL_46c8

    IL_46a7:  ldstr      "temp mismatch {0} {1}"
    IL_46ac:  ldsfld     int64 IntrinsicTest::temp
    IL_46b1:  box        [mscorlib]System.Int64
    IL_46b6:  ldsfld     int64 IntrinsicTest::idmp
    IL_46bb:  box        [mscorlib]System.Int64
    IL_46c0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_46c5:  ldc.i4.1
    IL_46c6:  stloc.s    V_20
    IL_46c8:  ldsflda    int64 IntrinsicTest::counter64
    IL_46cd:  ldsfld     int64 IntrinsicTest::counter64
    IL_46d2:  ldc.i4     0x261a0
    IL_46d7:  conv.i8
    IL_46d8:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_46dd:  stloc.s    V_4
    IL_46df:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_46e4:  stloc.s    V_14
    IL_46e6:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_46eb:  ldc.i4     0x261a0
    IL_46f0:  conv.i8
    IL_46f1:  bne.un.s   IL_46ff

    IL_46f3:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_46f8:  stsfld     int64 IntrinsicTest::id_counter64
    IL_46fd:  br.s       IL_4709

    IL_46ff:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4704:  stsfld     int64 IntrinsicTest::idjunk
    IL_4709:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 4 0"
    IL_470e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_4713:  ldloc.0
    IL_4714:  ldloc.s    V_10
    IL_4716:  beq.s      IL_4732

    IL_4718:  ldc.i4.1
    IL_4719:  stloc.s    V_20
    IL_471b:  ldstr      "te0 check failed {0} {1}"
    IL_4720:  ldloc.0
    IL_4721:  box        [mscorlib]System.Int32
    IL_4726:  ldloc.s    V_10
    IL_4728:  box        [mscorlib]System.Int32
    IL_472d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4732:  ldloc.1
    IL_4733:  ldloc.s    V_11
    IL_4735:  beq.s      IL_4751

    IL_4737:  ldc.i4.1
    IL_4738:  stloc.s    V_20
    IL_473a:  ldstr      "te1 check failed {0} {1}"
    IL_473f:  ldloc.1
    IL_4740:  box        [mscorlib]System.Int32
    IL_4745:  ldloc.s    V_11
    IL_4747:  box        [mscorlib]System.Int32
    IL_474c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4751:  ldloc.2
    IL_4752:  ldloc.s    V_12
    IL_4754:  beq.s      IL_4770

    IL_4756:  ldc.i4.1
    IL_4757:  stloc.s    V_20
    IL_4759:  ldstr      "te2 check failed {0} {1}"
    IL_475e:  ldloc.2
    IL_475f:  box        [mscorlib]System.Int32
    IL_4764:  ldloc.s    V_12
    IL_4766:  box        [mscorlib]System.Int32
    IL_476b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4770:  ldloc.3
    IL_4771:  ldloc.s    V_13
    IL_4773:  beq.s      IL_478f

    IL_4775:  ldc.i4.1
    IL_4776:  stloc.s    V_20
    IL_4778:  ldstr      "te3 check failed {0} {1}"
    IL_477d:  ldloc.3
    IL_477e:  box        [mscorlib]System.Int32
    IL_4783:  ldloc.s    V_13
    IL_4785:  box        [mscorlib]System.Int32
    IL_478a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_478f:  ldloc.s    V_4
    IL_4791:  ldloc.s    V_14
    IL_4793:  beq.s      IL_47b0

    IL_4795:  ldc.i4.1
    IL_4796:  stloc.s    V_20
    IL_4798:  ldstr      "te064 check failed {0} {1}"
    IL_479d:  ldloc.s    V_4
    IL_479f:  box        [mscorlib]System.Int64
    IL_47a4:  ldloc.s    V_14
    IL_47a6:  box        [mscorlib]System.Int64
    IL_47ab:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_47b0:  ldloc.s    V_5
    IL_47b2:  ldloc.s    V_15
    IL_47b4:  beq.s      IL_47d1

    IL_47b6:  ldc.i4.1
    IL_47b7:  stloc.s    V_20
    IL_47b9:  ldstr      "te164 check failed {0} {1}"
    IL_47be:  ldloc.s    V_5
    IL_47c0:  box        [mscorlib]System.Int64
    IL_47c5:  ldloc.s    V_15
    IL_47c7:  box        [mscorlib]System.Int64
    IL_47cc:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_47d1:  ldloc.s    V_6
    IL_47d3:  ldloc.s    V_16
    IL_47d5:  beq.s      IL_47f2

    IL_47d7:  ldc.i4.1
    IL_47d8:  stloc.s    V_20
    IL_47da:  ldstr      "te264 check failed {0} {1}"
    IL_47df:  ldloc.s    V_6
    IL_47e1:  box        [mscorlib]System.Int64
    IL_47e6:  ldloc.s    V_16
    IL_47e8:  box        [mscorlib]System.Int64
    IL_47ed:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_47f2:  ldloc.s    V_7
    IL_47f4:  ldloc.s    V_17
    IL_47f6:  beq.s      IL_4813

    IL_47f8:  ldc.i4.1
    IL_47f9:  stloc.s    V_20
    IL_47fb:  ldstr      "te364 check failed {0} {1}"
    IL_4800:  ldloc.s    V_7
    IL_4802:  box        [mscorlib]System.Int64
    IL_4807:  ldloc.s    V_17
    IL_4809:  box        [mscorlib]System.Int64
    IL_480e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4813:  ldloc.s    V_8
    IL_4815:  ldloc.s    V_18
    IL_4817:  beq.s      IL_4834

    IL_4819:  ldc.i4.1
    IL_481a:  stloc.s    V_20
    IL_481c:  ldstr      "dummy check failed {0} {1}"
    IL_4821:  ldloc.s    V_8
    IL_4823:  box        [mscorlib]System.Int32
    IL_4828:  ldloc.s    V_18
    IL_482a:  box        [mscorlib]System.Int32
    IL_482f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4834:  ldloc.s    V_9
    IL_4836:  ldloc.s    V_19
    IL_4838:  beq.s      IL_4855

    IL_483a:  ldc.i4.1
    IL_483b:  stloc.s    V_20
    IL_483d:  ldstr      "dummy64 check failed {0} {1}"
    IL_4842:  ldloc.s    V_9
    IL_4844:  box        [mscorlib]System.Int64
    IL_4849:  ldloc.s    V_19
    IL_484b:  box        [mscorlib]System.Int64
    IL_4850:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4855:  ldsfld     int32 IntrinsicTest::counter
    IL_485a:  ldsfld     int32 IntrinsicTest::id_counter
    IL_485f:  beq.s      IL_4882

    IL_4861:  ldstr      "counter mismatch {0} {1}"
    IL_4866:  ldsfld     int32 IntrinsicTest::counter
    IL_486b:  box        [mscorlib]System.Int32
    IL_4870:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4875:  box        [mscorlib]System.Int32
    IL_487a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_487f:  ldc.i4.1
    IL_4880:  stloc.s    V_20
    IL_4882:  ldsfld     int64 IntrinsicTest::counter64
    IL_4887:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_488c:  beq.s      IL_48af

    IL_488e:  ldstr      "counter64 mismatch {0} {1}"
    IL_4893:  ldsfld     int64 IntrinsicTest::counter64
    IL_4898:  box        [mscorlib]System.Int64
    IL_489d:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_48a2:  box        [mscorlib]System.Int64
    IL_48a7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_48ac:  ldc.i4.1
    IL_48ad:  stloc.s    V_20
    IL_48af:  ldloc.s    V_21
    IL_48b1:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_48b6:  ldloc.s    V_21
    IL_48b8:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_48bd:  beq.s      IL_48e4

    IL_48bf:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_48c4:  ldloc.s    V_21
    IL_48c6:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_48cb:  box        [mscorlib]System.Int32
    IL_48d0:  ldloc.s    V_21
    IL_48d2:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_48d7:  box        [mscorlib]System.Int32
    IL_48dc:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_48e1:  ldc.i4.1
    IL_48e2:  stloc.s    V_20
    IL_48e4:  ldloc.s    V_21
    IL_48e6:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_48eb:  ldloc.s    V_21
    IL_48ed:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_48f2:  beq.s      IL_4919

    IL_48f4:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_48f9:  ldloc.s    V_21
    IL_48fb:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_4900:  box        [mscorlib]System.Int64
    IL_4905:  ldloc.s    V_21
    IL_4907:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_490c:  box        [mscorlib]System.Int64
    IL_4911:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4916:  ldc.i4.1
    IL_4917:  stloc.s    V_20
    IL_4919:  ldsfld     int64 IntrinsicTest::temp
    IL_491e:  ldsfld     int64 IntrinsicTest::idmp
    IL_4923:  beq.s      IL_4946

    IL_4925:  ldstr      "temp mismatch {0} {1}"
    IL_492a:  ldsfld     int64 IntrinsicTest::temp
    IL_492f:  box        [mscorlib]System.Int64
    IL_4934:  ldsfld     int64 IntrinsicTest::idmp
    IL_4939:  box        [mscorlib]System.Int64
    IL_493e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4943:  ldc.i4.1
    IL_4944:  stloc.s    V_20
    IL_4946:  ldsflda    int64 IntrinsicTest::counter64
    IL_494b:  ldsfld     int64 IntrinsicTest::counter64
    IL_4950:  ldc.i4     0x261a0
    IL_4955:  conv.i8
    IL_4956:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_495b:  stsfld     int64 IntrinsicTest::counter64
    IL_4960:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4965:  stsfld     int64 IntrinsicTest::id_counter64
    IL_496a:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_496f:  ldc.i4     0x261a0
    IL_4974:  conv.i8
    IL_4975:  bne.un.s   IL_4983

    IL_4977:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_497c:  stsfld     int64 IntrinsicTest::idjunk
    IL_4981:  br.s       IL_498d

    IL_4983:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4988:  stsfld     int64 IntrinsicTest::idjunk
    IL_498d:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 4 1"
    IL_4992:  call       void [System.Console]System.Console::WriteLine(string)
    IL_4997:  ldloc.0
    IL_4998:  ldloc.s    V_10
    IL_499a:  beq.s      IL_49b6

    IL_499c:  ldc.i4.1
    IL_499d:  stloc.s    V_20
    IL_499f:  ldstr      "te0 check failed {0} {1}"
    IL_49a4:  ldloc.0
    IL_49a5:  box        [mscorlib]System.Int32
    IL_49aa:  ldloc.s    V_10
    IL_49ac:  box        [mscorlib]System.Int32
    IL_49b1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_49b6:  ldloc.1
    IL_49b7:  ldloc.s    V_11
    IL_49b9:  beq.s      IL_49d5

    IL_49bb:  ldc.i4.1
    IL_49bc:  stloc.s    V_20
    IL_49be:  ldstr      "te1 check failed {0} {1}"
    IL_49c3:  ldloc.1
    IL_49c4:  box        [mscorlib]System.Int32
    IL_49c9:  ldloc.s    V_11
    IL_49cb:  box        [mscorlib]System.Int32
    IL_49d0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_49d5:  ldloc.2
    IL_49d6:  ldloc.s    V_12
    IL_49d8:  beq.s      IL_49f4

    IL_49da:  ldc.i4.1
    IL_49db:  stloc.s    V_20
    IL_49dd:  ldstr      "te2 check failed {0} {1}"
    IL_49e2:  ldloc.2
    IL_49e3:  box        [mscorlib]System.Int32
    IL_49e8:  ldloc.s    V_12
    IL_49ea:  box        [mscorlib]System.Int32
    IL_49ef:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_49f4:  ldloc.3
    IL_49f5:  ldloc.s    V_13
    IL_49f7:  beq.s      IL_4a13

    IL_49f9:  ldc.i4.1
    IL_49fa:  stloc.s    V_20
    IL_49fc:  ldstr      "te3 check failed {0} {1}"
    IL_4a01:  ldloc.3
    IL_4a02:  box        [mscorlib]System.Int32
    IL_4a07:  ldloc.s    V_13
    IL_4a09:  box        [mscorlib]System.Int32
    IL_4a0e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4a13:  ldloc.s    V_4
    IL_4a15:  ldloc.s    V_14
    IL_4a17:  beq.s      IL_4a34

    IL_4a19:  ldc.i4.1
    IL_4a1a:  stloc.s    V_20
    IL_4a1c:  ldstr      "te064 check failed {0} {1}"
    IL_4a21:  ldloc.s    V_4
    IL_4a23:  box        [mscorlib]System.Int64
    IL_4a28:  ldloc.s    V_14
    IL_4a2a:  box        [mscorlib]System.Int64
    IL_4a2f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4a34:  ldloc.s    V_5
    IL_4a36:  ldloc.s    V_15
    IL_4a38:  beq.s      IL_4a55

    IL_4a3a:  ldc.i4.1
    IL_4a3b:  stloc.s    V_20
    IL_4a3d:  ldstr      "te164 check failed {0} {1}"
    IL_4a42:  ldloc.s    V_5
    IL_4a44:  box        [mscorlib]System.Int64
    IL_4a49:  ldloc.s    V_15
    IL_4a4b:  box        [mscorlib]System.Int64
    IL_4a50:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4a55:  ldloc.s    V_6
    IL_4a57:  ldloc.s    V_16
    IL_4a59:  beq.s      IL_4a76

    IL_4a5b:  ldc.i4.1
    IL_4a5c:  stloc.s    V_20
    IL_4a5e:  ldstr      "te264 check failed {0} {1}"
    IL_4a63:  ldloc.s    V_6
    IL_4a65:  box        [mscorlib]System.Int64
    IL_4a6a:  ldloc.s    V_16
    IL_4a6c:  box        [mscorlib]System.Int64
    IL_4a71:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4a76:  ldloc.s    V_7
    IL_4a78:  ldloc.s    V_17
    IL_4a7a:  beq.s      IL_4a97

    IL_4a7c:  ldc.i4.1
    IL_4a7d:  stloc.s    V_20
    IL_4a7f:  ldstr      "te364 check failed {0} {1}"
    IL_4a84:  ldloc.s    V_7
    IL_4a86:  box        [mscorlib]System.Int64
    IL_4a8b:  ldloc.s    V_17
    IL_4a8d:  box        [mscorlib]System.Int64
    IL_4a92:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4a97:  ldloc.s    V_8
    IL_4a99:  ldloc.s    V_18
    IL_4a9b:  beq.s      IL_4ab8

    IL_4a9d:  ldc.i4.1
    IL_4a9e:  stloc.s    V_20
    IL_4aa0:  ldstr      "dummy check failed {0} {1}"
    IL_4aa5:  ldloc.s    V_8
    IL_4aa7:  box        [mscorlib]System.Int32
    IL_4aac:  ldloc.s    V_18
    IL_4aae:  box        [mscorlib]System.Int32
    IL_4ab3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4ab8:  ldloc.s    V_9
    IL_4aba:  ldloc.s    V_19
    IL_4abc:  beq.s      IL_4ad9

    IL_4abe:  ldc.i4.1
    IL_4abf:  stloc.s    V_20
    IL_4ac1:  ldstr      "dummy64 check failed {0} {1}"
    IL_4ac6:  ldloc.s    V_9
    IL_4ac8:  box        [mscorlib]System.Int64
    IL_4acd:  ldloc.s    V_19
    IL_4acf:  box        [mscorlib]System.Int64
    IL_4ad4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4ad9:  ldsfld     int32 IntrinsicTest::counter
    IL_4ade:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4ae3:  beq.s      IL_4b06

    IL_4ae5:  ldstr      "counter mismatch {0} {1}"
    IL_4aea:  ldsfld     int32 IntrinsicTest::counter
    IL_4aef:  box        [mscorlib]System.Int32
    IL_4af4:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4af9:  box        [mscorlib]System.Int32
    IL_4afe:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4b03:  ldc.i4.1
    IL_4b04:  stloc.s    V_20
    IL_4b06:  ldsfld     int64 IntrinsicTest::counter64
    IL_4b0b:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4b10:  beq.s      IL_4b33

    IL_4b12:  ldstr      "counter64 mismatch {0} {1}"
    IL_4b17:  ldsfld     int64 IntrinsicTest::counter64
    IL_4b1c:  box        [mscorlib]System.Int64
    IL_4b21:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4b26:  box        [mscorlib]System.Int64
    IL_4b2b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4b30:  ldc.i4.1
    IL_4b31:  stloc.s    V_20
    IL_4b33:  ldloc.s    V_21
    IL_4b35:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_4b3a:  ldloc.s    V_21
    IL_4b3c:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_4b41:  beq.s      IL_4b68

    IL_4b43:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_4b48:  ldloc.s    V_21
    IL_4b4a:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_4b4f:  box        [mscorlib]System.Int32
    IL_4b54:  ldloc.s    V_21
    IL_4b56:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_4b5b:  box        [mscorlib]System.Int32
    IL_4b60:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4b65:  ldc.i4.1
    IL_4b66:  stloc.s    V_20
    IL_4b68:  ldloc.s    V_21
    IL_4b6a:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_4b6f:  ldloc.s    V_21
    IL_4b71:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_4b76:  beq.s      IL_4b9d

    IL_4b78:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_4b7d:  ldloc.s    V_21
    IL_4b7f:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_4b84:  box        [mscorlib]System.Int64
    IL_4b89:  ldloc.s    V_21
    IL_4b8b:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_4b90:  box        [mscorlib]System.Int64
    IL_4b95:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4b9a:  ldc.i4.1
    IL_4b9b:  stloc.s    V_20
    IL_4b9d:  ldsfld     int64 IntrinsicTest::temp
    IL_4ba2:  ldsfld     int64 IntrinsicTest::idmp
    IL_4ba7:  beq.s      IL_4bca

    IL_4ba9:  ldstr      "temp mismatch {0} {1}"
    IL_4bae:  ldsfld     int64 IntrinsicTest::temp
    IL_4bb3:  box        [mscorlib]System.Int64
    IL_4bb8:  ldsfld     int64 IntrinsicTest::idmp
    IL_4bbd:  box        [mscorlib]System.Int64
    IL_4bc2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4bc7:  ldc.i4.1
    IL_4bc8:  stloc.s    V_20
    IL_4bca:  ldloc.s    V_21
    IL_4bcc:  ldsflda    int64 IntrinsicTest::counter64
    IL_4bd1:  ldsfld     int64 IntrinsicTest::counter64
    IL_4bd6:  ldc.i4     0x261a0
    IL_4bdb:  conv.i8
    IL_4bdc:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_4be1:  stfld      int64 IntrinsicTest::instanceCounter64
    IL_4be6:  ldloc.s    V_21
    IL_4be8:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4bed:  stfld      int64 IntrinsicTest::id_instanceCounter64
    IL_4bf2:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4bf7:  ldc.i4     0x261a0
    IL_4bfc:  conv.i8
    IL_4bfd:  bne.un.s   IL_4c0b

    IL_4bff:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4c04:  stsfld     int64 IntrinsicTest::id_counter64
    IL_4c09:  br.s       IL_4c15

    IL_4c0b:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4c10:  stsfld     int64 IntrinsicTest::idjunk
    IL_4c15:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 4 2"
    IL_4c1a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_4c1f:  ldloc.0
    IL_4c20:  ldloc.s    V_10
    IL_4c22:  beq.s      IL_4c3e

    IL_4c24:  ldc.i4.1
    IL_4c25:  stloc.s    V_20
    IL_4c27:  ldstr      "te0 check failed {0} {1}"
    IL_4c2c:  ldloc.0
    IL_4c2d:  box        [mscorlib]System.Int32
    IL_4c32:  ldloc.s    V_10
    IL_4c34:  box        [mscorlib]System.Int32
    IL_4c39:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4c3e:  ldloc.1
    IL_4c3f:  ldloc.s    V_11
    IL_4c41:  beq.s      IL_4c5d

    IL_4c43:  ldc.i4.1
    IL_4c44:  stloc.s    V_20
    IL_4c46:  ldstr      "te1 check failed {0} {1}"
    IL_4c4b:  ldloc.1
    IL_4c4c:  box        [mscorlib]System.Int32
    IL_4c51:  ldloc.s    V_11
    IL_4c53:  box        [mscorlib]System.Int32
    IL_4c58:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4c5d:  ldloc.2
    IL_4c5e:  ldloc.s    V_12
    IL_4c60:  beq.s      IL_4c7c

    IL_4c62:  ldc.i4.1
    IL_4c63:  stloc.s    V_20
    IL_4c65:  ldstr      "te2 check failed {0} {1}"
    IL_4c6a:  ldloc.2
    IL_4c6b:  box        [mscorlib]System.Int32
    IL_4c70:  ldloc.s    V_12
    IL_4c72:  box        [mscorlib]System.Int32
    IL_4c77:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4c7c:  ldloc.3
    IL_4c7d:  ldloc.s    V_13
    IL_4c7f:  beq.s      IL_4c9b

    IL_4c81:  ldc.i4.1
    IL_4c82:  stloc.s    V_20
    IL_4c84:  ldstr      "te3 check failed {0} {1}"
    IL_4c89:  ldloc.3
    IL_4c8a:  box        [mscorlib]System.Int32
    IL_4c8f:  ldloc.s    V_13
    IL_4c91:  box        [mscorlib]System.Int32
    IL_4c96:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4c9b:  ldloc.s    V_4
    IL_4c9d:  ldloc.s    V_14
    IL_4c9f:  beq.s      IL_4cbc

    IL_4ca1:  ldc.i4.1
    IL_4ca2:  stloc.s    V_20
    IL_4ca4:  ldstr      "te064 check failed {0} {1}"
    IL_4ca9:  ldloc.s    V_4
    IL_4cab:  box        [mscorlib]System.Int64
    IL_4cb0:  ldloc.s    V_14
    IL_4cb2:  box        [mscorlib]System.Int64
    IL_4cb7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4cbc:  ldloc.s    V_5
    IL_4cbe:  ldloc.s    V_15
    IL_4cc0:  beq.s      IL_4cdd

    IL_4cc2:  ldc.i4.1
    IL_4cc3:  stloc.s    V_20
    IL_4cc5:  ldstr      "te164 check failed {0} {1}"
    IL_4cca:  ldloc.s    V_5
    IL_4ccc:  box        [mscorlib]System.Int64
    IL_4cd1:  ldloc.s    V_15
    IL_4cd3:  box        [mscorlib]System.Int64
    IL_4cd8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4cdd:  ldloc.s    V_6
    IL_4cdf:  ldloc.s    V_16
    IL_4ce1:  beq.s      IL_4cfe

    IL_4ce3:  ldc.i4.1
    IL_4ce4:  stloc.s    V_20
    IL_4ce6:  ldstr      "te264 check failed {0} {1}"
    IL_4ceb:  ldloc.s    V_6
    IL_4ced:  box        [mscorlib]System.Int64
    IL_4cf2:  ldloc.s    V_16
    IL_4cf4:  box        [mscorlib]System.Int64
    IL_4cf9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4cfe:  ldloc.s    V_7
    IL_4d00:  ldloc.s    V_17
    IL_4d02:  beq.s      IL_4d1f

    IL_4d04:  ldc.i4.1
    IL_4d05:  stloc.s    V_20
    IL_4d07:  ldstr      "te364 check failed {0} {1}"
    IL_4d0c:  ldloc.s    V_7
    IL_4d0e:  box        [mscorlib]System.Int64
    IL_4d13:  ldloc.s    V_17
    IL_4d15:  box        [mscorlib]System.Int64
    IL_4d1a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4d1f:  ldloc.s    V_8
    IL_4d21:  ldloc.s    V_18
    IL_4d23:  beq.s      IL_4d40

    IL_4d25:  ldc.i4.1
    IL_4d26:  stloc.s    V_20
    IL_4d28:  ldstr      "dummy check failed {0} {1}"
    IL_4d2d:  ldloc.s    V_8
    IL_4d2f:  box        [mscorlib]System.Int32
    IL_4d34:  ldloc.s    V_18
    IL_4d36:  box        [mscorlib]System.Int32
    IL_4d3b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4d40:  ldloc.s    V_9
    IL_4d42:  ldloc.s    V_19
    IL_4d44:  beq.s      IL_4d61

    IL_4d46:  ldc.i4.1
    IL_4d47:  stloc.s    V_20
    IL_4d49:  ldstr      "dummy64 check failed {0} {1}"
    IL_4d4e:  ldloc.s    V_9
    IL_4d50:  box        [mscorlib]System.Int64
    IL_4d55:  ldloc.s    V_19
    IL_4d57:  box        [mscorlib]System.Int64
    IL_4d5c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4d61:  ldsfld     int32 IntrinsicTest::counter
    IL_4d66:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4d6b:  beq.s      IL_4d8e

    IL_4d6d:  ldstr      "counter mismatch {0} {1}"
    IL_4d72:  ldsfld     int32 IntrinsicTest::counter
    IL_4d77:  box        [mscorlib]System.Int32
    IL_4d7c:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4d81:  box        [mscorlib]System.Int32
    IL_4d86:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4d8b:  ldc.i4.1
    IL_4d8c:  stloc.s    V_20
    IL_4d8e:  ldsfld     int64 IntrinsicTest::counter64
    IL_4d93:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4d98:  beq.s      IL_4dbb

    IL_4d9a:  ldstr      "counter64 mismatch {0} {1}"
    IL_4d9f:  ldsfld     int64 IntrinsicTest::counter64
    IL_4da4:  box        [mscorlib]System.Int64
    IL_4da9:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4dae:  box        [mscorlib]System.Int64
    IL_4db3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4db8:  ldc.i4.1
    IL_4db9:  stloc.s    V_20
    IL_4dbb:  ldloc.s    V_21
    IL_4dbd:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_4dc2:  ldloc.s    V_21
    IL_4dc4:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_4dc9:  beq.s      IL_4df0

    IL_4dcb:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_4dd0:  ldloc.s    V_21
    IL_4dd2:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_4dd7:  box        [mscorlib]System.Int32
    IL_4ddc:  ldloc.s    V_21
    IL_4dde:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_4de3:  box        [mscorlib]System.Int32
    IL_4de8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4ded:  ldc.i4.1
    IL_4dee:  stloc.s    V_20
    IL_4df0:  ldloc.s    V_21
    IL_4df2:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_4df7:  ldloc.s    V_21
    IL_4df9:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_4dfe:  beq.s      IL_4e25

    IL_4e00:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_4e05:  ldloc.s    V_21
    IL_4e07:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_4e0c:  box        [mscorlib]System.Int64
    IL_4e11:  ldloc.s    V_21
    IL_4e13:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_4e18:  box        [mscorlib]System.Int64
    IL_4e1d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4e22:  ldc.i4.1
    IL_4e23:  stloc.s    V_20
    IL_4e25:  ldsfld     int64 IntrinsicTest::temp
    IL_4e2a:  ldsfld     int64 IntrinsicTest::idmp
    IL_4e2f:  beq.s      IL_4e52

    IL_4e31:  ldstr      "temp mismatch {0} {1}"
    IL_4e36:  ldsfld     int64 IntrinsicTest::temp
    IL_4e3b:  box        [mscorlib]System.Int64
    IL_4e40:  ldsfld     int64 IntrinsicTest::idmp
    IL_4e45:  box        [mscorlib]System.Int64
    IL_4e4a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4e4f:  ldc.i4.1
    IL_4e50:  stloc.s    V_20
    IL_4e52:  ldsflda    int64 IntrinsicTest::counter64
    IL_4e57:  ldsfld     int64 IntrinsicTest::counter64
    IL_4e5c:  ldc.i4     0x261a0
    IL_4e61:  conv.i8
    IL_4e62:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_4e67:  stloc.s    V_4
    IL_4e69:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4e6e:  stloc.s    V_14
    IL_4e70:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4e75:  ldc.i4     0x261a0
    IL_4e7a:  conv.i8
    IL_4e7b:  bne.un.s   IL_4e89

    IL_4e7d:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4e82:  stsfld     int64 IntrinsicTest::id_counter64
    IL_4e87:  br.s       IL_4e93

    IL_4e89:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4e8e:  stsfld     int64 IntrinsicTest::idjunk
    IL_4e93:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 4 3"
    IL_4e98:  call       void [System.Console]System.Console::WriteLine(string)
    IL_4e9d:  ldloc.0
    IL_4e9e:  ldloc.s    V_10
    IL_4ea0:  beq.s      IL_4ebc

    IL_4ea2:  ldc.i4.1
    IL_4ea3:  stloc.s    V_20
    IL_4ea5:  ldstr      "te0 check failed {0} {1}"
    IL_4eaa:  ldloc.0
    IL_4eab:  box        [mscorlib]System.Int32
    IL_4eb0:  ldloc.s    V_10
    IL_4eb2:  box        [mscorlib]System.Int32
    IL_4eb7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4ebc:  ldloc.1
    IL_4ebd:  ldloc.s    V_11
    IL_4ebf:  beq.s      IL_4edb

    IL_4ec1:  ldc.i4.1
    IL_4ec2:  stloc.s    V_20
    IL_4ec4:  ldstr      "te1 check failed {0} {1}"
    IL_4ec9:  ldloc.1
    IL_4eca:  box        [mscorlib]System.Int32
    IL_4ecf:  ldloc.s    V_11
    IL_4ed1:  box        [mscorlib]System.Int32
    IL_4ed6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4edb:  ldloc.2
    IL_4edc:  ldloc.s    V_12
    IL_4ede:  beq.s      IL_4efa

    IL_4ee0:  ldc.i4.1
    IL_4ee1:  stloc.s    V_20
    IL_4ee3:  ldstr      "te2 check failed {0} {1}"
    IL_4ee8:  ldloc.2
    IL_4ee9:  box        [mscorlib]System.Int32
    IL_4eee:  ldloc.s    V_12
    IL_4ef0:  box        [mscorlib]System.Int32
    IL_4ef5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4efa:  ldloc.3
    IL_4efb:  ldloc.s    V_13
    IL_4efd:  beq.s      IL_4f19

    IL_4eff:  ldc.i4.1
    IL_4f00:  stloc.s    V_20
    IL_4f02:  ldstr      "te3 check failed {0} {1}"
    IL_4f07:  ldloc.3
    IL_4f08:  box        [mscorlib]System.Int32
    IL_4f0d:  ldloc.s    V_13
    IL_4f0f:  box        [mscorlib]System.Int32
    IL_4f14:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4f19:  ldloc.s    V_4
    IL_4f1b:  ldloc.s    V_14
    IL_4f1d:  beq.s      IL_4f3a

    IL_4f1f:  ldc.i4.1
    IL_4f20:  stloc.s    V_20
    IL_4f22:  ldstr      "te064 check failed {0} {1}"
    IL_4f27:  ldloc.s    V_4
    IL_4f29:  box        [mscorlib]System.Int64
    IL_4f2e:  ldloc.s    V_14
    IL_4f30:  box        [mscorlib]System.Int64
    IL_4f35:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4f3a:  ldloc.s    V_5
    IL_4f3c:  ldloc.s    V_15
    IL_4f3e:  beq.s      IL_4f5b

    IL_4f40:  ldc.i4.1
    IL_4f41:  stloc.s    V_20
    IL_4f43:  ldstr      "te164 check failed {0} {1}"
    IL_4f48:  ldloc.s    V_5
    IL_4f4a:  box        [mscorlib]System.Int64
    IL_4f4f:  ldloc.s    V_15
    IL_4f51:  box        [mscorlib]System.Int64
    IL_4f56:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4f5b:  ldloc.s    V_6
    IL_4f5d:  ldloc.s    V_16
    IL_4f5f:  beq.s      IL_4f7c

    IL_4f61:  ldc.i4.1
    IL_4f62:  stloc.s    V_20
    IL_4f64:  ldstr      "te264 check failed {0} {1}"
    IL_4f69:  ldloc.s    V_6
    IL_4f6b:  box        [mscorlib]System.Int64
    IL_4f70:  ldloc.s    V_16
    IL_4f72:  box        [mscorlib]System.Int64
    IL_4f77:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4f7c:  ldloc.s    V_7
    IL_4f7e:  ldloc.s    V_17
    IL_4f80:  beq.s      IL_4f9d

    IL_4f82:  ldc.i4.1
    IL_4f83:  stloc.s    V_20
    IL_4f85:  ldstr      "te364 check failed {0} {1}"
    IL_4f8a:  ldloc.s    V_7
    IL_4f8c:  box        [mscorlib]System.Int64
    IL_4f91:  ldloc.s    V_17
    IL_4f93:  box        [mscorlib]System.Int64
    IL_4f98:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4f9d:  ldloc.s    V_8
    IL_4f9f:  ldloc.s    V_18
    IL_4fa1:  beq.s      IL_4fbe

    IL_4fa3:  ldc.i4.1
    IL_4fa4:  stloc.s    V_20
    IL_4fa6:  ldstr      "dummy check failed {0} {1}"
    IL_4fab:  ldloc.s    V_8
    IL_4fad:  box        [mscorlib]System.Int32
    IL_4fb2:  ldloc.s    V_18
    IL_4fb4:  box        [mscorlib]System.Int32
    IL_4fb9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4fbe:  ldloc.s    V_9
    IL_4fc0:  ldloc.s    V_19
    IL_4fc2:  beq.s      IL_4fdf

    IL_4fc4:  ldc.i4.1
    IL_4fc5:  stloc.s    V_20
    IL_4fc7:  ldstr      "dummy64 check failed {0} {1}"
    IL_4fcc:  ldloc.s    V_9
    IL_4fce:  box        [mscorlib]System.Int64
    IL_4fd3:  ldloc.s    V_19
    IL_4fd5:  box        [mscorlib]System.Int64
    IL_4fda:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4fdf:  ldsfld     int32 IntrinsicTest::counter
    IL_4fe4:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4fe9:  beq.s      IL_500c

    IL_4feb:  ldstr      "counter mismatch {0} {1}"
    IL_4ff0:  ldsfld     int32 IntrinsicTest::counter
    IL_4ff5:  box        [mscorlib]System.Int32
    IL_4ffa:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4fff:  box        [mscorlib]System.Int32
    IL_5004:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5009:  ldc.i4.1
    IL_500a:  stloc.s    V_20
    IL_500c:  ldsfld     int64 IntrinsicTest::counter64
    IL_5011:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5016:  beq.s      IL_5039

    IL_5018:  ldstr      "counter64 mismatch {0} {1}"
    IL_501d:  ldsfld     int64 IntrinsicTest::counter64
    IL_5022:  box        [mscorlib]System.Int64
    IL_5027:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_502c:  box        [mscorlib]System.Int64
    IL_5031:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5036:  ldc.i4.1
    IL_5037:  stloc.s    V_20
    IL_5039:  ldloc.s    V_21
    IL_503b:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_5040:  ldloc.s    V_21
    IL_5042:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_5047:  beq.s      IL_506e

    IL_5049:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_504e:  ldloc.s    V_21
    IL_5050:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_5055:  box        [mscorlib]System.Int32
    IL_505a:  ldloc.s    V_21
    IL_505c:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_5061:  box        [mscorlib]System.Int32
    IL_5066:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_506b:  ldc.i4.1
    IL_506c:  stloc.s    V_20
    IL_506e:  ldloc.s    V_21
    IL_5070:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_5075:  ldloc.s    V_21
    IL_5077:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_507c:  beq.s      IL_50a3

    IL_507e:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_5083:  ldloc.s    V_21
    IL_5085:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_508a:  box        [mscorlib]System.Int64
    IL_508f:  ldloc.s    V_21
    IL_5091:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_5096:  box        [mscorlib]System.Int64
    IL_509b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_50a0:  ldc.i4.1
    IL_50a1:  stloc.s    V_20
    IL_50a3:  ldsfld     int64 IntrinsicTest::temp
    IL_50a8:  ldsfld     int64 IntrinsicTest::idmp
    IL_50ad:  beq.s      IL_50d0

    IL_50af:  ldstr      "temp mismatch {0} {1}"
    IL_50b4:  ldsfld     int64 IntrinsicTest::temp
    IL_50b9:  box        [mscorlib]System.Int64
    IL_50be:  ldsfld     int64 IntrinsicTest::idmp
    IL_50c3:  box        [mscorlib]System.Int64
    IL_50c8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_50cd:  ldc.i4.1
    IL_50ce:  stloc.s    V_20
    IL_50d0:  ldsflda    int64 IntrinsicTest::counter64
    IL_50d5:  ldsfld     int64 IntrinsicTest::counter64
    IL_50da:  ldc.i4     0x261a0
    IL_50df:  conv.i8
    IL_50e0:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_50e5:  stsfld     int64 IntrinsicTest::counter64
    IL_50ea:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_50ef:  stsfld     int64 IntrinsicTest::id_counter64
    IL_50f4:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_50f9:  ldc.i4     0x261a0
    IL_50fe:  conv.i8
    IL_50ff:  bne.un.s   IL_510d

    IL_5101:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5106:  stsfld     int64 IntrinsicTest::idjunk
    IL_510b:  br.s       IL_5117

    IL_510d:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5112:  stsfld     int64 IntrinsicTest::idjunk
    IL_5117:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 4 4"
    IL_511c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_5121:  ldloc.0
    IL_5122:  ldloc.s    V_10
    IL_5124:  beq.s      IL_5140

    IL_5126:  ldc.i4.1
    IL_5127:  stloc.s    V_20
    IL_5129:  ldstr      "te0 check failed {0} {1}"
    IL_512e:  ldloc.0
    IL_512f:  box        [mscorlib]System.Int32
    IL_5134:  ldloc.s    V_10
    IL_5136:  box        [mscorlib]System.Int32
    IL_513b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5140:  ldloc.1
    IL_5141:  ldloc.s    V_11
    IL_5143:  beq.s      IL_515f

    IL_5145:  ldc.i4.1
    IL_5146:  stloc.s    V_20
    IL_5148:  ldstr      "te1 check failed {0} {1}"
    IL_514d:  ldloc.1
    IL_514e:  box        [mscorlib]System.Int32
    IL_5153:  ldloc.s    V_11
    IL_5155:  box        [mscorlib]System.Int32
    IL_515a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_515f:  ldloc.2
    IL_5160:  ldloc.s    V_12
    IL_5162:  beq.s      IL_517e

    IL_5164:  ldc.i4.1
    IL_5165:  stloc.s    V_20
    IL_5167:  ldstr      "te2 check failed {0} {1}"
    IL_516c:  ldloc.2
    IL_516d:  box        [mscorlib]System.Int32
    IL_5172:  ldloc.s    V_12
    IL_5174:  box        [mscorlib]System.Int32
    IL_5179:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_517e:  ldloc.3
    IL_517f:  ldloc.s    V_13
    IL_5181:  beq.s      IL_519d

    IL_5183:  ldc.i4.1
    IL_5184:  stloc.s    V_20
    IL_5186:  ldstr      "te3 check failed {0} {1}"
    IL_518b:  ldloc.3
    IL_518c:  box        [mscorlib]System.Int32
    IL_5191:  ldloc.s    V_13
    IL_5193:  box        [mscorlib]System.Int32
    IL_5198:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_519d:  ldloc.s    V_4
    IL_519f:  ldloc.s    V_14
    IL_51a1:  beq.s      IL_51be

    IL_51a3:  ldc.i4.1
    IL_51a4:  stloc.s    V_20
    IL_51a6:  ldstr      "te064 check failed {0} {1}"
    IL_51ab:  ldloc.s    V_4
    IL_51ad:  box        [mscorlib]System.Int64
    IL_51b2:  ldloc.s    V_14
    IL_51b4:  box        [mscorlib]System.Int64
    IL_51b9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_51be:  ldloc.s    V_5
    IL_51c0:  ldloc.s    V_15
    IL_51c2:  beq.s      IL_51df

    IL_51c4:  ldc.i4.1
    IL_51c5:  stloc.s    V_20
    IL_51c7:  ldstr      "te164 check failed {0} {1}"
    IL_51cc:  ldloc.s    V_5
    IL_51ce:  box        [mscorlib]System.Int64
    IL_51d3:  ldloc.s    V_15
    IL_51d5:  box        [mscorlib]System.Int64
    IL_51da:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_51df:  ldloc.s    V_6
    IL_51e1:  ldloc.s    V_16
    IL_51e3:  beq.s      IL_5200

    IL_51e5:  ldc.i4.1
    IL_51e6:  stloc.s    V_20
    IL_51e8:  ldstr      "te264 check failed {0} {1}"
    IL_51ed:  ldloc.s    V_6
    IL_51ef:  box        [mscorlib]System.Int64
    IL_51f4:  ldloc.s    V_16
    IL_51f6:  box        [mscorlib]System.Int64
    IL_51fb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5200:  ldloc.s    V_7
    IL_5202:  ldloc.s    V_17
    IL_5204:  beq.s      IL_5221

    IL_5206:  ldc.i4.1
    IL_5207:  stloc.s    V_20
    IL_5209:  ldstr      "te364 check failed {0} {1}"
    IL_520e:  ldloc.s    V_7
    IL_5210:  box        [mscorlib]System.Int64
    IL_5215:  ldloc.s    V_17
    IL_5217:  box        [mscorlib]System.Int64
    IL_521c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5221:  ldloc.s    V_8
    IL_5223:  ldloc.s    V_18
    IL_5225:  beq.s      IL_5242

    IL_5227:  ldc.i4.1
    IL_5228:  stloc.s    V_20
    IL_522a:  ldstr      "dummy check failed {0} {1}"
    IL_522f:  ldloc.s    V_8
    IL_5231:  box        [mscorlib]System.Int32
    IL_5236:  ldloc.s    V_18
    IL_5238:  box        [mscorlib]System.Int32
    IL_523d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5242:  ldloc.s    V_9
    IL_5244:  ldloc.s    V_19
    IL_5246:  beq.s      IL_5263

    IL_5248:  ldc.i4.1
    IL_5249:  stloc.s    V_20
    IL_524b:  ldstr      "dummy64 check failed {0} {1}"
    IL_5250:  ldloc.s    V_9
    IL_5252:  box        [mscorlib]System.Int64
    IL_5257:  ldloc.s    V_19
    IL_5259:  box        [mscorlib]System.Int64
    IL_525e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5263:  ldsfld     int32 IntrinsicTest::counter
    IL_5268:  ldsfld     int32 IntrinsicTest::id_counter
    IL_526d:  beq.s      IL_5290

    IL_526f:  ldstr      "counter mismatch {0} {1}"
    IL_5274:  ldsfld     int32 IntrinsicTest::counter
    IL_5279:  box        [mscorlib]System.Int32
    IL_527e:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5283:  box        [mscorlib]System.Int32
    IL_5288:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_528d:  ldc.i4.1
    IL_528e:  stloc.s    V_20
    IL_5290:  ldsfld     int64 IntrinsicTest::counter64
    IL_5295:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_529a:  beq.s      IL_52bd

    IL_529c:  ldstr      "counter64 mismatch {0} {1}"
    IL_52a1:  ldsfld     int64 IntrinsicTest::counter64
    IL_52a6:  box        [mscorlib]System.Int64
    IL_52ab:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_52b0:  box        [mscorlib]System.Int64
    IL_52b5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_52ba:  ldc.i4.1
    IL_52bb:  stloc.s    V_20
    IL_52bd:  ldloc.s    V_21
    IL_52bf:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_52c4:  ldloc.s    V_21
    IL_52c6:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_52cb:  beq.s      IL_52f2

    IL_52cd:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_52d2:  ldloc.s    V_21
    IL_52d4:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_52d9:  box        [mscorlib]System.Int32
    IL_52de:  ldloc.s    V_21
    IL_52e0:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_52e5:  box        [mscorlib]System.Int32
    IL_52ea:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_52ef:  ldc.i4.1
    IL_52f0:  stloc.s    V_20
    IL_52f2:  ldloc.s    V_21
    IL_52f4:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_52f9:  ldloc.s    V_21
    IL_52fb:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_5300:  beq.s      IL_5327

    IL_5302:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_5307:  ldloc.s    V_21
    IL_5309:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_530e:  box        [mscorlib]System.Int64
    IL_5313:  ldloc.s    V_21
    IL_5315:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_531a:  box        [mscorlib]System.Int64
    IL_531f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5324:  ldc.i4.1
    IL_5325:  stloc.s    V_20
    IL_5327:  ldsfld     int64 IntrinsicTest::temp
    IL_532c:  ldsfld     int64 IntrinsicTest::idmp
    IL_5331:  beq.s      IL_5354

    IL_5333:  ldstr      "temp mismatch {0} {1}"
    IL_5338:  ldsfld     int64 IntrinsicTest::temp
    IL_533d:  box        [mscorlib]System.Int64
    IL_5342:  ldsfld     int64 IntrinsicTest::idmp
    IL_5347:  box        [mscorlib]System.Int64
    IL_534c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5351:  ldc.i4.1
    IL_5352:  stloc.s    V_20
    IL_5354:  ldloc.s    V_21
    IL_5356:  ldsflda    int64 IntrinsicTest::counter64
    IL_535b:  ldsfld     int64 IntrinsicTest::counter64
    IL_5360:  ldc.i4     0x261a0
    IL_5365:  conv.i8
    IL_5366:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_536b:  stfld      int64 IntrinsicTest::instanceCounter64
    IL_5370:  ldloc.s    V_21
    IL_5372:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5377:  stfld      int64 IntrinsicTest::id_instanceCounter64
    IL_537c:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5381:  ldc.i4     0x261a0
    IL_5386:  conv.i8
    IL_5387:  bne.un.s   IL_5395

    IL_5389:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_538e:  stsfld     int64 IntrinsicTest::id_counter64
    IL_5393:  br.s       IL_539f

    IL_5395:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_539a:  stsfld     int64 IntrinsicTest::idjunk
    IL_539f:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 4 5"
    IL_53a4:  call       void [System.Console]System.Console::WriteLine(string)
    IL_53a9:  ldloc.0
    IL_53aa:  ldloc.s    V_10
    IL_53ac:  beq.s      IL_53c8

    IL_53ae:  ldc.i4.1
    IL_53af:  stloc.s    V_20
    IL_53b1:  ldstr      "te0 check failed {0} {1}"
    IL_53b6:  ldloc.0
    IL_53b7:  box        [mscorlib]System.Int32
    IL_53bc:  ldloc.s    V_10
    IL_53be:  box        [mscorlib]System.Int32
    IL_53c3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_53c8:  ldloc.1
    IL_53c9:  ldloc.s    V_11
    IL_53cb:  beq.s      IL_53e7

    IL_53cd:  ldc.i4.1
    IL_53ce:  stloc.s    V_20
    IL_53d0:  ldstr      "te1 check failed {0} {1}"
    IL_53d5:  ldloc.1
    IL_53d6:  box        [mscorlib]System.Int32
    IL_53db:  ldloc.s    V_11
    IL_53dd:  box        [mscorlib]System.Int32
    IL_53e2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_53e7:  ldloc.2
    IL_53e8:  ldloc.s    V_12
    IL_53ea:  beq.s      IL_5406

    IL_53ec:  ldc.i4.1
    IL_53ed:  stloc.s    V_20
    IL_53ef:  ldstr      "te2 check failed {0} {1}"
    IL_53f4:  ldloc.2
    IL_53f5:  box        [mscorlib]System.Int32
    IL_53fa:  ldloc.s    V_12
    IL_53fc:  box        [mscorlib]System.Int32
    IL_5401:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5406:  ldloc.3
    IL_5407:  ldloc.s    V_13
    IL_5409:  beq.s      IL_5425

    IL_540b:  ldc.i4.1
    IL_540c:  stloc.s    V_20
    IL_540e:  ldstr      "te3 check failed {0} {1}"
    IL_5413:  ldloc.3
    IL_5414:  box        [mscorlib]System.Int32
    IL_5419:  ldloc.s    V_13
    IL_541b:  box        [mscorlib]System.Int32
    IL_5420:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5425:  ldloc.s    V_4
    IL_5427:  ldloc.s    V_14
    IL_5429:  beq.s      IL_5446

    IL_542b:  ldc.i4.1
    IL_542c:  stloc.s    V_20
    IL_542e:  ldstr      "te064 check failed {0} {1}"
    IL_5433:  ldloc.s    V_4
    IL_5435:  box        [mscorlib]System.Int64
    IL_543a:  ldloc.s    V_14
    IL_543c:  box        [mscorlib]System.Int64
    IL_5441:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5446:  ldloc.s    V_5
    IL_5448:  ldloc.s    V_15
    IL_544a:  beq.s      IL_5467

    IL_544c:  ldc.i4.1
    IL_544d:  stloc.s    V_20
    IL_544f:  ldstr      "te164 check failed {0} {1}"
    IL_5454:  ldloc.s    V_5
    IL_5456:  box        [mscorlib]System.Int64
    IL_545b:  ldloc.s    V_15
    IL_545d:  box        [mscorlib]System.Int64
    IL_5462:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5467:  ldloc.s    V_6
    IL_5469:  ldloc.s    V_16
    IL_546b:  beq.s      IL_5488

    IL_546d:  ldc.i4.1
    IL_546e:  stloc.s    V_20
    IL_5470:  ldstr      "te264 check failed {0} {1}"
    IL_5475:  ldloc.s    V_6
    IL_5477:  box        [mscorlib]System.Int64
    IL_547c:  ldloc.s    V_16
    IL_547e:  box        [mscorlib]System.Int64
    IL_5483:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5488:  ldloc.s    V_7
    IL_548a:  ldloc.s    V_17
    IL_548c:  beq.s      IL_54a9

    IL_548e:  ldc.i4.1
    IL_548f:  stloc.s    V_20
    IL_5491:  ldstr      "te364 check failed {0} {1}"
    IL_5496:  ldloc.s    V_7
    IL_5498:  box        [mscorlib]System.Int64
    IL_549d:  ldloc.s    V_17
    IL_549f:  box        [mscorlib]System.Int64
    IL_54a4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_54a9:  ldloc.s    V_8
    IL_54ab:  ldloc.s    V_18
    IL_54ad:  beq.s      IL_54ca

    IL_54af:  ldc.i4.1
    IL_54b0:  stloc.s    V_20
    IL_54b2:  ldstr      "dummy check failed {0} {1}"
    IL_54b7:  ldloc.s    V_8
    IL_54b9:  box        [mscorlib]System.Int32
    IL_54be:  ldloc.s    V_18
    IL_54c0:  box        [mscorlib]System.Int32
    IL_54c5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_54ca:  ldloc.s    V_9
    IL_54cc:  ldloc.s    V_19
    IL_54ce:  beq.s      IL_54eb

    IL_54d0:  ldc.i4.1
    IL_54d1:  stloc.s    V_20
    IL_54d3:  ldstr      "dummy64 check failed {0} {1}"
    IL_54d8:  ldloc.s    V_9
    IL_54da:  box        [mscorlib]System.Int64
    IL_54df:  ldloc.s    V_19
    IL_54e1:  box        [mscorlib]System.Int64
    IL_54e6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_54eb:  ldsfld     int32 IntrinsicTest::counter
    IL_54f0:  ldsfld     int32 IntrinsicTest::id_counter
    IL_54f5:  beq.s      IL_5518

    IL_54f7:  ldstr      "counter mismatch {0} {1}"
    IL_54fc:  ldsfld     int32 IntrinsicTest::counter
    IL_5501:  box        [mscorlib]System.Int32
    IL_5506:  ldsfld     int32 IntrinsicTest::id_counter
    IL_550b:  box        [mscorlib]System.Int32
    IL_5510:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5515:  ldc.i4.1
    IL_5516:  stloc.s    V_20
    IL_5518:  ldsfld     int64 IntrinsicTest::counter64
    IL_551d:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5522:  beq.s      IL_5545

    IL_5524:  ldstr      "counter64 mismatch {0} {1}"
    IL_5529:  ldsfld     int64 IntrinsicTest::counter64
    IL_552e:  box        [mscorlib]System.Int64
    IL_5533:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5538:  box        [mscorlib]System.Int64
    IL_553d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5542:  ldc.i4.1
    IL_5543:  stloc.s    V_20
    IL_5545:  ldloc.s    V_21
    IL_5547:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_554c:  ldloc.s    V_21
    IL_554e:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_5553:  beq.s      IL_557a

    IL_5555:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_555a:  ldloc.s    V_21
    IL_555c:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_5561:  box        [mscorlib]System.Int32
    IL_5566:  ldloc.s    V_21
    IL_5568:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_556d:  box        [mscorlib]System.Int32
    IL_5572:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5577:  ldc.i4.1
    IL_5578:  stloc.s    V_20
    IL_557a:  ldloc.s    V_21
    IL_557c:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_5581:  ldloc.s    V_21
    IL_5583:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_5588:  beq.s      IL_55af

    IL_558a:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_558f:  ldloc.s    V_21
    IL_5591:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_5596:  box        [mscorlib]System.Int64
    IL_559b:  ldloc.s    V_21
    IL_559d:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_55a2:  box        [mscorlib]System.Int64
    IL_55a7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_55ac:  ldc.i4.1
    IL_55ad:  stloc.s    V_20
    IL_55af:  ldsfld     int64 IntrinsicTest::temp
    IL_55b4:  ldsfld     int64 IntrinsicTest::idmp
    IL_55b9:  beq.s      IL_55dc

    IL_55bb:  ldstr      "temp mismatch {0} {1}"
    IL_55c0:  ldsfld     int64 IntrinsicTest::temp
    IL_55c5:  box        [mscorlib]System.Int64
    IL_55ca:  ldsfld     int64 IntrinsicTest::idmp
    IL_55cf:  box        [mscorlib]System.Int64
    IL_55d4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_55d9:  ldc.i4.1
    IL_55da:  stloc.s    V_20
    IL_55dc:  ldsflda    int64 IntrinsicTest::counter64
    IL_55e1:  ldsfld     int64 IntrinsicTest::counter64
    IL_55e6:  ldc.i4     0x261a0
    IL_55eb:  conv.i8
    IL_55ec:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_55f1:  stloc.s    V_7
    IL_55f3:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_55f8:  stloc.s    V_17
    IL_55fa:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_55ff:  ldc.i4     0x261a0
    IL_5604:  conv.i8
    IL_5605:  bne.un.s   IL_5613

    IL_5607:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_560c:  stsfld     int64 IntrinsicTest::id_counter64
    IL_5611:  br.s       IL_561d

    IL_5613:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5618:  stsfld     int64 IntrinsicTest::idjunk
    IL_561d:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 4 6"
    IL_5622:  call       void [System.Console]System.Console::WriteLine(string)
    IL_5627:  ldloc.0
    IL_5628:  ldloc.s    V_10
    IL_562a:  beq.s      IL_5646

    IL_562c:  ldc.i4.1
    IL_562d:  stloc.s    V_20
    IL_562f:  ldstr      "te0 check failed {0} {1}"
    IL_5634:  ldloc.0
    IL_5635:  box        [mscorlib]System.Int32
    IL_563a:  ldloc.s    V_10
    IL_563c:  box        [mscorlib]System.Int32
    IL_5641:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5646:  ldloc.1
    IL_5647:  ldloc.s    V_11
    IL_5649:  beq.s      IL_5665

    IL_564b:  ldc.i4.1
    IL_564c:  stloc.s    V_20
    IL_564e:  ldstr      "te1 check failed {0} {1}"
    IL_5653:  ldloc.1
    IL_5654:  box        [mscorlib]System.Int32
    IL_5659:  ldloc.s    V_11
    IL_565b:  box        [mscorlib]System.Int32
    IL_5660:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5665:  ldloc.2
    IL_5666:  ldloc.s    V_12
    IL_5668:  beq.s      IL_5684

    IL_566a:  ldc.i4.1
    IL_566b:  stloc.s    V_20
    IL_566d:  ldstr      "te2 check failed {0} {1}"
    IL_5672:  ldloc.2
    IL_5673:  box        [mscorlib]System.Int32
    IL_5678:  ldloc.s    V_12
    IL_567a:  box        [mscorlib]System.Int32
    IL_567f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5684:  ldloc.3
    IL_5685:  ldloc.s    V_13
    IL_5687:  beq.s      IL_56a3

    IL_5689:  ldc.i4.1
    IL_568a:  stloc.s    V_20
    IL_568c:  ldstr      "te3 check failed {0} {1}"
    IL_5691:  ldloc.3
    IL_5692:  box        [mscorlib]System.Int32
    IL_5697:  ldloc.s    V_13
    IL_5699:  box        [mscorlib]System.Int32
    IL_569e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_56a3:  ldloc.s    V_4
    IL_56a5:  ldloc.s    V_14
    IL_56a7:  beq.s      IL_56c4

    IL_56a9:  ldc.i4.1
    IL_56aa:  stloc.s    V_20
    IL_56ac:  ldstr      "te064 check failed {0} {1}"
    IL_56b1:  ldloc.s    V_4
    IL_56b3:  box        [mscorlib]System.Int64
    IL_56b8:  ldloc.s    V_14
    IL_56ba:  box        [mscorlib]System.Int64
    IL_56bf:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_56c4:  ldloc.s    V_5
    IL_56c6:  ldloc.s    V_15
    IL_56c8:  beq.s      IL_56e5

    IL_56ca:  ldc.i4.1
    IL_56cb:  stloc.s    V_20
    IL_56cd:  ldstr      "te164 check failed {0} {1}"
    IL_56d2:  ldloc.s    V_5
    IL_56d4:  box        [mscorlib]System.Int64
    IL_56d9:  ldloc.s    V_15
    IL_56db:  box        [mscorlib]System.Int64
    IL_56e0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_56e5:  ldloc.s    V_6
    IL_56e7:  ldloc.s    V_16
    IL_56e9:  beq.s      IL_5706

    IL_56eb:  ldc.i4.1
    IL_56ec:  stloc.s    V_20
    IL_56ee:  ldstr      "te264 check failed {0} {1}"
    IL_56f3:  ldloc.s    V_6
    IL_56f5:  box        [mscorlib]System.Int64
    IL_56fa:  ldloc.s    V_16
    IL_56fc:  box        [mscorlib]System.Int64
    IL_5701:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5706:  ldloc.s    V_7
    IL_5708:  ldloc.s    V_17
    IL_570a:  beq.s      IL_5727

    IL_570c:  ldc.i4.1
    IL_570d:  stloc.s    V_20
    IL_570f:  ldstr      "te364 check failed {0} {1}"
    IL_5714:  ldloc.s    V_7
    IL_5716:  box        [mscorlib]System.Int64
    IL_571b:  ldloc.s    V_17
    IL_571d:  box        [mscorlib]System.Int64
    IL_5722:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5727:  ldloc.s    V_8
    IL_5729:  ldloc.s    V_18
    IL_572b:  beq.s      IL_5748

    IL_572d:  ldc.i4.1
    IL_572e:  stloc.s    V_20
    IL_5730:  ldstr      "dummy check failed {0} {1}"
    IL_5735:  ldloc.s    V_8
    IL_5737:  box        [mscorlib]System.Int32
    IL_573c:  ldloc.s    V_18
    IL_573e:  box        [mscorlib]System.Int32
    IL_5743:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5748:  ldloc.s    V_9
    IL_574a:  ldloc.s    V_19
    IL_574c:  beq.s      IL_5769

    IL_574e:  ldc.i4.1
    IL_574f:  stloc.s    V_20
    IL_5751:  ldstr      "dummy64 check failed {0} {1}"
    IL_5756:  ldloc.s    V_9
    IL_5758:  box        [mscorlib]System.Int64
    IL_575d:  ldloc.s    V_19
    IL_575f:  box        [mscorlib]System.Int64
    IL_5764:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5769:  ldsfld     int32 IntrinsicTest::counter
    IL_576e:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5773:  beq.s      IL_5796

    IL_5775:  ldstr      "counter mismatch {0} {1}"
    IL_577a:  ldsfld     int32 IntrinsicTest::counter
    IL_577f:  box        [mscorlib]System.Int32
    IL_5784:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5789:  box        [mscorlib]System.Int32
    IL_578e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5793:  ldc.i4.1
    IL_5794:  stloc.s    V_20
    IL_5796:  ldsfld     int64 IntrinsicTest::counter64
    IL_579b:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_57a0:  beq.s      IL_57c3

    IL_57a2:  ldstr      "counter64 mismatch {0} {1}"
    IL_57a7:  ldsfld     int64 IntrinsicTest::counter64
    IL_57ac:  box        [mscorlib]System.Int64
    IL_57b1:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_57b6:  box        [mscorlib]System.Int64
    IL_57bb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_57c0:  ldc.i4.1
    IL_57c1:  stloc.s    V_20
    IL_57c3:  ldloc.s    V_21
    IL_57c5:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_57ca:  ldloc.s    V_21
    IL_57cc:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_57d1:  beq.s      IL_57f8

    IL_57d3:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_57d8:  ldloc.s    V_21
    IL_57da:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_57df:  box        [mscorlib]System.Int32
    IL_57e4:  ldloc.s    V_21
    IL_57e6:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_57eb:  box        [mscorlib]System.Int32
    IL_57f0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_57f5:  ldc.i4.1
    IL_57f6:  stloc.s    V_20
    IL_57f8:  ldloc.s    V_21
    IL_57fa:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_57ff:  ldloc.s    V_21
    IL_5801:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_5806:  beq.s      IL_582d

    IL_5808:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_580d:  ldloc.s    V_21
    IL_580f:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_5814:  box        [mscorlib]System.Int64
    IL_5819:  ldloc.s    V_21
    IL_581b:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_5820:  box        [mscorlib]System.Int64
    IL_5825:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_582a:  ldc.i4.1
    IL_582b:  stloc.s    V_20
    IL_582d:  ldsfld     int64 IntrinsicTest::temp
    IL_5832:  ldsfld     int64 IntrinsicTest::idmp
    IL_5837:  beq.s      IL_585a

    IL_5839:  ldstr      "temp mismatch {0} {1}"
    IL_583e:  ldsfld     int64 IntrinsicTest::temp
    IL_5843:  box        [mscorlib]System.Int64
    IL_5848:  ldsfld     int64 IntrinsicTest::idmp
    IL_584d:  box        [mscorlib]System.Int64
    IL_5852:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5857:  ldc.i4.1
    IL_5858:  stloc.s    V_20
    IL_585a:  ldsflda    int64 IntrinsicTest::counter64
    IL_585f:  ldsfld     int64 IntrinsicTest::counter64
    IL_5864:  ldloc.s    V_5
    IL_5866:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_586b:  stloc.s    V_4
    IL_586d:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5872:  stloc.s    V_14
    IL_5874:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5879:  ldloc.s    V_15
    IL_587b:  bne.un.s   IL_5889

    IL_587d:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5882:  stsfld     int64 IntrinsicTest::id_counter64
    IL_5887:  br.s       IL_5893

    IL_5889:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_588e:  stsfld     int64 IntrinsicTest::idjunk
    IL_5893:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 5 0"
    IL_5898:  call       void [System.Console]System.Console::WriteLine(string)
    IL_589d:  ldloc.0
    IL_589e:  ldloc.s    V_10
    IL_58a0:  beq.s      IL_58bc

    IL_58a2:  ldc.i4.1
    IL_58a3:  stloc.s    V_20
    IL_58a5:  ldstr      "te0 check failed {0} {1}"
    IL_58aa:  ldloc.0
    IL_58ab:  box        [mscorlib]System.Int32
    IL_58b0:  ldloc.s    V_10
    IL_58b2:  box        [mscorlib]System.Int32
    IL_58b7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_58bc:  ldloc.1
    IL_58bd:  ldloc.s    V_11
    IL_58bf:  beq.s      IL_58db

    IL_58c1:  ldc.i4.1
    IL_58c2:  stloc.s    V_20
    IL_58c4:  ldstr      "te1 check failed {0} {1}"
    IL_58c9:  ldloc.1
    IL_58ca:  box        [mscorlib]System.Int32
    IL_58cf:  ldloc.s    V_11
    IL_58d1:  box        [mscorlib]System.Int32
    IL_58d6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_58db:  ldloc.2
    IL_58dc:  ldloc.s    V_12
    IL_58de:  beq.s      IL_58fa

    IL_58e0:  ldc.i4.1
    IL_58e1:  stloc.s    V_20
    IL_58e3:  ldstr      "te2 check failed {0} {1}"
    IL_58e8:  ldloc.2
    IL_58e9:  box        [mscorlib]System.Int32
    IL_58ee:  ldloc.s    V_12
    IL_58f0:  box        [mscorlib]System.Int32
    IL_58f5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_58fa:  ldloc.3
    IL_58fb:  ldloc.s    V_13
    IL_58fd:  beq.s      IL_5919

    IL_58ff:  ldc.i4.1
    IL_5900:  stloc.s    V_20
    IL_5902:  ldstr      "te3 check failed {0} {1}"
    IL_5907:  ldloc.3
    IL_5908:  box        [mscorlib]System.Int32
    IL_590d:  ldloc.s    V_13
    IL_590f:  box        [mscorlib]System.Int32
    IL_5914:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5919:  ldloc.s    V_4
    IL_591b:  ldloc.s    V_14
    IL_591d:  beq.s      IL_593a

    IL_591f:  ldc.i4.1
    IL_5920:  stloc.s    V_20
    IL_5922:  ldstr      "te064 check failed {0} {1}"
    IL_5927:  ldloc.s    V_4
    IL_5929:  box        [mscorlib]System.Int64
    IL_592e:  ldloc.s    V_14
    IL_5930:  box        [mscorlib]System.Int64
    IL_5935:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_593a:  ldloc.s    V_5
    IL_593c:  ldloc.s    V_15
    IL_593e:  beq.s      IL_595b

    IL_5940:  ldc.i4.1
    IL_5941:  stloc.s    V_20
    IL_5943:  ldstr      "te164 check failed {0} {1}"
    IL_5948:  ldloc.s    V_5
    IL_594a:  box        [mscorlib]System.Int64
    IL_594f:  ldloc.s    V_15
    IL_5951:  box        [mscorlib]System.Int64
    IL_5956:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_595b:  ldloc.s    V_6
    IL_595d:  ldloc.s    V_16
    IL_595f:  beq.s      IL_597c

    IL_5961:  ldc.i4.1
    IL_5962:  stloc.s    V_20
    IL_5964:  ldstr      "te264 check failed {0} {1}"
    IL_5969:  ldloc.s    V_6
    IL_596b:  box        [mscorlib]System.Int64
    IL_5970:  ldloc.s    V_16
    IL_5972:  box        [mscorlib]System.Int64
    IL_5977:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_597c:  ldloc.s    V_7
    IL_597e:  ldloc.s    V_17
    IL_5980:  beq.s      IL_599d

    IL_5982:  ldc.i4.1
    IL_5983:  stloc.s    V_20
    IL_5985:  ldstr      "te364 check failed {0} {1}"
    IL_598a:  ldloc.s    V_7
    IL_598c:  box        [mscorlib]System.Int64
    IL_5991:  ldloc.s    V_17
    IL_5993:  box        [mscorlib]System.Int64
    IL_5998:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_599d:  ldloc.s    V_8
    IL_599f:  ldloc.s    V_18
    IL_59a1:  beq.s      IL_59be

    IL_59a3:  ldc.i4.1
    IL_59a4:  stloc.s    V_20
    IL_59a6:  ldstr      "dummy check failed {0} {1}"
    IL_59ab:  ldloc.s    V_8
    IL_59ad:  box        [mscorlib]System.Int32
    IL_59b2:  ldloc.s    V_18
    IL_59b4:  box        [mscorlib]System.Int32
    IL_59b9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_59be:  ldloc.s    V_9
    IL_59c0:  ldloc.s    V_19
    IL_59c2:  beq.s      IL_59df

    IL_59c4:  ldc.i4.1
    IL_59c5:  stloc.s    V_20
    IL_59c7:  ldstr      "dummy64 check failed {0} {1}"
    IL_59cc:  ldloc.s    V_9
    IL_59ce:  box        [mscorlib]System.Int64
    IL_59d3:  ldloc.s    V_19
    IL_59d5:  box        [mscorlib]System.Int64
    IL_59da:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_59df:  ldsfld     int32 IntrinsicTest::counter
    IL_59e4:  ldsfld     int32 IntrinsicTest::id_counter
    IL_59e9:  beq.s      IL_5a0c

    IL_59eb:  ldstr      "counter mismatch {0} {1}"
    IL_59f0:  ldsfld     int32 IntrinsicTest::counter
    IL_59f5:  box        [mscorlib]System.Int32
    IL_59fa:  ldsfld     int32 IntrinsicTest::id_counter
    IL_59ff:  box        [mscorlib]System.Int32
    IL_5a04:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5a09:  ldc.i4.1
    IL_5a0a:  stloc.s    V_20
    IL_5a0c:  ldsfld     int64 IntrinsicTest::counter64
    IL_5a11:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5a16:  beq.s      IL_5a39

    IL_5a18:  ldstr      "counter64 mismatch {0} {1}"
    IL_5a1d:  ldsfld     int64 IntrinsicTest::counter64
    IL_5a22:  box        [mscorlib]System.Int64
    IL_5a27:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5a2c:  box        [mscorlib]System.Int64
    IL_5a31:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5a36:  ldc.i4.1
    IL_5a37:  stloc.s    V_20
    IL_5a39:  ldloc.s    V_21
    IL_5a3b:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_5a40:  ldloc.s    V_21
    IL_5a42:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_5a47:  beq.s      IL_5a6e

    IL_5a49:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_5a4e:  ldloc.s    V_21
    IL_5a50:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_5a55:  box        [mscorlib]System.Int32
    IL_5a5a:  ldloc.s    V_21
    IL_5a5c:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_5a61:  box        [mscorlib]System.Int32
    IL_5a66:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5a6b:  ldc.i4.1
    IL_5a6c:  stloc.s    V_20
    IL_5a6e:  ldloc.s    V_21
    IL_5a70:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_5a75:  ldloc.s    V_21
    IL_5a77:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_5a7c:  beq.s      IL_5aa3

    IL_5a7e:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_5a83:  ldloc.s    V_21
    IL_5a85:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_5a8a:  box        [mscorlib]System.Int64
    IL_5a8f:  ldloc.s    V_21
    IL_5a91:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_5a96:  box        [mscorlib]System.Int64
    IL_5a9b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5aa0:  ldc.i4.1
    IL_5aa1:  stloc.s    V_20
    IL_5aa3:  ldsfld     int64 IntrinsicTest::temp
    IL_5aa8:  ldsfld     int64 IntrinsicTest::idmp
    IL_5aad:  beq.s      IL_5ad0

    IL_5aaf:  ldstr      "temp mismatch {0} {1}"
    IL_5ab4:  ldsfld     int64 IntrinsicTest::temp
    IL_5ab9:  box        [mscorlib]System.Int64
    IL_5abe:  ldsfld     int64 IntrinsicTest::idmp
    IL_5ac3:  box        [mscorlib]System.Int64
    IL_5ac8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5acd:  ldc.i4.1
    IL_5ace:  stloc.s    V_20
    IL_5ad0:  ldsflda    int64 IntrinsicTest::counter64
    IL_5ad5:  ldsfld     int64 IntrinsicTest::counter64
    IL_5ada:  ldloc.s    V_5
    IL_5adc:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_5ae1:  stsfld     int64 IntrinsicTest::counter64
    IL_5ae6:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5aeb:  stsfld     int64 IntrinsicTest::id_counter64
    IL_5af0:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5af5:  ldloc.s    V_15
    IL_5af7:  bne.un.s   IL_5b05

    IL_5af9:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5afe:  stsfld     int64 IntrinsicTest::idjunk
    IL_5b03:  br.s       IL_5b0f

    IL_5b05:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5b0a:  stsfld     int64 IntrinsicTest::idjunk
    IL_5b0f:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 5 1"
    IL_5b14:  call       void [System.Console]System.Console::WriteLine(string)
    IL_5b19:  ldloc.0
    IL_5b1a:  ldloc.s    V_10
    IL_5b1c:  beq.s      IL_5b38

    IL_5b1e:  ldc.i4.1
    IL_5b1f:  stloc.s    V_20
    IL_5b21:  ldstr      "te0 check failed {0} {1}"
    IL_5b26:  ldloc.0
    IL_5b27:  box        [mscorlib]System.Int32
    IL_5b2c:  ldloc.s    V_10
    IL_5b2e:  box        [mscorlib]System.Int32
    IL_5b33:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5b38:  ldloc.1
    IL_5b39:  ldloc.s    V_11
    IL_5b3b:  beq.s      IL_5b57

    IL_5b3d:  ldc.i4.1
    IL_5b3e:  stloc.s    V_20
    IL_5b40:  ldstr      "te1 check failed {0} {1}"
    IL_5b45:  ldloc.1
    IL_5b46:  box        [mscorlib]System.Int32
    IL_5b4b:  ldloc.s    V_11
    IL_5b4d:  box        [mscorlib]System.Int32
    IL_5b52:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5b57:  ldloc.2
    IL_5b58:  ldloc.s    V_12
    IL_5b5a:  beq.s      IL_5b76

    IL_5b5c:  ldc.i4.1
    IL_5b5d:  stloc.s    V_20
    IL_5b5f:  ldstr      "te2 check failed {0} {1}"
    IL_5b64:  ldloc.2
    IL_5b65:  box        [mscorlib]System.Int32
    IL_5b6a:  ldloc.s    V_12
    IL_5b6c:  box        [mscorlib]System.Int32
    IL_5b71:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5b76:  ldloc.3
    IL_5b77:  ldloc.s    V_13
    IL_5b79:  beq.s      IL_5b95

    IL_5b7b:  ldc.i4.1
    IL_5b7c:  stloc.s    V_20
    IL_5b7e:  ldstr      "te3 check failed {0} {1}"
    IL_5b83:  ldloc.3
    IL_5b84:  box        [mscorlib]System.Int32
    IL_5b89:  ldloc.s    V_13
    IL_5b8b:  box        [mscorlib]System.Int32
    IL_5b90:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5b95:  ldloc.s    V_4
    IL_5b97:  ldloc.s    V_14
    IL_5b99:  beq.s      IL_5bb6

    IL_5b9b:  ldc.i4.1
    IL_5b9c:  stloc.s    V_20
    IL_5b9e:  ldstr      "te064 check failed {0} {1}"
    IL_5ba3:  ldloc.s    V_4
    IL_5ba5:  box        [mscorlib]System.Int64
    IL_5baa:  ldloc.s    V_14
    IL_5bac:  box        [mscorlib]System.Int64
    IL_5bb1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5bb6:  ldloc.s    V_5
    IL_5bb8:  ldloc.s    V_15
    IL_5bba:  beq.s      IL_5bd7

    IL_5bbc:  ldc.i4.1
    IL_5bbd:  stloc.s    V_20
    IL_5bbf:  ldstr      "te164 check failed {0} {1}"
    IL_5bc4:  ldloc.s    V_5
    IL_5bc6:  box        [mscorlib]System.Int64
    IL_5bcb:  ldloc.s    V_15
    IL_5bcd:  box        [mscorlib]System.Int64
    IL_5bd2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5bd7:  ldloc.s    V_6
    IL_5bd9:  ldloc.s    V_16
    IL_5bdb:  beq.s      IL_5bf8

    IL_5bdd:  ldc.i4.1
    IL_5bde:  stloc.s    V_20
    IL_5be0:  ldstr      "te264 check failed {0} {1}"
    IL_5be5:  ldloc.s    V_6
    IL_5be7:  box        [mscorlib]System.Int64
    IL_5bec:  ldloc.s    V_16
    IL_5bee:  box        [mscorlib]System.Int64
    IL_5bf3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5bf8:  ldloc.s    V_7
    IL_5bfa:  ldloc.s    V_17
    IL_5bfc:  beq.s      IL_5c19

    IL_5bfe:  ldc.i4.1
    IL_5bff:  stloc.s    V_20
    IL_5c01:  ldstr      "te364 check failed {0} {1}"
    IL_5c06:  ldloc.s    V_7
    IL_5c08:  box        [mscorlib]System.Int64
    IL_5c0d:  ldloc.s    V_17
    IL_5c0f:  box        [mscorlib]System.Int64
    IL_5c14:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5c19:  ldloc.s    V_8
    IL_5c1b:  ldloc.s    V_18
    IL_5c1d:  beq.s      IL_5c3a

    IL_5c1f:  ldc.i4.1
    IL_5c20:  stloc.s    V_20
    IL_5c22:  ldstr      "dummy check failed {0} {1}"
    IL_5c27:  ldloc.s    V_8
    IL_5c29:  box        [mscorlib]System.Int32
    IL_5c2e:  ldloc.s    V_18
    IL_5c30:  box        [mscorlib]System.Int32
    IL_5c35:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5c3a:  ldloc.s    V_9
    IL_5c3c:  ldloc.s    V_19
    IL_5c3e:  beq.s      IL_5c5b

    IL_5c40:  ldc.i4.1
    IL_5c41:  stloc.s    V_20
    IL_5c43:  ldstr      "dummy64 check failed {0} {1}"
    IL_5c48:  ldloc.s    V_9
    IL_5c4a:  box        [mscorlib]System.Int64
    IL_5c4f:  ldloc.s    V_19
    IL_5c51:  box        [mscorlib]System.Int64
    IL_5c56:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5c5b:  ldsfld     int32 IntrinsicTest::counter
    IL_5c60:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5c65:  beq.s      IL_5c88

    IL_5c67:  ldstr      "counter mismatch {0} {1}"
    IL_5c6c:  ldsfld     int32 IntrinsicTest::counter
    IL_5c71:  box        [mscorlib]System.Int32
    IL_5c76:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5c7b:  box        [mscorlib]System.Int32
    IL_5c80:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5c85:  ldc.i4.1
    IL_5c86:  stloc.s    V_20
    IL_5c88:  ldsfld     int64 IntrinsicTest::counter64
    IL_5c8d:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5c92:  beq.s      IL_5cb5

    IL_5c94:  ldstr      "counter64 mismatch {0} {1}"
    IL_5c99:  ldsfld     int64 IntrinsicTest::counter64
    IL_5c9e:  box        [mscorlib]System.Int64
    IL_5ca3:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5ca8:  box        [mscorlib]System.Int64
    IL_5cad:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5cb2:  ldc.i4.1
    IL_5cb3:  stloc.s    V_20
    IL_5cb5:  ldloc.s    V_21
    IL_5cb7:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_5cbc:  ldloc.s    V_21
    IL_5cbe:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_5cc3:  beq.s      IL_5cea

    IL_5cc5:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_5cca:  ldloc.s    V_21
    IL_5ccc:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_5cd1:  box        [mscorlib]System.Int32
    IL_5cd6:  ldloc.s    V_21
    IL_5cd8:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_5cdd:  box        [mscorlib]System.Int32
    IL_5ce2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5ce7:  ldc.i4.1
    IL_5ce8:  stloc.s    V_20
    IL_5cea:  ldloc.s    V_21
    IL_5cec:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_5cf1:  ldloc.s    V_21
    IL_5cf3:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_5cf8:  beq.s      IL_5d1f

    IL_5cfa:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_5cff:  ldloc.s    V_21
    IL_5d01:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_5d06:  box        [mscorlib]System.Int64
    IL_5d0b:  ldloc.s    V_21
    IL_5d0d:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_5d12:  box        [mscorlib]System.Int64
    IL_5d17:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5d1c:  ldc.i4.1
    IL_5d1d:  stloc.s    V_20
    IL_5d1f:  ldsfld     int64 IntrinsicTest::temp
    IL_5d24:  ldsfld     int64 IntrinsicTest::idmp
    IL_5d29:  beq.s      IL_5d4c

    IL_5d2b:  ldstr      "temp mismatch {0} {1}"
    IL_5d30:  ldsfld     int64 IntrinsicTest::temp
    IL_5d35:  box        [mscorlib]System.Int64
    IL_5d3a:  ldsfld     int64 IntrinsicTest::idmp
    IL_5d3f:  box        [mscorlib]System.Int64
    IL_5d44:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5d49:  ldc.i4.1
    IL_5d4a:  stloc.s    V_20
    IL_5d4c:  ldloc.s    V_21
    IL_5d4e:  ldsflda    int64 IntrinsicTest::counter64
    IL_5d53:  ldsfld     int64 IntrinsicTest::counter64
    IL_5d58:  ldloc.s    V_5
    IL_5d5a:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_5d5f:  stfld      int64 IntrinsicTest::instanceCounter64
    IL_5d64:  ldloc.s    V_21
    IL_5d66:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5d6b:  stfld      int64 IntrinsicTest::id_instanceCounter64
    IL_5d70:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5d75:  ldloc.s    V_15
    IL_5d77:  bne.un.s   IL_5d85

    IL_5d79:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5d7e:  stsfld     int64 IntrinsicTest::id_counter64
    IL_5d83:  br.s       IL_5d8f

    IL_5d85:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5d8a:  stsfld     int64 IntrinsicTest::idjunk
    IL_5d8f:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 5 2"
    IL_5d94:  call       void [System.Console]System.Console::WriteLine(string)
    IL_5d99:  ldloc.0
    IL_5d9a:  ldloc.s    V_10
    IL_5d9c:  beq.s      IL_5db8

    IL_5d9e:  ldc.i4.1
    IL_5d9f:  stloc.s    V_20
    IL_5da1:  ldstr      "te0 check failed {0} {1}"
    IL_5da6:  ldloc.0
    IL_5da7:  box        [mscorlib]System.Int32
    IL_5dac:  ldloc.s    V_10
    IL_5dae:  box        [mscorlib]System.Int32
    IL_5db3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5db8:  ldloc.1
    IL_5db9:  ldloc.s    V_11
    IL_5dbb:  beq.s      IL_5dd7

    IL_5dbd:  ldc.i4.1
    IL_5dbe:  stloc.s    V_20
    IL_5dc0:  ldstr      "te1 check failed {0} {1}"
    IL_5dc5:  ldloc.1
    IL_5dc6:  box        [mscorlib]System.Int32
    IL_5dcb:  ldloc.s    V_11
    IL_5dcd:  box        [mscorlib]System.Int32
    IL_5dd2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5dd7:  ldloc.2
    IL_5dd8:  ldloc.s    V_12
    IL_5dda:  beq.s      IL_5df6

    IL_5ddc:  ldc.i4.1
    IL_5ddd:  stloc.s    V_20
    IL_5ddf:  ldstr      "te2 check failed {0} {1}"
    IL_5de4:  ldloc.2
    IL_5de5:  box        [mscorlib]System.Int32
    IL_5dea:  ldloc.s    V_12
    IL_5dec:  box        [mscorlib]System.Int32
    IL_5df1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5df6:  ldloc.3
    IL_5df7:  ldloc.s    V_13
    IL_5df9:  beq.s      IL_5e15

    IL_5dfb:  ldc.i4.1
    IL_5dfc:  stloc.s    V_20
    IL_5dfe:  ldstr      "te3 check failed {0} {1}"
    IL_5e03:  ldloc.3
    IL_5e04:  box        [mscorlib]System.Int32
    IL_5e09:  ldloc.s    V_13
    IL_5e0b:  box        [mscorlib]System.Int32
    IL_5e10:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5e15:  ldloc.s    V_4
    IL_5e17:  ldloc.s    V_14
    IL_5e19:  beq.s      IL_5e36

    IL_5e1b:  ldc.i4.1
    IL_5e1c:  stloc.s    V_20
    IL_5e1e:  ldstr      "te064 check failed {0} {1}"
    IL_5e23:  ldloc.s    V_4
    IL_5e25:  box        [mscorlib]System.Int64
    IL_5e2a:  ldloc.s    V_14
    IL_5e2c:  box        [mscorlib]System.Int64
    IL_5e31:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5e36:  ldloc.s    V_5
    IL_5e38:  ldloc.s    V_15
    IL_5e3a:  beq.s      IL_5e57

    IL_5e3c:  ldc.i4.1
    IL_5e3d:  stloc.s    V_20
    IL_5e3f:  ldstr      "te164 check failed {0} {1}"
    IL_5e44:  ldloc.s    V_5
    IL_5e46:  box        [mscorlib]System.Int64
    IL_5e4b:  ldloc.s    V_15
    IL_5e4d:  box        [mscorlib]System.Int64
    IL_5e52:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5e57:  ldloc.s    V_6
    IL_5e59:  ldloc.s    V_16
    IL_5e5b:  beq.s      IL_5e78

    IL_5e5d:  ldc.i4.1
    IL_5e5e:  stloc.s    V_20
    IL_5e60:  ldstr      "te264 check failed {0} {1}"
    IL_5e65:  ldloc.s    V_6
    IL_5e67:  box        [mscorlib]System.Int64
    IL_5e6c:  ldloc.s    V_16
    IL_5e6e:  box        [mscorlib]System.Int64
    IL_5e73:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5e78:  ldloc.s    V_7
    IL_5e7a:  ldloc.s    V_17
    IL_5e7c:  beq.s      IL_5e99

    IL_5e7e:  ldc.i4.1
    IL_5e7f:  stloc.s    V_20
    IL_5e81:  ldstr      "te364 check failed {0} {1}"
    IL_5e86:  ldloc.s    V_7
    IL_5e88:  box        [mscorlib]System.Int64
    IL_5e8d:  ldloc.s    V_17
    IL_5e8f:  box        [mscorlib]System.Int64
    IL_5e94:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5e99:  ldloc.s    V_8
    IL_5e9b:  ldloc.s    V_18
    IL_5e9d:  beq.s      IL_5eba

    IL_5e9f:  ldc.i4.1
    IL_5ea0:  stloc.s    V_20
    IL_5ea2:  ldstr      "dummy check failed {0} {1}"
    IL_5ea7:  ldloc.s    V_8
    IL_5ea9:  box        [mscorlib]System.Int32
    IL_5eae:  ldloc.s    V_18
    IL_5eb0:  box        [mscorlib]System.Int32
    IL_5eb5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5eba:  ldloc.s    V_9
    IL_5ebc:  ldloc.s    V_19
    IL_5ebe:  beq.s      IL_5edb

    IL_5ec0:  ldc.i4.1
    IL_5ec1:  stloc.s    V_20
    IL_5ec3:  ldstr      "dummy64 check failed {0} {1}"
    IL_5ec8:  ldloc.s    V_9
    IL_5eca:  box        [mscorlib]System.Int64
    IL_5ecf:  ldloc.s    V_19
    IL_5ed1:  box        [mscorlib]System.Int64
    IL_5ed6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5edb:  ldsfld     int32 IntrinsicTest::counter
    IL_5ee0:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5ee5:  beq.s      IL_5f08

    IL_5ee7:  ldstr      "counter mismatch {0} {1}"
    IL_5eec:  ldsfld     int32 IntrinsicTest::counter
    IL_5ef1:  box        [mscorlib]System.Int32
    IL_5ef6:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5efb:  box        [mscorlib]System.Int32
    IL_5f00:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5f05:  ldc.i4.1
    IL_5f06:  stloc.s    V_20
    IL_5f08:  ldsfld     int64 IntrinsicTest::counter64
    IL_5f0d:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5f12:  beq.s      IL_5f35

    IL_5f14:  ldstr      "counter64 mismatch {0} {1}"
    IL_5f19:  ldsfld     int64 IntrinsicTest::counter64
    IL_5f1e:  box        [mscorlib]System.Int64
    IL_5f23:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5f28:  box        [mscorlib]System.Int64
    IL_5f2d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5f32:  ldc.i4.1
    IL_5f33:  stloc.s    V_20
    IL_5f35:  ldloc.s    V_21
    IL_5f37:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_5f3c:  ldloc.s    V_21
    IL_5f3e:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_5f43:  beq.s      IL_5f6a

    IL_5f45:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_5f4a:  ldloc.s    V_21
    IL_5f4c:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_5f51:  box        [mscorlib]System.Int32
    IL_5f56:  ldloc.s    V_21
    IL_5f58:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_5f5d:  box        [mscorlib]System.Int32
    IL_5f62:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5f67:  ldc.i4.1
    IL_5f68:  stloc.s    V_20
    IL_5f6a:  ldloc.s    V_21
    IL_5f6c:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_5f71:  ldloc.s    V_21
    IL_5f73:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_5f78:  beq.s      IL_5f9f

    IL_5f7a:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_5f7f:  ldloc.s    V_21
    IL_5f81:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_5f86:  box        [mscorlib]System.Int64
    IL_5f8b:  ldloc.s    V_21
    IL_5f8d:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_5f92:  box        [mscorlib]System.Int64
    IL_5f97:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5f9c:  ldc.i4.1
    IL_5f9d:  stloc.s    V_20
    IL_5f9f:  ldsfld     int64 IntrinsicTest::temp
    IL_5fa4:  ldsfld     int64 IntrinsicTest::idmp
    IL_5fa9:  beq.s      IL_5fcc

    IL_5fab:  ldstr      "temp mismatch {0} {1}"
    IL_5fb0:  ldsfld     int64 IntrinsicTest::temp
    IL_5fb5:  box        [mscorlib]System.Int64
    IL_5fba:  ldsfld     int64 IntrinsicTest::idmp
    IL_5fbf:  box        [mscorlib]System.Int64
    IL_5fc4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5fc9:  ldc.i4.1
    IL_5fca:  stloc.s    V_20
    IL_5fcc:  ldsflda    int64 IntrinsicTest::counter64
    IL_5fd1:  ldsfld     int64 IntrinsicTest::counter64
    IL_5fd6:  ldloc.s    V_5
    IL_5fd8:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_5fdd:  stloc.s    V_4
    IL_5fdf:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5fe4:  stloc.s    V_14
    IL_5fe6:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5feb:  ldloc.s    V_15
    IL_5fed:  bne.un.s   IL_5ffb

    IL_5fef:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5ff4:  stsfld     int64 IntrinsicTest::id_counter64
    IL_5ff9:  br.s       IL_6005

    IL_5ffb:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6000:  stsfld     int64 IntrinsicTest::idjunk
    IL_6005:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 5 3"
    IL_600a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_600f:  ldloc.0
    IL_6010:  ldloc.s    V_10
    IL_6012:  beq.s      IL_602e

    IL_6014:  ldc.i4.1
    IL_6015:  stloc.s    V_20
    IL_6017:  ldstr      "te0 check failed {0} {1}"
    IL_601c:  ldloc.0
    IL_601d:  box        [mscorlib]System.Int32
    IL_6022:  ldloc.s    V_10
    IL_6024:  box        [mscorlib]System.Int32
    IL_6029:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_602e:  ldloc.1
    IL_602f:  ldloc.s    V_11
    IL_6031:  beq.s      IL_604d

    IL_6033:  ldc.i4.1
    IL_6034:  stloc.s    V_20
    IL_6036:  ldstr      "te1 check failed {0} {1}"
    IL_603b:  ldloc.1
    IL_603c:  box        [mscorlib]System.Int32
    IL_6041:  ldloc.s    V_11
    IL_6043:  box        [mscorlib]System.Int32
    IL_6048:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_604d:  ldloc.2
    IL_604e:  ldloc.s    V_12
    IL_6050:  beq.s      IL_606c

    IL_6052:  ldc.i4.1
    IL_6053:  stloc.s    V_20
    IL_6055:  ldstr      "te2 check failed {0} {1}"
    IL_605a:  ldloc.2
    IL_605b:  box        [mscorlib]System.Int32
    IL_6060:  ldloc.s    V_12
    IL_6062:  box        [mscorlib]System.Int32
    IL_6067:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_606c:  ldloc.3
    IL_606d:  ldloc.s    V_13
    IL_606f:  beq.s      IL_608b

    IL_6071:  ldc.i4.1
    IL_6072:  stloc.s    V_20
    IL_6074:  ldstr      "te3 check failed {0} {1}"
    IL_6079:  ldloc.3
    IL_607a:  box        [mscorlib]System.Int32
    IL_607f:  ldloc.s    V_13
    IL_6081:  box        [mscorlib]System.Int32
    IL_6086:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_608b:  ldloc.s    V_4
    IL_608d:  ldloc.s    V_14
    IL_608f:  beq.s      IL_60ac

    IL_6091:  ldc.i4.1
    IL_6092:  stloc.s    V_20
    IL_6094:  ldstr      "te064 check failed {0} {1}"
    IL_6099:  ldloc.s    V_4
    IL_609b:  box        [mscorlib]System.Int64
    IL_60a0:  ldloc.s    V_14
    IL_60a2:  box        [mscorlib]System.Int64
    IL_60a7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_60ac:  ldloc.s    V_5
    IL_60ae:  ldloc.s    V_15
    IL_60b0:  beq.s      IL_60cd

    IL_60b2:  ldc.i4.1
    IL_60b3:  stloc.s    V_20
    IL_60b5:  ldstr      "te164 check failed {0} {1}"
    IL_60ba:  ldloc.s    V_5
    IL_60bc:  box        [mscorlib]System.Int64
    IL_60c1:  ldloc.s    V_15
    IL_60c3:  box        [mscorlib]System.Int64
    IL_60c8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_60cd:  ldloc.s    V_6
    IL_60cf:  ldloc.s    V_16
    IL_60d1:  beq.s      IL_60ee

    IL_60d3:  ldc.i4.1
    IL_60d4:  stloc.s    V_20
    IL_60d6:  ldstr      "te264 check failed {0} {1}"
    IL_60db:  ldloc.s    V_6
    IL_60dd:  box        [mscorlib]System.Int64
    IL_60e2:  ldloc.s    V_16
    IL_60e4:  box        [mscorlib]System.Int64
    IL_60e9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_60ee:  ldloc.s    V_7
    IL_60f0:  ldloc.s    V_17
    IL_60f2:  beq.s      IL_610f

    IL_60f4:  ldc.i4.1
    IL_60f5:  stloc.s    V_20
    IL_60f7:  ldstr      "te364 check failed {0} {1}"
    IL_60fc:  ldloc.s    V_7
    IL_60fe:  box        [mscorlib]System.Int64
    IL_6103:  ldloc.s    V_17
    IL_6105:  box        [mscorlib]System.Int64
    IL_610a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_610f:  ldloc.s    V_8
    IL_6111:  ldloc.s    V_18
    IL_6113:  beq.s      IL_6130

    IL_6115:  ldc.i4.1
    IL_6116:  stloc.s    V_20
    IL_6118:  ldstr      "dummy check failed {0} {1}"
    IL_611d:  ldloc.s    V_8
    IL_611f:  box        [mscorlib]System.Int32
    IL_6124:  ldloc.s    V_18
    IL_6126:  box        [mscorlib]System.Int32
    IL_612b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6130:  ldloc.s    V_9
    IL_6132:  ldloc.s    V_19
    IL_6134:  beq.s      IL_6151

    IL_6136:  ldc.i4.1
    IL_6137:  stloc.s    V_20
    IL_6139:  ldstr      "dummy64 check failed {0} {1}"
    IL_613e:  ldloc.s    V_9
    IL_6140:  box        [mscorlib]System.Int64
    IL_6145:  ldloc.s    V_19
    IL_6147:  box        [mscorlib]System.Int64
    IL_614c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6151:  ldsfld     int32 IntrinsicTest::counter
    IL_6156:  ldsfld     int32 IntrinsicTest::id_counter
    IL_615b:  beq.s      IL_617e

    IL_615d:  ldstr      "counter mismatch {0} {1}"
    IL_6162:  ldsfld     int32 IntrinsicTest::counter
    IL_6167:  box        [mscorlib]System.Int32
    IL_616c:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6171:  box        [mscorlib]System.Int32
    IL_6176:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_617b:  ldc.i4.1
    IL_617c:  stloc.s    V_20
    IL_617e:  ldsfld     int64 IntrinsicTest::counter64
    IL_6183:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6188:  beq.s      IL_61ab

    IL_618a:  ldstr      "counter64 mismatch {0} {1}"
    IL_618f:  ldsfld     int64 IntrinsicTest::counter64
    IL_6194:  box        [mscorlib]System.Int64
    IL_6199:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_619e:  box        [mscorlib]System.Int64
    IL_61a3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_61a8:  ldc.i4.1
    IL_61a9:  stloc.s    V_20
    IL_61ab:  ldloc.s    V_21
    IL_61ad:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_61b2:  ldloc.s    V_21
    IL_61b4:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_61b9:  beq.s      IL_61e0

    IL_61bb:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_61c0:  ldloc.s    V_21
    IL_61c2:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_61c7:  box        [mscorlib]System.Int32
    IL_61cc:  ldloc.s    V_21
    IL_61ce:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_61d3:  box        [mscorlib]System.Int32
    IL_61d8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_61dd:  ldc.i4.1
    IL_61de:  stloc.s    V_20
    IL_61e0:  ldloc.s    V_21
    IL_61e2:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_61e7:  ldloc.s    V_21
    IL_61e9:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_61ee:  beq.s      IL_6215

    IL_61f0:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_61f5:  ldloc.s    V_21
    IL_61f7:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_61fc:  box        [mscorlib]System.Int64
    IL_6201:  ldloc.s    V_21
    IL_6203:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_6208:  box        [mscorlib]System.Int64
    IL_620d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6212:  ldc.i4.1
    IL_6213:  stloc.s    V_20
    IL_6215:  ldsfld     int64 IntrinsicTest::temp
    IL_621a:  ldsfld     int64 IntrinsicTest::idmp
    IL_621f:  beq.s      IL_6242

    IL_6221:  ldstr      "temp mismatch {0} {1}"
    IL_6226:  ldsfld     int64 IntrinsicTest::temp
    IL_622b:  box        [mscorlib]System.Int64
    IL_6230:  ldsfld     int64 IntrinsicTest::idmp
    IL_6235:  box        [mscorlib]System.Int64
    IL_623a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_623f:  ldc.i4.1
    IL_6240:  stloc.s    V_20
    IL_6242:  ldsflda    int64 IntrinsicTest::counter64
    IL_6247:  ldsfld     int64 IntrinsicTest::counter64
    IL_624c:  ldloc.s    V_5
    IL_624e:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_6253:  stsfld     int64 IntrinsicTest::counter64
    IL_6258:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_625d:  stsfld     int64 IntrinsicTest::id_counter64
    IL_6262:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6267:  ldloc.s    V_15
    IL_6269:  bne.un.s   IL_6277

    IL_626b:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6270:  stsfld     int64 IntrinsicTest::idjunk
    IL_6275:  br.s       IL_6281

    IL_6277:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_627c:  stsfld     int64 IntrinsicTest::idjunk
    IL_6281:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 5 4"
    IL_6286:  call       void [System.Console]System.Console::WriteLine(string)
    IL_628b:  ldloc.0
    IL_628c:  ldloc.s    V_10
    IL_628e:  beq.s      IL_62aa

    IL_6290:  ldc.i4.1
    IL_6291:  stloc.s    V_20
    IL_6293:  ldstr      "te0 check failed {0} {1}"
    IL_6298:  ldloc.0
    IL_6299:  box        [mscorlib]System.Int32
    IL_629e:  ldloc.s    V_10
    IL_62a0:  box        [mscorlib]System.Int32
    IL_62a5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_62aa:  ldloc.1
    IL_62ab:  ldloc.s    V_11
    IL_62ad:  beq.s      IL_62c9

    IL_62af:  ldc.i4.1
    IL_62b0:  stloc.s    V_20
    IL_62b2:  ldstr      "te1 check failed {0} {1}"
    IL_62b7:  ldloc.1
    IL_62b8:  box        [mscorlib]System.Int32
    IL_62bd:  ldloc.s    V_11
    IL_62bf:  box        [mscorlib]System.Int32
    IL_62c4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_62c9:  ldloc.2
    IL_62ca:  ldloc.s    V_12
    IL_62cc:  beq.s      IL_62e8

    IL_62ce:  ldc.i4.1
    IL_62cf:  stloc.s    V_20
    IL_62d1:  ldstr      "te2 check failed {0} {1}"
    IL_62d6:  ldloc.2
    IL_62d7:  box        [mscorlib]System.Int32
    IL_62dc:  ldloc.s    V_12
    IL_62de:  box        [mscorlib]System.Int32
    IL_62e3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_62e8:  ldloc.3
    IL_62e9:  ldloc.s    V_13
    IL_62eb:  beq.s      IL_6307

    IL_62ed:  ldc.i4.1
    IL_62ee:  stloc.s    V_20
    IL_62f0:  ldstr      "te3 check failed {0} {1}"
    IL_62f5:  ldloc.3
    IL_62f6:  box        [mscorlib]System.Int32
    IL_62fb:  ldloc.s    V_13
    IL_62fd:  box        [mscorlib]System.Int32
    IL_6302:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6307:  ldloc.s    V_4
    IL_6309:  ldloc.s    V_14
    IL_630b:  beq.s      IL_6328

    IL_630d:  ldc.i4.1
    IL_630e:  stloc.s    V_20
    IL_6310:  ldstr      "te064 check failed {0} {1}"
    IL_6315:  ldloc.s    V_4
    IL_6317:  box        [mscorlib]System.Int64
    IL_631c:  ldloc.s    V_14
    IL_631e:  box        [mscorlib]System.Int64
    IL_6323:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6328:  ldloc.s    V_5
    IL_632a:  ldloc.s    V_15
    IL_632c:  beq.s      IL_6349

    IL_632e:  ldc.i4.1
    IL_632f:  stloc.s    V_20
    IL_6331:  ldstr      "te164 check failed {0} {1}"
    IL_6336:  ldloc.s    V_5
    IL_6338:  box        [mscorlib]System.Int64
    IL_633d:  ldloc.s    V_15
    IL_633f:  box        [mscorlib]System.Int64
    IL_6344:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6349:  ldloc.s    V_6
    IL_634b:  ldloc.s    V_16
    IL_634d:  beq.s      IL_636a

    IL_634f:  ldc.i4.1
    IL_6350:  stloc.s    V_20
    IL_6352:  ldstr      "te264 check failed {0} {1}"
    IL_6357:  ldloc.s    V_6
    IL_6359:  box        [mscorlib]System.Int64
    IL_635e:  ldloc.s    V_16
    IL_6360:  box        [mscorlib]System.Int64
    IL_6365:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_636a:  ldloc.s    V_7
    IL_636c:  ldloc.s    V_17
    IL_636e:  beq.s      IL_638b

    IL_6370:  ldc.i4.1
    IL_6371:  stloc.s    V_20
    IL_6373:  ldstr      "te364 check failed {0} {1}"
    IL_6378:  ldloc.s    V_7
    IL_637a:  box        [mscorlib]System.Int64
    IL_637f:  ldloc.s    V_17
    IL_6381:  box        [mscorlib]System.Int64
    IL_6386:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_638b:  ldloc.s    V_8
    IL_638d:  ldloc.s    V_18
    IL_638f:  beq.s      IL_63ac

    IL_6391:  ldc.i4.1
    IL_6392:  stloc.s    V_20
    IL_6394:  ldstr      "dummy check failed {0} {1}"
    IL_6399:  ldloc.s    V_8
    IL_639b:  box        [mscorlib]System.Int32
    IL_63a0:  ldloc.s    V_18
    IL_63a2:  box        [mscorlib]System.Int32
    IL_63a7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_63ac:  ldloc.s    V_9
    IL_63ae:  ldloc.s    V_19
    IL_63b0:  beq.s      IL_63cd

    IL_63b2:  ldc.i4.1
    IL_63b3:  stloc.s    V_20
    IL_63b5:  ldstr      "dummy64 check failed {0} {1}"
    IL_63ba:  ldloc.s    V_9
    IL_63bc:  box        [mscorlib]System.Int64
    IL_63c1:  ldloc.s    V_19
    IL_63c3:  box        [mscorlib]System.Int64
    IL_63c8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_63cd:  ldsfld     int32 IntrinsicTest::counter
    IL_63d2:  ldsfld     int32 IntrinsicTest::id_counter
    IL_63d7:  beq.s      IL_63fa

    IL_63d9:  ldstr      "counter mismatch {0} {1}"
    IL_63de:  ldsfld     int32 IntrinsicTest::counter
    IL_63e3:  box        [mscorlib]System.Int32
    IL_63e8:  ldsfld     int32 IntrinsicTest::id_counter
    IL_63ed:  box        [mscorlib]System.Int32
    IL_63f2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_63f7:  ldc.i4.1
    IL_63f8:  stloc.s    V_20
    IL_63fa:  ldsfld     int64 IntrinsicTest::counter64
    IL_63ff:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6404:  beq.s      IL_6427

    IL_6406:  ldstr      "counter64 mismatch {0} {1}"
    IL_640b:  ldsfld     int64 IntrinsicTest::counter64
    IL_6410:  box        [mscorlib]System.Int64
    IL_6415:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_641a:  box        [mscorlib]System.Int64
    IL_641f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6424:  ldc.i4.1
    IL_6425:  stloc.s    V_20
    IL_6427:  ldloc.s    V_21
    IL_6429:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_642e:  ldloc.s    V_21
    IL_6430:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_6435:  beq.s      IL_645c

    IL_6437:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_643c:  ldloc.s    V_21
    IL_643e:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_6443:  box        [mscorlib]System.Int32
    IL_6448:  ldloc.s    V_21
    IL_644a:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_644f:  box        [mscorlib]System.Int32
    IL_6454:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6459:  ldc.i4.1
    IL_645a:  stloc.s    V_20
    IL_645c:  ldloc.s    V_21
    IL_645e:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_6463:  ldloc.s    V_21
    IL_6465:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_646a:  beq.s      IL_6491

    IL_646c:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_6471:  ldloc.s    V_21
    IL_6473:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_6478:  box        [mscorlib]System.Int64
    IL_647d:  ldloc.s    V_21
    IL_647f:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_6484:  box        [mscorlib]System.Int64
    IL_6489:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_648e:  ldc.i4.1
    IL_648f:  stloc.s    V_20
    IL_6491:  ldsfld     int64 IntrinsicTest::temp
    IL_6496:  ldsfld     int64 IntrinsicTest::idmp
    IL_649b:  beq.s      IL_64be

    IL_649d:  ldstr      "temp mismatch {0} {1}"
    IL_64a2:  ldsfld     int64 IntrinsicTest::temp
    IL_64a7:  box        [mscorlib]System.Int64
    IL_64ac:  ldsfld     int64 IntrinsicTest::idmp
    IL_64b1:  box        [mscorlib]System.Int64
    IL_64b6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_64bb:  ldc.i4.1
    IL_64bc:  stloc.s    V_20
    IL_64be:  ldloc.s    V_21
    IL_64c0:  ldsflda    int64 IntrinsicTest::counter64
    IL_64c5:  ldsfld     int64 IntrinsicTest::counter64
    IL_64ca:  ldloc.s    V_5
    IL_64cc:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_64d1:  stfld      int64 IntrinsicTest::instanceCounter64
    IL_64d6:  ldloc.s    V_21
    IL_64d8:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_64dd:  stfld      int64 IntrinsicTest::id_instanceCounter64
    IL_64e2:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_64e7:  ldloc.s    V_15
    IL_64e9:  bne.un.s   IL_64f7

    IL_64eb:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_64f0:  stsfld     int64 IntrinsicTest::id_counter64
    IL_64f5:  br.s       IL_6501

    IL_64f7:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_64fc:  stsfld     int64 IntrinsicTest::idjunk
    IL_6501:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 5 5"
    IL_6506:  call       void [System.Console]System.Console::WriteLine(string)
    IL_650b:  ldloc.0
    IL_650c:  ldloc.s    V_10
    IL_650e:  beq.s      IL_652a

    IL_6510:  ldc.i4.1
    IL_6511:  stloc.s    V_20
    IL_6513:  ldstr      "te0 check failed {0} {1}"
    IL_6518:  ldloc.0
    IL_6519:  box        [mscorlib]System.Int32
    IL_651e:  ldloc.s    V_10
    IL_6520:  box        [mscorlib]System.Int32
    IL_6525:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_652a:  ldloc.1
    IL_652b:  ldloc.s    V_11
    IL_652d:  beq.s      IL_6549

    IL_652f:  ldc.i4.1
    IL_6530:  stloc.s    V_20
    IL_6532:  ldstr      "te1 check failed {0} {1}"
    IL_6537:  ldloc.1
    IL_6538:  box        [mscorlib]System.Int32
    IL_653d:  ldloc.s    V_11
    IL_653f:  box        [mscorlib]System.Int32
    IL_6544:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6549:  ldloc.2
    IL_654a:  ldloc.s    V_12
    IL_654c:  beq.s      IL_6568

    IL_654e:  ldc.i4.1
    IL_654f:  stloc.s    V_20
    IL_6551:  ldstr      "te2 check failed {0} {1}"
    IL_6556:  ldloc.2
    IL_6557:  box        [mscorlib]System.Int32
    IL_655c:  ldloc.s    V_12
    IL_655e:  box        [mscorlib]System.Int32
    IL_6563:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6568:  ldloc.3
    IL_6569:  ldloc.s    V_13
    IL_656b:  beq.s      IL_6587

    IL_656d:  ldc.i4.1
    IL_656e:  stloc.s    V_20
    IL_6570:  ldstr      "te3 check failed {0} {1}"
    IL_6575:  ldloc.3
    IL_6576:  box        [mscorlib]System.Int32
    IL_657b:  ldloc.s    V_13
    IL_657d:  box        [mscorlib]System.Int32
    IL_6582:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6587:  ldloc.s    V_4
    IL_6589:  ldloc.s    V_14
    IL_658b:  beq.s      IL_65a8

    IL_658d:  ldc.i4.1
    IL_658e:  stloc.s    V_20
    IL_6590:  ldstr      "te064 check failed {0} {1}"
    IL_6595:  ldloc.s    V_4
    IL_6597:  box        [mscorlib]System.Int64
    IL_659c:  ldloc.s    V_14
    IL_659e:  box        [mscorlib]System.Int64
    IL_65a3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_65a8:  ldloc.s    V_5
    IL_65aa:  ldloc.s    V_15
    IL_65ac:  beq.s      IL_65c9

    IL_65ae:  ldc.i4.1
    IL_65af:  stloc.s    V_20
    IL_65b1:  ldstr      "te164 check failed {0} {1}"
    IL_65b6:  ldloc.s    V_5
    IL_65b8:  box        [mscorlib]System.Int64
    IL_65bd:  ldloc.s    V_15
    IL_65bf:  box        [mscorlib]System.Int64
    IL_65c4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_65c9:  ldloc.s    V_6
    IL_65cb:  ldloc.s    V_16
    IL_65cd:  beq.s      IL_65ea

    IL_65cf:  ldc.i4.1
    IL_65d0:  stloc.s    V_20
    IL_65d2:  ldstr      "te264 check failed {0} {1}"
    IL_65d7:  ldloc.s    V_6
    IL_65d9:  box        [mscorlib]System.Int64
    IL_65de:  ldloc.s    V_16
    IL_65e0:  box        [mscorlib]System.Int64
    IL_65e5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_65ea:  ldloc.s    V_7
    IL_65ec:  ldloc.s    V_17
    IL_65ee:  beq.s      IL_660b

    IL_65f0:  ldc.i4.1
    IL_65f1:  stloc.s    V_20
    IL_65f3:  ldstr      "te364 check failed {0} {1}"
    IL_65f8:  ldloc.s    V_7
    IL_65fa:  box        [mscorlib]System.Int64
    IL_65ff:  ldloc.s    V_17
    IL_6601:  box        [mscorlib]System.Int64
    IL_6606:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_660b:  ldloc.s    V_8
    IL_660d:  ldloc.s    V_18
    IL_660f:  beq.s      IL_662c

    IL_6611:  ldc.i4.1
    IL_6612:  stloc.s    V_20
    IL_6614:  ldstr      "dummy check failed {0} {1}"
    IL_6619:  ldloc.s    V_8
    IL_661b:  box        [mscorlib]System.Int32
    IL_6620:  ldloc.s    V_18
    IL_6622:  box        [mscorlib]System.Int32
    IL_6627:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_662c:  ldloc.s    V_9
    IL_662e:  ldloc.s    V_19
    IL_6630:  beq.s      IL_664d

    IL_6632:  ldc.i4.1
    IL_6633:  stloc.s    V_20
    IL_6635:  ldstr      "dummy64 check failed {0} {1}"
    IL_663a:  ldloc.s    V_9
    IL_663c:  box        [mscorlib]System.Int64
    IL_6641:  ldloc.s    V_19
    IL_6643:  box        [mscorlib]System.Int64
    IL_6648:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_664d:  ldsfld     int32 IntrinsicTest::counter
    IL_6652:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6657:  beq.s      IL_667a

    IL_6659:  ldstr      "counter mismatch {0} {1}"
    IL_665e:  ldsfld     int32 IntrinsicTest::counter
    IL_6663:  box        [mscorlib]System.Int32
    IL_6668:  ldsfld     int32 IntrinsicTest::id_counter
    IL_666d:  box        [mscorlib]System.Int32
    IL_6672:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6677:  ldc.i4.1
    IL_6678:  stloc.s    V_20
    IL_667a:  ldsfld     int64 IntrinsicTest::counter64
    IL_667f:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6684:  beq.s      IL_66a7

    IL_6686:  ldstr      "counter64 mismatch {0} {1}"
    IL_668b:  ldsfld     int64 IntrinsicTest::counter64
    IL_6690:  box        [mscorlib]System.Int64
    IL_6695:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_669a:  box        [mscorlib]System.Int64
    IL_669f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_66a4:  ldc.i4.1
    IL_66a5:  stloc.s    V_20
    IL_66a7:  ldloc.s    V_21
    IL_66a9:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_66ae:  ldloc.s    V_21
    IL_66b0:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_66b5:  beq.s      IL_66dc

    IL_66b7:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_66bc:  ldloc.s    V_21
    IL_66be:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_66c3:  box        [mscorlib]System.Int32
    IL_66c8:  ldloc.s    V_21
    IL_66ca:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_66cf:  box        [mscorlib]System.Int32
    IL_66d4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_66d9:  ldc.i4.1
    IL_66da:  stloc.s    V_20
    IL_66dc:  ldloc.s    V_21
    IL_66de:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_66e3:  ldloc.s    V_21
    IL_66e5:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_66ea:  beq.s      IL_6711

    IL_66ec:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_66f1:  ldloc.s    V_21
    IL_66f3:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_66f8:  box        [mscorlib]System.Int64
    IL_66fd:  ldloc.s    V_21
    IL_66ff:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_6704:  box        [mscorlib]System.Int64
    IL_6709:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_670e:  ldc.i4.1
    IL_670f:  stloc.s    V_20
    IL_6711:  ldsfld     int64 IntrinsicTest::temp
    IL_6716:  ldsfld     int64 IntrinsicTest::idmp
    IL_671b:  beq.s      IL_673e

    IL_671d:  ldstr      "temp mismatch {0} {1}"
    IL_6722:  ldsfld     int64 IntrinsicTest::temp
    IL_6727:  box        [mscorlib]System.Int64
    IL_672c:  ldsfld     int64 IntrinsicTest::idmp
    IL_6731:  box        [mscorlib]System.Int64
    IL_6736:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_673b:  ldc.i4.1
    IL_673c:  stloc.s    V_20
    IL_673e:  ldsflda    int64 IntrinsicTest::counter64
    IL_6743:  ldsfld     int64 IntrinsicTest::counter64
    IL_6748:  ldloc.s    V_5
    IL_674a:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_674f:  stloc.s    V_7
    IL_6751:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6756:  stloc.s    V_17
    IL_6758:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_675d:  ldloc.s    V_15
    IL_675f:  bne.un.s   IL_676d

    IL_6761:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6766:  stsfld     int64 IntrinsicTest::id_counter64
    IL_676b:  br.s       IL_6777

    IL_676d:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6772:  stsfld     int64 IntrinsicTest::idjunk
    IL_6777:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 5 6"
    IL_677c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_6781:  ldloc.0
    IL_6782:  ldloc.s    V_10
    IL_6784:  beq.s      IL_67a0

    IL_6786:  ldc.i4.1
    IL_6787:  stloc.s    V_20
    IL_6789:  ldstr      "te0 check failed {0} {1}"
    IL_678e:  ldloc.0
    IL_678f:  box        [mscorlib]System.Int32
    IL_6794:  ldloc.s    V_10
    IL_6796:  box        [mscorlib]System.Int32
    IL_679b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_67a0:  ldloc.1
    IL_67a1:  ldloc.s    V_11
    IL_67a3:  beq.s      IL_67bf

    IL_67a5:  ldc.i4.1
    IL_67a6:  stloc.s    V_20
    IL_67a8:  ldstr      "te1 check failed {0} {1}"
    IL_67ad:  ldloc.1
    IL_67ae:  box        [mscorlib]System.Int32
    IL_67b3:  ldloc.s    V_11
    IL_67b5:  box        [mscorlib]System.Int32
    IL_67ba:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_67bf:  ldloc.2
    IL_67c0:  ldloc.s    V_12
    IL_67c2:  beq.s      IL_67de

    IL_67c4:  ldc.i4.1
    IL_67c5:  stloc.s    V_20
    IL_67c7:  ldstr      "te2 check failed {0} {1}"
    IL_67cc:  ldloc.2
    IL_67cd:  box        [mscorlib]System.Int32
    IL_67d2:  ldloc.s    V_12
    IL_67d4:  box        [mscorlib]System.Int32
    IL_67d9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_67de:  ldloc.3
    IL_67df:  ldloc.s    V_13
    IL_67e1:  beq.s      IL_67fd

    IL_67e3:  ldc.i4.1
    IL_67e4:  stloc.s    V_20
    IL_67e6:  ldstr      "te3 check failed {0} {1}"
    IL_67eb:  ldloc.3
    IL_67ec:  box        [mscorlib]System.Int32
    IL_67f1:  ldloc.s    V_13
    IL_67f3:  box        [mscorlib]System.Int32
    IL_67f8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_67fd:  ldloc.s    V_4
    IL_67ff:  ldloc.s    V_14
    IL_6801:  beq.s      IL_681e

    IL_6803:  ldc.i4.1
    IL_6804:  stloc.s    V_20
    IL_6806:  ldstr      "te064 check failed {0} {1}"
    IL_680b:  ldloc.s    V_4
    IL_680d:  box        [mscorlib]System.Int64
    IL_6812:  ldloc.s    V_14
    IL_6814:  box        [mscorlib]System.Int64
    IL_6819:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_681e:  ldloc.s    V_5
    IL_6820:  ldloc.s    V_15
    IL_6822:  beq.s      IL_683f

    IL_6824:  ldc.i4.1
    IL_6825:  stloc.s    V_20
    IL_6827:  ldstr      "te164 check failed {0} {1}"
    IL_682c:  ldloc.s    V_5
    IL_682e:  box        [mscorlib]System.Int64
    IL_6833:  ldloc.s    V_15
    IL_6835:  box        [mscorlib]System.Int64
    IL_683a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_683f:  ldloc.s    V_6
    IL_6841:  ldloc.s    V_16
    IL_6843:  beq.s      IL_6860

    IL_6845:  ldc.i4.1
    IL_6846:  stloc.s    V_20
    IL_6848:  ldstr      "te264 check failed {0} {1}"
    IL_684d:  ldloc.s    V_6
    IL_684f:  box        [mscorlib]System.Int64
    IL_6854:  ldloc.s    V_16
    IL_6856:  box        [mscorlib]System.Int64
    IL_685b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6860:  ldloc.s    V_7
    IL_6862:  ldloc.s    V_17
    IL_6864:  beq.s      IL_6881

    IL_6866:  ldc.i4.1
    IL_6867:  stloc.s    V_20
    IL_6869:  ldstr      "te364 check failed {0} {1}"
    IL_686e:  ldloc.s    V_7
    IL_6870:  box        [mscorlib]System.Int64
    IL_6875:  ldloc.s    V_17
    IL_6877:  box        [mscorlib]System.Int64
    IL_687c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6881:  ldloc.s    V_8
    IL_6883:  ldloc.s    V_18
    IL_6885:  beq.s      IL_68a2

    IL_6887:  ldc.i4.1
    IL_6888:  stloc.s    V_20
    IL_688a:  ldstr      "dummy check failed {0} {1}"
    IL_688f:  ldloc.s    V_8
    IL_6891:  box        [mscorlib]System.Int32
    IL_6896:  ldloc.s    V_18
    IL_6898:  box        [mscorlib]System.Int32
    IL_689d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_68a2:  ldloc.s    V_9
    IL_68a4:  ldloc.s    V_19
    IL_68a6:  beq.s      IL_68c3

    IL_68a8:  ldc.i4.1
    IL_68a9:  stloc.s    V_20
    IL_68ab:  ldstr      "dummy64 check failed {0} {1}"
    IL_68b0:  ldloc.s    V_9
    IL_68b2:  box        [mscorlib]System.Int64
    IL_68b7:  ldloc.s    V_19
    IL_68b9:  box        [mscorlib]System.Int64
    IL_68be:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_68c3:  ldsfld     int32 IntrinsicTest::counter
    IL_68c8:  ldsfld     int32 IntrinsicTest::id_counter
    IL_68cd:  beq.s      IL_68f0

    IL_68cf:  ldstr      "counter mismatch {0} {1}"
    IL_68d4:  ldsfld     int32 IntrinsicTest::counter
    IL_68d9:  box        [mscorlib]System.Int32
    IL_68de:  ldsfld     int32 IntrinsicTest::id_counter
    IL_68e3:  box        [mscorlib]System.Int32
    IL_68e8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_68ed:  ldc.i4.1
    IL_68ee:  stloc.s    V_20
    IL_68f0:  ldsfld     int64 IntrinsicTest::counter64
    IL_68f5:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_68fa:  beq.s      IL_691d

    IL_68fc:  ldstr      "counter64 mismatch {0} {1}"
    IL_6901:  ldsfld     int64 IntrinsicTest::counter64
    IL_6906:  box        [mscorlib]System.Int64
    IL_690b:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6910:  box        [mscorlib]System.Int64
    IL_6915:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_691a:  ldc.i4.1
    IL_691b:  stloc.s    V_20
    IL_691d:  ldloc.s    V_21
    IL_691f:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_6924:  ldloc.s    V_21
    IL_6926:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_692b:  beq.s      IL_6952

    IL_692d:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_6932:  ldloc.s    V_21
    IL_6934:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_6939:  box        [mscorlib]System.Int32
    IL_693e:  ldloc.s    V_21
    IL_6940:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_6945:  box        [mscorlib]System.Int32
    IL_694a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_694f:  ldc.i4.1
    IL_6950:  stloc.s    V_20
    IL_6952:  ldloc.s    V_21
    IL_6954:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_6959:  ldloc.s    V_21
    IL_695b:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_6960:  beq.s      IL_6987

    IL_6962:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_6967:  ldloc.s    V_21
    IL_6969:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_696e:  box        [mscorlib]System.Int64
    IL_6973:  ldloc.s    V_21
    IL_6975:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_697a:  box        [mscorlib]System.Int64
    IL_697f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6984:  ldc.i4.1
    IL_6985:  stloc.s    V_20
    IL_6987:  ldsfld     int64 IntrinsicTest::temp
    IL_698c:  ldsfld     int64 IntrinsicTest::idmp
    IL_6991:  beq.s      IL_69b4

    IL_6993:  ldstr      "temp mismatch {0} {1}"
    IL_6998:  ldsfld     int64 IntrinsicTest::temp
    IL_699d:  box        [mscorlib]System.Int64
    IL_69a2:  ldsfld     int64 IntrinsicTest::idmp
    IL_69a7:  box        [mscorlib]System.Int64
    IL_69ac:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_69b1:  ldc.i4.1
    IL_69b2:  stloc.s    V_20
    IL_69b4:  ldsflda    int64 IntrinsicTest::counter64
    IL_69b9:  ldsfld     int64 IntrinsicTest::counter64
    IL_69be:  ldloc.s    V_6
    IL_69c0:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_69c5:  stloc.s    V_4
    IL_69c7:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_69cc:  stloc.s    V_14
    IL_69ce:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_69d3:  ldloc.s    V_16
    IL_69d5:  bne.un.s   IL_69e3

    IL_69d7:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_69dc:  stsfld     int64 IntrinsicTest::id_counter64
    IL_69e1:  br.s       IL_69ed

    IL_69e3:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_69e8:  stsfld     int64 IntrinsicTest::idjunk
    IL_69ed:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 6 0"
    IL_69f2:  call       void [System.Console]System.Console::WriteLine(string)
    IL_69f7:  ldloc.0
    IL_69f8:  ldloc.s    V_10
    IL_69fa:  beq.s      IL_6a16

    IL_69fc:  ldc.i4.1
    IL_69fd:  stloc.s    V_20
    IL_69ff:  ldstr      "te0 check failed {0} {1}"
    IL_6a04:  ldloc.0
    IL_6a05:  box        [mscorlib]System.Int32
    IL_6a0a:  ldloc.s    V_10
    IL_6a0c:  box        [mscorlib]System.Int32
    IL_6a11:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6a16:  ldloc.1
    IL_6a17:  ldloc.s    V_11
    IL_6a19:  beq.s      IL_6a35

    IL_6a1b:  ldc.i4.1
    IL_6a1c:  stloc.s    V_20
    IL_6a1e:  ldstr      "te1 check failed {0} {1}"
    IL_6a23:  ldloc.1
    IL_6a24:  box        [mscorlib]System.Int32
    IL_6a29:  ldloc.s    V_11
    IL_6a2b:  box        [mscorlib]System.Int32
    IL_6a30:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6a35:  ldloc.2
    IL_6a36:  ldloc.s    V_12
    IL_6a38:  beq.s      IL_6a54

    IL_6a3a:  ldc.i4.1
    IL_6a3b:  stloc.s    V_20
    IL_6a3d:  ldstr      "te2 check failed {0} {1}"
    IL_6a42:  ldloc.2
    IL_6a43:  box        [mscorlib]System.Int32
    IL_6a48:  ldloc.s    V_12
    IL_6a4a:  box        [mscorlib]System.Int32
    IL_6a4f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6a54:  ldloc.3
    IL_6a55:  ldloc.s    V_13
    IL_6a57:  beq.s      IL_6a73

    IL_6a59:  ldc.i4.1
    IL_6a5a:  stloc.s    V_20
    IL_6a5c:  ldstr      "te3 check failed {0} {1}"
    IL_6a61:  ldloc.3
    IL_6a62:  box        [mscorlib]System.Int32
    IL_6a67:  ldloc.s    V_13
    IL_6a69:  box        [mscorlib]System.Int32
    IL_6a6e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6a73:  ldloc.s    V_4
    IL_6a75:  ldloc.s    V_14
    IL_6a77:  beq.s      IL_6a94

    IL_6a79:  ldc.i4.1
    IL_6a7a:  stloc.s    V_20
    IL_6a7c:  ldstr      "te064 check failed {0} {1}"
    IL_6a81:  ldloc.s    V_4
    IL_6a83:  box        [mscorlib]System.Int64
    IL_6a88:  ldloc.s    V_14
    IL_6a8a:  box        [mscorlib]System.Int64
    IL_6a8f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6a94:  ldloc.s    V_5
    IL_6a96:  ldloc.s    V_15
    IL_6a98:  beq.s      IL_6ab5

    IL_6a9a:  ldc.i4.1
    IL_6a9b:  stloc.s    V_20
    IL_6a9d:  ldstr      "te164 check failed {0} {1}"
    IL_6aa2:  ldloc.s    V_5
    IL_6aa4:  box        [mscorlib]System.Int64
    IL_6aa9:  ldloc.s    V_15
    IL_6aab:  box        [mscorlib]System.Int64
    IL_6ab0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6ab5:  ldloc.s    V_6
    IL_6ab7:  ldloc.s    V_16
    IL_6ab9:  beq.s      IL_6ad6

    IL_6abb:  ldc.i4.1
    IL_6abc:  stloc.s    V_20
    IL_6abe:  ldstr      "te264 check failed {0} {1}"
    IL_6ac3:  ldloc.s    V_6
    IL_6ac5:  box        [mscorlib]System.Int64
    IL_6aca:  ldloc.s    V_16
    IL_6acc:  box        [mscorlib]System.Int64
    IL_6ad1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6ad6:  ldloc.s    V_7
    IL_6ad8:  ldloc.s    V_17
    IL_6ada:  beq.s      IL_6af7

    IL_6adc:  ldc.i4.1
    IL_6add:  stloc.s    V_20
    IL_6adf:  ldstr      "te364 check failed {0} {1}"
    IL_6ae4:  ldloc.s    V_7
    IL_6ae6:  box        [mscorlib]System.Int64
    IL_6aeb:  ldloc.s    V_17
    IL_6aed:  box        [mscorlib]System.Int64
    IL_6af2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6af7:  ldloc.s    V_8
    IL_6af9:  ldloc.s    V_18
    IL_6afb:  beq.s      IL_6b18

    IL_6afd:  ldc.i4.1
    IL_6afe:  stloc.s    V_20
    IL_6b00:  ldstr      "dummy check failed {0} {1}"
    IL_6b05:  ldloc.s    V_8
    IL_6b07:  box        [mscorlib]System.Int32
    IL_6b0c:  ldloc.s    V_18
    IL_6b0e:  box        [mscorlib]System.Int32
    IL_6b13:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6b18:  ldloc.s    V_9
    IL_6b1a:  ldloc.s    V_19
    IL_6b1c:  beq.s      IL_6b39

    IL_6b1e:  ldc.i4.1
    IL_6b1f:  stloc.s    V_20
    IL_6b21:  ldstr      "dummy64 check failed {0} {1}"
    IL_6b26:  ldloc.s    V_9
    IL_6b28:  box        [mscorlib]System.Int64
    IL_6b2d:  ldloc.s    V_19
    IL_6b2f:  box        [mscorlib]System.Int64
    IL_6b34:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6b39:  ldsfld     int32 IntrinsicTest::counter
    IL_6b3e:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6b43:  beq.s      IL_6b66

    IL_6b45:  ldstr      "counter mismatch {0} {1}"
    IL_6b4a:  ldsfld     int32 IntrinsicTest::counter
    IL_6b4f:  box        [mscorlib]System.Int32
    IL_6b54:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6b59:  box        [mscorlib]System.Int32
    IL_6b5e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6b63:  ldc.i4.1
    IL_6b64:  stloc.s    V_20
    IL_6b66:  ldsfld     int64 IntrinsicTest::counter64
    IL_6b6b:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6b70:  beq.s      IL_6b93

    IL_6b72:  ldstr      "counter64 mismatch {0} {1}"
    IL_6b77:  ldsfld     int64 IntrinsicTest::counter64
    IL_6b7c:  box        [mscorlib]System.Int64
    IL_6b81:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6b86:  box        [mscorlib]System.Int64
    IL_6b8b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6b90:  ldc.i4.1
    IL_6b91:  stloc.s    V_20
    IL_6b93:  ldloc.s    V_21
    IL_6b95:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_6b9a:  ldloc.s    V_21
    IL_6b9c:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_6ba1:  beq.s      IL_6bc8

    IL_6ba3:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_6ba8:  ldloc.s    V_21
    IL_6baa:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_6baf:  box        [mscorlib]System.Int32
    IL_6bb4:  ldloc.s    V_21
    IL_6bb6:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_6bbb:  box        [mscorlib]System.Int32
    IL_6bc0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6bc5:  ldc.i4.1
    IL_6bc6:  stloc.s    V_20
    IL_6bc8:  ldloc.s    V_21
    IL_6bca:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_6bcf:  ldloc.s    V_21
    IL_6bd1:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_6bd6:  beq.s      IL_6bfd

    IL_6bd8:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_6bdd:  ldloc.s    V_21
    IL_6bdf:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_6be4:  box        [mscorlib]System.Int64
    IL_6be9:  ldloc.s    V_21
    IL_6beb:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_6bf0:  box        [mscorlib]System.Int64
    IL_6bf5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6bfa:  ldc.i4.1
    IL_6bfb:  stloc.s    V_20
    IL_6bfd:  ldsfld     int64 IntrinsicTest::temp
    IL_6c02:  ldsfld     int64 IntrinsicTest::idmp
    IL_6c07:  beq.s      IL_6c2a

    IL_6c09:  ldstr      "temp mismatch {0} {1}"
    IL_6c0e:  ldsfld     int64 IntrinsicTest::temp
    IL_6c13:  box        [mscorlib]System.Int64
    IL_6c18:  ldsfld     int64 IntrinsicTest::idmp
    IL_6c1d:  box        [mscorlib]System.Int64
    IL_6c22:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6c27:  ldc.i4.1
    IL_6c28:  stloc.s    V_20
    IL_6c2a:  ldsflda    int64 IntrinsicTest::counter64
    IL_6c2f:  ldsfld     int64 IntrinsicTest::counter64
    IL_6c34:  ldloc.s    V_6
    IL_6c36:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_6c3b:  stsfld     int64 IntrinsicTest::counter64
    IL_6c40:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6c45:  stsfld     int64 IntrinsicTest::id_counter64
    IL_6c4a:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6c4f:  ldloc.s    V_16
    IL_6c51:  bne.un.s   IL_6c5f

    IL_6c53:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6c58:  stsfld     int64 IntrinsicTest::idjunk
    IL_6c5d:  br.s       IL_6c69

    IL_6c5f:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6c64:  stsfld     int64 IntrinsicTest::idjunk
    IL_6c69:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 6 1"
    IL_6c6e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_6c73:  ldloc.0
    IL_6c74:  ldloc.s    V_10
    IL_6c76:  beq.s      IL_6c92

    IL_6c78:  ldc.i4.1
    IL_6c79:  stloc.s    V_20
    IL_6c7b:  ldstr      "te0 check failed {0} {1}"
    IL_6c80:  ldloc.0
    IL_6c81:  box        [mscorlib]System.Int32
    IL_6c86:  ldloc.s    V_10
    IL_6c88:  box        [mscorlib]System.Int32
    IL_6c8d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6c92:  ldloc.1
    IL_6c93:  ldloc.s    V_11
    IL_6c95:  beq.s      IL_6cb1

    IL_6c97:  ldc.i4.1
    IL_6c98:  stloc.s    V_20
    IL_6c9a:  ldstr      "te1 check failed {0} {1}"
    IL_6c9f:  ldloc.1
    IL_6ca0:  box        [mscorlib]System.Int32
    IL_6ca5:  ldloc.s    V_11
    IL_6ca7:  box        [mscorlib]System.Int32
    IL_6cac:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6cb1:  ldloc.2
    IL_6cb2:  ldloc.s    V_12
    IL_6cb4:  beq.s      IL_6cd0

    IL_6cb6:  ldc.i4.1
    IL_6cb7:  stloc.s    V_20
    IL_6cb9:  ldstr      "te2 check failed {0} {1}"
    IL_6cbe:  ldloc.2
    IL_6cbf:  box        [mscorlib]System.Int32
    IL_6cc4:  ldloc.s    V_12
    IL_6cc6:  box        [mscorlib]System.Int32
    IL_6ccb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6cd0:  ldloc.3
    IL_6cd1:  ldloc.s    V_13
    IL_6cd3:  beq.s      IL_6cef

    IL_6cd5:  ldc.i4.1
    IL_6cd6:  stloc.s    V_20
    IL_6cd8:  ldstr      "te3 check failed {0} {1}"
    IL_6cdd:  ldloc.3
    IL_6cde:  box        [mscorlib]System.Int32
    IL_6ce3:  ldloc.s    V_13
    IL_6ce5:  box        [mscorlib]System.Int32
    IL_6cea:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6cef:  ldloc.s    V_4
    IL_6cf1:  ldloc.s    V_14
    IL_6cf3:  beq.s      IL_6d10

    IL_6cf5:  ldc.i4.1
    IL_6cf6:  stloc.s    V_20
    IL_6cf8:  ldstr      "te064 check failed {0} {1}"
    IL_6cfd:  ldloc.s    V_4
    IL_6cff:  box        [mscorlib]System.Int64
    IL_6d04:  ldloc.s    V_14
    IL_6d06:  box        [mscorlib]System.Int64
    IL_6d0b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6d10:  ldloc.s    V_5
    IL_6d12:  ldloc.s    V_15
    IL_6d14:  beq.s      IL_6d31

    IL_6d16:  ldc.i4.1
    IL_6d17:  stloc.s    V_20
    IL_6d19:  ldstr      "te164 check failed {0} {1}"
    IL_6d1e:  ldloc.s    V_5
    IL_6d20:  box        [mscorlib]System.Int64
    IL_6d25:  ldloc.s    V_15
    IL_6d27:  box        [mscorlib]System.Int64
    IL_6d2c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6d31:  ldloc.s    V_6
    IL_6d33:  ldloc.s    V_16
    IL_6d35:  beq.s      IL_6d52

    IL_6d37:  ldc.i4.1
    IL_6d38:  stloc.s    V_20
    IL_6d3a:  ldstr      "te264 check failed {0} {1}"
    IL_6d3f:  ldloc.s    V_6
    IL_6d41:  box        [mscorlib]System.Int64
    IL_6d46:  ldloc.s    V_16
    IL_6d48:  box        [mscorlib]System.Int64
    IL_6d4d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6d52:  ldloc.s    V_7
    IL_6d54:  ldloc.s    V_17
    IL_6d56:  beq.s      IL_6d73

    IL_6d58:  ldc.i4.1
    IL_6d59:  stloc.s    V_20
    IL_6d5b:  ldstr      "te364 check failed {0} {1}"
    IL_6d60:  ldloc.s    V_7
    IL_6d62:  box        [mscorlib]System.Int64
    IL_6d67:  ldloc.s    V_17
    IL_6d69:  box        [mscorlib]System.Int64
    IL_6d6e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6d73:  ldloc.s    V_8
    IL_6d75:  ldloc.s    V_18
    IL_6d77:  beq.s      IL_6d94

    IL_6d79:  ldc.i4.1
    IL_6d7a:  stloc.s    V_20
    IL_6d7c:  ldstr      "dummy check failed {0} {1}"
    IL_6d81:  ldloc.s    V_8
    IL_6d83:  box        [mscorlib]System.Int32
    IL_6d88:  ldloc.s    V_18
    IL_6d8a:  box        [mscorlib]System.Int32
    IL_6d8f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6d94:  ldloc.s    V_9
    IL_6d96:  ldloc.s    V_19
    IL_6d98:  beq.s      IL_6db5

    IL_6d9a:  ldc.i4.1
    IL_6d9b:  stloc.s    V_20
    IL_6d9d:  ldstr      "dummy64 check failed {0} {1}"
    IL_6da2:  ldloc.s    V_9
    IL_6da4:  box        [mscorlib]System.Int64
    IL_6da9:  ldloc.s    V_19
    IL_6dab:  box        [mscorlib]System.Int64
    IL_6db0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6db5:  ldsfld     int32 IntrinsicTest::counter
    IL_6dba:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6dbf:  beq.s      IL_6de2

    IL_6dc1:  ldstr      "counter mismatch {0} {1}"
    IL_6dc6:  ldsfld     int32 IntrinsicTest::counter
    IL_6dcb:  box        [mscorlib]System.Int32
    IL_6dd0:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6dd5:  box        [mscorlib]System.Int32
    IL_6dda:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6ddf:  ldc.i4.1
    IL_6de0:  stloc.s    V_20
    IL_6de2:  ldsfld     int64 IntrinsicTest::counter64
    IL_6de7:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6dec:  beq.s      IL_6e0f

    IL_6dee:  ldstr      "counter64 mismatch {0} {1}"
    IL_6df3:  ldsfld     int64 IntrinsicTest::counter64
    IL_6df8:  box        [mscorlib]System.Int64
    IL_6dfd:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6e02:  box        [mscorlib]System.Int64
    IL_6e07:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6e0c:  ldc.i4.1
    IL_6e0d:  stloc.s    V_20
    IL_6e0f:  ldloc.s    V_21
    IL_6e11:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_6e16:  ldloc.s    V_21
    IL_6e18:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_6e1d:  beq.s      IL_6e44

    IL_6e1f:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_6e24:  ldloc.s    V_21
    IL_6e26:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_6e2b:  box        [mscorlib]System.Int32
    IL_6e30:  ldloc.s    V_21
    IL_6e32:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_6e37:  box        [mscorlib]System.Int32
    IL_6e3c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6e41:  ldc.i4.1
    IL_6e42:  stloc.s    V_20
    IL_6e44:  ldloc.s    V_21
    IL_6e46:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_6e4b:  ldloc.s    V_21
    IL_6e4d:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_6e52:  beq.s      IL_6e79

    IL_6e54:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_6e59:  ldloc.s    V_21
    IL_6e5b:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_6e60:  box        [mscorlib]System.Int64
    IL_6e65:  ldloc.s    V_21
    IL_6e67:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_6e6c:  box        [mscorlib]System.Int64
    IL_6e71:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6e76:  ldc.i4.1
    IL_6e77:  stloc.s    V_20
    IL_6e79:  ldsfld     int64 IntrinsicTest::temp
    IL_6e7e:  ldsfld     int64 IntrinsicTest::idmp
    IL_6e83:  beq.s      IL_6ea6

    IL_6e85:  ldstr      "temp mismatch {0} {1}"
    IL_6e8a:  ldsfld     int64 IntrinsicTest::temp
    IL_6e8f:  box        [mscorlib]System.Int64
    IL_6e94:  ldsfld     int64 IntrinsicTest::idmp
    IL_6e99:  box        [mscorlib]System.Int64
    IL_6e9e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6ea3:  ldc.i4.1
    IL_6ea4:  stloc.s    V_20
    IL_6ea6:  ldloc.s    V_21
    IL_6ea8:  ldsflda    int64 IntrinsicTest::counter64
    IL_6ead:  ldsfld     int64 IntrinsicTest::counter64
    IL_6eb2:  ldloc.s    V_6
    IL_6eb4:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_6eb9:  stfld      int64 IntrinsicTest::instanceCounter64
    IL_6ebe:  ldloc.s    V_21
    IL_6ec0:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6ec5:  stfld      int64 IntrinsicTest::id_instanceCounter64
    IL_6eca:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6ecf:  ldloc.s    V_16
    IL_6ed1:  bne.un.s   IL_6edf

    IL_6ed3:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6ed8:  stsfld     int64 IntrinsicTest::id_counter64
    IL_6edd:  br.s       IL_6ee9

    IL_6edf:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6ee4:  stsfld     int64 IntrinsicTest::idjunk
    IL_6ee9:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 6 2"
    IL_6eee:  call       void [System.Console]System.Console::WriteLine(string)
    IL_6ef3:  ldloc.0
    IL_6ef4:  ldloc.s    V_10
    IL_6ef6:  beq.s      IL_6f12

    IL_6ef8:  ldc.i4.1
    IL_6ef9:  stloc.s    V_20
    IL_6efb:  ldstr      "te0 check failed {0} {1}"
    IL_6f00:  ldloc.0
    IL_6f01:  box        [mscorlib]System.Int32
    IL_6f06:  ldloc.s    V_10
    IL_6f08:  box        [mscorlib]System.Int32
    IL_6f0d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6f12:  ldloc.1
    IL_6f13:  ldloc.s    V_11
    IL_6f15:  beq.s      IL_6f31

    IL_6f17:  ldc.i4.1
    IL_6f18:  stloc.s    V_20
    IL_6f1a:  ldstr      "te1 check failed {0} {1}"
    IL_6f1f:  ldloc.1
    IL_6f20:  box        [mscorlib]System.Int32
    IL_6f25:  ldloc.s    V_11
    IL_6f27:  box        [mscorlib]System.Int32
    IL_6f2c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6f31:  ldloc.2
    IL_6f32:  ldloc.s    V_12
    IL_6f34:  beq.s      IL_6f50

    IL_6f36:  ldc.i4.1
    IL_6f37:  stloc.s    V_20
    IL_6f39:  ldstr      "te2 check failed {0} {1}"
    IL_6f3e:  ldloc.2
    IL_6f3f:  box        [mscorlib]System.Int32
    IL_6f44:  ldloc.s    V_12
    IL_6f46:  box        [mscorlib]System.Int32
    IL_6f4b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6f50:  ldloc.3
    IL_6f51:  ldloc.s    V_13
    IL_6f53:  beq.s      IL_6f6f

    IL_6f55:  ldc.i4.1
    IL_6f56:  stloc.s    V_20
    IL_6f58:  ldstr      "te3 check failed {0} {1}"
    IL_6f5d:  ldloc.3
    IL_6f5e:  box        [mscorlib]System.Int32
    IL_6f63:  ldloc.s    V_13
    IL_6f65:  box        [mscorlib]System.Int32
    IL_6f6a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6f6f:  ldloc.s    V_4
    IL_6f71:  ldloc.s    V_14
    IL_6f73:  beq.s      IL_6f90

    IL_6f75:  ldc.i4.1
    IL_6f76:  stloc.s    V_20
    IL_6f78:  ldstr      "te064 check failed {0} {1}"
    IL_6f7d:  ldloc.s    V_4
    IL_6f7f:  box        [mscorlib]System.Int64
    IL_6f84:  ldloc.s    V_14
    IL_6f86:  box        [mscorlib]System.Int64
    IL_6f8b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6f90:  ldloc.s    V_5
    IL_6f92:  ldloc.s    V_15
    IL_6f94:  beq.s      IL_6fb1

    IL_6f96:  ldc.i4.1
    IL_6f97:  stloc.s    V_20
    IL_6f99:  ldstr      "te164 check failed {0} {1}"
    IL_6f9e:  ldloc.s    V_5
    IL_6fa0:  box        [mscorlib]System.Int64
    IL_6fa5:  ldloc.s    V_15
    IL_6fa7:  box        [mscorlib]System.Int64
    IL_6fac:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6fb1:  ldloc.s    V_6
    IL_6fb3:  ldloc.s    V_16
    IL_6fb5:  beq.s      IL_6fd2

    IL_6fb7:  ldc.i4.1
    IL_6fb8:  stloc.s    V_20
    IL_6fba:  ldstr      "te264 check failed {0} {1}"
    IL_6fbf:  ldloc.s    V_6
    IL_6fc1:  box        [mscorlib]System.Int64
    IL_6fc6:  ldloc.s    V_16
    IL_6fc8:  box        [mscorlib]System.Int64
    IL_6fcd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6fd2:  ldloc.s    V_7
    IL_6fd4:  ldloc.s    V_17
    IL_6fd6:  beq.s      IL_6ff3

    IL_6fd8:  ldc.i4.1
    IL_6fd9:  stloc.s    V_20
    IL_6fdb:  ldstr      "te364 check failed {0} {1}"
    IL_6fe0:  ldloc.s    V_7
    IL_6fe2:  box        [mscorlib]System.Int64
    IL_6fe7:  ldloc.s    V_17
    IL_6fe9:  box        [mscorlib]System.Int64
    IL_6fee:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6ff3:  ldloc.s    V_8
    IL_6ff5:  ldloc.s    V_18
    IL_6ff7:  beq.s      IL_7014

    IL_6ff9:  ldc.i4.1
    IL_6ffa:  stloc.s    V_20
    IL_6ffc:  ldstr      "dummy check failed {0} {1}"
    IL_7001:  ldloc.s    V_8
    IL_7003:  box        [mscorlib]System.Int32
    IL_7008:  ldloc.s    V_18
    IL_700a:  box        [mscorlib]System.Int32
    IL_700f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7014:  ldloc.s    V_9
    IL_7016:  ldloc.s    V_19
    IL_7018:  beq.s      IL_7035

    IL_701a:  ldc.i4.1
    IL_701b:  stloc.s    V_20
    IL_701d:  ldstr      "dummy64 check failed {0} {1}"
    IL_7022:  ldloc.s    V_9
    IL_7024:  box        [mscorlib]System.Int64
    IL_7029:  ldloc.s    V_19
    IL_702b:  box        [mscorlib]System.Int64
    IL_7030:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7035:  ldsfld     int32 IntrinsicTest::counter
    IL_703a:  ldsfld     int32 IntrinsicTest::id_counter
    IL_703f:  beq.s      IL_7062

    IL_7041:  ldstr      "counter mismatch {0} {1}"
    IL_7046:  ldsfld     int32 IntrinsicTest::counter
    IL_704b:  box        [mscorlib]System.Int32
    IL_7050:  ldsfld     int32 IntrinsicTest::id_counter
    IL_7055:  box        [mscorlib]System.Int32
    IL_705a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_705f:  ldc.i4.1
    IL_7060:  stloc.s    V_20
    IL_7062:  ldsfld     int64 IntrinsicTest::counter64
    IL_7067:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_706c:  beq.s      IL_708f

    IL_706e:  ldstr      "counter64 mismatch {0} {1}"
    IL_7073:  ldsfld     int64 IntrinsicTest::counter64
    IL_7078:  box        [mscorlib]System.Int64
    IL_707d:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_7082:  box        [mscorlib]System.Int64
    IL_7087:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_708c:  ldc.i4.1
    IL_708d:  stloc.s    V_20
    IL_708f:  ldloc.s    V_21
    IL_7091:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_7096:  ldloc.s    V_21
    IL_7098:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_709d:  beq.s      IL_70c4

    IL_709f:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_70a4:  ldloc.s    V_21
    IL_70a6:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_70ab:  box        [mscorlib]System.Int32
    IL_70b0:  ldloc.s    V_21
    IL_70b2:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_70b7:  box        [mscorlib]System.Int32
    IL_70bc:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_70c1:  ldc.i4.1
    IL_70c2:  stloc.s    V_20
    IL_70c4:  ldloc.s    V_21
    IL_70c6:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_70cb:  ldloc.s    V_21
    IL_70cd:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_70d2:  beq.s      IL_70f9

    IL_70d4:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_70d9:  ldloc.s    V_21
    IL_70db:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_70e0:  box        [mscorlib]System.Int64
    IL_70e5:  ldloc.s    V_21
    IL_70e7:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_70ec:  box        [mscorlib]System.Int64
    IL_70f1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_70f6:  ldc.i4.1
    IL_70f7:  stloc.s    V_20
    IL_70f9:  ldsfld     int64 IntrinsicTest::temp
    IL_70fe:  ldsfld     int64 IntrinsicTest::idmp
    IL_7103:  beq.s      IL_7126

    IL_7105:  ldstr      "temp mismatch {0} {1}"
    IL_710a:  ldsfld     int64 IntrinsicTest::temp
    IL_710f:  box        [mscorlib]System.Int64
    IL_7114:  ldsfld     int64 IntrinsicTest::idmp
    IL_7119:  box        [mscorlib]System.Int64
    IL_711e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7123:  ldc.i4.1
    IL_7124:  stloc.s    V_20
    IL_7126:  ldsflda    int64 IntrinsicTest::counter64
    IL_712b:  ldsfld     int64 IntrinsicTest::counter64
    IL_7130:  ldloc.s    V_6
    IL_7132:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_7137:  stloc.s    V_4
    IL_7139:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_713e:  stloc.s    V_14
    IL_7140:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_7145:  ldloc.s    V_16
    IL_7147:  bne.un.s   IL_7155

    IL_7149:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_714e:  stsfld     int64 IntrinsicTest::id_counter64
    IL_7153:  br.s       IL_715f

    IL_7155:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_715a:  stsfld     int64 IntrinsicTest::idjunk
    IL_715f:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 6 3"
    IL_7164:  call       void [System.Console]System.Console::WriteLine(string)
    IL_7169:  ldloc.0
    IL_716a:  ldloc.s    V_10
    IL_716c:  beq.s      IL_7188

    IL_716e:  ldc.i4.1
    IL_716f:  stloc.s    V_20
    IL_7171:  ldstr      "te0 check failed {0} {1}"
    IL_7176:  ldloc.0
    IL_7177:  box        [mscorlib]System.Int32
    IL_717c:  ldloc.s    V_10
    IL_717e:  box        [mscorlib]System.Int32
    IL_7183:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7188:  ldloc.1
    IL_7189:  ldloc.s    V_11
    IL_718b:  beq.s      IL_71a7

    IL_718d:  ldc.i4.1
    IL_718e:  stloc.s    V_20
    IL_7190:  ldstr      "te1 check failed {0} {1}"
    IL_7195:  ldloc.1
    IL_7196:  box        [mscorlib]System.Int32
    IL_719b:  ldloc.s    V_11
    IL_719d:  box        [mscorlib]System.Int32
    IL_71a2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_71a7:  ldloc.2
    IL_71a8:  ldloc.s    V_12
    IL_71aa:  beq.s      IL_71c6

    IL_71ac:  ldc.i4.1
    IL_71ad:  stloc.s    V_20
    IL_71af:  ldstr      "te2 check failed {0} {1}"
    IL_71b4:  ldloc.2
    IL_71b5:  box        [mscorlib]System.Int32
    IL_71ba:  ldloc.s    V_12
    IL_71bc:  box        [mscorlib]System.Int32
    IL_71c1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_71c6:  ldloc.3
    IL_71c7:  ldloc.s    V_13
    IL_71c9:  beq.s      IL_71e5

    IL_71cb:  ldc.i4.1
    IL_71cc:  stloc.s    V_20
    IL_71ce:  ldstr      "te3 check failed {0} {1}"
    IL_71d3:  ldloc.3
    IL_71d4:  box        [mscorlib]System.Int32
    IL_71d9:  ldloc.s    V_13
    IL_71db:  box        [mscorlib]System.Int32
    IL_71e0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_71e5:  ldloc.s    V_4
    IL_71e7:  ldloc.s    V_14
    IL_71e9:  beq.s      IL_7206

    IL_71eb:  ldc.i4.1
    IL_71ec:  stloc.s    V_20
    IL_71ee:  ldstr      "te064 check failed {0} {1}"
    IL_71f3:  ldloc.s    V_4
    IL_71f5:  box        [mscorlib]System.Int64
    IL_71fa:  ldloc.s    V_14
    IL_71fc:  box        [mscorlib]System.Int64
    IL_7201:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7206:  ldloc.s    V_5
    IL_7208:  ldloc.s    V_15
    IL_720a:  beq.s      IL_7227

    IL_720c:  ldc.i4.1
    IL_720d:  stloc.s    V_20
    IL_720f:  ldstr      "te164 check failed {0} {1}"
    IL_7214:  ldloc.s    V_5
    IL_7216:  box        [mscorlib]System.Int64
    IL_721b:  ldloc.s    V_15
    IL_721d:  box        [mscorlib]System.Int64
    IL_7222:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7227:  ldloc.s    V_6
    IL_7229:  ldloc.s    V_16
    IL_722b:  beq.s      IL_7248

    IL_722d:  ldc.i4.1
    IL_722e:  stloc.s    V_20
    IL_7230:  ldstr      "te264 check failed {0} {1}"
    IL_7235:  ldloc.s    V_6
    IL_7237:  box        [mscorlib]System.Int64
    IL_723c:  ldloc.s    V_16
    IL_723e:  box        [mscorlib]System.Int64
    IL_7243:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7248:  ldloc.s    V_7
    IL_724a:  ldloc.s    V_17
    IL_724c:  beq.s      IL_7269

    IL_724e:  ldc.i4.1
    IL_724f:  stloc.s    V_20
    IL_7251:  ldstr      "te364 check failed {0} {1}"
    IL_7256:  ldloc.s    V_7
    IL_7258:  box        [mscorlib]System.Int64
    IL_725d:  ldloc.s    V_17
    IL_725f:  box        [mscorlib]System.Int64
    IL_7264:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7269:  ldloc.s    V_8
    IL_726b:  ldloc.s    V_18
    IL_726d:  beq.s      IL_728a

    IL_726f:  ldc.i4.1
    IL_7270:  stloc.s    V_20
    IL_7272:  ldstr      "dummy check failed {0} {1}"
    IL_7277:  ldloc.s    V_8
    IL_7279:  box        [mscorlib]System.Int32
    IL_727e:  ldloc.s    V_18
    IL_7280:  box        [mscorlib]System.Int32
    IL_7285:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_728a:  ldloc.s    V_9
    IL_728c:  ldloc.s    V_19
    IL_728e:  beq.s      IL_72ab

    IL_7290:  ldc.i4.1
    IL_7291:  stloc.s    V_20
    IL_7293:  ldstr      "dummy64 check failed {0} {1}"
    IL_7298:  ldloc.s    V_9
    IL_729a:  box        [mscorlib]System.Int64
    IL_729f:  ldloc.s    V_19
    IL_72a1:  box        [mscorlib]System.Int64
    IL_72a6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_72ab:  ldsfld     int32 IntrinsicTest::counter
    IL_72b0:  ldsfld     int32 IntrinsicTest::id_counter
    IL_72b5:  beq.s      IL_72d8

    IL_72b7:  ldstr      "counter mismatch {0} {1}"
    IL_72bc:  ldsfld     int32 IntrinsicTest::counter
    IL_72c1:  box        [mscorlib]System.Int32
    IL_72c6:  ldsfld     int32 IntrinsicTest::id_counter
    IL_72cb:  box        [mscorlib]System.Int32
    IL_72d0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_72d5:  ldc.i4.1
    IL_72d6:  stloc.s    V_20
    IL_72d8:  ldsfld     int64 IntrinsicTest::counter64
    IL_72dd:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_72e2:  beq.s      IL_7305

    IL_72e4:  ldstr      "counter64 mismatch {0} {1}"
    IL_72e9:  ldsfld     int64 IntrinsicTest::counter64
    IL_72ee:  box        [mscorlib]System.Int64
    IL_72f3:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_72f8:  box        [mscorlib]System.Int64
    IL_72fd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7302:  ldc.i4.1
    IL_7303:  stloc.s    V_20
    IL_7305:  ldloc.s    V_21
    IL_7307:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_730c:  ldloc.s    V_21
    IL_730e:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_7313:  beq.s      IL_733a

    IL_7315:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_731a:  ldloc.s    V_21
    IL_731c:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_7321:  box        [mscorlib]System.Int32
    IL_7326:  ldloc.s    V_21
    IL_7328:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_732d:  box        [mscorlib]System.Int32
    IL_7332:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7337:  ldc.i4.1
    IL_7338:  stloc.s    V_20
    IL_733a:  ldloc.s    V_21
    IL_733c:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_7341:  ldloc.s    V_21
    IL_7343:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_7348:  beq.s      IL_736f

    IL_734a:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_734f:  ldloc.s    V_21
    IL_7351:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_7356:  box        [mscorlib]System.Int64
    IL_735b:  ldloc.s    V_21
    IL_735d:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_7362:  box        [mscorlib]System.Int64
    IL_7367:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_736c:  ldc.i4.1
    IL_736d:  stloc.s    V_20
    IL_736f:  ldsfld     int64 IntrinsicTest::temp
    IL_7374:  ldsfld     int64 IntrinsicTest::idmp
    IL_7379:  beq.s      IL_739c

    IL_737b:  ldstr      "temp mismatch {0} {1}"
    IL_7380:  ldsfld     int64 IntrinsicTest::temp
    IL_7385:  box        [mscorlib]System.Int64
    IL_738a:  ldsfld     int64 IntrinsicTest::idmp
    IL_738f:  box        [mscorlib]System.Int64
    IL_7394:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7399:  ldc.i4.1
    IL_739a:  stloc.s    V_20
    IL_739c:  ldsflda    int64 IntrinsicTest::counter64
    IL_73a1:  ldsfld     int64 IntrinsicTest::counter64
    IL_73a6:  ldloc.s    V_6
    IL_73a8:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_73ad:  stsfld     int64 IntrinsicTest::counter64
    IL_73b2:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_73b7:  stsfld     int64 IntrinsicTest::id_counter64
    IL_73bc:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_73c1:  ldloc.s    V_16
    IL_73c3:  bne.un.s   IL_73d1

    IL_73c5:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_73ca:  stsfld     int64 IntrinsicTest::idjunk
    IL_73cf:  br.s       IL_73db

    IL_73d1:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_73d6:  stsfld     int64 IntrinsicTest::idjunk
    IL_73db:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 6 4"
    IL_73e0:  call       void [System.Console]System.Console::WriteLine(string)
    IL_73e5:  ldloc.0
    IL_73e6:  ldloc.s    V_10
    IL_73e8:  beq.s      IL_7404

    IL_73ea:  ldc.i4.1
    IL_73eb:  stloc.s    V_20
    IL_73ed:  ldstr      "te0 check failed {0} {1}"
    IL_73f2:  ldloc.0
    IL_73f3:  box        [mscorlib]System.Int32
    IL_73f8:  ldloc.s    V_10
    IL_73fa:  box        [mscorlib]System.Int32
    IL_73ff:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7404:  ldloc.1
    IL_7405:  ldloc.s    V_11
    IL_7407:  beq.s      IL_7423

    IL_7409:  ldc.i4.1
    IL_740a:  stloc.s    V_20
    IL_740c:  ldstr      "te1 check failed {0} {1}"
    IL_7411:  ldloc.1
    IL_7412:  box        [mscorlib]System.Int32
    IL_7417:  ldloc.s    V_11
    IL_7419:  box        [mscorlib]System.Int32
    IL_741e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7423:  ldloc.2
    IL_7424:  ldloc.s    V_12
    IL_7426:  beq.s      IL_7442

    IL_7428:  ldc.i4.1
    IL_7429:  stloc.s    V_20
    IL_742b:  ldstr      "te2 check failed {0} {1}"
    IL_7430:  ldloc.2
    IL_7431:  box        [mscorlib]System.Int32
    IL_7436:  ldloc.s    V_12
    IL_7438:  box        [mscorlib]System.Int32
    IL_743d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7442:  ldloc.3
    IL_7443:  ldloc.s    V_13
    IL_7445:  beq.s      IL_7461

    IL_7447:  ldc.i4.1
    IL_7448:  stloc.s    V_20
    IL_744a:  ldstr      "te3 check failed {0} {1}"
    IL_744f:  ldloc.3
    IL_7450:  box        [mscorlib]System.Int32
    IL_7455:  ldloc.s    V_13
    IL_7457:  box        [mscorlib]System.Int32
    IL_745c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7461:  ldloc.s    V_4
    IL_7463:  ldloc.s    V_14
    IL_7465:  beq.s      IL_7482

    IL_7467:  ldc.i4.1
    IL_7468:  stloc.s    V_20
    IL_746a:  ldstr      "te064 check failed {0} {1}"
    IL_746f:  ldloc.s    V_4
    IL_7471:  box        [mscorlib]System.Int64
    IL_7476:  ldloc.s    V_14
    IL_7478:  box        [mscorlib]System.Int64
    IL_747d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7482:  ldloc.s    V_5
    IL_7484:  ldloc.s    V_15
    IL_7486:  beq.s      IL_74a3

    IL_7488:  ldc.i4.1
    IL_7489:  stloc.s    V_20
    IL_748b:  ldstr      "te164 check failed {0} {1}"
    IL_7490:  ldloc.s    V_5
    IL_7492:  box        [mscorlib]System.Int64
    IL_7497:  ldloc.s    V_15
    IL_7499:  box        [mscorlib]System.Int64
    IL_749e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_74a3:  ldloc.s    V_6
    IL_74a5:  ldloc.s    V_16
    IL_74a7:  beq.s      IL_74c4

    IL_74a9:  ldc.i4.1
    IL_74aa:  stloc.s    V_20
    IL_74ac:  ldstr      "te264 check failed {0} {1}"
    IL_74b1:  ldloc.s    V_6
    IL_74b3:  box        [mscorlib]System.Int64
    IL_74b8:  ldloc.s    V_16
    IL_74ba:  box        [mscorlib]System.Int64
    IL_74bf:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_74c4:  ldloc.s    V_7
    IL_74c6:  ldloc.s    V_17
    IL_74c8:  beq.s      IL_74e5

    IL_74ca:  ldc.i4.1
    IL_74cb:  stloc.s    V_20
    IL_74cd:  ldstr      "te364 check failed {0} {1}"
    IL_74d2:  ldloc.s    V_7
    IL_74d4:  box        [mscorlib]System.Int64
    IL_74d9:  ldloc.s    V_17
    IL_74db:  box        [mscorlib]System.Int64
    IL_74e0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_74e5:  ldloc.s    V_8
    IL_74e7:  ldloc.s    V_18
    IL_74e9:  beq.s      IL_7506

    IL_74eb:  ldc.i4.1
    IL_74ec:  stloc.s    V_20
    IL_74ee:  ldstr      "dummy check failed {0} {1}"
    IL_74f3:  ldloc.s    V_8
    IL_74f5:  box        [mscorlib]System.Int32
    IL_74fa:  ldloc.s    V_18
    IL_74fc:  box        [mscorlib]System.Int32
    IL_7501:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7506:  ldloc.s    V_9
    IL_7508:  ldloc.s    V_19
    IL_750a:  beq.s      IL_7527

    IL_750c:  ldc.i4.1
    IL_750d:  stloc.s    V_20
    IL_750f:  ldstr      "dummy64 check failed {0} {1}"
    IL_7514:  ldloc.s    V_9
    IL_7516:  box        [mscorlib]System.Int64
    IL_751b:  ldloc.s    V_19
    IL_751d:  box        [mscorlib]System.Int64
    IL_7522:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7527:  ldsfld     int32 IntrinsicTest::counter
    IL_752c:  ldsfld     int32 IntrinsicTest::id_counter
    IL_7531:  beq.s      IL_7554

    IL_7533:  ldstr      "counter mismatch {0} {1}"
    IL_7538:  ldsfld     int32 IntrinsicTest::counter
    IL_753d:  box        [mscorlib]System.Int32
    IL_7542:  ldsfld     int32 IntrinsicTest::id_counter
    IL_7547:  box        [mscorlib]System.Int32
    IL_754c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7551:  ldc.i4.1
    IL_7552:  stloc.s    V_20
    IL_7554:  ldsfld     int64 IntrinsicTest::counter64
    IL_7559:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_755e:  beq.s      IL_7581

    IL_7560:  ldstr      "counter64 mismatch {0} {1}"
    IL_7565:  ldsfld     int64 IntrinsicTest::counter64
    IL_756a:  box        [mscorlib]System.Int64
    IL_756f:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_7574:  box        [mscorlib]System.Int64
    IL_7579:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_757e:  ldc.i4.1
    IL_757f:  stloc.s    V_20
    IL_7581:  ldloc.s    V_21
    IL_7583:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_7588:  ldloc.s    V_21
    IL_758a:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_758f:  beq.s      IL_75b6

    IL_7591:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_7596:  ldloc.s    V_21
    IL_7598:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_759d:  box        [mscorlib]System.Int32
    IL_75a2:  ldloc.s    V_21
    IL_75a4:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_75a9:  box        [mscorlib]System.Int32
    IL_75ae:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_75b3:  ldc.i4.1
    IL_75b4:  stloc.s    V_20
    IL_75b6:  ldloc.s    V_21
    IL_75b8:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_75bd:  ldloc.s    V_21
    IL_75bf:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_75c4:  beq.s      IL_75eb

    IL_75c6:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_75cb:  ldloc.s    V_21
    IL_75cd:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_75d2:  box        [mscorlib]System.Int64
    IL_75d7:  ldloc.s    V_21
    IL_75d9:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_75de:  box        [mscorlib]System.Int64
    IL_75e3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_75e8:  ldc.i4.1
    IL_75e9:  stloc.s    V_20
    IL_75eb:  ldsfld     int64 IntrinsicTest::temp
    IL_75f0:  ldsfld     int64 IntrinsicTest::idmp
    IL_75f5:  beq.s      IL_7618

    IL_75f7:  ldstr      "temp mismatch {0} {1}"
    IL_75fc:  ldsfld     int64 IntrinsicTest::temp
    IL_7601:  box        [mscorlib]System.Int64
    IL_7606:  ldsfld     int64 IntrinsicTest::idmp
    IL_760b:  box        [mscorlib]System.Int64
    IL_7610:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7615:  ldc.i4.1
    IL_7616:  stloc.s    V_20
    IL_7618:  ldloc.s    V_21
    IL_761a:  ldsflda    int64 IntrinsicTest::counter64
    IL_761f:  ldsfld     int64 IntrinsicTest::counter64
    IL_7624:  ldloc.s    V_6
    IL_7626:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_762b:  stfld      int64 IntrinsicTest::instanceCounter64
    IL_7630:  ldloc.s    V_21
    IL_7632:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_7637:  stfld      int64 IntrinsicTest::id_instanceCounter64
    IL_763c:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_7641:  ldloc.s    V_16
    IL_7643:  bne.un.s   IL_7651

    IL_7645:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_764a:  stsfld     int64 IntrinsicTest::id_counter64
    IL_764f:  br.s       IL_765b

    IL_7651:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_7656:  stsfld     int64 IntrinsicTest::idjunk
    IL_765b:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 6 5"
    IL_7660:  call       void [System.Console]System.Console::WriteLine(string)
    IL_7665:  ldloc.0
    IL_7666:  ldloc.s    V_10
    IL_7668:  beq.s      IL_7684

    IL_766a:  ldc.i4.1
    IL_766b:  stloc.s    V_20
    IL_766d:  ldstr      "te0 check failed {0} {1}"
    IL_7672:  ldloc.0
    IL_7673:  box        [mscorlib]System.Int32
    IL_7678:  ldloc.s    V_10
    IL_767a:  box        [mscorlib]System.Int32
    IL_767f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7684:  ldloc.1
    IL_7685:  ldloc.s    V_11
    IL_7687:  beq.s      IL_76a3

    IL_7689:  ldc.i4.1
    IL_768a:  stloc.s    V_20
    IL_768c:  ldstr      "te1 check failed {0} {1}"
    IL_7691:  ldloc.1
    IL_7692:  box        [mscorlib]System.Int32
    IL_7697:  ldloc.s    V_11
    IL_7699:  box        [mscorlib]System.Int32
    IL_769e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_76a3:  ldloc.2
    IL_76a4:  ldloc.s    V_12
    IL_76a6:  beq.s      IL_76c2

    IL_76a8:  ldc.i4.1
    IL_76a9:  stloc.s    V_20
    IL_76ab:  ldstr      "te2 check failed {0} {1}"
    IL_76b0:  ldloc.2
    IL_76b1:  box        [mscorlib]System.Int32
    IL_76b6:  ldloc.s    V_12
    IL_76b8:  box        [mscorlib]System.Int32
    IL_76bd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_76c2:  ldloc.3
    IL_76c3:  ldloc.s    V_13
    IL_76c5:  beq.s      IL_76e1

    IL_76c7:  ldc.i4.1
    IL_76c8:  stloc.s    V_20
    IL_76ca:  ldstr      "te3 check failed {0} {1}"
    IL_76cf:  ldloc.3
    IL_76d0:  box        [mscorlib]System.Int32
    IL_76d5:  ldloc.s    V_13
    IL_76d7:  box        [mscorlib]System.Int32
    IL_76dc:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_76e1:  ldloc.s    V_4
    IL_76e3:  ldloc.s    V_14
    IL_76e5:  beq.s      IL_7702

    IL_76e7:  ldc.i4.1
    IL_76e8:  stloc.s    V_20
    IL_76ea:  ldstr      "te064 check failed {0} {1}"
    IL_76ef:  ldloc.s    V_4
    IL_76f1:  box        [mscorlib]System.Int64
    IL_76f6:  ldloc.s    V_14
    IL_76f8:  box        [mscorlib]System.Int64
    IL_76fd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7702:  ldloc.s    V_5
    IL_7704:  ldloc.s    V_15
    IL_7706:  beq.s      IL_7723

    IL_7708:  ldc.i4.1
    IL_7709:  stloc.s    V_20
    IL_770b:  ldstr      "te164 check failed {0} {1}"
    IL_7710:  ldloc.s    V_5
    IL_7712:  box        [mscorlib]System.Int64
    IL_7717:  ldloc.s    V_15
    IL_7719:  box        [mscorlib]System.Int64
    IL_771e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7723:  ldloc.s    V_6
    IL_7725:  ldloc.s    V_16
    IL_7727:  beq.s      IL_7744

    IL_7729:  ldc.i4.1
    IL_772a:  stloc.s    V_20
    IL_772c:  ldstr      "te264 check failed {0} {1}"
    IL_7731:  ldloc.s    V_6
    IL_7733:  box        [mscorlib]System.Int64
    IL_7738:  ldloc.s    V_16
    IL_773a:  box        [mscorlib]System.Int64
    IL_773f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7744:  ldloc.s    V_7
    IL_7746:  ldloc.s    V_17
    IL_7748:  beq.s      IL_7765

    IL_774a:  ldc.i4.1
    IL_774b:  stloc.s    V_20
    IL_774d:  ldstr      "te364 check failed {0} {1}"
    IL_7752:  ldloc.s    V_7
    IL_7754:  box        [mscorlib]System.Int64
    IL_7759:  ldloc.s    V_17
    IL_775b:  box        [mscorlib]System.Int64
    IL_7760:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7765:  ldloc.s    V_8
    IL_7767:  ldloc.s    V_18
    IL_7769:  beq.s      IL_7786

    IL_776b:  ldc.i4.1
    IL_776c:  stloc.s    V_20
    IL_776e:  ldstr      "dummy check failed {0} {1}"
    IL_7773:  ldloc.s    V_8
    IL_7775:  box        [mscorlib]System.Int32
    IL_777a:  ldloc.s    V_18
    IL_777c:  box        [mscorlib]System.Int32
    IL_7781:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7786:  ldloc.s    V_9
    IL_7788:  ldloc.s    V_19
    IL_778a:  beq.s      IL_77a7

    IL_778c:  ldc.i4.1
    IL_778d:  stloc.s    V_20
    IL_778f:  ldstr      "dummy64 check failed {0} {1}"
    IL_7794:  ldloc.s    V_9
    IL_7796:  box        [mscorlib]System.Int64
    IL_779b:  ldloc.s    V_19
    IL_779d:  box        [mscorlib]System.Int64
    IL_77a2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_77a7:  ldsfld     int32 IntrinsicTest::counter
    IL_77ac:  ldsfld     int32 IntrinsicTest::id_counter
    IL_77b1:  beq.s      IL_77d4

    IL_77b3:  ldstr      "counter mismatch {0} {1}"
    IL_77b8:  ldsfld     int32 IntrinsicTest::counter
    IL_77bd:  box        [mscorlib]System.Int32
    IL_77c2:  ldsfld     int32 IntrinsicTest::id_counter
    IL_77c7:  box        [mscorlib]System.Int32
    IL_77cc:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_77d1:  ldc.i4.1
    IL_77d2:  stloc.s    V_20
    IL_77d4:  ldsfld     int64 IntrinsicTest::counter64
    IL_77d9:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_77de:  beq.s      IL_7801

    IL_77e0:  ldstr      "counter64 mismatch {0} {1}"
    IL_77e5:  ldsfld     int64 IntrinsicTest::counter64
    IL_77ea:  box        [mscorlib]System.Int64
    IL_77ef:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_77f4:  box        [mscorlib]System.Int64
    IL_77f9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_77fe:  ldc.i4.1
    IL_77ff:  stloc.s    V_20
    IL_7801:  ldloc.s    V_21
    IL_7803:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_7808:  ldloc.s    V_21
    IL_780a:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_780f:  beq.s      IL_7836

    IL_7811:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_7816:  ldloc.s    V_21
    IL_7818:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_781d:  box        [mscorlib]System.Int32
    IL_7822:  ldloc.s    V_21
    IL_7824:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_7829:  box        [mscorlib]System.Int32
    IL_782e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7833:  ldc.i4.1
    IL_7834:  stloc.s    V_20
    IL_7836:  ldloc.s    V_21
    IL_7838:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_783d:  ldloc.s    V_21
    IL_783f:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_7844:  beq.s      IL_786b

    IL_7846:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_784b:  ldloc.s    V_21
    IL_784d:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_7852:  box        [mscorlib]System.Int64
    IL_7857:  ldloc.s    V_21
    IL_7859:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_785e:  box        [mscorlib]System.Int64
    IL_7863:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7868:  ldc.i4.1
    IL_7869:  stloc.s    V_20
    IL_786b:  ldsfld     int64 IntrinsicTest::temp
    IL_7870:  ldsfld     int64 IntrinsicTest::idmp
    IL_7875:  beq.s      IL_7898

    IL_7877:  ldstr      "temp mismatch {0} {1}"
    IL_787c:  ldsfld     int64 IntrinsicTest::temp
    IL_7881:  box        [mscorlib]System.Int64
    IL_7886:  ldsfld     int64 IntrinsicTest::idmp
    IL_788b:  box        [mscorlib]System.Int64
    IL_7890:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7895:  ldc.i4.1
    IL_7896:  stloc.s    V_20
    IL_7898:  ldsflda    int64 IntrinsicTest::counter64
    IL_789d:  ldsfld     int64 IntrinsicTest::counter64
    IL_78a2:  ldloc.s    V_6
    IL_78a4:  call       int64 [mscorlib]System.Threading.Interlocked::CompareExchange(int64&,
                                                                                       int64,
                                                                                       int64)
    IL_78a9:  stloc.s    V_7
    IL_78ab:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_78b0:  stloc.s    V_17
    IL_78b2:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_78b7:  ldloc.s    V_16
    IL_78b9:  bne.un.s   IL_78c7

    IL_78bb:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_78c0:  stsfld     int64 IntrinsicTest::id_counter64
    IL_78c5:  br.s       IL_78d1

    IL_78c7:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_78cc:  stsfld     int64 IntrinsicTest::idjunk
    IL_78d1:  ldstr      "------------------------------------CMPXCHG 64 1 1"
    + " 6 6"
    IL_78d6:  call       void [System.Console]System.Console::WriteLine(string)
    IL_78db:  ldloc.0
    IL_78dc:  ldloc.s    V_10
    IL_78de:  beq.s      IL_78fa

    IL_78e0:  ldc.i4.1
    IL_78e1:  stloc.s    V_20
    IL_78e3:  ldstr      "te0 check failed {0} {1}"
    IL_78e8:  ldloc.0
    IL_78e9:  box        [mscorlib]System.Int32
    IL_78ee:  ldloc.s    V_10
    IL_78f0:  box        [mscorlib]System.Int32
    IL_78f5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_78fa:  ldloc.1
    IL_78fb:  ldloc.s    V_11
    IL_78fd:  beq.s      IL_7919

    IL_78ff:  ldc.i4.1
    IL_7900:  stloc.s    V_20
    IL_7902:  ldstr      "te1 check failed {0} {1}"
    IL_7907:  ldloc.1
    IL_7908:  box        [mscorlib]System.Int32
    IL_790d:  ldloc.s    V_11
    IL_790f:  box        [mscorlib]System.Int32
    IL_7914:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7919:  ldloc.2
    IL_791a:  ldloc.s    V_12
    IL_791c:  beq.s      IL_7938

    IL_791e:  ldc.i4.1
    IL_791f:  stloc.s    V_20
    IL_7921:  ldstr      "te2 check failed {0} {1}"
    IL_7926:  ldloc.2
    IL_7927:  box        [mscorlib]System.Int32
    IL_792c:  ldloc.s    V_12
    IL_792e:  box        [mscorlib]System.Int32
    IL_7933:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7938:  ldloc.3
    IL_7939:  ldloc.s    V_13
    IL_793b:  beq.s      IL_7957

    IL_793d:  ldc.i4.1
    IL_793e:  stloc.s    V_20
    IL_7940:  ldstr      "te3 check failed {0} {1}"
    IL_7945:  ldloc.3
    IL_7946:  box        [mscorlib]System.Int32
    IL_794b:  ldloc.s    V_13
    IL_794d:  box        [mscorlib]System.Int32
    IL_7952:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7957:  ldloc.s    V_4
    IL_7959:  ldloc.s    V_14
    IL_795b:  beq.s      IL_7978

    IL_795d:  ldc.i4.1
    IL_795e:  stloc.s    V_20
    IL_7960:  ldstr      "te064 check failed {0} {1}"
    IL_7965:  ldloc.s    V_4
    IL_7967:  box        [mscorlib]System.Int64
    IL_796c:  ldloc.s    V_14
    IL_796e:  box        [mscorlib]System.Int64
    IL_7973:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7978:  ldloc.s    V_5
    IL_797a:  ldloc.s    V_15
    IL_797c:  beq.s      IL_7999

    IL_797e:  ldc.i4.1
    IL_797f:  stloc.s    V_20
    IL_7981:  ldstr      "te164 check failed {0} {1}"
    IL_7986:  ldloc.s    V_5
    IL_7988:  box        [mscorlib]System.Int64
    IL_798d:  ldloc.s    V_15
    IL_798f:  box        [mscorlib]System.Int64
    IL_7994:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7999:  ldloc.s    V_6
    IL_799b:  ldloc.s    V_16
    IL_799d:  beq.s      IL_79ba

    IL_799f:  ldc.i4.1
    IL_79a0:  stloc.s    V_20
    IL_79a2:  ldstr      "te264 check failed {0} {1}"
    IL_79a7:  ldloc.s    V_6
    IL_79a9:  box        [mscorlib]System.Int64
    IL_79ae:  ldloc.s    V_16
    IL_79b0:  box        [mscorlib]System.Int64
    IL_79b5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_79ba:  ldloc.s    V_7
    IL_79bc:  ldloc.s    V_17
    IL_79be:  beq.s      IL_79db

    IL_79c0:  ldc.i4.1
    IL_79c1:  stloc.s    V_20
    IL_79c3:  ldstr      "te364 check failed {0} {1}"
    IL_79c8:  ldloc.s    V_7
    IL_79ca:  box        [mscorlib]System.Int64
    IL_79cf:  ldloc.s    V_17
    IL_79d1:  box        [mscorlib]System.Int64
    IL_79d6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_79db:  ldloc.s    V_8
    IL_79dd:  ldloc.s    V_18
    IL_79df:  beq.s      IL_79fc

    IL_79e1:  ldc.i4.1
    IL_79e2:  stloc.s    V_20
    IL_79e4:  ldstr      "dummy check failed {0} {1}"
    IL_79e9:  ldloc.s    V_8
    IL_79eb:  box        [mscorlib]System.Int32
    IL_79f0:  ldloc.s    V_18
    IL_79f2:  box        [mscorlib]System.Int32
    IL_79f7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_79fc:  ldloc.s    V_9
    IL_79fe:  ldloc.s    V_19
    IL_7a00:  beq.s      IL_7a1d

    IL_7a02:  ldc.i4.1
    IL_7a03:  stloc.s    V_20
    IL_7a05:  ldstr      "dummy64 check failed {0} {1}"
    IL_7a0a:  ldloc.s    V_9
    IL_7a0c:  box        [mscorlib]System.Int64
    IL_7a11:  ldloc.s    V_19
    IL_7a13:  box        [mscorlib]System.Int64
    IL_7a18:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7a1d:  ldsfld     int32 IntrinsicTest::counter
    IL_7a22:  ldsfld     int32 IntrinsicTest::id_counter
    IL_7a27:  beq.s      IL_7a4a

    IL_7a29:  ldstr      "counter mismatch {0} {1}"
    IL_7a2e:  ldsfld     int32 IntrinsicTest::counter
    IL_7a33:  box        [mscorlib]System.Int32
    IL_7a38:  ldsfld     int32 IntrinsicTest::id_counter
    IL_7a3d:  box        [mscorlib]System.Int32
    IL_7a42:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7a47:  ldc.i4.1
    IL_7a48:  stloc.s    V_20
    IL_7a4a:  ldsfld     int64 IntrinsicTest::counter64
    IL_7a4f:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_7a54:  beq.s      IL_7a77

    IL_7a56:  ldstr      "counter64 mismatch {0} {1}"
    IL_7a5b:  ldsfld     int64 IntrinsicTest::counter64
    IL_7a60:  box        [mscorlib]System.Int64
    IL_7a65:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_7a6a:  box        [mscorlib]System.Int64
    IL_7a6f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7a74:  ldc.i4.1
    IL_7a75:  stloc.s    V_20
    IL_7a77:  ldloc.s    V_21
    IL_7a79:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_7a7e:  ldloc.s    V_21
    IL_7a80:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_7a85:  beq.s      IL_7aac

    IL_7a87:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_7a8c:  ldloc.s    V_21
    IL_7a8e:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_7a93:  box        [mscorlib]System.Int32
    IL_7a98:  ldloc.s    V_21
    IL_7a9a:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_7a9f:  box        [mscorlib]System.Int32
    IL_7aa4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7aa9:  ldc.i4.1
    IL_7aaa:  stloc.s    V_20
    IL_7aac:  ldloc.s    V_21
    IL_7aae:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_7ab3:  ldloc.s    V_21
    IL_7ab5:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_7aba:  beq.s      IL_7ae1

    IL_7abc:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_7ac1:  ldloc.s    V_21
    IL_7ac3:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_7ac8:  box        [mscorlib]System.Int64
    IL_7acd:  ldloc.s    V_21
    IL_7acf:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_7ad4:  box        [mscorlib]System.Int64
    IL_7ad9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7ade:  ldc.i4.1
    IL_7adf:  stloc.s    V_20
    IL_7ae1:  ldsfld     int64 IntrinsicTest::temp
    IL_7ae6:  ldsfld     int64 IntrinsicTest::idmp
    IL_7aeb:  beq.s      IL_7b0e

    IL_7aed:  ldstr      "temp mismatch {0} {1}"
    IL_7af2:  ldsfld     int64 IntrinsicTest::temp
    IL_7af7:  box        [mscorlib]System.Int64
    IL_7afc:  ldsfld     int64 IntrinsicTest::idmp
    IL_7b01:  box        [mscorlib]System.Int64
    IL_7b06:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7b0b:  ldc.i4.1
    IL_7b0c:  stloc.s    V_20
    IL_7b0e:  ldloc.s    V_20
    IL_7b10:  ret
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



