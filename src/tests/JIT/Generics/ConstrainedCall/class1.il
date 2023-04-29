// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib {}
.assembly ASSEMBLY_NAME {}
.class interface private abstract auto ansi IncrDecr
{
  .method public hidebysig newslot abstract virtual 
          instance void  Incr(int32 a) cil managed
  {
  } 

  .method public hidebysig newslot abstract virtual 
          instance void  Decr(int32 a) cil managed
  {
  } 

  .method public hidebysig newslot abstract virtual 
          instance int32  Val() cil managed
  {
  } 

} 

.class private auto ansi beforefieldinit MyInt
       extends [mscorlib]System.Object
       implements IncrDecr
{
  .field private int32 x
  .method public hidebysig newslot virtual final 
          instance void  Incr(int32 a) cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  dup
    IL_0002:  ldfld      int32 MyInt::x
    IL_0007:  ldarg.1
    IL_0008:  add
    IL_0009:  stfld      int32 MyInt::x
    IL_000e:  ret
  } 

  .method public hidebysig newslot virtual final 
          instance void  Decr(int32 a) cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  dup
    IL_0002:  ldfld      int32 MyInt::x
    IL_0007:  ldarg.1
    IL_0008:  sub
    IL_0009:  stfld      int32 MyInt::x
    IL_000e:  ret
  } 

  .method public hidebysig newslot virtual final 
          instance int32  Val() cil managed
  {
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      int32 MyInt::x
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
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

.class private auto ansi beforefieldinit MyCounter<(IncrDecr) T>
       extends [mscorlib]System.Object
{
  .param type T 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.NewConstraintAttribute::.ctor() = ( 01 00 00 00 ) 
  .field private !T counter
  .field private !T[] counters
  .method public hidebysig instance void 
          Increment() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldflda      !0 class MyCounter<!T>::counter
    IL_0006:  
    IL_000b:  ldc.i4.s   100
    constrained. !T
    IL_000d:  callvirt   instance void IncrDecr::Incr(int32)
    IL_0012:  ret
  } 

  .method public hidebysig instance void 
          Decrement() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldflda      !0 class MyCounter<!T>::counter
    IL_0006:  
    IL_000b:  ldc.i4.s   100
    constrained. !T
    IL_000d:  callvirt   instance void IncrDecr::Decr(int32)
    IL_0012:  ret
  } 

  .method public hidebysig instance void 
          Increment(int32 index) cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      !0[] class MyCounter<!T>::counters
    IL_0006:  ldarg.1
    IL_0007:  ldtoken    !T
    IL_000c:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0011:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_0016:  unbox.any  !T
    IL_001b:  stelem     !T
    IL_0020:  ldarg.0
    IL_0021:  ldfld      !0[] class MyCounter<!T>::counters
    IL_0026:  ldarg.1
    IL_0027:  ldelema     !T
    IL_002c:  
    IL_0031:  ldc.i4.s   100
    constrained. !T
    IL_0033:  callvirt   instance void IncrDecr::Incr(int32)
    IL_0038:  ret
  } 

  .method public hidebysig instance void 
          Decrement(int32 index) cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      !0[] class MyCounter<!T>::counters
    IL_0006:  ldarg.1
    IL_0007:  ldelema     !T
    IL_000c:  
    IL_0011:  ldc.i4.s   100
    constrained. !T
    IL_0013:  callvirt   instance void IncrDecr::Decr(int32)
    IL_0018:  ret
  } 

  .method public hidebysig newslot virtual 
          instance void  Increment2(!T cnter) cil managed
  {
    .maxstack  8
    IL_0000:  ldarga 1
    IL_0001:  
    IL_0006:  ldc.i4.s   100
    constrained. !T
    IL_0008:  callvirt   instance void IncrDecr::Incr(int32)
    IL_000d:  ldarg.0
    IL_000e:  ldarg.1
    IL_000f:  stfld      !0 class MyCounter<!T>::counter
    IL_0014:  ret
  } 

  .method public hidebysig newslot virtual 
          instance void  Decrement2(!T cnter) cil managed
  {
    .maxstack  8
    IL_0000:  ldarga 1
    IL_0001:  
    IL_0006:  ldc.i4.s   100
    constrained. !T
    IL_0008:  callvirt   instance void IncrDecr::Decr(int32)
    IL_000d:  ldarg.0
    IL_000e:  ldarg.1
    IL_000f:  stfld      !0 class MyCounter<!T>::counter
    IL_0014:  ret
  } 

  .method public hidebysig instance int32 
          Val() cil managed
  {
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      !0 class MyCounter<!T>::counter
    IL_0006:  box        !T
    IL_000b:  callvirt   instance int32 IncrDecr::Val()
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret
  } 

  .method public hidebysig instance int32 
          Val(int32 index) cil managed
  {
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      !0[] class MyCounter<!T>::counters
    IL_0006:  ldarg.1
    IL_0007:  ldelem     !T
    IL_000c:  box        !T
    IL_0011:  callvirt   instance int32 IncrDecr::Val()
    IL_0016:  stloc.0
    IL_0017:  br.s       IL_0019

    IL_0019:  ldloc.0
    IL_001a:  ret
  } 

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldtoken    !T
    IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000b:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_0010:  unbox.any  !T
    IL_0015:  stfld      !0 class MyCounter<!T>::counter
    IL_001a:  ldarg.0
    IL_001b:  ldc.i4.1
    IL_001c:  newarr     !T
    IL_0021:  stfld      !0[] class MyCounter<!T>::counters
    IL_0026:  ldarg.0
    IL_0027:  call       instance void [mscorlib]System.Object::.ctor()
    IL_002c:  ret
  } 

} 

