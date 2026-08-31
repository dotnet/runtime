// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// valuetype -Inherited method
// ldflda, ldelema, ldarga
// Val*A: constrained, Val*B: box

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
  .method public hidebysig instance int32 
          Val1A() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldflda      !0 class MyCounter<!T>::counter
    IL_0006:  //box        !T
    constrained. !T
    IL_000b:  callvirt   instance int32 [mscorlib]System.Object::GetHashCode()
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret
  } // end of method MyCounter::Val1A

  .method public hidebysig instance int32 
          Val2A() cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      !0[] class MyCounter<!T>::counters
    IL_0006:  ldc.i4.0
    IL_0007:  ldelema     !T
    IL_000c:  //box        !T
    constrained. !T
    IL_0011:  callvirt   instance int32 [mscorlib]System.Object::GetHashCode()
    IL_0016:  stloc.0
    IL_0017:  br.s       IL_0019

    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method MyCounter::Val2A

  .method public hidebysig instance int32 
          Val3A(!T cnter) cil managed
  {
    // Code size       28 (0x1c)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      !0 class MyCounter<!T>::counter
    IL_0007:  ldarg.0
    IL_0008:  ldflda      !0 class MyCounter<!T>::counter
    IL_000d:  //box        !T
    constrained. !T
    IL_0012:  callvirt   instance int32 [mscorlib]System.Object::GetHashCode()
    IL_0017:  stloc.0
    IL_0018:  br.s       IL_001a

    IL_001a:  ldloc.0
    IL_001b:  ret
  } // end of method MyCounter::Val3A

  .method public hidebysig instance int32 
          Val1B() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      !0 class MyCounter<!T>::counter
    IL_0006:  box        !T
    IL_000b:  callvirt   instance int32 [mscorlib]System.Object::GetHashCode()
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret
  } // end of method MyCounter::Val1B

  .method public hidebysig instance int32 
          Val2B() cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      !0[] class MyCounter<!T>::counters
    IL_0006:  ldc.i4.0
    IL_0007:  ldelem     !T
    IL_000c:  box        !T
    IL_0011:  callvirt   instance int32 [mscorlib]System.Object::GetHashCode()
    IL_0016:  stloc.0
    IL_0017:  br.s       IL_0019

    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method MyCounter::Val2B

  .method public hidebysig instance int32 
          Val3B(!T cnter) cil managed
  {
    // Code size       28 (0x1c)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      !0 class MyCounter<!T>::counter
    IL_0007:  ldarg.0
    IL_0008:  ldfld      !0 class MyCounter<!T>::counter
    IL_000d:  box        !T
    IL_0012:  callvirt   instance int32 [mscorlib]System.Object::GetHashCode()
    IL_0017:  stloc.0
    IL_0018:  br.s       IL_001a

    IL_001a:  ldloc.0
    IL_001b:  ret
  } // end of method MyCounter::Val3B

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
    // Code size       218 (0xda)
    .maxstack  4
    .locals init (class MyCounter<valuetype MyInt> V_0,
             valuetype MyInt V_1,
             int32 V_2)
    IL_0000:  newobj     instance void class MyCounter<valuetype MyInt>::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val1A()
    IL_000c:  ldloc.0
    IL_000d:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val1B()
    IL_0012:  beq.s      IL_0045

    IL_0014:  ldstr      "FAILED"
    IL_0019:  call       void [System.Console]System.Console::WriteLine(string)
    IL_001e:  ldstr      "mc.Val1A()={0}, mc.Val1B()={0}"
    IL_0023:  ldloc.0
    IL_0024:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val1A()
    IL_0029:  box        [mscorlib]System.Int32
    IL_002e:  ldloc.0
    IL_002f:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val1B()
    IL_0034:  box        [mscorlib]System.Int32
    IL_0039:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_003e:  ldc.i4.1
    IL_003f:  stloc.2
    IL_0040:  br         IL_00d8

    IL_0045:  ldloc.0
    IL_0046:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val2A()
    IL_004b:  ldloc.0
    IL_004c:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val2B()
    IL_0051:  beq.s      IL_0081

    IL_0053:  ldstr      "FAILED"
    IL_0058:  call       void [System.Console]System.Console::WriteLine(string)
    IL_005d:  ldstr      "mc.Val1A()={0}, mc.Val1B()={0}"
    IL_0062:  ldloc.0
    IL_0063:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val2A()
    IL_0068:  box        [mscorlib]System.Int32
    IL_006d:  ldloc.0
    IL_006e:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val2B()
    IL_0073:  box        [mscorlib]System.Int32
    IL_0078:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_007d:  ldc.i4.2
    IL_007e:  stloc.2
    IL_007f:  br.s       IL_00d8

    IL_0081:  ldloca.s   V_1
    IL_0083:  initobj    MyInt
    IL_0089:  ldloc.0
    IL_008a:  ldloc.1
    IL_008b:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val3A(!0)
    IL_0090:  ldloc.0
    IL_0091:  ldloc.1
    IL_0092:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val3B(!0)
    IL_0097:  beq.s      IL_00c9

    IL_0099:  ldstr      "FAILED"
    IL_009e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00a3:  ldstr      "mc.Val1A()={0}, mc.Val1B()={0}"
    IL_00a8:  ldloc.0
    IL_00a9:  ldloc.1
    IL_00aa:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val3A(!0)
    IL_00af:  box        [mscorlib]System.Int32
    IL_00b4:  ldloc.0
    IL_00b5:  ldloc.1
    IL_00b6:  callvirt   instance int32 class MyCounter<valuetype MyInt>::Val3B(!0)
    IL_00bb:  box        [mscorlib]System.Int32
    IL_00c0:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object,
                                                                  object)
    IL_00c5:  ldc.i4.3
    IL_00c6:  stloc.2
    IL_00c7:  br.s       IL_00d8

    IL_00c9:  ldstr      "PASSED"
    IL_00ce:  call       void [System.Console]System.Console::WriteLine(string)
    IL_00d3:  ldc.i4.s   100
    IL_00d5:  stloc.2
    IL_00d6:  br.s       IL_00d8

    IL_00d8:  ldloc.2
    IL_00d9:  ret
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
