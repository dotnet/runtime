// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib {}
.assembly _simpleoddpower {}
.assembly extern xunit.core {}
.class interface private abstract auto ansi Power
{
  .method public hidebysig newslot abstract virtual 
          instance int32  Power4(int32 a,
                                 int32 b) cil managed
  {
  } 

} 

.class private auto ansi beforefieldinit FakePower
       extends [mscorlib]System.Object
       implements Power
{
  .method public hidebysig newslot virtual 
          instance int32  Power4(int32 a,
                                 int32 b) cil managed
  {
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  br.s       IL_0004

    IL_0004:  ldloc.0
    IL_0005:  ret
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

.class public auto ansi OddPower
       extends [mscorlib]System.Object
       implements Power
{
  .class auto ansi sealed nested private MyDeleg1a
         extends [mscorlib]System.MulticastDelegate
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(object 'object',
                                 native int 'method') runtime managed
    {
    } 

    .method public hidebysig newslot virtual 
            instance void  Invoke() runtime managed
    {
    } 

    .method public hidebysig newslot virtual 
            instance class [mscorlib]System.IAsyncResult 
            BeginInvoke(class [mscorlib]System.AsyncCallback callback,
                        object 'object') runtime managed
    {
    } 

    .method public hidebysig newslot virtual 
            instance void  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
    {
    } 

  } 

  .class auto ansi sealed nested private MyDeleg1b
         extends [mscorlib]System.MulticastDelegate
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(object 'object',
                                 native int 'method') runtime managed
    {
    } 

    .method public hidebysig newslot virtual 
            instance void  Invoke(int32 a) runtime managed
    {
    } 

    .method public hidebysig newslot virtual 
            instance class [mscorlib]System.IAsyncResult 
            BeginInvoke(int32 a,
                        class [mscorlib]System.AsyncCallback callback,
                        object 'object') runtime managed
    {
    } 

    .method public hidebysig newslot virtual 
            instance void  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
    {
    } 

  } 

  .class auto ansi sealed nested private MyDeleg2
         extends [mscorlib]System.MulticastDelegate
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(object 'object',
                                 native int 'method') runtime managed
    {
    } 

    .method public hidebysig newslot virtual 
            instance int32  Invoke(int32 a,
                                   int32 b) runtime managed
    {
    } 

    .method public hidebysig newslot virtual 
            instance class [mscorlib]System.IAsyncResult 
            BeginInvoke(int32 a,
                        int32 b,
                        class [mscorlib]System.AsyncCallback callback,
                        object 'object') runtime managed
    {
    } 

    .method public hidebysig newslot virtual 
            instance int32  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
    {
    } 

  } 

  .class auto ansi sealed nested private MyDeleg3
         extends [mscorlib]System.MulticastDelegate
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor(object 'object',
                                 native int 'method') runtime managed
    {
    } 

    .method public hidebysig newslot virtual 
            instance int32  Invoke(int32[] a) runtime managed
    {
      .param [1]
      .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
    } 

    .method public hidebysig newslot virtual 
            instance class [mscorlib]System.IAsyncResult 
            BeginInvoke(int32[] a,
                        class [mscorlib]System.AsyncCallback callback,
                        object 'object') runtime managed
    {
    } 

    .method public hidebysig newslot virtual 
            instance int32  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
    {
    } 

  } 

  .field private static int32 x
  .field public int32 y
  .field private static class OddPower/MyDeleg1a d1a
  .field private class OddPower/MyDeleg1b d1b
  .field private static class OddPower/MyDeleg2 d2a
  .field private class OddPower/MyDeleg2 d2b
  .field private static class OddPower/MyDeleg3 d3
  .field private static class OddPower/MyDeleg2 d4
  .field private static class OddPower/MyDeleg2 d5
  .field private static class OddPower/MyDeleg2 d6
  .field private static class OddPower/MyDeleg3 d7
  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldnull
    IL_0001:  
    		ldftn      void OddPower::.cctor()
    IL_0007:  newobj     instance void OddPower/MyDeleg1a::.ctor(object,
                                                                 native int)
    IL_000c:  stsfld     class OddPower/MyDeleg1a OddPower::d1a
    IL_0011:  ldnull
    IL_0012:  ldftn      int32 OddPower::Power1(int32,
                                                int32)
    IL_0018:  newobj     instance void OddPower/MyDeleg2::.ctor(object,
                                                                native int)
    IL_001d:  stsfld     class OddPower/MyDeleg2 OddPower::d2a
    IL_0022:  ldnull
    IL_0023:  ldftn      int32 OddPower::Power3(int32[])
    IL_0029:  newobj     instance void OddPower/MyDeleg3::.ctor(object,
                                                                native int)
    IL_002e:  stsfld     class OddPower/MyDeleg3 OddPower::d3
    IL_0033:  ldc.i4.2
    IL_0034:  stsfld     int32 OddPower::x
    IL_0039:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(int32 b) cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      int32 OddPower::y
    IL_000d:  ret
  } 

  .method public hidebysig static void  f1() cil managed
  {
    .maxstack  8
    IL_0000:  ldc.i4.2
    IL_0001:  stsfld     int32 OddPower::x
    IL_0006:  ret
  } 

  .method public hidebysig instance void 
          f2(int32 b) cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      int32 OddPower::y
    IL_0007:  ret
  } 

  .method public hidebysig static int32  Power1(int32 a,
                                                int32 b) cil managed
  {
    .maxstack  5
    .locals init (int32 V_0)
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_0007

    IL_0003:  ldc.i4.1
    IL_0004:  stloc.0
    IL_0005:  br.s       IL_001a

    IL_0007:  ldarg.0
    IL_0008:  ldsfld     class OddPower/MyDeleg2 OddPower::d2a
    IL_000d:  ldarg.0
    IL_000e:  ldarg.1
    IL_000f:  ldc.i4.1
    IL_0010:  sub
    IL_0011:  callvirt   instance int32 OddPower/MyDeleg2::Invoke(int32,
                                                                  int32)
    IL_0016:  mul
    IL_0017:  stloc.0
    IL_0018:  br.s       IL_001a

    IL_001a:  ldloc.0
    IL_001b:  ret
  } 

  .method public hidebysig instance int32 
          Power2(int32 a,
                 int32 b) cil managed
  {
    .maxstack  5
    .locals init (int32 V_0)
    IL_0000:  ldarg.2
    IL_0001:  brtrue.s   IL_0007

    IL_0003:  ldc.i4.1
    IL_0004:  stloc.0
    IL_0005:  br.s       IL_001b

    IL_0007:  ldarg.1
    IL_0008:  ldarg.0
    IL_0009:  ldfld      class OddPower/MyDeleg2 OddPower::d2b
    IL_000e:  ldarg.1
    IL_000f:  ldarg.2
    IL_0010:  ldc.i4.1
    IL_0011:  sub
    IL_0012:  callvirt   instance int32 OddPower/MyDeleg2::Invoke(int32,
                                                                  int32)
    IL_0017:  mul
    IL_0018:  stloc.0
    IL_0019:  br.s       IL_001b

    IL_001b:  ldloc.0
    IL_001c:  ret
  } 

  .method public hidebysig static int32  Power3(int32[] p) cil managed
  {
    .param [1]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
    .maxstack  6
    .locals init (int32 V_0,
             int32[] V_1)
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.1
    IL_0002:  ldelem.i4
    IL_0003:  brtrue.s   IL_0009

    IL_0005:  ldc.i4.1
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0030

    IL_0009:  ldarg.0
    IL_000a:  ldc.i4.0
    IL_000b:  ldelem.i4
    IL_000c:  ldsfld     class OddPower/MyDeleg3 OddPower::d3
    IL_0011:  ldc.i4.2
    IL_0012:  newarr     [mscorlib]System.Int32
    IL_0017:  stloc.1
    IL_0018:  ldloc.1
    IL_0019:  ldc.i4.0
    IL_001a:  ldarg.0
    IL_001b:  ldc.i4.0
    IL_001c:  ldelem.i4
    IL_001d:  stelem.i4
    IL_001e:  ldloc.1
    IL_001f:  ldc.i4.1
    IL_0020:  ldarg.0
    IL_0021:  ldc.i4.1
    IL_0022:  ldelem.i4
    IL_0023:  ldc.i4.1
    IL_0024:  sub
    IL_0025:  stelem.i4
    IL_0026:  ldloc.1
    IL_0027:  callvirt   instance int32 OddPower/MyDeleg3::Invoke(int32[])
    IL_002c:  mul
    IL_002d:  stloc.0
    IL_002e:  br.s       IL_0030

    IL_0030:  ldloc.0
    IL_0031:  ret
  } 

  .method public hidebysig newslot virtual 
          instance int32  Power4(int32 a,
                                 int32 b) cil managed
  {
    .maxstack  5
    .locals init (int32 V_0)
    IL_0000:  ldarg.2
    IL_0001:  brtrue.s   IL_0007

    IL_0003:  ldc.i4.1
    IL_0004:  stloc.0
    IL_0005:  br.s       IL_0016

    IL_0007:  ldarg.1
    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  ldarg.2
    IL_000b:  ldc.i4.1
    IL_000c:  sub
    IL_000d:  callvirt   instance int32 OddPower::Power4(int32,
                                                         int32)
    IL_0012:  mul
    IL_0013:  stloc.0
    IL_0014:  br.s       IL_0016

    IL_0016:  ldloc.0
    IL_0017:  ret
  } 

  .method public instance method int32 * () GetMthdPtr1()
  {
     ldarg.0
     ldvirtftn  instance int32 OddPower::Power4(int32,int32)
     ret
  }

  .method public static method int32 * () GetMthdPtr2()
  {
     ldftn      int32 OddPower::Power3(int32[])
     ret
  }

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  4
    .locals init (class OddPower V_0,
             int32 V_1,
             int32 V_2,
             int32 V_3,
             class FakePower V_4,
             int32 V_5,
             int32 V_6,
             int32 V_7,
             int32 V_8,
             int32 V_9,
             int32[] V_10)
    IL_0000:  ldc.i4.s   31
    IL_0002:  newobj     instance void OddPower::.ctor(int32)
    IL_0007:  stloc.0
    IL_0008:  ldloc.0
    IL_0009:  ldloc.0
    IL_000a:  
    		ldftn      instance void OddPower::.ctor(int32)
    IL_0010:  newobj     instance void OddPower/MyDeleg1b::.ctor(object,
                                                                 native int)
    IL_0015:  stfld      class OddPower/MyDeleg1b OddPower::d1b
    IL_001a:  ldsfld     class OddPower/MyDeleg1a OddPower::d1a
    IL_001f:  callvirt   instance void OddPower/MyDeleg1a::Invoke()
    IL_0024:  ldloc.0
    IL_0025:  ldfld      class OddPower/MyDeleg1b OddPower::d1b
    IL_002a:  ldc.i4.s   31
    IL_002c:  callvirt   instance void OddPower/MyDeleg1b::Invoke(int32)
    IL_0031:  ldsfld     class OddPower/MyDeleg2 OddPower::d2a
    IL_0036:  ldc.i4.2
    IL_0037:  ldc.i4.s   31
    IL_0039:  callvirt   instance int32 OddPower/MyDeleg2::Invoke(int32,
                                                                  int32)
    IL_003e:  stloc.1
    IL_003f:  ldloc.1
    IL_0040:  ldc.i4     0x80000000
    IL_0045:  beq.s      IL_0073

    IL_0047:  ldstr      "FAILED"
    IL_004c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0051:  ldstr      "d2a(2, 31)!=-2147483648"
    IL_0056:  call       void [System.Console]System.Console::WriteLine(string)
    IL_005b:  ldstr      "Expected: {0}"
    IL_0060:  ldloc.1
    IL_0061:  box        [mscorlib]System.Int32
    IL_0066:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_006b:  ldc.i4.1
    IL_006c:  stloc.s    V_9
    IL_006e:  br         IL_0299

    IL_0073:  ldloc.0
    IL_0074:  ldloc.0
    IL_0075:  ldftn      instance int32 OddPower::Power2(int32,
                                                         int32)
    IL_007b:  newobj     instance void OddPower/MyDeleg2::.ctor(object,
                                                                native int)
    IL_0080:  stfld      class OddPower/MyDeleg2 OddPower::d2b
    IL_0085:  ldloc.0
    IL_0086:  ldfld      class OddPower/MyDeleg2 OddPower::d2b
    IL_008b:  ldsfld     int32 OddPower::x
    IL_0090:  ldloc.0
    IL_0091:  ldfld      int32 OddPower::y
    IL_0096:  callvirt   instance int32 OddPower/MyDeleg2::Invoke(int32,
                                                                  int32)
    IL_009b:  stloc.2
    IL_009c:  ldloc.2
    IL_009d:  ldc.i4     0x80000000
    IL_00a2:  beq.s      IL_00d0

    IL_00a4:  ldstr      "FAILED"
    IL_00a9:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00ae:  ldstr      "o1.d2b(2, 31)!=-2147483648"
    IL_00b3:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00b8:  ldstr      "Expected: {0}"
    IL_00bd:  ldloc.2
    IL_00be:  box        [mscorlib]System.Int32
    IL_00c3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_00c8:  ldc.i4.2
    IL_00c9:  stloc.s    V_9
    IL_00cb:  br         IL_0299

    IL_00d0:  ldsfld     class OddPower/MyDeleg3 OddPower::d3
    IL_00d5:  ldc.i4.2
    IL_00d6:  newarr     [mscorlib]System.Int32
    IL_00db:  stloc.s    V_10
    IL_00dd:  ldloc.s    V_10
    IL_00df:  ldc.i4.0
    IL_00e0:  ldc.i4.2
    IL_00e1:  stelem.i4
    IL_00e2:  ldloc.s    V_10
    IL_00e4:  ldc.i4.1
    IL_00e5:  ldc.i4.s   31
    IL_00e7:  stelem.i4
    IL_00e8:  ldloc.s    V_10
    IL_00ea:  callvirt   instance int32 OddPower/MyDeleg3::Invoke(int32[])
    IL_00ef:  stloc.3
    IL_00f0:  ldloc.3
    IL_00f1:  ldc.i4     0x80000000
    IL_00f6:  beq.s      IL_0124

    IL_00f8:  ldstr      "FAILED"
    IL_00fd:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0102:  ldstr      "d3(2, 31)!=-2147483648"
    IL_0107:  call       void [System.Console]System.Console::WriteLine(string)
    IL_010c:  ldstr      "Expected: {0}"
    IL_0111:  ldloc.3
    IL_0112:  box        [mscorlib]System.Int32
    IL_0117:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_011c:  ldc.i4.1
    IL_011d:  stloc.s    V_9
    IL_011f:  br         IL_0299

    IL_0124:  newobj     instance void FakePower::.ctor()
    IL_0129:  stloc.s    V_4
    IL_012b:  ldloc.0
    IL_012c:  dup
    IL_012d:  
    			ldvirtftn  instance int32 FakePower::Power4(int32,int32)
    IL_0133:  newobj     instance void OddPower/MyDeleg2::.ctor(object,
                                                                native int)
    IL_0138:  stsfld     class OddPower/MyDeleg2 OddPower::d4
    IL_013d:  ldsfld     class OddPower/MyDeleg2 OddPower::d4
    IL_0142:  ldc.i4.2
    IL_0143:  ldc.i4.3
    IL_0144:  callvirt   instance int32 OddPower/MyDeleg2::Invoke(int32,
                                                                  int32)
    IL_0149:  stloc.s    V_5
    IL_014b:  ldloc.s    V_5
    IL_014d:  ldc.i4.8
    IL_014e:  beq.s      IL_017d

    IL_0150:  ldstr      "FAILED"
    IL_0155:  call       void [System.Console]System.Console::WriteLine(string)
    IL_015a:  ldstr      "d4(2,3)!=8"
    IL_015f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0164:  ldstr      "Expected: {0}"
    IL_0169:  ldloc.s    V_5
    IL_016b:  box        [mscorlib]System.Int32
    IL_0170:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0175:  ldc.i4.1
    IL_0176:  stloc.s    V_9
    IL_0178:  br         IL_0299

    IL_017d:  ldloc.s    V_4
    IL_017f:  dup
    IL_0180:  
    			ldvirtftn  instance int32 OddPower::Power4(int32, int32)
    IL_0186:  newobj     instance void OddPower/MyDeleg2::.ctor(object,
                                                                native int)
    IL_018b:  stsfld     class OddPower/MyDeleg2 OddPower::d5
    IL_0190:  ldsfld     class OddPower/MyDeleg2 OddPower::d5
    IL_0195:  ldc.i4.2
    IL_0196:  ldc.i4.3
    IL_0197:  callvirt   instance int32 OddPower/MyDeleg2::Invoke(int32,
                                                                  int32)
    IL_019c:  stloc.s    V_6
    IL_019e:  ldloc.s    V_6
    IL_01a0:  ldc.i4.1
    IL_01a1:  beq.s      IL_01d0

    IL_01a3:  ldstr      "FAILED"
    IL_01a8:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01ad:  ldstr      "d5(2,3)!=1"
    IL_01b2:  call       void [System.Console]System.Console::WriteLine(string)
    IL_01b7:  ldstr      "Expected: {0}"
    IL_01bc:  ldloc.s    V_6
    IL_01be:  box        [mscorlib]System.Int32
    IL_01c3:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_01c8:  ldc.i4.1
    IL_01c9:  stloc.s    V_9
    IL_01cb:  br         IL_0299

    IL_01d0:  ldloc.0
    IL_01d1:  dup
    IL_01d2:  
    	call instance method int32 * () OddPower::GetMthdPtr1()
    IL_01d8:  newobj     instance void OddPower/MyDeleg2::.ctor(object,
                                                                native int)
    IL_01dd:  stsfld     class OddPower/MyDeleg2 OddPower::d6
    IL_01e2:  ldsfld     class OddPower/MyDeleg2 OddPower::d6
    IL_01e7:  ldc.i4.2
    IL_01e8:  ldc.i4.s   16
    IL_01ea:  callvirt   instance int32 OddPower/MyDeleg2::Invoke(int32,
                                                                  int32)
    IL_01ef:  stloc.s    V_7
    IL_01f1:  ldloc.s    V_7
    IL_01f3:  ldc.i4     0x10000
    IL_01f8:  beq.s      IL_0224

    IL_01fa:  ldstr      "FAILED"
    IL_01ff:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0204:  ldstr      "d6(2,16)!=65536"
    IL_0209:  call       void [System.Console]System.Console::WriteLine(string)
    IL_020e:  ldstr      "Expected: {0}"
    IL_0213:  ldloc.s    V_7
    IL_0215:  box        [mscorlib]System.Int32
    IL_021a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_021f:  ldc.i4.1
    IL_0220:  stloc.s    V_9
    IL_0222:  br.s       IL_0299

    IL_0224:  ldnull
    IL_0225:  
    		call method int32 * () OddPower::GetMthdPtr2()
    IL_022b:  newobj     instance void OddPower/MyDeleg3::.ctor(object,
                                                                native int)
    IL_0230:  stsfld     class OddPower/MyDeleg3 OddPower::d7
    IL_0235:  ldsfld     class OddPower/MyDeleg3 OddPower::d7
    IL_023a:  ldc.i4.2
    IL_023b:  newarr     [mscorlib]System.Int32
    IL_0240:  stloc.s    V_10
    IL_0242:  ldloc.s    V_10
    IL_0244:  ldc.i4.0
    IL_0245:  ldc.i4.2
    IL_0246:  stelem.i4
    IL_0247:  ldloc.s    V_10
    IL_0249:  ldc.i4.1
    IL_024a:  ldc.i4.s   16
    IL_024c:  stelem.i4
    IL_024d:  ldloc.s    V_10
    IL_024f:  callvirt   instance int32 OddPower/MyDeleg3::Invoke(int32[])
    IL_0254:  stloc.s    V_8
    IL_0256:  ldloc.s    V_8
    IL_0258:  ldc.i4     0x10000
    IL_025d:  beq.s      IL_0289

    IL_025f:  ldstr      "FAILED"
    IL_0264:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0269:  ldstr      "d7(2,16)!=65536"
    IL_026e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0273:  ldstr      "Expected: {0}"
    IL_0278:  ldloc.s    V_8
    IL_027a:  box        [mscorlib]System.Int32
    IL_027f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0284:  ldc.i4.1
    IL_0285:  stloc.s    V_9
    IL_0287:  br.s       IL_0299

    IL_0289:  ldstr      "PASSED"
    IL_028e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0293:  ldc.i4.s   100
    IL_0295:  stloc.s    V_9
    IL_0297:  br.s       IL_0299

    IL_0299:  ldloc.s    V_9
    IL_029b:  ret
  } 

} 