.class public auto ansi beforefieldinit test
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  3
    .locals init (class MyCounter<class MyInt> V_0,
             class MyInt V_1,
             int32 V_2)
    IL_0000:  newobj     instance void class MyCounter<class MyInt>::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  callvirt   instance void class MyCounter<class MyInt>::Increment()
    IL_000c:  ldloc.0
    IL_000d:  callvirt   instance int32 class MyCounter<class MyInt>::Val()
    IL_0012:  ldc.i4.s   100
    IL_0014:  beq.s      IL_003c

    IL_0016:  ldstr      "FAILED"
    IL_001b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0020:  ldstr      "Expected: 100, Actual: {0}"
    IL_0025:  ldloc.0
    IL_0026:  callvirt   instance int32 class MyCounter<class MyInt>::Val()
    IL_002b:  box        [mscorlib]System.Int32
    IL_0030:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0035:  ldc.i4.1
    IL_0036:  stloc.2
    IL_0037:  br         IL_0158

    IL_003c:  ldloc.0
    IL_003d:  callvirt   instance void class MyCounter<class MyInt>::Decrement()
    IL_0042:  ldloc.0
    IL_0043:  callvirt   instance int32 class MyCounter<class MyInt>::Val()
    IL_0048:  brfalse.s  IL_0070

    IL_004a:  ldstr      "FAILED"
    IL_004f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0054:  ldstr      "Expected: 100, Actual: {0}"
    IL_0059:  ldloc.0
    IL_005a:  callvirt   instance int32 class MyCounter<class MyInt>::Val()
    IL_005f:  box        [mscorlib]System.Int32
    IL_0064:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0069:  ldc.i4.2
    IL_006a:  stloc.2
    IL_006b:  br         IL_0158

    IL_0070:  ldloc.0
    IL_0071:  ldc.i4.0
    IL_0072:  callvirt   instance void class MyCounter<class MyInt>::Increment(int32)
    IL_0077:  ldloc.0
    IL_0078:  ldc.i4.0
    IL_0079:  callvirt   instance int32 class MyCounter<class MyInt>::Val(int32)
    IL_007e:  ldc.i4.s   100
    IL_0080:  beq.s      IL_00a9

    IL_0082:  ldstr      "FAILED"
    IL_0087:  call       void [System.Console]System.Console::WriteLine(string)
    IL_008c:  ldstr      "Expected: 100, Actual: {0}"
    IL_0091:  ldloc.0
    IL_0092:  ldc.i4.0
    IL_0093:  callvirt   instance int32 class MyCounter<class MyInt>::Val(int32)
    IL_0098:  box        [mscorlib]System.Int32
    IL_009d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_00a2:  ldc.i4.3
    IL_00a3:  stloc.2
    IL_00a4:  br         IL_0158

    IL_00a9:  ldloc.0
    IL_00aa:  ldc.i4.0
    IL_00ab:  callvirt   instance void class MyCounter<class MyInt>::Decrement(int32)
    IL_00b0:  ldloc.0
    IL_00b1:  ldc.i4.0
    IL_00b2:  callvirt   instance int32 class MyCounter<class MyInt>::Val(int32)
    IL_00b7:  brfalse.s  IL_00dd

    IL_00b9:  ldstr      "FAILED"
    IL_00be:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00c3:  ldstr      "Expected: 100, Actual: {0}"
    IL_00c8:  ldloc.0
    IL_00c9:  ldc.i4.0
    IL_00ca:  callvirt   instance int32 class MyCounter<class MyInt>::Val(int32)
    IL_00cf:  box        [mscorlib]System.Int32
    IL_00d4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_00d9:  ldc.i4.4
    IL_00da:  stloc.2
    IL_00db:  br.s       IL_0158

    IL_00dd:  newobj     instance void MyInt::.ctor()
    IL_00e2:  stloc.1
    IL_00e3:  ldloc.0
    IL_00e4:  ldloc.1
    IL_00e5:  callvirt   instance void class MyCounter<class MyInt>::Increment2(!0)
    IL_00ea:  ldloc.0
    IL_00eb:  callvirt   instance int32 class MyCounter<class MyInt>::Val()
    IL_00f0:  ldc.i4.s   100
    IL_00f2:  beq.s      IL_0117

    IL_00f4:  ldstr      "FAILED"
    IL_00f9:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00fe:  ldstr      "Expected: 100, Actual: {0}"
    IL_0103:  ldloc.0
    IL_0104:  callvirt   instance int32 class MyCounter<class MyInt>::Val()
    IL_0109:  box        [mscorlib]System.Int32
    IL_010e:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0113:  ldc.i4.5
    IL_0114:  stloc.2
    IL_0115:  br.s       IL_0158

    IL_0117:  ldloc.0
    IL_0118:  ldloc.1
    IL_0119:  callvirt   instance void class MyCounter<class MyInt>::Decrement2(!0)
    IL_011e:  ldloc.0
    IL_011f:  callvirt   instance int32 class MyCounter<class MyInt>::Val()
    IL_0124:  brfalse.s  IL_0149

    IL_0126:  ldstr      "FAILED"
    IL_012b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0130:  ldstr      "Expected: 100, Actual: {0}"
    IL_0135:  ldloc.0
    IL_0136:  callvirt   instance int32 class MyCounter<class MyInt>::Val()
    IL_013b:  box        [mscorlib]System.Int32
    IL_0140:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0145:  ldc.i4.6
    IL_0146:  stloc.2
    IL_0147:  br.s       IL_0158

    IL_0149:  ldstr      "PASSED"
    IL_014e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0153:  ldc.i4.s   100
    IL_0155:  stloc.2
    IL_0156:  br.s       IL_0158

    IL_0158:  ldloc.2
    IL_0159:  ret
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
