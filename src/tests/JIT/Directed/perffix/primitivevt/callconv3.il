// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib {}
.assembly extern helper {}
.assembly callconv3 {}
.namespace PrimitiveVT
{
  .class public auto ansi beforefieldinit CallConv3
         extends [mscorlib]System.Object
  {
    .field private static class [mscorlib]System.Random rand
    .field private valuetype [helper]PrimitiveVT.VT1A vt1a
    .field private static valuetype [helper]PrimitiveVT.VT1A x
    .method private hidebysig static int32 
            f1(valuetype [helper]PrimitiveVT.VT1B x,
               valuetype [helper]PrimitiveVT.VT1B y) cil managed
    {
      .maxstack  2
      .locals init (int32 V_0)
      IL_0000:  ldarga.s   x
      IL_0002:  ldfld      int32 [helper]PrimitiveVT.VT1B::m
      IL_0007:  ldarga.s   y
      IL_0009:  ldfld      int32 [helper]PrimitiveVT.VT1B::m
      IL_000e:  add
      IL_000f:  stloc.0
      IL_0010:  br.s       IL_0012

      IL_0012:  ldloc.0
      IL_0013:  ret
    } 

    .method private hidebysig instance valuetype [helper]PrimitiveVT.VT1B 
            f2a(valuetype [helper]PrimitiveVT.VT1A x,
                valuetype [helper]PrimitiveVT.VT1B y) cil managed
    {
      .maxstack  3
      .locals init (valuetype [helper]PrimitiveVT.VT1B V_0,
               valuetype [helper]PrimitiveVT.VT1B V_1)
      IL_0000:  ldloca.s   V_0
      IL_0002:  ldarga.s   x
      IL_0004:  ldfld      int32 [helper]PrimitiveVT.VT1A::m
      IL_0009:  ldarga.s   y
      IL_000b:  ldfld      int32 [helper]PrimitiveVT.VT1B::m
      IL_0010:  add
      IL_0011:  stfld      int32 [helper]PrimitiveVT.VT1B::m
      IL_0016:  ldloc.0
      IL_0017:  stloc.1
      IL_0018:  br.s       IL_001a

      IL_001a:  ldloc.1
      IL_001b:  ret
    } 

    .method private hidebysig instance valuetype [helper]PrimitiveVT.VT1B 
            f2b(valuetype [helper]PrimitiveVT.VT1A x,
                valuetype [helper]PrimitiveVT.VT1B y) cil managed
    {
      .maxstack  3
      .locals init (valuetype [helper]PrimitiveVT.VT1B V_0)
      IL_0000:  ldarg.0
      IL_0001:  ldarg.1
      IL_0002:  ldarg.2
      IL_0003:  tail. call       instance valuetype [helper]PrimitiveVT.VT1B PrimitiveVT.CallConv3::f2a(valuetype [helper]PrimitiveVT.VT1A,
                                                                                                  valuetype [helper]PrimitiveVT.VT1B)
      IL_000c:  ret
    } 

    .method private hidebysig instance valuetype [helper]PrimitiveVT.VT1B 
            f2(valuetype [helper]PrimitiveVT.VT1A x,
               valuetype [helper]PrimitiveVT.VT1B y) cil managed
    {
      .maxstack  3
      .locals init (valuetype [helper]PrimitiveVT.VT1B V_0)
      IL_0000:  ldarg.0
      IL_0001:  ldarg.1
      IL_0002:  ldarg.2
      IL_0003:  tail. call       instance valuetype [helper]PrimitiveVT.VT1B PrimitiveVT.CallConv3::f2b(valuetype [helper]PrimitiveVT.VT1A,
                                                                                                  valuetype [helper]PrimitiveVT.VT1B)
      IL_000c:  ret
    } 

    .method private hidebysig static int32 
            f3(valuetype [helper]PrimitiveVT.VT1B& x,
               valuetype [helper]PrimitiveVT.VT1B y) cil managed
    {
      .maxstack  2
      .locals init (int32 V_0)
      IL_0000:  ldarg.0
      IL_0001:  ldfld      int32 [helper]PrimitiveVT.VT1B::m
      IL_0006:  ldarga.s   y
      IL_0008:  ldfld      int32 [helper]PrimitiveVT.VT1B::m
      IL_000d:  sub
      IL_000e:  stloc.0
      IL_000f:  br.s       IL_0011

      IL_0011:  ldloc.0
      IL_0012:  ret
    } 

    .method private hidebysig instance valuetype [helper]PrimitiveVT.VT1B 
            f4(valuetype [helper]PrimitiveVT.VT1A x,
               valuetype [helper]PrimitiveVT.VT1B y) cil managed
    {
      .maxstack  3
      .locals init (valuetype [helper]PrimitiveVT.VT1B V_0,
               valuetype [helper]PrimitiveVT.VT1B V_1)
      IL_0000:  ldloca.s   V_0
      IL_0002:  ldarga.s   x
      IL_0004:  ldfld      int32 [helper]PrimitiveVT.VT1A::m
      IL_0009:  ldarga.s   y
      IL_000b:  ldfld      int32 [helper]PrimitiveVT.VT1B::m
      IL_0010:  sub
      IL_0011:  stfld      int32 [helper]PrimitiveVT.VT1B::m
      IL_0016:  ldloc.0
      IL_0017:  stloc.1
      IL_0018:  br.s       IL_001a

      IL_001a:  ldloc.1
      IL_001b:  ret
    } 

    .method private hidebysig static int32 
            f5(valuetype [helper]PrimitiveVT.VT1B x,
               valuetype [helper]PrimitiveVT.VT1A y) cil managed
    {
      .maxstack  2
      .locals init (int32 V_0)
      IL_0000:  ldarga.s   x
      IL_0002:  ldfld      int32 [helper]PrimitiveVT.VT1B::m
      IL_0007:  ldarga.s   y
      IL_0009:  ldfld      int32 [helper]PrimitiveVT.VT1A::m
      IL_000e:  mul
      IL_000f:  stloc.0
      IL_0010:  br.s       IL_0012

      IL_0012:  ldloc.0
      IL_0013:  ret
    } 

    .method private hidebysig instance int32 
            f6(valuetype [helper]PrimitiveVT.VT1B[] x,
               valuetype [helper]PrimitiveVT.VT1B y) cil managed
    {
      .maxstack  2
      .locals init (int32 V_0)
      IL_0000:  ldarg.1
      IL_0001:  ldc.i4.0
      IL_0002:  ldelema    [helper]PrimitiveVT.VT1B
      IL_0007:  ldfld      int32 [helper]PrimitiveVT.VT1B::m
      IL_000c:  ldarga.s   y
      IL_000e:  ldfld      int32 [helper]PrimitiveVT.VT1B::m
      IL_0013:  mul
      IL_0014:  stloc.0
      IL_0015:  br.s       IL_0017

      IL_0017:  ldloc.0
      IL_0018:  ret
    } 

    .method private hidebysig instance valuetype [helper]PrimitiveVT.VT1B 
            f7(valuetype [helper]PrimitiveVT.VT1A x,
               valuetype [helper]PrimitiveVT.VT1B y) cil managed
    {
      .maxstack  3
      .locals init (valuetype [helper]PrimitiveVT.VT1B V_0)
      IL_0000:  ldarg.0
      IL_0001:  ldarg.1
      IL_0002:  ldarg.2
      IL_0003:  call       instance valuetype [helper]PrimitiveVT.VT1B PrimitiveVT.CallConv3::f4(valuetype [helper]PrimitiveVT.VT1A,
                                                                                                 valuetype [helper]PrimitiveVT.VT1B)
      IL_0008:  stloc.0
      IL_0009:  br.s       IL_000b

      IL_000b:  ldloc.0
      IL_000c:  ret
    } 

    .method private hidebysig instance float32 
            f8(valuetype [helper]PrimitiveVT.VT1A x,
               valuetype [helper]PrimitiveVT.VT1B y) cil managed
    {
      .maxstack  2
      .locals init (float32 V_0)
      IL_0000:  ldarga.s   x
      IL_0002:  ldfld      int32 [helper]PrimitiveVT.VT1A::m
      IL_0007:  ldarga.s   y
      IL_0009:  ldfld      int32 [helper]PrimitiveVT.VT1B::m
      IL_000e:  div
      IL_000f:  conv.r4
      IL_0010:  stloc.0
      IL_0011:  br.s       IL_0013

      IL_0013:  ldloc.0
      IL_0014:  ret
    } 

    .method private hidebysig static valuetype [helper]PrimitiveVT.VT1B[0...,0...] 
            f9a() cil managed
    {
      .maxstack  2
      .locals init (valuetype [helper]PrimitiveVT.VT1B[0...,0...] V_0)
      IL_0000:  ldc.i4.1
      IL_0001:  ldc.i4.2
      IL_0002:  newobj     instance void valuetype [helper]PrimitiveVT.VT1B[0...,0...]::.ctor(int32,
                                                                                              int32)
      IL_000b:  ret
    } 

    .method private hidebysig static valuetype [helper]PrimitiveVT.VT1B[0...,0...] 
            f9() cil managed
    {
      .maxstack  1
      .locals init (valuetype [helper]PrimitiveVT.VT1B[0...,0...] V_0)
      IL_0000:  jmp       valuetype [helper]PrimitiveVT.VT1B[0...,0...] PrimitiveVT.CallConv3::f9a()
    } 

    .method private hidebysig instance int32 
            f10(valuetype [helper]PrimitiveVT.VT1B[] args) cil managed
    {
      .param [1]
      .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
      .maxstack  3
      .locals init (int32 V_0,
               int32 V_1,
               int32 V_2)
      IL_0000:  ldc.i4.0
      IL_0001:  stloc.0
      IL_0002:  ldc.i4.0
      IL_0003:  stloc.1
      IL_0004:  br.s       IL_0022

      IL_0006:  ldloc.0
      IL_0007:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Implicit(int32)
      IL_000c:  ldarg.1
      IL_000d:  ldloc.1
      IL_000e:  ldelema    [helper]PrimitiveVT.VT1B
      IL_0013:  ldobj      [helper]PrimitiveVT.VT1B
      IL_0018:  call       int32 [helper]PrimitiveVT.VT1B::op_Addition(valuetype [helper]PrimitiveVT.VT1B,
                                                                       valuetype [helper]PrimitiveVT.VT1B)
      IL_001d:  stloc.0
      IL_001e:  ldloc.1
      IL_001f:  ldc.i4.1
      IL_0020:  add
      IL_0021:  stloc.1
      IL_0022:  ldloc.1
      IL_0023:  ldarg.1
      IL_0024:  ldlen
      IL_0025:  conv.i4
      IL_0026:  blt.s      IL_0006

      IL_0028:  ldloc.0
      IL_0029:  stloc.2
      IL_002a:  br.s       IL_002c

      IL_002c:  ldloc.2
      IL_002d:  ret
    } 

    .method public hidebysig static int32 Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  6
      .locals init (int32 V_0,
               class PrimitiveVT.CallConv3 V_1,
               valuetype [helper]PrimitiveVT.VT1B V_2,
               int32 V_3,
               int32 V_4,
               int32 V_5,
               int32 V_6,
               int32 V_7,
               valuetype [helper]PrimitiveVT.VT1B[] V_8,
               valuetype [helper]PrimitiveVT.VT1B V_9,
               valuetype [helper]PrimitiveVT.VT1B V_10,
               int32 V_11,
               valuetype [helper]PrimitiveVT.VT1B V_12,
               valuetype [helper]PrimitiveVT.VT1B* V_13,
               valuetype [helper]PrimitiveVT.VT1B[0...,0...][] V_14,
               int32 V_15,
               valuetype [helper]PrimitiveVT.VT1B[] V_16)
      IL_0000:  ldsfld     class [mscorlib]System.Random PrimitiveVT.CallConv3::rand
      IL_0005:  callvirt   instance int32 [mscorlib]System.Random::Next()
      IL_000a:  stloc.0
      IL_000b:  newobj     instance void PrimitiveVT.CallConv3::.ctor()
      IL_0010:  stloc.1
      IL_0011:  ldloc.1
      IL_0012:  ldflda     valuetype [helper]PrimitiveVT.VT1A PrimitiveVT.CallConv3::vt1a
      IL_0017:  ldloc.0
      IL_0018:  stfld      int32 [helper]PrimitiveVT.VT1A::m
      IL_001d:  ldloc.1
      IL_001e:  ldfld      valuetype [helper]PrimitiveVT.VT1A PrimitiveVT.CallConv3::vt1a
      IL_0023:  ldftn       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Explicit(valuetype [helper]PrimitiveVT.VT1A)
      			calli valuetype [helper]PrimitiveVT.VT1B (valuetype [helper]PrimitiveVT.VT1A)
      IL_0028:  stloc.2
      IL_0029:  ldloc.2
      IL_002a:  call       int32 [helper]PrimitiveVT.VT1B::op_Implicit(valuetype [helper]PrimitiveVT.VT1B)
      IL_002f:  stloc.3
      IL_0030:  ldloc.3
      IL_0031:  ldloc.0
      IL_0032:  beq.s      IL_0046

      IL_0034:  ldstr      "FAILED, b!=a"
      IL_0039:  call       void [System.Console]System.Console::WriteLine(string)
      IL_003e:  ldc.i4.1
      IL_003f:  stloc.s    V_15
      IL_0041:  br         IL_04a4

      IL_0046:  ldloc.1
      IL_0047:  ldfld      valuetype [helper]PrimitiveVT.VT1A PrimitiveVT.CallConv3::vt1a
      IL_004c:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Explicit(valuetype [helper]PrimitiveVT.VT1A)
      IL_0051:  ldftn       int32 [helper]PrimitiveVT.VT1B::op_Implicit(valuetype [helper]PrimitiveVT.VT1B)
      			calli        int32 (valuetype [helper]PrimitiveVT.VT1B)
      IL_0056:  ldftn       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Implicit(int32)
      			 calli      valuetype [helper]PrimitiveVT.VT1B (int32)
      IL_005b:  call       valuetype [helper]PrimitiveVT.VT1A [helper]PrimitiveVT.VT1B::op_Explicit(valuetype [helper]PrimitiveVT.VT1B)
      IL_0060:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Explicit(valuetype [helper]PrimitiveVT.VT1A)
      IL_0065:  call       int32 [helper]PrimitiveVT.VT1B::op_Implicit(valuetype [helper]PrimitiveVT.VT1B)
      IL_006a:  stloc.s    V_4
      IL_006c:  ldloc.s    V_4
      IL_006e:  ldloc.3
      IL_006f:  beq.s      IL_0083

      IL_0071:  ldstr      "FAILED, c!=b"
      IL_0076:  call       void [System.Console]System.Console::WriteLine(string)
      IL_007b:  ldc.i4.1
      IL_007c:  stloc.s    V_15
      IL_007e:  br         IL_04a4

      IL_0083:  ldsfld     class [mscorlib]System.Random PrimitiveVT.CallConv3::rand
      IL_0088:  callvirt   instance int32 [mscorlib]System.Random::Next()
      IL_008d:  stloc.s    V_5
      IL_008f:  ldc.i4     0x80000000
      IL_0094:  stloc.s    V_6
      IL_0096:  ldc.i4     0x3fffffff
      IL_009b:  stloc.s    V_7
      IL_009d:  ldsflda    valuetype [helper]PrimitiveVT.VT1A PrimitiveVT.CallConv3::x
      IL_00a2:  initobj    [helper]PrimitiveVT.VT1A
      IL_00a8:  ldc.i4.2
      IL_00a9:  newarr     [helper]PrimitiveVT.VT1B
      IL_00ae:  stloc.s    V_8
      IL_00b0:  ldloc.s    V_8
      IL_00b2:  ldc.i4.0
      IL_00b3:  ldelema    [helper]PrimitiveVT.VT1B
      IL_00b8:  ldloc.s    V_6
      IL_00ba:  call       instance void [helper]PrimitiveVT.VT1B::.ctor(int32)
      IL_00bf:  ldloc.s    V_8
      IL_00c1:  ldc.i4.0
      IL_00c2:  ldelema    [helper]PrimitiveVT.VT1B
      IL_00c7:  ldobj      [helper]PrimitiveVT.VT1B
      IL_00cc:  stloc.s    V_9
      IL_00ce:  ldsflda    valuetype [helper]PrimitiveVT.VT1A PrimitiveVT.CallConv3::x
      IL_00d3:  ldloc.s    V_5
      IL_00d5:  stfld      int32 [helper]PrimitiveVT.VT1A::m
      IL_00da:  ldsfld     valuetype [helper]PrimitiveVT.VT1A PrimitiveVT.CallConv3::x
      IL_00df:  ldloc.s    V_9
      IL_00e1:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Multiply(valuetype [helper]PrimitiveVT.VT1A,
                                                                                                    valuetype [helper]PrimitiveVT.VT1B)
      IL_00e6:  ldloc.s    V_7
      IL_00e8:  newobj     instance void [helper]PrimitiveVT.VT1B::.ctor(int32)
      IL_00ed:  call       int32 [helper]PrimitiveVT.VT1B::op_Subtraction(valuetype [helper]PrimitiveVT.VT1B,
                                                                          valuetype [helper]PrimitiveVT.VT1B)
      IL_00f2:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Implicit(int32)
      IL_00f7:  ldloc.s    V_8
      IL_00f9:  ldc.i4.0
      IL_00fa:  ldelema    [helper]PrimitiveVT.VT1B
      IL_00ff:  ldobj      [helper]PrimitiveVT.VT1B
      IL_0104:  call       int32 [helper]PrimitiveVT.VT1B::op_Addition(valuetype [helper]PrimitiveVT.VT1B,
                                                                       valuetype [helper]PrimitiveVT.VT1B)
      IL_0109:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Implicit(int32)
      IL_010e:  ldsfld     valuetype [helper]PrimitiveVT.VT1A PrimitiveVT.CallConv3::x
      IL_0113:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Explicit(valuetype [helper]PrimitiveVT.VT1A)
      IL_0118:  call       int32 [helper]PrimitiveVT.VT1B::op_Addition(valuetype [helper]PrimitiveVT.VT1B,
                                                                       valuetype [helper]PrimitiveVT.VT1B)
      IL_011d:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Implicit(int32)
      IL_0122:  ldloc.s    V_7
      IL_0124:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Implicit(int32)
      IL_0129:  call       int32 [helper]PrimitiveVT.VT1B::op_Addition(valuetype [helper]PrimitiveVT.VT1B,
                                                                       valuetype [helper]PrimitiveVT.VT1B)
      IL_012e:  ldloc.s    V_9
      IL_0130:  ldsfld     valuetype [helper]PrimitiveVT.VT1A PrimitiveVT.CallConv3::x
      IL_0135:  call       int32 [helper]PrimitiveVT.VT1B::op_Multiply(valuetype [helper]PrimitiveVT.VT1B,
                                                                       valuetype [helper]PrimitiveVT.VT1A)
      IL_013a:  add
      IL_013b:  ldsfld     valuetype [helper]PrimitiveVT.VT1A PrimitiveVT.CallConv3::x
      IL_0140:  ldloc.s    V_5
      IL_0142:  ldc.i4.2
      IL_0143:  rem
      IL_0144:  brfalse.s  IL_014f

      IL_0146:  ldloc.s    V_5
      IL_0148:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Implicit(int32)
      IL_014d:  br.s       IL_0158

      IL_014f:  ldloc.s    V_5
      IL_0151:  ldc.i4.2
      IL_0152:  div
      IL_0153:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Implicit(int32)
      IL_0158:  call       float32 [helper]PrimitiveVT.VT1B::op_Division(valuetype [helper]PrimitiveVT.VT1A,
                                                                         valuetype [helper]PrimitiveVT.VT1B)
      IL_015d:  conv.i4
      IL_015e:  add
      IL_015f:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Implicit(int32)
      IL_0164:  stloc.s    V_10
      IL_0166:  ldsfld     valuetype [helper]PrimitiveVT.VT1A PrimitiveVT.CallConv3::x
      IL_016b:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Explicit(valuetype [helper]PrimitiveVT.VT1A)
      IL_0170:  ldloc.s    V_9
      IL_0172:  call       valuetype [helper]PrimitiveVT.VT1A [helper]PrimitiveVT.VT1B::op_Explicit(valuetype [helper]PrimitiveVT.VT1B)
      IL_0177:  call       int32 PrimitiveVT.CallConv3::f5(valuetype [helper]PrimitiveVT.VT1B,
                                                           valuetype [helper]PrimitiveVT.VT1A)
      IL_017c:  ldloc.1
      IL_017d:  ldloc.s    V_8
      IL_017f:  ldsfld     valuetype [helper]PrimitiveVT.VT1A PrimitiveVT.CallConv3::x
      IL_0184:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Explicit(valuetype [helper]PrimitiveVT.VT1A)
      IL_0189:  callvirt   instance int32 PrimitiveVT.CallConv3::f6(valuetype [helper]PrimitiveVT.VT1B[],
                                                                    valuetype [helper]PrimitiveVT.VT1B)
      IL_018e:  add
      IL_018f:  ldloc.s    V_9
      IL_0191:  ldloc.s    V_5
      IL_0193:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Implicit(int32)
      IL_0198:  call       int32 PrimitiveVT.CallConv3::f1(valuetype [helper]PrimitiveVT.VT1B,
                                                           valuetype [helper]PrimitiveVT.VT1B)
      IL_019d:  add
      IL_019e:  ldloc.1
      IL_019f:  ldloc.s    V_5
      IL_01a1:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Implicit(int32)
      IL_01a6:  call       valuetype [helper]PrimitiveVT.VT1A [helper]PrimitiveVT.VT1B::op_Explicit(valuetype [helper]PrimitiveVT.VT1B)
      IL_01ab:  ldloc.s    V_5
      IL_01ad:  ldc.i4.2
      IL_01ae:  rem
      IL_01af:  brfalse.s  IL_01ba

      IL_01b1:  ldloc.s    V_5
      IL_01b3:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Implicit(int32)
      IL_01b8:  br.s       IL_01c3

      IL_01ba:  ldloc.s    V_5
      IL_01bc:  ldc.i4.2
      IL_01bd:  div
      IL_01be:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Implicit(int32)
      IL_01c3:  callvirt   instance float32 PrimitiveVT.CallConv3::f8(valuetype [helper]PrimitiveVT.VT1A,
                                                                      valuetype [helper]PrimitiveVT.VT1B)
      IL_01c8:  conv.i4
      IL_01c9:  add
      IL_01ca:  stloc.s    V_11
      IL_01cc:  ldloc.s    V_10
      IL_01ce:  call       int32 [helper]PrimitiveVT.VT1B::op_Implicit(valuetype [helper]PrimitiveVT.VT1B)
      IL_01d3:  ldloc.s    V_11
      IL_01d5:  beq.s      IL_01fc

      IL_01d7:  ldstr      "FAILED, u!=w"
      IL_01dc:  call       void [System.Console]System.Console::WriteLine(string)
      IL_01e1:  ldloc.s    V_10
      IL_01e3:  call       int32 [helper]PrimitiveVT.VT1B::op_Implicit(valuetype [helper]PrimitiveVT.VT1B)
      IL_01e8:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_01ed:  ldloc.s    V_11
      IL_01ef:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_01f4:  ldc.i4.1
      IL_01f5:  stloc.s    V_15
      IL_01f7:  br         IL_04a4

      IL_01fc:  ldc.i4.3
      IL_01fd:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Implicit(int32)
      IL_0202:  stloc.s    V_12
      IL_0204:  br.s       IL_0223

      IL_0206:  ldloc.s    V_12
      IL_0208:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Increment(valuetype [helper]PrimitiveVT.VT1B)
      IL_020d:  stloc.s    V_12
      IL_020f:  ldloc.1
      IL_0210:  ldloc.s    V_9
      IL_0212:  call       valuetype [helper]PrimitiveVT.VT1A [helper]PrimitiveVT.VT1B::op_Explicit(valuetype [helper]PrimitiveVT.VT1B)
      IL_0217:  ldc.i4.1
      IL_0218:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Implicit(int32)
      IL_021d:  callvirt   instance valuetype [helper]PrimitiveVT.VT1B PrimitiveVT.CallConv3::f2(valuetype [helper]PrimitiveVT.VT1A,
                                                                                                 valuetype [helper]PrimitiveVT.VT1B)
      IL_0222:  pop
      IL_0223:  ldloc.s    V_12
      IL_0225:  call       int32 [helper]PrimitiveVT.VT1B::op_Implicit(valuetype [helper]PrimitiveVT.VT1B)
      IL_022a:  ldc.i4.s   10
      IL_022c:  ble.s      IL_0206

      IL_022e:  ldloca.s   V_9
      IL_0230:  ldc.i4     0x80000000
      IL_0235:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Implicit(int32)
      IL_023a:  call       int32 PrimitiveVT.CallConv3::f3(valuetype [helper]PrimitiveVT.VT1B&,
                                                           valuetype [helper]PrimitiveVT.VT1B)
      IL_023f:  brfalse.s  IL_0269

      IL_0241:  ldstr      "FAILED, f3(y,Int32.MinValue)!=0"
      IL_0246:  call       void [System.Console]System.Console::WriteLine(string)
      IL_024b:  ldloca.s   V_9
      IL_024d:  ldc.i4     0x80000000
      IL_0252:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Implicit(int32)
      IL_0257:  call       int32 PrimitiveVT.CallConv3::f3(valuetype [helper]PrimitiveVT.VT1B&,
                                                           valuetype [helper]PrimitiveVT.VT1B)
      IL_025c:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_0261:  ldc.i4.1
      IL_0262:  stloc.s    V_15
      IL_0264:  br         IL_04a4

      IL_0269:  sizeof     [helper]PrimitiveVT.VT1B
      IL_026f:  ldc.i4.3
      IL_0270:  mul
      IL_0271:  localloc
      IL_0273:  stloc.s    V_13
      IL_0275:  ldloc.s    V_13
      IL_0277:  ldc.i4.1
      IL_0278:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Implicit(int32)
      IL_027d:  stobj      [helper]PrimitiveVT.VT1B
      IL_0282:  ldloc.s    V_13
      IL_0284:  sizeof     [helper]PrimitiveVT.VT1B
      IL_028a:  add
      IL_028b:  ldc.i4.2
      IL_028c:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Implicit(int32)
      IL_0291:  stobj      [helper]PrimitiveVT.VT1B
      IL_0296:  ldloc.s    V_13
      IL_0298:  ldc.i4.2
      IL_0299:  conv.i
      IL_029a:  sizeof     [helper]PrimitiveVT.VT1B
      IL_02a0:  mul
      IL_02a1:  add
      IL_02a2:  ldc.i4.3
      IL_02a3:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Implicit(int32)
      IL_02a8:  stobj      [helper]PrimitiveVT.VT1B
      IL_02ad:  ldloc.1
      IL_02ae:  ldloc.s    V_13
      IL_02b0:  ldc.i4.2
      IL_02b1:  conv.i
      IL_02b2:  sizeof     [helper]PrimitiveVT.VT1B
      IL_02b8:  mul
      IL_02b9:  add
      IL_02ba:  ldobj      [helper]PrimitiveVT.VT1B
      IL_02bf:  call       valuetype [helper]PrimitiveVT.VT1A [helper]PrimitiveVT.VT1B::op_Explicit(valuetype [helper]PrimitiveVT.VT1B)
      IL_02c4:  ldloc.s    V_13
      IL_02c6:  ldobj      [helper]PrimitiveVT.VT1B
      IL_02cb:  callvirt   instance valuetype [helper]PrimitiveVT.VT1B PrimitiveVT.CallConv3::f7(valuetype [helper]PrimitiveVT.VT1A,
                                                                                                 valuetype [helper]PrimitiveVT.VT1B)
      IL_02d0:  call       int32 [helper]PrimitiveVT.VT1B::op_Implicit(valuetype [helper]PrimitiveVT.VT1B)
      IL_02d5:  ldc.i4.2
      IL_02d6:  beq.s      IL_0317

      IL_02d8:  ldstr      "FAILED (t.f7((VT1A)o[2], o[0]))!=2"
      IL_02dd:  call       void [System.Console]System.Console::WriteLine(string)
      IL_02e2:  ldloc.1
      IL_02e3:  ldloc.s    V_13
      IL_02e5:  ldc.i4.2
      IL_02e6:  conv.i
      IL_02e7:  sizeof     [helper]PrimitiveVT.VT1B
      IL_02ed:  mul
      IL_02ee:  add
      IL_02ef:  ldobj      [helper]PrimitiveVT.VT1B
      IL_02f4:  call       valuetype [helper]PrimitiveVT.VT1A [helper]PrimitiveVT.VT1B::op_Explicit(valuetype [helper]PrimitiveVT.VT1B)
      IL_02f9:  ldloc.s    V_13
      IL_02fb:  ldobj      [helper]PrimitiveVT.VT1B
      IL_0300:  callvirt   instance valuetype [helper]PrimitiveVT.VT1B PrimitiveVT.CallConv3::f7(valuetype [helper]PrimitiveVT.VT1A,
                                                                                                 valuetype [helper]PrimitiveVT.VT1B)
      IL_0305:  call       int32 [helper]PrimitiveVT.VT1B::op_Implicit(valuetype [helper]PrimitiveVT.VT1B)
      IL_030a:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_030f:  ldc.i4.1
      IL_0310:  stloc.s    V_15
      IL_0312:  br         IL_04a4

      IL_0317:  ldc.i4.2
      IL_0318:  newarr     valuetype [helper]PrimitiveVT.VT1B[0...,0...]
      IL_031d:  stloc.s    V_14
      IL_031f:  ldloc.s    V_14
      IL_0321:  ldc.i4.1
      IL_0322:  call       valuetype [helper]PrimitiveVT.VT1B[0...,0...] PrimitiveVT.CallConv3::f9()
      IL_0327:  stelem.ref
      IL_0328:  ldloc.s    V_14
      IL_032a:  ldc.i4.1
      IL_032b:  ldelem.ref
      IL_032c:  ldc.i4.0
      IL_032d:  ldc.i4.0
      IL_032e:  call       instance valuetype [helper]PrimitiveVT.VT1B& valuetype [helper]PrimitiveVT.VT1B[0...,0...]::Address(int32,
                                                                                                                               int32)
      IL_0333:  ldloc.s    V_13
      IL_0335:  ldobj      [helper]PrimitiveVT.VT1B
      IL_033a:  stobj      [helper]PrimitiveVT.VT1B
      IL_033f:  ldloc.1
      IL_0340:  ldc.i4.1
      IL_0341:  newarr     [helper]PrimitiveVT.VT1B
      IL_0346:  stloc.s    V_16
      IL_0348:  ldloc.s    V_16
      IL_034a:  ldc.i4.0
      IL_034b:  ldelema    [helper]PrimitiveVT.VT1B
      IL_0350:  ldloc.s    V_14
      IL_0352:  ldc.i4.1
      IL_0353:  ldelem.ref
      IL_0354:  ldc.i4.0
      IL_0355:  ldc.i4.0
      IL_0356:  call       instance valuetype [helper]PrimitiveVT.VT1B& valuetype [helper]PrimitiveVT.VT1B[0...,0...]::Address(int32,
                                                                                                                               int32)
      IL_035b:  ldobj      [helper]PrimitiveVT.VT1B
      IL_0360:  stobj      [helper]PrimitiveVT.VT1B
      IL_0365:  ldloc.s    V_16
      IL_0367:  callvirt   instance int32 PrimitiveVT.CallConv3::f10(valuetype [helper]PrimitiveVT.VT1B[])
      IL_036c:  ldc.i4.1
      IL_036d:  beq.s      IL_03b3

      IL_036f:  ldstr      "FAILED (t.f10(arr[1][0,0]))!=1"
      IL_0374:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0379:  ldloc.1
      IL_037a:  ldc.i4.1
      IL_037b:  newarr     [helper]PrimitiveVT.VT1B
      IL_0380:  stloc.s    V_16
      IL_0382:  ldloc.s    V_16
      IL_0384:  ldc.i4.0
      IL_0385:  ldelema    [helper]PrimitiveVT.VT1B
      IL_038a:  ldloc.s    V_14
      IL_038c:  ldc.i4.1
      IL_038d:  ldelem.ref
      IL_038e:  ldc.i4.0
      IL_038f:  ldc.i4.0
      IL_0390:  callvirt       instance valuetype [helper]PrimitiveVT.VT1B& valuetype [helper]PrimitiveVT.VT1B[0...,0...]::Address(int32,
                                                                                                                               int32)
      IL_0395:  ldobj      [helper]PrimitiveVT.VT1B
      IL_039a:  stobj      [helper]PrimitiveVT.VT1B
      IL_039f:  ldloc.s    V_16
      IL_03a1:  callvirt   instance int32 PrimitiveVT.CallConv3::f10(valuetype [helper]PrimitiveVT.VT1B[])
      IL_03a6:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_03ab:  ldc.i4.1
      IL_03ac:  stloc.s    V_15
      IL_03ae:  br         IL_04a4

      IL_03b3:  ldloc.1
      IL_03b4:  ldc.i4.3
      IL_03b5:  newarr     [helper]PrimitiveVT.VT1B
      IL_03ba:  stloc.s    V_16
      IL_03bc:  ldloc.s    V_16
      IL_03be:  ldc.i4.0
      IL_03bf:  ldelema    [helper]PrimitiveVT.VT1B
      IL_03c4:  ldloc.s    V_14
      IL_03c6:  ldc.i4.1
      IL_03c7:  ldelem.ref
      IL_03c8:  ldc.i4.0
      IL_03c9:  ldc.i4.0
      IL_03ca:  call       instance valuetype [helper]PrimitiveVT.VT1B& valuetype [helper]PrimitiveVT.VT1B[0...,0...]::Address(int32,
                                                                                                                               int32)
      IL_03cf:  ldobj      [helper]PrimitiveVT.VT1B
      IL_03d4:  stobj      [helper]PrimitiveVT.VT1B
      IL_03d9:  ldloc.s    V_16
      IL_03db:  ldc.i4.1
      IL_03dc:  ldelema    [helper]PrimitiveVT.VT1B
      IL_03e1:  ldloc.1
      IL_03e2:  ldc.i4.2
      IL_03e3:  newobj     instance void [helper]PrimitiveVT.VT1B::.ctor(int32)
      IL_03e8:  call       valuetype [helper]PrimitiveVT.VT1A [helper]PrimitiveVT.VT1B::op_Explicit(valuetype [helper]PrimitiveVT.VT1B)
      IL_03ed:  ldloc.s    V_13
      IL_03ef:  ldobj      [helper]PrimitiveVT.VT1B
      IL_03f4:  callvirt   instance valuetype [helper]PrimitiveVT.VT1B PrimitiveVT.CallConv3::f7(valuetype [helper]PrimitiveVT.VT1A,
                                                                                                 valuetype [helper]PrimitiveVT.VT1B)
      IL_03f9:  stobj      [helper]PrimitiveVT.VT1B
      IL_03fe:  ldloc.s    V_16
      IL_0400:  ldc.i4.2
      IL_0401:  ldelema    [helper]PrimitiveVT.VT1B
      IL_0406:  ldc.i4.4
      IL_0407:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Implicit(int32)
      IL_040c:  stobj      [helper]PrimitiveVT.VT1B
      IL_0411:  ldloc.s    V_16
      IL_0413:  callvirt   instance int32 PrimitiveVT.CallConv3::f10(valuetype [helper]PrimitiveVT.VT1B[])
      IL_0418:  ldc.i4.6
      IL_0419:  beq      IL_0494

      IL_041b:  ldstr      "FAILED (t.f10(arr[1][0,0], t.f7((VT1A)(new VT1B(2)"
      + "), (VT1B)o[0]), 4))!=6"
      IL_0420:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0425:  ldloc.1
      IL_0426:  ldc.i4.3
      IL_0427:  newarr     [helper]PrimitiveVT.VT1B
      IL_042c:  stloc.s    V_16
      IL_042e:  ldloc.s    V_16
      IL_0430:  ldc.i4.0
      IL_0431:  ldelema    [helper]PrimitiveVT.VT1B
      IL_0436:  ldloc.s    V_14
      IL_0438:  ldc.i4.1
      IL_0439:  ldelem.ref
      IL_043a:  ldc.i4.0
      IL_043b:  ldc.i4.0
      IL_043c:  call       instance valuetype [helper]PrimitiveVT.VT1B& valuetype [helper]PrimitiveVT.VT1B[0...,0...]::Address(int32,
                                                                                                                               int32)
      IL_0441:  ldobj      [helper]PrimitiveVT.VT1B
      IL_0446:  stobj      [helper]PrimitiveVT.VT1B
      IL_044b:  ldloc.s    V_16
      IL_044d:  ldc.i4.1
      IL_044e:  ldelema    [helper]PrimitiveVT.VT1B
      IL_0453:  ldloc.1
      IL_0454:  ldc.i4.2
      IL_0455:  newobj     instance void [helper]PrimitiveVT.VT1B::.ctor(int32)
      IL_045a:  call       valuetype [helper]PrimitiveVT.VT1A [helper]PrimitiveVT.VT1B::op_Explicit(valuetype [helper]PrimitiveVT.VT1B)
      IL_045f:  ldloc.s    V_13
      IL_0461:  ldobj      [helper]PrimitiveVT.VT1B
      IL_0466:  callvirt   instance valuetype [helper]PrimitiveVT.VT1B PrimitiveVT.CallConv3::f7(valuetype [helper]PrimitiveVT.VT1A,
                                                                                                 valuetype [helper]PrimitiveVT.VT1B)
      IL_046b:  stobj      [helper]PrimitiveVT.VT1B
      IL_0470:  ldloc.s    V_16
      IL_0472:  ldc.i4.2
      IL_0473:  ldelema    [helper]PrimitiveVT.VT1B
      IL_0478:  ldc.i4.4
      IL_0479:  call       valuetype [helper]PrimitiveVT.VT1B [helper]PrimitiveVT.VT1B::op_Implicit(int32)
      IL_047e:  stobj      [helper]PrimitiveVT.VT1B
      IL_0483:  ldloc.s    V_16
      			dup
      IL_0485:  ldvirtftn   instance int32 PrimitiveVT.CallConv3::f10(valuetype [helper]PrimitiveVT.VT1B[])
      			calli	   instance int32 (valuetype [helper]PrimitiveVT.VT1B[])
      IL_048a:  call       void [System.Console]System.Console::WriteLine(int32)
      IL_048f:  ldc.i4.1
      IL_0490:  stloc.s    V_15
      IL_0492:  br.s       IL_04a4

      IL_0494:  ldstr      "PASSED"
      IL_0499:  call       void [System.Console]System.Console::WriteLine(string)
      IL_049e:  ldc.i4.s   100
      IL_04a0:  stloc.s    V_15
      IL_04a2:  br.s       IL_04a4

      IL_04a4:  ldloc.s    V_15
      IL_04a6:  ret
    } 

    .method private hidebysig specialname rtspecialname static 
            void  .cctor() cil managed
    {
      .maxstack  8
      IL_0000:  newobj     instance void [mscorlib]System.Random::.ctor()
      IL_0005:  stsfld     class [mscorlib]System.Random PrimitiveVT.CallConv3::rand
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
