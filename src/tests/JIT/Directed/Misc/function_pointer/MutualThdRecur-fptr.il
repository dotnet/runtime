// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console { auto }
.assembly extern xunit.core {}
.assembly extern System.Threading.Thread { auto }
.assembly extern legacy library mscorlib { auto }

.assembly 'MutualThdRecur-fptr'
{
}

.class public auto ansi Thread_EA
{
  .method public static void Dispatch(method explicit instance void *(int32), int32)
  {
    .maxstack  8
    ldarg 1
    ldarg 0
    calli void (int32)
    ret
  }

  .method public instance void Run() il managed
  {
    .maxstack  3
    .locals (int32 i)

    IL_0000:  ldc.i4     0x1
    IL_0005:  stloc.0
              ldftn      void ?Function1_1@@YAXH@Z(int32)
    IL_0006:  ldloc.0
    IL_0007:  ldc.i4     0x1
    IL_000c:  add
     call       void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0019:  ldftn      void ?Function1_2@@YAXH@Z(int32)
    IL_0012:  ldloc.0
    IL_0013:  ldc.i4     0x1
    IL_0018:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0025:  ldftn      void ?Function1_3@@YAXH@Z(int32)
    IL_001e:  ldloc.0
    IL_001f:  ldc.i4     0x1
    IL_0024:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0031:  ldftn      void ?Function1_4@@YAXH@Z(int32)
    IL_002a:  ldloc.0
    IL_002b:  ldc.i4     0x1
    IL_0030:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_003d:  ldftn      void ?Function1_5@@YAXH@Z(int32)
    IL_0036:  ldloc.0
    IL_0037:  ldc.i4     0x1
    IL_003c:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0049:  ldftn      void ?Function1_6@@YAXH@Z(int32)
    IL_0042:  ldloc.0
    IL_0043:  ldc.i4     0x1
    IL_0048:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0055:  ldftn      void ?Function1_7@@YAXH@Z(int32)
    IL_004e:  ldloc.0
    IL_004f:  ldc.i4     0x1
    IL_0054:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0061:  ldftn      void ?Function1_8@@YAXH@Z(int32)
    IL_005a:  ldloc.0
    IL_005b:  ldc.i4     0x1
    IL_0060:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_006d:  ldftn      void ?Function1_9@@YAXH@Z(int32)
    IL_0066:  ldloc.0
    IL_0067:  ldc.i4     0x1
    IL_006c:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0079:  ldftn      void ?Function1_10@@YAXH@Z(int32)
    IL_0072:  ldloc.0
    IL_0073:  ldc.i4     0x1
    IL_0078:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0085:  ldftn      void ?Function1_11@@YAXH@Z(int32)
    IL_007e:  ldloc.0
    IL_007f:  ldc.i4     0x1
    IL_0084:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0091:  ldftn      void ?Function1_12@@YAXH@Z(int32)
    IL_008a:  ldloc.0
    IL_008b:  ldc.i4     0x1
    IL_0090:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_009d:  ldftn      void ?Function1_13@@YAXH@Z(int32)
    IL_0096:  ldloc.0
    IL_0097:  ldc.i4     0x1
    IL_009c:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_00a9:  ldftn      void ?Function1_14@@YAXH@Z(int32)
    IL_00a2:  ldloc.0
    IL_00a3:  ldc.i4     0x1
    IL_00a8:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_00b5:  ldftn      void ?Function1_15@@YAXH@Z(int32)
    IL_00ae:  ldloc.0
    IL_00af:  ldc.i4     0x1
    IL_00b4:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_00c1:  ldftn      void ?Function1_16@@YAXH@Z(int32)
    IL_00ba:  ldloc.0
    IL_00bb:  ldc.i4     0x1
    IL_00c0:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_00cd:  ldftn      void ?Function1_17@@YAXH@Z(int32)
    IL_00c6:  ldloc.0
    IL_00c7:  ldc.i4     0x1
    IL_00cc:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_00d9:  ldftn      void ?Function1_18@@YAXH@Z(int32)
    IL_00d2:  ldloc.0
    IL_00d3:  ldc.i4     0x1
    IL_00d8:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_00e5:  ldftn      void ?Function1_19@@YAXH@Z(int32)
    IL_00de:  ldloc.0
    IL_00df:  ldc.i4     0x1
    IL_00e4:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_00f1:  ldftn      void ?Function1_20@@YAXH@Z(int32)
    IL_00ea:  ldloc.0
    IL_00eb:  ldc.i4     0x1
    IL_00f0:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_00fd:  ldftn      void ?Function1_21@@YAXH@Z(int32)
    IL_00f6:  ldloc.0
    IL_00f7:  ldc.i4     0x1
    IL_00fc:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0109:  ldftn      void ?Function1_22@@YAXH@Z(int32)
    IL_0102:  ldloc.0
    IL_0103:  ldc.i4     0x1
    IL_0108:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0115:  ldftn      void ?Function1_23@@YAXH@Z(int32)
    IL_010e:  ldloc.0
    IL_010f:  ldc.i4     0x1
    IL_0114:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0121:  ldftn      void ?Function1_24@@YAXH@Z(int32)
    IL_011a:  ldloc.0
    IL_011b:  ldc.i4     0x1
    IL_0120:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_012d:  ldftn      void ?Function1_25@@YAXH@Z(int32)
    IL_0126:  ldloc.0
    IL_0127:  ldc.i4     0x1
    IL_012c:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0139:  ldftn      void ?Function1_26@@YAXH@Z(int32)
    IL_0132:  ldloc.0
    IL_0133:  ldc.i4     0x1
    IL_0138:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0145:  ldftn      void ?Function1_27@@YAXH@Z(int32)
    IL_013e:  ldloc.0
    IL_013f:  ldc.i4     0x1
    IL_0144:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0151:  ldftn      void ?Function1_28@@YAXH@Z(int32)
    IL_014a:  ldloc.0
    IL_014b:  ldc.i4     0x1
    IL_0150:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_015d:  ldftn      void ?Function1_29@@YAXH@Z(int32)
    IL_0156:  ldloc.0
    IL_0157:  ldc.i4     0x1
    IL_015c:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0169:  ldftn      void ?Function1_30@@YAXH@Z(int32)
    IL_0162:  ldloc.0
    IL_0163:  ldc.i4     0x1
    IL_0168:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0175:  ldftn      void ?Function1_31@@YAXH@Z(int32)
    IL_016e:  ldloc.0
    IL_016f:  ldc.i4     0x1
    IL_0174:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0181:  ldftn      void ?Function1_32@@YAXH@Z(int32)
    IL_017a:  ldloc.0
    IL_017b:  ldc.i4     0x1
    IL_0180:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_018d:  ldftn      void ?Function1_33@@YAXH@Z(int32)
    IL_0186:  ldloc.0
    IL_0187:  ldc.i4     0x1
    IL_018c:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0199:  ldftn      void ?Function1_34@@YAXH@Z(int32)
    IL_0192:  ldloc.0
    IL_0193:  ldc.i4     0x1
    IL_0198:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_01a5:  ldftn      void ?Function1_35@@YAXH@Z(int32)
    IL_019e:  ldloc.0
    IL_019f:  ldc.i4     0x1
    IL_01a4:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_01b1:  ldftn      void ?Function1_36@@YAXH@Z(int32)
    IL_01aa:  ldloc.0
    IL_01ab:  ldc.i4     0x1
    IL_01b0:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_01bd:  ldftn      void ?Function1_37@@YAXH@Z(int32)
    IL_01b6:  ldloc.0
    IL_01b7:  ldc.i4     0x1
    IL_01bc:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_01c9:  ldftn      void ?Function1_38@@YAXH@Z(int32)
    IL_01c2:  ldloc.0
    IL_01c3:  ldc.i4     0x1
    IL_01c8:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_01d5:  ldftn      void ?Function1_39@@YAXH@Z(int32)
    IL_01ce:  ldloc.0
    IL_01cf:  ldc.i4     0x1
    IL_01d4:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_01e1:  ldftn      void ?Function1_40@@YAXH@Z(int32)
    IL_01da:  ldloc.0
    IL_01db:  ldc.i4     0x1
    IL_01e0:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_01ed:  ldftn      void ?Function1_41@@YAXH@Z(int32)
    IL_01e6:  ldloc.0
    IL_01e7:  ldc.i4     0x1
    IL_01ec:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_01f9:  ldftn      void ?Function1_42@@YAXH@Z(int32)
    IL_01f2:  ldloc.0
    IL_01f3:  ldc.i4     0x1
    IL_01f8:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0205:  ldftn      void ?Function1_43@@YAXH@Z(int32)
    IL_01fe:  ldloc.0
    IL_01ff:  ldc.i4     0x1
    IL_0204:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0211:  ldftn      void ?Function1_44@@YAXH@Z(int32)
    IL_020a:  ldloc.0
    IL_020b:  ldc.i4     0x1
    IL_0210:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_021d:  ldftn      void ?Function1_45@@YAXH@Z(int32)
    IL_0216:  ldloc.0
    IL_0217:  ldc.i4     0x1
    IL_021c:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0229:  ldftn      void ?Function1_46@@YAXH@Z(int32)
    IL_0222:  ldloc.0
    IL_0223:  ldc.i4     0x1
    IL_0228:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0235:  ldftn      void ?Function1_47@@YAXH@Z(int32)
    IL_022e:  ldloc.0
    IL_022f:  ldc.i4     0x1
    IL_0234:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0241:  ldftn      void ?Function1_48@@YAXH@Z(int32)
    IL_023a:  ldloc.0
    IL_023b:  ldc.i4     0x1
    IL_0240:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_024d:  ldftn      void ?Function1_49@@YAXH@Z(int32)
    IL_0246:  ldloc.0
    IL_0247:  ldc.i4     0x1
    IL_024c:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0259:  ldftn      void ?Function1_50@@YAXH@Z(int32)
    IL_0252:  ldloc.0
    IL_0253:  ldc.i4     0x1
    IL_0258:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0265:  ldftn      void ?Function1_51@@YAXH@Z(int32)
    IL_025e:  ldloc.0
    IL_025f:  ldc.i4     0x1
    IL_0264:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0271:  ldftn      void ?Function1_52@@YAXH@Z(int32)
    IL_026a:  ldloc.0
    IL_026b:  ldc.i4     0x1
    IL_0270:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_027d:  ldftn      void ?Function1_53@@YAXH@Z(int32)
    IL_0276:  ldloc.0
    IL_0277:  ldc.i4     0x1
    IL_027c:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0289:  ldftn      void ?Function1_54@@YAXH@Z(int32)
    IL_0282:  ldloc.0
    IL_0283:  ldc.i4     0x1
    IL_0288:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0295:  ldftn      void ?Function1_55@@YAXH@Z(int32)
    IL_028e:  ldloc.0
    IL_028f:  ldc.i4     0x1
    IL_0294:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_02a1:  ldftn      void ?Function1_56@@YAXH@Z(int32)
    IL_029a:  ldloc.0
    IL_029b:  ldc.i4     0x1
    IL_02a0:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_02ad:  ldftn      void ?Function1_57@@YAXH@Z(int32)
    IL_02a6:  ldloc.0
    IL_02a7:  ldc.i4     0x1
    IL_02ac:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_02b9:  ldftn      void ?Function1_58@@YAXH@Z(int32)
    IL_02b2:  ldloc.0
    IL_02b3:  ldc.i4     0x1
    IL_02b8:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_02c5:  ldftn      void ?Function1_59@@YAXH@Z(int32)
    IL_02be:  ldloc.0
    IL_02bf:  ldc.i4     0x1
    IL_02c4:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_02d1:  ldftn      void ?Function1_60@@YAXH@Z(int32)
    IL_02ca:  ldloc.0
    IL_02cb:  ldc.i4     0x1
    IL_02d0:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_02dd:  ldftn      void ?Function1_61@@YAXH@Z(int32)
    IL_02d6:  ldloc.0
    IL_02d7:  ldc.i4     0x1
    IL_02dc:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_02e9:  ldftn      void ?Function1_62@@YAXH@Z(int32)
    IL_02e2:  ldloc.0
    IL_02e3:  ldc.i4     0x1
    IL_02e8:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_02f5:  ldftn      void ?Function1_63@@YAXH@Z(int32)
    IL_02ee:  ldloc.0
    IL_02ef:  ldc.i4     0x1
    IL_02f4:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0301:  ldftn      void ?Function1_64@@YAXH@Z(int32)
    IL_02fa:  ldloc.0
    IL_02fb:  ldc.i4     0x1
    IL_0300:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_030d:  ldftn      void ?Function1_65@@YAXH@Z(int32)
    IL_0306:  ldloc.0
    IL_0307:  ldc.i4     0x1
    IL_030c:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0319:  ldftn      void ?Function1_66@@YAXH@Z(int32)
    IL_0312:  ldloc.0
    IL_0313:  ldc.i4     0x1
    IL_0318:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0325:  ldftn      void ?Function1_67@@YAXH@Z(int32)
    IL_031e:  ldloc.0
    IL_031f:  ldc.i4     0x1
    IL_0324:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0331:  ldftn      void ?Function1_68@@YAXH@Z(int32)
    IL_032a:  ldloc.0
    IL_032b:  ldc.i4     0x1
    IL_0330:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_033d:  ldftn      void ?Function1_69@@YAXH@Z(int32)
    IL_0336:  ldloc.0
    IL_0337:  ldc.i4     0x1
    IL_033c:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0349:  ldftn      void ?Function1_70@@YAXH@Z(int32)
    IL_0342:  ldloc.0
    IL_0343:  ldc.i4     0x1
    IL_0348:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0355:  ldftn      void ?Function1_1@@YAXH@Z(int32)
    IL_034e:  ldloc.0
    IL_034f:  ldc.i4     0x1
    IL_0354:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0361:  ldftn      void ?Function1_2@@YAXH@Z(int32)
    IL_035a:  ldloc.0
    IL_035b:  ldc.i4     0x1
    IL_0360:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_036d:  ldftn      void ?Function1_3@@YAXH@Z(int32)
    IL_0366:  ldloc.0
    IL_0367:  ldc.i4     0x1
    IL_036c:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0379:  ldftn      void ?Function1_4@@YAXH@Z(int32)
    IL_0372:  ldloc.0
    IL_0373:  ldc.i4     0x1
    IL_0378:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0385:  ldftn      void ?Function1_5@@YAXH@Z(int32)
    IL_037e:  ldloc.0
    IL_037f:  ldc.i4     0x1
    IL_0384:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0391:  ldftn      void ?Function1_6@@YAXH@Z(int32)
    IL_038a:  ldloc.0
    IL_038b:  ldc.i4     0x1
    IL_0390:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_039d:  ldftn      void ?Function1_7@@YAXH@Z(int32)
    IL_0396:  ldloc.0
    IL_0397:  ldc.i4     0x1
    IL_039c:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_03a9:  ldftn      void ?Function1_8@@YAXH@Z(int32)
    IL_03a2:  ldloc.0
    IL_03a3:  ldc.i4     0x1
    IL_03a8:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_03b5:  ldftn      void ?Function1_9@@YAXH@Z(int32)
    IL_03ae:  ldloc.0
    IL_03af:  ldc.i4     0x1
    IL_03b4:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_03c1:  ldftn      void ?Function1_10@@YAXH@Z(int32)
    IL_03ba:  ldloc.0
    IL_03bb:  ldc.i4     0x1
    IL_03c0:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_03cd:  ldftn      void ?Function1_11@@YAXH@Z(int32)
    IL_03c6:  ldloc.0
    IL_03c7:  ldc.i4     0x1
    IL_03cc:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_03d9:  ldftn      void ?Function1_12@@YAXH@Z(int32)
    IL_03d2:  ldloc.0
    IL_03d3:  ldc.i4     0x1
    IL_03d8:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_03e5:  ldftn      void ?Function1_13@@YAXH@Z(int32)
    IL_03de:  ldloc.0
    IL_03df:  ldc.i4     0x1
    IL_03e4:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_03f1:  ldftn      void ?Function1_14@@YAXH@Z(int32)
    IL_03ea:  ldloc.0
    IL_03eb:  ldc.i4     0x1
    IL_03f0:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_03fd:  ldftn      void ?Function1_15@@YAXH@Z(int32)
    IL_03f6:  ldloc.0
    IL_03f7:  ldc.i4     0x1
    IL_03fc:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0409:  ldftn      void ?Function1_16@@YAXH@Z(int32)
    IL_0402:  ldloc.0
    IL_0403:  ldc.i4     0x1
    IL_0408:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0415:  ldftn      void ?Function1_17@@YAXH@Z(int32)
    IL_040e:  ldloc.0
    IL_040f:  ldc.i4     0x1
    IL_0414:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0421:  ldftn      void ?Function1_18@@YAXH@Z(int32)
    IL_041a:  ldloc.0
    IL_041b:  ldc.i4     0x1
    IL_0420:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_042d:  ldftn      void ?Function1_19@@YAXH@Z(int32)
    IL_0426:  ldloc.0
    IL_0427:  ldc.i4     0x1
    IL_042c:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0439:  ldftn      void ?Function1_20@@YAXH@Z(int32)
    IL_0432:  ldloc.0
    IL_0433:  ldc.i4     0x1
    IL_0438:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0445:  ldftn      void ?Function1_21@@YAXH@Z(int32)
    IL_043e:  ldloc.0
    IL_043f:  ldc.i4     0x1
    IL_0444:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0451:  ldftn      void ?Function1_22@@YAXH@Z(int32)
    IL_044a:  ldloc.0
    IL_044b:  ldc.i4     0x1
    IL_0450:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_045d:  ldftn      void ?Function1_23@@YAXH@Z(int32)
    IL_0456:  ldloc.0
    IL_0457:  ldc.i4     0x1
    IL_045c:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0469:  ldftn      void ?Function1_24@@YAXH@Z(int32)
    IL_0462:  ldloc.0
    IL_0463:  ldc.i4     0x1
    IL_0468:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0475:  ldftn      void ?Function1_25@@YAXH@Z(int32)
    IL_046e:  ldloc.0
    IL_046f:  ldc.i4     0x1
    IL_0474:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0481:  ldftn      void ?Function1_26@@YAXH@Z(int32)
    IL_047a:  ldloc.0
    IL_047b:  ldc.i4     0x1
    IL_0480:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_048d:  ldftn      void ?Function1_27@@YAXH@Z(int32)
    IL_0486:  ldloc.0
    IL_0487:  ldc.i4     0x1
    IL_048c:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_0499:  ldftn      void ?Function1_28@@YAXH@Z(int32)
    IL_0492:  ldloc.0
    IL_0493:  ldc.i4     0x1
    IL_0498:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_04a5:  ldftn      void ?Function1_29@@YAXH@Z(int32)
    IL_049e:  ldloc.0
    IL_049f:  ldc.i4     0x1
    IL_04a4:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_04b1:  ldftn      void ?Function1_30@@YAXH@Z(int32)
    IL_04aa:  ldloc.0
    IL_04ab:  ldc.i4     0x1
    IL_04b0:  add
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
    IL_04b6:    call        class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
    IL_04bb:  ldstr "on thread finished"
    IL_04c0:

    IL_04c5:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(class [mscorlib]System.String)
    IL_04ca:  ret
  }

  .method public specialname rtspecialname  instance void .ctor() il managed
  {
    .maxstack  1

    IL_0000:  ldarg.0

    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }

}

