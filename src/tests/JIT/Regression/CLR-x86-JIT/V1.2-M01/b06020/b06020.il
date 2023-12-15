// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b06020 {}
.assembly extern xunit.core {}
.class public auto ansi sealed jb
       extends [mscorlib]System.Object
{
  .field privatescope int32 a$PST04000001
  .field privatescope int32 b$PST04000002
  .field privatescope class jb c$PST04000003
  .field privatescope class jb d$PST04000004
  .method public specialname rtspecialname 
          instance void  .ctor(int32 A_0,
                               int32 A_1,
                               class jb A_2,
                               class jb A_3) cil managed
  {
    // Code size       36 (0x24)
    .maxstack  5
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      int32 jb::a$PST04000001
    IL_000d:  ldarg.0
    IL_000e:  ldarg.2
    IL_000f:  stfld      int32 jb::b$PST04000002
    IL_0014:  ldarg.0
    IL_0015:  ldarg.3
    IL_0016:  stfld      class jb jb::c$PST04000003
    IL_001b:  ldarg.0
    IL_001c:  ldarg.s    A_3
    IL_001e:  stfld      class jb jb::d$PST04000004
    IL_0023:  ret
  } // end of method jb::.ctor

} // end of class jb

.class public auto ansi sealed ks
       extends [mscorlib]System.Object
{
  .field privatescope int32 a$PST04000005
  .field privatescope class jb b$PST04000006
  .field privatescope class jb c$PST04000007
  .method public specialname rtspecialname 
          instance void  .ctor(int32 A_0,
                               class jb A_1,
                               class jb A_2) cil managed
  {
    // Code size       28 (0x1c)
    .maxstack  4
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      int32 ks::a$PST04000005
    IL_000d:  ldarg.0
    IL_000e:  ldarg.2
    IL_000f:  stfld      class jb ks::b$PST04000006
    IL_0014:  ldarg.0
    IL_0015:  ldarg.3
    IL_0016:  stfld      class jb ks::c$PST04000007
    IL_001b:  ret
  } // end of method ks::.ctor

} // end of class ks

.class public auto ansi x
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method x::.ctor

  .method public virtual instance class jb 
          aN(class ks A_0) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  1
    IL_0000:  ldnull
    IL_0001:  throw
  } // end of method x::aN

  .method public virtual instance class jb 
          aO(class ks A_0) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  1
    IL_0000:  ldnull
    IL_0001:  throw
  } // end of method x::aO

  .method public virtual instance class jb 
          aP(class ks A_0) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  1
    IL_0000:  ldnull
    IL_0001:  throw
  } // end of method x::aP

  .method public virtual instance class jb 
          aQ(class ks A_0) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  1
    IL_0000:  ldnull
    IL_0001:  throw
  } // end of method x::aQ

} // end of class x

// $ isn't a valid identifier in C#, but we need the test entry point
// to be callable from C# (specifically the wrapper generator code)
.class public auto ansi _dollar_wrapper
{
  .method public static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  4
    call       int32 $::Main()
    ret
  }
}

