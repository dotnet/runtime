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

.class private sequential ansi sealed beforefieldinit MyInt
       extends [mscorlib]System.ValueType
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

} 

.class private auto ansi beforefieldinit MyCounter<(IncrDecr) T>
       extends [mscorlib]System.Object
{
  .param type T 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.NewConstraintAttribute::.ctor() = ( 01 00 00 00 ) 
  .field private !T counter
  .field private !T[] counters
  .method public hidebysig instance void 
          Increment<(IncrDecr) T2>() cil managed
  {
    .param type T2 
    .custom instance void [mscorlib]System.Runtime.CompilerServices.NewConstraintAttribute::.ctor() = ( 01 00 00 00 ) 
    .maxstack  2
    .locals init (!!T2 V_0)
    IL_0000:  ldtoken    !!T2
    IL_0005:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000a:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_000f:  unbox.any  !!T2
    IL_0014:  stloc.0
    IL_0015:  ldloca 0
    IL_0016:  
    IL_001b:  ldc.i4.s   100
    constrained. !!T2
    IL_001d:  callvirt   instance void IncrDecr::Incr(int32)
    IL_0022:  ldarg.0
    IL_0023:  ldloc.0
    IL_0024:  box        !!T2
    IL_0029:  unbox.any  !T
    IL_002e:  stfld      !0 class MyCounter<!T>::counter
    IL_0033:  ret
  } 

  .method public hidebysig instance void 
          Decrement<(IncrDecr) T2>() cil managed
  {
    .param type T2 
    .custom instance void [mscorlib]System.Runtime.CompilerServices.NewConstraintAttribute::.ctor() = ( 01 00 00 00 ) 
    .maxstack  2
    .locals init (!!T2 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      !0 class MyCounter<!T>::counter
    IL_0006:  box        !T
    IL_000b:  unbox.any  !!T2
    IL_0010:  stloc.0
    IL_0011:  ldloc.0
    IL_0012:  box        !!T2
    IL_0017:  ldc.i4.s   100
    IL_0019:  callvirt   instance void IncrDecr::Decr(int32)
    IL_001e:  ldarg.0
    IL_001f:  ldloc.0
    IL_0020:  box        !!T2
    IL_0025:  unbox.any  !T
    IL_002a:  stfld      !0 class MyCounter<!T>::counter
    IL_002f:  ret
  } 

  .method public hidebysig instance void 
          Increment<(IncrDecr) T2>(int32 index) cil managed
  {
    .param type T2 
    .custom instance void [mscorlib]System.Runtime.CompilerServices.NewConstraintAttribute::.ctor() = ( 01 00 00 00 ) 
    .maxstack  4
    .locals init (!!T2[] V_0)
    IL_0000:  ldc.i4.1
    IL_0001:  newarr     !!T2
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  ldarg.1
    IL_0009:  ldtoken    !!T2
    IL_000e:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0013:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
    IL_0018:  unbox.any  !!T2
    IL_001d:  stelem     !!T2
    IL_0022:  ldloc.0
    IL_0023:  ldarg.1
    IL_0024:  ldelema     !!T2
    IL_0029:  
    IL_002e:  ldc.i4.s   100
    constrained. !!T2
    IL_0030:  callvirt   instance void IncrDecr::Incr(int32)
    IL_0035:  ldarg.0
    IL_0036:  ldfld      !0[] class MyCounter<!T>::counters
    IL_003b:  ldarg.1
    IL_003c:  ldloc.0
    IL_003d:  ldarg.1
    IL_003e:  ldelem     !!T2
    IL_0043:  box        !!T2
    IL_0048:  unbox.any  !T
    IL_004d:  stelem     !T
    IL_0052:  ret
  } 

  .method public hidebysig instance void 
          Decrement<(IncrDecr) T2>(int32 index) cil managed
  {
    .param type T2 
    .custom instance void [mscorlib]System.Runtime.CompilerServices.NewConstraintAttribute::.ctor() = ( 01 00 00 00 ) 
    .maxstack  4
    .locals init (!!T2[] V_0)
    IL_0000:  ldc.i4.1
    IL_0001:  newarr     !!T2
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  ldarg.1
    IL_0009:  ldarg.0
    IL_000a:  ldfld      !0[] class MyCounter<!T>::counters
    IL_000f:  ldarg.1
    IL_0010:  ldelem     !T
    IL_0015:  box        !T
    IL_001a:  unbox.any  !!T2
    IL_001f:  stelem     !!T2
    IL_0024:  ldloc.0
    IL_0025:  ldarg.1
    IL_0026:  ldelem     !!T2
    IL_002b:  box        !!T2
    IL_0030:  ldc.i4.s   100
    IL_0032:  callvirt   instance void IncrDecr::Decr(int32)
    IL_0037:  ldarg.0
    IL_0038:  ldfld      !0[] class MyCounter<!T>::counters
    IL_003d:  ldarg.1
    IL_003e:  ldloc.0
    IL_003f:  ldarg.1
    IL_0040:  ldelem     !!T2
    IL_0045:  box        !!T2
    IL_004a:  unbox.any  !T
    IL_004f:  stelem     !T
    IL_0054:  ret
  } 

  .method public hidebysig newslot virtual 
          instance void  Increment2<(IncrDecr) T2>(!!T2 cnter) cil managed
  {
    .param type T2 
    .custom instance void [mscorlib]System.Runtime.CompilerServices.NewConstraintAttribute::.ctor() = ( 01 00 00 00 ) 
    .maxstack  8
    IL_0000:  ldarga 1
    IL_0001:  
    IL_0006:  ldc.i4.s   100
    constrained. !!T2
    IL_0008:  callvirt   instance void IncrDecr::Incr(int32)
    IL_000d:  ldarg.0
    IL_000e:  ldarg.1
    IL_000f:  box        !!T2
    IL_0014:  unbox.any  !T
    IL_0019:  stfld      !0 class MyCounter<!T>::counter
    IL_001e:  ret
  } 

  .method public hidebysig newslot virtual 
          instance void  Decrement2<(IncrDecr) T2>(!!T2 cnter) cil managed
  {
    .param type T2 
    .custom instance void [mscorlib]System.Runtime.CompilerServices.NewConstraintAttribute::.ctor() = ( 01 00 00 00 ) 
    .maxstack  8
    IL_0000:  ldarg.1
    IL_0001:  box        !!T2
    IL_0006:  ldc.i4.s   100
    IL_0008:  callvirt   instance void IncrDecr::Decr(int32)
    IL_000d:  ldarg.0
    IL_000e:  ldarg.1
    IL_000f:  box        !!T2
    IL_0014:  unbox.any  !T
    IL_0019:  stfld      !0 class MyCounter<!T>::counter
    IL_001e:  ret
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
    .locals init (class MyCounter<valuetype MyInt> V_0,
             valuetype MyInt V_1,
             int32 V_2)
    IL_0000:  newobj     instance void class MyCounter<valuetype MyInt>::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  callvirt   instance void class MyCounter<valuetype MyInt>::Increment<valuetype MyInt>()
    IL_000c:  ldloc.0
    IL_000d:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val()
    IL_0012:  ldc.i4.s   100
    IL_0014:  beq.s      IL_003c

    IL_0016:  ldstr      "FAILED"
    IL_001b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0020:  ldstr      "Expected: 100, Actual: {0}"
    IL_0025:  ldloc.0
    IL_0026:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val()
    IL_002b:  box        [mscorlib]System.Int32
    IL_0030:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0035:  ldc.i4.1
    IL_0036:  stloc.2
    IL_0037:  br         IL_015a

    IL_003c:  ldloc.0
    IL_003d:  callvirt   instance void class MyCounter<valuetype MyInt>::Decrement<valuetype MyInt>()
    IL_0042:  ldloc.0
    IL_0043:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val()
    	ldc.i4 100
    IL_0048:  beq.s  IL_0070

    IL_004a:  ldstr      "FAILED"
    IL_004f:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0054:  ldstr      "Expected: 100, Actual: {0}"
    IL_0059:  ldloc.0
    IL_005a:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val()
    IL_005f:  box        [mscorlib]System.Int32
    IL_0064:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0069:  ldc.i4.2
    IL_006a:  stloc.2
    IL_006b:  br         IL_015a

    IL_0070:  ldloc.0
    IL_0071:  ldc.i4.0
    IL_0072:  callvirt   instance void class MyCounter<valuetype MyInt>::Increment<valuetype MyInt>(int32)
    IL_0077:  ldloc.0
    IL_0078:  ldc.i4.0
    IL_0079:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val(int32)
    IL_007e:  ldc.i4.s   100
    IL_0080:  beq.s      IL_00a9

    IL_0082:  ldstr      "FAILED"
    IL_0087:  call       void [System.Console]System.Console::WriteLine(string)
    IL_008c:  ldstr      "Expected: 100, Actual: {0}"
    IL_0091:  ldloc.0
    IL_0092:  ldc.i4.0
    IL_0093:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val(int32)
    IL_0098:  box        [mscorlib]System.Int32
    IL_009d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_00a2:  ldc.i4.3
    IL_00a3:  stloc.2
    IL_00a4:  br         IL_015a

    IL_00a9:  ldloc.0
    IL_00aa:  ldc.i4.0
    IL_00ab:  callvirt   instance void class MyCounter<valuetype MyInt>::Decrement<valuetype MyInt>(int32)
    IL_00b0:  ldloc.0
    IL_00b1:  ldc.i4.0
    IL_00b2:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val(int32)
    	ldc.i4 100
    IL_00b7:  beq.s  IL_00dd

    IL_00b9:  ldstr      "FAILED"
    IL_00be:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00c3:  ldstr      "Expected: 100, Actual: {0}"
    IL_00c8:  ldloc.0
    IL_00c9:  ldc.i4.0
    IL_00ca:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val(int32)
    IL_00cf:  box        [mscorlib]System.Int32
    IL_00d4:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_00d9:  ldc.i4.4
    IL_00da:  stloc.2
    IL_00db:  br.s       IL_015a

    IL_00dd:  ldloca.s   V_1
    IL_00df:  initobj    MyInt
    IL_00e5:  ldloc.0
    IL_00e6:  ldloc.1
    IL_00e7:  callvirt   instance void class MyCounter<valuetype MyInt>::Increment2<valuetype MyInt>(!!0)
    IL_00ec:  ldloc.0
    IL_00ed:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val()
    IL_00f2:  ldc.i4.s   100
    IL_00f4:  beq.s      IL_0119

    IL_00f6:  ldstr      "FAILED"
    IL_00fb:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0100:  ldstr      "Expected: 100, Actual: {0}"
    IL_0105:  ldloc.0
    IL_0106:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val()
    IL_010b:  box        [mscorlib]System.Int32
    IL_0110:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0115:  ldc.i4.5
    IL_0116:  stloc.2
    IL_0117:  br.s       IL_015a

    IL_0119:  ldloc.0
    IL_011a:  ldloc.1
    IL_011b:  callvirt   instance void class MyCounter<valuetype MyInt>::Decrement2<valuetype MyInt>(!!0)
    IL_0120:  ldloc.0
    IL_0121:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val()
    IL_0126:  brfalse.s  IL_014b

    IL_0128:  ldstr      "FAILED"
    IL_012d:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0132:  ldstr      "Expected: 100, Actual: {0}"
    IL_0137:  ldloc.0
    IL_0138:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val()
    IL_013d:  box        [mscorlib]System.Int32
    IL_0142:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0147:  ldc.i4.6
    IL_0148:  stloc.2
    IL_0149:  br.s       IL_015a

    IL_014b:  ldstr      "PASSED"
    IL_0150:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0155:  ldc.i4.s   100
    IL_0157:  stloc.2
    IL_0158:  br.s       IL_015a

    IL_015a:  ldloc.2
    IL_015b:  ret
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