.class value sealed public explicit ansi $MultiByte$42
{
  .pack 1
  .size 42
}

.class value sealed public explicit  ansi $MultiByte$26
{
  .pack 1
  .size 26
}

.class value sealed public explicit  ansi $MultiByte$54
{
  .pack 1
  .size 54
}

.class value sealed public explicit  ansi $MultiByte$0
{
  .pack 1
  .size 0
}

.method public static void ?Function1_1@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0037:  ret
}

.method public static void ?Function1_2@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_1@@YAXH@Z(int32)
  IL_0037:  ldarg.0
  call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_3@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_2@@YAXH@Z(int32)
  IL_0037:  ldarg.0

call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_4@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_3@@YAXH@Z(int32)
  IL_0037:  ldarg.0

call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_5@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_4@@YAXH@Z(int32)
  IL_0037:  ldarg.0

call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_6@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_5@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_7@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_6@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_8@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_7@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_9@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_8@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_10@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_9@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_11@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_10@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_12@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_11@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_13@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_12@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_14@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_13@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_15@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_14@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_16@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_15@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_17@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_16@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_18@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_17@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_19@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_18@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_20@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_19@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_21@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_20@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_22@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_21@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_23@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_22@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_24@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_23@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_25@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_24@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_26@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_25@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_27@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_26@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_28@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_27@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_29@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_28@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_30@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_29@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_31@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_30@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_32@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_31@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_33@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_32@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_34@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_33@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_35@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_34@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_36@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_35@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_37@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_36@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_38@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0

  IL_0038:  ldftn      void ?Function1_37@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_39@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_38@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_40@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_39@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_41@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_40@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_42@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_41@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_43@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_42@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_44@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_43@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_45@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_44@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_46@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_45@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_47@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_46@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_48@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_47@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_49@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_48@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_50@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_49@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_51@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_50@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_52@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_51@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_53@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_52@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_54@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_53@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_55@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_54@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_56@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_55@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_57@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_56@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_58@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_57@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_59@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_58@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_60@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_59@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_61@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_60@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_62@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_61@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_63@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_62@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_64@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_63@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_65@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_64@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_66@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_65@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_67@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_66@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_68@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_67@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_69@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_68@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.method public static void ?Function1_70@@YAXH@Z(int32 A_0) il managed
{
  .maxstack  4
  .locals (unsigned int8[] mem)

  IL_0000:  ldarg.0
  IL_0001:  newarr     [mscorlib]System.Byte
  IL_0006:  stloc.0
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4     0x0
  IL_000d:  ble        IL_002e

  IL_0012:  ldloc.0
  IL_0013:  ldc.i4     0x0
  IL_0018:  ldc.i4     0x0
  IL_001d:  stelem.i1
  IL_001e:  ldloc.0
  IL_001f:  ldarg.0
  IL_0020:  ldc.i4     0x1
  IL_0025:  sub
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4     0xff
  IL_002c:  and
  IL_002d:  stelem.i1
  IL_002e:  ldarg.0
  IL_002f:  ldc.i4     0x1
  IL_0034:  add
  IL_0035:  starg.s    A_0
  IL_0038:  ldftn      void ?Function1_69@@YAXH@Z(int32)
  IL_0037:  ldarg.0
call void Thread_EA::Dispatch(method explicit instance void *(int32), int32)
  IL_003d:  ret
}

