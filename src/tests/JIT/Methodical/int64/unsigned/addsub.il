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
// MVID: {964B45BB-9F5B-4A2B-9ECD-E062E2FE8E23}
.namespace JitTest_addsub_unsigned_il
{
  .class public auto ansi Test
         extends ['mscorlib']System.Object
  {
    .method private hidebysig static void 
            testNumbers(unsigned int64 a,
                        unsigned int64 b) il managed
    {
      // Code size       161 (0xa1)
      .maxstack  4
      .locals (unsigned int64 V_0,
               unsigned int64 V_1,
               unsigned int64 V_2,
               unsigned int64 V_3,
               unsigned int64 V_4,
               unsigned int64 V_5,
               unsigned int64 V_6)
      IL_0000:  ldc.i4.0
      IL_0001:  conv.i8
      IL_0002:  stloc.0
      .try
      {
        IL_0003:  ldarg.0
        IL_0004:  ldarg.1
        IL_0005:  add.ovf.un
        IL_0006:  stloc.0
        IL_0007:  ldloc.0
        IL_0008:  ldarg.0
        IL_0009:  blt.un.s   IL_000f

        IL_000b:  ldloc.0
        IL_000c:  ldarg.1
        IL_000d:  bge.un.s   IL_0015

        IL_000f:  newobj     instance void ['mscorlib']System.Exception::.ctor()
        IL_0014:  throw

        IL_0015:  leave.s    IL_008e

      }  // end .try
      catch ['mscorlib']System.OverflowException 
      {
        IL_0017:  pop
        IL_0018:  ldarg.0
        IL_0019:  ldc.i4.s   32
        IL_001b:  shr.un
        IL_001c:  stloc.1
        IL_001d:  ldarg.0
        IL_001e:  ldc.i4.m1
        IL_001f:  conv.u8
        IL_0020:  and
        IL_0021:  stloc.2
        IL_0022:  ldarg.1
        IL_0023:  ldc.i4.s   32
        IL_0025:  shr.un
        IL_0026:  stloc.3
        IL_0027:  ldarg.1
        IL_0028:  ldc.i4.m1
        IL_0029:  conv.u8
        IL_002a:  and
        IL_002b:  stloc.s    V_4
        IL_002d:  ldloc.s    V_4
        IL_002f:  ldloc.2
        IL_0030:  add.ovf.un
        IL_0031:  stloc.s    V_5
        IL_0033:  ldloc.1
        IL_0034:  ldloc.3
        IL_0035:  add.ovf.un
        IL_0036:  stloc.s    V_6
        IL_0038:  ldloc.s    V_6
        IL_003a:  ldc.i8     0x100000000
        IL_0043:  bge.un.s   IL_008c

        IL_0045:  ldloc.s    V_5
        IL_0047:  ldc.i8     0x100000000
        IL_0050:  blt.un.s   IL_007a

        IL_0052:  ldloc.s    V_6
        IL_0054:  ldc.i4.1
        IL_0055:  conv.i8
        IL_0056:  add.ovf.un
        IL_0057:  stloc.s    V_6
        IL_0059:  ldloc.s    V_5
        IL_005b:  ldc.i8     0x100000000
        IL_0064:  sub.ovf.un
        IL_0065:  stloc.s    V_5
        IL_0067:  ldloc.s    V_5
        IL_0069:  ldc.i8     0x100000000
        IL_0072:  blt.un.s   IL_007a

        IL_0074:  newobj     instance void ['mscorlib']System.Exception::.ctor()
        IL_0079:  throw

        IL_007a:  ldloc.s    V_5
        IL_007c:  ldc.i4.m1
        IL_007d:  conv.i8
        IL_007e:  ldloc.s    V_6
        IL_0080:  ldc.i4.s   32
        IL_0082:  shl
        IL_0083:  sub.ovf.un
        IL_0084:  bgt.un.s   IL_008c

        IL_0086:  newobj     instance void ['mscorlib']System.Exception::.ctor()
        IL_008b:  throw

        IL_008c:  leave.s    IL_00a0

      }  // end handler
      IL_008e:  ldloc.0
      IL_008f:  ldarg.1
      IL_0090:  sub.ovf.un
      IL_0091:  ldarg.0
      IL_0092:  bne.un.s   IL_009a

      IL_0094:  ldloc.0
      IL_0095:  ldarg.0
      IL_0096:  sub.ovf.un
      IL_0097:  ldarg.1
      IL_0098:  beq.s      IL_00a0

      IL_009a:  newobj     instance void ['mscorlib']System.Exception::.ctor()
      IL_009f:  throw

      IL_00a0:  ret
    } // end of method 'Test::testNumbers'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       5045 (0x13b5)
      .maxstack  2
      .locals (int32 V_0)
      .try
      {
        IL_0000:  ldc.i4.s   9
        IL_0002:  conv.i8
        IL_0003:  ldc.i4     0xb8
        IL_0008:  conv.i8
        IL_0009:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_000e:  ldc.i4.s   9
        IL_0010:  conv.i8
        IL_0011:  ldc.i4     0xf9
        IL_0016:  conv.i8
        IL_0017:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_001c:  ldc.i4.s   110
        IL_001e:  conv.i8
        IL_001f:  ldc.i4     0x93
        IL_0024:  conv.i8
        IL_0025:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_002a:  ldc.i4.s   30
        IL_002c:  conv.i8
        IL_002d:  ldc.i4     0x86
        IL_0032:  conv.i8
        IL_0033:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0038:  ldc.i4     0xcc
        IL_003d:  conv.i8
        IL_003e:  ldc.i4     0x583f
        IL_0043:  conv.i8
        IL_0044:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0049:  ldc.i4     0xc9
        IL_004e:  conv.i8
        IL_004f:  ldc.i4     0xa94c
        IL_0054:  conv.i8
        IL_0055:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_005a:  ldc.i4.s   84
        IL_005c:  conv.i8
        IL_005d:  ldc.i4     0x2d06
        IL_0062:  conv.i8
        IL_0063:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0068:  ldc.i4.s   48
        IL_006a:  conv.i8
        IL_006b:  ldc.i4     0x9921
        IL_0070:  conv.i8
        IL_0071:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0076:  ldc.i4.s   29
        IL_0078:  conv.i8
        IL_0079:  ldc.i4     0x450842
        IL_007e:  conv.i8
        IL_007f:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0084:  ldc.i4.s   42
        IL_0086:  conv.i8
        IL_0087:  ldc.i4     0x999f6c
        IL_008c:  conv.i8
        IL_008d:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0092:  ldc.i4     0xc5
        IL_0097:  conv.i8
        IL_0098:  ldc.i4     0x90faa7
        IL_009d:  conv.i8
        IL_009e:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_00a3:  ldc.i4.s   80
        IL_00a5:  conv.i8
        IL_00a6:  ldc.i4     0x69de08
        IL_00ab:  conv.i8
        IL_00ac:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_00b1:  ldc.i4     0x9a
        IL_00b6:  conv.i8
        IL_00b7:  ldc.i4     0xcd715be
        IL_00bc:  conv.i8
        IL_00bd:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_00c2:  ldc.i4.s   57
        IL_00c4:  conv.i8
        IL_00c5:  ldc.i4     0x16a61eb5
        IL_00ca:  conv.i8
        IL_00cb:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_00d0:  ldc.i4     0xe0
        IL_00d5:  conv.i8
        IL_00d6:  ldc.i4     0x95575fef
        IL_00db:  conv.u8
        IL_00dc:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_00e1:  ldc.i4     0x93
        IL_00e6:  conv.i8
        IL_00e7:  ldc.i4     0x209e58c5
        IL_00ec:  conv.i8
        IL_00ed:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_00f2:  ldc.i4.s   59
        IL_00f4:  conv.i8
        IL_00f5:  ldc.i8     0xc3c34b48c
        IL_00fe:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0103:  ldc.i4     0xc2
        IL_0108:  conv.i8
        IL_0109:  ldc.i8     0x6a671c470f
        IL_0112:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0117:  ldc.i4.s   75
        IL_0119:  conv.i8
        IL_011a:  ldc.i8     0xf538cede2b
        IL_0123:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0128:  ldc.i4     0x99
        IL_012d:  conv.i8
        IL_012e:  ldc.i8     0x5ba885d43b
        IL_0137:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_013c:  ldc.i4.s   104
        IL_013e:  conv.i8
        IL_013f:  ldc.i8     0x9f692f98ac45
        IL_0148:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_014d:  ldc.i4     0xd9
        IL_0152:  conv.i8
        IL_0153:  ldc.i8     0x8d5eaa7f0a8e
        IL_015c:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0161:  ldc.i4     0xac
        IL_0166:  conv.i8
        IL_0167:  ldc.i8     0xba1316512e4c
        IL_0170:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0175:  ldc.i4.s   28
        IL_0177:  conv.i8
        IL_0178:  ldc.i8     0x8c4fbf2f14aa
        IL_0181:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0186:  ldc.i4     0xc0
        IL_018b:  conv.i8
        IL_018c:  ldc.i8     0x69a9eb9a9bc822
        IL_0195:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_019a:  ldc.i4.s   116
        IL_019c:  conv.i8
        IL_019d:  ldc.i8     0x3f8f5a893de200
        IL_01a6:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_01ab:  ldc.i4.s   39
        IL_01ad:  conv.i8
        IL_01ae:  ldc.i8     0x650eb1747a5bc
        IL_01b7:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_01bc:  ldc.i4     0xd9
        IL_01c1:  conv.i8
        IL_01c2:  ldc.i8     0xd3d50809c70fda
        IL_01cb:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_01d0:  ldc.i4     0xc0
        IL_01d5:  conv.i8
        IL_01d6:  ldc.i8     0xac6556a4ca94513e
        IL_01df:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_01e4:  ldc.i4.s   32
        IL_01e6:  conv.i8
        IL_01e7:  ldc.i8     0xa697fcbfd6d232d1
        IL_01f0:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_01f5:  ldc.i4     0x9c
        IL_01fa:  conv.i8
        IL_01fb:  ldc.i8     0xc4421a4f5147b9b8
        IL_0204:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0209:  ldc.i4     0x9e
        IL_020e:  conv.i8
        IL_020f:  ldc.i8     0xc5ef494112a7b33f
        IL_0218:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_021d:  ldc.i4     0xf7fa
        IL_0222:  conv.i8
        IL_0223:  ldc.i4     0xaf
        IL_0228:  conv.i8
        IL_0229:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_022e:  ldc.i4     0xad17
        IL_0233:  conv.i8
        IL_0234:  ldc.i4     0xe8
        IL_0239:  conv.i8
        IL_023a:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_023f:  ldc.i4     0xc9c4
        IL_0244:  conv.i8
        IL_0245:  ldc.i4.s   69
        IL_0247:  conv.i8
        IL_0248:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_024d:  ldc.i4     0xa704
        IL_0252:  conv.i8
        IL_0253:  ldc.i4.s   18
        IL_0255:  conv.i8
        IL_0256:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_025b:  ldc.i4     0xc55b
        IL_0260:  conv.i8
        IL_0261:  ldc.i4     0xa33a
        IL_0266:  conv.i8
        IL_0267:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_026c:  ldc.i4     0xab88
        IL_0271:  conv.i8
        IL_0272:  ldc.i4     0x9a3c
        IL_0277:  conv.i8
        IL_0278:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_027d:  ldc.i4     0xa539
        IL_0282:  conv.i8
        IL_0283:  ldc.i4     0xcf3a
        IL_0288:  conv.i8
        IL_0289:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_028e:  ldc.i4     0x5890
        IL_0293:  conv.i8
        IL_0294:  ldc.i4     0xeec8
        IL_0299:  conv.i8
        IL_029a:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_029f:  ldc.i4     0xe9e2
        IL_02a4:  conv.i8
        IL_02a5:  ldc.i4     0xfe7c46
        IL_02aa:  conv.i8
        IL_02ab:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_02b0:  ldc.i4     0x7303
        IL_02b5:  conv.i8
        IL_02b6:  ldc.i4     0x419f2a
        IL_02bb:  conv.i8
        IL_02bc:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_02c1:  ldc.i4     0xe105
        IL_02c6:  conv.i8
        IL_02c7:  ldc.i4     0x13f913
        IL_02cc:  conv.i8
        IL_02cd:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_02d2:  ldc.i4     0x8191
        IL_02d7:  conv.i8
        IL_02d8:  ldc.i4     0xfa2458
        IL_02dd:  conv.i8
        IL_02de:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_02e3:  ldc.i4     0x6d9
        IL_02e8:  conv.i8
        IL_02e9:  ldc.i4     0x91cf14f7
        IL_02ee:  conv.u8
        IL_02ef:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_02f4:  ldc.i4     0xbdb1
        IL_02f9:  conv.i8
        IL_02fa:  ldc.i4     0x86c2a97c
        IL_02ff:  conv.u8
        IL_0300:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0305:  ldc.i4     0xe905
        IL_030a:  conv.i8
        IL_030b:  ldc.i4     0x64f702f4
        IL_0310:  conv.i8
        IL_0311:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0316:  ldc.i4     0x2fdc
        IL_031b:  conv.i8
        IL_031c:  ldc.i4     0xf059caf6
        IL_0321:  conv.u8
        IL_0322:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0327:  ldc.i4     0xf8fd
        IL_032c:  conv.i8
        IL_032d:  ldc.i8     0x13f0265b1e
        IL_0336:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_033b:  ldc.i4     0xe8b8
        IL_0340:  conv.i8
        IL_0341:  ldc.i8     0xaa69a6308
        IL_034a:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_034f:  ldc.i4     0x3d00
        IL_0354:  conv.i8
        IL_0355:  ldc.i8     0xfbcb67879b
        IL_035e:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0363:  ldc.i4     0xaa46
        IL_0368:  conv.i8
        IL_0369:  ldc.i8     0x85c3d371d5
        IL_0372:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0377:  ldc.i4     0x5f60
        IL_037c:  conv.i8
        IL_037d:  ldc.i8     0x8cde4a63203
        IL_0386:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_038b:  ldc.i4     0x92b5
        IL_0390:  conv.i8
        IL_0391:  ldc.i8     0x7ca86ba2f30e
        IL_039a:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_039f:  ldc.i4     0x93c6
        IL_03a4:  conv.i8
        IL_03a5:  ldc.i8     0xa2d73fc4eac0
        IL_03ae:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_03b3:  ldc.i4     0x4156
        IL_03b8:  conv.i8
        IL_03b9:  ldc.i8     0x6dbd08f2fda
        IL_03c2:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_03c7:  ldc.i4     0x4597
        IL_03cc:  conv.i8
        IL_03cd:  ldc.i8     0x6cfb0ba5962826
        IL_03d6:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_03db:  ldc.i4     0x6bac
        IL_03e0:  conv.i8
        IL_03e1:  ldc.i8     0x1e79315071480f
        IL_03ea:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_03ef:  ldc.i4     0x2c3a
        IL_03f4:  conv.i8
        IL_03f5:  ldc.i8     0x92f12cbd82df69
        IL_03fe:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0403:  ldc.i4     0x9859
        IL_0408:  conv.i8
        IL_0409:  ldc.i8     0xb0f0cd9dc019f2
        IL_0412:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0417:  ldc.i4     0xb37f
        IL_041c:  conv.i8
        IL_041d:  ldc.i8     0x4966447d15850076
        IL_0426:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_042b:  ldc.i4     0x5e34
        IL_0430:  conv.i8
        IL_0431:  ldc.i8     0x7c1869c9ed2cad38
        IL_043a:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_043f:  ldc.i4     0x5c54
        IL_0444:  conv.i8
        IL_0445:  ldc.i8     0x7cee70ee82837a08
        IL_044e:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0453:  ldc.i4     0x967f
        IL_0458:  conv.i8
        IL_0459:  ldc.i8     0x4eb98adf4b8b0d32
        IL_0462:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0467:  ldc.i4     0xfd2919
        IL_046c:  conv.i8
        IL_046d:  ldc.i4.s   93
        IL_046f:  conv.i8
        IL_0470:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0475:  ldc.i4     0xabd5b1
        IL_047a:  conv.i8
        IL_047b:  ldc.i4     0x98
        IL_0480:  conv.i8
        IL_0481:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0486:  ldc.i4     0xab1887
        IL_048b:  conv.i8
        IL_048c:  ldc.i4     0xef
        IL_0491:  conv.i8
        IL_0492:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0497:  ldc.i4     0x96034a
        IL_049c:  conv.i8
        IL_049d:  ldc.i4.s   47
        IL_049f:  conv.i8
        IL_04a0:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_04a5:  ldc.i4     0xd5bb94
        IL_04aa:  conv.i8
        IL_04ab:  ldc.i4     0x57d2
        IL_04b0:  conv.i8
        IL_04b1:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_04b6:  ldc.i4     0xd7b2cb
        IL_04bb:  conv.i8
        IL_04bc:  ldc.i4     0x80f5
        IL_04c1:  conv.i8
        IL_04c2:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_04c7:  ldc.i4     0x4ccc6d
        IL_04cc:  conv.i8
        IL_04cd:  ldc.i4     0x87c
        IL_04d2:  conv.i8
        IL_04d3:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_04d8:  ldc.i4     0xec0c50
        IL_04dd:  conv.i8
        IL_04de:  ldc.i4     0xbdff
        IL_04e3:  conv.i8
        IL_04e4:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_04e9:  ldc.i4     0x8a6865
        IL_04ee:  conv.i8
        IL_04ef:  ldc.i4     0x76c014
        IL_04f4:  conv.i8
        IL_04f5:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_04fa:  ldc.i4     0xac38dd
        IL_04ff:  conv.i8
        IL_0500:  ldc.i4     0xf12b09
        IL_0505:  conv.i8
        IL_0506:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_050b:  ldc.i4     0x615e2a
        IL_0510:  conv.i8
        IL_0511:  ldc.i4     0xe7cbf8
        IL_0516:  conv.i8
        IL_0517:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_051c:  ldc.i4     0xe214f
        IL_0521:  conv.i8
        IL_0522:  ldc.i4     0x5b8e2f
        IL_0527:  conv.i8
        IL_0528:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_052d:  ldc.i4     0x3bd7c6
        IL_0532:  conv.i8
        IL_0533:  ldc.i4     0xc1db4e46
        IL_0538:  conv.u8
        IL_0539:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_053e:  ldc.i4     0xae208d
        IL_0543:  conv.i8
        IL_0544:  ldc.i4     0x1c9aa7a
        IL_0549:  conv.i8
        IL_054a:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_054f:  ldc.i4     0x8a9cef
        IL_0554:  conv.i8
        IL_0555:  ldc.i4     0x3930b07
        IL_055a:  conv.i8
        IL_055b:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0560:  ldc.i4     0x36b866
        IL_0565:  conv.i8
        IL_0566:  ldc.i4     0xd64b7bef
        IL_056b:  conv.u8
        IL_056c:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0571:  ldc.i4     0xd337cd
        IL_0576:  conv.i8
        IL_0577:  ldc.i8     0xa2b45fb7de
        IL_0580:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0585:  ldc.i4     0x24471
        IL_058a:  conv.i8
        IL_058b:  ldc.i8     0x5c5de3da89
        IL_0594:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0599:  ldc.i4     0x12b15
        IL_059e:  conv.i8
        IL_059f:  ldc.i8     0x7cd40030fe
        IL_05a8:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_05ad:  ldc.i4     0xd38af2
        IL_05b2:  conv.i8
        IL_05b3:  ldc.i8     0x5905921572
        IL_05bc:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_05c1:  ldc.i4     0xaca0d7
        IL_05c6:  conv.i8
        IL_05c7:  ldc.i8     0xc632301abeb8
        IL_05d0:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_05d5:  ldc.i4     0x4eadc2
        IL_05da:  conv.i8
        IL_05db:  ldc.i8     0x6a1ebf37403c
        IL_05e4:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_05e9:  ldc.i4     0x5d909c
        IL_05ee:  conv.i8
        IL_05ef:  ldc.i8     0x4021bfa15862
        IL_05f8:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_05fd:  ldc.i4     0x710e08
        IL_0602:  conv.i8
        IL_0603:  ldc.i8     0xe9a1a030b230
        IL_060c:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0611:  ldc.i4     0x478b9b
        IL_0616:  conv.i8
        IL_0617:  ldc.i8     0x804add8afc31d9
        IL_0620:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0625:  ldc.i4     0x5754ed
        IL_062a:  conv.i8
        IL_062b:  ldc.i8     0xaf85e7ebb1ce33
        IL_0634:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0639:  ldc.i4     0x3ab44e
        IL_063e:  conv.i8
        IL_063f:  ldc.i8     0xf41b9f70360f78
        IL_0648:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_064d:  ldc.i4     0x7aa129
        IL_0652:  conv.i8
        IL_0653:  ldc.i8     0xeb6e4eddf7eb87
        IL_065c:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0661:  ldc.i4     0x3b036f
        IL_0666:  conv.i8
        IL_0667:  ldc.i8     0x333874e4330fbfa4
        IL_0670:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0675:  ldc.i4     0xa33186
        IL_067a:  conv.i8
        IL_067b:  ldc.i8     0xec8607412503fc4c
        IL_0684:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0689:  ldc.i4     0x9af471
        IL_068e:  conv.i8
        IL_068f:  ldc.i8     0xe7ad0935fdbff151
        IL_0698:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_069d:  ldc.i4     0xc04e8c
        IL_06a2:  conv.i8
        IL_06a3:  ldc.i8     0x58ee406ab936ac24
        IL_06ac:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_06b1:  ldc.i4     0x54fdd28b
        IL_06b6:  conv.i8
        IL_06b7:  ldc.i4.s   52
        IL_06b9:  conv.i8
        IL_06ba:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_06bf:  ldc.i4     0x33736b36
        IL_06c4:  conv.i8
        IL_06c5:  ldc.i4     0xfd
        IL_06ca:  conv.i8
        IL_06cb:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_06d0:  ldc.i4     0x69cfe4b7
        IL_06d5:  conv.i8
        IL_06d6:  ldc.i4.s   38
        IL_06d8:  conv.i8
        IL_06d9:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_06de:  ldc.i4     0xfd078d36
        IL_06e3:  conv.u8
        IL_06e4:  ldc.i4     0xdc
        IL_06e9:  conv.i8
        IL_06ea:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_06ef:  ldc.i4     0x75cc3f36
        IL_06f4:  conv.i8
        IL_06f5:  ldc.i4     0x1617
        IL_06fa:  conv.i8
        IL_06fb:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0700:  ldc.i4     0x75d660e
        IL_0705:  conv.i8
        IL_0706:  ldc.i4     0x8511
        IL_070b:  conv.i8
        IL_070c:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0711:  ldc.i4     0x52acb037
        IL_0716:  conv.i8
        IL_0717:  ldc.i4     0x43cb
        IL_071c:  conv.i8
        IL_071d:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0722:  ldc.i4     0xa0db7bf5
        IL_0727:  conv.u8
        IL_0728:  ldc.i4     0x2c98
        IL_072d:  conv.i8
        IL_072e:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0733:  ldc.i4     0x83d4be11
        IL_0738:  conv.u8
        IL_0739:  ldc.i4     0xba37c9
        IL_073e:  conv.i8
        IL_073f:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0744:  ldc.i4     0x83d04f94
        IL_0749:  conv.u8
        IL_074a:  ldc.i4     0x3ddbd0
        IL_074f:  conv.i8
        IL_0750:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0755:  ldc.i4     0x5ed41f6a
        IL_075a:  conv.i8
        IL_075b:  ldc.i4     0xeaf1d5
        IL_0760:  conv.i8
        IL_0761:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0766:  ldc.i4     0xe364a9a
        IL_076b:  conv.i8
        IL_076c:  ldc.i4     0x85880c
        IL_0771:  conv.i8
        IL_0772:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0777:  ldc.i4     0x12657ecb
        IL_077c:  conv.i8
        IL_077d:  ldc.i4     0xa88b8a68
        IL_0782:  conv.u8
        IL_0783:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0788:  ldc.i4     0x9897a4ac
        IL_078d:  conv.u8
        IL_078e:  ldc.i4     0x76707981
        IL_0793:  conv.i8
        IL_0794:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0799:  ldc.i4     0x469cd1cf
        IL_079e:  conv.i8
        IL_079f:  ldc.i4     0xcf40f67a
        IL_07a4:  conv.u8
        IL_07a5:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_07aa:  ldc.i4     0xee7444c8
        IL_07af:  conv.u8
        IL_07b0:  ldc.i4     0xd1b0d7de
        IL_07b5:  conv.u8
        IL_07b6:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_07bb:  ldc.i4     0xfbb6f547
        IL_07c0:  conv.u8
        IL_07c1:  ldc.i8     0xc1ef3c4d9b
        IL_07ca:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_07cf:  ldc.i4     0xe20dd53
        IL_07d4:  conv.i8
        IL_07d5:  ldc.i8     0xb05833c7cf
        IL_07de:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_07e3:  ldc.i4     0xe5733fb8
        IL_07e8:  conv.u8
        IL_07e9:  ldc.i8     0x8eae18a855
        IL_07f2:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_07f7:  ldc.i4     0x5db1c271
        IL_07fc:  conv.i8
        IL_07fd:  ldc.i8     0xc4a2f7c27d
        IL_0806:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_080b:  ldc.i4     0x7add22a
        IL_0810:  conv.i8
        IL_0811:  ldc.i8     0xed9fd23dc3e
        IL_081a:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_081f:  ldc.i4     0x2239d1d5
        IL_0824:  conv.i8
        IL_0825:  ldc.i8     0xa1ae07a62635
        IL_082e:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0833:  ldc.i4     0x410d4d58
        IL_0838:  conv.i8
        IL_0839:  ldc.i8     0xc05c5205bed2
        IL_0842:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0847:  ldc.i4     0x4c3c435e
        IL_084c:  conv.i8
        IL_084d:  ldc.i8     0x1e30c1bf628a
        IL_0856:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_085b:  ldc.i4     0x96f44d5
        IL_0860:  conv.i8
        IL_0861:  ldc.i8     0x5488c521a6072b
        IL_086a:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_086f:  ldc.i4     0x17f28913
        IL_0874:  conv.i8
        IL_0875:  ldc.i8     0x796ff3891c44ff
        IL_087e:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0883:  ldc.i4     0x65be69cf
        IL_0888:  conv.i8
        IL_0889:  ldc.i8     0xdd5c6f9b3f3119
        IL_0892:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0897:  ldc.i4     0x2200f221
        IL_089c:  conv.i8
        IL_089d:  ldc.i8     0xab6c98c90cfe9d
        IL_08a6:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_08ab:  ldc.i4     0xd48bee1a
        IL_08b0:  conv.u8
        IL_08b1:  ldc.i8     0x64b76d7491a58799
        IL_08ba:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_08bf:  ldc.i4     0x6cb93100
        IL_08c4:  conv.i8
        IL_08c5:  ldc.i8     0xa515fe27402dad45
        IL_08ce:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_08d3:  ldc.i4     0xbed95abe
        IL_08d8:  conv.u8
        IL_08d9:  ldc.i8     0xc9924098acc74be9
        IL_08e2:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_08e7:  ldc.i4     0x92781a2e
        IL_08ec:  conv.u8
        IL_08ed:  ldc.i8     0x67ada9ef3f9e39b7
        IL_08f6:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_08fb:  ldc.i8     0xe3aafcdae2
        IL_0904:  ldc.i4     0x9c
        IL_0909:  conv.i8
        IL_090a:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_090f:  ldc.i8     0xd8dad80c34
        IL_0918:  ldc.i4     0x99
        IL_091d:  conv.i8
        IL_091e:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0923:  ldc.i8     0xaddcd074d6
        IL_092c:  ldc.i4     0xea
        IL_0931:  conv.i8
        IL_0932:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0937:  ldc.i8     0x96735bc25a
        IL_0940:  ldc.i4     0xba
        IL_0945:  conv.i8
        IL_0946:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_094b:  ldc.i8     0xf492ef7446
        IL_0954:  ldc.i4     0x39b1
        IL_0959:  conv.i8
        IL_095a:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_095f:  ldc.i8     0xbc86816119
        IL_0968:  ldc.i4     0x1520
        IL_096d:  conv.i8
        IL_096e:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0973:  ldc.i8     0x60a36818e7
        IL_097c:  ldc.i4     0xc5a8
        IL_0981:  conv.i8
        IL_0982:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0987:  ldc.i8     0x317121d508
        IL_0990:  ldc.i4     0xac3d
        IL_0995:  conv.i8
        IL_0996:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_099b:  ldc.i8     0x4abfdaf232
        IL_09a4:  ldc.i4     0x5cea57
        IL_09a9:  conv.i8
        IL_09aa:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_09af:  ldc.i8     0xacc458f392
        IL_09b8:  ldc.i4     0xa9c3e3
        IL_09bd:  conv.i8
        IL_09be:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_09c3:  ldc.i8     0x1020993532
        IL_09cc:  ldc.i4     0xdf6042
        IL_09d1:  conv.i8
        IL_09d2:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_09d7:  ldc.i8     0xad25b80abb
        IL_09e0:  ldc.i4     0xcec15b
        IL_09e5:  conv.i8
        IL_09e6:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_09eb:  ldc.i8     0x2305d2c443
        IL_09f4:  ldc.i4     0x2a26131c
        IL_09f9:  conv.i8
        IL_09fa:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_09ff:  ldc.i8     0x7c42e2ce0
        IL_0a08:  ldc.i4     0x9768024f
        IL_0a0d:  conv.u8
        IL_0a0e:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0a13:  ldc.i8     0x76f674816c
        IL_0a1c:  ldc.i4     0x8d33c7b4
        IL_0a21:  conv.u8
        IL_0a22:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0a27:  ldc.i8     0xbf567b23bc
        IL_0a30:  ldc.i4     0xef264890
        IL_0a35:  conv.u8
        IL_0a36:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0a3b:  ldc.i8     0xe3283681a0
        IL_0a44:  ldc.i8     0x2e66850719
        IL_0a4d:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0a52:  ldc.i8     0x11fe13754
        IL_0a5b:  ldc.i8     0x66fad0b407
        IL_0a64:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0a69:  ldc.i8     0x52f259009f
        IL_0a72:  ldc.i8     0xa2886ef414
        IL_0a7b:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0a80:  ldc.i8     0xa9ebb540fc
        IL_0a89:  ldc.i8     0x9d27ba694f
        IL_0a92:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0a97:  ldc.i8     0x83af60d7eb
        IL_0aa0:  ldc.i8     0xb6f2a0f51f4c
        IL_0aa9:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0aae:  ldc.i8     0xf2ec42d13a
        IL_0ab7:  ldc.i8     0x46855f279407
        IL_0ac0:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0ac5:  ldc.i8     0x94e71cb562
        IL_0ace:  ldc.i8     0x2d9566618e56
        IL_0ad7:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0adc:  ldc.i8     0xc0ee690ddc
        IL_0ae5:  ldc.i8     0x54295c8ca584
        IL_0aee:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0af3:  ldc.i8     0x2683cd5206
        IL_0afc:  ldc.i8     0xa5a2d269bcd188
        IL_0b05:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0b0a:  ldc.i8     0x2e77038305
        IL_0b13:  ldc.i8     0xc727f0f3787e22
        IL_0b1c:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0b21:  ldc.i8     0x8323b9d026
        IL_0b2a:  ldc.i8     0xfed29f8575c120
        IL_0b33:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0b38:  ldc.i8     0x7b3231f0fc
        IL_0b41:  ldc.i8     0x91080854b27d3e
        IL_0b4a:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0b4f:  ldc.i8     0x84522a7708
        IL_0b58:  ldc.i8     0x91ba8f22fccd6222
        IL_0b61:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0b66:  ldc.i8     0xafb1b50d90
        IL_0b6f:  ldc.i8     0x3261a532b65c7838
        IL_0b78:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0b7d:  ldc.i8     0x2c65e838c6
        IL_0b86:  ldc.i8     0x5b858452c9bf6f39
        IL_0b8f:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0b94:  ldc.i8     0x219e837734
        IL_0b9d:  ldc.i8     0x97873bed5bb0a44b
        IL_0ba6:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0bab:  ldc.i8     0x9f133e2f116f
        IL_0bb4:  ldc.i4.s   115
        IL_0bb6:  conv.i8
        IL_0bb7:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0bbc:  ldc.i8     0x887577574766
        IL_0bc5:  ldc.i4.s   72
        IL_0bc7:  conv.i8
        IL_0bc8:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0bcd:  ldc.i8     0xba4c778d4aa8
        IL_0bd6:  ldc.i4.s   58
        IL_0bd8:  conv.i8
        IL_0bd9:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0bde:  ldc.i8     0x2683df421474
        IL_0be7:  ldc.i4.s   86
        IL_0be9:  conv.i8
        IL_0bea:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0bef:  ldc.i8     0x6ff76294c275
        IL_0bf8:  ldc.i4     0x89f7
        IL_0bfd:  conv.i8
        IL_0bfe:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0c03:  ldc.i8     0xfdf053abefa2
        IL_0c0c:  ldc.i4     0xeb65
        IL_0c11:  conv.i8
        IL_0c12:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0c17:  ldc.i8     0xea4b254b24eb
        IL_0c20:  ldc.i4     0xba27
        IL_0c25:  conv.i8
        IL_0c26:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0c2b:  ldc.i8     0x9f7ce21b811
        IL_0c34:  ldc.i4     0xe8f6
        IL_0c39:  conv.i8
        IL_0c3a:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0c3f:  ldc.i8     0x9cc645fa08a1
        IL_0c48:  ldc.i4     0xa29ea3
        IL_0c4d:  conv.i8
        IL_0c4e:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0c53:  ldc.i8     0x726f9a9f816e
        IL_0c5c:  ldc.i4     0x70dce1
        IL_0c61:  conv.i8
        IL_0c62:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0c67:  ldc.i8     0xa4be34825ef6
        IL_0c70:  ldc.i4     0xbb2be7
        IL_0c75:  conv.i8
        IL_0c76:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0c7b:  ldc.i8     0x57ff147cb7c1
        IL_0c84:  ldc.i4     0xe255af
        IL_0c89:  conv.i8
        IL_0c8a:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0c8f:  ldc.i8     0xab9d6f546dd4
        IL_0c98:  ldc.i4     0x7e2772a5
        IL_0c9d:  conv.i8
        IL_0c9e:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0ca3:  ldc.i8     0xb148e3446e89
        IL_0cac:  ldc.i4     0x51ed3c28
        IL_0cb1:  conv.i8
        IL_0cb2:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0cb7:  ldc.i8     0x1e3abfe9725e
        IL_0cc0:  ldc.i4     0xd4dec3f4
        IL_0cc5:  conv.u8
        IL_0cc6:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0ccb:  ldc.i8     0xf61bcaba115e
        IL_0cd4:  ldc.i4     0xfade149f
        IL_0cd9:  conv.u8
        IL_0cda:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0cdf:  ldc.i8     0xae642b9a6626
        IL_0ce8:  ldc.i8     0xd8de0e0b9a
        IL_0cf1:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0cf6:  ldc.i8     0x9d015a13c8ae
        IL_0cff:  ldc.i8     0xafc8827997
        IL_0d08:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0d0d:  ldc.i8     0xecc72cc2df89
        IL_0d16:  ldc.i8     0x70d47ec7c4
        IL_0d1f:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0d24:  ldc.i8     0xfdbf05894fd2
        IL_0d2d:  ldc.i8     0x12aec393bd
        IL_0d36:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0d3b:  ldc.i8     0xcd7675a70874
        IL_0d44:  ldc.i8     0xd7d696a62cbc
        IL_0d4d:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0d52:  ldc.i8     0xfad44a89216d
        IL_0d5b:  ldc.i8     0xcb8cfc8ada4c
        IL_0d64:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0d69:  ldc.i8     0xf41eb5363551
        IL_0d72:  ldc.i8     0x9c040aa7775e
        IL_0d7b:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0d80:  ldc.i8     0x3c02d93e01f6
        IL_0d89:  ldc.i8     0xf1f4e68a14f8
        IL_0d92:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0d97:  ldc.i8     0xe0d99954b598
        IL_0da0:  ldc.i8     0xb2a2de4e453485
        IL_0da9:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0dae:  ldc.i8     0xa6081be866d9
        IL_0db7:  ldc.i8     0xf2a12e845e4f2e
        IL_0dc0:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0dc5:  ldc.i8     0xae56a5680dfd
        IL_0dce:  ldc.i8     0xc96cd7c15d5bec
        IL_0dd7:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0ddc:  ldc.i8     0x360363e37938
        IL_0de5:  ldc.i8     0xd4ed572e1937e0
        IL_0dee:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0df3:  ldc.i8     0x1f052aebf185
        IL_0dfc:  ldc.i8     0x3584e582d1c6db1a
        IL_0e05:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0e0a:  ldc.i8     0x3fac9c7b3d1b
        IL_0e13:  ldc.i8     0xa4b120f080d69113
        IL_0e1c:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0e21:  ldc.i8     0x5330d51c3217
        IL_0e2a:  ldc.i8     0xc16dd32ffd822c0e
        IL_0e33:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0e38:  ldc.i8     0xcd0694ff5ab0
        IL_0e41:  ldc.i8     0x29673fe67245fbfc
        IL_0e4a:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0e4f:  ldc.i8     0x98265e5a308523
        IL_0e58:  ldc.i4.s   125
        IL_0e5a:  conv.i8
        IL_0e5b:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0e60:  ldc.i8     0x560863350df217
        IL_0e69:  ldc.i4     0xc8
        IL_0e6e:  conv.i8
        IL_0e6f:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0e74:  ldc.i8     0x798ce804d829a1
        IL_0e7d:  ldc.i4     0xb1
        IL_0e82:  conv.i8
        IL_0e83:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0e88:  ldc.i8     0x7994c0051256fd
        IL_0e91:  ldc.i4.s   92
        IL_0e93:  conv.i8
        IL_0e94:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0e99:  ldc.i8     0xff1a2838e69f42
        IL_0ea2:  ldc.i4     0x3c16
        IL_0ea7:  conv.i8
        IL_0ea8:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0ead:  ldc.i8     0x9e7e95ac5de2c7
        IL_0eb6:  ldc.i4     0xed49
        IL_0ebb:  conv.i8
        IL_0ebc:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0ec1:  ldc.i8     0xfd6867eabba5c0
        IL_0eca:  ldc.i4     0xc689
        IL_0ecf:  conv.i8
        IL_0ed0:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0ed5:  ldc.i8     0x9d1632daf20de0
        IL_0ede:  ldc.i4     0xb74f
        IL_0ee3:  conv.i8
        IL_0ee4:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0ee9:  ldc.i8     0xee29d8f76d4e9c
        IL_0ef2:  ldc.i4     0x8020d4
        IL_0ef7:  conv.i8
        IL_0ef8:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0efd:  ldc.i8     0x89e03ecf8daa0a
        IL_0f06:  ldc.i4     0x3e7587
        IL_0f0b:  conv.i8
        IL_0f0c:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0f11:  ldc.i8     0x115763be4beb44
        IL_0f1a:  ldc.i4     0x88f762
        IL_0f1f:  conv.i8
        IL_0f20:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0f25:  ldc.i8     0x815cfc87c427d0
        IL_0f2e:  ldc.i4     0x9eec06
        IL_0f33:  conv.i8
        IL_0f34:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0f39:  ldc.i8     0x1d9c3c9ded0c1a
        IL_0f42:  ldc.i4     0xb9f6d331
        IL_0f47:  conv.u8
        IL_0f48:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0f4d:  ldc.i8     0x932225412f1222
        IL_0f56:  ldc.i4     0x130ff743
        IL_0f5b:  conv.i8
        IL_0f5c:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0f61:  ldc.i8     0xfe82151e2e0bf3
        IL_0f6a:  ldc.i4     0x781cd6f9
        IL_0f6f:  conv.i8
        IL_0f70:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0f75:  ldc.i8     0x2222abb5061b12
        IL_0f7e:  ldc.i4     0x491f1df
        IL_0f83:  conv.i8
        IL_0f84:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0f89:  ldc.i8     0x12ce0cf0452748
        IL_0f92:  ldc.i8     0xa8566274aa
        IL_0f9b:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0fa0:  ldc.i8     0xe570484e9937e1
        IL_0fa9:  ldc.i8     0xac81f171be
        IL_0fb2:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0fb7:  ldc.i8     0xeb371f7f8f514e
        IL_0fc0:  ldc.i8     0xdf0248189c
        IL_0fc9:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0fce:  ldc.i8     0x3777a7cc43dfd7
        IL_0fd7:  ldc.i8     0x3a7b8eaf40
        IL_0fe0:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0fe5:  ldc.i8     0xe181db76238786
        IL_0fee:  ldc.i8     0x4126e572a568
        IL_0ff7:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_0ffc:  ldc.i8     0xac1df87977e122
        IL_1005:  ldc.i8     0xe1e8cfde6678
        IL_100e:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_1013:  ldc.i8     0x1c858763a2c23b
        IL_101c:  ldc.i8     0x4ef61f3964f
        IL_1025:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_102a:  ldc.i8     0xbd786bbb71ce46
        IL_1033:  ldc.i8     0x2cda097a464f
        IL_103c:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_1041:  ldc.i8     0xa7a6de21a46360
        IL_104a:  ldc.i8     0x7afda16f98c3
        IL_1053:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_1058:  ldc.i8     0x6fed70a6ccfdf2
        IL_1061:  ldc.i8     0x9771441e8e00e8
        IL_106a:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_106f:  ldc.i8     0x5ad2782dcd5e60
        IL_1078:  ldc.i8     0xd170d518385f6
        IL_1081:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_1086:  ldc.i8     0x1fd67b153bc9b9
        IL_108f:  ldc.i8     0x7b3366dff66c6c
        IL_1098:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_109d:  ldc.i8     0xbf00203beb73f4
        IL_10a6:  ldc.i8     0x693495fefab1c77e
        IL_10af:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_10b4:  ldc.i8     0x2faac1b1b068f8
        IL_10bd:  ldc.i8     0x1cb11cc5c3aaff86
        IL_10c6:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_10cb:  ldc.i8     0xbb63cfbffe7648
        IL_10d4:  ldc.i8     0x84f5b0c583f9e77b
        IL_10dd:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_10e2:  ldc.i8     0x615db89673241c
        IL_10eb:  ldc.i8     0x8de5f125247eba0f
        IL_10f4:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_10f9:  ldc.i8     0x9be183a6b293dffe
        IL_1102:  ldc.i4.s   114
        IL_1104:  conv.i8
        IL_1105:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_110a:  ldc.i8     0xa3df9b76d8a51b19
        IL_1113:  ldc.i4     0xc4
        IL_1118:  conv.i8
        IL_1119:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_111e:  ldc.i8     0xb4cc300f0ea7566d
        IL_1127:  ldc.i4.s   126
        IL_1129:  conv.i8
        IL_112a:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_112f:  ldc.i8     0xfdac12a8e23e16e7
        IL_1138:  ldc.i4.s   21
        IL_113a:  conv.i8
        IL_113b:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_1140:  ldc.i8     0xc0805405aadc0f47
        IL_1149:  ldc.i4     0x19d4
        IL_114e:  conv.i8
        IL_114f:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_1154:  ldc.i8     0x843a391f8d9f8972
        IL_115d:  ldc.i4     0x317a
        IL_1162:  conv.i8
        IL_1163:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_1168:  ldc.i8     0x5a0d124c427ed453
        IL_1171:  ldc.i4     0x34fe
        IL_1176:  conv.i8
        IL_1177:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_117c:  ldc.i8     0x8631150f34008f1b
        IL_1185:  ldc.i4     0x2ecd
        IL_118a:  conv.i8
        IL_118b:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_1190:  ldc.i8     0x3ff4c18715ad3a76
        IL_1199:  ldc.i4     0x72d22a
        IL_119e:  conv.i8
        IL_119f:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_11a4:  ldc.i8     0x3ef93e5a649422bd
        IL_11ad:  ldc.i4     0xdb5c60
        IL_11b2:  conv.i8
        IL_11b3:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_11b8:  ldc.i8     0x6bdd1056ae58fe0e
        IL_11c1:  ldc.i4     0x805c75
        IL_11c6:  conv.i8
        IL_11c7:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_11cc:  ldc.i8     0xeff1fa30f3ad9ded
        IL_11d5:  ldc.i4     0xc83ca
        IL_11da:  conv.i8
        IL_11db:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_11e0:  ldc.i8     0xbbc143ac147e56a9
        IL_11e9:  ldc.i4     0x161179b7
        IL_11ee:  conv.i8
        IL_11ef:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_11f4:  ldc.i8     0x829dde88caa2e45
        IL_11fd:  ldc.i4     0x1443ab62
        IL_1202:  conv.i8
        IL_1203:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_1208:  ldc.i8     0x97ac43ff797a4514
        IL_1211:  ldc.i4     0x33eef42b
        IL_1216:  conv.i8
        IL_1217:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_121c:  ldc.i8     0x703e9cdf96a148aa
        IL_1225:  ldc.i4     0x8e08f3d8
        IL_122a:  conv.u8
        IL_122b:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_1230:  ldc.i8     0x75cbb739b54e2ad6
        IL_1239:  ldc.i8     0x7a8b12628c
        IL_1242:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_1247:  ldc.i8     0x91e42fafe97d638f
        IL_1250:  ldc.i8     0xfbe867c51
        IL_1259:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_125e:  ldc.i8     0x9159d77deec116c1
        IL_1267:  ldc.i8     0x96c0c774fc
        IL_1270:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_1275:  ldc.i8     0xb59dbb4c15761d88
        IL_127e:  ldc.i8     0x4a033a73e7
        IL_1287:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_128c:  ldc.i8     0xab668e9783af9617
        IL_1295:  ldc.i8     0x5aa18404076c
        IL_129e:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_12a3:  ldc.i8     0x54c68e5b5c4127df
        IL_12ac:  ldc.i8     0xf2934fd8dd1f
        IL_12b5:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_12ba:  ldc.i8     0xf490d3936184c9f9
        IL_12c3:  ldc.i8     0x4007477e2110
        IL_12cc:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_12d1:  ldc.i8     0x349e577c9d5c44e2
        IL_12da:  ldc.i8     0xbdb2235af963
        IL_12e3:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_12e8:  ldc.i8     0x58f3ac26cdafde28
        IL_12f1:  ldc.i8     0x17d4f4ade9ec35
        IL_12fa:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_12ff:  ldc.i8     0xa4a263c316d21f4c
        IL_1308:  ldc.i8     0xa7ec1e6fda834b
        IL_1311:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_1316:  ldc.i8     0x6ab14771c448666f
        IL_131f:  ldc.i8     0x5b0f49593c3a27
        IL_1328:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_132d:  ldc.i8     0x15f392c3602aa4f7
        IL_1336:  ldc.i8     0x18af171045f88e
        IL_133f:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_1344:  ldc.i8     0xf17de69c0063f62c
        IL_134d:  ldc.i8     0xee2a164c2c3a46f8
        IL_1356:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_135b:  ldc.i8     0xf34b743eeff8e5c6
        IL_1364:  ldc.i8     0x4f4067f1a0e404ad
        IL_136d:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_1372:  ldc.i8     0xee0296f678756647
        IL_137b:  ldc.i8     0xf1bbfdc6f0280d36
        IL_1384:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_1389:  ldc.i8     0x65c33db0c952b829
        IL_1392:  ldc.i8     0xa7ab9c39dcffbcf3
        IL_139b:  call       void JitTest_addsub_unsigned_il.Test::testNumbers(unsigned int64,
                                                            unsigned int64)
        IL_13a0:  ldstr      "All tests passed."
        IL_13a5:  call       void [System.Console]System.Console::WriteLine(class System.String)
        IL_13aa:  ldc.i4    0x64
        IL_13ab:  stloc.0
        IL_13ac:  leave.s    IL_13b3

      }  // end .try
      catch ['mscorlib']System.DivideByZeroException 
      {
        IL_13ae:  pop
        IL_13af:  ldc.i4.1
        IL_13b0:  stloc.0
        IL_13b1:  leave.s    IL_13b3

      }  // end handler
      IL_13b3:  ldloc.0
      IL_13b4:  ret
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

} // end of namespace 'JitTest_addsub_unsigned_il'

//*********** DISASSEMBLY COMPLETE ***********************
