// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME {}
.assembly extern xunit.core {}
.class public sealed VT extends [mscorlib]System.ValueType
{
  .field public float64 f
  .field public float64 delta
  .field public int32 a
  .field public float64 b
  .field public float64 f2
}

.class public CL extends [mscorlib]System.Object
{
  .field public float64 f
  .field public float64 delta
  .field public int32 a
  .field public float64 b
  .field public float64 f2
  .method public specialname rtspecialname instance void .ctor() 
  { 
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.r8     1.
    IL_0006:  stfld      float64 CL::f
    IL_000b:  ldarg.0
    IL_000c:  ldc.r8     1.0000000000000001e-018
    IL_0011:  stfld      float64 CL::delta
    IL_0016:  ldarg.0
    IL_0017:  ldc.i4.3
    IL_0018:  stfld      int32 CL::a
    IL_001d:  ldarg.0
    IL_001e:  ldc.r8     0.33333333333333331
    IL_0023:  stfld      float64 CL::b
    IL_0028:  ldarg.0
    IL_0029:  call       instance void [mscorlib]System.Object::.ctor()
    IL_002e:  ret
  } // end of method CL::.ctor

} // end of class CL

.class public convr8d extends [mscorlib]System.Object
{
  .field private static float64 f1
  .field private static float64 delta1
  .field private static int32 a1
  .field private static float64 b1
  .method private static void  disableInline(int32& x) 
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method convr8d::disableInline

  .method private static float64 doubleadd() 
  {
    // Code size       25 (0x19)
    .maxstack  2
    .locals (int32 V_0,
             float64 V_1)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  ldloca.s   V_0
    IL_0004:  call       void convr8d::disableInline(int32&)
    IL_0009:  ldsfld     float64 convr8d::f1
    IL_000e:  ldsfld     float64 convr8d::delta1
    IL_0013:  add
    IL_0014:  stloc.1
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.1
    IL_0018:  ret
  } // end of method convr8d::doubleadd

  .method private static float64 doublesub() 
  {
    // Code size       25 (0x19)
    .maxstack  2
    .locals (int32 V_0,
             float64 V_1)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  ldloca.s   V_0
    IL_0004:  call       void convr8d::disableInline(int32&)
    IL_0009:  ldsfld     float64 convr8d::f1
    IL_000e:  ldsfld     float64 convr8d::delta1
    IL_0013:  sub
    IL_0014:  stloc.1
    IL_0015:  br.s       IL_0017

    IL_0017:  ldloc.1
    IL_0018:  ret
  } // end of method convr8d::doublesub

  .method private static float64 doublemul() 
  {
    // Code size       26 (0x1a)
    .maxstack  2
    .locals (int32 V_0,
             float64 V_1)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  ldloca.s   V_0
    IL_0004:  call       void convr8d::disableInline(int32&)
    IL_0009:  ldsfld     int32 convr8d::a1
    IL_000e:  conv.r8
    IL_000f:  ldsfld     float64 convr8d::b1
    IL_0014:  mul
    IL_0015:  stloc.1
    IL_0016:  br.s       IL_0018

    IL_0018:  ldloc.1
    IL_0019:  ret
  } // end of method convr8d::doublemul

  .method private static float64 doublediv() 
  {
    // Code size       26 (0x1a)
    .maxstack  2
    .locals (int32 V_0,
             float64 V_1)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  ldloca.s   V_0
    IL_0004:  call       void convr8d::disableInline(int32&)
    IL_0009:  ldsfld     float64 convr8d::f1
    IL_000e:  ldsfld     int32 convr8d::a1
    IL_0013:  conv.r8
    IL_0014:  div
    IL_0015:  stloc.1
    IL_0016:  br.s       IL_0018

    IL_0018:  ldloc.1
    IL_0019:  ret
  } // end of method convr8d::doublediv

  .method private static float64 doubleadd_inline() 
  {
    // Code size       16 (0x10)
    .maxstack  2
    .locals (float64 V_0)
    IL_0000:  ldsfld     float64 convr8d::f1
    IL_0005:  ldsfld     float64 convr8d::delta1
    IL_000a:  add
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method convr8d::doubleadd_inline

  .method private static float64 doublesub_inline() 
  {
    // Code size       16 (0x10)
    .maxstack  2
    .locals (float64 V_0)
    IL_0000:  ldsfld     float64 convr8d::f1
    IL_0005:  ldsfld     float64 convr8d::delta1
    IL_000a:  sub
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method convr8d::doublesub_inline

  .method private static float64 doublemul_inline() 
  {
    // Code size       17 (0x11)
    .maxstack  2
    .locals (float64 V_0)
    IL_0000:  ldsfld     int32 convr8d::a1
    IL_0005:  conv.r8
    IL_0006:  ldsfld     float64 convr8d::b1
    IL_000b:  mul
    IL_000c:  stloc.0
    IL_000d:  br.s       IL_000f

    IL_000f:  ldloc.0
    IL_0010:  ret
  } // end of method convr8d::doublemul_inline

  .method private static float64 doublediv_inline() 
  {
    // Code size       17 (0x11)
    .maxstack  2
    .locals (float64 V_0)
    IL_0000:  ldsfld     float64 convr8d::f1
    IL_0005:  ldsfld     int32 convr8d::a1
    IL_000a:  conv.r8
    IL_000b:  div
    IL_000c:  stloc.0
    IL_000d:  br.s       IL_000f

    IL_000f:  ldloc.0
    IL_0010:  ret
  } // end of method convr8d::doublediv_inline

  .method public static int32 Main() 
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       1155 (0x483)
    .maxstack  5
    .locals (bool V_0,
             float64 V_5,
             float64[] V_6,
             valuetype VT V_7,
             class CL V_8,
             int32 V_9)
    IL_0000:  ldloca.s   V_7
    IL_0002:  initobj    VT
    IL_0008:  ldc.i4.1
    IL_0009:  stloc.0
    
    IL_001f:  ldc.i4.3
    IL_0020:  newarr     [mscorlib]System.Double
    IL_0025:  stloc.s    V_6
    IL_0027:  newobj     instance void CL::.ctor()
    IL_002c:  stloc.s    V_8
    IL_002e:  call       void [System.Console]System.Console::WriteLine()
    IL_0033:  ldstr      "***add***"
    IL_0038:  call       void [System.Console]System.Console::WriteLine(string)
    IL_003d:  ldc.r8     1.
    IL_003e:  ldc.r8     1.0000000000000001e-018
    IL_003f:  add
    IL_0040:  conv.r8
    IL_0041:  ldc.r8     1.
    IL_0042:  beq.s      IL_0050

    IL_0044:  ldstr      "((double)(f+delta))!=f"
    IL_0049:  call       void [System.Console]System.Console::WriteLine(string)
    IL_004e:  ldc.i4.0
    IL_004f:  stloc.0
    IL_0050:  ldc.r8     1.
    IL_0051:  ldc.r8     1.0000000000000001e-018
    IL_0052:  add
    IL_0053:  stloc.s    V_5
    IL_0055:  ldloc.s    V_5
    IL_0057:  conv.r8
    IL_0058:  ldc.r8     1.
    IL_0059:  beq.s      IL_0067

    IL_005b:  ldstr      "((double)f2)!=f, f2=f+delta"
    IL_0060:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0065:  ldc.i4.0
    IL_0066:  stloc.0
    IL_0067:  call       float64 convr8d::doubleadd()
    IL_006c:  conv.r8
    IL_006d:  ldc.r8     1.
    IL_006e:  beq.s      IL_007c

    IL_0070:  ldstr      "((double)doubleadd())!=f"
    IL_0075:  call       void [System.Console]System.Console::WriteLine(string)
    IL_007a:  ldc.i4.0
    IL_007b:  stloc.0
    IL_007c:  call       float64 convr8d::doubleadd_inline()
    IL_0081:  conv.r8
    IL_0082:  ldc.r8     1.
    IL_0083:  beq.s      IL_0091

    IL_0085:  ldstr      "((double)doubleadd_inline())!=f"
    IL_008a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_008f:  ldc.i4.0
    IL_0090:  stloc.0
    IL_0091:  ldloc.s    V_6
    IL_0093:  ldc.i4.0
    IL_0094:  ldsfld     float64 convr8d::f1
    IL_0099:  stelem.r8
    IL_009a:  ldloc.s    V_6
    IL_009c:  ldc.i4.1
    IL_009d:  ldsfld     float64 convr8d::delta1
    IL_00a2:  stelem.r8
    IL_00a3:  ldloc.s    V_6
    IL_00a5:  ldc.i4.2
    IL_00a6:  ldloc.s    V_6
    IL_00a8:  ldc.i4.0
    IL_00a9:  ldelem.r8
    IL_00aa:  ldloc.s    V_6
    IL_00ac:  ldc.i4.1
    IL_00ad:  ldelem.r8
    IL_00ae:  add
    IL_00af:  stelem.r8
    IL_00b0:  ldloc.s    V_6
    IL_00b2:  ldc.i4.2
    IL_00b3:  ldelem.r8
    IL_00b4:  conv.r8
    IL_00b5:  ldc.r8     1.
    IL_00b6:  beq.s      IL_00c4

    IL_00b8:  ldstr      "((double)arr[2])!=f"
    IL_00bd:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00c2:  ldc.i4.0
    IL_00c3:  stloc.0
    IL_00c4:  ldloca.s   V_7
    IL_00c6:  ldc.r8     1.
    IL_00cb:  stfld      float64 VT::f
    IL_00d0:  ldloca.s   V_7
    IL_00d2:  ldc.r8     1.0000000000000001e-018
    IL_00d7:  stfld      float64 VT::delta
    IL_00dc:  ldloca.s   V_7
    IL_00de:  ldloca.s   V_7
    IL_00e0:  ldfld      float64 VT::f
    IL_00e5:  ldloca.s   V_7
    IL_00e7:  ldfld      float64 VT::delta
    IL_00ec:  add
    IL_00ed:  stfld      float64 VT::f2
    IL_00f2:  ldloca.s   V_7
    IL_00f4:  ldfld      float64 VT::f2
    IL_00f9:  conv.r8
    IL_00fa:  ldc.r8     1.
    IL_00fb:  beq.s      IL_0109

    IL_00fd:  ldstr      "((double)vt1.f2)!=f"
    IL_0102:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0107:  ldc.i4.0
    IL_0108:  stloc.0
    IL_0109:  ldloc.s    V_8
    IL_010b:  ldloc.s    V_8
    IL_010d:  ldfld      float64 CL::f
    IL_0112:  ldloc.s    V_8
    IL_0114:  ldfld      float64 CL::delta
    IL_0119:  add
    IL_011a:  stfld      float64 CL::f2
    IL_011f:  ldloc.s    V_8
    IL_0121:  ldfld      float64 CL::f2
    IL_0126:  conv.r8
    IL_0127:  ldc.r8     1.
    IL_0128:  beq.s      IL_0136

    IL_012a:  ldstr      "((double)cl1.f2)!=f"
    IL_012f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0134:  ldc.i4.0
    IL_0135:  stloc.0
    IL_0136:  call       void [System.Console]System.Console::WriteLine()
    IL_013b:  ldstr      "***sub***"
    IL_0140:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0145:  ldc.r8     1.
    IL_0146:  ldc.r8     1.0000000000000001e-018
    IL_0147:  sub
    IL_0148:  conv.r8
    IL_0149:  ldc.r8     1.
    IL_014a:  beq.s      IL_0158

    IL_014c:  ldstr      "((double)(f-delta))!=f"
    IL_0151:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0156:  ldc.i4.0
    IL_0157:  stloc.0
    IL_0158:  ldc.r8     1.
    IL_0159:  ldc.r8     1.0000000000000001e-018
    IL_015a:  sub
    IL_015b:  stloc.s    V_5
    IL_015d:  ldloc.s    V_5
    IL_015f:  conv.r8
    IL_0160:  ldc.r8     1.
    IL_0161:  beq.s      IL_016f

    IL_0163:  ldstr      "((double)f2)!=f, f2=f-delta"
    IL_0168:  call       void [System.Console]System.Console::WriteLine(string)
    IL_016d:  ldc.i4.0
    IL_016e:  stloc.0
    IL_016f:  call       float64 convr8d::doublesub()
    IL_0174:  conv.r8
    IL_0175:  ldc.r8     1.
    IL_0176:  beq.s      IL_0184

    IL_0178:  ldstr      "((double)doublesub())!=f"
    IL_017d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0182:  ldc.i4.0
    IL_0183:  stloc.0
    IL_0184:  call       float64 convr8d::doublesub_inline()
    IL_0189:  conv.r8
    IL_018a:  ldc.r8     1.
    IL_018b:  beq.s      IL_0199

    IL_018d:  ldstr      "((double)doublesub_inline())!=f"
    IL_0192:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0197:  ldc.i4.0
    IL_0198:  stloc.0
    IL_0199:  ldloc.s    V_6
    IL_019b:  ldc.i4.0
    IL_019c:  ldsfld     float64 convr8d::f1
    IL_01a1:  stelem.r8
    IL_01a2:  ldloc.s    V_6
    IL_01a4:  ldc.i4.1
    IL_01a5:  ldsfld     float64 convr8d::delta1
    IL_01aa:  stelem.r8
    IL_01ab:  ldloc.s    V_6
    IL_01ad:  ldc.i4.2
    IL_01ae:  ldloc.s    V_6
    IL_01b0:  ldc.i4.0
    IL_01b1:  ldelem.r8
    IL_01b2:  ldloc.s    V_6
    IL_01b4:  ldc.i4.1
    IL_01b5:  ldelem.r8
    IL_01b6:  sub
    IL_01b7:  stelem.r8
    IL_01b8:  ldloc.s    V_6
    IL_01ba:  ldc.i4.2
    IL_01bb:  ldelem.r8
    IL_01bc:  conv.r8
    IL_01bd:  ldc.r8     1.
    IL_01be:  beq.s      IL_01cc

    IL_01c0:  ldstr      "((double)arr[2])!=f"
    IL_01c5:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01ca:  ldc.i4.0
    IL_01cb:  stloc.0
    IL_01cc:  ldloca.s   V_7
    IL_01ce:  ldc.r8     1.
    IL_01d3:  stfld      float64 VT::f
    IL_01d8:  ldloca.s   V_7
    IL_01da:  ldc.r8     1.0000000000000001e-018
    IL_01df:  stfld      float64 VT::delta
    IL_01e4:  ldloca.s   V_7
    IL_01e6:  ldloca.s   V_7
    IL_01e8:  ldfld      float64 VT::f
    IL_01ed:  ldloca.s   V_7
    IL_01ef:  ldfld      float64 VT::delta
    IL_01f4:  sub
    IL_01f5:  stfld      float64 VT::f2
    IL_01fa:  ldloca.s   V_7
    IL_01fc:  ldfld      float64 VT::f2
    IL_0201:  conv.r8
    IL_0202:  ldc.r8     1.
    IL_0203:  beq.s      IL_0211

    IL_0205:  ldstr      "((double)vt1.f2)!=f"
    IL_020a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_020f:  ldc.i4.0
    IL_0210:  stloc.0
    IL_0211:  ldloc.s    V_8
    IL_0213:  ldloc.s    V_8
    IL_0215:  ldfld      float64 CL::f
    IL_021a:  ldloc.s    V_8
    IL_021c:  ldfld      float64 CL::delta
    IL_0221:  sub
    IL_0222:  stfld      float64 CL::f2
    IL_0227:  ldloc.s    V_8
    IL_0229:  ldfld      float64 CL::f2
    IL_022e:  conv.r8
    IL_022f:  ldc.r8     1.
    IL_0230:  beq.s      IL_023e

    IL_0232:  ldstr      "((double)cl1.f2)!=f"
    IL_0237:  call       void [System.Console]System.Console::WriteLine(string)
    IL_023c:  ldc.i4.0
    IL_023d:  stloc.0
    IL_023e:  call       void [System.Console]System.Console::WriteLine()
    IL_0243:  ldstr      "***mul***"
    IL_0248:  call       void [System.Console]System.Console::WriteLine(string)
    IL_024d:  ldc.i4.3
    IL_024e:  conv.r8
    IL_024f:  ldc.r8     0.33333333333333331
    IL_0251:  mul
    IL_0252:  conv.r8
    IL_0253:  ldc.r8     1.
    IL_0254:  beq.s      IL_0262

    IL_0256:  ldstr      "((double)(a*b))!=f"
    IL_025b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0260:  ldc.i4.0
    IL_0261:  stloc.0
    IL_0262:  ldc.i4.3
    IL_0263:  conv.r8
    IL_0264:  ldc.r8     0.33333333333333331
    IL_0266:  mul
    IL_0267:  stloc.s    V_5
    IL_0269:  ldloc.s    V_5
    IL_026b:  conv.r8
    IL_026c:  ldc.r8     1.
    IL_026d:  beq.s      IL_027b

    IL_026f:  ldstr      "((double)f2)!=f, f2=a*b"
    IL_0274:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0279:  ldc.i4.0
    IL_027a:  stloc.0
    IL_027b:  call       float64 convr8d::doublemul()
    IL_0280:  conv.r8
    IL_0281:  ldc.r8     1.
    IL_0282:  beq.s      IL_0290

    IL_0284:  ldstr      "((double)doublemul())!=f"
    IL_0289:  call       void [System.Console]System.Console::WriteLine(string)
    IL_028e:  ldc.i4.0
    IL_028f:  stloc.0
    IL_0290:  call       float64 convr8d::doublemul_inline()
    IL_0295:  conv.r8
    IL_0296:  ldc.r8     1.
    IL_0297:  beq.s      IL_02a5

    IL_0299:  ldstr      "((double)doublemul_inline())!=f"
    IL_029e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_02a3:  ldc.i4.0
    IL_02a4:  stloc.0
    IL_02a5:  ldloc.s    V_6
    IL_02a7:  ldc.i4.0
    IL_02a8:  ldsfld     int32 convr8d::a1
    IL_02ad:  conv.r8
    IL_02ae:  stelem.r8
    IL_02af:  ldloc.s    V_6
    IL_02b1:  ldc.i4.1
    IL_02b2:  ldsfld     float64 convr8d::b1
    IL_02b7:  stelem.r8
    IL_02b8:  ldloc.s    V_6
    IL_02ba:  ldc.i4.2
    IL_02bb:  ldloc.s    V_6
    IL_02bd:  ldc.i4.0
    IL_02be:  ldelem.r8
    IL_02bf:  ldloc.s    V_6
    IL_02c1:  ldc.i4.1
    IL_02c2:  ldelem.r8
    IL_02c3:  mul
    IL_02c4:  stelem.r8
    IL_02c5:  ldloc.s    V_6
    IL_02c7:  ldc.i4.2
    IL_02c8:  ldelem.r8
    IL_02c9:  conv.r8
    IL_02ca:  ldc.r8     1.
    IL_02cb:  beq.s      IL_02d9

    IL_02cd:  ldstr      "((double)arr[2])!=f"
    IL_02d2:  call       void [System.Console]System.Console::WriteLine(string)
    IL_02d7:  ldc.i4.0
    IL_02d8:  stloc.0
    IL_02d9:  ldloca.s   V_7
    IL_02db:  ldc.i4.3
    IL_02dc:  stfld      int32 VT::a
    IL_02e1:  ldloca.s   V_7
    IL_02e3:  ldc.r8     0.33333333333333331
    IL_02e8:  stfld      float64 VT::b
    IL_02ed:  ldloca.s   V_7
    IL_02ef:  ldloca.s   V_7
    IL_02f1:  ldfld      int32 VT::a
    IL_02f6:  conv.r8
    IL_02f7:  ldloca.s   V_7
    IL_02f9:  ldfld      float64 VT::b
    IL_02fe:  mul
    IL_02ff:  stfld      float64 VT::f2
    IL_0304:  ldloca.s   V_7
    IL_0306:  ldfld      float64 VT::f2
    IL_030b:  conv.r8
    IL_030c:  ldc.r8     1.
    IL_030d:  beq.s      IL_031b

    IL_030f:  ldstr      "((double)vt1.f2)!=f"
    IL_0314:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0319:  ldc.i4.0
    IL_031a:  stloc.0
    IL_031b:  ldloc.s    V_8
    IL_031d:  ldloc.s    V_8
    IL_031f:  ldfld      int32 CL::a
    IL_0324:  conv.r8
    IL_0325:  ldloc.s    V_8
    IL_0327:  ldfld      float64 CL::b
    IL_032c:  mul
    IL_032d:  stfld      float64 CL::f2
    IL_0332:  ldloc.s    V_8
    IL_0334:  ldfld      float64 CL::f2
    IL_0339:  conv.r8
    IL_033a:  ldc.r8     1.
    IL_033b:  beq.s      IL_0349

    IL_033d:  ldstr      "((double)cl1.f2)!=f"
    IL_0342:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0347:  ldc.i4.0
    IL_0348:  stloc.0
    IL_0349:  call       void [System.Console]System.Console::WriteLine()
    IL_034e:  ldstr      "***div***"
    IL_0353:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0358:  ldc.r8     1.
    IL_0359:  ldc.i4.3
    IL_035a:  conv.r8
    IL_035b:  div
    IL_035c:  conv.r8
    IL_035d:  ldc.r8     0.33333333333333331
    IL_035f:  beq.s      IL_036d

    IL_0361:  ldstr      "((double)(f/a))!=b"
    IL_0366:  call       void [System.Console]System.Console::WriteLine(string)
    IL_036b:  ldc.i4.0
    IL_036c:  stloc.0
    IL_036d:  ldc.r8     1.
    IL_036e:  ldc.i4.3
    IL_036f:  conv.r8
    IL_0370:  div
    IL_0371:  stloc.s    V_5
    IL_0373:  ldloc.s    V_5
    IL_0375:  conv.r8
    IL_0376:  ldc.r8     0.33333333333333331
    IL_0378:  beq.s      IL_0386

    IL_037a:  ldstr      "((double)f2)!=f, f2=f/a"
    IL_037f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0384:  ldc.i4.0
    IL_0385:  stloc.0
    IL_0386:  call       float64 convr8d::doublediv()
    IL_038b:  conv.r8
    IL_038c:  ldc.r8     0.33333333333333331
    IL_038e:  beq.s      IL_039c

    IL_0390:  ldstr      "((double)doubledivl())!=b"
    IL_0395:  call       void [System.Console]System.Console::WriteLine(string)
    IL_039a:  ldc.i4.0
    IL_039b:  stloc.0
    IL_039c:  call       float64 convr8d::doublediv_inline()
    IL_03a1:  conv.r8
    IL_03a2:  ldc.r8     0.33333333333333331
    IL_03a4:  beq.s      IL_03b2

    IL_03a6:  ldstr      "((double)doublediv_inline())!=b"
    IL_03ab:  call       void [System.Console]System.Console::WriteLine(string)
    IL_03b0:  ldc.i4.0
    IL_03b1:  stloc.0
    IL_03b2:  ldloc.s    V_6
    IL_03b4:  ldc.i4.0
    IL_03b5:  ldsfld     float64 convr8d::f1
    IL_03ba:  stelem.r8
    IL_03bb:  ldloc.s    V_6
    IL_03bd:  ldc.i4.1
    IL_03be:  ldsfld     int32 convr8d::a1
    IL_03c3:  conv.r8
    IL_03c4:  stelem.r8
    IL_03c5:  ldloc.s    V_6
    IL_03c7:  ldc.i4.2
    IL_03c8:  ldloc.s    V_6
    IL_03ca:  ldc.i4.0
    IL_03cb:  ldelem.r8
    IL_03cc:  ldloc.s    V_6
    IL_03ce:  ldc.i4.1
    IL_03cf:  ldelem.r8
    IL_03d0:  div
    IL_03d1:  stelem.r8
    IL_03d2:  ldloc.s    V_6
    IL_03d4:  ldc.i4.2
    IL_03d5:  ldelem.r8
    IL_03d6:  conv.r8
    IL_03d7:  ldc.r8     0.33333333333333331
    IL_03d9:  beq.s      IL_03e7

    IL_03db:  ldstr      "((double)arr[2])!=b"
    IL_03e0:  call       void [System.Console]System.Console::WriteLine(string)
    IL_03e5:  ldc.i4.0
    IL_03e6:  stloc.0
    IL_03e7:  ldloca.s   V_7
    IL_03e9:  ldc.r8     1.
    IL_03ee:  stfld      float64 VT::f
    IL_03f3:  ldloca.s   V_7
    IL_03f5:  ldc.i4.3
    IL_03f6:  stfld      int32 VT::a
    IL_03fb:  ldloca.s   V_7
    IL_03fd:  ldloca.s   V_7
    IL_03ff:  ldfld      float64 VT::f
    IL_0404:  ldloca.s   V_7
    IL_0406:  ldfld      int32 VT::a
    IL_040b:  conv.r8
    IL_040c:  div
    IL_040d:  stfld      float64 VT::f2
    IL_0412:  ldloca.s   V_7
    IL_0414:  ldfld      float64 VT::f2
    IL_0419:  conv.r8
    IL_041a:  ldc.r8     0.33333333333333331
    IL_041c:  beq.s      IL_042a

    IL_041e:  ldstr      "((double)vt1.f2)!=b"
    IL_0423:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0428:  ldc.i4.0
    IL_0429:  stloc.0
    IL_042a:  ldloc.s    V_8
    IL_042c:  ldloc.s    V_8
    IL_042e:  ldfld      float64 CL::f
    IL_0433:  ldloc.s    V_8
    IL_0435:  ldfld      int32 CL::a
    IL_043a:  conv.r8
    IL_043b:  div
    IL_043c:  stfld      float64 CL::f2
    IL_0441:  ldloc.s    V_8
    IL_0443:  ldfld      float64 CL::f2
    IL_0448:  conv.r8
    IL_0449:  ldc.r8     0.33333333333333331
    IL_044b:  beq.s      IL_0459

    IL_044d:  ldstr      "((double)cl1.f2)!=b"
    IL_0452:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0457:  ldc.i4.0
    IL_0458:  stloc.0
    IL_0459:  call       void [System.Console]System.Console::WriteLine()
    IL_045e:  ldloc.0
    IL_045f:  brfalse.s  IL_0471

    IL_0461:  ldstr      "SUCCESS"
    IL_0466:  call       void [System.Console]System.Console::WriteLine(string)
    IL_046b:  ldc.i4.s   100
    IL_046d:  stloc.s    V_9
    IL_046f:  br.s       IL_0480

    IL_0471:  ldstr      "FAILURE: float not truncated properly"
    IL_0476:  call       void [System.Console]System.Console::WriteLine(string)
    IL_047b:  ldc.i4.1
    IL_047c:  stloc.s    V_9
    IL_047e:  br.s       IL_0480

    IL_0480:  ldloc.s    V_9
    IL_0482:  ret
  } // end of method convr8d::Main

  .method private specialname rtspecialname static 
          void .cctor() 
  {
    // Code size       37 (0x25)
    .maxstack  8
    IL_0000:  ldc.r8     1.
    IL_0005:  stsfld     float64 convr8d::f1
    IL_000a:  ldc.r8     1.0000000000000001e-018
    IL_000f:  stsfld     float64 convr8d::delta1
    IL_0014:  ldc.i4.3
    IL_0015:  stsfld     int32 convr8d::a1
    IL_001a:  ldc.r8     0.33333333333333331
    IL_001f:  stsfld     float64 convr8d::b1
    IL_0024:  ret
  } // end of method convr8d::.cctor 

} // end of class convr8d 
