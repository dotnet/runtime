// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
.module 'sinlib_il.dll'
.assembly extern legacy library mscorlib { auto }
.assembly 'sinlib_il' {}
.namespace SinCalcLib
{
  .class value public auto ansi sealed PiVal extends [mscorlib]System.ValueType
  {
    .field public float64 Value
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(float64 v) il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  initobj    SinCalcLib.PiVal
      IL_0007:  ldarg.0
      IL_0008:  ldarg.1
      IL_0009:  stfld      float64 SinCalcLib.PiVal::Value
      IL_000e:  ret
    } 

  } 

  .class public auto ansi SinCalcLib extends [mscorlib]System.Object
  {
    .field public static class System.Object PI
    .method public hidebysig static class System.Object 
            mySin(class System.Object Angle) il managed
    {
      .maxstack  3
      .locals (class System.Object V_0,
               class System.Object V_1,
               class System.Object V_2,
               class System.Object V_3,
               class System.Object V_4,
               float64 V_5,
               int32 V_6)
      IL_0000:  ldc.r8     1.
      IL_0009:  stloc.s    V_5
      IL_000b:  ldloc.s   V_5
      IL_000d:  box  [mscorlib]System.Double
      IL_0012:  stloc.3
      IL_0013:  ldarg.0
      IL_0014:  dup
      IL_0015:  stloc.2
      IL_0016:  stloc.0
      IL_0017:  ldc.r8     0.
      IL_0020:  stloc.s    V_5
      IL_0022:  ldloc.s   V_5
      IL_0024:  box  [mscorlib]System.Double
      IL_0029:  stloc.1
      IL_002a:  ldc.i4.1
      IL_002b:  stloc.s    V_6
      IL_002d:  ldloc.s   V_6
      IL_002f:  box  [mscorlib]System.Int32
      IL_0034:  stloc.s    V_4
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
      IL_004e:  box  [mscorlib]System.Double
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
      IL_0070:  box  [mscorlib]System.Double
      IL_0075:  stloc.0
      IL_0076:  ldloc.3
      IL_0077:  unbox      [mscorlib]System.Double
      IL_007c:  ldind.r8
      IL_007d:  ldloc.s    V_4
      IL_007f:  unbox      [mscorlib]System.Int32
      IL_0084:  ldind.i4
      IL_0085:  ldc.i4.1
      IL_0086:  add
      IL_0087:  conv.r8
      IL_0088:  mul
      IL_0089:  ldloc.s    V_4
      IL_008b:  unbox      [mscorlib]System.Int32
      IL_0090:  ldind.i4
      IL_0091:  ldc.i4.2
      IL_0092:  add
      IL_0093:  conv.r8
      IL_0094:  mul
      IL_0095:  stloc.s    V_5
      IL_0097:  ldloc.s   V_5
      IL_0099:  box  [mscorlib]System.Double
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
      IL_00b2:  box  [mscorlib]System.Double
      IL_00b7:  stloc.2
      IL_00b8:  ldloc.s    V_4
      IL_00ba:  unbox      [mscorlib]System.Int32
      IL_00bf:  ldind.i4
      IL_00c0:  ldc.i4.2
      IL_00c1:  add
      IL_00c2:  stloc.s    V_6
      IL_00c4:  ldloc.s   V_6
      IL_00c6:  box  [mscorlib]System.Int32
      IL_00cb:  stloc.s    V_4
      IL_00cd:  ldloc.s    V_4
      IL_00cf:  unbox      [mscorlib]System.Int32
      IL_00d4:  ldind.i4
      IL_00d5:  ldc.i4     0xc8
      IL_00da:  ble        IL_003b

      IL_00df:  ldloc.1
      IL_00e0:  ret
    } 

    .method public hidebysig specialname rtspecialname static 
            void .cctor() il managed
    {
      .maxstack  2
      .locals (value class SinCalcLib.PiVal V_0)
      IL_0000:  ldc.r8     3.1415926535897931
      IL_0009:  newobj     instance void SinCalcLib.PiVal::.ctor(float64)
      IL_000e:  stloc.0
      IL_000f:  ldloc.s   V_0
      IL_0011:  box  SinCalcLib.PiVal
      IL_0016:  stsfld     class System.Object SinCalcLib.SinCalcLib::PI
      IL_001b:  ret
    } 

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } 

  } 

} 
