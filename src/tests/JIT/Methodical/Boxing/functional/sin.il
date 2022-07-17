// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib
{
}
.assembly sin
{
}
.assembly extern xunit.core {}
.namespace SinCalc_sin_functional_il
{
  .class public auto ansi SinCalc_sin_functional_il
         extends [mscorlib]System.Object
  {
    .class auto ansi sealed nested family SinCalc$CalcCtx
           extends [mscorlib]System.ValueType
    {
      .field private float64 m_powX
      .field private float64 m_sumOfTerms
      .field private object m_term
      .field public float64 fact
      .method public hidebysig instance float64 
              get_powX() cil managed
      {
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  ldfld      float64 SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::m_powX
        IL_0006:  ret
      } 

      .method public hidebysig instance void 
              set_powX(float64 val) cil managed
      {
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  ldarg.1
        IL_0002:  stfld      float64 SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::m_powX
        IL_0007:  ret
      } 

      .method public hidebysig specialname 
              instance float64  get_sumOfTerms() cil managed
      {
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  ldfld      float64 SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::m_sumOfTerms
        IL_0006:  ret
      } 

      .method public hidebysig specialname 
              instance void  set_sumOfTerms(float64 'value') cil managed
      {
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  ldarg.1
        IL_0002:  stfld      float64 SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::m_sumOfTerms
        IL_0007:  ret
      } 

      .method public hidebysig specialname 
              instance float64  get_term() cil managed
      {
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  ldfld      object SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::m_term
        IL_0006:  unbox      [mscorlib]System.Double
        IL_000b:  ldind.r8
        IL_000c:  ret
      } 

      .method public hidebysig specialname 
              instance void  set_term(float64 'value') cil managed
      {
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  ldarg.s    'value'
        IL_0003:  box        [mscorlib]System.Double
        IL_0008:  stfld      object SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::m_term
        IL_000d:  ret
      } 

      .property instance float64 sumOfTerms()
      {
        .get instance float64 SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::get_sumOfTerms()
        .set instance void SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::set_sumOfTerms(float64)
      } 
      .property instance float64 term()
      {
        .get instance float64 SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::get_term()
        .set instance void SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::set_term(float64)
      } 
    } 

    .field static family object PI
    .method family hidebysig static object 
            mySin(object Angle) cil managed
    {
      .maxstack  4
      .locals (valuetype SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx V_0,
               object V_1,
               object V_2,
               float64 V_3,
               int32 V_4)
      IL_0000:  ldloca.s   V_0
      IL_0002:  initobj    SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx
      IL_0008:  ldloc.s    V_0
      IL_000a:  box        SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx
      IL_000f:  stloc.1
      IL_0010:  ldloc      V_1
      IL_0014:  unbox      SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx
      IL_0019:  ldc.r8     1.
      IL_0022:  stfld      float64 SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::fact
      IL_0027:  ldloc      V_1
      IL_002b:  unbox      SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx
      IL_0030:  ldloc      V_1
      IL_0034:  unbox      SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx
      IL_0039:  ldarg.0
      IL_003a:  unbox      [mscorlib]System.Double
      IL_003f:  ldind.r8
      IL_0040:  dup
      IL_0041:  stloc.3
      IL_0042:  call       instance void SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::set_term(float64)
      IL_0047:  ldloc.3
      IL_0048:  ldftn      instance void SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::set_powX(float64)
      IL_004e:  calli      instance void(float64)
      IL_0053:  ldloc      V_1
      IL_0057:  unbox      SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx
      IL_005c:  ldc.r8     0.0
      IL_0065:  call       instance void SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::set_sumOfTerms(float64)
      IL_006a:  ldc.i4.1
      IL_006b:  stloc.s    V_4
      IL_006d:  ldloc.s    V_4
      IL_006f:  box        [mscorlib]System.Int32
      IL_0074:  stloc.2
      IL_0075:  br         IL_0142

      IL_007a:  ldloc      V_1
      IL_007e:  unbox      SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx
      IL_0083:  ldloc      V_1
      IL_0087:  unbox      SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx
      IL_008c:  call       instance float64 SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::get_sumOfTerms()
      IL_0091:  ldloc      V_1
      IL_0095:  unbox      SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx
      IL_009a:  call       instance float64 SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::get_term()
      IL_009f:  add
      IL_00a0:  call       instance void SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::set_sumOfTerms(float64)
      IL_00a5:  ldloc      V_1
      IL_00a9:  unbox      SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx
      IL_00ae:  ldloc      V_1
      IL_00b2:  unbox      SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx
      IL_00b7:  call       instance float64 SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::get_powX()
      IL_00bc:  ldarg.0
      IL_00bd:  unbox      [mscorlib]System.Double
      IL_00c2:  ldind.r8
      IL_00c3:  neg
      IL_00c4:  ldarg.0
      IL_00c5:  unbox      [mscorlib]System.Double
      IL_00ca:  ldind.r8
      IL_00cb:  mul
      IL_00cc:  mul
      IL_00cd:  call       instance void SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::set_powX(float64)
      IL_00d2:  ldloc      V_1
      IL_00d6:  unbox      SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx
      IL_00db:  ldloc      V_1
      IL_00df:  unbox      SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx
      IL_00e4:  ldfld      float64 SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::fact
      IL_00e9:  ldloc.2
      IL_00ea:  unbox      [mscorlib]System.Int32
      IL_00ef:  ldind.i4
      IL_00f0:  ldc.i4.1
      IL_00f1:  add
      IL_00f2:  conv.r8
      IL_00f3:  mul
      IL_00f4:  ldloc.2
      IL_00f5:  unbox      [mscorlib]System.Int32
      IL_00fa:  ldind.i4
      IL_00fb:  ldc.i4.2
      IL_00fc:  add
      IL_00fd:  conv.r8
      IL_00fe:  mul
      IL_00ff:  stfld      float64 SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::fact
      IL_0104:  ldloc      V_1
      IL_0108:  unbox      SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx
      IL_010d:  ldloc      V_1
      IL_0111:  unbox      SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx
      IL_0116:  call       instance float64 SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::get_powX()
      IL_011b:  ldloc      V_1
      IL_011f:  unbox      SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx
      IL_0124:  ldfld      float64 SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::fact
      IL_0129:  div
      IL_012a:  call       instance void SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::set_term(float64)
      IL_012f:  ldloc.2
      IL_0130:  unbox      [mscorlib]System.Int32
      IL_0135:  ldind.i4
      IL_0136:  ldc.i4.2
      IL_0137:  add
      IL_0138:  stloc.s    V_4
      IL_013a:  ldloc.s    V_4
      IL_013c:  box        [mscorlib]System.Int32
      IL_0141:  stloc.2
      IL_0142:  ldloc.2
      IL_0143:  unbox      [mscorlib]System.Int32
      IL_0148:  ldind.i4
      IL_0149:  ldc.i4     0xc8
      IL_014e:  ble        IL_007a

      IL_0153:  ldloc      V_1
      IL_0157:  unbox      SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx
      IL_015c:  call       instance float64 SinCalc_sin_functional_il.SinCalc_sin_functional_il/SinCalc$CalcCtx::get_sumOfTerms()
      IL_0161:  stloc.3
      IL_0162:  ldloc.s    V_3
      IL_0164:  box        [mscorlib]System.Double
      IL_0169:  ret
    } 

    .method public hidebysig static int32 
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  3
      .locals (object V_0,
               object V_1,
               object V_2,
               object V_3,
               object[] V_4,
               object V_5,
               float64 V_6,
               int32 V_7)
      IL_0000:  ldc.i4.s   10
      IL_0002:  newarr     [mscorlib]System.Object
      IL_0007:  stloc.s    V_4
      IL_0009:  ldloc.s    V_4
      IL_000b:  ldc.i4.0
      IL_000c:  ldc.r8     0.0
      IL_0015:  stloc.s    V_6
      IL_0017:  ldloc.s    V_6
      IL_0019:  box        [mscorlib]System.Double
      IL_001e:  stelem.ref
      IL_001f:  ldloc.s    V_4
      IL_0021:  ldc.i4.1
      IL_0022:  ldc.r8     0.30901699399999999
      IL_002b:  stloc.s    V_6
      IL_002d:  ldloc.s    V_6
      IL_002f:  box        [mscorlib]System.Double
      IL_0034:  stelem.ref
      IL_0035:  ldloc.s    V_4
      IL_0037:  ldc.i4.2
      IL_0038:  ldc.r8     0.58778525199999998
      IL_0041:  stloc.s    V_6
      IL_0043:  ldloc.s    V_6
      IL_0045:  box        [mscorlib]System.Double
      IL_004a:  stelem.ref
      IL_004b:  ldloc.s    V_4
      IL_004d:  ldc.i4.3
      IL_004e:  ldc.r8     0.80901699400000004
      IL_0057:  stloc.s    V_6
      IL_0059:  ldloc.s    V_6
      IL_005b:  box        [mscorlib]System.Double
      IL_0060:  stelem.ref
      IL_0061:  ldloc.s    V_4
      IL_0063:  ldc.i4.4
      IL_0064:  ldc.r8     0.95105651599999996
      IL_006d:  stloc.s    V_6
      IL_006f:  ldloc.s    V_6
      IL_0071:  box        [mscorlib]System.Double
      IL_0076:  stelem.ref
      IL_0077:  ldloc.s    V_4
      IL_0079:  ldc.i4.5
      IL_007a:  ldc.r8     1.
      IL_0083:  stloc.s    V_6
      IL_0085:  ldloc.s    V_6
      IL_0087:  box        [mscorlib]System.Double
      IL_008c:  stelem.ref
      IL_008d:  ldloc.s    V_4
      IL_008f:  ldc.i4.6
      IL_0090:  ldc.r8     0.95105651599999996
      IL_0099:  stloc.s    V_6
      IL_009b:  ldloc.s    V_6
      IL_009d:  box        [mscorlib]System.Double
      IL_00a2:  stelem.ref
      IL_00a3:  ldloc.s    V_4
      IL_00a5:  ldc.i4.7
      IL_00a6:  ldc.r8     0.80901699400000004
      IL_00af:  stloc.s    V_6
      IL_00b1:  ldloc.s    V_6
      IL_00b3:  box        [mscorlib]System.Double
      IL_00b8:  stelem.ref
      IL_00b9:  ldloc.s    V_4
      IL_00bb:  ldc.i4.8
      IL_00bc:  ldc.r8     0.58778525199999998
      IL_00c5:  stloc.s    V_6
      IL_00c7:  ldloc.s    V_6
      IL_00c9:  box        [mscorlib]System.Double
      IL_00ce:  stelem.ref
      IL_00cf:  ldloc.s    V_4
      IL_00d1:  ldc.i4.s   9
      IL_00d3:  ldc.r8     0.30901699399999999
      IL_00dc:  stloc.s    V_6
      IL_00de:  ldloc.s    V_6
      IL_00e0:  box        [mscorlib]System.Double
      IL_00e5:  stelem.ref
      IL_00e6:  ldc.r8     1.0000000000000001e-009
      IL_00ef:  stloc.s    V_6
      IL_00f1:  ldloc.s    V_6
      IL_00f3:  box        [mscorlib]System.Double
      IL_00f8:  stloc.s    V_5
      IL_00fa:  ldc.i4.0
      IL_00fb:  stloc.s    V_7
      IL_00fd:  ldloc.s    V_7
      IL_00ff:  box        [mscorlib]System.Int32
      IL_0104:  stloc.0
      IL_0105:  br         IL_026a

      IL_010a:  ldsfld     object SinCalc_sin_functional_il.SinCalc_sin_functional_il::PI
      IL_010f:  unbox      [mscorlib]System.Double
      IL_0114:  ldind.r8
      IL_0115:  ldloc.0
      IL_0116:  unbox      [mscorlib]System.Int32
      IL_011b:  ldind.i4
      IL_011c:  conv.r8
      IL_011d:  ldc.r8     10.
      IL_0126:  div
      IL_0127:  mul
      IL_0128:  stloc.s    V_6
      IL_012a:  ldloc.s    V_6
      IL_012c:  box        [mscorlib]System.Double
      IL_0131:  stloc.1
      IL_0132:  ldstr      "Classlib Sin("
      IL_0137:  call       void [System.Console]System.Console::Write(string)
      IL_013c:  ldloc.1
      IL_013d:  call       void [System.Console]System.Console::Write(object)
      IL_0142:  ldstr      ")="
      IL_0147:  call       void [System.Console]System.Console::Write(string)
      IL_014c:  ldloc.1
      IL_014d:  unbox      [mscorlib]System.Double
      IL_0152:  ldind.r8
      IL_0153:  call       float64 [mscorlib]System.Math::Sin(float64)
      IL_0158:  stloc.s    V_6
      IL_015a:  ldloc.s    V_6
      IL_015c:  box        [mscorlib]System.Double
      IL_0161:  dup
      IL_0162:  stloc.2
      IL_0163:  call       void [System.Console]System.Console::WriteLine(object)
      IL_0168:  ldstr      "This Version("
      IL_016d:  call       void [System.Console]System.Console::Write(string)
      IL_0172:  ldloc.1
      IL_0173:  call       void [System.Console]System.Console::Write(object)
      IL_0178:  ldstr      ")="
      IL_017d:  call       void [System.Console]System.Console::Write(string)
      IL_0182:  ldloc.1
      IL_0183:  ldftn      object SinCalc_sin_functional_il.SinCalc_sin_functional_il::mySin(object)
      IL_0189:  calli      object(object)
      IL_018e:  unbox      [mscorlib]System.Double
      IL_0193:  ldind.r8
      IL_0194:  stloc.s    V_6
      IL_0196:  ldloc.s    V_6
      IL_0198:  box        [mscorlib]System.Double
      IL_019d:  dup
      IL_019e:  stloc.3
      IL_019f:  call       void [System.Console]System.Console::WriteLine(object)
      IL_01a4:  ldstr      "Error is:"
      IL_01a9:  call       void [System.Console]System.Console::Write(string)
      IL_01ae:  ldloc.2
      IL_01af:  unbox      [mscorlib]System.Double
      IL_01b4:  ldind.r8
      IL_01b5:  ldloc.3
      IL_01b6:  unbox      [mscorlib]System.Double
      IL_01bb:  ldind.r8
      IL_01bc:  sub
      IL_01bd:  call       void [System.Console]System.Console::WriteLine(float64)
      IL_01c2:  call       void [System.Console]System.Console::WriteLine()
      IL_01c7:  ldloc.2
      IL_01c8:  unbox      [mscorlib]System.Double
      IL_01cd:  ldind.r8
      IL_01ce:  ldloc.3
      IL_01cf:  unbox      [mscorlib]System.Double
      IL_01d4:  ldind.r8
      IL_01d5:  sub
      IL_01d6:  call       float64 [mscorlib]System.Math::Abs(float64)
      IL_01db:  ldloc.s    V_5
      IL_01dd:  unbox      [mscorlib]System.Double
      IL_01e2:  ldind.r8
      IL_01e3:  ble.un.s   IL_01f1

      IL_01e5:  ldstr      "ERROR, Versions too far apart!"
      IL_01ea:  call       void [System.Console]System.Console::WriteLine(string)
      IL_01ef:  ldc.i4.1
      IL_01f0:  ret

      IL_01f1:  ldloc.s    V_4
      IL_01f3:  ldloc.0
      IL_01f4:  unbox      [mscorlib]System.Int32
      IL_01f9:  ldind.i4
      IL_01fa:  ldelem.ref
      IL_01fb:  unbox      [mscorlib]System.Double
      IL_0200:  ldind.r8
      IL_0201:  ldloc.2
      IL_0202:  unbox      [mscorlib]System.Double
      IL_0207:  ldind.r8
      IL_0208:  sub
      IL_0209:  call       float64 [mscorlib]System.Math::Abs(float64)
      IL_020e:  ldloc.s    V_5
      IL_0210:  unbox      [mscorlib]System.Double
      IL_0215:  ldind.r8
      IL_0216:  ble.un.s   IL_0224

      IL_0218:  ldstr      "ERROR, Classlib version isnt right!"
      IL_021d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0222:  ldc.i4.1
      IL_0223:  ret

      IL_0224:  ldloc.s    V_4
      IL_0226:  ldloc.0
      IL_0227:  unbox      [mscorlib]System.Int32
      IL_022c:  ldind.i4
      IL_022d:  ldelem.ref
      IL_022e:  unbox      [mscorlib]System.Double
      IL_0233:  ldind.r8
      IL_0234:  ldloc.3
      IL_0235:  unbox      [mscorlib]System.Double
      IL_023a:  ldind.r8
      IL_023b:  sub
      IL_023c:  call       float64 [mscorlib]System.Math::Abs(float64)
      IL_0241:  ldloc.s    V_5
      IL_0243:  unbox      [mscorlib]System.Double
      IL_0248:  ldind.r8
      IL_0249:  ble.un.s   IL_0257

      IL_024b:  ldstr      "ERROR, our version isnt right!"
      IL_0250:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0255:  ldc.i4.1
      IL_0256:  ret

      IL_0257:  ldloc.0
      IL_0258:  unbox      [mscorlib]System.Int32
      IL_025d:  ldind.i4
      IL_025e:  ldc.i4.1
      IL_025f:  add
      IL_0260:  stloc.s    V_7
      IL_0262:  ldloc.s    V_7
      IL_0264:  box        [mscorlib]System.Int32
      IL_0269:  stloc.0
      IL_026a:  ldloc.0
      IL_026b:  unbox      [mscorlib]System.Int32
      IL_0270:  ldind.i4
      IL_0271:  ldc.i4.s   10
      IL_0273:  blt        IL_010a

      IL_0278:  ldstr      "Yippie, all correct"
      IL_027d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0282:  ldc.i4    0x64
      IL_0283:  ret
    } 

    .method public hidebysig specialname rtspecialname static 
            void  .cctor() cil managed
    {
      .maxstack  1
      .locals (float64 V_0)
      IL_0000:  ldc.r8     3.1415926535897931
      IL_0009:  stloc.0
      IL_000a:  ldloc.s    V_0
      IL_000c:  box        [mscorlib]System.Double
      IL_0011:  stsfld     object SinCalc_sin_functional_il.SinCalc_sin_functional_il::PI
      IL_0016:  ret
    } 

    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } 

  } 




} 
