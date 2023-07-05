// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern mscorlib { }
.assembly ASSEMBLY_NAME { }
.assembly extern xunit.core {}
.namespace SinCalc_try_seh_il
{
  .class auto ansi MistakeException extends [mscorlib]System.Exception
  {
    .field public class System.Object mistake
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(float64 m) il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldstr      "Mistake!"
      IL_0006:  call       instance void [mscorlib]System.Exception::.ctor(class System.String)
      IL_000b:  ldarg.0
      IL_000c:  ldarg.s   m
      IL_000e:  box       [mscorlib]System.Double
      IL_0013:  stfld      class System.Object SinCalc_try_seh_il.MistakeException::mistake
      IL_0018:  ret
    } // end of method 'MistakeException::.ctor'

    .method public hidebysig virtual instance class System.String
            ToString() il managed
    {
      .maxstack  8
      IL_0000:  ldstr      "Mistake is "
      IL_0005:  ldarg.0
      IL_0006:  ldfld      class System.Object SinCalc_try_seh_il.MistakeException::mistake
      IL_000b:  callvirt   instance class System.String [mscorlib]System.Object::ToString()
      IL_0010:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                               class System.String)
      IL_0015:  ret
    } // end of method 'MistakeException::ToString'

  } // end of class 'MistakeException'

  .class public auto ansi SinCalc_try_seh_il extends [mscorlib]System.Object
  {
    .field static family class System.Object PI
    .method family hidebysig static class System.Object 
            mySin(class System.Object Angle) il managed
    {
      .maxstack  3
      .locals ([0] class System.Object powX,
               [1] class System.Object sumOfTerms,
               [2] class System.Object term,
               [3] class System.Object fact,
               [4] class System.Object i,
               [5] float64 V_5,
               [6] int32 V_6)
      IL_0000:  ldc.r8     1.
      IL_0009:  stloc.s    V_5
      IL_000b:  ldloc.s   V_5
      IL_000d:  box       [mscorlib]System.Double
      IL_0012:  stloc.3
      IL_0013:  ldarg.0
      IL_0014:  dup
      IL_0015:  stloc.2
      IL_0016:  stloc.0
      IL_0017:  ldc.r8     0.
      IL_0020:  stloc.s    V_5
      IL_0022:  ldloc.s   V_5
      IL_0024:  box       [mscorlib]System.Double
      IL_0029:  stloc.1
      IL_002a:  ldc.i4.1
      IL_002b:  stloc.s    V_6
      IL_002d:  ldloc.s   V_6
      IL_002f:  box       [mscorlib]System.Int32
      IL_0034:  stloc.s    i
      IL_0036:  br         IL_00cd

      IL_003b:  ldloc.1
      IL_003c:  unbox      [mscorlib]System.Double
      IL_0041:  ldind.r8
      IL_0042:  ldloc.2
      IL_0043:  unbox      [mscorlib]System.Double
      IL_0048:  ldind.r8
      IL_0049:  add
      IL_004a:  stloc.s    V_5
      IL_004c:  ldloc.s   V_5
      IL_004e:  box       [mscorlib]System.Double
      IL_0053:  stloc.1
      IL_0054:  ldloc.0
      IL_0055:  unbox      [mscorlib]System.Double
      IL_005a:  ldind.r8
      IL_005b:  ldarg.0
      IL_005c:  unbox      [mscorlib]System.Double
      IL_0061:  ldind.r8
      IL_0062:  neg
      IL_0063:  ldarg.0
      IL_0064:  unbox      [mscorlib]System.Double
      IL_0069:  ldind.r8
      IL_006a:  mul
      IL_006b:  mul
      IL_006c:  stloc.s    V_5
      IL_006e:  ldloc.s   V_5
      IL_0070:  box       [mscorlib]System.Double
      IL_0075:  stloc.0
      IL_0076:  ldloc.3
      IL_0077:  unbox      [mscorlib]System.Double
      IL_007c:  ldind.r8
      IL_007d:  ldloc.s    i
      IL_007f:  unbox      [mscorlib]System.Int32
      IL_0084:  ldind.i4
      IL_0085:  ldc.i4.1
      IL_0086:  add
      IL_0087:  conv.r8
      IL_0088:  mul
      IL_0089:  ldloc.s    i
      IL_008b:  unbox      [mscorlib]System.Int32
      IL_0090:  ldind.i4
      IL_0091:  ldc.i4.2
      IL_0092:  add
      IL_0093:  conv.r8
      IL_0094:  mul
      IL_0095:  stloc.s    V_5
      IL_0097:  ldloc.s   V_5
      IL_0099:  box       [mscorlib]System.Double
      IL_009e:  stloc.3
      IL_009f:  ldloc.0
      IL_00a0:  unbox      [mscorlib]System.Double
      IL_00a5:  ldind.r8
      IL_00a6:  ldloc.3
      IL_00a7:  unbox      [mscorlib]System.Double
      IL_00ac:  ldind.r8
      IL_00ad:  div
      IL_00ae:  stloc.s    V_5
      IL_00b0:  ldloc.s   V_5
      IL_00b2:  box       [mscorlib]System.Double
      IL_00b7:  stloc.2
      IL_00b8:  ldloc.s    i
      IL_00ba:  unbox      [mscorlib]System.Int32
      IL_00bf:  ldind.i4
      IL_00c0:  ldc.i4.2
      IL_00c1:  add
      IL_00c2:  stloc.s    V_6
      IL_00c4:  ldloc.s   V_6
      IL_00c6:  box       [mscorlib]System.Int32
      IL_00cb:  stloc.s    i
      IL_00cd:  ldloc.s    i
      IL_00cf:  unbox      [mscorlib]System.Int32
      IL_00d4:  ldind.i4
      IL_00d5:  ldc.i4     0xc8
      IL_00da:  ble        IL_003b

      IL_00df:  ldloc.1
      IL_00e0:  ret
    } // end of method 'SinCalc_try_seh_il::mySin'

    .method family hidebysig static class System.Object 
            CalcAndCheck(class System.Object Angle,
                         class System.Object Expected) il managed
    {
      .maxstack  2
      .locals ([0] class System.Object mistake,
               [1] class System.Object Result1,
               [2] class System.Object Result2,
               [3] float64 V_3)
      IL_0000:  ldc.r8     1.0000000000000001e-009
      IL_0009:  stloc.3
      IL_000a:  ldloc.s   V_3
      IL_000c:  box       [mscorlib]System.Double
      IL_0011:  stloc.0
      IL_0012:  ldarg.0
      IL_0013:  unbox      [mscorlib]System.Double
      IL_0018:  ldind.r8
      IL_0019:  call       float64 [mscorlib]System.Math::Sin(float64)
      IL_001e:  stloc.3
      IL_001f:  ldloc.s   V_3
      IL_0021:  box       [mscorlib]System.Double
      IL_0026:  stloc.1
      IL_0027:  ldarg.0
      IL_0028:  call       class System.Object SinCalc_try_seh_il.SinCalc_try_seh_il::mySin(class System.Object)
      IL_002d:  unbox      [mscorlib]System.Double
      IL_0032:  ldind.r8
      IL_0033:  stloc.3
      IL_0034:  ldloc.s   V_3
      IL_0036:  box       [mscorlib]System.Double
      IL_003b:  stloc.2
      IL_003c:  ldloc.1
      IL_003d:  unbox      [mscorlib]System.Double
      IL_0042:  ldind.r8
      IL_0043:  ldloc.2
      IL_0044:  unbox      [mscorlib]System.Double
      IL_0049:  ldind.r8
      IL_004a:  sub
      IL_004b:  call       float64 [mscorlib]System.Math::Abs(float64)
      IL_0050:  ldloc.0
      IL_0051:  unbox      [mscorlib]System.Double
      IL_0056:  ldind.r8
      IL_0057:  ble.un.s   IL_0073

      IL_0059:  ldloc.1
      IL_005a:  unbox      [mscorlib]System.Double
      IL_005f:  ldind.r8
      IL_0060:  ldloc.2
      IL_0061:  unbox      [mscorlib]System.Double
      IL_0066:  ldind.r8
      IL_0067:  sub
      IL_0068:  call       float64 [mscorlib]System.Math::Abs(float64)
      IL_006d:  newobj     instance void SinCalc_try_seh_il.MistakeException::.ctor(float64)
      IL_0072:  throw

      IL_0073:  ldloc.1
      IL_0074:  unbox      [mscorlib]System.Double
      IL_0079:  ldind.r8
      IL_007a:  ldarg.1
      IL_007b:  unbox      [mscorlib]System.Double
      IL_0080:  ldind.r8
      IL_0081:  sub
      IL_0082:  call       float64 [mscorlib]System.Math::Abs(float64)
      IL_0087:  ldloc.0
      IL_0088:  unbox      [mscorlib]System.Double
      IL_008d:  ldind.r8
      IL_008e:  ble.un.s   IL_00aa

      IL_0090:  ldloc.1
      IL_0091:  unbox      [mscorlib]System.Double
      IL_0096:  ldind.r8
      IL_0097:  ldarg.1
      IL_0098:  unbox      [mscorlib]System.Double
      IL_009d:  ldind.r8
      IL_009e:  sub
      IL_009f:  call       float64 [mscorlib]System.Math::Abs(float64)
      IL_00a4:  newobj     instance void SinCalc_try_seh_il.MistakeException::.ctor(float64)
      IL_00a9:  throw

      IL_00aa:  ldloc.2
      IL_00ab:  unbox      [mscorlib]System.Double
      IL_00b0:  ldind.r8
      IL_00b1:  ldarg.1
      IL_00b2:  unbox      [mscorlib]System.Double
      IL_00b7:  ldind.r8
      IL_00b8:  sub
      IL_00b9:  call       float64 [mscorlib]System.Math::Abs(float64)
      IL_00be:  ldloc.0
      IL_00bf:  unbox      [mscorlib]System.Double
      IL_00c4:  ldind.r8
      IL_00c5:  ble.un.s   IL_00e1

      IL_00c7:  ldloc.2
      IL_00c8:  unbox      [mscorlib]System.Double
      IL_00cd:  ldind.r8
      IL_00ce:  ldarg.1
      IL_00cf:  unbox      [mscorlib]System.Double
      IL_00d4:  ldind.r8
      IL_00d5:  sub
      IL_00d6:  call       float64 [mscorlib]System.Math::Abs(float64)
      IL_00db:  newobj     instance void SinCalc_try_seh_il.MistakeException::.ctor(float64)
      IL_00e0:  throw

      IL_00e1:  ldloc.1
      IL_00e2:  ret
    } // end of method 'SinCalc_try_seh_il::CalcAndCheck'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  5
      .locals ([0] class System.Object i,
               [1] class System.Object Angle,
               [2] class System.Object[] testresults,
               [3] class System.Object total1,
               [4] class System.Object total2,
               [5] class System.Object total3,
               [6] class SinCalc_try_seh_il.MistakeException ex,
               [7] float64 V_7,
               [8] int32 V_8,
               [9] class System.String[] V_9)
      IL_0000:  ldc.i4.s   10
      IL_0002:  newarr     [mscorlib]System.Object
      IL_0007:  stloc.2
      IL_0008:  ldloc.2
      IL_0009:  ldc.i4.0
      IL_000a:  ldc.r8     0.
      IL_0013:  stloc.s    V_7
      IL_0015:  ldloc.s   V_7
      IL_0017:  box       [mscorlib]System.Double
      IL_001c:  stelem.ref
      IL_001d:  ldloc.2
      IL_001e:  ldc.i4.1
      IL_001f:  ldc.r8     0.30901699399999999
      IL_0028:  stloc.s    V_7
      IL_002a:  ldloc.s   V_7
      IL_002c:  box       [mscorlib]System.Double
      IL_0031:  stelem.ref
      IL_0032:  ldloc.2
      IL_0033:  ldc.i4.2
      IL_0034:  ldc.r8     0.58778525199999998
      IL_003d:  stloc.s    V_7
      IL_003f:  ldloc.s   V_7
      IL_0041:  box       [mscorlib]System.Double
      IL_0046:  stelem.ref
      IL_0047:  ldloc.2
      IL_0048:  ldc.i4.3
      IL_0049:  ldc.r8     0.80901699400000004
      IL_0052:  stloc.s    V_7
      IL_0054:  ldloc.s   V_7
      IL_0056:  box       [mscorlib]System.Double
      IL_005b:  stelem.ref
      IL_005c:  ldloc.2
      IL_005d:  ldc.i4.4
      IL_005e:  ldc.r8     0.95105651599999996
      IL_0067:  stloc.s    V_7
      IL_0069:  ldloc.s   V_7
      IL_006b:  box       [mscorlib]System.Double
      IL_0070:  stelem.ref
      IL_0071:  ldloc.2
      IL_0072:  ldc.i4.5
      IL_0073:  ldc.r8     1.
      IL_007c:  stloc.s    V_7
      IL_007e:  ldloc.s   V_7
      IL_0080:  box       [mscorlib]System.Double
      IL_0085:  stelem.ref
      IL_0086:  ldloc.2
      IL_0087:  ldc.i4.6
      IL_0088:  ldc.r8     0.95105651599999996
      IL_0091:  stloc.s    V_7
      IL_0093:  ldloc.s   V_7
      IL_0095:  box       [mscorlib]System.Double
      IL_009a:  stelem.ref
      IL_009b:  ldloc.2
      IL_009c:  ldc.i4.7
      IL_009d:  ldc.r8     0.80901699400000004
      IL_00a6:  stloc.s    V_7
      IL_00a8:  ldloc.s   V_7
      IL_00aa:  box       [mscorlib]System.Double
      IL_00af:  stelem.ref
      IL_00b0:  ldloc.2
      IL_00b1:  ldc.i4.8
      IL_00b2:  ldc.r8     0.58778525199999998
      IL_00bb:  stloc.s    V_7
      IL_00bd:  ldloc.s   V_7
      IL_00bf:  box       [mscorlib]System.Double
      IL_00c4:  stelem.ref
      IL_00c5:  ldloc.2
      IL_00c6:  ldc.i4.s   9
      IL_00c8:  ldc.r8     0.30901699399999999
      IL_00d1:  stloc.s    V_7
      IL_00d3:  ldloc.s   V_7
      IL_00d5:  box       [mscorlib]System.Double
      IL_00da:  stelem.ref
      IL_00db:  ldc.r8     0.
      IL_00e4:  stloc.s    V_7
      IL_00e6:  ldloc.s   V_7
      IL_00e8:  box       [mscorlib]System.Double
      IL_00ed:  stloc.3
      IL_00ee:  ldc.r8     0.
      IL_00f7:  stloc.s    V_7
      IL_00f9:  ldloc.s   V_7
      IL_00fb:  box       [mscorlib]System.Double
      IL_0100:  stloc.s    total2
      IL_0102:  ldc.r8     0.
      IL_010b:  stloc.s    V_7
      IL_010d:  ldloc.s   V_7
      IL_010f:  box       [mscorlib]System.Double
      IL_0114:  stloc.s    total3
      IL_0116:  ldc.i4.0
      IL_0117:  stloc.s    V_8
      IL_0119:  ldloc.s   V_8
      IL_011b:  box       [mscorlib]System.Int32
      IL_0120:  stloc.0
      IL_0121:  br         IL_026d

      .try
      {
        .try
        {
          IL_0126:  ldsfld     class System.Object SinCalc_try_seh_il.SinCalc_try_seh_il::PI
          IL_012b:  unbox      [mscorlib]System.Double
          IL_0130:  ldind.r8
          IL_0131:  ldloc.0
          IL_0132:  unbox      [mscorlib]System.Int32
          IL_0137:  ldind.i4
          IL_0138:  conv.r8
          IL_0139:  ldc.r8     10.
          IL_0142:  div
          IL_0143:  mul
          IL_0144:  stloc.s    V_7
          IL_0146:  ldloc.s   V_7
          IL_0148:  box       [mscorlib]System.Double
          IL_014d:  stloc.1
          IL_014e:  ldloc.s    total2
          IL_0150:  unbox      [mscorlib]System.Double
          IL_0155:  ldind.r8
          IL_0156:  ldloc.1
          IL_0157:  unbox      [mscorlib]System.Double
          IL_015c:  ldind.r8
          IL_015d:  add
          IL_015e:  stloc.s    V_7
          IL_0160:  ldloc.s   V_7
          IL_0162:  box       [mscorlib]System.Double
          IL_0167:  stloc.s    total2
          IL_0169:  ldloc.s    total3
          IL_016b:  unbox      [mscorlib]System.Double
          IL_0170:  ldind.r8
          IL_0171:  ldloc.1
          IL_0172:  ldloc.2
          IL_0173:  ldloc.0
          IL_0174:  unbox      [mscorlib]System.Int32
          IL_0179:  ldind.i4
          IL_017a:  ldelem.ref
          IL_017b:  unbox      [mscorlib]System.Double
          IL_0180:  ldind.r8
          IL_0181:  ldc.r8     4.0000000000000001e-010
          IL_018a:  ldloc.0
          IL_018b:  unbox      [mscorlib]System.Int32
          IL_0190:  ldind.i4
          IL_0191:  conv.r8
          IL_0192:  mul
          IL_0193:  add
          IL_0194:  stloc.s    V_7
          IL_0196:  ldloc.s   V_7
          IL_0198:  box       [mscorlib]System.Double
          IL_019d:  call       class System.Object SinCalc_try_seh_il.SinCalc_try_seh_il::CalcAndCheck(class System.Object,
                                                                                 class System.Object)
          IL_01a2:  unbox      [mscorlib]System.Double
          IL_01a7:  ldind.r8
          IL_01a8:  add
          IL_01a9:  stloc.s    V_7
          IL_01ab:  ldloc.s   V_7
          IL_01ad:  box       [mscorlib]System.Double
          IL_01b2:  stloc.s    total3
          IL_01b4:  ldstr      "OK"
          IL_01b9:  call       void [System.Console]System.Console::WriteLine(class System.String)
          IL_01be:  leave.s    IL_01fe

        }  // end .try
        catch SinCalc_try_seh_il.MistakeException 
        {
          IL_01c0:  stloc.s    ex
          IL_01c2:  ldstr      "Mistake is "
          IL_01c7:  ldloc.s    ex
          IL_01c9:  ldfld      class System.Object SinCalc_try_seh_il.MistakeException::mistake
          IL_01ce:  callvirt   instance class System.String [mscorlib]System.Object::ToString()
          IL_01d3:  call       class System.String [mscorlib]System.String::Concat(class System.String,
                                                                                   class System.String)
          IL_01d8:  call       void [System.Console]System.Console::WriteLine(class System.String)
          IL_01dd:  ldloc.3
          IL_01de:  unbox      [mscorlib]System.Double
          IL_01e3:  ldind.r8
          IL_01e4:  ldloc.s    ex
          IL_01e6:  ldfld      class System.Object SinCalc_try_seh_il.MistakeException::mistake
          IL_01eb:  unbox      [mscorlib]System.Double
          IL_01f0:  ldind.r8
          IL_01f1:  add
          IL_01f2:  stloc.s    V_7
          IL_01f4:  ldloc.s   V_7
          IL_01f6:  box       [mscorlib]System.Double
          IL_01fb:  stloc.3
          IL_01fc:  leave.s    IL_01fe

        }  // end handler
        IL_01fe:  leave.s    IL_025a

      }  // end .try
      finally
      {
        IL_0200:  ldc.i4.6
        IL_0201:  newarr     [mscorlib]System.String
        IL_0206:  stloc.s    V_9
        IL_0208:  ldloc.s    V_9
        IL_020a:  ldc.i4.0
        IL_020b:  ldstr      "Current totals "
        IL_0210:  stelem.ref
        IL_0211:  ldloc.s    V_9
        IL_0213:  ldc.i4.1
        IL_0214:  ldloc.3
        IL_0215:  callvirt   instance class System.String [mscorlib]System.Object::ToString()
        IL_021a:  stelem.ref
        IL_021b:  ldloc.s    V_9
        IL_021d:  ldc.i4.2
        IL_021e:  ldstr      " and "
        IL_0223:  stelem.ref
        IL_0224:  ldloc.s    V_9
        IL_0226:  ldc.i4.3
        IL_0227:  ldloc.s    total2
        IL_0229:  unbox      [mscorlib]System.Double
        IL_022e:  ldind.r8
        IL_022f:  stloc.s    V_7
        IL_0231:  ldloca.s   V_7
        IL_0233:  call       instance class System.String [mscorlib]System.Double::ToString()
        IL_0238:  stelem.ref
        IL_0239:  ldloc.s    V_9
        IL_023b:  ldc.i4.4
        IL_023c:  ldstr      " and "
        IL_0241:  stelem.ref
        IL_0242:  ldloc.s    V_9
        IL_0244:  ldc.i4.5
        IL_0245:  ldloc.s    total3
        IL_0247:  callvirt   instance class System.String [mscorlib]System.Object::ToString()
        IL_024c:  stelem.ref
        IL_024d:  ldloc.s    V_9
        IL_024f:  call       class System.String [mscorlib]System.String::Concat(class System.String[])
        IL_0254:  call       void [System.Console]System.Console::WriteLine(class System.String)
        IL_0259:  endfinally
      }  // end handler
      IL_025a:  ldloc.0
      IL_025b:  unbox      [mscorlib]System.Int32
      IL_0260:  ldind.i4
      IL_0261:  ldc.i4.1
      IL_0262:  add
      IL_0263:  stloc.s    V_8
      IL_0265:  ldloc.s   V_8
      IL_0267:  box       [mscorlib]System.Int32
      IL_026c:  stloc.0
      IL_026d:  ldloc.0
      IL_026e:  unbox      [mscorlib]System.Int32
      IL_0273:  ldind.i4
      IL_0274:  ldc.i4.s   10
      IL_0276:  blt        IL_0126

      IL_027b:  ldstr      "**** PASSED ****"
      IL_0280:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0285:  ldc.i4    0x64
      IL_0286:  ret
    } // end of method 'SinCalc_try_seh_il::Main'

    .method public hidebysig specialname rtspecialname static 
            void .cctor() il managed
    {
      .maxstack  1
      .locals (float64 V_0)
      IL_0000:  ldc.r8     3.1415926535897931
      IL_0009:  stloc.0
      IL_000a:  ldloc.s   V_0
      IL_000c:  box       [mscorlib]System.Double
      IL_0011:  stsfld     class System.Object SinCalc_try_seh_il.SinCalc_try_seh_il::PI
      IL_0016:  ret
    } // end of method 'SinCalc_try_seh_il::.cctor'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'SinCalc_try_seh_il::.ctor'

  } // end of class 'SinCalc_try_seh_il'

} // end of namespace 'SinCalc_try_seh_il'

