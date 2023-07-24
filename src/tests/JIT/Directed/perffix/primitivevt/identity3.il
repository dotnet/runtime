// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib {}
.assembly extern nativeinthelper {}
.assembly ASSEMBLY_NAME {}
.namespace PrimitiveVT
{
  .class public auto ansi beforefieldinit Identity3
         extends [mscorlib]System.Object
  {
    .field private static class [mscorlib]System.Random rand
    .field private valuetype [nativeinthelper]PrimitiveVT.VT1A vt1a
    .field private static valuetype [nativeinthelper]PrimitiveVT.VT1A x
    .method public hidebysig static int32 Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  5
      .locals (native int V_0,
               valuetype [nativeinthelper]PrimitiveVT.VT1B V_1,
               class PrimitiveVT.Identity3 V_2,
               native int V_3,
               native int V_4,
               native int V_5,
               native int V_6,
               native int V_7,
               valuetype [nativeinthelper]PrimitiveVT.VT1B V_8,
               valuetype [nativeinthelper]PrimitiveVT.VT1B V_9,
               native int V_10,
               valuetype [nativeinthelper]PrimitiveVT.VT1B V_11,
               valuetype [nativeinthelper]PrimitiveVT.VT1B* V_12,
               valuetype [nativeinthelper]PrimitiveVT.VT1A[0...,0...][] V_13,
               native int V_14)
      IL_0000:  ldsfld     class [mscorlib]System.Random PrimitiveVT.Identity3::rand
      IL_0005:  callvirt   instance int32 [mscorlib]System.Random::Next()
      IL_000a:  stloc.0
      IL_000b:  newobj     instance void PrimitiveVT.Identity3::.ctor()
      IL_0010:  stloc.2
      IL_0011:  ldloc.2
      IL_0012:  ldflda     valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::vt1a
      IL_0017:  ldloc.0
      IL_0018:  stfld      native int [nativeinthelper]PrimitiveVT.VT1A::m
      IL_001d:  ldloc.2
      IL_001e:  ldfld      valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::vt1a
      IL_0023:  box        [nativeinthelper]PrimitiveVT.VT1A
      IL_0028:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_002d:  ldtoken    [nativeinthelper]PrimitiveVT.VT1A
      IL_0032:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_0037:  beq.s      IL_0060

      IL_0039:  ldstr      "FAILED, t.vt1a.GetType()!=typeof(VT1A)"
      IL_003e:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0043:  ldloc.2
      IL_0044:  ldfld      valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::vt1a
      IL_0049:  box        [nativeinthelper]PrimitiveVT.VT1A
      IL_004e:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0053:  call       void [System.Console]System.Console::WriteLine(object)
      IL_0058:  ldc.i4.1
      IL_0059:  stloc.s    V_14
      IL_005b:  br         IL_0624

      IL_0060:  ldloc.2
      IL_0061:  ldfld      valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::vt1a
      IL_0066:  box        [nativeinthelper]PrimitiveVT.VT1A
      IL_006b:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0070:  callvirt   instance bool [mscorlib]System.Type::get_IsPrimitive()
      IL_0075:  brfalse.s  IL_009e

      IL_0077:  ldstr      "FAILED, t.vt1a.GetType().IsPrimitive"
      IL_007c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0081:  ldloc.2
      IL_0082:  ldfld      valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::vt1a
      IL_0087:  box        [nativeinthelper]PrimitiveVT.VT1A
      IL_008c:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0091:  call       void [System.Console]System.Console::WriteLine(object)
      IL_0096:  ldc.i4.1
      IL_0097:  stloc.s    V_14
      IL_0099:  br         IL_0624

      IL_009e:  ldloc.2
      IL_009f:  ldflda     valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::vt1a
      IL_00a4:  ldfld      native int [nativeinthelper]PrimitiveVT.VT1A::m
      IL_00a9:  box        [mscorlib]System.Int32
      IL_00ae:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_00b3:  ldtoken    [mscorlib]System.Int32
      IL_00b8:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_00bd:  beq.s      IL_00eb

      IL_00bf:  ldstr      "FAILED, t.vt1a.m.GetType()!=typeof(System.Int32)"
      IL_00c4:  call       void [System.Console]System.Console::WriteLine(string)
      IL_00c9:  ldloc.2
      IL_00ca:  ldflda     valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::vt1a
      IL_00cf:  ldfld      native int [nativeinthelper]PrimitiveVT.VT1A::m
      IL_00d4:  box        [mscorlib]System.Int32
      IL_00d9:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_00de:  call       void [System.Console]System.Console::WriteLine(object)
      IL_00e3:  ldc.i4.1
      IL_00e4:  stloc.s    V_14
      IL_00e6:  br         IL_0624

      IL_00eb:  ldloc.2
      IL_00ec:  ldfld      valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::vt1a
      IL_00f1:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Explicit(valuetype [nativeinthelper]PrimitiveVT.VT1A)
      IL_00f6:  stloc.1
      IL_00f7:  ldloc.1
      IL_00f8:  box        [nativeinthelper]PrimitiveVT.VT1B
      IL_00fd:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0102:  ldtoken    [nativeinthelper]PrimitiveVT.VT1B
      IL_0107:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_010c:  beq.s      IL_0130

      IL_010e:  ldstr      "FAILED vt1b.GetType()!=typeof(VT1B)"
      IL_0113:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0118:  ldloc.1
      IL_0119:  box        [nativeinthelper]PrimitiveVT.VT1B
      IL_011e:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0123:  call       void [System.Console]System.Console::WriteLine(object)
      IL_0128:  ldc.i4.1
      IL_0129:  stloc.s    V_14
      IL_012b:  br         IL_0624

      IL_0130:  ldloc.1
      IL_0131:  box        [nativeinthelper]PrimitiveVT.VT1B
      IL_0136:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_013b:  callvirt   instance bool [mscorlib]System.Type::get_IsValueType()
      IL_0140:  brtrue.s   IL_0164

      IL_0142:  ldstr      "FAILED !vt1b.GetType().IsValueType()"
      IL_0147:  call       void [System.Console]System.Console::WriteLine(string)
      IL_014c:  ldloc.1
      IL_014d:  box        [nativeinthelper]PrimitiveVT.VT1B
      IL_0152:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0157:  call       void [System.Console]System.Console::WriteLine(object)
      IL_015c:  ldc.i4.1
      IL_015d:  stloc.s    V_14
      IL_015f:  br         IL_0624

      IL_0164:  ldloc.2
      IL_0165:  ldloc.1
      IL_0166:  call       valuetype [nativeinthelper]PrimitiveVT.VT1A [nativeinthelper]PrimitiveVT.VT1B::op_Explicit(valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_016b:  stfld      valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::vt1a
      IL_0170:  ldloc.2
      IL_0171:  ldfld      valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::vt1a
      IL_0176:  box        [nativeinthelper]PrimitiveVT.VT1A
      IL_017b:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0180:  ldtoken    [nativeinthelper]PrimitiveVT.VT1A
      IL_0185:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_018a:  beq.s      IL_01b3

      IL_018c:  ldstr      "FAILED t.vt1a.GetType()!=typeof(VT1A)"
      IL_0191:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0196:  ldloc.2
      IL_0197:  ldfld      valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::vt1a
      IL_019c:  box        [nativeinthelper]PrimitiveVT.VT1A
      IL_01a1:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_01a6:  call       void [System.Console]System.Console::WriteLine(object)
      IL_01ab:  ldc.i4.1
      IL_01ac:  stloc.s    V_14
      IL_01ae:  br         IL_0624

      IL_01b3:  ldloc.1
      IL_01b4:  call       native int [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_01b9:  stloc.3
      IL_01ba:  ldloc.3
      IL_01bb:  ldloc.0
      IL_01bc:  beq.s      IL_01d0

      IL_01be:  ldstr      "FAILED, b!=a"
      IL_01c3:  call       void [System.Console]System.Console::WriteLine(string)
      IL_01c8:  ldc.i4.1
      IL_01c9:  stloc.s    V_14
      IL_01cb:  br         IL_0624

      IL_01d0:  ldloc.2
      IL_01d1:  ldfld      valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::vt1a
      IL_01d6:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Explicit(valuetype [nativeinthelper]PrimitiveVT.VT1A)
      IL_01db:  call       native int [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_01e0:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(native int)
      IL_01e5:  call       valuetype [nativeinthelper]PrimitiveVT.VT1A [nativeinthelper]PrimitiveVT.VT1B::op_Explicit(valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_01ea:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Explicit(valuetype [nativeinthelper]PrimitiveVT.VT1A)
      IL_01ef:  call       native int [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_01f4:  stloc.s    V_4
      IL_01f6:  ldloc.s    V_4
      IL_01f8:  ldloc.3
      IL_01f9:  beq.s      IL_020d

      IL_01fb:  ldstr      "FAILED, c!=b"
      IL_0200:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0205:  ldc.i4.1
      IL_0206:  stloc.s    V_14
      IL_0208:  br         IL_0624

      IL_020d:  ldsfld     class [mscorlib]System.Random PrimitiveVT.Identity3::rand
      IL_0212:  callvirt   instance int32 [mscorlib]System.Random::Next()
      IL_0217:  stloc.s    V_5
      IL_0219:  ldc.i4     0x80000000
      IL_021e:  stloc.s    V_6
      IL_0220:  ldc.i4     0x3fffffff
      IL_0225:  stloc.s    V_7
      IL_0227:  ldsflda    valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::x
      IL_022c:  initobj    [nativeinthelper]PrimitiveVT.VT1A
      IL_0232:  ldloca.s   V_8
      IL_0234:  ldloc.s    V_6
      IL_0236:  call       instance void [nativeinthelper]PrimitiveVT.VT1B::.ctor(native int)
      IL_023b:  ldsflda    valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::x
      IL_0240:  ldloc.s    V_5
      IL_0242:  stfld      native int [nativeinthelper]PrimitiveVT.VT1A::m
      IL_0247:  ldsfld     valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::x
      IL_024c:  ldloc.s    V_8
      IL_024e:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Multiply(valuetype [nativeinthelper]PrimitiveVT.VT1A,
                                                                                                    valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_0253:  ldloc.s    V_7
      IL_0255:  newobj     instance void [nativeinthelper]PrimitiveVT.VT1B::.ctor(native int)
      IL_025a:  call       native int [nativeinthelper]PrimitiveVT.VT1B::op_Subtraction(valuetype [nativeinthelper]PrimitiveVT.VT1B,
                                                                          valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_025f:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(native int)
      IL_0264:  ldloc.s    V_8
      IL_0266:  call       native int [nativeinthelper]PrimitiveVT.VT1B::op_Addition(valuetype [nativeinthelper]PrimitiveVT.VT1B,
                                                                       valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_026b:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(native int)
      IL_0270:  ldsfld     valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::x
      IL_0275:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Explicit(valuetype [nativeinthelper]PrimitiveVT.VT1A)
      IL_027a:  call       native int [nativeinthelper]PrimitiveVT.VT1B::op_Addition(valuetype [nativeinthelper]PrimitiveVT.VT1B,
                                                                       valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_027f:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(native int)
      IL_0284:  ldloc.s    V_7
      IL_0286:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(native int)
      IL_028b:  call       native int [nativeinthelper]PrimitiveVT.VT1B::op_Addition(valuetype [nativeinthelper]PrimitiveVT.VT1B,
                                                                       valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_0290:  ldloc.s    V_8
      IL_0292:  ldsfld     valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::x
      IL_0297:  call       native int [nativeinthelper]PrimitiveVT.VT1B::op_Multiply(valuetype [nativeinthelper]PrimitiveVT.VT1B,
                                                                       valuetype [nativeinthelper]PrimitiveVT.VT1A)
      IL_029c:  add
      IL_029d:  ldsfld     valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::x
      IL_02a2:  ldloc.s    V_5
      IL_02a4:  ldc.i4.2
      IL_02a5:  rem
      IL_02a6:  brfalse.s  IL_02b1

      IL_02a8:  ldloc.s    V_5
      IL_02aa:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(native int)
      IL_02af:  br.s       IL_02ba

      IL_02b1:  ldloc.s    V_5
      IL_02b3:  ldc.i4.2
      IL_02b4:  div
      IL_02b5:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(native int)
      IL_02ba:  call       float32 [nativeinthelper]PrimitiveVT.VT1B::op_Division(valuetype [nativeinthelper]PrimitiveVT.VT1A,
                                                                         valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_02bf:  conv.i4
      IL_02c0:  add
      IL_02c1:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(native int)
      IL_02c6:  stloc.s    V_9
      IL_02c8:  ldloc.s    V_8
      IL_02ca:  ldsfld     valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::x
      IL_02cf:  call       native int [nativeinthelper]PrimitiveVT.VT1B::op_Multiply(valuetype [nativeinthelper]PrimitiveVT.VT1B,
                                                                       valuetype [nativeinthelper]PrimitiveVT.VT1A)
      IL_02d4:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(native int)
      IL_02d9:  ldsfld     valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::x
      IL_02de:  ldloc.s    V_8
      IL_02e0:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Multiply(valuetype [nativeinthelper]PrimitiveVT.VT1A,
                                                                                                    valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_02e5:  call       native int [nativeinthelper]PrimitiveVT.VT1B::op_Addition(valuetype [nativeinthelper]PrimitiveVT.VT1B,
                                                                       valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_02ea:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(native int)
      IL_02ef:  ldloc.s    V_8
      IL_02f1:  call       native int [nativeinthelper]PrimitiveVT.VT1B::op_Addition(valuetype [nativeinthelper]PrimitiveVT.VT1B,
                                                                       valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_02f6:  ldloc.s    V_5
      IL_02f8:  add
      IL_02f9:  ldloc.s    V_5
      IL_02fb:  ldloc.s    V_5
      IL_02fd:  ldc.i4.2
      IL_02fe:  rem
      IL_02ff:  brfalse.s  IL_030a

      IL_0301:  ldloc.s    V_5
      IL_0303:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(native int)
      IL_0308:  br.s       IL_0313

      IL_030a:  ldloc.s    V_5
      IL_030c:  ldc.i4.2
      IL_030d:  div
      IL_030e:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(native int)
      IL_0313:  call       native int [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_0318:  div
      IL_0319:  add
      IL_031a:  stloc.s    V_10
      IL_031c:  ldloc.s    V_9
      IL_031e:  call       native int [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_0323:  ldloc.s    V_10
      IL_0325:  beq.s      IL_034c

      IL_0327:  ldstr      "FAILED, u!=w"
      IL_032c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0331:  ldloc.s    V_9
      IL_0333:  call       native int [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_0338:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_033d:  ldloc.s    V_10
      IL_033f:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_0344:  ldc.i4.1
      IL_0345:  stloc.s    V_14
      IL_0347:  br         IL_0624

      IL_034c:  ldloc.s    V_9
      IL_034e:  box        [nativeinthelper]PrimitiveVT.VT1B
      IL_0353:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0358:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Type::get_UnderlyingSystemType()
      IL_035d:  ldtoken    [nativeinthelper]PrimitiveVT.VT1B
      IL_0362:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_0367:  beq.s      IL_0391

      IL_0369:  ldstr      "FAILED u.GetType().UnderlyingSystemType!=typeof(VT"
      + "1B)"
      IL_036e:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0373:  ldloc.s    V_9
      IL_0375:  box        [nativeinthelper]PrimitiveVT.VT1B
      IL_037a:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_037f:  callvirt   instance class [mscorlib]System.Type [mscorlib]System.Type::get_UnderlyingSystemType()
      IL_0384:  call       void [System.Console]System.Console::WriteLine(object)
      IL_0389:  ldc.i4.1
      IL_038a:  stloc.s    V_14
      IL_038c:  br         IL_0624

      IL_0391:  ldsfld     valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::x
      IL_0396:  ldloc.s    V_8
      IL_0398:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Multiply(valuetype [nativeinthelper]PrimitiveVT.VT1A,
                                                                                                    valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_039d:  box        [nativeinthelper]PrimitiveVT.VT1B
      IL_03a2:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_03a7:  ldtoken    [nativeinthelper]PrimitiveVT.VT1B
      IL_03ac:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_03b1:  beq.s      IL_03e0

      IL_03b3:  ldstr      "FAILED (x*y).GetType()!=typeof(VT1B)"
      IL_03b8:  call       void [System.Console]System.Console::WriteLine(string)
      IL_03bd:  ldsfld     valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::x
      IL_03c2:  ldloc.s    V_8
      IL_03c4:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Multiply(valuetype [nativeinthelper]PrimitiveVT.VT1A,
                                                                                                    valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_03c9:  box        [nativeinthelper]PrimitiveVT.VT1B
      IL_03ce:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_03d3:  call       void [System.Console]System.Console::WriteLine(object)
      IL_03d8:  ldc.i4.1
      IL_03d9:  stloc.s    V_14
      IL_03db:  br         IL_0624

      IL_03e0:  ldc.i4.3
      IL_03e1:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(native int)
      IL_03e6:  ldloc.s    V_8
      IL_03e8:  call       native int [nativeinthelper]PrimitiveVT.VT1B::op_Multiply(valuetype [nativeinthelper]PrimitiveVT.VT1B,
                                                                       valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_03ed:  box        [mscorlib]System.Int32
      IL_03f2:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_03f7:  ldtoken    [mscorlib]System.Int32
      IL_03fc:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_0401:  beq.s      IL_0431

      IL_0403:  ldstr      "FAILED (3*y).GetType()!=typeof(System.Int32)"
      IL_0408:  call       void [System.Console]System.Console::WriteLine(string)
      IL_040d:  ldc.i4.3
      IL_040e:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(native int)
      IL_0413:  ldloc.s    V_8
      IL_0415:  call       native int [nativeinthelper]PrimitiveVT.VT1B::op_Multiply(valuetype [nativeinthelper]PrimitiveVT.VT1B,
                                                                       valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_041a:  box        [mscorlib]System.Int32
      IL_041f:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0424:  call       void [System.Console]System.Console::WriteLine(object)
      IL_0429:  ldc.i4.1
      IL_042a:  stloc.s    V_14
      IL_042c:  br         IL_0624

      IL_0431:  ldsfld     valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::x
      IL_0436:  ldloc.s    V_8
      IL_0438:  call       float32 [nativeinthelper]PrimitiveVT.VT1B::op_Division(valuetype [nativeinthelper]PrimitiveVT.VT1A,
                                                                         valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_043d:  box        [mscorlib]System.Single
      IL_0442:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0447:  ldtoken    [mscorlib]System.Single
      IL_044c:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_0451:  beq.s      IL_0480

      IL_0453:  ldstr      "FAILED (x/y).GetType()!=typeof(System.Single)"
      IL_0458:  call       void [System.Console]System.Console::WriteLine(string)
      IL_045d:  ldsfld     valuetype [nativeinthelper]PrimitiveVT.VT1A PrimitiveVT.Identity3::x
      IL_0462:  ldloc.s    V_8
      IL_0464:  call       float32 [nativeinthelper]PrimitiveVT.VT1B::op_Division(valuetype [nativeinthelper]PrimitiveVT.VT1A,
                                                                         valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_0469:  box        [mscorlib]System.Single
      IL_046e:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0473:  call       void [System.Console]System.Console::WriteLine(object)
      IL_0478:  ldc.i4.1
      IL_0479:  stloc.s    V_14
      IL_047b:  br         IL_0624

      IL_0480:  ldc.i4.3
      IL_0481:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(native int)
      IL_0486:  stloc.s    V_11
      IL_0488:  br.s       IL_049c

      IL_048a:  ldloc.s    V_11
      IL_048c:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Increment(valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_0491:  stloc.s    V_11
      IL_0493:  ldloc.s    V_8
      IL_0495:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Increment(valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_049a:  stloc.s    V_8
      IL_049c:  ldloc.s    V_11
      IL_049e:  call       native int [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_04a3:  ldc.i4.s   10
      IL_04a5:  ble.s      IL_048a

      IL_04a7:  ldloc.s    V_8
      IL_04a9:  ldc.i4     0x80000000
      IL_04ae:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(native int)
      IL_04b3:  call       native int [nativeinthelper]PrimitiveVT.VT1B::op_Subtraction(valuetype [nativeinthelper]PrimitiveVT.VT1B,
                                                                          valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_04b8:  ldc.i4.8
      IL_04b9:  beq.s      IL_04e3

      IL_04bb:  ldstr      "FAILED, y-Int32.MinValue!=8"
      IL_04c0:  call       void [System.Console]System.Console::WriteLine(string)
      IL_04c5:  ldloc.s    V_8
      IL_04c7:  ldc.i4     0x80000000
      IL_04cc:  call       valuetype [nativeinthelper]PrimitiveVT.VT1B [nativeinthelper]PrimitiveVT.VT1B::op_Implicit(native int)
      IL_04d1:  call       native int [nativeinthelper]PrimitiveVT.VT1B::op_Subtraction(valuetype [nativeinthelper]PrimitiveVT.VT1B,
                                                                          valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_04d6:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_04db:  ldc.i4.1
      IL_04dc:  stloc.s    V_14
      IL_04de:  br         IL_0624

      IL_04e3:  sizeof     [nativeinthelper]PrimitiveVT.VT1B
      IL_04e9:  ldc.i4.1
      IL_04ea:  mul
      IL_04eb:  localloc
      IL_04ed:  stloc.s    V_12
      IL_04ef:  ldloc.s    V_12
      IL_04f1:  sizeof     [nativeinthelper]PrimitiveVT.VT1B
      IL_04f7:  add
      IL_04f8:  ldobj      [nativeinthelper]PrimitiveVT.VT1B
      IL_04fd:  box        [nativeinthelper]PrimitiveVT.VT1B
      IL_0502:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0507:  ldtoken    [nativeinthelper]PrimitiveVT.VT1B
      IL_050c:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_0511:  beq.s      IL_0542

      IL_0513:  ldstr      "FAILED (o+1)->GetType()!=typeof(VT1B)"
      IL_0518:  call       void [System.Console]System.Console::WriteLine(string)
      IL_051d:  ldloc.s    V_12
      IL_051f:  sizeof     [nativeinthelper]PrimitiveVT.VT1B
      IL_0525:  add
      IL_0526:  ldobj      [nativeinthelper]PrimitiveVT.VT1B
      IL_052b:  box        [nativeinthelper]PrimitiveVT.VT1B
      IL_0530:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_0535:  call       void [System.Console]System.Console::WriteLine(object)
      IL_053a:  ldc.i4.1
      IL_053b:  stloc.s    V_14
      IL_053d:  br         IL_0624

      IL_0542:  ldc.i4.2
      IL_0543:  newarr     valuetype [nativeinthelper]PrimitiveVT.VT1A[0...,0...]
      IL_0548:  stloc.s    V_13
      IL_054a:  ldloc.s    V_13
      IL_054c:  ldc.i4.1
      IL_054d:  ldc.i4.1
      IL_054e:  ldc.i4.2
      IL_054f:  newobj     instance void valuetype [nativeinthelper]PrimitiveVT.VT1A[0...,0...]::.ctor(int32,
                                                                                              int32)
      IL_0554:  stelem.ref
      IL_0555:  ldloc.s    V_13
      IL_0557:  ldc.i4.1
      IL_0558:  ldelem.ref
      IL_0559:  ldc.i4.0
      IL_055a:  ldc.i4.0
      IL_055b:  call       instance valuetype [nativeinthelper]PrimitiveVT.VT1A& valuetype [nativeinthelper]PrimitiveVT.VT1A[0...,0...]::Address(int32,
                                                                                                                               int32)
      IL_0560:  ldloc.s    V_12
      IL_0562:  ldobj      [nativeinthelper]PrimitiveVT.VT1B
      IL_0567:  call       valuetype [nativeinthelper]PrimitiveVT.VT1A [nativeinthelper]PrimitiveVT.VT1B::op_Explicit(valuetype [nativeinthelper]PrimitiveVT.VT1B)
      IL_056c:  stobj      [nativeinthelper]PrimitiveVT.VT1A
      IL_0571:  ldloc.s    V_13
      IL_0573:  ldc.i4.1
      IL_0574:  ldelem.ref
      IL_0575:  ldc.i4.0
      IL_0576:  ldc.i4.0
      IL_0577:  call       instance valuetype [nativeinthelper]PrimitiveVT.VT1A& valuetype [nativeinthelper]PrimitiveVT.VT1A[0...,0...]::Address(int32, int32)
      IL_057c:  ldobj      [nativeinthelper]PrimitiveVT.VT1A
      IL_0581:  box        [nativeinthelper]PrimitiveVT.VT1A
      IL_0586:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_058b:  ldtoken    [nativeinthelper]PrimitiveVT.VT1A
      IL_0590:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_0595:  beq.s      IL_05c5

      IL_0597:  ldstr      "FAILED arr[1][0,0].GetType()!=typeof(VT1A)"
      IL_059c:  call       void [System.Console]System.Console::WriteLine(string)
      IL_05a1:  ldloc.s    V_13
      IL_05a3:  ldc.i4.1
      IL_05a4:  ldelem.ref
      IL_05a5:  ldc.i4.0
      IL_05a6:  ldc.i4.0
      IL_05a7:  call       instance valuetype [nativeinthelper]PrimitiveVT.VT1A& valuetype [nativeinthelper]PrimitiveVT.VT1A[0...,0...]::Address(int32, int32)
      IL_05ac:  ldobj      [nativeinthelper]PrimitiveVT.VT1A
      IL_05b1:  box        [nativeinthelper]PrimitiveVT.VT1A
      IL_05b6:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_05bb:  call       void [System.Console]System.Console::WriteLine(object)
      IL_05c0:  ldc.i4.1
      IL_05c1:  stloc.s    V_14
      IL_05c3:  br.s       IL_0624

      IL_05c5:  ldloc.s    V_13
      IL_05c7:  ldc.i4.1
      IL_05c8:  ldelem.ref
      IL_05c9:  ldc.i4.0
      IL_05ca:  ldc.i4.0
      IL_05cb:  call       instance valuetype [nativeinthelper]PrimitiveVT.VT1A& valuetype [nativeinthelper]PrimitiveVT.VT1A[0...,0...]::Address(int32, int32)
      IL_05d0:  ldobj      [nativeinthelper]PrimitiveVT.VT1A
      IL_05d5:  box        [nativeinthelper]PrimitiveVT.VT1A
      IL_05da:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_05df:  callvirt   instance bool [mscorlib]System.Type::get_IsPrimitive()
      IL_05e4:  brfalse.s  IL_0614

      IL_05e6:  ldstr      "FAILED arr[1][0,0].GetType().IsPrimitive()"
      IL_05eb:  call       void [System.Console]System.Console::WriteLine(string)
      IL_05f0:  ldloc.s    V_13
      IL_05f2:  ldc.i4.1
      IL_05f3:  ldelem.ref
      IL_05f4:  ldc.i4.0
      IL_05f5:  ldc.i4.0
      IL_05f6:  call       instance valuetype [nativeinthelper]PrimitiveVT.VT1A& valuetype [nativeinthelper]PrimitiveVT.VT1A[0...,0...]::Address(int32, int32)
      IL_05fb:  ldobj      [nativeinthelper]PrimitiveVT.VT1A
      IL_0600:  box        [nativeinthelper]PrimitiveVT.VT1A
      IL_0605:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
      IL_060a:  call       void [System.Console]System.Console::WriteLine(object)
      IL_060f:  ldc.i4.1
      IL_0610:  stloc.s    V_14
      IL_0612:  br.s       IL_0624

      IL_0614:  ldstr      "PASSED"
      IL_0619:  call       void [System.Console]System.Console::WriteLine(string)
      IL_061e:  ldc.i4.s   100
      IL_0620:  stloc.s    V_14
      IL_0622:  br.s       IL_0624

      IL_0624:  ldloc.s    V_14
      IL_0626:  ret
    } 

    .method private hidebysig specialname rtspecialname static 
            void  .cctor() cil managed
    {
      .maxstack  8
      IL_0000:  newobj     instance void [mscorlib]System.Random::.ctor()
      IL_0005:  stsfld     class [mscorlib]System.Random PrimitiveVT.Identity3::rand
      IL_000a:  ret
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
