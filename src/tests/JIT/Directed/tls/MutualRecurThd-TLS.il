// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console { auto }
.assembly extern xunit.core {}
.assembly extern System.Threading.Thread { auto }
.assembly extern legacy library mscorlib { auto }


.assembly  'mutualrecurthd-tls'
{
}
.data tls FieldData = int32(32)
.data tls FieldData2 = int64(64)

.class public auto ansi Thread_EA
{
  .field public static int32 TLSFieldData at FieldData
  .method public instance void Run() il managed
  {
    .maxstack  3
    .locals (int32 i)

    IL_0000:  ldc.i4     0x1
stsfld	int32 Thread_EA::TLSFieldData
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0007:  ldc.i4     0x1
    IL_000c:  add
    IL_000d:  call       void ?Function1_1@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0013:  ldc.i4     0x1
    IL_0018:  add
    IL_0019:  call       void ?Function1_2@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_001f:  ldc.i4     0x1
    IL_0024:  add
    IL_0025:  call       void ?Function1_3@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_002b:  ldc.i4     0x1
    IL_0030:  add
    IL_0031:  call       void ?Function1_4@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0037:  ldc.i4     0x1
    IL_003c:  add
    IL_003d:  call       void ?Function1_5@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0043:  ldc.i4     0x1
    IL_0048:  add
    IL_0049:  call       void ?Function1_6@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_004f:  ldc.i4     0x1
    IL_0054:  add
    IL_0055:  call       void ?Function1_7@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_005b:  ldc.i4     0x1
    IL_0060:  add
    IL_0061:  call       void ?Function1_8@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0067:  ldc.i4     0x1
    IL_006c:  add
    IL_006d:  call       void ?Function1_9@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0073:  ldc.i4     0x1
    IL_0078:  add
    IL_0079:  call       void ?Function1_10@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_007f:  ldc.i4     0x1
    IL_0084:  add
    IL_0085:  call       void ?Function1_11@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_008b:  ldc.i4     0x1
    IL_0090:  add
    IL_0091:  call       void ?Function1_12@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0097:  ldc.i4     0x1
    IL_009c:  add
    IL_009d:  call       void ?Function1_13@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_00a3:  ldc.i4     0x1
    IL_00a8:  add
    IL_00a9:  call       void ?Function1_14@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_00af:  ldc.i4     0x1
    IL_00b4:  add
    IL_00b5:  call       void ?Function1_15@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_00bb:  ldc.i4     0x1
    IL_00c0:  add
    IL_00c1:  call       void ?Function1_16@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_00c7:  ldc.i4     0x1
    IL_00cc:  add
    IL_00cd:  call       void ?Function1_17@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_00d3:  ldc.i4     0x1
    IL_00d8:  add
    IL_00d9:  call       void ?Function1_18@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_00df:  ldc.i4     0x1
    IL_00e4:  add
    IL_00e5:  call       void ?Function1_19@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_00eb:  ldc.i4     0x1
    IL_00f0:  add
    IL_00f1:  call       void ?Function1_20@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_00f7:  ldc.i4     0x1
    IL_00fc:  add
    IL_00fd:  call       void ?Function1_21@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0103:  ldc.i4     0x1
    IL_0108:  add
    IL_0109:  call       void ?Function1_22@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_010f:  ldc.i4     0x1
    IL_0114:  add
    IL_0115:  call       void ?Function1_23@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_011b:  ldc.i4     0x1
    IL_0120:  add
    IL_0121:  call       void ?Function1_24@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0127:  ldc.i4     0x1
    IL_012c:  add
    IL_012d:  call       void ?Function1_25@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0133:  ldc.i4     0x1
    IL_0138:  add
    IL_0139:  call       void ?Function1_26@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_013f:  ldc.i4     0x1
    IL_0144:  add
    IL_0145:  call       void ?Function1_27@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_014b:  ldc.i4     0x1
    IL_0150:  add
    IL_0151:  call       void ?Function1_28@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0157:  ldc.i4     0x1
    IL_015c:  add
    IL_015d:  call       void ?Function1_29@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0163:  ldc.i4     0x1
    IL_0168:  add
    IL_0169:  call       void ?Function1_30@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_016f:  ldc.i4     0x1
    IL_0174:  add
    IL_0175:  call       void ?Function1_31@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_017b:  ldc.i4     0x1
    IL_0180:  add
    IL_0181:  call       void ?Function1_32@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0187:  ldc.i4     0x1
    IL_018c:  add
    IL_018d:  call       void ?Function1_33@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0193:  ldc.i4     0x1
    IL_0198:  add
    IL_0199:  call       void ?Function1_34@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_019f:  ldc.i4     0x1
    IL_01a4:  add
    IL_01a5:  call       void ?Function1_35@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_01ab:  ldc.i4     0x1
    IL_01b0:  add
    IL_01b1:  call       void ?Function1_36@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_01b7:  ldc.i4     0x1
    IL_01bc:  add
    IL_01bd:  call       void ?Function1_37@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_01c3:  ldc.i4     0x1
    IL_01c8:  add
    IL_01c9:  call       void ?Function1_38@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_01cf:  ldc.i4     0x1
    IL_01d4:  add
    IL_01d5:  call       void ?Function1_39@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_01db:  ldc.i4     0x1
    IL_01e0:  add
    IL_01e1:  call       void ?Function1_40@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_01e7:  ldc.i4     0x1
    IL_01ec:  add
    IL_01ed:  call       void ?Function1_41@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_01f3:  ldc.i4     0x1
    IL_01f8:  add
    IL_01f9:  call       void ?Function1_42@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_01ff:  ldc.i4     0x1
    IL_0204:  add
    IL_0205:  call       void ?Function1_43@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_020b:  ldc.i4     0x1
    IL_0210:  add
    IL_0211:  call       void ?Function1_44@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0217:  ldc.i4     0x1
    IL_021c:  add
    IL_021d:  call       void ?Function1_45@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0223:  ldc.i4     0x1
    IL_0228:  add
    IL_0229:  call       void ?Function1_46@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_022f:  ldc.i4     0x1
    IL_0234:  add
    IL_0235:  call       void ?Function1_47@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_023b:  ldc.i4     0x1
    IL_0240:  add
    IL_0241:  call       void ?Function1_48@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0247:  ldc.i4     0x1
    IL_024c:  add
    IL_024d:  call       void ?Function1_49@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0253:  ldc.i4     0x1
    IL_0258:  add
    IL_0259:  call       void ?Function1_50@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_025f:  ldc.i4     0x1
    IL_0264:  add
    IL_0265:  call       void ?Function1_51@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_026b:  ldc.i4     0x1
    IL_0270:  add
    IL_0271:  call       void ?Function1_52@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0277:  ldc.i4     0x1
    IL_027c:  add
    IL_027d:  call       void ?Function1_53@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0283:  ldc.i4     0x1
    IL_0288:  add
    IL_0289:  call       void ?Function1_54@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_028f:  ldc.i4     0x1
    IL_0294:  add
    IL_0295:  call       void ?Function1_55@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_029b:  ldc.i4     0x1
    IL_02a0:  add
    IL_02a1:  call       void ?Function1_56@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_02a7:  ldc.i4     0x1
    IL_02ac:  add
    IL_02ad:  call       void ?Function1_57@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_02b3:  ldc.i4     0x1
    IL_02b8:  add
    IL_02b9:  call       void ?Function1_58@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_02bf:  ldc.i4     0x1
    IL_02c4:  add
    IL_02c5:  call       void ?Function1_59@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_02cb:  ldc.i4     0x1
    IL_02d0:  add
    IL_02d1:  call       void ?Function1_60@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_02d7:  ldc.i4     0x1
    IL_02dc:  add
    IL_02dd:  call       void ?Function1_61@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_02e3:  ldc.i4     0x1
    IL_02e8:  add
    IL_02e9:  call       void ?Function1_62@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_02ef:  ldc.i4     0x1
    IL_02f4:  add
    IL_02f5:  call       void ?Function1_63@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_02fb:  ldc.i4     0x1
    IL_0300:  add
    IL_0301:  call       void ?Function1_64@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0307:  ldc.i4     0x1
    IL_030c:  add
    IL_030d:  call       void ?Function1_65@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0313:  ldc.i4     0x1
    IL_0318:  add
    IL_0319:  call       void ?Function1_66@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_031f:  ldc.i4     0x1
    IL_0324:  add
    IL_0325:  call       void ?Function1_67@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_032b:  ldc.i4     0x1
    IL_0330:  add
    IL_0331:  call       void ?Function1_68@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0337:  ldc.i4     0x1
    IL_033c:  add
    IL_033d:  call       void ?Function1_69@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0343:  ldc.i4     0x1
    IL_0348:  add
    IL_0349:  call       void ?Function1_70@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_034f:  ldc.i4     0x1
    IL_0354:  add
    IL_0355:  call       void ?Function1_1@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_035b:  ldc.i4     0x1
    IL_0360:  add
    IL_0361:  call       void ?Function1_2@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0367:  ldc.i4     0x1
    IL_036c:  add
    IL_036d:  call       void ?Function1_3@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0373:  ldc.i4     0x1
    IL_0378:  add
    IL_0379:  call       void ?Function1_4@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_037f:  ldc.i4     0x1
    IL_0384:  add
    IL_0385:  call       void ?Function1_5@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_038b:  ldc.i4     0x1
    IL_0390:  add
    IL_0391:  call       void ?Function1_6@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0397:  ldc.i4     0x1
    IL_039c:  add
    IL_039d:  call       void ?Function1_7@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_03a3:  ldc.i4     0x1
    IL_03a8:  add
    IL_03a9:  call       void ?Function1_8@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_03af:  ldc.i4     0x1
    IL_03b4:  add
    IL_03b5:  call       void ?Function1_9@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_03bb:  ldc.i4     0x1
    IL_03c0:  add
    IL_03c1:  call       void ?Function1_10@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_03c7:  ldc.i4     0x1
    IL_03cc:  add
    IL_03cd:  call       void ?Function1_11@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_03d3:  ldc.i4     0x1
    IL_03d8:  add
    IL_03d9:  call       void ?Function1_12@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_03df:  ldc.i4     0x1
    IL_03e4:  add
    IL_03e5:  call       void ?Function1_13@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_03eb:  ldc.i4     0x1
    IL_03f0:  add
    IL_03f1:  call       void ?Function1_14@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_03f7:  ldc.i4     0x1
    IL_03fc:  add
    IL_03fd:  call       void ?Function1_15@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0403:  ldc.i4     0x1
    IL_0408:  add
    IL_0409:  call       void ?Function1_16@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_040f:  ldc.i4     0x1
    IL_0414:  add
    IL_0415:  call       void ?Function1_17@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_041b:  ldc.i4     0x1
    IL_0420:  add
    IL_0421:  call       void ?Function1_18@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0427:  ldc.i4     0x1
    IL_042c:  add
    IL_042d:  call       void ?Function1_19@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0433:  ldc.i4     0x1
    IL_0438:  add
    IL_0439:  call       void ?Function1_20@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_043f:  ldc.i4     0x1
    IL_0444:  add
    IL_0445:  call       void ?Function1_21@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_044b:  ldc.i4     0x1
    IL_0450:  add
    IL_0451:  call       void ?Function1_22@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0457:  ldc.i4     0x1
    IL_045c:  add
    IL_045d:  call       void ?Function1_23@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0463:  ldc.i4     0x1
    IL_0468:  add
    IL_0469:  call       void ?Function1_24@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_046f:  ldc.i4     0x1
    IL_0474:  add
    IL_0475:  call       void ?Function1_25@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_047b:  ldc.i4     0x1
    IL_0480:  add
    IL_0481:  call       void ?Function1_26@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0487:  ldc.i4     0x1
    IL_048c:  add
    IL_048d:  call       void ?Function1_27@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_0493:  ldc.i4     0x1
    IL_0498:  add
    IL_0499:  call       void ?Function1_28@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_049f:  ldc.i4     0x1
    IL_04a4:  add
    IL_04a5:  call       void ?Function1_29@@YAXH@Z(int32)
ldsfld	int32 Thread_EA::TLSFieldData
dup
ldc.i4 0x1
add
stsfld	int32 Thread_EA::TLSFieldData
    IL_04ab:  ldc.i4     0x1
    IL_04b0:  add
    IL_04b1:  call       void ?Function1_30@@YAXH@Z(int32)
    IL_04b6:  	call	    class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
    IL_04bb:  ldstr "one thread finished"
    IL_04c0:
    IL_04c5:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(class [mscorlib]System.String)
    IL_04ca:  ret
  }

  .method public specialname rtspecialname instance void .ctor() il managed
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

.class value sealed public explicit ansi $MultiByte$26
{
  .pack 1
  .size 26
}

.class value sealed public explicit ansi $MultiByte$54
{
  .pack 1
  .size 54
}

.class value sealed public explicit ansi $MultiByte$0
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
  IL_0037:  ldarg.0
  IL_0038:  call       void ?Function1_1@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_2@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0
  IL_0038:  call       void ?Function1_3@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_4@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_5@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_6@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_7@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_8@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_9@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_10@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_11@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_12@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_13@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_14@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_15@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_16@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_17@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_18@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_19@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_20@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_21@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_22@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_23@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_24@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_25@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_26@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_27@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_28@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_29@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_30@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_31@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_32@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_33@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_34@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_35@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_36@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_37@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_38@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_39@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_40@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_41@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_42@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_43@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_44@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_45@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_46@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_47@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_48@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_49@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_50@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_51@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_52@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_53@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_54@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_55@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_56@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_57@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_58@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_59@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_60@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_61@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_62@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_63@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_64@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_65@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_66@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_67@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_68@@YAXH@Z(int32)
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
  IL_0037:  ldarg.0

  IL_0038:  call       void ?Function1_69@@YAXH@Z(int32)
  IL_003d:  ret
}

.method public static int32 main() il managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  .maxstack  4
  .locals (class [System.Threading.Thread]System.Threading.Thread[] thrd,
           class Thread_EA ThdObj,
           int32 V_2)

    call	    class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
call       class [mscorlib]System.IO.TextWriter [mscorlib]System.IO.TextWriter::Synchronized(class [mscorlib]System.IO.TextWriter)
call       void [System.Console]System.Console::SetOut(class [mscorlib]System.IO.TextWriter)

  IL_0000:  ldc.i4     0xa
  IL_0005:  newarr     [System.Threading.Thread]System.Threading.Thread
  IL_000a:  stloc.0
  IL_000b:  newobj     instance void Thread_EA::.ctor()
  IL_0010:  stloc.1
  IL_0011: 	call	    class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
  IL_0016:  ldstr "test started"
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

  IL_008f: 	call	    class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
  IL_0094:  ldstr "Mututalrecurthd test passed"
  IL_0099:
  IL_009e:  callvirt   instance void [mscorlib]System.IO.TextWriter::WriteLine(class [mscorlib]System.String)
  IL_00a3:  ldc.i4     0x64
  IL_00a8:  ret
}
