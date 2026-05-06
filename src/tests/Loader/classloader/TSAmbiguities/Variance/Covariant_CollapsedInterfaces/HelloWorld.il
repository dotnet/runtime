// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly HelloWorld
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {713D21D5-3295-43C3-9260-D4E561C25BB7}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00300000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit HelloWorld
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       72 (0x48)
    .maxstack  2
    .locals init (string V_0,
             string V_1,
             int32 V_2,
             bool V_3)
    IL_0000:  nop
    IL_0001:  call       string HelloWorld::StepAB()
    IL_0006:  stloc.0
    IL_0007:  call       string HelloWorld::StepBA()
    IL_000c:  stloc.1
    IL_000d:  ldloc.0
    IL_000e:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0013:  nop
    IL_0014:  ldloc.1
    IL_0015:  call       void [System.Console]System.Console::WriteLine(string)
    IL_001a:  nop
    IL_001b:  ldloc.0
    IL_001c:  ldstr      "XAB.Print(A='C_1')"
    IL_0021:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0026:  brfalse.s  IL_0038

    IL_0028:  ldloc.1
    IL_0029:  ldstr      "XBA.Print(B='C_2')"
    IL_002e:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0033:  ldc.i4.0
    IL_0034:  ceq
    IL_0036:  br.s       IL_0039

    IL_0038:  ldc.i4.1
    IL_0039:  stloc.3
    IL_003a:  ldloc.3
    IL_003b:  brtrue.s   IL_0042

    IL_003d:  ldc.i4.s   100
    IL_003f:  stloc.2
    IL_0040:  br.s       IL_0046

    IL_0042:  ldc.i4.m1
    IL_0043:  stloc.2
    IL_0044:  br.s       IL_0046

    IL_0046:  ldloc.2
    IL_0047:  ret
  } // end of method HelloWorld::Main

  .method public hidebysig static string 
          StepAB() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  3
    .locals init (class I`1<class C> V_0,
             string V_1)
    IL_0000:  nop
    IL_0001:  newobj     instance void XAB::.ctor()
    IL_0006:  castclass  class I`1<class C>
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.1
    IL_000e:  newobj     instance void C::.ctor(int32)
    IL_0013:  callvirt   instance string class I`1<class C>::Print(!0)
    IL_0018:  stloc.1
    IL_0019:  br.s       IL_001b

    IL_001b:  ldloc.1
    IL_001c:  ret
  } // end of method HelloWorld::StepAB

  .method public hidebysig static string 
          StepBA() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  3
    .locals init (class I`1<class C> V_0,
             string V_1)
    IL_0000:  nop
    IL_0001:  newobj     instance void XBA::.ctor()
    IL_0006:  castclass  class I`1<class C>
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  ldc.i4.2
    IL_000e:  newobj     instance void C::.ctor(int32)
    IL_0013:  callvirt   instance string class I`1<class C>::Print(!0)
    IL_0018:  stloc.1
    IL_0019:  br.s       IL_001b

    IL_001b:  ldloc.1
    IL_001c:  ret
  } // end of method HelloWorld::StepBA

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method HelloWorld::.ctor

} // end of class HelloWorld

.class interface private abstract auto ansi I`1<-T>
{
  .method public hidebysig newslot abstract virtual 
          instance string  Print(!T t) cil managed
  {
  } // end of method I`1::Print

} // end of class I`1


.class private auto ansi beforefieldinit A
       extends [mscorlib]System.Object
{
  .field family string 'value'
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(int32 index) cil managed
  {
    // Code size       32 (0x20)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldarg.0
    IL_0009:  ldstr      "A_"
    IL_000e:  ldarg.1
    IL_000f:  box        [mscorlib]System.Int32
    IL_0014:  call       string [mscorlib]System.String::Concat(object,
                                                                object)
    IL_0019:  stfld      string A::'value'
    IL_001e:  nop
    IL_001f:  ret
  } // end of method A::.ctor

  .method public hidebysig specialname instance string 
          get_Value() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      string A::'value'
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a

    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method A::get_Value

  .property instance string Value()
  {
    .get instance string A::get_Value()
  } // end of property A::Value
} // end of class A

.class private auto ansi beforefieldinit B
       extends A
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(int32 index) cil managed
  {
    // Code size       33 (0x21)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       instance void A::.ctor(int32)
    IL_0007:  nop
    IL_0008:  nop
    IL_0009:  ldarg.0
    IL_000a:  ldstr      "B_"
    IL_000f:  ldarg.1
    IL_0010:  box        [mscorlib]System.Int32
    IL_0015:  call       string [mscorlib]System.String::Concat(object,
                                                                object)
    IL_001a:  stfld      string A::'value'
    IL_001f:  nop
    IL_0020:  ret
  } // end of method B::.ctor

} // end of class B

.class private auto ansi beforefieldinit C
       extends B
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(int32 index) cil managed
  {
    // Code size       33 (0x21)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       instance void B::.ctor(int32)
    IL_0007:  nop
    IL_0008:  nop
    IL_0009:  ldarg.0
    IL_000a:  ldstr      "C_"
    IL_000f:  ldarg.1
    IL_0010:  box        [mscorlib]System.Int32
    IL_0015:  call       string [mscorlib]System.String::Concat(object,
                                                                object)
    IL_001a:  stfld      string A::'value'
    IL_001f:  nop
    IL_0020:  ret
  } // end of method C::.ctor

} // end of class C

.class private auto ansi beforefieldinit XAB
       extends [mscorlib]System.Object
       implements class I`1<class A>,
                  class I`1<class B>
{
  .method public hidebysig newslot virtual final 
          instance string  Print(class A a) cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "XAB.Print(A='"
    IL_0006:  ldarg.1
    IL_0007:  callvirt   instance string A::get_Value()
    IL_000c:  ldstr      "')"
    IL_0011:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_0016:  stloc.0
    IL_0017:  br.s       IL_0019

    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method XAB::Print

  .method public hidebysig newslot virtual final 
          instance string  Print(class B b) cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "XAB.Print(B='"
    IL_0006:  ldarg.1
    IL_0007:  callvirt   instance string A::get_Value()
    IL_000c:  ldstr      "')"
    IL_0011:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_0016:  stloc.0
    IL_0017:  br.s       IL_0019

    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method XAB::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method XAB::.ctor

} // end of class XAB

.class private auto ansi beforefieldinit XBA
       extends [mscorlib]System.Object
       implements class I`1<class B>,
                  class I`1<class A>
{
  .method public hidebysig newslot virtual final 
          instance string  Print(class B b) cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "XBA.Print(B='"
    IL_0006:  ldarg.1
    IL_0007:  callvirt   instance string A::get_Value()
    IL_000c:  ldstr      "')"
    IL_0011:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_0016:  stloc.0
    IL_0017:  br.s       IL_0019

    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method XBA::Print

  .method public hidebysig newslot virtual final 
          instance string  Print(class A a) cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "XBA.Print(A='"
    IL_0006:  ldarg.1
    IL_0007:  callvirt   instance string A::get_Value()
    IL_000c:  ldstr      "')"
    IL_0011:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_0016:  stloc.0
    IL_0017:  br.s       IL_0019

    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method XBA::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method XBA::.ctor

} // end of class XBA


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file HelloWorld.res
