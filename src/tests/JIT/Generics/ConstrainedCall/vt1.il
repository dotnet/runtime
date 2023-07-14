// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// valuetype - implemented methods
// ldflda, ldelema, ldarga
// Increment: constrained, Decrement: box

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME {}
.class interface private abstract auto ansi IncrDecr
{
  .method public hidebysig newslot abstract virtual 
          instance void  Incr(int32 a) cil managed
  {
  } // end of method IncrDecr::Incr

  .method public hidebysig newslot abstract virtual 
          instance void  Decr(int32 a) cil managed
  {
  } // end of method IncrDecr::Decr

  .method public hidebysig newslot abstract virtual 
          instance int32  Val() cil managed
  {
  } // end of method IncrDecr::Val

} // end of class IncrDecr

.class private sequential ansi sealed beforefieldinit MyInt
       extends [mscorlib]System.ValueType
       implements IncrDecr
{
  .field private int32 x
  .method public hidebysig newslot virtual final 
          instance void  Incr(int32 a) cil managed
  {
    // Code size       15 (0xf)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  dup
    IL_0002:  ldfld      int32 MyInt::x
    IL_0007:  ldarg.1
    IL_0008:  add
    IL_0009:  stfld      int32 MyInt::x
    IL_000e:  ret
  } // end of method MyInt::Incr

  .method public hidebysig newslot virtual final 
          instance void  Decr(int32 a) cil managed
  {
    // Code size       15 (0xf)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  dup
    IL_0002:  ldfld      int32 MyInt::x
    IL_0007:  ldarg.1
    IL_0008:  sub
    IL_0009:  stfld      int32 MyInt::x
    IL_000e:  ret
  } // end of method MyInt::Decr

  .method public hidebysig newslot virtual final 
          instance int32  Val() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      int32 MyInt::x
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method MyInt::Val

} // end of class MyInt

.class private auto ansi beforefieldinit MyCounter<(IncrDecr) T>
       extends [mscorlib]System.Object
{
  .field private !T counter
  .field private !T[] counters
  .method public hidebysig instance void 
          Increment() cil managed
  {
    // Code size       19 (0x13)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldflda     !0 class MyCounter<!T>::counter
    IL_0006:  //box        !T
    IL_000b:  ldc.i4.s   100
    constrained. !0
    IL_000d:  callvirt   instance void IncrDecr::Incr(int32)
    IL_0012:  ret
  } // end of method MyCounter::Increment

  .method public hidebysig instance void 
          Decrement() cil managed
  {
    // Code size       19 (0x13)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      !0 class MyCounter<!T>::counter
    IL_0006:  box        !T
    IL_000b:  ldc.i4.s   99
    IL_000d:  callvirt   instance void IncrDecr::Decr(int32)
    IL_0012:  ret
  } // end of method MyCounter::Decrement

  .method public hidebysig instance void 
          Increment(int32 index) cil managed
  {
    // Code size       25 (0x19)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      !0[] class MyCounter<!T>::counters
    IL_0006:  ldarg.1
    IL_0007:  ldelema     !T
    IL_000c:  //box        !T
    IL_0011:  ldc.i4.s   100
    constrained. !T
    IL_0013:  callvirt   instance void IncrDecr::Incr(int32)
    IL_0018:  ret
  } // end of method MyCounter::Increment

  .method public hidebysig instance void 
          Decrement(int32 index) cil managed
  {
    // Code size       25 (0x19)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      !0[] class MyCounter<!T>::counters
    IL_0006:  ldarg.1
    IL_0007:  ldelem     !T
    IL_000c:  box        !T
    IL_0011:  ldc.i4.s   99
    IL_0013:  callvirt   instance void IncrDecr::Decr(int32)
    IL_0018:  ret
  } // end of method MyCounter::Decrement

  .method public hidebysig newslot virtual 
          instance void  Increment2(!T cnter) cil managed
  {
  // Code size       21 (0x15)
  .maxstack  8
  IL_0000:  ldarga 1
  IL_0001:  //box        !0
  IL_0006:  ldc.i4.s   100
  constrained. !0
  IL_0008:  callvirt   instance void IncrDecr::Incr(int32)  
  IL_000d:  ldarg.0
  IL_000e:  ldarg.1
  IL_000f:  stfld      !0 class MyCounter<!0>::counter
  IL_0014:  ret
  } // end of method MyCounter::Increment2

  .method public hidebysig newslot virtual 
          instance void  Decrement2(!T cnter) cil managed
  {
  // Code size       21 (0x15)
  .maxstack  8
  IL_0000:  ldarg.1
  IL_0001:  box        !0
  IL_0006:  ldc.i4.s   99
  IL_0008:  callvirt   instance void IncrDecr::Decr(int32)
  IL_000d:  ldarg.0
  IL_000e:  ldarg.1
  IL_000f:  stfld      !0 class MyCounter<!0>::counter
  IL_0014:  ret
  } // end of method MyCounter::Decrement2

  .method public hidebysig instance int32 
          Val() cil managed
  {
    // Code size       21 (0x15)
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
  } // end of method MyCounter::Val

  .method public hidebysig instance int32 
          Val(int32 index) cil managed
  {
    // Code size       27 (0x1b)
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
  } // end of method MyCounter::Val

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       19 (0x13)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.1
    IL_0002:  newarr     !T
    IL_0007:  stfld      !0[] class MyCounter<!T>::counters
    IL_000c:  ldarg.0
    IL_000d:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0012:  ret
  } // end of method MyCounter::.ctor

} // end of class MyCounter

