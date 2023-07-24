




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
.assembly 'rva_rvastatic2'
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
    IL_009c:  ldsflda    int32 IntrinsicTest::counter
    IL_00a1:  ldsfld     int32 IntrinsicTest::counter
    IL_00a6:  ldloc.0
    IL_00a7:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_00ac:  stloc.0
    IL_00ad:  ldsfld     int32 IntrinsicTest::id_counter
    IL_00b2:  stloc.s    V_10
    IL_00b4:  ldsfld     int32 IntrinsicTest::id_counter
    IL_00b9:  ldloc.s    V_10
    IL_00bb:  bne.un.s   IL_00c9

    IL_00bd:  ldsfld     int32 IntrinsicTest::id_counter
    IL_00c2:  stsfld     int32 IntrinsicTest::id_counter
    IL_00c7:  br.s       IL_00d4

    IL_00c9:  ldsfld     int32 IntrinsicTest::id_counter
    IL_00ce:  conv.i8
    IL_00cf:  stsfld     int64 IntrinsicTest::idjunk
    IL_00d4:  ldstr      "------------------------------------CMPXCHG  1 1 0 0"
    IL_00d9:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00de:  ldloc.0
    IL_00df:  ldloc.s    V_10
    IL_00e1:  beq.s      IL_00fd

    IL_00e3:  ldc.i4.1
    IL_00e4:  stloc.s    V_20
    IL_00e6:  ldstr      "te0 check failed {0} {1}"
    IL_00eb:  ldloc.0
    IL_00ec:  box        [mscorlib]System.Int32
    IL_00f1:  ldloc.s    V_10
    IL_00f3:  box        [mscorlib]System.Int32
    IL_00f8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_00fd:  ldloc.1
    IL_00fe:  ldloc.s    V_11
    IL_0100:  beq.s      IL_011c

    IL_0102:  ldc.i4.1
    IL_0103:  stloc.s    V_20
    IL_0105:  ldstr      "te1 check failed {0} {1}"
    IL_010a:  ldloc.1
    IL_010b:  box        [mscorlib]System.Int32
    IL_0110:  ldloc.s    V_11
    IL_0112:  box        [mscorlib]System.Int32
    IL_0117:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_011c:  ldloc.2
    IL_011d:  ldloc.s    V_12
    IL_011f:  beq.s      IL_013b

    IL_0121:  ldc.i4.1
    IL_0122:  stloc.s    V_20
    IL_0124:  ldstr      "te2 check failed {0} {1}"
    IL_0129:  ldloc.2
    IL_012a:  box        [mscorlib]System.Int32
    IL_012f:  ldloc.s    V_12
    IL_0131:  box        [mscorlib]System.Int32
    IL_0136:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_013b:  ldloc.3
    IL_013c:  ldloc.s    V_13
    IL_013e:  beq.s      IL_015a

    IL_0140:  ldc.i4.1
    IL_0141:  stloc.s    V_20
    IL_0143:  ldstr      "te3 check failed {0} {1}"
    IL_0148:  ldloc.3
    IL_0149:  box        [mscorlib]System.Int32
    IL_014e:  ldloc.s    V_13
    IL_0150:  box        [mscorlib]System.Int32
    IL_0155:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_015a:  ldloc.s    V_4
    IL_015c:  ldloc.s    V_14
    IL_015e:  beq.s      IL_017b

    IL_0160:  ldc.i4.1
    IL_0161:  stloc.s    V_20
    IL_0163:  ldstr      "te064 check failed {0} {1}"
    IL_0168:  ldloc.s    V_4
    IL_016a:  box        [mscorlib]System.Int64
    IL_016f:  ldloc.s    V_14
    IL_0171:  box        [mscorlib]System.Int64
    IL_0176:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_017b:  ldloc.s    V_5
    IL_017d:  ldloc.s    V_15
    IL_017f:  beq.s      IL_019c

    IL_0181:  ldc.i4.1
    IL_0182:  stloc.s    V_20
    IL_0184:  ldstr      "te164 check failed {0} {1}"
    IL_0189:  ldloc.s    V_5
    IL_018b:  box        [mscorlib]System.Int64
    IL_0190:  ldloc.s    V_15
    IL_0192:  box        [mscorlib]System.Int64
    IL_0197:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_019c:  ldloc.s    V_6
    IL_019e:  ldloc.s    V_16
    IL_01a0:  beq.s      IL_01bd

    IL_01a2:  ldc.i4.1
    IL_01a3:  stloc.s    V_20
    IL_01a5:  ldstr      "te264 check failed {0} {1}"
    IL_01aa:  ldloc.s    V_6
    IL_01ac:  box        [mscorlib]System.Int64
    IL_01b1:  ldloc.s    V_16
    IL_01b3:  box        [mscorlib]System.Int64
    IL_01b8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_01bd:  ldloc.s    V_7
    IL_01bf:  ldloc.s    V_17
    IL_01c1:  beq.s      IL_01de

    IL_01c3:  ldc.i4.1
    IL_01c4:  stloc.s    V_20
    IL_01c6:  ldstr      "te364 check failed {0} {1}"
    IL_01cb:  ldloc.s    V_7
    IL_01cd:  box        [mscorlib]System.Int64
    IL_01d2:  ldloc.s    V_17
    IL_01d4:  box        [mscorlib]System.Int64
    IL_01d9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_01de:  ldloc.s    V_8
    IL_01e0:  ldloc.s    V_18
    IL_01e2:  beq.s      IL_01ff

    IL_01e4:  ldc.i4.1
    IL_01e5:  stloc.s    V_20
    IL_01e7:  ldstr      "dummy check failed {0} {1}"
    IL_01ec:  ldloc.s    V_8
    IL_01ee:  box        [mscorlib]System.Int32
    IL_01f3:  ldloc.s    V_18
    IL_01f5:  box        [mscorlib]System.Int32
    IL_01fa:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_01ff:  ldloc.s    V_9
    IL_0201:  ldloc.s    V_19
    IL_0203:  beq.s      IL_0220

    IL_0205:  ldc.i4.1
    IL_0206:  stloc.s    V_20
    IL_0208:  ldstr      "dummy64 check failed {0} {1}"
    IL_020d:  ldloc.s    V_9
    IL_020f:  box        [mscorlib]System.Int64
    IL_0214:  ldloc.s    V_19
    IL_0216:  box        [mscorlib]System.Int64
    IL_021b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0220:  ldsfld     int32 IntrinsicTest::counter
    IL_0225:  ldsfld     int32 IntrinsicTest::id_counter
    IL_022a:  beq.s      IL_024d

    IL_022c:  ldstr      "counter mismatch {0} {1}"
    IL_0231:  ldsfld     int32 IntrinsicTest::counter
    IL_0236:  box        [mscorlib]System.Int32
    IL_023b:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0240:  box        [mscorlib]System.Int32
    IL_0245:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_024a:  ldc.i4.1
    IL_024b:  stloc.s    V_20
    IL_024d:  ldsfld     int64 IntrinsicTest::counter64
    IL_0252:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_0257:  beq.s      IL_027a

    IL_0259:  ldstr      "counter64 mismatch {0} {1}"
    IL_025e:  ldsfld     int64 IntrinsicTest::counter64
    IL_0263:  box        [mscorlib]System.Int64
    IL_0268:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_026d:  box        [mscorlib]System.Int64
    IL_0272:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0277:  ldc.i4.1
    IL_0278:  stloc.s    V_20
    IL_027a:  ldloc.s    V_21
    IL_027c:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_0281:  ldloc.s    V_21
    IL_0283:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_0288:  beq.s      IL_02af

    IL_028a:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_028f:  ldloc.s    V_21
    IL_0291:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_0296:  box        [mscorlib]System.Int32
    IL_029b:  ldloc.s    V_21
    IL_029d:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_02a2:  box        [mscorlib]System.Int32
    IL_02a7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_02ac:  ldc.i4.1
    IL_02ad:  stloc.s    V_20
    IL_02af:  ldloc.s    V_21
    IL_02b1:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_02b6:  ldloc.s    V_21
    IL_02b8:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_02bd:  beq.s      IL_02e4

    IL_02bf:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_02c4:  ldloc.s    V_21
    IL_02c6:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_02cb:  box        [mscorlib]System.Int64
    IL_02d0:  ldloc.s    V_21
    IL_02d2:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_02d7:  box        [mscorlib]System.Int64
    IL_02dc:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_02e1:  ldc.i4.1
    IL_02e2:  stloc.s    V_20
    IL_02e4:  ldsfld     int64 IntrinsicTest::temp
    IL_02e9:  ldsfld     int64 IntrinsicTest::idmp
    IL_02ee:  beq.s      IL_0311

    IL_02f0:  ldstr      "temp mismatch {0} {1}"
    IL_02f5:  ldsfld     int64 IntrinsicTest::temp
    IL_02fa:  box        [mscorlib]System.Int64
    IL_02ff:  ldsfld     int64 IntrinsicTest::idmp
    IL_0304:  box        [mscorlib]System.Int64
    IL_0309:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_030e:  ldc.i4.1
    IL_030f:  stloc.s    V_20
    IL_0311:  ldsflda    int32 IntrinsicTest::counter
    IL_0316:  ldsfld     int32 IntrinsicTest::counter
    IL_031b:  ldloc.0
    IL_031c:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_0321:  stsfld     int32 IntrinsicTest::counter
    IL_0326:  ldsfld     int32 IntrinsicTest::id_counter
    IL_032b:  stsfld     int32 IntrinsicTest::id_counter
    IL_0330:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0335:  ldloc.s    V_10
    IL_0337:  bne.un.s   IL_0346

    IL_0339:  ldsfld     int32 IntrinsicTest::id_counter
    IL_033e:  conv.i8
    IL_033f:  stsfld     int64 IntrinsicTest::idjunk
    IL_0344:  br.s       IL_0351

    IL_0346:  ldsfld     int32 IntrinsicTest::id_counter
    IL_034b:  conv.i8
    IL_034c:  stsfld     int64 IntrinsicTest::idjunk
    IL_0351:  ldstr      "------------------------------------CMPXCHG  1 1 0 1"
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
    IL_0590:  ldsflda    int32 IntrinsicTest::counter
    IL_0595:  ldsfld     int32 IntrinsicTest::counter
    IL_059a:  ldloc.0
    IL_059b:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_05a0:  stfld      int32 IntrinsicTest::instanceCounter
    IL_05a5:  ldloc.s    V_21
    IL_05a7:  ldsfld     int32 IntrinsicTest::id_counter
    IL_05ac:  stfld      int32 IntrinsicTest::id_instanceCounter
    IL_05b1:  ldsfld     int32 IntrinsicTest::id_counter
    IL_05b6:  ldloc.s    V_10
    IL_05b8:  bne.un.s   IL_05c6

    IL_05ba:  ldsfld     int32 IntrinsicTest::id_counter
    IL_05bf:  stsfld     int32 IntrinsicTest::id_counter
    IL_05c4:  br.s       IL_05d1

    IL_05c6:  ldsfld     int32 IntrinsicTest::id_counter
    IL_05cb:  conv.i8
    IL_05cc:  stsfld     int64 IntrinsicTest::idjunk
    IL_05d1:  ldstr      "------------------------------------CMPXCHG  1 1 0 2"
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
    IL_080e:  ldsflda    int32 IntrinsicTest::counter
    IL_0813:  ldsfld     int32 IntrinsicTest::counter
    IL_0818:  ldloc.0
    IL_0819:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_081e:  stloc.0
    IL_081f:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0824:  stloc.s    V_10
    IL_0826:  ldsfld     int32 IntrinsicTest::id_counter
    IL_082b:  ldloc.s    V_10
    IL_082d:  bne.un.s   IL_083b

    IL_082f:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0834:  stsfld     int32 IntrinsicTest::id_counter
    IL_0839:  br.s       IL_0846

    IL_083b:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0840:  conv.i8
    IL_0841:  stsfld     int64 IntrinsicTest::idjunk
    IL_0846:  ldstr      "------------------------------------CMPXCHG  1 1 0 3"
    IL_084b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0850:  ldloc.0
    IL_0851:  ldloc.s    V_10
    IL_0853:  beq.s      IL_086f

    IL_0855:  ldc.i4.1
    IL_0856:  stloc.s    V_20
    IL_0858:  ldstr      "te0 check failed {0} {1}"
    IL_085d:  ldloc.0
    IL_085e:  box        [mscorlib]System.Int32
    IL_0863:  ldloc.s    V_10
    IL_0865:  box        [mscorlib]System.Int32
    IL_086a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_086f:  ldloc.1
    IL_0870:  ldloc.s    V_11
    IL_0872:  beq.s      IL_088e

    IL_0874:  ldc.i4.1
    IL_0875:  stloc.s    V_20
    IL_0877:  ldstr      "te1 check failed {0} {1}"
    IL_087c:  ldloc.1
    IL_087d:  box        [mscorlib]System.Int32
    IL_0882:  ldloc.s    V_11
    IL_0884:  box        [mscorlib]System.Int32
    IL_0889:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_088e:  ldloc.2
    IL_088f:  ldloc.s    V_12
    IL_0891:  beq.s      IL_08ad

    IL_0893:  ldc.i4.1
    IL_0894:  stloc.s    V_20
    IL_0896:  ldstr      "te2 check failed {0} {1}"
    IL_089b:  ldloc.2
    IL_089c:  box        [mscorlib]System.Int32
    IL_08a1:  ldloc.s    V_12
    IL_08a3:  box        [mscorlib]System.Int32
    IL_08a8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_08ad:  ldloc.3
    IL_08ae:  ldloc.s    V_13
    IL_08b0:  beq.s      IL_08cc

    IL_08b2:  ldc.i4.1
    IL_08b3:  stloc.s    V_20
    IL_08b5:  ldstr      "te3 check failed {0} {1}"
    IL_08ba:  ldloc.3
    IL_08bb:  box        [mscorlib]System.Int32
    IL_08c0:  ldloc.s    V_13
    IL_08c2:  box        [mscorlib]System.Int32
    IL_08c7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_08cc:  ldloc.s    V_4
    IL_08ce:  ldloc.s    V_14
    IL_08d0:  beq.s      IL_08ed

    IL_08d2:  ldc.i4.1
    IL_08d3:  stloc.s    V_20
    IL_08d5:  ldstr      "te064 check failed {0} {1}"
    IL_08da:  ldloc.s    V_4
    IL_08dc:  box        [mscorlib]System.Int64
    IL_08e1:  ldloc.s    V_14
    IL_08e3:  box        [mscorlib]System.Int64
    IL_08e8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_08ed:  ldloc.s    V_5
    IL_08ef:  ldloc.s    V_15
    IL_08f1:  beq.s      IL_090e

    IL_08f3:  ldc.i4.1
    IL_08f4:  stloc.s    V_20
    IL_08f6:  ldstr      "te164 check failed {0} {1}"
    IL_08fb:  ldloc.s    V_5
    IL_08fd:  box        [mscorlib]System.Int64
    IL_0902:  ldloc.s    V_15
    IL_0904:  box        [mscorlib]System.Int64
    IL_0909:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_090e:  ldloc.s    V_6
    IL_0910:  ldloc.s    V_16
    IL_0912:  beq.s      IL_092f

    IL_0914:  ldc.i4.1
    IL_0915:  stloc.s    V_20
    IL_0917:  ldstr      "te264 check failed {0} {1}"
    IL_091c:  ldloc.s    V_6
    IL_091e:  box        [mscorlib]System.Int64
    IL_0923:  ldloc.s    V_16
    IL_0925:  box        [mscorlib]System.Int64
    IL_092a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_092f:  ldloc.s    V_7
    IL_0931:  ldloc.s    V_17
    IL_0933:  beq.s      IL_0950

    IL_0935:  ldc.i4.1
    IL_0936:  stloc.s    V_20
    IL_0938:  ldstr      "te364 check failed {0} {1}"
    IL_093d:  ldloc.s    V_7
    IL_093f:  box        [mscorlib]System.Int64
    IL_0944:  ldloc.s    V_17
    IL_0946:  box        [mscorlib]System.Int64
    IL_094b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0950:  ldloc.s    V_8
    IL_0952:  ldloc.s    V_18
    IL_0954:  beq.s      IL_0971

    IL_0956:  ldc.i4.1
    IL_0957:  stloc.s    V_20
    IL_0959:  ldstr      "dummy check failed {0} {1}"
    IL_095e:  ldloc.s    V_8
    IL_0960:  box        [mscorlib]System.Int32
    IL_0965:  ldloc.s    V_18
    IL_0967:  box        [mscorlib]System.Int32
    IL_096c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0971:  ldloc.s    V_9
    IL_0973:  ldloc.s    V_19
    IL_0975:  beq.s      IL_0992

    IL_0977:  ldc.i4.1
    IL_0978:  stloc.s    V_20
    IL_097a:  ldstr      "dummy64 check failed {0} {1}"
    IL_097f:  ldloc.s    V_9
    IL_0981:  box        [mscorlib]System.Int64
    IL_0986:  ldloc.s    V_19
    IL_0988:  box        [mscorlib]System.Int64
    IL_098d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0992:  ldsfld     int32 IntrinsicTest::counter
    IL_0997:  ldsfld     int32 IntrinsicTest::id_counter
    IL_099c:  beq.s      IL_09bf

    IL_099e:  ldstr      "counter mismatch {0} {1}"
    IL_09a3:  ldsfld     int32 IntrinsicTest::counter
    IL_09a8:  box        [mscorlib]System.Int32
    IL_09ad:  ldsfld     int32 IntrinsicTest::id_counter
    IL_09b2:  box        [mscorlib]System.Int32
    IL_09b7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_09bc:  ldc.i4.1
    IL_09bd:  stloc.s    V_20
    IL_09bf:  ldsfld     int64 IntrinsicTest::counter64
    IL_09c4:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_09c9:  beq.s      IL_09ec

    IL_09cb:  ldstr      "counter64 mismatch {0} {1}"
    IL_09d0:  ldsfld     int64 IntrinsicTest::counter64
    IL_09d5:  box        [mscorlib]System.Int64
    IL_09da:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_09df:  box        [mscorlib]System.Int64
    IL_09e4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_09e9:  ldc.i4.1
    IL_09ea:  stloc.s    V_20
    IL_09ec:  ldloc.s    V_21
    IL_09ee:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_09f3:  ldloc.s    V_21
    IL_09f5:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_09fa:  beq.s      IL_0a21

    IL_09fc:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_0a01:  ldloc.s    V_21
    IL_0a03:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_0a08:  box        [mscorlib]System.Int32
    IL_0a0d:  ldloc.s    V_21
    IL_0a0f:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_0a14:  box        [mscorlib]System.Int32
    IL_0a19:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0a1e:  ldc.i4.1
    IL_0a1f:  stloc.s    V_20
    IL_0a21:  ldloc.s    V_21
    IL_0a23:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_0a28:  ldloc.s    V_21
    IL_0a2a:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_0a2f:  beq.s      IL_0a56

    IL_0a31:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_0a36:  ldloc.s    V_21
    IL_0a38:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_0a3d:  box        [mscorlib]System.Int64
    IL_0a42:  ldloc.s    V_21
    IL_0a44:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_0a49:  box        [mscorlib]System.Int64
    IL_0a4e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0a53:  ldc.i4.1
    IL_0a54:  stloc.s    V_20
    IL_0a56:  ldsfld     int64 IntrinsicTest::temp
    IL_0a5b:  ldsfld     int64 IntrinsicTest::idmp
    IL_0a60:  beq.s      IL_0a83

    IL_0a62:  ldstr      "temp mismatch {0} {1}"
    IL_0a67:  ldsfld     int64 IntrinsicTest::temp
    IL_0a6c:  box        [mscorlib]System.Int64
    IL_0a71:  ldsfld     int64 IntrinsicTest::idmp
    IL_0a76:  box        [mscorlib]System.Int64
    IL_0a7b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0a80:  ldc.i4.1
    IL_0a81:  stloc.s    V_20
    IL_0a83:  ldsflda    int32 IntrinsicTest::counter
    IL_0a88:  ldsfld     int32 IntrinsicTest::counter
    IL_0a8d:  ldloc.0
    IL_0a8e:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_0a93:  stsfld     int32 IntrinsicTest::counter
    IL_0a98:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0a9d:  stsfld     int32 IntrinsicTest::id_counter
    IL_0aa2:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0aa7:  ldloc.s    V_10
    IL_0aa9:  bne.un.s   IL_0ab8

    IL_0aab:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0ab0:  conv.i8
    IL_0ab1:  stsfld     int64 IntrinsicTest::idjunk
    IL_0ab6:  br.s       IL_0ac3

    IL_0ab8:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0abd:  conv.i8
    IL_0abe:  stsfld     int64 IntrinsicTest::idjunk
    IL_0ac3:  ldstr      "------------------------------------CMPXCHG  1 1 0 4"
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
    IL_0d02:  ldsflda    int32 IntrinsicTest::counter
    IL_0d07:  ldsfld     int32 IntrinsicTest::counter
    IL_0d0c:  ldloc.0
    IL_0d0d:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_0d12:  stfld      int32 IntrinsicTest::instanceCounter
    IL_0d17:  ldloc.s    V_21
    IL_0d19:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0d1e:  stfld      int32 IntrinsicTest::id_instanceCounter
    IL_0d23:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0d28:  ldloc.s    V_10
    IL_0d2a:  bne.un.s   IL_0d38

    IL_0d2c:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0d31:  stsfld     int32 IntrinsicTest::id_counter
    IL_0d36:  br.s       IL_0d43

    IL_0d38:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0d3d:  conv.i8
    IL_0d3e:  stsfld     int64 IntrinsicTest::idjunk
    IL_0d43:  ldstr      "------------------------------------CMPXCHG  1 1 0 5"
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
    IL_0f80:  ldsflda    int32 IntrinsicTest::counter
    IL_0f85:  ldsfld     int32 IntrinsicTest::counter
    IL_0f8a:  ldloc.0
    IL_0f8b:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_0f90:  stloc.3
    IL_0f91:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0f96:  stloc.s    V_13
    IL_0f98:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0f9d:  ldloc.s    V_10
    IL_0f9f:  bne.un.s   IL_0fad

    IL_0fa1:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0fa6:  stsfld     int32 IntrinsicTest::id_counter
    IL_0fab:  br.s       IL_0fb8

    IL_0fad:  ldsfld     int32 IntrinsicTest::id_counter
    IL_0fb2:  conv.i8
    IL_0fb3:  stsfld     int64 IntrinsicTest::idjunk
    IL_0fb8:  ldstr      "------------------------------------CMPXCHG  1 1 0 6"
    IL_0fbd:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0fc2:  ldloc.0
    IL_0fc3:  ldloc.s    V_10
    IL_0fc5:  beq.s      IL_0fe1

    IL_0fc7:  ldc.i4.1
    IL_0fc8:  stloc.s    V_20
    IL_0fca:  ldstr      "te0 check failed {0} {1}"
    IL_0fcf:  ldloc.0
    IL_0fd0:  box        [mscorlib]System.Int32
    IL_0fd5:  ldloc.s    V_10
    IL_0fd7:  box        [mscorlib]System.Int32
    IL_0fdc:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_0fe1:  ldloc.1
    IL_0fe2:  ldloc.s    V_11
    IL_0fe4:  beq.s      IL_1000

    IL_0fe6:  ldc.i4.1
    IL_0fe7:  stloc.s    V_20
    IL_0fe9:  ldstr      "te1 check failed {0} {1}"
    IL_0fee:  ldloc.1
    IL_0fef:  box        [mscorlib]System.Int32
    IL_0ff4:  ldloc.s    V_11
    IL_0ff6:  box        [mscorlib]System.Int32
    IL_0ffb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1000:  ldloc.2
    IL_1001:  ldloc.s    V_12
    IL_1003:  beq.s      IL_101f

    IL_1005:  ldc.i4.1
    IL_1006:  stloc.s    V_20
    IL_1008:  ldstr      "te2 check failed {0} {1}"
    IL_100d:  ldloc.2
    IL_100e:  box        [mscorlib]System.Int32
    IL_1013:  ldloc.s    V_12
    IL_1015:  box        [mscorlib]System.Int32
    IL_101a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_101f:  ldloc.3
    IL_1020:  ldloc.s    V_13
    IL_1022:  beq.s      IL_103e

    IL_1024:  ldc.i4.1
    IL_1025:  stloc.s    V_20
    IL_1027:  ldstr      "te3 check failed {0} {1}"
    IL_102c:  ldloc.3
    IL_102d:  box        [mscorlib]System.Int32
    IL_1032:  ldloc.s    V_13
    IL_1034:  box        [mscorlib]System.Int32
    IL_1039:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_103e:  ldloc.s    V_4
    IL_1040:  ldloc.s    V_14
    IL_1042:  beq.s      IL_105f

    IL_1044:  ldc.i4.1
    IL_1045:  stloc.s    V_20
    IL_1047:  ldstr      "te064 check failed {0} {1}"
    IL_104c:  ldloc.s    V_4
    IL_104e:  box        [mscorlib]System.Int64
    IL_1053:  ldloc.s    V_14
    IL_1055:  box        [mscorlib]System.Int64
    IL_105a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_105f:  ldloc.s    V_5
    IL_1061:  ldloc.s    V_15
    IL_1063:  beq.s      IL_1080

    IL_1065:  ldc.i4.1
    IL_1066:  stloc.s    V_20
    IL_1068:  ldstr      "te164 check failed {0} {1}"
    IL_106d:  ldloc.s    V_5
    IL_106f:  box        [mscorlib]System.Int64
    IL_1074:  ldloc.s    V_15
    IL_1076:  box        [mscorlib]System.Int64
    IL_107b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1080:  ldloc.s    V_6
    IL_1082:  ldloc.s    V_16
    IL_1084:  beq.s      IL_10a1

    IL_1086:  ldc.i4.1
    IL_1087:  stloc.s    V_20
    IL_1089:  ldstr      "te264 check failed {0} {1}"
    IL_108e:  ldloc.s    V_6
    IL_1090:  box        [mscorlib]System.Int64
    IL_1095:  ldloc.s    V_16
    IL_1097:  box        [mscorlib]System.Int64
    IL_109c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_10a1:  ldloc.s    V_7
    IL_10a3:  ldloc.s    V_17
    IL_10a5:  beq.s      IL_10c2

    IL_10a7:  ldc.i4.1
    IL_10a8:  stloc.s    V_20
    IL_10aa:  ldstr      "te364 check failed {0} {1}"
    IL_10af:  ldloc.s    V_7
    IL_10b1:  box        [mscorlib]System.Int64
    IL_10b6:  ldloc.s    V_17
    IL_10b8:  box        [mscorlib]System.Int64
    IL_10bd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_10c2:  ldloc.s    V_8
    IL_10c4:  ldloc.s    V_18
    IL_10c6:  beq.s      IL_10e3

    IL_10c8:  ldc.i4.1
    IL_10c9:  stloc.s    V_20
    IL_10cb:  ldstr      "dummy check failed {0} {1}"
    IL_10d0:  ldloc.s    V_8
    IL_10d2:  box        [mscorlib]System.Int32
    IL_10d7:  ldloc.s    V_18
    IL_10d9:  box        [mscorlib]System.Int32
    IL_10de:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_10e3:  ldloc.s    V_9
    IL_10e5:  ldloc.s    V_19
    IL_10e7:  beq.s      IL_1104

    IL_10e9:  ldc.i4.1
    IL_10ea:  stloc.s    V_20
    IL_10ec:  ldstr      "dummy64 check failed {0} {1}"
    IL_10f1:  ldloc.s    V_9
    IL_10f3:  box        [mscorlib]System.Int64
    IL_10f8:  ldloc.s    V_19
    IL_10fa:  box        [mscorlib]System.Int64
    IL_10ff:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1104:  ldsfld     int32 IntrinsicTest::counter
    IL_1109:  ldsfld     int32 IntrinsicTest::id_counter
    IL_110e:  beq.s      IL_1131

    IL_1110:  ldstr      "counter mismatch {0} {1}"
    IL_1115:  ldsfld     int32 IntrinsicTest::counter
    IL_111a:  box        [mscorlib]System.Int32
    IL_111f:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1124:  box        [mscorlib]System.Int32
    IL_1129:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_112e:  ldc.i4.1
    IL_112f:  stloc.s    V_20
    IL_1131:  ldsfld     int64 IntrinsicTest::counter64
    IL_1136:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_113b:  beq.s      IL_115e

    IL_113d:  ldstr      "counter64 mismatch {0} {1}"
    IL_1142:  ldsfld     int64 IntrinsicTest::counter64
    IL_1147:  box        [mscorlib]System.Int64
    IL_114c:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1151:  box        [mscorlib]System.Int64
    IL_1156:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_115b:  ldc.i4.1
    IL_115c:  stloc.s    V_20
    IL_115e:  ldloc.s    V_21
    IL_1160:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_1165:  ldloc.s    V_21
    IL_1167:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_116c:  beq.s      IL_1193

    IL_116e:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_1173:  ldloc.s    V_21
    IL_1175:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_117a:  box        [mscorlib]System.Int32
    IL_117f:  ldloc.s    V_21
    IL_1181:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_1186:  box        [mscorlib]System.Int32
    IL_118b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1190:  ldc.i4.1
    IL_1191:  stloc.s    V_20
    IL_1193:  ldloc.s    V_21
    IL_1195:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_119a:  ldloc.s    V_21
    IL_119c:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_11a1:  beq.s      IL_11c8

    IL_11a3:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_11a8:  ldloc.s    V_21
    IL_11aa:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_11af:  box        [mscorlib]System.Int64
    IL_11b4:  ldloc.s    V_21
    IL_11b6:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_11bb:  box        [mscorlib]System.Int64
    IL_11c0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_11c5:  ldc.i4.1
    IL_11c6:  stloc.s    V_20
    IL_11c8:  ldsfld     int64 IntrinsicTest::temp
    IL_11cd:  ldsfld     int64 IntrinsicTest::idmp
    IL_11d2:  beq.s      IL_11f5

    IL_11d4:  ldstr      "temp mismatch {0} {1}"
    IL_11d9:  ldsfld     int64 IntrinsicTest::temp
    IL_11de:  box        [mscorlib]System.Int64
    IL_11e3:  ldsfld     int64 IntrinsicTest::idmp
    IL_11e8:  box        [mscorlib]System.Int64
    IL_11ed:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_11f2:  ldc.i4.1
    IL_11f3:  stloc.s    V_20
    IL_11f5:  ldsflda    int32 IntrinsicTest::counter
    IL_11fa:  ldsfld     int32 IntrinsicTest::counter
    IL_11ff:  ldsfld     int32 IntrinsicTest::counter
    IL_1204:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_1209:  stloc.0
    IL_120a:  ldsfld     int32 IntrinsicTest::id_counter
    IL_120f:  stloc.s    V_10
    IL_1211:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1216:  ldsfld     int32 IntrinsicTest::id_counter
    IL_121b:  bne.un.s   IL_1229

    IL_121d:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1222:  stsfld     int32 IntrinsicTest::id_counter
    IL_1227:  br.s       IL_1234

    IL_1229:  ldsfld     int32 IntrinsicTest::id_counter
    IL_122e:  conv.i8
    IL_122f:  stsfld     int64 IntrinsicTest::idjunk
    IL_1234:  ldstr      "------------------------------------CMPXCHG  1 1 1 0"
    IL_1239:  call       void [System.Console]System.Console::WriteLine(string)
    IL_123e:  ldloc.0
    IL_123f:  ldloc.s    V_10
    IL_1241:  beq.s      IL_125d

    IL_1243:  ldc.i4.1
    IL_1244:  stloc.s    V_20
    IL_1246:  ldstr      "te0 check failed {0} {1}"
    IL_124b:  ldloc.0
    IL_124c:  box        [mscorlib]System.Int32
    IL_1251:  ldloc.s    V_10
    IL_1253:  box        [mscorlib]System.Int32
    IL_1258:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_125d:  ldloc.1
    IL_125e:  ldloc.s    V_11
    IL_1260:  beq.s      IL_127c

    IL_1262:  ldc.i4.1
    IL_1263:  stloc.s    V_20
    IL_1265:  ldstr      "te1 check failed {0} {1}"
    IL_126a:  ldloc.1
    IL_126b:  box        [mscorlib]System.Int32
    IL_1270:  ldloc.s    V_11
    IL_1272:  box        [mscorlib]System.Int32
    IL_1277:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_127c:  ldloc.2
    IL_127d:  ldloc.s    V_12
    IL_127f:  beq.s      IL_129b

    IL_1281:  ldc.i4.1
    IL_1282:  stloc.s    V_20
    IL_1284:  ldstr      "te2 check failed {0} {1}"
    IL_1289:  ldloc.2
    IL_128a:  box        [mscorlib]System.Int32
    IL_128f:  ldloc.s    V_12
    IL_1291:  box        [mscorlib]System.Int32
    IL_1296:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_129b:  ldloc.3
    IL_129c:  ldloc.s    V_13
    IL_129e:  beq.s      IL_12ba

    IL_12a0:  ldc.i4.1
    IL_12a1:  stloc.s    V_20
    IL_12a3:  ldstr      "te3 check failed {0} {1}"
    IL_12a8:  ldloc.3
    IL_12a9:  box        [mscorlib]System.Int32
    IL_12ae:  ldloc.s    V_13
    IL_12b0:  box        [mscorlib]System.Int32
    IL_12b5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_12ba:  ldloc.s    V_4
    IL_12bc:  ldloc.s    V_14
    IL_12be:  beq.s      IL_12db

    IL_12c0:  ldc.i4.1
    IL_12c1:  stloc.s    V_20
    IL_12c3:  ldstr      "te064 check failed {0} {1}"
    IL_12c8:  ldloc.s    V_4
    IL_12ca:  box        [mscorlib]System.Int64
    IL_12cf:  ldloc.s    V_14
    IL_12d1:  box        [mscorlib]System.Int64
    IL_12d6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_12db:  ldloc.s    V_5
    IL_12dd:  ldloc.s    V_15
    IL_12df:  beq.s      IL_12fc

    IL_12e1:  ldc.i4.1
    IL_12e2:  stloc.s    V_20
    IL_12e4:  ldstr      "te164 check failed {0} {1}"
    IL_12e9:  ldloc.s    V_5
    IL_12eb:  box        [mscorlib]System.Int64
    IL_12f0:  ldloc.s    V_15
    IL_12f2:  box        [mscorlib]System.Int64
    IL_12f7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_12fc:  ldloc.s    V_6
    IL_12fe:  ldloc.s    V_16
    IL_1300:  beq.s      IL_131d

    IL_1302:  ldc.i4.1
    IL_1303:  stloc.s    V_20
    IL_1305:  ldstr      "te264 check failed {0} {1}"
    IL_130a:  ldloc.s    V_6
    IL_130c:  box        [mscorlib]System.Int64
    IL_1311:  ldloc.s    V_16
    IL_1313:  box        [mscorlib]System.Int64
    IL_1318:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_131d:  ldloc.s    V_7
    IL_131f:  ldloc.s    V_17
    IL_1321:  beq.s      IL_133e

    IL_1323:  ldc.i4.1
    IL_1324:  stloc.s    V_20
    IL_1326:  ldstr      "te364 check failed {0} {1}"
    IL_132b:  ldloc.s    V_7
    IL_132d:  box        [mscorlib]System.Int64
    IL_1332:  ldloc.s    V_17
    IL_1334:  box        [mscorlib]System.Int64
    IL_1339:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_133e:  ldloc.s    V_8
    IL_1340:  ldloc.s    V_18
    IL_1342:  beq.s      IL_135f

    IL_1344:  ldc.i4.1
    IL_1345:  stloc.s    V_20
    IL_1347:  ldstr      "dummy check failed {0} {1}"
    IL_134c:  ldloc.s    V_8
    IL_134e:  box        [mscorlib]System.Int32
    IL_1353:  ldloc.s    V_18
    IL_1355:  box        [mscorlib]System.Int32
    IL_135a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_135f:  ldloc.s    V_9
    IL_1361:  ldloc.s    V_19
    IL_1363:  beq.s      IL_1380

    IL_1365:  ldc.i4.1
    IL_1366:  stloc.s    V_20
    IL_1368:  ldstr      "dummy64 check failed {0} {1}"
    IL_136d:  ldloc.s    V_9
    IL_136f:  box        [mscorlib]System.Int64
    IL_1374:  ldloc.s    V_19
    IL_1376:  box        [mscorlib]System.Int64
    IL_137b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1380:  ldsfld     int32 IntrinsicTest::counter
    IL_1385:  ldsfld     int32 IntrinsicTest::id_counter
    IL_138a:  beq.s      IL_13ad

    IL_138c:  ldstr      "counter mismatch {0} {1}"
    IL_1391:  ldsfld     int32 IntrinsicTest::counter
    IL_1396:  box        [mscorlib]System.Int32
    IL_139b:  ldsfld     int32 IntrinsicTest::id_counter
    IL_13a0:  box        [mscorlib]System.Int32
    IL_13a5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_13aa:  ldc.i4.1
    IL_13ab:  stloc.s    V_20
    IL_13ad:  ldsfld     int64 IntrinsicTest::counter64
    IL_13b2:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_13b7:  beq.s      IL_13da

    IL_13b9:  ldstr      "counter64 mismatch {0} {1}"
    IL_13be:  ldsfld     int64 IntrinsicTest::counter64
    IL_13c3:  box        [mscorlib]System.Int64
    IL_13c8:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_13cd:  box        [mscorlib]System.Int64
    IL_13d2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_13d7:  ldc.i4.1
    IL_13d8:  stloc.s    V_20
    IL_13da:  ldloc.s    V_21
    IL_13dc:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_13e1:  ldloc.s    V_21
    IL_13e3:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_13e8:  beq.s      IL_140f

    IL_13ea:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_13ef:  ldloc.s    V_21
    IL_13f1:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_13f6:  box        [mscorlib]System.Int32
    IL_13fb:  ldloc.s    V_21
    IL_13fd:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_1402:  box        [mscorlib]System.Int32
    IL_1407:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_140c:  ldc.i4.1
    IL_140d:  stloc.s    V_20
    IL_140f:  ldloc.s    V_21
    IL_1411:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_1416:  ldloc.s    V_21
    IL_1418:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_141d:  beq.s      IL_1444

    IL_141f:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_1424:  ldloc.s    V_21
    IL_1426:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_142b:  box        [mscorlib]System.Int64
    IL_1430:  ldloc.s    V_21
    IL_1432:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_1437:  box        [mscorlib]System.Int64
    IL_143c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1441:  ldc.i4.1
    IL_1442:  stloc.s    V_20
    IL_1444:  ldsfld     int64 IntrinsicTest::temp
    IL_1449:  ldsfld     int64 IntrinsicTest::idmp
    IL_144e:  beq.s      IL_1471

    IL_1450:  ldstr      "temp mismatch {0} {1}"
    IL_1455:  ldsfld     int64 IntrinsicTest::temp
    IL_145a:  box        [mscorlib]System.Int64
    IL_145f:  ldsfld     int64 IntrinsicTest::idmp
    IL_1464:  box        [mscorlib]System.Int64
    IL_1469:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_146e:  ldc.i4.1
    IL_146f:  stloc.s    V_20
    IL_1471:  ldsflda    int32 IntrinsicTest::counter
    IL_1476:  ldsfld     int32 IntrinsicTest::counter
    IL_147b:  ldsfld     int32 IntrinsicTest::counter
    IL_1480:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_1485:  stsfld     int32 IntrinsicTest::counter
    IL_148a:  ldsfld     int32 IntrinsicTest::id_counter
    IL_148f:  stsfld     int32 IntrinsicTest::id_counter
    IL_1494:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1499:  ldsfld     int32 IntrinsicTest::id_counter
    IL_149e:  bne.un.s   IL_14ad

    IL_14a0:  ldsfld     int32 IntrinsicTest::id_counter
    IL_14a5:  conv.i8
    IL_14a6:  stsfld     int64 IntrinsicTest::idjunk
    IL_14ab:  br.s       IL_14b8

    IL_14ad:  ldsfld     int32 IntrinsicTest::id_counter
    IL_14b2:  conv.i8
    IL_14b3:  stsfld     int64 IntrinsicTest::idjunk
    IL_14b8:  ldstr      "------------------------------------CMPXCHG  1 1 1 1"
    IL_14bd:  call       void [System.Console]System.Console::WriteLine(string)
    IL_14c2:  ldloc.0
    IL_14c3:  ldloc.s    V_10
    IL_14c5:  beq.s      IL_14e1

    IL_14c7:  ldc.i4.1
    IL_14c8:  stloc.s    V_20
    IL_14ca:  ldstr      "te0 check failed {0} {1}"
    IL_14cf:  ldloc.0
    IL_14d0:  box        [mscorlib]System.Int32
    IL_14d5:  ldloc.s    V_10
    IL_14d7:  box        [mscorlib]System.Int32
    IL_14dc:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_14e1:  ldloc.1
    IL_14e2:  ldloc.s    V_11
    IL_14e4:  beq.s      IL_1500

    IL_14e6:  ldc.i4.1
    IL_14e7:  stloc.s    V_20
    IL_14e9:  ldstr      "te1 check failed {0} {1}"
    IL_14ee:  ldloc.1
    IL_14ef:  box        [mscorlib]System.Int32
    IL_14f4:  ldloc.s    V_11
    IL_14f6:  box        [mscorlib]System.Int32
    IL_14fb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1500:  ldloc.2
    IL_1501:  ldloc.s    V_12
    IL_1503:  beq.s      IL_151f

    IL_1505:  ldc.i4.1
    IL_1506:  stloc.s    V_20
    IL_1508:  ldstr      "te2 check failed {0} {1}"
    IL_150d:  ldloc.2
    IL_150e:  box        [mscorlib]System.Int32
    IL_1513:  ldloc.s    V_12
    IL_1515:  box        [mscorlib]System.Int32
    IL_151a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_151f:  ldloc.3
    IL_1520:  ldloc.s    V_13
    IL_1522:  beq.s      IL_153e

    IL_1524:  ldc.i4.1
    IL_1525:  stloc.s    V_20
    IL_1527:  ldstr      "te3 check failed {0} {1}"
    IL_152c:  ldloc.3
    IL_152d:  box        [mscorlib]System.Int32
    IL_1532:  ldloc.s    V_13
    IL_1534:  box        [mscorlib]System.Int32
    IL_1539:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_153e:  ldloc.s    V_4
    IL_1540:  ldloc.s    V_14
    IL_1542:  beq.s      IL_155f

    IL_1544:  ldc.i4.1
    IL_1545:  stloc.s    V_20
    IL_1547:  ldstr      "te064 check failed {0} {1}"
    IL_154c:  ldloc.s    V_4
    IL_154e:  box        [mscorlib]System.Int64
    IL_1553:  ldloc.s    V_14
    IL_1555:  box        [mscorlib]System.Int64
    IL_155a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_155f:  ldloc.s    V_5
    IL_1561:  ldloc.s    V_15
    IL_1563:  beq.s      IL_1580

    IL_1565:  ldc.i4.1
    IL_1566:  stloc.s    V_20
    IL_1568:  ldstr      "te164 check failed {0} {1}"
    IL_156d:  ldloc.s    V_5
    IL_156f:  box        [mscorlib]System.Int64
    IL_1574:  ldloc.s    V_15
    IL_1576:  box        [mscorlib]System.Int64
    IL_157b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1580:  ldloc.s    V_6
    IL_1582:  ldloc.s    V_16
    IL_1584:  beq.s      IL_15a1

    IL_1586:  ldc.i4.1
    IL_1587:  stloc.s    V_20
    IL_1589:  ldstr      "te264 check failed {0} {1}"
    IL_158e:  ldloc.s    V_6
    IL_1590:  box        [mscorlib]System.Int64
    IL_1595:  ldloc.s    V_16
    IL_1597:  box        [mscorlib]System.Int64
    IL_159c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_15a1:  ldloc.s    V_7
    IL_15a3:  ldloc.s    V_17
    IL_15a5:  beq.s      IL_15c2

    IL_15a7:  ldc.i4.1
    IL_15a8:  stloc.s    V_20
    IL_15aa:  ldstr      "te364 check failed {0} {1}"
    IL_15af:  ldloc.s    V_7
    IL_15b1:  box        [mscorlib]System.Int64
    IL_15b6:  ldloc.s    V_17
    IL_15b8:  box        [mscorlib]System.Int64
    IL_15bd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_15c2:  ldloc.s    V_8
    IL_15c4:  ldloc.s    V_18
    IL_15c6:  beq.s      IL_15e3

    IL_15c8:  ldc.i4.1
    IL_15c9:  stloc.s    V_20
    IL_15cb:  ldstr      "dummy check failed {0} {1}"
    IL_15d0:  ldloc.s    V_8
    IL_15d2:  box        [mscorlib]System.Int32
    IL_15d7:  ldloc.s    V_18
    IL_15d9:  box        [mscorlib]System.Int32
    IL_15de:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_15e3:  ldloc.s    V_9
    IL_15e5:  ldloc.s    V_19
    IL_15e7:  beq.s      IL_1604

    IL_15e9:  ldc.i4.1
    IL_15ea:  stloc.s    V_20
    IL_15ec:  ldstr      "dummy64 check failed {0} {1}"
    IL_15f1:  ldloc.s    V_9
    IL_15f3:  box        [mscorlib]System.Int64
    IL_15f8:  ldloc.s    V_19
    IL_15fa:  box        [mscorlib]System.Int64
    IL_15ff:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1604:  ldsfld     int32 IntrinsicTest::counter
    IL_1609:  ldsfld     int32 IntrinsicTest::id_counter
    IL_160e:  beq.s      IL_1631

    IL_1610:  ldstr      "counter mismatch {0} {1}"
    IL_1615:  ldsfld     int32 IntrinsicTest::counter
    IL_161a:  box        [mscorlib]System.Int32
    IL_161f:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1624:  box        [mscorlib]System.Int32
    IL_1629:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_162e:  ldc.i4.1
    IL_162f:  stloc.s    V_20
    IL_1631:  ldsfld     int64 IntrinsicTest::counter64
    IL_1636:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_163b:  beq.s      IL_165e

    IL_163d:  ldstr      "counter64 mismatch {0} {1}"
    IL_1642:  ldsfld     int64 IntrinsicTest::counter64
    IL_1647:  box        [mscorlib]System.Int64
    IL_164c:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1651:  box        [mscorlib]System.Int64
    IL_1656:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_165b:  ldc.i4.1
    IL_165c:  stloc.s    V_20
    IL_165e:  ldloc.s    V_21
    IL_1660:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_1665:  ldloc.s    V_21
    IL_1667:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_166c:  beq.s      IL_1693

    IL_166e:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_1673:  ldloc.s    V_21
    IL_1675:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_167a:  box        [mscorlib]System.Int32
    IL_167f:  ldloc.s    V_21
    IL_1681:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_1686:  box        [mscorlib]System.Int32
    IL_168b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1690:  ldc.i4.1
    IL_1691:  stloc.s    V_20
    IL_1693:  ldloc.s    V_21
    IL_1695:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_169a:  ldloc.s    V_21
    IL_169c:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_16a1:  beq.s      IL_16c8

    IL_16a3:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_16a8:  ldloc.s    V_21
    IL_16aa:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_16af:  box        [mscorlib]System.Int64
    IL_16b4:  ldloc.s    V_21
    IL_16b6:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_16bb:  box        [mscorlib]System.Int64
    IL_16c0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_16c5:  ldc.i4.1
    IL_16c6:  stloc.s    V_20
    IL_16c8:  ldsfld     int64 IntrinsicTest::temp
    IL_16cd:  ldsfld     int64 IntrinsicTest::idmp
    IL_16d2:  beq.s      IL_16f5

    IL_16d4:  ldstr      "temp mismatch {0} {1}"
    IL_16d9:  ldsfld     int64 IntrinsicTest::temp
    IL_16de:  box        [mscorlib]System.Int64
    IL_16e3:  ldsfld     int64 IntrinsicTest::idmp
    IL_16e8:  box        [mscorlib]System.Int64
    IL_16ed:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_16f2:  ldc.i4.1
    IL_16f3:  stloc.s    V_20
    IL_16f5:  ldloc.s    V_21
    IL_16f7:  ldsflda    int32 IntrinsicTest::counter
    IL_16fc:  ldsfld     int32 IntrinsicTest::counter
    IL_1701:  ldsfld     int32 IntrinsicTest::counter
    IL_1706:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_170b:  stfld      int32 IntrinsicTest::instanceCounter
    IL_1710:  ldloc.s    V_21
    IL_1712:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1717:  stfld      int32 IntrinsicTest::id_instanceCounter
    IL_171c:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1721:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1726:  bne.un.s   IL_1734

    IL_1728:  ldsfld     int32 IntrinsicTest::id_counter
    IL_172d:  stsfld     int32 IntrinsicTest::id_counter
    IL_1732:  br.s       IL_173f

    IL_1734:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1739:  conv.i8
    IL_173a:  stsfld     int64 IntrinsicTest::idjunk
    IL_173f:  ldstr      "------------------------------------CMPXCHG  1 1 1 2"
    IL_1744:  call       void [System.Console]System.Console::WriteLine(string)
    IL_1749:  ldloc.0
    IL_174a:  ldloc.s    V_10
    IL_174c:  beq.s      IL_1768

    IL_174e:  ldc.i4.1
    IL_174f:  stloc.s    V_20
    IL_1751:  ldstr      "te0 check failed {0} {1}"
    IL_1756:  ldloc.0
    IL_1757:  box        [mscorlib]System.Int32
    IL_175c:  ldloc.s    V_10
    IL_175e:  box        [mscorlib]System.Int32
    IL_1763:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1768:  ldloc.1
    IL_1769:  ldloc.s    V_11
    IL_176b:  beq.s      IL_1787

    IL_176d:  ldc.i4.1
    IL_176e:  stloc.s    V_20
    IL_1770:  ldstr      "te1 check failed {0} {1}"
    IL_1775:  ldloc.1
    IL_1776:  box        [mscorlib]System.Int32
    IL_177b:  ldloc.s    V_11
    IL_177d:  box        [mscorlib]System.Int32
    IL_1782:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1787:  ldloc.2
    IL_1788:  ldloc.s    V_12
    IL_178a:  beq.s      IL_17a6

    IL_178c:  ldc.i4.1
    IL_178d:  stloc.s    V_20
    IL_178f:  ldstr      "te2 check failed {0} {1}"
    IL_1794:  ldloc.2
    IL_1795:  box        [mscorlib]System.Int32
    IL_179a:  ldloc.s    V_12
    IL_179c:  box        [mscorlib]System.Int32
    IL_17a1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_17a6:  ldloc.3
    IL_17a7:  ldloc.s    V_13
    IL_17a9:  beq.s      IL_17c5

    IL_17ab:  ldc.i4.1
    IL_17ac:  stloc.s    V_20
    IL_17ae:  ldstr      "te3 check failed {0} {1}"
    IL_17b3:  ldloc.3
    IL_17b4:  box        [mscorlib]System.Int32
    IL_17b9:  ldloc.s    V_13
    IL_17bb:  box        [mscorlib]System.Int32
    IL_17c0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_17c5:  ldloc.s    V_4
    IL_17c7:  ldloc.s    V_14
    IL_17c9:  beq.s      IL_17e6

    IL_17cb:  ldc.i4.1
    IL_17cc:  stloc.s    V_20
    IL_17ce:  ldstr      "te064 check failed {0} {1}"
    IL_17d3:  ldloc.s    V_4
    IL_17d5:  box        [mscorlib]System.Int64
    IL_17da:  ldloc.s    V_14
    IL_17dc:  box        [mscorlib]System.Int64
    IL_17e1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_17e6:  ldloc.s    V_5
    IL_17e8:  ldloc.s    V_15
    IL_17ea:  beq.s      IL_1807

    IL_17ec:  ldc.i4.1
    IL_17ed:  stloc.s    V_20
    IL_17ef:  ldstr      "te164 check failed {0} {1}"
    IL_17f4:  ldloc.s    V_5
    IL_17f6:  box        [mscorlib]System.Int64
    IL_17fb:  ldloc.s    V_15
    IL_17fd:  box        [mscorlib]System.Int64
    IL_1802:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1807:  ldloc.s    V_6
    IL_1809:  ldloc.s    V_16
    IL_180b:  beq.s      IL_1828

    IL_180d:  ldc.i4.1
    IL_180e:  stloc.s    V_20
    IL_1810:  ldstr      "te264 check failed {0} {1}"
    IL_1815:  ldloc.s    V_6
    IL_1817:  box        [mscorlib]System.Int64
    IL_181c:  ldloc.s    V_16
    IL_181e:  box        [mscorlib]System.Int64
    IL_1823:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1828:  ldloc.s    V_7
    IL_182a:  ldloc.s    V_17
    IL_182c:  beq.s      IL_1849

    IL_182e:  ldc.i4.1
    IL_182f:  stloc.s    V_20
    IL_1831:  ldstr      "te364 check failed {0} {1}"
    IL_1836:  ldloc.s    V_7
    IL_1838:  box        [mscorlib]System.Int64
    IL_183d:  ldloc.s    V_17
    IL_183f:  box        [mscorlib]System.Int64
    IL_1844:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1849:  ldloc.s    V_8
    IL_184b:  ldloc.s    V_18
    IL_184d:  beq.s      IL_186a

    IL_184f:  ldc.i4.1
    IL_1850:  stloc.s    V_20
    IL_1852:  ldstr      "dummy check failed {0} {1}"
    IL_1857:  ldloc.s    V_8
    IL_1859:  box        [mscorlib]System.Int32
    IL_185e:  ldloc.s    V_18
    IL_1860:  box        [mscorlib]System.Int32
    IL_1865:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_186a:  ldloc.s    V_9
    IL_186c:  ldloc.s    V_19
    IL_186e:  beq.s      IL_188b

    IL_1870:  ldc.i4.1
    IL_1871:  stloc.s    V_20
    IL_1873:  ldstr      "dummy64 check failed {0} {1}"
    IL_1878:  ldloc.s    V_9
    IL_187a:  box        [mscorlib]System.Int64
    IL_187f:  ldloc.s    V_19
    IL_1881:  box        [mscorlib]System.Int64
    IL_1886:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_188b:  ldsfld     int32 IntrinsicTest::counter
    IL_1890:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1895:  beq.s      IL_18b8

    IL_1897:  ldstr      "counter mismatch {0} {1}"
    IL_189c:  ldsfld     int32 IntrinsicTest::counter
    IL_18a1:  box        [mscorlib]System.Int32
    IL_18a6:  ldsfld     int32 IntrinsicTest::id_counter
    IL_18ab:  box        [mscorlib]System.Int32
    IL_18b0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_18b5:  ldc.i4.1
    IL_18b6:  stloc.s    V_20
    IL_18b8:  ldsfld     int64 IntrinsicTest::counter64
    IL_18bd:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_18c2:  beq.s      IL_18e5

    IL_18c4:  ldstr      "counter64 mismatch {0} {1}"
    IL_18c9:  ldsfld     int64 IntrinsicTest::counter64
    IL_18ce:  box        [mscorlib]System.Int64
    IL_18d3:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_18d8:  box        [mscorlib]System.Int64
    IL_18dd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_18e2:  ldc.i4.1
    IL_18e3:  stloc.s    V_20
    IL_18e5:  ldloc.s    V_21
    IL_18e7:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_18ec:  ldloc.s    V_21
    IL_18ee:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_18f3:  beq.s      IL_191a

    IL_18f5:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_18fa:  ldloc.s    V_21
    IL_18fc:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_1901:  box        [mscorlib]System.Int32
    IL_1906:  ldloc.s    V_21
    IL_1908:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_190d:  box        [mscorlib]System.Int32
    IL_1912:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1917:  ldc.i4.1
    IL_1918:  stloc.s    V_20
    IL_191a:  ldloc.s    V_21
    IL_191c:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_1921:  ldloc.s    V_21
    IL_1923:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_1928:  beq.s      IL_194f

    IL_192a:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_192f:  ldloc.s    V_21
    IL_1931:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_1936:  box        [mscorlib]System.Int64
    IL_193b:  ldloc.s    V_21
    IL_193d:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_1942:  box        [mscorlib]System.Int64
    IL_1947:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_194c:  ldc.i4.1
    IL_194d:  stloc.s    V_20
    IL_194f:  ldsfld     int64 IntrinsicTest::temp
    IL_1954:  ldsfld     int64 IntrinsicTest::idmp
    IL_1959:  beq.s      IL_197c

    IL_195b:  ldstr      "temp mismatch {0} {1}"
    IL_1960:  ldsfld     int64 IntrinsicTest::temp
    IL_1965:  box        [mscorlib]System.Int64
    IL_196a:  ldsfld     int64 IntrinsicTest::idmp
    IL_196f:  box        [mscorlib]System.Int64
    IL_1974:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1979:  ldc.i4.1
    IL_197a:  stloc.s    V_20
    IL_197c:  ldsflda    int32 IntrinsicTest::counter
    IL_1981:  ldsfld     int32 IntrinsicTest::counter
    IL_1986:  ldsfld     int32 IntrinsicTest::counter
    IL_198b:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_1990:  stloc.0
    IL_1991:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1996:  stloc.s    V_10
    IL_1998:  ldsfld     int32 IntrinsicTest::id_counter
    IL_199d:  ldsfld     int32 IntrinsicTest::id_counter
    IL_19a2:  bne.un.s   IL_19b0

    IL_19a4:  ldsfld     int32 IntrinsicTest::id_counter
    IL_19a9:  stsfld     int32 IntrinsicTest::id_counter
    IL_19ae:  br.s       IL_19bb

    IL_19b0:  ldsfld     int32 IntrinsicTest::id_counter
    IL_19b5:  conv.i8
    IL_19b6:  stsfld     int64 IntrinsicTest::idjunk
    IL_19bb:  ldstr      "------------------------------------CMPXCHG  1 1 1 3"
    IL_19c0:  call       void [System.Console]System.Console::WriteLine(string)
    IL_19c5:  ldloc.0
    IL_19c6:  ldloc.s    V_10
    IL_19c8:  beq.s      IL_19e4

    IL_19ca:  ldc.i4.1
    IL_19cb:  stloc.s    V_20
    IL_19cd:  ldstr      "te0 check failed {0} {1}"
    IL_19d2:  ldloc.0
    IL_19d3:  box        [mscorlib]System.Int32
    IL_19d8:  ldloc.s    V_10
    IL_19da:  box        [mscorlib]System.Int32
    IL_19df:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_19e4:  ldloc.1
    IL_19e5:  ldloc.s    V_11
    IL_19e7:  beq.s      IL_1a03

    IL_19e9:  ldc.i4.1
    IL_19ea:  stloc.s    V_20
    IL_19ec:  ldstr      "te1 check failed {0} {1}"
    IL_19f1:  ldloc.1
    IL_19f2:  box        [mscorlib]System.Int32
    IL_19f7:  ldloc.s    V_11
    IL_19f9:  box        [mscorlib]System.Int32
    IL_19fe:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1a03:  ldloc.2
    IL_1a04:  ldloc.s    V_12
    IL_1a06:  beq.s      IL_1a22

    IL_1a08:  ldc.i4.1
    IL_1a09:  stloc.s    V_20
    IL_1a0b:  ldstr      "te2 check failed {0} {1}"
    IL_1a10:  ldloc.2
    IL_1a11:  box        [mscorlib]System.Int32
    IL_1a16:  ldloc.s    V_12
    IL_1a18:  box        [mscorlib]System.Int32
    IL_1a1d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1a22:  ldloc.3
    IL_1a23:  ldloc.s    V_13
    IL_1a25:  beq.s      IL_1a41

    IL_1a27:  ldc.i4.1
    IL_1a28:  stloc.s    V_20
    IL_1a2a:  ldstr      "te3 check failed {0} {1}"
    IL_1a2f:  ldloc.3
    IL_1a30:  box        [mscorlib]System.Int32
    IL_1a35:  ldloc.s    V_13
    IL_1a37:  box        [mscorlib]System.Int32
    IL_1a3c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1a41:  ldloc.s    V_4
    IL_1a43:  ldloc.s    V_14
    IL_1a45:  beq.s      IL_1a62

    IL_1a47:  ldc.i4.1
    IL_1a48:  stloc.s    V_20
    IL_1a4a:  ldstr      "te064 check failed {0} {1}"
    IL_1a4f:  ldloc.s    V_4
    IL_1a51:  box        [mscorlib]System.Int64
    IL_1a56:  ldloc.s    V_14
    IL_1a58:  box        [mscorlib]System.Int64
    IL_1a5d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1a62:  ldloc.s    V_5
    IL_1a64:  ldloc.s    V_15
    IL_1a66:  beq.s      IL_1a83

    IL_1a68:  ldc.i4.1
    IL_1a69:  stloc.s    V_20
    IL_1a6b:  ldstr      "te164 check failed {0} {1}"
    IL_1a70:  ldloc.s    V_5
    IL_1a72:  box        [mscorlib]System.Int64
    IL_1a77:  ldloc.s    V_15
    IL_1a79:  box        [mscorlib]System.Int64
    IL_1a7e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1a83:  ldloc.s    V_6
    IL_1a85:  ldloc.s    V_16
    IL_1a87:  beq.s      IL_1aa4

    IL_1a89:  ldc.i4.1
    IL_1a8a:  stloc.s    V_20
    IL_1a8c:  ldstr      "te264 check failed {0} {1}"
    IL_1a91:  ldloc.s    V_6
    IL_1a93:  box        [mscorlib]System.Int64
    IL_1a98:  ldloc.s    V_16
    IL_1a9a:  box        [mscorlib]System.Int64
    IL_1a9f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1aa4:  ldloc.s    V_7
    IL_1aa6:  ldloc.s    V_17
    IL_1aa8:  beq.s      IL_1ac5

    IL_1aaa:  ldc.i4.1
    IL_1aab:  stloc.s    V_20
    IL_1aad:  ldstr      "te364 check failed {0} {1}"
    IL_1ab2:  ldloc.s    V_7
    IL_1ab4:  box        [mscorlib]System.Int64
    IL_1ab9:  ldloc.s    V_17
    IL_1abb:  box        [mscorlib]System.Int64
    IL_1ac0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1ac5:  ldloc.s    V_8
    IL_1ac7:  ldloc.s    V_18
    IL_1ac9:  beq.s      IL_1ae6

    IL_1acb:  ldc.i4.1
    IL_1acc:  stloc.s    V_20
    IL_1ace:  ldstr      "dummy check failed {0} {1}"
    IL_1ad3:  ldloc.s    V_8
    IL_1ad5:  box        [mscorlib]System.Int32
    IL_1ada:  ldloc.s    V_18
    IL_1adc:  box        [mscorlib]System.Int32
    IL_1ae1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1ae6:  ldloc.s    V_9
    IL_1ae8:  ldloc.s    V_19
    IL_1aea:  beq.s      IL_1b07

    IL_1aec:  ldc.i4.1
    IL_1aed:  stloc.s    V_20
    IL_1aef:  ldstr      "dummy64 check failed {0} {1}"
    IL_1af4:  ldloc.s    V_9
    IL_1af6:  box        [mscorlib]System.Int64
    IL_1afb:  ldloc.s    V_19
    IL_1afd:  box        [mscorlib]System.Int64
    IL_1b02:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1b07:  ldsfld     int32 IntrinsicTest::counter
    IL_1b0c:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1b11:  beq.s      IL_1b34

    IL_1b13:  ldstr      "counter mismatch {0} {1}"
    IL_1b18:  ldsfld     int32 IntrinsicTest::counter
    IL_1b1d:  box        [mscorlib]System.Int32
    IL_1b22:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1b27:  box        [mscorlib]System.Int32
    IL_1b2c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1b31:  ldc.i4.1
    IL_1b32:  stloc.s    V_20
    IL_1b34:  ldsfld     int64 IntrinsicTest::counter64
    IL_1b39:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1b3e:  beq.s      IL_1b61

    IL_1b40:  ldstr      "counter64 mismatch {0} {1}"
    IL_1b45:  ldsfld     int64 IntrinsicTest::counter64
    IL_1b4a:  box        [mscorlib]System.Int64
    IL_1b4f:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1b54:  box        [mscorlib]System.Int64
    IL_1b59:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1b5e:  ldc.i4.1
    IL_1b5f:  stloc.s    V_20
    IL_1b61:  ldloc.s    V_21
    IL_1b63:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_1b68:  ldloc.s    V_21
    IL_1b6a:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_1b6f:  beq.s      IL_1b96

    IL_1b71:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_1b76:  ldloc.s    V_21
    IL_1b78:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_1b7d:  box        [mscorlib]System.Int32
    IL_1b82:  ldloc.s    V_21
    IL_1b84:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_1b89:  box        [mscorlib]System.Int32
    IL_1b8e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1b93:  ldc.i4.1
    IL_1b94:  stloc.s    V_20
    IL_1b96:  ldloc.s    V_21
    IL_1b98:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_1b9d:  ldloc.s    V_21
    IL_1b9f:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_1ba4:  beq.s      IL_1bcb

    IL_1ba6:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_1bab:  ldloc.s    V_21
    IL_1bad:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_1bb2:  box        [mscorlib]System.Int64
    IL_1bb7:  ldloc.s    V_21
    IL_1bb9:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_1bbe:  box        [mscorlib]System.Int64
    IL_1bc3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1bc8:  ldc.i4.1
    IL_1bc9:  stloc.s    V_20
    IL_1bcb:  ldsfld     int64 IntrinsicTest::temp
    IL_1bd0:  ldsfld     int64 IntrinsicTest::idmp
    IL_1bd5:  beq.s      IL_1bf8

    IL_1bd7:  ldstr      "temp mismatch {0} {1}"
    IL_1bdc:  ldsfld     int64 IntrinsicTest::temp
    IL_1be1:  box        [mscorlib]System.Int64
    IL_1be6:  ldsfld     int64 IntrinsicTest::idmp
    IL_1beb:  box        [mscorlib]System.Int64
    IL_1bf0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1bf5:  ldc.i4.1
    IL_1bf6:  stloc.s    V_20
    IL_1bf8:  ldsflda    int32 IntrinsicTest::counter
    IL_1bfd:  ldsfld     int32 IntrinsicTest::counter
    IL_1c02:  ldsfld     int32 IntrinsicTest::counter
    IL_1c07:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_1c0c:  stsfld     int32 IntrinsicTest::counter
    IL_1c11:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1c16:  stsfld     int32 IntrinsicTest::id_counter
    IL_1c1b:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1c20:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1c25:  bne.un.s   IL_1c34

    IL_1c27:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1c2c:  conv.i8
    IL_1c2d:  stsfld     int64 IntrinsicTest::idjunk
    IL_1c32:  br.s       IL_1c3f

    IL_1c34:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1c39:  conv.i8
    IL_1c3a:  stsfld     int64 IntrinsicTest::idjunk
    IL_1c3f:  ldstr      "------------------------------------CMPXCHG  1 1 1 4"
    IL_1c44:  call       void [System.Console]System.Console::WriteLine(string)
    IL_1c49:  ldloc.0
    IL_1c4a:  ldloc.s    V_10
    IL_1c4c:  beq.s      IL_1c68

    IL_1c4e:  ldc.i4.1
    IL_1c4f:  stloc.s    V_20
    IL_1c51:  ldstr      "te0 check failed {0} {1}"
    IL_1c56:  ldloc.0
    IL_1c57:  box        [mscorlib]System.Int32
    IL_1c5c:  ldloc.s    V_10
    IL_1c5e:  box        [mscorlib]System.Int32
    IL_1c63:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1c68:  ldloc.1
    IL_1c69:  ldloc.s    V_11
    IL_1c6b:  beq.s      IL_1c87

    IL_1c6d:  ldc.i4.1
    IL_1c6e:  stloc.s    V_20
    IL_1c70:  ldstr      "te1 check failed {0} {1}"
    IL_1c75:  ldloc.1
    IL_1c76:  box        [mscorlib]System.Int32
    IL_1c7b:  ldloc.s    V_11
    IL_1c7d:  box        [mscorlib]System.Int32
    IL_1c82:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1c87:  ldloc.2
    IL_1c88:  ldloc.s    V_12
    IL_1c8a:  beq.s      IL_1ca6

    IL_1c8c:  ldc.i4.1
    IL_1c8d:  stloc.s    V_20
    IL_1c8f:  ldstr      "te2 check failed {0} {1}"
    IL_1c94:  ldloc.2
    IL_1c95:  box        [mscorlib]System.Int32
    IL_1c9a:  ldloc.s    V_12
    IL_1c9c:  box        [mscorlib]System.Int32
    IL_1ca1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1ca6:  ldloc.3
    IL_1ca7:  ldloc.s    V_13
    IL_1ca9:  beq.s      IL_1cc5

    IL_1cab:  ldc.i4.1
    IL_1cac:  stloc.s    V_20
    IL_1cae:  ldstr      "te3 check failed {0} {1}"
    IL_1cb3:  ldloc.3
    IL_1cb4:  box        [mscorlib]System.Int32
    IL_1cb9:  ldloc.s    V_13
    IL_1cbb:  box        [mscorlib]System.Int32
    IL_1cc0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1cc5:  ldloc.s    V_4
    IL_1cc7:  ldloc.s    V_14
    IL_1cc9:  beq.s      IL_1ce6

    IL_1ccb:  ldc.i4.1
    IL_1ccc:  stloc.s    V_20
    IL_1cce:  ldstr      "te064 check failed {0} {1}"
    IL_1cd3:  ldloc.s    V_4
    IL_1cd5:  box        [mscorlib]System.Int64
    IL_1cda:  ldloc.s    V_14
    IL_1cdc:  box        [mscorlib]System.Int64
    IL_1ce1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1ce6:  ldloc.s    V_5
    IL_1ce8:  ldloc.s    V_15
    IL_1cea:  beq.s      IL_1d07

    IL_1cec:  ldc.i4.1
    IL_1ced:  stloc.s    V_20
    IL_1cef:  ldstr      "te164 check failed {0} {1}"
    IL_1cf4:  ldloc.s    V_5
    IL_1cf6:  box        [mscorlib]System.Int64
    IL_1cfb:  ldloc.s    V_15
    IL_1cfd:  box        [mscorlib]System.Int64
    IL_1d02:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1d07:  ldloc.s    V_6
    IL_1d09:  ldloc.s    V_16
    IL_1d0b:  beq.s      IL_1d28

    IL_1d0d:  ldc.i4.1
    IL_1d0e:  stloc.s    V_20
    IL_1d10:  ldstr      "te264 check failed {0} {1}"
    IL_1d15:  ldloc.s    V_6
    IL_1d17:  box        [mscorlib]System.Int64
    IL_1d1c:  ldloc.s    V_16
    IL_1d1e:  box        [mscorlib]System.Int64
    IL_1d23:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1d28:  ldloc.s    V_7
    IL_1d2a:  ldloc.s    V_17
    IL_1d2c:  beq.s      IL_1d49

    IL_1d2e:  ldc.i4.1
    IL_1d2f:  stloc.s    V_20
    IL_1d31:  ldstr      "te364 check failed {0} {1}"
    IL_1d36:  ldloc.s    V_7
    IL_1d38:  box        [mscorlib]System.Int64
    IL_1d3d:  ldloc.s    V_17
    IL_1d3f:  box        [mscorlib]System.Int64
    IL_1d44:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1d49:  ldloc.s    V_8
    IL_1d4b:  ldloc.s    V_18
    IL_1d4d:  beq.s      IL_1d6a

    IL_1d4f:  ldc.i4.1
    IL_1d50:  stloc.s    V_20
    IL_1d52:  ldstr      "dummy check failed {0} {1}"
    IL_1d57:  ldloc.s    V_8
    IL_1d59:  box        [mscorlib]System.Int32
    IL_1d5e:  ldloc.s    V_18
    IL_1d60:  box        [mscorlib]System.Int32
    IL_1d65:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1d6a:  ldloc.s    V_9
    IL_1d6c:  ldloc.s    V_19
    IL_1d6e:  beq.s      IL_1d8b

    IL_1d70:  ldc.i4.1
    IL_1d71:  stloc.s    V_20
    IL_1d73:  ldstr      "dummy64 check failed {0} {1}"
    IL_1d78:  ldloc.s    V_9
    IL_1d7a:  box        [mscorlib]System.Int64
    IL_1d7f:  ldloc.s    V_19
    IL_1d81:  box        [mscorlib]System.Int64
    IL_1d86:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1d8b:  ldsfld     int32 IntrinsicTest::counter
    IL_1d90:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1d95:  beq.s      IL_1db8

    IL_1d97:  ldstr      "counter mismatch {0} {1}"
    IL_1d9c:  ldsfld     int32 IntrinsicTest::counter
    IL_1da1:  box        [mscorlib]System.Int32
    IL_1da6:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1dab:  box        [mscorlib]System.Int32
    IL_1db0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1db5:  ldc.i4.1
    IL_1db6:  stloc.s    V_20
    IL_1db8:  ldsfld     int64 IntrinsicTest::counter64
    IL_1dbd:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1dc2:  beq.s      IL_1de5

    IL_1dc4:  ldstr      "counter64 mismatch {0} {1}"
    IL_1dc9:  ldsfld     int64 IntrinsicTest::counter64
    IL_1dce:  box        [mscorlib]System.Int64
    IL_1dd3:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_1dd8:  box        [mscorlib]System.Int64
    IL_1ddd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1de2:  ldc.i4.1
    IL_1de3:  stloc.s    V_20
    IL_1de5:  ldloc.s    V_21
    IL_1de7:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_1dec:  ldloc.s    V_21
    IL_1dee:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_1df3:  beq.s      IL_1e1a

    IL_1df5:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_1dfa:  ldloc.s    V_21
    IL_1dfc:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_1e01:  box        [mscorlib]System.Int32
    IL_1e06:  ldloc.s    V_21
    IL_1e08:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_1e0d:  box        [mscorlib]System.Int32
    IL_1e12:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1e17:  ldc.i4.1
    IL_1e18:  stloc.s    V_20
    IL_1e1a:  ldloc.s    V_21
    IL_1e1c:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_1e21:  ldloc.s    V_21
    IL_1e23:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_1e28:  beq.s      IL_1e4f

    IL_1e2a:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_1e2f:  ldloc.s    V_21
    IL_1e31:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_1e36:  box        [mscorlib]System.Int64
    IL_1e3b:  ldloc.s    V_21
    IL_1e3d:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_1e42:  box        [mscorlib]System.Int64
    IL_1e47:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1e4c:  ldc.i4.1
    IL_1e4d:  stloc.s    V_20
    IL_1e4f:  ldsfld     int64 IntrinsicTest::temp
    IL_1e54:  ldsfld     int64 IntrinsicTest::idmp
    IL_1e59:  beq.s      IL_1e7c

    IL_1e5b:  ldstr      "temp mismatch {0} {1}"
    IL_1e60:  ldsfld     int64 IntrinsicTest::temp
    IL_1e65:  box        [mscorlib]System.Int64
    IL_1e6a:  ldsfld     int64 IntrinsicTest::idmp
    IL_1e6f:  box        [mscorlib]System.Int64
    IL_1e74:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1e79:  ldc.i4.1
    IL_1e7a:  stloc.s    V_20
    IL_1e7c:  ldloc.s    V_21
    IL_1e7e:  ldsflda    int32 IntrinsicTest::counter
    IL_1e83:  ldsfld     int32 IntrinsicTest::counter
    IL_1e88:  ldsfld     int32 IntrinsicTest::counter
    IL_1e8d:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_1e92:  stfld      int32 IntrinsicTest::instanceCounter
    IL_1e97:  ldloc.s    V_21
    IL_1e99:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1e9e:  stfld      int32 IntrinsicTest::id_instanceCounter
    IL_1ea3:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1ea8:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1ead:  bne.un.s   IL_1ebb

    IL_1eaf:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1eb4:  stsfld     int32 IntrinsicTest::id_counter
    IL_1eb9:  br.s       IL_1ec6

    IL_1ebb:  ldsfld     int32 IntrinsicTest::id_counter
    IL_1ec0:  conv.i8
    IL_1ec1:  stsfld     int64 IntrinsicTest::idjunk
    IL_1ec6:  ldstr      "------------------------------------CMPXCHG  1 1 1 5"
    IL_1ecb:  call       void [System.Console]System.Console::WriteLine(string)
    IL_1ed0:  ldloc.0
    IL_1ed1:  ldloc.s    V_10
    IL_1ed3:  beq.s      IL_1eef

    IL_1ed5:  ldc.i4.1
    IL_1ed6:  stloc.s    V_20
    IL_1ed8:  ldstr      "te0 check failed {0} {1}"
    IL_1edd:  ldloc.0
    IL_1ede:  box        [mscorlib]System.Int32
    IL_1ee3:  ldloc.s    V_10
    IL_1ee5:  box        [mscorlib]System.Int32
    IL_1eea:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1eef:  ldloc.1
    IL_1ef0:  ldloc.s    V_11
    IL_1ef2:  beq.s      IL_1f0e

    IL_1ef4:  ldc.i4.1
    IL_1ef5:  stloc.s    V_20
    IL_1ef7:  ldstr      "te1 check failed {0} {1}"
    IL_1efc:  ldloc.1
    IL_1efd:  box        [mscorlib]System.Int32
    IL_1f02:  ldloc.s    V_11
    IL_1f04:  box        [mscorlib]System.Int32
    IL_1f09:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1f0e:  ldloc.2
    IL_1f0f:  ldloc.s    V_12
    IL_1f11:  beq.s      IL_1f2d

    IL_1f13:  ldc.i4.1
    IL_1f14:  stloc.s    V_20
    IL_1f16:  ldstr      "te2 check failed {0} {1}"
    IL_1f1b:  ldloc.2
    IL_1f1c:  box        [mscorlib]System.Int32
    IL_1f21:  ldloc.s    V_12
    IL_1f23:  box        [mscorlib]System.Int32
    IL_1f28:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1f2d:  ldloc.3
    IL_1f2e:  ldloc.s    V_13
    IL_1f30:  beq.s      IL_1f4c

    IL_1f32:  ldc.i4.1
    IL_1f33:  stloc.s    V_20
    IL_1f35:  ldstr      "te3 check failed {0} {1}"
    IL_1f3a:  ldloc.3
    IL_1f3b:  box        [mscorlib]System.Int32
    IL_1f40:  ldloc.s    V_13
    IL_1f42:  box        [mscorlib]System.Int32
    IL_1f47:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1f4c:  ldloc.s    V_4
    IL_1f4e:  ldloc.s    V_14
    IL_1f50:  beq.s      IL_1f6d

    IL_1f52:  ldc.i4.1
    IL_1f53:  stloc.s    V_20
    IL_1f55:  ldstr      "te064 check failed {0} {1}"
    IL_1f5a:  ldloc.s    V_4
    IL_1f5c:  box        [mscorlib]System.Int64
    IL_1f61:  ldloc.s    V_14
    IL_1f63:  box        [mscorlib]System.Int64
    IL_1f68:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1f6d:  ldloc.s    V_5
    IL_1f6f:  ldloc.s    V_15
    IL_1f71:  beq.s      IL_1f8e

    IL_1f73:  ldc.i4.1
    IL_1f74:  stloc.s    V_20
    IL_1f76:  ldstr      "te164 check failed {0} {1}"
    IL_1f7b:  ldloc.s    V_5
    IL_1f7d:  box        [mscorlib]System.Int64
    IL_1f82:  ldloc.s    V_15
    IL_1f84:  box        [mscorlib]System.Int64
    IL_1f89:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1f8e:  ldloc.s    V_6
    IL_1f90:  ldloc.s    V_16
    IL_1f92:  beq.s      IL_1faf

    IL_1f94:  ldc.i4.1
    IL_1f95:  stloc.s    V_20
    IL_1f97:  ldstr      "te264 check failed {0} {1}"
    IL_1f9c:  ldloc.s    V_6
    IL_1f9e:  box        [mscorlib]System.Int64
    IL_1fa3:  ldloc.s    V_16
    IL_1fa5:  box        [mscorlib]System.Int64
    IL_1faa:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1faf:  ldloc.s    V_7
    IL_1fb1:  ldloc.s    V_17
    IL_1fb3:  beq.s      IL_1fd0

    IL_1fb5:  ldc.i4.1
    IL_1fb6:  stloc.s    V_20
    IL_1fb8:  ldstr      "te364 check failed {0} {1}"
    IL_1fbd:  ldloc.s    V_7
    IL_1fbf:  box        [mscorlib]System.Int64
    IL_1fc4:  ldloc.s    V_17
    IL_1fc6:  box        [mscorlib]System.Int64
    IL_1fcb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1fd0:  ldloc.s    V_8
    IL_1fd2:  ldloc.s    V_18
    IL_1fd4:  beq.s      IL_1ff1

    IL_1fd6:  ldc.i4.1
    IL_1fd7:  stloc.s    V_20
    IL_1fd9:  ldstr      "dummy check failed {0} {1}"
    IL_1fde:  ldloc.s    V_8
    IL_1fe0:  box        [mscorlib]System.Int32
    IL_1fe5:  ldloc.s    V_18
    IL_1fe7:  box        [mscorlib]System.Int32
    IL_1fec:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_1ff1:  ldloc.s    V_9
    IL_1ff3:  ldloc.s    V_19
    IL_1ff5:  beq.s      IL_2012

    IL_1ff7:  ldc.i4.1
    IL_1ff8:  stloc.s    V_20
    IL_1ffa:  ldstr      "dummy64 check failed {0} {1}"
    IL_1fff:  ldloc.s    V_9
    IL_2001:  box        [mscorlib]System.Int64
    IL_2006:  ldloc.s    V_19
    IL_2008:  box        [mscorlib]System.Int64
    IL_200d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2012:  ldsfld     int32 IntrinsicTest::counter
    IL_2017:  ldsfld     int32 IntrinsicTest::id_counter
    IL_201c:  beq.s      IL_203f

    IL_201e:  ldstr      "counter mismatch {0} {1}"
    IL_2023:  ldsfld     int32 IntrinsicTest::counter
    IL_2028:  box        [mscorlib]System.Int32
    IL_202d:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2032:  box        [mscorlib]System.Int32
    IL_2037:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_203c:  ldc.i4.1
    IL_203d:  stloc.s    V_20
    IL_203f:  ldsfld     int64 IntrinsicTest::counter64
    IL_2044:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2049:  beq.s      IL_206c

    IL_204b:  ldstr      "counter64 mismatch {0} {1}"
    IL_2050:  ldsfld     int64 IntrinsicTest::counter64
    IL_2055:  box        [mscorlib]System.Int64
    IL_205a:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_205f:  box        [mscorlib]System.Int64
    IL_2064:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2069:  ldc.i4.1
    IL_206a:  stloc.s    V_20
    IL_206c:  ldloc.s    V_21
    IL_206e:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_2073:  ldloc.s    V_21
    IL_2075:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_207a:  beq.s      IL_20a1

    IL_207c:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_2081:  ldloc.s    V_21
    IL_2083:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_2088:  box        [mscorlib]System.Int32
    IL_208d:  ldloc.s    V_21
    IL_208f:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_2094:  box        [mscorlib]System.Int32
    IL_2099:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_209e:  ldc.i4.1
    IL_209f:  stloc.s    V_20
    IL_20a1:  ldloc.s    V_21
    IL_20a3:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_20a8:  ldloc.s    V_21
    IL_20aa:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_20af:  beq.s      IL_20d6

    IL_20b1:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_20b6:  ldloc.s    V_21
    IL_20b8:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_20bd:  box        [mscorlib]System.Int64
    IL_20c2:  ldloc.s    V_21
    IL_20c4:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_20c9:  box        [mscorlib]System.Int64
    IL_20ce:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_20d3:  ldc.i4.1
    IL_20d4:  stloc.s    V_20
    IL_20d6:  ldsfld     int64 IntrinsicTest::temp
    IL_20db:  ldsfld     int64 IntrinsicTest::idmp
    IL_20e0:  beq.s      IL_2103

    IL_20e2:  ldstr      "temp mismatch {0} {1}"
    IL_20e7:  ldsfld     int64 IntrinsicTest::temp
    IL_20ec:  box        [mscorlib]System.Int64
    IL_20f1:  ldsfld     int64 IntrinsicTest::idmp
    IL_20f6:  box        [mscorlib]System.Int64
    IL_20fb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2100:  ldc.i4.1
    IL_2101:  stloc.s    V_20
    IL_2103:  ldsflda    int32 IntrinsicTest::counter
    IL_2108:  ldsfld     int32 IntrinsicTest::counter
    IL_210d:  ldsfld     int32 IntrinsicTest::counter
    IL_2112:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_2117:  stloc.3
    IL_2118:  ldsfld     int32 IntrinsicTest::id_counter
    IL_211d:  stloc.s    V_13
    IL_211f:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2124:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2129:  bne.un.s   IL_2137

    IL_212b:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2130:  stsfld     int32 IntrinsicTest::id_counter
    IL_2135:  br.s       IL_2142

    IL_2137:  ldsfld     int32 IntrinsicTest::id_counter
    IL_213c:  conv.i8
    IL_213d:  stsfld     int64 IntrinsicTest::idjunk
    IL_2142:  ldstr      "------------------------------------CMPXCHG  1 1 1 6"
    IL_2147:  call       void [System.Console]System.Console::WriteLine(string)
    IL_214c:  ldloc.0
    IL_214d:  ldloc.s    V_10
    IL_214f:  beq.s      IL_216b

    IL_2151:  ldc.i4.1
    IL_2152:  stloc.s    V_20
    IL_2154:  ldstr      "te0 check failed {0} {1}"
    IL_2159:  ldloc.0
    IL_215a:  box        [mscorlib]System.Int32
    IL_215f:  ldloc.s    V_10
    IL_2161:  box        [mscorlib]System.Int32
    IL_2166:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_216b:  ldloc.1
    IL_216c:  ldloc.s    V_11
    IL_216e:  beq.s      IL_218a

    IL_2170:  ldc.i4.1
    IL_2171:  stloc.s    V_20
    IL_2173:  ldstr      "te1 check failed {0} {1}"
    IL_2178:  ldloc.1
    IL_2179:  box        [mscorlib]System.Int32
    IL_217e:  ldloc.s    V_11
    IL_2180:  box        [mscorlib]System.Int32
    IL_2185:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_218a:  ldloc.2
    IL_218b:  ldloc.s    V_12
    IL_218d:  beq.s      IL_21a9

    IL_218f:  ldc.i4.1
    IL_2190:  stloc.s    V_20
    IL_2192:  ldstr      "te2 check failed {0} {1}"
    IL_2197:  ldloc.2
    IL_2198:  box        [mscorlib]System.Int32
    IL_219d:  ldloc.s    V_12
    IL_219f:  box        [mscorlib]System.Int32
    IL_21a4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_21a9:  ldloc.3
    IL_21aa:  ldloc.s    V_13
    IL_21ac:  beq.s      IL_21c8

    IL_21ae:  ldc.i4.1
    IL_21af:  stloc.s    V_20
    IL_21b1:  ldstr      "te3 check failed {0} {1}"
    IL_21b6:  ldloc.3
    IL_21b7:  box        [mscorlib]System.Int32
    IL_21bc:  ldloc.s    V_13
    IL_21be:  box        [mscorlib]System.Int32
    IL_21c3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_21c8:  ldloc.s    V_4
    IL_21ca:  ldloc.s    V_14
    IL_21cc:  beq.s      IL_21e9

    IL_21ce:  ldc.i4.1
    IL_21cf:  stloc.s    V_20
    IL_21d1:  ldstr      "te064 check failed {0} {1}"
    IL_21d6:  ldloc.s    V_4
    IL_21d8:  box        [mscorlib]System.Int64
    IL_21dd:  ldloc.s    V_14
    IL_21df:  box        [mscorlib]System.Int64
    IL_21e4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_21e9:  ldloc.s    V_5
    IL_21eb:  ldloc.s    V_15
    IL_21ed:  beq.s      IL_220a

    IL_21ef:  ldc.i4.1
    IL_21f0:  stloc.s    V_20
    IL_21f2:  ldstr      "te164 check failed {0} {1}"
    IL_21f7:  ldloc.s    V_5
    IL_21f9:  box        [mscorlib]System.Int64
    IL_21fe:  ldloc.s    V_15
    IL_2200:  box        [mscorlib]System.Int64
    IL_2205:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_220a:  ldloc.s    V_6
    IL_220c:  ldloc.s    V_16
    IL_220e:  beq.s      IL_222b

    IL_2210:  ldc.i4.1
    IL_2211:  stloc.s    V_20
    IL_2213:  ldstr      "te264 check failed {0} {1}"
    IL_2218:  ldloc.s    V_6
    IL_221a:  box        [mscorlib]System.Int64
    IL_221f:  ldloc.s    V_16
    IL_2221:  box        [mscorlib]System.Int64
    IL_2226:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_222b:  ldloc.s    V_7
    IL_222d:  ldloc.s    V_17
    IL_222f:  beq.s      IL_224c

    IL_2231:  ldc.i4.1
    IL_2232:  stloc.s    V_20
    IL_2234:  ldstr      "te364 check failed {0} {1}"
    IL_2239:  ldloc.s    V_7
    IL_223b:  box        [mscorlib]System.Int64
    IL_2240:  ldloc.s    V_17
    IL_2242:  box        [mscorlib]System.Int64
    IL_2247:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_224c:  ldloc.s    V_8
    IL_224e:  ldloc.s    V_18
    IL_2250:  beq.s      IL_226d

    IL_2252:  ldc.i4.1
    IL_2253:  stloc.s    V_20
    IL_2255:  ldstr      "dummy check failed {0} {1}"
    IL_225a:  ldloc.s    V_8
    IL_225c:  box        [mscorlib]System.Int32
    IL_2261:  ldloc.s    V_18
    IL_2263:  box        [mscorlib]System.Int32
    IL_2268:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_226d:  ldloc.s    V_9
    IL_226f:  ldloc.s    V_19
    IL_2271:  beq.s      IL_228e

    IL_2273:  ldc.i4.1
    IL_2274:  stloc.s    V_20
    IL_2276:  ldstr      "dummy64 check failed {0} {1}"
    IL_227b:  ldloc.s    V_9
    IL_227d:  box        [mscorlib]System.Int64
    IL_2282:  ldloc.s    V_19
    IL_2284:  box        [mscorlib]System.Int64
    IL_2289:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_228e:  ldsfld     int32 IntrinsicTest::counter
    IL_2293:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2298:  beq.s      IL_22bb

    IL_229a:  ldstr      "counter mismatch {0} {1}"
    IL_229f:  ldsfld     int32 IntrinsicTest::counter
    IL_22a4:  box        [mscorlib]System.Int32
    IL_22a9:  ldsfld     int32 IntrinsicTest::id_counter
    IL_22ae:  box        [mscorlib]System.Int32
    IL_22b3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_22b8:  ldc.i4.1
    IL_22b9:  stloc.s    V_20
    IL_22bb:  ldsfld     int64 IntrinsicTest::counter64
    IL_22c0:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_22c5:  beq.s      IL_22e8

    IL_22c7:  ldstr      "counter64 mismatch {0} {1}"
    IL_22cc:  ldsfld     int64 IntrinsicTest::counter64
    IL_22d1:  box        [mscorlib]System.Int64
    IL_22d6:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_22db:  box        [mscorlib]System.Int64
    IL_22e0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_22e5:  ldc.i4.1
    IL_22e6:  stloc.s    V_20
    IL_22e8:  ldloc.s    V_21
    IL_22ea:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_22ef:  ldloc.s    V_21
    IL_22f1:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_22f6:  beq.s      IL_231d

    IL_22f8:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_22fd:  ldloc.s    V_21
    IL_22ff:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_2304:  box        [mscorlib]System.Int32
    IL_2309:  ldloc.s    V_21
    IL_230b:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_2310:  box        [mscorlib]System.Int32
    IL_2315:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_231a:  ldc.i4.1
    IL_231b:  stloc.s    V_20
    IL_231d:  ldloc.s    V_21
    IL_231f:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_2324:  ldloc.s    V_21
    IL_2326:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_232b:  beq.s      IL_2352

    IL_232d:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_2332:  ldloc.s    V_21
    IL_2334:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_2339:  box        [mscorlib]System.Int64
    IL_233e:  ldloc.s    V_21
    IL_2340:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_2345:  box        [mscorlib]System.Int64
    IL_234a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_234f:  ldc.i4.1
    IL_2350:  stloc.s    V_20
    IL_2352:  ldsfld     int64 IntrinsicTest::temp
    IL_2357:  ldsfld     int64 IntrinsicTest::idmp
    IL_235c:  beq.s      IL_237f

    IL_235e:  ldstr      "temp mismatch {0} {1}"
    IL_2363:  ldsfld     int64 IntrinsicTest::temp
    IL_2368:  box        [mscorlib]System.Int64
    IL_236d:  ldsfld     int64 IntrinsicTest::idmp
    IL_2372:  box        [mscorlib]System.Int64
    IL_2377:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_237c:  ldc.i4.1
    IL_237d:  stloc.s    V_20
    IL_237f:  ldsflda    int32 IntrinsicTest::counter
    IL_2384:  ldsfld     int32 IntrinsicTest::counter
    IL_2389:  ldloc.s    V_21
    IL_238b:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_2390:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_2395:  stloc.0
    IL_2396:  ldsfld     int32 IntrinsicTest::id_counter
    IL_239b:  stloc.s    V_10
    IL_239d:  ldsfld     int32 IntrinsicTest::id_counter
    IL_23a2:  ldloc.s    V_21
    IL_23a4:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_23a9:  bne.un.s   IL_23b7

    IL_23ab:  ldsfld     int32 IntrinsicTest::id_counter
    IL_23b0:  stsfld     int32 IntrinsicTest::id_counter
    IL_23b5:  br.s       IL_23c2

    IL_23b7:  ldsfld     int32 IntrinsicTest::id_counter
    IL_23bc:  conv.i8
    IL_23bd:  stsfld     int64 IntrinsicTest::idjunk
    IL_23c2:  ldstr      "------------------------------------CMPXCHG  1 1 2 0"
    IL_23c7:  call       void [System.Console]System.Console::WriteLine(string)
    IL_23cc:  ldloc.0
    IL_23cd:  ldloc.s    V_10
    IL_23cf:  beq.s      IL_23eb

    IL_23d1:  ldc.i4.1
    IL_23d2:  stloc.s    V_20
    IL_23d4:  ldstr      "te0 check failed {0} {1}"
    IL_23d9:  ldloc.0
    IL_23da:  box        [mscorlib]System.Int32
    IL_23df:  ldloc.s    V_10
    IL_23e1:  box        [mscorlib]System.Int32
    IL_23e6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_23eb:  ldloc.1
    IL_23ec:  ldloc.s    V_11
    IL_23ee:  beq.s      IL_240a

    IL_23f0:  ldc.i4.1
    IL_23f1:  stloc.s    V_20
    IL_23f3:  ldstr      "te1 check failed {0} {1}"
    IL_23f8:  ldloc.1
    IL_23f9:  box        [mscorlib]System.Int32
    IL_23fe:  ldloc.s    V_11
    IL_2400:  box        [mscorlib]System.Int32
    IL_2405:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_240a:  ldloc.2
    IL_240b:  ldloc.s    V_12
    IL_240d:  beq.s      IL_2429

    IL_240f:  ldc.i4.1
    IL_2410:  stloc.s    V_20
    IL_2412:  ldstr      "te2 check failed {0} {1}"
    IL_2417:  ldloc.2
    IL_2418:  box        [mscorlib]System.Int32
    IL_241d:  ldloc.s    V_12
    IL_241f:  box        [mscorlib]System.Int32
    IL_2424:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2429:  ldloc.3
    IL_242a:  ldloc.s    V_13
    IL_242c:  beq.s      IL_2448

    IL_242e:  ldc.i4.1
    IL_242f:  stloc.s    V_20
    IL_2431:  ldstr      "te3 check failed {0} {1}"
    IL_2436:  ldloc.3
    IL_2437:  box        [mscorlib]System.Int32
    IL_243c:  ldloc.s    V_13
    IL_243e:  box        [mscorlib]System.Int32
    IL_2443:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2448:  ldloc.s    V_4
    IL_244a:  ldloc.s    V_14
    IL_244c:  beq.s      IL_2469

    IL_244e:  ldc.i4.1
    IL_244f:  stloc.s    V_20
    IL_2451:  ldstr      "te064 check failed {0} {1}"
    IL_2456:  ldloc.s    V_4
    IL_2458:  box        [mscorlib]System.Int64
    IL_245d:  ldloc.s    V_14
    IL_245f:  box        [mscorlib]System.Int64
    IL_2464:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2469:  ldloc.s    V_5
    IL_246b:  ldloc.s    V_15
    IL_246d:  beq.s      IL_248a

    IL_246f:  ldc.i4.1
    IL_2470:  stloc.s    V_20
    IL_2472:  ldstr      "te164 check failed {0} {1}"
    IL_2477:  ldloc.s    V_5
    IL_2479:  box        [mscorlib]System.Int64
    IL_247e:  ldloc.s    V_15
    IL_2480:  box        [mscorlib]System.Int64
    IL_2485:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_248a:  ldloc.s    V_6
    IL_248c:  ldloc.s    V_16
    IL_248e:  beq.s      IL_24ab

    IL_2490:  ldc.i4.1
    IL_2491:  stloc.s    V_20
    IL_2493:  ldstr      "te264 check failed {0} {1}"
    IL_2498:  ldloc.s    V_6
    IL_249a:  box        [mscorlib]System.Int64
    IL_249f:  ldloc.s    V_16
    IL_24a1:  box        [mscorlib]System.Int64
    IL_24a6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_24ab:  ldloc.s    V_7
    IL_24ad:  ldloc.s    V_17
    IL_24af:  beq.s      IL_24cc

    IL_24b1:  ldc.i4.1
    IL_24b2:  stloc.s    V_20
    IL_24b4:  ldstr      "te364 check failed {0} {1}"
    IL_24b9:  ldloc.s    V_7
    IL_24bb:  box        [mscorlib]System.Int64
    IL_24c0:  ldloc.s    V_17
    IL_24c2:  box        [mscorlib]System.Int64
    IL_24c7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_24cc:  ldloc.s    V_8
    IL_24ce:  ldloc.s    V_18
    IL_24d0:  beq.s      IL_24ed

    IL_24d2:  ldc.i4.1
    IL_24d3:  stloc.s    V_20
    IL_24d5:  ldstr      "dummy check failed {0} {1}"
    IL_24da:  ldloc.s    V_8
    IL_24dc:  box        [mscorlib]System.Int32
    IL_24e1:  ldloc.s    V_18
    IL_24e3:  box        [mscorlib]System.Int32
    IL_24e8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_24ed:  ldloc.s    V_9
    IL_24ef:  ldloc.s    V_19
    IL_24f1:  beq.s      IL_250e

    IL_24f3:  ldc.i4.1
    IL_24f4:  stloc.s    V_20
    IL_24f6:  ldstr      "dummy64 check failed {0} {1}"
    IL_24fb:  ldloc.s    V_9
    IL_24fd:  box        [mscorlib]System.Int64
    IL_2502:  ldloc.s    V_19
    IL_2504:  box        [mscorlib]System.Int64
    IL_2509:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_250e:  ldsfld     int32 IntrinsicTest::counter
    IL_2513:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2518:  beq.s      IL_253b

    IL_251a:  ldstr      "counter mismatch {0} {1}"
    IL_251f:  ldsfld     int32 IntrinsicTest::counter
    IL_2524:  box        [mscorlib]System.Int32
    IL_2529:  ldsfld     int32 IntrinsicTest::id_counter
    IL_252e:  box        [mscorlib]System.Int32
    IL_2533:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2538:  ldc.i4.1
    IL_2539:  stloc.s    V_20
    IL_253b:  ldsfld     int64 IntrinsicTest::counter64
    IL_2540:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2545:  beq.s      IL_2568

    IL_2547:  ldstr      "counter64 mismatch {0} {1}"
    IL_254c:  ldsfld     int64 IntrinsicTest::counter64
    IL_2551:  box        [mscorlib]System.Int64
    IL_2556:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_255b:  box        [mscorlib]System.Int64
    IL_2560:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2565:  ldc.i4.1
    IL_2566:  stloc.s    V_20
    IL_2568:  ldloc.s    V_21
    IL_256a:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_256f:  ldloc.s    V_21
    IL_2571:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_2576:  beq.s      IL_259d

    IL_2578:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_257d:  ldloc.s    V_21
    IL_257f:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_2584:  box        [mscorlib]System.Int32
    IL_2589:  ldloc.s    V_21
    IL_258b:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_2590:  box        [mscorlib]System.Int32
    IL_2595:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_259a:  ldc.i4.1
    IL_259b:  stloc.s    V_20
    IL_259d:  ldloc.s    V_21
    IL_259f:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_25a4:  ldloc.s    V_21
    IL_25a6:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_25ab:  beq.s      IL_25d2

    IL_25ad:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_25b2:  ldloc.s    V_21
    IL_25b4:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_25b9:  box        [mscorlib]System.Int64
    IL_25be:  ldloc.s    V_21
    IL_25c0:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_25c5:  box        [mscorlib]System.Int64
    IL_25ca:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_25cf:  ldc.i4.1
    IL_25d0:  stloc.s    V_20
    IL_25d2:  ldsfld     int64 IntrinsicTest::temp
    IL_25d7:  ldsfld     int64 IntrinsicTest::idmp
    IL_25dc:  beq.s      IL_25ff

    IL_25de:  ldstr      "temp mismatch {0} {1}"
    IL_25e3:  ldsfld     int64 IntrinsicTest::temp
    IL_25e8:  box        [mscorlib]System.Int64
    IL_25ed:  ldsfld     int64 IntrinsicTest::idmp
    IL_25f2:  box        [mscorlib]System.Int64
    IL_25f7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_25fc:  ldc.i4.1
    IL_25fd:  stloc.s    V_20
    IL_25ff:  ldsflda    int32 IntrinsicTest::counter
    IL_2604:  ldsfld     int32 IntrinsicTest::counter
    IL_2609:  ldloc.s    V_21
    IL_260b:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_2610:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_2615:  stsfld     int32 IntrinsicTest::counter
    IL_261a:  ldsfld     int32 IntrinsicTest::id_counter
    IL_261f:  stsfld     int32 IntrinsicTest::id_counter
    IL_2624:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2629:  ldloc.s    V_21
    IL_262b:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_2630:  bne.un.s   IL_263f

    IL_2632:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2637:  conv.i8
    IL_2638:  stsfld     int64 IntrinsicTest::idjunk
    IL_263d:  br.s       IL_264a

    IL_263f:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2644:  conv.i8
    IL_2645:  stsfld     int64 IntrinsicTest::idjunk
    IL_264a:  ldstr      "------------------------------------CMPXCHG  1 1 2 1"
    IL_264f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_2654:  ldloc.0
    IL_2655:  ldloc.s    V_10
    IL_2657:  beq.s      IL_2673

    IL_2659:  ldc.i4.1
    IL_265a:  stloc.s    V_20
    IL_265c:  ldstr      "te0 check failed {0} {1}"
    IL_2661:  ldloc.0
    IL_2662:  box        [mscorlib]System.Int32
    IL_2667:  ldloc.s    V_10
    IL_2669:  box        [mscorlib]System.Int32
    IL_266e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2673:  ldloc.1
    IL_2674:  ldloc.s    V_11
    IL_2676:  beq.s      IL_2692

    IL_2678:  ldc.i4.1
    IL_2679:  stloc.s    V_20
    IL_267b:  ldstr      "te1 check failed {0} {1}"
    IL_2680:  ldloc.1
    IL_2681:  box        [mscorlib]System.Int32
    IL_2686:  ldloc.s    V_11
    IL_2688:  box        [mscorlib]System.Int32
    IL_268d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2692:  ldloc.2
    IL_2693:  ldloc.s    V_12
    IL_2695:  beq.s      IL_26b1

    IL_2697:  ldc.i4.1
    IL_2698:  stloc.s    V_20
    IL_269a:  ldstr      "te2 check failed {0} {1}"
    IL_269f:  ldloc.2
    IL_26a0:  box        [mscorlib]System.Int32
    IL_26a5:  ldloc.s    V_12
    IL_26a7:  box        [mscorlib]System.Int32
    IL_26ac:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_26b1:  ldloc.3
    IL_26b2:  ldloc.s    V_13
    IL_26b4:  beq.s      IL_26d0

    IL_26b6:  ldc.i4.1
    IL_26b7:  stloc.s    V_20
    IL_26b9:  ldstr      "te3 check failed {0} {1}"
    IL_26be:  ldloc.3
    IL_26bf:  box        [mscorlib]System.Int32
    IL_26c4:  ldloc.s    V_13
    IL_26c6:  box        [mscorlib]System.Int32
    IL_26cb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_26d0:  ldloc.s    V_4
    IL_26d2:  ldloc.s    V_14
    IL_26d4:  beq.s      IL_26f1

    IL_26d6:  ldc.i4.1
    IL_26d7:  stloc.s    V_20
    IL_26d9:  ldstr      "te064 check failed {0} {1}"
    IL_26de:  ldloc.s    V_4
    IL_26e0:  box        [mscorlib]System.Int64
    IL_26e5:  ldloc.s    V_14
    IL_26e7:  box        [mscorlib]System.Int64
    IL_26ec:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_26f1:  ldloc.s    V_5
    IL_26f3:  ldloc.s    V_15
    IL_26f5:  beq.s      IL_2712

    IL_26f7:  ldc.i4.1
    IL_26f8:  stloc.s    V_20
    IL_26fa:  ldstr      "te164 check failed {0} {1}"
    IL_26ff:  ldloc.s    V_5
    IL_2701:  box        [mscorlib]System.Int64
    IL_2706:  ldloc.s    V_15
    IL_2708:  box        [mscorlib]System.Int64
    IL_270d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2712:  ldloc.s    V_6
    IL_2714:  ldloc.s    V_16
    IL_2716:  beq.s      IL_2733

    IL_2718:  ldc.i4.1
    IL_2719:  stloc.s    V_20
    IL_271b:  ldstr      "te264 check failed {0} {1}"
    IL_2720:  ldloc.s    V_6
    IL_2722:  box        [mscorlib]System.Int64
    IL_2727:  ldloc.s    V_16
    IL_2729:  box        [mscorlib]System.Int64
    IL_272e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2733:  ldloc.s    V_7
    IL_2735:  ldloc.s    V_17
    IL_2737:  beq.s      IL_2754

    IL_2739:  ldc.i4.1
    IL_273a:  stloc.s    V_20
    IL_273c:  ldstr      "te364 check failed {0} {1}"
    IL_2741:  ldloc.s    V_7
    IL_2743:  box        [mscorlib]System.Int64
    IL_2748:  ldloc.s    V_17
    IL_274a:  box        [mscorlib]System.Int64
    IL_274f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2754:  ldloc.s    V_8
    IL_2756:  ldloc.s    V_18
    IL_2758:  beq.s      IL_2775

    IL_275a:  ldc.i4.1
    IL_275b:  stloc.s    V_20
    IL_275d:  ldstr      "dummy check failed {0} {1}"
    IL_2762:  ldloc.s    V_8
    IL_2764:  box        [mscorlib]System.Int32
    IL_2769:  ldloc.s    V_18
    IL_276b:  box        [mscorlib]System.Int32
    IL_2770:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2775:  ldloc.s    V_9
    IL_2777:  ldloc.s    V_19
    IL_2779:  beq.s      IL_2796

    IL_277b:  ldc.i4.1
    IL_277c:  stloc.s    V_20
    IL_277e:  ldstr      "dummy64 check failed {0} {1}"
    IL_2783:  ldloc.s    V_9
    IL_2785:  box        [mscorlib]System.Int64
    IL_278a:  ldloc.s    V_19
    IL_278c:  box        [mscorlib]System.Int64
    IL_2791:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2796:  ldsfld     int32 IntrinsicTest::counter
    IL_279b:  ldsfld     int32 IntrinsicTest::id_counter
    IL_27a0:  beq.s      IL_27c3

    IL_27a2:  ldstr      "counter mismatch {0} {1}"
    IL_27a7:  ldsfld     int32 IntrinsicTest::counter
    IL_27ac:  box        [mscorlib]System.Int32
    IL_27b1:  ldsfld     int32 IntrinsicTest::id_counter
    IL_27b6:  box        [mscorlib]System.Int32
    IL_27bb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_27c0:  ldc.i4.1
    IL_27c1:  stloc.s    V_20
    IL_27c3:  ldsfld     int64 IntrinsicTest::counter64
    IL_27c8:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_27cd:  beq.s      IL_27f0

    IL_27cf:  ldstr      "counter64 mismatch {0} {1}"
    IL_27d4:  ldsfld     int64 IntrinsicTest::counter64
    IL_27d9:  box        [mscorlib]System.Int64
    IL_27de:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_27e3:  box        [mscorlib]System.Int64
    IL_27e8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_27ed:  ldc.i4.1
    IL_27ee:  stloc.s    V_20
    IL_27f0:  ldloc.s    V_21
    IL_27f2:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_27f7:  ldloc.s    V_21
    IL_27f9:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_27fe:  beq.s      IL_2825

    IL_2800:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_2805:  ldloc.s    V_21
    IL_2807:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_280c:  box        [mscorlib]System.Int32
    IL_2811:  ldloc.s    V_21
    IL_2813:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_2818:  box        [mscorlib]System.Int32
    IL_281d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2822:  ldc.i4.1
    IL_2823:  stloc.s    V_20
    IL_2825:  ldloc.s    V_21
    IL_2827:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_282c:  ldloc.s    V_21
    IL_282e:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_2833:  beq.s      IL_285a

    IL_2835:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_283a:  ldloc.s    V_21
    IL_283c:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_2841:  box        [mscorlib]System.Int64
    IL_2846:  ldloc.s    V_21
    IL_2848:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_284d:  box        [mscorlib]System.Int64
    IL_2852:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2857:  ldc.i4.1
    IL_2858:  stloc.s    V_20
    IL_285a:  ldsfld     int64 IntrinsicTest::temp
    IL_285f:  ldsfld     int64 IntrinsicTest::idmp
    IL_2864:  beq.s      IL_2887

    IL_2866:  ldstr      "temp mismatch {0} {1}"
    IL_286b:  ldsfld     int64 IntrinsicTest::temp
    IL_2870:  box        [mscorlib]System.Int64
    IL_2875:  ldsfld     int64 IntrinsicTest::idmp
    IL_287a:  box        [mscorlib]System.Int64
    IL_287f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2884:  ldc.i4.1
    IL_2885:  stloc.s    V_20
    IL_2887:  ldloc.s    V_21
    IL_2889:  ldsflda    int32 IntrinsicTest::counter
    IL_288e:  ldsfld     int32 IntrinsicTest::counter
    IL_2893:  ldloc.s    V_21
    IL_2895:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_289a:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_289f:  stfld      int32 IntrinsicTest::instanceCounter
    IL_28a4:  ldloc.s    V_21
    IL_28a6:  ldsfld     int32 IntrinsicTest::id_counter
    IL_28ab:  stfld      int32 IntrinsicTest::id_instanceCounter
    IL_28b0:  ldsfld     int32 IntrinsicTest::id_counter
    IL_28b5:  ldloc.s    V_21
    IL_28b7:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_28bc:  bne.un.s   IL_28ca

    IL_28be:  ldsfld     int32 IntrinsicTest::id_counter
    IL_28c3:  stsfld     int32 IntrinsicTest::id_counter
    IL_28c8:  br.s       IL_28d5

    IL_28ca:  ldsfld     int32 IntrinsicTest::id_counter
    IL_28cf:  conv.i8
    IL_28d0:  stsfld     int64 IntrinsicTest::idjunk
    IL_28d5:  ldstr      "------------------------------------CMPXCHG  1 1 2 2"
    IL_28da:  call       void [System.Console]System.Console::WriteLine(string)
    IL_28df:  ldloc.0
    IL_28e0:  ldloc.s    V_10
    IL_28e2:  beq.s      IL_28fe

    IL_28e4:  ldc.i4.1
    IL_28e5:  stloc.s    V_20
    IL_28e7:  ldstr      "te0 check failed {0} {1}"
    IL_28ec:  ldloc.0
    IL_28ed:  box        [mscorlib]System.Int32
    IL_28f2:  ldloc.s    V_10
    IL_28f4:  box        [mscorlib]System.Int32
    IL_28f9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_28fe:  ldloc.1
    IL_28ff:  ldloc.s    V_11
    IL_2901:  beq.s      IL_291d

    IL_2903:  ldc.i4.1
    IL_2904:  stloc.s    V_20
    IL_2906:  ldstr      "te1 check failed {0} {1}"
    IL_290b:  ldloc.1
    IL_290c:  box        [mscorlib]System.Int32
    IL_2911:  ldloc.s    V_11
    IL_2913:  box        [mscorlib]System.Int32
    IL_2918:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_291d:  ldloc.2
    IL_291e:  ldloc.s    V_12
    IL_2920:  beq.s      IL_293c

    IL_2922:  ldc.i4.1
    IL_2923:  stloc.s    V_20
    IL_2925:  ldstr      "te2 check failed {0} {1}"
    IL_292a:  ldloc.2
    IL_292b:  box        [mscorlib]System.Int32
    IL_2930:  ldloc.s    V_12
    IL_2932:  box        [mscorlib]System.Int32
    IL_2937:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_293c:  ldloc.3
    IL_293d:  ldloc.s    V_13
    IL_293f:  beq.s      IL_295b

    IL_2941:  ldc.i4.1
    IL_2942:  stloc.s    V_20
    IL_2944:  ldstr      "te3 check failed {0} {1}"
    IL_2949:  ldloc.3
    IL_294a:  box        [mscorlib]System.Int32
    IL_294f:  ldloc.s    V_13
    IL_2951:  box        [mscorlib]System.Int32
    IL_2956:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_295b:  ldloc.s    V_4
    IL_295d:  ldloc.s    V_14
    IL_295f:  beq.s      IL_297c

    IL_2961:  ldc.i4.1
    IL_2962:  stloc.s    V_20
    IL_2964:  ldstr      "te064 check failed {0} {1}"
    IL_2969:  ldloc.s    V_4
    IL_296b:  box        [mscorlib]System.Int64
    IL_2970:  ldloc.s    V_14
    IL_2972:  box        [mscorlib]System.Int64
    IL_2977:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_297c:  ldloc.s    V_5
    IL_297e:  ldloc.s    V_15
    IL_2980:  beq.s      IL_299d

    IL_2982:  ldc.i4.1
    IL_2983:  stloc.s    V_20
    IL_2985:  ldstr      "te164 check failed {0} {1}"
    IL_298a:  ldloc.s    V_5
    IL_298c:  box        [mscorlib]System.Int64
    IL_2991:  ldloc.s    V_15
    IL_2993:  box        [mscorlib]System.Int64
    IL_2998:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_299d:  ldloc.s    V_6
    IL_299f:  ldloc.s    V_16
    IL_29a1:  beq.s      IL_29be

    IL_29a3:  ldc.i4.1
    IL_29a4:  stloc.s    V_20
    IL_29a6:  ldstr      "te264 check failed {0} {1}"
    IL_29ab:  ldloc.s    V_6
    IL_29ad:  box        [mscorlib]System.Int64
    IL_29b2:  ldloc.s    V_16
    IL_29b4:  box        [mscorlib]System.Int64
    IL_29b9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_29be:  ldloc.s    V_7
    IL_29c0:  ldloc.s    V_17
    IL_29c2:  beq.s      IL_29df

    IL_29c4:  ldc.i4.1
    IL_29c5:  stloc.s    V_20
    IL_29c7:  ldstr      "te364 check failed {0} {1}"
    IL_29cc:  ldloc.s    V_7
    IL_29ce:  box        [mscorlib]System.Int64
    IL_29d3:  ldloc.s    V_17
    IL_29d5:  box        [mscorlib]System.Int64
    IL_29da:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_29df:  ldloc.s    V_8
    IL_29e1:  ldloc.s    V_18
    IL_29e3:  beq.s      IL_2a00

    IL_29e5:  ldc.i4.1
    IL_29e6:  stloc.s    V_20
    IL_29e8:  ldstr      "dummy check failed {0} {1}"
    IL_29ed:  ldloc.s    V_8
    IL_29ef:  box        [mscorlib]System.Int32
    IL_29f4:  ldloc.s    V_18
    IL_29f6:  box        [mscorlib]System.Int32
    IL_29fb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2a00:  ldloc.s    V_9
    IL_2a02:  ldloc.s    V_19
    IL_2a04:  beq.s      IL_2a21

    IL_2a06:  ldc.i4.1
    IL_2a07:  stloc.s    V_20
    IL_2a09:  ldstr      "dummy64 check failed {0} {1}"
    IL_2a0e:  ldloc.s    V_9
    IL_2a10:  box        [mscorlib]System.Int64
    IL_2a15:  ldloc.s    V_19
    IL_2a17:  box        [mscorlib]System.Int64
    IL_2a1c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2a21:  ldsfld     int32 IntrinsicTest::counter
    IL_2a26:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2a2b:  beq.s      IL_2a4e

    IL_2a2d:  ldstr      "counter mismatch {0} {1}"
    IL_2a32:  ldsfld     int32 IntrinsicTest::counter
    IL_2a37:  box        [mscorlib]System.Int32
    IL_2a3c:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2a41:  box        [mscorlib]System.Int32
    IL_2a46:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2a4b:  ldc.i4.1
    IL_2a4c:  stloc.s    V_20
    IL_2a4e:  ldsfld     int64 IntrinsicTest::counter64
    IL_2a53:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2a58:  beq.s      IL_2a7b

    IL_2a5a:  ldstr      "counter64 mismatch {0} {1}"
    IL_2a5f:  ldsfld     int64 IntrinsicTest::counter64
    IL_2a64:  box        [mscorlib]System.Int64
    IL_2a69:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2a6e:  box        [mscorlib]System.Int64
    IL_2a73:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2a78:  ldc.i4.1
    IL_2a79:  stloc.s    V_20
    IL_2a7b:  ldloc.s    V_21
    IL_2a7d:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_2a82:  ldloc.s    V_21
    IL_2a84:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_2a89:  beq.s      IL_2ab0

    IL_2a8b:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_2a90:  ldloc.s    V_21
    IL_2a92:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_2a97:  box        [mscorlib]System.Int32
    IL_2a9c:  ldloc.s    V_21
    IL_2a9e:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_2aa3:  box        [mscorlib]System.Int32
    IL_2aa8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2aad:  ldc.i4.1
    IL_2aae:  stloc.s    V_20
    IL_2ab0:  ldloc.s    V_21
    IL_2ab2:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_2ab7:  ldloc.s    V_21
    IL_2ab9:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_2abe:  beq.s      IL_2ae5

    IL_2ac0:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_2ac5:  ldloc.s    V_21
    IL_2ac7:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_2acc:  box        [mscorlib]System.Int64
    IL_2ad1:  ldloc.s    V_21
    IL_2ad3:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_2ad8:  box        [mscorlib]System.Int64
    IL_2add:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2ae2:  ldc.i4.1
    IL_2ae3:  stloc.s    V_20
    IL_2ae5:  ldsfld     int64 IntrinsicTest::temp
    IL_2aea:  ldsfld     int64 IntrinsicTest::idmp
    IL_2aef:  beq.s      IL_2b12

    IL_2af1:  ldstr      "temp mismatch {0} {1}"
    IL_2af6:  ldsfld     int64 IntrinsicTest::temp
    IL_2afb:  box        [mscorlib]System.Int64
    IL_2b00:  ldsfld     int64 IntrinsicTest::idmp
    IL_2b05:  box        [mscorlib]System.Int64
    IL_2b0a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2b0f:  ldc.i4.1
    IL_2b10:  stloc.s    V_20
    IL_2b12:  ldsflda    int32 IntrinsicTest::counter
    IL_2b17:  ldsfld     int32 IntrinsicTest::counter
    IL_2b1c:  ldloc.s    V_21
    IL_2b1e:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_2b23:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_2b28:  stloc.0
    IL_2b29:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2b2e:  stloc.s    V_10
    IL_2b30:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2b35:  ldloc.s    V_21
    IL_2b37:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_2b3c:  bne.un.s   IL_2b4a

    IL_2b3e:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2b43:  stsfld     int32 IntrinsicTest::id_counter
    IL_2b48:  br.s       IL_2b55

    IL_2b4a:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2b4f:  conv.i8
    IL_2b50:  stsfld     int64 IntrinsicTest::idjunk
    IL_2b55:  ldstr      "------------------------------------CMPXCHG  1 1 2 3"
    IL_2b5a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_2b5f:  ldloc.0
    IL_2b60:  ldloc.s    V_10
    IL_2b62:  beq.s      IL_2b7e

    IL_2b64:  ldc.i4.1
    IL_2b65:  stloc.s    V_20
    IL_2b67:  ldstr      "te0 check failed {0} {1}"
    IL_2b6c:  ldloc.0
    IL_2b6d:  box        [mscorlib]System.Int32
    IL_2b72:  ldloc.s    V_10
    IL_2b74:  box        [mscorlib]System.Int32
    IL_2b79:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2b7e:  ldloc.1
    IL_2b7f:  ldloc.s    V_11
    IL_2b81:  beq.s      IL_2b9d

    IL_2b83:  ldc.i4.1
    IL_2b84:  stloc.s    V_20
    IL_2b86:  ldstr      "te1 check failed {0} {1}"
    IL_2b8b:  ldloc.1
    IL_2b8c:  box        [mscorlib]System.Int32
    IL_2b91:  ldloc.s    V_11
    IL_2b93:  box        [mscorlib]System.Int32
    IL_2b98:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2b9d:  ldloc.2
    IL_2b9e:  ldloc.s    V_12
    IL_2ba0:  beq.s      IL_2bbc

    IL_2ba2:  ldc.i4.1
    IL_2ba3:  stloc.s    V_20
    IL_2ba5:  ldstr      "te2 check failed {0} {1}"
    IL_2baa:  ldloc.2
    IL_2bab:  box        [mscorlib]System.Int32
    IL_2bb0:  ldloc.s    V_12
    IL_2bb2:  box        [mscorlib]System.Int32
    IL_2bb7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2bbc:  ldloc.3
    IL_2bbd:  ldloc.s    V_13
    IL_2bbf:  beq.s      IL_2bdb

    IL_2bc1:  ldc.i4.1
    IL_2bc2:  stloc.s    V_20
    IL_2bc4:  ldstr      "te3 check failed {0} {1}"
    IL_2bc9:  ldloc.3
    IL_2bca:  box        [mscorlib]System.Int32
    IL_2bcf:  ldloc.s    V_13
    IL_2bd1:  box        [mscorlib]System.Int32
    IL_2bd6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2bdb:  ldloc.s    V_4
    IL_2bdd:  ldloc.s    V_14
    IL_2bdf:  beq.s      IL_2bfc

    IL_2be1:  ldc.i4.1
    IL_2be2:  stloc.s    V_20
    IL_2be4:  ldstr      "te064 check failed {0} {1}"
    IL_2be9:  ldloc.s    V_4
    IL_2beb:  box        [mscorlib]System.Int64
    IL_2bf0:  ldloc.s    V_14
    IL_2bf2:  box        [mscorlib]System.Int64
    IL_2bf7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2bfc:  ldloc.s    V_5
    IL_2bfe:  ldloc.s    V_15
    IL_2c00:  beq.s      IL_2c1d

    IL_2c02:  ldc.i4.1
    IL_2c03:  stloc.s    V_20
    IL_2c05:  ldstr      "te164 check failed {0} {1}"
    IL_2c0a:  ldloc.s    V_5
    IL_2c0c:  box        [mscorlib]System.Int64
    IL_2c11:  ldloc.s    V_15
    IL_2c13:  box        [mscorlib]System.Int64
    IL_2c18:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2c1d:  ldloc.s    V_6
    IL_2c1f:  ldloc.s    V_16
    IL_2c21:  beq.s      IL_2c3e

    IL_2c23:  ldc.i4.1
    IL_2c24:  stloc.s    V_20
    IL_2c26:  ldstr      "te264 check failed {0} {1}"
    IL_2c2b:  ldloc.s    V_6
    IL_2c2d:  box        [mscorlib]System.Int64
    IL_2c32:  ldloc.s    V_16
    IL_2c34:  box        [mscorlib]System.Int64
    IL_2c39:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2c3e:  ldloc.s    V_7
    IL_2c40:  ldloc.s    V_17
    IL_2c42:  beq.s      IL_2c5f

    IL_2c44:  ldc.i4.1
    IL_2c45:  stloc.s    V_20
    IL_2c47:  ldstr      "te364 check failed {0} {1}"
    IL_2c4c:  ldloc.s    V_7
    IL_2c4e:  box        [mscorlib]System.Int64
    IL_2c53:  ldloc.s    V_17
    IL_2c55:  box        [mscorlib]System.Int64
    IL_2c5a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2c5f:  ldloc.s    V_8
    IL_2c61:  ldloc.s    V_18
    IL_2c63:  beq.s      IL_2c80

    IL_2c65:  ldc.i4.1
    IL_2c66:  stloc.s    V_20
    IL_2c68:  ldstr      "dummy check failed {0} {1}"
    IL_2c6d:  ldloc.s    V_8
    IL_2c6f:  box        [mscorlib]System.Int32
    IL_2c74:  ldloc.s    V_18
    IL_2c76:  box        [mscorlib]System.Int32
    IL_2c7b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2c80:  ldloc.s    V_9
    IL_2c82:  ldloc.s    V_19
    IL_2c84:  beq.s      IL_2ca1

    IL_2c86:  ldc.i4.1
    IL_2c87:  stloc.s    V_20
    IL_2c89:  ldstr      "dummy64 check failed {0} {1}"
    IL_2c8e:  ldloc.s    V_9
    IL_2c90:  box        [mscorlib]System.Int64
    IL_2c95:  ldloc.s    V_19
    IL_2c97:  box        [mscorlib]System.Int64
    IL_2c9c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2ca1:  ldsfld     int32 IntrinsicTest::counter
    IL_2ca6:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2cab:  beq.s      IL_2cce

    IL_2cad:  ldstr      "counter mismatch {0} {1}"
    IL_2cb2:  ldsfld     int32 IntrinsicTest::counter
    IL_2cb7:  box        [mscorlib]System.Int32
    IL_2cbc:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2cc1:  box        [mscorlib]System.Int32
    IL_2cc6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2ccb:  ldc.i4.1
    IL_2ccc:  stloc.s    V_20
    IL_2cce:  ldsfld     int64 IntrinsicTest::counter64
    IL_2cd3:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2cd8:  beq.s      IL_2cfb

    IL_2cda:  ldstr      "counter64 mismatch {0} {1}"
    IL_2cdf:  ldsfld     int64 IntrinsicTest::counter64
    IL_2ce4:  box        [mscorlib]System.Int64
    IL_2ce9:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2cee:  box        [mscorlib]System.Int64
    IL_2cf3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2cf8:  ldc.i4.1
    IL_2cf9:  stloc.s    V_20
    IL_2cfb:  ldloc.s    V_21
    IL_2cfd:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_2d02:  ldloc.s    V_21
    IL_2d04:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_2d09:  beq.s      IL_2d30

    IL_2d0b:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_2d10:  ldloc.s    V_21
    IL_2d12:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_2d17:  box        [mscorlib]System.Int32
    IL_2d1c:  ldloc.s    V_21
    IL_2d1e:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_2d23:  box        [mscorlib]System.Int32
    IL_2d28:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2d2d:  ldc.i4.1
    IL_2d2e:  stloc.s    V_20
    IL_2d30:  ldloc.s    V_21
    IL_2d32:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_2d37:  ldloc.s    V_21
    IL_2d39:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_2d3e:  beq.s      IL_2d65

    IL_2d40:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_2d45:  ldloc.s    V_21
    IL_2d47:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_2d4c:  box        [mscorlib]System.Int64
    IL_2d51:  ldloc.s    V_21
    IL_2d53:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_2d58:  box        [mscorlib]System.Int64
    IL_2d5d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2d62:  ldc.i4.1
    IL_2d63:  stloc.s    V_20
    IL_2d65:  ldsfld     int64 IntrinsicTest::temp
    IL_2d6a:  ldsfld     int64 IntrinsicTest::idmp
    IL_2d6f:  beq.s      IL_2d92

    IL_2d71:  ldstr      "temp mismatch {0} {1}"
    IL_2d76:  ldsfld     int64 IntrinsicTest::temp
    IL_2d7b:  box        [mscorlib]System.Int64
    IL_2d80:  ldsfld     int64 IntrinsicTest::idmp
    IL_2d85:  box        [mscorlib]System.Int64
    IL_2d8a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2d8f:  ldc.i4.1
    IL_2d90:  stloc.s    V_20
    IL_2d92:  ldsflda    int32 IntrinsicTest::counter
    IL_2d97:  ldsfld     int32 IntrinsicTest::counter
    IL_2d9c:  ldloc.s    V_21
    IL_2d9e:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_2da3:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_2da8:  stsfld     int32 IntrinsicTest::counter
    IL_2dad:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2db2:  stsfld     int32 IntrinsicTest::id_counter
    IL_2db7:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2dbc:  ldloc.s    V_21
    IL_2dbe:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_2dc3:  bne.un.s   IL_2dd2

    IL_2dc5:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2dca:  conv.i8
    IL_2dcb:  stsfld     int64 IntrinsicTest::idjunk
    IL_2dd0:  br.s       IL_2ddd

    IL_2dd2:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2dd7:  conv.i8
    IL_2dd8:  stsfld     int64 IntrinsicTest::idjunk
    IL_2ddd:  ldstr      "------------------------------------CMPXCHG  1 1 2 4"
    IL_2de2:  call       void [System.Console]System.Console::WriteLine(string)
    IL_2de7:  ldloc.0
    IL_2de8:  ldloc.s    V_10
    IL_2dea:  beq.s      IL_2e06

    IL_2dec:  ldc.i4.1
    IL_2ded:  stloc.s    V_20
    IL_2def:  ldstr      "te0 check failed {0} {1}"
    IL_2df4:  ldloc.0
    IL_2df5:  box        [mscorlib]System.Int32
    IL_2dfa:  ldloc.s    V_10
    IL_2dfc:  box        [mscorlib]System.Int32
    IL_2e01:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2e06:  ldloc.1
    IL_2e07:  ldloc.s    V_11
    IL_2e09:  beq.s      IL_2e25

    IL_2e0b:  ldc.i4.1
    IL_2e0c:  stloc.s    V_20
    IL_2e0e:  ldstr      "te1 check failed {0} {1}"
    IL_2e13:  ldloc.1
    IL_2e14:  box        [mscorlib]System.Int32
    IL_2e19:  ldloc.s    V_11
    IL_2e1b:  box        [mscorlib]System.Int32
    IL_2e20:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2e25:  ldloc.2
    IL_2e26:  ldloc.s    V_12
    IL_2e28:  beq.s      IL_2e44

    IL_2e2a:  ldc.i4.1
    IL_2e2b:  stloc.s    V_20
    IL_2e2d:  ldstr      "te2 check failed {0} {1}"
    IL_2e32:  ldloc.2
    IL_2e33:  box        [mscorlib]System.Int32
    IL_2e38:  ldloc.s    V_12
    IL_2e3a:  box        [mscorlib]System.Int32
    IL_2e3f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2e44:  ldloc.3
    IL_2e45:  ldloc.s    V_13
    IL_2e47:  beq.s      IL_2e63

    IL_2e49:  ldc.i4.1
    IL_2e4a:  stloc.s    V_20
    IL_2e4c:  ldstr      "te3 check failed {0} {1}"
    IL_2e51:  ldloc.3
    IL_2e52:  box        [mscorlib]System.Int32
    IL_2e57:  ldloc.s    V_13
    IL_2e59:  box        [mscorlib]System.Int32
    IL_2e5e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2e63:  ldloc.s    V_4
    IL_2e65:  ldloc.s    V_14
    IL_2e67:  beq.s      IL_2e84

    IL_2e69:  ldc.i4.1
    IL_2e6a:  stloc.s    V_20
    IL_2e6c:  ldstr      "te064 check failed {0} {1}"
    IL_2e71:  ldloc.s    V_4
    IL_2e73:  box        [mscorlib]System.Int64
    IL_2e78:  ldloc.s    V_14
    IL_2e7a:  box        [mscorlib]System.Int64
    IL_2e7f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2e84:  ldloc.s    V_5
    IL_2e86:  ldloc.s    V_15
    IL_2e88:  beq.s      IL_2ea5

    IL_2e8a:  ldc.i4.1
    IL_2e8b:  stloc.s    V_20
    IL_2e8d:  ldstr      "te164 check failed {0} {1}"
    IL_2e92:  ldloc.s    V_5
    IL_2e94:  box        [mscorlib]System.Int64
    IL_2e99:  ldloc.s    V_15
    IL_2e9b:  box        [mscorlib]System.Int64
    IL_2ea0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2ea5:  ldloc.s    V_6
    IL_2ea7:  ldloc.s    V_16
    IL_2ea9:  beq.s      IL_2ec6

    IL_2eab:  ldc.i4.1
    IL_2eac:  stloc.s    V_20
    IL_2eae:  ldstr      "te264 check failed {0} {1}"
    IL_2eb3:  ldloc.s    V_6
    IL_2eb5:  box        [mscorlib]System.Int64
    IL_2eba:  ldloc.s    V_16
    IL_2ebc:  box        [mscorlib]System.Int64
    IL_2ec1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2ec6:  ldloc.s    V_7
    IL_2ec8:  ldloc.s    V_17
    IL_2eca:  beq.s      IL_2ee7

    IL_2ecc:  ldc.i4.1
    IL_2ecd:  stloc.s    V_20
    IL_2ecf:  ldstr      "te364 check failed {0} {1}"
    IL_2ed4:  ldloc.s    V_7
    IL_2ed6:  box        [mscorlib]System.Int64
    IL_2edb:  ldloc.s    V_17
    IL_2edd:  box        [mscorlib]System.Int64
    IL_2ee2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2ee7:  ldloc.s    V_8
    IL_2ee9:  ldloc.s    V_18
    IL_2eeb:  beq.s      IL_2f08

    IL_2eed:  ldc.i4.1
    IL_2eee:  stloc.s    V_20
    IL_2ef0:  ldstr      "dummy check failed {0} {1}"
    IL_2ef5:  ldloc.s    V_8
    IL_2ef7:  box        [mscorlib]System.Int32
    IL_2efc:  ldloc.s    V_18
    IL_2efe:  box        [mscorlib]System.Int32
    IL_2f03:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2f08:  ldloc.s    V_9
    IL_2f0a:  ldloc.s    V_19
    IL_2f0c:  beq.s      IL_2f29

    IL_2f0e:  ldc.i4.1
    IL_2f0f:  stloc.s    V_20
    IL_2f11:  ldstr      "dummy64 check failed {0} {1}"
    IL_2f16:  ldloc.s    V_9
    IL_2f18:  box        [mscorlib]System.Int64
    IL_2f1d:  ldloc.s    V_19
    IL_2f1f:  box        [mscorlib]System.Int64
    IL_2f24:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2f29:  ldsfld     int32 IntrinsicTest::counter
    IL_2f2e:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2f33:  beq.s      IL_2f56

    IL_2f35:  ldstr      "counter mismatch {0} {1}"
    IL_2f3a:  ldsfld     int32 IntrinsicTest::counter
    IL_2f3f:  box        [mscorlib]System.Int32
    IL_2f44:  ldsfld     int32 IntrinsicTest::id_counter
    IL_2f49:  box        [mscorlib]System.Int32
    IL_2f4e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2f53:  ldc.i4.1
    IL_2f54:  stloc.s    V_20
    IL_2f56:  ldsfld     int64 IntrinsicTest::counter64
    IL_2f5b:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2f60:  beq.s      IL_2f83

    IL_2f62:  ldstr      "counter64 mismatch {0} {1}"
    IL_2f67:  ldsfld     int64 IntrinsicTest::counter64
    IL_2f6c:  box        [mscorlib]System.Int64
    IL_2f71:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_2f76:  box        [mscorlib]System.Int64
    IL_2f7b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2f80:  ldc.i4.1
    IL_2f81:  stloc.s    V_20
    IL_2f83:  ldloc.s    V_21
    IL_2f85:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_2f8a:  ldloc.s    V_21
    IL_2f8c:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_2f91:  beq.s      IL_2fb8

    IL_2f93:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_2f98:  ldloc.s    V_21
    IL_2f9a:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_2f9f:  box        [mscorlib]System.Int32
    IL_2fa4:  ldloc.s    V_21
    IL_2fa6:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_2fab:  box        [mscorlib]System.Int32
    IL_2fb0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2fb5:  ldc.i4.1
    IL_2fb6:  stloc.s    V_20
    IL_2fb8:  ldloc.s    V_21
    IL_2fba:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_2fbf:  ldloc.s    V_21
    IL_2fc1:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_2fc6:  beq.s      IL_2fed

    IL_2fc8:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_2fcd:  ldloc.s    V_21
    IL_2fcf:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_2fd4:  box        [mscorlib]System.Int64
    IL_2fd9:  ldloc.s    V_21
    IL_2fdb:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_2fe0:  box        [mscorlib]System.Int64
    IL_2fe5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_2fea:  ldc.i4.1
    IL_2feb:  stloc.s    V_20
    IL_2fed:  ldsfld     int64 IntrinsicTest::temp
    IL_2ff2:  ldsfld     int64 IntrinsicTest::idmp
    IL_2ff7:  beq.s      IL_301a

    IL_2ff9:  ldstr      "temp mismatch {0} {1}"
    IL_2ffe:  ldsfld     int64 IntrinsicTest::temp
    IL_3003:  box        [mscorlib]System.Int64
    IL_3008:  ldsfld     int64 IntrinsicTest::idmp
    IL_300d:  box        [mscorlib]System.Int64
    IL_3012:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3017:  ldc.i4.1
    IL_3018:  stloc.s    V_20
    IL_301a:  ldloc.s    V_21
    IL_301c:  ldsflda    int32 IntrinsicTest::counter
    IL_3021:  ldsfld     int32 IntrinsicTest::counter
    IL_3026:  ldloc.s    V_21
    IL_3028:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_302d:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_3032:  stfld      int32 IntrinsicTest::instanceCounter
    IL_3037:  ldloc.s    V_21
    IL_3039:  ldsfld     int32 IntrinsicTest::id_counter
    IL_303e:  stfld      int32 IntrinsicTest::id_instanceCounter
    IL_3043:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3048:  ldloc.s    V_21
    IL_304a:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_304f:  bne.un.s   IL_305d

    IL_3051:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3056:  stsfld     int32 IntrinsicTest::id_counter
    IL_305b:  br.s       IL_3068

    IL_305d:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3062:  conv.i8
    IL_3063:  stsfld     int64 IntrinsicTest::idjunk
    IL_3068:  ldstr      "------------------------------------CMPXCHG  1 1 2 5"
    IL_306d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_3072:  ldloc.0
    IL_3073:  ldloc.s    V_10
    IL_3075:  beq.s      IL_3091

    IL_3077:  ldc.i4.1
    IL_3078:  stloc.s    V_20
    IL_307a:  ldstr      "te0 check failed {0} {1}"
    IL_307f:  ldloc.0
    IL_3080:  box        [mscorlib]System.Int32
    IL_3085:  ldloc.s    V_10
    IL_3087:  box        [mscorlib]System.Int32
    IL_308c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3091:  ldloc.1
    IL_3092:  ldloc.s    V_11
    IL_3094:  beq.s      IL_30b0

    IL_3096:  ldc.i4.1
    IL_3097:  stloc.s    V_20
    IL_3099:  ldstr      "te1 check failed {0} {1}"
    IL_309e:  ldloc.1
    IL_309f:  box        [mscorlib]System.Int32
    IL_30a4:  ldloc.s    V_11
    IL_30a6:  box        [mscorlib]System.Int32
    IL_30ab:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_30b0:  ldloc.2
    IL_30b1:  ldloc.s    V_12
    IL_30b3:  beq.s      IL_30cf

    IL_30b5:  ldc.i4.1
    IL_30b6:  stloc.s    V_20
    IL_30b8:  ldstr      "te2 check failed {0} {1}"
    IL_30bd:  ldloc.2
    IL_30be:  box        [mscorlib]System.Int32
    IL_30c3:  ldloc.s    V_12
    IL_30c5:  box        [mscorlib]System.Int32
    IL_30ca:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_30cf:  ldloc.3
    IL_30d0:  ldloc.s    V_13
    IL_30d2:  beq.s      IL_30ee

    IL_30d4:  ldc.i4.1
    IL_30d5:  stloc.s    V_20
    IL_30d7:  ldstr      "te3 check failed {0} {1}"
    IL_30dc:  ldloc.3
    IL_30dd:  box        [mscorlib]System.Int32
    IL_30e2:  ldloc.s    V_13
    IL_30e4:  box        [mscorlib]System.Int32
    IL_30e9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_30ee:  ldloc.s    V_4
    IL_30f0:  ldloc.s    V_14
    IL_30f2:  beq.s      IL_310f

    IL_30f4:  ldc.i4.1
    IL_30f5:  stloc.s    V_20
    IL_30f7:  ldstr      "te064 check failed {0} {1}"
    IL_30fc:  ldloc.s    V_4
    IL_30fe:  box        [mscorlib]System.Int64
    IL_3103:  ldloc.s    V_14
    IL_3105:  box        [mscorlib]System.Int64
    IL_310a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_310f:  ldloc.s    V_5
    IL_3111:  ldloc.s    V_15
    IL_3113:  beq.s      IL_3130

    IL_3115:  ldc.i4.1
    IL_3116:  stloc.s    V_20
    IL_3118:  ldstr      "te164 check failed {0} {1}"
    IL_311d:  ldloc.s    V_5
    IL_311f:  box        [mscorlib]System.Int64
    IL_3124:  ldloc.s    V_15
    IL_3126:  box        [mscorlib]System.Int64
    IL_312b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3130:  ldloc.s    V_6
    IL_3132:  ldloc.s    V_16
    IL_3134:  beq.s      IL_3151

    IL_3136:  ldc.i4.1
    IL_3137:  stloc.s    V_20
    IL_3139:  ldstr      "te264 check failed {0} {1}"
    IL_313e:  ldloc.s    V_6
    IL_3140:  box        [mscorlib]System.Int64
    IL_3145:  ldloc.s    V_16
    IL_3147:  box        [mscorlib]System.Int64
    IL_314c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3151:  ldloc.s    V_7
    IL_3153:  ldloc.s    V_17
    IL_3155:  beq.s      IL_3172

    IL_3157:  ldc.i4.1
    IL_3158:  stloc.s    V_20
    IL_315a:  ldstr      "te364 check failed {0} {1}"
    IL_315f:  ldloc.s    V_7
    IL_3161:  box        [mscorlib]System.Int64
    IL_3166:  ldloc.s    V_17
    IL_3168:  box        [mscorlib]System.Int64
    IL_316d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3172:  ldloc.s    V_8
    IL_3174:  ldloc.s    V_18
    IL_3176:  beq.s      IL_3193

    IL_3178:  ldc.i4.1
    IL_3179:  stloc.s    V_20
    IL_317b:  ldstr      "dummy check failed {0} {1}"
    IL_3180:  ldloc.s    V_8
    IL_3182:  box        [mscorlib]System.Int32
    IL_3187:  ldloc.s    V_18
    IL_3189:  box        [mscorlib]System.Int32
    IL_318e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3193:  ldloc.s    V_9
    IL_3195:  ldloc.s    V_19
    IL_3197:  beq.s      IL_31b4

    IL_3199:  ldc.i4.1
    IL_319a:  stloc.s    V_20
    IL_319c:  ldstr      "dummy64 check failed {0} {1}"
    IL_31a1:  ldloc.s    V_9
    IL_31a3:  box        [mscorlib]System.Int64
    IL_31a8:  ldloc.s    V_19
    IL_31aa:  box        [mscorlib]System.Int64
    IL_31af:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_31b4:  ldsfld     int32 IntrinsicTest::counter
    IL_31b9:  ldsfld     int32 IntrinsicTest::id_counter
    IL_31be:  beq.s      IL_31e1

    IL_31c0:  ldstr      "counter mismatch {0} {1}"
    IL_31c5:  ldsfld     int32 IntrinsicTest::counter
    IL_31ca:  box        [mscorlib]System.Int32
    IL_31cf:  ldsfld     int32 IntrinsicTest::id_counter
    IL_31d4:  box        [mscorlib]System.Int32
    IL_31d9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_31de:  ldc.i4.1
    IL_31df:  stloc.s    V_20
    IL_31e1:  ldsfld     int64 IntrinsicTest::counter64
    IL_31e6:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_31eb:  beq.s      IL_320e

    IL_31ed:  ldstr      "counter64 mismatch {0} {1}"
    IL_31f2:  ldsfld     int64 IntrinsicTest::counter64
    IL_31f7:  box        [mscorlib]System.Int64
    IL_31fc:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3201:  box        [mscorlib]System.Int64
    IL_3206:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_320b:  ldc.i4.1
    IL_320c:  stloc.s    V_20
    IL_320e:  ldloc.s    V_21
    IL_3210:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_3215:  ldloc.s    V_21
    IL_3217:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_321c:  beq.s      IL_3243

    IL_321e:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_3223:  ldloc.s    V_21
    IL_3225:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_322a:  box        [mscorlib]System.Int32
    IL_322f:  ldloc.s    V_21
    IL_3231:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_3236:  box        [mscorlib]System.Int32
    IL_323b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3240:  ldc.i4.1
    IL_3241:  stloc.s    V_20
    IL_3243:  ldloc.s    V_21
    IL_3245:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_324a:  ldloc.s    V_21
    IL_324c:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_3251:  beq.s      IL_3278

    IL_3253:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_3258:  ldloc.s    V_21
    IL_325a:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_325f:  box        [mscorlib]System.Int64
    IL_3264:  ldloc.s    V_21
    IL_3266:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_326b:  box        [mscorlib]System.Int64
    IL_3270:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3275:  ldc.i4.1
    IL_3276:  stloc.s    V_20
    IL_3278:  ldsfld     int64 IntrinsicTest::temp
    IL_327d:  ldsfld     int64 IntrinsicTest::idmp
    IL_3282:  beq.s      IL_32a5

    IL_3284:  ldstr      "temp mismatch {0} {1}"
    IL_3289:  ldsfld     int64 IntrinsicTest::temp
    IL_328e:  box        [mscorlib]System.Int64
    IL_3293:  ldsfld     int64 IntrinsicTest::idmp
    IL_3298:  box        [mscorlib]System.Int64
    IL_329d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_32a2:  ldc.i4.1
    IL_32a3:  stloc.s    V_20
    IL_32a5:  ldsflda    int32 IntrinsicTest::counter
    IL_32aa:  ldsfld     int32 IntrinsicTest::counter
    IL_32af:  ldloc.s    V_21
    IL_32b1:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_32b6:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_32bb:  stloc.3
    IL_32bc:  ldsfld     int32 IntrinsicTest::id_counter
    IL_32c1:  stloc.s    V_13
    IL_32c3:  ldsfld     int32 IntrinsicTest::id_counter
    IL_32c8:  ldloc.s    V_21
    IL_32ca:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_32cf:  bne.un.s   IL_32dd

    IL_32d1:  ldsfld     int32 IntrinsicTest::id_counter
    IL_32d6:  stsfld     int32 IntrinsicTest::id_counter
    IL_32db:  br.s       IL_32e8

    IL_32dd:  ldsfld     int32 IntrinsicTest::id_counter
    IL_32e2:  conv.i8
    IL_32e3:  stsfld     int64 IntrinsicTest::idjunk
    IL_32e8:  ldstr      "------------------------------------CMPXCHG  1 1 2 6"
    IL_32ed:  call       void [System.Console]System.Console::WriteLine(string)
    IL_32f2:  ldloc.0
    IL_32f3:  ldloc.s    V_10
    IL_32f5:  beq.s      IL_3311

    IL_32f7:  ldc.i4.1
    IL_32f8:  stloc.s    V_20
    IL_32fa:  ldstr      "te0 check failed {0} {1}"
    IL_32ff:  ldloc.0
    IL_3300:  box        [mscorlib]System.Int32
    IL_3305:  ldloc.s    V_10
    IL_3307:  box        [mscorlib]System.Int32
    IL_330c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3311:  ldloc.1
    IL_3312:  ldloc.s    V_11
    IL_3314:  beq.s      IL_3330

    IL_3316:  ldc.i4.1
    IL_3317:  stloc.s    V_20
    IL_3319:  ldstr      "te1 check failed {0} {1}"
    IL_331e:  ldloc.1
    IL_331f:  box        [mscorlib]System.Int32
    IL_3324:  ldloc.s    V_11
    IL_3326:  box        [mscorlib]System.Int32
    IL_332b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3330:  ldloc.2
    IL_3331:  ldloc.s    V_12
    IL_3333:  beq.s      IL_334f

    IL_3335:  ldc.i4.1
    IL_3336:  stloc.s    V_20
    IL_3338:  ldstr      "te2 check failed {0} {1}"
    IL_333d:  ldloc.2
    IL_333e:  box        [mscorlib]System.Int32
    IL_3343:  ldloc.s    V_12
    IL_3345:  box        [mscorlib]System.Int32
    IL_334a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_334f:  ldloc.3
    IL_3350:  ldloc.s    V_13
    IL_3352:  beq.s      IL_336e

    IL_3354:  ldc.i4.1
    IL_3355:  stloc.s    V_20
    IL_3357:  ldstr      "te3 check failed {0} {1}"
    IL_335c:  ldloc.3
    IL_335d:  box        [mscorlib]System.Int32
    IL_3362:  ldloc.s    V_13
    IL_3364:  box        [mscorlib]System.Int32
    IL_3369:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_336e:  ldloc.s    V_4
    IL_3370:  ldloc.s    V_14
    IL_3372:  beq.s      IL_338f

    IL_3374:  ldc.i4.1
    IL_3375:  stloc.s    V_20
    IL_3377:  ldstr      "te064 check failed {0} {1}"
    IL_337c:  ldloc.s    V_4
    IL_337e:  box        [mscorlib]System.Int64
    IL_3383:  ldloc.s    V_14
    IL_3385:  box        [mscorlib]System.Int64
    IL_338a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_338f:  ldloc.s    V_5
    IL_3391:  ldloc.s    V_15
    IL_3393:  beq.s      IL_33b0

    IL_3395:  ldc.i4.1
    IL_3396:  stloc.s    V_20
    IL_3398:  ldstr      "te164 check failed {0} {1}"
    IL_339d:  ldloc.s    V_5
    IL_339f:  box        [mscorlib]System.Int64
    IL_33a4:  ldloc.s    V_15
    IL_33a6:  box        [mscorlib]System.Int64
    IL_33ab:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_33b0:  ldloc.s    V_6
    IL_33b2:  ldloc.s    V_16
    IL_33b4:  beq.s      IL_33d1

    IL_33b6:  ldc.i4.1
    IL_33b7:  stloc.s    V_20
    IL_33b9:  ldstr      "te264 check failed {0} {1}"
    IL_33be:  ldloc.s    V_6
    IL_33c0:  box        [mscorlib]System.Int64
    IL_33c5:  ldloc.s    V_16
    IL_33c7:  box        [mscorlib]System.Int64
    IL_33cc:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_33d1:  ldloc.s    V_7
    IL_33d3:  ldloc.s    V_17
    IL_33d5:  beq.s      IL_33f2

    IL_33d7:  ldc.i4.1
    IL_33d8:  stloc.s    V_20
    IL_33da:  ldstr      "te364 check failed {0} {1}"
    IL_33df:  ldloc.s    V_7
    IL_33e1:  box        [mscorlib]System.Int64
    IL_33e6:  ldloc.s    V_17
    IL_33e8:  box        [mscorlib]System.Int64
    IL_33ed:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_33f2:  ldloc.s    V_8
    IL_33f4:  ldloc.s    V_18
    IL_33f6:  beq.s      IL_3413

    IL_33f8:  ldc.i4.1
    IL_33f9:  stloc.s    V_20
    IL_33fb:  ldstr      "dummy check failed {0} {1}"
    IL_3400:  ldloc.s    V_8
    IL_3402:  box        [mscorlib]System.Int32
    IL_3407:  ldloc.s    V_18
    IL_3409:  box        [mscorlib]System.Int32
    IL_340e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3413:  ldloc.s    V_9
    IL_3415:  ldloc.s    V_19
    IL_3417:  beq.s      IL_3434

    IL_3419:  ldc.i4.1
    IL_341a:  stloc.s    V_20
    IL_341c:  ldstr      "dummy64 check failed {0} {1}"
    IL_3421:  ldloc.s    V_9
    IL_3423:  box        [mscorlib]System.Int64
    IL_3428:  ldloc.s    V_19
    IL_342a:  box        [mscorlib]System.Int64
    IL_342f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3434:  ldsfld     int32 IntrinsicTest::counter
    IL_3439:  ldsfld     int32 IntrinsicTest::id_counter
    IL_343e:  beq.s      IL_3461

    IL_3440:  ldstr      "counter mismatch {0} {1}"
    IL_3445:  ldsfld     int32 IntrinsicTest::counter
    IL_344a:  box        [mscorlib]System.Int32
    IL_344f:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3454:  box        [mscorlib]System.Int32
    IL_3459:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_345e:  ldc.i4.1
    IL_345f:  stloc.s    V_20
    IL_3461:  ldsfld     int64 IntrinsicTest::counter64
    IL_3466:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_346b:  beq.s      IL_348e

    IL_346d:  ldstr      "counter64 mismatch {0} {1}"
    IL_3472:  ldsfld     int64 IntrinsicTest::counter64
    IL_3477:  box        [mscorlib]System.Int64
    IL_347c:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3481:  box        [mscorlib]System.Int64
    IL_3486:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_348b:  ldc.i4.1
    IL_348c:  stloc.s    V_20
    IL_348e:  ldloc.s    V_21
    IL_3490:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_3495:  ldloc.s    V_21
    IL_3497:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_349c:  beq.s      IL_34c3

    IL_349e:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_34a3:  ldloc.s    V_21
    IL_34a5:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_34aa:  box        [mscorlib]System.Int32
    IL_34af:  ldloc.s    V_21
    IL_34b1:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_34b6:  box        [mscorlib]System.Int32
    IL_34bb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_34c0:  ldc.i4.1
    IL_34c1:  stloc.s    V_20
    IL_34c3:  ldloc.s    V_21
    IL_34c5:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_34ca:  ldloc.s    V_21
    IL_34cc:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_34d1:  beq.s      IL_34f8

    IL_34d3:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_34d8:  ldloc.s    V_21
    IL_34da:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_34df:  box        [mscorlib]System.Int64
    IL_34e4:  ldloc.s    V_21
    IL_34e6:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_34eb:  box        [mscorlib]System.Int64
    IL_34f0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_34f5:  ldc.i4.1
    IL_34f6:  stloc.s    V_20
    IL_34f8:  ldsfld     int64 IntrinsicTest::temp
    IL_34fd:  ldsfld     int64 IntrinsicTest::idmp
    IL_3502:  beq.s      IL_3525

    IL_3504:  ldstr      "temp mismatch {0} {1}"
    IL_3509:  ldsfld     int64 IntrinsicTest::temp
    IL_350e:  box        [mscorlib]System.Int64
    IL_3513:  ldsfld     int64 IntrinsicTest::idmp
    IL_3518:  box        [mscorlib]System.Int64
    IL_351d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3522:  ldc.i4.1
    IL_3523:  stloc.s    V_20
    IL_3525:  ldsflda    int32 IntrinsicTest::counter
    IL_352a:  ldsfld     int32 IntrinsicTest::counter
    IL_352f:  ldloc.s    V_21
    IL_3531:  callvirt   instance int32 IntrinsicTest::GetValue()
    IL_3536:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_353b:  stloc.0
    IL_353c:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3541:  stloc.s    V_10
    IL_3543:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3548:  ldloc.s    V_21
    IL_354a:  callvirt   instance int32 IntrinsicTest::id_GetValue()
    IL_354f:  bne.un.s   IL_355d

    IL_3551:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3556:  stsfld     int32 IntrinsicTest::id_counter
    IL_355b:  br.s       IL_3568

    IL_355d:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3562:  conv.i8
    IL_3563:  stsfld     int64 IntrinsicTest::idjunk
    IL_3568:  ldstr      "------------------------------------CMPXCHG  1 1 3 0"
    IL_356d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_3572:  ldloc.0
    IL_3573:  ldloc.s    V_10
    IL_3575:  beq.s      IL_3591

    IL_3577:  ldc.i4.1
    IL_3578:  stloc.s    V_20
    IL_357a:  ldstr      "te0 check failed {0} {1}"
    IL_357f:  ldloc.0
    IL_3580:  box        [mscorlib]System.Int32
    IL_3585:  ldloc.s    V_10
    IL_3587:  box        [mscorlib]System.Int32
    IL_358c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3591:  ldloc.1
    IL_3592:  ldloc.s    V_11
    IL_3594:  beq.s      IL_35b0

    IL_3596:  ldc.i4.1
    IL_3597:  stloc.s    V_20
    IL_3599:  ldstr      "te1 check failed {0} {1}"
    IL_359e:  ldloc.1
    IL_359f:  box        [mscorlib]System.Int32
    IL_35a4:  ldloc.s    V_11
    IL_35a6:  box        [mscorlib]System.Int32
    IL_35ab:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_35b0:  ldloc.2
    IL_35b1:  ldloc.s    V_12
    IL_35b3:  beq.s      IL_35cf

    IL_35b5:  ldc.i4.1
    IL_35b6:  stloc.s    V_20
    IL_35b8:  ldstr      "te2 check failed {0} {1}"
    IL_35bd:  ldloc.2
    IL_35be:  box        [mscorlib]System.Int32
    IL_35c3:  ldloc.s    V_12
    IL_35c5:  box        [mscorlib]System.Int32
    IL_35ca:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_35cf:  ldloc.3
    IL_35d0:  ldloc.s    V_13
    IL_35d2:  beq.s      IL_35ee

    IL_35d4:  ldc.i4.1
    IL_35d5:  stloc.s    V_20
    IL_35d7:  ldstr      "te3 check failed {0} {1}"
    IL_35dc:  ldloc.3
    IL_35dd:  box        [mscorlib]System.Int32
    IL_35e2:  ldloc.s    V_13
    IL_35e4:  box        [mscorlib]System.Int32
    IL_35e9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_35ee:  ldloc.s    V_4
    IL_35f0:  ldloc.s    V_14
    IL_35f2:  beq.s      IL_360f

    IL_35f4:  ldc.i4.1
    IL_35f5:  stloc.s    V_20
    IL_35f7:  ldstr      "te064 check failed {0} {1}"
    IL_35fc:  ldloc.s    V_4
    IL_35fe:  box        [mscorlib]System.Int64
    IL_3603:  ldloc.s    V_14
    IL_3605:  box        [mscorlib]System.Int64
    IL_360a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_360f:  ldloc.s    V_5
    IL_3611:  ldloc.s    V_15
    IL_3613:  beq.s      IL_3630

    IL_3615:  ldc.i4.1
    IL_3616:  stloc.s    V_20
    IL_3618:  ldstr      "te164 check failed {0} {1}"
    IL_361d:  ldloc.s    V_5
    IL_361f:  box        [mscorlib]System.Int64
    IL_3624:  ldloc.s    V_15
    IL_3626:  box        [mscorlib]System.Int64
    IL_362b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3630:  ldloc.s    V_6
    IL_3632:  ldloc.s    V_16
    IL_3634:  beq.s      IL_3651

    IL_3636:  ldc.i4.1
    IL_3637:  stloc.s    V_20
    IL_3639:  ldstr      "te264 check failed {0} {1}"
    IL_363e:  ldloc.s    V_6
    IL_3640:  box        [mscorlib]System.Int64
    IL_3645:  ldloc.s    V_16
    IL_3647:  box        [mscorlib]System.Int64
    IL_364c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3651:  ldloc.s    V_7
    IL_3653:  ldloc.s    V_17
    IL_3655:  beq.s      IL_3672

    IL_3657:  ldc.i4.1
    IL_3658:  stloc.s    V_20
    IL_365a:  ldstr      "te364 check failed {0} {1}"
    IL_365f:  ldloc.s    V_7
    IL_3661:  box        [mscorlib]System.Int64
    IL_3666:  ldloc.s    V_17
    IL_3668:  box        [mscorlib]System.Int64
    IL_366d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3672:  ldloc.s    V_8
    IL_3674:  ldloc.s    V_18
    IL_3676:  beq.s      IL_3693

    IL_3678:  ldc.i4.1
    IL_3679:  stloc.s    V_20
    IL_367b:  ldstr      "dummy check failed {0} {1}"
    IL_3680:  ldloc.s    V_8
    IL_3682:  box        [mscorlib]System.Int32
    IL_3687:  ldloc.s    V_18
    IL_3689:  box        [mscorlib]System.Int32
    IL_368e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3693:  ldloc.s    V_9
    IL_3695:  ldloc.s    V_19
    IL_3697:  beq.s      IL_36b4

    IL_3699:  ldc.i4.1
    IL_369a:  stloc.s    V_20
    IL_369c:  ldstr      "dummy64 check failed {0} {1}"
    IL_36a1:  ldloc.s    V_9
    IL_36a3:  box        [mscorlib]System.Int64
    IL_36a8:  ldloc.s    V_19
    IL_36aa:  box        [mscorlib]System.Int64
    IL_36af:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_36b4:  ldsfld     int32 IntrinsicTest::counter
    IL_36b9:  ldsfld     int32 IntrinsicTest::id_counter
    IL_36be:  beq.s      IL_36e1

    IL_36c0:  ldstr      "counter mismatch {0} {1}"
    IL_36c5:  ldsfld     int32 IntrinsicTest::counter
    IL_36ca:  box        [mscorlib]System.Int32
    IL_36cf:  ldsfld     int32 IntrinsicTest::id_counter
    IL_36d4:  box        [mscorlib]System.Int32
    IL_36d9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_36de:  ldc.i4.1
    IL_36df:  stloc.s    V_20
    IL_36e1:  ldsfld     int64 IntrinsicTest::counter64
    IL_36e6:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_36eb:  beq.s      IL_370e

    IL_36ed:  ldstr      "counter64 mismatch {0} {1}"
    IL_36f2:  ldsfld     int64 IntrinsicTest::counter64
    IL_36f7:  box        [mscorlib]System.Int64
    IL_36fc:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3701:  box        [mscorlib]System.Int64
    IL_3706:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_370b:  ldc.i4.1
    IL_370c:  stloc.s    V_20
    IL_370e:  ldloc.s    V_21
    IL_3710:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_3715:  ldloc.s    V_21
    IL_3717:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_371c:  beq.s      IL_3743

    IL_371e:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_3723:  ldloc.s    V_21
    IL_3725:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_372a:  box        [mscorlib]System.Int32
    IL_372f:  ldloc.s    V_21
    IL_3731:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_3736:  box        [mscorlib]System.Int32
    IL_373b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3740:  ldc.i4.1
    IL_3741:  stloc.s    V_20
    IL_3743:  ldloc.s    V_21
    IL_3745:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_374a:  ldloc.s    V_21
    IL_374c:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_3751:  beq.s      IL_3778

    IL_3753:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_3758:  ldloc.s    V_21
    IL_375a:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_375f:  box        [mscorlib]System.Int64
    IL_3764:  ldloc.s    V_21
    IL_3766:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_376b:  box        [mscorlib]System.Int64
    IL_3770:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3775:  ldc.i4.1
    IL_3776:  stloc.s    V_20
    IL_3778:  ldsfld     int64 IntrinsicTest::temp
    IL_377d:  ldsfld     int64 IntrinsicTest::idmp
    IL_3782:  beq.s      IL_37a5

    IL_3784:  ldstr      "temp mismatch {0} {1}"
    IL_3789:  ldsfld     int64 IntrinsicTest::temp
    IL_378e:  box        [mscorlib]System.Int64
    IL_3793:  ldsfld     int64 IntrinsicTest::idmp
    IL_3798:  box        [mscorlib]System.Int64
    IL_379d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_37a2:  ldc.i4.1
    IL_37a3:  stloc.s    V_20
    IL_37a5:  ldsflda    int32 IntrinsicTest::counter
    IL_37aa:  ldsfld     int32 IntrinsicTest::counter
    IL_37af:  ldloc.s    V_21
    IL_37b1:  callvirt   instance int32 IntrinsicTest::GetValue()
    IL_37b6:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_37bb:  stsfld     int32 IntrinsicTest::counter
    IL_37c0:  ldsfld     int32 IntrinsicTest::id_counter
    IL_37c5:  stsfld     int32 IntrinsicTest::id_counter
    IL_37ca:  ldsfld     int32 IntrinsicTest::id_counter
    IL_37cf:  ldloc.s    V_21
    IL_37d1:  callvirt   instance int32 IntrinsicTest::id_GetValue()
    IL_37d6:  bne.un.s   IL_37e5

    IL_37d8:  ldsfld     int32 IntrinsicTest::id_counter
    IL_37dd:  conv.i8
    IL_37de:  stsfld     int64 IntrinsicTest::idjunk
    IL_37e3:  br.s       IL_37f0

    IL_37e5:  ldsfld     int32 IntrinsicTest::id_counter
    IL_37ea:  conv.i8
    IL_37eb:  stsfld     int64 IntrinsicTest::idjunk
    IL_37f0:  ldstr      "------------------------------------CMPXCHG  1 1 3 1"
    IL_37f5:  call       void [System.Console]System.Console::WriteLine(string)
    IL_37fa:  ldloc.0
    IL_37fb:  ldloc.s    V_10
    IL_37fd:  beq.s      IL_3819

    IL_37ff:  ldc.i4.1
    IL_3800:  stloc.s    V_20
    IL_3802:  ldstr      "te0 check failed {0} {1}"
    IL_3807:  ldloc.0
    IL_3808:  box        [mscorlib]System.Int32
    IL_380d:  ldloc.s    V_10
    IL_380f:  box        [mscorlib]System.Int32
    IL_3814:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3819:  ldloc.1
    IL_381a:  ldloc.s    V_11
    IL_381c:  beq.s      IL_3838

    IL_381e:  ldc.i4.1
    IL_381f:  stloc.s    V_20
    IL_3821:  ldstr      "te1 check failed {0} {1}"
    IL_3826:  ldloc.1
    IL_3827:  box        [mscorlib]System.Int32
    IL_382c:  ldloc.s    V_11
    IL_382e:  box        [mscorlib]System.Int32
    IL_3833:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3838:  ldloc.2
    IL_3839:  ldloc.s    V_12
    IL_383b:  beq.s      IL_3857

    IL_383d:  ldc.i4.1
    IL_383e:  stloc.s    V_20
    IL_3840:  ldstr      "te2 check failed {0} {1}"
    IL_3845:  ldloc.2
    IL_3846:  box        [mscorlib]System.Int32
    IL_384b:  ldloc.s    V_12
    IL_384d:  box        [mscorlib]System.Int32
    IL_3852:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3857:  ldloc.3
    IL_3858:  ldloc.s    V_13
    IL_385a:  beq.s      IL_3876

    IL_385c:  ldc.i4.1
    IL_385d:  stloc.s    V_20
    IL_385f:  ldstr      "te3 check failed {0} {1}"
    IL_3864:  ldloc.3
    IL_3865:  box        [mscorlib]System.Int32
    IL_386a:  ldloc.s    V_13
    IL_386c:  box        [mscorlib]System.Int32
    IL_3871:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3876:  ldloc.s    V_4
    IL_3878:  ldloc.s    V_14
    IL_387a:  beq.s      IL_3897

    IL_387c:  ldc.i4.1
    IL_387d:  stloc.s    V_20
    IL_387f:  ldstr      "te064 check failed {0} {1}"
    IL_3884:  ldloc.s    V_4
    IL_3886:  box        [mscorlib]System.Int64
    IL_388b:  ldloc.s    V_14
    IL_388d:  box        [mscorlib]System.Int64
    IL_3892:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3897:  ldloc.s    V_5
    IL_3899:  ldloc.s    V_15
    IL_389b:  beq.s      IL_38b8

    IL_389d:  ldc.i4.1
    IL_389e:  stloc.s    V_20
    IL_38a0:  ldstr      "te164 check failed {0} {1}"
    IL_38a5:  ldloc.s    V_5
    IL_38a7:  box        [mscorlib]System.Int64
    IL_38ac:  ldloc.s    V_15
    IL_38ae:  box        [mscorlib]System.Int64
    IL_38b3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_38b8:  ldloc.s    V_6
    IL_38ba:  ldloc.s    V_16
    IL_38bc:  beq.s      IL_38d9

    IL_38be:  ldc.i4.1
    IL_38bf:  stloc.s    V_20
    IL_38c1:  ldstr      "te264 check failed {0} {1}"
    IL_38c6:  ldloc.s    V_6
    IL_38c8:  box        [mscorlib]System.Int64
    IL_38cd:  ldloc.s    V_16
    IL_38cf:  box        [mscorlib]System.Int64
    IL_38d4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_38d9:  ldloc.s    V_7
    IL_38db:  ldloc.s    V_17
    IL_38dd:  beq.s      IL_38fa

    IL_38df:  ldc.i4.1
    IL_38e0:  stloc.s    V_20
    IL_38e2:  ldstr      "te364 check failed {0} {1}"
    IL_38e7:  ldloc.s    V_7
    IL_38e9:  box        [mscorlib]System.Int64
    IL_38ee:  ldloc.s    V_17
    IL_38f0:  box        [mscorlib]System.Int64
    IL_38f5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_38fa:  ldloc.s    V_8
    IL_38fc:  ldloc.s    V_18
    IL_38fe:  beq.s      IL_391b

    IL_3900:  ldc.i4.1
    IL_3901:  stloc.s    V_20
    IL_3903:  ldstr      "dummy check failed {0} {1}"
    IL_3908:  ldloc.s    V_8
    IL_390a:  box        [mscorlib]System.Int32
    IL_390f:  ldloc.s    V_18
    IL_3911:  box        [mscorlib]System.Int32
    IL_3916:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_391b:  ldloc.s    V_9
    IL_391d:  ldloc.s    V_19
    IL_391f:  beq.s      IL_393c

    IL_3921:  ldc.i4.1
    IL_3922:  stloc.s    V_20
    IL_3924:  ldstr      "dummy64 check failed {0} {1}"
    IL_3929:  ldloc.s    V_9
    IL_392b:  box        [mscorlib]System.Int64
    IL_3930:  ldloc.s    V_19
    IL_3932:  box        [mscorlib]System.Int64
    IL_3937:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_393c:  ldsfld     int32 IntrinsicTest::counter
    IL_3941:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3946:  beq.s      IL_3969

    IL_3948:  ldstr      "counter mismatch {0} {1}"
    IL_394d:  ldsfld     int32 IntrinsicTest::counter
    IL_3952:  box        [mscorlib]System.Int32
    IL_3957:  ldsfld     int32 IntrinsicTest::id_counter
    IL_395c:  box        [mscorlib]System.Int32
    IL_3961:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3966:  ldc.i4.1
    IL_3967:  stloc.s    V_20
    IL_3969:  ldsfld     int64 IntrinsicTest::counter64
    IL_396e:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3973:  beq.s      IL_3996

    IL_3975:  ldstr      "counter64 mismatch {0} {1}"
    IL_397a:  ldsfld     int64 IntrinsicTest::counter64
    IL_397f:  box        [mscorlib]System.Int64
    IL_3984:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3989:  box        [mscorlib]System.Int64
    IL_398e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3993:  ldc.i4.1
    IL_3994:  stloc.s    V_20
    IL_3996:  ldloc.s    V_21
    IL_3998:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_399d:  ldloc.s    V_21
    IL_399f:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_39a4:  beq.s      IL_39cb

    IL_39a6:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_39ab:  ldloc.s    V_21
    IL_39ad:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_39b2:  box        [mscorlib]System.Int32
    IL_39b7:  ldloc.s    V_21
    IL_39b9:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_39be:  box        [mscorlib]System.Int32
    IL_39c3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_39c8:  ldc.i4.1
    IL_39c9:  stloc.s    V_20
    IL_39cb:  ldloc.s    V_21
    IL_39cd:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_39d2:  ldloc.s    V_21
    IL_39d4:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_39d9:  beq.s      IL_3a00

    IL_39db:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_39e0:  ldloc.s    V_21
    IL_39e2:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_39e7:  box        [mscorlib]System.Int64
    IL_39ec:  ldloc.s    V_21
    IL_39ee:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_39f3:  box        [mscorlib]System.Int64
    IL_39f8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_39fd:  ldc.i4.1
    IL_39fe:  stloc.s    V_20
    IL_3a00:  ldsfld     int64 IntrinsicTest::temp
    IL_3a05:  ldsfld     int64 IntrinsicTest::idmp
    IL_3a0a:  beq.s      IL_3a2d

    IL_3a0c:  ldstr      "temp mismatch {0} {1}"
    IL_3a11:  ldsfld     int64 IntrinsicTest::temp
    IL_3a16:  box        [mscorlib]System.Int64
    IL_3a1b:  ldsfld     int64 IntrinsicTest::idmp
    IL_3a20:  box        [mscorlib]System.Int64
    IL_3a25:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3a2a:  ldc.i4.1
    IL_3a2b:  stloc.s    V_20
    IL_3a2d:  ldloc.s    V_21
    IL_3a2f:  ldsflda    int32 IntrinsicTest::counter
    IL_3a34:  ldsfld     int32 IntrinsicTest::counter
    IL_3a39:  ldloc.s    V_21
    IL_3a3b:  callvirt   instance int32 IntrinsicTest::GetValue()
    IL_3a40:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_3a45:  stfld      int32 IntrinsicTest::instanceCounter
    IL_3a4a:  ldloc.s    V_21
    IL_3a4c:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3a51:  stfld      int32 IntrinsicTest::id_instanceCounter
    IL_3a56:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3a5b:  ldloc.s    V_21
    IL_3a5d:  callvirt   instance int32 IntrinsicTest::id_GetValue()
    IL_3a62:  bne.un.s   IL_3a70

    IL_3a64:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3a69:  stsfld     int32 IntrinsicTest::id_counter
    IL_3a6e:  br.s       IL_3a7b

    IL_3a70:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3a75:  conv.i8
    IL_3a76:  stsfld     int64 IntrinsicTest::idjunk
    IL_3a7b:  ldstr      "------------------------------------CMPXCHG  1 1 3 2"
    IL_3a80:  call       void [System.Console]System.Console::WriteLine(string)
    IL_3a85:  ldloc.0
    IL_3a86:  ldloc.s    V_10
    IL_3a88:  beq.s      IL_3aa4

    IL_3a8a:  ldc.i4.1
    IL_3a8b:  stloc.s    V_20
    IL_3a8d:  ldstr      "te0 check failed {0} {1}"
    IL_3a92:  ldloc.0
    IL_3a93:  box        [mscorlib]System.Int32
    IL_3a98:  ldloc.s    V_10
    IL_3a9a:  box        [mscorlib]System.Int32
    IL_3a9f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3aa4:  ldloc.1
    IL_3aa5:  ldloc.s    V_11
    IL_3aa7:  beq.s      IL_3ac3

    IL_3aa9:  ldc.i4.1
    IL_3aaa:  stloc.s    V_20
    IL_3aac:  ldstr      "te1 check failed {0} {1}"
    IL_3ab1:  ldloc.1
    IL_3ab2:  box        [mscorlib]System.Int32
    IL_3ab7:  ldloc.s    V_11
    IL_3ab9:  box        [mscorlib]System.Int32
    IL_3abe:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3ac3:  ldloc.2
    IL_3ac4:  ldloc.s    V_12
    IL_3ac6:  beq.s      IL_3ae2

    IL_3ac8:  ldc.i4.1
    IL_3ac9:  stloc.s    V_20
    IL_3acb:  ldstr      "te2 check failed {0} {1}"
    IL_3ad0:  ldloc.2
    IL_3ad1:  box        [mscorlib]System.Int32
    IL_3ad6:  ldloc.s    V_12
    IL_3ad8:  box        [mscorlib]System.Int32
    IL_3add:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3ae2:  ldloc.3
    IL_3ae3:  ldloc.s    V_13
    IL_3ae5:  beq.s      IL_3b01

    IL_3ae7:  ldc.i4.1
    IL_3ae8:  stloc.s    V_20
    IL_3aea:  ldstr      "te3 check failed {0} {1}"
    IL_3aef:  ldloc.3
    IL_3af0:  box        [mscorlib]System.Int32
    IL_3af5:  ldloc.s    V_13
    IL_3af7:  box        [mscorlib]System.Int32
    IL_3afc:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3b01:  ldloc.s    V_4
    IL_3b03:  ldloc.s    V_14
    IL_3b05:  beq.s      IL_3b22

    IL_3b07:  ldc.i4.1
    IL_3b08:  stloc.s    V_20
    IL_3b0a:  ldstr      "te064 check failed {0} {1}"
    IL_3b0f:  ldloc.s    V_4
    IL_3b11:  box        [mscorlib]System.Int64
    IL_3b16:  ldloc.s    V_14
    IL_3b18:  box        [mscorlib]System.Int64
    IL_3b1d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3b22:  ldloc.s    V_5
    IL_3b24:  ldloc.s    V_15
    IL_3b26:  beq.s      IL_3b43

    IL_3b28:  ldc.i4.1
    IL_3b29:  stloc.s    V_20
    IL_3b2b:  ldstr      "te164 check failed {0} {1}"
    IL_3b30:  ldloc.s    V_5
    IL_3b32:  box        [mscorlib]System.Int64
    IL_3b37:  ldloc.s    V_15
    IL_3b39:  box        [mscorlib]System.Int64
    IL_3b3e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3b43:  ldloc.s    V_6
    IL_3b45:  ldloc.s    V_16
    IL_3b47:  beq.s      IL_3b64

    IL_3b49:  ldc.i4.1
    IL_3b4a:  stloc.s    V_20
    IL_3b4c:  ldstr      "te264 check failed {0} {1}"
    IL_3b51:  ldloc.s    V_6
    IL_3b53:  box        [mscorlib]System.Int64
    IL_3b58:  ldloc.s    V_16
    IL_3b5a:  box        [mscorlib]System.Int64
    IL_3b5f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3b64:  ldloc.s    V_7
    IL_3b66:  ldloc.s    V_17
    IL_3b68:  beq.s      IL_3b85

    IL_3b6a:  ldc.i4.1
    IL_3b6b:  stloc.s    V_20
    IL_3b6d:  ldstr      "te364 check failed {0} {1}"
    IL_3b72:  ldloc.s    V_7
    IL_3b74:  box        [mscorlib]System.Int64
    IL_3b79:  ldloc.s    V_17
    IL_3b7b:  box        [mscorlib]System.Int64
    IL_3b80:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3b85:  ldloc.s    V_8
    IL_3b87:  ldloc.s    V_18
    IL_3b89:  beq.s      IL_3ba6

    IL_3b8b:  ldc.i4.1
    IL_3b8c:  stloc.s    V_20
    IL_3b8e:  ldstr      "dummy check failed {0} {1}"
    IL_3b93:  ldloc.s    V_8
    IL_3b95:  box        [mscorlib]System.Int32
    IL_3b9a:  ldloc.s    V_18
    IL_3b9c:  box        [mscorlib]System.Int32
    IL_3ba1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3ba6:  ldloc.s    V_9
    IL_3ba8:  ldloc.s    V_19
    IL_3baa:  beq.s      IL_3bc7

    IL_3bac:  ldc.i4.1
    IL_3bad:  stloc.s    V_20
    IL_3baf:  ldstr      "dummy64 check failed {0} {1}"
    IL_3bb4:  ldloc.s    V_9
    IL_3bb6:  box        [mscorlib]System.Int64
    IL_3bbb:  ldloc.s    V_19
    IL_3bbd:  box        [mscorlib]System.Int64
    IL_3bc2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3bc7:  ldsfld     int32 IntrinsicTest::counter
    IL_3bcc:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3bd1:  beq.s      IL_3bf4

    IL_3bd3:  ldstr      "counter mismatch {0} {1}"
    IL_3bd8:  ldsfld     int32 IntrinsicTest::counter
    IL_3bdd:  box        [mscorlib]System.Int32
    IL_3be2:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3be7:  box        [mscorlib]System.Int32
    IL_3bec:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3bf1:  ldc.i4.1
    IL_3bf2:  stloc.s    V_20
    IL_3bf4:  ldsfld     int64 IntrinsicTest::counter64
    IL_3bf9:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3bfe:  beq.s      IL_3c21

    IL_3c00:  ldstr      "counter64 mismatch {0} {1}"
    IL_3c05:  ldsfld     int64 IntrinsicTest::counter64
    IL_3c0a:  box        [mscorlib]System.Int64
    IL_3c0f:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3c14:  box        [mscorlib]System.Int64
    IL_3c19:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3c1e:  ldc.i4.1
    IL_3c1f:  stloc.s    V_20
    IL_3c21:  ldloc.s    V_21
    IL_3c23:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_3c28:  ldloc.s    V_21
    IL_3c2a:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_3c2f:  beq.s      IL_3c56

    IL_3c31:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_3c36:  ldloc.s    V_21
    IL_3c38:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_3c3d:  box        [mscorlib]System.Int32
    IL_3c42:  ldloc.s    V_21
    IL_3c44:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_3c49:  box        [mscorlib]System.Int32
    IL_3c4e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3c53:  ldc.i4.1
    IL_3c54:  stloc.s    V_20
    IL_3c56:  ldloc.s    V_21
    IL_3c58:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_3c5d:  ldloc.s    V_21
    IL_3c5f:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_3c64:  beq.s      IL_3c8b

    IL_3c66:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_3c6b:  ldloc.s    V_21
    IL_3c6d:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_3c72:  box        [mscorlib]System.Int64
    IL_3c77:  ldloc.s    V_21
    IL_3c79:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_3c7e:  box        [mscorlib]System.Int64
    IL_3c83:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3c88:  ldc.i4.1
    IL_3c89:  stloc.s    V_20
    IL_3c8b:  ldsfld     int64 IntrinsicTest::temp
    IL_3c90:  ldsfld     int64 IntrinsicTest::idmp
    IL_3c95:  beq.s      IL_3cb8

    IL_3c97:  ldstr      "temp mismatch {0} {1}"
    IL_3c9c:  ldsfld     int64 IntrinsicTest::temp
    IL_3ca1:  box        [mscorlib]System.Int64
    IL_3ca6:  ldsfld     int64 IntrinsicTest::idmp
    IL_3cab:  box        [mscorlib]System.Int64
    IL_3cb0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3cb5:  ldc.i4.1
    IL_3cb6:  stloc.s    V_20
    IL_3cb8:  ldsflda    int32 IntrinsicTest::counter
    IL_3cbd:  ldsfld     int32 IntrinsicTest::counter
    IL_3cc2:  ldloc.s    V_21
    IL_3cc4:  callvirt   instance int32 IntrinsicTest::GetValue()
    IL_3cc9:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_3cce:  stloc.0
    IL_3ccf:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3cd4:  stloc.s    V_10
    IL_3cd6:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3cdb:  ldloc.s    V_21
    IL_3cdd:  callvirt   instance int32 IntrinsicTest::id_GetValue()
    IL_3ce2:  bne.un.s   IL_3cf0

    IL_3ce4:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3ce9:  stsfld     int32 IntrinsicTest::id_counter
    IL_3cee:  br.s       IL_3cfb

    IL_3cf0:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3cf5:  conv.i8
    IL_3cf6:  stsfld     int64 IntrinsicTest::idjunk
    IL_3cfb:  ldstr      "------------------------------------CMPXCHG  1 1 3 3"
    IL_3d00:  call       void [System.Console]System.Console::WriteLine(string)
    IL_3d05:  ldloc.0
    IL_3d06:  ldloc.s    V_10
    IL_3d08:  beq.s      IL_3d24

    IL_3d0a:  ldc.i4.1
    IL_3d0b:  stloc.s    V_20
    IL_3d0d:  ldstr      "te0 check failed {0} {1}"
    IL_3d12:  ldloc.0
    IL_3d13:  box        [mscorlib]System.Int32
    IL_3d18:  ldloc.s    V_10
    IL_3d1a:  box        [mscorlib]System.Int32
    IL_3d1f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3d24:  ldloc.1
    IL_3d25:  ldloc.s    V_11
    IL_3d27:  beq.s      IL_3d43

    IL_3d29:  ldc.i4.1
    IL_3d2a:  stloc.s    V_20
    IL_3d2c:  ldstr      "te1 check failed {0} {1}"
    IL_3d31:  ldloc.1
    IL_3d32:  box        [mscorlib]System.Int32
    IL_3d37:  ldloc.s    V_11
    IL_3d39:  box        [mscorlib]System.Int32
    IL_3d3e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3d43:  ldloc.2
    IL_3d44:  ldloc.s    V_12
    IL_3d46:  beq.s      IL_3d62

    IL_3d48:  ldc.i4.1
    IL_3d49:  stloc.s    V_20
    IL_3d4b:  ldstr      "te2 check failed {0} {1}"
    IL_3d50:  ldloc.2
    IL_3d51:  box        [mscorlib]System.Int32
    IL_3d56:  ldloc.s    V_12
    IL_3d58:  box        [mscorlib]System.Int32
    IL_3d5d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3d62:  ldloc.3
    IL_3d63:  ldloc.s    V_13
    IL_3d65:  beq.s      IL_3d81

    IL_3d67:  ldc.i4.1
    IL_3d68:  stloc.s    V_20
    IL_3d6a:  ldstr      "te3 check failed {0} {1}"
    IL_3d6f:  ldloc.3
    IL_3d70:  box        [mscorlib]System.Int32
    IL_3d75:  ldloc.s    V_13
    IL_3d77:  box        [mscorlib]System.Int32
    IL_3d7c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3d81:  ldloc.s    V_4
    IL_3d83:  ldloc.s    V_14
    IL_3d85:  beq.s      IL_3da2

    IL_3d87:  ldc.i4.1
    IL_3d88:  stloc.s    V_20
    IL_3d8a:  ldstr      "te064 check failed {0} {1}"
    IL_3d8f:  ldloc.s    V_4
    IL_3d91:  box        [mscorlib]System.Int64
    IL_3d96:  ldloc.s    V_14
    IL_3d98:  box        [mscorlib]System.Int64
    IL_3d9d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3da2:  ldloc.s    V_5
    IL_3da4:  ldloc.s    V_15
    IL_3da6:  beq.s      IL_3dc3

    IL_3da8:  ldc.i4.1
    IL_3da9:  stloc.s    V_20
    IL_3dab:  ldstr      "te164 check failed {0} {1}"
    IL_3db0:  ldloc.s    V_5
    IL_3db2:  box        [mscorlib]System.Int64
    IL_3db7:  ldloc.s    V_15
    IL_3db9:  box        [mscorlib]System.Int64
    IL_3dbe:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3dc3:  ldloc.s    V_6
    IL_3dc5:  ldloc.s    V_16
    IL_3dc7:  beq.s      IL_3de4

    IL_3dc9:  ldc.i4.1
    IL_3dca:  stloc.s    V_20
    IL_3dcc:  ldstr      "te264 check failed {0} {1}"
    IL_3dd1:  ldloc.s    V_6
    IL_3dd3:  box        [mscorlib]System.Int64
    IL_3dd8:  ldloc.s    V_16
    IL_3dda:  box        [mscorlib]System.Int64
    IL_3ddf:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3de4:  ldloc.s    V_7
    IL_3de6:  ldloc.s    V_17
    IL_3de8:  beq.s      IL_3e05

    IL_3dea:  ldc.i4.1
    IL_3deb:  stloc.s    V_20
    IL_3ded:  ldstr      "te364 check failed {0} {1}"
    IL_3df2:  ldloc.s    V_7
    IL_3df4:  box        [mscorlib]System.Int64
    IL_3df9:  ldloc.s    V_17
    IL_3dfb:  box        [mscorlib]System.Int64
    IL_3e00:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3e05:  ldloc.s    V_8
    IL_3e07:  ldloc.s    V_18
    IL_3e09:  beq.s      IL_3e26

    IL_3e0b:  ldc.i4.1
    IL_3e0c:  stloc.s    V_20
    IL_3e0e:  ldstr      "dummy check failed {0} {1}"
    IL_3e13:  ldloc.s    V_8
    IL_3e15:  box        [mscorlib]System.Int32
    IL_3e1a:  ldloc.s    V_18
    IL_3e1c:  box        [mscorlib]System.Int32
    IL_3e21:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3e26:  ldloc.s    V_9
    IL_3e28:  ldloc.s    V_19
    IL_3e2a:  beq.s      IL_3e47

    IL_3e2c:  ldc.i4.1
    IL_3e2d:  stloc.s    V_20
    IL_3e2f:  ldstr      "dummy64 check failed {0} {1}"
    IL_3e34:  ldloc.s    V_9
    IL_3e36:  box        [mscorlib]System.Int64
    IL_3e3b:  ldloc.s    V_19
    IL_3e3d:  box        [mscorlib]System.Int64
    IL_3e42:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3e47:  ldsfld     int32 IntrinsicTest::counter
    IL_3e4c:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3e51:  beq.s      IL_3e74

    IL_3e53:  ldstr      "counter mismatch {0} {1}"
    IL_3e58:  ldsfld     int32 IntrinsicTest::counter
    IL_3e5d:  box        [mscorlib]System.Int32
    IL_3e62:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3e67:  box        [mscorlib]System.Int32
    IL_3e6c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3e71:  ldc.i4.1
    IL_3e72:  stloc.s    V_20
    IL_3e74:  ldsfld     int64 IntrinsicTest::counter64
    IL_3e79:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3e7e:  beq.s      IL_3ea1

    IL_3e80:  ldstr      "counter64 mismatch {0} {1}"
    IL_3e85:  ldsfld     int64 IntrinsicTest::counter64
    IL_3e8a:  box        [mscorlib]System.Int64
    IL_3e8f:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_3e94:  box        [mscorlib]System.Int64
    IL_3e99:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3e9e:  ldc.i4.1
    IL_3e9f:  stloc.s    V_20
    IL_3ea1:  ldloc.s    V_21
    IL_3ea3:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_3ea8:  ldloc.s    V_21
    IL_3eaa:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_3eaf:  beq.s      IL_3ed6

    IL_3eb1:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_3eb6:  ldloc.s    V_21
    IL_3eb8:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_3ebd:  box        [mscorlib]System.Int32
    IL_3ec2:  ldloc.s    V_21
    IL_3ec4:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_3ec9:  box        [mscorlib]System.Int32
    IL_3ece:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3ed3:  ldc.i4.1
    IL_3ed4:  stloc.s    V_20
    IL_3ed6:  ldloc.s    V_21
    IL_3ed8:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_3edd:  ldloc.s    V_21
    IL_3edf:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_3ee4:  beq.s      IL_3f0b

    IL_3ee6:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_3eeb:  ldloc.s    V_21
    IL_3eed:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_3ef2:  box        [mscorlib]System.Int64
    IL_3ef7:  ldloc.s    V_21
    IL_3ef9:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_3efe:  box        [mscorlib]System.Int64
    IL_3f03:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3f08:  ldc.i4.1
    IL_3f09:  stloc.s    V_20
    IL_3f0b:  ldsfld     int64 IntrinsicTest::temp
    IL_3f10:  ldsfld     int64 IntrinsicTest::idmp
    IL_3f15:  beq.s      IL_3f38

    IL_3f17:  ldstr      "temp mismatch {0} {1}"
    IL_3f1c:  ldsfld     int64 IntrinsicTest::temp
    IL_3f21:  box        [mscorlib]System.Int64
    IL_3f26:  ldsfld     int64 IntrinsicTest::idmp
    IL_3f2b:  box        [mscorlib]System.Int64
    IL_3f30:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3f35:  ldc.i4.1
    IL_3f36:  stloc.s    V_20
    IL_3f38:  ldsflda    int32 IntrinsicTest::counter
    IL_3f3d:  ldsfld     int32 IntrinsicTest::counter
    IL_3f42:  ldloc.s    V_21
    IL_3f44:  callvirt   instance int32 IntrinsicTest::GetValue()
    IL_3f49:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_3f4e:  stsfld     int32 IntrinsicTest::counter
    IL_3f53:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3f58:  stsfld     int32 IntrinsicTest::id_counter
    IL_3f5d:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3f62:  ldloc.s    V_21
    IL_3f64:  callvirt   instance int32 IntrinsicTest::id_GetValue()
    IL_3f69:  bne.un.s   IL_3f78

    IL_3f6b:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3f70:  conv.i8
    IL_3f71:  stsfld     int64 IntrinsicTest::idjunk
    IL_3f76:  br.s       IL_3f83

    IL_3f78:  ldsfld     int32 IntrinsicTest::id_counter
    IL_3f7d:  conv.i8
    IL_3f7e:  stsfld     int64 IntrinsicTest::idjunk
    IL_3f83:  ldstr      "------------------------------------CMPXCHG  1 1 3 4"
    IL_3f88:  call       void [System.Console]System.Console::WriteLine(string)
    IL_3f8d:  ldloc.0
    IL_3f8e:  ldloc.s    V_10
    IL_3f90:  beq.s      IL_3fac

    IL_3f92:  ldc.i4.1
    IL_3f93:  stloc.s    V_20
    IL_3f95:  ldstr      "te0 check failed {0} {1}"
    IL_3f9a:  ldloc.0
    IL_3f9b:  box        [mscorlib]System.Int32
    IL_3fa0:  ldloc.s    V_10
    IL_3fa2:  box        [mscorlib]System.Int32
    IL_3fa7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3fac:  ldloc.1
    IL_3fad:  ldloc.s    V_11
    IL_3faf:  beq.s      IL_3fcb

    IL_3fb1:  ldc.i4.1
    IL_3fb2:  stloc.s    V_20
    IL_3fb4:  ldstr      "te1 check failed {0} {1}"
    IL_3fb9:  ldloc.1
    IL_3fba:  box        [mscorlib]System.Int32
    IL_3fbf:  ldloc.s    V_11
    IL_3fc1:  box        [mscorlib]System.Int32
    IL_3fc6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3fcb:  ldloc.2
    IL_3fcc:  ldloc.s    V_12
    IL_3fce:  beq.s      IL_3fea

    IL_3fd0:  ldc.i4.1
    IL_3fd1:  stloc.s    V_20
    IL_3fd3:  ldstr      "te2 check failed {0} {1}"
    IL_3fd8:  ldloc.2
    IL_3fd9:  box        [mscorlib]System.Int32
    IL_3fde:  ldloc.s    V_12
    IL_3fe0:  box        [mscorlib]System.Int32
    IL_3fe5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_3fea:  ldloc.3
    IL_3feb:  ldloc.s    V_13
    IL_3fed:  beq.s      IL_4009

    IL_3fef:  ldc.i4.1
    IL_3ff0:  stloc.s    V_20
    IL_3ff2:  ldstr      "te3 check failed {0} {1}"
    IL_3ff7:  ldloc.3
    IL_3ff8:  box        [mscorlib]System.Int32
    IL_3ffd:  ldloc.s    V_13
    IL_3fff:  box        [mscorlib]System.Int32
    IL_4004:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4009:  ldloc.s    V_4
    IL_400b:  ldloc.s    V_14
    IL_400d:  beq.s      IL_402a

    IL_400f:  ldc.i4.1
    IL_4010:  stloc.s    V_20
    IL_4012:  ldstr      "te064 check failed {0} {1}"
    IL_4017:  ldloc.s    V_4
    IL_4019:  box        [mscorlib]System.Int64
    IL_401e:  ldloc.s    V_14
    IL_4020:  box        [mscorlib]System.Int64
    IL_4025:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_402a:  ldloc.s    V_5
    IL_402c:  ldloc.s    V_15
    IL_402e:  beq.s      IL_404b

    IL_4030:  ldc.i4.1
    IL_4031:  stloc.s    V_20
    IL_4033:  ldstr      "te164 check failed {0} {1}"
    IL_4038:  ldloc.s    V_5
    IL_403a:  box        [mscorlib]System.Int64
    IL_403f:  ldloc.s    V_15
    IL_4041:  box        [mscorlib]System.Int64
    IL_4046:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_404b:  ldloc.s    V_6
    IL_404d:  ldloc.s    V_16
    IL_404f:  beq.s      IL_406c

    IL_4051:  ldc.i4.1
    IL_4052:  stloc.s    V_20
    IL_4054:  ldstr      "te264 check failed {0} {1}"
    IL_4059:  ldloc.s    V_6
    IL_405b:  box        [mscorlib]System.Int64
    IL_4060:  ldloc.s    V_16
    IL_4062:  box        [mscorlib]System.Int64
    IL_4067:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_406c:  ldloc.s    V_7
    IL_406e:  ldloc.s    V_17
    IL_4070:  beq.s      IL_408d

    IL_4072:  ldc.i4.1
    IL_4073:  stloc.s    V_20
    IL_4075:  ldstr      "te364 check failed {0} {1}"
    IL_407a:  ldloc.s    V_7
    IL_407c:  box        [mscorlib]System.Int64
    IL_4081:  ldloc.s    V_17
    IL_4083:  box        [mscorlib]System.Int64
    IL_4088:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_408d:  ldloc.s    V_8
    IL_408f:  ldloc.s    V_18
    IL_4091:  beq.s      IL_40ae

    IL_4093:  ldc.i4.1
    IL_4094:  stloc.s    V_20
    IL_4096:  ldstr      "dummy check failed {0} {1}"
    IL_409b:  ldloc.s    V_8
    IL_409d:  box        [mscorlib]System.Int32
    IL_40a2:  ldloc.s    V_18
    IL_40a4:  box        [mscorlib]System.Int32
    IL_40a9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_40ae:  ldloc.s    V_9
    IL_40b0:  ldloc.s    V_19
    IL_40b2:  beq.s      IL_40cf

    IL_40b4:  ldc.i4.1
    IL_40b5:  stloc.s    V_20
    IL_40b7:  ldstr      "dummy64 check failed {0} {1}"
    IL_40bc:  ldloc.s    V_9
    IL_40be:  box        [mscorlib]System.Int64
    IL_40c3:  ldloc.s    V_19
    IL_40c5:  box        [mscorlib]System.Int64
    IL_40ca:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_40cf:  ldsfld     int32 IntrinsicTest::counter
    IL_40d4:  ldsfld     int32 IntrinsicTest::id_counter
    IL_40d9:  beq.s      IL_40fc

    IL_40db:  ldstr      "counter mismatch {0} {1}"
    IL_40e0:  ldsfld     int32 IntrinsicTest::counter
    IL_40e5:  box        [mscorlib]System.Int32
    IL_40ea:  ldsfld     int32 IntrinsicTest::id_counter
    IL_40ef:  box        [mscorlib]System.Int32
    IL_40f4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_40f9:  ldc.i4.1
    IL_40fa:  stloc.s    V_20
    IL_40fc:  ldsfld     int64 IntrinsicTest::counter64
    IL_4101:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4106:  beq.s      IL_4129

    IL_4108:  ldstr      "counter64 mismatch {0} {1}"
    IL_410d:  ldsfld     int64 IntrinsicTest::counter64
    IL_4112:  box        [mscorlib]System.Int64
    IL_4117:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_411c:  box        [mscorlib]System.Int64
    IL_4121:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4126:  ldc.i4.1
    IL_4127:  stloc.s    V_20
    IL_4129:  ldloc.s    V_21
    IL_412b:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_4130:  ldloc.s    V_21
    IL_4132:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_4137:  beq.s      IL_415e

    IL_4139:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_413e:  ldloc.s    V_21
    IL_4140:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_4145:  box        [mscorlib]System.Int32
    IL_414a:  ldloc.s    V_21
    IL_414c:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_4151:  box        [mscorlib]System.Int32
    IL_4156:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_415b:  ldc.i4.1
    IL_415c:  stloc.s    V_20
    IL_415e:  ldloc.s    V_21
    IL_4160:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_4165:  ldloc.s    V_21
    IL_4167:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_416c:  beq.s      IL_4193

    IL_416e:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_4173:  ldloc.s    V_21
    IL_4175:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_417a:  box        [mscorlib]System.Int64
    IL_417f:  ldloc.s    V_21
    IL_4181:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_4186:  box        [mscorlib]System.Int64
    IL_418b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4190:  ldc.i4.1
    IL_4191:  stloc.s    V_20
    IL_4193:  ldsfld     int64 IntrinsicTest::temp
    IL_4198:  ldsfld     int64 IntrinsicTest::idmp
    IL_419d:  beq.s      IL_41c0

    IL_419f:  ldstr      "temp mismatch {0} {1}"
    IL_41a4:  ldsfld     int64 IntrinsicTest::temp
    IL_41a9:  box        [mscorlib]System.Int64
    IL_41ae:  ldsfld     int64 IntrinsicTest::idmp
    IL_41b3:  box        [mscorlib]System.Int64
    IL_41b8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_41bd:  ldc.i4.1
    IL_41be:  stloc.s    V_20
    IL_41c0:  ldloc.s    V_21
    IL_41c2:  ldsflda    int32 IntrinsicTest::counter
    IL_41c7:  ldsfld     int32 IntrinsicTest::counter
    IL_41cc:  ldloc.s    V_21
    IL_41ce:  callvirt   instance int32 IntrinsicTest::GetValue()
    IL_41d3:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_41d8:  stfld      int32 IntrinsicTest::instanceCounter
    IL_41dd:  ldloc.s    V_21
    IL_41df:  ldsfld     int32 IntrinsicTest::id_counter
    IL_41e4:  stfld      int32 IntrinsicTest::id_instanceCounter
    IL_41e9:  ldsfld     int32 IntrinsicTest::id_counter
    IL_41ee:  ldloc.s    V_21
    IL_41f0:  callvirt   instance int32 IntrinsicTest::id_GetValue()
    IL_41f5:  bne.un.s   IL_4203

    IL_41f7:  ldsfld     int32 IntrinsicTest::id_counter
    IL_41fc:  stsfld     int32 IntrinsicTest::id_counter
    IL_4201:  br.s       IL_420e

    IL_4203:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4208:  conv.i8
    IL_4209:  stsfld     int64 IntrinsicTest::idjunk
    IL_420e:  ldstr      "------------------------------------CMPXCHG  1 1 3 5"
    IL_4213:  call       void [System.Console]System.Console::WriteLine(string)
    IL_4218:  ldloc.0
    IL_4219:  ldloc.s    V_10
    IL_421b:  beq.s      IL_4237

    IL_421d:  ldc.i4.1
    IL_421e:  stloc.s    V_20
    IL_4220:  ldstr      "te0 check failed {0} {1}"
    IL_4225:  ldloc.0
    IL_4226:  box        [mscorlib]System.Int32
    IL_422b:  ldloc.s    V_10
    IL_422d:  box        [mscorlib]System.Int32
    IL_4232:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4237:  ldloc.1
    IL_4238:  ldloc.s    V_11
    IL_423a:  beq.s      IL_4256

    IL_423c:  ldc.i4.1
    IL_423d:  stloc.s    V_20
    IL_423f:  ldstr      "te1 check failed {0} {1}"
    IL_4244:  ldloc.1
    IL_4245:  box        [mscorlib]System.Int32
    IL_424a:  ldloc.s    V_11
    IL_424c:  box        [mscorlib]System.Int32
    IL_4251:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4256:  ldloc.2
    IL_4257:  ldloc.s    V_12
    IL_4259:  beq.s      IL_4275

    IL_425b:  ldc.i4.1
    IL_425c:  stloc.s    V_20
    IL_425e:  ldstr      "te2 check failed {0} {1}"
    IL_4263:  ldloc.2
    IL_4264:  box        [mscorlib]System.Int32
    IL_4269:  ldloc.s    V_12
    IL_426b:  box        [mscorlib]System.Int32
    IL_4270:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4275:  ldloc.3
    IL_4276:  ldloc.s    V_13
    IL_4278:  beq.s      IL_4294

    IL_427a:  ldc.i4.1
    IL_427b:  stloc.s    V_20
    IL_427d:  ldstr      "te3 check failed {0} {1}"
    IL_4282:  ldloc.3
    IL_4283:  box        [mscorlib]System.Int32
    IL_4288:  ldloc.s    V_13
    IL_428a:  box        [mscorlib]System.Int32
    IL_428f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4294:  ldloc.s    V_4
    IL_4296:  ldloc.s    V_14
    IL_4298:  beq.s      IL_42b5

    IL_429a:  ldc.i4.1
    IL_429b:  stloc.s    V_20
    IL_429d:  ldstr      "te064 check failed {0} {1}"
    IL_42a2:  ldloc.s    V_4
    IL_42a4:  box        [mscorlib]System.Int64
    IL_42a9:  ldloc.s    V_14
    IL_42ab:  box        [mscorlib]System.Int64
    IL_42b0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_42b5:  ldloc.s    V_5
    IL_42b7:  ldloc.s    V_15
    IL_42b9:  beq.s      IL_42d6

    IL_42bb:  ldc.i4.1
    IL_42bc:  stloc.s    V_20
    IL_42be:  ldstr      "te164 check failed {0} {1}"
    IL_42c3:  ldloc.s    V_5
    IL_42c5:  box        [mscorlib]System.Int64
    IL_42ca:  ldloc.s    V_15
    IL_42cc:  box        [mscorlib]System.Int64
    IL_42d1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_42d6:  ldloc.s    V_6
    IL_42d8:  ldloc.s    V_16
    IL_42da:  beq.s      IL_42f7

    IL_42dc:  ldc.i4.1
    IL_42dd:  stloc.s    V_20
    IL_42df:  ldstr      "te264 check failed {0} {1}"
    IL_42e4:  ldloc.s    V_6
    IL_42e6:  box        [mscorlib]System.Int64
    IL_42eb:  ldloc.s    V_16
    IL_42ed:  box        [mscorlib]System.Int64
    IL_42f2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_42f7:  ldloc.s    V_7
    IL_42f9:  ldloc.s    V_17
    IL_42fb:  beq.s      IL_4318

    IL_42fd:  ldc.i4.1
    IL_42fe:  stloc.s    V_20
    IL_4300:  ldstr      "te364 check failed {0} {1}"
    IL_4305:  ldloc.s    V_7
    IL_4307:  box        [mscorlib]System.Int64
    IL_430c:  ldloc.s    V_17
    IL_430e:  box        [mscorlib]System.Int64
    IL_4313:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4318:  ldloc.s    V_8
    IL_431a:  ldloc.s    V_18
    IL_431c:  beq.s      IL_4339

    IL_431e:  ldc.i4.1
    IL_431f:  stloc.s    V_20
    IL_4321:  ldstr      "dummy check failed {0} {1}"
    IL_4326:  ldloc.s    V_8
    IL_4328:  box        [mscorlib]System.Int32
    IL_432d:  ldloc.s    V_18
    IL_432f:  box        [mscorlib]System.Int32
    IL_4334:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4339:  ldloc.s    V_9
    IL_433b:  ldloc.s    V_19
    IL_433d:  beq.s      IL_435a

    IL_433f:  ldc.i4.1
    IL_4340:  stloc.s    V_20
    IL_4342:  ldstr      "dummy64 check failed {0} {1}"
    IL_4347:  ldloc.s    V_9
    IL_4349:  box        [mscorlib]System.Int64
    IL_434e:  ldloc.s    V_19
    IL_4350:  box        [mscorlib]System.Int64
    IL_4355:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_435a:  ldsfld     int32 IntrinsicTest::counter
    IL_435f:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4364:  beq.s      IL_4387

    IL_4366:  ldstr      "counter mismatch {0} {1}"
    IL_436b:  ldsfld     int32 IntrinsicTest::counter
    IL_4370:  box        [mscorlib]System.Int32
    IL_4375:  ldsfld     int32 IntrinsicTest::id_counter
    IL_437a:  box        [mscorlib]System.Int32
    IL_437f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4384:  ldc.i4.1
    IL_4385:  stloc.s    V_20
    IL_4387:  ldsfld     int64 IntrinsicTest::counter64
    IL_438c:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4391:  beq.s      IL_43b4

    IL_4393:  ldstr      "counter64 mismatch {0} {1}"
    IL_4398:  ldsfld     int64 IntrinsicTest::counter64
    IL_439d:  box        [mscorlib]System.Int64
    IL_43a2:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_43a7:  box        [mscorlib]System.Int64
    IL_43ac:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_43b1:  ldc.i4.1
    IL_43b2:  stloc.s    V_20
    IL_43b4:  ldloc.s    V_21
    IL_43b6:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_43bb:  ldloc.s    V_21
    IL_43bd:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_43c2:  beq.s      IL_43e9

    IL_43c4:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_43c9:  ldloc.s    V_21
    IL_43cb:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_43d0:  box        [mscorlib]System.Int32
    IL_43d5:  ldloc.s    V_21
    IL_43d7:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_43dc:  box        [mscorlib]System.Int32
    IL_43e1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_43e6:  ldc.i4.1
    IL_43e7:  stloc.s    V_20
    IL_43e9:  ldloc.s    V_21
    IL_43eb:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_43f0:  ldloc.s    V_21
    IL_43f2:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_43f7:  beq.s      IL_441e

    IL_43f9:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_43fe:  ldloc.s    V_21
    IL_4400:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_4405:  box        [mscorlib]System.Int64
    IL_440a:  ldloc.s    V_21
    IL_440c:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_4411:  box        [mscorlib]System.Int64
    IL_4416:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_441b:  ldc.i4.1
    IL_441c:  stloc.s    V_20
    IL_441e:  ldsfld     int64 IntrinsicTest::temp
    IL_4423:  ldsfld     int64 IntrinsicTest::idmp
    IL_4428:  beq.s      IL_444b

    IL_442a:  ldstr      "temp mismatch {0} {1}"
    IL_442f:  ldsfld     int64 IntrinsicTest::temp
    IL_4434:  box        [mscorlib]System.Int64
    IL_4439:  ldsfld     int64 IntrinsicTest::idmp
    IL_443e:  box        [mscorlib]System.Int64
    IL_4443:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4448:  ldc.i4.1
    IL_4449:  stloc.s    V_20
    IL_444b:  ldsflda    int32 IntrinsicTest::counter
    IL_4450:  ldsfld     int32 IntrinsicTest::counter
    IL_4455:  ldloc.s    V_21
    IL_4457:  callvirt   instance int32 IntrinsicTest::GetValue()
    IL_445c:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_4461:  stloc.3
    IL_4462:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4467:  stloc.s    V_13
    IL_4469:  ldsfld     int32 IntrinsicTest::id_counter
    IL_446e:  ldloc.s    V_21
    IL_4470:  callvirt   instance int32 IntrinsicTest::id_GetValue()
    IL_4475:  bne.un.s   IL_4483

    IL_4477:  ldsfld     int32 IntrinsicTest::id_counter
    IL_447c:  stsfld     int32 IntrinsicTest::id_counter
    IL_4481:  br.s       IL_448e

    IL_4483:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4488:  conv.i8
    IL_4489:  stsfld     int64 IntrinsicTest::idjunk
    IL_448e:  ldstr      "------------------------------------CMPXCHG  1 1 3 6"
    IL_4493:  call       void [System.Console]System.Console::WriteLine(string)
    IL_4498:  ldloc.0
    IL_4499:  ldloc.s    V_10
    IL_449b:  beq.s      IL_44b7

    IL_449d:  ldc.i4.1
    IL_449e:  stloc.s    V_20
    IL_44a0:  ldstr      "te0 check failed {0} {1}"
    IL_44a5:  ldloc.0
    IL_44a6:  box        [mscorlib]System.Int32
    IL_44ab:  ldloc.s    V_10
    IL_44ad:  box        [mscorlib]System.Int32
    IL_44b2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_44b7:  ldloc.1
    IL_44b8:  ldloc.s    V_11
    IL_44ba:  beq.s      IL_44d6

    IL_44bc:  ldc.i4.1
    IL_44bd:  stloc.s    V_20
    IL_44bf:  ldstr      "te1 check failed {0} {1}"
    IL_44c4:  ldloc.1
    IL_44c5:  box        [mscorlib]System.Int32
    IL_44ca:  ldloc.s    V_11
    IL_44cc:  box        [mscorlib]System.Int32
    IL_44d1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_44d6:  ldloc.2
    IL_44d7:  ldloc.s    V_12
    IL_44d9:  beq.s      IL_44f5

    IL_44db:  ldc.i4.1
    IL_44dc:  stloc.s    V_20
    IL_44de:  ldstr      "te2 check failed {0} {1}"
    IL_44e3:  ldloc.2
    IL_44e4:  box        [mscorlib]System.Int32
    IL_44e9:  ldloc.s    V_12
    IL_44eb:  box        [mscorlib]System.Int32
    IL_44f0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_44f5:  ldloc.3
    IL_44f6:  ldloc.s    V_13
    IL_44f8:  beq.s      IL_4514

    IL_44fa:  ldc.i4.1
    IL_44fb:  stloc.s    V_20
    IL_44fd:  ldstr      "te3 check failed {0} {1}"
    IL_4502:  ldloc.3
    IL_4503:  box        [mscorlib]System.Int32
    IL_4508:  ldloc.s    V_13
    IL_450a:  box        [mscorlib]System.Int32
    IL_450f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4514:  ldloc.s    V_4
    IL_4516:  ldloc.s    V_14
    IL_4518:  beq.s      IL_4535

    IL_451a:  ldc.i4.1
    IL_451b:  stloc.s    V_20
    IL_451d:  ldstr      "te064 check failed {0} {1}"
    IL_4522:  ldloc.s    V_4
    IL_4524:  box        [mscorlib]System.Int64
    IL_4529:  ldloc.s    V_14
    IL_452b:  box        [mscorlib]System.Int64
    IL_4530:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4535:  ldloc.s    V_5
    IL_4537:  ldloc.s    V_15
    IL_4539:  beq.s      IL_4556

    IL_453b:  ldc.i4.1
    IL_453c:  stloc.s    V_20
    IL_453e:  ldstr      "te164 check failed {0} {1}"
    IL_4543:  ldloc.s    V_5
    IL_4545:  box        [mscorlib]System.Int64
    IL_454a:  ldloc.s    V_15
    IL_454c:  box        [mscorlib]System.Int64
    IL_4551:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4556:  ldloc.s    V_6
    IL_4558:  ldloc.s    V_16
    IL_455a:  beq.s      IL_4577

    IL_455c:  ldc.i4.1
    IL_455d:  stloc.s    V_20
    IL_455f:  ldstr      "te264 check failed {0} {1}"
    IL_4564:  ldloc.s    V_6
    IL_4566:  box        [mscorlib]System.Int64
    IL_456b:  ldloc.s    V_16
    IL_456d:  box        [mscorlib]System.Int64
    IL_4572:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4577:  ldloc.s    V_7
    IL_4579:  ldloc.s    V_17
    IL_457b:  beq.s      IL_4598

    IL_457d:  ldc.i4.1
    IL_457e:  stloc.s    V_20
    IL_4580:  ldstr      "te364 check failed {0} {1}"
    IL_4585:  ldloc.s    V_7
    IL_4587:  box        [mscorlib]System.Int64
    IL_458c:  ldloc.s    V_17
    IL_458e:  box        [mscorlib]System.Int64
    IL_4593:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4598:  ldloc.s    V_8
    IL_459a:  ldloc.s    V_18
    IL_459c:  beq.s      IL_45b9

    IL_459e:  ldc.i4.1
    IL_459f:  stloc.s    V_20
    IL_45a1:  ldstr      "dummy check failed {0} {1}"
    IL_45a6:  ldloc.s    V_8
    IL_45a8:  box        [mscorlib]System.Int32
    IL_45ad:  ldloc.s    V_18
    IL_45af:  box        [mscorlib]System.Int32
    IL_45b4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_45b9:  ldloc.s    V_9
    IL_45bb:  ldloc.s    V_19
    IL_45bd:  beq.s      IL_45da

    IL_45bf:  ldc.i4.1
    IL_45c0:  stloc.s    V_20
    IL_45c2:  ldstr      "dummy64 check failed {0} {1}"
    IL_45c7:  ldloc.s    V_9
    IL_45c9:  box        [mscorlib]System.Int64
    IL_45ce:  ldloc.s    V_19
    IL_45d0:  box        [mscorlib]System.Int64
    IL_45d5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_45da:  ldsfld     int32 IntrinsicTest::counter
    IL_45df:  ldsfld     int32 IntrinsicTest::id_counter
    IL_45e4:  beq.s      IL_4607

    IL_45e6:  ldstr      "counter mismatch {0} {1}"
    IL_45eb:  ldsfld     int32 IntrinsicTest::counter
    IL_45f0:  box        [mscorlib]System.Int32
    IL_45f5:  ldsfld     int32 IntrinsicTest::id_counter
    IL_45fa:  box        [mscorlib]System.Int32
    IL_45ff:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4604:  ldc.i4.1
    IL_4605:  stloc.s    V_20
    IL_4607:  ldsfld     int64 IntrinsicTest::counter64
    IL_460c:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4611:  beq.s      IL_4634

    IL_4613:  ldstr      "counter64 mismatch {0} {1}"
    IL_4618:  ldsfld     int64 IntrinsicTest::counter64
    IL_461d:  box        [mscorlib]System.Int64
    IL_4622:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4627:  box        [mscorlib]System.Int64
    IL_462c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4631:  ldc.i4.1
    IL_4632:  stloc.s    V_20
    IL_4634:  ldloc.s    V_21
    IL_4636:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_463b:  ldloc.s    V_21
    IL_463d:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_4642:  beq.s      IL_4669

    IL_4644:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_4649:  ldloc.s    V_21
    IL_464b:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_4650:  box        [mscorlib]System.Int32
    IL_4655:  ldloc.s    V_21
    IL_4657:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_465c:  box        [mscorlib]System.Int32
    IL_4661:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4666:  ldc.i4.1
    IL_4667:  stloc.s    V_20
    IL_4669:  ldloc.s    V_21
    IL_466b:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_4670:  ldloc.s    V_21
    IL_4672:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_4677:  beq.s      IL_469e

    IL_4679:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_467e:  ldloc.s    V_21
    IL_4680:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_4685:  box        [mscorlib]System.Int64
    IL_468a:  ldloc.s    V_21
    IL_468c:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_4691:  box        [mscorlib]System.Int64
    IL_4696:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_469b:  ldc.i4.1
    IL_469c:  stloc.s    V_20
    IL_469e:  ldsfld     int64 IntrinsicTest::temp
    IL_46a3:  ldsfld     int64 IntrinsicTest::idmp
    IL_46a8:  beq.s      IL_46cb

    IL_46aa:  ldstr      "temp mismatch {0} {1}"
    IL_46af:  ldsfld     int64 IntrinsicTest::temp
    IL_46b4:  box        [mscorlib]System.Int64
    IL_46b9:  ldsfld     int64 IntrinsicTest::idmp
    IL_46be:  box        [mscorlib]System.Int64
    IL_46c3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_46c8:  ldc.i4.1
    IL_46c9:  stloc.s    V_20
    IL_46cb:  ldsflda    int32 IntrinsicTest::counter
    IL_46d0:  ldsfld     int32 IntrinsicTest::counter
    IL_46d5:  ldc.i4     0x618
    IL_46da:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_46df:  stloc.0
    IL_46e0:  ldsfld     int32 IntrinsicTest::id_counter
    IL_46e5:  stloc.s    V_10
    IL_46e7:  ldsfld     int32 IntrinsicTest::id_counter
    IL_46ec:  ldc.i4     0x618
    IL_46f1:  bne.un.s   IL_46ff

    IL_46f3:  ldsfld     int32 IntrinsicTest::id_counter
    IL_46f8:  stsfld     int32 IntrinsicTest::id_counter
    IL_46fd:  br.s       IL_470a

    IL_46ff:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4704:  conv.i8
    IL_4705:  stsfld     int64 IntrinsicTest::idjunk
    IL_470a:  ldstr      "------------------------------------CMPXCHG  1 1 4 0"
    IL_470f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_4714:  ldloc.0
    IL_4715:  ldloc.s    V_10
    IL_4717:  beq.s      IL_4733

    IL_4719:  ldc.i4.1
    IL_471a:  stloc.s    V_20
    IL_471c:  ldstr      "te0 check failed {0} {1}"
    IL_4721:  ldloc.0
    IL_4722:  box        [mscorlib]System.Int32
    IL_4727:  ldloc.s    V_10
    IL_4729:  box        [mscorlib]System.Int32
    IL_472e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4733:  ldloc.1
    IL_4734:  ldloc.s    V_11
    IL_4736:  beq.s      IL_4752

    IL_4738:  ldc.i4.1
    IL_4739:  stloc.s    V_20
    IL_473b:  ldstr      "te1 check failed {0} {1}"
    IL_4740:  ldloc.1
    IL_4741:  box        [mscorlib]System.Int32
    IL_4746:  ldloc.s    V_11
    IL_4748:  box        [mscorlib]System.Int32
    IL_474d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4752:  ldloc.2
    IL_4753:  ldloc.s    V_12
    IL_4755:  beq.s      IL_4771

    IL_4757:  ldc.i4.1
    IL_4758:  stloc.s    V_20
    IL_475a:  ldstr      "te2 check failed {0} {1}"
    IL_475f:  ldloc.2
    IL_4760:  box        [mscorlib]System.Int32
    IL_4765:  ldloc.s    V_12
    IL_4767:  box        [mscorlib]System.Int32
    IL_476c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4771:  ldloc.3
    IL_4772:  ldloc.s    V_13
    IL_4774:  beq.s      IL_4790

    IL_4776:  ldc.i4.1
    IL_4777:  stloc.s    V_20
    IL_4779:  ldstr      "te3 check failed {0} {1}"
    IL_477e:  ldloc.3
    IL_477f:  box        [mscorlib]System.Int32
    IL_4784:  ldloc.s    V_13
    IL_4786:  box        [mscorlib]System.Int32
    IL_478b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4790:  ldloc.s    V_4
    IL_4792:  ldloc.s    V_14
    IL_4794:  beq.s      IL_47b1

    IL_4796:  ldc.i4.1
    IL_4797:  stloc.s    V_20
    IL_4799:  ldstr      "te064 check failed {0} {1}"
    IL_479e:  ldloc.s    V_4
    IL_47a0:  box        [mscorlib]System.Int64
    IL_47a5:  ldloc.s    V_14
    IL_47a7:  box        [mscorlib]System.Int64
    IL_47ac:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_47b1:  ldloc.s    V_5
    IL_47b3:  ldloc.s    V_15
    IL_47b5:  beq.s      IL_47d2

    IL_47b7:  ldc.i4.1
    IL_47b8:  stloc.s    V_20
    IL_47ba:  ldstr      "te164 check failed {0} {1}"
    IL_47bf:  ldloc.s    V_5
    IL_47c1:  box        [mscorlib]System.Int64
    IL_47c6:  ldloc.s    V_15
    IL_47c8:  box        [mscorlib]System.Int64
    IL_47cd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_47d2:  ldloc.s    V_6
    IL_47d4:  ldloc.s    V_16
    IL_47d6:  beq.s      IL_47f3

    IL_47d8:  ldc.i4.1
    IL_47d9:  stloc.s    V_20
    IL_47db:  ldstr      "te264 check failed {0} {1}"
    IL_47e0:  ldloc.s    V_6
    IL_47e2:  box        [mscorlib]System.Int64
    IL_47e7:  ldloc.s    V_16
    IL_47e9:  box        [mscorlib]System.Int64
    IL_47ee:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_47f3:  ldloc.s    V_7
    IL_47f5:  ldloc.s    V_17
    IL_47f7:  beq.s      IL_4814

    IL_47f9:  ldc.i4.1
    IL_47fa:  stloc.s    V_20
    IL_47fc:  ldstr      "te364 check failed {0} {1}"
    IL_4801:  ldloc.s    V_7
    IL_4803:  box        [mscorlib]System.Int64
    IL_4808:  ldloc.s    V_17
    IL_480a:  box        [mscorlib]System.Int64
    IL_480f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4814:  ldloc.s    V_8
    IL_4816:  ldloc.s    V_18
    IL_4818:  beq.s      IL_4835

    IL_481a:  ldc.i4.1
    IL_481b:  stloc.s    V_20
    IL_481d:  ldstr      "dummy check failed {0} {1}"
    IL_4822:  ldloc.s    V_8
    IL_4824:  box        [mscorlib]System.Int32
    IL_4829:  ldloc.s    V_18
    IL_482b:  box        [mscorlib]System.Int32
    IL_4830:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4835:  ldloc.s    V_9
    IL_4837:  ldloc.s    V_19
    IL_4839:  beq.s      IL_4856

    IL_483b:  ldc.i4.1
    IL_483c:  stloc.s    V_20
    IL_483e:  ldstr      "dummy64 check failed {0} {1}"
    IL_4843:  ldloc.s    V_9
    IL_4845:  box        [mscorlib]System.Int64
    IL_484a:  ldloc.s    V_19
    IL_484c:  box        [mscorlib]System.Int64
    IL_4851:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4856:  ldsfld     int32 IntrinsicTest::counter
    IL_485b:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4860:  beq.s      IL_4883

    IL_4862:  ldstr      "counter mismatch {0} {1}"
    IL_4867:  ldsfld     int32 IntrinsicTest::counter
    IL_486c:  box        [mscorlib]System.Int32
    IL_4871:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4876:  box        [mscorlib]System.Int32
    IL_487b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4880:  ldc.i4.1
    IL_4881:  stloc.s    V_20
    IL_4883:  ldsfld     int64 IntrinsicTest::counter64
    IL_4888:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_488d:  beq.s      IL_48b0

    IL_488f:  ldstr      "counter64 mismatch {0} {1}"
    IL_4894:  ldsfld     int64 IntrinsicTest::counter64
    IL_4899:  box        [mscorlib]System.Int64
    IL_489e:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_48a3:  box        [mscorlib]System.Int64
    IL_48a8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_48ad:  ldc.i4.1
    IL_48ae:  stloc.s    V_20
    IL_48b0:  ldloc.s    V_21
    IL_48b2:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_48b7:  ldloc.s    V_21
    IL_48b9:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_48be:  beq.s      IL_48e5

    IL_48c0:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_48c5:  ldloc.s    V_21
    IL_48c7:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_48cc:  box        [mscorlib]System.Int32
    IL_48d1:  ldloc.s    V_21
    IL_48d3:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_48d8:  box        [mscorlib]System.Int32
    IL_48dd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_48e2:  ldc.i4.1
    IL_48e3:  stloc.s    V_20
    IL_48e5:  ldloc.s    V_21
    IL_48e7:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_48ec:  ldloc.s    V_21
    IL_48ee:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_48f3:  beq.s      IL_491a

    IL_48f5:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_48fa:  ldloc.s    V_21
    IL_48fc:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_4901:  box        [mscorlib]System.Int64
    IL_4906:  ldloc.s    V_21
    IL_4908:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_490d:  box        [mscorlib]System.Int64
    IL_4912:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4917:  ldc.i4.1
    IL_4918:  stloc.s    V_20
    IL_491a:  ldsfld     int64 IntrinsicTest::temp
    IL_491f:  ldsfld     int64 IntrinsicTest::idmp
    IL_4924:  beq.s      IL_4947

    IL_4926:  ldstr      "temp mismatch {0} {1}"
    IL_492b:  ldsfld     int64 IntrinsicTest::temp
    IL_4930:  box        [mscorlib]System.Int64
    IL_4935:  ldsfld     int64 IntrinsicTest::idmp
    IL_493a:  box        [mscorlib]System.Int64
    IL_493f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4944:  ldc.i4.1
    IL_4945:  stloc.s    V_20
    IL_4947:  ldsflda    int32 IntrinsicTest::counter
    IL_494c:  ldsfld     int32 IntrinsicTest::counter
    IL_4951:  ldc.i4     0x618
    IL_4956:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_495b:  stsfld     int32 IntrinsicTest::counter
    IL_4960:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4965:  stsfld     int32 IntrinsicTest::id_counter
    IL_496a:  ldsfld     int32 IntrinsicTest::id_counter
    IL_496f:  ldc.i4     0x618
    IL_4974:  bne.un.s   IL_4983

    IL_4976:  ldsfld     int32 IntrinsicTest::id_counter
    IL_497b:  conv.i8
    IL_497c:  stsfld     int64 IntrinsicTest::idjunk
    IL_4981:  br.s       IL_498e

    IL_4983:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4988:  conv.i8
    IL_4989:  stsfld     int64 IntrinsicTest::idjunk
    IL_498e:  ldstr      "------------------------------------CMPXCHG  1 1 4 1"
    IL_4993:  call       void [System.Console]System.Console::WriteLine(string)
    IL_4998:  ldloc.0
    IL_4999:  ldloc.s    V_10
    IL_499b:  beq.s      IL_49b7

    IL_499d:  ldc.i4.1
    IL_499e:  stloc.s    V_20
    IL_49a0:  ldstr      "te0 check failed {0} {1}"
    IL_49a5:  ldloc.0
    IL_49a6:  box        [mscorlib]System.Int32
    IL_49ab:  ldloc.s    V_10
    IL_49ad:  box        [mscorlib]System.Int32
    IL_49b2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_49b7:  ldloc.1
    IL_49b8:  ldloc.s    V_11
    IL_49ba:  beq.s      IL_49d6

    IL_49bc:  ldc.i4.1
    IL_49bd:  stloc.s    V_20
    IL_49bf:  ldstr      "te1 check failed {0} {1}"
    IL_49c4:  ldloc.1
    IL_49c5:  box        [mscorlib]System.Int32
    IL_49ca:  ldloc.s    V_11
    IL_49cc:  box        [mscorlib]System.Int32
    IL_49d1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_49d6:  ldloc.2
    IL_49d7:  ldloc.s    V_12
    IL_49d9:  beq.s      IL_49f5

    IL_49db:  ldc.i4.1
    IL_49dc:  stloc.s    V_20
    IL_49de:  ldstr      "te2 check failed {0} {1}"
    IL_49e3:  ldloc.2
    IL_49e4:  box        [mscorlib]System.Int32
    IL_49e9:  ldloc.s    V_12
    IL_49eb:  box        [mscorlib]System.Int32
    IL_49f0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_49f5:  ldloc.3
    IL_49f6:  ldloc.s    V_13
    IL_49f8:  beq.s      IL_4a14

    IL_49fa:  ldc.i4.1
    IL_49fb:  stloc.s    V_20
    IL_49fd:  ldstr      "te3 check failed {0} {1}"
    IL_4a02:  ldloc.3
    IL_4a03:  box        [mscorlib]System.Int32
    IL_4a08:  ldloc.s    V_13
    IL_4a0a:  box        [mscorlib]System.Int32
    IL_4a0f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4a14:  ldloc.s    V_4
    IL_4a16:  ldloc.s    V_14
    IL_4a18:  beq.s      IL_4a35

    IL_4a1a:  ldc.i4.1
    IL_4a1b:  stloc.s    V_20
    IL_4a1d:  ldstr      "te064 check failed {0} {1}"
    IL_4a22:  ldloc.s    V_4
    IL_4a24:  box        [mscorlib]System.Int64
    IL_4a29:  ldloc.s    V_14
    IL_4a2b:  box        [mscorlib]System.Int64
    IL_4a30:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4a35:  ldloc.s    V_5
    IL_4a37:  ldloc.s    V_15
    IL_4a39:  beq.s      IL_4a56

    IL_4a3b:  ldc.i4.1
    IL_4a3c:  stloc.s    V_20
    IL_4a3e:  ldstr      "te164 check failed {0} {1}"
    IL_4a43:  ldloc.s    V_5
    IL_4a45:  box        [mscorlib]System.Int64
    IL_4a4a:  ldloc.s    V_15
    IL_4a4c:  box        [mscorlib]System.Int64
    IL_4a51:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4a56:  ldloc.s    V_6
    IL_4a58:  ldloc.s    V_16
    IL_4a5a:  beq.s      IL_4a77

    IL_4a5c:  ldc.i4.1
    IL_4a5d:  stloc.s    V_20
    IL_4a5f:  ldstr      "te264 check failed {0} {1}"
    IL_4a64:  ldloc.s    V_6
    IL_4a66:  box        [mscorlib]System.Int64
    IL_4a6b:  ldloc.s    V_16
    IL_4a6d:  box        [mscorlib]System.Int64
    IL_4a72:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4a77:  ldloc.s    V_7
    IL_4a79:  ldloc.s    V_17
    IL_4a7b:  beq.s      IL_4a98

    IL_4a7d:  ldc.i4.1
    IL_4a7e:  stloc.s    V_20
    IL_4a80:  ldstr      "te364 check failed {0} {1}"
    IL_4a85:  ldloc.s    V_7
    IL_4a87:  box        [mscorlib]System.Int64
    IL_4a8c:  ldloc.s    V_17
    IL_4a8e:  box        [mscorlib]System.Int64
    IL_4a93:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4a98:  ldloc.s    V_8
    IL_4a9a:  ldloc.s    V_18
    IL_4a9c:  beq.s      IL_4ab9

    IL_4a9e:  ldc.i4.1
    IL_4a9f:  stloc.s    V_20
    IL_4aa1:  ldstr      "dummy check failed {0} {1}"
    IL_4aa6:  ldloc.s    V_8
    IL_4aa8:  box        [mscorlib]System.Int32
    IL_4aad:  ldloc.s    V_18
    IL_4aaf:  box        [mscorlib]System.Int32
    IL_4ab4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4ab9:  ldloc.s    V_9
    IL_4abb:  ldloc.s    V_19
    IL_4abd:  beq.s      IL_4ada

    IL_4abf:  ldc.i4.1
    IL_4ac0:  stloc.s    V_20
    IL_4ac2:  ldstr      "dummy64 check failed {0} {1}"
    IL_4ac7:  ldloc.s    V_9
    IL_4ac9:  box        [mscorlib]System.Int64
    IL_4ace:  ldloc.s    V_19
    IL_4ad0:  box        [mscorlib]System.Int64
    IL_4ad5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4ada:  ldsfld     int32 IntrinsicTest::counter
    IL_4adf:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4ae4:  beq.s      IL_4b07

    IL_4ae6:  ldstr      "counter mismatch {0} {1}"
    IL_4aeb:  ldsfld     int32 IntrinsicTest::counter
    IL_4af0:  box        [mscorlib]System.Int32
    IL_4af5:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4afa:  box        [mscorlib]System.Int32
    IL_4aff:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4b04:  ldc.i4.1
    IL_4b05:  stloc.s    V_20
    IL_4b07:  ldsfld     int64 IntrinsicTest::counter64
    IL_4b0c:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4b11:  beq.s      IL_4b34

    IL_4b13:  ldstr      "counter64 mismatch {0} {1}"
    IL_4b18:  ldsfld     int64 IntrinsicTest::counter64
    IL_4b1d:  box        [mscorlib]System.Int64
    IL_4b22:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_4b27:  box        [mscorlib]System.Int64
    IL_4b2c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4b31:  ldc.i4.1
    IL_4b32:  stloc.s    V_20
    IL_4b34:  ldloc.s    V_21
    IL_4b36:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_4b3b:  ldloc.s    V_21
    IL_4b3d:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_4b42:  beq.s      IL_4b69

    IL_4b44:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_4b49:  ldloc.s    V_21
    IL_4b4b:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_4b50:  box        [mscorlib]System.Int32
    IL_4b55:  ldloc.s    V_21
    IL_4b57:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_4b5c:  box        [mscorlib]System.Int32
    IL_4b61:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4b66:  ldc.i4.1
    IL_4b67:  stloc.s    V_20
    IL_4b69:  ldloc.s    V_21
    IL_4b6b:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_4b70:  ldloc.s    V_21
    IL_4b72:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_4b77:  beq.s      IL_4b9e

    IL_4b79:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_4b7e:  ldloc.s    V_21
    IL_4b80:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_4b85:  box        [mscorlib]System.Int64
    IL_4b8a:  ldloc.s    V_21
    IL_4b8c:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_4b91:  box        [mscorlib]System.Int64
    IL_4b96:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4b9b:  ldc.i4.1
    IL_4b9c:  stloc.s    V_20
    IL_4b9e:  ldsfld     int64 IntrinsicTest::temp
    IL_4ba3:  ldsfld     int64 IntrinsicTest::idmp
    IL_4ba8:  beq.s      IL_4bcb

    IL_4baa:  ldstr      "temp mismatch {0} {1}"
    IL_4baf:  ldsfld     int64 IntrinsicTest::temp
    IL_4bb4:  box        [mscorlib]System.Int64
    IL_4bb9:  ldsfld     int64 IntrinsicTest::idmp
    IL_4bbe:  box        [mscorlib]System.Int64
    IL_4bc3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4bc8:  ldc.i4.1
    IL_4bc9:  stloc.s    V_20
    IL_4bcb:  ldloc.s    V_21
    IL_4bcd:  ldsflda    int32 IntrinsicTest::counter
    IL_4bd2:  ldsfld     int32 IntrinsicTest::counter
    IL_4bd7:  ldc.i4     0x618
    IL_4bdc:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_4be1:  stfld      int32 IntrinsicTest::instanceCounter
    IL_4be6:  ldloc.s    V_21
    IL_4be8:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4bed:  stfld      int32 IntrinsicTest::id_instanceCounter
    IL_4bf2:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4bf7:  ldc.i4     0x618
    IL_4bfc:  bne.un.s   IL_4c0a

    IL_4bfe:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4c03:  stsfld     int32 IntrinsicTest::id_counter
    IL_4c08:  br.s       IL_4c15

    IL_4c0a:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4c0f:  conv.i8
    IL_4c10:  stsfld     int64 IntrinsicTest::idjunk
    IL_4c15:  ldstr      "------------------------------------CMPXCHG  1 1 4 2"
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
    IL_4e52:  ldsflda    int32 IntrinsicTest::counter
    IL_4e57:  ldsfld     int32 IntrinsicTest::counter
    IL_4e5c:  ldc.i4     0x618
    IL_4e61:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_4e66:  stloc.0
    IL_4e67:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4e6c:  stloc.s    V_10
    IL_4e6e:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4e73:  ldc.i4     0x618
    IL_4e78:  bne.un.s   IL_4e86

    IL_4e7a:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4e7f:  stsfld     int32 IntrinsicTest::id_counter
    IL_4e84:  br.s       IL_4e91

    IL_4e86:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4e8b:  conv.i8
    IL_4e8c:  stsfld     int64 IntrinsicTest::idjunk
    IL_4e91:  ldstr      "------------------------------------CMPXCHG  1 1 4 3"
    IL_4e96:  call       void [System.Console]System.Console::WriteLine(string)
    IL_4e9b:  ldloc.0
    IL_4e9c:  ldloc.s    V_10
    IL_4e9e:  beq.s      IL_4eba

    IL_4ea0:  ldc.i4.1
    IL_4ea1:  stloc.s    V_20
    IL_4ea3:  ldstr      "te0 check failed {0} {1}"
    IL_4ea8:  ldloc.0
    IL_4ea9:  box        [mscorlib]System.Int32
    IL_4eae:  ldloc.s    V_10
    IL_4eb0:  box        [mscorlib]System.Int32
    IL_4eb5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4eba:  ldloc.1
    IL_4ebb:  ldloc.s    V_11
    IL_4ebd:  beq.s      IL_4ed9

    IL_4ebf:  ldc.i4.1
    IL_4ec0:  stloc.s    V_20
    IL_4ec2:  ldstr      "te1 check failed {0} {1}"
    IL_4ec7:  ldloc.1
    IL_4ec8:  box        [mscorlib]System.Int32
    IL_4ecd:  ldloc.s    V_11
    IL_4ecf:  box        [mscorlib]System.Int32
    IL_4ed4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4ed9:  ldloc.2
    IL_4eda:  ldloc.s    V_12
    IL_4edc:  beq.s      IL_4ef8

    IL_4ede:  ldc.i4.1
    IL_4edf:  stloc.s    V_20
    IL_4ee1:  ldstr      "te2 check failed {0} {1}"
    IL_4ee6:  ldloc.2
    IL_4ee7:  box        [mscorlib]System.Int32
    IL_4eec:  ldloc.s    V_12
    IL_4eee:  box        [mscorlib]System.Int32
    IL_4ef3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4ef8:  ldloc.3
    IL_4ef9:  ldloc.s    V_13
    IL_4efb:  beq.s      IL_4f17

    IL_4efd:  ldc.i4.1
    IL_4efe:  stloc.s    V_20
    IL_4f00:  ldstr      "te3 check failed {0} {1}"
    IL_4f05:  ldloc.3
    IL_4f06:  box        [mscorlib]System.Int32
    IL_4f0b:  ldloc.s    V_13
    IL_4f0d:  box        [mscorlib]System.Int32
    IL_4f12:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4f17:  ldloc.s    V_4
    IL_4f19:  ldloc.s    V_14
    IL_4f1b:  beq.s      IL_4f38

    IL_4f1d:  ldc.i4.1
    IL_4f1e:  stloc.s    V_20
    IL_4f20:  ldstr      "te064 check failed {0} {1}"
    IL_4f25:  ldloc.s    V_4
    IL_4f27:  box        [mscorlib]System.Int64
    IL_4f2c:  ldloc.s    V_14
    IL_4f2e:  box        [mscorlib]System.Int64
    IL_4f33:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4f38:  ldloc.s    V_5
    IL_4f3a:  ldloc.s    V_15
    IL_4f3c:  beq.s      IL_4f59

    IL_4f3e:  ldc.i4.1
    IL_4f3f:  stloc.s    V_20
    IL_4f41:  ldstr      "te164 check failed {0} {1}"
    IL_4f46:  ldloc.s    V_5
    IL_4f48:  box        [mscorlib]System.Int64
    IL_4f4d:  ldloc.s    V_15
    IL_4f4f:  box        [mscorlib]System.Int64
    IL_4f54:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4f59:  ldloc.s    V_6
    IL_4f5b:  ldloc.s    V_16
    IL_4f5d:  beq.s      IL_4f7a

    IL_4f5f:  ldc.i4.1
    IL_4f60:  stloc.s    V_20
    IL_4f62:  ldstr      "te264 check failed {0} {1}"
    IL_4f67:  ldloc.s    V_6
    IL_4f69:  box        [mscorlib]System.Int64
    IL_4f6e:  ldloc.s    V_16
    IL_4f70:  box        [mscorlib]System.Int64
    IL_4f75:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4f7a:  ldloc.s    V_7
    IL_4f7c:  ldloc.s    V_17
    IL_4f7e:  beq.s      IL_4f9b

    IL_4f80:  ldc.i4.1
    IL_4f81:  stloc.s    V_20
    IL_4f83:  ldstr      "te364 check failed {0} {1}"
    IL_4f88:  ldloc.s    V_7
    IL_4f8a:  box        [mscorlib]System.Int64
    IL_4f8f:  ldloc.s    V_17
    IL_4f91:  box        [mscorlib]System.Int64
    IL_4f96:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4f9b:  ldloc.s    V_8
    IL_4f9d:  ldloc.s    V_18
    IL_4f9f:  beq.s      IL_4fbc

    IL_4fa1:  ldc.i4.1
    IL_4fa2:  stloc.s    V_20
    IL_4fa4:  ldstr      "dummy check failed {0} {1}"
    IL_4fa9:  ldloc.s    V_8
    IL_4fab:  box        [mscorlib]System.Int32
    IL_4fb0:  ldloc.s    V_18
    IL_4fb2:  box        [mscorlib]System.Int32
    IL_4fb7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4fbc:  ldloc.s    V_9
    IL_4fbe:  ldloc.s    V_19
    IL_4fc0:  beq.s      IL_4fdd

    IL_4fc2:  ldc.i4.1
    IL_4fc3:  stloc.s    V_20
    IL_4fc5:  ldstr      "dummy64 check failed {0} {1}"
    IL_4fca:  ldloc.s    V_9
    IL_4fcc:  box        [mscorlib]System.Int64
    IL_4fd1:  ldloc.s    V_19
    IL_4fd3:  box        [mscorlib]System.Int64
    IL_4fd8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_4fdd:  ldsfld     int32 IntrinsicTest::counter
    IL_4fe2:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4fe7:  beq.s      IL_500a

    IL_4fe9:  ldstr      "counter mismatch {0} {1}"
    IL_4fee:  ldsfld     int32 IntrinsicTest::counter
    IL_4ff3:  box        [mscorlib]System.Int32
    IL_4ff8:  ldsfld     int32 IntrinsicTest::id_counter
    IL_4ffd:  box        [mscorlib]System.Int32
    IL_5002:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5007:  ldc.i4.1
    IL_5008:  stloc.s    V_20
    IL_500a:  ldsfld     int64 IntrinsicTest::counter64
    IL_500f:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5014:  beq.s      IL_5037

    IL_5016:  ldstr      "counter64 mismatch {0} {1}"
    IL_501b:  ldsfld     int64 IntrinsicTest::counter64
    IL_5020:  box        [mscorlib]System.Int64
    IL_5025:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_502a:  box        [mscorlib]System.Int64
    IL_502f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5034:  ldc.i4.1
    IL_5035:  stloc.s    V_20
    IL_5037:  ldloc.s    V_21
    IL_5039:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_503e:  ldloc.s    V_21
    IL_5040:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_5045:  beq.s      IL_506c

    IL_5047:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_504c:  ldloc.s    V_21
    IL_504e:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_5053:  box        [mscorlib]System.Int32
    IL_5058:  ldloc.s    V_21
    IL_505a:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_505f:  box        [mscorlib]System.Int32
    IL_5064:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5069:  ldc.i4.1
    IL_506a:  stloc.s    V_20
    IL_506c:  ldloc.s    V_21
    IL_506e:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_5073:  ldloc.s    V_21
    IL_5075:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_507a:  beq.s      IL_50a1

    IL_507c:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_5081:  ldloc.s    V_21
    IL_5083:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_5088:  box        [mscorlib]System.Int64
    IL_508d:  ldloc.s    V_21
    IL_508f:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_5094:  box        [mscorlib]System.Int64
    IL_5099:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_509e:  ldc.i4.1
    IL_509f:  stloc.s    V_20
    IL_50a1:  ldsfld     int64 IntrinsicTest::temp
    IL_50a6:  ldsfld     int64 IntrinsicTest::idmp
    IL_50ab:  beq.s      IL_50ce

    IL_50ad:  ldstr      "temp mismatch {0} {1}"
    IL_50b2:  ldsfld     int64 IntrinsicTest::temp
    IL_50b7:  box        [mscorlib]System.Int64
    IL_50bc:  ldsfld     int64 IntrinsicTest::idmp
    IL_50c1:  box        [mscorlib]System.Int64
    IL_50c6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_50cb:  ldc.i4.1
    IL_50cc:  stloc.s    V_20
    IL_50ce:  ldsflda    int32 IntrinsicTest::counter
    IL_50d3:  ldsfld     int32 IntrinsicTest::counter
    IL_50d8:  ldc.i4     0x618
    IL_50dd:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_50e2:  stsfld     int32 IntrinsicTest::counter
    IL_50e7:  ldsfld     int32 IntrinsicTest::id_counter
    IL_50ec:  stsfld     int32 IntrinsicTest::id_counter
    IL_50f1:  ldsfld     int32 IntrinsicTest::id_counter
    IL_50f6:  ldc.i4     0x618
    IL_50fb:  bne.un.s   IL_510a

    IL_50fd:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5102:  conv.i8
    IL_5103:  stsfld     int64 IntrinsicTest::idjunk
    IL_5108:  br.s       IL_5115

    IL_510a:  ldsfld     int32 IntrinsicTest::id_counter
    IL_510f:  conv.i8
    IL_5110:  stsfld     int64 IntrinsicTest::idjunk
    IL_5115:  ldstr      "------------------------------------CMPXCHG  1 1 4 4"
    IL_511a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_511f:  ldloc.0
    IL_5120:  ldloc.s    V_10
    IL_5122:  beq.s      IL_513e

    IL_5124:  ldc.i4.1
    IL_5125:  stloc.s    V_20
    IL_5127:  ldstr      "te0 check failed {0} {1}"
    IL_512c:  ldloc.0
    IL_512d:  box        [mscorlib]System.Int32
    IL_5132:  ldloc.s    V_10
    IL_5134:  box        [mscorlib]System.Int32
    IL_5139:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_513e:  ldloc.1
    IL_513f:  ldloc.s    V_11
    IL_5141:  beq.s      IL_515d

    IL_5143:  ldc.i4.1
    IL_5144:  stloc.s    V_20
    IL_5146:  ldstr      "te1 check failed {0} {1}"
    IL_514b:  ldloc.1
    IL_514c:  box        [mscorlib]System.Int32
    IL_5151:  ldloc.s    V_11
    IL_5153:  box        [mscorlib]System.Int32
    IL_5158:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_515d:  ldloc.2
    IL_515e:  ldloc.s    V_12
    IL_5160:  beq.s      IL_517c

    IL_5162:  ldc.i4.1
    IL_5163:  stloc.s    V_20
    IL_5165:  ldstr      "te2 check failed {0} {1}"
    IL_516a:  ldloc.2
    IL_516b:  box        [mscorlib]System.Int32
    IL_5170:  ldloc.s    V_12
    IL_5172:  box        [mscorlib]System.Int32
    IL_5177:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_517c:  ldloc.3
    IL_517d:  ldloc.s    V_13
    IL_517f:  beq.s      IL_519b

    IL_5181:  ldc.i4.1
    IL_5182:  stloc.s    V_20
    IL_5184:  ldstr      "te3 check failed {0} {1}"
    IL_5189:  ldloc.3
    IL_518a:  box        [mscorlib]System.Int32
    IL_518f:  ldloc.s    V_13
    IL_5191:  box        [mscorlib]System.Int32
    IL_5196:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_519b:  ldloc.s    V_4
    IL_519d:  ldloc.s    V_14
    IL_519f:  beq.s      IL_51bc

    IL_51a1:  ldc.i4.1
    IL_51a2:  stloc.s    V_20
    IL_51a4:  ldstr      "te064 check failed {0} {1}"
    IL_51a9:  ldloc.s    V_4
    IL_51ab:  box        [mscorlib]System.Int64
    IL_51b0:  ldloc.s    V_14
    IL_51b2:  box        [mscorlib]System.Int64
    IL_51b7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_51bc:  ldloc.s    V_5
    IL_51be:  ldloc.s    V_15
    IL_51c0:  beq.s      IL_51dd

    IL_51c2:  ldc.i4.1
    IL_51c3:  stloc.s    V_20
    IL_51c5:  ldstr      "te164 check failed {0} {1}"
    IL_51ca:  ldloc.s    V_5
    IL_51cc:  box        [mscorlib]System.Int64
    IL_51d1:  ldloc.s    V_15
    IL_51d3:  box        [mscorlib]System.Int64
    IL_51d8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_51dd:  ldloc.s    V_6
    IL_51df:  ldloc.s    V_16
    IL_51e1:  beq.s      IL_51fe

    IL_51e3:  ldc.i4.1
    IL_51e4:  stloc.s    V_20
    IL_51e6:  ldstr      "te264 check failed {0} {1}"
    IL_51eb:  ldloc.s    V_6
    IL_51ed:  box        [mscorlib]System.Int64
    IL_51f2:  ldloc.s    V_16
    IL_51f4:  box        [mscorlib]System.Int64
    IL_51f9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_51fe:  ldloc.s    V_7
    IL_5200:  ldloc.s    V_17
    IL_5202:  beq.s      IL_521f

    IL_5204:  ldc.i4.1
    IL_5205:  stloc.s    V_20
    IL_5207:  ldstr      "te364 check failed {0} {1}"
    IL_520c:  ldloc.s    V_7
    IL_520e:  box        [mscorlib]System.Int64
    IL_5213:  ldloc.s    V_17
    IL_5215:  box        [mscorlib]System.Int64
    IL_521a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_521f:  ldloc.s    V_8
    IL_5221:  ldloc.s    V_18
    IL_5223:  beq.s      IL_5240

    IL_5225:  ldc.i4.1
    IL_5226:  stloc.s    V_20
    IL_5228:  ldstr      "dummy check failed {0} {1}"
    IL_522d:  ldloc.s    V_8
    IL_522f:  box        [mscorlib]System.Int32
    IL_5234:  ldloc.s    V_18
    IL_5236:  box        [mscorlib]System.Int32
    IL_523b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5240:  ldloc.s    V_9
    IL_5242:  ldloc.s    V_19
    IL_5244:  beq.s      IL_5261

    IL_5246:  ldc.i4.1
    IL_5247:  stloc.s    V_20
    IL_5249:  ldstr      "dummy64 check failed {0} {1}"
    IL_524e:  ldloc.s    V_9
    IL_5250:  box        [mscorlib]System.Int64
    IL_5255:  ldloc.s    V_19
    IL_5257:  box        [mscorlib]System.Int64
    IL_525c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5261:  ldsfld     int32 IntrinsicTest::counter
    IL_5266:  ldsfld     int32 IntrinsicTest::id_counter
    IL_526b:  beq.s      IL_528e

    IL_526d:  ldstr      "counter mismatch {0} {1}"
    IL_5272:  ldsfld     int32 IntrinsicTest::counter
    IL_5277:  box        [mscorlib]System.Int32
    IL_527c:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5281:  box        [mscorlib]System.Int32
    IL_5286:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_528b:  ldc.i4.1
    IL_528c:  stloc.s    V_20
    IL_528e:  ldsfld     int64 IntrinsicTest::counter64
    IL_5293:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5298:  beq.s      IL_52bb

    IL_529a:  ldstr      "counter64 mismatch {0} {1}"
    IL_529f:  ldsfld     int64 IntrinsicTest::counter64
    IL_52a4:  box        [mscorlib]System.Int64
    IL_52a9:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_52ae:  box        [mscorlib]System.Int64
    IL_52b3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_52b8:  ldc.i4.1
    IL_52b9:  stloc.s    V_20
    IL_52bb:  ldloc.s    V_21
    IL_52bd:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_52c2:  ldloc.s    V_21
    IL_52c4:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_52c9:  beq.s      IL_52f0

    IL_52cb:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_52d0:  ldloc.s    V_21
    IL_52d2:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_52d7:  box        [mscorlib]System.Int32
    IL_52dc:  ldloc.s    V_21
    IL_52de:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_52e3:  box        [mscorlib]System.Int32
    IL_52e8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_52ed:  ldc.i4.1
    IL_52ee:  stloc.s    V_20
    IL_52f0:  ldloc.s    V_21
    IL_52f2:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_52f7:  ldloc.s    V_21
    IL_52f9:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_52fe:  beq.s      IL_5325

    IL_5300:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_5305:  ldloc.s    V_21
    IL_5307:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_530c:  box        [mscorlib]System.Int64
    IL_5311:  ldloc.s    V_21
    IL_5313:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_5318:  box        [mscorlib]System.Int64
    IL_531d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5322:  ldc.i4.1
    IL_5323:  stloc.s    V_20
    IL_5325:  ldsfld     int64 IntrinsicTest::temp
    IL_532a:  ldsfld     int64 IntrinsicTest::idmp
    IL_532f:  beq.s      IL_5352

    IL_5331:  ldstr      "temp mismatch {0} {1}"
    IL_5336:  ldsfld     int64 IntrinsicTest::temp
    IL_533b:  box        [mscorlib]System.Int64
    IL_5340:  ldsfld     int64 IntrinsicTest::idmp
    IL_5345:  box        [mscorlib]System.Int64
    IL_534a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_534f:  ldc.i4.1
    IL_5350:  stloc.s    V_20
    IL_5352:  ldloc.s    V_21
    IL_5354:  ldsflda    int32 IntrinsicTest::counter
    IL_5359:  ldsfld     int32 IntrinsicTest::counter
    IL_535e:  ldc.i4     0x618
    IL_5363:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_5368:  stfld      int32 IntrinsicTest::instanceCounter
    IL_536d:  ldloc.s    V_21
    IL_536f:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5374:  stfld      int32 IntrinsicTest::id_instanceCounter
    IL_5379:  ldsfld     int32 IntrinsicTest::id_counter
    IL_537e:  ldc.i4     0x618
    IL_5383:  bne.un.s   IL_5391

    IL_5385:  ldsfld     int32 IntrinsicTest::id_counter
    IL_538a:  stsfld     int32 IntrinsicTest::id_counter
    IL_538f:  br.s       IL_539c

    IL_5391:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5396:  conv.i8
    IL_5397:  stsfld     int64 IntrinsicTest::idjunk
    IL_539c:  ldstr      "------------------------------------CMPXCHG  1 1 4 5"
    IL_53a1:  call       void [System.Console]System.Console::WriteLine(string)
    IL_53a6:  ldloc.0
    IL_53a7:  ldloc.s    V_10
    IL_53a9:  beq.s      IL_53c5

    IL_53ab:  ldc.i4.1
    IL_53ac:  stloc.s    V_20
    IL_53ae:  ldstr      "te0 check failed {0} {1}"
    IL_53b3:  ldloc.0
    IL_53b4:  box        [mscorlib]System.Int32
    IL_53b9:  ldloc.s    V_10
    IL_53bb:  box        [mscorlib]System.Int32
    IL_53c0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_53c5:  ldloc.1
    IL_53c6:  ldloc.s    V_11
    IL_53c8:  beq.s      IL_53e4

    IL_53ca:  ldc.i4.1
    IL_53cb:  stloc.s    V_20
    IL_53cd:  ldstr      "te1 check failed {0} {1}"
    IL_53d2:  ldloc.1
    IL_53d3:  box        [mscorlib]System.Int32
    IL_53d8:  ldloc.s    V_11
    IL_53da:  box        [mscorlib]System.Int32
    IL_53df:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_53e4:  ldloc.2
    IL_53e5:  ldloc.s    V_12
    IL_53e7:  beq.s      IL_5403

    IL_53e9:  ldc.i4.1
    IL_53ea:  stloc.s    V_20
    IL_53ec:  ldstr      "te2 check failed {0} {1}"
    IL_53f1:  ldloc.2
    IL_53f2:  box        [mscorlib]System.Int32
    IL_53f7:  ldloc.s    V_12
    IL_53f9:  box        [mscorlib]System.Int32
    IL_53fe:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5403:  ldloc.3
    IL_5404:  ldloc.s    V_13
    IL_5406:  beq.s      IL_5422

    IL_5408:  ldc.i4.1
    IL_5409:  stloc.s    V_20
    IL_540b:  ldstr      "te3 check failed {0} {1}"
    IL_5410:  ldloc.3
    IL_5411:  box        [mscorlib]System.Int32
    IL_5416:  ldloc.s    V_13
    IL_5418:  box        [mscorlib]System.Int32
    IL_541d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5422:  ldloc.s    V_4
    IL_5424:  ldloc.s    V_14
    IL_5426:  beq.s      IL_5443

    IL_5428:  ldc.i4.1
    IL_5429:  stloc.s    V_20
    IL_542b:  ldstr      "te064 check failed {0} {1}"
    IL_5430:  ldloc.s    V_4
    IL_5432:  box        [mscorlib]System.Int64
    IL_5437:  ldloc.s    V_14
    IL_5439:  box        [mscorlib]System.Int64
    IL_543e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5443:  ldloc.s    V_5
    IL_5445:  ldloc.s    V_15
    IL_5447:  beq.s      IL_5464

    IL_5449:  ldc.i4.1
    IL_544a:  stloc.s    V_20
    IL_544c:  ldstr      "te164 check failed {0} {1}"
    IL_5451:  ldloc.s    V_5
    IL_5453:  box        [mscorlib]System.Int64
    IL_5458:  ldloc.s    V_15
    IL_545a:  box        [mscorlib]System.Int64
    IL_545f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5464:  ldloc.s    V_6
    IL_5466:  ldloc.s    V_16
    IL_5468:  beq.s      IL_5485

    IL_546a:  ldc.i4.1
    IL_546b:  stloc.s    V_20
    IL_546d:  ldstr      "te264 check failed {0} {1}"
    IL_5472:  ldloc.s    V_6
    IL_5474:  box        [mscorlib]System.Int64
    IL_5479:  ldloc.s    V_16
    IL_547b:  box        [mscorlib]System.Int64
    IL_5480:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5485:  ldloc.s    V_7
    IL_5487:  ldloc.s    V_17
    IL_5489:  beq.s      IL_54a6

    IL_548b:  ldc.i4.1
    IL_548c:  stloc.s    V_20
    IL_548e:  ldstr      "te364 check failed {0} {1}"
    IL_5493:  ldloc.s    V_7
    IL_5495:  box        [mscorlib]System.Int64
    IL_549a:  ldloc.s    V_17
    IL_549c:  box        [mscorlib]System.Int64
    IL_54a1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_54a6:  ldloc.s    V_8
    IL_54a8:  ldloc.s    V_18
    IL_54aa:  beq.s      IL_54c7

    IL_54ac:  ldc.i4.1
    IL_54ad:  stloc.s    V_20
    IL_54af:  ldstr      "dummy check failed {0} {1}"
    IL_54b4:  ldloc.s    V_8
    IL_54b6:  box        [mscorlib]System.Int32
    IL_54bb:  ldloc.s    V_18
    IL_54bd:  box        [mscorlib]System.Int32
    IL_54c2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_54c7:  ldloc.s    V_9
    IL_54c9:  ldloc.s    V_19
    IL_54cb:  beq.s      IL_54e8

    IL_54cd:  ldc.i4.1
    IL_54ce:  stloc.s    V_20
    IL_54d0:  ldstr      "dummy64 check failed {0} {1}"
    IL_54d5:  ldloc.s    V_9
    IL_54d7:  box        [mscorlib]System.Int64
    IL_54dc:  ldloc.s    V_19
    IL_54de:  box        [mscorlib]System.Int64
    IL_54e3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_54e8:  ldsfld     int32 IntrinsicTest::counter
    IL_54ed:  ldsfld     int32 IntrinsicTest::id_counter
    IL_54f2:  beq.s      IL_5515

    IL_54f4:  ldstr      "counter mismatch {0} {1}"
    IL_54f9:  ldsfld     int32 IntrinsicTest::counter
    IL_54fe:  box        [mscorlib]System.Int32
    IL_5503:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5508:  box        [mscorlib]System.Int32
    IL_550d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5512:  ldc.i4.1
    IL_5513:  stloc.s    V_20
    IL_5515:  ldsfld     int64 IntrinsicTest::counter64
    IL_551a:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_551f:  beq.s      IL_5542

    IL_5521:  ldstr      "counter64 mismatch {0} {1}"
    IL_5526:  ldsfld     int64 IntrinsicTest::counter64
    IL_552b:  box        [mscorlib]System.Int64
    IL_5530:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5535:  box        [mscorlib]System.Int64
    IL_553a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_553f:  ldc.i4.1
    IL_5540:  stloc.s    V_20
    IL_5542:  ldloc.s    V_21
    IL_5544:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_5549:  ldloc.s    V_21
    IL_554b:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_5550:  beq.s      IL_5577

    IL_5552:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_5557:  ldloc.s    V_21
    IL_5559:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_555e:  box        [mscorlib]System.Int32
    IL_5563:  ldloc.s    V_21
    IL_5565:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_556a:  box        [mscorlib]System.Int32
    IL_556f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5574:  ldc.i4.1
    IL_5575:  stloc.s    V_20
    IL_5577:  ldloc.s    V_21
    IL_5579:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_557e:  ldloc.s    V_21
    IL_5580:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_5585:  beq.s      IL_55ac

    IL_5587:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_558c:  ldloc.s    V_21
    IL_558e:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_5593:  box        [mscorlib]System.Int64
    IL_5598:  ldloc.s    V_21
    IL_559a:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_559f:  box        [mscorlib]System.Int64
    IL_55a4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_55a9:  ldc.i4.1
    IL_55aa:  stloc.s    V_20
    IL_55ac:  ldsfld     int64 IntrinsicTest::temp
    IL_55b1:  ldsfld     int64 IntrinsicTest::idmp
    IL_55b6:  beq.s      IL_55d9

    IL_55b8:  ldstr      "temp mismatch {0} {1}"
    IL_55bd:  ldsfld     int64 IntrinsicTest::temp
    IL_55c2:  box        [mscorlib]System.Int64
    IL_55c7:  ldsfld     int64 IntrinsicTest::idmp
    IL_55cc:  box        [mscorlib]System.Int64
    IL_55d1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_55d6:  ldc.i4.1
    IL_55d7:  stloc.s    V_20
    IL_55d9:  ldsflda    int32 IntrinsicTest::counter
    IL_55de:  ldsfld     int32 IntrinsicTest::counter
    IL_55e3:  ldc.i4     0x618
    IL_55e8:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_55ed:  stloc.3
    IL_55ee:  ldsfld     int32 IntrinsicTest::id_counter
    IL_55f3:  stloc.s    V_13
    IL_55f5:  ldsfld     int32 IntrinsicTest::id_counter
    IL_55fa:  ldc.i4     0x618
    IL_55ff:  bne.un.s   IL_560d

    IL_5601:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5606:  stsfld     int32 IntrinsicTest::id_counter
    IL_560b:  br.s       IL_5618

    IL_560d:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5612:  conv.i8
    IL_5613:  stsfld     int64 IntrinsicTest::idjunk
    IL_5618:  ldstr      "------------------------------------CMPXCHG  1 1 4 6"
    IL_561d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_5622:  ldloc.0
    IL_5623:  ldloc.s    V_10
    IL_5625:  beq.s      IL_5641

    IL_5627:  ldc.i4.1
    IL_5628:  stloc.s    V_20
    IL_562a:  ldstr      "te0 check failed {0} {1}"
    IL_562f:  ldloc.0
    IL_5630:  box        [mscorlib]System.Int32
    IL_5635:  ldloc.s    V_10
    IL_5637:  box        [mscorlib]System.Int32
    IL_563c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5641:  ldloc.1
    IL_5642:  ldloc.s    V_11
    IL_5644:  beq.s      IL_5660

    IL_5646:  ldc.i4.1
    IL_5647:  stloc.s    V_20
    IL_5649:  ldstr      "te1 check failed {0} {1}"
    IL_564e:  ldloc.1
    IL_564f:  box        [mscorlib]System.Int32
    IL_5654:  ldloc.s    V_11
    IL_5656:  box        [mscorlib]System.Int32
    IL_565b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5660:  ldloc.2
    IL_5661:  ldloc.s    V_12
    IL_5663:  beq.s      IL_567f

    IL_5665:  ldc.i4.1
    IL_5666:  stloc.s    V_20
    IL_5668:  ldstr      "te2 check failed {0} {1}"
    IL_566d:  ldloc.2
    IL_566e:  box        [mscorlib]System.Int32
    IL_5673:  ldloc.s    V_12
    IL_5675:  box        [mscorlib]System.Int32
    IL_567a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_567f:  ldloc.3
    IL_5680:  ldloc.s    V_13
    IL_5682:  beq.s      IL_569e

    IL_5684:  ldc.i4.1
    IL_5685:  stloc.s    V_20
    IL_5687:  ldstr      "te3 check failed {0} {1}"
    IL_568c:  ldloc.3
    IL_568d:  box        [mscorlib]System.Int32
    IL_5692:  ldloc.s    V_13
    IL_5694:  box        [mscorlib]System.Int32
    IL_5699:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_569e:  ldloc.s    V_4
    IL_56a0:  ldloc.s    V_14
    IL_56a2:  beq.s      IL_56bf

    IL_56a4:  ldc.i4.1
    IL_56a5:  stloc.s    V_20
    IL_56a7:  ldstr      "te064 check failed {0} {1}"
    IL_56ac:  ldloc.s    V_4
    IL_56ae:  box        [mscorlib]System.Int64
    IL_56b3:  ldloc.s    V_14
    IL_56b5:  box        [mscorlib]System.Int64
    IL_56ba:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_56bf:  ldloc.s    V_5
    IL_56c1:  ldloc.s    V_15
    IL_56c3:  beq.s      IL_56e0

    IL_56c5:  ldc.i4.1
    IL_56c6:  stloc.s    V_20
    IL_56c8:  ldstr      "te164 check failed {0} {1}"
    IL_56cd:  ldloc.s    V_5
    IL_56cf:  box        [mscorlib]System.Int64
    IL_56d4:  ldloc.s    V_15
    IL_56d6:  box        [mscorlib]System.Int64
    IL_56db:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_56e0:  ldloc.s    V_6
    IL_56e2:  ldloc.s    V_16
    IL_56e4:  beq.s      IL_5701

    IL_56e6:  ldc.i4.1
    IL_56e7:  stloc.s    V_20
    IL_56e9:  ldstr      "te264 check failed {0} {1}"
    IL_56ee:  ldloc.s    V_6
    IL_56f0:  box        [mscorlib]System.Int64
    IL_56f5:  ldloc.s    V_16
    IL_56f7:  box        [mscorlib]System.Int64
    IL_56fc:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5701:  ldloc.s    V_7
    IL_5703:  ldloc.s    V_17
    IL_5705:  beq.s      IL_5722

    IL_5707:  ldc.i4.1
    IL_5708:  stloc.s    V_20
    IL_570a:  ldstr      "te364 check failed {0} {1}"
    IL_570f:  ldloc.s    V_7
    IL_5711:  box        [mscorlib]System.Int64
    IL_5716:  ldloc.s    V_17
    IL_5718:  box        [mscorlib]System.Int64
    IL_571d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5722:  ldloc.s    V_8
    IL_5724:  ldloc.s    V_18
    IL_5726:  beq.s      IL_5743

    IL_5728:  ldc.i4.1
    IL_5729:  stloc.s    V_20
    IL_572b:  ldstr      "dummy check failed {0} {1}"
    IL_5730:  ldloc.s    V_8
    IL_5732:  box        [mscorlib]System.Int32
    IL_5737:  ldloc.s    V_18
    IL_5739:  box        [mscorlib]System.Int32
    IL_573e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5743:  ldloc.s    V_9
    IL_5745:  ldloc.s    V_19
    IL_5747:  beq.s      IL_5764

    IL_5749:  ldc.i4.1
    IL_574a:  stloc.s    V_20
    IL_574c:  ldstr      "dummy64 check failed {0} {1}"
    IL_5751:  ldloc.s    V_9
    IL_5753:  box        [mscorlib]System.Int64
    IL_5758:  ldloc.s    V_19
    IL_575a:  box        [mscorlib]System.Int64
    IL_575f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5764:  ldsfld     int32 IntrinsicTest::counter
    IL_5769:  ldsfld     int32 IntrinsicTest::id_counter
    IL_576e:  beq.s      IL_5791

    IL_5770:  ldstr      "counter mismatch {0} {1}"
    IL_5775:  ldsfld     int32 IntrinsicTest::counter
    IL_577a:  box        [mscorlib]System.Int32
    IL_577f:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5784:  box        [mscorlib]System.Int32
    IL_5789:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_578e:  ldc.i4.1
    IL_578f:  stloc.s    V_20
    IL_5791:  ldsfld     int64 IntrinsicTest::counter64
    IL_5796:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_579b:  beq.s      IL_57be

    IL_579d:  ldstr      "counter64 mismatch {0} {1}"
    IL_57a2:  ldsfld     int64 IntrinsicTest::counter64
    IL_57a7:  box        [mscorlib]System.Int64
    IL_57ac:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_57b1:  box        [mscorlib]System.Int64
    IL_57b6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_57bb:  ldc.i4.1
    IL_57bc:  stloc.s    V_20
    IL_57be:  ldloc.s    V_21
    IL_57c0:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_57c5:  ldloc.s    V_21
    IL_57c7:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_57cc:  beq.s      IL_57f3

    IL_57ce:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_57d3:  ldloc.s    V_21
    IL_57d5:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_57da:  box        [mscorlib]System.Int32
    IL_57df:  ldloc.s    V_21
    IL_57e1:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_57e6:  box        [mscorlib]System.Int32
    IL_57eb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_57f0:  ldc.i4.1
    IL_57f1:  stloc.s    V_20
    IL_57f3:  ldloc.s    V_21
    IL_57f5:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_57fa:  ldloc.s    V_21
    IL_57fc:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_5801:  beq.s      IL_5828

    IL_5803:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_5808:  ldloc.s    V_21
    IL_580a:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_580f:  box        [mscorlib]System.Int64
    IL_5814:  ldloc.s    V_21
    IL_5816:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_581b:  box        [mscorlib]System.Int64
    IL_5820:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5825:  ldc.i4.1
    IL_5826:  stloc.s    V_20
    IL_5828:  ldsfld     int64 IntrinsicTest::temp
    IL_582d:  ldsfld     int64 IntrinsicTest::idmp
    IL_5832:  beq.s      IL_5855

    IL_5834:  ldstr      "temp mismatch {0} {1}"
    IL_5839:  ldsfld     int64 IntrinsicTest::temp
    IL_583e:  box        [mscorlib]System.Int64
    IL_5843:  ldsfld     int64 IntrinsicTest::idmp
    IL_5848:  box        [mscorlib]System.Int64
    IL_584d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5852:  ldc.i4.1
    IL_5853:  stloc.s    V_20
    IL_5855:  ldsflda    int32 IntrinsicTest::counter
    IL_585a:  ldsfld     int32 IntrinsicTest::counter
    IL_585f:  ldloc.1
    IL_5860:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_5865:  stloc.0
    IL_5866:  ldsfld     int32 IntrinsicTest::id_counter
    IL_586b:  stloc.s    V_10
    IL_586d:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5872:  ldloc.s    V_11
    IL_5874:  bne.un.s   IL_5882

    IL_5876:  ldsfld     int32 IntrinsicTest::id_counter
    IL_587b:  stsfld     int32 IntrinsicTest::id_counter
    IL_5880:  br.s       IL_588d

    IL_5882:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5887:  conv.i8
    IL_5888:  stsfld     int64 IntrinsicTest::idjunk
    IL_588d:  ldstr      "------------------------------------CMPXCHG  1 1 5 0"
    IL_5892:  call       void [System.Console]System.Console::WriteLine(string)
    IL_5897:  ldloc.0
    IL_5898:  ldloc.s    V_10
    IL_589a:  beq.s      IL_58b6

    IL_589c:  ldc.i4.1
    IL_589d:  stloc.s    V_20
    IL_589f:  ldstr      "te0 check failed {0} {1}"
    IL_58a4:  ldloc.0
    IL_58a5:  box        [mscorlib]System.Int32
    IL_58aa:  ldloc.s    V_10
    IL_58ac:  box        [mscorlib]System.Int32
    IL_58b1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_58b6:  ldloc.1
    IL_58b7:  ldloc.s    V_11
    IL_58b9:  beq.s      IL_58d5

    IL_58bb:  ldc.i4.1
    IL_58bc:  stloc.s    V_20
    IL_58be:  ldstr      "te1 check failed {0} {1}"
    IL_58c3:  ldloc.1
    IL_58c4:  box        [mscorlib]System.Int32
    IL_58c9:  ldloc.s    V_11
    IL_58cb:  box        [mscorlib]System.Int32
    IL_58d0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_58d5:  ldloc.2
    IL_58d6:  ldloc.s    V_12
    IL_58d8:  beq.s      IL_58f4

    IL_58da:  ldc.i4.1
    IL_58db:  stloc.s    V_20
    IL_58dd:  ldstr      "te2 check failed {0} {1}"
    IL_58e2:  ldloc.2
    IL_58e3:  box        [mscorlib]System.Int32
    IL_58e8:  ldloc.s    V_12
    IL_58ea:  box        [mscorlib]System.Int32
    IL_58ef:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_58f4:  ldloc.3
    IL_58f5:  ldloc.s    V_13
    IL_58f7:  beq.s      IL_5913

    IL_58f9:  ldc.i4.1
    IL_58fa:  stloc.s    V_20
    IL_58fc:  ldstr      "te3 check failed {0} {1}"
    IL_5901:  ldloc.3
    IL_5902:  box        [mscorlib]System.Int32
    IL_5907:  ldloc.s    V_13
    IL_5909:  box        [mscorlib]System.Int32
    IL_590e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5913:  ldloc.s    V_4
    IL_5915:  ldloc.s    V_14
    IL_5917:  beq.s      IL_5934

    IL_5919:  ldc.i4.1
    IL_591a:  stloc.s    V_20
    IL_591c:  ldstr      "te064 check failed {0} {1}"
    IL_5921:  ldloc.s    V_4
    IL_5923:  box        [mscorlib]System.Int64
    IL_5928:  ldloc.s    V_14
    IL_592a:  box        [mscorlib]System.Int64
    IL_592f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5934:  ldloc.s    V_5
    IL_5936:  ldloc.s    V_15
    IL_5938:  beq.s      IL_5955

    IL_593a:  ldc.i4.1
    IL_593b:  stloc.s    V_20
    IL_593d:  ldstr      "te164 check failed {0} {1}"
    IL_5942:  ldloc.s    V_5
    IL_5944:  box        [mscorlib]System.Int64
    IL_5949:  ldloc.s    V_15
    IL_594b:  box        [mscorlib]System.Int64
    IL_5950:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5955:  ldloc.s    V_6
    IL_5957:  ldloc.s    V_16
    IL_5959:  beq.s      IL_5976

    IL_595b:  ldc.i4.1
    IL_595c:  stloc.s    V_20
    IL_595e:  ldstr      "te264 check failed {0} {1}"
    IL_5963:  ldloc.s    V_6
    IL_5965:  box        [mscorlib]System.Int64
    IL_596a:  ldloc.s    V_16
    IL_596c:  box        [mscorlib]System.Int64
    IL_5971:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5976:  ldloc.s    V_7
    IL_5978:  ldloc.s    V_17
    IL_597a:  beq.s      IL_5997

    IL_597c:  ldc.i4.1
    IL_597d:  stloc.s    V_20
    IL_597f:  ldstr      "te364 check failed {0} {1}"
    IL_5984:  ldloc.s    V_7
    IL_5986:  box        [mscorlib]System.Int64
    IL_598b:  ldloc.s    V_17
    IL_598d:  box        [mscorlib]System.Int64
    IL_5992:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5997:  ldloc.s    V_8
    IL_5999:  ldloc.s    V_18
    IL_599b:  beq.s      IL_59b8

    IL_599d:  ldc.i4.1
    IL_599e:  stloc.s    V_20
    IL_59a0:  ldstr      "dummy check failed {0} {1}"
    IL_59a5:  ldloc.s    V_8
    IL_59a7:  box        [mscorlib]System.Int32
    IL_59ac:  ldloc.s    V_18
    IL_59ae:  box        [mscorlib]System.Int32
    IL_59b3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_59b8:  ldloc.s    V_9
    IL_59ba:  ldloc.s    V_19
    IL_59bc:  beq.s      IL_59d9

    IL_59be:  ldc.i4.1
    IL_59bf:  stloc.s    V_20
    IL_59c1:  ldstr      "dummy64 check failed {0} {1}"
    IL_59c6:  ldloc.s    V_9
    IL_59c8:  box        [mscorlib]System.Int64
    IL_59cd:  ldloc.s    V_19
    IL_59cf:  box        [mscorlib]System.Int64
    IL_59d4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_59d9:  ldsfld     int32 IntrinsicTest::counter
    IL_59de:  ldsfld     int32 IntrinsicTest::id_counter
    IL_59e3:  beq.s      IL_5a06

    IL_59e5:  ldstr      "counter mismatch {0} {1}"
    IL_59ea:  ldsfld     int32 IntrinsicTest::counter
    IL_59ef:  box        [mscorlib]System.Int32
    IL_59f4:  ldsfld     int32 IntrinsicTest::id_counter
    IL_59f9:  box        [mscorlib]System.Int32
    IL_59fe:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5a03:  ldc.i4.1
    IL_5a04:  stloc.s    V_20
    IL_5a06:  ldsfld     int64 IntrinsicTest::counter64
    IL_5a0b:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5a10:  beq.s      IL_5a33

    IL_5a12:  ldstr      "counter64 mismatch {0} {1}"
    IL_5a17:  ldsfld     int64 IntrinsicTest::counter64
    IL_5a1c:  box        [mscorlib]System.Int64
    IL_5a21:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5a26:  box        [mscorlib]System.Int64
    IL_5a2b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5a30:  ldc.i4.1
    IL_5a31:  stloc.s    V_20
    IL_5a33:  ldloc.s    V_21
    IL_5a35:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_5a3a:  ldloc.s    V_21
    IL_5a3c:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_5a41:  beq.s      IL_5a68

    IL_5a43:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_5a48:  ldloc.s    V_21
    IL_5a4a:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_5a4f:  box        [mscorlib]System.Int32
    IL_5a54:  ldloc.s    V_21
    IL_5a56:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_5a5b:  box        [mscorlib]System.Int32
    IL_5a60:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5a65:  ldc.i4.1
    IL_5a66:  stloc.s    V_20
    IL_5a68:  ldloc.s    V_21
    IL_5a6a:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_5a6f:  ldloc.s    V_21
    IL_5a71:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_5a76:  beq.s      IL_5a9d

    IL_5a78:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_5a7d:  ldloc.s    V_21
    IL_5a7f:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_5a84:  box        [mscorlib]System.Int64
    IL_5a89:  ldloc.s    V_21
    IL_5a8b:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_5a90:  box        [mscorlib]System.Int64
    IL_5a95:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5a9a:  ldc.i4.1
    IL_5a9b:  stloc.s    V_20
    IL_5a9d:  ldsfld     int64 IntrinsicTest::temp
    IL_5aa2:  ldsfld     int64 IntrinsicTest::idmp
    IL_5aa7:  beq.s      IL_5aca

    IL_5aa9:  ldstr      "temp mismatch {0} {1}"
    IL_5aae:  ldsfld     int64 IntrinsicTest::temp
    IL_5ab3:  box        [mscorlib]System.Int64
    IL_5ab8:  ldsfld     int64 IntrinsicTest::idmp
    IL_5abd:  box        [mscorlib]System.Int64
    IL_5ac2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5ac7:  ldc.i4.1
    IL_5ac8:  stloc.s    V_20
    IL_5aca:  ldsflda    int32 IntrinsicTest::counter
    IL_5acf:  ldsfld     int32 IntrinsicTest::counter
    IL_5ad4:  ldloc.1
    IL_5ad5:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_5ada:  stsfld     int32 IntrinsicTest::counter
    IL_5adf:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5ae4:  stsfld     int32 IntrinsicTest::id_counter
    IL_5ae9:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5aee:  ldloc.s    V_11
    IL_5af0:  bne.un.s   IL_5aff

    IL_5af2:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5af7:  conv.i8
    IL_5af8:  stsfld     int64 IntrinsicTest::idjunk
    IL_5afd:  br.s       IL_5b0a

    IL_5aff:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5b04:  conv.i8
    IL_5b05:  stsfld     int64 IntrinsicTest::idjunk
    IL_5b0a:  ldstr      "------------------------------------CMPXCHG  1 1 5 1"
    IL_5b0f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_5b14:  ldloc.0
    IL_5b15:  ldloc.s    V_10
    IL_5b17:  beq.s      IL_5b33

    IL_5b19:  ldc.i4.1
    IL_5b1a:  stloc.s    V_20
    IL_5b1c:  ldstr      "te0 check failed {0} {1}"
    IL_5b21:  ldloc.0
    IL_5b22:  box        [mscorlib]System.Int32
    IL_5b27:  ldloc.s    V_10
    IL_5b29:  box        [mscorlib]System.Int32
    IL_5b2e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5b33:  ldloc.1
    IL_5b34:  ldloc.s    V_11
    IL_5b36:  beq.s      IL_5b52

    IL_5b38:  ldc.i4.1
    IL_5b39:  stloc.s    V_20
    IL_5b3b:  ldstr      "te1 check failed {0} {1}"
    IL_5b40:  ldloc.1
    IL_5b41:  box        [mscorlib]System.Int32
    IL_5b46:  ldloc.s    V_11
    IL_5b48:  box        [mscorlib]System.Int32
    IL_5b4d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5b52:  ldloc.2
    IL_5b53:  ldloc.s    V_12
    IL_5b55:  beq.s      IL_5b71

    IL_5b57:  ldc.i4.1
    IL_5b58:  stloc.s    V_20
    IL_5b5a:  ldstr      "te2 check failed {0} {1}"
    IL_5b5f:  ldloc.2
    IL_5b60:  box        [mscorlib]System.Int32
    IL_5b65:  ldloc.s    V_12
    IL_5b67:  box        [mscorlib]System.Int32
    IL_5b6c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5b71:  ldloc.3
    IL_5b72:  ldloc.s    V_13
    IL_5b74:  beq.s      IL_5b90

    IL_5b76:  ldc.i4.1
    IL_5b77:  stloc.s    V_20
    IL_5b79:  ldstr      "te3 check failed {0} {1}"
    IL_5b7e:  ldloc.3
    IL_5b7f:  box        [mscorlib]System.Int32
    IL_5b84:  ldloc.s    V_13
    IL_5b86:  box        [mscorlib]System.Int32
    IL_5b8b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5b90:  ldloc.s    V_4
    IL_5b92:  ldloc.s    V_14
    IL_5b94:  beq.s      IL_5bb1

    IL_5b96:  ldc.i4.1
    IL_5b97:  stloc.s    V_20
    IL_5b99:  ldstr      "te064 check failed {0} {1}"
    IL_5b9e:  ldloc.s    V_4
    IL_5ba0:  box        [mscorlib]System.Int64
    IL_5ba5:  ldloc.s    V_14
    IL_5ba7:  box        [mscorlib]System.Int64
    IL_5bac:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5bb1:  ldloc.s    V_5
    IL_5bb3:  ldloc.s    V_15
    IL_5bb5:  beq.s      IL_5bd2

    IL_5bb7:  ldc.i4.1
    IL_5bb8:  stloc.s    V_20
    IL_5bba:  ldstr      "te164 check failed {0} {1}"
    IL_5bbf:  ldloc.s    V_5
    IL_5bc1:  box        [mscorlib]System.Int64
    IL_5bc6:  ldloc.s    V_15
    IL_5bc8:  box        [mscorlib]System.Int64
    IL_5bcd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5bd2:  ldloc.s    V_6
    IL_5bd4:  ldloc.s    V_16
    IL_5bd6:  beq.s      IL_5bf3

    IL_5bd8:  ldc.i4.1
    IL_5bd9:  stloc.s    V_20
    IL_5bdb:  ldstr      "te264 check failed {0} {1}"
    IL_5be0:  ldloc.s    V_6
    IL_5be2:  box        [mscorlib]System.Int64
    IL_5be7:  ldloc.s    V_16
    IL_5be9:  box        [mscorlib]System.Int64
    IL_5bee:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5bf3:  ldloc.s    V_7
    IL_5bf5:  ldloc.s    V_17
    IL_5bf7:  beq.s      IL_5c14

    IL_5bf9:  ldc.i4.1
    IL_5bfa:  stloc.s    V_20
    IL_5bfc:  ldstr      "te364 check failed {0} {1}"
    IL_5c01:  ldloc.s    V_7
    IL_5c03:  box        [mscorlib]System.Int64
    IL_5c08:  ldloc.s    V_17
    IL_5c0a:  box        [mscorlib]System.Int64
    IL_5c0f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5c14:  ldloc.s    V_8
    IL_5c16:  ldloc.s    V_18
    IL_5c18:  beq.s      IL_5c35

    IL_5c1a:  ldc.i4.1
    IL_5c1b:  stloc.s    V_20
    IL_5c1d:  ldstr      "dummy check failed {0} {1}"
    IL_5c22:  ldloc.s    V_8
    IL_5c24:  box        [mscorlib]System.Int32
    IL_5c29:  ldloc.s    V_18
    IL_5c2b:  box        [mscorlib]System.Int32
    IL_5c30:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5c35:  ldloc.s    V_9
    IL_5c37:  ldloc.s    V_19
    IL_5c39:  beq.s      IL_5c56

    IL_5c3b:  ldc.i4.1
    IL_5c3c:  stloc.s    V_20
    IL_5c3e:  ldstr      "dummy64 check failed {0} {1}"
    IL_5c43:  ldloc.s    V_9
    IL_5c45:  box        [mscorlib]System.Int64
    IL_5c4a:  ldloc.s    V_19
    IL_5c4c:  box        [mscorlib]System.Int64
    IL_5c51:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5c56:  ldsfld     int32 IntrinsicTest::counter
    IL_5c5b:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5c60:  beq.s      IL_5c83

    IL_5c62:  ldstr      "counter mismatch {0} {1}"
    IL_5c67:  ldsfld     int32 IntrinsicTest::counter
    IL_5c6c:  box        [mscorlib]System.Int32
    IL_5c71:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5c76:  box        [mscorlib]System.Int32
    IL_5c7b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5c80:  ldc.i4.1
    IL_5c81:  stloc.s    V_20
    IL_5c83:  ldsfld     int64 IntrinsicTest::counter64
    IL_5c88:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5c8d:  beq.s      IL_5cb0

    IL_5c8f:  ldstr      "counter64 mismatch {0} {1}"
    IL_5c94:  ldsfld     int64 IntrinsicTest::counter64
    IL_5c99:  box        [mscorlib]System.Int64
    IL_5c9e:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5ca3:  box        [mscorlib]System.Int64
    IL_5ca8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5cad:  ldc.i4.1
    IL_5cae:  stloc.s    V_20
    IL_5cb0:  ldloc.s    V_21
    IL_5cb2:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_5cb7:  ldloc.s    V_21
    IL_5cb9:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_5cbe:  beq.s      IL_5ce5

    IL_5cc0:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_5cc5:  ldloc.s    V_21
    IL_5cc7:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_5ccc:  box        [mscorlib]System.Int32
    IL_5cd1:  ldloc.s    V_21
    IL_5cd3:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_5cd8:  box        [mscorlib]System.Int32
    IL_5cdd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5ce2:  ldc.i4.1
    IL_5ce3:  stloc.s    V_20
    IL_5ce5:  ldloc.s    V_21
    IL_5ce7:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_5cec:  ldloc.s    V_21
    IL_5cee:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_5cf3:  beq.s      IL_5d1a

    IL_5cf5:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_5cfa:  ldloc.s    V_21
    IL_5cfc:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_5d01:  box        [mscorlib]System.Int64
    IL_5d06:  ldloc.s    V_21
    IL_5d08:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_5d0d:  box        [mscorlib]System.Int64
    IL_5d12:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5d17:  ldc.i4.1
    IL_5d18:  stloc.s    V_20
    IL_5d1a:  ldsfld     int64 IntrinsicTest::temp
    IL_5d1f:  ldsfld     int64 IntrinsicTest::idmp
    IL_5d24:  beq.s      IL_5d47

    IL_5d26:  ldstr      "temp mismatch {0} {1}"
    IL_5d2b:  ldsfld     int64 IntrinsicTest::temp
    IL_5d30:  box        [mscorlib]System.Int64
    IL_5d35:  ldsfld     int64 IntrinsicTest::idmp
    IL_5d3a:  box        [mscorlib]System.Int64
    IL_5d3f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5d44:  ldc.i4.1
    IL_5d45:  stloc.s    V_20
    IL_5d47:  ldloc.s    V_21
    IL_5d49:  ldsflda    int32 IntrinsicTest::counter
    IL_5d4e:  ldsfld     int32 IntrinsicTest::counter
    IL_5d53:  ldloc.1
    IL_5d54:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_5d59:  stfld      int32 IntrinsicTest::instanceCounter
    IL_5d5e:  ldloc.s    V_21
    IL_5d60:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5d65:  stfld      int32 IntrinsicTest::id_instanceCounter
    IL_5d6a:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5d6f:  ldloc.s    V_11
    IL_5d71:  bne.un.s   IL_5d7f

    IL_5d73:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5d78:  stsfld     int32 IntrinsicTest::id_counter
    IL_5d7d:  br.s       IL_5d8a

    IL_5d7f:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5d84:  conv.i8
    IL_5d85:  stsfld     int64 IntrinsicTest::idjunk
    IL_5d8a:  ldstr      "------------------------------------CMPXCHG  1 1 5 2"
    IL_5d8f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_5d94:  ldloc.0
    IL_5d95:  ldloc.s    V_10
    IL_5d97:  beq.s      IL_5db3

    IL_5d99:  ldc.i4.1
    IL_5d9a:  stloc.s    V_20
    IL_5d9c:  ldstr      "te0 check failed {0} {1}"
    IL_5da1:  ldloc.0
    IL_5da2:  box        [mscorlib]System.Int32
    IL_5da7:  ldloc.s    V_10
    IL_5da9:  box        [mscorlib]System.Int32
    IL_5dae:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5db3:  ldloc.1
    IL_5db4:  ldloc.s    V_11
    IL_5db6:  beq.s      IL_5dd2

    IL_5db8:  ldc.i4.1
    IL_5db9:  stloc.s    V_20
    IL_5dbb:  ldstr      "te1 check failed {0} {1}"
    IL_5dc0:  ldloc.1
    IL_5dc1:  box        [mscorlib]System.Int32
    IL_5dc6:  ldloc.s    V_11
    IL_5dc8:  box        [mscorlib]System.Int32
    IL_5dcd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5dd2:  ldloc.2
    IL_5dd3:  ldloc.s    V_12
    IL_5dd5:  beq.s      IL_5df1

    IL_5dd7:  ldc.i4.1
    IL_5dd8:  stloc.s    V_20
    IL_5dda:  ldstr      "te2 check failed {0} {1}"
    IL_5ddf:  ldloc.2
    IL_5de0:  box        [mscorlib]System.Int32
    IL_5de5:  ldloc.s    V_12
    IL_5de7:  box        [mscorlib]System.Int32
    IL_5dec:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5df1:  ldloc.3
    IL_5df2:  ldloc.s    V_13
    IL_5df4:  beq.s      IL_5e10

    IL_5df6:  ldc.i4.1
    IL_5df7:  stloc.s    V_20
    IL_5df9:  ldstr      "te3 check failed {0} {1}"
    IL_5dfe:  ldloc.3
    IL_5dff:  box        [mscorlib]System.Int32
    IL_5e04:  ldloc.s    V_13
    IL_5e06:  box        [mscorlib]System.Int32
    IL_5e0b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5e10:  ldloc.s    V_4
    IL_5e12:  ldloc.s    V_14
    IL_5e14:  beq.s      IL_5e31

    IL_5e16:  ldc.i4.1
    IL_5e17:  stloc.s    V_20
    IL_5e19:  ldstr      "te064 check failed {0} {1}"
    IL_5e1e:  ldloc.s    V_4
    IL_5e20:  box        [mscorlib]System.Int64
    IL_5e25:  ldloc.s    V_14
    IL_5e27:  box        [mscorlib]System.Int64
    IL_5e2c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5e31:  ldloc.s    V_5
    IL_5e33:  ldloc.s    V_15
    IL_5e35:  beq.s      IL_5e52

    IL_5e37:  ldc.i4.1
    IL_5e38:  stloc.s    V_20
    IL_5e3a:  ldstr      "te164 check failed {0} {1}"
    IL_5e3f:  ldloc.s    V_5
    IL_5e41:  box        [mscorlib]System.Int64
    IL_5e46:  ldloc.s    V_15
    IL_5e48:  box        [mscorlib]System.Int64
    IL_5e4d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5e52:  ldloc.s    V_6
    IL_5e54:  ldloc.s    V_16
    IL_5e56:  beq.s      IL_5e73

    IL_5e58:  ldc.i4.1
    IL_5e59:  stloc.s    V_20
    IL_5e5b:  ldstr      "te264 check failed {0} {1}"
    IL_5e60:  ldloc.s    V_6
    IL_5e62:  box        [mscorlib]System.Int64
    IL_5e67:  ldloc.s    V_16
    IL_5e69:  box        [mscorlib]System.Int64
    IL_5e6e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5e73:  ldloc.s    V_7
    IL_5e75:  ldloc.s    V_17
    IL_5e77:  beq.s      IL_5e94

    IL_5e79:  ldc.i4.1
    IL_5e7a:  stloc.s    V_20
    IL_5e7c:  ldstr      "te364 check failed {0} {1}"
    IL_5e81:  ldloc.s    V_7
    IL_5e83:  box        [mscorlib]System.Int64
    IL_5e88:  ldloc.s    V_17
    IL_5e8a:  box        [mscorlib]System.Int64
    IL_5e8f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5e94:  ldloc.s    V_8
    IL_5e96:  ldloc.s    V_18
    IL_5e98:  beq.s      IL_5eb5

    IL_5e9a:  ldc.i4.1
    IL_5e9b:  stloc.s    V_20
    IL_5e9d:  ldstr      "dummy check failed {0} {1}"
    IL_5ea2:  ldloc.s    V_8
    IL_5ea4:  box        [mscorlib]System.Int32
    IL_5ea9:  ldloc.s    V_18
    IL_5eab:  box        [mscorlib]System.Int32
    IL_5eb0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5eb5:  ldloc.s    V_9
    IL_5eb7:  ldloc.s    V_19
    IL_5eb9:  beq.s      IL_5ed6

    IL_5ebb:  ldc.i4.1
    IL_5ebc:  stloc.s    V_20
    IL_5ebe:  ldstr      "dummy64 check failed {0} {1}"
    IL_5ec3:  ldloc.s    V_9
    IL_5ec5:  box        [mscorlib]System.Int64
    IL_5eca:  ldloc.s    V_19
    IL_5ecc:  box        [mscorlib]System.Int64
    IL_5ed1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5ed6:  ldsfld     int32 IntrinsicTest::counter
    IL_5edb:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5ee0:  beq.s      IL_5f03

    IL_5ee2:  ldstr      "counter mismatch {0} {1}"
    IL_5ee7:  ldsfld     int32 IntrinsicTest::counter
    IL_5eec:  box        [mscorlib]System.Int32
    IL_5ef1:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5ef6:  box        [mscorlib]System.Int32
    IL_5efb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5f00:  ldc.i4.1
    IL_5f01:  stloc.s    V_20
    IL_5f03:  ldsfld     int64 IntrinsicTest::counter64
    IL_5f08:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5f0d:  beq.s      IL_5f30

    IL_5f0f:  ldstr      "counter64 mismatch {0} {1}"
    IL_5f14:  ldsfld     int64 IntrinsicTest::counter64
    IL_5f19:  box        [mscorlib]System.Int64
    IL_5f1e:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_5f23:  box        [mscorlib]System.Int64
    IL_5f28:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5f2d:  ldc.i4.1
    IL_5f2e:  stloc.s    V_20
    IL_5f30:  ldloc.s    V_21
    IL_5f32:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_5f37:  ldloc.s    V_21
    IL_5f39:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_5f3e:  beq.s      IL_5f65

    IL_5f40:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_5f45:  ldloc.s    V_21
    IL_5f47:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_5f4c:  box        [mscorlib]System.Int32
    IL_5f51:  ldloc.s    V_21
    IL_5f53:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_5f58:  box        [mscorlib]System.Int32
    IL_5f5d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5f62:  ldc.i4.1
    IL_5f63:  stloc.s    V_20
    IL_5f65:  ldloc.s    V_21
    IL_5f67:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_5f6c:  ldloc.s    V_21
    IL_5f6e:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_5f73:  beq.s      IL_5f9a

    IL_5f75:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_5f7a:  ldloc.s    V_21
    IL_5f7c:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_5f81:  box        [mscorlib]System.Int64
    IL_5f86:  ldloc.s    V_21
    IL_5f88:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_5f8d:  box        [mscorlib]System.Int64
    IL_5f92:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5f97:  ldc.i4.1
    IL_5f98:  stloc.s    V_20
    IL_5f9a:  ldsfld     int64 IntrinsicTest::temp
    IL_5f9f:  ldsfld     int64 IntrinsicTest::idmp
    IL_5fa4:  beq.s      IL_5fc7

    IL_5fa6:  ldstr      "temp mismatch {0} {1}"
    IL_5fab:  ldsfld     int64 IntrinsicTest::temp
    IL_5fb0:  box        [mscorlib]System.Int64
    IL_5fb5:  ldsfld     int64 IntrinsicTest::idmp
    IL_5fba:  box        [mscorlib]System.Int64
    IL_5fbf:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_5fc4:  ldc.i4.1
    IL_5fc5:  stloc.s    V_20
    IL_5fc7:  ldsflda    int32 IntrinsicTest::counter
    IL_5fcc:  ldsfld     int32 IntrinsicTest::counter
    IL_5fd1:  ldloc.1
    IL_5fd2:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_5fd7:  stloc.0
    IL_5fd8:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5fdd:  stloc.s    V_10
    IL_5fdf:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5fe4:  ldloc.s    V_11
    IL_5fe6:  bne.un.s   IL_5ff4

    IL_5fe8:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5fed:  stsfld     int32 IntrinsicTest::id_counter
    IL_5ff2:  br.s       IL_5fff

    IL_5ff4:  ldsfld     int32 IntrinsicTest::id_counter
    IL_5ff9:  conv.i8
    IL_5ffa:  stsfld     int64 IntrinsicTest::idjunk
    IL_5fff:  ldstr      "------------------------------------CMPXCHG  1 1 5 3"
    IL_6004:  call       void [System.Console]System.Console::WriteLine(string)
    IL_6009:  ldloc.0
    IL_600a:  ldloc.s    V_10
    IL_600c:  beq.s      IL_6028

    IL_600e:  ldc.i4.1
    IL_600f:  stloc.s    V_20
    IL_6011:  ldstr      "te0 check failed {0} {1}"
    IL_6016:  ldloc.0
    IL_6017:  box        [mscorlib]System.Int32
    IL_601c:  ldloc.s    V_10
    IL_601e:  box        [mscorlib]System.Int32
    IL_6023:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6028:  ldloc.1
    IL_6029:  ldloc.s    V_11
    IL_602b:  beq.s      IL_6047

    IL_602d:  ldc.i4.1
    IL_602e:  stloc.s    V_20
    IL_6030:  ldstr      "te1 check failed {0} {1}"
    IL_6035:  ldloc.1
    IL_6036:  box        [mscorlib]System.Int32
    IL_603b:  ldloc.s    V_11
    IL_603d:  box        [mscorlib]System.Int32
    IL_6042:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6047:  ldloc.2
    IL_6048:  ldloc.s    V_12
    IL_604a:  beq.s      IL_6066

    IL_604c:  ldc.i4.1
    IL_604d:  stloc.s    V_20
    IL_604f:  ldstr      "te2 check failed {0} {1}"
    IL_6054:  ldloc.2
    IL_6055:  box        [mscorlib]System.Int32
    IL_605a:  ldloc.s    V_12
    IL_605c:  box        [mscorlib]System.Int32
    IL_6061:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6066:  ldloc.3
    IL_6067:  ldloc.s    V_13
    IL_6069:  beq.s      IL_6085

    IL_606b:  ldc.i4.1
    IL_606c:  stloc.s    V_20
    IL_606e:  ldstr      "te3 check failed {0} {1}"
    IL_6073:  ldloc.3
    IL_6074:  box        [mscorlib]System.Int32
    IL_6079:  ldloc.s    V_13
    IL_607b:  box        [mscorlib]System.Int32
    IL_6080:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6085:  ldloc.s    V_4
    IL_6087:  ldloc.s    V_14
    IL_6089:  beq.s      IL_60a6

    IL_608b:  ldc.i4.1
    IL_608c:  stloc.s    V_20
    IL_608e:  ldstr      "te064 check failed {0} {1}"
    IL_6093:  ldloc.s    V_4
    IL_6095:  box        [mscorlib]System.Int64
    IL_609a:  ldloc.s    V_14
    IL_609c:  box        [mscorlib]System.Int64
    IL_60a1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_60a6:  ldloc.s    V_5
    IL_60a8:  ldloc.s    V_15
    IL_60aa:  beq.s      IL_60c7

    IL_60ac:  ldc.i4.1
    IL_60ad:  stloc.s    V_20
    IL_60af:  ldstr      "te164 check failed {0} {1}"
    IL_60b4:  ldloc.s    V_5
    IL_60b6:  box        [mscorlib]System.Int64
    IL_60bb:  ldloc.s    V_15
    IL_60bd:  box        [mscorlib]System.Int64
    IL_60c2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_60c7:  ldloc.s    V_6
    IL_60c9:  ldloc.s    V_16
    IL_60cb:  beq.s      IL_60e8

    IL_60cd:  ldc.i4.1
    IL_60ce:  stloc.s    V_20
    IL_60d0:  ldstr      "te264 check failed {0} {1}"
    IL_60d5:  ldloc.s    V_6
    IL_60d7:  box        [mscorlib]System.Int64
    IL_60dc:  ldloc.s    V_16
    IL_60de:  box        [mscorlib]System.Int64
    IL_60e3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_60e8:  ldloc.s    V_7
    IL_60ea:  ldloc.s    V_17
    IL_60ec:  beq.s      IL_6109

    IL_60ee:  ldc.i4.1
    IL_60ef:  stloc.s    V_20
    IL_60f1:  ldstr      "te364 check failed {0} {1}"
    IL_60f6:  ldloc.s    V_7
    IL_60f8:  box        [mscorlib]System.Int64
    IL_60fd:  ldloc.s    V_17
    IL_60ff:  box        [mscorlib]System.Int64
    IL_6104:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6109:  ldloc.s    V_8
    IL_610b:  ldloc.s    V_18
    IL_610d:  beq.s      IL_612a

    IL_610f:  ldc.i4.1
    IL_6110:  stloc.s    V_20
    IL_6112:  ldstr      "dummy check failed {0} {1}"
    IL_6117:  ldloc.s    V_8
    IL_6119:  box        [mscorlib]System.Int32
    IL_611e:  ldloc.s    V_18
    IL_6120:  box        [mscorlib]System.Int32
    IL_6125:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_612a:  ldloc.s    V_9
    IL_612c:  ldloc.s    V_19
    IL_612e:  beq.s      IL_614b

    IL_6130:  ldc.i4.1
    IL_6131:  stloc.s    V_20
    IL_6133:  ldstr      "dummy64 check failed {0} {1}"
    IL_6138:  ldloc.s    V_9
    IL_613a:  box        [mscorlib]System.Int64
    IL_613f:  ldloc.s    V_19
    IL_6141:  box        [mscorlib]System.Int64
    IL_6146:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_614b:  ldsfld     int32 IntrinsicTest::counter
    IL_6150:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6155:  beq.s      IL_6178

    IL_6157:  ldstr      "counter mismatch {0} {1}"
    IL_615c:  ldsfld     int32 IntrinsicTest::counter
    IL_6161:  box        [mscorlib]System.Int32
    IL_6166:  ldsfld     int32 IntrinsicTest::id_counter
    IL_616b:  box        [mscorlib]System.Int32
    IL_6170:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6175:  ldc.i4.1
    IL_6176:  stloc.s    V_20
    IL_6178:  ldsfld     int64 IntrinsicTest::counter64
    IL_617d:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6182:  beq.s      IL_61a5

    IL_6184:  ldstr      "counter64 mismatch {0} {1}"
    IL_6189:  ldsfld     int64 IntrinsicTest::counter64
    IL_618e:  box        [mscorlib]System.Int64
    IL_6193:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6198:  box        [mscorlib]System.Int64
    IL_619d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_61a2:  ldc.i4.1
    IL_61a3:  stloc.s    V_20
    IL_61a5:  ldloc.s    V_21
    IL_61a7:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_61ac:  ldloc.s    V_21
    IL_61ae:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_61b3:  beq.s      IL_61da

    IL_61b5:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_61ba:  ldloc.s    V_21
    IL_61bc:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_61c1:  box        [mscorlib]System.Int32
    IL_61c6:  ldloc.s    V_21
    IL_61c8:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_61cd:  box        [mscorlib]System.Int32
    IL_61d2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_61d7:  ldc.i4.1
    IL_61d8:  stloc.s    V_20
    IL_61da:  ldloc.s    V_21
    IL_61dc:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_61e1:  ldloc.s    V_21
    IL_61e3:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_61e8:  beq.s      IL_620f

    IL_61ea:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_61ef:  ldloc.s    V_21
    IL_61f1:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_61f6:  box        [mscorlib]System.Int64
    IL_61fb:  ldloc.s    V_21
    IL_61fd:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_6202:  box        [mscorlib]System.Int64
    IL_6207:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_620c:  ldc.i4.1
    IL_620d:  stloc.s    V_20
    IL_620f:  ldsfld     int64 IntrinsicTest::temp
    IL_6214:  ldsfld     int64 IntrinsicTest::idmp
    IL_6219:  beq.s      IL_623c

    IL_621b:  ldstr      "temp mismatch {0} {1}"
    IL_6220:  ldsfld     int64 IntrinsicTest::temp
    IL_6225:  box        [mscorlib]System.Int64
    IL_622a:  ldsfld     int64 IntrinsicTest::idmp
    IL_622f:  box        [mscorlib]System.Int64
    IL_6234:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6239:  ldc.i4.1
    IL_623a:  stloc.s    V_20
    IL_623c:  ldsflda    int32 IntrinsicTest::counter
    IL_6241:  ldsfld     int32 IntrinsicTest::counter
    IL_6246:  ldloc.1
    IL_6247:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_624c:  stsfld     int32 IntrinsicTest::counter
    IL_6251:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6256:  stsfld     int32 IntrinsicTest::id_counter
    IL_625b:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6260:  ldloc.s    V_11
    IL_6262:  bne.un.s   IL_6271

    IL_6264:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6269:  conv.i8
    IL_626a:  stsfld     int64 IntrinsicTest::idjunk
    IL_626f:  br.s       IL_627c

    IL_6271:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6276:  conv.i8
    IL_6277:  stsfld     int64 IntrinsicTest::idjunk
    IL_627c:  ldstr      "------------------------------------CMPXCHG  1 1 5 4"
    IL_6281:  call       void [System.Console]System.Console::WriteLine(string)
    IL_6286:  ldloc.0
    IL_6287:  ldloc.s    V_10
    IL_6289:  beq.s      IL_62a5

    IL_628b:  ldc.i4.1
    IL_628c:  stloc.s    V_20
    IL_628e:  ldstr      "te0 check failed {0} {1}"
    IL_6293:  ldloc.0
    IL_6294:  box        [mscorlib]System.Int32
    IL_6299:  ldloc.s    V_10
    IL_629b:  box        [mscorlib]System.Int32
    IL_62a0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_62a5:  ldloc.1
    IL_62a6:  ldloc.s    V_11
    IL_62a8:  beq.s      IL_62c4

    IL_62aa:  ldc.i4.1
    IL_62ab:  stloc.s    V_20
    IL_62ad:  ldstr      "te1 check failed {0} {1}"
    IL_62b2:  ldloc.1
    IL_62b3:  box        [mscorlib]System.Int32
    IL_62b8:  ldloc.s    V_11
    IL_62ba:  box        [mscorlib]System.Int32
    IL_62bf:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_62c4:  ldloc.2
    IL_62c5:  ldloc.s    V_12
    IL_62c7:  beq.s      IL_62e3

    IL_62c9:  ldc.i4.1
    IL_62ca:  stloc.s    V_20
    IL_62cc:  ldstr      "te2 check failed {0} {1}"
    IL_62d1:  ldloc.2
    IL_62d2:  box        [mscorlib]System.Int32
    IL_62d7:  ldloc.s    V_12
    IL_62d9:  box        [mscorlib]System.Int32
    IL_62de:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_62e3:  ldloc.3
    IL_62e4:  ldloc.s    V_13
    IL_62e6:  beq.s      IL_6302

    IL_62e8:  ldc.i4.1
    IL_62e9:  stloc.s    V_20
    IL_62eb:  ldstr      "te3 check failed {0} {1}"
    IL_62f0:  ldloc.3
    IL_62f1:  box        [mscorlib]System.Int32
    IL_62f6:  ldloc.s    V_13
    IL_62f8:  box        [mscorlib]System.Int32
    IL_62fd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6302:  ldloc.s    V_4
    IL_6304:  ldloc.s    V_14
    IL_6306:  beq.s      IL_6323

    IL_6308:  ldc.i4.1
    IL_6309:  stloc.s    V_20
    IL_630b:  ldstr      "te064 check failed {0} {1}"
    IL_6310:  ldloc.s    V_4
    IL_6312:  box        [mscorlib]System.Int64
    IL_6317:  ldloc.s    V_14
    IL_6319:  box        [mscorlib]System.Int64
    IL_631e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6323:  ldloc.s    V_5
    IL_6325:  ldloc.s    V_15
    IL_6327:  beq.s      IL_6344

    IL_6329:  ldc.i4.1
    IL_632a:  stloc.s    V_20
    IL_632c:  ldstr      "te164 check failed {0} {1}"
    IL_6331:  ldloc.s    V_5
    IL_6333:  box        [mscorlib]System.Int64
    IL_6338:  ldloc.s    V_15
    IL_633a:  box        [mscorlib]System.Int64
    IL_633f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6344:  ldloc.s    V_6
    IL_6346:  ldloc.s    V_16
    IL_6348:  beq.s      IL_6365

    IL_634a:  ldc.i4.1
    IL_634b:  stloc.s    V_20
    IL_634d:  ldstr      "te264 check failed {0} {1}"
    IL_6352:  ldloc.s    V_6
    IL_6354:  box        [mscorlib]System.Int64
    IL_6359:  ldloc.s    V_16
    IL_635b:  box        [mscorlib]System.Int64
    IL_6360:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6365:  ldloc.s    V_7
    IL_6367:  ldloc.s    V_17
    IL_6369:  beq.s      IL_6386

    IL_636b:  ldc.i4.1
    IL_636c:  stloc.s    V_20
    IL_636e:  ldstr      "te364 check failed {0} {1}"
    IL_6373:  ldloc.s    V_7
    IL_6375:  box        [mscorlib]System.Int64
    IL_637a:  ldloc.s    V_17
    IL_637c:  box        [mscorlib]System.Int64
    IL_6381:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6386:  ldloc.s    V_8
    IL_6388:  ldloc.s    V_18
    IL_638a:  beq.s      IL_63a7

    IL_638c:  ldc.i4.1
    IL_638d:  stloc.s    V_20
    IL_638f:  ldstr      "dummy check failed {0} {1}"
    IL_6394:  ldloc.s    V_8
    IL_6396:  box        [mscorlib]System.Int32
    IL_639b:  ldloc.s    V_18
    IL_639d:  box        [mscorlib]System.Int32
    IL_63a2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_63a7:  ldloc.s    V_9
    IL_63a9:  ldloc.s    V_19
    IL_63ab:  beq.s      IL_63c8

    IL_63ad:  ldc.i4.1
    IL_63ae:  stloc.s    V_20
    IL_63b0:  ldstr      "dummy64 check failed {0} {1}"
    IL_63b5:  ldloc.s    V_9
    IL_63b7:  box        [mscorlib]System.Int64
    IL_63bc:  ldloc.s    V_19
    IL_63be:  box        [mscorlib]System.Int64
    IL_63c3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_63c8:  ldsfld     int32 IntrinsicTest::counter
    IL_63cd:  ldsfld     int32 IntrinsicTest::id_counter
    IL_63d2:  beq.s      IL_63f5

    IL_63d4:  ldstr      "counter mismatch {0} {1}"
    IL_63d9:  ldsfld     int32 IntrinsicTest::counter
    IL_63de:  box        [mscorlib]System.Int32
    IL_63e3:  ldsfld     int32 IntrinsicTest::id_counter
    IL_63e8:  box        [mscorlib]System.Int32
    IL_63ed:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_63f2:  ldc.i4.1
    IL_63f3:  stloc.s    V_20
    IL_63f5:  ldsfld     int64 IntrinsicTest::counter64
    IL_63fa:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_63ff:  beq.s      IL_6422

    IL_6401:  ldstr      "counter64 mismatch {0} {1}"
    IL_6406:  ldsfld     int64 IntrinsicTest::counter64
    IL_640b:  box        [mscorlib]System.Int64
    IL_6410:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6415:  box        [mscorlib]System.Int64
    IL_641a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_641f:  ldc.i4.1
    IL_6420:  stloc.s    V_20
    IL_6422:  ldloc.s    V_21
    IL_6424:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_6429:  ldloc.s    V_21
    IL_642b:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_6430:  beq.s      IL_6457

    IL_6432:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_6437:  ldloc.s    V_21
    IL_6439:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_643e:  box        [mscorlib]System.Int32
    IL_6443:  ldloc.s    V_21
    IL_6445:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_644a:  box        [mscorlib]System.Int32
    IL_644f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6454:  ldc.i4.1
    IL_6455:  stloc.s    V_20
    IL_6457:  ldloc.s    V_21
    IL_6459:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_645e:  ldloc.s    V_21
    IL_6460:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_6465:  beq.s      IL_648c

    IL_6467:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_646c:  ldloc.s    V_21
    IL_646e:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_6473:  box        [mscorlib]System.Int64
    IL_6478:  ldloc.s    V_21
    IL_647a:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_647f:  box        [mscorlib]System.Int64
    IL_6484:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6489:  ldc.i4.1
    IL_648a:  stloc.s    V_20
    IL_648c:  ldsfld     int64 IntrinsicTest::temp
    IL_6491:  ldsfld     int64 IntrinsicTest::idmp
    IL_6496:  beq.s      IL_64b9

    IL_6498:  ldstr      "temp mismatch {0} {1}"
    IL_649d:  ldsfld     int64 IntrinsicTest::temp
    IL_64a2:  box        [mscorlib]System.Int64
    IL_64a7:  ldsfld     int64 IntrinsicTest::idmp
    IL_64ac:  box        [mscorlib]System.Int64
    IL_64b1:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_64b6:  ldc.i4.1
    IL_64b7:  stloc.s    V_20
    IL_64b9:  ldloc.s    V_21
    IL_64bb:  ldsflda    int32 IntrinsicTest::counter
    IL_64c0:  ldsfld     int32 IntrinsicTest::counter
    IL_64c5:  ldloc.1
    IL_64c6:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_64cb:  stfld      int32 IntrinsicTest::instanceCounter
    IL_64d0:  ldloc.s    V_21
    IL_64d2:  ldsfld     int32 IntrinsicTest::id_counter
    IL_64d7:  stfld      int32 IntrinsicTest::id_instanceCounter
    IL_64dc:  ldsfld     int32 IntrinsicTest::id_counter
    IL_64e1:  ldloc.s    V_11
    IL_64e3:  bne.un.s   IL_64f1

    IL_64e5:  ldsfld     int32 IntrinsicTest::id_counter
    IL_64ea:  stsfld     int32 IntrinsicTest::id_counter
    IL_64ef:  br.s       IL_64fc

    IL_64f1:  ldsfld     int32 IntrinsicTest::id_counter
    IL_64f6:  conv.i8
    IL_64f7:  stsfld     int64 IntrinsicTest::idjunk
    IL_64fc:  ldstr      "------------------------------------CMPXCHG  1 1 5 5"
    IL_6501:  call       void [System.Console]System.Console::WriteLine(string)
    IL_6506:  ldloc.0
    IL_6507:  ldloc.s    V_10
    IL_6509:  beq.s      IL_6525

    IL_650b:  ldc.i4.1
    IL_650c:  stloc.s    V_20
    IL_650e:  ldstr      "te0 check failed {0} {1}"
    IL_6513:  ldloc.0
    IL_6514:  box        [mscorlib]System.Int32
    IL_6519:  ldloc.s    V_10
    IL_651b:  box        [mscorlib]System.Int32
    IL_6520:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6525:  ldloc.1
    IL_6526:  ldloc.s    V_11
    IL_6528:  beq.s      IL_6544

    IL_652a:  ldc.i4.1
    IL_652b:  stloc.s    V_20
    IL_652d:  ldstr      "te1 check failed {0} {1}"
    IL_6532:  ldloc.1
    IL_6533:  box        [mscorlib]System.Int32
    IL_6538:  ldloc.s    V_11
    IL_653a:  box        [mscorlib]System.Int32
    IL_653f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6544:  ldloc.2
    IL_6545:  ldloc.s    V_12
    IL_6547:  beq.s      IL_6563

    IL_6549:  ldc.i4.1
    IL_654a:  stloc.s    V_20
    IL_654c:  ldstr      "te2 check failed {0} {1}"
    IL_6551:  ldloc.2
    IL_6552:  box        [mscorlib]System.Int32
    IL_6557:  ldloc.s    V_12
    IL_6559:  box        [mscorlib]System.Int32
    IL_655e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6563:  ldloc.3
    IL_6564:  ldloc.s    V_13
    IL_6566:  beq.s      IL_6582

    IL_6568:  ldc.i4.1
    IL_6569:  stloc.s    V_20
    IL_656b:  ldstr      "te3 check failed {0} {1}"
    IL_6570:  ldloc.3
    IL_6571:  box        [mscorlib]System.Int32
    IL_6576:  ldloc.s    V_13
    IL_6578:  box        [mscorlib]System.Int32
    IL_657d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6582:  ldloc.s    V_4
    IL_6584:  ldloc.s    V_14
    IL_6586:  beq.s      IL_65a3

    IL_6588:  ldc.i4.1
    IL_6589:  stloc.s    V_20
    IL_658b:  ldstr      "te064 check failed {0} {1}"
    IL_6590:  ldloc.s    V_4
    IL_6592:  box        [mscorlib]System.Int64
    IL_6597:  ldloc.s    V_14
    IL_6599:  box        [mscorlib]System.Int64
    IL_659e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_65a3:  ldloc.s    V_5
    IL_65a5:  ldloc.s    V_15
    IL_65a7:  beq.s      IL_65c4

    IL_65a9:  ldc.i4.1
    IL_65aa:  stloc.s    V_20
    IL_65ac:  ldstr      "te164 check failed {0} {1}"
    IL_65b1:  ldloc.s    V_5
    IL_65b3:  box        [mscorlib]System.Int64
    IL_65b8:  ldloc.s    V_15
    IL_65ba:  box        [mscorlib]System.Int64
    IL_65bf:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_65c4:  ldloc.s    V_6
    IL_65c6:  ldloc.s    V_16
    IL_65c8:  beq.s      IL_65e5

    IL_65ca:  ldc.i4.1
    IL_65cb:  stloc.s    V_20
    IL_65cd:  ldstr      "te264 check failed {0} {1}"
    IL_65d2:  ldloc.s    V_6
    IL_65d4:  box        [mscorlib]System.Int64
    IL_65d9:  ldloc.s    V_16
    IL_65db:  box        [mscorlib]System.Int64
    IL_65e0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_65e5:  ldloc.s    V_7
    IL_65e7:  ldloc.s    V_17
    IL_65e9:  beq.s      IL_6606

    IL_65eb:  ldc.i4.1
    IL_65ec:  stloc.s    V_20
    IL_65ee:  ldstr      "te364 check failed {0} {1}"
    IL_65f3:  ldloc.s    V_7
    IL_65f5:  box        [mscorlib]System.Int64
    IL_65fa:  ldloc.s    V_17
    IL_65fc:  box        [mscorlib]System.Int64
    IL_6601:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6606:  ldloc.s    V_8
    IL_6608:  ldloc.s    V_18
    IL_660a:  beq.s      IL_6627

    IL_660c:  ldc.i4.1
    IL_660d:  stloc.s    V_20
    IL_660f:  ldstr      "dummy check failed {0} {1}"
    IL_6614:  ldloc.s    V_8
    IL_6616:  box        [mscorlib]System.Int32
    IL_661b:  ldloc.s    V_18
    IL_661d:  box        [mscorlib]System.Int32
    IL_6622:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6627:  ldloc.s    V_9
    IL_6629:  ldloc.s    V_19
    IL_662b:  beq.s      IL_6648

    IL_662d:  ldc.i4.1
    IL_662e:  stloc.s    V_20
    IL_6630:  ldstr      "dummy64 check failed {0} {1}"
    IL_6635:  ldloc.s    V_9
    IL_6637:  box        [mscorlib]System.Int64
    IL_663c:  ldloc.s    V_19
    IL_663e:  box        [mscorlib]System.Int64
    IL_6643:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6648:  ldsfld     int32 IntrinsicTest::counter
    IL_664d:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6652:  beq.s      IL_6675

    IL_6654:  ldstr      "counter mismatch {0} {1}"
    IL_6659:  ldsfld     int32 IntrinsicTest::counter
    IL_665e:  box        [mscorlib]System.Int32
    IL_6663:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6668:  box        [mscorlib]System.Int32
    IL_666d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6672:  ldc.i4.1
    IL_6673:  stloc.s    V_20
    IL_6675:  ldsfld     int64 IntrinsicTest::counter64
    IL_667a:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_667f:  beq.s      IL_66a2

    IL_6681:  ldstr      "counter64 mismatch {0} {1}"
    IL_6686:  ldsfld     int64 IntrinsicTest::counter64
    IL_668b:  box        [mscorlib]System.Int64
    IL_6690:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6695:  box        [mscorlib]System.Int64
    IL_669a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_669f:  ldc.i4.1
    IL_66a0:  stloc.s    V_20
    IL_66a2:  ldloc.s    V_21
    IL_66a4:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_66a9:  ldloc.s    V_21
    IL_66ab:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_66b0:  beq.s      IL_66d7

    IL_66b2:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_66b7:  ldloc.s    V_21
    IL_66b9:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_66be:  box        [mscorlib]System.Int32
    IL_66c3:  ldloc.s    V_21
    IL_66c5:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_66ca:  box        [mscorlib]System.Int32
    IL_66cf:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_66d4:  ldc.i4.1
    IL_66d5:  stloc.s    V_20
    IL_66d7:  ldloc.s    V_21
    IL_66d9:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_66de:  ldloc.s    V_21
    IL_66e0:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_66e5:  beq.s      IL_670c

    IL_66e7:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_66ec:  ldloc.s    V_21
    IL_66ee:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_66f3:  box        [mscorlib]System.Int64
    IL_66f8:  ldloc.s    V_21
    IL_66fa:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_66ff:  box        [mscorlib]System.Int64
    IL_6704:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6709:  ldc.i4.1
    IL_670a:  stloc.s    V_20
    IL_670c:  ldsfld     int64 IntrinsicTest::temp
    IL_6711:  ldsfld     int64 IntrinsicTest::idmp
    IL_6716:  beq.s      IL_6739

    IL_6718:  ldstr      "temp mismatch {0} {1}"
    IL_671d:  ldsfld     int64 IntrinsicTest::temp
    IL_6722:  box        [mscorlib]System.Int64
    IL_6727:  ldsfld     int64 IntrinsicTest::idmp
    IL_672c:  box        [mscorlib]System.Int64
    IL_6731:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6736:  ldc.i4.1
    IL_6737:  stloc.s    V_20
    IL_6739:  ldsflda    int32 IntrinsicTest::counter
    IL_673e:  ldsfld     int32 IntrinsicTest::counter
    IL_6743:  ldloc.1
    IL_6744:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_6749:  stloc.3
    IL_674a:  ldsfld     int32 IntrinsicTest::id_counter
    IL_674f:  stloc.s    V_13
    IL_6751:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6756:  ldloc.s    V_11
    IL_6758:  bne.un.s   IL_6766

    IL_675a:  ldsfld     int32 IntrinsicTest::id_counter
    IL_675f:  stsfld     int32 IntrinsicTest::id_counter
    IL_6764:  br.s       IL_6771

    IL_6766:  ldsfld     int32 IntrinsicTest::id_counter
    IL_676b:  conv.i8
    IL_676c:  stsfld     int64 IntrinsicTest::idjunk
    IL_6771:  ldstr      "------------------------------------CMPXCHG  1 1 5 6"
    IL_6776:  call       void [System.Console]System.Console::WriteLine(string)
    IL_677b:  ldloc.0
    IL_677c:  ldloc.s    V_10
    IL_677e:  beq.s      IL_679a

    IL_6780:  ldc.i4.1
    IL_6781:  stloc.s    V_20
    IL_6783:  ldstr      "te0 check failed {0} {1}"
    IL_6788:  ldloc.0
    IL_6789:  box        [mscorlib]System.Int32
    IL_678e:  ldloc.s    V_10
    IL_6790:  box        [mscorlib]System.Int32
    IL_6795:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_679a:  ldloc.1
    IL_679b:  ldloc.s    V_11
    IL_679d:  beq.s      IL_67b9

    IL_679f:  ldc.i4.1
    IL_67a0:  stloc.s    V_20
    IL_67a2:  ldstr      "te1 check failed {0} {1}"
    IL_67a7:  ldloc.1
    IL_67a8:  box        [mscorlib]System.Int32
    IL_67ad:  ldloc.s    V_11
    IL_67af:  box        [mscorlib]System.Int32
    IL_67b4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_67b9:  ldloc.2
    IL_67ba:  ldloc.s    V_12
    IL_67bc:  beq.s      IL_67d8

    IL_67be:  ldc.i4.1
    IL_67bf:  stloc.s    V_20
    IL_67c1:  ldstr      "te2 check failed {0} {1}"
    IL_67c6:  ldloc.2
    IL_67c7:  box        [mscorlib]System.Int32
    IL_67cc:  ldloc.s    V_12
    IL_67ce:  box        [mscorlib]System.Int32
    IL_67d3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_67d8:  ldloc.3
    IL_67d9:  ldloc.s    V_13
    IL_67db:  beq.s      IL_67f7

    IL_67dd:  ldc.i4.1
    IL_67de:  stloc.s    V_20
    IL_67e0:  ldstr      "te3 check failed {0} {1}"
    IL_67e5:  ldloc.3
    IL_67e6:  box        [mscorlib]System.Int32
    IL_67eb:  ldloc.s    V_13
    IL_67ed:  box        [mscorlib]System.Int32
    IL_67f2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_67f7:  ldloc.s    V_4
    IL_67f9:  ldloc.s    V_14
    IL_67fb:  beq.s      IL_6818

    IL_67fd:  ldc.i4.1
    IL_67fe:  stloc.s    V_20
    IL_6800:  ldstr      "te064 check failed {0} {1}"
    IL_6805:  ldloc.s    V_4
    IL_6807:  box        [mscorlib]System.Int64
    IL_680c:  ldloc.s    V_14
    IL_680e:  box        [mscorlib]System.Int64
    IL_6813:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6818:  ldloc.s    V_5
    IL_681a:  ldloc.s    V_15
    IL_681c:  beq.s      IL_6839

    IL_681e:  ldc.i4.1
    IL_681f:  stloc.s    V_20
    IL_6821:  ldstr      "te164 check failed {0} {1}"
    IL_6826:  ldloc.s    V_5
    IL_6828:  box        [mscorlib]System.Int64
    IL_682d:  ldloc.s    V_15
    IL_682f:  box        [mscorlib]System.Int64
    IL_6834:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6839:  ldloc.s    V_6
    IL_683b:  ldloc.s    V_16
    IL_683d:  beq.s      IL_685a

    IL_683f:  ldc.i4.1
    IL_6840:  stloc.s    V_20
    IL_6842:  ldstr      "te264 check failed {0} {1}"
    IL_6847:  ldloc.s    V_6
    IL_6849:  box        [mscorlib]System.Int64
    IL_684e:  ldloc.s    V_16
    IL_6850:  box        [mscorlib]System.Int64
    IL_6855:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_685a:  ldloc.s    V_7
    IL_685c:  ldloc.s    V_17
    IL_685e:  beq.s      IL_687b

    IL_6860:  ldc.i4.1
    IL_6861:  stloc.s    V_20
    IL_6863:  ldstr      "te364 check failed {0} {1}"
    IL_6868:  ldloc.s    V_7
    IL_686a:  box        [mscorlib]System.Int64
    IL_686f:  ldloc.s    V_17
    IL_6871:  box        [mscorlib]System.Int64
    IL_6876:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_687b:  ldloc.s    V_8
    IL_687d:  ldloc.s    V_18
    IL_687f:  beq.s      IL_689c

    IL_6881:  ldc.i4.1
    IL_6882:  stloc.s    V_20
    IL_6884:  ldstr      "dummy check failed {0} {1}"
    IL_6889:  ldloc.s    V_8
    IL_688b:  box        [mscorlib]System.Int32
    IL_6890:  ldloc.s    V_18
    IL_6892:  box        [mscorlib]System.Int32
    IL_6897:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_689c:  ldloc.s    V_9
    IL_689e:  ldloc.s    V_19
    IL_68a0:  beq.s      IL_68bd

    IL_68a2:  ldc.i4.1
    IL_68a3:  stloc.s    V_20
    IL_68a5:  ldstr      "dummy64 check failed {0} {1}"
    IL_68aa:  ldloc.s    V_9
    IL_68ac:  box        [mscorlib]System.Int64
    IL_68b1:  ldloc.s    V_19
    IL_68b3:  box        [mscorlib]System.Int64
    IL_68b8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_68bd:  ldsfld     int32 IntrinsicTest::counter
    IL_68c2:  ldsfld     int32 IntrinsicTest::id_counter
    IL_68c7:  beq.s      IL_68ea

    IL_68c9:  ldstr      "counter mismatch {0} {1}"
    IL_68ce:  ldsfld     int32 IntrinsicTest::counter
    IL_68d3:  box        [mscorlib]System.Int32
    IL_68d8:  ldsfld     int32 IntrinsicTest::id_counter
    IL_68dd:  box        [mscorlib]System.Int32
    IL_68e2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_68e7:  ldc.i4.1
    IL_68e8:  stloc.s    V_20
    IL_68ea:  ldsfld     int64 IntrinsicTest::counter64
    IL_68ef:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_68f4:  beq.s      IL_6917

    IL_68f6:  ldstr      "counter64 mismatch {0} {1}"
    IL_68fb:  ldsfld     int64 IntrinsicTest::counter64
    IL_6900:  box        [mscorlib]System.Int64
    IL_6905:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_690a:  box        [mscorlib]System.Int64
    IL_690f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6914:  ldc.i4.1
    IL_6915:  stloc.s    V_20
    IL_6917:  ldloc.s    V_21
    IL_6919:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_691e:  ldloc.s    V_21
    IL_6920:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_6925:  beq.s      IL_694c

    IL_6927:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_692c:  ldloc.s    V_21
    IL_692e:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_6933:  box        [mscorlib]System.Int32
    IL_6938:  ldloc.s    V_21
    IL_693a:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_693f:  box        [mscorlib]System.Int32
    IL_6944:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6949:  ldc.i4.1
    IL_694a:  stloc.s    V_20
    IL_694c:  ldloc.s    V_21
    IL_694e:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_6953:  ldloc.s    V_21
    IL_6955:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_695a:  beq.s      IL_6981

    IL_695c:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_6961:  ldloc.s    V_21
    IL_6963:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_6968:  box        [mscorlib]System.Int64
    IL_696d:  ldloc.s    V_21
    IL_696f:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_6974:  box        [mscorlib]System.Int64
    IL_6979:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_697e:  ldc.i4.1
    IL_697f:  stloc.s    V_20
    IL_6981:  ldsfld     int64 IntrinsicTest::temp
    IL_6986:  ldsfld     int64 IntrinsicTest::idmp
    IL_698b:  beq.s      IL_69ae

    IL_698d:  ldstr      "temp mismatch {0} {1}"
    IL_6992:  ldsfld     int64 IntrinsicTest::temp
    IL_6997:  box        [mscorlib]System.Int64
    IL_699c:  ldsfld     int64 IntrinsicTest::idmp
    IL_69a1:  box        [mscorlib]System.Int64
    IL_69a6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_69ab:  ldc.i4.1
    IL_69ac:  stloc.s    V_20
    IL_69ae:  ldsflda    int32 IntrinsicTest::counter
    IL_69b3:  ldsfld     int32 IntrinsicTest::counter
    IL_69b8:  ldloc.2
    IL_69b9:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_69be:  stloc.0
    IL_69bf:  ldsfld     int32 IntrinsicTest::id_counter
    IL_69c4:  stloc.s    V_10
    IL_69c6:  ldsfld     int32 IntrinsicTest::id_counter
    IL_69cb:  ldloc.s    V_12
    IL_69cd:  bne.un.s   IL_69db

    IL_69cf:  ldsfld     int32 IntrinsicTest::id_counter
    IL_69d4:  stsfld     int32 IntrinsicTest::id_counter
    IL_69d9:  br.s       IL_69e6

    IL_69db:  ldsfld     int32 IntrinsicTest::id_counter
    IL_69e0:  conv.i8
    IL_69e1:  stsfld     int64 IntrinsicTest::idjunk
    IL_69e6:  ldstr      "------------------------------------CMPXCHG  1 1 6 0"
    IL_69eb:  call       void [System.Console]System.Console::WriteLine(string)
    IL_69f0:  ldloc.0
    IL_69f1:  ldloc.s    V_10
    IL_69f3:  beq.s      IL_6a0f

    IL_69f5:  ldc.i4.1
    IL_69f6:  stloc.s    V_20
    IL_69f8:  ldstr      "te0 check failed {0} {1}"
    IL_69fd:  ldloc.0
    IL_69fe:  box        [mscorlib]System.Int32
    IL_6a03:  ldloc.s    V_10
    IL_6a05:  box        [mscorlib]System.Int32
    IL_6a0a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6a0f:  ldloc.1
    IL_6a10:  ldloc.s    V_11
    IL_6a12:  beq.s      IL_6a2e

    IL_6a14:  ldc.i4.1
    IL_6a15:  stloc.s    V_20
    IL_6a17:  ldstr      "te1 check failed {0} {1}"
    IL_6a1c:  ldloc.1
    IL_6a1d:  box        [mscorlib]System.Int32
    IL_6a22:  ldloc.s    V_11
    IL_6a24:  box        [mscorlib]System.Int32
    IL_6a29:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6a2e:  ldloc.2
    IL_6a2f:  ldloc.s    V_12
    IL_6a31:  beq.s      IL_6a4d

    IL_6a33:  ldc.i4.1
    IL_6a34:  stloc.s    V_20
    IL_6a36:  ldstr      "te2 check failed {0} {1}"
    IL_6a3b:  ldloc.2
    IL_6a3c:  box        [mscorlib]System.Int32
    IL_6a41:  ldloc.s    V_12
    IL_6a43:  box        [mscorlib]System.Int32
    IL_6a48:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6a4d:  ldloc.3
    IL_6a4e:  ldloc.s    V_13
    IL_6a50:  beq.s      IL_6a6c

    IL_6a52:  ldc.i4.1
    IL_6a53:  stloc.s    V_20
    IL_6a55:  ldstr      "te3 check failed {0} {1}"
    IL_6a5a:  ldloc.3
    IL_6a5b:  box        [mscorlib]System.Int32
    IL_6a60:  ldloc.s    V_13
    IL_6a62:  box        [mscorlib]System.Int32
    IL_6a67:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6a6c:  ldloc.s    V_4
    IL_6a6e:  ldloc.s    V_14
    IL_6a70:  beq.s      IL_6a8d

    IL_6a72:  ldc.i4.1
    IL_6a73:  stloc.s    V_20
    IL_6a75:  ldstr      "te064 check failed {0} {1}"
    IL_6a7a:  ldloc.s    V_4
    IL_6a7c:  box        [mscorlib]System.Int64
    IL_6a81:  ldloc.s    V_14
    IL_6a83:  box        [mscorlib]System.Int64
    IL_6a88:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6a8d:  ldloc.s    V_5
    IL_6a8f:  ldloc.s    V_15
    IL_6a91:  beq.s      IL_6aae

    IL_6a93:  ldc.i4.1
    IL_6a94:  stloc.s    V_20
    IL_6a96:  ldstr      "te164 check failed {0} {1}"
    IL_6a9b:  ldloc.s    V_5
    IL_6a9d:  box        [mscorlib]System.Int64
    IL_6aa2:  ldloc.s    V_15
    IL_6aa4:  box        [mscorlib]System.Int64
    IL_6aa9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6aae:  ldloc.s    V_6
    IL_6ab0:  ldloc.s    V_16
    IL_6ab2:  beq.s      IL_6acf

    IL_6ab4:  ldc.i4.1
    IL_6ab5:  stloc.s    V_20
    IL_6ab7:  ldstr      "te264 check failed {0} {1}"
    IL_6abc:  ldloc.s    V_6
    IL_6abe:  box        [mscorlib]System.Int64
    IL_6ac3:  ldloc.s    V_16
    IL_6ac5:  box        [mscorlib]System.Int64
    IL_6aca:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6acf:  ldloc.s    V_7
    IL_6ad1:  ldloc.s    V_17
    IL_6ad3:  beq.s      IL_6af0

    IL_6ad5:  ldc.i4.1
    IL_6ad6:  stloc.s    V_20
    IL_6ad8:  ldstr      "te364 check failed {0} {1}"
    IL_6add:  ldloc.s    V_7
    IL_6adf:  box        [mscorlib]System.Int64
    IL_6ae4:  ldloc.s    V_17
    IL_6ae6:  box        [mscorlib]System.Int64
    IL_6aeb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6af0:  ldloc.s    V_8
    IL_6af2:  ldloc.s    V_18
    IL_6af4:  beq.s      IL_6b11

    IL_6af6:  ldc.i4.1
    IL_6af7:  stloc.s    V_20
    IL_6af9:  ldstr      "dummy check failed {0} {1}"
    IL_6afe:  ldloc.s    V_8
    IL_6b00:  box        [mscorlib]System.Int32
    IL_6b05:  ldloc.s    V_18
    IL_6b07:  box        [mscorlib]System.Int32
    IL_6b0c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6b11:  ldloc.s    V_9
    IL_6b13:  ldloc.s    V_19
    IL_6b15:  beq.s      IL_6b32

    IL_6b17:  ldc.i4.1
    IL_6b18:  stloc.s    V_20
    IL_6b1a:  ldstr      "dummy64 check failed {0} {1}"
    IL_6b1f:  ldloc.s    V_9
    IL_6b21:  box        [mscorlib]System.Int64
    IL_6b26:  ldloc.s    V_19
    IL_6b28:  box        [mscorlib]System.Int64
    IL_6b2d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6b32:  ldsfld     int32 IntrinsicTest::counter
    IL_6b37:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6b3c:  beq.s      IL_6b5f

    IL_6b3e:  ldstr      "counter mismatch {0} {1}"
    IL_6b43:  ldsfld     int32 IntrinsicTest::counter
    IL_6b48:  box        [mscorlib]System.Int32
    IL_6b4d:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6b52:  box        [mscorlib]System.Int32
    IL_6b57:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6b5c:  ldc.i4.1
    IL_6b5d:  stloc.s    V_20
    IL_6b5f:  ldsfld     int64 IntrinsicTest::counter64
    IL_6b64:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6b69:  beq.s      IL_6b8c

    IL_6b6b:  ldstr      "counter64 mismatch {0} {1}"
    IL_6b70:  ldsfld     int64 IntrinsicTest::counter64
    IL_6b75:  box        [mscorlib]System.Int64
    IL_6b7a:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6b7f:  box        [mscorlib]System.Int64
    IL_6b84:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6b89:  ldc.i4.1
    IL_6b8a:  stloc.s    V_20
    IL_6b8c:  ldloc.s    V_21
    IL_6b8e:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_6b93:  ldloc.s    V_21
    IL_6b95:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_6b9a:  beq.s      IL_6bc1

    IL_6b9c:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_6ba1:  ldloc.s    V_21
    IL_6ba3:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_6ba8:  box        [mscorlib]System.Int32
    IL_6bad:  ldloc.s    V_21
    IL_6baf:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_6bb4:  box        [mscorlib]System.Int32
    IL_6bb9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6bbe:  ldc.i4.1
    IL_6bbf:  stloc.s    V_20
    IL_6bc1:  ldloc.s    V_21
    IL_6bc3:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_6bc8:  ldloc.s    V_21
    IL_6bca:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_6bcf:  beq.s      IL_6bf6

    IL_6bd1:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_6bd6:  ldloc.s    V_21
    IL_6bd8:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_6bdd:  box        [mscorlib]System.Int64
    IL_6be2:  ldloc.s    V_21
    IL_6be4:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_6be9:  box        [mscorlib]System.Int64
    IL_6bee:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6bf3:  ldc.i4.1
    IL_6bf4:  stloc.s    V_20
    IL_6bf6:  ldsfld     int64 IntrinsicTest::temp
    IL_6bfb:  ldsfld     int64 IntrinsicTest::idmp
    IL_6c00:  beq.s      IL_6c23

    IL_6c02:  ldstr      "temp mismatch {0} {1}"
    IL_6c07:  ldsfld     int64 IntrinsicTest::temp
    IL_6c0c:  box        [mscorlib]System.Int64
    IL_6c11:  ldsfld     int64 IntrinsicTest::idmp
    IL_6c16:  box        [mscorlib]System.Int64
    IL_6c1b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6c20:  ldc.i4.1
    IL_6c21:  stloc.s    V_20
    IL_6c23:  ldsflda    int32 IntrinsicTest::counter
    IL_6c28:  ldsfld     int32 IntrinsicTest::counter
    IL_6c2d:  ldloc.2
    IL_6c2e:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_6c33:  stsfld     int32 IntrinsicTest::counter
    IL_6c38:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6c3d:  stsfld     int32 IntrinsicTest::id_counter
    IL_6c42:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6c47:  ldloc.s    V_12
    IL_6c49:  bne.un.s   IL_6c58

    IL_6c4b:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6c50:  conv.i8
    IL_6c51:  stsfld     int64 IntrinsicTest::idjunk
    IL_6c56:  br.s       IL_6c63

    IL_6c58:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6c5d:  conv.i8
    IL_6c5e:  stsfld     int64 IntrinsicTest::idjunk
    IL_6c63:  ldstr      "------------------------------------CMPXCHG  1 1 6 1"
    IL_6c68:  call       void [System.Console]System.Console::WriteLine(string)
    IL_6c6d:  ldloc.0
    IL_6c6e:  ldloc.s    V_10
    IL_6c70:  beq.s      IL_6c8c

    IL_6c72:  ldc.i4.1
    IL_6c73:  stloc.s    V_20
    IL_6c75:  ldstr      "te0 check failed {0} {1}"
    IL_6c7a:  ldloc.0
    IL_6c7b:  box        [mscorlib]System.Int32
    IL_6c80:  ldloc.s    V_10
    IL_6c82:  box        [mscorlib]System.Int32
    IL_6c87:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6c8c:  ldloc.1
    IL_6c8d:  ldloc.s    V_11
    IL_6c8f:  beq.s      IL_6cab

    IL_6c91:  ldc.i4.1
    IL_6c92:  stloc.s    V_20
    IL_6c94:  ldstr      "te1 check failed {0} {1}"
    IL_6c99:  ldloc.1
    IL_6c9a:  box        [mscorlib]System.Int32
    IL_6c9f:  ldloc.s    V_11
    IL_6ca1:  box        [mscorlib]System.Int32
    IL_6ca6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6cab:  ldloc.2
    IL_6cac:  ldloc.s    V_12
    IL_6cae:  beq.s      IL_6cca

    IL_6cb0:  ldc.i4.1
    IL_6cb1:  stloc.s    V_20
    IL_6cb3:  ldstr      "te2 check failed {0} {1}"
    IL_6cb8:  ldloc.2
    IL_6cb9:  box        [mscorlib]System.Int32
    IL_6cbe:  ldloc.s    V_12
    IL_6cc0:  box        [mscorlib]System.Int32
    IL_6cc5:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6cca:  ldloc.3
    IL_6ccb:  ldloc.s    V_13
    IL_6ccd:  beq.s      IL_6ce9

    IL_6ccf:  ldc.i4.1
    IL_6cd0:  stloc.s    V_20
    IL_6cd2:  ldstr      "te3 check failed {0} {1}"
    IL_6cd7:  ldloc.3
    IL_6cd8:  box        [mscorlib]System.Int32
    IL_6cdd:  ldloc.s    V_13
    IL_6cdf:  box        [mscorlib]System.Int32
    IL_6ce4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6ce9:  ldloc.s    V_4
    IL_6ceb:  ldloc.s    V_14
    IL_6ced:  beq.s      IL_6d0a

    IL_6cef:  ldc.i4.1
    IL_6cf0:  stloc.s    V_20
    IL_6cf2:  ldstr      "te064 check failed {0} {1}"
    IL_6cf7:  ldloc.s    V_4
    IL_6cf9:  box        [mscorlib]System.Int64
    IL_6cfe:  ldloc.s    V_14
    IL_6d00:  box        [mscorlib]System.Int64
    IL_6d05:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6d0a:  ldloc.s    V_5
    IL_6d0c:  ldloc.s    V_15
    IL_6d0e:  beq.s      IL_6d2b

    IL_6d10:  ldc.i4.1
    IL_6d11:  stloc.s    V_20
    IL_6d13:  ldstr      "te164 check failed {0} {1}"
    IL_6d18:  ldloc.s    V_5
    IL_6d1a:  box        [mscorlib]System.Int64
    IL_6d1f:  ldloc.s    V_15
    IL_6d21:  box        [mscorlib]System.Int64
    IL_6d26:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6d2b:  ldloc.s    V_6
    IL_6d2d:  ldloc.s    V_16
    IL_6d2f:  beq.s      IL_6d4c

    IL_6d31:  ldc.i4.1
    IL_6d32:  stloc.s    V_20
    IL_6d34:  ldstr      "te264 check failed {0} {1}"
    IL_6d39:  ldloc.s    V_6
    IL_6d3b:  box        [mscorlib]System.Int64
    IL_6d40:  ldloc.s    V_16
    IL_6d42:  box        [mscorlib]System.Int64
    IL_6d47:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6d4c:  ldloc.s    V_7
    IL_6d4e:  ldloc.s    V_17
    IL_6d50:  beq.s      IL_6d6d

    IL_6d52:  ldc.i4.1
    IL_6d53:  stloc.s    V_20
    IL_6d55:  ldstr      "te364 check failed {0} {1}"
    IL_6d5a:  ldloc.s    V_7
    IL_6d5c:  box        [mscorlib]System.Int64
    IL_6d61:  ldloc.s    V_17
    IL_6d63:  box        [mscorlib]System.Int64
    IL_6d68:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6d6d:  ldloc.s    V_8
    IL_6d6f:  ldloc.s    V_18
    IL_6d71:  beq.s      IL_6d8e

    IL_6d73:  ldc.i4.1
    IL_6d74:  stloc.s    V_20
    IL_6d76:  ldstr      "dummy check failed {0} {1}"
    IL_6d7b:  ldloc.s    V_8
    IL_6d7d:  box        [mscorlib]System.Int32
    IL_6d82:  ldloc.s    V_18
    IL_6d84:  box        [mscorlib]System.Int32
    IL_6d89:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6d8e:  ldloc.s    V_9
    IL_6d90:  ldloc.s    V_19
    IL_6d92:  beq.s      IL_6daf

    IL_6d94:  ldc.i4.1
    IL_6d95:  stloc.s    V_20
    IL_6d97:  ldstr      "dummy64 check failed {0} {1}"
    IL_6d9c:  ldloc.s    V_9
    IL_6d9e:  box        [mscorlib]System.Int64
    IL_6da3:  ldloc.s    V_19
    IL_6da5:  box        [mscorlib]System.Int64
    IL_6daa:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6daf:  ldsfld     int32 IntrinsicTest::counter
    IL_6db4:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6db9:  beq.s      IL_6ddc

    IL_6dbb:  ldstr      "counter mismatch {0} {1}"
    IL_6dc0:  ldsfld     int32 IntrinsicTest::counter
    IL_6dc5:  box        [mscorlib]System.Int32
    IL_6dca:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6dcf:  box        [mscorlib]System.Int32
    IL_6dd4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6dd9:  ldc.i4.1
    IL_6dda:  stloc.s    V_20
    IL_6ddc:  ldsfld     int64 IntrinsicTest::counter64
    IL_6de1:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6de6:  beq.s      IL_6e09

    IL_6de8:  ldstr      "counter64 mismatch {0} {1}"
    IL_6ded:  ldsfld     int64 IntrinsicTest::counter64
    IL_6df2:  box        [mscorlib]System.Int64
    IL_6df7:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_6dfc:  box        [mscorlib]System.Int64
    IL_6e01:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6e06:  ldc.i4.1
    IL_6e07:  stloc.s    V_20
    IL_6e09:  ldloc.s    V_21
    IL_6e0b:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_6e10:  ldloc.s    V_21
    IL_6e12:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_6e17:  beq.s      IL_6e3e

    IL_6e19:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_6e1e:  ldloc.s    V_21
    IL_6e20:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_6e25:  box        [mscorlib]System.Int32
    IL_6e2a:  ldloc.s    V_21
    IL_6e2c:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_6e31:  box        [mscorlib]System.Int32
    IL_6e36:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6e3b:  ldc.i4.1
    IL_6e3c:  stloc.s    V_20
    IL_6e3e:  ldloc.s    V_21
    IL_6e40:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_6e45:  ldloc.s    V_21
    IL_6e47:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_6e4c:  beq.s      IL_6e73

    IL_6e4e:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_6e53:  ldloc.s    V_21
    IL_6e55:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_6e5a:  box        [mscorlib]System.Int64
    IL_6e5f:  ldloc.s    V_21
    IL_6e61:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_6e66:  box        [mscorlib]System.Int64
    IL_6e6b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6e70:  ldc.i4.1
    IL_6e71:  stloc.s    V_20
    IL_6e73:  ldsfld     int64 IntrinsicTest::temp
    IL_6e78:  ldsfld     int64 IntrinsicTest::idmp
    IL_6e7d:  beq.s      IL_6ea0

    IL_6e7f:  ldstr      "temp mismatch {0} {1}"
    IL_6e84:  ldsfld     int64 IntrinsicTest::temp
    IL_6e89:  box        [mscorlib]System.Int64
    IL_6e8e:  ldsfld     int64 IntrinsicTest::idmp
    IL_6e93:  box        [mscorlib]System.Int64
    IL_6e98:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6e9d:  ldc.i4.1
    IL_6e9e:  stloc.s    V_20
    IL_6ea0:  ldloc.s    V_21
    IL_6ea2:  ldsflda    int32 IntrinsicTest::counter
    IL_6ea7:  ldsfld     int32 IntrinsicTest::counter
    IL_6eac:  ldloc.2
    IL_6ead:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_6eb2:  stfld      int32 IntrinsicTest::instanceCounter
    IL_6eb7:  ldloc.s    V_21
    IL_6eb9:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6ebe:  stfld      int32 IntrinsicTest::id_instanceCounter
    IL_6ec3:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6ec8:  ldloc.s    V_12
    IL_6eca:  bne.un.s   IL_6ed8

    IL_6ecc:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6ed1:  stsfld     int32 IntrinsicTest::id_counter
    IL_6ed6:  br.s       IL_6ee3

    IL_6ed8:  ldsfld     int32 IntrinsicTest::id_counter
    IL_6edd:  conv.i8
    IL_6ede:  stsfld     int64 IntrinsicTest::idjunk
    IL_6ee3:  ldstr      "------------------------------------CMPXCHG  1 1 6 2"
    IL_6ee8:  call       void [System.Console]System.Console::WriteLine(string)
    IL_6eed:  ldloc.0
    IL_6eee:  ldloc.s    V_10
    IL_6ef0:  beq.s      IL_6f0c

    IL_6ef2:  ldc.i4.1
    IL_6ef3:  stloc.s    V_20
    IL_6ef5:  ldstr      "te0 check failed {0} {1}"
    IL_6efa:  ldloc.0
    IL_6efb:  box        [mscorlib]System.Int32
    IL_6f00:  ldloc.s    V_10
    IL_6f02:  box        [mscorlib]System.Int32
    IL_6f07:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6f0c:  ldloc.1
    IL_6f0d:  ldloc.s    V_11
    IL_6f0f:  beq.s      IL_6f2b

    IL_6f11:  ldc.i4.1
    IL_6f12:  stloc.s    V_20
    IL_6f14:  ldstr      "te1 check failed {0} {1}"
    IL_6f19:  ldloc.1
    IL_6f1a:  box        [mscorlib]System.Int32
    IL_6f1f:  ldloc.s    V_11
    IL_6f21:  box        [mscorlib]System.Int32
    IL_6f26:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6f2b:  ldloc.2
    IL_6f2c:  ldloc.s    V_12
    IL_6f2e:  beq.s      IL_6f4a

    IL_6f30:  ldc.i4.1
    IL_6f31:  stloc.s    V_20
    IL_6f33:  ldstr      "te2 check failed {0} {1}"
    IL_6f38:  ldloc.2
    IL_6f39:  box        [mscorlib]System.Int32
    IL_6f3e:  ldloc.s    V_12
    IL_6f40:  box        [mscorlib]System.Int32
    IL_6f45:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6f4a:  ldloc.3
    IL_6f4b:  ldloc.s    V_13
    IL_6f4d:  beq.s      IL_6f69

    IL_6f4f:  ldc.i4.1
    IL_6f50:  stloc.s    V_20
    IL_6f52:  ldstr      "te3 check failed {0} {1}"
    IL_6f57:  ldloc.3
    IL_6f58:  box        [mscorlib]System.Int32
    IL_6f5d:  ldloc.s    V_13
    IL_6f5f:  box        [mscorlib]System.Int32
    IL_6f64:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6f69:  ldloc.s    V_4
    IL_6f6b:  ldloc.s    V_14
    IL_6f6d:  beq.s      IL_6f8a

    IL_6f6f:  ldc.i4.1
    IL_6f70:  stloc.s    V_20
    IL_6f72:  ldstr      "te064 check failed {0} {1}"
    IL_6f77:  ldloc.s    V_4
    IL_6f79:  box        [mscorlib]System.Int64
    IL_6f7e:  ldloc.s    V_14
    IL_6f80:  box        [mscorlib]System.Int64
    IL_6f85:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6f8a:  ldloc.s    V_5
    IL_6f8c:  ldloc.s    V_15
    IL_6f8e:  beq.s      IL_6fab

    IL_6f90:  ldc.i4.1
    IL_6f91:  stloc.s    V_20
    IL_6f93:  ldstr      "te164 check failed {0} {1}"
    IL_6f98:  ldloc.s    V_5
    IL_6f9a:  box        [mscorlib]System.Int64
    IL_6f9f:  ldloc.s    V_15
    IL_6fa1:  box        [mscorlib]System.Int64
    IL_6fa6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6fab:  ldloc.s    V_6
    IL_6fad:  ldloc.s    V_16
    IL_6faf:  beq.s      IL_6fcc

    IL_6fb1:  ldc.i4.1
    IL_6fb2:  stloc.s    V_20
    IL_6fb4:  ldstr      "te264 check failed {0} {1}"
    IL_6fb9:  ldloc.s    V_6
    IL_6fbb:  box        [mscorlib]System.Int64
    IL_6fc0:  ldloc.s    V_16
    IL_6fc2:  box        [mscorlib]System.Int64
    IL_6fc7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6fcc:  ldloc.s    V_7
    IL_6fce:  ldloc.s    V_17
    IL_6fd0:  beq.s      IL_6fed

    IL_6fd2:  ldc.i4.1
    IL_6fd3:  stloc.s    V_20
    IL_6fd5:  ldstr      "te364 check failed {0} {1}"
    IL_6fda:  ldloc.s    V_7
    IL_6fdc:  box        [mscorlib]System.Int64
    IL_6fe1:  ldloc.s    V_17
    IL_6fe3:  box        [mscorlib]System.Int64
    IL_6fe8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_6fed:  ldloc.s    V_8
    IL_6fef:  ldloc.s    V_18
    IL_6ff1:  beq.s      IL_700e

    IL_6ff3:  ldc.i4.1
    IL_6ff4:  stloc.s    V_20
    IL_6ff6:  ldstr      "dummy check failed {0} {1}"
    IL_6ffb:  ldloc.s    V_8
    IL_6ffd:  box        [mscorlib]System.Int32
    IL_7002:  ldloc.s    V_18
    IL_7004:  box        [mscorlib]System.Int32
    IL_7009:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_700e:  ldloc.s    V_9
    IL_7010:  ldloc.s    V_19
    IL_7012:  beq.s      IL_702f

    IL_7014:  ldc.i4.1
    IL_7015:  stloc.s    V_20
    IL_7017:  ldstr      "dummy64 check failed {0} {1}"
    IL_701c:  ldloc.s    V_9
    IL_701e:  box        [mscorlib]System.Int64
    IL_7023:  ldloc.s    V_19
    IL_7025:  box        [mscorlib]System.Int64
    IL_702a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_702f:  ldsfld     int32 IntrinsicTest::counter
    IL_7034:  ldsfld     int32 IntrinsicTest::id_counter
    IL_7039:  beq.s      IL_705c

    IL_703b:  ldstr      "counter mismatch {0} {1}"
    IL_7040:  ldsfld     int32 IntrinsicTest::counter
    IL_7045:  box        [mscorlib]System.Int32
    IL_704a:  ldsfld     int32 IntrinsicTest::id_counter
    IL_704f:  box        [mscorlib]System.Int32
    IL_7054:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7059:  ldc.i4.1
    IL_705a:  stloc.s    V_20
    IL_705c:  ldsfld     int64 IntrinsicTest::counter64
    IL_7061:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_7066:  beq.s      IL_7089

    IL_7068:  ldstr      "counter64 mismatch {0} {1}"
    IL_706d:  ldsfld     int64 IntrinsicTest::counter64
    IL_7072:  box        [mscorlib]System.Int64
    IL_7077:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_707c:  box        [mscorlib]System.Int64
    IL_7081:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7086:  ldc.i4.1
    IL_7087:  stloc.s    V_20
    IL_7089:  ldloc.s    V_21
    IL_708b:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_7090:  ldloc.s    V_21
    IL_7092:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_7097:  beq.s      IL_70be

    IL_7099:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_709e:  ldloc.s    V_21
    IL_70a0:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_70a5:  box        [mscorlib]System.Int32
    IL_70aa:  ldloc.s    V_21
    IL_70ac:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_70b1:  box        [mscorlib]System.Int32
    IL_70b6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_70bb:  ldc.i4.1
    IL_70bc:  stloc.s    V_20
    IL_70be:  ldloc.s    V_21
    IL_70c0:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_70c5:  ldloc.s    V_21
    IL_70c7:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_70cc:  beq.s      IL_70f3

    IL_70ce:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_70d3:  ldloc.s    V_21
    IL_70d5:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_70da:  box        [mscorlib]System.Int64
    IL_70df:  ldloc.s    V_21
    IL_70e1:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_70e6:  box        [mscorlib]System.Int64
    IL_70eb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_70f0:  ldc.i4.1
    IL_70f1:  stloc.s    V_20
    IL_70f3:  ldsfld     int64 IntrinsicTest::temp
    IL_70f8:  ldsfld     int64 IntrinsicTest::idmp
    IL_70fd:  beq.s      IL_7120

    IL_70ff:  ldstr      "temp mismatch {0} {1}"
    IL_7104:  ldsfld     int64 IntrinsicTest::temp
    IL_7109:  box        [mscorlib]System.Int64
    IL_710e:  ldsfld     int64 IntrinsicTest::idmp
    IL_7113:  box        [mscorlib]System.Int64
    IL_7118:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_711d:  ldc.i4.1
    IL_711e:  stloc.s    V_20
    IL_7120:  ldsflda    int32 IntrinsicTest::counter
    IL_7125:  ldsfld     int32 IntrinsicTest::counter
    IL_712a:  ldloc.2
    IL_712b:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_7130:  stloc.0
    IL_7131:  ldsfld     int32 IntrinsicTest::id_counter
    IL_7136:  stloc.s    V_10
    IL_7138:  ldsfld     int32 IntrinsicTest::id_counter
    IL_713d:  ldloc.s    V_12
    IL_713f:  bne.un.s   IL_714d

    IL_7141:  ldsfld     int32 IntrinsicTest::id_counter
    IL_7146:  stsfld     int32 IntrinsicTest::id_counter
    IL_714b:  br.s       IL_7158

    IL_714d:  ldsfld     int32 IntrinsicTest::id_counter
    IL_7152:  conv.i8
    IL_7153:  stsfld     int64 IntrinsicTest::idjunk
    IL_7158:  ldstr      "------------------------------------CMPXCHG  1 1 6 3"
    IL_715d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_7162:  ldloc.0
    IL_7163:  ldloc.s    V_10
    IL_7165:  beq.s      IL_7181

    IL_7167:  ldc.i4.1
    IL_7168:  stloc.s    V_20
    IL_716a:  ldstr      "te0 check failed {0} {1}"
    IL_716f:  ldloc.0
    IL_7170:  box        [mscorlib]System.Int32
    IL_7175:  ldloc.s    V_10
    IL_7177:  box        [mscorlib]System.Int32
    IL_717c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7181:  ldloc.1
    IL_7182:  ldloc.s    V_11
    IL_7184:  beq.s      IL_71a0

    IL_7186:  ldc.i4.1
    IL_7187:  stloc.s    V_20
    IL_7189:  ldstr      "te1 check failed {0} {1}"
    IL_718e:  ldloc.1
    IL_718f:  box        [mscorlib]System.Int32
    IL_7194:  ldloc.s    V_11
    IL_7196:  box        [mscorlib]System.Int32
    IL_719b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_71a0:  ldloc.2
    IL_71a1:  ldloc.s    V_12
    IL_71a3:  beq.s      IL_71bf

    IL_71a5:  ldc.i4.1
    IL_71a6:  stloc.s    V_20
    IL_71a8:  ldstr      "te2 check failed {0} {1}"
    IL_71ad:  ldloc.2
    IL_71ae:  box        [mscorlib]System.Int32
    IL_71b3:  ldloc.s    V_12
    IL_71b5:  box        [mscorlib]System.Int32
    IL_71ba:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_71bf:  ldloc.3
    IL_71c0:  ldloc.s    V_13
    IL_71c2:  beq.s      IL_71de

    IL_71c4:  ldc.i4.1
    IL_71c5:  stloc.s    V_20
    IL_71c7:  ldstr      "te3 check failed {0} {1}"
    IL_71cc:  ldloc.3
    IL_71cd:  box        [mscorlib]System.Int32
    IL_71d2:  ldloc.s    V_13
    IL_71d4:  box        [mscorlib]System.Int32
    IL_71d9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_71de:  ldloc.s    V_4
    IL_71e0:  ldloc.s    V_14
    IL_71e2:  beq.s      IL_71ff

    IL_71e4:  ldc.i4.1
    IL_71e5:  stloc.s    V_20
    IL_71e7:  ldstr      "te064 check failed {0} {1}"
    IL_71ec:  ldloc.s    V_4
    IL_71ee:  box        [mscorlib]System.Int64
    IL_71f3:  ldloc.s    V_14
    IL_71f5:  box        [mscorlib]System.Int64
    IL_71fa:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_71ff:  ldloc.s    V_5
    IL_7201:  ldloc.s    V_15
    IL_7203:  beq.s      IL_7220

    IL_7205:  ldc.i4.1
    IL_7206:  stloc.s    V_20
    IL_7208:  ldstr      "te164 check failed {0} {1}"
    IL_720d:  ldloc.s    V_5
    IL_720f:  box        [mscorlib]System.Int64
    IL_7214:  ldloc.s    V_15
    IL_7216:  box        [mscorlib]System.Int64
    IL_721b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7220:  ldloc.s    V_6
    IL_7222:  ldloc.s    V_16
    IL_7224:  beq.s      IL_7241

    IL_7226:  ldc.i4.1
    IL_7227:  stloc.s    V_20
    IL_7229:  ldstr      "te264 check failed {0} {1}"
    IL_722e:  ldloc.s    V_6
    IL_7230:  box        [mscorlib]System.Int64
    IL_7235:  ldloc.s    V_16
    IL_7237:  box        [mscorlib]System.Int64
    IL_723c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7241:  ldloc.s    V_7
    IL_7243:  ldloc.s    V_17
    IL_7245:  beq.s      IL_7262

    IL_7247:  ldc.i4.1
    IL_7248:  stloc.s    V_20
    IL_724a:  ldstr      "te364 check failed {0} {1}"
    IL_724f:  ldloc.s    V_7
    IL_7251:  box        [mscorlib]System.Int64
    IL_7256:  ldloc.s    V_17
    IL_7258:  box        [mscorlib]System.Int64
    IL_725d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7262:  ldloc.s    V_8
    IL_7264:  ldloc.s    V_18
    IL_7266:  beq.s      IL_7283

    IL_7268:  ldc.i4.1
    IL_7269:  stloc.s    V_20
    IL_726b:  ldstr      "dummy check failed {0} {1}"
    IL_7270:  ldloc.s    V_8
    IL_7272:  box        [mscorlib]System.Int32
    IL_7277:  ldloc.s    V_18
    IL_7279:  box        [mscorlib]System.Int32
    IL_727e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7283:  ldloc.s    V_9
    IL_7285:  ldloc.s    V_19
    IL_7287:  beq.s      IL_72a4

    IL_7289:  ldc.i4.1
    IL_728a:  stloc.s    V_20
    IL_728c:  ldstr      "dummy64 check failed {0} {1}"
    IL_7291:  ldloc.s    V_9
    IL_7293:  box        [mscorlib]System.Int64
    IL_7298:  ldloc.s    V_19
    IL_729a:  box        [mscorlib]System.Int64
    IL_729f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_72a4:  ldsfld     int32 IntrinsicTest::counter
    IL_72a9:  ldsfld     int32 IntrinsicTest::id_counter
    IL_72ae:  beq.s      IL_72d1

    IL_72b0:  ldstr      "counter mismatch {0} {1}"
    IL_72b5:  ldsfld     int32 IntrinsicTest::counter
    IL_72ba:  box        [mscorlib]System.Int32
    IL_72bf:  ldsfld     int32 IntrinsicTest::id_counter
    IL_72c4:  box        [mscorlib]System.Int32
    IL_72c9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_72ce:  ldc.i4.1
    IL_72cf:  stloc.s    V_20
    IL_72d1:  ldsfld     int64 IntrinsicTest::counter64
    IL_72d6:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_72db:  beq.s      IL_72fe

    IL_72dd:  ldstr      "counter64 mismatch {0} {1}"
    IL_72e2:  ldsfld     int64 IntrinsicTest::counter64
    IL_72e7:  box        [mscorlib]System.Int64
    IL_72ec:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_72f1:  box        [mscorlib]System.Int64
    IL_72f6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_72fb:  ldc.i4.1
    IL_72fc:  stloc.s    V_20
    IL_72fe:  ldloc.s    V_21
    IL_7300:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_7305:  ldloc.s    V_21
    IL_7307:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_730c:  beq.s      IL_7333

    IL_730e:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_7313:  ldloc.s    V_21
    IL_7315:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_731a:  box        [mscorlib]System.Int32
    IL_731f:  ldloc.s    V_21
    IL_7321:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_7326:  box        [mscorlib]System.Int32
    IL_732b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7330:  ldc.i4.1
    IL_7331:  stloc.s    V_20
    IL_7333:  ldloc.s    V_21
    IL_7335:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_733a:  ldloc.s    V_21
    IL_733c:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_7341:  beq.s      IL_7368

    IL_7343:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_7348:  ldloc.s    V_21
    IL_734a:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_734f:  box        [mscorlib]System.Int64
    IL_7354:  ldloc.s    V_21
    IL_7356:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_735b:  box        [mscorlib]System.Int64
    IL_7360:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7365:  ldc.i4.1
    IL_7366:  stloc.s    V_20
    IL_7368:  ldsfld     int64 IntrinsicTest::temp
    IL_736d:  ldsfld     int64 IntrinsicTest::idmp
    IL_7372:  beq.s      IL_7395

    IL_7374:  ldstr      "temp mismatch {0} {1}"
    IL_7379:  ldsfld     int64 IntrinsicTest::temp
    IL_737e:  box        [mscorlib]System.Int64
    IL_7383:  ldsfld     int64 IntrinsicTest::idmp
    IL_7388:  box        [mscorlib]System.Int64
    IL_738d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7392:  ldc.i4.1
    IL_7393:  stloc.s    V_20
    IL_7395:  ldsflda    int32 IntrinsicTest::counter
    IL_739a:  ldsfld     int32 IntrinsicTest::counter
    IL_739f:  ldloc.2
    IL_73a0:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_73a5:  stsfld     int32 IntrinsicTest::counter
    IL_73aa:  ldsfld     int32 IntrinsicTest::id_counter
    IL_73af:  stsfld     int32 IntrinsicTest::id_counter
    IL_73b4:  ldsfld     int32 IntrinsicTest::id_counter
    IL_73b9:  ldloc.s    V_12
    IL_73bb:  bne.un.s   IL_73ca

    IL_73bd:  ldsfld     int32 IntrinsicTest::id_counter
    IL_73c2:  conv.i8
    IL_73c3:  stsfld     int64 IntrinsicTest::idjunk
    IL_73c8:  br.s       IL_73d5

    IL_73ca:  ldsfld     int32 IntrinsicTest::id_counter
    IL_73cf:  conv.i8
    IL_73d0:  stsfld     int64 IntrinsicTest::idjunk
    IL_73d5:  ldstr      "------------------------------------CMPXCHG  1 1 6 4"
    IL_73da:  call       void [System.Console]System.Console::WriteLine(string)
    IL_73df:  ldloc.0
    IL_73e0:  ldloc.s    V_10
    IL_73e2:  beq.s      IL_73fe

    IL_73e4:  ldc.i4.1
    IL_73e5:  stloc.s    V_20
    IL_73e7:  ldstr      "te0 check failed {0} {1}"
    IL_73ec:  ldloc.0
    IL_73ed:  box        [mscorlib]System.Int32
    IL_73f2:  ldloc.s    V_10
    IL_73f4:  box        [mscorlib]System.Int32
    IL_73f9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_73fe:  ldloc.1
    IL_73ff:  ldloc.s    V_11
    IL_7401:  beq.s      IL_741d

    IL_7403:  ldc.i4.1
    IL_7404:  stloc.s    V_20
    IL_7406:  ldstr      "te1 check failed {0} {1}"
    IL_740b:  ldloc.1
    IL_740c:  box        [mscorlib]System.Int32
    IL_7411:  ldloc.s    V_11
    IL_7413:  box        [mscorlib]System.Int32
    IL_7418:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_741d:  ldloc.2
    IL_741e:  ldloc.s    V_12
    IL_7420:  beq.s      IL_743c

    IL_7422:  ldc.i4.1
    IL_7423:  stloc.s    V_20
    IL_7425:  ldstr      "te2 check failed {0} {1}"
    IL_742a:  ldloc.2
    IL_742b:  box        [mscorlib]System.Int32
    IL_7430:  ldloc.s    V_12
    IL_7432:  box        [mscorlib]System.Int32
    IL_7437:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_743c:  ldloc.3
    IL_743d:  ldloc.s    V_13
    IL_743f:  beq.s      IL_745b

    IL_7441:  ldc.i4.1
    IL_7442:  stloc.s    V_20
    IL_7444:  ldstr      "te3 check failed {0} {1}"
    IL_7449:  ldloc.3
    IL_744a:  box        [mscorlib]System.Int32
    IL_744f:  ldloc.s    V_13
    IL_7451:  box        [mscorlib]System.Int32
    IL_7456:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_745b:  ldloc.s    V_4
    IL_745d:  ldloc.s    V_14
    IL_745f:  beq.s      IL_747c

    IL_7461:  ldc.i4.1
    IL_7462:  stloc.s    V_20
    IL_7464:  ldstr      "te064 check failed {0} {1}"
    IL_7469:  ldloc.s    V_4
    IL_746b:  box        [mscorlib]System.Int64
    IL_7470:  ldloc.s    V_14
    IL_7472:  box        [mscorlib]System.Int64
    IL_7477:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_747c:  ldloc.s    V_5
    IL_747e:  ldloc.s    V_15
    IL_7480:  beq.s      IL_749d

    IL_7482:  ldc.i4.1
    IL_7483:  stloc.s    V_20
    IL_7485:  ldstr      "te164 check failed {0} {1}"
    IL_748a:  ldloc.s    V_5
    IL_748c:  box        [mscorlib]System.Int64
    IL_7491:  ldloc.s    V_15
    IL_7493:  box        [mscorlib]System.Int64
    IL_7498:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_749d:  ldloc.s    V_6
    IL_749f:  ldloc.s    V_16
    IL_74a1:  beq.s      IL_74be

    IL_74a3:  ldc.i4.1
    IL_74a4:  stloc.s    V_20
    IL_74a6:  ldstr      "te264 check failed {0} {1}"
    IL_74ab:  ldloc.s    V_6
    IL_74ad:  box        [mscorlib]System.Int64
    IL_74b2:  ldloc.s    V_16
    IL_74b4:  box        [mscorlib]System.Int64
    IL_74b9:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_74be:  ldloc.s    V_7
    IL_74c0:  ldloc.s    V_17
    IL_74c2:  beq.s      IL_74df

    IL_74c4:  ldc.i4.1
    IL_74c5:  stloc.s    V_20
    IL_74c7:  ldstr      "te364 check failed {0} {1}"
    IL_74cc:  ldloc.s    V_7
    IL_74ce:  box        [mscorlib]System.Int64
    IL_74d3:  ldloc.s    V_17
    IL_74d5:  box        [mscorlib]System.Int64
    IL_74da:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_74df:  ldloc.s    V_8
    IL_74e1:  ldloc.s    V_18
    IL_74e3:  beq.s      IL_7500

    IL_74e5:  ldc.i4.1
    IL_74e6:  stloc.s    V_20
    IL_74e8:  ldstr      "dummy check failed {0} {1}"
    IL_74ed:  ldloc.s    V_8
    IL_74ef:  box        [mscorlib]System.Int32
    IL_74f4:  ldloc.s    V_18
    IL_74f6:  box        [mscorlib]System.Int32
    IL_74fb:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7500:  ldloc.s    V_9
    IL_7502:  ldloc.s    V_19
    IL_7504:  beq.s      IL_7521

    IL_7506:  ldc.i4.1
    IL_7507:  stloc.s    V_20
    IL_7509:  ldstr      "dummy64 check failed {0} {1}"
    IL_750e:  ldloc.s    V_9
    IL_7510:  box        [mscorlib]System.Int64
    IL_7515:  ldloc.s    V_19
    IL_7517:  box        [mscorlib]System.Int64
    IL_751c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7521:  ldsfld     int32 IntrinsicTest::counter
    IL_7526:  ldsfld     int32 IntrinsicTest::id_counter
    IL_752b:  beq.s      IL_754e

    IL_752d:  ldstr      "counter mismatch {0} {1}"
    IL_7532:  ldsfld     int32 IntrinsicTest::counter
    IL_7537:  box        [mscorlib]System.Int32
    IL_753c:  ldsfld     int32 IntrinsicTest::id_counter
    IL_7541:  box        [mscorlib]System.Int32
    IL_7546:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_754b:  ldc.i4.1
    IL_754c:  stloc.s    V_20
    IL_754e:  ldsfld     int64 IntrinsicTest::counter64
    IL_7553:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_7558:  beq.s      IL_757b

    IL_755a:  ldstr      "counter64 mismatch {0} {1}"
    IL_755f:  ldsfld     int64 IntrinsicTest::counter64
    IL_7564:  box        [mscorlib]System.Int64
    IL_7569:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_756e:  box        [mscorlib]System.Int64
    IL_7573:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7578:  ldc.i4.1
    IL_7579:  stloc.s    V_20
    IL_757b:  ldloc.s    V_21
    IL_757d:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_7582:  ldloc.s    V_21
    IL_7584:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_7589:  beq.s      IL_75b0

    IL_758b:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_7590:  ldloc.s    V_21
    IL_7592:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_7597:  box        [mscorlib]System.Int32
    IL_759c:  ldloc.s    V_21
    IL_759e:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_75a3:  box        [mscorlib]System.Int32
    IL_75a8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_75ad:  ldc.i4.1
    IL_75ae:  stloc.s    V_20
    IL_75b0:  ldloc.s    V_21
    IL_75b2:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_75b7:  ldloc.s    V_21
    IL_75b9:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_75be:  beq.s      IL_75e5

    IL_75c0:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_75c5:  ldloc.s    V_21
    IL_75c7:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_75cc:  box        [mscorlib]System.Int64
    IL_75d1:  ldloc.s    V_21
    IL_75d3:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_75d8:  box        [mscorlib]System.Int64
    IL_75dd:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_75e2:  ldc.i4.1
    IL_75e3:  stloc.s    V_20
    IL_75e5:  ldsfld     int64 IntrinsicTest::temp
    IL_75ea:  ldsfld     int64 IntrinsicTest::idmp
    IL_75ef:  beq.s      IL_7612

    IL_75f1:  ldstr      "temp mismatch {0} {1}"
    IL_75f6:  ldsfld     int64 IntrinsicTest::temp
    IL_75fb:  box        [mscorlib]System.Int64
    IL_7600:  ldsfld     int64 IntrinsicTest::idmp
    IL_7605:  box        [mscorlib]System.Int64
    IL_760a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_760f:  ldc.i4.1
    IL_7610:  stloc.s    V_20
    IL_7612:  ldloc.s    V_21
    IL_7614:  ldsflda    int32 IntrinsicTest::counter
    IL_7619:  ldsfld     int32 IntrinsicTest::counter
    IL_761e:  ldloc.2
    IL_761f:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_7624:  stfld      int32 IntrinsicTest::instanceCounter
    IL_7629:  ldloc.s    V_21
    IL_762b:  ldsfld     int32 IntrinsicTest::id_counter
    IL_7630:  stfld      int32 IntrinsicTest::id_instanceCounter
    IL_7635:  ldsfld     int32 IntrinsicTest::id_counter
    IL_763a:  ldloc.s    V_12
    IL_763c:  bne.un.s   IL_764a

    IL_763e:  ldsfld     int32 IntrinsicTest::id_counter
    IL_7643:  stsfld     int32 IntrinsicTest::id_counter
    IL_7648:  br.s       IL_7655

    IL_764a:  ldsfld     int32 IntrinsicTest::id_counter
    IL_764f:  conv.i8
    IL_7650:  stsfld     int64 IntrinsicTest::idjunk
    IL_7655:  ldstr      "------------------------------------CMPXCHG  1 1 6 5"
    IL_765a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_765f:  ldloc.0
    IL_7660:  ldloc.s    V_10
    IL_7662:  beq.s      IL_767e

    IL_7664:  ldc.i4.1
    IL_7665:  stloc.s    V_20
    IL_7667:  ldstr      "te0 check failed {0} {1}"
    IL_766c:  ldloc.0
    IL_766d:  box        [mscorlib]System.Int32
    IL_7672:  ldloc.s    V_10
    IL_7674:  box        [mscorlib]System.Int32
    IL_7679:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_767e:  ldloc.1
    IL_767f:  ldloc.s    V_11
    IL_7681:  beq.s      IL_769d

    IL_7683:  ldc.i4.1
    IL_7684:  stloc.s    V_20
    IL_7686:  ldstr      "te1 check failed {0} {1}"
    IL_768b:  ldloc.1
    IL_768c:  box        [mscorlib]System.Int32
    IL_7691:  ldloc.s    V_11
    IL_7693:  box        [mscorlib]System.Int32
    IL_7698:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_769d:  ldloc.2
    IL_769e:  ldloc.s    V_12
    IL_76a0:  beq.s      IL_76bc

    IL_76a2:  ldc.i4.1
    IL_76a3:  stloc.s    V_20
    IL_76a5:  ldstr      "te2 check failed {0} {1}"
    IL_76aa:  ldloc.2
    IL_76ab:  box        [mscorlib]System.Int32
    IL_76b0:  ldloc.s    V_12
    IL_76b2:  box        [mscorlib]System.Int32
    IL_76b7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_76bc:  ldloc.3
    IL_76bd:  ldloc.s    V_13
    IL_76bf:  beq.s      IL_76db

    IL_76c1:  ldc.i4.1
    IL_76c2:  stloc.s    V_20
    IL_76c4:  ldstr      "te3 check failed {0} {1}"
    IL_76c9:  ldloc.3
    IL_76ca:  box        [mscorlib]System.Int32
    IL_76cf:  ldloc.s    V_13
    IL_76d1:  box        [mscorlib]System.Int32
    IL_76d6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_76db:  ldloc.s    V_4
    IL_76dd:  ldloc.s    V_14
    IL_76df:  beq.s      IL_76fc

    IL_76e1:  ldc.i4.1
    IL_76e2:  stloc.s    V_20
    IL_76e4:  ldstr      "te064 check failed {0} {1}"
    IL_76e9:  ldloc.s    V_4
    IL_76eb:  box        [mscorlib]System.Int64
    IL_76f0:  ldloc.s    V_14
    IL_76f2:  box        [mscorlib]System.Int64
    IL_76f7:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_76fc:  ldloc.s    V_5
    IL_76fe:  ldloc.s    V_15
    IL_7700:  beq.s      IL_771d

    IL_7702:  ldc.i4.1
    IL_7703:  stloc.s    V_20
    IL_7705:  ldstr      "te164 check failed {0} {1}"
    IL_770a:  ldloc.s    V_5
    IL_770c:  box        [mscorlib]System.Int64
    IL_7711:  ldloc.s    V_15
    IL_7713:  box        [mscorlib]System.Int64
    IL_7718:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_771d:  ldloc.s    V_6
    IL_771f:  ldloc.s    V_16
    IL_7721:  beq.s      IL_773e

    IL_7723:  ldc.i4.1
    IL_7724:  stloc.s    V_20
    IL_7726:  ldstr      "te264 check failed {0} {1}"
    IL_772b:  ldloc.s    V_6
    IL_772d:  box        [mscorlib]System.Int64
    IL_7732:  ldloc.s    V_16
    IL_7734:  box        [mscorlib]System.Int64
    IL_7739:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_773e:  ldloc.s    V_7
    IL_7740:  ldloc.s    V_17
    IL_7742:  beq.s      IL_775f

    IL_7744:  ldc.i4.1
    IL_7745:  stloc.s    V_20
    IL_7747:  ldstr      "te364 check failed {0} {1}"
    IL_774c:  ldloc.s    V_7
    IL_774e:  box        [mscorlib]System.Int64
    IL_7753:  ldloc.s    V_17
    IL_7755:  box        [mscorlib]System.Int64
    IL_775a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_775f:  ldloc.s    V_8
    IL_7761:  ldloc.s    V_18
    IL_7763:  beq.s      IL_7780

    IL_7765:  ldc.i4.1
    IL_7766:  stloc.s    V_20
    IL_7768:  ldstr      "dummy check failed {0} {1}"
    IL_776d:  ldloc.s    V_8
    IL_776f:  box        [mscorlib]System.Int32
    IL_7774:  ldloc.s    V_18
    IL_7776:  box        [mscorlib]System.Int32
    IL_777b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7780:  ldloc.s    V_9
    IL_7782:  ldloc.s    V_19
    IL_7784:  beq.s      IL_77a1

    IL_7786:  ldc.i4.1
    IL_7787:  stloc.s    V_20
    IL_7789:  ldstr      "dummy64 check failed {0} {1}"
    IL_778e:  ldloc.s    V_9
    IL_7790:  box        [mscorlib]System.Int64
    IL_7795:  ldloc.s    V_19
    IL_7797:  box        [mscorlib]System.Int64
    IL_779c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_77a1:  ldsfld     int32 IntrinsicTest::counter
    IL_77a6:  ldsfld     int32 IntrinsicTest::id_counter
    IL_77ab:  beq.s      IL_77ce

    IL_77ad:  ldstr      "counter mismatch {0} {1}"
    IL_77b2:  ldsfld     int32 IntrinsicTest::counter
    IL_77b7:  box        [mscorlib]System.Int32
    IL_77bc:  ldsfld     int32 IntrinsicTest::id_counter
    IL_77c1:  box        [mscorlib]System.Int32
    IL_77c6:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_77cb:  ldc.i4.1
    IL_77cc:  stloc.s    V_20
    IL_77ce:  ldsfld     int64 IntrinsicTest::counter64
    IL_77d3:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_77d8:  beq.s      IL_77fb

    IL_77da:  ldstr      "counter64 mismatch {0} {1}"
    IL_77df:  ldsfld     int64 IntrinsicTest::counter64
    IL_77e4:  box        [mscorlib]System.Int64
    IL_77e9:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_77ee:  box        [mscorlib]System.Int64
    IL_77f3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_77f8:  ldc.i4.1
    IL_77f9:  stloc.s    V_20
    IL_77fb:  ldloc.s    V_21
    IL_77fd:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_7802:  ldloc.s    V_21
    IL_7804:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_7809:  beq.s      IL_7830

    IL_780b:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_7810:  ldloc.s    V_21
    IL_7812:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_7817:  box        [mscorlib]System.Int32
    IL_781c:  ldloc.s    V_21
    IL_781e:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_7823:  box        [mscorlib]System.Int32
    IL_7828:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_782d:  ldc.i4.1
    IL_782e:  stloc.s    V_20
    IL_7830:  ldloc.s    V_21
    IL_7832:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_7837:  ldloc.s    V_21
    IL_7839:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_783e:  beq.s      IL_7865

    IL_7840:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_7845:  ldloc.s    V_21
    IL_7847:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_784c:  box        [mscorlib]System.Int64
    IL_7851:  ldloc.s    V_21
    IL_7853:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_7858:  box        [mscorlib]System.Int64
    IL_785d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7862:  ldc.i4.1
    IL_7863:  stloc.s    V_20
    IL_7865:  ldsfld     int64 IntrinsicTest::temp
    IL_786a:  ldsfld     int64 IntrinsicTest::idmp
    IL_786f:  beq.s      IL_7892

    IL_7871:  ldstr      "temp mismatch {0} {1}"
    IL_7876:  ldsfld     int64 IntrinsicTest::temp
    IL_787b:  box        [mscorlib]System.Int64
    IL_7880:  ldsfld     int64 IntrinsicTest::idmp
    IL_7885:  box        [mscorlib]System.Int64
    IL_788a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_788f:  ldc.i4.1
    IL_7890:  stloc.s    V_20
    IL_7892:  ldsflda    int32 IntrinsicTest::counter
    IL_7897:  ldsfld     int32 IntrinsicTest::counter
    IL_789c:  ldloc.2
    IL_789d:  call       int32 [mscorlib]System.Threading.Interlocked::CompareExchange(int32&,
                                                                                       int32,
                                                                                       int32)
    IL_78a2:  stloc.3
    IL_78a3:  ldsfld     int32 IntrinsicTest::id_counter
    IL_78a8:  stloc.s    V_13
    IL_78aa:  ldsfld     int32 IntrinsicTest::id_counter
    IL_78af:  ldloc.s    V_12
    IL_78b1:  bne.un.s   IL_78bf

    IL_78b3:  ldsfld     int32 IntrinsicTest::id_counter
    IL_78b8:  stsfld     int32 IntrinsicTest::id_counter
    IL_78bd:  br.s       IL_78ca

    IL_78bf:  ldsfld     int32 IntrinsicTest::id_counter
    IL_78c4:  conv.i8
    IL_78c5:  stsfld     int64 IntrinsicTest::idjunk
    IL_78ca:  ldstr      "------------------------------------CMPXCHG  1 1 6 6"
    IL_78cf:  call       void [System.Console]System.Console::WriteLine(string)
    IL_78d4:  ldloc.0
    IL_78d5:  ldloc.s    V_10
    IL_78d7:  beq.s      IL_78f3

    IL_78d9:  ldc.i4.1
    IL_78da:  stloc.s    V_20
    IL_78dc:  ldstr      "te0 check failed {0} {1}"
    IL_78e1:  ldloc.0
    IL_78e2:  box        [mscorlib]System.Int32
    IL_78e7:  ldloc.s    V_10
    IL_78e9:  box        [mscorlib]System.Int32
    IL_78ee:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_78f3:  ldloc.1
    IL_78f4:  ldloc.s    V_11
    IL_78f6:  beq.s      IL_7912

    IL_78f8:  ldc.i4.1
    IL_78f9:  stloc.s    V_20
    IL_78fb:  ldstr      "te1 check failed {0} {1}"
    IL_7900:  ldloc.1
    IL_7901:  box        [mscorlib]System.Int32
    IL_7906:  ldloc.s    V_11
    IL_7908:  box        [mscorlib]System.Int32
    IL_790d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7912:  ldloc.2
    IL_7913:  ldloc.s    V_12
    IL_7915:  beq.s      IL_7931

    IL_7917:  ldc.i4.1
    IL_7918:  stloc.s    V_20
    IL_791a:  ldstr      "te2 check failed {0} {1}"
    IL_791f:  ldloc.2
    IL_7920:  box        [mscorlib]System.Int32
    IL_7925:  ldloc.s    V_12
    IL_7927:  box        [mscorlib]System.Int32
    IL_792c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7931:  ldloc.3
    IL_7932:  ldloc.s    V_13
    IL_7934:  beq.s      IL_7950

    IL_7936:  ldc.i4.1
    IL_7937:  stloc.s    V_20
    IL_7939:  ldstr      "te3 check failed {0} {1}"
    IL_793e:  ldloc.3
    IL_793f:  box        [mscorlib]System.Int32
    IL_7944:  ldloc.s    V_13
    IL_7946:  box        [mscorlib]System.Int32
    IL_794b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7950:  ldloc.s    V_4
    IL_7952:  ldloc.s    V_14
    IL_7954:  beq.s      IL_7971

    IL_7956:  ldc.i4.1
    IL_7957:  stloc.s    V_20
    IL_7959:  ldstr      "te064 check failed {0} {1}"
    IL_795e:  ldloc.s    V_4
    IL_7960:  box        [mscorlib]System.Int64
    IL_7965:  ldloc.s    V_14
    IL_7967:  box        [mscorlib]System.Int64
    IL_796c:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7971:  ldloc.s    V_5
    IL_7973:  ldloc.s    V_15
    IL_7975:  beq.s      IL_7992

    IL_7977:  ldc.i4.1
    IL_7978:  stloc.s    V_20
    IL_797a:  ldstr      "te164 check failed {0} {1}"
    IL_797f:  ldloc.s    V_5
    IL_7981:  box        [mscorlib]System.Int64
    IL_7986:  ldloc.s    V_15
    IL_7988:  box        [mscorlib]System.Int64
    IL_798d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7992:  ldloc.s    V_6
    IL_7994:  ldloc.s    V_16
    IL_7996:  beq.s      IL_79b3

    IL_7998:  ldc.i4.1
    IL_7999:  stloc.s    V_20
    IL_799b:  ldstr      "te264 check failed {0} {1}"
    IL_79a0:  ldloc.s    V_6
    IL_79a2:  box        [mscorlib]System.Int64
    IL_79a7:  ldloc.s    V_16
    IL_79a9:  box        [mscorlib]System.Int64
    IL_79ae:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_79b3:  ldloc.s    V_7
    IL_79b5:  ldloc.s    V_17
    IL_79b7:  beq.s      IL_79d4

    IL_79b9:  ldc.i4.1
    IL_79ba:  stloc.s    V_20
    IL_79bc:  ldstr      "te364 check failed {0} {1}"
    IL_79c1:  ldloc.s    V_7
    IL_79c3:  box        [mscorlib]System.Int64
    IL_79c8:  ldloc.s    V_17
    IL_79ca:  box        [mscorlib]System.Int64
    IL_79cf:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_79d4:  ldloc.s    V_8
    IL_79d6:  ldloc.s    V_18
    IL_79d8:  beq.s      IL_79f5

    IL_79da:  ldc.i4.1
    IL_79db:  stloc.s    V_20
    IL_79dd:  ldstr      "dummy check failed {0} {1}"
    IL_79e2:  ldloc.s    V_8
    IL_79e4:  box        [mscorlib]System.Int32
    IL_79e9:  ldloc.s    V_18
    IL_79eb:  box        [mscorlib]System.Int32
    IL_79f0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_79f5:  ldloc.s    V_9
    IL_79f7:  ldloc.s    V_19
    IL_79f9:  beq.s      IL_7a16

    IL_79fb:  ldc.i4.1
    IL_79fc:  stloc.s    V_20
    IL_79fe:  ldstr      "dummy64 check failed {0} {1}"
    IL_7a03:  ldloc.s    V_9
    IL_7a05:  box        [mscorlib]System.Int64
    IL_7a0a:  ldloc.s    V_19
    IL_7a0c:  box        [mscorlib]System.Int64
    IL_7a11:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7a16:  ldsfld     int32 IntrinsicTest::counter
    IL_7a1b:  ldsfld     int32 IntrinsicTest::id_counter
    IL_7a20:  beq.s      IL_7a43

    IL_7a22:  ldstr      "counter mismatch {0} {1}"
    IL_7a27:  ldsfld     int32 IntrinsicTest::counter
    IL_7a2c:  box        [mscorlib]System.Int32
    IL_7a31:  ldsfld     int32 IntrinsicTest::id_counter
    IL_7a36:  box        [mscorlib]System.Int32
    IL_7a3b:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7a40:  ldc.i4.1
    IL_7a41:  stloc.s    V_20
    IL_7a43:  ldsfld     int64 IntrinsicTest::counter64
    IL_7a48:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_7a4d:  beq.s      IL_7a70

    IL_7a4f:  ldstr      "counter64 mismatch {0} {1}"
    IL_7a54:  ldsfld     int64 IntrinsicTest::counter64
    IL_7a59:  box        [mscorlib]System.Int64
    IL_7a5e:  ldsfld     int64 IntrinsicTest::id_counter64
    IL_7a63:  box        [mscorlib]System.Int64
    IL_7a68:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7a6d:  ldc.i4.1
    IL_7a6e:  stloc.s    V_20
    IL_7a70:  ldloc.s    V_21
    IL_7a72:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_7a77:  ldloc.s    V_21
    IL_7a79:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_7a7e:  beq.s      IL_7aa5

    IL_7a80:  ldstr      "instanceCounter mismatch {0} {1}"
    IL_7a85:  ldloc.s    V_21
    IL_7a87:  ldfld      int32 IntrinsicTest::instanceCounter
    IL_7a8c:  box        [mscorlib]System.Int32
    IL_7a91:  ldloc.s    V_21
    IL_7a93:  ldfld      int32 IntrinsicTest::id_instanceCounter
    IL_7a98:  box        [mscorlib]System.Int32
    IL_7a9d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7aa2:  ldc.i4.1
    IL_7aa3:  stloc.s    V_20
    IL_7aa5:  ldloc.s    V_21
    IL_7aa7:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_7aac:  ldloc.s    V_21
    IL_7aae:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_7ab3:  beq.s      IL_7ada

    IL_7ab5:  ldstr      "instanceCounter64 mismatch {0} {1}"
    IL_7aba:  ldloc.s    V_21
    IL_7abc:  ldfld      int64 IntrinsicTest::instanceCounter64
    IL_7ac1:  box        [mscorlib]System.Int64
    IL_7ac6:  ldloc.s    V_21
    IL_7ac8:  ldfld      int64 IntrinsicTest::id_instanceCounter64
    IL_7acd:  box        [mscorlib]System.Int64
    IL_7ad2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7ad7:  ldc.i4.1
    IL_7ad8:  stloc.s    V_20
    IL_7ada:  ldsfld     int64 IntrinsicTest::temp
    IL_7adf:  ldsfld     int64 IntrinsicTest::idmp
    IL_7ae4:  beq.s      IL_7b07

    IL_7ae6:  ldstr      "temp mismatch {0} {1}"
    IL_7aeb:  ldsfld     int64 IntrinsicTest::temp
    IL_7af0:  box        [mscorlib]System.Int64
    IL_7af5:  ldsfld     int64 IntrinsicTest::idmp
    IL_7afa:  box        [mscorlib]System.Int64
    IL_7aff:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_7b04:  ldc.i4.1
    IL_7b05:  stloc.s    V_20
    IL_7b07:  ldloc.s    V_20
    IL_7b09:  ret
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