.class public auto ansi MutualThdRecur_fptr {
.method public static int32 main() il managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .maxstack  4
  .entrypoint
  .locals (class [System.Threading.Thread]System.Threading.Thread[] thrd,
           class Thread_EA ThdObj,
           int32 V_2)

  IL_0000:  ldc.i4     0xa
  IL_0005:  newarr     [System.Threading.Thread]System.Threading.Thread
  IL_000a:  stloc.0
  IL_000b:  newobj     instance void Thread_EA::.ctor()
  IL_0010:  stloc.1
  IL_0011:  call       class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
  IL_0016:  ldstr      "test started"
  IL_001b:
  IL_0020:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(class [mscorlib]System.String)
  IL_0025:  ldc.i4     0x0
  IL_002a:  stloc.2
  IL_002b:  br         IL_0038

  IL_0030:  ldloc.2
  IL_0031:  ldc.i4     0x1
  IL_0036:  add
  IL_0037:  stloc.2
  IL_0038:  ldloc.2
  IL_0039:  ldc.i4     0xa
  IL_003e:  bge        IL_0064

  IL_0043:  ldloc.0
  IL_0044:  ldloc.2
  IL_0045:  ldloc.1
  IL_0046:  ldftn      instance void Thread_EA::Run()
  IL_004c:  newobj     instance void [System.Threading.Thread]System.Threading.ThreadStart::.ctor(class [mscorlib]System.Object,native int)
  IL_0051:  newobj     instance void [System.Threading.Thread]System.Threading.Thread::.ctor(class [System.Threading.Thread]System.Threading.ThreadStart)
  IL_0056:  stelem.ref
  IL_0057:  ldloc.0
  IL_0058:  ldloc.2
  IL_0059:  ldelem.ref
  IL_005a:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Start()
  IL_005f:  br         IL_0030

  IL_0064:  ldc.i4     0x0
  IL_0069:  stloc.2
  IL_006a:  br         IL_0077

  IL_006f:  ldloc.2
  IL_0070:  ldc.i4     0x1
  IL_0075:  add
  IL_0076:  stloc.2
  IL_0077:  ldloc.2
  IL_0078:  ldc.i4     0xa
  IL_007d:  bge        IL_008f

  IL_0082:  ldloc.0
  IL_0083:  ldloc.2
  IL_0084:  ldelem.ref
  IL_0085:  callvirt   instance void [System.Threading.Thread]System.Threading.Thread::Join()
  IL_008a:  br         IL_006f

  IL_008f:  call       class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
  IL_0094:  ldstr      "mutualthdrecur-fptr done"
  IL_0099:
  IL_009e:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(class [mscorlib]System.String)
  IL_00a3:  ldc.i4     0x64
  IL_00a8:  ret
}
}