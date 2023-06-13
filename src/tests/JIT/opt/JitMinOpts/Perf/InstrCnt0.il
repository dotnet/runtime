// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// DEFAULT_MIN_OPTS_INSTR_COUNT  20000
// OPTIONS: opts.MinOpts() == false
// TID ad4: IL Code Size,Instr 20006,19998, Basic Block count   1, Local Variable Num,Ref count   0,  0 for method <Module>.f():int

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly InstrCnt0 {}
.class public auto ansi InstrCnt0 {
.method public static int32  Main() cil managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  // Code size       12 (0xc)
  .maxstack  8
  IL_0000:  call       int32 f()
  IL_0005:  ldc.i4     0x1
  IL_000a:  add
  IL_000b:  ret
} // end of global method Main
}

.method public static int32  f() cil managed
{
  // Code size       39998 (0x9c3e)
  .maxstack  8
  IL_0000:  ldc.i4.m1
  IL_0001:  dup
  IL_0002:  ldc.i4.6
  IL_0003:  sub
  IL_0004:  pop
  IL_0009:  dup
  IL_000a:  ldc.i4.8
  IL_000b:  add
  IL_000c:  pop
  IL_0011:  dup
  IL_0012:  ldc.i4.4
  IL_0013:  and
  IL_0014:  pop
  IL_0019:  dup
  IL_001a:  ldc.i4.6
  IL_001b:  mul
  IL_001c:  pop
  IL_0021:  dup
  IL_0022:  ldc.i4.3
  IL_0023:  and
  IL_0024:  pop
  IL_0029:  dup
  IL_002a:  ldc.i4.4
  IL_002b:  sub
  IL_002c:  pop
  IL_0031:  dup
  IL_0032:  ldc.i4.6
  IL_0033:  shr
  IL_0034:  pop
  IL_0039:  dup
  IL_003a:  ldc.i4.1
  IL_003b:  mul
  IL_003c:  pop
  IL_0041:  dup
  IL_0042:  ldc.i4.8
  IL_0043:  shr
  IL_0044:  pop
  IL_0049:  dup
  IL_004a:  ldc.i4.8
  IL_004b:  shl
  IL_004c:  pop
  IL_0051:  dup
  IL_0052:  ldc.i4.4
  IL_0053:  add
  IL_0054:  pop
  IL_0059:  dup
  IL_005a:  ldc.i4.5
  IL_005b:  and
  IL_005c:  pop
  IL_0061:  dup
  IL_0062:  ldc.i4.1
  IL_0063:  add
  IL_0064:  pop
  IL_0069:  dup
  IL_006a:  ldc.i4.1
  IL_006b:  sub
  IL_006c:  pop
  IL_0071:  dup
  IL_0072:  ldc.i4.8
  IL_0073:  sub
  IL_0074:  pop
  IL_0079:  dup
  IL_007a:  ldc.i4.2
  IL_007b:  or
  IL_007c:  pop
  IL_0081:  dup
  IL_0082:  ldc.i4.0
  IL_0083:  shr
  IL_0084:  pop
  IL_0089:  dup
  IL_008a:  ldc.i4.7
  IL_008b:  sub
  IL_008c:  pop
  IL_0091:  dup
  IL_0092:  ldc.i4.6
  IL_0093:  shl
  IL_0094:  pop
  IL_0099:  dup
  IL_009a:  ldc.i4.1
  IL_009b:  shr
  IL_009c:  pop
  IL_00a1:  dup
  IL_00a2:  ldc.i4.1
  IL_00a3:  sub
  IL_00a4:  pop
  IL_00a9:  dup
  IL_00aa:  ldc.i4.3
  IL_00ab:  sub
  IL_00ac:  pop
  IL_00b1:  dup
  IL_00b2:  ldc.i4.4
  IL_00b3:  add
  IL_00b4:  pop
  IL_00b9:  dup
  IL_00ba:  ldc.i4.3
  IL_00bb:  shr
  IL_00bc:  pop
  IL_00c1:  dup
  IL_00c2:  ldc.i4.7
  IL_00c3:  xor
  IL_00c4:  pop
  IL_00c9:  dup
  IL_00ca:  ldc.i4.8
  IL_00cb:  shr
  IL_00cc:  pop
  IL_00d1:  dup
  IL_00d2:  ldc.i4.7
  IL_00d3:  and
  IL_00d4:  pop
  IL_00d9:  dup
  IL_00da:  ldc.i4.3
  IL_00db:  shr
  IL_00dc:  pop
  IL_00e1:  dup
  IL_00e2:  ldc.i4.5
  IL_00e3:  sub
  IL_00e4:  pop
  IL_00e9:  dup
  IL_00ea:  ldc.i4.7
  IL_00eb:  xor
  IL_00ec:  pop
  IL_00f1:  dup
  IL_00f2:  ldc.i4.5
  IL_00f3:  shl
  IL_00f4:  pop
  IL_00f9:  dup
  IL_00fa:  ldc.i4.3
  IL_00fb:  sub
  IL_00fc:  pop
  IL_0101:  dup
  IL_0102:  ldc.i4.4
  IL_0103:  shl
  IL_0104:  pop
  IL_0109:  dup
  IL_010a:  ldc.i4.1
  IL_010b:  or
  IL_010c:  pop
  IL_0111:  dup
  IL_0112:  ldc.i4.7
  IL_0113:  add
  IL_0114:  pop
  IL_0119:  dup
  IL_011a:  ldc.i4.5
  IL_011b:  mul
  IL_011c:  pop
  IL_0121:  dup
  IL_0122:  ldc.i4.4
  IL_0123:  sub
  IL_0124:  pop
  IL_0129:  dup
  IL_012a:  ldc.i4.3
  IL_012b:  mul
  IL_012c:  pop
  IL_0131:  dup
  IL_0132:  ldc.i4.5
  IL_0133:  xor
  IL_0134:  pop
  IL_0139:  dup
  IL_013a:  ldc.i4.2
  IL_013b:  sub
  IL_013c:  pop
  IL_0141:  dup
  IL_0142:  ldc.i4.6
  IL_0143:  shl
  IL_0144:  pop
  IL_0149:  dup
  IL_014a:  ldc.i4.3
  IL_014b:  xor
  IL_014c:  pop
  IL_0151:  dup
  IL_0152:  ldc.i4.2
  IL_0153:  shl
  IL_0154:  pop
  IL_0159:  dup
  IL_015a:  ldc.i4.0
  IL_015b:  mul
  IL_015c:  pop
  IL_0161:  dup
  IL_0162:  ldc.i4.2
  IL_0163:  add
  IL_0164:  pop
  IL_0169:  dup
  IL_016a:  ldc.i4.4
  IL_016b:  shr
  IL_016c:  pop
  IL_0171:  dup
  IL_0172:  ldc.i4.6
  IL_0173:  and
  IL_0174:  pop
  IL_0179:  dup
  IL_017a:  ldc.i4.1
  IL_017b:  shr
  IL_017c:  pop
  IL_0181:  dup
  IL_0182:  ldc.i4.6
  IL_0183:  sub
  IL_0184:  pop
  IL_0189:  dup
  IL_018a:  ldc.i4.6
  IL_018b:  or
  IL_018c:  pop
  IL_0191:  dup
  IL_0192:  ldc.i4.7
  IL_0193:  xor
  IL_0194:  pop
  IL_0199:  dup
  IL_019a:  ldc.i4.7
  IL_019b:  shr
  IL_019c:  pop
  IL_01a1:  dup
  IL_01a2:  ldc.i4.8
  IL_01a3:  shr
  IL_01a4:  pop
  IL_01a9:  dup
  IL_01aa:  ldc.i4.2
  IL_01ab:  shl
  IL_01ac:  pop
  IL_01b1:  dup
  IL_01b2:  ldc.i4.3
  IL_01b3:  add
  IL_01b4:  pop
  IL_01b9:  dup
  IL_01ba:  ldc.i4.0
  IL_01bb:  sub
  IL_01bc:  pop
  IL_01c1:  dup
  IL_01c2:  ldc.i4.7
  IL_01c3:  shr
  IL_01c4:  pop
  IL_01c9:  dup
  IL_01ca:  ldc.i4.6
  IL_01cb:  add
  IL_01cc:  pop
  IL_01d1:  dup
  IL_01d2:  ldc.i4.0
  IL_01d3:  or
  IL_01d4:  pop
  IL_01d9:  dup
  IL_01da:  ldc.i4.8
  IL_01db:  or
  IL_01dc:  pop
  IL_01e1:  dup
  IL_01e2:  ldc.i4.4
  IL_01e3:  or
  IL_01e4:  pop
  IL_01e9:  dup
  IL_01ea:  ldc.i4.5
  IL_01eb:  and
  IL_01ec:  pop
  IL_01f1:  dup
  IL_01f2:  ldc.i4.4
  IL_01f3:  or
  IL_01f4:  pop
  IL_01f9:  dup
  IL_01fa:  ldc.i4.5
  IL_01fb:  shr
  IL_01fc:  pop
  IL_0201:  dup
  IL_0202:  ldc.i4.0
  IL_0203:  and
  IL_0204:  pop
  IL_0209:  dup
  IL_020a:  ldc.i4.5
  IL_020b:  xor
  IL_020c:  pop
  IL_0211:  dup
  IL_0212:  ldc.i4.6
  IL_0213:  mul
  IL_0214:  pop
  IL_0219:  dup
  IL_021a:  ldc.i4.2
  IL_021b:  mul
  IL_021c:  pop
  IL_0221:  dup
  IL_0222:  ldc.i4.6
  IL_0223:  shr
  IL_0224:  pop
  IL_0229:  dup
  IL_022a:  ldc.i4.5
  IL_022b:  shl
  IL_022c:  pop
  IL_0231:  dup
  IL_0232:  ldc.i4.2
  IL_0233:  shl
  IL_0234:  pop
  IL_0239:  dup
  IL_023a:  ldc.i4.8
  IL_023b:  mul
  IL_023c:  pop
  IL_0241:  dup
  IL_0242:  ldc.i4.8
  IL_0243:  mul
  IL_0244:  pop
  IL_0249:  dup
  IL_024a:  ldc.i4.6
  IL_024b:  add
  IL_024c:  pop
  IL_0251:  dup
  IL_0252:  ldc.i4.0
  IL_0253:  add
  IL_0254:  pop
  IL_0259:  dup
  IL_025a:  ldc.i4.5
  IL_025b:  and
  IL_025c:  pop
  IL_0261:  dup
  IL_0262:  ldc.i4.2
  IL_0263:  shr
  IL_0264:  pop
  IL_0269:  dup
  IL_026a:  ldc.i4.3
  IL_026b:  or
  IL_026c:  pop
  IL_0271:  dup
  IL_0272:  ldc.i4.7
  IL_0273:  or
  IL_0274:  pop
  IL_0279:  dup
  IL_027a:  ldc.i4.2
  IL_027b:  or
  IL_027c:  pop
  IL_0281:  dup
  IL_0282:  ldc.i4.0
  IL_0283:  shl
  IL_0284:  pop
  IL_0289:  dup
  IL_028a:  ldc.i4.4
  IL_028b:  shl
  IL_028c:  pop
  IL_0291:  dup
  IL_0292:  ldc.i4.4
  IL_0293:  shr
  IL_0294:  pop
  IL_0299:  dup
  IL_029a:  ldc.i4.4
  IL_029b:  xor
  IL_029c:  pop
  IL_02a1:  dup
  IL_02a2:  ldc.i4.4
  IL_02a3:  xor
  IL_02a4:  pop
  IL_02a9:  dup
  IL_02aa:  ldc.i4.3
  IL_02ab:  xor
  IL_02ac:  pop
  IL_02b1:  dup
  IL_02b2:  ldc.i4.2
  IL_02b3:  mul
  IL_02b4:  pop
  IL_02b9:  dup
  IL_02ba:  ldc.i4.6
  IL_02bb:  or
  IL_02bc:  pop
  IL_02c1:  dup
  IL_02c2:  ldc.i4.0
  IL_02c3:  mul
  IL_02c4:  pop
  IL_02c9:  dup
  IL_02ca:  ldc.i4.7
  IL_02cb:  sub
  IL_02cc:  pop
  IL_02d1:  dup
  IL_02d2:  ldc.i4.1
  IL_02d3:  shl
  IL_02d4:  pop
  IL_02d9:  dup
  IL_02da:  ldc.i4.6
  IL_02db:  or
  IL_02dc:  pop
  IL_02e1:  dup
  IL_02e2:  ldc.i4.1
  IL_02e3:  and
  IL_02e4:  pop
  IL_02e9:  dup
  IL_02ea:  ldc.i4.1
  IL_02eb:  add
  IL_02ec:  pop
  IL_02f1:  dup
  IL_02f2:  ldc.i4.8
  IL_02f3:  mul
  IL_02f4:  pop
  IL_02f9:  dup
  IL_02fa:  ldc.i4.4
  IL_02fb:  shr
  IL_02fc:  pop
  IL_0301:  dup
  IL_0302:  ldc.i4.4
  IL_0303:  shr
  IL_0304:  pop
  IL_0309:  dup
  IL_030a:  ldc.i4.4
  IL_030b:  and
  IL_030c:  pop
  IL_0311:  dup
  IL_0312:  ldc.i4.0
  IL_0313:  mul
  IL_0314:  pop
  IL_0319:  dup
  IL_031a:  ldc.i4.7
  IL_031b:  mul
  IL_031c:  pop
  IL_0321:  dup
  IL_0322:  ldc.i4.7
  IL_0323:  mul
  IL_0324:  pop
  IL_0329:  dup
  IL_032a:  ldc.i4.5
  IL_032b:  shl
  IL_032c:  pop
  IL_0331:  dup
  IL_0332:  ldc.i4.6
  IL_0333:  sub
  IL_0334:  pop
  IL_0339:  dup
  IL_033a:  ldc.i4.4
  IL_033b:  or
  IL_033c:  pop
  IL_0341:  dup
  IL_0342:  ldc.i4.0
  IL_0343:  xor
  IL_0344:  pop
  IL_0349:  dup
  IL_034a:  ldc.i4.8
  IL_034b:  xor
  IL_034c:  pop
  IL_0351:  dup
  IL_0352:  ldc.i4.0
  IL_0353:  add
  IL_0354:  pop
  IL_0359:  dup
  IL_035a:  ldc.i4.7
  IL_035b:  shl
  IL_035c:  pop
  IL_0361:  dup
  IL_0362:  ldc.i4.7
  IL_0363:  shl
  IL_0364:  pop
  IL_0369:  dup
  IL_036a:  ldc.i4.3
  IL_036b:  shr
  IL_036c:  pop
  IL_0371:  dup
  IL_0372:  ldc.i4.3
  IL_0373:  xor
  IL_0374:  pop
  IL_0379:  dup
  IL_037a:  ldc.i4.4
  IL_037b:  add
  IL_037c:  pop
  IL_0381:  dup
  IL_0382:  ldc.i4.8
  IL_0383:  shl
  IL_0384:  pop
  IL_0389:  dup
  IL_038a:  ldc.i4.8
  IL_038b:  shr
  IL_038c:  pop
  IL_0391:  dup
  IL_0392:  ldc.i4.7
  IL_0393:  or
  IL_0394:  pop
  IL_0399:  dup
  IL_039a:  ldc.i4.8
  IL_039b:  shr
  IL_039c:  pop
  IL_03a1:  dup
  IL_03a2:  ldc.i4.5
  IL_03a3:  shl
  IL_03a4:  pop
  IL_03a9:  dup
  IL_03aa:  ldc.i4.2
  IL_03ab:  and
  IL_03ac:  pop
  IL_03b1:  dup
  IL_03b2:  ldc.i4.8
  IL_03b3:  sub
  IL_03b4:  pop
  IL_03b9:  dup
  IL_03ba:  ldc.i4.2
  IL_03bb:  add
  IL_03bc:  pop
  IL_03c1:  dup
  IL_03c2:  ldc.i4.2
  IL_03c3:  mul
  IL_03c4:  pop
  IL_03c9:  dup
  IL_03ca:  ldc.i4.8
  IL_03cb:  xor
  IL_03cc:  pop
  IL_03d1:  dup
  IL_03d2:  ldc.i4.3
  IL_03d3:  add
  IL_03d4:  pop
  IL_03d9:  dup
  IL_03da:  ldc.i4.5
  IL_03db:  mul
  IL_03dc:  pop
  IL_03e1:  dup
  IL_03e2:  ldc.i4.7
  IL_03e3:  add
  IL_03e4:  pop
  IL_03e9:  dup
  IL_03ea:  ldc.i4.8
  IL_03eb:  shl
  IL_03ec:  pop
  IL_03f1:  dup
  IL_03f2:  ldc.i4.8
  IL_03f3:  add
  IL_03f4:  pop
  IL_03f9:  dup
  IL_03fa:  ldc.i4.8
  IL_03fb:  xor
  IL_03fc:  pop
  IL_0401:  dup
  IL_0402:  ldc.i4.6
  IL_0403:  xor
  IL_0404:  pop
  IL_0409:  dup
  IL_040a:  ldc.i4.5
  IL_040b:  sub
  IL_040c:  pop
  IL_0411:  dup
  IL_0412:  ldc.i4.1
  IL_0413:  and
  IL_0414:  pop
  IL_0419:  dup
  IL_041a:  ldc.i4.1
  IL_041b:  sub
  IL_041c:  pop
  IL_0421:  dup
  IL_0422:  ldc.i4.4
  IL_0423:  mul
  IL_0424:  pop
  IL_0429:  dup
  IL_042a:  ldc.i4.7
  IL_042b:  shl
  IL_042c:  pop
  IL_0431:  dup
  IL_0432:  ldc.i4.7
  IL_0433:  or
  IL_0434:  pop
  IL_0439:  dup
  IL_043a:  ldc.i4.4
  IL_043b:  shr
  IL_043c:  pop
  IL_0441:  dup
  IL_0442:  ldc.i4.2
  IL_0443:  mul
  IL_0444:  pop
  IL_0449:  dup
  IL_044a:  ldc.i4.3
  IL_044b:  add
  IL_044c:  pop
  IL_0451:  dup
  IL_0452:  ldc.i4.3
  IL_0453:  add
  IL_0454:  pop
  IL_0459:  dup
  IL_045a:  ldc.i4.6
  IL_045b:  or
  IL_045c:  pop
  IL_0461:  dup
  IL_0462:  ldc.i4.8
  IL_0463:  shl
  IL_0464:  pop
  IL_0469:  dup
  IL_046a:  ldc.i4.8
  IL_046b:  shr
  IL_046c:  pop
  IL_0471:  dup
  IL_0472:  ldc.i4.8
  IL_0473:  xor
  IL_0474:  pop
  IL_0479:  dup
  IL_047a:  ldc.i4.6
  IL_047b:  xor
  IL_047c:  pop
  IL_0481:  dup
  IL_0482:  ldc.i4.5
  IL_0483:  and
  IL_0484:  pop
  IL_0489:  dup
  IL_048a:  ldc.i4.1
  IL_048b:  shl
  IL_048c:  pop
  IL_0491:  dup
  IL_0492:  ldc.i4.5
  IL_0493:  add
  IL_0494:  pop
  IL_0499:  dup
  IL_049a:  ldc.i4.7
  IL_049b:  xor
  IL_049c:  pop
  IL_04a1:  dup
  IL_04a2:  ldc.i4.1
  IL_04a3:  and
  IL_04a4:  pop
  IL_04a9:  dup
  IL_04aa:  ldc.i4.8
  IL_04ab:  sub
  IL_04ac:  pop
  IL_04b1:  dup
  IL_04b2:  ldc.i4.1
  IL_04b3:  add
  IL_04b4:  pop
  IL_04b9:  dup
  IL_04ba:  ldc.i4.4
  IL_04bb:  and
  IL_04bc:  pop
  IL_04c1:  dup
  IL_04c2:  ldc.i4.4
  IL_04c3:  mul
  IL_04c4:  pop
  IL_04c9:  dup
  IL_04ca:  ldc.i4.1
  IL_04cb:  shr
  IL_04cc:  pop
  IL_04d1:  dup
  IL_04d2:  ldc.i4.6
  IL_04d3:  and
  IL_04d4:  pop
  IL_04d9:  dup
  IL_04da:  ldc.i4.0
  IL_04db:  shr
  IL_04dc:  pop
  IL_04e1:  dup
  IL_04e2:  ldc.i4.6
  IL_04e3:  shr
  IL_04e4:  pop
  IL_04e9:  dup
  IL_04ea:  ldc.i4.1
  IL_04eb:  shl
  IL_04ec:  pop
  IL_04f1:  dup
  IL_04f2:  ldc.i4.8
  IL_04f3:  xor
  IL_04f4:  pop
  IL_04f9:  dup
  IL_04fa:  ldc.i4.1
  IL_04fb:  add
  IL_04fc:  pop
  IL_0501:  dup
  IL_0502:  ldc.i4.5
  IL_0503:  mul
  IL_0504:  pop
  IL_0509:  dup
  IL_050a:  ldc.i4.8
  IL_050b:  add
  IL_050c:  pop
  IL_0511:  dup
  IL_0512:  ldc.i4.7
  IL_0513:  or
  IL_0514:  pop
  IL_0519:  dup
  IL_051a:  ldc.i4.0
  IL_051b:  sub
  IL_051c:  pop
  IL_0521:  dup
  IL_0522:  ldc.i4.3
  IL_0523:  or
  IL_0524:  pop
  IL_0529:  dup
  IL_052a:  ldc.i4.8
  IL_052b:  shl
  IL_052c:  pop
  IL_0531:  dup
  IL_0532:  ldc.i4.0
  IL_0533:  shl
  IL_0534:  pop
  IL_0539:  dup
  IL_053a:  ldc.i4.6
  IL_053b:  or
  IL_053c:  pop
  IL_0541:  dup
  IL_0542:  ldc.i4.1
  IL_0543:  add
  IL_0544:  pop
  IL_0549:  dup
  IL_054a:  ldc.i4.2
  IL_054b:  shr
  IL_054c:  pop
  IL_0551:  dup
  IL_0552:  ldc.i4.1
  IL_0553:  or
  IL_0554:  pop
  IL_0559:  dup
  IL_055a:  ldc.i4.7
  IL_055b:  and
  IL_055c:  pop
  IL_0561:  dup
  IL_0562:  ldc.i4.7
  IL_0563:  xor
  IL_0564:  pop
  IL_0569:  dup
  IL_056a:  ldc.i4.7
  IL_056b:  or
  IL_056c:  pop
  IL_0571:  dup
  IL_0572:  ldc.i4.7
  IL_0573:  mul
  IL_0574:  pop
  IL_0579:  dup
  IL_057a:  ldc.i4.5
  IL_057b:  and
  IL_057c:  pop
  IL_0581:  dup
  IL_0582:  ldc.i4.6
  IL_0583:  or
  IL_0584:  pop
  IL_0589:  dup
  IL_058a:  ldc.i4.4
  IL_058b:  and
  IL_058c:  pop
  IL_0591:  dup
  IL_0592:  ldc.i4.1
  IL_0593:  xor
  IL_0594:  pop
  IL_0599:  dup
  IL_059a:  ldc.i4.8
  IL_059b:  shl
  IL_059c:  pop
  IL_05a1:  dup
  IL_05a2:  ldc.i4.1
  IL_05a3:  or
  IL_05a4:  pop
  IL_05a9:  dup
  IL_05aa:  ldc.i4.2
  IL_05ab:  and
  IL_05ac:  pop
  IL_05b1:  dup
  IL_05b2:  ldc.i4.8
  IL_05b3:  xor
  IL_05b4:  pop
  IL_05b9:  dup
  IL_05ba:  ldc.i4.5
  IL_05bb:  sub
  IL_05bc:  pop
  IL_05c1:  dup
  IL_05c2:  ldc.i4.8
  IL_05c3:  mul
  IL_05c4:  pop
  IL_05c9:  dup
  IL_05ca:  ldc.i4.4
  IL_05cb:  and
  IL_05cc:  pop
  IL_05d1:  dup
  IL_05d2:  ldc.i4.1
  IL_05d3:  shl
  IL_05d4:  pop
  IL_05d9:  dup
  IL_05da:  ldc.i4.7
  IL_05db:  shl
  IL_05dc:  pop
  IL_05e1:  dup
  IL_05e2:  ldc.i4.5
  IL_05e3:  add
  IL_05e4:  pop
  IL_05e9:  dup
  IL_05ea:  ldc.i4.2
  IL_05eb:  xor
  IL_05ec:  pop
  IL_05f1:  dup
  IL_05f2:  ldc.i4.1
  IL_05f3:  sub
  IL_05f4:  pop
  IL_05f9:  dup
  IL_05fa:  ldc.i4.3
  IL_05fb:  sub
  IL_05fc:  pop
  IL_0601:  dup
  IL_0602:  ldc.i4.3
  IL_0603:  add
  IL_0604:  pop
  IL_0609:  dup
  IL_060a:  ldc.i4.7
  IL_060b:  sub
  IL_060c:  pop
  IL_0611:  dup
  IL_0612:  ldc.i4.0
  IL_0613:  or
  IL_0614:  pop
  IL_0619:  dup
  IL_061a:  ldc.i4.6
  IL_061b:  shr
  IL_061c:  pop
  IL_0621:  dup
  IL_0622:  ldc.i4.0
  IL_0623:  shr
  IL_0624:  pop
  IL_0629:  dup
  IL_062a:  ldc.i4.4
  IL_062b:  sub
  IL_062c:  pop
  IL_0631:  dup
  IL_0632:  ldc.i4.6
  IL_0633:  or
  IL_0634:  pop
  IL_0639:  dup
  IL_063a:  ldc.i4.8
  IL_063b:  mul
  IL_063c:  pop
  IL_0641:  dup
  IL_0642:  ldc.i4.7
  IL_0643:  shl
  IL_0644:  pop
  IL_0649:  dup
  IL_064a:  ldc.i4.8
  IL_064b:  shl
  IL_064c:  pop
  IL_0651:  dup
  IL_0652:  ldc.i4.7
  IL_0653:  shr
  IL_0654:  pop
  IL_0659:  dup
  IL_065a:  ldc.i4.6
  IL_065b:  sub
  IL_065c:  pop
  IL_0661:  dup
  IL_0662:  ldc.i4.0
  IL_0663:  or
  IL_0664:  pop
  IL_0669:  dup
  IL_066a:  ldc.i4.2
  IL_066b:  add
  IL_066c:  pop
  IL_0671:  dup
  IL_0672:  ldc.i4.1
  IL_0673:  shl
  IL_0674:  pop
  IL_0679:  dup
  IL_067a:  ldc.i4.2
  IL_067b:  add
  IL_067c:  pop
  IL_0681:  dup
  IL_0682:  ldc.i4.5
  IL_0683:  add
  IL_0684:  pop
  IL_0689:  dup
  IL_068a:  ldc.i4.3
  IL_068b:  and
  IL_068c:  pop
  IL_0691:  dup
  IL_0692:  ldc.i4.5
  IL_0693:  xor
  IL_0694:  pop
  IL_0699:  dup
  IL_069a:  ldc.i4.1
  IL_069b:  add
  IL_069c:  pop
  IL_06a1:  dup
  IL_06a2:  ldc.i4.5
  IL_06a3:  shr
  IL_06a4:  pop
  IL_06a9:  dup
  IL_06aa:  ldc.i4.7
  IL_06ab:  xor
  IL_06ac:  pop
  IL_06b1:  dup
  IL_06b2:  ldc.i4.0
  IL_06b3:  shr
  IL_06b4:  pop
  IL_06b9:  dup
  IL_06ba:  ldc.i4.7
  IL_06bb:  shl
  IL_06bc:  pop
  IL_06c1:  dup
  IL_06c2:  ldc.i4.2
  IL_06c3:  or
  IL_06c4:  pop
  IL_06c9:  dup
  IL_06ca:  ldc.i4.5
  IL_06cb:  shl
  IL_06cc:  pop
  IL_06d1:  dup
  IL_06d2:  ldc.i4.1
  IL_06d3:  and
  IL_06d4:  pop
  IL_06d9:  dup
  IL_06da:  ldc.i4.3
  IL_06db:  shr
  IL_06dc:  pop
  IL_06e1:  dup
  IL_06e2:  ldc.i4.3
  IL_06e3:  shr
  IL_06e4:  pop
  IL_06e9:  dup
  IL_06ea:  ldc.i4.1
  IL_06eb:  add
  IL_06ec:  pop
  IL_06f1:  dup
  IL_06f2:  ldc.i4.6
  IL_06f3:  shr
  IL_06f4:  pop
  IL_06f9:  dup
  IL_06fa:  ldc.i4.4
  IL_06fb:  xor
  IL_06fc:  pop
  IL_0701:  dup
  IL_0702:  ldc.i4.3
  IL_0703:  xor
  IL_0704:  pop
  IL_0709:  dup
  IL_070a:  ldc.i4.5
  IL_070b:  shr
  IL_070c:  pop
  IL_0711:  dup
  IL_0712:  ldc.i4.5
  IL_0713:  add
  IL_0714:  pop
  IL_0719:  dup
  IL_071a:  ldc.i4.7
  IL_071b:  mul
  IL_071c:  pop
  IL_0721:  dup
  IL_0722:  ldc.i4.3
  IL_0723:  or
  IL_0724:  pop
  IL_0729:  dup
  IL_072a:  ldc.i4.8
  IL_072b:  add
  IL_072c:  pop
  IL_0731:  dup
  IL_0732:  ldc.i4.8
  IL_0733:  sub
  IL_0734:  pop
  IL_0739:  dup
  IL_073a:  ldc.i4.0
  IL_073b:  mul
  IL_073c:  pop
  IL_0741:  dup
  IL_0742:  ldc.i4.0
  IL_0743:  shr
  IL_0744:  pop
  IL_0749:  dup
  IL_074a:  ldc.i4.7
  IL_074b:  mul
  IL_074c:  pop
  IL_0751:  dup
  IL_0752:  ldc.i4.8
  IL_0753:  shr
  IL_0754:  pop
  IL_0759:  dup
  IL_075a:  ldc.i4.7
  IL_075b:  and
  IL_075c:  pop
  IL_0761:  dup
  IL_0762:  ldc.i4.6
  IL_0763:  and
  IL_0764:  pop
  IL_0769:  dup
  IL_076a:  ldc.i4.3
  IL_076b:  and
  IL_076c:  pop
  IL_0771:  dup
  IL_0772:  ldc.i4.3
  IL_0773:  add
  IL_0774:  pop
  IL_0779:  dup
  IL_077a:  ldc.i4.3
  IL_077b:  and
  IL_077c:  pop
  IL_0781:  dup
  IL_0782:  ldc.i4.1
  IL_0783:  mul
  IL_0784:  pop
  IL_0789:  dup
  IL_078a:  ldc.i4.0
  IL_078b:  shr
  IL_078c:  pop
  IL_0791:  dup
  IL_0792:  ldc.i4.0
  IL_0793:  sub
  IL_0794:  pop
  IL_0799:  dup
  IL_079a:  ldc.i4.0
  IL_079b:  sub
  IL_079c:  pop
  IL_07a1:  dup
  IL_07a2:  ldc.i4.0
  IL_07a3:  mul
  IL_07a4:  pop
  IL_07a9:  dup
  IL_07aa:  ldc.i4.1
  IL_07ab:  sub
  IL_07ac:  pop
  IL_07b1:  dup
  IL_07b2:  ldc.i4.7
  IL_07b3:  mul
  IL_07b4:  pop
  IL_07b9:  dup
  IL_07ba:  ldc.i4.6
  IL_07bb:  sub
  IL_07bc:  pop
  IL_07c1:  dup
  IL_07c2:  ldc.i4.5
  IL_07c3:  shr
  IL_07c4:  pop
  IL_07c9:  dup
  IL_07ca:  ldc.i4.8
  IL_07cb:  add
  IL_07cc:  pop
  IL_07d1:  dup
  IL_07d2:  ldc.i4.8
  IL_07d3:  sub
  IL_07d4:  pop
  IL_07d9:  dup
  IL_07da:  ldc.i4.4
  IL_07db:  and
  IL_07dc:  pop
  IL_07e1:  dup
  IL_07e2:  ldc.i4.4
  IL_07e3:  shr
  IL_07e4:  pop
  IL_07e9:  dup
  IL_07ea:  ldc.i4.8
  IL_07eb:  and
  IL_07ec:  pop
  IL_07f1:  dup
  IL_07f2:  ldc.i4.5
  IL_07f3:  or
  IL_07f4:  pop
  IL_07f9:  dup
  IL_07fa:  ldc.i4.8
  IL_07fb:  mul
  IL_07fc:  pop
  IL_0801:  dup
  IL_0802:  ldc.i4.5
  IL_0803:  sub
  IL_0804:  pop
  IL_0809:  dup
  IL_080a:  ldc.i4.7
  IL_080b:  shl
  IL_080c:  pop
  IL_0811:  dup
  IL_0812:  ldc.i4.2
  IL_0813:  xor
  IL_0814:  pop
  IL_0819:  dup
  IL_081a:  ldc.i4.2
  IL_081b:  and
  IL_081c:  pop
  IL_0821:  dup
  IL_0822:  ldc.i4.5
  IL_0823:  shr
  IL_0824:  pop
  IL_0829:  dup
  IL_082a:  ldc.i4.2
  IL_082b:  shl
  IL_082c:  pop
  IL_0831:  dup
  IL_0832:  ldc.i4.4
  IL_0833:  shr
  IL_0834:  pop
  IL_0839:  dup
  IL_083a:  ldc.i4.0
  IL_083b:  xor
  IL_083c:  pop
  IL_0841:  dup
  IL_0842:  ldc.i4.0
  IL_0843:  mul
  IL_0844:  pop
  IL_0849:  dup
  IL_084a:  ldc.i4.2
  IL_084b:  shr
  IL_084c:  pop
  IL_0851:  dup
  IL_0852:  ldc.i4.1
  IL_0853:  mul
  IL_0854:  pop
  IL_0859:  dup
  IL_085a:  ldc.i4.8
  IL_085b:  or
  IL_085c:  pop
  IL_0861:  dup
  IL_0862:  ldc.i4.7
  IL_0863:  mul
  IL_0864:  pop
  IL_0869:  dup
  IL_086a:  ldc.i4.1
  IL_086b:  xor
  IL_086c:  pop
  IL_0871:  dup
  IL_0872:  ldc.i4.2
  IL_0873:  sub
  IL_0874:  pop
  IL_0879:  dup
  IL_087a:  ldc.i4.4
  IL_087b:  sub
  IL_087c:  pop
  IL_0881:  dup
  IL_0882:  ldc.i4.4
  IL_0883:  and
  IL_0884:  pop
  IL_0889:  dup
  IL_088a:  ldc.i4.5
  IL_088b:  shr
  IL_088c:  pop
  IL_0891:  dup
  IL_0892:  ldc.i4.4
  IL_0893:  sub
  IL_0894:  pop
  IL_0899:  dup
  IL_089a:  ldc.i4.8
  IL_089b:  sub
  IL_089c:  pop
  IL_08a1:  dup
  IL_08a2:  ldc.i4.4
  IL_08a3:  sub
  IL_08a4:  pop
  IL_08a9:  dup
  IL_08aa:  ldc.i4.3
  IL_08ab:  mul
  IL_08ac:  pop
  IL_08b1:  dup
  IL_08b2:  ldc.i4.0
  IL_08b3:  or
  IL_08b4:  pop
  IL_08b9:  dup
  IL_08ba:  ldc.i4.2
  IL_08bb:  and
  IL_08bc:  pop
  IL_08c1:  dup
  IL_08c2:  ldc.i4.5
  IL_08c3:  shl
  IL_08c4:  pop
  IL_08c9:  dup
  IL_08ca:  ldc.i4.7
  IL_08cb:  xor
  IL_08cc:  pop
  IL_08d1:  dup
  IL_08d2:  ldc.i4.6
  IL_08d3:  mul
  IL_08d4:  pop
  IL_08d9:  dup
  IL_08da:  ldc.i4.1
  IL_08db:  mul
  IL_08dc:  pop
  IL_08e1:  dup
  IL_08e2:  ldc.i4.1
  IL_08e3:  and
  IL_08e4:  pop
  IL_08e9:  dup
  IL_08ea:  ldc.i4.7
  IL_08eb:  or
  IL_08ec:  pop
  IL_08f1:  dup
  IL_08f2:  ldc.i4.2
  IL_08f3:  and
  IL_08f4:  pop
  IL_08f9:  dup
  IL_08fa:  ldc.i4.5
  IL_08fb:  xor
  IL_08fc:  pop
  IL_0901:  dup
  IL_0902:  ldc.i4.1
  IL_0903:  add
  IL_0904:  pop
  IL_0909:  dup
  IL_090a:  ldc.i4.1
  IL_090b:  shl
  IL_090c:  pop
  IL_0911:  dup
  IL_0912:  ldc.i4.0
  IL_0913:  xor
  IL_0914:  pop
  IL_0919:  dup
  IL_091a:  ldc.i4.8
  IL_091b:  and
  IL_091c:  pop
  IL_0921:  dup
  IL_0922:  ldc.i4.2
  IL_0923:  add
  IL_0924:  pop
  IL_0929:  dup
  IL_092a:  ldc.i4.1
  IL_092b:  or
  IL_092c:  pop
  IL_0931:  dup
  IL_0932:  ldc.i4.8
  IL_0933:  shr
  IL_0934:  pop
  IL_0939:  dup
  IL_093a:  ldc.i4.8
  IL_093b:  and
  IL_093c:  pop
  IL_0941:  dup
  IL_0942:  ldc.i4.8
  IL_0943:  mul
  IL_0944:  pop
  IL_0949:  dup
  IL_094a:  ldc.i4.7
  IL_094b:  shr
  IL_094c:  pop
  IL_0951:  dup
  IL_0952:  ldc.i4.3
  IL_0953:  add
  IL_0954:  pop
  IL_0959:  dup
  IL_095a:  ldc.i4.4
  IL_095b:  sub
  IL_095c:  pop
  IL_0961:  dup
  IL_0962:  ldc.i4.5
  IL_0963:  mul
  IL_0964:  pop
  IL_0969:  dup
  IL_096a:  ldc.i4.4
  IL_096b:  shl
  IL_096c:  pop
  IL_0971:  dup
  IL_0972:  ldc.i4.3
  IL_0973:  or
  IL_0974:  pop
  IL_0979:  dup
  IL_097a:  ldc.i4.8
  IL_097b:  shr
  IL_097c:  pop
  IL_0981:  dup
  IL_0982:  ldc.i4.5
  IL_0983:  add
  IL_0984:  pop
  IL_0989:  dup
  IL_098a:  ldc.i4.1
  IL_098b:  or
  IL_098c:  pop
  IL_0991:  dup
  IL_0992:  ldc.i4.7
  IL_0993:  shr
  IL_0994:  pop
  IL_0999:  dup
  IL_099a:  ldc.i4.6
  IL_099b:  sub
  IL_099c:  pop
  IL_09a1:  dup
  IL_09a2:  ldc.i4.5
  IL_09a3:  or
  IL_09a4:  pop
  IL_09a9:  dup
  IL_09aa:  ldc.i4.0
  IL_09ab:  shl
  IL_09ac:  pop
  IL_09b1:  dup
  IL_09b2:  ldc.i4.0
  IL_09b3:  sub
  IL_09b4:  pop
  IL_09b9:  dup
  IL_09ba:  ldc.i4.3
  IL_09bb:  shl
  IL_09bc:  pop
  IL_09c1:  dup
  IL_09c2:  ldc.i4.7
  IL_09c3:  or
  IL_09c4:  pop
  IL_09c9:  dup
  IL_09ca:  ldc.i4.0
  IL_09cb:  or
  IL_09cc:  pop
  IL_09d1:  dup
  IL_09d2:  ldc.i4.0
  IL_09d3:  and
  IL_09d4:  pop
  IL_09d9:  dup
  IL_09da:  ldc.i4.8
  IL_09db:  add
  IL_09dc:  pop
  IL_09e1:  dup
  IL_09e2:  ldc.i4.5
  IL_09e3:  or
  IL_09e4:  pop
  IL_09e9:  dup
  IL_09ea:  ldc.i4.8
  IL_09eb:  and
  IL_09ec:  pop
  IL_09f1:  dup
  IL_09f2:  ldc.i4.8
  IL_09f3:  mul
  IL_09f4:  pop
  IL_09f9:  dup
  IL_09fa:  ldc.i4.4
  IL_09fb:  xor
  IL_09fc:  pop
  IL_0a01:  dup
  IL_0a02:  ldc.i4.1
  IL_0a03:  shl
  IL_0a04:  pop
  IL_0a09:  dup
  IL_0a0a:  ldc.i4.3
  IL_0a0b:  or
  IL_0a0c:  pop
  IL_0a11:  dup
  IL_0a12:  ldc.i4.4
  IL_0a13:  add
  IL_0a14:  pop
  IL_0a19:  dup
  IL_0a1a:  ldc.i4.0
  IL_0a1b:  mul
  IL_0a1c:  pop
  IL_0a21:  dup
  IL_0a22:  ldc.i4.5
  IL_0a23:  shl
  IL_0a24:  pop
  IL_0a29:  dup
  IL_0a2a:  ldc.i4.1
  IL_0a2b:  xor
  IL_0a2c:  pop
  IL_0a31:  dup
  IL_0a32:  ldc.i4.0
  IL_0a33:  or
  IL_0a34:  pop
  IL_0a39:  dup
  IL_0a3a:  ldc.i4.0
  IL_0a3b:  xor
  IL_0a3c:  pop
  IL_0a41:  dup
  IL_0a42:  ldc.i4.8
  IL_0a43:  or
  IL_0a44:  pop
  IL_0a49:  dup
  IL_0a4a:  ldc.i4.6
  IL_0a4b:  xor
  IL_0a4c:  pop
  IL_0a51:  dup
  IL_0a52:  ldc.i4.8
  IL_0a53:  add
  IL_0a54:  pop
  IL_0a59:  dup
  IL_0a5a:  ldc.i4.5
  IL_0a5b:  shr
  IL_0a5c:  pop
  IL_0a61:  dup
  IL_0a62:  ldc.i4.0
  IL_0a63:  sub
  IL_0a64:  pop
  IL_0a69:  dup
  IL_0a6a:  ldc.i4.2
  IL_0a6b:  and
  IL_0a6c:  pop
  IL_0a71:  dup
  IL_0a72:  ldc.i4.6
  IL_0a73:  or
  IL_0a74:  pop
  IL_0a79:  dup
  IL_0a7a:  ldc.i4.2
  IL_0a7b:  sub
  IL_0a7c:  pop
  IL_0a81:  dup
  IL_0a82:  ldc.i4.4
  IL_0a83:  shr
  IL_0a84:  pop
  IL_0a89:  dup
  IL_0a8a:  ldc.i4.3
  IL_0a8b:  shl
  IL_0a8c:  pop
  IL_0a91:  dup
  IL_0a92:  ldc.i4.7
  IL_0a93:  shl
  IL_0a94:  pop
  IL_0a99:  dup
  IL_0a9a:  ldc.i4.0
  IL_0a9b:  and
  IL_0a9c:  pop
  IL_0aa1:  dup
  IL_0aa2:  ldc.i4.7
  IL_0aa3:  shr
  IL_0aa4:  pop
  IL_0aa9:  dup
  IL_0aaa:  ldc.i4.2
  IL_0aab:  xor
  IL_0aac:  pop
  IL_0ab1:  dup
  IL_0ab2:  ldc.i4.6
  IL_0ab3:  xor
  IL_0ab4:  pop
  IL_0ab9:  dup
  IL_0aba:  ldc.i4.8
  IL_0abb:  shr
  IL_0abc:  pop
  IL_0ac1:  dup
  IL_0ac2:  ldc.i4.7
  IL_0ac3:  shl
  IL_0ac4:  pop
  IL_0ac9:  dup
  IL_0aca:  ldc.i4.8
  IL_0acb:  add
  IL_0acc:  pop
  IL_0ad1:  dup
  IL_0ad2:  ldc.i4.5
  IL_0ad3:  add
  IL_0ad4:  pop
  IL_0ad9:  dup
  IL_0ada:  ldc.i4.4
  IL_0adb:  sub
  IL_0adc:  pop
  IL_0ae1:  dup
  IL_0ae2:  ldc.i4.8
  IL_0ae3:  shr
  IL_0ae4:  pop
  IL_0ae9:  dup
  IL_0aea:  ldc.i4.7
  IL_0aeb:  mul
  IL_0aec:  pop
  IL_0af1:  dup
  IL_0af2:  ldc.i4.7
  IL_0af3:  sub
  IL_0af4:  pop
  IL_0af9:  dup
  IL_0afa:  ldc.i4.5
  IL_0afb:  and
  IL_0afc:  pop
  IL_0b01:  dup
  IL_0b02:  ldc.i4.2
  IL_0b03:  or
  IL_0b04:  pop
  IL_0b09:  dup
  IL_0b0a:  ldc.i4.0
  IL_0b0b:  xor
  IL_0b0c:  pop
  IL_0b11:  dup
  IL_0b12:  ldc.i4.5
  IL_0b13:  xor
  IL_0b14:  pop
  IL_0b19:  dup
  IL_0b1a:  ldc.i4.5
  IL_0b1b:  xor
  IL_0b1c:  pop
  IL_0b21:  dup
  IL_0b22:  ldc.i4.7
  IL_0b23:  or
  IL_0b24:  pop
  IL_0b29:  dup
  IL_0b2a:  ldc.i4.4
  IL_0b2b:  mul
  IL_0b2c:  pop
  IL_0b31:  dup
  IL_0b32:  ldc.i4.6
  IL_0b33:  add
  IL_0b34:  pop
  IL_0b39:  dup
  IL_0b3a:  ldc.i4.0
  IL_0b3b:  shl
  IL_0b3c:  pop
  IL_0b41:  dup
  IL_0b42:  ldc.i4.1
  IL_0b43:  shl
  IL_0b44:  pop
  IL_0b49:  dup
  IL_0b4a:  ldc.i4.8
  IL_0b4b:  sub
  IL_0b4c:  pop
  IL_0b51:  dup
  IL_0b52:  ldc.i4.8
  IL_0b53:  mul
  IL_0b54:  pop
  IL_0b59:  dup
  IL_0b5a:  ldc.i4.5
  IL_0b5b:  xor
  IL_0b5c:  pop
  IL_0b61:  dup
  IL_0b62:  ldc.i4.1
  IL_0b63:  sub
  IL_0b64:  pop
  IL_0b69:  dup
  IL_0b6a:  ldc.i4.6
  IL_0b6b:  add
  IL_0b6c:  pop
  IL_0b71:  dup
  IL_0b72:  ldc.i4.5
  IL_0b73:  shr
  IL_0b74:  pop
  IL_0b79:  dup
  IL_0b7a:  ldc.i4.0
  IL_0b7b:  shr
  IL_0b7c:  pop
  IL_0b81:  dup
  IL_0b82:  ldc.i4.0
  IL_0b83:  xor
  IL_0b84:  pop
  IL_0b89:  dup
  IL_0b8a:  ldc.i4.2
  IL_0b8b:  and
  IL_0b8c:  pop
  IL_0b91:  dup
  IL_0b92:  ldc.i4.3
  IL_0b93:  shl
  IL_0b94:  pop
  IL_0b99:  dup
  IL_0b9a:  ldc.i4.1
  IL_0b9b:  mul
  IL_0b9c:  pop
  IL_0ba1:  dup
  IL_0ba2:  ldc.i4.8
  IL_0ba3:  shl
  IL_0ba4:  pop
  IL_0ba9:  dup
  IL_0baa:  ldc.i4.1
  IL_0bab:  shr
  IL_0bac:  pop
  IL_0bb1:  dup
  IL_0bb2:  ldc.i4.4
  IL_0bb3:  sub
  IL_0bb4:  pop
  IL_0bb9:  dup
  IL_0bba:  ldc.i4.3
  IL_0bbb:  and
  IL_0bbc:  pop
  IL_0bc1:  dup
  IL_0bc2:  ldc.i4.6
  IL_0bc3:  mul
  IL_0bc4:  pop
  IL_0bc9:  dup
  IL_0bca:  ldc.i4.1
  IL_0bcb:  sub
  IL_0bcc:  pop
  IL_0bd1:  dup
  IL_0bd2:  ldc.i4.2
  IL_0bd3:  xor
  IL_0bd4:  pop
  IL_0bd9:  dup
  IL_0bda:  ldc.i4.6
  IL_0bdb:  or
  IL_0bdc:  pop
  IL_0be1:  dup
  IL_0be2:  ldc.i4.2
  IL_0be3:  shr
  IL_0be4:  pop
  IL_0be9:  dup
  IL_0bea:  ldc.i4.1
  IL_0beb:  xor
  IL_0bec:  pop
  IL_0bf1:  dup
  IL_0bf2:  ldc.i4.7
  IL_0bf3:  shl
  IL_0bf4:  pop
  IL_0bf9:  dup
  IL_0bfa:  ldc.i4.6
  IL_0bfb:  add
  IL_0bfc:  pop
  IL_0c01:  dup
  IL_0c02:  ldc.i4.8
  IL_0c03:  or
  IL_0c04:  pop
  IL_0c09:  dup
  IL_0c0a:  ldc.i4.1
  IL_0c0b:  mul
  IL_0c0c:  pop
  IL_0c11:  dup
  IL_0c12:  ldc.i4.7
  IL_0c13:  sub
  IL_0c14:  pop
  IL_0c19:  dup
  IL_0c1a:  ldc.i4.1
  IL_0c1b:  shr
  IL_0c1c:  pop
  IL_0c21:  dup
  IL_0c22:  ldc.i4.3
  IL_0c23:  shl
  IL_0c24:  pop
  IL_0c29:  dup
  IL_0c2a:  ldc.i4.2
  IL_0c2b:  xor
  IL_0c2c:  pop
  IL_0c31:  dup
  IL_0c32:  ldc.i4.0
  IL_0c33:  or
  IL_0c34:  pop
  IL_0c39:  dup
  IL_0c3a:  ldc.i4.8
  IL_0c3b:  or
  IL_0c3c:  pop
  IL_0c41:  dup
  IL_0c42:  ldc.i4.6
  IL_0c43:  and
  IL_0c44:  pop
  IL_0c49:  dup
  IL_0c4a:  ldc.i4.3
  IL_0c4b:  xor
  IL_0c4c:  pop
  IL_0c51:  dup
  IL_0c52:  ldc.i4.5
  IL_0c53:  and
  IL_0c54:  pop
  IL_0c59:  dup
  IL_0c5a:  ldc.i4.3
  IL_0c5b:  shl
  IL_0c5c:  pop
  IL_0c61:  dup
  IL_0c62:  ldc.i4.6
  IL_0c63:  sub
  IL_0c64:  pop
  IL_0c69:  dup
  IL_0c6a:  ldc.i4.4
  IL_0c6b:  and
  IL_0c6c:  pop
  IL_0c71:  dup
  IL_0c72:  ldc.i4.2
  IL_0c73:  and
  IL_0c74:  pop
  IL_0c79:  dup
  IL_0c7a:  ldc.i4.4
  IL_0c7b:  xor
  IL_0c7c:  pop
  IL_0c81:  dup
  IL_0c82:  ldc.i4.6
  IL_0c83:  add
  IL_0c84:  pop
  IL_0c89:  dup
  IL_0c8a:  ldc.i4.7
  IL_0c8b:  shl
  IL_0c8c:  pop
  IL_0c91:  dup
  IL_0c92:  ldc.i4.8
  IL_0c93:  add
  IL_0c94:  pop
  IL_0c99:  dup
  IL_0c9a:  ldc.i4.8
  IL_0c9b:  shl
  IL_0c9c:  pop
  IL_0ca1:  dup
  IL_0ca2:  ldc.i4.0
  IL_0ca3:  xor
  IL_0ca4:  pop
  IL_0ca9:  dup
  IL_0caa:  ldc.i4.7
  IL_0cab:  shl
  IL_0cac:  pop
  IL_0cb1:  dup
  IL_0cb2:  ldc.i4.2
  IL_0cb3:  mul
  IL_0cb4:  pop
  IL_0cb9:  dup
  IL_0cba:  ldc.i4.8
  IL_0cbb:  xor
  IL_0cbc:  pop
  IL_0cc1:  dup
  IL_0cc2:  ldc.i4.6
  IL_0cc3:  sub
  IL_0cc4:  pop
  IL_0cc9:  dup
  IL_0cca:  ldc.i4.5
  IL_0ccb:  or
  IL_0ccc:  pop
  IL_0cd1:  dup
  IL_0cd2:  ldc.i4.1
  IL_0cd3:  mul
  IL_0cd4:  pop
  IL_0cd9:  dup
  IL_0cda:  ldc.i4.4
  IL_0cdb:  sub
  IL_0cdc:  pop
  IL_0ce1:  dup
  IL_0ce2:  ldc.i4.4
  IL_0ce3:  shr
  IL_0ce4:  pop
  IL_0ce9:  dup
  IL_0cea:  ldc.i4.5
  IL_0ceb:  shr
  IL_0cec:  pop
  IL_0cf1:  dup
  IL_0cf2:  ldc.i4.5
  IL_0cf3:  mul
  IL_0cf4:  pop
  IL_0cf9:  dup
  IL_0cfa:  ldc.i4.3
  IL_0cfb:  shr
  IL_0cfc:  pop
  IL_0d01:  dup
  IL_0d02:  ldc.i4.0
  IL_0d03:  and
  IL_0d04:  pop
  IL_0d09:  dup
  IL_0d0a:  ldc.i4.6
  IL_0d0b:  or
  IL_0d0c:  pop
  IL_0d11:  dup
  IL_0d12:  ldc.i4.1
  IL_0d13:  mul
  IL_0d14:  pop
  IL_0d19:  dup
  IL_0d1a:  ldc.i4.0
  IL_0d1b:  shr
  IL_0d1c:  pop
  IL_0d21:  dup
  IL_0d22:  ldc.i4.8
  IL_0d23:  or
  IL_0d24:  pop
  IL_0d29:  dup
  IL_0d2a:  ldc.i4.2
  IL_0d2b:  sub
  IL_0d2c:  pop
  IL_0d31:  dup
  IL_0d32:  ldc.i4.8
  IL_0d33:  and
  IL_0d34:  pop
  IL_0d39:  dup
  IL_0d3a:  ldc.i4.2
  IL_0d3b:  xor
  IL_0d3c:  pop
  IL_0d41:  dup
  IL_0d42:  ldc.i4.2
  IL_0d43:  sub
  IL_0d44:  pop
  IL_0d49:  dup
  IL_0d4a:  ldc.i4.0
  IL_0d4b:  add
  IL_0d4c:  pop
  IL_0d51:  dup
  IL_0d52:  ldc.i4.1
  IL_0d53:  shl
  IL_0d54:  pop
  IL_0d59:  dup
  IL_0d5a:  ldc.i4.2
  IL_0d5b:  xor
  IL_0d5c:  pop
  IL_0d61:  dup
  IL_0d62:  ldc.i4.5
  IL_0d63:  shr
  IL_0d64:  pop
  IL_0d69:  dup
  IL_0d6a:  ldc.i4.1
  IL_0d6b:  mul
  IL_0d6c:  pop
  IL_0d71:  dup
  IL_0d72:  ldc.i4.4
  IL_0d73:  mul
  IL_0d74:  pop
  IL_0d79:  dup
  IL_0d7a:  ldc.i4.7
  IL_0d7b:  shr
  IL_0d7c:  pop
  IL_0d81:  dup
  IL_0d82:  ldc.i4.0
  IL_0d83:  sub
  IL_0d84:  pop
  IL_0d89:  dup
  IL_0d8a:  ldc.i4.4
  IL_0d8b:  xor
  IL_0d8c:  pop
  IL_0d91:  dup
  IL_0d92:  ldc.i4.5
  IL_0d93:  or
  IL_0d94:  pop
  IL_0d99:  dup
  IL_0d9a:  ldc.i4.2
  IL_0d9b:  xor
  IL_0d9c:  pop
  IL_0da1:  dup
  IL_0da2:  ldc.i4.1
  IL_0da3:  add
  IL_0da4:  pop
  IL_0da9:  dup
  IL_0daa:  ldc.i4.0
  IL_0dab:  sub
  IL_0dac:  pop
  IL_0db1:  dup
  IL_0db2:  ldc.i4.0
  IL_0db3:  mul
  IL_0db4:  pop
  IL_0db9:  dup
  IL_0dba:  ldc.i4.2
  IL_0dbb:  shr
  IL_0dbc:  pop
  IL_0dc1:  dup
  IL_0dc2:  ldc.i4.3
  IL_0dc3:  sub
  IL_0dc4:  pop
  IL_0dc9:  dup
  IL_0dca:  ldc.i4.2
  IL_0dcb:  shl
  IL_0dcc:  pop
  IL_0dd1:  dup
  IL_0dd2:  ldc.i4.2
  IL_0dd3:  mul
  IL_0dd4:  pop
  IL_0dd9:  dup
  IL_0dda:  ldc.i4.4
  IL_0ddb:  shl
  IL_0ddc:  pop
  IL_0de1:  dup
  IL_0de2:  ldc.i4.4
  IL_0de3:  mul
  IL_0de4:  pop
  IL_0de9:  dup
  IL_0dea:  ldc.i4.2
  IL_0deb:  xor
  IL_0dec:  pop
  IL_0df1:  dup
  IL_0df2:  ldc.i4.1
  IL_0df3:  shr
  IL_0df4:  pop
  IL_0df9:  dup
  IL_0dfa:  ldc.i4.1
  IL_0dfb:  shl
  IL_0dfc:  pop
  IL_0e01:  dup
  IL_0e02:  ldc.i4.0
  IL_0e03:  shl
  IL_0e04:  pop
  IL_0e09:  dup
  IL_0e0a:  ldc.i4.0
  IL_0e0b:  shr
  IL_0e0c:  pop
  IL_0e11:  dup
  IL_0e12:  ldc.i4.2
  IL_0e13:  and
  IL_0e14:  pop
  IL_0e19:  dup
  IL_0e1a:  ldc.i4.7
  IL_0e1b:  mul
  IL_0e1c:  pop
  IL_0e21:  dup
  IL_0e22:  ldc.i4.7
  IL_0e23:  shl
  IL_0e24:  pop
  IL_0e29:  dup
  IL_0e2a:  ldc.i4.8
  IL_0e2b:  add
  IL_0e2c:  pop
  IL_0e31:  dup
  IL_0e32:  ldc.i4.4
  IL_0e33:  add
  IL_0e34:  pop
  IL_0e39:  dup
  IL_0e3a:  ldc.i4.3
  IL_0e3b:  mul
  IL_0e3c:  pop
  IL_0e41:  dup
  IL_0e42:  ldc.i4.3
  IL_0e43:  xor
  IL_0e44:  pop
  IL_0e49:  dup
  IL_0e4a:  ldc.i4.8
  IL_0e4b:  mul
  IL_0e4c:  pop
  IL_0e51:  dup
  IL_0e52:  ldc.i4.0
  IL_0e53:  mul
  IL_0e54:  pop
  IL_0e59:  dup
  IL_0e5a:  ldc.i4.3
  IL_0e5b:  and
  IL_0e5c:  pop
  IL_0e61:  dup
  IL_0e62:  ldc.i4.6
  IL_0e63:  add
  IL_0e64:  pop
  IL_0e69:  dup
  IL_0e6a:  ldc.i4.8
  IL_0e6b:  mul
  IL_0e6c:  pop
  IL_0e71:  dup
  IL_0e72:  ldc.i4.5
  IL_0e73:  and
  IL_0e74:  pop
  IL_0e79:  dup
  IL_0e7a:  ldc.i4.2
  IL_0e7b:  shl
  IL_0e7c:  pop
  IL_0e81:  dup
  IL_0e82:  ldc.i4.8
  IL_0e83:  shl
  IL_0e84:  pop
  IL_0e89:  dup
  IL_0e8a:  ldc.i4.1
  IL_0e8b:  xor
  IL_0e8c:  pop
  IL_0e91:  dup
  IL_0e92:  ldc.i4.1
  IL_0e93:  shl
  IL_0e94:  pop
  IL_0e99:  dup
  IL_0e9a:  ldc.i4.3
  IL_0e9b:  xor
  IL_0e9c:  pop
  IL_0ea1:  dup
  IL_0ea2:  ldc.i4.3
  IL_0ea3:  or
  IL_0ea4:  pop
  IL_0ea9:  dup
  IL_0eaa:  ldc.i4.5
  IL_0eab:  shr
  IL_0eac:  pop
  IL_0eb1:  dup
  IL_0eb2:  ldc.i4.3
  IL_0eb3:  xor
  IL_0eb4:  pop
  IL_0eb9:  dup
  IL_0eba:  ldc.i4.0
  IL_0ebb:  xor
  IL_0ebc:  pop
  IL_0ec1:  dup
  IL_0ec2:  ldc.i4.7
  IL_0ec3:  or
  IL_0ec4:  pop
  IL_0ec9:  dup
  IL_0eca:  ldc.i4.1
  IL_0ecb:  shr
  IL_0ecc:  pop
  IL_0ed1:  dup
  IL_0ed2:  ldc.i4.6
  IL_0ed3:  and
  IL_0ed4:  pop
  IL_0ed9:  dup
  IL_0eda:  ldc.i4.4
  IL_0edb:  shr
  IL_0edc:  pop
  IL_0ee1:  dup
  IL_0ee2:  ldc.i4.1
  IL_0ee3:  mul
  IL_0ee4:  pop
  IL_0ee9:  dup
  IL_0eea:  ldc.i4.8
  IL_0eeb:  sub
  IL_0eec:  pop
  IL_0ef1:  dup
  IL_0ef2:  ldc.i4.8
  IL_0ef3:  xor
  IL_0ef4:  pop
  IL_0ef9:  dup
  IL_0efa:  ldc.i4.6
  IL_0efb:  add
  IL_0efc:  pop
  IL_0f01:  dup
  IL_0f02:  ldc.i4.2
  IL_0f03:  mul
  IL_0f04:  pop
  IL_0f09:  dup
  IL_0f0a:  ldc.i4.1
  IL_0f0b:  or
  IL_0f0c:  pop
  IL_0f11:  dup
  IL_0f12:  ldc.i4.7
  IL_0f13:  or
  IL_0f14:  pop
  IL_0f19:  dup
  IL_0f1a:  ldc.i4.1
  IL_0f1b:  and
  IL_0f1c:  pop
  IL_0f21:  dup
  IL_0f22:  ldc.i4.0
  IL_0f23:  shl
  IL_0f24:  pop
  IL_0f29:  dup
  IL_0f2a:  ldc.i4.8
  IL_0f2b:  add
  IL_0f2c:  pop
  IL_0f31:  dup
  IL_0f32:  ldc.i4.6
  IL_0f33:  shr
  IL_0f34:  pop
  IL_0f39:  dup
  IL_0f3a:  ldc.i4.4
  IL_0f3b:  mul
  IL_0f3c:  pop
  IL_0f41:  dup
  IL_0f42:  ldc.i4.0
  IL_0f43:  shl
  IL_0f44:  pop
  IL_0f49:  dup
  IL_0f4a:  ldc.i4.4
  IL_0f4b:  shr
  IL_0f4c:  pop
  IL_0f51:  dup
  IL_0f52:  ldc.i4.5
  IL_0f53:  shl
  IL_0f54:  pop
  IL_0f59:  dup
  IL_0f5a:  ldc.i4.7
  IL_0f5b:  add
  IL_0f5c:  pop
  IL_0f61:  dup
  IL_0f62:  ldc.i4.8
  IL_0f63:  xor
  IL_0f64:  pop
  IL_0f69:  dup
  IL_0f6a:  ldc.i4.2
  IL_0f6b:  or
  IL_0f6c:  pop
  IL_0f71:  dup
  IL_0f72:  ldc.i4.5
  IL_0f73:  sub
  IL_0f74:  pop
  IL_0f79:  dup
  IL_0f7a:  ldc.i4.5
  IL_0f7b:  or
  IL_0f7c:  pop
  IL_0f81:  dup
  IL_0f82:  ldc.i4.2
  IL_0f83:  shl
  IL_0f84:  pop
  IL_0f89:  dup
  IL_0f8a:  ldc.i4.3
  IL_0f8b:  add
  IL_0f8c:  pop
  IL_0f91:  dup
  IL_0f92:  ldc.i4.1
  IL_0f93:  add
  IL_0f94:  pop
  IL_0f99:  dup
  IL_0f9a:  ldc.i4.4
  IL_0f9b:  or
  IL_0f9c:  pop
  IL_0fa1:  dup
  IL_0fa2:  ldc.i4.7
  IL_0fa3:  mul
  IL_0fa4:  pop
  IL_0fa9:  dup
  IL_0faa:  ldc.i4.5
  IL_0fab:  sub
  IL_0fac:  pop
  IL_0fb1:  dup
  IL_0fb2:  ldc.i4.7
  IL_0fb3:  xor
  IL_0fb4:  pop
  IL_0fb9:  dup
  IL_0fba:  ldc.i4.0
  IL_0fbb:  xor
  IL_0fbc:  pop
  IL_0fc1:  dup
  IL_0fc2:  ldc.i4.7
  IL_0fc3:  shr
  IL_0fc4:  pop
  IL_0fc9:  dup
  IL_0fca:  ldc.i4.1
  IL_0fcb:  mul
  IL_0fcc:  pop
  IL_0fd1:  dup
  IL_0fd2:  ldc.i4.0
  IL_0fd3:  or
  IL_0fd4:  pop
  IL_0fd9:  dup
  IL_0fda:  ldc.i4.4
  IL_0fdb:  shr
  IL_0fdc:  pop
  IL_0fe1:  dup
  IL_0fe2:  ldc.i4.4
  IL_0fe3:  add
  IL_0fe4:  pop
  IL_0fe9:  dup
  IL_0fea:  ldc.i4.8
  IL_0feb:  xor
  IL_0fec:  pop
  IL_0ff1:  dup
  IL_0ff2:  ldc.i4.5
  IL_0ff3:  sub
  IL_0ff4:  pop
  IL_0ff9:  dup
  IL_0ffa:  ldc.i4.0
  IL_0ffb:  or
  IL_0ffc:  pop
  IL_1001:  dup
  IL_1002:  ldc.i4.8
  IL_1003:  add
  IL_1004:  pop
  IL_1009:  dup
  IL_100a:  ldc.i4.7
  IL_100b:  sub
  IL_100c:  pop
  IL_1011:  dup
  IL_1012:  ldc.i4.2
  IL_1013:  xor
  IL_1014:  pop
  IL_1019:  dup
  IL_101a:  ldc.i4.0
  IL_101b:  or
  IL_101c:  pop
  IL_1021:  dup
  IL_1022:  ldc.i4.0
  IL_1023:  shl
  IL_1024:  pop
  IL_1029:  dup
  IL_102a:  ldc.i4.8
  IL_102b:  mul
  IL_102c:  pop
  IL_1031:  dup
  IL_1032:  ldc.i4.0
  IL_1033:  shr
  IL_1034:  pop
  IL_1039:  dup
  IL_103a:  ldc.i4.2
  IL_103b:  shl
  IL_103c:  pop
  IL_1041:  dup
  IL_1042:  ldc.i4.4
  IL_1043:  and
  IL_1044:  pop
  IL_1049:  dup
  IL_104a:  ldc.i4.1
  IL_104b:  add
  IL_104c:  pop
  IL_1051:  dup
  IL_1052:  ldc.i4.8
  IL_1053:  sub
  IL_1054:  pop
  IL_1059:  dup
  IL_105a:  ldc.i4.7
  IL_105b:  mul
  IL_105c:  pop
  IL_1061:  dup
  IL_1062:  ldc.i4.0
  IL_1063:  shr
  IL_1064:  pop
  IL_1069:  dup
  IL_106a:  ldc.i4.4
  IL_106b:  sub
  IL_106c:  pop
  IL_1071:  dup
  IL_1072:  ldc.i4.0
  IL_1073:  sub
  IL_1074:  pop
  IL_1079:  dup
  IL_107a:  ldc.i4.2
  IL_107b:  shl
  IL_107c:  pop
  IL_1081:  dup
  IL_1082:  ldc.i4.2
  IL_1083:  shl
  IL_1084:  pop
  IL_1089:  dup
  IL_108a:  ldc.i4.2
  IL_108b:  and
  IL_108c:  pop
  IL_1091:  dup
  IL_1092:  ldc.i4.5
  IL_1093:  and
  IL_1094:  pop
  IL_1099:  dup
  IL_109a:  ldc.i4.2
  IL_109b:  mul
  IL_109c:  pop
  IL_10a1:  dup
  IL_10a2:  ldc.i4.4
  IL_10a3:  sub
  IL_10a4:  pop
  IL_10a9:  dup
  IL_10aa:  ldc.i4.3
  IL_10ab:  xor
  IL_10ac:  pop
  IL_10b1:  dup
  IL_10b2:  ldc.i4.0
  IL_10b3:  sub
  IL_10b4:  pop
  IL_10b9:  dup
  IL_10ba:  ldc.i4.6
  IL_10bb:  and
  IL_10bc:  pop
  IL_10c1:  dup
  IL_10c2:  ldc.i4.6
  IL_10c3:  xor
  IL_10c4:  pop
  IL_10c9:  dup
  IL_10ca:  ldc.i4.8
  IL_10cb:  and
  IL_10cc:  pop
  IL_10d1:  dup
  IL_10d2:  ldc.i4.0
  IL_10d3:  add
  IL_10d4:  pop
  IL_10d9:  dup
  IL_10da:  ldc.i4.0
  IL_10db:  shr
  IL_10dc:  pop
  IL_10e1:  dup
  IL_10e2:  ldc.i4.1
  IL_10e3:  shl
  IL_10e4:  pop
  IL_10e9:  dup
  IL_10ea:  ldc.i4.0
  IL_10eb:  shl
  IL_10ec:  pop
  IL_10f1:  dup
  IL_10f2:  ldc.i4.8
  IL_10f3:  or
  IL_10f4:  pop
  IL_10f9:  dup
  IL_10fa:  ldc.i4.8
  IL_10fb:  and
  IL_10fc:  pop
  IL_1101:  dup
  IL_1102:  ldc.i4.2
  IL_1103:  xor
  IL_1104:  pop
  IL_1109:  dup
  IL_110a:  ldc.i4.0
  IL_110b:  xor
  IL_110c:  pop
  IL_1111:  dup
  IL_1112:  ldc.i4.3
  IL_1113:  shl
  IL_1114:  pop
  IL_1119:  dup
  IL_111a:  ldc.i4.8
  IL_111b:  shl
  IL_111c:  pop
  IL_1121:  dup
  IL_1122:  ldc.i4.5
  IL_1123:  and
  IL_1124:  pop
  IL_1129:  dup
  IL_112a:  ldc.i4.4
  IL_112b:  shr
  IL_112c:  pop
  IL_1131:  dup
  IL_1132:  ldc.i4.7
  IL_1133:  xor
  IL_1134:  pop
  IL_1139:  dup
  IL_113a:  ldc.i4.1
  IL_113b:  or
  IL_113c:  pop
  IL_1141:  dup
  IL_1142:  ldc.i4.0
  IL_1143:  shr
  IL_1144:  pop
  IL_1149:  dup
  IL_114a:  ldc.i4.3
  IL_114b:  shl
  IL_114c:  pop
  IL_1151:  dup
  IL_1152:  ldc.i4.1
  IL_1153:  xor
  IL_1154:  pop
  IL_1159:  dup
  IL_115a:  ldc.i4.4
  IL_115b:  sub
  IL_115c:  pop
  IL_1161:  dup
  IL_1162:  ldc.i4.5
  IL_1163:  shl
  IL_1164:  pop
  IL_1169:  dup
  IL_116a:  ldc.i4.5
  IL_116b:  add
  IL_116c:  pop
  IL_1171:  dup
  IL_1172:  ldc.i4.6
  IL_1173:  shr
  IL_1174:  pop
  IL_1179:  dup
  IL_117a:  ldc.i4.3
  IL_117b:  shl
  IL_117c:  pop
  IL_1181:  dup
  IL_1182:  ldc.i4.2
  IL_1183:  xor
  IL_1184:  pop
  IL_1189:  dup
  IL_118a:  ldc.i4.1
  IL_118b:  shr
  IL_118c:  pop
  IL_1191:  dup
  IL_1192:  ldc.i4.1
  IL_1193:  mul
  IL_1194:  pop
  IL_1199:  dup
  IL_119a:  ldc.i4.3
  IL_119b:  sub
  IL_119c:  pop
  IL_11a1:  dup
  IL_11a2:  ldc.i4.5
  IL_11a3:  mul
  IL_11a4:  pop
  IL_11a9:  dup
  IL_11aa:  ldc.i4.2
  IL_11ab:  mul
  IL_11ac:  pop
  IL_11b1:  dup
  IL_11b2:  ldc.i4.5
  IL_11b3:  mul
  IL_11b4:  pop
  IL_11b9:  dup
  IL_11ba:  ldc.i4.8
  IL_11bb:  and
  IL_11bc:  pop
  IL_11c1:  dup
  IL_11c2:  ldc.i4.3
  IL_11c3:  sub
  IL_11c4:  pop
  IL_11c9:  dup
  IL_11ca:  ldc.i4.4
  IL_11cb:  mul
  IL_11cc:  pop
  IL_11d1:  dup
  IL_11d2:  ldc.i4.5
  IL_11d3:  mul
  IL_11d4:  pop
  IL_11d9:  dup
  IL_11da:  ldc.i4.6
  IL_11db:  or
  IL_11dc:  pop
  IL_11e1:  dup
  IL_11e2:  ldc.i4.8
  IL_11e3:  and
  IL_11e4:  pop
  IL_11e9:  dup
  IL_11ea:  ldc.i4.7
  IL_11eb:  or
  IL_11ec:  pop
  IL_11f1:  dup
  IL_11f2:  ldc.i4.8
  IL_11f3:  or
  IL_11f4:  pop
  IL_11f9:  dup
  IL_11fa:  ldc.i4.7
  IL_11fb:  and
  IL_11fc:  pop
  IL_1201:  dup
  IL_1202:  ldc.i4.3
  IL_1203:  and
  IL_1204:  pop
  IL_1209:  dup
  IL_120a:  ldc.i4.3
  IL_120b:  mul
  IL_120c:  pop
  IL_1211:  dup
  IL_1212:  ldc.i4.1
  IL_1213:  shl
  IL_1214:  pop
  IL_1219:  dup
  IL_121a:  ldc.i4.6
  IL_121b:  shr
  IL_121c:  pop
  IL_1221:  dup
  IL_1222:  ldc.i4.1
  IL_1223:  sub
  IL_1224:  pop
  IL_1229:  dup
  IL_122a:  ldc.i4.8
  IL_122b:  and
  IL_122c:  pop
  IL_1231:  dup
  IL_1232:  ldc.i4.1
  IL_1233:  sub
  IL_1234:  pop
  IL_1239:  dup
  IL_123a:  ldc.i4.4
  IL_123b:  sub
  IL_123c:  pop
  IL_1241:  dup
  IL_1242:  ldc.i4.5
  IL_1243:  and
  IL_1244:  pop
  IL_1249:  dup
  IL_124a:  ldc.i4.5
  IL_124b:  shl
  IL_124c:  pop
  IL_1251:  dup
  IL_1252:  ldc.i4.1
  IL_1253:  add
  IL_1254:  pop
  IL_1259:  dup
  IL_125a:  ldc.i4.0
  IL_125b:  or
  IL_125c:  pop
  IL_1261:  dup
  IL_1262:  ldc.i4.7
  IL_1263:  mul
  IL_1264:  pop
  IL_1269:  dup
  IL_126a:  ldc.i4.6
  IL_126b:  mul
  IL_126c:  pop
  IL_1271:  dup
  IL_1272:  ldc.i4.4
  IL_1273:  shl
  IL_1274:  pop
  IL_1279:  dup
  IL_127a:  ldc.i4.2
  IL_127b:  shr
  IL_127c:  pop
  IL_1281:  dup
  IL_1282:  ldc.i4.4
  IL_1283:  xor
  IL_1284:  pop
  IL_1289:  dup
  IL_128a:  ldc.i4.8
  IL_128b:  and
  IL_128c:  pop
  IL_1291:  dup
  IL_1292:  ldc.i4.8
  IL_1293:  shl
  IL_1294:  pop
  IL_1299:  dup
  IL_129a:  ldc.i4.5
  IL_129b:  and
  IL_129c:  pop
  IL_12a1:  dup
  IL_12a2:  ldc.i4.4
  IL_12a3:  and
  IL_12a4:  pop
  IL_12a9:  dup
  IL_12aa:  ldc.i4.0
  IL_12ab:  xor
  IL_12ac:  pop
  IL_12b1:  dup
  IL_12b2:  ldc.i4.3
  IL_12b3:  and
  IL_12b4:  pop
  IL_12b9:  dup
  IL_12ba:  ldc.i4.6
  IL_12bb:  shr
  IL_12bc:  pop
  IL_12c1:  dup
  IL_12c2:  ldc.i4.2
  IL_12c3:  shl
  IL_12c4:  pop
  IL_12c9:  dup
  IL_12ca:  ldc.i4.2
  IL_12cb:  sub
  IL_12cc:  pop
  IL_12d1:  dup
  IL_12d2:  ldc.i4.2
  IL_12d3:  mul
  IL_12d4:  pop
  IL_12d9:  dup
  IL_12da:  ldc.i4.5
  IL_12db:  mul
  IL_12dc:  pop
  IL_12e1:  dup
  IL_12e2:  ldc.i4.7
  IL_12e3:  xor
  IL_12e4:  pop
  IL_12e9:  dup
  IL_12ea:  ldc.i4.4
  IL_12eb:  or
  IL_12ec:  pop
  IL_12f1:  dup
  IL_12f2:  ldc.i4.7
  IL_12f3:  xor
  IL_12f4:  pop
  IL_12f9:  dup
  IL_12fa:  ldc.i4.2
  IL_12fb:  shr
  IL_12fc:  pop
  IL_1301:  dup
  IL_1302:  ldc.i4.8
  IL_1303:  mul
  IL_1304:  pop
  IL_1309:  dup
  IL_130a:  ldc.i4.3
  IL_130b:  shr
  IL_130c:  pop
  IL_1311:  dup
  IL_1312:  ldc.i4.1
  IL_1313:  shr
  IL_1314:  pop
  IL_1319:  dup
  IL_131a:  ldc.i4.2
  IL_131b:  add
  IL_131c:  pop
  IL_1321:  dup
  IL_1322:  ldc.i4.1
  IL_1323:  and
  IL_1324:  pop
  IL_1329:  dup
  IL_132a:  ldc.i4.0
  IL_132b:  xor
  IL_132c:  pop
  IL_1331:  dup
  IL_1332:  ldc.i4.0
  IL_1333:  shl
  IL_1334:  pop
  IL_1339:  dup
  IL_133a:  ldc.i4.5
  IL_133b:  add
  IL_133c:  pop
  IL_1341:  dup
  IL_1342:  ldc.i4.4
  IL_1343:  add
  IL_1344:  pop
  IL_1349:  dup
  IL_134a:  ldc.i4.0
  IL_134b:  or
  IL_134c:  pop
  IL_1351:  dup
  IL_1352:  ldc.i4.2
  IL_1353:  or
  IL_1354:  pop
  IL_1359:  dup
  IL_135a:  ldc.i4.3
  IL_135b:  sub
  IL_135c:  pop
  IL_1361:  dup
  IL_1362:  ldc.i4.7
  IL_1363:  shr
  IL_1364:  pop
  IL_1369:  dup
  IL_136a:  ldc.i4.8
  IL_136b:  shl
  IL_136c:  pop
  IL_1371:  dup
  IL_1372:  ldc.i4.0
  IL_1373:  shr
  IL_1374:  pop
  IL_1379:  dup
  IL_137a:  ldc.i4.0
  IL_137b:  add
  IL_137c:  pop
  IL_1381:  dup
  IL_1382:  ldc.i4.5
  IL_1383:  xor
  IL_1384:  pop
  IL_1389:  dup
  IL_138a:  ldc.i4.4
  IL_138b:  add
  IL_138c:  pop
  IL_1391:  dup
  IL_1392:  ldc.i4.3
  IL_1393:  add
  IL_1394:  pop
  IL_1399:  dup
  IL_139a:  ldc.i4.4
  IL_139b:  shl
  IL_139c:  pop
  IL_13a1:  dup
  IL_13a2:  ldc.i4.3
  IL_13a3:  sub
  IL_13a4:  pop
  IL_13a9:  dup
  IL_13aa:  ldc.i4.0
  IL_13ab:  xor
  IL_13ac:  pop
  IL_13b1:  dup
  IL_13b2:  ldc.i4.2
  IL_13b3:  mul
  IL_13b4:  pop
  IL_13b9:  dup
  IL_13ba:  ldc.i4.2
  IL_13bb:  mul
  IL_13bc:  pop
  IL_13c1:  dup
  IL_13c2:  ldc.i4.7
  IL_13c3:  shl
  IL_13c4:  pop
  IL_13c9:  dup
  IL_13ca:  ldc.i4.3
  IL_13cb:  and
  IL_13cc:  pop
  IL_13d1:  dup
  IL_13d2:  ldc.i4.3
  IL_13d3:  mul
  IL_13d4:  pop
  IL_13d9:  dup
  IL_13da:  ldc.i4.3
  IL_13db:  or
  IL_13dc:  pop
  IL_13e1:  dup
  IL_13e2:  ldc.i4.8
  IL_13e3:  mul
  IL_13e4:  pop
  IL_13e9:  dup
  IL_13ea:  ldc.i4.8
  IL_13eb:  shl
  IL_13ec:  pop
  IL_13f1:  dup
  IL_13f2:  ldc.i4.7
  IL_13f3:  shr
  IL_13f4:  pop
  IL_13f9:  dup
  IL_13fa:  ldc.i4.8
  IL_13fb:  xor
  IL_13fc:  pop
  IL_1401:  dup
  IL_1402:  ldc.i4.7
  IL_1403:  or
  IL_1404:  pop
  IL_1409:  dup
  IL_140a:  ldc.i4.7
  IL_140b:  shl
  IL_140c:  pop
  IL_1411:  dup
  IL_1412:  ldc.i4.0
  IL_1413:  shl
  IL_1414:  pop
  IL_1419:  dup
  IL_141a:  ldc.i4.5
  IL_141b:  xor
  IL_141c:  pop
  IL_1421:  dup
  IL_1422:  ldc.i4.4
  IL_1423:  xor
  IL_1424:  pop
  IL_1429:  dup
  IL_142a:  ldc.i4.4
  IL_142b:  add
  IL_142c:  pop
  IL_1431:  dup
  IL_1432:  ldc.i4.8
  IL_1433:  and
  IL_1434:  pop
  IL_1439:  dup
  IL_143a:  ldc.i4.8
  IL_143b:  xor
  IL_143c:  pop
  IL_1441:  dup
  IL_1442:  ldc.i4.4
  IL_1443:  shr
  IL_1444:  pop
  IL_1449:  dup
  IL_144a:  ldc.i4.6
  IL_144b:  shl
  IL_144c:  pop
  IL_1451:  dup
  IL_1452:  ldc.i4.2
  IL_1453:  sub
  IL_1454:  pop
  IL_1459:  dup
  IL_145a:  ldc.i4.6
  IL_145b:  sub
  IL_145c:  pop
  IL_1461:  dup
  IL_1462:  ldc.i4.8
  IL_1463:  shl
  IL_1464:  pop
  IL_1469:  dup
  IL_146a:  ldc.i4.0
  IL_146b:  sub
  IL_146c:  pop
  IL_1471:  dup
  IL_1472:  ldc.i4.1
  IL_1473:  add
  IL_1474:  pop
  IL_1479:  dup
  IL_147a:  ldc.i4.7
  IL_147b:  shr
  IL_147c:  pop
  IL_1481:  dup
  IL_1482:  ldc.i4.2
  IL_1483:  shl
  IL_1484:  pop
  IL_1489:  dup
  IL_148a:  ldc.i4.6
  IL_148b:  mul
  IL_148c:  pop
  IL_1491:  dup
  IL_1492:  ldc.i4.4
  IL_1493:  shr
  IL_1494:  pop
  IL_1499:  dup
  IL_149a:  ldc.i4.2
  IL_149b:  mul
  IL_149c:  pop
  IL_14a1:  dup
  IL_14a2:  ldc.i4.8
  IL_14a3:  and
  IL_14a4:  pop
  IL_14a9:  dup
  IL_14aa:  ldc.i4.2
  IL_14ab:  or
  IL_14ac:  pop
  IL_14b1:  dup
  IL_14b2:  ldc.i4.7
  IL_14b3:  xor
  IL_14b4:  pop
  IL_14b9:  dup
  IL_14ba:  ldc.i4.0
  IL_14bb:  sub
  IL_14bc:  pop
  IL_14c1:  dup
  IL_14c2:  ldc.i4.3
  IL_14c3:  xor
  IL_14c4:  pop
  IL_14c9:  dup
  IL_14ca:  ldc.i4.0
  IL_14cb:  add
  IL_14cc:  pop
  IL_14d1:  dup
  IL_14d2:  ldc.i4.8
  IL_14d3:  xor
  IL_14d4:  pop
  IL_14d9:  dup
  IL_14da:  ldc.i4.1
  IL_14db:  shl
  IL_14dc:  pop
  IL_14e1:  dup
  IL_14e2:  ldc.i4.1
  IL_14e3:  shl
  IL_14e4:  pop
  IL_14e9:  dup
  IL_14ea:  ldc.i4.5
  IL_14eb:  xor
  IL_14ec:  pop
  IL_14f1:  dup
  IL_14f2:  ldc.i4.4
  IL_14f3:  xor
  IL_14f4:  pop
  IL_14f9:  dup
  IL_14fa:  ldc.i4.2
  IL_14fb:  shl
  IL_14fc:  pop
  IL_1501:  dup
  IL_1502:  ldc.i4.5
  IL_1503:  and
  IL_1504:  pop
  IL_1509:  dup
  IL_150a:  ldc.i4.6
  IL_150b:  mul
  IL_150c:  pop
  IL_1511:  dup
  IL_1512:  ldc.i4.1
  IL_1513:  shl
  IL_1514:  pop
  IL_1519:  dup
  IL_151a:  ldc.i4.0
  IL_151b:  and
  IL_151c:  pop
  IL_1521:  dup
  IL_1522:  ldc.i4.3
  IL_1523:  xor
  IL_1524:  pop
  IL_1529:  dup
  IL_152a:  ldc.i4.5
  IL_152b:  xor
  IL_152c:  pop
  IL_1531:  dup
  IL_1532:  ldc.i4.0
  IL_1533:  and
  IL_1534:  pop
  IL_1539:  dup
  IL_153a:  ldc.i4.3
  IL_153b:  shl
  IL_153c:  pop
  IL_1541:  dup
  IL_1542:  ldc.i4.5
  IL_1543:  or
  IL_1544:  pop
  IL_1549:  dup
  IL_154a:  ldc.i4.6
  IL_154b:  shr
  IL_154c:  pop
  IL_1551:  dup
  IL_1552:  ldc.i4.8
  IL_1553:  shr
  IL_1554:  pop
  IL_1559:  dup
  IL_155a:  ldc.i4.7
  IL_155b:  and
  IL_155c:  pop
  IL_1561:  dup
  IL_1562:  ldc.i4.3
  IL_1563:  sub
  IL_1564:  pop
  IL_1569:  dup
  IL_156a:  ldc.i4.0
  IL_156b:  and
  IL_156c:  pop
  IL_1571:  dup
  IL_1572:  ldc.i4.8
  IL_1573:  and
  IL_1574:  pop
  IL_1579:  dup
  IL_157a:  ldc.i4.6
  IL_157b:  xor
  IL_157c:  pop
  IL_1581:  dup
  IL_1582:  ldc.i4.4
  IL_1583:  shr
  IL_1584:  pop
  IL_1589:  dup
  IL_158a:  ldc.i4.2
  IL_158b:  add
  IL_158c:  pop
  IL_1591:  dup
  IL_1592:  ldc.i4.5
  IL_1593:  shr
  IL_1594:  pop
  IL_1599:  dup
  IL_159a:  ldc.i4.8
  IL_159b:  shr
  IL_159c:  pop
  IL_15a1:  dup
  IL_15a2:  ldc.i4.3
  IL_15a3:  sub
  IL_15a4:  pop
  IL_15a9:  dup
  IL_15aa:  ldc.i4.6
  IL_15ab:  xor
  IL_15ac:  pop
  IL_15b1:  dup
  IL_15b2:  ldc.i4.7
  IL_15b3:  shr
  IL_15b4:  pop
  IL_15b9:  dup
  IL_15ba:  ldc.i4.4
  IL_15bb:  mul
  IL_15bc:  pop
  IL_15c1:  dup
  IL_15c2:  ldc.i4.2
  IL_15c3:  add
  IL_15c4:  pop
  IL_15c9:  dup
  IL_15ca:  ldc.i4.8
  IL_15cb:  shr
  IL_15cc:  pop
  IL_15d1:  dup
  IL_15d2:  ldc.i4.6
  IL_15d3:  shl
  IL_15d4:  pop
  IL_15d9:  dup
  IL_15da:  ldc.i4.4
  IL_15db:  or
  IL_15dc:  pop
  IL_15e1:  dup
  IL_15e2:  ldc.i4.0
  IL_15e3:  or
  IL_15e4:  pop
  IL_15e9:  dup
  IL_15ea:  ldc.i4.7
  IL_15eb:  or
  IL_15ec:  pop
  IL_15f1:  dup
  IL_15f2:  ldc.i4.4
  IL_15f3:  sub
  IL_15f4:  pop
  IL_15f9:  dup
  IL_15fa:  ldc.i4.7
  IL_15fb:  shr
  IL_15fc:  pop
  IL_1601:  dup
  IL_1602:  ldc.i4.6
  IL_1603:  sub
  IL_1604:  pop
  IL_1609:  dup
  IL_160a:  ldc.i4.8
  IL_160b:  mul
  IL_160c:  pop
  IL_1611:  dup
  IL_1612:  ldc.i4.5
  IL_1613:  mul
  IL_1614:  pop
  IL_1619:  dup
  IL_161a:  ldc.i4.7
  IL_161b:  and
  IL_161c:  pop
  IL_1621:  dup
  IL_1622:  ldc.i4.8
  IL_1623:  add
  IL_1624:  pop
  IL_1629:  dup
  IL_162a:  ldc.i4.3
  IL_162b:  and
  IL_162c:  pop
  IL_1631:  dup
  IL_1632:  ldc.i4.8
  IL_1633:  xor
  IL_1634:  pop
  IL_1639:  dup
  IL_163a:  ldc.i4.8
  IL_163b:  shr
  IL_163c:  pop
  IL_1641:  dup
  IL_1642:  ldc.i4.5
  IL_1643:  shr
  IL_1644:  pop
  IL_1649:  dup
  IL_164a:  ldc.i4.5
  IL_164b:  and
  IL_164c:  pop
  IL_1651:  dup
  IL_1652:  ldc.i4.7
  IL_1653:  add
  IL_1654:  pop
  IL_1659:  dup
  IL_165a:  ldc.i4.6
  IL_165b:  or
  IL_165c:  pop
  IL_1661:  dup
  IL_1662:  ldc.i4.1
  IL_1663:  mul
  IL_1664:  pop
  IL_1669:  dup
  IL_166a:  ldc.i4.0
  IL_166b:  shr
  IL_166c:  pop
  IL_1671:  dup
  IL_1672:  ldc.i4.7
  IL_1673:  or
  IL_1674:  pop
  IL_1679:  dup
  IL_167a:  ldc.i4.1
  IL_167b:  sub
  IL_167c:  pop
  IL_1681:  dup
  IL_1682:  ldc.i4.3
  IL_1683:  add
  IL_1684:  pop
  IL_1689:  dup
  IL_168a:  ldc.i4.5
  IL_168b:  shr
  IL_168c:  pop
  IL_1691:  dup
  IL_1692:  ldc.i4.6
  IL_1693:  mul
  IL_1694:  pop
  IL_1699:  dup
  IL_169a:  ldc.i4.7
  IL_169b:  or
  IL_169c:  pop
  IL_16a1:  dup
  IL_16a2:  ldc.i4.2
  IL_16a3:  add
  IL_16a4:  pop
  IL_16a9:  dup
  IL_16aa:  ldc.i4.6
  IL_16ab:  shr
  IL_16ac:  pop
  IL_16b1:  dup
  IL_16b2:  ldc.i4.1
  IL_16b3:  and
  IL_16b4:  pop
  IL_16b9:  dup
  IL_16ba:  ldc.i4.0
  IL_16bb:  shr
  IL_16bc:  pop
  IL_16c1:  dup
  IL_16c2:  ldc.i4.8
  IL_16c3:  sub
  IL_16c4:  pop
  IL_16c9:  dup
  IL_16ca:  ldc.i4.3
  IL_16cb:  and
  IL_16cc:  pop
  IL_16d1:  dup
  IL_16d2:  ldc.i4.4
  IL_16d3:  or
  IL_16d4:  pop
  IL_16d9:  dup
  IL_16da:  ldc.i4.7
  IL_16db:  shr
  IL_16dc:  pop
  IL_16e1:  dup
  IL_16e2:  ldc.i4.3
  IL_16e3:  or
  IL_16e4:  pop
  IL_16e9:  dup
  IL_16ea:  ldc.i4.0
  IL_16eb:  mul
  IL_16ec:  pop
  IL_16f1:  dup
  IL_16f2:  ldc.i4.3
  IL_16f3:  sub
  IL_16f4:  pop
  IL_16f9:  dup
  IL_16fa:  ldc.i4.0
  IL_16fb:  or
  IL_16fc:  pop
  IL_1701:  dup
  IL_1702:  ldc.i4.7
  IL_1703:  sub
  IL_1704:  pop
  IL_1709:  dup
  IL_170a:  ldc.i4.3
  IL_170b:  or
  IL_170c:  pop
  IL_1711:  dup
  IL_1712:  ldc.i4.6
  IL_1713:  mul
  IL_1714:  pop
  IL_1719:  dup
  IL_171a:  ldc.i4.8
  IL_171b:  mul
  IL_171c:  pop
  IL_1721:  dup
  IL_1722:  ldc.i4.8
  IL_1723:  shr
  IL_1724:  pop
  IL_1729:  dup
  IL_172a:  ldc.i4.5
  IL_172b:  and
  IL_172c:  pop
  IL_1731:  dup
  IL_1732:  ldc.i4.3
  IL_1733:  add
  IL_1734:  pop
  IL_1739:  dup
  IL_173a:  ldc.i4.6
  IL_173b:  xor
  IL_173c:  pop
  IL_1741:  dup
  IL_1742:  ldc.i4.2
  IL_1743:  mul
  IL_1744:  pop
  IL_1749:  dup
  IL_174a:  ldc.i4.6
  IL_174b:  shr
  IL_174c:  pop
  IL_1751:  dup
  IL_1752:  ldc.i4.5
  IL_1753:  mul
  IL_1754:  pop
  IL_1759:  dup
  IL_175a:  ldc.i4.5
  IL_175b:  mul
  IL_175c:  pop
  IL_1761:  dup
  IL_1762:  ldc.i4.1
  IL_1763:  or
  IL_1764:  pop
  IL_1769:  dup
  IL_176a:  ldc.i4.1
  IL_176b:  and
  IL_176c:  pop
  IL_1771:  dup
  IL_1772:  ldc.i4.1
  IL_1773:  xor
  IL_1774:  pop
  IL_1779:  dup
  IL_177a:  ldc.i4.5
  IL_177b:  add
  IL_177c:  pop
  IL_1781:  dup
  IL_1782:  ldc.i4.8
  IL_1783:  and
  IL_1784:  pop
  IL_1789:  dup
  IL_178a:  ldc.i4.7
  IL_178b:  add
  IL_178c:  pop
  IL_1791:  dup
  IL_1792:  ldc.i4.3
  IL_1793:  add
  IL_1794:  pop
  IL_1799:  dup
  IL_179a:  ldc.i4.8
  IL_179b:  shr
  IL_179c:  pop
  IL_17a1:  dup
  IL_17a2:  ldc.i4.1
  IL_17a3:  or
  IL_17a4:  pop
  IL_17a9:  dup
  IL_17aa:  ldc.i4.8
  IL_17ab:  and
  IL_17ac:  pop
  IL_17b1:  dup
  IL_17b2:  ldc.i4.3
  IL_17b3:  or
  IL_17b4:  pop
  IL_17b9:  dup
  IL_17ba:  ldc.i4.2
  IL_17bb:  mul
  IL_17bc:  pop
  IL_17c1:  dup
  IL_17c2:  ldc.i4.2
  IL_17c3:  shr
  IL_17c4:  pop
  IL_17c9:  dup
  IL_17ca:  ldc.i4.0
  IL_17cb:  or
  IL_17cc:  pop
  IL_17d1:  dup
  IL_17d2:  ldc.i4.4
  IL_17d3:  xor
  IL_17d4:  pop
  IL_17d9:  dup
  IL_17da:  ldc.i4.2
  IL_17db:  xor
  IL_17dc:  pop
  IL_17e1:  dup
  IL_17e2:  ldc.i4.4
  IL_17e3:  add
  IL_17e4:  pop
  IL_17e9:  dup
  IL_17ea:  ldc.i4.7
  IL_17eb:  shr
  IL_17ec:  pop
  IL_17f1:  dup
  IL_17f2:  ldc.i4.1
  IL_17f3:  sub
  IL_17f4:  pop
  IL_17f9:  dup
  IL_17fa:  ldc.i4.1
  IL_17fb:  xor
  IL_17fc:  pop
  IL_1801:  dup
  IL_1802:  ldc.i4.1
  IL_1803:  shl
  IL_1804:  pop
  IL_1809:  dup
  IL_180a:  ldc.i4.7
  IL_180b:  shr
  IL_180c:  pop
  IL_1811:  dup
  IL_1812:  ldc.i4.2
  IL_1813:  shr
  IL_1814:  pop
  IL_1819:  dup
  IL_181a:  ldc.i4.0
  IL_181b:  sub
  IL_181c:  pop
  IL_1821:  dup
  IL_1822:  ldc.i4.3
  IL_1823:  add
  IL_1824:  pop
  IL_1829:  dup
  IL_182a:  ldc.i4.4
  IL_182b:  shr
  IL_182c:  pop
  IL_1831:  dup
  IL_1832:  ldc.i4.3
  IL_1833:  and
  IL_1834:  pop
  IL_1839:  dup
  IL_183a:  ldc.i4.0
  IL_183b:  sub
  IL_183c:  pop
  IL_1841:  dup
  IL_1842:  ldc.i4.6
  IL_1843:  and
  IL_1844:  pop
  IL_1849:  dup
  IL_184a:  ldc.i4.5
  IL_184b:  shl
  IL_184c:  pop
  IL_1851:  dup
  IL_1852:  ldc.i4.3
  IL_1853:  shl
  IL_1854:  pop
  IL_1859:  dup
  IL_185a:  ldc.i4.5
  IL_185b:  shl
  IL_185c:  pop
  IL_1861:  dup
  IL_1862:  ldc.i4.4
  IL_1863:  mul
  IL_1864:  pop
  IL_1869:  dup
  IL_186a:  ldc.i4.4
  IL_186b:  mul
  IL_186c:  pop
  IL_1871:  dup
  IL_1872:  ldc.i4.1
  IL_1873:  sub
  IL_1874:  pop
  IL_1879:  dup
  IL_187a:  ldc.i4.4
  IL_187b:  add
  IL_187c:  pop
  IL_1881:  dup
  IL_1882:  ldc.i4.7
  IL_1883:  xor
  IL_1884:  pop
  IL_1889:  dup
  IL_188a:  ldc.i4.6
  IL_188b:  and
  IL_188c:  pop
  IL_1891:  dup
  IL_1892:  ldc.i4.1
  IL_1893:  shr
  IL_1894:  pop
  IL_1899:  dup
  IL_189a:  ldc.i4.0
  IL_189b:  shl
  IL_189c:  pop
  IL_18a1:  dup
  IL_18a2:  ldc.i4.5
  IL_18a3:  and
  IL_18a4:  pop
  IL_18a9:  dup
  IL_18aa:  ldc.i4.5
  IL_18ab:  mul
  IL_18ac:  pop
  IL_18b1:  dup
  IL_18b2:  ldc.i4.7
  IL_18b3:  shl
  IL_18b4:  pop
  IL_18b9:  dup
  IL_18ba:  ldc.i4.1
  IL_18bb:  shr
  IL_18bc:  pop
  IL_18c1:  dup
  IL_18c2:  ldc.i4.0
  IL_18c3:  shr
  IL_18c4:  pop
  IL_18c9:  dup
  IL_18ca:  ldc.i4.0
  IL_18cb:  mul
  IL_18cc:  pop
  IL_18d1:  dup
  IL_18d2:  ldc.i4.5
  IL_18d3:  shr
  IL_18d4:  pop
  IL_18d9:  dup
  IL_18da:  ldc.i4.0
  IL_18db:  shl
  IL_18dc:  pop
  IL_18e1:  dup
  IL_18e2:  ldc.i4.8
  IL_18e3:  mul
  IL_18e4:  pop
  IL_18e9:  dup
  IL_18ea:  ldc.i4.1
  IL_18eb:  or
  IL_18ec:  pop
  IL_18f1:  dup
  IL_18f2:  ldc.i4.1
  IL_18f3:  and
  IL_18f4:  pop
  IL_18f9:  dup
  IL_18fa:  ldc.i4.0
  IL_18fb:  sub
  IL_18fc:  pop
  IL_1901:  dup
  IL_1902:  ldc.i4.5
  IL_1903:  shl
  IL_1904:  pop
  IL_1909:  dup
  IL_190a:  ldc.i4.8
  IL_190b:  or
  IL_190c:  pop
  IL_1911:  dup
  IL_1912:  ldc.i4.1
  IL_1913:  sub
  IL_1914:  pop
  IL_1919:  dup
  IL_191a:  ldc.i4.0
  IL_191b:  add
  IL_191c:  pop
  IL_1921:  dup
  IL_1922:  ldc.i4.6
  IL_1923:  add
  IL_1924:  pop
  IL_1929:  dup
  IL_192a:  ldc.i4.8
  IL_192b:  shl
  IL_192c:  pop
  IL_1931:  dup
  IL_1932:  ldc.i4.0
  IL_1933:  mul
  IL_1934:  pop
  IL_1939:  dup
  IL_193a:  ldc.i4.6
  IL_193b:  and
  IL_193c:  pop
  IL_1941:  dup
  IL_1942:  ldc.i4.2
  IL_1943:  and
  IL_1944:  pop
  IL_1949:  dup
  IL_194a:  ldc.i4.1
  IL_194b:  shl
  IL_194c:  pop
  IL_1951:  dup
  IL_1952:  ldc.i4.1
  IL_1953:  sub
  IL_1954:  pop
  IL_1959:  dup
  IL_195a:  ldc.i4.3
  IL_195b:  add
  IL_195c:  pop
  IL_1961:  dup
  IL_1962:  ldc.i4.3
  IL_1963:  add
  IL_1964:  pop
  IL_1969:  dup
  IL_196a:  ldc.i4.7
  IL_196b:  and
  IL_196c:  pop
  IL_1971:  dup
  IL_1972:  ldc.i4.4
  IL_1973:  sub
  IL_1974:  pop
  IL_1979:  dup
  IL_197a:  ldc.i4.3
  IL_197b:  and
  IL_197c:  pop
  IL_1981:  dup
  IL_1982:  ldc.i4.7
  IL_1983:  mul
  IL_1984:  pop
  IL_1989:  dup
  IL_198a:  ldc.i4.6
  IL_198b:  mul
  IL_198c:  pop
  IL_1991:  dup
  IL_1992:  ldc.i4.6
  IL_1993:  sub
  IL_1994:  pop
  IL_1999:  dup
  IL_199a:  ldc.i4.5
  IL_199b:  and
  IL_199c:  pop
  IL_19a1:  dup
  IL_19a2:  ldc.i4.5
  IL_19a3:  or
  IL_19a4:  pop
  IL_19a9:  dup
  IL_19aa:  ldc.i4.4
  IL_19ab:  shl
  IL_19ac:  pop
  IL_19b1:  dup
  IL_19b2:  ldc.i4.6
  IL_19b3:  shl
  IL_19b4:  pop
  IL_19b9:  dup
  IL_19ba:  ldc.i4.5
  IL_19bb:  shr
  IL_19bc:  pop
  IL_19c1:  dup
  IL_19c2:  ldc.i4.5
  IL_19c3:  mul
  IL_19c4:  pop
  IL_19c9:  dup
  IL_19ca:  ldc.i4.6
  IL_19cb:  mul
  IL_19cc:  pop
  IL_19d1:  dup
  IL_19d2:  ldc.i4.5
  IL_19d3:  xor
  IL_19d4:  pop
  IL_19d9:  dup
  IL_19da:  ldc.i4.0
  IL_19db:  xor
  IL_19dc:  pop
  IL_19e1:  dup
  IL_19e2:  ldc.i4.0
  IL_19e3:  and
  IL_19e4:  pop
  IL_19e9:  dup
  IL_19ea:  ldc.i4.4
  IL_19eb:  add
  IL_19ec:  pop
  IL_19f1:  dup
  IL_19f2:  ldc.i4.3
  IL_19f3:  sub
  IL_19f4:  pop
  IL_19f9:  dup
  IL_19fa:  ldc.i4.5
  IL_19fb:  shl
  IL_19fc:  pop
  IL_1a01:  dup
  IL_1a02:  ldc.i4.5
  IL_1a03:  add
  IL_1a04:  pop
  IL_1a09:  dup
  IL_1a0a:  ldc.i4.6
  IL_1a0b:  shr
  IL_1a0c:  pop
  IL_1a11:  dup
  IL_1a12:  ldc.i4.4
  IL_1a13:  mul
  IL_1a14:  pop
  IL_1a19:  dup
  IL_1a1a:  ldc.i4.1
  IL_1a1b:  add
  IL_1a1c:  pop
  IL_1a21:  dup
  IL_1a22:  ldc.i4.2
  IL_1a23:  mul
  IL_1a24:  pop
  IL_1a29:  dup
  IL_1a2a:  ldc.i4.0
  IL_1a2b:  mul
  IL_1a2c:  pop
  IL_1a31:  dup
  IL_1a32:  ldc.i4.6
  IL_1a33:  or
  IL_1a34:  pop
  IL_1a39:  dup
  IL_1a3a:  ldc.i4.3
  IL_1a3b:  or
  IL_1a3c:  pop
  IL_1a41:  dup
  IL_1a42:  ldc.i4.4
  IL_1a43:  sub
  IL_1a44:  pop
  IL_1a49:  dup
  IL_1a4a:  ldc.i4.2
  IL_1a4b:  sub
  IL_1a4c:  pop
  IL_1a51:  dup
  IL_1a52:  ldc.i4.4
  IL_1a53:  sub
  IL_1a54:  pop
  IL_1a59:  dup
  IL_1a5a:  ldc.i4.1
  IL_1a5b:  mul
  IL_1a5c:  pop
  IL_1a61:  dup
  IL_1a62:  ldc.i4.2
  IL_1a63:  mul
  IL_1a64:  pop
  IL_1a69:  dup
  IL_1a6a:  ldc.i4.2
  IL_1a6b:  or
  IL_1a6c:  pop
  IL_1a71:  dup
  IL_1a72:  ldc.i4.6
  IL_1a73:  shl
  IL_1a74:  pop
  IL_1a79:  dup
  IL_1a7a:  ldc.i4.3
  IL_1a7b:  xor
  IL_1a7c:  pop
  IL_1a81:  dup
  IL_1a82:  ldc.i4.2
  IL_1a83:  or
  IL_1a84:  pop
  IL_1a89:  dup
  IL_1a8a:  ldc.i4.7
  IL_1a8b:  shr
  IL_1a8c:  pop
  IL_1a91:  dup
  IL_1a92:  ldc.i4.6
  IL_1a93:  or
  IL_1a94:  pop
  IL_1a99:  dup
  IL_1a9a:  ldc.i4.1
  IL_1a9b:  sub
  IL_1a9c:  pop
  IL_1aa1:  dup
  IL_1aa2:  ldc.i4.1
  IL_1aa3:  shr
  IL_1aa4:  pop
  IL_1aa9:  dup
  IL_1aaa:  ldc.i4.8
  IL_1aab:  mul
  IL_1aac:  pop
  IL_1ab1:  dup
  IL_1ab2:  ldc.i4.4
  IL_1ab3:  and
  IL_1ab4:  pop
  IL_1ab9:  dup
  IL_1aba:  ldc.i4.6
  IL_1abb:  sub
  IL_1abc:  pop
  IL_1ac1:  dup
  IL_1ac2:  ldc.i4.3
  IL_1ac3:  shl
  IL_1ac4:  pop
  IL_1ac9:  dup
  IL_1aca:  ldc.i4.4
  IL_1acb:  mul
  IL_1acc:  pop
  IL_1ad1:  dup
  IL_1ad2:  ldc.i4.8
  IL_1ad3:  xor
  IL_1ad4:  pop
  IL_1ad9:  dup
  IL_1ada:  ldc.i4.0
  IL_1adb:  shl
  IL_1adc:  pop
  IL_1ae1:  dup
  IL_1ae2:  ldc.i4.7
  IL_1ae3:  xor
  IL_1ae4:  pop
  IL_1ae9:  dup
  IL_1aea:  ldc.i4.3
  IL_1aeb:  sub
  IL_1aec:  pop
  IL_1af1:  dup
  IL_1af2:  ldc.i4.0
  IL_1af3:  sub
  IL_1af4:  pop
  IL_1af9:  dup
  IL_1afa:  ldc.i4.2
  IL_1afb:  and
  IL_1afc:  pop
  IL_1b01:  dup
  IL_1b02:  ldc.i4.8
  IL_1b03:  shl
  IL_1b04:  pop
  IL_1b09:  dup
  IL_1b0a:  ldc.i4.8
  IL_1b0b:  shr
  IL_1b0c:  pop
  IL_1b11:  dup
  IL_1b12:  ldc.i4.0
  IL_1b13:  shr
  IL_1b14:  pop
  IL_1b19:  dup
  IL_1b1a:  ldc.i4.1
  IL_1b1b:  shr
  IL_1b1c:  pop
  IL_1b21:  dup
  IL_1b22:  ldc.i4.8
  IL_1b23:  add
  IL_1b24:  pop
  IL_1b29:  dup
  IL_1b2a:  ldc.i4.0
  IL_1b2b:  and
  IL_1b2c:  pop
  IL_1b31:  dup
  IL_1b32:  ldc.i4.1
  IL_1b33:  or
  IL_1b34:  pop
  IL_1b39:  dup
  IL_1b3a:  ldc.i4.7
  IL_1b3b:  xor
  IL_1b3c:  pop
  IL_1b41:  dup
  IL_1b42:  ldc.i4.5
  IL_1b43:  sub
  IL_1b44:  pop
  IL_1b49:  dup
  IL_1b4a:  ldc.i4.4
  IL_1b4b:  sub
  IL_1b4c:  pop
  IL_1b51:  dup
  IL_1b52:  ldc.i4.0
  IL_1b53:  sub
  IL_1b54:  pop
  IL_1b59:  dup
  IL_1b5a:  ldc.i4.3
  IL_1b5b:  and
  IL_1b5c:  pop
  IL_1b61:  dup
  IL_1b62:  ldc.i4.8
  IL_1b63:  mul
  IL_1b64:  pop
  IL_1b69:  dup
  IL_1b6a:  ldc.i4.7
  IL_1b6b:  mul
  IL_1b6c:  pop
  IL_1b71:  dup
  IL_1b72:  ldc.i4.4
  IL_1b73:  or
  IL_1b74:  pop
  IL_1b79:  dup
  IL_1b7a:  ldc.i4.1
  IL_1b7b:  or
  IL_1b7c:  pop
  IL_1b81:  dup
  IL_1b82:  ldc.i4.3
  IL_1b83:  sub
  IL_1b84:  pop
  IL_1b89:  dup
  IL_1b8a:  ldc.i4.3
  IL_1b8b:  and
  IL_1b8c:  pop
  IL_1b91:  dup
  IL_1b92:  ldc.i4.7
  IL_1b93:  or
  IL_1b94:  pop
  IL_1b99:  dup
  IL_1b9a:  ldc.i4.0
  IL_1b9b:  and
  IL_1b9c:  pop
  IL_1ba1:  dup
  IL_1ba2:  ldc.i4.3
  IL_1ba3:  and
  IL_1ba4:  pop
  IL_1ba9:  dup
  IL_1baa:  ldc.i4.2
  IL_1bab:  or
  IL_1bac:  pop
  IL_1bb1:  dup
  IL_1bb2:  ldc.i4.3
  IL_1bb3:  sub
  IL_1bb4:  pop
  IL_1bb9:  dup
  IL_1bba:  ldc.i4.3
  IL_1bbb:  or
  IL_1bbc:  pop
  IL_1bc1:  dup
  IL_1bc2:  ldc.i4.5
  IL_1bc3:  or
  IL_1bc4:  pop
  IL_1bc9:  dup
  IL_1bca:  ldc.i4.5
  IL_1bcb:  or
  IL_1bcc:  pop
  IL_1bd1:  dup
  IL_1bd2:  ldc.i4.6
  IL_1bd3:  add
  IL_1bd4:  pop
  IL_1bd9:  dup
  IL_1bda:  ldc.i4.7
  IL_1bdb:  and
  IL_1bdc:  pop
  IL_1be1:  dup
  IL_1be2:  ldc.i4.1
  IL_1be3:  sub
  IL_1be4:  pop
  IL_1be9:  dup
  IL_1bea:  ldc.i4.7
  IL_1beb:  shr
  IL_1bec:  pop
  IL_1bf1:  dup
  IL_1bf2:  ldc.i4.7
  IL_1bf3:  and
  IL_1bf4:  pop
  IL_1bf9:  dup
  IL_1bfa:  ldc.i4.1
  IL_1bfb:  add
  IL_1bfc:  pop
  IL_1c01:  dup
  IL_1c02:  ldc.i4.7
  IL_1c03:  add
  IL_1c04:  pop
  IL_1c09:  dup
  IL_1c0a:  ldc.i4.4
  IL_1c0b:  or
  IL_1c0c:  pop
  IL_1c11:  dup
  IL_1c12:  ldc.i4.5
  IL_1c13:  add
  IL_1c14:  pop
  IL_1c19:  dup
  IL_1c1a:  ldc.i4.6
  IL_1c1b:  shr
  IL_1c1c:  pop
  IL_1c21:  dup
  IL_1c22:  ldc.i4.0
  IL_1c23:  shr
  IL_1c24:  pop
  IL_1c29:  dup
  IL_1c2a:  ldc.i4.6
  IL_1c2b:  sub
  IL_1c2c:  pop
  IL_1c31:  dup
  IL_1c32:  ldc.i4.7
  IL_1c33:  shl
  IL_1c34:  pop
  IL_1c39:  dup
  IL_1c3a:  ldc.i4.3
  IL_1c3b:  and
  IL_1c3c:  pop
  IL_1c41:  dup
  IL_1c42:  ldc.i4.8
  IL_1c43:  or
  IL_1c44:  pop
  IL_1c49:  dup
  IL_1c4a:  ldc.i4.6
  IL_1c4b:  shl
  IL_1c4c:  pop
  IL_1c51:  dup
  IL_1c52:  ldc.i4.3
  IL_1c53:  sub
  IL_1c54:  pop
  IL_1c59:  dup
  IL_1c5a:  ldc.i4.1
  IL_1c5b:  mul
  IL_1c5c:  pop
  IL_1c61:  dup
  IL_1c62:  ldc.i4.3
  IL_1c63:  shl
  IL_1c64:  pop
  IL_1c69:  dup
  IL_1c6a:  ldc.i4.5
  IL_1c6b:  shr
  IL_1c6c:  pop
  IL_1c71:  dup
  IL_1c72:  ldc.i4.1
  IL_1c73:  xor
  IL_1c74:  pop
  IL_1c79:  dup
  IL_1c7a:  ldc.i4.8
  IL_1c7b:  shl
  IL_1c7c:  pop
  IL_1c81:  dup
  IL_1c82:  ldc.i4.6
  IL_1c83:  mul
  IL_1c84:  pop
  IL_1c89:  dup
  IL_1c8a:  ldc.i4.0
  IL_1c8b:  mul
  IL_1c8c:  pop
  IL_1c91:  dup
  IL_1c92:  ldc.i4.6
  IL_1c93:  xor
  IL_1c94:  pop
  IL_1c99:  dup
  IL_1c9a:  ldc.i4.2
  IL_1c9b:  shl
  IL_1c9c:  pop
  IL_1ca1:  dup
  IL_1ca2:  ldc.i4.2
  IL_1ca3:  or
  IL_1ca4:  pop
  IL_1ca9:  dup
  IL_1caa:  ldc.i4.8
  IL_1cab:  add
  IL_1cac:  pop
  IL_1cb1:  dup
  IL_1cb2:  ldc.i4.3
  IL_1cb3:  shr
  IL_1cb4:  pop
  IL_1cb9:  dup
  IL_1cba:  ldc.i4.8
  IL_1cbb:  shr
  IL_1cbc:  pop
  IL_1cc1:  dup
  IL_1cc2:  ldc.i4.6
  IL_1cc3:  add
  IL_1cc4:  pop
  IL_1cc9:  dup
  IL_1cca:  ldc.i4.6
  IL_1ccb:  or
  IL_1ccc:  pop
  IL_1cd1:  dup
  IL_1cd2:  ldc.i4.1
  IL_1cd3:  shl
  IL_1cd4:  pop
  IL_1cd9:  dup
  IL_1cda:  ldc.i4.5
  IL_1cdb:  shl
  IL_1cdc:  pop
  IL_1ce1:  dup
  IL_1ce2:  ldc.i4.8
  IL_1ce3:  sub
  IL_1ce4:  pop
  IL_1ce9:  dup
  IL_1cea:  ldc.i4.4
  IL_1ceb:  or
  IL_1cec:  pop
  IL_1cf1:  dup
  IL_1cf2:  ldc.i4.3
  IL_1cf3:  add
  IL_1cf4:  pop
  IL_1cf9:  dup
  IL_1cfa:  ldc.i4.4
  IL_1cfb:  shl
  IL_1cfc:  pop
  IL_1d01:  dup
  IL_1d02:  ldc.i4.6
  IL_1d03:  add
  IL_1d04:  pop
  IL_1d09:  dup
  IL_1d0a:  ldc.i4.4
  IL_1d0b:  shl
  IL_1d0c:  pop
  IL_1d11:  dup
  IL_1d12:  ldc.i4.4
  IL_1d13:  add
  IL_1d14:  pop
  IL_1d19:  dup
  IL_1d1a:  ldc.i4.1
  IL_1d1b:  shl
  IL_1d1c:  pop
  IL_1d21:  dup
  IL_1d22:  ldc.i4.5
  IL_1d23:  sub
  IL_1d24:  pop
  IL_1d29:  dup
  IL_1d2a:  ldc.i4.3
  IL_1d2b:  shl
  IL_1d2c:  pop
  IL_1d31:  dup
  IL_1d32:  ldc.i4.3
  IL_1d33:  sub
  IL_1d34:  pop
  IL_1d39:  dup
  IL_1d3a:  ldc.i4.7
  IL_1d3b:  and
  IL_1d3c:  pop
  IL_1d41:  dup
  IL_1d42:  ldc.i4.6
  IL_1d43:  or
  IL_1d44:  pop
  IL_1d49:  dup
  IL_1d4a:  ldc.i4.8
  IL_1d4b:  add
  IL_1d4c:  pop
  IL_1d51:  dup
  IL_1d52:  ldc.i4.6
  IL_1d53:  add
  IL_1d54:  pop
  IL_1d59:  dup
  IL_1d5a:  ldc.i4.4
  IL_1d5b:  add
  IL_1d5c:  pop
  IL_1d61:  dup
  IL_1d62:  ldc.i4.5
  IL_1d63:  xor
  IL_1d64:  pop
  IL_1d69:  dup
  IL_1d6a:  ldc.i4.3
  IL_1d6b:  xor
  IL_1d6c:  pop
  IL_1d71:  dup
  IL_1d72:  ldc.i4.8
  IL_1d73:  mul
  IL_1d74:  pop
  IL_1d79:  dup
  IL_1d7a:  ldc.i4.0
  IL_1d7b:  sub
  IL_1d7c:  pop
  IL_1d81:  dup
  IL_1d82:  ldc.i4.2
  IL_1d83:  mul
  IL_1d84:  pop
  IL_1d89:  dup
  IL_1d8a:  ldc.i4.2
  IL_1d8b:  mul
  IL_1d8c:  pop
  IL_1d91:  dup
  IL_1d92:  ldc.i4.1
  IL_1d93:  mul
  IL_1d94:  pop
  IL_1d99:  dup
  IL_1d9a:  ldc.i4.1
  IL_1d9b:  shr
  IL_1d9c:  pop
  IL_1da1:  dup
  IL_1da2:  ldc.i4.8
  IL_1da3:  sub
  IL_1da4:  pop
  IL_1da9:  dup
  IL_1daa:  ldc.i4.2
  IL_1dab:  or
  IL_1dac:  pop
  IL_1db1:  dup
  IL_1db2:  ldc.i4.2
  IL_1db3:  sub
  IL_1db4:  pop
  IL_1db9:  dup
  IL_1dba:  ldc.i4.2
  IL_1dbb:  and
  IL_1dbc:  pop
  IL_1dc1:  dup
  IL_1dc2:  ldc.i4.3
  IL_1dc3:  shr
  IL_1dc4:  pop
  IL_1dc9:  dup
  IL_1dca:  ldc.i4.2
  IL_1dcb:  xor
  IL_1dcc:  pop
  IL_1dd1:  dup
  IL_1dd2:  ldc.i4.0
  IL_1dd3:  xor
  IL_1dd4:  pop
  IL_1dd9:  dup
  IL_1dda:  ldc.i4.7
  IL_1ddb:  shr
  IL_1ddc:  pop
  IL_1de1:  dup
  IL_1de2:  ldc.i4.5
  IL_1de3:  mul
  IL_1de4:  pop
  IL_1de9:  dup
  IL_1dea:  ldc.i4.8
  IL_1deb:  add
  IL_1dec:  pop
  IL_1df1:  dup
  IL_1df2:  ldc.i4.5
  IL_1df3:  shr
  IL_1df4:  pop
  IL_1df9:  dup
  IL_1dfa:  ldc.i4.5
  IL_1dfb:  mul
  IL_1dfc:  pop
  IL_1e01:  dup
  IL_1e02:  ldc.i4.1
  IL_1e03:  sub
  IL_1e04:  pop
  IL_1e09:  dup
  IL_1e0a:  ldc.i4.1
  IL_1e0b:  add
  IL_1e0c:  pop
  IL_1e11:  dup
  IL_1e12:  ldc.i4.8
  IL_1e13:  shl
  IL_1e14:  pop
  IL_1e19:  dup
  IL_1e1a:  ldc.i4.5
  IL_1e1b:  xor
  IL_1e1c:  pop
  IL_1e21:  dup
  IL_1e22:  ldc.i4.7
  IL_1e23:  mul
  IL_1e24:  pop
  IL_1e29:  dup
  IL_1e2a:  ldc.i4.1
  IL_1e2b:  shr
  IL_1e2c:  pop
  IL_1e31:  dup
  IL_1e32:  ldc.i4.7
  IL_1e33:  shl
  IL_1e34:  pop
  IL_1e39:  dup
  IL_1e3a:  ldc.i4.7
  IL_1e3b:  xor
  IL_1e3c:  pop
  IL_1e41:  dup
  IL_1e42:  ldc.i4.1
  IL_1e43:  shr
  IL_1e44:  pop
  IL_1e49:  dup
  IL_1e4a:  ldc.i4.1
  IL_1e4b:  mul
  IL_1e4c:  pop
  IL_1e51:  dup
  IL_1e52:  ldc.i4.0
  IL_1e53:  shr
  IL_1e54:  pop
  IL_1e59:  dup
  IL_1e5a:  ldc.i4.1
  IL_1e5b:  or
  IL_1e5c:  pop
  IL_1e61:  dup
  IL_1e62:  ldc.i4.3
  IL_1e63:  shl
  IL_1e64:  pop
  IL_1e69:  dup
  IL_1e6a:  ldc.i4.5
  IL_1e6b:  and
  IL_1e6c:  pop
  IL_1e71:  dup
  IL_1e72:  ldc.i4.2
  IL_1e73:  sub
  IL_1e74:  pop
  IL_1e79:  dup
  IL_1e7a:  ldc.i4.0
  IL_1e7b:  mul
  IL_1e7c:  pop
  IL_1e81:  dup
  IL_1e82:  ldc.i4.4
  IL_1e83:  or
  IL_1e84:  pop
  IL_1e89:  dup
  IL_1e8a:  ldc.i4.6
  IL_1e8b:  sub
  IL_1e8c:  pop
  IL_1e91:  dup
  IL_1e92:  ldc.i4.8
  IL_1e93:  sub
  IL_1e94:  pop
  IL_1e99:  dup
  IL_1e9a:  ldc.i4.7
  IL_1e9b:  and
  IL_1e9c:  pop
  IL_1ea1:  dup
  IL_1ea2:  ldc.i4.0
  IL_1ea3:  and
  IL_1ea4:  pop
  IL_1ea9:  dup
  IL_1eaa:  ldc.i4.5
  IL_1eab:  and
  IL_1eac:  pop
  IL_1eb1:  dup
  IL_1eb2:  ldc.i4.2
  IL_1eb3:  sub
  IL_1eb4:  pop
  IL_1eb9:  dup
  IL_1eba:  ldc.i4.7
  IL_1ebb:  add
  IL_1ebc:  pop
  IL_1ec1:  dup
  IL_1ec2:  ldc.i4.4
  IL_1ec3:  shl
  IL_1ec4:  pop
  IL_1ec9:  dup
  IL_1eca:  ldc.i4.7
  IL_1ecb:  add
  IL_1ecc:  pop
  IL_1ed1:  dup
  IL_1ed2:  ldc.i4.4
  IL_1ed3:  mul
  IL_1ed4:  pop
  IL_1ed9:  dup
  IL_1eda:  ldc.i4.2
  IL_1edb:  xor
  IL_1edc:  pop
  IL_1ee1:  dup
  IL_1ee2:  ldc.i4.8
  IL_1ee3:  mul
  IL_1ee4:  pop
  IL_1ee9:  dup
  IL_1eea:  ldc.i4.5
  IL_1eeb:  mul
  IL_1eec:  pop
  IL_1ef1:  dup
  IL_1ef2:  ldc.i4.8
  IL_1ef3:  and
  IL_1ef4:  pop
  IL_1ef9:  dup
  IL_1efa:  ldc.i4.0
  IL_1efb:  shr
  IL_1efc:  pop
  IL_1f01:  dup
  IL_1f02:  ldc.i4.2
  IL_1f03:  or
  IL_1f04:  pop
  IL_1f09:  dup
  IL_1f0a:  ldc.i4.0
  IL_1f0b:  and
  IL_1f0c:  pop
  IL_1f11:  dup
  IL_1f12:  ldc.i4.7
  IL_1f13:  shr
  IL_1f14:  pop
  IL_1f19:  dup
  IL_1f1a:  ldc.i4.4
  IL_1f1b:  add
  IL_1f1c:  pop
  IL_1f21:  dup
  IL_1f22:  ldc.i4.7
  IL_1f23:  mul
  IL_1f24:  pop
  IL_1f29:  dup
  IL_1f2a:  ldc.i4.1
  IL_1f2b:  sub
  IL_1f2c:  pop
  IL_1f31:  dup
  IL_1f32:  ldc.i4.0
  IL_1f33:  mul
  IL_1f34:  pop
  IL_1f39:  dup
  IL_1f3a:  ldc.i4.6
  IL_1f3b:  sub
  IL_1f3c:  pop
  IL_1f41:  dup
  IL_1f42:  ldc.i4.6
  IL_1f43:  shl
  IL_1f44:  pop
  IL_1f49:  dup
  IL_1f4a:  ldc.i4.2
  IL_1f4b:  and
  IL_1f4c:  pop
  IL_1f51:  dup
  IL_1f52:  ldc.i4.2
  IL_1f53:  add
  IL_1f54:  pop
  IL_1f59:  dup
  IL_1f5a:  ldc.i4.7
  IL_1f5b:  sub
  IL_1f5c:  pop
  IL_1f61:  dup
  IL_1f62:  ldc.i4.6
  IL_1f63:  mul
  IL_1f64:  pop
  IL_1f69:  dup
  IL_1f6a:  ldc.i4.1
  IL_1f6b:  shr
  IL_1f6c:  pop
  IL_1f71:  dup
  IL_1f72:  ldc.i4.5
  IL_1f73:  shl
  IL_1f74:  pop
  IL_1f79:  dup
  IL_1f7a:  ldc.i4.7
  IL_1f7b:  mul
  IL_1f7c:  pop
  IL_1f81:  dup
  IL_1f82:  ldc.i4.6
  IL_1f83:  or
  IL_1f84:  pop
  IL_1f89:  dup
  IL_1f8a:  ldc.i4.5
  IL_1f8b:  mul
  IL_1f8c:  pop
  IL_1f91:  dup
  IL_1f92:  ldc.i4.1
  IL_1f93:  or
  IL_1f94:  pop
  IL_1f99:  dup
  IL_1f9a:  ldc.i4.2
  IL_1f9b:  and
  IL_1f9c:  pop
  IL_1fa1:  dup
  IL_1fa2:  ldc.i4.0
  IL_1fa3:  and
  IL_1fa4:  pop
  IL_1fa9:  dup
  IL_1faa:  ldc.i4.1
  IL_1fab:  sub
  IL_1fac:  pop
  IL_1fb1:  dup
  IL_1fb2:  ldc.i4.1
  IL_1fb3:  add
  IL_1fb4:  pop
  IL_1fb9:  dup
  IL_1fba:  ldc.i4.3
  IL_1fbb:  shr
  IL_1fbc:  pop
  IL_1fc1:  dup
  IL_1fc2:  ldc.i4.5
  IL_1fc3:  xor
  IL_1fc4:  pop
  IL_1fc9:  dup
  IL_1fca:  ldc.i4.5
  IL_1fcb:  mul
  IL_1fcc:  pop
  IL_1fd1:  dup
  IL_1fd2:  ldc.i4.4
  IL_1fd3:  mul
  IL_1fd4:  pop
  IL_1fd9:  dup
  IL_1fda:  ldc.i4.3
  IL_1fdb:  sub
  IL_1fdc:  pop
  IL_1fe1:  dup
  IL_1fe2:  ldc.i4.1
  IL_1fe3:  or
  IL_1fe4:  pop
  IL_1fe9:  dup
  IL_1fea:  ldc.i4.1
  IL_1feb:  shl
  IL_1fec:  pop
  IL_1ff1:  dup
  IL_1ff2:  ldc.i4.5
  IL_1ff3:  and
  IL_1ff4:  pop
  IL_1ff9:  dup
  IL_1ffa:  ldc.i4.4
  IL_1ffb:  mul
  IL_1ffc:  pop
  IL_2001:  dup
  IL_2002:  ldc.i4.4
  IL_2003:  and
  IL_2004:  pop
  IL_2009:  dup
  IL_200a:  ldc.i4.3
  IL_200b:  sub
  IL_200c:  pop
  IL_2011:  dup
  IL_2012:  ldc.i4.6
  IL_2013:  mul
  IL_2014:  pop
  IL_2019:  dup
  IL_201a:  ldc.i4.0
  IL_201b:  shr
  IL_201c:  pop
  IL_2021:  dup
  IL_2022:  ldc.i4.2
  IL_2023:  xor
  IL_2024:  pop
  IL_2029:  dup
  IL_202a:  ldc.i4.6
  IL_202b:  xor
  IL_202c:  pop
  IL_2031:  dup
  IL_2032:  ldc.i4.7
  IL_2033:  shr
  IL_2034:  pop
  IL_2039:  dup
  IL_203a:  ldc.i4.0
  IL_203b:  shl
  IL_203c:  pop
  IL_2041:  dup
  IL_2042:  ldc.i4.8
  IL_2043:  xor
  IL_2044:  pop
  IL_2049:  dup
  IL_204a:  ldc.i4.0
  IL_204b:  sub
  IL_204c:  pop
  IL_2051:  dup
  IL_2052:  ldc.i4.8
  IL_2053:  shl
  IL_2054:  pop
  IL_2059:  dup
  IL_205a:  ldc.i4.7
  IL_205b:  xor
  IL_205c:  pop
  IL_2061:  dup
  IL_2062:  ldc.i4.4
  IL_2063:  xor
  IL_2064:  pop
  IL_2069:  dup
  IL_206a:  ldc.i4.1
  IL_206b:  sub
  IL_206c:  pop
  IL_2071:  dup
  IL_2072:  ldc.i4.7
  IL_2073:  shr
  IL_2074:  pop
  IL_2079:  dup
  IL_207a:  ldc.i4.2
  IL_207b:  sub
  IL_207c:  pop
  IL_2081:  dup
  IL_2082:  ldc.i4.3
  IL_2083:  or
  IL_2084:  pop
  IL_2089:  dup
  IL_208a:  ldc.i4.6
  IL_208b:  mul
  IL_208c:  pop
  IL_2091:  dup
  IL_2092:  ldc.i4.6
  IL_2093:  sub
  IL_2094:  pop
  IL_2099:  dup
  IL_209a:  ldc.i4.8
  IL_209b:  sub
  IL_209c:  pop
  IL_20a1:  dup
  IL_20a2:  ldc.i4.2
  IL_20a3:  and
  IL_20a4:  pop
  IL_20a9:  dup
  IL_20aa:  ldc.i4.0
  IL_20ab:  add
  IL_20ac:  pop
  IL_20b1:  dup
  IL_20b2:  ldc.i4.4
  IL_20b3:  or
  IL_20b4:  pop
  IL_20b9:  dup
  IL_20ba:  ldc.i4.3
  IL_20bb:  shl
  IL_20bc:  pop
  IL_20c1:  dup
  IL_20c2:  ldc.i4.7
  IL_20c3:  shl
  IL_20c4:  pop
  IL_20c9:  dup
  IL_20ca:  ldc.i4.5
  IL_20cb:  add
  IL_20cc:  pop
  IL_20d1:  dup
  IL_20d2:  ldc.i4.6
  IL_20d3:  mul
  IL_20d4:  pop
  IL_20d9:  dup
  IL_20da:  ldc.i4.2
  IL_20db:  or
  IL_20dc:  pop
  IL_20e1:  dup
  IL_20e2:  ldc.i4.0
  IL_20e3:  or
  IL_20e4:  pop
  IL_20e9:  dup
  IL_20ea:  ldc.i4.6
  IL_20eb:  mul
  IL_20ec:  pop
  IL_20f1:  dup
  IL_20f2:  ldc.i4.1
  IL_20f3:  and
  IL_20f4:  pop
  IL_20f9:  dup
  IL_20fa:  ldc.i4.8
  IL_20fb:  shl
  IL_20fc:  pop
  IL_2101:  dup
  IL_2102:  ldc.i4.1
  IL_2103:  shl
  IL_2104:  pop
  IL_2109:  dup
  IL_210a:  ldc.i4.0
  IL_210b:  or
  IL_210c:  pop
  IL_2111:  dup
  IL_2112:  ldc.i4.8
  IL_2113:  shr
  IL_2114:  pop
  IL_2119:  dup
  IL_211a:  ldc.i4.8
  IL_211b:  shr
  IL_211c:  pop
  IL_2121:  dup
  IL_2122:  ldc.i4.4
  IL_2123:  and
  IL_2124:  pop
  IL_2129:  dup
  IL_212a:  ldc.i4.7
  IL_212b:  sub
  IL_212c:  pop
  IL_2131:  dup
  IL_2132:  ldc.i4.4
  IL_2133:  and
  IL_2134:  pop
  IL_2139:  dup
  IL_213a:  ldc.i4.8
  IL_213b:  shl
  IL_213c:  pop
  IL_2141:  dup
  IL_2142:  ldc.i4.8
  IL_2143:  shr
  IL_2144:  pop
  IL_2149:  dup
  IL_214a:  ldc.i4.3
  IL_214b:  sub
  IL_214c:  pop
  IL_2151:  dup
  IL_2152:  ldc.i4.1
  IL_2153:  sub
  IL_2154:  pop
  IL_2159:  dup
  IL_215a:  ldc.i4.4
  IL_215b:  add
  IL_215c:  pop
  IL_2161:  dup
  IL_2162:  ldc.i4.5
  IL_2163:  and
  IL_2164:  pop
  IL_2169:  dup
  IL_216a:  ldc.i4.2
  IL_216b:  shr
  IL_216c:  pop
  IL_2171:  dup
  IL_2172:  ldc.i4.4
  IL_2173:  shl
  IL_2174:  pop
  IL_2179:  dup
  IL_217a:  ldc.i4.0
  IL_217b:  xor
  IL_217c:  pop
  IL_2181:  dup
  IL_2182:  ldc.i4.8
  IL_2183:  shl
  IL_2184:  pop
  IL_2189:  dup
  IL_218a:  ldc.i4.7
  IL_218b:  or
  IL_218c:  pop
  IL_2191:  dup
  IL_2192:  ldc.i4.7
  IL_2193:  xor
  IL_2194:  pop
  IL_2199:  dup
  IL_219a:  ldc.i4.5
  IL_219b:  sub
  IL_219c:  pop
  IL_21a1:  dup
  IL_21a2:  ldc.i4.6
  IL_21a3:  or
  IL_21a4:  pop
  IL_21a9:  dup
  IL_21aa:  ldc.i4.5
  IL_21ab:  or
  IL_21ac:  pop
  IL_21b1:  dup
  IL_21b2:  ldc.i4.4
  IL_21b3:  sub
  IL_21b4:  pop
  IL_21b9:  dup
  IL_21ba:  ldc.i4.3
  IL_21bb:  sub
  IL_21bc:  pop
  IL_21c1:  dup
  IL_21c2:  ldc.i4.0
  IL_21c3:  sub
  IL_21c4:  pop
  IL_21c9:  dup
  IL_21ca:  ldc.i4.3
  IL_21cb:  shl
  IL_21cc:  pop
  IL_21d1:  dup
  IL_21d2:  ldc.i4.4
  IL_21d3:  shr
  IL_21d4:  pop
  IL_21d9:  dup
  IL_21da:  ldc.i4.3
  IL_21db:  or
  IL_21dc:  pop
  IL_21e1:  dup
  IL_21e2:  ldc.i4.0
  IL_21e3:  add
  IL_21e4:  pop
  IL_21e9:  dup
  IL_21ea:  ldc.i4.2
  IL_21eb:  sub
  IL_21ec:  pop
  IL_21f1:  dup
  IL_21f2:  ldc.i4.3
  IL_21f3:  sub
  IL_21f4:  pop
  IL_21f9:  dup
  IL_21fa:  ldc.i4.2
  IL_21fb:  or
  IL_21fc:  pop
  IL_2201:  dup
  IL_2202:  ldc.i4.7
  IL_2203:  xor
  IL_2204:  pop
  IL_2209:  dup
  IL_220a:  ldc.i4.2
  IL_220b:  or
  IL_220c:  pop
  IL_2211:  dup
  IL_2212:  ldc.i4.8
  IL_2213:  or
  IL_2214:  pop
  IL_2219:  dup
  IL_221a:  ldc.i4.5
  IL_221b:  shl
  IL_221c:  pop
  IL_2221:  dup
  IL_2222:  ldc.i4.0
  IL_2223:  mul
  IL_2224:  pop
  IL_2229:  dup
  IL_222a:  ldc.i4.4
  IL_222b:  sub
  IL_222c:  pop
  IL_2231:  dup
  IL_2232:  ldc.i4.5
  IL_2233:  shl
  IL_2234:  pop
  IL_2239:  dup
  IL_223a:  ldc.i4.5
  IL_223b:  xor
  IL_223c:  pop
  IL_2241:  dup
  IL_2242:  ldc.i4.3
  IL_2243:  add
  IL_2244:  pop
  IL_2249:  dup
  IL_224a:  ldc.i4.6
  IL_224b:  mul
  IL_224c:  pop
  IL_2251:  dup
  IL_2252:  ldc.i4.2
  IL_2253:  xor
  IL_2254:  pop
  IL_2259:  dup
  IL_225a:  ldc.i4.8
  IL_225b:  sub
  IL_225c:  pop
  IL_2261:  dup
  IL_2262:  ldc.i4.0
  IL_2263:  and
  IL_2264:  pop
  IL_2269:  dup
  IL_226a:  ldc.i4.8
  IL_226b:  and
  IL_226c:  pop
  IL_2271:  dup
  IL_2272:  ldc.i4.2
  IL_2273:  xor
  IL_2274:  pop
  IL_2279:  dup
  IL_227a:  ldc.i4.6
  IL_227b:  shl
  IL_227c:  pop
  IL_2281:  dup
  IL_2282:  ldc.i4.5
  IL_2283:  shr
  IL_2284:  pop
  IL_2289:  dup
  IL_228a:  ldc.i4.1
  IL_228b:  add
  IL_228c:  pop
  IL_2291:  dup
  IL_2292:  ldc.i4.8
  IL_2293:  xor
  IL_2294:  pop
  IL_2299:  dup
  IL_229a:  ldc.i4.2
  IL_229b:  sub
  IL_229c:  pop
  IL_22a1:  dup
  IL_22a2:  ldc.i4.4
  IL_22a3:  and
  IL_22a4:  pop
  IL_22a9:  dup
  IL_22aa:  ldc.i4.4
  IL_22ab:  sub
  IL_22ac:  pop
  IL_22b1:  dup
  IL_22b2:  ldc.i4.1
  IL_22b3:  sub
  IL_22b4:  pop
  IL_22b9:  dup
  IL_22ba:  ldc.i4.3
  IL_22bb:  shl
  IL_22bc:  pop
  IL_22c1:  dup
  IL_22c2:  ldc.i4.3
  IL_22c3:  and
  IL_22c4:  pop
  IL_22c9:  dup
  IL_22ca:  ldc.i4.7
  IL_22cb:  xor
  IL_22cc:  pop
  IL_22d1:  dup
  IL_22d2:  ldc.i4.4
  IL_22d3:  or
  IL_22d4:  pop
  IL_22d9:  dup
  IL_22da:  ldc.i4.6
  IL_22db:  mul
  IL_22dc:  pop
  IL_22e1:  dup
  IL_22e2:  ldc.i4.3
  IL_22e3:  add
  IL_22e4:  pop
  IL_22e9:  dup
  IL_22ea:  ldc.i4.7
  IL_22eb:  add
  IL_22ec:  pop
  IL_22f1:  dup
  IL_22f2:  ldc.i4.0
  IL_22f3:  and
  IL_22f4:  pop
  IL_22f9:  dup
  IL_22fa:  ldc.i4.2
  IL_22fb:  shl
  IL_22fc:  pop
  IL_2301:  dup
  IL_2302:  ldc.i4.5
  IL_2303:  or
  IL_2304:  pop
  IL_2309:  dup
  IL_230a:  ldc.i4.4
  IL_230b:  or
  IL_230c:  pop
  IL_2311:  dup
  IL_2312:  ldc.i4.3
  IL_2313:  shl
  IL_2314:  pop
  IL_2319:  dup
  IL_231a:  ldc.i4.5
  IL_231b:  and
  IL_231c:  pop
  IL_2321:  dup
  IL_2322:  ldc.i4.6
  IL_2323:  shl
  IL_2324:  pop
  IL_2329:  dup
  IL_232a:  ldc.i4.6
  IL_232b:  xor
  IL_232c:  pop
  IL_2331:  dup
  IL_2332:  ldc.i4.8
  IL_2333:  and
  IL_2334:  pop
  IL_2339:  dup
  IL_233a:  ldc.i4.0
  IL_233b:  or
  IL_233c:  pop
  IL_2341:  dup
  IL_2342:  ldc.i4.4
  IL_2343:  mul
  IL_2344:  pop
  IL_2349:  dup
  IL_234a:  ldc.i4.3
  IL_234b:  xor
  IL_234c:  pop
  IL_2351:  dup
  IL_2352:  ldc.i4.5
  IL_2353:  mul
  IL_2354:  pop
  IL_2359:  dup
  IL_235a:  ldc.i4.0
  IL_235b:  shr
  IL_235c:  pop
  IL_2361:  dup
  IL_2362:  ldc.i4.8
  IL_2363:  and
  IL_2364:  pop
  IL_2369:  dup
  IL_236a:  ldc.i4.6
  IL_236b:  or
  IL_236c:  pop
  IL_2371:  dup
  IL_2372:  ldc.i4.5
  IL_2373:  sub
  IL_2374:  pop
  IL_2379:  dup
  IL_237a:  ldc.i4.1
  IL_237b:  shr
  IL_237c:  pop
  IL_2381:  dup
  IL_2382:  ldc.i4.5
  IL_2383:  or
  IL_2384:  pop
  IL_2389:  dup
  IL_238a:  ldc.i4.5
  IL_238b:  sub
  IL_238c:  pop
  IL_2391:  dup
  IL_2392:  ldc.i4.6
  IL_2393:  xor
  IL_2394:  pop
  IL_2399:  dup
  IL_239a:  ldc.i4.1
  IL_239b:  or
  IL_239c:  pop
  IL_23a1:  dup
  IL_23a2:  ldc.i4.1
  IL_23a3:  or
  IL_23a4:  pop
  IL_23a9:  dup
  IL_23aa:  ldc.i4.6
  IL_23ab:  or
  IL_23ac:  pop
  IL_23b1:  dup
  IL_23b2:  ldc.i4.2
  IL_23b3:  mul
  IL_23b4:  pop
  IL_23b9:  dup
  IL_23ba:  ldc.i4.3
  IL_23bb:  shl
  IL_23bc:  pop
  IL_23c1:  dup
  IL_23c2:  ldc.i4.8
  IL_23c3:  or
  IL_23c4:  pop
  IL_23c9:  dup
  IL_23ca:  ldc.i4.5
  IL_23cb:  shr
  IL_23cc:  pop
  IL_23d1:  dup
  IL_23d2:  ldc.i4.3
  IL_23d3:  and
  IL_23d4:  pop
  IL_23d9:  dup
  IL_23da:  ldc.i4.8
  IL_23db:  mul
  IL_23dc:  pop
  IL_23e1:  dup
  IL_23e2:  ldc.i4.7
  IL_23e3:  and
  IL_23e4:  pop
  IL_23e9:  dup
  IL_23ea:  ldc.i4.0
  IL_23eb:  shr
  IL_23ec:  pop
  IL_23f1:  dup
  IL_23f2:  ldc.i4.7
  IL_23f3:  shr
  IL_23f4:  pop
  IL_23f9:  dup
  IL_23fa:  ldc.i4.1
  IL_23fb:  shl
  IL_23fc:  pop
  IL_2401:  dup
  IL_2402:  ldc.i4.3
  IL_2403:  add
  IL_2404:  pop
  IL_2409:  dup
  IL_240a:  ldc.i4.7
  IL_240b:  add
  IL_240c:  pop
  IL_2411:  dup
  IL_2412:  ldc.i4.1
  IL_2413:  or
  IL_2414:  pop
  IL_2419:  dup
  IL_241a:  ldc.i4.1
  IL_241b:  add
  IL_241c:  pop
  IL_2421:  dup
  IL_2422:  ldc.i4.1
  IL_2423:  shl
  IL_2424:  pop
  IL_2429:  dup
  IL_242a:  ldc.i4.2
  IL_242b:  shr
  IL_242c:  pop
  IL_2431:  dup
  IL_2432:  ldc.i4.5
  IL_2433:  sub
  IL_2434:  pop
  IL_2439:  dup
  IL_243a:  ldc.i4.3
  IL_243b:  shl
  IL_243c:  pop
  IL_2441:  dup
  IL_2442:  ldc.i4.3
  IL_2443:  add
  IL_2444:  pop
  IL_2449:  dup
  IL_244a:  ldc.i4.8
  IL_244b:  shr
  IL_244c:  pop
  IL_2451:  dup
  IL_2452:  ldc.i4.5
  IL_2453:  xor
  IL_2454:  pop
  IL_2459:  dup
  IL_245a:  ldc.i4.2
  IL_245b:  and
  IL_245c:  pop
  IL_2461:  dup
  IL_2462:  ldc.i4.8
  IL_2463:  mul
  IL_2464:  pop
  IL_2469:  dup
  IL_246a:  ldc.i4.2
  IL_246b:  and
  IL_246c:  pop
  IL_2471:  dup
  IL_2472:  ldc.i4.2
  IL_2473:  shl
  IL_2474:  pop
  IL_2479:  dup
  IL_247a:  ldc.i4.0
  IL_247b:  xor
  IL_247c:  pop
  IL_2481:  dup
  IL_2482:  ldc.i4.6
  IL_2483:  sub
  IL_2484:  pop
  IL_2489:  dup
  IL_248a:  ldc.i4.4
  IL_248b:  mul
  IL_248c:  pop
  IL_2491:  dup
  IL_2492:  ldc.i4.4
  IL_2493:  mul
  IL_2494:  pop
  IL_2499:  dup
  IL_249a:  ldc.i4.4
  IL_249b:  add
  IL_249c:  pop
  IL_24a1:  dup
  IL_24a2:  ldc.i4.6
  IL_24a3:  xor
  IL_24a4:  pop
  IL_24a9:  dup
  IL_24aa:  ldc.i4.4
  IL_24ab:  or
  IL_24ac:  pop
  IL_24b1:  dup
  IL_24b2:  ldc.i4.4
  IL_24b3:  mul
  IL_24b4:  pop
  IL_24b9:  dup
  IL_24ba:  ldc.i4.6
  IL_24bb:  shr
  IL_24bc:  pop
  IL_24c1:  dup
  IL_24c2:  ldc.i4.4
  IL_24c3:  xor
  IL_24c4:  pop
  IL_24c9:  dup
  IL_24ca:  ldc.i4.2
  IL_24cb:  shr
  IL_24cc:  pop
  IL_24d1:  dup
  IL_24d2:  ldc.i4.6
  IL_24d3:  shl
  IL_24d4:  pop
  IL_24d9:  dup
  IL_24da:  ldc.i4.7
  IL_24db:  or
  IL_24dc:  pop
  IL_24e1:  dup
  IL_24e2:  ldc.i4.6
  IL_24e3:  or
  IL_24e4:  pop
  IL_24e9:  dup
  IL_24ea:  ldc.i4.1
  IL_24eb:  and
  IL_24ec:  pop
  IL_24f1:  dup
  IL_24f2:  ldc.i4.5
  IL_24f3:  xor
  IL_24f4:  pop
  IL_24f9:  dup
  IL_24fa:  ldc.i4.7
  IL_24fb:  shr
  IL_24fc:  pop
  IL_2501:  dup
  IL_2502:  ldc.i4.3
  IL_2503:  and
  IL_2504:  pop
  IL_2509:  dup
  IL_250a:  ldc.i4.8
  IL_250b:  xor
  IL_250c:  pop
  IL_2511:  dup
  IL_2512:  ldc.i4.6
  IL_2513:  add
  IL_2514:  pop
  IL_2519:  dup
  IL_251a:  ldc.i4.0
  IL_251b:  add
  IL_251c:  pop
  IL_2521:  dup
  IL_2522:  ldc.i4.5
  IL_2523:  or
  IL_2524:  pop
  IL_2529:  dup
  IL_252a:  ldc.i4.8
  IL_252b:  add
  IL_252c:  pop
  IL_2531:  dup
  IL_2532:  ldc.i4.8
  IL_2533:  mul
  IL_2534:  pop
  IL_2539:  dup
  IL_253a:  ldc.i4.2
  IL_253b:  shl
  IL_253c:  pop
  IL_2541:  dup
  IL_2542:  ldc.i4.6
  IL_2543:  shr
  IL_2544:  pop
  IL_2549:  dup
  IL_254a:  ldc.i4.2
  IL_254b:  and
  IL_254c:  pop
  IL_2551:  dup
  IL_2552:  ldc.i4.2
  IL_2553:  mul
  IL_2554:  pop
  IL_2559:  dup
  IL_255a:  ldc.i4.6
  IL_255b:  sub
  IL_255c:  pop
  IL_2561:  dup
  IL_2562:  ldc.i4.2
  IL_2563:  shl
  IL_2564:  pop
  IL_2569:  dup
  IL_256a:  ldc.i4.5
  IL_256b:  shl
  IL_256c:  pop
  IL_2571:  dup
  IL_2572:  ldc.i4.2
  IL_2573:  shr
  IL_2574:  pop
  IL_2579:  dup
  IL_257a:  ldc.i4.4
  IL_257b:  mul
  IL_257c:  pop
  IL_2581:  dup
  IL_2582:  ldc.i4.6
  IL_2583:  and
  IL_2584:  pop
  IL_2589:  dup
  IL_258a:  ldc.i4.4
  IL_258b:  shr
  IL_258c:  pop
  IL_2591:  dup
  IL_2592:  ldc.i4.3
  IL_2593:  mul
  IL_2594:  pop
  IL_2599:  dup
  IL_259a:  ldc.i4.8
  IL_259b:  mul
  IL_259c:  pop
  IL_25a1:  dup
  IL_25a2:  ldc.i4.3
  IL_25a3:  sub
  IL_25a4:  pop
  IL_25a9:  dup
  IL_25aa:  ldc.i4.1
  IL_25ab:  and
  IL_25ac:  pop
  IL_25b1:  dup
  IL_25b2:  ldc.i4.5
  IL_25b3:  and
  IL_25b4:  pop
  IL_25b9:  dup
  IL_25ba:  ldc.i4.8
  IL_25bb:  mul
  IL_25bc:  pop
  IL_25c1:  dup
  IL_25c2:  ldc.i4.6
  IL_25c3:  shl
  IL_25c4:  pop
  IL_25c9:  dup
  IL_25ca:  ldc.i4.0
  IL_25cb:  or
  IL_25cc:  pop
  IL_25d1:  dup
  IL_25d2:  ldc.i4.0
  IL_25d3:  or
  IL_25d4:  pop
  IL_25d9:  dup
  IL_25da:  ldc.i4.3
  IL_25db:  add
  IL_25dc:  pop
  IL_25e1:  dup
  IL_25e2:  ldc.i4.0
  IL_25e3:  shr
  IL_25e4:  pop
  IL_25e9:  dup
  IL_25ea:  ldc.i4.1
  IL_25eb:  sub
  IL_25ec:  pop
  IL_25f1:  dup
  IL_25f2:  ldc.i4.4
  IL_25f3:  shl
  IL_25f4:  pop
  IL_25f9:  dup
  IL_25fa:  ldc.i4.7
  IL_25fb:  or
  IL_25fc:  pop
  IL_2601:  dup
  IL_2602:  ldc.i4.3
  IL_2603:  shl
  IL_2604:  pop
  IL_2609:  dup
  IL_260a:  ldc.i4.3
  IL_260b:  sub
  IL_260c:  pop
  IL_2611:  dup
  IL_2612:  ldc.i4.8
  IL_2613:  shr
  IL_2614:  pop
  IL_2619:  dup
  IL_261a:  ldc.i4.1
  IL_261b:  xor
  IL_261c:  pop
  IL_2621:  dup
  IL_2622:  ldc.i4.6
  IL_2623:  or
  IL_2624:  pop
  IL_2629:  dup
  IL_262a:  ldc.i4.3
  IL_262b:  add
  IL_262c:  pop
  IL_2631:  dup
  IL_2632:  ldc.i4.1
  IL_2633:  or
  IL_2634:  pop
  IL_2639:  dup
  IL_263a:  ldc.i4.5
  IL_263b:  shl
  IL_263c:  pop
  IL_2641:  dup
  IL_2642:  ldc.i4.4
  IL_2643:  sub
  IL_2644:  pop
  IL_2649:  dup
  IL_264a:  ldc.i4.7
  IL_264b:  shr
  IL_264c:  pop
  IL_2651:  dup
  IL_2652:  ldc.i4.5
  IL_2653:  shl
  IL_2654:  pop
  IL_2659:  dup
  IL_265a:  ldc.i4.2
  IL_265b:  shr
  IL_265c:  pop
  IL_2661:  dup
  IL_2662:  ldc.i4.4
  IL_2663:  mul
  IL_2664:  pop
  IL_2669:  dup
  IL_266a:  ldc.i4.4
  IL_266b:  shr
  IL_266c:  pop
  IL_2671:  dup
  IL_2672:  ldc.i4.1
  IL_2673:  shr
  IL_2674:  pop
  IL_2679:  dup
  IL_267a:  ldc.i4.7
  IL_267b:  or
  IL_267c:  pop
  IL_2681:  dup
  IL_2682:  ldc.i4.3
  IL_2683:  sub
  IL_2684:  pop
  IL_2689:  dup
  IL_268a:  ldc.i4.3
  IL_268b:  add
  IL_268c:  pop
  IL_2691:  dup
  IL_2692:  ldc.i4.2
  IL_2693:  and
  IL_2694:  pop
  IL_2699:  dup
  IL_269a:  ldc.i4.4
  IL_269b:  add
  IL_269c:  pop
  IL_26a1:  dup
  IL_26a2:  ldc.i4.2
  IL_26a3:  shr
  IL_26a4:  pop
  IL_26a9:  dup
  IL_26aa:  ldc.i4.2
  IL_26ab:  sub
  IL_26ac:  pop
  IL_26b1:  dup
  IL_26b2:  ldc.i4.5
  IL_26b3:  mul
  IL_26b4:  pop
  IL_26b9:  dup
  IL_26ba:  ldc.i4.8
  IL_26bb:  sub
  IL_26bc:  pop
  IL_26c1:  dup
  IL_26c2:  ldc.i4.0
  IL_26c3:  shl
  IL_26c4:  pop
  IL_26c9:  dup
  IL_26ca:  ldc.i4.6
  IL_26cb:  mul
  IL_26cc:  pop
  IL_26d1:  dup
  IL_26d2:  ldc.i4.5
  IL_26d3:  add
  IL_26d4:  pop
  IL_26d9:  dup
  IL_26da:  ldc.i4.2
  IL_26db:  or
  IL_26dc:  pop
  IL_26e1:  dup
  IL_26e2:  ldc.i4.3
  IL_26e3:  shr
  IL_26e4:  pop
  IL_26e9:  dup
  IL_26ea:  ldc.i4.6
  IL_26eb:  shl
  IL_26ec:  pop
  IL_26f1:  dup
  IL_26f2:  ldc.i4.1
  IL_26f3:  xor
  IL_26f4:  pop
  IL_26f9:  dup
  IL_26fa:  ldc.i4.2
  IL_26fb:  add
  IL_26fc:  pop
  IL_2701:  dup
  IL_2702:  ldc.i4.0
  IL_2703:  xor
  IL_2704:  pop
  IL_2709:  dup
  IL_270a:  ldc.i4.6
  IL_270b:  or
  IL_270c:  pop
  IL_2711:  dup
  IL_2712:  ldc.i4.4
  IL_2713:  shl
  IL_2714:  pop
  IL_2719:  dup
  IL_271a:  ldc.i4.1
  IL_271b:  shr
  IL_271c:  pop
  IL_2721:  dup
  IL_2722:  ldc.i4.7
  IL_2723:  and
  IL_2724:  pop
  IL_2729:  dup
  IL_272a:  ldc.i4.3
  IL_272b:  or
  IL_272c:  pop
  IL_2731:  dup
  IL_2732:  ldc.i4.2
  IL_2733:  add
  IL_2734:  pop
  IL_2739:  dup
  IL_273a:  ldc.i4.1
  IL_273b:  sub
  IL_273c:  pop
  IL_2741:  dup
  IL_2742:  ldc.i4.3
  IL_2743:  mul
  IL_2744:  pop
  IL_2749:  dup
  IL_274a:  ldc.i4.6
  IL_274b:  shl
  IL_274c:  pop
  IL_2751:  dup
  IL_2752:  ldc.i4.0
  IL_2753:  shl
  IL_2754:  pop
  IL_2759:  dup
  IL_275a:  ldc.i4.2
  IL_275b:  mul
  IL_275c:  pop
  IL_2761:  dup
  IL_2762:  ldc.i4.8
  IL_2763:  shl
  IL_2764:  pop
  IL_2769:  dup
  IL_276a:  ldc.i4.6
  IL_276b:  shr
  IL_276c:  pop
  IL_2771:  dup
  IL_2772:  ldc.i4.1
  IL_2773:  or
  IL_2774:  pop
  IL_2779:  dup
  IL_277a:  ldc.i4.8
  IL_277b:  or
  IL_277c:  pop
  IL_2781:  dup
  IL_2782:  ldc.i4.3
  IL_2783:  sub
  IL_2784:  pop
  IL_2789:  dup
  IL_278a:  ldc.i4.0
  IL_278b:  add
  IL_278c:  pop
  IL_2791:  dup
  IL_2792:  ldc.i4.5
  IL_2793:  or
  IL_2794:  pop
  IL_2799:  dup
  IL_279a:  ldc.i4.6
  IL_279b:  xor
  IL_279c:  pop
  IL_27a1:  dup
  IL_27a2:  ldc.i4.3
  IL_27a3:  or
  IL_27a4:  pop
  IL_27a9:  dup
  IL_27aa:  ldc.i4.4
  IL_27ab:  and
  IL_27ac:  pop
  IL_27b1:  dup
  IL_27b2:  ldc.i4.5
  IL_27b3:  mul
  IL_27b4:  pop
  IL_27b9:  dup
  IL_27ba:  ldc.i4.5
  IL_27bb:  sub
  IL_27bc:  pop
  IL_27c1:  dup
  IL_27c2:  ldc.i4.4
  IL_27c3:  shl
  IL_27c4:  pop
  IL_27c9:  dup
  IL_27ca:  ldc.i4.0
  IL_27cb:  and
  IL_27cc:  pop
  IL_27d1:  dup
  IL_27d2:  ldc.i4.6
  IL_27d3:  and
  IL_27d4:  pop
  IL_27d9:  dup
  IL_27da:  ldc.i4.8
  IL_27db:  shl
  IL_27dc:  pop
  IL_27e1:  dup
  IL_27e2:  ldc.i4.1
  IL_27e3:  mul
  IL_27e4:  pop
  IL_27e9:  dup
  IL_27ea:  ldc.i4.0
  IL_27eb:  or
  IL_27ec:  pop
  IL_27f1:  dup
  IL_27f2:  ldc.i4.8
  IL_27f3:  shl
  IL_27f4:  pop
  IL_27f9:  dup
  IL_27fa:  ldc.i4.5
  IL_27fb:  and
  IL_27fc:  pop
  IL_2801:  dup
  IL_2802:  ldc.i4.8
  IL_2803:  shl
  IL_2804:  pop
  IL_2809:  dup
  IL_280a:  ldc.i4.4
  IL_280b:  add
  IL_280c:  pop
  IL_2811:  dup
  IL_2812:  ldc.i4.0
  IL_2813:  add
  IL_2814:  pop
  IL_2819:  dup
  IL_281a:  ldc.i4.5
  IL_281b:  xor
  IL_281c:  pop
  IL_2821:  dup
  IL_2822:  ldc.i4.4
  IL_2823:  sub
  IL_2824:  pop
  IL_2829:  dup
  IL_282a:  ldc.i4.1
  IL_282b:  add
  IL_282c:  pop
  IL_2831:  dup
  IL_2832:  ldc.i4.8
  IL_2833:  xor
  IL_2834:  pop
  IL_2839:  dup
  IL_283a:  ldc.i4.6
  IL_283b:  shl
  IL_283c:  pop
  IL_2841:  dup
  IL_2842:  ldc.i4.8
  IL_2843:  xor
  IL_2844:  pop
  IL_2849:  dup
  IL_284a:  ldc.i4.1
  IL_284b:  shl
  IL_284c:  pop
  IL_2851:  dup
  IL_2852:  ldc.i4.7
  IL_2853:  add
  IL_2854:  pop
  IL_2859:  dup
  IL_285a:  ldc.i4.1
  IL_285b:  shl
  IL_285c:  pop
  IL_2861:  dup
  IL_2862:  ldc.i4.2
  IL_2863:  xor
  IL_2864:  pop
  IL_2869:  dup
  IL_286a:  ldc.i4.7
  IL_286b:  mul
  IL_286c:  pop
  IL_2871:  dup
  IL_2872:  ldc.i4.8
  IL_2873:  and
  IL_2874:  pop
  IL_2879:  dup
  IL_287a:  ldc.i4.4
  IL_287b:  and
  IL_287c:  pop
  IL_2881:  dup
  IL_2882:  ldc.i4.3
  IL_2883:  shr
  IL_2884:  pop
  IL_2889:  dup
  IL_288a:  ldc.i4.7
  IL_288b:  sub
  IL_288c:  pop
  IL_2891:  dup
  IL_2892:  ldc.i4.7
  IL_2893:  xor
  IL_2894:  pop
  IL_2899:  dup
  IL_289a:  ldc.i4.1
  IL_289b:  xor
  IL_289c:  pop
  IL_28a1:  dup
  IL_28a2:  ldc.i4.8
  IL_28a3:  shr
  IL_28a4:  pop
  IL_28a9:  dup
  IL_28aa:  ldc.i4.2
  IL_28ab:  shl
  IL_28ac:  pop
  IL_28b1:  dup
  IL_28b2:  ldc.i4.5
  IL_28b3:  or
  IL_28b4:  pop
  IL_28b9:  dup
  IL_28ba:  ldc.i4.5
  IL_28bb:  shr
  IL_28bc:  pop
  IL_28c1:  dup
  IL_28c2:  ldc.i4.5
  IL_28c3:  xor
  IL_28c4:  pop
  IL_28c9:  dup
  IL_28ca:  ldc.i4.8
  IL_28cb:  xor
  IL_28cc:  pop
  IL_28d1:  dup
  IL_28d2:  ldc.i4.4
  IL_28d3:  or
  IL_28d4:  pop
  IL_28d9:  dup
  IL_28da:  ldc.i4.0
  IL_28db:  or
  IL_28dc:  pop
  IL_28e1:  dup
  IL_28e2:  ldc.i4.3
  IL_28e3:  or
  IL_28e4:  pop
  IL_28e9:  dup
  IL_28ea:  ldc.i4.7
  IL_28eb:  shl
  IL_28ec:  pop
  IL_28f1:  dup
  IL_28f2:  ldc.i4.2
  IL_28f3:  mul
  IL_28f4:  pop
  IL_28f9:  dup
  IL_28fa:  ldc.i4.4
  IL_28fb:  shr
  IL_28fc:  pop
  IL_2901:  dup
  IL_2902:  ldc.i4.6
  IL_2903:  mul
  IL_2904:  pop
  IL_2909:  dup
  IL_290a:  ldc.i4.6
  IL_290b:  mul
  IL_290c:  pop
  IL_2911:  dup
  IL_2912:  ldc.i4.6
  IL_2913:  shl
  IL_2914:  pop
  IL_2919:  dup
  IL_291a:  ldc.i4.6
  IL_291b:  xor
  IL_291c:  pop
  IL_2921:  dup
  IL_2922:  ldc.i4.2
  IL_2923:  shr
  IL_2924:  pop
  IL_2929:  dup
  IL_292a:  ldc.i4.5
  IL_292b:  sub
  IL_292c:  pop
  IL_2931:  dup
  IL_2932:  ldc.i4.7
  IL_2933:  shl
  IL_2934:  pop
  IL_2939:  dup
  IL_293a:  ldc.i4.0
  IL_293b:  mul
  IL_293c:  pop
  IL_2941:  dup
  IL_2942:  ldc.i4.7
  IL_2943:  sub
  IL_2944:  pop
  IL_2949:  dup
  IL_294a:  ldc.i4.6
  IL_294b:  xor
  IL_294c:  pop
  IL_2951:  dup
  IL_2952:  ldc.i4.8
  IL_2953:  add
  IL_2954:  pop
  IL_2959:  dup
  IL_295a:  ldc.i4.3
  IL_295b:  mul
  IL_295c:  pop
  IL_2961:  dup
  IL_2962:  ldc.i4.5
  IL_2963:  or
  IL_2964:  pop
  IL_2969:  dup
  IL_296a:  ldc.i4.6
  IL_296b:  or
  IL_296c:  pop
  IL_2971:  dup
  IL_2972:  ldc.i4.5
  IL_2973:  shr
  IL_2974:  pop
  IL_2979:  dup
  IL_297a:  ldc.i4.1
  IL_297b:  shr
  IL_297c:  pop
  IL_2981:  dup
  IL_2982:  ldc.i4.1
  IL_2983:  shl
  IL_2984:  pop
  IL_2989:  dup
  IL_298a:  ldc.i4.7
  IL_298b:  mul
  IL_298c:  pop
  IL_2991:  dup
  IL_2992:  ldc.i4.1
  IL_2993:  mul
  IL_2994:  pop
  IL_2999:  dup
  IL_299a:  ldc.i4.0
  IL_299b:  or
  IL_299c:  pop
  IL_29a1:  dup
  IL_29a2:  ldc.i4.6
  IL_29a3:  xor
  IL_29a4:  pop
  IL_29a9:  dup
  IL_29aa:  ldc.i4.2
  IL_29ab:  shr
  IL_29ac:  pop
  IL_29b1:  dup
  IL_29b2:  ldc.i4.2
  IL_29b3:  or
  IL_29b4:  pop
  IL_29b9:  dup
  IL_29ba:  ldc.i4.0
  IL_29bb:  and
  IL_29bc:  pop
  IL_29c1:  dup
  IL_29c2:  ldc.i4.8
  IL_29c3:  shl
  IL_29c4:  pop
  IL_29c9:  dup
  IL_29ca:  ldc.i4.6
  IL_29cb:  add
  IL_29cc:  pop
  IL_29d1:  dup
  IL_29d2:  ldc.i4.4
  IL_29d3:  add
  IL_29d4:  pop
  IL_29d9:  dup
  IL_29da:  ldc.i4.6
  IL_29db:  shl
  IL_29dc:  pop
  IL_29e1:  dup
  IL_29e2:  ldc.i4.8
  IL_29e3:  mul
  IL_29e4:  pop
  IL_29e9:  dup
  IL_29ea:  ldc.i4.6
  IL_29eb:  shr
  IL_29ec:  pop
  IL_29f1:  dup
  IL_29f2:  ldc.i4.7
  IL_29f3:  and
  IL_29f4:  pop
  IL_29f9:  dup
  IL_29fa:  ldc.i4.7
  IL_29fb:  shl
  IL_29fc:  pop
  IL_2a01:  dup
  IL_2a02:  ldc.i4.4
  IL_2a03:  or
  IL_2a04:  pop
  IL_2a09:  dup
  IL_2a0a:  ldc.i4.0
  IL_2a0b:  sub
  IL_2a0c:  pop
  IL_2a11:  dup
  IL_2a12:  ldc.i4.6
  IL_2a13:  shr
  IL_2a14:  pop
  IL_2a19:  dup
  IL_2a1a:  ldc.i4.1
  IL_2a1b:  shl
  IL_2a1c:  pop
  IL_2a21:  dup
  IL_2a22:  ldc.i4.0
  IL_2a23:  and
  IL_2a24:  pop
  IL_2a29:  dup
  IL_2a2a:  ldc.i4.6
  IL_2a2b:  xor
  IL_2a2c:  pop
  IL_2a31:  dup
  IL_2a32:  ldc.i4.7
  IL_2a33:  shr
  IL_2a34:  pop
  IL_2a39:  dup
  IL_2a3a:  ldc.i4.0
  IL_2a3b:  shr
  IL_2a3c:  pop
  IL_2a41:  dup
  IL_2a42:  ldc.i4.7
  IL_2a43:  or
  IL_2a44:  pop
  IL_2a49:  dup
  IL_2a4a:  ldc.i4.8
  IL_2a4b:  add
  IL_2a4c:  pop
  IL_2a51:  dup
  IL_2a52:  ldc.i4.8
  IL_2a53:  add
  IL_2a54:  pop
  IL_2a59:  dup
  IL_2a5a:  ldc.i4.1
  IL_2a5b:  add
  IL_2a5c:  pop
  IL_2a61:  dup
  IL_2a62:  ldc.i4.7
  IL_2a63:  mul
  IL_2a64:  pop
  IL_2a69:  dup
  IL_2a6a:  ldc.i4.3
  IL_2a6b:  and
  IL_2a6c:  pop
  IL_2a71:  dup
  IL_2a72:  ldc.i4.5
  IL_2a73:  mul
  IL_2a74:  pop
  IL_2a79:  dup
  IL_2a7a:  ldc.i4.1
  IL_2a7b:  or
  IL_2a7c:  pop
  IL_2a81:  dup
  IL_2a82:  ldc.i4.5
  IL_2a83:  shl
  IL_2a84:  pop
  IL_2a89:  dup
  IL_2a8a:  ldc.i4.1
  IL_2a8b:  mul
  IL_2a8c:  pop
  IL_2a91:  dup
  IL_2a92:  ldc.i4.7
  IL_2a93:  or
  IL_2a94:  pop
  IL_2a99:  dup
  IL_2a9a:  ldc.i4.0
  IL_2a9b:  sub
  IL_2a9c:  pop
  IL_2aa1:  dup
  IL_2aa2:  ldc.i4.3
  IL_2aa3:  sub
  IL_2aa4:  pop
  IL_2aa9:  dup
  IL_2aaa:  ldc.i4.0
  IL_2aab:  and
  IL_2aac:  pop
  IL_2ab1:  dup
  IL_2ab2:  ldc.i4.3
  IL_2ab3:  sub
  IL_2ab4:  pop
  IL_2ab9:  dup
  IL_2aba:  ldc.i4.6
  IL_2abb:  mul
  IL_2abc:  pop
  IL_2ac1:  dup
  IL_2ac2:  ldc.i4.3
  IL_2ac3:  or
  IL_2ac4:  pop
  IL_2ac9:  dup
  IL_2aca:  ldc.i4.8
  IL_2acb:  or
  IL_2acc:  pop
  IL_2ad1:  dup
  IL_2ad2:  ldc.i4.7
  IL_2ad3:  and
  IL_2ad4:  pop
  IL_2ad9:  dup
  IL_2ada:  ldc.i4.6
  IL_2adb:  xor
  IL_2adc:  pop
  IL_2ae1:  dup
  IL_2ae2:  ldc.i4.7
  IL_2ae3:  add
  IL_2ae4:  pop
  IL_2ae9:  dup
  IL_2aea:  ldc.i4.2
  IL_2aeb:  xor
  IL_2aec:  pop
  IL_2af1:  dup
  IL_2af2:  ldc.i4.3
  IL_2af3:  mul
  IL_2af4:  pop
  IL_2af9:  dup
  IL_2afa:  ldc.i4.8
  IL_2afb:  and
  IL_2afc:  pop
  IL_2b01:  dup
  IL_2b02:  ldc.i4.5
  IL_2b03:  shr
  IL_2b04:  pop
  IL_2b09:  dup
  IL_2b0a:  ldc.i4.7
  IL_2b0b:  mul
  IL_2b0c:  pop
  IL_2b11:  dup
  IL_2b12:  ldc.i4.3
  IL_2b13:  shr
  IL_2b14:  pop
  IL_2b19:  dup
  IL_2b1a:  ldc.i4.7
  IL_2b1b:  or
  IL_2b1c:  pop
  IL_2b21:  dup
  IL_2b22:  ldc.i4.7
  IL_2b23:  and
  IL_2b24:  pop
  IL_2b29:  dup
  IL_2b2a:  ldc.i4.6
  IL_2b2b:  shr
  IL_2b2c:  pop
  IL_2b31:  dup
  IL_2b32:  ldc.i4.0
  IL_2b33:  xor
  IL_2b34:  pop
  IL_2b39:  dup
  IL_2b3a:  ldc.i4.5
  IL_2b3b:  shr
  IL_2b3c:  pop
  IL_2b41:  dup
  IL_2b42:  ldc.i4.5
  IL_2b43:  add
  IL_2b44:  pop
  IL_2b49:  dup
  IL_2b4a:  ldc.i4.4
  IL_2b4b:  shl
  IL_2b4c:  pop
  IL_2b51:  dup
  IL_2b52:  ldc.i4.3
  IL_2b53:  sub
  IL_2b54:  pop
  IL_2b59:  dup
  IL_2b5a:  ldc.i4.1
  IL_2b5b:  and
  IL_2b5c:  pop
  IL_2b61:  dup
  IL_2b62:  ldc.i4.8
  IL_2b63:  shr
  IL_2b64:  pop
  IL_2b69:  dup
  IL_2b6a:  ldc.i4.4
  IL_2b6b:  and
  IL_2b6c:  pop
  IL_2b71:  dup
  IL_2b72:  ldc.i4.5
  IL_2b73:  shl
  IL_2b74:  pop
  IL_2b79:  dup
  IL_2b7a:  ldc.i4.7
  IL_2b7b:  sub
  IL_2b7c:  pop
  IL_2b81:  dup
  IL_2b82:  ldc.i4.2
  IL_2b83:  mul
  IL_2b84:  pop
  IL_2b89:  dup
  IL_2b8a:  ldc.i4.1
  IL_2b8b:  shr
  IL_2b8c:  pop
  IL_2b91:  dup
  IL_2b92:  ldc.i4.3
  IL_2b93:  xor
  IL_2b94:  pop
  IL_2b99:  dup
  IL_2b9a:  ldc.i4.7
  IL_2b9b:  shr
  IL_2b9c:  pop
  IL_2ba1:  dup
  IL_2ba2:  ldc.i4.1
  IL_2ba3:  or
  IL_2ba4:  pop
  IL_2ba9:  dup
  IL_2baa:  ldc.i4.0
  IL_2bab:  add
  IL_2bac:  pop
  IL_2bb1:  dup
  IL_2bb2:  ldc.i4.0
  IL_2bb3:  add
  IL_2bb4:  pop
  IL_2bb9:  dup
  IL_2bba:  ldc.i4.4
  IL_2bbb:  xor
  IL_2bbc:  pop
  IL_2bc1:  dup
  IL_2bc2:  ldc.i4.3
  IL_2bc3:  shr
  IL_2bc4:  pop
  IL_2bc9:  dup
  IL_2bca:  ldc.i4.7
  IL_2bcb:  mul
  IL_2bcc:  pop
  IL_2bd1:  dup
  IL_2bd2:  ldc.i4.7
  IL_2bd3:  mul
  IL_2bd4:  pop
  IL_2bd9:  dup
  IL_2bda:  ldc.i4.3
  IL_2bdb:  xor
  IL_2bdc:  pop
  IL_2be1:  dup
  IL_2be2:  ldc.i4.5
  IL_2be3:  add
  IL_2be4:  pop
  IL_2be9:  dup
  IL_2bea:  ldc.i4.3
  IL_2beb:  shr
  IL_2bec:  pop
  IL_2bf1:  dup
  IL_2bf2:  ldc.i4.1
  IL_2bf3:  add
  IL_2bf4:  pop
  IL_2bf9:  dup
  IL_2bfa:  ldc.i4.3
  IL_2bfb:  sub
  IL_2bfc:  pop
  IL_2c01:  dup
  IL_2c02:  ldc.i4.8
  IL_2c03:  shl
  IL_2c04:  pop
  IL_2c09:  dup
  IL_2c0a:  ldc.i4.4
  IL_2c0b:  shr
  IL_2c0c:  pop
  IL_2c11:  dup
  IL_2c12:  ldc.i4.2
  IL_2c13:  or
  IL_2c14:  pop
  IL_2c19:  dup
  IL_2c1a:  ldc.i4.2
  IL_2c1b:  add
  IL_2c1c:  pop
  IL_2c21:  dup
  IL_2c22:  ldc.i4.1
  IL_2c23:  and
  IL_2c24:  pop
  IL_2c29:  dup
  IL_2c2a:  ldc.i4.4
  IL_2c2b:  xor
  IL_2c2c:  pop
  IL_2c31:  dup
  IL_2c32:  ldc.i4.1
  IL_2c33:  add
  IL_2c34:  pop
  IL_2c39:  dup
  IL_2c3a:  ldc.i4.6
  IL_2c3b:  or
  IL_2c3c:  pop
  IL_2c41:  dup
  IL_2c42:  ldc.i4.1
  IL_2c43:  add
  IL_2c44:  pop
  IL_2c49:  dup
  IL_2c4a:  ldc.i4.3
  IL_2c4b:  and
  IL_2c4c:  pop
  IL_2c51:  dup
  IL_2c52:  ldc.i4.6
  IL_2c53:  shl
  IL_2c54:  pop
  IL_2c59:  dup
  IL_2c5a:  ldc.i4.2
  IL_2c5b:  or
  IL_2c5c:  pop
  IL_2c61:  dup
  IL_2c62:  ldc.i4.7
  IL_2c63:  shr
  IL_2c64:  pop
  IL_2c69:  dup
  IL_2c6a:  ldc.i4.0
  IL_2c6b:  mul
  IL_2c6c:  pop
  IL_2c71:  dup
  IL_2c72:  ldc.i4.0
  IL_2c73:  sub
  IL_2c74:  pop
  IL_2c79:  dup
  IL_2c7a:  ldc.i4.4
  IL_2c7b:  add
  IL_2c7c:  pop
  IL_2c81:  dup
  IL_2c82:  ldc.i4.4
  IL_2c83:  and
  IL_2c84:  pop
  IL_2c89:  dup
  IL_2c8a:  ldc.i4.1
  IL_2c8b:  shl
  IL_2c8c:  pop
  IL_2c91:  dup
  IL_2c92:  ldc.i4.4
  IL_2c93:  and
  IL_2c94:  pop
  IL_2c99:  dup
  IL_2c9a:  ldc.i4.1
  IL_2c9b:  shl
  IL_2c9c:  pop
  IL_2ca1:  dup
  IL_2ca2:  ldc.i4.3
  IL_2ca3:  and
  IL_2ca4:  pop
  IL_2ca9:  dup
  IL_2caa:  ldc.i4.0
  IL_2cab:  add
  IL_2cac:  pop
  IL_2cb1:  dup
  IL_2cb2:  ldc.i4.7
  IL_2cb3:  or
  IL_2cb4:  pop
  IL_2cb9:  dup
  IL_2cba:  ldc.i4.3
  IL_2cbb:  shl
  IL_2cbc:  pop
  IL_2cc1:  dup
  IL_2cc2:  ldc.i4.2
  IL_2cc3:  xor
  IL_2cc4:  pop
  IL_2cc9:  dup
  IL_2cca:  ldc.i4.4
  IL_2ccb:  add
  IL_2ccc:  pop
  IL_2cd1:  dup
  IL_2cd2:  ldc.i4.5
  IL_2cd3:  shl
  IL_2cd4:  pop
  IL_2cd9:  dup
  IL_2cda:  ldc.i4.4
  IL_2cdb:  mul
  IL_2cdc:  pop
  IL_2ce1:  dup
  IL_2ce2:  ldc.i4.2
  IL_2ce3:  shl
  IL_2ce4:  pop
  IL_2ce9:  dup
  IL_2cea:  ldc.i4.7
  IL_2ceb:  shr
  IL_2cec:  pop
  IL_2cf1:  dup
  IL_2cf2:  ldc.i4.7
  IL_2cf3:  or
  IL_2cf4:  pop
  IL_2cf9:  dup
  IL_2cfa:  ldc.i4.0
  IL_2cfb:  add
  IL_2cfc:  pop
  IL_2d01:  dup
  IL_2d02:  ldc.i4.2
  IL_2d03:  xor
  IL_2d04:  pop
  IL_2d09:  dup
  IL_2d0a:  ldc.i4.8
  IL_2d0b:  mul
  IL_2d0c:  pop
  IL_2d11:  dup
  IL_2d12:  ldc.i4.8
  IL_2d13:  sub
  IL_2d14:  pop
  IL_2d19:  dup
  IL_2d1a:  ldc.i4.3
  IL_2d1b:  xor
  IL_2d1c:  pop
  IL_2d21:  dup
  IL_2d22:  ldc.i4.8
  IL_2d23:  or
  IL_2d24:  pop
  IL_2d29:  dup
  IL_2d2a:  ldc.i4.3
  IL_2d2b:  or
  IL_2d2c:  pop
  IL_2d31:  dup
  IL_2d32:  ldc.i4.4
  IL_2d33:  mul
  IL_2d34:  pop
  IL_2d39:  dup
  IL_2d3a:  ldc.i4.0
  IL_2d3b:  or
  IL_2d3c:  pop
  IL_2d41:  dup
  IL_2d42:  ldc.i4.2
  IL_2d43:  shl
  IL_2d44:  pop
  IL_2d49:  dup
  IL_2d4a:  ldc.i4.0
  IL_2d4b:  shr
  IL_2d4c:  pop
  IL_2d51:  dup
  IL_2d52:  ldc.i4.5
  IL_2d53:  xor
  IL_2d54:  pop
  IL_2d59:  dup
  IL_2d5a:  ldc.i4.4
  IL_2d5b:  add
  IL_2d5c:  pop
  IL_2d61:  dup
  IL_2d62:  ldc.i4.2
  IL_2d63:  and
  IL_2d64:  pop
  IL_2d69:  dup
  IL_2d6a:  ldc.i4.2
  IL_2d6b:  or
  IL_2d6c:  pop
  IL_2d71:  dup
  IL_2d72:  ldc.i4.8
  IL_2d73:  shl
  IL_2d74:  pop
  IL_2d79:  dup
  IL_2d7a:  ldc.i4.6
  IL_2d7b:  shl
  IL_2d7c:  pop
  IL_2d81:  dup
  IL_2d82:  ldc.i4.6
  IL_2d83:  add
  IL_2d84:  pop
  IL_2d89:  dup
  IL_2d8a:  ldc.i4.6
  IL_2d8b:  xor
  IL_2d8c:  pop
  IL_2d91:  dup
  IL_2d92:  ldc.i4.8
  IL_2d93:  add
  IL_2d94:  pop
  IL_2d99:  dup
  IL_2d9a:  ldc.i4.5
  IL_2d9b:  add
  IL_2d9c:  pop
  IL_2da1:  dup
  IL_2da2:  ldc.i4.0
  IL_2da3:  add
  IL_2da4:  pop
  IL_2da9:  dup
  IL_2daa:  ldc.i4.0
  IL_2dab:  shr
  IL_2dac:  pop
  IL_2db1:  dup
  IL_2db2:  ldc.i4.0
  IL_2db3:  shl
  IL_2db4:  pop
  IL_2db9:  dup
  IL_2dba:  ldc.i4.7
  IL_2dbb:  or
  IL_2dbc:  pop
  IL_2dc1:  dup
  IL_2dc2:  ldc.i4.5
  IL_2dc3:  and
  IL_2dc4:  pop
  IL_2dc9:  dup
  IL_2dca:  ldc.i4.2
  IL_2dcb:  mul
  IL_2dcc:  pop
  IL_2dd1:  dup
  IL_2dd2:  ldc.i4.8
  IL_2dd3:  xor
  IL_2dd4:  pop
  IL_2dd9:  dup
  IL_2dda:  ldc.i4.4
  IL_2ddb:  and
  IL_2ddc:  pop
  IL_2de1:  dup
  IL_2de2:  ldc.i4.8
  IL_2de3:  or
  IL_2de4:  pop
  IL_2de9:  dup
  IL_2dea:  ldc.i4.0
  IL_2deb:  sub
  IL_2dec:  pop
  IL_2df1:  dup
  IL_2df2:  ldc.i4.7
  IL_2df3:  xor
  IL_2df4:  pop
  IL_2df9:  dup
  IL_2dfa:  ldc.i4.4
  IL_2dfb:  xor
  IL_2dfc:  pop
  IL_2e01:  dup
  IL_2e02:  ldc.i4.1
  IL_2e03:  or
  IL_2e04:  pop
  IL_2e09:  dup
  IL_2e0a:  ldc.i4.2
  IL_2e0b:  xor
  IL_2e0c:  pop
  IL_2e11:  dup
  IL_2e12:  ldc.i4.5
  IL_2e13:  shl
  IL_2e14:  pop
  IL_2e19:  dup
  IL_2e1a:  ldc.i4.0
  IL_2e1b:  mul
  IL_2e1c:  pop
  IL_2e21:  dup
  IL_2e22:  ldc.i4.8
  IL_2e23:  or
  IL_2e24:  pop
  IL_2e29:  dup
  IL_2e2a:  ldc.i4.4
  IL_2e2b:  shl
  IL_2e2c:  pop
  IL_2e31:  dup
  IL_2e32:  ldc.i4.3
  IL_2e33:  shr
  IL_2e34:  pop
  IL_2e39:  dup
  IL_2e3a:  ldc.i4.8
  IL_2e3b:  and
  IL_2e3c:  pop
  IL_2e41:  dup
  IL_2e42:  ldc.i4.8
  IL_2e43:  xor
  IL_2e44:  pop
  IL_2e49:  dup
  IL_2e4a:  ldc.i4.3
  IL_2e4b:  add
  IL_2e4c:  pop
  IL_2e51:  dup
  IL_2e52:  ldc.i4.1
  IL_2e53:  xor
  IL_2e54:  pop
  IL_2e59:  dup
  IL_2e5a:  ldc.i4.5
  IL_2e5b:  mul
  IL_2e5c:  pop
  IL_2e61:  dup
  IL_2e62:  ldc.i4.5
  IL_2e63:  or
  IL_2e64:  pop
  IL_2e69:  dup
  IL_2e6a:  ldc.i4.4
  IL_2e6b:  add
  IL_2e6c:  pop
  IL_2e71:  dup
  IL_2e72:  ldc.i4.0
  IL_2e73:  xor
  IL_2e74:  pop
  IL_2e79:  dup
  IL_2e7a:  ldc.i4.1
  IL_2e7b:  shr
  IL_2e7c:  pop
  IL_2e81:  dup
  IL_2e82:  ldc.i4.4
  IL_2e83:  shl
  IL_2e84:  pop
  IL_2e89:  dup
  IL_2e8a:  ldc.i4.4
  IL_2e8b:  sub
  IL_2e8c:  pop
  IL_2e91:  dup
  IL_2e92:  ldc.i4.2
  IL_2e93:  xor
  IL_2e94:  pop
  IL_2e99:  dup
  IL_2e9a:  ldc.i4.2
  IL_2e9b:  add
  IL_2e9c:  pop
  IL_2ea1:  dup
  IL_2ea2:  ldc.i4.7
  IL_2ea3:  add
  IL_2ea4:  pop
  IL_2ea9:  dup
  IL_2eaa:  ldc.i4.2
  IL_2eab:  or
  IL_2eac:  pop
  IL_2eb1:  dup
  IL_2eb2:  ldc.i4.7
  IL_2eb3:  or
  IL_2eb4:  pop
  IL_2eb9:  dup
  IL_2eba:  ldc.i4.3
  IL_2ebb:  sub
  IL_2ebc:  pop
  IL_2ec1:  dup
  IL_2ec2:  ldc.i4.8
  IL_2ec3:  sub
  IL_2ec4:  pop
  IL_2ec9:  dup
  IL_2eca:  ldc.i4.1
  IL_2ecb:  xor
  IL_2ecc:  pop
  IL_2ed1:  dup
  IL_2ed2:  ldc.i4.0
  IL_2ed3:  shl
  IL_2ed4:  pop
  IL_2ed9:  dup
  IL_2eda:  ldc.i4.2
  IL_2edb:  shr
  IL_2edc:  pop
  IL_2ee1:  dup
  IL_2ee2:  ldc.i4.3
  IL_2ee3:  and
  IL_2ee4:  pop
  IL_2ee9:  dup
  IL_2eea:  ldc.i4.7
  IL_2eeb:  shr
  IL_2eec:  pop
  IL_2ef1:  dup
  IL_2ef2:  ldc.i4.0
  IL_2ef3:  or
  IL_2ef4:  pop
  IL_2ef9:  dup
  IL_2efa:  ldc.i4.2
  IL_2efb:  xor
  IL_2efc:  pop
  IL_2f01:  dup
  IL_2f02:  ldc.i4.6
  IL_2f03:  and
  IL_2f04:  pop
  IL_2f09:  dup
  IL_2f0a:  ldc.i4.8
  IL_2f0b:  and
  IL_2f0c:  pop
  IL_2f11:  dup
  IL_2f12:  ldc.i4.1
  IL_2f13:  and
  IL_2f14:  pop
  IL_2f19:  dup
  IL_2f1a:  ldc.i4.4
  IL_2f1b:  shl
  IL_2f1c:  pop
  IL_2f21:  dup
  IL_2f22:  ldc.i4.0
  IL_2f23:  mul
  IL_2f24:  pop
  IL_2f29:  dup
  IL_2f2a:  ldc.i4.1
  IL_2f2b:  sub
  IL_2f2c:  pop
  IL_2f31:  dup
  IL_2f32:  ldc.i4.1
  IL_2f33:  shr
  IL_2f34:  pop
  IL_2f39:  dup
  IL_2f3a:  ldc.i4.4
  IL_2f3b:  shr
  IL_2f3c:  pop
  IL_2f41:  dup
  IL_2f42:  ldc.i4.4
  IL_2f43:  sub
  IL_2f44:  pop
  IL_2f49:  dup
  IL_2f4a:  ldc.i4.1
  IL_2f4b:  and
  IL_2f4c:  pop
  IL_2f51:  dup
  IL_2f52:  ldc.i4.2
  IL_2f53:  shr
  IL_2f54:  pop
  IL_2f59:  dup
  IL_2f5a:  ldc.i4.5
  IL_2f5b:  add
  IL_2f5c:  pop
  IL_2f61:  dup
  IL_2f62:  ldc.i4.3
  IL_2f63:  and
  IL_2f64:  pop
  IL_2f69:  dup
  IL_2f6a:  ldc.i4.7
  IL_2f6b:  xor
  IL_2f6c:  pop
  IL_2f71:  dup
  IL_2f72:  ldc.i4.5
  IL_2f73:  shr
  IL_2f74:  pop
  IL_2f79:  dup
  IL_2f7a:  ldc.i4.8
  IL_2f7b:  or
  IL_2f7c:  pop
  IL_2f81:  dup
  IL_2f82:  ldc.i4.6
  IL_2f83:  and
  IL_2f84:  pop
  IL_2f89:  dup
  IL_2f8a:  ldc.i4.2
  IL_2f8b:  add
  IL_2f8c:  pop
  IL_2f91:  dup
  IL_2f92:  ldc.i4.8
  IL_2f93:  shl
  IL_2f94:  pop
  IL_2f99:  dup
  IL_2f9a:  ldc.i4.0
  IL_2f9b:  add
  IL_2f9c:  pop
  IL_2fa1:  dup
  IL_2fa2:  ldc.i4.6
  IL_2fa3:  shr
  IL_2fa4:  pop
  IL_2fa9:  dup
  IL_2faa:  ldc.i4.3
  IL_2fab:  shr
  IL_2fac:  pop
  IL_2fb1:  dup
  IL_2fb2:  ldc.i4.2
  IL_2fb3:  add
  IL_2fb4:  pop
  IL_2fb9:  dup
  IL_2fba:  ldc.i4.5
  IL_2fbb:  shr
  IL_2fbc:  pop
  IL_2fc1:  dup
  IL_2fc2:  ldc.i4.3
  IL_2fc3:  sub
  IL_2fc4:  pop
  IL_2fc9:  dup
  IL_2fca:  ldc.i4.0
  IL_2fcb:  and
  IL_2fcc:  pop
  IL_2fd1:  dup
  IL_2fd2:  ldc.i4.7
  IL_2fd3:  shl
  IL_2fd4:  pop
  IL_2fd9:  dup
  IL_2fda:  ldc.i4.1
  IL_2fdb:  or
  IL_2fdc:  pop
  IL_2fe1:  dup
  IL_2fe2:  ldc.i4.2
  IL_2fe3:  or
  IL_2fe4:  pop
  IL_2fe9:  dup
  IL_2fea:  ldc.i4.3
  IL_2feb:  mul
  IL_2fec:  pop
  IL_2ff1:  dup
  IL_2ff2:  ldc.i4.0
  IL_2ff3:  add
  IL_2ff4:  pop
  IL_2ff9:  dup
  IL_2ffa:  ldc.i4.8
  IL_2ffb:  xor
  IL_2ffc:  pop
  IL_3001:  dup
  IL_3002:  ldc.i4.3
  IL_3003:  mul
  IL_3004:  pop
  IL_3009:  dup
  IL_300a:  ldc.i4.3
  IL_300b:  mul
  IL_300c:  pop
  IL_3011:  dup
  IL_3012:  ldc.i4.3
  IL_3013:  shl
  IL_3014:  pop
  IL_3019:  dup
  IL_301a:  ldc.i4.6
  IL_301b:  or
  IL_301c:  pop
  IL_3021:  dup
  IL_3022:  ldc.i4.1
  IL_3023:  add
  IL_3024:  pop
  IL_3029:  dup
  IL_302a:  ldc.i4.8
  IL_302b:  shl
  IL_302c:  pop
  IL_3031:  dup
  IL_3032:  ldc.i4.2
  IL_3033:  and
  IL_3034:  pop
  IL_3039:  dup
  IL_303a:  ldc.i4.6
  IL_303b:  mul
  IL_303c:  pop
  IL_3041:  dup
  IL_3042:  ldc.i4.1
  IL_3043:  add
  IL_3044:  pop
  IL_3049:  dup
  IL_304a:  ldc.i4.0
  IL_304b:  xor
  IL_304c:  pop
  IL_3051:  dup
  IL_3052:  ldc.i4.5
  IL_3053:  add
  IL_3054:  pop
  IL_3059:  dup
  IL_305a:  ldc.i4.1
  IL_305b:  sub
  IL_305c:  pop
  IL_3061:  dup
  IL_3062:  ldc.i4.6
  IL_3063:  mul
  IL_3064:  pop
  IL_3069:  dup
  IL_306a:  ldc.i4.6
  IL_306b:  or
  IL_306c:  pop
  IL_3071:  dup
  IL_3072:  ldc.i4.1
  IL_3073:  shr
  IL_3074:  pop
  IL_3079:  dup
  IL_307a:  ldc.i4.1
  IL_307b:  and
  IL_307c:  pop
  IL_3081:  dup
  IL_3082:  ldc.i4.6
  IL_3083:  add
  IL_3084:  pop
  IL_3089:  dup
  IL_308a:  ldc.i4.2
  IL_308b:  add
  IL_308c:  pop
  IL_3091:  dup
  IL_3092:  ldc.i4.5
  IL_3093:  mul
  IL_3094:  pop
  IL_3099:  dup
  IL_309a:  ldc.i4.2
  IL_309b:  and
  IL_309c:  pop
  IL_30a1:  dup
  IL_30a2:  ldc.i4.3
  IL_30a3:  sub
  IL_30a4:  pop
  IL_30a9:  dup
  IL_30aa:  ldc.i4.6
  IL_30ab:  and
  IL_30ac:  pop
  IL_30b1:  dup
  IL_30b2:  ldc.i4.4
  IL_30b3:  shl
  IL_30b4:  pop
  IL_30b9:  dup
  IL_30ba:  ldc.i4.5
  IL_30bb:  xor
  IL_30bc:  pop
  IL_30c1:  dup
  IL_30c2:  ldc.i4.1
  IL_30c3:  sub
  IL_30c4:  pop
  IL_30c9:  dup
  IL_30ca:  ldc.i4.1
  IL_30cb:  or
  IL_30cc:  pop
  IL_30d1:  dup
  IL_30d2:  ldc.i4.8
  IL_30d3:  shl
  IL_30d4:  pop
  IL_30d9:  dup
  IL_30da:  ldc.i4.7
  IL_30db:  mul
  IL_30dc:  pop
  IL_30e1:  dup
  IL_30e2:  ldc.i4.1
  IL_30e3:  sub
  IL_30e4:  pop
  IL_30e9:  dup
  IL_30ea:  ldc.i4.4
  IL_30eb:  sub
  IL_30ec:  pop
  IL_30f1:  dup
  IL_30f2:  ldc.i4.6
  IL_30f3:  shr
  IL_30f4:  pop
  IL_30f9:  dup
  IL_30fa:  ldc.i4.7
  IL_30fb:  xor
  IL_30fc:  pop
  IL_3101:  dup
  IL_3102:  ldc.i4.8
  IL_3103:  sub
  IL_3104:  pop
  IL_3109:  dup
  IL_310a:  ldc.i4.7
  IL_310b:  mul
  IL_310c:  pop
  IL_3111:  dup
  IL_3112:  ldc.i4.5
  IL_3113:  shr
  IL_3114:  pop
  IL_3119:  dup
  IL_311a:  ldc.i4.5
  IL_311b:  and
  IL_311c:  pop
  IL_3121:  dup
  IL_3122:  ldc.i4.7
  IL_3123:  sub
  IL_3124:  pop
  IL_3129:  dup
  IL_312a:  ldc.i4.8
  IL_312b:  xor
  IL_312c:  pop
  IL_3131:  dup
  IL_3132:  ldc.i4.3
  IL_3133:  mul
  IL_3134:  pop
  IL_3139:  dup
  IL_313a:  ldc.i4.6
  IL_313b:  sub
  IL_313c:  pop
  IL_3141:  dup
  IL_3142:  ldc.i4.5
  IL_3143:  xor
  IL_3144:  pop
  IL_3149:  dup
  IL_314a:  ldc.i4.6
  IL_314b:  add
  IL_314c:  pop
  IL_3151:  dup
  IL_3152:  ldc.i4.5
  IL_3153:  sub
  IL_3154:  pop
  IL_3159:  dup
  IL_315a:  ldc.i4.7
  IL_315b:  sub
  IL_315c:  pop
  IL_3161:  dup
  IL_3162:  ldc.i4.1
  IL_3163:  or
  IL_3164:  pop
  IL_3169:  dup
  IL_316a:  ldc.i4.8
  IL_316b:  xor
  IL_316c:  pop
  IL_3171:  dup
  IL_3172:  ldc.i4.7
  IL_3173:  sub
  IL_3174:  pop
  IL_3179:  dup
  IL_317a:  ldc.i4.0
  IL_317b:  shr
  IL_317c:  pop
  IL_3181:  dup
  IL_3182:  ldc.i4.2
  IL_3183:  sub
  IL_3184:  pop
  IL_3189:  dup
  IL_318a:  ldc.i4.7
  IL_318b:  mul
  IL_318c:  pop
  IL_3191:  dup
  IL_3192:  ldc.i4.6
  IL_3193:  mul
  IL_3194:  pop
  IL_3199:  dup
  IL_319a:  ldc.i4.7
  IL_319b:  shl
  IL_319c:  pop
  IL_31a1:  dup
  IL_31a2:  ldc.i4.4
  IL_31a3:  mul
  IL_31a4:  pop
  IL_31a9:  dup
  IL_31aa:  ldc.i4.0
  IL_31ab:  shr
  IL_31ac:  pop
  IL_31b1:  dup
  IL_31b2:  ldc.i4.5
  IL_31b3:  xor
  IL_31b4:  pop
  IL_31b9:  dup
  IL_31ba:  ldc.i4.8
  IL_31bb:  and
  IL_31bc:  pop
  IL_31c1:  dup
  IL_31c2:  ldc.i4.4
  IL_31c3:  xor
  IL_31c4:  pop
  IL_31c9:  dup
  IL_31ca:  ldc.i4.5
  IL_31cb:  mul
  IL_31cc:  pop
  IL_31d1:  dup
  IL_31d2:  ldc.i4.8
  IL_31d3:  and
  IL_31d4:  pop
  IL_31d9:  dup
  IL_31da:  ldc.i4.7
  IL_31db:  shr
  IL_31dc:  pop
  IL_31e1:  dup
  IL_31e2:  ldc.i4.3
  IL_31e3:  shr
  IL_31e4:  pop
  IL_31e9:  dup
  IL_31ea:  ldc.i4.0
  IL_31eb:  shr
  IL_31ec:  pop
  IL_31f1:  dup
  IL_31f2:  ldc.i4.6
  IL_31f3:  sub
  IL_31f4:  pop
  IL_31f9:  dup
  IL_31fa:  ldc.i4.0
  IL_31fb:  shr
  IL_31fc:  pop
  IL_3201:  dup
  IL_3202:  ldc.i4.1
  IL_3203:  mul
  IL_3204:  pop
  IL_3209:  dup
  IL_320a:  ldc.i4.1
  IL_320b:  sub
  IL_320c:  pop
  IL_3211:  dup
  IL_3212:  ldc.i4.4
  IL_3213:  xor
  IL_3214:  pop
  IL_3219:  dup
  IL_321a:  ldc.i4.4
  IL_321b:  sub
  IL_321c:  pop
  IL_3221:  dup
  IL_3222:  ldc.i4.5
  IL_3223:  sub
  IL_3224:  pop
  IL_3229:  dup
  IL_322a:  ldc.i4.4
  IL_322b:  mul
  IL_322c:  pop
  IL_3231:  dup
  IL_3232:  ldc.i4.1
  IL_3233:  sub
  IL_3234:  pop
  IL_3239:  dup
  IL_323a:  ldc.i4.5
  IL_323b:  and
  IL_323c:  pop
  IL_3241:  dup
  IL_3242:  ldc.i4.0
  IL_3243:  sub
  IL_3244:  pop
  IL_3249:  dup
  IL_324a:  ldc.i4.7
  IL_324b:  xor
  IL_324c:  pop
  IL_3251:  dup
  IL_3252:  ldc.i4.5
  IL_3253:  shr
  IL_3254:  pop
  IL_3259:  dup
  IL_325a:  ldc.i4.6
  IL_325b:  shl
  IL_325c:  pop
  IL_3261:  dup
  IL_3262:  ldc.i4.3
  IL_3263:  sub
  IL_3264:  pop
  IL_3269:  dup
  IL_326a:  ldc.i4.7
  IL_326b:  add
  IL_326c:  pop
  IL_3271:  dup
  IL_3272:  ldc.i4.2
  IL_3273:  sub
  IL_3274:  pop
  IL_3279:  dup
  IL_327a:  ldc.i4.7
  IL_327b:  sub
  IL_327c:  pop
  IL_3281:  dup
  IL_3282:  ldc.i4.2
  IL_3283:  xor
  IL_3284:  pop
  IL_3289:  dup
  IL_328a:  ldc.i4.4
  IL_328b:  mul
  IL_328c:  pop
  IL_3291:  dup
  IL_3292:  ldc.i4.2
  IL_3293:  shr
  IL_3294:  pop
  IL_3299:  dup
  IL_329a:  ldc.i4.2
  IL_329b:  sub
  IL_329c:  pop
  IL_32a1:  dup
  IL_32a2:  ldc.i4.8
  IL_32a3:  mul
  IL_32a4:  pop
  IL_32a9:  dup
  IL_32aa:  ldc.i4.7
  IL_32ab:  and
  IL_32ac:  pop
  IL_32b1:  dup
  IL_32b2:  ldc.i4.2
  IL_32b3:  shl
  IL_32b4:  pop
  IL_32b9:  dup
  IL_32ba:  ldc.i4.4
  IL_32bb:  mul
  IL_32bc:  pop
  IL_32c1:  dup
  IL_32c2:  ldc.i4.0
  IL_32c3:  mul
  IL_32c4:  pop
  IL_32c9:  dup
  IL_32ca:  ldc.i4.5
  IL_32cb:  shl
  IL_32cc:  pop
  IL_32d1:  dup
  IL_32d2:  ldc.i4.3
  IL_32d3:  shl
  IL_32d4:  pop
  IL_32d9:  dup
  IL_32da:  ldc.i4.0
  IL_32db:  xor
  IL_32dc:  pop
  IL_32e1:  dup
  IL_32e2:  ldc.i4.5
  IL_32e3:  shl
  IL_32e4:  pop
  IL_32e9:  dup
  IL_32ea:  ldc.i4.7
  IL_32eb:  mul
  IL_32ec:  pop
  IL_32f1:  dup
  IL_32f2:  ldc.i4.4
  IL_32f3:  xor
  IL_32f4:  pop
  IL_32f9:  dup
  IL_32fa:  ldc.i4.8
  IL_32fb:  xor
  IL_32fc:  pop
  IL_3301:  dup
  IL_3302:  ldc.i4.3
  IL_3303:  mul
  IL_3304:  pop
  IL_3309:  dup
  IL_330a:  ldc.i4.8
  IL_330b:  and
  IL_330c:  pop
  IL_3311:  dup
  IL_3312:  ldc.i4.5
  IL_3313:  mul
  IL_3314:  pop
  IL_3319:  dup
  IL_331a:  ldc.i4.4
  IL_331b:  mul
  IL_331c:  pop
  IL_3321:  dup
  IL_3322:  ldc.i4.8
  IL_3323:  shr
  IL_3324:  pop
  IL_3329:  dup
  IL_332a:  ldc.i4.4
  IL_332b:  mul
  IL_332c:  pop
  IL_3331:  dup
  IL_3332:  ldc.i4.8
  IL_3333:  or
  IL_3334:  pop
  IL_3339:  dup
  IL_333a:  ldc.i4.2
  IL_333b:  and
  IL_333c:  pop
  IL_3341:  dup
  IL_3342:  ldc.i4.5
  IL_3343:  shl
  IL_3344:  pop
  IL_3349:  dup
  IL_334a:  ldc.i4.8
  IL_334b:  or
  IL_334c:  pop
  IL_3351:  dup
  IL_3352:  ldc.i4.4
  IL_3353:  sub
  IL_3354:  pop
  IL_3359:  dup
  IL_335a:  ldc.i4.8
  IL_335b:  shr
  IL_335c:  pop
  IL_3361:  dup
  IL_3362:  ldc.i4.2
  IL_3363:  add
  IL_3364:  pop
  IL_3369:  dup
  IL_336a:  ldc.i4.6
  IL_336b:  shr
  IL_336c:  pop
  IL_3371:  dup
  IL_3372:  ldc.i4.8
  IL_3373:  or
  IL_3374:  pop
  IL_3379:  dup
  IL_337a:  ldc.i4.5
  IL_337b:  xor
  IL_337c:  pop
  IL_3381:  dup
  IL_3382:  ldc.i4.3
  IL_3383:  xor
  IL_3384:  pop
  IL_3389:  dup
  IL_338a:  ldc.i4.3
  IL_338b:  or
  IL_338c:  pop
  IL_3391:  dup
  IL_3392:  ldc.i4.5
  IL_3393:  and
  IL_3394:  pop
  IL_3399:  dup
  IL_339a:  ldc.i4.6
  IL_339b:  shr
  IL_339c:  pop
  IL_33a1:  dup
  IL_33a2:  ldc.i4.6
  IL_33a3:  mul
  IL_33a4:  pop
  IL_33a9:  dup
  IL_33aa:  ldc.i4.6
  IL_33ab:  shl
  IL_33ac:  pop
  IL_33b1:  dup
  IL_33b2:  ldc.i4.1
  IL_33b3:  and
  IL_33b4:  pop
  IL_33b9:  dup
  IL_33ba:  ldc.i4.4
  IL_33bb:  shl
  IL_33bc:  pop
  IL_33c1:  dup
  IL_33c2:  ldc.i4.3
  IL_33c3:  add
  IL_33c4:  pop
  IL_33c9:  dup
  IL_33ca:  ldc.i4.6
  IL_33cb:  or
  IL_33cc:  pop
  IL_33d1:  dup
  IL_33d2:  ldc.i4.4
  IL_33d3:  add
  IL_33d4:  pop
  IL_33d9:  dup
  IL_33da:  ldc.i4.7
  IL_33db:  sub
  IL_33dc:  pop
  IL_33e1:  dup
  IL_33e2:  ldc.i4.3
  IL_33e3:  mul
  IL_33e4:  pop
  IL_33e9:  dup
  IL_33ea:  ldc.i4.4
  IL_33eb:  xor
  IL_33ec:  pop
  IL_33f1:  dup
  IL_33f2:  ldc.i4.5
  IL_33f3:  or
  IL_33f4:  pop
  IL_33f9:  dup
  IL_33fa:  ldc.i4.3
  IL_33fb:  add
  IL_33fc:  pop
  IL_3401:  dup
  IL_3402:  ldc.i4.0
  IL_3403:  add
  IL_3404:  pop
  IL_3409:  dup
  IL_340a:  ldc.i4.8
  IL_340b:  shl
  IL_340c:  pop
  IL_3411:  dup
  IL_3412:  ldc.i4.5
  IL_3413:  mul
  IL_3414:  pop
  IL_3419:  dup
  IL_341a:  ldc.i4.1
  IL_341b:  and
  IL_341c:  pop
  IL_3421:  dup
  IL_3422:  ldc.i4.8
  IL_3423:  mul
  IL_3424:  pop
  IL_3429:  dup
  IL_342a:  ldc.i4.1
  IL_342b:  sub
  IL_342c:  pop
  IL_3431:  dup
  IL_3432:  ldc.i4.4
  IL_3433:  mul
  IL_3434:  pop
  IL_3439:  dup
  IL_343a:  ldc.i4.4
  IL_343b:  xor
  IL_343c:  pop
  IL_3441:  dup
  IL_3442:  ldc.i4.5
  IL_3443:  add
  IL_3444:  pop
  IL_3449:  dup
  IL_344a:  ldc.i4.2
  IL_344b:  and
  IL_344c:  pop
  IL_3451:  dup
  IL_3452:  ldc.i4.1
  IL_3453:  shr
  IL_3454:  pop
  IL_3459:  dup
  IL_345a:  ldc.i4.8
  IL_345b:  mul
  IL_345c:  pop
  IL_3461:  dup
  IL_3462:  ldc.i4.4
  IL_3463:  sub
  IL_3464:  pop
  IL_3469:  dup
  IL_346a:  ldc.i4.4
  IL_346b:  mul
  IL_346c:  pop
  IL_3471:  dup
  IL_3472:  ldc.i4.3
  IL_3473:  sub
  IL_3474:  pop
  IL_3479:  dup
  IL_347a:  ldc.i4.8
  IL_347b:  or
  IL_347c:  pop
  IL_3481:  dup
  IL_3482:  ldc.i4.0
  IL_3483:  shr
  IL_3484:  pop
  IL_3489:  dup
  IL_348a:  ldc.i4.4
  IL_348b:  add
  IL_348c:  pop
  IL_3491:  dup
  IL_3492:  ldc.i4.8
  IL_3493:  or
  IL_3494:  pop
  IL_3499:  dup
  IL_349a:  ldc.i4.8
  IL_349b:  add
  IL_349c:  pop
  IL_34a1:  dup
  IL_34a2:  ldc.i4.8
  IL_34a3:  or
  IL_34a4:  pop
  IL_34a9:  dup
  IL_34aa:  ldc.i4.0
  IL_34ab:  sub
  IL_34ac:  pop
  IL_34b1:  dup
  IL_34b2:  ldc.i4.7
  IL_34b3:  shr
  IL_34b4:  pop
  IL_34b9:  dup
  IL_34ba:  ldc.i4.6
  IL_34bb:  sub
  IL_34bc:  pop
  IL_34c1:  dup
  IL_34c2:  ldc.i4.6
  IL_34c3:  add
  IL_34c4:  pop
  IL_34c9:  dup
  IL_34ca:  ldc.i4.8
  IL_34cb:  shl
  IL_34cc:  pop
  IL_34d1:  dup
  IL_34d2:  ldc.i4.5
  IL_34d3:  xor
  IL_34d4:  pop
  IL_34d9:  dup
  IL_34da:  ldc.i4.7
  IL_34db:  xor
  IL_34dc:  pop
  IL_34e1:  dup
  IL_34e2:  ldc.i4.0
  IL_34e3:  shl
  IL_34e4:  pop
  IL_34e9:  dup
  IL_34ea:  ldc.i4.8
  IL_34eb:  shl
  IL_34ec:  pop
  IL_34f1:  dup
  IL_34f2:  ldc.i4.7
  IL_34f3:  and
  IL_34f4:  pop
  IL_34f9:  dup
  IL_34fa:  ldc.i4.4
  IL_34fb:  shr
  IL_34fc:  pop
  IL_3501:  dup
  IL_3502:  ldc.i4.2
  IL_3503:  mul
  IL_3504:  pop
  IL_3509:  dup
  IL_350a:  ldc.i4.1
  IL_350b:  and
  IL_350c:  pop
  IL_3511:  dup
  IL_3512:  ldc.i4.6
  IL_3513:  xor
  IL_3514:  pop
  IL_3519:  dup
  IL_351a:  ldc.i4.4
  IL_351b:  shl
  IL_351c:  pop
  IL_3521:  dup
  IL_3522:  ldc.i4.1
  IL_3523:  mul
  IL_3524:  pop
  IL_3529:  dup
  IL_352a:  ldc.i4.8
  IL_352b:  sub
  IL_352c:  pop
  IL_3531:  dup
  IL_3532:  ldc.i4.6
  IL_3533:  and
  IL_3534:  pop
  IL_3539:  dup
  IL_353a:  ldc.i4.4
  IL_353b:  or
  IL_353c:  pop
  IL_3541:  dup
  IL_3542:  ldc.i4.4
  IL_3543:  xor
  IL_3544:  pop
  IL_3549:  dup
  IL_354a:  ldc.i4.4
  IL_354b:  mul
  IL_354c:  pop
  IL_3551:  dup
  IL_3552:  ldc.i4.2
  IL_3553:  xor
  IL_3554:  pop
  IL_3559:  dup
  IL_355a:  ldc.i4.2
  IL_355b:  shl
  IL_355c:  pop
  IL_3561:  dup
  IL_3562:  ldc.i4.7
  IL_3563:  sub
  IL_3564:  pop
  IL_3569:  dup
  IL_356a:  ldc.i4.0
  IL_356b:  xor
  IL_356c:  pop
  IL_3571:  dup
  IL_3572:  ldc.i4.0
  IL_3573:  xor
  IL_3574:  pop
  IL_3579:  dup
  IL_357a:  ldc.i4.1
  IL_357b:  add
  IL_357c:  pop
  IL_3581:  dup
  IL_3582:  ldc.i4.4
  IL_3583:  mul
  IL_3584:  pop
  IL_3589:  dup
  IL_358a:  ldc.i4.7
  IL_358b:  shl
  IL_358c:  pop
  IL_3591:  dup
  IL_3592:  ldc.i4.4
  IL_3593:  and
  IL_3594:  pop
  IL_3599:  dup
  IL_359a:  ldc.i4.3
  IL_359b:  mul
  IL_359c:  pop
  IL_35a1:  dup
  IL_35a2:  ldc.i4.4
  IL_35a3:  mul
  IL_35a4:  pop
  IL_35a9:  dup
  IL_35aa:  ldc.i4.3
  IL_35ab:  mul
  IL_35ac:  pop
  IL_35b1:  dup
  IL_35b2:  ldc.i4.5
  IL_35b3:  shr
  IL_35b4:  pop
  IL_35b9:  dup
  IL_35ba:  ldc.i4.6
  IL_35bb:  sub
  IL_35bc:  pop
  IL_35c1:  dup
  IL_35c2:  ldc.i4.0
  IL_35c3:  add
  IL_35c4:  pop
  IL_35c9:  dup
  IL_35ca:  ldc.i4.0
  IL_35cb:  xor
  IL_35cc:  pop
  IL_35d1:  dup
  IL_35d2:  ldc.i4.2
  IL_35d3:  add
  IL_35d4:  pop
  IL_35d9:  dup
  IL_35da:  ldc.i4.3
  IL_35db:  or
  IL_35dc:  pop
  IL_35e1:  dup
  IL_35e2:  ldc.i4.8
  IL_35e3:  shr
  IL_35e4:  pop
  IL_35e9:  dup
  IL_35ea:  ldc.i4.0
  IL_35eb:  shr
  IL_35ec:  pop
  IL_35f1:  dup
  IL_35f2:  ldc.i4.3
  IL_35f3:  add
  IL_35f4:  pop
  IL_35f9:  dup
  IL_35fa:  ldc.i4.5
  IL_35fb:  shr
  IL_35fc:  pop
  IL_3601:  dup
  IL_3602:  ldc.i4.0
  IL_3603:  and
  IL_3604:  pop
  IL_3609:  dup
  IL_360a:  ldc.i4.6
  IL_360b:  mul
  IL_360c:  pop
  IL_3611:  dup
  IL_3612:  ldc.i4.0
  IL_3613:  or
  IL_3614:  pop
  IL_3619:  dup
  IL_361a:  ldc.i4.3
  IL_361b:  shl
  IL_361c:  pop
  IL_3621:  dup
  IL_3622:  ldc.i4.0
  IL_3623:  sub
  IL_3624:  pop
  IL_3629:  dup
  IL_362a:  ldc.i4.7
  IL_362b:  add
  IL_362c:  pop
  IL_3631:  dup
  IL_3632:  ldc.i4.0
  IL_3633:  add
  IL_3634:  pop
  IL_3639:  dup
  IL_363a:  ldc.i4.8
  IL_363b:  sub
  IL_363c:  pop
  IL_3641:  dup
  IL_3642:  ldc.i4.3
  IL_3643:  or
  IL_3644:  pop
  IL_3649:  dup
  IL_364a:  ldc.i4.2
  IL_364b:  or
  IL_364c:  pop
  IL_3651:  dup
  IL_3652:  ldc.i4.3
  IL_3653:  and
  IL_3654:  pop
  IL_3659:  dup
  IL_365a:  ldc.i4.7
  IL_365b:  mul
  IL_365c:  pop
  IL_3661:  dup
  IL_3662:  ldc.i4.8
  IL_3663:  or
  IL_3664:  pop
  IL_3669:  dup
  IL_366a:  ldc.i4.5
  IL_366b:  and
  IL_366c:  pop
  IL_3671:  dup
  IL_3672:  ldc.i4.7
  IL_3673:  shr
  IL_3674:  pop
  IL_3679:  dup
  IL_367a:  ldc.i4.8
  IL_367b:  xor
  IL_367c:  pop
  IL_3681:  dup
  IL_3682:  ldc.i4.6
  IL_3683:  and
  IL_3684:  pop
  IL_3689:  dup
  IL_368a:  ldc.i4.1
  IL_368b:  and
  IL_368c:  pop
  IL_3691:  dup
  IL_3692:  ldc.i4.8
  IL_3693:  mul
  IL_3694:  pop
  IL_3699:  dup
  IL_369a:  ldc.i4.1
  IL_369b:  sub
  IL_369c:  pop
  IL_36a1:  dup
  IL_36a2:  ldc.i4.6
  IL_36a3:  xor
  IL_36a4:  pop
  IL_36a9:  dup
  IL_36aa:  ldc.i4.3
  IL_36ab:  add
  IL_36ac:  pop
  IL_36b1:  dup
  IL_36b2:  ldc.i4.2
  IL_36b3:  xor
  IL_36b4:  pop
  IL_36b9:  dup
  IL_36ba:  ldc.i4.8
  IL_36bb:  or
  IL_36bc:  pop
  IL_36c1:  dup
  IL_36c2:  ldc.i4.6
  IL_36c3:  and
  IL_36c4:  pop
  IL_36c9:  dup
  IL_36ca:  ldc.i4.1
  IL_36cb:  xor
  IL_36cc:  pop
  IL_36d1:  dup
  IL_36d2:  ldc.i4.7
  IL_36d3:  shr
  IL_36d4:  pop
  IL_36d9:  dup
  IL_36da:  ldc.i4.2
  IL_36db:  mul
  IL_36dc:  pop
  IL_36e1:  dup
  IL_36e2:  ldc.i4.8
  IL_36e3:  mul
  IL_36e4:  pop
  IL_36e9:  dup
  IL_36ea:  ldc.i4.4
  IL_36eb:  add
  IL_36ec:  pop
  IL_36f1:  dup
  IL_36f2:  ldc.i4.2
  IL_36f3:  shl
  IL_36f4:  pop
  IL_36f9:  dup
  IL_36fa:  ldc.i4.7
  IL_36fb:  mul
  IL_36fc:  pop
  IL_3701:  dup
  IL_3702:  ldc.i4.2
  IL_3703:  xor
  IL_3704:  pop
  IL_3709:  dup
  IL_370a:  ldc.i4.2
  IL_370b:  sub
  IL_370c:  pop
  IL_3711:  dup
  IL_3712:  ldc.i4.7
  IL_3713:  sub
  IL_3714:  pop
  IL_3719:  dup
  IL_371a:  ldc.i4.1
  IL_371b:  add
  IL_371c:  pop
  IL_3721:  dup
  IL_3722:  ldc.i4.6
  IL_3723:  add
  IL_3724:  pop
  IL_3729:  dup
  IL_372a:  ldc.i4.2
  IL_372b:  or
  IL_372c:  pop
  IL_3731:  dup
  IL_3732:  ldc.i4.3
  IL_3733:  and
  IL_3734:  pop
  IL_3739:  dup
  IL_373a:  ldc.i4.0
  IL_373b:  shl
  IL_373c:  pop
  IL_3741:  dup
  IL_3742:  ldc.i4.0
  IL_3743:  shr
  IL_3744:  pop
  IL_3749:  dup
  IL_374a:  ldc.i4.1
  IL_374b:  or
  IL_374c:  pop
  IL_3751:  dup
  IL_3752:  ldc.i4.5
  IL_3753:  shl
  IL_3754:  pop
  IL_3759:  dup
  IL_375a:  ldc.i4.5
  IL_375b:  add
  IL_375c:  pop
  IL_3761:  dup
  IL_3762:  ldc.i4.4
  IL_3763:  or
  IL_3764:  pop
  IL_3769:  dup
  IL_376a:  ldc.i4.7
  IL_376b:  shl
  IL_376c:  pop
  IL_3771:  dup
  IL_3772:  ldc.i4.7
  IL_3773:  add
  IL_3774:  pop
  IL_3779:  dup
  IL_377a:  ldc.i4.8
  IL_377b:  sub
  IL_377c:  pop
  IL_3781:  dup
  IL_3782:  ldc.i4.5
  IL_3783:  sub
  IL_3784:  pop
  IL_3789:  dup
  IL_378a:  ldc.i4.6
  IL_378b:  or
  IL_378c:  pop
  IL_3791:  dup
  IL_3792:  ldc.i4.0
  IL_3793:  shr
  IL_3794:  pop
  IL_3799:  dup
  IL_379a:  ldc.i4.0
  IL_379b:  shl
  IL_379c:  pop
  IL_37a1:  dup
  IL_37a2:  ldc.i4.5
  IL_37a3:  add
  IL_37a4:  pop
  IL_37a9:  dup
  IL_37aa:  ldc.i4.6
  IL_37ab:  shr
  IL_37ac:  pop
  IL_37b1:  dup
  IL_37b2:  ldc.i4.3
  IL_37b3:  sub
  IL_37b4:  pop
  IL_37b9:  dup
  IL_37ba:  ldc.i4.8
  IL_37bb:  sub
  IL_37bc:  pop
  IL_37c1:  dup
  IL_37c2:  ldc.i4.2
  IL_37c3:  or
  IL_37c4:  pop
  IL_37c9:  dup
  IL_37ca:  ldc.i4.0
  IL_37cb:  shl
  IL_37cc:  pop
  IL_37d1:  dup
  IL_37d2:  ldc.i4.3
  IL_37d3:  and
  IL_37d4:  pop
  IL_37d9:  dup
  IL_37da:  ldc.i4.6
  IL_37db:  and
  IL_37dc:  pop
  IL_37e1:  dup
  IL_37e2:  ldc.i4.6
  IL_37e3:  mul
  IL_37e4:  pop
  IL_37e9:  dup
  IL_37ea:  ldc.i4.6
  IL_37eb:  or
  IL_37ec:  pop
  IL_37f1:  dup
  IL_37f2:  ldc.i4.3
  IL_37f3:  shl
  IL_37f4:  pop
  IL_37f9:  dup
  IL_37fa:  ldc.i4.2
  IL_37fb:  shr
  IL_37fc:  pop
  IL_3801:  dup
  IL_3802:  ldc.i4.1
  IL_3803:  add
  IL_3804:  pop
  IL_3809:  dup
  IL_380a:  ldc.i4.3
  IL_380b:  sub
  IL_380c:  pop
  IL_3811:  dup
  IL_3812:  ldc.i4.0
  IL_3813:  sub
  IL_3814:  pop
  IL_3819:  dup
  IL_381a:  ldc.i4.3
  IL_381b:  mul
  IL_381c:  pop
  IL_3821:  dup
  IL_3822:  ldc.i4.6
  IL_3823:  shr
  IL_3824:  pop
  IL_3829:  dup
  IL_382a:  ldc.i4.2
  IL_382b:  xor
  IL_382c:  pop
  IL_3831:  dup
  IL_3832:  ldc.i4.7
  IL_3833:  or
  IL_3834:  pop
  IL_3839:  dup
  IL_383a:  ldc.i4.5
  IL_383b:  mul
  IL_383c:  pop
  IL_3841:  dup
  IL_3842:  ldc.i4.8
  IL_3843:  shr
  IL_3844:  pop
  IL_3849:  dup
  IL_384a:  ldc.i4.3
  IL_384b:  or
  IL_384c:  pop
  IL_3851:  dup
  IL_3852:  ldc.i4.0
  IL_3853:  xor
  IL_3854:  pop
  IL_3859:  dup
  IL_385a:  ldc.i4.6
  IL_385b:  and
  IL_385c:  pop
  IL_3861:  dup
  IL_3862:  ldc.i4.3
  IL_3863:  or
  IL_3864:  pop
  IL_3869:  dup
  IL_386a:  ldc.i4.1
  IL_386b:  and
  IL_386c:  pop
  IL_3871:  dup
  IL_3872:  ldc.i4.8
  IL_3873:  add
  IL_3874:  pop
  IL_3879:  dup
  IL_387a:  ldc.i4.1
  IL_387b:  add
  IL_387c:  pop
  IL_3881:  dup
  IL_3882:  ldc.i4.7
  IL_3883:  add
  IL_3884:  pop
  IL_3889:  dup
  IL_388a:  ldc.i4.4
  IL_388b:  mul
  IL_388c:  pop
  IL_3891:  dup
  IL_3892:  ldc.i4.7
  IL_3893:  shr
  IL_3894:  pop
  IL_3899:  dup
  IL_389a:  ldc.i4.0
  IL_389b:  add
  IL_389c:  pop
  IL_38a1:  dup
  IL_38a2:  ldc.i4.4
  IL_38a3:  shr
  IL_38a4:  pop
  IL_38a9:  dup
  IL_38aa:  ldc.i4.7
  IL_38ab:  and
  IL_38ac:  pop
  IL_38b1:  dup
  IL_38b2:  ldc.i4.5
  IL_38b3:  sub
  IL_38b4:  pop
  IL_38b9:  dup
  IL_38ba:  ldc.i4.8
  IL_38bb:  and
  IL_38bc:  pop
  IL_38c1:  dup
  IL_38c2:  ldc.i4.5
  IL_38c3:  xor
  IL_38c4:  pop
  IL_38c9:  dup
  IL_38ca:  ldc.i4.0
  IL_38cb:  and
  IL_38cc:  pop
  IL_38d1:  dup
  IL_38d2:  ldc.i4.2
  IL_38d3:  mul
  IL_38d4:  pop
  IL_38d9:  dup
  IL_38da:  ldc.i4.5
  IL_38db:  shr
  IL_38dc:  pop
  IL_38e1:  dup
  IL_38e2:  ldc.i4.2
  IL_38e3:  shr
  IL_38e4:  pop
  IL_38e9:  dup
  IL_38ea:  ldc.i4.7
  IL_38eb:  add
  IL_38ec:  pop
  IL_38f1:  dup
  IL_38f2:  ldc.i4.0
  IL_38f3:  shl
  IL_38f4:  pop
  IL_38f9:  dup
  IL_38fa:  ldc.i4.3
  IL_38fb:  shr
  IL_38fc:  pop
  IL_3901:  dup
  IL_3902:  ldc.i4.4
  IL_3903:  sub
  IL_3904:  pop
  IL_3909:  dup
  IL_390a:  ldc.i4.6
  IL_390b:  shr
  IL_390c:  pop
  IL_3911:  dup
  IL_3912:  ldc.i4.3
  IL_3913:  sub
  IL_3914:  pop
  IL_3919:  dup
  IL_391a:  ldc.i4.3
  IL_391b:  mul
  IL_391c:  pop
  IL_3921:  dup
  IL_3922:  ldc.i4.4
  IL_3923:  sub
  IL_3924:  pop
  IL_3929:  dup
  IL_392a:  ldc.i4.3
  IL_392b:  mul
  IL_392c:  pop
  IL_3931:  dup
  IL_3932:  ldc.i4.5
  IL_3933:  or
  IL_3934:  pop
  IL_3939:  dup
  IL_393a:  ldc.i4.1
  IL_393b:  sub
  IL_393c:  pop
  IL_3941:  dup
  IL_3942:  ldc.i4.0
  IL_3943:  mul
  IL_3944:  pop
  IL_3949:  dup
  IL_394a:  ldc.i4.1
  IL_394b:  shl
  IL_394c:  pop
  IL_3951:  dup
  IL_3952:  ldc.i4.0
  IL_3953:  mul
  IL_3954:  pop
  IL_3959:  dup
  IL_395a:  ldc.i4.7
  IL_395b:  shl
  IL_395c:  pop
  IL_3961:  dup
  IL_3962:  ldc.i4.3
  IL_3963:  and
  IL_3964:  pop
  IL_3969:  dup
  IL_396a:  ldc.i4.0
  IL_396b:  sub
  IL_396c:  pop
  IL_3971:  dup
  IL_3972:  ldc.i4.8
  IL_3973:  add
  IL_3974:  pop
  IL_3979:  dup
  IL_397a:  ldc.i4.0
  IL_397b:  or
  IL_397c:  pop
  IL_3981:  dup
  IL_3982:  ldc.i4.2
  IL_3983:  sub
  IL_3984:  pop
  IL_3989:  dup
  IL_398a:  ldc.i4.2
  IL_398b:  mul
  IL_398c:  pop
  IL_3991:  dup
  IL_3992:  ldc.i4.4
  IL_3993:  mul
  IL_3994:  pop
  IL_3999:  dup
  IL_399a:  ldc.i4.3
  IL_399b:  xor
  IL_399c:  pop
  IL_39a1:  dup
  IL_39a2:  ldc.i4.0
  IL_39a3:  and
  IL_39a4:  pop
  IL_39a9:  dup
  IL_39aa:  ldc.i4.1
  IL_39ab:  add
  IL_39ac:  pop
  IL_39b1:  dup
  IL_39b2:  ldc.i4.8
  IL_39b3:  mul
  IL_39b4:  pop
  IL_39b9:  dup
  IL_39ba:  ldc.i4.0
  IL_39bb:  mul
  IL_39bc:  pop
  IL_39c1:  dup
  IL_39c2:  ldc.i4.4
  IL_39c3:  shl
  IL_39c4:  pop
  IL_39c9:  dup
  IL_39ca:  ldc.i4.0
  IL_39cb:  shr
  IL_39cc:  pop
  IL_39d1:  dup
  IL_39d2:  ldc.i4.3
  IL_39d3:  or
  IL_39d4:  pop
  IL_39d9:  dup
  IL_39da:  ldc.i4.5
  IL_39db:  sub
  IL_39dc:  pop
  IL_39e1:  dup
  IL_39e2:  ldc.i4.3
  IL_39e3:  sub
  IL_39e4:  pop
  IL_39e9:  dup
  IL_39ea:  ldc.i4.2
  IL_39eb:  shl
  IL_39ec:  pop
  IL_39f1:  dup
  IL_39f2:  ldc.i4.1
  IL_39f3:  sub
  IL_39f4:  pop
  IL_39f9:  dup
  IL_39fa:  ldc.i4.3
  IL_39fb:  xor
  IL_39fc:  pop
  IL_3a01:  dup
  IL_3a02:  ldc.i4.1
  IL_3a03:  xor
  IL_3a04:  pop
  IL_3a09:  dup
  IL_3a0a:  ldc.i4.0
  IL_3a0b:  xor
  IL_3a0c:  pop
  IL_3a11:  dup
  IL_3a12:  ldc.i4.6
  IL_3a13:  and
  IL_3a14:  pop
  IL_3a19:  dup
  IL_3a1a:  ldc.i4.6
  IL_3a1b:  shr
  IL_3a1c:  pop
  IL_3a21:  dup
  IL_3a22:  ldc.i4.8
  IL_3a23:  and
  IL_3a24:  pop
  IL_3a29:  dup
  IL_3a2a:  ldc.i4.4
  IL_3a2b:  mul
  IL_3a2c:  pop
  IL_3a31:  dup
  IL_3a32:  ldc.i4.1
  IL_3a33:  and
  IL_3a34:  pop
  IL_3a39:  dup
  IL_3a3a:  ldc.i4.5
  IL_3a3b:  add
  IL_3a3c:  pop
  IL_3a41:  dup
  IL_3a42:  ldc.i4.6
  IL_3a43:  shr
  IL_3a44:  pop
  IL_3a49:  dup
  IL_3a4a:  ldc.i4.7
  IL_3a4b:  mul
  IL_3a4c:  pop
  IL_3a51:  dup
  IL_3a52:  ldc.i4.0
  IL_3a53:  xor
  IL_3a54:  pop
  IL_3a59:  dup
  IL_3a5a:  ldc.i4.4
  IL_3a5b:  mul
  IL_3a5c:  pop
  IL_3a61:  dup
  IL_3a62:  ldc.i4.5
  IL_3a63:  add
  IL_3a64:  pop
  IL_3a69:  dup
  IL_3a6a:  ldc.i4.8
  IL_3a6b:  mul
  IL_3a6c:  pop
  IL_3a71:  dup
  IL_3a72:  ldc.i4.0
  IL_3a73:  and
  IL_3a74:  pop
  IL_3a79:  dup
  IL_3a7a:  ldc.i4.1
  IL_3a7b:  and
  IL_3a7c:  pop
  IL_3a81:  dup
  IL_3a82:  ldc.i4.3
  IL_3a83:  shr
  IL_3a84:  pop
  IL_3a89:  dup
  IL_3a8a:  ldc.i4.8
  IL_3a8b:  xor
  IL_3a8c:  pop
  IL_3a91:  dup
  IL_3a92:  ldc.i4.2
  IL_3a93:  shl
  IL_3a94:  pop
  IL_3a99:  dup
  IL_3a9a:  ldc.i4.5
  IL_3a9b:  shl
  IL_3a9c:  pop
  IL_3aa1:  dup
  IL_3aa2:  ldc.i4.7
  IL_3aa3:  mul
  IL_3aa4:  pop
  IL_3aa9:  dup
  IL_3aaa:  ldc.i4.7
  IL_3aab:  shr
  IL_3aac:  pop
  IL_3ab1:  dup
  IL_3ab2:  ldc.i4.3
  IL_3ab3:  mul
  IL_3ab4:  pop
  IL_3ab9:  dup
  IL_3aba:  ldc.i4.0
  IL_3abb:  shr
  IL_3abc:  pop
  IL_3ac1:  dup
  IL_3ac2:  ldc.i4.5
  IL_3ac3:  sub
  IL_3ac4:  pop
  IL_3ac9:  dup
  IL_3aca:  ldc.i4.7
  IL_3acb:  shl
  IL_3acc:  pop
  IL_3ad1:  dup
  IL_3ad2:  ldc.i4.3
  IL_3ad3:  add
  IL_3ad4:  pop
  IL_3ad9:  dup
  IL_3ada:  ldc.i4.4
  IL_3adb:  and
  IL_3adc:  pop
  IL_3ae1:  dup
  IL_3ae2:  ldc.i4.8
  IL_3ae3:  shr
  IL_3ae4:  pop
  IL_3ae9:  dup
  IL_3aea:  ldc.i4.7
  IL_3aeb:  shl
  IL_3aec:  pop
  IL_3af1:  dup
  IL_3af2:  ldc.i4.8
  IL_3af3:  mul
  IL_3af4:  pop
  IL_3af9:  dup
  IL_3afa:  ldc.i4.6
  IL_3afb:  add
  IL_3afc:  pop
  IL_3b01:  dup
  IL_3b02:  ldc.i4.5
  IL_3b03:  shl
  IL_3b04:  pop
  IL_3b09:  dup
  IL_3b0a:  ldc.i4.7
  IL_3b0b:  xor
  IL_3b0c:  pop
  IL_3b11:  dup
  IL_3b12:  ldc.i4.7
  IL_3b13:  sub
  IL_3b14:  pop
  IL_3b19:  dup
  IL_3b1a:  ldc.i4.7
  IL_3b1b:  sub
  IL_3b1c:  pop
  IL_3b21:  dup
  IL_3b22:  ldc.i4.0
  IL_3b23:  sub
  IL_3b24:  pop
  IL_3b29:  dup
  IL_3b2a:  ldc.i4.4
  IL_3b2b:  shr
  IL_3b2c:  pop
  IL_3b31:  dup
  IL_3b32:  ldc.i4.5
  IL_3b33:  or
  IL_3b34:  pop
  IL_3b39:  dup
  IL_3b3a:  ldc.i4.8
  IL_3b3b:  mul
  IL_3b3c:  pop
  IL_3b41:  dup
  IL_3b42:  ldc.i4.0
  IL_3b43:  mul
  IL_3b44:  pop
  IL_3b49:  dup
  IL_3b4a:  ldc.i4.7
  IL_3b4b:  and
  IL_3b4c:  pop
  IL_3b51:  dup
  IL_3b52:  ldc.i4.0
  IL_3b53:  shr
  IL_3b54:  pop
  IL_3b59:  dup
  IL_3b5a:  ldc.i4.4
  IL_3b5b:  mul
  IL_3b5c:  pop
  IL_3b61:  dup
  IL_3b62:  ldc.i4.2
  IL_3b63:  sub
  IL_3b64:  pop
  IL_3b69:  dup
  IL_3b6a:  ldc.i4.4
  IL_3b6b:  shl
  IL_3b6c:  pop
  IL_3b71:  dup
  IL_3b72:  ldc.i4.6
  IL_3b73:  add
  IL_3b74:  pop
  IL_3b79:  dup
  IL_3b7a:  ldc.i4.6
  IL_3b7b:  shl
  IL_3b7c:  pop
  IL_3b81:  dup
  IL_3b82:  ldc.i4.5
  IL_3b83:  shr
  IL_3b84:  pop
  IL_3b89:  dup
  IL_3b8a:  ldc.i4.0
  IL_3b8b:  and
  IL_3b8c:  pop
  IL_3b91:  dup
  IL_3b92:  ldc.i4.4
  IL_3b93:  shl
  IL_3b94:  pop
  IL_3b99:  dup
  IL_3b9a:  ldc.i4.6
  IL_3b9b:  mul
  IL_3b9c:  pop
  IL_3ba1:  dup
  IL_3ba2:  ldc.i4.2
  IL_3ba3:  shr
  IL_3ba4:  pop
  IL_3ba9:  dup
  IL_3baa:  ldc.i4.4
  IL_3bab:  sub
  IL_3bac:  pop
  IL_3bb1:  dup
  IL_3bb2:  ldc.i4.5
  IL_3bb3:  and
  IL_3bb4:  pop
  IL_3bb9:  dup
  IL_3bba:  ldc.i4.1
  IL_3bbb:  add
  IL_3bbc:  pop
  IL_3bc1:  dup
  IL_3bc2:  ldc.i4.6
  IL_3bc3:  shl
  IL_3bc4:  pop
  IL_3bc9:  dup
  IL_3bca:  ldc.i4.4
  IL_3bcb:  shr
  IL_3bcc:  pop
  IL_3bd1:  dup
  IL_3bd2:  ldc.i4.5
  IL_3bd3:  or
  IL_3bd4:  pop
  IL_3bd9:  dup
  IL_3bda:  ldc.i4.0
  IL_3bdb:  or
  IL_3bdc:  pop
  IL_3be1:  dup
  IL_3be2:  ldc.i4.1
  IL_3be3:  shl
  IL_3be4:  pop
  IL_3be9:  dup
  IL_3bea:  ldc.i4.1
  IL_3beb:  shr
  IL_3bec:  pop
  IL_3bf1:  dup
  IL_3bf2:  ldc.i4.6
  IL_3bf3:  sub
  IL_3bf4:  pop
  IL_3bf9:  dup
  IL_3bfa:  ldc.i4.3
  IL_3bfb:  add
  IL_3bfc:  pop
  IL_3c01:  dup
  IL_3c02:  ldc.i4.4
  IL_3c03:  or
  IL_3c04:  pop
  IL_3c09:  dup
  IL_3c0a:  ldc.i4.0
  IL_3c0b:  or
  IL_3c0c:  pop
  IL_3c11:  dup
  IL_3c12:  ldc.i4.7
  IL_3c13:  sub
  IL_3c14:  pop
  IL_3c19:  dup
  IL_3c1a:  ldc.i4.7
  IL_3c1b:  xor
  IL_3c1c:  pop
  IL_3c21:  dup
  IL_3c22:  ldc.i4.4
  IL_3c23:  xor
  IL_3c24:  pop
  IL_3c29:  dup
  IL_3c2a:  ldc.i4.4
  IL_3c2b:  mul
  IL_3c2c:  pop
  IL_3c31:  dup
  IL_3c32:  ldc.i4.1
  IL_3c33:  mul
  IL_3c34:  pop
  IL_3c39:  dup
  IL_3c3a:  ldc.i4.6
  IL_3c3b:  xor
  IL_3c3c:  pop
  IL_3c41:  dup
  IL_3c42:  ldc.i4.8
  IL_3c43:  xor
  IL_3c44:  pop
  IL_3c49:  dup
  IL_3c4a:  ldc.i4.7
  IL_3c4b:  sub
  IL_3c4c:  pop
  IL_3c51:  dup
  IL_3c52:  ldc.i4.4
  IL_3c53:  add
  IL_3c54:  pop
  IL_3c59:  dup
  IL_3c5a:  ldc.i4.8
  IL_3c5b:  shr
  IL_3c5c:  pop
  IL_3c61:  dup
  IL_3c62:  ldc.i4.5
  IL_3c63:  sub
  IL_3c64:  pop
  IL_3c69:  dup
  IL_3c6a:  ldc.i4.6
  IL_3c6b:  or
  IL_3c6c:  pop
  IL_3c71:  dup
  IL_3c72:  ldc.i4.4
  IL_3c73:  add
  IL_3c74:  pop
  IL_3c79:  dup
  IL_3c7a:  ldc.i4.5
  IL_3c7b:  add
  IL_3c7c:  pop
  IL_3c81:  dup
  IL_3c82:  ldc.i4.2
  IL_3c83:  xor
  IL_3c84:  pop
  IL_3c89:  dup
  IL_3c8a:  ldc.i4.6
  IL_3c8b:  shr
  IL_3c8c:  pop
  IL_3c91:  dup
  IL_3c92:  ldc.i4.6
  IL_3c93:  mul
  IL_3c94:  pop
  IL_3c99:  dup
  IL_3c9a:  ldc.i4.1
  IL_3c9b:  shl
  IL_3c9c:  pop
  IL_3ca1:  dup
  IL_3ca2:  ldc.i4.7
  IL_3ca3:  add
  IL_3ca4:  pop
  IL_3ca9:  dup
  IL_3caa:  ldc.i4.1
  IL_3cab:  sub
  IL_3cac:  pop
  IL_3cb1:  dup
  IL_3cb2:  ldc.i4.3
  IL_3cb3:  shr
  IL_3cb4:  pop
  IL_3cb9:  dup
  IL_3cba:  ldc.i4.6
  IL_3cbb:  and
  IL_3cbc:  pop
  IL_3cc1:  dup
  IL_3cc2:  ldc.i4.6
  IL_3cc3:  shr
  IL_3cc4:  pop
  IL_3cc9:  dup
  IL_3cca:  ldc.i4.6
  IL_3ccb:  mul
  IL_3ccc:  pop
  IL_3cd1:  dup
  IL_3cd2:  ldc.i4.3
  IL_3cd3:  sub
  IL_3cd4:  pop
  IL_3cd9:  dup
  IL_3cda:  ldc.i4.4
  IL_3cdb:  xor
  IL_3cdc:  pop
  IL_3ce1:  dup
  IL_3ce2:  ldc.i4.8
  IL_3ce3:  mul
  IL_3ce4:  pop
  IL_3ce9:  dup
  IL_3cea:  ldc.i4.2
  IL_3ceb:  shr
  IL_3cec:  pop
  IL_3cf1:  dup
  IL_3cf2:  ldc.i4.4
  IL_3cf3:  and
  IL_3cf4:  pop
  IL_3cf9:  dup
  IL_3cfa:  ldc.i4.0
  IL_3cfb:  xor
  IL_3cfc:  pop
  IL_3d01:  dup
  IL_3d02:  ldc.i4.7
  IL_3d03:  xor
  IL_3d04:  pop
  IL_3d09:  dup
  IL_3d0a:  ldc.i4.0
  IL_3d0b:  shr
  IL_3d0c:  pop
  IL_3d11:  dup
  IL_3d12:  ldc.i4.4
  IL_3d13:  add
  IL_3d14:  pop
  IL_3d19:  dup
  IL_3d1a:  ldc.i4.6
  IL_3d1b:  shr
  IL_3d1c:  pop
  IL_3d21:  dup
  IL_3d22:  ldc.i4.1
  IL_3d23:  mul
  IL_3d24:  pop
  IL_3d29:  dup
  IL_3d2a:  ldc.i4.3
  IL_3d2b:  xor
  IL_3d2c:  pop
  IL_3d31:  dup
  IL_3d32:  ldc.i4.8
  IL_3d33:  and
  IL_3d34:  pop
  IL_3d39:  dup
  IL_3d3a:  ldc.i4.3
  IL_3d3b:  or
  IL_3d3c:  pop
  IL_3d41:  dup
  IL_3d42:  ldc.i4.4
  IL_3d43:  and
  IL_3d44:  pop
  IL_3d49:  dup
  IL_3d4a:  ldc.i4.2
  IL_3d4b:  and
  IL_3d4c:  pop
  IL_3d51:  dup
  IL_3d52:  ldc.i4.2
  IL_3d53:  mul
  IL_3d54:  pop
  IL_3d59:  dup
  IL_3d5a:  ldc.i4.6
  IL_3d5b:  add
  IL_3d5c:  pop
  IL_3d61:  dup
  IL_3d62:  ldc.i4.8
  IL_3d63:  mul
  IL_3d64:  pop
  IL_3d69:  dup
  IL_3d6a:  ldc.i4.6
  IL_3d6b:  xor
  IL_3d6c:  pop
  IL_3d71:  dup
  IL_3d72:  ldc.i4.0
  IL_3d73:  shl
  IL_3d74:  pop
  IL_3d79:  dup
  IL_3d7a:  ldc.i4.1
  IL_3d7b:  or
  IL_3d7c:  pop
  IL_3d81:  dup
  IL_3d82:  ldc.i4.0
  IL_3d83:  shr
  IL_3d84:  pop
  IL_3d89:  dup
  IL_3d8a:  ldc.i4.2
  IL_3d8b:  or
  IL_3d8c:  pop
  IL_3d91:  dup
  IL_3d92:  ldc.i4.5
  IL_3d93:  add
  IL_3d94:  pop
  IL_3d99:  dup
  IL_3d9a:  ldc.i4.2
  IL_3d9b:  shr
  IL_3d9c:  pop
  IL_3da1:  dup
  IL_3da2:  ldc.i4.8
  IL_3da3:  add
  IL_3da4:  pop
  IL_3da9:  dup
  IL_3daa:  ldc.i4.1
  IL_3dab:  add
  IL_3dac:  pop
  IL_3db1:  dup
  IL_3db2:  ldc.i4.7
  IL_3db3:  add
  IL_3db4:  pop
  IL_3db9:  dup
  IL_3dba:  ldc.i4.4
  IL_3dbb:  sub
  IL_3dbc:  pop
  IL_3dc1:  dup
  IL_3dc2:  ldc.i4.8
  IL_3dc3:  mul
  IL_3dc4:  pop
  IL_3dc9:  dup
  IL_3dca:  ldc.i4.1
  IL_3dcb:  shr
  IL_3dcc:  pop
  IL_3dd1:  dup
  IL_3dd2:  ldc.i4.5
  IL_3dd3:  sub
  IL_3dd4:  pop
  IL_3dd9:  dup
  IL_3dda:  ldc.i4.1
  IL_3ddb:  shl
  IL_3ddc:  pop
  IL_3de1:  dup
  IL_3de2:  ldc.i4.5
  IL_3de3:  add
  IL_3de4:  pop
  IL_3de9:  dup
  IL_3dea:  ldc.i4.7
  IL_3deb:  sub
  IL_3dec:  pop
  IL_3df1:  dup
  IL_3df2:  ldc.i4.3
  IL_3df3:  mul
  IL_3df4:  pop
  IL_3df9:  dup
  IL_3dfa:  ldc.i4.6
  IL_3dfb:  shl
  IL_3dfc:  pop
  IL_3e01:  dup
  IL_3e02:  ldc.i4.1
  IL_3e03:  mul
  IL_3e04:  pop
  IL_3e09:  dup
  IL_3e0a:  ldc.i4.3
  IL_3e0b:  mul
  IL_3e0c:  pop
  IL_3e11:  dup
  IL_3e12:  ldc.i4.4
  IL_3e13:  xor
  IL_3e14:  pop
  IL_3e19:  dup
  IL_3e1a:  ldc.i4.2
  IL_3e1b:  xor
  IL_3e1c:  pop
  IL_3e21:  dup
  IL_3e22:  ldc.i4.4
  IL_3e23:  shl
  IL_3e24:  pop
  IL_3e29:  dup
  IL_3e2a:  ldc.i4.7
  IL_3e2b:  sub
  IL_3e2c:  pop
  IL_3e31:  dup
  IL_3e32:  ldc.i4.1
  IL_3e33:  shr
  IL_3e34:  pop
  IL_3e39:  dup
  IL_3e3a:  ldc.i4.1
  IL_3e3b:  and
  IL_3e3c:  pop
  IL_3e41:  dup
  IL_3e42:  ldc.i4.7
  IL_3e43:  xor
  IL_3e44:  pop
  IL_3e49:  dup
  IL_3e4a:  ldc.i4.3
  IL_3e4b:  shr
  IL_3e4c:  pop
  IL_3e51:  dup
  IL_3e52:  ldc.i4.3
  IL_3e53:  xor
  IL_3e54:  pop
  IL_3e59:  dup
  IL_3e5a:  ldc.i4.3
  IL_3e5b:  and
  IL_3e5c:  pop
  IL_3e61:  dup
  IL_3e62:  ldc.i4.4
  IL_3e63:  and
  IL_3e64:  pop
  IL_3e69:  dup
  IL_3e6a:  ldc.i4.3
  IL_3e6b:  shl
  IL_3e6c:  pop
  IL_3e71:  dup
  IL_3e72:  ldc.i4.1
  IL_3e73:  add
  IL_3e74:  pop
  IL_3e79:  dup
  IL_3e7a:  ldc.i4.1
  IL_3e7b:  shr
  IL_3e7c:  pop
  IL_3e81:  dup
  IL_3e82:  ldc.i4.3
  IL_3e83:  xor
  IL_3e84:  pop
  IL_3e89:  dup
  IL_3e8a:  ldc.i4.7
  IL_3e8b:  shl
  IL_3e8c:  pop
  IL_3e91:  dup
  IL_3e92:  ldc.i4.6
  IL_3e93:  sub
  IL_3e94:  pop
  IL_3e99:  dup
  IL_3e9a:  ldc.i4.6
  IL_3e9b:  shl
  IL_3e9c:  pop
  IL_3ea1:  dup
  IL_3ea2:  ldc.i4.0
  IL_3ea3:  shr
  IL_3ea4:  pop
  IL_3ea9:  dup
  IL_3eaa:  ldc.i4.1
  IL_3eab:  add
  IL_3eac:  pop
  IL_3eb1:  dup
  IL_3eb2:  ldc.i4.3
  IL_3eb3:  or
  IL_3eb4:  pop
  IL_3eb9:  dup
  IL_3eba:  ldc.i4.3
  IL_3ebb:  or
  IL_3ebc:  pop
  IL_3ec1:  dup
  IL_3ec2:  ldc.i4.8
  IL_3ec3:  or
  IL_3ec4:  pop
  IL_3ec9:  dup
  IL_3eca:  ldc.i4.3
  IL_3ecb:  or
  IL_3ecc:  pop
  IL_3ed1:  dup
  IL_3ed2:  ldc.i4.8
  IL_3ed3:  add
  IL_3ed4:  pop
  IL_3ed9:  dup
  IL_3eda:  ldc.i4.1
  IL_3edb:  shr
  IL_3edc:  pop
  IL_3ee1:  dup
  IL_3ee2:  ldc.i4.8
  IL_3ee3:  shl
  IL_3ee4:  pop
  IL_3ee9:  dup
  IL_3eea:  ldc.i4.7
  IL_3eeb:  shr
  IL_3eec:  pop
  IL_3ef1:  dup
  IL_3ef2:  ldc.i4.0
  IL_3ef3:  shr
  IL_3ef4:  pop
  IL_3ef9:  dup
  IL_3efa:  ldc.i4.2
  IL_3efb:  xor
  IL_3efc:  pop
  IL_3f01:  dup
  IL_3f02:  ldc.i4.3
  IL_3f03:  sub
  IL_3f04:  pop
  IL_3f09:  dup
  IL_3f0a:  ldc.i4.7
  IL_3f0b:  shr
  IL_3f0c:  pop
  IL_3f11:  dup
  IL_3f12:  ldc.i4.8
  IL_3f13:  shr
  IL_3f14:  pop
  IL_3f19:  dup
  IL_3f1a:  ldc.i4.1
  IL_3f1b:  shl
  IL_3f1c:  pop
  IL_3f21:  dup
  IL_3f22:  ldc.i4.7
  IL_3f23:  and
  IL_3f24:  pop
  IL_3f29:  dup
  IL_3f2a:  ldc.i4.5
  IL_3f2b:  shr
  IL_3f2c:  pop
  IL_3f31:  dup
  IL_3f32:  ldc.i4.2
  IL_3f33:  mul
  IL_3f34:  pop
  IL_3f39:  dup
  IL_3f3a:  ldc.i4.3
  IL_3f3b:  shl
  IL_3f3c:  pop
  IL_3f41:  dup
  IL_3f42:  ldc.i4.3
  IL_3f43:  xor
  IL_3f44:  pop
  IL_3f49:  dup
  IL_3f4a:  ldc.i4.6
  IL_3f4b:  mul
  IL_3f4c:  pop
  IL_3f51:  dup
  IL_3f52:  ldc.i4.5
  IL_3f53:  mul
  IL_3f54:  pop
  IL_3f59:  dup
  IL_3f5a:  ldc.i4.6
  IL_3f5b:  mul
  IL_3f5c:  pop
  IL_3f61:  dup
  IL_3f62:  ldc.i4.2
  IL_3f63:  sub
  IL_3f64:  pop
  IL_3f69:  dup
  IL_3f6a:  ldc.i4.4
  IL_3f6b:  add
  IL_3f6c:  pop
  IL_3f71:  dup
  IL_3f72:  ldc.i4.4
  IL_3f73:  or
  IL_3f74:  pop
  IL_3f79:  dup
  IL_3f7a:  ldc.i4.4
  IL_3f7b:  xor
  IL_3f7c:  pop
  IL_3f81:  dup
  IL_3f82:  ldc.i4.3
  IL_3f83:  shr
  IL_3f84:  pop
  IL_3f89:  dup
  IL_3f8a:  ldc.i4.5
  IL_3f8b:  sub
  IL_3f8c:  pop
  IL_3f91:  dup
  IL_3f92:  ldc.i4.0
  IL_3f93:  shr
  IL_3f94:  pop
  IL_3f99:  dup
  IL_3f9a:  ldc.i4.8
  IL_3f9b:  mul
  IL_3f9c:  pop
  IL_3fa1:  dup
  IL_3fa2:  ldc.i4.7
  IL_3fa3:  or
  IL_3fa4:  pop
  IL_3fa9:  dup
  IL_3faa:  ldc.i4.1
  IL_3fab:  add
  IL_3fac:  pop
  IL_3fb1:  dup
  IL_3fb2:  ldc.i4.3
  IL_3fb3:  xor
  IL_3fb4:  pop
  IL_3fb9:  dup
  IL_3fba:  ldc.i4.3
  IL_3fbb:  shr
  IL_3fbc:  pop
  IL_3fc1:  dup
  IL_3fc2:  ldc.i4.1
  IL_3fc3:  mul
  IL_3fc4:  pop
  IL_3fc9:  dup
  IL_3fca:  ldc.i4.3
  IL_3fcb:  shl
  IL_3fcc:  pop
  IL_3fd1:  dup
  IL_3fd2:  ldc.i4.8
  IL_3fd3:  or
  IL_3fd4:  pop
  IL_3fd9:  dup
  IL_3fda:  ldc.i4.7
  IL_3fdb:  add
  IL_3fdc:  pop
  IL_3fe1:  dup
  IL_3fe2:  ldc.i4.3
  IL_3fe3:  shl
  IL_3fe4:  pop
  IL_3fe9:  dup
  IL_3fea:  ldc.i4.3
  IL_3feb:  and
  IL_3fec:  pop
  IL_3ff1:  dup
  IL_3ff2:  ldc.i4.1
  IL_3ff3:  add
  IL_3ff4:  pop
  IL_3ff9:  dup
  IL_3ffa:  ldc.i4.0
  IL_3ffb:  and
  IL_3ffc:  pop
  IL_4001:  dup
  IL_4002:  ldc.i4.2
  IL_4003:  sub
  IL_4004:  pop
  IL_4009:  dup
  IL_400a:  ldc.i4.2
  IL_400b:  shl
  IL_400c:  pop
  IL_4011:  dup
  IL_4012:  ldc.i4.6
  IL_4013:  sub
  IL_4014:  pop
  IL_4019:  dup
  IL_401a:  ldc.i4.4
  IL_401b:  shr
  IL_401c:  pop
  IL_4021:  dup
  IL_4022:  ldc.i4.4
  IL_4023:  xor
  IL_4024:  pop
  IL_4029:  dup
  IL_402a:  ldc.i4.4
  IL_402b:  shr
  IL_402c:  pop
  IL_4031:  dup
  IL_4032:  ldc.i4.0
  IL_4033:  shr
  IL_4034:  pop
  IL_4039:  dup
  IL_403a:  ldc.i4.8
  IL_403b:  and
  IL_403c:  pop
  IL_4041:  dup
  IL_4042:  ldc.i4.6
  IL_4043:  or
  IL_4044:  pop
  IL_4049:  dup
  IL_404a:  ldc.i4.8
  IL_404b:  sub
  IL_404c:  pop
  IL_4051:  dup
  IL_4052:  ldc.i4.5
  IL_4053:  or
  IL_4054:  pop
  IL_4059:  dup
  IL_405a:  ldc.i4.3
  IL_405b:  xor
  IL_405c:  pop
  IL_4061:  dup
  IL_4062:  ldc.i4.8
  IL_4063:  shl
  IL_4064:  pop
  IL_4069:  dup
  IL_406a:  ldc.i4.6
  IL_406b:  shl
  IL_406c:  pop
  IL_4071:  dup
  IL_4072:  ldc.i4.3
  IL_4073:  sub
  IL_4074:  pop
  IL_4079:  dup
  IL_407a:  ldc.i4.1
  IL_407b:  shr
  IL_407c:  pop
  IL_4081:  dup
  IL_4082:  ldc.i4.7
  IL_4083:  mul
  IL_4084:  pop
  IL_4089:  dup
  IL_408a:  ldc.i4.7
  IL_408b:  sub
  IL_408c:  pop
  IL_4091:  dup
  IL_4092:  ldc.i4.1
  IL_4093:  and
  IL_4094:  pop
  IL_4099:  dup
  IL_409a:  ldc.i4.8
  IL_409b:  or
  IL_409c:  pop
  IL_40a1:  dup
  IL_40a2:  ldc.i4.7
  IL_40a3:  shr
  IL_40a4:  pop
  IL_40a9:  dup
  IL_40aa:  ldc.i4.1
  IL_40ab:  shl
  IL_40ac:  pop
  IL_40b1:  dup
  IL_40b2:  ldc.i4.4
  IL_40b3:  add
  IL_40b4:  pop
  IL_40b9:  dup
  IL_40ba:  ldc.i4.0
  IL_40bb:  and
  IL_40bc:  pop
  IL_40c1:  dup
  IL_40c2:  ldc.i4.5
  IL_40c3:  shl
  IL_40c4:  pop
  IL_40c9:  dup
  IL_40ca:  ldc.i4.0
  IL_40cb:  sub
  IL_40cc:  pop
  IL_40d1:  dup
  IL_40d2:  ldc.i4.0
  IL_40d3:  or
  IL_40d4:  pop
  IL_40d9:  dup
  IL_40da:  ldc.i4.1
  IL_40db:  mul
  IL_40dc:  pop
  IL_40e1:  dup
  IL_40e2:  ldc.i4.6
  IL_40e3:  and
  IL_40e4:  pop
  IL_40e9:  dup
  IL_40ea:  ldc.i4.5
  IL_40eb:  sub
  IL_40ec:  pop
  IL_40f1:  dup
  IL_40f2:  ldc.i4.4
  IL_40f3:  or
  IL_40f4:  pop
  IL_40f9:  dup
  IL_40fa:  ldc.i4.3
  IL_40fb:  sub
  IL_40fc:  pop
  IL_4101:  dup
  IL_4102:  ldc.i4.2
  IL_4103:  shr
  IL_4104:  pop
  IL_4109:  dup
  IL_410a:  ldc.i4.8
  IL_410b:  and
  IL_410c:  pop
  IL_4111:  dup
  IL_4112:  ldc.i4.7
  IL_4113:  shr
  IL_4114:  pop
  IL_4119:  dup
  IL_411a:  ldc.i4.5
  IL_411b:  or
  IL_411c:  pop
  IL_4121:  dup
  IL_4122:  ldc.i4.8
  IL_4123:  add
  IL_4124:  pop
  IL_4129:  dup
  IL_412a:  ldc.i4.3
  IL_412b:  or
  IL_412c:  pop
  IL_4131:  dup
  IL_4132:  ldc.i4.7
  IL_4133:  or
  IL_4134:  pop
  IL_4139:  dup
  IL_413a:  ldc.i4.8
  IL_413b:  and
  IL_413c:  pop
  IL_4141:  dup
  IL_4142:  ldc.i4.4
  IL_4143:  add
  IL_4144:  pop
  IL_4149:  dup
  IL_414a:  ldc.i4.8
  IL_414b:  and
  IL_414c:  pop
  IL_4151:  dup
  IL_4152:  ldc.i4.1
  IL_4153:  or
  IL_4154:  pop
  IL_4159:  dup
  IL_415a:  ldc.i4.5
  IL_415b:  or
  IL_415c:  pop
  IL_4161:  dup
  IL_4162:  ldc.i4.1
  IL_4163:  shl
  IL_4164:  pop
  IL_4169:  dup
  IL_416a:  ldc.i4.4
  IL_416b:  xor
  IL_416c:  pop
  IL_4171:  dup
  IL_4172:  ldc.i4.1
  IL_4173:  shr
  IL_4174:  pop
  IL_4179:  dup
  IL_417a:  ldc.i4.3
  IL_417b:  or
  IL_417c:  pop
  IL_4181:  dup
  IL_4182:  ldc.i4.3
  IL_4183:  or
  IL_4184:  pop
  IL_4189:  dup
  IL_418a:  ldc.i4.2
  IL_418b:  and
  IL_418c:  pop
  IL_4191:  dup
  IL_4192:  ldc.i4.1
  IL_4193:  mul
  IL_4194:  pop
  IL_4199:  dup
  IL_419a:  ldc.i4.8
  IL_419b:  and
  IL_419c:  pop
  IL_41a1:  dup
  IL_41a2:  ldc.i4.8
  IL_41a3:  sub
  IL_41a4:  pop
  IL_41a9:  dup
  IL_41aa:  ldc.i4.2
  IL_41ab:  add
  IL_41ac:  pop
  IL_41b1:  dup
  IL_41b2:  ldc.i4.4
  IL_41b3:  sub
  IL_41b4:  pop
  IL_41b9:  dup
  IL_41ba:  ldc.i4.4
  IL_41bb:  and
  IL_41bc:  pop
  IL_41c1:  dup
  IL_41c2:  ldc.i4.8
  IL_41c3:  and
  IL_41c4:  pop
  IL_41c9:  dup
  IL_41ca:  ldc.i4.6
  IL_41cb:  xor
  IL_41cc:  pop
  IL_41d1:  dup
  IL_41d2:  ldc.i4.8
  IL_41d3:  shl
  IL_41d4:  pop
  IL_41d9:  dup
  IL_41da:  ldc.i4.0
  IL_41db:  xor
  IL_41dc:  pop
  IL_41e1:  dup
  IL_41e2:  ldc.i4.0
  IL_41e3:  add
  IL_41e4:  pop
  IL_41e9:  dup
  IL_41ea:  ldc.i4.5
  IL_41eb:  mul
  IL_41ec:  pop
  IL_41f1:  dup
  IL_41f2:  ldc.i4.2
  IL_41f3:  sub
  IL_41f4:  pop
  IL_41f9:  dup
  IL_41fa:  ldc.i4.6
  IL_41fb:  mul
  IL_41fc:  pop
  IL_4201:  dup
  IL_4202:  ldc.i4.5
  IL_4203:  shl
  IL_4204:  pop
  IL_4209:  dup
  IL_420a:  ldc.i4.5
  IL_420b:  or
  IL_420c:  pop
  IL_4211:  dup
  IL_4212:  ldc.i4.6
  IL_4213:  add
  IL_4214:  pop
  IL_4219:  dup
  IL_421a:  ldc.i4.6
  IL_421b:  mul
  IL_421c:  pop
  IL_4221:  dup
  IL_4222:  ldc.i4.1
  IL_4223:  sub
  IL_4224:  pop
  IL_4229:  dup
  IL_422a:  ldc.i4.6
  IL_422b:  add
  IL_422c:  pop
  IL_4231:  dup
  IL_4232:  ldc.i4.5
  IL_4233:  shl
  IL_4234:  pop
  IL_4239:  dup
  IL_423a:  ldc.i4.4
  IL_423b:  or
  IL_423c:  pop
  IL_4241:  dup
  IL_4242:  ldc.i4.0
  IL_4243:  shl
  IL_4244:  pop
  IL_4249:  dup
  IL_424a:  ldc.i4.4
  IL_424b:  xor
  IL_424c:  pop
  IL_4251:  dup
  IL_4252:  ldc.i4.0
  IL_4253:  or
  IL_4254:  pop
  IL_4259:  dup
  IL_425a:  ldc.i4.8
  IL_425b:  and
  IL_425c:  pop
  IL_4261:  dup
  IL_4262:  ldc.i4.8
  IL_4263:  and
  IL_4264:  pop
  IL_4269:  dup
  IL_426a:  ldc.i4.6
  IL_426b:  add
  IL_426c:  pop
  IL_4271:  dup
  IL_4272:  ldc.i4.5
  IL_4273:  shl
  IL_4274:  pop
  IL_4279:  dup
  IL_427a:  ldc.i4.4
  IL_427b:  and
  IL_427c:  pop
  IL_4281:  dup
  IL_4282:  ldc.i4.3
  IL_4283:  or
  IL_4284:  pop
  IL_4289:  dup
  IL_428a:  ldc.i4.3
  IL_428b:  or
  IL_428c:  pop
  IL_4291:  dup
  IL_4292:  ldc.i4.5
  IL_4293:  shl
  IL_4294:  pop
  IL_4299:  dup
  IL_429a:  ldc.i4.1
  IL_429b:  and
  IL_429c:  pop
  IL_42a1:  dup
  IL_42a2:  ldc.i4.0
  IL_42a3:  xor
  IL_42a4:  pop
  IL_42a9:  dup
  IL_42aa:  ldc.i4.2
  IL_42ab:  and
  IL_42ac:  pop
  IL_42b1:  dup
  IL_42b2:  ldc.i4.7
  IL_42b3:  or
  IL_42b4:  pop
  IL_42b9:  dup
  IL_42ba:  ldc.i4.7
  IL_42bb:  xor
  IL_42bc:  pop
  IL_42c1:  dup
  IL_42c2:  ldc.i4.5
  IL_42c3:  shr
  IL_42c4:  pop
  IL_42c9:  dup
  IL_42ca:  ldc.i4.2
  IL_42cb:  shr
  IL_42cc:  pop
  IL_42d1:  dup
  IL_42d2:  ldc.i4.6
  IL_42d3:  mul
  IL_42d4:  pop
  IL_42d9:  dup
  IL_42da:  ldc.i4.2
  IL_42db:  shr
  IL_42dc:  pop
  IL_42e1:  dup
  IL_42e2:  ldc.i4.5
  IL_42e3:  and
  IL_42e4:  pop
  IL_42e9:  dup
  IL_42ea:  ldc.i4.0
  IL_42eb:  or
  IL_42ec:  pop
  IL_42f1:  dup
  IL_42f2:  ldc.i4.3
  IL_42f3:  shr
  IL_42f4:  pop
  IL_42f9:  dup
  IL_42fa:  ldc.i4.5
  IL_42fb:  shl
  IL_42fc:  pop
  IL_4301:  dup
  IL_4302:  ldc.i4.5
  IL_4303:  or
  IL_4304:  pop
  IL_4309:  dup
  IL_430a:  ldc.i4.5
  IL_430b:  shr
  IL_430c:  pop
  IL_4311:  dup
  IL_4312:  ldc.i4.1
  IL_4313:  xor
  IL_4314:  pop
  IL_4319:  dup
  IL_431a:  ldc.i4.2
  IL_431b:  shl
  IL_431c:  pop
  IL_4321:  dup
  IL_4322:  ldc.i4.3
  IL_4323:  shr
  IL_4324:  pop
  IL_4329:  dup
  IL_432a:  ldc.i4.3
  IL_432b:  shr
  IL_432c:  pop
  IL_4331:  dup
  IL_4332:  ldc.i4.5
  IL_4333:  sub
  IL_4334:  pop
  IL_4339:  dup
  IL_433a:  ldc.i4.8
  IL_433b:  or
  IL_433c:  pop
  IL_4341:  dup
  IL_4342:  ldc.i4.0
  IL_4343:  mul
  IL_4344:  pop
  IL_4349:  dup
  IL_434a:  ldc.i4.4
  IL_434b:  or
  IL_434c:  pop
  IL_4351:  dup
  IL_4352:  ldc.i4.2
  IL_4353:  and
  IL_4354:  pop
  IL_4359:  dup
  IL_435a:  ldc.i4.0
  IL_435b:  sub
  IL_435c:  pop
  IL_4361:  dup
  IL_4362:  ldc.i4.6
  IL_4363:  and
  IL_4364:  pop
  IL_4369:  dup
  IL_436a:  ldc.i4.3
  IL_436b:  and
  IL_436c:  pop
  IL_4371:  dup
  IL_4372:  ldc.i4.4
  IL_4373:  mul
  IL_4374:  pop
  IL_4379:  dup
  IL_437a:  ldc.i4.3
  IL_437b:  xor
  IL_437c:  pop
  IL_4381:  dup
  IL_4382:  ldc.i4.7
  IL_4383:  shr
  IL_4384:  pop
  IL_4389:  dup
  IL_438a:  ldc.i4.1
  IL_438b:  and
  IL_438c:  pop
  IL_4391:  dup
  IL_4392:  ldc.i4.2
  IL_4393:  sub
  IL_4394:  pop
  IL_4399:  dup
  IL_439a:  ldc.i4.2
  IL_439b:  add
  IL_439c:  pop
  IL_43a1:  dup
  IL_43a2:  ldc.i4.3
  IL_43a3:  and
  IL_43a4:  pop
  IL_43a9:  dup
  IL_43aa:  ldc.i4.2
  IL_43ab:  or
  IL_43ac:  pop
  IL_43b1:  dup
  IL_43b2:  ldc.i4.8
  IL_43b3:  shl
  IL_43b4:  pop
  IL_43b9:  dup
  IL_43ba:  ldc.i4.8
  IL_43bb:  xor
  IL_43bc:  pop
  IL_43c1:  dup
  IL_43c2:  ldc.i4.7
  IL_43c3:  add
  IL_43c4:  pop
  IL_43c9:  dup
  IL_43ca:  ldc.i4.7
  IL_43cb:  add
  IL_43cc:  pop
  IL_43d1:  dup
  IL_43d2:  ldc.i4.1
  IL_43d3:  shr
  IL_43d4:  pop
  IL_43d9:  dup
  IL_43da:  ldc.i4.3
  IL_43db:  and
  IL_43dc:  pop
  IL_43e1:  dup
  IL_43e2:  ldc.i4.8
  IL_43e3:  xor
  IL_43e4:  pop
  IL_43e9:  dup
  IL_43ea:  ldc.i4.8
  IL_43eb:  xor
  IL_43ec:  pop
  IL_43f1:  dup
  IL_43f2:  ldc.i4.0
  IL_43f3:  shl
  IL_43f4:  pop
  IL_43f9:  dup
  IL_43fa:  ldc.i4.8
  IL_43fb:  sub
  IL_43fc:  pop
  IL_4401:  dup
  IL_4402:  ldc.i4.2
  IL_4403:  or
  IL_4404:  pop
  IL_4409:  dup
  IL_440a:  ldc.i4.7
  IL_440b:  or
  IL_440c:  pop
  IL_4411:  dup
  IL_4412:  ldc.i4.0
  IL_4413:  sub
  IL_4414:  pop
  IL_4419:  dup
  IL_441a:  ldc.i4.5
  IL_441b:  or
  IL_441c:  pop
  IL_4421:  dup
  IL_4422:  ldc.i4.0
  IL_4423:  xor
  IL_4424:  pop
  IL_4429:  dup
  IL_442a:  ldc.i4.4
  IL_442b:  xor
  IL_442c:  pop
  IL_4431:  dup
  IL_4432:  ldc.i4.5
  IL_4433:  add
  IL_4434:  pop
  IL_4439:  dup
  IL_443a:  ldc.i4.1
  IL_443b:  sub
  IL_443c:  pop
  IL_4441:  dup
  IL_4442:  ldc.i4.7
  IL_4443:  add
  IL_4444:  pop
  IL_4449:  dup
  IL_444a:  ldc.i4.7
  IL_444b:  xor
  IL_444c:  pop
  IL_4451:  dup
  IL_4452:  ldc.i4.3
  IL_4453:  mul
  IL_4454:  pop
  IL_4459:  dup
  IL_445a:  ldc.i4.2
  IL_445b:  add
  IL_445c:  pop
  IL_4461:  dup
  IL_4462:  ldc.i4.3
  IL_4463:  xor
  IL_4464:  pop
  IL_4469:  dup
  IL_446a:  ldc.i4.7
  IL_446b:  and
  IL_446c:  pop
  IL_4471:  dup
  IL_4472:  ldc.i4.1
  IL_4473:  and
  IL_4474:  pop
  IL_4479:  dup
  IL_447a:  ldc.i4.8
  IL_447b:  and
  IL_447c:  pop
  IL_4481:  dup
  IL_4482:  ldc.i4.7
  IL_4483:  add
  IL_4484:  pop
  IL_4489:  dup
  IL_448a:  ldc.i4.5
  IL_448b:  or
  IL_448c:  pop
  IL_4491:  dup
  IL_4492:  ldc.i4.6
  IL_4493:  add
  IL_4494:  pop
  IL_4499:  dup
  IL_449a:  ldc.i4.3
  IL_449b:  shr
  IL_449c:  pop
  IL_44a1:  dup
  IL_44a2:  ldc.i4.3
  IL_44a3:  or
  IL_44a4:  pop
  IL_44a9:  dup
  IL_44aa:  ldc.i4.0
  IL_44ab:  shr
  IL_44ac:  pop
  IL_44b1:  dup
  IL_44b2:  ldc.i4.1
  IL_44b3:  or
  IL_44b4:  pop
  IL_44b9:  dup
  IL_44ba:  ldc.i4.1
  IL_44bb:  and
  IL_44bc:  pop
  IL_44c1:  dup
  IL_44c2:  ldc.i4.2
  IL_44c3:  and
  IL_44c4:  pop
  IL_44c9:  dup
  IL_44ca:  ldc.i4.5
  IL_44cb:  add
  IL_44cc:  pop
  IL_44d1:  dup
  IL_44d2:  ldc.i4.5
  IL_44d3:  shl
  IL_44d4:  pop
  IL_44d9:  dup
  IL_44da:  ldc.i4.6
  IL_44db:  add
  IL_44dc:  pop
  IL_44e1:  dup
  IL_44e2:  ldc.i4.6
  IL_44e3:  shr
  IL_44e4:  pop
  IL_44e9:  dup
  IL_44ea:  ldc.i4.4
  IL_44eb:  shl
  IL_44ec:  pop
  IL_44f1:  dup
  IL_44f2:  ldc.i4.2
  IL_44f3:  and
  IL_44f4:  pop
  IL_44f9:  dup
  IL_44fa:  ldc.i4.6
  IL_44fb:  sub
  IL_44fc:  pop
  IL_4501:  dup
  IL_4502:  ldc.i4.4
  IL_4503:  shr
  IL_4504:  pop
  IL_4509:  dup
  IL_450a:  ldc.i4.5
  IL_450b:  shl
  IL_450c:  pop
  IL_4511:  dup
  IL_4512:  ldc.i4.3
  IL_4513:  sub
  IL_4514:  pop
  IL_4519:  dup
  IL_451a:  ldc.i4.3
  IL_451b:  shr
  IL_451c:  pop
  IL_4521:  dup
  IL_4522:  ldc.i4.5
  IL_4523:  sub
  IL_4524:  pop
  IL_4529:  dup
  IL_452a:  ldc.i4.0
  IL_452b:  xor
  IL_452c:  pop
  IL_4531:  dup
  IL_4532:  ldc.i4.1
  IL_4533:  shl
  IL_4534:  pop
  IL_4539:  dup
  IL_453a:  ldc.i4.8
  IL_453b:  xor
  IL_453c:  pop
  IL_4541:  dup
  IL_4542:  ldc.i4.2
  IL_4543:  add
  IL_4544:  pop
  IL_4549:  dup
  IL_454a:  ldc.i4.5
  IL_454b:  xor
  IL_454c:  pop
  IL_4551:  dup
  IL_4552:  ldc.i4.1
  IL_4553:  or
  IL_4554:  pop
  IL_4559:  dup
  IL_455a:  ldc.i4.2
  IL_455b:  mul
  IL_455c:  pop
  IL_4561:  dup
  IL_4562:  ldc.i4.3
  IL_4563:  shl
  IL_4564:  pop
  IL_4569:  dup
  IL_456a:  ldc.i4.2
  IL_456b:  add
  IL_456c:  pop
  IL_4571:  dup
  IL_4572:  ldc.i4.4
  IL_4573:  and
  IL_4574:  pop
  IL_4579:  dup
  IL_457a:  ldc.i4.3
  IL_457b:  shl
  IL_457c:  pop
  IL_4581:  dup
  IL_4582:  ldc.i4.2
  IL_4583:  or
  IL_4584:  pop
  IL_4589:  dup
  IL_458a:  ldc.i4.1
  IL_458b:  xor
  IL_458c:  pop
  IL_4591:  dup
  IL_4592:  ldc.i4.2
  IL_4593:  shl
  IL_4594:  pop
  IL_4599:  dup
  IL_459a:  ldc.i4.3
  IL_459b:  add
  IL_459c:  pop
  IL_45a1:  dup
  IL_45a2:  ldc.i4.3
  IL_45a3:  or
  IL_45a4:  pop
  IL_45a9:  dup
  IL_45aa:  ldc.i4.4
  IL_45ab:  xor
  IL_45ac:  pop
  IL_45b1:  dup
  IL_45b2:  ldc.i4.4
  IL_45b3:  mul
  IL_45b4:  pop
  IL_45b9:  dup
  IL_45ba:  ldc.i4.7
  IL_45bb:  add
  IL_45bc:  pop
  IL_45c1:  dup
  IL_45c2:  ldc.i4.6
  IL_45c3:  add
  IL_45c4:  pop
  IL_45c9:  dup
  IL_45ca:  ldc.i4.3
  IL_45cb:  sub
  IL_45cc:  pop
  IL_45d1:  dup
  IL_45d2:  ldc.i4.2
  IL_45d3:  sub
  IL_45d4:  pop
  IL_45d9:  dup
  IL_45da:  ldc.i4.0
  IL_45db:  shl
  IL_45dc:  pop
  IL_45e1:  dup
  IL_45e2:  ldc.i4.4
  IL_45e3:  mul
  IL_45e4:  pop
  IL_45e9:  dup
  IL_45ea:  ldc.i4.1
  IL_45eb:  and
  IL_45ec:  pop
  IL_45f1:  dup
  IL_45f2:  ldc.i4.8
  IL_45f3:  mul
  IL_45f4:  pop
  IL_45f9:  dup
  IL_45fa:  ldc.i4.1
  IL_45fb:  or
  IL_45fc:  pop
  IL_4601:  dup
  IL_4602:  ldc.i4.6
  IL_4603:  xor
  IL_4604:  pop
  IL_4609:  dup
  IL_460a:  ldc.i4.1
  IL_460b:  mul
  IL_460c:  pop
  IL_4611:  dup
  IL_4612:  ldc.i4.3
  IL_4613:  shl
  IL_4614:  pop
  IL_4619:  dup
  IL_461a:  ldc.i4.1
  IL_461b:  shr
  IL_461c:  pop
  IL_4621:  dup
  IL_4622:  ldc.i4.6
  IL_4623:  xor
  IL_4624:  pop
  IL_4629:  dup
  IL_462a:  ldc.i4.8
  IL_462b:  sub
  IL_462c:  pop
  IL_4631:  dup
  IL_4632:  ldc.i4.3
  IL_4633:  and
  IL_4634:  pop
  IL_4639:  dup
  IL_463a:  ldc.i4.7
  IL_463b:  add
  IL_463c:  pop
  IL_4641:  dup
  IL_4642:  ldc.i4.6
  IL_4643:  sub
  IL_4644:  pop
  IL_4649:  dup
  IL_464a:  ldc.i4.2
  IL_464b:  xor
  IL_464c:  pop
  IL_4651:  dup
  IL_4652:  ldc.i4.3
  IL_4653:  sub
  IL_4654:  pop
  IL_4659:  dup
  IL_465a:  ldc.i4.3
  IL_465b:  add
  IL_465c:  pop
  IL_4661:  dup
  IL_4662:  ldc.i4.7
  IL_4663:  shl
  IL_4664:  pop
  IL_4669:  dup
  IL_466a:  ldc.i4.8
  IL_466b:  add
  IL_466c:  pop
  IL_4671:  dup
  IL_4672:  ldc.i4.3
  IL_4673:  shl
  IL_4674:  pop
  IL_4679:  dup
  IL_467a:  ldc.i4.0
  IL_467b:  add
  IL_467c:  pop
  IL_4681:  dup
  IL_4682:  ldc.i4.6
  IL_4683:  shl
  IL_4684:  pop
  IL_4689:  dup
  IL_468a:  ldc.i4.6
  IL_468b:  add
  IL_468c:  pop
  IL_4691:  dup
  IL_4692:  ldc.i4.1
  IL_4693:  shl
  IL_4694:  pop
  IL_4699:  dup
  IL_469a:  ldc.i4.6
  IL_469b:  sub
  IL_469c:  pop
  IL_46a1:  dup
  IL_46a2:  ldc.i4.8
  IL_46a3:  shr
  IL_46a4:  pop
  IL_46a9:  dup
  IL_46aa:  ldc.i4.4
  IL_46ab:  shr
  IL_46ac:  pop
  IL_46b1:  dup
  IL_46b2:  ldc.i4.2
  IL_46b3:  and
  IL_46b4:  pop
  IL_46b9:  dup
  IL_46ba:  ldc.i4.1
  IL_46bb:  shr
  IL_46bc:  pop
  IL_46c1:  dup
  IL_46c2:  ldc.i4.3
  IL_46c3:  add
  IL_46c4:  pop
  IL_46c9:  dup
  IL_46ca:  ldc.i4.0
  IL_46cb:  and
  IL_46cc:  pop
  IL_46d1:  dup
  IL_46d2:  ldc.i4.0
  IL_46d3:  shr
  IL_46d4:  pop
  IL_46d9:  dup
  IL_46da:  ldc.i4.4
  IL_46db:  shl
  IL_46dc:  pop
  IL_46e1:  dup
  IL_46e2:  ldc.i4.0
  IL_46e3:  add
  IL_46e4:  pop
  IL_46e9:  dup
  IL_46ea:  ldc.i4.4
  IL_46eb:  and
  IL_46ec:  pop
  IL_46f1:  dup
  IL_46f2:  ldc.i4.5
  IL_46f3:  add
  IL_46f4:  pop
  IL_46f9:  dup
  IL_46fa:  ldc.i4.1
  IL_46fb:  add
  IL_46fc:  pop
  IL_4701:  dup
  IL_4702:  ldc.i4.3
  IL_4703:  or
  IL_4704:  pop
  IL_4709:  dup
  IL_470a:  ldc.i4.4
  IL_470b:  and
  IL_470c:  pop
  IL_4711:  dup
  IL_4712:  ldc.i4.0
  IL_4713:  and
  IL_4714:  pop
  IL_4719:  dup
  IL_471a:  ldc.i4.8
  IL_471b:  mul
  IL_471c:  pop
  IL_4721:  dup
  IL_4722:  ldc.i4.3
  IL_4723:  add
  IL_4724:  pop
  IL_4729:  dup
  IL_472a:  ldc.i4.4
  IL_472b:  shr
  IL_472c:  pop
  IL_4731:  dup
  IL_4732:  ldc.i4.6
  IL_4733:  shr
  IL_4734:  pop
  IL_4739:  dup
  IL_473a:  ldc.i4.7
  IL_473b:  add
  IL_473c:  pop
  IL_4741:  dup
  IL_4742:  ldc.i4.4
  IL_4743:  mul
  IL_4744:  pop
  IL_4749:  dup
  IL_474a:  ldc.i4.5
  IL_474b:  mul
  IL_474c:  pop
  IL_4751:  dup
  IL_4752:  ldc.i4.4
  IL_4753:  mul
  IL_4754:  pop
  IL_4759:  dup
  IL_475a:  ldc.i4.0
  IL_475b:  add
  IL_475c:  pop
  IL_4761:  dup
  IL_4762:  ldc.i4.1
  IL_4763:  sub
  IL_4764:  pop
  IL_4769:  dup
  IL_476a:  ldc.i4.8
  IL_476b:  add
  IL_476c:  pop
  IL_4771:  dup
  IL_4772:  ldc.i4.0
  IL_4773:  or
  IL_4774:  pop
  IL_4779:  dup
  IL_477a:  ldc.i4.5
  IL_477b:  and
  IL_477c:  pop
  IL_4781:  dup
  IL_4782:  ldc.i4.4
  IL_4783:  xor
  IL_4784:  pop
  IL_4789:  dup
  IL_478a:  ldc.i4.3
  IL_478b:  mul
  IL_478c:  pop
  IL_4791:  dup
  IL_4792:  ldc.i4.2
  IL_4793:  mul
  IL_4794:  pop
  IL_4799:  dup
  IL_479a:  ldc.i4.6
  IL_479b:  shl
  IL_479c:  pop
  IL_47a1:  dup
  IL_47a2:  ldc.i4.1
  IL_47a3:  and
  IL_47a4:  pop
  IL_47a9:  dup
  IL_47aa:  ldc.i4.3
  IL_47ab:  or
  IL_47ac:  pop
  IL_47b1:  dup
  IL_47b2:  ldc.i4.1
  IL_47b3:  shr
  IL_47b4:  pop
  IL_47b9:  dup
  IL_47ba:  ldc.i4.7
  IL_47bb:  xor
  IL_47bc:  pop
  IL_47c1:  dup
  IL_47c2:  ldc.i4.2
  IL_47c3:  mul
  IL_47c4:  pop
  IL_47c9:  dup
  IL_47ca:  ldc.i4.2
  IL_47cb:  mul
  IL_47cc:  pop
  IL_47d1:  dup
  IL_47d2:  ldc.i4.4
  IL_47d3:  or
  IL_47d4:  pop
  IL_47d9:  dup
  IL_47da:  ldc.i4.5
  IL_47db:  add
  IL_47dc:  pop
  IL_47e1:  dup
  IL_47e2:  ldc.i4.7
  IL_47e3:  xor
  IL_47e4:  pop
  IL_47e9:  dup
  IL_47ea:  ldc.i4.6
  IL_47eb:  and
  IL_47ec:  pop
  IL_47f1:  dup
  IL_47f2:  ldc.i4.8
  IL_47f3:  and
  IL_47f4:  pop
  IL_47f9:  dup
  IL_47fa:  ldc.i4.1
  IL_47fb:  xor
  IL_47fc:  pop
  IL_4801:  dup
  IL_4802:  ldc.i4.4
  IL_4803:  shr
  IL_4804:  pop
  IL_4809:  dup
  IL_480a:  ldc.i4.2
  IL_480b:  sub
  IL_480c:  pop
  IL_4811:  dup
  IL_4812:  ldc.i4.2
  IL_4813:  xor
  IL_4814:  pop
  IL_4819:  dup
  IL_481a:  ldc.i4.7
  IL_481b:  sub
  IL_481c:  pop
  IL_4821:  dup
  IL_4822:  ldc.i4.5
  IL_4823:  add
  IL_4824:  pop
  IL_4829:  dup
  IL_482a:  ldc.i4.1
  IL_482b:  shr
  IL_482c:  pop
  IL_4831:  dup
  IL_4832:  ldc.i4.8
  IL_4833:  sub
  IL_4834:  pop
  IL_4839:  dup
  IL_483a:  ldc.i4.7
  IL_483b:  mul
  IL_483c:  pop
  IL_4841:  dup
  IL_4842:  ldc.i4.3
  IL_4843:  shl
  IL_4844:  pop
  IL_4849:  dup
  IL_484a:  ldc.i4.7
  IL_484b:  shr
  IL_484c:  pop
  IL_4851:  dup
  IL_4852:  ldc.i4.5
  IL_4853:  mul
  IL_4854:  pop
  IL_4859:  dup
  IL_485a:  ldc.i4.3
  IL_485b:  shl
  IL_485c:  pop
  IL_4861:  dup
  IL_4862:  ldc.i4.7
  IL_4863:  sub
  IL_4864:  pop
  IL_4869:  dup
  IL_486a:  ldc.i4.5
  IL_486b:  and
  IL_486c:  pop
  IL_4871:  dup
  IL_4872:  ldc.i4.4
  IL_4873:  and
  IL_4874:  pop
  IL_4879:  dup
  IL_487a:  ldc.i4.6
  IL_487b:  mul
  IL_487c:  pop
  IL_4881:  dup
  IL_4882:  ldc.i4.5
  IL_4883:  or
  IL_4884:  pop
  IL_4889:  dup
  IL_488a:  ldc.i4.3
  IL_488b:  shl
  IL_488c:  pop
  IL_4891:  dup
  IL_4892:  ldc.i4.4
  IL_4893:  shl
  IL_4894:  pop
  IL_4899:  dup
  IL_489a:  ldc.i4.1
  IL_489b:  and
  IL_489c:  pop
  IL_48a1:  dup
  IL_48a2:  ldc.i4.5
  IL_48a3:  add
  IL_48a4:  pop
  IL_48a9:  dup
  IL_48aa:  ldc.i4.5
  IL_48ab:  xor
  IL_48ac:  pop
  IL_48b1:  dup
  IL_48b2:  ldc.i4.6
  IL_48b3:  and
  IL_48b4:  pop
  IL_48b9:  dup
  IL_48ba:  ldc.i4.1
  IL_48bb:  shl
  IL_48bc:  pop
  IL_48c1:  dup
  IL_48c2:  ldc.i4.5
  IL_48c3:  xor
  IL_48c4:  pop
  IL_48c9:  dup
  IL_48ca:  ldc.i4.4
  IL_48cb:  xor
  IL_48cc:  pop
  IL_48d1:  dup
  IL_48d2:  ldc.i4.0
  IL_48d3:  xor
  IL_48d4:  pop
  IL_48d9:  dup
  IL_48da:  ldc.i4.7
  IL_48db:  sub
  IL_48dc:  pop
  IL_48e1:  dup
  IL_48e2:  ldc.i4.2
  IL_48e3:  shr
  IL_48e4:  pop
  IL_48e9:  dup
  IL_48ea:  ldc.i4.3
  IL_48eb:  add
  IL_48ec:  pop
  IL_48f1:  dup
  IL_48f2:  ldc.i4.7
  IL_48f3:  or
  IL_48f4:  pop
  IL_48f9:  dup
  IL_48fa:  ldc.i4.6
  IL_48fb:  and
  IL_48fc:  pop
  IL_4901:  dup
  IL_4902:  ldc.i4.3
  IL_4903:  and
  IL_4904:  pop
  IL_4909:  dup
  IL_490a:  ldc.i4.1
  IL_490b:  add
  IL_490c:  pop
  IL_4911:  dup
  IL_4912:  ldc.i4.7
  IL_4913:  mul
  IL_4914:  pop
  IL_4919:  dup
  IL_491a:  ldc.i4.7
  IL_491b:  shr
  IL_491c:  pop
  IL_4921:  dup
  IL_4922:  ldc.i4.3
  IL_4923:  add
  IL_4924:  pop
  IL_4929:  dup
  IL_492a:  ldc.i4.1
  IL_492b:  shl
  IL_492c:  pop
  IL_4931:  dup
  IL_4932:  ldc.i4.6
  IL_4933:  and
  IL_4934:  pop
  IL_4939:  dup
  IL_493a:  ldc.i4.7
  IL_493b:  add
  IL_493c:  pop
  IL_4941:  dup
  IL_4942:  ldc.i4.0
  IL_4943:  or
  IL_4944:  pop
  IL_4949:  dup
  IL_494a:  ldc.i4.2
  IL_494b:  add
  IL_494c:  pop
  IL_4951:  dup
  IL_4952:  ldc.i4.3
  IL_4953:  mul
  IL_4954:  pop
  IL_4959:  dup
  IL_495a:  ldc.i4.2
  IL_495b:  add
  IL_495c:  pop
  IL_4961:  dup
  IL_4962:  ldc.i4.0
  IL_4963:  sub
  IL_4964:  pop
  IL_4969:  dup
  IL_496a:  ldc.i4.2
  IL_496b:  and
  IL_496c:  pop
  IL_4971:  dup
  IL_4972:  ldc.i4.2
  IL_4973:  add
  IL_4974:  pop
  IL_4979:  dup
  IL_497a:  ldc.i4.8
  IL_497b:  shl
  IL_497c:  pop
  IL_4981:  dup
  IL_4982:  ldc.i4.3
  IL_4983:  and
  IL_4984:  pop
  IL_4989:  dup
  IL_498a:  ldc.i4.1
  IL_498b:  or
  IL_498c:  pop
  IL_4991:  dup
  IL_4992:  ldc.i4.6
  IL_4993:  add
  IL_4994:  pop
  IL_4999:  dup
  IL_499a:  ldc.i4.7
  IL_499b:  mul
  IL_499c:  pop
  IL_49a1:  dup
  IL_49a2:  ldc.i4.4
  IL_49a3:  shr
  IL_49a4:  pop
  IL_49a9:  dup
  IL_49aa:  ldc.i4.0
  IL_49ab:  or
  IL_49ac:  pop
  IL_49b1:  dup
  IL_49b2:  ldc.i4.3
  IL_49b3:  sub
  IL_49b4:  pop
  IL_49b9:  dup
  IL_49ba:  ldc.i4.4
  IL_49bb:  xor
  IL_49bc:  pop
  IL_49c1:  dup
  IL_49c2:  ldc.i4.7
  IL_49c3:  mul
  IL_49c4:  pop
  IL_49c9:  dup
  IL_49ca:  ldc.i4.8
  IL_49cb:  or
  IL_49cc:  pop
  IL_49d1:  dup
  IL_49d2:  ldc.i4.6
  IL_49d3:  shl
  IL_49d4:  pop
  IL_49d9:  dup
  IL_49da:  ldc.i4.3
  IL_49db:  add
  IL_49dc:  pop
  IL_49e1:  dup
  IL_49e2:  ldc.i4.4
  IL_49e3:  add
  IL_49e4:  pop
  IL_49e9:  dup
  IL_49ea:  ldc.i4.8
  IL_49eb:  shl
  IL_49ec:  pop
  IL_49f1:  dup
  IL_49f2:  ldc.i4.8
  IL_49f3:  or
  IL_49f4:  pop
  IL_49f9:  dup
  IL_49fa:  ldc.i4.4
  IL_49fb:  mul
  IL_49fc:  pop
  IL_4a01:  dup
  IL_4a02:  ldc.i4.1
  IL_4a03:  shl
  IL_4a04:  pop
  IL_4a09:  dup
  IL_4a0a:  ldc.i4.1
  IL_4a0b:  and
  IL_4a0c:  pop
  IL_4a11:  dup
  IL_4a12:  ldc.i4.6
  IL_4a13:  or
  IL_4a14:  pop
  IL_4a19:  dup
  IL_4a1a:  ldc.i4.3
  IL_4a1b:  or
  IL_4a1c:  pop
  IL_4a21:  dup
  IL_4a22:  ldc.i4.1
  IL_4a23:  and
  IL_4a24:  pop
  IL_4a29:  dup
  IL_4a2a:  ldc.i4.5
  IL_4a2b:  shr
  IL_4a2c:  pop
  IL_4a31:  dup
  IL_4a32:  ldc.i4.2
  IL_4a33:  mul
  IL_4a34:  pop
  IL_4a39:  dup
  IL_4a3a:  ldc.i4.6
  IL_4a3b:  and
  IL_4a3c:  pop
  IL_4a41:  dup
  IL_4a42:  ldc.i4.6
  IL_4a43:  and
  IL_4a44:  pop
  IL_4a49:  dup
  IL_4a4a:  ldc.i4.0
  IL_4a4b:  or
  IL_4a4c:  pop
  IL_4a51:  dup
  IL_4a52:  ldc.i4.1
  IL_4a53:  or
  IL_4a54:  pop
  IL_4a59:  dup
  IL_4a5a:  ldc.i4.8
  IL_4a5b:  and
  IL_4a5c:  pop
  IL_4a61:  dup
  IL_4a62:  ldc.i4.4
  IL_4a63:  add
  IL_4a64:  pop
  IL_4a69:  dup
  IL_4a6a:  ldc.i4.4
  IL_4a6b:  shr
  IL_4a6c:  pop
  IL_4a71:  dup
  IL_4a72:  ldc.i4.1
  IL_4a73:  sub
  IL_4a74:  pop
  IL_4a79:  dup
  IL_4a7a:  ldc.i4.3
  IL_4a7b:  shl
  IL_4a7c:  pop
  IL_4a81:  dup
  IL_4a82:  ldc.i4.2
  IL_4a83:  shr
  IL_4a84:  pop
  IL_4a89:  dup
  IL_4a8a:  ldc.i4.7
  IL_4a8b:  and
  IL_4a8c:  pop
  IL_4a91:  dup
  IL_4a92:  ldc.i4.0
  IL_4a93:  shr
  IL_4a94:  pop
  IL_4a99:  dup
  IL_4a9a:  ldc.i4.6
  IL_4a9b:  or
  IL_4a9c:  pop
  IL_4aa1:  dup
  IL_4aa2:  ldc.i4.8
  IL_4aa3:  add
  IL_4aa4:  pop
  IL_4aa9:  dup
  IL_4aaa:  ldc.i4.6
  IL_4aab:  and
  IL_4aac:  pop
  IL_4ab1:  dup
  IL_4ab2:  ldc.i4.8
  IL_4ab3:  shr
  IL_4ab4:  pop
  IL_4ab9:  dup
  IL_4aba:  ldc.i4.7
  IL_4abb:  sub
  IL_4abc:  pop
  IL_4ac1:  dup
  IL_4ac2:  ldc.i4.2
  IL_4ac3:  sub
  IL_4ac4:  pop
  IL_4ac9:  dup
  IL_4aca:  ldc.i4.7
  IL_4acb:  sub
  IL_4acc:  pop
  IL_4ad1:  dup
  IL_4ad2:  ldc.i4.8
  IL_4ad3:  xor
  IL_4ad4:  pop
  IL_4ad9:  dup
  IL_4ada:  ldc.i4.1
  IL_4adb:  sub
  IL_4adc:  pop
  IL_4ae1:  dup
  IL_4ae2:  ldc.i4.5
  IL_4ae3:  xor
  IL_4ae4:  pop
  IL_4ae9:  dup
  IL_4aea:  ldc.i4.3
  IL_4aeb:  shl
  IL_4aec:  pop
  IL_4af1:  dup
  IL_4af2:  ldc.i4.3
  IL_4af3:  shr
  IL_4af4:  pop
  IL_4af9:  dup
  IL_4afa:  ldc.i4.7
  IL_4afb:  add
  IL_4afc:  pop
  IL_4b01:  dup
  IL_4b02:  ldc.i4.4
  IL_4b03:  add
  IL_4b04:  pop
  IL_4b09:  dup
  IL_4b0a:  ldc.i4.3
  IL_4b0b:  shl
  IL_4b0c:  pop
  IL_4b11:  dup
  IL_4b12:  ldc.i4.4
  IL_4b13:  or
  IL_4b14:  pop
  IL_4b19:  dup
  IL_4b1a:  ldc.i4.7
  IL_4b1b:  add
  IL_4b1c:  pop
  IL_4b21:  dup
  IL_4b22:  ldc.i4.1
  IL_4b23:  sub
  IL_4b24:  pop
  IL_4b29:  dup
  IL_4b2a:  ldc.i4.0
  IL_4b2b:  add
  IL_4b2c:  pop
  IL_4b31:  dup
  IL_4b32:  ldc.i4.2
  IL_4b33:  add
  IL_4b34:  pop
  IL_4b39:  dup
  IL_4b3a:  ldc.i4.7
  IL_4b3b:  shr
  IL_4b3c:  pop
  IL_4b41:  dup
  IL_4b42:  ldc.i4.2
  IL_4b43:  mul
  IL_4b44:  pop
  IL_4b49:  dup
  IL_4b4a:  ldc.i4.3
  IL_4b4b:  or
  IL_4b4c:  pop
  IL_4b51:  dup
  IL_4b52:  ldc.i4.2
  IL_4b53:  mul
  IL_4b54:  pop
  IL_4b59:  dup
  IL_4b5a:  ldc.i4.5
  IL_4b5b:  and
  IL_4b5c:  pop
  IL_4b61:  dup
  IL_4b62:  ldc.i4.5
  IL_4b63:  shr
  IL_4b64:  pop
  IL_4b69:  dup
  IL_4b6a:  ldc.i4.2
  IL_4b6b:  shl
  IL_4b6c:  pop
  IL_4b71:  dup
  IL_4b72:  ldc.i4.2
  IL_4b73:  add
  IL_4b74:  pop
  IL_4b79:  dup
  IL_4b7a:  ldc.i4.4
  IL_4b7b:  sub
  IL_4b7c:  pop
  IL_4b81:  dup
  IL_4b82:  ldc.i4.1
  IL_4b83:  shr
  IL_4b84:  pop
  IL_4b89:  dup
  IL_4b8a:  ldc.i4.3
  IL_4b8b:  or
  IL_4b8c:  pop
  IL_4b91:  dup
  IL_4b92:  ldc.i4.2
  IL_4b93:  mul
  IL_4b94:  pop
  IL_4b99:  dup
  IL_4b9a:  ldc.i4.5
  IL_4b9b:  mul
  IL_4b9c:  pop
  IL_4ba1:  dup
  IL_4ba2:  ldc.i4.3
  IL_4ba3:  and
  IL_4ba4:  pop
  IL_4ba9:  dup
  IL_4baa:  ldc.i4.8
  IL_4bab:  and
  IL_4bac:  pop
  IL_4bb1:  dup
  IL_4bb2:  ldc.i4.3
  IL_4bb3:  add
  IL_4bb4:  pop
  IL_4bb9:  dup
  IL_4bba:  ldc.i4.7
  IL_4bbb:  shl
  IL_4bbc:  pop
  IL_4bc1:  dup
  IL_4bc2:  ldc.i4.5
  IL_4bc3:  shl
  IL_4bc4:  pop
  IL_4bc9:  dup
  IL_4bca:  ldc.i4.2
  IL_4bcb:  add
  IL_4bcc:  pop
  IL_4bd1:  dup
  IL_4bd2:  ldc.i4.2
  IL_4bd3:  and
  IL_4bd4:  pop
  IL_4bd9:  dup
  IL_4bda:  ldc.i4.6
  IL_4bdb:  sub
  IL_4bdc:  pop
  IL_4be1:  dup
  IL_4be2:  ldc.i4.4
  IL_4be3:  shr
  IL_4be4:  pop
  IL_4be9:  dup
  IL_4bea:  ldc.i4.8
  IL_4beb:  and
  IL_4bec:  pop
  IL_4bf1:  dup
  IL_4bf2:  ldc.i4.6
  IL_4bf3:  sub
  IL_4bf4:  pop
  IL_4bf9:  dup
  IL_4bfa:  ldc.i4.7
  IL_4bfb:  add
  IL_4bfc:  pop
  IL_4c01:  dup
  IL_4c02:  ldc.i4.5
  IL_4c03:  and
  IL_4c04:  pop
  IL_4c09:  dup
  IL_4c0a:  ldc.i4.7
  IL_4c0b:  shl
  IL_4c0c:  pop
  IL_4c11:  dup
  IL_4c12:  ldc.i4.6
  IL_4c13:  and
  IL_4c14:  pop
  IL_4c19:  dup
  IL_4c1a:  ldc.i4.3
  IL_4c1b:  add
  IL_4c1c:  pop
  IL_4c21:  dup
  IL_4c22:  ldc.i4.6
  IL_4c23:  add
  IL_4c24:  pop
  IL_4c29:  dup
  IL_4c2a:  ldc.i4.5
  IL_4c2b:  shl
  IL_4c2c:  pop
  IL_4c31:  dup
  IL_4c32:  ldc.i4.2
  IL_4c33:  and
  IL_4c34:  pop
  IL_4c39:  dup
  IL_4c3a:  ldc.i4.2
  IL_4c3b:  shl
  IL_4c3c:  pop
  IL_4c41:  dup
  IL_4c42:  ldc.i4.7
  IL_4c43:  shr
  IL_4c44:  pop
  IL_4c49:  dup
  IL_4c4a:  ldc.i4.8
  IL_4c4b:  shr
  IL_4c4c:  pop
  IL_4c51:  dup
  IL_4c52:  ldc.i4.7
  IL_4c53:  xor
  IL_4c54:  pop
  IL_4c59:  dup
  IL_4c5a:  ldc.i4.8
  IL_4c5b:  xor
  IL_4c5c:  pop
  IL_4c61:  dup
  IL_4c62:  ldc.i4.8
  IL_4c63:  sub
  IL_4c64:  pop
  IL_4c69:  dup
  IL_4c6a:  ldc.i4.4
  IL_4c6b:  shr
  IL_4c6c:  pop
  IL_4c71:  dup
  IL_4c72:  ldc.i4.3
  IL_4c73:  and
  IL_4c74:  pop
  IL_4c79:  dup
  IL_4c7a:  ldc.i4.6
  IL_4c7b:  sub
  IL_4c7c:  pop
  IL_4c81:  dup
  IL_4c82:  ldc.i4.8
  IL_4c83:  or
  IL_4c84:  pop
  IL_4c89:  dup
  IL_4c8a:  ldc.i4.1
  IL_4c8b:  shl
  IL_4c8c:  pop
  IL_4c91:  dup
  IL_4c92:  ldc.i4.1
  IL_4c93:  mul
  IL_4c94:  pop
  IL_4c99:  dup
  IL_4c9a:  ldc.i4.7
  IL_4c9b:  and
  IL_4c9c:  pop
  IL_4ca1:  dup
  IL_4ca2:  ldc.i4.1
  IL_4ca3:  mul
  IL_4ca4:  pop
  IL_4ca9:  dup
  IL_4caa:  ldc.i4.2
  IL_4cab:  sub
  IL_4cac:  pop
  IL_4cb1:  dup
  IL_4cb2:  ldc.i4.1
  IL_4cb3:  sub
  IL_4cb4:  pop
  IL_4cb9:  dup
  IL_4cba:  ldc.i4.8
  IL_4cbb:  shr
  IL_4cbc:  pop
  IL_4cc1:  dup
  IL_4cc2:  ldc.i4.2
  IL_4cc3:  xor
  IL_4cc4:  pop
  IL_4cc9:  dup
  IL_4cca:  ldc.i4.8
  IL_4ccb:  or
  IL_4ccc:  pop
  IL_4cd1:  dup
  IL_4cd2:  ldc.i4.2
  IL_4cd3:  add
  IL_4cd4:  pop
  IL_4cd9:  dup
  IL_4cda:  ldc.i4.2
  IL_4cdb:  sub
  IL_4cdc:  pop
  IL_4ce1:  dup
  IL_4ce2:  ldc.i4.8
  IL_4ce3:  xor
  IL_4ce4:  pop
  IL_4ce9:  dup
  IL_4cea:  ldc.i4.5
  IL_4ceb:  shl
  IL_4cec:  pop
  IL_4cf1:  dup
  IL_4cf2:  ldc.i4.3
  IL_4cf3:  shr
  IL_4cf4:  pop
  IL_4cf9:  dup
  IL_4cfa:  ldc.i4.6
  IL_4cfb:  or
  IL_4cfc:  pop
  IL_4d01:  dup
  IL_4d02:  ldc.i4.3
  IL_4d03:  xor
  IL_4d04:  pop
  IL_4d09:  dup
  IL_4d0a:  ldc.i4.4
  IL_4d0b:  mul
  IL_4d0c:  pop
  IL_4d11:  dup
  IL_4d12:  ldc.i4.5
  IL_4d13:  shr
  IL_4d14:  pop
  IL_4d19:  dup
  IL_4d1a:  ldc.i4.3
  IL_4d1b:  shl
  IL_4d1c:  pop
  IL_4d21:  dup
  IL_4d22:  ldc.i4.7
  IL_4d23:  sub
  IL_4d24:  pop
  IL_4d29:  dup
  IL_4d2a:  ldc.i4.4
  IL_4d2b:  shl
  IL_4d2c:  pop
  IL_4d31:  dup
  IL_4d32:  ldc.i4.1
  IL_4d33:  shr
  IL_4d34:  pop
  IL_4d39:  dup
  IL_4d3a:  ldc.i4.1
  IL_4d3b:  and
  IL_4d3c:  pop
  IL_4d41:  dup
  IL_4d42:  ldc.i4.1
  IL_4d43:  and
  IL_4d44:  pop
  IL_4d49:  dup
  IL_4d4a:  ldc.i4.3
  IL_4d4b:  or
  IL_4d4c:  pop
  IL_4d51:  dup
  IL_4d52:  ldc.i4.7
  IL_4d53:  shr
  IL_4d54:  pop
  IL_4d59:  dup
  IL_4d5a:  ldc.i4.8
  IL_4d5b:  or
  IL_4d5c:  pop
  IL_4d61:  dup
  IL_4d62:  ldc.i4.5
  IL_4d63:  add
  IL_4d64:  pop
  IL_4d69:  dup
  IL_4d6a:  ldc.i4.5
  IL_4d6b:  shr
  IL_4d6c:  pop
  IL_4d71:  dup
  IL_4d72:  ldc.i4.5
  IL_4d73:  mul
  IL_4d74:  pop
  IL_4d79:  dup
  IL_4d7a:  ldc.i4.3
  IL_4d7b:  xor
  IL_4d7c:  pop
  IL_4d81:  dup
  IL_4d82:  ldc.i4.4
  IL_4d83:  xor
  IL_4d84:  pop
  IL_4d89:  dup
  IL_4d8a:  ldc.i4.7
  IL_4d8b:  shr
  IL_4d8c:  pop
  IL_4d91:  dup
  IL_4d92:  ldc.i4.5
  IL_4d93:  shl
  IL_4d94:  pop
  IL_4d99:  dup
  IL_4d9a:  ldc.i4.0
  IL_4d9b:  shl
  IL_4d9c:  pop
  IL_4da1:  dup
  IL_4da2:  ldc.i4.0
  IL_4da3:  mul
  IL_4da4:  pop
  IL_4da9:  dup
  IL_4daa:  ldc.i4.1
  IL_4dab:  add
  IL_4dac:  pop
  IL_4db1:  dup
  IL_4db2:  ldc.i4.4
  IL_4db3:  shr
  IL_4db4:  pop
  IL_4db9:  dup
  IL_4dba:  ldc.i4.8
  IL_4dbb:  mul
  IL_4dbc:  pop
  IL_4dc1:  dup
  IL_4dc2:  ldc.i4.2
  IL_4dc3:  and
  IL_4dc4:  pop
  IL_4dc9:  dup
  IL_4dca:  ldc.i4.3
  IL_4dcb:  shr
  IL_4dcc:  pop
  IL_4dd1:  dup
  IL_4dd2:  ldc.i4.3
  IL_4dd3:  and
  IL_4dd4:  pop
  IL_4dd9:  dup
  IL_4dda:  ldc.i4.0
  IL_4ddb:  shr
  IL_4ddc:  pop
  IL_4de1:  dup
  IL_4de2:  ldc.i4.5
  IL_4de3:  shl
  IL_4de4:  pop
  IL_4de9:  dup
  IL_4dea:  ldc.i4.6
  IL_4deb:  shl
  IL_4dec:  pop
  IL_4df1:  dup
  IL_4df2:  ldc.i4.0
  IL_4df3:  shr
  IL_4df4:  pop
  IL_4df9:  dup
  IL_4dfa:  ldc.i4.8
  IL_4dfb:  xor
  IL_4dfc:  pop
  IL_4e01:  dup
  IL_4e02:  ldc.i4.7
  IL_4e03:  add
  IL_4e04:  pop
  IL_4e09:  dup
  IL_4e0a:  ldc.i4.8
  IL_4e0b:  shr
  IL_4e0c:  pop
  IL_4e11:  dup
  IL_4e12:  ldc.i4.7
  IL_4e13:  xor
  IL_4e14:  pop
  IL_4e19:  dup
  IL_4e1a:  ldc.i4.1
  IL_4e1b:  shl
  IL_4e1c:  pop
  IL_4e21:  dup
  IL_4e22:  ldc.i4.6
  IL_4e23:  and
  IL_4e24:  pop
  IL_4e29:  dup
  IL_4e2a:  ldc.i4.8
  IL_4e2b:  xor
  IL_4e2c:  pop
  IL_4e31:  dup
  IL_4e32:  ldc.i4.4
  IL_4e33:  and
  IL_4e34:  pop
  IL_4e39:  dup
  IL_4e3a:  ldc.i4.3
  IL_4e3b:  or
  IL_4e3c:  pop
  IL_4e41:  dup
  IL_4e42:  ldc.i4.0
  IL_4e43:  mul
  IL_4e44:  pop
  IL_4e49:  dup
  IL_4e4a:  ldc.i4.3
  IL_4e4b:  or
  IL_4e4c:  pop
  IL_4e51:  dup
  IL_4e52:  ldc.i4.8
  IL_4e53:  shr
  IL_4e54:  pop
  IL_4e59:  dup
  IL_4e5a:  ldc.i4.0
  IL_4e5b:  and
  IL_4e5c:  pop
  IL_4e61:  dup
  IL_4e62:  ldc.i4.3
  IL_4e63:  sub
  IL_4e64:  pop
  IL_4e69:  dup
  IL_4e6a:  ldc.i4.1
  IL_4e6b:  or
  IL_4e6c:  pop
  IL_4e71:  dup
  IL_4e72:  ldc.i4.8
  IL_4e73:  shl
  IL_4e74:  pop
  IL_4e79:  dup
  IL_4e7a:  ldc.i4.4
  IL_4e7b:  xor
  IL_4e7c:  pop
  IL_4e81:  dup
  IL_4e82:  ldc.i4.3
  IL_4e83:  and
  IL_4e84:  pop
  IL_4e89:  dup
  IL_4e8a:  ldc.i4.2
  IL_4e8b:  xor
  IL_4e8c:  pop
  IL_4e91:  dup
  IL_4e92:  ldc.i4.6
  IL_4e93:  sub
  IL_4e94:  pop
  IL_4e99:  dup
  IL_4e9a:  ldc.i4.4
  IL_4e9b:  or
  IL_4e9c:  pop
  IL_4ea1:  dup
  IL_4ea2:  ldc.i4.4
  IL_4ea3:  and
  IL_4ea4:  pop
  IL_4ea9:  dup
  IL_4eaa:  ldc.i4.8
  IL_4eab:  shl
  IL_4eac:  pop
  IL_4eb1:  dup
  IL_4eb2:  ldc.i4.7
  IL_4eb3:  shl
  IL_4eb4:  pop
  IL_4eb9:  dup
  IL_4eba:  ldc.i4.2
  IL_4ebb:  shr
  IL_4ebc:  pop
  IL_4ec1:  dup
  IL_4ec2:  ldc.i4.2
  IL_4ec3:  and
  IL_4ec4:  pop
  IL_4ec9:  dup
  IL_4eca:  ldc.i4.3
  IL_4ecb:  and
  IL_4ecc:  pop
  IL_4ed1:  dup
  IL_4ed2:  ldc.i4.2
  IL_4ed3:  add
  IL_4ed4:  pop
  IL_4ed9:  dup
  IL_4eda:  ldc.i4.4
  IL_4edb:  add
  IL_4edc:  pop
  IL_4ee1:  dup
  IL_4ee2:  ldc.i4.8
  IL_4ee3:  sub
  IL_4ee4:  pop
  IL_4ee9:  dup
  IL_4eea:  ldc.i4.3
  IL_4eeb:  shr
  IL_4eec:  pop
  IL_4ef1:  dup
  IL_4ef2:  ldc.i4.2
  IL_4ef3:  sub
  IL_4ef4:  pop
  IL_4ef9:  dup
  IL_4efa:  ldc.i4.6
  IL_4efb:  add
  IL_4efc:  pop
  IL_4f01:  dup
  IL_4f02:  ldc.i4.2
  IL_4f03:  shl
  IL_4f04:  pop
  IL_4f09:  dup
  IL_4f0a:  ldc.i4.0
  IL_4f0b:  shr
  IL_4f0c:  pop
  IL_4f11:  dup
  IL_4f12:  ldc.i4.5
  IL_4f13:  or
  IL_4f14:  pop
  IL_4f19:  dup
  IL_4f1a:  ldc.i4.1
  IL_4f1b:  and
  IL_4f1c:  pop
  IL_4f21:  dup
  IL_4f22:  ldc.i4.7
  IL_4f23:  xor
  IL_4f24:  pop
  IL_4f29:  dup
  IL_4f2a:  ldc.i4.3
  IL_4f2b:  sub
  IL_4f2c:  pop
  IL_4f31:  dup
  IL_4f32:  ldc.i4.7
  IL_4f33:  shl
  IL_4f34:  pop
  IL_4f39:  dup
  IL_4f3a:  ldc.i4.8
  IL_4f3b:  and
  IL_4f3c:  pop
  IL_4f41:  dup
  IL_4f42:  ldc.i4.7
  IL_4f43:  xor
  IL_4f44:  pop
  IL_4f49:  dup
  IL_4f4a:  ldc.i4.6
  IL_4f4b:  sub
  IL_4f4c:  pop
  IL_4f51:  dup
  IL_4f52:  ldc.i4.1
  IL_4f53:  shr
  IL_4f54:  pop
  IL_4f59:  dup
  IL_4f5a:  ldc.i4.0
  IL_4f5b:  xor
  IL_4f5c:  pop
  IL_4f61:  dup
  IL_4f62:  ldc.i4.2
  IL_4f63:  sub
  IL_4f64:  pop
  IL_4f69:  dup
  IL_4f6a:  ldc.i4.4
  IL_4f6b:  shl
  IL_4f6c:  pop
  IL_4f71:  dup
  IL_4f72:  ldc.i4.7
  IL_4f73:  and
  IL_4f74:  pop
  IL_4f79:  dup
  IL_4f7a:  ldc.i4.5
  IL_4f7b:  mul
  IL_4f7c:  pop
  IL_4f81:  dup
  IL_4f82:  ldc.i4.0
  IL_4f83:  and
  IL_4f84:  pop
  IL_4f89:  dup
  IL_4f8a:  ldc.i4.2
  IL_4f8b:  mul
  IL_4f8c:  pop
  IL_4f91:  dup
  IL_4f92:  ldc.i4.4
  IL_4f93:  shl
  IL_4f94:  pop
  IL_4f99:  dup
  IL_4f9a:  ldc.i4.0
  IL_4f9b:  mul
  IL_4f9c:  pop
  IL_4fa1:  dup
  IL_4fa2:  ldc.i4.5
  IL_4fa3:  shl
  IL_4fa4:  pop
  IL_4fa9:  dup
  IL_4faa:  ldc.i4.8
  IL_4fab:  and
  IL_4fac:  pop
  IL_4fb1:  dup
  IL_4fb2:  ldc.i4.6
  IL_4fb3:  sub
  IL_4fb4:  pop
  IL_4fb9:  dup
  IL_4fba:  ldc.i4.2
  IL_4fbb:  xor
  IL_4fbc:  pop
  IL_4fc1:  dup
  IL_4fc2:  ldc.i4.2
  IL_4fc3:  shl
  IL_4fc4:  pop
  IL_4fc9:  dup
  IL_4fca:  ldc.i4.7
  IL_4fcb:  and
  IL_4fcc:  pop
  IL_4fd1:  dup
  IL_4fd2:  ldc.i4.4
  IL_4fd3:  shl
  IL_4fd4:  pop
  IL_4fd9:  dup
  IL_4fda:  ldc.i4.8
  IL_4fdb:  shl
  IL_4fdc:  pop
  IL_4fe1:  dup
  IL_4fe2:  ldc.i4.4
  IL_4fe3:  shr
  IL_4fe4:  pop
  IL_4fe9:  dup
  IL_4fea:  ldc.i4.3
  IL_4feb:  xor
  IL_4fec:  pop
  IL_4ff1:  dup
  IL_4ff2:  ldc.i4.3
  IL_4ff3:  shr
  IL_4ff4:  pop
  IL_4ff9:  dup
  IL_4ffa:  ldc.i4.8
  IL_4ffb:  add
  IL_4ffc:  pop
  IL_5001:  dup
  IL_5002:  ldc.i4.7
  IL_5003:  add
  IL_5004:  pop
  IL_5009:  dup
  IL_500a:  ldc.i4.1
  IL_500b:  or
  IL_500c:  pop
  IL_5011:  dup
  IL_5012:  ldc.i4.2
  IL_5013:  shl
  IL_5014:  pop
  IL_5019:  dup
  IL_501a:  ldc.i4.2
  IL_501b:  mul
  IL_501c:  pop
  IL_5021:  dup
  IL_5022:  ldc.i4.3
  IL_5023:  xor
  IL_5024:  pop
  IL_5029:  dup
  IL_502a:  ldc.i4.5
  IL_502b:  shl
  IL_502c:  pop
  IL_5031:  dup
  IL_5032:  ldc.i4.6
  IL_5033:  mul
  IL_5034:  pop
  IL_5039:  dup
  IL_503a:  ldc.i4.6
  IL_503b:  add
  IL_503c:  pop
  IL_5041:  dup
  IL_5042:  ldc.i4.7
  IL_5043:  add
  IL_5044:  pop
  IL_5049:  dup
  IL_504a:  ldc.i4.2
  IL_504b:  mul
  IL_504c:  pop
  IL_5051:  dup
  IL_5052:  ldc.i4.8
  IL_5053:  and
  IL_5054:  pop
  IL_5059:  dup
  IL_505a:  ldc.i4.7
  IL_505b:  shl
  IL_505c:  pop
  IL_5061:  dup
  IL_5062:  ldc.i4.8
  IL_5063:  shr
  IL_5064:  pop
  IL_5069:  dup
  IL_506a:  ldc.i4.8
  IL_506b:  and
  IL_506c:  pop
  IL_5071:  dup
  IL_5072:  ldc.i4.0
  IL_5073:  shl
  IL_5074:  pop
  IL_5079:  dup
  IL_507a:  ldc.i4.7
  IL_507b:  or
  IL_507c:  pop
  IL_5081:  dup
  IL_5082:  ldc.i4.5
  IL_5083:  or
  IL_5084:  pop
  IL_5089:  dup
  IL_508a:  ldc.i4.8
  IL_508b:  shr
  IL_508c:  pop
  IL_5091:  dup
  IL_5092:  ldc.i4.0
  IL_5093:  xor
  IL_5094:  pop
  IL_5099:  dup
  IL_509a:  ldc.i4.1
  IL_509b:  shr
  IL_509c:  pop
  IL_50a1:  dup
  IL_50a2:  ldc.i4.2
  IL_50a3:  xor
  IL_50a4:  pop
  IL_50a9:  dup
  IL_50aa:  ldc.i4.3
  IL_50ab:  add
  IL_50ac:  pop
  IL_50b1:  dup
  IL_50b2:  ldc.i4.8
  IL_50b3:  mul
  IL_50b4:  pop
  IL_50b9:  dup
  IL_50ba:  ldc.i4.7
  IL_50bb:  sub
  IL_50bc:  pop
  IL_50c1:  dup
  IL_50c2:  ldc.i4.8
  IL_50c3:  xor
  IL_50c4:  pop
  IL_50c9:  dup
  IL_50ca:  ldc.i4.5
  IL_50cb:  mul
  IL_50cc:  pop
  IL_50d1:  dup
  IL_50d2:  ldc.i4.3
  IL_50d3:  shr
  IL_50d4:  pop
  IL_50d9:  dup
  IL_50da:  ldc.i4.0
  IL_50db:  add
  IL_50dc:  pop
  IL_50e1:  dup
  IL_50e2:  ldc.i4.1
  IL_50e3:  shl
  IL_50e4:  pop
  IL_50e9:  dup
  IL_50ea:  ldc.i4.8
  IL_50eb:  shr
  IL_50ec:  pop
  IL_50f1:  dup
  IL_50f2:  ldc.i4.5
  IL_50f3:  xor
  IL_50f4:  pop
  IL_50f9:  dup
  IL_50fa:  ldc.i4.2
  IL_50fb:  and
  IL_50fc:  pop
  IL_5101:  dup
  IL_5102:  ldc.i4.5
  IL_5103:  shr
  IL_5104:  pop
  IL_5109:  dup
  IL_510a:  ldc.i4.0
  IL_510b:  or
  IL_510c:  pop
  IL_5111:  dup
  IL_5112:  ldc.i4.2
  IL_5113:  or
  IL_5114:  pop
  IL_5119:  dup
  IL_511a:  ldc.i4.8
  IL_511b:  xor
  IL_511c:  pop
  IL_5121:  dup
  IL_5122:  ldc.i4.8
  IL_5123:  shl
  IL_5124:  pop
  IL_5129:  dup
  IL_512a:  ldc.i4.0
  IL_512b:  shl
  IL_512c:  pop
  IL_5131:  dup
  IL_5132:  ldc.i4.4
  IL_5133:  and
  IL_5134:  pop
  IL_5139:  dup
  IL_513a:  ldc.i4.5
  IL_513b:  shr
  IL_513c:  pop
  IL_5141:  dup
  IL_5142:  ldc.i4.7
  IL_5143:  shr
  IL_5144:  pop
  IL_5149:  dup
  IL_514a:  ldc.i4.7
  IL_514b:  shr
  IL_514c:  pop
  IL_5151:  dup
  IL_5152:  ldc.i4.0
  IL_5153:  xor
  IL_5154:  pop
  IL_5159:  dup
  IL_515a:  ldc.i4.4
  IL_515b:  or
  IL_515c:  pop
  IL_5161:  dup
  IL_5162:  ldc.i4.7
  IL_5163:  or
  IL_5164:  pop
  IL_5169:  dup
  IL_516a:  ldc.i4.4
  IL_516b:  shl
  IL_516c:  pop
  IL_5171:  dup
  IL_5172:  ldc.i4.3
  IL_5173:  xor
  IL_5174:  pop
  IL_5179:  dup
  IL_517a:  ldc.i4.0
  IL_517b:  and
  IL_517c:  pop
  IL_5181:  dup
  IL_5182:  ldc.i4.1
  IL_5183:  shl
  IL_5184:  pop
  IL_5189:  dup
  IL_518a:  ldc.i4.3
  IL_518b:  mul
  IL_518c:  pop
  IL_5191:  dup
  IL_5192:  ldc.i4.2
  IL_5193:  or
  IL_5194:  pop
  IL_5199:  dup
  IL_519a:  ldc.i4.7
  IL_519b:  add
  IL_519c:  pop
  IL_51a1:  dup
  IL_51a2:  ldc.i4.3
  IL_51a3:  add
  IL_51a4:  pop
  IL_51a9:  dup
  IL_51aa:  ldc.i4.3
  IL_51ab:  or
  IL_51ac:  pop
  IL_51b1:  dup
  IL_51b2:  ldc.i4.5
  IL_51b3:  shl
  IL_51b4:  pop
  IL_51b9:  dup
  IL_51ba:  ldc.i4.5
  IL_51bb:  mul
  IL_51bc:  pop
  IL_51c1:  dup
  IL_51c2:  ldc.i4.8
  IL_51c3:  mul
  IL_51c4:  pop
  IL_51c9:  dup
  IL_51ca:  ldc.i4.7
  IL_51cb:  or
  IL_51cc:  pop
  IL_51d1:  dup
  IL_51d2:  ldc.i4.5
  IL_51d3:  shl
  IL_51d4:  pop
  IL_51d9:  dup
  IL_51da:  ldc.i4.6
  IL_51db:  add
  IL_51dc:  pop
  IL_51e1:  dup
  IL_51e2:  ldc.i4.0
  IL_51e3:  sub
  IL_51e4:  pop
  IL_51e9:  dup
  IL_51ea:  ldc.i4.0
  IL_51eb:  xor
  IL_51ec:  pop
  IL_51f1:  dup
  IL_51f2:  ldc.i4.4
  IL_51f3:  xor
  IL_51f4:  pop
  IL_51f9:  dup
  IL_51fa:  ldc.i4.0
  IL_51fb:  shl
  IL_51fc:  pop
  IL_5201:  dup
  IL_5202:  ldc.i4.4
  IL_5203:  mul
  IL_5204:  pop
  IL_5209:  dup
  IL_520a:  ldc.i4.3
  IL_520b:  or
  IL_520c:  pop
  IL_5211:  dup
  IL_5212:  ldc.i4.3
  IL_5213:  mul
  IL_5214:  pop
  IL_5219:  dup
  IL_521a:  ldc.i4.3
  IL_521b:  and
  IL_521c:  pop
  IL_5221:  dup
  IL_5222:  ldc.i4.7
  IL_5223:  and
  IL_5224:  pop
  IL_5229:  dup
  IL_522a:  ldc.i4.2
  IL_522b:  or
  IL_522c:  pop
  IL_5231:  dup
  IL_5232:  ldc.i4.0
  IL_5233:  shr
  IL_5234:  pop
  IL_5239:  dup
  IL_523a:  ldc.i4.2
  IL_523b:  mul
  IL_523c:  pop
  IL_5241:  dup
  IL_5242:  ldc.i4.2
  IL_5243:  sub
  IL_5244:  pop
  IL_5249:  dup
  IL_524a:  ldc.i4.5
  IL_524b:  sub
  IL_524c:  pop
  IL_5251:  dup
  IL_5252:  ldc.i4.5
  IL_5253:  add
  IL_5254:  pop
  IL_5259:  dup
  IL_525a:  ldc.i4.0
  IL_525b:  shl
  IL_525c:  pop
  IL_5261:  dup
  IL_5262:  ldc.i4.7
  IL_5263:  mul
  IL_5264:  pop
  IL_5269:  dup
  IL_526a:  ldc.i4.1
  IL_526b:  add
  IL_526c:  pop
  IL_5271:  dup
  IL_5272:  ldc.i4.7
  IL_5273:  xor
  IL_5274:  pop
  IL_5279:  dup
  IL_527a:  ldc.i4.4
  IL_527b:  add
  IL_527c:  pop
  IL_5281:  dup
  IL_5282:  ldc.i4.0
  IL_5283:  or
  IL_5284:  pop
  IL_5289:  dup
  IL_528a:  ldc.i4.3
  IL_528b:  mul
  IL_528c:  pop
  IL_5291:  dup
  IL_5292:  ldc.i4.1
  IL_5293:  shr
  IL_5294:  pop
  IL_5299:  dup
  IL_529a:  ldc.i4.8
  IL_529b:  shl
  IL_529c:  pop
  IL_52a1:  dup
  IL_52a2:  ldc.i4.7
  IL_52a3:  or
  IL_52a4:  pop
  IL_52a9:  dup
  IL_52aa:  ldc.i4.0
  IL_52ab:  and
  IL_52ac:  pop
  IL_52b1:  dup
  IL_52b2:  ldc.i4.2
  IL_52b3:  and
  IL_52b4:  pop
  IL_52b9:  dup
  IL_52ba:  ldc.i4.0
  IL_52bb:  xor
  IL_52bc:  pop
  IL_52c1:  dup
  IL_52c2:  ldc.i4.7
  IL_52c3:  and
  IL_52c4:  pop
  IL_52c9:  dup
  IL_52ca:  ldc.i4.0
  IL_52cb:  mul
  IL_52cc:  pop
  IL_52d1:  dup
  IL_52d2:  ldc.i4.7
  IL_52d3:  or
  IL_52d4:  pop
  IL_52d9:  dup
  IL_52da:  ldc.i4.8
  IL_52db:  mul
  IL_52dc:  pop
  IL_52e1:  dup
  IL_52e2:  ldc.i4.1
  IL_52e3:  and
  IL_52e4:  pop
  IL_52e9:  dup
  IL_52ea:  ldc.i4.0
  IL_52eb:  add
  IL_52ec:  pop
  IL_52f1:  dup
  IL_52f2:  ldc.i4.0
  IL_52f3:  mul
  IL_52f4:  pop
  IL_52f9:  dup
  IL_52fa:  ldc.i4.0
  IL_52fb:  xor
  IL_52fc:  pop
  IL_5301:  dup
  IL_5302:  ldc.i4.2
  IL_5303:  mul
  IL_5304:  pop
  IL_5309:  dup
  IL_530a:  ldc.i4.7
  IL_530b:  sub
  IL_530c:  pop
  IL_5311:  dup
  IL_5312:  ldc.i4.2
  IL_5313:  or
  IL_5314:  pop
  IL_5319:  dup
  IL_531a:  ldc.i4.1
  IL_531b:  shr
  IL_531c:  pop
  IL_5321:  dup
  IL_5322:  ldc.i4.1
  IL_5323:  or
  IL_5324:  pop
  IL_5329:  dup
  IL_532a:  ldc.i4.2
  IL_532b:  shl
  IL_532c:  pop
  IL_5331:  dup
  IL_5332:  ldc.i4.0
  IL_5333:  sub
  IL_5334:  pop
  IL_5339:  dup
  IL_533a:  ldc.i4.2
  IL_533b:  or
  IL_533c:  pop
  IL_5341:  dup
  IL_5342:  ldc.i4.2
  IL_5343:  and
  IL_5344:  pop
  IL_5349:  dup
  IL_534a:  ldc.i4.1
  IL_534b:  add
  IL_534c:  pop
  IL_5351:  dup
  IL_5352:  ldc.i4.3
  IL_5353:  sub
  IL_5354:  pop
  IL_5359:  dup
  IL_535a:  ldc.i4.1
  IL_535b:  sub
  IL_535c:  pop
  IL_5361:  dup
  IL_5362:  ldc.i4.8
  IL_5363:  add
  IL_5364:  pop
  IL_5369:  dup
  IL_536a:  ldc.i4.0
  IL_536b:  mul
  IL_536c:  pop
  IL_5371:  dup
  IL_5372:  ldc.i4.5
  IL_5373:  and
  IL_5374:  pop
  IL_5379:  dup
  IL_537a:  ldc.i4.4
  IL_537b:  xor
  IL_537c:  pop
  IL_5381:  dup
  IL_5382:  ldc.i4.8
  IL_5383:  shr
  IL_5384:  pop
  IL_5389:  dup
  IL_538a:  ldc.i4.5
  IL_538b:  add
  IL_538c:  pop
  IL_5391:  dup
  IL_5392:  ldc.i4.8
  IL_5393:  and
  IL_5394:  pop
  IL_5399:  dup
  IL_539a:  ldc.i4.1
  IL_539b:  and
  IL_539c:  pop
  IL_53a1:  dup
  IL_53a2:  ldc.i4.5
  IL_53a3:  xor
  IL_53a4:  pop
  IL_53a9:  dup
  IL_53aa:  ldc.i4.0
  IL_53ab:  and
  IL_53ac:  pop
  IL_53b1:  dup
  IL_53b2:  ldc.i4.5
  IL_53b3:  mul
  IL_53b4:  pop
  IL_53b9:  dup
  IL_53ba:  ldc.i4.2
  IL_53bb:  or
  IL_53bc:  pop
  IL_53c1:  dup
  IL_53c2:  ldc.i4.4
  IL_53c3:  add
  IL_53c4:  pop
  IL_53c9:  dup
  IL_53ca:  ldc.i4.7
  IL_53cb:  mul
  IL_53cc:  pop
  IL_53d1:  dup
  IL_53d2:  ldc.i4.1
  IL_53d3:  or
  IL_53d4:  pop
  IL_53d9:  dup
  IL_53da:  ldc.i4.0
  IL_53db:  add
  IL_53dc:  pop
  IL_53e1:  dup
  IL_53e2:  ldc.i4.0
  IL_53e3:  shr
  IL_53e4:  pop
  IL_53e9:  dup
  IL_53ea:  ldc.i4.2
  IL_53eb:  sub
  IL_53ec:  pop
  IL_53f1:  dup
  IL_53f2:  ldc.i4.8
  IL_53f3:  and
  IL_53f4:  pop
  IL_53f9:  dup
  IL_53fa:  ldc.i4.0
  IL_53fb:  mul
  IL_53fc:  pop
  IL_5401:  dup
  IL_5402:  ldc.i4.4
  IL_5403:  and
  IL_5404:  pop
  IL_5409:  dup
  IL_540a:  ldc.i4.5
  IL_540b:  mul
  IL_540c:  pop
  IL_5411:  dup
  IL_5412:  ldc.i4.5
  IL_5413:  mul
  IL_5414:  pop
  IL_5419:  dup
  IL_541a:  ldc.i4.8
  IL_541b:  xor
  IL_541c:  pop
  IL_5421:  dup
  IL_5422:  ldc.i4.5
  IL_5423:  mul
  IL_5424:  pop
  IL_5429:  dup
  IL_542a:  ldc.i4.4
  IL_542b:  or
  IL_542c:  pop
  IL_5431:  dup
  IL_5432:  ldc.i4.1
  IL_5433:  xor
  IL_5434:  pop
  IL_5439:  dup
  IL_543a:  ldc.i4.4
  IL_543b:  shl
  IL_543c:  pop
  IL_5441:  dup
  IL_5442:  ldc.i4.6
  IL_5443:  add
  IL_5444:  pop
  IL_5449:  dup
  IL_544a:  ldc.i4.6
  IL_544b:  shl
  IL_544c:  pop
  IL_5451:  dup
  IL_5452:  ldc.i4.0
  IL_5453:  sub
  IL_5454:  pop
  IL_5459:  dup
  IL_545a:  ldc.i4.3
  IL_545b:  add
  IL_545c:  pop
  IL_5461:  dup
  IL_5462:  ldc.i4.6
  IL_5463:  shl
  IL_5464:  pop
  IL_5469:  dup
  IL_546a:  ldc.i4.8
  IL_546b:  sub
  IL_546c:  pop
  IL_5471:  dup
  IL_5472:  ldc.i4.5
  IL_5473:  add
  IL_5474:  pop
  IL_5479:  dup
  IL_547a:  ldc.i4.8
  IL_547b:  shr
  IL_547c:  pop
  IL_5481:  dup
  IL_5482:  ldc.i4.3
  IL_5483:  shr
  IL_5484:  pop
  IL_5489:  dup
  IL_548a:  ldc.i4.2
  IL_548b:  shr
  IL_548c:  pop
  IL_5491:  dup
  IL_5492:  ldc.i4.6
  IL_5493:  or
  IL_5494:  pop
  IL_5499:  dup
  IL_549a:  ldc.i4.3
  IL_549b:  mul
  IL_549c:  pop
  IL_54a1:  dup
  IL_54a2:  ldc.i4.1
  IL_54a3:  mul
  IL_54a4:  pop
  IL_54a9:  dup
  IL_54aa:  ldc.i4.6
  IL_54ab:  and
  IL_54ac:  pop
  IL_54b1:  dup
  IL_54b2:  ldc.i4.4
  IL_54b3:  add
  IL_54b4:  pop
  IL_54b9:  dup
  IL_54ba:  ldc.i4.7
  IL_54bb:  shr
  IL_54bc:  pop
  IL_54c1:  dup
  IL_54c2:  ldc.i4.1
  IL_54c3:  shr
  IL_54c4:  pop
  IL_54c9:  dup
  IL_54ca:  ldc.i4.3
  IL_54cb:  and
  IL_54cc:  pop
  IL_54d1:  dup
  IL_54d2:  ldc.i4.0
  IL_54d3:  xor
  IL_54d4:  pop
  IL_54d9:  dup
  IL_54da:  ldc.i4.2
  IL_54db:  mul
  IL_54dc:  pop
  IL_54e1:  dup
  IL_54e2:  ldc.i4.4
  IL_54e3:  shl
  IL_54e4:  pop
  IL_54e9:  dup
  IL_54ea:  ldc.i4.5
  IL_54eb:  add
  IL_54ec:  pop
  IL_54f1:  dup
  IL_54f2:  ldc.i4.3
  IL_54f3:  shr
  IL_54f4:  pop
  IL_54f9:  dup
  IL_54fa:  ldc.i4.7
  IL_54fb:  shl
  IL_54fc:  pop
  IL_5501:  dup
  IL_5502:  ldc.i4.3
  IL_5503:  shr
  IL_5504:  pop
  IL_5509:  dup
  IL_550a:  ldc.i4.4
  IL_550b:  xor
  IL_550c:  pop
  IL_5511:  dup
  IL_5512:  ldc.i4.0
  IL_5513:  and
  IL_5514:  pop
  IL_5519:  dup
  IL_551a:  ldc.i4.7
  IL_551b:  and
  IL_551c:  pop
  IL_5521:  dup
  IL_5522:  ldc.i4.6
  IL_5523:  xor
  IL_5524:  pop
  IL_5529:  dup
  IL_552a:  ldc.i4.3
  IL_552b:  shr
  IL_552c:  pop
  IL_5531:  dup
  IL_5532:  ldc.i4.4
  IL_5533:  shl
  IL_5534:  pop
  IL_5539:  dup
  IL_553a:  ldc.i4.4
  IL_553b:  shl
  IL_553c:  pop
  IL_5541:  dup
  IL_5542:  ldc.i4.6
  IL_5543:  sub
  IL_5544:  pop
  IL_5549:  dup
  IL_554a:  ldc.i4.0
  IL_554b:  or
  IL_554c:  pop
  IL_5551:  dup
  IL_5552:  ldc.i4.5
  IL_5553:  add
  IL_5554:  pop
  IL_5559:  dup
  IL_555a:  ldc.i4.5
  IL_555b:  or
  IL_555c:  pop
  IL_5561:  dup
  IL_5562:  ldc.i4.3
  IL_5563:  add
  IL_5564:  pop
  IL_5569:  dup
  IL_556a:  ldc.i4.2
  IL_556b:  sub
  IL_556c:  pop
  IL_5571:  dup
  IL_5572:  ldc.i4.2
  IL_5573:  mul
  IL_5574:  pop
  IL_5579:  dup
  IL_557a:  ldc.i4.8
  IL_557b:  xor
  IL_557c:  pop
  IL_5581:  dup
  IL_5582:  ldc.i4.4
  IL_5583:  sub
  IL_5584:  pop
  IL_5589:  dup
  IL_558a:  ldc.i4.3
  IL_558b:  and
  IL_558c:  pop
  IL_5591:  dup
  IL_5592:  ldc.i4.5
  IL_5593:  xor
  IL_5594:  pop
  IL_5599:  dup
  IL_559a:  ldc.i4.5
  IL_559b:  xor
  IL_559c:  pop
  IL_55a1:  dup
  IL_55a2:  ldc.i4.6
  IL_55a3:  shr
  IL_55a4:  pop
  IL_55a9:  dup
  IL_55aa:  ldc.i4.0
  IL_55ab:  shl
  IL_55ac:  pop
  IL_55b1:  dup
  IL_55b2:  ldc.i4.1
  IL_55b3:  shl
  IL_55b4:  pop
  IL_55b9:  dup
  IL_55ba:  ldc.i4.7
  IL_55bb:  or
  IL_55bc:  pop
  IL_55c1:  dup
  IL_55c2:  ldc.i4.0
  IL_55c3:  add
  IL_55c4:  pop
  IL_55c9:  dup
  IL_55ca:  ldc.i4.2
  IL_55cb:  add
  IL_55cc:  pop
  IL_55d1:  dup
  IL_55d2:  ldc.i4.6
  IL_55d3:  or
  IL_55d4:  pop
  IL_55d9:  dup
  IL_55da:  ldc.i4.8
  IL_55db:  mul
  IL_55dc:  pop
  IL_55e1:  dup
  IL_55e2:  ldc.i4.0
  IL_55e3:  shl
  IL_55e4:  pop
  IL_55e9:  dup
  IL_55ea:  ldc.i4.0
  IL_55eb:  add
  IL_55ec:  pop
  IL_55f1:  dup
  IL_55f2:  ldc.i4.5
  IL_55f3:  and
  IL_55f4:  pop
  IL_55f9:  dup
  IL_55fa:  ldc.i4.3
  IL_55fb:  xor
  IL_55fc:  pop
  IL_5601:  dup
  IL_5602:  ldc.i4.5
  IL_5603:  or
  IL_5604:  pop
  IL_5609:  dup
  IL_560a:  ldc.i4.2
  IL_560b:  mul
  IL_560c:  pop
  IL_5611:  dup
  IL_5612:  ldc.i4.1
  IL_5613:  shr
  IL_5614:  pop
  IL_5619:  dup
  IL_561a:  ldc.i4.8
  IL_561b:  mul
  IL_561c:  pop
  IL_5621:  dup
  IL_5622:  ldc.i4.5
  IL_5623:  shr
  IL_5624:  pop
  IL_5629:  dup
  IL_562a:  ldc.i4.1
  IL_562b:  or
  IL_562c:  pop
  IL_5631:  dup
  IL_5632:  ldc.i4.8
  IL_5633:  add
  IL_5634:  pop
  IL_5639:  dup
  IL_563a:  ldc.i4.7
  IL_563b:  and
  IL_563c:  pop
  IL_5641:  dup
  IL_5642:  ldc.i4.1
  IL_5643:  mul
  IL_5644:  pop
  IL_5649:  dup
  IL_564a:  ldc.i4.0
  IL_564b:  sub
  IL_564c:  pop
  IL_5651:  dup
  IL_5652:  ldc.i4.8
  IL_5653:  and
  IL_5654:  pop
  IL_5659:  dup
  IL_565a:  ldc.i4.6
  IL_565b:  shl
  IL_565c:  pop
  IL_5661:  dup
  IL_5662:  ldc.i4.1
  IL_5663:  add
  IL_5664:  pop
  IL_5669:  dup
  IL_566a:  ldc.i4.6
  IL_566b:  add
  IL_566c:  pop
  IL_5671:  dup
  IL_5672:  ldc.i4.3
  IL_5673:  shl
  IL_5674:  pop
  IL_5679:  dup
  IL_567a:  ldc.i4.7
  IL_567b:  or
  IL_567c:  pop
  IL_5681:  dup
  IL_5682:  ldc.i4.1
  IL_5683:  shr
  IL_5684:  pop
  IL_5689:  dup
  IL_568a:  ldc.i4.0
  IL_568b:  sub
  IL_568c:  pop
  IL_5691:  dup
  IL_5692:  ldc.i4.2
  IL_5693:  shl
  IL_5694:  pop
  IL_5699:  dup
  IL_569a:  ldc.i4.7
  IL_569b:  mul
  IL_569c:  pop
  IL_56a1:  dup
  IL_56a2:  ldc.i4.1
  IL_56a3:  add
  IL_56a4:  pop
  IL_56a9:  dup
  IL_56aa:  ldc.i4.4
  IL_56ab:  or
  IL_56ac:  pop
  IL_56b1:  dup
  IL_56b2:  ldc.i4.6
  IL_56b3:  add
  IL_56b4:  pop
  IL_56b9:  dup
  IL_56ba:  ldc.i4.2
  IL_56bb:  add
  IL_56bc:  pop
  IL_56c1:  dup
  IL_56c2:  ldc.i4.6
  IL_56c3:  sub
  IL_56c4:  call       void [System.Console]System.Console::Write(int32)
  IL_56c9:  dup
  IL_56ca:  ldc.i4.8
  IL_56cb:  mul
  IL_56cc:  call       void [System.Console]System.Console::Write(int32)
  IL_56d1:  dup
  IL_56d2:  ldc.i4.6
  IL_56d3:  sub
  IL_56d4:  call       void [System.Console]System.Console::Write(int32)
  IL_56d9:  dup
  IL_56da:  ldc.i4.4
  IL_56db:  shr
  IL_56dc:  call       void [System.Console]System.Console::Write(int32)
  IL_56e1:  dup
  IL_56e2:  ldc.i4.1
  IL_56e3:  and
  IL_56e4:  call       void [System.Console]System.Console::Write(int32)
  IL_56e9:  dup
  IL_56ea:  ldc.i4.1
  IL_56eb:  add
  IL_56ec:  call       void [System.Console]System.Console::Write(int32)
  IL_56f1:  dup
  IL_56f2:  ldc.i4.8
  IL_56f3:  or
  IL_56f4:  call       void [System.Console]System.Console::Write(int32)
  IL_56f9:  dup
  IL_56fa:  ldc.i4.8
  IL_56fb:  or
  IL_56fc:  call       void [System.Console]System.Console::Write(int32)
  IL_5701:  dup
  IL_5702:  ldc.i4.7
  IL_5703:  sub
  IL_5704:  call       void [System.Console]System.Console::Write(int32)
  IL_5709:  dup
  IL_570a:  ldc.i4.6
  IL_570b:  mul
  IL_570c:  call       void [System.Console]System.Console::Write(int32)
  IL_5711:  dup
  IL_5712:  ldc.i4.8
  IL_5713:  shl
  IL_5714:  call       void [System.Console]System.Console::Write(int32)
  IL_5719:  dup
  IL_571a:  ldc.i4.5
  IL_571b:  shl
  IL_571c:  call       void [System.Console]System.Console::Write(int32)
  IL_5721:  dup
  IL_5722:  ldc.i4.4
  IL_5723:  shr
  IL_5724:  call       void [System.Console]System.Console::Write(int32)
  IL_5729:  dup
  IL_572a:  ldc.i4.1
  IL_572b:  or
  IL_572c:  call       void [System.Console]System.Console::Write(int32)
  IL_5731:  dup
  IL_5732:  ldc.i4.2
  IL_5733:  and
  IL_5734:  call       void [System.Console]System.Console::Write(int32)
  IL_5739:  dup
  IL_573a:  ldc.i4.7
  IL_573b:  sub
  IL_573c:  call       void [System.Console]System.Console::Write(int32)
  IL_5741:  dup
  IL_5742:  ldc.i4.6
  IL_5743:  shr
  IL_5744:  call       void [System.Console]System.Console::Write(int32)
  IL_5749:  dup
  IL_574a:  ldc.i4.2
  IL_574b:  mul
  IL_574c:  call       void [System.Console]System.Console::Write(int32)
  IL_5751:  dup
  IL_5752:  ldc.i4.6
  IL_5753:  shl
  IL_5754:  call       void [System.Console]System.Console::Write(int32)
  IL_5759:  dup
  IL_575a:  ldc.i4.2
  IL_575b:  add
  IL_575c:  call       void [System.Console]System.Console::Write(int32)
  IL_5761:  dup
  IL_5762:  ldc.i4.1
  IL_5763:  mul
  IL_5764:  call       void [System.Console]System.Console::Write(int32)
  IL_5769:  dup
  IL_576a:  ldc.i4.0
  IL_576b:  shl
  IL_576c:  call       void [System.Console]System.Console::Write(int32)
  IL_5771:  dup
  IL_5772:  ldc.i4.3
  IL_5773:  shr
  IL_5774:  call       void [System.Console]System.Console::Write(int32)
  IL_5779:  dup
  IL_577a:  ldc.i4.3
  IL_577b:  sub
  IL_577c:  call       void [System.Console]System.Console::Write(int32)
  IL_5781:  dup
  IL_5782:  ldc.i4.0
  IL_5783:  shl
  IL_5784:  call       void [System.Console]System.Console::Write(int32)
  IL_5789:  dup
  IL_578a:  ldc.i4.1
  IL_578b:  xor
  IL_578c:  call       void [System.Console]System.Console::Write(int32)
  IL_5791:  dup
  IL_5792:  ldc.i4.2
  IL_5793:  or
  IL_5794:  call       void [System.Console]System.Console::Write(int32)
  IL_5799:  dup
  IL_579a:  ldc.i4.0
  IL_579b:  sub
  IL_579c:  call       void [System.Console]System.Console::Write(int32)
  IL_57a1:  dup
  IL_57a2:  ldc.i4.4
  IL_57a3:  mul
  IL_57a4:  call       void [System.Console]System.Console::Write(int32)
  IL_57a9:  dup
  IL_57aa:  ldc.i4.7
  IL_57ab:  add
  IL_57ac:  call       void [System.Console]System.Console::Write(int32)
  IL_57b1:  dup
  IL_57b2:  ldc.i4.0
  IL_57b3:  xor
  IL_57b4:  call       void [System.Console]System.Console::Write(int32)
  IL_57b9:  dup
  IL_57ba:  ldc.i4.3
  IL_57bb:  xor
  IL_57bc:  call       void [System.Console]System.Console::Write(int32)
  IL_57c1:  dup
  IL_57c2:  ldc.i4.8
  IL_57c3:  add
  IL_57c4:  call       void [System.Console]System.Console::Write(int32)
  IL_57c9:  dup
  IL_57ca:  ldc.i4.3
  IL_57cb:  mul
  IL_57cc:  call       void [System.Console]System.Console::Write(int32)
  IL_57d1:  dup
  IL_57d2:  ldc.i4.5
  IL_57d3:  shr
  IL_57d4:  call       void [System.Console]System.Console::Write(int32)
  IL_57d9:  dup
  IL_57da:  ldc.i4.2
  IL_57db:  mul
  IL_57dc:  call       void [System.Console]System.Console::Write(int32)
  IL_57e1:  dup
  IL_57e2:  ldc.i4.8
  IL_57e3:  shr
  IL_57e4:  call       void [System.Console]System.Console::Write(int32)
  IL_57e9:  dup
  IL_57ea:  ldc.i4.1
  IL_57eb:  shr
  IL_57ec:  call       void [System.Console]System.Console::Write(int32)
  IL_57f1:  dup
  IL_57f2:  ldc.i4.5
  IL_57f3:  add
  IL_57f4:  call       void [System.Console]System.Console::Write(int32)
  IL_57f9:  dup
  IL_57fa:  ldc.i4.2
  IL_57fb:  shr
  IL_57fc:  call       void [System.Console]System.Console::Write(int32)
  IL_5801:  dup
  IL_5802:  ldc.i4.2
  IL_5803:  or
  IL_5804:  call       void [System.Console]System.Console::Write(int32)
  IL_5809:  dup
  IL_580a:  ldc.i4.7
  IL_580b:  shr
  IL_580c:  call       void [System.Console]System.Console::Write(int32)
  IL_5811:  dup
  IL_5812:  ldc.i4.5
  IL_5813:  mul
  IL_5814:  call       void [System.Console]System.Console::Write(int32)
  IL_5819:  dup
  IL_581a:  ldc.i4.7
  IL_581b:  and
  IL_581c:  call       void [System.Console]System.Console::Write(int32)
  IL_5821:  dup
  IL_5822:  ldc.i4.6
  IL_5823:  sub
  IL_5824:  call       void [System.Console]System.Console::Write(int32)
  IL_5829:  dup
  IL_582a:  ldc.i4.6
  IL_582b:  xor
  IL_582c:  call       void [System.Console]System.Console::Write(int32)
  IL_5831:  dup
  IL_5832:  ldc.i4.6
  IL_5833:  mul
  IL_5834:  call       void [System.Console]System.Console::Write(int32)
  IL_5839:  dup
  IL_583a:  ldc.i4.8
  IL_583b:  shr
  IL_583c:  call       void [System.Console]System.Console::Write(int32)
  IL_5841:  dup
  IL_5842:  ldc.i4.7
  IL_5843:  shl
  IL_5844:  call       void [System.Console]System.Console::Write(int32)
  IL_5849:  dup
  IL_584a:  ldc.i4.4
  IL_584b:  mul
  IL_584c:  call       void [System.Console]System.Console::Write(int32)
  IL_5851:  dup
  IL_5852:  ldc.i4.3
  IL_5853:  sub
  IL_5854:  call       void [System.Console]System.Console::Write(int32)
  IL_5859:  dup
  IL_585a:  ldc.i4.5
  IL_585b:  xor
  IL_585c:  call       void [System.Console]System.Console::Write(int32)
  IL_5861:  dup
  IL_5862:  ldc.i4.2
  IL_5863:  and
  IL_5864:  call       void [System.Console]System.Console::Write(int32)
  IL_5869:  dup
  IL_586a:  ldc.i4.4
  IL_586b:  shl
  IL_586c:  call       void [System.Console]System.Console::Write(int32)
  IL_5871:  dup
  IL_5872:  ldc.i4.7
  IL_5873:  mul
  IL_5874:  call       void [System.Console]System.Console::Write(int32)
  IL_5879:  dup
  IL_587a:  ldc.i4.5
  IL_587b:  xor
  IL_587c:  call       void [System.Console]System.Console::Write(int32)
  IL_5881:  dup
  IL_5882:  ldc.i4.0
  IL_5883:  add
  IL_5884:  call       void [System.Console]System.Console::Write(int32)
  IL_5889:  dup
  IL_588a:  ldc.i4.6
  IL_588b:  sub
  IL_588c:  call       void [System.Console]System.Console::Write(int32)
  IL_5891:  dup
  IL_5892:  ldc.i4.5
  IL_5893:  shr
  IL_5894:  call       void [System.Console]System.Console::Write(int32)
  IL_5899:  dup
  IL_589a:  ldc.i4.7
  IL_589b:  shr
  IL_589c:  call       void [System.Console]System.Console::Write(int32)
  IL_58a1:  dup
  IL_58a2:  ldc.i4.1
  IL_58a3:  shl
  IL_58a4:  call       void [System.Console]System.Console::Write(int32)
  IL_58a9:  dup
  IL_58aa:  ldc.i4.5
  IL_58ab:  xor
  IL_58ac:  call       void [System.Console]System.Console::Write(int32)
  IL_58b1:  dup
  IL_58b2:  ldc.i4.8
  IL_58b3:  and
  IL_58b4:  call       void [System.Console]System.Console::Write(int32)
  IL_58b9:  dup
  IL_58ba:  ldc.i4.4
  IL_58bb:  shr
  IL_58bc:  call       void [System.Console]System.Console::Write(int32)
  IL_58c1:  dup
  IL_58c2:  ldc.i4.7
  IL_58c3:  xor
  IL_58c4:  call       void [System.Console]System.Console::Write(int32)
  IL_58c9:  dup
  IL_58ca:  ldc.i4.7
  IL_58cb:  or
  IL_58cc:  call       void [System.Console]System.Console::Write(int32)
  IL_58d1:  dup
  IL_58d2:  ldc.i4.4
  IL_58d3:  or
  IL_58d4:  call       void [System.Console]System.Console::Write(int32)
  IL_58d9:  dup
  IL_58da:  ldc.i4.3
  IL_58db:  sub
  IL_58dc:  call       void [System.Console]System.Console::Write(int32)
  IL_58e1:  dup
  IL_58e2:  ldc.i4.3
  IL_58e3:  xor
  IL_58e4:  call       void [System.Console]System.Console::Write(int32)
  IL_58e9:  dup
  IL_58ea:  ldc.i4.3
  IL_58eb:  and
  IL_58ec:  call       void [System.Console]System.Console::Write(int32)
  IL_58f1:  dup
  IL_58f2:  ldc.i4.6
  IL_58f3:  mul
  IL_58f4:  call       void [System.Console]System.Console::Write(int32)
  IL_58f9:  dup
  IL_58fa:  ldc.i4.7
  IL_58fb:  mul
  IL_58fc:  call       void [System.Console]System.Console::Write(int32)
  IL_5901:  dup
  IL_5902:  ldc.i4.4
  IL_5903:  sub
  IL_5904:  call       void [System.Console]System.Console::Write(int32)
  IL_5909:  dup
  IL_590a:  ldc.i4.3
  IL_590b:  shr
  IL_590c:  call       void [System.Console]System.Console::Write(int32)
  IL_5911:  dup
  IL_5912:  ldc.i4.4
  IL_5913:  and
  IL_5914:  call       void [System.Console]System.Console::Write(int32)
  IL_5919:  dup
  IL_591a:  ldc.i4.0
  IL_591b:  sub
  IL_591c:  call       void [System.Console]System.Console::Write(int32)
  IL_5921:  dup
  IL_5922:  ldc.i4.7
  IL_5923:  or
  IL_5924:  call       void [System.Console]System.Console::Write(int32)
  IL_5929:  dup
  IL_592a:  ldc.i4.1
  IL_592b:  and
  IL_592c:  call       void [System.Console]System.Console::Write(int32)
  IL_5931:  dup
  IL_5932:  ldc.i4.5
  IL_5933:  add
  IL_5934:  call       void [System.Console]System.Console::Write(int32)
  IL_5939:  dup
  IL_593a:  ldc.i4.4
  IL_593b:  shl
  IL_593c:  call       void [System.Console]System.Console::Write(int32)
  IL_5941:  dup
  IL_5942:  ldc.i4.2
  IL_5943:  and
  IL_5944:  call       void [System.Console]System.Console::Write(int32)
  IL_5949:  dup
  IL_594a:  ldc.i4.6
  IL_594b:  xor
  IL_594c:  call       void [System.Console]System.Console::Write(int32)
  IL_5951:  dup
  IL_5952:  ldc.i4.5
  IL_5953:  shr
  IL_5954:  call       void [System.Console]System.Console::Write(int32)
  IL_5959:  dup
  IL_595a:  ldc.i4.8
  IL_595b:  add
  IL_595c:  call       void [System.Console]System.Console::Write(int32)
  IL_5961:  dup
  IL_5962:  ldc.i4.6
  IL_5963:  shl
  IL_5964:  call       void [System.Console]System.Console::Write(int32)
  IL_5969:  dup
  IL_596a:  ldc.i4.7
  IL_596b:  sub
  IL_596c:  call       void [System.Console]System.Console::Write(int32)
  IL_5971:  dup
  IL_5972:  ldc.i4.3
  IL_5973:  add
  IL_5974:  call       void [System.Console]System.Console::Write(int32)
  IL_5979:  dup
  IL_597a:  ldc.i4.8
  IL_597b:  and
  IL_597c:  call       void [System.Console]System.Console::Write(int32)
  IL_5981:  dup
  IL_5982:  ldc.i4.6
  IL_5983:  sub
  IL_5984:  call       void [System.Console]System.Console::Write(int32)
  IL_5989:  dup
  IL_598a:  ldc.i4.8
  IL_598b:  shr
  IL_598c:  call       void [System.Console]System.Console::Write(int32)
  IL_5991:  dup
  IL_5992:  ldc.i4.3
  IL_5993:  and
  IL_5994:  call       void [System.Console]System.Console::Write(int32)
  IL_5999:  dup
  IL_599a:  ldc.i4.1
  IL_599b:  or
  IL_599c:  call       void [System.Console]System.Console::Write(int32)
  IL_59a1:  dup
  IL_59a2:  ldc.i4.2
  IL_59a3:  shr
  IL_59a4:  call       void [System.Console]System.Console::Write(int32)
  IL_59a9:  dup
  IL_59aa:  ldc.i4.1
  IL_59ab:  shr
  IL_59ac:  call       void [System.Console]System.Console::Write(int32)
  IL_59b1:  dup
  IL_59b2:  ldc.i4.6
  IL_59b3:  shl
  IL_59b4:  call       void [System.Console]System.Console::Write(int32)
  IL_59b9:  dup
  IL_59ba:  ldc.i4.5
  IL_59bb:  sub
  IL_59bc:  call       void [System.Console]System.Console::Write(int32)
  IL_59c1:  dup
  IL_59c2:  ldc.i4.7
  IL_59c3:  and
  IL_59c4:  call       void [System.Console]System.Console::Write(int32)
  IL_59c9:  dup
  IL_59ca:  ldc.i4.4
  IL_59cb:  or
  IL_59cc:  call       void [System.Console]System.Console::Write(int32)
  IL_59d1:  dup
  IL_59d2:  ldc.i4.5
  IL_59d3:  xor
  IL_59d4:  call       void [System.Console]System.Console::Write(int32)
  IL_59d9:  dup
  IL_59da:  ldc.i4.6
  IL_59db:  or
  IL_59dc:  call       void [System.Console]System.Console::Write(int32)
  IL_59e1:  dup
  IL_59e2:  ldc.i4.2
  IL_59e3:  mul
  IL_59e4:  call       void [System.Console]System.Console::Write(int32)
  IL_59e9:  dup
  IL_59ea:  ldc.i4.8
  IL_59eb:  or
  IL_59ec:  call       void [System.Console]System.Console::Write(int32)
  IL_59f1:  dup
  IL_59f2:  ldc.i4.0
  IL_59f3:  or
  IL_59f4:  call       void [System.Console]System.Console::Write(int32)
  IL_59f9:  dup
  IL_59fa:  ldc.i4.7
  IL_59fb:  shr
  IL_59fc:  call       void [System.Console]System.Console::Write(int32)
  IL_5a01:  dup
  IL_5a02:  ldc.i4.8
  IL_5a03:  mul
  IL_5a04:  call       void [System.Console]System.Console::Write(int32)
  IL_5a09:  dup
  IL_5a0a:  ldc.i4.0
  IL_5a0b:  mul
  IL_5a0c:  call       void [System.Console]System.Console::Write(int32)
  IL_5a11:  dup
  IL_5a12:  ldc.i4.1
  IL_5a13:  and
  IL_5a14:  call       void [System.Console]System.Console::Write(int32)
  IL_5a19:  dup
  IL_5a1a:  ldc.i4.1
  IL_5a1b:  xor
  IL_5a1c:  call       void [System.Console]System.Console::Write(int32)
  IL_5a21:  dup
  IL_5a22:  ldc.i4.5
  IL_5a23:  shr
  IL_5a24:  call       void [System.Console]System.Console::Write(int32)
  IL_5a29:  dup
  IL_5a2a:  ldc.i4.1
  IL_5a2b:  or
  IL_5a2c:  call       void [System.Console]System.Console::Write(int32)
  IL_5a31:  dup
  IL_5a32:  ldc.i4.4
  IL_5a33:  mul
  IL_5a34:  call       void [System.Console]System.Console::Write(int32)
  IL_5a39:  dup
  IL_5a3a:  ldc.i4.3
  IL_5a3b:  sub
  IL_5a3c:  call       void [System.Console]System.Console::Write(int32)
  IL_5a41:  dup
  IL_5a42:  ldc.i4.0
  IL_5a43:  shl
  IL_5a44:  call       void [System.Console]System.Console::Write(int32)
  IL_5a49:  dup
  IL_5a4a:  ldc.i4.2
  IL_5a4b:  xor
  IL_5a4c:  call       void [System.Console]System.Console::Write(int32)
  IL_5a51:  dup
  IL_5a52:  ldc.i4.5
  IL_5a53:  or
  IL_5a54:  call       void [System.Console]System.Console::Write(int32)
  IL_5a59:  dup
  IL_5a5a:  ldc.i4.1
  IL_5a5b:  mul
  IL_5a5c:  call       void [System.Console]System.Console::Write(int32)
  IL_5a61:  dup
  IL_5a62:  ldc.i4.4
  IL_5a63:  add
  IL_5a64:  call       void [System.Console]System.Console::Write(int32)
  IL_5a69:  dup
  IL_5a6a:  ldc.i4.3
  IL_5a6b:  add
  IL_5a6c:  call       void [System.Console]System.Console::Write(int32)
  IL_5a71:  dup
  IL_5a72:  ldc.i4.8
  IL_5a73:  mul
  IL_5a74:  call       void [System.Console]System.Console::Write(int32)
  IL_5a79:  dup
  IL_5a7a:  ldc.i4.8
  IL_5a7b:  xor
  IL_5a7c:  call       void [System.Console]System.Console::Write(int32)
  IL_5a81:  dup
  IL_5a82:  ldc.i4.7
  IL_5a83:  xor
  IL_5a84:  call       void [System.Console]System.Console::Write(int32)
  IL_5a89:  dup
  IL_5a8a:  ldc.i4.5
  IL_5a8b:  xor
  IL_5a8c:  call       void [System.Console]System.Console::Write(int32)
  IL_5a91:  dup
  IL_5a92:  ldc.i4.4
  IL_5a93:  sub
  IL_5a94:  call       void [System.Console]System.Console::Write(int32)
  IL_5a99:  dup
  IL_5a9a:  ldc.i4.0
  IL_5a9b:  add
  IL_5a9c:  call       void [System.Console]System.Console::Write(int32)
  IL_5aa1:  dup
  IL_5aa2:  ldc.i4.6
  IL_5aa3:  add
  IL_5aa4:  call       void [System.Console]System.Console::Write(int32)
  IL_5aa9:  dup
  IL_5aaa:  ldc.i4.2
  IL_5aab:  or
  IL_5aac:  call       void [System.Console]System.Console::Write(int32)
  IL_5ab1:  dup
  IL_5ab2:  ldc.i4.1
  IL_5ab3:  and
  IL_5ab4:  call       void [System.Console]System.Console::Write(int32)
  IL_5ab9:  dup
  IL_5aba:  ldc.i4.3
  IL_5abb:  add
  IL_5abc:  call       void [System.Console]System.Console::Write(int32)
  IL_5ac1:  dup
  IL_5ac2:  ldc.i4.8
  IL_5ac3:  and
  IL_5ac4:  call       void [System.Console]System.Console::Write(int32)
  IL_5ac9:  dup
  IL_5aca:  ldc.i4.7
  IL_5acb:  xor
  IL_5acc:  call       void [System.Console]System.Console::Write(int32)
  IL_5ad1:  dup
  IL_5ad2:  ldc.i4.5
  IL_5ad3:  mul
  IL_5ad4:  call       void [System.Console]System.Console::Write(int32)
  IL_5ad9:  dup
  IL_5ada:  ldc.i4.1
  IL_5adb:  add
  IL_5adc:  call       void [System.Console]System.Console::Write(int32)
  IL_5ae1:  dup
  IL_5ae2:  ldc.i4.1
  IL_5ae3:  and
  IL_5ae4:  call       void [System.Console]System.Console::Write(int32)
  IL_5ae9:  dup
  IL_5aea:  ldc.i4.7
  IL_5aeb:  sub
  IL_5aec:  call       void [System.Console]System.Console::Write(int32)
  IL_5af1:  dup
  IL_5af2:  ldc.i4.4
  IL_5af3:  add
  IL_5af4:  call       void [System.Console]System.Console::Write(int32)
  IL_5af9:  dup
  IL_5afa:  ldc.i4.4
  IL_5afb:  xor
  IL_5afc:  call       void [System.Console]System.Console::Write(int32)
  IL_5b01:  dup
  IL_5b02:  ldc.i4.6
  IL_5b03:  and
  IL_5b04:  call       void [System.Console]System.Console::Write(int32)
  IL_5b09:  dup
  IL_5b0a:  ldc.i4.0
  IL_5b0b:  add
  IL_5b0c:  call       void [System.Console]System.Console::Write(int32)
  IL_5b11:  dup
  IL_5b12:  ldc.i4.5
  IL_5b13:  or
  IL_5b14:  call       void [System.Console]System.Console::Write(int32)
  IL_5b19:  dup
  IL_5b1a:  ldc.i4.6
  IL_5b1b:  and
  IL_5b1c:  call       void [System.Console]System.Console::Write(int32)
  IL_5b21:  dup
  IL_5b22:  ldc.i4.4
  IL_5b23:  mul
  IL_5b24:  call       void [System.Console]System.Console::Write(int32)
  IL_5b29:  dup
  IL_5b2a:  ldc.i4.7
  IL_5b2b:  shl
  IL_5b2c:  call       void [System.Console]System.Console::Write(int32)
  IL_5b31:  dup
  IL_5b32:  ldc.i4.6
  IL_5b33:  mul
  IL_5b34:  call       void [System.Console]System.Console::Write(int32)
  IL_5b39:  dup
  IL_5b3a:  ldc.i4.1
  IL_5b3b:  shr
  IL_5b3c:  call       void [System.Console]System.Console::Write(int32)
  IL_5b41:  dup
  IL_5b42:  ldc.i4.5
  IL_5b43:  mul
  IL_5b44:  call       void [System.Console]System.Console::Write(int32)
  IL_5b49:  dup
  IL_5b4a:  ldc.i4.1
  IL_5b4b:  add
  IL_5b4c:  call       void [System.Console]System.Console::Write(int32)
  IL_5b51:  dup
  IL_5b52:  ldc.i4.3
  IL_5b53:  shl
  IL_5b54:  call       void [System.Console]System.Console::Write(int32)
  IL_5b59:  dup
  IL_5b5a:  ldc.i4.7
  IL_5b5b:  shl
  IL_5b5c:  call       void [System.Console]System.Console::Write(int32)
  IL_5b61:  dup
  IL_5b62:  ldc.i4.2
  IL_5b63:  shr
  IL_5b64:  call       void [System.Console]System.Console::Write(int32)
  IL_5b69:  dup
  IL_5b6a:  ldc.i4.3
  IL_5b6b:  shr
  IL_5b6c:  call       void [System.Console]System.Console::Write(int32)
  IL_5b71:  dup
  IL_5b72:  ldc.i4.3
  IL_5b73:  and
  IL_5b74:  call       void [System.Console]System.Console::Write(int32)
  IL_5b79:  dup
  IL_5b7a:  ldc.i4.5
  IL_5b7b:  shr
  IL_5b7c:  call       void [System.Console]System.Console::Write(int32)
  IL_5b81:  dup
  IL_5b82:  ldc.i4.4
  IL_5b83:  and
  IL_5b84:  call       void [System.Console]System.Console::Write(int32)
  IL_5b89:  dup
  IL_5b8a:  ldc.i4.1
  IL_5b8b:  xor
  IL_5b8c:  call       void [System.Console]System.Console::Write(int32)
  IL_5b91:  dup
  IL_5b92:  ldc.i4.7
  IL_5b93:  xor
  IL_5b94:  call       void [System.Console]System.Console::Write(int32)
  IL_5b99:  dup
  IL_5b9a:  ldc.i4.3
  IL_5b9b:  add
  IL_5b9c:  call       void [System.Console]System.Console::Write(int32)
  IL_5ba1:  dup
  IL_5ba2:  ldc.i4.1
  IL_5ba3:  add
  IL_5ba4:  call       void [System.Console]System.Console::Write(int32)
  IL_5ba9:  dup
  IL_5baa:  ldc.i4.7
  IL_5bab:  mul
  IL_5bac:  call       void [System.Console]System.Console::Write(int32)
  IL_5bb1:  dup
  IL_5bb2:  ldc.i4.4
  IL_5bb3:  shl
  IL_5bb4:  call       void [System.Console]System.Console::Write(int32)
  IL_5bb9:  dup
  IL_5bba:  ldc.i4.2
  IL_5bbb:  and
  IL_5bbc:  call       void [System.Console]System.Console::Write(int32)
  IL_5bc1:  dup
  IL_5bc2:  ldc.i4.5
  IL_5bc3:  sub
  IL_5bc4:  call       void [System.Console]System.Console::Write(int32)
  IL_5bc9:  dup
  IL_5bca:  ldc.i4.5
  IL_5bcb:  add
  IL_5bcc:  call       void [System.Console]System.Console::Write(int32)
  IL_5bd1:  dup
  IL_5bd2:  ldc.i4.7
  IL_5bd3:  xor
  IL_5bd4:  call       void [System.Console]System.Console::Write(int32)
  IL_5bd9:  dup
  IL_5bda:  ldc.i4.8
  IL_5bdb:  sub
  IL_5bdc:  call       void [System.Console]System.Console::Write(int32)
  IL_5be1:  dup
  IL_5be2:  ldc.i4.0
  IL_5be3:  shl
  IL_5be4:  call       void [System.Console]System.Console::Write(int32)
  IL_5be9:  dup
  IL_5bea:  ldc.i4.6
  IL_5beb:  or
  IL_5bec:  call       void [System.Console]System.Console::Write(int32)
  IL_5bf1:  dup
  IL_5bf2:  ldc.i4.4
  IL_5bf3:  or
  IL_5bf4:  call       void [System.Console]System.Console::Write(int32)
  IL_5bf9:  dup
  IL_5bfa:  ldc.i4.3
  IL_5bfb:  shr
  IL_5bfc:  call       void [System.Console]System.Console::Write(int32)
  IL_5c01:  dup
  IL_5c02:  ldc.i4.6
  IL_5c03:  add
  IL_5c04:  call       void [System.Console]System.Console::Write(int32)
  IL_5c09:  dup
  IL_5c0a:  ldc.i4.1
  IL_5c0b:  and
  IL_5c0c:  call       void [System.Console]System.Console::Write(int32)
  IL_5c11:  dup
  IL_5c12:  ldc.i4.0
  IL_5c13:  shr
  IL_5c14:  call       void [System.Console]System.Console::Write(int32)
  IL_5c19:  dup
  IL_5c1a:  ldc.i4.7
  IL_5c1b:  sub
  IL_5c1c:  call       void [System.Console]System.Console::Write(int32)
  IL_5c21:  dup
  IL_5c22:  ldc.i4.8
  IL_5c23:  sub
  IL_5c24:  call       void [System.Console]System.Console::Write(int32)
  IL_5c29:  dup
  IL_5c2a:  ldc.i4.8
  IL_5c2b:  mul
  IL_5c2c:  call       void [System.Console]System.Console::Write(int32)
  IL_5c31:  dup
  IL_5c32:  ldc.i4.6
  IL_5c33:  xor
  IL_5c34:  call       void [System.Console]System.Console::Write(int32)
  IL_5c39:  dup
  IL_5c3a:  ldc.i4.8
  IL_5c3b:  and
  IL_5c3c:  call       void [System.Console]System.Console::Write(int32)
  IL_5c41:  dup
  IL_5c42:  ldc.i4.3
  IL_5c43:  shl
  IL_5c44:  call       void [System.Console]System.Console::Write(int32)
  IL_5c49:  dup
  IL_5c4a:  ldc.i4.0
  IL_5c4b:  sub
  IL_5c4c:  call       void [System.Console]System.Console::Write(int32)
  IL_5c51:  dup
  IL_5c52:  ldc.i4.2
  IL_5c53:  xor
  IL_5c54:  call       void [System.Console]System.Console::Write(int32)
  IL_5c59:  dup
  IL_5c5a:  ldc.i4.7
  IL_5c5b:  add
  IL_5c5c:  call       void [System.Console]System.Console::Write(int32)
  IL_5c61:  dup
  IL_5c62:  ldc.i4.7
  IL_5c63:  add
  IL_5c64:  call       void [System.Console]System.Console::Write(int32)
  IL_5c69:  dup
  IL_5c6a:  ldc.i4.3
  IL_5c6b:  mul
  IL_5c6c:  call       void [System.Console]System.Console::Write(int32)
  IL_5c71:  dup
  IL_5c72:  ldc.i4.6
  IL_5c73:  xor
  IL_5c74:  call       void [System.Console]System.Console::Write(int32)
  IL_5c79:  dup
  IL_5c7a:  ldc.i4.4
  IL_5c7b:  sub
  IL_5c7c:  call       void [System.Console]System.Console::Write(int32)
  IL_5c81:  dup
  IL_5c82:  ldc.i4.6
  IL_5c83:  sub
  IL_5c84:  call       void [System.Console]System.Console::Write(int32)
  IL_5c89:  dup
  IL_5c8a:  ldc.i4.5
  IL_5c8b:  xor
  IL_5c8c:  call       void [System.Console]System.Console::Write(int32)
  IL_5c91:  dup
  IL_5c92:  ldc.i4.3
  IL_5c93:  xor
  IL_5c94:  call       void [System.Console]System.Console::Write(int32)
  IL_5c99:  dup
  IL_5c9a:  ldc.i4.6
  IL_5c9b:  or
  IL_5c9c:  call       void [System.Console]System.Console::Write(int32)
  IL_5ca1:  dup
  IL_5ca2:  ldc.i4.3
  IL_5ca3:  xor
  IL_5ca4:  call       void [System.Console]System.Console::Write(int32)
  IL_5ca9:  dup
  IL_5caa:  ldc.i4.1
  IL_5cab:  shr
  IL_5cac:  call       void [System.Console]System.Console::Write(int32)
  IL_5cb1:  dup
  IL_5cb2:  ldc.i4.4
  IL_5cb3:  shr
  IL_5cb4:  call       void [System.Console]System.Console::Write(int32)
  IL_5cb9:  dup
  IL_5cba:  ldc.i4.4
  IL_5cbb:  shl
  IL_5cbc:  call       void [System.Console]System.Console::Write(int32)
  IL_5cc1:  dup
  IL_5cc2:  ldc.i4.4
  IL_5cc3:  or
  IL_5cc4:  call       void [System.Console]System.Console::Write(int32)
  IL_5cc9:  dup
  IL_5cca:  ldc.i4.5
  IL_5ccb:  add
  IL_5ccc:  call       void [System.Console]System.Console::Write(int32)
  IL_5cd1:  dup
  IL_5cd2:  ldc.i4.8
  IL_5cd3:  sub
  IL_5cd4:  call       void [System.Console]System.Console::Write(int32)
  IL_5cd9:  dup
  IL_5cda:  ldc.i4.3
  IL_5cdb:  sub
  IL_5cdc:  call       void [System.Console]System.Console::Write(int32)
  IL_5ce1:  dup
  IL_5ce2:  ldc.i4.2
  IL_5ce3:  add
  IL_5ce4:  call       void [System.Console]System.Console::Write(int32)
  IL_5ce9:  dup
  IL_5cea:  ldc.i4.8
  IL_5ceb:  add
  IL_5cec:  call       void [System.Console]System.Console::Write(int32)
  IL_5cf1:  dup
  IL_5cf2:  ldc.i4.2
  IL_5cf3:  shl
  IL_5cf4:  call       void [System.Console]System.Console::Write(int32)
  IL_5cf9:  dup
  IL_5cfa:  ldc.i4.4
  IL_5cfb:  shl
  IL_5cfc:  call       void [System.Console]System.Console::Write(int32)
  IL_5d01:  dup
  IL_5d02:  ldc.i4.5
  IL_5d03:  and
  IL_5d04:  call       void [System.Console]System.Console::Write(int32)
  IL_5d09:  dup
  IL_5d0a:  ldc.i4.5
  IL_5d0b:  xor
  IL_5d0c:  call       void [System.Console]System.Console::Write(int32)
  IL_5d11:  dup
  IL_5d12:  ldc.i4.7
  IL_5d13:  shl
  IL_5d14:  call       void [System.Console]System.Console::Write(int32)
  IL_5d19:  dup
  IL_5d1a:  ldc.i4.3
  IL_5d1b:  or
  IL_5d1c:  call       void [System.Console]System.Console::Write(int32)
  IL_5d21:  dup
  IL_5d22:  ldc.i4.7
  IL_5d23:  sub
  IL_5d24:  call       void [System.Console]System.Console::Write(int32)
  IL_5d29:  dup
  IL_5d2a:  ldc.i4.7
  IL_5d2b:  or
  IL_5d2c:  call       void [System.Console]System.Console::Write(int32)
  IL_5d31:  dup
  IL_5d32:  ldc.i4.3
  IL_5d33:  add
  IL_5d34:  call       void [System.Console]System.Console::Write(int32)
  IL_5d39:  dup
  IL_5d3a:  ldc.i4.4
  IL_5d3b:  sub
  IL_5d3c:  call       void [System.Console]System.Console::Write(int32)
  IL_5d41:  dup
  IL_5d42:  ldc.i4.4
  IL_5d43:  and
  IL_5d44:  call       void [System.Console]System.Console::Write(int32)
  IL_5d49:  dup
  IL_5d4a:  ldc.i4.6
  IL_5d4b:  and
  IL_5d4c:  call       void [System.Console]System.Console::Write(int32)
  IL_5d51:  dup
  IL_5d52:  ldc.i4.7
  IL_5d53:  xor
  IL_5d54:  call       void [System.Console]System.Console::Write(int32)
  IL_5d59:  dup
  IL_5d5a:  ldc.i4.1
  IL_5d5b:  xor
  IL_5d5c:  call       void [System.Console]System.Console::Write(int32)
  IL_5d61:  dup
  IL_5d62:  ldc.i4.7
  IL_5d63:  xor
  IL_5d64:  call       void [System.Console]System.Console::Write(int32)
  IL_5d69:  dup
  IL_5d6a:  ldc.i4.1
  IL_5d6b:  mul
  IL_5d6c:  call       void [System.Console]System.Console::Write(int32)
  IL_5d71:  dup
  IL_5d72:  ldc.i4.3
  IL_5d73:  shr
  IL_5d74:  call       void [System.Console]System.Console::Write(int32)
  IL_5d79:  dup
  IL_5d7a:  ldc.i4.5
  IL_5d7b:  and
  IL_5d7c:  call       void [System.Console]System.Console::Write(int32)
  IL_5d81:  dup
  IL_5d82:  ldc.i4.7
  IL_5d83:  shl
  IL_5d84:  call       void [System.Console]System.Console::Write(int32)
  IL_5d89:  dup
  IL_5d8a:  ldc.i4.0
  IL_5d8b:  shr
  IL_5d8c:  call       void [System.Console]System.Console::Write(int32)
  IL_5d91:  dup
  IL_5d92:  ldc.i4.0
  IL_5d93:  or
  IL_5d94:  call       void [System.Console]System.Console::Write(int32)
  IL_5d99:  dup
  IL_5d9a:  ldc.i4.6
  IL_5d9b:  or
  IL_5d9c:  call       void [System.Console]System.Console::Write(int32)
  IL_5da1:  dup
  IL_5da2:  ldc.i4.4
  IL_5da3:  shl
  IL_5da4:  call       void [System.Console]System.Console::Write(int32)
  IL_5da9:  dup
  IL_5daa:  ldc.i4.2
  IL_5dab:  and
  IL_5dac:  call       void [System.Console]System.Console::Write(int32)
  IL_5db1:  dup
  IL_5db2:  ldc.i4.3
  IL_5db3:  xor
  IL_5db4:  call       void [System.Console]System.Console::Write(int32)
  IL_5db9:  dup
  IL_5dba:  ldc.i4.7
  IL_5dbb:  sub
  IL_5dbc:  call       void [System.Console]System.Console::Write(int32)
  IL_5dc1:  dup
  IL_5dc2:  ldc.i4.5
  IL_5dc3:  and
  IL_5dc4:  call       void [System.Console]System.Console::Write(int32)
  IL_5dc9:  dup
  IL_5dca:  ldc.i4.4
  IL_5dcb:  xor
  IL_5dcc:  call       void [System.Console]System.Console::Write(int32)
  IL_5dd1:  dup
  IL_5dd2:  ldc.i4.7
  IL_5dd3:  and
  IL_5dd4:  call       void [System.Console]System.Console::Write(int32)
  IL_5dd9:  dup
  IL_5dda:  ldc.i4.6
  IL_5ddb:  sub
  IL_5ddc:  call       void [System.Console]System.Console::Write(int32)
  IL_5de1:  dup
  IL_5de2:  ldc.i4.5
  IL_5de3:  add
  IL_5de4:  call       void [System.Console]System.Console::Write(int32)
  IL_5de9:  dup
  IL_5dea:  ldc.i4.6
  IL_5deb:  xor
  IL_5dec:  call       void [System.Console]System.Console::Write(int32)
  IL_5df1:  dup
  IL_5df2:  ldc.i4.3
  IL_5df3:  sub
  IL_5df4:  call       void [System.Console]System.Console::Write(int32)
  IL_5df9:  dup
  IL_5dfa:  ldc.i4.5
  IL_5dfb:  sub
  IL_5dfc:  call       void [System.Console]System.Console::Write(int32)
  IL_5e01:  dup
  IL_5e02:  ldc.i4.5
  IL_5e03:  or
  IL_5e04:  call       void [System.Console]System.Console::Write(int32)
  IL_5e09:  dup
  IL_5e0a:  ldc.i4.0
  IL_5e0b:  and
  IL_5e0c:  call       void [System.Console]System.Console::Write(int32)
  IL_5e11:  dup
  IL_5e12:  ldc.i4.0
  IL_5e13:  shr
  IL_5e14:  call       void [System.Console]System.Console::Write(int32)
  IL_5e19:  dup
  IL_5e1a:  ldc.i4.0
  IL_5e1b:  shl
  IL_5e1c:  call       void [System.Console]System.Console::Write(int32)
  IL_5e21:  dup
  IL_5e22:  ldc.i4.1
  IL_5e23:  shr
  IL_5e24:  call       void [System.Console]System.Console::Write(int32)
  IL_5e29:  dup
  IL_5e2a:  ldc.i4.2
  IL_5e2b:  mul
  IL_5e2c:  call       void [System.Console]System.Console::Write(int32)
  IL_5e31:  dup
  IL_5e32:  ldc.i4.2
  IL_5e33:  xor
  IL_5e34:  call       void [System.Console]System.Console::Write(int32)
  IL_5e39:  dup
  IL_5e3a:  ldc.i4.0
  IL_5e3b:  mul
  IL_5e3c:  call       void [System.Console]System.Console::Write(int32)
  IL_5e41:  dup
  IL_5e42:  ldc.i4.4
  IL_5e43:  or
  IL_5e44:  call       void [System.Console]System.Console::Write(int32)
  IL_5e49:  dup
  IL_5e4a:  ldc.i4.5
  IL_5e4b:  shl
  IL_5e4c:  call       void [System.Console]System.Console::Write(int32)
  IL_5e51:  dup
  IL_5e52:  ldc.i4.0
  IL_5e53:  add
  IL_5e54:  call       void [System.Console]System.Console::Write(int32)
  IL_5e59:  dup
  IL_5e5a:  ldc.i4.8
  IL_5e5b:  add
  IL_5e5c:  call       void [System.Console]System.Console::Write(int32)
  IL_5e61:  dup
  IL_5e62:  ldc.i4.8
  IL_5e63:  or
  IL_5e64:  call       void [System.Console]System.Console::Write(int32)
  IL_5e69:  dup
  IL_5e6a:  ldc.i4.1
  IL_5e6b:  or
  IL_5e6c:  call       void [System.Console]System.Console::Write(int32)
  IL_5e71:  dup
  IL_5e72:  ldc.i4.2
  IL_5e73:  sub
  IL_5e74:  call       void [System.Console]System.Console::Write(int32)
  IL_5e79:  dup
  IL_5e7a:  ldc.i4.5
  IL_5e7b:  mul
  IL_5e7c:  call       void [System.Console]System.Console::Write(int32)
  IL_5e81:  dup
  IL_5e82:  ldc.i4.0
  IL_5e83:  add
  IL_5e84:  call       void [System.Console]System.Console::Write(int32)
  IL_5e89:  dup
  IL_5e8a:  ldc.i4.1
  IL_5e8b:  xor
  IL_5e8c:  call       void [System.Console]System.Console::Write(int32)
  IL_5e91:  dup
  IL_5e92:  ldc.i4.3
  IL_5e93:  add
  IL_5e94:  call       void [System.Console]System.Console::Write(int32)
  IL_5e99:  dup
  IL_5e9a:  ldc.i4.1
  IL_5e9b:  or
  IL_5e9c:  call       void [System.Console]System.Console::Write(int32)
  IL_5ea1:  dup
  IL_5ea2:  ldc.i4.6
  IL_5ea3:  or
  IL_5ea4:  call       void [System.Console]System.Console::Write(int32)
  IL_5ea9:  dup
  IL_5eaa:  ldc.i4.2
  IL_5eab:  sub
  IL_5eac:  call       void [System.Console]System.Console::Write(int32)
  IL_5eb1:  dup
  IL_5eb2:  ldc.i4.4
  IL_5eb3:  xor
  IL_5eb4:  call       void [System.Console]System.Console::Write(int32)
  IL_5eb9:  dup
  IL_5eba:  ldc.i4.8
  IL_5ebb:  sub
  IL_5ebc:  call       void [System.Console]System.Console::Write(int32)
  IL_5ec1:  dup
  IL_5ec2:  ldc.i4.0
  IL_5ec3:  xor
  IL_5ec4:  call       void [System.Console]System.Console::Write(int32)
  IL_5ec9:  dup
  IL_5eca:  ldc.i4.8
  IL_5ecb:  xor
  IL_5ecc:  call       void [System.Console]System.Console::Write(int32)
  IL_5ed1:  dup
  IL_5ed2:  ldc.i4.4
  IL_5ed3:  or
  IL_5ed4:  call       void [System.Console]System.Console::Write(int32)
  IL_5ed9:  dup
  IL_5eda:  ldc.i4.0
  IL_5edb:  xor
  IL_5edc:  call       void [System.Console]System.Console::Write(int32)
  IL_5ee1:  dup
  IL_5ee2:  ldc.i4.0
  IL_5ee3:  or
  IL_5ee4:  call       void [System.Console]System.Console::Write(int32)
  IL_5ee9:  dup
  IL_5eea:  ldc.i4.8
  IL_5eeb:  add
  IL_5eec:  call       void [System.Console]System.Console::Write(int32)
  IL_5ef1:  dup
  IL_5ef2:  ldc.i4.5
  IL_5ef3:  add
  IL_5ef4:  call       void [System.Console]System.Console::Write(int32)
  IL_5ef9:  dup
  IL_5efa:  ldc.i4.2
  IL_5efb:  or
  IL_5efc:  call       void [System.Console]System.Console::Write(int32)
  IL_5f01:  dup
  IL_5f02:  ldc.i4.1
  IL_5f03:  or
  IL_5f04:  call       void [System.Console]System.Console::Write(int32)
  IL_5f09:  dup
  IL_5f0a:  ldc.i4.2
  IL_5f0b:  sub
  IL_5f0c:  call       void [System.Console]System.Console::Write(int32)
  IL_5f11:  dup
  IL_5f12:  ldc.i4.1
  IL_5f13:  shr
  IL_5f14:  call       void [System.Console]System.Console::Write(int32)
  IL_5f19:  dup
  IL_5f1a:  ldc.i4.7
  IL_5f1b:  xor
  IL_5f1c:  call       void [System.Console]System.Console::Write(int32)
  IL_5f21:  dup
  IL_5f22:  ldc.i4.7
  IL_5f23:  shr
  IL_5f24:  call       void [System.Console]System.Console::Write(int32)
  IL_5f29:  dup
  IL_5f2a:  ldc.i4.7
  IL_5f2b:  sub
  IL_5f2c:  call       void [System.Console]System.Console::Write(int32)
  IL_5f31:  dup
  IL_5f32:  ldc.i4.7
  IL_5f33:  and
  IL_5f34:  call       void [System.Console]System.Console::Write(int32)
  IL_5f39:  dup
  IL_5f3a:  ldc.i4.5
  IL_5f3b:  xor
  IL_5f3c:  call       void [System.Console]System.Console::Write(int32)
  IL_5f41:  dup
  IL_5f42:  ldc.i4.0
  IL_5f43:  or
  IL_5f44:  call       void [System.Console]System.Console::Write(int32)
  IL_5f49:  dup
  IL_5f4a:  ldc.i4.8
  IL_5f4b:  or
  IL_5f4c:  call       void [System.Console]System.Console::Write(int32)
  IL_5f51:  dup
  IL_5f52:  ldc.i4.2
  IL_5f53:  sub
  IL_5f54:  call       void [System.Console]System.Console::Write(int32)
  IL_5f59:  dup
  IL_5f5a:  ldc.i4.7
  IL_5f5b:  add
  IL_5f5c:  call       void [System.Console]System.Console::Write(int32)
  IL_5f61:  dup
  IL_5f62:  ldc.i4.8
  IL_5f63:  shr
  IL_5f64:  call       void [System.Console]System.Console::Write(int32)
  IL_5f69:  dup
  IL_5f6a:  ldc.i4.3
  IL_5f6b:  xor
  IL_5f6c:  call       void [System.Console]System.Console::Write(int32)
  IL_5f71:  dup
  IL_5f72:  ldc.i4.1
  IL_5f73:  add
  IL_5f74:  call       void [System.Console]System.Console::Write(int32)
  IL_5f79:  dup
  IL_5f7a:  ldc.i4.3
  IL_5f7b:  shr
  IL_5f7c:  call       void [System.Console]System.Console::Write(int32)
  IL_5f81:  dup
  IL_5f82:  ldc.i4.5
  IL_5f83:  mul
  IL_5f84:  call       void [System.Console]System.Console::Write(int32)
  IL_5f89:  dup
  IL_5f8a:  ldc.i4.0
  IL_5f8b:  shl
  IL_5f8c:  call       void [System.Console]System.Console::Write(int32)
  IL_5f91:  dup
  IL_5f92:  ldc.i4.1
  IL_5f93:  shr
  IL_5f94:  call       void [System.Console]System.Console::Write(int32)
  IL_5f99:  dup
  IL_5f9a:  ldc.i4.8
  IL_5f9b:  xor
  IL_5f9c:  call       void [System.Console]System.Console::Write(int32)
  IL_5fa1:  dup
  IL_5fa2:  ldc.i4.4
  IL_5fa3:  xor
  IL_5fa4:  call       void [System.Console]System.Console::Write(int32)
  IL_5fa9:  dup
  IL_5faa:  ldc.i4.4
  IL_5fab:  and
  IL_5fac:  call       void [System.Console]System.Console::Write(int32)
  IL_5fb1:  dup
  IL_5fb2:  ldc.i4.1
  IL_5fb3:  or
  IL_5fb4:  call       void [System.Console]System.Console::Write(int32)
  IL_5fb9:  dup
  IL_5fba:  ldc.i4.5
  IL_5fbb:  mul
  IL_5fbc:  call       void [System.Console]System.Console::Write(int32)
  IL_5fc1:  dup
  IL_5fc2:  ldc.i4.3
  IL_5fc3:  xor
  IL_5fc4:  call       void [System.Console]System.Console::Write(int32)
  IL_5fc9:  dup
  IL_5fca:  ldc.i4.0
  IL_5fcb:  shl
  IL_5fcc:  call       void [System.Console]System.Console::Write(int32)
  IL_5fd1:  dup
  IL_5fd2:  ldc.i4.1
  IL_5fd3:  xor
  IL_5fd4:  call       void [System.Console]System.Console::Write(int32)
  IL_5fd9:  dup
  IL_5fda:  ldc.i4.6
  IL_5fdb:  or
  IL_5fdc:  call       void [System.Console]System.Console::Write(int32)
  IL_5fe1:  dup
  IL_5fe2:  ldc.i4.0
  IL_5fe3:  sub
  IL_5fe4:  call       void [System.Console]System.Console::Write(int32)
  IL_5fe9:  dup
  IL_5fea:  ldc.i4.1
  IL_5feb:  xor
  IL_5fec:  call       void [System.Console]System.Console::Write(int32)
  IL_5ff1:  dup
  IL_5ff2:  ldc.i4.2
  IL_5ff3:  xor
  IL_5ff4:  call       void [System.Console]System.Console::Write(int32)
  IL_5ff9:  dup
  IL_5ffa:  ldc.i4.2
  IL_5ffb:  shr
  IL_5ffc:  call       void [System.Console]System.Console::Write(int32)
  IL_6001:  dup
  IL_6002:  ldc.i4.2
  IL_6003:  sub
  IL_6004:  call       void [System.Console]System.Console::Write(int32)
  IL_6009:  dup
  IL_600a:  ldc.i4.4
  IL_600b:  shl
  IL_600c:  call       void [System.Console]System.Console::Write(int32)
  IL_6011:  dup
  IL_6012:  ldc.i4.6
  IL_6013:  or
  IL_6014:  call       void [System.Console]System.Console::Write(int32)
  IL_6019:  dup
  IL_601a:  ldc.i4.3
  IL_601b:  mul
  IL_601c:  call       void [System.Console]System.Console::Write(int32)
  IL_6021:  dup
  IL_6022:  ldc.i4.0
  IL_6023:  shl
  IL_6024:  call       void [System.Console]System.Console::Write(int32)
  IL_6029:  dup
  IL_602a:  ldc.i4.3
  IL_602b:  and
  IL_602c:  call       void [System.Console]System.Console::Write(int32)
  IL_6031:  dup
  IL_6032:  ldc.i4.6
  IL_6033:  add
  IL_6034:  call       void [System.Console]System.Console::Write(int32)
  IL_6039:  dup
  IL_603a:  ldc.i4.8
  IL_603b:  add
  IL_603c:  call       void [System.Console]System.Console::Write(int32)
  IL_6041:  dup
  IL_6042:  ldc.i4.1
  IL_6043:  add
  IL_6044:  call       void [System.Console]System.Console::Write(int32)
  IL_6049:  dup
  IL_604a:  ldc.i4.0
  IL_604b:  and
  IL_604c:  call       void [System.Console]System.Console::Write(int32)
  IL_6051:  dup
  IL_6052:  ldc.i4.0
  IL_6053:  and
  IL_6054:  call       void [System.Console]System.Console::Write(int32)
  IL_6059:  dup
  IL_605a:  ldc.i4.4
  IL_605b:  mul
  IL_605c:  call       void [System.Console]System.Console::Write(int32)
  IL_6061:  dup
  IL_6062:  ldc.i4.4
  IL_6063:  sub
  IL_6064:  call       void [System.Console]System.Console::Write(int32)
  IL_6069:  dup
  IL_606a:  ldc.i4.5
  IL_606b:  or
  IL_606c:  call       void [System.Console]System.Console::Write(int32)
  IL_6071:  dup
  IL_6072:  ldc.i4.4
  IL_6073:  xor
  IL_6074:  call       void [System.Console]System.Console::Write(int32)
  IL_6079:  dup
  IL_607a:  ldc.i4.1
  IL_607b:  add
  IL_607c:  call       void [System.Console]System.Console::Write(int32)
  IL_6081:  dup
  IL_6082:  ldc.i4.1
  IL_6083:  and
  IL_6084:  call       void [System.Console]System.Console::Write(int32)
  IL_6089:  dup
  IL_608a:  ldc.i4.4
  IL_608b:  or
  IL_608c:  call       void [System.Console]System.Console::Write(int32)
  IL_6091:  dup
  IL_6092:  ldc.i4.4
  IL_6093:  or
  IL_6094:  call       void [System.Console]System.Console::Write(int32)
  IL_6099:  dup
  IL_609a:  ldc.i4.4
  IL_609b:  xor
  IL_609c:  call       void [System.Console]System.Console::Write(int32)
  IL_60a1:  dup
  IL_60a2:  ldc.i4.0
  IL_60a3:  and
  IL_60a4:  call       void [System.Console]System.Console::Write(int32)
  IL_60a9:  dup
  IL_60aa:  ldc.i4.4
  IL_60ab:  shr
  IL_60ac:  call       void [System.Console]System.Console::Write(int32)
  IL_60b1:  dup
  IL_60b2:  ldc.i4.8
  IL_60b3:  or
  IL_60b4:  call       void [System.Console]System.Console::Write(int32)
  IL_60b9:  dup
  IL_60ba:  ldc.i4.1
  IL_60bb:  or
  IL_60bc:  call       void [System.Console]System.Console::Write(int32)
  IL_60c1:  dup
  IL_60c2:  ldc.i4.2
  IL_60c3:  sub
  IL_60c4:  call       void [System.Console]System.Console::Write(int32)
  IL_60c9:  dup
  IL_60ca:  ldc.i4.2
  IL_60cb:  sub
  IL_60cc:  call       void [System.Console]System.Console::Write(int32)
  IL_60d1:  dup
  IL_60d2:  ldc.i4.2
  IL_60d3:  xor
  IL_60d4:  call       void [System.Console]System.Console::Write(int32)
  IL_60d9:  dup
  IL_60da:  ldc.i4.6
  IL_60db:  shl
  IL_60dc:  call       void [System.Console]System.Console::Write(int32)
  IL_60e1:  dup
  IL_60e2:  ldc.i4.6
  IL_60e3:  sub
  IL_60e4:  call       void [System.Console]System.Console::Write(int32)
  IL_60e9:  dup
  IL_60ea:  ldc.i4.0
  IL_60eb:  shr
  IL_60ec:  call       void [System.Console]System.Console::Write(int32)
  IL_60f1:  dup
  IL_60f2:  ldc.i4.3
  IL_60f3:  xor
  IL_60f4:  call       void [System.Console]System.Console::Write(int32)
  IL_60f9:  dup
  IL_60fa:  ldc.i4.6
  IL_60fb:  shl
  IL_60fc:  call       void [System.Console]System.Console::Write(int32)
  IL_6101:  dup
  IL_6102:  ldc.i4.3
  IL_6103:  mul
  IL_6104:  call       void [System.Console]System.Console::Write(int32)
  IL_6109:  dup
  IL_610a:  ldc.i4.5
  IL_610b:  and
  IL_610c:  call       void [System.Console]System.Console::Write(int32)
  IL_6111:  dup
  IL_6112:  ldc.i4.5
  IL_6113:  or
  IL_6114:  call       void [System.Console]System.Console::Write(int32)
  IL_6119:  dup
  IL_611a:  ldc.i4.5
  IL_611b:  sub
  IL_611c:  call       void [System.Console]System.Console::Write(int32)
  IL_6121:  dup
  IL_6122:  ldc.i4.5
  IL_6123:  shr
  IL_6124:  call       void [System.Console]System.Console::Write(int32)
  IL_6129:  dup
  IL_612a:  ldc.i4.6
  IL_612b:  sub
  IL_612c:  call       void [System.Console]System.Console::Write(int32)
  IL_6131:  dup
  IL_6132:  ldc.i4.2
  IL_6133:  and
  IL_6134:  call       void [System.Console]System.Console::Write(int32)
  IL_6139:  dup
  IL_613a:  ldc.i4.2
  IL_613b:  shr
  IL_613c:  call       void [System.Console]System.Console::Write(int32)
  IL_6141:  dup
  IL_6142:  ldc.i4.8
  IL_6143:  shl
  IL_6144:  call       void [System.Console]System.Console::Write(int32)
  IL_6149:  dup
  IL_614a:  ldc.i4.6
  IL_614b:  xor
  IL_614c:  call       void [System.Console]System.Console::Write(int32)
  IL_6151:  dup
  IL_6152:  ldc.i4.1
  IL_6153:  or
  IL_6154:  call       void [System.Console]System.Console::Write(int32)
  IL_6159:  dup
  IL_615a:  ldc.i4.6
  IL_615b:  shr
  IL_615c:  call       void [System.Console]System.Console::Write(int32)
  IL_6161:  dup
  IL_6162:  ldc.i4.0
  IL_6163:  or
  IL_6164:  call       void [System.Console]System.Console::Write(int32)
  IL_6169:  dup
  IL_616a:  ldc.i4.2
  IL_616b:  sub
  IL_616c:  call       void [System.Console]System.Console::Write(int32)
  IL_6171:  dup
  IL_6172:  ldc.i4.8
  IL_6173:  and
  IL_6174:  call       void [System.Console]System.Console::Write(int32)
  IL_6179:  dup
  IL_617a:  ldc.i4.0
  IL_617b:  shr
  IL_617c:  call       void [System.Console]System.Console::Write(int32)
  IL_6181:  dup
  IL_6182:  ldc.i4.0
  IL_6183:  or
  IL_6184:  call       void [System.Console]System.Console::Write(int32)
  IL_6189:  dup
  IL_618a:  ldc.i4.2
  IL_618b:  shr
  IL_618c:  call       void [System.Console]System.Console::Write(int32)
  IL_6191:  dup
  IL_6192:  ldc.i4.2
  IL_6193:  mul
  IL_6194:  call       void [System.Console]System.Console::Write(int32)
  IL_6199:  dup
  IL_619a:  ldc.i4.3
  IL_619b:  mul
  IL_619c:  call       void [System.Console]System.Console::Write(int32)
  IL_61a1:  dup
  IL_61a2:  ldc.i4.5
  IL_61a3:  sub
  IL_61a4:  call       void [System.Console]System.Console::Write(int32)
  IL_61a9:  dup
  IL_61aa:  ldc.i4.5
  IL_61ab:  shr
  IL_61ac:  call       void [System.Console]System.Console::Write(int32)
  IL_61b1:  dup
  IL_61b2:  ldc.i4.6
  IL_61b3:  xor
  IL_61b4:  call       void [System.Console]System.Console::Write(int32)
  IL_61b9:  dup
  IL_61ba:  ldc.i4.2
  IL_61bb:  or
  IL_61bc:  call       void [System.Console]System.Console::Write(int32)
  IL_61c1:  dup
  IL_61c2:  ldc.i4.8
  IL_61c3:  xor
  IL_61c4:  call       void [System.Console]System.Console::Write(int32)
  IL_61c9:  dup
  IL_61ca:  ldc.i4.6
  IL_61cb:  and
  IL_61cc:  call       void [System.Console]System.Console::Write(int32)
  IL_61d1:  dup
  IL_61d2:  ldc.i4.6
  IL_61d3:  mul
  IL_61d4:  call       void [System.Console]System.Console::Write(int32)
  IL_61d9:  dup
  IL_61da:  ldc.i4.3
  IL_61db:  xor
  IL_61dc:  call       void [System.Console]System.Console::Write(int32)
  IL_61e1:  dup
  IL_61e2:  ldc.i4.4
  IL_61e3:  and
  IL_61e4:  call       void [System.Console]System.Console::Write(int32)
  IL_61e9:  dup
  IL_61ea:  ldc.i4.6
  IL_61eb:  shl
  IL_61ec:  call       void [System.Console]System.Console::Write(int32)
  IL_61f1:  dup
  IL_61f2:  ldc.i4.6
  IL_61f3:  mul
  IL_61f4:  call       void [System.Console]System.Console::Write(int32)
  IL_61f9:  dup
  IL_61fa:  ldc.i4.2
  IL_61fb:  and
  IL_61fc:  call       void [System.Console]System.Console::Write(int32)
  IL_6201:  dup
  IL_6202:  ldc.i4.5
  IL_6203:  add
  IL_6204:  call       void [System.Console]System.Console::Write(int32)
  IL_6209:  dup
  IL_620a:  ldc.i4.1
  IL_620b:  sub
  IL_620c:  call       void [System.Console]System.Console::Write(int32)
  IL_6211:  dup
  IL_6212:  ldc.i4.4
  IL_6213:  shl
  IL_6214:  call       void [System.Console]System.Console::Write(int32)
  IL_6219:  dup
  IL_621a:  ldc.i4.3
  IL_621b:  shl
  IL_621c:  call       void [System.Console]System.Console::Write(int32)
  IL_6221:  dup
  IL_6222:  ldc.i4.1
  IL_6223:  xor
  IL_6224:  call       void [System.Console]System.Console::Write(int32)
  IL_6229:  dup
  IL_622a:  ldc.i4.7
  IL_622b:  add
  IL_622c:  call       void [System.Console]System.Console::Write(int32)
  IL_6231:  dup
  IL_6232:  ldc.i4.3
  IL_6233:  add
  IL_6234:  call       void [System.Console]System.Console::Write(int32)
  IL_6239:  dup
  IL_623a:  ldc.i4.8
  IL_623b:  shr
  IL_623c:  call       void [System.Console]System.Console::Write(int32)
  IL_6241:  dup
  IL_6242:  ldc.i4.5
  IL_6243:  mul
  IL_6244:  call       void [System.Console]System.Console::Write(int32)
  IL_6249:  dup
  IL_624a:  ldc.i4.2
  IL_624b:  shl
  IL_624c:  call       void [System.Console]System.Console::Write(int32)
  IL_6251:  dup
  IL_6252:  ldc.i4.1
  IL_6253:  mul
  IL_6254:  call       void [System.Console]System.Console::Write(int32)
  IL_6259:  dup
  IL_625a:  ldc.i4.8
  IL_625b:  and
  IL_625c:  call       void [System.Console]System.Console::Write(int32)
  IL_6261:  dup
  IL_6262:  ldc.i4.5
  IL_6263:  or
  IL_6264:  call       void [System.Console]System.Console::Write(int32)
  IL_6269:  dup
  IL_626a:  ldc.i4.1
  IL_626b:  xor
  IL_626c:  call       void [System.Console]System.Console::Write(int32)
  IL_6271:  dup
  IL_6272:  ldc.i4.5
  IL_6273:  and
  IL_6274:  call       void [System.Console]System.Console::Write(int32)
  IL_6279:  dup
  IL_627a:  ldc.i4.4
  IL_627b:  sub
  IL_627c:  call       void [System.Console]System.Console::Write(int32)
  IL_6281:  dup
  IL_6282:  ldc.i4.7
  IL_6283:  and
  IL_6284:  call       void [System.Console]System.Console::Write(int32)
  IL_6289:  dup
  IL_628a:  ldc.i4.0
  IL_628b:  shr
  IL_628c:  call       void [System.Console]System.Console::Write(int32)
  IL_6291:  dup
  IL_6292:  ldc.i4.2
  IL_6293:  add
  IL_6294:  call       void [System.Console]System.Console::Write(int32)
  IL_6299:  dup
  IL_629a:  ldc.i4.3
  IL_629b:  xor
  IL_629c:  call       void [System.Console]System.Console::Write(int32)
  IL_62a1:  dup
  IL_62a2:  ldc.i4.0
  IL_62a3:  add
  IL_62a4:  call       void [System.Console]System.Console::Write(int32)
  IL_62a9:  dup
  IL_62aa:  ldc.i4.5
  IL_62ab:  mul
  IL_62ac:  call       void [System.Console]System.Console::Write(int32)
  IL_62b1:  dup
  IL_62b2:  ldc.i4.4
  IL_62b3:  xor
  IL_62b4:  call       void [System.Console]System.Console::Write(int32)
  IL_62b9:  dup
  IL_62ba:  ldc.i4.0
  IL_62bb:  xor
  IL_62bc:  call       void [System.Console]System.Console::Write(int32)
  IL_62c1:  dup
  IL_62c2:  ldc.i4.7
  IL_62c3:  add
  IL_62c4:  call       void [System.Console]System.Console::Write(int32)
  IL_62c9:  dup
  IL_62ca:  ldc.i4.8
  IL_62cb:  xor
  IL_62cc:  call       void [System.Console]System.Console::Write(int32)
  IL_62d1:  dup
  IL_62d2:  ldc.i4.0
  IL_62d3:  shr
  IL_62d4:  call       void [System.Console]System.Console::Write(int32)
  IL_62d9:  dup
  IL_62da:  ldc.i4.5
  IL_62db:  mul
  IL_62dc:  call       void [System.Console]System.Console::Write(int32)
  IL_62e1:  dup
  IL_62e2:  ldc.i4.1
  IL_62e3:  xor
  IL_62e4:  call       void [System.Console]System.Console::Write(int32)
  IL_62e9:  dup
  IL_62ea:  ldc.i4.5
  IL_62eb:  shl
  IL_62ec:  call       void [System.Console]System.Console::Write(int32)
  IL_62f1:  dup
  IL_62f2:  ldc.i4.3
  IL_62f3:  or
  IL_62f4:  call       void [System.Console]System.Console::Write(int32)
  IL_62f9:  dup
  IL_62fa:  ldc.i4.0
  IL_62fb:  mul
  IL_62fc:  call       void [System.Console]System.Console::Write(int32)
  IL_6301:  dup
  IL_6302:  ldc.i4.8
  IL_6303:  shl
  IL_6304:  call       void [System.Console]System.Console::Write(int32)
  IL_6309:  dup
  IL_630a:  ldc.i4.1
  IL_630b:  shl
  IL_630c:  call       void [System.Console]System.Console::Write(int32)
  IL_6311:  dup
  IL_6312:  ldc.i4.8
  IL_6313:  mul
  IL_6314:  call       void [System.Console]System.Console::Write(int32)
  IL_6319:  dup
  IL_631a:  ldc.i4.3
  IL_631b:  shl
  IL_631c:  call       void [System.Console]System.Console::Write(int32)
  IL_6321:  dup
  IL_6322:  ldc.i4.8
  IL_6323:  or
  IL_6324:  call       void [System.Console]System.Console::Write(int32)
  IL_6329:  dup
  IL_632a:  ldc.i4.4
  IL_632b:  add
  IL_632c:  call       void [System.Console]System.Console::Write(int32)
  IL_6331:  dup
  IL_6332:  ldc.i4.5
  IL_6333:  shl
  IL_6334:  call       void [System.Console]System.Console::Write(int32)
  IL_6339:  dup
  IL_633a:  ldc.i4.2
  IL_633b:  sub
  IL_633c:  call       void [System.Console]System.Console::Write(int32)
  IL_6341:  dup
  IL_6342:  ldc.i4.7
  IL_6343:  shr
  IL_6344:  call       void [System.Console]System.Console::Write(int32)
  IL_6349:  dup
  IL_634a:  ldc.i4.5
  IL_634b:  xor
  IL_634c:  call       void [System.Console]System.Console::Write(int32)
  IL_6351:  dup
  IL_6352:  ldc.i4.7
  IL_6353:  add
  IL_6354:  call       void [System.Console]System.Console::Write(int32)
  IL_6359:  dup
  IL_635a:  ldc.i4.1
  IL_635b:  sub
  IL_635c:  call       void [System.Console]System.Console::Write(int32)
  IL_6361:  dup
  IL_6362:  ldc.i4.1
  IL_6363:  and
  IL_6364:  call       void [System.Console]System.Console::Write(int32)
  IL_6369:  dup
  IL_636a:  ldc.i4.4
  IL_636b:  or
  IL_636c:  call       void [System.Console]System.Console::Write(int32)
  IL_6371:  dup
  IL_6372:  ldc.i4.3
  IL_6373:  and
  IL_6374:  call       void [System.Console]System.Console::Write(int32)
  IL_6379:  dup
  IL_637a:  ldc.i4.0
  IL_637b:  sub
  IL_637c:  call       void [System.Console]System.Console::Write(int32)
  IL_6381:  dup
  IL_6382:  ldc.i4.0
  IL_6383:  mul
  IL_6384:  call       void [System.Console]System.Console::Write(int32)
  IL_6389:  dup
  IL_638a:  ldc.i4.3
  IL_638b:  and
  IL_638c:  call       void [System.Console]System.Console::Write(int32)
  IL_6391:  dup
  IL_6392:  ldc.i4.2
  IL_6393:  xor
  IL_6394:  call       void [System.Console]System.Console::Write(int32)
  IL_6399:  dup
  IL_639a:  ldc.i4.4
  IL_639b:  shl
  IL_639c:  call       void [System.Console]System.Console::Write(int32)
  IL_63a1:  dup
  IL_63a2:  ldc.i4.6
  IL_63a3:  mul
  IL_63a4:  call       void [System.Console]System.Console::Write(int32)
  IL_63a9:  dup
  IL_63aa:  ldc.i4.4
  IL_63ab:  sub
  IL_63ac:  call       void [System.Console]System.Console::Write(int32)
  IL_63b1:  dup
  IL_63b2:  ldc.i4.2
  IL_63b3:  shl
  IL_63b4:  call       void [System.Console]System.Console::Write(int32)
  IL_63b9:  dup
  IL_63ba:  ldc.i4.6
  IL_63bb:  shl
  IL_63bc:  call       void [System.Console]System.Console::Write(int32)
  IL_63c1:  dup
  IL_63c2:  ldc.i4.1
  IL_63c3:  xor
  IL_63c4:  call       void [System.Console]System.Console::Write(int32)
  IL_63c9:  dup
  IL_63ca:  ldc.i4.7
  IL_63cb:  shr
  IL_63cc:  call       void [System.Console]System.Console::Write(int32)
  IL_63d1:  dup
  IL_63d2:  ldc.i4.2
  IL_63d3:  shl
  IL_63d4:  call       void [System.Console]System.Console::Write(int32)
  IL_63d9:  dup
  IL_63da:  ldc.i4.4
  IL_63db:  and
  IL_63dc:  call       void [System.Console]System.Console::Write(int32)
  IL_63e1:  dup
  IL_63e2:  ldc.i4.3
  IL_63e3:  or
  IL_63e4:  call       void [System.Console]System.Console::Write(int32)
  IL_63e9:  dup
  IL_63ea:  ldc.i4.3
  IL_63eb:  sub
  IL_63ec:  call       void [System.Console]System.Console::Write(int32)
  IL_63f1:  dup
  IL_63f2:  ldc.i4.6
  IL_63f3:  xor
  IL_63f4:  call       void [System.Console]System.Console::Write(int32)
  IL_63f9:  dup
  IL_63fa:  ldc.i4.1
  IL_63fb:  sub
  IL_63fc:  call       void [System.Console]System.Console::Write(int32)
  IL_6401:  dup
  IL_6402:  ldc.i4.7
  IL_6403:  mul
  IL_6404:  call       void [System.Console]System.Console::Write(int32)
  IL_6409:  dup
  IL_640a:  ldc.i4.8
  IL_640b:  xor
  IL_640c:  call       void [System.Console]System.Console::Write(int32)
  IL_6411:  dup
  IL_6412:  ldc.i4.1
  IL_6413:  xor
  IL_6414:  call       void [System.Console]System.Console::Write(int32)
  IL_6419:  dup
  IL_641a:  ldc.i4.8
  IL_641b:  xor
  IL_641c:  call       void [System.Console]System.Console::Write(int32)
  IL_6421:  dup
  IL_6422:  ldc.i4.1
  IL_6423:  add
  IL_6424:  call       void [System.Console]System.Console::Write(int32)
  IL_6429:  dup
  IL_642a:  ldc.i4.6
  IL_642b:  shl
  IL_642c:  call       void [System.Console]System.Console::Write(int32)
  IL_6431:  dup
  IL_6432:  ldc.i4.4
  IL_6433:  or
  IL_6434:  call       void [System.Console]System.Console::Write(int32)
  IL_6439:  dup
  IL_643a:  ldc.i4.7
  IL_643b:  shl
  IL_643c:  call       void [System.Console]System.Console::Write(int32)
  IL_6441:  dup
  IL_6442:  ldc.i4.0
  IL_6443:  or
  IL_6444:  call       void [System.Console]System.Console::Write(int32)
  IL_6449:  dup
  IL_644a:  ldc.i4.6
  IL_644b:  or
  IL_644c:  call       void [System.Console]System.Console::Write(int32)
  IL_6451:  dup
  IL_6452:  ldc.i4.8
  IL_6453:  mul
  IL_6454:  call       void [System.Console]System.Console::Write(int32)
  IL_6459:  dup
  IL_645a:  ldc.i4.8
  IL_645b:  shl
  IL_645c:  call       void [System.Console]System.Console::Write(int32)
  IL_6461:  dup
  IL_6462:  ldc.i4.6
  IL_6463:  shl
  IL_6464:  call       void [System.Console]System.Console::Write(int32)
  IL_6469:  dup
  IL_646a:  ldc.i4.3
  IL_646b:  mul
  IL_646c:  call       void [System.Console]System.Console::Write(int32)
  IL_6471:  dup
  IL_6472:  ldc.i4.0
  IL_6473:  mul
  IL_6474:  call       void [System.Console]System.Console::Write(int32)
  IL_6479:  dup
  IL_647a:  ldc.i4.7
  IL_647b:  mul
  IL_647c:  call       void [System.Console]System.Console::Write(int32)
  IL_6481:  dup
  IL_6482:  ldc.i4.0
  IL_6483:  mul
  IL_6484:  call       void [System.Console]System.Console::Write(int32)
  IL_6489:  dup
  IL_648a:  ldc.i4.2
  IL_648b:  add
  IL_648c:  call       void [System.Console]System.Console::Write(int32)
  IL_6491:  dup
  IL_6492:  ldc.i4.5
  IL_6493:  mul
  IL_6494:  call       void [System.Console]System.Console::Write(int32)
  IL_6499:  dup
  IL_649a:  ldc.i4.3
  IL_649b:  shr
  IL_649c:  call       void [System.Console]System.Console::Write(int32)
  IL_64a1:  dup
  IL_64a2:  ldc.i4.4
  IL_64a3:  xor
  IL_64a4:  call       void [System.Console]System.Console::Write(int32)
  IL_64a9:  dup
  IL_64aa:  ldc.i4.4
  IL_64ab:  sub
  IL_64ac:  call       void [System.Console]System.Console::Write(int32)
  IL_64b1:  dup
  IL_64b2:  ldc.i4.7
  IL_64b3:  or
  IL_64b4:  call       void [System.Console]System.Console::Write(int32)
  IL_64b9:  dup
  IL_64ba:  ldc.i4.2
  IL_64bb:  xor
  IL_64bc:  call       void [System.Console]System.Console::Write(int32)
  IL_64c1:  dup
  IL_64c2:  ldc.i4.1
  IL_64c3:  and
  IL_64c4:  call       void [System.Console]System.Console::Write(int32)
  IL_64c9:  dup
  IL_64ca:  ldc.i4.0
  IL_64cb:  and
  IL_64cc:  call       void [System.Console]System.Console::Write(int32)
  IL_64d1:  dup
  IL_64d2:  ldc.i4.6
  IL_64d3:  mul
  IL_64d4:  call       void [System.Console]System.Console::Write(int32)
  IL_64d9:  dup
  IL_64da:  ldc.i4.1
  IL_64db:  shl
  IL_64dc:  call       void [System.Console]System.Console::Write(int32)
  IL_64e1:  dup
  IL_64e2:  ldc.i4.3
  IL_64e3:  xor
  IL_64e4:  call       void [System.Console]System.Console::Write(int32)
  IL_64e9:  dup
  IL_64ea:  ldc.i4.5
  IL_64eb:  shl
  IL_64ec:  call       void [System.Console]System.Console::Write(int32)
  IL_64f1:  dup
  IL_64f2:  ldc.i4.8
  IL_64f3:  shr
  IL_64f4:  call       void [System.Console]System.Console::Write(int32)
  IL_64f9:  dup
  IL_64fa:  ldc.i4.3
  IL_64fb:  or
  IL_64fc:  call       void [System.Console]System.Console::Write(int32)
  IL_6501:  dup
  IL_6502:  ldc.i4.2
  IL_6503:  xor
  IL_6504:  call       void [System.Console]System.Console::Write(int32)
  IL_6509:  dup
  IL_650a:  ldc.i4.3
  IL_650b:  sub
  IL_650c:  call       void [System.Console]System.Console::Write(int32)
  IL_6511:  dup
  IL_6512:  ldc.i4.6
  IL_6513:  and
  IL_6514:  call       void [System.Console]System.Console::Write(int32)
  IL_6519:  dup
  IL_651a:  ldc.i4.8
  IL_651b:  and
  IL_651c:  call       void [System.Console]System.Console::Write(int32)
  IL_6521:  dup
  IL_6522:  ldc.i4.5
  IL_6523:  add
  IL_6524:  call       void [System.Console]System.Console::Write(int32)
  IL_6529:  dup
  IL_652a:  ldc.i4.3
  IL_652b:  shl
  IL_652c:  call       void [System.Console]System.Console::Write(int32)
  IL_6531:  dup
  IL_6532:  ldc.i4.7
  IL_6533:  shr
  IL_6534:  call       void [System.Console]System.Console::Write(int32)
  IL_6539:  dup
  IL_653a:  ldc.i4.5
  IL_653b:  and
  IL_653c:  call       void [System.Console]System.Console::Write(int32)
  IL_6541:  dup
  IL_6542:  ldc.i4.2
  IL_6543:  or
  IL_6544:  call       void [System.Console]System.Console::Write(int32)
  IL_6549:  dup
  IL_654a:  ldc.i4.1
  IL_654b:  sub
  IL_654c:  call       void [System.Console]System.Console::Write(int32)
  IL_6551:  dup
  IL_6552:  ldc.i4.2
  IL_6553:  add
  IL_6554:  call       void [System.Console]System.Console::Write(int32)
  IL_6559:  dup
  IL_655a:  ldc.i4.4
  IL_655b:  and
  IL_655c:  call       void [System.Console]System.Console::Write(int32)
  IL_6561:  dup
  IL_6562:  ldc.i4.1
  IL_6563:  and
  IL_6564:  call       void [System.Console]System.Console::Write(int32)
  IL_6569:  dup
  IL_656a:  ldc.i4.5
  IL_656b:  sub
  IL_656c:  call       void [System.Console]System.Console::Write(int32)
  IL_6571:  dup
  IL_6572:  ldc.i4.5
  IL_6573:  and
  IL_6574:  call       void [System.Console]System.Console::Write(int32)
  IL_6579:  dup
  IL_657a:  ldc.i4.6
  IL_657b:  and
  IL_657c:  call       void [System.Console]System.Console::Write(int32)
  IL_6581:  dup
  IL_6582:  ldc.i4.0
  IL_6583:  shl
  IL_6584:  call       void [System.Console]System.Console::Write(int32)
  IL_6589:  dup
  IL_658a:  ldc.i4.0
  IL_658b:  xor
  IL_658c:  call       void [System.Console]System.Console::Write(int32)
  IL_6591:  dup
  IL_6592:  ldc.i4.4
  IL_6593:  add
  IL_6594:  call       void [System.Console]System.Console::Write(int32)
  IL_6599:  dup
  IL_659a:  ldc.i4.6
  IL_659b:  mul
  IL_659c:  call       void [System.Console]System.Console::Write(int32)
  IL_65a1:  dup
  IL_65a2:  ldc.i4.8
  IL_65a3:  mul
  IL_65a4:  call       void [System.Console]System.Console::Write(int32)
  IL_65a9:  dup
  IL_65aa:  ldc.i4.1
  IL_65ab:  shr
  IL_65ac:  call       void [System.Console]System.Console::Write(int32)
  IL_65b1:  dup
  IL_65b2:  ldc.i4.8
  IL_65b3:  shl
  IL_65b4:  call       void [System.Console]System.Console::Write(int32)
  IL_65b9:  dup
  IL_65ba:  ldc.i4.5
  IL_65bb:  shr
  IL_65bc:  call       void [System.Console]System.Console::Write(int32)
  IL_65c1:  dup
  IL_65c2:  ldc.i4.7
  IL_65c3:  and
  IL_65c4:  call       void [System.Console]System.Console::Write(int32)
  IL_65c9:  dup
  IL_65ca:  ldc.i4.2
  IL_65cb:  or
  IL_65cc:  call       void [System.Console]System.Console::Write(int32)
  IL_65d1:  dup
  IL_65d2:  ldc.i4.4
  IL_65d3:  sub
  IL_65d4:  call       void [System.Console]System.Console::Write(int32)
  IL_65d9:  dup
  IL_65da:  ldc.i4.5
  IL_65db:  sub
  IL_65dc:  call       void [System.Console]System.Console::Write(int32)
  IL_65e1:  dup
  IL_65e2:  ldc.i4.7
  IL_65e3:  shr
  IL_65e4:  call       void [System.Console]System.Console::Write(int32)
  IL_65e9:  dup
  IL_65ea:  ldc.i4.8
  IL_65eb:  or
  IL_65ec:  call       void [System.Console]System.Console::Write(int32)
  IL_65f1:  dup
  IL_65f2:  ldc.i4.1
  IL_65f3:  shr
  IL_65f4:  call       void [System.Console]System.Console::Write(int32)
  IL_65f9:  dup
  IL_65fa:  ldc.i4.1
  IL_65fb:  and
  IL_65fc:  call       void [System.Console]System.Console::Write(int32)
  IL_6601:  dup
  IL_6602:  ldc.i4.3
  IL_6603:  shl
  IL_6604:  call       void [System.Console]System.Console::Write(int32)
  IL_6609:  dup
  IL_660a:  ldc.i4.4
  IL_660b:  mul
  IL_660c:  call       void [System.Console]System.Console::Write(int32)
  IL_6611:  dup
  IL_6612:  ldc.i4.6
  IL_6613:  sub
  IL_6614:  call       void [System.Console]System.Console::Write(int32)
  IL_6619:  dup
  IL_661a:  ldc.i4.2
  IL_661b:  sub
  IL_661c:  call       void [System.Console]System.Console::Write(int32)
  IL_6621:  dup
  IL_6622:  ldc.i4.2
  IL_6623:  and
  IL_6624:  call       void [System.Console]System.Console::Write(int32)
  IL_6629:  dup
  IL_662a:  ldc.i4.1
  IL_662b:  or
  IL_662c:  call       void [System.Console]System.Console::Write(int32)
  IL_6631:  dup
  IL_6632:  ldc.i4.8
  IL_6633:  or
  IL_6634:  call       void [System.Console]System.Console::Write(int32)
  IL_6639:  dup
  IL_663a:  ldc.i4.8
  IL_663b:  shl
  IL_663c:  call       void [System.Console]System.Console::Write(int32)
  IL_6641:  dup
  IL_6642:  ldc.i4.1
  IL_6643:  or
  IL_6644:  call       void [System.Console]System.Console::Write(int32)
  IL_6649:  dup
  IL_664a:  ldc.i4.4
  IL_664b:  and
  IL_664c:  call       void [System.Console]System.Console::Write(int32)
  IL_6651:  dup
  IL_6652:  ldc.i4.5
  IL_6653:  shr
  IL_6654:  call       void [System.Console]System.Console::Write(int32)
  IL_6659:  dup
  IL_665a:  ldc.i4.3
  IL_665b:  or
  IL_665c:  call       void [System.Console]System.Console::Write(int32)
  IL_6661:  dup
  IL_6662:  ldc.i4.8
  IL_6663:  and
  IL_6664:  call       void [System.Console]System.Console::Write(int32)
  IL_6669:  dup
  IL_666a:  ldc.i4.5
  IL_666b:  and
  IL_666c:  call       void [System.Console]System.Console::Write(int32)
  IL_6671:  dup
  IL_6672:  ldc.i4.1
  IL_6673:  and
  IL_6674:  call       void [System.Console]System.Console::Write(int32)
  IL_6679:  dup
  IL_667a:  ldc.i4.1
  IL_667b:  mul
  IL_667c:  call       void [System.Console]System.Console::Write(int32)
  IL_6681:  dup
  IL_6682:  ldc.i4.1
  IL_6683:  xor
  IL_6684:  call       void [System.Console]System.Console::Write(int32)
  IL_6689:  dup
  IL_668a:  ldc.i4.2
  IL_668b:  mul
  IL_668c:  call       void [System.Console]System.Console::Write(int32)
  IL_6691:  dup
  IL_6692:  ldc.i4.0
  IL_6693:  mul
  IL_6694:  call       void [System.Console]System.Console::Write(int32)
  IL_6699:  dup
  IL_669a:  ldc.i4.0
  IL_669b:  shl
  IL_669c:  call       void [System.Console]System.Console::Write(int32)
  IL_66a1:  dup
  IL_66a2:  ldc.i4.4
  IL_66a3:  add
  IL_66a4:  call       void [System.Console]System.Console::Write(int32)
  IL_66a9:  dup
  IL_66aa:  ldc.i4.6
  IL_66ab:  shr
  IL_66ac:  call       void [System.Console]System.Console::Write(int32)
  IL_66b1:  dup
  IL_66b2:  ldc.i4.8
  IL_66b3:  shr
  IL_66b4:  call       void [System.Console]System.Console::Write(int32)
  IL_66b9:  dup
  IL_66ba:  ldc.i4.2
  IL_66bb:  or
  IL_66bc:  call       void [System.Console]System.Console::Write(int32)
  IL_66c1:  dup
  IL_66c2:  ldc.i4.6
  IL_66c3:  xor
  IL_66c4:  call       void [System.Console]System.Console::Write(int32)
  IL_66c9:  dup
  IL_66ca:  ldc.i4.6
  IL_66cb:  sub
  IL_66cc:  call       void [System.Console]System.Console::Write(int32)
  IL_66d1:  dup
  IL_66d2:  ldc.i4.1
  IL_66d3:  mul
  IL_66d4:  call       void [System.Console]System.Console::Write(int32)
  IL_66d9:  dup
  IL_66da:  ldc.i4.1
  IL_66db:  shr
  IL_66dc:  call       void [System.Console]System.Console::Write(int32)
  IL_66e1:  dup
  IL_66e2:  ldc.i4.1
  IL_66e3:  shl
  IL_66e4:  call       void [System.Console]System.Console::Write(int32)
  IL_66e9:  dup
  IL_66ea:  ldc.i4.3
  IL_66eb:  and
  IL_66ec:  call       void [System.Console]System.Console::Write(int32)
  IL_66f1:  dup
  IL_66f2:  ldc.i4.5
  IL_66f3:  xor
  IL_66f4:  call       void [System.Console]System.Console::Write(int32)
  IL_66f9:  dup
  IL_66fa:  ldc.i4.0
  IL_66fb:  mul
  IL_66fc:  call       void [System.Console]System.Console::Write(int32)
  IL_6701:  dup
  IL_6702:  ldc.i4.6
  IL_6703:  or
  IL_6704:  call       void [System.Console]System.Console::Write(int32)
  IL_6709:  dup
  IL_670a:  ldc.i4.7
  IL_670b:  shl
  IL_670c:  call       void [System.Console]System.Console::Write(int32)
  IL_6711:  dup
  IL_6712:  ldc.i4.5
  IL_6713:  shl
  IL_6714:  call       void [System.Console]System.Console::Write(int32)
  IL_6719:  dup
  IL_671a:  ldc.i4.4
  IL_671b:  and
  IL_671c:  call       void [System.Console]System.Console::Write(int32)
  IL_6721:  dup
  IL_6722:  ldc.i4.8
  IL_6723:  and
  IL_6724:  call       void [System.Console]System.Console::Write(int32)
  IL_6729:  dup
  IL_672a:  ldc.i4.3
  IL_672b:  xor
  IL_672c:  call       void [System.Console]System.Console::Write(int32)
  IL_6731:  dup
  IL_6732:  ldc.i4.8
  IL_6733:  shl
  IL_6734:  call       void [System.Console]System.Console::Write(int32)
  IL_6739:  dup
  IL_673a:  ldc.i4.8
  IL_673b:  sub
  IL_673c:  call       void [System.Console]System.Console::Write(int32)
  IL_6741:  dup
  IL_6742:  ldc.i4.5
  IL_6743:  shl
  IL_6744:  call       void [System.Console]System.Console::Write(int32)
  IL_6749:  dup
  IL_674a:  ldc.i4.1
  IL_674b:  and
  IL_674c:  call       void [System.Console]System.Console::Write(int32)
  IL_6751:  dup
  IL_6752:  ldc.i4.1
  IL_6753:  or
  IL_6754:  call       void [System.Console]System.Console::Write(int32)
  IL_6759:  dup
  IL_675a:  ldc.i4.0
  IL_675b:  and
  IL_675c:  call       void [System.Console]System.Console::Write(int32)
  IL_6761:  dup
  IL_6762:  ldc.i4.1
  IL_6763:  shl
  IL_6764:  call       void [System.Console]System.Console::Write(int32)
  IL_6769:  dup
  IL_676a:  ldc.i4.1
  IL_676b:  xor
  IL_676c:  call       void [System.Console]System.Console::Write(int32)
  IL_6771:  dup
  IL_6772:  ldc.i4.7
  IL_6773:  sub
  IL_6774:  call       void [System.Console]System.Console::Write(int32)
  IL_6779:  dup
  IL_677a:  ldc.i4.8
  IL_677b:  add
  IL_677c:  call       void [System.Console]System.Console::Write(int32)
  IL_6781:  dup
  IL_6782:  ldc.i4.0
  IL_6783:  xor
  IL_6784:  call       void [System.Console]System.Console::Write(int32)
  IL_6789:  dup
  IL_678a:  ldc.i4.0
  IL_678b:  add
  IL_678c:  call       void [System.Console]System.Console::Write(int32)
  IL_6791:  dup
  IL_6792:  ldc.i4.7
  IL_6793:  and
  IL_6794:  call       void [System.Console]System.Console::Write(int32)
  IL_6799:  dup
  IL_679a:  ldc.i4.3
  IL_679b:  sub
  IL_679c:  call       void [System.Console]System.Console::Write(int32)
  IL_67a1:  dup
  IL_67a2:  ldc.i4.7
  IL_67a3:  and
  IL_67a4:  call       void [System.Console]System.Console::Write(int32)
  IL_67a9:  dup
  IL_67aa:  ldc.i4.2
  IL_67ab:  mul
  IL_67ac:  call       void [System.Console]System.Console::Write(int32)
  IL_67b1:  dup
  IL_67b2:  ldc.i4.8
  IL_67b3:  shr
  IL_67b4:  call       void [System.Console]System.Console::Write(int32)
  IL_67b9:  dup
  IL_67ba:  ldc.i4.6
  IL_67bb:  shl
  IL_67bc:  call       void [System.Console]System.Console::Write(int32)
  IL_67c1:  dup
  IL_67c2:  ldc.i4.6
  IL_67c3:  sub
  IL_67c4:  call       void [System.Console]System.Console::Write(int32)
  IL_67c9:  dup
  IL_67ca:  ldc.i4.2
  IL_67cb:  add
  IL_67cc:  call       void [System.Console]System.Console::Write(int32)
  IL_67d1:  dup
  IL_67d2:  ldc.i4.2
  IL_67d3:  mul
  IL_67d4:  call       void [System.Console]System.Console::Write(int32)
  IL_67d9:  dup
  IL_67da:  ldc.i4.2
  IL_67db:  shr
  IL_67dc:  call       void [System.Console]System.Console::Write(int32)
  IL_67e1:  dup
  IL_67e2:  ldc.i4.7
  IL_67e3:  add
  IL_67e4:  call       void [System.Console]System.Console::Write(int32)
  IL_67e9:  dup
  IL_67ea:  ldc.i4.2
  IL_67eb:  or
  IL_67ec:  call       void [System.Console]System.Console::Write(int32)
  IL_67f1:  dup
  IL_67f2:  ldc.i4.3
  IL_67f3:  add
  IL_67f4:  call       void [System.Console]System.Console::Write(int32)
  IL_67f9:  dup
  IL_67fa:  ldc.i4.0
  IL_67fb:  and
  IL_67fc:  call       void [System.Console]System.Console::Write(int32)
  IL_6801:  dup
  IL_6802:  ldc.i4.8
  IL_6803:  mul
  IL_6804:  call       void [System.Console]System.Console::Write(int32)
  IL_6809:  dup
  IL_680a:  ldc.i4.3
  IL_680b:  shl
  IL_680c:  call       void [System.Console]System.Console::Write(int32)
  IL_6811:  dup
  IL_6812:  ldc.i4.5
  IL_6813:  and
  IL_6814:  call       void [System.Console]System.Console::Write(int32)
  IL_6819:  dup
  IL_681a:  ldc.i4.3
  IL_681b:  and
  IL_681c:  call       void [System.Console]System.Console::Write(int32)
  IL_6821:  dup
  IL_6822:  ldc.i4.2
  IL_6823:  add
  IL_6824:  call       void [System.Console]System.Console::Write(int32)
  IL_6829:  dup
  IL_682a:  ldc.i4.8
  IL_682b:  mul
  IL_682c:  call       void [System.Console]System.Console::Write(int32)
  IL_6831:  dup
  IL_6832:  ldc.i4.5
  IL_6833:  and
  IL_6834:  call       void [System.Console]System.Console::Write(int32)
  IL_6839:  dup
  IL_683a:  ldc.i4.8
  IL_683b:  xor
  IL_683c:  call       void [System.Console]System.Console::Write(int32)
  IL_6841:  dup
  IL_6842:  ldc.i4.8
  IL_6843:  shl
  IL_6844:  call       void [System.Console]System.Console::Write(int32)
  IL_6849:  dup
  IL_684a:  ldc.i4.6
  IL_684b:  shl
  IL_684c:  call       void [System.Console]System.Console::Write(int32)
  IL_6851:  dup
  IL_6852:  ldc.i4.8
  IL_6853:  or
  IL_6854:  call       void [System.Console]System.Console::Write(int32)
  IL_6859:  dup
  IL_685a:  ldc.i4.6
  IL_685b:  and
  IL_685c:  call       void [System.Console]System.Console::Write(int32)
  IL_6861:  dup
  IL_6862:  ldc.i4.1
  IL_6863:  mul
  IL_6864:  call       void [System.Console]System.Console::Write(int32)
  IL_6869:  dup
  IL_686a:  ldc.i4.2
  IL_686b:  and
  IL_686c:  call       void [System.Console]System.Console::Write(int32)
  IL_6871:  dup
  IL_6872:  ldc.i4.4
  IL_6873:  xor
  IL_6874:  call       void [System.Console]System.Console::Write(int32)
  IL_6879:  dup
  IL_687a:  ldc.i4.6
  IL_687b:  shr
  IL_687c:  call       void [System.Console]System.Console::Write(int32)
  IL_6881:  dup
  IL_6882:  ldc.i4.7
  IL_6883:  shl
  IL_6884:  call       void [System.Console]System.Console::Write(int32)
  IL_6889:  dup
  IL_688a:  ldc.i4.4
  IL_688b:  shl
  IL_688c:  call       void [System.Console]System.Console::Write(int32)
  IL_6891:  dup
  IL_6892:  ldc.i4.2
  IL_6893:  sub
  IL_6894:  call       void [System.Console]System.Console::Write(int32)
  IL_6899:  dup
  IL_689a:  ldc.i4.0
  IL_689b:  and
  IL_689c:  call       void [System.Console]System.Console::Write(int32)
  IL_68a1:  dup
  IL_68a2:  ldc.i4.7
  IL_68a3:  xor
  IL_68a4:  call       void [System.Console]System.Console::Write(int32)
  IL_68a9:  dup
  IL_68aa:  ldc.i4.6
  IL_68ab:  mul
  IL_68ac:  call       void [System.Console]System.Console::Write(int32)
  IL_68b1:  dup
  IL_68b2:  ldc.i4.3
  IL_68b3:  or
  IL_68b4:  call       void [System.Console]System.Console::Write(int32)
  IL_68b9:  dup
  IL_68ba:  ldc.i4.0
  IL_68bb:  add
  IL_68bc:  call       void [System.Console]System.Console::Write(int32)
  IL_68c1:  dup
  IL_68c2:  ldc.i4.0
  IL_68c3:  and
  IL_68c4:  call       void [System.Console]System.Console::Write(int32)
  IL_68c9:  dup
  IL_68ca:  ldc.i4.0
  IL_68cb:  and
  IL_68cc:  call       void [System.Console]System.Console::Write(int32)
  IL_68d1:  dup
  IL_68d2:  ldc.i4.3
  IL_68d3:  xor
  IL_68d4:  call       void [System.Console]System.Console::Write(int32)
  IL_68d9:  dup
  IL_68da:  ldc.i4.7
  IL_68db:  or
  IL_68dc:  call       void [System.Console]System.Console::Write(int32)
  IL_68e1:  dup
  IL_68e2:  ldc.i4.5
  IL_68e3:  shr
  IL_68e4:  call       void [System.Console]System.Console::Write(int32)
  IL_68e9:  dup
  IL_68ea:  ldc.i4.3
  IL_68eb:  mul
  IL_68ec:  call       void [System.Console]System.Console::Write(int32)
  IL_68f1:  dup
  IL_68f2:  ldc.i4.4
  IL_68f3:  shl
  IL_68f4:  call       void [System.Console]System.Console::Write(int32)
  IL_68f9:  dup
  IL_68fa:  ldc.i4.3
  IL_68fb:  and
  IL_68fc:  call       void [System.Console]System.Console::Write(int32)
  IL_6901:  dup
  IL_6902:  ldc.i4.2
  IL_6903:  sub
  IL_6904:  call       void [System.Console]System.Console::Write(int32)
  IL_6909:  dup
  IL_690a:  ldc.i4.3
  IL_690b:  sub
  IL_690c:  call       void [System.Console]System.Console::Write(int32)
  IL_6911:  dup
  IL_6912:  ldc.i4.2
  IL_6913:  mul
  IL_6914:  call       void [System.Console]System.Console::Write(int32)
  IL_6919:  dup
  IL_691a:  ldc.i4.1
  IL_691b:  shr
  IL_691c:  call       void [System.Console]System.Console::Write(int32)
  IL_6921:  dup
  IL_6922:  ldc.i4.5
  IL_6923:  or
  IL_6924:  call       void [System.Console]System.Console::Write(int32)
  IL_6929:  dup
  IL_692a:  ldc.i4.7
  IL_692b:  xor
  IL_692c:  call       void [System.Console]System.Console::Write(int32)
  IL_6931:  dup
  IL_6932:  ldc.i4.1
  IL_6933:  shl
  IL_6934:  call       void [System.Console]System.Console::Write(int32)
  IL_6939:  dup
  IL_693a:  ldc.i4.3
  IL_693b:  sub
  IL_693c:  call       void [System.Console]System.Console::Write(int32)
  IL_6941:  dup
  IL_6942:  ldc.i4.2
  IL_6943:  mul
  IL_6944:  call       void [System.Console]System.Console::Write(int32)
  IL_6949:  dup
  IL_694a:  ldc.i4.6
  IL_694b:  shr
  IL_694c:  call       void [System.Console]System.Console::Write(int32)
  IL_6951:  dup
  IL_6952:  ldc.i4.2
  IL_6953:  or
  IL_6954:  call       void [System.Console]System.Console::Write(int32)
  IL_6959:  dup
  IL_695a:  ldc.i4.3
  IL_695b:  xor
  IL_695c:  call       void [System.Console]System.Console::Write(int32)
  IL_6961:  dup
  IL_6962:  ldc.i4.5
  IL_6963:  shl
  IL_6964:  call       void [System.Console]System.Console::Write(int32)
  IL_6969:  dup
  IL_696a:  ldc.i4.8
  IL_696b:  shr
  IL_696c:  call       void [System.Console]System.Console::Write(int32)
  IL_6971:  dup
  IL_6972:  ldc.i4.7
  IL_6973:  and
  IL_6974:  call       void [System.Console]System.Console::Write(int32)
  IL_6979:  dup
  IL_697a:  ldc.i4.2
  IL_697b:  mul
  IL_697c:  call       void [System.Console]System.Console::Write(int32)
  IL_6981:  dup
  IL_6982:  ldc.i4.1
  IL_6983:  or
  IL_6984:  call       void [System.Console]System.Console::Write(int32)
  IL_6989:  dup
  IL_698a:  ldc.i4.8
  IL_698b:  sub
  IL_698c:  call       void [System.Console]System.Console::Write(int32)
  IL_6991:  dup
  IL_6992:  ldc.i4.5
  IL_6993:  or
  IL_6994:  call       void [System.Console]System.Console::Write(int32)
  IL_6999:  dup
  IL_699a:  ldc.i4.6
  IL_699b:  shr
  IL_699c:  call       void [System.Console]System.Console::Write(int32)
  IL_69a1:  dup
  IL_69a2:  ldc.i4.5
  IL_69a3:  xor
  IL_69a4:  call       void [System.Console]System.Console::Write(int32)
  IL_69a9:  dup
  IL_69aa:  ldc.i4.1
  IL_69ab:  xor
  IL_69ac:  call       void [System.Console]System.Console::Write(int32)
  IL_69b1:  dup
  IL_69b2:  ldc.i4.5
  IL_69b3:  xor
  IL_69b4:  call       void [System.Console]System.Console::Write(int32)
  IL_69b9:  dup
  IL_69ba:  ldc.i4.6
  IL_69bb:  add
  IL_69bc:  call       void [System.Console]System.Console::Write(int32)
  IL_69c1:  dup
  IL_69c2:  ldc.i4.2
  IL_69c3:  sub
  IL_69c4:  call       void [System.Console]System.Console::Write(int32)
  IL_69c9:  dup
  IL_69ca:  ldc.i4.0
  IL_69cb:  sub
  IL_69cc:  call       void [System.Console]System.Console::Write(int32)
  IL_69d1:  dup
  IL_69d2:  ldc.i4.7
  IL_69d3:  shr
  IL_69d4:  call       void [System.Console]System.Console::Write(int32)
  IL_69d9:  dup
  IL_69da:  ldc.i4.5
  IL_69db:  mul
  IL_69dc:  call       void [System.Console]System.Console::Write(int32)
  IL_69e1:  dup
  IL_69e2:  ldc.i4.7
  IL_69e3:  or
  IL_69e4:  call       void [System.Console]System.Console::Write(int32)
  IL_69e9:  dup
  IL_69ea:  ldc.i4.4
  IL_69eb:  and
  IL_69ec:  call       void [System.Console]System.Console::Write(int32)
  IL_69f1:  dup
  IL_69f2:  ldc.i4.3
  IL_69f3:  shl
  IL_69f4:  call       void [System.Console]System.Console::Write(int32)
  IL_69f9:  dup
  IL_69fa:  ldc.i4.8
  IL_69fb:  and
  IL_69fc:  call       void [System.Console]System.Console::Write(int32)
  IL_6a01:  dup
  IL_6a02:  ldc.i4.5
  IL_6a03:  shl
  IL_6a04:  call       void [System.Console]System.Console::Write(int32)
  IL_6a09:  dup
  IL_6a0a:  ldc.i4.2
  IL_6a0b:  mul
  IL_6a0c:  call       void [System.Console]System.Console::Write(int32)
  IL_6a11:  dup
  IL_6a12:  ldc.i4.4
  IL_6a13:  mul
  IL_6a14:  call       void [System.Console]System.Console::Write(int32)
  IL_6a19:  dup
  IL_6a1a:  ldc.i4.5
  IL_6a1b:  mul
  IL_6a1c:  call       void [System.Console]System.Console::Write(int32)
  IL_6a21:  dup
  IL_6a22:  ldc.i4.5
  IL_6a23:  or
  IL_6a24:  call       void [System.Console]System.Console::Write(int32)
  IL_6a29:  dup
  IL_6a2a:  ldc.i4.0
  IL_6a2b:  or
  IL_6a2c:  call       void [System.Console]System.Console::Write(int32)
  IL_6a31:  dup
  IL_6a32:  ldc.i4.7
  IL_6a33:  or
  IL_6a34:  call       void [System.Console]System.Console::Write(int32)
  IL_6a39:  dup
  IL_6a3a:  ldc.i4.7
  IL_6a3b:  sub
  IL_6a3c:  call       void [System.Console]System.Console::Write(int32)
  IL_6a41:  dup
  IL_6a42:  ldc.i4.7
  IL_6a43:  or
  IL_6a44:  call       void [System.Console]System.Console::Write(int32)
  IL_6a49:  dup
  IL_6a4a:  ldc.i4.3
  IL_6a4b:  shl
  IL_6a4c:  call       void [System.Console]System.Console::Write(int32)
  IL_6a51:  dup
  IL_6a52:  ldc.i4.6
  IL_6a53:  add
  IL_6a54:  call       void [System.Console]System.Console::Write(int32)
  IL_6a59:  dup
  IL_6a5a:  ldc.i4.3
  IL_6a5b:  xor
  IL_6a5c:  call       void [System.Console]System.Console::Write(int32)
  IL_6a61:  dup
  IL_6a62:  ldc.i4.2
  IL_6a63:  shr
  IL_6a64:  call       void [System.Console]System.Console::Write(int32)
  IL_6a69:  dup
  IL_6a6a:  ldc.i4.3
  IL_6a6b:  shr
  IL_6a6c:  call       void [System.Console]System.Console::Write(int32)
  IL_6a71:  dup
  IL_6a72:  ldc.i4.3
  IL_6a73:  or
  IL_6a74:  call       void [System.Console]System.Console::Write(int32)
  IL_6a79:  dup
  IL_6a7a:  ldc.i4.3
  IL_6a7b:  add
  IL_6a7c:  call       void [System.Console]System.Console::Write(int32)
  IL_6a81:  dup
  IL_6a82:  ldc.i4.4
  IL_6a83:  mul
  IL_6a84:  call       void [System.Console]System.Console::Write(int32)
  IL_6a89:  dup
  IL_6a8a:  ldc.i4.7
  IL_6a8b:  add
  IL_6a8c:  call       void [System.Console]System.Console::Write(int32)
  IL_6a91:  dup
  IL_6a92:  ldc.i4.1
  IL_6a93:  xor
  IL_6a94:  call       void [System.Console]System.Console::Write(int32)
  IL_6a99:  dup
  IL_6a9a:  ldc.i4.5
  IL_6a9b:  or
  IL_6a9c:  call       void [System.Console]System.Console::Write(int32)
  IL_6aa1:  dup
  IL_6aa2:  ldc.i4.5
  IL_6aa3:  shr
  IL_6aa4:  call       void [System.Console]System.Console::Write(int32)
  IL_6aa9:  dup
  IL_6aaa:  ldc.i4.6
  IL_6aab:  or
  IL_6aac:  call       void [System.Console]System.Console::Write(int32)
  IL_6ab1:  dup
  IL_6ab2:  ldc.i4.5
  IL_6ab3:  shr
  IL_6ab4:  call       void [System.Console]System.Console::Write(int32)
  IL_6ab9:  dup
  IL_6aba:  ldc.i4.3
  IL_6abb:  and
  IL_6abc:  call       void [System.Console]System.Console::Write(int32)
  IL_6ac1:  dup
  IL_6ac2:  ldc.i4.0
  IL_6ac3:  sub
  IL_6ac4:  call       void [System.Console]System.Console::Write(int32)
  IL_6ac9:  dup
  IL_6aca:  ldc.i4.8
  IL_6acb:  xor
  IL_6acc:  call       void [System.Console]System.Console::Write(int32)
  IL_6ad1:  dup
  IL_6ad2:  ldc.i4.0
  IL_6ad3:  mul
  IL_6ad4:  call       void [System.Console]System.Console::Write(int32)
  IL_6ad9:  dup
  IL_6ada:  ldc.i4.1
  IL_6adb:  xor
  IL_6adc:  call       void [System.Console]System.Console::Write(int32)
  IL_6ae1:  dup
  IL_6ae2:  ldc.i4.1
  IL_6ae3:  and
  IL_6ae4:  call       void [System.Console]System.Console::Write(int32)
  IL_6ae9:  dup
  IL_6aea:  ldc.i4.8
  IL_6aeb:  and
  IL_6aec:  call       void [System.Console]System.Console::Write(int32)
  IL_6af1:  dup
  IL_6af2:  ldc.i4.8
  IL_6af3:  or
  IL_6af4:  call       void [System.Console]System.Console::Write(int32)
  IL_6af9:  dup
  IL_6afa:  ldc.i4.7
  IL_6afb:  shr
  IL_6afc:  call       void [System.Console]System.Console::Write(int32)
  IL_6b01:  dup
  IL_6b02:  ldc.i4.5
  IL_6b03:  or
  IL_6b04:  call       void [System.Console]System.Console::Write(int32)
  IL_6b09:  dup
  IL_6b0a:  ldc.i4.4
  IL_6b0b:  shl
  IL_6b0c:  call       void [System.Console]System.Console::Write(int32)
  IL_6b11:  dup
  IL_6b12:  ldc.i4.0
  IL_6b13:  and
  IL_6b14:  call       void [System.Console]System.Console::Write(int32)
  IL_6b19:  dup
  IL_6b1a:  ldc.i4.0
  IL_6b1b:  xor
  IL_6b1c:  call       void [System.Console]System.Console::Write(int32)
  IL_6b21:  dup
  IL_6b22:  ldc.i4.2
  IL_6b23:  xor
  IL_6b24:  call       void [System.Console]System.Console::Write(int32)
  IL_6b29:  dup
  IL_6b2a:  ldc.i4.7
  IL_6b2b:  add
  IL_6b2c:  call       void [System.Console]System.Console::Write(int32)
  IL_6b31:  dup
  IL_6b32:  ldc.i4.3
  IL_6b33:  xor
  IL_6b34:  call       void [System.Console]System.Console::Write(int32)
  IL_6b39:  dup
  IL_6b3a:  ldc.i4.7
  IL_6b3b:  shr
  IL_6b3c:  call       void [System.Console]System.Console::Write(int32)
  IL_6b41:  dup
  IL_6b42:  ldc.i4.2
  IL_6b43:  xor
  IL_6b44:  call       void [System.Console]System.Console::Write(int32)
  IL_6b49:  dup
  IL_6b4a:  ldc.i4.6
  IL_6b4b:  sub
  IL_6b4c:  call       void [System.Console]System.Console::Write(int32)
  IL_6b51:  dup
  IL_6b52:  ldc.i4.8
  IL_6b53:  or
  IL_6b54:  call       void [System.Console]System.Console::Write(int32)
  IL_6b59:  dup
  IL_6b5a:  ldc.i4.8
  IL_6b5b:  add
  IL_6b5c:  call       void [System.Console]System.Console::Write(int32)
  IL_6b61:  dup
  IL_6b62:  ldc.i4.1
  IL_6b63:  mul
  IL_6b64:  call       void [System.Console]System.Console::Write(int32)
  IL_6b69:  dup
  IL_6b6a:  ldc.i4.8
  IL_6b6b:  mul
  IL_6b6c:  call       void [System.Console]System.Console::Write(int32)
  IL_6b71:  dup
  IL_6b72:  ldc.i4.3
  IL_6b73:  and
  IL_6b74:  call       void [System.Console]System.Console::Write(int32)
  IL_6b79:  dup
  IL_6b7a:  ldc.i4.0
  IL_6b7b:  and
  IL_6b7c:  call       void [System.Console]System.Console::Write(int32)
  IL_6b81:  dup
  IL_6b82:  ldc.i4.7
  IL_6b83:  add
  IL_6b84:  call       void [System.Console]System.Console::Write(int32)
  IL_6b89:  dup
  IL_6b8a:  ldc.i4.2
  IL_6b8b:  shr
  IL_6b8c:  call       void [System.Console]System.Console::Write(int32)
  IL_6b91:  dup
  IL_6b92:  ldc.i4.1
  IL_6b93:  or
  IL_6b94:  call       void [System.Console]System.Console::Write(int32)
  IL_6b99:  dup
  IL_6b9a:  ldc.i4.7
  IL_6b9b:  sub
  IL_6b9c:  call       void [System.Console]System.Console::Write(int32)
  IL_6ba1:  dup
  IL_6ba2:  ldc.i4.1
  IL_6ba3:  shl
  IL_6ba4:  call       void [System.Console]System.Console::Write(int32)
  IL_6ba9:  dup
  IL_6baa:  ldc.i4.1
  IL_6bab:  shr
  IL_6bac:  call       void [System.Console]System.Console::Write(int32)
  IL_6bb1:  dup
  IL_6bb2:  ldc.i4.4
  IL_6bb3:  add
  IL_6bb4:  call       void [System.Console]System.Console::Write(int32)
  IL_6bb9:  dup
  IL_6bba:  ldc.i4.0
  IL_6bbb:  shr
  IL_6bbc:  call       void [System.Console]System.Console::Write(int32)
  IL_6bc1:  dup
  IL_6bc2:  ldc.i4.0
  IL_6bc3:  mul
  IL_6bc4:  call       void [System.Console]System.Console::Write(int32)
  IL_6bc9:  dup
  IL_6bca:  ldc.i4.4
  IL_6bcb:  xor
  IL_6bcc:  call       void [System.Console]System.Console::Write(int32)
  IL_6bd1:  dup
  IL_6bd2:  ldc.i4.1
  IL_6bd3:  shr
  IL_6bd4:  call       void [System.Console]System.Console::Write(int32)
  IL_6bd9:  dup
  IL_6bda:  ldc.i4.6
  IL_6bdb:  shr
  IL_6bdc:  call       void [System.Console]System.Console::Write(int32)
  IL_6be1:  dup
  IL_6be2:  ldc.i4.8
  IL_6be3:  mul
  IL_6be4:  call       void [System.Console]System.Console::Write(int32)
  IL_6be9:  dup
  IL_6bea:  ldc.i4.2
  IL_6beb:  shr
  IL_6bec:  call       void [System.Console]System.Console::Write(int32)
  IL_6bf1:  dup
  IL_6bf2:  ldc.i4.7
  IL_6bf3:  and
  IL_6bf4:  call       void [System.Console]System.Console::Write(int32)
  IL_6bf9:  dup
  IL_6bfa:  ldc.i4.8
  IL_6bfb:  shl
  IL_6bfc:  call       void [System.Console]System.Console::Write(int32)
  IL_6c01:  dup
  IL_6c02:  ldc.i4.3
  IL_6c03:  add
  IL_6c04:  call       void [System.Console]System.Console::Write(int32)
  IL_6c09:  dup
  IL_6c0a:  ldc.i4.1
  IL_6c0b:  mul
  IL_6c0c:  call       void [System.Console]System.Console::Write(int32)
  IL_6c11:  dup
  IL_6c12:  ldc.i4.1
  IL_6c13:  add
  IL_6c14:  call       void [System.Console]System.Console::Write(int32)
  IL_6c19:  dup
  IL_6c1a:  ldc.i4.4
  IL_6c1b:  xor
  IL_6c1c:  call       void [System.Console]System.Console::Write(int32)
  IL_6c21:  dup
  IL_6c22:  ldc.i4.6
  IL_6c23:  xor
  IL_6c24:  call       void [System.Console]System.Console::Write(int32)
  IL_6c29:  dup
  IL_6c2a:  ldc.i4.0
  IL_6c2b:  mul
  IL_6c2c:  call       void [System.Console]System.Console::Write(int32)
  IL_6c31:  dup
  IL_6c32:  ldc.i4.7
  IL_6c33:  mul
  IL_6c34:  call       void [System.Console]System.Console::Write(int32)
  IL_6c39:  dup
  IL_6c3a:  ldc.i4.5
  IL_6c3b:  add
  IL_6c3c:  call       void [System.Console]System.Console::Write(int32)
  IL_6c41:  dup
  IL_6c42:  ldc.i4.2
  IL_6c43:  mul
  IL_6c44:  call       void [System.Console]System.Console::Write(int32)
  IL_6c49:  dup
  IL_6c4a:  ldc.i4.2
  IL_6c4b:  add
  IL_6c4c:  call       void [System.Console]System.Console::Write(int32)
  IL_6c51:  dup
  IL_6c52:  ldc.i4.3
  IL_6c53:  shl
  IL_6c54:  call       void [System.Console]System.Console::Write(int32)
  IL_6c59:  dup
  IL_6c5a:  ldc.i4.5
  IL_6c5b:  sub
  IL_6c5c:  call       void [System.Console]System.Console::Write(int32)
  IL_6c61:  dup
  IL_6c62:  ldc.i4.3
  IL_6c63:  shl
  IL_6c64:  call       void [System.Console]System.Console::Write(int32)
  IL_6c69:  dup
  IL_6c6a:  ldc.i4.6
  IL_6c6b:  and
  IL_6c6c:  call       void [System.Console]System.Console::Write(int32)
  IL_6c71:  dup
  IL_6c72:  ldc.i4.5
  IL_6c73:  add
  IL_6c74:  call       void [System.Console]System.Console::Write(int32)
  IL_6c79:  dup
  IL_6c7a:  ldc.i4.3
  IL_6c7b:  xor
  IL_6c7c:  call       void [System.Console]System.Console::Write(int32)
  IL_6c81:  dup
  IL_6c82:  ldc.i4.5
  IL_6c83:  shl
  IL_6c84:  call       void [System.Console]System.Console::Write(int32)
  IL_6c89:  dup
  IL_6c8a:  ldc.i4.2
  IL_6c8b:  xor
  IL_6c8c:  call       void [System.Console]System.Console::Write(int32)
  IL_6c91:  dup
  IL_6c92:  ldc.i4.8
  IL_6c93:  and
  IL_6c94:  call       void [System.Console]System.Console::Write(int32)
  IL_6c99:  dup
  IL_6c9a:  ldc.i4.4
  IL_6c9b:  or
  IL_6c9c:  call       void [System.Console]System.Console::Write(int32)
  IL_6ca1:  dup
  IL_6ca2:  ldc.i4.7
  IL_6ca3:  add
  IL_6ca4:  call       void [System.Console]System.Console::Write(int32)
  IL_6ca9:  dup
  IL_6caa:  ldc.i4.6
  IL_6cab:  and
  IL_6cac:  call       void [System.Console]System.Console::Write(int32)
  IL_6cb1:  dup
  IL_6cb2:  ldc.i4.5
  IL_6cb3:  mul
  IL_6cb4:  call       void [System.Console]System.Console::Write(int32)
  IL_6cb9:  dup
  IL_6cba:  ldc.i4.8
  IL_6cbb:  shr
  IL_6cbc:  call       void [System.Console]System.Console::Write(int32)
  IL_6cc1:  dup
  IL_6cc2:  ldc.i4.6
  IL_6cc3:  sub
  IL_6cc4:  call       void [System.Console]System.Console::Write(int32)
  IL_6cc9:  dup
  IL_6cca:  ldc.i4.4
  IL_6ccb:  shl
  IL_6ccc:  call       void [System.Console]System.Console::Write(int32)
  IL_6cd1:  dup
  IL_6cd2:  ldc.i4.3
  IL_6cd3:  and
  IL_6cd4:  call       void [System.Console]System.Console::Write(int32)
  IL_6cd9:  dup
  IL_6cda:  ldc.i4.1
  IL_6cdb:  and
  IL_6cdc:  call       void [System.Console]System.Console::Write(int32)
  IL_6ce1:  dup
  IL_6ce2:  ldc.i4.4
  IL_6ce3:  or
  IL_6ce4:  call       void [System.Console]System.Console::Write(int32)
  IL_6ce9:  dup
  IL_6cea:  ldc.i4.8
  IL_6ceb:  shl
  IL_6cec:  call       void [System.Console]System.Console::Write(int32)
  IL_6cf1:  dup
  IL_6cf2:  ldc.i4.3
  IL_6cf3:  shr
  IL_6cf4:  call       void [System.Console]System.Console::Write(int32)
  IL_6cf9:  dup
  IL_6cfa:  ldc.i4.7
  IL_6cfb:  shr
  IL_6cfc:  call       void [System.Console]System.Console::Write(int32)
  IL_6d01:  dup
  IL_6d02:  ldc.i4.0
  IL_6d03:  shr
  IL_6d04:  call       void [System.Console]System.Console::Write(int32)
  IL_6d09:  dup
  IL_6d0a:  ldc.i4.6
  IL_6d0b:  sub
  IL_6d0c:  call       void [System.Console]System.Console::Write(int32)
  IL_6d11:  dup
  IL_6d12:  ldc.i4.0
  IL_6d13:  sub
  IL_6d14:  call       void [System.Console]System.Console::Write(int32)
  IL_6d19:  dup
  IL_6d1a:  ldc.i4.1
  IL_6d1b:  xor
  IL_6d1c:  call       void [System.Console]System.Console::Write(int32)
  IL_6d21:  dup
  IL_6d22:  ldc.i4.7
  IL_6d23:  xor
  IL_6d24:  call       void [System.Console]System.Console::Write(int32)
  IL_6d29:  dup
  IL_6d2a:  ldc.i4.2
  IL_6d2b:  mul
  IL_6d2c:  call       void [System.Console]System.Console::Write(int32)
  IL_6d31:  dup
  IL_6d32:  ldc.i4.7
  IL_6d33:  and
  IL_6d34:  call       void [System.Console]System.Console::Write(int32)
  IL_6d39:  dup
  IL_6d3a:  ldc.i4.5
  IL_6d3b:  add
  IL_6d3c:  call       void [System.Console]System.Console::Write(int32)
  IL_6d41:  dup
  IL_6d42:  ldc.i4.6
  IL_6d43:  add
  IL_6d44:  call       void [System.Console]System.Console::Write(int32)
  IL_6d49:  dup
  IL_6d4a:  ldc.i4.0
  IL_6d4b:  mul
  IL_6d4c:  call       void [System.Console]System.Console::Write(int32)
  IL_6d51:  dup
  IL_6d52:  ldc.i4.5
  IL_6d53:  and
  IL_6d54:  call       void [System.Console]System.Console::Write(int32)
  IL_6d59:  dup
  IL_6d5a:  ldc.i4.8
  IL_6d5b:  shr
  IL_6d5c:  call       void [System.Console]System.Console::Write(int32)
  IL_6d61:  dup
  IL_6d62:  ldc.i4.4
  IL_6d63:  xor
  IL_6d64:  call       void [System.Console]System.Console::Write(int32)
  IL_6d69:  dup
  IL_6d6a:  ldc.i4.7
  IL_6d6b:  or
  IL_6d6c:  call       void [System.Console]System.Console::Write(int32)
  IL_6d71:  dup
  IL_6d72:  ldc.i4.7
  IL_6d73:  or
  IL_6d74:  call       void [System.Console]System.Console::Write(int32)
  IL_6d79:  dup
  IL_6d7a:  ldc.i4.4
  IL_6d7b:  sub
  IL_6d7c:  call       void [System.Console]System.Console::Write(int32)
  IL_6d81:  dup
  IL_6d82:  ldc.i4.1
  IL_6d83:  add
  IL_6d84:  call       void [System.Console]System.Console::Write(int32)
  IL_6d89:  dup
  IL_6d8a:  ldc.i4.6
  IL_6d8b:  or
  IL_6d8c:  call       void [System.Console]System.Console::Write(int32)
  IL_6d91:  dup
  IL_6d92:  ldc.i4.5
  IL_6d93:  shr
  IL_6d94:  call       void [System.Console]System.Console::Write(int32)
  IL_6d99:  dup
  IL_6d9a:  ldc.i4.5
  IL_6d9b:  shl
  IL_6d9c:  call       void [System.Console]System.Console::Write(int32)
  IL_6da1:  dup
  IL_6da2:  ldc.i4.0
  IL_6da3:  or
  IL_6da4:  call       void [System.Console]System.Console::Write(int32)
  IL_6da9:  dup
  IL_6daa:  ldc.i4.6
  IL_6dab:  or
  IL_6dac:  call       void [System.Console]System.Console::Write(int32)
  IL_6db1:  dup
  IL_6db2:  ldc.i4.5
  IL_6db3:  and
  IL_6db4:  call       void [System.Console]System.Console::Write(int32)
  IL_6db9:  dup
  IL_6dba:  ldc.i4.8
  IL_6dbb:  shr
  IL_6dbc:  call       void [System.Console]System.Console::Write(int32)
  IL_6dc1:  dup
  IL_6dc2:  ldc.i4.3
  IL_6dc3:  or
  IL_6dc4:  call       void [System.Console]System.Console::Write(int32)
  IL_6dc9:  dup
  IL_6dca:  ldc.i4.7
  IL_6dcb:  xor
  IL_6dcc:  call       void [System.Console]System.Console::Write(int32)
  IL_6dd1:  dup
  IL_6dd2:  ldc.i4.5
  IL_6dd3:  xor
  IL_6dd4:  call       void [System.Console]System.Console::Write(int32)
  IL_6dd9:  dup
  IL_6dda:  ldc.i4.0
  IL_6ddb:  mul
  IL_6ddc:  call       void [System.Console]System.Console::Write(int32)
  IL_6de1:  dup
  IL_6de2:  ldc.i4.6
  IL_6de3:  add
  IL_6de4:  call       void [System.Console]System.Console::Write(int32)
  IL_6de9:  dup
  IL_6dea:  ldc.i4.6
  IL_6deb:  shr
  IL_6dec:  call       void [System.Console]System.Console::Write(int32)
  IL_6df1:  dup
  IL_6df2:  ldc.i4.2
  IL_6df3:  sub
  IL_6df4:  call       void [System.Console]System.Console::Write(int32)
  IL_6df9:  dup
  IL_6dfa:  ldc.i4.6
  IL_6dfb:  or
  IL_6dfc:  call       void [System.Console]System.Console::Write(int32)
  IL_6e01:  dup
  IL_6e02:  ldc.i4.8
  IL_6e03:  sub
  IL_6e04:  call       void [System.Console]System.Console::Write(int32)
  IL_6e09:  dup
  IL_6e0a:  ldc.i4.1
  IL_6e0b:  shr
  IL_6e0c:  call       void [System.Console]System.Console::Write(int32)
  IL_6e11:  dup
  IL_6e12:  ldc.i4.1
  IL_6e13:  shl
  IL_6e14:  call       void [System.Console]System.Console::Write(int32)
  IL_6e19:  dup
  IL_6e1a:  ldc.i4.3
  IL_6e1b:  or
  IL_6e1c:  call       void [System.Console]System.Console::Write(int32)
  IL_6e21:  dup
  IL_6e22:  ldc.i4.4
  IL_6e23:  shl
  IL_6e24:  call       void [System.Console]System.Console::Write(int32)
  IL_6e29:  dup
  IL_6e2a:  ldc.i4.2
  IL_6e2b:  shr
  IL_6e2c:  call       void [System.Console]System.Console::Write(int32)
  IL_6e31:  dup
  IL_6e32:  ldc.i4.0
  IL_6e33:  or
  IL_6e34:  call       void [System.Console]System.Console::Write(int32)
  IL_6e39:  dup
  IL_6e3a:  ldc.i4.1
  IL_6e3b:  shl
  IL_6e3c:  call       void [System.Console]System.Console::Write(int32)
  IL_6e41:  dup
  IL_6e42:  ldc.i4.3
  IL_6e43:  sub
  IL_6e44:  call       void [System.Console]System.Console::Write(int32)
  IL_6e49:  dup
  IL_6e4a:  ldc.i4.4
  IL_6e4b:  and
  IL_6e4c:  call       void [System.Console]System.Console::Write(int32)
  IL_6e51:  dup
  IL_6e52:  ldc.i4.0
  IL_6e53:  add
  IL_6e54:  call       void [System.Console]System.Console::Write(int32)
  IL_6e59:  dup
  IL_6e5a:  ldc.i4.5
  IL_6e5b:  shr
  IL_6e5c:  call       void [System.Console]System.Console::Write(int32)
  IL_6e61:  dup
  IL_6e62:  ldc.i4.0
  IL_6e63:  xor
  IL_6e64:  call       void [System.Console]System.Console::Write(int32)
  IL_6e69:  dup
  IL_6e6a:  ldc.i4.2
  IL_6e6b:  shl
  IL_6e6c:  call       void [System.Console]System.Console::Write(int32)
  IL_6e71:  dup
  IL_6e72:  ldc.i4.0
  IL_6e73:  and
  IL_6e74:  call       void [System.Console]System.Console::Write(int32)
  IL_6e79:  dup
  IL_6e7a:  ldc.i4.2
  IL_6e7b:  or
  IL_6e7c:  call       void [System.Console]System.Console::Write(int32)
  IL_6e81:  dup
  IL_6e82:  ldc.i4.2
  IL_6e83:  and
  IL_6e84:  call       void [System.Console]System.Console::Write(int32)
  IL_6e89:  dup
  IL_6e8a:  ldc.i4.0
  IL_6e8b:  xor
  IL_6e8c:  call       void [System.Console]System.Console::Write(int32)
  IL_6e91:  dup
  IL_6e92:  ldc.i4.6
  IL_6e93:  mul
  IL_6e94:  call       void [System.Console]System.Console::Write(int32)
  IL_6e99:  dup
  IL_6e9a:  ldc.i4.4
  IL_6e9b:  and
  IL_6e9c:  call       void [System.Console]System.Console::Write(int32)
  IL_6ea1:  dup
  IL_6ea2:  ldc.i4.4
  IL_6ea3:  or
  IL_6ea4:  call       void [System.Console]System.Console::Write(int32)
  IL_6ea9:  dup
  IL_6eaa:  ldc.i4.8
  IL_6eab:  add
  IL_6eac:  call       void [System.Console]System.Console::Write(int32)
  IL_6eb1:  dup
  IL_6eb2:  ldc.i4.1
  IL_6eb3:  xor
  IL_6eb4:  call       void [System.Console]System.Console::Write(int32)
  IL_6eb9:  dup
  IL_6eba:  ldc.i4.2
  IL_6ebb:  shr
  IL_6ebc:  call       void [System.Console]System.Console::Write(int32)
  IL_6ec1:  dup
  IL_6ec2:  ldc.i4.7
  IL_6ec3:  add
  IL_6ec4:  call       void [System.Console]System.Console::Write(int32)
  IL_6ec9:  dup
  IL_6eca:  ldc.i4.2
  IL_6ecb:  add
  IL_6ecc:  call       void [System.Console]System.Console::Write(int32)
  IL_6ed1:  dup
  IL_6ed2:  ldc.i4.5
  IL_6ed3:  and
  IL_6ed4:  call       void [System.Console]System.Console::Write(int32)
  IL_6ed9:  dup
  IL_6eda:  ldc.i4.7
  IL_6edb:  and
  IL_6edc:  call       void [System.Console]System.Console::Write(int32)
  IL_6ee1:  dup
  IL_6ee2:  ldc.i4.5
  IL_6ee3:  xor
  IL_6ee4:  call       void [System.Console]System.Console::Write(int32)
  IL_6ee9:  dup
  IL_6eea:  ldc.i4.5
  IL_6eeb:  and
  IL_6eec:  call       void [System.Console]System.Console::Write(int32)
  IL_6ef1:  dup
  IL_6ef2:  ldc.i4.2
  IL_6ef3:  and
  IL_6ef4:  call       void [System.Console]System.Console::Write(int32)
  IL_6ef9:  dup
  IL_6efa:  ldc.i4.8
  IL_6efb:  shl
  IL_6efc:  call       void [System.Console]System.Console::Write(int32)
  IL_6f01:  dup
  IL_6f02:  ldc.i4.2
  IL_6f03:  mul
  IL_6f04:  call       void [System.Console]System.Console::Write(int32)
  IL_6f09:  dup
  IL_6f0a:  ldc.i4.3
  IL_6f0b:  sub
  IL_6f0c:  call       void [System.Console]System.Console::Write(int32)
  IL_6f11:  dup
  IL_6f12:  ldc.i4.8
  IL_6f13:  shr
  IL_6f14:  call       void [System.Console]System.Console::Write(int32)
  IL_6f19:  dup
  IL_6f1a:  ldc.i4.8
  IL_6f1b:  sub
  IL_6f1c:  call       void [System.Console]System.Console::Write(int32)
  IL_6f21:  dup
  IL_6f22:  ldc.i4.6
  IL_6f23:  shr
  IL_6f24:  call       void [System.Console]System.Console::Write(int32)
  IL_6f29:  dup
  IL_6f2a:  ldc.i4.3
  IL_6f2b:  sub
  IL_6f2c:  call       void [System.Console]System.Console::Write(int32)
  IL_6f31:  dup
  IL_6f32:  ldc.i4.1
  IL_6f33:  xor
  IL_6f34:  call       void [System.Console]System.Console::Write(int32)
  IL_6f39:  dup
  IL_6f3a:  ldc.i4.4
  IL_6f3b:  shr
  IL_6f3c:  call       void [System.Console]System.Console::Write(int32)
  IL_6f41:  dup
  IL_6f42:  ldc.i4.1
  IL_6f43:  shl
  IL_6f44:  call       void [System.Console]System.Console::Write(int32)
  IL_6f49:  dup
  IL_6f4a:  ldc.i4.6
  IL_6f4b:  or
  IL_6f4c:  call       void [System.Console]System.Console::Write(int32)
  IL_6f51:  dup
  IL_6f52:  ldc.i4.5
  IL_6f53:  add
  IL_6f54:  call       void [System.Console]System.Console::Write(int32)
  IL_6f59:  dup
  IL_6f5a:  ldc.i4.2
  IL_6f5b:  xor
  IL_6f5c:  call       void [System.Console]System.Console::Write(int32)
  IL_6f61:  dup
  IL_6f62:  ldc.i4.8
  IL_6f63:  sub
  IL_6f64:  call       void [System.Console]System.Console::Write(int32)
  IL_6f69:  dup
  IL_6f6a:  ldc.i4.7
  IL_6f6b:  mul
  IL_6f6c:  call       void [System.Console]System.Console::Write(int32)
  IL_6f71:  dup
  IL_6f72:  ldc.i4.5
  IL_6f73:  shr
  IL_6f74:  call       void [System.Console]System.Console::Write(int32)
  IL_6f79:  dup
  IL_6f7a:  ldc.i4.5
  IL_6f7b:  shr
  IL_6f7c:  call       void [System.Console]System.Console::Write(int32)
  IL_6f81:  dup
  IL_6f82:  ldc.i4.8
  IL_6f83:  mul
  IL_6f84:  call       void [System.Console]System.Console::Write(int32)
  IL_6f89:  dup
  IL_6f8a:  ldc.i4.7
  IL_6f8b:  and
  IL_6f8c:  call       void [System.Console]System.Console::Write(int32)
  IL_6f91:  dup
  IL_6f92:  ldc.i4.0
  IL_6f93:  or
  IL_6f94:  call       void [System.Console]System.Console::Write(int32)
  IL_6f99:  dup
  IL_6f9a:  ldc.i4.6
  IL_6f9b:  sub
  IL_6f9c:  call       void [System.Console]System.Console::Write(int32)
  IL_6fa1:  dup
  IL_6fa2:  ldc.i4.1
  IL_6fa3:  sub
  IL_6fa4:  call       void [System.Console]System.Console::Write(int32)
  IL_6fa9:  dup
  IL_6faa:  ldc.i4.3
  IL_6fab:  or
  IL_6fac:  call       void [System.Console]System.Console::Write(int32)
  IL_6fb1:  dup
  IL_6fb2:  ldc.i4.4
  IL_6fb3:  shr
  IL_6fb4:  call       void [System.Console]System.Console::Write(int32)
  IL_6fb9:  dup
  IL_6fba:  ldc.i4.6
  IL_6fbb:  add
  IL_6fbc:  call       void [System.Console]System.Console::Write(int32)
  IL_6fc1:  dup
  IL_6fc2:  ldc.i4.7
  IL_6fc3:  or
  IL_6fc4:  call       void [System.Console]System.Console::Write(int32)
  IL_6fc9:  dup
  IL_6fca:  ldc.i4.5
  IL_6fcb:  shr
  IL_6fcc:  call       void [System.Console]System.Console::Write(int32)
  IL_6fd1:  dup
  IL_6fd2:  ldc.i4.1
  IL_6fd3:  add
  IL_6fd4:  call       void [System.Console]System.Console::Write(int32)
  IL_6fd9:  dup
  IL_6fda:  ldc.i4.0
  IL_6fdb:  sub
  IL_6fdc:  call       void [System.Console]System.Console::Write(int32)
  IL_6fe1:  dup
  IL_6fe2:  ldc.i4.0
  IL_6fe3:  or
  IL_6fe4:  call       void [System.Console]System.Console::Write(int32)
  IL_6fe9:  dup
  IL_6fea:  ldc.i4.2
  IL_6feb:  shr
  IL_6fec:  call       void [System.Console]System.Console::Write(int32)
  IL_6ff1:  dup
  IL_6ff2:  ldc.i4.8
  IL_6ff3:  shl
  IL_6ff4:  call       void [System.Console]System.Console::Write(int32)
  IL_6ff9:  dup
  IL_6ffa:  ldc.i4.4
  IL_6ffb:  or
  IL_6ffc:  call       void [System.Console]System.Console::Write(int32)
  IL_7001:  dup
  IL_7002:  ldc.i4.1
  IL_7003:  shr
  IL_7004:  call       void [System.Console]System.Console::Write(int32)
  IL_7009:  dup
  IL_700a:  ldc.i4.2
  IL_700b:  and
  IL_700c:  call       void [System.Console]System.Console::Write(int32)
  IL_7011:  dup
  IL_7012:  ldc.i4.6
  IL_7013:  shr
  IL_7014:  call       void [System.Console]System.Console::Write(int32)
  IL_7019:  dup
  IL_701a:  ldc.i4.5
  IL_701b:  sub
  IL_701c:  call       void [System.Console]System.Console::Write(int32)
  IL_7021:  dup
  IL_7022:  ldc.i4.7
  IL_7023:  shl
  IL_7024:  call       void [System.Console]System.Console::Write(int32)
  IL_7029:  dup
  IL_702a:  ldc.i4.6
  IL_702b:  xor
  IL_702c:  call       void [System.Console]System.Console::Write(int32)
  IL_7031:  dup
  IL_7032:  ldc.i4.6
  IL_7033:  mul
  IL_7034:  call       void [System.Console]System.Console::Write(int32)
  IL_7039:  dup
  IL_703a:  ldc.i4.8
  IL_703b:  mul
  IL_703c:  call       void [System.Console]System.Console::Write(int32)
  IL_7041:  dup
  IL_7042:  ldc.i4.4
  IL_7043:  shr
  IL_7044:  call       void [System.Console]System.Console::Write(int32)
  IL_7049:  dup
  IL_704a:  ldc.i4.4
  IL_704b:  shr
  IL_704c:  call       void [System.Console]System.Console::Write(int32)
  IL_7051:  dup
  IL_7052:  ldc.i4.1
  IL_7053:  or
  IL_7054:  call       void [System.Console]System.Console::Write(int32)
  IL_7059:  dup
  IL_705a:  ldc.i4.4
  IL_705b:  and
  IL_705c:  call       void [System.Console]System.Console::Write(int32)
  IL_7061:  dup
  IL_7062:  ldc.i4.2
  IL_7063:  shr
  IL_7064:  call       void [System.Console]System.Console::Write(int32)
  IL_7069:  dup
  IL_706a:  ldc.i4.6
  IL_706b:  sub
  IL_706c:  call       void [System.Console]System.Console::Write(int32)
  IL_7071:  dup
  IL_7072:  ldc.i4.6
  IL_7073:  add
  IL_7074:  call       void [System.Console]System.Console::Write(int32)
  IL_7079:  dup
  IL_707a:  ldc.i4.2
  IL_707b:  shl
  IL_707c:  call       void [System.Console]System.Console::Write(int32)
  IL_7081:  dup
  IL_7082:  ldc.i4.6
  IL_7083:  xor
  IL_7084:  call       void [System.Console]System.Console::Write(int32)
  IL_7089:  dup
  IL_708a:  ldc.i4.7
  IL_708b:  and
  IL_708c:  call       void [System.Console]System.Console::Write(int32)
  IL_7091:  dup
  IL_7092:  ldc.i4.4
  IL_7093:  and
  IL_7094:  call       void [System.Console]System.Console::Write(int32)
  IL_7099:  dup
  IL_709a:  ldc.i4.3
  IL_709b:  sub
  IL_709c:  call       void [System.Console]System.Console::Write(int32)
  IL_70a1:  dup
  IL_70a2:  ldc.i4.3
  IL_70a3:  xor
  IL_70a4:  call       void [System.Console]System.Console::Write(int32)
  IL_70a9:  dup
  IL_70aa:  ldc.i4.7
  IL_70ab:  shl
  IL_70ac:  call       void [System.Console]System.Console::Write(int32)
  IL_70b1:  dup
  IL_70b2:  ldc.i4.3
  IL_70b3:  shr
  IL_70b4:  call       void [System.Console]System.Console::Write(int32)
  IL_70b9:  dup
  IL_70ba:  ldc.i4.4
  IL_70bb:  and
  IL_70bc:  call       void [System.Console]System.Console::Write(int32)
  IL_70c1:  dup
  IL_70c2:  ldc.i4.8
  IL_70c3:  add
  IL_70c4:  call       void [System.Console]System.Console::Write(int32)
  IL_70c9:  dup
  IL_70ca:  ldc.i4.2
  IL_70cb:  sub
  IL_70cc:  call       void [System.Console]System.Console::Write(int32)
  IL_70d1:  dup
  IL_70d2:  ldc.i4.0
  IL_70d3:  and
  IL_70d4:  call       void [System.Console]System.Console::Write(int32)
  IL_70d9:  dup
  IL_70da:  ldc.i4.7
  IL_70db:  sub
  IL_70dc:  call       void [System.Console]System.Console::Write(int32)
  IL_70e1:  dup
  IL_70e2:  ldc.i4.1
  IL_70e3:  xor
  IL_70e4:  call       void [System.Console]System.Console::Write(int32)
  IL_70e9:  dup
  IL_70ea:  ldc.i4.4
  IL_70eb:  add
  IL_70ec:  call       void [System.Console]System.Console::Write(int32)
  IL_70f1:  dup
  IL_70f2:  ldc.i4.8
  IL_70f3:  xor
  IL_70f4:  call       void [System.Console]System.Console::Write(int32)
  IL_70f9:  dup
  IL_70fa:  ldc.i4.4
  IL_70fb:  and
  IL_70fc:  call       void [System.Console]System.Console::Write(int32)
  IL_7101:  dup
  IL_7102:  ldc.i4.3
  IL_7103:  add
  IL_7104:  call       void [System.Console]System.Console::Write(int32)
  IL_7109:  dup
  IL_710a:  ldc.i4.6
  IL_710b:  sub
  IL_710c:  call       void [System.Console]System.Console::Write(int32)
  IL_7111:  dup
  IL_7112:  ldc.i4.4
  IL_7113:  sub
  IL_7114:  call       void [System.Console]System.Console::Write(int32)
  IL_7119:  dup
  IL_711a:  ldc.i4.7
  IL_711b:  shl
  IL_711c:  call       void [System.Console]System.Console::Write(int32)
  IL_7121:  dup
  IL_7122:  ldc.i4.2
  IL_7123:  mul
  IL_7124:  call       void [System.Console]System.Console::Write(int32)
  IL_7129:  dup
  IL_712a:  ldc.i4.2
  IL_712b:  shr
  IL_712c:  call       void [System.Console]System.Console::Write(int32)
  IL_7131:  dup
  IL_7132:  ldc.i4.1
  IL_7133:  or
  IL_7134:  call       void [System.Console]System.Console::Write(int32)
  IL_7139:  dup
  IL_713a:  ldc.i4.4
  IL_713b:  shr
  IL_713c:  call       void [System.Console]System.Console::Write(int32)
  IL_7141:  dup
  IL_7142:  ldc.i4.1
  IL_7143:  or
  IL_7144:  call       void [System.Console]System.Console::Write(int32)
  IL_7149:  dup
  IL_714a:  ldc.i4.2
  IL_714b:  shr
  IL_714c:  call       void [System.Console]System.Console::Write(int32)
  IL_7151:  dup
  IL_7152:  ldc.i4.6
  IL_7153:  add
  IL_7154:  call       void [System.Console]System.Console::Write(int32)
  IL_7159:  dup
  IL_715a:  ldc.i4.4
  IL_715b:  or
  IL_715c:  call       void [System.Console]System.Console::Write(int32)
  IL_7161:  dup
  IL_7162:  ldc.i4.5
  IL_7163:  shl
  IL_7164:  call       void [System.Console]System.Console::Write(int32)
  IL_7169:  dup
  IL_716a:  ldc.i4.5
  IL_716b:  add
  IL_716c:  call       void [System.Console]System.Console::Write(int32)
  IL_7171:  dup
  IL_7172:  ldc.i4.3
  IL_7173:  xor
  IL_7174:  call       void [System.Console]System.Console::Write(int32)
  IL_7179:  dup
  IL_717a:  ldc.i4.2
  IL_717b:  or
  IL_717c:  call       void [System.Console]System.Console::Write(int32)
  IL_7181:  dup
  IL_7182:  ldc.i4.0
  IL_7183:  or
  IL_7184:  call       void [System.Console]System.Console::Write(int32)
  IL_7189:  dup
  IL_718a:  ldc.i4.1
  IL_718b:  mul
  IL_718c:  call       void [System.Console]System.Console::Write(int32)
  IL_7191:  dup
  IL_7192:  ldc.i4.8
  IL_7193:  sub
  IL_7194:  call       void [System.Console]System.Console::Write(int32)
  IL_7199:  dup
  IL_719a:  ldc.i4.2
  IL_719b:  shr
  IL_719c:  call       void [System.Console]System.Console::Write(int32)
  IL_71a1:  dup
  IL_71a2:  ldc.i4.1
  IL_71a3:  shr
  IL_71a4:  call       void [System.Console]System.Console::Write(int32)
  IL_71a9:  dup
  IL_71aa:  ldc.i4.7
  IL_71ab:  and
  IL_71ac:  call       void [System.Console]System.Console::Write(int32)
  IL_71b1:  dup
  IL_71b2:  ldc.i4.4
  IL_71b3:  sub
  IL_71b4:  call       void [System.Console]System.Console::Write(int32)
  IL_71b9:  dup
  IL_71ba:  ldc.i4.0
  IL_71bb:  mul
  IL_71bc:  call       void [System.Console]System.Console::Write(int32)
  IL_71c1:  dup
  IL_71c2:  ldc.i4.8
  IL_71c3:  and
  IL_71c4:  call       void [System.Console]System.Console::Write(int32)
  IL_71c9:  dup
  IL_71ca:  ldc.i4.7
  IL_71cb:  or
  IL_71cc:  call       void [System.Console]System.Console::Write(int32)
  IL_71d1:  dup
  IL_71d2:  ldc.i4.1
  IL_71d3:  shr
  IL_71d4:  call       void [System.Console]System.Console::Write(int32)
  IL_71d9:  dup
  IL_71da:  ldc.i4.0
  IL_71db:  mul
  IL_71dc:  call       void [System.Console]System.Console::Write(int32)
  IL_71e1:  dup
  IL_71e2:  ldc.i4.4
  IL_71e3:  and
  IL_71e4:  call       void [System.Console]System.Console::Write(int32)
  IL_71e9:  dup
  IL_71ea:  ldc.i4.5
  IL_71eb:  or
  IL_71ec:  call       void [System.Console]System.Console::Write(int32)
  IL_71f1:  dup
  IL_71f2:  ldc.i4.6
  IL_71f3:  shr
  IL_71f4:  call       void [System.Console]System.Console::Write(int32)
  IL_71f9:  dup
  IL_71fa:  ldc.i4.2
  IL_71fb:  or
  IL_71fc:  call       void [System.Console]System.Console::Write(int32)
  IL_7201:  dup
  IL_7202:  ldc.i4.0
  IL_7203:  xor
  IL_7204:  call       void [System.Console]System.Console::Write(int32)
  IL_7209:  dup
  IL_720a:  ldc.i4.5
  IL_720b:  sub
  IL_720c:  call       void [System.Console]System.Console::Write(int32)
  IL_7211:  dup
  IL_7212:  ldc.i4.6
  IL_7213:  sub
  IL_7214:  call       void [System.Console]System.Console::Write(int32)
  IL_7219:  dup
  IL_721a:  ldc.i4.6
  IL_721b:  or
  IL_721c:  call       void [System.Console]System.Console::Write(int32)
  IL_7221:  dup
  IL_7222:  ldc.i4.6
  IL_7223:  shr
  IL_7224:  call       void [System.Console]System.Console::Write(int32)
  IL_7229:  dup
  IL_722a:  ldc.i4.4
  IL_722b:  sub
  IL_722c:  call       void [System.Console]System.Console::Write(int32)
  IL_7231:  dup
  IL_7232:  ldc.i4.2
  IL_7233:  shr
  IL_7234:  call       void [System.Console]System.Console::Write(int32)
  IL_7239:  dup
  IL_723a:  ldc.i4.3
  IL_723b:  xor
  IL_723c:  call       void [System.Console]System.Console::Write(int32)
  IL_7241:  dup
  IL_7242:  ldc.i4.5
  IL_7243:  mul
  IL_7244:  call       void [System.Console]System.Console::Write(int32)
  IL_7249:  dup
  IL_724a:  ldc.i4.1
  IL_724b:  shl
  IL_724c:  call       void [System.Console]System.Console::Write(int32)
  IL_7251:  dup
  IL_7252:  ldc.i4.5
  IL_7253:  mul
  IL_7254:  call       void [System.Console]System.Console::Write(int32)
  IL_7259:  dup
  IL_725a:  ldc.i4.7
  IL_725b:  mul
  IL_725c:  call       void [System.Console]System.Console::Write(int32)
  IL_7261:  dup
  IL_7262:  ldc.i4.8
  IL_7263:  or
  IL_7264:  call       void [System.Console]System.Console::Write(int32)
  IL_7269:  dup
  IL_726a:  ldc.i4.6
  IL_726b:  add
  IL_726c:  call       void [System.Console]System.Console::Write(int32)
  IL_7271:  dup
  IL_7272:  ldc.i4.5
  IL_7273:  mul
  IL_7274:  call       void [System.Console]System.Console::Write(int32)
  IL_7279:  dup
  IL_727a:  ldc.i4.8
  IL_727b:  xor
  IL_727c:  call       void [System.Console]System.Console::Write(int32)
  IL_7281:  dup
  IL_7282:  ldc.i4.3
  IL_7283:  or
  IL_7284:  call       void [System.Console]System.Console::Write(int32)
  IL_7289:  dup
  IL_728a:  ldc.i4.6
  IL_728b:  add
  IL_728c:  call       void [System.Console]System.Console::Write(int32)
  IL_7291:  dup
  IL_7292:  ldc.i4.4
  IL_7293:  or
  IL_7294:  call       void [System.Console]System.Console::Write(int32)
  IL_7299:  dup
  IL_729a:  ldc.i4.4
  IL_729b:  and
  IL_729c:  call       void [System.Console]System.Console::Write(int32)
  IL_72a1:  dup
  IL_72a2:  ldc.i4.0
  IL_72a3:  and
  IL_72a4:  call       void [System.Console]System.Console::Write(int32)
  IL_72a9:  dup
  IL_72aa:  ldc.i4.1
  IL_72ab:  shl
  IL_72ac:  call       void [System.Console]System.Console::Write(int32)
  IL_72b1:  dup
  IL_72b2:  ldc.i4.1
  IL_72b3:  or
  IL_72b4:  call       void [System.Console]System.Console::Write(int32)
  IL_72b9:  dup
  IL_72ba:  ldc.i4.8
  IL_72bb:  and
  IL_72bc:  call       void [System.Console]System.Console::Write(int32)
  IL_72c1:  dup
  IL_72c2:  ldc.i4.3
  IL_72c3:  sub
  IL_72c4:  call       void [System.Console]System.Console::Write(int32)
  IL_72c9:  dup
  IL_72ca:  ldc.i4.3
  IL_72cb:  xor
  IL_72cc:  call       void [System.Console]System.Console::Write(int32)
  IL_72d1:  dup
  IL_72d2:  ldc.i4.5
  IL_72d3:  shr
  IL_72d4:  call       void [System.Console]System.Console::Write(int32)
  IL_72d9:  dup
  IL_72da:  ldc.i4.2
  IL_72db:  and
  IL_72dc:  call       void [System.Console]System.Console::Write(int32)
  IL_72e1:  dup
  IL_72e2:  ldc.i4.5
  IL_72e3:  xor
  IL_72e4:  call       void [System.Console]System.Console::Write(int32)
  IL_72e9:  dup
  IL_72ea:  ldc.i4.1
  IL_72eb:  and
  IL_72ec:  call       void [System.Console]System.Console::Write(int32)
  IL_72f1:  dup
  IL_72f2:  ldc.i4.3
  IL_72f3:  shl
  IL_72f4:  call       void [System.Console]System.Console::Write(int32)
  IL_72f9:  dup
  IL_72fa:  ldc.i4.2
  IL_72fb:  xor
  IL_72fc:  call       void [System.Console]System.Console::Write(int32)
  IL_7301:  dup
  IL_7302:  ldc.i4.5
  IL_7303:  add
  IL_7304:  call       void [System.Console]System.Console::Write(int32)
  IL_7309:  dup
  IL_730a:  ldc.i4.6
  IL_730b:  mul
  IL_730c:  call       void [System.Console]System.Console::Write(int32)
  IL_7311:  dup
  IL_7312:  ldc.i4.0
  IL_7313:  mul
  IL_7314:  call       void [System.Console]System.Console::Write(int32)
  IL_7319:  dup
  IL_731a:  ldc.i4.8
  IL_731b:  shr
  IL_731c:  call       void [System.Console]System.Console::Write(int32)
  IL_7321:  dup
  IL_7322:  ldc.i4.6
  IL_7323:  mul
  IL_7324:  call       void [System.Console]System.Console::Write(int32)
  IL_7329:  dup
  IL_732a:  ldc.i4.4
  IL_732b:  and
  IL_732c:  call       void [System.Console]System.Console::Write(int32)
  IL_7331:  dup
  IL_7332:  ldc.i4.0
  IL_7333:  mul
  IL_7334:  call       void [System.Console]System.Console::Write(int32)
  IL_7339:  dup
  IL_733a:  ldc.i4.0
  IL_733b:  add
  IL_733c:  call       void [System.Console]System.Console::Write(int32)
  IL_7341:  dup
  IL_7342:  ldc.i4.8
  IL_7343:  or
  IL_7344:  call       void [System.Console]System.Console::Write(int32)
  IL_7349:  dup
  IL_734a:  ldc.i4.6
  IL_734b:  shl
  IL_734c:  call       void [System.Console]System.Console::Write(int32)
  IL_7351:  dup
  IL_7352:  ldc.i4.0
  IL_7353:  shl
  IL_7354:  call       void [System.Console]System.Console::Write(int32)
  IL_7359:  dup
  IL_735a:  ldc.i4.7
  IL_735b:  shr
  IL_735c:  call       void [System.Console]System.Console::Write(int32)
  IL_7361:  dup
  IL_7362:  ldc.i4.5
  IL_7363:  or
  IL_7364:  call       void [System.Console]System.Console::Write(int32)
  IL_7369:  dup
  IL_736a:  ldc.i4.4
  IL_736b:  sub
  IL_736c:  call       void [System.Console]System.Console::Write(int32)
  IL_7371:  dup
  IL_7372:  ldc.i4.6
  IL_7373:  and
  IL_7374:  call       void [System.Console]System.Console::Write(int32)
  IL_7379:  dup
  IL_737a:  ldc.i4.3
  IL_737b:  xor
  IL_737c:  call       void [System.Console]System.Console::Write(int32)
  IL_7381:  dup
  IL_7382:  ldc.i4.5
  IL_7383:  and
  IL_7384:  call       void [System.Console]System.Console::Write(int32)
  IL_7389:  dup
  IL_738a:  ldc.i4.7
  IL_738b:  add
  IL_738c:  call       void [System.Console]System.Console::Write(int32)
  IL_7391:  dup
  IL_7392:  ldc.i4.1
  IL_7393:  shr
  IL_7394:  call       void [System.Console]System.Console::Write(int32)
  IL_7399:  dup
  IL_739a:  ldc.i4.7
  IL_739b:  sub
  IL_739c:  call       void [System.Console]System.Console::Write(int32)
  IL_73a1:  dup
  IL_73a2:  ldc.i4.1
  IL_73a3:  shr
  IL_73a4:  call       void [System.Console]System.Console::Write(int32)
  IL_73a9:  dup
  IL_73aa:  ldc.i4.5
  IL_73ab:  mul
  IL_73ac:  call       void [System.Console]System.Console::Write(int32)
  IL_73b1:  dup
  IL_73b2:  ldc.i4.2
  IL_73b3:  shl
  IL_73b4:  call       void [System.Console]System.Console::Write(int32)
  IL_73b9:  dup
  IL_73ba:  ldc.i4.6
  IL_73bb:  mul
  IL_73bc:  call       void [System.Console]System.Console::Write(int32)
  IL_73c1:  dup
  IL_73c2:  ldc.i4.2
  IL_73c3:  sub
  IL_73c4:  call       void [System.Console]System.Console::Write(int32)
  IL_73c9:  dup
  IL_73ca:  ldc.i4.6
  IL_73cb:  xor
  IL_73cc:  call       void [System.Console]System.Console::Write(int32)
  IL_73d1:  dup
  IL_73d2:  ldc.i4.8
  IL_73d3:  and
  IL_73d4:  call       void [System.Console]System.Console::Write(int32)
  IL_73d9:  dup
  IL_73da:  ldc.i4.3
  IL_73db:  add
  IL_73dc:  call       void [System.Console]System.Console::Write(int32)
  IL_73e1:  dup
  IL_73e2:  ldc.i4.2
  IL_73e3:  add
  IL_73e4:  call       void [System.Console]System.Console::Write(int32)
  IL_73e9:  dup
  IL_73ea:  ldc.i4.6
  IL_73eb:  sub
  IL_73ec:  call       void [System.Console]System.Console::Write(int32)
  IL_73f1:  dup
  IL_73f2:  ldc.i4.6
  IL_73f3:  and
  IL_73f4:  call       void [System.Console]System.Console::Write(int32)
  IL_73f9:  dup
  IL_73fa:  ldc.i4.8
  IL_73fb:  or
  IL_73fc:  call       void [System.Console]System.Console::Write(int32)
  IL_7401:  dup
  IL_7402:  ldc.i4.8
  IL_7403:  or
  IL_7404:  call       void [System.Console]System.Console::Write(int32)
  IL_7409:  dup
  IL_740a:  ldc.i4.1
  IL_740b:  mul
  IL_740c:  call       void [System.Console]System.Console::Write(int32)
  IL_7411:  dup
  IL_7412:  ldc.i4.3
  IL_7413:  or
  IL_7414:  call       void [System.Console]System.Console::Write(int32)
  IL_7419:  dup
  IL_741a:  ldc.i4.7
  IL_741b:  mul
  IL_741c:  call       void [System.Console]System.Console::Write(int32)
  IL_7421:  dup
  IL_7422:  ldc.i4.1
  IL_7423:  shl
  IL_7424:  call       void [System.Console]System.Console::Write(int32)
  IL_7429:  dup
  IL_742a:  ldc.i4.2
  IL_742b:  mul
  IL_742c:  call       void [System.Console]System.Console::Write(int32)
  IL_7431:  dup
  IL_7432:  ldc.i4.8
  IL_7433:  shl
  IL_7434:  call       void [System.Console]System.Console::Write(int32)
  IL_7439:  dup
  IL_743a:  ldc.i4.3
  IL_743b:  sub
  IL_743c:  call       void [System.Console]System.Console::Write(int32)
  IL_7441:  dup
  IL_7442:  ldc.i4.1
  IL_7443:  sub
  IL_7444:  call       void [System.Console]System.Console::Write(int32)
  IL_7449:  dup
  IL_744a:  ldc.i4.7
  IL_744b:  shl
  IL_744c:  call       void [System.Console]System.Console::Write(int32)
  IL_7451:  dup
  IL_7452:  ldc.i4.0
  IL_7453:  or
  IL_7454:  call       void [System.Console]System.Console::Write(int32)
  IL_7459:  dup
  IL_745a:  ldc.i4.4
  IL_745b:  and
  IL_745c:  call       void [System.Console]System.Console::Write(int32)
  IL_7461:  dup
  IL_7462:  ldc.i4.3
  IL_7463:  and
  IL_7464:  call       void [System.Console]System.Console::Write(int32)
  IL_7469:  dup
  IL_746a:  ldc.i4.8
  IL_746b:  add
  IL_746c:  call       void [System.Console]System.Console::Write(int32)
  IL_7471:  dup
  IL_7472:  ldc.i4.8
  IL_7473:  shr
  IL_7474:  call       void [System.Console]System.Console::Write(int32)
  IL_7479:  dup
  IL_747a:  ldc.i4.3
  IL_747b:  xor
  IL_747c:  call       void [System.Console]System.Console::Write(int32)
  IL_7481:  dup
  IL_7482:  ldc.i4.6
  IL_7483:  shr
  IL_7484:  call       void [System.Console]System.Console::Write(int32)
  IL_7489:  dup
  IL_748a:  ldc.i4.3
  IL_748b:  mul
  IL_748c:  call       void [System.Console]System.Console::Write(int32)
  IL_7491:  dup
  IL_7492:  ldc.i4.2
  IL_7493:  xor
  IL_7494:  call       void [System.Console]System.Console::Write(int32)
  IL_7499:  dup
  IL_749a:  ldc.i4.8
  IL_749b:  mul
  IL_749c:  call       void [System.Console]System.Console::Write(int32)
  IL_74a1:  dup
  IL_74a2:  ldc.i4.2
  IL_74a3:  xor
  IL_74a4:  call       void [System.Console]System.Console::Write(int32)
  IL_74a9:  dup
  IL_74aa:  ldc.i4.2
  IL_74ab:  mul
  IL_74ac:  call       void [System.Console]System.Console::Write(int32)
  IL_74b1:  dup
  IL_74b2:  ldc.i4.4
  IL_74b3:  add
  IL_74b4:  call       void [System.Console]System.Console::Write(int32)
  IL_74b9:  dup
  IL_74ba:  ldc.i4.0
  IL_74bb:  shr
  IL_74bc:  call       void [System.Console]System.Console::Write(int32)
  IL_74c1:  dup
  IL_74c2:  ldc.i4.6
  IL_74c3:  shl
  IL_74c4:  call       void [System.Console]System.Console::Write(int32)
  IL_74c9:  dup
  IL_74ca:  ldc.i4.8
  IL_74cb:  shl
  IL_74cc:  call       void [System.Console]System.Console::Write(int32)
  IL_74d1:  dup
  IL_74d2:  ldc.i4.8
  IL_74d3:  xor
  IL_74d4:  call       void [System.Console]System.Console::Write(int32)
  IL_74d9:  dup
  IL_74da:  ldc.i4.8
  IL_74db:  or
  IL_74dc:  call       void [System.Console]System.Console::Write(int32)
  IL_74e1:  dup
  IL_74e2:  ldc.i4.4
  IL_74e3:  xor
  IL_74e4:  call       void [System.Console]System.Console::Write(int32)
  IL_74e9:  dup
  IL_74ea:  ldc.i4.7
  IL_74eb:  shl
  IL_74ec:  call       void [System.Console]System.Console::Write(int32)
  IL_74f1:  dup
  IL_74f2:  ldc.i4.0
  IL_74f3:  and
  IL_74f4:  call       void [System.Console]System.Console::Write(int32)
  IL_74f9:  dup
  IL_74fa:  ldc.i4.3
  IL_74fb:  xor
  IL_74fc:  call       void [System.Console]System.Console::Write(int32)
  IL_7501:  dup
  IL_7502:  ldc.i4.1
  IL_7503:  or
  IL_7504:  call       void [System.Console]System.Console::Write(int32)
  IL_7509:  dup
  IL_750a:  ldc.i4.5
  IL_750b:  mul
  IL_750c:  call       void [System.Console]System.Console::Write(int32)
  IL_7511:  dup
  IL_7512:  ldc.i4.1
  IL_7513:  sub
  IL_7514:  call       void [System.Console]System.Console::Write(int32)
  IL_7519:  dup
  IL_751a:  ldc.i4.7
  IL_751b:  shr
  IL_751c:  call       void [System.Console]System.Console::Write(int32)
  IL_7521:  dup
  IL_7522:  ldc.i4.2
  IL_7523:  shl
  IL_7524:  call       void [System.Console]System.Console::Write(int32)
  IL_7529:  dup
  IL_752a:  ldc.i4.2
  IL_752b:  shl
  IL_752c:  call       void [System.Console]System.Console::Write(int32)
  IL_7531:  dup
  IL_7532:  ldc.i4.6
  IL_7533:  sub
  IL_7534:  call       void [System.Console]System.Console::Write(int32)
  IL_7539:  dup
  IL_753a:  ldc.i4.2
  IL_753b:  mul
  IL_753c:  call       void [System.Console]System.Console::Write(int32)
  IL_7541:  dup
  IL_7542:  ldc.i4.6
  IL_7543:  sub
  IL_7544:  call       void [System.Console]System.Console::Write(int32)
  IL_7549:  dup
  IL_754a:  ldc.i4.3
  IL_754b:  xor
  IL_754c:  call       void [System.Console]System.Console::Write(int32)
  IL_7551:  dup
  IL_7552:  ldc.i4.5
  IL_7553:  and
  IL_7554:  call       void [System.Console]System.Console::Write(int32)
  IL_7559:  dup
  IL_755a:  ldc.i4.4
  IL_755b:  mul
  IL_755c:  call       void [System.Console]System.Console::Write(int32)
  IL_7561:  dup
  IL_7562:  ldc.i4.6
  IL_7563:  xor
  IL_7564:  call       void [System.Console]System.Console::Write(int32)
  IL_7569:  dup
  IL_756a:  ldc.i4.7
  IL_756b:  or
  IL_756c:  call       void [System.Console]System.Console::Write(int32)
  IL_7571:  dup
  IL_7572:  ldc.i4.5
  IL_7573:  mul
  IL_7574:  call       void [System.Console]System.Console::Write(int32)
  IL_7579:  dup
  IL_757a:  ldc.i4.2
  IL_757b:  xor
  IL_757c:  call       void [System.Console]System.Console::Write(int32)
  IL_7581:  dup
  IL_7582:  ldc.i4.0
  IL_7583:  and
  IL_7584:  call       void [System.Console]System.Console::Write(int32)
  IL_7589:  dup
  IL_758a:  ldc.i4.1
  IL_758b:  shl
  IL_758c:  call       void [System.Console]System.Console::Write(int32)
  IL_7591:  dup
  IL_7592:  ldc.i4.4
  IL_7593:  shr
  IL_7594:  call       void [System.Console]System.Console::Write(int32)
  IL_7599:  dup
  IL_759a:  ldc.i4.4
  IL_759b:  shl
  IL_759c:  call       void [System.Console]System.Console::Write(int32)
  IL_75a1:  dup
  IL_75a2:  ldc.i4.6
  IL_75a3:  mul
  IL_75a4:  call       void [System.Console]System.Console::Write(int32)
  IL_75a9:  dup
  IL_75aa:  ldc.i4.3
  IL_75ab:  xor
  IL_75ac:  call       void [System.Console]System.Console::Write(int32)
  IL_75b1:  dup
  IL_75b2:  ldc.i4.1
  IL_75b3:  mul
  IL_75b4:  call       void [System.Console]System.Console::Write(int32)
  IL_75b9:  dup
  IL_75ba:  ldc.i4.1
  IL_75bb:  mul
  IL_75bc:  call       void [System.Console]System.Console::Write(int32)
  IL_75c1:  dup
  IL_75c2:  ldc.i4.1
  IL_75c3:  shl
  IL_75c4:  call       void [System.Console]System.Console::Write(int32)
  IL_75c9:  dup
  IL_75ca:  ldc.i4.7
  IL_75cb:  and
  IL_75cc:  call       void [System.Console]System.Console::Write(int32)
  IL_75d1:  dup
  IL_75d2:  ldc.i4.4
  IL_75d3:  or
  IL_75d4:  call       void [System.Console]System.Console::Write(int32)
  IL_75d9:  dup
  IL_75da:  ldc.i4.7
  IL_75db:  mul
  IL_75dc:  call       void [System.Console]System.Console::Write(int32)
  IL_75e1:  dup
  IL_75e2:  ldc.i4.3
  IL_75e3:  mul
  IL_75e4:  call       void [System.Console]System.Console::Write(int32)
  IL_75e9:  dup
  IL_75ea:  ldc.i4.5
  IL_75eb:  shr
  IL_75ec:  call       void [System.Console]System.Console::Write(int32)
  IL_75f1:  dup
  IL_75f2:  ldc.i4.2
  IL_75f3:  or
  IL_75f4:  call       void [System.Console]System.Console::Write(int32)
  IL_75f9:  dup
  IL_75fa:  ldc.i4.0
  IL_75fb:  or
  IL_75fc:  call       void [System.Console]System.Console::Write(int32)
  IL_7601:  dup
  IL_7602:  ldc.i4.2
  IL_7603:  and
  IL_7604:  call       void [System.Console]System.Console::Write(int32)
  IL_7609:  dup
  IL_760a:  ldc.i4.5
  IL_760b:  or
  IL_760c:  call       void [System.Console]System.Console::Write(int32)
  IL_7611:  dup
  IL_7612:  ldc.i4.8
  IL_7613:  shr
  IL_7614:  call       void [System.Console]System.Console::Write(int32)
  IL_7619:  dup
  IL_761a:  ldc.i4.7
  IL_761b:  add
  IL_761c:  call       void [System.Console]System.Console::Write(int32)
  IL_7621:  dup
  IL_7622:  ldc.i4.6
  IL_7623:  and
  IL_7624:  call       void [System.Console]System.Console::Write(int32)
  IL_7629:  dup
  IL_762a:  ldc.i4.5
  IL_762b:  mul
  IL_762c:  call       void [System.Console]System.Console::Write(int32)
  IL_7631:  dup
  IL_7632:  ldc.i4.4
  IL_7633:  add
  IL_7634:  call       void [System.Console]System.Console::Write(int32)
  IL_7639:  dup
  IL_763a:  ldc.i4.1
  IL_763b:  mul
  IL_763c:  call       void [System.Console]System.Console::Write(int32)
  IL_7641:  dup
  IL_7642:  ldc.i4.2
  IL_7643:  shl
  IL_7644:  call       void [System.Console]System.Console::Write(int32)
  IL_7649:  dup
  IL_764a:  ldc.i4.6
  IL_764b:  add
  IL_764c:  call       void [System.Console]System.Console::Write(int32)
  IL_7651:  dup
  IL_7652:  ldc.i4.4
  IL_7653:  xor
  IL_7654:  call       void [System.Console]System.Console::Write(int32)
  IL_7659:  dup
  IL_765a:  ldc.i4.0
  IL_765b:  add
  IL_765c:  call       void [System.Console]System.Console::Write(int32)
  IL_7661:  dup
  IL_7662:  ldc.i4.8
  IL_7663:  and
  IL_7664:  call       void [System.Console]System.Console::Write(int32)
  IL_7669:  dup
  IL_766a:  ldc.i4.1
  IL_766b:  sub
  IL_766c:  call       void [System.Console]System.Console::Write(int32)
  IL_7671:  dup
  IL_7672:  ldc.i4.0
  IL_7673:  and
  IL_7674:  call       void [System.Console]System.Console::Write(int32)
  IL_7679:  dup
  IL_767a:  ldc.i4.2
  IL_767b:  and
  IL_767c:  call       void [System.Console]System.Console::Write(int32)
  IL_7681:  dup
  IL_7682:  ldc.i4.2
  IL_7683:  xor
  IL_7684:  call       void [System.Console]System.Console::Write(int32)
  IL_7689:  dup
  IL_768a:  ldc.i4.1
  IL_768b:  xor
  IL_768c:  call       void [System.Console]System.Console::Write(int32)
  IL_7691:  dup
  IL_7692:  ldc.i4.7
  IL_7693:  sub
  IL_7694:  call       void [System.Console]System.Console::Write(int32)
  IL_7699:  dup
  IL_769a:  ldc.i4.3
  IL_769b:  shl
  IL_769c:  call       void [System.Console]System.Console::Write(int32)
  IL_76a1:  dup
  IL_76a2:  ldc.i4.6
  IL_76a3:  and
  IL_76a4:  call       void [System.Console]System.Console::Write(int32)
  IL_76a9:  dup
  IL_76aa:  ldc.i4.1
  IL_76ab:  shr
  IL_76ac:  call       void [System.Console]System.Console::Write(int32)
  IL_76b1:  dup
  IL_76b2:  ldc.i4.3
  IL_76b3:  shl
  IL_76b4:  call       void [System.Console]System.Console::Write(int32)
  IL_76b9:  dup
  IL_76ba:  ldc.i4.8
  IL_76bb:  xor
  IL_76bc:  call       void [System.Console]System.Console::Write(int32)
  IL_76c1:  dup
  IL_76c2:  ldc.i4.1
  IL_76c3:  mul
  IL_76c4:  call       void [System.Console]System.Console::Write(int32)
  IL_76c9:  dup
  IL_76ca:  ldc.i4.3
  IL_76cb:  and
  IL_76cc:  call       void [System.Console]System.Console::Write(int32)
  IL_76d1:  dup
  IL_76d2:  ldc.i4.1
  IL_76d3:  or
  IL_76d4:  call       void [System.Console]System.Console::Write(int32)
  IL_76d9:  dup
  IL_76da:  ldc.i4.4
  IL_76db:  or
  IL_76dc:  call       void [System.Console]System.Console::Write(int32)
  IL_76e1:  dup
  IL_76e2:  ldc.i4.7
  IL_76e3:  shl
  IL_76e4:  call       void [System.Console]System.Console::Write(int32)
  IL_76e9:  dup
  IL_76ea:  ldc.i4.8
  IL_76eb:  sub
  IL_76ec:  call       void [System.Console]System.Console::Write(int32)
  IL_76f1:  dup
  IL_76f2:  ldc.i4.4
  IL_76f3:  shl
  IL_76f4:  call       void [System.Console]System.Console::Write(int32)
  IL_76f9:  dup
  IL_76fa:  ldc.i4.8
  IL_76fb:  and
  IL_76fc:  call       void [System.Console]System.Console::Write(int32)
  IL_7701:  dup
  IL_7702:  ldc.i4.5
  IL_7703:  and
  IL_7704:  call       void [System.Console]System.Console::Write(int32)
  IL_7709:  dup
  IL_770a:  ldc.i4.8
  IL_770b:  add
  IL_770c:  call       void [System.Console]System.Console::Write(int32)
  IL_7711:  dup
  IL_7712:  ldc.i4.8
  IL_7713:  and
  IL_7714:  call       void [System.Console]System.Console::Write(int32)
  IL_7719:  dup
  IL_771a:  ldc.i4.4
  IL_771b:  add
  IL_771c:  call       void [System.Console]System.Console::Write(int32)
  IL_7721:  dup
  IL_7722:  ldc.i4.1
  IL_7723:  sub
  IL_7724:  call       void [System.Console]System.Console::Write(int32)
  IL_7729:  dup
  IL_772a:  ldc.i4.2
  IL_772b:  shl
  IL_772c:  call       void [System.Console]System.Console::Write(int32)
  IL_7731:  dup
  IL_7732:  ldc.i4.1
  IL_7733:  xor
  IL_7734:  call       void [System.Console]System.Console::Write(int32)
  IL_7739:  dup
  IL_773a:  ldc.i4.5
  IL_773b:  xor
  IL_773c:  call       void [System.Console]System.Console::Write(int32)
  IL_7741:  dup
  IL_7742:  ldc.i4.8
  IL_7743:  and
  IL_7744:  call       void [System.Console]System.Console::Write(int32)
  IL_7749:  dup
  IL_774a:  ldc.i4.0
  IL_774b:  shr
  IL_774c:  call       void [System.Console]System.Console::Write(int32)
  IL_7751:  dup
  IL_7752:  ldc.i4.3
  IL_7753:  xor
  IL_7754:  call       void [System.Console]System.Console::Write(int32)
  IL_7759:  dup
  IL_775a:  ldc.i4.5
  IL_775b:  mul
  IL_775c:  call       void [System.Console]System.Console::Write(int32)
  IL_7761:  dup
  IL_7762:  ldc.i4.8
  IL_7763:  sub
  IL_7764:  call       void [System.Console]System.Console::Write(int32)
  IL_7769:  dup
  IL_776a:  ldc.i4.5
  IL_776b:  sub
  IL_776c:  call       void [System.Console]System.Console::Write(int32)
  IL_7771:  dup
  IL_7772:  ldc.i4.2
  IL_7773:  sub
  IL_7774:  call       void [System.Console]System.Console::Write(int32)
  IL_7779:  dup
  IL_777a:  ldc.i4.0
  IL_777b:  sub
  IL_777c:  call       void [System.Console]System.Console::Write(int32)
  IL_7781:  dup
  IL_7782:  ldc.i4.8
  IL_7783:  mul
  IL_7784:  call       void [System.Console]System.Console::Write(int32)
  IL_7789:  dup
  IL_778a:  ldc.i4.0
  IL_778b:  shl
  IL_778c:  call       void [System.Console]System.Console::Write(int32)
  IL_7791:  dup
  IL_7792:  ldc.i4.4
  IL_7793:  and
  IL_7794:  call       void [System.Console]System.Console::Write(int32)
  IL_7799:  dup
  IL_779a:  ldc.i4.4
  IL_779b:  xor
  IL_779c:  call       void [System.Console]System.Console::Write(int32)
  IL_77a1:  dup
  IL_77a2:  ldc.i4.7
  IL_77a3:  mul
  IL_77a4:  call       void [System.Console]System.Console::Write(int32)
  IL_77a9:  dup
  IL_77aa:  ldc.i4.5
  IL_77ab:  add
  IL_77ac:  call       void [System.Console]System.Console::Write(int32)
  IL_77b1:  dup
  IL_77b2:  ldc.i4.5
  IL_77b3:  and
  IL_77b4:  call       void [System.Console]System.Console::Write(int32)
  IL_77b9:  dup
  IL_77ba:  ldc.i4.6
  IL_77bb:  xor
  IL_77bc:  call       void [System.Console]System.Console::Write(int32)
  IL_77c1:  dup
  IL_77c2:  ldc.i4.7
  IL_77c3:  shl
  IL_77c4:  call       void [System.Console]System.Console::Write(int32)
  IL_77c9:  dup
  IL_77ca:  ldc.i4.1
  IL_77cb:  shr
  IL_77cc:  call       void [System.Console]System.Console::Write(int32)
  IL_77d1:  dup
  IL_77d2:  ldc.i4.4
  IL_77d3:  sub
  IL_77d4:  call       void [System.Console]System.Console::Write(int32)
  IL_77d9:  dup
  IL_77da:  ldc.i4.3
  IL_77db:  sub
  IL_77dc:  call       void [System.Console]System.Console::Write(int32)
  IL_77e1:  dup
  IL_77e2:  ldc.i4.0
  IL_77e3:  shl
  IL_77e4:  call       void [System.Console]System.Console::Write(int32)
  IL_77e9:  dup
  IL_77ea:  ldc.i4.1
  IL_77eb:  shr
  IL_77ec:  call       void [System.Console]System.Console::Write(int32)
  IL_77f1:  dup
  IL_77f2:  ldc.i4.1
  IL_77f3:  mul
  IL_77f4:  call       void [System.Console]System.Console::Write(int32)
  IL_77f9:  dup
  IL_77fa:  ldc.i4.7
  IL_77fb:  or
  IL_77fc:  call       void [System.Console]System.Console::Write(int32)
  IL_7801:  dup
  IL_7802:  ldc.i4.0
  IL_7803:  sub
  IL_7804:  call       void [System.Console]System.Console::Write(int32)
  IL_7809:  dup
  IL_780a:  ldc.i4.6
  IL_780b:  and
  IL_780c:  call       void [System.Console]System.Console::Write(int32)
  IL_7811:  dup
  IL_7812:  ldc.i4.2
  IL_7813:  add
  IL_7814:  call       void [System.Console]System.Console::Write(int32)
  IL_7819:  dup
  IL_781a:  ldc.i4.7
  IL_781b:  add
  IL_781c:  call       void [System.Console]System.Console::Write(int32)
  IL_7821:  dup
  IL_7822:  ldc.i4.2
  IL_7823:  shr
  IL_7824:  call       void [System.Console]System.Console::Write(int32)
  IL_7829:  dup
  IL_782a:  ldc.i4.8
  IL_782b:  add
  IL_782c:  call       void [System.Console]System.Console::Write(int32)
  IL_7831:  dup
  IL_7832:  ldc.i4.6
  IL_7833:  shr
  IL_7834:  call       void [System.Console]System.Console::Write(int32)
  IL_7839:  dup
  IL_783a:  ldc.i4.6
  IL_783b:  add
  IL_783c:  call       void [System.Console]System.Console::Write(int32)
  IL_7841:  dup
  IL_7842:  ldc.i4.4
  IL_7843:  sub
  IL_7844:  call       void [System.Console]System.Console::Write(int32)
  IL_7849:  dup
  IL_784a:  ldc.i4.3
  IL_784b:  shr
  IL_784c:  call       void [System.Console]System.Console::Write(int32)
  IL_7851:  dup
  IL_7852:  ldc.i4.8
  IL_7853:  xor
  IL_7854:  call       void [System.Console]System.Console::Write(int32)
  IL_7859:  dup
  IL_785a:  ldc.i4.5
  IL_785b:  shr
  IL_785c:  call       void [System.Console]System.Console::Write(int32)
  IL_7861:  dup
  IL_7862:  ldc.i4.8
  IL_7863:  and
  IL_7864:  call       void [System.Console]System.Console::Write(int32)
  IL_7869:  dup
  IL_786a:  ldc.i4.4
  IL_786b:  or
  IL_786c:  call       void [System.Console]System.Console::Write(int32)
  IL_7871:  dup
  IL_7872:  ldc.i4.6
  IL_7873:  and
  IL_7874:  call       void [System.Console]System.Console::Write(int32)
  IL_7879:  dup
  IL_787a:  ldc.i4.1
  IL_787b:  xor
  IL_787c:  call       void [System.Console]System.Console::Write(int32)
  IL_7881:  dup
  IL_7882:  ldc.i4.5
  IL_7883:  shr
  IL_7884:  call       void [System.Console]System.Console::Write(int32)
  IL_7889:  dup
  IL_788a:  ldc.i4.8
  IL_788b:  xor
  IL_788c:  call       void [System.Console]System.Console::Write(int32)
  IL_7891:  dup
  IL_7892:  ldc.i4.1
  IL_7893:  and
  IL_7894:  call       void [System.Console]System.Console::Write(int32)
  IL_7899:  dup
  IL_789a:  ldc.i4.0
  IL_789b:  and
  IL_789c:  call       void [System.Console]System.Console::Write(int32)
  IL_78a1:  dup
  IL_78a2:  ldc.i4.6
  IL_78a3:  sub
  IL_78a4:  call       void [System.Console]System.Console::Write(int32)
  IL_78a9:  dup
  IL_78aa:  ldc.i4.2
  IL_78ab:  or
  IL_78ac:  call       void [System.Console]System.Console::Write(int32)
  IL_78b1:  dup
  IL_78b2:  ldc.i4.2
  IL_78b3:  mul
  IL_78b4:  call       void [System.Console]System.Console::Write(int32)
  IL_78b9:  dup
  IL_78ba:  ldc.i4.3
  IL_78bb:  xor
  IL_78bc:  call       void [System.Console]System.Console::Write(int32)
  IL_78c1:  dup
  IL_78c2:  ldc.i4.8
  IL_78c3:  add
  IL_78c4:  call       void [System.Console]System.Console::Write(int32)
  IL_78c9:  dup
  IL_78ca:  ldc.i4.0
  IL_78cb:  or
  IL_78cc:  call       void [System.Console]System.Console::Write(int32)
  IL_78d1:  dup
  IL_78d2:  ldc.i4.8
  IL_78d3:  add
  IL_78d4:  call       void [System.Console]System.Console::Write(int32)
  IL_78d9:  dup
  IL_78da:  ldc.i4.8
  IL_78db:  shr
  IL_78dc:  call       void [System.Console]System.Console::Write(int32)
  IL_78e1:  dup
  IL_78e2:  ldc.i4.5
  IL_78e3:  add
  IL_78e4:  call       void [System.Console]System.Console::Write(int32)
  IL_78e9:  dup
  IL_78ea:  ldc.i4.8
  IL_78eb:  xor
  IL_78ec:  call       void [System.Console]System.Console::Write(int32)
  IL_78f1:  dup
  IL_78f2:  ldc.i4.5
  IL_78f3:  or
  IL_78f4:  call       void [System.Console]System.Console::Write(int32)
  IL_78f9:  dup
  IL_78fa:  ldc.i4.3
  IL_78fb:  xor
  IL_78fc:  call       void [System.Console]System.Console::Write(int32)
  IL_7901:  dup
  IL_7902:  ldc.i4.4
  IL_7903:  xor
  IL_7904:  call       void [System.Console]System.Console::Write(int32)
  IL_7909:  dup
  IL_790a:  ldc.i4.4
  IL_790b:  add
  IL_790c:  call       void [System.Console]System.Console::Write(int32)
  IL_7911:  dup
  IL_7912:  ldc.i4.6
  IL_7913:  sub
  IL_7914:  call       void [System.Console]System.Console::Write(int32)
  IL_7919:  dup
  IL_791a:  ldc.i4.8
  IL_791b:  shr
  IL_791c:  call       void [System.Console]System.Console::Write(int32)
  IL_7921:  dup
  IL_7922:  ldc.i4.1
  IL_7923:  shl
  IL_7924:  call       void [System.Console]System.Console::Write(int32)
  IL_7929:  dup
  IL_792a:  ldc.i4.8
  IL_792b:  and
  IL_792c:  call       void [System.Console]System.Console::Write(int32)
  IL_7931:  dup
  IL_7932:  ldc.i4.1
  IL_7933:  shr
  IL_7934:  call       void [System.Console]System.Console::Write(int32)
  IL_7939:  dup
  IL_793a:  ldc.i4.3
  IL_793b:  shl
  IL_793c:  call       void [System.Console]System.Console::Write(int32)
  IL_7941:  dup
  IL_7942:  ldc.i4.3
  IL_7943:  add
  IL_7944:  call       void [System.Console]System.Console::Write(int32)
  IL_7949:  dup
  IL_794a:  ldc.i4.0
  IL_794b:  shr
  IL_794c:  call       void [System.Console]System.Console::Write(int32)
  IL_7951:  dup
  IL_7952:  ldc.i4.6
  IL_7953:  sub
  IL_7954:  call       void [System.Console]System.Console::Write(int32)
  IL_7959:  dup
  IL_795a:  ldc.i4.4
  IL_795b:  xor
  IL_795c:  call       void [System.Console]System.Console::Write(int32)
  IL_7961:  dup
  IL_7962:  ldc.i4.6
  IL_7963:  sub
  IL_7964:  call       void [System.Console]System.Console::Write(int32)
  IL_7969:  dup
  IL_796a:  ldc.i4.7
  IL_796b:  add
  IL_796c:  call       void [System.Console]System.Console::Write(int32)
  IL_7971:  dup
  IL_7972:  ldc.i4.8
  IL_7973:  shl
  IL_7974:  call       void [System.Console]System.Console::Write(int32)
  IL_7979:  dup
  IL_797a:  ldc.i4.2
  IL_797b:  shr
  IL_797c:  call       void [System.Console]System.Console::Write(int32)
  IL_7981:  dup
  IL_7982:  ldc.i4.7
  IL_7983:  and
  IL_7984:  call       void [System.Console]System.Console::Write(int32)
  IL_7989:  dup
  IL_798a:  ldc.i4.3
  IL_798b:  and
  IL_798c:  call       void [System.Console]System.Console::Write(int32)
  IL_7991:  dup
  IL_7992:  ldc.i4.7
  IL_7993:  xor
  IL_7994:  call       void [System.Console]System.Console::Write(int32)
  IL_7999:  dup
  IL_799a:  ldc.i4.6
  IL_799b:  shr
  IL_799c:  call       void [System.Console]System.Console::Write(int32)
  IL_79a1:  dup
  IL_79a2:  ldc.i4.1
  IL_79a3:  xor
  IL_79a4:  call       void [System.Console]System.Console::Write(int32)
  IL_79a9:  dup
  IL_79aa:  ldc.i4.7
  IL_79ab:  xor
  IL_79ac:  call       void [System.Console]System.Console::Write(int32)
  IL_79b1:  dup
  IL_79b2:  ldc.i4.1
  IL_79b3:  add
  IL_79b4:  call       void [System.Console]System.Console::Write(int32)
  IL_79b9:  dup
  IL_79ba:  ldc.i4.5
  IL_79bb:  or
  IL_79bc:  call       void [System.Console]System.Console::Write(int32)
  IL_79c1:  dup
  IL_79c2:  ldc.i4.6
  IL_79c3:  mul
  IL_79c4:  call       void [System.Console]System.Console::Write(int32)
  IL_79c9:  dup
  IL_79ca:  ldc.i4.0
  IL_79cb:  shl
  IL_79cc:  call       void [System.Console]System.Console::Write(int32)
  IL_79d1:  dup
  IL_79d2:  ldc.i4.8
  IL_79d3:  shr
  IL_79d4:  call       void [System.Console]System.Console::Write(int32)
  IL_79d9:  dup
  IL_79da:  ldc.i4.6
  IL_79db:  sub
  IL_79dc:  call       void [System.Console]System.Console::Write(int32)
  IL_79e1:  dup
  IL_79e2:  ldc.i4.8
  IL_79e3:  add
  IL_79e4:  call       void [System.Console]System.Console::Write(int32)
  IL_79e9:  dup
  IL_79ea:  ldc.i4.2
  IL_79eb:  shl
  IL_79ec:  call       void [System.Console]System.Console::Write(int32)
  IL_79f1:  dup
  IL_79f2:  ldc.i4.3
  IL_79f3:  and
  IL_79f4:  call       void [System.Console]System.Console::Write(int32)
  IL_79f9:  dup
  IL_79fa:  ldc.i4.6
  IL_79fb:  shl
  IL_79fc:  call       void [System.Console]System.Console::Write(int32)
  IL_7a01:  dup
  IL_7a02:  ldc.i4.2
  IL_7a03:  mul
  IL_7a04:  call       void [System.Console]System.Console::Write(int32)
  IL_7a09:  dup
  IL_7a0a:  ldc.i4.0
  IL_7a0b:  mul
  IL_7a0c:  call       void [System.Console]System.Console::Write(int32)
  IL_7a11:  dup
  IL_7a12:  ldc.i4.2
  IL_7a13:  shl
  IL_7a14:  call       void [System.Console]System.Console::Write(int32)
  IL_7a19:  dup
  IL_7a1a:  ldc.i4.7
  IL_7a1b:  add
  IL_7a1c:  call       void [System.Console]System.Console::Write(int32)
  IL_7a21:  dup
  IL_7a22:  ldc.i4.2
  IL_7a23:  mul
  IL_7a24:  call       void [System.Console]System.Console::Write(int32)
  IL_7a29:  dup
  IL_7a2a:  ldc.i4.4
  IL_7a2b:  mul
  IL_7a2c:  call       void [System.Console]System.Console::Write(int32)
  IL_7a31:  dup
  IL_7a32:  ldc.i4.3
  IL_7a33:  add
  IL_7a34:  call       void [System.Console]System.Console::Write(int32)
  IL_7a39:  dup
  IL_7a3a:  ldc.i4.2
  IL_7a3b:  shr
  IL_7a3c:  call       void [System.Console]System.Console::Write(int32)
  IL_7a41:  dup
  IL_7a42:  ldc.i4.5
  IL_7a43:  or
  IL_7a44:  call       void [System.Console]System.Console::Write(int32)
  IL_7a49:  dup
  IL_7a4a:  ldc.i4.3
  IL_7a4b:  shr
  IL_7a4c:  call       void [System.Console]System.Console::Write(int32)
  IL_7a51:  dup
  IL_7a52:  ldc.i4.5
  IL_7a53:  shr
  IL_7a54:  call       void [System.Console]System.Console::Write(int32)
  IL_7a59:  dup
  IL_7a5a:  ldc.i4.7
  IL_7a5b:  sub
  IL_7a5c:  call       void [System.Console]System.Console::Write(int32)
  IL_7a61:  dup
  IL_7a62:  ldc.i4.0
  IL_7a63:  sub
  IL_7a64:  call       void [System.Console]System.Console::Write(int32)
  IL_7a69:  dup
  IL_7a6a:  ldc.i4.7
  IL_7a6b:  shr
  IL_7a6c:  call       void [System.Console]System.Console::Write(int32)
  IL_7a71:  dup
  IL_7a72:  ldc.i4.1
  IL_7a73:  sub
  IL_7a74:  call       void [System.Console]System.Console::Write(int32)
  IL_7a79:  dup
  IL_7a7a:  ldc.i4.2
  IL_7a7b:  mul
  IL_7a7c:  call       void [System.Console]System.Console::Write(int32)
  IL_7a81:  dup
  IL_7a82:  ldc.i4.6
  IL_7a83:  mul
  IL_7a84:  call       void [System.Console]System.Console::Write(int32)
  IL_7a89:  dup
  IL_7a8a:  ldc.i4.0
  IL_7a8b:  and
  IL_7a8c:  call       void [System.Console]System.Console::Write(int32)
  IL_7a91:  dup
  IL_7a92:  ldc.i4.0
  IL_7a93:  mul
  IL_7a94:  call       void [System.Console]System.Console::Write(int32)
  IL_7a99:  dup
  IL_7a9a:  ldc.i4.5
  IL_7a9b:  add
  IL_7a9c:  call       void [System.Console]System.Console::Write(int32)
  IL_7aa1:  dup
  IL_7aa2:  ldc.i4.4
  IL_7aa3:  add
  IL_7aa4:  call       void [System.Console]System.Console::Write(int32)
  IL_7aa9:  dup
  IL_7aaa:  ldc.i4.2
  IL_7aab:  shl
  IL_7aac:  call       void [System.Console]System.Console::Write(int32)
  IL_7ab1:  dup
  IL_7ab2:  ldc.i4.1
  IL_7ab3:  xor
  IL_7ab4:  call       void [System.Console]System.Console::Write(int32)
  IL_7ab9:  dup
  IL_7aba:  ldc.i4.7
  IL_7abb:  or
  IL_7abc:  call       void [System.Console]System.Console::Write(int32)
  IL_7ac1:  dup
  IL_7ac2:  ldc.i4.6
  IL_7ac3:  shl
  IL_7ac4:  call       void [System.Console]System.Console::Write(int32)
  IL_7ac9:  dup
  IL_7aca:  ldc.i4.7
  IL_7acb:  mul
  IL_7acc:  call       void [System.Console]System.Console::Write(int32)
  IL_7ad1:  dup
  IL_7ad2:  ldc.i4.3
  IL_7ad3:  add
  IL_7ad4:  call       void [System.Console]System.Console::Write(int32)
  IL_7ad9:  dup
  IL_7ada:  ldc.i4.8
  IL_7adb:  shr
  IL_7adc:  call       void [System.Console]System.Console::Write(int32)
  IL_7ae1:  dup
  IL_7ae2:  ldc.i4.4
  IL_7ae3:  or
  IL_7ae4:  call       void [System.Console]System.Console::Write(int32)
  IL_7ae9:  dup
  IL_7aea:  ldc.i4.1
  IL_7aeb:  add
  IL_7aec:  call       void [System.Console]System.Console::Write(int32)
  IL_7af1:  dup
  IL_7af2:  ldc.i4.6
  IL_7af3:  add
  IL_7af4:  call       void [System.Console]System.Console::Write(int32)
  IL_7af9:  dup
  IL_7afa:  ldc.i4.7
  IL_7afb:  sub
  IL_7afc:  call       void [System.Console]System.Console::Write(int32)
  IL_7b01:  dup
  IL_7b02:  ldc.i4.0
  IL_7b03:  sub
  IL_7b04:  call       void [System.Console]System.Console::Write(int32)
  IL_7b09:  dup
  IL_7b0a:  ldc.i4.5
  IL_7b0b:  or
  IL_7b0c:  call       void [System.Console]System.Console::Write(int32)
  IL_7b11:  dup
  IL_7b12:  ldc.i4.8
  IL_7b13:  xor
  IL_7b14:  call       void [System.Console]System.Console::Write(int32)
  IL_7b19:  dup
  IL_7b1a:  ldc.i4.5
  IL_7b1b:  mul
  IL_7b1c:  call       void [System.Console]System.Console::Write(int32)
  IL_7b21:  dup
  IL_7b22:  ldc.i4.3
  IL_7b23:  mul
  IL_7b24:  call       void [System.Console]System.Console::Write(int32)
  IL_7b29:  dup
  IL_7b2a:  ldc.i4.1
  IL_7b2b:  shl
  IL_7b2c:  call       void [System.Console]System.Console::Write(int32)
  IL_7b31:  dup
  IL_7b32:  ldc.i4.4
  IL_7b33:  sub
  IL_7b34:  call       void [System.Console]System.Console::Write(int32)
  IL_7b39:  dup
  IL_7b3a:  ldc.i4.8
  IL_7b3b:  shr
  IL_7b3c:  call       void [System.Console]System.Console::Write(int32)
  IL_7b41:  dup
  IL_7b42:  ldc.i4.2
  IL_7b43:  or
  IL_7b44:  call       void [System.Console]System.Console::Write(int32)
  IL_7b49:  dup
  IL_7b4a:  ldc.i4.8
  IL_7b4b:  shl
  IL_7b4c:  call       void [System.Console]System.Console::Write(int32)
  IL_7b51:  dup
  IL_7b52:  ldc.i4.2
  IL_7b53:  or
  IL_7b54:  call       void [System.Console]System.Console::Write(int32)
  IL_7b59:  dup
  IL_7b5a:  ldc.i4.0
  IL_7b5b:  shr
  IL_7b5c:  call       void [System.Console]System.Console::Write(int32)
  IL_7b61:  dup
  IL_7b62:  ldc.i4.3
  IL_7b63:  xor
  IL_7b64:  call       void [System.Console]System.Console::Write(int32)
  IL_7b69:  dup
  IL_7b6a:  ldc.i4.3
  IL_7b6b:  sub
  IL_7b6c:  call       void [System.Console]System.Console::Write(int32)
  IL_7b71:  dup
  IL_7b72:  ldc.i4.0
  IL_7b73:  shl
  IL_7b74:  call       void [System.Console]System.Console::Write(int32)
  IL_7b79:  dup
  IL_7b7a:  ldc.i4.3
  IL_7b7b:  xor
  IL_7b7c:  call       void [System.Console]System.Console::Write(int32)
  IL_7b81:  dup
  IL_7b82:  ldc.i4.1
  IL_7b83:  add
  IL_7b84:  call       void [System.Console]System.Console::Write(int32)
  IL_7b89:  dup
  IL_7b8a:  ldc.i4.4
  IL_7b8b:  add
  IL_7b8c:  call       void [System.Console]System.Console::Write(int32)
  IL_7b91:  dup
  IL_7b92:  ldc.i4.8
  IL_7b93:  and
  IL_7b94:  call       void [System.Console]System.Console::Write(int32)
  IL_7b99:  dup
  IL_7b9a:  ldc.i4.8
  IL_7b9b:  mul
  IL_7b9c:  call       void [System.Console]System.Console::Write(int32)
  IL_7ba1:  dup
  IL_7ba2:  ldc.i4.0
  IL_7ba3:  xor
  IL_7ba4:  call       void [System.Console]System.Console::Write(int32)
  IL_7ba9:  dup
  IL_7baa:  ldc.i4.0
  IL_7bab:  add
  IL_7bac:  call       void [System.Console]System.Console::Write(int32)
  IL_7bb1:  dup
  IL_7bb2:  ldc.i4.7
  IL_7bb3:  mul
  IL_7bb4:  call       void [System.Console]System.Console::Write(int32)
  IL_7bb9:  dup
  IL_7bba:  ldc.i4.1
  IL_7bbb:  mul
  IL_7bbc:  call       void [System.Console]System.Console::Write(int32)
  IL_7bc1:  dup
  IL_7bc2:  ldc.i4.8
  IL_7bc3:  shl
  IL_7bc4:  call       void [System.Console]System.Console::Write(int32)
  IL_7bc9:  dup
  IL_7bca:  ldc.i4.6
  IL_7bcb:  and
  IL_7bcc:  call       void [System.Console]System.Console::Write(int32)
  IL_7bd1:  dup
  IL_7bd2:  ldc.i4.0
  IL_7bd3:  mul
  IL_7bd4:  call       void [System.Console]System.Console::Write(int32)
  IL_7bd9:  dup
  IL_7bda:  ldc.i4.7
  IL_7bdb:  add
  IL_7bdc:  call       void [System.Console]System.Console::Write(int32)
  IL_7be1:  dup
  IL_7be2:  ldc.i4.1
  IL_7be3:  and
  IL_7be4:  call       void [System.Console]System.Console::Write(int32)
  IL_7be9:  dup
  IL_7bea:  ldc.i4.4
  IL_7beb:  shl
  IL_7bec:  call       void [System.Console]System.Console::Write(int32)
  IL_7bf1:  dup
  IL_7bf2:  ldc.i4.0
  IL_7bf3:  xor
  IL_7bf4:  call       void [System.Console]System.Console::Write(int32)
  IL_7bf9:  dup
  IL_7bfa:  ldc.i4.2
  IL_7bfb:  and
  IL_7bfc:  call       void [System.Console]System.Console::Write(int32)
  IL_7c01:  dup
  IL_7c02:  ldc.i4.8
  IL_7c03:  shr
  IL_7c04:  call       void [System.Console]System.Console::Write(int32)
  IL_7c09:  dup
  IL_7c0a:  ldc.i4.3
  IL_7c0b:  mul
  IL_7c0c:  call       void [System.Console]System.Console::Write(int32)
  IL_7c11:  dup
  IL_7c12:  ldc.i4.6
  IL_7c13:  or
  IL_7c14:  call       void [System.Console]System.Console::Write(int32)
  IL_7c19:  dup
  IL_7c1a:  ldc.i4.6
  IL_7c1b:  xor
  IL_7c1c:  call       void [System.Console]System.Console::Write(int32)
  IL_7c21:  dup
  IL_7c22:  ldc.i4.0
  IL_7c23:  shr
  IL_7c24:  call       void [System.Console]System.Console::Write(int32)
  IL_7c29:  dup
  IL_7c2a:  ldc.i4.4
  IL_7c2b:  and
  IL_7c2c:  call       void [System.Console]System.Console::Write(int32)
  IL_7c31:  dup
  IL_7c32:  ldc.i4.8
  IL_7c33:  or
  IL_7c34:  call       void [System.Console]System.Console::Write(int32)
  IL_7c39:  dup
  IL_7c3a:  ldc.i4.7
  IL_7c3b:  add
  IL_7c3c:  call       void [System.Console]System.Console::Write(int32)
  IL_7c41:  dup
  IL_7c42:  ldc.i4.2
  IL_7c43:  add
  IL_7c44:  call       void [System.Console]System.Console::Write(int32)
  IL_7c49:  dup
  IL_7c4a:  ldc.i4.2
  IL_7c4b:  shl
  IL_7c4c:  call       void [System.Console]System.Console::Write(int32)
  IL_7c51:  dup
  IL_7c52:  ldc.i4.7
  IL_7c53:  shl
  IL_7c54:  call       void [System.Console]System.Console::Write(int32)
  IL_7c59:  dup
  IL_7c5a:  ldc.i4.3
  IL_7c5b:  and
  IL_7c5c:  call       void [System.Console]System.Console::Write(int32)
  IL_7c61:  dup
  IL_7c62:  ldc.i4.2
  IL_7c63:  xor
  IL_7c64:  call       void [System.Console]System.Console::Write(int32)
  IL_7c69:  dup
  IL_7c6a:  ldc.i4.7
  IL_7c6b:  add
  IL_7c6c:  call       void [System.Console]System.Console::Write(int32)
  IL_7c71:  dup
  IL_7c72:  ldc.i4.2
  IL_7c73:  xor
  IL_7c74:  call       void [System.Console]System.Console::Write(int32)
  IL_7c79:  dup
  IL_7c7a:  ldc.i4.2
  IL_7c7b:  shr
  IL_7c7c:  call       void [System.Console]System.Console::Write(int32)
  IL_7c81:  dup
  IL_7c82:  ldc.i4.1
  IL_7c83:  sub
  IL_7c84:  call       void [System.Console]System.Console::Write(int32)
  IL_7c89:  dup
  IL_7c8a:  ldc.i4.0
  IL_7c8b:  sub
  IL_7c8c:  call       void [System.Console]System.Console::Write(int32)
  IL_7c91:  dup
  IL_7c92:  ldc.i4.8
  IL_7c93:  and
  IL_7c94:  call       void [System.Console]System.Console::Write(int32)
  IL_7c99:  dup
  IL_7c9a:  ldc.i4.5
  IL_7c9b:  or
  IL_7c9c:  call       void [System.Console]System.Console::Write(int32)
  IL_7ca1:  dup
  IL_7ca2:  ldc.i4.7
  IL_7ca3:  mul
  IL_7ca4:  call       void [System.Console]System.Console::Write(int32)
  IL_7ca9:  dup
  IL_7caa:  ldc.i4.6
  IL_7cab:  xor
  IL_7cac:  call       void [System.Console]System.Console::Write(int32)
  IL_7cb1:  dup
  IL_7cb2:  ldc.i4.7
  IL_7cb3:  add
  IL_7cb4:  call       void [System.Console]System.Console::Write(int32)
  IL_7cb9:  dup
  IL_7cba:  ldc.i4.1
  IL_7cbb:  sub
  IL_7cbc:  call       void [System.Console]System.Console::Write(int32)
  IL_7cc1:  dup
  IL_7cc2:  ldc.i4.0
  IL_7cc3:  xor
  IL_7cc4:  call       void [System.Console]System.Console::Write(int32)
  IL_7cc9:  dup
  IL_7cca:  ldc.i4.1
  IL_7ccb:  or
  IL_7ccc:  call       void [System.Console]System.Console::Write(int32)
  IL_7cd1:  dup
  IL_7cd2:  ldc.i4.1
  IL_7cd3:  and
  IL_7cd4:  call       void [System.Console]System.Console::Write(int32)
  IL_7cd9:  dup
  IL_7cda:  ldc.i4.0
  IL_7cdb:  mul
  IL_7cdc:  call       void [System.Console]System.Console::Write(int32)
  IL_7ce1:  dup
  IL_7ce2:  ldc.i4.2
  IL_7ce3:  shl
  IL_7ce4:  call       void [System.Console]System.Console::Write(int32)
  IL_7ce9:  dup
  IL_7cea:  ldc.i4.0
  IL_7ceb:  mul
  IL_7cec:  call       void [System.Console]System.Console::Write(int32)
  IL_7cf1:  dup
  IL_7cf2:  ldc.i4.1
  IL_7cf3:  shr
  IL_7cf4:  call       void [System.Console]System.Console::Write(int32)
  IL_7cf9:  dup
  IL_7cfa:  ldc.i4.1
  IL_7cfb:  shr
  IL_7cfc:  call       void [System.Console]System.Console::Write(int32)
  IL_7d01:  dup
  IL_7d02:  ldc.i4.3
  IL_7d03:  and
  IL_7d04:  call       void [System.Console]System.Console::Write(int32)
  IL_7d09:  dup
  IL_7d0a:  ldc.i4.5
  IL_7d0b:  shr
  IL_7d0c:  call       void [System.Console]System.Console::Write(int32)
  IL_7d11:  dup
  IL_7d12:  ldc.i4.8
  IL_7d13:  shr
  IL_7d14:  call       void [System.Console]System.Console::Write(int32)
  IL_7d19:  dup
  IL_7d1a:  ldc.i4.0
  IL_7d1b:  xor
  IL_7d1c:  call       void [System.Console]System.Console::Write(int32)
  IL_7d21:  dup
  IL_7d22:  ldc.i4.4
  IL_7d23:  mul
  IL_7d24:  call       void [System.Console]System.Console::Write(int32)
  IL_7d29:  dup
  IL_7d2a:  ldc.i4.6
  IL_7d2b:  xor
  IL_7d2c:  call       void [System.Console]System.Console::Write(int32)
  IL_7d31:  dup
  IL_7d32:  ldc.i4.7
  IL_7d33:  or
  IL_7d34:  call       void [System.Console]System.Console::Write(int32)
  IL_7d39:  dup
  IL_7d3a:  ldc.i4.8
  IL_7d3b:  mul
  IL_7d3c:  call       void [System.Console]System.Console::Write(int32)
  IL_7d41:  dup
  IL_7d42:  ldc.i4.2
  IL_7d43:  shr
  IL_7d44:  call       void [System.Console]System.Console::Write(int32)
  IL_7d49:  dup
  IL_7d4a:  ldc.i4.0
  IL_7d4b:  and
  IL_7d4c:  call       void [System.Console]System.Console::Write(int32)
  IL_7d51:  dup
  IL_7d52:  ldc.i4.1
  IL_7d53:  mul
  IL_7d54:  call       void [System.Console]System.Console::Write(int32)
  IL_7d59:  dup
  IL_7d5a:  ldc.i4.4
  IL_7d5b:  xor
  IL_7d5c:  call       void [System.Console]System.Console::Write(int32)
  IL_7d61:  dup
  IL_7d62:  ldc.i4.1
  IL_7d63:  shr
  IL_7d64:  call       void [System.Console]System.Console::Write(int32)
  IL_7d69:  dup
  IL_7d6a:  ldc.i4.7
  IL_7d6b:  xor
  IL_7d6c:  call       void [System.Console]System.Console::Write(int32)
  IL_7d71:  dup
  IL_7d72:  ldc.i4.6
  IL_7d73:  mul
  IL_7d74:  call       void [System.Console]System.Console::Write(int32)
  IL_7d79:  dup
  IL_7d7a:  ldc.i4.7
  IL_7d7b:  add
  IL_7d7c:  call       void [System.Console]System.Console::Write(int32)
  IL_7d81:  dup
  IL_7d82:  ldc.i4.1
  IL_7d83:  add
  IL_7d84:  call       void [System.Console]System.Console::Write(int32)
  IL_7d89:  dup
  IL_7d8a:  ldc.i4.0
  IL_7d8b:  add
  IL_7d8c:  call       void [System.Console]System.Console::Write(int32)
  IL_7d91:  dup
  IL_7d92:  ldc.i4.3
  IL_7d93:  xor
  IL_7d94:  call       void [System.Console]System.Console::Write(int32)
  IL_7d99:  dup
  IL_7d9a:  ldc.i4.1
  IL_7d9b:  mul
  IL_7d9c:  call       void [System.Console]System.Console::Write(int32)
  IL_7da1:  dup
  IL_7da2:  ldc.i4.3
  IL_7da3:  shr
  IL_7da4:  call       void [System.Console]System.Console::Write(int32)
  IL_7da9:  dup
  IL_7daa:  ldc.i4.4
  IL_7dab:  and
  IL_7dac:  call       void [System.Console]System.Console::Write(int32)
  IL_7db1:  dup
  IL_7db2:  ldc.i4.0
  IL_7db3:  shl
  IL_7db4:  call       void [System.Console]System.Console::Write(int32)
  IL_7db9:  dup
  IL_7dba:  ldc.i4.4
  IL_7dbb:  mul
  IL_7dbc:  call       void [System.Console]System.Console::Write(int32)
  IL_7dc1:  dup
  IL_7dc2:  ldc.i4.5
  IL_7dc3:  and
  IL_7dc4:  call       void [System.Console]System.Console::Write(int32)
  IL_7dc9:  dup
  IL_7dca:  ldc.i4.0
  IL_7dcb:  xor
  IL_7dcc:  call       void [System.Console]System.Console::Write(int32)
  IL_7dd1:  dup
  IL_7dd2:  ldc.i4.5
  IL_7dd3:  mul
  IL_7dd4:  call       void [System.Console]System.Console::Write(int32)
  IL_7dd9:  dup
  IL_7dda:  ldc.i4.4
  IL_7ddb:  sub
  IL_7ddc:  call       void [System.Console]System.Console::Write(int32)
  IL_7de1:  dup
  IL_7de2:  ldc.i4.3
  IL_7de3:  sub
  IL_7de4:  call       void [System.Console]System.Console::Write(int32)
  IL_7de9:  dup
  IL_7dea:  ldc.i4.5
  IL_7deb:  sub
  IL_7dec:  call       void [System.Console]System.Console::Write(int32)
  IL_7df1:  dup
  IL_7df2:  ldc.i4.7
  IL_7df3:  shr
  IL_7df4:  call       void [System.Console]System.Console::Write(int32)
  IL_7df9:  dup
  IL_7dfa:  ldc.i4.7
  IL_7dfb:  sub
  IL_7dfc:  call       void [System.Console]System.Console::Write(int32)
  IL_7e01:  dup
  IL_7e02:  ldc.i4.4
  IL_7e03:  shl
  IL_7e04:  call       void [System.Console]System.Console::Write(int32)
  IL_7e09:  dup
  IL_7e0a:  ldc.i4.3
  IL_7e0b:  and
  IL_7e0c:  call       void [System.Console]System.Console::Write(int32)
  IL_7e11:  dup
  IL_7e12:  ldc.i4.8
  IL_7e13:  or
  IL_7e14:  call       void [System.Console]System.Console::Write(int32)
  IL_7e19:  dup
  IL_7e1a:  ldc.i4.7
  IL_7e1b:  and
  IL_7e1c:  call       void [System.Console]System.Console::Write(int32)
  IL_7e21:  dup
  IL_7e22:  ldc.i4.4
  IL_7e23:  shr
  IL_7e24:  call       void [System.Console]System.Console::Write(int32)
  IL_7e29:  dup
  IL_7e2a:  ldc.i4.4
  IL_7e2b:  xor
  IL_7e2c:  call       void [System.Console]System.Console::Write(int32)
  IL_7e31:  dup
  IL_7e32:  ldc.i4.7
  IL_7e33:  shl
  IL_7e34:  call       void [System.Console]System.Console::Write(int32)
  IL_7e39:  dup
  IL_7e3a:  ldc.i4.3
  IL_7e3b:  shr
  IL_7e3c:  call       void [System.Console]System.Console::Write(int32)
  IL_7e41:  dup
  IL_7e42:  ldc.i4.1
  IL_7e43:  shl
  IL_7e44:  call       void [System.Console]System.Console::Write(int32)
  IL_7e49:  dup
  IL_7e4a:  ldc.i4.7
  IL_7e4b:  and
  IL_7e4c:  call       void [System.Console]System.Console::Write(int32)
  IL_7e51:  dup
  IL_7e52:  ldc.i4.2
  IL_7e53:  xor
  IL_7e54:  call       void [System.Console]System.Console::Write(int32)
  IL_7e59:  dup
  IL_7e5a:  ldc.i4.4
  IL_7e5b:  and
  IL_7e5c:  call       void [System.Console]System.Console::Write(int32)
  IL_7e61:  dup
  IL_7e62:  ldc.i4.5
  IL_7e63:  or
  IL_7e64:  call       void [System.Console]System.Console::Write(int32)
  IL_7e69:  dup
  IL_7e6a:  ldc.i4.5
  IL_7e6b:  sub
  IL_7e6c:  call       void [System.Console]System.Console::Write(int32)
  IL_7e71:  dup
  IL_7e72:  ldc.i4.7
  IL_7e73:  or
  IL_7e74:  call       void [System.Console]System.Console::Write(int32)
  IL_7e79:  dup
  IL_7e7a:  ldc.i4.5
  IL_7e7b:  shr
  IL_7e7c:  call       void [System.Console]System.Console::Write(int32)
  IL_7e81:  dup
  IL_7e82:  ldc.i4.8
  IL_7e83:  mul
  IL_7e84:  call       void [System.Console]System.Console::Write(int32)
  IL_7e89:  dup
  IL_7e8a:  ldc.i4.3
  IL_7e8b:  xor
  IL_7e8c:  call       void [System.Console]System.Console::Write(int32)
  IL_7e91:  dup
  IL_7e92:  ldc.i4.1
  IL_7e93:  shr
  IL_7e94:  call       void [System.Console]System.Console::Write(int32)
  IL_7e99:  dup
  IL_7e9a:  ldc.i4.4
  IL_7e9b:  shr
  IL_7e9c:  call       void [System.Console]System.Console::Write(int32)
  IL_7ea1:  dup
  IL_7ea2:  ldc.i4.8
  IL_7ea3:  mul
  IL_7ea4:  call       void [System.Console]System.Console::Write(int32)
  IL_7ea9:  dup
  IL_7eaa:  ldc.i4.7
  IL_7eab:  or
  IL_7eac:  call       void [System.Console]System.Console::Write(int32)
  IL_7eb1:  dup
  IL_7eb2:  ldc.i4.7
  IL_7eb3:  add
  IL_7eb4:  call       void [System.Console]System.Console::Write(int32)
  IL_7eb9:  dup
  IL_7eba:  ldc.i4.3
  IL_7ebb:  and
  IL_7ebc:  call       void [System.Console]System.Console::Write(int32)
  IL_7ec1:  dup
  IL_7ec2:  ldc.i4.6
  IL_7ec3:  or
  IL_7ec4:  call       void [System.Console]System.Console::Write(int32)
  IL_7ec9:  dup
  IL_7eca:  ldc.i4.8
  IL_7ecb:  mul
  IL_7ecc:  call       void [System.Console]System.Console::Write(int32)
  IL_7ed1:  dup
  IL_7ed2:  ldc.i4.8
  IL_7ed3:  or
  IL_7ed4:  call       void [System.Console]System.Console::Write(int32)
  IL_7ed9:  dup
  IL_7eda:  ldc.i4.8
  IL_7edb:  add
  IL_7edc:  call       void [System.Console]System.Console::Write(int32)
  IL_7ee1:  dup
  IL_7ee2:  ldc.i4.1
  IL_7ee3:  xor
  IL_7ee4:  call       void [System.Console]System.Console::Write(int32)
  IL_7ee9:  dup
  IL_7eea:  ldc.i4.1
  IL_7eeb:  or
  IL_7eec:  call       void [System.Console]System.Console::Write(int32)
  IL_7ef1:  dup
  IL_7ef2:  ldc.i4.2
  IL_7ef3:  shl
  IL_7ef4:  call       void [System.Console]System.Console::Write(int32)
  IL_7ef9:  dup
  IL_7efa:  ldc.i4.8
  IL_7efb:  shl
  IL_7efc:  call       void [System.Console]System.Console::Write(int32)
  IL_7f01:  dup
  IL_7f02:  ldc.i4.1
  IL_7f03:  and
  IL_7f04:  call       void [System.Console]System.Console::Write(int32)
  IL_7f09:  dup
  IL_7f0a:  ldc.i4.4
  IL_7f0b:  shl
  IL_7f0c:  call       void [System.Console]System.Console::Write(int32)
  IL_7f11:  dup
  IL_7f12:  ldc.i4.1
  IL_7f13:  or
  IL_7f14:  call       void [System.Console]System.Console::Write(int32)
  IL_7f19:  dup
  IL_7f1a:  ldc.i4.4
  IL_7f1b:  xor
  IL_7f1c:  call       void [System.Console]System.Console::Write(int32)
  IL_7f21:  dup
  IL_7f22:  ldc.i4.0
  IL_7f23:  sub
  IL_7f24:  call       void [System.Console]System.Console::Write(int32)
  IL_7f29:  dup
  IL_7f2a:  ldc.i4.7
  IL_7f2b:  or
  IL_7f2c:  call       void [System.Console]System.Console::Write(int32)
  IL_7f31:  dup
  IL_7f32:  ldc.i4.5
  IL_7f33:  shl
  IL_7f34:  call       void [System.Console]System.Console::Write(int32)
  IL_7f39:  dup
  IL_7f3a:  ldc.i4.0
  IL_7f3b:  shl
  IL_7f3c:  call       void [System.Console]System.Console::Write(int32)
  IL_7f41:  dup
  IL_7f42:  ldc.i4.5
  IL_7f43:  and
  IL_7f44:  call       void [System.Console]System.Console::Write(int32)
  IL_7f49:  dup
  IL_7f4a:  ldc.i4.4
  IL_7f4b:  or
  IL_7f4c:  call       void [System.Console]System.Console::Write(int32)
  IL_7f51:  dup
  IL_7f52:  ldc.i4.8
  IL_7f53:  mul
  IL_7f54:  call       void [System.Console]System.Console::Write(int32)
  IL_7f59:  dup
  IL_7f5a:  ldc.i4.7
  IL_7f5b:  add
  IL_7f5c:  call       void [System.Console]System.Console::Write(int32)
  IL_7f61:  dup
  IL_7f62:  ldc.i4.2
  IL_7f63:  xor
  IL_7f64:  call       void [System.Console]System.Console::Write(int32)
  IL_7f69:  dup
  IL_7f6a:  ldc.i4.3
  IL_7f6b:  shr
  IL_7f6c:  call       void [System.Console]System.Console::Write(int32)
  IL_7f71:  dup
  IL_7f72:  ldc.i4.5
  IL_7f73:  xor
  IL_7f74:  call       void [System.Console]System.Console::Write(int32)
  IL_7f79:  dup
  IL_7f7a:  ldc.i4.2
  IL_7f7b:  xor
  IL_7f7c:  call       void [System.Console]System.Console::Write(int32)
  IL_7f81:  dup
  IL_7f82:  ldc.i4.3
  IL_7f83:  mul
  IL_7f84:  call       void [System.Console]System.Console::Write(int32)
  IL_7f89:  dup
  IL_7f8a:  ldc.i4.7
  IL_7f8b:  add
  IL_7f8c:  call       void [System.Console]System.Console::Write(int32)
  IL_7f91:  dup
  IL_7f92:  ldc.i4.5
  IL_7f93:  and
  IL_7f94:  call       void [System.Console]System.Console::Write(int32)
  IL_7f99:  dup
  IL_7f9a:  ldc.i4.5
  IL_7f9b:  and
  IL_7f9c:  call       void [System.Console]System.Console::Write(int32)
  IL_7fa1:  dup
  IL_7fa2:  ldc.i4.4
  IL_7fa3:  shl
  IL_7fa4:  call       void [System.Console]System.Console::Write(int32)
  IL_7fa9:  dup
  IL_7faa:  ldc.i4.2
  IL_7fab:  and
  IL_7fac:  call       void [System.Console]System.Console::Write(int32)
  IL_7fb1:  dup
  IL_7fb2:  ldc.i4.0
  IL_7fb3:  or
  IL_7fb4:  call       void [System.Console]System.Console::Write(int32)
  IL_7fb9:  dup
  IL_7fba:  ldc.i4.3
  IL_7fbb:  shl
  IL_7fbc:  call       void [System.Console]System.Console::Write(int32)
  IL_7fc1:  dup
  IL_7fc2:  ldc.i4.3
  IL_7fc3:  shl
  IL_7fc4:  call       void [System.Console]System.Console::Write(int32)
  IL_7fc9:  dup
  IL_7fca:  ldc.i4.3
  IL_7fcb:  xor
  IL_7fcc:  call       void [System.Console]System.Console::Write(int32)
  IL_7fd1:  dup
  IL_7fd2:  ldc.i4.0
  IL_7fd3:  add
  IL_7fd4:  call       void [System.Console]System.Console::Write(int32)
  IL_7fd9:  dup
  IL_7fda:  ldc.i4.4
  IL_7fdb:  and
  IL_7fdc:  call       void [System.Console]System.Console::Write(int32)
  IL_7fe1:  dup
  IL_7fe2:  ldc.i4.8
  IL_7fe3:  xor
  IL_7fe4:  call       void [System.Console]System.Console::Write(int32)
  IL_7fe9:  dup
  IL_7fea:  ldc.i4.1
  IL_7feb:  and
  IL_7fec:  call       void [System.Console]System.Console::Write(int32)
  IL_7ff1:  dup
  IL_7ff2:  ldc.i4.5
  IL_7ff3:  shr
  IL_7ff4:  call       void [System.Console]System.Console::Write(int32)
  IL_7ff9:  dup
  IL_7ffa:  ldc.i4.8
  IL_7ffb:  shl
  IL_7ffc:  call       void [System.Console]System.Console::Write(int32)
  IL_8001:  dup
  IL_8002:  ldc.i4.7
  IL_8003:  xor
  IL_8004:  call       void [System.Console]System.Console::Write(int32)
  IL_8009:  dup
  IL_800a:  ldc.i4.5
  IL_800b:  sub
  IL_800c:  call       void [System.Console]System.Console::Write(int32)
  IL_8011:  dup
  IL_8012:  ldc.i4.6
  IL_8013:  or
  IL_8014:  call       void [System.Console]System.Console::Write(int32)
  IL_8019:  dup
  IL_801a:  ldc.i4.8
  IL_801b:  shr
  IL_801c:  call       void [System.Console]System.Console::Write(int32)
  IL_8021:  dup
  IL_8022:  ldc.i4.2
  IL_8023:  shr
  IL_8024:  call       void [System.Console]System.Console::Write(int32)
  IL_8029:  dup
  IL_802a:  ldc.i4.3
  IL_802b:  xor
  IL_802c:  call       void [System.Console]System.Console::Write(int32)
  IL_8031:  dup
  IL_8032:  ldc.i4.0
  IL_8033:  add
  IL_8034:  call       void [System.Console]System.Console::Write(int32)
  IL_8039:  dup
  IL_803a:  ldc.i4.8
  IL_803b:  mul
  IL_803c:  call       void [System.Console]System.Console::Write(int32)
  IL_8041:  dup
  IL_8042:  ldc.i4.8
  IL_8043:  shr
  IL_8044:  call       void [System.Console]System.Console::Write(int32)
  IL_8049:  dup
  IL_804a:  ldc.i4.2
  IL_804b:  or
  IL_804c:  call       void [System.Console]System.Console::Write(int32)
  IL_8051:  dup
  IL_8052:  ldc.i4.0
  IL_8053:  and
  IL_8054:  call       void [System.Console]System.Console::Write(int32)
  IL_8059:  dup
  IL_805a:  ldc.i4.2
  IL_805b:  mul
  IL_805c:  call       void [System.Console]System.Console::Write(int32)
  IL_8061:  dup
  IL_8062:  ldc.i4.7
  IL_8063:  or
  IL_8064:  call       void [System.Console]System.Console::Write(int32)
  IL_8069:  dup
  IL_806a:  ldc.i4.1
  IL_806b:  sub
  IL_806c:  call       void [System.Console]System.Console::Write(int32)
  IL_8071:  dup
  IL_8072:  ldc.i4.5
  IL_8073:  and
  IL_8074:  call       void [System.Console]System.Console::Write(int32)
  IL_8079:  dup
  IL_807a:  ldc.i4.1
  IL_807b:  and
  IL_807c:  call       void [System.Console]System.Console::Write(int32)
  IL_8081:  dup
  IL_8082:  ldc.i4.5
  IL_8083:  shr
  IL_8084:  call       void [System.Console]System.Console::Write(int32)
  IL_8089:  dup
  IL_808a:  ldc.i4.8
  IL_808b:  shl
  IL_808c:  call       void [System.Console]System.Console::Write(int32)
  IL_8091:  dup
  IL_8092:  ldc.i4.2
  IL_8093:  sub
  IL_8094:  call       void [System.Console]System.Console::Write(int32)
  IL_8099:  dup
  IL_809a:  ldc.i4.8
  IL_809b:  xor
  IL_809c:  call       void [System.Console]System.Console::Write(int32)
  IL_80a1:  dup
  IL_80a2:  ldc.i4.5
  IL_80a3:  shr
  IL_80a4:  call       void [System.Console]System.Console::Write(int32)
  IL_80a9:  dup
  IL_80aa:  ldc.i4.2
  IL_80ab:  mul
  IL_80ac:  call       void [System.Console]System.Console::Write(int32)
  IL_80b1:  dup
  IL_80b2:  ldc.i4.5
  IL_80b3:  add
  IL_80b4:  call       void [System.Console]System.Console::Write(int32)
  IL_80b9:  dup
  IL_80ba:  ldc.i4.7
  IL_80bb:  or
  IL_80bc:  call       void [System.Console]System.Console::Write(int32)
  IL_80c1:  dup
  IL_80c2:  ldc.i4.4
  IL_80c3:  and
  IL_80c4:  call       void [System.Console]System.Console::Write(int32)
  IL_80c9:  dup
  IL_80ca:  ldc.i4.7
  IL_80cb:  or
  IL_80cc:  call       void [System.Console]System.Console::Write(int32)
  IL_80d1:  dup
  IL_80d2:  ldc.i4.4
  IL_80d3:  or
  IL_80d4:  call       void [System.Console]System.Console::Write(int32)
  IL_80d9:  dup
  IL_80da:  ldc.i4.4
  IL_80db:  or
  IL_80dc:  call       void [System.Console]System.Console::Write(int32)
  IL_80e1:  dup
  IL_80e2:  ldc.i4.1
  IL_80e3:  mul
  IL_80e4:  call       void [System.Console]System.Console::Write(int32)
  IL_80e9:  dup
  IL_80ea:  ldc.i4.3
  IL_80eb:  shr
  IL_80ec:  call       void [System.Console]System.Console::Write(int32)
  IL_80f1:  dup
  IL_80f2:  ldc.i4.7
  IL_80f3:  and
  IL_80f4:  call       void [System.Console]System.Console::Write(int32)
  IL_80f9:  dup
  IL_80fa:  ldc.i4.1
  IL_80fb:  xor
  IL_80fc:  call       void [System.Console]System.Console::Write(int32)
  IL_8101:  dup
  IL_8102:  ldc.i4.5
  IL_8103:  shr
  IL_8104:  call       void [System.Console]System.Console::Write(int32)
  IL_8109:  dup
  IL_810a:  ldc.i4.8
  IL_810b:  mul
  IL_810c:  call       void [System.Console]System.Console::Write(int32)
  IL_8111:  dup
  IL_8112:  ldc.i4.0
  IL_8113:  mul
  IL_8114:  call       void [System.Console]System.Console::Write(int32)
  IL_8119:  dup
  IL_811a:  ldc.i4.0
  IL_811b:  and
  IL_811c:  call       void [System.Console]System.Console::Write(int32)
  IL_8121:  dup
  IL_8122:  ldc.i4.7
  IL_8123:  and
  IL_8124:  call       void [System.Console]System.Console::Write(int32)
  IL_8129:  dup
  IL_812a:  ldc.i4.2
  IL_812b:  xor
  IL_812c:  call       void [System.Console]System.Console::Write(int32)
  IL_8131:  dup
  IL_8132:  ldc.i4.5
  IL_8133:  or
  IL_8134:  call       void [System.Console]System.Console::Write(int32)
  IL_8139:  dup
  IL_813a:  ldc.i4.6
  IL_813b:  and
  IL_813c:  call       void [System.Console]System.Console::Write(int32)
  IL_8141:  dup
  IL_8142:  ldc.i4.1
  IL_8143:  mul
  IL_8144:  call       void [System.Console]System.Console::Write(int32)
  IL_8149:  dup
  IL_814a:  ldc.i4.5
  IL_814b:  and
  IL_814c:  call       void [System.Console]System.Console::Write(int32)
  IL_8151:  dup
  IL_8152:  ldc.i4.8
  IL_8153:  sub
  IL_8154:  call       void [System.Console]System.Console::Write(int32)
  IL_8159:  dup
  IL_815a:  ldc.i4.2
  IL_815b:  shl
  IL_815c:  call       void [System.Console]System.Console::Write(int32)
  IL_8161:  dup
  IL_8162:  ldc.i4.2
  IL_8163:  or
  IL_8164:  call       void [System.Console]System.Console::Write(int32)
  IL_8169:  dup
  IL_816a:  ldc.i4.3
  IL_816b:  or
  IL_816c:  call       void [System.Console]System.Console::Write(int32)
  IL_8171:  dup
  IL_8172:  ldc.i4.6
  IL_8173:  sub
  IL_8174:  call       void [System.Console]System.Console::Write(int32)
  IL_8179:  dup
  IL_817a:  ldc.i4.6
  IL_817b:  and
  IL_817c:  call       void [System.Console]System.Console::Write(int32)
  IL_8181:  dup
  IL_8182:  ldc.i4.5
  IL_8183:  and
  IL_8184:  call       void [System.Console]System.Console::Write(int32)
  IL_8189:  dup
  IL_818a:  ldc.i4.6
  IL_818b:  or
  IL_818c:  call       void [System.Console]System.Console::Write(int32)
  IL_8191:  dup
  IL_8192:  ldc.i4.1
  IL_8193:  shl
  IL_8194:  call       void [System.Console]System.Console::Write(int32)
  IL_8199:  dup
  IL_819a:  ldc.i4.7
  IL_819b:  sub
  IL_819c:  call       void [System.Console]System.Console::Write(int32)
  IL_81a1:  dup
  IL_81a2:  ldc.i4.7
  IL_81a3:  add
  IL_81a4:  call       void [System.Console]System.Console::Write(int32)
  IL_81a9:  dup
  IL_81aa:  ldc.i4.1
  IL_81ab:  shr
  IL_81ac:  call       void [System.Console]System.Console::Write(int32)
  IL_81b1:  dup
  IL_81b2:  ldc.i4.5
  IL_81b3:  add
  IL_81b4:  call       void [System.Console]System.Console::Write(int32)
  IL_81b9:  dup
  IL_81ba:  ldc.i4.2
  IL_81bb:  sub
  IL_81bc:  call       void [System.Console]System.Console::Write(int32)
  IL_81c1:  dup
  IL_81c2:  ldc.i4.4
  IL_81c3:  shr
  IL_81c4:  call       void [System.Console]System.Console::Write(int32)
  IL_81c9:  dup
  IL_81ca:  ldc.i4.5
  IL_81cb:  shl
  IL_81cc:  call       void [System.Console]System.Console::Write(int32)
  IL_81d1:  dup
  IL_81d2:  ldc.i4.1
  IL_81d3:  mul
  IL_81d4:  call       void [System.Console]System.Console::Write(int32)
  IL_81d9:  dup
  IL_81da:  ldc.i4.4
  IL_81db:  mul
  IL_81dc:  call       void [System.Console]System.Console::Write(int32)
  IL_81e1:  dup
  IL_81e2:  ldc.i4.3
  IL_81e3:  mul
  IL_81e4:  call       void [System.Console]System.Console::Write(int32)
  IL_81e9:  dup
  IL_81ea:  ldc.i4.0
  IL_81eb:  add
  IL_81ec:  call       void [System.Console]System.Console::Write(int32)
  IL_81f1:  dup
  IL_81f2:  ldc.i4.8
  IL_81f3:  add
  IL_81f4:  call       void [System.Console]System.Console::Write(int32)
  IL_81f9:  dup
  IL_81fa:  ldc.i4.0
  IL_81fb:  shl
  IL_81fc:  call       void [System.Console]System.Console::Write(int32)
  IL_8201:  dup
  IL_8202:  ldc.i4.0
  IL_8203:  xor
  IL_8204:  call       void [System.Console]System.Console::Write(int32)
  IL_8209:  dup
  IL_820a:  ldc.i4.7
  IL_820b:  and
  IL_820c:  call       void [System.Console]System.Console::Write(int32)
  IL_8211:  dup
  IL_8212:  ldc.i4.1
  IL_8213:  and
  IL_8214:  call       void [System.Console]System.Console::Write(int32)
  IL_8219:  dup
  IL_821a:  ldc.i4.5
  IL_821b:  shl
  IL_821c:  call       void [System.Console]System.Console::Write(int32)
  IL_8221:  dup
  IL_8222:  ldc.i4.4
  IL_8223:  xor
  IL_8224:  call       void [System.Console]System.Console::Write(int32)
  IL_8229:  dup
  IL_822a:  ldc.i4.8
  IL_822b:  or
  IL_822c:  call       void [System.Console]System.Console::Write(int32)
  IL_8231:  dup
  IL_8232:  ldc.i4.1
  IL_8233:  shr
  IL_8234:  call       void [System.Console]System.Console::Write(int32)
  IL_8239:  dup
  IL_823a:  ldc.i4.8
  IL_823b:  sub
  IL_823c:  call       void [System.Console]System.Console::Write(int32)
  IL_8241:  dup
  IL_8242:  ldc.i4.6
  IL_8243:  sub
  IL_8244:  call       void [System.Console]System.Console::Write(int32)
  IL_8249:  dup
  IL_824a:  ldc.i4.4
  IL_824b:  add
  IL_824c:  call       void [System.Console]System.Console::Write(int32)
  IL_8251:  dup
  IL_8252:  ldc.i4.5
  IL_8253:  sub
  IL_8254:  call       void [System.Console]System.Console::Write(int32)
  IL_8259:  dup
  IL_825a:  ldc.i4.5
  IL_825b:  sub
  IL_825c:  call       void [System.Console]System.Console::Write(int32)
  IL_8261:  dup
  IL_8262:  ldc.i4.3
  IL_8263:  mul
  IL_8264:  call       void [System.Console]System.Console::Write(int32)
  IL_8269:  dup
  IL_826a:  ldc.i4.4
  IL_826b:  and
  IL_826c:  call       void [System.Console]System.Console::Write(int32)
  IL_8271:  dup
  IL_8272:  ldc.i4.4
  IL_8273:  shr
  IL_8274:  call       void [System.Console]System.Console::Write(int32)
  IL_8279:  dup
  IL_827a:  ldc.i4.2
  IL_827b:  shr
  IL_827c:  call       void [System.Console]System.Console::Write(int32)
  IL_8281:  dup
  IL_8282:  ldc.i4.0
  IL_8283:  shl
  IL_8284:  call       void [System.Console]System.Console::Write(int32)
  IL_8289:  dup
  IL_828a:  ldc.i4.4
  IL_828b:  sub
  IL_828c:  call       void [System.Console]System.Console::Write(int32)
  IL_8291:  dup
  IL_8292:  ldc.i4.1
  IL_8293:  shl
  IL_8294:  call       void [System.Console]System.Console::Write(int32)
  IL_8299:  dup
  IL_829a:  ldc.i4.0
  IL_829b:  shr
  IL_829c:  call       void [System.Console]System.Console::Write(int32)
  IL_82a1:  dup
  IL_82a2:  ldc.i4.0
  IL_82a3:  add
  IL_82a4:  call       void [System.Console]System.Console::Write(int32)
  IL_82a9:  dup
  IL_82aa:  ldc.i4.0
  IL_82ab:  and
  IL_82ac:  call       void [System.Console]System.Console::Write(int32)
  IL_82b1:  dup
  IL_82b2:  ldc.i4.3
  IL_82b3:  shl
  IL_82b4:  call       void [System.Console]System.Console::Write(int32)
  IL_82b9:  dup
  IL_82ba:  ldc.i4.2
  IL_82bb:  shr
  IL_82bc:  call       void [System.Console]System.Console::Write(int32)
  IL_82c1:  dup
  IL_82c2:  ldc.i4.3
  IL_82c3:  and
  IL_82c4:  call       void [System.Console]System.Console::Write(int32)
  IL_82c9:  dup
  IL_82ca:  ldc.i4.3
  IL_82cb:  shr
  IL_82cc:  call       void [System.Console]System.Console::Write(int32)
  IL_82d1:  dup
  IL_82d2:  ldc.i4.5
  IL_82d3:  or
  IL_82d4:  call       void [System.Console]System.Console::Write(int32)
  IL_82d9:  dup
  IL_82da:  ldc.i4.8
  IL_82db:  or
  IL_82dc:  call       void [System.Console]System.Console::Write(int32)
  IL_82e1:  dup
  IL_82e2:  ldc.i4.7
  IL_82e3:  shl
  IL_82e4:  call       void [System.Console]System.Console::Write(int32)
  IL_82e9:  dup
  IL_82ea:  ldc.i4.4
  IL_82eb:  and
  IL_82ec:  call       void [System.Console]System.Console::Write(int32)
  IL_82f1:  dup
  IL_82f2:  ldc.i4.2
  IL_82f3:  and
  IL_82f4:  call       void [System.Console]System.Console::Write(int32)
  IL_82f9:  dup
  IL_82fa:  ldc.i4.0
  IL_82fb:  and
  IL_82fc:  call       void [System.Console]System.Console::Write(int32)
  IL_8301:  dup
  IL_8302:  ldc.i4.1
  IL_8303:  mul
  IL_8304:  call       void [System.Console]System.Console::Write(int32)
  IL_8309:  dup
  IL_830a:  ldc.i4.7
  IL_830b:  shl
  IL_830c:  call       void [System.Console]System.Console::Write(int32)
  IL_8311:  dup
  IL_8312:  ldc.i4.0
  IL_8313:  and
  IL_8314:  call       void [System.Console]System.Console::Write(int32)
  IL_8319:  dup
  IL_831a:  ldc.i4.0
  IL_831b:  sub
  IL_831c:  call       void [System.Console]System.Console::Write(int32)
  IL_8321:  dup
  IL_8322:  ldc.i4.1
  IL_8323:  and
  IL_8324:  call       void [System.Console]System.Console::Write(int32)
  IL_8329:  dup
  IL_832a:  ldc.i4.0
  IL_832b:  shl
  IL_832c:  call       void [System.Console]System.Console::Write(int32)
  IL_8331:  dup
  IL_8332:  ldc.i4.0
  IL_8333:  and
  IL_8334:  call       void [System.Console]System.Console::Write(int32)
  IL_8339:  dup
  IL_833a:  ldc.i4.7
  IL_833b:  and
  IL_833c:  call       void [System.Console]System.Console::Write(int32)
  IL_8341:  dup
  IL_8342:  ldc.i4.1
  IL_8343:  and
  IL_8344:  call       void [System.Console]System.Console::Write(int32)
  IL_8349:  dup
  IL_834a:  ldc.i4.4
  IL_834b:  shr
  IL_834c:  call       void [System.Console]System.Console::Write(int32)
  IL_8351:  dup
  IL_8352:  ldc.i4.2
  IL_8353:  shl
  IL_8354:  call       void [System.Console]System.Console::Write(int32)
  IL_8359:  dup
  IL_835a:  ldc.i4.1
  IL_835b:  add
  IL_835c:  call       void [System.Console]System.Console::Write(int32)
  IL_8361:  dup
  IL_8362:  ldc.i4.5
  IL_8363:  shl
  IL_8364:  call       void [System.Console]System.Console::Write(int32)
  IL_8369:  dup
  IL_836a:  ldc.i4.2
  IL_836b:  shl
  IL_836c:  call       void [System.Console]System.Console::Write(int32)
  IL_8371:  dup
  IL_8372:  ldc.i4.0
  IL_8373:  sub
  IL_8374:  call       void [System.Console]System.Console::Write(int32)
  IL_8379:  dup
  IL_837a:  ldc.i4.3
  IL_837b:  sub
  IL_837c:  call       void [System.Console]System.Console::Write(int32)
  IL_8381:  dup
  IL_8382:  ldc.i4.8
  IL_8383:  mul
  IL_8384:  call       void [System.Console]System.Console::Write(int32)
  IL_8389:  dup
  IL_838a:  ldc.i4.5
  IL_838b:  add
  IL_838c:  call       void [System.Console]System.Console::Write(int32)
  IL_8391:  dup
  IL_8392:  ldc.i4.5
  IL_8393:  shr
  IL_8394:  call       void [System.Console]System.Console::Write(int32)
  IL_8399:  dup
  IL_839a:  ldc.i4.5
  IL_839b:  shl
  IL_839c:  call       void [System.Console]System.Console::Write(int32)
  IL_83a1:  dup
  IL_83a2:  ldc.i4.7
  IL_83a3:  mul
  IL_83a4:  call       void [System.Console]System.Console::Write(int32)
  IL_83a9:  dup
  IL_83aa:  ldc.i4.5
  IL_83ab:  and
  IL_83ac:  call       void [System.Console]System.Console::Write(int32)
  IL_83b1:  dup
  IL_83b2:  ldc.i4.7
  IL_83b3:  xor
  IL_83b4:  call       void [System.Console]System.Console::Write(int32)
  IL_83b9:  dup
  IL_83ba:  ldc.i4.7
  IL_83bb:  add
  IL_83bc:  call       void [System.Console]System.Console::Write(int32)
  IL_83c1:  dup
  IL_83c2:  ldc.i4.6
  IL_83c3:  shr
  IL_83c4:  call       void [System.Console]System.Console::Write(int32)
  IL_83c9:  dup
  IL_83ca:  ldc.i4.6
  IL_83cb:  xor
  IL_83cc:  call       void [System.Console]System.Console::Write(int32)
  IL_83d1:  dup
  IL_83d2:  ldc.i4.3
  IL_83d3:  xor
  IL_83d4:  call       void [System.Console]System.Console::Write(int32)
  IL_83d9:  dup
  IL_83da:  ldc.i4.4
  IL_83db:  shr
  IL_83dc:  call       void [System.Console]System.Console::Write(int32)
  IL_83e1:  dup
  IL_83e2:  ldc.i4.1
  IL_83e3:  shr
  IL_83e4:  call       void [System.Console]System.Console::Write(int32)
  IL_83e9:  dup
  IL_83ea:  ldc.i4.0
  IL_83eb:  shl
  IL_83ec:  call       void [System.Console]System.Console::Write(int32)
  IL_83f1:  dup
  IL_83f2:  ldc.i4.4
  IL_83f3:  xor
  IL_83f4:  call       void [System.Console]System.Console::Write(int32)
  IL_83f9:  dup
  IL_83fa:  ldc.i4.0
  IL_83fb:  or
  IL_83fc:  call       void [System.Console]System.Console::Write(int32)
  IL_8401:  dup
  IL_8402:  ldc.i4.3
  IL_8403:  and
  IL_8404:  call       void [System.Console]System.Console::Write(int32)
  IL_8409:  dup
  IL_840a:  ldc.i4.6
  IL_840b:  and
  IL_840c:  call       void [System.Console]System.Console::Write(int32)
  IL_8411:  dup
  IL_8412:  ldc.i4.2
  IL_8413:  and
  IL_8414:  call       void [System.Console]System.Console::Write(int32)
  IL_8419:  dup
  IL_841a:  ldc.i4.1
  IL_841b:  xor
  IL_841c:  call       void [System.Console]System.Console::Write(int32)
  IL_8421:  dup
  IL_8422:  ldc.i4.2
  IL_8423:  or
  IL_8424:  call       void [System.Console]System.Console::Write(int32)
  IL_8429:  dup
  IL_842a:  ldc.i4.7
  IL_842b:  or
  IL_842c:  call       void [System.Console]System.Console::Write(int32)
  IL_8431:  dup
  IL_8432:  ldc.i4.8
  IL_8433:  mul
  IL_8434:  call       void [System.Console]System.Console::Write(int32)
  IL_8439:  dup
  IL_843a:  ldc.i4.2
  IL_843b:  sub
  IL_843c:  call       void [System.Console]System.Console::Write(int32)
  IL_8441:  dup
  IL_8442:  ldc.i4.6
  IL_8443:  mul
  IL_8444:  call       void [System.Console]System.Console::Write(int32)
  IL_8449:  dup
  IL_844a:  ldc.i4.7
  IL_844b:  xor
  IL_844c:  call       void [System.Console]System.Console::Write(int32)
  IL_8451:  dup
  IL_8452:  ldc.i4.4
  IL_8453:  and
  IL_8454:  call       void [System.Console]System.Console::Write(int32)
  IL_8459:  dup
  IL_845a:  ldc.i4.7
  IL_845b:  xor
  IL_845c:  call       void [System.Console]System.Console::Write(int32)
  IL_8461:  dup
  IL_8462:  ldc.i4.6
  IL_8463:  or
  IL_8464:  call       void [System.Console]System.Console::Write(int32)
  IL_8469:  dup
  IL_846a:  ldc.i4.8
  IL_846b:  or
  IL_846c:  call       void [System.Console]System.Console::Write(int32)
  IL_8471:  dup
  IL_8472:  ldc.i4.6
  IL_8473:  add
  IL_8474:  call       void [System.Console]System.Console::Write(int32)
  IL_8479:  dup
  IL_847a:  ldc.i4.0
  IL_847b:  xor
  IL_847c:  call       void [System.Console]System.Console::Write(int32)
  IL_8481:  dup
  IL_8482:  ldc.i4.1
  IL_8483:  shr
  IL_8484:  call       void [System.Console]System.Console::Write(int32)
  IL_8489:  dup
  IL_848a:  ldc.i4.3
  IL_848b:  add
  IL_848c:  call       void [System.Console]System.Console::Write(int32)
  IL_8491:  dup
  IL_8492:  ldc.i4.6
  IL_8493:  add
  IL_8494:  call       void [System.Console]System.Console::Write(int32)
  IL_8499:  dup
  IL_849a:  ldc.i4.6
  IL_849b:  or
  IL_849c:  call       void [System.Console]System.Console::Write(int32)
  IL_84a1:  dup
  IL_84a2:  ldc.i4.4
  IL_84a3:  mul
  IL_84a4:  call       void [System.Console]System.Console::Write(int32)
  IL_84a9:  dup
  IL_84aa:  ldc.i4.1
  IL_84ab:  xor
  IL_84ac:  call       void [System.Console]System.Console::Write(int32)
  IL_84b1:  dup
  IL_84b2:  ldc.i4.5
  IL_84b3:  shl
  IL_84b4:  call       void [System.Console]System.Console::Write(int32)
  IL_84b9:  dup
  IL_84ba:  ldc.i4.6
  IL_84bb:  and
  IL_84bc:  call       void [System.Console]System.Console::Write(int32)
  IL_84c1:  dup
  IL_84c2:  ldc.i4.4
  IL_84c3:  and
  IL_84c4:  call       void [System.Console]System.Console::Write(int32)
  IL_84c9:  dup
  IL_84ca:  ldc.i4.7
  IL_84cb:  sub
  IL_84cc:  call       void [System.Console]System.Console::Write(int32)
  IL_84d1:  dup
  IL_84d2:  ldc.i4.4
  IL_84d3:  shr
  IL_84d4:  call       void [System.Console]System.Console::Write(int32)
  IL_84d9:  dup
  IL_84da:  ldc.i4.4
  IL_84db:  shl
  IL_84dc:  call       void [System.Console]System.Console::Write(int32)
  IL_84e1:  dup
  IL_84e2:  ldc.i4.8
  IL_84e3:  sub
  IL_84e4:  call       void [System.Console]System.Console::Write(int32)
  IL_84e9:  dup
  IL_84ea:  ldc.i4.0
  IL_84eb:  mul
  IL_84ec:  call       void [System.Console]System.Console::Write(int32)
  IL_84f1:  dup
  IL_84f2:  ldc.i4.8
  IL_84f3:  sub
  IL_84f4:  call       void [System.Console]System.Console::Write(int32)
  IL_84f9:  dup
  IL_84fa:  ldc.i4.6
  IL_84fb:  sub
  IL_84fc:  call       void [System.Console]System.Console::Write(int32)
  IL_8501:  dup
  IL_8502:  ldc.i4.7
  IL_8503:  xor
  IL_8504:  call       void [System.Console]System.Console::Write(int32)
  IL_8509:  dup
  IL_850a:  ldc.i4.7
  IL_850b:  mul
  IL_850c:  call       void [System.Console]System.Console::Write(int32)
  IL_8511:  dup
  IL_8512:  ldc.i4.8
  IL_8513:  sub
  IL_8514:  call       void [System.Console]System.Console::Write(int32)
  IL_8519:  dup
  IL_851a:  ldc.i4.4
  IL_851b:  shr
  IL_851c:  call       void [System.Console]System.Console::Write(int32)
  IL_8521:  dup
  IL_8522:  ldc.i4.5
  IL_8523:  or
  IL_8524:  call       void [System.Console]System.Console::Write(int32)
  IL_8529:  dup
  IL_852a:  ldc.i4.8
  IL_852b:  sub
  IL_852c:  call       void [System.Console]System.Console::Write(int32)
  IL_8531:  dup
  IL_8532:  ldc.i4.5
  IL_8533:  xor
  IL_8534:  call       void [System.Console]System.Console::Write(int32)
  IL_8539:  dup
  IL_853a:  ldc.i4.7
  IL_853b:  sub
  IL_853c:  call       void [System.Console]System.Console::Write(int32)
  IL_8541:  dup
  IL_8542:  ldc.i4.2
  IL_8543:  add
  IL_8544:  call       void [System.Console]System.Console::Write(int32)
  IL_8549:  dup
  IL_854a:  ldc.i4.3
  IL_854b:  and
  IL_854c:  call       void [System.Console]System.Console::Write(int32)
  IL_8551:  dup
  IL_8552:  ldc.i4.2
  IL_8553:  or
  IL_8554:  call       void [System.Console]System.Console::Write(int32)
  IL_8559:  dup
  IL_855a:  ldc.i4.8
  IL_855b:  xor
  IL_855c:  call       void [System.Console]System.Console::Write(int32)
  IL_8561:  dup
  IL_8562:  ldc.i4.1
  IL_8563:  and
  IL_8564:  call       void [System.Console]System.Console::Write(int32)
  IL_8569:  dup
  IL_856a:  ldc.i4.0
  IL_856b:  xor
  IL_856c:  call       void [System.Console]System.Console::Write(int32)
  IL_8571:  dup
  IL_8572:  ldc.i4.0
  IL_8573:  xor
  IL_8574:  call       void [System.Console]System.Console::Write(int32)
  IL_8579:  dup
  IL_857a:  ldc.i4.8
  IL_857b:  sub
  IL_857c:  call       void [System.Console]System.Console::Write(int32)
  IL_8581:  dup
  IL_8582:  ldc.i4.4
  IL_8583:  add
  IL_8584:  call       void [System.Console]System.Console::Write(int32)
  IL_8589:  dup
  IL_858a:  ldc.i4.4
  IL_858b:  sub
  IL_858c:  call       void [System.Console]System.Console::Write(int32)
  IL_8591:  dup
  IL_8592:  ldc.i4.6
  IL_8593:  mul
  IL_8594:  call       void [System.Console]System.Console::Write(int32)
  IL_8599:  dup
  IL_859a:  ldc.i4.8
  IL_859b:  mul
  IL_859c:  call       void [System.Console]System.Console::Write(int32)
  IL_85a1:  dup
  IL_85a2:  ldc.i4.3
  IL_85a3:  sub
  IL_85a4:  call       void [System.Console]System.Console::Write(int32)
  IL_85a9:  dup
  IL_85aa:  ldc.i4.5
  IL_85ab:  shl
  IL_85ac:  call       void [System.Console]System.Console::Write(int32)
  IL_85b1:  dup
  IL_85b2:  ldc.i4.6
  IL_85b3:  sub
  IL_85b4:  call       void [System.Console]System.Console::Write(int32)
  IL_85b9:  dup
  IL_85ba:  ldc.i4.8
  IL_85bb:  xor
  IL_85bc:  call       void [System.Console]System.Console::Write(int32)
  IL_85c1:  dup
  IL_85c2:  ldc.i4.3
  IL_85c3:  and
  IL_85c4:  call       void [System.Console]System.Console::Write(int32)
  IL_85c9:  dup
  IL_85ca:  ldc.i4.8
  IL_85cb:  or
  IL_85cc:  call       void [System.Console]System.Console::Write(int32)
  IL_85d1:  dup
  IL_85d2:  ldc.i4.7
  IL_85d3:  or
  IL_85d4:  call       void [System.Console]System.Console::Write(int32)
  IL_85d9:  dup
  IL_85da:  ldc.i4.7
  IL_85db:  or
  IL_85dc:  call       void [System.Console]System.Console::Write(int32)
  IL_85e1:  dup
  IL_85e2:  ldc.i4.4
  IL_85e3:  mul
  IL_85e4:  call       void [System.Console]System.Console::Write(int32)
  IL_85e9:  dup
  IL_85ea:  ldc.i4.0
  IL_85eb:  add
  IL_85ec:  call       void [System.Console]System.Console::Write(int32)
  IL_85f1:  dup
  IL_85f2:  ldc.i4.1
  IL_85f3:  or
  IL_85f4:  call       void [System.Console]System.Console::Write(int32)
  IL_85f9:  dup
  IL_85fa:  ldc.i4.4
  IL_85fb:  sub
  IL_85fc:  call       void [System.Console]System.Console::Write(int32)
  IL_8601:  dup
  IL_8602:  ldc.i4.8
  IL_8603:  shr
  IL_8604:  call       void [System.Console]System.Console::Write(int32)
  IL_8609:  dup
  IL_860a:  ldc.i4.6
  IL_860b:  shl
  IL_860c:  call       void [System.Console]System.Console::Write(int32)
  IL_8611:  dup
  IL_8612:  ldc.i4.8
  IL_8613:  shr
  IL_8614:  call       void [System.Console]System.Console::Write(int32)
  IL_8619:  dup
  IL_861a:  ldc.i4.4
  IL_861b:  or
  IL_861c:  call       void [System.Console]System.Console::Write(int32)
  IL_8621:  dup
  IL_8622:  ldc.i4.0
  IL_8623:  add
  IL_8624:  call       void [System.Console]System.Console::Write(int32)
  IL_8629:  dup
  IL_862a:  ldc.i4.4
  IL_862b:  sub
  IL_862c:  call       void [System.Console]System.Console::Write(int32)
  IL_8631:  dup
  IL_8632:  ldc.i4.3
  IL_8633:  mul
  IL_8634:  call       void [System.Console]System.Console::Write(int32)
  IL_8639:  dup
  IL_863a:  ldc.i4.6
  IL_863b:  or
  IL_863c:  call       void [System.Console]System.Console::Write(int32)
  IL_8641:  dup
  IL_8642:  ldc.i4.8
  IL_8643:  shl
  IL_8644:  call       void [System.Console]System.Console::Write(int32)
  IL_8649:  dup
  IL_864a:  ldc.i4.1
  IL_864b:  mul
  IL_864c:  call       void [System.Console]System.Console::Write(int32)
  IL_8651:  dup
  IL_8652:  ldc.i4.4
  IL_8653:  add
  IL_8654:  call       void [System.Console]System.Console::Write(int32)
  IL_8659:  dup
  IL_865a:  ldc.i4.3
  IL_865b:  shl
  IL_865c:  call       void [System.Console]System.Console::Write(int32)
  IL_8661:  dup
  IL_8662:  ldc.i4.1
  IL_8663:  shr
  IL_8664:  call       void [System.Console]System.Console::Write(int32)
  IL_8669:  dup
  IL_866a:  ldc.i4.5
  IL_866b:  xor
  IL_866c:  call       void [System.Console]System.Console::Write(int32)
  IL_8671:  dup
  IL_8672:  ldc.i4.2
  IL_8673:  or
  IL_8674:  call       void [System.Console]System.Console::Write(int32)
  IL_8679:  dup
  IL_867a:  ldc.i4.1
  IL_867b:  xor
  IL_867c:  call       void [System.Console]System.Console::Write(int32)
  IL_8681:  dup
  IL_8682:  ldc.i4.5
  IL_8683:  xor
  IL_8684:  call       void [System.Console]System.Console::Write(int32)
  IL_8689:  dup
  IL_868a:  ldc.i4.5
  IL_868b:  or
  IL_868c:  call       void [System.Console]System.Console::Write(int32)
  IL_8691:  dup
  IL_8692:  ldc.i4.5
  IL_8693:  mul
  IL_8694:  call       void [System.Console]System.Console::Write(int32)
  IL_8699:  dup
  IL_869a:  ldc.i4.5
  IL_869b:  shr
  IL_869c:  call       void [System.Console]System.Console::Write(int32)
  IL_86a1:  dup
  IL_86a2:  ldc.i4.2
  IL_86a3:  add
  IL_86a4:  call       void [System.Console]System.Console::Write(int32)
  IL_86a9:  dup
  IL_86aa:  ldc.i4.7
  IL_86ab:  shr
  IL_86ac:  call       void [System.Console]System.Console::Write(int32)
  IL_86b1:  dup
  IL_86b2:  ldc.i4.3
  IL_86b3:  shr
  IL_86b4:  call       void [System.Console]System.Console::Write(int32)
  IL_86b9:  dup
  IL_86ba:  ldc.i4.4
  IL_86bb:  sub
  IL_86bc:  call       void [System.Console]System.Console::Write(int32)
  IL_86c1:  dup
  IL_86c2:  ldc.i4.5
  IL_86c3:  sub
  IL_86c4:  call       void [System.Console]System.Console::Write(int32)
  IL_86c9:  dup
  IL_86ca:  ldc.i4.1
  IL_86cb:  shl
  IL_86cc:  call       void [System.Console]System.Console::Write(int32)
  IL_86d1:  dup
  IL_86d2:  ldc.i4.7
  IL_86d3:  mul
  IL_86d4:  call       void [System.Console]System.Console::Write(int32)
  IL_86d9:  dup
  IL_86da:  ldc.i4.6
  IL_86db:  and
  IL_86dc:  call       void [System.Console]System.Console::Write(int32)
  IL_86e1:  dup
  IL_86e2:  ldc.i4.3
  IL_86e3:  add
  IL_86e4:  call       void [System.Console]System.Console::Write(int32)
  IL_86e9:  dup
  IL_86ea:  ldc.i4.4
  IL_86eb:  mul
  IL_86ec:  call       void [System.Console]System.Console::Write(int32)
  IL_86f1:  dup
  IL_86f2:  ldc.i4.0
  IL_86f3:  add
  IL_86f4:  call       void [System.Console]System.Console::Write(int32)
  IL_86f9:  dup
  IL_86fa:  ldc.i4.6
  IL_86fb:  sub
  IL_86fc:  call       void [System.Console]System.Console::Write(int32)
  IL_8701:  dup
  IL_8702:  ldc.i4.8
  IL_8703:  add
  IL_8704:  call       void [System.Console]System.Console::Write(int32)
  IL_8709:  dup
  IL_870a:  ldc.i4.5
  IL_870b:  or
  IL_870c:  call       void [System.Console]System.Console::Write(int32)
  IL_8711:  dup
  IL_8712:  ldc.i4.6
  IL_8713:  and
  IL_8714:  call       void [System.Console]System.Console::Write(int32)
  IL_8719:  dup
  IL_871a:  ldc.i4.3
  IL_871b:  shr
  IL_871c:  call       void [System.Console]System.Console::Write(int32)
  IL_8721:  dup
  IL_8722:  ldc.i4.0
  IL_8723:  shl
  IL_8724:  call       void [System.Console]System.Console::Write(int32)
  IL_8729:  dup
  IL_872a:  ldc.i4.8
  IL_872b:  xor
  IL_872c:  call       void [System.Console]System.Console::Write(int32)
  IL_8731:  dup
  IL_8732:  ldc.i4.2
  IL_8733:  shr
  IL_8734:  call       void [System.Console]System.Console::Write(int32)
  IL_8739:  dup
  IL_873a:  ldc.i4.1
  IL_873b:  xor
  IL_873c:  call       void [System.Console]System.Console::Write(int32)
  IL_8741:  dup
  IL_8742:  ldc.i4.0
  IL_8743:  xor
  IL_8744:  call       void [System.Console]System.Console::Write(int32)
  IL_8749:  dup
  IL_874a:  ldc.i4.7
  IL_874b:  add
  IL_874c:  call       void [System.Console]System.Console::Write(int32)
  IL_8751:  dup
  IL_8752:  ldc.i4.6
  IL_8753:  shr
  IL_8754:  call       void [System.Console]System.Console::Write(int32)
  IL_8759:  dup
  IL_875a:  ldc.i4.4
  IL_875b:  mul
  IL_875c:  call       void [System.Console]System.Console::Write(int32)
  IL_8761:  dup
  IL_8762:  ldc.i4.6
  IL_8763:  shr
  IL_8764:  call       void [System.Console]System.Console::Write(int32)
  IL_8769:  dup
  IL_876a:  ldc.i4.5
  IL_876b:  xor
  IL_876c:  call       void [System.Console]System.Console::Write(int32)
  IL_8771:  dup
  IL_8772:  ldc.i4.7
  IL_8773:  mul
  IL_8774:  call       void [System.Console]System.Console::Write(int32)
  IL_8779:  dup
  IL_877a:  ldc.i4.5
  IL_877b:  sub
  IL_877c:  call       void [System.Console]System.Console::Write(int32)
  IL_8781:  dup
  IL_8782:  ldc.i4.3
  IL_8783:  shr
  IL_8784:  call       void [System.Console]System.Console::Write(int32)
  IL_8789:  dup
  IL_878a:  ldc.i4.6
  IL_878b:  mul
  IL_878c:  call       void [System.Console]System.Console::Write(int32)
  IL_8791:  dup
  IL_8792:  ldc.i4.1
  IL_8793:  shl
  IL_8794:  call       void [System.Console]System.Console::Write(int32)
  IL_8799:  dup
  IL_879a:  ldc.i4.3
  IL_879b:  and
  IL_879c:  call       void [System.Console]System.Console::Write(int32)
  IL_87a1:  dup
  IL_87a2:  ldc.i4.6
  IL_87a3:  shl
  IL_87a4:  call       void [System.Console]System.Console::Write(int32)
  IL_87a9:  dup
  IL_87aa:  ldc.i4.4
  IL_87ab:  mul
  IL_87ac:  call       void [System.Console]System.Console::Write(int32)
  IL_87b1:  dup
  IL_87b2:  ldc.i4.3
  IL_87b3:  mul
  IL_87b4:  call       void [System.Console]System.Console::Write(int32)
  IL_87b9:  dup
  IL_87ba:  ldc.i4.5
  IL_87bb:  shl
  IL_87bc:  call       void [System.Console]System.Console::Write(int32)
  IL_87c1:  dup
  IL_87c2:  ldc.i4.8
  IL_87c3:  add
  IL_87c4:  call       void [System.Console]System.Console::Write(int32)
  IL_87c9:  dup
  IL_87ca:  ldc.i4.1
  IL_87cb:  shl
  IL_87cc:  call       void [System.Console]System.Console::Write(int32)
  IL_87d1:  dup
  IL_87d2:  ldc.i4.2
  IL_87d3:  shl
  IL_87d4:  call       void [System.Console]System.Console::Write(int32)
  IL_87d9:  dup
  IL_87da:  ldc.i4.3
  IL_87db:  mul
  IL_87dc:  call       void [System.Console]System.Console::Write(int32)
  IL_87e1:  dup
  IL_87e2:  ldc.i4.5
  IL_87e3:  sub
  IL_87e4:  call       void [System.Console]System.Console::Write(int32)
  IL_87e9:  dup
  IL_87ea:  ldc.i4.1
  IL_87eb:  add
  IL_87ec:  call       void [System.Console]System.Console::Write(int32)
  IL_87f1:  dup
  IL_87f2:  ldc.i4.1
  IL_87f3:  or
  IL_87f4:  call       void [System.Console]System.Console::Write(int32)
  IL_87f9:  dup
  IL_87fa:  ldc.i4.7
  IL_87fb:  shr
  IL_87fc:  call       void [System.Console]System.Console::Write(int32)
  IL_8801:  dup
  IL_8802:  ldc.i4.8
  IL_8803:  shr
  IL_8804:  call       void [System.Console]System.Console::Write(int32)
  IL_8809:  dup
  IL_880a:  ldc.i4.0
  IL_880b:  shr
  IL_880c:  call       void [System.Console]System.Console::Write(int32)
  IL_8811:  dup
  IL_8812:  ldc.i4.8
  IL_8813:  and
  IL_8814:  call       void [System.Console]System.Console::Write(int32)
  IL_8819:  dup
  IL_881a:  ldc.i4.2
  IL_881b:  xor
  IL_881c:  call       void [System.Console]System.Console::Write(int32)
  IL_8821:  dup
  IL_8822:  ldc.i4.0
  IL_8823:  add
  IL_8824:  call       void [System.Console]System.Console::Write(int32)
  IL_8829:  dup
  IL_882a:  ldc.i4.3
  IL_882b:  sub
  IL_882c:  call       void [System.Console]System.Console::Write(int32)
  IL_8831:  dup
  IL_8832:  ldc.i4.1
  IL_8833:  sub
  IL_8834:  call       void [System.Console]System.Console::Write(int32)
  IL_8839:  dup
  IL_883a:  ldc.i4.8
  IL_883b:  xor
  IL_883c:  call       void [System.Console]System.Console::Write(int32)
  IL_8841:  dup
  IL_8842:  ldc.i4.1
  IL_8843:  add
  IL_8844:  call       void [System.Console]System.Console::Write(int32)
  IL_8849:  dup
  IL_884a:  ldc.i4.5
  IL_884b:  and
  IL_884c:  call       void [System.Console]System.Console::Write(int32)
  IL_8851:  dup
  IL_8852:  ldc.i4.0
  IL_8853:  or
  IL_8854:  call       void [System.Console]System.Console::Write(int32)
  IL_8859:  dup
  IL_885a:  ldc.i4.8
  IL_885b:  and
  IL_885c:  call       void [System.Console]System.Console::Write(int32)
  IL_8861:  dup
  IL_8862:  ldc.i4.1
  IL_8863:  xor
  IL_8864:  call       void [System.Console]System.Console::Write(int32)
  IL_8869:  dup
  IL_886a:  ldc.i4.6
  IL_886b:  or
  IL_886c:  call       void [System.Console]System.Console::Write(int32)
  IL_8871:  dup
  IL_8872:  ldc.i4.3
  IL_8873:  xor
  IL_8874:  call       void [System.Console]System.Console::Write(int32)
  IL_8879:  dup
  IL_887a:  ldc.i4.5
  IL_887b:  and
  IL_887c:  call       void [System.Console]System.Console::Write(int32)
  IL_8881:  dup
  IL_8882:  ldc.i4.6
  IL_8883:  and
  IL_8884:  call       void [System.Console]System.Console::Write(int32)
  IL_8889:  dup
  IL_888a:  ldc.i4.3
  IL_888b:  shr
  IL_888c:  call       void [System.Console]System.Console::Write(int32)
  IL_8891:  dup
  IL_8892:  ldc.i4.2
  IL_8893:  or
  IL_8894:  call       void [System.Console]System.Console::Write(int32)
  IL_8899:  dup
  IL_889a:  ldc.i4.5
  IL_889b:  add
  IL_889c:  call       void [System.Console]System.Console::Write(int32)
  IL_88a1:  dup
  IL_88a2:  ldc.i4.6
  IL_88a3:  shr
  IL_88a4:  call       void [System.Console]System.Console::Write(int32)
  IL_88a9:  dup
  IL_88aa:  ldc.i4.5
  IL_88ab:  sub
  IL_88ac:  call       void [System.Console]System.Console::Write(int32)
  IL_88b1:  dup
  IL_88b2:  ldc.i4.1
  IL_88b3:  sub
  IL_88b4:  call       void [System.Console]System.Console::Write(int32)
  IL_88b9:  dup
  IL_88ba:  ldc.i4.1
  IL_88bb:  xor
  IL_88bc:  call       void [System.Console]System.Console::Write(int32)
  IL_88c1:  dup
  IL_88c2:  ldc.i4.2
  IL_88c3:  shr
  IL_88c4:  call       void [System.Console]System.Console::Write(int32)
  IL_88c9:  dup
  IL_88ca:  ldc.i4.7
  IL_88cb:  sub
  IL_88cc:  call       void [System.Console]System.Console::Write(int32)
  IL_88d1:  dup
  IL_88d2:  ldc.i4.3
  IL_88d3:  add
  IL_88d4:  call       void [System.Console]System.Console::Write(int32)
  IL_88d9:  dup
  IL_88da:  ldc.i4.5
  IL_88db:  add
  IL_88dc:  call       void [System.Console]System.Console::Write(int32)
  IL_88e1:  dup
  IL_88e2:  ldc.i4.6
  IL_88e3:  sub
  IL_88e4:  call       void [System.Console]System.Console::Write(int32)
  IL_88e9:  dup
  IL_88ea:  ldc.i4.3
  IL_88eb:  shl
  IL_88ec:  call       void [System.Console]System.Console::Write(int32)
  IL_88f1:  dup
  IL_88f2:  ldc.i4.0
  IL_88f3:  mul
  IL_88f4:  call       void [System.Console]System.Console::Write(int32)
  IL_88f9:  dup
  IL_88fa:  ldc.i4.0
  IL_88fb:  shl
  IL_88fc:  call       void [System.Console]System.Console::Write(int32)
  IL_8901:  dup
  IL_8902:  ldc.i4.3
  IL_8903:  shl
  IL_8904:  call       void [System.Console]System.Console::Write(int32)
  IL_8909:  dup
  IL_890a:  ldc.i4.4
  IL_890b:  mul
  IL_890c:  call       void [System.Console]System.Console::Write(int32)
  IL_8911:  dup
  IL_8912:  ldc.i4.7
  IL_8913:  mul
  IL_8914:  call       void [System.Console]System.Console::Write(int32)
  IL_8919:  dup
  IL_891a:  ldc.i4.1
  IL_891b:  sub
  IL_891c:  call       void [System.Console]System.Console::Write(int32)
  IL_8921:  dup
  IL_8922:  ldc.i4.3
  IL_8923:  xor
  IL_8924:  call       void [System.Console]System.Console::Write(int32)
  IL_8929:  dup
  IL_892a:  ldc.i4.0
  IL_892b:  xor
  IL_892c:  call       void [System.Console]System.Console::Write(int32)
  IL_8931:  dup
  IL_8932:  ldc.i4.3
  IL_8933:  and
  IL_8934:  call       void [System.Console]System.Console::Write(int32)
  IL_8939:  dup
  IL_893a:  ldc.i4.5
  IL_893b:  add
  IL_893c:  call       void [System.Console]System.Console::Write(int32)
  IL_8941:  dup
  IL_8942:  ldc.i4.2
  IL_8943:  xor
  IL_8944:  call       void [System.Console]System.Console::Write(int32)
  IL_8949:  dup
  IL_894a:  ldc.i4.5
  IL_894b:  shr
  IL_894c:  call       void [System.Console]System.Console::Write(int32)
  IL_8951:  dup
  IL_8952:  ldc.i4.4
  IL_8953:  xor
  IL_8954:  call       void [System.Console]System.Console::Write(int32)
  IL_8959:  dup
  IL_895a:  ldc.i4.4
  IL_895b:  shr
  IL_895c:  call       void [System.Console]System.Console::Write(int32)
  IL_8961:  dup
  IL_8962:  ldc.i4.1
  IL_8963:  xor
  IL_8964:  call       void [System.Console]System.Console::Write(int32)
  IL_8969:  dup
  IL_896a:  ldc.i4.8
  IL_896b:  add
  IL_896c:  call       void [System.Console]System.Console::Write(int32)
  IL_8971:  dup
  IL_8972:  ldc.i4.4
  IL_8973:  add
  IL_8974:  call       void [System.Console]System.Console::Write(int32)
  IL_8979:  dup
  IL_897a:  ldc.i4.0
  IL_897b:  xor
  IL_897c:  call       void [System.Console]System.Console::Write(int32)
  IL_8981:  dup
  IL_8982:  ldc.i4.5
  IL_8983:  add
  IL_8984:  call       void [System.Console]System.Console::Write(int32)
  IL_8989:  dup
  IL_898a:  ldc.i4.7
  IL_898b:  shl
  IL_898c:  call       void [System.Console]System.Console::Write(int32)
  IL_8991:  dup
  IL_8992:  ldc.i4.5
  IL_8993:  and
  IL_8994:  call       void [System.Console]System.Console::Write(int32)
  IL_8999:  dup
  IL_899a:  ldc.i4.5
  IL_899b:  add
  IL_899c:  call       void [System.Console]System.Console::Write(int32)
  IL_89a1:  dup
  IL_89a2:  ldc.i4.4
  IL_89a3:  shr
  IL_89a4:  call       void [System.Console]System.Console::Write(int32)
  IL_89a9:  dup
  IL_89aa:  ldc.i4.6
  IL_89ab:  and
  IL_89ac:  call       void [System.Console]System.Console::Write(int32)
  IL_89b1:  dup
  IL_89b2:  ldc.i4.8
  IL_89b3:  sub
  IL_89b4:  call       void [System.Console]System.Console::Write(int32)
  IL_89b9:  dup
  IL_89ba:  ldc.i4.6
  IL_89bb:  or
  IL_89bc:  call       void [System.Console]System.Console::Write(int32)
  IL_89c1:  dup
  IL_89c2:  ldc.i4.4
  IL_89c3:  xor
  IL_89c4:  call       void [System.Console]System.Console::Write(int32)
  IL_89c9:  dup
  IL_89ca:  ldc.i4.2
  IL_89cb:  shr
  IL_89cc:  call       void [System.Console]System.Console::Write(int32)
  IL_89d1:  dup
  IL_89d2:  ldc.i4.6
  IL_89d3:  mul
  IL_89d4:  call       void [System.Console]System.Console::Write(int32)
  IL_89d9:  dup
  IL_89da:  ldc.i4.0
  IL_89db:  or
  IL_89dc:  call       void [System.Console]System.Console::Write(int32)
  IL_89e1:  dup
  IL_89e2:  ldc.i4.0
  IL_89e3:  and
  IL_89e4:  call       void [System.Console]System.Console::Write(int32)
  IL_89e9:  dup
  IL_89ea:  ldc.i4.1
  IL_89eb:  xor
  IL_89ec:  call       void [System.Console]System.Console::Write(int32)
  IL_89f1:  dup
  IL_89f2:  ldc.i4.2
  IL_89f3:  add
  IL_89f4:  call       void [System.Console]System.Console::Write(int32)
  IL_89f9:  dup
  IL_89fa:  ldc.i4.6
  IL_89fb:  mul
  IL_89fc:  call       void [System.Console]System.Console::Write(int32)
  IL_8a01:  dup
  IL_8a02:  ldc.i4.4
  IL_8a03:  shr
  IL_8a04:  call       void [System.Console]System.Console::Write(int32)
  IL_8a09:  dup
  IL_8a0a:  ldc.i4.6
  IL_8a0b:  mul
  IL_8a0c:  call       void [System.Console]System.Console::Write(int32)
  IL_8a11:  dup
  IL_8a12:  ldc.i4.8
  IL_8a13:  add
  IL_8a14:  call       void [System.Console]System.Console::Write(int32)
  IL_8a19:  dup
  IL_8a1a:  ldc.i4.3
  IL_8a1b:  mul
  IL_8a1c:  call       void [System.Console]System.Console::Write(int32)
  IL_8a21:  dup
  IL_8a22:  ldc.i4.7
  IL_8a23:  shl
  IL_8a24:  call       void [System.Console]System.Console::Write(int32)
  IL_8a29:  dup
  IL_8a2a:  ldc.i4.1
  IL_8a2b:  or
  IL_8a2c:  call       void [System.Console]System.Console::Write(int32)
  IL_8a31:  dup
  IL_8a32:  ldc.i4.6
  IL_8a33:  shr
  IL_8a34:  call       void [System.Console]System.Console::Write(int32)
  IL_8a39:  dup
  IL_8a3a:  ldc.i4.6
  IL_8a3b:  or
  IL_8a3c:  call       void [System.Console]System.Console::Write(int32)
  IL_8a41:  dup
  IL_8a42:  ldc.i4.6
  IL_8a43:  add
  IL_8a44:  call       void [System.Console]System.Console::Write(int32)
  IL_8a49:  dup
  IL_8a4a:  ldc.i4.4
  IL_8a4b:  add
  IL_8a4c:  call       void [System.Console]System.Console::Write(int32)
  IL_8a51:  dup
  IL_8a52:  ldc.i4.7
  IL_8a53:  mul
  IL_8a54:  call       void [System.Console]System.Console::Write(int32)
  IL_8a59:  dup
  IL_8a5a:  ldc.i4.6
  IL_8a5b:  mul
  IL_8a5c:  call       void [System.Console]System.Console::Write(int32)
  IL_8a61:  dup
  IL_8a62:  ldc.i4.8
  IL_8a63:  and
  IL_8a64:  call       void [System.Console]System.Console::Write(int32)
  IL_8a69:  dup
  IL_8a6a:  ldc.i4.4
  IL_8a6b:  and
  IL_8a6c:  call       void [System.Console]System.Console::Write(int32)
  IL_8a71:  dup
  IL_8a72:  ldc.i4.6
  IL_8a73:  sub
  IL_8a74:  call       void [System.Console]System.Console::Write(int32)
  IL_8a79:  dup
  IL_8a7a:  ldc.i4.6
  IL_8a7b:  xor
  IL_8a7c:  call       void [System.Console]System.Console::Write(int32)
  IL_8a81:  dup
  IL_8a82:  ldc.i4.8
  IL_8a83:  xor
  IL_8a84:  call       void [System.Console]System.Console::Write(int32)
  IL_8a89:  dup
  IL_8a8a:  ldc.i4.3
  IL_8a8b:  mul
  IL_8a8c:  call       void [System.Console]System.Console::Write(int32)
  IL_8a91:  dup
  IL_8a92:  ldc.i4.4
  IL_8a93:  mul
  IL_8a94:  call       void [System.Console]System.Console::Write(int32)
  IL_8a99:  dup
  IL_8a9a:  ldc.i4.8
  IL_8a9b:  xor
  IL_8a9c:  call       void [System.Console]System.Console::Write(int32)
  IL_8aa1:  dup
  IL_8aa2:  ldc.i4.0
  IL_8aa3:  or
  IL_8aa4:  call       void [System.Console]System.Console::Write(int32)
  IL_8aa9:  dup
  IL_8aaa:  ldc.i4.8
  IL_8aab:  add
  IL_8aac:  call       void [System.Console]System.Console::Write(int32)
  IL_8ab1:  dup
  IL_8ab2:  ldc.i4.1
  IL_8ab3:  sub
  IL_8ab4:  call       void [System.Console]System.Console::Write(int32)
  IL_8ab9:  dup
  IL_8aba:  ldc.i4.1
  IL_8abb:  xor
  IL_8abc:  call       void [System.Console]System.Console::Write(int32)
  IL_8ac1:  dup
  IL_8ac2:  ldc.i4.1
  IL_8ac3:  mul
  IL_8ac4:  call       void [System.Console]System.Console::Write(int32)
  IL_8ac9:  dup
  IL_8aca:  ldc.i4.6
  IL_8acb:  mul
  IL_8acc:  call       void [System.Console]System.Console::Write(int32)
  IL_8ad1:  dup
  IL_8ad2:  ldc.i4.5
  IL_8ad3:  shl
  IL_8ad4:  call       void [System.Console]System.Console::Write(int32)
  IL_8ad9:  dup
  IL_8ada:  ldc.i4.4
  IL_8adb:  add
  IL_8adc:  call       void [System.Console]System.Console::Write(int32)
  IL_8ae1:  dup
  IL_8ae2:  ldc.i4.8
  IL_8ae3:  xor
  IL_8ae4:  call       void [System.Console]System.Console::Write(int32)
  IL_8ae9:  dup
  IL_8aea:  ldc.i4.3
  IL_8aeb:  sub
  IL_8aec:  call       void [System.Console]System.Console::Write(int32)
  IL_8af1:  dup
  IL_8af2:  ldc.i4.5
  IL_8af3:  shl
  IL_8af4:  call       void [System.Console]System.Console::Write(int32)
  IL_8af9:  dup
  IL_8afa:  ldc.i4.5
  IL_8afb:  shl
  IL_8afc:  call       void [System.Console]System.Console::Write(int32)
  IL_8b01:  dup
  IL_8b02:  ldc.i4.8
  IL_8b03:  shr
  IL_8b04:  call       void [System.Console]System.Console::Write(int32)
  IL_8b09:  dup
  IL_8b0a:  ldc.i4.8
  IL_8b0b:  mul
  IL_8b0c:  call       void [System.Console]System.Console::Write(int32)
  IL_8b11:  dup
  IL_8b12:  ldc.i4.2
  IL_8b13:  xor
  IL_8b14:  call       void [System.Console]System.Console::Write(int32)
  IL_8b19:  dup
  IL_8b1a:  ldc.i4.3
  IL_8b1b:  mul
  IL_8b1c:  call       void [System.Console]System.Console::Write(int32)
  IL_8b21:  dup
  IL_8b22:  ldc.i4.0
  IL_8b23:  add
  IL_8b24:  call       void [System.Console]System.Console::Write(int32)
  IL_8b29:  dup
  IL_8b2a:  ldc.i4.0
  IL_8b2b:  and
  IL_8b2c:  call       void [System.Console]System.Console::Write(int32)
  IL_8b31:  dup
  IL_8b32:  ldc.i4.4
  IL_8b33:  shl
  IL_8b34:  call       void [System.Console]System.Console::Write(int32)
  IL_8b39:  dup
  IL_8b3a:  ldc.i4.0
  IL_8b3b:  and
  IL_8b3c:  call       void [System.Console]System.Console::Write(int32)
  IL_8b41:  dup
  IL_8b42:  ldc.i4.5
  IL_8b43:  add
  IL_8b44:  call       void [System.Console]System.Console::Write(int32)
  IL_8b49:  dup
  IL_8b4a:  ldc.i4.5
  IL_8b4b:  xor
  IL_8b4c:  call       void [System.Console]System.Console::Write(int32)
  IL_8b51:  dup
  IL_8b52:  ldc.i4.4
  IL_8b53:  mul
  IL_8b54:  call       void [System.Console]System.Console::Write(int32)
  IL_8b59:  dup
  IL_8b5a:  ldc.i4.8
  IL_8b5b:  mul
  IL_8b5c:  call       void [System.Console]System.Console::Write(int32)
  IL_8b61:  dup
  IL_8b62:  ldc.i4.8
  IL_8b63:  mul
  IL_8b64:  call       void [System.Console]System.Console::Write(int32)
  IL_8b69:  dup
  IL_8b6a:  ldc.i4.3
  IL_8b6b:  add
  IL_8b6c:  call       void [System.Console]System.Console::Write(int32)
  IL_8b71:  dup
  IL_8b72:  ldc.i4.8
  IL_8b73:  and
  IL_8b74:  call       void [System.Console]System.Console::Write(int32)
  IL_8b79:  dup
  IL_8b7a:  ldc.i4.8
  IL_8b7b:  xor
  IL_8b7c:  call       void [System.Console]System.Console::Write(int32)
  IL_8b81:  dup
  IL_8b82:  ldc.i4.2
  IL_8b83:  mul
  IL_8b84:  call       void [System.Console]System.Console::Write(int32)
  IL_8b89:  dup
  IL_8b8a:  ldc.i4.1
  IL_8b8b:  shl
  IL_8b8c:  call       void [System.Console]System.Console::Write(int32)
  IL_8b91:  dup
  IL_8b92:  ldc.i4.1
  IL_8b93:  or
  IL_8b94:  call       void [System.Console]System.Console::Write(int32)
  IL_8b99:  dup
  IL_8b9a:  ldc.i4.2
  IL_8b9b:  shr
  IL_8b9c:  call       void [System.Console]System.Console::Write(int32)
  IL_8ba1:  dup
  IL_8ba2:  ldc.i4.7
  IL_8ba3:  mul
  IL_8ba4:  call       void [System.Console]System.Console::Write(int32)
  IL_8ba9:  dup
  IL_8baa:  ldc.i4.2
  IL_8bab:  xor
  IL_8bac:  call       void [System.Console]System.Console::Write(int32)
  IL_8bb1:  dup
  IL_8bb2:  ldc.i4.4
  IL_8bb3:  shr
  IL_8bb4:  call       void [System.Console]System.Console::Write(int32)
  IL_8bb9:  dup
  IL_8bba:  ldc.i4.5
  IL_8bbb:  xor
  IL_8bbc:  call       void [System.Console]System.Console::Write(int32)
  IL_8bc1:  dup
  IL_8bc2:  ldc.i4.3
  IL_8bc3:  shr
  IL_8bc4:  call       void [System.Console]System.Console::Write(int32)
  IL_8bc9:  dup
  IL_8bca:  ldc.i4.4
  IL_8bcb:  shl
  IL_8bcc:  call       void [System.Console]System.Console::Write(int32)
  IL_8bd1:  dup
  IL_8bd2:  ldc.i4.8
  IL_8bd3:  sub
  IL_8bd4:  call       void [System.Console]System.Console::Write(int32)
  IL_8bd9:  dup
  IL_8bda:  ldc.i4.0
  IL_8bdb:  shl
  IL_8bdc:  call       void [System.Console]System.Console::Write(int32)
  IL_8be1:  dup
  IL_8be2:  ldc.i4.7
  IL_8be3:  shr
  IL_8be4:  call       void [System.Console]System.Console::Write(int32)
  IL_8be9:  dup
  IL_8bea:  ldc.i4.3
  IL_8beb:  or
  IL_8bec:  call       void [System.Console]System.Console::Write(int32)
  IL_8bf1:  dup
  IL_8bf2:  ldc.i4.0
  IL_8bf3:  mul
  IL_8bf4:  call       void [System.Console]System.Console::Write(int32)
  IL_8bf9:  dup
  IL_8bfa:  ldc.i4.3
  IL_8bfb:  sub
  IL_8bfc:  call       void [System.Console]System.Console::Write(int32)
  IL_8c01:  dup
  IL_8c02:  ldc.i4.1
  IL_8c03:  shr
  IL_8c04:  call       void [System.Console]System.Console::Write(int32)
  IL_8c09:  dup
  IL_8c0a:  ldc.i4.5
  IL_8c0b:  sub
  IL_8c0c:  call       void [System.Console]System.Console::Write(int32)
  IL_8c11:  dup
  IL_8c12:  ldc.i4.3
  IL_8c13:  shl
  IL_8c14:  call       void [System.Console]System.Console::Write(int32)
  IL_8c19:  dup
  IL_8c1a:  ldc.i4.4
  IL_8c1b:  shr
  IL_8c1c:  call       void [System.Console]System.Console::Write(int32)
  IL_8c21:  dup
  IL_8c22:  ldc.i4.7
  IL_8c23:  or
  IL_8c24:  call       void [System.Console]System.Console::Write(int32)
  IL_8c29:  dup
  IL_8c2a:  ldc.i4.5
  IL_8c2b:  sub
  IL_8c2c:  call       void [System.Console]System.Console::Write(int32)
  IL_8c31:  dup
  IL_8c32:  ldc.i4.0
  IL_8c33:  xor
  IL_8c34:  call       void [System.Console]System.Console::Write(int32)
  IL_8c39:  dup
  IL_8c3a:  ldc.i4.6
  IL_8c3b:  sub
  IL_8c3c:  call       void [System.Console]System.Console::Write(int32)
  IL_8c41:  dup
  IL_8c42:  ldc.i4.5
  IL_8c43:  or
  IL_8c44:  call       void [System.Console]System.Console::Write(int32)
  IL_8c49:  dup
  IL_8c4a:  ldc.i4.6
  IL_8c4b:  mul
  IL_8c4c:  call       void [System.Console]System.Console::Write(int32)
  IL_8c51:  dup
  IL_8c52:  ldc.i4.2
  IL_8c53:  mul
  IL_8c54:  call       void [System.Console]System.Console::Write(int32)
  IL_8c59:  dup
  IL_8c5a:  ldc.i4.4
  IL_8c5b:  add
  IL_8c5c:  call       void [System.Console]System.Console::Write(int32)
  IL_8c61:  dup
  IL_8c62:  ldc.i4.1
  IL_8c63:  shr
  IL_8c64:  call       void [System.Console]System.Console::Write(int32)
  IL_8c69:  dup
  IL_8c6a:  ldc.i4.6
  IL_8c6b:  shl
  IL_8c6c:  call       void [System.Console]System.Console::Write(int32)
  IL_8c71:  dup
  IL_8c72:  ldc.i4.4
  IL_8c73:  sub
  IL_8c74:  call       void [System.Console]System.Console::Write(int32)
  IL_8c79:  dup
  IL_8c7a:  ldc.i4.5
  IL_8c7b:  shl
  IL_8c7c:  call       void [System.Console]System.Console::Write(int32)
  IL_8c81:  dup
  IL_8c82:  ldc.i4.8
  IL_8c83:  add
  IL_8c84:  call       void [System.Console]System.Console::Write(int32)
  IL_8c89:  dup
  IL_8c8a:  ldc.i4.2
  IL_8c8b:  or
  IL_8c8c:  call       void [System.Console]System.Console::Write(int32)
  IL_8c91:  dup
  IL_8c92:  ldc.i4.0
  IL_8c93:  xor
  IL_8c94:  call       void [System.Console]System.Console::Write(int32)
  IL_8c99:  dup
  IL_8c9a:  ldc.i4.0
  IL_8c9b:  shr
  IL_8c9c:  call       void [System.Console]System.Console::Write(int32)
  IL_8ca1:  dup
  IL_8ca2:  ldc.i4.0
  IL_8ca3:  and
  IL_8ca4:  call       void [System.Console]System.Console::Write(int32)
  IL_8ca9:  dup
  IL_8caa:  ldc.i4.6
  IL_8cab:  or
  IL_8cac:  call       void [System.Console]System.Console::Write(int32)
  IL_8cb1:  dup
  IL_8cb2:  ldc.i4.5
  IL_8cb3:  or
  IL_8cb4:  call       void [System.Console]System.Console::Write(int32)
  IL_8cb9:  dup
  IL_8cba:  ldc.i4.4
  IL_8cbb:  xor
  IL_8cbc:  call       void [System.Console]System.Console::Write(int32)
  IL_8cc1:  dup
  IL_8cc2:  ldc.i4.8
  IL_8cc3:  sub
  IL_8cc4:  call       void [System.Console]System.Console::Write(int32)
  IL_8cc9:  dup
  IL_8cca:  ldc.i4.3
  IL_8ccb:  sub
  IL_8ccc:  call       void [System.Console]System.Console::Write(int32)
  IL_8cd1:  dup
  IL_8cd2:  ldc.i4.5
  IL_8cd3:  add
  IL_8cd4:  call       void [System.Console]System.Console::Write(int32)
  IL_8cd9:  dup
  IL_8cda:  ldc.i4.7
  IL_8cdb:  and
  IL_8cdc:  call       void [System.Console]System.Console::Write(int32)
  IL_8ce1:  dup
  IL_8ce2:  ldc.i4.0
  IL_8ce3:  xor
  IL_8ce4:  call       void [System.Console]System.Console::Write(int32)
  IL_8ce9:  dup
  IL_8cea:  ldc.i4.8
  IL_8ceb:  shr
  IL_8cec:  call       void [System.Console]System.Console::Write(int32)
  IL_8cf1:  dup
  IL_8cf2:  ldc.i4.7
  IL_8cf3:  or
  IL_8cf4:  call       void [System.Console]System.Console::Write(int32)
  IL_8cf9:  dup
  IL_8cfa:  ldc.i4.1
  IL_8cfb:  shl
  IL_8cfc:  call       void [System.Console]System.Console::Write(int32)
  IL_8d01:  dup
  IL_8d02:  ldc.i4.2
  IL_8d03:  add
  IL_8d04:  call       void [System.Console]System.Console::Write(int32)
  IL_8d09:  dup
  IL_8d0a:  ldc.i4.2
  IL_8d0b:  or
  IL_8d0c:  call       void [System.Console]System.Console::Write(int32)
  IL_8d11:  dup
  IL_8d12:  ldc.i4.0
  IL_8d13:  and
  IL_8d14:  call       void [System.Console]System.Console::Write(int32)
  IL_8d19:  dup
  IL_8d1a:  ldc.i4.1
  IL_8d1b:  sub
  IL_8d1c:  call       void [System.Console]System.Console::Write(int32)
  IL_8d21:  dup
  IL_8d22:  ldc.i4.7
  IL_8d23:  add
  IL_8d24:  call       void [System.Console]System.Console::Write(int32)
  IL_8d29:  dup
  IL_8d2a:  ldc.i4.2
  IL_8d2b:  shl
  IL_8d2c:  call       void [System.Console]System.Console::Write(int32)
  IL_8d31:  dup
  IL_8d32:  ldc.i4.5
  IL_8d33:  shl
  IL_8d34:  call       void [System.Console]System.Console::Write(int32)
  IL_8d39:  dup
  IL_8d3a:  ldc.i4.4
  IL_8d3b:  mul
  IL_8d3c:  call       void [System.Console]System.Console::Write(int32)
  IL_8d41:  dup
  IL_8d42:  ldc.i4.1
  IL_8d43:  mul
  IL_8d44:  call       void [System.Console]System.Console::Write(int32)
  IL_8d49:  dup
  IL_8d4a:  ldc.i4.5
  IL_8d4b:  mul
  IL_8d4c:  call       void [System.Console]System.Console::Write(int32)
  IL_8d51:  dup
  IL_8d52:  ldc.i4.7
  IL_8d53:  xor
  IL_8d54:  call       void [System.Console]System.Console::Write(int32)
  IL_8d59:  dup
  IL_8d5a:  ldc.i4.8
  IL_8d5b:  and
  IL_8d5c:  call       void [System.Console]System.Console::Write(int32)
  IL_8d61:  dup
  IL_8d62:  ldc.i4.2
  IL_8d63:  shl
  IL_8d64:  call       void [System.Console]System.Console::Write(int32)
  IL_8d69:  dup
  IL_8d6a:  ldc.i4.7
  IL_8d6b:  shl
  IL_8d6c:  call       void [System.Console]System.Console::Write(int32)
  IL_8d71:  dup
  IL_8d72:  ldc.i4.4
  IL_8d73:  mul
  IL_8d74:  call       void [System.Console]System.Console::Write(int32)
  IL_8d79:  dup
  IL_8d7a:  ldc.i4.7
  IL_8d7b:  add
  IL_8d7c:  call       void [System.Console]System.Console::Write(int32)
  IL_8d81:  dup
  IL_8d82:  ldc.i4.5
  IL_8d83:  sub
  IL_8d84:  call       void [System.Console]System.Console::Write(int32)
  IL_8d89:  dup
  IL_8d8a:  ldc.i4.5
  IL_8d8b:  shl
  IL_8d8c:  call       void [System.Console]System.Console::Write(int32)
  IL_8d91:  dup
  IL_8d92:  ldc.i4.5
  IL_8d93:  shl
  IL_8d94:  call       void [System.Console]System.Console::Write(int32)
  IL_8d99:  dup
  IL_8d9a:  ldc.i4.2
  IL_8d9b:  add
  IL_8d9c:  call       void [System.Console]System.Console::Write(int32)
  IL_8da1:  dup
  IL_8da2:  ldc.i4.0
  IL_8da3:  sub
  IL_8da4:  call       void [System.Console]System.Console::Write(int32)
  IL_8da9:  dup
  IL_8daa:  ldc.i4.3
  IL_8dab:  shl
  IL_8dac:  call       void [System.Console]System.Console::Write(int32)
  IL_8db1:  dup
  IL_8db2:  ldc.i4.7
  IL_8db3:  mul
  IL_8db4:  call       void [System.Console]System.Console::Write(int32)
  IL_8db9:  dup
  IL_8dba:  ldc.i4.2
  IL_8dbb:  shl
  IL_8dbc:  call       void [System.Console]System.Console::Write(int32)
  IL_8dc1:  dup
  IL_8dc2:  ldc.i4.0
  IL_8dc3:  shr
  IL_8dc4:  call       void [System.Console]System.Console::Write(int32)
  IL_8dc9:  dup
  IL_8dca:  ldc.i4.5
  IL_8dcb:  shl
  IL_8dcc:  call       void [System.Console]System.Console::Write(int32)
  IL_8dd1:  dup
  IL_8dd2:  ldc.i4.3
  IL_8dd3:  and
  IL_8dd4:  call       void [System.Console]System.Console::Write(int32)
  IL_8dd9:  dup
  IL_8dda:  ldc.i4.6
  IL_8ddb:  and
  IL_8ddc:  call       void [System.Console]System.Console::Write(int32)
  IL_8de1:  dup
  IL_8de2:  ldc.i4.0
  IL_8de3:  xor
  IL_8de4:  call       void [System.Console]System.Console::Write(int32)
  IL_8de9:  dup
  IL_8dea:  ldc.i4.6
  IL_8deb:  shl
  IL_8dec:  call       void [System.Console]System.Console::Write(int32)
  IL_8df1:  dup
  IL_8df2:  ldc.i4.0
  IL_8df3:  shl
  IL_8df4:  call       void [System.Console]System.Console::Write(int32)
  IL_8df9:  dup
  IL_8dfa:  ldc.i4.5
  IL_8dfb:  xor
  IL_8dfc:  call       void [System.Console]System.Console::Write(int32)
  IL_8e01:  dup
  IL_8e02:  ldc.i4.1
  IL_8e03:  sub
  IL_8e04:  call       void [System.Console]System.Console::Write(int32)
  IL_8e09:  dup
  IL_8e0a:  ldc.i4.8
  IL_8e0b:  mul
  IL_8e0c:  call       void [System.Console]System.Console::Write(int32)
  IL_8e11:  dup
  IL_8e12:  ldc.i4.8
  IL_8e13:  or
  IL_8e14:  call       void [System.Console]System.Console::Write(int32)
  IL_8e19:  dup
  IL_8e1a:  ldc.i4.5
  IL_8e1b:  shl
  IL_8e1c:  call       void [System.Console]System.Console::Write(int32)
  IL_8e21:  dup
  IL_8e22:  ldc.i4.8
  IL_8e23:  shl
  IL_8e24:  call       void [System.Console]System.Console::Write(int32)
  IL_8e29:  dup
  IL_8e2a:  ldc.i4.3
  IL_8e2b:  shl
  IL_8e2c:  call       void [System.Console]System.Console::Write(int32)
  IL_8e31:  dup
  IL_8e32:  ldc.i4.0
  IL_8e33:  xor
  IL_8e34:  call       void [System.Console]System.Console::Write(int32)
  IL_8e39:  dup
  IL_8e3a:  ldc.i4.8
  IL_8e3b:  or
  IL_8e3c:  call       void [System.Console]System.Console::Write(int32)
  IL_8e41:  dup
  IL_8e42:  ldc.i4.2
  IL_8e43:  mul
  IL_8e44:  call       void [System.Console]System.Console::Write(int32)
  IL_8e49:  dup
  IL_8e4a:  ldc.i4.5
  IL_8e4b:  and
  IL_8e4c:  call       void [System.Console]System.Console::Write(int32)
  IL_8e51:  dup
  IL_8e52:  ldc.i4.7
  IL_8e53:  mul
  IL_8e54:  call       void [System.Console]System.Console::Write(int32)
  IL_8e59:  dup
  IL_8e5a:  ldc.i4.6
  IL_8e5b:  shr
  IL_8e5c:  call       void [System.Console]System.Console::Write(int32)
  IL_8e61:  dup
  IL_8e62:  ldc.i4.4
  IL_8e63:  and
  IL_8e64:  call       void [System.Console]System.Console::Write(int32)
  IL_8e69:  dup
  IL_8e6a:  ldc.i4.4
  IL_8e6b:  sub
  IL_8e6c:  call       void [System.Console]System.Console::Write(int32)
  IL_8e71:  dup
  IL_8e72:  ldc.i4.7
  IL_8e73:  and
  IL_8e74:  call       void [System.Console]System.Console::Write(int32)
  IL_8e79:  dup
  IL_8e7a:  ldc.i4.0
  IL_8e7b:  xor
  IL_8e7c:  call       void [System.Console]System.Console::Write(int32)
  IL_8e81:  dup
  IL_8e82:  ldc.i4.5
  IL_8e83:  or
  IL_8e84:  call       void [System.Console]System.Console::Write(int32)
  IL_8e89:  dup
  IL_8e8a:  ldc.i4.3
  IL_8e8b:  shr
  IL_8e8c:  call       void [System.Console]System.Console::Write(int32)
  IL_8e91:  dup
  IL_8e92:  ldc.i4.3
  IL_8e93:  or
  IL_8e94:  call       void [System.Console]System.Console::Write(int32)
  IL_8e99:  dup
  IL_8e9a:  ldc.i4.5
  IL_8e9b:  add
  IL_8e9c:  call       void [System.Console]System.Console::Write(int32)
  IL_8ea1:  dup
  IL_8ea2:  ldc.i4.0
  IL_8ea3:  or
  IL_8ea4:  call       void [System.Console]System.Console::Write(int32)
  IL_8ea9:  dup
  IL_8eaa:  ldc.i4.5
  IL_8eab:  and
  IL_8eac:  call       void [System.Console]System.Console::Write(int32)
  IL_8eb1:  dup
  IL_8eb2:  ldc.i4.4
  IL_8eb3:  add
  IL_8eb4:  call       void [System.Console]System.Console::Write(int32)
  IL_8eb9:  dup
  IL_8eba:  ldc.i4.6
  IL_8ebb:  shl
  IL_8ebc:  call       void [System.Console]System.Console::Write(int32)
  IL_8ec1:  dup
  IL_8ec2:  ldc.i4.4
  IL_8ec3:  and
  IL_8ec4:  call       void [System.Console]System.Console::Write(int32)
  IL_8ec9:  dup
  IL_8eca:  ldc.i4.2
  IL_8ecb:  shr
  IL_8ecc:  call       void [System.Console]System.Console::Write(int32)
  IL_8ed1:  dup
  IL_8ed2:  ldc.i4.8
  IL_8ed3:  shr
  IL_8ed4:  call       void [System.Console]System.Console::Write(int32)
  IL_8ed9:  dup
  IL_8eda:  ldc.i4.5
  IL_8edb:  and
  IL_8edc:  call       void [System.Console]System.Console::Write(int32)
  IL_8ee1:  dup
  IL_8ee2:  ldc.i4.3
  IL_8ee3:  sub
  IL_8ee4:  call       void [System.Console]System.Console::Write(int32)
  IL_8ee9:  dup
  IL_8eea:  ldc.i4.7
  IL_8eeb:  sub
  IL_8eec:  call       void [System.Console]System.Console::Write(int32)
  IL_8ef1:  dup
  IL_8ef2:  ldc.i4.3
  IL_8ef3:  xor
  IL_8ef4:  call       void [System.Console]System.Console::Write(int32)
  IL_8ef9:  dup
  IL_8efa:  ldc.i4.7
  IL_8efb:  sub
  IL_8efc:  call       void [System.Console]System.Console::Write(int32)
  IL_8f01:  dup
  IL_8f02:  ldc.i4.4
  IL_8f03:  or
  IL_8f04:  call       void [System.Console]System.Console::Write(int32)
  IL_8f09:  dup
  IL_8f0a:  ldc.i4.8
  IL_8f0b:  sub
  IL_8f0c:  call       void [System.Console]System.Console::Write(int32)
  IL_8f11:  dup
  IL_8f12:  ldc.i4.0
  IL_8f13:  sub
  IL_8f14:  call       void [System.Console]System.Console::Write(int32)
  IL_8f19:  dup
  IL_8f1a:  ldc.i4.5
  IL_8f1b:  xor
  IL_8f1c:  call       void [System.Console]System.Console::Write(int32)
  IL_8f21:  dup
  IL_8f22:  ldc.i4.6
  IL_8f23:  shl
  IL_8f24:  call       void [System.Console]System.Console::Write(int32)
  IL_8f29:  dup
  IL_8f2a:  ldc.i4.7
  IL_8f2b:  and
  IL_8f2c:  call       void [System.Console]System.Console::Write(int32)
  IL_8f31:  dup
  IL_8f32:  ldc.i4.6
  IL_8f33:  or
  IL_8f34:  call       void [System.Console]System.Console::Write(int32)
  IL_8f39:  dup
  IL_8f3a:  ldc.i4.1
  IL_8f3b:  xor
  IL_8f3c:  call       void [System.Console]System.Console::Write(int32)
  IL_8f41:  dup
  IL_8f42:  ldc.i4.0
  IL_8f43:  or
  IL_8f44:  call       void [System.Console]System.Console::Write(int32)
  IL_8f49:  dup
  IL_8f4a:  ldc.i4.5
  IL_8f4b:  add
  IL_8f4c:  call       void [System.Console]System.Console::Write(int32)
  IL_8f51:  dup
  IL_8f52:  ldc.i4.4
  IL_8f53:  mul
  IL_8f54:  call       void [System.Console]System.Console::Write(int32)
  IL_8f59:  dup
  IL_8f5a:  ldc.i4.4
  IL_8f5b:  or
  IL_8f5c:  call       void [System.Console]System.Console::Write(int32)
  IL_8f61:  dup
  IL_8f62:  ldc.i4.2
  IL_8f63:  shl
  IL_8f64:  call       void [System.Console]System.Console::Write(int32)
  IL_8f69:  dup
  IL_8f6a:  ldc.i4.3
  IL_8f6b:  add
  IL_8f6c:  call       void [System.Console]System.Console::Write(int32)
  IL_8f71:  dup
  IL_8f72:  ldc.i4.0
  IL_8f73:  xor
  IL_8f74:  call       void [System.Console]System.Console::Write(int32)
  IL_8f79:  dup
  IL_8f7a:  ldc.i4.5
  IL_8f7b:  shl
  IL_8f7c:  call       void [System.Console]System.Console::Write(int32)
  IL_8f81:  dup
  IL_8f82:  ldc.i4.1
  IL_8f83:  mul
  IL_8f84:  call       void [System.Console]System.Console::Write(int32)
  IL_8f89:  dup
  IL_8f8a:  ldc.i4.2
  IL_8f8b:  shl
  IL_8f8c:  call       void [System.Console]System.Console::Write(int32)
  IL_8f91:  dup
  IL_8f92:  ldc.i4.0
  IL_8f93:  shl
  IL_8f94:  call       void [System.Console]System.Console::Write(int32)
  IL_8f99:  dup
  IL_8f9a:  ldc.i4.5
  IL_8f9b:  mul
  IL_8f9c:  call       void [System.Console]System.Console::Write(int32)
  IL_8fa1:  dup
  IL_8fa2:  ldc.i4.1
  IL_8fa3:  or
  IL_8fa4:  call       void [System.Console]System.Console::Write(int32)
  IL_8fa9:  dup
  IL_8faa:  ldc.i4.0
  IL_8fab:  xor
  IL_8fac:  call       void [System.Console]System.Console::Write(int32)
  IL_8fb1:  dup
  IL_8fb2:  ldc.i4.7
  IL_8fb3:  and
  IL_8fb4:  call       void [System.Console]System.Console::Write(int32)
  IL_8fb9:  dup
  IL_8fba:  ldc.i4.0
  IL_8fbb:  xor
  IL_8fbc:  call       void [System.Console]System.Console::Write(int32)
  IL_8fc1:  dup
  IL_8fc2:  ldc.i4.0
  IL_8fc3:  xor
  IL_8fc4:  call       void [System.Console]System.Console::Write(int32)
  IL_8fc9:  dup
  IL_8fca:  ldc.i4.0
  IL_8fcb:  shl
  IL_8fcc:  call       void [System.Console]System.Console::Write(int32)
  IL_8fd1:  dup
  IL_8fd2:  ldc.i4.7
  IL_8fd3:  and
  IL_8fd4:  call       void [System.Console]System.Console::Write(int32)
  IL_8fd9:  dup
  IL_8fda:  ldc.i4.6
  IL_8fdb:  sub
  IL_8fdc:  call       void [System.Console]System.Console::Write(int32)
  IL_8fe1:  dup
  IL_8fe2:  ldc.i4.3
  IL_8fe3:  add
  IL_8fe4:  call       void [System.Console]System.Console::Write(int32)
  IL_8fe9:  dup
  IL_8fea:  ldc.i4.8
  IL_8feb:  xor
  IL_8fec:  call       void [System.Console]System.Console::Write(int32)
  IL_8ff1:  dup
  IL_8ff2:  ldc.i4.7
  IL_8ff3:  xor
  IL_8ff4:  call       void [System.Console]System.Console::Write(int32)
  IL_8ff9:  dup
  IL_8ffa:  ldc.i4.3
  IL_8ffb:  sub
  IL_8ffc:  call       void [System.Console]System.Console::Write(int32)
  IL_9001:  dup
  IL_9002:  ldc.i4.7
  IL_9003:  mul
  IL_9004:  call       void [System.Console]System.Console::Write(int32)
  IL_9009:  dup
  IL_900a:  ldc.i4.5
  IL_900b:  xor
  IL_900c:  call       void [System.Console]System.Console::Write(int32)
  IL_9011:  dup
  IL_9012:  ldc.i4.6
  IL_9013:  shl
  IL_9014:  call       void [System.Console]System.Console::Write(int32)
  IL_9019:  dup
  IL_901a:  ldc.i4.3
  IL_901b:  shl
  IL_901c:  call       void [System.Console]System.Console::Write(int32)
  IL_9021:  dup
  IL_9022:  ldc.i4.2
  IL_9023:  mul
  IL_9024:  call       void [System.Console]System.Console::Write(int32)
  IL_9029:  dup
  IL_902a:  ldc.i4.7
  IL_902b:  shl
  IL_902c:  call       void [System.Console]System.Console::Write(int32)
  IL_9031:  dup
  IL_9032:  ldc.i4.3
  IL_9033:  add
  IL_9034:  call       void [System.Console]System.Console::Write(int32)
  IL_9039:  dup
  IL_903a:  ldc.i4.0
  IL_903b:  xor
  IL_903c:  call       void [System.Console]System.Console::Write(int32)
  IL_9041:  dup
  IL_9042:  ldc.i4.4
  IL_9043:  or
  IL_9044:  call       void [System.Console]System.Console::Write(int32)
  IL_9049:  dup
  IL_904a:  ldc.i4.0
  IL_904b:  shr
  IL_904c:  call       void [System.Console]System.Console::Write(int32)
  IL_9051:  dup
  IL_9052:  ldc.i4.3
  IL_9053:  add
  IL_9054:  call       void [System.Console]System.Console::Write(int32)
  IL_9059:  dup
  IL_905a:  ldc.i4.6
  IL_905b:  xor
  IL_905c:  call       void [System.Console]System.Console::Write(int32)
  IL_9061:  dup
  IL_9062:  ldc.i4.5
  IL_9063:  add
  IL_9064:  call       void [System.Console]System.Console::Write(int32)
  IL_9069:  dup
  IL_906a:  ldc.i4.1
  IL_906b:  or
  IL_906c:  call       void [System.Console]System.Console::Write(int32)
  IL_9071:  dup
  IL_9072:  ldc.i4.5
  IL_9073:  sub
  IL_9074:  call       void [System.Console]System.Console::Write(int32)
  IL_9079:  dup
  IL_907a:  ldc.i4.4
  IL_907b:  shr
  IL_907c:  call       void [System.Console]System.Console::Write(int32)
  IL_9081:  dup
  IL_9082:  ldc.i4.4
  IL_9083:  and
  IL_9084:  call       void [System.Console]System.Console::Write(int32)
  IL_9089:  dup
  IL_908a:  ldc.i4.5
  IL_908b:  shl
  IL_908c:  call       void [System.Console]System.Console::Write(int32)
  IL_9091:  dup
  IL_9092:  ldc.i4.1
  IL_9093:  shr
  IL_9094:  call       void [System.Console]System.Console::Write(int32)
  IL_9099:  dup
  IL_909a:  ldc.i4.6
  IL_909b:  xor
  IL_909c:  call       void [System.Console]System.Console::Write(int32)
  IL_90a1:  dup
  IL_90a2:  ldc.i4.0
  IL_90a3:  shl
  IL_90a4:  call       void [System.Console]System.Console::Write(int32)
  IL_90a9:  dup
  IL_90aa:  ldc.i4.2
  IL_90ab:  xor
  IL_90ac:  call       void [System.Console]System.Console::Write(int32)
  IL_90b1:  dup
  IL_90b2:  ldc.i4.8
  IL_90b3:  sub
  IL_90b4:  call       void [System.Console]System.Console::Write(int32)
  IL_90b9:  dup
  IL_90ba:  ldc.i4.7
  IL_90bb:  mul
  IL_90bc:  call       void [System.Console]System.Console::Write(int32)
  IL_90c1:  dup
  IL_90c2:  ldc.i4.0
  IL_90c3:  shl
  IL_90c4:  call       void [System.Console]System.Console::Write(int32)
  IL_90c9:  dup
  IL_90ca:  ldc.i4.8
  IL_90cb:  or
  IL_90cc:  call       void [System.Console]System.Console::Write(int32)
  IL_90d1:  dup
  IL_90d2:  ldc.i4.3
  IL_90d3:  shr
  IL_90d4:  call       void [System.Console]System.Console::Write(int32)
  IL_90d9:  dup
  IL_90da:  ldc.i4.7
  IL_90db:  shr
  IL_90dc:  call       void [System.Console]System.Console::Write(int32)
  IL_90e1:  dup
  IL_90e2:  ldc.i4.4
  IL_90e3:  sub
  IL_90e4:  call       void [System.Console]System.Console::Write(int32)
  IL_90e9:  dup
  IL_90ea:  ldc.i4.6
  IL_90eb:  shl
  IL_90ec:  call       void [System.Console]System.Console::Write(int32)
  IL_90f1:  dup
  IL_90f2:  ldc.i4.3
  IL_90f3:  xor
  IL_90f4:  call       void [System.Console]System.Console::Write(int32)
  IL_90f9:  dup
  IL_90fa:  ldc.i4.8
  IL_90fb:  sub
  IL_90fc:  call       void [System.Console]System.Console::Write(int32)
  IL_9101:  dup
  IL_9102:  ldc.i4.7
  IL_9103:  add
  IL_9104:  call       void [System.Console]System.Console::Write(int32)
  IL_9109:  dup
  IL_910a:  ldc.i4.0
  IL_910b:  shl
  IL_910c:  call       void [System.Console]System.Console::Write(int32)
  IL_9111:  dup
  IL_9112:  ldc.i4.4
  IL_9113:  xor
  IL_9114:  call       void [System.Console]System.Console::Write(int32)
  IL_9119:  dup
  IL_911a:  ldc.i4.2
  IL_911b:  and
  IL_911c:  call       void [System.Console]System.Console::Write(int32)
  IL_9121:  dup
  IL_9122:  ldc.i4.1
  IL_9123:  shr
  IL_9124:  call       void [System.Console]System.Console::Write(int32)
  IL_9129:  dup
  IL_912a:  ldc.i4.5
  IL_912b:  and
  IL_912c:  call       void [System.Console]System.Console::Write(int32)
  IL_9131:  dup
  IL_9132:  ldc.i4.7
  IL_9133:  mul
  IL_9134:  call       void [System.Console]System.Console::Write(int32)
  IL_9139:  dup
  IL_913a:  ldc.i4.4
  IL_913b:  xor
  IL_913c:  call       void [System.Console]System.Console::Write(int32)
  IL_9141:  dup
  IL_9142:  ldc.i4.2
  IL_9143:  and
  IL_9144:  call       void [System.Console]System.Console::Write(int32)
  IL_9149:  dup
  IL_914a:  ldc.i4.7
  IL_914b:  add
  IL_914c:  call       void [System.Console]System.Console::Write(int32)
  IL_9151:  dup
  IL_9152:  ldc.i4.2
  IL_9153:  shl
  IL_9154:  call       void [System.Console]System.Console::Write(int32)
  IL_9159:  dup
  IL_915a:  ldc.i4.3
  IL_915b:  and
  IL_915c:  call       void [System.Console]System.Console::Write(int32)
  IL_9161:  dup
  IL_9162:  ldc.i4.8
  IL_9163:  and
  IL_9164:  call       void [System.Console]System.Console::Write(int32)
  IL_9169:  dup
  IL_916a:  ldc.i4.0
  IL_916b:  or
  IL_916c:  call       void [System.Console]System.Console::Write(int32)
  IL_9171:  dup
  IL_9172:  ldc.i4.8
  IL_9173:  add
  IL_9174:  call       void [System.Console]System.Console::Write(int32)
  IL_9179:  dup
  IL_917a:  ldc.i4.0
  IL_917b:  shr
  IL_917c:  call       void [System.Console]System.Console::Write(int32)
  IL_9181:  dup
  IL_9182:  ldc.i4.5
  IL_9183:  add
  IL_9184:  call       void [System.Console]System.Console::Write(int32)
  IL_9189:  dup
  IL_918a:  ldc.i4.5
  IL_918b:  and
  IL_918c:  call       void [System.Console]System.Console::Write(int32)
  IL_9191:  dup
  IL_9192:  ldc.i4.1
  IL_9193:  mul
  IL_9194:  call       void [System.Console]System.Console::Write(int32)
  IL_9199:  dup
  IL_919a:  ldc.i4.8
  IL_919b:  shr
  IL_919c:  call       void [System.Console]System.Console::Write(int32)
  IL_91a1:  dup
  IL_91a2:  ldc.i4.2
  IL_91a3:  sub
  IL_91a4:  call       void [System.Console]System.Console::Write(int32)
  IL_91a9:  dup
  IL_91aa:  ldc.i4.6
  IL_91ab:  shr
  IL_91ac:  call       void [System.Console]System.Console::Write(int32)
  IL_91b1:  dup
  IL_91b2:  ldc.i4.1
  IL_91b3:  or
  IL_91b4:  call       void [System.Console]System.Console::Write(int32)
  IL_91b9:  dup
  IL_91ba:  ldc.i4.7
  IL_91bb:  mul
  IL_91bc:  call       void [System.Console]System.Console::Write(int32)
  IL_91c1:  dup
  IL_91c2:  ldc.i4.6
  IL_91c3:  mul
  IL_91c4:  call       void [System.Console]System.Console::Write(int32)
  IL_91c9:  dup
  IL_91ca:  ldc.i4.2
  IL_91cb:  shl
  IL_91cc:  call       void [System.Console]System.Console::Write(int32)
  IL_91d1:  dup
  IL_91d2:  ldc.i4.4
  IL_91d3:  or
  IL_91d4:  call       void [System.Console]System.Console::Write(int32)
  IL_91d9:  dup
  IL_91da:  ldc.i4.7
  IL_91db:  mul
  IL_91dc:  call       void [System.Console]System.Console::Write(int32)
  IL_91e1:  dup
  IL_91e2:  ldc.i4.6
  IL_91e3:  sub
  IL_91e4:  call       void [System.Console]System.Console::Write(int32)
  IL_91e9:  dup
  IL_91ea:  ldc.i4.5
  IL_91eb:  shl
  IL_91ec:  call       void [System.Console]System.Console::Write(int32)
  IL_91f1:  dup
  IL_91f2:  ldc.i4.3
  IL_91f3:  mul
  IL_91f4:  call       void [System.Console]System.Console::Write(int32)
  IL_91f9:  dup
  IL_91fa:  ldc.i4.7
  IL_91fb:  add
  IL_91fc:  call       void [System.Console]System.Console::Write(int32)
  IL_9201:  dup
  IL_9202:  ldc.i4.5
  IL_9203:  or
  IL_9204:  call       void [System.Console]System.Console::Write(int32)
  IL_9209:  dup
  IL_920a:  ldc.i4.2
  IL_920b:  and
  IL_920c:  call       void [System.Console]System.Console::Write(int32)
  IL_9211:  dup
  IL_9212:  ldc.i4.6
  IL_9213:  shr
  IL_9214:  call       void [System.Console]System.Console::Write(int32)
  IL_9219:  dup
  IL_921a:  ldc.i4.3
  IL_921b:  add
  IL_921c:  call       void [System.Console]System.Console::Write(int32)
  IL_9221:  dup
  IL_9222:  ldc.i4.8
  IL_9223:  sub
  IL_9224:  call       void [System.Console]System.Console::Write(int32)
  IL_9229:  dup
  IL_922a:  ldc.i4.6
  IL_922b:  sub
  IL_922c:  call       void [System.Console]System.Console::Write(int32)
  IL_9231:  dup
  IL_9232:  ldc.i4.7
  IL_9233:  shr
  IL_9234:  call       void [System.Console]System.Console::Write(int32)
  IL_9239:  dup
  IL_923a:  ldc.i4.6
  IL_923b:  add
  IL_923c:  call       void [System.Console]System.Console::Write(int32)
  IL_9241:  dup
  IL_9242:  ldc.i4.8
  IL_9243:  and
  IL_9244:  call       void [System.Console]System.Console::Write(int32)
  IL_9249:  dup
  IL_924a:  ldc.i4.1
  IL_924b:  shr
  IL_924c:  call       void [System.Console]System.Console::Write(int32)
  IL_9251:  dup
  IL_9252:  ldc.i4.7
  IL_9253:  shl
  IL_9254:  call       void [System.Console]System.Console::Write(int32)
  IL_9259:  dup
  IL_925a:  ldc.i4.1
  IL_925b:  shr
  IL_925c:  call       void [System.Console]System.Console::Write(int32)
  IL_9261:  dup
  IL_9262:  ldc.i4.2
  IL_9263:  mul
  IL_9264:  call       void [System.Console]System.Console::Write(int32)
  IL_9269:  dup
  IL_926a:  ldc.i4.1
  IL_926b:  or
  IL_926c:  call       void [System.Console]System.Console::Write(int32)
  IL_9271:  dup
  IL_9272:  ldc.i4.6
  IL_9273:  mul
  IL_9274:  call       void [System.Console]System.Console::Write(int32)
  IL_9279:  dup
  IL_927a:  ldc.i4.2
  IL_927b:  or
  IL_927c:  call       void [System.Console]System.Console::Write(int32)
  IL_9281:  dup
  IL_9282:  ldc.i4.2
  IL_9283:  shl
  IL_9284:  call       void [System.Console]System.Console::Write(int32)
  IL_9289:  dup
  IL_928a:  ldc.i4.1
  IL_928b:  sub
  IL_928c:  call       void [System.Console]System.Console::Write(int32)
  IL_9291:  dup
  IL_9292:  ldc.i4.6
  IL_9293:  add
  IL_9294:  call       void [System.Console]System.Console::Write(int32)
  IL_9299:  dup
  IL_929a:  ldc.i4.5
  IL_929b:  add
  IL_929c:  call       void [System.Console]System.Console::Write(int32)
  IL_92a1:  dup
  IL_92a2:  ldc.i4.8
  IL_92a3:  mul
  IL_92a4:  call       void [System.Console]System.Console::Write(int32)
  IL_92a9:  dup
  IL_92aa:  ldc.i4.5
  IL_92ab:  mul
  IL_92ac:  call       void [System.Console]System.Console::Write(int32)
  IL_92b1:  dup
  IL_92b2:  ldc.i4.1
  IL_92b3:  shr
  IL_92b4:  call       void [System.Console]System.Console::Write(int32)
  IL_92b9:  dup
  IL_92ba:  ldc.i4.4
  IL_92bb:  add
  IL_92bc:  call       void [System.Console]System.Console::Write(int32)
  IL_92c1:  dup
  IL_92c2:  ldc.i4.4
  IL_92c3:  shl
  IL_92c4:  call       void [System.Console]System.Console::Write(int32)
  IL_92c9:  dup
  IL_92ca:  ldc.i4.5
  IL_92cb:  shr
  IL_92cc:  call       void [System.Console]System.Console::Write(int32)
  IL_92d1:  dup
  IL_92d2:  ldc.i4.1
  IL_92d3:  sub
  IL_92d4:  call       void [System.Console]System.Console::Write(int32)
  IL_92d9:  dup
  IL_92da:  ldc.i4.2
  IL_92db:  add
  IL_92dc:  call       void [System.Console]System.Console::Write(int32)
  IL_92e1:  dup
  IL_92e2:  ldc.i4.7
  IL_92e3:  shl
  IL_92e4:  call       void [System.Console]System.Console::Write(int32)
  IL_92e9:  dup
  IL_92ea:  ldc.i4.4
  IL_92eb:  xor
  IL_92ec:  call       void [System.Console]System.Console::Write(int32)
  IL_92f1:  dup
  IL_92f2:  ldc.i4.4
  IL_92f3:  xor
  IL_92f4:  call       void [System.Console]System.Console::Write(int32)
  IL_92f9:  dup
  IL_92fa:  ldc.i4.2
  IL_92fb:  shl
  IL_92fc:  call       void [System.Console]System.Console::Write(int32)
  IL_9301:  dup
  IL_9302:  ldc.i4.8
  IL_9303:  and
  IL_9304:  call       void [System.Console]System.Console::Write(int32)
  IL_9309:  dup
  IL_930a:  ldc.i4.7
  IL_930b:  sub
  IL_930c:  call       void [System.Console]System.Console::Write(int32)
  IL_9311:  dup
  IL_9312:  ldc.i4.4
  IL_9313:  xor
  IL_9314:  call       void [System.Console]System.Console::Write(int32)
  IL_9319:  dup
  IL_931a:  ldc.i4.2
  IL_931b:  or
  IL_931c:  call       void [System.Console]System.Console::Write(int32)
  IL_9321:  dup
  IL_9322:  ldc.i4.1
  IL_9323:  sub
  IL_9324:  call       void [System.Console]System.Console::Write(int32)
  IL_9329:  dup
  IL_932a:  ldc.i4.5
  IL_932b:  shl
  IL_932c:  call       void [System.Console]System.Console::Write(int32)
  IL_9331:  dup
  IL_9332:  ldc.i4.8
  IL_9333:  or
  IL_9334:  call       void [System.Console]System.Console::Write(int32)
  IL_9339:  dup
  IL_933a:  ldc.i4.4
  IL_933b:  shl
  IL_933c:  call       void [System.Console]System.Console::Write(int32)
  IL_9341:  dup
  IL_9342:  ldc.i4.7
  IL_9343:  add
  IL_9344:  call       void [System.Console]System.Console::Write(int32)
  IL_9349:  dup
  IL_934a:  ldc.i4.2
  IL_934b:  sub
  IL_934c:  call       void [System.Console]System.Console::Write(int32)
  IL_9351:  dup
  IL_9352:  ldc.i4.6
  IL_9353:  shl
  IL_9354:  call       void [System.Console]System.Console::Write(int32)
  IL_9359:  dup
  IL_935a:  ldc.i4.7
  IL_935b:  add
  IL_935c:  call       void [System.Console]System.Console::Write(int32)
  IL_9361:  dup
  IL_9362:  ldc.i4.2
  IL_9363:  add
  IL_9364:  call       void [System.Console]System.Console::Write(int32)
  IL_9369:  dup
  IL_936a:  ldc.i4.3
  IL_936b:  add
  IL_936c:  call       void [System.Console]System.Console::Write(int32)
  IL_9371:  dup
  IL_9372:  ldc.i4.4
  IL_9373:  sub
  IL_9374:  call       void [System.Console]System.Console::Write(int32)
  IL_9379:  dup
  IL_937a:  ldc.i4.4
  IL_937b:  shl
  IL_937c:  call       void [System.Console]System.Console::Write(int32)
  IL_9381:  dup
  IL_9382:  ldc.i4.0
  IL_9383:  or
  IL_9384:  call       void [System.Console]System.Console::Write(int32)
  IL_9389:  dup
  IL_938a:  ldc.i4.2
  IL_938b:  xor
  IL_938c:  call       void [System.Console]System.Console::Write(int32)
  IL_9391:  dup
  IL_9392:  ldc.i4.7
  IL_9393:  sub
  IL_9394:  call       void [System.Console]System.Console::Write(int32)
  IL_9399:  dup
  IL_939a:  ldc.i4.5
  IL_939b:  add
  IL_939c:  call       void [System.Console]System.Console::Write(int32)
  IL_93a1:  dup
  IL_93a2:  ldc.i4.2
  IL_93a3:  shr
  IL_93a4:  call       void [System.Console]System.Console::Write(int32)
  IL_93a9:  dup
  IL_93aa:  ldc.i4.4
  IL_93ab:  or
  IL_93ac:  call       void [System.Console]System.Console::Write(int32)
  IL_93b1:  dup
  IL_93b2:  ldc.i4.5
  IL_93b3:  and
  IL_93b4:  call       void [System.Console]System.Console::Write(int32)
  IL_93b9:  dup
  IL_93ba:  ldc.i4.1
  IL_93bb:  or
  IL_93bc:  call       void [System.Console]System.Console::Write(int32)
  IL_93c1:  dup
  IL_93c2:  ldc.i4.1
  IL_93c3:  add
  IL_93c4:  call       void [System.Console]System.Console::Write(int32)
  IL_93c9:  dup
  IL_93ca:  ldc.i4.4
  IL_93cb:  xor
  IL_93cc:  call       void [System.Console]System.Console::Write(int32)
  IL_93d1:  dup
  IL_93d2:  ldc.i4.1
  IL_93d3:  sub
  IL_93d4:  call       void [System.Console]System.Console::Write(int32)
  IL_93d9:  dup
  IL_93da:  ldc.i4.8
  IL_93db:  xor
  IL_93dc:  call       void [System.Console]System.Console::Write(int32)
  IL_93e1:  dup
  IL_93e2:  ldc.i4.8
  IL_93e3:  and
  IL_93e4:  call       void [System.Console]System.Console::Write(int32)
  IL_93e9:  dup
  IL_93ea:  ldc.i4.8
  IL_93eb:  xor
  IL_93ec:  call       void [System.Console]System.Console::Write(int32)
  IL_93f1:  dup
  IL_93f2:  ldc.i4.0
  IL_93f3:  mul
  IL_93f4:  call       void [System.Console]System.Console::Write(int32)
  IL_93f9:  dup
  IL_93fa:  ldc.i4.3
  IL_93fb:  add
  IL_93fc:  call       void [System.Console]System.Console::Write(int32)
  IL_9401:  dup
  IL_9402:  ldc.i4.5
  IL_9403:  sub
  IL_9404:  call       void [System.Console]System.Console::Write(int32)
  IL_9409:  dup
  IL_940a:  ldc.i4.4
  IL_940b:  or
  IL_940c:  call       void [System.Console]System.Console::Write(int32)
  IL_9411:  dup
  IL_9412:  ldc.i4.5
  IL_9413:  add
  IL_9414:  call       void [System.Console]System.Console::Write(int32)
  IL_9419:  dup
  IL_941a:  ldc.i4.5
  IL_941b:  shr
  IL_941c:  call       void [System.Console]System.Console::Write(int32)
  IL_9421:  dup
  IL_9422:  ldc.i4.4
  IL_9423:  mul
  IL_9424:  call       void [System.Console]System.Console::Write(int32)
  IL_9429:  dup
  IL_942a:  ldc.i4.8
  IL_942b:  or
  IL_942c:  call       void [System.Console]System.Console::Write(int32)
  IL_9431:  dup
  IL_9432:  ldc.i4.1
  IL_9433:  and
  IL_9434:  call       void [System.Console]System.Console::Write(int32)
  IL_9439:  dup
  IL_943a:  ldc.i4.4
  IL_943b:  shl
  IL_943c:  call       void [System.Console]System.Console::Write(int32)
  IL_9441:  dup
  IL_9442:  ldc.i4.8
  IL_9443:  shl
  IL_9444:  call       void [System.Console]System.Console::Write(int32)
  IL_9449:  dup
  IL_944a:  ldc.i4.8
  IL_944b:  xor
  IL_944c:  call       void [System.Console]System.Console::Write(int32)
  IL_9451:  dup
  IL_9452:  ldc.i4.5
  IL_9453:  add
  IL_9454:  call       void [System.Console]System.Console::Write(int32)
  IL_9459:  dup
  IL_945a:  ldc.i4.3
  IL_945b:  and
  IL_945c:  call       void [System.Console]System.Console::Write(int32)
  IL_9461:  dup
  IL_9462:  ldc.i4.0
  IL_9463:  or
  IL_9464:  call       void [System.Console]System.Console::Write(int32)
  IL_9469:  dup
  IL_946a:  ldc.i4.4
  IL_946b:  add
  IL_946c:  call       void [System.Console]System.Console::Write(int32)
  IL_9471:  dup
  IL_9472:  ldc.i4.2
  IL_9473:  sub
  IL_9474:  call       void [System.Console]System.Console::Write(int32)
  IL_9479:  dup
  IL_947a:  ldc.i4.0
  IL_947b:  or
  IL_947c:  call       void [System.Console]System.Console::Write(int32)
  IL_9481:  dup
  IL_9482:  ldc.i4.3
  IL_9483:  xor
  IL_9484:  call       void [System.Console]System.Console::Write(int32)
  IL_9489:  dup
  IL_948a:  ldc.i4.3
  IL_948b:  xor
  IL_948c:  call       void [System.Console]System.Console::Write(int32)
  IL_9491:  dup
  IL_9492:  ldc.i4.2
  IL_9493:  sub
  IL_9494:  call       void [System.Console]System.Console::Write(int32)
  IL_9499:  dup
  IL_949a:  ldc.i4.2
  IL_949b:  sub
  IL_949c:  call       void [System.Console]System.Console::Write(int32)
  IL_94a1:  dup
  IL_94a2:  ldc.i4.3
  IL_94a3:  or
  IL_94a4:  call       void [System.Console]System.Console::Write(int32)
  IL_94a9:  dup
  IL_94aa:  ldc.i4.8
  IL_94ab:  and
  IL_94ac:  call       void [System.Console]System.Console::Write(int32)
  IL_94b1:  dup
  IL_94b2:  ldc.i4.2
  IL_94b3:  add
  IL_94b4:  call       void [System.Console]System.Console::Write(int32)
  IL_94b9:  dup
  IL_94ba:  ldc.i4.2
  IL_94bb:  add
  IL_94bc:  call       void [System.Console]System.Console::Write(int32)
  IL_94c1:  dup
  IL_94c2:  ldc.i4.2
  IL_94c3:  xor
  IL_94c4:  call       void [System.Console]System.Console::Write(int32)
  IL_94c9:  dup
  IL_94ca:  ldc.i4.4
  IL_94cb:  shl
  IL_94cc:  call       void [System.Console]System.Console::Write(int32)
  IL_94d1:  dup
  IL_94d2:  ldc.i4.3
  IL_94d3:  or
  IL_94d4:  call       void [System.Console]System.Console::Write(int32)
  IL_94d9:  dup
  IL_94da:  ldc.i4.3
  IL_94db:  shl
  IL_94dc:  call       void [System.Console]System.Console::Write(int32)
  IL_94e1:  dup
  IL_94e2:  ldc.i4.6
  IL_94e3:  shr
  IL_94e4:  call       void [System.Console]System.Console::Write(int32)
  IL_94e9:  dup
  IL_94ea:  ldc.i4.7
  IL_94eb:  shr
  IL_94ec:  call       void [System.Console]System.Console::Write(int32)
  IL_94f1:  dup
  IL_94f2:  ldc.i4.4
  IL_94f3:  shl
  IL_94f4:  call       void [System.Console]System.Console::Write(int32)
  IL_94f9:  dup
  IL_94fa:  ldc.i4.3
  IL_94fb:  mul
  IL_94fc:  call       void [System.Console]System.Console::Write(int32)
  IL_9501:  dup
  IL_9502:  ldc.i4.2
  IL_9503:  or
  IL_9504:  call       void [System.Console]System.Console::Write(int32)
  IL_9509:  dup
  IL_950a:  ldc.i4.5
  IL_950b:  shr
  IL_950c:  call       void [System.Console]System.Console::Write(int32)
  IL_9511:  dup
  IL_9512:  ldc.i4.4
  IL_9513:  add
  IL_9514:  call       void [System.Console]System.Console::Write(int32)
  IL_9519:  dup
  IL_951a:  ldc.i4.1
  IL_951b:  shr
  IL_951c:  call       void [System.Console]System.Console::Write(int32)
  IL_9521:  dup
  IL_9522:  ldc.i4.1
  IL_9523:  shr
  IL_9524:  call       void [System.Console]System.Console::Write(int32)
  IL_9529:  dup
  IL_952a:  ldc.i4.0
  IL_952b:  shr
  IL_952c:  call       void [System.Console]System.Console::Write(int32)
  IL_9531:  dup
  IL_9532:  ldc.i4.0
  IL_9533:  shl
  IL_9534:  call       void [System.Console]System.Console::Write(int32)
  IL_9539:  dup
  IL_953a:  ldc.i4.5
  IL_953b:  add
  IL_953c:  call       void [System.Console]System.Console::Write(int32)
  IL_9541:  dup
  IL_9542:  ldc.i4.5
  IL_9543:  xor
  IL_9544:  call       void [System.Console]System.Console::Write(int32)
  IL_9549:  dup
  IL_954a:  ldc.i4.7
  IL_954b:  and
  IL_954c:  call       void [System.Console]System.Console::Write(int32)
  IL_9551:  dup
  IL_9552:  ldc.i4.6
  IL_9553:  xor
  IL_9554:  call       void [System.Console]System.Console::Write(int32)
  IL_9559:  dup
  IL_955a:  ldc.i4.5
  IL_955b:  xor
  IL_955c:  call       void [System.Console]System.Console::Write(int32)
  IL_9561:  dup
  IL_9562:  ldc.i4.0
  IL_9563:  and
  IL_9564:  call       void [System.Console]System.Console::Write(int32)
  IL_9569:  dup
  IL_956a:  ldc.i4.6
  IL_956b:  shr
  IL_956c:  call       void [System.Console]System.Console::Write(int32)
  IL_9571:  dup
  IL_9572:  ldc.i4.2
  IL_9573:  shl
  IL_9574:  call       void [System.Console]System.Console::Write(int32)
  IL_9579:  dup
  IL_957a:  ldc.i4.5
  IL_957b:  or
  IL_957c:  call       void [System.Console]System.Console::Write(int32)
  IL_9581:  dup
  IL_9582:  ldc.i4.4
  IL_9583:  shl
  IL_9584:  call       void [System.Console]System.Console::Write(int32)
  IL_9589:  dup
  IL_958a:  ldc.i4.5
  IL_958b:  mul
  IL_958c:  call       void [System.Console]System.Console::Write(int32)
  IL_9591:  dup
  IL_9592:  ldc.i4.3
  IL_9593:  shl
  IL_9594:  call       void [System.Console]System.Console::Write(int32)
  IL_9599:  dup
  IL_959a:  ldc.i4.4
  IL_959b:  sub
  IL_959c:  call       void [System.Console]System.Console::Write(int32)
  IL_95a1:  dup
  IL_95a2:  ldc.i4.1
  IL_95a3:  shr
  IL_95a4:  call       void [System.Console]System.Console::Write(int32)
  IL_95a9:  dup
  IL_95aa:  ldc.i4.3
  IL_95ab:  shl
  IL_95ac:  call       void [System.Console]System.Console::Write(int32)
  IL_95b1:  dup
  IL_95b2:  ldc.i4.7
  IL_95b3:  shr
  IL_95b4:  call       void [System.Console]System.Console::Write(int32)
  IL_95b9:  dup
  IL_95ba:  ldc.i4.3
  IL_95bb:  sub
  IL_95bc:  call       void [System.Console]System.Console::Write(int32)
  IL_95c1:  dup
  IL_95c2:  ldc.i4.0
  IL_95c3:  and
  IL_95c4:  call       void [System.Console]System.Console::Write(int32)
  IL_95c9:  dup
  IL_95ca:  ldc.i4.0
  IL_95cb:  add
  IL_95cc:  call       void [System.Console]System.Console::Write(int32)
  IL_95d1:  dup
  IL_95d2:  ldc.i4.6
  IL_95d3:  shl
  IL_95d4:  call       void [System.Console]System.Console::Write(int32)
  IL_95d9:  dup
  IL_95da:  ldc.i4.5
  IL_95db:  and
  IL_95dc:  call       void [System.Console]System.Console::Write(int32)
  IL_95e1:  dup
  IL_95e2:  ldc.i4.3
  IL_95e3:  add
  IL_95e4:  call       void [System.Console]System.Console::Write(int32)
  IL_95e9:  dup
  IL_95ea:  ldc.i4.6
  IL_95eb:  shr
  IL_95ec:  call       void [System.Console]System.Console::Write(int32)
  IL_95f1:  dup
  IL_95f2:  ldc.i4.3
  IL_95f3:  shl
  IL_95f4:  call       void [System.Console]System.Console::Write(int32)
  IL_95f9:  dup
  IL_95fa:  ldc.i4.3
  IL_95fb:  shl
  IL_95fc:  call       void [System.Console]System.Console::Write(int32)
  IL_9601:  dup
  IL_9602:  ldc.i4.1
  IL_9603:  add
  IL_9604:  call       void [System.Console]System.Console::Write(int32)
  IL_9609:  dup
  IL_960a:  ldc.i4.1
  IL_960b:  shl
  IL_960c:  call       void [System.Console]System.Console::Write(int32)
  IL_9611:  dup
  IL_9612:  ldc.i4.8
  IL_9613:  mul
  IL_9614:  call       void [System.Console]System.Console::Write(int32)
  IL_9619:  dup
  IL_961a:  ldc.i4.5
  IL_961b:  sub
  IL_961c:  call       void [System.Console]System.Console::Write(int32)
  IL_9621:  dup
  IL_9622:  ldc.i4.7
  IL_9623:  shr
  IL_9624:  call       void [System.Console]System.Console::Write(int32)
  IL_9629:  dup
  IL_962a:  ldc.i4.5
  IL_962b:  or
  IL_962c:  call       void [System.Console]System.Console::Write(int32)
  IL_9631:  dup
  IL_9632:  ldc.i4.0
  IL_9633:  or
  IL_9634:  call       void [System.Console]System.Console::Write(int32)
  IL_9639:  dup
  IL_963a:  ldc.i4.5
  IL_963b:  mul
  IL_963c:  call       void [System.Console]System.Console::Write(int32)
  IL_9641:  dup
  IL_9642:  ldc.i4.3
  IL_9643:  xor
  IL_9644:  call       void [System.Console]System.Console::Write(int32)
  IL_9649:  dup
  IL_964a:  ldc.i4.5
  IL_964b:  xor
  IL_964c:  call       void [System.Console]System.Console::Write(int32)
  IL_9651:  dup
  IL_9652:  ldc.i4.5
  IL_9653:  shl
  IL_9654:  call       void [System.Console]System.Console::Write(int32)
  IL_9659:  dup
  IL_965a:  ldc.i4.2
  IL_965b:  add
  IL_965c:  call       void [System.Console]System.Console::Write(int32)
  IL_9661:  dup
  IL_9662:  ldc.i4.8
  IL_9663:  and
  IL_9664:  call       void [System.Console]System.Console::Write(int32)
  IL_9669:  dup
  IL_966a:  ldc.i4.8
  IL_966b:  mul
  IL_966c:  call       void [System.Console]System.Console::Write(int32)
  IL_9671:  dup
  IL_9672:  ldc.i4.7
  IL_9673:  and
  IL_9674:  call       void [System.Console]System.Console::Write(int32)
  IL_9679:  dup
  IL_967a:  ldc.i4.7
  IL_967b:  shr
  IL_967c:  call       void [System.Console]System.Console::Write(int32)
  IL_9681:  dup
  IL_9682:  ldc.i4.2
  IL_9683:  and
  IL_9684:  call       void [System.Console]System.Console::Write(int32)
  IL_9689:  dup
  IL_968a:  ldc.i4.4
  IL_968b:  shr
  IL_968c:  call       void [System.Console]System.Console::Write(int32)
  IL_9691:  dup
  IL_9692:  ldc.i4.4
  IL_9693:  or
  IL_9694:  call       void [System.Console]System.Console::Write(int32)
  IL_9699:  dup
  IL_969a:  ldc.i4.2
  IL_969b:  shr
  IL_969c:  call       void [System.Console]System.Console::Write(int32)
  IL_96a1:  dup
  IL_96a2:  ldc.i4.2
  IL_96a3:  or
  IL_96a4:  call       void [System.Console]System.Console::Write(int32)
  IL_96a9:  dup
  IL_96aa:  ldc.i4.1
  IL_96ab:  add
  IL_96ac:  call       void [System.Console]System.Console::Write(int32)
  IL_96b1:  dup
  IL_96b2:  ldc.i4.8
  IL_96b3:  shr
  IL_96b4:  call       void [System.Console]System.Console::Write(int32)
  IL_96b9:  dup
  IL_96ba:  ldc.i4.6
  IL_96bb:  mul
  IL_96bc:  call       void [System.Console]System.Console::Write(int32)
  IL_96c1:  dup
  IL_96c2:  ldc.i4.4
  IL_96c3:  mul
  IL_96c4:  call       void [System.Console]System.Console::Write(int32)
  IL_96c9:  dup
  IL_96ca:  ldc.i4.3
  IL_96cb:  or
  IL_96cc:  call       void [System.Console]System.Console::Write(int32)
  IL_96d1:  dup
  IL_96d2:  ldc.i4.8
  IL_96d3:  or
  IL_96d4:  call       void [System.Console]System.Console::Write(int32)
  IL_96d9:  dup
  IL_96da:  ldc.i4.8
  IL_96db:  shl
  IL_96dc:  call       void [System.Console]System.Console::Write(int32)
  IL_96e1:  dup
  IL_96e2:  ldc.i4.7
  IL_96e3:  sub
  IL_96e4:  call       void [System.Console]System.Console::Write(int32)
  IL_96e9:  dup
  IL_96ea:  ldc.i4.5
  IL_96eb:  add
  IL_96ec:  call       void [System.Console]System.Console::Write(int32)
  IL_96f1:  dup
  IL_96f2:  ldc.i4.3
  IL_96f3:  mul
  IL_96f4:  call       void [System.Console]System.Console::Write(int32)
  IL_96f9:  dup
  IL_96fa:  ldc.i4.2
  IL_96fb:  add
  IL_96fc:  call       void [System.Console]System.Console::Write(int32)
  IL_9701:  dup
  IL_9702:  ldc.i4.7
  IL_9703:  or
  IL_9704:  call       void [System.Console]System.Console::Write(int32)
  IL_9709:  dup
  IL_970a:  ldc.i4.5
  IL_970b:  sub
  IL_970c:  call       void [System.Console]System.Console::Write(int32)
  IL_9711:  dup
  IL_9712:  ldc.i4.4
  IL_9713:  or
  IL_9714:  call       void [System.Console]System.Console::Write(int32)
  IL_9719:  dup
  IL_971a:  ldc.i4.7
  IL_971b:  xor
  IL_971c:  call       void [System.Console]System.Console::Write(int32)
  IL_9721:  dup
  IL_9722:  ldc.i4.1
  IL_9723:  or
  IL_9724:  call       void [System.Console]System.Console::Write(int32)
  IL_9729:  dup
  IL_972a:  ldc.i4.1
  IL_972b:  shr
  IL_972c:  call       void [System.Console]System.Console::Write(int32)
  IL_9731:  dup
  IL_9732:  ldc.i4.2
  IL_9733:  sub
  IL_9734:  call       void [System.Console]System.Console::Write(int32)
  IL_9739:  dup
  IL_973a:  ldc.i4.1
  IL_973b:  sub
  IL_973c:  call       void [System.Console]System.Console::Write(int32)
  IL_9741:  dup
  IL_9742:  ldc.i4.0
  IL_9743:  shl
  IL_9744:  call       void [System.Console]System.Console::Write(int32)
  IL_9749:  dup
  IL_974a:  ldc.i4.7
  IL_974b:  xor
  IL_974c:  call       void [System.Console]System.Console::Write(int32)
  IL_9751:  dup
  IL_9752:  ldc.i4.4
  IL_9753:  or
  IL_9754:  call       void [System.Console]System.Console::Write(int32)
  IL_9759:  dup
  IL_975a:  ldc.i4.6
  IL_975b:  mul
  IL_975c:  call       void [System.Console]System.Console::Write(int32)
  IL_9761:  dup
  IL_9762:  ldc.i4.7
  IL_9763:  or
  IL_9764:  call       void [System.Console]System.Console::Write(int32)
  IL_9769:  dup
  IL_976a:  ldc.i4.7
  IL_976b:  xor
  IL_976c:  call       void [System.Console]System.Console::Write(int32)
  IL_9771:  dup
  IL_9772:  ldc.i4.3
  IL_9773:  sub
  IL_9774:  call       void [System.Console]System.Console::Write(int32)
  IL_9779:  dup
  IL_977a:  ldc.i4.2
  IL_977b:  or
  IL_977c:  call       void [System.Console]System.Console::Write(int32)
  IL_9781:  dup
  IL_9782:  ldc.i4.5
  IL_9783:  sub
  IL_9784:  call       void [System.Console]System.Console::Write(int32)
  IL_9789:  dup
  IL_978a:  ldc.i4.2
  IL_978b:  or
  IL_978c:  call       void [System.Console]System.Console::Write(int32)
  IL_9791:  dup
  IL_9792:  ldc.i4.0
  IL_9793:  shl
  IL_9794:  call       void [System.Console]System.Console::Write(int32)
  IL_9799:  dup
  IL_979a:  ldc.i4.7
  IL_979b:  shl
  IL_979c:  call       void [System.Console]System.Console::Write(int32)
  IL_97a1:  dup
  IL_97a2:  ldc.i4.4
  IL_97a3:  or
  IL_97a4:  call       void [System.Console]System.Console::Write(int32)
  IL_97a9:  dup
  IL_97aa:  ldc.i4.7
  IL_97ab:  add
  IL_97ac:  call       void [System.Console]System.Console::Write(int32)
  IL_97b1:  dup
  IL_97b2:  ldc.i4.6
  IL_97b3:  mul
  IL_97b4:  call       void [System.Console]System.Console::Write(int32)
  IL_97b9:  dup
  IL_97ba:  ldc.i4.2
  IL_97bb:  shr
  IL_97bc:  call       void [System.Console]System.Console::Write(int32)
  IL_97c1:  dup
  IL_97c2:  ldc.i4.0
  IL_97c3:  mul
  IL_97c4:  call       void [System.Console]System.Console::Write(int32)
  IL_97c9:  dup
  IL_97ca:  ldc.i4.0
  IL_97cb:  and
  IL_97cc:  call       void [System.Console]System.Console::Write(int32)
  IL_97d1:  dup
  IL_97d2:  ldc.i4.3
  IL_97d3:  and
  IL_97d4:  call       void [System.Console]System.Console::Write(int32)
  IL_97d9:  dup
  IL_97da:  ldc.i4.5
  IL_97db:  or
  IL_97dc:  call       void [System.Console]System.Console::Write(int32)
  IL_97e1:  dup
  IL_97e2:  ldc.i4.2
  IL_97e3:  mul
  IL_97e4:  call       void [System.Console]System.Console::Write(int32)
  IL_97e9:  dup
  IL_97ea:  ldc.i4.3
  IL_97eb:  shl
  IL_97ec:  call       void [System.Console]System.Console::Write(int32)
  IL_97f1:  dup
  IL_97f2:  ldc.i4.0
  IL_97f3:  xor
  IL_97f4:  call       void [System.Console]System.Console::Write(int32)
  IL_97f9:  dup
  IL_97fa:  ldc.i4.0
  IL_97fb:  or
  IL_97fc:  call       void [System.Console]System.Console::Write(int32)
  IL_9801:  dup
  IL_9802:  ldc.i4.5
  IL_9803:  sub
  IL_9804:  call       void [System.Console]System.Console::Write(int32)
  IL_9809:  dup
  IL_980a:  ldc.i4.1
  IL_980b:  sub
  IL_980c:  call       void [System.Console]System.Console::Write(int32)
  IL_9811:  dup
  IL_9812:  ldc.i4.8
  IL_9813:  sub
  IL_9814:  call       void [System.Console]System.Console::Write(int32)
  IL_9819:  dup
  IL_981a:  ldc.i4.1
  IL_981b:  xor
  IL_981c:  call       void [System.Console]System.Console::Write(int32)
  IL_9821:  dup
  IL_9822:  ldc.i4.6
  IL_9823:  mul
  IL_9824:  call       void [System.Console]System.Console::Write(int32)
  IL_9829:  dup
  IL_982a:  ldc.i4.8
  IL_982b:  shl
  IL_982c:  call       void [System.Console]System.Console::Write(int32)
  IL_9831:  dup
  IL_9832:  ldc.i4.1
  IL_9833:  shr
  IL_9834:  call       void [System.Console]System.Console::Write(int32)
  IL_9839:  dup
  IL_983a:  ldc.i4.5
  IL_983b:  shl
  IL_983c:  call       void [System.Console]System.Console::Write(int32)
  IL_9841:  dup
  IL_9842:  ldc.i4.5
  IL_9843:  sub
  IL_9844:  call       void [System.Console]System.Console::Write(int32)
  IL_9849:  dup
  IL_984a:  ldc.i4.3
  IL_984b:  add
  IL_984c:  call       void [System.Console]System.Console::Write(int32)
  IL_9851:  dup
  IL_9852:  ldc.i4.1
  IL_9853:  shl
  IL_9854:  call       void [System.Console]System.Console::Write(int32)
  IL_9859:  dup
  IL_985a:  ldc.i4.5
  IL_985b:  shr
  IL_985c:  call       void [System.Console]System.Console::Write(int32)
  IL_9861:  dup
  IL_9862:  ldc.i4.5
  IL_9863:  mul
  IL_9864:  call       void [System.Console]System.Console::Write(int32)
  IL_9869:  dup
  IL_986a:  ldc.i4.6
  IL_986b:  and
  IL_986c:  call       void [System.Console]System.Console::Write(int32)
  IL_9871:  dup
  IL_9872:  ldc.i4.1
  IL_9873:  sub
  IL_9874:  call       void [System.Console]System.Console::Write(int32)
  IL_9879:  dup
  IL_987a:  ldc.i4.4
  IL_987b:  add
  IL_987c:  call       void [System.Console]System.Console::Write(int32)
  IL_9881:  dup
  IL_9882:  ldc.i4.7
  IL_9883:  and
  IL_9884:  call       void [System.Console]System.Console::Write(int32)
  IL_9889:  dup
  IL_988a:  ldc.i4.3
  IL_988b:  shl
  IL_988c:  call       void [System.Console]System.Console::Write(int32)
  IL_9891:  dup
  IL_9892:  ldc.i4.1
  IL_9893:  sub
  IL_9894:  call       void [System.Console]System.Console::Write(int32)
  IL_9899:  dup
  IL_989a:  ldc.i4.6
  IL_989b:  and
  IL_989c:  call       void [System.Console]System.Console::Write(int32)
  IL_98a1:  dup
  IL_98a2:  ldc.i4.0
  IL_98a3:  shl
  IL_98a4:  call       void [System.Console]System.Console::Write(int32)
  IL_98a9:  dup
  IL_98aa:  ldc.i4.0
  IL_98ab:  add
  IL_98ac:  call       void [System.Console]System.Console::Write(int32)
  IL_98b1:  dup
  IL_98b2:  ldc.i4.7
  IL_98b3:  add
  IL_98b4:  call       void [System.Console]System.Console::Write(int32)
  IL_98b9:  dup
  IL_98ba:  ldc.i4.7
  IL_98bb:  shl
  IL_98bc:  call       void [System.Console]System.Console::Write(int32)
  IL_98c1:  dup
  IL_98c2:  ldc.i4.5
  IL_98c3:  shr
  IL_98c4:  call       void [System.Console]System.Console::Write(int32)
  IL_98c9:  dup
  IL_98ca:  ldc.i4.0
  IL_98cb:  and
  IL_98cc:  call       void [System.Console]System.Console::Write(int32)
  IL_98d1:  dup
  IL_98d2:  ldc.i4.0
  IL_98d3:  add
  IL_98d4:  call       void [System.Console]System.Console::Write(int32)
  IL_98d9:  dup
  IL_98da:  ldc.i4.6
  IL_98db:  add
  IL_98dc:  call       void [System.Console]System.Console::Write(int32)
  IL_98e1:  dup
  IL_98e2:  ldc.i4.5
  IL_98e3:  xor
  IL_98e4:  call       void [System.Console]System.Console::Write(int32)
  IL_98e9:  dup
  IL_98ea:  ldc.i4.4
  IL_98eb:  shr
  IL_98ec:  call       void [System.Console]System.Console::Write(int32)
  IL_98f1:  dup
  IL_98f2:  ldc.i4.4
  IL_98f3:  xor
  IL_98f4:  call       void [System.Console]System.Console::Write(int32)
  IL_98f9:  dup
  IL_98fa:  ldc.i4.2
  IL_98fb:  shl
  IL_98fc:  call       void [System.Console]System.Console::Write(int32)
  IL_9901:  dup
  IL_9902:  ldc.i4.7
  IL_9903:  and
  IL_9904:  call       void [System.Console]System.Console::Write(int32)
  IL_9909:  dup
  IL_990a:  ldc.i4.6
  IL_990b:  sub
  IL_990c:  call       void [System.Console]System.Console::Write(int32)
  IL_9911:  dup
  IL_9912:  ldc.i4.2
  IL_9913:  sub
  IL_9914:  call       void [System.Console]System.Console::Write(int32)
  IL_9919:  dup
  IL_991a:  ldc.i4.4
  IL_991b:  xor
  IL_991c:  call       void [System.Console]System.Console::Write(int32)
  IL_9921:  dup
  IL_9922:  ldc.i4.4
  IL_9923:  shl
  IL_9924:  call       void [System.Console]System.Console::Write(int32)
  IL_9929:  dup
  IL_992a:  ldc.i4.8
  IL_992b:  shl
  IL_992c:  call       void [System.Console]System.Console::Write(int32)
  IL_9931:  dup
  IL_9932:  ldc.i4.4
  IL_9933:  shl
  IL_9934:  call       void [System.Console]System.Console::Write(int32)
  IL_9939:  dup
  IL_993a:  ldc.i4.7
  IL_993b:  shl
  IL_993c:  call       void [System.Console]System.Console::Write(int32)
  IL_9941:  dup
  IL_9942:  ldc.i4.4
  IL_9943:  sub
  IL_9944:  call       void [System.Console]System.Console::Write(int32)
  IL_9949:  dup
  IL_994a:  ldc.i4.1
  IL_994b:  xor
  IL_994c:  call       void [System.Console]System.Console::Write(int32)
  IL_9951:  dup
  IL_9952:  ldc.i4.2
  IL_9953:  shl
  IL_9954:  call       void [System.Console]System.Console::Write(int32)
  IL_9959:  dup
  IL_995a:  ldc.i4.0
  IL_995b:  and
  IL_995c:  call       void [System.Console]System.Console::Write(int32)
  IL_9961:  dup
  IL_9962:  ldc.i4.1
  IL_9963:  mul
  IL_9964:  call       void [System.Console]System.Console::Write(int32)
  IL_9969:  dup
  IL_996a:  ldc.i4.8
  IL_996b:  shr
  IL_996c:  call       void [System.Console]System.Console::Write(int32)
  IL_9971:  dup
  IL_9972:  ldc.i4.5
  IL_9973:  add
  IL_9974:  call       void [System.Console]System.Console::Write(int32)
  IL_9979:  dup
  IL_997a:  ldc.i4.3
  IL_997b:  shr
  IL_997c:  call       void [System.Console]System.Console::Write(int32)
  IL_9981:  dup
  IL_9982:  ldc.i4.2
  IL_9983:  shl
  IL_9984:  call       void [System.Console]System.Console::Write(int32)
  IL_9989:  dup
  IL_998a:  ldc.i4.1
  IL_998b:  add
  IL_998c:  call       void [System.Console]System.Console::Write(int32)
  IL_9991:  dup
  IL_9992:  ldc.i4.2
  IL_9993:  shl
  IL_9994:  call       void [System.Console]System.Console::Write(int32)
  IL_9999:  dup
  IL_999a:  ldc.i4.1
  IL_999b:  shl
  IL_999c:  call       void [System.Console]System.Console::Write(int32)
  IL_99a1:  dup
  IL_99a2:  ldc.i4.2
  IL_99a3:  shl
  IL_99a4:  call       void [System.Console]System.Console::Write(int32)
  IL_99a9:  dup
  IL_99aa:  ldc.i4.2
  IL_99ab:  xor
  IL_99ac:  call       void [System.Console]System.Console::Write(int32)
  IL_99b1:  dup
  IL_99b2:  ldc.i4.0
  IL_99b3:  sub
  IL_99b4:  call       void [System.Console]System.Console::Write(int32)
  IL_99b9:  dup
  IL_99ba:  ldc.i4.5
  IL_99bb:  or
  IL_99bc:  call       void [System.Console]System.Console::Write(int32)
  IL_99c1:  dup
  IL_99c2:  ldc.i4.5
  IL_99c3:  or
  IL_99c4:  call       void [System.Console]System.Console::Write(int32)
  IL_99c9:  dup
  IL_99ca:  ldc.i4.0
  IL_99cb:  mul
  IL_99cc:  call       void [System.Console]System.Console::Write(int32)
  IL_99d1:  dup
  IL_99d2:  ldc.i4.3
  IL_99d3:  and
  IL_99d4:  call       void [System.Console]System.Console::Write(int32)
  IL_99d9:  dup
  IL_99da:  ldc.i4.2
  IL_99db:  mul
  IL_99dc:  call       void [System.Console]System.Console::Write(int32)
  IL_99e1:  dup
  IL_99e2:  ldc.i4.7
  IL_99e3:  and
  IL_99e4:  call       void [System.Console]System.Console::Write(int32)
  IL_99e9:  dup
  IL_99ea:  ldc.i4.1
  IL_99eb:  add
  IL_99ec:  call       void [System.Console]System.Console::Write(int32)
  IL_99f1:  dup
  IL_99f2:  ldc.i4.1
  IL_99f3:  and
  IL_99f4:  call       void [System.Console]System.Console::Write(int32)
  IL_99f9:  dup
  IL_99fa:  ldc.i4.6
  IL_99fb:  shl
  IL_99fc:  call       void [System.Console]System.Console::Write(int32)
  IL_9a01:  dup
  IL_9a02:  ldc.i4.1
  IL_9a03:  mul
  IL_9a04:  call       void [System.Console]System.Console::Write(int32)
  IL_9a09:  dup
  IL_9a0a:  ldc.i4.2
  IL_9a0b:  or
  IL_9a0c:  call       void [System.Console]System.Console::Write(int32)
  IL_9a11:  dup
  IL_9a12:  ldc.i4.4
  IL_9a13:  and
  IL_9a14:  call       void [System.Console]System.Console::Write(int32)
  IL_9a19:  dup
  IL_9a1a:  ldc.i4.2
  IL_9a1b:  or
  IL_9a1c:  call       void [System.Console]System.Console::Write(int32)
  IL_9a21:  dup
  IL_9a22:  ldc.i4.8
  IL_9a23:  shl
  IL_9a24:  call       void [System.Console]System.Console::Write(int32)
  IL_9a29:  dup
  IL_9a2a:  ldc.i4.1
  IL_9a2b:  shr
  IL_9a2c:  call       void [System.Console]System.Console::Write(int32)
  IL_9a31:  dup
  IL_9a32:  ldc.i4.2
  IL_9a33:  shl
  IL_9a34:  call       void [System.Console]System.Console::Write(int32)
  IL_9a39:  dup
  IL_9a3a:  ldc.i4.7
  IL_9a3b:  or
  IL_9a3c:  call       void [System.Console]System.Console::Write(int32)
  IL_9a41:  dup
  IL_9a42:  ldc.i4.5
  IL_9a43:  or
  IL_9a44:  call       void [System.Console]System.Console::Write(int32)
  IL_9a49:  dup
  IL_9a4a:  ldc.i4.1
  IL_9a4b:  and
  IL_9a4c:  call       void [System.Console]System.Console::Write(int32)
  IL_9a51:  dup
  IL_9a52:  ldc.i4.8
  IL_9a53:  add
  IL_9a54:  call       void [System.Console]System.Console::Write(int32)
  IL_9a59:  dup
  IL_9a5a:  ldc.i4.2
  IL_9a5b:  xor
  IL_9a5c:  call       void [System.Console]System.Console::Write(int32)
  IL_9a61:  dup
  IL_9a62:  ldc.i4.2
  IL_9a63:  or
  IL_9a64:  call       void [System.Console]System.Console::Write(int32)
  IL_9a69:  dup
  IL_9a6a:  ldc.i4.1
  IL_9a6b:  xor
  IL_9a6c:  call       void [System.Console]System.Console::Write(int32)
  IL_9a71:  dup
  IL_9a72:  ldc.i4.3
  IL_9a73:  sub
  IL_9a74:  call       void [System.Console]System.Console::Write(int32)
  IL_9a79:  dup
  IL_9a7a:  ldc.i4.2
  IL_9a7b:  xor
  IL_9a7c:  call       void [System.Console]System.Console::Write(int32)
  IL_9a81:  dup
  IL_9a82:  ldc.i4.7
  IL_9a83:  and
  IL_9a84:  call       void [System.Console]System.Console::Write(int32)
  IL_9a89:  dup
  IL_9a8a:  ldc.i4.2
  IL_9a8b:  and
  IL_9a8c:  call       void [System.Console]System.Console::Write(int32)
  IL_9a91:  dup
  IL_9a92:  ldc.i4.7
  IL_9a93:  shr
  IL_9a94:  call       void [System.Console]System.Console::Write(int32)
  IL_9a99:  dup
  IL_9a9a:  ldc.i4.4
  IL_9a9b:  shr
  IL_9a9c:  call       void [System.Console]System.Console::Write(int32)
  IL_9aa1:  dup
  IL_9aa2:  ldc.i4.5
  IL_9aa3:  add
  IL_9aa4:  call       void [System.Console]System.Console::Write(int32)
  IL_9aa9:  dup
  IL_9aaa:  ldc.i4.2
  IL_9aab:  shr
  IL_9aac:  call       void [System.Console]System.Console::Write(int32)
  IL_9ab1:  dup
  IL_9ab2:  ldc.i4.6
  IL_9ab3:  shl
  IL_9ab4:  call       void [System.Console]System.Console::Write(int32)
  IL_9ab9:  dup
  IL_9aba:  ldc.i4.8
  IL_9abb:  sub
  IL_9abc:  call       void [System.Console]System.Console::Write(int32)
  IL_9ac1:  dup
  IL_9ac2:  ldc.i4.0
  IL_9ac3:  sub
  IL_9ac4:  call       void [System.Console]System.Console::Write(int32)
  IL_9ac9:  dup
  IL_9aca:  ldc.i4.3
  IL_9acb:  mul
  IL_9acc:  call       void [System.Console]System.Console::Write(int32)
  IL_9ad1:  dup
  IL_9ad2:  ldc.i4.6
  IL_9ad3:  add
  IL_9ad4:  call       void [System.Console]System.Console::Write(int32)
  IL_9ad9:  dup
  IL_9ada:  ldc.i4.5
  IL_9adb:  add
  IL_9adc:  call       void [System.Console]System.Console::Write(int32)
  IL_9ae1:  dup
  IL_9ae2:  ldc.i4.8
  IL_9ae3:  or
  IL_9ae4:  call       void [System.Console]System.Console::Write(int32)
  IL_9ae9:  dup
  IL_9aea:  ldc.i4.6
  IL_9aeb:  shl
  IL_9aec:  call       void [System.Console]System.Console::Write(int32)
  IL_9af1:  dup
  IL_9af2:  ldc.i4.5
  IL_9af3:  and
  IL_9af4:  call       void [System.Console]System.Console::Write(int32)
  IL_9af9:  dup
  IL_9afa:  ldc.i4.4
  IL_9afb:  and
  IL_9afc:  call       void [System.Console]System.Console::Write(int32)
  IL_9b01:  dup
  IL_9b02:  ldc.i4.2
  IL_9b03:  shr
  IL_9b04:  call       void [System.Console]System.Console::Write(int32)
  IL_9b09:  dup
  IL_9b0a:  ldc.i4.7
  IL_9b0b:  shl
  IL_9b0c:  call       void [System.Console]System.Console::Write(int32)
  IL_9b11:  dup
  IL_9b12:  ldc.i4.2
  IL_9b13:  add
  IL_9b14:  call       void [System.Console]System.Console::Write(int32)
  IL_9b19:  dup
  IL_9b1a:  ldc.i4.1
  IL_9b1b:  or
  IL_9b1c:  call       void [System.Console]System.Console::Write(int32)
  IL_9b21:  dup
  IL_9b22:  ldc.i4.3
  IL_9b23:  xor
  IL_9b24:  call       void [System.Console]System.Console::Write(int32)
  IL_9b29:  dup
  IL_9b2a:  ldc.i4.1
  IL_9b2b:  and
  IL_9b2c:  call       void [System.Console]System.Console::Write(int32)
  IL_9b31:  dup
  IL_9b32:  ldc.i4.7
  IL_9b33:  shl
  IL_9b34:  call       void [System.Console]System.Console::Write(int32)
  IL_9b39:  dup
  IL_9b3a:  ldc.i4.6
  IL_9b3b:  shr
  IL_9b3c:  call       void [System.Console]System.Console::Write(int32)
  IL_9b41:  dup
  IL_9b42:  ldc.i4.8
  IL_9b43:  or
  IL_9b44:  call       void [System.Console]System.Console::Write(int32)
  IL_9b49:  dup
  IL_9b4a:  ldc.i4.4
  IL_9b4b:  mul
  IL_9b4c:  call       void [System.Console]System.Console::Write(int32)
  IL_9b51:  dup
  IL_9b52:  ldc.i4.7
  IL_9b53:  add
  IL_9b54:  call       void [System.Console]System.Console::Write(int32)
  IL_9b59:  dup
  IL_9b5a:  ldc.i4.6
  IL_9b5b:  and
  IL_9b5c:  call       void [System.Console]System.Console::Write(int32)
  IL_9b61:  dup
  IL_9b62:  ldc.i4.2
  IL_9b63:  sub
  IL_9b64:  call       void [System.Console]System.Console::Write(int32)
  IL_9b69:  dup
  IL_9b6a:  ldc.i4.7
  IL_9b6b:  or
  IL_9b6c:  call       void [System.Console]System.Console::Write(int32)
  IL_9b71:  dup
  IL_9b72:  ldc.i4.0
  IL_9b73:  or
  IL_9b74:  call       void [System.Console]System.Console::Write(int32)
  IL_9b79:  dup
  IL_9b7a:  ldc.i4.0
  IL_9b7b:  and
  IL_9b7c:  call       void [System.Console]System.Console::Write(int32)
  IL_9b81:  dup
  IL_9b82:  ldc.i4.5
  IL_9b83:  xor
  IL_9b84:  call       void [System.Console]System.Console::Write(int32)
  IL_9b89:  dup
  IL_9b8a:  ldc.i4.3
  IL_9b8b:  or
  IL_9b8c:  call       void [System.Console]System.Console::Write(int32)
  IL_9b91:  dup
  IL_9b92:  ldc.i4.7
  IL_9b93:  xor
  IL_9b94:  call       void [System.Console]System.Console::Write(int32)
  IL_9b99:  dup
  IL_9b9a:  ldc.i4.8
  IL_9b9b:  add
  IL_9b9c:  call       void [System.Console]System.Console::Write(int32)
  IL_9ba1:  dup
  IL_9ba2:  ldc.i4.0
  IL_9ba3:  xor
  IL_9ba4:  call       void [System.Console]System.Console::Write(int32)
  IL_9ba9:  dup
  IL_9baa:  ldc.i4.7
  IL_9bab:  mul
  IL_9bac:  call       void [System.Console]System.Console::Write(int32)
  IL_9bb1:  dup
  IL_9bb2:  ldc.i4.7
  IL_9bb3:  and
  IL_9bb4:  call       void [System.Console]System.Console::Write(int32)
  IL_9bb9:  dup
  IL_9bba:  ldc.i4.2
  IL_9bbb:  xor
  IL_9bbc:  call       void [System.Console]System.Console::Write(int32)
  IL_9bc1:  dup
  IL_9bc2:  ldc.i4.1
  IL_9bc3:  add
  IL_9bc4:  call       void [System.Console]System.Console::Write(int32)
  IL_9bc9:  dup
  IL_9bca:  ldc.i4.5
  IL_9bcb:  shr
  IL_9bcc:  call       void [System.Console]System.Console::Write(int32)
  IL_9bd1:  dup
  IL_9bd2:  ldc.i4.3
  IL_9bd3:  mul
  IL_9bd4:  call       void [System.Console]System.Console::Write(int32)
  IL_9bd9:  dup
  IL_9bda:  ldc.i4.8
  IL_9bdb:  mul
  IL_9bdc:  call       void [System.Console]System.Console::Write(int32)
  IL_9be1:  dup
  IL_9be2:  ldc.i4.8
  IL_9be3:  add
  IL_9be4:  call       void [System.Console]System.Console::Write(int32)
  IL_9be9:  dup
  IL_9bea:  ldc.i4.2
  IL_9beb:  add
  IL_9bec:  call       void [System.Console]System.Console::Write(int32)
  IL_9bf1:  dup
  IL_9bf2:  ldc.i4.2
  IL_9bf3:  mul
  IL_9bf4:  call       void [System.Console]System.Console::Write(int32)
  IL_9bf9:  dup
  IL_9bfa:  ldc.i4.8
  IL_9bfb:  xor
  IL_9bfc:  call       void [System.Console]System.Console::Write(int32)
  IL_9c01:  dup
  IL_9c02:  ldc.i4.2
  IL_9c03:  xor
  IL_9c04:  call       void [System.Console]System.Console::Write(int32)
  IL_9c09:  dup
  IL_9c0a:  ldc.i4.2
  IL_9c0b:  mul
  IL_9c0c:  call       void [System.Console]System.Console::Write(int32)
  IL_9c11:  dup
  IL_9c12:  ldc.i4.1
  IL_9c13:  and
  IL_9c14:  call       void [System.Console]System.Console::Write(int32)
  IL_9c19:  dup
  IL_9c1a:  ldc.i4.7
  IL_9c1b:  shl
  IL_9c1c:  call       void [System.Console]System.Console::Write(int32)
  IL_9c21:  dup
  IL_9c22:  ldc.i4.0
  IL_9c23:  xor
  IL_9c24:  call       void [System.Console]System.Console::Write(int32)
  IL_9c29:  dup
  IL_9c2a:  ldc.i4.2
  IL_9c2b:  xor
  IL_9c2c:  ldc.i4     0x1234
  IL_9c31:  add
  IL_9c32:  call       void [System.Console]System.Console::WriteLine(int32)
  IL_9c37:  ldc.i4     0x64
  IL_9c3c:  add
  IL_9c3d:  ret
}