.class public auto ansi beforefieldinit test
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       361 (0x169)
    .maxstack  3
    .locals init (class MyCounter<valuetype MyInt> V_0,
             valuetype MyInt V_1,
             int32 V_2)
    IL_0000:  newobj     instance void class MyCounter<valuetype MyInt>::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  callvirt   instance void class MyCounter<valuetype MyInt>::Increment()
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
    IL_0037:  br         IL_0167

    IL_003c:  ldloc.0
    IL_003d:  callvirt   instance void class MyCounter<valuetype MyInt>::Decrement()
    IL_0042:  ldloc.0
    IL_0043:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val()
    IL_0048:  ldc.i4.s   100
    IL_004a:  beq.s      IL_0072

    IL_004c:  ldstr      "FAILED"
    IL_0051:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0056:  ldstr      "Expected: 100, Actual: {0}"
    IL_005b:  ldloc.0
    IL_005c:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val()
    IL_0061:  box        [mscorlib]System.Int32
    IL_0066:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_006b:  ldc.i4.2
    IL_006c:  stloc.2
    IL_006d:  br         IL_0167

    IL_0072:  ldloc.0
    IL_0073:  ldc.i4.0
    IL_0074:  callvirt   instance void class MyCounter<valuetype MyInt>::Increment(int32)
    IL_0079:  ldloc.0
    IL_007a:  ldc.i4.0
    IL_007b:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val(int32)
    IL_0080:  ldc.i4.s   100
    IL_0082:  beq.s      IL_00ab

    IL_0084:  ldstr      "FAILED"
    IL_0089:  call       void [System.Console]System.Console::WriteLine(string)
    IL_008e:  ldstr      "Expected: 100, Actual: {0}"
    IL_0093:  ldloc.0
    IL_0094:  ldc.i4.0
    IL_0095:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val(int32)
    IL_009a:  box        [mscorlib]System.Int32
    IL_009f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_00a4:  ldc.i4.3
    IL_00a5:  stloc.2
    IL_00a6:  br         IL_0167

    IL_00ab:  ldloc.0
    IL_00ac:  ldc.i4.0
    IL_00ad:  callvirt   instance void class MyCounter<valuetype MyInt>::Decrement(int32)
    IL_00b2:  ldloc.0
    IL_00b3:  ldc.i4.0
    IL_00b4:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val(int32)
    IL_00b9:  ldc.i4.s   100
    IL_00bb:  beq.s      IL_00e4

    IL_00bd:  ldstr      "FAILED"
    IL_00c2:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00c7:  ldstr      "Expected: 100, Actual: {0}"
  IL_00cc:  ldloc.0
  IL_00cd:  ldc.i4.0
  IL_00ce:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val(int32)
  IL_00d3:  box        [mscorlib]System.Int32
  IL_00d8:  call       void [System.Console]System.Console::WriteLine(string,
                                                                object)
  IL_00dd:  ldc.i4.4
  IL_00de:  stloc.2
  IL_00df:  br         IL_0169
  IL_00e4:  ldloca.s   V_1
  IL_00e6:  initobj    MyInt
  IL_00ec:  ldloc.0
  IL_00ed:  ldloc.1
  IL_00ee:  callvirt   instance void class MyCounter<valuetype MyInt>::Increment2(!0)
  IL_00f3:  ldloc.0
  IL_00f4:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val()
  IL_00f9:  ldc.i4     100
  IL_00fe:  beq.s      IL_0123
  IL_0100:  ldstr      "FAILED"
  IL_0105:  call       void [System.Console]System.Console::WriteLine(string)
  IL_010a:  ldstr      "Expected: 100, Actual: {0}"
  IL_010f:  ldloc.0
  IL_0110:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val()
  IL_0115:  box        [mscorlib]System.Int32
  IL_011a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                object)
  IL_011f:  ldc.i4.5
  IL_0120:  stloc.2
  IL_0121:  br.s       IL_0169
  IL_0123:  ldloc.0
  IL_0124:  ldloc.1
  IL_0125:  callvirt   instance void class MyCounter<valuetype MyInt>::Decrement2(!0)
  IL_012a:  ldloc.0
  IL_012b:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val()
  IL_0130:  ldc.i4     0
  IL_0135:  beq.s      IL_015a
  IL_0137:  ldstr      "FAILED"
  IL_013c:  call       void [System.Console]System.Console::WriteLine(string)
  IL_0141:  ldstr      "Expected: 100, Actual: {0}"
  IL_0146:  ldloc.0
  IL_0147:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val()
  IL_014c:  box        [mscorlib]System.Int32
  IL_0151:  call       void [System.Console]System.Console::WriteLine(string,
                                                                object)
  IL_0156:  ldc.i4.6
  IL_0157:  stloc.2
  IL_0158:  br.s       IL_0169
  IL_015a:  ldstr      "PASSED"
  IL_015f:  call       void [System.Console]System.Console::WriteLine(string)
  IL_0164:  ldc.i4.s   100
  IL_0166:  stloc.2
  IL_0167:  br.s       IL_0169
  IL_0169:  ldloc.2
  IL_016a:  ret

  } // end of method test::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method test::.ctor

}