.class public auto ansi $
       extends [mscorlib]System.Object
{
  .field static privatescope class x aL$PST04000008
  .field static privatescope class x aM$PST04000009
  .field static privatescope class x aN$PST0400000A
  .field static privatescope class x aO$PST0400000B
  .method public static int32  Main() cil managed
  {
    // Code size       14 (0xe)
    .maxstack  4
    IL_0000:  ldc.i4     0x7b
    IL_0005:  ldnull
    IL_0006:  ldnull
    IL_0007:  call       class jb $::gg(int32,
                                        class jb,
                                        class jb)
    IL_000c:  pop
		ldc.i4 100
    IL_000d:  ret
  } // end of method $::Main

  .method public static bool  F(int32 A_0,
                                int32 A_1) cil managed
  {
    // Code size       16 (0x10)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  blt        IL_000c

    IL_0007:  br         IL_000e

    IL_000c:  ldc.i4.0
    IL_000d:  ret

    IL_000e:  ldc.i4.1
    IL_000f:  ret
  } // end of method $::F

  .method public static class jb  gg(int32 A_0,
                                     class jb A_1,
                                     class jb A_2) cil managed
  {
    // Code size       1152 (0x480)
    .maxstack  12
    .locals init (class ks V_0,
             int32 V_1,
             int32 V_2,
             class jb V_3,
             class jb V_4,
             int32 V_5,
             class jb V_6,
             class jb V_7,
             int32 V_8,
             int32 V_9,
             class jb V_10,
             class jb V_11,
             class ks V_12,
             int32 V_13,
             int32 V_14,
             bool V_15,
             class jb V_16,
             class jb V_17,
             class jb V_18,
             class jb V_19,
             int32 V_20,
             int32 V_21,
             bool V_22,
             class jb V_23,
             class jb V_24,
             class jb V_25,
             class jb V_26,
             class jb V_27,
             class jb V_28,
             class jb V_29,
             class jb V_30,
             class jb V_31,
             bool V_32,
             class jb V_33,
             class jb V_34,
             class jb V_35,
             class jb V_36,
             class jb V_37,
             class jb V_38,
             class jb V_39,
             class jb V_40,
             class jb V_41,
             class jb V_42,
             class jb V_43,
             class jb V_44,
             class jb V_45,
             class jb V_46,
             bool V_47,
             class jb V_48,
             class jb V_49,
             class jb V_50,
             class jb V_51,
             class jb V_52)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldarg.2
    IL_0003:  newobj     instance void ks::.ctor(int32,
                                                 class jb,
                                                 class jb)
    IL_0008:  stloc.0
    IL_0009:  ldarg.1
    IL_000a:  brtrue     IL_0014

    IL_000f:  br         IL_025e

    IL_0014:  ldarg.1
    IL_0015:  stloc.s    V_52
    IL_0017:  ldloc.s    V_52
    IL_0019:  stloc.s    V_36
    IL_001b:  ldloc.s    V_36
    IL_001d:  ldfld      class jb jb::c$PST04000003
    IL_0022:  brtrue     IL_002c

    IL_0027:  br         IL_016a

    IL_002c:  ldloc.s    V_36
    IL_002e:  ldfld      class jb jb::c$PST04000003
    IL_0033:  stloc.s    V_51
    IL_0035:  ldloc.s    V_51
    IL_0037:  stloc.s    V_43
    IL_0039:  ldloc.s    V_36
    IL_003b:  ldfld      class jb jb::d$PST04000004
    IL_0040:  brtrue     IL_004a

    IL_0045:  br         IL_00fd

    IL_004a:  ldloc.s    V_36
    IL_004c:  ldfld      class jb jb::d$PST04000004
    IL_0051:  stloc.s    V_50
    IL_0053:  ldloc.s    V_50
    IL_0055:  stloc.s    V_46
    IL_0057:  ldarg.2
    IL_0058:  brtrue     IL_0062

    IL_005d:  br         IL_00b7

    IL_0062:  ldarg.2
    IL_0063:  stloc.s    V_49
    IL_0065:  ldloc.s    V_49
    IL_0067:  stloc.s    V_48
    IL_0069:  ldloc.0
    IL_006a:  ldloc.s    V_36
    IL_006c:  ldloc.s    V_48
    IL_006e:  ldloc.s    V_36
    IL_0070:  ldfld      int32 jb::b$PST04000002
    IL_0075:  ldarg.0
    IL_0076:  ldloc.s    V_46
    IL_0078:  ldloc.s    V_48
    IL_007a:  ldfld      class jb jb::c$PST04000003
    IL_007f:  ldloc.s    V_48
    IL_0081:  ldfld      int32 jb::b$PST04000002
    IL_0086:  ldloc.s    V_43
    IL_0088:  ldloc.s    V_48
    IL_008a:  ldfld      class jb jb::d$PST04000004
    IL_008f:  ldloc.s    V_36
    IL_0091:  ldfld      int32 jb::a$PST04000001
    IL_0096:  ldloc.s    V_48
    IL_0098:  ldfld      int32 jb::a$PST04000001
    IL_009d:  stloc.1
    IL_009e:  stloc.2
    IL_009f:  stloc.3
    IL_00a0:  stloc.s    V_4
    IL_00a2:  stloc.s    V_5
    IL_00a4:  stloc.s    V_6
    IL_00a6:  stloc.s    V_7
    IL_00a8:  stloc.s    V_8
    IL_00aa:  stloc.s    V_9
    IL_00ac:  stloc.s    V_10
    IL_00ae:  stloc.s    V_11
    IL_00b0:  stloc.s    V_12
    IL_00b2:  br         IL_0341

    IL_00b7:  ldloc.s    V_43
    IL_00b9:  ldfld      int32 jb::b$PST04000002
    IL_00be:  ldloc.s    V_46
    IL_00c0:  ldfld      int32 jb::b$PST04000002
    IL_00c5:  bgt        IL_00d2

    IL_00ca:  ldc.i4.0
    IL_00cb:  stloc.s    V_47
    IL_00cd:  br         IL_00d5

    IL_00d2:  ldc.i4.1
    IL_00d3:  stloc.s    V_47
    IL_00d5:  ldloc.s    V_47
    IL_00d7:  brtrue     IL_00e1

    IL_00dc:  br         IL_00ef

    IL_00e1:  ldsfld     class x $::aM$PST04000009
    IL_00e6:  ldloc.0
    IL_00e7:  tail.
    IL_00e9:  callvirt   instance class jb x::aO(class ks)
    IL_00ee:  ret

    IL_00ef:  ldsfld     class x $::aO$PST0400000B
    IL_00f4:  ldloc.0
    IL_00f5:  tail.
    IL_00f7:  callvirt   instance class jb x::aQ(class ks)
    IL_00fc:  ret

    IL_00fd:  ldarg.2
    IL_00fe:  brtrue     IL_0108

    IL_0103:  br         IL_015c

    IL_0108:  ldarg.2
    IL_0109:  stloc.s    V_45
    IL_010b:  ldloc.s    V_45
    IL_010d:  stloc.s    V_44
    IL_010f:  ldloc.0
    IL_0110:  ldloc.s    V_36
    IL_0112:  ldloc.s    V_44
    IL_0114:  ldloc.s    V_36
    IL_0116:  ldfld      int32 jb::b$PST04000002
    IL_011b:  ldarg.0
    IL_011c:  ldnull
    IL_011d:  ldloc.s    V_44
    IL_011f:  ldfld      class jb jb::c$PST04000003
    IL_0124:  ldloc.s    V_44
    IL_0126:  ldfld      int32 jb::b$PST04000002
    IL_012b:  ldloc.s    V_43
    IL_012d:  ldloc.s    V_44
    IL_012f:  ldfld      class jb jb::d$PST04000004
    IL_0134:  ldloc.s    V_36
    IL_0136:  ldfld      int32 jb::a$PST04000001
    IL_013b:  ldloc.s    V_44
    IL_013d:  ldfld      int32 jb::a$PST04000001
    IL_0142:  stloc.1
    IL_0143:  stloc.2
    IL_0144:  stloc.3
    IL_0145:  stloc.s    V_4
    IL_0147:  stloc.s    V_5
    IL_0149:  stloc.s    V_6
    IL_014b:  stloc.s    V_7
    IL_014d:  stloc.s    V_8
    IL_014f:  stloc.s    V_9
    IL_0151:  stloc.s    V_10
    IL_0153:  stloc.s    V_11
    IL_0155:  stloc.s    V_12
    IL_0157:  br         IL_0341

    IL_015c:  ldsfld     class x $::aM$PST04000009
    IL_0161:  ldloc.0
    IL_0162:  tail.
    IL_0164:  callvirt   instance class jb x::aO(class ks)
    IL_0169:  ret

    IL_016a:  ldloc.s    V_36
    IL_016c:  ldfld      class jb jb::d$PST04000004
    IL_0171:  brtrue     IL_017b

    IL_0176:  br         IL_01f5

    IL_017b:  ldloc.s    V_36
    IL_017d:  ldfld      class jb jb::d$PST04000004
    IL_0182:  stloc.s    V_42
    IL_0184:  ldloc.s    V_42
    IL_0186:  stloc.s    V_39
    IL_0188:  ldarg.2
    IL_0189:  brtrue     IL_0193

    IL_018e:  br         IL_01e7

    IL_0193:  ldarg.2
    IL_0194:  stloc.s    V_41
    IL_0196:  ldloc.s    V_41
    IL_0198:  stloc.s    V_40
    IL_019a:  ldloc.0
    IL_019b:  ldloc.s    V_36
    IL_019d:  ldloc.s    V_40
    IL_019f:  ldloc.s    V_36
    IL_01a1:  ldfld      int32 jb::b$PST04000002
    IL_01a6:  ldarg.0
    IL_01a7:  ldloc.s    V_39
    IL_01a9:  ldloc.s    V_40
    IL_01ab:  ldfld      class jb jb::c$PST04000003
    IL_01b0:  ldloc.s    V_40
    IL_01b2:  ldfld      int32 jb::b$PST04000002
    IL_01b7:  ldnull
    IL_01b8:  ldloc.s    V_40
    IL_01ba:  ldfld      class jb jb::d$PST04000004
    IL_01bf:  ldloc.s    V_36
    IL_01c1:  ldfld      int32 jb::a$PST04000001
    IL_01c6:  ldloc.s    V_40
    IL_01c8:  ldfld      int32 jb::a$PST04000001
    IL_01cd:  stloc.1
    IL_01ce:  stloc.2
    IL_01cf:  stloc.3
    IL_01d0:  stloc.s    V_4
    IL_01d2:  stloc.s    V_5
    IL_01d4:  stloc.s    V_6
    IL_01d6:  stloc.s    V_7
    IL_01d8:  stloc.s    V_8
    IL_01da:  stloc.s    V_9
    IL_01dc:  stloc.s    V_10
    IL_01de:  stloc.s    V_11
    IL_01e0:  stloc.s    V_12
    IL_01e2:  br         IL_0341

    IL_01e7:  ldsfld     class x $::aO$PST0400000B
    IL_01ec:  ldloc.0
    IL_01ed:  tail.
    IL_01ef:  callvirt   instance class jb x::aQ(class ks)
    IL_01f4:  ret

    IL_01f5:  ldarg.2
    IL_01f6:  brtrue     IL_0200

    IL_01fb:  br         IL_0253

    IL_0200:  ldarg.2
    IL_0201:  stloc.s    V_38
    IL_0203:  ldloc.s    V_38
    IL_0205:  stloc.s    V_37
    IL_0207:  ldloc.0
    IL_0208:  ldloc.s    V_36
    IL_020a:  ldloc.s    V_37
    IL_020c:  ldloc.s    V_36
    IL_020e:  ldfld      int32 jb::b$PST04000002
    IL_0213:  ldarg.0
    IL_0214:  ldnull
    IL_0215:  ldloc.s    V_37
    IL_0217:  ldfld      class jb jb::c$PST04000003
    IL_021c:  ldloc.s    V_37
    IL_021e:  ldfld      int32 jb::b$PST04000002
    IL_0223:  ldnull
    IL_0224:  ldloc.s    V_37
    IL_0226:  ldfld      class jb jb::d$PST04000004
    IL_022b:  ldloc.s    V_36
    IL_022d:  ldfld      int32 jb::a$PST04000001
    IL_0232:  ldloc.s    V_37
    IL_0234:  ldfld      int32 jb::a$PST04000001
    IL_0239:  stloc.1
    IL_023a:  stloc.2
    IL_023b:  stloc.3
    IL_023c:  stloc.s    V_4
    IL_023e:  stloc.s    V_5
    IL_0240:  stloc.s    V_6
    IL_0242:  stloc.s    V_7
    IL_0244:  stloc.s    V_8
    IL_0246:  stloc.s    V_9
    IL_0248:  stloc.s    V_10
    IL_024a:  stloc.s    V_11
    IL_024c:  stloc.s    V_12
    IL_024e:  br         IL_0341

    IL_0253:  ldarg.0
    IL_0254:  ldc.i4.2
    IL_0255:  ldloc.s    V_36
    IL_0257:  ldnull
    IL_0258:  newobj     instance void jb::.ctor(int32,
                                                 int32,
                                                 class jb,
                                                 class jb)
    IL_025d:  ret

    IL_025e:  ldarg.2
    IL_025f:  brtrue     IL_0269

    IL_0264:  br         IL_0337

    IL_0269:  ldarg.2
    IL_026a:  stloc.s    V_35
    IL_026c:  ldloc.s    V_35
    IL_026e:  stloc.s    V_27
    IL_0270:  ldloc.s    V_27
    IL_0272:  ldfld      class jb jb::c$PST04000003
    IL_0277:  brtrue     IL_0281

    IL_027c:  br         IL_0300

    IL_0281:  ldloc.s    V_27
    IL_0283:  ldfld      class jb jb::c$PST04000003
    IL_0288:  stloc.s    V_34
    IL_028a:  ldloc.s    V_34
    IL_028c:  stloc.s    V_30
    IL_028e:  ldloc.s    V_27
    IL_0290:  ldfld      class jb jb::d$PST04000004
    IL_0295:  brtrue     IL_029f

    IL_029a:  br         IL_02f2

    IL_029f:  ldloc.s    V_27
    IL_02a1:  ldfld      class jb jb::d$PST04000004
    IL_02a6:  stloc.s    V_33
    IL_02a8:  ldloc.s    V_33
    IL_02aa:  stloc.s    V_31
    IL_02ac:  ldloc.s    V_30
    IL_02ae:  ldfld      int32 jb::b$PST04000002
    IL_02b3:  ldloc.s    V_31
    IL_02b5:  ldfld      int32 jb::b$PST04000002
    IL_02ba:  blt        IL_02c7

    IL_02bf:  ldc.i4.0
    IL_02c0:  stloc.s    V_32
    IL_02c2:  br         IL_02ca

    IL_02c7:  ldc.i4.1
    IL_02c8:  stloc.s    V_32
    IL_02ca:  ldloc.s    V_32
    IL_02cc:  brtrue     IL_02d6

    IL_02d1:  br         IL_02e4

    IL_02d6:  ldsfld     class x $::aL$PST04000008
    IL_02db:  ldloc.0
    IL_02dc:  tail.
    IL_02de:  callvirt   instance class jb x::aN(class ks)
    IL_02e3:  ret

    IL_02e4:  ldsfld     class x $::aN$PST0400000A
    IL_02e9:  ldloc.0
    IL_02ea:  tail.
    IL_02ec:  callvirt   instance class jb x::aP(class ks)
    IL_02f1:  ret

    IL_02f2:  ldsfld     class x $::aN$PST0400000A
    IL_02f7:  ldloc.0
    IL_02f8:  tail.
    IL_02fa:  callvirt   instance class jb x::aP(class ks)
    IL_02ff:  ret

    IL_0300:  ldloc.s    V_27
    IL_0302:  ldfld      class jb jb::d$PST04000004
    IL_0307:  brtrue     IL_0311

    IL_030c:  br         IL_032c

    IL_0311:  ldloc.s    V_27
    IL_0313:  ldfld      class jb jb::d$PST04000004
    IL_0318:  stloc.s    V_29
    IL_031a:  ldloc.s    V_29
    IL_031c:  stloc.s    V_28
    IL_031e:  ldsfld     class x $::aL$PST04000008
    IL_0323:  ldloc.0
    IL_0324:  tail.
    IL_0326:  callvirt   instance class jb x::aN(class ks)
    IL_032b:  ret

    IL_032c:  ldarg.0
    IL_032d:  ldc.i4.2
    IL_032e:  ldnull
    IL_032f:  ldloc.s    V_27
    IL_0331:  newobj     instance void jb::.ctor(int32,
                                                 int32,
                                                 class jb,
                                                 class jb)
    IL_0336:  ret

    IL_0337:  ldarg.0
    IL_0338:  ldc.i4.1
    IL_0339:  ldnull
    IL_033a:  ldnull
    IL_033b:  newobj     instance void jb::.ctor(int32,
                                                 int32,
                                                 class jb,
                                                 class jb)
    IL_0340:  ret

    IL_0341:  ldloc.s    V_5
    IL_0343:  ldloc.s    V_9
    IL_0345:  ldloc.s    V_9
    IL_0347:  add
    IL_0348:  ldloc.s    V_9
    IL_034a:  add
    IL_034b:  call       bool $::F(int32,
                                   int32)
    IL_0350:  brtrue     IL_035a

    IL_0355:  br         IL_03d6

    IL_035a:  ldloc.s    V_6
    IL_035c:  brtrue     IL_0369

    IL_0361:  ldc.i4.0
    IL_0362:  stloc.s    V_20
    IL_0364:  br         IL_037a

    IL_0369:  ldloc.s    V_6
    IL_036b:  stloc.s    V_26
    IL_036d:  ldloc.s    V_26
    IL_036f:  stloc.s    V_25
    IL_0371:  ldloc.s    V_25
    IL_0373:  ldfld      int32 jb::b$PST04000002
    IL_0378:  stloc.s    V_20
    IL_037a:  ldloc.3
    IL_037b:  brtrue     IL_0388

    IL_0380:  ldc.i4.0
    IL_0381:  stloc.s    V_21
    IL_0383:  br         IL_0398

    IL_0388:  ldloc.3
    IL_0389:  stloc.s    V_24
    IL_038b:  ldloc.s    V_24
    IL_038d:  stloc.s    V_23
    IL_038f:  ldloc.s    V_23
    IL_0391:  ldfld      int32 jb::b$PST04000002
    IL_0396:  stloc.s    V_21
    IL_0398:  ldloc.s    V_20
    IL_039a:  ldloc.s    V_21
    IL_039c:  blt        IL_03a9

    IL_03a1:  ldc.i4.0
    IL_03a2:  stloc.s    V_22
    IL_03a4:  br         IL_03ac

    IL_03a9:  ldc.i4.1
    IL_03aa:  stloc.s    V_22
    IL_03ac:  ldloc.s    V_22
    IL_03ae:  brtrue     IL_03b8

    IL_03b3:  br         IL_03c7

    IL_03b8:  ldsfld     class x $::aL$PST04000008
    IL_03bd:  ldloc.s    V_12
    IL_03bf:  tail.
    IL_03c1:  callvirt   instance class jb x::aN(class ks)
    IL_03c6:  ret

    IL_03c7:  ldsfld     class x $::aN$PST0400000A
    IL_03cc:  ldloc.s    V_12
    IL_03ce:  tail.
    IL_03d0:  callvirt   instance class jb x::aP(class ks)
    IL_03d5:  ret

    IL_03d6:  ldloc.s    V_9
    IL_03d8:  ldloc.s    V_5
    IL_03da:  ldloc.s    V_5
    IL_03dc:  add
    IL_03dd:  ldloc.s    V_5
    IL_03df:  add
    IL_03e0:  call       bool $::F(int32,
                                   int32)
    IL_03e5:  brtrue     IL_03ef

    IL_03ea:  br         IL_046d

    IL_03ef:  ldloc.s    V_4
    IL_03f1:  brtrue     IL_03fe

    IL_03f6:  ldc.i4.0
    IL_03f7:  stloc.s    V_13
    IL_03f9:  br         IL_040f

    IL_03fe:  ldloc.s    V_4
    IL_0400:  stloc.s    V_19
    IL_0402:  ldloc.s    V_19
    IL_0404:  stloc.s    V_18
    IL_0406:  ldloc.s    V_18
    IL_0408:  ldfld      int32 jb::b$PST04000002
    IL_040d:  stloc.s    V_13
    IL_040f:  ldloc.s    V_7
    IL_0411:  brtrue     IL_041e

    IL_0416:  ldc.i4.0
    IL_0417:  stloc.s    V_14
    IL_0419:  br         IL_042f

    IL_041e:  ldloc.s    V_7
    IL_0420:  stloc.s    V_17
    IL_0422:  ldloc.s    V_17
    IL_0424:  stloc.s    V_16
    IL_0426:  ldloc.s    V_16
    IL_0428:  ldfld      int32 jb::b$PST04000002
    IL_042d:  stloc.s    V_14
    IL_042f:  ldloc.s    V_14
    IL_0431:  ldloc.s    V_13
    IL_0433:  blt        IL_0440

    IL_0438:  ldc.i4.0
    IL_0439:  stloc.s    V_15
    IL_043b:  br         IL_0443

    IL_0440:  ldc.i4.1
    IL_0441:  stloc.s    V_15
    IL_0443:  ldloc.s    V_15
    IL_0445:  brtrue     IL_044f

    IL_044a:  br         IL_045e

    IL_044f:  ldsfld     class x $::aM$PST04000009
    IL_0454:  ldloc.s    V_12
    IL_0456:  tail.
    IL_0458:  callvirt   instance class jb x::aO(class ks)
    IL_045d:  ret

    IL_045e:  ldsfld     class x $::aO$PST0400000B
    IL_0463:  ldloc.s    V_12
    IL_0465:  tail.
    IL_0467:  callvirt   instance class jb x::aQ(class ks)
    IL_046c:  ret

    IL_046d:  ldloc.s    V_8
    IL_046f:  ldloc.s    V_9
    IL_0471:  ldloc.s    V_5
    IL_0473:  add
    IL_0474:  ldc.i4.1
    IL_0475:  add
    IL_0476:  ldloc.s    V_11
    IL_0478:  ldloc.s    V_10
    IL_047a:  newobj     instance void jb::.ctor(int32,
                                                 int32,
                                                 class jb,
                                                 class jb)
    IL_047f:  ret
  } // end of method $::gg

}
